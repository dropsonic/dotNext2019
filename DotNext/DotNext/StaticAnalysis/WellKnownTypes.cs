using Microsoft.CodeAnalysis;

namespace DotNext.StaticAnalysis
{
	public static class WellKnownTypes
	{
		public static INamedTypeSymbol IDisposable(Compilation compilation) =>
			compilation.GetTypeByMetadataName("System.IDisposable");
	}
}
