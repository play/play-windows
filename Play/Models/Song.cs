using System;
using System.Collections.Generic;
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
    public class Song : IEquatable<Song>
    {
// ReSharper disable InconsistentNaming
        public string album { get; set; }
        public bool starred { get; set; }
        public bool queued { get; set; }
        public string artist { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public string last_played { get; set; }
// ReSharper restore InconsistentNaming

        public DateTimeOffset? LastPlayedAsDate {
            get {
                if (String.IsNullOrWhiteSpace(last_played)) {
                    return null;
                }

                return DateTimeOffset.Parse(last_played);
            } 
        }

        public bool Equals(Song other)
        {
            return this.id == other.id;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SongQueue
    {
// ReSharper disable InconsistentNaming
        public List<Song> songs { get; set; }
// ReSharper restore InconsistentNaming

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}