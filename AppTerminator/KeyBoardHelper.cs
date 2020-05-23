using System;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using VideoPlayerController;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Management;
using System.Media;
using AppTerminator;
#if DEBUG
using NLog;
#endif

namespace KbHelper
{
    public class KeyBoardHelper
    {
#if DEBUG
        private static Logger log = LogManager.GetCurrentClassLogger();
#endif      
        public string ProcName;
        public static string xmlFile = "settings.xml";
        public static string WorkDirectory = Environment.CurrentDirectory;

        public KeyBoardHelper()
        {
#if DEBUG
            log.Debug("KeyBoardHelper::KeyBoardHelper()");
#endif
        }

        public string GetSpecialSymbol()
        {
            string s;
            byte[] symbol = new byte[] { 0x22 };
            s = Encoding.ASCII.GetString(symbol);
            return s;
        }

        public bool IsAutorunFlag()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
            int len = Convert.ToString(key.GetValue("AppTerminator")).Length;

            if (len > 0) return true;
            else return false;
        }

        public void SetupSettingFilePath()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            int len = Convert.ToString(key.GetValue("SettingFilePath")).Length;

            if (len == 0)
            {
                key.SetValue("SettingFilePath", WorkDirectory + "/settings.xml");
            }
        }

        public string GetSettingFilePath()
        {
            string path;
            var Subkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\KeyBoardHelper\\", true);
            path = Convert.ToString(Subkey.GetValue("SettingFilePath"));
            int len = path.Length;

            if (len > 0)
            {
                return path;
            }
            return "";
        }             
    }

    public class KeyBoardHelperNative
    {
        private Form1 MainForm;
        public AudioManager audioMgr;

        public KeyBoardHelperNative(Form1 form)
        {
            MainForm = form;
            audioMgr = new AudioManager();
        }

        protected SoundPlayer sp;
        protected Process[] proc;
        public bool CD_Drive_state = false;

        //MessageBox флаги.
        const int MB_YESNO = 0x00000004;
        const int MB_ICONQUESTION = 0x00000020;

        //ShowWindowAsync флаги.
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        //WinApi импорт.
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern UInt32 GetWindowThreadProcessId(IntPtr hwnd, ref Int32 pid);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
        protected static extern int mciSendString (string mciCommand, StringBuilder returnValue, int returnLength, IntPtr callback);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop,
        EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int _GetWindowText(IntPtr hWnd,
        StringBuilder lpWindowText, int nMaxCount);

        private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        public String GetActiveWindowName()
        {
            IntPtr h;
            int pid = 0;
            h = GetForegroundWindow();
            GetWindowThreadProcessId(h, ref pid);
            Process p = Process.GetProcessById(pid);
            return p.ProcessName;
        }

        public String GetActiveWindowProcessName()
        {
            IntPtr h = IntPtr.Zero;
            int pid = 0;
            h = GetForegroundWindow();
            GetWindowThreadProcessId(h, ref pid);
            Process p = Process.GetProcessById(pid);
            return p.ProcessName;
        }

        public bool CheakActiveWindowProcess()
        {
            bool cheak_result = false;
            IntPtr h = IntPtr.Zero;
            int pid = 0;
            h = GetForegroundWindow();
            GetWindowThreadProcessId(h, ref pid);
            Process p = Process.GetProcessById(pid);
            if (p.ProcessName != "explorer" && p.ProcessName != "AppTerminator")
            {
                cheak_result = true;
            }
            return cheak_result;
        }

        public void TerminateActiveWindowProcess(bool notifi)
        {
            if (ShowTopQuestionMessageBox())
            {
                string ProcName = GetActiveWindowName();
                ProcName = ProcName.Substring(0, 1).ToUpper() + ProcName.Remove(0, 1);                
                IntPtr h = IntPtr.Zero;
                int pid = 0;
                h = GetForegroundWindow();
                GetWindowThreadProcessId(h, ref pid);
                Process p = Process.GetProcessById(pid);
                if (p.ProcessName != "explorer" && p.ProcessName != "AppTerminator")
                    p.Kill();
                MainForm.notifiIcon1.BalloonTipText = ProcName + " завершен!";
                if(notifi)
                    MainForm.notifiIcon1.ShowBalloonTip(500);
            }
        }

        public void StartCMD()
        {
            Process.Start("cmd.exe");
        }

        public void MuteSound()
        {
            audioMgr.SetMasterVolumeMute(!audioMgr.GetMasterVolumeMute());
        }

        public void AddMasterVolume(int step)
        {
            int MasterVolume = Convert.ToInt32(audioMgr.GetMasterVolume());
            int NewMasterVolume = MasterVolume + step;

            if (MasterVolume < 100)
            {
                if (NewMasterVolume > 100)
                    NewMasterVolume = 100;
                audioMgr.SetMasterVolume(NewMasterVolume);
                //int val1 = MasterVolume % 10;
                //int addTo = 10 - val1;                        
                //audioMgr.SetMasterVolume(MasterVolume + addTo);                        
            }
        } 

        public void ReduceMasterVolume(int step)
        {
            int MasterVolume = Convert.ToInt32(audioMgr.GetMasterVolume());
            int NewMasterVolume = MasterVolume - step;

            if (MasterVolume > 0)
            {
                if (NewMasterVolume < 0)
                    NewMasterVolume = 0;
                audioMgr.SetMasterVolume(NewMasterVolume);
            }
        }
        
        public void PlayWindowSound()
        {
            if (File.Exists("sound/nitify.wav"))
            {
                sp = new SoundPlayer(@"sound/nitify.wav");
                sp.Play();
            }
        }

        public void PlayProcessSound()
        {
            if (File.Exists("sound/process.wav"))
            {
                sp = new SoundPlayer(@"sound/process.wav");
                sp.Play();
            }
        }

        public void PlayButtonSwitchSound()
        {
            if (File.Exists("sound/switch.wav"))
            {
                sp = new SoundPlayer(@"sound/switch.wav");
                sp.Play();
            }
        }

        private static bool EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            if (hWnd != GetDesktopWindow())
            {
                if (IsWindowVisible(hWnd) & IsIconic(hWnd))
                {
                    ShowWindowAsync(hWnd, SW_RESTORE);
                }

                if (IsWindowVisible(hWnd) & !IsIconic(hWnd))
                {
                    ShowWindowAsync(hWnd, SW_MINIMIZE);
                }
            }

            return true;
        }

        public void MinimizeOpenWindows()
        {
            EnumDelegate delEnumfunc = new EnumDelegate(EnumWindowsProc);
            EnumDesktopWindows(IntPtr.Zero, delEnumfunc, IntPtr.Zero);
        }

        public void CD_DriveOpen()
        {
            if (!CD_Drive_state)
            {
                CD_Drive_state = !CD_Drive_state;
                mciSendString("set cdaudio door open", null, 0, IntPtr.Zero);
            }
            else
            {
                CD_Drive_state = !CD_Drive_state;
                mciSendString("set cdaudio door closed", null, 0, IntPtr.Zero);                
            }
        }

        public bool ShowTopQuestionMessageBox()
        {
            IntPtr h = IntPtr.Zero;
            int result;
            int pid = 0;
            h = GetForegroundWindow();
            GetWindowThreadProcessId(h, ref pid);
            Process p = Process.GetProcessById(pid);
            if (p.ProcessName != "explorer" && p.ProcessName != "AppTerminator")
            {
                result = MessageBox(h, "Завершить " + p.ProcessName + "?", "KeyBoard Helper", MB_YESNO | MB_ICONQUESTION);
                if (result == 6) return true;
                else
                    return false;
            }
            return false;
        }

        public bool SetForegdWindow(IntPtr windowHandle)
        {
            return SetForegroundWindow(windowHandle);
        }
    }
}