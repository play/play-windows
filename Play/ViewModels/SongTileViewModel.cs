using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using Play.Models;
using ReactiveUI;
using ReactiveUI.Xaml;

namespace Play.ViewModels
{
    public interface ISongTileViewModel : IReactiveNotifyPropertyChanged
    {
        Song Model { get; }
        BitmapImage AlbumArt { get; }

        ReactiveAsyncCommand QueueSong { get; }
        ReactiveAsyncCommand QueueAlbum { get; }
        ReactiveAsyncCommand ShowSongsFromArtist { get; }
        ReactiveAsyncCommand ShowSongsFromAlbum { get; }
    }

    public class SongTileViewModel : ReactiveObject, ISongTileViewModel
    {
        public Song Model { get; protected set; }

        ObservableAsPropertyHelper<BitmapImage> _AlbumArt;
        public BitmapImage AlbumArt {
            get { return _AlbumArt.Value; }
        }

        public ReactiveAsyncCommand QueueSong { get; protected set; }
        public ReactiveAsyncCommand QueueAlbum { get; protected set; }
        public ReactiveAsyncCommand ShowSongsFromArtist { get; protected set; }
        public ReactiveAsyncCommand ShowSongsFromAlbum { get; protected set; }

        public SongTileViewModel(Song model, IPlayApi playApi)
        {
            Model = model;

            playApi.FetchImageForAlbum(model).ToProperty(this, x => x.AlbumArt);

            QueueSong = new ReactiveAsyncCommand();
            QueueAlbum = new ReactiveAsyncCommand();

            QueueSong.RegisterAsyncObservable(_ => playApi.QueueSong(Model))
                .Subscribe(
                    x => this.Log().Info("Queued {0}", Model.name),
                    ex => this.Log().WarnException("Failed to queue", ex));

            QueueAlbum.RegisterAsyncObservable(_ => playApi.AllSongsOnAlbum(Model.artist, Model.album))
                .SelectMany(x => x.ToObservable())
                .Select(x => reallyTryToQueueSong(playApi, x)).Concat()
                .Subscribe(
                    x => this.Log().Info("Queued song"),
                    ex => this.Log().WarnException("Failed to queue album", ex));
        }

        IObservable<Unit> reallyTryToQueueSong(IPlayApi playApi, Song song)
        {
            return Observable.Defer(() => playApi.QueueSong(song))
                .Timeout(TimeSpan.FromSeconds(20), RxApp.TaskpoolScheduler)
                .Retry(3);
        }
    }
}