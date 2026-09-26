using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Warum der Rechenweg eine Eingabe nicht annimmt (Konzept 2.2: kein stiller Rückfall).</summary>
    internal enum ZapfEingabefehler
    {
        /// <summary>Kalender der Klimaregion fehlt oder hat nicht 365 Tage; Wochentag außerhalb 0 … 6.</summary>
        KalenderUngueltig = 1,

        /// <summary>Die Projektgrößen (<c>Tab_TwwProjekt</c>) fehlen.</summary>
        ProjektFehlt = 2,

        /// <summary>Die Zone verweist auf eine Nutzungsart, die der Katalog nicht führt.</summary>
        NutzungsartFehlt = 3,

        /// <summary>Der gewählte Tagesgangsatz fehlt oder trägt nicht alle vier Tagtypen.</summary>
        TagesgangsatzFehlt = 4,

        /// <summary>Bezugsmenge fehlt, ist nicht positiv oder nicht endlich.</summary>
        BezugsmengeFehlt = 5,

        /// <summary>Ein Raster (Monate, Wochentage, Tagesgang, Niveau) ist falsch lang, negativ oder nicht endlich.</summary>
        RasterUngueltig = 6,

        /// <summary>Eine Temperaturdifferenz ist nicht positiv — die Umrechnung ist nicht definiert.</summary>
        TemperaturUngueltig = 7,

        /// <summary>Ein Parameter fehlt im Parametersatz (<see cref="ParametersatzException"/>).</summary>
        ParameterFehlt = 8,

        /// <summary>Der Messwert ist widersprüchlich oder nicht verrechenbar.</summary>
        MesswertUngueltig = 9,

        /// <summary>Die Zirkulationsangaben sind unvollständig oder ungültig.</summary>
        ZirkulationUngueltig = 10,

        /// <summary>Die Belegung einer Wohnung ist weder gesetzt noch aus dem Katalog bestimmbar.</summary>
        BelegungFehlt = 11,

        /// <summary>Der Tagesbedarf steht auf „manuell", der Wert fehlt oder ist ungültig.</summary>
        TagesbedarfUngueltig = 12,

        /// <summary>Die Zeitstruktur verteilt keine Zapfung: jeder Tag hat das Gewicht 0.</summary>
        KeineVerteilung = 13,

        /// <summary>Der Generator ist in dieser Datenbank nicht verfügbar — die Tww-Tabellen fehlen (3.2).</summary>
        NichtVerfuegbar = 14,

        /// <summary>
        /// Die Stochastik ist nicht rechenbar (4.4): Zapfkategorien fehlen oder sind ungültig (auch
        /// eine nicht endliche Rate der Ereignisse), die Einheiten sind nicht bestimmbar, die Zahl der
        /// Realisierungen liegt außerhalb 1 … Obergrenze (Jahresreihe <c>Jahresensemble.HOECHSTENS</c>,
        /// Bedarfstag <c>Zapfensemble.HOECHSTENS</c>), das Auslegungsensemble überschreitet die
        /// Einheitentage (<c>Zapfensemble.HOECHSTENS_EINHEITSTAGE</c>) oder das Jahr zum Seed trägt
        /// keine Zapfung.
        /// </summary>
        StochastikUngueltig = 15,

        /// <summary>
        /// Der Typtagweg des Jahresgangs ist nicht rechenbar (4.2, Stufe Z4b): Es sind keine
        /// Typtage eingespielt, das Paket führt die gewählte Klimazone oder Gebäudeart nicht
        /// oder nicht vollständig, ein Kennwert des Verfahrens fehlt, die Tagesmittel der
        /// Temperatur oder des Bedeckungsgrads fehlen, oder die Bezugsart der Nutzungsart ist
        /// keine Wohngröße (Personen oder Wohneinheiten).
        /// </summary>
        TyptageUngueltig = 16
    }

    /// <summary>
    /// Die benannte Ablehnung einer Eingabe: Grund, betroffene Zone (leer = Projekt) und
    /// Klartext für Protokoll und Test. Die Fassade fängt sie je Zone ab; die Zone trägt dann
    /// 0 und das Ergebnis nennt sie (Konzept 2.2, „kein stiller Rückfall").
    /// </summary>
    internal sealed class ZapfprofilEingabeException : Exception
    {
        internal ZapfprofilEingabeException(ZapfEingabefehler fehler, string zone, ZapfSatz satz)
            : base(satz?.Klartext ?? "")
        {
            Fehler = fehler;
            Zone = zone ?? "";
            Satz = satz;
        }

        /// <summary>Der Grund der Ablehnung.</summary>
        internal ZapfEingabefehler Fehler { get; }

        /// <summary>Die Zone; leer, wenn das Projekt betroffen ist.</summary>
        internal string Zone { get; }

        /// <summary>
        /// Der Satz der Ablehnung als Kennung und Werte (N11 (k)) — die Hülle baut daraus den Satz
        /// der Oberflächensprache; die Meldung der Ausnahme ist sein deutscher Wortlaut.
        /// </summary>
        internal ZapfSatz Satz { get; }

        /// <summary>Die Kennung des Satzes (<see cref="ZapfSatz.Kennung"/>); <c>null</c> ohne Satz.</summary>
        internal string Kennung => Satz?.Kennung;

        /// <summary>Die Werte des Satzes, sprachfrei und getrennt; <c>null</c> ohne Satz.</summary>
        internal IReadOnlyList<object> Argumente => Satz?.Werte;
    }

    /// <summary>
    /// Ein nicht blockierender Hinweis des Rechenwegs: Zone (leer = Projekt), Kennung der Art
    /// (<see cref="Code"/>, etwa <c>TAGESGANG_SUMME</c>) und der Satz als Kennung und Werte
    /// (<see cref="Satz"/>, N11 (k)); <see cref="Text"/> ist sein deutscher Wortlaut.
    /// </summary>
    internal sealed record ZapfHinweis(string Zone, string Code, ZapfSatz Satz)
    {
        /// <summary>Kennung „Parameter fehlt" eines Parameters, der die Rechnung nicht entscheidet (N7).</summary>
        internal const string PARAMETER_FEHLT = "PARAMETER_FEHLT";

        /// <summary>Der deutsche Wortlaut des Satzes (Protokoll, Test).</summary>
        public string Text => Satz?.Klartext ?? "";

        /// <summary>
        /// Stufe der Warnlogik (Z4): <c>true</c> = Warnung — ein Befund, der das Ergebnis fraglich
        /// macht (Doppelzählung ZU5, Bedarf außerhalb der Bandbreite, Messwert weit ab,
        /// Energieprobe); <c>false</c> = Hinweis (etwa eine Zirkulation, die groß gegen die Zapfung ist —
        /// in kleinen Mehrfamilienhäusern üblich). Entscheidet die Rechnung nie.
        /// </summary>
        public bool Warnung { get; init; }

        /// <summary>
        /// Der Hinweis, dass ein nicht rechnungsentscheidender Parameter fehlt: Er nennt den
        /// Schlüssel und die Folge; ein Rückfallwert wird nicht gesetzt (Konzept 2.1, N7).
        /// </summary>
        internal static ZapfHinweis ParameterFehlt(string schluessel, ZapfSatz folge)
            => new ZapfHinweis("", PARAMETER_FEHLT, ZapfSatz.Neu("HINWEIS_PARAMETER_FEHLT", schluessel ?? "", folge));

        /// <summary>Nimmt einen Hinweis nur auf, wenn derselbe noch nicht in der Liste steht.</summary>
        internal static void Einmal(ICollection<ZapfHinweis> liste, ZapfHinweis h)
        {
            if (liste != null && h != null && !liste.Contains(h)) liste.Add(h);
        }
    }

    /// <summary>
    /// <b>Die Schlüssel des Parametersatzes, die der Bilanzrechenweg liest</b> (Konzept 2.1,
    /// Kapitel 6 (a)). Die Schlüssel stehen im Code, die Werte nie — sie kommen aus
    /// <c>Tab_TwwParameter_STAMM</c>. Fehlt ein Schlüssel, den eine Rechnung braucht, lehnt
    /// sie benannt ab (<see cref="ParametersatzException"/>); die zwei Schwellen der Hinweise
    /// und die Vorgabe der Wohnfläche je WE für die Zonenfläche entscheiden die Rechnung nicht —
    /// fehlen sie, entfällt ihre Prüfung bzw. die Fläche, und der Hinweis
    /// <see cref="ZapfHinweis.PARAMETER_FEHLT"/> nennt den Schlüssel; einen Rückfallwert gibt es
    /// nicht (N7). Das Hinweisverhältnis der Zirkulation, die Anzeigetemperatur und die
    /// Stundenschwelle (Stufe Z4) sind Vorgaben der Anzeige und der Warnlogik: Fehlen sie, entfällt
    /// der Hinweis bzw. die Literanzeige oder die Zählung — ohne eigenen Hinweis.
    /// </summary>
    internal static class ZapfParameter
    {
        /// <summary>Jahresmittel des Kaltwassers der Bilanz [°C] (Vorgabe von θ̄_KW, 4.0, K4).</summary>
        internal const string KALTWASSER_MITTEL = "Kaltwasser.Bilanz.Mittel";

        /// <summary>Amplitude des Kaltwasser-Jahresgangs der Bilanz [K] (K4).</summary>
        internal const string KALTWASSER_AMPLITUDE = "Kaltwasser.Bilanz.Amplitude";

        /// <summary>Monat des Kaltwassermaximums (1 … 12) (K4).</summary>
        internal const string KALTWASSER_MONAT_MAXIMUM = "Kaltwasser.Bilanz.MonatMaximum";

        /// <summary>Flächenformel Wohnen: Koeffizient a [kWh/(m²·a)] (Verfahren der DIN V 18599-10, 4.1).</summary>
        internal const string WOHNEN_FORMEL_A = "DIN18599.Wohnen.a";

        /// <summary>Flächenformel Wohnen: Koeffizient b [kWh/(m⁴·a)].</summary>
        internal const string WOHNEN_FORMEL_B = "DIN18599.Wohnen.b";

        /// <summary>Flächenformel Wohnen: Untergrenze c [kWh/(m²·a)].</summary>
        internal const string WOHNEN_FORMEL_C = "DIN18599.Wohnen.c";

        /// <summary>Vorgabe der Wohnfläche je Wohneinheit [m²], wenn die Zone keine trägt.</summary>
        internal const string WOHNEN_FLAECHE_JE_WE = "Wohnen.FlaecheJeWe";

        /// <summary>Methode Anteil: Anteil a der Zirkulation am Tagesbedarf [-] (4.3).</summary>
        internal const string ZIRKULATION_ANTEIL = "Zirkulation.Anteil";

        /// <summary>Tägliche Laufzeit der Zirkulation [h] (Rahmen nach DVGW W 551, 4.3).</summary>
        internal const string ZIRKULATION_LAUFZEIT = "Zirkulation.Laufzeit";

        /// <summary>Vorgabe der Leitungslage (1 innerhalb, 2 außerhalb der Hülle).</summary>
        internal const string ZIRKULATION_LAGE = "Zirkulation.Lage";

        /// <summary>Flächenkennwert k_A der Lage 1 [kWh/(m²·a)] (Verfahren nach DIN V 4701-10).</summary>
        internal const string ZIRKULATION_KENNWERT_LAGE1 = "Zirkulation.Kennwert.Lage1";

        /// <summary>Flächenkennwert k_A der Lage 2 [kWh/(m²·a)].</summary>
        internal const string ZIRKULATION_KENNWERT_LAGE2 = "Zirkulation.Kennwert.Lage2";

        /// <summary>Methode Leitungslänge: Vorgabe des spezifischen Verlusts q' [W/m].</summary>
        internal const string ZIRKULATION_VERLUST_JE_METER = "Zirkulation.VerlustJeMeter";

        /// <summary>Rückfrageschwelle Messwert gegen Katalogwert [-] (Hinweis, Methodikkonzept 2.2).</summary>
        internal const string MESSWERT_RUECKFRAGESCHWELLE = "Zapfprofil.Messwert.Rueckfrageschwelle";

        /// <summary>Warnschwelle der Formvektor-Summe [-] (Hinweis, INEKON-Setzung, 2.4).</summary>
        internal const string FORMVEKTOR_WARNSCHWELLE = "Zapfprofil.Formvektor.Warnschwelle";

        /// <summary>
        /// Hinweisverhältnis der Zirkulation [-] (Warnlogik Z4, INEKON-Setzung, freier Paketteil):
        /// Verliert die Zirkulation mehr als dieses Vielfache der Zapfung, nennt es ein Hinweis; ohne
        /// Parameter kein Hinweis.
        /// </summary>
        internal const string ZIRKULATION_HINWEISVERHAELTNIS = "Zapfprofil.Zirkulation.Hinweisverhaeltnis";

        /// <summary>
        /// Vorgabe der Temperatur der Literanzeige θ_Anzeige [°C] (4.0, 4.6; INEKON-Setzung, freier
        /// Paketteil) — gilt, wenn weder der Dialog noch die Einstellung eine nennt; nur für Kennzahlen.
        /// </summary>
        internal const string ANZEIGETEMPERATUR = "Zapfprofil.Anzeigetemperatur";

        /// <summary>
        /// Vorgabe der Schwelle der Stundenzählung [kW] (4.6; INEKON-Setzung, freier Paketteil) — gilt,
        /// wenn weder der Dialog noch die Einstellung eine nennt.
        /// </summary>
        internal const string STUNDENSCHWELLE = "Zapfprofil.Stundenschwelle";

        /// <summary>
        /// Untere Grenze des Bands der synthetischen Spitze [-] (Stufe Z5, Kapitel 7 Zeile Z5
        /// Kennzahl (b); INEKON-Setzung, freier Paketteil): das Perzentil der gerechneten
        /// Dauerlinie, unter dem die gemessene Spitze als „unterhalb" gilt. Vorgabe 0,95
        /// (Anwenderentscheid ZU35, aus der Bandanalyse des zweiten Validierungslaufs).
        /// </summary>
        internal const string VALIDIERUNG_BAND_UNTEN = "Zapfprofil.Validierung.Band.Unten";

        /// <summary>
        /// Obere Grenze des Bands der synthetischen Spitze [-] (Stufe Z5; INEKON-Setzung, freier
        /// Paketteil): das Perzentil, über dem die gemessene Spitze als „oberhalb" gilt — dann
        /// überschätzt die Rechnung die Spitze nicht, sie unterschätzt sie. Vorgabe 0,999
        /// (Anwenderentscheid ZU35).
        /// </summary>
        internal const string VALIDIERUNG_BAND_OBEN = "Zapfprofil.Validierung.Band.Oben";

        /// <summary>
        /// Kleinste Zahl der Einheiten einer Anlage, ab der das Band bewertet wird [-] (Stufe Z5;
        /// INEKON-Setzung, freier Paketteil; Anwenderentscheid ZU35): darunter misst ein Quantilband
        /// der Dauerlinie die Ziehung einer Stunde, und die Bandkennzahl heißt „nicht bewertbar"
        /// (gelb). Einheiten = Summe der Bezugsmengen der Zonen, wie für √N. Vorgabe 10.
        /// </summary>
        internal const string VALIDIERUNG_BAND_MINDEST_EINHEITEN = "Zapfprofil.Validierung.Band.MindestEinheiten";

        /// <summary>
        /// Schwelle des Formabgleichs des Tagesgangs [-] (Stufe Z5, Kennzahl (d); INEKON-Setzung,
        /// freier Paketteil): die mittlere absolute Abweichung der 24 Stundenanteile je Tagtyp, ab
        /// der die Form als abweichend gilt. Vorgabe 0,01 — ein Prozentpunkt je Stunde im Mittel.
        /// </summary>
        internal const string VALIDIERUNG_FORMSCHWELLE = "Zapfprofil.Validierung.Formschwelle";

        /// <summary>
        /// Höchster zugelassener Anteil gefüllter Lücken einer Messreihe [-] (Stufe Z5;
        /// INEKON-Setzung, freier Paketteil): darüber lehnt <c>Messreihenleser</c> die Datei ab,
        /// darunter nennt er die Lücken. Vorgabe 0,05.
        /// </summary>
        internal const string VALIDIERUNG_LUECKENANTEIL = "Zapfprofil.Validierung.Lueckenanteil";

        /// <summary>
        /// Kürzeste Messreihe für einen Kalibriervorschlag der Nichtwohn-Parameter [d] (Stufe Z5;
        /// INEKON-Setzung, freier Paketteil): Unter dieser Zahl von Tagen trägt der mittlere
        /// Tagesgang je Tagtyp zu wenige Tage, und der Vorschlag wird benannt abgelehnt. Vorgabe 30.
        /// </summary>
        internal const string VALIDIERUNG_KALIBRIERUNG_TAGE = "Zapfprofil.Validierung.Kalibrierung.MindestTage";
    }

    /// <summary>
    /// <b>Der Eingang des Bilanzrechenwegs</b> (Konzept 2.1, Zeile „Eingang"; 2.3): die Zonen
    /// und Projektgrößen aus dem Arbeitsstand (<see cref="ZapfprofilStand"/>, Z0), der Kalender
    /// der Klimaregion (<see cref="WochentagJan1"/> Montag = 0 … Sonntag = 6, 365 Kennzeichen
    /// <see cref="We"/> für Wochenende und Feiertag, A6) und der Parametersatz.
    ///
    /// <para><b>Wahlfrei</b>: <see cref="Tagesgangsaetze"/> (nur nötig, wenn eine Zone einen
    /// eigenen Satz wählt), <see cref="BelegungJeRaumzahl"/> (Belegung einer Wohnung nach
    /// Raumzahl aus dem DIN-4708-Katalog, Schlüssel wie <c>Tab_TwwDin4708Wert_STAMM.Schluessel</c>),
    /// <see cref="AnzeigetemperaturC"/> (Literanzeige der Kennzahlen, 4.6) und
    /// <see cref="SchwelleKw"/> (Stunden über einer Schwelle, 4.6). Kein Feld kennt eine
    /// Datenbank oder einen Dienst.</para>
    /// </summary>
    internal sealed record Zapfprofileingang
    {
        /// <summary>Die Zonen des Projekts in ihrer Reihenfolge.</summary>
        public IReadOnlyList<ZonenStand> Zonen { get; init; } = new ZonenStand[0];

        /// <summary>Weiche und gebäudeweite Größen; Pflicht.</summary>
        public ProjektStand Projekt { get; init; }

        /// <summary>Wochentag des 1. Januar, Montag = 0 … Sonntag = 6 (Bestandskonvention F3).</summary>
        public int WochentagJan1 { get; init; }

        /// <summary>365 Kennzeichen „Wochenende oder Feiertag" der Klimaregion.</summary>
        public bool[] We { get; init; }

        /// <summary>Der Parametersatz der Katalogversion; Pflicht.</summary>
        public Parametersatz Parameter { get; init; }

        /// <summary>Tagesgangsätze für Zonen mit eigenem Satz; leer, wenn keine Zone einen wählt.</summary>
        public IReadOnlyList<Tagesgangsatz> Tagesgangsaetze { get; init; } = new Tagesgangsatz[0];

        /// <summary>Belegung (Personen) je Raumzahl; <c>null</c> = keine Katalogbelegung verfügbar.</summary>
        public IReadOnlyDictionary<string, double> BelegungJeRaumzahl { get; init; }

        /// <summary>Temperatur der Literanzeige [°C]; <c>null</c> = keine Literanzeige.</summary>
        public double? AnzeigetemperaturC { get; init; }

        /// <summary>Schwelle für „Stunden über" [kW]; <c>null</c> = keine Zählung.</summary>
        public double? SchwelleKw { get; init; }

        /// <summary>
        /// Hinweise, die schon der Eingang kennt (etwa eine ungültige Einstellung der Anzeige, Z4) —
        /// der Rechenweg stellt sie seinen eigenen voran.
        /// </summary>
        public IReadOnlyList<ZapfHinweis> Vorhinweise { get; init; } = new ZapfHinweis[0];

        /// <summary>
        /// Die Netzverluste des Projekts (Einstellungen, in % oder kWh/a — hier zählt nur, ob sie
        /// gesetzt sind): Größer 0 und zugleich eine gerechnete Zirkulation ergibt den Hinweis
        /// <c>NETZVERLUST_UND_ZIRKULATION</c> (Konzept 9, ZU5). Die Netzverlustverteilung selbst
        /// bleibt unberührt. 0 = keine Netzverluste oder unbekannt.
        /// </summary>
        public double NetzverlusteProjekt { get; init; }

        /// <summary>
        /// Die Zapfkategorien des Katalogs (T2, 4.4) für die Nutzungsarten der Zonen — gelesen von
        /// <c>ZapfprofilCtrl.Eingang</c> aus <c>Tab_TwwZapfkategorie_STAMM</c> je Nutzungsart in der
        /// Reihenfolge des Katalogs; leer, solange der Katalog keine trägt oder die Datenbank die
        /// Tabelle nicht führt (Stand vor 115). Gebraucht nur auf dem stochastischen Weg — der Jahresreihe
        /// (<see cref="ProjektStand.JahresreiheStochastisch"/>) und des Auslegungsensembles; fehlen die
        /// Kategorien einer Nutzungsart dort, lehnt die Zone benannt ab.
        /// </summary>
        public IReadOnlyList<Zapfkategorie> Zapfkategorien { get; init; } = new Zapfkategorie[0];

        /// <summary>
        /// <b>Die Weiche des Jahresgangs auf die eingespielten Typtage</b> (T3, 4.2, 5.3; Stufe
        /// Z4b): <c>null</c> = der Jahresgang rechnet wie im Bestand über den Formvektor
        /// (Monats-, Wochen- und Tagesfaktoren des Katalogs). Gesetzt rechnet er über die Typtage
        /// des lizenzierten Anwenders — Jahreszeit aus der Tagesmitteltemperatur, Tagart aus dem
        /// Zapfkalender, Bewölkung aus dem Tagesmittel des Bedeckungsgrads (4.2). Sind keine
        /// Typtage eingespielt oder führt das Paket die gewählte Zone oder Gebäudeart nicht,
        /// lehnt die Zone <b>benannt</b> ab (<see cref="ZapfEingabefehler.TyptageUngueltig"/>) —
        /// nie still zurück auf den Bestandsweg. Die AUSLEGUNG bleibt unberührt (Wochenreihe,
        /// Bedarfstag, Summenlinie): Die Referenzlastprofile sind nicht für Auslegungsspitzen
        /// gedacht.
        /// </summary>
        public Typtaganbindung Typtage { get; init; }

        /// <summary>Der Eingang aus einem Arbeitsstand, dem Kalender und dem Parametersatz.</summary>
        internal static Zapfprofileingang Aus(ZapfprofilStand stand, int wochentagJan1, bool[] we, Parametersatz ps)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            return new Zapfprofileingang
            {
                Zonen = stand.Zonen ?? new ZonenStand[0],
                Projekt = stand.Projekt,
                WochentagJan1 = wochentagJan1,
                We = we,
                Parameter = ps
            };
        }
    }
}
