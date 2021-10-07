using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LiveMusicLite.Services
{
    public class VolumeGlyphState : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _volumeGlyph;
        readonly MusicInfomation MusicInfomation;
        readonly MusicService MusicService;

        public VolumeGlyphState(MusicService musicServiceArgs,MusicInfomation musicInfomationArgs)
        {
            MusicService = musicServiceArgs;
            MusicInfomation = musicInfomationArgs;
            ChangeVolumeGlyph();
            MusicService.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
            MusicService.MediaPlayer.IsMutedChanged += MediaPlayer_IsMutedChanged;
        }

        private void MediaPlayer_IsMutedChanged(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            switch (MusicService.MediaPlayer.IsMuted)
            {
                case true:
                    VolumeGlyph = "\uE198";
                    break;
                case false:
                    ChangeVolumeGlyph();
                    break;
            }
        }

        private void MediaPlayer_VolumeChanged(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            if (MusicService.MediaPlayer.IsMuted != true)
            {
                ChangeVolumeGlyph();
            }
        }

        public void ChangeVolumeGlyph()
        {
            double MediaPlayerVolume = MusicService.MusicVolumeProperties;
            if (MediaPlayerVolume > 0.6)
            {
                VolumeGlyph = "\uE995";
            }
            else if (MediaPlayerVolume > 0.3 && MediaPlayerVolume < 0.6)
            {
                VolumeGlyph = "\uE994";
            }
            else if (MediaPlayerVolume > 0 && MediaPlayerVolume < 0.3)
            {
                VolumeGlyph = "\uE993";
            }
            else if (MediaPlayerVolume == 0)
            {
                VolumeGlyph = "\uE992";
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

        public string VolumeGlyph
        {
            get => _volumeGlyph;
            set
            {
                _volumeGlyph = value;
                OnPropertiesChanged();
            }
        }
    }
}