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
    public partial class PlayView : UserControl, IViewForViewModel<IPlayViewModel>
    {
        public IPlayViewModel ViewModel {
            get { return (IPlayViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(IPlayViewModel), typeof(PlayView), new UIPropertyMetadata(null));

        public PlayView()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DataContext = ViewModel;

            bool isPlaying = false;
            mediaElement.LoadedBehavior = MediaState.Manual;

            ViewModel.TogglePlay
                .Where(_ => ViewModel.AuthenticatedClient != null)
                .Subscribe(_ => {
                    if (!isPlaying) {
                        mediaElement.Source = GetUriFromRestClient(ViewModel.AuthenticatedClient);
                        mediaElement.Play();
                    } else {
                        mediaElement.Stop();
                    }
                        
                    isPlaying = !isPlaying;
                });
        }

        public static Uri GetUriFromRestClient(IRestClient authenticatedClient)
        {
            var uri = new Uri(authenticatedClient.BaseUrl);
            return new Uri(String.Format("{0}:8000/listen", uri.GetLeftPart(UriPartial.Authority)));
        }

        object IViewForViewModel.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (PlayViewModel)value; }
        }
    }
}
