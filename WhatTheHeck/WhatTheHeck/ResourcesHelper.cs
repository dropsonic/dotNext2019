using Microsoft.CodeAnalysis;

namespace WhatTheHeck
{
	internal static class ResourcesHelper
	{
		public static LocalizableString GetLocalized(this string resourceName)
		{
			return new LocalizableResourceString(resourceName, Resources.ResourceManager, typeof(Resources));
		}
	}
}
