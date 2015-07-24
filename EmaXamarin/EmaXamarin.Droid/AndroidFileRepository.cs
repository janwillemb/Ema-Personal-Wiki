using System;
using System.IO;
using System.Threading.Tasks;
using EmaXamarin.Api;
using Environment = Android.OS.Environment;

namespace EmaXamarin.Droid
{
    internal class AndroidFileRepository : IFileRepository
    {
        private bool _isInitialized;
        private string _storageDirectory;
        private const string ErrorMessage = "Could not initialize external storage. Has the SD-card been mounted?";

        private bool Initialize(bool throwOnError)
        {
            if (_isInitialized)
            {
                return true;
            }

            try
            {
                if (!Directory.Exists(StorageDirectory))
                {
                    Directory.CreateDirectory(StorageDirectory);
                }
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                if (throwOnError)
                {
                    throw new IOException(ErrorMessage, ex);
                }
                return false;
            }
        }

        public string GetText(string fileName)
        {
            if (!Initialize(false))
            {
                return ErrorMessage;
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

		public string DefaultStorageDirectory 
		{
			get 
			{
				return Path.Combine (Environment.ExternalStorageDirectory.AbsolutePath, "PersonalWiki");
			}
		}

        public string StorageDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_storageDirectory))
                {
                    return DefaultStorageDirectory;
                }
                return _storageDirectory;
            }
            set
            {
                _storageDirectory = value;
                _isInitialized = false;
                Initialize(true);
            }
        }

        public void SaveText(string fileName, string text)
        {
            Initialize(true);

            File.WriteAllText(GetPath(fileName), text);
        }

        public Task<bool> MoveTo(string otherDirectory)
        {
            return Task.Run(() =>
            {
                Directory.Move(StorageDirectory, otherDirectory);
                StorageDirectory = otherDirectory;
                return true;
            });
        }

        public Task<bool> CopyTo(string otherDirectory)
        {
            return Task.Run(() =>
            {
                CopyFilesRecursively(new DirectoryInfo(StorageDirectory), new DirectoryInfo(otherDirectory));
                StorageDirectory = otherDirectory;
                return true;
            });
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            if (!target.Exists)
            {
                target.Create();
            }

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }


    }
}