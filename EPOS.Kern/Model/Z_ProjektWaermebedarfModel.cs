using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjWaermebedarfModel
    {
        public Z_ProjWaermebedarfModel[] items;
        public int m_ID_Z;
        public int m_ID_Projekt;
        public int m_ID_Ganglinie;
        public string m_szBezeichner;

        /// <summary>
        /// Bedarfskanal dieser Ganglinie — <c>Z_ProjektWaermebedarf.Kanal</c>
        /// (Migrationsschritt 48, Entscheidung F18). Einer der Steuerwerte
        /// <see cref="DbWerte.KANAL_HEIZUNG"/>, <see cref="DbWerte.KANAL_BRAUCHWASSER"/>
        /// oder <see cref="DbWerte.KANAL_PROZESS"/>, NIE ein Anzeigetext.
        ///
        /// <para>Vorbelegung ist Heizung — der altverhaltenserhaltende Wert: Vor
        /// Schritt 48 lief jede externe Ganglinie in den Heizbedarf. Leer und NULL
        /// gelten bei jedem Leser ebenfalls als Heizung, eine Datenbank ohne die Spalte
        /// rechnet also unverändert.</para>
        ///
        /// <para>Die Feldnamen dieses Modells tragen historisch das Präfix
        /// <c>m_</c>; der Rechenkern greift auf den Kanal zu, deshalb steht er hier
        /// bewusst unter dem heutigen Namensschema.</para>
        /// </summary>
        public string Kanal;

        public Z_ProjWaermebedarfModel()
        {
            items = null;
            m_ID_Z = 0;
            m_ID_Projekt = 0;
            m_ID_Ganglinie = 0;
            m_szBezeichner = "";
            Kanal = DbWerte.KANAL_HEIZUNG;
        }
    }
}
