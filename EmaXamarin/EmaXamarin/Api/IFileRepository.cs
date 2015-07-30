using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmaXamarin.CloudStorage;

namespace EmaXamarin.Api
{
    public interface IFileRepository
    {
        string GetText(string fileName);
        string DefaultStorageDirectory { get; }
        string StorageDirectory { get; set; }
        void SaveText(string fileName, string text);
        Task<bool> MoveTo(string otherDirectory);
        Task<bool> CopyTo(string otherDirectory);
        IEnumerable<string> EnumerateFiles(string txt);
        Stream OpenRead(string localPath);
        SyncedDirectory GetLocalSyncState();
        void DeleteFile(string path);
        void CreateDirectory(string dir);
    }
}