using Microsoft.DotNet.DesignTools.Editors;
using System;
using System.ComponentModel.Design;

namespace RibbonLib
{
    /// <summary>
    /// Design-time editor for <see cref="RibbonTab.Panels"/>. Lets panels be added,
    /// removed and reordered from the Properties grid.
    /// </summary>
    /// <remarks>
    /// Design-time only, and not used by the out-of-process designer: its client always opens its
    /// own default collection editor, so this class never edits a collection. What Add creates is
    /// decided by the collection's Item property - see README.md in this folder.
    /// </remarks>
    public class RibbonPanelCollectionEditor : CollectionEditor
    {
        public RibbonPanelCollectionEditor(IServiceProvider serviceProvider)
            : base(serviceProvider, typeof(RibbonPanelCollection))
        {
        }

        protected override Type CreateCollectionItemType()
        {
            return typeof(RibbonPanel);
        }

        protected override Type[] CreateNewItemTypes()
        {
            return new[]
            {
                typeof(RibbonPanel)
            };
        }
    }
}
