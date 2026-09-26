using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Datenseite der Auslegung</b> (<c>EPOS.Kern/Controller/ZapfprofilCtrl.Auslegung.cs</c>;
    /// Umsetzungskonzept Zapfprofilgenerator 3.3, 4.5, 4.7; Stufe Z2, Gruppe 2): Leser der
    /// Bedarfstage und der DIN-4708-Ausstattungen, die Nenninhalte aus Einstellung und
    /// Parametersatz, der Anlagenbestand als Vorschlag der Erzeugerart, der Konstruktor bei θ_KW,A
    /// des Parametersatzes und der Schreibweg des konstruierten Tags im Vorgang von
    /// <c>ZapfprofilCtrl.Speichern</c> — samt benannter Ablehnung ohne Schreiben.
    ///
    /// <para>Gearbeitet wird auf einer ARBEITSKOPIE der Testdatenbank (Projekt 1006, kein Projekt
    /// der Referenzbasis) bzw. auf einer leeren Tww-Datenbank; die Auslegungsparameter legt
    /// <see cref="AuslegungTestbau.ParameterEinspielen"/> auf der Kopie an. Alle Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilAuslegungCtrlTests
    {
        private const int PROJEKT = 1006;
        private const string VERSION = "TEST-1";

        // =================================================================================
        // Leser
        // =================================================================================

        [Fact]
        public void Die_Bedarfstage_kommen_samt_Ereignissen_in_ihrer_Reihenfolge()
        {
            using var db = new TwwTestdatenbank();
            int b = TagAnlegen("Tag B (fiktiv)", 2, 20.0, (600, 30, 3.0, 2), (420, 10, 1.0, 1));
            int a = TagAnlegen("Tag A (fiktiv)", 4, null, (720, 5, 0.5, 1));

            IReadOnlyList<BedarfstagKatalogzeile> tage = ZapfprofilCtrl.Bedarfstage();

            Assert.Equal(new[] { "Tag A (fiktiv)", "Tag B (fiktiv)" }, tage.Select(t => t.Bezeichner).ToArray());
            BedarfstagKatalogzeile tb = tage[1];
            Assert.Equal(b, tb.Id);
            Assert.Equal(ZapfBedarfstagquelle.A100Referenz, tb.QuelleArt);
            Assert.Equal(20.0, tb.Bezugsmenge);
            Assert.Equal(new[] { 420, 600 }, tb.Ereignisse.Select(e => e.MinuteBeginn).ToArray());   // nach Reihenfolge
            Assert.Equal(ZapfKatalogstatus.Eigen, tb.Status);
            Assert.Equal(Herkunftsart.Fiktiv, tb.Herkunft.Art);
            Assert.Null(tage[0].Bezugsmenge);
            Assert.Equal(a, tage[0].Id);
            Assert.Single(tage[0].Ereignisse);
        }

        [Fact]
        public void Ohne_Tabellen_gibt_es_keine_Bedarfstage_und_keine_Ausstattung()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            Assert.Empty(ZapfprofilCtrl.Bedarfstage());
            Assert.Empty(ZapfprofilCtrl.Ausstattungen(VERSION));
            Assert.Null(ZapfprofilCtrl.Din4708Katalog(VERSION).BelegungJeRaumzahl);
        }

        [Fact]
        public void Der_DIN_4708_Katalog_liest_Belegung_und_Ausstattung_nur_der_Katalogversion()
        {
            using var db = new TwwTestdatenbank();
            WertAnlegen("BELEGUNG", "3", 1.5, VERSION);
            WertAnlegen("AUSSTATTUNG", "Klasse X (fiktiv)", 7000.0, VERSION);
            WertAnlegen("AUSSTATTUNG", "Klasse Y (fiktiv)", 8000.0, "ANDERE-1");

            Din4708Katalog k = ZapfprofilCtrl.Din4708Katalog(VERSION);
            Assert.Equal(1.5, k.BelegungJeRaumzahl["3"]);
            Din4708Ausstattung x = Assert.Single(k.Ausstattungen);
            Assert.Equal("Klasse X (fiktiv)", x.Schluessel);
            Assert.Equal(7000.0, x.WertWh);
            Assert.True(x.Id > 0);
        }

        // =================================================================================
        // Nenninhalte
        // =================================================================================

        [Fact]
        public void Die_Nenninhalte_kommen_aus_der_Einstellung_sonst_aus_dem_Parametersatz()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                var einstellungen = new FluechtigeEinstellungen();
                Dienste.Einstellungen = einstellungen;
                Parametersatz ps = MitListe();

                // Ohne Einstellung: die Vorgabe des Parametersatzes.
                Nenninhaltswahl w = ZapfprofilCtrl.Nenninhalte(ps);
                Assert.Equal(Nenninhaltsquelle.Parameter, w.Quelle);
                Assert.Equal(AuslegungTestbau.NENNINHALTE, w.Liste.WerteL.ToArray());
                Assert.Null(w.Hinweis);

                // Die Einstellung geht vor — Semikolon, Leerraum, Dezimalkomma.
                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_NENNINHALTE, "150; 300,5  700");
                w = ZapfprofilCtrl.Nenninhalte(ps);
                Assert.Equal(Nenninhaltsquelle.Einstellung, w.Quelle);
                Assert.Equal(new[] { 150.0, 300.5, 700.0 }, w.Liste.WerteL.ToArray());

                // Eine ungültige Einstellung wird benannt verworfen, die Vorgabe gilt.
                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_NENNINHALTE, "300; 200");
                w = ZapfprofilCtrl.Nenninhalte(ps);
                Assert.Equal(Nenninhaltsquelle.Parameter, w.Quelle);
                Assert.Equal(ZapfprofilCtrl.HINWEIS_NENNINHALTE_EINSTELLUNG, w.Hinweis.Code);
                Assert.Contains("Zapfprofil.Nenninhalte", w.Hinweis.Text);
                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_NENNINHALTE, "100; groß");
                Assert.Contains("„groß“ ist keine Zahl", ZapfprofilCtrl.Nenninhalte(ps).Hinweis.Text);

                // Ohne beides: keine Liste, kein Hinweis (die Auslegung sagt es selbst).
                einstellungen.Loesche(ZapfprofilCtrl.EINSTELLUNG_NENNINHALTE);
                w = ZapfprofilCtrl.Nenninhalte(AuslegungTestbau.Auslegungssatz());
                Assert.Equal(Nenninhaltsquelle.Keine, w.Quelle);
                Assert.Null(w.Liste);
                Assert.Null(w.Hinweis);
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }

        [Fact]
        public void Eine_Nenninhaltsliste_des_Parametersatzes_ohne_ganze_Stelle_wird_benannt_abgelehnt()
        {
            Parametersatz ps = AuslegungTestbau.Auslegungssatz(new Dictionary<string, double>
            {
                [ZapfAuslegungParameter.NENNINHALT_LISTE + "eins"] = 100.0
            });
            var ex = Assert.Throws<ZapfAuslegungException>(() => Nenninhaltsliste.AusParametern(ps));
            Assert.Equal(ZapfAuslegungsfehler.GroesseUngueltig, ex.Fehler);
            Assert.Null(Nenninhaltsliste.AusParametern(null));
        }

        // =================================================================================
        // Anlagenbestand
        // =================================================================================

        [Fact]
        public void Der_Anlagenbestand_schlaegt_die_Erzeugerart_nur_eindeutig_vor()
        {
            using var db = new TwwTestdatenbank();
            Assert.Null(ZapfprofilCtrl.Erzeugerbestand(1).Vorschlag);   // ohne Tabellen: 0 und 0

            DataRepository.ExecuteNonQuery("CREATE TABLE \"Tab_WP\" (\"ID\" INTEGER PRIMARY KEY, \"ID_Projekt\" INTEGER)");
            DataRepository.ExecuteNonQuery("CREATE TABLE \"Tab_Heizkessel\" (\"ID\" INTEGER PRIMARY KEY, \"ID_Projekt\" INTEGER)");
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_WP\" (\"ID_Projekt\") VALUES (1), (1)");
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Heizkessel\" (\"ID_Projekt\") VALUES (2)");

            Assert.Equal(new ZapfErzeugerbestand(2, 0), ZapfprofilCtrl.Erzeugerbestand(1));
            Assert.Equal(ZapfErzeugerart.Waermepumpe, ZapfprofilCtrl.Erzeugerbestand(1).Vorschlag);
            Assert.Equal(ZapfErzeugerart.Kessel, ZapfprofilCtrl.Erzeugerbestand(2).Vorschlag);

            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Heizkessel\" (\"ID_Projekt\") VALUES (1)");
            ZapfErzeugerbestand beide = ZapfprofilCtrl.Erzeugerbestand(1);
            Assert.True(beide.Mehrdeutig);
            Assert.Null(beide.Vorschlag);                                // die Wahl bleibt beim Anwender
        }

        // =================================================================================
        // Konstruktor
        // =================================================================================

        [Fact]
        public void Der_Konstruktor_legt_den_Tag_beim_Kaltwasser_des_Parametersatzes_als_Entwurf_ab()
        {
            Parametersatz ps = AuslegungTestbau.Auslegungssatz();
            Zapfregel regel = Assert.Single(Zapfregel.AusParametern(AuslegungTestbau.Auslegungssatz(new Dictionary<string, double>
            {
                ["Konstruktor.Regel.Probebrause.Volumenstrom"] = 8.0,
                ["Konstruktor.Regel.Probebrause.Dauer"] = 5.0,
                ["Konstruktor.Regel.Probebrause.Temperatur"] = 42.0
            })));
            var zeilen = new[]
            {
                Konstruktorzeile.AusVorgaengen(420, 450, 3.0, regel),              // 3 · 8 l/min · 5 min = 120 l bei 42 °C
                new Konstruktorzeile(1080, 1140, 60.0, 52.0, "Küche")
            };

            BedarfstagKatalogzeile t = ZapfprofilCtrl.BedarfstagKonstruieren(zeilen, "  Eigener Tag (fiktiv) ", ps);

            Assert.Equal(ZapfprofilCtrl.ENTWURF_ID, t.Id);
            Assert.Equal("Eigener Tag (fiktiv)", t.Bezeichner);
            Assert.Equal(ps.Katalogversion, t.Katalogversion);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, t.QuelleArt);
            Assert.Null(t.Bezugsmenge);
            Assert.Equal(ZapfKatalogstatus.Eigen, t.Status);
            Assert.False(t.ReadOnly);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, t.Herkunft.Art);
            Assert.Equal(TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION, t.Herkunft.Quelle);

            // θ_KW,A des PARAMETERSATZES (12 °C), nicht eines Projekts: E = V · c_w · (θ − 12) / 1000.
            double cw = Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K;
            Assert.Equal(2, t.Ereignisse.Count);
            Assert.Equal(420, t.Ereignisse[0].MinuteBeginn);
            Assert.Equal(30, t.Ereignisse[0].DauerMin);
            Assert.Equal(120.0 * cw * (42.0 - 12.0) / 1000.0, t.Ereignisse[0].EnergieKwh, 12);
            Assert.Equal(60.0 * cw * (52.0 - 12.0) / 1000.0, t.Ereignisse[1].EnergieKwh, 12);

            // Ohne Namen oder mit leerem Fenster: benannt abgelehnt.
            Assert.Equal(ZapfAuslegungsfehler.BedarfstagUngueltig,
                Assert.Throws<ZapfAuslegungException>(() => ZapfprofilCtrl.BedarfstagKonstruieren(zeilen, " ", ps)).Fehler);
            Assert.Equal(ZapfAuslegungsfehler.BedarfstagUngueltig,
                Assert.Throws<ZapfAuslegungException>(() => ZapfprofilCtrl.BedarfstagKonstruieren(
                    new[] { new Konstruktorzeile(600, 600, 10.0, 45.0) }, "Leer", ps)).Fehler);
        }

        // =================================================================================
        // Schreibweg des Entwurfs
        // =================================================================================

        [Fact]
        public void Speichern_legt_den_Entwurf_als_eigene_Katalogzeile_an_und_haengt_das_Projekt_daran()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            int vorher = ZapfprofilCtrl.Bedarfstage().Count;

            BedarfstagKatalogzeile entwurf = ZapfprofilCtrl.BedarfstagKonstruieren(
                new[] { new Konstruktorzeile(420, 440, 80.0, 45.0), new Konstruktorzeile(1100, 1160, 150.0, 45.0) },
                ZapfprofilCtrl.FreierBedarfstagname("Eigener Tag (fiktiv)", ps.Katalogversion), ps);
            var stand = new ZapfprofilStand(BrauchwasserWeg.Bestand, new ZonenStand[0], null) { BedarfstagEntwurf = entwurf };

            ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(PROJEKT, stand);

            Assert.Null(geschrieben.BedarfstagEntwurf);
            Assert.NotNull(geschrieben.Projekt.IdBedarfstag);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, geschrieben.Projekt.BedarfstagQuelle);
            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(geschrieben.Projekt.IdBedarfstag, gelesen.Projekt.IdBedarfstag);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, gelesen.Projekt.BedarfstagQuelle);
            Assert.Equal(1, gelesen.Projekt.Seed);                        // die übrigen Größen: Vorgaben der DDL

            IReadOnlyList<BedarfstagKatalogzeile> tage = ZapfprofilCtrl.Bedarfstage();
            Assert.Equal(vorher + 1, tage.Count);
            BedarfstagKatalogzeile t = Assert.Single(tage, x => x.Id == gelesen.Projekt.IdBedarfstag.Value);
            Assert.Equal("Eigener Tag (fiktiv)", t.Bezeichner);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, t.QuelleArt);
            Assert.Equal(ZapfKatalogstatus.Eigen, t.Status);
            Assert.False(t.ReadOnly);
            Assert.Null(t.Bezugsmenge);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, t.Herkunft.Art);
            Assert.Equal(entwurf.Ereignisse.Select(e => (e.MinuteBeginn, e.DauerMin, e.EnergieKwh)),
                         t.Ereignisse.Select(e => (e.MinuteBeginn, e.DauerMin, e.EnergieKwh)));

            // Ein zweites Speichern des gelesenen Stands legt keinen Tag an.
            ZapfprofilCtrl.Speichern(PROJEKT, gelesen);
            Assert.Equal(vorher + 1, ZapfprofilCtrl.Bedarfstage().Count);

            // Derselbe Name ist jetzt belegt: der Vorschlag weicht aus.
            Assert.Equal("Eigener Tag (fiktiv) (2)", ZapfprofilCtrl.FreierBedarfstagname("Eigener Tag (fiktiv)", ps.Katalogversion));
        }

        [Fact]
        public void Ein_belegter_Name_lehnt_benannt_ab_und_schreibt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            BedarfstagKatalogzeile vorhanden = ZapfprofilCtrl.Bedarfstage().First(t => t.Katalogversion == ps.Katalogversion);
            int vorher = ZapfprofilCtrl.Bedarfstage().Count;

            BedarfstagKatalogzeile entwurf = ZapfprofilCtrl.BedarfstagKonstruieren(
                new[] { new Konstruktorzeile(420, 440, 80.0, 45.0) }, vorhanden.Bezeichner, ps);
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null) { BedarfstagEntwurf = entwurf };

            var ex = Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(PROJEKT, stand));
            Assert.Equal(ZapfSpeicherfehler.BedarfstagNameBelegt, ex.Fehler);
            Assert.Contains(vorhanden.Bezeichner, ex.Message);
            Assert.Equal(vorher, ZapfprofilCtrl.Bedarfstage().Count);
            Assert.Null(ZapfprofilCtrl.Lies(PROJEKT).Projekt);
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));

            // Ein Entwurf ohne Ereignis ist ungültig.
            var leer = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null)
            {
                BedarfstagEntwurf = entwurf with { Bezeichner = "Neuer Tag (fiktiv)", Ereignisse = new Zapfereignis[0] }
            };
            Assert.Equal(ZapfSpeicherfehler.BedarfstagUngueltig,
                Assert.Throws<ZapfprofilSpeicherException>(() => ZapfprofilCtrl.Speichern(PROJEKT, leer)).Fehler);
        }

        // =================================================================================
        // Die Auslegung eines Arbeitsstands
        // =================================================================================

        [Fact]
        public void Die_Auslegung_rechnet_den_Entwurf_als_gewaehlten_Tag_mit_den_Laufangaben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            Assert.True(ZapfprofilCtrl.KalenderLesen(PROJEKT, out int jan1, out bool[] we));
            Assert.Equal(365, we.Length);
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            var zone = new ZonenStand { Name = "Zone Probe", IdNutzungsart = Nutzungsart("Testnutzung A (fiktiv)"), Bezugsmenge = 8.0 };
            var ohne = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, null);

            // Ohne Tag: die Vorgaberegel öffnet den Konstruktor, benannt - kein stilles Stundenprofil.
            Auslegungsrechnung r0 = ZapfprofilCtrl.Auslegung(PROJEKT, ohne, jan1, we, new Auslegungslauf(null, null));
            Auslegungsgruppe g0 = Assert.Single(r0.Ergebnis.Gruppen);
            Assert.Equal(ZapfTopologie.Speicher, g0.Topologie);
            Assert.False(g0.Empfehlung.Rechenbar);
            Assert.Contains(g0.Hinweise, h => h.Code == "KONSTRUKTOR_OEFFNEN");
            Assert.Equal(Nenninhaltsquelle.Parameter, r0.Nenninhalte.Quelle);
            Assert.Equal(r0.Bestand.Vorschlag, r0.Erzeugerart);

            // Mit Entwurf und Werkstoff: der Punkt der Summenlinie samt Nenninhalt der Vorgabeliste.
            BedarfstagKatalogzeile entwurf = ZapfprofilCtrl.BedarfstagKonstruieren(
                new[] { new Konstruktorzeile(420, 450, 120.0, 45.0), new Konstruktorzeile(1110, 1170, 200.0, 45.0) },
                "Probetag (fiktiv)", ps);
            ZapfprofilStand mit = ohne with { BedarfstagEntwurf = entwurf };
            Auslegungsrechnung r = ZapfprofilCtrl.Auslegung(PROJEKT, mit, jan1, we,
                new Auslegungslauf(ZapfErzeugerart.Waermepumpe, ZapfUebertragerwerkstoff.Stahl));
            Auslegungsgruppe g = Assert.Single(r.Ergebnis.Gruppen);
            Assert.Equal(ZapfBedarfstagquelle.Konstruktor, g.Bedarfstagwahl.Quelle);
            Assert.Equal("Probetag (fiktiv)", g.Bedarfstag.Bezeichner);
            Assert.True(g.Empfehlung.Rechenbar, g.Empfehlung.GrundText);
            Assert.True(g.Empfehlung.VolumenL > 0);
            double nenn = g.Empfehlung.NenninhaltL.Value;
            // Die Vorgabeliste der Testdatenbank: die der Vorlage V4 aus dem freien Paketteil (N27).
            Assert.True(AuslegungTestbau.NenninhalteDerDatenbank().Contains(nenn), "Nenninhalt " + nenn + " l steht nicht in der Vorgabeliste.");
            Assert.True(nenn >= g.Empfehlung.VolumenL);
            Assert.NotNull(g.Speicherauslegung);
            Assert.Equal(ZapfErzeugerart.Waermepumpe, r.Erzeugerart);

            // Ohne Werkstoff und ohne Übertrager im Projekt: die Summenlinie lehnt benannt ab.
            Auslegungsrechnung ohneWerkstoff = ZapfprofilCtrl.Auslegung(PROJEKT, mit, jan1, we,
                new Auslegungslauf(ZapfErzeugerart.Waermepumpe, null));
            Auslegungsgruppe gw = Assert.Single(ohneWerkstoff.Ergebnis.Gruppen);
            Assert.False(gw.Empfehlung.Rechenbar);
            Assert.Contains("Werkstoff", gw.Empfehlung.GrundText);

            // Schritt 124 (N10 (i)): Ohne Laufangabe gilt die gespeicherte Wahl des Projekts — der
            // Werkstoff aus der Projektzeile macht die Summenlinie rechenbar, die Erzeugerart der
            // Projektzeile geht dem Vorschlag des Anlagenbestands vor.
            ZapfprofilStand gewaehlt = mit with
            {
                Projekt = ZapfprofilCtrl.ProjektVorgabe() with
                {
                    Erzeugerart = ZapfErzeugerart.Kessel, UebertragerWerkstoff = ZapfUebertragerwerkstoff.Stahl
                }
            };
            Auslegungsrechnung ausProjekt = ZapfprofilCtrl.Auslegung(PROJEKT, gewaehlt, jan1, we, new Auslegungslauf(null, null));
            Assert.Equal(ZapfErzeugerart.Kessel, ausProjekt.Erzeugerart);
            Assert.True(Assert.Single(ausProjekt.Ergebnis.Gruppen).Empfehlung.Rechenbar);
            Assert.Equal(ZapfErzeugerart.Waermepumpe,
                ZapfprofilCtrl.Auslegung(PROJEKT, gewaehlt, jan1, we, new Auslegungslauf(ZapfErzeugerart.Waermepumpe, null)).Erzeugerart);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Der Auslegungssatz samt Vorgabe der Nenninhaltsliste.</summary>
        private static Parametersatz MitListe()
        {
            var liste = new Dictionary<string, double>();
            for (int k = 0; k < AuslegungTestbau.NENNINHALTE.Length; k++)
                liste[ZapfAuslegungParameter.NENNINHALT_LISTE + (k + 1)] = AuslegungTestbau.NENNINHALTE[k];
            return AuslegungTestbau.Auslegungssatz(liste);
        }

        private static int TagAnlegen(string bezeichner, int quelleArt, double? bezugsmenge,
                                      params (int Beginn, int Dauer, double Energie, int Reihenfolge)[] ereignisse)
        {
            int id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO \"Tab_TwwBedarfstag_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Quelle_Art\", \"Bezugsmenge\", " +
                "\"Quelle\", \"Ausgabe\", \"Version\", \"Herkunftsart\", \"Status\", \"Beleg\", \"ReadOnly\") " +
                "VALUES (?, ?, ?, ?, ?, NULL, ?, 'FIKTIV', 'EIGEN', NULL, 0)",
                new[]
                {
                    new DbParam("@b", bezeichner), new DbParam("@k", VERSION), new DbParam("@a", quelleArt),
                    new DbParam("@m", bezugsmenge), new DbParam("@q", TwwTestdatenbank.QUELLE), new DbParam("@v", VERSION)
                });
            foreach (var e in ereignisse)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"Tab_TwwBedarfstagEreignis_STAMM\" (\"ID_Bedarfstag\", \"Minute_Beginn\", \"Dauer_min\", " +
                    "\"Energie_Kwh\", \"Reihenfolge\") VALUES (?, ?, ?, ?, ?)",
                    new DbParam("@t", id), new DbParam("@b", e.Beginn), new DbParam("@d", e.Dauer),
                    new DbParam("@e", e.Energie), new DbParam("@r", e.Reihenfolge));
            return id;
        }

        private static void WertAnlegen(string art, string schluessel, double wert, string version)
            => DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_TwwDin4708Wert_STAMM\" (\"Art\", \"Schluessel\", \"Wert\", \"Katalogversion\", \"Quelle\", " +
                "\"Ausgabe\", \"Version\", \"Herkunftsart\", \"Status\", \"Beleg\", \"ReadOnly\") " +
                "VALUES (?, ?, ?, ?, ?, NULL, ?, 'FIKTIV', 'EIGEN', NULL, 0)",
                new DbParam("@a", art), new DbParam("@s", schluessel), new DbParam("@w", wert), new DbParam("@k", version),
                new DbParam("@q", TwwTestdatenbank.QUELLE), new DbParam("@v", version));

        private static int Nutzungsart(string name)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", name), new DbParam("@k", VERSION)));
    }
}
