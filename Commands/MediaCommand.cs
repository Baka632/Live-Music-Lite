﻿using LiveMusicLite.Models;
using LiveMusicLite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveMusicLite.Commands
{
    public class MediaCommand : DelegateCommand
    {
        public event Action<MediaCommandExecutedEventArgs> CommandExecuted;

        public MediaCommand(MusicService musicService, MediaCommandType type)
        {
            switch (type)
            {
                case MediaCommandType.PlayAndPause:
                    ExecuteAction = (object obj) => musicService.PlayPauseMusic();
                    break;
                case MediaCommandType.Next:
                    ExecuteAction = (object obj) => musicService.NextMusic();
                    break;
                case MediaCommandType.Previous:
                    ExecuteAction = (object obj) => musicService.PreviousMusic();
                    break;
                case MediaCommandType.Stop:
                    ExecuteAction = (object obj) => musicService.StopMusic();
                    break;
                case MediaCommandType.Repeat:
                    ExecuteAction = (object obj) =>
                    {
                        bool? parameter = false;
                        switch (obj as bool?)
                        {
                            case true:
                                parameter = false;
                                break;
                            case false:
                                parameter = true;
                                break;
                            case null:
                                parameter = null;
                                break;
                            default:
                                break;
                        }
                        musicService.RepeatMusic(parameter);
                    };
                    break;
                case MediaCommandType.Shuffle:
                    ExecuteAction = (object obj) => musicService.ShuffleMusic();
                    break;
                case MediaCommandType.Mute:
                    ExecuteAction = (object obj) => musicService.MediaPlayer.IsMuted = musicService.MediaPlayer.IsMuted == false;
                    break;
                default:
                    break;
            }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);
            CommandExecuted?.Invoke(new MediaCommandExecutedEventArgs() { Parameter = parameter });
        }
    }
}
