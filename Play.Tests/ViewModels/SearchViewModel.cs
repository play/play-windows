using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using ReactiveUI;
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

    public class SearchResultTileViewModelTests : IEnableLogger
    {
        [Fact]
        public void QueuingASongShouldCallPlayApi()
        {
            var kernel = new MoqMockingKernel();
            var song = Fakes.GetSong();
            var fixture = setupStandardFixture(song, kernel);

            fixture.QueueSong.Execute(null);
            kernel.GetMock<IPlayApi>().Verify(x => x.QueueSong(It.IsAny<Song>()));
        }

        [Fact]
        public void QueueingAlbumShouldCallQueueSongForEverySong()
        {
            var kernel = new MoqMockingKernel();
            var song = Fakes.GetSong();

            var fakeAlbum = Fakes.GetAlbum();
            var queuedSongs = new List<Song>();
            var fixture = setupStandardFixture(song, kernel);

            kernel.GetMock<IPlayApi>().Setup(x => x.QueueSong(It.IsAny<Song>()))
                .Callback<Song>(queuedSongs.Add)
                .Returns(Observable.Return(Unit.Default))
                .Verifiable();

            fixture.QueueAlbum.Execute(null);

            this.Log().Info("Queued songs: {0}", String.Join(",", queuedSongs.Select(x => x.name)));
            queuedSongs.Count.Should().Be(fakeAlbum.Count);
            fakeAlbum.Zip(queuedSongs, (e, a) => e.id == a.id).All(x => x).Should().BeTrue();
        }

        static ISearchResultTileViewModel setupStandardFixture(Song song, MoqMockingKernel kernel)
        {
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("UserAccount");
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");

            kernel.GetMock<IPlayApi>().Setup(x => x.QueueSong(It.IsAny<Song>()))
                .Returns(Observable.Return(Unit.Default))
                .Verifiable();

            kernel.GetMock<IPlayApi>().Setup(x => x.AllSongsOnAlbum(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Observable.Return(Fakes.GetAlbum()))
                .Verifiable();

            kernel.GetMock<IPlayApi>().Setup(x => x.FetchImageForAlbum(It.IsAny<Song>()))
                .Returns(Observable.Return(new BitmapImage()));

            return new SearchResultTileViewModel(song, kernel.Get<IPlayApi>());
        }
    }
}