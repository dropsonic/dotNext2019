using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNext.StaticAnalysis
{
	internal class SuppressionManager
	{
		public const string SuppressionFileExtension = ".suppression";
		public const string SuppressionCommentFormat = "// Rehecker disable once {0}";
		public const string PropertyKey = "suppression";

		private static readonly ConditionalWeakTable<AnalyzerOptions, SuppressionManager> _instances = 
			new ConditionalWeakTable<AnalyzerOptions, SuppressionManager>();

		private static readonly Regex CommentRegex = new Regex(@"Rehecker disable once (DN\d{4})", RegexOptions.Compiled);

		private readonly ImmutableHashSet<string> _suppressions;

		private SuppressionManager(AnalyzerOptions analyzerOptions)
		{
			var set = ImmutableHashSet<string>.Empty.ToBuilder();

			// Читаем все строки из всех suppression-файлов
			foreach (var file in analyzerOptions.AdditionalFiles
				.Where(f => !String.IsNullOrEmpty(f.Path) 
				            && f.Path.EndsWith(SuppressionFileExtension, StringComparison.OrdinalIgnoreCase)))
			{
				foreach (var line in file.GetText().Lines)
				{
					set.Add(line.Text.ToString(line.Span));
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

		// Возвращаем существующий SuppressionManager из кэша или создаём новый
		public static SuppressionManager Get(AnalyzerOptions options)
		{
			if (!_instances.TryGetValue(options, out var instance))
				instance = _instances.GetValue(options, o => new SuppressionManager(o));

			return instance;
		}
	}
}
