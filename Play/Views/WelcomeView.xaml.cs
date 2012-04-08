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
    /// Interaction logic for WelcomeViewModel.xaml
    /// </summary>
    public partial class WelcomeView : UserControl, IViewForViewModel<WelcomeViewModel>
    {
        public WelcomeView()
        {
            InitializeComponent();
        }

        public WelcomeViewModel ViewModel {
            get { return (WelcomeViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(WelcomeViewModel), typeof(WelcomeView), new UIPropertyMetadata(null));

        object IViewForViewModel.ViewModel {
            get { return ViewModel; }
            set { ViewModel = (WelcomeViewModel)value; }
        }
    }
}
