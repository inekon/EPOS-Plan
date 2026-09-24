using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E8b (U43; Register Q18, Konzept § 2.11.2 V‑G12) — die <b>Anhang-E-Checkliste</b>
    /// der DIN EN 17463: 15 Punkte in fünf Gruppen, je Punkt Anforderung, Stelle im Bericht
    /// und ein Stand, der aus der LAGE des Laufs folgt (<see cref="ChecklistenLage"/>) statt
    /// behauptet zu werden. Wortbericht, Mappe und Ergebnisseite lesen dieselben Punkte
    /// (<see cref="AnhangECheckliste.Punkte"/>); die Beurteilung 1–5 vergibt der Prüfer.
    ///
    /// <para>Ohne Datenbank: Die Punkte hängen allein an der Lage; das Blatt der Mappe
    /// wird in eine leere Mappe geschrieben, gespeichert und wieder gelesen. Wo die
    /// Checkliste im ganzen Bericht steht (letztes Blatt, Abschlussseite), hält
    /// <see cref="BerichtBlattstrukturWacheTests"/>.</para>
    /// </summary>
    public class AnhangEChecklisteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly string[] NUMMERN =
            { "0.1", "0.2", "1", "2a", "2b", "3a", "3b", "4", "5", "6", "7", "8", "9", "10", "11" };

        private static ChecklistenPunkt Punkt(ChecklistenLage lage, string nummer)
            => AnhangECheckliste.Punkte(lage).Single(p => p.Nummer == nummer);

        private static string[] MitStand(ChecklistenLage lage, ChecklistenStand stand)
            => AnhangECheckliste.Punkte(lage).Where(p => p.Stand == stand).Select(p => p.Nummer).ToArray();

        private static ChecklistenLage VolleLage() => new ChecklistenLage
        {
            Gerechnet = true,
            NichtMonetaerErfasst = true,
            ZeitraumBegruendet = true,
            PositionenOhneNutzungsdauer = false,
            SensitivitaetGerechnet = true,
            SzenarienGerechnet = true,
            VorschlagVorhanden = true
        };

        [Fact]
        public void Die_Checkliste_traegt_fuenfzehn_Punkte_in_Normreihenfolge()
        {
            var punkte = AnhangECheckliste.Punkte(new ChecklistenLage());

            Assert.Equal(AnhangECheckliste.ANZAHL, punkte.Count);
            Assert.Equal(NUMMERN, punkte.Select(p => p.Nummer).ToArray());
            Assert.Equal(new[] { R.WIRT_AE_GRUPPE_0, R.WIRT_AE_GRUPPE_A, R.WIRT_AE_GRUPPE_B,
                                 R.WIRT_AE_GRUPPE_C, R.WIRT_AE_GRUPPE_D },
                         punkte.Select(p => p.Gruppe).Distinct().ToArray());
            Assert.Equal(new[] { "7", "8", "9" },
                         punkte.Where(p => p.Gruppe == R.WIRT_AE_GRUPPE_B).Select(p => p.Nummer).ToArray());

            // Jeder Punkt nennt Thema, Anforderung, die Stelle im Bericht und was EPOS liefert.
            Assert.All(punkte, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Thema), p.Nummer);
                Assert.False(string.IsNullOrWhiteSpace(p.Anforderung), p.Nummer);
                Assert.False(string.IsNullOrWhiteSpace(p.Stelle), p.Nummer);
                Assert.False(string.IsNullOrWhiteSpace(p.StandText), p.Nummer);
            });

            // Die Stelle nennt beide Berichte.
            Assert.All(punkte, p => Assert.Contains("Wortbericht", p.Stelle));
            Assert.All(punkte, p => Assert.Contains("Tabellenbericht", p.Stelle));
        }

        /// <summary>Ohne Rechnung behauptet die Checkliste nichts, was ein Lauf liefern
        /// müsste: Die rechnenden Punkte sind offen und sagen, was fehlt.</summary>
        [Fact]
        public void Ohne_Rechnung_sind_die_rechnenden_Punkte_offen()
        {
            var leer = new ChecklistenLage();

            Assert.Equal(new[] { "1", "2b", "3a", "3b", "4", "7", "8", "9", "10" }, MitStand(leer, ChecklistenStand.Offen));
            Assert.Equal(new[] { "0.2", "5", "6" }, MitStand(leer, ChecklistenStand.Teilweise));
            Assert.Equal(new[] { "0.1", "2a", "11" }, MitStand(leer, ChecklistenStand.Erfuellt));

            Assert.Equal(R.WIRT_AE_OHNE_RECHNUNG, Punkt(leer, "1").StandText);
            Assert.Equal(R.WIRT_AE_OHNE_RECHNUNG, Punkt(leer, "7").StandText);
            Assert.Equal(R.WIRT_AE_OHNE_RECHNUNG, Punkt(leer, "4").StandText);   // Zahlungszeitpunkte zeigt erst ein Lauf
            Assert.Equal(R.WIRT_AE_NM_OFFEN, Punkt(leer, "2b").StandText);
            Assert.Equal(R.WIRT_AE_8_OFFEN, Punkt(leer, "8").StandText);
        }

        /// <summary>Mit allem, was EPOS liefern kann, ist kein Punkt mehr offen — und sechs
        /// bleiben „teilweise": Was EPOS nicht erfasst, steht im Standtext.</summary>
        [Fact]
        public void Die_volle_Lage_hebt_jeden_Punkt_so_weit_EPOS_ihn_liefert()
        {
            var voll = VolleLage();

            Assert.Empty(MitStand(voll, ChecklistenStand.Offen));
            Assert.Equal(new[] { "0.1", "1", "2a", "3a", "4", "7", "9", "10", "11" }, MitStand(voll, ChecklistenStand.Erfuellt));
            Assert.Equal(new[] { "0.2", "2b", "3b", "5", "6", "8" }, MitStand(voll, ChecklistenStand.Teilweise));
            Assert.Equal(R.WIRT_AE_NM_TEILWEISE, Punkt(voll, "3b").StandText);
            Assert.Equal(R.WIRT_AE_10_ERFUELLT, Punkt(voll, "10").StandText);
        }

        /// <summary>Punkt 4 verlangt den Abgleich OHNE gemeldete Position ohne
        /// Nutzungsdauer; Punkt 10 den Vorschlag UND die Szenarien.</summary>
        [Fact]
        public void Zeitraum_und_Vorschlag_verlangen_beide_Bedingungen()
        {
            var lage = VolleLage();
            lage.PositionenOhneNutzungsdauer = true;
            Assert.Equal(ChecklistenStand.Teilweise, Punkt(lage, "4").Stand);
            Assert.Equal(R.WIRT_AE_4_TEILWEISE, Punkt(lage, "4").StandText);

            lage = VolleLage();
            lage.ZeitraumBegruendet = false;
            Assert.Equal(ChecklistenStand.Teilweise, Punkt(lage, "4").Stand);

            lage = VolleLage();
            lage.SzenarienGerechnet = false;
            Assert.Equal(ChecklistenStand.Offen, Punkt(lage, "10").Stand);
            Assert.Equal(ChecklistenStand.Teilweise, Punkt(lage, "9").Stand);   // Erwartet ist gerechnet

            lage = VolleLage();
            lage.VorschlagVorhanden = false;
            Assert.Equal(ChecklistenStand.Offen, Punkt(lage, "10").Stand);
        }

        /// <summary>Die Szenarioanalyse (Punkt 9) verlangt Ungünstig UND Günstig mit Zahl —
        /// eine Bandbreite, deren Zeilen nur „—" tragen, zählt nicht (sie entsteht je Stand,
        /// auch ohne Rechnung).</summary>
        [Fact]
        public void Die_Lage_des_Berichts_verlangt_Unguenstig_und_Guenstig_mit_Zahl()
        {
            var bewertung = new WirtschaftlichkeitBewertung();
            Assert.False(ChecklistenLage.AusBericht(null, null, bewertung).SzenarienGerechnet);

            bewertung.Bandbreite.Zeilen.Add(new BandbreitenZeile { IdProjekt = 2, Erwartet = 1000.0 });
            bewertung.Bandbreite.Zeilen.Add(new BandbreitenZeile { IdProjekt = 3, Worst = -500.0, Erwartet = 800.0 });
            Assert.False(bewertung.Bandbreite.Leer);
            Assert.False(ChecklistenLage.AusBericht(null, null, bewertung).SzenarienGerechnet);
            Assert.True(ChecklistenLage.AusBericht(null, null, bewertung).EinSzenarioGerechnet);

            bewertung.Bandbreite.Zeilen.Add(new BandbreitenZeile { IdProjekt = 4, Worst = -500.0, Erwartet = 800.0, Best = 2500.0 });
            Assert.True(ChecklistenLage.AusBericht(null, null, bewertung).SzenarienGerechnet);
        }

        /// <summary>
        /// ETAPPE E13 (E9b‑Q5 b) — Punkt 9 (Szenarioanalyse) in drei Fällen: „offen" ohne Lauf;
        /// „teilweise" mit nur Erwartet oder nur einem der Szenarien Günstig und Ungünstig;
        /// „erfüllt", sobald beide mit Kapitalwert gerechnet sind — auch ohne einen einzigen
        /// szenarierten Parameter. Der Ausweis „n von m Parametern szenariert" bleibt Beleg.
        /// </summary>
        [Fact]
        public void Punkt_9_ist_offen_teilweise_oder_erfuellt()
        {
            ChecklistenPunkt offen = Punkt(new ChecklistenLage(), "9");
            Assert.Equal(ChecklistenStand.Offen, offen.Stand);
            Assert.Equal(R.WIRT_AE_9_OFFEN, offen.StandText);

            ChecklistenPunkt nurErwartet = Punkt(new ChecklistenLage { Gerechnet = true }, "9");
            Assert.Equal(ChecklistenStand.Teilweise, nurErwartet.Stand);
            Assert.Equal(R.WIRT_AE_9_TEILWEISE, nurErwartet.StandText);

            ChecklistenPunkt einSzenario = Punkt(new ChecklistenLage
            {
                Gerechnet = true, EinSzenarioGerechnet = true,
                Szenarioabdeckung = "1 von 11 Parametern szenariert: Arbeitspreis Erdgas E"
            }, "9");
            Assert.Equal(ChecklistenStand.Teilweise, einSzenario.Stand);
            Assert.Equal(string.Format(R.WIRT_AE_9_TEILWEISE_ABDECKUNG, "1 von 11 Parametern szenariert: Arbeitspreis Erdgas E"),
                         einSzenario.StandText);

            // Beide Szenarien gerechnet, kein Parameter szenariert: trotzdem „erfüllt".
            ChecklistenPunkt erfuellt = Punkt(new ChecklistenLage
            {
                Gerechnet = true, EinSzenarioGerechnet = true, SzenarienGerechnet = true,
                Szenarioabdeckung = "0 von 11 Parametern szenariert"
            }, "9");
            Assert.Equal(ChecklistenStand.Erfuellt, erfuellt.Stand);
            Assert.Equal("erfüllt: drei vollständige Läufe, je Szenario mit eigenem Parametersatz; "
                         + "0 von 11 Parametern szenariert.", erfuellt.StandZeile);

            // Kein Text von Punkt 9 behauptet mehr, Zeitraum und Mengen blieben unverändert.
            foreach (string text in new[] { R.WIRT_AE_9_OFFEN, R.WIRT_AE_9_TEILWEISE, R.WIRT_AE_9_ERFUELLT,
                                            R.WIRT_AE_9_ABDECKUNG, R.WIRT_AE_9_TEILWEISE_ABDECKUNG })
            {
                Assert.DoesNotContain("unverändert", text);
                Assert.DoesNotContain("Mengen", text);
            }
        }

        /// <summary>
        /// ETAPPE E13 — das Blatt „Checkliste Anhang E" der Mappe zeigt für Punkt 9 denselben
        /// Stand wie die Punkte selbst: „erfüllt" mit beiden Szenarien.
        /// </summary>
        [Fact]
        public void Das_Blatt_der_Mappe_zeigt_Punkt_9_mit_demselben_Stand()
        {
            var lage = new ChecklistenLage { Gerechnet = true, EinSzenarioGerechnet = true, SzenarienGerechnet = true };
            string erwartet = Punkt(lage, "9").StandZeile;
            Assert.StartsWith(R.WIRT_AE_STAND_ERFUELLT + ": ", erwartet);

            using var wb = new XLWorkbook();
            AnhangECheckliste.SchreibeExcel(wb, AnhangECheckliste.Punkte(lage));
            IXLWorksheet ws = wb.Worksheet("Checkliste Anhang E");
            Assert.Equal(erwartet, Enumerable.Range(5, 20).Where(z => ws.Cell(z, 1).GetString() == "9")
                                             .Select(z => ws.Cell(z, 5).GetString()).Single());
        }

        /// <summary>ETAPPE E14 — Punkt 11 (Nachvollziehbarkeit) nennt die Formelbasis: Die
        /// Mappe rechnet alle drei Szenarien formelbasiert; der Punkt ist erfüllt.</summary>
        [Fact]
        public void Punkt_11_nennt_alle_drei_Szenarien_formelbasiert()
        {
            ChecklistenPunkt p = Punkt(new ChecklistenLage(), "11");
            Assert.Equal(ChecklistenStand.Erfuellt, p.Stand);
            Assert.Equal(R.WIRT_AE_11_STAND, p.StandText);
            Assert.Contains("alle drei Szenarien formelbasiert", p.StandText);
        }

        /// <summary>Die Zelle „Stand in EPOS" ist in allen drei Darstellungen dieselbe
        /// Zeile: Wort, Doppelpunkt, Erläuterung.</summary>
        [Fact]
        public void Die_Standzeile_verbindet_Wort_und_Erlaeuterung()
        {
            ChecklistenPunkt p = Punkt(new ChecklistenLage(), "2b");
            Assert.Equal("offen", p.StandWort);
            Assert.Equal("offen: " + R.WIRT_AE_NM_OFFEN, p.StandZeile);
            Assert.Equal("erfüllt", Punkt(new ChecklistenLage(), "0.1").StandWort);
            Assert.Equal("teilweise", Punkt(new ChecklistenLage(), "0.2").StandWort);
        }

        /// <summary>
        /// Das Blatt der Mappe: Titel, Hinweis, Kopf in Zeile 4, die fünf Gruppenzeilen
        /// zwischen den 15 Punkten, und die Spalte „Beurteilung 1–5" nimmt nur ganze Zahlen
        /// von 1 bis 5 an (Gültigkeitsprüfung mit Fehlermeldung).
        /// </summary>
        [Fact]
        public void Das_Blatt_der_Mappe_traegt_Kopf_Gruppen_und_die_Notenpruefung()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-anhange-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string ziel = Path.Combine(ordner, "checkliste.xlsx");
                using (var wb = new XLWorkbook())
                {
                    AnhangECheckliste.SchreibeExcel(wb, AnhangECheckliste.Punkte(new ChecklistenLage { Gerechnet = true }));
                    wb.SaveAs(ziel);
                }

                using var gelesen = new XLWorkbook(ziel);
                IXLWorksheet ws = gelesen.Worksheet("Checkliste Anhang E");
                Assert.Equal(R.WIRT_AE_TITEL, ws.Cell(1, 1).GetString());
                Assert.Equal(R.WIRT_AE_HINWEIS, ws.Cell(2, 1).GetString());
                Assert.Equal(new[] { "Nr.", "Thema", "Anforderung", "Stelle im Bericht", "Stand in EPOS", "Beurteilung 1–5" },
                             Enumerable.Range(1, 6).Select(c => ws.Cell(4, c).GetString()).ToArray());

                // 5 Gruppenzeilen + 15 Punkte ab Zeile 5.
                Assert.Equal(24, ws.LastRowUsed().RowNumber());
                Assert.Equal(R.WIRT_AE_GRUPPE_0, ws.Cell(5, 1).GetString());
                Assert.Equal("0.1", ws.Cell(6, 1).GetString());
                Assert.Equal(NUMMERN, Enumerable.Range(5, 20).Select(z => ws.Cell(z, 1).GetString())
                                                .Where(t => NUMMERN.Contains(t)).ToArray());
                Assert.Equal(R.WIRT_AE_STAND_ERFUELLT + ": " + R.WIRT_AE_1_STAND,
                             Enumerable.Range(5, 20).Where(z => ws.Cell(z, 1).GetString() == "1")
                                       .Select(z => ws.Cell(z, 5).GetString()).Single());

                // Die Beurteilung trägt der Prüfer ein — die Spalte ist leer und geprüft.
                Assert.True(Enumerable.Range(5, 20).All(z => ws.Cell(z, AnhangECheckliste.SPALTE_NOTE).IsEmpty()));
                IXLDataValidation note = Assert.Single(ws.DataValidations);
                Assert.Equal(XLAllowedValues.WholeNumber, note.AllowedValues);
                Assert.Equal(XLOperator.Between, note.Operator);
                Assert.Equal("1", note.MinValue);
                Assert.Equal("5", note.MaxValue);
                Assert.Equal(R.WIRT_AE_NOTE_FEHLER, note.ErrorMessage);
                Assert.Contains(note.Ranges, b => b.RangeAddress.ToString() == "F5:F24");
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }
    }
}
