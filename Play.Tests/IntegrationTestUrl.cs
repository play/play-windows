using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Tests
{
    public static class IntegrationTestUrl
    {
        public static string Current {
            get {
                //return "https://play.yourcompany.com";
                throw new Exception("Configure IntegrationTestUrl.cs first");
            } 
        }

        public static string Token {
            get {
                //return "a04d03";
                throw new Exception("Configure IntegrationTestUrl.cs first");
            } 
        }

        public static string PusherApiKey {
            get {
                //return "a04d03ca023f0ab99a";
                throw new Exception("Configure IntegrationTestUrl.cs first");
            }
        }
    }
}
