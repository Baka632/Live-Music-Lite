using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace LiveMusicLite
{
    public sealed partial class SettingsContentDialog : ContentDialog, INotifyPropertyChanged
    {
        public SettingsContentDialog()
        {
            this.InitializeComponent();
        }

        private enum ThemeSettings
        {
            Light, Dark, Default
        }

        private string _ThemeSettings = App.settings.ThemeSettings;

        public event PropertyChangedEventHandler PropertyChanged;

        public string AppVersionProperty { get; } = string.Format("{0} {1}.{2}.{3}.{4}",
                    "版本",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision
        );


        public string ThemeSettingProperty
        {
            get
            {
                switch (_ThemeSettings)
                {
                    case "Light":
                        _ThemeSettings = "浅色";
                        break;
                    case "Dark":
                        _ThemeSettings = "深色";
                        break;
                    case "Default":
                        _ThemeSettings = "使用系统设置";
                        break;
                    default:
                        break;
                }
                return _ThemeSettings;
            }
            set
            {
                _ThemeSettings = value;
                OnPropertiesChanged();
            }
        }

        private string _MusicOpenOperationString = App.settings.MediaOpenOperation == true ? "覆盖掉现在的播放列表" : "加入到现在的播放列表";
        private bool IsUserMode = false;
        private string _TempFolderSize;

        public string MusicOpenOperationStringProperty
        {
            get => _MusicOpenOperationString;
            set 
            {
                _MusicOpenOperationString = value;
                OnPropertiesChanged(); 
            }
        }


        public async void OnPropertiesChanged([CallerMemberName] string propertyName = "")
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private void ThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            string Theme = e.AddedItems[0].ToString();
            switch (Theme)
            {
                case "浅色":
                    App.settings.ThemeSettings = ThemeSettings.Light.ToString();
                    ThemeSettingProperty = "浅色";
                    break;
                case "深色":
                    App.settings.ThemeSettings = ThemeSettings.Dark.ToString();
                    ThemeSettingProperty = "深色";
                    break;
                case "使用系统设置":
                    App.settings.ThemeSettings = ThemeSettings.Default.ToString();
                    ThemeSettingProperty = "使用系统设置";
                    break;
            }
            if (IsUserMode == false)
            {
                IsUserMode = true;
            }
            else
            {
                restartStackPanel.Visibility = Visibility.Visible;
            }
        }

        private void MusicOpenOperationChanged(object sender, SelectionChangedEventArgs e)
        {
            string MusicOpenOperation = e.AddedItems[0].ToString();
            switch (MusicOpenOperation)
            {
                case "覆盖掉现在的播放列表":
                    App.settings.MediaOpenOperation = true;
                    MusicOpenOperationStringProperty = "覆盖掉现在的播放列表";
                    break;
                case "加入到现在的播放列表":
                    App.settings.MediaOpenOperation = false;
                    MusicOpenOperationStringProperty = "加入到现在的播放列表";
                    break;
            }
        }

        private async void MailTo(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:stevemc123456@outlook.com"));
        }

        private async void GoToGithub(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/Baka632/Live-Music-Lite"));
        }

        private async void RestartApp(object sender, RoutedEventArgs e)
        {
            AppRestartFailureReason result = await CoreApplication.RequestRestartAsync(String.Empty);
            if (result == AppRestartFailureReason.NotInForeground ||
                result == AppRestartFailureReason.RestartPending ||
                result == AppRestartFailureReason.Other)
            {
                System.Diagnostics.Debug.WriteLine($"RequestRestartAsync failed: {result}");
            }
        }

        public string TempFolderSize
        {
            get => _TempFolderSize;
            set
            {
                _TempFolderSize = value;
                OnPropertiesChanged();
            }
        }

        private async void ClearTempFolder(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            clearProgreeRing.IsActive = true;
            await Task.Run(async () =>
            {
                IReadOnlyList<StorageFile> files = await ApplicationData.Current.TemporaryFolder.GetFilesAsync();
                if (files.Count > 0)
                {
                    foreach (var item in files)
                    {
                        await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
            });
            (sender as Button).IsEnabled = true;
            clearProgreeRing.IsActive = false;
            uint count = 0;
            await Task.Run(async () =>
            {
                count = await ApplicationData.Current.TemporaryFolder.CreateFileQuery().GetItemCountAsync();
            });
            TempFolderSize = $"缓存的图片数:{count}";
        }

        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            uint count = 0;
            await Task.Run(async () =>
            {
                count = await ApplicationData.Current.TemporaryFolder.CreateFileQuery().GetItemCountAsync();
            });
            TempFolderSize = $"缓存的图片数:{count}";
        }
    }
}
