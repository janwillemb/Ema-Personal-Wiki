using System;
using System.IO;
using EmaXamarin.Api;
using Environment = Android.OS.Environment;

namespace EmaXamarin.Droid
{
    internal class AndroidFileRepository : IFileRepository
    {
        private readonly bool _isInitialized;

        public AndroidFileRepository()
        {
            try
            {
                if (!Directory.Exists(StorageDirectory))
                {
                    Directory.CreateDirectory(StorageDirectory);
                }
                _isInitialized = true;
            }
            catch
            {

                throw;
            }
        }

        public string GetText(string fileName)
        {
            if (!_isInitialized)
            {
                return "Could not initialize external storage. Is the SD-card mounted?";
            }

            var path = GetPath(fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return string.Empty;
        }

        private string GetPath(string path)
        {
            return Path.Combine(StorageDirectory, path);
        }

        public string StorageDirectory => Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, "PersonalWiki");

        public void SaveText(string fileName, string text)
        {
            if (!_isInitialized)
            {
                return;
            }

            File.WriteAllText(GetPath(fileName), text);
        }
    }
}