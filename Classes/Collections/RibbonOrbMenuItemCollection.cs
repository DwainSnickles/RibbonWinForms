using System.ComponentModel;
using System.Drawing.Design;

namespace RibbonLib.Collections
{
    [Editor("RibbonLib.RibbonOrbMenuItemCollectionEditor, RibbonLib", typeof(UITypeEditor))]
    public class RibbonOrbMenuItemCollection : RibbonItemCollection
    {
        public RibbonOrbMenuItemCollection() : base()
        {
        }
    }
}

