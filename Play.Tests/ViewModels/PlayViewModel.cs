using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
    public class PlayViewModelTests
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
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();
            kernel.GetMock<IPlayApi>().Setup(x => x.ListenUrl()).Returns(Observable.Never<string>());
            kernel.GetMock<IPlayApi>().Setup(x => x.ConnectToSongChangeNotifications()).Returns(Observable.Never<Unit>());
            kernel.GetMock<IPlayApi>().Setup(x => x.NowPlaying()).Returns(Observable.Return(Fakes.GetSong()));
            kernel.GetMock<IPlayApi>().Setup(x => x.Queue()).Returns(Observable.Return(Fakes.GetAlbum()));
            kernel.GetMock<IPlayApi>().Setup(x => x.FetchImageForAlbum(It.IsAny<Song>())).Returns(Observable.Return<BitmapImage>(null));

            var router = new RoutingState();
            kernel.GetMock<IScreen>().Setup(x => x.Router).Returns(router);

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.EraseCredentialsAndNavigateToLogin())
                .Callback(() => router.Navigate.Execute(kernel.Get<IWelcomeViewModel>()));

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.CurrentAuthenticatedClient)
                .Returns(kernel.Get<IPlayApi>());

            var fixture = kernel.Get<IPlayViewModel>();
            router.Navigate.Execute(fixture);
            (router.GetCurrentViewModel() is IPlayViewModel).Should().BeTrue();
        }

        [Fact]
        public void LogoutButtonShouldSendMeToWelcomePage()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();
            kernel.GetMock<IPlayApi>().Setup(x => x.ListenUrl()).Returns(Observable.Never<string>());
            kernel.GetMock<IPlayApi>().Setup(x => x.ConnectToSongChangeNotifications()).Returns(Observable.Never<Unit>());
            kernel.GetMock<IPlayApi>().Setup(x => x.NowPlaying()).Returns(Observable.Return(Fakes.GetSong()));
            kernel.GetMock<IPlayApi>().Setup(x => x.Queue()).Returns(Observable.Return(Fakes.GetAlbum()));
            kernel.GetMock<IPlayApi>().Setup(x => x.FetchImageForAlbum(It.IsAny<Song>())).Returns(Observable.Return<BitmapImage>(null));

            var router = new RoutingState();
            kernel.GetMock<IScreen>().Setup(x => x.Router).Returns(router);

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.EraseCredentialsAndNavigateToLogin())
                .Callback(() => router.Navigate.Execute(kernel.Get<IWelcomeViewModel>()));

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.CurrentAuthenticatedClient)
                .Returns(kernel.Get<IPlayApi>());

            var fixture = kernel.Get<IPlayViewModel>();
            router.Navigate.Execute(fixture);
            fixture.Logout.Execute(null);

            (router.GetCurrentViewModel() is IPlayViewModel).Should().BeFalse();
        }

        [Fact]
        public void ListenUrlShouldCorrespondToActualUrl()
        {
            var kernel = new MoqMockingKernel();
            kernel.Bind<IPlayViewModel>().To<PlayViewModel>();
            kernel.Bind<IPlayApi>().To<PlayApi>();
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");

            var client = new RestClient("https://example.com");
            kernel.Bind<IRestClient>().ToConstant(client);

            var router = new RoutingState();
            kernel.GetMock<IScreen>().Setup(x => x.Router).Returns(router);

            kernel.GetMock<ILoginMethods>()
                .Setup(x => x.CurrentAuthenticatedClient).Returns(kernel.Get<IPlayApi>());

            var fixture = kernel.Get<IPlayViewModel>();
            router.Navigate.Execute(fixture);

            var result = fixture.WhenAny(x => x.ListenUrl, x => x.Value)
                .Where(x => x != null)
                .First();

            result.Should().Be("http://example.com:8000/listen");
        }
    }
}
