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
        NowPlaying Model { get; }
        IRestClient AuthenticatedClient { get; }

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

        ObservableAsPropertyHelper<NowPlaying> _Model;
        public NowPlaying Model {
            get { return _Model.Value; }
        }

        ObservableAsPropertyHelper<IRestClient> _AuthenticatedClient;
        public IRestClient AuthenticatedClient {
            get { return _AuthenticatedClient.Value; }
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
                .SelectMany(_ => bootstrapper.GetAuthenticatedClient())
                .Catch(Observable.Return<IRestClient>(null));

            newClient.ToProperty(this, x => x.AuthenticatedClient);

            newClient
                .Where(client => client == null)
                .Subscribe(client => HostScreen.Router.Navigate.Execute(AppBootstrapper.Kernel.Get<IWelcomeViewModel>()));

            var latestTrack = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5), RxApp.TaskpoolScheduler)
                .Where(_ => AuthenticatedClient != null)
                .SelectMany(client => NowPlayingHelper.FetchCurrent(AuthenticatedClient))
                .Catch(Observable.Return<NowPlaying>(null))
                .DistinctUntilChanged(x => x.id)
                .Multicast(new Subject<NowPlaying>());

            _inner = latestTrack.Connect();

            _Model = latestTrack
                .Where(track => track != null)
                .ToProperty(this, x => x.Model);

            _AlbumArt = latestTrack
                .Where(track => AuthenticatedClient != null && track != null)
                .SelectMany(x => x.FetchImageForAlbum(AuthenticatedClient))
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
