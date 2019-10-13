using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace DotNext.StaticAnalysis
{
	public enum Category
	{
		Default,
	}

	// Класс, содержащий экземпляры дескрипторов для всех диагностик
	public static class Descriptors
	{
		private static readonly ConcurrentDictionary<Category, string> _categoryMapping = new ConcurrentDictionary<Category, string>();

		private static DiagnosticDescriptor Rule(string id, LocalizableString title, Category category, DiagnosticSeverity defaultSeverity, 
			LocalizableString messageFormat = null, LocalizableString description = null)
		{
			messageFormat = messageFormat ?? title;
			string categoryName = _categoryMapping.GetOrAdd(category, c => c.ToString());
			return new DiagnosticDescriptor(id, title, messageFormat, categoryName, defaultSeverity,
				isEnabledByDefault: true, description: description);
		}

		public static DiagnosticDescriptor DN1000_WhatTheHeckComment { get; } = 
			Rule("DN1000", nameof(Resources.DN1000Title).GetLocalized(), Category.Default, DiagnosticSeverity.Warning);
	}
}
