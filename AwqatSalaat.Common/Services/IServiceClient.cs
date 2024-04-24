using System.Threading.Tasks;

namespace AwqatSalaat.Services
{
    public interface IServiceClient
    {
        Task<ServiceData> GetDataAsync(IRequest request);
    }
}