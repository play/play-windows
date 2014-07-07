using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Akavache;
using FluentAssertions;
using Moq;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
using Play.ViewModels;
using ReactiveUI;
using Xunit;

namespace Play.Tests.ViewModels
{
    public class BackgroundTaskTileViewModelTests
    {
        [Fact]
        public void DownloadAlbumShouldQueueABackgroundTask()
        {
            var kernel = new MoqMockingKernel();
            var taskHost = new BackgroundTaskHostViewModel();
            kernel.Bind<IBackgroundTaskHostViewModel>().ToConstant(taskHost);

            var fixture = setupStandardFixture(Fakes.GetSong(), kernel);

            var list = new List<int>();
            taskHost.BackgroundTasks.CollectionCountChanged.Subscribe(list.Add);
            fixture.DownloadAlbum.Execute(null);

            list.Contains(1).Should().BeTrue();
        }

        [Fact]
        public void DownloadSongShouldQueueABackgroundTask()
        {
            var kernel = new MoqMockingKernel();
            var taskHost = new BackgroundTaskHostViewModel();
            kernel.Bind<IBackgroundTaskHostViewModel>().ToConstant(taskHost);

            var fixture = setupStandardFixture(Fakes.GetSong(), kernel);

            var list = new List<int>();
            taskHost.BackgroundTasks.CollectionCountChanged.Subscribe(list.Add);
            fixture.DownloadSong.Execute(null);

            list.Contains(1).Should().BeTrue();
        }

        static ISongTileViewModel setupStandardFixture(Song song, MoqMockingKernel kernel)
        {
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("UserAccount");
            kernel.Bind<IBlobCache>().To<TestBlobCache>().Named("LocalMachine");
            RxApp.ConfigureServiceLocator((t,s) => kernel.Get(t,s), (t,s) => kernel.GetAll(t,s));

            kernel.GetMock<IPlayApi>().Setup(x => x.FetchImageForAlbum(It.IsAny<Song>()))
                .Returns(Observable.Return(new BitmapImage()));

            kernel.GetMock<IPlayApi>().Setup(x => x.DownloadAlbum(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Observable.Return<Tuple<string, byte[]>>(null));
            kernel.GetMock<IPlayApi>().Setup(x => x.DownloadSong(It.IsAny<Song>()))
                .Returns(Observable.Return<Tuple<string, byte[]>>(null));

            return new SongTileViewModel(song, kernel.Get<IPlayApi>());
        }
    }
}
