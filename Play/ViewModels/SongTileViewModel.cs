using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
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
        bool IsStarred { get; set; }
        Visibility QueueSongVisibility { get; set; }

        ReactiveCommand QueueSong { get; }
        ReactiveCommand QueueAlbum { get; }

        ReactiveCommand ShowSongsFromArtist { get; }
        ReactiveCommand ShowSongsFromAlbum { get; }

        ReactiveCommand ToggleStarred { get; }
    }

    public class SongTileViewModel : ReactiveObject, ISongTileViewModel
    {
        public Song Model { get; protected set; }

        ObservableAsPropertyHelper<BitmapImage> _AlbumArt;
        public BitmapImage AlbumArt {
            get { return _AlbumArt.Value; }
        }

        bool _IsStarred;
        public bool IsStarred {
            get { return _IsStarred; }
            set { this.RaiseAndSetIfChanged(ref _IsStarred, value); }
        }

        Visibility _QueueSongVisibility;
        public Visibility QueueSongVisibility {
            get { return _QueueSongVisibility; }
            set { this.RaiseAndSetIfChanged(ref _QueueSongVisibility, value); }
        }

        public ReactiveCommand QueueSong { get; protected set; }
        public ReactiveCommand QueueAlbum { get; protected set; }

        public ReactiveCommand ShowSongsFromArtist { get; protected set; }
        public ReactiveCommand ShowSongsFromAlbum { get; protected set; }

        public ReactiveCommand ToggleStarred { get; protected set; }

        public SongTileViewModel(Song model, IPlayApi playApi)
        {
            Model = model;

            playApi.FetchImageForAlbum(model)
                .LoggedCatch(this, Observable.Return(default(BitmapImage)))
                .ToProperty(this, x => x.AlbumArt, out _AlbumArt);

            QueueSong = new ReactiveCommand();
            QueueAlbum = new ReactiveCommand();

            QueueSong.RegisterAsync(_ => playApi.QueueSong(Model))
                .Subscribe(
                    x => this.Log().Info("Queued {0}", Model.name),
                    ex => this.Log().WarnException("Failed to queue", ex));

            QueueAlbum.RegisterAsync(_ => playApi.AllSongsOnAlbum(Model.artist, Model.album))
                .SelectMany(x => x.ToObservable())
                .Select(x => reallyTryToQueueSong(playApi, x)).Concat()
                .Subscribe(
                    x => this.Log().Info("Queued song"),
                    ex => this.Log().WarnException("Failed to queue album", ex));

            QueueAlbum.ThrownExceptions.Subscribe(x => { });

            IsStarred = model.starred;
            ToggleStarred = new ReactiveCommand();

            ToggleStarred.RegisterAsync(_ => IsStarred ? playApi.Unstar(Model) : playApi.Star(Model))
                .Select(_ => true).LoggedCatch(this, Observable.Return(false))
                .Subscribe(result => {
                    if (result) IsStarred = !IsStarred;
                }, ex => this.Log().WarnException("Couldn't star/unstar song", ex));

            ToggleStarred.ThrownExceptions.Subscribe(x => { });
        }

        IObservable<Unit> reallyTryToQueueSong(IPlayApi playApi, Song song)
        {
            return Observable.Defer(() => playApi.QueueSong(song))
                .Timeout(TimeSpan.FromSeconds(20), RxApp.TaskpoolScheduler)
                .Retry(3);
        }
    }
}