using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 38 — Rundlauf der Zonen-Konditionierung über IFC</b> (Datenaustauschkonzept 6.3, 16.3, 16.7): Der IFC-Export
    /// schreibt je Zone <c>EPOS_Zone</c> (Nutzung, Heiz- und Kühlsollwert, Luftwechsel der Nutzer) und je Kalender
    /// <c>EPOS_Kalender_&lt;Größe&gt;</c> (Grundangabe, Woche, Nennwert, Bemerkung, Perioden); der IFC-Leser nimmt sie als
    /// <see cref="AbbildKonditionierung"/> zurück, der Zonenplan als Konditionierung mit Herkunft „aus IFC-Datei (EPOS)“.
    /// Referenzprojekt 1052 (drei Zonen mit Zonenkalendern) aus der Testdatenbank, dazu eine synthetische Zone mit allen
    /// Angabearten und die Gegenproben ohne <c>EPOS_*</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class IfcKonditionierungRundlaufTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private const int PROJEKT = 1052;

        private readonly TestDatenbank _db;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public IfcKonditionierungRundlaufTests(TestDatenbank db) => _db = db;

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Probe 38 am Referenzprojekt 1052
        // ==================================================================

        [Fact]
        public void Probe38_Referenzprojekt_1052_kommt_gleich_zurueck_und_zweiter_Export_ist_bytegleich()
        {
            if (!_db.Vorhanden) return;
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Z_ProjektGebaeude WHERE ID_Projekt = ? ORDER BY ID LIMIT 1",
                                                                   new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);
            GebaeudeExportSatz satz = GebaeudeExportSatz.Lesen(PROJEKT, idZ, "80331");
            Assert.Equal(3, satz.Zonen.Count);
            GebaeudeExportProfil profil = IfcExportProbe.Profil();
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(satz, profil);
            List<AbbildRaum> zonen = plan.Abbild.Gebaeude[0].Raeume.Where(r => r.Konditionierung != null).ToList();
            Assert.Equal(3, zonen.Count);
            int kalender = zonen.Sum(r => r.Konditionierung.Kalender.Count);
            Assert.True(kalender > 0, "1052 trägt Zonenkalender");

            byte[] erste = ExportSatzProbe.Datei(plan, profil);
            byte[] zweite = ExportSatzProbe.Datei(ExportSatzProbe.Plan(GebaeudeExportSatz.Lesen(PROJEKT, idZ, "80331"), profil), profil);
            Assert.Equal(erste, zweite);

            GebaeudeAbbild gelesen = IfcExportProbe.Lesen(erste);
            Assert.DoesNotContain(gelesen.Gebaeude.SelectMany(g => g.Meldungen), m => m.Schluessel == "IMP_IFC_PROT_KOND_UEBERSPRUNGEN");
            AbbildGebaeude g0 = Assert.Single(gelesen.Gebaeude);
            int perioden = 0;
            foreach (AbbildRaum quelle in zonen)
            {
                AbbildRaum zurueck = g0.Raeume.Single(r => r.Name == quelle.Name);
                Gleich(quelle.Konditionierung, zurueck.Konditionierung);
                perioden += quelle.Konditionierung.Kalender.Sum(k => k.Kalender.Perioden.Count);
            }
            Assert.True(perioden > 0, "1052 trägt Perioden");

            // Der Zonenplan nimmt die Konditionierung je Zone mit Herkunft „aus IFC-Datei (EPOS)“.
            Zonenplan zp = Zonenplan.Vorschlag(gelesen, 0, IfcImportProfil.ZONENREGEL_Z1);
            Assert.Null(zp.Ablehnung);
            List<Planzone> mit = zp.Zonen.Where(z => z.Projektdatei != null).ToList();
            Assert.Equal(3, mit.Count);
            Assert.All(mit, z => Assert.True(z.Projektdatei.AusIfc));
            Groessenkonditionierung heiz = mit.SelectMany(z => z.Projektdatei.Groessen).First(x => x.Kalender != null);
            Assert.StartsWith(WindowsFormsApplication1.MyResource.Resource.IMP_IFC_KOND_HERKUNFT, heiz.Bemerkung);
        }

        // ==================================================================
        //  Alle Angabearten, Nennwert, Bemerkung, unbekanntes Format
        // ==================================================================

        [Fact]
        public void Alle_Angabearten_laufen_rund_und_ein_unbekannter_Periodentext_wird_benannt_uebersprungen()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), IfcExportProbe.Profil());
            AbbildRaum raum = plan.Abbild.Gebaeude[0].Raeume.First(r => r.Konditionierung != null);
            raum.Konditionierung = Synthetisch();
            byte[] datei = IfcExportProbe.Schreiben(plan.Abbild, null, null, null, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Contains(new IfcSchreiber().Vorschau(plan.Abbild, IfcExportProbe.Profil()), m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_KONDITIONIERUNG);

            AbbildRaum zurueck = IfcExportProbe.Lesen(datei).Gebaeude[0].Raeume.Single(r => r.Name == raum.Name);
            Gleich(raum.Konditionierung, zurueck.Konditionierung);

            // Ein Periodentext in unbekanntem Format: die Periode fällt benannt weg, der Rest des Kalenders bleibt.
            byte[] kaputt = IfcExportProbe.Schreiben(plan.Abbild, null, null, m =>
            {
                using (ITransaction t = m.BeginTransaction("Periode verfälschen"))
                {
                    var p = m.Instances.OfType<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>().First(x => x.Name == "Periode_110");
                    p.NominalValue = new IfcText("ZEITRAUM;1;5");
                    t.Commit();
                }
            }, out _);
            AbbildGebaeude g = IfcExportProbe.Lesen(kaputt).Gebaeude[0];
            PruefMeldung meldung = Assert.Single(g.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_KOND_UEBERSPRUNGEN");
            Assert.Contains("EPOS_Kalender_HEIZSOLL.Periode_110", meldung.Werte[2]);
            Konditionierungskalender heiz = g.Raeume.Single(r => r.Name == raum.Name).Konditionierung.KalenderVon(Konditionierungsgroesse.Heizsoll).Kalender;
            Assert.Equal(3, heiz.Perioden.Count);
        }

        [Fact]
        public void Ohne_Zone_und_ohne_Konditionierung_bleibt_die_Datei_wie_sie_war()
        {
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), IfcExportProbe.Profil());
            foreach (AbbildRaum r in plan.Abbild.Gebaeude[0].Raeume) r.Konditionierung = null;
            string text = System.Text.Encoding.UTF8.GetString(IfcExportProbe.Schreiben(plan.Abbild));
            Assert.DoesNotContain(IfcKonditionierungssatz.PRAEFIX_KALENDER, text);
            Assert.DoesNotContain(IfcKonditionierungssatz.HEIZSOLL_TAG, text);
            Assert.DoesNotContain(new IfcSchreiber().Vorschau(plan.Abbild, IfcExportProbe.Profil()), m => m.Schluessel == GebaeudeExportAblauf.BEIPACK_KONDITIONIERUNG);
        }

        // ==================================================================
        //  Gegenproben: Dateien ohne EPOS_*
        // ==================================================================

        [Theory]
        [InlineData("ifc4_zonen.ifc")]
        [InlineData("hottcad")]
        public void Gegenprobe_Datei_ohne_EPOS_Saetze_ergibt_keine_Konditionierung(string datei)
        {
            string pfad = datei == "hottcad" ? SqprojProbenErzeuger.HottcadZonenhaus(Wurzel()) : GbxmlImportTests.Probe(datei);
            try
            {
                GebaeudeAbbild a;
                using (FileStream s = File.OpenRead(pfad))
                    a = new IfcLeser().Lesen(s, new IfcImportProfil(), null, System.Threading.CancellationToken.None);
                Assert.NotEmpty(a.Gebaeude.SelectMany(g => g.Raeume));
                Assert.All(a.Gebaeude.SelectMany(g => g.Raeume), r => Assert.Null(r.Konditionierung));
                Assert.All(Zonenplan.Vorschlag(a, 0).Zonen, z => Assert.Null(z.Projektdatei));
            }
            finally
            {
                if (datei == "hottcad") File.Delete(pfad);
            }
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>Eine Zone mit Heizkalender (Woche, vier Perioden aller Angabearten), Personen mit Nennwert und Wertgrundangabe, Lüftung „aus“.</summary>
        internal static AbbildKonditionierung Synthetisch()
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int i = 0; i < woche.Length; i++) woche[i] = i % 24 >= 7 && i % 24 < 17 ? 21.5 : i % 24 == 23 ? double.NaN : 17.25;
            var ferien = (double[])woche.Clone();
            for (int i = 0; i < ferien.Length; i++) if (double.IsFinite(ferien[i])) ferien[i] = 16.0;
            var heiz = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(woche), null, new[]
            {
                Kalenderregel.Zeitraum(110, DbWerte.KOND_ART_FERIEN, "Sommerferien; lang", 200, 240, Kalenderangabe.AusWoche(ferien)),
                Kalenderregel.Zeitraum(120, DbWerte.KOND_ART_ZEITRAUM, "Winterpause", 355, 5, Kalenderangabe.Abgeschaltet),
                Kalenderregel.Zeitraum(130, DbWerte.KOND_ART_BETRIEBSPAUSE, "Revision", 100, 101, Kalenderangabe.AusWert(18.5)),
                Kalenderregel.Feiertag(140, "Feiertage", Feiertage.Regeln[0], Kalenderangabe.AlsWochentag(7)),
            });
            var personen = new Konditionierungskalender(Konditionierungsgroesse.Personen, Kalenderangabe.AusWert(0.35), 420.5, null);
            var lueftung = new Konditionierungskalender(Konditionierungsgroesse.Lueftung, Kalenderangabe.Abgeschaltet, null, null);
            var k = new AbbildKonditionierung
            {
                Nutzung = DbWerte.KOND_NUTZUNG_BUERO, HeizsollTagC = 21.5, HeizsollNachtC = 17.25, KuehlsollC = 26.0, LuftwechselNutzerJeH = 0.45,
            };
            k.Kalender.Add(new AbbildKalender(heiz, "aus Vorlage Büro · Zeitfenster"));
            k.Kalender.Add(new AbbildKalender(lueftung, null));
            k.Kalender.Add(new AbbildKalender(personen, "Ä-Ö-Ü ß"));
            return k;
        }

        /// <summary>Gleichheit zweier Konditionierungen: Zellen, Nutzung, je Kalender Grundangabe, Woche byteweise, Nennwert, Bemerkung, Perioden.</summary>
        internal static void Gleich(AbbildKonditionierung a, AbbildKonditionierung b)
        {
            Assert.NotNull(b);
            Assert.Equal(a.Nutzung, b.Nutzung);
            Assert.Equal(a.HeizsollTagC, b.HeizsollTagC);
            Assert.Equal(a.HeizsollNachtC, b.HeizsollNachtC);
            Assert.Equal(a.KuehlsollC, b.KuehlsollC);
            Assert.Equal(a.LuftwechselNutzerJeH, b.LuftwechselNutzerJeH);
            Assert.Equal(a.Kalender.Select(k => k.Kalender.Groesse), b.Kalender.Select(k => k.Kalender.Groesse));
            foreach (AbbildKalender ka in a.Kalender)
            {
                AbbildKalender kb = b.KalenderVon(ka.Kalender.Groesse);
                Konditionierungsgroesse g = ka.Kalender.Groesse;
                Assert.Equal(ka.Kalender.Grundangabe.Art, kb.Kalender.Grundangabe.Art);
                if (ka.Kalender.Grundangabe.Art == Angabeart.Wert) Assert.Equal(ka.Kalender.Grundangabe.Wert, kb.Kalender.Grundangabe.Wert);
                if (ka.Kalender.Standardwoche != null)
                    Assert.Equal(Kalenderwoche.Schreiben(ka.Kalender.Standardwoche, g), Kalenderwoche.Schreiben(kb.Kalender.Standardwoche, g));
                Assert.Equal(ka.Kalender.Nennwert, kb.Kalender.Nennwert);
                Assert.Equal(string.IsNullOrWhiteSpace(ka.Bemerkung) ? null : ka.Bemerkung.Trim(), kb.Bemerkung);
                Assert.Equal(ka.Kalender.Perioden.Select(p => p.Rang + "|" + p.Bezeichner + "|" + IfcKonditionierungssatz.Periodentext(p, g)),
                             kb.Kalender.Perioden.Select(p => p.Rang + "|" + p.Bezeichner + "|" + IfcKonditionierungssatz.Periodentext(p, g)));
            }
        }

        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj"))) d = d.Parent;
            return d!.FullName;
        }
    }
}
