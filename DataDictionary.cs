using System;
using System.Collections.Generic;


namespace FFBitrateViewer
{
    // Used for storing values
    // Key   -- case sensitive string
    // Value -- null, int or double
    // On adding a new item to the dictionary the value is automatically converted from string to null/int/double
    public class DataDictionary : Dictionary<string, object?>
    {
        public new void Add(string key, object value)
        {
            if (value == null) base.Add(key, null);
            else if (value is string @string)
            {
                if (int.TryParse(@string, out int @int)) base.Add(key, @int);
                else if (Helpers.TryParseDouble(@string, out double @double, true/*withInfinity*/)) base.Add(key, @double);
                else base.Add(key, null);
            }
            else if (Helpers.IsFloatingPointNumber(ref value)) base.Add(key, Convert.ToDouble(value));
            else if (Helpers.IsIntegralNumber(ref value)) base.Add(key, Convert.ToInt32(value));
            else throw new System.InvalidOperationException("Value must be numeric, string or null");
        }


        public void Add(Dictionary<string, string> pairs)
        {
            if(pairs != null) foreach(var pair in pairs) Add(pair.Key, pair.Value);
        }


        public void Add(Dictionary<string, object> pairs)
        {
            if (pairs != null) foreach (var pair in pairs) Add(pair.Key, pair.Value);
        }
    }

}
