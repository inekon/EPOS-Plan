using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Rundlauf der Zonen über JEDE Spalte</b> (Gebäudesimulation G6a, Welle 2) — die Vorrichtung
    /// und der Abdruck. Zwei Gebäude des Projekts 1039 mit je drei Zonen, gemischtem Rang, NULL-Werten
    /// und gesetzter Kühlübergabe; eine Zone kommt über den Bauteilvorschlag eines gbXML-Imports
    /// (<see cref="GebaeudeZonenCtrl.VorschlagSchreiben"/>) samt Herkunft, Aufbauten und
    /// Importpaarungen, dazu ein Projektaufbau mit Projektstoff, eine Trennfläche zur Nachbarzone und
    /// je Gebäude ein Luftstrom (Schritt S-G). So trägt jede der acht Tabellen Zeilen: <c>Tab_Zone</c>,
    /// <c>Tab_Bauteil</c>, <c>Tab_Zonenluftstrom</c>, <c>Tab_Bauteilaufbau</c>, <c>Tab_Bauteilschicht</c>,
    /// <c>Tab_Baustoff</c> (Projektzeilen), <c>Tab_Importquelle</c>, <c>Tab_Importzuordnung</c>.
    ///
    /// <para><b>Der Abdruck liest <c>SELECT *</c></b>, nicht eine Spaltenliste: Eine künftige Spalte ist
    /// von selbst mitgeprüft (Muster <see cref="GebaeudeRundlaufTests"/>). Die Verweise
    /// (<c>ID</c>, <c>ID_Projekt</c>, <c>ID_Gebaeude</c>, <c>ID_Zone</c>, <c>ID_Aufbau</c>,
    /// <c>ID_Baustoff</c>, <c>ID_Importquelle</c>, <c>ID_Bauteil</c>) stehen wahlweise roh oder als Rang
    /// der Zielzeile im Projekt — so vergleicht sich eine Kopie mit ihrer Quelle, ohne dass die
    /// Schlüssel gleich sein müssen, und ein Verweis, der auf die Quelle statt auf die Kopie zeigt,
    /// fällt auf.</para>
    /// </summary>
    internal static class ZonenRundlauf
    {
        internal const int PROJEKT = 1039;
        internal const int GEBAEUDE_A = 10642;
        internal const int GEBAEUDE_B = 10644;
        internal const string PROBE = "gbxml_haus_si.xml";

        private const string TAB_AUFBAU = "Tab_Bauteilaufbau";

        /// <summary>Die acht Tabellen des Rundlaufs, Eltern zuerst (mit Schritt S-G der Luftstrom).</summary>
        internal static readonly string[] TABELLEN =
        {
            SchemaKatalog.TAB_ZONE, SchemaKatalog.TAB_BAUTEIL, SchemaKatalog.TAB_ZONENLUFTSTROM, TAB_AUFBAU,
            SchemaKatalog.TAB_BAUTEILSCHICHT, SchemaKatalog.TAB_BAUSTOFF, SchemaKatalog.TAB_IMPORTQUELLE,
            SchemaKatalog.TAB_IMPORTZUORDNUNG
        };

        /// <summary>Die Verweisspalten der acht Tabellen und ihre Zieltabelle.</summary>
        private static readonly Dictionary<string, string> VERWEISE = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ID_Projekt", "Tab_Projekt" },
            { "ID_Gebaeude", "Tab_Gebaeude" },
            { "ID_Zone", SchemaKatalog.TAB_ZONE },
            { "ID_Nachbarzone", SchemaKatalog.TAB_ZONE },
            { "ID_ZoneA", SchemaKatalog.TAB_ZONE },
            { "ID_ZoneB", SchemaKatalog.TAB_ZONE },
            { "ID_Aufbau", TAB_AUFBAU },
            { "ID_Baustoff", SchemaKatalog.TAB_BAUSTOFF },
            { "ID_Importquelle", SchemaKatalog.TAB_IMPORTQUELLE },
            { "ID_Bauteil", SchemaKatalog.TAB_BAUTEIL },
        };

        private const string GEBAEUDE_DES_PROJEKTS = "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?";

        /// <summary>Die Zeilen einer Tabelle, die zum Projekt (bzw. zum Gebäude) gehören — eigene Filter, nicht die des Plans.</summary>
        private static string Filter(string tabelle, bool jeGebaeude)
        {
            string geb = jeGebaeude ? "?" : GEBAEUDE_DES_PROJEKTS;
            string gebIn = jeGebaeude ? "= ?" : "IN (" + GEBAEUDE_DES_PROJEKTS + ")";
            switch (tabelle)
            {
                case SchemaKatalog.TAB_ZONE: return "ID_Gebaeude " + gebIn;
                case SchemaKatalog.TAB_BAUTEIL: return "ID_Zone IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude " + gebIn + ")";
                case SchemaKatalog.TAB_ZONENLUFTSTROM: return "ID_ZoneA IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude " + gebIn + ")";
                case SchemaKatalog.TAB_IMPORTQUELLE: return "ID_Gebaeude " + gebIn;
                case SchemaKatalog.TAB_IMPORTZUORDNUNG: return "ID_Importquelle IN (SELECT ID FROM Tab_Importquelle WHERE ID_Gebaeude " + gebIn + ")";
                case SchemaKatalog.TAB_BAUTEILSCHICHT: return "ID_Aufbau IN (SELECT ID FROM Tab_Bauteilaufbau WHERE ID_Projekt = ?)";
                default: return "ID_Projekt = ?";
            }
        }

        /// <summary>Hängt die Tabelle am Gebäude (sonst am Projekt)?</summary>
        internal static bool AmGebaeude(string tabelle)
            => tabelle == SchemaKatalog.TAB_ZONE || tabelle == SchemaKatalog.TAB_BAUTEIL
               || tabelle == SchemaKatalog.TAB_ZONENLUFTSTROM
               || tabelle == SchemaKatalog.TAB_IMPORTQUELLE || tabelle == SchemaKatalog.TAB_IMPORTZUORDNUNG;

        internal static long Zahl(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        // =============================================================================
        //  Die Vorrichtung
        // =============================================================================

        /// <summary>Ein Katalogaufbau mit Katalogstoff und freier Schicht, als Projektkopie (samt Projektstoff).</summary>
        private static int Projektaufbau()
        {
            var ctrl = new BauteilaufbauCtrl();
            var wand = new BauteilaufbauModel
            {
                Bezeichner = "Wand G6a", Beschreibung = "Außenwand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                Schichten =
                {
                    new BauteilschichtModel { ID_Baustoff = 18, Dicke = 0.175 },
                    new BauteilschichtModel { Dicke = 0.14, Lambda = 0.035, Rho = 30.0, Cp = 1500.0 },
                    new BauteilschichtModel { Dicke = 0.02, IstLuftschicht = true }
                }
            };
            int stamm = ctrl.KatalogSpeichern(wand).Id;
            int kopie = ctrl.CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            return kopie;
        }

        /// <summary>Legt die Vorrichtung an (Klassenkopf).</summary>
        internal static void Anlegen()
        {
            var zonenCtrl = new GebaeudeZonenCtrl();

            // Gebaeude A: die Zone aus dem Import - Herkunft, Quellkennung, Aufbauten, Paarungen.
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null);
            Assert.False(v.Abgelehnt);
            GebaeudeZonenCtrl.Vorschlagsergebnis e = zonenCtrl.VorschlagSchreiben(GEBAEUDE_A, v);
            Assert.True(e.Ok, e.Meldung);
            GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE_A, ablauf.Quelle, e.Zuordnungen);
            Assert.True(h.Ok, h.Meldung);

            int aufbau = Projektaufbau();

            // Gebaeude A: zwei weitere Zonen, die importierte in der Mitte (gemischter Rang).
            ZoneModel importiert = Assert.Single(zonenCtrl.LesenJeGebaeude(GEBAEUDE_A));
            var anbau = new ZoneModel
            {
                ID = -1, Bezeichner = "Anbau A", Nutzflaeche = 40, Raumhoehe = 2.8, Raumsolltemperatur_Tag = 21,
                Kuehlung_Aktiv = true, Kuehl_Sollwert = 25, Kuehlleistung_Max = 3,
                Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE, Kuehl_Uebergabe_Exponent = 1.05,
                Kuehl_Uebergabe_Leistung_Nenn = 2.5, Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE, Uebergabe_Exponent = 1.1,
                Herkunft = DbWerte.HERKUNFT_MANUELL,
                Bauteile =
                {
                    new BauteilModel { ID = -1, Bezeichner = "Wand Nord", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 18.5,
                                       Azimut = 0, ID_Aufbau = aufbau, Psi_L = 1.2 },
                    new BauteilModel { ID = -2, Bezeichner = "Fenster Nord", Bauteilart = DbWerte.BAUTEILART_FENSTER, Flaeche = 4.2,
                                       U_Wert = 1.1, g_Wert = 0.5, Rahmenanteil = 0.3, Verschattungsfaktor = 0.8, Azimut = 0 },
                    // Schritt S-G: eine Trennfläche zum Lager (vorläufige Id -2, umgeschlüsselt beim Speichern).
                    new BauteilModel { ID = -5, Bezeichner = "Trennwand Lager", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 12,
                                       U_Wert = 1.4, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = -2,
                                       Trennflaeche_Zuordnung = DbWerte.TRENNFLAECHE_AW }
                }
            };
            var lager = new ZoneModel
            {
                ID = -2, Bezeichner = "Lager A", Nutzflaeche = 25, IstBeheizt = false,
                Bauteile =
                {
                    new BauteilModel { ID = -3, Bezeichner = "Boden Lager", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Flaeche = 25,
                                       U_Wert = 0.3, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH },
                    new BauteilModel { ID = -4, Bezeichner = "Trennwand", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 10, U_Wert = 1.5 }
                }
            };
            // Schritt S-G: ein Luftstrom zwischen dem Anbau (vorläufig) und der importierten Zone, verkehrt herum eingegeben.
            var stroeme = new List<ZonenluftstromModel> { new ZonenluftstromModel { ID = -1, ID_ZoneA = importiert.ID, ID_ZoneB = -1, Volumenstrom = 45 } };
            GebaeudeZonenCtrl.Ergebnis a = zonenCtrl.SpeichernJeGebaeude(GEBAEUDE_A, new List<ZoneModel> { anbau, importiert, lager }, stroeme);
            Assert.True(a.Ok, a.Meldung);

            // Gebaeude B: drei Zonen - Werte, NULL, Kuehluebergabe ideal.
            var b = new List<ZoneModel>
            {
                new ZoneModel
                {
                    ID = -1, Bezeichner = "Büro B", Nutzflaeche = 50, Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_IDEAL,
                    Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR, Uebergabe_Exponent = 1.3, Uebergabe_Leistung_Nenn = 4.0,
                    Bauteile = { new BauteilModel { ID = -1, Bezeichner = "Wand Süd B", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                                    Flaeche = 30, Azimut = 180, ID_Aufbau = aufbau } }
                },
                new ZoneModel
                {
                    ID = -2, Bezeichner = "Flur B", Nutzflaeche = 30, Raumsolltemperatur_Nachtabsenkung = 17, Luftwechsel_Infiltration = 0.2,
                    Bewohner = 2, Interne_Waermegewinne = 150,
                    Bauteile = { new BauteilModel { ID = -2, Bezeichner = "Tür B", Bauteilart = DbWerte.BAUTEILART_TUER, Flaeche = 2.1,
                                                    U_Wert = 1.8, Azimut = 90 } }
                },
                new ZoneModel
                {
                    ID = -3, Bezeichner = "Dachgeschoss B", Nutzflaeche = 20,
                    Bauteile = { new BauteilModel { ID = -3, Bezeichner = "Dach B", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 40, U_Wert = 0.2,
                                                    Neigung = 30, Azimut = 180 } }
                }
            };
            Assert.True(zonenCtrl.SpeichernJeGebaeude(GEBAEUDE_B, b,
                new List<ZonenluftstromModel> { new ZonenluftstromModel { ID = -1, ID_ZoneA = -1, ID_ZoneB = -2, Volumenstrom = 120 } }).Ok);
            // Umordnen: der Rang läuft danach gegen die Reihenfolge der Ids; der Luftstrom bleibt stehen.
            List<ZoneModel> gelesen = zonenCtrl.LesenJeGebaeude(GEBAEUDE_B);
            Assert.True(zonenCtrl.SpeichernJeGebaeude(GEBAEUDE_B, new List<ZoneModel> { gelesen[2], gelesen[0], gelesen[1] }).Ok);
        }

        /// <summary>
        /// Ein weiteres Gebäude über den Weg der Gebäudeliste mit Importherkunft und Bauteilvorschlag
        /// (Stufe G4b: <c>WizardCtrl.GebaeudeZuordnungAnlegen</c> schreibt Projektkopie, Aufbauten, Zone,
        /// Bauteile und Herkunft samt Paarungen in EINEM Vorgang).
        /// </summary>
        /// <returns>Die Zuordnung (<c>Z_ProjektGebaeude.ID</c>) und die Projektkopie (<c>Tab_Gebaeude.ID</c>).</returns>
        internal static (int IdZ, int IdGebaeude) ImportUeberGebaeudeliste()
        {
            GebaeudeImportSatz satz = GbxmlImportTests.Satz(PROBE);
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag(PROBE);
            Assert.False(v.Abgelehnt);
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            var vorher = new HashSet<int>(liste.Select(z => z.ID_Z));
            Z_ProjGebModel vorlage = liste[0];
            liste.Add(new Z_ProjGebModel
            {
                ID_Z = 100000, ID_Projekt = PROJEKT, ID_Gebaeude_Stamm = vorlage.ID_Gebaeude_Stamm,
                Gebaeudename = vorlage.Gebaeudename, Wohnflaeche = 100, Einheit = "Wohnfläche [m²]", Jahresnutzungsgrad = 1,
                Importherkunft = new GebaeudeImportHerkunft(satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz), v)
            });
            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);
            Assert.True(ok, meldung);
            int idZ = Z_ProjGebCtrl.LiesProjekt(PROJEKT).Select(z => z.ID_Z).Single(id => !vorher.Contains(id));
            return (idZ, GebaeudeBedarfCtrl.TabGebaeudeId(idZ));
        }

        // =============================================================================
        //  Der Abdruck
        // =============================================================================

        /// <summary>Die Zeilen einer Tabelle des Projekts (bzw. eines Gebäudes), nach ID sortiert.</summary>
        internal static DataTable Zeilen(string tabelle, int projekt, int? gebaeude = null)
        {
            bool jeGebaeude = gebaeude.HasValue && AmGebaeude(tabelle);
            int wert = jeGebaeude ? gebaeude.Value : projekt;
            return DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" WHERE " + Filter(tabelle, jeGebaeude) + " ORDER BY ID",
                                               new DbParam("@p", wert));
        }

        /// <summary>Der Rang je Id der Zielzeilen eines Projekts.</summary>
        private static Dictionary<string, Dictionary<long, int>> Raenge(int projekt)
        {
            var r = new Dictionary<string, Dictionary<long, int>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<long, int> Rang(DataTable t)
            {
                var d = new Dictionary<long, int>();
                foreach (DataRow z in t.Rows) d[Convert.ToInt64(z["ID"], CultureInfo.InvariantCulture)] = d.Count;
                return d;
            }
            r["Tab_Projekt"] = new Dictionary<long, int> { { projekt, 0 } };
            r["Tab_Gebaeude"] = Rang(DataRepository.GetDataTable(GEBAEUDE_DES_PROJEKTS + " ORDER BY ID", new DbParam("@p", projekt)));
            foreach (string t in TABELLEN) r[t] = Rang(Zeilen(t, projekt));
            return r;
        }

        private static string Wert(object v)
        {
            if (v == null || v is DBNull) return "NULL";
            if (v is double d) return d.ToString("R", CultureInfo.InvariantCulture);
            if (v is float f) return ((double)f).ToString("R", CultureInfo.InvariantCulture);
            if (v is byte[] b) return Convert.ToBase64String(b);
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Der Abdruck je Tabelle: jede Zeile über JEDE Spalte als Text. <paramref name="ohneIds"/>: Die
        /// Verweise stehen als Rang der Zielzeile im Projekt („#3"), ein Verweis aus dem Projekt hinaus als
        /// „fremd"; sonst roh. Mit <paramref name="gebaeude"/> nur die Zeilen dieses Gebäudes (die
        /// Tabellen am Projekt ganz).
        /// </summary>
        internal static Dictionary<string, List<string>> Abdruck(int projekt, bool ohneIds, int? gebaeude = null)
        {
            Dictionary<string, Dictionary<long, int>> raenge = ohneIds ? Raenge(projekt) : null;
            var abdruck = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string t in TABELLEN)
            {
                DataTable dt = Zeilen(t, projekt, gebaeude);
                var zeilen = new List<string>();
                foreach (DataRow r in dt.Rows)
                {
                    var teile = new List<string>();
                    foreach (DataColumn c in dt.Columns)
                    {
                        object v = r[c];
                        string ziel = string.Equals(c.ColumnName, "ID", StringComparison.OrdinalIgnoreCase) ? t
                                      : VERWEISE.TryGetValue(c.ColumnName, out string z) ? z : null;
                        string text;
                        if (ohneIds && ziel != null && !(v is DBNull))
                            text = raenge[ziel].TryGetValue(Convert.ToInt64(v, CultureInfo.InvariantCulture), out int rang)
                                ? "#" + rang.ToString(CultureInfo.InvariantCulture) : "fremd:" + Wert(v);
                        else
                            text = Wert(v);
                        teile.Add(c.ColumnName + "=" + text);
                    }
                    zeilen.Add(string.Join("|", teile));
                }
                abdruck[t] = zeilen;
            }
            return abdruck;
        }

        /// <summary>Jede der acht Tabellen trägt Zeilen — sonst prüft der Rundlauf nichts.</summary>
        internal static void Belegt(Dictionary<string, List<string>> abdruck)
        {
            foreach (string t in TABELLEN)
                Assert.True(abdruck[t].Count > 0, t + " trägt in der Vorrichtung keine Zeile.");
        }

        /// <summary>Zwei Abdrücke sind gleich — Tabelle für Tabelle, Zeile für Zeile, Spalte für Spalte.</summary>
        internal static void Gleich(Dictionary<string, List<string>> erwartet, Dictionary<string, List<string>> ist, string weg)
        {
            foreach (string t in TABELLEN)
            {
                Assert.True(erwartet[t].Count == ist[t].Count,
                            weg + ", " + t + ": erwartet " + erwartet[t].Count + " Zeilen, gefunden " + ist[t].Count + ".");
                for (int i = 0; i < erwartet[t].Count; i++)
                    Assert.True(erwartet[t][i] == ist[t][i], weg + ", " + t + ", Zeile " + (i + 1) + ":\n  erwartet " + erwartet[t][i] + "\n  gefunden " + ist[t][i]);
            }
        }
    }

    /// <summary>
    /// <b>Stufe G6a, Welle 2 — der Rundlauf der Zonen über jeden Kopier- und Löschweg:</b> Duplikat,
    /// Variante, Transfer in eine Datenbank ohne Zonen (Zeilen zählen und Spaltengleichheit,
    /// Softwarearchitektur 2.6), Löschen eines Gebäudes, Löschen des Projekts und gewöhnliches
    /// Speichern (Gebäudeliste und OK im Editor ohne Änderung). Die Vorrichtung und der Abdruck über
    /// <c>SELECT *</c> stehen in <see cref="ZonenRundlauf"/>; die Kopierwege mit einem Gebäude halten
    /// <see cref="GebaeudeG3CtrlTests"/> über denselben Abdruck.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeZonenRundlaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static string Projektname()
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                             new DbParam("@p", ZonenRundlauf.PROJEKT)), CultureInfo.InvariantCulture);

        [Fact]
        public void Das_Projektduplikat_traegt_jede_Spalte()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            Dictionary<string, List<string>> quelle = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: true);
            Dictionary<string, List<string>> roh = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false);
            ZonenRundlauf.Belegt(quelle);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(Projektname(), Projektname() + " G6a");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");

            ZonenRundlauf.Gleich(quelle, ZonenRundlauf.Abdruck(neu, ohneIds: true), "Duplikat");
            ZonenRundlauf.Gleich(roh, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false), "Quelle nach dem Duplizieren");
        }

        [Fact]
        public void Die_Variante_traegt_jede_Spalte()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            Dictionary<string, List<string>> quelle = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: true);

            int neu = new VariantenCtrl().AnlegenAusStamm(ZonenRundlauf.PROJEKT, Projektname(), "G6a", out string fehler);
            Assert.True(neu > 0, "Variante nicht angelegt: " + fehler);

            ZonenRundlauf.Gleich(quelle, ZonenRundlauf.Abdruck(neu, ohneIds: true), "Variante");
        }

        /// <summary>
        /// Der Projekttransfer in eine Datenbank, die keine Zeile der acht Tabellen trägt: Nach dem
        /// Einlesen stehen dort genau die Zeilen des Pakets, und jede Spalte gleicht der Quelle
        /// (Softwarearchitektur 2.6, Abnahme des Transferwegs).
        /// </summary>
        [Fact]
        public void Der_Transfer_in_eine_Datenbank_ohne_Zonen_traegt_jede_Zeile_und_Spalte()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            Dictionary<string, List<string>> quelle = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: true);
            string name = Projektname();

            string ordner = Path.Combine(Path.GetTempPath(), "epos-g6a-transfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "g6a.wpx");
                Assert.True(new ProjektExportImportCtrl().Exportieren(name, paket));

                using var ziel = new TestDatenbank();
                Assert.True(ziel.Vorhanden);
                foreach (string t in ZonenRundlauf.TABELLEN)
                    Assert.True(ZonenRundlauf.Zahl(t) == 0, t + " trägt in der Zieldatenbank schon Zeilen.");

                int neu = new ProjektExportImportCtrl().Importieren(paket, name + " Transfer",
                    ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

                foreach (string t in ZonenRundlauf.TABELLEN)
                    Assert.True(ZonenRundlauf.Zahl(t) == quelle[t].Count,
                                t + ": erwartet " + quelle[t].Count + " Zeilen, die Zieldatenbank trägt " + ZonenRundlauf.Zahl(t) + ".");
                ZonenRundlauf.Gleich(quelle, ZonenRundlauf.Abdruck(neu, ohneIds: true), "Transfer");
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen kostet keinen Test */ }
            }
        }

        /// <summary>
        /// Das Löschen eines Gebäudes nimmt seine Zonen, Bauteile, Importquelle und Paarungen mit
        /// (Kaskade); das Nachbargebäude und die Projektzeilen (Aufbauten, Schichten, Stoffe) stehen
        /// Spalte für Spalte wie vorher.
        /// </summary>
        [Fact]
        public void Das_Loeschen_eines_Gebaeudes_nimmt_nur_seine_Zeilen_mit()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            Dictionary<string, List<string>> b = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, ZonenRundlauf.GEBAEUDE_B);
            Dictionary<string, List<string>> a = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, ZonenRundlauf.GEBAEUDE_A);
            Assert.All(ZonenRundlauf.TABELLEN.Where(ZonenRundlauf.AmGebaeude), t => Assert.NotEmpty(a[t]));

            Assert.True(new WizardCtrl().Del_Projekt_ZuordungGebäude(ZonenRundlauf.PROJEKT, ZonenRundlauf.GEBAEUDE_A));

            Dictionary<string, List<string>> nachA = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, ZonenRundlauf.GEBAEUDE_A);
            foreach (string t in ZonenRundlauf.TABELLEN.Where(ZonenRundlauf.AmGebaeude))
                Assert.True(nachA[t].Count == 0, t + " trägt nach dem Löschen des Gebäudes noch Zeilen.");
            ZonenRundlauf.Gleich(b, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, ZonenRundlauf.GEBAEUDE_B), "Nachbargebäude");
        }

        [Fact]
        public void Das_Loeschen_des_Projekts_nimmt_alle_Zeilen_mit()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            ZonenRundlauf.Belegt(ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false));

            ProjektCtrl.LoeschenMitVorarbeiten(ZonenRundlauf.PROJEKT, Projektname());

            foreach (string t in ZonenRundlauf.TABELLEN)
                Assert.True(ZonenRundlauf.Zahl(t) == 0, t + " trägt nach dem Löschen des Projekts noch Zeilen.");
        }

        /// <summary>
        /// <b>Der Importweg der Gebäudeliste</b> (Stufe G4b, <c>WizardCtrl.GebaeudeZuordnungAnlegen</c>):
        /// Ein Gebäude, das mit Herkunft und Bauteilvorschlag in die Liste kommt, trägt Zone, Bauteile,
        /// Aufbauten, Importquelle und Paarungen — und sie reisen über jeden Weg des Rundlaufs: Das
        /// Speichern der Liste lässt jede Spalte stehen, Duplikat und Transfer tragen jede Zeile und
        /// Spalte, das Entfernen des Gebäudes nimmt seine Zeilen mit und lässt die übrigen stehen.
        /// </summary>
        [Fact]
        public void Der_Import_ueber_die_Gebaeudeliste_reist_ueber_jeden_Weg()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            (int idZ, int idGebaeude) = ZonenRundlauf.ImportUeberGebaeudeliste();
            Dictionary<string, List<string>> importiert = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, idGebaeude);
            // Eine importierte Einzelzone hat keinen Luftstrom.
            Assert.All(ZonenRundlauf.TABELLEN.Where(ZonenRundlauf.AmGebaeude).Where(t => t != SchemaKatalog.TAB_ZONENLUFTSTROM),
                       t => Assert.NotEmpty(importiert[t]));
            Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude));

            Dictionary<string, List<string>> quelle = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: true);
            Dictionary<string, List<string>> roh = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false);
            string name = Projektname();

            // Gewoehnliches Speichern der Liste.
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(ZonenRundlauf.PROJEKT, Z_ProjGebCtrl.LiesProjekt(ZonenRundlauf.PROJEKT)).Gelungen);
            ZonenRundlauf.Gleich(roh, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false), "Gebäudeliste nach dem Import");

            // Duplikat.
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Import");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            ZonenRundlauf.Gleich(quelle, ZonenRundlauf.Abdruck(kopie, ohneIds: true), "Duplikat mit Import");

            // Transfer in eine Datenbank ohne Zonen.
            string ordner = Path.Combine(Path.GetTempPath(), "epos-g6a-import-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "g6a.wpx");
                Assert.True(new ProjektExportImportCtrl().Exportieren(name, paket));
                using (var ziel = new TestDatenbank())
                {
                    Assert.True(ziel.Vorhanden);
                    int neu = new ProjektExportImportCtrl().Importieren(paket, name + " Transfer",
                        ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                    Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                    foreach (string t in ZonenRundlauf.TABELLEN)
                        Assert.True(ZonenRundlauf.Zahl(t) == quelle[t].Count, t + ": Zeilenzahl nach dem Transfer.");
                    ZonenRundlauf.Gleich(quelle, ZonenRundlauf.Abdruck(neu, ohneIds: true), "Transfer mit Import");
                }
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen kostet keinen Test */ }
            }

            // Entfernen des importierten Gebaeudes: seine Zeilen fallen, die der uebrigen stehen.
            Dictionary<string, List<string>> a = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, ZonenRundlauf.GEBAEUDE_A);
            Assert.True(new WizardCtrl().Del_Projekt_ZuordungGebäude(ZonenRundlauf.PROJEKT, idZ));
            Dictionary<string, List<string>> nachImport = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, idGebaeude);
            foreach (string t in ZonenRundlauf.TABELLEN.Where(ZonenRundlauf.AmGebaeude))
                Assert.True(nachImport[t].Count == 0, t + " trägt nach dem Entfernen des importierten Gebäudes noch Zeilen.");
            ZonenRundlauf.Gleich(a, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false, ZonenRundlauf.GEBAEUDE_A), "Nachbargebäude");
        }

        /// <summary>
        /// Gewöhnliches Speichern ändert keine Spalte: die Gebäudeliste unverändert und mit geänderter
        /// Fläche (Startseite und Assistent gehen denselben Abgleich), und das OK des Gebäudeeditors über
        /// die Hülle, ohne dass an den Zonen etwas geändert ist — Schlüssel, Rang und jede Spalte bleiben.
        /// </summary>
        [Fact]
        public void Gewoehnliches_Speichern_laesst_jede_Spalte_stehen()
        {
            if (!_db.Vorhanden) return;
            ZonenRundlauf.Anlegen();
            Dictionary<string, List<string>> vorher = ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false);

            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(ZonenRundlauf.PROJEKT);
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(ZonenRundlauf.PROJEKT, liste).Gelungen);
            ZonenRundlauf.Gleich(vorher, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false), "Gebäudeliste");

            liste = Z_ProjGebCtrl.LiesProjekt(ZonenRundlauf.PROJEKT);
            liste[0].Wohnflaeche += 10;
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(ZonenRundlauf.PROJEKT, liste).Gelungen);
            ZonenRundlauf.Gleich(vorher, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false), "Gebäudeliste mit Fläche");

            foreach (Z_ProjGebModel z in Z_ProjGebCtrl.LiesProjekt(ZonenRundlauf.PROJEKT))
            {
                int idGebaeude = GebaeudeBedarfCtrl.TabGebaeudeId(z.ID_Z);
                GebaeudeZonenweg weg = GebaeudeKatalogHuelle.Zonenweg(ZonenRundlauf.PROJEKT, z.ID_Z, idGebaeude);
                var stand = new GebaeudeArbeitsstand();
                stand.ZonenLaden(weg.Zonen, true);
                Assert.Equal("", weg.Speichern!(stand.Zonen));
            }
            ZonenRundlauf.Gleich(vorher, ZonenRundlauf.Abdruck(ZonenRundlauf.PROJEKT, ohneIds: false), "OK im Editor");
        }
    }
}
