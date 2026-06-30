using System.Windows.Controls;

namespace RUKNBIM.ElementNavigator
{
    public partial class ElementNavigatorView : UserControl
    {
        public ElementNavigatorView()
        {
            InitializeComponent();
            DataContext = new ElementNavigatorViewModel();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
    }
}
