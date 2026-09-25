using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Stufe G4, Welle 4 — die Profilwahl nach der Dateiendung (EINE Dateiwahl für gbXML und IFC)
    /// und die Grenze je Plattform, ohne Datenbank.
    /// </summary>
    public class GebaeudeImportProfilwahlTests
    {
        [Theory]
        [InlineData("haus.xml", "GBXML")]
        [InlineData("HAUS.GBXML", "GBXML")]
        [InlineData(@"C:\Plaene\haus.gbxml", "GBXML")]
        [InlineData("haus.ifc", "IFC")]
        [InlineData("/var/mobile/Haus.IfcXml", "IFC")]
        [InlineData("haus.ifczip", "IFC")]
        public void Das_Profil_folgt_der_Endung(string datei, string format)
        {
            GebaeudeImportProfil p = GebaeudeImportProfil.FuerDatei(datei);
            Assert.NotNull(p);
            Assert.Equal(format, p.Format);
            Assert.Equal(format == "IFC" ? typeof(IfcImportProfil) : typeof(GbxmlImportProfil), p.GetType());
            Assert.Equal(format == "IFC" ? IfcImportProfil.MAX_BYTES_WINDOWS : GbxmlImportProfil.MAX_BYTES_WINDOWS, p.MaxBytes);
        }

        [Theory]
        [InlineData("haus.txt")]
        [InlineData("haus.zip")]
        [InlineData("haus")]
        [InlineData("haus.ifc.bak")]
        [InlineData("")]
        [InlineData(null)]
        public void Eine_andere_Endung_ergibt_kein_Profil(string datei)
            => Assert.Null(GebaeudeImportProfil.FuerDatei(datei));

        [Fact]
        public void Endung_Filter_Grenzen_und_Hilfe_stehen_je_Format()
        {
            Assert.Equal(".ifcxml", GebaeudeImportProfil.Endung(@"D:\a.b\Haus.IFCXML"));
            Assert.Equal("", GebaeudeImportProfil.Endung(@"D:\a.b\Haus"));

            // Der gemeinsame Filter trägt die Muster beider Profile.
            foreach (string muster in new[] { "*.xml", "*.gbxml", "*.ifc", "*.ifcxml", "*.ifczip" })
                Assert.Contains(muster, GebaeudeImportProfil.DATEIFILTER_ALLE.Split('|')[1].Split(';'));

            // Die IFC-Grenze je Plattform (U11) — wie die gbXML-Grenze (D15).
            var ifc = new IfcImportProfil();
            Assert.Equal(50L * 1024 * 1024, ifc.GrenzeFuerPlattform(false));
            Assert.Equal(20L * 1024 * 1024, ifc.GrenzeFuerPlattform(true));

            // Beide Formate zeigen auf die eigene Hilfeseite des Zuordnungsdialogs.
            Assert.Equal("Form_GebaeudeImport.btn_Help", GebaeudeImportProfil.HILFE_ZUORDNUNG);
            Assert.Equal(GebaeudeImportProfil.HILFE_ZUORDNUNG, new GbxmlImportProfil().HilfeSchluessel);
            Assert.Equal(GebaeudeImportProfil.HILFE_ZUORDNUNG, ifc.HilfeSchluessel);
        }
    }

    /// <summary>
    /// Stufe G4, Welle 4 — Belegwerte und Zahlen in Meldungen des Zuordnungsmodells erscheinen in
    /// der ANZEIGEKULTUR; gespeichert und verglichen wird weiter invariant.
    /// </summary>
    public class GebaeudeImportAnzeigekulturTests
    {
        [Theory]
        [InlineData("de-DE", "18.37", "18,37")]
        [InlineData("en-US", "18.37", "18.37")]
        [InlineData("de-DE", "-0.5", "-0,5")]
        [InlineData("de-DE", "2024", "2024")]            // Ganzzahl: kein Tausendertrennzeichen
        [InlineData("de-DE", "1E-05", "0,00001")]
        [InlineData("en-US", "1E-05", "0.00001")]
        [InlineData("de-DE", "haus.xml", "haus.xml")]
        [InlineData("de-DE", "geb-1", "geb-1")]
        [InlineData("de-DE", "1.2.0.4", "1.2.0.4")]
        [InlineData("de-DE", "", "")]
        public void Ein_Wert_erscheint_in_der_Anzeigekultur(string kultur, string wert, string erwartet)
        {
            using var k = new Kulturvorrichtung(kultur);
            Assert.Equal(erwartet, GebaeudeZuordnungsModell.AnzeigeWert(wert));
        }

        [Fact]
        public void Der_Beleg_zeigt_seine_Dezimalzahl_mit_Komma_und_behaelt_sie_invariant()
        {
            var beleg = new GebaeudeBeleg("GIMP_BELEG_BAUWEISE_SCHICHTEN", "919", "18.37");
            using (new Kulturvorrichtung("de-DE"))
                Assert.Equal("aus den Schichten 919 Wh/K (18,37 Wh/(m²K)) — der Gebäudeeditor bildet die Bauweise aus der Bauart",
                             GebaeudeZuordnungsModell.BelegText(beleg));
            using (new Kulturvorrichtung("en-US"))
                Assert.Contains("919 Wh/K (18.37 Wh/(m²K))", GebaeudeZuordnungsModell.BelegText(beleg));
            Assert.Equal("18.37", beleg.Werte[1]);     // gespeichert bleibt invariant
        }

        [Fact]
        public void Die_Meldung_zeigt_ihre_Zahlen_in_der_Anzeigekultur_den_Versionswert_nicht()
        {
            var u = new PruefMeldung(PruefStufe.Warnung, GebaeudeImportAblauf.MELDUNG + "U_AUSSERHALB",
                                     GebaeudeZielfelder.U_AUSSENWAND, "7.5", "0.1", "6");
            var version = new PruefMeldung(PruefStufe.Info, GbxmlImportProfil.MELDUNGSPRAEFIX + "VERSION_UNBEKANNT", "0.40");
            using (new Kulturvorrichtung("de-DE"))
            {
                Assert.Equal("U-Wert Außenwand = 7,5 W/(m²K) liegt außerhalb 0,1 … 6 W/(m²K).", GebaeudeZuordnungsModell.MeldungText(u));
                Assert.Contains("„0.40“", GebaeudeZuordnungsModell.MeldungText(version));
            }
            using (new Kulturvorrichtung("en-US"))
            {
                string text = GebaeudeZuordnungsModell.MeldungText(u);
                Assert.Contains("= 7.5 W/(m²K)", text);
                Assert.Contains("0.1 … 6", text);
            }
            Assert.Equal("7.5", u.Werte[1]);
        }
    }

    /// <summary>
    /// Stufe G4, Welle 4 — der Gebäudeimport im Gebäudedialog gegen die Arbeitskopie der
    /// Testdatenbank: Die ausstehende Herkunft einer NEUEN Zeile wird mit der Gebäudeliste im selben
    /// Vorgang an die neue Projektkopie geschrieben; ohne Herkunft entsteht nichts; scheitert die
    /// Herkunft, scheitert die ganze Liste; eine bleibende Zeile schreibt sie nicht neu; und die
    /// Abfrage „schon importiert" je Projekt. Den ganzen Weg über die Hüllen prüft
    /// <see cref="GebaeudeImportDialogwegTests"/>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeImportAnbindungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const int ANDERES_PROJEKT = 1039;

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static long Zeilen(string tabelle) => Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\"");

        /// <summary>Die Projektkopie einer Zuordnung (<c>Tab_Gebaeude.ID_ProjektGebaeude</c>).</summary>
        private static int Kopie(int idZ)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", idZ), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        /// <summary>Eine NEUE Zeile der Liste aus dem Katalogsatz der ersten Projektzeile — mit oder ohne Herkunft.</summary>
        private static Z_ProjGebModel NeueZeile(List<Z_ProjGebModel> liste, GebaeudeImportHerkunft herkunft)
        {
            Z_ProjGebModel vorlage = liste[0];
            return new Z_ProjGebModel
            {
                ID_Z = 100000,
                ID_Projekt = PROJEKT,
                ID_Gebaeude_Stamm = vorlage.ID_Gebaeude_Stamm,
                Gebaeudename = vorlage.Gebaeudename,
                Wohnflaeche = 100,
                Einheit = "Wohnfläche [m²]",
                Jahresnutzungsgrad = 1,
                Importherkunft = herkunft
            };
        }

        private static GebaeudeImportHerkunft Herkunft(out GebaeudeImportSatz satz)
        {
            satz = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            return new GebaeudeImportHerkunft(satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz));
        }

        // =============================================================================
        //  Der Speicherweg der Gebäudeliste
        // =============================================================================

        [Fact]
        public void Eine_neue_Zeile_schreibt_ihre_Herkunft_an_die_neue_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, Herkunft(out GebaeudeImportSatz satz));
            liste.Add(neu);

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);
            Assert.True(ok, meldung);
            Assert.NotEqual(100000, neu.ID_Z);                     // die echte Zuordnungs-Id

            int kopie = Kopie(neu.ID_Z);
            var ctrl = new GebaeudeImportCtrl();
            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(kopie));
            Assert.Equal(1L, Zeilen("Tab_Importquelle"));
            Assert.Equal("gbxml_haus_si.xml", q.Dateiname);
            Assert.Equal(satz.Quelle.Hash, q.Hash);
            Assert.Equal(DbWerte.IMPORT_FORMAT_GBXML, q.Format);
            ImportzuordnungModel z = Assert.Single(ctrl.LesenZuordnungen(q.ID));
            Assert.Equal(kopie, z.ID_Gebaeude);
            Assert.Equal("Building", z.Quelltyp);
            Assert.Equal(satz.Gebaeudekennung, z.Quellkennung);
        }

        [Fact]
        public void Ohne_Herkunft_entstehen_keine_Herkunftszeilen()
        {
            if (!_db.Vorhanden) return;
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            liste.Add(NeueZeile(liste, null));

            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste).Gelungen);
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));
        }

        /// <summary>
        /// Scheitert das Schreiben der Herkunft, scheitert die Übernahme wie jeder andere Schritt:
        /// Der Vorgang rollt zurück — weder Zuordnung noch Projektkopie noch Herkunft bleiben, die
        /// Zeile behält ihre vorläufige Id, und die Meldung nennt den Grund.
        /// </summary>
        [Fact]
        public void Scheitert_die_Herkunft_wird_nichts_geschrieben()
        {
            if (!_db.Vorhanden) return;
            long zuordnungenVorher = Zeilen("Z_ProjektGebaeude");
            long kopienVorher = Zeilen("Tab_Gebaeude");
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            GebaeudeQuelle kaputt = ImportzuordnungSchemaRegelTests.Quelle(hash: new string('A', 64));   // kein Hash der Hausregel
            Z_ProjGebModel neu = NeueZeile(liste, new GebaeudeImportHerkunft(kaputt, Array.Empty<GebaeudeQuellzuordnung>()));
            liste.Add(neu);

            var wizard = new WizardCtrl();
            (bool ok, string meldung) = wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);

            Assert.False(ok);
            Assert.StartsWith(R.GEB_MSG_LISTE_NICHT_GESPEICHERT, meldung);
            Assert.Contains(R.HERKUNFT_MSG_HASH, meldung);
            Assert.Equal(R.HERKUNFT_MSG_HASH, wizard.Herkunftsfehler);
            Assert.Equal(100000, neu.ID_Z);
            Assert.Equal(zuordnungenVorher, Zeilen("Z_ProjektGebaeude"));
            Assert.Equal(kopienVorher, Zeilen("Tab_Gebaeude"));
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));
        }

        [Fact]
        public void Eine_bleibende_Zeile_behaelt_ihre_Herkunft_beim_zweiten_Speichern()
        {
            if (!_db.Vorhanden) return;
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, Herkunft(out _));
            liste.Add(neu);
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste).Gelungen);
            int kopie = Kopie(neu.ID_Z);
            ImportquelleModel vorher = Assert.Single(new GebaeudeImportCtrl().LesenQuellen(kopie));

            // Dieselbe Liste noch einmal (die Zeile trägt ihre Herkunft weiter) — und frisch gelesen.
            neu.Wohnflaeche = 120;
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste).Gelungen);
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, Z_ProjGebCtrl.LiesProjekt(PROJEKT)).Gelungen);

            Assert.Equal(kopie, Kopie(neu.ID_Z));
            ImportquelleModel nachher = Assert.Single(new GebaeudeImportCtrl().LesenQuellen(kopie));
            Assert.Equal(vorher.ID, nachher.ID);
            Assert.Equal(1L, Zeilen("Tab_Importquelle"));
            Assert.Equal(1L, Zeilen("Tab_Importzuordnung"));
        }

        [Fact]
        public void ImporteImProjekt_nennt_Gebaeude_und_Zeitpunkt_je_Projekt()
        {
            if (!_db.Vorhanden) return;
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, Herkunft(out GebaeudeImportSatz satz));
            liste.Add(neu);
            var ctrl = new GebaeudeImportCtrl();
            Assert.Empty(ctrl.ImporteImProjekt(PROJEKT, satz.Quelle.Hash));
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste).Gelungen);

            GebaeudeImportCtrl.ImportTreffer t = Assert.Single(ctrl.ImporteImProjekt(PROJEKT, satz.Quelle.Hash.ToUpperInvariant()));
            Assert.Equal(Kopie(neu.ID_Z), t.IdGebaeude);
            Assert.Equal(neu.Gebaeudename, t.Gebaeudename);
            Assert.Equal(satz.Quelle.Zeitpunkt, t.Zeitpunkt);
            Assert.Empty(ctrl.ImporteImProjekt(ANDERES_PROJEKT, satz.Quelle.Hash));
            Assert.Empty(ctrl.ImporteImProjekt(PROJEKT, new string('b', 64)));
            Assert.Empty(ctrl.ImporteImProjekt(PROJEKT, "kurz"));
        }
    }
}
