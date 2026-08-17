namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Kostenprofil eines Projekts (eine Zeile in Tab_Kostenprofil; Fachkonzept
    // Stromspeicher 4.1 b, angelegt von SchemaMigration Schritt 12c).
    //
    // Ablageformat wie Tab_Energieanlagen.WQ_Monatswerte/WQ_Wochenwerte: zwei
    // ";"-getrennte Zeichenketten mit InvariantCulture - genau das Format, das
    // Form_Quellprofil schon liest und schreibt. Die Umrechnung in ein 8760er
    // Jahresprofil macht die Engine (SpeicherEngine.PreisModell), nicht dieses Modell.
    //
    // Einheit beider Wertesaetze: ct/kWh. Der Monatswert traegt das Preisniveau, der
    // Wochenwert die Abweichung davon (Tagesgang, HT/NT, Wochenende).
    // ---------------------------------------------------------------------------
    public class KostenprofilModel
    {
        /// <summary>Primaerschluessel (MAX(ID)+1-Hausmuster).</summary>
        public int ID;

        /// <summary>Projekt, zu dem das Profil gehoert. 0 = keinem Projekt zugeordnet.</summary>
        public int ID_Projekt;

        /// <summary>Anzeigename, vom Anwender vergeben.</summary>
        public string Bezeichner = "";

        /// <summary>12 Monatswerte als "m1;...;m12" [ct/kWh], InvariantCulture.</summary>
        public string Monatswerte = "";

        /// <summary>
        /// 168 Wochenwerte als "w1;...;w168" [ct/kWh] ab Montag 0 Uhr,
        /// InvariantCulture. Darf leer sein - dann ist das Profil je Monat konstant.
        /// </summary>
        public string Wochenwerte = "";

        public KostenprofilModel()
        {
        }
    }
}
