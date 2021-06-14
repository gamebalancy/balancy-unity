using System.Collections.Generic;
using System.ComponentModel;
using Balancy.Models;
using Newtonsoft.Json;

namespace Balancy
{
    public partial class DataEditor
    {
        protected class ParseWrapper<T>
        {
            [JsonProperty("list")]
            public T[] List;
            
            [JsonProperty("config")]
            [DefaultValue(null)]
            public Config Config;
        }
        
        protected class Config
        {
            [JsonProperty("selected")]
            [DefaultValue(null)]
            public string Selected;
        }
        
        private static Dictionary<string, BaseModel> _allModels;
        
        public static T GetModelByUnnyId<T>(string unnyId) where T : BaseModel
        {
            if (unnyId == null)
                return null;
            BaseModel model;
            if (_allModels != null && _allModels.TryGetValue(unnyId, out model))
                return model as T;
            return null;
        }
        
        static partial void PrepareGeneratedData();

        public static void Init()
        {
            Storage.OnPrepareModelsAndData += PrepareModelsAndData;
        }

        private static void PrepareModelsAndData()
        {
            _allModels = new Dictionary<string, BaseModel>();
            PrepareGeneratedData();
        }
        
        protected static ParseWrapper<T> ParseDictionary<T>() where T : BaseModel
        {
            string json = Storage.GetRawDictionary(typeof(T).Name);
            if (string.IsNullOrEmpty(json))
                return null;

            var wrapper = JsonConvert.DeserializeObject<ParseWrapper<T>>(json);
            AddModels(wrapper);
            return wrapper;
        }
        
        private static void AddModels<T>(ParseWrapper<T> wrapper) where T : BaseModel
        {
            if (wrapper == null || wrapper.List == null) return;

            foreach (var child in wrapper.List)
            {
                if (_allModels.ContainsKey(child.UnnyId))
                    UnnyLogger.Critical("Model Duplicate id = " + child.UnnyId + " current type =" + typeof(T).Name);
                else
                    _allModels.Add(child.UnnyId, child);
            }
        }
    }
}
