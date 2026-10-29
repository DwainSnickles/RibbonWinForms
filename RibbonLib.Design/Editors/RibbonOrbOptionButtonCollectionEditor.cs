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
