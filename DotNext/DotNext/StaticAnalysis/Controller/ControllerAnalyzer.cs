using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
// ReSharper disable UnusedMember.Global
// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace DotNext.StaticAnalysis.Controller
{
	// Помечаем класс как Roslyn Analyzer для языка C#
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ControllerAnalyzer : DiagnosticAnalyzer
	{
		private readonly ImmutableArray<IControllerAnalyzer> _innerAnalyzers;

		// Список всех диагностик, о которых может сообщать данный анализатор
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

		public ControllerAnalyzer()
			: this(new ControllerActionDuplicateAnalyzer())
		{
		}

		// Конструктор для unit-тестов (чтобы тестировать каждый внутренний анализатор по-отдельности)
		public ControllerAnalyzer(params IControllerAnalyzer[] innerAnalyzers)
		{
			_innerAnalyzers = ImmutableArray.Create(innerAnalyzers);
			SupportedDiagnostics = ImmutableArray.CreateRange(innerAnalyzers.SelectMany(a => a.SupportedDiagnostics));
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(compilationStartContext =>
			{
				compilationStartContext.RegisterSymbolAction(symbolContext =>
				{
					// Создаём одну общую модель для всех анализаторов
					var symbol = (INamedTypeSymbol) symbolContext.Symbol;
					var model = CreateSemanticModel(symbol, symbolContext.Compilation);

					if (model != null)
					{
						// Запускаем все "внутренние" анализаторы в параллель
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
			// Проверяем, что это класс-наследник ControllerBase
			var controllerBase = WellKnownTypes.ControllerBase(compilation);
			if (controllerBase == null || !symbol.InheritsFrom(controllerBase))
				return null;

			var model = new ControllerModel().WithSymbol(symbol);

			// Получаем RouteAttribute для контроллера
			var controllerRoute = symbol.GetAttributes()
				.FirstOrDefault(a => a.AttributeClass.Equals(WellKnownTypes.RouteAttribute(compilation)));
			if (controllerRoute != null)
			{
				var prefix = controllerRoute.ConstructorArguments
					.FirstOrDefault(a => a.Type.SpecialType == SpecialType.System_String);
				model = model.WithRoutePrefix(prefix.Value as string);
			}

			var actions = ImmutableArray<ControllerAction>.Empty.ToBuilder();

			// Собираем информацию о controller actions
			foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>()
				.Where(m => m.DeclaredAccessibility.HasFlag(Accessibility.Public)))
			{
				HttpMethod httpMethod = null;
				string route = null;

				// Собираем атрибуты action'а:
				foreach (var attr in method.GetAttributes())
				{
					// 1. Http Method
					if (attr.AttributeClass.Equals(WellKnownTypes.HttpGetAttribute(compilation)))
						httpMethod = HttpMethod.Get;
					else if (attr.AttributeClass.Equals(WellKnownTypes.HttpPostAttribute(compilation)))
						httpMethod = HttpMethod.Post;
					// 2. Route
					else if (attr.AttributeClass.Equals(WellKnownTypes.RouteAttribute(compilation)))
					{
						route = attr.ConstructorArguments
							.FirstOrDefault(a => a.Type.SpecialType == SpecialType.System_String).Value as string;
					}
				}

				if (httpMethod != null)
					actions.Add(new ControllerAction(httpMethod, route, method));
			}

			return model.WithActions(actions.ToImmutable());
		}
	}
}
