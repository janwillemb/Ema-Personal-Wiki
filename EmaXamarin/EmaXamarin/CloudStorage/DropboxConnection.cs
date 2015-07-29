using System;
using System.IO;
using System.Threading.Tasks;
using DropNetRT;
using DropNetRT.Models;

namespace EmaXamarin.CloudStorage
{
    public class DropboxConnection
    {
        private readonly DropNetClient _dropboxClient;
        private const string RemoteWikiDirectoryName = "PersonalWiki";

        public DropboxConnection(UserLogin userLogin)
        {
            _dropboxClient = DropboxUserPermission.GetAuthenticatedClient(userLogin);
        }

        /// <summary>
        /// get all information for a remote directory from the Dropbox metadata
        /// </summary>
        private SyncedDirectory SniffRemoteDirectory(Metadata remoteDirectory)
        {
            var result = new SyncedDirectory {Name = remoteDirectory.Name};

            foreach (var child in remoteDirectory.Contents)
            {
                if (child.IsDirectory)
                {
                    result.SubDirectories.Add(SniffRemoteDirectory(child));
                }
                else
                {
                    result.Files.Add(SniffFile(child));
                }
            }

            return result;
        }

        /// <summary>
        /// get all information for a remote file from the Dropbox metadata
        /// </summary>
        private SyncedFile SniffFile(Metadata file)
        {
            var result = new SyncedFile
            {
                Name = file.Name
            };

            result.CurrentSyncTimestamp.Remote = file.UTCDateModified;

            return result;
        }

        public async Task<SyncedDirectory> GetRemoteSyncState()
        {
            var remotePath = "/" + RemoteWikiDirectoryName;

            //check if the directory exists
            Metadata wikiDir = null;
            try
            {
                wikiDir = await _dropboxClient.GetMetaData(remotePath);
            }
            catch (Exception e)
            {
            }

            if (wikiDir == null || wikiDir.IsDeleted || !wikiDir.IsDirectory)
            {
                await _dropboxClient.CreateFolder(remotePath);
            }

            //get remote files info
            return SniffRemoteDirectory(wikiDir);
        }

        public async Task Sync(SyncCommand syncCommand)
        {
            if (syncCommand.Type == SyncType.Download)
            {
                //TODO: save file to stream

                var fileBytes = await _dropboxClient.GetFile(syncCommand.RemotePath);
            }
            else if (syncCommand.Type == SyncType.Upload)
            {
                //TODO: create stream from file
                await _dropboxClient.Upload(syncCommand.LocalPath, syncCommand.Name, Stream.Null);
            }
        }

        public Task<byte[]> GetFile(string remotePath)
        {
            return _dropboxClient.GetFile(remotePath);
        }

        public Task Upload(string subDir, string name, Stream localFileStream)
        {
            return _dropboxClient.Upload(Path.Combine("/" + RemoteWikiDirectoryName, subDir), name, localFileStream);
        }
    }
}