using System;
using System.Collections.Generic;
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
        string Username { get; set; }
        string ErrorMessage { get; }
        ReactiveCommand OkButton { get; }
    }

    public class WelcomeViewModel : ReactiveObject, IWelcomeViewModel
    {
        string _BaseUrl;
        public string BaseUrl {
            get { return _BaseUrl; }
            set { this.RaiseAndSetIfChanged(x => x.BaseUrl, value); }
        }

        string _Username;
        public string Username {
            get { return _Username; }
            set { this.RaiseAndSetIfChanged(x => x.Username, value); }
        }

        ObservableAsPropertyHelper<string> _ErrorMessage;
        public string ErrorMessage {
            get { return _ErrorMessage.Value; }
        }

        public ReactiveCommand OkButton { get; protected set; }

        public string UrlPathSegment {
            get { return "login"; }
        }

        public IScreen HostScreen { get; protected set; }

        [Inject]
        public WelcomeViewModel(
            IScreen screen, 
            ISecureBlobCache credCache,
            [Named("connectToServer")] [Optional] Func<string, string, IObservable<Unit>> connectToServerMock)
        {
            HostScreen = screen;

            var canOk = this.WhenAny(x => x.BaseUrl, x => x.Username,
                (b, u) => isValidUrl(b.Value) && !String.IsNullOrWhiteSpace(u.Value));

            OkButton = new ReactiveCommand(canOk);

            var connectToServer = connectToServerMock ?? ConnectToPlay;

            Observable.Defer(() => OkButton.SelectMany(_ => connectToServer(BaseUrl, Username)))
                .Select(_ => true).Catch(Observable.Return(false))
                .Repeat()
                .Subscribe(result => {
                    if (result == false) {
                        UserError.Throw("Couldn't connect to Play instance.");
                        return;
                    }

                    credCache.InsertObject("BaseUrl", BaseUrl);
                    credCache.InsertObject("Username", Username);
                    screen.Router.NavigateBack.Execute(null);
                });

            var error = new Subject<string>();
            UserError.RegisterHandler(ex => {
                error.OnNext(ex.ErrorMessage);
                return Observable.Return(RecoveryOptionResult.CancelOperation);
            });

            this.WhenAny(x => x.Username, x => x.BaseUrl, (_, __) => Unit.Default)
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

        public IObservable<Unit> ConnectToPlay(string baseUrl, string username)
        {
            var client = new RestClient(baseUrl);
            client.AddDefaultParameter("login", username);

            var api = new PlayApi(client, null);
            return api.NowPlaying().Select(_ => Unit.Default);
        }
    }
}
