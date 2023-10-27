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
							Console.WriteLine($"Found: groupName:{groupName} => value:{match.Value}");
							yamlMatch.groupName = groupName;
							yamlMatch.value = match.Value;
							outList.Add(yamlMatch);
						}

						++i;
					}
				}
				else
				{
					Console.WriteLine("NO MATCH:" + match.Value);
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

				activeField.SetValue(targetObject, convertedValue);
			}
			else if(activeProperty != null)
			{
				var convertedValue = Convert.ChangeType(v, activeProperty.PropertyType);
				activeProperty.SetValue(targetObject, convertedValue, null);
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

		private void SetIndent(int indent)
		{
			if(indent == currentIndent + 1)
			{
				var context = new Context()
					{ o = targetObject, fieldInfo = activeField, propertyInfo = activeProperty };
				contexts.Push(context);
				if(activeField != null)
				{
					var fieldValue = activeField.GetValue(targetObject);
					if(fieldValue == null)
					{
						var instance = Activator.CreateInstance(activeField.FieldType);
						targetObject = instance;
					}
					else
					{
						targetObject = fieldValue;
					}
				}
				else
				{
					var propertyValue = activeProperty.GetValue(targetObject, null);
					if(propertyValue == null)
					{
						var instance = Activator.CreateInstance(activeProperty.PropertyType);
						targetObject = instance;
					}
					else
					{
						targetObject = propertyValue;
					}
				}
			}
			else if(indent == currentIndent)
			{
			}
			else if(indent < currentIndent)
			{
				for (var i = 0; i < currentIndent - indent; ++i)
				{
					var parentContext = contexts.Pop();
					if(parentContext.propertyInfo != null)
					{
						parentContext.propertyInfo.SetValue(parentContext.o, targetObject, null);
					}
					else if(parentContext.fieldInfo != null)
					{
						parentContext.fieldInfo.SetValue(parentContext.o, targetObject);
					}

					targetObject = parentContext.o;
				}
			}
			else
			{
				throw new Exception("Illegal indent:" + indent + " current:" + currentIndent);
			}

			currentIndent = indent;
		}

		public T Parse<T>(string testData)
		{
			var root = (T)Activator.CreateInstance(typeof(T));
			targetObject = root;
			var list = FindMatches(testData);
			foreach (var item in list)
			{
				switch (item.groupName)
				{
					case "variable":
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
						SetIndent(indent);
						break;
				}
			}

			if(contexts.Count != 0)
			{
				SetIndent(0);
			}

			return root;
		}
	}
}