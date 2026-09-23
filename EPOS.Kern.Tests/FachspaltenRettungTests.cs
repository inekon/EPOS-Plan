using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Allgemein;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Block FS1 — die Rettung der FACHSPALTEN von <c>Tab_Energieanlagen</c> über den
    /// Speicherweg Löschen + Neuanlegen (<c>WizardCtrl.FachspaltenSichern</c> vor dem
    /// DELETE, <c>WizardCtrl.FachspaltenWiederherstellen</c> nach dem Add).
    ///
    /// <para><b>Befund 23.09.2026:</b> Die Rettung schrieb nur auf neue Zeilen, deren
    /// Fachspalten sämtlich NULL waren. Seit Schemaschritt 105 trägt jede neue Zeile
    /// <c>KWKG_Abwaermeabfuhr = 0</c> (<c>NOT NULL DEFAULT 0</c>) und galt damit als
    /// „schon belegt" — jedes Speichern über den Assistenten, eine Kachel oder ein
    /// Kontextmenü verlor die KWKG-, Steuer- und Quellangaben der Anlagen.</para>
    ///
    /// <para><b>Geprüft wird</b> der Weg des Anwenders (Assistent, Bearbeiten-Lauf ohne
    /// Änderung) und der Weg je Typ (Kachel, Kontextmenü), und zwar mit JEDER Fachspalte,
    /// die das Schema führt — eine künftige Spalte ist so von selbst mitgeprüft; dazu die
    /// Modellspalte <c>Kuehl_ID_Carrier</c> (Schritt 114) und eine nachgebildete künftige
    /// Spalte mit Vorgabe samt einer nullbaren, deren NULL nicht zur Vorgabe werden darf.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — jeder Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class FachspaltenRettungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Kaskade: zwei BHKW, ein Kessel, ein Puffer.</summary>
        private const int KASKADE = 1030;

        /// <summary>Das erste BHKW, der Kessel und der Puffer der Kaskade (Anlagenzeilen).</summary>
        private const int BHKW_1030 = 14920, KESSEL_1030 = 11334, PUFFER_1030 = 11331;

        /// <summary>Das Referenzprojekt mit Kühlung: Wärmepumpe, BHKW, Kessel, Stromspeicher.</summary>
        private const int KUEHLPROJEKT = 1017;

        /// <summary>Ein Stromträger des Katalogs (<c>pricing_model = 'ELECTRICITY'</c>).</summary>
        private const int STROMTRAEGER = 60;

        // =============================================================================
        //  Der Befund - der Weg des Anwenders mit den Angaben, die verloren gingen
        // =============================================================================

        /// <summary>
        /// Die gepflegten Angaben des Befunds — Abwärmeabfuhr, Stromkennzahl, Steuerwahl,
        /// Aufteilung und Hilfsenergie am BHKW, Temperaturmodus, Anschlusshöhe und
        /// Hilfsenergie am Kessel — und die gesäten KWKG-Angaben der Testdatenbank
        /// überstehen einen Bearbeiten-Lauf des Assistenten. Die Anlagenzeilen sind danach
        /// NEU angelegt, der Puffer steht (FR-1).
        /// </summary>
        [Fact]
        public void Das_Speichern_ueber_den_Assistenten_verliert_keine_KWKG_Steuer_und_Quellangaben()
        {
            if (!_db.Vorhanden) return;

            Setzen(BHKW_1030, "KWKG_Abwaermeabfuhr", 1L);
            Setzen(BHKW_1030, "KWKG_Stromkennzahl", 0.617);
            Setzen(BHKW_1030, "Hilfsenergie_Anteil", 1.5);
            Setzen(BHKW_1030, "Energiesteuer_Wahl", DbWerte.ENERGIESTEUER_WAHL_53A);
            Setzen(BHKW_1030, "Aufteilung_Methode", DbWerte.AUFTEILUNG_ENERGETISCH);
            Setzen(KESSEL_1030, "WQ_TemperaturModus", DbWerte.WQ_TEMPMODUS_FEST);
            Setzen(KESSEL_1030, "WQ_Anschlusshoehe", 0.42);
            Setzen(KESSEL_1030, "Hilfsenergie_Anteil", 2.5);

            string bhkw = Bezeichner(BHKW_1030), kessel = Bezeichner(KESSEL_1030);

            UeberDenAssistentenSpeichern(KASKADE);

            DataRow b = Anlage(KASKADE, WizardItemClass.BHKW_TYP, bhkw);
            Assert.NotEqual(BHKW_1030, Ganzzahl(b["ID"]));
            Assert.Equal(1L, Ganzzahl(b["KWKG_Abwaermeabfuhr"]));
            Assert.Equal(0.617, Kommazahl(b["KWKG_Stromkennzahl"]), 12);
            Assert.Equal(1.5, Kommazahl(b["Hilfsenergie_Anteil"]), 12);
            Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_53A, b["Energiesteuer_Wahl"]);
            Assert.Equal(DbWerte.AUFTEILUNG_ENERGETISCH, b["Aufteilung_Methode"]);

            // Der gesäte Bestand der Testdatenbank (E7c1/9).
            Assert.Equal(DbWerte.KWKG_ANLAGENART_NEU, b["KWKG_Anlagenart"]);
            Assert.Equal(30000.0, Kommazahl(b["KWKG_Vbh_Kontingent"]), 12);
            Assert.Equal(8.0, Kommazahl(b["KWKG_Satz_Einspeisung"]), 12);
            Assert.Equal(4.0, Kommazahl(b["KWKG_Satz_Eigen"]), 12);

            DataRow k = Anlage(KASKADE, WizardItemClass.KESSEL_TYP, kessel);
            Assert.NotEqual(KESSEL_1030, Ganzzahl(k["ID"]));
            Assert.Equal(DbWerte.WQ_TEMPMODUS_FEST, k["WQ_TemperaturModus"]);
            Assert.Equal(0.42, Kommazahl(k["WQ_Anschlusshoehe"]), 12);
            Assert.Equal(2.5, Kommazahl(k["Hilfsenergie_Anteil"]), 12);

            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = ?", PUFFER_1030));
        }

        // =============================================================================
        //  Jede Fachspalte des Schemas - auf dem Weg des Assistenten und je Typ
        // =============================================================================

        /// <summary>
        /// JEDE Spalte aus <see cref="WizardCtrl.Fachspalten"/> bekommt auf jeder
        /// Anlagenzeile des Projekts einen eigenen Wert abseits ihrer Vorgabe, die
        /// Wärmepumpen dazu einen Kühlträger; nach dem Speichern trägt jede Anlage
        /// (Typ, Bezeichner, n-tes Vorkommen) wieder genau diese Werte. <c>typ = 0</c> ist
        /// der Bearbeiten-Lauf des Assistenten, sonst der Weg der Kachel dieses Typs.
        /// Zeilen, die der Weg nicht neu schreibt (Puffer, andere Typen), bleiben stehen.
        /// </summary>
        [Theory]
        [InlineData(KASKADE, 0)]
        [InlineData(KASKADE, WizardItemClass.BHKW_TYP)]
        [InlineData(KUEHLPROJEKT, 0)]
        [InlineData(KUEHLPROJEKT, WizardItemClass.WP_TYP)]
        public void Jede_Fachspalte_uebersteht_den_Speicherweg(int projekt, int typ)
        {
            if (!_db.Vorhanden) return;

            List<string> fach = WizardCtrl.Fachspalten();
            Assert.Contains("KWKG_Abwaermeabfuhr", fach);
            Assert.DoesNotContain(KuehlungSchema.SPALTE_KUEHL_ID_CARRIER, fach, StringComparer.OrdinalIgnoreCase);

            AlleFachspaltenBelegen(projekt);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET [" + KuehlungSchema.SPALTE_KUEHL_ID_CARRIER + "] = ? " +
                "WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("?", STROMTRAEGER), new DbParam("?", projekt), new DbParam("?", WizardItemClass.WP_TYP));

            List<string> spalten = new List<string>(fach) { KuehlungSchema.SPALTE_KUEHL_ID_CARRIER };
            SortedDictionary<string, string> vorher = Abdruck(projekt, spalten);
            long hoechsteId = Zahl("SELECT MAX(ID) FROM Tab_Energieanlagen");

            if (typ == 0) UeberDenAssistentenSpeichern(projekt);
            else UeberDieKachelSpeichern(projekt, typ);

            // Der Weg hat Zeilen neu angelegt - sonst prüfte der Fall nichts.
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID > ?",
                             projekt, hoechsteId) > 0, "Der Speicherweg hat keine Anlagenzeile neu angelegt.");

            OhneAbweichung(vorher, Abdruck(projekt, spalten));
        }

        /// <summary>
        /// Wie ein künftiger Schemaschritt: eine Fachspalte <c>NOT NULL</c> mit Vorgabe —
        /// die Bauart von <c>KWKG_Abwaermeabfuhr</c> — und eine nullbare mit Vorgabe. Der
        /// gepflegte Wert kommt zurück, und ein NULL bleibt NULL, statt auf der neuen
        /// Zeile zur Vorgabe zu werden.
        /// </summary>
        [Fact]
        public void Eine_kuenftige_Fachspalte_mit_Vorgabe_haelt_die_Rettung_nicht_auf()
        {
            if (!_db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "ALTER TABLE Tab_Energieanlagen ADD COLUMN Probe_Vorgabe INTEGER NOT NULL DEFAULT 7"));
            Assert.True(DataRepository.ExecuteSQL(
                "ALTER TABLE Tab_Energieanlagen ADD COLUMN Probe_Text TEXT DEFAULT 'Vorgabe'"));
            Assert.Contains("Probe_Vorgabe", WizardCtrl.Fachspalten());
            Assert.Contains("Probe_Text", WizardCtrl.Fachspalten());

            Setzen(KESSEL_1030, "Probe_Vorgabe", 3L);
            Setzen(KESSEL_1030, "Probe_Text", DBNull.Value);
            Setzen(BHKW_1030, "Probe_Text", "eigen");
            string kessel = Bezeichner(KESSEL_1030), bhkw = Bezeichner(BHKW_1030);

            UeberDenAssistentenSpeichern(KASKADE);

            DataRow k = Anlage(KASKADE, WizardItemClass.KESSEL_TYP, kessel);
            Assert.Equal(3L, Ganzzahl(k["Probe_Vorgabe"]));
            Assert.True(k["Probe_Text"] == DBNull.Value, "NULL ist zur Vorgabe geworden: " + k["Probe_Text"]);

            DataRow b = Anlage(KASKADE, WizardItemClass.BHKW_TYP, bhkw);
            Assert.Equal(7L, Ganzzahl(b["Probe_Vorgabe"]));
            Assert.Equal("eigen", b["Probe_Text"]);
        }

        // =============================================================================
        //  Die beiden Speicherwege
        // =============================================================================

        /// <summary>
        /// Der Bearbeiten-Lauf des Assistenten ohne Änderung — die Seiten so geschaltet,
        /// wie der Komponentenschritt sie aus dem Bestand stellt, der Kopf wie die erste
        /// Seite ihn liefert (Muster <c>AssistentCtrlTests</c>).
        /// </summary>
        private static void UeberDenAssistentenSpeichern(int projekt)
        {
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", projekt)),
                CultureInfo.InvariantCulture);

            WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl.Aktueller = new WizardCtrl();

                AssistentCtrl a = new AssistentCtrl();
                a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
                a.ProjektId = projekt;
                a.Laden(name);

                KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(projekt);
                for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
                    a.SeiteSchalten(bestand[k].SeitenIndex, bestand[k].Vorhanden);

                ProjektKopfDaten kopf = ProjektCtrl.Kopf(name);
                Assert.NotNull(kopf);
                a.Kopf[0].Name = kopf.Name;
                a.Kopf[0].Beschreibung = kopf.Beschreibung;
                a.Kopf[0].Kunde = kopf.Kunde;
                a.Kopf[0].Bearbeiter = kopf.Bearbeiter;
                a.Kopf[0].Erstelldatum = kopf.Erstelldatum;
                a.Kopf[0].IdKlimaregion = kopf.IdKlimaregion;
                a.Kopf[0].Klimaname = kopf.Klimaname;

                AssistentErgebnis e = a.Speichern();
                Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
            }
            finally { WizardCtrl.Aktueller = vorherCtrl; }
        }

        /// <summary>Der Weg der Kachel und des Kontextmenüs: die Anlagen EINES Typs.</summary>
        private static void UeberDieKachelSpeichern(int projekt, int typ)
        {
            List<WErzeugerModel> liste = WErzeugerCtrl.ModelleJeTyp(projekt, typ);
            Assert.NotEmpty(liste);

            WizardCtrl wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(projekt, typ));
            Assert.True(wizard.Add_WP_Waermeerzeuger(projekt, liste));
        }

        // =============================================================================
        //  Belegen und vergleichen
        // =============================================================================

        /// <summary>
        /// Belegt jede Fachspalte auf jeder Anlagenzeile des Projekts mit einem eigenen
        /// Wert abseits ihrer Vorgabe: ein Schalter (<c>CHECK … IN (0,1)</c> oder
        /// <c>SchemaTypKatalog.BoolSpalten</c>) das Gegenteil seiner Vorgabe, ein Verweis
        /// einen vorhandenen Satz, eine Datumsspalte (<c>SchemaTypKatalog.DatumSpalten</c>)
        /// einen Zeitpunkt, sonst Zahl oder Text — je Zeile und Spalte verschieden, damit
        /// Namensdoppel und Vertauschungen auffallen. Eine künftige Spalte, die keiner
        /// dieser Bauarten folgt, scheitert hier mit ihrem Namen.
        /// </summary>
        private static void AlleFachspaltenBelegen(int projekt)
        {
            // Der Verweis WQ_ID_Quellprofil braucht einen Satz; die Testdatenbank führt keinen.
            DataRepository.ExecuteSQL("INSERT INTO Tab_Quellprofil (ID_Projekt, Bezeichner) VALUES (?, ?)",
                                      new DbParam("?", projekt), new DbParam("?", "FS1-Probe"));

            string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Tab_Energieanlagen'"),
                CultureInfo.InvariantCulture);
            DataTable info = DataRepository.GetDataTable(
                "SELECT name, type, dflt_value FROM pragma_table_info('Tab_Energieanlagen')");
            DataTable verweise = DataRepository.GetDataTable(
                "SELECT \"table\" AS Tabelle, \"from\" AS Spalte, \"to\" AS Ziel " +
                "FROM pragma_foreign_key_list('Tab_Energieanlagen')");

            List<string> fach = WizardCtrl.Fachspalten();
            List<long> ids = DataRepository.GetDataTable(
                    "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID", new DbParam("?", projekt))
                .Rows.Cast<DataRow>().Select(r => Ganzzahl(r["ID"])).ToList();
            Assert.NotEmpty(ids);

            for (int z = 0; z < ids.Count; z++)
            {
                string set = "";
                List<DbParam> ps = new List<DbParam>();
                for (int s = 0; s < fach.Count; s++)
                {
                    DataRow spalte = info.Rows.Cast<DataRow>().Single(r =>
                        string.Equals(Convert.ToString(r["name"], CultureInfo.InvariantCulture), fach[s],
                                      StringComparison.OrdinalIgnoreCase));
                    object wert = Probewert(fach[s], spalte, ddl, verweise, z, s);

                    set += (set.Length > 0 ? ", " : "") + "[" + fach[s] + "] = ?";
                    ps.Add(new DbParam("?", wert));
                }
                ps.Add(new DbParam("?", ids[z]));

                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET " + set + " WHERE ID = ?",
                                                      ps.ToArray()),
                            "Die Probewerte der Anlage " + ids[z] + " liessen sich nicht schreiben - folgt eine " +
                            "neue Fachspalte keiner der Bauarten in Probewert?");
            }
        }

        private static object Probewert(string name, DataRow spalte, string ddl, DataTable verweise, int zeile, int nr)
        {
            string typ = Convert.ToString(spalte["type"], CultureInfo.InvariantCulture).ToUpperInvariant();
            string vorgabe = spalte["dflt_value"] == DBNull.Value
                ? null : Convert.ToString(spalte["dflt_value"], CultureInfo.InvariantCulture);

            foreach (DataRow v in verweise.Rows)
            {
                if (!string.Equals(Convert.ToString(v["Spalte"], CultureInfo.InvariantCulture), name,
                                   StringComparison.OrdinalIgnoreCase)) continue;

                object ziel = DataRepository.ExecuteScalar(
                    "SELECT MIN([" + v["Ziel"] + "]) FROM [" + v["Tabelle"] + "]");
                Assert.False(ziel == null || ziel == DBNull.Value,
                             "Die Fachspalte " + name + " verweist auf " + v["Tabelle"] + ", dort steht kein Satz.");
                return ziel;
            }

            if (ddl.Contains("CHECK (\"" + name + "\" IN (0,1))", StringComparison.Ordinal) ||
                SchemaTypKatalog.BoolSpalten.Contains(name))
                return vorgabe == "1" ? 0L : 1L;

            object wert;
            if (SchemaTypKatalog.DatumSpalten.Contains(name))
                wert = new DateTime(2030, 1, 1).AddDays(40 * zeile + nr)
                                               .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            else if (typ.StartsWith("INT", StringComparison.Ordinal)) wert = (long)(100 * (zeile + 1) + nr);
            else if (typ == "REAL") wert = 1000.0 * (zeile + 1) + nr + 0.25;
            else wert = "F" + (zeile + 1).ToString(CultureInfo.InvariantCulture) +
                        "S" + nr.ToString(CultureInfo.InvariantCulture);

            Assert.NotEqual(vorgabe, Convert.ToString(wert, CultureInfo.InvariantCulture));
            return wert;
        }

        /// <summary>
        /// Die Werte der Spalten je Anlage — Schlüssel (Typ, Bezeichner, n-tes Vorkommen in
        /// Id-Reihenfolge), denn die Ids wechseln beim Neuanlegen.
        /// </summary>
        private static SortedDictionary<string, string> Abdruck(int projekt, List<string> spalten)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Type, Bezeichner, " + string.Join(", ", spalten.Select(s => "[" + s + "]")) +
                " FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID", new DbParam("?", projekt));

            SortedDictionary<string, string> abdruck = new SortedDictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, int> vorkommen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DataRow r in dt.Rows)
            {
                string anlage = Convert.ToString(r["ID_Type"], CultureInfo.InvariantCulture) + "|" +
                                Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture);
                vorkommen[anlage] = vorkommen.TryGetValue(anlage, out int n) ? n + 1 : 1;
                string schluessel = anlage + "|" + vorkommen[anlage].ToString(CultureInfo.InvariantCulture);

                foreach (string s in spalten)
                    abdruck[schluessel + "|" + s] = r[s] == DBNull.Value
                        ? "NULL" : Convert.ToString(r[s], CultureInfo.InvariantCulture);
            }
            return abdruck;
        }

        private static void OhneAbweichung(SortedDictionary<string, string> vorher,
                                           SortedDictionary<string, string> nachher)
        {
            List<string> abweichungen = new List<string>();
            foreach (KeyValuePair<string, string> kv in vorher)
            {
                if (!nachher.TryGetValue(kv.Key, out string neu))
                    abweichungen.Add(kv.Key + ": fehlt nach dem Speichern");
                else if (neu != kv.Value)
                    abweichungen.Add(kv.Key + ": " + kv.Value + " -> " + neu);
            }
            foreach (string schluessel in nachher.Keys)
                if (!vorher.ContainsKey(schluessel)) abweichungen.Add(schluessel + ": neu");

            Assert.True(abweichungen.Count == 0,
                        abweichungen.Count + " Abweichung(en):" + Environment.NewLine +
                        string.Join(Environment.NewLine, abweichungen.Take(40)));
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static void Setzen(int idAnlage, string spalte, object wert)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET [" + spalte + "] = ? WHERE ID = ?",
                new DbParam("?", wert), new DbParam("?", idAnlage)));
        }

        private static string Bezeichner(int idAnlage)
        {
            return Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("?", idAnlage)),
                CultureInfo.InvariantCulture);
        }

        /// <summary>Die EINE Anlagenzeile dieses Typs und Bezeichners nach dem Speichern.</summary>
        private static DataRow Anlage(int projekt, int typ, string bezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                new DbParam("?", projekt), new DbParam("?", typ), new DbParam("?", bezeichner));
            Assert.Equal(1, dt.Rows.Count);
            return dt.Rows[0];
        }

        private static long Zahl(string sql, params object[] werte)
        {
            object v = DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray());
            return v == null || v == DBNull.Value ? 0L : Convert.ToInt64(v, CultureInfo.InvariantCulture);
        }

        private static long Ganzzahl(object v) => Convert.ToInt64(v, CultureInfo.InvariantCulture);

        private static double Kommazahl(object v) => Convert.ToDouble(v, CultureInfo.InvariantCulture);
    }
}
