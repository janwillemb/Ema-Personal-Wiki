namespace EmaXamarin.Api
{
    public interface IFileRepository
    {
        string GetText(string fileName);
        string StorageDirectory { get; }
        void SaveText(string fileName, string text);
    }
}