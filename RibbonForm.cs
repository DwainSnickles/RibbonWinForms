using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;
using RibbonLib.RibbonHelpers;

namespace RibbonLib
{
    // Named rather than typeof(...): a type token makes every runtime resolution of this
    // attribute demand Microsoft.DotNet.DesignTools.Server, which does not exist outside
    // Visual Studio. See Ribbon.cs for the same change.
    [Designer("RibbonLib.RibbonDesign.RibbonDesigner, RibbonLib.Design")]
    public partial class RibbonForm : Form, IRibbonForm
    {
        #region Fields

        private bool? _isOpenInVisualStudioDesigner;
        private Ribbon ribbon1;
        private Ribbon ribbonControl;

        #endregion

        #region Ctor

        public RibbonForm()
            : base()
        {
            InitializeRibbon();

            if (!IsOpenInVisualStudioDesigner())
            {
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.DoubleBuffered = true;
                this.FormBorderStyle = FormBorderStyle.None;
            }

            Helper = new RibbonFormHelper(this);
        }

        #endregion

        public void BeginInit()
        {
            // Called when designer begins initialization
        }

        public void EndInit()
        {
            // Called when designer finishes initialization
            // THIS is where you can detect “added to form”
            if (this.Site != null && this.Site.DesignMode)
            {
                var hostForm = this.Site.Container.Components
                    .OfType<Form>()
                    .FirstOrDefault();

                if (hostForm != null)
                {
                    // Ribbon has been added to a Form at design time
                    // You can now auto-create tabs, panels, etc.
                }
            }
        }

        #region Designer detection & Ribbon initialization

        protected void InitializeRibbon()
        {
            ribbonControl = new Ribbon
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(ribbonControl);

            var tab = new RibbonTab("Home");
            ribbonControl.Tabs.Add(tab);

            var panel = new RibbonPanel("Actions");
            tab.Panels.Add(panel);

            panel.Items.Add(new RibbonButton("Run"));
            //var context = new RibbonContext("Default Group");
            //ribbonControl.Contexts.Add(context);

            //var tab = new RibbonTab("Default Tab")
            //{
            //    Context = context
            //};
            //ribbonControl.Tabs.Add(tab);

            //var panel = new RibbonPanel("Default Panel");
            //tab.Panels.Add(panel);

            //var button = new RibbonButton("Default Button");
            //panel.Items.Add(button);
        }

        private void ConfigureRibbon()
        {
            var context = new RibbonContext("Tools");
            ribbonControl.Contexts.Add(context);

            var tab = new RibbonTab("Tools");
            tab.Context = context;
            ribbonControl.Tabs.Add(tab);

            var panel = new RibbonPanel("Actions");
            tab.Panels.Add(panel);

            panel.Items.Add(new RibbonButton("Run"));
        }

        private void InitializeComponent()
        {

        }

        protected bool IsOpenInVisualStudioDesigner()
        {
            if (_isOpenInVisualStudioDesigner.HasValue)
                return _isOpenInVisualStudioDesigner.Value;

            // Primary check: LicenseManager + DesignMode
            bool inDesigner =
                LicenseManager.UsageMode == LicenseUsageMode.Designtime ||
                DesignMode;

            if (!inDesigner)
            {
                try
                {
                    using (var process = Process.GetCurrentProcess())
                    {
                        // If running inside Visual Studio (devenv), treat as designer
                        var name = process.ProcessName.ToLowerInvariant();
                        inDesigner = name.Contains("devenv");
                    }
                }
                catch
                {
                    // If anything fails, be conservative: assume runtime
                    inDesigner = false;
                }
            }

            _isOpenInVisualStudioDesigner = inDesigner;
            return inDesigner;
        }

        #endregion

        #region Overrides

        //No overrides required at this stage

        #endregion

        #region IRibbonForm Members

        public RibbonFormHelper Helper { get; }

        public Ribbon RibbonControl => ribbonControl;

        #endregion
    }
}