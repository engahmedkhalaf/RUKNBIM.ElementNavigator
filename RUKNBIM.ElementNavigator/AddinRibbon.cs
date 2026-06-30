using Autodesk.Navisworks.Api.Plugins;

namespace RUKNBIM.ElementNavigator
{
    [PluginAttribute("ElementNavigatorAddin", "RUKN", DisplayName = "Element Navigator", ToolTip = "Open Element Navigator")]
    [AddInPluginAttribute(AddInLocation.AddIn)]
    public class AddinRibbon : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            var pluginRecord = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin("ElementNavigator.RUKN");
            if (pluginRecord != null)
            {
                if (!pluginRecord.IsLoaded)
                {
                    pluginRecord.LoadPlugin();
                }

                if (pluginRecord.LoadedPlugin is DockPanePlugin dockPane)
                {
                    dockPane.Visible = true;
                }
            }
            return 0;
        }
    }
}
