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
using Play.Models;
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
            String.IsNullOrWhiteSpace(fixture.Token).Should().BeTrue();

            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.BaseUrl = "http://www.example.com";
            fixture.OkButton.CanExecute(null).Should().BeFalse();

            fixture.Token = "foo";
            fixture.OkButton.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void CantHitOkWhenBaseUrlIsntAUrl()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            var fixture = kernel.Get<IWelcomeViewModel>();

            fixture.BaseUrl = "Foobar";
            fixture.Token = "foo";
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
                fixture.Token = "Foo";
                fixture.BaseUrl = "http://bar";
                fixture.OkButton.Execute(null);
            }

            errorThrown.Should().BeTrue();
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
                fixture.Token = expectedUser;
                fixture.BaseUrl = expectedUrl;
                fixture.OkButton.Execute(null);
            }

            errorThrown.Should().BeFalse();

            kernel.Get<IScreen>().Router.GetCurrentViewModel().Should().Be(initialPage);
        }

        [Fact]
        public void SucceededLoginSetsTheCurrentAuthenticatedClient()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IWelcomeViewModel>().To<WelcomeViewModel>();

            string expectedUser = "Foo";
            string expectedUrl = "http://bar";

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
            using (UserError.OverrideHandlersForTesting(ex => { errorThrown = true; return Observable.Return(RecoveryOptionResult.CancelOperation); })) {
                fixture.Token = expectedUser;
                fixture.BaseUrl = expectedUrl;
                fixture.OkButton.Execute(null);
            }

            errorThrown.Should().BeFalse();

            kernel.Get<IScreen>().Router.GetCurrentViewModel().Should().Be(initialPage);
            kernel.GetMock<ILoginMethods>().Verify(x => x.SaveCredentials(expectedUrl, expectedUser), Times.Once());
        }
    }

    public class WelcomeViewModelIntegrationTests
    {
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
            fixture.Token = IntegrationTestUrl.Token;
            fixture.OkButton.Execute(null);

            kernel.Get<IScreen>().Router.ViewModelObservable().Skip(1)
                .Timeout(TimeSpan.FromSeconds(10.0), RxApp.TaskpoolScheduler)
                .First();

            fixture.ErrorMessage.Should().BeNull();
            kernel.Get<IScreen>().Router.GetCurrentViewModel().Should().Be(initialPage);
        }
    }
}
