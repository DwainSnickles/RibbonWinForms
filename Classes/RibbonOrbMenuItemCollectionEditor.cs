using Microsoft.DotNet.DesignTools.Editors;
using RibbonLib.Collections;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace RibbonLib
{
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
            if (itemType == typeof(RibbonOrbMenuItem))
            {
                return new RibbonOrbMenuItem();
            }

            if (itemType == typeof(RibbonSeparator))
            {
                return new RibbonSeparator();
            }

            if (itemType.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"Cannot create an instance of abstract type '{itemType.FullName}'.");
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