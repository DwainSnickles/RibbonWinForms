using System;
using System.Reflection;
using System.Windows.Forms;

namespace RibbonLib
{
    /// <summary>
    /// Runtime-safe access to the design-time RibbonDesigner.
    /// </summary>
    /// <remarks>
    /// RibbonDesigner derives from design-tool types that only exist inside the Visual Studio
    /// design process. A deployed app has no Microsoft.DotNet.DesignTools.Server, so touching a
    /// member whose signature or body names that type - or looking the designer up by name -
    /// makes the runtime fail the moment that method is compiled or the type is resolved. That
    /// is what used to abort Ribbon.PaintOn after the orb was drawn (nothing but the orb
    /// appeared at runtime) and what threw FileNotFoundException inside the design server when
    /// the design-tools assembly version it was compiled against (1.6.0.0) differed from the one
    /// Visual Studio hosts (18.12.0.0).
    ///
    /// The designer therefore registers itself here when it is created, and everything at
    /// runtime reads that registration as a plain object reference: no reflection, no name
    /// lookup, no design-tools types anywhere in this file.
    /// </remarks>
    internal static class RibbonDesignTime
    {
        /// <summary>
        /// Gets or sets the active designer, or <c>null</c> when running outside the designer.
        /// </summary>
        internal static object? Current { get; set; }

        /// <summary>
        /// Gets a value indicating whether a RibbonDesigner is currently active.
        /// </summary>
        internal static bool IsActive => Current != null;

        /// <summary>
        /// Gets a value indicating whether a designer owns the given ribbon.
        /// </summary>
        internal static bool IsDesigning(Ribbon ribbon)
        {
            return FindDesigner(ribbon) != null;
        }

        /// <summary>
        /// Lets the designer place the cursor over a design-time pill.
        /// </summary>
        /// <returns>
        /// <c>false</c> when no designer owns this ribbon, in which case the caller keeps its own
        /// cursor handling.
        /// </returns>
        internal static bool TryUpdatePillHover(Ribbon ribbon, int x, int y, out bool overPill)
        {
            overPill = false;

            object? designer = FindDesigner(ribbon);

            if (designer == null)
                return false;

            try
            {
                Cache(designer.GetType());

                object? result = _updatePillHover?.Invoke(designer, new object[] { x, y });

                overPill = result is bool hit && hit;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gives the designer first refusal on a mouse-down over a design-time pill.
        /// </summary>
        /// <returns><c>true</c> when the designer handled the click.</returns>
        internal static bool TryHandlePillMouseDown(Ribbon ribbon, int x, int y, bool rightButton)
        {
            object? designer = FindDesigner(ribbon);

            if (designer == null)
                return false;

            try
            {
                Cache(designer.GetType());

                return _pillMouseDown?.Invoke(designer, new object[] { x, y, rightButton }) is bool handled && handled;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Routes a click on the ribbon to the designer that owns it.
        /// </summary>
        internal static void HandleRibbonMouseUp(Ribbon ribbon, int x, int y, bool rightButton)
        {
            object? designer = FindDesigner(ribbon);

            if (designer == null)
                return;

            try
            {
                Cache(designer.GetType());

                _handleRibbonMouseUp?.Invoke(null, new object[] { ribbon, x, y, rightButton });
            }
            catch (Exception)
            {
                // ignored: design-time only
            }
        }

        /// <summary>
        /// Tells the active designer which element is selected.
        /// </summary>
        internal static void SelectElement(RibbonItem item)
        {
            SetSelectedElement(Current, item);
        }

        /// <summary>
        /// Gets the element currently selected in the designer.
        /// </summary>
        internal static RibbonItem? GetSelectedElement()
        {
            return GetSelectedElement(Current);
        }

        /// <summary>
        /// Creates an orb item in the designer through one of its CreateOrb* methods.
        /// </summary>
        internal static void CreateOrbItem(string methodName, Type itemType)
        {
            object? designer = Current;

            if (designer == null)
                return;

            try
            {
                designer.GetType()
                    .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.Invoke(designer, new object[] { itemType });
            }
            catch (Exception)
            {
                // ignored: design-time only
            }
        }

        private static Type? _cachedOwner;
        private static MethodInfo? _forRibbon;
        private static MethodInfo? _updatePillHover;
        private static MethodInfo? _handleRibbonMouseUp;
        private static MethodInfo? _pillMouseDown;

        /// <summary>
        /// Gets the designer that owns the ribbon, or <c>null</c> outside the designer.
        /// </summary>
        private static object? FindDesigner(Ribbon? ribbon)
        {
            object? designer = Current;

            if (designer == null || ribbon == null || ribbon.IsDisposed)
                return null;

            try
            {
                Cache(designer.GetType());

                return _forRibbon?.Invoke(null, new object[] { ribbon });
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Looks the designer's members up once per designer type. They are only touched in the
        /// designer process, where the design-time types exist.
        /// </summary>
        private static void Cache(Type owner)
        {
            if (_cachedOwner == owner)
                return;

            const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            _forRibbon = owner.GetMethod("For", Static, null, new[] { typeof(Ribbon) }, null);
            _updatePillHover = owner.GetMethod("UpdatePillHover", Instance, null, new[] { typeof(int), typeof(int) }, null);
            _pillMouseDown = owner.GetMethod(
                "OnRibbonPillMouseDown", Instance, null, new[] { typeof(int), typeof(int), typeof(bool) }, null);
            _handleRibbonMouseUp = owner.GetMethod(
                "HandleRibbonMouseUp", Static, null, new[] { typeof(Ribbon), typeof(int), typeof(int), typeof(bool) }, null);

            _cachedOwner = owner;
        }

        private static void SetSelectedElement(object? designer, RibbonItem? item)
        {
            if (designer == null)
                return;

            try
            {
                designer.GetType()
                    .GetProperty("SelectedElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(designer, item);
            }
            catch (Exception)
            {
                // ignored: design-time only
            }
        }

        private static RibbonItem? GetSelectedElement(object? designer)
        {
            if (designer == null)
                return null;

            try
            {
                return designer.GetType()
                    .GetProperty("SelectedElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(designer) as RibbonItem;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
