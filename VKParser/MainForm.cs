using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace VKParser
{
    public partial class MainForm : Form
    {
        delegate void ControlAction();

        public MainForm()
        {
            InitializeComponent();
            Log("Парсер ВК инициализирован!");
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            parserWorker1.RunWorkerAsync();
        }

        private void parserWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void parserWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!parserWorker1.ParsingEnded)
            {
                MessageBox.Show("Закрыть парсер можно только после окончания работы!");
                e.Cancel = true;
            }
        }

        public void Log(string text)
        {
            ControlAction listBoxAction = new ControlAction(() =>
                {
                    string nowTime = DateTime.Now.ToString("[HH:mm:ss] ");
                    listBox1.Items.Add(nowTime + text);
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                });

            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(listBoxAction);
            }
            else
            {
                listBoxAction();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Program.FeedForm.Show();
        }
    }
}
