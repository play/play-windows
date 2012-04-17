using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Akavache;
using FluentAssertions;
using Moq;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
using Play.ViewModels;
using ReactiveUI.Routing;
using ReactiveUI.Xaml;
using Xunit;

namespace Play.Tests.ViewModels
{
    public class SearchViewModelTests
    {
        [Fact]
        public void PlayApiShouldBeCalledOnPerformSearch()
        {
            var kernel = new MoqMockingKernel();
            var fixture = setupStandardFixture(kernel);
            fixture.PerformSearch.CanExecute(null).Should().BeFalse();

            fixture.SearchQuery = "Foo";
            fixture.PerformSearch.CanExecute(null).Should().BeTrue();
            fixture.PerformSearch.Execute(null);

            kernel.GetMock<IPlayApi>().Verify(x => x.Search("Foo"), Times.Once());

            fixture.SearchResults.Count.Should().Be(1);
            fixture.SearchResults[0].Model.id.Should().Be("4B52884E1E293890");
        }

        [Fact]
        public void DontThrashPlayApiOnMultipleSearchCalls()
        {
            var kernel = new MoqMockingKernel();
            var fixture = setupStandardFixture(kernel);

            fixture.SearchQuery = "Foo";
            fixture.PerformSearch.Execute(null);
            fixture.PerformSearch.Execute(null);
            fixture.PerformSearch.Execute(null);

            kernel.GetMock<IPlayApi>().Verify(x => x.Search("Foo"), Times.Once());
        }

        [Fact]
        public void EnsureWeFetchAnAlbumCover()
        {
            var kernel = new MoqMockingKernel();
            var fixture = setupStandardFixture(kernel);

            fixture.SearchQuery = "Foo";
            fixture.PerformSearch.Execute(null);

            kernel.GetMock<IPlayApi>().Verify(x => x.Search("Foo"), Times.Once());
            kernel.GetMock<IPlayApi>().Verify(x => x.FetchImageForAlbum(It.IsAny<Song>()), Times.Once());
        }

        static ISearchViewModel setupStandardFixture(MoqMockingKernel kernel)
        {
            kernel.Bind<ISearchViewModel>().To<SearchViewModel>();
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("UserAccount");
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");

            kernel.GetMock<IPlayApi>().Setup(x => x.Search("Foo"))
                .Returns(Observable.Return(new List<Song>() { Fakes.GetSong() }))
                .Verifiable();

            var img = new BitmapImage();
            kernel.GetMock<IPlayApi>().Setup(x => x.FetchImageForAlbum(It.IsAny<Song>()))
                .Returns(Observable.Return(img))
                .Verifiable();

            var fixture = kernel.Get<ISearchViewModel>();
            return fixture;
        }
    }
}