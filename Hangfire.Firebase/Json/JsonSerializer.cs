using Newtonsoft.Json;
using FireSharp.Interfaces;

namespace Hangfire.Firebase.Json
{
    internal class JsonSerializer : ISerializer
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new LowerCaseDelimitedPropertyNamesContractResovler()
        };

        public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, settings);
        public string Serialize<T>(T value) => JsonConvert.SerializeObject(value, settings);
    }
}
