/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

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