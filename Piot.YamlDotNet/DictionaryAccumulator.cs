/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Piot.Yaml
{
	public class DictionaryAccumulator : ITargetContainer
	{
		private IDictionary items;
		private Type valueType;
		private Type keyType;
		private string debugName;

		public DictionaryAccumulator(Type keyType, Type valueType, string debugName)
		{
			this.keyType = keyType;
			this.valueType = valueType;
			this.debugName = debugName;

			var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
			// Convert the List<object> to an array of the determined element type
			items = (IDictionary)Activator.CreateInstance(dictionaryType);
		}

		public override string ToString()
		{
			return $"[dict {keyType} {valueType} {items.Count}]";
		}

		public Type FieldOrPropertyType => valueType;


		public bool SetContainerFromString(string value)
		{
			if(value.Trim() != "{}")
			{
				throw new Exception($"not a valid string for an dictionary '{value}'");
			}

			return true;
		}

		public bool SupportsSubObject => false;

		public IFieldOrPropertyReference GetReferenceToPropertyFromName(object propertyName)
		{
			object convertedValue;
			try
			{
				convertedValue = Convert.ChangeType(propertyName, keyType,
					CultureInfo.InvariantCulture);
			}
			catch (FormatException e)
			{
				throw new FormatException(
					$"PiotYaml: Couldn't format {keyType} value: {propertyName} because {e}");
			}

			return new DictionaryReferenceItem(items, convertedValue, valueType);
		}

		public object ContainerObject => items;
	}
}