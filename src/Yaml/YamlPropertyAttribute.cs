using System;

namespace Piot.Yaml
{
	public class YamlPropertyAttribute : Attribute
	{
		public string Description { get; }

		public YamlPropertyAttribute(string description)
		{
			Description = description;
		}
	}
}