using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DiaryInfo
{
    public partial class AuthForm : Form
    {
        private OptionsHandlerDelegate d;
        bool closedWithSendData = false;
        public AuthForm(OptionsHandlerDelegate sender)
        {
            InitializeComponent();
            d = sender;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Visible = false;
            d.Invoke(textBox1.Text, textBox2.Text, textBox3.Text);
            closedWithSendData = true;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox2.PasswordChar = Char.MinValue;
            }
            else 
            {
                textBox2.PasswordChar = '*';
            }
        }

        private void AuthForm_Load(object sender, EventArgs e)
        {
            this.Select();
            this.ActiveControl = textBox1;
            textBox1.Focus();
            textBox1.Select();
            ShowInTaskbar = false;
            StringBuilder sb = new StringBuilder();
            sb.Append("Author: Yuri Astrov\n").Append("Version: ").Append(typeof(AuthForm).Assembly.GetName().Version.ToString());
            label5.Text = sb.ToString();
        }

        private void AuthForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!closedWithSendData)
                Application.Exit();
        }
    }
}
