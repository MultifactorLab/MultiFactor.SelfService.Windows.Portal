using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Core.Http
{
    public class NewtonsoftJsonDataSerializer : IJsonDataSerializer
    {

        public StringContent Serialize(object data, string logPrefix = null)
        {
            var jsonRequest = JsonConvert.SerializeObject(data, SerializingSettings.JsonSerializerSettings);
            return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content, string logPrefix = null)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonResponse, SerializingSettings.JsonSerializerSettings);
        }
    }
}
