using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
using PusherClientDotNet;
using ReactiveUI;
using Xunit;

namespace Play.Tests.Models
{
    public class PusherHelperTests
    {
        [Fact(Skip = "This test is super slow and is for debug purposes")]
        public void PusherIntegrationSmokeTest()
        {
#if FALSE
            Func<Pusher> factory = () => new Pusher(IntegrationTestUrl.PusherApiKey);

            var result = PusherHelper.Connect<Dictionary<string, object>>(factory, "now_playing_updates", "update_now_playing")
                .Timeout(TimeSpan.FromMinutes(4.0), RxApp.TaskpoolScheduler)
                .Take(1)
                /*
                .Select(x =>
                    new Tuple<Song, List<Song>>(
                        JsonConvert.DeserializeObject<Song>(x["now_playing"].ToString()),
                        JsonConvert.DeserializeObject<SongQueue>(x["songs"].ToString()).songs))
                */
                .First();

            result.Should().NotBeNull();
#endif
        }
    }
}
