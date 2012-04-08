using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using ReactiveUI;
using RestSharp;

namespace Play
{
    public static class RestSharpRxHelper
    {
        public static IObservable<RestResponse<T>> RequestAsync<T>(this IRestClient client, IRestRequest request) where T : new()
        {
            var ret = Observable.Start(() => client.Execute<T>(request), RxApp.TaskpoolScheduler);
            return ret.throwOnRestResponseFailure();
        }

        public static IObservable<RestResponse> RequestAsync(this IRestClient client, IRestRequest request)
        {
            var ret = Observable.Start(() => client.Execute(request), RxApp.TaskpoolScheduler);
            return ret.throwOnRestResponseFailure();
        }

        static IObservable<T> throwOnRestResponseFailure<T>(this IObservable<T> observable)
            where T : RestResponseBase
        {
            return observable.SelectMany(x =>
            {
                if (x == null)
                {
                    return Observable.Return(x);
                }

                if (x.ErrorException != null)
                {
                    return Observable.Throw<T>(x.ErrorException);
                }

                if (x.ResponseStatus == ResponseStatus.Error)
                {
                    LogHost.Default.Warn("Response Status failed for {0}: {1}", x.ResponseUri, x.ResponseStatus);
                    return Observable.Throw<T>(new Exception("Request Error"));
                }

                if (x.ResponseStatus == ResponseStatus.TimedOut)
                {
                    LogHost.Default.Warn("Response Status failed for {0}: {1}", x.ResponseUri, x.ResponseStatus);
                    return Observable.Throw<T>(new Exception("Request Timed Out"));
                }

                if ((int)x.StatusCode >= 400)
                {
                    LogHost.Default.Warn("Response Status failed for {0}: {1}", x.ResponseUri, x.StatusCode);
                    return Observable.Throw<T>(new WebException(x.Content));
                }

                if (x.ResponseStatus == ResponseStatus.Aborted)
                {
                    LogHost.Default.Warn("Response Status failed for {0}: {1}", x.ResponseUri, x.ResponseStatus);
                    return Observable.Throw<T>(new Exception("Request aborted"));
                }

                return Observable.Return(x);
            });
        }
    }
}