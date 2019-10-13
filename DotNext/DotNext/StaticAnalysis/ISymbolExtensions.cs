using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace DotNext.StaticAnalysis
{
	internal static class ISymbolExtensions
	{
		/// <summary>
		/// Checks if the given method implements IDisposable.Dispose()
		/// </summary>
		public static bool IsDisposeImplementation(this IMethodSymbol method, Compilation compilation)
		{
			INamedTypeSymbol iDisposable = WellKnownTypes.IDisposable(compilation);
			return method.IsDisposeImplementation(iDisposable);
		}

		/// <summary>
		/// Checks if the given method implements <see cref="IDisposable.Dispose"/> or overrides an implementation of <see cref="IDisposable.Dispose"/>.
		/// </summary>
		public static bool IsDisposeImplementation(this IMethodSymbol method, INamedTypeSymbol iDisposable)
		{
			if (method == null)
			{
				return false;
			}

			if (method.IsOverride)
			{
				return method.OverriddenMethod.IsDisposeImplementation(iDisposable);
			}

			// Identify the implementor of IDisposable.Dispose in the given method's containing type and check
			// if it is the given method.
			return method.ReturnsVoid &&
			       method.Parameters.Length == 0 &&
			       method.IsImplementationOfInterfaceMethod(null, iDisposable, "Dispose");
		}

		/// <summary>
		/// Checks if the given method is an implementation of the given interface method 
		/// Substituted with the given typeargument.
		/// </summary>
		public static bool IsImplementationOfInterfaceMethod(this IMethodSymbol method, ITypeSymbol typeArgument, INamedTypeSymbol interfaceType, string interfaceMethodName)
		{
			INamedTypeSymbol constructedInterface = typeArgument != null ? interfaceType?.Construct(typeArgument) : interfaceType;

			return constructedInterface?.GetMembers(interfaceMethodName).Single() is IMethodSymbol interfaceMethod && method.Equals(method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod));
		}

		public static SyntaxNode GetSyntax(this ISymbol symbol, CancellationToken cancellationToken = default)
		{
			if (symbol == null)
				return null;

			var declarations = symbol.DeclaringSyntaxReferences;

			if (declarations.Length == 0)
				return null;

			return declarations[0].GetSyntax(cancellationToken);
		}
	}
}
