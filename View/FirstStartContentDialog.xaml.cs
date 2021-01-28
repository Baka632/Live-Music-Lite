using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class FirstStartContentDialog : ContentDialog
    {
        private enum ThemeSettings
        {
            Light, Dark, Default
        }

        public FirstStartContentDialog()
        {
            this.InitializeComponent();
        }

        private void ThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            string Theme = e.AddedItems[0].ToString();
            switch (Theme)
            {
                case "浅色":
                    App.settings.ThemeSettings = ThemeSettings.Light.ToString();
                    break;
                case "深色":
                    App.settings.ThemeSettings = ThemeSettings.Dark.ToString();
                    break;
                case "使用系统设置":
                    App.settings.ThemeSettings = ThemeSettings.Default.ToString();
                    break;
            }
        }

        private void MusicOpenOperationChanged(object sender, SelectionChangedEventArgs e)
        {
            string MusicOpenOperation = e.AddedItems[0].ToString();
            switch (MusicOpenOperation)
            {
                case "覆盖掉现在的播放列表":
                    App.settings.MediaOpenOperation = true;
                    break;
                case "加入到现在的播放列表":
                    App.settings.MediaOpenOperation = false;
                    break;
            }
        }
    }
}
