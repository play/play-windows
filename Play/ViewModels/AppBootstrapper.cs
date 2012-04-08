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
            Kernel = testKernel ?? new StandardKernel();
            Router = router ?? new RoutingState();

            Kernel.Bind<IScreen>().ToConstant(this);
            Kernel.Bind<IAppBootstrapper>().ToConstant(this);
            Kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();
            Kernel.Bind<IPlayViewModel>().To<PlayViewModel>();

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
    }
}
