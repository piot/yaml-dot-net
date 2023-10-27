namespace Piot.Yaml
{
	public static class YamlDeserializer
	{
		public static T Deserialize<T>(string s)
		{
			var parser = new YamlParser();
			return parser.Parse<T>(s);
		}
	}
}