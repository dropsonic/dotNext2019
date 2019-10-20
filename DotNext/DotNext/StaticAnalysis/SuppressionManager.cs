using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DotNext.StaticAnalysis
{
	internal class SuppressionManager
	{
		public const string SuppressionFileExtension = ".suppression";
		public const string SuppressionCommentFormat = "// Rehecker disable once {0}";
		public const string PropertyKey = "suppression";

		private static readonly ConditionalWeakTable<AnalyzerOptions, SuppressionManager> _instances = 
			new ConditionalWeakTable<AnalyzerOptions, SuppressionManager>();
		private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(Suppressions), new[] { typeof(Suppression) });

		private static readonly Regex CommentRegex = new Regex(@"Rehecker disable once (DN\d{4})", RegexOptions.Compiled);

		private readonly ImmutableHashSet<Suppression> _suppressions = ImmutableHashSet<Suppression>.Empty;

		private SuppressionManager(AnalyzerOptions analyzerOptions)
		{
			// Находим suppression-файл
			var suppressionFile = analyzerOptions.AdditionalFiles
				.FirstOrDefault(f => !String.IsNullOrEmpty(f.Path)
				                     && f.Path.EndsWith(SuppressionFileExtension, StringComparison.OrdinalIgnoreCase));

			// Читаем suppression'ы из файла
			if (suppressionFile != null)
				_suppressions = ReadSuppressions(suppressionFile.GetText());
		}

		public bool IsSuppressed(Diagnostic diagnostic)
		{
			if (diagnostic.Location.SourceTree != null)
			{
				var root = diagnostic.Location.SourceTree.GetRoot();
				var span = diagnostic.Location.SourceSpan;
				var trivia = root.FindTrivia(span.Start);
				
				// Диагностика была добавлена непосредственно на trivia
				if (trivia.FullSpan.Equals(span))
				{
					var syntaxNode = root.FindNode(span);
					string context = GetSyntaxNodeText(syntaxNode) ?? String.Empty;
					string target = trivia.ToString();

					// Проверяем наличие в suppression файле
					if (_suppressions.Contains(new Suppression(diagnostic.Id, context, target)))
						return true;

					// Если там не нашлось, ищем suppression-комментарий
					foreach (var t in trivia.Token.GetAllTrivia())
					{
						if (t.IsEquivalentTo(trivia))
							break;

						if (t.IsKind(SyntaxKind.SingleLineCommentTrivia))
						{
							var match = CommentRegex.Match(t.ToFullString());
					
							if (match.Success && match.Groups[1].Value == diagnostic.Id)
								return true;
						}
					}
				}
				else
				{
					// Тут должен быть код, который обрабатывает другие типы диагностик
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

		internal SourceText ToText()
		{
			using (var writer = new StringWriter())
			{
				_serializer.Serialize(writer, new Suppressions() { Items = _suppressions.ToArray() });
				return SourceText.From(writer.ToString());
			}
		}


		private Suppression GetSuppression(Diagnostic diagnostic)
		{
			if (diagnostic.Location.SourceTree != null)
			{
				var root = diagnostic.Location.SourceTree.GetRoot();
				var span = diagnostic.Location.SourceSpan;
				var trivia = root.FindTrivia(span.Start);
				
				// Диагностика была добавлена непосредственно на trivia
				if (trivia.FullSpan.Equals(span))
				{
					var syntaxNode = root.FindNode(span);
					string context = GetSyntaxNodeText(syntaxNode) ?? String.Empty;
					string target = trivia.ToString();

					return new Suppression(diagnostic.Id, context, target);
				}
			}

			// Тут должен быть код, который обрабатывает другие типы диагностик
			return new Suppression(diagnostic.Id, "", "");
		}

		private static ImmutableHashSet<Suppression> ReadSuppressions(SourceText sourceText)
		{
			var setBuilder = ImmutableHashSet<Suppression>.Empty.ToBuilder();

			string text = sourceText.ToString();
				
			if (!String.IsNullOrWhiteSpace(text))
			{
				using (var reader = new StringReader(text))
				{
					var suppressions = (Suppressions) _serializer.Deserialize(reader);

					if (suppressions.Items != null)
					{
						foreach (var item in suppressions.Items)
							setBuilder.Add(item);
					}
				}
			}

			return setBuilder.ToImmutable();
		}

		private static string GetSyntaxNodeText(SyntaxNode syntaxNode)
		{
			switch (syntaxNode)
			{
				case FieldDeclarationSyntax fieldDeclaration:
					return fieldDeclaration.Declaration.ToString();
				case PropertyDeclarationSyntax propertyDeclaration:
					return propertyDeclaration.Identifier.ToString();
				case ClassDeclarationSyntax classDeclaration:
					return classDeclaration.Identifier.ToString();
				case MethodDeclarationSyntax methodDeclaration:
					return methodDeclaration.Identifier.ToString();
				case ConstructorDeclarationSyntax constructorDeclaration:
					return constructorDeclaration.Identifier.ToString();
				default:
					return null;
			}
		}
	}
}
