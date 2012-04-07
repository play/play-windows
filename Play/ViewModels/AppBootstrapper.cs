using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Ninject;
using ReactiveUI;
using ReactiveUI.Routing;

namespace Play.ViewModels
{
    public class AppBootstrapper : ReactiveObject, IScreen
    {
        public IRoutingState Router { get; protected set; }

        public AppBootstrapper(IKernel testKernel = null, IRoutingState router = null)
        {
            Kernel = testKernel ?? new StandardKernel();
            Router = router ?? new RoutingState();

            Kernel.Bind<AppBootstrapper>().ToConstant(this);
            Kernel.Bind<IScreen>().ToConstant(this);

            RxApp.ConfigureServiceLocator(
                (type, contract) => Kernel.Get(type, contract),
                (type, contract) => Kernel.GetAll(type, contract));

            BlobCache.ApplicationName = "PlayForWindows";
        }

        public static IKernel Kernel { get; protected set; }
    }
}
