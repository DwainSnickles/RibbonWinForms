using Microsoft.DotNet.DesignTools.Editors;
using System;
using System.ComponentModel.Design;

namespace RibbonLib
{
    /// <remarks>
    /// Design-time only, and not used by the out-of-process designer: its client always opens its
    /// own default collection editor, so this class never edits a collection and the
    /// <see cref="CreateInstance(Type)"/> override below never runs. What Add creates is decided by
    /// the collection's Item property - see README.md in this folder.
    /// </remarks>
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
            try
            {
                return CreateInstanceCore(itemType);
            }
            catch (Exception ex)
            {
                RibbonDesignLog.Exception("RibbonOrbRecentItemCollectionEditor.CreateInstance", ex);
                throw;
            }
        }

        private object CreateInstanceCore(Type itemType)
        {
            RibbonDesignLog.Write($"CreateInstance asked for {itemType?.FullName ?? "null"}");

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
