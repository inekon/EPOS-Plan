using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using NReco.Csv;

namespace WindowsFormsApplication1
{
    public partial class Form_Start : Form
    {
        int index = 0;

        public Form_Start()
        {
            InitializeComponent();
            Init_TreeView();

        }

 

        private void Form_Start_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
        }

        private void Init_TreeView()
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll(); 

            for (int i = 0; i < ctrl.rows; i++)
            {
                TreeNode root = new TreeNode(ctrl.items[i].m_szProjektname );
                
                CreateNode(root);
                treeView_Projekt.Nodes.Add(root);
            }

        }

        void CreateNode(TreeNode node)
        {
            index++;
            if (index > 2) return;
            for (int i = 0; i < 10; i++)
            {
                TreeNode tnode = new TreeNode(i.ToString());
           
                node.Nodes.Add(tnode);
                CreateNode(tnode);
   
            }

        }
    }
}
