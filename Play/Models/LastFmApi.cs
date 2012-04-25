using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akavache;
using Lpfm.LastFmScrobbler;
using Ninject;
using ReactiveUI;

namespace Play.Models
{
    public interface ILastFmApi : IReactiveNotifyPropertyChanged, IDisposable
    {
        bool CanScrobble { get; }
        IObservable<Unit> BeginAuthorize(IObservable<Unit> retrySessionHint = null, bool canShowAuthUi = true);
        IObservable<Unit> Scrobble(Song song);
    }

    public class LastFmApiHelper : ReactiveObject, ILastFmApi
    {
        IDisposable inner;
        readonly IBlobCache userCache;
        Scrobbler scrobbler;

        const string apiKey = "94d5da4eae83c83cc5c3444095b9d55a";
        const string apiSecret = "3b434aa2f8d60ef88a138aeabcec7e93";

        public bool CanScrobble {
            get { throw new NotImplementedException(); }
        }

        public LastFmApiHelper([Named("UserAccount")] IBlobCache userCache)
        {
            this.userCache = userCache;
        }

        public IObservable<Unit> BeginAuthorize(IObservable<Unit> retrySessionHint = null, bool canShowAuthUi = true)
        {
            var showAuthUiAndPollOnTimer = Observable.Return(new Scrobbler(apiKey, apiSecret))
                .Do(x => Process.Start(x.GetAuthorisationUri()))
                .SelectMany(x => Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), RxApp.TaskpoolScheduler).Select(_ => x).Take(60))
                .SelectMany(x => Observable.Start(() => x.GetSession(), RxApp.TaskpoolScheduler).Catch(Observable.Return<string>(null)))
                .Where(x => x != null)
                .Take(1);

            return userCache.GetObjectAsync<string>("lastFm")
                .Select(x => new Scrobbler(apiKey, apiSecret, x))
                .Do(x => scrobbler = x).Select(_ => Unit.Default)
                .LoggedCatch(this, canShowAuthUi ? 
                    showAuthUiAndPollOnTimer.Do(x => scrobbler = new Scrobbler(apiKey, apiSecret, x)).Select(_ => Unit.Default) : 
                    Observable.Throw<Unit>(new Exception("last.fm has not been authorized")));
        }

        public IObservable<Unit> Scrobble(Song song)
        {
            return Observable.Start(() => {
                // XXX: Track length is a required parameter, and we also have 
                // to prove that we scrobbled the track at least 'n' seconds
                // after it started.
                scrobbler.Scrobble(new Track() { ArtistName = song.artist, AlbumName = song.album, TrackName = song.name });
            });
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref inner, null);
            if (disp != null) {
                disp.Dispose();
            }
        }
    }
}
