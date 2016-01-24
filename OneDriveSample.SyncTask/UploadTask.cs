using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Microsoft.OneDrive.Sdk;

namespace OneDriveSample.SyncTask
{
    public sealed class UploadTask : IBackgroundTask
    {
        private readonly string[] Scopes = new[] { "wl.signin", "wl.offline_access", "onedrive.readwrite" };
        private string ClientId = "";

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("RefreshToken"))
            {
                string refreshToken = ApplicationData.Current.LocalSettings.Values["RefreshToken"].ToString();
                string returnUrl = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
                IOneDriveClient client = await OneDriveClient.GetSilentlyAuthenticatedMicrosoftAccountClient(ClientId, returnUrl, Scopes,
                    refreshToken);

                bool isFolderExisting = true;
                try
                {
                    await client.Drive.Root.ItemWithPath("OneDrive Sample").Request().GetAsync();
                }
                catch (OneDriveException exc)
                {
                    isFolderExisting = false;
                }

                if (isFolderExisting)
                {
                    StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("BackgroundWallpaper.jpg");
                    using (Stream stream = await file.OpenStreamForReadAsync())
                    {
                        await client.Drive.Root.ItemWithPath("OneDrive Sample/BackgroundWallpaper.png").Content.Request().PutAsync<Item>(stream);
                    }
                }
            }

            deferral.Complete();
        }
    }
}
