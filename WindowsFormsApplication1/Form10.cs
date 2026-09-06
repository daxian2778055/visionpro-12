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
    public partial class Form10 : Form
    {
        CogToolBlock block3;
        public Form10(CogToolBlock block_33)
        {
            block3 = block_33;
            InitializeComponent();
            cogToolBlockEdit1.Subject = block3;
        }

        private void Form10_Load(object sender, EventArgs e)
        {

        }

        private void Form10_FormClosing(object sender, FormClosingEventArgs e)
        {
            cogToolBlockEdit1.Subject = null;
        }
    }
}
