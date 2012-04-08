using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Play.Views;
using RestSharp;
using Xunit;

namespace Play.Tests.Views
{
    public class PlayViewTests
    {
        [Fact]
        public void GetUriFromRestClientTest()
        {
            var test = new RestClient("https://play.yourcompany.com");
            PlayView.GetUriFromRestClient(test).Should().Be("http://play.yourcompany.com:8000/listen");
        }
    }
}
