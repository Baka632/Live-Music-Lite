using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using LiveMusicLite.ViewModel;
using LiveMusicLite.Services;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas;
using Windows.UI.Xaml.Media.Animation;

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
        private readonly MusicInfomation MusicInfomation;

        /// <summary>
        /// 音乐服务的实例
        /// </summary>
        private readonly MusicService MusicService;

        /// <summary>
        /// 在主界面上显示的音乐图片的列表
        /// </summary>
        private readonly ObservableCollection<BitmapImage> musicImages = new ObservableCollection<BitmapImage>();

        private Storyboard _scrollAnimation;
        private bool IsTextScrolling = false;
        private bool AllowTextScrolling = false;
        private HorizontalAlignment _musicNameInnerStackPanelHorizontalAlignment = HorizontalAlignment.Center;
        internal MainPageViewModel ViewModel { get; }

        /// <summary>
        /// 初始化MainPage类的新实例
        /// </summary>
        public MainPage()
        {
            ViewModel = new MainPageViewModel();
            this.InitializeComponent();
            MusicService = ViewModel.MusicService;
            MusicInfomation = ViewModel.MusicInfomation;
            App.MainPageViewModel = ViewModel;

            MusicService.MediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;

            processSlider.AddHandler(PointerReleasedEvent, new PointerEventHandler(UIElement_OnPointerReleased), true);
            processSlider.AddHandler(PointerPressedEvent, new PointerEventHandler(UIElement_EnterPressedReleased), true);
        }



        public HorizontalAlignment musicNameInnerStackPanelHorizontalAlignment
        {
            get => _musicNameInnerStackPanelHorizontalAlignment;
            set
            {
                _musicNameInnerStackPanelHorizontalAlignment = value;
                OnPropertiesChanged();
            }
        }

        private async void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (_scrollAnimation != null)
            {
                IsTextScrolling = false;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _scrollAnimation.Stop());
            }
        }

        /// <summary>
        /// 开始拖拽进度条时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElement_EnterPressedReleased(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.OnProgressSliderEnterPressedReleased();
        }

        /// <summary>
        /// 结束拖拽进度条时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void UIElement_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.OnProgressSliderPointerReleased();
        }

        /// <summary>
        /// 当进度条被拖动时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void processSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ViewModel.OnProcessSliderValueChanged(e);
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
        /// 当volumeSlider的值发生改变时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            MusicService.MusicVolumeProperties = e.NewValue / 100;
            if (MusicService.MediaPlayer.IsMuted)
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
            base.OnNavigatedTo(e);
            if (e.Parameter is IReadOnlyList<StorageFile> file)
            {
                ViewModel.OpenAndPlayMusicCommand.Execute(file);
            }
        }

        private string HandleText(string text)
        {
            CanvasTextFormat textFormat = new CanvasTextFormat
            {
                FontSize = (float)musicName.FontSize,
                FontFamily = musicName.FontFamily.Source,
                Direction = CanvasTextDirection.LeftToRightThenTopToBottom,
                WordWrapping = CanvasWordWrapping.NoWrap
            };

            double textWidth = MeasureTextSize(text, textFormat, (float)musicNameStackPanel.ActualWidth);
            if (textWidth >= musicNameStackPanel.ActualWidth)
            {
                AllowTextScrolling = true;
                musicNameInnerStackPanelHorizontalAlignment = HorizontalAlignment.Left;
                return $"{text}     {text}";
            }
            else
            {
                AllowTextScrolling = false;
                musicNameInnerStackPanelHorizontalAlignment = HorizontalAlignment.Center;
                return text;
            }
        }

        private double MeasureTextSize(string text, CanvasTextFormat textFormat, float limitedToWidth = 0.0f)
        {
            CanvasDevice device = CanvasDevice.GetSharedDevice();

            float width = float.IsNaN(limitedToWidth) || limitedToWidth < 0 ? 0 : limitedToWidth;
            var layout = new CanvasTextLayout(device, text, textFormat, width, 0);

            return layout.LayoutBounds.Width;
        }

        private void _scrollAnimation_Completed(object sender, object e)
        {
            IsTextScrolling = false;
            _scrollAnimation.Stop();
        }

        private void musicNameStackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            StartTextScrollAnimation();
        }

        private void StartTextScrollAnimation()
        {
            if (AllowTextScrolling == false)
            {
                return;
            }

            if (IsTextScrolling == false)
            {
                AnimationInit();
                _scrollAnimation.Begin();
                IsTextScrolling = true;
            }
        }
        private void musicName_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StartTextScrollAnimation();
        }

        private void AnimationInit()
        {
            _scrollAnimation = new Storyboard();
            _scrollAnimation.Completed += _scrollAnimation_Completed;
            DoubleAnimation animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(musicName.ActualWidth / musicName.FontSize / 4),
                //animation.RepeatBehavior = new RepeatBehavior(1);
                From = 0,
                // Here you need to calculate based on the number of spaces and the current FontSize
                To = -((musicName.ActualWidth / 2) + (musicName.FontSize / 2))
            };
            Storyboard.SetTarget(animation, musicName);
            Storyboard.SetTargetProperty(animation, "(UIElement.RenderTransform).(TranslateTransform.X)");
            _scrollAnimation.Children.Add(animation);
        }

        private void PlayRateSelectionChanged(object sender, ItemClickEventArgs e)
        {
            ViewModel.ChangePlayRateCommand.Execute(e.ClickedItem);
        }

        private void OnMusicDragOver(object sender, DragEventArgs e)
        {
            ViewModel.OnMusicDragOver(e);
        }

        private void OnMusicDrop(object sender, DragEventArgs e)
        {
            ViewModel.OnMusicDrop(e);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //For performance
            //ViewModel.GetMusicImages(musicImages);
        }
    }
}
