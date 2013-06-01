using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Play.Models;
using Play.Views;
using ReactiveUI;
using RestSharp;

namespace Play.ViewModels
{
    public interface ILoginMethods : IReactiveNotifyPropertyChanged
    {
        IPlayApi CurrentAuthenticatedClient { get; set; }

        void EraseCredentialsAndNavigateToLogin();
        void SaveCredentials(string baseUrl, string username);
        IObservable<IPlayApi> LoadCredentials();
    }

    public interface IAppBootstrapper : IScreen, ILoginMethods { }

    public class AppBootstrapper : ReactiveObject, IAppBootstrapper
    {
        public IRoutingState Router { get; protected set; }

        readonly Func<IObservable<IPlayApi>> apiFactory;
        public AppBootstrapper(IMutableDependencyResolver dependencyResolver = null, IRoutingState router = null)
        {
            BlobCache.ApplicationName = "PlayForWindows";

            dependencyResolver = dependencyResolver ?? RxApp.MutableResolver;
            Router = router ?? new RoutingState();

            apiFactory = dependencyResolver.GetService<Func<IObservable<IPlayApi>>>("ApiFactory");
            registerParts(dependencyResolver);

            LoadCredentials().Subscribe(
                x => {
                    CurrentAuthenticatedClient = x;
                    Router.Navigate.Execute(new PlayViewModel(this));
                }, ex => {
                    this.Log().WarnException("Failed to load credentials, going to login screen", ex);
                    Router.Navigate.Execute(new PlayViewModel(this));
                });
        }

        IPlayApi _CurrentAuthenticatedClient;
        public IPlayApi CurrentAuthenticatedClient {
            get { return _CurrentAuthenticatedClient; }
            set { this.RaiseAndSetIfChanged(ref _CurrentAuthenticatedClient, value); }
        }
        
        /*
         * ILoginMethods
         */

        public void EraseCredentialsAndNavigateToLogin()
        {
            var blobCache = BlobCache.Secure;

            blobCache.Invalidate("BaseUrl");
            blobCache.Invalidate("Token");

            Router.Navigate.Execute(new WelcomeViewModel(this));
        }

        public void SaveCredentials(string baseUrl, string username)
        {
            var blobCache = BlobCache.Secure;

            blobCache.InsertObject("BaseUrl", baseUrl);
            blobCache.InsertObject("Token", username);

            CurrentAuthenticatedClient = createPlayApiFromCreds(baseUrl, username);
        }

        public IObservable<IPlayApi> LoadCredentials() { return apiFactory != null ? apiFactory() : loadCredentials().ToObservable(); }
        Task<IPlayApi> loadCredentials()
        {
            var blobCache = BlobCache.Secure;

            return Observable.Zip(
                    blobCache.GetObjectAsync<string>("BaseUrl"), blobCache.GetObjectAsync<string>("Token"),
                    (BaseUrl, Token) => new { BaseUrl, Token })
                .Select(x => (IPlayApi)createPlayApiFromCreds(x.BaseUrl, x.Token))
                .ToTask();
        }

        PlayApi createPlayApiFromCreds(string baseUrl, string token)
        {
            var localMachine = BlobCache.LocalMachine;
            var rc = new RestClient(baseUrl);
            rc.AddDefaultHeader("Authorization", token);

            var ret = new PlayApi(rc, localMachine);
            return ret;
        }

        void registerParts(IMutableDependencyResolver dependencyResolver)
        {
            dependencyResolver.RegisterConstant(this, typeof(ILoginMethods));
            dependencyResolver.RegisterConstant(this, typeof(IScreen));
            dependencyResolver.RegisterConstant(this, typeof(IAppBootstrapper));

            dependencyResolver.Register(() => new WelcomeView(), typeof(IViewFor<WelcomeViewModel>));
            dependencyResolver.Register(() => new PlayView(), typeof(IViewFor<PlayViewModel>));
            dependencyResolver.Register(() => new SearchView(), typeof(IViewFor<SearchViewModel>));
            dependencyResolver.Register(() => new SongTileView(), typeof(IViewFor<SongTileViewModel>));
        }
    }
}
