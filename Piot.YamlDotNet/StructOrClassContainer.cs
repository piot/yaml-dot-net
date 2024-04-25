/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Reflection;

namespace Piot.Yaml
{
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


		public IFieldOrPropertyReference GetReferenceToPropertyFromName(object propertyName)
		{
			return FindFieldOrProperty(ContainerObject, propertyName as string);
		}

		public object ContainerObject { get; }
	}
}