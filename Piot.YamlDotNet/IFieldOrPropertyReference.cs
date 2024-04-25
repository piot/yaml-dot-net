/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Yaml
{
	public interface IFieldOrPropertyReference
	{
		public Type FieldOrPropertyType { get; }
		public void SetValue(object boxedValue);
		public bool SetValueFromString(string value);
	}
}