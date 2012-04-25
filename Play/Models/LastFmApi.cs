using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace Play.Models
{
    public interface ILastFmApi : IReactiveNotifyPropertyChanged, IDisposable
    {
        bool CanScrobble { get; }
        IObservable<Unit> BeginAuthorize(IObservable<Unit> retrySessionHint = null);
        IObservable<Unit> Scrobble(Song song);
    }

    public class LastFmApiHelper : ReactiveObject, ILastFmApi
    {
        IDisposable inner;

        public bool CanScrobble {
            get { throw new NotImplementedException(); }
        }

        public IObservable<Unit> BeginAuthorize(IObservable<Unit> retrySessionHint = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> Scrobble(Song song)
        {
            throw new NotImplementedException();
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
