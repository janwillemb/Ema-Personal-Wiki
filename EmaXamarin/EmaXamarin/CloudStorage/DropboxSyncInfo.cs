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
        private readonly string[] _excludedFiles = new[] { SyncInfoFileName };

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
            _syncState = await BuildUpSyncState();
        }

        public async Task<SyncedDirectory> BuildUpSyncState()
        {
            var result = await _connection.GetRemoteSyncState();

            //get local files info
            _fileRepository.MergeLocalStateInfoInto(result);

            //get info from previous sync
            MergePrevSyncInfoInto(result);

            return result;
        }

        public async Task SaveAfterSync()
        {
            //load the syncinfo again, with the new timestamps from the server, to save that information for the next sync
            var syncStateAfterTheFact = await BuildUpSyncState();

            CopyCurrentTimestampsToAfterLastSyncFrom(_syncState, syncStateAfterTheFact);

            var syncStateText = JsonConvert.SerializeObject(_syncState);
            _fileRepository.SaveText(SyncInfoFileName, syncStateText);
        }

        private void CopyCurrentTimestampsToAfterLastSyncFrom(SyncedDirectory inputForSync, SyncedDirectory syncStateAfterTheFact)
        {
            foreach (var inputDir in inputForSync.SubDirectories)
            {
                var dirAfterTheFact = syncStateAfterTheFact.SubDirectories.FirstOrDefault(x => x.NameEquals(inputDir.Name));
                dirAfterTheFact = dirAfterTheFact ?? new SyncedDirectory(); //apparently removed during sync by another app or manually
                CopyCurrentTimestampsToAfterLastSyncFrom(inputDir, dirAfterTheFact);
            }

            foreach (var inputFile in inputForSync.Files)
            {
                var fileAfterTheFact = syncStateAfterTheFact.Files.FirstOrDefault(x => x.NameEquals(inputFile.Name));
                if (fileAfterTheFact != null)
                {
                    inputFile.TimestampAfterLastSync = fileAfterTheFact.CurrentSyncTimestamp;
                }
            }
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
                if (prevFile != null)
                {
                    file.TimestampOnLastSync = prevFile.TimestampAfterLastSync;
                }
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
                if (_excludedFiles.Contains(file.Name))
                {
                    continue;
                }

                //local changes go first: remote often has a versioning system in place
                if (!file.TimestampOnLastSync.Local.HasValue || file.CurrentSyncTimestamp.Local > file.TimestampOnLastSync.Local)
                {
                    //TODO: subdirs
                    result.Add(new SyncCommand { LocalPath = file.Name, RemotePath = file.Name, Name = file.Name, Type = SyncType.Upload });
                }
                else if (!file.TimestampOnLastSync.Remote.HasValue || file.CurrentSyncTimestamp.Remote > file.TimestampOnLastSync.Remote)
                {
                    //TODO: subdirs
                    result.Add(new SyncCommand { LocalPath = file.Name, RemotePath = file.Name, Name = file.Name, Type = SyncType.Download });
                }
            }
            return result;
        }
    }
}