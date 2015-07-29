using System.IO;
using System.Threading.Tasks;

namespace EmaXamarin.CloudStorage
{
    public interface ICloudStorageConnection
    {
        Task<SyncedDirectory> GetRemoteSyncState();
        Task<byte[]> GetFile(string remotePath);
        Task Upload(string subDir, string name, Stream localFileStream);
        Task DeleteFile(string remotePath);
    }
}