using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
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
        IPlayApi AuthenticatedClient { get; }
        BitmapImage AlbumArt { get; }
        string ListenUrl { get; }
        Song CurrentSong { get; }
        IEnumerable<Song> Queue { get; }

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

        ObservableAsPropertyHelper<Song> _CurrentSong;
        public Song CurrentSong {
            get { return _CurrentSong.Value; }
        }

        ObservableAsPropertyHelper<IEnumerable<Song>> _Queue;
        public IEnumerable<Song> Queue {
            get { return _Queue.Value; }
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

            // XXX: God I hate that I have to do this
            Observable.Never<Song>().ToProperty(this, x => x.CurrentSong);
            Observable.Never<IEnumerable<Song>>().ToProperty(this, x => x.Queue);
            Observable.Never<BitmapImage>().ToProperty(this, x => x.AlbumArt);

            this.WhenNavigatedTo(() => {
                var playApi = loginMethods.CurrentAuthenticatedClient;
                if (playApi == null) {
                    loginMethods.EraseCredentialsAndNavigateToLogin();
                    return null;
                }

                playApi.ListenUrl().ToProperty(this, x => x.ListenUrl);

                var pusherSubj = playApi.ConnectToSongChangeNotifications().Multicast(new Subject<Unit>());
                var shouldUpdate = Observable.Defer(() => 
                        pusherSubj.Take(1).Timeout(TimeSpan.FromMinutes(4.0), RxApp.TaskpoolScheduler)).Catch(Observable.Return(Unit.Default))
                    .Repeat()
                    .StartWith(Unit.Default)
                    .Multicast(new Subject<Unit>());

                var nowPlaying = shouldUpdate.SelectMany(_ => playApi.NowPlaying()).Multicast(new Subject<Song>());
                shouldUpdate.SelectMany(_ => playApi.Queue()).ToProperty(this, x => x.Queue);

                nowPlaying.ToProperty(this, x => x.CurrentSong);

                nowPlaying.SelectMany(playApi.FetchImageForAlbum)
                    .Catch<BitmapImage, Exception>(ex => { this.Log().WarnException("Failed to load album art", ex); return Observable.Return<BitmapImage>(null); })
                    .ToProperty(this, x => x.AlbumArt);

                var ret = new CompositeDisposable();
                ret.Add(nowPlaying.Connect());
                ret.Add(shouldUpdate.Connect());
                ret.Add(pusherSubj.Connect());
                return ret;
            });

            Logout.Subscribe(_ => loginMethods.EraseCredentialsAndNavigateToLogin());
        }
    }
}
