using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Berichtsbäume der Berichtswachen</b> — EINE Quelle für
    /// <see cref="WordBerichtSvgWacheTests"/> und <see cref="BerichtVorlagenMesslatteTests"/>
    /// (Konzept Berichtsvorlagen, Etappe BV-E0). Wer eine Probe ändert, ändert sie für jede
    /// Wache, die sie liest; eine Kopie gibt es nicht.
    ///
    /// <para><b>Zwei Proben.</b> Das Referenzprojekt 1030 der Testdatenbank, frisch
    /// simuliert — Zeitreihen aus dem Lauf, Ergebniszeilen aus der Datenbank —, und eine
    /// synthetische Gruppe: der Stamm <see cref="STAMM"/> und die Varianten darüber, jede mit
    /// dem Zeitreihensatz der Gruppe (d), einer Speicherzeile und eigenen Energiekosten.
    /// Die synthetischen Kennungen stehen NICHT in <c>Tab_Projekt</c>; was projektgebunden
    /// gespeichert würde, scheitert dort am Fremdschlüssel — die Proben speichern deshalb
    /// nichts.</para>
    ///
    /// <para><b>Die Wirtschaftlichkeit</b> hängen <see cref="MitWirtschaftlichkeit"/> und
    /// <see cref="MitWirkungen"/> an einen Baum: Rechnung ohne Speichern, die Bewertung des
    /// Laufs und zwei nicht monetarisierbare Wirkungen. Erst damit schreibt der Wortbericht im
    /// Kapitel „Wirtschaftlichkeit“ die Tabelle „Nicht monetarisierbare Wirkungen“ — ihre
    /// Quelle ist <see cref="WirtschaftlichkeitBewertung.Wirkungen"/>, nicht das Ergebnis der
    /// Rechnung.</para>
    /// </summary>
    internal static class Berichtsdatenproben
    {
        /// <summary>Das Referenzprojekt der Testdatenbank — der Bericht, den ein Anwender bekommt.</summary>
        internal const int PROJEKT_1030 = 1030;

        /// <summary>Der synthetische Stamm der Gruppe; die Varianten zählen von hier aufwärts.</summary>
        internal const int STAMM = 9101;

        /// <summary>
        /// Der Berichtsbaum des Referenzprojekts 1030: frisch simuliert, Zeitreihen
        /// aus dem Lauf, Ergebniszeilen aus der Datenbank — derselbe Weg, den der
        /// <c>BerichtsDatenSammler</c> der Schale geht.
        /// </summary>
        internal static BerichtsDaten Projektdaten1030()
        {
            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_1030, out fehler), "Lauf gescheitert: " + fehler);

            var daten = new BerichtsDaten { IdStamm = PROJEKT_1030, Stammprojektname = "Referenzprojekt 1030" };
            daten.Varianten.Add(new VariantenDaten
            {
                IdProjekt = PROJEKT_1030,
                IstStamm = true,
                Projektname = daten.Stammprojektname,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT_1030) ?? new ErgebnisModel(),
                Zeitreihen = ZeitreihenExtraktor.AusLauf(laeufer)
            });
            return daten;
        }

        /// <summary>
        /// Die synthetische Gruppe mit Ganglinien: <paramref name="staende"/> Stände, der
        /// erste ist der Stamm, die übrigen heißen „Variante A“, „Variante B“ … Jeder Stand
        /// trägt einen Zeitreihensatz; die Energiekosten fallen je Stand um 1 000 € (Stamm
        /// 12 000 €), damit die Wirtschaftlichkeit Differenzen hat.
        /// </summary>
        internal static BerichtsDaten Gruppendaten(int staende = 2)
        {
            if (staende < 1 || staende > 27)
                throw new ArgumentOutOfRangeException(nameof(staende), staende, "1 bis 27 Stände");

            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            for (int i = 1; i < staende; i++)
                daten.Varianten.Add(Stand(STAMM + i, false, "Variante " + (char)('A' + i - 1),
                                          12000.0 - 1000.0 * i));
            return daten;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energiekosten)
        {
            var ergebnis = new ErgebnisModel();

            // Eine Speicherzeile MIT Temperaturkennzahl — sonst lässt der Baustein
            // „Projektbeschreibung" den Abschnitt samt Bild aus.
            ergebnis.Pufferspeicher.Add(new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 11,
                Bezeichner = "Heizungspuffer",
                Verwendung = "Heizung",
                T_oben_Mittel = 62.0,
                T_oben_Min = 48.0
            });

            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = ergebnis,
                Energiekosten = energiekosten,
                Zeitreihen = ChartRendererGruppeDTests.Satz()
            };
        }

        /// <summary>Alle Bausteine an — sonst fehlte gerade die Stelle, um die es geht.</summary>
        internal static BerichtsKonfiguration VolleKonfiguration()
        {
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                k.AktiveBausteine.Add(d.Schluessel);
            return k;
        }

        /// <summary>
        /// Der Parametersatz der synthetischen Gruppe (Muster
        /// <c>BerichtBlattstrukturWacheTests</c>): i 3 %, T 20 a, keine Preissteigerung,
        /// Referenz Stamm.
        /// </summary>
        internal static WirtschaftlichkeitParameter Parametersatz(int idStamm)
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = idStamm,
                IdReferenzprojekt = 0,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0
            };
        }

        /// <summary>
        /// Die Wirtschaftlichkeit des Baums, gerechnet OHNE Speichern gegen den Stamm, und die
        /// Bewertung des Laufs (<see cref="WirtschaftlichkeitBewertung.FuerBericht(BerichtsDaten, IEnumerable{WirtschaftlichkeitErgebnis}, WirtschaftlichkeitParameter, System.Globalization.CultureInfo, IEnumerable{SensitivitaetZeile})"/>)
        /// mit den Sensitivitätszeilen dieses Laufs — derselbe Schritt, den der Sammler nach
        /// der Simulation geht, nur ohne Buchung.
        /// </summary>
        internal static void MitWirtschaftlichkeit(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            List<SensitivitaetZeile> sens;
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, p, 0, false, out sens);
            daten.Bewertung = WirtschaftlichkeitBewertung.FuerBericht(daten, daten.Wirtschaftlichkeit, p,
                                                                      BerichtTexte.Kultur, sens);
        }

        /// <summary>
        /// Legt die zwei Wirkungen der <see cref="Wirkungsprobe"/> an die Bewertung des Baums —
        /// dieselbe Stelle, an die <c>FuerBericht</c> die Wirkungen des Stammprojekts aus
        /// <c>Tab_ProjektWirkung</c> legt, samt der Deklarationszeile, die sie daraus bildet.
        /// Unmittelbar statt über die Tabelle, weil die synthetischen Stände dort keine Zeile
        /// haben dürfen. Setzt <see cref="MitWirtschaftlichkeit"/> voraus.
        /// </summary>
        internal static void MitWirkungen(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            Assert.NotNull(daten.Bewertung);
            List<ProjektWirkung> wirkungen = Wirkungsprobe();
            daten.Bewertung.Wirkungen = wirkungen;
            daten.Bewertung.Deklarationen = ValeriAusweis.Deklarationen(NichtMonetaereWirkungen.Kurztext(wirkungen), p);
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts vom Testausgabeordner gesucht; sonst <c>null</c>.</summary>
        internal static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }

        /// <summary>Die Berichtsvorlage, repo-relativ.</summary>
        internal const string VORLAGE_REPO = "WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx";

        /// <summary>
        /// Die ECHTE Berichtsvorlage des Repositoriums — dieselbe Datei, die die Schale neben
        /// die EXE legt. <c>null</c> ohne Repositorium (Lauf außerhalb des Arbeitsbaums).
        /// </summary>
        internal static string Berichtsvorlage()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, VORLAGE_REPO.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(pfad), "Die Berichtsvorlage fehlt im Repositorium: " + VORLAGE_REPO);
            return pfad;
        }

        /// <summary>Zwei Wirkungen — eine beurteilt (lang × mittel = 6), eine nur beschrieben.</summary>
        internal static List<ProjektWirkung> Wirkungsprobe() => new List<ProjektWirkung>
        {
            new ProjektWirkung { Kategorie = NichtMonetaereWirkungen.ENERGIEFLUSS, Beschreibung = "Versorgungssicherheit",
                                 Dauer = 3, WirkungOrganisation = 2, WirkungUmwelt = 1 },
            new ProjektWirkung { Kategorie = NichtMonetaereWirkungen.SONSTIG, Beschreibung = "Außenwirkung" }
        };
    }
}
