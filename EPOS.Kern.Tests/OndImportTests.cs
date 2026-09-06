using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des OND-Imports</b> (Anwenderentscheid <b>W6‑O‑1</b> vom
    /// 06.09.2026: „der OND-Import soll umgesetzt werden";
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c>, Kapitel 5.2).
    ///
    /// <para><b>Zwei synthetische Proben</b> unter
    /// <c>Referenzlaeufe/Importproben</c>: <c>ond_muster_2500tl.ond</c> trägt Zahl für
    /// Zahl das Prüfbeispiel aus Anhang A des Konzepts (2,50 kW, 1 MPPT, 80…500 V,
    /// 600 V, 12,0 A, η 0,900/0,940/0,962/0,970/0,975/0,970), und
    /// <c>ond_muster_10000tl_3profile.ond</c> führt <c>ProfilPIO</c> in den DREI
    /// Fassungen, die PVsyst schreibt — untere, nominale und obere MPP-Spannung.
    /// Beide sind ANSI/Windows‑1252 kodiert und tragen Umlaute, damit die Kodierung
    /// nicht nur behauptet ist.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche.</b> Der OND-Zweig endet am
    /// <c>WechselrichterModel</c>; was danach kommt (Dublettenprüfung, Schreibweg)
    /// prüft <c>WechselrichterKatalogTests</c>, und zwar für beide Quellen gleich —
    /// genau das ist die Zusage des Konzepts.</para>
    ///
    /// <para>Die Kultur ist auf de-DE gepinnt: Die Datei führt den DEZIMALPUNKT, und
    /// ein Läufer in einer Kultur mit Komma darf daran nichts ändern.</para>
    /// </summary>
    public class OndImportTests
    {
        public OndImportTests()
        {
            var de = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = de;
            CultureInfo.DefaultThreadCurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        private const double GENAU = 1e-6;

        // =================================================================================
        // 1 — Das Zerlegen
        // =================================================================================

        /// <summary>
        /// Die Kopfdaten der Probe — Hersteller, Typ, Baujahr und der Bezeichner, der
        /// wie beim PAN-Import aus <c>Manufacturer</c> und <c>Model</c> entsteht.
        /// </summary>
        [Fact]
        public void Die_Probe_traegt_ihre_Kopfdaten()
        {
            OndWechselrichter g = Muster();

            Assert.Equal("Musterwerk", g.Manufacturer);
            Assert.Equal("Muster 2500TL", g.Model);
            Assert.Equal(2024, g.YearBeg);
            Assert.Equal("Musterwerk Muster 2500TL", g.Name);
            Assert.Equal("Musterwerk", g.Hersteller);
        }

        /// <summary>
        /// <b>ANSI/Windows‑1252, nicht UTF‑8</b> (dieselbe Auflage wie beim PAN-Import,
        /// <c>KONTEXT_Importkodierung_ANSI.md</c>): Der Kommentar der Probe trägt „ü";
        /// als UTF‑8 gelesen stünde dort U+FFFD, und der Text landete beschädigt im
        /// Katalog.
        /// </summary>
        [Fact]
        public void Die_Datei_wird_als_ANSI_gelesen()
        {
            OndWechselrichter g = Muster();

            Assert.Contains("Prüfprobe", g.Comment);
            Assert.DoesNotContain("�", g.Comment);
        }

        /// <summary>
        /// Die Zahlen des Anhangs A, in den Einheiten der DATEI: Leistungen in kW,
        /// Schwellen in W, Wirkungsgrade in Prozent.
        /// </summary>
        [Fact]
        public void Die_Zahlen_der_Datei_stehen_in_den_Einheiten_von_PVsyst()
        {
            OndWechselrichter g = Muster();

            Assert.Equal(2.500, g.PNomConv, 6);
            Assert.Equal(2.500, g.PMaxOUT, 6);
            Assert.Equal(2.600, g.PNomDC, 6);
            Assert.Equal(2.750, g.PMaxDC, 6);

            Assert.Equal(80.0, g.VMppMin, 6);
            Assert.Equal(350.0, g.VMppNom, 6);
            Assert.Equal(500.0, g.VMPPMax, 6);
            Assert.Equal(600.0, g.VAbsMax, 6);
            Assert.Equal(100.0, g.VStart, 6);
            Assert.Equal(12.0, g.IMaxDC, 6);

            Assert.Equal(12.0, g.PSeuil, 6);     // W
            Assert.Equal(0.50, g.Pnight, 6);     // W
            Assert.Equal(1, g.NbMPPT);
            Assert.Equal(1, g.NbInputs);

            Assert.Equal(97.50, g.EfficMax, 6);  // %
            Assert.Equal(96.80, g.EfficEuro, 6); // %
        }

        /// <summary>
        /// <b>Eine Datei ohne <c>pvGInverter</c> ist kein Gerät.</b> Der Zerleger gibt
        /// <c>null</c> zurück, statt einen leeren Katalogsatz zu bauen — eine
        /// <c>.pan</c>-Datei, die versehentlich hier landet, wird damit erkannt.
        /// </summary>
        [Fact]
        public void Eine_fremde_Datei_ist_kein_Wechselrichter()
        {
            Assert.Null(OndWechselrichterDienst.Zerlege("PVObject_=pvModule\r\n  PNom=400\r\n", "x.pan"));
            Assert.Null(OndWechselrichterDienst.Zerlege("", "leer.ond"));
        }

        // =================================================================================
        // 2 — Die Kennlinie (Konzept 3.3.1 und 5.2)
        // =================================================================================

        /// <summary>
        /// <b>Die sechs Stützstellen des Anhangs A</b>, aus der Wertetabelle
        /// <c>ProfilPIO</c> interpoliert: 0,900 / 0,940 / 0,962 / 0,970 / 0,975 / 0,970
        /// bei 5, 10, 20, 30, 50 und 100 % der AC-Nennleistung.
        /// </summary>
        [Fact]
        public void Die_sechs_Stuetzstellen_kommen_aus_der_Wertetabelle()
        {
            double?[] etas = Muster().Stuetzstellen();

            double[] soll = { 0.900, 0.940, 0.962, 0.970, 0.975, 0.970 };
            for (int i = 0; i < soll.Length; i++)
            {
                Assert.True(etas[i].HasValue, "Stützstelle " + i + " fehlt.");
                Assert.Equal(soll[i], etas[i].Value, GENAU);
            }
        }

        /// <summary>
        /// <b>Zwischen den Stützpunkten wird linear interpoliert</b>, und außerhalb der
        /// Tabelle wird NICHTS fortgeschrieben: Eine Nennleistung, die die Tabelle nicht
        /// abdeckt, liefert <c>null</c> statt einer erfundenen Zahl.
        /// </summary>
        [Fact]
        public void Ausserhalb_der_Tabelle_bleibt_die_Stuetzstelle_leer()
        {
            var punkte = new List<(double PIn, double POut)>
            {
                (0.0, 0.0), (1100.0, 1000.0), (2100.0, 2000.0)
            };

            // Nennleistung 2 000 W: 5 % = 100 W liegt UNTER dem kleinsten Punkt (1 000 W).
            double?[] etas = WechselrichterKennlinie.AusProfil(punkte, 2000.0);

            Assert.Null(etas[0]);                                   // 5 %
            Assert.Null(etas[1]);                                   // 10 %
            Assert.Null(etas[2]);                                   // 20 %
            Assert.True(etas[5].HasValue);                          // 100 % = 2 000 W
            Assert.Equal(2000.0 / 2100.0, etas[5].Value, GENAU);

            // 50 % = 1 000 W trifft den ersten Punkt exakt.
            Assert.Equal(1000.0 / 1100.0, etas[4].Value, GENAU);
        }

        /// <summary>
        /// Ein Punktepaar ohne Leistung (der Nullpunkt, den PVsyst als ersten schreibt)
        /// und ein Wirkungsgrad über 1 werden verworfen, statt die Interpolation zu
        /// vergiften.
        /// </summary>
        [Fact]
        public void Unbrauchbare_Punkte_werden_verworfen()
        {
            var punkte = new List<(double PIn, double POut)>
            {
                (0.0, 0.0),          // Nullpunkt: eta nicht definiert
                (900.0, 1000.0),     // eta > 1: unmoeglich
                (1050.0, 1000.0),
                (2100.0, 2000.0)
            };

            double?[] etas = WechselrichterKennlinie.AusProfil(punkte, 2000.0);

            Assert.Equal(1000.0 / 1050.0, etas[4].Value, GENAU);
            Assert.Equal(2000.0 / 2100.0, etas[5].Value, GENAU);
        }

        /// <summary>Ohne Punkte oder ohne Nennleistung sind alle sechs leer.</summary>
        [Fact]
        public void Ohne_Tabelle_gibt_es_keine_Stuetzstellen()
        {
            Assert.All(WechselrichterKennlinie.AusProfil(null, 2500.0), e => Assert.Null(e));
            Assert.All(WechselrichterKennlinie.AusProfil(
                new List<(double, double)> { (1000.0, 970.0) }, 0.0), e => Assert.Null(e));
        }

        // =================================================================================
        // 3 — Drei ProfilPIO-Fassungen: die NOMINALE gilt
        // =================================================================================

        /// <summary>
        /// <b>PVsyst führt <c>ProfilPIO</c> in drei Fassungen</b> — untere, nominale und
        /// obere MPP-Spannung. Genommen wird die NOMINALE (<c>ProfilPIOV2</c>, Konzept
        /// 5.2); die anderen zwei brauchte erst ein spannungsabhängiges Modell
        /// (Stufe E3, zurückgestellt). Welche es war, steht in der Beschreibung des
        /// Katalogsatzes.
        /// </summary>
        [Fact]
        public void Bei_drei_Fassungen_gilt_die_nominale()
        {
            OndWechselrichter g = Drei();

            Assert.Equal("ProfilPIOV2", g.Kennlinienfassung);

            double?[] etas = g.Stuetzstellen();
            double[] soll = { 0.960, 0.965, 0.975, 0.980, 0.982, 0.975 };
            for (int i = 0; i < soll.Length; i++)
                Assert.Equal(soll[i], etas[i].Value, GENAU);

            // Die untere Fassung liegt um 0,020 darunter, die obere um 0,005 darüber -
            // beide sind gelesen, aber KEINE von beiden wird genommen.
            Assert.DoesNotContain(etas, e => e.HasValue && Math.Abs(e.Value - 0.940) < GENAU);
            Assert.DoesNotContain(etas, e => e.HasValue && Math.Abs(e.Value - 0.965 - 0.005) < GENAU);

            Assert.Contains("ProfilPIOV2", g.NachModell().m_szBeschreibung);
        }

        /// <summary>
        /// Führt die Datei nur EINE Fassung, heißt sie <c>ProfilPIO</c> und wird ohne
        /// Umstände genommen.
        /// </summary>
        [Fact]
        public void Bei_einer_Fassung_gilt_diese()
        {
            Assert.Equal("ProfilPIO", Muster().Kennlinienfassung);
        }

        // =================================================================================
        // 4 — Der Katalogsatz (Konzept 5.2)
        // =================================================================================

        /// <summary>
        /// <b>Die Feldzuordnung aus Konzept 5.2</b> — und mit ihr die vier Größen, die
        /// der CEC-Import offen lässt (offener Punkt W6‑O‑2): Scheinleistung,
        /// DC-Leistung, Einschaltspannung und MPPT-Zahl.
        /// </summary>
        [Fact]
        public void Der_Katalogsatz_traegt_die_Werte_der_Datei()
        {
            WechselrichterModel m = Muster().NachModell();

            Assert.Equal("Musterwerk Muster 2500TL", m.m_szName);
            Assert.Equal("Musterwerk", m.m_szFirma);

            Assert.Equal(2.500, m.m_P_AC_Nenn.Value, 6);       // kW
            Assert.Equal(2.500, m.m_S_AC_Max.Value, 6);        // kVA-Ausweis aus PMaxOUT
            Assert.Equal(2.750, m.m_P_DC_Max.Value, 6);        // der groessere aus PMaxDC/PNomDC
            Assert.Equal(80.0, m.m_U_Mpp_Min.Value, 6);
            Assert.Equal(500.0, m.m_U_Mpp_Max.Value, 6);
            Assert.Equal(600.0, m.m_U_Dc_Max.Value, 6);
            Assert.Equal(100.0, m.m_U_Start.Value, 6);
            Assert.Equal(12.0, m.m_I_Dc_Max.Value, 6);
            Assert.Equal(1, m.m_Anzahl_Mppt.Value);
            Assert.Null(m.m_Straenge_Je_Mppt);

            Assert.Equal(12.0, m.m_P_Standby.Value, 6);        // W
            Assert.Equal(0.50, m.m_P_Nacht.Value, 6);          // W

            // Wirkungsgrade als FAKTOR 0…1, nicht als Prozent.
            Assert.Equal(0.9680, m.m_Eta_Euro.Value, 6);
            Assert.Equal(0.9750, m.m_Eta_Max.Value, 6);
            Assert.Equal(0.900, m.m_Eta05.Value, GENAU);
            Assert.Equal(0.970, m.m_Eta100.Value, GENAU);

            // Eine OND-Datei fuehrt kein Sandia-Modell; VMppNom ist die Bezugsspannung.
            Assert.Null(m.m_Sandia_Pdco);
            Assert.Null(m.m_Sandia_C0);
            Assert.Equal(350.0, m.m_Sandia_Vdco.Value, 6);
        }

        /// <summary>
        /// <b>Die Herkunft steht im Katalog</b> (Konzept 3.1): <c>OND</c> statt
        /// <c>CEC</c> oder <c>HAND</c>. Daran erkennt der Anwender in der Verwaltung,
        /// woher die Zahlen stammen — und die Dublettenprüfung vergleicht die Spalte
        /// mit.
        /// </summary>
        [Fact]
        public void Die_Herkunft_ist_OND()
        {
            Assert.Equal(DbWerte.WR_HERKUNFT_OND, Muster().NachModell().m_Herkunft);
            Assert.Equal("OND", DbWerte.WR_HERKUNFT_OND);

            IDictionary<string, object> werte = Muster().Vergleichswerte("Musterwerk Muster 2500TL");
            Assert.Equal(DbWerte.WR_HERKUNFT_OND, werte[WechselrichterSchema.SPALTE_HERKUNFT]);
        }

        /// <summary>
        /// <b>Die Vergleichswerte sind die des CEC-Zweigs</b> — genau die
        /// <c>ImportSpalten</c> der Registry-Definition „WECHSELRICHTER". Damit läuft
        /// der Konfliktweg für beide Quellen gleich, und ein Gerät, das erst aus CEC und
        /// dann aus einer OND-Datei kommt, wird als Dublette erkannt.
        /// </summary>
        [Fact]
        public void Der_Konfliktweg_vergleicht_dieselben_Spalten_wie_bei_CEC()
        {
            KatalogDefinition katalog = KatalogRegistry.Finde("WECHSELRICHTER");

            IDictionary<string, object> ond = Muster().Vergleichswerte("X");
            IDictionary<string, object> cec = new CecWechselrichter
            {
                Name = "X", Paco = 2500, Pdco = 2580, Pso = 12, C0 = -8e-06,
                Vdcmax = 600, Idcmax = 12, MpptLow = 80, MpptHigh = 500
            }.Vergleichswerte("X");

            Assert.Equal(cec.Keys.OrderBy(k => k), ond.Keys.OrderBy(k => k));
            foreach (string spalte in katalog.ImportSpalten)
                Assert.True(ond.ContainsKey(spalte), "Die Spalte " + spalte + " fehlt im OND-Kandidaten.");
        }

        /// <summary>
        /// <b>Die Plausibilitätsprüfung trägt auch für OND</b> — dieselbe wie beim
        /// CEC-Import: Das Muster 2500TL geht sauber durch.
        /// </summary>
        [Fact]
        public void Das_Muster_ist_plausibel()
        {
            WechselrichterPlausibilitaet.Befund b =
                WechselrichterPlausibilitaet.Pruefe(Muster().NachModell());

            Assert.True(b.Ok, string.Join(" | ", b.Fehler));
            Assert.Empty(b.Warnungen);
        }

        // =================================================================================
        // 5 — Der Dienst
        // =================================================================================

        /// <summary>
        /// Der Dienst liest die Datei, meldet die Zahl der Geräte als SCHLÜSSEL und
        /// sammelt mehrere Dateien einer Sitzung. Eine erneut eingelesene Datei ERSETZT
        /// ihren Altbestand — wörtlich <c>PanDataService.Aufnehmen</c>.
        /// </summary>
        [Fact]
        public void Der_Dienst_sammelt_die_Dateien_einer_Sitzung()
        {
            var dienst = new OndWechselrichterDienst();

            (bool Erfolg, CecFortschritt Meldung) r = dienst.AusDatei(Probe("ond_muster_2500tl.ond"));
            Assert.True(r.Erfolg);
            Assert.Equal("OND_MSG_GELESEN", r.Meldung.Schluessel);
            Assert.Equal("1", r.Meldung.Werte[0]);
            Assert.Single(dienst.AlleGeraete);

            dienst.AusDatei(Probe("ond_muster_10000tl_3profile.ond"));
            Assert.Equal(2, dienst.AlleGeraete.Count);

            // Dieselbe Datei noch einmal: sie ersetzt, sie verdoppelt nicht.
            dienst.AusDatei(Probe("ond_muster_2500tl.ond"));
            Assert.Equal(2, dienst.AlleGeraete.Count);

            Assert.Equal(new[] { "Musterwerk" }, dienst.Hersteller().ToArray());
        }

        /// <summary>Eine fehlende Datei meldet sich, statt zu werfen.</summary>
        [Fact]
        public void Eine_fehlende_Datei_meldet_sich()
        {
            (bool Erfolg, CecFortschritt Meldung) r = new OndWechselrichterDienst()
                .AusDatei(Path.Combine(Path.GetTempPath(), "gibt-es-nicht.ond"));

            Assert.False(r.Erfolg);
            Assert.Equal("OND_MSG_DATEI_FEHLT", r.Meldung.Schluessel);
        }

        /// <summary>
        /// Eine Datei, die kein <c>pvGInverter</c>-Objekt ist, meldet sich ebenfalls —
        /// und schreibt nichts in die Sitzungsliste.
        /// </summary>
        [Fact]
        public void Eine_fremde_Datei_meldet_sich()
        {
            string pfad = Path.Combine(Path.GetTempPath(),
                "epos-ond-probe-" + Guid.NewGuid().ToString("N") + ".ond");
            File.WriteAllText(pfad, "PVObject_=pvModule\r\n  PNom=400\r\n");

            try
            {
                var dienst = new OndWechselrichterDienst();
                (bool Erfolg, CecFortschritt Meldung) r = dienst.AusDatei(pfad);

                Assert.False(r.Erfolg);
                Assert.Equal("OND_MSG_KEIN_GERAET", r.Meldung.Schluessel);
                Assert.Empty(dienst.AlleGeraete);
            }
            finally
            {
                File.Delete(pfad);
            }
        }

        // =================================================================================
        // 6 — Die Zeilenform des einen Importwirts (W6‑O‑1)
        // =================================================================================

        /// <summary>
        /// <b>Beide Quellen füllen DIESELBE Zeilenform</b> — das ist die Zusage des
        /// einen Importwirts: <c>ModulImportProfil.Zeile</c> nimmt einen
        /// <c>CecWechselrichter</c> ebenso wie einen <c>OndWechselrichter</c> und
        /// liefert denselben Spalten- und Feldsatz; verschieden ist nur die Herkunft.
        /// </summary>
        [Fact]
        public void Beide_Quellen_fuellen_dieselbe_Zeilenform()
        {
            ModulImportProfil profil = ModulImportProfil.Finde(ModulImportArt.Wechselrichter);

            ImportZeile ond = profil.Zeile(0, Muster());
            ImportZeile cec = profil.Zeile(1, new CecWechselrichter
            {
                Name = "Alpha AG: A-3000", Paco = 3000, Pdco = 3150, Pso = 18,
                C0 = -8e-06, Vdcmax = 600, Idcmax = 12, MpptLow = 100, MpptHigh = 480
            });

            Assert.Equal(cec.Spalten.Keys.OrderBy(k => k), ond.Spalten.Keys.OrderBy(k => k));
            Assert.Equal(cec.Felder.Keys.OrderBy(k => k), ond.Felder.Keys.OrderBy(k => k));

            Assert.Equal("OND", ond.Spalte(ModulImportProfil.SpalteQuelle));
            Assert.Equal("CEC", cec.Spalte(ModulImportProfil.SpalteQuelle));

            // Jede Spalte des Profils ist in der Zeile belegt - keine leere Zelle,
            // weil eine Quelle eine Spalte vergessen hat.
            foreach (ImportSpalte spalte in profil.Spalten)
            {
                Assert.True(ond.Spalten.ContainsKey(spalte.Schluessel), spalte.Schluessel + " (OND)");
                Assert.True(cec.Spalten.ContainsKey(spalte.Schluessel), spalte.Schluessel + " (CEC)");
            }
            foreach (ImportFeld feld in profil.Felder)
            {
                Assert.True(ond.Felder.ContainsKey(feld.Schluessel), feld.Schluessel + " (OND)");
                Assert.True(cec.Felder.ContainsKey(feld.Schluessel), feld.Schluessel + " (CEC)");
            }
        }

        /// <summary>
        /// <b>Der Wirt trägt beide Ausprägungen vollständig</b>: Jede Ausprägung nennt
        /// ihren Katalog, ihre Quellen (Netz und Datei), ihre Spalten, Reiter und
        /// Felder — und jedes Feld sitzt in einem Reiter, den es gibt.
        /// </summary>
        [Fact]
        public void Beide_Auspraegungen_sind_vollstaendig()
        {
            foreach (ModulImportArt art in ModulImportProfil.AlleArten)
            {
                ModulImportProfil p = ModulImportProfil.Finde(art);

                Assert.False(string.IsNullOrEmpty(p.Katalog));
                Assert.NotNull(KatalogRegistry.Finde(p.Katalog));

                Assert.Equal(3, p.Quellen.Count);
                Assert.Single(p.Quellen, q => q.AusDemNetz);
                Assert.Single(p.Quellen, q => q.Primaer);
                foreach (ImportQuelle q in p.Quellen.Where(q => !q.AusDemNetz))
                {
                    Assert.False(string.IsNullOrEmpty(q.Dateifilter));
                    Assert.False(string.IsNullOrEmpty(q.Unterordner));
                }

                Assert.NotEmpty(p.Spalten);
                Assert.NotEmpty(p.Reiter);
                Assert.NotEmpty(p.Felder);
                Assert.NotEmpty(p.Zahlenfilter);
                Assert.False(string.IsNullOrEmpty(p.HilfeSchluessel));

                foreach (ImportFeld feld in p.Felder)
                    Assert.InRange(feld.Reiter, 0, p.Reiter.Count - 1);
            }
        }

        /// <summary>
        /// <b>Die zwei Hilfeschlüssel bleiben gültig</b> (Auflage von W6‑O‑1): Der eine
        /// Wirt führt weiterhin den Schlüssel der jeweiligen Maske, keiner wandert.
        /// </summary>
        [Fact]
        public void Beide_Hilfeschluessel_bleiben_stehen()
        {
            Assert.Equal("Main_PV_Test.btn_Help",
                ModulImportProfil.Finde(ModulImportArt.Photovoltaik).HilfeSchluessel);
            Assert.Equal("Form_WechselrichterImport.btn_Help",
                ModulImportProfil.Finde(ModulImportArt.Wechselrichter).HilfeSchluessel);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static OndWechselrichter Muster() => Lies("ond_muster_2500tl.ond");

        private static OndWechselrichter Drei() => Lies("ond_muster_10000tl_3profile.ond");

        private static OndWechselrichter Lies(string name)
        {
            string pfad = Probe(name);
            return OndWechselrichterDienst.Zerlege(
                File.ReadAllText(pfad, AnsiEncoding.Get()), Path.GetFileName(pfad));
        }

        /// <summary>
        /// Die Importprobe unter <c>Referenzlaeufe/Importproben</c> — dasselbe
        /// Aufwärtssuchen wie in <c>KatalogImportTests</c>.
        /// </summary>
        private static string Probe(string name)
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", name);
                if (File.Exists(kandidat)) return kandidat;
            }

            Assert.Fail("Die Importprobe " + name + " wurde nicht gefunden.");
            return null;
        }
    }
}
