using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;
using RibbonLib.RibbonDesign;

namespace RibbonLib
{
    public class RibbonOrbAdornerGlyph : Glyph
    { 
        #region Fields

    private readonly BehaviorService _behaviorService;
        private readonly Ribbon _ribbon;
        private readonly RibbonDesigner _componentDesigner;

#endregion

        #region Constructor

        public RibbonOrbAdornerGlyph(
            BehaviorService behaviorService,
            RibbonDesigner designer,
            Ribbon ribbon)
            : base(new RibbonOrbAdornerGlyphBehavior(ribbon, designer))
        {
            _behaviorService = behaviorService;
            _componentDesigner = designer;
            _ribbon = ribbon;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the orb menu is visible in the designer.
        /// </summary>
        public bool MenuVisible { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the bounds of the orb glyph in adorner coordinates.
        /// </summary>
        public override Rectangle Bounds
        {
            get
            {
                try
                {
                    if (_behaviorService == null ||
                        _ribbon == null ||
                        _ribbon.IsDisposed ||
                        !_ribbon.IsHandleCreated)
                    {
                        return Rectangle.Empty;
                    }

                    Rectangle orbBounds = _ribbon.OrbBounds;

                    if (orbBounds.Width <= 0 || orbBounds.Height <= 0)
                    {
                        return Rectangle.Empty;
                    }

                    Point edge =
                        _behaviorService.ControlToAdornerWindow(_ribbon);

                    return new Rectangle(
                        edge.X + orbBounds.Left,
                        edge.Y + orbBounds.Top,
                        orbBounds.Height,
                        orbBounds.Height);
                }
                catch (ObjectDisposedException)
                {
                    return Rectangle.Empty;
                }
                catch (InvalidOperationException)
                {
                    return Rectangle.Empty;
                }
            }
        }

        /// <summary>
        /// Determines whether the mouse is over the orb glyph.
        /// </summary>
        public override Cursor GetHitTest(Point p)
        {
            Rectangle bounds = Bounds;

            if (!bounds.IsEmpty && bounds.Contains(p))
            {
                return Cursors.Hand;
            }

            return null;
        }

        /// <summary>
        /// Paints the orb glyph.
        /// </summary>
        public override void Paint(PaintEventArgs pe)
        {
            if (pe == null || pe.Graphics == null)
            {
                return;
            }

            // The orb glyph is intentionally invisible.
            // Bounds and hit testing are still available to the designer.
        }

        #endregion
    }

    public class RibbonOrbAdornerGlyphBehavior : Behavior
    {
        private readonly Ribbon _ribbon;
        private readonly RibbonDesigner _designer;

        public RibbonOrbAdornerGlyphBehavior(Ribbon ribbon, RibbonDesigner designer)
        {
            _ribbon = ribbon;
            _designer = designer;
        }

        public override bool OnMouseUp(
            Glyph? g,
            MouseButtons button,
            Point adornerCoordinates)
        {
            if (_ribbon != null && !_ribbon.IsDisposed)
            {
                _ribbon.OrbMouseDown();
            }
            return base.OnMouseUp(g, button, adornerCoordinates);
        }
    }


}