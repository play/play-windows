using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media.Imaging;
using Ninject;
using Play.Models;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Xaml;
using RestSharp;

namespace Play.ViewModels
{
    public class PlayViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment {
            get { return "play"; }
        }

        public IScreen HostScreen { get; protected set; }

        public ReactiveCommand TogglePlay { get; protected set; }

        ObservableAsPropertyHelper<BitmapImage> _AlbumArt;
        public BitmapImage AlbumArt {
            get { return _AlbumArt.Value; }
        }

        ObservableAsPropertyHelper<NowPlaying> _Model;
        public NowPlaying Model {
            get { return _Model.Value; }
        }

        IRestClient authenticatedClient = null;

        [Inject]
        public PlayViewModel(IAppBootstrapper bootstrapper)
        {
            HostScreen = bootstrapper;
            TogglePlay = new ReactiveCommand();

            var latestTrack = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5), RxApp.DeferredScheduler)
                .Where(_ => authenticatedClient != null)
                .SelectMany(client => NowPlayingHelper.FetchCurrent(authenticatedClient))
                .DistinctUntilChanged(x => x.id)
                .Do(x => Console.WriteLine(x.name), ex => Console.WriteLine(ex))
                .Multicast(new Subject<NowPlaying>());

            latestTrack.Connect();

            _Model = latestTrack.ToProperty(this, x => x.Model);

            _AlbumArt = latestTrack
                .Where(_ => authenticatedClient != null)
                .SelectMany(x => x.FetchImageForAlbum(authenticatedClient))
                .ToProperty(this, x => x.AlbumArt);

            this.NavigatedToMe()
                .SelectMany(_ => bootstrapper.GetAuthenticatedClient())
                .Catch(Observable.Return<IRestClient>(null))
                .Repeat()
                .Subscribe(client => {
                    if (client == null) {
                        authenticatedClient = null;
                        HostScreen.Router.Navigate.Execute(AppBootstrapper.Kernel.Get<IWelcomeViewModel>());
                    }

                    authenticatedClient = client;
                });
        }
    }
}
