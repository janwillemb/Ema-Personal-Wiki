using System;
using System.IO;
using System.Threading.Tasks;
using DropNetRT;
using DropNetRT.Exceptions;
using DropNetRT.Models;

namespace EmaXamarin.CloudStorage
{
    public class DropboxConnection : ICloudStorageConnection
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
            var result = new SyncedDirectory
            {
                Name = remoteDirectory.Name,
                RemoteDeleted = remoteDirectory.IsDirectory
            };

            foreach (var child in remoteDirectory.Contents)
            {
                if (child.IsDirectory)
                {
                    result.SubDirectories.Add(SniffRemoteDirectory(child));
                }
                else
                {
                    result.Files.Add(SniffRemoteFile(child));
                }
            }

            return result;
        }

        /// <summary>
        /// get all information for a remote file from the Dropbox metadata
        /// </summary>
        private SyncedFile SniffRemoteFile(Metadata file)
        {
            var result = new SyncedFile
            {
                Name = file.Name,
                RemoteDeleted = file.IsDeleted,
                RemotePath = file.Path
            };

            result.CurrentSyncTimestamp.Remote = file.UTCDateModified;

            return result;
        }

        public async Task<SyncedDirectory> GetRemoteSyncState()
        {
            try
            {

                var remotePath = "/" + RemoteWikiDirectoryName;

                //check if the directory exists
                Metadata wikiDir = null;
                try
                {
                    wikiDir = await _dropboxClient.GetMetaDataWithDeleted(remotePath);
                }
                catch
                {
                }

                if (wikiDir == null || wikiDir.IsDeleted || !wikiDir.IsDirectory)
                {
                    await _dropboxClient.CreateFolder(remotePath);
                }

                //get remote files info
                return SniffRemoteDirectory(wikiDir);
            }
            catch (DropboxException ex)
            {
                throw CreateUsableExceptionFrom(ex);
            }
        }

        /// <summary>
        /// DropboxException hides the true error in other properties
        /// </summary>
        private Exception CreateUsableExceptionFrom(DropboxException ex)
        {
            return new Exception("Dropbox error " + ex.Response.ReasonPhrase + " (" + ex.StatusCode + ")", ex);
        }

        public Task<byte[]> GetFile(string remotePath)
        {
            try
            {
                return _dropboxClient.GetFile(remotePath);
            }
            catch (DropboxException ex)
            {
                throw CreateUsableExceptionFrom(ex);
            }
        }

        public Task Upload(string subDir, string name, Stream localFileStream)
        {
            try
            {
                return _dropboxClient.Upload(Path.Combine("/" + RemoteWikiDirectoryName, subDir), name, localFileStream);
            }
            catch (DropboxException ex)
            {
                throw CreateUsableExceptionFrom(ex);
            }
        }

        public Task DeleteFile(string remotePath)
        {
            try
            {
                return _dropboxClient.Delete(remotePath);
            }
            catch (DropboxException ex)
            {
                throw CreateUsableExceptionFrom(ex);
            }
        }
    }
}