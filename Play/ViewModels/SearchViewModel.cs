﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Akavache;
using Play.Models;
using ReactiveUI;
using ReactiveUI.Xaml;

namespace Play.ViewModels
{
    public interface ISearchViewModel : IRoutableViewModel
    {
        string SearchQuery { get; set; }
        Visibility SearchBusySpinner { get; }

        ReactiveList<ISongTileViewModel> SearchResults { get; }
        ReactiveCommand PerformSearch { get; }
        ReactiveCommand GoBack { get; }
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
            set { this.RaiseAndSetIfChanged(ref _SearchQuery, value); }
        }

        ObservableAsPropertyHelper<Visibility> _SearchBusySpinner;
        public Visibility SearchBusySpinner {
            get { return _SearchBusySpinner.Value; }
        }

        ObservableAsPropertyHelper<ReactiveList<ISongTileViewModel>> _SearchResults;
        public ReactiveList<ISongTileViewModel> SearchResults {
            get { return _SearchResults.Value; }
        }

        public ReactiveCommand PerformSearch { get; protected set; }
        public ReactiveCommand GoBack { get; protected set; }

        public SearchViewModel(IScreen hostScreen, ILoginMethods loginMethods = null, IBlobCache userCache = null)
        {
            HostScreen = hostScreen;
            loginMethods = loginMethods ?? RxApp.DependencyResolver.GetService<ILoginMethods>();
            userCache = userCache ?? BlobCache.UserAccount;

            var playApi = loginMethods.CurrentAuthenticatedClient;

            if (playApi == null) {
                hostScreen.Router.Navigate.Execute(new WelcomeViewModel(HostScreen));
                return;
            }

            var canSearch = this.WhenAny(x => x.SearchQuery, x => !String.IsNullOrWhiteSpace(x.Value));
            PerformSearch = new ReactiveCommand(canSearch);

            this.ObservableForProperty(x => x.SearchQuery)
                .Throttle(TimeSpan.FromMilliseconds(700), RxApp.MainThreadScheduler)
                .InvokeCommand(PerformSearch);

            var searchResults = PerformSearch.RegisterAsync(_ =>
                userCache.GetOrFetchObject(
                    "search__" + SearchQuery, 
                    () => playApi.Search(SearchQuery), 
                    RxApp.TaskpoolScheduler.Now + TimeSpan.FromMinutes(1.0)));

            searchResults
                .LoggedCatch(this, Observable.Return(new List<Song>()))
                .Select(x => new ReactiveList<ISongTileViewModel>(x.Select(y => new SongTileViewModel(y, playApi))))
                .ToProperty(this, x => x.SearchResults, out _SearchResults);

            PerformSearch.IsExecuting
                .Select(x => x ? Visibility.Visible : Visibility.Hidden)
                .ToProperty(this, x => x.SearchBusySpinner, out _SearchBusySpinner);

            PerformSearch.ThrownExceptions.Subscribe(_ => { });

            GoBack = new ReactiveCommand();
            GoBack.InvokeCommand(hostScreen.Router.NavigateBack);
        }
    }
}