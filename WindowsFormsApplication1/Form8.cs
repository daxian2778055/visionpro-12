using Cognex.VisionPro.ToolBlock;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form8 : Form
    {
        ErrorLog MsgErroeLog = new ErrorLog();
        CogToolBlock block3;
        public Form8(CogToolBlock block_33)
        {
            block3 = block_33;
            InitializeComponent();
            cogToolBlockEdit1.Subject = block3;
            //绑定ToolBlock事件
            //cogToolBlockEdit1.Subject.Ran += new EventHandler(GetResult1_VisionPro);
        }
        //cogToolBlockEdit1绑定事件
        public void GetResult1_VisionPro(object sender, EventArgs e)
        {
            try
            {
                //获取ToolBlock中输出参数内容
                int OutPutElementsCount = cogToolBlockEdit1.Subject.Outputs.Count;
                string[] OutPutElements = cogToolBlockEdit1.Subject.Outputs.GetFormattedTerminalStrings();
                for (int i = 0; i < cogToolBlockEdit1.Subject.Outputs.Count; i++)
                {

                    int StartPosition = OutPutElements[i].IndexOf('|');
                    int EndPosition = OutPutElements[i].LastIndexOf('|');
                    string OutPutElementsName = OutPutElements[i].Substring(StartPosition + 1, EndPosition - StartPosition - 1);
                    string OutPutElementsValue = (string)cogToolBlockEdit1.Subject.Outputs[OutPutElementsName].Value;
                    this.Result_label.Text = OutPutElementsName.ToString() + ":" + OutPutElementsValue + "\n";

                }
            }
            catch(Exception ex)
            {
                MsgErroeLog.WriteLog(ex.Message + "_窗口2");
            };

        }

        private void Form8_Load(object sender, EventArgs e)
        {

        }

        private void Form8_FormClosing(object sender, FormClosingEventArgs e)
        {
            cogToolBlockEdit1.Subject = null;
        }
    }
}
