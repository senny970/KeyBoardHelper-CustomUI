using System;
using System.ComponentModel;
using System.Windows.Forms;
using KbHelper;
using VideoPlayerController;
using Microsoft.Win32;
using System.Drawing;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.InteropServices;


#if DEBUG
using NLog;
#endif

namespace AppTerminator
{

    public partial class Form1 : Form
    {
#if DEBUG
            private static Logger log = LogManager.GetCurrentClassLogger();
#endif
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
        );

        public GlobalHook KeyHook;
        public KeyBoardHelperNative keyboardHelperNative;
        public AudioManager audioMgr;
        public KeyBoardHelper keyboardHelper;     
        KeyInput KeyInputForm;

        public ListViewItem focus_item = null;
        public DataGridViewRow focus_row = null;
        public DataGridViewRow focus_row_edit = null;
        public int ItemIndexToEdit = 0;
        public Item_t EditedItem;
        public string phrase;

        [Serializable]
        public struct Item_t
        {
            public string Event;
            public string Key;
            public string ModifierKey;
            public string Command;
            public string Options;
            public int ImageIndex;
        }

        public List<Item_t> EventsCollection = new List<Item_t>();
        public DataTable BindEventsTable = new DataTable();

        public Keys Bind_Key = Keys.None;
        public Keys Bind_ModiffiersKey = Keys.None;
        public static string xmlFile = "/Events.xml";
        public static string WorkDirectory = Environment.CurrentDirectory;
        public int PaneDataGrid1_Pos_X = 0;
        public bool AboutVisible = false;        
        public bool isEditKeysMode = false;
        public bool IsAutorunEnabled = false;
        public bool IsNotifyEnabled = false;
        public bool isCmdEnabled = false;
        public bool isCmdFormShow = false;
        public bool isKeyInputFormShow = false;
        public string Command = "";
        public string CommandEvent = "";
        public string CommandEventOptions = "";
        public string CommandEventCommand = "";
        public char word;

        void SetupAutorunImage()
        {
            if (IsAutorunEnabled)
            {
                pictureBox3.Image = Properties.Resources.autorun_glow;                      
            }
            else
            {
                pictureBox3.Image = Properties.Resources.autorun;
            }
            pictureBox3.Refresh();
        }

        void SetupNotifyImage()
        {
            if (IsNotifyEnabled)
                pictureBox4.Image = Properties.Resources.notify_glow;
            else
                pictureBox4.Image = Properties.Resources.notify;
            pictureBox4.Refresh();
        }

        void SetupCMDImage()
        {
            if (isCmdEnabled)
                pictureBox12.Image = Properties.Resources.CMD_glow;
            else
                pictureBox12.Image = Properties.Resources.CMD;
            pictureBox12.Refresh();
        }

        public bool IsAutorunEnabledFlag()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
            int len = Convert.ToString(key.GetValue("AppTerminator")).Length;

