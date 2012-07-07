using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Windows;
using ReactiveUI;
using ReactiveUI.Xaml;

namespace Play.ViewModels
{
    public interface IBackgroundTaskHostViewModel : IReactiveNotifyPropertyChanged
    {
        ReactiveCollection<IBackgroundTaskTileViewModel> BackgroundTasks { get; }
        Visibility ShouldShowBackgroundTaskPane { get; }
    }

    public interface IBackgroundTaskTileViewModel : IReactiveNotifyPropertyChanged
    {
        int CurrentProgress { get; }
        string CurrentText { get; set; }
        object Tag { get; set; }

        ReactiveCommand Cancel { get; }
    }

    public class BackgroundTaskHostViewModel : ReactiveObject, IBackgroundTaskHostViewModel
    {
        public ReactiveCollection<IBackgroundTaskTileViewModel> BackgroundTasks { get; protected set; }

        ObservableAsPropertyHelper<Visibility> _ShouldShowBackgroundTaskPane;
        public Visibility ShouldShowBackgroundTaskPane {
            get { return _ShouldShowBackgroundTaskPane.Value; }
        }

        public BackgroundTaskHostViewModel()
        {
            BackgroundTasks = new ReactiveCollection<IBackgroundTaskTileViewModel>();

            BackgroundTasks.CollectionCountChanged
                .Select(x => x == 0 ? Visibility.Visible : Visibility.Hidden)
                .ToProperty(this, x => x.ShouldShowBackgroundTaskPane);
        }
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

        public ReactiveCommand Cancel { get; protected set; }

        public BackgroundTaskTileViewModel(IObservable<int> progress)
        {
            progress.ToProperty(this, x => x.CurrentProgress);
            Cancel = new ReactiveCommand();
        }

        public static DisposableContainer<IBackgroundTaskTileViewModel> Create(IObservable<int> progress, string captionText, IDisposable workSubscription)
        {
            var prg = progress.Multicast(new Subject<int>());
            var ret = new BackgroundTaskTileViewModel(prg) {CurrentText = captionText};

            var collection = RxApp.GetService<IBackgroundTaskHostViewModel>().BackgroundTasks;

            prg.ObserveOn(RxApp.DeferredScheduler).Subscribe(
                x => { },
                ex => { collection.Remove(ret); UserError.Throw(new BackgroundTaskUserError("Transfer Failed", ex)); },
                () => collection.Remove(ret));

            var disp = prg.Connect();

            collection.Add(ret);

            var container = DisposableContainer.Create((IBackgroundTaskTileViewModel)ret, 
                new CompositeDisposable(disp, workSubscription ?? Disposable.Empty));

            ret.Cancel.Subscribe(_ => container.Dispose());

            return container;
        }
    }

    public static class DisposableContainer
    {
        public static DisposableContainer<T1> Create<T1>(T1 value, IDisposable disposable)
        {
            return new DisposableContainer<T1>(value, disposable);
        }
    }

    public sealed class DisposableContainer<T> : IDisposable
    {
        IDisposable _inner;

        public T Value { get; private set; }

        public DisposableContainer(T value, IDisposable disposable)
        {
            Value = value;
            _inner = disposable;
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref _inner, null);
            if (disp != null) {
                disp.Dispose();
            }
        }
    }
}
