using Refractored.Xam.Settings;
using Xamarin.Forms;

namespace EmaXamarin
{
    public static class PersistedState
    {
        public static string AutoSaveEditText
        {
            get { return GetValue("AutoSaveEditText"); }
            set { SetValue("AutoSaveEditText", value); }
        }

        public static string CustomStorageDirectory
        {
            get { return GetValue("CustomStorageDirectory"); }
            set { SetValue("CustomStorageDirectory", value); }
        }
        private static void SetValue(string key, string value)
        {
            CrossSettings.Current.AddOrUpdateValue(key, value);
        }

        private static string GetValue(string key)
        {
            return CrossSettings.Current.GetValueOrDefault(key, string.Empty);
        }
    }
}