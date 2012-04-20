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
    /// Interaction logic for SearchViewModel.xaml
    /// </summary>
    public partial class SearchView : UserControl, IViewForViewModel<SearchViewModel>
    {
        public SearchView()
        {
            InitializeComponent();
        }

        public SearchViewModel ViewModel {
            get { return (SearchViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SearchViewModel), typeof(SearchView), new UIPropertyMetadata(null));

        object IViewForViewModel.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (SearchViewModel)value; }
        }
    }
}
