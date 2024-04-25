/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Globalization;

namespace Piot.Yaml
{
	public class DictionaryReferenceItem : IFieldOrPropertyReference
	{
		private IDictionary dictionary;
		private object key;

		public DictionaryReferenceItem(IDictionary dictionary, object key, Type valueType)
		{
			this.dictionary = dictionary;

			this.key = key;

			FieldOrPropertyType = valueType;
		}

		public Type FieldOrPropertyType { get; }


		public void SetValue(object boxedValue)
		{
			object convertedValue;
			try
			{
				convertedValue = Convert.ChangeType(boxedValue, FieldOrPropertyType,
					CultureInfo.InvariantCulture);
			}
			catch (FormatException e)
			{
				throw new FormatException(
					$"PiotYaml: Couldn't format {FieldOrPropertyType} value: {boxedValue} because {e}");
			}

			dictionary.Add(key, convertedValue);
		}

		public bool SetValueFromString(string value)
		{
			throw new NotImplementedException();
		}

		public IFieldOrPropertyReference FindUsingPropertyName(string propertyName)
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return $"[dictRef {key}]";
		}

		public object ObjectThatHoldsPropertyOrField => dictionary;
	}
}