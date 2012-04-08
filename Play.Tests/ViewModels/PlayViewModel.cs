using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using FluentAssertions;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.ViewModels;
using ReactiveUI.Routing;
using Xunit;

namespace Play.Tests.ViewModels
{
    public class PlayViewModelTests
    {
        [Fact]
        public void NavigatingToPlayWithoutAPasswordShouldNavigateToLogin()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            var app = new AppBootstrapper(kernel);
            var fixture = kernel.Get<IPlayViewModel>();
            app.Router.Navigate.Execute(fixture);

            (app.Router.GetCurrentViewModel() is IWelcomeViewModel).Should().BeTrue();
        }
    }
}
