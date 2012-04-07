using System;
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

        public static IObservable<NowPlaying> FetchCurrent(IRestClient client)
        {
            var url = String.Format("{0}/now_playing?login=hubot", client.BaseUrl);
            var localMachineCache = AppBootstrapper.Kernel.Get<IBlobCache>("LocalMachine");

            return localMachineCache.DownloadUrl(url, null, true)
                .Select(x => Encoding.UTF8.GetString(x))
                .Do(Console.WriteLine)
                .Select(JsonConvert.DeserializeObject<NowPlaying>);
        }

        public IObservable<BitmapImage> FetchImageForAlbum(IRestClient client)
        {
            var url = String.Format("{0}/images/art/{1}.png?login=hubot", client.BaseUrl, id);
            var localMachineCache = AppBootstrapper.Kernel.Get<IBlobCache>("LocalMachine");

            return localMachineCache.LoadImageFromUrl(url);
        }
    }
}