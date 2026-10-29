using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using RibbonLib.Collections;

namespace RibbonLib
{
    [Editor("RibbonLib.RibbonItemCollectionEditor, RibbonLib.Design", typeof(UITypeEditor))]
    public class RibbonItemCollection : RibbonCollectionBase<RibbonItem>
    {
        internal RibbonItemCollection() : base(null) { }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RibbonPanel OwnerPanel { get; private set; }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RibbonTab OwnerTab { get; private set; }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RibbonItem OwnerItem { get; private set; }

        /// <summary>
        /// Gets the item that owns the items of this collection. Collections that host their
        /// items inside another ribbon item (for example a button list) override this so the
        /// children point at that item instead of at the collection's own owner.
        /// </summary>
        protected virtual RibbonItem OwnerItemForChildren => OwnerItem;

        private bool IsDesignTime =>
            Owner?.Site?.DesignMode == true ||
            OwnerPanel?.Site?.DesignMode == true ||
            OwnerTab?.Site?.DesignMode == true;

        private IEnumerable<RibbonItem> SafeEnum()
        {
            // Prevent "collection modified; enumeration may not execute"
            return new List<RibbonItem>(this);
        }

        internal void SetOwnerTab(RibbonTab tab)
        {
            OwnerTab = tab;
            foreach (RibbonItem item in SafeEnum())
                item.SetOwnerTab(tab);
        }

        internal void SetOwnerPanel(RibbonPanel panel)
        {
            OwnerPanel = panel;
            foreach (RibbonItem item in SafeEnum())
                item.SetOwnerPanel(panel);
        }

        internal void SetOwnerItem(RibbonItem item)
        {
            OwnerItem = item;
            foreach (RibbonItem subitem in SafeEnum())
                subitem.SetOwnerItem(item);
        }

        internal override void SetOwner(Ribbon owner)
        {
            base.SetOwner(owner);
            foreach (RibbonItem item in SafeEnum())
                item.SetOwner(owner);
        }

        internal override void SetOwner(RibbonItem item)
        {
            item.SetOwner(Owner);
            item.SetOwnerPanel(OwnerPanel);
            item.SetOwnerTab(OwnerTab);
            item.SetOwnerItem(OwnerItemForChildren);
        }

        internal override void ClearOwner(RibbonItem item)
        {
            item.ClearOwner();
        }

        internal override void UpdateRegions()
        {
            if (IsDesignTime)
                return; // avoid designer crashes

            try
            {
                OwnerTab?.UpdatePanelsRegions();
                if (Owner != null && Owner.IsDisposed == false)
                {
                    Owner.UpdateRegions();
                    Owner.Invalidate();
                }
            }
            catch
            {
                // ignored
            }
        }

        private void CheckRestrictions(RibbonItem item)
        {
            if (OwnerItem != null)
            {
                // dropdown menu restrictions (kept empty for now)
            }
            else
            {
                if (item is RibbonDescriptionMenuItem)
                    throw new ArgumentException("The RibbonDescriptionMenuItem item is not supported on a panel", nameof(item));
            }
        }

        public override void Add(RibbonItem item)
        {
            CheckRestrictions(item);

            item.SetOwner(Owner);
            item.SetOwnerPanel(OwnerPanel);
            item.SetOwnerTab(OwnerTab);
            item.SetOwnerItem(OwnerItemForChildren);

            base.Add(item);

            if (!IsDesignTime)
                UpdateRegions();
        }

        public override void AddRange(IEnumerable<RibbonItem> items)
        {
            foreach (RibbonItem item in items)
            {
                CheckRestrictions(item);

                item.SetOwner(Owner);
                item.SetOwnerPanel(OwnerPanel);
                item.SetOwnerTab(OwnerTab);
                item.SetOwnerItem(OwnerItemForChildren);
            }

            base.AddRange(items);

            if (!IsDesignTime)
                UpdateRegions();
        }

        public override void Insert(int index, RibbonItem item)
        {
            CheckRestrictions(item);

            item.SetOwner(Owner);
            item.SetOwnerPanel(OwnerPanel);
            item.SetOwnerTab(OwnerTab);
            item.SetOwnerItem(OwnerItemForChildren);

            base.Insert(index, item);

            if (!IsDesignTime)
                UpdateRegions();
        }

        // ------------------------------
        // Layout helper methods
        // ------------------------------

        internal int GetItemsLeft(IEnumerable<RibbonItem> items)
        {
            if (Count == 0) return 0;

            int min = int.MaxValue;

            foreach (RibbonItem item in items)
                if (item.Bounds.X < min)
                    min = item.Bounds.X;

            return min;
        }

