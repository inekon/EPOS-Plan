using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ANWENDERENTSCHEID 19.09.2026 — „Auf Endenergiebedarf umstellen".</b>
    ///
    /// <para>Die Hilfsenergie einer Erzeugeranlage ist STROM. In der Katalogvorlage
    /// „Standard" rechnete sie an drei Gewerken trotzdem als „% der Endenergiekosten"
    /// (Weg A) — ihr Betrag war damit ein Anteil der BRENNSTOFFRECHNUNG der Anlage, am
    /// Gaskessel also ein Anteil der Gasrechnung. Ab Schemaschritt 94 rechnet die Saat
    /// als „% des Endenergiebedarfs" (Weg B): dieselbe Menge, bewertet mit dem
    /// STROMBEZUGSPREIS des Projekts.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Fünf Dinge:
    /// <list type="number">
    ///   <item><description>Das INVENTAR der Saat: welche Vorlagenposition Hilfsstrom
    ///     meint und mit welcher Bemessung sie rechnet — alle sieben auf
    ///     einmal.</description></item>
    ///   <item><description>Der Schemaschritt 94 steht, und in der Testdatenbank ist
    ///     keine Hilfsstrom-Position der Saat mehr auf Weg A.</description></item>
    ///   <item><description>Der MIGRATIONSLAUF: Er trifft genau die drei Zeilen, lässt
    ///     <c>Tab_ProjektWerte</c> unangetastet und fasst beim zweiten Lauf nichts
    ///     mehr an.</description></item>
    ///   <item><description>KEIN BLINDTAUSCH: Eine Weg-A-Position mit anderem Namen und
    ///     eine eigene Variante des Anwenders bleiben, wie sie sind.</description></item>
    ///   <item><description>Die BEWERTUNG: Weg B liefert am Heizkessel Menge ×
    ///     Strompreis, der Betrag daraus × Satz — und ohne Strompreis nennt die Abhilfe
    ///     den STROMPREIS, nicht den Brennstoffpreis.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist; die
    /// Fälle 3 und 4 SCHREIBEN in ihre Arbeitskopie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class HilfsstromBemessungVorlageTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>BHKW-Kaskade mit Gaskessel — dasselbe Projekt wie in der
        /// Bemessungsmatrix; sein Stromträger führt einen Arbeitspreis.</summary>
        private const int PROJEKT_KASKADE = 1030;

        /// <summary>Die Kessel-Betriebszeile von 1030 (Komponente 2, Anlage 11334) — seit
        /// E30 (#541) die Pflichtzeile „Vollwartung / Wartung Kessel"; die Altzeile 101600098
        /// ist mit der Datenpflege B3 entfallen.</summary>
        private const int Z_KASKADE_KESSEL = 101600585;

        /// <summary>Die Kessel-Betriebszeile von 1026 — „Erdgas E" ohne Arbeitspreis,
        /// und das Projekt führt gar keinen Stromträger.</summary>
        private const int Z_SECHS_KESSEL = 101600574;

        /// <summary>Die Anlage der Kesselzeile von 1030.</summary>
        private const int ANLAGE_KESSEL_KASKADE = 11334;

        // =====================================================================
        // 1 — Das Inventar der Saat
        // =====================================================================

        /// <summary>
        /// Die Hilfsenergie-Positionen der Katalogvorlage „Standard", je Gewerk eine, mit
        /// ihrer Bemessung. Sieben Gewerke führen eine; DREI rechneten mit Weg A und
        /// sind der Gegenstand des Schrittes 94, die VIER übrigen tragen einen festen
        /// Jahresbetrag und haben mit Weg A nie gerechnet.
        ///
        /// <para>Der Fall ist die Messlatte für jede spätere Saatpflege: Kommt eine
        /// achte Hilfsstrom-Position hinzu oder wechselt eine ihre Bemessung, fällt es
        /// hier auf und nicht erst beim Anwender.</para>
        /// </summary>
        [Fact]
        public void Das_Inventar_der_Hilfsenergie_Positionen_steht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(
                string.Join("\n", new[]
                {
                    "BHKW | Hilfsenergiekosten | PROZENT_ENDENERGIEBEDARF",
                    "Heizkessel | Hilfsenergiekosten (Strom) | PROZENT_ENDENERGIEBEDARF",
                    "Wärmepumpe | Hilfsenergiekosten (Pumpen) | PROZENT_ENDENERGIEBEDARF",
                    "Solarthermie | Hilfsenergiekosten (Solarpumpe) | JAHRESBETRAG",
                    "Pufferspeicher | Hilfsenergiekosten (Speicherladepumpe) | JAHRESBETRAG",
                    "Photovoltaik | Hilfsenergiekosten | JAHRESBETRAG",
                    "Stromspeicher | Hilfsenergiekosten | JAHRESBETRAG",
                }),
                Inventar());
        }

        /// <summary>Die Hilfsenergie-Positionen der Standardvorlagen in Vorlagenfolge —
        /// Gewerk, Bezeichnung, Bemessung.</summary>
        private static string Inventar()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT k.[" + SchemaKatalog.SPALTE_KK_KOMPONENTE + "], p.[" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], p.[" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] " +
                "FROM [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] AS p " +
                "INNER JOIN [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] AS v ON p.[" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = v.[ID] " +
                "INNER JOIN [" + SchemaKatalog.TAB_KOSTENKOMPONENTE + "] AS k ON v.[" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = k.[ID] " +
                "WHERE v.[" + SchemaKatalog.SPALTE_KV_IST_STANDARD + "] = 1 AND p.[" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] LIKE ? ORDER BY p.[ID]",
                new DbParam("@muster", HilfsstromBemessungVorlage.MUSTER));

            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(Text(r, 0)).Append(" | ").Append(Text(r, 1)).Append(" | ").Append(Text(r, 2));
            }
            return sb.ToString();
        }

        // =====================================================================
        // 2 — Der Schemaschritt
        // =====================================================================

        /// <summary>
        /// Der Zielstand trägt den Schritt 94, und die Testdatenbank ist auf ihm: keine
        /// Hilfsstrom-Position der Saat rechnet noch mit Weg A. Die drei Gewerke der
        /// Umstellung stehen namentlich in der Quelle — sie ist die EINE, aus der sich
        /// Migration, Werkzeug und Nachweis bedienen.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_94()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(SchemaStand.Zielversion >= 94,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 94.");

            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, HilfsstromBemessungVorlage.VON);
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, HilfsstromBemessungVorlage.NACH);
            Assert.Equal(new[]
            {
                DbWerte.KOSTEN_KOMPONENTE_BHKW,
                DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
                DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE,
            }, HilfsstromBemessungVorlage.Komponenten);

            Assert.True(HilfsstromBemessungVorlage.Vorhanden(), "Der Kostenkatalog fehlt.");
            Assert.Equal(0, HilfsstromBemessungVorlage.Offen());
            Assert.Equal(3, HilfsstromBemessungVorlage.Umgestellt());
            Assert.Empty(HilfsstromBemessungVorlage.Anweisungen);
        }

        /// <summary>
        /// SAAT UND NACHZUG TRAGEN DIESELBE ART. Eine frisch gesäte Datenbank bekommt
        /// ihre zwanzig Auslieferungsvorlagen aus
        /// <see cref="SchemaKatalog.Schritt39_Vorlagen"/>, eine bestehende den Nachzug
        /// aus <see cref="HilfsstromBemessungVorlage"/>. Liefen beide auseinander, hinge
        /// die Bemessung daran, wann die Datenbank entstanden ist.
        ///
        /// <para>Geprüft werden alle drei Betriebsvorlagen auf einmal — und zugleich,
        /// dass die vier übrigen Gewerke ihren festen Jahresbetrag behalten.</para>
        /// </summary>
        [Fact]
        public void Saat_und_Nachzug_tragen_dieselbe_Art()
        {
            var ausDerSaat = new List<string>();
            foreach (SchemaKatalog.KostenVorlagenSeed s in SchemaKatalog.Schritt39_Vorlagen)
            {
                if (s.KategorieId != DbWerte.KOSTEN_KATEGORIE_BETRIEB) continue;
                foreach (SchemaKatalog.VorlagenPositionSeed pos in s.Positionen)
                    if (pos.Bezeichnung.StartsWith("Hilfsenergiekosten", StringComparison.Ordinal))
                        ausDerSaat.Add(s.Komponente + " | " + pos.Bezeichnung + " | " + pos.Bemessung);
            }

            Assert.Equal(
                string.Join("\n", new[]
                {
                    "BHKW | Hilfsenergiekosten | PROZENT_ENDENERGIEBEDARF",
                    "Heizkessel | Hilfsenergiekosten (Strom) | PROZENT_ENDENERGIEBEDARF",
                    "Wärmepumpe | Hilfsenergiekosten (Pumpen) | PROZENT_ENDENERGIEBEDARF",
                    "Solarthermie | Hilfsenergiekosten (Solarpumpe) | JAHRESBETRAG",
                    "Pufferspeicher | Hilfsenergiekosten (Speicherladepumpe) | JAHRESBETRAG",
                    "Photovoltaik | Hilfsenergiekosten | JAHRESBETRAG",
                    "Stromspeicher | Hilfsenergiekosten | JAHRESBETRAG",
                }),
                string.Join("\n", ausDerSaat));

            // Keine Saatzeile trägt die Art, die der Schritt verlässt.
            foreach (string zeile in ausDerSaat)
                Assert.DoesNotContain(HilfsstromBemessungVorlage.VON, zeile, StringComparison.Ordinal);
        }

        // =====================================================================
        // 3 — Der Migrationslauf
        // =====================================================================

        /// <summary>
        /// Der Schritt auf einer Datenbank vom Stand 93: Er trifft genau die drei
        /// Vorlagenzeilen, lässt <c>Tab_ProjektWerte</c> Zeile für Zeile unangetastet,
        /// und der zweite Lauf ändert nichts mehr.
        ///
        /// <para>Der Fall stellt den Stand 93 selbst her, statt ihn vorauszusetzen: Die
        /// Arbeitskopie kommt aus der Repo-Datei, und die steht längst auf 94.</para>
        /// </summary>
        [Fact]
        public void Der_Schritt_trifft_die_drei_Zeilen_und_sonst_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZurueckAufWegA();
            Assert.Equal(3, HilfsstromBemessungVorlage.Offen());

            string projektwerteVorher = Fingerabdruck("Tab_ProjektWerte");
            string vorlagenVorher = Fingerabdruck(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION);

            int gelaufen = Ausfuehren();
            Assert.Equal(1, gelaufen);

            Assert.Equal(0, HilfsstromBemessungVorlage.Offen());
            Assert.Equal(3, HilfsstromBemessungVorlage.Umgestellt());

            // Die Projektpositionen bleiben, wie sie erfasst sind — Zeile für Zeile.
            Assert.Equal(projektwerteVorher, Fingerabdruck("Tab_ProjektWerte"));

            // In den Vorlagen haben sich GENAU drei Zeilen bewegt.
            Assert.Equal(3, Unterschiede(vorlagenVorher,
                                         Fingerabdruck(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION)));

            // Der zweite Lauf hat nichts mehr zu tun.
            string nachDemErsten = Fingerabdruck(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION);
            Assert.Equal(0, Ausfuehren());
            Assert.Equal(nachDemErsten, Fingerabdruck(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION));
        }

        /// <summary>
        /// KEIN BLINDTAUSCH. Zwei Nachbarn, die ebenfalls mit Weg A rechnen, dürfen der
        /// Schritt nicht mitnehmen: eine Position mit anderem Namen in derselben
        /// Standardvorlage, und eine Hilfsenergiekosten-Position in einer EIGENEN
        /// Variante des Anwenders — was er selbst angelegt hat, ist seine Entscheidung.
        /// </summary>
        [Fact]
        public void Weder_ein_anderer_Name_noch_eine_eigene_Variante_werden_mitgenommen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZurueckAufWegA();

            int fremderName = PositionAnlegen(VORLAGE_KESSEL_STANDARD, "Wartungskosten");
            int eigeneVariante = EigeneVarianteAnlegen();
            int inDerVariante = PositionAnlegen(eigeneVariante, "Hilfsenergiekosten (Strom)");

            // Der Schritt sieht weiterhin nur die drei Zeilen der Saat.
            Assert.Equal(3, HilfsstromBemessungVorlage.Offen());
            Ausfuehren();

            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, BemessungVon(fremderName));
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, BemessungVon(inDerVariante));
            Assert.Equal(3, HilfsstromBemessungVorlage.Umgestellt());
        }

        // =====================================================================
        // 4 — Die Bewertung mit dem Strompreis
        // =====================================================================

        /// <summary>
        /// Am Heizkessel mit Endenergie und einem Projekt, das einen Stromträger samt
        /// Arbeitspreis führt: Weg B liefert MENGE × STROMPREIS, und der Betrag daraus
        /// ist Bezugsgröße × Satz / 100.
        ///
        /// <para><b>Die Gegenprobe steht daneben:</b> Weg A bemisst sich an DERSELBEN
        /// Menge, bewertet mit dem Arbeitspreis des BRENNSTOFFTRÄGERS — zwei Preise,
        /// zwei Zahlen. Ohne sie bliebe offen, ob die Umstellung überhaupt etwas
        /// bewegt.</para>
        /// </summary>
        [Fact]
        public void Weg_B_bewertet_die_Menge_mit_dem_Strompreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EndenergieAufloeser aufloeser = EndenergieAufloeser.FuerProjekt(PROJEKT_KASKADE);
            Assert.NotNull(aufloeser);

            double? strompreis = aufloeser.StrompreisJeKwh;
            Assert.True(strompreis.HasValue && strompreis.Value > 0,
                        "Das Projekt führt keinen Strombezugspreis.");

            EndenergieAufloeser.Groesse g = aufloeser.FuerPosition(
                BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL, ANLAGE_KESSEL_KASKADE);
            Assert.NotNull(g);
            Assert.True(g.BedarfKwh > 0, "Der Lauf führt keine Brennstoffmenge.");

            double bedarf = Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF);
            Assert.Equal(g.BedarfKwh * strompreis.Value, bedarf, 6);

            // Der Betrag: Bezugsgröße × Satz / 100 — der Satz der Saat liegt in der
            // Empfehlungsspanne der Kesselvorlage.
            const double satz = 6.0;
            Assert.Equal(bedarf * satz / 100.0,
                         BetriebskostenCtrl.Betrag(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                                                   0.0, bedarf, satz, false), 6);

            // Die Gegenprobe: Weg A bewertet dieselbe Menge mit dem Brennstoffpreis.
            Assert.True(g.KostenEuro.HasValue);
            double kosten = Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN);
            Assert.Equal(g.KostenEuro.Value, kosten, 6);
            Assert.NotEqual(kosten, bedarf);
        }

        /// <summary>
        /// Ohne Stromträger und ohne Arbeitspreis bleibt Weg B ohne Bezugsgröße. An
        /// einem BRENNSTOFFkessel vermisst Weg B den Arbeitspreis des
        /// PROJEKT-Stromträgers, Weg A den des Brennstoffträgers — zwei Einträge der
        /// Energieträgerverwaltung, zwei Steuerwerte, zwei Abhilfen. Wer am falschen
        /// Träger pflegt, ändert nichts.
        ///
        /// <para>ANWENDERENTSCHEID 19.09.2026: Der Steuerwert des Weg-B-Falles heißt
        /// <c>STROMPREIS</c> statt <c>PREIS</c>. Er muss die Lage allein tragen: Seit
        /// die Preisauflösung anlagenscharf ist, sagt die Bemessungsart nicht mehr,
        /// welcher Träger gemeint ist — dieselbe Art trägt an einer Stromanlage den
        /// eigenen, an einer Brennstoffanlage den Projektträger.</para>
        /// </summary>
        [Fact]
        public void Ohne_Strompreis_nennt_die_Abhilfe_den_Strompreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS,
                         Grund(Z_SECHS_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS,
                         Grund(Z_SECHS_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));

            string wegB = KostenHerleitung.GrundText(WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS);
            string wegA = KostenHerleitung.GrundText(WirtschaftlichkeitCtrl.BASISGRUND_PREIS);

            Assert.Contains("Stromträger", wegB, StringComparison.Ordinal);
            Assert.NotEqual(wegA, wegB);
            Assert.DoesNotContain("Stromträger", wegA, StringComparison.Ordinal);

            // Jede andere Lage bleibt, wie sie war.
            Assert.NotEqual("", KostenHerleitung.GrundText(WirtschaftlichkeitCtrl.BASISGRUND_MENGE));
            Assert.Equal("", KostenHerleitung.GrundText(""));
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>Die Standardvorlage der Betriebskosten des Heizkessels.</summary>
        private static int VORLAGE_KESSEL_STANDARD
        {
            get
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT MIN(v.[ID]) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] AS v " +
                    "INNER JOIN [" + SchemaKatalog.TAB_KOSTENKOMPONENTE + "] AS k ON v.[" +
                    SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = k.[ID] " +
                    "WHERE v.[" + SchemaKatalog.SPALTE_KV_IST_STANDARD + "] = 1 AND k.[" +
                    SchemaKatalog.SPALTE_KK_KOMPONENTE + "] = ? AND EXISTS " +
                    "(SELECT 1 FROM [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] AS p " +
                    "WHERE p.[" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = v.[ID] AND p.[" +
                    SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] LIKE ?)",
                    new DbParam("@k", DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL),
                    new DbParam("@muster", HilfsstromBemessungVorlage.MUSTER));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
        }

        /// <summary>Stellt den Stand 93 her: die drei Zeilen der Saat zurück auf Weg A.</summary>
        private static void ZurueckAufWegA()
        {
            DataRepository.ExecuteSQL(
                "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] = ? WHERE [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] = ? AND [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] LIKE ?",
                new DbParam("@a", HilfsstromBemessungVorlage.VON),
                new DbParam("@b", HilfsstromBemessungVorlage.NACH),
                new DbParam("@muster", HilfsstromBemessungVorlage.MUSTER));
        }

        /// <summary>Arbeitet die Anweisungen des Schrittes ab; Rückgabe ist ihre Anzahl.</summary>
        private static int Ausfuehren()
        {
            int n = 0;
            foreach (KeyValuePair<string, HilfsstromBemessungVorlage.Anweisung> a
                     in HilfsstromBemessungVorlage.Anweisungen)
            {
                DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
                n++;
            }
            return n;
        }

        /// <summary>Eine neue Vorlagenposition mit Weg A; Rückgabe ist ihre ID.</summary>
        private static int PositionAnlegen(int vorlageId, string bezeichnung)
        {
            Assert.True(vorlageId > 0, "Vorlage nicht gefunden.");
            int id = NaechsteId(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION);
            DataRepository.ExecuteSQL(
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] ([ID], [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "], [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]) VALUES (?, ?, ?, ?, ?, ?)",
                new DbParam("@id", id),
                new DbParam("@v", vorlageId),
                new DbParam("@b", bezeichnung),
                new DbParam("@k", DbWerte.KOSTENART_BEDARFSGEBUNDEN),
                new DbParam("@m", HilfsstromBemessungVorlage.VON),
                new DbParam("@s", 900));
            return id;
        }

        /// <summary>Eine EIGENE Variante des Anwenders am Heizkessel
        /// (<c>IstStandard = 0</c>); Rückgabe ist ihre ID.</summary>
        private static int EigeneVarianteAnlegen()
        {
            object komponente = DataRepository.ExecuteScalar(
                "SELECT [" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new DbParam("@id", VORLAGE_KESSEL_STANDARD));

            int id = NaechsteId(SchemaKatalog.TAB_KOSTENVORLAGE);
            DataRepository.ExecuteSQL(
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] ([ID], [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "], [" +
                SchemaKatalog.SPALTE_KV_NAME + "], [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "], [" +
                SchemaKatalog.SPALTE_KV_READONLY + "]) VALUES (?, ?, ?, ?, 0, 0)",
                new DbParam("@id", id),
                new DbParam("@k", komponente),
                new DbParam("@kat", 2),
                new DbParam("@n", "Eigene Variante"));
            return id;
        }

        /// <summary>MAX(ID) + 1 — das Hausmuster dieses Schemas.</summary>
        private static int NaechsteId(string tabelle)
        {
            object o = DataRepository.ExecuteScalar("SELECT MAX([ID]) FROM [" + tabelle + "]");
            return (o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o)) + 1;
        }

        /// <summary>Die Bemessung EINER Vorlagenposition.</summary>
        private static string BemessungVon(int positionsId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [ID] = ?",
                new DbParam("@id", positionsId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Alle Zeilen einer Tabelle als Text, eine Zeile je Datensatz —
        /// der Vergleich Zeile für Zeile, nicht Datei gegen Datei.</summary>
        private static string Fingerabdruck(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] ORDER BY [ID]");
            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0) sb.Append('\u001f');
                    sb.Append(r[i] == DBNull.Value
                                  ? ""
                                  : Convert.ToString(r[i], CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Wie viele Zeilen zweier Fingerabdrücke unterscheiden sich?</summary>
        private static int Unterschiede(string vorher, string nachher)
        {
            string[] a = vorher.Split('\n');
            string[] b = nachher.Split('\n');
            Assert.Equal(a.Length, b.Length);

            int n = 0;
            for (int i = 0; i < a.Length; i++)
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>Die Bezugsgröße einer Position zu einer Bemessungsart; der Fall
        /// bricht ab, wenn es keine gibt (dann sagt der Grund, warum).</summary>
        private static double Basis(int positionsId, string bemessung)
        {
            string grund;
            double? b = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bemessung, out grund);
            Assert.True(b.HasValue, "Keine Bezugsgröße zu " + bemessung + ": " + grund);
            return b.Value;
        }

        /// <summary>Der Grund, wenn es keine Bezugsgröße gibt.</summary>
        private static string Grund(int positionsId, string bemessung)
        {
            string grund;
            double? b = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bemessung, out grund);
            Assert.False(b.HasValue, "Unerwartete Bezugsgröße zu " + bemessung + ": " + b);
            return grund;
        }

        private static string Text(DataRow r, int spalte)
        {
            return r[spalte] == DBNull.Value
                ? "" : Convert.ToString(r[spalte], CultureInfo.InvariantCulture);
        }
    }
}
