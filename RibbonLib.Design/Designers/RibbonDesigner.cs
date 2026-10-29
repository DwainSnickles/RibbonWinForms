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


//using System.Windows.Forms.Design.Behavior;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Actions;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;
using RibbonLib.Collections;
using RibbonLib.RibbonHelpers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms.Design;

namespace RibbonLib.RibbonDesign
{
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
    public class RibbonDesigner
       : ControlDesigner
    {
        private DesignerVerbCollection _verbs;
        private IComponentChangeService _changeService;
        private IDesignerHost _designerHost;
        private ISelectionService? _selectionService;


        public override DesignerVerbCollection Verbs
        {
            get
            {
                // If the control is deleted, return an empty collection
                if (Component == null || Component.Site == null)
                {
                    return new DesignerVerbCollection();
                }

                // Normal behavior
                if (_verbs == null)
                {
                    _verbs = new DesignerVerbCollection
            {
                new DesignerVerb("Add Tab", AddTabVerb),
                new DesignerVerb("Add Context", AddContextVerb)
            };
                }

                return _verbs;
            }
        }

        private void ForceDesignerRefresh()
        {
            var changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            if (changeService != null)
            {
                changeService.OnComponentChanged(Component, null, null, null);
            }

            var host = GetService(typeof(IDesignerHost)) as IDesignerHost;
            host?.Activate();

        }


        protected override void PreFilterProperties(IDictionary properties)
        {
            base.PreFilterProperties(properties);

            // -----------------------------------------
            // Your existing Ribbon property injections
            // -----------------------------------------

            properties["OrbMenuItems"] = TypeDescriptor.CreateProperty(
                typeof(Ribbon),
                "OrbMenuItems",
                typeof(RibbonOrbMenuItemCollection),
                new Attribute[]
                {
            new EditorAttribute(typeof(RibbonOrbMenuItemCollectionEditor), typeof(UITypeEditor)),
            new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)
                }
            );

            properties["OrbOptionItems"] = TypeDescriptor.CreateProperty(
                typeof(Ribbon),
                "OrbOptionItems",
                typeof(RibbonOrbOptionButtonCollection),
                new Attribute[]
                {
            new EditorAttribute(typeof(RibbonOrbOptionButtonCollectionEditor), typeof(UITypeEditor)),
            new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)
                }
            );

