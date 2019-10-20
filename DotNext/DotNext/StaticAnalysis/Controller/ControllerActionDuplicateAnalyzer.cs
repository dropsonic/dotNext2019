using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis.Controller
{
	public class ControllerActionDuplicateAnalyzer : IControllerAnalyzer
	{
		// Список всех диагностик, о которых может сообщать данный анализатор
		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(Descriptors.DN1002_DuplicateControllerAction);

		public void Analyze(SymbolAnalysisContext context, ControllerModel model)
		{
			// Находим дубликаты
			foreach (var group in model.Actions.GroupBy(a => (a.Method, a.Route)))
			{
				if (group.Count() > 1)
				{
					// На каждый метод-дубликат добавляем диагностику
					foreach (var action in group)
					{
						context.ReportDiagnostic(
							Diagnostic.Create(
								Descriptors.DN1002_DuplicateControllerAction,
								action.Symbol.Locations[0]));
					}
				}
			}
		}
	}
}