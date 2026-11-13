using Microsoft.DotNet.DesignTools.Editors;
using System;
using System.ComponentModel.Design;

namespace RibbonLib
{
    public class RibbonOrbRecentItemCollectionEditor : CollectionEditor
    {
        public RibbonOrbRecentItemCollectionEditor(IServiceProvider serviceProvider)
    : base(serviceProvider, typeof(RibbonOrbRecentItemCollection))
        {
        }

        protected override Type CreateCollectionItemType()
        {
            return typeof(RibbonOrbRecentItem);
        }

        protected override object CreateInstance(Type itemType)
        {
            // The editor can ask for the collection's element type, and RibbonItem is
            // abstract. Fall back to the concrete item type instead of throwing.
            if (itemType == null || itemType.IsAbstract)
            {
                itemType = typeof(RibbonOrbRecentItem);
            }

            return base.CreateInstance(itemType);
        }

        protected override Type[] CreateNewItemTypes()
        {
            return new[]
            {
                typeof(RibbonOrbRecentItem),
                typeof(RibbonSeparator)
            };
        }
    }
}
