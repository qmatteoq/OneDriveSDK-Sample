using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.OneDrive.Sdk;

namespace OneDriveSample
{
    public sealed partial class MainPage : Page
    {
        private readonly string[] Scopes = new[] { "wl.signin", "wl.offline_access", "onedrive.readwrite" };
        private string ClientId = "";
        private string refreshToken;
        private IOneDriveClient _client;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void OnOneDriveLogin(object sender, RoutedEventArgs e)
        {
            _client = OneDriveClientExtensions.GetClientUsingOnlineIdAuthenticator(Scopes);
            await _client.AuthenticateAsync();
        }

        private async void OnOneDriveWebViewLogin(object sender, RoutedEventArgs e)
        {
            _client = OneDriveClientExtensions.GetClientUsingWebAuthenticationBroker(ClientId, Scopes);
            await _client.AuthenticateAsync();
            refreshToken = _client.AuthenticationProvider.CurrentAccountSession.RefreshToken;
            ApplicationData.Current.LocalSettings.Values["RefreshToken"] = refreshToken;
        }

        private async void OnCreateFolder(object sender, RoutedEventArgs e)
        {
            bool isFolderExisting = true;
            try
            {
                await _client.Drive.Root.ItemWithPath("OneDrive Sample").Request().GetAsync();
            }
            catch (OneDriveException exc)
            {
                isFolderExisting = false;
            }

            if (!isFolderExisting)
            {
                Item newItem = new Item
                {
                    Name = "OneDrive Sample",
                    Folder = new Folder()
                };
                await _client.Drive.Root.Children.Request().AddAsync(newItem);
            }
        }

        private async void OnUploadFile(object sender, RoutedEventArgs e)
        {
            StorageFile file = await Package.Current.InstalledLocation.GetFileAsync("Wallpaper.png");
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                await _client.Drive.Root.ItemWithPath("OneDrive Sample/Wallpaper.png").Content.Request().PutAsync<Item>(stream);
            }
        }

        private async void OnDownloadFile(object sender, RoutedEventArgs e)
        {
            bool isFileExisting = true;
            try
            {
                await _client.Drive.Root.ItemWithPath("OneDrive Sample/Wallpaper.png").Request().GetAsync();
            }
            catch (OneDriveException exc)
            {
                isFileExisting = false;
            }

            if (isFileExisting)
            {
                using (Stream stream = await _client.Drive.Root.ItemWithPath("OneDrive Sample/Wallpaper.png").Content.Request().GetAsync())
                {
                    FileSavePicker picker = new FileSavePicker();
                    picker.FileTypeChoices.Add("Pictures", new List<string> { ".png" });
                    picker.SuggestedFileName = "Wallpaper.png";
                    picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                    StorageFile destinationFile = await picker.PickSaveFileAsync();

                    using (Stream destinationStream = await destinationFile.OpenStreamForWriteAsync())
                    {
                        await stream.CopyToAsync(destinationStream);
                        await destinationStream.FlushAsync();
                    }
                }
            }
        }

        private async void OnRegisterTask(object sender, RoutedEventArgs e)
        {
            if (BackgroundTaskRegistration.AllTasks.All(x => x.Value.Name != "SyncTask"))
            {
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder
                {
                    Name = "SyncTask",
                    TaskEntryPoint = "OneDriveSample.SyncTask.UploadTask"
                };

                builder.SetTrigger(new TimeTrigger(60, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status != BackgroundAccessStatus.Denied)
                {
                    builder.Register();
                }
            }
        }
    }
}
