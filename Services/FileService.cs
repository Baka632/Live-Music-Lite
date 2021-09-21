using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace LiveMusicLite.Services
{
    public class FileService
    {
        public FileService()
        {

        }

        public async Task<string> CreateMusicAlbumCoverFile(string name, IRandomAccessStream stream)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            string fileName = name.Replace(":", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty).Replace("?", string.Empty).Replace("*", string.Empty).Replace("|", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);
            StorageFolder folder = ApplicationData.Current.TemporaryFolder;
            StorageFile file = await folder.CreateFileAsync($"{fileName}.png", CreationCollisionOption.OpenIfExists);

            Stream fileStream = (await file.OpenAsync(FileAccessMode.ReadWrite)).AsStreamForWrite();
            Stream _stream = stream.AsStreamForRead();
            _stream.Seek(0, SeekOrigin.Begin);
            await _stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            fileStream.Dispose();
            return file.Path;
        }

        public async Task<IList<MediaPlaybackItem>> GetMusicPropertiesAysnc(IReadOnlyList<StorageFile> fileList)
        {
            List<MediaPlaybackItem> mediaPlaybackItems = await Task.Run(async () => await operation(fileList));
            return mediaPlaybackItems;

            async Task<List<MediaPlaybackItem>> operation(IReadOnlyList<StorageFile> _fileList)
            {
                List<MediaPlaybackItem> mediaPlaybackItemList = new List<MediaPlaybackItem>(_fileList.Count);
                for (int i = 0; i < _fileList.Count; i++)
                {
                    StorageFile file = _fileList[i];

                    MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                    if (string.IsNullOrWhiteSpace(musicProperties.Artist))
                    {
                        musicProperties.AlbumArtist = "未知艺术家";
                    }

                    if (string.IsNullOrWhiteSpace(musicProperties.Title))
                    {
                        musicProperties.Title = file.Name;
                    }

                    if (string.IsNullOrWhiteSpace(musicProperties.Album))
                    {
                        musicProperties.Album = "未知专辑";
                    }

                    MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(_fileList[i]));
                    MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                    props.Type = Windows.Media.MediaPlaybackType.Music;
                    props.MusicProperties.Title = musicProperties.Title;
                    props.MusicProperties.Artist = musicProperties.Artist;
                    props.MusicProperties.AlbumTitle = musicProperties.Album;
                    props.MusicProperties.TrackNumber = musicProperties.TrackNumber;
                    props.MusicProperties.AlbumArtist = musicProperties.AlbumArtist;
                    foreach (string item in musicProperties.Genre)
                    {
                        props.MusicProperties.Genres.Add(item);
                    }
                    #region Image
                    //下面这三个语句可能有问题
                    //Mp3Stream mp3 = new Mp3Stream(await file.OpenStreamForReadAsync());
                    //var tag2x = mp3.GetTag(Id3TagFamily.Version2x);
                    //var tag1x = mp3.GetTag(Id3TagFamily.Version1x);

                    //HACK: Test only
                    StorageItemThumbnail Thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                    props.Thumbnail = RandomAccessStreamReference.CreateFromStream(Thumbnail);

                    //if (tag2x?.Pictures.Count < 1 && tag1x?.Pictures.Count < 1)
                    //{
                    //    var Thumbnail = await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/NullAlbum.png"))).GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                    //    props.Thumbnail = RandomAccessStreamReference.CreateFromStream(Thumbnail);
                    //}
                    //else
                    //{
                    //    StorageItemThumbnail Thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                    //    props.Thumbnail = RandomAccessStreamReference.CreateFromStream(Thumbnail);
                    //}
                    #endregion
                    mediaPlaybackItem.ApplyDisplayProperties(props);
                    mediaPlaybackItemList.Add(mediaPlaybackItem);
                }
                return mediaPlaybackItemList;
            }
        }

        public async Task GetMusicPropertiesAndPlayAysnc(IReadOnlyList<StorageFile> fileList, MediaPlaybackList playbackList)
        {
            for (int i = 0; i < fileList.Count; i++)
            {
                StorageFile file = fileList[i];

                MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                if (string.IsNullOrWhiteSpace(musicProperties.Artist))
                {
                    musicProperties.AlbumArtist = "未知艺术家";
                }

                if (string.IsNullOrWhiteSpace(musicProperties.Title))
                {
                    musicProperties.Title = file.Name;
                }

                if (string.IsNullOrWhiteSpace(musicProperties.Album))
                {
                    musicProperties.Album = "未知专辑";
                }

                MediaPlaybackItem mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(fileList[i]));
                MediaItemDisplayProperties props = mediaPlaybackItem.GetDisplayProperties();
                props.Type = Windows.Media.MediaPlaybackType.Music;
                props.MusicProperties.Title = musicProperties.Title;
                props.MusicProperties.Artist = musicProperties.Artist;
                props.MusicProperties.AlbumTitle = musicProperties.Album;
                props.MusicProperties.TrackNumber = musicProperties.TrackNumber;
                props.MusicProperties.AlbumArtist = musicProperties.AlbumArtist;
                foreach (string item in musicProperties.Genre)
                {
                    props.MusicProperties.Genres.Add(item);
                }
                #region Image
                //下面这三个语句可能有问题
                //Mp3Stream mp3 = new Mp3Stream(await file.OpenStreamForReadAsync());
                //var tag2x = mp3.GetTag(Id3TagFamily.Version2x);
                //var tag1x = mp3.GetTag(Id3TagFamily.Version1x);

                //HACK: Test only
                StorageItemThumbnail Thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                props.Thumbnail = RandomAccessStreamReference.CreateFromStream(Thumbnail);

                //if (tag2x?.Pictures.Count < 1 && tag1x?.Pictures.Count < 1)
                //{
                //    var Thumbnail = await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/NullAlbum.png"))).GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                //    props.Thumbnail = RandomAccessStreamReference.CreateFromStream(Thumbnail);
                //}
                //else
                //{
                //    StorageItemThumbnail Thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                //    props.Thumbnail = RandomAccessStreamReference.CreateFromStream(Thumbnail);
                //}
                #endregion
                mediaPlaybackItem.ApplyDisplayProperties(props);
                playbackList.Items.Add(mediaPlaybackItem);
            }
        }
    }
}
