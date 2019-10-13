using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis.ThrowInDispose
{
	internal class ThrowWalker : NestedInvocationWalker
	{
		private readonly SymbolAnalysisContext _context;
		private readonly DiagnosticDescriptor _diagnosticDescriptor;

		public ThrowWalker(SymbolAnalysisContext context, DiagnosticDescriptor diagnosticDescriptor) 
			: base(context.Compilation, context.CancellationToken)
		{
			_context = context;
			_diagnosticDescriptor = diagnosticDescriptor;
		}

		public override void VisitThrowStatement(ThrowStatementSyntax node)
		{
			ReportDiagnostic(_context.ReportDiagnostic, _diagnosticDescriptor, node);
		}
	}
}
