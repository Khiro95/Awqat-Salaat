using System.Threading.Tasks;

namespace AwqatSalaat.DataModel
{
    public interface IServiceClient
    {
        Task<ServiceData> GetDataAsync(IRequest request);
    }
}