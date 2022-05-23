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

namespace De.Hochstaetter.FroniusMonitor.Assets.Images
{
    /// <summary>
    /// Interaction logic for VisibilityIcon.xaml
    /// </summary>
    public partial class VisibilityIcon
    {
        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register
        (
            nameof(Visible), typeof(bool), typeof(VisibilityIcon)
        );

        public bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }


        public VisibilityIcon()
        {
            InitializeComponent();
        }
    }
}
