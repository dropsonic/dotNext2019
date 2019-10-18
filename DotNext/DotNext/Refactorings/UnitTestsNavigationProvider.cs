using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext.StaticAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
// ReSharper disable UnusedMember.Global

namespace DotNext.Refactorings
{
	// Помечаем класс как refactoring provider для C#
	[ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
	public class UnitTestsNavigationProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			// Находим синтаксическую ноду по текущему положению курсора
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var node = root?.FindNode(context.Span);
			if (node == null) return;
			
			// Получаем именованный тип через семантическую модель
			var semanticModel = await context.Document
				.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
			var type = semanticModel.GetDeclaredSymbol(node, context.CancellationToken) as INamedTypeSymbol;

			// Нас интересуют только объявления классов
			if (type == null || type.TypeKind != TypeKind.Class) return;
			
			// Ищем во всех проектах текущего solution'а
			// соответствующий класс с юнит-тестами,
			// предполагая, что он имеет постфикс "Tests"
			string typeName = type.MetadataName.Split('.').Last() + "Tests";

			INamedTypeSymbol testsClass = null;
			Project testProject = null;
			
			foreach (var project in context.Document.Project.Solution.Projects)
			{
				var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
				testsClass = compilation
					.GetSymbolsWithName(n => n == typeName, SymbolFilter.Type, context.CancellationToken)
					.OfType<INamedTypeSymbol>()
					.FirstOrDefault();

				if (testsClass != null)
				{
					testProject = project;
					break;
				}
			}

			// Получаем для него синтаксическую ноду...
			var testsClassNode = testsClass?.GetSyntax();
			if (testsClassNode == null) return;

			// ...а по ней — позицию в документе
			int position = testsClassNode.GetLocation().SourceSpan.Start;

			// Ищем документ, в котором эта синтаксическая нода находится
			var filePath = testsClassNode.SyntaxTree?.FilePath;
			var document = testProject.Documents
				.FirstOrDefault(d => d.FilePath == filePath);
			if (document == null) return;

			// Добавляем рефакторинг
			string title = $"Go To Unit Tests ({testsClass.Name})";
			context.RegisterRefactoring(
				new NavigationCodeAction(title, document.Id, position));
		}

		private class NavigationCodeAction : CodeAction
		{
			private readonly DocumentId _documentId;
			private readonly int _position;

			public override string Title { get; }
			public override string EquivalenceKey => Title;
			
			// Для совершения навигации нужно:
			// 1. Документ, который мы хотим открыть (DocumentId из Workspaces API)
			// 2. Позиция в документе (int position)
			public NavigationCodeAction(string title, DocumentId documentId, int position)
			{
				_documentId = documentId;
				_position = position;
				Title = title;
			}
			
			// Возвращаем операцию навигации в определённый документ и определённую позицию
			// как результат нашего CodeAction'а
			protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
				CancellationToken cancellationToken)
			{
				IEnumerable<CodeActionOperation> operations = new CodeActionOperation[]
				{
					new DocumentNavigationOperation(_documentId, _position),
				};

				return Task.FromResult(operations);
			}
		}
	}
}
