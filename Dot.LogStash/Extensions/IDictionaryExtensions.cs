using System;
using System.Collections.Generic;
using System.Text;

namespace Dot.LogStash.Extensions
{
    public static class IDictionaryExtensions
    {
        public static void AddOrIgnoreNull<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (!dic.ContainsKey(key) && value != null)
            { 
                dic.Add(key, value);
            }
        }

        public static void ReplaceOrIgnoreNull<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (value != null)
            {
                dic[key] = value;
            }
        }
    }
}
