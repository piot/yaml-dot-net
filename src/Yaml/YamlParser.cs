/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Piot.Yaml
{
	internal class YamlParser
	{
		struct YamlMatch
		{
			public string groupName;
			public string value;
		}

		struct Context
		{
			public Object o;
			public PropertyInfo propertyInfo;
			public FieldInfo fieldInfo;
		}

		Object targetObject;
		readonly Stack<Context> contexts = new();
		PropertyInfo activeProperty;
		FieldInfo activeField;
		int currentIndent;
		private int lastDetectedIndent;


		List<YamlMatch> FindMatches(string testData)
		{
			var outList = new List<YamlMatch>();

			var variable = @"(?<variable>[a-zA-Z_$][a-zA-Z0-9_$]*:)";
			var hyphen = @"(?<hyphen>^-\s(.+)$)";
			var indent = @"(?<indent>\n+(\s{2})*)";
			var stringMatch = @"(?<string>.*)";
			var integerMatch = @"(?<integer>\s*[-+]?[1-9]\d*)";
			var hexMatch = @"(?<hex>\s*0[xX][0-9a-fA-F]*)";
			var floatMatch = @"(?<float>\s*[-+]?[0-9]*\.[0-9]+([eE][-+]?[0-9]+)?)";
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

		void ParseVariable(string propertyName)
		{
			var t = targetObject.GetType();

			activeField = null;
			activeProperty = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			if(activeProperty == null)
			{
				activeField = t.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
			}

			if(activeProperty == null && activeField == null)
			{
				var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var hasAttribute = Attribute.IsDefined(field, typeof(YamlPropertyAttribute));
					if(hasAttribute)
					{
						var attribute =
							(YamlPropertyAttribute)Attribute.GetCustomAttribute(field, typeof(YamlPropertyAttribute));
						if(attribute.Description == propertyName)
						{
							activeField = field;
							return;
						}
					}
				}

				var properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				foreach (var property in properties)
				{
					var hasAttribute = Attribute.IsDefined(property, typeof(YamlPropertyAttribute));
					if(hasAttribute)
					{
						var attribute =
							(YamlPropertyAttribute)Attribute.GetCustomAttribute(property,
								typeof(YamlPropertyAttribute));
						if(attribute.Description == propertyName)
						{
							activeProperty = property;
							return;
						}
					}
				}

				throw new Exception("Couldn't find property:" + propertyName);
			}
		}

		void SetValue(object v)
		{
			if(activeField != null)
			{
				object convertedValue;
				try
				{
					convertedValue = Convert.ChangeType(v, activeField.FieldType);
				}
				catch (FormatException e)
				{
					throw new FormatException(" Couldn't format:" + activeField.Name + " value:" + v.ToString() +
					                          " because:" + e.ToString());
				}

				// Console.WriteLine($"set field {activeField} in {targetObject} <- {convertedValue}");
				activeField.SetValue(targetObject, convertedValue);
			}
			else if(activeProperty != null)
			{
				var convertedValue = Convert.ChangeType(v, activeProperty.PropertyType);
				activeProperty.SetValue(targetObject, convertedValue, null);
				// Console.WriteLine($"set property {targetObject} <- {convertedValue}");
			}
			else
			{
				throw new Exception("Can not set value in cyberspace!");
			}
		}

		void SetStringValue(string v)
		{
			SetValue(v);
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
				{ o = targetObject, fieldInfo = activeField, propertyInfo = activeProperty };
//			Console.WriteLine($"pushing context {lastIndent}");
			contexts.Push(context);

			if(activeField != null)
			{
				var instance = Activator.CreateInstance(activeField.FieldType);
				targetObject = instance;
			}
			else
			{
				var instance = Activator.CreateInstance(activeProperty.PropertyType);
				targetObject = instance;
			}

			activeField = null;
			activeProperty = null;
		}

		private void PopContext()
		{
			for (var i = 0; i < currentIndent - lastDetectedIndent; ++i)
			{
				var parentContext = contexts.Pop();
				if(parentContext.propertyInfo != null)
				{
					//Console.WriteLine(
					//	$"setting to parent class {parentContext.propertyInfo} {parentContext.o}  from value that has been done {targetObject}");
					parentContext.propertyInfo.SetValue(parentContext.o, targetObject, null);
				}
				else if(parentContext.fieldInfo != null)
				{
					//Console.WriteLine(
					//	$"setting to parent object {parentContext.fieldInfo} {parentContext.o} {parentContext.o.GetType().FullName} from value that has been done {targetObject}");
					parentContext.fieldInfo.SetValue(parentContext.o, targetObject);
				}

				targetObject = parentContext.o;
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
			var root = (T)Activator.CreateInstance(typeof(T));
			//var root = (T)FormatterServices.GetUninitializedObject(typeof(T));

			targetObject = root;
			activeProperty = null;
			activeField = null;

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

						ParseVariable(item.value.Substring(0, item.value.Length - 1));
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
						if(s.Length > 0)
						{
							if((s[0] == '\"' || s[0] == '\''))
							{
								SetStringValue(s.Substring(1, s.Length - 2));
							}
							else
							{
								SetStringValue(s);
							}
						}

						break;
					case "float":
						SetValue(item.value);
						break;
					case "boolean":
						SetValue(item.value == "true");
						break;
					case "indent":
						var indent = (item.value.Length - 1) / 2;
						lastDetectedIndent = indent;
						break;
				}
			}

			lastDetectedIndent = 0;
			PopContext();

			return (T)targetObject;
		}
	}
}