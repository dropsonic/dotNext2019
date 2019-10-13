// ReSharper disable UnusedMember.Global
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace DotNext.IntelliSense
{
	// Маркерный атрибут для встраивания в MEF
	[ExportCompletionProvider(nameof(DapperCompletionProvider), LanguageNames.CSharp)]
	public class DapperCompletionProvider : CompletionProvider
	{
		// Даём понять системе, нужно ли использовать наш completion provider
		public override bool ShouldTriggerCompletion(SourceText text, int position, CompletionTrigger trigger, OptionSet options)
		{
			return trigger.Kind == CompletionTriggerKind.Invoke || trigger.Kind == CompletionTriggerKind.Insertion;
		}

		public override async Task ProvideCompletionsAsync(CompletionContext context)
		{
			var cancellationToken = context.CancellationToken;
			// Получаем семантическую модель (она понадобится для работы с типами и методами)
			var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			// Находим синтаксическую ноду, внутри который был вызван IntelliSense
			var syntaxRoot = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var node = syntaxRoot.FindNode(context.CompletionListSpan);

			// Проверяем, что мы внутри строкового литерала, который к тому же является аргументом вызова метода
			if (!(node is ArgumentSyntax argNode) || !(argNode.Expression is LiteralExpressionSyntax literalNode) ||
			    !(argNode.Parent.Parent is InvocationExpressionSyntax invNode))
				return;

			// Получаем семантическую информацию о вызываемом методе
			var symbolInfo = semanticModel.GetSymbolInfo(invNode, cancellationToken);
			var methodSymbol = (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;
			if (methodSymbol == null)
				return;

			// Проверяем, что это действительно метод Dapper
			var sqlMapperSymbol = semanticModel.Compilation.GetTypeByMetadataName("Dapper.SqlMapper");
			if (sqlMapperSymbol == null || !methodSymbol.ContainingType.Equals(sqlMapperSymbol) ||
			    methodSymbol.Name != "Query" || !methodSymbol.IsGenericMethod)
				return;

			// Даём системе понять, что мы хотим показывать только наши completion items, и никакие другие
			// (не использовать другие completion provider'ы, кроме текущего)
			context.IsExclusive = true;

			// Получаем строку от начала строкового литерала до курсора ввода
			string text = literalNode.Token.ValueText;
			int selectedLength = context.Position - literalNode.Token.SpanStart - 1;
			if (selectedLength < text.Length)
			text = text.Substring(0, selectedLength);

			// Собираем информацию о всех свойствах DTO-классов, используемых в запросе
			var properties = methodSymbol.TypeArguments.SelectMany(t => t
				.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility.HasFlag(Accessibility.Public) &&
				            p.GetMethod != null)).ToArray();

			// Определяем, в какой части SQL-запроса мы сейчас находимся, и что же нужно показывать пользователю
			if (String.IsNullOrEmpty(text))
			{
				// Добавляем новый пункт в IntelliSense...
				context.AddItem(CompletionItem.Create("SELECT")
					// ...показывая его, как ключевое слово языка (соответствующая иконка)
					.AddTag(WellKnownTags.Keyword));
			}
			else
			{
				if (text.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) 
				    && !text.EndsWith(" FROM ", StringComparison.OrdinalIgnoreCase))
				{
					if (text.Equals("SELECT ", StringComparison.OrdinalIgnoreCase))
					{
						context.AddItem(CompletionItem.Create("DISTINCT", sortText: "K DISTINCT")
							.AddTag(WellKnownTags.Keyword));
						context.AddItem(CompletionItem.Create("TOP", sortText: "K TOP")
							.AddTag(WellKnownTags.Keyword));
					}

					if (!text.EndsWith(" TOP ", StringComparison.OrdinalIgnoreCase))
					{
						context.AddItem(CompletionItem.Create("*", sortText: "K *")
							.AddTag(WellKnownTags.Keyword));

						bool singleTable = methodSymbol.TypeArguments.Length == 1;

						foreach (var property in properties)
						{
							string propText = singleTable ? property.Name : $"{property.ContainingType.Name}.{property.Name}";
							context.AddItem(CompletionItem.Create(propText, sortText: $"P {propText}")
								.AddTag(WellKnownTags.Property)
								// Добавляем XML-комментарий от свойства DTO-класса как метаинформацию
								.AddProperty("comment", property.GetDocumentationCommentXml()));
						}
					}

					if (text.EndsWith(" * ", StringComparison.OrdinalIgnoreCase) ||
					    properties.Any(p => text.EndsWith(p.Name + " ")))
					{
						context.AddItem(CompletionItem.Create("FROM", sortText: "K FROM")
							.AddTag(WellKnownTags.Keyword));
					}
				}
				else if (text.EndsWith(" FROM ", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var typePar in methodSymbol.TypeArguments)
					{
						context.AddItem(CompletionItem.Create(typePar.Name)
							.AddTag(WellKnownTags.Class)
							// Добавляем XML-комментарий от DTO-класса как метаинформацию
							.AddProperty("comment", typePar.GetDocumentationCommentXml()));
					}
				}
			}
		}

		// Метод, определяющий, какой текст подсказки (hint) показывать для пункта IntelliSense
		public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
		{
			// Добываем ранее записанный в метаинформацию XML-комментарий
			if (item.Properties.TryGetValue("comment", out string comment) && !String.IsNullOrWhiteSpace(comment))
			{
				// Достаём содержимое тега <summary> и возвращаем его как подсказку
				string summary = XDocument.Parse(comment).Descendants("summary").FirstOrDefault()?.Value;
				if (!String.IsNullOrWhiteSpace(summary))
					return Task.FromResult(CompletionDescription.FromText(summary));
			}

			return base.GetDescriptionAsync(document, item, cancellationToken);
		}

		// Точка расширения для замены подставляемого при completion текста
		public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
		{
			// Если это свойство DTO-класса, то экранируем его квадратными скобками (escaping)
			if (item.Tags.Contains(WellKnownTags.Property))
			{
				string[] splitted = item.DisplayText.Split('.');
				string newText = splitted.Length > 1 ? $"[{splitted[0]}].[{splitted[1]}]" : $"[{splitted[0]}]";
				return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, newText)));
			}

			return base.GetChangeAsync(document, item, commitKey, cancellationToken);
		}
	}
}
