using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AppTerminator
{
    public partial class CommandInput : Form
    {
        private Form1 MainForm;
        public CommandInput(Form1 form)
        {
            InitializeComponent();
            MainForm = form;
            MainForm.isCmdFormShow = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Main
            if (!MainForm.isEditKeysMode)
            {
                MainForm.isCmdFormShow = false;
                MainForm.Command = textBox1.Text;
                MainForm.AddItemToList();
                this.Dispose();
            }

            //EditItem
            if (MainForm.isEditKeysMode)
            {
                MainForm.Command = textBox1.Text;
                MainForm.EditItem();
                MainForm.isCmdFormShow = false;
                MainForm.isEditKeysMode = false;
                this.Dispose();
            }
        }

        private void CommandInput_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
            }
        }

        private void CommandInput_Load(object sender, EventArgs e)
        {

        }
    }
}
