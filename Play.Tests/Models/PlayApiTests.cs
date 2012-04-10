using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using FluentAssertions;
using Ninject;
using Ninject.MockingKernel.Moq;
using Play.Models;
using ReactiveUI;
using RestSharp;
using Xunit;

namespace Play.Tests.Models
{
    public class PlayApiTests : IEnableLogger
    {
        [Fact]
        public void FetchNowPlayingIntegrationTest()
        {
            var kernel = new MoqMockingKernel();
            var client = new RestClient(IntegrationTestUrl.Current);

            client.AddDefaultParameter("login", "xpaulbettsx");
            kernel.Bind<IBlobCache>().To<TestBlobCache>();

            var api = new PlayApi(client, kernel.Get<IBlobCache>());

            var result = api.NowPlaying()
                .Timeout(TimeSpan.FromSeconds(9.0), RxApp.TaskpoolScheduler)
                .First();

            this.Log().Info(result.ToString());
            result.id.Should().NotBeNullOrEmpty();
        }
    }
}
