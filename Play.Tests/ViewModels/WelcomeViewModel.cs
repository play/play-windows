using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.ViewModels;
using ReactiveUI;
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
    }
}
