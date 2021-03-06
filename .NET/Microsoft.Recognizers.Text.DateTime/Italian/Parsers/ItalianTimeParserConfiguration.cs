﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.Recognizers.Definitions.Italian;
using Microsoft.Recognizers.Text.DateTime.Utilities;

namespace Microsoft.Recognizers.Text.DateTime.Italian
{
    public class ItalianTimeParserConfiguration : BaseOptionsConfiguration, ITimeParserConfiguration
    {
        public ItalianTimeParserConfiguration(ICommonDateTimeParserConfiguration config)
            : base(config.Options)
        {
            TimeTokenPrefix = DateTimeDefinitions.TimeTokenPrefix;
            AtRegex = ItalianTimeExtractorConfiguration.AtRegex;
            TimeRegexes = ItalianTimeExtractorConfiguration.TimeRegexList;
            UtilityConfiguration = config.UtilityConfiguration;
            Numbers = config.Numbers;
            TimeZoneParser = config.TimeZoneParser;
        }

        public string TimeTokenPrefix { get; }

        public Regex AtRegex { get; }

        public IEnumerable<Regex> TimeRegexes { get; }

        public IImmutableDictionary<string, int> Numbers { get; }

        public IDateTimeUtilityConfiguration UtilityConfiguration { get; }

        public IDateTimeParser TimeZoneParser { get; }

        public void AdjustByPrefix(string prefix, ref int hour, ref int min, ref bool hasMin)
        {
            var deltaMin = 0;
            var trimedPrefix = prefix.Trim().ToLowerInvariant();

            // c'este 8 heures et demie, - "it's half past 8"
            if (trimedPrefix.EndsWith("demie"))
            {
                deltaMin = 30;
            }
            else if (trimedPrefix.EndsWith("un quart") || trimedPrefix.EndsWith("quart"))
            {
                deltaMin = 15;
            }
            else if (trimedPrefix.EndsWith("trois quarts"))
            {
                deltaMin = 45;
            }
            else
            {
                var match = ItalianTimeExtractorConfiguration.LessThanOneHour.Match(trimedPrefix);
                var minStr = match.Groups["deltamin"].Value;
                if (!string.IsNullOrWhiteSpace(minStr))
                {
                    deltaMin = int.Parse(minStr);
                }
                else
                {
                    minStr = match.Groups["deltaminnum"].Value.ToLower();
                    deltaMin = Numbers[minStr];
                }
            }

            // 'to' i.e 'one to five' = 'un à cinq'
            if (trimedPrefix.EndsWith("à"))
            {
                deltaMin = -deltaMin;
            }

            min += deltaMin;
            if (min < 0)
            {
                min += 60;
                hour -= 1;
            }

            hasMin = true;
        }

        public void AdjustBySuffix(string suffix, ref int hour, ref int min, ref bool hasMin, ref bool hasAm, ref bool hasPm)
        {
            var lowerSuffix = suffix.ToLowerInvariant();
            var deltaHour = 0;
            var match = ItalianTimeExtractorConfiguration.TimeSuffix.MatchExact(lowerSuffix, trim: true);

            if (match.Success)
            {
                var oclockStr = match.Groups["heures"].Value;
                if (string.IsNullOrEmpty(oclockStr))
                {
                    var matchAmStr = match.Groups["am"].Value;
                    if (!string.IsNullOrEmpty(matchAmStr))
                    {
                        if (hour >= 12)
                        {
                            deltaHour = -12;
                        }

                        hasAm = true;
                    }

                    var matchPmStr = match.Groups["pm"].Value;
                    if (!string.IsNullOrEmpty(matchPmStr))
                    {
                        if (hour < 12)
                        {
                            deltaHour = 12;
                        }

                        hasPm = true;
                    }
                }
            }

            hour = (hour + deltaHour) % 24;
        }
    }
}
