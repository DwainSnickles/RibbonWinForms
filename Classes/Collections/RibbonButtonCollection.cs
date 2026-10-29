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


using System.Collections.Generic;
using System.ComponentModel;

namespace RibbonLib.Collections
{
    public class RibbonButtonCollection : RibbonItemCollection
    {
        internal RibbonButtonCollection(RibbonButtonList list)
        {
            OwnerList = list;
        }

        /// <summary>
        /// Gets the list that owns the collection (If any)
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RibbonButtonList OwnerList { get; }

        /// <summary>
        /// Checks for the restrictions that buttons should have on the RibbonButton List
        /// </summary>
        /// <param name="button"></param>
        private void CheckRestrictions(RibbonButton button)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button), "The RibbonButtonList only accepts button in the Buttons collection");

            //if (!string.IsNullOrEmpty(button.Text))
            //    throw new ArgumentException("The buttons on the RibbonButtonList should have no text");

            if (button.Style != RibbonButtonStyle.Normal)
                throw new ArgumentException("The only style supported by the RibbonButtonList is Normal");
        }

        /// <summary>
        /// Gets the item that owns the buttons of this collection: the button list itself.
        /// </summary>
        protected override RibbonItem OwnerItemForChildren => OwnerList;

        /// <summary>
        /// Adds the specified item to the collection
        /// </summary>
        public override void Add(RibbonItem item)
        {
            CheckRestrictions(item as RibbonButton);

            item.Click += OwnerList.item_Click;

            // base.Add wires owner, panel, tab and OwnerItem (OwnerItemForChildren == OwnerList)
            base.Add(item);
        }

        /// <inheritdoc />
        /// <summary>
        /// Adds the specified range of items
        /// </summary>
        /// <param name="items">Items to add</param>
        public override void AddRange(IEnumerable<RibbonItem> items)
        {
            // validate everything before anything is added
            List<RibbonItem> toAdd = new List<RibbonItem>(items);

            foreach (RibbonItem item in toAdd)
                CheckRestrictions(item as RibbonButton);

            base.AddRange(toAdd);
        }

        /// <inheritdoc />
        /// <summary>
        /// Inserts the specified item at the desired index
        /// </summary>
        /// <param name="index">Desired index of the item</param>
        /// <param name="item">Item to insert</param>
        public override void Insert(int index, RibbonItem item)
        {
            CheckRestrictions(item as RibbonButton);

            // base.Insert wires owner, panel, tab and OwnerItem (OwnerItemForChildren == OwnerList)
            base.Insert(index, item);
        }
    }
}