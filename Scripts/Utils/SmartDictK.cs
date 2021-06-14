using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using Newtonsoft.Json;
using Balancy.Data;
using Balancy.Models;

namespace Balancy
{
    [Preserve]
    public class SmartDictKConverter<TK, TV> : JsonConverter where TV : BaseModel where TK : BaseModel
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ((SmartDictK<TK, TV>) value).WriteToJson(writer, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new SmartDictK<TK, TV>(serializer.Deserialize<Dictionary<string, TV>>(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }

    [Serializable]
    public class SmartDictK<TK, TV> : BaseData where TK : BaseModel
    {
        private Dictionary<TK, TV> _dictFinal;
        private Dictionary<string, TV> _dict;

        #region List methods

        public void Add(TK key, TV value)
        {
            _dictFinal.Add(key, value);
            _dict.Add(key.UnnyId, value);
            SetDirty();
        }
        
        public void Set(TK key, TV value)
        {
            _dictFinal[key] = value;
            _dict[key.UnnyId] = value;
            SetDirty();
        }

        public bool Remove(TK key)
        {
            SetDirty();
            return _dictFinal.Remove(key) && _dict.Remove(key.UnnyId);
        }

        public TV this[TK key]
        {
            get => _dictFinal[key];
            set => Set(key, value);
        }

        public int Count => _dict.Count;

        public Dictionary<TK, TV>.Enumerator GetEnumerator()
        {
            return _dictFinal.GetEnumerator();
        }
        
        public void Clear()
        {
            SetDirty();
            _dict.Clear();
        }

        public bool ContainsKey(TK key)
        {
            return _dictFinal.ContainsKey(key);
        }
        
        public bool ContainsValue(TV value)
        {
            return _dictFinal.ContainsValue(value);
        }

        #endregion

        public void WriteToJson(JsonWriter writer, JsonSerializer serializer)
        {
            serializer.Serialize(writer, _dict);
        }

        public SmartDictK()
        {
            _dict = new Dictionary<string, TV>();
            _dictFinal = new Dictionary<TK, TV>();
        }

        public SmartDictK(Dictionary<string, TV> dict)
        {
            if (dict == null)
            {
                _dict = new Dictionary<string, TV>();
                _dictFinal = new Dictionary<TK, TV>();
            }
            else
            {
                _dict = dict;
                _dictFinal = new Dictionary<TK, TV>();
                foreach (var kvp in _dict)
                    _dictFinal.Add((TK) GetKey(kvp.Key), kvp.Value);
            }
        }

        private BaseModel GetKey(string key)
        {
            return DataEditor.GetModelByUnnyId<BaseModel>(key);
        }
    }
}