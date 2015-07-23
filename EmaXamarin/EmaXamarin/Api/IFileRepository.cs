using System.Threading.Tasks;

namespace EmaXamarin.Api
{
    public interface IFileRepository
    {
        string GetText(string fileName);
        string DefaultStorageDirectory { get;  }
        string StorageDirectory { get; set; }
        void SaveText(string fileName, string text);
        Task<bool> MoveTo(string otherDirectory);
        Task<bool> CopyTo(string otherDirectory);
    }
}