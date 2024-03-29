﻿using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Dot.LogStash.SmartFormatters
{
    public abstract class SmartFormatter
    {
        private readonly MatchCollection _matches;
        private readonly bool _dontBother;

        public string Raw { get; protected set; }

        protected SmartFormatter(string input, MatchCollection matches)
        {
            Raw = input;
            _matches = matches;
            _dontBother = _matches.Count == 0;
        }

        public string Format()
        {
            return Format(new Dictionary<string, object>());
        }

        public string Format(Dictionary<string, object> jObj)
        {
            if (_dontBother)
            {
                return Raw;
            }

            var result = new StringBuilder(Raw);
            foreach (Match match in _matches)
            {
                string replacementString;
                if (TryProcessMatch(jObj, match, out replacementString))
                {
                    result.Replace(match.Value, replacementString);
                }
            }

            return result.ToString();
        }

        protected abstract bool TryProcessMatch(Dictionary<string, object> dictionary, Match match, out string replacementString);

        public override string ToString()
        {
            return Raw;
        }

        public static implicit operator string(SmartFormatter f)
        {
            return f == null ? null : f.Raw;
        }
    }
}
