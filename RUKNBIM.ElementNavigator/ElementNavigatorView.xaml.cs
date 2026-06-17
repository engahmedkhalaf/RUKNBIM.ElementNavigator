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
    }
}
