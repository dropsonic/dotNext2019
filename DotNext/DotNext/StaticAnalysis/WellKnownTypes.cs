using Microsoft.CodeAnalysis;

namespace DotNext.StaticAnalysis
{
	public static class WellKnownTypes
	{
		public static INamedTypeSymbol IDisposable(Compilation compilation) =>
			compilation.GetTypeByMetadataName("System.IDisposable");
		
		public static INamedTypeSymbol ControllerBase(Compilation compilation) =>
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");

		public static INamedTypeSymbol RouteAttribute(Compilation compilation) =>
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.RouteAttribute");

		public static INamedTypeSymbol HttpMethodAttribute(Compilation compilation) =>
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute");

		public static INamedTypeSymbol HttpGetAttribute(Compilation compilation) =>
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Routing.HttpGetAttribute");

		public static INamedTypeSymbol HttpGetAttribute(Compilation compilation) =>
			compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Routing.HttpGetAttribute");
	}
}
