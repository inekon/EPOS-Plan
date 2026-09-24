using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SÄTZE DER NUTZUNGSDAUERTABELLE - Etappe E10, Stufe S3 des Konzepts
    // "Nutzungsdauer je Technik und Positionsart aus einer AfA-Tabelle"
    // (Dokumentation/ueberholt/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md, § 3 und ND-Q6).
    //
    // WOZU. Tab_Nutzungsdauer führt seit Schritt 75 die zwei Satzspalten
    // Instandsetzung_Prozent und Wartung_Prozent (VDI 2067 Blatt 1, Tabelle A2) - angelegt,
    // aber leer, ungezeigt und ungelesen. Mit S3 werden sie gesät, gezeigt und gelesen:
    // Eine Betriebskostenposition "Instandhaltung …" bzw. "Wartung …" mit der Bemessung
    // "% der Investition" bekommt den Satz ihrer Technik, wenn der Anwender ihn einträgt -
    // über "Sätze vorbelegen…" der Kostenverwaltung oder die Übernahme einer Kostenvorlage.
    // Die Tabelle rechnet nicht selbst (Fassung E10/9, Anwenderentscheid ND-Q4).
    //
    // WOHER DIE SÄTZE KOMMEN (ND-Q8: Normwerte werden nicht erfunden). Aus den
    // KONSTANTEN, die das Haus schon führt: den Empfehlungsbereichen der
    // Betriebsvorlagen-Saat (SchemaKatalog.Schritt39_Vorlagen, Positionen mit der
    // Bemessung "% der Investition"). Gesät wird die MITTE des Bereichs - "Instandhaltung
    // Heizkessel 1,5 bis 2,5 %" ergibt 2,0 % an der Standardzeile des Heizkessels. Wo die
    // Vorlage keinen Bereich kennt, bleibt die Zelle leer; Wartung ist in keiner Vorlage
    // ein Prozentsatz (Kessel €/kWh, BHKW €/kWh el, sonst fester Jahresbetrag) und bleibt
    // deshalb durchweg leer.
    //
    // WELCHE POSITION WELCHE ZEILE NIMMT. Die Betriebspositionen tragen keine Positionsart
    // (NutzungsdauerID bleibt dort NULL - die S1-Zuordnung kennt nur Investitionen). Die
    // Zuordnung steht deshalb hier, über den eingefrorenen POSITIONSSCHLÜSSEL
    // (Tab_Kostenfaktor.Bezeichnung, DbWerte.VDI_POS_*): "Instandhaltung Wärmezentrale" nimmt
    // den Satz der Wärmezentrale, auch wenn sie in der Betriebsvorlage des BHKW steht - so
    // gilt der Satz, den die Vorlage für DIESE Position empfiehlt, und nicht der des Moduls.
    // Eine Position ohne Eintrag nimmt keinen Satz aus der Tabelle.
    //
    // WARUM EIN SCHEMASCHRITT (120) UND NICHT EINE NACHSAAT BEIM START. Bestehende
    // Datenbanken tragen die Tabelle seit Schritt 75, aber keine Sätze. Die Nachsaat ist
    // Datenpflege an der Auslieferung und läuft wie Schritt 112/113 als nummerierter,
    // wiederholbarer DML-Schritt (ADR-001): EINE Quelle für Migration,
    // Werkzeuge/Testdatenbankschema und die Nachzieh-Liste der Tests. Beim Start gäbe es
    // keinen Ort dafür - die iOS-Schale migriert nicht, sie nimmt die Seed-Kopie -, und die
    // Testdatenbank als Messlatte muss die Sätze tragen.
    //
    // WAS DER SCHRITT ANFASST. Allein leere Satzzellen (NULL) der Zeilen, für die die Saat
    // einen Satz führt - gesetzt wird nur, was leer ist; ein zweiter Lauf ändert nichts.
    // Keine Positionszeile, kein Katalog, kein Projekt wird berührt, und keine gerechnete
    // Wirtschaftlichkeit ändert sich: Rechenwirksam wird ein Satz erst, wenn die Vorbelegung
    // (NutzungsdauerSatzCtrl) ihn ausdrücklich in eine Position schreibt.
    // ====================================================================================

    /// <summary>
    /// Welcher Satz einer Zeile der Nutzungsdauertabelle gemeint ist (VDI 2067 Blatt 1,
    /// Tabelle A2). Sprachneutral; die Anzeige kommt aus den Ressourcen.
    /// </summary>
    public enum Satzart
    {
        /// <summary>Instandsetzung — <c>Tab_Nutzungsdauer.Instandsetzung_Prozent</c>.</summary>
        Instandsetzung = 1,

        /// <summary>Wartung (und Inspektion) — <c>Tab_Nutzungsdauer.Wartung_Prozent</c>.</summary>
        Wartung = 2
    }

    /// <summary>
    /// Eine Betriebskostenposition und die Zeile der Nutzungsdauertabelle, deren Satz sie
    /// nimmt (Etappe E10). Zugeordnet wird über den Positionsschlüssel
    /// (<c>Tab_Kostenfaktor.Bezeichnung</c>), buchstabengetreu wie in der Vorlagen-Saat.
    /// </summary>
    public sealed class BetriebssatzZuordnung
    {
        public BetriebssatzZuordnung(string bezeichnung, int? komponentenId, string positionsart,
                                     Satzart art)
        {
            Bezeichnung = bezeichnung;
            KomponentenId = komponentenId;
            Positionsart = positionsart;
            Art = art;
        }

        /// <summary>Der Positionsschlüssel (<c>Tab_Kostenfaktor.Bezeichnung</c>).</summary>
        public string Bezeichnung { get; }

        /// <summary>Die Technik der Zeile (<c>Tab_KostenKomponente.ID</c>); <c>null</c> = die
        /// Technik der Komponente, an der die Position steht.</summary>
        public int? KomponentenId { get; }

        /// <summary>Die Positionsart der Zeile; <c>null</c> = die Standardzeile der Technik.
        /// Trägt die benannte Zeile keinen Satz, gilt der der Standardzeile.</summary>
        public string Positionsart { get; }

        /// <summary>Instandsetzung oder Wartung.</summary>
        public Satzart Art { get; }
    }

    /// <summary>
    /// Ein ausgelieferter Satz an der Standardzeile einer Technik — die Mitte des
    /// Empfehlungsbereichs einer Betriebsposition der Vorlagen-Saat (Etappe E10).
    /// </summary>
    public sealed class BetriebssatzSaat
    {
        internal BetriebssatzSaat(int komponentenId, Satzart art, double von, double bis,
                                  string quellposition, string quellvorlage)
        {
            KomponentenId = komponentenId;
            Art = art;
            Von = von;
            Bis = bis;
            Satz = Math.Round((von + bis) / 2.0, 4);
            Quellposition = quellposition;
            Quellvorlage = quellvorlage;
        }

        /// <summary>Die Technik (<c>Tab_KostenKomponente.ID</c>); gesät wird ihre Standardzeile.</summary>
        public int KomponentenId { get; }

        /// <summary>Instandsetzung oder Wartung.</summary>
        public Satzart Art { get; }

        /// <summary>Der Satz [% der Investition je Jahr] — die Mitte von <see cref="Von"/>
        /// und <see cref="Bis"/>.</summary>
        public double Satz { get; }

        /// <summary>Untergrenze des Empfehlungsbereichs der Vorlage [%].</summary>
        public double Von { get; }

        /// <summary>Obergrenze des Empfehlungsbereichs der Vorlage [%].</summary>
        public double Bis { get; }

        /// <summary>Die Vorlagenposition, aus deren Bereich der Satz stammt.</summary>
        public string Quellposition { get; }

        /// <summary>Die Komponente der Vorlage, in der die Position steht.</summary>
        public string Quellvorlage { get; }
    }

    /// <summary>
    /// <b>Die Sätze der Nutzungsdauertabelle</b> — Zuordnung der Betriebspositionen, Saat
    /// aus den Vorlagen-Konstanten und Schemaschritt 120 (die Nachsaat). Anlass, Herkunft
    /// und Ergebnisneutralität stehen im Klassenkopf oben.
    /// </summary>
    public static class NutzungsdauerSaetze
    {
        /// <summary>Die Spalte eines Satzes in <c>Tab_Nutzungsdauer</c>.</summary>
        public static string Spalte(Satzart art)
        {
            return art == Satzart.Wartung
                ? NutzungsdauerSchema.SPALTE_WARTUNG
                : NutzungsdauerSchema.SPALTE_INSTANDSETZUNG;
        }

        // =================================================================
        //  Die Zuordnung der Betriebspositionen
        // =================================================================

        /// <summary>
        /// Die Betriebspositionen der zehn Betriebsvorlagen, die einen Satz der Tabelle nehmen
        /// können: jede „Instandhaltung …" als Instandsetzung, jede „Wartung …" als Wartung.
        /// „Personalkosten" und „Steuern, Versicherung, Verwaltung" sind weder das eine noch
        /// das andere und stehen deshalb nicht hier.
        /// </summary>
        public static readonly BetriebssatzZuordnung[] Zuordnungen =
        {
            // ---- Instandsetzung ---------------------------------------------------
            new BetriebssatzZuordnung(DbWerte.VDI_POS_INSTANDHALTUNG_BHKW,             7, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung(DbWerte.VDI_POS_INSTANDHALTUNG_KESSEL,           2, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung(DbWerte.VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE,   8, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung(DbWerte.VDI_POS_INSTANDHALTUNG_BAULICH,          9, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung(DbWerte.VDI_POS_INSTANDHALTUNG_STROMEINSPEISUNG, 10, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Wärmepumpe",                     1, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Umweltwärmequelle",              1, "Erdsonden / Erdkollektor", Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Sonnenkollektoren",              4, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Solarspeicher / Zubehör",        4, "Speicher", Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Pufferspeicher",                 6, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Dämmung / Isolierung",           6, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Armaturen / Pumpen",             6, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung PV-Module / Gestell",            3, null, Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Wechselrichter / Speicher",      3, "Wechselrichter", Satzart.Instandsetzung),
            new BetriebssatzZuordnung("Instandhaltung Stromspeicher",                  5, null, Satzart.Instandsetzung),

            // ---- Wartung ------------------------------------------------------------
            // Keine dieser Positionen ist in der Auslieferung „% der Investition" - sie
            // nehmen den Satz erst, wenn der Anwender die Bemessung darauf stellt.
            // „Wartung / Sichtprüfung Speicher" steht in den Vorlagen beider Speicher; sie
            // nimmt deshalb die Technik ihrer Komponente (null).
            new BetriebssatzZuordnung(DbWerte.VDI_POS_WARTUNG_BHKW,                    7, null, Satzart.Wartung),
            new BetriebssatzZuordnung("Vollwartung / Wartung Kessel",                  2, null, Satzart.Wartung),
            new BetriebssatzZuordnung("Wartung Wärmepumpe",                            1, null, Satzart.Wartung),
            new BetriebssatzZuordnung("Wartung Solarthermie-Anlage",                   4, null, Satzart.Wartung),
            new BetriebssatzZuordnung("Wartung / Sichtprüfung Speicher",            null, null, Satzart.Wartung),
            new BetriebssatzZuordnung("Wartung / Inspektion PV-Anlage",                3, null, Satzart.Wartung),
        };

        /// <summary>Die Zuordnung zu einem Positionsschlüssel; <c>null</c> = die Position
        /// nimmt keinen Satz aus der Tabelle. Verglichen wird ohne Randleerzeichen und
        /// zeichengenau.</summary>
        public static BetriebssatzZuordnung ZuordnungZu(string bezeichnung)
        {
            if (string.IsNullOrWhiteSpace(bezeichnung)) return null;
            string b = bezeichnung.Trim();
            foreach (BetriebssatzZuordnung z in Zuordnungen)
                if (string.Equals(z.Bezeichnung, b, StringComparison.Ordinal)) return z;
            return null;
        }

        // =================================================================
        //  Die Saat - aus den Konstanten der Vorlagen
        // =================================================================

        private static IReadOnlyList<BetriebssatzSaat> _saat;

        /// <summary>
        /// Die ausgelieferten Sätze, einmal je Prozess aus
        /// <see cref="SchemaKatalog.Schritt39_Vorlagen"/> gebildet: je Zuordnung auf die
        /// STANDARDZEILE einer Technik der Empfehlungsbereich derselben Position in einer
        /// Betriebsvorlage — bevorzugt in der Vorlage der Technik selbst —, sofern die
        /// Position dort „% der Investition" ist und beide Grenzen trägt.
        /// </summary>
        public static IReadOnlyList<BetriebssatzSaat> Saat
        {
            get { return _saat ?? (_saat = SaatBilden()); }
        }

        private static IReadOnlyList<BetriebssatzSaat> SaatBilden()
        {
            var liste = new List<BetriebssatzSaat>();
            var belegt = new HashSet<string>(StringComparer.Ordinal);

            foreach (BetriebssatzZuordnung z in Zuordnungen)
            {
                if (!z.KomponentenId.HasValue || z.Positionsart != null) continue;
                string schluessel = z.KomponentenId.Value.ToString(CultureInfo.InvariantCulture) +
                                    "|" + ((int)z.Art).ToString(CultureInfo.InvariantCulture);
                if (belegt.Contains(schluessel)) continue;

                string eigeneVorlage = Technikname(z.KomponentenId.Value);
                SchemaKatalog.VorlagenPositionSeed treffer = null;
                string trefferVorlage = null;

                foreach (SchemaKatalog.KostenVorlagenSeed v in SchemaKatalog.Schritt39_Vorlagen)
                {
                    if (v.KategorieId != DbWerte.KOSTEN_KATEGORIE_BETRIEB) continue;
                    foreach (SchemaKatalog.VorlagenPositionSeed p in v.Positionen)
                    {
                        if (!string.Equals(p.Bezeichnung, z.Bezeichnung, StringComparison.Ordinal)) continue;
                        if (!string.Equals(p.Bemessung, DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                                           StringComparison.Ordinal)) continue;
                        if (!p.EmpfehlungVon.HasValue || !p.EmpfehlungBis.HasValue) continue;

                        bool eigene = string.Equals(v.Komponente, eigeneVorlage, StringComparison.Ordinal);
                        if (treffer == null || eigene)
                        {
                            treffer = p;
                            trefferVorlage = v.Komponente;
                        }
                    }
                    if (treffer != null &&
                        string.Equals(trefferVorlage, eigeneVorlage, StringComparison.Ordinal)) break;
                }

                if (treffer == null) continue;
                liste.Add(new BetriebssatzSaat(z.KomponentenId.Value, z.Art,
                                               treffer.EmpfehlungVon.Value, treffer.EmpfehlungBis.Value,
                                               treffer.Bezeichnung, trefferVorlage));
                belegt.Add(schluessel);
            }
            return liste;
        }

        /// <summary>Der ausgelieferte Satz der STANDARDZEILE einer Technik; <c>null</c> =
        /// keiner.</summary>
        public static double? SaatSatz(int komponentenId, Satzart art)
        {
            foreach (BetriebssatzSaat s in Saat)
                if (s.KomponentenId == komponentenId && s.Art == art) return s.Satz;
            return null;
        }

        /// <summary>
        /// Der ausgelieferte Satz einer Saatzeile der Tabelle (<see cref="NutzungsdauerSchema.Saat"/>):
        /// nur Standardzeilen tragen einen; alle übrigen Zeilen bleiben leer.
        /// </summary>
        public static double? SaatSatz(NutzungsdauerSaat zeile, Satzart art)
        {
            if (zeile == null || !zeile.IstStandard || !zeile.KomponentenId.HasValue) return null;
            return SaatSatz(zeile.KomponentenId.Value, art);
        }

        /// <summary>Der Name der Kostenkomponente (<c>Tab_KostenKomponente.Komponente</c>)
        /// zu ihrer festen Nummer — die Namen der Vorlagen-Saat.</summary>
        private static string Technikname(int komponentenId)
        {
            switch (komponentenId)
            {
                case 1: return DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE;
                case 2: return DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL;
                case 3: return DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK;
                case 4: return DbWerte.KOSTEN_KOMPONENTE_SOLARTHERMIE;
                case 5: return DbWerte.KOSTEN_KOMPONENTE_STROMSPEICHER;
                case 6: return DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER;
                case 7: return DbWerte.KOSTEN_KOMPONENTE_BHKW;
                case 8: return DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE;
                case 9: return DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN;
                case 10: return DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG;
                default: return "";
            }
        }

        // =================================================================
        //  Schritt 120 - die Nachsaat
        // =================================================================

        /// <summary>Was die Nachsaat getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Satzzellen, die leer waren und jetzt den ausgelieferten Satz tragen.</summary>
            public int Gesetzt;

            /// <summary>Satzzellen, die schon einen Wert trugen und stehen blieben.</summary>
            public int Belegt;

            /// <summary>Sätze der Saat, deren Standardzeile es in der Datenbank nicht gibt.</summary>
            public int OhneZeile;

            /// <summary>Der Satz für das Protokoll.</summary>
            public string Text()
            {
                return Gesetzt.ToString(CultureInfo.InvariantCulture) + " Satz/Saetze gesetzt, " +
                       Belegt.ToString(CultureInfo.InvariantCulture) + " schon belegt, " +
                       OhneZeile.ToString(CultureInfo.InvariantCulture) + " ohne Standardzeile";
            }
        }

        /// <summary>
        /// <b>Schritt 120</b> — schreibt die ausgelieferten Sätze in die LEEREN Satzzellen der
        /// Standardzeilen. Wiederholbar: Eine belegte Zelle bleibt, wie sie ist.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            foreach (BetriebssatzSaat s in Saat)
            {
                int id = StandardzeileZu(s.KomponentenId);
                if (id <= 0) { b.OhneZeile++; continue; }

                string spalte = Spalte(s.Art);
                int n = DataRepository.ExecuteNonQuery(
                    "UPDATE \"" + NutzungsdauerSchema.TABELLE + "\" SET \"" + spalte +
                    "\" = ? WHERE \"ID\" = ? AND \"" + spalte + "\" IS NULL",
                    NutzungsdauerSchema.Wert("@satz", s.Satz),
                    new DbParam("@id", id));
                if (n == 1) b.Gesetzt++;
                else b.Belegt++;
            }
            return b;
        }

        /// <summary>
        /// Wie viele Sätze der Saat stehen noch LEER an einer vorhandenen Standardzeile?
        /// 0 = der Schritt ist gelaufen (die Nachprobe der Migration).
        /// </summary>
        public static int Offen()
        {
            int offen = 0;
            foreach (BetriebssatzSaat s in Saat)
            {
                int id = StandardzeileZu(s.KomponentenId);
                if (id <= 0) continue;
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + NutzungsdauerSchema.TABELLE + "\" WHERE \"ID\" = ? AND \"" +
                    Spalte(s.Art) + "\" IS NULL",
                    new DbParam("@id", id));
                if (o != null && o != DBNull.Value && Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0)
                    offen++;
            }
            return offen;
        }

        /// <summary>Die Id der Standardzeile einer Technik; 0, wenn es keine gibt.</summary>
        private static int StandardzeileZu(int komponentenId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT \"ID\" FROM \"" + NutzungsdauerSchema.TABELLE + "\" WHERE \"" +
                NutzungsdauerSchema.SPALTE_KOMPONENTENID + "\" = ? AND \"" +
                NutzungsdauerSchema.SPALTE_IST_STANDARD + "\" = 1 ORDER BY \"ID\" LIMIT 1",
                new DbParam("@kid", komponentenId));
            return (o == null || o == DBNull.Value)
                ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }
    }
}
