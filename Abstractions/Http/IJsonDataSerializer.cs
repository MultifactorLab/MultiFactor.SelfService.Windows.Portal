using System.Net.Http;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Abstractions.Http
{
    public interface IJsonDataSerializer
    {
        StringContent Serialize(object data, string logPrefix = null);
        Task<T> DeserializeAsync<T>(HttpContent content, string logPrefix = null);
    }
}