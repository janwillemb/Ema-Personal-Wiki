using System.Threading.Tasks;
using DropNetRT;
using DropNetRT.Models;
using EmaXamarin.Api;

namespace EmaXamarin.CloudStorage
{
    public class DropboxUserPermission
    {
        private DropNetClient _client;
        private const string AppKey = "l8tliwhtfvkrxl7";
        private const string AppSecret = "lfh5rpahsdhrqbp";

        public async void AskUserForPermission(IExternalBrowserService externalBrowserService)
        {
            _client = new DropNetClient(AppKey, AppSecret);
            var token = await _client.GetRequestToken();
            var url = _client.BuildAuthorizeUrl(token);

            externalBrowserService.OpenUrl(url);
        }

        public async Task<UserLogin> VerifiedUserPermission()
        {
            var userLogin = await _client.GetAccessToken();
            return userLogin;
        }

        public static DropNetClient GetAuthenticatedClient(UserLogin login)
        {
            return new DropNetClient(AppKey, AppSecret, login.Token, login.Secret);
        }
    }
}