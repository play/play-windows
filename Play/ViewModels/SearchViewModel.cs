using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Akavache;
using Ninject;
using Play.Models;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Xaml;

namespace Play.ViewModels
{
    public interface ISearchViewModel : IRoutableViewModel
    {
        string SearchQuery { get; set; }

        ReactiveCollection<ISearchResultTileViewModel> SearchResults { get; }
        ReactiveAsyncCommand PerformSearch { get; }
    }

    public interface ISearchResultTileViewModel : IReactiveNotifyPropertyChanged
    {
        Song Model { get; }
        BitmapImage AlbumArt { get; }

        ReactiveAsyncCommand QueueSong { get; }
        ReactiveAsyncCommand QueueAlbum { get; }
        ReactiveAsyncCommand ShowSongsFromArtist { get; }
        ReactiveAsyncCommand ShowSongsFromAlbum { get; }
    }

    public class SearchViewModel : ReactiveObject, ISearchViewModel
    {
        public string UrlPathSegment {
            get { return "/search"; }
        }

        public IScreen HostScreen { get; protected set; }

        string _SearchQuery;
        public string SearchQuery {
            get { return _SearchQuery; }
            set { this.RaiseAndSetIfChanged(x => x.SearchQuery, value); }
        }

        public ReactiveCollection<ISearchResultTileViewModel> SearchResults { get; protected set; }
        public ReactiveAsyncCommand PerformSearch { get; protected set; }

        [Inject]
        public SearchViewModel(IScreen hostScreen, IPlayApi playApi, [Named("UserAccount")] IBlobCache userCache)
        {
            HostScreen = hostScreen;
            SearchResults = new ReactiveCollection<ISearchResultTileViewModel>();

            var canSearch = this.WhenAny(x => x.SearchQuery, x => !String.IsNullOrWhiteSpace(x.Value));
            PerformSearch = new ReactiveAsyncCommand(canSearch);

            var searchResults = PerformSearch.RegisterAsyncObservable(_ =>
                userCache.GetOrFetchObject(
                    "search__" + SearchQuery, 
                    () => playApi.Search(SearchQuery), 
                    RxApp.TaskpoolScheduler.Now + TimeSpan.FromMinutes(1.0)));

            SearchResults = searchResults
                .Do(_ => SearchResults.Clear())
                .SelectMany(list => list.ToObservable())
                .CreateCollection(x => (ISearchResultTileViewModel) new SearchResultTileViewModel(x, playApi));
        }
    }

    public class SearchResultTileViewModel : ReactiveObject, ISearchResultTileViewModel
    {
        public Song Model { get; protected set; }

        ObservableAsPropertyHelper<BitmapImage> _AlbumArt;
        public BitmapImage AlbumArt {
            get { return _AlbumArt.Value; }
        }

        public ReactiveAsyncCommand QueueSong { get; protected set; }
        public ReactiveAsyncCommand QueueAlbum { get; protected set; }
        public ReactiveAsyncCommand ShowSongsFromArtist { get; protected set; }
        public ReactiveAsyncCommand ShowSongsFromAlbum { get; protected set; }

        public SearchResultTileViewModel(Song model, IPlayApi playApi)
        {
            Model = model;

            playApi.FetchImageForAlbum(model).ToProperty(this, x => x.AlbumArt);

            QueueSong = new ReactiveAsyncCommand();
            QueueAlbum = new ReactiveAsyncCommand();

            QueueSong.RegisterAsyncObservable(_ => playApi.QueueSong(Model))
                .Subscribe(
                    x => this.Log().Info("Queued {0}", Model.name),
                    ex => this.Log().WarnException("Failed to queue", ex));

            QueueAlbum.RegisterAsyncObservable(_ => playApi.AllSongsOnAlbum(Model.artist, Model.album))
                .SelectMany(x => x.ToObservable())
                .Select(x => reallyTryToQueueSong(playApi, x)).Concat()
                .Subscribe(
                    x => this.Log().Info("Queued song"),
                    ex => this.Log().WarnException("Failed to queue album", ex));
        }

        IObservable<Unit> reallyTryToQueueSong(IPlayApi playApi, Song song)
        {
            return Observable.Defer(() => playApi.QueueSong(song))
                .Timeout(TimeSpan.FromSeconds(20), RxApp.TaskpoolScheduler)
                .Retry(3);
        }
    }
}
