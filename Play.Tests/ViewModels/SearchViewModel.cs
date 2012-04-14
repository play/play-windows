using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
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

            kernel.GetMock<IPlayApi>()
                .Setup(x => x.Search("Foo"))
                .Returns(Observable.Return(new List<Song>() { new Song() { id = "12345" } }))
                .Verifiable();

            var fixture = kernel.Get<ISearchViewModel>();
            fixture.PerformSearch.CanExecute(null).Should().BeFalse();

            fixture.SearchQuery = "Foo";
            fixture.PerformSearch.CanExecute(null).Should().BeTrue();
            fixture.PerformSearch.Execute(null);

            kernel.GetMock<IPlayApi>().Verify(x => x.Search("Foo"), Times.Once());

            fixture.SearchResults.Count.Should().Be(1);
            fixture.SearchResults[0].Model.id.Should().Be("12345");
        }
    }

    public interface ISearchViewModel : IRoutableViewModel
    {
        string SearchQuery { get; set; }

        ReactiveCollection<ISearchResultTileViewModel> SearchResults { get; }
        ReactiveAsyncCommand PerformSearch { get; }
    }

    public interface ISearchResultTileViewModel : IReactiveNotifyPropertyChanged
    {
        Song Model { get; }

        ReactiveCommand QueueSong { get; }
        ReactiveCommand QueueAlbum { get; }
        ReactiveCommand ShowSongsFromArtist { get; }
        ReactiveCommand ShowSongsFromAlbum { get; }
    }
}