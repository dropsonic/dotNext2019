using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
			var suppressionManager = SuppressionManager.Get(options);

			if (diagnostic.Location.SourceTree != null)
			{
				var root = diagnostic.Location.SourceTree.GetRoot();
				var span = diagnostic.Location.SourceSpan;
				var trivia = root.FindTrivia(span.Start);
				
				// Диагностика была добавлена непосредственно на trivia
				if (trivia.FullSpan.Equals(span))
				{
					if (suppressionManager.IsSuppressed(diagnostic.Descriptor, trivia))
						return;

					//var syntaxNode = root.FindNode(span);
					//string nodeText = GetSyntaxNodeText(syntaxNode) ?? String.Empty;
					//string targetText = trivia.ToString();
				}
			}

			reportDiagnostic(diagnostic);
		}

		private static string GetSyntaxNodeText(SyntaxNode syntaxNode)
		{
			switch (syntaxNode)
			{
				case FieldDeclarationSyntax fieldDeclaration:
					return fieldDeclaration.Declaration.ToString();
				case PropertyDeclarationSyntax propertyDeclaration:
					return propertyDeclaration.Identifier.ToString();
				case ClassDeclarationSyntax classDeclaration:
					return classDeclaration.Identifier.ToString();
				case MethodDeclarationSyntax methodDeclaration:
					return methodDeclaration.Identifier.ToString();
				case ConstructorDeclarationSyntax constructorDeclaration:
					return constructorDeclaration.Identifier.ToString();
				default:
					return null;
			}
		}
	}
}