            properties["OrbRecentItems"] = TypeDescriptor.CreateProperty(
                typeof(Ribbon),
                "OrbRecentItems",
                typeof(RibbonOrbRecentItemCollection),
                new Attribute[]
                {
            new EditorAttribute(typeof(RibbonOrbRecentItemCollectionEditor), typeof(UITypeEditor)),
            new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content)
                }
            );

            // -----------------------------------------
            // NEW: Verb cleanup when Ribbon is removed
            // -----------------------------------------

            if (Component.Site == null) // Ribbon has been deleted
            {
                // Hide and clear verbs
                if (_verbs != null)
                {
                    foreach (DesignerVerb verb in _verbs)
                    {
                        verb.Visible = false;
                        verb.Enabled = false;
                    }

                    _verbs.Clear();
                    _verbs = null;
                }

                // Remove smart‑tag actions
                var actionService = GetService(typeof(DesignerActionService)) as DesignerActionService;
                actionService?.Remove(Component);

                // Clear action lists
                ActionLists?.Clear();
            }
        }


        #region Static

        /// <summary>
        /// The designer currently attached to a ribbon, or <c>null</c>. RibbonLib code at runtime
        /// reads this through <see cref="RibbonDesignTime.Current"/>, which is set alongside it
        /// so runtime never has to resolve the design-tools types this class derives from.
        /// </summary>
        internal static RibbonDesigner Current;

        #endregion

        #region Fields

        private IRibbonElement _selectedElement;
        private bool pillHandledOnDown;

        private Adorner _quickAccessAdorner;
        private Adorner _orbAdorner;

        /// <summary>
        /// The panel whose "Add Control" menu is open, or <c>null</c> when it is closed.
        /// </summary>
        private RibbonPanel _addItemMenuPanel;

        /// <summary>
        /// The menu row under the mouse, or -1.
        /// </summary>
        private int _addItemMenuHover = -1;

        /// <summary>
        /// Watches where the pointer is, so the menu closes when it leaves the pill and the menu.
        /// </summary>
        private System.Windows.Forms.Timer _addItemMenuWatcher;

        /// <summary>
        /// Carries the invisible glyph that turns a click on a painted pill into an add.
        /// </summary>
        private Adorner _pillsAdorner;

        private int _lastPillClickTick;
        private Point _lastPillClickPoint;

        /// <summary>
        /// The design-time pills are drawn by the designer rather than by adorner glyphs, because
        /// the tooling does not reliably hit-test glyphs for the cursor or forward their clicks.
        /// </summary>
        private enum DesignerPill
        {
            None,
            AddTab,
            AddPanel,
            AddItem
        }

        private DesignerPill _hoverPill;
        private RibbonPanel _hoverPillPanel;

        #endregion

        #region Ctor
        public RibbonDesigner()
        {
            Current = this;
            RibbonDesignTime.Current = this;
            System.Diagnostics.Debug.WriteLine(
      "RibbonDesigner constructor called");
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            System.Diagnostics.Debug.WriteLine(
                $"RibbonDesigner.Dispose called. Disposing: {disposing}");

            if (disposing)
            {
                // Unhook change service
                if (_changeService != null)
                {
                    _changeService.ComponentRemoved -= changeService_ComponentRemoved;
                    _changeService.ComponentAdded -= changeService_ComponentAdded;
                    _changeService = null;
                }

                if (_selectionService != null)
                {
                    _selectionService.SelectionChanged -= selectionService_SelectionChanged;
                    _selectionService = null;
                }

                CloseAddItemMenu();

                if (_addItemMenuWatcher != null)
                {
                    _addItemMenuWatcher.Stop();
                    _addItemMenuWatcher.Dispose();
                    _addItemMenuWatcher = null;
                }

                if (_pillsAdorner != null)
                {
                    BehaviorService?.Adorners.Remove(_pillsAdorner);
                    _pillsAdorner.Glyphs.Clear();
                    _pillsAdorner = null;
                }

                if (_orbAdorner != null)
                {
                    BehaviorService?.Adorners.Remove(_orbAdorner);
                    _orbAdorner.Glyphs.Clear();
                    _orbAdorner = null;
                }

                if (_quickAccessAdorner != null)
                {
                    BehaviorService?.Adorners.Remove(_quickAccessAdorner);
                    _quickAccessAdorner.Glyphs.Clear();
                    _quickAccessAdorner = null;
                }

                // Remove smart‑tag actions (THIS is the critical fix)
                var actionService = GetService(typeof(DesignerActionService)) as DesignerActionService;
                if (actionService != null)
                {
                    actionService.Remove(Component);
                }

                // Clear designer verbs
                if (_verbs != null)
                {
                    foreach (DesignerVerb verb in _verbs)
                    {
                        verb.Visible = false;
                        verb.Enabled = false;
                    }

                    _verbs.Clear();
                    _verbs = null;
                }

                // Clear action lists
                if (this.ActionLists != null)
                {
                    this.ActionLists.Clear();
                }

                // Clear static reference
                if (Current == this)
                {
                    Current = null;
                    RibbonDesignTime.Current = null;
                }

                // Force designer refresh
                var host = GetService(typeof(IDesignerHost)) as IDesignerHost;
                host?.Activate();

                //ForceDesignerRefresh();

                _designerHost = null;
                Ribbon?.Invalidate(true);

            }

            base.Dispose(disposing);
        }

        //Finalize is called by base class "System.ComponentModel.Design.ComponentDesigner"

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the currently selected RibbonElement
        /// </summary>
        public IRibbonElement SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (Ribbon == null) return;
                _selectedElement = value;

                if (GetService(typeof(ISelectionService)) is ISelectionService selector && value != null)
                    selector.SetSelectedComponents(new[] { value as Component }, SelectionTypes.Primary);

                if (value is RibbonButton button)
                {
                    button.ShowDropDown();
                }

                Ribbon.Refresh();
            }
        }

        /// <summary>
        /// Gets the Ribbon of the designer
        /// </summary>
        public Ribbon Ribbon => Control as Ribbon;

        #endregion

        #region Methods

        public virtual void CreateItem(Ribbon ribbon, RibbonItemCollection collection, Type t)
        {
            if (GetService(typeof(IDesignerHost)) is IDesignerHost host && collection != null && ribbon != null)
            {
                DesignerTransaction transaction = host.CreateTransaction("AddRibbonItem_" + Component.Site.Name);

                MemberDescriptor member = TypeDescriptor.GetProperties(Component)["Items"];
                RaiseComponentChanging(member);

                RibbonItem item = host.CreateComponent(t) as RibbonItem;

                if (!(item is RibbonSeparator)) item.Text = item.Site.Name;

                collection.Add(item);
                ribbon.OnRegionsChanged();

                RaiseComponentChanged(member, null, null);
                transaction.Commit();
            }
        }

        private void CreateOrbItem(string collectionName, RibbonItemCollection collection, Type t)
        {
            if (Ribbon == null) return;

            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
            DesignerTransaction transaction = host.CreateTransaction("AddRibbonOrbItem_" + Component.Site.Name);
            MemberDescriptor member = TypeDescriptor.GetProperties(Ribbon.OrbDropDown)[collectionName];
            RaiseComponentChanging(member);

            RibbonItem item = host.CreateComponent(t) as RibbonItem;

            if (!(item is RibbonSeparator)) item.Text = item.Site.Name;

            collection.Add(item);
            Ribbon.OrbDropDown.OnRegionsChanged();

            RaiseComponentChanged(member, null, null);
            transaction.Commit();

            Ribbon.OrbDropDown.SelectOnDesigner(item);
            Ribbon.OrbDropDown.WrappedDropDown.Size = Ribbon.OrbDropDown.Size;
        }

        /// <summary>
        /// Creates an Orb's MenuItem
        /// </summary>
        /// <param name="t"></param>
        public void CreateOrbMenuItem(Type t)
        {
            CreateOrbItem("MenuItems", Ribbon.OrbDropDown.MenuItems, t);
        }

        /// <summary>
        /// Creates an Orb's RecentItem
        /// </summary>
        /// <param name="t"></param>
        public void CreateOrbRecentItem(Type t)
        {
            CreateOrbItem("RecentItems", Ribbon.OrbDropDown.RecentItems, t);
        }

        /// <summary>
        /// Creates an Orb's OptionItem
        /// </summary>
        /// <param name="t"></param>
        public void CreateOrbOptionItem(Type t)
        {
            CreateOrbItem("OptionItems", Ribbon.OrbDropDown.OptionItems, t);
        }

        private void AssignEventHandler()
        {
            //TODO: Didn't work
            //if (SelectedElement == null) return;

            //IEventBindingService binder = GetService(typeof(IEventBindingService)) as IEventBindingService;

            //EventDescriptorCollection evts = TypeDescriptor.GetEvents(SelectedElement);

            ////string id = binder.CreateUniqueMethodName(SelectedElement as Component, evts["Click"]);

            //binder.ShowCode(SelectedElement as Component, evts["Click"]);
        }

        private void SelectRibbon()
        {
            if (GetService(typeof(ISelectionService)) is ISelectionService selector)
                selector.SetSelectedComponents(new Component[] { Ribbon }, SelectionTypes.Primary);

        }

        /// <summary>
        /// Adds a tab to the ribbon.
        /// </summary>
        /// <remarks>
        /// The collection change is reported to the change service, as the panel and item adds
        /// do; without it the designer never sees the form as modified and the new tab is not
        /// written into InitializeComponent.
        /// </remarks>
        public void AddTabVerb(object sender, EventArgs e)
        {
            if (!(Control is Ribbon r) || Component?.Site == null)
            {
                return;
            }

            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;

            if (host == null)
            {
                return;
            }

            DesignerTransaction transaction = host.CreateTransaction("AddRibbonTab_" + Component.Site.Name);
            MemberDescriptor member = TypeDescriptor.GetProperties(r)["Tabs"];
            RaiseComponentChanging(member);

            try
            {
                RibbonTab tab = host.CreateComponent(typeof(RibbonTab)) as RibbonTab;

                if (tab == null)
                {
                    transaction.Cancel();
                    return;
                }

                tab.Text = tab.Site?.Name ?? tab.Text;

                r.Tabs.Add(tab);

                RaiseComponentChanged(member, null, null);

                r.Refresh();
                transaction.Commit();
            }
            catch
            {
                transaction.Cancel();
                throw;
            }
        }

        /// <summary>
        /// Adds a context to the ribbon.
        /// </summary>
        /// <remarks>
        /// A context is hidden by default, which hides every tab that belongs to it at runtime,
        /// so one created in the designer is made visible. The collection change is reported like
        /// the tab add, so the context is saved into InitializeComponent.
        /// </remarks>
        public void AddContextVerb(object sender, EventArgs e)
        {
            if (!(Control is Ribbon r) || Component?.Site == null)
            {
                return;
            }

            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;

            if (host == null)
            {
                return;
            }

            DesignerTransaction transaction = host.CreateTransaction("AddRibbonContext_" + Component.Site.Name);
            MemberDescriptor member = TypeDescriptor.GetProperties(r)["Contexts"];
            RaiseComponentChanging(member);

            try
            {
                RibbonContext context = host.CreateComponent(typeof(RibbonContext)) as RibbonContext;

                if (context == null)
                {
                    transaction.Cancel();
                    return;
                }

                context.Text = context.Site?.Name ?? context.Text;
                context.Visible = true;

                Random rnd1 = new Random();

                context.GlowColor = Color.FromArgb(rnd1.Next(155, 255), rnd1.Next(155, 255), rnd1.Next(155, 255));

                r.Contexts.Add(context);

                RaiseComponentChanged(member, null, null);

                r.Refresh();
                transaction.Commit();
            }
            catch
            {
                transaction.Cancel();
                throw;
            }
        }

        public void AddPanelToTab(RibbonTab tab)
        {
            if (tab == null || Ribbon == null) return;

            IDesignerHost host = GetService(typeof(IDesignerHost)) as IDesignerHost;
            if (host == null) return;

            DesignerTransaction transaction = host.CreateTransaction("AddPanel" + tab.Site.Name);
            MemberDescriptor member = TypeDescriptor.GetProperties(tab)["Panels"];
            RaiseComponentChanging(member);

            if (host.CreateComponent(typeof(RibbonPanel)) is RibbonPanel panel)
            {
                panel.Text = panel.Site.Name;
                panel.Index = tab.Panels.Count;

                if (panel.Index == 0)
                {
                    panel.IsFirstPanel = true;
                }
                else
                {
                    foreach (RibbonPanel pnl in tab.Panels)
                    {
                        pnl.IsLastPanel = false;
                    }
                    panel.IsLastPanel = true;
                }

                tab.Panels.Add(panel);
                Ribbon.OnRegionsChanged();
            }

            RaiseComponentChanged(member, null, null);
            transaction.Commit();
        }

        /// <summary>
        /// Runs the design-time click logic from the ribbon's own mouse handling.
        /// </summary>
        /// <remarks>
        /// The out-of-process WinForms designer does not forward mouse messages to this
        /// designer's <see cref="WndProc"/>, so nothing here was ever reached and neither tabs,
        /// panels nor the panel pill could be clicked. The ribbon control does receive the mouse
        /// messages (that is why the orb works), so it calls this instead.
        /// </remarks>
        public void OnRibbonMouseUp(int x, int y, bool rightButton)
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return;
            }

            HitOn(x, y);
        }

        /// <summary>
        /// Runs a pill from the ribbon's mouse-down. Kept for hosts that do deliver the control's
        /// mouse messages; the adorner glyph is the path that works in the out-of-process
        /// designer.
        /// </summary>
        /// <returns><c>true</c> when a pill was run.</returns>
        public bool OnRibbonPillMouseDown(int x, int y, bool rightButton)
        {
            if (!HandlePillAt(x, y, rightButton))
            {
                return false;
            }

            pillHandledOnDown = true;

            return true;
        }

        /// <summary>
        /// Runs the pill under a point on the ribbon, ignoring a repeat of the same click.
        /// </summary>
        /// <remarks>
        /// The same physical click can arrive from both the control and the adorner glyph, so a
        /// click at the same spot within a quarter of a second is treated as the one already
        /// handled and only adds a single element.
        /// </remarks>
        /// <returns><c>true</c> when a pill was hit.</returns>
        public bool HandlePillAt(int x, int y, bool rightButton)
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return false;
            }

            // A menu that is open takes the click: it either picks a row or dismisses itself,
            // so a stray click can never add an item by accident.
            if (_addItemMenuPanel != null)
            {
                RibbonPanel menuPanel = _addItemMenuPanel;
                int row = GetAddItemMenuRowIndex(menuPanel, x, y);

                CloseAddItemMenu();

                if (row >= 0 && row < AddablePanelItemTypes.Length)
                {
                    AddItemToPanel(menuPanel, AddablePanelItemTypes[row]);
                }

                return true;
            }

            int now = Environment.TickCount;

            if (now - _lastPillClickTick < 250 &&
                Math.Abs(_lastPillClickPoint.X - x) <= 3 &&
                Math.Abs(_lastPillClickPoint.Y - y) <= 3)
            {
                return false;
            }

            if (!HandlePillClick(x, y))
            {
                return false;
            }

            _lastPillClickTick = now;
            _lastPillClickPoint = new Point(x, y);

            _pillsAdorner?.Invalidate();

            return true;
        }

        /// <summary>
        /// Gets the area covered by the pills, in ribbon coordinates, for the adorner glyph.
        /// </summary>
        public Rectangle GetAddPillArea()
        {
            Rectangle area = GetAddTabPillBounds();

            RibbonTab tab = Ribbon == null || Ribbon.IsDisposed ? null : Ribbon.ActiveTab;

            if (tab != null)
            {
                Rectangle addPanel = GetAddPanelPillBounds(tab);

                if (!addPanel.IsEmpty)
                {
                    area = area.IsEmpty ? addPanel : Rectangle.Union(area, addPanel);
                }
            }

            foreach (RibbonPanel panel in GetPanelsWithAddButton())
            {
                Rectangle addItem = GetAddButtonPillBounds(panel);

                if (!addItem.IsEmpty)
                {
                    area = area.IsEmpty ? addItem : Rectangle.Union(area, addItem);
                }
            }

            return area;
        }

        /// <summary>
        /// Routes a design-time click from the ribbon control to the designer that owns it.
        /// </summary>
        public static void HandleRibbonMouseUp(Ribbon ribbon, int x, int y, bool rightButton)
        {
            For(ribbon)?.OnRibbonMouseUp(x, y, rightButton);
        }

        /// <summary>
        /// Gets the designer that owns the given ribbon.
        /// </summary>
        public static RibbonDesigner For(Ribbon ribbon)
        {
            if (ribbon == null || ribbon.IsDisposed)
            {
                return null;
            }

            RibbonDesigner designer = Current;

            if (designer != null && ReferenceEquals(designer.Ribbon, ribbon))
            {
                return designer;
            }

            return (designer?.GetService(typeof(IDesignerHost)) as IDesignerHost)
                ?.GetDesigner(ribbon) as RibbonDesigner;
        }

        /// <summary>
        /// Tracks which pill the mouse is over, so all three look and feel live.
        /// </summary>
        /// <returns><c>true</c> when the point is over a pill.</returns>
        public bool UpdatePillHover(int x, int y)
        {
            DesignerPill hovered = DesignerPill.None;
            RibbonPanel hoveredPanel = null;
            int hoveredRow = -1;
            bool overMenu = false;

            if (Ribbon != null && !Ribbon.IsDisposed)
            {
                if (_addItemMenuPanel != null &&
                    GetAddItemMenuBounds(_addItemMenuPanel).Contains(x, y))
                {
                    // The open menu is live: hand cursor anywhere on it, with the row under the
                    // mouse highlighted.
                    overMenu = true;
                    hovered = DesignerPill.AddItem;
                    hoveredPanel = _addItemMenuPanel;
                    hoveredRow = GetAddItemMenuRowIndex(_addItemMenuPanel, x, y);
                }
                else if (GetAddTabPillBounds().Contains(x, y))
                {
                    hovered = DesignerPill.AddTab;
                }
                else
                {
                    RibbonTab tab = Ribbon.ActiveTab;

                    if (tab != null && GetAddPanelPillBounds(tab).Contains(x, y))
                    {
                        hovered = DesignerPill.AddPanel;
                    }
                    else
                    {
                        foreach (RibbonPanel panel in GetPanelsWithAddButton())
                        {
                            if (!GetAddButtonPillBounds(panel).Contains(x, y))
                            {
                                continue;
                            }

                            hovered = DesignerPill.AddItem;
                            hoveredPanel = panel;
                            break;
                        }
                    }
                }
            }

            if (hovered != _hoverPill ||
                !ReferenceEquals(hoveredPanel, _hoverPillPanel) ||
                hoveredRow != _addItemMenuHover)
            {
                _hoverPill = hovered;
                _hoverPillPanel = hoveredPanel;
                _addItemMenuHover = hoveredRow;
                Ribbon?.Invalidate();
            }

            return overMenu || hovered != DesignerPill.None;
        }


        protected override void WndProc(ref Message m)
        {
            if (m.HWnd == Control.Handle)
            {
                switch (m.Msg)
                {
                    case 0x203: //WM_LBUTTONDBLCLK
                        AssignEventHandler();
                        break;
                    case 0x201: //WM_LBUTTONDOWN
                    case 0x204: //WM_RBUTTONDOWN
                        return;
                    case 0x202: //WM_LBUTTONUP
                        HitOn(WinApi.Get_X_LParam(m.LParam), WinApi.Get_Y_LParam(m.LParam));
                        return;
                    case 0x205: //WM_RBUTTONUP
                        HitOn(WinApi.Get_X_LParam(m.LParam), WinApi.Get_Y_LParam(m.LParam));
                        return;
                    default:
                        break;
                }
            }


            base.WndProc(ref m);

        }

        private void HitOn(int x, int y)
        {
            // A pill already ran on mouse-down, so do not run it a second time here.
            if (pillHandledOnDown)
            {
                pillHandledOnDown = false;
                return;
            }

            // The pills are checked first: they are live with nothing selected, and "Add Tab"
            // has to work on a ribbon that has no tabs at all yet.
            if (HandlePillClick(x, y))
            {
                return;
            }

            SelectAt(x, y);
        }

        /// <summary>
        /// Selects the tab, panel, item or context under a point on the ribbon.
        /// </summary>
        /// <remarks>
        /// Runs from the adorner glyph as well as from the ribbon's own mouse handling, because
        /// the out-of-process designer never delivers the click to the control. Without it a tab,
        /// panel or button exists on the surface but can never be selected to edit its
        /// properties.
        /// </remarks>
        public void SelectAt(int x, int y)
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return;
            }

            if (Ribbon.Tabs.Count == 0 || Ribbon.ActiveTab == null)
            {
                SelectRibbon();
                return;
            }

            if (Ribbon != null)
            {
                if (Ribbon.TabHitTest(x, y))
                {
                    SelectedElement = Ribbon.ActiveTab;
                }
                else if (Ribbon.ContextHitTest(x, y))
                {
                    foreach (RibbonContext context in Ribbon.Contexts)
                    {
                        if (context.Bounds.Contains(x, y))
                        {
                            SelectedElement = context;
                            break;
                        }
                    }
                }
                else
                {
                    #region Tab ScrollTest

                    if (Ribbon.ActiveTab.TabContentBounds.Contains(x, y))
                    {
                        if (Ribbon.ActiveTab.ScrollLeftBounds.Contains(x, y) && Ribbon.ActiveTab.ScrollLeftVisible)
                        {
                            Ribbon.ActiveTab.ScrollLeft();
                            SelectedElement = Ribbon.ActiveTab;
                            return;
                        }

                        if (Ribbon.ActiveTab.ScrollRightBounds.Contains(x, y) && Ribbon.ActiveTab.ScrollRightVisible)
                        {
                            Ribbon.ActiveTab.ScrollRight();
                            SelectedElement = Ribbon.ActiveTab;
                            return;
                        }
                    }

                    #endregion

                    //Check Panel
                    if (Ribbon.ActiveTab.TabContentBounds.Contains(x, y))
                    {
                        RibbonPanel hittedPanel = null;

                        foreach (RibbonPanel panel in Ribbon.ActiveTab.Panels)
                            if (panel.Bounds.Contains(x, y))
                            {
                                hittedPanel = panel;
                                break;
                            }

                        if (hittedPanel != null)
                        {
                            //Check item
                            RibbonItem hittedItem = null;

                            foreach (RibbonItem item in hittedPanel.Items)
                                if (item.Bounds.Contains(x, y))
                                {
                                    hittedItem = item;
                                    break;
                                }

                            if (hittedItem != null && hittedItem is IContainsSelectableRibbonItems)
                            {
                                //Check subitem
                                RibbonItem hittedSubItem = null;

                                foreach (RibbonItem subItem in (hittedItem as IContainsSelectableRibbonItems).GetItems())
                                    if (subItem.Bounds.Contains(x, y))
                                    {
                                        hittedSubItem = subItem;
                                        break;
                                    }

                                if (hittedSubItem != null)
                                {
                                    SelectedElement = hittedSubItem;
                                }
                                else
                                {
                                    SelectedElement = hittedItem;
                                }
                            }
                            else if (hittedItem != null)
                            {
                                SelectedElement = hittedItem;
                            }
                            else
                            {
                                SelectedElement = hittedPanel;
                            }
                        }
                        else
                        {
                            SelectedElement = Ribbon.ActiveTab;
                        }
                    }
                    else if (Ribbon.QuickAccessToolbar.SuperBounds.Contains(x, y))
                    {
                        bool itemHitted = false;

                        foreach (RibbonItem item in Ribbon.QuickAccessToolbar.Items)
                        {
                            if (item.Bounds.Contains(x, y))
                            {
                                itemHitted = true;
                                SelectedElement = item;
                                break;
                            }
                        }
                        if (!itemHitted)
                            SelectedElement = Ribbon.QuickAccessToolbar;
                    }
                    else if (Ribbon.OrbBounds.Contains(x, y))
                    {
                        Ribbon.OrbMouseDown();
                    }
                    else
                    {
                        SelectRibbon();

                        Ribbon.ForceOrbMenu = false;
                        Ribbon.OrbDropDown.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a click at a point would select a ribbon element.
        /// </summary>
        /// <remarks>
        /// Used for the adorner glyph's hit test: where this returns <c>false</c> the click falls
        /// through to the design surface, which selects the ribbon component itself. Areas owned
        /// by another glyph (the orb, the quick access toolbar) are left to those glyphs.
        /// </remarks>
        public bool CanSelectAt(int x, int y)
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return false;
            }

            if (_addItemMenuPanel != null &&
                GetAddItemMenuBounds(_addItemMenuPanel).Contains(x, y))
            {
                return false;
            }

            if (Ribbon.OrbVisible && Ribbon.OrbBounds.Contains(x, y))
            {
                return false;
            }

            if (Ribbon.QuickAccessToolbar.Visible &&
                Ribbon.QuickAccessToolbar.SuperBounds.Contains(x, y))
            {
                return false;
            }

            // Tab strip. Tab bounds are tested directly, because the ribbon's own tab hit test
            // also switches the active tab, which must not happen while only hovering.
            foreach (RibbonTab tab in Ribbon.Tabs)
            {
                if (tab.Bounds.Contains(x, y))
                {
                    return true;
                }
            }

            foreach (RibbonContext context in Ribbon.Contexts)
            {
                if (context.Bounds.Contains(x, y))
                {
                    return true;
                }
            }

            RibbonTab active = Ribbon.ActiveTab;

            // The tab content holds the panels and their items.
            return active != null && active.TabContentBounds.Contains(x, y);
        }

        /// <summary>
        /// Closes the orb menu as soon as the designer selection moves away from the orb items.
        /// </summary>
        /// <remarks>
        /// A click on the form or on another control never reaches the ribbon's own window
        /// procedure: the design surface turns it into a selection change. That is why the orb
        /// menu could only be closed by clicking the orb again.
        /// </remarks>
        private void selectionService_SelectionChanged(object? sender, EventArgs e)
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return;
            }

            // keep the menu open while its own items are being picked, otherwise the add item
            // glyphs would close it after every click
            if (_selectionService?.PrimarySelection is RibbonItem item &&
                Ribbon.OrbDropDown.AllItems.Contains(item))
            {
                return;
            }

            Ribbon.OrbDropDown.Close();
        }

        protected override void OnPaintAdornments(PaintEventArgs pe)
        {
            base.OnPaintAdornments(pe);

            using (Pen p = new Pen(Color.Black))
            {
                p.DashStyle = DashStyle.Dot;

                if (GetService(typeof(ISelectionService)) is ISelectionService host)
                {
                    foreach (IComponent comp in host.GetSelectedComponents())
                    {
                        if (comp is RibbonContext)
                        {
                            if (comp is RibbonContext item)
                            {
                                Rectangle selection = (item.ContextualTabsCount > 0) ? item.HeaderBounds : item.Bounds;
                                selection.Inflate(-1, -1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                        else if (comp is RibbonTab)
                        {
                            if (comp is RibbonTab item)
                            {

                                Rectangle selection = item.Bounds;
                                selection.Inflate(-1, -1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                        else if (comp is RibbonPanel)
                        {
                            if (comp is RibbonPanel item)
                            {
                                Rectangle selection = item.Bounds;
                                selection.Inflate(-1, -1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                        else if (comp is RibbonItem)
                        {
                            if (comp is RibbonItem item && !Ribbon.OrbDropDown.AllItems.Contains(item))
                            {
                                Rectangle selection = item.Bounds;
                                selection.Inflate(1, 1);

                                pe.Graphics.DrawRectangle(p, selection);
                            }
                        }
                    }
                }
            }

            PaintDesignerPills(pe.Graphics);
            PaintAddItemMenu(pe.Graphics);
        }

        #endregion

        #region Site

        public BehaviorService GetBehaviorService()
        {
            return BehaviorService;
        }

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);

            _changeService =
                GetService(typeof(IComponentChangeService))
                as IComponentChangeService;

            _designerHost =
                GetService(typeof(IDesignerHost))
                as IDesignerHost;

            _selectionService =
                GetService(typeof(ISelectionService))
                as ISelectionService;

            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -=
                    selectionService_SelectionChanged;

                _selectionService.SelectionChanged +=
                    selectionService_SelectionChanged;
            }

            if (_changeService != null)
            {
                _changeService.ComponentRemoved -=
                    changeService_ComponentRemoved;

                _changeService.ComponentRemoved +=
                    changeService_ComponentRemoved;

                _changeService.ComponentAdded +=
                    changeService_ComponentAdded;
            }

            _orbAdorner = new Adorner();

            if (BehaviorService != null && Ribbon != null)
            {
                BehaviorService.Adorners.Add(_orbAdorner);

                _orbAdorner.Glyphs.Add(
                    new RibbonOrbAdornerGlyph(
                        BehaviorService, this, Ribbon));

                // The pills are painted here, but only an adorner glyph receives the click in
                // the out-of-process designer (the orb works the same way).
                _pillsAdorner = new Adorner();

                BehaviorService.Adorners.Add(_pillsAdorner);

                _pillsAdorner.Glyphs.Add(
                    new RibbonAddPillGlyph(BehaviorService, this, Ribbon));

                if (Ribbon.QuickAccessToolbar.Visible)
                {
                    _quickAccessAdorner = new Adorner();

                    BehaviorService.Adorners.Add(_quickAccessAdorner);

                    _quickAccessAdorner.Glyphs.Add(
                        new RibbonQuickAccessToolbarGlyph(
                            BehaviorService, this, Ribbon));
                }
                else
                {
                    _quickAccessAdorner = null;
                }
            }
        }

        /// <summary>
        /// Size of the "Add Button" pill drawn on a panel.
        /// </summary>
        private static readonly Size AddButtonPillSize = new Size(80, 16);

        /// <summary>
        /// Gets the panels that show an "Add Button" pill: the panels of the tab that is showing.
        /// Panels of other tabs keep stale bounds, so they are left out.
        /// </summary>
        private IEnumerable<RibbonPanel> GetPanelsWithAddButton()
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                yield break;
            }

            RibbonTab tab = Ribbon.ActiveTab;

            if (tab == null)
            {
                yield break;
            }

            foreach (RibbonPanel panel in tab.Panels)
            {
                yield return panel;
            }
        }

        /// <summary>
        /// Works out where the "Add Control" pill goes on a panel, in ribbon coordinates. It sits
        /// under the last row of items, using the ribbon's bottom padding when the panel runs to
        /// its edge, so it does not cover the items. A narrow panel lets the pill run past its
        /// own edge so the caption still reads "Add Control".
        /// </summary>
        private static Rectangle GetAddButtonPillBounds(RibbonPanel panel)
        {
            if (panel == null)
            {
                return Rectangle.Empty;
            }

            Rectangle panelBounds = panel.Bounds;

            if (panelBounds.Width < 16 || panelBounds.Height < 12)
            {
                return Rectangle.Empty;
            }

            Ribbon owner = panel.Owner;
            int available = owner == null || owner.IsDisposed
                ? panelBounds.Width - 2
                : owner.Width - panelBounds.Left - 2;

            int width = Math.Min(AddButtonPillSize.Width, Math.Max(24, available));

            int itemsBottom = panelBounds.Top + 2;

            foreach (RibbonItem item in panel.Items)
            {
                if (item == null || item.Bounds.Height <= 0)
                {
                    continue;
                }

                itemsBottom = Math.Max(itemsBottom, item.Bounds.Bottom);
            }

            int bottomLimit = panelBounds.Bottom - 1;
            Ribbon ribbon = panel.Owner;

            if (ribbon != null && ribbon.Height > 0)
            {
                bottomLimit = Math.Max(bottomLimit, ribbon.Height - 1);
            }

            int top = itemsBottom + 2;
            int height = Math.Min(AddButtonPillSize.Height, bottomLimit - top);

            if (height < 10)
            {
                // The panel is full: use the panel's bottom edge and accept a small overlap.
                height = Math.Min(AddButtonPillSize.Height, panelBounds.Height - 2);
                top = panelBounds.Bottom - 1 - height;
            }

            return new Rectangle(panelBounds.Left + 1, top, width, height);
        }

        /// <summary>
        /// Item types a panel accepts, in the order the pill's menu offers them. A panel rejects
        /// <see cref="RibbonDescriptionMenuItem"/>, so it is left out.
        /// </summary>
        private static readonly Type[] AddablePanelItemTypes =
        {
            typeof(RibbonButton),
            typeof(RibbonComboBox),
            typeof(RibbonTextBox),
            typeof(RibbonCheckBox),
            typeof(RibbonUpDown),
            typeof(RibbonLabel),
            typeof(RibbonButtonList),
            typeof(RibbonItemGroup),
            typeof(RibbonColorChooser),
            typeof(RibbonHost),
            typeof(RibbonSeparator)
        };

        /// <summary>
        /// Size of the "Add Tab" and "Add Panel" pills.
        /// </summary>
        private static readonly Size AddEntryPillSize = new Size(60, 16);

        /// <summary>
        /// Size of one row in the panel's "Add Control" menu.
        /// </summary>
        private static readonly Size AddItemMenuRowSize = new Size(112, 17);

        private const int AddItemMenuPadding = 4;

        /// <summary>
        /// Gets the tabs that show an "Add Panel" pill: the one that is showing, because the
        /// others keep stale bounds.
        /// </summary>
        private IEnumerable<RibbonTab> GetTabsWithAddPanel()
        {
            RibbonTab tab = Ribbon == null || Ribbon.IsDisposed ? null : Ribbon.ActiveTab;

            if (tab != null)
            {
                yield return tab;
            }
        }

        /// <summary>
        /// Where the "Add Tab" pill goes: just after the last tab. In ribbon coordinates.
        /// </summary>
        private Rectangle GetAddTabPillBounds()
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return Rectangle.Empty;
            }

            Rectangle bounds = new Rectangle(
                5,
                Ribbon.OrbBounds.Bottom + 5,
                AddEntryPillSize.Width,
                AddEntryPillSize.Height);

            if (Ribbon.Tabs.Count > 0)
            {
                Rectangle last = Ribbon.Tabs[Ribbon.Tabs.Count - 1].Bounds;

                bounds.Y = last.Top + 2;
                bounds.X = Ribbon.RightToLeft == RightToLeft.Yes
                    ? last.Left - 5 - bounds.Width
                    : last.Right + 5;
            }

            return bounds;
        }

        /// <summary>
        /// Where a tab's "Add Panel" pill goes: just after that tab's last panel, in ribbon
        /// coordinates.
        /// </summary>
        private Rectangle GetAddPanelPillBounds(RibbonTab tab)
        {
            if (tab == null || Ribbon == null || Ribbon.IsDisposed)
            {
                return Rectangle.Empty;
            }

            Rectangle bounds = new Rectangle(
                5,
                tab.TabBounds.Bottom + 5,
                AddEntryPillSize.Width,
                AddEntryPillSize.Height);

            if (tab.Panels.Count > 0)
            {
                Rectangle last = tab.Panels[tab.Panels.Count - 1].Bounds;

                bounds.X = Ribbon.RightToLeft == RightToLeft.Yes
                    ? last.Left - 5 - bounds.Width
                    : last.Right + 5;
            }

            return bounds;
        }

        /// <summary>
        /// Runs the pill under the given point, if any: "Add Tab", "Add Panel", or a panel's
        /// "Add Button". A right click on a panel's pill offers the other item types.
        /// </summary>
        /// <returns><c>true</c> when a pill was hit.</returns>
        private bool HandlePillClick(int x, int y)
        {
            if (Ribbon == null || Ribbon.IsDisposed)
            {
                return false;
            }

            if (GetAddTabPillBounds().Contains(x, y))
            {
                AddTabVerb(this, EventArgs.Empty);
                return true;
            }

            foreach (RibbonTab tab in GetTabsWithAddPanel())
            {
                if (!GetAddPanelPillBounds(tab).Contains(x, y))
                {
                    continue;
                }

                AddPanelToTab(tab);
                return true;
            }

            foreach (RibbonPanel panel in GetPanelsWithAddButton())
            {
                if (!GetAddButtonPillBounds(panel).Contains(x, y))
                {
                    continue;
                }

                // The pill offers every item type a panel accepts, rather than adding a
                // button straight away.
                OpenAddItemMenu(panel);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens the item menu of a panel.
        /// </summary>
        public void OpenAddItemMenu(RibbonPanel panel)
        {
            if (panel == null || Ribbon == null || Ribbon.IsDisposed)
            {
                return;
            }

            _addItemMenuPanel = panel;
            _addItemMenuHover = -1;

            // A WinForms menu cannot stay open here: it is shown by the designer's own process,
            // so it loses activation the moment the mouse moves over the design surface. The menu
            // is therefore drawn on the surface, and this watches the pointer to close it again
            // when it leaves the pill and the menu.
            if (_addItemMenuWatcher == null)
            {
                _addItemMenuWatcher = new System.Windows.Forms.Timer { Interval = 120 };
                _addItemMenuWatcher.Tick += AddItemMenuWatcherTick;
            }

            if (!_addItemMenuWatcher.Enabled)
            {
                _addItemMenuWatcher.Start();
            }

            Ribbon.Invalidate();
        }

        /// <summary>
        /// Closes the panel's item menu, if one is open.
        /// </summary>
        public void CloseAddItemMenu()
        {
            if (_addItemMenuPanel == null)
            {
                return;
            }

            _addItemMenuPanel = null;
            _addItemMenuHover = -1;

            _addItemMenuWatcher?.Stop();

            Ribbon?.Invalidate();
        }

        /// <summary>
        /// Gets whether a panel's item menu is open.
        /// </summary>
        public bool IsAddItemMenuOpen => _addItemMenuPanel != null;

        /// <summary>
        /// Closes the menu once the pointer is no longer over the pill or the menu itself.
        /// </summary>
        private void AddItemMenuWatcherTick(object sender, EventArgs e)
        {
            if (_addItemMenuPanel == null || Ribbon == null || Ribbon.IsDisposed)
            {
                CloseAddItemMenu();
                return;
            }

            if (!Ribbon.IsHandleCreated)
            {
                return;
            }

            Point pointer = Ribbon.PointToClient(Control.MousePosition);

            if (!IsPointOnAddItemMenu(_addItemMenuPanel, pointer.X, pointer.Y))
            {
                CloseAddItemMenu();
            }
        }

        /// <summary>
        /// Gets whether a point is on the open menu or on the pill that opened it, so the pointer
        /// can travel between them without the menu closing.
        /// </summary>
        private bool IsPointOnAddItemMenu(RibbonPanel panel, int x, int y)
        {
            Rectangle pill = GetAddButtonPillBounds(panel);
            Rectangle menu = GetAddItemMenuBounds(panel);

            if (!pill.IsEmpty)
            {
                pill.Inflate(3, 3);
            }

            if (!menu.IsEmpty)
            {
                menu.Inflate(3, 3);
            }

            return (menu.Contains(x, y)) || (pill.Contains(x, y));
        }

        /// <summary>
        /// Works out the menu layout for a panel: rows per column, columns, and the menu bounds
        /// in ribbon coordinates. The menu sits against the pill and is kept inside the ribbon,
        /// wrapping into columns when the ribbon is too short for a single list.
        /// </summary>
        private Rectangle GetAddItemMenuBounds(
            RibbonPanel panel,
            out int rowsPerColumn,
            out int columns)
        {
            rowsPerColumn = 1;
            columns = 1;

            if (panel == null || Ribbon == null || Ribbon.IsDisposed)
            {
                return Rectangle.Empty;
            }

            int count = AddablePanelItemTypes.Length;
            int rowHeight = AddItemMenuRowSize.Height;
            int usable = Math.Max(rowHeight, Ribbon.Height - AddItemMenuPadding * 2);

            rowsPerColumn = Math.Max(1, (usable - AddItemMenuPadding * 2) / rowHeight);
            rowsPerColumn = Math.Min(rowsPerColumn, count);
            columns = (count + rowsPerColumn - 1) / rowsPerColumn;

            int width = columns * AddItemMenuRowSize.Width + AddItemMenuPadding * 2;
            int height = Math.Min(rowsPerColumn, count) * rowHeight + AddItemMenuPadding * 2;

            Rectangle pill = GetAddButtonPillBounds(panel);
            Rectangle anchor = pill.IsEmpty ? panel.Bounds : pill;

            int x = anchor.Left;
            int y = pill.IsEmpty ? anchor.Bottom + 1 : anchor.Bottom + 1;

            if (y + height > Ribbon.Height - 1)
            {
                y = Math.Max(1, anchor.Top - height - 1);
            }

            if (x + width > Ribbon.Width - 1)
            {
                x = Math.Max(1, Ribbon.Width - 1 - width);
            }

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// Gets the bounds of the open item menu, or <see cref="Rectangle.Empty"/>.
        /// </summary>
        public Rectangle GetAddItemMenuBounds(RibbonPanel panel)
        {
            return GetAddItemMenuBounds(panel, out _, out _);
        }

        /// <summary>
        /// Gets one row of the item menu, in ribbon coordinates.
        /// </summary>
        private Rectangle GetAddItemMenuRowBounds(RibbonPanel panel, int index)
        {
            Rectangle menu = GetAddItemMenuBounds(panel, out int rowsPerColumn, out _);

            if (menu.IsEmpty || index < 0 || index >= AddablePanelItemTypes.Length)
            {
                return Rectangle.Empty;
            }

            int column = index / rowsPerColumn;
            int row = index % rowsPerColumn;

            return new Rectangle(
                menu.Left + AddItemMenuPadding + column * AddItemMenuRowSize.Width,
                menu.Top + AddItemMenuPadding + row * AddItemMenuRowSize.Height,
                AddItemMenuRowSize.Width,
                AddItemMenuRowSize.Height);
        }

        /// <summary>
        /// Gets the item menu row under a point, or -1.
        /// </summary>
        private int GetAddItemMenuRowIndex(RibbonPanel panel, int x, int y)
        {
            if (panel == null || !GetAddItemMenuBounds(panel).Contains(x, y))
            {
                return -1;
            }

            for (int i = 0; i < AddablePanelItemTypes.Length; i++)
            {
                if (GetAddItemMenuRowBounds(panel, i).Contains(x, y))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Paints the open item menu on the design surface.
        /// </summary>
        private void PaintAddItemMenu(Graphics graphics)
        {
            RibbonPanel panel = _addItemMenuPanel;

            if (graphics == null || panel == null)
            {
                return;
            }

            Rectangle bounds = GetAddItemMenuBounds(panel);

            if (bounds.IsEmpty)
            {
                return;
            }

            SmoothingMode smoothing = graphics.SmoothingMode;

            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (SolidBrush background = new SolidBrush(Color.White))
                using (Pen border = new Pen(SystemColors.ControlDark))
                {
                    graphics.FillRectangle(background, bounds);
                    graphics.DrawRectangle(border, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
                }

                using (StringFormat format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                {
                    for (int i = 0; i < AddablePanelItemTypes.Length; i++)
                    {
                        Rectangle row = GetAddItemMenuRowBounds(panel, i);

                        if (row.IsEmpty)
                        {
                            continue;
                        }

                        bool hovered = i == _addItemMenuHover;

                        if (hovered)
                        {
                            using (SolidBrush highlight = new SolidBrush(Color.FromArgb(200, 32, 96, 196)))
                            {
                                graphics.FillRectangle(highlight, row);
                            }
                        }

                        graphics.DrawString(
                            GetItemTypeName(AddablePanelItemTypes[i]),
                            SystemFonts.DefaultFont,
                            hovered ? Brushes.White : Brushes.Black,
                            new Rectangle(row.Left + 5, row.Top, row.Width - 8, row.Height),
                            format);
                    }
                }
            }
            finally
            {
                graphics.SmoothingMode = smoothing;
            }
        }

        /// <summary>
        /// Adds an item of the given type to the panel under the pill and selects it, so it can be
        /// renamed straight away in the Properties grid.
        /// </summary>
        private void AddItemToPanel(RibbonPanel panel, Type itemType)
        {
            if (panel == null || itemType == null || Ribbon == null || Ribbon.IsDisposed)
            {
                return;
            }

            int before = panel.Items.Count;
            CreateItem(Ribbon, panel.Items, itemType);

            if (panel.Items.Count == before)
            {
                return;
            }

            Ribbon.OnRegionsChanged();
            Ribbon.Invalidate(true);
            Ribbon.Update();

            RibbonItem added = panel.Items[panel.Items.Count - 1];

            if (_selectionService != null)
            {
                _selectionService.SetSelectedComponents(
                    new Component[] { added },
                    SelectionTypes.Primary);
            }
        }

        /// <summary>
        /// Turns a ribbon item type into the words shown in the pill menu: RibbonComboBox
        /// becomes "Combo Box", RibbonItemGroup becomes "Item Group".
        /// </summary>
        private static string GetItemTypeName(Type itemType)
        {
            string name = itemType.Name;

            if (name.StartsWith("Ribbon", StringComparison.Ordinal))
            {
                name = name.Substring("Ribbon".Length);
            }

            StringBuilder spaced = new StringBuilder(name.Length + 4);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1]))
                {
                    spaced.Append(' ');
                }

                spaced.Append(c);
            }

            return spaced.ToString();
        }

        private void PaintDesignerPills(Graphics graphics)
        {
            if (graphics == null)
            {
                return;
            }

            SmoothingMode smoothing = graphics.SmoothingMode;

            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                PaintPill(
                    graphics,
                    GetAddTabPillBounds(),
                    "Add Tab",
                    _hoverPill == DesignerPill.AddTab);

                RibbonTab tab = Ribbon == null || Ribbon.IsDisposed ? null : Ribbon.ActiveTab;

                if (tab != null)
                {
                    PaintPill(
                        graphics,
                        GetAddPanelPillBounds(tab),
                        "Add Panel",
                        _hoverPill == DesignerPill.AddPanel);
                }

                foreach (RibbonPanel panel in GetPanelsWithAddButton())
                {
                    bool hovered = _hoverPill == DesignerPill.AddItem &&
                                   ReferenceEquals(panel, _hoverPillPanel);

                    PaintPill(graphics, GetAddButtonPillBounds(panel), "Add Control", hovered);
                }
            }
            finally
            {
                graphics.SmoothingMode = smoothing;
            }
        }

        /// <summary>
        /// Paints one design-time pill, brighter while the mouse is over it.
        /// </summary>
        private static void PaintPill(Graphics graphics, Rectangle bounds, string caption, bool hovered)
        {
            if (graphics == null || bounds.IsEmpty)
            {
                return;
            }

            using (GraphicsPath path = RibbonProfessionalRenderer.RoundRectangle(bounds, 3))
            using (SolidBrush brush = new SolidBrush(
                       hovered
                           ? Color.FromArgb(225, 32, 96, 196)
                           : Color.FromArgb(150, 32, 96, 196)))
            using (StringFormat format = StringFormatFactory.CenterNoWrap(StringTrimming.None))
            {
                graphics.FillPath(brush, path);

                graphics.DrawString(
                    bounds.Width < 52 || bounds.Height < 14 ? "+" : caption,
                    SystemFonts.DefaultFont,
                    Brushes.White,
                    bounds,
                    format);
            }
        }


        /// <summary>
        /// Catches the event of a component on the ribbon being added
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void changeService_ComponentAdded(object sender, ComponentEventArgs e)
        {
            // The pills are painted on demand, so a new component only needs a repaint.
            Ribbon?.Invalidate();
        }

        /// <summary>
        /// Catches the event of a component on the ribbon being removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void changeService_ComponentRemoved(object sender, ComponentEventArgs e)
        {
            if (ReferenceEquals(e.Component, Component))
            {
                if (_changeService != null)
                {
                    _changeService.ComponentRemoved -=
                        changeService_ComponentRemoved;

                    _changeService = null;
                }

                if (_verbs != null)
                {
                    foreach (DesignerVerb verb in _verbs)
                    {
                        verb.Visible = false;
                        verb.Enabled = false;
                    }
                }

                return;
            }

            RibbonTab tab = e.Component as RibbonTab; RibbonContext context = e.Component as RibbonContext; RibbonPanel panel = e.Component as RibbonPanel; RibbonItem item = e.Component as RibbonItem;

            IDesignerHost designerService =
                GetService(typeof(IDesignerHost)) as IDesignerHost;

            RemoveRecursive(
                e.Component as IContainsRibbonComponents,
                designerService);

            if (tab != null && Ribbon != null)
            {
                Ribbon.Tabs.Remove(tab);
            }
            else if (context != null && Ribbon != null)
            {
                Ribbon.Contexts.Remove(context);
            }
            else if (panel != null && panel.OwnerTab != null)
            {
                panel.OwnerTab.Panels.Remove(panel);
            }
            else if (item != null)
            {
                if (item.Canvas is RibbonOrbDropDown)
                {
                    Ribbon.OrbDropDown.HandleDesignerItemRemoved(item);
                }
                else if (item.OwnerItem is RibbonItemGroup group)
                {
                    group.Items.Remove(item);
                }
                else if (item.OwnerPanel != null)
                {
                    item.OwnerPanel.Items.Remove(item);
                }
                else if (Ribbon != null &&
                         Ribbon.QuickAccessToolbar.Items.Contains(item))
                {
                    Ribbon.QuickAccessToolbar.Items.Remove(item);
                }
            }

            SelectedElement = null;

            if (Ribbon != null)
            {
                Ribbon.OnRegionsChanged();
                Ribbon.Invalidate(true);
                Ribbon.Update();
            }


        }

        public void RemoveRecursive(IContainsRibbonComponents item, IDesignerHost service)
        {
            if (item == null || service == null) return;

            // Snapshot the children before destroying components.
            System.Collections.Generic.List<Component> children =
                new System.Collections.Generic.List<Component>(
                    item.GetAllChildComponents());

            foreach (Component child in children)
            {
                if (child == null)
                    continue;

                if (child is IContainsRibbonComponents components)
                {
                    RemoveRecursive(components, service);
                }

                // Destroy the component only if it still belongs
                // to a container.
                if (child.Site != null &&
                    child.Site.Container != null)
                {
                    service.DestroyComponent(child);
                }
            }


        }

        #endregion
    }
}