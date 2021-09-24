using LiveMusicLite.Commands;
using LiveMusicLite.Helper;
using LiveMusicLite.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

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
        private string NowPlayingState = "暂停";
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
        public MediaCommand StopMusicCommand { get; set; }
        public MediaCommand PlayPauseMusicCommand { get; }
        public MediaCommand NextMusicCommand { get; }
        public MediaCommand PreviousMusicCommand { get; }
        public MediaCommand MuteCommand { get; set; }
        public MediaCommand ShuffleMusicCommand { get; }
        public DelegateCommand OpenAndPlayMusicCommand { get; }
        public DelegateCommand OpenSettingsCommand { get; } = new DelegateCommand()
        {
            ExecuteAction = async (object obj) =>
            {
                SettingsContentDialog settingsContentDialog = new SettingsContentDialog();
                await settingsContentDialog.ShowAsync();
            }
        };
        public VolumeGlyphState VolumeGlyphState { get; }
        public TileHelper TileHelper { get; } = new TileHelper();
        private FileService FileService { get; }

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

        /// <summary>
        /// 播放器"正在播放"状态的属性
        /// </summary>
        public string NowPlayingProperties
        {
            get => NowPlayingState;
            set
            {
                NowPlayingState = value;
                OnPropertiesChangedUsingMainThread();
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

        private bool _IsMediaControlShow;

        public bool IsMediaControlShow
        {
            get => _IsMediaControlShow;
            set
            {
                _IsMediaControlShow = value;
                OnPropertiesChanged();
                OnPropertiesChanged(nameof(IsMediaControlShowReverse));
            }
        }

        public bool IsMediaControlShowReverse
        {
            get => !IsMediaControlShow;
        }

        private string _PausePlayingButtonIcon = "\uE102";

        public string PausePlayingButtonIcon
        {
            get => _PausePlayingButtonIcon;
            set
            {
                _PausePlayingButtonIcon = value;
                OnPropertiesChangedUsingMainThread();
            }
        }

        private string _TimeTextBlockText;

        public string TimeTextBlockText
        {
            get => _TimeTextBlockText;
            set
            {
                _TimeTextBlockText = value;
                OnPropertiesChangedUsingMainThread();
            }
        }

        private double _ProcessSliderValue;

        public double ProcessSliderValue
        {
            get => _ProcessSliderValue;
            set
            {
                _ProcessSliderValue = value;
                OnPropertiesChangedUsingMainThread();
            }
        }


        public MainPageViewModel()
        {
            MusicService = new MusicService();
            MusicInfomation = new MusicInfomation(MusicService);
            MuteCommand = new MediaCommand(MusicService, Models.MediaCommandType.Mute);
            RepeatMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Repeat);
            RepeatMusicCommand.CommandExecuted += OnRepeatMusicCommandExecuted;
            PlayPauseMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.PlayAndPause);
            NextMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Next);
            PreviousMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Previous);
            ShuffleMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Shuffle);
            StopMusicCommand = new MediaCommand(MusicService, Models.MediaCommandType.Stop)
            {
                ExecuteAction = (object obj) =>
                {
                    IsMediaControlShow = false;
                    MusicService.StopMusic();
                    MusicInfomation.ResetAllMusicProperties();
                    TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                },
                CanExecuteFunc = (object obj) =>
                {
                    return IsMediaControlShow;
                }
            };
            ShuffleMusicCommand.CommandExecuted += OnShuffleMusicCommandExecuted;
            OpenAndPlayMusicCommand = new DelegateCommand() { ExecuteAction = async (object obj) => await OpenAndPlayMusicFile(obj) };
            FileService = new FileService();
            VolumeGlyphState = new VolumeGlyphState(MusicService, MusicInfomation);

            MusicService.MediaPlaybackList.CurrentItemChanged += OnCurrentItemChanged;
            MusicService.MediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            MusicService.MediaPlayer.PlaybackSession.NaturalDurationChanged += OnNaturalDurationChanged;
            MusicService.MediaPlayer.PlaybackSession.PositionChanged += OnPositionChanged;
        }

        private void OnPositionChanged(MediaPlaybackSession sender, object args)
        {
            ProcessSliderValue = MusicService.MediaPlayer.PlaybackSession.Position.TotalSeconds;
            TimeTextBlockText = MusicService.MediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
        }

        private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
        {
            MusicInfomation.MusicLenthProperties = sender.NaturalDuration.ToString(@"m\:ss");
            MusicInfomation.MusicDurationProperties = sender.NaturalDuration.TotalSeconds;
        }

        private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    NowPlayingProperties = "暂停";
                    PausePlayingButtonIcon = "\uE103";
                    break;
                case MediaPlaybackState.Paused:
                    NowPlayingProperties = "播放";
                    PausePlayingButtonIcon = "\uE102";
                    break;
                case MediaPlaybackState.None:
                case MediaPlaybackState.Opening:
                case MediaPlaybackState.Buffering:
                default:
                    break;
            }
        }

        private async void OnCurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (MusicService.MediaPlaybackList.CurrentItem != null)
            {
                MusicDisplayProperties currentMusicInfomation = MusicService.MediaPlaybackList.CurrentItem.GetDisplayProperties().MusicProperties;
                RandomAccessStreamReference CurrentItemMusicThumbnail = sender.CurrentItem.GetDisplayProperties().Thumbnail;
                IRandomAccessStream musicThumbnail = await CurrentItemMusicThumbnail.OpenReadAsync();
                MusicInfomation.MusicAlbumArtistProperties = currentMusicInfomation.AlbumArtist;
                MusicInfomation.MusicTitleProperties = currentMusicInfomation.Title;
                MusicInfomation.MusicAlbumProperties = currentMusicInfomation.AlbumTitle;
                await RunOnMainThread(async () => await WorkOnMainThread(musicThumbnail));
                _ = await FileService.CreateMusicAlbumCoverFile(currentMusicInfomation.AlbumTitle, musicThumbnail);
                SetTileSource();
            }
            else
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }

            async Task WorkOnMainThread(IRandomAccessStream musicThumbnail)
            {
                BitmapImage image = new BitmapImage();
                await image.SetSourceAsync(musicThumbnail);
                MusicInfomation.MusicImageProperties = image;
            }
        }

        private void OnShuffleMusicCommandExecuted(MediaCommandExecutedEventArgs obj)
        {
            ShufflingMusicProperties = MusicService.MediaPlaybackList.ShuffleEnabled ? "随机播放:开" : "随机播放:关";
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

        private async Task OpenAndPlayMusicFile(object storageFile = null)
        {
            bool IsUsingFilePicker = false;
            IReadOnlyList<StorageFile> fileList;
            if (storageFile is IReadOnlyList<StorageFile> storageFiles)
            {
                fileList = storageFiles;
            }
            else
            {
                IsUsingFilePicker = true;
                Windows.Storage.Pickers.FileOpenPicker MusicPicker = new Windows.Storage.Pickers.FileOpenPicker
                {
                    ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
                };
                foreach (string fileExtension in SupportedAudioFormats)
                {
                    MusicPicker.FileTypeFilter.Add(fileExtension);
                }

                fileList = await MusicPicker.PickMultipleFilesAsync();
            }

            if (fileList.Count > 0)
            {
                if (IsMediaControlShow != true)
                {
                    IsMediaControlShow = true;
                }

                if (Settings.MediaOpenOperation && IsUsingFilePicker != true)
                {
                    MusicInfomation.ResetAllMusicProperties();
                    if (MusicService.MediaPlaybackList.Items != null)
                    {
                        MusicService.StopMusic();
                    }
                    await FileService.GetMusicPropertiesAndPlayAysnc(fileList, MusicService.MediaPlaybackList);
                }
                else
                {
                    await FileService.GetMusicPropertiesAndPlayAysnc(fileList, MusicService.MediaPlaybackList);
                }
            }
        }

        /// <summary>
        /// 设置磁贴的源
        /// </summary>
        private async void SetTileSource()
        {
            string album = $"{MusicInfomation.MusicAlbumProperties.Replace(":", string.Empty).Replace(" / ", string.Empty).Replace("\\", string.Empty).Replace(" ? ", string.Empty).Replace(" * ", string.Empty).Replace(" | ", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty)}";
            string imagePath = $"{ApplicationData.Current.TemporaryFolder.Path}\\{album}.jpg";
            if (album == "未知专辑")
            {
                imagePath = (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/NullAlbum.png"))).Path;
            }
            if (MusicService.MediaPlaybackList.Items.Count > MusicService.MediaPlaybackList.CurrentItemIndex + 1)
            {
                int index = (int)(MusicService.MediaPlaybackList.CurrentItemIndex + 1);
                Windows.Media.MusicDisplayProperties MusicProps = MusicService.MediaPlaybackList.Items[index].GetDisplayProperties().MusicProperties;
                var tileContent = new TileContent()
                {
                    Visual = new TileVisual()
                    {
                        TileSmall = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                BackgroundImage = new TileBackgroundImage()
                                {
                                    Source = imagePath
                                }
                            }
                        },
                        TileMedium = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicTitleProperties,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumArtistProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = imagePath
                                }
                            }
                        },
                        TileWide = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicTitleProperties,
                                        HintStyle = AdaptiveTextStyle.Subtitle,
                                        HintMaxLines = 1,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumArtistProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{album}.jpg"
                                }
                            }
                        },
                        TileLarge = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                Children =
                                    {
                                        new AdaptiveText()
                                        {
                                            Text = MusicInfomation.MusicTitleProperties,
                                            HintStyle = AdaptiveTextStyle.Title,
                                            HintMaxLines = 2,
                                            HintWrap = true,
                                            HintAlign = AdaptiveTextAlign.Left
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = MusicInfomation.MusicAlbumArtistProperties,
                                            HintWrap = true
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = MusicInfomation.MusicAlbumProperties,
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = ""
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = MusicProps.Title,
                                            HintWrap = true,
                                            HintAlign = AdaptiveTextAlign.Left
                                        },
                                        new AdaptiveText()
                                        {
                                        Text = MusicProps.AlbumArtist,
                                        HintWrap = true
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = MusicProps.AlbumTitle,
                                            HintWrap = true
                                        }
                                    },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = imagePath
                                }
                            }
                        }
                    }
                };
                TileHelper.ShowTitle(tileContent);
            }
            else
            {
                var tileContent = new TileContent()
                {
                    Visual = new TileVisual()
                    {
                        #region TheSameToBefore
                        TileSmall = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                BackgroundImage = new TileBackgroundImage()
                                {
                                    Source = imagePath
                                }
                            }
                        },
                        TileMedium = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicTitleProperties,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumArtistProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = imagePath
                                }
                            }
                        },
                        TileWide = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicTitleProperties,
                                        HintStyle = AdaptiveTextStyle.Subtitle,
                                        HintMaxLines = 1,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumArtistProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = imagePath
                                }
                            }
                        },
                        #endregion

                        TileLarge = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicTitleProperties,
                                        HintStyle = AdaptiveTextStyle.Title,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumArtistProperties,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = MusicInfomation.MusicAlbumProperties,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = imagePath
                                }
                            }
                        }
                    }
                };
                TileHelper.ShowTitle(tileContent);
            }
        }

        private async Task RunOnMainThread(DispatchedHandler handler, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            await Dispatcher.RunAsync(priority, handler);
        }

    }
}
