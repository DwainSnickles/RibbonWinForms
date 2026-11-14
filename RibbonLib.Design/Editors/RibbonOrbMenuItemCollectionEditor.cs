using Microsoft.DotNet.DesignTools.Editors;
using RibbonLib.Collections;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace RibbonLib
{
    /// <remarks>
    /// Design-time only, and not used by the out-of-process designer: its client always opens its
    /// own default collection editor, so this class never edits a collection and the
    /// <see cref="CreateInstance(Type)"/> override below never runs. What Add creates is decided by
    /// the collection's Item property - see README.md in this folder.
    /// </remarks>
    public class RibbonOrbMenuItemCollectionEditor : CollectionEditor
    {
        public RibbonOrbMenuItemCollectionEditor(IServiceProvider provider) : base(provider, typeof(RibbonOrbMenuItemCollection)) { }

        protected override Type CreateCollectionItemType()
        {
            return typeof(RibbonOrbMenuItem);
        }

        protected override Type[] CreateNewItemTypes()
        {
            return new[]
            {
            typeof(RibbonOrbMenuItem),
            typeof(RibbonSeparator)
        };
        }

        protected override object CreateInstance(Type itemType)
        {
            try
            {
                return CreateInstanceCore(itemType);
            }
            catch (Exception ex)
            {
                RibbonDesignLog.Exception("RibbonOrbMenuItemCollectionEditor.CreateInstance", ex);
                throw;
            }
        }

        private object CreateInstanceCore(Type itemType)
        {
            RibbonDesignLog.Write($"CreateInstance asked for {itemType?.FullName ?? "null"}");

            if (itemType == typeof(RibbonOrbMenuItem))
            {
                return new RibbonOrbMenuItem();
            }

            if (itemType == typeof(RibbonSeparator))
            {
                return new RibbonSeparator();
            }

            // The editor can ask for the collection's element type, and RibbonItem is
            // abstract. Fall back to the concrete item type instead of throwing.
            if (itemType == null || itemType.IsAbstract)
            {
                itemType = typeof(RibbonOrbMenuItem);
            }

            return base.CreateInstance(itemType);
        }

        protected override string GetDisplayText(object value)
        {
            if (value is RibbonOrbMenuItem item)
                return item.Text ?? "(Orb Menu Item)";

            if (value is RibbonSeparator)
                return "(Separator)";

            return base.GetDisplayText(value);
        }
    }


}