using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Ninject;
using Play.Models;
using ReactiveUI;
using ReactiveUI.Routing;
using ReactiveUI.Xaml;
using RestSharp;

namespace Play.ViewModels
{
    public interface IWelcomeViewModel : IRoutableViewModel
    {
        string BaseUrl { get; set; }
        string Token { get; set; }
        string ErrorMessage { get; }
        ReactiveCommand OkButton { get; }
        ReactiveCommand OpenTokenPage { get; }
    }

    public class WelcomeViewModel : ReactiveObject, IWelcomeViewModel
    {
        string _BaseUrl;
        public string BaseUrl {
            get { return _BaseUrl; }
            set { this.RaiseAndSetIfChanged(x => x.BaseUrl, value); }
        }

        string _Token;
        public string Token {
            get { return _Token; }
            set { this.RaiseAndSetIfChanged(x => x.Token, value); }
        }

        ObservableAsPropertyHelper<string> _ErrorMessage;
        public string ErrorMessage {
            get { return _ErrorMessage.Value; }
        }

        public ReactiveCommand OkButton { get; protected set; }
        public ReactiveCommand OpenTokenPage { get; protected set; }

        public string UrlPathSegment {
            get { return "login"; }
        }

        public IScreen HostScreen { get; protected set; }

        [Inject]
        public WelcomeViewModel(
            IScreen screen, 
            ILoginMethods loginMethods,
            [Named("connectToServer")] [Optional] Func<string, string, IObservable<Unit>> connectToServerMock)
        {
            HostScreen = screen;

            var canOk = this.WhenAny(x => x.BaseUrl, x => x.Token,
                (b, u) => isValidUrl(b.Value) && !String.IsNullOrWhiteSpace(u.Value));

            OkButton = new ReactiveCommand(canOk);

            OpenTokenPage = new ReactiveCommand(this.WhenAny(x => x.BaseUrl, x => isValidUrl(x.Value)));

            var connectToServer = connectToServerMock ?? ConnectToPlay;

            Observable.Defer(() => OkButton.SelectMany(_ => connectToServer(BaseUrl, Token)))
                .Select(_ => true).Catch(Observable.Return(false))
                .Repeat()
                .Subscribe(result => {
                    if (result == false) {
                        UserError.Throw("Couldn't connect to Play instance.");
                        return;
                    }

                    loginMethods.SaveCredentials(BaseUrl, Token);
                    screen.Router.NavigateBack.Execute(null);
                });

            OpenTokenPage.Subscribe(_ => Process.Start(String.Format("{0}/token", BaseUrl)));

            var error = new Subject<string>();
            UserError.RegisterHandler(ex => {
                error.OnNext(ex.ErrorMessage);
                return Observable.Return(RecoveryOptionResult.CancelOperation);
            });

            this.WhenAny(x => x.Token, x => x.BaseUrl, (_, __) => Unit.Default)
                .Subscribe(_ => error.OnNext(null));

            error.ToProperty(this, x => x.ErrorMessage);
        }

        bool isValidUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url)) return false;

            try {
                var dontcare = new Uri(url);
                this.Log().Debug("Entered {0}", url);
            } catch (Exception ex) {
                return false;
            }

            return url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase);
        }

        public IObservable<Unit> ConnectToPlay(string baseUrl, string token)
        {
            var client = new RestClient(baseUrl);
            client.AddDefaultHeader("Authorization", token);

            var api = new PlayApi(client, null);
            return api.NowPlaying().Select(_ => Unit.Default);
        }
    }
}
