using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Media.Imaging;
using Ninject;
using Play.Models;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Xaml;
using RestSharp;

namespace Play.ViewModels
{
    public interface IPlayViewModel : IRoutableViewModel
    {
        BitmapImage AlbumArt { get; }
        Song Model { get; }
        IPlayApi AuthenticatedClient { get; }
        string ListenUrl { get; }

        ReactiveCommand TogglePlay { get; }
        ReactiveCommand Logout { get; }
    }

    public class PlayViewModel : ReactiveObject, IPlayViewModel
    {
        public string UrlPathSegment {
            get { return "play"; }
        }

        public IScreen HostScreen { get; protected set; }

        ObservableAsPropertyHelper<BitmapImage> _AlbumArt;
        public BitmapImage AlbumArt {
            get { return _AlbumArt.Value; }
        }

        ObservableAsPropertyHelper<Song> _Model;
        public Song Model {
            get { return _Model.Value; }
        }

        ObservableAsPropertyHelper<IPlayApi> _AuthenticatedClient;
        public IPlayApi AuthenticatedClient {
            get { return _AuthenticatedClient.Value; }
        }

        ObservableAsPropertyHelper<string> _ListenUrl;
        public string ListenUrl {
            get { return _ListenUrl.Value; }
        }

        public ReactiveCommand TogglePlay { get; protected set; }
        public ReactiveCommand Logout { get; protected set; }

        [Inject]
        public PlayViewModel(IScreen screen, ILoginMethods loginMethods)
        {
            HostScreen = screen;
            TogglePlay = new ReactiveCommand();
            Logout = new ReactiveCommand();

            Observable.Never<Song>().ToProperty(this, x => x.Model);
            Observable.Never<BitmapImage>().ToProperty(this, x => x.AlbumArt);

            this.WhenNavigatedTo(() => {
                var playApi = loginMethods.CurrentAuthenticatedClient;
                if (playApi == null) {
                    loginMethods.EraseCredentialsAndNavigateToLogin();
                    return null;
                }

                playApi.ListenUrl().ToProperty(this, x => x.ListenUrl);

                var model = new Subject<Song>();
                var ret = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5.0), RxApp.TaskpoolScheduler)
                    .SelectMany(_ => playApi.NowPlaying())
                    .Subscribe(model);

                model.ToProperty(this, x => x.Model);

                model.SelectMany(playApi.FetchImageForAlbum)
                    .Catch<BitmapImage, Exception>(ex => { this.Log().WarnException("Failed to load album art", ex); return Observable.Return<BitmapImage>(null); })
                    .ToProperty(this, x => x.AlbumArt);
                return ret;
            });

            Logout.Subscribe(_ => loginMethods.EraseCredentialsAndNavigateToLogin());
        }
    }
}
