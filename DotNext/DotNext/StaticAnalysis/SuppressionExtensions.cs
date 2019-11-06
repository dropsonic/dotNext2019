using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis
{
	internal static class SuppressionExtensions
	{
		public static void ReportDiagnosticWithSuppressionCheck(
			this SyntaxTreeAnalysisContext context,
			Diagnostic diagnostic)
		{
			ReportDiagnosticWithSuppressionCheck(context.Options, diagnostic, context.ReportDiagnostic);
		}

		private static void ReportDiagnosticWithSuppressionCheck(
			AnalyzerOptions options,
			Diagnostic diagnostic, 
			Action<Diagnostic> reportDiagnostic)
		{
			if (!SuppressionManager.Get(options).IsSuppressed(diagnostic))
				reportDiagnostic(diagnostic);
		}

		// ...
		// ещё несколько extension методов для других типов анализаторов
	}
}
