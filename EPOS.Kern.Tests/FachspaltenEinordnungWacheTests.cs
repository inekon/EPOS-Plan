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
        /// Steuer und Aufteilung, Kältemaschine (KU3), Sondenfeld (Schritt 195), Kältefolge (Schritt 212),
        /// Rückkühlwerk und Wasserpreis (K-F1).
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
            "WQ_Kopfueberdeckung", "WQ_Betrachtungsjahr", "WQ_Sondenanordnung",
            // KB-D (Schritt 212): der Rang in der Kaeltefolge - die Zeile traegt ihn durch Assistent und Kopie.
            "Kaelte_Rang",
            // K-F1: das Rueckkuehlwerk der Kaeltemaschine (projekteigene Zeile, AnlagenFachspalten.PROJEKTBEZUG)
            // und der Wasserpreis der Anlage - beide Eingabewerte, die Assistent, Uebernahme und Kopie retten.
            "ID_Rueckkuehlwerk", "Wasserpreis_EUR_m3"
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
            "Kuehl_Frei_Leistung_kW", "Einbindung", "Vorwaermbetrieb"
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
        /// (<see cref="AnlagenFachspalten.ANLAGENKIND_AUSSCHLUSS"/>, Ergebnisse nie mitkopiert);
        /// Kostenpositionen gehen mit, nach Kapazität skaliert (siehe nächster Fall).
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

        /// <summary>
        /// Die Sonderspalten der Anlagenkopie (Anwenderentscheid 07.10.2026, Kostenpositionen nach
        /// Kapazität) sind eingeordnet: Jede skalierte, bewusst unskalierte und Ankerspalte
        /// gehört zu einer Tabelle aus <see cref="AnlagenFachspalten.ANLAGENKINDER"/> und
        /// besteht; keine ist zugleich skaliert und unskaliert; und JEDE Zahlenspalte (REAL)
        /// einer skalierten Tabelle ist entschieden — eine neue Betragsspalte von
        /// <c>Tab_ProjektWerte</c> fällt auf, statt still unskaliert mitzugehen.
        /// </summary>
        [Fact]
        public void Jede_Zahlenspalte_einer_skalierten_Kindtabelle_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            List<string> fehler = UneingeordneteKostenspalten(AnlagenFachspalten.ANLAGENKIND_SKALIERT,
                AnlagenFachspalten.ANLAGENKIND_UNSKALIERT, AnlagenFachspalten.ANLAGENKIND_GERAETEANKER);
            Assert.True(fehler.Count == 0, "Sonderspalten der Anlagenkopie nicht eingeordnet: " +
                string.Join(", ", fehler) + ". Eine Betrags- oder Mengenspalte gehört in " +
                "AnlagenFachspalten.ANLAGENKIND_SKALIERT, ein Satz oder eine Dauer in ANLAGENKIND_UNSKALIERT.");
            Assert.Contains("Tab_ProjektWerte.EingegebenerWert", AnlagenFachspalten.ANLAGENKIND_SKALIERT);
            Assert.Contains("Tab_ProjektWerte.Einheitpreis", AnlagenFachspalten.ANLAGENKIND_UNSKALIERT);
            Assert.Contains("Tab_ProjektWerte", AnlagenFachspalten.ANLAGENKINDER.Select(k => k.Tabelle));
            Assert.DoesNotContain("Tab_ProjektWerte", AnlagenFachspalten.ANLAGENKIND_AUSSCHLUSS);
        }

        /// <summary>
        /// Gegenprobe: eine neue REAL-Spalte an <c>Tab_ProjektWerte</c>, eine verwaiste
        /// Sonderspalte, eine Spalte einer nicht kopierten Tabelle und ein Doppeleintrag fallen auf.
        /// </summary>
        [Fact]
        public void Gegenprobe_eine_neue_Betragsspalte_und_falsche_Eintraege_fallen_auf()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL("ALTER TABLE Tab_ProjektWerte ADD COLUMN Probe_Betrag REAL"));
            var skaliert = new HashSet<string>(AnlagenFachspalten.ANLAGENKIND_SKALIERT, StringComparer.OrdinalIgnoreCase)
            {
                "Tab_ProjektWerte.GibtEsNicht", "Tab_ErgebnisStromspeicher.ID", "Tab_ProjektWerte.Einheitpreis"
            };
            List<string> fehler = UneingeordneteKostenspalten(skaliert,
                AnlagenFachspalten.ANLAGENKIND_UNSKALIERT, AnlagenFachspalten.ANLAGENKIND_GERAETEANKER);
            Assert.Contains("Tab_ProjektWerte.Probe_Betrag", fehler);
            Assert.Contains("Tab_ProjektWerte.GibtEsNicht", fehler);
            Assert.Contains("Tab_ErgebnisStromspeicher.ID", fehler);
            Assert.Contains("Tab_ProjektWerte.Einheitpreis", fehler);
            Assert.Equal(4, fehler.Count);

            Assert.True(DataRepository.ExecuteSQL("ALTER TABLE Tab_ProjektWerte DROP COLUMN Probe_Betrag"));
            Assert.Empty(UneingeordneteKostenspalten(AnlagenFachspalten.ANLAGENKIND_SKALIERT,
                AnlagenFachspalten.ANLAGENKIND_UNSKALIERT, AnlagenFachspalten.ANLAGENKIND_GERAETEANKER));
        }

        /// <summary>Die Fehler der Sonderspalten als „Tabelle.Spalte“ (je Spalte einmal).</summary>
        private static List<string> UneingeordneteKostenspalten(ISet<string> skaliert, ISet<string> unskaliert,
                                                                 ISet<string> anker)
        {
            var fehler = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var kinder = new HashSet<string>(AnlagenFachspalten.ANLAGENKINDER.Select(k => k.Tabelle),
                                             StringComparer.OrdinalIgnoreCase);
            var spaltenJeTabelle = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> Spalten(string tabelle)
            {
                if (!spaltenJeTabelle.TryGetValue(tabelle, out Dictionary<string, string> d))
                {
                    d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT name, type FROM pragma_table_info(?)", new DbParam("@t", tabelle));
                    foreach (DataRow r in dt.Rows)
                        d[Convert.ToString(r["name"], CultureInfo.InvariantCulture)] =
                            Convert.ToString(r["type"], CultureInfo.InvariantCulture);
                    spaltenJeTabelle[tabelle] = d;
                }
                return d;
            }

            foreach (string eintrag in skaliert.Concat(unskaliert).Concat(anker))
            {
                int punkt = eintrag.IndexOf('.');
                string tabelle = punkt > 0 ? eintrag.Substring(0, punkt) : eintrag;
                string spalte = punkt > 0 ? eintrag.Substring(punkt + 1) : "";
                if (!kinder.Contains(tabelle) || !Spalten(tabelle).ContainsKey(spalte)) fehler.Add(eintrag);
            }
            foreach (string doppelt in skaliert.Where(unskaliert.Contains)) fehler.Add(doppelt);

            foreach (string tabelle in skaliert.Select(x => x.Split('.')[0]).Where(kinder.Contains)
                                              .Distinct(StringComparer.OrdinalIgnoreCase))
                foreach (KeyValuePair<string, string> sp in Spalten(tabelle))
                {
                    string name = tabelle + "." + sp.Key;
                    if (string.Equals(sp.Value, "REAL", StringComparison.OrdinalIgnoreCase) &&
                        !skaliert.Contains(name) && !unskaliert.Contains(name))
                        fehler.Add(name);
                }
            return fehler.ToList();
        }

        /// <summary>
        /// Jede Verweisspalte einer Anlagenkind-Tabelle (<see cref="AnlagenFachspalten.ANLAGENKINDER"/>) —
        /// jede Spalte mit Fremdschlüssel und jede, die nach einem Schlüssel heißt (<c>ID_…</c>,
        /// <c>…ID</c>) — ist eingeordnet: Anlagenverweis, Umschlüsselung innerhalb der Anlage,
        /// Geräteanker, PROJEKTBEZUG (über Projektgrenzen auf die gleichnamige Gegenstelle),
        /// Projektspalte oder PROJEKTFREI. Ein Verweis auf eine Tabelle mit <c>ID_Projekt</c>
        /// darf nicht projektfrei heißen, und jede Zieltabelle eines Projektbezugs trägt
        /// <c>ID_Projekt</c> und <c>Bezeichner</c> — sonst zeigte die Kindzeile einer
        /// Komponentenübernahme still ins Quellprojekt.
        /// </summary>
        [Fact]
        public void Jede_Verweisspalte_einer_Anlagenkind_Tabelle_ist_eingeordnet()
        {
            if (!_db.Vorhanden) return;

            List<string> fehler = UneingeordneteKindverweise(AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG,
                                                             AnlagenFachspalten.ANLAGENKIND_PROJEKTFREI);
            Assert.True(fehler.Count == 0, "Nicht eingeordnete Verweise von Anlagenkindern: " + string.Join("; ", fehler) +
                ". Einordnen in AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG (projektgebunden, wird über " +
                "Projektgrenzen umgeschlüsselt) oder ANLAGENKIND_PROJEKTFREI (Katalog).");

            foreach (string zieltabelle in AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG.Values)
            {
                List<string> spalten = DataRepository.SpaltenVonTabelle(zieltabelle);
                Assert.Contains("ID_Projekt", spalten, StringComparer.OrdinalIgnoreCase);
                Assert.Contains("Bezeichner", spalten, StringComparer.OrdinalIgnoreCase);
            }
            foreach (string s in AnlagenFachspalten.ANLAGENKIND_TRAGEND)
                Assert.True(AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG.ContainsKey(s), s);
            foreach (KeyValuePair<string, string> art in AnlagenFachspalten.PROJEKTBEZUG_ART)
                Assert.Contains(art.Value, DataRepository.SpaltenVonTabelle(art.Key), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gegenprobe: eine neue Verweisspalte auf eine projektgebundene Tabelle fällt auf, und
        /// ein projektgebundener Verweis, der als projektfrei eingeordnet wäre, ebenso.
        /// </summary>
        [Fact]
        public void Gegenprobe_ein_neuer_oder_falsch_eingeordneter_Kindverweis_faellt_auf()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "ALTER TABLE Z_AnlageStrang ADD COLUMN ID_Probe INTEGER REFERENCES Tab_Wechselrichter (ID)"));
            List<string> fehler = UneingeordneteKindverweise(AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG,
                                                             AnlagenFachspalten.ANLAGENKIND_PROJEKTFREI);
            Assert.Contains("Z_AnlageStrang.ID_Probe", fehler);
            Assert.Single(fehler);
            Assert.True(DataRepository.ExecuteSQL("ALTER TABLE Z_AnlageStrang DROP COLUMN ID_Probe"));

            var bezug = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG)
                if (!string.Equals(kv.Key, "Z_AnlageStrang.ID_Wechselrichter", StringComparison.OrdinalIgnoreCase))
                    bezug[kv.Key] = kv.Value;
            var frei = new HashSet<string>(AnlagenFachspalten.ANLAGENKIND_PROJEKTFREI, StringComparer.OrdinalIgnoreCase)
            { "Z_AnlageStrang.ID_Wechselrichter" };
            fehler = UneingeordneteKindverweise(bezug, frei);
            Assert.Contains("Z_AnlageStrang.ID_Wechselrichter (projektgebunden)", fehler);
            Assert.Single(fehler);

            Assert.Empty(UneingeordneteKindverweise(AnlagenFachspalten.ANLAGENKIND_PROJEKTBEZUG,
                                                    AnlagenFachspalten.ANLAGENKIND_PROJEKTFREI));
        }

        /// <summary>Die Verweisspalten „Tabelle.Spalte“ der Anlagenkinder ohne (richtige) Einordnung.</summary>
        private static List<string> UneingeordneteKindverweise(IReadOnlyDictionary<string, string> projektbezug,
                                                               ISet<string> projektfrei)
        {
            var fehler = new List<string>();
            foreach ((string tabelle, string fk) in AnlagenFachspalten.ANLAGENKINDER)
            {
                var ziele = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                DataTable fks = DataRepository.GetDataTable(
                    "SELECT \"from\" AS Spalte, \"table\" AS Ziel FROM pragma_foreign_key_list(?)",
                    new DbParam("@t", tabelle));
                foreach (DataRow f in fks.Rows)
                    ziele[Convert.ToString(f["Spalte"], CultureInfo.InvariantCulture)] =
                        Convert.ToString(f["Ziel"], CultureInfo.InvariantCulture);

                foreach (string spalte in DataRepository.SpaltenVonTabelle(tabelle))
                {
                    if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                    bool verweis = ziele.ContainsKey(spalte) ||
                                   spalte.StartsWith("ID_", StringComparison.OrdinalIgnoreCase) ||
                                   spalte.EndsWith("ID", StringComparison.Ordinal);
                    if (!verweis) continue;

                    string schluessel = tabelle + "." + spalte;
                    bool eingeordnet = string.Equals(spalte, fk, StringComparison.OrdinalIgnoreCase) ||
                        AnlagenFachspalten.ANLAGENKIND_UMSCHLUESSEL.ContainsKey(schluessel) ||
                        AnlagenFachspalten.ANLAGENKIND_GERAETEANKER.Contains(schluessel) ||
                        AnlagenFachspalten.ANLAGENKIND_PROJEKTSPALTE.Contains(schluessel) ||
                        projektbezug.ContainsKey(schluessel) || projektfrei.Contains(schluessel);
                    if (!eingeordnet) { fehler.Add(schluessel); continue; }

                    if (projektfrei.Contains(schluessel) && ziele.TryGetValue(spalte, out string ziel) &&
                        DataRepository.SpaltenVonTabelle(ziel).Contains("ID_Projekt", StringComparer.OrdinalIgnoreCase))
                        fehler.Add(schluessel + " (projektgebunden)");
                }
            }
            return fehler;
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
