using Autodesk.Navisworks.Api.Plugins;
using System.Windows.Forms.Integration;

namespace RUKNBIM.ElementNavigator
{
    [PluginAttribute("ElementNavigator", "RUKN", DisplayName = "RUKN Element Navigator", ToolTip = "Search and highlight elements by ID")]
    [DockPanePluginAttribute(250, 400, FixedSize = false)]
    public class DockPanePlugin : Autodesk.Navisworks.Api.Plugins.DockPanePlugin
    {
        public override System.Windows.Forms.Control CreateControlPane()
        {
            var host = new ElementHost();
            var view = new ElementNavigatorView();
            host.Child = view;
            host.Dock = System.Windows.Forms.DockStyle.Fill;
            return host;
        }

        public override void DestroyControlPane(System.Windows.Forms.Control pane)
        {
            pane.Dispose();
        }
    }
}
