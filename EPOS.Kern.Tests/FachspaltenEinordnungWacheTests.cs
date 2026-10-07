using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Wache über die Spalten von <c>Tab_Energieanlagen</c>: Jede Spalte ist entweder
    /// MODELLspalte (sie steht in <c>AnlagenSql.SQL_ANLAGE_INSERT</c>), eingeordnete
    /// FACHspalte (sie wird beim Speichern im Assistenten gerettet und bei Komponenten-
    /// übernahme und Flottenstudie aus der Quellzeile übertragen) oder AUSSCHLUSS
    /// (Schlüssel, Projektbezug, Ergebnis der Simulation).
    ///
    /// <para><b>Warum eine Liste neben dem Komplement.</b> Übertragen wird von selbst jede
    /// Spalte, die das Modell nicht nennt — eine neue Fachspalte kommt ohne Nacharbeit
    /// mit. Diese Wache verlangt trotzdem, dass sie hier eingeordnet wird: Eine neue
    /// Ergebnisspalte darf nicht mitwandern, und ein neuer Verweis auf eine projekteigene
    /// Zeile braucht seinen Eintrag in <see cref="AnlagenFachspalten.PROJEKTBEZUG"/>, sonst
    /// zeigte die Kopie in ein fremdes Projekt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class FachspaltenEinordnungWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>
        /// Die eingeordneten Fachspalten: KWKG (Schritte 22/61/105), Quellangaben,
        /// Steuer und Aufteilung, Kältemaschine (KU3), Sondenfeld (Schritt 195).
        /// </summary>
        private static readonly string[] FACHSPALTEN =
        {
            "KWKG_Stichtag", "KWKG_Inbetriebnahme", "KWKG_Anlagenart", "KWKG_Eigenstromfall",
            "KWKG_Satz_Einspeisung", "KWKG_Satz_Eigen", "KWKG_Vbh_Kontingent", "KWKG_Vbh_Jahresdeckel",
            "KWKG_Kostenanteil", "KWKG_Abwaermeabfuhr", "KWKG_Stromkennzahl",
            "WQ_Anschlusshoehe", "WQ_ID_Quellprofil", "WQ_TemperaturModus",
            "Energiesteuer_Wahl", "Aufteilung_Methode", "Hilfsenergie_Anteil",
            "ID_Kaeltemaschine", "Kaeltemaschine_Anzahl",
            "WQ_Sondenabstand", "WQ_Bohrlochdurchmesser", "WQ_Bohrlochwiderstand",
            "WQ_Kopfueberdeckung", "WQ_Betrachtungsjahr", "WQ_Sondenanordnung"
        };

        /// <summary>
        /// Die eingeordneten MODELLspalten — die Spalten der vollständigen Einfügeanweisung.
        /// Eine vollständige KOPIE der Zeile (Flottenstudie) überträgt sie mit; eine neue
        /// Modellspalte muss deshalb hier ebenso eingeordnet werden (Kopie oder
        /// <see cref="AnlagenFachspalten.KOPIE_AUSSCHLUSS"/>/<see cref="AnlagenFachspalten.GERAETEVERWEISE"/>).
        /// </summary>
        private static readonly string[] MODELLSPALTEN =
        {
            "ID_Projekt", "Bezeichner", "ID_Type", "ID_WP", "Betriebsart", "Sperrung", "Sperrzeit_von",
            "Sperrzeit_bis", "Vorlauf", "Rücklauf", "Bivalenter_Betrieb", "Abschaltpunkt", "Nutzungszeit",
            "ID_SP", "ID_PV", "ID_Solar", "Heizstab", "Volumen", "rendeMix", "Solaranteil", "ID_Kessel",
            "ID_BHKW", "Grenzleistung", "Kollektormodulanzahl", "PV_Leistung", "Neigung", "Azimut",
            "ID_PUFFER", "Prioritaet", "WQ_Typ", "WQ_Temp", "WQ_Monatswerte", "WQ_CSV", "WQ_Wochenwerte",
            "WQ_Puffer", "WQ_Spreizung", "WQ_Regeneration", "WQ_Unbegrenzt", "WS_Typ", "BM_Typ",
            "ID_Carrier", "WQ_Tiefe", "WQ_Flaeche", "WQ_Anzahl", "WQ_Bodentyp", "WQ_Quellsystem", "WS_Ziel",
            "WS_ID_Puffer", "WS_Ladeprio", "WS_Ladegrenze", "WS_Ladeprio_PV", "WS_Ziel2", "WS_ID_Puffer2",
            "WS_Ladeprio2", "WS_Ladegrenze2", "WQ_ID_Puffer", "PV_WrWirkungsgrad", "PV_Systemverluste",
            "PV_Modell", "PV_WrNennleistungKw", "PV_WrEta10", "PV_WrEta50", "PV_WrEta100",
            "PV_Wechselrichterweg", "Kuehl_ID_Carrier", "Kuehl_EigenerZaehler", "Albedo", "Pumpenleistung_W",
            "Solarkreisverluste_Prozent", "Uebertrager_Graedigkeit_K", "Kollektor_Spreizung_K",
            "Arbeitstemperatur_Weg", "Zeitprogramm", "Vorlauf_Max", "Kuehl_Frei", "Kuehl_Frei_Graedigkeit_K",
            "Kuehl_Frei_Leistung_kW"
        };

        /// <summary>Was eine Kopie nie überträgt: der eigene Bezeichner.</summary>
        private static readonly string[] KOPIE_AUSSCHLUSS = { "Bezeichner" };

        /// <summary>Die Geräteverweise — den eigenen setzt die Kopie neu.</summary>
        private static readonly string[] GERAETEVERWEISE =
            { "ID_WP", "ID_SP", "ID_PV", "ID_Solar", "ID_Kessel", "ID_BHKW", "ID_PUFFER", "ID_Kaeltemaschine" };

        private const string HINWEIS =
            " Einordnen: ins Modell (AnlagenSql.SQL_ANLAGE_INSERT), als Fachspalte (FACHSPALTEN " +
            "dieser Wache; ein Verweis auf eine projekteigene Zeile zusätzlich in " +
            "AnlagenFachspalten.PROJEKTBEZUG) oder als Ausschluss (AnlagenFachspalten.AUSSCHLUSS " +
            "bzw. ERGEBNIS für Werte, die die Simulation schreibt).";

        private static List<string> Spalten()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM pragma_table_info(?)", new DbParam("@t", "Tab_Energieanlagen"));
            return dt.Rows.Cast<DataRow>()
                     .Select(r => Convert.ToString(r["name"], CultureInfo.InvariantCulture)).ToList();
        }

        /// <summary>Jede Spalte des Schemas ist eingeordnet — genau einmal.</summary>
        [Fact]
        public void Jede_Spalte_der_Anlagentabelle_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            HashSet<string> modell = AnlagenFachspalten.Modellspalten();
            var fach = new HashSet<string>(FACHSPALTEN, StringComparer.OrdinalIgnoreCase);
            var ausschluss = new HashSet<string>(AnlagenFachspalten.AUSSCHLUSS
                .Concat(AnlagenFachspalten.ERGEBNIS), StringComparer.OrdinalIgnoreCase);

            var offen = new List<string>();
            var doppelt = new List<string>();
            foreach (string spalte in Spalten())
            {
                int n = (modell.Contains(spalte) ? 1 : 0) + (fach.Contains(spalte) ? 1 : 0) +
                        (ausschluss.Contains(spalte) && !modell.Contains(spalte) ? 1 : 0);
                if (n == 0) offen.Add(spalte);
                else if (n > 1) doppelt.Add(spalte);
            }

            Assert.True(offen.Count == 0, "Nicht eingeordnete Spalte(n): " + string.Join(", ", offen) + "." + HINWEIS);
            Assert.True(doppelt.Count == 0, "Doppelt eingeordnet: " + string.Join(", ", doppelt) + ".");
        }

        /// <summary>
        /// Die Fachspalten der Wache sind genau das, was übertragen wird — keine Spalte, die
        /// das Schema nicht mehr führt, und keine, die ins Modell gewandert ist.
        /// </summary>
        [Fact]
        public void Die_uebertragenen_Spalten_sind_genau_die_eingeordneten_Fachspalten()
        {
            if (!_db.Vorhanden) return;

            var erwartet = new HashSet<string>(FACHSPALTEN, StringComparer.OrdinalIgnoreCase);
            erwartet.ExceptWith(AnlagenFachspalten.ERGEBNIS);
            var ist = new HashSet<string>(AnlagenFachspalten.UebertragbareSpalten(), StringComparer.OrdinalIgnoreCase);

            Assert.True(erwartet.SetEquals(ist),
                "Fehlt in der Übertragung: " + string.Join(", ", erwartet.Except(ist)) +
                "; nicht eingeordnet: " + string.Join(", ", ist.Except(erwartet)) + "." + HINWEIS);
            Assert.True(new HashSet<string>(WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase)
                            .IsSupersetOf(ist), "Assistent und Übernahme retten verschiedene Spalten.");
        }

        /// <summary>
        /// Jeder Fremdschlüssel einer Fachspalte auf eine Tabelle mit <c>ID_Projekt</c> steht
        /// in <see cref="AnlagenFachspalten.PROJEKTBEZUG"/> — sonst zeigte die übernommene
        /// Anlage in das Quellprojekt.
        /// </summary>
        [Fact]
        public void Jeder_Projektverweis_einer_Fachspalte_wird_abgebildet()
        {
            if (!_db.Vorhanden) return;

            var fach = new HashSet<string>(AnlagenFachspalten.UebertragbareSpalten(), StringComparer.OrdinalIgnoreCase);
            DataTable fks = DataRepository.GetDataTable(
                "SELECT \"from\" AS Spalte, \"table\" AS Ziel FROM pragma_foreign_key_list(?)",
                new DbParam("@t", "Tab_Energieanlagen"));

            var fehlend = new List<string>();
            foreach (DataRow r in fks.Rows)
            {
                string spalte = Convert.ToString(r["Spalte"], CultureInfo.InvariantCulture);
                string ziel = Convert.ToString(r["Ziel"], CultureInfo.InvariantCulture);
                if (!fach.Contains(spalte)) continue;

                bool projekteigen = DataRepository.SpaltenVonTabelle(ziel)
                    .Any(s => string.Equals(s, "ID_Projekt", StringComparison.OrdinalIgnoreCase));
                if (!projekteigen) continue;

                if (!AnlagenFachspalten.PROJEKTBEZUG.TryGetValue(spalte, out string tabelle) ||
                    !string.Equals(tabelle, ziel, StringComparison.OrdinalIgnoreCase))
                    fehlend.Add(spalte + " -> " + ziel);
            }

            Assert.True(fehlend.Count == 0, "Projektverweis ohne Abbildung: " + string.Join(", ", fehlend) + "." + HINWEIS);
        }

        /// <summary>
        /// Die Modellspalten der Wache sind genau die der Einfügeanweisung — eine neue
        /// Modellspalte wandert in die vollständige Kopie und muss eingeordnet werden.
        /// </summary>
        [Fact]
        public void Jede_Modellspalte_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            var erwartet = new HashSet<string>(MODELLSPALTEN, StringComparer.OrdinalIgnoreCase);
            HashSet<string> ist = AnlagenFachspalten.Modellspalten();
            Assert.True(erwartet.SetEquals(ist),
                "Nicht eingeordnete Modellspalte(n): " + string.Join(", ", ist.Except(erwartet, StringComparer.OrdinalIgnoreCase)) +
                "; nicht mehr im Modell: " + string.Join(", ", erwartet.Except(ist, StringComparer.OrdinalIgnoreCase)) +
                ". Einordnen in MODELLSPALTEN, bei Bedarf zusätzlich in AnlagenFachspalten.KOPIE_AUSSCHLUSS " +
                "oder GERAETEVERWEISE.");
        }

        /// <summary>
        /// Die vollständige Kopie (Flottenstudie) ist genau: Modell- und Fachspalten ohne
        /// Projekt, Bezeichner und den neu gesetzten Geräteverweis; Ausschluss- und
        /// Geräteliste des Kernwegs sind die eingeordneten.
        /// </summary>
        [Fact]
        public void Die_vollstaendige_Kopie_ist_genau_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            Assert.True(new HashSet<string>(KOPIE_AUSSCHLUSS, StringComparer.OrdinalIgnoreCase)
                            .SetEquals(AnlagenFachspalten.KOPIE_AUSSCHLUSS), "KOPIE_AUSSCHLUSS weicht ab.");
            Assert.True(new HashSet<string>(GERAETEVERWEISE, StringComparer.OrdinalIgnoreCase)
                            .SetEquals(AnlagenFachspalten.GERAETEVERWEISE), "GERAETEVERWEISE weicht ab.");

            var schema = new HashSet<string>(Spalten(), StringComparer.OrdinalIgnoreCase);
            Assert.True(schema.IsSupersetOf(GERAETEVERWEISE), "Geräteverweis ohne Spalte.");
            Assert.True(schema.IsSupersetOf(KOPIE_AUSSCHLUSS), "Kopie-Ausschluss ohne Spalte.");

            var erwartet = new HashSet<string>(MODELLSPALTEN.Concat(FACHSPALTEN), StringComparer.OrdinalIgnoreCase);
            erwartet.ExceptWith(AnlagenFachspalten.AUSSCHLUSS);
            erwartet.ExceptWith(AnlagenFachspalten.ERGEBNIS);
            erwartet.ExceptWith(KOPIE_AUSSCHLUSS);
            erwartet.Remove("ID_SP");
            var ist = new HashSet<string>(AnlagenFachspalten.KopieSpalten("ID_SP"), StringComparer.OrdinalIgnoreCase);
            Assert.True(erwartet.SetEquals(ist),
                "Fehlt in der Kopie: " + string.Join(", ", erwartet.Except(ist)) +
                "; nicht eingeordnet: " + string.Join(", ", ist.Except(erwartet)) + ".");
        }

        /// <summary>
        /// Jede Tabelle, die auf eine projekteigene Verweistabelle zeigt, kommt bei deren
        /// Projektkopie mit (<see cref="AnlagenFachspalten.PROJEKTKINDER"/>) oder ist
        /// bewusst ausgenommen (<see cref="AnlagenFachspalten.KIND_AUSSCHLUSS"/>, Ergebnisse).
        /// </summary>
        [Fact]
        public void Jede_Kindtabelle_einer_Projektkopie_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            DataTable tabellen = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%'");
            var fehlend = new List<string>();
            foreach (string ziel in AnlagenFachspalten.PROJEKTBEZUG.Values)
            {
                AnlagenFachspalten.PROJEKTKINDER.TryGetValue(ziel, out (string Tabelle, string Fk)[] kinder);
                foreach (DataRow t in tabellen.Rows)
                {
                    string name = Convert.ToString(t["name"], CultureInfo.InvariantCulture);
                    DataTable fks = DataRepository.GetDataTable(
                        "SELECT \"from\" AS Spalte, \"table\" AS Ziel FROM pragma_foreign_key_list(?)",
                        new DbParam("@t", name));
                    foreach (DataRow f in fks.Rows)
                    {
                        if (!string.Equals(Convert.ToString(f["Ziel"], CultureInfo.InvariantCulture), ziel,
                                           StringComparison.OrdinalIgnoreCase)) continue;
                        string spalte = Convert.ToString(f["Spalte"], CultureInfo.InvariantCulture);
                        bool eingeordnet = AnlagenFachspalten.KIND_AUSSCHLUSS.Contains(name) ||
                            (kinder != null && kinder.Any(k => string.Equals(k.Tabelle, name, StringComparison.OrdinalIgnoreCase) &&
                                                               string.Equals(k.Fk, spalte, StringComparison.OrdinalIgnoreCase)));
                        if (!eingeordnet) fehlend.Add(name + "." + spalte + " -> " + ziel);
                    }
                }
            }
            Assert.True(fehlend.Count == 0, "Kindtabelle ohne Einordnung: " + string.Join(", ", fehlend) +
                ". Einordnen in AnlagenFachspalten.PROJEKTKINDER (wird mitkopiert) oder KIND_AUSSCHLUSS.");
        }

        /// <summary>
        /// Jede Tabelle mit Verweis auf eine Anlagenzeile — Fremdschlüssel auf
        /// <c>Tab_Energieanlagen</c> oder eine Spalte aus
        /// <see cref="AnlagenFachspalten.ANLAGENVERWEIS_SPALTEN"/> ohne Fremdschlüssel — ist für die
        /// vollständige Kopie einer Anlage eingeordnet: mitkopiert
        /// (<see cref="AnlagenFachspalten.ANLAGENKINDER"/>) oder ausgenommen
        /// (<see cref="AnlagenFachspalten.ANLAGENKIND_AUSSCHLUSS"/>, Ergebnisse nie mitkopiert).
        /// </summary>
        [Fact]
        public void Jede_Tabelle_mit_Anlagenverweis_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            List<string> fehlend = UneingeordneteAnlagenkinder();
            Assert.True(fehlend.Count == 0, "Tabelle mit Anlagenverweis ohne Einordnung: " +
                string.Join(", ", fehlend) + ". Einordnen in AnlagenFachspalten.ANLAGENKINDER (wird bei " +
                "der Kopie einer Anlage mitkopiert) oder ANLAGENKIND_AUSSCHLUSS (Ergebnisse, Studien).");

            // Die Liste ist sauber: kein Kind zugleich ausgenommen, keine Ergebnistabelle kopiert,
            // jedes Kind trägt seinen Verweis wirklich.
            foreach ((string tabelle, string fk) in AnlagenFachspalten.ANLAGENKINDER)
            {
                Assert.DoesNotContain(tabelle, AnlagenFachspalten.ANLAGENKIND_AUSSCHLUSS);
                Assert.DoesNotContain("Ergebnis", tabelle, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(fk, DataRepository.SpaltenVonTabelle(tabelle), StringComparer.OrdinalIgnoreCase);
            }
            foreach (string bezug in AnlagenFachspalten.ANLAGENKIND_UMSCHLUESSEL.Values)
                Assert.Contains(AnlagenFachspalten.ANLAGENKINDER, k => string.Equals(k.Tabelle, bezug, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gegenprobe: eine neue Tabelle mit Fremdschlüssel auf die Anlagenzeile und eine mit
        /// <c>ID_Anlage</c> ohne Fremdschlüssel fallen auf, eingeordnet nicht mehr.
        /// </summary>
        [Fact]
        public void Gegenprobe_eine_neue_Tabelle_mit_Anlagenverweis_faellt_auf()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "CREATE TABLE Probe_AnlageKind (ID INTEGER PRIMARY KEY, ID_Energieanlage INTEGER " +
                "REFERENCES Tab_Energieanlagen (ID) ON DELETE CASCADE, Wert REAL) STRICT"));
            Assert.True(DataRepository.ExecuteSQL(
                "CREATE TABLE Probe_AnlageOhneFk (ID INTEGER PRIMARY KEY, ID_Anlage INTEGER, Wert REAL) STRICT"));

            List<string> fehlend = UneingeordneteAnlagenkinder();
            Assert.Contains("Probe_AnlageKind.ID_Energieanlage", fehlend);
            Assert.Contains("Probe_AnlageOhneFk.ID_Anlage", fehlend);
            Assert.Equal(2, fehlend.Count);

            Assert.True(DataRepository.ExecuteSQL("DROP TABLE Probe_AnlageKind"));
            Assert.True(DataRepository.ExecuteSQL("DROP TABLE Probe_AnlageOhneFk"));
            Assert.Empty(UneingeordneteAnlagenkinder());
        }

        /// <summary>Die Verweise „Tabelle.Spalte“ auf eine Anlagenzeile ohne Einordnung.</summary>
        private static List<string> UneingeordneteAnlagenkinder()
        {
            DataTable tabellen = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%'");
            var fehlend = new List<string>();
            foreach (DataRow t in tabellen.Rows)
            {
                string name = Convert.ToString(t["name"], CultureInfo.InvariantCulture);
                if (string.Equals(name, "Tab_Energieanlagen", StringComparison.OrdinalIgnoreCase)) continue;
                var verweise = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                DataTable fks = DataRepository.GetDataTable(
                    "SELECT \"from\" AS Spalte, \"table\" AS Ziel FROM pragma_foreign_key_list(?)",
                    new DbParam("@t", name));
                foreach (DataRow f in fks.Rows)
                    if (string.Equals(Convert.ToString(f["Ziel"], CultureInfo.InvariantCulture), "Tab_Energieanlagen",
                                      StringComparison.OrdinalIgnoreCase))
                        verweise.Add(Convert.ToString(f["Spalte"], CultureInfo.InvariantCulture));
                foreach (string spalte in DataRepository.SpaltenVonTabelle(name))
                    if (AnlagenFachspalten.ANLAGENVERWEIS_SPALTEN.Contains(spalte)) verweise.Add(spalte);

                foreach (string spalte in verweise)
                {
                    bool eingeordnet = AnlagenFachspalten.ANLAGENKIND_AUSSCHLUSS.Contains(name) ||
                        AnlagenFachspalten.ANLAGENKINDER.Any(k =>
                            string.Equals(k.Tabelle, name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(k.Fk, spalte, StringComparison.OrdinalIgnoreCase));
                    if (!eingeordnet) fehlend.Add(name + "." + spalte);
                }
            }
            return fehlend;
        }
    }
}
