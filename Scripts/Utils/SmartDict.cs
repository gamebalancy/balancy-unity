using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using Newtonsoft.Json;
using Balancy.Data;

namespace Balancy
{
    [Preserve]
    public class SmartDictConverter<TK, TV> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ((SmartDict<TK, TV>) value).WriteToJson(writer, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new SmartDict<TK, TV>(serializer.Deserialize<Dictionary<TK, TV>>(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }

    [Serializable]
    public class SmartDict<TK, TV> : BaseData
    {
        private Dictionary<TK, TV> _dict;

        #region List methods

        public void Add(TK key, TV value)
        {
            _dict.Add(key, value);
            SetDirty();
        }
        
        public void Set(TK key, TV value)
        {
            _dict[key] = value;
            SetDirty();
        }

        public bool Remove(TK key)
        {
            SetDirty();
            return _dict.Remove(key);
        }

        public TV this[TK key]
        {
            get => _dict[key];
            set => Set(key, value);
        }

        public int Count => _dict.Count;

        public Dictionary<TK, TV>.Enumerator GetEnumerator()
        {
            return _dict.GetEnumerator();
        }
        
        public void Clear()
        {
            SetDirty();
            _dict.Clear();
        }

        public bool ContainsKey(TK key)
        {
            return _dict.ContainsKey(key);
        }
        
        public bool ContainsValue(TV value)
        {
            return _dict.ContainsValue(value);
        }

        #endregion

        public void WriteToJson(JsonWriter writer, JsonSerializer serializer)
        {
            serializer.Serialize(writer, _dict);
        }

        public SmartDict()
        {
            _dict = new Dictionary<TK, TV>();
        }

        public SmartDict(Dictionary<TK, TV> dict)
        {
            _dict = dict ?? new Dictionary<TK, TV>();
        }
    }
}