using LiveMusicLite.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.Storage;
using Windows.UI.Notifications;

namespace LiveMusicLite.Helper
{
    public class TileHelper
    {
        /// <summary>
        /// 显示磁贴
        /// </summary>
        /// <param name="songTitle">歌曲标题</param>
        /// <param name="albumArtistName">歌曲专辑艺术家名称</param>
        /// <param name="albumName">专辑名</param>
        /// <param name="showNextSong">指示是否显示下一首歌曲的消息的值</param>
        /// <param name="nextSongTitle">下一首歌曲标题</param>
        /// <param name="nextSongAlbumArtistName">下一首歌曲专辑艺术家名称</param>
        /// <param name="nextSongAlbumName">下一首专辑名</param>
        public async void ShowTitle(string songTitle, string albumArtistName, string albumName, bool showNextSong, string nextSongTitle = null, string nextSongAlbumArtistName = null, string nextSongAlbumName = null)
        {
            TileContent tileContent;
            string albumFileName = $"{albumName.Replace(":", string.Empty).Replace(" / ", string.Empty).Replace("\\", string.Empty).Replace(" ? ", string.Empty).Replace(" * ", string.Empty).Replace(" | ", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty)}";
            string imagePath = $"{ApplicationData.Current.TemporaryFolder.Path}\\{albumFileName}.png";
            if (albumName == "未知专辑")
            {
                imagePath = (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/NullAlbum.png"))).Path;
            }

            if (showNextSong)
            {
                tileContent = new TileContent()
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
                                        Text = songTitle,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumArtistName,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumName,
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
                                        Text = songTitle,
                                        HintStyle = AdaptiveTextStyle.Subtitle,
                                        HintMaxLines = 1,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumArtistName,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumName,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    }
                                },
                                PeekImage = new TilePeekImage()
                                {
                                    Source = $"{ApplicationData.Current.TemporaryFolder.Path}\\{albumFileName}.jpg"
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
                                            Text = songTitle,
                                            HintStyle = AdaptiveTextStyle.Title,
                                            HintMaxLines = 2,
                                            HintWrap = true,
                                            HintAlign = AdaptiveTextAlign.Left
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = albumArtistName,
                                            HintWrap = true
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = albumName,
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = ""
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = nextSongTitle,
                                            HintWrap = true,
                                            HintAlign = AdaptiveTextAlign.Left
                                        },
                                        new AdaptiveText()
                                        {
                                        Text = nextSongAlbumArtistName,
                                        HintWrap = true
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = nextSongAlbumName,
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
            }
            else
            {
                tileContent = new TileContent()
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
                                        Text = songTitle,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumArtistName,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumName,
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
                                        Text = songTitle,
                                        HintStyle = AdaptiveTextStyle.Subtitle,
                                        HintMaxLines = 1,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumArtistName,
                                        HintMaxLines = 1,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumName,
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
                                        Text = songTitle,
                                        HintStyle = AdaptiveTextStyle.Title,
                                        HintMaxLines = 2,
                                        HintWrap = true,
                                        HintAlign = AdaptiveTextAlign.Left
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumArtistName,
                                        HintWrap = true
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = albumName,
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
            }

            // Create the tile notification
            TileNotification tileNotif = new TileNotification(tileContent.GetXml());
            try
            {
                // And send the notification to the primary tile
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("磁贴显示出现了故障,这真是令人尴尬");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// 删除磁贴
        /// </summary>
        public void DeleteTile()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }
    }
}
