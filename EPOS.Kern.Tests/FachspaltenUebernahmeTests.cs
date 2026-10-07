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
            Assert.NotEqual(q["Bezeichner"], z["Bezeichner"]);
            foreach (string spalte in AnlagenFachspalten.UebertragbareSpalten())
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

        // =============================================================================
        //  Prüfstand
        // =============================================================================

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
