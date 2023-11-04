/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Piot.Yaml
{
	internal class YamlParser
	{
		public interface IFieldOrPropertyTarget
		{
			public Type FieldOrPropertyType { get; }
			public bool NeedsPushDown { get; }
			public void SetValue(object boxedValue);
			public bool SetValueFromString(string value);

			public object ObjectThatHoldsPropertyOrField { get; }
		}

		public class ListAccumulator : IFieldOrPropertyTarget
		{
			private List<object> items = new();
			private Type itemType;
			private string debugName;

			public ListAccumulator(Type itemType, string debugName)
			{
				this.itemType = itemType;
				this.debugName = debugName;
			}

			public override string ToString()
			{
				return $"[listacc {itemType} {items.Count}]";
			}

			public Type FieldOrPropertyType => itemType;
			public bool NeedsPushDown => false;

			public void SetValue(object boxedValue)
			{
				items.Add(boxedValue);
			}

			public bool SetValueFromString(string value)
			{
				if(value.Trim() != "[]")
				{
					throw new Exception($"not a valid string for an array/list '{value}'");
				}

				return true;
			}

			public object ObjectThatHoldsPropertyOrField
			{
				get
				{
					// Convert the List<object> to an array of the determined element type
					var array = Array.CreateInstance(itemType, items.Count);
					items.ToArray().CopyTo(array, 0);

					return array;
				}
			}
		}

		public class FieldOrPropertyTarget : IFieldOrPropertyTarget
		{
			private readonly PropertyInfo propertyInfo;
			private readonly FieldInfo fieldInfo;
			private readonly Type fieldOrPropertyType;
			private readonly string debugName;

			public Type FieldOrPropertyType => propertyInfo != null ? propertyInfo.PropertyType :
				fieldInfo != null ? fieldInfo.FieldType : throw new Exception($"internal error");

			public bool NeedsPushDown => true;

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

		struct YamlMatch
		{
			public string groupName;
			public string value;
		}

		struct Context
		{
			public int indent;
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
			var hyphen = @"(?<hyphen>-\s*)";
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

		void Push()
		{
			var context = new Context
				{ indent = currentIndent, savedPropertyTarget = targetFieldOrProperty };

			contexts.Push(context);
		}

		IFieldOrPropertyTarget CreateFieldOrPropertyTarget(PropertyInfo propertyInfo, FieldInfo fieldInfo,
			object o, string debugName)
		{
			var foundTarget = new FieldOrPropertyTarget(propertyInfo, fieldInfo, o, debugName);

			if(!foundTarget.FieldOrPropertyType.IsArray)
			{
				return foundTarget;
			}

			targetFieldOrProperty = foundTarget;
			Push();

			var elementType = foundTarget.FieldOrPropertyType.GetElementType();

			targetObject = null;

			var listAccumulator = new ListAccumulator(elementType, debugName);
			targetFieldOrProperty = listAccumulator;
			lastDetectedIndent++;

			var context2 = new Context
				{ indent = -1, savedPropertyTarget = listAccumulator };
			contexts.Push(context2);

			return listAccumulator;
		}

		IFieldOrPropertyTarget FindFieldOrProperty(object o, string propertyName)
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
				return CreateFieldOrPropertyTarget(foundPropertyInfo, foundFieldInfo, o, propertyName);
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
					return CreateFieldOrPropertyTarget(null, field, o, propertyName);
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
					return CreateFieldOrPropertyTarget(property, null, o, propertyName);
				}
			}

			throw new Exception(
				$"Couldn't find property: {propertyName} on object {targetObject} {targetObject.GetType().FullName}");
		}

		void ParseVariable(string propertyName)
		{
			if(targetObject == null)
			{
				throw new Exception($"illegal state {propertyName} is on a null object");
			}

			targetFieldOrProperty = FindFieldOrProperty(targetObject, propertyName);
		}

		void SetValue(object v)
		{
			targetFieldOrProperty.SetValue(v);
		}

		void ParseHyphen()
		{
			var parentFieldOrProperty = contexts.Peek().savedPropertyTarget;
			if(targetObject != null)
			{
				if(parentFieldOrProperty.NeedsPushDown)
				{
					throw new Exception($"internal error");
				}

				parentFieldOrProperty.SetValue(targetObject);
				if(lastDetectedIndent != currentIndent)
				{
					throw new Exception($"wrong hyphen indent {lastDetectedIndent} {currentIndent}");
				}
			}
			else
			{
				if(lastDetectedIndent != currentIndent + 1)
				{
					throw new Exception($"wrong hyphen indent {lastDetectedIndent} {currentIndent}");
				}
			}

			targetObject = Activator.CreateInstance(parentFieldOrProperty.FieldOrPropertyType);
			if(targetObject == null)
			{
				throw new Exception(
					$"could not create an instance of type {parentFieldOrProperty.FieldOrPropertyType}");
			}
		}


		void SetStringValue(string v)
		{
			var objectIsDone = targetFieldOrProperty.SetValueFromString(v);
			if(objectIsDone)
			{
				PopContext(currentIndent);
			}
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
			Push();
			targetObject = Activator.CreateInstance(targetFieldOrProperty.FieldOrPropertyType);

			targetFieldOrProperty = null;
		}

		private void PopContext(int targetIndent)
		{
			while (contexts.Count > 0)
			{
				var parentContext = contexts.Pop();
				var savedPropertyTarget = parentContext.savedPropertyTarget;

				if(targetObject != null)
				{
					savedPropertyTarget.SetValue(targetObject);
				}

				targetObject = savedPropertyTarget.ObjectThatHoldsPropertyOrField;
				if(targetObject == null)
				{
					throw new Exception($"something is wrong with {savedPropertyTarget}");
				}

				if(targetIndent != parentContext.indent)
				{
					continue;
				}

				targetFieldOrProperty = savedPropertyTarget;

				break;
			}
		}

		private void SetIndent()
		{
			if(lastDetectedIndent == currentIndent + 1)
			{
				if(targetFieldOrProperty.NeedsPushDown)
				{
					PushDown();
				}
				else
				{
				}
			}
			else if(lastDetectedIndent < currentIndent)
			{
				PopContext(lastDetectedIndent);
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
					case "hyphen":


						ParseHyphen();
						break;
					case "comment":
						break;
					default:
						throw new Exception($"Unhandled group: {item.groupName}");
				}
			}

			lastDetectedIndent = 0;

			PopContext(0);

			return (T)targetObject;
		}
	}
}