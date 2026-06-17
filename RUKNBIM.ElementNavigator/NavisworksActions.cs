using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using System.Linq;

namespace RUKNBIM.ElementNavigator
{
    public static class NavisworksActions
    {
        public static void HighlightAndSelect(Document doc, ModelItemCollection items, bool isolate, bool zoom)
        {
            // Clear current selection
            doc.CurrentSelection.Clear();

            if (items.Count > 0)
            {
                // Select items natively
                doc.CurrentSelection.CopyFrom(items);

                if (isolate)
                {
                    // Reset hidden state first
                    doc.Models.ResetAllHidden();

                    // Ultra-fast isolate: Hide only the siblings of the selected items and their ancestors.
                    // This avoids searching through millions of elements.
                    var toHide = new ModelItemCollection();
                    var allAncestors = new ModelItemCollection();
                    
                    foreach (var item in items)
                    {
                        allAncestors.AddRange(item.AncestorsAndSelf);
                    }

                    foreach (var item in items)
                    {
                        var ancestors = item.AncestorsAndSelf.ToList();
                        foreach (var ancestor in ancestors)
                        {
                            if (ancestor.Parent != null)
                            {
                                foreach (var sibling in ancestor.Parent.Children)
                                {
                                    if (!allAncestors.Contains(sibling))
                                    {
                                        toHide.Add(sibling);
                                    }
                                }
                            }
                            else
                            {
                                // Root level siblings
                                foreach (var model in doc.Models)
                                {
                                    if (!allAncestors.Contains(model.RootItem))
                                    {
                                        toHide.Add(model.RootItem);
                                    }
                                }
                            }
                        }
                    }

                    if (toHide.Count > 0)
                    {
                        doc.Models.SetHidden(toHide, true);
                    }
                }

                if (zoom)
                {
                    // Use native C++ engine to zoom to selection
                    dynamic state = ComApiBridge.State;
                    state.ZoomInCurViewOnCurSel();
                }
            }
        }
    }
}
