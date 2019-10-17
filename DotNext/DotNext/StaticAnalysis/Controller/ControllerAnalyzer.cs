using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis.Controller
{
	// Помечаем класс как Roslyn Analyzer для языка C#
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ControllerAnalyzer : DiagnosticAnalyzer
	{
		private readonly ImmutableArray<IControllerAnalyzer> _innerAnalyzers = ImmutableArray.Create<IControllerAnalyzer>(
			new FooControllerAnalyzer());

		// Список всех диагностик, о которых может сообщать данный анализатор
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray<DiagnosticDescriptor>.Empty;

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterCompilationStartAction(compilationStartContext =>
			{
				compilationStartContext.RegisterSymbolAction(symbolContext =>
				{
					var symbol = (INamedTypeSymbol) symbolContext.Symbol;
					var model = CreateSemanticModel(symbol, symbolContext.Compilation);

					if (model != null)
					{
						var parallelOptions = new ParallelOptions
						{
							CancellationToken = symbolContext.CancellationToken
						};

						Parallel.ForEach(_innerAnalyzers, parallelOptions, innerAnalyzer =>
						{
							symbolContext.CancellationToken.ThrowIfCancellationRequested();
							innerAnalyzer.Analyze(symbolContext, model);
						});
					}
				}, SymbolKind.NamedType);
			});
		}

		private ControllerModel CreateSemanticModel(INamedTypeSymbol symbol, Compilation compilation)
		{
			var controllerBase = WellKnownTypes.ControllerBase(compilation);
			if (controllerBase == null || !symbol.InheritsFrom(controllerBase))
				return null;

			var model = new ControllerModel().WithSymbol(symbol);

			var controllerRoute = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.Equals(WellKnownTypes.RouteAttribute(compilation)));
			if (controllerRoute != null)
			{
				var prefix = controllerRoute.ConstructorArguments[0].Values
					.FirstOrDefault(a => a.Type.SpecialType == SpecialType.System_String);
				model = model.WithRoutePrefix(prefix.Value as string);
			}

			var actions = ImmutableArray<ControllerAction>.Empty.ToBuilder();

			foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>()
				.Where(m => m.DeclaredAccessibility.HasFlag(Accessibility.Public)))
			{
				HttpMethod httpMethod = null;
				string route = null;

				foreach (var attr in method.GetAttributes())
				{
					if (attr.AttributeClass.Equals(WellKnownTypes.HttpGetAttribute(compilation)))
						httpMethod = HttpMethod.Get;
					else if (attr.AttributeClass.Equals(WellKnownTypes.HttpPostAttribute(compilation)))
						httpMethod = HttpMethod.Post;
					else if (attr.AttributeClass.Equals(WellKnownTypes.RouteAttribute(compilation)))
					{
						route = attr.ConstructorArguments[0].Values
							.FirstOrDefault(a => a.Type.SpecialType == SpecialType.System_String).Value as string;
					}
				}

				if (httpMethod != null)
					actions.Add(new ControllerAction(httpMethod, route, method));
			}

			return model.WithActions(actions.ToImmutable());
		}
	}

	public interface IControllerAnalyzer
	{
		void Analyze(SymbolAnalysisContext context, ControllerModel model);
	}

	public class FooControllerAnalyzer : IControllerAnalyzer
	{
		public void Analyze(SymbolAnalysisContext context, ControllerModel model)
		{
			
		}
	}

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
