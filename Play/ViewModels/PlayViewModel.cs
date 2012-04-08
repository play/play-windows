using System;
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
    public interface IPlayViewModel : IRoutableViewModel, IDisposable
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

        IDisposable _inner;

        [Inject]
        public PlayViewModel(IAppBootstrapper bootstrapper)
        {
            HostScreen = bootstrapper;
            TogglePlay = new ReactiveCommand();
            Logout = new ReactiveCommand();

            Logout.Subscribe(_ => {
                bootstrapper.EraseCredentials();
                HostScreen.Router.Navigate.Execute(AppBootstrapper.Kernel.Get<IWelcomeViewModel>());
            });

            var newClient = this.NavigatedToMe()
                .SelectMany(_ => bootstrapper.GetPlayApi())
                .Catch(Observable.Return<IPlayApi>(null));

            newClient.ToProperty(this, x => x.AuthenticatedClient);

            newClient
                .Where(client => client == null)
                .Subscribe(client => HostScreen.Router.Navigate.Execute(AppBootstrapper.Kernel.Get<IWelcomeViewModel>()));

            newClient
                .Where(x => x != null)
                .SelectMany(x => x.ListenUrl())
                .ToProperty(this, x => x.ListenUrl);

            var latestTrack = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5), RxApp.TaskpoolScheduler)
                .Where(_ => AuthenticatedClient != null)
                .SelectMany(_ => AuthenticatedClient.NowPlaying())
                .Catch(Observable.Return<Song>(null))
                .DistinctUntilChanged(x => x.id)
                .Multicast(new Subject<Song>());

            _inner = latestTrack.Connect();

            latestTrack
                .Where(track => track != null)
                .ToProperty(this, x => x.Model);

            latestTrack
                .Where(track => AuthenticatedClient != null && track != null)
                .SelectMany(x => AuthenticatedClient.FetchImageForAlbum(x))
                .ToProperty(this, x => x.AlbumArt);
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref _inner, null);
            if (disp != null) {
                disp.Dispose();
            }
        }
    }
}
