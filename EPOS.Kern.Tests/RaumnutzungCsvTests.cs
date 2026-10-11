using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das CSV-Format der Nutzungsprofile</b> (Stufe NP4a; Konzept Nutzungsprofile 6.4, NP-F11, NP-F20) ohne Datenbank:
    /// Rundlauf Schreiben → Lesen mit denselben Werten (Kennwerte, Stundenprofile, Zeilenbild, Texte mit Trenner und
    /// Anführungszeichen), jede Meldungsart (übernommen, ignoriert, Fehler mit Grund), Komma statt Punkt benannt abgelehnt,
    /// BOM toleriert, Doppelte der Datei abgelehnt und die Beispieldatei der Proben.
    /// </summary>
    public sealed class RaumnutzungCsvTests
    {
        internal const string KOPF = "Nummer;Bezeichner;Heiz_Soll;Heiz_Soll_Ausserhalb";

        internal static string Beispielpfad()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests", "Proben", "Nutzungsprofile", "nutzungsprofile_beispiel.csv");
                if (File.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Die Beispieldatei fehlt.");
            return null;
        }

        /// <summary>Ein Profil, das jede Spalte des Formats belegt — runde Phantasiewerte.</summary>
        internal static Raumnutzungsprofil Vollprofil(string name, string nummer) => new Raumnutzungsprofil
        {
            Nummer = nummer,
            Bezeichner = name,
            Beschreibung = "Text mit ; Trenner und \"Zitat\"",
            Nutzung_Von = 7, Nutzung_Bis = 18, Betrieb_Von = 6, Betrieb_Bis = 19,
            Nutzungstage_Woche = "0111110", Feiertage_Wie_Sonntag = true,
            Heiz_Soll = 20.5, Heiz_Soll_Ausserhalb = 15, Heiz_Aus_Ausserhalb = false,
            Kuehl_Soll = 25, Kuehl_Soll_Ausserhalb = 30, Kuehl_Aus_Ausserhalb = true,
            Aussenluft = 2.5, Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_FLAECHE, Aussenluft_Ausserhalb = 0.5,
            Personen_Flaeche = 20, Personen_Waerme = 100, Personen_Anteil = 0.75, Personen_Anteil_Ausserhalb = 0.1,
            Geraete_Leistung = 5, Geraete_Anteil = 1, Geraete_Anteil_Ausserhalb = 0.2,
            Beleuchtung_Leistung = 5, Beleuchtung_Anteil = 0.5,
            Stunden = new List<Raumnutzungsstunden>
            {
                new Raumnutzungsstunden(DbWerte.KOND_GROESSE_GERAETE, RaumnutzungSchema.TAGESART_WERKTAG,
                                        string.Join(";", Enumerable.Range(0, 24).Select(h => h is >= 8 and < 18 ? "1" : "0.25"))),
                new Raumnutzungsstunden(DbWerte.KOND_GROESSE_HEIZSOLL, RaumnutzungSchema.TAGESART_FREI,
                                        string.Join(";", Enumerable.Range(0, 24).Select(h => h < 6 ? "aus" : "20"))),
            },
            Zeilen = new List<Vorgabezeile>
            {
                new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_PERSONEN, Zeile = DbWerte.KOND_ZEILE_TAG, Wert = 0.5 },
                new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_PERSONEN, Zeile = DbWerte.KOND_ZEILE_NACHT, Wert = 1, Von = 22, Bis = 6 },
                new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_HEIZSOLL, Zeile = DbWerte.KOND_ZEILE_WOCHENENDE, Aus = true },
            },
        };

        /// <summary>Kennwerte, Zeilenbild und Stunden als vergleichbarer Text.</summary>
        internal static string Abdruck(Raumnutzungsprofil p)
        {
            var sb = new StringBuilder();
            sb.Append(p.Nummer).Append('|').Append(p.Bezeichner).Append('|').Append(p.Beschreibung).Append('|');
            foreach (object w in p.Kennwerte()) sb.Append(Convert.ToString(w, CultureInfo.InvariantCulture)).Append('|');
            foreach (Vorgabezeile z in (p.Zeilen ?? new List<Vorgabezeile>()).OrderBy(z => z.Groesse).ThenBy(z => z.Zeile))
                sb.Append(z.Groesse).Append('/').Append(z.Zeile).Append('=').Append(RaumnutzungCsv.Zelltext(z)).Append('|');
            foreach (Raumnutzungsstunden s in (p.Stunden ?? new List<Raumnutzungsstunden>()).OrderBy(s => s.Groesse).ThenBy(s => s.Tagesart))
                sb.Append(s.Groesse).Append('/').Append(s.Tagesart).Append('=').Append(s.Werte).Append('|');
            return sb.ToString();
        }

        private static List<RaumnutzungCsvMeldung> Meldungen(RaumnutzungCsvLesung l, int zeile, string spalte)
            => l.Meldungen.Where(m => m.Zeile == zeile && m.Spalte == spalte).ToList();

        [Fact]
        public void Die_Kennwertspalten_sind_die_des_Schemas_ohne_Nutzungstage_Jahr()
        {
            Assert.Equal(RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte).Where(s => s != RaumnutzungCsv.SPALTE_TAGE_JAHR),
                         RaumnutzungCsv.Kennwertspalten);
        }

        [Fact]
        public void Rundlauf_Schreiben_Lesen_ergibt_dieselben_Werte()
        {
            var profile = new List<Raumnutzungsprofil>
            {
                Vollprofil("Beispiel voll", "1.1"),
                new Raumnutzungsprofil { Bezeichner = "Beispiel leer" },
                new Raumnutzungsprofil { Bezeichner = " führt Leerzeichen", Nummer = "7", Heiz_Soll = 19.25 },
            };
            profile[2].Bezeichner = "Umlaut ä ö ü ß";
            string text = RaumnutzungCsv.Schreiben(profile);
            Assert.DoesNotContain(RaumnutzungCsv.SPALTE_TAGE_JAHR, text);

            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(RaumnutzungCsv.KODIERUNG.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray());
            Assert.Null(l.Abbruch);
            Assert.Equal(3, l.Zeilen.Count);
            Assert.All(l.Zeilen, z => Assert.True(z.Uebernehmbar, z.Ablehnung));
            Assert.DoesNotContain(l.Meldungen, m => m.Art != RaumnutzungCsvArt.Uebernommen);
            for (int i = 0; i < profile.Count; i++)
                Assert.Equal(Abdruck(profile[i]), Abdruck(l.Zeilen[i].Profil));
            // Ein zweiter Rundlauf ist bytegleich.
            Assert.Equal(text, RaumnutzungCsv.Schreiben(l.Zeilen.Select(z => z.Profil)));
        }

        [Fact]
        public void Der_Schreiber_setzt_ein_BOM_und_der_Leser_toleriert_es()
        {
            byte[] mit = RaumnutzungCsv.SchreibenBytes(new[] { Vollprofil("Mit BOM", null) });
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, mit.Take(3).ToArray());
            byte[] ohne = mit.Skip(3).ToArray();
            Assert.Equal(Abdruck(RaumnutzungCsv.Lesen(ohne).Zeilen.Single().Profil),
                         Abdruck(RaumnutzungCsv.Lesen(mit).Zeilen.Single().Profil));
        }

        [Fact]
        public void Jede_Meldungsart_kommt_mit_Zeile_Spalte_und_Grund()
        {
            string text = "Bezeichner;Heiz_Soll;Unbekannt;Nutzungstage_Jahr;Kuehl_Soll\r\n" +
                          "Profil A;20;x;250;99\r\n" +
                          "Profil B;21;;;;überzählig\r\n";
            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(text);
            Assert.Null(l.Abbruch);

            // ignoriert: die unbekannte Spalte und die abgeleitete Zahl (E93) — in der Kopfzeile, einmal.
            Assert.Equal(RaumnutzungCsvArt.Ignoriert, Meldungen(l, 1, "Unbekannt").Single().Art);
            Assert.Equal(RaumnutzungCsvArt.Ignoriert, Meldungen(l, 1, RaumnutzungCsv.SPALTE_TAGE_JAHR).Single().Art);
            Assert.All(l.Meldungen.Where(m => m.Art == RaumnutzungCsvArt.Ignoriert), m => Assert.NotEqual("", m.Grund));
            // übernommen mit dem Wert.
            RaumnutzungCsvMeldung heiz = Meldungen(l, 2, "Heiz_Soll").Single();
            Assert.Equal(RaumnutzungCsvArt.Uebernommen, heiz.Art);
            Assert.Equal("20", heiz.Grund);
            // Fehler mit Grund (Grenzen der Kühlspalte): der Wert bleibt leer, das Profil bleibt übernehmbar (NP-F11).
            RaumnutzungCsvMeldung kuehl = Meldungen(l, 2, "Kuehl_Soll").Single();
            Assert.Equal(RaumnutzungCsvArt.Fehler, kuehl.Art);
            Assert.Contains("99", kuehl.Grund);
            Assert.Null(l.Zeilen[0].Profil.Kuehl_Soll);
            Assert.True(l.Zeilen[0].Uebernehmbar);
            // ein Wert ohne Spalte: ignoriert.
            Assert.Equal(RaumnutzungCsvArt.Ignoriert, Meldungen(l, 3, "#6").Single().Art);
            Assert.Equal(21.0, l.Zeilen[1].Profil.Heiz_Soll);
        }

        [Fact]
        public void Komma_statt_Punkt_wird_benannt_abgelehnt()
        {
            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(KOPF + "\n1;Komma;20,5;16\n");
            RaumnutzungCsvMeldung m = Meldungen(l, 2, "Heiz_Soll").Single();
            Assert.Equal(RaumnutzungCsvArt.Fehler, m.Art);
            Assert.Contains("20,5", m.Grund);
            Assert.Null(l.Zeilen.Single().Profil.Heiz_Soll);
            Assert.Equal(16.0, l.Zeilen.Single().Profil.Heiz_Soll_Ausserhalb);

            // Stundenwerte mit Komma ebenso.
            RaumnutzungCsvLesung s = RaumnutzungCsv.Lesen("Bezeichner;Stunden_GERAETE_WERKTAG\nS;" +
                                                          string.Join(" ", Enumerable.Repeat("0,5", 24)) + "\n");
            Assert.Equal(RaumnutzungCsvArt.Fehler, Meldungen(s, 2, "Stunden_GERAETE_WERKTAG").Single().Art);
            Assert.Empty(s.Zeilen.Single().Profil.Stunden);

            // Eine Kopfzeile mit Komma als Trenner: die Datei als Ganzes.
            RaumnutzungCsvLesung k = RaumnutzungCsv.Lesen("Nummer,Bezeichner,Heiz_Soll\n1,A,20\n");
            Assert.NotNull(k.Abbruch);
            Assert.Empty(k.Zeilen);
        }

        [Fact]
        public void Ohne_UTF8_ohne_Bezeichner_und_leer_bricht_die_Datei_benannt_ab()
        {
            byte[] latin = Encoding.Latin1.GetBytes("Bezeichner\nBüro\n");
            Assert.NotNull(RaumnutzungCsv.Lesen(latin).Abbruch);
            Assert.NotNull(RaumnutzungCsv.Lesen("Nummer;Heiz_Soll\n1;20\n").Abbruch);
            Assert.NotNull(RaumnutzungCsv.Lesen(Array.Empty<byte>()).Abbruch);
            Assert.NotNull(RaumnutzungCsv.Lesen("\r\n\r\n").Abbruch);
        }

        [Fact]
        public void Die_Absenkung_wird_in_den_Sollwert_ausserhalb_umgerechnet()
        {
            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(
                "Bezeichner;Heiz_Soll;Heiz_Absenkung_K;Heiz_Soll_Ausserhalb\n" +
                "Umgerechnet;20;4;\n" +
                "Vorrang;20;4;17\n" +
                "Ohne Soll;;4;\n");
            Assert.Equal(16.0, l.Zeilen[0].Profil.Heiz_Soll_Ausserhalb);
            Assert.Equal(RaumnutzungCsvArt.Uebernommen, Meldungen(l, 2, RaumnutzungCsv.SPALTE_ABSENKUNG).Single().Art);
            Assert.Equal(17.0, l.Zeilen[1].Profil.Heiz_Soll_Ausserhalb);
            Assert.Equal(RaumnutzungCsvArt.Ignoriert, Meldungen(l, 3, RaumnutzungCsv.SPALTE_ABSENKUNG).Single().Art);
            Assert.Null(l.Zeilen[2].Profil.Heiz_Soll_Ausserhalb);
            Assert.Equal(RaumnutzungCsvArt.Fehler, Meldungen(l, 4, RaumnutzungCsv.SPALTE_ABSENKUNG).Single().Art);
        }

        [Fact]
        public void Doppelte_Namen_und_Nummern_der_Datei_werden_als_Zeile_abgelehnt()
        {
            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(KOPF + "\n1;Raum;20;\n2;raum;21;\n1;Anderer;20;\n;;20;\n");
            Assert.True(l.Zeilen[0].Uebernehmbar);
            Assert.False(l.Zeilen[1].Uebernehmbar);   // Name ohne Unterschied der Schreibung (NP-F20)
            Assert.False(l.Zeilen[2].Uebernehmbar);   // Nummer (NP-F5)
            Assert.False(l.Zeilen[3].Uebernehmbar);   // ohne Bezeichner
            Assert.Single(l.Uebernehmbare);
            Assert.Equal(RaumnutzungCsvArt.Fehler, Meldungen(l, 3, RaumnutzungCsv.SPALTE_BEZEICHNER).Single().Art);
            Assert.Equal(RaumnutzungCsvArt.Fehler, Meldungen(l, 4, RaumnutzungCsv.SPALTE_NUMMER).Single().Art);
            // Eine abgelehnte Zeile meldet keinen Wert als übernommen.
            Assert.DoesNotContain(l.Meldungen, m => m.Zeile >= 3 && m.Art == RaumnutzungCsvArt.Uebernommen);
        }

        [Fact]
        public void Grenzen_Woche_Einheit_und_Stunden_werden_je_Wert_gemeldet()
        {
            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(
                "Bezeichner;Nutzung_Von;Nutzungstage_Woche;Personen_Anteil;Personen_Flaeche;Aussenluft;Feiertage_Wie_Sonntag;Stunden_PERSONEN_WERKTAG\n" +
                "Grenzen;25;111110;1.5;0;2;ja;" + string.Join(" ", Enumerable.Repeat("1", 23)) + "\n");
            RaumnutzungCsvZeile z = l.Zeilen.Single();
            foreach (string spalte in new[] { "Nutzung_Von", "Nutzungstage_Woche", "Personen_Anteil", "Personen_Flaeche",
                                              "Feiertage_Wie_Sonntag", "Stunden_PERSONEN_WERKTAG", "Aussenluft_Einheit" })
                Assert.Equal(RaumnutzungCsvArt.Fehler, Meldungen(l, 2, spalte).Single().Art);
            // Außenluft ohne Einheit: beide Werte bleiben leer, kein „übernommen" (NP-F10).
            Assert.Null(z.Profil.Aussenluft);
            Assert.Empty(Meldungen(l, 2, "Aussenluft"));
            Assert.True(z.Uebernehmbar);
            Assert.True(z.Profil.IstLeer);
        }

        [Fact]
        public void Die_Beispieldatei_liest_drei_Profile_ohne_Fehler()
        {
            RaumnutzungCsvLesung l = RaumnutzungCsv.Lesen(File.ReadAllBytes(Beispielpfad()));
            Assert.Null(l.Abbruch);
            Assert.Equal(3, l.Uebernehmbare.Count());
            Assert.DoesNotContain(l.Meldungen, m => m.Art != RaumnutzungCsvArt.Uebernommen);
            Assert.Equal(new[] { "B1", "B2", "B3" }, l.Zeilen.Select(z => z.Profil.Nummer));
            Assert.Contains(";", l.Zeilen[1].Profil.Beschreibung);
            Raumnutzungsprofil c = l.Zeilen[2].Profil;
            Assert.Equal(2, c.Stunden.Count);
            Assert.All(c.Stunden, s => Assert.Equal(DbWerte.KOND_GROESSE_PERSONEN, s.Groesse));
            Assert.Equal(RaumnutzungSchema.EINHEIT_JE_FLAECHE, c.Aussenluft_Einheit);
            Assert.All(l.Zeilen, z => Assert.Null(RaumnutzungCtrl.Profilpruefung(z.Profil.Kopie())));
        }
    }

    /// <summary>
    /// <b>Übernehmen und Export gegen die Testdatenbank</b> (Stufe NP4a): Übernehmen in eine neue oder eigene Kategorie, nie
    /// in eine ausgelieferte; vorhandenes Profil gleichen Namens ersetzen (Id bleibt) oder überspringen; eine Nummer, die
    /// das Ziel schon trägt, benannt abgelehnt; Rundlauf Export → Import einer ausgelieferten Kategorie mit Zeilenbild.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungCsvUebernahmeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static byte[] Beispiel() => File.ReadAllBytes(RaumnutzungCsvTests.Beispielpfad());

        [Fact]
        public void Uebernehmen_schreibt_in_eine_neue_eigene_Kategorie_und_der_Export_gibt_dieselben_Werte()
        {
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCsvLesung l = ctrl.CsvPruefen(Beispiel(), null);
            RaumnutzungCtrl.CsvBilanz b = ctrl.CsvUebernehmen(l, null, "Beispiel CSV", ersetzen: false);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(3, b.Angelegt);

            RaumnutzungCtrl.Kategorie k = ctrl.KategorieLesen(b.IdKatalog);
            Assert.Equal(RaumnutzungSchema.ART_EIGEN, k.Art);
            Assert.False(k.Ausgeliefert);
            List<Raumnutzungsprofil> gelesen = ctrl.Profile(b.IdKatalog);
            Assert.All(gelesen, p => Assert.False(p.Ausgeliefert));
            Assert.Equal(l.Zeilen.Select(z => RaumnutzungCsvTests.Abdruck(z.Profil)).OrderBy(x => x),
                         gelesen.Select(RaumnutzungCsvTests.Abdruck).OrderBy(x => x));

            RaumnutzungCtrl.CsvAusgabe a = ctrl.CsvExportieren(b.IdKatalog);
            Assert.True(a.Ok, a.Meldung);
            Assert.Equal(3, a.Profile);
            RaumnutzungCsvLesung zurueck = RaumnutzungCsv.Lesen(a.Inhalt);
            Assert.Equal(gelesen.Select(RaumnutzungCsvTests.Abdruck).OrderBy(x => x),
                         zurueck.Zeilen.Select(z => RaumnutzungCsvTests.Abdruck(z.Profil)).OrderBy(x => x));
        }

        [Fact]
        public void Rundlauf_einer_ausgelieferten_Kategorie_mit_Zeilenbild()
        {
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCtrl.Kategorie muster = ctrl.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER);
            List<Raumnutzungsprofil> quelle = ctrl.Profile(muster.Id);
            Assert.Contains(quelle, p => p.Zeilen.Count > 0);

            RaumnutzungCtrl.CsvAusgabe a = ctrl.CsvExportieren(muster.Id);
            Assert.True(a.Ok, a.Meldung);
            RaumnutzungCsvLesung l = ctrl.CsvPruefen(a.Inhalt, null);
            Assert.DoesNotContain(l.Meldungen, m => m.Art != RaumnutzungCsvArt.Uebernommen);
            RaumnutzungCtrl.CsvBilanz b = ctrl.CsvUebernehmen(l, null, "Muster aus CSV", false);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(quelle.Select(RaumnutzungCsvTests.Abdruck).OrderBy(x => x),
                         ctrl.Profile(b.IdKatalog).Select(RaumnutzungCsvTests.Abdruck).OrderBy(x => x));
        }

        [Fact]
        public void Eine_ausgelieferte_Zielkategorie_wird_benannt_abgelehnt()
        {
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCtrl.Kategorie muster = ctrl.Kategorien().First(k => k.Ausgeliefert);
            int vorher = ctrl.Profile(muster.Id).Count;
            RaumnutzungCtrl.CsvBilanz b = ctrl.CsvUebernehmen(ctrl.CsvPruefen(Beispiel(), muster.Id), muster.Id, null, true);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
            Assert.Equal(vorher, ctrl.Profile(muster.Id).Count);
        }

        [Fact]
        public void Ein_vorhandenes_Profil_wird_ersetzt_oder_uebersprungen()
        {
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCtrl.CsvBilanz erst = ctrl.CsvUebernehmen(ctrl.CsvPruefen(Beispiel(), null), null, "Ziel", false);
            Assert.True(erst.Ok, erst.Meldung);
            long ziel = erst.IdKatalog;
            Raumnutzungsprofil a = ctrl.Profile(ziel).Single(p => p.Bezeichner == "Beispielraum A");
            ctrl.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_IFC, "Csvprobe", a.Id);

            string neu = RaumnutzungCsvTests.KOPF + "\nB1;beispielraum a;18;12\n";
            RaumnutzungCsvLesung l = ctrl.CsvPruefen(Encoding.UTF8.GetBytes(neu), ziel);
            Assert.Equal(a.Id, l.Zeilen.Single().Vorhanden);

            RaumnutzungCtrl.CsvBilanz ueber = ctrl.CsvUebernehmen(l, ziel, null, ersetzen: false);
            Assert.True(ueber.Ok, ueber.Meldung);
            Assert.Equal((0, 0, 1), (ueber.Angelegt, ueber.Ersetzt, ueber.Uebersprungen));
            Assert.Equal(20.0, ctrl.ProfilLesen(a.Id).Heiz_Soll);

            RaumnutzungCtrl.CsvBilanz ersetzt = ctrl.CsvUebernehmen(l, ziel, null, ersetzen: true);
            Assert.True(ersetzt.Ok, ersetzt.Meldung);
            Assert.Equal(1, ersetzt.Ersetzt);
            Raumnutzungsprofil n = ctrl.ProfilLesen(a.Id);
            Assert.Equal(18.0, n.Heiz_Soll);
            Assert.Equal(12.0, n.Heiz_Soll_Ausserhalb);
            Assert.Null(n.Personen_Flaeche);   // ersetzt im Ganzen, nicht gemischt
            Assert.Equal(3, ctrl.Profile(ziel).Count);
            Assert.Equal(a.Id, ctrl.ZuordnungAufloesen(RaumnutzungSchema.ZUORDNUNG_IFC, "Csvprobe")?.Id);
        }

        [Fact]
        public void Eine_Nummer_die_das_Ziel_schon_traegt_lehnt_die_Zeile_benannt_ab()
        {
            var ctrl = new RaumnutzungCtrl();
            long ziel = ctrl.CsvUebernehmen(ctrl.CsvPruefen(Beispiel(), null), null, "Nummernziel", false).IdKatalog;
            RaumnutzungCsvLesung l = ctrl.CsvPruefen(Encoding.UTF8.GetBytes(RaumnutzungCsvTests.KOPF + "\nB2;Neuer Raum;20;\n"), ziel);
            Assert.False(l.Zeilen.Single().Uebernehmbar);
            Assert.Equal(RaumnutzungCsvArt.Fehler, l.AlleMeldungen.Single(m => m.Spalte == RaumnutzungCsv.SPALTE_NUMMER).Art);
            // Eine abgelehnte Zeile meldet keinen Wert als übernommen — auch nicht nach der Ablehnung durch das Ziel.
            Assert.DoesNotContain(l.AlleMeldungen, m => m.Art == RaumnutzungCsvArt.Uebernommen);
            RaumnutzungCtrl.CsvBilanz b = ctrl.CsvUebernehmen(l, ziel, null, true);
            Assert.False(b.Ok);
            Assert.Equal(3, ctrl.Profile(ziel).Count);
        }

        [Fact]
        public void Ein_doppelter_Kategoriename_wird_benannt_abgelehnt()
        {
            var ctrl = new RaumnutzungCtrl();
            string vorhanden = ctrl.Kategorien().First().Bezeichner;
            int vorher = ctrl.Kategorien().Count;
            RaumnutzungCtrl.CsvBilanz b = ctrl.CsvUebernehmen(ctrl.CsvPruefen(Beispiel(), null), null, vorhanden.ToUpperInvariant(), false);
            Assert.False(b.Ok);
            Assert.Equal(vorher, ctrl.Kategorien().Count);
        }
    }
}