        internal int GetItemsRight(IEnumerable<RibbonItem> items)
        {
            if (Count == 0) return 0;

            int max = int.MinValue;

            foreach (RibbonItem item in items)
                if (item.Bounds.Right > max)
                    max = item.Bounds.Right;

            return max;
        }

        internal int GetItemsTop(IEnumerable<RibbonItem> items)
        {
            if (Count == 0) return 0;

            int min = int.MaxValue;

            foreach (RibbonItem item in items)
                if (item.Bounds.Y < min)
                    min = item.Bounds.Y;

            return min;
        }

        internal int GetItemsBottom(IEnumerable<RibbonItem> items)
        {
            if (Count == 0) return 0;

            int max = int.MinValue;

            foreach (RibbonItem item in items)
                if (item.Bounds.Bottom > max)
                    max = item.Bounds.Bottom;

            return max;
        }

        internal int GetItemsWidth(IEnumerable<RibbonItem> items)
            => GetItemsRight(items) - GetItemsLeft(items);

        internal int GetItemsHeight(IEnumerable<RibbonItem> items)
            => GetItemsBottom(items) - GetItemsTop(items);

        internal Rectangle GetItemsBounds(IEnumerable<RibbonItem> items)
            => Rectangle.FromLTRB(GetItemsLeft(items), GetItemsTop(items), GetItemsRight(items), GetItemsBottom(items));

        internal int GetItemsLeft()
        {
            if (Count == 0) return 0;

            int min = int.MaxValue;

            foreach (RibbonItem item in this)
                if (item.Bounds.X < min)
                    min = item.Bounds.X;

            return min;
        }

        internal int GetItemsRight()
        {
            if (Count == 0) return 0;

            int max = int.MinValue;

            foreach (RibbonItem item in this)
                if (item.Visible && item.Bounds.Right > max)
                    max = item.Bounds.Right;

            if (max == int.MinValue) max = 0;

            return max;
        }

        internal int GetItemsTop()
        {
            if (Count == 0) return 0;

            int min = int.MaxValue;

            foreach (RibbonItem item in this)
                if (item.Bounds.Y < min)
                    min = item.Bounds.Y;

            return min;
        }

        internal int GetItemsBottom()
        {
            if (Count == 0) return 0;

            int max = int.MinValue;

            foreach (RibbonItem item in this)
                if (item.Visible && item.Bounds.Bottom > max)
                    max = item.Bounds.Bottom;

            if (max == int.MinValue) max = 0;

            return max;
        }

        internal int GetItemsWidth()
            => GetItemsRight() - GetItemsLeft();

        internal int GetItemsHeight()
            => GetItemsBottom() - GetItemsTop();

        internal Rectangle GetItemsBounds()
            => Rectangle.FromLTRB(GetItemsLeft(), GetItemsTop(), GetItemsRight(), GetItemsBottom());

        internal void MoveTo(Point p)
        {
            MoveTo(this, p);
        }

        internal void MoveTo(IEnumerable<RibbonItem> items, Point p)
        {
            Rectangle oldBounds = GetItemsBounds(items);

            foreach (RibbonItem item in items)
            {
                int dx = item.Bounds.X - oldBounds.Left;
                int dy = item.Bounds.Y - oldBounds.Top;

                item.SetBounds(new Rectangle(new Point(p.X + dx, p.Y + dy), item.Bounds.Size));
            }
        }

        internal void CenterItemsInto(Rectangle rectangle)
        {
            CenterItemsInto(this, rectangle);
        }

        internal void CenterItemsVerticallyInto(Rectangle rectangle)
        {
            CenterItemsVerticallyInto(this, rectangle);
        }

        internal void CenterItemsHorizontallyInto(Rectangle rectangle)
        {
            CenterItemsHorizontallyInto(this, rectangle);
        }

        internal void CenterItemsInto(IEnumerable<RibbonItem> items, Rectangle rectangle)
        {
            int x = rectangle.Left + (rectangle.Width - GetItemsWidth(items)) / 2;
            int y = rectangle.Top + (rectangle.Height - GetItemsHeight(items)) / 2;

            MoveTo(items, new Point(x, y));
        }

        internal void CenterItemsVerticallyInto(IEnumerable<RibbonItem> items, Rectangle rectangle)
        {
            int x = GetItemsLeft(items);
            int y = rectangle.Top + (rectangle.Height - GetItemsHeight(items)) / 2;

            MoveTo(items, new Point(x, y));
        }

        internal void CenterItemsHorizontallyInto(IEnumerable<RibbonItem> items, Rectangle rectangle)
        {
            int x = rectangle.Left + (rectangle.Width - GetItemsWidth(items)) / 2;
            int y = GetItemsTop(items);

            MoveTo(items, new Point(x, y));
        }
    }
}
