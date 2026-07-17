using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Serialization
{
    public static class Json
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        private static readonly Dictionary<string, Type> polymorphic = Discover();

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static Serializable Deserialize(string json)
        {
            try
            {
                JObject o = JObject.Parse(json);
                string tag = (string)o["type"];
                if (tag == null || !polymorphic.TryGetValue(tag, out Type type)) return null;
                return (Serializable)o.ToObject(type);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static Dictionary<string, Type> Discover()
        {
            var byName = new Dictionary<string, Type>();
            foreach (Type type in typeof(Serializable).Assembly.GetTypes())
                if (type.IsSubclassOf(typeof(Serializable)) && !type.IsAbstract)
                    byName[type.Name] = type;
            return byName;
        }
    }
}
