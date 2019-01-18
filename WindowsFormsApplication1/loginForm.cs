using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using agsXMPP;
using System.Threading;
using agsXMPP.protocol.iq.roster;

namespace WindowsFormsApplication1
{
    public partial class loginForm : Form
    {
        XmppClientConnection xmppCon;
        bool _bWait = false;
        bool onLogin = false;
        string prefix = "@odnoklassniki.ru";

        public loginForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label3.ForeColor = Color.Green;
            label3.Text = "Вход...";

            xmppCon = new XmppClientConnection();

            Jid jid = new Jid(textBox1.Text + prefix);

            xmppCon.Password = textBox2.Text;
            xmppCon.Username = jid.User;
            xmppCon.Server = jid.Server;
            xmppCon.AutoAgents = false;
            xmppCon.AutoPresence = true;
            xmppCon.AutoRoster = true;
            xmppCon.AutoResolveConnectServer = true;

            try
            {
                xmppCon.OnLogin += new ObjectHandler(xmppCon_OnLogin);
                xmppCon.Open();
                Wait();

            }
            catch (Exception b)
            {
                MessageBox.Show(b.Message);
            }

            if (onLogin)
            {
                Hide();
                xmppCon.Close();
                Thread.Sleep(200); //Надо для того чтобы соединение успело закрыться
                Form mainForm = new mainForm(xmppCon, this);
                mainForm.Show();
                label3.Text = "";
                textBox2.Clear();
            }
            else
            {
                label3.ForeColor = Color.Red;
                label3.Text = "Вы ввели неверный логин и/или пароль";
            }


        }

        void Wait()
        {
            int i = 0;
            _bWait = true;

            while (_bWait)
            {
                i++;
                if (i == 60)
                    _bWait = false;

                Thread.Sleep(50);
            }
        }


        void xmppCon_OnLogin(object sender)
        {
            onLogin = true;
        }
    }
}
