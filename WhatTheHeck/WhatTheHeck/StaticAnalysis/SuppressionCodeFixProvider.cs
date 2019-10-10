using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace WhatTheHeck.StaticAnalysis
{
	[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
	public sealed class SuppressionCodeFixProvider : CodeFixProvider
	{
		static SuppressionCodeFixProvider()
		{
			Type diagnosticsType = typeof(Descriptors);
			var propertiesInfo = diagnosticsType.GetRuntimeProperties();

			_fixableDiagnosticIds = propertiesInfo
				.Where(x => x.PropertyType == typeof(DiagnosticDescriptor))
				.Select(x =>
				{
					var descriptor = (DiagnosticDescriptor)x.GetValue(x);
					return descriptor.Id;
				})
				.ToImmutableArray();
		}

		private static readonly ImmutableArray<string> _fixableDiagnosticIds;
		public override ImmutableArray<string> FixableDiagnosticIds { get; } =
			_fixableDiagnosticIds;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			foreach (var diagnostic in context.Diagnostics)
			{
				AddSuppressInFile(context, diagnostic);
				await AddSuppressByCommentAsync(context, diagnostic).ConfigureAwait(false);
			}
		}

		private void AddSuppressInFile(CodeFixContext context, Diagnostic diagnostic)
		{
			context.RegisterCodeFix(new SuppressInFileCodeAction(diagnostic, context.Document), diagnostic);
		}

		private async Task AddSuppressByCommentAsync(CodeFixContext context, Diagnostic diagnostic)
		{
			string title = $"Suppress {diagnostic.Id} with a comment";
			var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);
			var token = root.FindToken(context.Span.Start);

			context.RegisterCodeFix(CodeAction.Create(title, ct =>
			{
				var suppressionTrivia = SyntaxFactory.Comment(
					String.Format(SuppressionManager.SuppressionCommentFormat, diagnostic.Id));
				string text = diagnostic.Properties[SuppressionManager.PropertyKey];

				SyntaxTrivia whitespaceTrivia = SyntaxFactory.Whitespace("");
				foreach (var trivia in token.LeadingTrivia)
				{
					if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
						whitespaceTrivia = trivia;
					if (trivia.ToFullString() == text)
						break;
				}

				var newToken = token
					.WithLeadingTrivia(token.LeadingTrivia
						.Insert(0, SyntaxFactory.EndOfLine(Environment.NewLine))
						.Insert(0, suppressionTrivia)
						.Insert(0, whitespaceTrivia));
				
				var newRoot = root.ReplaceToken(token, newToken);
				return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));

			}, title), diagnostic);
		}
	}

	internal class SuppressInFileCodeAction : CodeAction
	{
		private readonly Diagnostic _diagnostic;
		private readonly Document _document;

		public SuppressInFileCodeAction(Diagnostic diagnostic, Document document)
		{
			_diagnostic = diagnostic;
			_document = document;
			Title = EquivalenceKey = $"Suppress {diagnostic.Id} in suppression file";
		}

		public override string Title { get; }
		public override string EquivalenceKey { get; }

		// Отключаем preview
		protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(
				CancellationToken cancellationToken) => Task.FromResult<IEnumerable<CodeActionOperation>>(null);

		protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			=> Task.FromResult<Document>(null);

		protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
		{
			var project = _document.Project;
			var suppressionDoc = project.AdditionalDocuments
				.FirstOrDefault(d => !String.IsNullOrEmpty(d.FilePath)
				                     && d.FilePath.EndsWith(SuppressionManager.SuppressionFileExtension));

			Solution solution;
			if (suppressionDoc == null)
			{
				solution = project.Solution.AddAdditionalDocument(DocumentId.CreateNewId(project.Id, debugName: "Suppression File"),
					name: project.Name + SuppressionManager.SuppressionFileExtension,
					text: _diagnostic.Properties[SuppressionManager.PropertyKey]);
			}
			else
			{
				SourceText text = await suppressionDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
				string newText = text + Environment.NewLine + _diagnostic.Properties[SuppressionManager.PropertyKey];
				solution = project.Solution
					.RemoveAdditionalDocument(suppressionDoc.Id)
					.AddAdditionalDocument(suppressionDoc.Id, suppressionDoc.Name, newText,
						suppressionDoc.Folders, suppressionDoc.FilePath);
			}
			
			return solution;
		}
	}
}
