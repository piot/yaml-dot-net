/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/yaml-dot-net
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Piot.Yaml
{
	internal static class FindTokens
	{
		internal static List<YamlMatch> FindMatches(string testData)
		{
			var outList = new List<YamlMatch>();

			var variable = @"(?<variable>[a-zA-Z0-9_$]+\s*:)";
			var hyphen = @"(?<hyphen>- \s*)";
			var stringMatch = @"(?<string>.*)";
			var integerMatch = @"(?<integer>\s*-?\d+)";
			var hexMatch = @"(?<hex>\s*0[xX][0-9a-fA-F]+)";
			var floatMatch = @"(?<float>\s*-?\d+\.\d+)";
			var booleanMatch = @"(?<boolean>(true|false))";
			var commentMatch = @"(?<comment>\s*\#.+)";

			var expressions = new[]
			{
				variable,
				hyphen,
				hexMatch,
				floatMatch,
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
							var yamlMatch = new YamlMatch
							{
								groupName = groupName,
								value = match.Value,
							};
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
	}
}