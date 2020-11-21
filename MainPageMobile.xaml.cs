﻿using System;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace LiveMusicLite
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPageMobile : Page , INotifyPropertyChanged
    {
        public MainPageMobile()
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
            Task GetMusicTask = Task.Run(() => GetMusicImages());
        }
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
        /// 新的进度条值
        /// </summary>
        public static double SliderNewValue;
        /// <summary>
        /// 指示鼠标指针是否在拖动进度条的值
        /// </summary>
        public static bool IsPointerEntered = false;
        /// <summary>
        /// SMTC显示属性的实例
        /// </summary>
        MediaItemDisplayProperties props;
        /// <summary>
        /// 声音图标状态的实例
        /// </summary>
        VolumeGlyphState volumeGlyphState = App.volumeGlyphState;
        /// <summary>
        /// 磁贴助手的实例
        /// </summary>
        TileHelper tileHelper = new TileHelper();
        int TotalIndex = 0;

        /// <summary>
        /// 指示是否从启动以来第一次添加音乐
        /// </summary>
        bool IsFirstTimeAddMusic = true;

        /// <summary>
        /// 音乐缩略图的列表
        /// </summary>
        Dictionary<int, BitmapImage> MusicImageList = new Dictionary<int, BitmapImage>();
        /// <summary>
        /// 音乐属性的列表
        /// </summary>
        Dictionary<int, MusicProperties> MusicPropertiesList = new Dictionary<int, MusicProperties>();
        /// <summary>
        /// 播放器支持的音乐文件格式的数组
        /// </summary>
        private static readonly string[] supportedAudioFormats = new string[]
        {
            ".mp3", ".wav", ".wma",".3g2", ".3gp2", ".3gp", ".3gpp", ".m4a", ".asf", ".aac", ".adt", ".adts", ".ac3", ".ec3",
        };

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
        private void ShuffleMusic(object sender, RoutedEventArgs e) => musicService.ShuffleMusic();

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
                    break;
                case false:
                    musicService.RepeatMusic(true);
                    repeatMusicButton.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE1CD", FontSize = 16 };
                    break;
                case null:
                    musicService.RepeatMusic(null);
                    repeatMusicButton.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE1CC", FontSize = 16 };
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
                int CurrentItemIndex = (int)musicService.mediaPlaybackList.CurrentItemIndex;

                musicInfomation.MusicAlbumArtistProperties = MusicPropertiesList[CurrentItemIndex].AlbumArtist;
                musicInfomation.MusicTitleProperties = MusicPropertiesList[CurrentItemIndex].Title;
                musicInfomation.MusicImageProperties = MusicImageList[CurrentItemIndex];
                musicInfomation.MusicLenthProperties = MusicPropertiesList[CurrentItemIndex].Duration.ToString(@"m\:ss");
                musicInfomation.MusicDurationProperties = MusicPropertiesList[CurrentItemIndex].Duration.TotalSeconds;
                musicInfomation.MusicAlbumProperties = MusicPropertiesList[CurrentItemIndex].Album;

                StorageFile storageFile = await ApplicationData.Current.TemporaryFolder.GetFileAsync($"{musicInfomation.MusicAlbumProperties.Replace(":", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty).Replace("?", string.Empty).Replace("*", string.Empty).Replace("|", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty)}.jpg");
                props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = Windows.Media.MediaPlaybackType.Music;
                props.MusicProperties.AlbumTitle = musicInfomation.MusicAlbumProperties;
                props.MusicProperties.AlbumArtist = musicInfomation.MusicAlbumArtistProperties;
                props.MusicProperties.Title = musicInfomation.MusicTitleProperties;
                props.MusicProperties.Artist = musicInfomation.MusicAlbumArtistProperties;
                props.MusicProperties.AlbumTrackCount = MusicPropertiesList[CurrentItemIndex].TrackNumber;
                props.MusicProperties.TrackNumber = MusicPropertiesList[CurrentItemIndex].TrackNumber;
                props.Thumbnail = RandomAccessStreamReference.CreateFromFile(storageFile);
                mediaPlaybackItem.ApplyDisplayProperties(props);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    processSlider.Maximum = musicInfomation.MusicDurationProperties;
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
            TotalIndex = 0;
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
                    TotalIndex = 0;
                    ResetMusicPropertiesList();
                    if (musicService.mediaPlaybackList.Items != null)
                    {
                        musicService.mediaPlaybackList.Items.Clear();
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
            for (int i = 0; i < fileList.Count; i++)
            {
                mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(fileList[i]));
                musicService.mediaPlaybackList.Items.Add(mediaPlaybackItem);
            }

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

            for (int i = 0; i < fileList.Count; i++)
            {
                StorageFile file = fileList[i];

                Task<MusicProperties> musicPropertiesTask = file.Properties.GetMusicPropertiesAsync().AsTask();
                musicPropertiesTask.Wait();
                MusicProperties musicProperties = musicPropertiesTask.Result;

                if (string.IsNullOrWhiteSpace(musicProperties.Artist) == true)
                {
                    musicProperties.AlbumArtist = "未知艺术家";
                }

                if (string.IsNullOrWhiteSpace(musicProperties.Title) == true)
                {
                    musicProperties.Title = file.Name;
                }

                if (string.IsNullOrWhiteSpace(musicProperties.Album) == true)
                {
                    musicProperties.Album = "未知专辑";
                }

                MusicPropertiesList.Add(TotalIndex, musicProperties);

                Task<StorageItemThumbnail> musicThumbnailTask = file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem).AsTask();
                musicThumbnailTask.Wait();

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(musicThumbnailTask.Result);

                MusicImageList.Add(TotalIndex, bitmapImage);

                string AlbumSaveName = musicProperties.Album;
                AlbumSaveName = AlbumSaveName.Replace(":", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty).Replace("?", string.Empty).Replace("*", string.Empty).Replace("|", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);

                using (var fileStream = File.Create($"{ApplicationData.Current.TemporaryFolder.Path}\\{AlbumSaveName}.jpg"))
                {
                    await WindowsRuntimeStreamExtensions.AsStreamForRead(musicThumbnailTask.Result.GetInputStreamAt(0)).CopyToAsync(fileStream);
                }
                TotalIndex++;
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
            if (e.Parameter is IReadOnlyList<StorageFile> file && e.Parameter != null)
            {
                OpenMusicFile(file, App.settings.MediaOpenOperation);
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
            MusicPropertiesList.Clear();
            MusicImageList.Clear();
        }

        private async void GetMusicImages()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                int x = 0;
                StorageFolder musicFolder = KnownFolders.MusicLibrary;
                IReadOnlyList<StorageFile> list = await musicFolder.GetFilesAsync(CommonFileQuery.OrderByName);
                List<string> albumList = new List<string>();
                for (int i = 0; i < list.Count; x++)
                {
                    if (i == 30)
                    {
                        break;
                    }
                    else
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
                        {
                            MusicProperties musicProperties = await list[x].Properties.GetMusicPropertiesAsync();
                            if (albumList.Contains(musicProperties.Album) == false && list[x].ContentType == "audio/mpeg" || list[x].ContentType == "audio/x-wav")
                            {
                                StorageFile file = list[x];
                                var thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                                _ = await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
                                randomAccessStream.Seek(0);
                                await bitmapImage.SetSourceAsync(randomAccessStream);
                                Image image = new Image
                                {
                                    Source = bitmapImage,
                                    Stretch = Stretch.UniformToFill,
                                    Height = 100,
                                    Width = 100,
                                };
                                backgroundImagesGridView.Items.Add(image);
                                albumList.Add(musicProperties.Album);
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
            string album = $"{musicInfomation.MusicAlbumProperties.Replace(":", string.Empty).Replace(" / ", string.Empty).Replace("\\", string.Empty).Replace(" ? ", string.Empty).Replace(" * ", string.Empty).Replace(" | ", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty)}";
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
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
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{album}.jpg"
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
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{album}.jpg"
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
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{album}.jpg"
                                }
                            }
                        }
                    }
                };

                tileHelper.ShowTitle(tileContent);
            });
        }

        private class TileHelper
        {
            /// <summary>
            /// 显示磁贴
            /// </summary>
            /// <param name="tileContent">磁贴源</param>
            public void ShowTitle(TileContent tileContent)
            {
                // Create the tile notification
                var tileNotif = new TileNotification(tileContent.GetXml());

                // And send the notification to the primary tile
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
            }

            /// <summary>
            /// 删除磁贴
            /// </summary>
            public void DeleteTile() => TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }

        private async void ShowSettings(object sender, RoutedEventArgs e)
        {
            SettingsContentDialog settingsContentDialog = new SettingsContentDialog();
            await settingsContentDialog.ShowAsync();
        }
    }
}
