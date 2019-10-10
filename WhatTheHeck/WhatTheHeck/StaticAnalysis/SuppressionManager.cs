using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WhatTheHeck.StaticAnalysis
{
	internal class SuppressionManager
	{
		private const string SuppressionFileExtension = ".suppression";
		private static readonly Regex CommentRegex = new Regex(@"Rehecker disable once (DN\d{4})", RegexOptions.Compiled);

		private readonly ImmutableHashSet<string> _suppressions;

		public SuppressionManager(AnalyzerOptions analyzerOptions)
		{
			var set = ImmutableHashSet<string>.Empty.ToBuilder();

			// Читаем все строки из всех suppression-файлов
			foreach (var file in analyzerOptions.AdditionalFiles
				.Where(f => !String.IsNullOrEmpty(f.Path) 
				            && f.Path.EndsWith(SuppressionFileExtension, StringComparison.OrdinalIgnoreCase)))
			{
				foreach (var line in file.GetText().Lines)
				{
					set.Add(line.Text.ToString());
				}
			}

			_suppressions = set.ToImmutable();
		}

		public bool IsSuppressed(DiagnosticDescriptor descriptor, SyntaxTrivia trivia)
		{
			// Проверяем наличие в suppression файле
			if (_suppressions.Contains(trivia.ToFullString()))
				return true;

			// Если там не нашлось, ищем suppression-комментарий
			foreach (var t in trivia.Token.GetAllTrivia())
			{
				if (t.IsEquivalentTo(trivia))
					break;

				if (t.IsKind(SyntaxKind.SingleLineCommentTrivia))
				{
					var match = CommentRegex.Match(t.ToFullString());
					
					if (match.Success && match.Groups[1].Value == descriptor.Id)
						return true;
				}
			}

			return false;
		}
	}
}
