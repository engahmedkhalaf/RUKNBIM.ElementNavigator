using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace RUKNBIM.ElementNavigator
{
    public partial class ElementNavigatorView : UserControl
    {
        public ElementNavigatorView()
        {
            InitializeComponent();
            DataContext = new ElementNavigatorViewModel();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
