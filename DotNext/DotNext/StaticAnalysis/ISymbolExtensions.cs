using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Gets the base types and this in this collection. The types are returned from the most derived ones to the most base <see cref="Object"/> type
		/// </summary>
		/// <param name="type">The type to act on.</param>
		/// <returns/>
		public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (type is ITypeParameterSymbol typeParameter)
			{
				return typeParameter.GetAllConstraintTypes(includeInterfaces: false)
					.SelectMany(GetBaseTypesImplementation)
					.Distinct();
			}

			return type.GetBaseTypesImplementation();			
		}

		public static bool InheritsFrom(this ITypeSymbol type, ITypeSymbol baseType, bool includeInterfaces = false)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (baseType == null) throw new ArgumentNullException(nameof(baseType));

			IEnumerable<ITypeSymbol> baseTypes = type.GetBaseTypes();

			if (includeInterfaces)
			{
				baseTypes = baseTypes.Concat(type.AllInterfaces);
			}
			
			return baseTypes.Any(t => t.Equals(baseType));
		}

		private static IEnumerable<INamedTypeSymbol> GetBaseTypesImplementation(this ITypeSymbol typeToUse)
		{
			var current = typeToUse.BaseType;

			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
		}

		/// <summary>
		/// Gets all constraint types for the given <paramref name="typeParameterSymbol"/>.
		/// </summary>
		/// <param name="typeParameterSymbol">The typeParameterSymbol to act on.</param>
		/// <param name="includeInterfaces">(Optional) True to include, false to exclude the interfaces.</param>
		/// <returns/>
		public static IEnumerable<ITypeSymbol> GetAllConstraintTypes(this ITypeParameterSymbol typeParameterSymbol, bool includeInterfaces = true)
		{
			if (typeParameterSymbol == null) throw new ArgumentNullException(nameof(typeParameterSymbol));

			var constraintTypes = includeInterfaces
				? GetAllConstraintTypesImplementation(typeParameterSymbol)
				: GetAllConstraintTypesImplementation(typeParameterSymbol)
					.Where(type => type.TypeKind != TypeKind.Interface);

			return constraintTypes.Distinct();

			//---------------------------------Local Functions--------------------------------------------------------
			IEnumerable<ITypeSymbol> GetAllConstraintTypesImplementation(ITypeParameterSymbol typeParameter, int recursionLevel = 0)
			{
				const int maxRecursionLevel = 40;

				if (recursionLevel > maxRecursionLevel || typeParameter.ConstraintTypes.Length == 0)
					yield break;

				foreach (ITypeSymbol constraintType in typeParameter.ConstraintTypes)
				{
					if (constraintType is ITypeParameterSymbol constraintTypeParameter)
					{
						var nextOrderTypeParams = GetAllConstraintTypesImplementation(constraintTypeParameter, recursionLevel + 1);

						foreach (ITypeSymbol type in nextOrderTypeParams)
						{
							yield return type;
						}
					}
					else
					{
						yield return constraintType;
					}
				}
			}
		}
	}
}
