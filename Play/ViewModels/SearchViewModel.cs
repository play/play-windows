using System;
using System.Collections.Generic;
using System.Linq;
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

        ReactiveCommand QueueSong { get; }
        ReactiveCommand QueueAlbum { get; }
        ReactiveCommand ShowSongsFromArtist { get; }
        ReactiveCommand ShowSongsFromAlbum { get; }
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

        public ReactiveCommand QueueSong { get; protected set; }
        public ReactiveCommand QueueAlbum { get; protected set; }
        public ReactiveCommand ShowSongsFromArtist { get; protected set; }
        public ReactiveCommand ShowSongsFromAlbum { get; protected set; }

        public SearchResultTileViewModel(Song model, IPlayApi playApi)
        {
            Model = model;

            playApi.FetchImageForAlbum(model).ToProperty(this, x => x.AlbumArt);
        }
    }
}
