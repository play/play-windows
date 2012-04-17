using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Play.Views
{
    /// <summary>
    /// Interaction logic for SongTileView.xaml
    /// </summary>
    public partial class SongTileView : UserControl, IViewForViewModel<SongTileViewModel>
    {
        public SongTileViewModel ViewModel {
            get { return (SongTileViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SongTileViewModel), typeof(SongTileView), new UIPropertyMetadata(null));

        public SongTileView()
        {
            InitializeComponent();
        }

        object IViewForViewModel.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (SongTileViewModel)value; }
        }
    }
}
