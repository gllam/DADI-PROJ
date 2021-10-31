using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class Form1 : Form
    {
        PuppetMasterLogic puppetMaster;
        string[] scriptLines;
        int currentCommandLineIndex = 0;
        int allProccessesRead = 0;
        bool debugMode = false;
        List<string[]> workers = new List<string[]>();
        List<string[]> storages = new List<string[]>();
        string[] scheduler = null;
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
                textBoxScript.Text = "";
                currentCommandLineIndex = 0;
                int highlightStartPoint = 0;
                textFileScript.Text = fdlg.FileName;
                List<string> list = new List<string>();
                foreach (string line in System.IO.File.ReadLines(@fdlg.FileName))
                {
                    //Start the workers, storages, schedulers
                    string[] buffer = line.Split(' ');
                    switch (buffer[0])
                    {
                        case "debug":
                            debugMode = true;
                            currentCommandLineIndex++;
                            break;
                        case "scheduler":
                            if (scheduler != null)//Only allowed 1 scheduler per script
                                break;
                            currentCommandLineIndex++;
                            highlightStartPoint += line.Length + 1; //need to count the /n char
                            scheduler = buffer;
                            break;
                        case "worker":
                            workers.Add(buffer);
                            highlightStartPoint += line.Length + 1; //need to count the /n char
                            currentCommandLineIndex++;
                            break;
                        case "storage":
                            storages.Add(buffer);
                            highlightStartPoint += line.Length + 1; //need to count the /n char
                            currentCommandLineIndex++;
                            break;
                        default:
                            if(allProccessesRead == 0)
                            {
                                puppetMaster.CreateAllConfigEvents(scheduler, workers.ToArray(), storages.ToArray());
                                allProccessesRead = 1;
                            }
                            break;
                    }
                    list.Add(line);
                    textBoxScript.Text += line + "\r\n";
                }

                textBoxScript.Text += "End" + "\r\n";
                scriptLines = list.ToArray();

                textBoxScript.SelectionStart = highlightStartPoint;
                textBoxScript.SelectionLength = scriptLines[currentCommandLineIndex].Length;
                //Console.WriteLine(scriptLines[0]);
                textBoxScript.SelectionBackColor = Color.Yellow;
            }
        }

        private void ButtonBrowseAppData_Click(object sender, EventArgs e)
        {
            /*OpenFileDialog fdlg = new OpenFileDialog
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
            }*/

        }

        private void ButtonSendAppData_Click(object sender, EventArgs e)
        {
            /*puppetMaster.SendAppDataToScheduler(
                textAppDataFile.Text,
                textAppInput.Text);*/
        }

        private void TextAppInput_Click(object sender, EventArgs e)
        {
            textAppInput.Text = null;
        }

        private void ButtonCreateConnectionWithScheduler_Click(object sender, EventArgs e)
        {
            //puppetMaster.CreateChannelWithScheduler(this, "localhost", 4001, "localhost");
        }

        private void ButtonDebugCreateScheduler_Click(object sender, EventArgs e)
        {
            //puppetMaster.SendCreateProccessInstanceRequest("sched1", "http://localhost:2000");
        }

        private void ButtonNextStep_Click(object sender, EventArgs e)
        {

            if (currentCommandLineIndex + 1 > scriptLines.Length) { return; }
            //Console.WriteLine(scriptLines[0], currentCommandLineIndex);
            int index = currentCommandLineIndex;
            Thread t = new Thread(new ThreadStart(() =>
                                   puppetMaster.ExecuteCommand(scriptLines[index])));
            t.Start();

            currentCommandLineIndex += 1;
            textBoxScript.SelectionBackColor = Color.White;
            textBoxScript.SelectionStart += textBoxScript.SelectionLength + 1;

            if (currentCommandLineIndex + 1 <= scriptLines.Length)
                textBoxScript.SelectionLength = scriptLines[currentCommandLineIndex].Length;
            else { textBoxScript.SelectionLength = 3; }
            textBoxScript.SelectionBackColor = Color.Yellow;

        }
    }
}
