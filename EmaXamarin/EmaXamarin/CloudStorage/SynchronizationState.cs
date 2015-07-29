using System.Collections.Generic;
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
        private SyncedDirectory _syncState;
        private const string SyncInfoFileName = ".EmaSyncInfo";
        private readonly string[] _excludedFiles = {SyncInfoFileName};

        public SynchronizationState(ICloudStorageConnection connection, IFileRepository fileRepository)
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

            //get local files info and merge it into our result
            var localState = _fileRepository.GetLocalSyncState();
            MergeLocalDirInfoIntoSyncState(localState, result);

            //get info from previous sync and merge into our result
            var previousSyncInfo = GetPrevSyncInfo();
            MergePrevSyncInfoIntoSyncState(previousSyncInfo, result);

            ReevaluateLocallyDeletedFilesAndDirs(result);

            return result;
        }

        private void ReevaluateLocallyDeletedFilesAndDirs(SyncedDirectory syncedDirectory)
        {
            //evaluate the files first (needed to evaluate the dir)
            foreach (var syncedFile in syncedDirectory.Files.Where(x => x.LocalDeleted).ToArray())
            {
                if (!syncedFile.TimestampOnLastSync.Local.HasValue)
                {
                    //this file wasn't present on the last sync, so it isn't really deleted, but new on the remote side
                    syncedFile.LocalDeleted = false;
                }
            }

            if (syncedDirectory.LocalDeleted)
            {
                if (syncedDirectory.Files.Any(x => !x.LocalDeleted))
                {
                    //if it has non-deleted files in it, then this is a new dir and not a deleted dir
                    syncedDirectory.LocalDeleted = false;
                }
            }

            foreach (var subDir in syncedDirectory.SubDirectories)
            {
                ReevaluateLocallyDeletedFilesAndDirs(subDir);
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
            _fileRepository.SaveText(SyncInfoFileName, syncStateText);
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

        /// <summary>
        /// restore sync info from previous sync from disk
        /// </summary>
        private SyncedDirectory GetPrevSyncInfo()
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
            return prevSyncInfo;
        }

        /// <summary>
        /// merge info from previous sync into the current state (mainly 'last sync datetime')
        /// </summary>
        private void MergePrevSyncInfoIntoSyncState(SyncedDirectory prevSyncInfo, SyncedDirectory syncedDirectory)
        {
            foreach (var subDir in syncedDirectory.SubDirectories)
            {
                var prevSubDir = prevSyncInfo.SubDirectories.FirstOrDefault(x => x.NameEquals(subDir.Name));
                prevSubDir = prevSubDir ?? new SyncedDirectory();
                MergePrevSyncInfoIntoSyncState(subDir, prevSubDir);
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

       /// <summary>
        /// merge state from local directory into the current state.
        /// </summary>
        private void MergeLocalDirInfoIntoSyncState(SyncedDirectory localDirInfo, SyncedDirectory syncState)
        {
            //add local directories to cloudDirStateinfo that don't exist remotely
            foreach (var localSubDirectory in localDirInfo.SubDirectories)
            {
                var existsInRemoteDir = syncState.SubDirectories.Any(x => x.NameEquals(localSubDirectory.Name));
                if (!existsInRemoteDir)
                {
                    var newRemoteDir = new SyncedDirectory {Name = localSubDirectory.Name};
                    syncState.SubDirectories.Add(newRemoteDir);
                }
            }

            foreach (var subDirSyncState in syncState.SubDirectories)
            {
                var localSubDirectory = localDirInfo.SubDirectories.FirstOrDefault(x => x.NameEquals(subDirSyncState.Name));
                if (localSubDirectory == null)
                {
                    //may be deleted locally, but could also be new on the remote side.
                    //we don't know yet, this will be re-evaluated later (in this class)
                    subDirSyncState.LocalDeleted = true;
                    localSubDirectory = new SyncedDirectory();
                }

                MergeLocalDirInfoIntoSyncState(localSubDirectory, subDirSyncState);
            }

            //add local files to cloudDirState that don't exist remotely
            foreach (var localFile in localDirInfo.Files)
            {
                var existsInRemoteDir = syncState.Files.Any(x => x.NameEquals(localFile.Name));
                if (!existsInRemoteDir)
                {
                    var newRemoteFile = new SyncedFile {Name = localFile.Name};
                    syncState.Files.Add(newRemoteFile);
                }
            }

            foreach (var fileSyncState in syncState.Files)
            {
                var localFile = localDirInfo.Files.FirstOrDefault(x => x.NameEquals(fileSyncState.Name));

                if (localFile == null)
                {
                    //may be deleted locally, but could also be new on the remote side.
                    //we don't know yet, this will be re-evaluated later (in this class)
                    fileSyncState.LocalDeleted = true;
                }
                else
                {
                    fileSyncState.CurrentSyncTimestamp.Local = localFile.CurrentSyncTimestamp.Local;
                    fileSyncState.LocalPath = localFile.LocalPath;
                }
            }
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
                if (_excludedFiles.Contains(file.Name))
                {
                    continue;
                }

                //local changes go first: remote often has a versioning system in place

                if (file.LocalDeleted)
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
                else if (!file.TimestampOnLastSync.Local.HasValue || file.CurrentSyncTimestamp.Local > file.TimestampOnLastSync.Local)
                {
                    result.Add(SyncCommand.Upload(file));
                }
                else if (!file.TimestampOnLastSync.Remote.HasValue || file.CurrentSyncTimestamp.Remote > file.TimestampOnLastSync.Remote)
                {
                    result.Add(SyncCommand.Download(file));
                }
            }
            return result;
        }
    }
}