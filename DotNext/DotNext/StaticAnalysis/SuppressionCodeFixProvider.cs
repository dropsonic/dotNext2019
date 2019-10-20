using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

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

			context.RegisterCodeFix(CodeAction.Create(title, ct =>
			{
				// Пытаемся найти suppression-файл внутри проекта
				var project = context.Document.Project;
				var suppressionDoc = project.AdditionalDocuments
					.FirstOrDefault(d => !String.IsNullOrEmpty(d.FilePath)
					                     && d.FilePath.EndsWith(SuppressionManager.SuppressionFileExtension));

				var text = SuppressionManager.Get(project.AnalyzerOptions)
					.AddSuppression(diagnostic)
					.ToText();

				Solution solution;
				// Если файла нет, то создаём его
				if (suppressionDoc == null)
				{
					// Есть открытый баг от 2015 года, что Roslyn не проставляет Build Action = AdditionalFiles
					// https://github.com/dotnet/roslyn/issues/4655
					solution = project.Solution.AddAdditionalDocument(
						DocumentId.CreateNewId(project.Id, debugName: "Suppression File"),
						name: project.Name + SuppressionManager.SuppressionFileExtension,
						text: text);
				}
				// Если файл уже есть, дописываем в конец новую строчку и обновляем solution
				else
				{
					solution = project.Solution
						.WithAdditionalDocumentText(suppressionDoc.Id, text);
				}
			
				return Task.FromResult(solution);
			}, title), diagnostic);
		}

		private void AddSuppressByComment(CodeFixContext context, Diagnostic diagnostic)
		{
			string title = $"Suppress {diagnostic.Id} with a comment";

			context.RegisterCodeFix(CodeAction.Create(title, ct =>
			{
				var suppressionManager = SuppressionManager.Get(
					context.Document.Project.AnalyzerOptions);

				return suppressionManager.AddSuppressionCommentAsync(
					context.Document, diagnostic);
			}, title), diagnostic);
		}
	}
}
