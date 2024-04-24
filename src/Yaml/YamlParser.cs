/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Piot.Yaml
{
	internal class YamlParser
	{
		public interface IContainerObject
		{
			public object ContainerObject { get; }
		}

		public interface IFieldOrPropertyReference
		{
			public Type FieldOrPropertyType { get; }
			public void SetValue(object boxedValue);
			public bool SetValueFromString(string value);
		}

		public interface ITargetContainer : IContainerObject
		{
			// true means that the list was completed by this string
			public bool SetContainerFromString(string value);

			public bool SupportsSubObject { get; }

			public IFieldOrPropertyReference GetReferenceToPropertyFromName(object propertyName);
		}

		public interface ITargetList : IContainerObject
		{
			public void Add(object value);

			// true means that the list was completed by this string
			public bool AddUsingString(string value);

			public Type ItemType { get; }
		}

		public class StructOrClassContainer : ITargetContainer
		{
			public StructOrClassContainer(object o)
			{
				ContainerObject = o;
			}

			public override string ToString()
			{
				return $"[structOrClass {ContainerObject.GetType().Name} '{ContainerObject}']";
			}

			public bool SetContainerFromString(string value)
			{
				throw new NotImplementedException();
			}

			public bool SupportsSubObject => true;

			public IFieldOrPropertyReference GetReferenceToPropertyFromName(object propertyName)
			{
				return FindFieldOrProperty(ContainerObject, propertyName as string);
			}

			public object ContainerObject { get; }
		}


		public class ListAccumulator : ITargetList
		{
			private IList list;
			private Type elementType;
			private string debugName;

			public ListAccumulator(Type elementType, string debugName)
			{
				this.elementType = elementType;
				this.debugName = debugName;
				var listType = typeof(List<>).MakeGenericType(elementType);
				list = (IList)Activator.CreateInstance(listType);
			}

			public override string ToString()
			{
				return $"[listacc {elementType} {list.Count}]";
			}

			public Type ItemType => elementType;

			public void Add(object boxedValue)
			{
				list.Add(boxedValue);
			}

			public bool AddUsingString(string value)
			{
				if(value.Trim() != "[]")
				{
					throw new Exception($"not a valid string for an array/list '{value}'");
				}

				return true;
			}

			public IFieldOrPropertyReference FindUsingPropertyName(string propertyName)
			{
				throw new NotImplementedException();
			}

			public object ContainerObject
			{
				get
				{
					var array = Array.CreateInstance(elementType, list.Count);
					list.CopyTo(array, 0);
					return array;
				}
			}
		}

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

			public IFieldOrPropertyReference FindUsingPropertyName(string propertyName)
			{
				throw new NotImplementedException();
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
			public IFieldOrPropertyReference savedPropertyReference;
			public ITargetList savedList;
			public ITargetContainer targetContainer;
		}

		readonly Stack<Context> contexts = new();

		private IFieldOrPropertyReference referenceFieldOrProperty;
		private ITargetList targetList;
		private ITargetContainer targetContainer;

		int currentIndent;
		private int lastDetectedIndent;

		List<YamlMatch> FindMatches(string testData)
		{
			var outList = new List<YamlMatch>();

			var variable = @"(?<variable>[a-zA-Z0-9_$]*\s*:)";
			var hyphen = @"(?<hyphen>- \s*)";
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

		/*
		void DebugLogStack(string debug)
		{
			Console.WriteLine($"stack: {debug} {contexts.Count}");
			foreach (var x in contexts.ToArray())
			{
				Console.WriteLine(
					$"*** {x.indent} list: {x.savedList} container: {x.targetContainer} propertyReference {x.savedPropertyReference}");
			}
		}
		*/

		void Push()
		{
			if(referenceFieldOrProperty == null)
			{
				throw new Exception($"can not push with null reference");
			}

			var context = new Context
			{
				indent = currentIndent,
				targetContainer = targetContainer,
				savedPropertyReference = referenceFieldOrProperty,
				savedList = targetList
			};

			contexts.Push(context);
		}


		static bool CheckForDictionary(Type type, out Type keyType, out Type valueType)
		{
			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var typeArguments = type.GetGenericArguments();
				keyType = typeArguments[0];
				valueType = typeArguments[1];
				return true;
			}

			keyType = default;
			valueType = default;

			return false;
		}

		void PushExtraDependingOnFieldOrPropertyReference(string debugName)
		{
			var foundType = referenceFieldOrProperty.FieldOrPropertyType;
			if(foundType.IsArray)
			{
				Push();

				var elementType = foundType.GetElementType();


				var listAccumulator = new ListAccumulator(elementType, debugName);
				targetList = listAccumulator;

				targetContainer = null;
				referenceFieldOrProperty = null;
				currentIndent++;

				return;
			}

			if(CheckForDictionary(foundType, out var keyType, out var valueType))
			{
				Push();

				var dictionaryAccumulator = new DictionaryAccumulator(keyType, valueType, debugName);
				targetList = null;
				targetContainer = dictionaryAccumulator;
				currentIndent++;
				referenceFieldOrProperty = null;
			}
		}


		static IFieldOrPropertyReference FindFieldOrProperty(object o, string propertyName)
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
				return new FieldOrPropertyReference(foundPropertyInfo, foundFieldInfo, o, propertyName);
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
					return new FieldOrPropertyReference(null, field, o, propertyName);
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
					return new FieldOrPropertyReference(property, null, o, propertyName);
				}
			}

			throw new Exception(
				$"Couldn't find property: {propertyName} on object {o} {o.GetType().FullName}");
		}

		void SetReferenceToPropertyName(object propertyName)
		{
			if(targetContainer == null)
			{
				throw new Exception($"illegal state {propertyName} is on a null object");
			}

			referenceFieldOrProperty = targetContainer.GetReferenceToPropertyFromName(propertyName);
			PushExtraDependingOnFieldOrPropertyReference(propertyName.ToString());
		}


		void SetValue(object v)
		{
			referenceFieldOrProperty.SetValue(v);
		}

		void ParseHyphen()
		{
			Console.WriteLine("parse hyphen");
			// The hyphen and space should be ignored when it comes to the lastDetectedIndent, so just increase it
			lastDetectedIndent++;

			if(targetList == null)
			{
				throw new Exception($"internal error. Tried to add an item, but there is no known active list");
			}

			if(targetContainer != null)
			{
				// We were working on a container object, add that to the list
				targetList.Add(targetContainer.ContainerObject);
				if(lastDetectedIndent != currentIndent)
				{
					throw new Exception($"wrong hyphen indent in container {lastDetectedIndent} {currentIndent}");
				}

				targetContainer = null;
			}
			else
			{	
				if(lastDetectedIndent != currentIndent + 1 && lastDetectedIndent != currentIndent)
				{
					throw new Exception($"wrong hyphen indent {lastDetectedIndent} {currentIndent}");
				}
			}

			if(targetList.ItemType.IsPrimitive)
			{
				return;
			}

			targetContainer =
				new StructOrClassContainer(Activator.CreateInstance(targetList.ItemType));
			if(targetContainer == null)
			{
				throw new Exception(
					$"could not create an instance of type {targetList.ItemType}");
			}
		}


		void SetStringValue(string v)
		{
			var containerOrPropertyIsDone = false;

			if(targetContainer != null && referenceFieldOrProperty == null)
			{
				containerOrPropertyIsDone = targetContainer.SetContainerFromString(v);
			}
			else
			{
				if(referenceFieldOrProperty == null)
				{
					throw new Exception($"referenceFieldOrProperty is null");
				}

				containerOrPropertyIsDone = referenceFieldOrProperty.SetValueFromString(v);
			}

			if(containerOrPropertyIsDone)
			{
				DedentTo(currentIndent);
			}
		}

		void SetIntegerValue(int v)
		{
			if(referenceFieldOrProperty != null)
			{
				SetValue(v);
			}
			else if(targetList is not null)
			{
				targetList.Add(v);
			}
			else if(targetContainer != null)
			{
				object o = v;
				SetReferenceToPropertyName(o);
			}
			else
			{
				throw new Exception($"unexpected integer {v}");
			}
		}

		void SetUnsignedIntegerValue(ulong v)
		{
			SetValue(v);
		}


		private IContainerObject ContainerObject => targetList != null
			? targetList
			: targetContainer ?? throw new Exception($"must have list or container to find object");


		private void FinishOngoingListOnDedent()
		{
			if(targetList != null && targetContainer != null)
			{
				targetList.Add(targetContainer.ContainerObject);
				targetContainer = null;
			}
		}

		private void DedentTo(int targetIndent)
		{
			FinishOngoingListOnDedent();
			while (contexts.Count > 0)
			{
				var parentContext = contexts.Pop();
				var savedPropertyTarget = parentContext.savedPropertyReference;

				// Set to saved property target and clear it
				if(savedPropertyTarget != null)
				{
					savedPropertyTarget.SetValue(ContainerObject!.ContainerObject);
				}

				referenceFieldOrProperty = null;
				targetContainer = parentContext.targetContainer;
				targetList = parentContext.savedList;

				if(targetContainer == null && targetList == null)
				{
					throw new Exception($"something is wrong with");
				}

				if(targetIndent == parentContext.indent)
				{
					break;
				}
			}
		}


		private void Indent()
		{
			if(targetList != null)
			{
				// Ignore first indent
				return;
			}

			Push();

			if(referenceFieldOrProperty != null)
			{
				targetContainer =
					new StructOrClassContainer(Activator.CreateInstance(referenceFieldOrProperty.FieldOrPropertyType));
			}

			referenceFieldOrProperty = null;
		}


		private void SetIndentation()
		{
			if(lastDetectedIndent == currentIndent + 1)
			{
				Indent();
			}
			else if(lastDetectedIndent < currentIndent)
			{
				DedentTo(lastDetectedIndent);
			}
			else
			{
				throw new Exception("Illegal indentation:" + lastDetectedIndent + " current:" + currentIndent);
			}

			currentIndent = lastDetectedIndent;
		}

		public T Parse<T>(string testData)
		{
			//var root = (T)FormatterServices.GetUninitializedObject(typeof(T));
			var rootObject = (T)Activator.CreateInstance(typeof(T));
			targetContainer = new StructOrClassContainer(rootObject);
			referenceFieldOrProperty = null;

			var list = FindMatches(testData);

			foreach (var item in list)
			{
				switch (item.groupName)
				{
					case "variable":
						if(lastDetectedIndent != currentIndent)
						{
							SetIndentation();
						}

						SetReferenceToPropertyName(item.value.Substring(0, item.value.Length - 1).Trim());
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

			DedentTo(0);

			return (T)targetContainer.ContainerObject;
		}
	}
}