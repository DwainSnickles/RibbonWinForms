// *********************************
// Message from Original Author:
//
// 2008 Jose Menendez Poo
// Please give me credit if you use this code. It's all I ask.
// Contact me for more info: menendezpoo@gmail.com
// *********************************
//
// Original project from http://ribbon.codeplex.com/
// Continue to support and maintain by http://officeribbon.codeplex.com/


using Microsoft.DotNet.DesignTools.Editors;
using System.ComponentModel.Design;

namespace RibbonLib
{
    public class RibbonQuickAccessToolbarItemCollectionEditor
        : CollectionEditor
    {
        public RibbonQuickAccessToolbarItemCollectionEditor(IServiceProvider serviceProvider)
    : base(serviceProvider, typeof(RibbonQuickAccessToolbarItemCollection))
        {
        }

        protected override Type CreateCollectionItemType()
        {
            // RibbonItem is abstract, so the editor cannot instantiate it when Add is
            // clicked and throws "cannot create an instance ... abstract class". Offer the
            // concrete type every quick access toolbar item starts as instead.
            return typeof(RibbonButton);
        }

        protected override object CreateInstance(Type itemType)
        {
            if (itemType == null || itemType.IsAbstract)
            {
                itemType = typeof(RibbonButton);
            }

            return base.CreateInstance(itemType);
        }

        protected override Type[] CreateNewItemTypes()
        {
            return new[] {
            typeof(RibbonButton),
            typeof(RibbonComboBox),
            typeof(RibbonSeparator),
            typeof(RibbonTextBox),
            typeof(RibbonColorChooser),
            typeof(RibbonCheckBox),
            typeof(RibbonUpDown),
            typeof(RibbonLabel),
            typeof(RibbonHost)
         };
        }
    }
}
