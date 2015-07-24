using Android.App;
using Android.Content;
using Android.Net;
using EmaXamarin.Api;

namespace EmaXamarin.Droid
{
    public class ExternalBrowserService : IExternalBrowserService
    {
        public void OpenUrl(string url)
        {
            var intent = new Intent(Intent.ActionView, Uri.Parse(url));
            intent.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(intent);
        }
    }
}