using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Helfer-Klasse zum Öffnen von Sub-Forms.
    ///
    /// Historisch: Die App war eine MDI-Anwendung; "openForm" hat Sub-Forms als MDI-Children
    /// in MDIMainForm eingehängt. Seit der Umstellung auf SDI (MDIMainForm.IsMdiContainer=false,
    /// Form_Start als eingebettete Hauptansicht) werden Sub-Forms stattdessen
    /// **modal** über der Hauptform gezeigt.
    /// </summary>
    class MDIHelperClass
    {
        public Form newMDIChild;

        public Form openForm(Type clazz, Form mainForm)
        {
            object theObject = Activator.CreateInstance(clazz);
            Form openFrm = (Form)theObject;

            // Owner setzen, damit die Dialog-Form korrekt minimiert/positioniert wird
            // und immer über der Hauptform liegt.
            newMDIChild = openFrm;
            if (mainForm != null && !mainForm.IsDisposed)
            {
                openFrm.ShowDialog(mainForm);
            }
            else
            {
                openFrm.ShowDialog();
            }
            return newMDIChild;
        }
    }
}
