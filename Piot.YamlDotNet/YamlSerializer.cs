/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

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