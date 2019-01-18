using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using agsXMPP;
using agsXMPP.protocol.client;
using System.Threading;
using agsXMPP.protocol.iq.roster;
using agsXMPP.Collections;
using System.IO;
using System.Xml;

namespace WindowsFormsApplication1
{
    public partial class mainForm : Form
    {
        bool _bWait;
        XmppClientConnection xmppCon;
        RosterItem curUser;
        XmlDocument xDoc = new XmlDocument();
        Form logForm;
        Hashtable unReadList = new Hashtable();
        bool view = true; //Для таймера

        //string postfix = "@odnoklassniki.ru";

        List<RosterItem> friendList = new List<RosterItem>();

        public mainForm(XmppClientConnection xmpp, Form sender)
        {
            InitializeComponent();

            xmppCon = xmpp;
            logForm = sender;

            xmppCon.OnRosterItem += new XmppClientConnection.RosterHandler(xmppCon_OnRosterItem);
            xmppCon.OnMessage += new MessageHandler(xmppCon_OnMessage);
            xmppCon.Open();

            Wait();

            fillFriendCBox();

            if (File.Exists("src.xml"))
            {
                File.Delete("src.xml");
            }

            XmlTextWriter textWritter = new XmlTextWriter("src.xml", null);
            textWritter.WriteStartDocument();
            textWritter.WriteStartElement("Dialogs");
            textWritter.WriteEndElement();
            textWritter.Close();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            string To = "";
            if (textBox1.Text != "" && curUser != null)
            {
                To = curUser.Jid.ToString();

                string msg = textBox1.Text;

                xmppCon.Send(new agsXMPP.protocol.client.Message(new Jid(To), MessageType.chat, msg));
            
            richTextBox1.SelectionColor = Color.Blue;
            richTextBox1.AppendText("Я: ");
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.AppendText(textBox1.Text + "\n");
            textBox1.Clear();
            }
            else MessageBox.Show("Вы не выбрали друга и/или не написали текст сообщение");

        }

        private void MessageCallback(object sender, agsXMPP.protocol.client.Message msg, object data)
        {
            if (InvokeRequired)
            {			
                BeginInvoke(new MessageCB(MessageCallback), new object[] { sender, msg, data });
                return;
            }

            if (msg.Body != null)
                recieveMsg(msg);
        }

