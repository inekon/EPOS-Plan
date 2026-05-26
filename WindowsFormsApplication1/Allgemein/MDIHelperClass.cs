using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class MDIHelperClass
    {
        public Form newMDIChild;

        public Form openForm(Type clazz, Form mainForm)
        {
            Object theObject = Activator.CreateInstance(clazz);
            Form parentForm = mainForm; // Hier muss die Instanz des übergeordneten MDI-Formulars sein

            // Zugriff auf die Sammlung der MDI-Child-Fenster
            Form[] mdiChildren = parentForm.MdiChildren;

            // Iterieren über die Sammlung
            bool bFound = false;
            foreach (Form childForm in mdiChildren)
            {
                Console.WriteLine("MDI-Child-Fenster: " + childForm.Text);

                if (childForm.Text == ((Form)theObject).Text)
                {
                    childForm.BringToFront();
                    bFound = true;
                    newMDIChild = childForm;
                }
            }

            if (!bFound)
            {
                newMDIChild = (Form)theObject;
 
                // Set the Parent Form of the Child window.
                newMDIChild.MdiParent = mainForm;

                newMDIChild.Height = mainForm.ClientRectangle.Height - System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight; ;
                newMDIChild.Width = mainForm.ClientRectangle.Width - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth; 
                // Display the new form.
                newMDIChild.Show();
            }
            mainForm.Refresh();
            return newMDIChild;
        }
    }
}
