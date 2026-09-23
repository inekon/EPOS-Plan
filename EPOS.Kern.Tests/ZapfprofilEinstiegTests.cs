using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zapfprofil-Einstieg im Bedarfsprofil-Dialog</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.2; ZU4, ZU6, ZU10; Stufe Z1, Gruppe 3) — der plattformfreie Teil,
    /// den die Windows-Hüllen (Bedarfsprofil, Startseite) und der Gebäudekatalog rufen:
    /// <see cref="ZapfprofilHuelle.Einhaengen"/>, <see cref="ZapfprofilHuelle.Leistenmeldung"/>,
    /// <see cref="ZapfprofilHuelle.BrauchwasserSchreiben"/> und die gestapelte Brauchwassersicht
    /// des Ergebnisdialogs.
    ///
    /// <para>Ohne Datenbank: der benannte Grund ohne Naht und ohne gespeichertes Projekt, die
    /// Beschriftungen in beiden Sprachen, die leere Meldung des Bestandswegs. Auf der
    /// Arbeitskopie der Testdatenbank (fiktiver Katalog TEST-1, Projekt 1007): Parametersatz mit
    /// Optionsgruppe, die Meldung der Leiste zu einer Nullzone, der EINE Vorgang für Zuordnungen
    /// und Zapfprofil samt Rückrollen, und die Zirkulation im Ergebnisdialog. Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilEinstiegTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const string NUTZUNG = "Testnutzung A (fiktiv)";
        private const string VERSION = "TEST-1";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Ohne Datenbank
        // =================================================================================

        [Fact]
        public void Ohne_Behaelter_gibt_es_nur_den_benannten_Grund_der_Plattform()
        {
            var gaben = new Dictionary<string, object>();
            ZapfprofilHuelle.Einhaengen(gaben, PROJEKT, true, null);

            Assert.Null(gaben["ZapfprofilGaben"]);
            Assert.Null(gaben["ZapfprofilUebernommen"]);
            Assert.Equal("Der Zapfprofilgenerator ist auf dieser Plattform noch nicht erreichbar.", gaben["ZapfprofilSperrgrund"]);
            Assert.False(gaben.ContainsKey("RechenwegGesetzt"));
            Assert.IsType<ZapfprofilEinstiegTexte>(gaben["ZapfprofilEinstiegTexte"]);
        }

        [Fact]
        public void Ohne_gespeichertes_Projekt_ist_der_Knopf_benannt_gesperrt()
        {
            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            var gaben = new Dictionary<string, object>();
            ZapfprofilHuelle.Einhaengen(gaben, PROJEKT, false, behaelter);

            Assert.Null(gaben["ZapfprofilGaben"]);
            Assert.Equal("Das Zapfprofil braucht ein gespeichertes Projekt.", gaben["ZapfprofilSperrgrund"]);
            Assert.False(gaben.ContainsKey("RechenwegGesetzt"));
            Assert.False(behaelter.Geaendert);
        }

        [Fact]
        public void Die_Beschriftungen_des_Einstiegs_folgen_der_Oberflaechensprache()
        {
            ZapfprofilEinstiegTexte de = ZapfprofilHuelle.EinstiegTexte();
            Assert.Equal("Zapfprofil erzeugen…", de.Knopf);
            Assert.Equal("Rechenweg Brauchwasser", de.LabelRechenweg);
            Assert.Contains("Brauchwasser (Trinkwarmwasser)", de.HinweisRechenweg);

            using (new Kulturvorrichtung("en-US"))
            {
                ZapfprofilEinstiegTexte en = ZapfprofilHuelle.EinstiegTexte();
                Assert.Equal("Generate draw-off profile…", en.Knopf);
                Assert.Equal("DHW calculation method", en.LabelRechenweg);
            }
        }

        [Fact]
        public void Der_Bestandsweg_hat_keine_Leistenmeldung()
        {
            Assert.Equal("", ZapfprofilHuelle.Leistenmeldung(null));
            Assert.Equal("", ZapfprofilHuelle.Leistenmeldung(new BedarfsVorschau { Art = BedarfsArt.Brauchwasser }));
        }

        // =================================================================================
        // Auf der Arbeitskopie der Testdatenbank
        // =================================================================================

        [Fact]
        public void Mit_Projekt_haengt_die_Huelle_Knopf_und_Optionsgruppe_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            var gaben = new Dictionary<string, object>();
            ZapfprofilHuelle.Einhaengen(gaben, PROJEKT, true, behaelter);

            Assert.NotNull(gaben["ZapfprofilGaben"]);
            Assert.NotNull(gaben["ZapfprofilUebernommen"]);
            Assert.Equal("", gaben["ZapfprofilSperrgrund"]);
            Assert.Equal(ZapfprofilWeg.Bestand, gaben["RechenwegBrauchwasser"]);
            Assert.Equal(0, gaben["ZapfprofilZonen"]);

            // Die Optionsgruppe schreibt nicht, sie füllt den Behälter.
            ((Action<ZapfprofilWeg>)gaben["RechenwegGesetzt"])(ZapfprofilWeg.Generator);
            Assert.True(behaelter.Geaendert);
            Assert.Equal(ZapfprofilWeg.Generator, behaelter.Weg);
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
        }

        [Fact]
        public void Die_Leiste_rechnet_den_Arbeitsstand_und_nennt_eine_Nullzone()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            behaelter.Uebernehmen(new ZapfprofilStand(BrauchwasserWeg.Generator, new[]
            {
                new ZonenStand { Name = "Zone Probe", IdNutzungsart = Nutzungsart(), Bezugsmenge = 10 },
                new ZonenStand { Name = "Zone leer", IdNutzungsart = Nutzungsart(), Bezugsmenge = 0 }
            }, null));

            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT,
                                                                   new List<string>(), behaelter.Arbeitsstand);
            Assert.True(v.Erfolgreich, v.Meldung);
            Assert.True(v.Zapfprofilweg);

            string meldung = ZapfprofilHuelle.Leistenmeldung(v);
            Assert.StartsWith("Zone „Zone leer“ trägt 0: ", meldung);
            Assert.DoesNotContain(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX, meldung);

            // Der Ergebnisdialog stapelt die Zirkulation und nennt sie als eigenen Posten.
            var daten = (BedarfErgebnisDaten)BedarfErgebnisHuelle.Gaben(v.Waerme, true, 2, "Zapfprofilgenerator")["Daten"];
            Assert.Contains(daten.Kennzahlen, k => k.Bezeichnung == "davon Zirkulation:");
            Monatssicht brauchwasser = Assert.Single(daten.Sichten, s => s.IstBrauchwasser);
            Assert.NotNull(brauchwasser.Modell);
            Assert.NotNull(brauchwasser.ModellKWh);
            Assert.Equal("Zapfprofilgenerator", daten.TitelZusatz);
        }

        [Fact]
        public void Der_Bestandsweg_des_Ergebnisdialogs_bleibt_ohne_Zirkulationsposten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string profil = Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).First().szBezeichner;
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT, new List<string> { profil });
            Assert.True(v.Erfolgreich);
            Assert.False(v.Zapfprofilweg);
            Assert.Equal("", ZapfprofilHuelle.Leistenmeldung(v));

            var daten = (BedarfErgebnisDaten)BedarfErgebnisHuelle.Gaben(v.Waerme, true, 2, profil)["Daten"];
            Assert.DoesNotContain(daten.Kennzahlen, k => k.Bezeichnung == "davon Zirkulation:");
        }

        /// <summary>
        /// 5.2: Zuordnungen und Zapfprofil in EINEM Vorgang — beide stehen danach, und der Behälter
        /// gilt wieder als unverändert.
        /// </summary>
        [Fact]
        public void Zuordnungen_und_Zapfprofil_stehen_nach_einem_Vorgang()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjektBrauchwasserModel> liste = Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT);
            Assert.NotEmpty(liste);
            liste[0].Summe = 12.5;

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            behaelter.Uebernehmen(new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { new ZonenStand { Name = "Zone Probe", IdNutzungsart = Nutzungsart(), Bezugsmenge = 10 } }, null));

            ZapfprofilSpeicherergebnis e = ZapfprofilHuelle.BrauchwasserSchreiben(PROJEKT, liste, behaelter);

            Assert.True(e.Erfolg, e.Meldung?.Text);
            Assert.False(behaelter.Geaendert);
            Assert.Equal(12.5, Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Single().Summe, 9);
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.Equal("Zone Probe", Assert.Single(ZapfprofilCtrl.Lies(PROJEKT).Zonen).Name);
        }

        /// <summary>
        /// Eine Ablehnung des Zapfprofils rollt den GANZEN Vorgang zurück — auch das Löschen der
        /// Zuordnungen — und kommt benannt zurück; der Behälter behält seinen Stand.
        /// </summary>
        [Fact]
        public void Eine_Ablehnung_rollt_auch_die_Zuordnungen_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Count;
            Assert.True(vorher > 0);

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            behaelter.Uebernehmen(new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { new ZonenStand { Name = "Zone X", IdNutzungsart = 987654, Bezugsmenge = 5 } }, null));

            ZapfprofilSpeicherergebnis e = ZapfprofilHuelle.BrauchwasserSchreiben(
                PROJEKT, new List<Z_ProjektBrauchwasserModel>(), behaelter);

            Assert.False(e.Erfolg);
            Assert.Equal("ZPG_SPEICHER_NUTZUNGSART_FEHLT", e.Meldung.Kennung);
            Assert.StartsWith("Das Zapfprofil wurde nicht gespeichert — ", e.Meldung.Text);
            Assert.Equal(vorher, Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Count);
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.True(behaelter.Geaendert);
        }

        /// <summary>
        /// Der Schreibweg im OK des Bedarfsprofil-Dialogs (Parameter <c>Speichern</c>): die
        /// Projektzeilen des Dialogs als Zuordnungen, leer = geschrieben; eine Ablehnung liefert
        /// den Grund in der Oberflächensprache, rollt alles zurück, und ein zweiter Versuch mit
        /// berichtigtem Stand schreibt — ohne Doppelung der Zuordnungen.
        /// </summary>
        [Fact]
        public void Der_Schreibweg_des_OK_nennt_eine_Ablehnung_und_schreibt_beim_zweiten_Versuch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<BedarfsProfilZeile> zeilen = Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT)
                .Select(m => new BedarfsProfilZeile { IdZ = m.ID_Z, IdStamm = m.ID_Brauchwasser, Name = m.szBezeichner, Summe = m.Summe })
                .ToList();
            Assert.NotEmpty(zeilen);
            zeilen[0].Summe = 7.25;

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            behaelter.Uebernehmen(new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { new ZonenStand { Name = "Zone X", IdNutzungsart = 987654, Bezugsmenge = 5 } }, null));

            string grund = ZapfprofilHuelle.Schreibweg(PROJEKT, zeilen, behaelter);

            Assert.StartsWith("Das Zapfprofil wurde nicht gespeichert — ", grund);
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.NotEqual(7.25, Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Single().Summe);
            Assert.True(behaelter.Geaendert);

            behaelter.Uebernehmen(new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { new ZonenStand { Name = "Zone X", IdNutzungsart = Nutzungsart(), Bezugsmenge = 5 } }, null));

            Assert.Equal("", ZapfprofilHuelle.Schreibweg(PROJEKT, zeilen, behaelter));
            Assert.Equal(zeilen.Count, Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Count);
            Assert.Equal(7.25, Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Single().Summe, 9);
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.False(behaelter.Geaendert);

            // Ohne Behälter (Verwaltung) schreibt derselbe Weg nur die Zuordnungen.
            Assert.Equal("", ZapfprofilHuelle.Schreibweg(PROJEKT, zeilen, null));
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));
        }

        private static int Nutzungsart()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", NUTZUNG), new DbParam("@k", VERSION)), CultureInfo.InvariantCulture);
    }
}
