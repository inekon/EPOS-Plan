using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Allgemein;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die FACHSPALTEN von <c>Tab_Energieanlagen</c> auf den Übernahmewegen, die eine neue
    /// Anlagenzeile aus einer BESTEHENDEN Quellzeile anlegen: Komponentenübernahme aus
    /// einem anderen Projekt (<see cref="KomponentenUebernahmeCtrl.Uebernehmen"/>) und das
    /// zweite und jedes weitere Stück einer vertretenen Anlage aus der Flottenstudie (dort als
    /// vollständige Kopie der Zeile)
    /// (<see cref="SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen"/>).
    ///
    /// <para><b>Befund 07.10.2026:</b> Beide Wege schreiben die Zeile über das Modell
    /// (<c>AnlagenSql.Einfuegen</c>); jede Spalte, die das Modell nicht trägt — die sechs
    /// Sondenfeldwerte, <c>WQ_TemperaturModus</c>, KWKG-, Steuer- und Quellangaben —
    /// stand danach auf NULL oder ihrer Vorgabe.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — jeder Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class FachspaltenUebernahmeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Quelle und Ziel der Komponentenübernahme (Muster <c>UebernahmeNachzugTests</c>).</summary>
        private const int QUELLE = 1043, ZIEL = 1019;
        private const string GEWERK = "Wärmepumpe";

        /// <summary>Projekt 1007 und seine Stromspeicheranlage „Vaillant“ (eigene Gerätezeile).</summary>
        private const int FLOTTENPROJEKT = 1007, SPEICHERANLAGE = 10357;

        /// <summary>
        /// Gepflegte Fachwerte: die sechs Sondenfeldwerte (Schemaschritt 195), der
        /// Temperaturmodus, die Anschlusshöhe, Steuerwahl, Hilfsenergie und ein KWKG-Schalter.
        /// </summary>
        private static readonly (string Spalte, object Wert)[] FACHWERTE =
        {
            ("WQ_Sondenabstand", 7.5), ("WQ_Bohrlochdurchmesser", 0.152),
            ("WQ_Bohrlochwiderstand", 0.11), ("WQ_Kopfueberdeckung", 1.5),
            ("WQ_Betrachtungsjahr", 25L), ("WQ_Sondenanordnung", "Reihe"),
            ("WQ_TemperaturModus", "Fest"), ("WQ_Anschlusshoehe", 2.5),
            ("Energiesteuer_Wahl", "Befreit"), ("Hilfsenergie_Anteil", 0.03),
            ("KWKG_Abwaermeabfuhr", 1L)
        };

        // =============================================================================
        //  Der Kernweg
        // =============================================================================

        /// <summary>
        /// <see cref="AnlagenFachspalten.Uebertragen"/> schreibt jede übertragbare
        /// Fachspalte der Quellzeile auf eine über das Modell angelegte Zeile — NULL
        /// eingeschlossen, ohne ID, Projekt, Bezeichner oder Gerät anzufassen.
        /// </summary>
        [Fact]
        public void Der_Kernweg_uebertraegt_jede_Fachspalte_einer_Quellzeile()
        {
            if (!_db.Vorhanden) return;

            foreach ((string spalte, object wert) in FACHWERTE) Setzen(SPEICHERANLAGE, spalte, wert);
            Setzen(SPEICHERANLAGE, "KWKG_Stichtag", DBNull.Value);
            List<string> spalten = AnlagenFachspalten.UebertragbareSpalten();
            Assert.DoesNotContain("ID", spalten);
            Assert.DoesNotContain("ID_Projekt", spalten);

            bool fahrplan = AnlagenfahrplanSchema.AnlagenspaltenVorhanden();
            bool freieKuehlung = FreieKuehlungSoleSchema.AnlagenspaltenVorhanden();
            int neu;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                var zeile = new WErzeugerModel
                {
                    ID_Projekt = FLOTTENPROJEKT, Bezeichner = "Kopie Kernweg",
                    ID_Type = WizardItemClass.SP_TYP, ID_SP = Convert.ToInt32(Zeile(SPEICHERANLAGE)["ID_SP"],
                                                                              CultureInfo.InvariantCulture)
                };
                (string sql, DbParam[] werte) = AnlagenSql.Einfuegen(FLOTTENPROJEKT, zeile, null, fahrplan, freieKuehlung);
                neu = v.EinfuegenUndId(sql, werte);
                Setzen(v, neu, "KWKG_Stichtag", "2026-01-01");      // muss zu NULL werden
                AnlagenFachspalten.Ausgang aus = AnlagenFachspalten.Uebertragen(v, spalten, SPEICHERANLAGE, neu);
                Assert.Equal(0, aus.Kopiert);
                Assert.Equal(0, aus.Verloren);
                v.Commit();
            }

            DataRow q = Zeile(SPEICHERANLAGE), z = Zeile(neu);
            Assert.Equal("Kopie Kernweg", z["Bezeichner"]);
            Assert.Equal(FLOTTENPROJEKT, Ganzzahl(z["ID_Projekt"]));
            Assert.True(z["KWKG_Stichtag"] == DBNull.Value, "NULL der Quelle nicht übertragen.");
            foreach (string spalte in spalten)
                Assert.True(Gleich(q[spalte], z[spalte]),
                    spalte + ": Quelle " + q[spalte] + ", Ziel " + z[spalte]);
        }

        // =============================================================================
        //  Komponentenübernahme
        // =============================================================================

        /// <summary>
        /// Jede Fachspalte der Quellanlage steht nach der Übernahme gleich auf der neuen
        /// Anlage im Ziel; ID und Projekt sind neu.
        /// </summary>
        [Fact]
        public void Die_Komponentenuebernahme_traegt_jede_Fachspalte_der_Quellanlage()
        {
            if (!_db.Vorhanden) return;

            List<int> quellen = Anlagen(QUELLE);
            Assert.NotEmpty(quellen);
            foreach (int id in quellen)
                foreach ((string spalte, object wert) in FACHWERTE) Setzen(id, spalte, wert);

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(QUELLE, ZIEL, GEWERK,
                out string fehler, out string _), fehler);

            List<int> ziele = Anlagen(ZIEL);
            Assert.Equal(quellen.Count, ziele.Count);
            List<string> fach = AnlagenFachspalten.UebertragbareSpalten();
            Assert.Contains("WQ_Sondenanordnung", fach);
            Assert.Contains("WQ_TemperaturModus", fach);

            for (int i = 0; i < quellen.Count; i++)
            {
                DataRow q = Zeile(quellen[i]), z = Zeile(ziele[i]);
                Assert.NotEqual(quellen[i], ziele[i]);
                Assert.Equal(ZIEL, Ganzzahl(z["ID_Projekt"]));
                Assert.Equal(q["Bezeichner"], z["Bezeichner"]);
                foreach (string spalte in fach)
                    Assert.True(Gleich(q[spalte], z[spalte]),
                        spalte + ": Quelle " + q[spalte] + ", Ziel " + z[spalte]);
                foreach ((string spalte, object wert) in FACHWERTE)
                    Assert.True(Gleich(wert, z[spalte]), spalte + " verloren: " + z[spalte]);
            }
        }

        /// <summary>
        /// Ein Verweis auf ein PROJEKTEIGENES Quellprofil wird über den Bezeichner auf das
        /// gleichnamige Profil des Ziels abgebildet; fehlt es dort, wird es samt Werten als
        /// Projektkopie ins Ziel übernommen (Anwenderentscheid 07.10.2026) und gemeldet — die
        /// Anlage zeigt nie auf das Profil des Quellprojekts.
        /// </summary>
        [Fact]
        public void Ein_Quellprofil_wird_abgebildet_oder_als_Projektkopie_uebernommen()
        {
            if (!_db.Vorhanden) return;

            List<int> quellen = Anlagen(QUELLE);
            Assert.True(quellen.Count >= 2, "Die Quelle braucht zwei Wärmepumpen.");

            int profilQuelleA = Profil(QUELLE, "Sole Feld A");
            int profilQuelleB = Profil(QUELLE, "Sole Feld B");
            int profilZielA = Profil(ZIEL, "Sole Feld A");
            for (int i = 0; i < 12; i++) Profilwert(profilQuelleB, i, 4.0 + i);
            Setzen(quellen[0], "WQ_ID_Quellprofil", (long)profilQuelleA);
            Setzen(quellen[1], "WQ_ID_Quellprofil", (long)profilQuelleB);
            int profileVorher = Anzahl("SELECT COUNT(*) FROM Tab_Quellprofil WHERE ID_Projekt = ?", ZIEL);

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(QUELLE, ZIEL, GEWERK,
                out string fehler, out string hinweise), fehler);

            List<int> ziele = Anlagen(ZIEL);
            // Gleichnamig vorhanden: abgebildet, nicht kopiert.
            Assert.Equal((long)profilZielA, Ganzzahl(Zeile(ziele[0])["WQ_ID_Quellprofil"]));

            // Fehlend: Projektkopie im Ziel, samt den zwölf Werten.
            int kopie = (int)Ganzzahl(Zeile(ziele[1])["WQ_ID_Quellprofil"]);
            Assert.NotEqual(profilQuelleB, kopie);
            Assert.Equal(ZIEL, Anzahl("SELECT ID_Projekt FROM Tab_Quellprofil WHERE ID = ?", kopie));
            Assert.Equal("Sole Feld B", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Quellprofil WHERE ID = ?", new DbParam("@id", kopie)),
                CultureInfo.InvariantCulture));
            Assert.Equal(12, Anzahl("SELECT COUNT(*) FROM Tab_QuellprofilDaten WHERE ID_Quellprofil = ?", kopie));
            Assert.Equal(15.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Wert FROM Tab_QuellprofilDaten WHERE ID_Quellprofil = ? AND [Index] = 11",
                new DbParam("@id", kopie)), CultureInfo.InvariantCulture), 9);
            Assert.Equal(12, Anzahl("SELECT COUNT(*) FROM Tab_QuellprofilDaten WHERE ID_Quellprofil = ?", profilQuelleB));
            Assert.Equal(profileVorher + 1, Anzahl("SELECT COUNT(*) FROM Tab_Quellprofil WHERE ID_Projekt = ?", ZIEL));

            Assert.Contains(string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.BK_KOMP_HINW_PROJEKTKOPIE, 1), hinweise);
            Assert.DoesNotContain(string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.BK_KOMP_HINW_PROJEKTBEZUG, 1), hinweise);
        }

        /// <summary>
        /// Dasselbe Muster für die Kältemaschine: Fehlt ihre Projektkopie im Ziel, kommt sie
        /// samt Kennlinie (<c>Tab_Kenndaten_Kaeltemaschine</c>) mit; zwei Anlagen mit
        /// demselben Verweis teilen sich EINE Kopie.
        /// </summary>
        [Fact]
        public void Eine_fehlende_Kaeltemaschine_kommt_samt_Kennlinie_einmal_ins_Ziel()
        {
            if (!_db.Vorhanden) return;

            List<int> quellen = Anlagen(QUELLE);
            Assert.True(quellen.Count >= 2, "Die Quelle braucht zwei Wärmepumpen.");

            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Kaeltemaschine (ID_Projekt, Bezeichner, Nennkaelteleistung_kW, Nenn_EER) VALUES (?, 'KM Probe', 50.0, 3.5)",
                new DbParam("@p", QUELLE)));
            int km = Anzahl("SELECT MAX(ID) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", QUELLE);
            foreach ((double rk, double kw) in new[] { (27.0, 6.0), (32.0, 6.0), (32.0, 12.0) })
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kenndaten_Kaeltemaschine (ID_Projekt, ID_Kaeltemaschine, Rueckkuehltemperatur, Kaltwassertemperatur, EER) VALUES (?, ?, ?, ?, 3.0)",
                    new DbParam("@p", QUELLE), new DbParam("@k", km), new DbParam("@r", rk), new DbParam("@w", kw)));
            Setzen(quellen[0], "ID_Kaeltemaschine", (long)km);
            Setzen(quellen[1], "ID_Kaeltemaschine", (long)km);

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(QUELLE, ZIEL, GEWERK,
                out string fehler, out string hinweise), fehler);

            List<int> ziele = Anlagen(ZIEL);
            int kopie = (int)Ganzzahl(Zeile(ziele[0])["ID_Kaeltemaschine"]);
            Assert.NotEqual(km, kopie);
            Assert.Equal((long)kopie, Ganzzahl(Zeile(ziele[1])["ID_Kaeltemaschine"]));
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ? AND Bezeichner = 'KM Probe'", ZIEL));
            Assert.Equal(3, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ? AND ID_Projekt = ?",
                                   kopie, ZIEL));
            Assert.Contains(string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.BK_KOMP_HINW_PROJEKTKOPIE, 1), hinweise);
        }

        // =============================================================================
        //  Flottenstudie
        // =============================================================================

        /// <summary>
        /// Zwei Stück einer Einheit, die eine Projektanlage vertritt: das erste schreibt in
        /// die Anlage zurück, das zweite entsteht neu — als vollständige Kopie der Anlagenzeile
        /// mit eigener ID, eigenem Gerät und eigenem Bezeichner.
        /// </summary>
        [Fact]
        public void Das_zweite_Stueck_einer_vertretenen_Anlage_ist_eine_vollstaendige_Kopie_ihrer_Zeile()
        {
            if (!_db.Vorhanden) return;

            foreach ((string spalte, object wert) in FACHWERTE) Setzen(SPEICHERANLAGE, spalte, wert);
            Setzen(SPEICHERANLAGE, "Prioritaet", 7L);
            Setzen(SPEICHERANLAGE, "BM_Typ", "Probe");

            var einheit = new FlottenEinheit
            {
                Id = "e1", Name = "Speicher", AnlageId = SPEICHERANLAGE.ToString(CultureInfo.InvariantCulture),
                KapazitaetKWh = 20.0, LadeleistungKw = 10.0, EntladeleistungKw = 10.0,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            };
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { einheit }, new[] { 2 });
            Assert.True(e.Erfolg, e.Meldung);

            FlottenUebernahmeAnlage neu = Assert.Single(e.Anlagen, a => a.Neu);
            DataRow q = Zeile(SPEICHERANLAGE), z = Zeile(neu.AnlageId);
            Assert.NotEqual(SPEICHERANLAGE, neu.AnlageId);
            Assert.Equal(FLOTTENPROJEKT, Ganzzahl(z["ID_Projekt"]));
            Assert.Equal((long)neu.GeraeteId, Ganzzahl(z["ID_SP"]));
            Assert.NotEqual(Ganzzahl(q["ID_SP"]), Ganzzahl(z["ID_SP"]));
            Assert.Equal("Speicher 2", z["Bezeichner"]);

            // Vollständige Kopie (Anwenderentscheid 07.10.2026): jede Spalte ausser ID,
            // Projekt, Bezeichner und ID_SP - Modell- wie Fachspalten.
            List<string> kopie = AnlagenFachspalten.KopieSpalten("ID_SP");
            Assert.Contains("Prioritaet", kopie);
            Assert.Contains("WQ_Sondenanordnung", kopie);
            Assert.DoesNotContain("Bezeichner", kopie);
            Assert.DoesNotContain("ID_SP", kopie);
            foreach (string spalte in kopie)
                Assert.True(Gleich(q[spalte], z[spalte]),
                    spalte + ": Quelle " + q[spalte] + ", Ziel " + z[spalte]);
        }

        /// <summary>
        /// Eine freie Einheit (aus dem Katalog, ohne Anlagenzeile) hat nichts zu retten:
        /// die neue Zeile trägt die Vorgaben.
        /// </summary>
        [Fact]
        public void Eine_freie_Einheit_traegt_die_Vorgaben()
        {
            if (!_db.Vorhanden) return;

            foreach ((string spalte, object wert) in FACHWERTE) Setzen(SPEICHERANLAGE, spalte, wert);

            var einheit = new FlottenEinheit
            {
                Id = "e1", Name = "Frei", AnlageId = null,
                KapazitaetKWh = 20.0, LadeleistungKw = 10.0, EntladeleistungKw = 10.0,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            };
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { einheit });
            Assert.True(e.Erfolg, e.Meldung);

            DataRow z = Zeile(Assert.Single(e.Anlagen).AnlageId);
            Assert.True(z["WQ_Sondenabstand"] == DBNull.Value);
            Assert.True(z["WQ_Sondenanordnung"] == DBNull.Value);
            Assert.Equal(0L, Ganzzahl(z["KWKG_Abwaermeabfuhr"]));
        }

        /// <summary>
        /// Anwenderentscheid 07.10.2026: Das weitere Stück bekommt auch die ZUGEHÖRIGEN Zeilen
        /// der vertretenen Anlage (<see cref="AnlagenFachspalten.ANLAGENKINDER"/>) — Betriebs-
        /// führung, Senke, Parallelverbund (auf die kopierte Senke umgeschlüsselt), Strang und
        /// Sperrfenster — mit neuen IDs; die Quelle bleibt unverändert.
        /// </summary>
        [Fact]
        public void Das_zweite_Stueck_traegt_die_Kindzeilen_der_vertretenen_Anlage()
        {
            if (!_db.Vorhanden) return;

            KindzeilenAnlegen();
            var vorher = AnlagenkinderStand(SPEICHERANLAGE);
            List<string> quellIds = KindIds(SPEICHERANLAGE);

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit() }, new[] { 2 });
            Assert.True(e.Erfolg, e.Meldung);
            int neu = Assert.Single(e.Anlagen, a => a.Neu).AnlageId;

            // Dieselben Werte (ohne ID und Anlagenverweis), die Quelle unverändert.
            var kopie = AnlagenkinderStand(neu);
            Assert.Equal(vorher.Keys, kopie.Keys);
            foreach (string tabelle in vorher.Keys)
            {
                Assert.True(vorher[tabelle].Count > 0, tabelle + " ohne Probezeile");
                Assert.Equal(vorher[tabelle], kopie[tabelle]);
            }
            var danach = AnlagenkinderStand(SPEICHERANLAGE);
            foreach (string tabelle in vorher.Keys) Assert.Equal(vorher[tabelle], danach[tabelle]);
            Assert.Equal(quellIds, KindIds(SPEICHERANLAGE));
            Assert.Empty(KindIds(neu).Intersect(quellIds));

            // Der Verbund zeigt auf die KOPIERTE Senke, der Puffer bleibt geteilt.
            long senkeNeu = Ganzzahl(DataRepository.ExecuteScalar(
                "SELECT ID FROM Z_AnlageSenke WHERE ID_Anlage = ?", new DbParam("@a", neu)));
            Assert.Equal(senkeNeu, Ganzzahl(DataRepository.ExecuteScalar(
                "SELECT ID_Senke FROM Z_AnlagePufferVerbund WHERE ID_Anlage = ?", new DbParam("@a", neu))));
            Assert.Equal(PUFFER, Ganzzahl(DataRepository.ExecuteScalar(
                "SELECT ID_Puffer FROM Z_AnlageSenke WHERE ID = ?", new DbParam("@s", senkeNeu))));
        }

        /// <summary>
        /// Scheitert ein Kindzeilen-INSERT, rollt die ganze Übernahme zurück: keine neue
        /// Anlage, kein neues Gerät, keine halbe Kindzeile, das erste Stück unverändert.
        /// </summary>
        [Fact]
        public void Scheitert_eine_Kindzeile_rollt_die_ganze_Uebernahme_zurueck()
        {
            if (!_db.Vorhanden) return;

            KindzeilenAnlegen();
            int anlagen = Anzahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", FLOTTENPROJEKT);
            int geraete = Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher WHERE ID_Projekt = ?", FLOTTENPROJEKT);
            int senken = Anzahl("SELECT COUNT(*) FROM Z_AnlageSenke WHERE ID_Anlage > ?", 0);
            string geraet = Geraetetext(SPEICHERANLAGE);

            // Das Sperrfenster der Kopie scheitert (die Senke davor ist schon geschrieben).
            Assert.True(DataRepository.ExecuteSQL(
                "CREATE TRIGGER Probe_Sperrfenster BEFORE INSERT ON Tab_Sperrfenster " +
                "WHEN NEW.ID_Energieanlage <> " + SPEICHERANLAGE.ToString(CultureInfo.InvariantCulture) +
                " BEGIN SELECT RAISE(ABORT, 'Probe'); END"));

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit() }, new[] { 2 });

            Assert.False(e.Erfolg);
            Assert.Equal(anlagen, Anzahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", FLOTTENPROJEKT));
            Assert.Equal(geraete, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher WHERE ID_Projekt = ?", FLOTTENPROJEKT));
            Assert.Equal(senken, Anzahl("SELECT COUNT(*) FROM Z_AnlageSenke WHERE ID_Anlage > ?", 0));
            Assert.Equal(geraet, Geraetetext(SPEICHERANLAGE));
        }

        // =============================================================================
        //  Kostenpositionen nach Kapazität (Anwenderentscheid 07.10.2026)
        // =============================================================================

        /// <summary>
        /// Beide Stücke tragen die Kostenpositionen nach der neuen Kapazität: Beträge und
        /// Menge mit Faktor neue ÷ alte nutzbare Kapazität (hier 15 ÷ 20 = 0,75, gleiches
        /// SoC-Band) — das erste Stück (die vertretene Anlage) an Ort und Stelle, das zweite
        /// als Kopie davon; Satz und Nutzungsdauer bleiben, der Geräteanker zeigt auf das
        /// neue Gerät, der Hinweis nennt den Faktor und beide Stücke.
        /// </summary>
        [Fact]
        public void Kostenpositionen_folgen_der_Kapazitaet_an_beiden_Stuecken()
        {
            if (!_db.Vorhanden) return;

            KostenquelleAnlegen(20.0);

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(15.0) }, new[] { 2 });
            Assert.True(e.Erfolg, e.Meldung);
            FlottenUebernahmeAnlage neu = Assert.Single(e.Anlagen, a => a.Neu);
            FlottenUebernahmeAnlage erstes = Assert.Single(e.Anlagen, a => !a.Neu);

            Assert.Equal(KostenText(SPEICHERANLAGE, ohneAnker: true), KostenText(neu.AnlageId, ohneAnker: true));
            Assert.Equal(750.0, Zahl(Kosten(SPEICHERANLAGE).Rows[0]["EingegebenerWert"]), 9);
            DataTable k = Kosten(neu.AnlageId);
            Assert.Equal(2, k.Rows.Count);
            DataRow betrag = k.Rows[0], prozent = k.Rows[1];
            Assert.Equal(750.0, Zahl(betrag["EingegebenerWert"]), 9);
            Assert.Equal(900.0, Zahl(betrag["Worstcase"]), 9);
            Assert.Equal(675.0, Zahl(betrag["Bestcase"]), 9);
            Assert.Equal(15.0, Zahl(betrag["Nutzungsdauer"]), 9);
            Assert.Equal(3000.0, Zahl(prozent["Menge"]), 9);
            Assert.Equal(2.0, Zahl(prozent["Einheitpreis"]), 9);
            Assert.Equal(60.0, Zahl(prozent["EingegebenerWert"]), 9);
            Assert.Equal(0.0, Zahl(prozent["Worstcase"]), 9);   // Vorgabe 0 bleibt 0
            Assert.Equal("PROZENT_INVESTITION", Convert.ToString(prozent["Bemessung"], CultureInfo.InvariantCulture));
            foreach (DataRow r in k.Rows)
            {
                Assert.Equal((long)neu.GeraeteId, Ganzzahl(r["ID_AnlageGeraet"]));
                Assert.Equal((long)FLOTTENPROJEKT, Ganzzahl(r["ProjektID"]));
            }

            string hinweis = Assert.Single(e.Hinweise);
            Assert.Contains(0.75.ToString("0.00", CultureInfo.CurrentCulture), hinweis);
            Assert.Contains(neu.Bezeichner, hinweis);
            Assert.Contains(erstes.Bezeichner, hinweis);
        }

        /// <summary>
        /// Das erste Stück allein: Schreibt die Übernahme eine neue Kapazität zurück
        /// (20 → 30 kWh, Faktor 1,5), wachsen die eigenen Kostenpositionen der Anlage mit;
        /// Satz und Nutzungsdauer bleiben, der Hinweis nennt den Faktor.
        /// </summary>
        [Fact]
        public void Das_erste_Stueck_skaliert_seine_Kostenpositionen_bei_neuer_Kapazitaet()
        {
            if (!_db.Vorhanden) return;

            KostenquelleAnlegen(20.0);
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(30.0) });
            Assert.True(e.Erfolg, e.Meldung);
            FlottenUebernahmeAnlage erstes = Assert.Single(e.Anlagen);
            Assert.False(erstes.Neu);

            DataTable k = Kosten(SPEICHERANLAGE);
            Assert.Equal(2, k.Rows.Count);
            DataRow betrag = k.Rows[0], prozent = k.Rows[1];
            Assert.Equal(1500.0, Zahl(betrag["EingegebenerWert"]), 9);
            Assert.Equal(1800.0, Zahl(betrag["Worstcase"]), 9);
            Assert.Equal(1350.0, Zahl(betrag["Bestcase"]), 9);
            Assert.Equal(15.0, Zahl(betrag["Nutzungsdauer"]), 9);
            Assert.Equal(6000.0, Zahl(prozent["Menge"]), 9);
            Assert.Equal(2.0, Zahl(prozent["Einheitpreis"]), 9);
            Assert.Equal(120.0, Zahl(prozent["EingegebenerWert"]), 9);

            string hinweis = Assert.Single(e.Hinweise);
            Assert.Contains(1.5.ToString("0.00", CultureInfo.CurrentCulture), hinweis);
            Assert.Contains(erstes.Bezeichner, hinweis);
        }

        /// <summary>
        /// Im selben Lauf: Die Betragsart wächst mit der Kapazität, die Prozentart mit
        /// Energiekostenbezug (% des Endenergiebedarfs) bleibt unverändert — an der
        /// vertretenen Anlage wie an der Kopie.
        /// </summary>
        [Fact]
        public void Prozentart_mit_Energiekostenbezug_bleibt_Betragsart_wird_skaliert()
        {
            if (!_db.Vorhanden) return;

            KostenquelleAnlegen(20.0, mitEnergiekostenart: true);
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(10.0) }, new[] { 2 });
            Assert.True(e.Erfolg, e.Meldung);
            FlottenUebernahmeAnlage neu = Assert.Single(e.Anlagen, a => a.Neu);

            foreach (int anlage in new[] { SPEICHERANLAGE, neu.AnlageId })
            {
                DataTable k = Kosten(anlage);
                Assert.Equal(3, k.Rows.Count);
                Assert.Equal(500.0, Zahl(k.Rows[0]["EingegebenerWert"]), 9);       // BETRAG × 0,5
                DataRow energie = k.Rows[2];
                Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                             Convert.ToString(energie["Bemessung"], CultureInfo.InvariantCulture));
                Assert.Equal(150.0, Zahl(energie["EingegebenerWert"]), 9);
                Assert.Equal(5000.0, Zahl(energie["Menge"]), 9);
                Assert.Equal(3.0, Zahl(energie["Einheitpreis"]), 9);
                Assert.Equal(160.0, Zahl(energie["Worstcase"]), 9);
            }
            Assert.Contains(0.5.ToString("0.00", CultureInfo.CurrentCulture), Assert.Single(e.Hinweise));
        }

        /// <summary>Der Katalog nennt genau die vier Arten mit Energiekostenbezug als nicht skalierbar.</summary>
        [Fact]
        public void Nur_Arten_mit_Energiekostenbezug_skalieren_nicht()
        {
            List<string> nicht = BemessungKatalog.Alle.Select(i => i.Persistenz)
                .Where(p => !BemessungKatalog.NachKapazitaetSkalierbar(p))
                .OrderBy(p => p, StringComparer.Ordinal).ToList();
            List<string> erwartet = new[]
            {
                DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN
            }.OrderBy(p => p, StringComparer.Ordinal).ToList();
            Assert.Equal(erwartet, nicht);
            Assert.True(BemessungKatalog.NachKapazitaetSkalierbar(null));
            Assert.True(BemessungKatalog.NachKapazitaetSkalierbar(DbWerte.BEMESSUNG_PROZENT_INVESTITION));
        }

        /// <summary>Gleiche Kapazität: Faktor 1, die Kopie trägt dieselben Werte, kein Hinweis.</summary>
        [Fact]
        public void Gleiche_Kapazitaet_kopiert_die_Kostenpositionen_unveraendert()
        {
            if (!_db.Vorhanden) return;

            KostenquelleAnlegen(20.0);
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(20.0) }, new[] { 3 });
            Assert.True(e.Erfolg, e.Meldung);

            string quelle = KostenText(SPEICHERANLAGE, ohneAnker: true);
            List<FlottenUebernahmeAnlage> neue = e.Anlagen.Where(a => a.Neu).ToList();
            Assert.Equal(2, neue.Count);
            foreach (FlottenUebernahmeAnlage a in neue)
                Assert.Equal(quelle, KostenText(a.AnlageId, ohneAnker: true));
            Assert.Equal(1000.0, Zahl(Kosten(SPEICHERANLAGE).Rows[0]["EingegebenerWert"]), 9);
            // Faktor 1: still.
            Assert.Empty(e.Hinweise);
        }

        /// <summary>
        /// Fehlt die Kapazität der vertretenen Anlage, gehen die Positionen unverändert mit
        /// und der Hinweis sagt, dass kein Faktor gebildet wurde.
        /// </summary>
        [Fact]
        public void Ohne_Kapazitaet_unveraendert_kopiert_mit_Hinweis()
        {
            if (!_db.Vorhanden) return;

            KostenquelleAnlegen(0.0);
            string quelle = KostenText(SPEICHERANLAGE, ohneAnker: true);
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(15.0) }, new[] { 2 });
            Assert.True(e.Erfolg, e.Meldung);

            Assert.Equal(quelle, KostenText(Assert.Single(e.Anlagen, a => a.Neu).AnlageId, ohneAnker: true));
            Assert.Equal(quelle, KostenText(SPEICHERANLAGE, ohneAnker: true));
            string erwartet = string.Format(CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.FLOTTE_UEBERNAHME_HINW_KOSTEN_OHNE_KAPAZITAET, "§", "¶");
            Assert.StartsWith(erwartet.Substring(0, erwartet.IndexOf('§')), Assert.Single(e.Hinweise));
        }

        /// <summary>Eine Anlage ohne Kostenpositionen: kein Hinweis.</summary>
        [Fact]
        public void Ohne_Kostenpositionen_kein_Hinweis()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ?", SPEICHERANLAGE));
            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(15.0) }, new[] { 2 });
            Assert.True(e.Erfolg, e.Meldung);
            Assert.Empty(e.Hinweise);
        }

        /// <summary>
        /// Scheitert die Kostenkopie, rollt die ganze Übernahme zurück: keine neue Anlage,
        /// keine neue Kostenposition, Quelle und Gerät unverändert.
        /// </summary>
        [Fact]
        public void Scheitert_die_Kostenkopie_rollt_die_ganze_Uebernahme_zurueck()
        {
            if (!_db.Vorhanden) return;

            KostenquelleAnlegen(20.0);
            int anlagen = Anzahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", FLOTTENPROJEKT);
            int positionen = Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", FLOTTENPROJEKT);
            string quelle = KostenText(SPEICHERANLAGE);
            string geraet = Geraetetext(SPEICHERANLAGE);
            Assert.True(DataRepository.ExecuteSQL(
                "CREATE TRIGGER Probe_Kosten BEFORE INSERT ON Tab_ProjektWerte " +
                "WHEN NEW.ID_Anlage <> " + SPEICHERANLAGE.ToString(CultureInfo.InvariantCulture) +
                " BEGIN SELECT RAISE(ABORT, 'Probe'); END"));

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                FLOTTENPROJEKT, new[] { Einheit(15.0) }, new[] { 2 });

            Assert.False(e.Erfolg);
            Assert.Empty(e.Hinweise);
            Assert.Equal(anlagen, Anzahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", FLOTTENPROJEKT));
            Assert.Equal(positionen, Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", FLOTTENPROJEKT));
            Assert.Equal(quelle, KostenText(SPEICHERANLAGE));
            Assert.Equal(geraet, Geraetetext(SPEICHERANLAGE));
        }

        /// <summary>
        /// Gibt der Speicheranlage die Kapazität <paramref name="energie"/> (0 = keine, NULL),
        /// das Band 10/90 % und zwei Kostenpositionen: einen festen Betrag (1.000 €, Band
        /// 900/1.200 €, 15 a) und „% der Investition" (Menge 4.000 €, Satz 2 %, erfasst 80 €).
        /// </summary>
        private static void KostenquelleAnlegen(double energie, bool mitEnergiekostenart = false)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromspeicher SET Energie = ? WHERE ID = (SELECT ID_SP FROM Tab_Energieanlagen WHERE ID = ?)",
                new DbParam("@e", energie > 0.0 ? energie : DBNull.Value), new DbParam("@a", SPEICHERANLAGE)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_StromspeicherVariante SET SoC_Min_Prozent = 10, SoC_Max_Prozent = 90 WHERE ID_Energieanlage = ?",
                new DbParam("@a", SPEICHERANLAGE)));
            long geraet = Ganzzahl(DataRepository.ExecuteScalar(
                "SELECT ID_SP FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@a", SPEICHERANLAGE)));
            object komponente = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?", new DbParam("@k", DbWerte.ERZEUGER_STROMSPEICHER));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, KomponentenID, KategorieID, EingegebenerWert, Worstcase, " +
                "Bestcase, Nutzungsdauer, Kostenart, Bemessung, ID_Anlage, ID_AnlageGeraet) " +
                "VALUES (?, ?, 1, 1000, 1200, 900, 15, 'KAPITALGEBUNDEN', 'BETRAG', ?, ?)",
                new DbParam("@p", FLOTTENPROJEKT), new DbParam("@k", komponente ?? DBNull.Value),
                new DbParam("@a", SPEICHERANLAGE), new DbParam("@g", geraet)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, KomponentenID, KategorieID, EingegebenerWert, Nutzungsdauer, " +
                "Kostenart, Bemessung, Menge, Einheitpreis, ID_Anlage, ID_AnlageGeraet) " +
                "VALUES (?, ?, 1, 80, 1, 'BETRIEBSGEBUNDEN', 'PROZENT_INVESTITION', 4000, 2, ?, ?)",
                new DbParam("@p", FLOTTENPROJEKT), new DbParam("@k", komponente ?? DBNull.Value),
                new DbParam("@a", SPEICHERANLAGE), new DbParam("@g", geraet)));
            if (!mitEnergiekostenart) return;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, KomponentenID, KategorieID, EingegebenerWert, Worstcase, " +
                "Nutzungsdauer, Kostenart, Bemessung, Menge, Einheitpreis, ID_Anlage, ID_AnlageGeraet) " +
                "VALUES (?, ?, 1, 150, 160, 1, 'BETRIEBSGEBUNDEN', ?, 5000, 3, ?, ?)",
                new DbParam("@p", FLOTTENPROJEKT), new DbParam("@k", komponente ?? DBNull.Value),
                new DbParam("@b", DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF),
                new DbParam("@a", SPEICHERANLAGE), new DbParam("@g", geraet)));
        }

        private static DataTable Kosten(int anlage) => DataRepository.GetDataTable(
            "SELECT * FROM Tab_ProjektWerte WHERE ID_Anlage = ? ORDER BY ID", new DbParam("@a", anlage));

        /// <summary>Die Kostenpositionen der Anlage als Text — ohne ID und Anlagenverweis,
        /// auf Wunsch ohne Geräteanker.</summary>
        private static string KostenText(int anlage, bool ohneAnker = false)
        {
            DataTable dt = Kosten(anlage);
            return string.Join("\n", dt.Rows.Cast<DataRow>().Select(r => string.Join("|",
                dt.Columns.Cast<DataColumn>()
                  .Where(c => c.ColumnName != "ID" && c.ColumnName != "ID_Anlage" &&
                              !(ohneAnker && c.ColumnName == "ID_AnlageGeraet"))
                  .Select(c => c.ColumnName + "=" + Convert.ToString(r[c], CultureInfo.InvariantCulture)))));
        }

        private static double Zahl(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);


        // =============================================================================
        //  Prüfstand
        // =============================================================================

        /// <summary>Ein Puffer des Flottenprojekts (Senke der Probe).</summary>
        private const long PUFFER = 1007007;

        private static FlottenEinheit Einheit(double kapazitaet = 20.0) => new FlottenEinheit
        {
            Id = "e1", Name = "Speicher", AnlageId = SPEICHERANLAGE.ToString(CultureInfo.InvariantCulture),
            KapazitaetKWh = kapazitaet, LadeleistungKw = 10.0, EntladeleistungKw = 10.0,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };

        /// <summary>
        /// Gibt der Speicheranlage je Kindtabelle mindestens eine gepflegte Zeile: die
        /// vorhandene Betriebsführung mit eigenen Werten, eine Senke, einen Verbund an dieser
        /// Senke, einen Strang und ein Sperrfenster.
        /// </summary>
        private static void KindzeilenAnlegen()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_StromspeicherVariante SET Betriebsart = ?, SoC_Min_Prozent = ?, Netzentladung = 1 " +
                "WHERE ID_Energieanlage = ?",
                new DbParam("@b", "Probe"), new DbParam("@s", 12.5), new DbParam("@a", SPEICHERANLAGE)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Z_AnlageSenke (ID_Anlage, Rang, Ziel, Bedarfsart, ID_Puffer, Ladeprio, Ladegrenze) " +
                "VALUES (?, 1, 'Puffer', 'Heizung', ?, 3, 0.8)",
                new DbParam("@a", SPEICHERANLAGE), new DbParam("@p", PUFFER)));
            long senke = Ganzzahl(DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM Z_AnlageSenke WHERE ID_Anlage = ?", new DbParam("@a", SPEICHERANLAGE)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Z_AnlagePufferVerbund (ID_Anlage, ID_Puffer, ID_Senke) VALUES (?, ?, ?)",
                new DbParam("@a", SPEICHERANLAGE), new DbParam("@p", PUFFER), new DbParam("@s", senke)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Z_AnlageStrang (ID_Anlage, Rang, Bezeichner, Module_Reihe, Neigung, Azimut) " +
                "VALUES (?, 1, 'Dach Süd', 8, 30, 0)", new DbParam("@a", SPEICHERANLAGE)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h, Wochentage, Heizstab_gesperrt, Reihenfolge) " +
                "VALUES (?, 11.5, 2, 31, 0, 1)", new DbParam("@a", SPEICHERANLAGE)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, KategorieID, EingegebenerWert, Kostenart, Bemessung, ID_Anlage) " +
                "VALUES (?, 1, 500, 'KAPITALGEBUNDEN', 'BETRAG', ?)",
                new DbParam("@p", FLOTTENPROJEKT), new DbParam("@a", SPEICHERANLAGE)));
        }

        /// <summary>
        /// Je Kindtabelle die Zeilen der Anlage als Text — ohne ID, ohne Anlagenverweis, ohne
        /// umgeschlüsselte, skalierte und Ankerspalten (die prüfen eigene Fälle).
        /// </summary>
        private static SortedDictionary<string, List<string>> AnlagenkinderStand(int anlage)
        {
            var stand = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            foreach ((string tabelle, string fk) in AnlagenFachspalten.ANLAGENKINDER)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + tabelle + "] WHERE [" + fk + "] = ? ORDER BY ID", new DbParam("@a", anlage));
                var zeilen = new List<string>();
                foreach (DataRow r in dt.Rows)
                    zeilen.Add(string.Join("|", dt.Columns.Cast<DataColumn>()
                        .Where(c => c.ColumnName != "ID" && c.ColumnName != fk &&
                                    !AnlagenFachspalten.ANLAGENKIND_UMSCHLUESSEL.ContainsKey(tabelle + "." + c.ColumnName) &&
                                    !AnlagenFachspalten.ANLAGENKIND_SKALIERT.Contains(tabelle + "." + c.ColumnName) &&
                                    !AnlagenFachspalten.ANLAGENKIND_GERAETEANKER.Contains(tabelle + "." + c.ColumnName))
                        .Select(c => c.ColumnName + "=" + Convert.ToString(r[c], CultureInfo.InvariantCulture))));
                stand[tabelle] = zeilen;
            }
            return stand;
        }

        /// <summary>Die Gerätezeile der Anlage als Text (Probe auf das Zurückschreiben des ersten Stücks).</summary>
        private static string Geraetetext(int anlage)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT s.* FROM Tab_Stromspeicher s JOIN Tab_Energieanlagen a ON a.ID_SP = s.ID WHERE a.ID = ?",
                new DbParam("@a", anlage));
            Assert.Equal(1, dt.Rows.Count);
            return string.Join("|", dt.Rows[0].ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)));
        }

        /// <summary>Die Kindzeilen der Anlage als „Tabelle:ID“ (über alle Kindtabellen).</summary>
        private static List<string> KindIds(int anlage)
        {
            var ids = new List<string>();
            foreach ((string tabelle, string fk) in AnlagenFachspalten.ANLAGENKINDER)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID FROM [" + tabelle + "] WHERE [" + fk + "] = ? ORDER BY ID", new DbParam("@a", anlage));
                ids.AddRange(dt.Rows.Cast<DataRow>().Select(r => tabelle + ":" + Ganzzahl(r["ID"])));
            }
            return ids;
        }

        private static List<int> Anlagen(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type IN (?, ?) ORDER BY ID",
                new DbParam("@p", projekt), new DbParam("@t1", WizardItemClass.WP_TYP),
                new DbParam("@t2", WizardItemClass.REF_WP_TYP));
            return dt.Rows.Cast<DataRow>().Select(r => (int)Ganzzahl(r["ID"])).ToList();
        }

        private static DataRow Zeile(int id)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Energieanlagen WHERE ID = ?",
                                                      new DbParam("@id", id));
            Assert.Equal(1, dt.Rows.Count);
            return dt.Rows[0];
        }

        private static void Setzen(int id, string spalte, object wert)
            => Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET [" + spalte + "] = ? WHERE ID = ?",
                new DbParam("@w", wert), new DbParam("@id", id)), spalte);

        private static void Setzen(DbVorgang v, int id, string spalte, object wert)
            => v.Ausfuehren("UPDATE Tab_Energieanlagen SET [" + spalte + "] = ? WHERE ID = ?",
                            new DbParam("@w", wert), new DbParam("@id", id));

        private static int Profil(int projekt, string bezeichner)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Quellprofil (ID_Projekt, Bezeichner, Betriebsart, Einheit) VALUES (?, ?, 'Monat', '°C')",
                new DbParam("@p", projekt), new DbParam("@b", bezeichner)));
            return Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM Tab_Quellprofil WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", projekt), new DbParam("@b", bezeichner)), CultureInfo.InvariantCulture);
        }

        private static void Profilwert(int profil, int index, double wert)
            => Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_QuellprofilDaten (ID_Quellprofil, [Index], Wert) VALUES (?, ?, ?)",
                new DbParam("@p", profil), new DbParam("@i", index), new DbParam("@w", wert)));

        private static int Anzahl(string sql, params int[] werte)
            => Convert.ToInt32(DataRepository.ExecuteScalar(sql,
                   werte.Select(w => new DbParam("@w", w)).ToArray()), CultureInfo.InvariantCulture);

        private static long Ganzzahl(object o) => Convert.ToInt64(o, CultureInfo.InvariantCulture);

        private static bool Gleich(object a, object b)
        {
            bool aLeer = a == null || a == DBNull.Value, bLeer = b == null || b == DBNull.Value;
            if (aLeer || bLeer) return aLeer && bLeer;
            if (a is string || b is string)
                return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture),
                                     Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            return Math.Abs(Convert.ToDouble(a, CultureInfo.InvariantCulture) -
                            Convert.ToDouble(b, CultureInfo.InvariantCulture)) < 1e-12;
        }
    }
}
