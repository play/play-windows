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
    public class Song
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
}