using Microsoft.DotNet.DesignTools.Editors;
using System;
using System.ComponentModel.Design;

namespace RibbonLib
{
    /// <summary>
    /// Design-time editor for <see cref="RibbonTab.Panels"/>. Lets panels be added,
    /// removed and reordered from the Properties grid.
    /// </summary>
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
