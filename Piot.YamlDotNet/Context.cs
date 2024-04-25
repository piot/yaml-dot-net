/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

namespace Piot.Yaml
{
	struct Context
	{
		public int indent;
		public IFieldOrPropertyReference savedPropertyReference;
		public ITargetList savedList;
		public ITargetContainer targetContainer;

		public override string ToString()
		{
			return $"[context {savedPropertyReference} <- {savedList} <= {targetContainer}]";
		}
	}
}