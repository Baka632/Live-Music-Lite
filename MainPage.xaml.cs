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
        MusicInfomation musicInfomation = App.musicInfomation;
        /// <summary>
        /// 音乐服务的实例
        /// </summary>
        MusicService musicService = App.musicService;
        /// <summary>
        /// 单个媒体播放项
        /// </summary>
        MediaPlaybackItem mediaPlaybackItem;
        /// <summary>
        /// 计时器DispatcherTimer的实例
        /// </summary>
        public static DispatcherTimer dispatcherTimer;
        /// <summary>
        /// 从文件读取的音乐属性
        /// </summary>
        MusicProperties musicProperties;
        /// <summary>
        /// 新的进度条值
        /// </summary>
        public static double SliderNewValue;
        /// <summary>
        /// 指示鼠标指针是否在拖动进度条的值
        /// </summary>
        public static bool IsPointerEntered = false;
        /// <summary>
        /// 用于保存专辑缩略图的文件名
        /// </summary>
        string AlbumSaveName = "";
        /// <summary>
        /// SMTC显示属性的实例
        /// </summary>
        MediaItemDisplayProperties props;
        /// <summary>
        /// 声音图标状态的实例
        /// </summary>
        VolumeGlyphState volumeGlyphState = App.volumeGlyphState;

        /// <summary>
        /// 指示是否从启动以来第一次添加音乐
        /// </summary>
        bool IsFirstTimeAddMusic = true;
        /// <summary>
        /// 正在播放的状态
        /// </summary>
        string NowPlayingState = "播放";
        /// <summary>
        /// 循环播放的状态
        /// </summary>
        string RepeatingMusicState = "循环播放:关";
        /// <summary>
        /// 随机播放的状态
        /// </summary>
        string ShufflingMusicState = "随机播放:关";

        /// <summary>
        /// 音乐艺术家的列表
        /// </summary>
        Dictionary<int, string> MusicArtistList = new Dictionary<int, string>();
        /// <summary>
        /// 音乐标题的列表
        /// </summary>
        Dictionary<int, string> MusicTitleList = new Dictionary<int, string>();
        /// <summary>
        /// 音乐缩略图的列表
        /// </summary>
        Dictionary<int, BitmapImage> MusicImageList = new Dictionary<int, BitmapImage>();
        /// <summary>
        /// 音乐缩略图主题色的列表
        /// </summary>
        Dictionary<int, Color> MusicGirdColorsList = new Dictionary<int, Color>();
        /// <summary>
        /// 音乐长度的列表
        /// </summary>
        Dictionary<int, string> MusicLenthList = new Dictionary<int, string>();
        /// <summary>
        /// 音乐实际长度的列表(未被转换为string)
        /// </summary>
        Dictionary<int, double> MusicDurationList = new Dictionary<int, double>();
        /// <summary>
        /// 音乐专辑名称的列表
        /// </summary>
        Dictionary<int, string> MusicAlbumList = new Dictionary<int, string>();
        ObservableCollection<BitmapImage> bitmapImages = new ObservableCollection<BitmapImage>();

        /// <summary>
        /// 初始化MainPage类的新实例
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            pausePlayingButton.IsEnabled = false;
            stopPlayingButton.IsEnabled = false;

            musicService.mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
            musicService.mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            mediaControlStackPanel.Visibility = Visibility.Collapsed;
            musicProcessStackPanel.Visibility = Visibility.Collapsed;

            NavigationCacheMode = NavigationCacheMode.Required;

            processSlider.IsEnabled = false;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            processSlider.AddHandler(UIElement.PointerReleasedEvent /*哪个事件*/, new PointerEventHandler(UIElement_OnPointerReleased) /*使用哪个函数处理*/, true /*如果在之前处理，是否还使用函数*/);
            processSlider.AddHandler(UIElement.PointerPressedEvent /*哪个事件*/, new PointerEventHandler(UIElement_EnterPressedReleased) /*使用哪个函数处理*/, true /*如果在之前处理，是否还使用函数*/);
            GetMusicImages();
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
            musicService.mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SliderNewValue);
            musicNowPlayingTimeTextBlock.Text = musicService.mediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
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
                processSlider.Value = musicService.mediaPlayer.PlaybackSession.Position.TotalSeconds;
                musicNowPlayingTimeTextBlock.Text = musicService.mediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
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
        private void MuteMusic(object sender, RoutedEventArgs e) => musicService.mediaPlayer.IsMuted = (musicService.mediaPlayer.IsMuted == false);

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
        /// 切换到上一个音乐
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviousMusic(object sender, RoutedEventArgs e) => musicService.PreviousMusic();

        /// <summary>
        /// 切换到下一个音乐
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextMusic(object sender, RoutedEventArgs e) => musicService.NextMusic();

        /// <summary>
        /// 改变播放器的"随机播放"属性
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShuffleMusic(object sender, RoutedEventArgs e)
        {
            musicService.ShuffleMusic();
            if (musicService.mediaPlaybackList.ShuffleEnabled == true)
            {
                ShufflingMusicProperties = "随机播放:开";
            }
            else
            {
                ShufflingMusicProperties = "随机播放:关";
            }
        }

        /// <summary>
        /// 改变播放器"重复播放"的属性
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepeatMusic(object sender, RoutedEventArgs e)
        {
            switch (repeatMusicButton.IsChecked)
            {
                case true:
                    musicService.RepeatMusic(false);
                    RepeatingMusicProperties = "循环播放:全部循环";
                    break;
                case false:
                    musicService.RepeatMusic(true);
                    repeatMusicButton.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE1CD", FontSize = 16 };
                    RepeatingMusicProperties = "循环播放:关闭循环";
                    break;
                case null:
                    musicService.RepeatMusic(null);
                    repeatMusicButton.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE1CC", FontSize = 16 };
                    RepeatingMusicProperties = "循环播放:单曲循环";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 当播放器播放状态发生改变时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (musicService.mediaPlayer.PlaybackSession.PlaybackState)
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
                        if (musicService.mediaPlaybackList.Items.Count == musicService.mediaPlaybackList.CurrentItemIndex + 1 && (int)processSlider.Value == (int)processSlider.Maximum)
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
        /// 改变播放器播放的状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayPauseMusic(object sender, RoutedEventArgs e) => musicService.PlayPauseMusic();

        /// <summary>
        /// 当媒体播放列表的当前播放项目发生更改时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (musicService.mediaPlaybackList.CurrentItem != null)
            {
                int CurrentItemHashCode = musicService.mediaPlaybackList.CurrentItem.GetHashCode();

                musicInfomation.MusicAlbumArtistProperties = MusicArtistList[CurrentItemHashCode];
                musicInfomation.MusicTitleProperties = MusicTitleList[CurrentItemHashCode];
                musicInfomation.MusicImageProperties = MusicImageList[CurrentItemHashCode];
                musicInfomation.MusicLenthProperties = MusicLenthList[CurrentItemHashCode];
                musicInfomation.MusicDurationProperties = MusicDurationList[CurrentItemHashCode];
                musicInfomation.MusicAlbumProperties = MusicAlbumList[CurrentItemHashCode];

                StorageFile storageFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync($"{AlbumSaveName}.jpg");
                props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = Windows.Media.MediaPlaybackType.Music;
                props.MusicProperties.Title = musicInfomation.MusicTitleProperties;
                props.MusicProperties.Artist = musicInfomation.MusicAlbumArtistProperties;
                props.Thumbnail = RandomAccessStreamReference.CreateFromFile(storageFile);
                props.MusicProperties.AlbumTitle = musicInfomation.MusicAlbumProperties;
                mediaPlaybackItem.ApplyDisplayProperties(props);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    processSlider.Maximum = musicInfomation.MusicDurationProperties;
                    processSlider.IsEnabled = true;
                    processSlider.Value = 0;
                    musicNowPlayingTimeTextBlock.Text = "0:00";
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
        private void StopMusic(object sender, RoutedEventArgs e)
        {
            musicProcessStackPanel.Visibility = Visibility.Collapsed;
            noneMusicStackPanel.Visibility = Visibility.Visible;
            backgroundImagesGridView.Visibility = Visibility.Visible;
            stopPlayingButton.IsEnabled = false;
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            ChangeMusicControlButtonsUsableState();
            ResetMusicPropertiesList();
            musicService.StopMusic();
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
        /// 播放器"重复播放"状态的属性
        /// </summary>
        private string RepeatingMusicProperties
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
        private string ShufflingMusicProperties
        {
            get => ShufflingMusicState;
            set
            {
                ShufflingMusicState = value;
                OnPropertiesChanged();
            }
        }

        /// <summary>
        /// 直接打开音乐文件来播放音乐(应用外)
        /// </summary>
        /// <param name="file">传入的文件</param>
        /// <param name="IsOverwrite">是否覆盖当前的播放列表</param>
        public void OpenMusicFile(StorageFile file ,bool IsOverwrite = true)
        {
            if (IsOverwrite == true)
            {
                if (file != null)
                {
                    ResetMusicPropertiesList();
                    if (musicService.mediaPlaybackList.Items != null)
                    {
                        musicService.mediaPlaybackList.Items.Clear();
                    }
                    PlayAndGetMusicProperites(file);
                    mediaControlStackPanel.Visibility = Visibility.Visible;
                    musicProcessStackPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (file != null)
                {
                    PlayAndGetMusicProperites(file);
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
            MusicPicker.FileTypeFilter.Add(".mp3");
            MusicPicker.FileTypeFilter.Add(".wav");
            MusicPicker.FileTypeFilter.Add(".wma"); // TODO: Add more file type

            StorageFile file = await MusicPicker.PickSingleFileAsync();

            if (file != null)
            {
                PlayAndGetMusicProperites(file);
                mediaControlStackPanel.Visibility = Visibility.Visible;
                musicProcessStackPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 播放音乐,以及获取音乐的属性
        /// </summary>
        /// <param name="file">传入的音乐文件</param>
        private async void PlayAndGetMusicProperites(StorageFile file)
        {
            mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file));
            musicService.mediaPlaybackList.Items.Add(mediaPlaybackItem);

            int mediaPlayBackItemHashCode = mediaPlaybackItem.GetHashCode();

            musicProperties = await file.Properties.GetMusicPropertiesAsync();
            if (string.IsNullOrWhiteSpace(musicProperties.Artist) != true)
            {
                MusicArtistList.Add(mediaPlayBackItemHashCode, musicProperties.AlbumArtist);
            }
            else
            {
                MusicArtistList.Add(mediaPlayBackItemHashCode, "未知艺术家");
            }

            if (string.IsNullOrWhiteSpace(musicProperties.Title) != true)
            {
                MusicTitleList.Add(mediaPlayBackItemHashCode, musicProperties.Title);
            }
            else
            {
                MusicTitleList.Add(mediaPlayBackItemHashCode, file.Name);
            }

            if (string.IsNullOrWhiteSpace(musicProperties.Album) != true)
            {
                MusicAlbumList.Add(mediaPlayBackItemHashCode, musicProperties.Album);
            }
            else
            {
                MusicAlbumList.Add(mediaPlayBackItemHashCode, "未知专辑");
            }

            BitmapImage bitmapImage = new BitmapImage();
            using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
            {
                var thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
                randomAccessStream.Seek(0);
                await bitmapImage.SetSourceAsync(randomAccessStream);
                MusicImageList.Add(mediaPlayBackItemHashCode, bitmapImage);
                MusicLenthList.Add(mediaPlayBackItemHashCode, musicProperties.Duration.ToString(@"m\:ss"));
                MusicDurationList.Add(mediaPlayBackItemHashCode, musicProperties.Duration.TotalSeconds);

                if (IsFirstTimeAddMusic == true)
                {
                    musicService.mediaPlayer.Source = musicService.mediaPlaybackList;
                    musicService.mediaPlayer.Play();
                    ChangeMusicControlButtonsUsableState();
                    IsFirstTimeAddMusic = false;
                }
                if (musicService.mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None || musicService.mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
                {
                    musicService.mediaPlayer.Play();
                }

                AlbumSaveName = musicProperties.Album;
                AlbumSaveName = AlbumSaveName.Replace(":", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty).Replace("?", string.Empty).Replace("*", string.Empty).Replace("|", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);

                using (var fileStream = File.Create($"{ApplicationData.Current.TemporaryFolder.Path}\\{AlbumSaveName}.jpg"))
                {
                    await WindowsRuntimeStreamExtensions.AsStreamForRead(thumbnail.GetInputStreamAt(0)).CopyToAsync(fileStream);
                }
            }
            noneMusicStackPanel.Visibility = Visibility.Collapsed;
            backgroundImagesGridView.Visibility = Visibility.Collapsed;
            SetTileSource();
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
            musicInfomation.MusicVolumeProperties = e.NewValue / 100;
            if (musicService.mediaPlayer.IsMuted == true)
            {
                musicService.mediaPlayer.IsMuted = false;
            }
        }

        /// <summary>
        /// 重写的OnNavigatedTo方法
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is StorageFile file && e.Parameter != null)
            {
                OpenMusicFile(file,App.settings.MediaOpenOperation);
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
            musicInfomation.ResetAllMusicProperties();
            MusicGirdColorsList.Clear();
            MusicArtistList.Clear();
            MusicImageList.Clear();
            MusicTitleList.Clear();
            MusicLenthList.Clear();
            MusicDurationList.Clear();
            MusicAlbumList.Clear();
        }

        private async void GetMusicImages()
        {
            StorageFolder musicFolder = KnownFolders.MusicLibrary;
            IReadOnlyList<StorageFile> list = await musicFolder.GetFilesAsync(CommonFileQuery.OrderByName);
            List<string> albumList = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == 420)
                {
                    break;
                }
                else
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                    MusicProperties musicProperties = await list[i].Properties.GetMusicPropertiesAsync();
                    if (albumList.Contains(musicProperties.Album) == false && list[i].ContentType == "audio/mpeg" || list[i].ContentType == "audio/x-wav")
                    {
                        StorageFile file = list[i];
                        var thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                        await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
                        randomAccessStream.Seek(0);
                        await bitmapImage.SetSourceAsync(randomAccessStream);
                        bitmapImages.Add(bitmapImage);
                        Image image = new Image
                        {
                            Source = bitmapImage,
                            Stretch = Stretch.UniformToFill,
                            Height = 100,
                            Width = 100,
                        };
                        backgroundImagesGridView.Items.Add(image);
                        albumList.Add(musicProperties.Album);
                    }
                }
            }
        }

        /// <summary>
        /// 设置磁贴的源
        /// </summary>
        private async void SetTileSource()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TileContent tileContent = new TileContent()
                {
                    Visual = new TileVisual()
                    {
                        TileSmall = new TileBinding()
                        {
                            Content = new TileBindingContentAdaptive()
                            {
                                BackgroundImage = new TileBackgroundImage()
                                {
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{AlbumSaveName}.jpg"
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
                                        Text = musicInfomation.MusicTitleProperties,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = musicInfomation.MusicAlbumArtistProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = musicInfomation.MusicAlbumProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{AlbumSaveName}.jpg"
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
                                        Text = musicInfomation.MusicTitleProperties,
                                        HintStyle = AdaptiveTextStyle.Subtitle,
                                        HintMaxLines = 1,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = musicInfomation.MusicAlbumArtistProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = musicInfomation.MusicAlbumProperties,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{AlbumSaveName}.jpg"
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
                                        Text = musicInfomation.MusicTitleProperties,
                                        HintStyle = AdaptiveTextStyle.Title,
                                        HintMaxLines = 3,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = musicInfomation.MusicAlbumArtistProperties,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = musicInfomation.MusicAlbumProperties,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{AlbumSaveName}.jpg"
                                }
                            }
                        }
                    }
                };

                // Create the tile notification
                var tileNotif = new TileNotification(tileContent.GetXml());

                // And send the notification to the primary tile
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
            });
        }
    }
}
