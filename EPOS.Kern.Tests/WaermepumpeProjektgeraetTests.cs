using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Stammfelder der Wärmepumpen-ANLAGE gehören dem Projekt</b>
    /// (Anwenderentscheid 16.09.2026) — die drei Schreibwege, die daraus folgen:
    /// <see cref="WPCtrl.ProjektgeraetSchreiben"/> (Projektkopie),
    /// <see cref="WPStammCtrl.UebernehmenAusProjekt"/> (Übernahme in den Katalog auf
    /// Zuruf) und <see cref="KenndatenCtrl.AbgleichenProjekt"/> (die Kennlinien des
    /// Projekts statt der des Katalogs).
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Der Anlagendialog schrieb die Felder
    /// Hersteller, Beschreibung, Typ, Regelung, Aufstellung, Baujahr, Nennleistung und
    /// Heizstableistung in den KATALOG — also in jedes andere Projekt mit, das dasselbe
    /// Gerät führt. Dass die Änderung jetzt bei EINEM Projekt bleibt, ist nicht am
    /// Rechenergebnis abzulesen: Der Referenzlauf rechnet einen bestehenden Stand nach,
    /// er speichert keinen Dialog. Geprüft wird deshalb hier, und zwar an einem Gerät,
    /// das in der Testdatenbank in DREI Projekten unter demselben Bezeichner steht.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> und braucht deshalb ihre eigene Arbeitskopie;
    /// <see cref="TestDatenbank"/> als <c>IClassFixture</c> legt je Testklasse eine an.
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> selbst bleibt unberührt. Fehlt die
    /// Datei, schweigen die Fälle. Jeder Fall merkt sich, was er vergleichen will, SELBST
    /// — die Fälle teilen sich eine Kopie und laufen in keiner zugesicherten
    /// Reihenfolge.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeProjektgeraetTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public WaermepumpeProjektgeraetTests(TestDatenbank db) { _db = db; }

        // --- Das GESCHUETZTE Gerät: Katalogsatz 20 trägt ReadOnly = 1, und drei
        //     Projekte (1006, 1009, 1032) führen eine Kopie gleichen Bezeichners.
        private const string NAME_GESCHUETZT = "T 800-2";
        private const int STAMM_GESCHUETZT = 20;
        private const int WP_1006 = 1006020;
        private const int PROJEKT_1006 = 1006;
        private const int WP_1009 = 1009020;
        private const int PROJEKT_1009 = 1009;

        // --- Das FREIE Gerät: Katalogsatz 61 ohne Schreibschutz, Kopien in vier
        //     Projekten (1008, 1029, 1042, 1043), 27 Stützstellen hüben wie drüben.
        private const string NAME_FREI = "CS7800iLW 16";
        private const int STAMM_FREI = 61;
        private const int WP_1008 = 1008061;
        private const int PROJEKT_1008 = 1008;

        // --- Eine Id, die es in KEINER der beiden Tabellen gibt (Tab_WP endet bei
        //     1672045, Tab_WP_STAMM bei 72).
        private const int ID_UNBEKANNT = 987654321;

        // =================================================================================
        // 1 - ProjektgeraetSchreiben: die Kopie des einen Projekts
        // =================================================================================

        /// <summary>
        /// Geschrieben wird GENAU die Kopie des genannten Projekts — das gleichnamige
        /// Gerät eines zweiten Projekts bleibt Zeichen für Zeichen stehen.
        /// </summary>
        /// <remarks>
        /// Das ist der Kern des Entscheids und zugleich die Lehre aus der entfallenen
        /// <c>WPCtrl.Update()</c>: Sie filterte <c>WHERE Bezeichner = ?</c> ohne Projekt
        /// und hätte beide Zeilen zugleich überschrieben.
        /// </remarks>
        [Fact]
        public void ProjektgeraetSchreiben_aendert_nur_die_Kopie_des_einen_Projekts()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataRow fremdVorher = Projektsatz(WP_1009);
            string fremdFirma = Text(fremdVorher, "Firma");
            int fremdNenn = Zahl(fremdVorher, "Nennleistung");

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Firma: "Prüfhersteller", Nennleistung: 99));

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(NAME_GESCHUETZT, e.Name);

            DataRow eigen = Projektsatz(WP_1006);
            Assert.Equal("Prüfhersteller", Text(eigen, "Firma"));
            Assert.Equal(99, Zahl(eigen, "Nennleistung"));

            DataRow fremd = Projektsatz(WP_1009);
            Assert.Equal(fremdFirma, Text(fremd, "Firma"));
            Assert.Equal(fremdNenn, Zahl(fremd, "Nennleistung"));
            Assert.NotEqual("Prüfhersteller", Text(fremd, "Firma"));
        }

        /// <summary>
        /// Der KATALOGSATZ bleibt unberührt — das ist der Unterschied zum bisherigen
        /// Verhalten des Anlagendialogs.
        /// </summary>
        [Fact]
        public void ProjektgeraetSchreiben_laesst_den_Katalogsatz_stehen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataRow vorher = Katalogsatz(STAMM_FREI);
            string firmaVorher = Text(vorher, "Firma");
            int nennVorher = Zahl(vorher, "Nennleistung");

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1008, PROJEKT_1008,
                new WPCtrl.ProjektgeraetFelder(Firma: "Nur im Projekt", Nennleistung: 7));
            Assert.True(e.Ok, e.Meldung);

            DataRow nachher = Katalogsatz(STAMM_FREI);
            Assert.Equal(firmaVorher, Text(nachher, "Firma"));
            Assert.Equal(nennVorher, Zahl(nachher, "Nennleistung"));
            Assert.Equal("Nur im Projekt", Text(Projektsatz(WP_1008), "Firma"));
        }

        /// <summary>Was nicht mitkommt (<c>null</c>), bleibt stehen.</summary>
        [Fact]
        public void ProjektgeraetSchreiben_laesst_ausgelassene_Felder_stehen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataRow vorher = Projektsatz(WP_1009);
            string typ = Text(vorher, "Typ");
            string regelung = Text(vorher, "Regelung");
            string aufstellung = Text(vorher, "Aufstellung");
            int baujahr = Zahl(vorher, "Baujahr");
            int nenn = Zahl(vorher, "Nennleistung");
            int heizung = Zahl(vorher, "Heizung");

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1009, PROJEKT_1009,
                new WPCtrl.ProjektgeraetFelder(Beschreibung: "Nur die Beschreibung"));
            Assert.True(e.Ok, e.Meldung);

            DataRow nachher = Projektsatz(WP_1009);
            Assert.Equal("Nur die Beschreibung", Text(nachher, "Beschreibung"));
            Assert.Equal(typ, Text(nachher, "Typ"));
            Assert.Equal(regelung, Text(nachher, "Regelung"));
            Assert.Equal(aufstellung, Text(nachher, "Aufstellung"));
            Assert.Equal(baujahr, Zahl(nachher, "Baujahr"));
            Assert.Equal(nenn, Zahl(nachher, "Nennleistung"));
            Assert.Equal(heizung, Zahl(nachher, "Heizung"));
        }

        /// <summary>
        /// Die Heizstableistung landet in <c>Tab_WP.Heizung</c> — bis zu diesem Auftrag
        /// schrieb sie KEIN Weg in irgendeine Tabelle.
        /// </summary>
        [Fact]
        public void ProjektgeraetSchreiben_schreibt_die_Heizstableistung()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Heizung: 13));
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(13, Zahl(Projektsatz(WP_1006), "Heizung"));
        }

        /// <summary>
        /// <b>Die KÜHLLEISTUNG geht denselben Weg</b> (Anwenderentscheid 16.09.2026) —
        /// sie steht seither im Stammfeldblock des Anlagendialogs und ist dort
        /// bearbeitbar wie die Nennleistung daneben.
        /// </summary>
        /// <remarks>
        /// <b>Und sie wird UNVERKÜRZT geschrieben.</b> <c>Tab_WP.Kuehlleistung</c> ist
        /// eine <c>REAL</c>-Spalte; ein <c>int?</c> im Feldsatz hätte 5,5 kW still auf
        /// 5 kW abgeschnitten. Der Fall prüft genau diese Nachkommastelle.
        /// </remarks>
        [Fact]
        public void ProjektgeraetSchreiben_schreibt_die_Kuehlleistung_als_Kommazahl()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Kuehlleistung: 5.5));
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(5.5, Kommazahl(Projektsatz(WP_1006), "Kuehlleistung"), 3);

            // Ausgelassen heisst unveraendert - nicht 0.
            var zweite = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Firma: "Ohne Kuehlangabe"));
            Assert.True(zweite.Ok, zweite.Meldung);
            Assert.Equal(5.5, Kommazahl(Projektsatz(WP_1006), "Kuehlleistung"), 3);
        }

        /// <summary>Eine negative Kühlleistung wird benannt abgelehnt, wie jede andere.</summary>
        [Fact]
        public void ProjektgeraetSchreiben_lehnt_eine_negative_Kuehlleistung_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            double vorher = Kommazahl(Projektsatz(WP_1009), "Kuehlleistung");

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1009, PROJEKT_1009,
                new WPCtrl.ProjektgeraetFelder(Kuehlleistung: -1.0));

            Assert.False(e.Ok);
            Assert.NotEqual("", e.Meldung);
            Assert.Equal(vorher, Kommazahl(Projektsatz(WP_1009), "Kuehlleistung"), 3);
        }

        /// <summary>
        /// Eine KATALOG-Id wird benannt abgelehnt: Die Anlagenzeile trägt sie, solange
        /// die Anlage noch nicht gespeichert ist (zweistufige Suche in
        /// <c>WaermepumpeGeraeteCtrl</c>).
        /// </summary>
        [Fact]
        public void ProjektgeraetSchreiben_lehnt_eine_Katalog_Id_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var e = WPCtrl.ProjektgeraetSchreiben(STAMM_GESCHUETZT, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Firma: "Darf nicht ankommen"));

            Assert.False(e.Ok);
            Assert.Contains("noch nicht gespeichert", e.Meldung);
        }

        /// <summary>Eine Id, die es nirgends gibt, bekommt die andere Meldung.</summary>
        [Fact]
        public void ProjektgeraetSchreiben_lehnt_eine_unbekannte_Id_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var e = WPCtrl.ProjektgeraetSchreiben(ID_UNBEKANNT, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Firma: "Darf nicht ankommen"));

            Assert.False(e.Ok);
            Assert.False(string.IsNullOrWhiteSpace(e.Meldung));
            Assert.DoesNotContain("noch nicht gespeichert", e.Meldung);
        }

        /// <summary>
        /// Ein FREMDES Projekt zur richtigen Geräte-Id trifft nichts — der Projektfilter
        /// ist keine Verzierung.
        /// </summary>
        [Fact]
        public void ProjektgeraetSchreiben_lehnt_ein_fremdes_Projekt_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            string firmaVorher = Text(Projektsatz(WP_1006), "Firma");

            var e = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1009,
                new WPCtrl.ProjektgeraetFelder(Firma: "Darf nicht ankommen"));

            Assert.False(e.Ok);
            Assert.Equal(firmaVorher, Text(Projektsatz(WP_1006), "Firma"));
        }

        /// <summary>Negative Leistungen werden benannt abgelehnt, und es wird nichts geschrieben.</summary>
        [Fact]
        public void ProjektgeraetSchreiben_lehnt_negative_Leistungen_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            string beschreibungVorher = Text(Projektsatz(WP_1006), "Beschreibung");

            var nenn = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Beschreibung: "darf nicht ankommen", Nennleistung: -1));
            Assert.False(nenn.Ok);
            Assert.Contains("Nennleistung", nenn.Meldung);

            var heiz = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Beschreibung: "darf nicht ankommen", Heizung: -5));
            Assert.False(heiz.Ok);
            Assert.Contains("Heizstab", heiz.Meldung);

            Assert.Equal(beschreibungVorher, Text(Projektsatz(WP_1006), "Beschreibung"));
        }

        /// <summary>Ein Baujahr ausserhalb des Rahmens wird benannt abgelehnt.</summary>
        [Fact]
        public void ProjektgeraetSchreiben_lehnt_ein_unmoegliches_Baujahr_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var zuFrueh = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Baujahr: WPCtrl.BAUJAHR_KLEINSTES - 1));
            Assert.False(zuFrueh.Ok);
            Assert.Contains("Baujahr", zuFrueh.Meldung);

            var zuSpaet = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Baujahr: WPCtrl.BaujahrGroesstes + 1));
            Assert.False(zuSpaet.Ok);
            Assert.Contains("Baujahr", zuSpaet.Meldung);

            // Der Rand selbst ist zulaessig.
            var rand = WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Baujahr: WPCtrl.BaujahrGroesstes));
            Assert.True(rand.Ok, rand.Meldung);
            Assert.Equal(WPCtrl.BaujahrGroesstes, Zahl(Projektsatz(WP_1006), "Baujahr"));
        }

        // =================================================================================
        // 2 - UebernahmeVorschau
        // =================================================================================

        /// <summary>
        /// Die Vorschau nennt den Katalogsatz, seinen Schreibschutz und die Zahl der
        /// ANDEREN Projekte mit eigener Kopie — das eigene zählt nicht mit.
        /// </summary>
        [Fact]
        public void UebernahmeVorschau_zaehlt_die_anderen_Projekte()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            int andereGeschuetzt = AndereProjekte(NAME_GESCHUETZT, PROJEKT_1006);
            var g = WPStammCtrl.UebernahmeVorschau(WP_1006, PROJEKT_1006);
            Assert.Equal(NAME_GESCHUETZT, g.Bezeichner);
            Assert.True(g.KatalogsatzVorhanden);
            Assert.True(g.ReadOnly);
            Assert.Equal(andereGeschuetzt, g.AnzahlProjekteMitKopie);
            Assert.True(g.AnzahlProjekteMitKopie >= 2);

            int andereFrei = AndereProjekte(NAME_FREI, PROJEKT_1008);
            var f = WPStammCtrl.UebernahmeVorschau(WP_1008, PROJEKT_1008);
            Assert.Equal(NAME_FREI, f.Bezeichner);
            Assert.True(f.KatalogsatzVorhanden);
            Assert.False(f.ReadOnly);
            Assert.Equal(andereFrei, f.AnzahlProjekteMitKopie);
        }

        /// <summary>Zu einer unbekannten Id gibt es nichts zu warnen.</summary>
        [Fact]
        public void UebernahmeVorschau_schweigt_zu_einer_unbekannten_Id()
        {
            if (!_db.Vorhanden) return;

            var v = WPStammCtrl.UebernahmeVorschau(ID_UNBEKANNT, PROJEKT_1006);
            Assert.Equal("", v.Bezeichner);
            Assert.False(v.KatalogsatzVorhanden);
            Assert.False(v.ReadOnly);
            Assert.Equal(0, v.AnzahlProjekteMitKopie);
        }

        // =================================================================================
        // 3 - UebernehmenAusProjekt
        // =================================================================================

        /// <summary>Ein freier Katalogsatz bekommt den Stand der Projektkopie.</summary>
        [Fact]
        public void UebernehmenAusProjekt_ueberschreibt_einen_freien_Katalogsatz()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Assert.True(WPCtrl.ProjektgeraetSchreiben(WP_1008, PROJEKT_1008,
                new WPCtrl.ProjektgeraetFelder(Firma: "Prüfwerk Übernahme",
                                               Beschreibung: "aus dem Projekt",
                                               Nennleistung: 42)).Ok);

            var e = WPStammCtrl.UebernehmenAusProjekt(WP_1008, PROJEKT_1008, false);

            Assert.True(e.Ok, e.Meldung);
            Assert.Contains("überschrieben", e.Meldung);
            Assert.Contains(NAME_FREI, e.Meldung);

            DataRow k = Katalogsatz(STAMM_FREI);
            Assert.Equal("Prüfwerk Übernahme", Text(k, "Firma"));
            Assert.Equal("aus dem Projekt", Text(k, "Beschreibung"));
            Assert.Equal(42, Zahl(k, "Nennleistung"));
            Assert.Equal(NAME_FREI, Text(k, "Bezeichner"));
            Assert.False(Convert.ToBoolean(k["ReadOnly"]));
        }

        /// <summary>
        /// Ohne Katalogsatz gleichen Bezeichners entsteht ein NEUER — und die Meldung
        /// sagt das.
        /// </summary>
        [Fact]
        public void UebernehmenAusProjekt_legt_einen_fehlenden_Katalogsatz_an()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const string name = "Prüfgerät Übernahme";
            const int idWp = 9000001;
            EigenesProjektgeraet(idWp, PROJEKT_1006, name);

            var vorschau = WPStammCtrl.UebernahmeVorschau(idWp, PROJEKT_1006);
            Assert.Equal(name, vorschau.Bezeichner);
            Assert.False(vorschau.KatalogsatzVorhanden);

            var e = WPStammCtrl.UebernehmenAusProjekt(idWp, PROJEKT_1006, false);

            Assert.True(e.Ok, e.Meldung);
            Assert.Contains("angelegt", e.Meldung);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM Tab_WP_STAMM WHERE Bezeichner = ?", new DbParam("@bez", name));
            Assert.Single(dt.Rows);
            DataRow k = dt.Rows[0];
            Assert.Equal("Prüfwerk", Text(k, "Firma"));
            Assert.Equal("Luft-Wasser", Text(k, "Typ"));
            Assert.Equal(2021, Zahl(k, "Baujahr"));
            Assert.Equal(33, Zahl(k, "Nennleistung"));
            Assert.Equal(4, Zahl(k, "Heizung"));
            Assert.False(Convert.ToBoolean(k["ReadOnly"]));
        }

        /// <summary>
        /// Ein AUSLIEFERUNGSSATZ wird nicht überschrieben — die Ablehnung trägt den
        /// Hinweis, der auch im Importkonflikt steht.
        /// </summary>
        [Fact]
        public void UebernehmenAusProjekt_laesst_einen_Auslieferungssatz_stehen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataRow vorher = Katalogsatz(STAMM_GESCHUETZT);
            string firmaVorher = Text(vorher, "Firma");
            int nennVorher = Zahl(vorher, "Nennleistung");

            Assert.True(WPCtrl.ProjektgeraetSchreiben(WP_1006, PROJEKT_1006,
                new WPCtrl.ProjektgeraetFelder(Firma: "Darf nicht in den Katalog",
                                               Nennleistung: 55)).Ok);

            var e = WPStammCtrl.UebernehmenAusProjekt(WP_1006, PROJEKT_1006, true);

            Assert.False(e.Ok);
            Assert.Contains("Auslieferungssatz", e.Meldung);
            Assert.Contains("nicht überschrieben", e.Meldung);

            DataRow nachher = Katalogsatz(STAMM_GESCHUETZT);
            Assert.Equal(firmaVorher, Text(nachher, "Firma"));
            Assert.Equal(nennVorher, Zahl(nachher, "Nennleistung"));
        }

        /// <summary>
        /// Mit Kennlinien führt der Katalogsatz danach GENAU die Stützstellen der
        /// Projektkopie — die alten sind ersetzt, nicht ergänzt.
        /// </summary>
        [Fact]
        public void UebernehmenAusProjekt_ersetzt_die_Katalogstuetzstellen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            // Der Projektstand wird zuerst verändert, damit „ersetzt" von „war ohnehin
            // gleich" zu unterscheiden ist: eine Zeile weniger, eine neue dazu.
            var zeilen = KenndatenCtrl.LiesProjekt(WP_1008).ToList();
            Assert.True(zeilen.Count > 2);
            zeilen.RemoveAt(zeilen.Count - 1);
            zeilen.Add(new KenndatenModel
            {
                m_ID = 0, m_ID_WP = WP_1008,
                m_nVorlauf = 65, m_nTemperatur = -7, m_nCOP = 1.23, m_nPTherm = 4.56
            });
            Assert.True(KenndatenCtrl.AbgleichenProjekt(WP_1008, zeilen));

            var projekt = Stuetzstellen("Tab_Kenndaten", WP_1008);
            int katalogVorher = Zeilenzahl("Tab_Kenndaten_STAMM", STAMM_FREI);

            var e = WPStammCtrl.UebernehmenAusProjekt(WP_1008, PROJEKT_1008, true);
            Assert.True(e.Ok, e.Meldung);

            var katalog = Stuetzstellen("Tab_Kenndaten_STAMM", STAMM_FREI);
            Assert.Equal(projekt.Count, katalog.Count);
            Assert.Equal(projekt.OrderBy(t => t).ToList(), katalog.OrderBy(t => t).ToList());
            Assert.Contains((65, -7, 1.23, 4.56), katalog);
            Assert.NotEqual(katalogVorher + projekt.Count, katalog.Count);
        }

        /// <summary>Der Kühlzweig läuft mit — eigene Tabelle, eigene Spalten.</summary>
        [Fact]
        public void UebernehmenAusProjekt_nimmt_die_Kuehlkennlinien_mit()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?",
                new DbParam("@id", WP_1008));
            KuehlzeileAnlegen(WP_1008, 18, 27, 3.5, 11.0, 100);
            KuehlzeileAnlegen(WP_1008, 18, 32, 2.9, 9.5, 50);

            var e = WPStammCtrl.UebernehmenAusProjekt(WP_1008, PROJEKT_1008, true);
            Assert.True(e.Ok, e.Meldung);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Vorlauf, Temperatur, COP, Pkuehl, [Last] FROM Tab_Kenndaten_Kuehlung_STAMM " +
                "WHERE ID_WP = ? ORDER BY Temperatur", new DbParam("@id", STAMM_FREI));
            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal(27, Zahl(dt.Rows[0], "Temperatur"));
            Assert.Equal(3.5, Convert.ToDouble(dt.Rows[0]["COP"]), 6);
            Assert.Equal(11.0, Convert.ToDouble(dt.Rows[0]["Pkuehl"]), 6);
            Assert.Equal(100, Zahl(dt.Rows[0], "Last"));
            Assert.Equal(32, Zahl(dt.Rows[1], "Temperatur"));
            Assert.Equal(50, Zahl(dt.Rows[1], "Last"));
        }

        // =================================================================================
        // 4 - Die Projektkennlinien
        // =================================================================================

        /// <summary>
        /// Lesen, ändern, schreiben, wieder lesen — auf <c>Tab_Kenndaten</c>, und der
        /// Katalog bleibt dabei unberührt.
        /// </summary>
        [Fact]
        public void LiesProjekt_und_AbgleichenProjekt_bilden_einen_Rundlauf()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var katalogVorher = Stuetzstellen("Tab_Kenndaten_STAMM", STAMM_FREI);

            var zeilen = KenndatenCtrl.LiesProjekt(WP_1008).ToList();
            Assert.NotEmpty(zeilen);
            Assert.All(zeilen, z => Assert.Equal(WP_1008, z.m_ID_WP));
            Assert.All(zeilen, z => Assert.True(z.m_ID > 0));

            int anzahlVorher = zeilen.Count;
            int geaenderteId = zeilen[0].m_ID;
            zeilen[0].m_nCOP = 9.87;
            zeilen.RemoveAt(zeilen.Count - 1);
            zeilen.Add(new KenndatenModel
            {
                m_ID = 0, m_ID_WP = WP_1008,
                m_nVorlauf = 55, m_nTemperatur = 12, m_nCOP = 5.55, m_nPTherm = 6.66
            });

            Assert.True(KenndatenCtrl.AbgleichenProjekt(WP_1008, zeilen));

            var nachher = KenndatenCtrl.LiesProjekt(WP_1008).ToList();
            Assert.Equal(anzahlVorher, nachher.Count);
            Assert.Equal(9.87, nachher.Single(z => z.m_ID == geaenderteId).m_nCOP, 6);
            Assert.Contains(nachher, z => z.m_nVorlauf == 55 && z.m_nTemperatur == 12 &&
                                          Math.Abs(z.m_nCOP - 5.55) < 1e-9);
            Assert.All(nachher, z => Assert.Equal(WP_1008, z.m_ID_WP));

            var katalogNachher = Stuetzstellen("Tab_Kenndaten_STAMM", STAMM_FREI);
            Assert.Equal(katalogVorher.OrderBy(t => t).ToList(),
                         katalogNachher.OrderBy(t => t).ToList());
        }

        /// <summary>
        /// <see cref="KenndatenCtrl.LiesStamm"/> und <see cref="KenndatenCtrl.LiesProjekt"/>
        /// lesen zwei VERSCHIEDENE Tabellen — eine Projekt-Id im Katalog trifft nichts.
        /// </summary>
        [Fact]
        public void LiesProjekt_und_LiesStamm_lesen_getrennte_Tabellen()
        {
            if (!_db.Vorhanden) return;

            Assert.NotEmpty(KenndatenCtrl.LiesProjekt(WP_1008));
            Assert.Empty(KenndatenCtrl.LiesStamm(WP_1008));
            Assert.NotEmpty(KenndatenCtrl.LiesStamm(STAMM_FREI));
        }

        // =================================================================================
        // Handreichungen
        // =================================================================================

        private static DataRow Projektsatz(int idWp)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM Tab_WP WHERE ID = ?", new DbParam("@id", idWp));
            Assert.NotNull(dt);
            Assert.NotEmpty(dt.Rows);
            return dt.Rows[0];
        }

        private static DataRow Katalogsatz(int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM Tab_WP_STAMM WHERE ID = ?", new DbParam("@id", id));
            Assert.NotNull(dt);
            Assert.NotEmpty(dt.Rows);
            return dt.Rows[0];
        }

        /// <summary>Wie viele ANDERE Projekte fuehren eine Kopie gleichen Bezeichners?</summary>
        private static int AndereProjekte(string bezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(DISTINCT ID_Projekt) FROM Tab_WP WHERE Bezeichner = ? AND ID_Projekt <> ?",
                new DbParam("@bez", bezeichner), new DbParam("@proj", idProjekt));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static int Zeilenzahl(string tabelle, int idWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + tabelle + " WHERE ID_WP = ?", new DbParam("@id", idWp));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        /// <summary>Die Stuetzstellen einer Waerme-Kennlinientabelle als Vergleichswerte.</summary>
        private static List<(int, int, double, double)> Stuetzstellen(string tabelle, int idWp)
        {
            var liste = new List<(int, int, double, double)>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Vorlauf, Temperatur, COP, Ptherm FROM " + tabelle + " WHERE ID_WP = ?",
                new DbParam("@id", idWp));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add((Zahl(r, "Vorlauf"), Zahl(r, "Temperatur"),
                           Math.Round(Convert.ToDouble(r["COP"]), 6),
                           Math.Round(Convert.ToDouble(r["Ptherm"]), 6)));
            return liste;
        }

        /// <summary>
        /// Ein Projektgeraet, das es im Katalog NICHT gibt — der Fall „Insert" der
        /// Uebernahme. Angelegt wird es unmittelbar, damit kein anderer Fall seinen
        /// Bestand verschiebt.
        /// </summary>
        private static void EigenesProjektgeraet(int idWp, int idProjekt, string bezeichner)
        {
            DataRepository.ExecuteSQL("DELETE FROM Tab_WP_STAMM WHERE Bezeichner = ?",
                new DbParam("@bez", bezeichner));
            DataRepository.ExecuteSQL("DELETE FROM Tab_WP WHERE ID = ?", new DbParam("@id", idWp));
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_WP (ID, Bezeichner, ID_Projekt, Firma, Beschreibung, Typ, " +
                "Baujahr, Aufstellung, Nennleistung, maxPtherm, Heizung, Regelung, Modulkosten, " +
                "Kuehlleistung, Bauart) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam("@id", idWp),
                new DbParam("@bez", bezeichner),
                new DbParam("@proj", idProjekt),
                new DbParam("@fir", "Prüfwerk"),
                new DbParam("@bes", "Prüfsatz der Übernahme"),
                new DbParam("@typ", "Luft-Wasser"),
                new DbParam("@bau", 2021),
                new DbParam("@auf", "innen"),
                new DbParam("@nen", 33),
                new DbParam("@max", 40),
                new DbParam("@hei", 4),
                new DbParam("@reg", "stetig"),
                new DbParam("@mod", 1000),
                new DbParam("@kue", 0.0),
                new DbParam("@bart", "Prüfbauart"));
        }

        /// <summary>Eine Kuehl-Stuetzstelle der PROJEKTKOPIE (die Tabelle ist im Bestand leer).</summary>
        private static void KuehlzeileAnlegen(int idWp, int vorlauf, int temperatur,
                                              double cop, double pkuehl, int last)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Kenndaten_Kuehlung (ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) " +
                "VALUES (?, ?, ?, ?, ?, ?)",
                new DbParam("@wp", idWp),
                new DbParam("@vor", vorlauf),
                new DbParam("@tem", temperatur),
                new DbParam("@cop", cop),
                new DbParam("@pk", pkuehl),
                new DbParam("@last", last));
        }

        private static string Text(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? "" : r[spalte].ToString();

        private static int Zahl(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? 0 : Convert.ToInt32(r[spalte]);

        /// <summary>Eine REAL-Spalte, invariant gelesen (die Fälle pinnen die Kultur).</summary>
        private static double Kommazahl(DataRow r, string spalte)
            => r[spalte] == DBNull.Value
                ? 0.0
                : Convert.ToDouble(r[spalte], System.Globalization.CultureInfo.InvariantCulture);
    }
}
