using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmaXamarin.Api;

namespace EmaXamarin.CloudStorage
{
    public class DropboxSynchronization
    {
        private readonly DropboxConnection _connection;
        private readonly IFileRepository _fileRepository;
        private readonly ISyncProgress _syncProgress;
        private static readonly Logging Logger = Logging.For<DropboxSynchronization>();

        public DropboxSynchronization(DropboxConnection connection, IFileRepository fileRepository, ISyncProgress syncProgress)
        {
            _connection = connection;
            _fileRepository = fileRepository;
            _syncProgress = syncProgress;
        }

        public async Task DoSync()
        {
            _syncProgress.ReportProgress(100, 2, "Constructing sync info...");
            var syncInfo = new DropboxSyncInfo(_connection, _fileRepository);
            await syncInfo.Initialize();

            var commands = syncInfo.CreateSyncCommands().ToArray();
            int num = 0;
            foreach (var syncCommand in commands)
            {
                _syncProgress.ReportProgress(commands.Length, num++, syncCommand + " (" + num + "/" + commands.Length + ")");

                Logger.Info(syncCommand.ToString());
                if (syncCommand.Type == SyncType.Download)
                {
                    using (StreamWriter localFileWriter = _fileRepository.OpenStreamWriter(syncCommand.LocalPath))
                    {
                        var fileBytes = await _connection.GetFile(syncCommand.RemotePath);
                        localFileWriter.Write(fileBytes);
                    }
                }
                else if (syncCommand.Type == SyncType.Upload)
                {
                    using (Stream localFileStream = _fileRepository.OpenRead(syncCommand.LocalPath))
                    {
                        //TODO: subdirectories 
                        await _connection.Upload("", syncCommand.Name, localFileStream);
                    }
                }
            }

            _syncProgress.ReportProgress(100, 100, "Saving sync info...");

            //save the timestamps for use in the next syncinfo
            await syncInfo.SaveAfterSync();
        }
    }
}