using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class MenueCtrl
    {
        public Form OpenForm(Type clazz, bool bChildWindow=true)
        {
            if (bChildWindow)
            {
                MDIHelperClass openFrm = new MDIHelperClass();
                Form mdichild = openFrm.openForm(clazz, Program.mdifrm);
                return mdichild;
            }
            else
            {
                Object theObject = Activator.CreateInstance(clazz);
                Form openFrm = (Form)theObject;
                openFrm.Show();
                openFrm.Focus();
                return openFrm;
            }
        }

        public void SetProjektname()
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle("Select * from Tab_Applikation where ID=1");
            FormMain frm = (FormMain)Program.mainfrm;
            frm.SetProjekt(ctrl.Projektname);
        }
    }
}
