using System.ComponentModel;
using System.Drawing.Design;

namespace RibbonLib
{
    [Editor("RibbonLib.RibbonOrbRecentItemCollectionEditor, RibbonLib.Design", typeof(UITypeEditor))]
    public class RibbonOrbRecentItemCollection : RibbonItemCollection
    {
        /// <summary>
        /// Gets or sets the recent item at the specified index.
        /// </summary>
        /// <remarks>
        /// The out-of-process designer's collection editor takes the type it creates for
        /// Add from the collection's Item property. The generic base exposes
        /// <see cref="RibbonItem"/>, which is abstract, so Add failed with "cannot create an
        /// instance ... abstract class". Hiding the indexer with the concrete item type is
        /// what makes the plain (server-created) collection editor add recent items.
        /// </remarks>
        public new RibbonOrbRecentItem this[int index]
        {
            get => (RibbonOrbRecentItem)base[index];
            set => base[index] = value;
        }
    }
}
