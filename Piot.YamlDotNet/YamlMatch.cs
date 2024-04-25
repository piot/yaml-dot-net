/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

namespace Piot.Yaml
{
	internal struct YamlMatch
	{
		public string groupName;
		public string value;

		public override string ToString()
		{
			return $"[match {groupName} ({value})]";
		}
	}
}