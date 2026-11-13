using Microsoft.DotNet.DesignTools.Editors;
using RibbonLib.Collections;
using System;
using System.ComponentModel.Design;

namespace RibbonLib
{
    public class RibbonOrbOptionButtonCollectionEditor : CollectionEditor
    {
        public RibbonOrbOptionButtonCollectionEditor(IServiceProvider serviceProvider)
     : base(serviceProvider, typeof(RibbonOrbOptionButtonCollection))
        {
        }

        protected override Type CreateCollectionItemType()
        {
            return typeof(RibbonOrbOptionButton);
        }

        protected override object CreateInstance(Type itemType)
        {
            // The editor can ask for the collection's element type, and RibbonItem is
            // abstract. Fall back to the concrete item type instead of throwing.
            if (itemType == null || itemType.IsAbstract)
            {
                itemType = typeof(RibbonOrbOptionButton);
            }

            return base.CreateInstance(itemType);
        }

        protected override Type[] CreateNewItemTypes()
        {
            return new[]
            {
                typeof(RibbonOrbOptionButton),
                typeof(RibbonSeparator)
            };
        }
    }
}
