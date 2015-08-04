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
            get
            {
                var result = new UserLogin();
                result.Token = GetValue("UserLogin.Token");
                result.Secret = GetValue("UserLogin.Secret");
                return result;
            }
            set
            {
                var token = default(string);
                var secret = default(string);
                if (value != null)
                {
                    token = value.Token;
                    secret = value.Secret;
                }
                SetValue("UserLogin.Token", token);
                SetValue("UserLogin.Secret", secret);
            }
        }

        public static int SyncInterval
        {
            get
            {
                int result;
                if (!int.TryParse(GetValue("SyncInterval"), out result))
                {
                    //10 minutes = default
                    return 10;
                }
                return result;
            }
            set { SetValue("SyncInterval", value.ToString()); }
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