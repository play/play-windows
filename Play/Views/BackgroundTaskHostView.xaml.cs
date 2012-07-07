using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Play.ViewModels;
using ReactiveUI.Routing;

namespace Play
{
	/// <summary>
	/// Interaction logic for BackgroundTaskView.xaml
	/// </summary>
	public partial class BackgroundTaskHostView : UserControl, IViewForViewModel<IBackgroundTaskHostViewModel>
	{
		public BackgroundTaskHostView()
		{
			this.InitializeComponent();
		}

        public IBackgroundTaskHostViewModel ViewModel {
            get { return (IBackgroundTaskHostViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(IBackgroundTaskHostViewModel), typeof(BackgroundTaskHostView), new UIPropertyMetadata(null));

	    object IViewForViewModel.ViewModel { get { return ViewModel; } set { ViewModel = (IBackgroundTaskHostViewModel)value; } }
	}
}