using DropNetRT.Models;
using Refractored.Xam.Settings;

namespace EmaXamarin
{
    public static class PersistedState
    {
        public static string AutoSaveEditText
        {
            get { return GetValue("AutoSaveEditText"); }
            set { SetValue("AutoSaveEditText", value); }
        }

        public static string PageInEditMode
        {
            get { return GetValue("PageInEditMode"); }
            set { SetValue("PageInEditMode", value); }
        }

        public static string CustomStorageDirectory
        {
            get { return GetValue("CustomStorageDirectory"); }
            set { SetValue("CustomStorageDirectory", value); }
        }

        public static UserLogin UserLogin
        {
            get { return CrossSettings.Current.GetValueOrDefault("UserLogin", new UserLogin()); }
            set { CrossSettings.Current.AddOrUpdateValue("UserLogin", value); }
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