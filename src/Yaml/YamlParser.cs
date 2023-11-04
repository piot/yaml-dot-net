/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Piot.Yaml
{
	internal class YamlParser
	{
		public interface IFieldOrPropertyTarget
		{
			public Type FieldOrPropertyType { get; }
			public void SetValue(object boxedValue);
			public void SetValueFromString(string value);

			public object ObjectThatHoldsPropertyOrField { get; }
		}

		public class FieldOrPropertyTarget : IFieldOrPropertyTarget
		{
			private readonly PropertyInfo propertyInfo;
			private readonly FieldInfo fieldInfo;
			private readonly Type fieldOrPropertyType;
			private readonly string debugName;

			public Type FieldOrPropertyType => propertyInfo != null ? propertyInfo.PropertyType :
				fieldInfo != null ? fieldInfo.FieldType : throw new Exception($"internal error");

			public FieldOrPropertyTarget(PropertyInfo propertyInfo, FieldInfo fieldInfo, object targetObject,
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

			void SetValueToEnum(string enumValueString)
			{
				object enumValue;
				try
				{
					enumValue = Enum.Parse(fieldOrPropertyType, enumValueString);
				}
				catch (ArgumentException e)
				{
					enumValue = GetEnumTypeFromValue(fieldOrPropertyType, enumValueString);
					if(enumValue == null)
					{
						// try to find it using a value
						throw new ArgumentException(
							$"PiotYaml: Enum value '{enumValueString} was not found in enum of type {fieldOrPropertyType} {debugName} {e}");
					}
				}

				SetValueInternal(enumValue);
			}

			void SetValueToEnum(object o)
			{
				SetValueInternal(o);
			}

			public void SetValueFromString(string v)
			{
				if(fieldOrPropertyType.IsEnum)
				{
					SetValueToEnum(v);
					return;
				}

				SetValue(v);
			}

			public void SetValue(object v)
			{
				if(fieldOrPropertyType.IsEnum)
				{
					SetValueToEnum(v);
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

		struct YamlMatch
		{
			public string groupName;
			public string value;
		}

		struct Context
		{
			public IFieldOrPropertyTarget savedPropertyTarget;
		}

		readonly Stack<Context> contexts = new();

		private IFieldOrPropertyTarget targetFieldOrProperty;
		private object targetObject;

		int currentIndent;
		private int lastDetectedIndent;

		List<YamlMatch> FindMatches(string testData)
		{
			var outList = new List<YamlMatch>();

			var variable = @"(?<variable>[a-zA-Z_$][a-zA-Z0-9_$]*\s*:)";
			var hyphen = @"(?<hyphen>^-\s*$)";
			var indent = @"(?<indent>\n+(?<indentspaces>\s{2})*)";
			var stringMatch = @"(?<string>.*)";
			var integerMatch = @"(?<integer>\s*[-+]?[1-9]\d*)";
			var hexMatch = @"(?<hex>\s*0[xX][0-9a-fA-F]*)";
			var floatMatch = @"(?<float>\s*[-+]?[0-9]*\.[0-9]+(?<exponent>[eE][-+]?[0-9]+)?)";
			var booleanMatch = @"(?<boolean>(true)|(false))";
			var commentMatch = @"(?<comment>\s*\#.+)";

			var expressions = new[]
			{
				variable,
				hyphen,
				hexMatch,
				floatMatch,
				indent,
				commentMatch,
				integerMatch,
				booleanMatch,
				stringMatch, // string must be last
			};
			var pattern = string.Join("|", expressions);

			var regExPattern = new Regex(pattern);
			var matches = regExPattern.Matches(testData);

			foreach (Match match in matches)
			{
				if(match.Success)
				{
					var i = 0;
					foreach (Group groupData in match.Groups)
					{
						if(i > 0 && groupData.Success)
						{
							var groupName = regExPattern.GroupNameFromNumber(i);
							var yamlMatch = new YamlMatch();
							yamlMatch.groupName = groupName;
							yamlMatch.value = match.Value;
							outList.Add(yamlMatch);
						}

						++i;
					}
				}
				else
				{
					throw new Exception($"NO MATCH:" + match.Value);
				}
			}

			return outList;
		}

		static IFieldOrPropertyTarget FindFieldOrProperty(object o, string propertyName)
		{
			var t = o.GetType();
			FieldInfo foundFieldInfo = null;
			var foundPropertyInfo = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			if(foundPropertyInfo == null)
			{
				foundFieldInfo = t.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
			}

			if(foundPropertyInfo != null || foundFieldInfo != null)
			{
				return new FieldOrPropertyTarget(foundPropertyInfo, foundFieldInfo, o, propertyName);
			}

			var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var field in fields)
			{
				var hasAttribute = Attribute.IsDefined(field, typeof(YamlPropertyAttribute));
				if(!hasAttribute) continue;
				var attribute =
					(YamlPropertyAttribute)Attribute.GetCustomAttribute(field, typeof(YamlPropertyAttribute));
				if(attribute.Description == propertyName)
				{
					return new FieldOrPropertyTarget(null, field, o, propertyName);
				}
			}

			var properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var property in properties)
			{
				var hasAttribute = Attribute.IsDefined(property, typeof(YamlPropertyAttribute));
				if(!hasAttribute) continue;
				var attribute =
					(YamlPropertyAttribute)Attribute.GetCustomAttribute(property,
						typeof(YamlPropertyAttribute));
				if(attribute.Description == propertyName)
				{
					return new FieldOrPropertyTarget(property, null, o, propertyName);
				}
			}

			throw new Exception("Couldn't find property:" + propertyName);
		}

		void ParseVariable(string propertyName)
		{
			targetFieldOrProperty = FindFieldOrProperty(targetObject, propertyName);
		}

		void SetValue(object v)
		{
			targetFieldOrProperty.SetValue(v);
		}


		void SetStringValue(string v)
		{
			targetFieldOrProperty.SetValueFromString(v);
		}

		void SetIntegerValue(int v)
		{
			SetValue(v);
		}

		void SetUnsignedIntegerValue(ulong v)
		{
			SetValue(v);
		}

		private void PushDown()
		{
			var context = new Context
				{ savedPropertyTarget = targetFieldOrProperty };
			contexts.Push(context);
			targetObject = Activator.CreateInstance(targetFieldOrProperty.FieldOrPropertyType);
			targetFieldOrProperty = null;
		}

		private void PopContext()
		{
			for (var i = 0; i < currentIndent - lastDetectedIndent; ++i)
			{
				var parentContext = contexts.Pop();
				parentContext.savedPropertyTarget.SetValue(targetObject);
				targetObject = parentContext.savedPropertyTarget.ObjectThatHoldsPropertyOrField;
			}
		}

		private void SetIndent()
		{
			if(lastDetectedIndent == currentIndent + 1)
			{
				PushDown();
			}
			else if(lastDetectedIndent < currentIndent)
			{
				PopContext();
			}
			else
			{
				throw new Exception("Illegal indent:" + lastDetectedIndent + " current:" + currentIndent);
			}

			currentIndent = lastDetectedIndent;
		}

		public T Parse<T>(string testData)
		{
			targetObject = (T)Activator.CreateInstance(typeof(T));
			//var root = (T)FormatterServices.GetUninitializedObject(typeof(T));

			var list = FindMatches(testData);

			foreach (var item in list)
			{
				switch (item.groupName)
				{
					case "variable":
						if(lastDetectedIndent != currentIndent)
						{
							SetIndent();
						}

						ParseVariable(item.value.Substring(0, item.value.Length - 1).Trim());
						break;
					case "integer":
						var integerValue = int.Parse(item.value);
						SetIntegerValue(integerValue);
						break;
					case "hex":
						var hexy = item.value.Trim().ToUpper().Substring(2);
						var integerHexValue = Convert.ToUInt32(hexy, 16);
						SetUnsignedIntegerValue(integerHexValue);
						break;
					case "string":
						var s = item.value.Trim();
						if(s.Length == 0)
						{
							break;
						}

						if((s[0] == '\"' || s[0] == '\''))
						{
							s = s.Substring(1, s.Length - 2);
						}

						SetStringValue(s);

						break;
					case "float":
						SetValue(item.value);
						break;
					case "boolean":
						SetValue(item.value == "true");
						break;
					case "indent":
						lastDetectedIndent = (item.value.Length - 1) / 2;
						break;
					case "indentspaces":
						break;
					case "comment":
						break;
					default:
						throw new Exception($"Unhandled group: {item.groupName}");
				}
			}

			lastDetectedIndent = 0;

			PopContext();

			return (T)targetObject;
		}
	}
}