using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
		private struct SuppressionInfo
		{
			public SuppressionInfo(Suppression suppression, 
				Func<bool> hasSuppressionComment, 
				Func<Document, Task<Document>> addSuppressionComment)
			{
				Suppression = suppression;
				HasSuppressionComment = hasSuppressionComment;
				AddSuppressionCommentAsync = addSuppressionComment;
			}

			public Suppression Suppression { get; }
			public Func<bool> HasSuppressionComment { get; }
			public Func<Document, Task<Document>> AddSuppressionCommentAsync { get; }
		}

		public const string SuppressionFileExtension = ".suppression";
		public const string SuppressionCommentFormat = "// Rehecker disable once {0}";

		private static readonly ConditionalWeakTable<AnalyzerOptions, SuppressionManager> _instances = 
			new ConditionalWeakTable<AnalyzerOptions, SuppressionManager>();
		private static readonly XmlSerializer _serializer = 
			new XmlSerializer(typeof(Suppressions), new[] { typeof(Suppression) });
		private static readonly Regex _commentRegex = 
			new Regex(@"Rehecker disable once (DN\d{4})", RegexOptions.Compiled);

		private readonly ImmutableHashSet<Suppression> _suppressions = 
			ImmutableHashSet<Suppression>.Empty;

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

		// Используется для code fix'а
		private SuppressionManager(ImmutableHashSet<Suppression> suppressions)
		{
			_suppressions = suppressions;
		}

		// Возвращаем существующий SuppressionManager из кэша или создаём новый
		public static SuppressionManager Get(AnalyzerOptions options)
		{
			if (!_instances.TryGetValue(options, out var instance))
				instance = _instances.GetValue(options, o => new SuppressionManager(o));

			return instance;
		}

		public bool IsSuppressed(Diagnostic diagnostic)
		{
			var info = GetSuppressionInfo(diagnostic);

			// Сначала проверяем наличие исключения в suppression-файле
			if (_suppressions.Contains(info.Suppression))
				return true;

			// Если там не нашлось, то ищем suppression-комментарий
			return info.HasSuppressionComment();
		}
		
		private SuppressionInfo GetSuppressionInfo(Diagnostic diagnostic)
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

					return new SuppressionInfo(
						new Suppression(diagnostic.Id, context, target), 
						() => HasSuppressionComment(diagnostic.Id, trivia),
						doc => AddSuppressionCommentAsync(doc, diagnostic.Id, trivia));
				}
			}

			// Тут должен быть код, который обрабатывает другие типы диагностик
			return new SuppressionInfo(new Suppression(diagnostic.Id, "", ""), () => false, Task.FromResult);
		}

		private static bool HasSuppressionComment(string diagnosticId, SyntaxTrivia trivia)
		{
			// Проверяем всю trivia перед той trivia,
			// на которой висит диагностика
			foreach (var t in trivia.Token.GetAllTrivia())
			{
				if (t.IsEquivalentTo(trivia))
					break;

				// Если это однострочный комментарий...
				if (t.IsKind(SyntaxKind.SingleLineCommentTrivia))
				{
					// ...то проверяем, что это suppression-комментарий...
					var match = _commentRegex.Match(t.ToFullString());
					// ...и что он относится именно к текущей диагностике
					if (match.Success && match.Groups[1].Value == diagnosticId)
						return true;
				}
			}

			return false;
		}

		private static async Task<Document> AddSuppressionCommentAsync(
			Document document,
			string diagnosticId, 
			SyntaxTrivia trivia)
		{
			// Находим токен, к которому привязан комментарий
			var token = trivia.Token;

			// Создаём suppression-комментарий
			var suppressionTrivia = SyntaxFactory.Comment(
				String.Format(SuppressionCommentFormat, diagnosticId));

			// Вставляем его перед оригинальным комментарием с F-word
			SyntaxTrivia whitespaceTrivia = SyntaxFactory.Whitespace("");

			foreach (var t in token.LeadingTrivia)
			{
				if (t.IsKind(SyntaxKind.WhitespaceTrivia))
					whitespaceTrivia = t;
				if (t.Equals(trivia))
					break;
			}

			var newToken = token
				.WithLeadingTrivia(token.LeadingTrivia
					.Insert(0, SyntaxFactory.EndOfLine(Environment.NewLine))
					.Insert(0, suppressionTrivia)
					.Insert(0, whitespaceTrivia));
				
			// Возвращаем новый документ
			var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
			var newRoot = root.ReplaceToken(token, newToken);
			return document.WithSyntaxRoot(newRoot);
		}

		// Используется в code fix'е
		internal SuppressionManager AddSuppression(Diagnostic diagnostic)
		{
			var info = GetSuppressionInfo(diagnostic);
			return new SuppressionManager(_suppressions.Add(info.Suppression));
		}

		// Используется в code fix'е
		internal Task<Document> AddSuppressionCommentAsync(Document document, Diagnostic diagnostic)
		{
			var info = GetSuppressionInfo(diagnostic);
			return info.AddSuppressionCommentAsync(document);
		}

		// Используется в code fix'е
		internal SourceText ToText()
		{
			using (var writer = new StringWriter())
			{
				_serializer.Serialize(writer, new Suppressions() { Items = _suppressions.ToArray() });
				return SourceText.From(writer.ToString());
			}
		}

		
		// Читаем все suppressions из файла (XML)
		private static ImmutableHashSet<Suppression> ReadSuppressions(SourceText sourceText)
		{
			var setBuilder = ImmutableHashSet<Suppression>.Empty.ToBuilder();

			string text = sourceText.ToString();
				
			if (!String.IsNullOrWhiteSpace(text))
			{
				using (var reader = new StringReader(text))
				{
					try
					{
						var suppressions = (Suppressions) _serializer.Deserialize(reader);

						if (suppressions.Items != null)
						{
							foreach (var item in suppressions.Items)
								setBuilder.Add(item);
						}
					}
					catch { /* log the exception */ }
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
				case IdentifierNameSyntax identifierName:
					return identifierName.Identifier.ToString();
				default:
					return null;
			}
		}
	}
}
