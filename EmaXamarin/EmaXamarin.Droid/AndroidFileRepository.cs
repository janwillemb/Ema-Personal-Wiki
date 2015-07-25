using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void MergeLocalStateInfoInto(SyncedDirectory remoteWikiStateInfo)
        {
            MergeLocalStateInfoInto(new DirectoryInfo(StorageDirectory), remoteWikiStateInfo);
        }

        private void MergeLocalStateInfoInto(DirectoryInfo localDir, SyncedDirectory syncedDirectoryStateInfo)
        {
            //add local directories to cloudDirStateinfo that don't exist remotely
            foreach (var localSubDirectory in localDir.GetDirectories())
            {
                var existsInRemoteDir = syncedDirectoryStateInfo.SubDirectories.Any(x => x.NameEquals(localSubDirectory.Name));
                if (!existsInRemoteDir)
                {
                    var newRemoteDir = new SyncedDirectory { Name = localSubDirectory.Name };
                    syncedDirectoryStateInfo.SubDirectories.Add(newRemoteDir);
                }
            }

            foreach (var cloudSubDir in syncedDirectoryStateInfo.SubDirectories)
            {
                var localSubDirectory = new DirectoryInfo(Path.Combine(localDir.FullName, cloudSubDir.Name));
                MergeLocalStateInfoInto(localSubDirectory, cloudSubDir);
            }

            //add local files to cloudDirState that don't exist remotely
            foreach (var localFile in localDir.GetFiles())
            {
                var existsInRemoteDir = syncedDirectoryStateInfo.Files.Any(x => x.NameEquals(localFile.Name));
                if (!existsInRemoteDir)
                {
                    var newRemoteFile = new SyncedFile { Name = localFile.Name };
                    syncedDirectoryStateInfo.Files.Add(newRemoteFile);
                }
            }

            foreach (var cloudFile in syncedDirectoryStateInfo.Files)
            {
                var localFile = new FileInfo(Path.Combine(localDir.FullName, cloudFile.Name));
                if (localFile.Exists)
                {
                    cloudFile.CurrentSyncTimestamp.Local = localFile.LastWriteTimeUtc;
                }
            }
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