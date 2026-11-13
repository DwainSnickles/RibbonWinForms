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


using RibbonLib.RibbonDesign;
using System.ComponentModel.Design;

namespace RibbonLib
{
    internal class RibbonButtonDesigner
        : RibbonElementWithItemCollectionDesigner
    {
        /// <summary>
        /// Adds the orb item image commands to the verbs VS shows in its own right-click
        /// menu. They only appear for items that live in the ribbon's orb drop-down, where
        /// the design-time menu cannot reliably take the right click itself.
        /// </summary>
        protected override DesignerVerbCollection OnGetVerbs()
        {
            DesignerVerbCollection verbs = base.OnGetVerbs();

            if (Component is RibbonItem item &&
                Ribbon?.OrbDropDown?.AllItems.Contains(item) == true)
            {
                verbs.Add(new DesignerVerb("Add Image...", (s, e) => PickOrbItemImage(item)));
                verbs.Add(new DesignerVerb("Add Image from Resources...", (s, e) => PickOrbItemImageFromResources(item)));
                verbs.Add(new DesignerVerb("Clear Image", (s, e) => ClearOrbItemImage(item)));
            }

            return verbs;
        }

        private void PickOrbItemImage(RibbonItem item)
        {
            RibbonDesigner.For(Ribbon)?.PickOrbItemImage(item);
        }

        private void PickOrbItemImageFromResources(RibbonItem item)
        {
            RibbonDesigner.For(Ribbon)?.PickOrbItemImageFromResources(item);
        }

        private void ClearOrbItemImage(RibbonItem item)
        {
            RibbonDesigner.For(Ribbon)?.ClearOrbItemImage(item);
        }

        public override Ribbon Ribbon
        {
            get
            {
                if (Component is RibbonButton button)
                {
                    return button.Owner;
                }
                return null;
            }
        }

        public override RibbonItemCollection Collection
        {
            get
            {
                if (Component is RibbonButton button)
                {
                    return button.DropDownItems;
                }
                return null;
            }
        }

        protected override void AddButton(object sender, EventArgs e)
        {
            base.AddButton(sender, e);

        }
    }
}