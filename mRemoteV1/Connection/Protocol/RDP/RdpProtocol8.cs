﻿using System;
using System.Drawing;
using System.Windows.Forms;
using AxMSTSCLib;
using mRemoteNG.App;
using mRemoteNG.Messages;
using MSTSCLib;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /* RDP v8 requires Windows 7 with:
		* https://support.microsoft.com/en-us/kb/2592687 
		* OR
		* https://support.microsoft.com/en-us/kb/2923545
		* 
		* Windows 8+ support RDP v8 out of the box.
		*/
    public class RdpProtocol8 : RdpProtocol7
    {
        private new MsRdpClient8NotSafeForScripting _rdpClient;
        private Size _controlBeginningSize;

        public override bool SmartSize
        {
            get { return base.SmartSize; }
            protected set
            {
                base.SmartSize = value;
                ReconnectForResize();
            }
        }

        public override bool Fullscreen
        {
            get => base.Fullscreen;
            protected set
            {
                base.Fullscreen = value;
                ReconnectForResize();
            }
        }

        public RdpProtocol8()
        {
            Control = new AxMsRdpClient8NotSafeForScripting();
        }

        public override void ResizeBegin(object sender, EventArgs e)
        {
            _controlBeginningSize = Control.Size;
        }

        public override void Resize(object sender, EventArgs e)
        {
            if (DoResize() && _controlBeginningSize.IsEmpty)
            {
                ReconnectForResize();
            }
            base.Resize(sender, e);
        }

        public override void ResizeEnd(object sender, EventArgs e)
        {
            DoResize();
            if (!(Control.Size == _controlBeginningSize))
            {
                ReconnectForResize();
            }
            _controlBeginningSize = Size.Empty;
        }

        protected override object CreateRdpClientControl()
        {
            _rdpClient = (MsRdpClient8NotSafeForScripting)((AxMsRdpClient8NotSafeForScripting)Control).GetOcx();
            return _rdpClient;
        }

        private void ReconnectForResize()
        {
            if (!loginComplete)
                return;

            if (!InterfaceControl.Info.AutomaticResize)
                return;

            if (!(InterfaceControl.Info.Resolution == RDPResolutions.FitToWindow ||
                  InterfaceControl.Info.Resolution == RDPResolutions.Fullscreen))
                return;

            if (SmartSize)
                return;

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"Resizing RDP connection to host '{connectionInfo.Hostname}'");

            try
            {
                var size = Fullscreen
                    ? Screen.FromControl(Control).Bounds.Size
                    : Control.Size;
                _rdpClient.Reconnect((uint)size.Width, (uint)size.Height);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(
                    string.Format(Language.ChangeConnectionResolutionError,
                        connectionInfo.Hostname),
                    ex, MessageClass.WarningMsg, false);
            }
        }

        private bool DoResize()
        {
            Control.Location = InterfaceControl.Location;
            // kmscode - this doesn't look right to me. But I'm not aware of any functionality issues with this currently...
            if (!(Control.Size == InterfaceControl.Size) && !(InterfaceControl.Size == Size.Empty))
            {
                Control.Size = InterfaceControl.Size;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
