using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akavache;
using FluentAssertions;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
using Play.ViewModels;
using ReactiveUI;
using ReactiveUI.Routing;
using Xunit;
using Moq;

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
            using (var fixture = kernel.Get<IPlayViewModel>()) {
                app.Router.Navigate.Execute(fixture);
                (app.Router.GetCurrentViewModel() is IWelcomeViewModel).Should().BeTrue();
            }
        }

        [Fact]
        public void NavigatingToPlayWithCredsShouldStayOnPlay()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);
            cache.InsertObject("BaseUrl", "https://example.com");
            cache.InsertObject("Username", "hubot");

            var app = new AppBootstrapper(kernel);
            using (var fixture = kernel.Get<IPlayViewModel>()) {
                app.Router.Navigate.Execute(fixture);
                (app.Router.GetCurrentViewModel() is IPlayViewModel).Should().BeTrue();
            }
        }

        [Fact]
        public void LogoutButtonShouldSendMeToWelcomePage()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            cache.InsertObject("BaseUrl", "https://example.com");
            cache.InsertObject("Username", "hubot");

            var app = new AppBootstrapper(kernel);
            using (var fixture = kernel.Get<IPlayViewModel>()) {
                app.Router.Navigate.Execute(fixture);
                var vm = app.Router.GetCurrentViewModel();
                (vm is IPlayViewModel).Should().BeTrue();

                fixture.Logout.Execute(null);
                (app.Router.GetCurrentViewModel() is IWelcomeViewModel).Should().BeTrue();
            }
        } 

        [Fact]
        public void ListenUrlShouldCorrespondToActualUrl()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("UserAccount");

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            cache.InsertObject("BaseUrl", "https://example.com");
            cache.InsertObject("Username", "hubot");

            var app = new AppBootstrapper(kernel);
            using (var fixture = kernel.Get<IPlayViewModel>()) {
                app.Router.Navigate.Execute(fixture);
                var vm = app.Router.GetCurrentViewModel() as IPlayViewModel;
                vm.Should().NotBeNull();

                var result = vm.WhenAny(x => x.ListenUrl, x => x.Value)
                    .Where(x => x != null)
                    .First();

                result.Should().Be("http://example.com:8000/listen");
            }
        }       
    }
}
