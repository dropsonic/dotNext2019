using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WhatTheHeck.StaticAnalysis
{
	// Помечаем класс как Roslyn Analyzer для языка C#
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class WhatTheHeckAnalyzer : DiagnosticAnalyzer
	{
		internal const string FWord = "fuck";

		// Список всех диагностик, о которых может сообщать данный анализатор
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(Descriptors.DN1000_WhatTheHeckComment);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			// Подписываемся на окончание парсинга документа
			context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
		}

		private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			// Получаем корневой узел синтаксического дерева
			var root = context.Tree.GetRoot(context.CancellationToken);

			// Создаём SuppressionManager
			var suppressionManager = new SuppressionManager(context.Options);

			// Ищем все SyntaxTrivia в документе...
			foreach (SyntaxTrivia trivia in root.DescendantTrivia()
			// ...которые являются однострочными или многострочными комментариями...
				.Where(t => (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
			// ...содержат неприличное слово...
					&& ContainsFWord(t.ToFullString())
			// ...и не подавлены каким-либо образом (в suppression-файле или с помощью специального комментария)
					&& !suppressionManager.IsSuppressed(Descriptors.DN1000_WhatTheHeckComment, t)))
			{
				// Добавляем диагностику
				context.ReportDiagnostic(
					Diagnostic.Create(Descriptors.DN1000_WhatTheHeckComment, trivia.GetLocation()));
			}
		}


		private static bool ContainsFWord(string text) => !String.IsNullOrEmpty(text) 
		                                                  && (text.StartsWith(FWord, StringComparison.OrdinalIgnoreCase) 
		                                                      || text.IndexOf(FWord.PadLeft(1), StringComparison.OrdinalIgnoreCase) >= 0);

		private static ImmutableHashSet<string> GetSuppressions(ImmutableArray<AdditionalText> additionalFiles)
		{
			var set = ImmutableHashSet<string>.Empty.ToBuilder();

			// Читаем все строки из всех suppression-файлов
			foreach (var file in additionalFiles
				.Where(f => !String.IsNullOrEmpty(f.Path) && f.Path.EndsWith(".suppression", StringComparison.OrdinalIgnoreCase)))
			{
				foreach (var line in file.GetText().Lines)
				{
					set.Add(line.Text.ToString());
				}
			}

			return set.ToImmutable();
		}
	}
}
