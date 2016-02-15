using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

namespace yaml {
	internal class YamlParser {
		struct YamlMatch {
			public string groupName;
			public string value;
			public int count;
		}
		struct Context {
			public Object o;
			public PropertyInfo propertyInfo;
			public FieldInfo fieldInfo;
		}
		Object targetObject;
		Stack<Context> contexts = new Stack<Context>();
		PropertyInfo activeProperty;
		FieldInfo activeField;
		int currentIndent;

		public YamlParser() {
		}

		List<YamlMatch> FindMatches (string testData) {
			var outList = new List<YamlMatch>();

			var variable = "(?<variable>[a-zA-Z_$][a-zA-Z0-9_$]*:)";
			var hyphen = "(?<hyphen>\\- )";
			var indent = "(?<indent>\\n(\\s{2})*)";
			var stringMatch = "(?<string>\\'.*\\'|\\\".*\\\")";
			var integerMatch = "(?<integer>[-+]?\\d+)";
			var floatMatch = "(?<float>[-+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?)";
			var booleanMatch = "(?<boolean>(true)|(false))";

			var expressions = new string[] { variable, hyphen, floatMatch, indent, stringMatch, integerMatch, booleanMatch };
			var pattern = string.Join("|", expressions);

			var regExPattern = new Regex(pattern);
			var matches = regExPattern.Matches(testData);

			foreach (Match match in matches) {
				if (match.Success) {
					var i = 0;
					foreach (Group groupData in match.Groups) {
						if (i > 0 && groupData.Success) {
							var groupName = regExPattern.GroupNameFromNumber(i);
							var yamlMatch = new YamlMatch();
							yamlMatch.groupName = groupName;
							yamlMatch.value = match.Value;
							outList.Add(yamlMatch);
						}
						++i;
					}
				} else {
					Console.WriteLine("NO MATCH:" + match.Value);
				}
			}
			return outList;
		}

		void ParseVariable (string propertyName) {
			var t = targetObject.GetType();
			activeField = null;
			activeProperty = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			if (activeProperty == null) {
				activeField = t.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
			}
			if (activeProperty == null && activeField == null) {
				throw new Exception("Couldn't find property:" + propertyName);
			}
		}

		void SetValue(Object v) {
			if (activeField != null) {
				var convertedValue = Convert.ChangeType(v, activeField.FieldType);
				activeField.SetValue(targetObject, convertedValue);
			} else if (activeProperty != null) {
				var convertedValue = Convert.ChangeType(v, activeProperty.PropertyType);
				activeProperty.SetValue(targetObject, convertedValue, null);
			} else {
				throw new Exception("Can not set value in cyberspace!");
			}
		}

		void SetStringValue (string v) {
			SetValue(v);
		}

		void SetIntegerValue (int v) {
			SetValue(v);
		}

		public T Parse <T> ( string testData) {
			var root = (T) Activator.CreateInstance(typeof(T));
			targetObject = root;
			var list = FindMatches(testData);
			foreach (var item in list) {
				switch (item.groupName) {
					case "variable":
						ParseVariable(item.value.Substring(0, item.value.Length - 1));
						break;
					case "integer":
						var integerValue = Int32.Parse(item.value);
						SetIntegerValue(integerValue);
						break;
					case "string":
						SetStringValue(item.value.Substring(1, item.value.Length - 2));
						break;
					case "float":
						SetValue(item.value);
						break;
					case "boolean":
						SetValue(item.value == "true" ? true : false);
						break;
					case "indent":
						var indent = (item.value.Length - 1) / 2;
						if (indent == currentIndent + 1) {
							var context = new Context() { o = targetObject, fieldInfo = activeField, propertyInfo = activeProperty };
							contexts.Push(context);
							if (activeField != null) {
								var fieldValue = activeField.GetValue(targetObject);
								if (fieldValue == null) {
									var instance = Activator.CreateInstance(activeField.FieldType);
									targetObject = instance;
								} else {
									targetObject = fieldValue;
								}
							} else {
								var propertyValue = activeProperty.GetValue(targetObject, null);
								if (propertyValue == null) {
									var instance = Activator.CreateInstance(activeProperty.PropertyType);
									targetObject = instance;
								} else {
									targetObject = propertyValue;
								}
							}
						} else if (indent == currentIndent) {
						} else if (indent < currentIndent) {
							for (var i = 0; i < currentIndent - indent; ++i)  {
								var parentContext = contexts.Pop();
								if (parentContext.propertyInfo != null) {
									parentContext.propertyInfo.SetValue(parentContext.o, targetObject, null);
								} else if (parentContext.fieldInfo != null) {
									parentContext.fieldInfo .SetValue(parentContext.o, targetObject);
								}
								targetObject = parentContext.o;
							}
						} else {
							throw new Exception("Illegal indent:" + indent + " current:" + currentIndent);
						}
						currentIndent = indent;
						break;
				}
			}

			if (contexts.Count != 0) {
				throw new Exception("Illegal count");
			}
			return root;
		}
	}
}

