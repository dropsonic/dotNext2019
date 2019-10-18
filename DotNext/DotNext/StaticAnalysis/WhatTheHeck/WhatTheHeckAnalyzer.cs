using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis.WhatTheHeck
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
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			// Подписываемся на окончание парсинга документа
			context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
		}

		private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			// Получаем корневой узел синтаксического дерева
			var root = context.Tree.GetRoot(context.CancellationToken);
			
			// Ищем все SyntaxTrivia в документе...
			foreach (SyntaxTrivia trivia in root.DescendantTrivia()
			// ...которые являются однострочными или многострочными комментариями...
				.Where(t => (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
			// ...содержат неприличное слово
					&& ContainsFWord(t.ToFullString())))
			{
				// Добавляем диагностику
				var properties = ImmutableDictionary<string, string>.Empty.Add(
					SuppressionManager.PropertyKey, trivia.ToFullString());
				context.ReportDiagnosticWithSuppressionCheck(
					Diagnostic.Create(Descriptors.DN1000_WhatTheHeckComment, trivia.GetLocation(), properties));
			}
		}

		private static bool ContainsFWord(string text) => !String.IsNullOrEmpty(text) 
		                                                  && (text.StartsWith(FWord, StringComparison.OrdinalIgnoreCase) 
		                                                      || text.IndexOf(FWord.PadLeft(1), StringComparison.OrdinalIgnoreCase) >= 0);
	}
}
