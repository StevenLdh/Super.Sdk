using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using CC.Public;
using CC.Framework.DAL;
using CC.DataAccess.Extend;

namespace Threading.Timer
{
    public partial class FrmMain : Form
    {
        //定义全局变量
        public int currentCount = 0;
        //定义Timer类
        System.Threading.Timer threadTimer;
        //定义委托
        public delegate void SetControlValue(object value);

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            InitTimer();
        }

        /// <summary>
        /// 初始化Timer类
        /// </summary>
        private void InitTimer()
        {
            threadTimer = new System.Threading.Timer(new TimerCallback(TimerUp), null, Timeout.Infinite, 1000);
        }

        /// <summary>
        /// 定时到点执行的事件
        /// </summary>
        /// <param name="value"></param>
        private void TimerUp(object value)
        {
            currentCount += 1;
            ViewData();
            this.Invoke(new SetControlValue(SetTextBoxValue), currentCount);
        }

        /// <summary>
        /// 给文本框赋值
        /// </summary>
        /// <param name="value"></param>
        private void SetTextBoxValue(object value)
        {
            this.txt_Count.Text = value.ToString();
        }

        /// <summary>
        /// 开始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Start_Click(object sender, EventArgs e)
        {
            //立即开始计时，时间间隔1000毫秒
            threadTimer.Change(0, 3000);
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Stop_Click(object sender, EventArgs e)
        {
            //停止计时
            threadTimer.Change(Timeout.Infinite, 3000);
        }
        public static void ViewData()
        {
            HashObjectList datasourcecombase =
                new DALBase("erp").GetDataList(HashObject.CreateWith("profileid", 10004621), "select * from bas_account where profileid=10004621;", SqlType.CmdText);
            //Console.WriteLine(datasourcecombase.Count());
        }
    }
}
