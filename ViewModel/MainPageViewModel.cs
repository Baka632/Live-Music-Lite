using LiveMusicLite.Commands;
using LiveMusicLite.Helper;
using LiveMusicLite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace LiveMusicLite.ViewModel
{
    public class MainPageViewModel : NotificationObject
    {
        /// <summary>
        /// 循环播放的状态
        /// </summary>
        private string RepeatingMusicState = "循环播放:关";
        private string _RepeatButtonIconString = "\uE1CD";
        private string ShufflingMusicState = "随机播放:关";
        /// <summary>
        /// 支持的音频格式数组
        /// </summary>
        private static readonly string[] SupportedAudioFormats = new string[]
        {
            ".mp3", ".wav", ".wma", ".aac", ".adt", ".adts", ".ac3", ".ec3",
        };

        public MusicService MusicService { get; }
        public MusicInfomation MusicInfomation { get; }
        public MediaCommand RepeatMusicCommand { get; }
        public MediaCommand PlayPauseMusicCommand { get; }
        public MediaCommand NextMusicCommand { get; }
        public MediaCommand PreviousMusicCommand { get; }
        public MediaCommand ShuffleMusicCommand { get; }
        public DelegateCommand OpenMusicCommand { get; }
        public DelegateCommand OpenSettingsCommand { get; } = new DelegateCommand()
        {
            ExecuteAction = async (object obj) =>
            {
                SettingsContentDialog settingsContentDialog = new SettingsContentDialog();
                await settingsContentDialog.ShowAsync();
            }
        };
        public FileService FileService { get; }
        public VolumeGlyphState VolumeGlyphState { get; }
        public TileHelper TileHelper { get; } = new TileHelper();

        /// <summary>
        /// 播放器"重复播放"状态的属性
        /// </summary>
        public string RepeatingMusicProperties
        {
            get => RepeatingMusicState;
            set
            {
                RepeatingMusicState = value;
                OnPropertiesChanged();
            }
        }

        /// <summary>
        /// 播放器"循环播放"状态的属性
        /// </summary>
        public string ShufflingMusicProperties
        {
            get => ShufflingMusicState;
            set
            {
                ShufflingMusicState = value;
                OnPropertiesChanged();
            }
        }

        public string RepeatButtonIconString
        {
            get => _RepeatButtonIconString;
            set 
            { 
                _RepeatButtonIconString = value;
                OnPropertiesChanged();
            }
        }

        public MainPageViewModel()
        {
            MusicService = new MusicService();
            MusicInfomation = new MusicInfomation(MusicService);
            RepeatMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Repeat);
            RepeatMusicCommand.CommandExecuted += OnRepeatMusicCommandExecuted;
            PlayPauseMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.PlayAndPause);
            NextMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Next);
            PreviousMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Previous);
            ShuffleMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Shuffle);
            ShuffleMusicCommand.CommandExecuted += OnShuffleMusicCommandExecuted;
            OpenMusicCommand = new DelegateCommand() { ExecuteAction = (object obj) => OpenMusicFile() };
            FileService = new FileService(MusicService);
            VolumeGlyphState = new VolumeGlyphState(MusicService,MusicInfomation);
        }

        private void OnShuffleMusicCommandExecuted(MediaCommandExecutedEventArgs obj)
        {
            if (MusicService.MediaPlaybackList.ShuffleEnabled == true)
            {
                ShufflingMusicProperties = "随机播放:开";
            }
            else
            {
                ShufflingMusicProperties = "随机播放:关";
            }
        }

        private void OnRepeatMusicCommandExecuted(MediaCommandExecutedEventArgs args)
        {
            switch ((bool?)args.Parameter)
            {
                case true:
                    RepeatingMusicProperties = "循环播放:全部循环";
                    break;
                case false:
                    RepeatButtonIconString = "\uE1CD";
                    RepeatingMusicProperties = "循环播放:关闭循环";
                    break;
                case null:
                    RepeatButtonIconString = "\uE1CC";
                    RepeatingMusicProperties = "循环播放:单曲循环";
                    break;
                default:
                    break;
            }
        }

        private async void OpenMusicFile()
        {
            Windows.Storage.Pickers.FileOpenPicker MusicPicker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };
            foreach (string fileExtension in SupportedAudioFormats)
            {
                MusicPicker.FileTypeFilter.Add(fileExtension);
            }

            IReadOnlyList<StorageFile> fileList = await MusicPicker.PickMultipleFilesAsync();

            if (fileList.Count > 0)
            {
                await FileService.GetMusicPropertiesAysnc(fileList);
            }
        }
    }
}
