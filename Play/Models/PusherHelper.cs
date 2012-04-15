using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using PusherClientDotNet;

namespace Play.Models
{
    public static class PusherHelper
    {
        public static IObservable<T> Connect<T>(Func<Pusher> pusherFactory, string channel, string eventName)
        {
            var pusher = pusherFactory();
            var disp = new CompositeDisposable();

            return Observable.Create<T>(subj => {
                bool hasCompleted = false;
                try {
                    pusher.Connect();
                    disp.Add(Disposable.Create(pusher.Disconnect));

                    var ch = pusher.Subscribe(channel);
                    disp.Add(Disposable.Create(() => pusher.Unsubscribe(channel)));

                    ch.Bind(eventName, x => {
                        if (hasCompleted) return;
                        subj.OnNext((T)x);
                    });

                    disp.Add(Disposable.Create(ch.Disconnect));
                } catch (Exception ex) {
                    subj.OnError(ex);
                    hasCompleted = true;
                }

                return disp;
            });
        }
    }
}
