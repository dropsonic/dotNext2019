using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace DotNext.StaticAnalysis
{
	[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
	public sealed class SuppressionCodeFixProvider : CodeFixProvider
	{
		static SuppressionCodeFixProvider()
		{
			// Регистрируем текущий code fix provider для всех существующих диагностик
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
		public override ImmutableArray<string> FixableDiagnosticIds => _fixableDiagnosticIds;

		public override FixAllProvider GetFixAllProvider() => null;

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			// Для каждой найденной диагностики добавляем два code fix'а:
			foreach (var diagnostic in context.Diagnostics)
			{
				// suppress диагностики в suppression файле
				AddSuppressInFile(context, diagnostic);
				// и suppress диагностики с помощью комментария
				AddSuppressByComment(context, diagnostic);
			}

			return Task.CompletedTask;
		}

		private void AddSuppressInFile(CodeFixContext context, Diagnostic diagnostic)
		{
			string title = $"Suppress {diagnostic.Id} in suppression file";

			context.RegisterCodeFix(CodeAction.Create(title, async ct =>
			{
				// Пытаемся найти suppression-файл внутри проекта
				var project = context.Document.Project;
				var suppressionDoc = project.AdditionalDocuments
					.FirstOrDefault(d => !String.IsNullOrEmpty(d.FilePath)
					                     && d.FilePath.EndsWith(SuppressionManager.SuppressionFileExtension));

				Solution solution;
				// Если файла нет, то создаём его
				if (suppressionDoc == null)
				{
					// Есть открытый баг от 2015 года, что Roslyn не проставляет Build Action = AdditionalFiles
					// https://github.com/dotnet/roslyn/issues/4655
					solution = project.Solution.AddAdditionalDocument(
						DocumentId.CreateNewId(project.Id, debugName: "Suppression File"),
						name: project.Name + SuppressionManager.SuppressionFileExtension,
						text: diagnostic.Properties[SuppressionManager.PropertyKey]);
				}
				// Если файл уже есть, дописываем в конец новую строчку и обновляем solution
				else
				{
					SourceText text = await suppressionDoc.GetTextAsync(ct).ConfigureAwait(false);
					string newText = text 
					                 + Environment.NewLine 
					                 + diagnostic.Properties[SuppressionManager.PropertyKey];

					solution = project.Solution
						.WithAdditionalDocumentText(suppressionDoc.Id, SourceText.From(newText));
				}
			
				return solution;
			}, title), diagnostic);
		}

		private void AddSuppressByComment(CodeFixContext context, Diagnostic diagnostic)
		{
			string title = $"Suppress {diagnostic.Id} with a comment";

			context.RegisterCodeFix(CodeAction.Create(title, async ct =>
			{
				// Находим токен, к которому привязан комментарий
				var root = await context.Document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
				var token = root.FindToken(context.Span.Start);

				// Создаём suppression-комментарий
				var suppressionTrivia = SyntaxFactory.Comment(
					String.Format(SuppressionManager.SuppressionCommentFormat, diagnostic.Id));

				// Вставляем его перед оригинальным комментарием с F-word
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
				return context.Document.WithSyntaxRoot(newRoot);

			}, title), diagnostic);
		}
	}
}
