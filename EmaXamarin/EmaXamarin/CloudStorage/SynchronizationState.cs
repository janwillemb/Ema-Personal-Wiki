using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmaXamarin.Api;
using Newtonsoft.Json;

namespace EmaXamarin.CloudStorage
{
    public class SynchronizationState
    {
        private readonly ICloudStorageConnection _connection;
        private readonly IFileRepository _fileRepository;
        private readonly string _syncName;
        private SyncedDirectory _syncState;
        private const string SyncInfoFileName = ".EmaSyncInfo";

        public SynchronizationState(ICloudStorageConnection connection, IFileRepository fileRepository, string syncName)
        {
            _connection = connection;
            _fileRepository = fileRepository;
            _syncName = syncName;
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

            //get local files info and merge it into our result
            result.CopyInfoFromLocalState(_fileRepository.GetLocalSyncState());

            //get info from previous sync and merge into our result
            result.CopyInfoFromPreviousSync(GetPrevSyncInfo());

            EvaluateLocallyAbsentFilesAndDirs(result);

            return result;
        }

        private void EvaluateLocallyAbsentFilesAndDirs(SyncedDirectory syncedDirectory)
        {
            //evaluate the files first (needed to evaluate the dir)
            foreach (var syncedFile in syncedDirectory.Files.Where(x => x.LocallyAbsent && !x.RemoteDeleted).ToArray())
            {
                //if this file was present on the last sync, it's really deleted, otherwise it is new on the remote side
                syncedFile.LocallyDeleted = syncedFile.TimestampOnLastSync.Local.HasValue;

                if (!syncedFile.LocallyDeleted)
                {
                    //remote new, construct local directory
                    syncedFile.LocalDirectory = Path.Combine(_fileRepository.StorageDirectory, syncedDirectory.GetRelativePath());
                }
            }

            if (syncedDirectory.LocallyAbsent)
            {
                var shouldLive = syncedDirectory.Files.Any(x => !x.LocallyDeleted);
                syncedDirectory.LocallyDeleted = !shouldLive;
            }

            foreach (var subDir in syncedDirectory.SubDirectories)
            {
                EvaluateLocallyAbsentFilesAndDirs(subDir);
            }
        }

        /// <summary>
        /// add latest info to syncinfo and preserve on disk
        /// </summary>
        /// <returns></returns>
        public async Task SaveAfterSync()
        {
            //load the syncinfo again, with the new timestamps from the server, to save that information for the next sync
            var syncStateAfterTheFact = await BuildUpSyncState();

            CopyCurrentTimestampsToAfterLastSyncFrom(_syncState, syncStateAfterTheFact);

            var syncStateText = JsonConvert.SerializeObject(_syncState);
            _fileRepository.SaveText(GetSyncInfoFileName(), syncStateText);
        }

        /// <summary>
        /// merge syncinfo from after sync to the properties in the syncinfo
        /// </summary>
        /// <param name="inputForSync">syncinfo before and during sync</param>
        /// <param name="syncStateAfterTheFact">syncinfo after sync</param>
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

        private string GetSyncInfoFileName()
        {
            return SyncInfoFileName + "_" + _syncName;
        }

        /// <summary>
        /// restore sync info from previous sync from disk
        /// </summary>
        private SyncedDirectory GetPrevSyncInfo()
        {
            var prevSyncInfoText = _fileRepository.GetText(GetSyncInfoFileName());
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
            return prevSyncInfo;
        }


        /// <summary>
        /// evaluate syncstate and destill sync commands 
        /// </summary>
        public IEnumerable<SyncCommand> CreateSyncCommands()
        {
            return CreateSyncCommands(_syncState);
        }

        /// <summary>
        /// recursively evaluate syncstate and destill sync commands 
        /// </summary>
        private IEnumerable<SyncCommand> CreateSyncCommands(SyncedDirectory dir)
        {
            var result = new List<SyncCommand>();
            foreach (var subDir in dir.SubDirectories)
            {
                result.AddRange(CreateSyncCommands(subDir));
            }

            foreach (var file in dir.Files)
            {
                if (GetSyncInfoFileName().Equals(file.Name))
                {
                    continue;
                }

                var isNewLocally = !file.LocallyAbsent && !file.TimestampOnLastSync.Local.HasValue;
                var isNewRemote = file.LocallyAbsent && !file.TimestampOnLastSync.Remote.HasValue;

                //local changes go first: remote often has a versioning system in place

                if (file.LocallyDeleted)
                {
                    if (!file.RemoteDeleted)
                    {
                        result.Add(SyncCommand.DeleteRemote(file));
                    }
                }
                else if (file.RemoteDeleted)
                {
                    if (file.LocalPath != null) //may already be deleted locally
                    {
                        result.Add(SyncCommand.DeleteLocal(file));
                    }
                }
                else if (isNewLocally || file.CurrentSyncTimestamp.Local > file.TimestampOnLastSync.Local)
                {
                    result.Add(SyncCommand.Upload(file));
                }
                else if (isNewRemote || file.CurrentSyncTimestamp.Remote > file.TimestampOnLastSync.Remote)
                {
                    result.Add(SyncCommand.Download(file));
                }
            }
            return result;
        }
    }
}