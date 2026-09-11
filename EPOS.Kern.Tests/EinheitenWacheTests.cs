using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die zwei Wächter über die EINHEITENREGEL des Rechenkerns
    /// (Anwenderentscheid <b>W8‑O‑5c</b> vom 07.09.2026, Frage Q1: „Regel
    /// festschreiben"; Stufe S1.4 des <c>Konzept_Einheiten_EPOS-Plan.md</c>).
    ///
    /// <para><b>Die Regel, die sie halten.</b></para>
    /// <list type="number">
    ///   <item>Zeitreihen führen <b>kWh</b> (Viertelstundenreihen kW), immer.</item>
    ///   <item>Jahres- und Monatssummen, die den Kern VERLASSEN, führen <b>MWh</b>.</item>
    ///   <item>Jede Größe, die den Kern verlässt, trägt ihre <b>Einheit im NAMEN</b>
    ///   (<c>…Kwh</c>, <c>…Mwh</c>, <c>…Kw</c>) oder im DTO.</item>
    ///   <item>Umgerechnet wird an genau <b>zwei Nähten</b> —
    ///   <c>SimulationErgebnisCtrl</c> (Anzeige) und <c>SimulationRunner</c>
    ///   (Datenbank) — und sonst nur in der Anzeige über <c>Energieeinheit</c>.</item>
    /// </list>
    ///
    /// <para><b>Warum es die Wächter braucht.</b> Der Fehler aus W8‑O‑5b entstand nicht
    /// daran, dass es zwei Einheiten gibt, sondern daran, dass ein Feld sie NICHT
    /// NANNTE: <c>SimulationWaermebedarf.Waermebedarf_Brauchwasser</c> stand im Lauf in
    /// MWh und in der Vorschau in kWh, und die Ergebnishülle teilte den Wert des Laufs
    /// ein zweites Mal — der Brauchwasserbedarf stand um den Faktor 1000 zu klein auf
    /// dem Bildschirm. Ein Kommentar hält die Einheit nicht (Befund U7: der Kommentar an
    /// <c>Strombedarf_Max</c> nannte seit dem Bestand kWh für eine Leistung in kW). Der
    /// NAME hält sie, und diese zwei Fälle halten den Namen.</para>
    ///
    /// <para>Beide lesen den QUELLTEXT, nicht die Metadaten — ein Verstoß soll auffallen,
    /// bevor jemand die falsche Zahl auf dem Bildschirm sucht.</para>
    /// </summary>
    public class EinheitenWacheTests
    {
        // =====================================================================
        //  Wächter 1 — kein Faktor 1000 auf einer Energiemenge in der Anzeige
        // =====================================================================

        /// <summary>
        /// Die fünf Schreibweisen, mit denen im Bestand zwischen kWh und MWh (bzw.
        /// zwischen Viertelstunden-kW und MWh) umgerechnet wird: <c>/ 1000</c>,
        /// <c>* 1000</c>, <c>/= 1000</c>, <c>*= 1000</c>, <c>/ 4000</c> — dazu die zwei
        /// Schreibweisen des Kehrwerts, <c>0.001</c> und <c>1e-3</c>.
        ///
        /// <para>Eine Suche nach <c>/ 1000</c> allein reicht NICHT: Die zentrale
        /// Umrechnung des Programms (<c>BhkwPlan.VectorSumme</c> und
        /// <c>BhkwPlan.MonatsSumme</c>, aus denen jede Monatsreihe und jede Jahressumme
        /// entsteht) schreibt <c>0.001</c>, und die Viertelstundenreihen teilen durch
        /// 4000. Wer nur die eine Schreibweise kennt, findet die anderen 26 Stellen des
        /// Bestands nicht (Konzept 1.1).</para>
        /// </summary>
        private static readonly Regex Faktor = new Regex(
            @"(?<![A-Za-z0-9_.])(?:/=|\*=|/|\*)\s*(?:1000|4000)(?:\.0*)?[fFdDmM]?(?![0-9A-Za-z_.])"
            + @"|(?<![A-Za-z0-9_.])0\.001(?![0-9])"
            + @"|(?<![A-Za-z0-9_.])1[eE]-3(?![0-9])",
            RegexOptions.Compiled);

        /// <summary>
        /// Die BEGRÜNDETEN Ausnahmen — Datei:Zeile ist bewusst NICHT Teil des
        /// Schlüssels, damit ein Verschieben der Zeile den Wächter nicht rot färbt; der
        /// Schlüssel ist die Datei plus der Ausdruck.
        ///
        /// <para><b>Keine davon ist eine Energiemenge.</b> Vier rechnen eine LEISTUNG
        /// von Watt auf Kilowatt um (Modul-Nennleistung <c>Tab_PV.Leistung</c> steht in
        /// W je Modul, die Anzeige führt kWp), die fünfte ist eine Untergrenze für eine
        /// Leistung. Ein Modul, das 400 W leistet, bleibt 0,4 kW — egal, in welcher
        /// Einheit die Jahresarbeit steht (Konzept 1.2).</para>
        /// </summary>
        private static readonly (string Datei, string Ausdruck, string Grund)[] AusnahmenAnzeige =
        {
            ("PhotovoltaikVerguetungDialog.razor", "Math.Max(0.001, Kwp)",
             "Untergrenze der ANLAGENLEISTUNG [kWp] gegen eine Division durch null - keine Energiemenge."),
            ("PhotovoltaikHuelle.cs", "d.Leistung * (zeile.AnzahlModule ?? 0) / 1000.0",
             "Modul-Nennleistung [W] x Modulzahl -> kWp. Leistung, keine Energiemenge."),
            ("SimulationKonfigHuelle.cs", "(modul.m_Leistung * anzahl / 1000.0)",
             "Modul-Nennleistung [W] x Modulzahl -> kWp (Anzeigetext der PV-Karte)."),
            ("SimulationKonfigHuelle.cs", "double kwp = modul.m_Leistung * anzahl / 1000.0;",
             "Modul-Nennleistung [W] x Modulzahl -> kWp."),
            ("SimulationKonfigHuelle.cs", "kwp += modul.m_Leistung / 1000.0 * s.Modulzahl;",
             "Modul-Nennleistung [W] -> kW, je Strang summiert. Leistung, keine Energiemenge."),
        };

        /// <summary>
        /// <b>Wächter 1.</b> In <c>EPOS.UI</c> und in den Windows-Ansichten
        /// (<c>WindowsFormsApplication1/Views</c>) steht auf einer Energiemenge KEIN
        /// Faktor 1000.
        ///
        /// <para>Umgerechnet wird dort ausschließlich über <c>Energieeinheit</c> — die
        /// Klasse, die die Einheit AM WERT führt und deren Identität exakt ist. Alles
        /// andere gehört an die zwei Nähte im Kern. Die Ergebnisseite hat den Weg mit
        /// W8‑O‑5c / S1.2 hinter sich: Ihre elf Energie-Divisionen (Befund U6) sind nach
        /// <c>SimulationErgebnisCtrl</c> gewandert und heißen dort <c>…Mwh</c>.</para>
        ///
        /// <para>Die Meldung nennt Datei und Zeile — wer eine neue Umrechnung braucht,
        /// verschiebt sie in den Kern oder trägt sie mit Grund in
        /// <see cref="AusnahmenAnzeige"/> ein.</para>
        /// </summary>
        [Fact]
        public void In_Anzeige_und_Huellen_steht_kein_Faktor_1000_auf_einer_Energiemenge()
        {
            var funde = new List<string>();

            foreach (string datei in Anzeigedateien())
            {
                string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                {
                    string zeile = zeilen[i];
                    if (IstKommentar(zeile)) continue;
                    if (!Faktor.IsMatch(zeile)) continue;
                    if (IstAusgenommen(datei, zeile)) continue;

                    funde.Add(Kurzname(datei) + ":" + (i + 1) + "  " + zeile.Trim());
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Stellen rechnen in der ANZEIGE mit einem Faktor 1000 auf einer " +
                "Energiemenge (Einheitenregel W8-O-5c, Q1). Umgerechnet wird im Kern - an " +
                "den zwei Naehten SimulationErgebnisCtrl und SimulationRunner - oder ueber " +
                "Energieeinheit; eine Leistung (W -> kW) traegt ihren Grund in " +
                "AusnahmenAnzeige.\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Alle sieben Schreibweisen treffen, und die
        /// Stellen, die NICHT gemeint sind, treffen nicht. Ohne diesen Fall wäre der
        /// Wächter oben stumm, sobald jemand den Ausdruck anders schreibt.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_alle_sieben_Schreibweisen()
        {
            Assert.Matches(Faktor, "double m = kwh / 1000.0;");
            Assert.Matches(Faktor, "double kwh = mwh * 1000;");
            Assert.Matches(Faktor, "Restwaerme /= 1000f;");
            Assert.Matches(Faktor, "summe *= 1000.0;");
            Assert.Matches(Faktor, "double mwh = reihe.Sum() / 4000;");
            Assert.Matches(Faktor, "summe = acc * 0.001;");
            Assert.Matches(Faktor, "double x = wert * 1e-3;");

            // Keine Umrechnung: eine Zahl 1000 ohne Operator, ein Feldname, ein Text.
            Assert.DoesNotMatch(Faktor, "var puffer = new float[1000];");
            Assert.DoesNotMatch(Faktor, "double x = grenze1000 * faktor;");
            Assert.DoesNotMatch(Faktor, "return wert / 10000.0;");
            Assert.DoesNotMatch(Faktor, "return Energieeinheit.MWh.AusKWh(summe);");
        }

        /// <summary>
        /// <b>Gegenprobe zur Kommentarschonung:</b> Die Klassenköpfe und die
        /// Formelnachweise NENNEN den Ausdruck absichtlich — geprüft wird nur, was der
        /// Übersetzer sieht.
        /// </summary>
        [Fact]
        public void Ein_Kommentar_zaehlt_nicht_als_Umrechnung()
        {
            Assert.True(IstKommentar("            // Q_max [kWh] = Volumen [l] * 1,16 * dT / 1000"));
            Assert.True(IstKommentar("        /// <c>Volumen · 1,16 · ΔT / 1000</c> und"));
            Assert.True(IstKommentar("     * summe / 1000.0"));
            Assert.True(IstKommentar("@* Anteil / 1000 *@"));
            Assert.False(IstKommentar("            return summe / 1000.0;"));
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene
        /// Dateien, und die fünf ausgenommenen Stellen stehen wirklich dort. Ein
        /// leerer Bestand — oder eine Ausnahmeliste, die ins Leere zeigt — liefe sonst
        /// grün durch, ohne je etwas geprüft zu haben.
        /// </summary>
        [Fact]
        public void Der_Anzeigewaechter_sieht_den_Bestand_und_jede_Ausnahme_existiert()
        {
            string[] dateien = Anzeigedateien();
            Assert.True(dateien.Length > 100,
                        "Nur " + dateien.Length + " Anzeigedateien gefunden.");

            foreach (var a in AusnahmenAnzeige)
            {
                bool da = dateien.Any(d => string.Equals(Path.GetFileName(d), a.Datei, StringComparison.Ordinal)
                                        && File.ReadAllText(d).Contains(a.Ausdruck, StringComparison.Ordinal));
                Assert.True(da, "Die Ausnahme '" + a.Ausdruck + "' steht nicht mehr in " + a.Datei +
                                " - sie gehoert aus der Liste entfernt.");
            }
        }

        // =====================================================================
        //  Wächter 2 — die Einheit am Namen der Jahressummen
        // =====================================================================

        /// <summary>
        /// Die sechs Erzeuger- und Speicherklassen des Laufs und
        /// <c>SimulationControl</c>. Genau hier standen die <b>31 Jahressummen</b>, von
        /// denen vor W8‑O‑5c keine einzige ihre Einheit im Namen trug, obwohl einige
        /// kWh und andere MWh führten (Konzept 0.3 und U4).
        /// </summary>
        private static readonly string[] Simulationsklassen =
        {
            "SimulationWaermepumpe.cs", "SimulationSPK.cs", "SimulationBHKW.cs",
            "SimulationSolarthermie.cs", "SimulationPV.cs", "SimulationPufferspeicher.cs",
            "SimulationControl.cs",
        };

        /// <summary>
        /// Ein <c>public</c>/<c>internal</c> SKALARFELD oder eine Property vom Typ
        /// <c>double</c> oder <c>float</c>.
        ///
        /// <para>Die Stundenreihen (<c>double[8760]</c>) sind bewusst
        /// draußen: Sie führen nach der Regel IMMER kWh, sie stehen in 300 der 312
        /// CSV-Dateien der Referenzbasis, und ihr Name ist dort der Dateiname. Eine
        /// Umbenennung würde die Basis kosten, ohne eine Zweideutigkeit zu beheben.</para>
        /// </summary>
        private static readonly Regex Skalarfeld = new Regex(
            @"^\s*(?:public|internal)\s+(?:static\s+)?(?:readonly\s+)?(?:double|float)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;|\{|=>)",
            RegexOptions.Compiled);

        /// <summary>
        /// Die sieben Namensteile, die eine ENERGIEMENGE ankündigen. Wer so heißt, muss
        /// seine Einheit nennen.
        /// </summary>
        private static readonly Regex Energiename = new Regex(
            "bedarf|verbrauch|produktion|ertrag|summe|gesamt|rest",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Die Einheit am Ende des Namens — <c>…Kwh</c> oder <c>…Mwh</c>.</summary>
        private static readonly Regex Einheitensuffix = new Regex(
            "(kwh|mwh)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Die BEGRÜNDETEN Ausnahmen des zweiten Wächters — je Klasse und Feldname,
        /// jede mit ihrem Grund.
        ///
        /// <para>Zwei Gruppen, und keine dritte: <b>(a)</b> fünf Felder, die trotz ihres
        /// Namens keine Energiemenge führen — vier Leistungsspitzen in kW und eine
        /// Vollbenutzungsstundenzahl in h; <b>(b)</b> die acht Jahressummen des
        /// Pufferspeichers, deren Namen als SCHLÜSSEL in den CSV-Dateien der
        /// eingefrorenen Basis stehen (<c>Puffer.Ladung_gesamt</c>,
        /// <c>Puffer.Entladung_gesamt</c>, <c>Puffer.Verluste_gesamt</c> in
        /// <c>Referenzlauf/Ergebnisexport.cs</c>) — dieselbe Regel wie Q7 für
        /// <c>Sim.Restwaerme</c>. Die Klasse rechnet durchgängig kWh, und
        /// <c>Tab_ErgebnisPufferspeicher</c> speichert kWh; es gibt in ihr keine zweite
        /// Einheit, an der man sich vertun könnte.</para>
        /// </summary>
        private static readonly (string Klasse, string Feld, string Grund)[] AusnahmenNamen =
        {
            ("SimulationSPK.cs", "Max_Waermebedarf",
             "LEISTUNGSSPITZE [kW] der Bedarfsreihe, keine Energiemenge."),
            ("SimulationSolarthermie.cs", "Max_Waermebedarf",
             "LEISTUNGSSPITZE [kW] der Bedarfsreihe, keine Energiemenge."),
            ("SimulationSolarthermie.cs", "Waermeproduktion_max",
             "LEISTUNGSSPITZE [kW] der Produktionsreihe, keine Energiemenge."),
            ("SimulationPV.cs", "Stromproduktion_Max",
             "LEISTUNGSSPITZE [kW] der Produktionsreihe, keine Energiemenge."),
            ("SimulationBHKW.cs", "VbhElektrischGesamt",
             "Leistungsgewichtete VOLLBENUTZUNGSSTUNDEN [h/a], keine Energiemenge."),

            ("SimulationPufferspeicher.cs", "Ladung_gesamt",
             "Altfeld: der Name ist der CSV-Schluessel 'Puffer.Ladung_gesamt' der Basis R3 (Q7-Regel). Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Entladung_gesamt",
             "Altfeld: CSV-Schluessel 'Puffer.Entladung_gesamt' der Basis R3 (Q7-Regel). Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Verluste_gesamt",
             "Altfeld: CSV-Schluessel 'Puffer.Verluste_gesamt' der Basis R3 (Q7-Regel). Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Durchsatz_Ladung_gesamt",
             "Geschwister der drei Schluesselfelder; halb umbenannt waere der Block unleserlich. Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Durchsatz_Entladung_gesamt",
             "Geschwister der drei Schluesselfelder. Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Durchsatz_Verluste_gesamt",
             "Geschwister der drei Schluesselfelder. Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Ladung_gesamt_Brutto",
             "Summe zweier Schluesselfelder; traegt deren Namen. Einheit kWh, eindeutig."),
            ("SimulationPufferspeicher.cs", "Entladung_gesamt_Brutto",
             "Summe zweier Schluesselfelder; traegt deren Namen. Einheit kWh, eindeutig."),
        };

        /// <summary>
        /// <b>Wächter 2.</b> Jedes <c>public</c>/<c>internal</c> Skalarfeld und jede
        /// Property vom Typ <c>double</c>/<c>float</c> in den sieben Simulations- und
        /// Ergebnisklassen, deren Name eine Energiemenge ankündigt
        /// (<c>Bedarf</c>, <c>Verbrauch</c>, <c>Produktion</c>, <c>Ertrag</c>,
        /// <c>Summe</c>, <c>gesamt</c>, <c>Rest</c>), endet auf <c>Kwh</c> oder
        /// <c>Mwh</c>.
        ///
        /// <para>Das ist die Lehre aus U4: <c>SimulationSPK</c> führte
        /// <c>Strombedarf_gesamt</c> in kWh und <c>Stromverbrauch_Spk</c> in MWh — beide
        /// <c>double</c>, beide nach Strom benannt, beide Jahressummen. Wer sie addierte,
        /// musste den Rechenweg lesen; die zwei Aufrufer taten es und schrieben einen
        /// HALBEN Teiler in eine Summe.</para>
        /// </summary>
        [Fact]
        public void Jede_Jahressumme_der_Simulationsklassen_traegt_ihre_Einheit_im_Namen()
        {
            var funde = new List<string>();

            foreach (string datei in Simulationsdateien())
            {
                string klasse = Path.GetFileName(datei);
                string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');

                for (int i = 0; i < zeilen.Length; i++)
                {
                    Match m = Skalarfeld.Match(zeilen[i]);
                    if (!m.Success) continue;

                    string feld = m.Groups[1].Value;
                    if (!Energiename.IsMatch(feld)) continue;
                    if (Einheitensuffix.IsMatch(feld)) continue;
                    if (AusnahmenNamen.Any(a => a.Klasse == klasse && a.Feld == feld)) continue;

                    funde.Add(klasse + ":" + (i + 1) + "  " + feld);
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Jahressummen nennen ihre Einheit nicht (Einheitenregel W8-O-5c, Q1). " +
                "Ein Feld, dessen Name eine Energiemenge ankuendigt, endet auf Kwh oder Mwh - " +
                "oder es steht mit Grund in AusnahmenNamen.\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Die Deklarationszeile wird erkannt, das
        /// Einheitensuffix ebenso — und eine Stundenreihe (ein Feld mit <c>[]</c>)
        /// gerade NICHT, weil sie nach der Regel immer kWh führt und in der
        /// Referenzbasis steht.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_Skalarfeld_und_Einheitensuffix()
        {
            Assert.Matches(Skalarfeld, "        public double Waermebedarf_gesamt = 0;");
            Assert.Matches(Skalarfeld, "        public float Restwaerme;");
            Assert.Matches(Skalarfeld, "        internal double Ladung_gesamt = 0;");
            Assert.Matches(Skalarfeld, "        public double Ladung_gesamt_Brutto { get { return 0; } }");

            // Stundenreihen und private Felder sind draussen.
            Assert.DoesNotMatch(Skalarfeld, "        public float[] Waermebedarf = new float[8760];");
            Assert.DoesNotMatch(Skalarfeld, "        private double Kessel_Verbrauch_MWh_Spk;");

            Assert.Matches(Einheitensuffix, "WaermebedarfGesamtKwh");
            Assert.Matches(Einheitensuffix, "StromverbrauchSpkMwh");
            Assert.Matches(Einheitensuffix, "Waermeproduktion_BHKW_MWh");
            Assert.DoesNotMatch(Einheitensuffix, "Waermebedarf_gesamt");
            Assert.DoesNotMatch(Einheitensuffix, "KwhProJahr");

            Assert.Matches(Energiename, "Waermebedarf_gesamt");
            Assert.Matches(Energiename, "Ueberschuss_summe");
            Assert.Matches(Energiename, "Stromverbrauch_Spk");
            Assert.DoesNotMatch(Energiename, "Max_Leistung");
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Die sieben Klassen sind da, sie führen wirklich
        /// benannte Jahressummen, und jede der 13 Ausnahmen zeigt auf ein Feld, das es
        /// noch gibt. Eine Ausnahmeliste, die ins Leere zeigt, ist eine stille Lücke.
        /// </summary>
        [Fact]
        public void Der_Namenswaechter_sieht_den_Bestand_und_jede_Ausnahme_existiert()
        {
            string[] dateien = Simulationsdateien();
            Assert.Equal(Simulationsklassen.Length, dateien.Length);

            int benannt = 0;
            foreach (string datei in dateien)
                foreach (string zeile in File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n'))
                {
                    Match m = Skalarfeld.Match(zeile);
                    if (m.Success && Einheitensuffix.IsMatch(m.Groups[1].Value)) benannt++;
                }

            Assert.True(benannt >= 30,
                        "Nur " + benannt + " benannte Jahressummen gefunden (erwartet: mindestens 30).");

            foreach (var a in AusnahmenNamen)
            {
                string datei = dateien.Single(d => string.Equals(Path.GetFileName(d), a.Klasse, StringComparison.Ordinal));
                bool da = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n')
                              .Select(z => Skalarfeld.Match(z))
                              .Any(m => m.Success && m.Groups[1].Value == a.Feld);
                Assert.True(da, "Die Ausnahme " + a.Klasse + "." + a.Feld +
                                " gibt es nicht mehr - sie gehoert aus der Liste entfernt.");
            }
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Der Pfad ab der Wurzel des Arbeitsbaums — das nennt die Meldung.</summary>
        private static string Kurzname(string datei)
        {
            string wurzel = Arbeitsbaum();
            return datei.StartsWith(wurzel, StringComparison.Ordinal)
                 ? datei.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar)
                       .Replace(Path.DirectorySeparatorChar, '/')
                 : datei;
        }

        private static bool IstAusgenommen(string datei, string zeile)
        {
            string name = Path.GetFileName(datei);
            return AusnahmenAnzeige.Any(a => string.Equals(a.Datei, name, StringComparison.Ordinal)
                                          && zeile.Contains(a.Ausdruck, StringComparison.Ordinal));
        }

        /// <summary>
        /// Steht die Zeile in einem Kommentar? Die Nachweise im Bestand nennen die
        /// Formeln absichtlich mit ihrem Faktor; geprüft wird nur, was rechnet.
        /// </summary>
        private static bool IstKommentar(string zeile)
        {
            string s = zeile.TrimStart();
            return s.StartsWith("//", StringComparison.Ordinal)
                || s.StartsWith("*", StringComparison.Ordinal)
                || s.StartsWith("/*", StringComparison.Ordinal)
                || s.StartsWith("@*", StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Dateien, über die Wächter 1 läuft: <c>EPOS.UI</c>, die plattformfreie
        /// Datenseite <c>EPOS.UI.Daten</c> (seit Auftrag #208 — die Simulationshüllen
        /// liegen dort, und mit ihnen die drei begründeten Ausnahmen dieser Liste) und
        /// die Windows-Ansichten.
        /// </summary>
        private static string[] Anzeigedateien()
        {
            string wurzel = Arbeitsbaum();
            var ordner = new[]
            {
                Path.Combine(wurzel, "EPOS.UI"),
                Path.Combine(wurzel, "EPOS.UI.Daten"),
                Path.Combine(wurzel, "WindowsFormsApplication1", "Views"),
            };

            var dateien = new List<string>();
            foreach (string o in ordner)
            {
                Assert.True(Directory.Exists(o), "Ordner nicht gefunden: " + o);
                dateien.AddRange(Directory.GetFiles(o, "*.cs", SearchOption.AllDirectories));
                dateien.AddRange(Directory.GetFiles(o, "*.razor", SearchOption.AllDirectories));
            }

            return dateien.Where(OhneBauordner).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }

        /// <summary>Die sieben Quelldateien, über die Wächter 2 läuft.</summary>
        private static string[] Simulationsdateien()
        {
            string ordner = Path.Combine(Arbeitsbaum(), "EPOS.Kern", "Allgemein", "Simulation");
            Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);

            return Simulationsklassen
                   .Select(k => Path.Combine(ordner, k))
                   .Select(p => { Assert.True(File.Exists(p), "Datei nicht gefunden: " + p); return p; })
                   .OrderBy(p => p, StringComparer.Ordinal)
                   .ToArray();
        }

        private static bool OhneBauordner(string pfad)
        {
            char t = Path.DirectorySeparatorChar;
            return pfad.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0
                && pfad.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0;
        }

        /// <summary>
        /// Die Wurzel des Arbeitsbaums. Der Weg dorthin führt über
        /// <see cref="CallerFilePathAttribute"/> — die Datei kennt ihren eigenen Ort.
        /// Steht der Quelltext nicht dort (ein Lauf aus verschobenen Binärdateien), wird
        /// wie in den anderen Wächtern vom Ausgabeordner aufwärts gesucht.
        /// </summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
