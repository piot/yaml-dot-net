using System;

namespace yaml {
	public static class YamlSerializer {
		public static string Serialize(Object o) {
			var writer = new YamlWriter();
			return writer.Write(o);
		}
	}
}

