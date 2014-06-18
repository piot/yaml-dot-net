using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

namespace yaml {
	public class YamlParser {
		struct YamlMatch {
			public string groupName;
			public string value;
		}

		Object obj;
		Stack<Object> context = new Stack<Object>();
		PropertyInfo activeProperty;
		FieldInfo activeField;
		int currentIndent;

		public YamlParser() {
		}

		List<YamlMatch> FindMatches (string testData) {
			var outList = new List<YamlMatch>();

			var variable = "(?<variable>[a-zA-Z_$][a-zA-Z0-9_$]*:)";
			var hyphen = "(?<hyphen>[\\-])";
			var indent = "(?<indent>\\n[\\t]*)";
			var stringMatch = "(?<string>\\'.*\\')";
			var integerMatch = "(?<integer>[-+]?\\d+)";
			var pattern = variable + "|" + hyphen + "|" + indent + "|" + stringMatch + "|" + integerMatch;

			var regExPattern = new Regex(pattern);
			var matches = regExPattern.Matches(testData);

			foreach (Match match in matches) {
				if (match.Length > 0) {
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
				}
			}
			return outList;
		}

		void ParseVariable (string propertyName) {
			var t = obj.GetType();
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
				activeField.SetValue(obj, v);
			} else if (activeProperty != null) {
				activeProperty.SetValue(obj, v, null);
			}
		}

		void SetStringValue (string v) {
			SetValue(v);
		}

		void SetIntegerValue (int v) {
			SetValue(v);
		}

		public void Parse (Object o, string testData) {
			obj = o;
			var list = FindMatches(testData);
			foreach (var item in list) {
				Console.WriteLine("group:" + item.groupName + " value:" + item.value);
				switch (item.groupName) {
					case "variable":
						ParseVariable(item.value.Substring(0, item.value.Length - 1));
						break;
					case "integer":
						SetIntegerValue(Int32.Parse(item.value));
						break;
					case "string":
						SetStringValue(item.value.Substring(1, item.value.Length - 2));
						break;
					case "indent":
						var indent = item.value.Length - 1;
						Console.WriteLine("indent:" + indent);
						if (indent == currentIndent + 1) {
							context.Push(obj);
							if (activeField != null) {
								var fieldValue = activeField.GetValue(obj);
								if (fieldValue == null) {
									var instance = Activator.CreateInstance(activeField.FieldType);
									activeField.SetValue(obj, instance);
									obj = instance;
								} else {
									obj = fieldValue;
								}
							} else {
								var propertyValue = activeProperty.GetValue(obj, null);
								if (propertyValue == null) {
									var instance = Activator.CreateInstance(activeProperty.PropertyType);
									activeProperty.SetValue(obj, instance, null);
									obj = instance;
								}
							}
						} else if (indent == currentIndent) {
						} else if (indent == currentIndent - 1) {
							obj = context.Pop();
						} else {
							throw new Exception("Illegal indent!");
						}
						currentIndent = indent;
						break;
				}
			}
		}
	}
}

