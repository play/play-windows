using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Akavache;
using Ninject;
using ReactiveUI;
using RestSharp;

namespace Play.Models
{
    public interface IPlayApi
    {
        IObservable<Song> NowPlaying();
        IObservable<BitmapImage> FetchImageForAlbum(Song song);
        IObservable<string> ListenUrl();
    }

    public class PlayApi : IPlayApi
    {
        readonly IRestClient client;
        readonly IBlobCache cache;

        [Inject]
        public PlayApi(IRestClient authedClient, [Named("LocalMachine")] IBlobCache blobCache)
        {
            client = authedClient;
            cache = blobCache;
        }

        public IObservable<Song> NowPlaying()
        {
            var rq = new RestRequest("now_playing");
            return client.RequestAsync<Song>(rq).Select(x => x.Data);
        }

        public IObservable<BitmapImage> FetchImageForAlbum(Song song)
        {
            var url = String.Format("images/art/{0}.png", song.id);
            var rq = new RestRequest();
            return cache.LoadImageFromUrl(client.BuildUri(rq).ToString());
        }

        public IObservable<string> ListenUrl()
        {
            var uri = new Uri(client.BaseUrl);
            return Observable.Return(
                String.Format("{0}:8000/listen", uri.GetLeftPart(UriPartial.Authority).Replace("https", "http")));
        }
    }
}
