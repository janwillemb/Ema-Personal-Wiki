using System.IO;
using System.Threading.Tasks;
using DropNetRT;
using DropNetRT.Models;

namespace EmaXamarin.CloudStorage
{
    public class DropboxConnection
    {
        private readonly DropNetClient _dropboxClient;

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

        public async Task<SyncedDirectory> GetRemoteSyncState(string remotePath)
        {
            var metadata = await _dropboxClient.GetMetaData(remotePath);

            if (metadata.IsDeleted || !metadata.IsDirectory)
            {
                await _dropboxClient.CreateFolder(remotePath);
            }

            //get remote files info
            return SniffRemoteDirectory(metadata);
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
    }
}