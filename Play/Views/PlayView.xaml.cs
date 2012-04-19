using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ninject;
using Play.ViewModels;
using Play.ViewModels;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Xaml;
using RestSharp;

namespace Play.Views
{
    public partial class PlayView : UserControl, IViewForViewModel<PlayViewModel>
    {
        public PlayViewModel ViewModel {
            get { return (PlayViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(PlayViewModel), typeof(PlayView), new UIPropertyMetadata(null));

        public PlayView()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DataContext = ViewModel;

            mediaElement.LoadedBehavior = MediaState.Manual;

            ViewModel.TogglePlay
                .Where(_ => ViewModel.ListenUrl != null)
                .Subscribe(nowPlayingUrl => {
                    if (!ViewModel.IsPlaying) {
                        mediaElement.Source = new Uri(ViewModel.ListenUrl);
                        mediaElement.Play();
                    } else {
                        mediaElement.Stop();
                    }

                    ViewModel.IsPlaying = !ViewModel.IsPlaying;
                });
        }

        object IViewForViewModel.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (PlayViewModel)value; }
        }
    }
}
