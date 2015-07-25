using DropNetRT;
using DropNetRT.Models;
using EmaXamarin.Api;
using Newtonsoft.Json;

namespace EmaXamarin.CloudStorage
{
    internal class DropboxFiles
    {
        private readonly IFileRepository _fileRepository;
        private readonly DropNetClient _dropboxClient;

        public DropboxFiles(UserLogin userLogin, IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
            _dropboxClient = DropboxUserPermission.GetAuthenticatedClient(userLogin);
        }

        /// <summary>
        /// Synchronize all files to and from Dropbox
        /// </summary>
        public async void Sync()
        {
            var metadata = await _dropboxClient.GetMetaData("/PersonalWiki");

            if (metadata.IsDeleted || !metadata.IsDirectory)
            {
                await _dropboxClient.CreateFolder("/PersonalWiki");
            }

            var cloudWikiStateInfo = SniffRemoteDirectory(metadata);
            _fileRepository.MergeLocalStateInfoInto(cloudWikiStateInfo);

            var syncInfo = JsonConvert.SerializeObject(cloudWikiStateInfo);
            _fileRepository.SaveText("syncInfo.json", syncInfo);
        }

        /// <summary>
        /// get all information for a remote directory from the Dropbox metadata
        /// </summary>
        private CloudDir SniffRemoteDirectory(Metadata remoteDirectory)
        {
            var result = new CloudDir {Name = remoteDirectory.Name};

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
        private CloudFile SniffFile(Metadata file)
        {
            var result = new CloudFile
            {
                Name = file.Name,
                RemoteModifiedDateTime = file.UTCDateModified,
                RemoteSize = long.Parse(file.Size)
            };
            return result;
        }
    }
}