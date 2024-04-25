/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Piot.Yaml
{
	internal class YamlParser
	{
		readonly Stack<Context> contexts = new();

		private IFieldOrPropertyReference referenceFieldOrProperty;
		private ITargetList targetList;
		private ITargetContainer targetContainer;

		int currentIndent;
		private int lastDetectedIndent;
		private int lineNumber;

		void Push()
		{
			if(referenceFieldOrProperty is null && targetContainer is null)
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

		void FinalizePreviousItemInListOnSameLevel()
		{
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
					throw new Exception(
						$"expected hyphen to be on same level, but it was different: {lastDetectedIndent} {currentIndent}");
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
		}

		void ParseHyphen()
		{
			FinalizePreviousItemInListOnSameLevel();

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


		private void FinishOngoingListOnPop()
		{
			if(targetList == null || targetContainer == null)
			{
				return;
			}

			targetList.Add(targetContainer.ContainerObject);
			targetContainer = null;
		}

		private void DedentTo(int targetIndent)
		{
			while (contexts.Count > 0)
			{
				FinishOngoingListOnPop();

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

		static int IndentSpaces(string input)
		{
			int count = 0;
			foreach (char c in input)
			{
				if(c == ' ')
					count++;
				else
					break;
			}

			return count;
		}

		static int FindIndent(string input, out string remainingString)
		{
			if(input == null)
			{
				throw new ArgumentNullException(nameof(input));
			}

			var spaceCount = IndentSpaces(input);
			if(spaceCount % 2 != 0)
			{
				throw new IndentationException("The number of leading spaces must be even.", spaceCount);
			}

			remainingString = input[spaceCount..];
			var indent = spaceCount / 2;

			return indent;
		}

		private void ParseMatch(YamlMatch item)
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
				case "indentspaces":
					break;
				case "hyphen":
					lastDetectedIndent++;
					if(lastDetectedIndent != currentIndent)
					{
						SetIndentation();
					}

					ParseHyphen();
					break;
				case "comment":
					break;
				default:
					throw new Exception($"Unhandled group: {item.groupName}");
			}
		}

		private void ParseLine(string line)
		{
			lastDetectedIndent = FindIndent(line, out var restOfLine);
			var matches = FindTokens.FindMatches(restOfLine);

			try
			{
				foreach (var item in matches)
				{
					ParseMatch(item);
				}
			}
			catch (Exception e)
			{
				throw new ParseException(lineNumber, e);
			}
		}

		public T Parse<T>(string content)
		{
			var rootObject = (T)Activator.CreateInstance(typeof(T));
			targetContainer = new StructOrClassContainer(rootObject);
			referenceFieldOrProperty = null;

			var lines = content.Split("\n");
			foreach (var line in lines)
			{
				lineNumber++;
				ParseLine(line);
			}

			lastDetectedIndent = 0;
			DedentTo(0);

			return (T)targetContainer.ContainerObject;
		}
	}
}