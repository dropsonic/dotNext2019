﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNext.StaticAnalysis
{
	/// <summary>
	/// Syntax walker that follows method invocations, property getters, etc.,
	/// and analyzes corresponding symbols in a recursive manner.
	/// </summary>
	/// <remarks>
	/// Please note that it doesn't analyze symbols which don't have any source code available.
	/// </remarks>
	/// <example>
	///	<code title="Example">
	/// string descr = SomeHelper.GetDescription(); // this code is being analyzed
	/// ...
	/// // In some other file or even in a different assembly
	/// public static class SomeHelper
	/// {
	///		public static void GetDescription()
	///		{
	///			var graph = new PXGraph(); // this code will be analyzed too
	///		}
	/// }
	///	</code>
	/// </example>
	// ReSharper disable once InheritdocConsiderUsage
	public abstract class NestedInvocationWalker : CSharpSyntaxWalker
	{
		private const int MaxDepth = 100; // для борьбы с circular dependencies
		private readonly Compilation _compilation;
		private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModels = new Dictionary<SyntaxTree, SemanticModel>();
		private readonly ISet<(SyntaxNode, DiagnosticDescriptor)> _reportedDiagnostics = new HashSet<(SyntaxNode, DiagnosticDescriptor)>();
		private readonly Stack<SyntaxNode> _nodesStack = new Stack<SyntaxNode>();
		private readonly HashSet<IMethodSymbol> _methodsInStack = new HashSet<IMethodSymbol>();

        protected CancellationToken CancellationToken { get; }
		
        // Синтаксическая нода в оригинальном дереве, с которого начался анализ.
        // Обычно это нода, на которой и нужно показывать диагностику.
        protected SyntaxNode OriginalNode { get; private set; }

		
		protected NestedInvocationWalker(Compilation compilation, CancellationToken cancellationToken)
		{
			_compilation = compilation;
            CancellationToken = cancellationToken;
		}

		protected void ThrowIfCancellationRequested() => CancellationToken.ThrowIfCancellationRequested();

		/// <summary>
		/// Returns a symbol for an invocation expression, or, 
		/// if the exact symbol cannot be found, returns the first candidate.
		/// </summary>
		protected virtual T GetSymbol<T>(ExpressionSyntax node)
			where T : class, ISymbol
		{
			var semanticModel = GetSemanticModel(node.SyntaxTree);

			if (semanticModel != null)
			{
				var symbolInfo = semanticModel.GetSymbolInfo(node, CancellationToken);

				if (symbolInfo.Symbol is T symbol)
				{
					return symbol;
				}

				if (!symbolInfo.CandidateSymbols.IsEmpty)
				{
					return symbolInfo.CandidateSymbols.OfType<T>().FirstOrDefault();
				}
			}

			return null;
		}

		protected virtual SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
		{
			if (!_compilation.ContainsSyntaxTree(syntaxTree))
				return null;

			if (_semanticModels.TryGetValue(syntaxTree, out var semanticModel))
				return semanticModel;

			semanticModel = _compilation.GetSemanticModel(syntaxTree);
			_semanticModels[syntaxTree] = semanticModel;
			return semanticModel;
		}

		/// <summary>
		/// Reports a diagnostic for the provided descriptor on the original syntax node from which recursive analysis started.
		/// This method must be used for all diagnostic reporting within the walker
		/// because it does diagnostic deduplication and determine the right syntax node to perform diagnostic reporting.
		/// </summary>
		/// <param name="reportDiagnostic">Action that reports a diagnostic in the current context (e.g., <code>SymbolAnalysisContext.ReportDiagnostic</code>)</param>
		/// <param name="diagnosticDescriptor">Diagnostic descriptor</param>
		/// <param name="node">Current syntax node that is being analyzed. Diagnostic will be reported on the original node.</param>
		/// <param name="messageArgs">Arguments to the message of the diagnostic</param>
		/// <remarks>This method takes a report diagnostic method as a parameter because it is different for each analyzer type 
		/// (<code>SymbolAnalysisContext.ReportDiagnostic</code>, <code>SyntaxNodeAnalysisContext.ReportDiagnostic</code>, etc.)</remarks>
		protected virtual void ReportDiagnostic(Action<Diagnostic> reportDiagnostic, DiagnosticDescriptor diagnosticDescriptor,
												SyntaxNode node, params object[] messageArgs)
		{
			var nodeToReport = OriginalNode ?? node;
			var diagnosticKey = (nodeToReport, diagnosticDescriptor);

			if (!_reportedDiagnostics.Contains(diagnosticKey))
			{
				var diagnostic = Diagnostic.Create(diagnosticDescriptor, nodeToReport.GetLocation(), messageArgs);
				reportDiagnostic(diagnostic);
				_reportedDiagnostics.Add(diagnosticKey);
			}
		}

		private void Push(SyntaxNode node, IMethodSymbol symbol)
		{
			if (_nodesStack.Count == 0)
				OriginalNode = node;

			_nodesStack.Push(node);
            _methodsInStack.Add(symbol);
		}

		private void Pop(IMethodSymbol symbol)
		{
			_nodesStack.Pop();
            _methodsInStack.Remove(symbol);

			if (_nodesStack.Count == 0)
				OriginalNode = null;
		}

		private bool RecursiveAnalysisEnabled()
		{
			return _nodesStack.Count <= MaxDepth;
		}

		#region Visit

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			ThrowIfCancellationRequested();

			if (RecursiveAnalysisEnabled() && node.Parent?.Kind() != SyntaxKind.ConditionalAccessExpression)
			{
				var methodSymbol = GetSymbol<IMethodSymbol>(node);
				VisitMethodSymbol(methodSymbol, node);
			}

			base.VisitInvocationExpression(node);
		}

		public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			ThrowIfCancellationRequested();

			if (RecursiveAnalysisEnabled() && node.Parent != null
				&& !node.Parent.IsKind(SyntaxKind.ObjectInitializerExpression)
			    && !(node.Parent is AssignmentExpressionSyntax))
			{
				var propertySymbol = GetSymbol<IPropertySymbol>(node);
				VisitMethodSymbol(propertySymbol?.GetMethod, node);
			}

			base.VisitMemberAccessExpression(node);
		}

		public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
		{
			ThrowIfCancellationRequested();

			if (RecursiveAnalysisEnabled())
			{
				var propertySymbol = GetSymbol<IPropertySymbol>(node.Left);
				VisitMethodSymbol(propertySymbol?.SetMethod, node);
			}

			base.VisitAssignmentExpression(node);
		}

		public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
		{
			ThrowIfCancellationRequested();

			if (RecursiveAnalysisEnabled())
			{
				var methodSymbol = GetSymbol<IMethodSymbol>(node);
				VisitMethodSymbol(methodSymbol, node);
			}

			base.VisitObjectCreationExpression(node);
		}

		public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
		{
			ThrowIfCancellationRequested();

			if (RecursiveAnalysisEnabled() && node.WhenNotNull != null)
			{
				var propertySymbol = GetSymbol<IPropertySymbol>(node.WhenNotNull);
				var methodSymbol = propertySymbol != null 
					? propertySymbol.GetMethod 
					: GetSymbol<IMethodSymbol>(node.WhenNotNull);

				VisitMethodSymbol(methodSymbol, node);
			}

			base.VisitConditionalAccessExpression(node);
		}

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
        }

		public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
		}

		private void VisitMethodSymbol(IMethodSymbol symbol, SyntaxNode originalNode)
		{
			if (symbol?.GetSyntax(CancellationToken) is CSharpSyntaxNode methodNode &&
                !IsMethodInStack(symbol))
			{
				Push(originalNode, symbol);
				methodNode.Accept(this);
				Pop(symbol);
			}
		}

        private bool IsMethodInStack(IMethodSymbol symbol)
        {
            return _methodsInStack.Contains(symbol);
        }

        #endregion
    }
}