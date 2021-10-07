using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveMusicLite.ViewModel;
using Windows.Media.Playback;
using Windows.UI.Xaml;

namespace LiveMusicLite.Services
{
    /// <summary>
    /// 为音乐播放及音乐播放列表提供类和方法
    /// </summary>
    public class MusicService : NotificationObject, IDisposable
    {
        public MediaPlayer MediaPlayer { get; } = new MediaPlayer();

        public MediaPlaybackList MediaPlaybackList { get; } = new MediaPlaybackList();

        private MediaPlaybackState _MediaPlaybackState;

        public MediaPlaybackState MediaPlaybackState
        {
            get => _MediaPlaybackState;
            set
            {
                _MediaPlaybackState = value;
                OnPropertiesChangedUsingMainThread();
            }
        }

        private bool IsFirstTimeOpenMusic = true;

        /// <summary>
        /// 初始化MusicService类的新实例
        /// </summary>
        public MusicService()
        {
            MediaPlayer.AutoPlay = true;
            MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
            MediaPlaybackList.MaxPlayedItemsToKeepOpen = 5;
            MediaPlaybackList.Items.VectorChanged += OnMediaPlayBackListItemsChanged;
            MediaPlayer.Source = MediaPlaybackList;
            MediaPlayer.PlaybackSession.PlaybackStateChanged += OnCurrentStateChanged;
            MediaPlaybackState = MediaPlayer.PlaybackSession.PlaybackState;
        }

        private void OnMediaPlayBackListItemsChanged(Windows.Foundation.Collections.IObservableVector<MediaPlaybackItem> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            if (IsFirstTimeOpenMusic)
            {
                if (MediaPlaybackState != MediaPlaybackState.Playing)
                {
                    PlayMusic();
                }
                IsFirstTimeOpenMusic = false;
            }
        }

        private void OnCurrentStateChanged(MediaPlaybackSession session, object args)
        {
            MediaPlaybackState = session.PlaybackState;
        }

        /// <summary>
        /// 终止音乐播放
        /// </summary>
        public void StopMusic()
        {
            MediaPlayer.Pause();
            MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            MediaPlaybackList.Items.Clear();
            IsFirstTimeOpenMusic = true;
        }

        /// <summary>
        /// 设置播放器的音量
        /// </summary>
        /// <param name="Volume">将要设置的音量大小,其值范围应在0和1之间,其余值将受到限制</param>
        public void SetMusicPlayerVolume(double Volume)
        {
            MediaPlayer.Volume = Volume;
        }

        /// <summary>
        /// 切换到上一个播放项
        /// </summary>
        public void PreviousMusic()
        {
            MediaPlaybackList.MovePrevious();
        }

        /// <summary>
        /// 切换到下一个播放项
        /// </summary>
        public void NextMusic()
        {
            MediaPlaybackList.MoveNext();
        }

        /// <summary>
        /// 改变播放状态,如果现在的状态为播放,则切换到暂停状态,反之亦然
        /// </summary>
        public void PlayPauseMusic()
        {
            switch (MediaPlayer.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.None:
                    MediaPlayer.Play();
                    break;
                case MediaPlaybackState.Opening:
                    break;
                case MediaPlaybackState.Buffering:
                    break;
                case MediaPlaybackState.Playing:
                    MediaPlayer.Pause();
                    break;
                case MediaPlaybackState.Paused:
                    MediaPlayer.Play();
                    break;
                default:
                    break;
            }
        }

        public void PlayMusic()
        {
            MediaPlayer.Play();
        }

        /// <summary>
        /// 控制是否随机播放音乐,如果现在的状态为false,则改为true,反之亦然
        /// </summary>
        public void ShuffleMusic()
        {
            if (MediaPlaybackList.ShuffleEnabled)
            {
                MediaPlaybackList.ShuffleEnabled = false;
            }
            else
            {
                MediaPlaybackList.ShuffleEnabled = true;
            }
        }

        /// <summary>
        /// 控制是否循环播放
        /// </summary>
        /// <param name="IsRepeating">用于判断是否执行某些操作的值,若值为true,则关闭循环播放,若为false,则启用全部循环,若为null,则会单曲循环</param>
        public void RepeatMusic(bool? IsRepeating)
        {
            switch (IsRepeating)
            {
                case true:
                    MediaPlaybackList.AutoRepeatEnabled = false;
                    MediaPlayer.IsLoopingEnabled = false;
                    break;
                case false:
                    MediaPlaybackList.AutoRepeatEnabled = true;
                    break;
                case null:
                    MediaPlaybackList.AutoRepeatEnabled = true;
                    MediaPlayer.IsLoopingEnabled = true;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 异步添加多个音乐
        /// </summary>
        /// <param name="items">要添加的音乐项</param>
        /// <returns></returns>
        public async Task AddMusicAsync(IList<MediaPlaybackItem> items)
        {
            await Task.Run(() => AddItems(items));

            void AddItems(IList<MediaPlaybackItem> mediaPlaybackItems)
            {
                foreach (MediaPlaybackItem item in mediaPlaybackItems)
                {
                    MediaPlaybackList.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// 异步添加音乐
        /// </summary>
        /// <param name="item">要添加的音乐项</param>
        /// <returns></returns>
        public async Task AddMusicAsync(MediaPlaybackItem item)
        {
            await Task.Run(() => MediaPlaybackList.Items.Add(item));
        }

        /// <summary>
        /// 设置播放器的播放倍数
        /// </summary>
        /// <param name="rate">播放倍数</param>
        public void SetMediaPlayerPlayRate(double rate)
        {
            MediaPlayer.PlaybackSession.PlaybackRate = rate;
        }

        public void Dispose()
        {
            ((IDisposable)MediaPlayer).Dispose();
        }
    }
}
