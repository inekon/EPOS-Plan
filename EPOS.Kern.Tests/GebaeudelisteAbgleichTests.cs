using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gebäudeliste eines Projekts wird ABGEGLICHEN, nicht neu aufgebaut</b>
    /// (Konzept Administrationsdialoge 7.1 (a)): <c>WizardCtrl.Schreibe_Projekt_ZuordungGebäude</c>
    /// und sein Startseitenweg <c>Speichere_Projekt_Gebaeudeliste</c>. Eine unveränderte
    /// Zuordnung behält ihre Projektkopie samt Feld-Übernahmen und Tagesverteilung, eine
    /// entfernte geht, eine neue entsteht aus dem Katalog, die Zuordnungswerte werden
    /// fortgeschrieben — und ein Fehlschlag nimmt den ganzen Vorgang zurück.
    ///
    /// <para><b>Der Träger.</b> Projekt 1007 „Laurentiuskirche" mit genau einem Gebäude:
    /// Zuordnung und Kopie 10614 aus Katalogsatz 142 „EFH-A-TS-212", mit einer Zeile
    /// Tagesverteilung. Neu hinzu kommt Katalogsatz 125 „GMH-D-S-118". Jeder Fall bekommt
    /// seine eigene Arbeitskopie der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudelisteAbgleichTests
    {
        private const int PROJEKT = 1007;
        private const int ZUORDNUNG_1007 = 10614;
        private const int KOPIE_1007 = 10614;
        private const int STAMM_EFH = 142;
        private const int STAMM_GMH = 125;
        private const string NAME_GMH = "GMH-D-S-118";

        /// <summary>Die vorläufige Id einer noch nicht gespeicherten Zeile (<c>GebaeudeHuelle.STARTINDEX</c>).</summary>
        private const int VORLAEUFIG = 100000;

        /// <summary>Eine Feld-Übernahme in die Projektkopie — ein U-Wert, den der Katalog nicht führt.</summary>
        private const double UWERT_UEBERNOMMEN = 0.987;

        // =====================================================================
        //  Kopien bleiben
        // =====================================================================

        /// <summary>
        /// <b>Die Feld-Übernahme überlebt das Speichern der Liste:</b> Die Kopie bleibt
        /// dieselbe Zeile (Id, übernommener U-Wert, Tagesverteilung), nur die Zuordnungswerte
        /// der Liste werden geschrieben — und das Projekt gilt als geändert.
        /// </summary>
        [Fact]
        public void Unveraenderte_Zuordnung_behaelt_Kopie_und_Feld_Uebernahme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Feldübernahme();
            long tagV = Zahl("SELECT COUNT(*) FROM Tab_DBTagV WHERE ID_Gebaeude = ?", KOPIE_1007);
            Assert.True(tagV > 0, "Die Testdatenbank fuehrt keine Tagesverteilung mehr fuer die Kopie.");
            Sql("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?", "2020-01-01 00:00:00", PROJEKT);

            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel zeile = Assert.Single(liste);
            zeile.Wohnflaeche = 432.5;
            zeile.Einheit = "kWh/a";
            zeile.Jahresnutzungsgrad = 0.85;
            zeile.DezentralWarmwasser = true;

            (bool gelungen, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);
            Assert.True(gelungen, meldung);
            Assert.Equal("", meldung);

            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(UWERT_UEBERNOMMEN, Kommazahl("SELECT k_Wert_Außenwand FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007), 9);
            Assert.Equal((long)STAMM_EFH, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007));
            Assert.Equal(tagV, Zahl("SELECT COUNT(*) FROM Tab_DBTagV WHERE ID_Gebaeude = ?", KOPIE_1007));

            Z_ProjGebModel nachher = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
            Assert.Equal(ZUORDNUNG_1007, nachher.ID_Z);
            Assert.Equal(432.5, nachher.Wohnflaeche, 9);
            Assert.Equal("kWh/a", nachher.Einheit);
            Assert.Equal(0.85, nachher.Jahresnutzungsgrad, 9);
            Assert.True(nachher.DezentralWarmwasser);

            DateTime? geaendert = MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT);
            Assert.True(geaendert.HasValue && geaendert.Value.Year > 2020, "Aenderungsdatum nicht gesetzt.");
        }

        /// <summary>
        /// <b>Assistent und Startseite gehen denselben Weg:</b> Der BEARBEITEN-Zweig des
        /// Assistenten schreibt die Gebäudeliste über dieselbe Methode — die Feld-Übernahme
        /// in die Kopie bleibt auch dort stehen.
        /// </summary>
        [Fact]
        public void Der_Assistent_behaelt_die_Kopie_ebenso()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WizardCtrl vorher = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl.Aktueller = new WizardCtrl();
                Feldübernahme();

                // Ein Bearbeitenlauf, gestellt wie in AssistentCtrlTests.Bearbeitenlauf.
                const string NAME = "Laurentiuskirche";
                AssistentCtrl a = new AssistentCtrl();
                a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
                a.ProjektId = PROJEKT;
                a.Laden(NAME);

                KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(PROJEKT);
                for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
                    a.SeiteSchalten(bestand[k].SeitenIndex, bestand[k].Vorhanden);

                ProjektKopfDaten kopf = ProjektCtrl.Kopf(NAME);
                Assert.NotNull(kopf);
                a.Kopf[0].Name = kopf.Name;
                a.Kopf[0].Beschreibung = kopf.Beschreibung;
                a.Kopf[0].Kunde = kopf.Kunde;
                a.Kopf[0].Bearbeiter = kopf.Bearbeiter;
                a.Kopf[0].Erstelldatum = kopf.Erstelldatum;
                a.Kopf[0].IdKlimaregion = kopf.IdKlimaregion;
                a.Kopf[0].Klimaname = kopf.Klimaname;

                Z_ProjGebModel zeile = Assert.Single(a.Gebaeude);
                zeile.Wohnflaeche = 377.0;

                AssistentErgebnis e = a.Speichern();
                Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);

                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT));
                Assert.Equal(UWERT_UEBERNOMMEN, Kommazahl("SELECT k_Wert_Außenwand FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007), 9);
                Assert.Equal(377.0, Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT)).Wohnflaeche, 9);
            }
            finally { WizardCtrl.Aktueller = vorher; }
        }

        // =====================================================================
        //  Neu, entfernt, geändert
        // =====================================================================

        /// <summary>
        /// <b>Neu und entfernt:</b> Eine vorläufige Zeile entsteht aus dem Katalog, die
        /// bestehende bleibt; im zweiten Schritt fehlt die bestehende in der Liste und geht
        /// samt Kopie und Tagesverteilung, während die inzwischen gespeicherte neue bleibt.
        /// </summary>
        [Fact]
        public void Neue_Zeile_entsteht_und_entfernte_Zuordnung_geht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var wizard = new WizardCtrl();
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            liste.Add(Neu(NAME_GMH, STAMM_GMH));

            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, liste).Gelungen);
            List<Z_ProjGebModel> zwei = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(2, zwei.Count);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007));
            Z_ProjGebModel gmh = Assert.Single(zwei, z => z.ID_Gebaeude_Stamm == STAMM_GMH);
            Assert.Equal(250.0, gmh.Wohnflaeche, 9);
            long kopieGmh = Zahl("SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?", gmh.ID_Z);

            // Zweiter Schritt: nur noch das neue Gebaeude.
            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, new List<Z_ProjGebModel> { gmh }).Gelungen);
            Z_ProjGebModel rest = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
            Assert.Equal(gmh.ID_Z, rest.ID_Z);
            Assert.Equal(kopieGmh, Zahl("SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?", gmh.ID_Z));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID = ?", ZUORDNUNG_1007));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_DBTagV WHERE ID_Gebaeude = ?", KOPIE_1007));

            // Eine leere Liste raeumt das Projekt ganz ab - wie der Neuaufbau.
            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, new List<Z_ProjGebModel>()).Gelungen);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", PROJEKT));
        }

        /// <summary>
        /// <b>Ein geänderter Katalogverweis ist keine unveränderte Zuordnung:</b> Dieselbe
        /// Zeilen-Id mit einem anderen Katalogsatz ersetzt die Kopie durch eine neue aus dem
        /// neuen Satz. Und die Zeilen-Id eines FREMDEN Projekts wird nicht übernommen — sie
        /// gilt als neue Zeile, die fremde Zuordnung bleibt unberührt.
        /// </summary>
        [Fact]
        public void Geaenderter_Verweis_und_fremde_Zeile_entstehen_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Feldübernahme();
            var wizard = new WizardCtrl();

            Z_ProjGebModel zeile = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
            zeile.ID_Gebaeude_Stamm = STAMM_GMH;
            zeile.Gebaeudename = NAME_GMH;
            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, new List<Z_ProjGebModel> { zeile }).Gelungen);

            Z_ProjGebModel neu = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
            Assert.NotEqual(ZUORDNUNG_1007, neu.ID_Z);
            Assert.Equal(STAMM_GMH, neu.ID_Gebaeude_Stamm);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007));

            // Die Zeile eines fremden Projekts (1017).
            Z_ProjGebModel fremd = Assert.Single(Z_ProjGebCtrl.LiesProjekt(1017));
            long fremdKopien = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?", fremd.ID_Z);
            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, new List<Z_ProjGebModel> { neu, fremd }).Gelungen);

            List<Z_ProjGebModel> nachher = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(2, nachher.Count);
            Assert.Contains(nachher, z => z.ID_Z == neu.ID_Z);
            Assert.DoesNotContain(nachher, z => z.ID_Z == fremd.ID_Z);
            Assert.Equal(1017L, Zahl("SELECT ID_Projekt FROM Z_ProjektGebaeude WHERE ID = ?", fremd.ID_Z));
            Assert.Equal(fremdKopien, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?", fremd.ID_Z));
        }

        // =====================================================================
        //  Der Vorgang
        // =====================================================================

        /// <summary>
        /// <b>Ein Fehlschlag nimmt alles zurück:</b> Die Liste entfernt die bestehende
        /// Zuordnung und bringt eine neue Zeile, deren Katalogsatz es nicht gibt. Der Weg
        /// der Startseite meldet das benannt, und das Projekt behält Zuordnung, Kopie samt
        /// Feld-Übernahme, Tagesverteilung und Änderungsdatum.
        /// </summary>
        [Fact]
        public void Fehlschlag_rollt_den_ganzen_Vorgang_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Feldübernahme();
            Sql("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?", "2020-01-01 00:00:00", PROJEKT);
            DateTime? datumVorher = MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT);
            string zuordnungVorher = Abbild("SELECT * FROM Z_ProjektGebaeude WHERE ID_Projekt = ?");
            string kopieVorher = Abbild("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?");
            long tagV = Zahl("SELECT COUNT(*) FROM Tab_DBTagV WHERE ID_Gebaeude = ?", KOPIE_1007);
            long maxZ = Zahl("SELECT MAX(ID) FROM Z_ProjektGebaeude");

            const string UNBEKANNT = "Kein Gebaeude dieses Namens (#475)";
            List<Z_ProjGebModel> liste = new List<Z_ProjGebModel>
            {
                Neu(NAME_GMH, STAMM_GMH),
                Neu(UNBEKANNT, null)
            };

            (bool gelungen, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);

            Assert.False(gelungen);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture,
                             WindowsFormsApplication1.MyResource.Resource.GEB_MSG_LISTE_KATALOGSATZ_FEHLT, UNBEKANNT), meldung);
            Assert.Contains(UNBEKANNT, meldung);

            Assert.Equal(zuordnungVorher, Abbild("SELECT * FROM Z_ProjektGebaeude WHERE ID_Projekt = ?"));
            Assert.Equal(kopieVorher, Abbild("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?"));
            Assert.Equal(UWERT_UEBERNOMMEN, Kommazahl("SELECT k_Wert_Außenwand FROM Tab_Gebaeude WHERE ID = ?", KOPIE_1007), 9);
            Assert.Equal(tagV, Zahl("SELECT COUNT(*) FROM Tab_DBTagV WHERE ID_Gebaeude = ?", KOPIE_1007));
            Assert.Equal(maxZ, Zahl("SELECT MAX(ID) FROM Z_ProjektGebaeude"));
            Assert.Equal(datumVorher, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT));
        }

        /// <summary>
        /// Ohne Gebäudenamen (oder ohne Gebäudebezug, etwa bei einer Ausnahme) nennt die
        /// Meldung nur den Ausgang — hier erzwungen über eine neue Zeile ohne Namen und
        /// Verweis; das Projekt behält sein Gebäude.
        /// </summary>
        [Fact]
        public void Fehlschlag_ohne_Namen_meldet_den_allgemeinen_Satz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            (bool gelungen, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(
                PROJEKT, new List<Z_ProjGebModel> { Neu(null, null) });

            Assert.False(gelungen);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_MSG_LISTE_NICHT_GESPEICHERT, meldung);
            Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static Z_ProjGebModel Neu(string name, int? stamm) => new Z_ProjGebModel
        {
            ID_Z = VORLAEUFIG,
            ID_Projekt = PROJEKT,
            Gebaeudename = name,
            ID_Gebaeude_Stamm = stamm,
            Wohnflaeche = 250.0,
            Einheit = "Wohnfläche [m²]",
            Jahresnutzungsgrad = 1.0
        };

        /// <summary>Die Feld-Übernahme — ein zielgenaues UPDATE einer Spalte der Kopie.</summary>
        private static void Feldübernahme()
            => Sql("UPDATE Tab_Gebaeude SET k_Wert_Außenwand = ? WHERE ID = ?", UWERT_UEBERNOMMEN, KOPIE_1007);

        /// <summary>Der vollständige Zeileninhalt einer Projektabfrage als Text.</summary>
        private static string Abbild(string sql)
        {
            var dt = DataRepository.GetDataTable(sql, new DbParam("@p", PROJEKT));
            Assert.NotNull(dt);
            return string.Join("\n", dt.Rows.Cast<System.Data.DataRow>().Select(r =>
                string.Join("|", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))));
        }

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static void Sql(string sql, params object[] werte)
            => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), "Fehlgeschlagen: " + sql);

        private static long Zahl(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static double Kommazahl(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? double.NaN : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }
    }
}
