using System.IO;
using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IStorageService
    {
        Task<string> UploadAsync(string path, string fileName, byte[] data);
        Task DownloadFileToStreamAsync(string filePath, Stream outputStream);
    }
}
