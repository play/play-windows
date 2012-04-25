using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Akavache;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
using Play.ViewModels;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Testing;
using RestSharp;
using Xunit;
using Moq;

namespace Play.Tests.ViewModels
{
    public class PlayViewModelTests : IEnableLogger
    {
        [Fact]
        public void NavigatingToPlayWithoutAPasswordShouldNavigateToLogin()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();

            var cache = new TestBlobCache(null, (IEnumerable<KeyValuePair<string, byte[]>>)null);
            kernel.Bind<ISecureBlobCache>().ToConstant(cache);

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.EraseCredentialsAndNavigateToLogin()).Verifiable();

            var router = new RoutingState();
            kernel.GetMock<IScreen>().Setup(x => x.Router).Returns(router);

            var fixture = kernel.Get<IPlayViewModel>();
            router.Navigate.Execute(fixture);
            kernel.GetMock<ILoginMethods>().Verify(x => x.EraseCredentialsAndNavigateToLogin(), Times.Once());
        }

        [Fact]
        public void NavigatingToPlayWithCredsShouldStayOnPlay()
        {
            var kernel = new MoqMockingKernel();
            var router = new RoutingState();

            var fixture = setupStandardMock(kernel, router);

            router.Navigate.Execute(fixture);
            (router.GetCurrentViewModel() is IPlayViewModel).Should().BeTrue();
        }

        [Fact]
        public void LogoutButtonShouldSendMeToWelcomePage()
        {
            var kernel = new MoqMockingKernel();
            var router = new RoutingState();

            var fixture = setupStandardMock(kernel, router);
            router.Navigate.Execute(fixture);
            fixture.Logout.Execute(null);

            (router.GetCurrentViewModel() is IPlayViewModel).Should().BeFalse();
        }

        [Fact]
        public void WhenPusherFiresWeShouldUpdateTheAlbum()
        {
            var kernel = new MoqMockingKernel();
            var router = new RoutingState();
            var pusher = new Subject<Unit>();
            int nowPlayingExecuteCount = 0;

            var fixture = setupStandardMock(kernel, router, () => {
                kernel.GetMock<IPlayApi>().Setup(x => x.ConnectToSongChangeNotifications()).Returns(pusher);
                kernel.GetMock<IPlayApi>().Setup(x => x.NowPlaying())
                    .Callback(() => nowPlayingExecuteCount++).Returns(Observable.Return(Fakes.GetSong()));
            });

            router.Navigate.Execute(fixture);
            nowPlayingExecuteCount.Should().Be(1);

            pusher.OnNext(Unit.Default);
            nowPlayingExecuteCount.Should().Be(2);
        }

        [Fact]
        public void TheTimerShouldFireIfPusherDoesnt()
        {
            (new TestScheduler()).With(sched =>
            {
                var kernel = new MoqMockingKernel();
                var router = new RoutingState();
                var pusher = new Subject<Unit>();
                int nowPlayingExecuteCount = 0;

                var fixture = setupStandardMock(kernel, router, () => {
                    kernel.GetMock<IPlayApi>().Setup(x => x.ConnectToSongChangeNotifications()).Returns(pusher);
                    kernel.GetMock<IPlayApi>().Setup(x => x.NowPlaying())
                        .Callback(() => nowPlayingExecuteCount++).Returns(Observable.Return(Fakes.GetSong()));
                });

                router.Navigate.Execute(fixture);
                sched.AdvanceToMs(10);
                nowPlayingExecuteCount.Should().Be(1);

                sched.AdvanceToMs(1000);
                nowPlayingExecuteCount.Should().Be(1);

                pusher.OnNext(Unit.Default);
                sched.AdvanceToMs(1010);
                nowPlayingExecuteCount.Should().Be(2);

                // NB: The 2 minute timer starts after the last Pusher notification
                // make sure we *don't* tick.
                sched.AdvanceToMs(2*60*1000 + 10);
                nowPlayingExecuteCount.Should().Be(2);

                sched.AdvanceToMs(3*60*1000 + 1500);
                nowPlayingExecuteCount.Should().Be(3);
            });
            
        }

        IPlayViewModel setupStandardMock(MoqMockingKernel kernel, IRoutingState router, Action extraSetup = null)
        {
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();

            var playApi = kernel.GetMock<IPlayApi>();
            playApi.Setup(x => x.ListenUrl()).Returns(Observable.Never<string>());
            playApi.Setup(x => x.ConnectToSongChangeNotifications()).Returns(Observable.Never<Unit>());
            playApi.Setup(x => x.NowPlaying()).Returns(Observable.Return(Fakes.GetSong()));
            playApi.Setup(x => x.Queue()).Returns(Observable.Return(Fakes.GetAlbum()));
            playApi.Setup(x => x.FetchImageForAlbum(It.IsAny<Song>())).Returns(Observable.Return<BitmapImage>(null));

            kernel.GetMock<IScreen>().Setup(x => x.Router).Returns(router);

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.EraseCredentialsAndNavigateToLogin())
                .Callback(() => router.Navigate.Execute(kernel.Get<IWelcomeViewModel>()));

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.CurrentAuthenticatedClient)
                .Returns(kernel.Get<IPlayApi>());

            if (extraSetup != null) extraSetup();

            RxApp.ConfigureServiceLocator((t,s) => kernel.Get(t,s), (t,s) => kernel.GetAll(t,s));
            return kernel.Get<IPlayViewModel>();
        }

    }
}
