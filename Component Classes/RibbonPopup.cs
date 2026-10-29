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


using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Permissions;
using RibbonLib.RibbonDesign;
using RibbonLib.RibbonHelpers;

namespace RibbonLib
{
    [ToolboxItem(false)]
    public class RibbonPopup
        : Control
    {
        #region Fields

        #endregion

        #region Events

        public event EventHandler Showed;

        /// <summary>
        /// Raised when the popup is closed
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Raised when the popup is about to be closed
        /// </summary>
        public event ToolStripDropDownClosingEventHandler Closing;

        /// <summary>
        /// Raised when the Popup is about to be opened
        /// </summary>
        public event CancelEventHandler Opening;

        #endregion

        #region Ctor

        public RibbonPopup()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            BorderRoundness = 3;
        }

        #endregion

        #region Props

        /// <summary>
        /// Gets or sets the roundness of the border
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BorderRoundness { get; set; }


        /// <summary>
        /// Gets the related ToolStripDropDown
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal RibbonWrappedDropDown WrappedDropDown { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Shows this Popup on the specified location of the screen
        /// </summary>
        /// <param name="screenLocation"></param>
        public void Show(Point screenLocation)
        {
            if (WrappedDropDown == null)
            {
                ToolStripControlHost host = new ToolStripControlHost(this);
                WrappedDropDown = new RibbonWrappedDropDown
                {
                    AutoClose = RibbonDesignTime.IsActive
                };
                WrappedDropDown.Items.Add(host);

                WrappedDropDown.Padding = Padding.Empty;
                WrappedDropDown.Margin = Padding.Empty;
                host.Padding = Padding.Empty;
                host.Margin = Padding.Empty;

                WrappedDropDown.Opening += ToolStripDropDown_Opening;
                WrappedDropDown.Closing += ToolStripDropDown_Closing;
                WrappedDropDown.Closed += ToolStripDropDown_Closed;
                WrappedDropDown.Size = Size;
            }
            WrappedDropDown.Show(screenLocation);
            RibbonPopupManager.Register(this);

            OnShowed(EventArgs.Empty);
        }

        /// <summary>
        /// Handles the Opening event of the ToolStripDropDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripDropDown_Opening(object sender, CancelEventArgs e)
        {
            OnOpening(e);
        }

        /// <summary>
        /// Called when pop-up is being opened
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnOpening(CancelEventArgs e)
        {
            if (Opening != null)
            {
                Opening(this, e);
            }
        }

        /// <summary>
        /// Handles the Closing event of the ToolStripDropDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripDropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            OnClosing(e);
        }

        /// <summary>
        /// Handles the closed event of the ToolStripDropDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripDropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            OnClosed(EventArgs.Empty);
        }

        /// <summary>
        /// Closes this popup.
        /// </summary>
        public void Close()
        {
            if (WrappedDropDown != null)
            {
                WrappedDropDown.Close();
            }
        }

        /// <summary>
        /// Disposes the popup together with the drop-down window that hosts it.
        /// </summary>
        /// <remarks>
        /// Disposing only the hosted control leaves the ToolStripDropDown window on screen with a
        /// dead child inside it. That is what left the orb menu floating over the form after the
        /// ribbon had been disposed (designer reload, form closed), and any later click on it
        /// threw ObjectDisposedException from the ribbon accessors.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RibbonPopupManager.Unregister(this);

                RibbonWrappedDropDown wrappedDropDown = WrappedDropDown;
                WrappedDropDown = null;

                if (wrappedDropDown != null)
                {
                    try
                    {
                        if (!wrappedDropDown.IsDisposed && wrappedDropDown.Visible)
                            wrappedDropDown.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        // window was already gone
                    }
                    catch (InvalidOperationException)
                    {
                        // window is being destroyed: nothing left to close
                    }

                    wrappedDropDown.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClosing(ToolStripDropDownClosingEventArgs e)
        {
            if (Closing != null)
            {
                Closing(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="Closed"/> event.
        /// <remarks>If you override this event don't forget to call base! Otherwise the popup will not be unregistered and hook will not work!</remarks>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClosed(EventArgs e)
        {
            RibbonPopupManager.Unregister(this);

            if (Closed != null)
            {
                Closed(this, e);
            }

            //if (NextPopup != null)
            //{
            //    NextPopup.CloseForward();
            //    NextPopup = null;
            //}

            //if (PreviousPopup != null && PreviousPopup.NextPopup.Equals(this))
            //{
            //    PreviousPopup.NextPopup = null;
            //}
        }

        /// <summary>
        /// Raises the Showed event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnShowed(EventArgs e)
        {
            if (Showed != null)
            {
                Showed(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="Control.Paint"/> event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (GraphicsPath p = RibbonProfessionalRenderer.RoundRectangle(new Rectangle(Point.Empty, Size), BorderRoundness))
            {
                using (Region r = new Region(p))
                {
                    if (WrappedDropDown != null)
                    {
                        WrappedDropDown.Region = r;
                    }
                }
            }
        }

        /// <summary>
        /// Overriden. Used to drop a shadow on the popup
        /// </summary>
        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                CreateParams cp = base.CreateParams;

                if (WinApi.IsXP)
                {
                    cp.ClassStyle |= WinApi.CS_DROPSHADOW;

                }

                return cp;
            }
        }

        #endregion
    }
}
