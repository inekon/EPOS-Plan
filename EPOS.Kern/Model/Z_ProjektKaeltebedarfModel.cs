namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zuordnungszeile <c>Z_Projekt_Kaeltebedarf</c> (Welle K1, Konzept Kältebedarf 3.3/3.4): Projektkopie, Jahressumme,
    /// Betriebskalender, Temperaturpaar der Kopie (Angabe) und die Deckungsart samt Split-Feldern. Bei „zentral“ stehen die
    /// Split-Felder leer (<c>null</c>), nicht 0 (<see cref="Z_ProjektKaeltebedarfCtrl.Normalisieren"/>).
    /// </summary>
    public class Z_ProjektKaeltebedarfModel
    {
        /// <summary>Kennung der Zuordnungszeile.</summary>
        public int ID_Z;

        /// <summary>Das Projekt.</summary>
        public int ID_Projekt;

        /// <summary>Die Projektkopie (<c>Tab_Kaeltebedarf.ID</c>); eine neu aufgenommene Zeile trägt die Katalog-ID.</summary>
        public int ID_Kaeltebedarf;

        /// <summary>Der Bezeichner (Name des Kopfsatzes).</summary>
        public string Bezeichner = "";

        /// <summary>Jahressumme [MWh].</summary>
        public double Summe;

        /// <summary>Betriebskalender der Zeile; <c>null</c> = ohne.</summary>
        public int? ID_Betriebskalender;

        /// <summary>Vorlauf der Projektkopie [°C] — Angabe ohne Wirkung.</summary>
        public double? Vorlauf;

        /// <summary>Rücklauf der Projektkopie [°C] — Angabe ohne Wirkung.</summary>
        public double? Ruecklauf;

        /// <summary>Das Temperaturpaar wurde im Dialog geändert und geht beim Speichern in die Projektkopie.</summary>
        public bool TemperaturGeaendert;

        /// <summary>Deckungsart: <see cref="KaeltebedarfSchema.DECKUNG_ZENTRAL"/> oder <see cref="KaeltebedarfSchema.DECKUNG_SPLIT"/>.</summary>
        public string Deckung = KaeltebedarfSchema.DECKUNG_ZENTRAL;

        /// <summary>EER-Weg bei Split (<c>fest</c>/<c>linear</c>); leer bei zentral.</summary>
        public string EerWeg;

        /// <summary>Fester EER bzw. EER am Punkt 1.</summary>
        public double? Eer1;

        /// <summary>Außentemperatur am Punkt 1 [°C].</summary>
        public double? Taussen1;

        /// <summary>EER am Punkt 2 (nur linear).</summary>
        public double? Eer2;

        /// <summary>Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
        public double? Taussen2;

        /// <summary>Kühlträger des Split-Stroms; <c>null</c> = Stromträger des Projekts.</summary>
        public int? KuehlIdCarrier;

        /// <summary>Eigener Zähler des Split-Stroms.</summary>
        public bool KuehlEigenerZaehler;

        /// <summary>
        /// Art der Kältebedarfszeile (KWN-0, Schritt <see cref="KaelteArtSchema.SCHRITT"/>): <see cref="KaelteArtSchema.RAUM"/>
        /// (Vorgabe) oder <see cref="KaelteArtSchema.PROZESS"/>. Prozesskälte bleibt im Kühlkanal und wird als Unterposten
        /// <c>Kaeltebedarf_Prozess</c> mitgeführt.
        /// </summary>
        public string KaelteArt = KaelteArtSchema.RAUM;

        /// <summary>
        /// Eingabeart der Zuordnung (KBP-2, Schritt <see cref="KaeltebedarfsprofilVorlagenSchema.SCHRITT"/>): 'jahr' (Vorgabe, Jahressumme
        /// mit Vorlage), 'monat', 'woche' oder 'typtag'. Gewählt im Bedarfsprofil-Dialog (KBP-3a), geschrieben von
        /// <see cref="WizardCtrl.Add_Projekt_Kaelte"/>.
        /// </summary>
        public string Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR;

        /// <summary>
        /// KBP-3a: bearbeitete Monatsanteile [%] der Projektkopie (12 Werte); <c>null</c> = unverändert. Geschrieben von
        /// <see cref="WizardCtrl.Add_Projekt_Kaelte"/> in <c>Tab_Kaeltebedarf.Monat_1…12</c> der Kopie (Konzept 3.2).
        /// </summary>
        public double[] Monatsanteile;

        /// <summary>
        /// KBP-3a: bearbeitetes Wochenprofil der Projektkopie (168 Werte, Montag 0 Uhr zuerst); <c>null</c> = unverändert.
        /// Geschrieben in <c>Tab_Kaeltetyp."1"…"168"</c> der Kopie (Konzept 3.3).
        /// </summary>
        public double[] Wochenprofil;

        /// <summary>
        /// KBP-4b: bearbeitete Typtage samt Zuordnungsmatrix der Projektkopie (Konzept 3.4); <c>null</c> = unverändert.
        /// Geschrieben von <see cref="WizardCtrl.Add_Projekt_Kaelte"/> über <see cref="KaeltetyptagCtrl.Schreiben"/>.
        /// </summary>
        public KaeltetyptagEingabe Typtage;

        /// <summary>
        /// KBP-5a: die Nutzungstage nach Norm der Projektkopie (<c>Tab_Kaeltebedarf.Nutzungstage_Norm</c>, Schritt
        /// <see cref="BedarfsprofilEingabeartSchema.SCHRITT"/>); <c>null</c> = keine Angabe. Nur gelesen — Grundlage des
        /// Kalendervorschlags beim Übernehmen (KBP-5b), kein Rechenweg liest sie.
        /// </summary>
        public int? NutzungstageNorm;
    }
}
