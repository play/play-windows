using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Moq;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.ViewModels;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Testing;
using ReactiveUI.Xaml;
using Xunit;

namespace Play.Tests.ViewModels
{
    public class WelcomeViewModelTests : IEnableLogger
    {
        [Fact]
        public void CantHitOkWhenFieldsAreBlank()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            var fixture = kernel.Get<IWelcomeViewModel>();

            String.IsNullOrWhiteSpace(fixture.BaseUrl).Should().BeTrue();
            String.IsNullOrWhiteSpace(fixture.Username).Should().BeTrue();

            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.BaseUrl = "http://www.example.com";
            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.Username = "foo";
            fixture.OkButton.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void CantHitOkWhenBaseUrlIsntAUrl()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            var fixture = kernel.Get<IWelcomeViewModel>();

            fixture.BaseUrl = "Foobar";
            fixture.Username = "foo";
            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.BaseUrl = "ftp://google.com";
            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.BaseUrl = "    ";
            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.BaseUrl = "http://$#%(@#$)@#!!)@(";
            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.BaseUrl = "http://foobar";
            fixture.OkButton.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void FailedLoginIsAUserError()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            kernel.Bind<Func<string, string, IObservable<Unit>>>()
                .ToConstant<Func<string, string, IObservable<Unit>>>((url, user) => Observable.Throw<Unit>(new Exception()))
                .Named("connectToServer");

            var fixture = kernel.Get<IWelcomeViewModel>();

            bool errorThrown = false;
            using (UserError.OverrideHandlersForTesting(ex => { errorThrown = true; return Observable.Return(RecoveryOptionResult.CancelOperation); })) {
                fixture.Username = "Foo";
                fixture.BaseUrl = "http://bar";
                fixture.OkButton.Execute(null);
            }

            errorThrown.Should().BeTrue();
        }

        [Fact]
        public void SucceededLoginSavesTheInfo()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            kernel.Bind<Func<string, string, IObservable<Unit>>>()
                .ToConstant<Func<string, string, IObservable<Unit>>>((url, user) => Observable.Return<Unit>(Unit.Default))
                .Named("connectToServer");

            var mock = kernel.GetMock<IScreen>();
            mock.Setup(x => x.Router).Returns(new RoutingState());

            kernel.Bind<IScreen>().ToConstant(mock.Object);

            var initialPage = kernel.Get<IRoutableViewModel>();
            kernel.Get<IScreen>().Router.NavigateAndReset.Execute(initialPage);

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            var fixture = kernel.Get<IWelcomeViewModel>();

            bool errorThrown = false;
            string expectedUser = "Foo";
            string expectedUrl = "http://bar";
            using (UserError.OverrideHandlersForTesting(ex => { errorThrown = true; return Observable.Return(RecoveryOptionResult.CancelOperation); })) {
                fixture.Username = expectedUser;
                fixture.BaseUrl = expectedUrl;
                fixture.OkButton.Execute(null);
            }

            errorThrown.Should().BeFalse();

            cache.GetObjectAsync<string>("BaseUrl").First().Should().Be(expectedUrl);
            cache.GetObjectAsync<string>("Username").First().Should().Be(expectedUser);
        }

        [Fact]
        public void SucceededLoginNavigatesBackToInitialPage()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            kernel.Bind<Func<string, string, IObservable<Unit>>>()
                .ToConstant<Func<string, string, IObservable<Unit>>>((url, user) => Observable.Return<Unit>(Unit.Default))
                .Named("connectToServer");

            var mock = kernel.GetMock<IScreen>();
            var routingState = new RoutingState();
            mock.Setup(x => x.Router).Returns(routingState);

            kernel.Bind<IScreen>().ToConstant(mock.Object);

            var initialPage = kernel.Get<IRoutableViewModel>();
            kernel.Get<IScreen>().Router.NavigateAndReset.Execute(initialPage);

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            var fixture = kernel.Get<IWelcomeViewModel>();
            kernel.Get<IScreen>().Router.Navigate.Execute(fixture);

            bool errorThrown = false;
            string expectedUser = "Foo";
            string expectedUrl = "http://bar";
            using (UserError.OverrideHandlersForTesting(ex => { errorThrown = true; return Observable.Return(RecoveryOptionResult.CancelOperation); })) {
                fixture.Username = expectedUser;
                fixture.BaseUrl = expectedUrl;
                fixture.OkButton.Execute(null);
            }

            errorThrown.Should().BeFalse();

            kernel.Get<IScreen>().Router.GetCurrentViewModel().Should().Be(initialPage);
        }
    }

    public class WelcomeViewModelIntegrationTests
    {
        const string dummyUser = "xpaulbettsx";

        [Fact]
        public void SuccessfulLoginIntegrationTest()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            var mock = kernel.GetMock<IScreen>();
            var routingState = new RoutingState();
            mock.Setup(x => x.Router).Returns(routingState);

            var initialPage = kernel.Get<IRoutableViewModel>();
            kernel.Get<IScreen>().Router.NavigateAndReset.Execute(initialPage);

            var fixture = kernel.Get<IWelcomeViewModel>();
            kernel.Get<IScreen>().Router.Navigate.Execute(fixture);

            fixture.BaseUrl = IntegrationTestUrl.Current;
            fixture.Username = dummyUser;
            fixture.OkButton.Execute(null);

            kernel.Get<IScreen>().Router.ViewModelObservable().Skip(1)
                .Timeout(TimeSpan.FromSeconds(6.0), RxApp.TaskpoolScheduler)
                .First();

            fixture.ErrorMessage.Should().BeNull();
            kernel.Get<IScreen>().Router.GetCurrentViewModel().Should().Be(initialPage);
        }
    }
}
