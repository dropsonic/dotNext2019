using System;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;

namespace WhatTheHeck.StaticAnalysis
{
	[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
	public sealed class WhatTheHeckCodeFixProvider : CodeFixProvider
	{
		private const string FWordReplacement = "heck";

		public override ImmutableArray<string> FixableDiagnosticIds { get; } = 
			ImmutableArray.Create(Descriptors.DN1000_WhatTheHeckComment.Id);
		
		// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			string title = nameof(Resources.DN1000Fix).GetLocalized().ToString();

			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			
			foreach (var diagnostic in context.Diagnostics)
			{
				var comment = root.FindTrivia(diagnostic.Location.SourceSpan.Start);
				
				if (comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || comment.IsKind(SyntaxKind.MultiLineCommentTrivia))
				{
					context.RegisterCodeFix(CodeAction.Create(title, ct =>
					{
						if (FWordReplacement.Length != WhatTheHeckAnalyzer.FWord.Length)
							throw new NotSupportedException();

						var newComment = SyntaxFactory.Comment(ReplaceFWord(comment.ToFullString()));

						return Task.FromResult(
							context.Document.WithSyntaxRoot(
								root.ReplaceTrivia(comment, newComment)));
					}), diagnostic);
				}
			}
			
		}

		private static string ReplaceFWord(string text)
		{
			string word = WhatTheHeckAnalyzer.FWord.PadLeft(1);
			char[] newText = text.ToCharArray();
			text = text.PadLeft(1);

			int index;
			while ((index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				for (int i = index + 1, j = 0; i < index + WhatTheHeckAnalyzer.FWord.Length; i++, j++)
				{
					if (char.IsUpper(text[i]))
						newText[i - 1] = char.ToUpperInvariant(FWordReplacement[j]);
					else if (char.IsLower(text[i]))
						newText[i - 1] = char.ToLowerInvariant(FWordReplacement[j]);
				}

				text = new string(newText);
			}

			return text;
		}
	}
}
