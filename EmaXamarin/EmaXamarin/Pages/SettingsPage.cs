using System.IO;
using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    public class SettingsPage : ContentPage
    {
        private readonly IFileRepository _fileRepository;
        private SwitchCell _customStorageSwitch;
        private EntryCell _customStorageDirectoryEntry;

        public SettingsPage(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;

            InitializeStorageSettings(fileRepository);

            Content = new TableView
            {
                Intent = TableIntent.Settings,
                Root = new TableRoot
                {
                    new TableSection("Storage")
                    {
                        _customStorageSwitch,
                        _customStorageDirectoryEntry
                    }
                }
            };
        }

        private void InitializeStorageSettings(IFileRepository fileRepository)
        {
            bool hasCustomStorageDir = fileRepository.StorageDirectory != fileRepository.DefaultStorageDirectory;
            _customStorageSwitch = new SwitchCell
            {
                Text = "Custom storage directory",
                On = hasCustomStorageDir
            };
            _customStorageDirectoryEntry = new EntryCell
            {
                Label = "Path",
                Text = fileRepository.StorageDirectory,
                IsEnabled = hasCustomStorageDir
            };
            _customStorageSwitch.OnChanged += (sender, args) =>
            {
                _customStorageDirectoryEntry.IsEnabled = args.Value;
                if (!args.Value)
                {
                    //reset to default
                    _customStorageDirectoryEntry.Text = fileRepository.DefaultStorageDirectory;
                    SetStorageDir(fileRepository.DefaultStorageDirectory);
                }
            };
            _customStorageDirectoryEntry.Completed += (sender, args) =>
            {
                SetStorageDir(_customStorageDirectoryEntry.Text);
            };
        }

        /// <summary>
        /// set the storage dir to a different value (or null for the default value)
        /// </summary>
        /// <param name="value"></param>
        private async void SetStorageDir(string value)
        {
            try
            {
                if (_fileRepository.StorageDirectory != value)
                {
                    string answer = await DisplayActionSheet("Wiki storage directory changed", "Cancel", null, "Move data to new directory", "Copy data to new directory", "Leave data alone");

                    switch (answer)
                    {
                        case "Cancel":
                            _customStorageDirectoryEntry.Text = _fileRepository.StorageDirectory;
                            return;

                        case "Move data to new directory":
                            await _fileRepository.MoveTo(value);
                            break;

                        case "Copy data to new directory":
                            await _fileRepository.CopyTo(value);
                            break;

                        default:
                            _fileRepository.StorageDirectory = value;
                            break;
                    }

                    if (_fileRepository.DefaultStorageDirectory == value)
                    {
                        value = null;
                        _customStorageSwitch.On = false;
                    }
                    PersistedState.CustomStorageDirectory = value;
                }
            }
            catch (IOException ex)
            {
                await DisplayAlert("Not good", ex.Message, "OK");
            }
        }
    }
}