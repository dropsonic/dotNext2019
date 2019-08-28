using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WhatTheHeck.StaticAnalysis
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class WhatTheHeckAnalyzer : DiagnosticAnalyzer
	{
		private const string FWord = "fuck";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.DN1000_WhatTheHeckComment);

		public override void Initialize(AnalysisContext context)
		{
			//context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
		}

		private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			var root = context.Tree.GetRoot(context.CancellationToken);

			foreach (var trivia in root.DescendantTrivia()
				.Where(t => (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
					&& ContainsFWord(t.ToFullString())))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(Descriptors.DN1000_WhatTheHeckComment, trivia.GetLocation()));
			}
		}

		private static bool ContainsFWord(string text) => !String.IsNullOrEmpty(text) 
		                                                  && (text.StartsWith(FWord, StringComparison.OrdinalIgnoreCase) 
		                                                      || text.IndexOf(FWord.PadLeft(1), StringComparison.OrdinalIgnoreCase) >= 0);
	}
}
