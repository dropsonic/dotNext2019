using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis.Controller
{
	public interface IControllerAnalyzer
	{
		ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		void Analyze(SymbolAnalysisContext context, ControllerModel model);
	}
}