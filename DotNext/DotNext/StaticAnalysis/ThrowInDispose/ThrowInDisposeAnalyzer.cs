using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis.ThrowInDispose
{
	// Помечаем класс как Roslyn Analyzer для языка C#
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ThrowInDisposeAnalyzer : DiagnosticAnalyzer
	{
		// Список всех диагностик, о которых может сообщать данный анализатор
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(Descriptors.DN1001_ThrowInDispose);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterCompilationStartAction(compilationStartContext =>
			{
				compilationStartContext.RegisterOperationBlockStartAction(operationBlockContext =>
				{
					// Проверяем, что мы находимся внутри метода IDisposable.Dispose
					if (operationBlockContext.OwningSymbol is IMethodSymbol methodSymbol
						&& methodSymbol.IsDisposeImplementation(operationBlockContext.Compilation))
					{
						// Подписываемся на операцию "throw"
						operationBlockContext.RegisterOperationAction(operationContext =>
						{
							// Добавляем диагностику
							operationContext.ReportDiagnostic(
								Diagnostic.Create(
									Descriptors.DN1001_ThrowInDispose,
									operationContext.Operation.Syntax.GetLocation()));
						}, OperationKind.Throw);
					}
				});
			});
		}

		//public override void Initialize(AnalysisContext context)
		//{
		//	context.RegisterCompilationStartAction(compilationStartContext =>
		//	{
		//		// Вместо использования Operation API, подписываемся на символы типа "метод"
		//		compilationStartContext.RegisterSymbolAction(symbolContext =>
		//		{
		//			// Проверяем, что мы находимся внутри метода IDisposable.Dispose
		//			if (symbolContext.Symbol is IMethodSymbol methodSymbol
		//				&& methodSymbol.IsDisposeImplementation(symbolContext.Compilation))
		//			{
		//				// Получаем синтаксическую ноду и запускаем визитор на её содержимое
		//				var methodSyntax = methodSymbol.GetSyntax() as CSharpSyntaxNode;
		//				methodSyntax?.Accept(new ThrowWalker(symbolContext,
		//					Descriptors.DN1001_ThrowInDispose));
		//			}
		//		}, SymbolKind.Method);
		//	});
		//}
	}
}
