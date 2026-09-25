using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Schlüssel des Parametersatzes, die die Auslegung liest</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 2.1, 4.0, 4.5, 4.7, Kapitel 6 (a)). Die Schlüssel stehen im Code, die
    /// Werte nie — sie kommen aus <c>Tab_TwwParameter_STAMM</c>. Fehlt ein Schlüssel, den eine
    /// Rechnung braucht, lehnt sie benannt ab (<see cref="ParametersatzException"/>); ein
    /// Parameter, der die Rechnung nicht entscheidet (die Zahl der Wertepaare, die Schwellen der
    /// Warnliste), meldet nur <see cref="ZapfHinweis.PARAMETER_FEHLT"/>, einen Rückfallwert gibt
    /// es nicht (N7).
    /// </summary>
    internal static class ZapfAuslegungParameter
    {
        // --- Temperaturen (4.0) ------------------------------------------------------------

        /// <summary>Feste Kaltwassertemperatur der Auslegung θ_KW,Auslegung [°C] (K4).</summary>
        internal const string KALTWASSER_AUSLEGUNG = "A100.Kaltwasser.Auslegung";

        /// <summary>Mindesttemperatur des Speichers nach DVGW W 551 [°C]; Vorgabe von θ_Speicher bei Großanlage (4.0).</summary>
        internal const string W551_MINDESTTEMPERATUR = "W551.Mindesttemperatur";

        /// <summary>
        /// Vorgabe von θ_Speicher [°C] außerhalb der Großanlage und des Schnellpfads (INEKON-Setzung;
        /// N10) — die Mindesttemperatur nach DVGW W 551 gilt nur bei Großanlage.
        /// </summary>
        internal const string SPEICHERTEMPERATUR_VORGABE = "Speicherauslegung.Speichertemperatur_Vorgabe";

        // --- Summenlinie nach DIN EN 12831-3 mit A100/A1 (4.5 a) --------------------------

        /// <summary>Ladungsfaktor f_l [-].</summary>
        internal const string LADUNGSFAKTOR = "A100.Ladungsfaktor";

        /// <summary>Vorgabe der Sensorhöhe h_sensor/h_sto [-], wenn das Projekt keine trägt.</summary>
        internal const string SENSORHOEHE = "A100.Sensorhoehe";

        /// <summary>Mischwassertemperatur an der Zapfstelle θ_w,draw [°C] für Q_sto,min des gemischten Speichers.</summary>
        internal const string MISCHWASSERTEMPERATUR = "A100.Mischwassertemperatur";

        /// <summary>Zeitverzögerung t_lag des Erzeugers und der Verteilung [min].</summary>
        internal const string VERZOEGERUNG = "A100.Verzoegerung";

        /// <summary>Wärmedurchgangskoeffizient U eines Übertragers aus Stahl [W/(m²·K)], wenn das Projekt kein U·A nennt.</summary>
        internal const string UEBERTRAGER_U_STAHL = "A100.Uebertrager.U.Stahl";

        /// <summary>Wärmedurchgangskoeffizient U eines Übertragers aus Edelstahl [W/(m²·K)].</summary>
        internal const string UEBERTRAGER_U_EDELSTAHL = "A100.Uebertrager.U.Edelstahl";

        /// <summary>Heizmittelübertemperatur Δθ_Ü des Übertragers [K].</summary>
        internal const string UEBERTRAGER_UEBERTEMPERATUR = "A100.Uebertrager.Uebertemperatur";

        /// <summary>Schätzformel der Übertragerfläche am Kessel (NA.1), A_HE = Steigung · V + Achsabschnitt: Steigung [m²/l].</summary>
        internal const string UEBERTRAGERFLAECHE_KESSEL_STEIGUNG = "A100.Uebertragerflaeche.Kessel.Steigung";

        /// <summary>Schätzformel der Übertragerfläche am Kessel (NA.1): Achsabschnitt [m²].</summary>
        internal const string UEBERTRAGERFLAECHE_KESSEL_ACHSABSCHNITT = "A100.Uebertragerflaeche.Kessel.Achsabschnitt";

        /// <summary>Schätzformel der Übertragerfläche an der Wärmepumpe (NA.2): Steigung [m²/l].</summary>
        internal const string UEBERTRAGERFLAECHE_WAERMEPUMPE_STEIGUNG = "A100.Uebertragerflaeche.Waermepumpe.Steigung";

        /// <summary>Schätzformel der Übertragerfläche an der Wärmepumpe (NA.2): Achsabschnitt [m²].</summary>
        internal const string UEBERTRAGERFLAECHE_WAERMEPUMPE_ACHSABSCHNITT = "A100.Uebertragerflaeche.Waermepumpe.Achsabschnitt";

        /// <summary>Der Schlüssel des U-Werts zum Werkstoff des Übertragers.</summary>
        internal static string UebertragerU(ZapfUebertragerwerkstoff werkstoff)
            => werkstoff == ZapfUebertragerwerkstoff.Edelstahl ? UEBERTRAGER_U_EDELSTAHL : UEBERTRAGER_U_STAHL;

        /// <summary>Die Schlüssel der Schätzformel (Steigung, Achsabschnitt) zur Erzeugerart: Kessel NA.1, Wärmepumpe NA.2.</summary>
        internal static (string Steigung, string Achsabschnitt) Uebertragerflaeche(ZapfErzeugerart art)
            => art == ZapfErzeugerart.Waermepumpe
                ? (UEBERTRAGERFLAECHE_WAERMEPUMPE_STEIGUNG, UEBERTRAGERFLAECHE_WAERMEPUMPE_ACHSABSCHNITT)
                : (UEBERTRAGERFLAECHE_KESSEL_STEIGUNG, UEBERTRAGERFLAECHE_KESSEL_ACHSABSCHNITT);

        /// <summary>
        /// Koeffizient k_τ der Zeitkonstante nach A1 [min·W/kJ]: <c>τ = m · c_w / (U·A) · k_τ</c> mit
        /// m [kg], c_w [kJ/(kg·K)], U·A [W/K] — der Koeffizient der A1, keine Einheitenumrechnung
        /// von Wh; nur informativ.
        /// </summary>
        internal const string ZEITKONSTANTE_KOEFFIZIENT = "A100.Zeitkonstante.Koeffizient";

        /// <summary>Anwendungsgrenze des Vereinfachungsverfahrens der A100 [WE].</summary>
        internal const string VEREINFACHUNG_GRENZE = "A100.Vereinfachung.Anwendungsgrenze";

        /// <summary>Setzung des Vereinfachungsverfahrens: Sensorhöhe h_sensor/h_sto [-].</summary>
        internal const string VEREINFACHUNG_SENSORHOEHE = "A100.Vereinfachung.Sensorhoehe";

        /// <summary>Setzung des Vereinfachungsverfahrens: Speichertemperatur [°C].</summary>
        internal const string VEREINFACHUNG_SPEICHERTEMPERATUR = "A100.Vereinfachung.Speichertemperatur";

        /// <summary>Zahl der Punkte der Wertepaarkurve [-] (INEKON-Setzung; entscheidet den Auslegungspunkt nicht).</summary>
        internal const string WERTEPAARE = "Summenlinie.Wertepaare";

        // --- DIN-4708-Kennzahl (4.5 c) ------------------------------------------------------

        /// <summary>Koeffizient a_1 der Kennzahl [1/h].</summary>
        internal const string DIN4708_A1 = "DIN4708.a1";

        /// <summary>Koeffizient a_2 der Kennzahl [1/h].</summary>
        internal const string DIN4708_A2 = "DIN4708.a2";

        /// <summary>Zapfperiode z [h].</summary>
        internal const string DIN4708_Z = "DIN4708.z";

        /// <summary>Belegung der Einheitswohnung p_b [Personen].</summary>
        internal const string DIN4708_PB = "DIN4708.p_b";

        /// <summary>Zapfstellenbedarf der Einheitswohnung w_b [Wh].</summary>
        internal const string DIN4708_WB_ZAPFSTELLE = "DIN4708.w_b";

        /// <summary>Bedarf W_b der Formel des Wärmebedarfs W_z [Wh].</summary>
        internal const string DIN4708_WB_BEDARF = "DIN4708.W_b";

        /// <summary>Kappung der Fehlerfunktion: K(u) = erf(u) für u unter der Kappung, sonst 1 [-].</summary>
        internal const string DIN4708_KAPPUNG = "DIN4708.Kappung";

        /// <summary>Zahl der Zapfblöcke des DIN-4708-Profils [-].</summary>
        internal const string DIN4708_PROFIL_BLOECKE = "DIN4708.Profil.Bloecke";

        /// <summary>Präfix eines Zapfblocks: <c>DIN4708.Profil.Block.{k}.Beginn|Dauer|Anteil</c>.</summary>
        internal const string DIN4708_PROFIL_BLOCK = "DIN4708.Profil.Block.";

        // --- Speicherauslegung nach Vorlage V4 (4.7) --------------------------------------

        /// <summary>Vorgabe des nutzbaren Speicheranteils f_nutz [-] (INEKON-Setzung aus V4).</summary>
        internal const string NUTZANTEIL = "Speicherauslegung.Nutzanteil";

        /// <summary>Vorgabe des Sicherheitszuschlags z_S [-] (INEKON-Setzung aus V4).</summary>
        internal const string ZUSCHLAG = "Speicherauslegung.Zuschlag";

        /// <summary>Vorgabe der Länge des Ladefensters t_F [h].</summary>
        internal const string LADEFENSTER_LAENGE = "Speicherauslegung.Ladefenster.Laenge";

        /// <summary>Vorgabe des Beginns des Ladefensters t_B [h].</summary>
        internal const string LADEFENSTER_BEGINN = "Speicherauslegung.Ladefenster.Beginn";

        /// <summary>
        /// Gültigkeitsgrenze N_GLF des Gleichzeitigkeitsverfahrens [-] (INEKON-Setzung, N10): über
        /// ihr steht das Verfahren außerhalb des Bands; der Wert ist an Vorlage und Summenlinie
        /// festzulegen — kein fester Wert im Code.
        /// </summary>
        internal const string GLF_GUELTIGKEITSGRENZE = "Speicherauslegung.GLF_Gueltigkeitsgrenze";

        /// <summary>Klassischer Faustwert v_klass [l/(P·d)] (nur nachrichtlich).</summary>
        internal const string KLASSISCH_LITER = "Speicherauslegung.Klassisch.LiterJePersonTag";

        /// <summary>Bezugsspreizung Δθ_ref des klassischen Faustwerts [K].</summary>
        internal const string KLASSISCH_SPREIZUNG = "Speicherauslegung.Klassisch.Spreizung";

        /// <summary>Warnfaktor: klassischer Faustwert über Warnfaktor · V_max des Bands [-].</summary>
        internal const string KLASSISCH_WARNFAKTOR = "Speicherauslegung.Klassisch.Warnfaktor";

        /// <summary>Raster der Rundung über dem Ende der Nenninhaltsliste [l] (INEKON-Setzung aus V4).</summary>
        internal const string NENNINHALT_RASTER = "Speicherauslegung.Nenninhalt.Raster";

        /// <summary>
        /// Präfix der Vorgabe der Nenninhaltsliste [l]: <c>Speicherauslegung.Nenninhalt.Liste.{k}</c>
        /// (k = 1 … n, INEKON-Setzung, neutral). Die Einstellung <c>Zapfprofil.Nenninhalte</c> geht
        /// ihr vor (Stufe Z2, Gruppe 2); die Werte stehen im Katalog, nie im Code.
        /// </summary>
        internal const string NENNINHALT_LISTE = "Speicherauslegung.Nenninhalt.Liste.";

        // --- Großanlage nach DVGW W 551 (4.7) ------------------------------------------------

        /// <summary>Schwelle des Speichervolumens einer Großanlage [l].</summary>
        internal const string W551_GROSS_VOLUMEN = "W551.Grossanlage.Speichervolumen";

        /// <summary>Schwelle des Leitungsinhalts einer Großanlage [l].</summary>
        internal const string W551_GROSS_LEITUNG = "W551.Grossanlage.Leitungsinhalt";

        /// <summary>Leitungsinhalt je Meter Zirkulationsleitung [l/m], wenn das Projekt keinen Inhalt nennt.</summary>
        internal const string W551_INHALT_JE_METER = "W551.Leitungsinhalt.JeMeter";

        // --- Konstruktor (NA.5.2.3) ----------------------------------------------------------

        /// <summary>Präfix einer Zapfregel: <c>Konstruktor.Regel.{Name}.Volumenstrom|Dauer|Temperatur</c>.</summary>
        internal const string KONSTRUKTOR_REGEL = "Konstruktor.Regel.";

        /// <summary>
        /// Der Wert eines Parameters, der die Rechnung nicht entscheidet: fehlt er, nennt ein
        /// Hinweis den Schlüssel einmal, und der Aufrufer lässt die Prüfung weg (N7).
        /// </summary>
        internal static double? Wahlweise(Parametersatz ps, string schluessel, ZapfSatz folge,
                                          ICollection<Auslegungshinweis> hinweise)
        {
            if (ps != null && ps.Enthaelt(schluessel)) return ps.Wert(schluessel);
            Auslegungshinweis.Einmal(hinweise, Auslegungshinweis.ParameterFehlt(schluessel, folge));
            return null;
        }

        /// <summary>
        /// Ein Projektwert oder, wenn er fehlt, der Parameter; beides mit Herkunft im Protokoll.
        /// <paramref name="was"/> benennt die Größe in einer Ablehnung (ein Begriff, N11 (k)).
        /// </summary>
        internal static double ProjektOderParameter(double? projektwert, string schluessel, Parametersatz ps,
                                                    Herkunftsprotokoll prot, string feld, string einheit, ZapfSatz was)
        {
            if (projektwert.HasValue)
            {
                Auslegungspruefung.Endlich(projektwert.Value, was);
                prot?.Vermerken("", feld, projektwert.Value, einheit, Wertstatus.Ueberschrieben, null);
                return projektwert.Value;
            }
            ZapfParameterwert pw = ps.Lies(schluessel);
            prot?.Vermerken("", feld, pw.Wert, einheit, Wertstatus.Vorgabe, pw.Herkunft,
                            ZapfSatz.Neu("HERKUNFT_PARAMETER", schluessel));
            return pw.Wert;
        }
    }

    /// <summary>
    /// Die Erzeugerart am Speicher — wählt die Schätzformel der Übertragerfläche (A100: Kessel
    /// NA.1, Wärmepumpe NA.2). Eine Laufangabe des Auslegungseingangs (N10), keine Spalte.
    /// </summary>
    internal enum ZapfErzeugerart
    {
        /// <summary>Kessel (Schätzformel NA.1).</summary>
        Kessel = 1,

        /// <summary>Wärmepumpe (Schätzformel NA.2).</summary>
        Waermepumpe = 2
    }

    /// <summary>Der Werkstoff des Übertragers — wählt den U-Wert (N10). Eine Laufangabe des Auslegungseingangs.</summary>
    internal enum ZapfUebertragerwerkstoff
    {
        /// <summary>Stahl.</summary>
        Stahl = 1,

        /// <summary>Edelstahl.</summary>
        Edelstahl = 2
    }

    /// <summary>Warum die Auslegung eine Eingabe nicht annimmt (Konzept 2.2: kein stiller Rückfall).</summary>
    internal enum ZapfAuslegungsfehler
    {
        /// <summary>Der Bedarfstag fehlt, ist leer oder trägt ein ungültiges Ereignis.</summary>
        BedarfstagUngueltig = 1,

        /// <summary>Die Wochenreihe fehlt oder trägt nicht 168 Stunden.</summary>
        WochenreiheUngueltig = 2,

        /// <summary>Eine Temperaturdifferenz ist nicht positiv.</summary>
        TemperaturUngueltig = 3,

        /// <summary>Eine Größe ist negativ, nicht endlich oder außerhalb ihres Bereichs.</summary>
        GroesseUngueltig = 4,

        /// <summary>Die Wohnungstabelle fehlt — die DIN-4708-Kennzahl ist nicht rechenbar, nie geschätzt.</summary>
        WohnungstabelleFehlt = 5,

        /// <summary>Die Belegung oder Ausstattung eines Wohnungstyps ist nicht bestimmbar.</summary>
        WohnungstypUngueltig = 6,

        /// <summary>Weder Erzeuger- noch Übertragerleistung ist bestimmbar.</summary>
        LeistungFehlt = 7,

        /// <summary>Der Nachweis findet kein Volumen (die Suche endet ohne Ergebnis).</summary>
        KeinVolumen = 8,

        /// <summary>Ein Parameter fehlt im Parametersatz.</summary>
        ParameterFehlt = 9,

        /// <summary>Das Verfahren ist für diese Zone oder Topologie nicht gültig.</summary>
        NichtGueltig = 10,

        /// <summary>Der Übertrager ist nicht bestimmbar: Werkstoff (U) oder Erzeugerart (Schätzformel) fehlt.</summary>
        UebertragerUnbestimmt = 11
    }

    /// <summary>
    /// Die benannte Ablehnung einer Auslegungseingabe: Grund und Satz als Kennung und Werte (N11 (k));
    /// die Meldung ist sein deutscher Wortlaut.
    /// </summary>
    internal sealed class ZapfAuslegungException : Exception
    {
        internal ZapfAuslegungException(ZapfAuslegungsfehler fehler, ZapfSatz satz) : base(satz?.Klartext ?? "")
        {
            Fehler = fehler;
            Satz = satz;
        }

        /// <summary>Der Grund der Ablehnung.</summary>
        internal ZapfAuslegungsfehler Fehler { get; }

        /// <summary>Der Satz der Ablehnung als Kennung und Werte.</summary>
        internal ZapfSatz Satz { get; }
    }

    /// <summary>
    /// Ein Eintrag der Warnliste der Auslegung (4.7): Kennung der Art (<see cref="Code"/>), der Satz
    /// als Kennung und Werte (<see cref="Satz"/>, N11 (k)) und <see cref="Warnung"/> für die
    /// hervorgehobenen Einträge. Nie blockierend. <see cref="Text"/> ist der deutsche Wortlaut.
    /// </summary>
    internal sealed record Auslegungshinweis(string Code, ZapfSatz Satz, bool Warnung = false)
    {
        /// <summary>Der deutsche Wortlaut des Satzes (Protokoll, Test).</summary>
        public string Text => Satz?.Klartext ?? "";

        /// <summary>
        /// Die benannte Ablehnung hinter dem Hinweis (Kennung und sprachfreie Werte, etwa fehlende
        /// Zapfkategorien); sonst <c>null</c>. Die Hülle baut daraus den Satz der Oberflächensprache.
        /// </summary>
        public ZapfAblehnung Ablehnung { get; init; }

        /// <summary>Der Hinweis, dass ein nicht rechnungsentscheidender Parameter fehlt (N7).</summary>
        internal static Auslegungshinweis ParameterFehlt(string schluessel, ZapfSatz folge)
            => new Auslegungshinweis(ZapfHinweis.PARAMETER_FEHLT, ZapfSatz.Neu("HINWEIS_PARAMETER_FEHLT", schluessel ?? "", folge));

        /// <summary>Der Hinweis „Parameter fehlt" aus der benannten Ablehnung des Parametersatzes und der Folge.</summary>
        internal static Auslegungshinweis ParameterFehlt(ParametersatzException ex, ZapfSatz folge)
            => new Auslegungshinweis(ZapfHinweis.PARAMETER_FEHLT, ZapfSatz.Neu("AUSHINWEIS_GRUND_UND_FOLGE", ex.Satz, folge));

        /// <summary>Nimmt einen Hinweis nur auf, wenn derselbe noch nicht in der Liste steht.</summary>
        internal static void Einmal(ICollection<Auslegungshinweis> liste, Auslegungshinweis h)
        {
            if (liste != null && h != null && !liste.Contains(h)) liste.Add(h);
        }
    }

    /// <summary>
    /// Eine auto/manuell-Größe (4.7, Muster der Vorlage V4): der Vorschlag des Verfahrens und der
    /// Wert des Anwenders stehen nebeneinander; angesetzt ist der Vorschlag, solange „auto" gilt
    /// oder kein manueller Wert vorliegt.
    /// </summary>
    internal readonly record struct Schaetzwert(bool Auto, double Vorschlag, double? Manuell)
    {
        /// <summary>Der angesetzte Wert.</summary>
        internal double Angesetzt => Auto || Manuell is null ? Vorschlag : Manuell.Value;

        /// <summary>Gilt der manuelle Wert?</summary>
        internal bool IstManuell => !Auto && Manuell is not null;
    }

    /// <summary>Woher die Speichertemperatur einer Topologiegruppe kommt (4.0, N10).</summary>
    internal enum Speichertemperaturquelle
    {
        /// <summary>Projektwert <c>Speicher_C</c>.</summary>
        Projekt = 1,

        /// <summary>Großanlage nach DVGW W 551: Mindesttemperatur (Parameter).</summary>
        Grossanlage = 2,

        /// <summary>Schnellpfad: Setzung des Vereinfachungsverfahrens der A100 (Parameter).</summary>
        Schnellpfad = 3,

        /// <summary>Sonst: Vorgabe der Speicherauslegung (Parameter, INEKON-Setzung).</summary>
        Vorgabe = 4
    }

    /// <summary>
    /// <b>Die eine Speichertemperatur einer Topologiegruppe</b> (4.0, N10): Summenlinie, V_DIN,
    /// Verfahrensvergleich, Band und Reihenfolge rechnen mit demselben θ_Speicher.
    ///
    /// <code>
    /// θ_Speicher = Speicher_C des Projekts                       wenn gesetzt
    ///            = W551.Mindesttemperatur                        bei Großanlage
    ///            = A100.Vereinfachung.Speichertemperatur         im Schnellpfad (Wohnen bis zur Anwendungsgrenze)
    ///            = Speicherauslegung.Speichertemperatur_Vorgabe  sonst
    /// </code>
    ///
    /// <para>Die Großanlage geht dem Schnellpfad vor: Die Setzung des Vereinfachungsverfahrens
    /// hält die Mindesttemperatur nicht zugesichert ein, eine Großanlage rechnet deshalb das
    /// Vollverfahren. Jede Wahl steht mit Herkunft im Protokoll (<c>Auslegung.SpeicherC</c>).</para>
    /// </summary>
    internal sealed record Speichertemperaturwahl(double SpeicherC, Speichertemperaturquelle Quelle, bool Schnellpfad)
    {
        /// <summary>
        /// Wählt die Temperatur. <paramref name="grossanlage"/>: die Gruppe ist als Großanlage
        /// erkannt; <paramref name="schnellpfadGilt"/>: der Schnellpfad ist zulässig (Wohnen bis
        /// zur Anwendungsgrenze, keine Sensorhöhe im Projekt). Ein fehlender Parameter der
        /// gewählten Quelle lehnt benannt ab.
        /// </summary>
        internal static Speichertemperaturwahl Waehlen(ProjektStand p, Parametersatz ps, bool grossanlage, bool schnellpfadGilt,
                                                     Herkunftsprotokoll prot)
        {
            const string feld = ZapfFeld.AUSLEGUNG_SPEICHER_C;
            if (p?.SpeicherC != null)
                return new Speichertemperaturwahl(
                    ZapfAuslegungParameter.ProjektOderParameter(p.SpeicherC, ZapfAuslegungParameter.W551_MINDESTTEMPERATUR, ps,
                                                                prot, feld, "°C", ZapfSatz.Neu("BEGRIFF_SPEICHERTEMPERATUR")),
                    Speichertemperaturquelle.Projekt, false);
            string schluessel;
            Speichertemperaturquelle quelle;
            if (grossanlage)
            {
                schluessel = ZapfAuslegungParameter.W551_MINDESTTEMPERATUR;
                quelle = Speichertemperaturquelle.Grossanlage;
            }
            else if (schnellpfadGilt)
            {
                schluessel = ZapfAuslegungParameter.VEREINFACHUNG_SPEICHERTEMPERATUR;
                quelle = Speichertemperaturquelle.Schnellpfad;
            }
            else
            {
                schluessel = ZapfAuslegungParameter.SPEICHERTEMPERATUR_VORGABE;
                quelle = Speichertemperaturquelle.Vorgabe;
            }
            ZapfParameterwert pw = ps.Lies(schluessel);
            Auslegungspruefung.Endlich(pw.Wert, ZapfSatz.Neu("BEGRIFF_SPEICHERTEMPERATUR_PARAMETER", schluessel));
            prot?.Vermerken("", feld, pw.Wert, "°C", Wertstatus.Vorgabe, pw.Herkunft,
                            Parametervermerk(schluessel, quelle));
            return new Speichertemperaturwahl(pw.Wert, quelle, quelle == Speichertemperaturquelle.Schnellpfad);
        }

        /// <summary>
        /// Der Vermerk zur gewählten Quelle der Speichertemperatur: der Parameterschlüssel, und wo
        /// die Großanlage oder der Schnellpfad ihn wählt, eine eigene Kennung (N13 (b)).
        /// </summary>
        private static ZapfSatz Parametervermerk(string schluessel, Speichertemperaturquelle quelle) => quelle switch
        {
            Speichertemperaturquelle.Grossanlage => ZapfSatz.Neu("HERKUNFT_PARAMETER_GROSSANLAGE", schluessel),
            Speichertemperaturquelle.Schnellpfad => ZapfSatz.Neu("HERKUNFT_PARAMETER_SCHNELLAUSLEGUNG", schluessel),
            _ => ZapfSatz.Neu("HERKUNFT_PARAMETER", schluessel)
        };
    }

    /// <summary>Die Wertprüfungen der Auslegung: jede Verletzung ist eine benannte Ablehnung.</summary>
    internal static class Auslegungspruefung
    {
        /// <summary>Eine endliche Zahl, sonst <see cref="ZapfAuslegungsfehler.GroesseUngueltig"/>; <paramref name="was"/> ist ein Begriff.</summary>
        internal static double Endlich(double wert, ZapfSatz was)
        {
            if (double.IsNaN(wert) || double.IsInfinity(wert))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_NICHT_ENDLICH", was));
            return wert;
        }

        /// <summary>Eine endliche, nicht negative Zahl.</summary>
        internal static double NichtNegativ(double wert, ZapfSatz was)
        {
            Endlich(wert, was);
            if (wert < 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_NEGATIV", was));
            return wert;
        }

        /// <summary>Eine endliche, positive Zahl.</summary>
        internal static double Positiv(double wert, ZapfSatz was)
        {
            Endlich(wert, was);
            if (!(wert > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_NICHT_POSITIV", was));
            return wert;
        }

        /// <summary>Eine positive Temperaturdifferenz, sonst <see cref="ZapfAuslegungsfehler.TemperaturUngueltig"/>.</summary>
        internal static double Spreizung(double obenC, double untenC, ZapfSatz was)
        {
            double d = obenC - untenC;
            if (double.IsNaN(d) || double.IsInfinity(d) || !(d > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.TemperaturUngueltig, ZapfSatz.Neu("AUSLEGUNG_SPREIZUNG", was));
            return d;
        }
    }

    /// <summary>
    /// Zahlformat der Rechenweg-Sätze und Hinweise — feste Kultur (invariant, Punkt als
    /// Dezimalzeichen) wie die Vermerke des Mengengerüsts: derselbe Satz auf jedem Rechner und in
    /// jeder Oberflächensprache, damit Tests und Vergleich ihn lesen können.
    /// </summary>
    internal static class Auslegungstext
    {
        /// <summary>Eine Zahl mit höchstens drei Nachkommastellen.</summary>
        internal static string Z(double wert) => wert.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>Eine Zahl ohne Nachkommastellen.</summary>
        internal static string G(double wert) => wert.ToString("0", CultureInfo.InvariantCulture);
    }
}
