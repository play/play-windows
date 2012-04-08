using System;
using System.Collections.Generic;
using System.Linq;
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
    public interface IAppBootstrapper : IScreen
    {
        IObservable<IPlayApi> GetPlayApi();
        void EraseCredentials();
    }

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

            Router.Navigate.Execute(Kernel.Get<IPlayViewModel>());
        }

        public void EraseCredentials()
        {
            var blobCache = Kernel.Get<ISecureBlobCache>();
            blobCache.Invalidate("BaseUrl");
            blobCache.Invalidate("Username");
        }

        public IObservable<IPlayApi> GetPlayApi() { return apiFactory != null ? apiFactory() : getPlayApi().ToObservable(); }
        async Task<IPlayApi> getPlayApi()
        {
            var blobCache = Kernel.Get<ISecureBlobCache>();
            var localMachine = Kernel.Get<IBlobCache>("LocalMachine");
            var baseUrl = await blobCache.GetObjectAsync<string>("BaseUrl");
            var userName = await blobCache.GetObjectAsync<string>("Username");

            var ret = new RestClient(baseUrl);
            ret.AddDefaultParameter("login", userName);
            return new PlayApi(ret, localMachine);
        }

        public static IKernel Kernel { get; protected set; }

        IKernel createDefaultKernel()
        {
            var ret = new StandardKernel();

            ret.Bind<IScreen>().ToConstant(this);
            ret.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();
            ret.Bind<IPlayViewModel>().To<PlayViewModel>();
            ret.Bind<ISecureBlobCache>().ToConstant(BlobCache.Secure);
            ret.Bind<IBlobCache>().ToConstant(BlobCache.LocalMachine).Named("LocalMachine");
            ret.Bind<IBlobCache>().ToConstant(BlobCache.UserAccount).Named("UserAccount");
            ret.Bind<IViewForViewModel<WelcomeViewModel>>().To<WelcomeView>();
            ret.Bind<IViewForViewModel<PlayViewModel>>().To<PlayView>();

            return ret;
        }
    }
}
