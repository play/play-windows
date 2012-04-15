using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Play.Models;

namespace Play.Tests
{
    public static class Fakes
    {
        const string albumJson = "{\"songs\":[{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"Get Innocuous!\",\"id\":\"4B52884E1E293890\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"Time To Get Away\",\"id\":\"044198D2344B2CAE\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"North American Scum\",\"id\":\"2659D99BCA5BF132\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"Someone Great\",\"id\":\"EDC2A0C6F45A31C3\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"All My Friends\",\"id\":\"F85FDE2A63393803\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"Us V Them\",\"id\":\"BA41D8967BBDD1B6\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"Watch The Tapes\",\"id\":\"4DBE327B3EAE10AA\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"Sound Of Silver\",\"id\":\"ADFE3123DC34FCD9\",\"artist\":\"LCD Soundsystem\"},{\"starred\":false,\"album\":\"Sound Of Silver\",\"queued\":false,\"name\":\"New York, I Love You But You're Bringing Me Down\",\"id\":\"9EA31D331E9ED436\",\"artist\":\"LCD Soundsystem\"}]}";

        public static List<Song> GetAlbum()
        {
            return JsonConvert.DeserializeObject<SongQueue>(albumJson).songs;
        }

        public static Song GetSong()
        {
            return GetAlbum().First();
        }
    }
}
