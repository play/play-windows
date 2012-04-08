using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Akavache;
using Newtonsoft.Json;
using Ninject;
using Play.ViewModels;
using RestSharp;

namespace Play.Models
{
    public class NowPlaying
    {
// ReSharper disable InconsistentNaming
        public string album { get; set; }
        public bool starred { get; set; }
        public bool queued { get; set; }
        public string artist { get; set; }
        public string name { get; set; }
        public string id { get; set; }
// ReSharper restore InconsistentNaming

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public static class NowPlayingHelper
    {
        public static IObservable<NowPlaying> FetchCurrent(IRestClient client, IBlobCache localMachineCache = null)
        {
            var user = client.DefaultParameters.FirstOrDefault(x => x.Name == "login").Value;
            var url = String.Format("{0}/now_playing?login={1}", client.BaseUrl, user);
            localMachineCache = localMachineCache ?? AppBootstrapper.Kernel.Get<IBlobCache>("LocalMachine");

            return localMachineCache.DownloadUrl(url, null, true)
                .Select(x => Encoding.UTF8.GetString(x))
                .Do(Console.WriteLine)
                .Select(JsonConvert.DeserializeObject<NowPlaying>);
        }

        public static IObservable<BitmapImage> FetchImageForAlbum(this NowPlaying This, IRestClient client, IBlobCache localMachineCache = null)
        {
            var user = client.DefaultParameters.FirstOrDefault(x => x.Name == "login").Value;
            var url = String.Format("{0}/images/art/{1}.png?login={2}", client.BaseUrl, This.id, user);
            localMachineCache = localMachineCache ?? AppBootstrapper.Kernel.Get<IBlobCache>("LocalMachine");

            return localMachineCache.LoadImageFromUrl(url);
        }
    }
}