using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Play
{
    public class FadeImageButton : Button
    {
        public object HighlightContent {
            get { return (object)GetValue(HighlightContentProperty); }
            set { SetValue(HighlightContentProperty, value); }
        }
        public static readonly DependencyProperty HighlightContentProperty =
            DependencyProperty.Register("HighlightContent", typeof(object), typeof(FadeImageButton), new UIPropertyMetadata(null));
    }

    public class FadeImageToggleButton : ToggleButton
    {
        public object HighlightContent {
            get { return (object)GetValue(HighlightContentProperty); }
            set { SetValue(HighlightContentProperty, value); }
        }
        public static readonly DependencyProperty HighlightContentProperty =
            DependencyProperty.Register("HighlightContent", typeof(object), typeof(FadeImageToggleButton), new UIPropertyMetadata(null));
    }
}
