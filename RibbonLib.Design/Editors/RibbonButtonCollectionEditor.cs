using Microsoft.DotNet.DesignTools.Editors;
using RibbonLib.Collections;
using System;
using System.ComponentModel.Design;

namespace RibbonLib
{
    /// <summary>
    /// Design-time editor for <see cref="RibbonButtonList.Buttons"/>. A button list only
    /// accepts <see cref="RibbonButton"/> items with the Normal style, so this editor
    /// offers that single type instead of the full ribbon item list.
    /// </summary>
    public class RibbonButtonCollectionEditor : CollectionEditor
    {
        public RibbonButtonCollectionEditor(IServiceProvider serviceProvider)
            : base(serviceProvider, typeof(RibbonButtonCollection))
        {
        }

        protected override Type CreateCollectionItemType()
        {
            return typeof(RibbonButton);
        }

        protected override Type[] CreateNewItemTypes()
        {
            return new[]
            {
                typeof(RibbonButton)
            };
        }
    }
}