            if (len > 0) return true;
            else return false;
        }

        void SetupAutorun(bool autorun)
        {
            if (autorun)
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
                key.SetValue("AppTerminator", keyboardHelper.GetSpecialSymbol() + Application.ExecutablePath + keyboardHelper.GetSpecialSymbol() + " -a");
                key.Close();
            }

            else
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
                key.DeleteValue("AppTerminator");
                key.Close();
            }
        }

        void SetupNotify(bool IsNotifyEnabled)
        {
            if (IsNotifyEnabled)
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
                key.SetValue("IsNotifyEnabled", true);
                key.Close();
            }

            else
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
                key.DeleteValue("IsNotifyEnabled");
                key.Close();
            }
        }

        public bool IsNotifyEnabledFlag()
        {
            bool notify = false;
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            notify = Convert.ToBoolean(key.GetValue("IsNotifyEnabled"));
            return notify;
        }

        void SetupCMD(bool isCMD)
        {
            if (isCMD)
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
                key.SetValue("IsCMD", true);
                key.Close();
            }

            else
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
                key.DeleteValue("IsCMD");
                key.Close();
            }
        }

        public bool IsCMDFlag()
        {
            bool CMD = false;
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            CMD = Convert.ToBoolean(key.GetValue("IsCMD"));
            return CMD;
        }

        public void SetupEventsFilePath()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            int len = Convert.ToString(key.GetValue("EventsFilePath")).Length;

            if (len == 0)
            {
                key.SetValue("EventsFilePath", WorkDirectory + "/Events.xml");
            }
        }

        public string GetEventsFilePath()
        {
            string path;
            var Subkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\KeyBoardHelper\\", true);
            path = Convert.ToString(Subkey.GetValue("EventsFilePath"));
            int len = path.Length;

            if (len > 0)
            {
                return path;
            }
            return "";
        }
        
        public void SaveFormWidth()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            key.SetValue("FormWidth", this.Width.ToString());
            key.Close();
        }

        public void SaveFormHeight()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            key.SetValue("FormHeight", this.Height.ToString());
            key.Close();
        }

        public int GetFormWidth()
        {
            int Width = 0;
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            Width = Convert.ToInt32(key.GetValue("FormWidth"));
            if (Width == 0)
                return 1100; //default Width
            return Width;
        }

        public int GetFormHeight()
        {
            int Height = 0;
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true).CreateSubKey("KeyBoardHelper");
            Height = Convert.ToInt32(key.GetValue("FormHeight"));
            if (Height == 0)
                return 446; //default Height
            return Height;
        }

        private void OnPowerModeChange(object s, PowerModeChangedEventArgs e)
        {
#if DEBUG
            log.Debug("OnPowerModeChange()");
#endif
            if (e.Mode == PowerModes.Resume)
            {
                KeyHook.Dispose();
                keyboardHelperNative = null;
                audioMgr = null;
                keyboardHelper = null;

                KeyHook = new GlobalHook();
                KeyHook.KeyDown += OnKeyDownEvent_Global;
                keyboardHelperNative = new KeyBoardHelperNative(this);
                audioMgr = new AudioManager();
                keyboardHelper = new KeyBoardHelper();
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //SaveAdditionalItems();
            SaveFormWidth();
            SaveFormHeight();
        }

        private void ShowTip(string msg, string title, Control obj)
        {
            toolTip1.SetToolTip(obj, msg);
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.ToolTipTitle = title;            
            toolTip1.IsBalloon = true;
        }

        private void OnKeyDownEvent_Global(object s, KeyEventArgs ev)
        {
#if DEBUG
            log.Debug("OnKeyDownEvent_Global()");
#endif
            //Commands        
            if (isCmdEnabled && !isKeyInputFormShow && !isCmdFormShow)
            {
                if (ev.KeyCode == Keys.Space)
                {
                    phrase = phrase + " ";
                }

                if (char.TryParse(ev.KeyCode.ToString(), out word))
                    phrase = (phrase + word.ToString()).ToLower();

                if (phrase.Length > 20)
                    phrase = "";

                //Command enter debug
                //notifiIcon1.BalloonTipText = phrase;
                //notifiIcon1.ShowBalloonTip(500);

                foreach (Item_t it in EventsCollection)
                {
                    if (phrase.Contains(it.Command + " "))
                    {
                        RunEvent(it.Event, it.Options);
                        CommandEvent = it.Event;
                        CommandEventOptions = it.Options;
                        CommandEventCommand = it.Command;
                    }
                }

                if (CommandEvent.Length > 0)
                {
                    if (IsNotifyEnabled)
                    {
                        if (CommandEventOptions.Length > 0 && CommandEventOptions != "None")
                        {
                            notifiIcon1.BalloonTipText = "Команда: " + CommandEventCommand + "\n" + CommandEvent + ": " + CommandEventOptions;
                            notifiIcon1.ShowBalloonTip(500);
                        }
                        else
                        {
                            notifiIcon1.BalloonTipText = "Команда: " + CommandEventCommand + "\n" + CommandEvent;
                            notifiIcon1.ShowBalloonTip(500);
                        }

                        phrase = "";
                        CommandEvent = "";
                        CommandEventOptions = "";
                        CommandEventCommand = "";
                    }
                }
            }

            if (isKeyInputFormShow == false)
            {
                foreach (Item_t it in EventsCollection)
                {
                    if (ev.KeyCode == (Keys)Enum.Parse(typeof(Keys), it.Key) && Control.ModifierKeys == (Keys)Enum.Parse(typeof(Keys), it.ModifierKey))
                    {
                        RunEvent(it.Event, it.Options);
                    }
                }
            }

            if (KeyInputForm != null && ev.KeyCode != Keys.Enter)
            {
                if (KeyInputForm.Created)
                {
                    if (Control.ModifierKeys == Keys.None)
                    {
                        Bind_Key = ev.KeyCode;
                        Bind_ModiffiersKey = Keys.None;
                        KeyInputForm.SetTextBoxText(Convert.ToString(ev.KeyCode));
                    }
                    else
                    {
                        Bind_Key = ev.KeyCode;
                        Bind_ModiffiersKey = Control.ModifierKeys;
                        KeyInputForm.SetTextBoxText(Convert.ToString((Keys)Control.ModifierKeys) + " + " + Convert.ToString((Keys)ev.KeyCode));
                    }
                }
            }

            if (ev.KeyCode == Keys.Delete && this.Visible && !isKeyInputFormShow && !isCmdFormShow)
            {
                DeleteItem();
            }
        }
         
        void EventsCollectionInit()
        {
            DataTable table = new DataTable();

            table.Columns.Add("Иконка", typeof(Image));
            table.Columns.Add("Событие", typeof(string));
            table.Columns.Add("Копка", typeof(string));
            table.Columns.Add("Модификатор", typeof(string));
            table.Columns.Add("Параметры", typeof(string));

            table.Rows.Add(imageList1.Images[0], "Открыть папку", "null", "null", "null");
            table.Rows.Add(imageList1.Images[4], "Открыть файл", "null", "null", "null");
            table.Rows.Add(imageList1.Images[3], "Открыть страницу", "null", "null", "null");
            table.Rows.Add(imageList1.Images[5], "Завершить процесс", "null", "null", "null");
            table.Rows.Add(imageList1.Images[6], "Свернуть окна", "null", "null", "null");
            table.Rows.Add(imageList1.Images[1], "Открыть панель управления", "null", "null", "null");
            table.Rows.Add(imageList1.Images[2], "Открыть удаление программ", "null", "null", "null");                                   
            table.Rows.Add(imageList1.Images[7], "Открыть дисковод", "null", "null", "null");            
            table.Rows.Add(imageList1.Images[10], "Открыть командную строку", "null", "null", "null");            
            table.Rows.Add(imageList1.Images[9], "Добавить громокость", "null", "null", "null");
            table.Rows.Add(imageList1.Images[9], "Уменьшить громокость", "null", "null", "null");
            table.Rows.Add(imageList1.Images[8], "Отключить звук", "null", "null", "null");
            table.Rows.Add(imageList1.Images[11], "Завершение работы", "null", "null", "null");
            table.Rows.Add(imageList1.Images[12], "Перезагрузка", "null", "null", "null");
            table.Rows.Add(imageList1.Images[13], "Сон", "null", "null", "null");
            table.Rows.Add(imageList1.Images[14], "Редактор реэстра", "null", "null", "null");
            table.Rows.Add(imageList1.Images[15], "Выполнить", "null", "null", "null");

            dataGridView1.DataSource = table;

            dataGridView1.RowHeadersVisible = false;
            dataGridView1.Rows[0].Cells[0].Selected = false;
            dataGridView1.CurrentCell.Selected = false;
            dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomCenter;
            dataGridView1.GridColor = Color.FromArgb(255, 58, 58, 59);
            dataGridView1.BackgroundColor = Color.FromArgb(255, 58, 58, 59);
            dataGridView1.ScrollBars = ScrollBars.None;            

            DataGridViewColumn column0 = dataGridView1.Columns[0];
            column0.Width = 50;
            DataGridViewColumn column1 = dataGridView1.Columns[1];
            column1.Width = 175;
            DataGridViewColumn column2 = dataGridView1.Columns[2];
            column2.Visible = false;
            DataGridViewColumn column3 = dataGridView1.Columns[3];
            column3.Visible = false;
            DataGridViewColumn column4 = dataGridView1.Columns[4];
            column4.Visible = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Height = 35;
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 58, 58, 59);
            }

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 48, 48, 49);
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView1.EnableHeadersVisualStyles = false;

            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.AllowUserToResizeRows = false;

            dataGridView1.MultiSelect = false;
        }

        void AdditionalEventsFieldsInit()
        {
            BindEventsTable.Columns.Add("Иконка", typeof(Image));
            BindEventsTable.Columns.Add("Событие", typeof(string));
            BindEventsTable.Columns.Add("Сочетание клавишь", typeof(string));
            BindEventsTable.Columns.Add("Команда", typeof(string));
            BindEventsTable.Columns.Add("Параметры", typeof(string));            

            dataGridView2.DataSource = BindEventsTable;

            BindEventsTable.Rows.Add(imageList1.Images[0], "Открыть папку", "null", "null");

            dataGridView2.RowHeadersVisible = false;
            dataGridView2.Rows[0].Cells[0].Selected = false;
            dataGridView2.CurrentCell.Selected = false;
            dataGridView2.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView2.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.BottomCenter;
            dataGridView2.GridColor = Color.FromArgb(255, 58, 58, 59);
            dataGridView2.BackgroundColor = Color.FromArgb(255, 58, 58, 59);
            dataGridView2.ScrollBars = ScrollBars.None;

            DataGridViewColumn column0 = dataGridView2.Columns[0];
            column0.Width = 50;
            DataGridViewColumn column1 = dataGridView2.Columns[1];
            column1.Width = 160;
            DataGridViewColumn column2 = dataGridView2.Columns[2];
            column2.Width = 115;
            column2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewColumn column3 = dataGridView2.Columns[3];
            column3.Width = 90;
            column3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewColumn column4 = dataGridView2.Columns[4];
            column4.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; //if resize
            //column4.Width = 440;

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                row.Height = 35;
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 58, 58, 59);
            }

            foreach (DataGridViewColumn col in dataGridView2.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dataGridView2.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView2.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 48, 48, 49);
            dataGridView2.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView2.EnableHeadersVisualStyles = false;

            dataGridView2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView2.AllowUserToResizeRows = false;
            dataGridView2.MultiSelect = false;    
            
            dataGridView2.Rows.Remove(dataGridView2.Rows[0]);            
        }

        public Form1()
        {
#if DEBUG
            log.Debug("Form1::Form1()");
#endif
            InitializeComponent();
            FileInfo info = new FileInfo(Application.ExecutablePath);
            label3.Text = "Дата: " + info.LastWriteTime.ToString();
            string version = Application.ProductVersion;
            label4.Text = "Версия: " + "2.0.0.0";
            this.Width = GetFormWidth();
            this.Height = GetFormHeight();
            //Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 5, 5));
            SystemEvents.PowerModeChanged += this.OnPowerModeChange;            
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            KeyHook = new GlobalHook();
            keyboardHelperNative = new KeyBoardHelperNative(this);
            audioMgr = new AudioManager();            
            keyboardHelper = new KeyBoardHelper();
            KeyHook.KeyDown += OnKeyDownEvent_Global;
            SetupEventsFilePath();         
        }

        private const int WM_NCHITTEST = 0x84;
        private const int HT_CLIENT = 0x1;
        private const int HT_CAPTION = 0x2;

        /*protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
            m.Result = (IntPtr)(HT_CAPTION);
        }*/

        //Form resize by mouse
        protected override void WndProc(ref Message m)
        {
            const int wmNcHitTest = 0x84;
            const int htBottomLeft = 16;
            const int htBottomRight = 17;
            if (m.Msg == wmNcHitTest)
            {
                int x = (int)(m.LParam.ToInt64() & 0xFFFF);
                int y = (int)((m.LParam.ToInt64() & 0xFFFF0000) >> 16);
                Point pt = PointToClient(new Point(x, y));
                Size clientSize = ClientSize;
                if (clientSize.Width < 920 || clientSize.Height < 343)
                {
                    this.Width = 920;
                    this.Height = 343;
                }
                if (pt.X >= clientSize.Width - 16 && pt.Y >= clientSize.Height - 16 && clientSize.Height >= 16)
                {
                    m.Result = (IntPtr)(IsMirrored ? htBottomLeft : htBottomRight);
                    return;
                }
            }                        
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)(HT_CAPTION);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.MouseWheel += new MouseEventHandler(DataGridView1_MouseWheel);
            dataGridView2.MouseWheel += new MouseEventHandler(DataGridView2_MouseWheel);
            
            EventsCollectionInit();            
            AdditionalEventsFieldsInit();
            LoadAdditionalItems();

            IsAutorunEnabled = IsAutorunEnabledFlag();            
            SetupAutorunImage();
            IsNotifyEnabled = IsNotifyEnabledFlag();
            SetupNotifyImage();
            isCmdEnabled = IsCMDFlag();
            SetupCMDImage();

            this.Opacity = 0.0;
            timer_opacity.Enabled = true;

