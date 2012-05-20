using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Ninject;
using Play.Models;
using Play.Views;
using ReactiveUI;
using ReactiveUI.Routing;
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
        public AppBootstrapper(IKernel testKernel = null, IRoutingState router = null)
        {
            BlobCache.ApplicationName = "PlayForWindows";

            Kernel = testKernel ?? createDefaultKernel();
            Kernel.Bind<IAppBootstrapper>().ToConstant(this);
            Router = router ?? new RoutingState();

            apiFactory = Kernel.TryGet<Func<IObservable<IPlayApi>>>("ApiFactory");

            RxApp.ConfigureServiceLocator(
                (type, contract) => Kernel.Get(type, contract),
                (type, contract) => Kernel.GetAll(type, contract));

            LoadCredentials().Subscribe(
                x => {
                    CurrentAuthenticatedClient = x;
                    Router.Navigate.Execute(Kernel.Get<IPlayViewModel>());
                }, ex => {
                    this.Log().WarnException("Failed to load credentials, going to login screen", ex);
                    Router.Navigate.Execute(Kernel.Get<IPlayViewModel>());
                });
        }

        public static IKernel Kernel { get; protected set; }

        IPlayApi _CurrentAuthenticatedClient;
        public IPlayApi CurrentAuthenticatedClient {
            get { return _CurrentAuthenticatedClient; }
            set { this.RaiseAndSetIfChanged(x => x.CurrentAuthenticatedClient, value); }
        }
        
        /*
         * ILoginMethods
         */

        public void EraseCredentialsAndNavigateToLogin()
        {
            var blobCache = Kernel.Get<ISecureBlobCache>();

            blobCache.Invalidate("BaseUrl");
            blobCache.Invalidate("Token");

            Router.Navigate.Execute(Kernel.Get<IWelcomeViewModel>());
        }

        public void SaveCredentials(string baseUrl, string username)
        {
            var blobCache = Kernel.Get<ISecureBlobCache>();

            blobCache.InsertObject("BaseUrl", baseUrl);
            blobCache.InsertObject("Token", username);

            CurrentAuthenticatedClient = createPlayApiFromCreds(baseUrl, username);
        }

        public IObservable<IPlayApi> LoadCredentials() { return apiFactory != null ? apiFactory() : loadCredentials().ToObservable(); }
        Task<IPlayApi> loadCredentials()
        {
            var blobCache = Kernel.Get<ISecureBlobCache>();

            return Observable.Zip(
                    blobCache.GetObjectAsync<string>("BaseUrl"), blobCache.GetObjectAsync<string>("Token"),
                    (BaseUrl, Token) => new { BaseUrl, Token })
                .Select(x => (IPlayApi)createPlayApiFromCreds(x.BaseUrl, x.Token))
                .ToTask();
        }

        PlayApi createPlayApiFromCreds(string baseUrl, string token)
        {
            var localMachine = Kernel.Get<IBlobCache>("LocalMachine");
            var rc = new RestClient(baseUrl);
            rc.AddDefaultHeader("Authorization", token);

            var ret = new PlayApi(rc, localMachine);
            return ret;
        }

        IKernel createDefaultKernel()
        {
            var ret = new StandardKernel();

            ret.Bind<IScreen>().ToConstant(this);
            ret.Bind<ILoginMethods>().ToConstant(this);
            ret.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();
            ret.Bind<IPlayViewModel>().To<PlayViewModel>();
            ret.Bind<ISearchViewModel>().To<SearchViewModel>();
            ret.Bind<IViewForViewModel<WelcomeViewModel>>().To<WelcomeView>();
            ret.Bind<IViewForViewModel<PlayViewModel>>().To<PlayView>();
            ret.Bind<IViewForViewModel<SearchViewModel>>().To<SearchView>();
            ret.Bind<IViewForViewModel<SongTileViewModel>>().To<SongTileView>().InTransientScope();

#if DEBUG
            var testBlobCache = new TestBlobCache();
            ret.Bind<IBlobCache>().ToConstant(testBlobCache).Named("LocalMachine");
            ret.Bind<IBlobCache>().ToConstant(testBlobCache).Named("UserAccount");
            ret.Bind<ISecureBlobCache>().ToConstant(testBlobCache);
#else
            ret.Bind<ISecureBlobCache>().ToConstant(BlobCache.Secure);
            ret.Bind<IBlobCache>().ToConstant(BlobCache.LocalMachine).Named("LocalMachine");
            ret.Bind<IBlobCache>().ToConstant(BlobCache.UserAccount).Named("UserAccount");
#endif

            return ret;
        }
    }

    public static class LoginMethodsMixins
    {
        public static void LoadAuthenticatedUserFromCache(ILoginMethods login, ISecureBlobCache loginCache)
        {
            Observable.Zip(loginCache.GetObjectAsync<string>("BaseUrl"), loginCache.GetObjectAsync<string>("Token"),
                (url, name) => new Tuple<string, string>(url, name))
                .Catch(Observable.Return<Tuple<string, string>>(null))
                .Subscribe(x => {
                });
        }
    }
}
