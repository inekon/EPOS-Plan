namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sprachneutralen Maskenschlüssel für <see cref="INavigation.OeffneMaske"/>.
    ///
    /// <para>Bis auf die drei zusammengesetzten Abläufe am Ende ist der Schlüssel der
    /// KLASSENNAME der Maske — dadurch bleibt jede Fundstelle ohne Nachschlagen lesbar
    /// und die Tabelle in <c>WinFormsNavigation</c> ist eine reine Zuordnung. Ein
    /// unbekannter Schlüssel tut nichts und liefert <c>false</c>.</para>
    /// </summary>
    public static class Masken
    {
        /// <summary>Stammdaten Wärmepumpen.</summary>
        public const string WpAdministration = "Form_WP";
        /// <summary>Stammdaten Stromspeicher.</summary>
        public const string StromspeicherAdmin = "Form_AdminStromspeicher";
        /// <summary>Lastspitzenkappung; Argument: Projekt-ID.</summary>
        public const string PeakShaving = "Form_PeakShaving";
        /// <summary>Stammdaten Gebäude.</summary>
        public const string GebaeudeAdmin = "Form_Gebaeude";
        /// <summary>Stammdaten Gebäudetypen.</summary>
        public const string GebaeudetypenAdmin = "Form_EingGebTyp";
        /// <summary>Stammdaten eingelesener Wärmebedarf.</summary>
        public const string WaermebedarfExternAdmin = "Form_AdminWaermeeinlesen";
        /// <summary>Stammdaten Prozesswärme.</summary>
        public const string ProzesswaermeAdmin = "Form_Prozesswaerme_Admin";
        /// <summary>Stammdaten Stromverbraucher.</summary>
        public const string StromverbraucherAdmin = "Form_Stromverbraucher_Admin";
        /// <summary>Stammdaten Stromganglinien.</summary>
        public const string StromganglinieAdmin = "Form_Stromganglinie_Admin";
        /// <summary>Stammdaten Solarganglinien.</summary>
        public const string SolarganglinieAdmin = "Form_Solarganglinie_Admin";
        /// <summary>Herstellerdaten Wärmepumpen einlesen.</summary>
        public const string WpImport = "Form_WP_einlesen";
        /// <summary>Stammdaten Heizkessel.</summary>
        public const string HeizkesselAdmin = "Form_Heizkessel_Admin";
        /// <summary>Stammdaten BHKW.</summary>
        public const string BhkwAdmin = "Form_BHKWAdmin";
        /// <summary>Stammdaten Solarkollektoren.</summary>
        public const string SolarkollektorenAdmin = "Form_SolarKollektorenAdmin";
        /// <summary>Stammdaten Photovoltaik.</summary>
        public const string PvAdmin = "Form_AdminPV";
        /// <summary>Herstellerdaten Heizkessel einlesen.</summary>
        public const string HeizkesselImport = "Form_Heizkessel_einlesen";
        /// <summary>Herstellerdaten Pufferspeicher einlesen.</summary>
        public const string PufferSpImport = "Form_PufferSp_einlesen";
        /// <summary>Stammdaten Pufferspeicher.</summary>
        public const string PufferSpAdmin = "Form_PufferSp_Admin";
        /// <summary>Stammdaten Brauchwasser.</summary>
        public const string BrauchwasserAdmin = "Form_Brauchwasser_Admin";
        /// <summary>Herstellerdaten Solarkollektoren einlesen.</summary>
        public const string SolarkollektorenImport = "Form_SolarKollektoren_einlesen";
        /// <summary>„Speichern unter…" — dupliziert ein Projekt.</summary>
        public const string ProjektSpeichernUnter = "Form_ProjektSpeichernUnter";

        /// <summary>
        /// Projektauswahl. Argument: ein <see cref="Projektwahl"/>-Fach, in das die
        /// Maske ihr Ergebnis legt.
        /// </summary>
        public const string ProjektAuswahl = "Form_ProjektAuswahl";

        /// <summary>
        /// Projekt löschen — nur die AUSWAHL des Projekts; gelöscht wird danach vom
        /// Aufrufer. Argument: ein <see cref="Projektwahl"/>-Fach.
        /// </summary>
        public const string ProjektDelete = "Form_ProjektDelete";

        /// <summary>
        /// Der Projektassistent. Argument: die Betriebsart
        /// (<c>WizardParent.WIZARD_MODE_NEU</c> bzw. <c>…_BEARBEITEN</c>).
        /// Rückgabe <c>true</c>, wenn der Assistent gespeichert hat.
        /// </summary>
        public const string Assistent = "ASSISTENT";

        /// <summary>
        /// Das Detailformular „Konfiguration Projekt" mit allen Gewerkslisten und
        /// Kontextmenüs. Argumente: Projektname und Projekt-ID.
        /// </summary>
        public const string ProjektDetail = "PROJEKT_DETAIL";
    }
}
