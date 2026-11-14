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
    /// <remarks>
    /// Design-time only, and not used by the out-of-process designer: its client always opens its
    /// own default collection editor, so this class never edits a collection. What Add creates is
    /// decided by the collection's Item property - see README.md in this folder.
    /// </remarks>
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
