﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFirewallNotifier
{

    static class Program
    {
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public uint cbData;
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)]
            public string lpData;
        }

        public static Mutex mutex;
        public static bool firstInstance;

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            try
            {
                showNotification(argv);
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to initialize WFN", e);

                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Dictionary<string, string> parseParameters(string[] args)
        {
            Dictionary<string, string> ret = null;
            try
            {
                ret = new Dictionary<string, string>(args.Length / 2);
                for (int i = 0; i < args.Length; i += 2)
                {
                    ret.Add(args[i], args[i + 1]);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Unable to parse the parameters: argv = " + String.Join(" ", args), e);
            }

            return ret;
        }

        
        /// <summary>
        /// 
        /// </summary>
        private static void showNotification(string[] argv)
        {
            MainForm mainForm;
            Process currentProc = Process.GetCurrentProcess();

            if (argv.Length == 0 || argv[1].Contains("$"))
            {
                argv = new string[] { "-pid", "0", "-ip", "127.0.0.1", "-port", "0", "-protocol", "0", "-localport", "0", "-path", "N/A" };
            }

            Dictionary<string, string> pars = parseParameters(argv);
            int pid = int.Parse(pars["-pid"]);

            uint SessID = (uint)currentProc.SessionId;

            if (WindowsIdentity.GetCurrent().IsSystem)
            {
                IntPtr userToken = IntPtr.Zero;
                try
                {
                    using (Process srcPr = Process.GetProcessById(pid))
                    {
                        SessID = (uint)srcPr.SessionId;
                    }
                }
                catch { }

                if (SessID == uint.MaxValue || SessID == 4 || SessID == 0)
                {
                    // Retrieves the currently active session if the process was not running
                    SessID = (uint)CommonHelper.WTSGetActiveConsoleSessionId();
                    if (SessID == 0xFFFFFFFF)
                    {
                        throw new Exception("No active session found. Aborting.");
                    }
                    /*else
                    {
                        int errCode = Marshal.GetLastWin32Error();
                        if (errCode != 0)
                        {
                            throw new Exception("Unable to retrieve the active session ID. ErrCode = " + errCode);
                        }
                    }*/
                }

                if (SessID != 0 && SessID != currentProc.SessionId)
                {
                    if (!CommonHelper.WTSQueryUserToken(SessID, ref userToken))
                    {
                        throw new Exception("Unable to retrieve the current user token. ErrCode = " + Marshal.GetLastWin32Error());
                    }

                    string argstr = String.Join(" ", argv.Select(a => a.Contains(" ") ? "\"" + a + "\"" : a).ToArray());
                    Impersonation.LaunchProcessAsUser(Application.ExecutablePath, argstr, userToken);
                }
                else
                {
                    throw new Exception("WFN can not start in the SYSTEM session.");
                }

                Environment.Exit(0);
            }

            mutex = new Mutex(true, "WindowsFirewallNotifierMutex_" + SessID, out firstInstance);

            string currentTarget = pars["-ip"];
            string currentTargetPort = pars["-port"];
            string currentProtocol = pars["-protocol"];
            string currentLocalPort = pars["-localport"];
            string currentPath = pars["-path"];
            pars = null;

            //            var notifications = System.Windows.UI.Notifications;

            //// Get the toast notification manager for the current app.
            //var notificationManager = notifications.ToastNotificationManager;

            //// The getTemplateContent method returns a Windows.Data.Xml.Dom.XmlDocument object
            //// that contains the toast notification XML content.
            //var template = notifications.toastTemplateType.toastImageAndText01;
            //var toastXml = notificationManager.getTemplateContent(notifications.ToastTemplateType[template]);

            //// You can use the methods from the XML document to specify the required elements for the toast.
            //var images = toastXml.getElementsByTagName("image");
            //images[0].setAttribute("src", "images/toastImageAndText.png");

            //var textNodes = toastXml.getElementsByTagName("text");
            //textNodes.forEach(function (value, index) {
            //    var textNumber = index + 1;
            //    var text = "";
            //    for (var j = 0; j < 10; j++) {
            //        text += "Text input " + /*@static_cast(String)*/textNumber + " ";
            //    }
            //    value.appendChild(toastXml.createTextNode(text));
            //});

            //// Create a toast notification from the XML, then create a ToastNotifier object
            //// to send the toast.
            //var toast = new notifications.ToastNotification(toastXml);

            //notificationManager.createToastNotifier().show(toast);
            Process prevInst = null;
            if (!firstInstance)
            {
                while (!firstInstance && prevInst == null)
                {
                    prevInst = Process.GetProcessesByName(currentProc.ProcessName)
                                      .FirstOrDefault(pr => pr.SessionId == SessID && pr.Id != currentProc.Id && pr.MainWindowHandle != null);

                    if (prevInst == null)
                    {
                        mutex = new Mutex(true, "WindowsFirewallNotifierMutex_" + SessID, out firstInstance);
                    }
                }
            }

            if (firstInstance)
            {
                mainForm = new MainForm(pid, currentPath, currentTarget, currentProtocol, currentTargetPort, currentLocalPort);
                if (!mainForm.IsDisposed)
                {
                    mainForm.FormClosing += new FormClosingEventHandler(mainForm_FormClosing);
                    Application.Run(mainForm);
                }
            }
            else
            {
                string msg = pid + "#$#" + currentPath + "#$#" + currentTarget + "#$#" + currentProtocol + "#$#" + currentTargetPort + "#$#" + currentLocalPort;

                COPYDATASTRUCT copyData;
                copyData.dwData = IntPtr.Zero;
                copyData.lpData = msg;
                copyData.cbData = (uint)(msg.Length + 1) * 2;

                SendMessage(prevInst.MainWindowHandle, 0x004A, IntPtr.Zero, ref copyData);
            }
        }

        static void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*Impersonation.Cancel();
            if (wic != null)
                wic.Undo();*/
        }
    }
}
