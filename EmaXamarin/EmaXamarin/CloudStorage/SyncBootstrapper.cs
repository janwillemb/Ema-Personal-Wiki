using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmaXamarin.Api;
using EmaXamarin.CloudStorage.Dropbox;

namespace EmaXamarin.CloudStorage
{
    public class SyncBootstrapper
    {
        private static readonly Dictionary<string, Synchronization> Synchronizations = new Dictionary<string, Synchronization>();
        private static readonly object LockObject = new object();
        private static bool _isSyncing;
        private static bool _stop;
        private static bool _taskIsActive;
        private static readonly Logging Logger = Logging.For<SyncBootstrapper>();
        private static ISyncProgress _syncProgress;
        private static Exception _syncException;
        private static int _intervalMinutes;
        private static DateTime _lastSyncDateTime = DateTime.MinValue;

        public static bool CanSync
        {
            get { return Synchronizations.Any(); }
        }

        public static void AddSync(string key, Synchronization sync)
        {
            lock (LockObject)
                Synchronizations[key] = sync;
        }

        public static void RemoveSync(string key)
        {
            lock (LockObject)
            {
                if (Synchronizations.ContainsKey(key))
                {
                    Synchronizations.Remove(key);
                }
            }
        }

        public static void ShowSyncProgressIn(ISyncProgress syncProgress)
        {
            _syncProgress = syncProgress;
        }

        private static void DontSyncPeriodically()
        {
            _stop = true;
        }

        private static void SyncPeriodically(int intervalMinutes)
        {
            _intervalMinutes = intervalMinutes;
            if (_taskIsActive)
            {
                return;
            }

            _stop = false;
            Task.Run(async () =>
            {
                _taskIsActive = true;

                //always wait a few seconds: allow the UI to appear, so we have a progressbar available to show
                await Task.Delay(TimeSpan.FromSeconds(5));

                while (!_stop)
                {
                    if (_lastSyncDateTime.AddMinutes(_intervalMinutes) < DateTime.Now)
                    {
                        Logger.Info("Sync triggered by interval");
                        await StartSync();
                    }
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                _taskIsActive = false;
            });
        }

        public static Task StartNow()
        {
            return StartSync();
        }

        private static async Task StartSync()
        {
            if (_isSyncing)
                return;

            try
            {
                Synchronization[] synchronizations;
                lock (LockObject)
                {
                    if (_isSyncing)
                        return;

                    _isSyncing = true;
                    synchronizations = Synchronizations.Values.ToArray();
                }

                if (_syncProgress != null)
                    _syncProgress.OnSyncStart();

                foreach (var sync in synchronizations)
                {
                    await sync.DoSync(_syncProgress);
                }

                if (_syncProgress != null)
                    _syncProgress.OnSyncFinished();
            }
            catch (Exception ex)
            {
                Logger.Error("Error during sync", ex);
                lock (LockObject)
                    _syncException = ex;
            }
            finally
            {
                _isSyncing = false;
                _lastSyncDateTime = DateTime.Now;
            }
        }

        public static Exception ConsumeSyncException()
        {
            Exception ex;
            lock (LockObject)
            {
                ex = _syncException;
                _syncException = null;
            }

            return ex;
        }

        public static void RefreshFromSyncInterval()
        {
            var interval = PersistedState.SyncInterval;
            if (interval == 0)
            {
                DontSyncPeriodically();
            }
            else
            {
                SyncPeriodically(interval);
            }
        }

        public static void RefreshDropboxSync(IFileRepository fileRepository)
        {
            var userPermission = PersistedState.UserLogin;
            if (string.IsNullOrEmpty(userPermission.Secret) || string.IsNullOrEmpty(userPermission.Token))
            {
                RemoveSync("Dropbox");
            }
            else
            {
                AddSync("Dropbox", new Synchronization(new DropboxConnection(userPermission), fileRepository));
            }
        }
    }
}