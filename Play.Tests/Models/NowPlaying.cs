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
    public class NowPlayingTests : IEnableLogger
    {
        const string baseUrl = "https://play.yourcompany.com";

        [Fact]
        public void FetchNowPlayingIntegrationTest()
        {
            var kernel = new MoqMockingKernel();
            var client = new RestClient(baseUrl);
            kernel.Bind<IBlobCache>().To<TestBlobCache>();

            var result = NowPlayingHelper.FetchCurrent(client, kernel.Get<IBlobCache>()).First();

            this.Log().Info(result.ToString());
            result.id.Should().NotBeNullOrEmpty();
        }
    }
}
