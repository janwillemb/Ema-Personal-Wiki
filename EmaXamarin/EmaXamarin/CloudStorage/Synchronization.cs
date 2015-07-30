using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmaXamarin.Api;

namespace EmaXamarin.CloudStorage
{
    public class Synchronization
    {
        private readonly ICloudStorageConnection _connection;
        private readonly IFileRepository _fileRepository;
        private readonly ISyncProgress _syncProgress;
        private static readonly Logging Logger = Logging.For<Synchronization>();

        public Synchronization(ICloudStorageConnection connection, IFileRepository fileRepository, ISyncProgress syncProgress)
        {
            _connection = connection;
            _fileRepository = fileRepository;
            _syncProgress = syncProgress;
        }

        public async Task DoSync()
        {
            _syncProgress.ReportProgress(100, 2, "Constructing sync info...");
            var syncInfo = new SynchronizationState(_connection, _fileRepository);
            await syncInfo.Initialize();

            var commands = syncInfo.CreateSyncCommands().ToArray();
            int num = 0;
            foreach (var syncCommand in commands)
            {
                _syncProgress.ReportProgress(commands.Length + 1, num++, syncCommand + " (" + num + "/" + commands.Length + ")");

                Logger.Info(syncCommand.ToString());
                switch (syncCommand.Type)
                {
                    case SyncType.Download:
                        _fileRepository.CreateDirectory(syncCommand.File.LocalDirectory);
                        var fileBytes = await _connection.GetFile(syncCommand.File.RemotePath);
                        var contents = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length);
                        _fileRepository.SaveText(syncCommand.File.LocalPath, contents);
                        break;

                    case SyncType.Upload:
                        using (Stream localFileStream = _fileRepository.OpenRead(syncCommand.File.LocalPath))
                        {
                            var subDir = syncCommand.File.LocalDirectory.Substring(_fileRepository.StorageDirectory.Length);
                            await _connection.Upload(subDir, syncCommand.File.Name, localFileStream);
                        }
                        break;

                    case SyncType.DeleteLocal:
                        _fileRepository.DeleteFile(syncCommand.File.LocalPath);
                        break;

                    case SyncType.DeleteRemote:
                        await _connection.DeleteFile(syncCommand.File.RemotePath);
                        break;
                }
            }

            _syncProgress.ReportProgress(100, 99, "Saving sync info...");

            //save the timestamps for use in the next syncinfo
            await syncInfo.SaveAfterSync();
        }
    }
}