#if DEBUG
            log.Debug("Form1::Form1_Load()");
#endif
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.Visible == false && e.Button == MouseButtons.Left)
            {               
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }

            else if (e.Button == MouseButtons.Left)
            {
                this.Hide();
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void развернутьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.Visible == false)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void свернутьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.Visible == true)
            {
                this.Show();
                this.WindowState = FormWindowState.Minimized;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;            
            this.Hide();
            this.WindowState = FormWindowState.Minimized;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            //keyboardHelper.SaveSettingsFile();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)           
                contextMenuStrip1.Show(MousePosition);
        }

        private void tabPage1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                contextMenuStrip1.Show(MousePosition);
        }

        private void tabPage2_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                contextMenuStrip1.Show(MousePosition);
        }

        void OpenUrl(string url)
        {
            Process.Start(url);
        }

        void OpenAppUninstaller()
        {
            Process.Start("appwiz.cpl");
        }

        void OpenControlPanel()
        {
            Process.Start("control");
        }

        void OpenFile(string file)
        {
            if(System.IO.File.Exists(file))
                Process.Start(file);
        }

        void OpenFolder(string folder) {
            if(System.IO.Directory.Exists(folder))
                Process.Start(folder);
        }

        string SelectFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }

            return "null";
        }

        string SelectFolder()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult SelectFolderResult = dialog.ShowDialog();
            if (SelectFolderResult == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            else
            {
                if (SelectFolderResult == DialogResult.Cancel)
                {
                    dialog.Dispose();
                    DialogResult MBresult;
                    MBresult = MessageBox.Show("Вы отменили выбор папки.\nЗадать путь папки с буффера обмена?", " KeyBoaedHelper - Выбор папки", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (MBresult == DialogResult.Yes)
                    {
                        return Clipboard.GetText();
                    }
                }
            }
                return "null";
        }

        void PowerOff()
        {
            Process.Start("shutdown", "/s /t 0");
        }

        void Reboot()
        {
            Process.Start("shutdown", "/r /t 0");
        }

        void Hibernate()
        {
            bool isHibernate = Application.SetSuspendState(PowerState.Hibernate, false, false);
            if (isHibernate == false)
                MessageBox.Show("Ошибка перевода системы в режим Сон!", "KeyBoardHelper", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void RegEditor()
        {
            Process.Start("regedit");
        }

        void RunCommand()
        {

        }

        void SaveAdditionalItems()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Item_t>));
            using (StreamWriter sw = new StreamWriter(GetEventsFilePath()))
                serializer.Serialize(sw, EventsCollection);
        }

        void LoadAdditionalItems()
        {
            EventsCollection.Clear();

            if (File.Exists(GetEventsFilePath()))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Item_t>));
                using (FileStream fs = new FileStream(GetEventsFilePath(), FileMode.Open))
                    EventsCollection = (List<Item_t>)serializer.Deserialize(fs);
            }

            AddItemToListView(true);
        }

        void AddItemToListView(bool fromFile)
        {
            //Добавление элемента в listView2           
            int icon = 0;
            string ev = "";
            string key = "";
            string command = "None";
            string options = "None";

            if (!fromFile)
            {
                foreach (Item_t it in EventsCollection)
                {
                    if (it.Event == focus_row.Cells["Событие"].Value.ToString())
                    {
                        if (it.ModifierKey == Keys.None.ToString())
                            key = it.Key.ToString();
                        else
                            key = it.ModifierKey.ToString() + " + " + it.Key.ToString();
                        if (it.Options.Length > 0)
                            options = it.Options;                        
                        if (it.Command.Length > 0)
                            command = it.Command;
                        ev = it.Event;
                        icon = GetImageIndex(focus_row.Cells["Событие"].Value.ToString());                        
                    }
                }
                BindEventsTable.Rows.Add(imageList1.Images[icon], ev, key, command, options);                
            }

            if (fromFile){
                foreach (Item_t it in EventsCollection){
                    if (it.ModifierKey == Keys.None.ToString())
                        key = it.Key.ToString();
                    else
                        key = it.ModifierKey.ToString() + " + " + it.Key.ToString();                    
                    if (it.Options.Length > 0)
                        options = it.Options;
                    if (it.Command.Length > 0)
                        command = it.Command;
                    ev = it.Event;
                    icon = GetImageIndex(it.Event);                   
                    BindEventsTable.Rows.Add(imageList1.Images[icon], ev, key, command, options);
                }
            }

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                row.Height = 35;
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 58, 58, 59);
            }

            dataGridView2.ClearSelection();

            SaveAdditionalItems();
        }

        public void AddItemToList()
        {
           if (!CheackHotKeys(Bind_Key.ToString(), Bind_ModiffiersKey.ToString()))
                return;           

            //Добавление элемента в список
            Item_t ItemToList;
            ItemToList.Event = focus_row.Cells["Событие"].Value.ToString();
            ItemToList.Key = Bind_Key.ToString();
            ItemToList.ModifierKey = Bind_ModiffiersKey.ToString();
            ItemToList.Command = "None";
            ItemToList.Options = "None";

            switch (focus_row.Cells["Событие"].Value.ToString())
            {
                case "Открыть папку":
                    ItemToList.Options = SelectFolder();
                    break;
                case "Открыть файл":
                    ItemToList.Options = SelectFile();
                    break;
                case "Открыть страницу":
                    MessageBox.Show("Скопируйте текст в буффер обмена, после чего нажимте ОК.", "KeyBoard Helper - вставка ссылки", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    ItemToList.Options = Clipboard.GetText();
                    break;

                default:
                    break;
            }

            ItemToList.ImageIndex = GetImageIndex(focus_row.Cells["Событие"].Value.ToString());
            if(Command.Length > 0)
                ItemToList.Command = Command;

            if (!CheackCommand(ItemToList.Command))
                return;

            foreach(Item_t it in EventsCollection)
            {
                if (ItemToList.Options.Length > 0)
                {
                    if (it.Options != "None" && it.Options == ItemToList.Options)
                    {
                        MessageBox.Show("Событие " + "'" + ItemToList.Event + "'" + " с такими параметрами уже назначено!", "KeyBoaedHelper - добавление события", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            EventsCollection.Add(ItemToList);

            AddItemToListView(false);
        }

        int GetImageIndex(string ev)
        {
            int imgIndex = 0;
            switch (ev)
            {
                case "Открыть папку":
                    imgIndex = 0;
                    break;
                case "Открыть файл":
                    imgIndex = 4;
                    break;
                case "Открыть страницу":
                    imgIndex = 3;
                    break;
                case "Завершить процесс":
                    imgIndex = 5;
                    break;
                case "Свернуть окна":
                    imgIndex = 6;
                    break;
                case "Открыть панель управления":
                    imgIndex = 1;
                    break;
                case "Открыть удаление программ":
                    imgIndex = 2;
                    break;
                case "Открыть дисковод":
                    imgIndex = 7;
                    break;
                case "Открыть командную строку":
                    imgIndex = 10;
                    break;
                case "Добавить громокость":
                    imgIndex = 9;
                    break;
                case "Уменьшить громокость":
                    imgIndex = 9;
                    break;
                case "Отключить звук":
                    imgIndex = 8;
                    break;
                case "Завершение работы":
                    imgIndex = 11;
                    break;
                case "Перезагрузка":
                    imgIndex = 12;
                    break;
                case "Сон":
                    imgIndex = 13;
                    break;
                case "Редактор реэстра":
                    imgIndex = 14;
                    break;
                case "Выполнить":
                    imgIndex = 15;
                    break;
                default:
                    imgIndex = 0;
                    break;
            }
            return imgIndex;
        }

        /*listView1_DoubleClick()
             foreach (ListViewItem it in listView2.Items){
                if (listView1.FocusedItem.Text == it.Text
                && it.Text != "Открыть папку"
                && it.Text != "Открыть файл"
                && it.Text != "Открыть страницу")
                return;
            }

    focus_item = listView1.FocusedItem;

            KeyInputForm = new KeyInput(this);
    KeyInputForm.Show();*/




        private void listView2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
        }

        void DataGridView1_MouseWheel(object sender, MouseEventArgs e)
        {
            int currentIndex = this.dataGridView1.FirstDisplayedScrollingRowIndex;
            int scrollLines = SystemInformation.MouseWheelScrollLines;

            if (e.Delta > 0)
            {
                this.dataGridView1.FirstDisplayedScrollingRowIndex
                    = Math.Max(0, currentIndex - scrollLines);
            }
            else if (e.Delta < 0)
            {
                this.dataGridView1.FirstDisplayedScrollingRowIndex
                    = currentIndex + scrollLines;
            }
        }

        void DataGridView2_MouseWheel(object sender, MouseEventArgs e)
        {
            int currentIndex = this.dataGridView2.FirstDisplayedScrollingRowIndex;
            int scrollLines = SystemInformation.MouseWheelScrollLines;
            if (dataGridView2.Rows.Count > 7)
            {
                if (e.Delta > 0)
                {
                    this.dataGridView2.FirstDisplayedScrollingRowIndex
                        = Math.Max(0, currentIndex - scrollLines);
                }
                else if (e.Delta < 0)
                {
                    this.dataGridView2.FirstDisplayedScrollingRowIndex
                        = currentIndex + scrollLines;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.ClearSelection();
        }

        private void pictureBox3_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (IsAutorunEnabled)
                    pictureBox3.Image = Properties.Resources.autorun;
                else
                    pictureBox3.Image = Properties.Resources.autorun_glow;
                pictureBox3.Refresh();

                IsAutorunEnabled = !IsAutorunEnabled;
                SetupAutorun(IsAutorunEnabled);
            }
        }

        private void pictureBox3_MouseHover(object sender, EventArgs e)
        {
            ShowTip("Добавить программу в автозагрузку?", "Автозагрузка",pictureBox3);
        }

        private void pictureBox4_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (IsNotifyEnabled)
                    pictureBox4.Image = Properties.Resources.notify;
                else
                    pictureBox4.Image = Properties.Resources.notify_glow;
                pictureBox4.Refresh();

                IsNotifyEnabled = !IsNotifyEnabled;
                SetupNotify(IsNotifyEnabled);
            }
        }

        private void pictureBox5_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox5.Image = Properties.Resources.about_glow;
        }

        private void pictureBox5_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox5.Image = Properties.Resources.about;
        }

        private void pictureBox4_MouseHover(object sender, EventArgs e)
        {
            ShowTip("Активировать уведомления?", "Уведомления", pictureBox4);
        }

        private void pictureBox7_MouseClick_1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                System.Diagnostics.Process.Start("https://t.me/Senny970");
        }

        private void pictureBox8_MouseClick_1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                System.Diagnostics.Process.Start("https://vk.com/senny970");
        }

        private void pictureBox9_MouseClick_1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                System.Diagnostics.Process.Start("https://www.facebook.com/senny970");
        }

        private void pictureBox11_MouseClick_1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                timer1.Enabled = true;
        }

        private void pictureBox10_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                System.Diagnostics.Process.Start("https://steamcommunity.com/id/--Senny--");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!AboutVisible)
            {
                if (PaneDataGrid1_Pos_X <= 1098)
                {
                    PaneDataGrid1_Pos_X = panel2.Location.X + 10;
                    panel2.Location = new Point(PaneDataGrid1_Pos_X, panel2.Location.Y);
                }
                else
                {
                    timer1.Enabled = false;
                    AboutVisible = true;
                }
            }

            if (AboutVisible)
            {
                if (PaneDataGrid1_Pos_X > 870)
                {
                    PaneDataGrid1_Pos_X = panel2.Location.X - 10;
                    panel2.Location = new Point(PaneDataGrid1_Pos_X, panel2.Location.Y);
                }
                else
                {
                    timer1.Enabled = false;
                    AboutVisible = false;                    
                }
            }
        }

        private void pictureBox5_MouseClick(object sender, MouseEventArgs e)
        {
            timer1.Enabled = true;
        }

        private void dataGridView1_SelectionChanged_1(object sender, EventArgs e)
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.ClearSelection();
        }

        private void dataGridView1_CellDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            foreach (Item_t it in EventsCollection)
            {
                if (dataGridView1.CurrentRow.Cells["Событие"].Value.ToString() == it.Event
                    && it.Event.ToString() != "Открыть папку"
                    && it.Event.ToString() != "Открыть файл"
                    && it.Event.ToString() != "Открыть страницу")
                {
                    MessageBox.Show("Событие " + "'" + dataGridView1.CurrentRow.Cells["Событие"].Value.ToString() + "'" + " уже назначено!", "KeyBoaedHelper - добавление события", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            focus_row = dataGridView1.CurrentRow;

            KeyInputForm = new KeyInput(this);
            KeyInputForm.Show();
        }

        public bool CheackHotKeys(string MainKey, string Modifier)
        {
            foreach (Item_t it in EventsCollection)
            {
                if (it.Key == MainKey && Modifier == Keys.None.ToString() && it.Key != Keys.None.ToString())
                {
                    MessageBox.Show("Клавиша " + MainKey.ToString() + " уже назначена на - " + "'" + it.Event + "'" + " !", "KeyBoaedHelper - проверка сочетания клавишь", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (it.Key == MainKey && it.ModifierKey == Modifier && it.Key != Keys.None.ToString())
                {
                    MessageBox.Show("Сочетание клавишь " + Modifier.ToString() + " + " + MainKey.ToString() + " уже назначено на - " + "'" + it.Event + "'" + " !", "KeyBoaedHelper - проверка сочетания клавишь", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        public bool CheackCommand(string command)
        {
            foreach (Item_t it in EventsCollection)
            {
                if (it.Command!= "None" && it.Command == command)
                {
                    MessageBox.Show("Команда " + command + " уже назначена на - " + "'" + it.Event + "'" + " !", "KeyBoaedHelper - проверка команды", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
                return true;
        }

        public void EditItem()
        {
            EditedItem = new Item_t();
            EditedItem = EventsCollection[ItemIndexToEdit];
            EditedItem.Key = Bind_Key.ToString();
            EditedItem.ModifierKey = Bind_ModiffiersKey.ToString();
            EditedItem.Command = Command;
            EventsCollection[ItemIndexToEdit] = EditedItem;

            if (EditedItem.ModifierKey == Keys.None.ToString())
                focus_row_edit.Cells["Сочетание клавишь"].Value = EditedItem.Key.ToString();
            else
                focus_row_edit.Cells["Сочетание клавишь"].Value = EditedItem.ModifierKey.ToString() + " + " + EditedItem.Key.ToString();

            focus_row_edit.Cells["Команда"].Value = EditedItem.Command.ToString();

            SaveAdditionalItems();
        }

        void DeleteItem()
        {
            DialogResult MBresult = DialogResult.No;
            foreach (Item_t it in EventsCollection)
            {
                if (it.Event == dataGridView2.CurrentRow.Cells["Событие"].Value.ToString()
                    && it.Options == dataGridView2.CurrentRow.Cells["Параметры"].Value.ToString()
                    && it.Command == dataGridView2.CurrentRow.Cells["Команда"].Value.ToString())
                {
                    MBresult = MessageBox.Show("Удалить запись " + "'" + it.Event + "' ?", " KeyBoaedHelper - удаление записи", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (MBresult == DialogResult.Yes)
                    {
                        EventsCollection.Remove(it);
                        break;
                    }
                }
            }

            if (MBresult == DialogResult.Yes)
            {
                dataGridView2.Rows.Remove(dataGridView2.CurrentRow);
                SaveAdditionalItems();
            }
        }

        void RunEvent(string ev, string options){
            switch (ev)
            {
                case "Открыть папку":
                    OpenFolder(options);
                    break;
                case "Открыть панель управления":
                    OpenControlPanel();
                    break;
                case "Удаление программ":
                    OpenAppUninstaller();
                    break;
                case "Открыть файл":
                    OpenFile(options);
                    break;
                case "Открыть страницу":
                    OpenUrl(options);
                    break;
                case "Свернуть окна":
                    keyboardHelperNative.MinimizeOpenWindows();
                    break;
                case "Открыть удаление программ":
                    OpenAppUninstaller();
                    break;
                case "Завершить процесс":
                    keyboardHelperNative.TerminateActiveWindowProcess(true);
                    break;
                case "Открыть командную строку":
                    keyboardHelperNative.StartCMD();
                    break;
                case "Отключить звук":
                    keyboardHelperNative.MuteSound();
                    break;
                case "Добавить громокость":
                    keyboardHelperNative.AddMasterVolume(5);
                    break;
                case "Уменьшить громокость":
                    keyboardHelperNative.ReduceMasterVolume(5);
                    break;
                case "Открыть дисковод":
                    keyboardHelperNative.CD_DriveOpen();
                    break;
                case "Завершение работы":
                    PowerOff();
                    break;
                case "Перезагрузка":
                    Reboot();
                    break;
                case "Сон":
                    Hibernate();
                    break;
                case "Редактор реэстра":
                    RegEditor();
                    break;
                case "Выполнить":
                    RunCommand();
                    break;
                default:
                    break;
            }
        }

        //EditMode
        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            foreach (Item_t it in EventsCollection)
            {
                if (dataGridView2.CurrentRow.Cells["Событие"].Value.ToString() == it.Event
                    && dataGridView2.CurrentRow.Cells["Параметры"].Value.ToString() == it.Options)
                {                    
                    ItemIndexToEdit = EventsCollection.IndexOf(it);
                    focus_row_edit = dataGridView2.CurrentRow;

                    isEditKeysMode = true;
                    KeyInputForm = new KeyInput(this);
                    KeyInputForm.Show();
                }
            }
        }

        private void pictureBox12_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isCmdEnabled)
                    pictureBox12.Image = Properties.Resources.CMD;
                else
                    pictureBox12.Image = Properties.Resources.CMD_glow;
                pictureBox12.Refresh();

                isCmdEnabled = !isCmdEnabled;
                SetupCMD(isCmdEnabled);
            }
        }

        private void pictureBox12_MouseHover(object sender, EventArgs e)
        {
            ShowTip("Активировать ввод команд?", "Команды", pictureBox12);
        }

        private void pictureBox5_MouseHover(object sender, EventArgs e)
        {
            ShowTip("Просмотреть сведенья о программе?", "О программе", pictureBox5);
        }

        private void timer_opacity_Tick(object sender, EventArgs e)
        {
            //Show form
            if(this.Visible && this.Opacity <= 1)
            {
                this.Opacity = this.Opacity + 0.05;
            }

            if (this.Opacity == 1.0f)
            { 
                timer_opacity.Enabled = false;
            }
        }

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Opacity = 0.0;
                timer_opacity.Enabled = true;
            }
        }
    }
}