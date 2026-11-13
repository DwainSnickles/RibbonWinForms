using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;
using RibbonLib.RibbonDesign;

namespace RibbonLib
{
    /// <summary>
    /// Invisible hit area over the "Add Tab", "Add Panel" and "Add Button" pills.
    /// </summary>
    /// <remarks>
    /// The pills are painted by <see cref="RibbonDesigner"/>, but in the out-of-process WinForms
    /// designer the ribbon control never receives the mouse messages that would let it act on a
    /// click on them. Only adorner glyphs get clicks there - that is how the orb has always
    /// worked (see <see cref="RibbonOrbAdornerGlyph"/>) - so the pills get their clicks from a
    /// glyph too, and the hit test is the same designer code that paints and hovers them.
    /// </remarks>
    public class RibbonAddPillGlyph : Glyph
    {
        private readonly BehaviorService _behaviorService;
        private readonly RibbonDesigner _designer;
        private readonly Ribbon _ribbon;

        public RibbonAddPillGlyph(
            BehaviorService behaviorService,
            RibbonDesigner designer,
            Ribbon ribbon)
            : base(new RibbonAddPillGlyphBehavior(designer, ribbon))
        {
            _behaviorService = behaviorService;
            _designer = designer;
            _ribbon = ribbon;
        }

        /// <summary>
        /// Gets the ribbon's area in adorner coordinates. The glyph draws nothing, but it has to
        /// span the whole ribbon so its hit test is consulted over every tab, panel and item.
        /// </summary>
        public override Rectangle Bounds
        {
            get
            {
                try
                {
                    if (_designer == null ||
                        _ribbon == null ||
                        _ribbon.IsDisposed ||
                        !_ribbon.IsHandleCreated)
                    {
                        return Rectangle.Empty;
                    }

                    Rectangle area = _ribbon.ClientRectangle;

                    if (area.IsEmpty)
                    {
                        return Rectangle.Empty;
                    }

                    Point edge = _behaviorService == null
                        ? Point.Empty
                        : _behaviorService.ControlToAdornerWindow(_ribbon);

                    area.Offset(edge);

                    // The design-time orb menu hangs below the orb and can run past the
                    // ribbon's bottom edge, so the glyph has to cover it too or clicks on
                    // the menu would fall through to the design surface.
                    Rectangle menu = _designer == null
                        ? Rectangle.Empty
                        : _designer.GetOrbMenuBounds();

                    if (!menu.IsEmpty)
                    {
                        menu.Offset(edge);
                        area = area.IsEmpty ? menu : Rectangle.Union(area, menu);
                    }

                    return area;
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
        /// Decides whether the design surface routes the mouse to this glyph: over a pill (which
        /// gets the hand cursor and highlights), over a tab (which switches the page), or over a
        /// panel, item or context, which the click selects. Anywhere else the click falls through
        /// and selects the ribbon itself.
        /// </summary>
        public override Cursor GetHitTest(Point p)
        {
            if (!TryToRibbonPoint(p, out int x, out int y))
            {
                return null;
            }

            // The open orb menu is live: hand cursor and row highlight while over it.
            if (_designer.IsPointOnOrbMenu(x, y))
            {
                _designer.UpdateOrbMenuHover(x, y);
                return Cursors.Hand;
            }

            // A tab always answers with a cursor: the surface only delivers a click to a glyph
            // whose hit test answers with one, and a tab has to receive that click for the page
            // shown in the designer to change.
            if (_designer.GetTabAt(x, y) != null)
            {
                return Cursors.Hand;
            }

            if (_designer.UpdatePillHover(x, y))
            {
                return Cursors.Hand;
            }

            return _designer.CanSelectAt(x, y) ? Cursors.Default : null;
        }

        /// <summary>
        /// The pills are painted by the designer, so the glyph itself draws nothing.
        /// </summary>
        public override void Paint(PaintEventArgs pe)
        {
        }

        /// <summary>
        /// Converts a point in adorner coordinates to a point on the ribbon.
        /// </summary>
        public bool TryToRibbonPoint(Point adornerPoint, out int x, out int y)
        {
            x = 0;
            y = 0;

            try
            {
                if (_ribbon == null || _ribbon.IsDisposed || !_ribbon.IsHandleCreated)
                {
                    return false;
                }

                Point edge = _behaviorService == null
                    ? Point.Empty
                    : _behaviorService.ControlToAdornerWindow(_ribbon);

                x = adornerPoint.X - edge.X;
                y = adornerPoint.Y - edge.Y;

                // The orb menu may hang below the ribbon, so points inside it count even
                // when they are past the control's bottom edge.
                return x >= 0 && y >= 0 && x < _ribbon.Width &&
                    (y < _ribbon.Height ||
                     (_designer != null && _designer.IsPointOnOrbMenu(x, y)));
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Runs the pill under the mouse when the adorner gets the click.
    /// </summary>
    public class RibbonAddPillGlyphBehavior : Behavior
    {
        private readonly RibbonDesigner _designer;
        private readonly Ribbon _ribbon;

        public RibbonAddPillGlyphBehavior(RibbonDesigner designer, Ribbon ribbon)
        {
            _designer = designer;
            _ribbon = ribbon;
        }

        public override bool OnMouseUp(
            Glyph? g,
            MouseButtons button,
            Point adornerCoordinates)
        {
            if (_ribbon != null &&
                !_ribbon.IsDisposed &&
                g is RibbonAddPillGlyph pill &&
                pill.TryToRibbonPoint(adornerCoordinates, out int x, out int y))
            {
                // An open orb menu takes the click first: it either picks an entry or adds
                // one, so a stray click can never select whatever is painted underneath.
                if (_designer.HandleOrbMenuClick(x, y))
                {
                    return base.OnMouseUp(g, button, adornerCoordinates);
                }

                // A pill adds; anything else the glyph covered is there to be selected, so the
                // tab, panel or item under the click can be edited in the Properties window.
                if (!_designer.HandlePillAt(x, y, button == MouseButtons.Right))
                {
                    _designer.SelectAt(x, y);
                }
            }

            return base.OnMouseUp(g, button, adornerCoordinates);
        }
    }
}
