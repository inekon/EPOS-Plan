using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class StromganglinieModel
    {
        public int ID;
        public int m_ID_Ganglinie;
        public string m_szBezeichner;
        public int m_Zeitinterval;

        /// <summary>
        /// Auslieferungssatz (<c>Tab_Stromganglinie_STAMM.ReadOnly</c>) — er darf
        /// nicht geloescht werden (iU9-W12-E-1).
        ///
        /// <para><b>Warum es hier steht.</b> Das Kennzeichen stand bis hierher nur
        /// hinter <see cref="StromganglinieStammCtrl.IsReadOnly"/>, einer eigenen
        /// Abfrage je Name: Die Verwaltungshuelle stellte damit je Katalogzeile eine
        /// zweite Abfrage (N+1), und die Zuordnungshuelle gab schlicht <c>false</c>
        /// weiter — der Projektdialog konnte einen Auslieferungssatz also gar nicht
        /// erkennen. <c>ReadAll</c> liest die Spalte ohnehin mit
        /// (<c>SELECT *</c>); sie wird jetzt nur nicht mehr weggeworfen.</para>
        /// </summary>
        public bool m_bReadOnly;

        public StromganglinieModel[] items;

        public StromganglinieModel()
        {
            ID = 0;
            m_szBezeichner = "";
            m_ID_Ganglinie = 0;
            m_Zeitinterval = 0;
            m_bReadOnly = false;
            items = null;
        }
    }
}
