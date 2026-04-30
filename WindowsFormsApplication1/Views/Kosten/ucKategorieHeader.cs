using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class ucKategorieHeader : UserControl
    {
        public ucKategorieHeader(string titel)
        {
            InitializeComponent();
            this.lblTitle.Text = titel.ToUpper();

            // Styling passend zu deiner Form_Kosten
            this.BackColor = Color.FromArgb(15, 31, 61); // Dein 'Navy'
            this.ForeColor = Color.White;
            this.Height = 30;
            this.Margin = new Padding(0, 10, 0, 5); // Oben etwas Platz zum Trennen
        }
    }
}