        void xmppCon_OnRosterItem(object sender, RosterItem item)
        {
            friendList.Add(item);
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

        string getNameByJID(Jid jid)
        {
            foreach (RosterItem item in friendList)
                if (jid.Equals(item.Jid)) return item.Name;
            return "неизвестный";
        }

        void recieveMsg(agsXMPP.protocol.client.Message msg)
        {

            if (curUser!=null && msg.From.Equals(curUser.Jid))  //Если написал тот пользователь, с кем сейчас диалог
            {
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.AppendText(curUser.Name + ": ");
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.AppendText(msg.Body + "\n");
            }
            else
            {
                xDoc.Load("src.xml");
                XmlElement xRoot = xDoc.DocumentElement;

                XmlNode node = xRoot.SelectSingleNode("user[@JID='" + msg.From + "']");

                if (node == null)
                {
                    XmlElement userElem = xDoc.CreateElement("user");
                    XmlElement textUser = xDoc.CreateElement("text");
                    XmlElement rtfUser = xDoc.CreateElement("rtf");
                    XmlAttribute jidAttr = xDoc.CreateAttribute("JID");

                    jidAttr.AppendChild(xDoc.CreateTextNode(msg.From.ToString()));
                    userElem.Attributes.Append(jidAttr);

                    newMsgBox.SelectionColor = Color.Red;
                    newMsgBox.AppendText(getNameByJID(msg.From) + ": ");
                    newMsgBox.SelectionColor = Color.Black;
                    newMsgBox.AppendText(msg.Body + "\n");

                    textUser.InnerText = newMsgBox.Text;
                    rtfUser.InnerText = newMsgBox.Rtf;

                    newMsgBox.Clear();

                    userElem.AppendChild(textUser);
                    userElem.AppendChild(rtfUser);

                    xRoot.AppendChild(userElem);
                }
                else
                {
                    newMsgBox.Rtf = node.SelectSingleNode("rtf").InnerText;

                    newMsgBox.SelectionColor = Color.Red;
                    newMsgBox.AppendText(getNameByJID(msg.From) + ": ");
                    newMsgBox.SelectionColor = Color.Black;
                    newMsgBox.AppendText(msg.Body + "\n");

                    node.SelectSingleNode("text").InnerText = newMsgBox.Text;
                    node.SelectSingleNode("rtf").InnerText = newMsgBox.Rtf;

                    newMsgBox.Clear();
                }
                addUnRead(msg.From.ToString());
                xDoc.Save("src.xml");
            }
        }

        void addUnRead(string jid)
        {
            if (unReadList.ContainsKey(jid)) unReadList[jid] = (int)unReadList[jid] + 1;
            else unReadList.Add(jid, 1);

            if (!timer1.Enabled) timer1.Enabled = true;
            for (int i = 0; i < friendList.Count; i++)
                if (friendList[i].Jid.ToString() == jid)
                {
                    friendComboBox.Items.RemoveAt(i);
                    friendComboBox.Items.Insert(i, "(" + unReadList[jid] + ")" + friendList[i].Name);
                    break;
                }
        }

        void xmppCon_OnMessage(object sender, agsXMPP.protocol.client.Message msg)
        {
        }

        void fillFriendCBox()
        {
            foreach (RosterItem item in friendList)
            {
                friendComboBox.Items.Add(item.Name);
                xmppCon.MessageGrabber.Add(item.Jid, new BareJidComparer(), new MessageCB(MessageCallback), null);
            }
        }

        void checkUnRead() //Удалить, если прочитал
        {
            if (unReadList.ContainsKey(curUser.Jid.ToString()))
            {
                unReadList.Remove(curUser.Jid.ToString());
                for (int i = 0; i < friendList.Count; i++)
                    if (friendList[i].Jid.Equals(curUser.Jid))
                    {
                        friendComboBox.Items.RemoveAt(i);
                        friendComboBox.Items.Insert(i, friendList[i].Name);
                        friendComboBox.SelectedItem = friendComboBox.Items[i];
                        break;
                    }
            }

            if (unReadList.Count == 0)
            {
                timer1.Enabled = false;
                Text = "Одноклассники";
            }
        }

        private void friendComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (curUser != null)
            {
                xDoc.Load("src.xml");
                //Сохраняем все в XML
                XmlElement xRoot = xDoc.DocumentElement;

                //Ищем текущего юзера в XMLке
                XmlNode node = xRoot.SelectSingleNode("user[@JID='" + curUser.Jid + "']");

                if (node == null)  //Если пользователя еще не было - добавляем его
                {
                    XmlElement userElem = xDoc.CreateElement("user");
                    XmlElement textUser = xDoc.CreateElement("text");
                    XmlElement rtfUser = xDoc.CreateElement("rtf");
                    XmlAttribute jidAttr = xDoc.CreateAttribute("JID");

                    jidAttr.AppendChild(xDoc.CreateTextNode(curUser.Jid.ToString()));
                    userElem.Attributes.Append(jidAttr);

                    textUser.InnerText = richTextBox1.Text;
                    rtfUser.InnerText = richTextBox1.Rtf;


                    userElem.AppendChild(textUser);
                    userElem.AppendChild(rtfUser);

                    xRoot.AppendChild(userElem);
                }
                else
                {
                    node.SelectSingleNode("text").InnerText = richTextBox1.Text;
                    node.SelectSingleNode("rtf").InnerText = richTextBox1.Rtf;
                }
                xDoc.Save("src.xml");
            }

            richTextBox1.Clear();

            if (friendComboBox.SelectedItem != null)
            {
                curUser = friendList[friendComboBox.SelectedIndex];

                xDoc.Load("src.xml");
                XmlElement xRoot = xDoc.DocumentElement;
                XmlNode node = xRoot.SelectSingleNode("user[@JID='" + curUser.Jid + "']");
                if (node != null)
                {
                    richTextBox1.Rtf = node.SelectSingleNode("rtf").InnerText;
                }
            }

            checkUnRead();  //Проверяем, выбран ли юзер с которым были непрочитанные сообщения
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Вы уверены что хотите выйти ?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                logForm.Show();
            }
            else e.Cancel = true;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Text = view ? "Вам сообщение" : "***************";
            view = !view;
        }
    }
}
