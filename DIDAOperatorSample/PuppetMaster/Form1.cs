using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class Form1 : Form
    {
        PuppetMasterLogic puppetMaster;
        public Form1()
        {
            InitializeComponent();
            puppetMaster = new PuppetMasterLogic(this);
        }

        private void ButtonBrowseConfigScript_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog
            {
                Title = "C# Corner Open File Dialog",
                InitialDirectory = @"c:\",
                Filter = "All files (*.*)|*.*|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textFileScript.Text = fdlg.FileName;
                //Missing -> process the script and then send to the correspondent PCS's
                foreach (string line in System.IO.File.ReadLines(@fdlg.FileName))
                {
                    if (line == "")
                        continue;
                    string[] buffer = line.Split(' ');
                    puppetMaster.CreateNewConfigEvent(buffer);
                }
            }
        }

        private void ButtonBrowseAppData_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog
            {
                Title = "C# Corner Open File Dialog",
                InitialDirectory = @"c:\",
                Filter = "All files (*.*)|*.*|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textAppDataFile.Text = fdlg.FileName;
            }

        }

        private void ButtonSendAppData_Click(object sender, EventArgs e)
        {
            puppetMaster.SendAppDataToScheduler(
                textAppDataFile.Text,
                textAppInput.Text);
        }

        private void TextAppInput_Click(object sender, EventArgs e)
        {
            textAppInput.Text = null;
        }

        private void ButtonCreateConnectionWithScheduler_Click(object sender, EventArgs e)
        {
            puppetMaster.CreateChannelWithScheduler(this, "localhost", 4001, "localhost");
        }

        private void ButtonDebugCreateScheduler_Click(object sender, EventArgs e)
        {
            //puppetMaster.SendCreateProccessInstanceRequest("sched1", "http://localhost:2000");
        }
    }
}
