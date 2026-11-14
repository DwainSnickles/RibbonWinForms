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

using System;
using System.Linq;
using Microsoft.DotNet.DesignTools.Editors;
using System.ComponentModel.Design;

namespace RibbonLib
{
    /// <remarks>
    /// Design-time only, and not used by the out-of-process designer: its client always opens its
    /// own default collection editor, so this class never edits a collection and the
    /// <see cref="CreateInstance(Type)"/> override below never runs. What Add creates is decided by
    /// the collection's Item property - see README.md in this folder.
    /// </remarks>
    public class RibbonItemCollectionEditor
        : CollectionEditor
    {
        public RibbonItemCollectionEditor(IServiceProvider serviceProvider)
    : base(serviceProvider, typeof(RibbonItemCollection))
        {

        }

        protected override Type CreateCollectionItemType()
        {
            return typeof(RibbonButton);
        }

        protected override object CreateInstance(Type itemType)
        {
            // The editor can ask for the collection's element type, and RibbonItem is
            // abstract, which made Add fail with "cannot create an instance ... abstract
            // class". Fall back to a button, the type CreateCollectionItemType advertises.
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
            typeof(RibbonButtonList),
            typeof(RibbonItemGroup),
            typeof(RibbonComboBox),
            typeof(RibbonSeparator),
            typeof(RibbonTextBox),
            typeof(RibbonColorChooser),
            typeof(RibbonDescriptionMenuItem),
            typeof(RibbonCheckBox),
            typeof(RibbonUpDown),
            typeof(RibbonLabel),
            typeof(RibbonHost)
         };
        }
    }

    /// <summary>
    /// Design-time editor for <see cref="RibbonPanel.Items"/>.
    /// </summary>
    /// <remarks>
    /// Design-time only, and not used by the out-of-process designer: its client always opens its
    /// own default collection editor, so this class never edits a collection and the
    /// <see cref="CreateInstance(Type)"/> override below never runs. What Add creates is decided by
    /// the collection's Item property - see README.md in this folder.
    /// </remarks>
    public class RibbonPanelItemCollectionEditor : CollectionEditor
    {
        public RibbonPanelItemCollectionEditor(IServiceProvider serviceProvider)
            : base(serviceProvider, typeof(RibbonItemCollection))
        {
        }

        protected override Type CreateCollectionItemType()
        {
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
            return new[]
            {
                typeof(RibbonButton),
                typeof(RibbonButtonList),
                typeof(RibbonItemGroup),
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
