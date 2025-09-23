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
    public partial class Form_Klimaregionen : Form
    {
        public Form_Klimaregionen()
        {
            InitializeComponent();
        }

        private void Form_Klimaregionen_Load(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.ReadAllKlimaregion();
            ctrl.FillListBox(listBox_Klimaregionen);


        }
        private void button_Klima_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.OpenExcel("Klima");
            ctrl.WriteKlimaregion();
            ctrl.WriteKlimadaten();
            ctrl.ReadAllKlimaregion();
            ctrl.FillListBox(listBox_Klimaregionen);
        }
    }
}
