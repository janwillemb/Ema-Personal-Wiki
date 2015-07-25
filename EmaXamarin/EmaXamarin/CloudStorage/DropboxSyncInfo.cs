using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmaXamarin.Api;
using Newtonsoft.Json;

namespace EmaXamarin.CloudStorage
{
    public class DropboxSyncInfo
    {
        private readonly DropboxConnection _connection;
        private readonly IFileRepository _fileRepository;
        private SyncedDirectory _syncState;
        private const string SyncInfoFileName = ".EmaSyncInfo";
        private const string RemoteWikiDirectoryName = "PersonalWiki_2";

        public DropboxSyncInfo(DropboxConnection connection, IFileRepository fileRepository)
        {
            _connection = connection;
            _fileRepository = fileRepository;
        }

        /// <summary>
        /// Synchronize all files to and from Dropbox
        /// </summary>
        public async Task Initialize()
        {
            _syncState = await _connection.GetRemoteSyncState("/" + RemoteWikiDirectoryName);

            //get local files info
            _fileRepository.MergeLocalStateInfoInto(_syncState);

            //get info from previous sync
            MergePrevSyncInfoInto(_syncState);
        }

        public void Save()
        {
            var syncStateText = JsonConvert.SerializeObject(_syncState);
            _fileRepository.SaveText(SyncInfoFileName, syncStateText);
        }

        /// <summary>
        /// merge info from previous sync into the current state (mainly 'last sync datetime')
        /// </summary>
        /// <param name="cloudWikiStateInfo"></param>
        private void MergePrevSyncInfoInto(SyncedDirectory cloudWikiStateInfo)
        {
            var prevSyncInfoText = _fileRepository.GetText(SyncInfoFileName);
            var prevSyncInfo = new SyncedDirectory();
            if (!string.IsNullOrEmpty(prevSyncInfoText))
            {
                try
                {
                    prevSyncInfo = JsonConvert.DeserializeObject<SyncedDirectory>(prevSyncInfoText);
                }
                catch
                {
                    //file is apparently broken: all sync information is lost.
                    //will be overwritten with new information.
                }
            }

            MergePrevSyncInfo(cloudWikiStateInfo, prevSyncInfo);
        }

        private void MergePrevSyncInfo(SyncedDirectory syncedDirectory, SyncedDirectory prevSyncInfo)
        {
            foreach (var subDir in syncedDirectory.SubDirectories)
            {
                var prevSubDir = prevSyncInfo.SubDirectories.FirstOrDefault(x => x.NameEquals(subDir.Name));
                prevSubDir = prevSubDir ?? new SyncedDirectory();
                MergePrevSyncInfo(subDir, prevSubDir);
            }

            foreach (var file in syncedDirectory.Files)
            {
                var prevFile = prevSyncInfo.Files.FirstOrDefault(x => x.NameEquals(file.Name));
                file.TimestampOnLastSync = prevFile.TimestampAfterLastSync;
            }
        }


        public IEnumerable<SyncCommand> CreateSyncCommands()
        {
            return CreateSyncCommands(_syncState);
        }

        private IEnumerable<SyncCommand> CreateSyncCommands(SyncedDirectory dir)
        {
            var result = new List<SyncCommand>();
            foreach (var subDir in dir.SubDirectories)
            {
                result.AddRange(CreateSyncCommands(subDir));
            }

            foreach (var file in dir.Files)
            {
                //local changes go first: remote often has a versioning system in place
                if (file.CurrentSyncTimestamp.Local > file.TimestampOnLastSync.Local)
                {
                    result.Add(new SyncCommand {LocalPath = file.Name, RemotePath = file.Name, Name = file.Name, Type = SyncType.Upload});
                }
                else if (file.CurrentSyncTimestamp.Remote > file.TimestampOnLastSync.Remote)
                {
                    result.Add(new SyncCommand {LocalPath = file.Name, RemotePath = file.Name, Name = file.Name, Type = SyncType.Download});
                }
            }
            return result;
        }
    }
}