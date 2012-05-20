using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using ReactiveUI;
using ReactiveUI.Xaml;

namespace Play.ViewModels
{
    public interface IBackgroundTaskTileViewModel : IReactiveNotifyPropertyChanged
    {
        int CurrentProgress { get; }
        string CurrentText { get; set; }
        object Tag { get; set; }
    }

    public class BackgroundTaskUserError : UserError
    {
        public BackgroundTaskUserError(string errorMessage, Exception innerException) : base(errorMessage, innerException: innerException)
        {
            RecoveryOptions.Add(new RecoveryCommand("Retry", _ => RecoveryOptionResult.RetryOperation));
            RecoveryOptions.Add(RecoveryCommand.Cancel);
        }
    }

    public class BackgroundTaskTileViewModel : ReactiveObject, IBackgroundTaskTileViewModel
    {
        ObservableAsPropertyHelper<int> _CurrentProgress;
        public int CurrentProgress {
            get { return _CurrentProgress.Value; }
        }

        string _CurrentText;
        public string CurrentText {
            get { return _CurrentText; } 
            set { this.RaiseAndSetIfChanged(x => x.CurrentText, value); }
        }

        object _Tag;
        public object Tag {
            get { return _Tag; }
            set { this.RaiseAndSetIfChanged(x => x.Tag, value); }
        }

        public BackgroundTaskTileViewModel(IObservable<int> progress)
        {
            progress.ToProperty(this, x => x.CurrentProgress);
        }

        public static IBackgroundTaskTileViewModel Create(ReactiveCollection<IBackgroundTaskTileViewModel> collection, IObservable<int> progress, string captionText)
        {
            var prg = progress.Multicast(new Subject<int>());

            var ret = new BackgroundTaskTileViewModel(prg) {CurrentText = captionText};

            prg.ObserveOn(RxApp.DeferredScheduler).Subscribe(
                x => { },
                ex => { collection.Remove(ret); UserError.Throw(new BackgroundTaskUserError("Transfer Failed", ex)); },
                () => collection.Remove(ret));
            prg.Connect();

            collection.Add(ret);
            return ret;
        }
    }
}
