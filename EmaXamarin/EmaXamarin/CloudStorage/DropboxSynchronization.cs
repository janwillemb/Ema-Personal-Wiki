using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmaXamarin.Api;

namespace EmaXamarin.CloudStorage
{
    public class DropboxSynchronization
    {
        private readonly DropboxConnection _connection;
        private readonly IFileRepository _fileRepository;

        public DropboxSynchronization(DropboxConnection connection, IFileRepository fileRepository)
        {
            _connection = connection;
            _fileRepository = fileRepository;
        }

        public async void DoSync()
        {
            var syncInfo = new DropboxSyncInfo(_connection, _fileRepository);
            await syncInfo.Initialize();

            foreach (var syncCommand in syncInfo.CreateSyncCommands())
            {
                await _connection.Sync(syncCommand);
            }
        }
    }
}
