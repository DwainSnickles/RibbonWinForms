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
