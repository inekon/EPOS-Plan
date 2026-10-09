using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KM3 - TEILLAST UND TAKTEN DER KAELTEMASCHINE (Fachkonzept Teillast und Takten der Kaeltemaschine,
    // Abschnitte 4.1, 5.3 und 6; Umsetzungskonzept Abschnitt 3.1, Etappe KM3-E1, Welle E1-a; Schemaschritt 209).
    //
    // WOZU. Die Kaeltemaschine bekommt eine Lastachse (Teillastkurve EIRFPLR mit drei Beiwerten und ihrer unteren
    // Gueltigkeit), den Taktverlustfaktor C_d, die Verdichterregelung und den Weg an den Raendern des Kennfelds; das
    // Ergebnis je Maschine fuehrt Taktstrom, Starts, Teillaststunden, mittleren Lastgrad und die extrapolierten Stunden.
    // Ein Schritt fuer alle 21 Spalten:
    //
    //   Tab_Kaeltemaschine_STAMM,     Teillast_Weg                 TEXT IN ('LINEAR','KURVE')            NULL = heutiger Weg
    //   Tab_Kaeltemaschine            Teillastkurve_a, _b, _c      REAL                                  NULL = keine Kurve
    //                                 Teillastkurve_Lastgrad_Min   REAL 0..1                             NULL = Mindestteillast
    //                                 Taktverlustfaktor_Cd         REAL 0..1                             NULL = 0,9 (KM3-Q1 a)
    //                                 Verdichterregelung           TEXT IN ('EIN_AUS','STUFEN','DREHZAHL') NULL = keine Angabe
    //                                 Kennfeld_Randweg             TEXT IN ('RANDWERT','GUETEGRAD')      NULL = RANDWERT
    //   Tab_ErgebnisKaeltemaschine    Taktstrom_MWh                REAL >= 0
    //                                 Starts                       INTEGER >= 0
    //                                 Teillaststunden              INTEGER 0..8760
    //                                 Lastgrad_Mittel              REAL 0..1
    //                                 Stunden_Extrapoliert         INTEGER 0..8760
    //
    // Die Bereiche der Beiwerte (a -1..2, b und c -2..3) sind Eingabegrenzen von Pruefung und Dialog, kein CHECK
    // (Fachkonzept 4.1: massgeblich ist die Plausibilitaet 3.2).
    //
    // DML allein ueber KaeltemaschinenTypkennfelder.Ergaenzen() an den ausgelieferten Typkennfeldern (ReadOnly = 1,
    // Schluessel KM:TYPKENNFELD_...): Weg, Kurve, x_u und Verdichterregelung aus dem eingebetteten Copper-Satz, je Feld nur
    // wo leer, Pruefsumme neu (KatalogSchluesselSaat). Projektkopien und eigene Saetze bleiben unberuehrt. Das DDL in
    // EINEM Vorgang mit abgeschalteten Fremdschluesseln, die Ergaenzung in einem eigenen; wiederholbar.
    //
    // NUMMER. 209 haengt an 208 KatalogkostenUrsprungSchema (KA1), die zur Bauzeit angemeldet, aber noch nicht gebaut
    // ist; beim Merge auf KatalogkostenUrsprungSchema.SCHRITT + 1 umhaengen.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und
    // die Paketanhebung (Art Katalog).
    // ====================================================================================

    /// <summary>
    /// <b>KM3</b> — Teillast und Takten der Kältemaschine: acht Eingabespalten an Katalog und Projektkopie, fünf
    /// Ergebnisspalten (Fachkonzept Teillast und Takten der Kältemaschine, Abschnitte 4.1 und 5.3): EINE Quelle für
    /// Migration, Werkzeug, Testkopie, Controller und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KaeltemaschineTeillastSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 209) — die EINE Stelle, an der sie steht. Hängt an 208
        /// <c>KatalogkostenUrsprungSchema</c> (KA1); beim Merge auf <c>KatalogkostenUrsprungSchema.SCHRITT + 1</c> umhängen.
        /// </summary>
        public const int SCHRITT = 209;

        /// <summary>Der Katalog der Kältemaschinen.</summary>
        public const string TAB_STAMM = KaeltemaschineSchema.TAB_STAMM;

        /// <summary>Die Projektkopie der Kältemaschine.</summary>
        public const string TAB_PROJEKT = KaeltemaschineSchema.TAB_PROJEKT;

        /// <summary>Das Ergebnis je Kältemaschine.</summary>
        public const string TAB_ERGEBNIS = KaeltemaschineAnlageSchema.TAB_ERGEBNIS;

        // ---- Eingabespalten (Katalog und Projektkopie) ----

        /// <summary>Weg der Teillast: <c>LINEAR</c> oder <c>KURVE</c>; NULL = heutiger Weg.</summary>
        public const string SPALTE_TEILLAST_WEG = "Teillast_Weg";

        /// <summary>Beiwert a der Teillastkurve EIRFPLR.</summary>
        public const string SPALTE_KURVE_A = "Teillastkurve_a";

        /// <summary>Beiwert b der Teillastkurve EIRFPLR.</summary>
        public const string SPALTE_KURVE_B = "Teillastkurve_b";

        /// <summary>Beiwert c der Teillastkurve EIRFPLR.</summary>
        public const string SPALTE_KURVE_C = "Teillastkurve_c";

        /// <summary>Untere Gültigkeit x_u der Kurve [0…1]; NULL = Mindestteillast, sonst 0.</summary>
        public const string SPALTE_KURVE_LASTGRAD_MIN = "Teillastkurve_Lastgrad_Min";

        /// <summary>Taktverlustfaktor C_d [0…1]; NULL = 0,9 (derselbe Name wie an <c>Tab_WP</c>).</summary>
        public const string SPALTE_CD = "Taktverlustfaktor_Cd";

        /// <summary>Verdichterregelung: <c>EIN_AUS</c>, <c>STUFEN</c> oder <c>DREHZAHL</c>; NULL = keine Angabe.</summary>
        public const string SPALTE_VERDICHTERREGELUNG = "Verdichterregelung";

        /// <summary>Weg an den Rändern des Kennfelds: <c>RANDWERT</c> oder <c>GUETEGRAD</c>; NULL = <c>RANDWERT</c>.</summary>
        public const string SPALTE_RANDWEG = "Kennfeld_Randweg";

        // ---- Persistenzwerte ----

        public const string WEG_LINEAR = "LINEAR";
        public const string WEG_KURVE = "KURVE";
        public const string REGELUNG_EIN_AUS = "EIN_AUS";
        public const string REGELUNG_STUFEN = "STUFEN";
        public const string REGELUNG_DREHZAHL = "DREHZAHL";
        public const string RANDWEG_RANDWERT = "RANDWERT";
        public const string RANDWEG_GUETEGRAD = "GUETEGRAD";

        /// <summary>Die zwei Teillastwege.</summary>
        public static readonly IReadOnlyList<string> TEILLAST_WEGE = new[] { WEG_LINEAR, WEG_KURVE };

        /// <summary>Die drei Verdichterregelungen.</summary>
        public static readonly IReadOnlyList<string> VERDICHTERREGELUNGEN = new[] { REGELUNG_EIN_AUS, REGELUNG_STUFEN, REGELUNG_DREHZAHL };

        /// <summary>Die zwei Randwege.</summary>
        public static readonly IReadOnlyList<string> RANDWEGE = new[] { RANDWEG_RANDWERT, RANDWEG_GUETEGRAD };

        // ---- Eingabegrenzen und Plausibilitaet (Fachkonzept 3.2 und 4.1; Hauswerte) ----

        /// <summary>Eingabegrenzen der Beiwerte a, b, c (Fachkonzept 4.1).</summary>
        public const double KURVE_A_MIN = -1, KURVE_A_MAX = 2, KURVE_BC_MIN = -2, KURVE_BC_MAX = 3;

        /// <summary>Bereich von EIRFPLR(1), in dem eine Kurve gilt (Fachkonzept 3.2).</summary>
        public const double EIRFPLR1_MIN = 0.9, EIRFPLR1_MAX = 1.1;

        /// <summary>Bereich des EER-Verhältnisses g(x) = x / E(x) auf [x_u, 1] (Fachkonzept 3.2).</summary>
        public const double G_MIN = 0.5, G_MAX = 2.0;

        /// <summary>
        /// Unterer Lastgrad, ab dem die Prüfung g(x) hält, wenn x_u und die Mindestteillast fehlen oder darunter liegen
        /// (Hauswert): g(0) ist für jede Kurve mit a ≠ 0 null, die Prüfung auf [0, 1] verwürfe jede Kurve.
        /// </summary>
        public const double PRUEF_LASTGRAD_UNTEN = 0.1;

        /// <summary>Zahl der gleichmäßigen Prüfstellen der Kurve auf [x_u, 1] (Hauswert).</summary>
        public const int PRUEF_STELLEN = 100;

        // ---- Ergebnisspalten ----

        public const string SPALTE_TAKTSTROM = "Taktstrom_MWh";
        public const string SPALTE_STARTS = "Starts";
        public const string SPALTE_TEILLASTSTUNDEN = "Teillaststunden";
        public const string SPALTE_LASTGRAD_MITTEL = "Lastgrad_Mittel";
        public const string SPALTE_STUNDEN_EXTRAPOLIERT = "Stunden_Extrapoliert";

        private static string Q(string s) => "\"" + s + "\"";

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static string Text(string s, IEnumerable<string> werte)
            => "TEXT CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (" + Liste(werte) + "))";

        private static string Anteil(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN 0 AND 1)";

        /// <summary>Die acht Eingabespalten samt Typ und Prüfklausel, in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> EINGABE_SPALTEN = new[]
        {
            (SPALTE_TEILLAST_WEG, Text(SPALTE_TEILLAST_WEG, TEILLAST_WEGE)),
            (SPALTE_KURVE_A, "REAL"),
            (SPALTE_KURVE_B, "REAL"),
            (SPALTE_KURVE_C, "REAL"),
            (SPALTE_KURVE_LASTGRAD_MIN, Anteil(SPALTE_KURVE_LASTGRAD_MIN)),
            (SPALTE_CD, Anteil(SPALTE_CD)),
            (SPALTE_VERDICHTERREGELUNG, Text(SPALTE_VERDICHTERREGELUNG, VERDICHTERREGELUNGEN)),
            (SPALTE_RANDWEG, Text(SPALTE_RANDWEG, RANDWEGE)),
        };

        /// <summary>Die Namen der acht Eingabespalten in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<string> EINGABESPALTEN = EINGABE_SPALTEN.Select(s => s.Spalte).ToArray();

        /// <summary>Die fünf Ergebnisspalten samt Typ und Prüfklausel, in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> ERGEBNIS_SPALTEN = new[]
        {
            (SPALTE_TAKTSTROM, AnlagenfahrplanSchema.NichtNegativ(SPALTE_TAKTSTROM)),
            (SPALTE_STARTS, "INTEGER CHECK (" + Q(SPALTE_STARTS) + " IS NULL OR " + Q(SPALTE_STARTS) + " >= 0)"),
            (SPALTE_TEILLASTSTUNDEN, AnlagenfahrplanSchema.Stunden(SPALTE_TEILLASTSTUNDEN)),
            (SPALTE_LASTGRAD_MITTEL, Anteil(SPALTE_LASTGRAD_MITTEL)),
            (SPALTE_STUNDEN_EXTRAPOLIERT, AnlagenfahrplanSchema.Stunden(SPALTE_STUNDEN_EXTRAPOLIERT)),
        };

        /// <summary>Alle 21 Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            EINGABE_SPALTEN.Select(s => (TAB_STAMM, s.Spalte, s.Typ))
                .Concat(EINGABE_SPALTEN.Select(s => (TAB_PROJEKT, s.Spalte, s.Typ)))
                .Concat(ERGEBNIS_SPALTEN.Select(s => (TAB_ERGEBNIS, s.Spalte, s.Typ)))
                .ToArray();

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_STAMM, TAB_PROJEKT, TAB_ERGEBNIS };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen alle 21 Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die acht Eingabespalten an <paramref name="tabelle"/> (für Leser auf einem älteren Stand)?</summary>
        public static bool EingabespaltenVorhanden(string tabelle)
            => EINGABESPALTEN.All(s => DataRepository.SpalteVorhanden(tabelle, s));

        /// <summary>Stehen die fünf Ergebnisspalten?</summary>
        public static bool ErgebnisspaltenVorhanden()
            => ERGEBNIS_SPALTEN.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS, s.Spalte));

        /// <summary>Das Ergebnis eines Laufs: angelegte Spalten und die von <c>Ergaenzen</c> gefüllten Typkennfelder.</summary>
        public readonly record struct Laufergebnis(int Angelegt, int Ergaenzt);

        /// <summary>
        /// Führt den Schritt aus — für die Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. Erst das DDL in EINEM Vorgang mit abgeschalteten Fremdschlüsseln, dann
        /// <see cref="KaeltemaschinenTypkennfelder.Ergaenzen"/>. <b>Wiederholbar:</b> Eine stehende Spalte wird übergangen.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        public static Laufergebnis Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            int angelegt = 0;
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Teillast und Takten an " + TAB_STAMM + " und " + TAB_PROJEKT +
                             ", Kennzahlen an " + TAB_ERGEBNIS + "; nichts anzulegen");
            }
            else
            {
                var zeilen = new List<string>();
                using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
                {
                    try
                    {
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        {
                            v.Ausfuehren(Anlegen(s));
                            zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                            angelegt++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (bericht != null)
                    foreach (string z in zeilen) bericht.Add(z);
            }

            KaeltemaschinenTypkennfelder.Ergaenzungsergebnis erg = KaeltemaschinenTypkennfelder.ErgaenzenMitZaehlung();
            bericht?.Add(erg.Gefuellt.ToString(CultureInfo.InvariantCulture) + " Typkennfeld(er) um die Teillastkurve ergaenzt, " +
                         erg.Uebersprungen.ToString(CultureInfo.InvariantCulture) + " uebersprungen (nichts leer oder nicht " +
                         "eingespielt); Projektkopien und eigene Saetze unberuehrt, der Referenzlauf bleibt byte-gleich");
            return new Laufergebnis(angelegt, erg.Gefuellt);
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
