using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ergebnistabelle je Gebäude</b> (Entscheid E30, Schemaschritt 107, Konzept
    /// Gebäudesimulation N1.35): Schema, Schreiben im Lauf, Lesen, Bericht und
    /// Produktausweis (A12).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErgebnisGebaeudeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Drei Gebäude, alle auf dem VDI-Weg (Spalte NULL = Vorgabe).</summary>
        private const int PROJEKT_DREI_VDI = 1039;

        /// <summary>Ein Gebäude auf dem Tagesbilanz-Weg (A15).</summary>
        private const int PROJEKT_TAGESBILANZ = 1040;

        /// <summary>Ohne Gebäude.</summary>
        private const int PROJEKT_OHNE_GEBAEUDE = 1030;

        // =================================================================
        // Schema
        // =================================================================

        [Fact]
        public void Die_Tabelle_entsteht_STRICT_und_wiederholbar()
        {
            using SqliteConnection c = Leer();
            Anlegen(c);
            Anlegen(c);   // IF NOT EXISTS - der zweite Lauf tut nichts

            string sql = (string)Skalar(c, "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Tab_ErgebnisGebaeude'");
            Assert.EndsWith(") STRICT", sql, StringComparison.Ordinal);
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL, Spalten(c).Count);

            // Die Einheit steht im Namen jeder Zahlenspalte (Einheitenregel 3).
            foreach (string s in Spalten(c).Where(s => s != "ID" && !s.StartsWith("ID_", StringComparison.Ordinal)
                                                      && s != "Merkplatz" && s != "Gebaeudename" && s != "Rechenweg"))
                Assert.True(s.EndsWith("_Mwh") || s.EndsWith("_Kw") || s.EndsWith("_H") || s.EndsWith("_C"), s + " ohne Einheit im Namen.");

            // Beziehungen über IDs, beide mit Löschweitergabe.
            var fks = Zeilen(c, "SELECT \"from\", \"table\", on_delete FROM pragma_foreign_key_list('Tab_ErgebnisGebaeude')");
            Assert.Contains(fks, f => (string)f[0] == "ID_Ergebnis" && (string)f[1] == "Tab_Ergebnis" && (string)f[2] == "CASCADE");
            Assert.Contains(fks, f => (string)f[0] == "ID_Gebaeude" && (string)f[1] == "Tab_Gebaeude" && (string)f[2] == "CASCADE");

            // Zwei Indizes auf den Verweisen.
            Assert.NotNull(Skalar(c, "SELECT name FROM sqlite_master WHERE type = 'index' AND name = $n", ("$n", ErgebnisGebaeudeSchema.INDEX_ERGEBNIS)));
            Assert.NotNull(Skalar(c, "SELECT name FROM sqlite_master WHERE type = 'index' AND name = $n", ("$n", ErgebnisGebaeudeSchema.INDEX_GEBAEUDE)));
        }

        [Fact]
        public void Jede_Pruefung_weist_ihren_Fremdwert_ab_und_NULL_heisst_nicht_gerechnet()
        {
            using SqliteConnection c = Leer();
            Anlegen(c);
            Ausfuehren(c, "INSERT INTO Tab_Ergebnis (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_Gebaeude (ID) VALUES (7)");
            const string zeile = "INSERT INTO Tab_ErgebnisGebaeude (ID, ID_Ergebnis, ID_Gebaeude, Merkplatz, Rechenweg, " +
                                 "Heizwaerme_Mwh, Spitze_Kw, SpitzeTagesmittel_Kw, Spitze95_Kw) VALUES (1, 1, 7, 0, 'TAGESBILANZ', 1.0, 2.0, 1.5, 1.0)";
            Ausfuehren(c, zeile);   // Tagesbilanz: die VDI-Größen bleiben NULL

            foreach (string fremd in new[]
            {
                "UPDATE Tab_ErgebnisGebaeude SET Rechenweg = 'ANDERS'",
                "UPDATE Tab_ErgebnisGebaeude SET Merkplatz = -1",
                "UPDATE Tab_ErgebnisGebaeude SET Kuehlstunden_H = 8761",
                "UPDATE Tab_ErgebnisGebaeude SET Ueberhitzungsstunden_H = -1",
                "UPDATE Tab_ErgebnisGebaeude SET Sommerlueftungsstunden_H = 9000",
                "UPDATE Tab_ErgebnisGebaeude SET Heizwaerme_Mwh = NULL",
                "UPDATE Tab_ErgebnisGebaeude SET Heizwaerme_Mwh = 'viel'",
                "UPDATE Tab_ErgebnisGebaeude SET ID_Gebaeude = 99",
            })
                Assert.True(Wirft(c, fremd), fremd);

            // Die Löschweitergabe: Kopf weg -> Zeile weg.
            Ausfuehren(c, "DELETE FROM Tab_Ergebnis WHERE ID = 1");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_ErgebnisGebaeude"));
        }

        /// <summary>
        /// Der Schritt vom Vorzustand 106: Auf der Testdatenbank ohne die Tabelle legen die
        /// Anweisungen genau die Tabelle an, die die Messlatte trägt — und die Messlatte steht
        /// auf dem Zielstand.
        /// </summary>
        [Fact]
        public void Der_Schritt_fuehrt_die_Testdatenbank_vom_Vorzustand_auf_den_Zielstand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Die Messlatte traegt die Tabelle des Schritts 107 und dahinter die vier Spalten des
            // Schritts 128 (SQLite schreibt ein ADD COLUMN in den gespeicherten Text).
            string soll = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@n", ErgebnisGebaeudeSchema.TAB)));
            string kopf107 = ErgebnisGebaeudeSchema.SQL_CREATE.Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE");
            kopf107 = kopf107.Substring(0, kopf107.LastIndexOf("\n) STRICT", StringComparison.Ordinal));
            Assert.StartsWith(kopf107, soll, StringComparison.Ordinal);
            Assert.EndsWith(") STRICT", soll, StringComparison.Ordinal);
            Assert.True(Convert.ToInt32(DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation")) >= ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS);
            Assert.True(ErgebnisGebaeudeSchema.HeizkreisVollstaendig());
            // Die Messlatte steht auf dem Zielstand: dazu die fünf Spalten des Kältekreises (KAK-S3, E37).
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_KUEHLKREIS, DataRepository.SpaltenVonTabelle(ErgebnisGebaeudeSchema.TAB).Count);

            // Vorzustand herstellen, Schritt 107 fahren, zweimal - dann Schritt 128, zweimal.
            DataRepository.ExecuteNonQuery("DROP TABLE " + ErgebnisGebaeudeSchema.TAB);
            Assert.False(ErgebnisGebaeudeSchema.Vorhanden());
            Assert.False(ErgebnisGebaeudeSchema.HeizkreisVollstaendig());
            Assert.Equal(0, ErgebnisGebaeudeSchema.HeizkreisAlle(null));   // ohne Tabelle: nichts
            for (int lauf = 0; lauf < 2; lauf++)
                foreach (KeyValuePair<string, string> a in ErgebnisGebaeudeSchema.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);
            Assert.True(ErgebnisGebaeudeSchema.Vorhanden());
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL, DataRepository.SpaltenVonTabelle(ErgebnisGebaeudeSchema.TAB).Count);
            Assert.False(ErgebnisGebaeudeSchema.HeizkreisVollstaendig());

            var bericht = new List<string>();
            Assert.Equal(4, ErgebnisGebaeudeSchema.HeizkreisAlle(bericht));
            Assert.Equal(0, ErgebnisGebaeudeSchema.HeizkreisAlle(bericht));   // wiederholbar
            Assert.True(ErgebnisGebaeudeSchema.HeizkreisVollstaendig());
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_HEIZKREIS, DataRepository.SpaltenVonTabelle(ErgebnisGebaeudeSchema.TAB).Count);
            Assert.Contains(bericht, z => z.StartsWith("4 von 4", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Die Nummer steht an EINER Stelle</b> (<see cref="ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS"/>):
        /// Migration, Werkzeug und Nachzieh-Liste der Testkopie bedienen sich aus derselben Quelle,
        /// der Schritt steht in der Migration NACH 127 (Risikomodul 125, Katalogreparatur 126,
        /// Wirkungen 127 waren beim Merge belegt), und der Zielstand reicht bis zu ihm.
        /// </summary>
        [Fact]
        public void Schritt_128_steht_nach_127_in_Migration_Werkzeug_und_Testkopie()
        {
            Assert.Equal(128, ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS);
            Assert.True(SchemaStand.Zielversion >= ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS);
            Assert.True(ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS > ProjektWirkungSchema.SCHRITT);

            string wurzel = null;
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) { wurzel = d.FullName; break; }
            if (wurzel == null) return;

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_128_ERGEBNIS_HEIZKREIS = ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS", migration);
            int ort127 = migration.IndexOf("new Schritt(SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN", StringComparison.Ordinal);
            int ort128 = migration.IndexOf("new Schritt(SCHRITT_128_ERGEBNIS_HEIZKREIS", StringComparison.Ordinal);
            Assert.True(ort127 > 0 && ort128 > ort127, "Der Schritt 128 steht nicht nach 127.");

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("ErgebnisGebaeudeSchema.HeizkreisAlle(", werkzeug);
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("ErgebnisGebaeudeSchema.HeizkreisAlle(null)", vorrichtung);
        }

        /// <summary>
        /// <b>Schritt 128 — der Heizkreis je Gebäude</b> (Anlagenkopplung AK1 Welle 3): vier
        /// nullbare Spalten, die Prüfungen der Übergabeart und der Stunden, und NULL heißt „nicht
        /// gekoppelt gerechnet" — eine Zeile ohne Kopplung bleibt, wie sie war.
        /// </summary>
        [Fact]
        public void Schritt_128_haengt_den_Heizkreis_an_und_prueft_Art_und_Stunden()
        {
            using SqliteConnection c = Leer();
            Anlegen(c);
            Ausfuehren(c, "INSERT INTO Tab_Ergebnis (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_Gebaeude (ID) VALUES (7)");
            Ausfuehren(c, "INSERT INTO Tab_ErgebnisGebaeude (ID, ID_Ergebnis, ID_Gebaeude, Merkplatz, Rechenweg, " +
                          "Heizwaerme_Mwh, Spitze_Kw, SpitzeTagesmittel_Kw, Spitze95_Kw) VALUES (1, 1, 7, 0, 'VDI6007', 1.0, 2.0, 1.5, 1.0)");

            foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
                Ausfuehren(c, ErgebnisGebaeudeSchema.SpalteAnlegen(s));
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_HEIZKREIS, Spalten(c).Count);
            Assert.EndsWith(") STRICT", (string)Skalar(c, "SELECT sql FROM sqlite_master WHERE name = 'Tab_ErgebnisGebaeude'"), StringComparison.Ordinal);

            // Die vorhandene Zeile: alles NULL - nicht gekoppelt gerechnet.
            foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
                Assert.Equal(DBNull.Value, Skalar(c, "SELECT \"" + s.Key + "\" FROM Tab_ErgebnisGebaeude"));

            // Eine gekoppelte Zeile.
            Ausfuehren(c, "UPDATE Tab_ErgebnisGebaeude SET Uebergabe_Art = 'RADIATOR', VorlaufMittel_C = 35.3, " +
                          "RuecklaufMittel_C = 32.05, UebergabeBegrenzt_H = 210.9");
            Assert.Equal(210.9, (double)Skalar(c, "SELECT UebergabeBegrenzt_H FROM Tab_ErgebnisGebaeude"));

            foreach (string fremd in new[]
            {
                "UPDATE Tab_ErgebnisGebaeude SET Uebergabe_Art = 'IDEAL'",       // ideal ist „nicht gekoppelt" = NULL
                "UPDATE Tab_ErgebnisGebaeude SET Uebergabe_Art = 'radiator'",
                "UPDATE Tab_ErgebnisGebaeude SET UebergabeBegrenzt_H = 8760.5",
                "UPDATE Tab_ErgebnisGebaeude SET UebergabeBegrenzt_H = -0.1",
                "UPDATE Tab_ErgebnisGebaeude SET VorlaufMittel_C = 'warm'",
            })
                Assert.True(Wirft(c, fremd), fremd);

            // Die Einheit steht im Namen jeder Zahlenspalte des Schritts (Einheitenregel 3).
            foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
                if (s.Key != ErgebnisGebaeudeSchema.SPALTE_UEBERGABE_ART)
                    Assert.True(s.Key.EndsWith("_C", StringComparison.Ordinal) || s.Key.EndsWith("_H", StringComparison.Ordinal), s.Key);
        }

        // =================================================================
        // Schreiben im Lauf
        // =================================================================

        /// <summary>
        /// Drei Gebäude auf dem VDI-Weg: drei Zeilen, deren Werte die des Laufs im Speicher
        /// sind; ein zweiter Lauf ersetzt sie; <c>Load</c> liest dieselben Zahlen.
        /// </summary>
        [Fact]
        public void Der_Lauf_schreibt_je_Gebaeude_eine_Zeile_mit_den_Werten_im_Speicher()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationRunner laeufer = Rechne(PROJEKT_DREI_VDI);
            List<ErgebnisGebaeudeModel> imSpeicher = laeufer.simulation_Waermebedarf.GebaeudeKennzahlenListe;
            int gebaeude = Gebaeudezahl(PROJEKT_DREI_VDI);
            Assert.Equal(3, gebaeude);
            Assert.Equal(gebaeude, imSpeicher.Count);

            DataTable zeilen = Zeilen(PROJEKT_DREI_VDI);
            Assert.Equal(gebaeude, zeilen.Rows.Count);
            for (int i = 0; i < gebaeude; i++)
            {
                DataRow r = zeilen.Rows[i];
                ErgebnisGebaeudeModel m = imSpeicher[i];
                GebaeudeModellErgebnis vdi = laeufer.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(i);
                Assert.NotNull(vdi);

                Assert.Equal(i, Convert.ToInt32(r["Merkplatz"]));
                Assert.Equal(m.ID_Gebaeude, Convert.ToInt32(r["ID_Gebaeude"]));
                Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, (string)r["Rechenweg"]);
                Assert.Equal(m.HeizwaermeMwh, (double)r["Heizwaerme_Mwh"]);
                Assert.Equal(m.SpitzeKw, (double)r["Spitze_Kw"]);
                Assert.Equal(m.SpitzeTagesmittelKw, (double)r["SpitzeTagesmittel_Kw"]);
                Assert.Equal(m.Spitze95Kw, (double)r["Spitze95_Kw"]);
                // E32: Die Gebäude von 1039 werden nicht gekühlt und laufen frei - Kühlenergie
                // und Kühlstunden gibt es nicht, die Zellen bleiben NULL („nicht verfügbar").
                Assert.False(vdi.KuehlungWirksam);
                Assert.Null(vdi.KuehlenergieMwh);
                Assert.Null(m.KuehlenergieMwh);
                Assert.Equal(DBNull.Value, r["Kuehlenergie_Mwh"]);
                Assert.Equal(DBNull.Value, r["Kuehlstunden_H"]);
                Assert.Equal(vdi.MittlereRaumtemperaturHeizzeit, (double)r["MittlereRaumtemperatur_C"]);
                Assert.Equal(vdi.Ueberhitzungsstunden, Convert.ToInt32(r["Ueberhitzungsstunden_H"]));
                Assert.Equal(vdi.StundenMitSommerlueftung, Convert.ToInt32(r["Sommerlueftungsstunden_H"]));
                Assert.Equal(vdi.ThetaMax, (double)r["ObereRaumtemperatur_C"]);
                // Schritt 128: ohne Kopplung tragen die vier Spalten des Heizkreises NULL.
                Assert.Null(vdi.Heizkreis);
                Assert.False(m.IstGekoppelt);
                foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
                    Assert.Equal(DBNull.Value, r[s.Key]);

                // Die Stundenspitze der Reihe ist die Spitze des Ergebnisträgers (beide W -> kW).
                Nahe(vdi.SpitzeKw, m.SpitzeKw);
                Nahe(vdi.JahresheizwaermeMwh, m.HeizwaermeMwh);
            }

            // Die Summe über die Gebäude ist der Gebäudeanteil des Heizkanals.
            Nahe(laeufer.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt,
                 imSpeicher.Sum(g => g.HeizwaermeMwh));

            // Load liest dieselben Zahlen.
            ErgebnisModel geladen = new ErgebnisCtrl().Load(PROJEKT_DREI_VDI);
            Assert.Equal(gebaeude, geladen.Gebaeude.Count);
            Assert.Equal(imSpeicher.Select(g => g.HeizwaermeMwh), geladen.Gebaeude.Select(g => g.HeizwaermeMwh));
            Assert.All(geladen.Gebaeude, g => Assert.True(g.IstVdi6007));

            // Ein zweiter Lauf ersetzt die Zeilen, statt sie zu verdoppeln.
            Rechne(PROJEKT_DREI_VDI);
            Assert.Equal(gebaeude, Zeilen(PROJEKT_DREI_VDI).Rows.Count);
            Assert.Equal(gebaeude, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ErgebnisGebaeude g INNER JOIN Tab_Gebaeude t ON t.ID = g.ID_Gebaeude WHERE t.ID_Projekt = ?",
                new DbParam("@p", PROJEKT_DREI_VDI))));
        }

        /// <summary>
        /// Ein Gebäude auf dem Tagesbilanz-Weg trägt Rechenweg, Wärmebedarf und die drei
        /// Spitzenwerte; die Größen des VDI-Wegs sind NULL — und die Auskunft des
        /// Gebäudedialogs nennt dieselben Zahlen.
        /// </summary>
        [Fact]
        public void Ein_Tagesbilanz_Gebaeude_traegt_NULL_fuer_die_Groessen_des_VDI_Wegs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Rechne(PROJEKT_TAGESBILANZ);
            DataRow r = Assert.Single(Zeilen(PROJEKT_TAGESBILANZ).Rows.Cast<DataRow>());
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, (string)r["Rechenweg"]);
            Assert.True((double)r["Heizwaerme_Mwh"] > 0);
            foreach (string s in new[] { "Kuehlenergie_Mwh", "Kuehlstunden_H", "MittlereRaumtemperatur_C",
                                         "Ueberhitzungsstunden_H", "Sommerlueftungsstunden_H", "ObereRaumtemperatur_C" })
                Assert.Equal(DBNull.Value, r[s]);

            // Dieselbe Zahl wie im Gebäudedialog (GebaeudeBedarfCtrl, W9-E-2).
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@g", Convert.ToInt32(r["ID_Gebaeude"]))));
            int klima = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT_TAGESBILANZ)));
            GebaeudeBedarfErgebnis dialog = GebaeudeBedarfCtrl.Rechnen(PROJEKT_TAGESBILANZ, klima, idZ);
            Assert.True(dialog.Erfolgreich);
            Assert.Equal(dialog.HeizwaermeMwh, (double)r["Heizwaerme_Mwh"]);
            Assert.Equal(dialog.MaxLastKw, (double)r["Spitze_Kw"]);
            Assert.Equal(dialog.SpitzeTagesmittelKw.Value, (double)r["SpitzeTagesmittel_Kw"]);
            Assert.Equal(dialog.SpitzeQuantil95Kw.Value, (double)r["Spitze95_Kw"]);
        }

        [Fact]
        public void Ein_Projekt_ohne_Gebaeude_schreibt_keine_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Gebaeudezahl(PROJEKT_OHNE_GEBAEUDE));
            Rechne(PROJEKT_OHNE_GEBAEUDE);
            Assert.Equal(0, Zeilen(PROJEKT_OHNE_GEBAEUDE).Rows.Count);
            Assert.Empty(new ErgebnisCtrl().Load(PROJEKT_OHNE_GEBAEUDE).Gebaeude);
        }

        // =================================================================
        // Bericht
        // =================================================================

        [Fact]
        public void Der_Bericht_zeigt_den_Abschnitt_Gebaeude_und_den_Produktausweis()
        {
            BerichtsDaten daten = Daten(
                Zeile(0, "Haus A", DbWerte.GEBAEUDE_MODELL_VDI6007, 12.5, kuehl: 1.25),
                Zeile(1, "Haus B", DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, 30.0, kuehl: null));

            string text = Schreibe(new ProjektbeschreibungBaustein(), daten);
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_GEBAEUDE_ERGEBNIS, text);
            Assert.Contains("Haus A", text);
            Assert.Contains("Haus B", text);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_VDI6007, text);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_RECHENWEG_TAGESBILANZ, text);
            // Stufe KU1 (Kuehlkonzept 6.4): ohne „(informativ)", und die Grenze der Zahl (K5)
            // steht neben den Kuehlzahlen der Gebaeude.
            Assert.Contains("Kühlenergie", text);
            Assert.DoesNotContain("(informativ)", text);
            Assert.Contains(WindowsFormsApplication1.SimulationKaeltebedarf.GrenzeFeuchte, text);
            Assert.Contains("12,5 MWh/a", text);

            Assert.True(DeckblattBaustein.ProduktausweisNoetig(daten));
            string kopf = Schreibe(new DeckblattBaustein(), daten);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007, kopf);
            Assert.StartsWith("Rechenkern nach VDI 6007 Blatt 1", WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007, StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Gebaeude_entfaellt_der_Abschnitt_und_ohne_VDI_Gebaeude_der_Ausweis()
        {
            BerichtsDaten ohne = Daten();
            Assert.Empty(ProjektbeschreibungBaustein.GebaeudeZeilen(ohne.Varianten[0]));
            Assert.DoesNotContain(ProjektbeschreibungBaustein.UEBERSCHRIFT_GEBAEUDE_ERGEBNIS, Schreibe(new ProjektbeschreibungBaustein(), ohne));
            Assert.False(DeckblattBaustein.ProduktausweisNoetig(ohne));
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007, Schreibe(new DeckblattBaustein(), ohne));

            // Nur Tagesbilanz: der Abschnitt steht, der Ausweis nach E10 nicht (E20).
            BerichtsDaten alt = Daten(Zeile(0, "Haus B", DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, 30.0, kuehl: null));
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_GEBAEUDE_ERGEBNIS, Schreibe(new ProjektbeschreibungBaustein(), alt));
            Assert.False(DeckblattBaustein.ProduktausweisNoetig(alt));

            // Ohne Ergebnis überhaupt: kein Abschnitt, kein Fehler.
            var leer = new BerichtsDaten { Stammprojektname = "P" };
            leer.Varianten.Add(new VariantenDaten { IstStamm = true });
            Assert.Empty(ProjektbeschreibungBaustein.GebaeudeZeilen(leer.Varianten[0]));
            Assert.False(DeckblattBaustein.ProduktausweisNoetig(leer));
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static SimulationRunner Rechne(int idProjekt)
        {
            var laeufer = new SimulationRunner();
            int id = laeufer.SimuliereUndSpeichere(idProjekt, out string fehler);
            Assert.True(id > 0, "Lauf gescheitert: " + fehler);
            return laeufer;
        }

        /// <summary>Gleich bis auf die double-Rundung zweier Summationsreihenfolgen (relativ 1e-9).</summary>
        private static void Nahe(double soll, double ist)
        {
            Assert.True(Math.Abs(soll - ist) <= 1e-9 * Math.Max(1.0, Math.Abs(soll)), soll + " != " + ist);
        }

        private static int Gebaeudezahl(int idProjekt) =>
            Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                                                         new DbParam("@p", idProjekt)));

        private static DataTable Zeilen(int idProjekt) =>
            DataRepository.GetDataTable(
                "SELECT g.* FROM Tab_ErgebnisGebaeude g INNER JOIN Tab_Ergebnis e ON e.ID = g.ID_Ergebnis " +
                "WHERE e.ID_Projekt = ? ORDER BY g.Merkplatz", new DbParam("@p", idProjekt));

        private static ErgebnisGebaeudeModel Zeile(int platz, string name, string weg, double mwh, double? kuehl) =>
            new ErgebnisGebaeudeModel
            {
                Merkplatz = platz, Gebaeudename = name, Rechenweg = weg, HeizwaermeMwh = mwh,
                SpitzeKw = 10, SpitzeTagesmittelKw = 8, Spitze95Kw = 6,
                KuehlenergieMwh = kuehl, KuehlstundenH = kuehl.HasValue ? 100 : null,
                MittlereRaumtemperaturC = kuehl.HasValue ? 21.0 : null,
                UeberhitzungsstundenH = kuehl.HasValue ? 5 : null,
            };

        private static BerichtsDaten Daten(params ErgebnisGebaeudeModel[] gebaeude)
        {
            var erg = new ErgebnisModel();
            erg.Gebaeude.AddRange(gebaeude);
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(new VariantenDaten { IstStamm = true, Projektname = "Probe", Ergebnis = erg });
            return d;
        }

        /// <summary>Schreibt einen Baustein in ein Dokument im Speicher und liefert dessen Text.</summary>
        private static string Schreibe(IBerichtsBaustein baustein, BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            baustein.SchreibeWord(new WordKontext(main, main.Document.Body, null), daten, BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<Text>().Select(t => t.Text));
        }

        private static SqliteConnection Leer()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE Tab_Ergebnis (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE Tab_Gebaeude (ID INTEGER PRIMARY KEY) STRICT");
            return c;
        }

        private static void Anlegen(SqliteConnection c)
        {
            foreach (KeyValuePair<string, string> a in ErgebnisGebaeudeSchema.Anweisungen) Ausfuehren(c, a.Value);
        }

        private static List<string> Spalten(SqliteConnection c) =>
            Zeilen(c, "SELECT name FROM pragma_table_info('Tab_ErgebnisGebaeude')").Select(r => (string)r[0]).ToList();

        private static void Ausfuehren(SqliteConnection c, string sql, params (string, object)[] p)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, w) in p) cmd.Parameters.AddWithValue(n, w);
            cmd.ExecuteNonQuery();
        }

        private static object Skalar(SqliteConnection c, string sql, params (string, object)[] p)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, w) in p) cmd.Parameters.AddWithValue(n, w);
            return cmd.ExecuteScalar();
        }

        private static List<object[]> Zeilen(SqliteConnection c, string sql)
        {
            var l = new List<object[]>();
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            using SqliteDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                var z = new object[r.FieldCount];
                r.GetValues(z);
                l.Add(z);
            }
            return l;
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
