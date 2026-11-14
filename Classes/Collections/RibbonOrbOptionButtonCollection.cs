using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;

namespace RibbonLib.Collections
{
    [Editor("RibbonLib.RibbonOrbOptionButtonCollectionEditor, RibbonLib.Design", typeof(UITypeEditor))]
    public class RibbonOrbOptionButtonCollection : RibbonItemCollection
    {
        /// <summary>
        /// Gets or sets the orb option button at the specified index.
        /// </summary>
        /// <remarks>
        /// The out-of-process designer's collection editor takes the type it creates for
        /// Add from the collection's Item property. The generic base exposes
        /// <see cref="RibbonItem"/>, which is abstract, so Add failed with "cannot create an
        /// instance ... abstract class". Hiding the indexer with the concrete item type is
        /// what makes the plain (server-created) collection editor add option buttons.
        /// </remarks>
        public new RibbonOrbOptionButton this[int index]
        {
            get => (RibbonOrbOptionButton)base[index];
            set => base[index] = value;
        }
    }
}
