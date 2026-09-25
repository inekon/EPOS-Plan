using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der reine Wertesatz der Wirtschaftlichkeit</b> (Konzept Berichtsvorlagen 5.1, 8.5, 13; Etappe BV-E3):
    /// Was Wirtschaftlichkeitsbaustein, Anhang E, Tabellenbericht und Formelmappe beim Schreiben aus der
    /// Datenbank lasen, ermittelt der Sammler EINMAL (<see cref="BerichtsDaten.Wirtschaft"/>); beim Füllen
    /// wird die Datenbank nicht berührt, und der <see cref="Berichtsbedarf"/> steuert Stundenreihen, Verlauf
    /// und Emissionsbilanz.
    ///
    /// <para><b>Ohne Datenbank</b> heißt hier geprüft, nicht behauptet: Nach dem Sammeln liegt unter
    /// <see cref="DataRepository"/> ein Zugriff, der bei JEDEM Vorgang wirft und ihn notiert, und der
    /// Datenbankpfad zeigt in einen Ordner, den es nicht gibt — auch eine eigene Verbindung
    /// (<c>StilleDb</c>) scheitert dann, und die Ausnahme der ersten Chance zählt sie mit, selbst wenn ein
    /// Aufrufer sie fängt.</para>
    ///
    /// <para><b>Die Probe gegen den Bausteinweg</b> hält die Messlatten, die vor BV-E3 eingefroren wurden
    /// (<see cref="BerichtVorlagenMesslatteTests"/>), gegen den gesammelten Weg — und vergleicht Zahl für Zahl
    /// jede Tabellenzelle, jeden Absatz, jedes Bild und jede Zelle der Mappe mit dem Weg, auf dem jeder
    /// Schreiber selbst rechnet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtWertesatzTests : IDisposable
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>Die Firma und Fassung des Deckblatts — dieselben wie in der Messlatte des Vorlagenwegs.</summary>
        private const string FIRMA = "INEKON GmbH";
        private const string FASSUNG = "9.9.9.9";

        /// <summary>Die Referenzgruppe mit zwei Varianten und Kraftwerkspark (Emissionsbilanz) der Testdatenbank.</summary>
        private const int GRUPPE_1019 = 1019;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e3");

        public BerichtWertesatzTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  (a) Füllen ohne Datenbank, (b) Word gegen Excel — nach dem Sammler
        // =====================================================================

        /// <summary>
        /// <b>Nach dem Sammeln entsteht der Bericht ohne Datenbank</b> — Word über die Standardvorlage und die
        /// Engine, Word auf dem bisherigen Weg (Stilvorlage) und die Mappe; alle drei gültig, kein Zugriff,
        /// nichts nachgeholt. Das Referenzprojekt 1030 und die Gruppe 1019 (zwei Varianten, Kraftwerkspark:
        /// Emissionsbilanz und Referenzkessel). <b>Und Word zeigt die Zahlen der Mappe</b>: Jede Zelle der
        /// Kennzahltafel im Erwartungsfall ist die Zahl der Mappe im Format ihrer Zeile, die Mappe führt in
        /// allen drei Szenarien die Werte des Wertesatzes, die Szenarienübersicht dieselben Differenzen wie die
        /// Bandbreitentafel.
        /// </summary>
        [Theory]
        [InlineData(Berichtsdatenproben.PROJEKT_1030)]
        [InlineData(GRUPPE_1019)]
        public void Nach_dem_Sammeln_entsteht_der_Bericht_ohne_Datenbank_und_Word_zeigt_die_Zahlen_der_Mappe(int stamm)
        {
            byte[] standard = Standardvorlage();
            string stilvorlage = Berichtsdatenproben.Berichtsvorlage();
            if (standard == null || stilvorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            var sammler = new BerichtsDatenSammler();
            BerichtsDaten daten = sammler.SammleFuerBericht(stamm, "Probe " + stamm, Varianten(stamm),
                Berichtsbedarf.FuerLauf(konfig, null, Startweg.Gewaehlt, true), null, CancellationToken.None, null);

            Assert.NotNull(daten.Wirtschaft);
            Assert.True(daten.Wirtschaft.Gesammelt);
            Assert.True(daten.Wirtschaft.AusDiesemLauf, "Die Rechnung dieses Laufs fehlt: " + daten.WirtschaftlichkeitFehler);
            Assert.Equal(Berichtsbedarf.Alles, daten.Wirtschaft.Bedarf);
            Assert.Equal(1, daten.Wirtschaft.VerlaufRechnungen);
            Assert.NotNull(daten.Wirtschaft.Verlauf);

            string alt = Path.Combine(_ordner, stamm + "_alt.docx");
            string vorlage = Path.Combine(_ordner, stamm + "_vorlage.docx");
            string mappe = Path.Combine(_ordner, stamm + ".xlsx");
            Fuellergebnis ergebnis = null;
            List<string> zugriffe = OhneDatenbank(() =>
            {
                new WordBerichtGenerator().Erzeuge(daten, konfig, alt, stilvorlage);
                ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, standard, Ersteller(), vorlage);
                new ExcelBerichtGenerator().Erzeuge(daten, konfig, mappe);
            });

            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen (" + zugriffe.Count + "):\n" +
                                             string.Join("\n", zugriffe.Take(20)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);
            Assert.Empty(ergebnis.Unbekannte);
            Assert.Empty(Validierungsfehler(alt));
            Assert.Empty(Validierungsfehler(vorlage));
            using (var wb = new XLWorkbook(mappe))
            {
                Assert.Contains(wb.Worksheets, w => w.Name == BerichtTexte.T("Wirtschaftlichkeit"));
                Assert.Contains(wb.Worksheets, w => w.Name == AnhangECheckliste.Blattname);
            }

            if (stamm == GRUPPE_1019)
            {
                Assert.True(daten.Wirtschaft.EmissionsbilanzRechnungen >= 1, "Die Emissionsbilanz der Gruppe fehlt im Wertesatz.");
                Assert.Contains(WordInhalt(alt), z => z.Contains("Emissionsbilanz — gekoppelte vs. getrennte Erzeugung", StringComparison.Ordinal));
            }

            // (b) Word gegen Excel — im bisherigen Weg und im Vorlagenweg.
            HashSet<string> geprueft = WordGleichExcel(daten, alt, mappe, stamm + " alter Weg");
            WordGleichExcel(daten, vorlage, mappe, stamm + " Vorlagenweg");
            _ausgabe.WriteLine(stamm + ": mit Zahl geprüft " + string.Join(", ", geprueft.OrderBy(s => s, StringComparer.Ordinal)));
            foreach (string schluessel in new[] { "INVESTITION", "BETRIEBSKOSTEN", "ENERGIEKOSTEN", "NETTOBARWERT" })
                Assert.Contains(schluessel, geprueft);
            if (stamm == GRUPPE_1019)
                foreach (string schluessel in new[] { "KAPITALWERT_DIFF", "ANNUITAET" })
                    Assert.Contains(schluessel, geprueft);
        }

        // =====================================================================
        //  (c)/(d) Probe gegen den Bausteinweg
        // =====================================================================

        /// <summary>
        /// <b>Der gesammelte Wertesatz schreibt den Bericht des Bausteinwegs.</b> Die Proben der Messlatte
        /// (1030 und die synthetische Gruppe) einmal so, wie jeder Schreiber selbst rechnet — der Weg vor
        /// BV-E3 —, einmal mit dem Wertesatz vorab (<see cref="WirtschaftsBerichtswerte.Ermittle"/>) und ohne
        /// Datenbank geschrieben: Beide treffen die eingefrorenen Messlatten des alten Wegs und des
        /// Vorlagenwegs, und beide sind Zahl für Zahl gleich — jeder Absatz, jede Tabellenzelle, jedes Bild,
        /// jede Zelle und jeder Name der Mappe.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Der_gesammelte_Wertesatz_schreibt_den_Bericht_des_Bausteinwegs(string probe)
        {
            byte[] standard = Standardvorlage();
            string stilvorlage = Berichtsdatenproben.Berichtsvorlage();
            if (standard == null || stilvorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(probe);
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();

            // 1. Der Schreiberweg: ohne Sammler rechnet jeder Schreiber selbst.
            Assert.Null(daten.Wirtschaft);
            string altA = Path.Combine(_ordner, "a_alt.docx"), vorlageA = Path.Combine(_ordner, "a_vorlage.docx"),
                   mappeA = Path.Combine(_ordner, "a.xlsx");
            new WordBerichtGenerator().Erzeuge(daten, konfig, altA, stilvorlage);
            new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, standard, Ersteller(), vorlageA);
            new ExcelBerichtGenerator().Erzeuge(daten, konfig, mappeA);

            // 2. Der Sammlerweg: der Wertesatz vorab, geschrieben ohne Datenbank.
            daten.Wirtschaft = WirtschaftsBerichtswerte.Ermittle(daten, Berichtsbedarf.Alles);
            string altB = Path.Combine(_ordner, "b_alt.docx"), vorlageB = Path.Combine(_ordner, "b_vorlage.docx"),
                   mappeB = Path.Combine(_ordner, "b.xlsx");
            List<string> zugriffe = OhneDatenbank(() =>
            {
                new WordBerichtGenerator().Erzeuge(daten, konfig, altB, stilvorlage);
                new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, standard, Ersteller(), vorlageB);
                new ExcelBerichtGenerator().Erzeuge(daten, konfig, mappeB);
            });
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen:\n" + string.Join("\n", zugriffe.Take(20)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);

            // 3. Die Messlatten, eingefroren vor BV-E3 — für beide Wege unverändert.
            var befunde = new List<string>();
            foreach ((string weg, string alt, string vorlage, string mappe) in new[]
                     { ("Schreiberweg", altA, vorlageA, mappeA), ("Sammlerweg", altB, vorlageB, mappeB) })
            {
                Messlatte(weg, "Bericht_Word_" + probe + ".txt", Berichtsstruktur.Word(alt), befunde);
                Messlatte(weg, "Bericht_Word_" + probe + "_Vorlage.txt",
                          Berichtsstruktur.Word(vorlage).SkipWhile(z => z != "## Rumpf").ToList(), befunde);
                Messlatte(weg, "Bericht_Excel_" + probe + ".txt", Berichtsstruktur.Excel(mappe), befunde);
            }

            // 4. Zahl für Zahl gleich.
            Gleich("Word, alter Weg", WordInhalt(altA), WordInhalt(altB), befunde);
            Gleich("Word, Vorlagenweg", WordInhalt(vorlageA), WordInhalt(vorlageB), befunde);
            Gleich("Excel", ExcelInhalt(mappeA), ExcelInhalt(mappeB), befunde);
            Assert.True(befunde.Count == 0, string.Join(Environment.NewLine, befunde));
        }

        // =====================================================================
        //  (e) Der Bedarf
        // =====================================================================

        /// <summary>
        /// Die Vorgabe folgt den Häkchen wie vor BV-E3: Zeitreihen mit „Ergebnisse je Variante“, Verlauf und
        /// Emissionsbilanz mit „Wirtschaftlichkeit“, ohne Konfiguration alles. Die Mappe folgt ihr, ein
        /// Startbefund mit dem Weg „Mit Standardvorlage“ ebenso.
        /// </summary>
        [Fact]
        public void Die_Vorgabe_folgt_den_Haekchen()
        {
            Assert.Equal(Berichtsbedarf.Alles, Berichtsbedarf.Vorgabe(null));
            Assert.Equal(Berichtsbedarf.Alles, Berichtsbedarf.Vorgabe(Berichtsdatenproben.VolleKonfiguration()));
            Assert.Equal(new Berichtsbedarf(Vorlagenbedarf.Zeitreihen), Berichtsbedarf.Vorgabe(BerichtsKonfiguration.Standard()));

            BerichtsKonfiguration ohneErgebnisse = Berichtsdatenproben.VolleKonfiguration();
            ohneErgebnisse.AktiveBausteine.Remove(BerichtsKonfiguration.B_ERGEBNISSE);
            Assert.Equal(new Berichtsbedarf(Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz),
                         Berichtsbedarf.Vorgabe(ohneErgebnisse));

            var nurDeckblatt = new BerichtsKonfiguration();
            nurDeckblatt.AktiveBausteine.Add(BerichtsKonfiguration.B_DECKBLATT);
            Assert.Equal(Berichtsbedarf.Nichts, Berichtsbedarf.Vorgabe(nurDeckblatt));

            // Ohne Startbefund die Vorgabe — mit und ohne Mappe.
            Assert.Equal(Berichtsbedarf.Vorgabe(ohneErgebnisse),
                         Berichtsbedarf.FuerLauf(ohneErgebnisse, null, Startweg.Gewaehlt, false));
            Assert.Equal(Berichtsbedarf.Vorgabe(ohneErgebnisse),
                         Berichtsbedarf.FuerLauf(ohneErgebnisse, null, Startweg.Gewaehlt, true));
        }

        /// <summary>
        /// Der Bedarf einer Vorlage aus ihren Platzhaltern (Konzept 5.1): Deckblattangaben brauchen nichts, ein
        /// Kapitel den Bedarf seines Bausteins — nur mit gesetztem Häkchen —, die Kältestunden die Stundenreihen,
        /// der Sammelanker und eine Vorlage ohne Platzhalter den der angehakten Kapitel; eine unlesbare Vorlage
        /// fällt auf die Vorgabe zurück.
        /// </summary>
        [Fact]
        public void Der_Bedarf_einer_Vorlage_kommt_aus_ihren_Platzhaltern()
        {
            BerichtsKonfiguration voll = Berichtsdatenproben.VolleKonfiguration();
            BerichtsKonfiguration standard = BerichtsKonfiguration.Standard();

            Assert.Equal(Berichtsbedarf.Nichts, Bedarf(voll, "{{bericht.titel}}", "Kunde {{projekt.kunde}}", "{{bericht.datum}}"));
            Assert.Equal(new Berichtsbedarf(Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz),
                         Bedarf(voll, "{{bericht.titel}}", "{{kapitel.wirtschaftlichkeit}}"));
            Assert.Equal(Berichtsbedarf.Nichts, Bedarf(standard, "{{kapitel.wirtschaftlichkeit}}"));
            Assert.Equal(new Berichtsbedarf(Vorlagenbedarf.Zeitreihen), Bedarf(voll, "{{kapitel.ergebnisse}}", "{{kapitel.anhang_e}}"));
            Assert.Equal(new Berichtsbedarf(Vorlagenbedarf.Zeitreihen),
                         Bedarf(standard, "Kältestunden {{stamm.kennzahl." + KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN + "}}"));
            Assert.Equal(Berichtsbedarf.Vorgabe(standard), Bedarf(standard, "{{bericht.titel}}", "{{bericht.inhalt}}"));
            Assert.Equal(Berichtsbedarf.Vorgabe(voll), Bedarf(voll, "Nur Text, kein Platzhalter"));
            Assert.Equal(Berichtsbedarf.Vorgabe(voll), Berichtsbedarf.AusVorlage(null, voll));

            // Die Standardvorlage braucht, was die Häkchen schalten — den Bedarf von heute.
            byte[] vorlage = Standardvorlage();
            if (vorlage == null) return;
            foreach (BerichtsKonfiguration k in new[] { voll, standard, OhneErgebnisse() })
                Assert.Equal(Berichtsbedarf.Vorgabe(k),
                             Berichtsbedarf.AusVorlage(Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Pruefkontext.Aus(k, false)), k));
        }

        /// <summary>
        /// <b>Eine Vorlage nur mit Deckblattangaben</b> erhebt keine Stundenreihen und rechnet keinen Verlauf
        /// (Sammleraufrufe gezählt) — die Vorprüfung legt den Bedarf in den Startbefund, der Lauf reicht ihn an den
        /// Sammler, und das Füllen braucht keine Datenbank. <b>Die Standardvorlage</b> erhebt beides wie heute.
        /// </summary>
        [Fact]
        public void Eine_Vorlage_nur_mit_Deckblatt_erhebt_weder_Zeitreihen_noch_Verlauf()
        {
            byte[] standard = Standardvorlage();
            if (standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string app = Directory.CreateDirectory(Path.Combine(_ordner, "App", "Vorlagen")).FullName;
            string dokumente = Directory.CreateDirectory(Path.Combine(_ordner, "Dokumente")).FullName;
            string quellen = Directory.CreateDirectory(Path.Combine(_ordner, "Quellen")).FullName;
            string ziel = Directory.CreateDirectory(Path.Combine(_ordner, "Berichte")).FullName;
            File.WriteAllBytes(Path.Combine(app, BerichtsvorlagenCtrl.DATEI_STANDARD), standard);
            var vorlagen = new BerichtsvorlagenCtrl(new Probepfade(dokumente, app), new FluechtigeEinstellungen(), () => FIRMA);
            var ctrl = new BerichtCtrl(vorlagen);

            string quelle = Path.Combine(quellen, "Deckblatt.docx");
            File.WriteAllBytes(quelle, Probevorlagen.AusAbsaetzen("{{bericht.titel}}", "Kunde {{projekt.kunde}}", "Stand {{bericht.datum}}"));
            Vorlagenergebnis hinzu = vorlagen.Hinzufuegen(quelle);
            Assert.True(hinzu.Erfolg, hinzu.Meldung);

            // Deckblatt: nichts über den Regellauf hinaus.
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            konfig.ZielOrdner = ziel;
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, hinzu.Eintrag);
            Startbefund start = ctrl.PruefeVorStart(konfig, false, 1);
            Assert.False(start.BrauchtRueckfrage);
            Assert.Equal(Berichtsbedarf.Nichts, start.Bedarf);
            Berichtsbedarf bedarf = Berichtsbedarf.FuerLauf(konfig, start, Startweg.Gewaehlt, false);
            Assert.Equal(Berichtsbedarf.Nichts, bedarf);
            Assert.Equal(Berichtsbedarf.Vorgabe(konfig), Berichtsbedarf.FuerLauf(konfig, start, Startweg.Standard, false));
            Assert.Equal(Berichtsbedarf.Vorgabe(konfig), Berichtsbedarf.FuerLauf(konfig, start, Startweg.Gewaehlt, true));

            var sammler = new BerichtsDatenSammler();
            BerichtsDaten daten = sammler.SammleFuerBericht(Berichtsdatenproben.PROJEKT_1030, "Referenzprojekt 1030",
                new List<int>(), bedarf, null, CancellationToken.None, null);
            Assert.False(KostenEmissionRechner.StromLeistungspreisGepflegt(Berichtsdatenproben.PROJEKT_1030, null),
                         "Die Probe setzt voraus, dass 1030 keinen Strom-Leistungspreis führt.");
            Assert.Equal(0, sammler.ZeitreihenErhoben);
            Assert.All(daten.Varianten, v => Assert.Null(v.Zeitreihen));
            Assert.Equal(0, daten.Wirtschaft.VerlaufRechnungen);
            Assert.Equal(0, daten.Wirtschaft.EmissionsbilanzRechnungen);
            Assert.True(daten.Wirtschaftlichkeit.Count > 0, "Die Wirtschaftlichkeit rechnet immer.");

            Berichtslauf lauf = null;
            List<string> zugriffe = OhneDatenbank(() => lauf = ctrl.ErzeugeWord(daten, konfig, start, Startweg.Gewaehlt));
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen:\n" + string.Join("\n", zugriffe.Take(20)));
            Assert.Equal("Deckblatt", lauf.VorlageName);
            Assert.False(lauf.IstRueckfall);
            Assert.Empty(daten.Wirtschaft.Nachgeholt);

            // Die Standardvorlage mit allen Häkchen: Stundenreihen und Verlauf wie heute.
            BerichtsKonfiguration voll = Berichtsdatenproben.VolleKonfiguration();
            voll.ZielOrdner = ziel;
            Startbefund startStandard = ctrl.PruefeVorStart(voll, false, 1);
            Assert.Equal(Berichtsbedarf.Alles, startStandard.Bedarf);
            var sammlerStandard = new BerichtsDatenSammler();
            BerichtsDaten datenStandard = sammlerStandard.SammleFuerBericht(Berichtsdatenproben.PROJEKT_1030, "Referenzprojekt 1030",
                new List<int>(), Berichtsbedarf.FuerLauf(voll, startStandard, Startweg.Gewaehlt, true),
                null, CancellationToken.None, null);
            Assert.Equal(1, sammlerStandard.ZeitreihenErhoben);
            Assert.All(datenStandard.Varianten, v => Assert.NotNull(v.Zeitreihen));
            Assert.Equal(1, datenStandard.Wirtschaft.VerlaufRechnungen);
        }

        // =====================================================================
        //  (f) Laufzeit
        // =====================================================================

        /// <summary>Erzeugungen je Probe und Weg.</summary>
        private const int LAEUFE = 3;

        /// <summary>
        /// <b>Die Laufzeit des gesammelten Wegs</b> gegen den Schreiberweg (Konzept 8.5: „heute + 10 %“): Wort-
        /// und Tabellenbericht der Proben 1030 und der Gruppe mit sieben Ständen, einmal wie vor BV-E3 (jeder
        /// Schreiber rechnet seinen Teil, den Verlauf zweimal), einmal Wertesatz plus beide Schreiber (der
        /// Verlauf einmal). Die Zeiten stehen in der Testausgabe; der Fall hält nur die Sicherheitsgrenze.
        /// </summary>
        [Fact]
        public void Laufzeit_des_Wertesatzes()
        {
            string stilvorlage = Berichtsdatenproben.Berichtsvorlage();
            if (stilvorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            var proben = new List<(string Name, BerichtsDaten Daten)>
            {
                ("1030", BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030)),
                ("Gruppe mit 7 Ständen", BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, 7)),
            };

            int nummer = 0;
            foreach ((string name, BerichtsDaten daten) in proben)
            {
                var schreiber = new List<long>();
                var gesammelt = new List<long>();
                for (int lauf = 1; lauf <= LAEUFE; lauf++)
                {
                    daten.Wirtschaft = null;
                    Stopwatch uhr = Stopwatch.StartNew();
                    new WordBerichtGenerator().Erzeuge(daten, konfig, Path.Combine(_ordner, "l" + (++nummer) + ".docx"), stilvorlage);
                    new ExcelBerichtGenerator().Erzeuge(daten, konfig, Path.Combine(_ordner, "l" + (++nummer) + ".xlsx"));
                    schreiber.Add(uhr.ElapsedMilliseconds);

                    uhr.Restart();
                    daten.Wirtschaft = WirtschaftsBerichtswerte.Ermittle(daten, Berichtsbedarf.Alles);
                    new WordBerichtGenerator().Erzeuge(daten, konfig, Path.Combine(_ordner, "l" + (++nummer) + ".docx"), stilvorlage);
                    new ExcelBerichtGenerator().Erzeuge(daten, konfig, Path.Combine(_ordner, "l" + (++nummer) + ".xlsx"));
                    gesammelt.Add(uhr.ElapsedMilliseconds);
                }
                daten.Wirtschaft = null;
                long s = schreiber.OrderBy(z => z).ElementAt(LAEUFE / 2), g = gesammelt.OrderBy(z => z).ElementAt(LAEUFE / 2);
                _ausgabe.WriteLine("Laufzeit " + name + " · Word + Excel · Schreiberweg " + string.Join(" / ", schreiber) +
                                   " ms (Median " + s + ") · Wertesatz + Schreiber " + string.Join(" / ", gesammelt) +
                                   " ms (Median " + g + ")");
                Assert.True(g < 120_000 && s < 120_000, name + ": über der Sicherheitsgrenze.");
            }
        }

        // =====================================================================
        //  Word gegen Excel
        // =====================================================================

        /// <summary>
        /// Die Kennzahltafel „Erwartet“ des Wortberichts gegen die Blöcke der Mappe und den Wertesatz; die
        /// Szenarienübersicht gegen die Bandbreitentafel. Rückgabe: die Zeilenschlüssel, die mit Zahl geprüft
        /// wurden.
        /// </summary>
        private static HashSet<string> WordGleichExcel(BerichtsDaten daten, string docx, string xlsx, string wo)
        {
            WirtschaftsBerichtswerte w = daten.Wirtschaft;
            List<WirtschaftlichkeitErgebnis> alle = w.Ergebnisse;
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(w.Zeilen(w.IdReferenzTafel), alle);
            var befunde = new List<string>();
            var geprueft = new HashSet<string>(StringComparer.Ordinal);

            using WordprocessingDocument doc = WordprocessingDocument.Open(docx, false);
            Body body = doc.MainDocumentPart.Document.Body;
            using var wb = new XLWorkbook(xlsx);
            IXLWorksheet ws = wb.Worksheet(BerichtTexte.T("Wirtschaftlichkeit"));

            // --- Die Mappe in allen drei Szenarien gegen den Wertesatz ---
            var bloecke = new Dictionary<string, (int Kopf, Dictionary<string, int> Spalten)>(StringComparer.Ordinal);
            foreach (string sz in new[] { WirtschaftlichkeitSzenario.ERWARTET, WirtschaftlichkeitSzenario.BEST,
                                          WirtschaftlichkeitSzenario.WORST })
            {
                if (!alle.Any(e => e.Szenario == sz)) continue;
                int titel = Zeile(ws, 1, BerichtTexte.T("Szenario") + ": " + VerlaufZeilen.Szenarioname(sz), 1);
                Assert.True(titel > 0, wo + ": Block „" + sz + "“ fehlt in der Mappe.");
                int kopf = Zeile(ws, 1, BerichtTexte.T("Kennzahl"), titel);
                var spalten = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int c = 2; !ws.Cell(kopf, c).IsEmpty(); c++) spalten[ws.Cell(kopf, c).GetString()] = c;
                bloecke[sz] = (kopf, spalten);

                for (int i = 0; i < zeilen.Count; i++)
                {
                    WirtZeile z = zeilen[i];
                    if (z.IstUeberschrift) continue;
                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        if (!spalten.TryGetValue(Name(v), out int c)) continue;
                        WirtschaftlichkeitErgebnis e = Ergebnis(alle, v.IdProjekt, sz);
                        IXLCell zelle = ws.Cell(kopf + 1 + i, c);
                        if (z.IstText)
                        {
                            string t = e == null ? "" : z.Text(e) ?? "";
                            if (zelle.GetString() != t) befunde.Add(wo + " · Mappe " + sz + " · " + z.Schluessel + " · " + Name(v) + ": „" + zelle.GetString() + "“ statt „" + t + "“");
                            continue;
                        }
                        double? soll = e == null ? null : z.ExcelWert(e);
                        double? ist = Zahl(zelle);
                        if (!GleicheZahl(soll, ist))
                            befunde.Add(wo + " · Mappe " + sz + " · " + z.Schluessel + " · " + Name(v) + ": " + Z(ist) + " statt " + Z(soll));
                    }
                }
            }

            // --- Word „Erwartet“ gegen die Mappe ---
            List<Table> tafeln = TafelnUnter(body, "Kennzahlen im Szenario „Erwartet“");
            Assert.True(tafeln.Count > 0, wo + ": Die Kennzahltafel fehlt im Wortbericht.");
            (int kopfE, Dictionary<string, int> spaltenE) = bloecke[WirtschaftlichkeitSzenario.ERWARTET];
            foreach (Table t in tafeln)
            {
                List<TableRow> reihen = t.Elements<TableRow>().ToList();
                List<string> kopf = reihen[0].Elements<TableCell>().Select(c => c.InnerText).ToList();
                Assert.Equal(BerichtTexte.T("Kennzahl"), kopf[0]);
                Assert.True(reihen.Count == zeilen.Count + 1,
                            wo + ": Die Tafel führt " + (reihen.Count - 1) + " Zeilen, der Wertesatz " + zeilen.Count + ".");
                for (int i = 0; i < zeilen.Count; i++)
                {
                    WirtZeile z = zeilen[i];
                    List<string> zellen = reihen[i + 1].Elements<TableCell>().Select(c => c.InnerText).ToList();
                    for (int s = 1; s < kopf.Count; s++)
                    {
                        VariantenDaten v = daten.Varianten.First(x => Name(x) == kopf[s]);
                        WirtschaftlichkeitErgebnis e = Ergebnis(alle, v.IdProjekt, WirtschaftlichkeitSzenario.ERWARTET);
                        string soll = z.IstUeberschrift ? "" : z.Anzeige(e, DE);
                        if (zellen[s] != soll)
                            befunde.Add(wo + " · Word · " + z.Schluessel + " · " + kopf[s] + ": „" + zellen[s] + "“ statt „" + soll + "“");
                        if (z.IstUeberschrift || z.IstText || !spaltenE.TryGetValue(kopf[s], out int c)) continue;

                        double? inExcel = Zahl(ws.Cell(kopfE + 1 + i, c));
                        if (!inExcel.HasValue) continue;
                        // Rundung nach Format: Die Word-Zelle ist die Zahl der Mappe im Format der Zeile.
                        string gerundet = inExcel.Value.ToString(z.Format, DE);
                        if (zellen[s] != gerundet)
                            befunde.Add(wo + " · Word gegen Excel · " + z.Schluessel + " · " + kopf[s] + ": Word „" + zellen[s] +
                                        "“, Mappe " + Z(inExcel) + " (" + gerundet + ")");
                        else geprueft.Add(z.Schluessel);
                    }
                }
            }

            // --- Szenarienübersicht gegen die Bandbreitentafel ---
            WirtschaftlichkeitBandbreite band = (daten.Bewertung ?? w.Bewertung)?.Bandbreite;
            if (band != null && !band.Leer)
            {
                string ueberschrift = string.Format(DE, R.WIRT_SZ_UEBERSCHRIFT, R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET, R.WIRT_SZEN_BEST);
                Table tafel = TafelnUnter(body, ueberschrift).FirstOrDefault();
                Assert.True(tafel != null, wo + ": Die Szenarienübersicht fehlt im Wortbericht.");
                List<TableRow> reihen = tafel.Elements<TableRow>().ToList();
                int titel = Zeile(ws, 1, R.WIRT_SZ_BANDBREITE_TITEL, 1);
                Assert.True(titel > 0, wo + ": Die Bandbreitentafel fehlt in der Mappe.");
                Assert.Equal(band.Zeilen.Count + 2, reihen.Count);
                for (int i = 0; i < band.Zeilen.Count; i++)
                {
                    BandbreitenZeile z = band.Zeilen[i];
                    List<string> zellen = reihen[i + 2].Elements<TableCell>().Select(c => c.InnerText).ToList();
                    int zeile = titel + 3 + i;       // Titel, Kopf, Referenzzeile
                    double?[] werte = { z.Worst, z.Erwartet, z.Best, z.Spanne };
                    for (int k = 0; k < werte.Length; k++)
                    {
                        string soll = werte[k].HasValue ? werte[k].Value.ToString("N0", DE) : "—";
                        double? inExcel = Zahl(ws.Cell(zeile, 2 + k));
                        if (zellen[1 + k] != soll || !GleicheZahl(werte[k], inExcel))
                            befunde.Add(wo + " · Bandbreite · " + z.Anzeige + " · Spalte " + (k + 1) + ": Word „" + zellen[1 + k] +
                                        "“, Mappe " + Z(inExcel) + ", Wertesatz " + Z(werte[k]));
                        else if (werte[k].HasValue) geprueft.Add("BANDBREITE_" + (k + 1).ToString(CultureInfo.InvariantCulture));
                    }
                    string amort = z.AmortisationJahre.HasValue ? z.AmortisationJahre.Value.ToString("N1", DE) : "—";
                    if (zellen[5] != amort || !GleicheZahl(z.AmortisationJahre, Zahl(ws.Cell(zeile, 6))))
                        befunde.Add(wo + " · Bandbreite · " + z.Anzeige + " · Amortisation: Word „" + zellen[5] + "“, Mappe " +
                                    Z(Zahl(ws.Cell(zeile, 6))));
                }
            }

            Assert.True(befunde.Count == 0, string.Join(Environment.NewLine, befunde.Take(40)));
            return geprueft;
        }

        /// <summary>Die Tafeln unter einer Überschrift bis zur nächsten Überschrift (Überschrift n, Kapitelkopf).</summary>
        private static List<Table> TafelnUnter(Body body, string ueberschrift)
        {
            var tafeln = new List<Table>();
            bool darunter = false;
            foreach (OpenXmlElement e in body.ChildElements)
            {
                if (e is Paragraph p)
                {
                    string stil = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
                    if (stil.StartsWith("Heading", StringComparison.Ordinal) || stil.StartsWith("EPOS", StringComparison.Ordinal))
                    {
                        if (darunter) break;
                        darunter = string.Equals(p.InnerText, ueberschrift, StringComparison.Ordinal);
                    }
                }
                else if (darunter && e is Table t) tafeln.Add(t);
            }
            return tafeln;
        }

        /// <summary>Die erste Zeile ab <paramref name="ab"/>, deren Zelle in Spalte <paramref name="spalte"/> den Text trägt; 0 = keine.</summary>
        private static int Zeile(IXLWorksheet ws, int spalte, string text, int ab)
        {
            int letzte = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = Math.Max(1, ab); r <= letzte; r++)
                if (string.Equals(ws.Cell(r, spalte).GetString(), text, StringComparison.Ordinal)) return r;
            return 0;
        }

        private static string Name(VariantenDaten v) => v.IstStamm ? "Stamm" : v.Anzeige;

        private static WirtschaftlichkeitErgebnis Ergebnis(List<WirtschaftlichkeitErgebnis> alle, int idProjekt, string szenario)
            => alle.FirstOrDefault(x => x.IdProjekt == idProjekt && x.Szenario == szenario);

        /// <summary>Die Zahl einer Zelle — einer Formelzelle ihr nachgetragenes Ergebnis; <c>null</c> ohne Zahl.</summary>
        private static double? Zahl(IXLCell zelle)
        {
            XLCellValue v = zelle.HasFormula ? zelle.CachedValue : zelle.Value;
            return v.IsNumber ? v.GetNumber() : (double?)null;
        }

        private static bool GleicheZahl(double? a, double? b)
            => a.HasValue == b.HasValue && (!a.HasValue || Math.Abs(a.Value - b.Value) <= 1e-9 * Math.Max(1.0, Math.Abs(a.Value)));

        private static string Z(double? v) => v.HasValue ? v.Value.ToString("R", CultureInfo.InvariantCulture) : "leer";

        // =====================================================================
        //  Inhalt Zahl für Zahl
        // =====================================================================

        /// <summary>
        /// Der ganze Inhalt eines Wortberichts: je Kind des Rumpfs sein Text, je Tabellenzeile ihre Zellen, dazu
        /// jede Bildstelle mit der Prüfsumme ihres Bildteils (PNG und SVG) und Kopf- und Fußzeilen.
        /// </summary>
        private static List<string> WordInhalt(string pfad)
        {
            var z = new List<string>();
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            MainDocumentPart main = doc.MainDocumentPart;
            foreach (OpenXmlElement e in main.Document.Body.ChildElements)
            {
                if (e is Table t)
                    foreach (TableRow r in t.Elements<TableRow>())
                        z.Add("| " + string.Join(" | ", r.Elements<TableCell>().Select(c => c.InnerText)));
                else z.Add(e.LocalName + ": " + e.InnerText);
                foreach (OpenXmlElement bild in e.Descendants().Where(d => d.LocalName == "blip" || d.LocalName == "svgBlip"))
                {
                    string id = bild.GetAttributes().FirstOrDefault(a => a.LocalName == "embed").Value;
                    z.Add("  Bild " + bild.LocalName + " " + Pruefsumme(main.GetPartById(id)));
                }
            }
            foreach (HeaderPart k in main.HeaderParts) z.Add("Kopf: " + k.Header.InnerText);
            foreach (FooterPart f in main.FooterParts) z.Add("Fuß: " + f.Footer.InnerText);
            return z;
        }

        private static string Pruefsumme(OpenXmlPart teil)
        {
            using Stream s = teil.GetStream();
            return Convert.ToHexString(SHA256.HashData(s));
        }

        /// <summary>Der ganze Inhalt einer Mappe: je Blatt jede Zelle mit Wert (Zahl rundungsfrei) bzw. Formel und Ergebnis, dazu die Namen.</summary>
        private static List<string> ExcelInhalt(string pfad)
        {
            var z = new List<string>();
            using var wb = new XLWorkbook(pfad);
            foreach (IXLWorksheet ws in wb.Worksheets.OrderBy(w => w.Position))
            {
                z.Add("# " + ws.Name);
                foreach (IXLCell c in ws.CellsUsed(XLCellsUsedOptions.Contents))
                    z.Add(c.Address + " " + (c.HasFormula ? "=" + c.FormulaA1 + " → " + Wert(c.CachedValue) : Wert(c.Value)));
            }
            foreach (IXLDefinedName n in wb.DefinedNames) z.Add("Name " + n.Name + " = " + n.RefersTo);
            return z;
        }

        private static string Wert(XLCellValue v)
        {
            if (v.IsNumber) return v.GetNumber().ToString("R", CultureInfo.InvariantCulture);
            if (v.IsDateTime) return v.GetDateTime().ToString("o", CultureInfo.InvariantCulture);
            if (v.IsText) return "„" + v.GetText() + "“";
            return v.ToString();
        }

        private static void Gleich(string was, List<string> a, List<string> b, List<string> befunde)
        {
            if (a.SequenceEqual(b, StringComparer.Ordinal)) return;
            int i = 0;
            while (i < a.Count && i < b.Count && string.Equals(a[i], b[i], StringComparison.Ordinal)) i++;
            befunde.Add(was + ": Schreiberweg und Sammlerweg weichen ab (" + a.Count + " / " + b.Count + " Zeilen), zuerst in Zeile " +
                        (i + 1) + ": „" + (i < a.Count ? a[i] : "—") + "“ gegen „" + (i < b.Count ? b[i] : "—") + "“");
        }

        /// <summary>Die Liste gegen die eingefrorene Messlatte (ohne ihre Kopfzeile).</summary>
        private static void Messlatte(string weg, string datei, List<string> struktur, List<string> befunde)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            string pfad = Path.Combine(wurzel, BerichtVorlagenMesslatteTests.MESSLATTEN_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            Assert.True(File.Exists(pfad), "Die Messlatte fehlt: " + pfad);
            List<string> erwartet = File.ReadAllText(pfad, Encoding.UTF8).Split('\n').Select(z => z.TrimEnd('\r'))
                                        .Where(z => !z.StartsWith("# Strukturmesslatte", StringComparison.Ordinal)).ToList();
            if (erwartet.Count > 0 && erwartet[^1].Length == 0) erwartet.RemoveAt(erwartet.Count - 1);
            Gleich(weg + " · Messlatte " + datei, erwartet, struktur, befunde);
        }

        // =====================================================================
        //  Ohne Datenbank
        // =====================================================================

        /// <summary>
        /// Führt <paramref name="schreiben"/> ohne Datenbank aus: Jeder Vorgang über
        /// <see cref="DataRepository.Zugriff"/> wirft und wird notiert; eine eigene Verbindung scheitert am Pfad
        /// ohne Ordner und wird über die Ausnahme der ersten Chance notiert — auch wenn ein Aufrufer sie fängt.
        /// Rückgabe: alle notierten Zugriffe. Zugriff und Pfad werden im <c>finally</c> zurückgelegt.
        /// </summary>
        internal static List<string> OhneDatenbank(Action schreiben)
        {
            var zugriff = new WerfenderZugriff();
            var direkt = new List<string>();
            IDatenzugriff vorher = DataRepository.Zugriff;
            string pfadVorher = DataRepository.PfadUeberschreibung;
            string nirgends = Path.Combine(Path.GetTempPath(), "epos-bv-e3-ohne-db-" + Guid.NewGuid().ToString("N"),
                                           "gibt-es-nicht.sqlite");
            EventHandler<FirstChanceExceptionEventArgs> fang = (s, e) =>
            {
                if (e.Exception.GetType().Name.StartsWith("Sqlite", StringComparison.Ordinal))
                    lock (direkt) direkt.Add("eigene Verbindung: " + e.Exception.GetType().Name + " " + e.Exception.Message);
            };
            DataRepository.Zugriff = zugriff;
            DataRepository.PfadUeberschreibung = nirgends;
            AppDomain.CurrentDomain.FirstChanceException += fang;
            try { schreiben(); }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= fang;
                DataRepository.PfadUeberschreibung = pfadVorher;
                DataRepository.Zugriff = vorher;
            }
            var alle = new List<string>(zugriff.Zugriffe);
            lock (direkt) alle.AddRange(direkt);
            return alle;
        }

        /// <summary>Ein Zugriff, der bei JEDEM Vorgang wirft — und ihn vorher notiert.</summary>
        private sealed class WerfenderZugriff : IDatenzugriff
        {
            private readonly List<string> _zugriffe = new List<string>();

            internal IReadOnlyList<string> Zugriffe { get { lock (_zugriffe) return _zugriffe.ToList(); } }

            private InvalidOperationException Wirf(string was)
            {
                string zeile = (was ?? "").Replace("\r", " ").Replace("\n", " ");
                lock (_zugriffe) _zugriffe.Add(zeile);
                return new InvalidOperationException("BV-E3: Datenbankzugriff beim Füllen — " + zeile);
            }

            public DataTable GetDataTable(string sql, params DbParam[] parameter) => throw Wirf(sql);
            public bool ExecuteSQL(string sql, params DbParam[] parameter) => throw Wirf(sql);
            public int ExecuteNonQuery(string sql, params DbParam[] parameter) => throw Wirf(sql);
            public int ExecuteInsertAndGetId(string insertSql, DbParam[] parameter) => throw Wirf(insertSql);
            public object ExecuteScalar(string sql, params DbParam[] parameter) => throw Wirf(sql);
            public DbVorgang Vorgang() => throw Wirf("Vorgang");
            public DbVorgang VorgangOhneFremdschluessel() => throw Wirf("VorgangOhneFremdschluessel");
            public bool TabelleVorhanden(string name) => throw Wirf("TabelleVorhanden " + name);
            public bool SpalteVorhanden(string tabelle, string spalte) => throw Wirf("SpalteVorhanden " + tabelle + "." + spalte);
            public List<string> SpaltenVonTabelle(string tabelle) => throw Wirf("SpaltenVonTabelle " + tabelle);
            public DataTable IndexListe(string tabelle) => throw Wirf("IndexListe " + tabelle);
            public DataTable FremdschluesselListe(string tabelle) => throw Wirf("FremdschluesselListe " + tabelle);
            public bool DatenbankVorhanden() => throw Wirf("DatenbankVorhanden");
            public string DatenbankPfad => throw Wirf("DatenbankPfad");
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Pfade, deren Dokumente- und Auslieferungsordner der Fall bestimmt.</summary>
        private sealed class Probepfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Probepfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }

        private static Erstellerangaben Ersteller() => new Erstellerangaben { Firma = FIRMA, Version = FASSUNG };

        /// <summary>Die Standardvorlage des Repositoriums; <c>null</c> ohne Repositorium.</summary>
        private static byte[] Standardvorlage()
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            return pfad != null && File.Exists(pfad) ? File.ReadAllBytes(pfad) : null;
        }

        /// <summary>Die Varianten der Gruppe eines Stammprojekts in der Testdatenbank.</summary>
        private static List<int> Varianten(int stamm)
        {
            return new VariantenCtrl().LadeGruppe(stamm, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
        }

        /// <summary>Der Bedarf einer Vorlage aus Absätzen unter der Konfiguration <paramref name="konfig"/>.</summary>
        private static Berichtsbedarf Bedarf(BerichtsKonfiguration konfig, params string[] absaetze)
        {
            byte[] vorlage = Probevorlagen.AusAbsaetzen(absaetze);
            Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, Pruefkontext.Aus(konfig, false));
            Assert.True(befund.IstLesbar);
            return Berichtsbedarf.AusVorlage(befund, konfig);
        }

        private static BerichtsKonfiguration OhneErgebnisse()
        {
            BerichtsKonfiguration k = Berichtsdatenproben.VolleKonfiguration();
            k.AktiveBausteine.Remove(BerichtsKonfiguration.B_ERGEBNISSE);
            return k;
        }

        /// <summary>Die Befunde des Validators in allen Office-Fassungen 2007 bis 2021.</summary>
        private List<string> Validierungsfehler(string pfad)
        {
            var befunde = new List<string>();
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
                foreach (ValidationErrorInfo f in new OpenXmlValidator(fassung).Validate(doc).Take(5))
                {
                    string text = fassung + ": " + f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath;
                    befunde.Add(text);
                    _ausgabe.WriteLine(text);
                }
            return befunde;
        }
    }
}
