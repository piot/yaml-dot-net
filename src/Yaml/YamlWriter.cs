/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Piot.Yaml
{
	internal class YamlWriter
	{
		bool IsPrimitive(Type t)
		{
			return t.IsPrimitive || t == typeof(Decimal) || t == typeof(String) || t.IsEnum;
		}

		bool IsDictionary(Type t)
		{
			return typeof(IDictionary<,>).IsAssignableFrom(t) || typeof(IDictionary).IsAssignableFrom(t);
		}

		IDictionary ToDictionary(object o)
		{
			if(!IsDictionary(o.GetType()))
			{
				return null;
			}

			return (IDictionary)o;
		}

		bool ShouldNewLineAndIndent(Type t)
		{
			return !IsPrimitive(t) || t.IsArray;
		}

		bool ShouldNewLineAndIndent(Object o)
		{
			if(o != null && (
				   (o.GetType().IsArray && ((Array)o).Length == 0) ||
				   (IsDictionary(o.GetType()) && ToDictionary(o).Count == 0)))
			{
				return false;
			}

			return o != null && ShouldNewLineAndIndent(o.GetType());
		}


		void WriteArray(object o, TextWriter writer, int indent)
		{
			var arr = (Array)o;

			foreach (var item in arr)
			{
				WriteLeafOrObject(item, writer, indent + 1, true);
			}
		}

		void WriteDictionary(object o, TextWriter writer, int indent)
		{
			var dictionary = (IDictionary)o;
			var tabs = new String(' ', indent * 2);

			foreach (var key in dictionary.Keys)
			{
				var value = dictionary[key];
				writer.Write($"\n{tabs}{key}:");
				WriteLeafOrObject(value, writer, indent + 1);
			}
		}

		void WriteLeafLine(object subValue, TextWriter writer)
		{
			var subValueToWrite = subValue;

			if(subValue is null)
			{
				subValueToWrite = "{}";
			}
			else
			{
				if(subValue.GetType().IsEnum)
				{
					subValueToWrite = $"{subValue}";
				}

				if(subValue is string)
				{
					subValueToWrite = "'" + subValueToWrite + "'";
				}

				if(subValue is bool truth)
				{
					subValueToWrite = truth ? "true" : "false";
				}

				if(!IsPrimitive(subValue.GetType()))
				{
					subValueToWrite = "{}";
				}
				else if(subValue.GetType().IsArray && ((Array)subValue).Length == 0)
				{
					subValueToWrite = "[]";
				}
				else if(IsDictionary(subValue.GetType()) && ToDictionary(subValue).Count == 0)
				{
					subValueToWrite = "{}";
				}
			}

			writer.WriteLine($"{subValueToWrite}");
		}

		void WriteLeafOrObject(object o, TextWriter writer, int indent, bool prefixWithDash = false)
		{
			if(ShouldNewLineAndIndent(o))
			{
				WriteObject(o, writer, indent, prefixWithDash);
			}
			else
			{
				WriteLeafLine(o, writer);
			}
		}

		void WriteObject(object o, TextWriter writer, int indent, bool prefixWithDash = false)
		{
			var tabs = new String(' ', indent * 2);
			var tabsOneLess = "";
			if(prefixWithDash)
			{
				tabsOneLess = new String(' ', (indent - 1) * 2);
			}

			bool firstLine = true;

			var t = o.GetType();

			Console.WriteLine($"writing object of type {t.Name} isDictionary: {IsDictionary(t)}");


			if(t.IsArray)
			{
				WriteArray(o, writer, indent);
				return;
			}

			if(IsDictionary(t))
			{
				WriteDictionary(o, writer, indent);
				return;
			}

			writer.Write($"\n");

			var properties =
				t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var p in properties)
			{
				var subValue = p.GetValue(o, null);
				var propertyName = GetSerializerName(p);
				if(firstLine && prefixWithDash)
				{
					writer.Write($"{tabsOneLess}- ");
				}
				else
				{
					writer.Write($"{tabs}");
				}

				firstLine = false;

				if(ShouldNewLineAndIndent(subValue))
				{
					writer.Write("{propertyName}:");
					WriteObject(subValue, writer, indent + 1);
				}
				else
				{
					writer.Write($"{propertyName}: ");
					WriteLeafLine(subValue, writer);
				}
			}

			var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var f in fields)
			{
				var subValue = f.GetValue(o);
				var fieldName = GetSerializerName(f);

				if(firstLine && prefixWithDash)
				{
					writer.Write($"{tabsOneLess}- ");
				}
				else
				{
					writer.Write($"{tabs}");
				}

				firstLine = false;

				if(ShouldNewLineAndIndent(subValue))
				{
					writer.Write($"{fieldName}:");
					WriteObject(subValue, writer, indent + 1);
				}
				else
				{
					writer.Write($"{fieldName}: ");
					WriteLeafLine(subValue, writer);
				}
			}
		}

		private string GetSerializerName(FieldInfo fieldInfo)
		{
			var attribute =
				(YamlPropertyAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(YamlPropertyAttribute));
			return attribute != null ? attribute.Description : fieldInfo.Name;
		}

		private string GetSerializerName(PropertyInfo propertyInfo)
		{
			var attribute =
				(YamlPropertyAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(YamlPropertyAttribute));
			return attribute != null ? attribute.Description : propertyInfo.Name;
		}

		public string Write(Object o)
		{
			var writer = new StringWriter();
			WriteObject(o, writer, 0);
			writer.Close();
			return writer.ToString();
		}
	}
}