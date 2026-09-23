using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Weiche des Brauchwasserkanals (Konzept 2.2): je Projekt genau ein Weg.
    /// Ohne Zeile in <c>Tab_TwwProjekt</c> oder ohne Tabelle gilt <see cref="Bestand"/>.
    /// </summary>
    internal enum BrauchwasserWeg
    {
        /// <summary><c>BESTAND</c> — der heutige Code, Zeichen für Zeichen (Vorgabe).</summary>
        Bestand = 0,

        /// <summary><c>GENERATOR</c> — der Kanal kommt aus dem Zapfprofilgenerator.</summary>
        Generator = 1
    }

    /// <summary>Die Einheit eines Jahresmesswerts (<c>Tab_TwwZone.Jahresmesswert_Einheit</c>).</summary>
    internal enum ZapfMesswerteinheit
    {
        KwhJeJahr = 1,
        KubikmeterJeJahr = 2
    }

    /// <summary>Die Methode der Zirkulation (<c>Tab_TwwProjekt.Zirk_Methode</c>, Konzept 4.3).</summary>
    internal enum ZapfZirkulationsmethode
    {
        Leitungslaenge = 1,
        Anteil = 2,
        Flaechenkennwert = 3
    }

    /// <summary>Die Lage der Zirkulationsleitung (<c>Tab_TwwProjekt.Zirk_Lage</c>).</summary>
    internal enum ZapfLeitungslage
    {
        InnerhalbHuelle = 1,
        AusserhalbHuelle = 2
    }

    /// <summary>Die Speicherart der Auslegung (<c>Tab_TwwProjekt.Speicherart</c>, Konzept 4.5).</summary>
    internal enum ZapfSpeicherart
    {
        Ladespeicher = 1,
        GemischterSpeicher = 2
    }

    /// <summary>Die Quelle des Bedarfstags der Auslegung (<c>Tab_TwwProjekt.Bedarfstag_Quelle</c>, Konzept 4.5).</summary>
    internal enum ZapfBedarfstagquelle
    {
        Stundenprofil = 1,
        A100Referenz = 2,
        Din4708Profil = 3,
        Konstruktor = 4,
        Ecodesign = 5
    }

    /// <summary>Eine Zeile der Wohnungstabelle einer Zone (<c>Tab_TwwWohnungstyp</c>).</summary>
    internal sealed record WohnungstypStand
    {
        public int Id { get; init; }
        public int Anzahl { get; init; }
        public double? Raumzahl { get; init; }

        /// <summary><c>null</c> = Belegung nach Raumzahl aus dem Katalog.</summary>
        public double? Personen { get; init; }

        /// <summary>Ausstattungsklasse aus <c>Tab_TwwDin4708Wert_STAMM</c>; <c>null</c> = Vorgabeklasse.</summary>
        public int? IdAusstattung { get; init; }

        public int Reihenfolge { get; init; }
    }

    /// <summary>
    /// Eine Zone eines Projekts (<c>Tab_TwwZone</c>, Konzept 3.1). Jede nullbare Größe heißt
    /// <c>null</c> = Vorgabe; die Einheit steht im Namen.
    /// </summary>
    internal sealed record ZonenStand
    {
        public int Id { get; init; }
        public int IdNutzungsart { get; init; }

        /// <summary><c>null</c> = Tagesgangsatz der Nutzungsart.</summary>
        public int? IdTagesgangsatz { get; init; }

        public int? IdGebaeude { get; init; }
        public int Reihenfolge { get; init; }
        public string Name { get; init; } = "";

        /// <summary>Menge in der Bezugsart der Nutzungsart.</summary>
        public double Bezugsmenge { get; init; }

        public ZapfNiveau Niveau { get; init; } = ZapfNiveau.Mittel;
        public double? PersonenJeWe { get; init; }
        public double? WohnflaecheJeWeM2 { get; init; }
        public ZapfTopologie Topologie { get; init; } = ZapfTopologie.Speicher;

        /// <summary><c>false</c> nimmt die Zone aus dem Zirkulationsanteil (4.3).</summary>
        public bool Zirkulation { get; init; } = true;

        /// <summary>Vier Ferienfenster, Jahrestag des Beginns; <c>null</c>, 0 und 366 = keine Angabe.</summary>
        public int?[] Ferienbeginn { get; init; } = new int?[4];

        /// <summary>Vier Ferienfenster, Jahrestag des Endes.</summary>
        public int?[] Ferienende { get; init; } = new int?[4];

        public double? Jahresmesswert { get; init; }
        public ZapfMesswerteinheit? JahresmesswertEinheit { get; init; }
        public ZapfBilanzgrenze? JahresmesswertBilanzgrenze { get; init; }
        public string JahresmesswertQuelle { get; init; }
        public string JahresmesswertZeitraum { get; init; }
        public double? SpeicherverlustKwhJeJahr { get; init; }
        public bool TagesbedarfAuto { get; init; } = true;
        public double? TagesbedarfManuellKwh { get; init; }
        public double? BedarfSpezKwhJeEinheitTag { get; init; }
        public double? ZapftemperaturC { get; init; }
        public double? KaltwasserMittelC { get; init; }
        public double? KaltwasserAmplitudeK { get; init; }

        /// <summary>Zwölf Auslastungsfaktoren als Überschreibung der Monatsfaktoren; <c>null</c> = Katalog.</summary>
        public double?[] Auslastung { get; init; } = new double?[12];

        /// <summary>Die Wohnungstabelle der Zone in ihrer Reihenfolge; leer, wenn keine gepflegt ist.</summary>
        public IReadOnlyList<WohnungstypStand> Wohnungen { get; init; } = new WohnungstypStand[0];
    }

    /// <summary>
    /// Weiche und gebäudeweite Größen eines Projekts (<c>Tab_TwwProjekt</c>, Konzept 3.1).
    /// Jede nullbare Größe heißt <c>null</c> = Vorgabe; die Einheit steht im Namen.
    /// </summary>
    internal sealed record ProjektStand
    {
        public int Id { get; init; }
        public BrauchwasserWeg Weg { get; init; }
        public bool JahresreiheStochastisch { get; init; }
        public int Seed { get; init; }
        public int Realisierungen { get; init; }
        public int? RealisierungenAuslegung { get; init; }
        public int Perzentil { get; init; }
        public bool ZirkAuto { get; init; }
        public ZapfZirkulationsmethode ZirkMethode { get; init; }
        public ZapfLeitungslage? ZirkLage { get; init; }
        public double? ZirkLaengeM { get; init; }
        public double? ZirkVerlustWJeM { get; init; }
        public double? ZirkAnteil { get; init; }
        public double? ZirkKennwert { get; init; }
        public double? ZirkFlaecheM2 { get; init; }
        public double? ZirkLaufzeitH { get; init; }
        public double? ZirkManuellKw { get; init; }
        public double? LeitungsinhaltL { get; init; }
        public bool LadeAuto { get; init; }
        public double? LadefensterH { get; init; }
        public double? LadefensterBeginnH { get; init; }
        public double? LadeManuellKw { get; init; }
        public double? SpeicherC { get; init; }
        public double? KaltwasserAuslegungC { get; init; }
        public double? ErzeugerKw { get; init; }
        public double? UebertragerKw { get; init; }
        public double? UebertragerUaWJeK { get; init; }
        public double? UebertragerFlaecheM2 { get; init; }
        public ZapfSpeicherart Speicherart { get; init; }
        public double? SensorhoeheAnteil { get; init; }
        public double? NachweisVolumenL { get; init; }
        public double? SpeicherverlustW { get; init; }
        public double? Nutzanteil { get; init; }
        public double? Zuschlag { get; init; }
        public ZapfBedarfstagquelle? BedarfstagQuelle { get; init; }
        public int? IdBedarfstag { get; init; }
        public double? AuslegungVolumenL { get; init; }
        public double? AuslegungLeistungKw { get; init; }
        public string Aenderungsdatum { get; init; }
    }

    /// <summary>
    /// <b>Der Arbeitsstand eines Projekts</b> (Konzept 2.1) — die Übergabeform zwischen
    /// <see cref="ZapfprofilCtrl"/> und Hülle, ohne Oberflächenbezug.
    ///
    /// <para><see cref="Projekt"/> ist <c>null</c>, solange das Projekt keine Zeile in
    /// <c>Tab_TwwProjekt</c> trägt: Die Vorgaben stehen dann allein in der DDL
    /// (<see cref="TwwSchema"/>) und entstehen erst mit dem ersten Speichern (Z1) — eine
    /// zweite Abschrift der Vorgabewerte im Quelltext gibt es nicht.</para>
    /// </summary>
    internal sealed record ZapfprofilStand(BrauchwasserWeg Weg, IReadOnlyList<ZonenStand> Zonen, ProjektStand Projekt);

    /// <summary>Warum der Zapfprofilgenerator nicht zur Verfügung steht.</summary>
    internal enum ZapfVerfuegbarkeitsgrund
    {
        /// <summary>Tabellen und Katalogversion stehen.</summary>
        Verfuegbar = 0,

        /// <summary>Mindestens eine der zehn Tww-Tabellen fehlt (etwa ein älterer iOS-Seed, 3.2).</summary>
        TabellenFehlen = 1,

        /// <summary>Die Tabellen stehen, aber <c>Tab_TwwParameter_STAMM</c> trägt keine Katalogversion.</summary>
        KeineKatalogversion = 2
    }

    /// <summary>
    /// Die benannte Antwort auf „kann der Generator hier laufen?" — ein Kennzeichen, der Grund
    /// und ein Klartext für Protokoll und Test (die Oberfläche übersetzt den Grund selbst).
    /// </summary>
    internal sealed record ZapfVerfuegbarkeit(bool Ja, ZapfVerfuegbarkeitsgrund Grund, string Klartext);
}
