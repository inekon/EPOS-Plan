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
    /// zweite und jedes weitere Stück einer vertretenen Anlage aus der Flottenstudie
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
                Assert.Equal(0, AnlagenFachspalten.Uebertragen(v, spalten, SPEICHERANLAGE, neu));
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
