using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Koeffizientensatz eines PV-Moduls, so wie ihn die CEC-Liste fuehrt — vier
    /// Zahlen und der Name, unter dem sie gelten.
    ///
    /// <para>Die Einheiten sind die der Datenbank und die der Liste: <c>alpha_SC</c> in
    /// A/K, <c>beta_OC</c> in V/K, <c>gamma_PMP</c> in %/K, <c>T_NOCT</c> in Grad C. Es
    /// wird NICHTS umgerechnet — die Spalten der Liste tragen dieselben Einheiten wie
    /// die Spalten des Katalogs (Kopfzeile <c>A/K;V/K;C;…;%/K</c>).</para>
    /// </summary>
    public sealed class PvModulKoeffizienten
    {
        public PvModulKoeffizienten(string bezeichner, string firma,
                                    double alphaSc, double betaOc, double gammaPmp, double tNoct)
        {
            Bezeichner = bezeichner ?? "";
            Firma = firma ?? "";
            AlphaSc = alphaSc;
            BetaOc = betaOc;
            GammaPmp = gammaPmp;
            TNoct = tNoct;
        }

        /// <summary>Der Modulname — der Schluessel des Abgleichs (<c>Tab_PV.Bezeichner</c>).</summary>
        public string Bezeichner { get; }

        /// <summary>Der Hersteller; leer heisst „nicht gefuehrt" und passt dann auf jeden.</summary>
        public string Firma { get; }

        /// <summary>Temperaturkoeffizient des Kurzschlussstroms [A/K].</summary>
        public double AlphaSc { get; }

        /// <summary>Temperaturkoeffizient der Leerlaufspannung [V/K].</summary>
        public double BetaOc { get; }

        /// <summary>Temperaturkoeffizient der Leistung [%/K].</summary>
        public double GammaPmp { get; }

        /// <summary>Nennbetriebszelltemperatur [Grad C].</summary>
        public double TNoct { get; }

        /// <summary>Der Wert zu einer der vier Spalten; <c>double.NaN</c> bei unbekanntem Namen.</summary>
        public double Wert(string spalte)
        {
            if (spalte == PvKoeffizientenReparatur.SPALTE_ALPHA) return AlphaSc;
            if (spalte == PvKoeffizientenReparatur.SPALTE_BETA) return BetaOc;
            if (spalte == PvKoeffizientenReparatur.SPALTE_GAMMA) return GammaPmp;
            if (spalte == PvKoeffizientenReparatur.SPALTE_NOCT) return TNoct;
            return double.NaN;
        }
    }

    /// <summary>
    /// Die WERTEQUELLE des Schrittes 69 — die eingebetteten Auslieferungsmodule und,
    /// wenn sie am Herstellerdatenpfad liegt, die CEC-Liste selbst.
    ///
    /// <para><b>Die Einbettung hat Vorrang.</b> Sie ist im Test gegen die Liste
    /// geprueft; wo beide dasselbe Modul fuehren, ist der Wert derselbe. Der Vorrang
    /// entscheidet daher nichts fachlich — er macht den Schritt UNABHAENGIG davon, ob
    /// die Datei da ist. Der Anwenderrechner ohne den Ordner <c>VDI-3805-Daten</c>
    /// (der ist seit W6‑O‑9 abwaehlbare Setup-Komponente) repariert seine sechs
    /// Auslieferungsmodule genauso wie einer mit dem Ordner.</para>
    /// </summary>
    public sealed class PvKoeffizientenquelle
    {
        private readonly Dictionary<string, PvModulKoeffizienten> _saetze =
            new Dictionary<string, PvModulKoeffizienten>(StringComparer.OrdinalIgnoreCase);

        internal PvKoeffizientenquelle(IEnumerable<PvModulKoeffizienten> eingebettet)
        {
            foreach (PvModulKoeffizienten k in eingebettet)
                if (!_saetze.ContainsKey(k.Bezeichner)) _saetze[k.Bezeichner] = k;

            Eingebettet = _saetze.Count;
        }

        /// <summary>Zahl der eingebetteten Auslieferungssaetze.</summary>
        public int Eingebettet { get; }

        /// <summary>Zahl der Saetze, die zusaetzlich aus der CEC-Datei kamen.</summary>
        public int AusDerDatei { get; private set; }

        /// <summary>Der Pfad, an dem die CEC-Datei gesucht wurde.</summary>
        public string Dateipfad { get; private set; } = "";

        /// <summary>Wurde die CEC-Datei gelesen? Sonst gelten nur die eingebetteten Werte.</summary>
        public bool DateiGelesen { get; private set; }

        /// <summary>Alle Bezeichner der Quelle — Auskunft fuer Bericht und Nachweis.</summary>
        public IEnumerable<string> Bezeichner => _saetze.Keys;

        /// <summary>Zahl aller Saetze der Quelle.</summary>
        public int Anzahl => _saetze.Count;

        /// <summary>
        /// Ein Satz Zusatzwerte — die CEC-Datei. Vorhandene Bezeichner bleiben, wie sie
        /// sind (Vorrang der Einbettung).
        /// </summary>
        internal void Ergaenze(string pfad, bool gelesen, IEnumerable<PvModulKoeffizienten> saetze)
        {
            Dateipfad = pfad ?? "";
            DateiGelesen = gelesen;
            if (saetze == null) return;

            foreach (PvModulKoeffizienten k in saetze)
            {
                if (string.IsNullOrWhiteSpace(k.Bezeichner)) continue;
                if (_saetze.ContainsKey(k.Bezeichner)) continue;
                _saetze[k.Bezeichner] = k;
                AusDerDatei++;
            }
        }

        /// <summary>
        /// Sucht den Satz zu einem Modulnamen. <paramref name="firma"/> darf leer sein;
        /// ist sie GESETZT und fuehrt die Quelle einen anderen Hersteller, gilt der Satz
        /// als NICHT gefunden — zwei gleichnamige Module verschiedener Haeuser sind
        /// verschiedene Module.
        /// </summary>
        public bool Finde(string bezeichner, string firma, out PvModulKoeffizienten satz)
        {
            satz = null;
            if (string.IsNullOrWhiteSpace(bezeichner)) return false;
            if (!_saetze.TryGetValue(bezeichner.Trim(), out PvModulKoeffizienten k)) return false;

            if (!string.IsNullOrWhiteSpace(firma) && !string.IsNullOrWhiteSpace(k.Firma) &&
                !string.Equals(firma.Trim(), k.Firma.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            satz = k;
            return true;
        }
    }

    // ====================================================================================
    // Die REPARATUR der verdorbenen PV-Modulkoeffizienten (Befund W6-B-5,
    // Anwenderentscheide W6-B-5-Q1 bis Q3 vom 07.09.2026, Migrationsschritt 69).
    //
    // WOZU. Paket-A-Befund A1 des Konzept_Photovoltaik_Ertragsmodell (N3.3): Der alte
    // Katalogeditor Form_AdminPV schrieb alpha_SC, beta_OC und T_NOCT beim Speichern mit
    // 0 zurueck, und ein aelterer Schreibweg hatte dieselben drei Spalten mit dem Wert
    // von I_Kurzschluss gefuellt. Der SCHREIBWEG ist seit Schemastand 62 repariert, die
    // DATEN waren es nicht - im Bestand steht in alpha_SC, beta_OC und T_NOCT der
    // Kurzschlussstrom des Moduls, also etwa 9,014 statt 0,0034 A/K. Die Simulation faengt
    // T_NOCT mit dem Fenster 20…60 Grad C ab (SimulationPV.NoctDesModuls, Stufe E1.2), die
    // Strangplausibilitaet rechnet mit alpha_SC und beta_OC OHNE Fenster - und leuchtet
    // deshalb grau statt gruen (Konzept_Wechselrichter Kapitel 10, Punkt 7).
    //
    // WAS DER SCHRITT TUT, in dieser Reihenfolge je Tabelle (Tab_PV_STAMM zuerst,
    // Tab_PV danach):
    //   a) GIFTSIGNATUR ERKENNEN, nicht IDs raten. Ein Feld ist VERDORBEN, wenn es
    //      gesetzt ist und entweder auf 1e-06 genau dem I_Kurzschluss derselben Zeile
    //      gleicht (der Kopierfehler) oder ausserhalb seines physikalischen Fensters
    //      liegt (Fenster wie in sql/pv_katalog/messung_pv_katalog.py; die 0 liegt in
    //      JEDEM der vier Fenster ausserhalb). NULL heisst "nicht gepflegt" und ist
    //      damit kein Schaden, sondern der Zielzustand ohne Quelle.
    //   b) REPARIEREN aus der Wertequelle: Bezeichner (und Firma, falls beidseits
    //      gesetzt) treffen einen Satz der Einbettung oder der CEC-Liste -> die vier
    //      Werte gehen in genau die Spalten, die nicht gesund sind. Eine gesunde Spalte
    //      bleibt unberuehrt, auch wenn die Liste etwas anderes sagt.
    //   c) UEBERNAHME in die Projektkopie: was in Tab_PV noch verdorben ist, holt sich
    //      den GESUNDEN Wert des Stammsatzes gleichen Bezeichners - der ist in b) gerade
    //      repariert worden oder von Hand gepflegt.
    //   d) REST AUF NULL: ein verdorbener Wert ohne Treffer wird leer. Nie ein
    //      erfundener Wert - "nicht gepflegt" ist eine ehrliche Aussage, ein geratener
    //      Koeffizient nicht. Die Strangplausibilitaet sagt danach "fehlt", die
    //      Simulation nimmt den NOCT-Rueckfall 45 Grad C. Je Satz eine Protokollzeile.
    //
    // ES AENDERT SICH KEINE ANDERE SPALTE. Der Schritt fasst ausschliesslich alpha_SC,
    // beta_OC, gamma_PMP und T_NOCT an; Leistung, Wirkungsgrad, U/I-Kennwerte, Laenge,
    // Breite, Modulkosten, Firma, Beschreibung und ReadOnly bleiben, wie sie sind.
    //
    // NICHT ERGEBNISNEUTRAL - UND DAS IST DER ZWECK (Entscheid W6-B-5-Q3). alpha_SC und
    // beta_OC liest KEIN Rechenweg (Grep 07.09.2026: nur StrangPlausibilitaet und die
    // Importpruefung). T_NOCT dagegen geht in beide Modelle: In EINFACH ueber
    // BerechnePV(..., tNoct), in ERWEITERT ueber dieselbe Zelltemperaturformel. Wo der
    // Katalogwert bisher AUSSERHALB des Fensters lag, rechnete die Simulation mit dem
    // Rueckfall 45 Grad C; steht nach dem Schritt der Listenwert (Ablytek: 47,4) darin,
    // rechnet sie mit ihm. Das ist eine gewollte Aenderung der ZAHLEN, keine des
    // Rechenwegs - und deshalb die einzige Stelle, an der dieser Schritt eine neue
    // Referenzbasis verlangt (siehe Referenzlaeufe/LIESMICH.md).
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // KlimaWaisenBereinigung (62), BhkwLeistungsgrenzeVorgabe (67) und
    // StromspeicherFirmaNachtrag (68): Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema
    // und den Nachweis in EPOS.Kern.Tests.
    //
    // IDEMPOTENZ. Jede Anweisung traegt ihre Bedingung selbst: Repariert wird nur, was
    // nicht gesund ist, geleert nur, was verdorben ist. Nach dem ersten Lauf ist jede
    // angefasste Spalte entweder gesund oder NULL - beides schliesst die Bedingung des
    // zweiten Laufs aus. Ein zweiter Lauf aendert 0 Zeilen.
    //
    // BEFUND auf Referenzlaeufe/Kenndaten_Test.sqlite (07.09.2026, vor dem Schritt):
    // Tab_PV_STAMM fuehrt SECHS Saetze, davon fuenf mit mindestens einem verdorbenen
    // Feld (nur "Philadelphia Solar PS-M144(HCBF)-530W" ist quellrichtig); Tab_PV fuehrt
    // NEUN, davon sieben verdorben (1015248/1015249 des Projekts 1045 sind seit W6-O-7
    // von Hand gepflegt). Die drei Ablytek-Module und das Philadelphia-Modul stehen in
    // der CEC-Liste, "Jinkosolar JKM 260P-60" und "LG Electronics LG 320 N1K-A5" nicht -
    // deren Werte werden leer.
    // ====================================================================================
    public static class PvKoeffizientenReparatur
    {
        /// <summary>Die Stammtabelle des Modulkatalogs.</summary>
        public const string TAB_STAMM = SchemaKatalog.TAB_PV_STAMM;

        /// <summary>Die Projektkopie desselben Katalogs (Entscheid W6‑B‑5‑Q2).</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_PV;

        /// <summary>Der Modulname — der Schlüssel des Abgleichs mit der Liste.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Der Hersteller — die zweite Hälfte des Schlüssels, wenn er gepflegt ist.</summary>
        public const string SPALTE_FIRMA = "Firma";

        /// <summary>Der Kurzschlussstrom — der Wert, den der alte Schreibweg kopiert hat.</summary>
        public const string SPALTE_ISC = "I_Kurzschluss";

        /// <summary>Temperaturkoeffizient des Kurzschlussstroms [A/K].</summary>
        public const string SPALTE_ALPHA = "alpha_SC";

        /// <summary>Temperaturkoeffizient der Leerlaufspannung [V/K].</summary>
        public const string SPALTE_BETA = "beta_OC";

        /// <summary>Temperaturkoeffizient der Leistung [%/K].</summary>
        public const string SPALTE_GAMMA = "gamma_PMP";

        /// <summary>Nennbetriebszelltemperatur [Grad C].</summary>
        public const string SPALTE_NOCT = "T_NOCT";

        /// <summary>
        /// Die vier Spalten, die dieser Schritt anfasst — und <b>nur</b> sie.
        /// </summary>
        public static readonly string[] SPALTEN =
            { SPALTE_ALPHA, SPALTE_BETA, SPALTE_GAMMA, SPALTE_NOCT };

        /// <summary>
        /// Die zwei Tabellen in der Reihenfolge, in der sie bearbeitet werden: erst der
        /// STAMM, dann die Projektkopie — sonst fände die Übernahme aus dem Stammsatz
        /// (Schritt c) einen noch nicht reparierten Stand vor.
        /// </summary>
        public static readonly string[] TABELLEN = { TAB_STAMM, TAB_PROJEKT };

        /// <summary>
        /// Der Abstand, unter dem ein Wert als <b>Kopie</b> von <see cref="SPALTE_ISC"/>
        /// gilt. Ein exaktes <c>=</c> täte es hier auch — der alte Schreibweg hat das Bit
        /// für Bit gleiche <c>double</c> geschrieben —, aber ein Bestand, der einmal durch
        /// eine Textdarstellung gelaufen ist, hat den Wert vielleicht gerundet. 1e‑6 ist
        /// enger als jeder echte Koeffizient und weiter als jede Rundung.
        /// </summary>
        public const double KOPIE_TOLERANZ = 1e-6;

        /// <summary>Die Toleranz als SQL-Literal — eine Quelle, zwei Schreibweisen.</summary>
        private const string KOPIE_TOLERANZ_SQL = "1e-06";

        // --- Die physikalischen Fenster ---------------------------------------------
        // Sie stehen wortgleich in sql/pv_katalog/messung_pv_katalog.py (FENSTER) und in
        // dessen Notfall-Duplikat im Reparatur-Runner. Was ausserhalb liegt, ist
        // verdorben - die 0 ist es damit in jedem der vier Faelle.

        /// <summary><c>alpha_SC</c> [A/K]: 0 &lt; x ≤ 0,05 (typisch 0,002…0,006).</summary>
        public const double ALPHA_MIN = 0.0;

        /// <summary>Obergrenze zu <see cref="ALPHA_MIN"/>.</summary>
        public const double ALPHA_MAX = 0.05;

        /// <summary><c>beta_OC</c> [V/K]: −0,5 ≤ x &lt; 0 (typisch −0,10…−0,15).</summary>
        public const double BETA_MIN = -0.5;

        /// <summary>Obergrenze zu <see cref="BETA_MIN"/>.</summary>
        public const double BETA_MAX = 0.0;

        /// <summary><c>gamma_PMP</c> [%/K]: −1,0 ≤ x &lt; 0 (typisch −0,30…−0,45).</summary>
        public const double GAMMA_MIN = -1.0;

        /// <summary>Obergrenze zu <see cref="GAMMA_MIN"/>.</summary>
        public const double GAMMA_MAX = 0.0;

        /// <summary>
        /// <c>T_NOCT</c> [Grad C]: 20 ≤ x ≤ 60 (typisch 42…48). <b>Dasselbe Fenster</b>
        /// wie <c>SimulationPV.NOCT_MIN</c>/<c>NOCT_MAX</c> — der Schritt räumt genau die
        /// Werte weg, die dort in den Rückfall laufen.
        /// </summary>
        public const double NOCT_MIN = 20.0;

        /// <summary>Obergrenze zu <see cref="NOCT_MIN"/>.</summary>
        public const double NOCT_MAX = 60.0;

        /// <summary>Der Unterordner der CEC-Listen im Herstellerdatenordner.</summary>
        public const string CEC_ORDNER = "PV";

        /// <summary>Die ausgelieferte CEC-Modulliste (W6‑O‑9).</summary>
        public const string CEC_DATEI = "CEC Modules.csv";

        /// <summary>
        /// Die höchste Zahl an Protokollzeilen, die
        /// <see cref="Protokollabfrage"/> liefert. Der Bericht eines Migrationslaufs
        /// soll lesbar bleiben; die Zahl der betroffenen Sätze nennt daneben
        /// <see cref="ZaehlungVerdorben"/> vollständig.
        /// </summary>
        public const int PROTOKOLL_HOECHSTZAHL = 200;

        /// <summary>Das Trennzeichen der Sammelabfragen — ein Zeilenumbruch.</summary>
        private const string TRENNER_SQL = "char(10)";

        // =================================================================================
        // 1 — Die eingebetteten Werte der Auslieferungsmodule
        // =================================================================================

        /// <summary>
        /// Die Koeffizienten der <b>ausgelieferten</b> Katalogmodule, aus
        /// <c>VDI-3805-Daten/PV/CEC Modules.csv</c> gezogen und dort im Test
        /// nachgehalten (<c>PvKoeffizientenReparaturTests</c>).
        ///
        /// <para><b>Warum eingebettet.</b> Ein Schemaschritt läuft auf dem
        /// Anwenderrechner. Der Ordner <c>VDI-3805-Daten</c> ist seit <b>W6‑O‑9</b> eine
        /// ABWÄHLBARE Setup-Komponente — er kann fehlen. Ohne die Einbettung hinge die
        /// Reparatur der ausgelieferten Module an einer Datei, die es vielleicht nicht
        /// gibt, und derselbe Bestand sähe auf zwei Rechnern verschieden aus.</para>
        ///
        /// <para><b>Vier Sätze, nicht sechs.</b> Von den sechs Modulen des
        /// Auslieferungskatalogs stehen nur diese vier in der CEC-Liste; die zwei
        /// übrigen nennt <see cref="OHNE_CEC_TREFFER"/>. Für sie gibt es keinen Wert zum
        /// Einbetten — und deshalb werden ihre verdorbenen Felder leer statt geraten.</para>
        /// </summary>
        public static readonly PvModulKoeffizienten[] AUSLIEFERUNG =
        {
            new PvModulKoeffizienten("Ablytek 6MN6A270", "Ablytek",
                                     0.00486614, -0.121182, -0.4509, 47.4),
            new PvModulKoeffizienten("Ablytek 6MN6A275", "Ablytek",
                                     0.00490782, -0.122249, -0.4509, 47.4),
            new PvModulKoeffizienten("Ablytek 6MN6A290", "Ablytek",
                                     0.00503807, -0.125449, -0.4509, 47.4),
            new PvModulKoeffizienten("Philadelphia Solar PS-M144(HCBF)-530W", "Philadelphia Solar",
                                     0.00272, -0.128904, -0.385, 45.3),
        };

        /// <summary>
        /// Die zwei Auslieferungsmodule, die die CEC-Liste <b>nicht</b> führt — beide
        /// stammen aus einer PVsyst-<c>.PAN</c>-Datei eines anderen Prüflabors.
        ///
        /// <para>Die Liste kennt Schwesterzeilen („Jinko Solar Co, Ltd JKM260P-60",
        /// „LG Electronics Inc, LG320N1K-A5"), aber mit ANDEREN Kennwerten: I_sc 8,98
        /// statt 9,014 beim Jinko, 10,19 statt 10,35 beim LG. Das ist ein anderes
        /// Messprotokoll und damit ein anderes Modul — schon der Handlauf
        /// <c>sql/pv_katalog/reparatur_pv_katalog.sql</c> hat diese Zuordnung
        /// ausdrücklich <b>auskommentiert</b> gelassen. Sie hier zu übernehmen hiesse,
        /// eine Zahl zu erfinden; der Schritt setzt stattdessen leer.</para>
        /// </summary>
        public static readonly string[] OHNE_CEC_TREFFER =
        {
            "Jinkosolar JKM 260P-60",
            "LG Electronics LG 320 N1K-A5",
        };

        // =================================================================================
        // 2 — Die Wertequelle: Einbettung, dann die Datei
        // =================================================================================

        /// <summary>
        /// Der Pfad, an dem die CEC-Modulliste erwartet wird — über
        /// <c>Dienste.Pfade</c> und den Herstellerdatenpfad des Anwenders
        /// (<c>EinstellungenCtrl.HerstellerdatenpfadOderVorgabe</c>, Reihenfolge aus
        /// W6‑O‑9: gespeicherte Einstellung, sonst Auslieferung, sonst der alte
        /// Vorgabeordner). Der Kern greift nie unmittelbar auf einen Systempfad zu.
        /// </summary>
        public static string CecDateipfad()
        {
            string wurzel = EinstellungenCtrl.HerstellerdatenpfadOderVorgabe();
            if (string.IsNullOrWhiteSpace(wurzel)) return "";
            return Dienste.Pfade.Verbinde(wurzel, CEC_ORDNER, CEC_DATEI);
        }

        /// <summary>
        /// Die Wertequelle des Schrittes: <see cref="AUSLIEFERUNG"/> und — wenn die Datei
        /// am Herstellerdatenpfad liegt — die vollständige CEC-Liste dazu.
        ///
        /// <para><b>Dieselbe Leseroutine wie der Import</b>: <c>CECDataService</c>. Es
        /// gibt keinen zweiten CSV-Leser; jede Eigenheit der Datei (zwei Trennzeichen,
        /// zwei Dezimalzeichen, Einheiten- und Schlüsselzeile, Pflichtspalten) ist dort
        /// entschieden und bleibt es. Fehlt die Datei oder scheitert das Lesen, bleibt es
        /// bei den eingebetteten Sätzen — <b>ohne Fehler</b>: Der Schritt soll auch auf
        /// einem Rechner ohne Herstellerdaten durchlaufen.</para>
        /// </summary>
        public static PvKoeffizientenquelle Quelle()
        {
            var quelle = new PvKoeffizientenquelle(AUSLIEFERUNG);

            string pfad = CecDateipfad();
            if (string.IsNullOrWhiteSpace(pfad))
            {
                quelle.Ergaenze("", false, null);
                return quelle;
            }

            try
            {
                var dienst = new CECDataService();
                (bool erfolg, CecFortschritt _) = dienst.LoadFromFile(pfad);
                if (!erfolg)
                {
                    quelle.Ergaenze(pfad, false, null);
                    return quelle;
                }

                var aus = new List<PvModulKoeffizienten>();
                foreach (PVModule m in dienst.AllModules)
                    aus.Add(new PvModulKoeffizienten(m.Name, m.Manufacturer,
                                                     m.alpha_sc, m.beta_oc, m.gamma_pmp, m.T_NOCT));

                quelle.Ergaenze(pfad, true, aus);
            }
            catch (Exception)
            {
                // Eine unlesbare Herstellerdatei darf die Migration nicht anhalten -
                // die Auslieferungsmodule stehen eingebettet bereit.
                quelle.Ergaenze(pfad, false, null);
            }

            return quelle;
        }

        // =================================================================================
        // 3 — Die Giftsignatur als SQL-Bedingung
        // =================================================================================

        /// <summary>
        /// Das physikalische Fenster einer der vier Spalten als SQL-Bedingung.
        /// <paramref name="praefix"/> ist der Tabellenpräfix („" oder „s.").
        /// </summary>
        public static string Fenster(string spalte, string praefix = "")
        {
            string s = (praefix ?? "") + spalte;

            if (spalte == SPALTE_ALPHA)
                return "(" + s + " > " + Z(ALPHA_MIN) + " AND " + s + " <= " + Z(ALPHA_MAX) + ")";
            if (spalte == SPALTE_BETA)
                return "(" + s + " >= " + Z(BETA_MIN) + " AND " + s + " < " + Z(BETA_MAX) + ")";
            if (spalte == SPALTE_GAMMA)
                return "(" + s + " >= " + Z(GAMMA_MIN) + " AND " + s + " < " + Z(GAMMA_MAX) + ")";
            if (spalte == SPALTE_NOCT)
                return "(" + s + " >= " + Z(NOCT_MIN) + " AND " + s + " <= " + Z(NOCT_MAX) + ")";

            throw new ArgumentException("Unbekannte Spalte: " + spalte, nameof(spalte));
        }

        /// <summary>
        /// Die KOPIE-Bedingung: Der Wert gleicht auf <see cref="KOPIE_TOLERANZ"/> genau
        /// dem <see cref="SPALTE_ISC"/> derselben Zeile — die Signatur des alten
        /// Schreibwegs.
        /// </summary>
        public static string Kopiert(string spalte, string praefix = "")
        {
            string p = praefix ?? "";
            return "(" + p + SPALTE_ISC + " IS NOT NULL AND abs(" + p + spalte + " - " +
                   p + SPALTE_ISC + ") < " + KOPIE_TOLERANZ_SQL + ")";
        }

        /// <summary>
        /// <b>Gesund</b>: gesetzt, im Fenster und keine Kopie des Kurzschlussstroms.
        /// Genau das wird NIE angefasst.
        /// </summary>
        public static string Gesund(string spalte, string praefix = "")
        {
            string p = praefix ?? "";
            return "(" + p + spalte + " IS NOT NULL AND " + Fenster(spalte, p) +
                   " AND NOT " + Kopiert(spalte, p) + ")";
        }

        /// <summary>
        /// <b>Verdorben</b>: gesetzt, aber nicht gesund. <c>NULL</c> ist NICHT verdorben —
        /// „nicht gepflegt" ist kein Schaden, sondern der Zielzustand ohne Quelle.
        /// </summary>
        public static string Verdorben(string spalte, string praefix = "")
        {
            string p = praefix ?? "";
            return "(" + p + spalte + " IS NOT NULL AND NOT " + Gesund(spalte, p) + ")";
        }

        /// <summary>
        /// <b>Pflegebedürftig</b>: nicht gesund — also verdorben ODER leer. Das ist die
        /// Bedingung, unter der ein Treffer der Wertequelle eingetragen wird.
        /// </summary>
        public static string Pflegebeduerftig(string spalte, string praefix = "")
        {
            return "(NOT " + Gesund(spalte, praefix) + ")";
        }

        /// <summary>Die Oder-Verknüpfung einer Bedingung über alle vier Spalten.</summary>
        private static string UeberAlleSpalten(Func<string, string> bedingung)
        {
            var teile = new List<string>();
            foreach (string s in SPALTEN) teile.Add(bedingung(s));
            return "(" + string.Join(" OR ", teile) + ")";
        }

        // =================================================================================
        // 4 — Zählungen und Protokoll
        // =================================================================================

        /// <summary>
        /// Zählt die Sätze mit mindestens einem <b>verdorbenen</b> Feld — vor und nach
        /// dem Schritt, damit der Lauf-Bericht sagen kann, was er getan hat.
        /// </summary>
        public static string ZaehlungVerdorben(string tabelle)
        {
            return "SELECT COUNT(*) FROM " + Tabelle(tabelle) +
                   " WHERE " + UeberAlleSpalten(s => Verdorben(s));
        }

        /// <summary>
        /// Zählt die Sätze mit mindestens einem <b>pflegebedürftigen</b> Feld — die
        /// Menge, für die überhaupt in der Wertequelle gesucht wird.
        /// </summary>
        public static string ZaehlungPflegebeduerftig(string tabelle)
        {
            return "SELECT COUNT(*) FROM " + Tabelle(tabelle) +
                   " WHERE " + UeberAlleSpalten(s => Pflegebeduerftig(s));
        }

        /// <summary>Zählt alle Sätze der Tabelle — Auskunft für den Bericht.</summary>
        public static string Gesamtzahl(string tabelle)
        {
            return "SELECT COUNT(*) FROM " + Tabelle(tabelle);
        }

        /// <summary>
        /// Die BEZEICHNER der pflegebedürftigen Sätze, ein Name je Zeile — die Liste,
        /// die der Schritt in der Wertequelle nachschlägt.
        ///
        /// <para>Sie kommt als EIN Text zurück (<c>group_concat</c> mit Zeilenumbruch),
        /// weil der SQLite-Zweig der Migration nur Skalare lesen kann; zerlegt wird er
        /// mit <see cref="Zerlege"/>. Ein Modulname enthält keinen Zeilenumbruch — der
        /// Katalog führt eine einzeilige Textspalte.</para>
        /// </summary>
        public static string BezeichnerAbfrage(string tabelle)
        {
            return "SELECT group_concat(b, " + TRENNER_SQL + ") FROM (" +
                   "SELECT DISTINCT " + SPALTE_BEZEICHNER + " AS b FROM " + Tabelle(tabelle) +
                   " WHERE " + SPALTE_BEZEICHNER + " IS NOT NULL AND " + SPALTE_BEZEICHNER + " <> '' " +
                   "AND " + UeberAlleSpalten(s => Pflegebeduerftig(s)) +
                   " ORDER BY b)";
        }

        /// <summary>
        /// Eine PROTOKOLLZEILE je Satz, dessen verdorbene Felder gleich geleert werden —
        /// abzufragen <b>vor</b> <see cref="Leerung"/>, denn danach ist die Bedingung
        /// falsch.
        ///
        /// <para>Die Zeile nennt Tabelle, ID, Modulname, die betroffenen Felder und den
        /// Grund; „= I_Kurzschluss" steht dort, wo die Kopie-Signatur greift, sonst
        /// „ausserhalb des Fensters".</para>
        /// </summary>
        public static string Protokollabfrage(string tabelle)
        {
            var felder = new StringBuilder();
            foreach (string s in SPALTEN)
            {
                if (felder.Length > 0) felder.Append(" || ");
                felder.Append("CASE WHEN ").Append(Verdorben(s))
                      .Append(" THEN ', ").Append(s).Append("' ELSE '' END");
            }

            string grund =
                "CASE WHEN " + UeberAlleSpalten(s => "(" + Verdorben(s) + " AND " + Kopiert(s) + ")") +
                " THEN 'Kopie von " + SPALTE_ISC + "' ELSE 'ausserhalb des Fensters' END";

            return "SELECT group_concat(z, " + TRENNER_SQL + ") FROM (" +
                   "SELECT 'PV-Modul \"' || coalesce(" + SPALTE_BEZEICHNER + ", '?') || " +
                   "'\" (" + Tabelle(tabelle) + " ID ' || ID || '): ' || " +
                   "substr(" + felder + ", 3) || " +
                   "' verdorben (' || " + grund + " || '), kein Treffer in der CEC-Liste - " +
                   "auf leer gesetzt. Pflege ueber den Modulkatalog.' AS z " +
                   "FROM " + Tabelle(tabelle) +
                   " WHERE " + UeberAlleSpalten(s => Verdorben(s)) +
                   " ORDER BY ID LIMIT " + PROTOKOLL_HOECHSTZAHL.ToString(CultureInfo.InvariantCulture) + ")";
        }

        /// <summary>
        /// Zerlegt das Ergebnis einer Sammelabfrage in seine Zeilen; leere Zeilen fallen
        /// weg. <c>null</c> und Leertext ergeben eine leere Liste.
        /// </summary>
        public static IList<string> Zerlege(string sammeltext)
        {
            var raus = new List<string>();
            if (string.IsNullOrEmpty(sammeltext)) return raus;

            foreach (string z in sammeltext.Replace("\r\n", "\n").Split('\n'))
                if (!string.IsNullOrWhiteSpace(z)) raus.Add(z.Trim());

            return raus;
        }

        // =================================================================================
        // 5 — Die drei Anweisungsarten
        // =================================================================================

        /// <summary>
        /// Die REPARATUR eines Modulnamens aus einem Satz der Wertequelle: Jede der vier
        /// Spalten bekommt den Listenwert <b>nur dann</b>, wenn sie nicht gesund ist —
        /// eine gesunde Spalte bleibt, wie sie ist, auch wenn die Liste etwas anderes
        /// sagt (die Handpflege des Anwenders schlägt die Liste).
        ///
        /// <para>Die Firma steht in der Bedingung, wenn BEIDE Seiten sie führen: Zwei
        /// gleichnamige Module verschiedener Häuser sind verschiedene Module. Fehlt sie
        /// auf einer Seite, entscheidet der Name allein.</para>
        ///
        /// <para><b>Ein Listenwert, der selbst nicht im Fenster liegt, wird NICHT
        /// eingetragen</b> (<see cref="WertGesund"/>) — die CEC-Liste führt für manche
        /// Module eine 0 in <c>T_NOCT</c>, und eine 0 wäre nach genau derselben Regel
        /// wieder verdorben: Der zweite Lauf träfe denselben Satz noch einmal an, und die
        /// Idempotenzzusage wäre gebrochen. Solche Spalten fallen in die Leerung.</para>
        ///
        /// <para><c>null</c>, wenn KEINE der vier Spalten einen brauchbaren Listenwert
        /// hat — dann gibt es für diesen Satz nichts zu tun.</para>
        /// </summary>
        public static string Reparatur(string tabelle, PvModulKoeffizienten satz)
        {
            if (satz == null) throw new ArgumentNullException(nameof(satz));

            var zuweisungen = new List<string>();
            var bedingungen = new List<string>();
            foreach (string s in SPALTEN)
            {
                if (!WertGesund(s, satz.Wert(s))) continue;

                zuweisungen.Add(s + " = CASE WHEN " + Pflegebeduerftig(s) + " THEN " + Z(satz.Wert(s)) +
                                " ELSE " + s + " END");
                bedingungen.Add(Pflegebeduerftig(s));
            }

            if (zuweisungen.Count == 0) return null;

            string bedingung = SPALTE_BEZEICHNER + " = " + Text(satz.Bezeichner);
            if (!string.IsNullOrWhiteSpace(satz.Firma))
                bedingung += " AND (" + SPALTE_FIRMA + " IS NULL OR trim(" + SPALTE_FIRMA + ") = '' OR " +
                             "lower(trim(" + SPALTE_FIRMA + ")) = lower(" + Text(satz.Firma.Trim()) + "))";

            return "UPDATE " + Tabelle(tabelle) + " SET " + string.Join(", ", zuweisungen) +
                   " WHERE " + bedingung + " AND (" + string.Join(" OR ", bedingungen) + ")";
        }

        /// <summary>
        /// Liegt ein WERT im physikalischen Fenster seiner Spalte? Dieselbe Regel wie
        /// <see cref="Fenster"/>, nur in C# statt in SQL — gebraucht wird sie dort, wo
        /// über einen Wert der Wertequelle entschieden wird, und im Nachweis.
        /// </summary>
        public static bool WertGesund(string spalte, double wert)
        {
            if (double.IsNaN(wert) || double.IsInfinity(wert)) return false;

            if (spalte == SPALTE_ALPHA) return wert > ALPHA_MIN && wert <= ALPHA_MAX;
            if (spalte == SPALTE_BETA) return wert >= BETA_MIN && wert < BETA_MAX;
            if (spalte == SPALTE_GAMMA) return wert >= GAMMA_MIN && wert < GAMMA_MAX;
            if (spalte == SPALTE_NOCT) return wert >= NOCT_MIN && wert <= NOCT_MAX;

            throw new ArgumentException("Unbekannte Spalte: " + spalte, nameof(spalte));
        }

        /// <summary>
        /// Die ÜBERNAHME einer Spalte aus dem Stammsatz gleichen Bezeichners in die
        /// Projektkopie (Entscheid W6‑B‑5‑Q2, Regel d). Genommen wird nur ein
        /// <see cref="Gesund"/>er Stammwert — der ist entweder gerade aus der Liste
        /// repariert worden oder von Hand gepflegt.
        ///
        /// <para>Bei mehreren Stammsätzen desselben Namens entscheidet die kleinste ID;
        /// das ist der ältere und damit der ausgelieferte Satz.</para>
        /// </summary>
        public static string UebernahmeAusStamm(string spalte)
        {
            string wert = "(SELECT s." + spalte + " FROM " + TAB_STAMM + " s" +
                          " WHERE s." + SPALTE_BEZEICHNER + " = " + TAB_PROJEKT + "." + SPALTE_BEZEICHNER +
                          " AND " + Gesund(spalte, "s.") +
                          " ORDER BY s.ID LIMIT 1)";

            return "UPDATE " + TAB_PROJEKT + " SET " + spalte + " = " + wert +
                   " WHERE " + Verdorben(spalte) + " AND " + wert + " IS NOT NULL";
        }

        /// <summary>
        /// Die LEERUNG einer Spalte: Was nach Reparatur und Übernahme noch verdorben ist,
        /// hat keine Quelle — und wird <c>NULL</c>. <b>Nie ein erfundener Wert.</b>
        /// </summary>
        public static string Leerung(string tabelle, string spalte)
        {
            return "UPDATE " + Tabelle(tabelle) + " SET " + spalte + " = NULL" +
                   " WHERE " + Verdorben(spalte);
        }

        // =================================================================================
        // 6 — Hilfsmittel
        // =================================================================================

        /// <summary>
        /// Prüft, dass eine Tabellenangabe eine der zwei erlaubten ist — der einzige
        /// Weg, auf dem ein Name in den SQL-Text gerät, der nicht aus einer Konstanten
        /// stammt.
        /// </summary>
        private static string Tabelle(string tabelle)
        {
            if (tabelle == TAB_STAMM || tabelle == TAB_PROJEKT) return tabelle;
            throw new ArgumentException("Unbekannte Tabelle: " + tabelle, nameof(tabelle));
        }

        /// <summary>Eine Zahl als SQL-Literal — invariante Kultur, volle Genauigkeit.</summary>
        private static string Z(double wert)
        {
            return wert.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>Ein Text als SQL-Literal — einfache Anführungszeichen verdoppelt.</summary>
        private static string Text(string wert)
        {
            return "'" + (wert ?? "").Replace("'", "''") + "'";
        }
    }
}
