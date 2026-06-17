using Autodesk.Navisworks.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RUKNBIM.ElementNavigator
{
    public class NavisworksSearchEngine
    {
        private Dictionary<string, ModelItem> _elementCache;

        public void BuildCache(Document doc)
        {
            _elementCache = new Dictionary<string, ModelItem>(StringComparer.OrdinalIgnoreCase);
            
            // Traverse all items to build cache
            foreach (var item in doc.Models.SelectMany(m => m.RootItem.DescendantsAndSelf))
            {
                string id = GetRevitId(item);
                if (!string.IsNullOrEmpty(id) && !_elementCache.ContainsKey(id))
                {
                    _elementCache[id] = item;
                }
            }
        }

        public ModelItemCollection FindElements(IEnumerable<string> ids)
        {
            var collection = new ModelItemCollection();
            if (_elementCache == null) return collection;

            foreach (var id in ids)
            {
                if (_elementCache.TryGetValue(id, out var item))
                {
                    collection.Add(item);
                }
            }
            return collection;
        }

        public List<string> GetMissingIds(IEnumerable<string> ids)
        {
            var missing = new List<string>();
            if (_elementCache == null) return ids.ToList();

            foreach (var id in ids)
            {
                if (!_elementCache.ContainsKey(id))
                {
                    missing.Add(id);
                }
            }
            return missing;
        }

        private string GetRevitId(ModelItem item)
        {
            // Try different common properties for Revit Element ID in Navisworks
            var prop = item.PropertyCategories.FindPropertyByDisplayName("Item", "Element Id");
            if (prop != null && prop.Value != null)
                return prop.Value.ToDisplayString();

            prop = item.PropertyCategories.FindPropertyByDisplayName("Element ID", "Value");
            if (prop != null && prop.Value != null)
                return prop.Value.ToDisplayString();

            return null;
        }
    }
}
