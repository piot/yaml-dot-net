/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Yaml
{
	public class ParseException : Exception
	{
		public int LineNumber { get; }
		public Exception InnerException { get; }

		public ParseException(int lineNumber, Exception e)
			: base($"Error on line number ({lineNumber}) {e}.")
		{
			LineNumber = lineNumber;
			InnerException = e;
		}
	}
}