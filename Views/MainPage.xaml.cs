using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using Id3;
using LiveMusicLite.ViewModel;
using LiveMusicLite.Services;
using LiveMusicLite.Helper;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LiveMusicLite
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 音乐信息的实例
        /// </summary>
        MusicInfomation MusicInfomation;
        /// <summary>
        /// 音乐服务的实例
        /// </summary>
        MusicService MusicService;
        /// <summary>
        /// 文件服务的实例
        /// </summary>
        FileService FileService;
        /// <summary>
        /// 计时器DispatcherTimer的实例
        /// </summary>
        DispatcherTimer dispatcherTimer;
        /// <summary>
        /// 在主界面上显示的音乐图片的列表
        /// </summary>
        ObservableCollection<Image> musicImages = new ObservableCollection<Image>();

        /// <summary>
        /// 新的进度条值
        /// </summary>
        public static double SliderNewValue;
        /// <summary>
        /// 指示鼠标指针是否在拖动进度条的值
        /// </summary>
        public static bool IsPointerEntered = false;
        /// <summary>
        /// 声音图标状态的实例
        /// </summary>
        VolumeGlyphState volumeGlyphState;
        /// <summary>
        /// 磁贴助手的实例
        /// </summary>
        TileHelper tileHelper;

        /// <summary>
        /// 指示是否从启动以来第一次添加音乐
        /// </summary>
        bool IsFirstTimeAddMusic = true;
        /// <summary>
        /// 正在播放的状态
        /// </summary>
        string NowPlayingState = "播放";

        /// <summary>
        /// 支持的音频格式数组
        /// </summary>
        private static string[] supportedAudioFormats = new string[]
        {
            ".mp3", ".wav", ".wma", ".aac", ".adt", ".adts", ".ac3", ".ec3",
        };

        private MainPageViewModel ViewModel { get; }

        /// <summary>
        /// 初始化MainPage类的新实例
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MainPageViewModel();
            MusicService = ViewModel.MusicService;
            MusicInfomation = ViewModel.MusicInfomation;
            FileService = ViewModel.FileService;
            volumeGlyphState = ViewModel.VolumeGlyphState;
            tileHelper = ViewModel.TileHelper;
            App.MainPageViewModel = ViewModel;

            pausePlayingButton.IsEnabled = false;
            stopPlayingButton.IsEnabled = false;

            MusicService.MediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
            MusicService.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            MusicService.MediaPlayer.PlaybackSession.NaturalDurationChanged += PlaybackSession_NaturalDurationChanged;
            mediaControlStackPanel.Visibility = Visibility.Collapsed;
            musicProcessStackPanel.Visibility = Visibility.Collapsed;

            NavigationCacheMode = NavigationCacheMode.Required;

            processSlider.IsEnabled = false;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            processSlider.AddHandler(PointerReleasedEvent, new PointerEventHandler(UIElement_OnPointerReleased), true);
            processSlider.AddHandler(PointerPressedEvent, new PointerEventHandler(UIElement_EnterPressedReleased), true);
        }

        private void PlaybackSession_NaturalDurationChanged(MediaPlaybackSession sender, object args)
        {
            MusicInfomation.MusicLenthProperties = sender.NaturalDuration.ToString(@"m\:ss");
            MusicInfomation.MusicDurationProperties = sender.NaturalDuration.TotalSeconds;
        }

        /// <summary>
        /// 开始拖拽进度条时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElement_EnterPressedReleased(object sender, PointerRoutedEventArgs e) => IsPointerEntered = true;

        /// <summary>
        /// 结束拖拽进度条时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void UIElement_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            MusicService.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SliderNewValue);
            musicNowPlayingTimeTextBlock.Text = MusicService.MediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
            IsPointerEntered = false;
        }

        /// <summary>
        /// 当超过计时器间隔时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, object e)
        {
            if (IsPointerEntered == false)
            {
                processSlider.Value = MusicService.MediaPlayer.PlaybackSession.Position.TotalSeconds;
                musicNowPlayingTimeTextBlock.Text = MusicService.MediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
            }
        }

        /// <summary>
        /// 通知系统属性已经发生更改
        /// </summary>
        /// <param name="propertyName">发生更改的属性名称,其填充是自动完成的</param>
        public async void OnPropertiesChanged([CallerMemberName] string propertyName = "")
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        /// <summary>
        /// 改变播放器的静音状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteMusic(object sender, RoutedEventArgs e) => MusicService.MediaPlayer.IsMuted = (MusicService.MediaPlayer.IsMuted == false);

        /// <summary>
        /// 当进度条被拖动时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void processSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SliderNewValue = e.NewValue;
            if (IsPointerEntered)
            {
                musicNowPlayingTimeTextBlock.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"m\:ss");
            }
        }

        /// <summary>
        /// 当播放器播放状态发生改变时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (MusicService.MediaPlayer.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        pausePlayingButton.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE103", FontSize = 27 };
                        NowPlayingProperties = "暂停";
                        if (dispatcherTimer.IsEnabled == false)
                        {
                            dispatcherTimer.Start();
                        }
                    });
                    break;
                case MediaPlaybackState.Paused:
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        pausePlayingButton.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE102", FontSize = 27 };
                        NowPlayingProperties = "播放";
                        if (MusicService.MediaPlaybackList.Items.Count == MusicService.MediaPlaybackList.CurrentItemIndex + 1 && (int)processSlider.Value == (int)processSlider.Maximum)
                        {
                            dispatcherTimer.Stop();
                            musicNowPlayingTimeTextBlock.Text = "0:00";
                            processSlider.Value = 0;
                        }
                    });
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 当媒体播放列表的当前播放项目发生更改时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (MusicService.MediaPlaybackList.CurrentItem != null)
            {
                Windows.Media.MusicDisplayProperties currentMusicInfomation = MusicService.MediaPlaybackList.CurrentItem.GetDisplayProperties().MusicProperties;
                RandomAccessStreamReference CurrentItemMusicThumbnail = sender.CurrentItem.GetDisplayProperties().Thumbnail;
                IRandomAccessStream musicThumbnail = await CurrentItemMusicThumbnail.OpenReadAsync();

                MusicInfomation.MusicAlbumArtistProperties = currentMusicInfomation.AlbumArtist;
                MusicInfomation.MusicTitleProperties = currentMusicInfomation.Title;
                MusicInfomation.MusicAlbumProperties = currentMusicInfomation.AlbumTitle;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(musicThumbnail);
                    MusicInfomation.MusicImageProperties = image;

                    processSlider.IsEnabled = true;
                    processSlider.Value = 0;
                    musicNowPlayingTimeTextBlock.Text = "0:00";
                    SetTileSource();
                    dispatcherTimer.Start();
                });
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    dispatcherTimer.Stop();
                    TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                });
            }
        }

        /// <summary>
        /// 终止播放音乐
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopMusic(object sender = null, RoutedEventArgs e = null)
        {
            musicProcessStackPanel.Visibility = Visibility.Collapsed;
            noneMusicStackPanel.Visibility = Visibility.Visible;
            backgroundImagesGridView.Visibility = Visibility.Visible;
            stopPlayingButton.IsEnabled = false;
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            ChangeMusicControlButtonsUsableState();
            ResetMusicPropertiesList();
            MusicService.StopMusic();
        }

        /// <summary>
        /// 播放器"正在播放"状态的属性
        /// </summary>
        private string NowPlayingProperties
        {
            get => NowPlayingState;
            set
            {
                NowPlayingState = value;
                OnPropertiesChanged();
            }
        }

        /// <summary>
        /// 直接打开音乐文件来播放音乐(应用外)
        /// </summary>
        /// <param name="fileList">传入的文件</param>
        /// <param name="IsOverwrite">是否覆盖当前的播放列表</param>
        public void OpenMusicFile(IReadOnlyList<StorageFile> fileList, bool IsOverwrite = true)
        {
            if (IsOverwrite == true)
            {
                if (fileList.Count > 0)
                {
                    ResetMusicPropertiesList();
                    if (MusicService.MediaPlaybackList.Items != null)
                    {
                        MusicService.MediaPlaybackList.Items.Clear();
                    }
                    PlayAndGetMusicProperites(fileList);
                    mediaControlStackPanel.Visibility = Visibility.Visible;
                    musicProcessStackPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (fileList.Count > 0)
                {
                    PlayAndGetMusicProperites(fileList);
                    mediaControlStackPanel.Visibility = Visibility.Visible;
                    musicProcessStackPanel.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 手动打开音乐文件(应用内)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenMusicFile(object sender, RoutedEventArgs e)
        {
            var MusicPicker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };
            foreach (string fileExtension in supportedAudioFormats)
            {
                MusicPicker.FileTypeFilter.Add(fileExtension);
            }

            IReadOnlyList<StorageFile> fileList = await MusicPicker.PickMultipleFilesAsync();

            if (fileList.Count > 0)
            {
                PlayAndGetMusicProperites(fileList);
                mediaControlStackPanel.Visibility = Visibility.Visible;
                musicProcessStackPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 播放音乐,以及获取音乐的属性
        /// </summary>
        /// <param name="file">传入的音乐文件</param>
        private async void PlayAndGetMusicProperites(IReadOnlyList<StorageFile> fileList)
        {
            backgroundImagesGridView.Visibility = Visibility.Collapsed;
            noneMusicStackPanel.Visibility = Visibility.Collapsed;

            await FileService.GetMusicPropertiesAysnc(fileList);
            if (IsFirstTimeAddMusic == true)
            {
                MusicService.MediaPlayer.Source = MusicService.MediaPlaybackList;
                ChangeMusicControlButtonsUsableState();
                IsFirstTimeAddMusic = false;
            }
            else
            {
                MusicService.MediaPlayer.Play();
            }
        }

        /// <summary>
        /// 改变控制音乐播放的按钮的可用性
        /// </summary>
        private void ChangeMusicControlButtonsUsableState()
        {
            if (pausePlayingButton.IsEnabled == nextMusicButton.IsEnabled == previousMusicButton.IsEnabled == false)
            {
                pausePlayingButton.IsEnabled = true;
                nextMusicButton.IsEnabled = true;
                previousMusicButton.IsEnabled = true;
                stopPlayingButton.IsEnabled = true;
            }
            else
            {
                stopPlayingButton.IsEnabled = false;
                pausePlayingButton.IsEnabled = false;
                nextMusicButton.IsEnabled = false;
                previousMusicButton.IsEnabled = false;
                IsFirstTimeAddMusic = true;
            }
        }

        /// <summary>
        /// 当volumeSlider的值发生改变时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            MusicInfomation.MusicVolumeProperties = e.NewValue / 100;
            if (MusicService.MediaPlayer.IsMuted == true)
            {
                MusicService.MediaPlayer.IsMuted = false;
            }
        }

        /// <summary>
        /// 重写的OnNavigatedTo方法
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is IReadOnlyList<StorageFile> file && e.Parameter != null)
            {
                OpenMusicFile(file, Settings.MediaOpenOperation);
            }
            else
            {
                //Do nothing
            }
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// 重置音乐属性的列表
        /// </summary>
        private void ResetMusicPropertiesList()
        {
            MusicInfomation.ResetAllMusicProperties();
        }

        private async Task GetMusicImages()
        {
            int x = 0;
            StorageFolder musicFolder = KnownFolders.MusicLibrary;
            List<StorageFile> list =
                (
                    from StorageFile file
                    in await musicFolder.GetFilesAsync(CommonFileQuery.OrderByName)
                    where file.ContentType == "audio/mpeg" || file.ContentType == "audio/x-wav"
                    select file
                ).AsParallel().ToList();
            List<string> albumList = new List<string>();

            await Task.Run(async () =>
            {
                for (int i = 0; i < list.Count; x++)
                {
                    if (i == 20 && Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                    {
                        break;
                    }
                    else if (i == 72)
                    {
                        break;
                    }
                    else
                    {
                        using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
                        {
                            MusicProperties musicProperties = await list[x].Properties.GetMusicPropertiesAsync();
                            if (albumList.Contains(musicProperties.Album) == false)
                            {
                                StorageFile file = list[x];
                                var thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                                await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
                                randomAccessStream.Seek(0);
                                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                                 {
                                     BitmapImage bitmapImage = new BitmapImage();
                                     await bitmapImage.SetSourceAsync(randomAccessStream);
                                     Image image = new Image
                                     {
                                         Source = bitmapImage,
                                         Stretch = Stretch.UniformToFill,
                                         Height = 100,
                                         Width = 100,
                                     };
                                     musicImages.Add(image);
                                     albumList.Add(musicProperties.Album);
                                 });
                                i++;
                            }
                        }
                    }
                }
            });
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
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
                    tileHelper.ShowTitle(tileContent);
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

                    tileHelper.ShowTitle(tileContent);
                }
            });
        }
    }
}
