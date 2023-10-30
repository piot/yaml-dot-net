/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

﻿namespace Piot.Yaml
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