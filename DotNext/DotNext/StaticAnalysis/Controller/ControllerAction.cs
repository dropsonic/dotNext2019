using System.Diagnostics;
using System.Net.Http;
using Microsoft.CodeAnalysis;

namespace DotNext.StaticAnalysis.Controller
{
	[DebuggerDisplay("{Method}, Route = {Route}, Name = {Symbol?.Name}")]
	public class ControllerAction
	{
		public ControllerAction(HttpMethod method, string route, IMethodSymbol symbol)
		{
			Method = method;
			Route = route;
			Symbol = symbol;
		}

		public HttpMethod Method { get; }
		public string Route { get; }
		public IMethodSymbol Symbol { get; }
	}
}