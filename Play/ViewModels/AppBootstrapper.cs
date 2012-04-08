using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Ninject;
using ReactiveUI;
using ReactiveUI.Routing;
using RestSharp;

namespace Play.ViewModels
{
    public interface IAppBootstrapper : IScreen
    {
        IObservable<IRestClient> GetAuthenticatedClient();
    }

    public class AppBootstrapper : ReactiveObject, IAppBootstrapper
    {
        public IRoutingState Router { get; protected set; }

        public AppBootstrapper(IKernel testKernel = null, IRoutingState router = null)
        {
            Kernel = testKernel ?? createDefaultKernel();
            Kernel.Bind<IAppBootstrapper>().ToConstant(this);
            Router = router ?? new RoutingState();

            RxApp.ConfigureServiceLocator(
                (type, contract) => Kernel.Get(type, contract),
                (type, contract) => Kernel.GetAll(type, contract));

            BlobCache.ApplicationName = "PlayForWindows";
        }

        public IObservable<IRestClient> GetAuthenticatedClient() { return getAuthenticatedClient().ToObservable(); }
        async Task<IRestClient> getAuthenticatedClient()
        {
            var blobCache = Kernel.Get<ISecureBlobCache>();
            var baseUrl = await blobCache.GetObjectAsync<string>("BaseUrl");
            var userName = await blobCache.GetObjectAsync<string>("Username");

            var ret = new RestClient(baseUrl);
            ret.AddDefaultParameter("login", userName);
            return ret;
        }

        public static IKernel Kernel { get; protected set; }

        IKernel createDefaultKernel()
        {
            var ret = new StandardKernel();

            ret.Bind<IScreen>().ToConstant(this);
            ret.Bind<IAppBootstrapper>().ToConstant(this);
            ret.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();
            ret.Bind<IPlayViewModel>().To<PlayViewModel>();

            return ret;
        }
    }
}
