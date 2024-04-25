/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Yaml
{
	public interface ITargetList : IContainerObject
	{
		public void Add(object value);

		// true means that the list was completed by this string
		public bool AddUsingString(string value);

		public Type ItemType { get; }
	}
}