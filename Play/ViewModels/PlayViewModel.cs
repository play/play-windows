using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
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
        string ListenUrl { get; }
        Song CurrentSong { get; }
        IEnumerable<Song> Queue { get; }
        IEnumerable<SongTileViewModel> AllSongs { get; }
            
        ReactiveCommand TogglePlay { get; }
        ReactiveCommand Search { get; }
        ReactiveCommand Logout { get; }
    }

    public class PlayViewModel : ReactiveObject, IPlayViewModel
    {
        public string UrlPathSegment {
            get { return "play"; }
        }

        public IScreen HostScreen { get; protected set; }

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

        ObservableAsPropertyHelper<IEnumerable<SongTileViewModel>> _AllSongs;
        public IEnumerable<SongTileViewModel> AllSongs {
            get { return _AllSongs.Value; }
        }

        bool _IsPlaying;
        public bool IsPlaying {
            get { return _IsPlaying; }
            set { this.RaiseAndSetIfChanged(x => x.IsPlaying, value); }
        }

        public ReactiveCommand TogglePlay { get; protected set; }
        public ReactiveCommand Search { get; protected set; }
        public ReactiveCommand Logout { get; protected set; }

        [Inject]
        public PlayViewModel(IScreen screen, ILoginMethods loginMethods)
        {
            HostScreen = screen;
            TogglePlay = new ReactiveCommand();
            Logout = new ReactiveCommand();
            Search = new ReactiveCommand();

            // XXX: God I hate that I have to do this
            Observable.Never<Song>().ToProperty(this, x => x.CurrentSong);
            Observable.Never<IEnumerable<Song>>().ToProperty(this, x => x.Queue);
            Observable.Never<IEnumerable<SongTileViewModel>>().ToProperty(this, x => x.AllSongs);

            this.WhenNavigatedTo(() => {
                var playApi = loginMethods.CurrentAuthenticatedClient;
                if (playApi == null) {
                    loginMethods.EraseCredentialsAndNavigateToLogin();
                    return null;
                }

                // Get the Listen URL or die trying
                Observable.Defer(playApi.ListenUrl)
                    .Timeout(TimeSpan.FromSeconds(30), RxApp.TaskpoolScheduler)
                    .Retry()
                    .ToProperty(this, x => x.ListenUrl);

                var pusherSubj = playApi.ConnectToSongChangeNotifications()
                    .Retry(25)
                    .Multicast(new Subject<Unit>());

                var shouldUpdate = Observable.Defer(() => 
                        pusherSubj.Take(1).Timeout(TimeSpan.FromMinutes(2.0), RxApp.TaskpoolScheduler)).Catch(Observable.Return(Unit.Default))
                    .Repeat()
                    .StartWith(Unit.Default)
                    .Multicast(new Subject<Unit>());

                var nowPlaying = shouldUpdate.SelectMany(_ => playApi.NowPlaying()).Multicast(new Subject<Song>());
                shouldUpdate.SelectMany(_ => playApi.Queue())
                    .Catch(Observable.Return(Enumerable.Empty<Song>()))
                    .ToProperty(this, x => x.Queue);

                nowPlaying
                    .Catch(Observable.Return(new Song()))
                    .ToProperty(this, x => x.CurrentSong);

                this.WhenAny(x => x.CurrentSong, x => x.Queue, 
                        (song, queue) => (queue.Value != null && song.Value != null ? queue.Value.StartWith(song.Value) : Enumerable.Empty<Song>()))
                    .Do(x => this.Log().Info("Found {0} items", x.Count()))
                    .Select(x => x.Select(y => new SongTileViewModel(y, loginMethods.CurrentAuthenticatedClient) { QueueSongVisibility = Visibility.Collapsed }))
                    .ToProperty(this, x => x.AllSongs);

                MessageBus.Current.RegisterMessageSource(this.WhenAny(x => x.IsPlaying, x => x.Value), "IsPlaying");

                var ret = new CompositeDisposable();
                ret.Add(nowPlaying.Connect());
                ret.Add(shouldUpdate.Connect());
                ret.Add(pusherSubj.Connect());
                return ret;
            });

            Logout.Subscribe(_ => loginMethods.EraseCredentialsAndNavigateToLogin());
            Search.Subscribe(_ => screen.Router.Navigate.Execute(RxApp.GetService<ISearchViewModel>()));
        }
    }
}
