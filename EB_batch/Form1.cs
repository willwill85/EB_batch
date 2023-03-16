using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace EB_batch
{
    public partial class Form1 : Form
    {
        #region WIN API---引入系统API,用于把程序打开的窗口整合到SHOW页面中
        #region  宏定义
        private const int SWP_NOOWNERZORDER = 0x200;
        private const int SWP_NOREDRAW = 0x8;
        private const int SWP_NOZORDER = 0x4;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int WS_EX_MDICHILD = 0x40;
        private const int SWP_FRAMECHANGED = 0x20;
        private const int SWP_NOACTIVATE = 0x10;
        private const int SWP_ASYNCWINDOWPOS = 0x4000;
        private const int SWP_NOMOVE = 0x2;
        private const int SWP_NOSIZE = 0x1;
        private const int GWL_STYLE = (-16);
        private const int WS_VISIBLE = 0x10000000;
        private const int WM_CLOSE = 0x10;
        private const int WS_CHILD = 0x40000000;
        #endregion
        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true,
             CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        private static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
        private static extern long GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
        private static extern long SetWindowLong(IntPtr hwnd, int nIndex, long dwNewLong);
        //private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetWindowPos(IntPtr hwnd, long hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hwnd, uint Msg, long wParam, long lParam);
        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        //移动鼠标
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        #endregion

        Process process = null;
        IntPtr appWin;
        private string exeName = "";
        string dir;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            
        }
        private void KillProcess(string processName)
        {
            System.Diagnostics.Process myproc = new System.Diagnostics.Process();
            foreach (Process thisproc in Process.GetProcessesByName(processName))
            {
                if (!thisproc.CloseMainWindow())
                {
                    thisproc.Kill();
                    GC.Collect();
                }
                Process[] prcs = Process.GetProcesses();
                foreach (Process p in prcs)
                {
                    if (p.ProcessName.Equals(processName))
                    {
                        p.Kill();
                    }
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //杀掉指定进程
            KillProcess("Ebsynth");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
        public void get_cpu(string ebs)
        {

        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }
        public void clear_status()
        {
            listBox1.Items.Clear();
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel1.Text = "正在处理的文件：            ";
            toolStripStatusLabel2.Text = "CPU占用：0";

        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

            string filename = openFileDialog1.SafeFileName;
            string title = filename.Split('_')[0];
            dir = openFileDialog1.FileName.Replace(filename, "");
            toolStripStatusLabel1.Text = dir;
            try
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    if (file.Contains(title))
                        listBox1.Items.Add(file.Replace(dir,""));
                }
            }
            catch
            {
                MessageBox.Show("ebs文件格式不对");
            }
            //listBox1.Items.Add(filename);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            exeName = textBox1.Text;
            backgroundWorker1.RunWorkerAsync();
        }
        public void workload(string ebsfilename)
        {
            try
            {
                // Start the process 
                process = System.Diagnostics.Process.Start(this.exeName,ebsfilename);
                // Wait for process to be created and enter idle condition 
                process.WaitForInputIdle();
                appWin = process.MainWindowHandle;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error");
            }
            SetParent(appWin, this.panel1.Handle);
            // Remove border and whatnot
            //SetWindowLong(appWin, GWL_STYLE, WS_VISIBLE);
            // Move the window to overlay it on this window
            MoveWindow(appWin, 0, 0, panel1.Width, panel1.Height, true);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillProcess("Ebsynth");
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            workload(dir + listBox1.Items[0].ToString());
        }
        //执行点击事件
        private void MouseClick(int x,int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);      
        }
        
        private void button3_Click(object sender, EventArgs e)
        {
            int offsetx = this.Location.X;
            int offsety = this.Location.Y;

            MouseClick(int.Parse(textBox2.Text)+offsetx, int.Parse(textBox3.Text)+offsety);
        }
    }
}
