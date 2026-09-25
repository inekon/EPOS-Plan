namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Paarung EPOS-Zeile ↔ Quellentität — eine Zeile aus <c>Tab_Importzuordnung</c>
    /// (Schemaschritt S-F, <see cref="ImportzuordnungSchema"/>).
    ///
    /// <para><b>Genau eines der fünf Ziele ist gesetzt</b> (<c>CHECK</c> der Tabelle); die übrigen
    /// sind <c>null</c>. Öffentliche Felder nach dem Hausmuster der <c>*Model</c>-Klassen; geschrieben
    /// wird allein über <see cref="GebaeudeImportCtrl"/>.</para>
    /// </summary>
    public class ImportzuordnungModel
    {
        /// <summary>Primärschlüssel; 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>Die Quelle der Paarung (<c>Tab_Importquelle.ID</c>).</summary>
        public int ID_Importquelle;

        /// <summary>Ziel Projektgebäude; <c>null</c> = anderes Ziel.</summary>
        public int? ID_Gebaeude;

        /// <summary>Ziel Zone; <c>null</c> = anderes Ziel.</summary>
        public int? ID_Zone;

        /// <summary>Ziel Bauteil; <c>null</c> = anderes Ziel.</summary>
        public int? ID_Bauteil;

        /// <summary>Ziel Aufbau (Projektkopie); <c>null</c> = anderes Ziel.</summary>
        public int? ID_Aufbau;

        /// <summary>Ziel Baustoff (Projektkopie); <c>null</c> = anderes Ziel.</summary>
        public int? ID_Baustoff;

        /// <summary><c>IfcGloballyUniqueId</c> bzw. gbXML-<c>id</c>, höchstens 64 Zeichen (gekürzt).</summary>
        public string Quellkennung = "";

        /// <summary>Typ der Quellentität (<c>Building</c>, <c>Space</c>, <c>IfcSpace</c> …).</summary>
        public string Quelltyp = "";
    }
}
