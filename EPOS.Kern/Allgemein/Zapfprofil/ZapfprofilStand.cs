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

    /// <summary>
    /// Die Stufe des Zapfprofil-Dialogs (Konzept 5.1): Sie blendet an der Oberfläche nur ein und aus;
    /// im Kern bestimmt sie allein die Marke „Schnellauslegung" der Auslegung (N11 (c)). Die Zahlen
    /// sind die des DTO <c>ZapfprofilStufe</c>.
    /// </summary>
    internal enum ZapfStufe
    {
        Einfach = 0,
        Erweitert = 1,
        Experte = 2
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

        /// <summary>
        /// Fläche aus dem gebundenen Gebäude (A8) [m²] — <b>nur im Eingang</b> gesetzt
        /// (<c>ZapfprofilCtrl.Eingang</c>), nie gelesen oder gespeichert; <c>null</c> ohne
        /// Bindung. Gilt nur, wenn die Zone keine eigene Fläche trägt
        /// (<see cref="Mengengeruest.FlaecheM2"/>).
        /// </summary>
        public double? GebaeudeflaecheM2 { get; init; }
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

        /// <summary>
        /// Die gewählte Erzeugerart am Speicher (<c>Tab_TwwProjekt.Erzeugerart</c>, Schritt 124;
        /// N10 (i), N11 (i)); <c>null</c> = keine Angabe — dann gilt der Vorschlag des
        /// Anlagenbestands, wenn er eindeutig ist. Der Vorschlag wird nie gespeichert.
        /// </summary>
        public ZapfErzeugerart? Erzeugerart { get; init; }

        /// <summary>Der Werkstoff des Übertragers (<c>Tab_TwwProjekt.Uebertrager_Werkstoff</c>, Schritt 124); <c>null</c> = keine Angabe.</summary>
        public ZapfUebertragerwerkstoff? UebertragerWerkstoff { get; init; }

        /// <summary>
        /// Die Personen des Verfahrensvergleichs auto/manuell (<c>Tab_TwwProjekt.Personen_Auto</c>,
        /// Schritt 124; N11 (d)): <c>true</c> = aus dem Mengengerüst. Ohne Projektzeile oder vor
        /// Schritt 124 gilt die DDL-Vorgabe (1).
        /// </summary>
        public bool PersonenAuto { get; init; } = true;

        /// <summary>Der manuelle Wert der Personen (<c>Tab_TwwProjekt.Personen_Manuell</c>); wirkt nur bei <see cref="PersonenAuto"/> = false.</summary>
        public double? PersonenManuell { get; init; }

        /// <summary>Der Bezug des Füllstands (<c>Tab_TwwProjekt.Fuellstand_Bezug</c>, Schritt 124; N10 (k), N11 (d)); <c>null</c> = Vorgabe.</summary>
        public ZapfFuellstandbezug? FuellstandBezug { get; init; }

        /// <summary>
        /// <b>Rechnet der Jahresgang über die eingespielten Typtage?</b>
        /// (<c>Tab_TwwProjekt.Typtage_Aktiv</c>, Schritt 131; Konzept 4.2, N14 (i), Stufe Z4b)
        /// <c>false</c> = der Formvektor wie im Bestand. Ohne Projektzeile oder vor Schritt 131
        /// gilt die DDL-Vorgabe (0) — der Bestandsweg.
        /// </summary>
        public bool TyptageAktiv { get; init; }

        /// <summary>
        /// Die gewählte Klimazone des eingespielten Pakets
        /// (<c>Tab_TwwProjekt.Typtage_Klimazone</c>, Schritt 131); <c>null</c> = keine Wahl. Die
        /// Nummer ist die des Pakets, nicht eine Kennung der Datenbank (N14 (d)).
        /// </summary>
        public int? TyptageKlimazone { get; init; }

        /// <summary>
        /// Die gewählte Gebäudeart des eingespielten Pakets
        /// (<c>Tab_TwwProjekt.Typtage_Gebaeudeart</c>, Schritt 131); <c>null</c> oder leer = keine
        /// Wahl.
        /// </summary>
        public string TyptageGebaeudeart { get; init; }
    }

    /// <summary>
    /// <b>Der Arbeitsstand eines Projekts</b> (Konzept 2.1) — die Übergabeform zwischen
    /// <see cref="ZapfprofilCtrl"/> und Hülle, ohne Oberflächenbezug.
    ///
    /// <para><see cref="Projekt"/> ist <c>null</c>, solange das Projekt keine Zeile in
    /// <c>Tab_TwwProjekt</c> trägt: Die Vorgaben stehen dann allein in der DDL
    /// (<see cref="TwwSchema"/>) und entstehen erst mit dem ersten Speichern (Z1) — eine
    /// zweite Abschrift der Vorgabewerte im Quelltext gibt es nicht.</para>
    ///
    /// <para><see cref="BedarfstagEntwurf"/> trägt einen vom Konstruktor gebauten, noch nicht
    /// gespeicherten Bedarfstag der Auslegung (Stufe Z2, Gruppe 2): Er wird erst mit dem
    /// Arbeitsstand geschrieben — als Katalogzeile im selben Vorgang
    /// (<c>ZapfprofilCtrl.Speichern</c>); bis dahin verwirft ein Abbrechen ihn mit dem Stand.</para>
    /// </summary>
    internal sealed record ZapfprofilStand(BrauchwasserWeg Weg, IReadOnlyList<ZonenStand> Zonen, ProjektStand Projekt)
    {
        /// <summary>
        /// Der konstruierte, noch ungespeicherte Bedarfstag (Quelle Konstruktor, Id
        /// <c>ZapfprofilCtrl.ENTWURF_ID</c>); <c>null</c> = keiner. Solange er steht, zeigt die
        /// Projektzeile mit <see cref="ProjektStand.BedarfstagQuelle"/> Konstruktor auf ihn
        /// (<see cref="ProjektStand.IdBedarfstag"/> leer).
        /// </summary>
        public BedarfstagKatalogzeile BedarfstagEntwurf { get; init; }

        /// <summary>
        /// Die Zeilen, aus denen der Konstruktor den <see cref="BedarfstagEntwurf"/> baute (N11 (j))
        /// — und die er, einmal gespeichert, wieder hergibt: Ein erneutes Öffnen des Konstruktors
        /// beginnt mit ihnen. Sie stehen an <c>Tab_TwwKonstruktorzeile</c> am Auslegungssatz
        /// (Schemaschritt T5, Anwenderentscheid ZU25), reisen mit Projektkopie und Paket und werden
        /// beim Speichern ERSETZEND geschrieben. Die Katalogzeile des Tags trägt weiter nur
        /// Ereignisse — aus Minuten und Energien ließe sich keine Zeile zurückrechnen. Leer = keine.
        /// </summary>
        public IReadOnlyList<KonstruktorzeileStand> Konstruktorzeilen { get; init; } = new KonstruktorzeileStand[0];

        /// <summary>
        /// Der Bezug eines <b>gespeicherten</b> Konstruktortags — Bezugsart und Bezugsmenge seiner
        /// Katalogzeile (<c>Tab_TwwBedarfstag_STAMM</c>, Schritt 124), gelesen, wenn die Projektzeile
        /// mit Quelle Konstruktor auf einen Katalogtag zeigt. Ein erneut geöffneter Konstruktor
        /// beginnt mit ihm, damit ein erneutes OK denselben Tag baut. <c>null</c> = kein
        /// gespeicherter Konstruktortag (oder ein noch ungespeicherter <see cref="BedarfstagEntwurf"/>,
        /// der seinen Bezug selbst trägt).
        /// </summary>
        public KonstruktorBezugStand KonstruktorBezug { get; init; }

        /// <summary>
        /// Die Laufangaben der Anzeige (N9 (h)):Temperatur der Literanzeige und Schwelle der
        /// Stundenzählung — nicht gespeichert; <c>null</c> = die Einstellung, sonst die Vorgabe des
        /// Parametersatzes (<see cref="ZapfParameter.ANZEIGETEMPERATUR"/>,
        /// <see cref="ZapfParameter.STUNDENSCHWELLE"/>; aufgelöst in <c>ZapfprofilCtrl.Eingang</c>).
        /// </summary>
        public ZapfAnzeige Anzeige { get; init; }
    }

    /// <summary>
    /// Eine Zeile des Konstruktors, wie der Dialog sie führt (N11 (j)): Fenster in Stunden,
    /// wahlweise eine Zapfregel des Katalogs mit Anzahl der Vorgänge oder Volumen [l] und
    /// Zapftemperatur [°C], Verbraucher. Eine Zeile von <c>Tab_TwwKonstruktorzeile</c>
    /// (Schemaschritt T5, ZU25) — dieselben Felder, dieselbe Reihenfolge.
    /// </summary>
    internal sealed record KonstruktorzeileStand(double? BeginnH, double? EndeH, string Regel, double? Anzahl, double? VolumenL,
                                                 double? ZapftemperaturC, string Verbraucher);

    /// <summary>
    /// Der Bezug eines gespeicherten Konstruktortags (Folge (a) aus N21): <paramref name="TagGefunden"/>
    /// = die Katalogzeile, auf die die Projektzeile zeigt, steht noch; <paramref name="Bezugsmenge"/>
    /// und <paramref name="Bezugsart"/> wie an ihr gespeichert (beide leer = ohne Bezug; vor
    /// Schritt 124 fehlt die Bezugsart).
    /// </summary>
    internal sealed record KonstruktorBezugStand(bool TagGefunden, double? Bezugsmenge, ZapfBezugsart? Bezugsart)
    {
        /// <summary>Menge größer 0 samt Bezugsart — nur so skaliert die Auslegung den Tag.</summary>
        public bool Vollstaendig => Bezugsmenge is double m && m > 0 && Bezugsart.HasValue;
    }

    /// <summary>
    /// Die Laufangaben der Anzeige einer Bilanz (N9 (h), 4.0, 4.6): Temperatur der Literanzeige
    /// θ_Anzeige [°C] und Schwelle der Stundenzählung [kW] — beide nur für Kennzahlen, nie für die Reihe.
    /// <c>null</c> je Größe = die Einstellung, sonst die Vorgabe des Parametersatzes
    /// (<c>Zapfprofil.Anzeigetemperatur</c> bzw. <c>Zapfprofil.Stundenschwelle</c>).
    /// </summary>
    internal sealed record ZapfAnzeige(double? AnzeigetemperaturC, double? SchwelleKw);

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
    /// und der Satz als Kennung und Werte (N11 (k)); <see cref="Klartext"/> ist sein deutscher
    /// Wortlaut für Protokoll und Test.
    /// </summary>
    internal sealed record ZapfVerfuegbarkeit(bool Ja, ZapfVerfuegbarkeitsgrund Grund, ZapfSatz Satz)
    {
        /// <summary>Der deutsche Wortlaut (Protokoll, Test).</summary>
        public string Klartext => Satz?.Klartext ?? "";
    }
}
