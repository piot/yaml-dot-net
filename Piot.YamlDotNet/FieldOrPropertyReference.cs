/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Piot.Yaml
{
	public class FieldOrPropertyReference : IFieldOrPropertyReference
	{
		private readonly PropertyInfo propertyInfo;
		private readonly FieldInfo fieldInfo;
		private readonly Type fieldOrPropertyType;
		private readonly string debugName;

		public Type FieldOrPropertyType => propertyInfo != null ? propertyInfo.PropertyType :
			fieldInfo != null ? fieldInfo.FieldType : throw new Exception($"internal error");


		public FieldOrPropertyReference(PropertyInfo propertyInfo, FieldInfo fieldInfo, object targetObject,
			string debugName)
		{
			if(propertyInfo != null)
			{
				fieldOrPropertyType = propertyInfo.PropertyType;
				this.propertyInfo = propertyInfo;
			}
			else if(fieldInfo != null)
			{
				fieldOrPropertyType = fieldInfo.FieldType;
				this.fieldInfo = fieldInfo;
			}
			else
			{
				throw new ArgumentException($"Piot.Yaml: you must provide either a propertyInfo or fieldInfo");
			}

			ObjectThatHoldsPropertyOrField = targetObject;
			this.debugName = debugName;
		}

		public override string ToString()
		{
			return $"[propOrField {FieldOrPropertyType.FullName} {debugName} ]";
		}

		static object GetEnumTypeFromValue(Type enumType, string enumValue)
		{
			foreach (var value in Enum.GetValues(enumType))
			{
				var enumFieldInfo = enumType.GetField(value.ToString());
				var allCustomAttributes =
					(YamlPropertyAttribute[])enumFieldInfo.GetCustomAttributes(typeof(YamlPropertyAttribute),
						false);
				if(allCustomAttributes.Length <= 0) continue;
				if(allCustomAttributes[0].Description == enumValue)
				{
					return value;
				}
			}

			return null;
		}

		static string GetReflectionEnumNameFromCustom(Type enumType, string enumValue)
		{
			foreach (var value in Enum.GetValues(enumType))
			{
				var enumFieldInfo = enumType.GetField(value.ToString());
				var allCustomAttributes =
					(YamlPropertyAttribute[])enumFieldInfo.GetCustomAttributes(typeof(YamlPropertyAttribute),
						false);
				if(allCustomAttributes.Length <= 0) continue;
				if(allCustomAttributes[0].Description == enumValue)
				{
					return value.ToString();
				}
			}

			return enumValue;
		}

		static object ParseEnum(Type enumType, string name)
		{
			object enumValue;
			try
			{
				enumValue = Enum.Parse(enumType, name);
			}
			catch (ArgumentException e)
			{
				// try to find it using a value
				throw new ArgumentException(
					$"PiotYaml: Enum value '{name} was not found in enum of type {enumType} {name} {e}");
			}

			return enumValue;
		}

		void SetValueToEnum(string enumValueStringRaw)
		{
			var enumValueString = enumValueStringRaw.Replace("|", ", ");
			var enumValues = enumValueString.Split(',');
			string enumStringToSet;
			if(enumValues.Length == 0)
			{
				enumStringToSet = GetReflectionEnumNameFromCustom(fieldOrPropertyType, enumValueString.Trim());
			}
			else
			{
				var translated = new string[enumValues.Length];

				var i = 0;
				foreach (var enumValueWithSpaces in enumValues)
				{
					var enumValue = enumValueWithSpaces.Trim();
					translated[i++] = GetReflectionEnumNameFromCustom(fieldOrPropertyType, enumValue);
				}

				enumStringToSet = string.Join(", ", translated.ToArray());
			}

			var enumObject = ParseEnum(fieldOrPropertyType, enumStringToSet);
			SetValueInternal(enumObject);
		}

		void SetValueToEnum(object o)
		{
			SetValueInternal(o);
		}

		public bool SetValueFromString(string v)
		{
			if(fieldOrPropertyType.IsEnum)
			{
				SetValueToEnum(v);
				return false;
			}

			SetValue(v);

			return false;
		}


		public void SetValue(object v)
		{
			if(fieldOrPropertyType.IsEnum)
			{
				SetValueToEnum(v);
				return;
			}

			if(fieldOrPropertyType.IsArray)
			{
				SetValueInternal(v);
				return;
			}

			object convertedValue;
			try
			{
				convertedValue = Convert.ChangeType(v, fieldOrPropertyType,
					CultureInfo.InvariantCulture);
			}
			catch (FormatException e)
			{
				throw new FormatException(
					$"PiotYaml: Couldn't format {fieldOrPropertyType} value: {v} because {e}");
			}

			SetValueInternal(convertedValue);
		}

		void SetValueInternal(object convertedValue)
		{
			if(fieldInfo != null)
			{
				fieldInfo.SetValue(ObjectThatHoldsPropertyOrField, convertedValue);
			}
			else if(propertyInfo != null)
			{
				propertyInfo.SetValue(ObjectThatHoldsPropertyOrField, convertedValue, null);
			}
			else
			{
				throw new Exception("Must have either valid PropertyInfo or FieldInfo");
			}
		}

		public object ObjectThatHoldsPropertyOrField { get; }
	}
}