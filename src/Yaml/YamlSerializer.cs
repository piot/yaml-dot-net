﻿using System;

namespace Piot.Yaml
{
	public static class YamlSerializer
	{
		public static string Serialize(Object o)
		{
			var writer = new YamlWriter();
			return writer.Write(o);
		}
	}
}