/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Piot.Yaml
{
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
}