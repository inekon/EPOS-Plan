using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E1 — der <b>Formatpaar-Wächter</b> der Kennzahlentabelle
    /// (Analysepapier 2026-09-19, Protokoll 05/§ 6.2).
    ///
    /// <para><b>Worum es geht.</b> Jede <see cref="WirtZeile"/> trägt ZWEI
    /// Formatangaben nebeneinander: <see cref="WirtZeile.Format"/> ist das
    /// .NET-Zahlformat für Word und den Ergebnisreiter, <see cref="WirtZeile.ExcelFormat"/>
    /// das Zellformat für Excel. Sie beschreiben dieselbe Genauigkeit in zwei
    /// Sprachen und müssen deshalb paarweise zusammenpassen — „N2" gehört zu
    /// „#,##0.00", nichts anderes.</para>
    ///
    /// <para><b>Warum ein Wächter.</b> Die Vorgabe ist das Paar N0/#,##0; nur vier
    /// Zeilen weichen ab und setzen BEIDE Felder von Hand (Muster
    /// <c>WirtschaftlichkeitZeilen.cs:479/619/625/631</c>). Wer eine fünfte Zeile
    /// ergänzt und nur <c>Format</c> anfasst, bekommt eine Tabelle, die in Word drei
    /// Nachkommastellen zeigt und in Excel keine — ein Fehler, den kein Zahlenanker
    /// findet, weil die WERTE stimmen. Dieser Fall findet ihn.</para>
    ///
    /// <para>Geprüft wird über die Zeilen, die
    /// <see cref="WirtschaftlichkeitZeilen.Kennzahlen(IList{WirtschaftlichkeitErgebnis}, TarifParameter)"/>
    /// liefert — mit ausdrücklicher Referenz, ohne, sowie gefiltert durch
    /// <see cref="WirtschaftlichkeitZeilen.Sichtbare"/>; und zwar sowohl über eine
    /// synthetische Menge, die möglichst viele Zeilen aufspannt, als auch über die
    /// echten Ergebnisse des BHKW-Referenzprojekts.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WirtZeileFormatWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT_BHKW = 1030;

        /// <summary>
        /// Die erlaubten Paare (Protokoll 05/§ 6.2). Links das .NET-Zahlformat für
        /// Word und Reiter, rechts das Excel-Zellformat.
        /// </summary>
        private static readonly Dictionary<string, string> PAARE =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "N0", "#,##0" },
                { "N1", "#,##0.0" },
                { "N2", "#,##0.00" },
                { "N3", "#,##0.000" },
                { "N4", "#,##0.0000" },
            };

        // =====================================================================
        //  Die Prüfung
        // =====================================================================

        private static void PruefePaare(IEnumerable<WirtZeile> zeilen, string woher)
        {
            foreach (WirtZeile z in zeilen)
            {
                Assert.True(PAARE.ContainsKey(z.Format),
                    woher + ": Zeile „" + z.Schluessel + "\" trägt das unbekannte Zahlformat „"
                    + z.Format + "\". Erlaubt sind " + string.Join(", ", PAARE.Keys) + ".");

                Assert.True(string.Equals(PAARE[z.Format], z.ExcelFormat, StringComparison.Ordinal),
                    woher + ": Zeile „" + z.Schluessel + "\" trägt Format „" + z.Format
                    + "\", dazu gehört das Excel-Format „" + PAARE[z.Format]
                    + "\" — gefunden wurde aber „" + z.ExcelFormat + "\".");
            }
        }

        // =====================================================================
        //  Die Mengen
        // =====================================================================

        /// <summary>
        /// Eine synthetische Ergebnismenge, die möglichst viele Zeilen aufspannt:
        /// Stamm und Variante, alle drei Szenarien, und je Stand die Größen belegt,
        /// an denen die vier abweichenden Formate hängen (anzulegender Wert N2,
        /// Amortisation und interner Zinsfuß N1, Gestehungskosten N3).
        /// </summary>
        private static List<WirtschaftlichkeitErgebnis> Menge()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            foreach (bool stamm in new[] { true, false })
                foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
                    alle.Add(Stand(stamm ? 1000 : 1001, stamm, szenario));
            return alle;
        }

        private static WirtschaftlichkeitErgebnis Stand(int id, bool stamm, string szenario)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = id,
                IstStamm = stamm,
                Szenario = szenario,
                Anzeige = stamm ? "Stammprojekt" : "Variante A",

                Investition = 250000.0,
                Zuschuss = 10000.0,
                BetriebskostenJahr = 8000.0,
                EnergiekostenJahr = 120000.0,
                EinspeiseerloesJahr = 9000.0,
                EinspeiseerloesPvJahr = 6000.0,
                EinspeiseerloesKwkJahr = 3000.0,
                BarwertAusgaben = 1800000.0,
                BarwertEinnahmen = 130000.0,
                RestwertBarwert = 20000.0,
                ErsatzBarwert = 35000.0,
                CO2AbgabeJahr = 4500.0,

                KwkgErloesJahr1 = 22000.0,
                KwkgVbhElektrisch = 5500.0,
                KwkgPauschaleEur = 1500.0,

                EnergiesteuerJahr1 = 21202.71,
                StromsteuerBefreiungJahr1 = 3400.0,
                StromsteuerEntlastungJahr1 = 900.0,
                SteuerHerkunft = "Probe",
                ProduzierendesGewerbe = true,

                VermiedenArbeitJahr = 1200.0,
                VermiedenLeistungJahr = 800.0,
                VermiedenGesamtJahr = 2000.0,
                VermiedenMengeMWh = 150.0,
                VermiedenEntlastung9bJahr = 250.0,
                AufschlagJahr = 700.0,

                PvVerguetungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE,
                PvAnzulegenderWert = 6.04,          // -> Format N2
                PvMarktpraemie = 2457.84,
                PvVerguetungsausfallKwh = 1200.0,
                PvVerguetungsausfall = 96.0,
                PvKompensation51a = 1095.52,
                PvKappungsverlustKwh = 340.0,
                PvVermiedenerBezug = 5000.0,

                StromkostenTarif = 118000.0,
                BezugsspitzeKW = 320.0,

                Kapitalwert = stamm ? -2200000.0 : -2100000.0,
                KapitalwertDiff = stamm ? (double?)null : 100000.0,
                AnnuitaetKW = stamm ? (double?)null : 6700.0,
                AmortisationJahre = stamm ? (double?)null : 12.4,   // -> Format N1
                IRR = stamm ? (double?)null : 4.7,                  // -> Format N1
                Gestehungskosten = 0.123,                           // -> Format N3
            };
        }

        /// <summary>Die echten Ergebnisse des BHKW-Referenzprojekts.</summary>
        private static List<WirtschaftlichkeitErgebnis> EchteMenge()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_BHKW);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT_BHKW,
                IstStamm = true,
                Projektname = "Formatwache " + PROJEKT_BHKW,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT_BHKW)
            };
            KostenEmissionRechner.Berechne(v);

            var daten = new BerichtsDaten { IdStamm = PROJEKT_BHKW, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p);
        }

        // =====================================================================
        //  Die Fälle
        // =====================================================================

        [Fact]
        public void Jede_Kennzahlenzeile_traegt_das_passende_Excel_Format()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> menge = Menge();

            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(menge, null);
            Assert.NotEmpty(zeilen);
            PruefePaare(zeilen, "Kennzahlen(ohne Referenz)");

            PruefePaare(WirtschaftlichkeitZeilen.Kennzahlen(menge, null, 1001),
                        "Kennzahlen(Referenz 1001)");

            PruefePaare(WirtschaftlichkeitZeilen.Sichtbare(zeilen, menge), "Sichtbare");
        }

        /// <summary>
        /// Die Wache liefe ins Leere, wenn alle Zeilen die Vorgabe N0/#,##0 trügen —
        /// dann prüfte sie nur den Feldinitialisierer. Dieser Fall belegt, dass die
        /// geprüfte Menge WIRKLICH mehrere Genauigkeiten aufspannt.
        /// </summary>
        [Fact]
        public void Die_geprueften_Zeilen_spannen_mehrere_Genauigkeiten_auf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(Menge(), null);

            HashSet<string> formate = zeilen.Select(z => z.Format).ToHashSet(StringComparer.Ordinal);

            Assert.Contains("N0", formate);
            Assert.Contains("N1", formate);   // Amortisation, interner Zinsfuß
            Assert.Contains("N2", formate);   // anzulegender Wert
            Assert.Contains("N3", formate);   // Gestehungskosten
        }

        [Fact]
        public void Auch_die_Zeilen_eines_echten_Projekts_tragen_passende_Paare()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> menge = EchteMenge();
            Assert.NotEmpty(menge);

            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(menge, null);
            Assert.NotEmpty(zeilen);

            PruefePaare(zeilen, "Kennzahlen(Projekt " + PROJEKT_BHKW + ")");
            PruefePaare(WirtschaftlichkeitZeilen.Sichtbare(zeilen, menge),
                        "Sichtbare(Projekt " + PROJEKT_BHKW + ")");
        }

        /// <summary>
        /// Die Vorgabe des Feldinitialisierers ist selbst ein gültiges Paar — sonst
        /// wäre jede neue Zeile von Geburt an falsch.
        /// </summary>
        [Fact]
        public void Die_Vorgabe_einer_frischen_Zeile_ist_ein_gueltiges_Paar()
        {
            var frisch = new WirtZeile();

            Assert.Equal("N0", frisch.Format);
            Assert.Equal("#,##0", frisch.ExcelFormat);
            PruefePaare(new[] { frisch }, "Vorgabe");
        }
    }
}
