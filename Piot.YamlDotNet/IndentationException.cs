/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Yaml
{
	public class IndentationException : Exception
	{
		public int IndentationSpaces { get; }

		public IndentationException(int indentationSpaces)
			: base($"The number of leading spaces ({indentationSpaces}) must be even.")
		{
			IndentationSpaces = indentationSpaces;
		}

		public IndentationException(string message, int indentationSpaces)
			: base(message)
		{
			IndentationSpaces = indentationSpaces;
		}

		public IndentationException(string message, int indentationSpaces, Exception innerException)
			: base(message, innerException)
		{
			IndentationSpaces = indentationSpaces;
		}
	}
}