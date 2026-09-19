using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die fuenf Proben des PROJEKTTRANSFERS (iU9-W15a.0j, Befund W15a-B34).
    ///
    /// <para><b>Warum es sie gibt.</b> Der einzige jemals gelaufene Export/Import-Nachweis
    /// (<c>kd1runner transfer</c>, „17/17 PASS", <c>Konzept_Projekttransfer_EPOS-Plan.md:192</c>)
    /// lag in einem Scratchpad und ist verloren. Bis zu dieser Welle rief KEIN Test
    /// <c>Exportieren</c> oder <c>Importieren</c> auch nur auf — und genau dieser
    /// Controller (1 278 Zeilen) zieht mit iU9-W15a in den Kern um. Die Proben entstehen
    /// deshalb VOR dem Umzug und laufen danach unveraendert erneut (Risiko R-W15a-2).</para>
    ///
    /// <para><b>Warum nicht „bitgleich".</b> Ein Paket ist als GANZES nicht reproduzierbar:
    /// <c>exportedUtc</c> im Manifest, die Eintragszeitstempel des ZIP und — der
    /// eigentliche Grund — der Import vergibt bewusst NEUE Ids (Befund W15a-B33). Was
    /// bitgleich sein KANN, sind die JSON-Eintraege selbst; alles Uebrige wird ueber die
    /// Kriterien des Konzept-Pruefstands geprueft: Zeilenzahlen, FK-Integritaet,
    /// Variantenverknuepfung, Versionsabweisung.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Probe.</b> Alle fuenf schreiben (schon der Export
    /// oeffnet einen Vorgang); geteilt wuerde ein Fall den naechsten sehen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjekttransferTests
    {
        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030).</summary>
        private const string PROJEKT = "Referenz BHKW-Kaskade (Regressionstest)";

        /// <summary>Ein Stammprojekt mit ZWEI Varianten im Testbestand (Id 1019).</summary>
        private const string STAMM = "Wöhler";
        private const string VARIANTE_1 = "Wöhler - Test1";
        private const string VARIANTE_2 = "Wöhler - Test2";

        // =============================================================================
        //  P1 — Determinismus des Pakets
        // =============================================================================
        [Fact]
        public void P1_Zwei_Exporte_desselben_Projekts_liefern_dasselbe_Paket()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string a = ordner.Datei("a.wpx");
            string b = ordner.Datei("b.wpx");

            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, a));
            Assert.True(io.Exportieren(PROJEKT, b));

            Dictionary<string, byte[]> ea = Eintraege(a);
            Dictionary<string, byte[]> eb = Eintraege(b);

            // Gleiche Eintragsnamen, und jeder ausser dem Manifest byteweise gleich.
            Assert.Equal(ea.Keys.OrderBy(k => k, StringComparer.Ordinal),
                         eb.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Contains("manifest.json", ea.Keys);
            Assert.True(ea.Count > 1, "Das Paket traegt ausser dem Manifest keine Daten.");

            foreach (string name in ea.Keys.Where(k => k != "manifest.json"))
                Assert.True(ea[name].SequenceEqual(eb[name]),
                            "Eintrag " + name + " unterscheidet sich zwischen zwei Exporten.");

            // Das Manifest unterscheidet sich NUR im Ausgabezeitpunkt.
            Assert.Equal(OhneZeitstempel(ea["manifest.json"]), OhneZeitstempel(eb["manifest.json"]));
        }

        // =============================================================================
        //  P2 — Rundreise-Zaehlung (Pruefstand-Punkt 2)
        // =============================================================================
        [Fact]
        public void P2_Nach_der_Rundreise_stimmen_die_Zeilenzahlen_je_Pakettabelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("rund.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(quelle > 0);

            int neu = io.Importieren(paket, "Rundreise P2", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.NotEqual(quelle, neu);

            Dictionary<string, int> imPaket = PaketZeilen(paket);
            Assert.NotEmpty(imPaket);

            var plan = Plan();
            foreach (var kvp in imPaket)
            {
                Assert.True(plan.ContainsKey(kvp.Key), "Pakettabelle " + kvp.Key + " steht nicht im Plan.");
                int imZiel = Zaehle(plan[kvp.Key], neu);
                Assert.True(kvp.Value == imZiel,
                            kvp.Key + ": " + kvp.Value + " Zeilen im Paket, " + imZiel + " im Ziel.");

                // Quelle == Ziel. Einzige Ausnahme ist Tab_ProjektWerte: der Export laesst
                // Kostenpositionen ohne gueltige Anlagenzuordnung bewusst zurueck (T6).
                int inQuelle = Zaehle(plan[kvp.Key], quelle);
                if (kvp.Key.Equals("Tab_ProjektWerte", StringComparison.OrdinalIgnoreCase))
                    Assert.True(inQuelle >= imZiel, "Tab_ProjektWerte: Ziel hat mehr Zeilen als die Quelle.");
                else
                    Assert.True(inQuelle == imZiel,
                                kvp.Key + ": Quelle " + inQuelle + ", Ziel " + imZiel + ".");
            }
        }

        // =============================================================================
        //  P3 — Rundreise-Integritaet (Pruefstand-Punkt 2, FK-Seite)
        // =============================================================================
        [Fact]
        public void P3_Nach_der_Rundreise_gibt_es_keine_verwaisten_Verweise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("integritaet.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            int neu = io.Importieren(paket, "Rundreise P3", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            var plan = Plan();
            var waisen = new List<string>();
            int geprueft = 0;

            foreach (string tabelle in PaketZeilen(paket).Keys)
            {
                string filter = string.Format(plan[tabelle].Filter, neu);
                foreach ((string spalte, string zielTab, string zielSpalte) in Fremdschluessel(tabelle))
                {
                    geprueft++;
                    object o = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM [" + tabelle + "] AS k WHERE (" + filter + ") " +
                        "AND k.[" + spalte + "] IS NOT NULL AND k.[" + spalte + "] <> 0 " +
                        "AND NOT EXISTS (SELECT 1 FROM [" + zielTab + "] AS e " +
                        "WHERE e.[" + zielSpalte + "] = k.[" + spalte + "])");
                    int zahl = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                    if (zahl > 0) waisen.Add(tabelle + "." + spalte + " -> " + zielTab + ": " + zahl);
                }
            }

            Assert.True(geprueft > 0, "Es wurde kein einziger Fremdschluessel geprueft.");
            Assert.True(waisen.Count == 0, "Verwaiste Verweise: " + string.Join(", ", waisen));

            // Ae24: keine Kostenposition, die auf eine Anlage eines FREMDEN Projekts zeigt.
            object lose = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte AS p WHERE p.ProjektID = " + neu +
                " AND p.ID_Anlage IS NOT NULL AND p.ID_Anlage <> 0 " +
                "AND NOT EXISTS (SELECT 1 FROM Tab_Energieanlagen AS a " +
                "WHERE a.ID = p.ID_Anlage AND a.ID_Projekt = " + neu + ")");
            Assert.Equal(0, lose == null || lose == DBNull.Value ? 0 : Convert.ToInt32(lose));
        }

        // =============================================================================
        //  P4 — Variantenpaket (T3, Pruefstand-Punkt 4)
        // =============================================================================
        [Fact]
        public void P4_Ein_Variantenpaket_verknuepft_die_importierten_Projekte_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("varianten.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(STAMM, new List<string> { VARIANTE_1, VARIANTE_2 }, paket));

            // Das Manifest fuehrt zwei Varianten-Baeume und zwei Verknuepfungen.
            JsonElement man = Manifest(paket);
            Assert.Equal(2, man.GetProperty("formatVersion").GetInt32());
            Assert.Equal(2, man.GetProperty("variants").GetArrayLength());
            Assert.Equal(2, man.GetProperty("variantLinks").GetArrayLength());

            int neu = io.Importieren(paket, "Wöhler P4", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            // Tab_Variante zeigt auf die IMPORTIERTEN Projekte, nicht auf die Quelle.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT v.ID_Projekt, v.Variantenname, p.Projektname " +
                "FROM Tab_Variante AS v INNER JOIN Tab_Projekt AS p ON v.ID_Projekt = p.ID " +
                "WHERE v.ID_ProjektRef = " + neu + " ORDER BY v.Variantenname");
            Assert.NotNull(dt);
            Assert.Equal(2, dt.Rows.Count);

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(STAMM);
            foreach (DataRow r in dt.Rows)
            {
                int idVariante = Convert.ToInt32(r["ID_Projekt"]);
                Assert.NotEqual(quelle, idVariante);
                Assert.True(idVariante > 0);
                // Die importierte Variante ist ein EIGENES Projekt und traegt Zeilen.
                Assert.True(Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + idVariante)) == 1);
            }
            Assert.Equal(new[] { "Test1", "Test2" },
                         dt.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["Variantenname"])).ToArray());
        }

        // =============================================================================
        //  P5 — Versions-Ablehnung (B2/TF4)
        // =============================================================================
        [Fact]
        public void P5_Ein_Paket_mit_fremdem_Schemastand_wird_abgelehnt_ein_Altpaket_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("version.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            // Das Paket traegt den echten Migrationsstand.
            Assert.Equal(SchemaStand.Zielversion, Manifest(paket).GetProperty("schemaVersion").GetInt32());

            string fremd = ordner.Datei("fremd.wpx");
            SchreibeMitSchemastand(paket, fremd, SchemaStand.Zielversion - 1);
            int abgelehnt = io.Importieren(fremd, "Version P5a",
                                           ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.Equal(-1, abgelehnt);
            Assert.Contains("Schemastand " + (SchemaStand.Zielversion - 1), fehler, StringComparison.Ordinal);
            Assert.Contains("Stand " + SchemaStand.Zielversion, fehler, StringComparison.Ordinal);

            // schemaVersion 0 ist ein V1-Altpaket (vor T2 exportiert) und bleibt zugelassen.
            string alt = ordner.Datei("alt.wpx");
            SchreibeMitSchemastand(paket, alt, 0);
            int angenommen = io.Importieren(alt, "Version P5b",
                                            ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler2);
            Assert.True(angenommen > 0, "Altpaket abgelehnt: " + fehler2);
        }

        // =============================================================================
        //  P6 — Wechselrichter und Straenge reisen mit (Stufe S2, W6-E-2)
        // =============================================================================

        /// <summary>
        /// <b>Die zwei neuen Tabellen der Stufe S2 im Paket</b> (Konzept
        /// Wechselrichter, S2.5): die Projektkopie <c>Tab_Wechselrichter</c> — sie führt
        /// ein eigenes <c>ID_Projekt</c> und kommt damit über den GENERISCHEN Weg mit —
        /// und die Strangliste <c>Z_AnlageStrang</c>, die an der Anlage hängt und
        /// deshalb einen festen Eintrag in <c>ProjektDuplizierenCtrl.KINDER</c> braucht.
        ///
        /// <para><b>Warum eine eigene Probe.</b> P2 und P3 prüfen jede Pakettabelle,
        /// aber nur die, die im Paket auch VORKOMMT — und das Regressionsprojekt führt
        /// weder einen Wechselrichter noch einen Strang. Ohne diesen Fall wären die zwei
        /// Tabellen also grün, indem sie fehlen. Der Fall legt deshalb erst einen
        /// Gerätesatz und eine Strangzeile an und exportiert danach.</para>
        ///
        /// <para><b>Und er prüft den VERSATZ:</b> Die importierte Strangzeile muss auf
        /// die Projektkopie des ZIELprojekts zeigen, nicht auf die der Quelle. Genau das
        /// leistet der Eintrag <c>ID_Wechselrichter</c> in <c>FK_MAP</c>; ohne ihn
        /// rechnete die Kopie mit dem Gerät des fremden Projekts, und weil
        /// <c>Tab_Wechselrichter</c> keinen Fremdschlüssel auf <c>Tab_Projekt</c> führt,
        /// fiele es nicht einmal beim Einfügen auf.</para>
        /// </summary>
        [Fact]
        public void P6_Wechselrichter_und_Straenge_reisen_mit_und_zeigen_auf_die_eigene_Kopie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(quelle > 0);

            // --- Gerätekopie und Strangzeile im QUELLprojekt anlegen -------------------
            int geraet = DataRepository.GetMaxID(SchemaKatalog.TAB_WECHSELRICHTER) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + SchemaKatalog.TAB_WECHSELRICHTER + "] " +
                "(ID, ID_Projekt, Bezeichner, P_AC_Nenn) VALUES (?,?,?,?)",
                new DbParam("@id", geraet), new DbParam("@p", quelle),
                new DbParam("@b", "Transferprobe 2500TL"), new DbParam("@n", 2.5)));

            object anlage = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN + " WHERE ID_Projekt = ?",
                new DbParam("@p", quelle));
            int idAnlage = Convert.ToInt32(anlage);
            Assert.True(idAnlage > 0);

            var strangCtrl = new AnlageStrangCtrl();
            Assert.True(strangCtrl.SchreibenJeAnlage(idAnlage, new List<AnlageStrangModel>
            {
                new AnlageStrangModel { Bezeichner = "Dach Süd", ID_Wechselrichter = geraet,
                                        Mppt = 1, Module_Reihe = 10 }
            }));

            // --- Rundreise -----------------------------------------------------------
            string paket = ordner.Datei("wechselrichter.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            Dictionary<string, int> imPaket = PaketZeilen(paket);
            Assert.True(imPaket.ContainsKey(SchemaKatalog.TAB_WECHSELRICHTER),
                        "Das Paket fuehrt " + SchemaKatalog.TAB_WECHSELRICHTER + " nicht.");
            Assert.True(imPaket.ContainsKey(SchemaKatalog.Z_ANLAGESTRANG),
                        "Das Paket fuehrt " + SchemaKatalog.Z_ANLAGESTRANG + " nicht.");
            Assert.Equal(1, imPaket[SchemaKatalog.TAB_WECHSELRICHTER]);
            Assert.Equal(1, imPaket[SchemaKatalog.Z_ANLAGESTRANG]);

            int neu = io.Importieren(paket, "Wechselrichter P6",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.NotEqual(quelle, neu);

            // --- Die Strangzeile des ZIELprojekts zeigt auf DESSEN Gerätekopie ---------
            List<AnlageStrangModel> gereist = strangCtrl.LesenJeProjekt(neu);
            AnlageStrangModel z = Assert.Single(gereist);
            Assert.Equal("Dach Süd", z.Bezeichner);
            Assert.Equal(10, z.Module_Reihe);
            Assert.NotEqual(geraet, z.ID_Wechselrichter);

            object projektDesGeraets = DataRepository.ExecuteScalar(
                "SELECT ID_Projekt FROM [" + SchemaKatalog.TAB_WECHSELRICHTER + "] WHERE ID = ?",
                new DbParam("@id", z.ID_Wechselrichter.Value));
            Assert.Equal(neu, Convert.ToInt32(projektDesGeraets));
        }

        /// <summary>
        /// <b>Ein Altpaket ohne die zwei Tabellen lädt weiter</b> (Konzept S2.5): Der
        /// Import ist tolerant — was im Paket fehlt, entsteht im Ziel nicht, und das ist
        /// kein Fehler. Das Regressionsprojekt führt weder Wechselrichter noch Strang;
        /// sein Paket ist damit selbst der Altfall.
        /// </summary>
        [Fact]
        public void P7_Ein_Paket_ohne_Wechselrichter_und_Straenge_laedt_weiter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("ohne.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            Dictionary<string, int> imPaket = PaketZeilen(paket);
            Assert.False(imPaket.ContainsKey(SchemaKatalog.Z_ANLAGESTRANG) &&
                         imPaket[SchemaKatalog.Z_ANLAGESTRANG] > 0);

            int neu = io.Importieren(paket, "Ohne Straenge P7",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.Empty(new AnlageStrangCtrl().LesenJeProjekt(neu));
        }

        // =============================================================================
        //  P8 bis P13 — die Verguetung beim Transfer (Konzept § 2.16)
        // =============================================================================

        /// <summary>
        /// <b>Die Beilage</b> (§ 2.16, Randfall Projekttransfer). Eine EINZELN
        /// transferierte Variante, welche die PV-Verguetung ihres Stamms uebernimmt,
        /// faende am Ziel weder Stamm noch Zeile: Sie rechnete still den flachen
        /// Einspeisesatz. Das Paket traegt deshalb die geltende Zeile des Stamms als
        /// eigenen Abschnitt mit, und der Import macht daraus die EIGENEN Werte der
        /// importierten Variante (<c>Uebernahme_Stamm = 0</c>).
        /// </summary>
        [Fact]
        public void P8_Eine_uebernehmende_Variante_bringt_die_Stammverguetung_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int idStamm = Id(STAMM);
            PvLoeschen(idStamm); PvLoeschen(Id(VARIANTE_1));
            PvSchreiben(idStamm, aktiv: true, aw: 7.5);

            string paket = ordner.Datei("beilage.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(VARIANTE_1, paket));

            JsonElement abschnitt = Manifest(paket).GetProperty("pvVerguetungStamm");
            Assert.Equal(1, abschnitt.GetArrayLength());
            Assert.Equal(VARIANTE_1, abschnitt[0].GetProperty("projekt").GetString());
            Assert.Equal(STAMM, abschnitt[0].GetProperty("stamm").GetString());
            Assert.Contains("pvstamm/0.json", Eintraege(paket).Keys);

            // Am Ziel ist der Stamm nicht auffindbar — die Verknuepfung scheitert.
            int neu; string fehler;
            Umbenennen(idStamm, STAMM + " (nicht im Ziel)");
            try
            {
                neu = io.Importieren(paket, "Beilage P8",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out fehler);
            }
            finally { Umbenennen(idStamm, STAMM); }
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            var ctrl = new ProjektPhotovoltaikCtrl();
            ProjektPhotovoltaikModel eigen = ctrl.Lies(neu);
            Assert.NotNull(eigen);
            Assert.False(eigen.UebernahmeStamm);
            Assert.True(eigen.Aktiv);
            Assert.Equal(7.5, eigen.AwOverride);
            Assert.Equal(neu, eigen.ID_Projekt);
            Assert.Contains(io.LetzterBericht,
                            z => z == string.Format(Ressource("TRANSFER_PV_BEILAGE"), STAMM));
        }

        /// <summary>
        /// <b>Steht der Stamm am Ziel, bleibt die Beilage liegen</b> (§ 2.16): Die
        /// Verknuepfung wird hergestellt, die importierte Variante uebernimmt wie zuvor
        /// — sie bekommt KEINE eigene Zeile, und die Aufloesung liefert die Werte des
        /// Stamms.
        /// </summary>
        [Fact]
        public void P9_Steht_der_Stamm_am_Ziel_bleibt_die_Beilage_liegen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int idStamm = Id(STAMM);
            PvLoeschen(idStamm); PvLoeschen(Id(VARIANTE_1));
            PvSchreiben(idStamm, aktiv: true, aw: 7.5);

            string paket = ordner.Datei("mitstamm.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(VARIANTE_1, paket));

            int neu = io.Importieren(paket, "Mit Stamm P9",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            var ctrl = new ProjektPhotovoltaikCtrl();
            Assert.Null(ctrl.Lies(neu));
            Assert.Equal(idStamm, new VariantenCtrl().StammRefDerVariante(neu));

            PvVerguetungStand stand = ctrl.LiesAufgeloest(neu);
            Assert.True(stand.Uebernommen);
            Assert.Equal(7.5, stand.Modell.AwOverride);
        }

        /// <summary>
        /// <b>Ohne aktive Stammzeile gibt es keine Beilage</b> (§ 2.16): Es ist nichts
        /// zu uebernehmen — beide rechnen Flat. Der Import sagt es, statt es still zu
        /// lassen, und die Variante bleibt ohne Zeile.
        /// </summary>
        [Fact]
        public void P10_Ohne_Stammzeile_gibt_es_keine_Beilage_sondern_eine_Meldung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int idStamm = Id(STAMM);
            PvLoeschen(idStamm); PvLoeschen(Id(VARIANTE_1));

            string paket = ordner.Datei("ohnebeilage.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(VARIANTE_1, paket));

            Assert.Equal(0, Manifest(paket).GetProperty("pvVerguetungStamm").GetArrayLength());
            Assert.DoesNotContain("pvstamm/0.json", Eintraege(paket).Keys);

            int neu; string fehler;
            Umbenennen(idStamm, STAMM + " (nicht im Ziel)");
            try
            {
                neu = io.Importieren(paket, "Ohne Beilage P10",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out fehler);
            }
            finally { Umbenennen(idStamm, STAMM); }
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            Assert.Null(new ProjektPhotovoltaikCtrl().Lies(neu));
            Assert.Contains(io.LetzterBericht, z => z == Ressource("TRANSFER_PV_OHNE_BEILAGE"));
        }

        /// <summary>
        /// <b>Die Kohaerenzzeile zum fehlenden Stamm</b> (§ 2.16, § 3.9, ohne
        /// Rechenwirkung). Sie gilt allgemein, nicht nur nach einem Import — in beiden
        /// Lagen, in denen die Wahl „uebernehmen" ins Leere zeigt: die Verknuepfung
        /// weist auf ein Projekt, das es nicht gibt, oder es gibt gar keine
        /// Verknuepfung mehr (die Spur eines Einzeltransfers).
        /// </summary>
        [Fact]
        public void P11_Eine_Variante_ohne_Stammprojekt_bekommt_die_Kohaerenzzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idStamm = Id(STAMM);
            int idVariante = Id(VARIANTE_1);
            PvLoeschen(idStamm); PvLoeschen(idVariante);

            string erwartetVariante = string.Format(Ressource("KOH_PV_STAMM_FEHLT"), VARIANTE_1);
            string erwartetStamm = string.Format(Ressource("KOH_PV_STAMM_FEHLT"), STAMM);

            // (a) Die Verknuepfung zeigt auf ein Projekt, das es nicht gibt.
            Assert.DoesNotContain(KohaerenzPruefung.Pruefe(idVariante, null),
                                  h => h.Text == erwartetVariante);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Variante SET ID_ProjektRef = 999999 WHERE ID_Projekt = " + idVariante);
            try
            {
                Assert.Contains(KohaerenzPruefung.Pruefe(idVariante, null),
                                h => h.Text == erwartetVariante);
            }
            finally
            {
                DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_Variante SET ID_ProjektRef = " + idStamm +
                    " WHERE ID_Projekt = " + idVariante);
            }

            // (b) Keine Verknuepfung, aber die Wahl „uebernehmen" an einer nicht
            //     angewendeten Zeile — so kommt eine einzeln transferierte Variante an.
            Assert.DoesNotContain(KohaerenzPruefung.Pruefe(idStamm, null), h => h.Text == erwartetStamm);
            PvSchreiben(idStamm, aktiv: false, aw: 7.5, uebernahme: true);
            Assert.Contains(KohaerenzPruefung.Pruefe(idStamm, null), h => h.Text == erwartetStamm);
            PvLoeschen(idStamm);
        }

        /// <summary>
        /// <b>Der Variantenbaum bleibt, wie er ist</b> (§ 2.16): Stamm und Wahl reisen
        /// mit, es gibt nichts beizulegen. Am Ziel uebernimmt die eine Variante weiter
        /// vom importierten Stamm, die andere behaelt ihre eigenen Werte.
        /// </summary>
        [Fact]
        public void P12_Ein_Variantenbaum_traegt_Stamm_und_Wahl_ohne_Beilage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int idStamm = Id(STAMM);
            PvLoeschen(idStamm); PvLoeschen(Id(VARIANTE_1)); PvLoeschen(Id(VARIANTE_2));
            PvSchreiben(idStamm, aktiv: true, aw: 7.5);
            PvSchreiben(Id(VARIANTE_2), aktiv: true, aw: 9.0);

            string paket = ordner.Datei("baum.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(STAMM, new List<string> { VARIANTE_1, VARIANTE_2 }, paket));
            Assert.Equal(0, Manifest(paket).GetProperty("pvVerguetungStamm").GetArrayLength());

            int neu = io.Importieren(paket, "Baum P12",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            var ctrl = new ProjektPhotovoltaikCtrl();
            Assert.Equal(7.5, ctrl.Lies(neu).AwOverride);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Projekt, Variantenname FROM Tab_Variante " +
                "WHERE ID_ProjektRef = " + neu + " ORDER BY Variantenname");
            Assert.NotNull(dt);
            Assert.Equal(2, dt.Rows.Count);
            int idTest1 = Convert.ToInt32(dt.Rows[0]["ID_Projekt"]);
            int idTest2 = Convert.ToInt32(dt.Rows[1]["ID_Projekt"]);

            // Test1 uebernimmt (keine eigene Zeile), Test2 fuehrt eigene Werte.
            Assert.Null(ctrl.Lies(idTest1));
            PvVerguetungStand uebernommen = ctrl.LiesAufgeloest(idTest1);
            Assert.True(uebernommen.Uebernommen);
            Assert.Equal(7.5, uebernommen.Modell.AwOverride);

            PvVerguetungStand eigen = ctrl.LiesAufgeloest(idTest2);
            Assert.False(eigen.Uebernommen);
            Assert.Equal(9.0, eigen.Modell.AwOverride);
        }

        /// <summary>
        /// <b>Ein Altpaket ohne den Abschnitt laedt unveraendert</b> (§ 2.16): Der
        /// Manifestteil ist kein Pflichtteil; fehlt er, laeuft der Import wie zuvor —
        /// die Variante steht eigenstaendig und ohne Zeile da.
        /// </summary>
        [Fact]
        public void P13_Ein_Paket_ohne_den_Abschnitt_importiert_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int idStamm = Id(STAMM);
            PvLoeschen(idStamm); PvLoeschen(Id(VARIANTE_1));
            PvSchreiben(idStamm, aktiv: true, aw: 7.5);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(VARIANTE_1, paket));

            string alt = ordner.Datei("alt.wpx");
            OhnePvAbschnitt(paket, alt);
            Assert.False(Manifest(alt).TryGetProperty("pvVerguetungStamm", out _));

            int neu; string fehler;
            Umbenennen(idStamm, STAMM + " (nicht im Ziel)");
            try
            {
                neu = io.Importieren(alt, "Altpaket P13",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out fehler);
            }
            finally { Umbenennen(idStamm, STAMM); }
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.Null(new ProjektPhotovoltaikCtrl().Lies(neu));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>Ein Ordner fuer die Paketdateien einer Probe; er raeumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad;

            public Arbeitsordner()
            {
                _pfad = Path.Combine(Path.GetTempPath(),
                                     "epos-transfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(_pfad);
            }

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        private static int Id(string projektname) => new ProjektDuplizierenCtrl().GetProjektId(projektname);

        /// <summary>Eine PV-Verguetungszeile fuer das Projekt — die Werte, auf die
        /// die Proben pruefen.</summary>
        private static void PvSchreiben(int idProjekt, bool aktiv, double aw, bool uebernahme = false)
        {
            var ctrl = new ProjektPhotovoltaikCtrl();
            ProjektPhotovoltaikModel m = ctrl.LiesOderVorbelegt(idProjekt);
            m.ID_Projekt = idProjekt;
            m.Aktiv = aktiv;
            m.AwOverride = aw;
            m.Inbetriebnahme = new DateTime(2026, 1, 1);
            m.UebernahmeStamm = uebernahme;
            Assert.True(ctrl.Speichern(m));
        }

        private static void PvLoeschen(int idProjekt) =>
            DataRepository.ExecuteNonQuery(
                "DELETE FROM " + SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK + " WHERE ID_Projekt = " + idProjekt);

        /// <summary>Benennt ein Projekt um — so wird der Stamm am Ziel unauffindbar,
        /// ohne die Arbeitskopie sonst anzufassen.</summary>
        private static void Umbenennen(int idProjekt, string name) =>
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                new DbParam("@n", name), new DbParam("@id", idProjekt)));

        /// <summary>Der Ressourcentext, genau so gelesen wie im Kern (ohne Kulturwahl).</summary>
        private static string Ressource(string schluessel) =>
            WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel) ?? "";

        /// <summary>Schreibt das Paket ohne den Abschnitt <c>pvVerguetungStamm</c> und
        /// ohne die Beilagen neu — ein Paket, wie es vor diesem Abschnitt entstand.</summary>
        private static void OhnePvAbschnitt(string quelle, string ziel)
        {
            Dictionary<string, byte[]> eintraege = Eintraege(quelle);
            JsonObject wurzel = JsonNode.Parse(Text(eintraege["manifest.json"])).AsObject();
            wurzel.Remove("pvVerguetungStamm");
            string manifest = wurzel.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            using var stream = new FileStream(ziel, FileMode.Create);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (var kvp in eintraege)
            {
                if (kvp.Key.StartsWith("pvstamm/", StringComparison.Ordinal)) continue;
                using var s = zip.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                byte[] roh = kvp.Key == "manifest.json"
                    ? new UTF8Encoding(false).GetBytes(manifest) : kvp.Value;
                s.Write(roh, 0, roh.Length);
            }
        }

        private static Dictionary<string, byte[]> Eintraege(string paket)
        {
            var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using var zip = ZipFile.OpenRead(paket);
            foreach (ZipArchiveEntry e in zip.Entries)
            {
                using var s = e.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                map[e.FullName] = ms.ToArray();
            }
            return map;
        }

        private static string Text(byte[] roh) => new UTF8Encoding(false).GetString(roh);

        /// <summary>Das Manifest ohne die Zeile <c>exportedUtc</c> — alles Uebrige ist bestimmt.</summary>
        private static string OhneZeitstempel(byte[] manifest) =>
            string.Join("\n", Text(manifest).Split('\n')
                       .Where(z => !z.Contains("\"exportedUtc\"")));

        private static JsonElement Manifest(string paket)
        {
            using var doc = JsonDocument.Parse(Text(Eintraege(paket)["manifest.json"]));
            return doc.RootElement.Clone();
        }

        /// <summary>Tabellenname -> Zeilenzahl im Stammbaum <c>data/</c> des Pakets.</summary>
        private static Dictionary<string, int> PaketZeilen(string paket)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, byte[]> eintraege = Eintraege(paket);
            foreach (JsonElement t in Manifest(paket).GetProperty("tables").EnumerateArray())
            {
                string name = t.GetProperty("name").GetString();
                using var doc = JsonDocument.Parse(Text(eintraege["data/" + name + ".json"]));
                map[name] = doc.RootElement.GetArrayLength();
            }
            return map;
        }

        /// <summary>Schreibt das Paket neu und setzt dabei den Schemastand im Manifest.</summary>
        private static void SchreibeMitSchemastand(string quelle, string ziel, int stand)
        {
            Dictionary<string, byte[]> eintraege = Eintraege(quelle);
            string manifest = Text(eintraege["manifest.json"]);
            using (var doc = JsonDocument.Parse(manifest))
            {
                int alt = doc.RootElement.GetProperty("schemaVersion").GetInt32();
                manifest = manifest.Replace("\"schemaVersion\": " + alt, "\"schemaVersion\": " + stand);
            }

            using var stream = new FileStream(ziel, FileMode.Create);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (var kvp in eintraege)
            {
                using var s = zip.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                byte[] roh = kvp.Key == "manifest.json" ? new UTF8Encoding(false).GetBytes(manifest) : kvp.Value;
                s.Write(roh, 0, roh.Length);
            }
        }

        /// <summary>Der Plan des TRANSFERS, nach Tabellenname greifbar. Nicht der des
        /// Duplizierers: Der Transfer nimmt <c>Tab_ProjektPhotovoltaik</c> zusaetzlich
        /// mit (§ 2.16), und P2/P3 muessen genau die Tabellen kennen, die im Paket
        /// stehen koennen.</summary>
        private static Dictionary<string, ProjektDuplizierenCtrl.Spec> Plan()
        {
            var map = new Dictionary<string, ProjektDuplizierenCtrl.Spec>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in new ProjektExportImportCtrl().Transferplan()) map[s.Tabelle] = s;
            return map;
        }

        private static int Zaehle(ProjektDuplizierenCtrl.Spec spec, int projektId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + spec.Tabelle + "] WHERE " + string.Format(spec.Filter, projektId));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Die erzwungenen Fremdschluessel einer Tabelle (Spalte, Zieltabelle, Zielspalte).</summary>
        private static List<(string, string, string)> Fremdschluessel(string tabelle)
        {
            var liste = new List<(string, string, string)>();
            DataTable dt = DataRepository.GetDataTable("PRAGMA foreign_key_list('" + tabelle + "')");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                string spalte = Convert.ToString(r["from"]);
                string zielTab = Convert.ToString(r["table"]);
                string zielSpalte = r["to"] == DBNull.Value ? null : Convert.ToString(r["to"]);
                if (string.IsNullOrEmpty(zielSpalte)) zielSpalte = "ID";
                if (string.IsNullOrEmpty(spalte) || string.IsNullOrEmpty(zielTab)) continue;
                liste.Add((spalte, zielTab, zielSpalte));
            }
            return liste;
        }
    }
}
