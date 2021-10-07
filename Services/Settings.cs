using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace LiveMusicLite.Services
{
    /// <summary>
    /// 为应用程序的设置提供属性
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// 访问本地设置的实例
        /// </summary>
        static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        /// <summary>
        /// 主题设置的值
        /// </summary>
        static string _ThemeSettings;
        /// <summary>
        /// 音量大小的值
        /// </summary>
        static double _MusicVolume;
        /// <summary>
        /// 指示是否在启动时加载音乐列表的值
        /// </summary>
        static bool _IsLoadMusicOnStartUp;
        /// <summary>
        /// 指示在手动打开音乐时选择何种操作的值
        /// </summary>
        static bool _MediaOpenOperation;
        static bool _IsShuffleEnabled;

        /// <summary>
        /// 初始化Settings类的新实例
        /// </summary>
        static Settings()
        {
            switch ((string)localSettings.Values["ThemeSetting"])
            {
                case null:
                    _ThemeSettings = "Default";
                    localSettings.Values["ThemeSetting"] = "Default";
                    break;
                default:
                    _ThemeSettings = (string)localSettings.Values["ThemeSetting"];
                    break;
            }
            switch (localSettings.Values["MusicVolume"])
            {
                case null:
                    _MusicVolume = 1d;
                    localSettings.Values["MusicVolume"] = 1d;
                    break;
                default:
                    _MusicVolume = (double)localSettings.Values["MusicVolume"];
                    break;
            }
            switch (localSettings.Values["IsLoadMusicOnStartUp"])
            {
                case null:
                    _IsLoadMusicOnStartUp = false;
                    localSettings.Values["IsLoadMusicOnStartUp"] = false;
                    break;
                default:
                    _IsLoadMusicOnStartUp = (bool)localSettings.Values["IsLoadMusicOnStartUp"];
                    break;
            }
            switch (localSettings.Values["MediaOpenOperation"])
            {
                case null:
                    _MediaOpenOperation = true;
                    localSettings.Values["MediaOpenOperation"] = true;
                    break;
                default:
                    _MediaOpenOperation = (bool)localSettings.Values["MediaOpenOperation"];
                    break;
            }
            switch (localSettings.Values["IsShuffleEnabled"])
            {
                case null:
                    _IsShuffleEnabled = false;
                    localSettings.Values["IsShuffleEnabled"] = false;
                    break;
                default:
                    _IsShuffleEnabled = (bool)localSettings.Values["IsShuffleEnabled"];
                    break;
            }
        }

        /// <summary>
        /// 设置中主题设置的属性
        /// </summary>
        public static string ThemeSettings
        {
            get => _ThemeSettings;
            set
            {
                _ThemeSettings = value;
                localSettings.Values["ThemeSetting"] = value;
            }
        }

        /// <summary>
        /// 设置中音乐大小的属性
        /// </summary>
        public static double MusicVolume
        {
            get => _MusicVolume;
            set
            {
                _MusicVolume = value;
                localSettings.Values["MusicVolume"] = value;
            }
        }

        /// <summary>
        /// 设置中是否加载音乐列表的设置的属性
        /// </summary>
        public static bool IsLoadMusicOnStartUp
        {
            get => _IsLoadMusicOnStartUp;
            set
            {
                _IsLoadMusicOnStartUp = value;
                localSettings.Values["IsLoadMusicOnStartUp"] = value;
            }
        }

        /// <summary>
        /// 设置中手动打开音乐文件操作的属性
        /// </summary>
        public static bool MediaOpenOperation
        {
            get => _MediaOpenOperation;
            set
            {
                _MediaOpenOperation = value;
                localSettings.Values["MediaOpenOperation"] = value;
            }
        }

        /// <summary>
        /// 设置中是否随机播放音乐的属性
        /// </summary>
        public static bool IsShuffleEnabled
        {
            get => _IsShuffleEnabled;
            set
            {
                _IsShuffleEnabled = value;
                localSettings.Values["IsShuffleEnabled"] = value;
            }
        }
    }
}
