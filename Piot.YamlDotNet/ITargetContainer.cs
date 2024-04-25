/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

namespace Piot.Yaml
{

	public interface ITargetContainer : IContainerObject
	{
		// true means that the list was completed by this string
		public bool SetContainerFromString(string value);

		public bool SupportsSubObject { get; }

		public IFieldOrPropertyReference GetReferenceToPropertyFromName(object propertyName);
	}
}