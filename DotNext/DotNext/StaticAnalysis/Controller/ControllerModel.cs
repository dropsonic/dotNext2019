using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace DotNext.StaticAnalysis.Controller
{
	[DebuggerDisplay("{Symbol?.Name}, Route = {RoutePrefix}")]
	public class ControllerModel
	{
		public ControllerModel() { }

		public ControllerModel(string routePrefix, INamedTypeSymbol symbol, ImmutableArray<ControllerAction> actions)
		{
			RoutePrefix = routePrefix;
			Symbol = symbol;
			Actions = actions;
		}

		public string RoutePrefix { get; }
		public INamedTypeSymbol Symbol { get; }
		public ImmutableArray<ControllerAction> Actions { get; }

		public ControllerModel WithRoutePrefix(string prefix)
		{
			return new ControllerModel(prefix, Symbol, Actions);
		}

		public ControllerModel WithSymbol(INamedTypeSymbol symbol)
		{
			return new ControllerModel(RoutePrefix, symbol, Actions);
		}

		public ControllerModel WithActions(ImmutableArray<ControllerAction> actions)
		{
			return new ControllerModel(RoutePrefix, Symbol, actions);
		}
	}
}