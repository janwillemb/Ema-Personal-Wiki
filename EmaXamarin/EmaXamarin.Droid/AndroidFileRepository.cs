using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmaXamarin.Api;
using EmaXamarin.CloudStorage;
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
                CreateDirectory(StorageDirectory);
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
            get { return Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, "PersonalWiki"); }
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

        public IEnumerable<string> EnumerateFiles(string searchPattern)
        {
            return Directory.EnumerateFiles(StorageDirectory, searchPattern);
        }

        public StreamWriter OpenStreamWriter(string localPath)
        {
            return new StreamWriter(Path.Combine(StorageDirectory, localPath));
        }

        public Stream OpenRead(string localPath)
        {
            return File.OpenRead(Path.Combine(StorageDirectory, localPath));
        }

        public SyncedDirectory GetLocalSyncState()
        {
            return GetLocalSyncState(new DirectoryInfo(StorageDirectory));
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void CreateDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private SyncedDirectory GetLocalSyncState(DirectoryInfo dir)
        {
            var result = new SyncedDirectory { Name = dir.Name };
            foreach (var subDir in dir.GetDirectories())
            {
                result.AddDir(GetLocalSyncState(subDir));
            }

            foreach (var file in dir.GetFiles())
            {
                result.AddFile(new SyncedFile
                {
                    Name = file.Name,
                    LocalDirectory = file.DirectoryName,
                    CurrentSyncTimestamp = { Local = file.LastWriteTimeUtc }
                });
            }

            return result;
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