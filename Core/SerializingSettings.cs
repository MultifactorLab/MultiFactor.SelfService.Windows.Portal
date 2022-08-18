using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public static class SerializingSettings
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings 
        { 
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),

            }
        };
    }
}