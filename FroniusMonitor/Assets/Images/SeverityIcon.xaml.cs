using System.Windows;
using De.Hochstaetter.Fronius.Models.Gen24;

namespace De.Hochstaetter.FroniusMonitor.Assets.Images
{
    public partial class SeverityIcon
    {
        public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register
        (
            nameof(Severity), typeof(Severity), typeof(SeverityIcon)
        );

        public Severity Severity
        {
            get => (Severity)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        public SeverityIcon()
        {
            InitializeComponent();
        }
    }
}
