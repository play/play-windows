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
	/// Interaction logic for BackgroundTaskTileView.xaml
	/// </summary>
	public partial class BackgroundTaskTileView : UserControl, IViewForViewModel<BackgroundTaskTileViewModel>
	{
		public BackgroundTaskTileView()
		{
			this.InitializeComponent();
		}

        public BackgroundTaskTileViewModel ViewModel {
            get { return (BackgroundTaskTileViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(BackgroundTaskTileViewModel), typeof(BackgroundTaskTileView), new UIPropertyMetadata(null));

	    object IViewForViewModel.ViewModel { get { return ViewModel; } set { ViewModel = (BackgroundTaskTileViewModel)value; } }
	}
}