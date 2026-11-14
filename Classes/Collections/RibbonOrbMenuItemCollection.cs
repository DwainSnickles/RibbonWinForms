using System.ComponentModel;
using System.Drawing.Design;

namespace RibbonLib.Collections
{
    [Editor("RibbonLib.RibbonOrbMenuItemCollectionEditor, RibbonLib.Design", typeof(UITypeEditor))]
    public class RibbonOrbMenuItemCollection : RibbonItemCollection
    {
        public RibbonOrbMenuItemCollection() : base()
        {
        }

        /// <summary>
        /// Gets or sets the orb menu item at the specified index.
        /// </summary>
        /// <remarks>
        /// The out-of-process designer's collection editor takes the type it creates for
        /// Add from the collection's Item property. The generic base exposes
        /// <see cref="RibbonItem"/>, which is abstract, so Add failed with "cannot create an
        /// instance ... abstract class". Hiding the indexer with the concrete item type is
        /// what makes the plain (server-created) collection editor add orb menu items.
        /// </remarks>
        public new RibbonOrbMenuItem this[int index]
        {
            get => (RibbonOrbMenuItem)base[index];
            set => base[index] = value;
        }
    }
}

