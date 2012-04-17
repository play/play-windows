using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
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

        ReactiveCollection<ISongTileViewModel> SearchResults { get; }
        ReactiveAsyncCommand PerformSearch { get; }
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

        public ReactiveCollection<ISongTileViewModel> SearchResults { get; protected set; }
        public ReactiveAsyncCommand PerformSearch { get; protected set; }

        [Inject]
        public SearchViewModel(IScreen hostScreen, IPlayApi playApi, [Named("UserAccount")] IBlobCache userCache)
        {
            HostScreen = hostScreen;
            SearchResults = new ReactiveCollection<ISongTileViewModel>();

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
                .CreateCollection(x => (ISongTileViewModel) new SongTileViewModel(x, playApi));
        }
    }
}
