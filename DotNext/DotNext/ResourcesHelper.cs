using Microsoft.CodeAnalysis;

namespace DotNext
{
	internal static class ResourcesHelper
	{
		public static LocalizableString GetLocalized(this string resourceName)
		{
			return new LocalizableResourceString(resourceName, Resources.ResourceManager, typeof(Resources));
		}
	}
}
