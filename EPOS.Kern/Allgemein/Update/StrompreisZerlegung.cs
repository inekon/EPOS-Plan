using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE ANTEILE SIND EINE ZERLEGUNG DES ARBEITSPREISES, KEIN AUFSCHLAG DARAUF
    // - Migrationsschritt 83 (Anwenderentscheide SP-E-2/SP-E-3 vom 17.09.2026).
    //
    // WOZU. Bis zu diesem Entscheid trug der Block "Aufschlaege auf den
    // Strombezugspreis" fuenf Saetze, die auf den Arbeitspreis ADDIERT wurden - in der
    // Speichersimulation immer, in der Wirtschaftlichkeit nur bei gesetztem
    // Projektschalter. Damit gab es zwei Preiswahrheiten fuer denselben Strombezug, und
    // welche galt, hing vom Rechenweg ab. Der Entscheid dreht das um:
    //
    //     Arbeitspreis = Beschaffung + Vertrieb + Arbeitspreis Netz
    //                    + Stromsteuer + Konzessionsabgabe + Umlagen
    //
    // Der ARBEITSPREIS ist ab hier die eine Wahrheit; die Anteile sagen nur, woraus er
    // besteht. Auf eine Spot- oder Profilreihe kommen weiterhin die uebrigen Anteile,
    // weil DIESE Reihe die Beschaffung ist - das ist die einzige Ausnahme.
    //
    // DIE FALTUNG. Wer bisher wirksam aufschlug, haette ab hier einen um den Aufschlag
    // ZU NIEDRIGEN Bezugspreis gerechnet. Der Schritt faltet ihn deshalb in den
    // Arbeitspreis:
    //
    //     Beschaffung        := bisheriger wirksamer Arbeitspreis
    //     Arbeitspreis (neu) := bisheriger + bisher wirksamer Aufschlag
    //
    // Danach ist die Summe der aktiven Anteile genau der neue Arbeitspreis, und die
    // Summe OHNE Beschaffung genau der alte Aufschlag - die Preisreihe jedes Laufs
    // bleibt, wie sie war. Genau das weist der byte-gleiche Referenzlauf nach.
    //
    // GESCHRIEBEN WIRD, WO DIE VORRANGKETTE LIEST. StromPreisCtrl.ArbeitspreisCtKwh
    // nimmt (1) die juengste Preisversion aus energy_price, (2) die aelteste ueberhaupt,
    // (3) energy_project_settings.custom_price_work, (4) den Katalogpreis. Ein Faltung,
    // die nur custom_price_work anhebt, kaeme bei jedem Projekt mit Preishistorie nie
    // an. Angehoben werden deshalb BEIDE: die Projekteinstellung und JEDE
    // Preisversion dieses (Projekt, Traeger) mit einem Arbeitspreis > 0. Das ist auch
    // fachlich die richtige Aussage - der Aufschlag galt fuer jede Version gleich, es
    // gab ihn je (Projekt, Traeger) nur EINMAL. Eine Version mit Arbeitspreis 0 bleibt
    // unberuehrt: 0 heisst "nicht gepflegt", und die Vorrangkette ueberspringt sie.
    //
    // DER GESAMTWERT LAESST SICH NICHT ZERLEGEN. Eine Zeile im Modus "Gesamtwert" trug
    // EINE Zahl ohne Aufteilung. Sie wandert vollstaendig in den Arbeitspreis, die
    // Anteilsfelder bleiben als Vorschlag stehen, aber INAKTIV - eine erfundene
    // Zuordnung waere schlimmer als eine benannte Luecke. Jede solche Zeile bekommt eine
    // Protokollzeile mit Projekt, Traeger und altem Aufschlag.
    //
    // ZEILEN OHNE WIRKSAMEN AUFSCHLAG WERDEN STILLGELEGT, NICHT GELEERT. Wer Werte
    // gepflegt, aber "kein Aufschlag" gewaehlt hatte, rechnete mit 0. Ohne Modus
    // rechneten dieselben Werte ab hier mit - der Schritt setzt ihre Aktiv-Schalter
    // deshalb auf 0. Die Zahlen bleiben sichtbar; sie zaehlen nur nicht.
    //
    // NUR STROMTRAEGER. Die vierzehn Spalten aus Schritt 12 stehen an allen Traegern,
    // gepflegt wurde der Block aber ausschliesslich am Stromtraeger
    // (pricing_model = 'ELECTRICITY'); nur dort gibt es ihn in der Maske. Ein
    // Brennstofftraeger hat seine eigene Zerlegung (Anteil_*, Schritt 60) und wird hier
    // nicht angefasst.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // NutzungsdauerSchema (75) bis ProjektWerteLoeschschutz (81): Die Anweisungen
    // brauchen drei Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Die Faltung des Strom-Aufschlags in den Arbeitspreis (Schemaschritt 83) - EINE
    /// Quelle fuer Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class StrompreisZerlegung
    {
        /// <summary>Die Projekteinstellungen je (Projekt, Energietraeger).</summary>
        public const string TABELLE = SchemaKatalog.ENERGY_PROJECT_SETTINGS;

        /// <summary>Die stichtagsversionierte Preishistorie.</summary>
        public const string TABELLE_PREIS = "energy_price";

        /// <summary>Der Traegerkatalog - er traegt das Preismodell.</summary>
        public const string TABELLE_TRAEGER = "energy_carrier";

        /// <summary>Preismodell des Stromtraegers.</summary>
        public const string PREISMODELL_STROM = "ELECTRICITY";

        /// <summary>Modustext "aufgeschluesselt" des Bestands.</summary>
        public const string MODUS_AUFGESCHLUESSELT = DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;

        /// <summary>Modustext "Gesamtwert" des Bestands.</summary>
        public const string MODUS_GESAMTWERT = DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT;

        /// <summary>Modustext "kein Aufschlag" des Bestands.</summary>
        public const string MODUS_KEINER = DbWerte.SP_AUFSCHLAG_MODUS_KEINER;

        /// <summary>ct/kWh in EUR/kWh.</summary>
        private const double CT_IN_EUR = 100.0;

        /// <summary>Die fuenf Anteilsspalten des Bestands, in Anzeigereihenfolge.</summary>
        public static readonly string[] BESTANDSANTEILE =
        {
            SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT,
            SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN,
            SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER,
            SchemaKatalog.SPALTE_AUFSCHLAG_KONZESSION,
            SchemaKatalog.SPALTE_AUFSCHLAG_VERTRIEB
        };

        /// <summary>
        /// Eine Zeile, wie der Schritt sie vorfindet: Zeilenbezug, bisher wirksamer
        /// Aufschlag und bisher wirksamer Arbeitspreis - alles in ct/kWh.
        /// </summary>
        public sealed class Zeile
        {
            public int Projekt;
            public int Traeger;

            /// <summary>Der Modustext, wie er in der Spalte steht (kann leer sein).</summary>
            public string Modus = "";

            /// <summary>Bisher wirksamer Aufschlag [ct/kWh]: Summe der aktiven Anteile bzw. Override.</summary>
            public double AufschlagCtKwh;

            /// <summary>Bisher wirksamer Arbeitspreis [ct/kWh] nach der Vorrangkette.</summary>
            public double ArbeitspreisCtKwh;

            /// <summary>Heizwert je Abrechnungseinheit; 1 bei Direktabrechnung nach kWh.</summary>
            public double HeizwertJeEinheit = 1.0;

            /// <summary>true, wenn der Aufschlag als nicht zerlegbarer Gesamtwert erfasst war.</summary>
            public bool Gesamtwert;

            /// <summary>true, wenn Anteilswerte gepflegt sind (gleich welchen Modus die Zeile trug).</summary>
            public bool AnteileGepflegt;

            /// <summary>
            /// true, wenn die Zeile den Schritt schon hinter sich hat: Eine gepflegte
            /// Beschaffung gibt es erst ab Schritt 83. <b>Das ist die Idempotenz-Marke</b> -
            /// ohne sie legte ein zweiter Lauf die soeben gefalteten Anteile still, weil
            /// die Zeile danach den Modus „Keiner" traegt.
            /// </summary>
            public bool BereitsGefaltet;
        }

        // =================================================================
        //  Auskunft
        // =================================================================

        /// <summary>Stehen die neun Spalten des Schritts schon? Dann ist er gelaufen.</summary>
        public static bool SpaltenVorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG);
        }

        /// <summary>
        /// Wie viele Zeilen faltet der Schritt? Das sind die Zeilen mit einem wirksamen
        /// Aufschlag &gt; 0. 0 = nichts zu falten (der Schritt legt dann nur die Spalten an).
        /// </summary>
        public static int ZaehlungFaltung()
        {
            int n = 0;
            foreach (Zeile z in Zeilen())
                if (z.AufschlagCtKwh != 0.0) n++;
            return n;
        }

        /// <summary>
        /// Alle Stromzeilen der Tabelle samt bisher wirksamem Aufschlag und
        /// Arbeitspreis - die Grundlage der Faltung und ihres Protokolls.
        /// </summary>
        public static IReadOnlyList<Zeile> Zeilen()
        {
            List<Zeile> liste = new List<Zeile>();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT eps.*, ec.hi_kwh_per_unit AS traeger_hi, ec.price_work AS traeger_preis " +
                "FROM [" + TABELLE + "] AS eps " +
                "INNER JOIN " + TABELLE_TRAEGER + " AS ec ON eps.[ID_Energieträger] = ec.id " +
                "WHERE ec.pricing_model = ? " +
                "ORDER BY eps.ID_Projekt, eps.[ID_Energieträger]",
                new DbParam("@pm", DbParamTyp.VarWChar) { Wert = PREISMODELL_STROM });

            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                Zeile z = new Zeile();
                z.Projekt = Ganzzahl(dt, r, "ID_Projekt");
                z.Traeger = Ganzzahl(dt, r, "ID_Energieträger");
                z.Modus = Text(dt, r, StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS);

                double summe = 0.0;
                bool gepflegt = false;
                foreach (string spalte in BESTANDSANTEILE)
                {
                    double? wert = Zahl(dt, r, spalte);
                    if (!wert.HasValue) continue;
                    gepflegt = true;
                    if (Schalter(dt, r, spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX))
                        summe += wert.Value;
                }
                z.AnteileGepflegt = gepflegt;
                z.BereitsGefaltet =
                    Zahl(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG).HasValue;

                double over = Zahl(dt, r, StrompreisAltspalten.SPALTE_AUFSCHLAG_OVERRIDE) ?? 0.0;
                z.Gesamtwert = string.Equals(z.Modus, MODUS_GESAMTWERT, StringComparison.Ordinal)
                               && over != 0.0;

                if (z.BereitsGefaltet) z.AufschlagCtKwh = 0.0;
                else if (z.Gesamtwert) z.AufschlagCtKwh = over;
                else if (string.Equals(z.Modus, MODUS_AUFGESCHLUESSELT, StringComparison.Ordinal))
                    z.AufschlagCtKwh = summe;
                else z.AufschlagCtKwh = 0.0;

                double hi = Zahl(dt, r, "custom_hi") ?? Zahl(dt, r, "traeger_hi") ?? 1.0;
                z.HeizwertJeEinheit = hi > 0.0 ? hi : 1.0;

                z.ArbeitspreisCtKwh = WirksamerArbeitspreisCtKwh(
                    z.Projekt, z.Traeger, Zahl(dt, r, "custom_price_work"),
                    Zahl(dt, r, "traeger_preis"), z.HeizwertJeEinheit);

                liste.Add(z);
            }

            return liste;
        }

        // =================================================================
        //  Die Faltung
        // =================================================================

        /// <summary>
        /// Faltet den bisher wirksamen Aufschlag in den Arbeitspreis und legt die
        /// Anteile still, die nicht mehr rechnen duerfen.
        ///
        /// <para><b>Wiederholbar.</b> Gefaltet wird nur, was noch einen wirksamen
        /// Aufschlag traegt; nach dem Lauf steht dort kein Modus mehr, der einen
        /// hergaebe, und <see cref="ZaehlungFaltung"/> liefert 0.</para>
        /// </summary>
        /// <returns>Das Protokoll - eine Zeile je angefasster Zeile, leer wenn nichts war.</returns>
        public static IReadOnlyList<string> Falten()
        {
            List<string> protokoll = new List<string>();
            CultureInfo k = CultureInfo.InvariantCulture;

            foreach (Zeile z in Zeilen())
            {
                // Schon gefaltet - nichts zu tun. Ohne diese Marke legte ein zweiter
                // Lauf die soeben gefalteten Anteile still: Die Zeile traegt danach den
                // Modus „Keiner", und genau der fuehrt unten in die Stilllegung.
                if (z.BereitsGefaltet) continue;

                if (z.AufschlagCtKwh == 0.0)
                {
                    // Kein wirksamer Aufschlag - aber gepflegte Werte, die ohne Modus ab
                    // hier mitrechnen wuerden. Sie werden stillgelegt, nicht geleert.
                    if (z.AnteileGepflegt && AktivIrgendwo(z))
                    {
                        AnteileStilllegen(z.Projekt, z.Traeger);
                        protokoll.Add("Projekt " + z.Projekt + ", Traeger " + z.Traeger +
                                      ": Anteile gepflegt, aber ohne wirksamen Aufschlag - " +
                                      "Aktiv-Schalter auf 0 gesetzt, die Werte bleiben als " +
                                      "Vorschlag stehen.");
                    }
                    continue;
                }

                double alt = z.ArbeitspreisCtKwh;
                double neu = alt + z.AufschlagCtKwh;

                // ct/kWh -> Abrechnungseinheit (Strom: kWh, Heizwert 1).
                double deltaJeEinheit = z.AufschlagCtKwh / CT_IN_EUR * z.HeizwertJeEinheit;

                ArbeitspreisAnheben(z.Projekt, z.Traeger, deltaJeEinheit);

                if (z.Gesamtwert)
                {
                    // Der Gesamtwert laesst sich nicht in Anteile zerlegen: Er wandert
                    // vollstaendig in den Arbeitspreis. Die Anteile bleiben stehen, aber
                    // inaktiv; Beschaffung bekommt den NEUEN Arbeitspreis, damit die
                    // Zerlegung wieder aufgeht.
                    AnteileStilllegen(z.Projekt, z.Traeger);
                    BeschaffungSetzen(z.Projekt, z.Traeger, neu);
                    ModusSetzen(z.Projekt, z.Traeger, MODUS_KEINER);

                    protokoll.Add("Projekt " + z.Projekt + ", Traeger " + z.Traeger +
                                  ": Gesamtaufschlag " + z.AufschlagCtKwh.ToString("0.###", k) +
                                  " ct/kWh war nicht aufgeschluesselt. Er ist in den " +
                                  "Arbeitspreis gefaltet (" + alt.ToString("0.###", k) + " -> " +
                                  neu.ToString("0.###", k) + " ct/kWh) und steht dort " +
                                  "vollstaendig unter \"Beschaffung\"; die Anteilsfelder " +
                                  "bleiben als Vorschlag stehen und sind inaktiv. Eine " +
                                  "Zuordnung auf die Anteile wurde NICHT erfunden.");
                }
                else
                {
                    BeschaffungSetzen(z.Projekt, z.Traeger, alt);
                    ModusSetzen(z.Projekt, z.Traeger, MODUS_KEINER);

                    protokoll.Add("Projekt " + z.Projekt + ", Traeger " + z.Traeger +
                                  ": Aufschlag " + z.AufschlagCtKwh.ToString("0.###", k) +
                                  " ct/kWh in den Arbeitspreis gefaltet (" +
                                  alt.ToString("0.###", k) + " -> " + neu.ToString("0.###", k) +
                                  " ct/kWh); Beschaffung := " + alt.ToString("0.###", k) +
                                  " ct/kWh. Summe der Anteile = Arbeitspreis, Summe ohne " +
                                  "Beschaffung = bisheriger Aufschlag.");
                }
            }

            return protokoll;
        }

        // =================================================================
        //  Die einzelnen Handgriffe
        // =================================================================

        /// <summary>
        /// Hebt den Arbeitspreis an - in der Projekteinstellung UND in jeder
        /// Preisversion mit einem Arbeitspreis &gt; 0 (siehe Kopfkommentar).
        /// </summary>
        private static void ArbeitspreisAnheben(int projekt, int traeger, double deltaJeEinheit)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [" + TABELLE + "] SET custom_price_work = custom_price_work + ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ? AND custom_price_work > 0",
                new DbParam("@d", DbParamTyp.Double) { Wert = deltaJeEinheit },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });

            DataRepository.ExecuteNonQuery(
                "UPDATE " + TABELLE_PREIS + " SET arbeitspreis = arbeitspreis + ? " +
                "WHERE ID_Projekt = ? AND carrier_id = ? AND arbeitspreis > 0",
                new DbParam("@d", DbParamTyp.Double) { Wert = deltaJeEinheit },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });
        }

        /// <summary>Traegt die Beschaffung [ct/kWh] ein und schaltet sie aktiv.</summary>
        private static void BeschaffungSetzen(int projekt, int traeger, double ctKwh)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [" + TABELLE + "] SET [" + SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG +
                "] = ?, [" + SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG +
                SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX + "] = 1 " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@w", DbParamTyp.Double) { Wert = ctKwh },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });
        }

        /// <summary>Setzt alle fuenf Bestandsanteile auf inaktiv; die Werte bleiben stehen.</summary>
        private static void AnteileStilllegen(int projekt, int traeger)
        {
            foreach (string spalte in BESTANDSANTEILE)
                DataRepository.ExecuteNonQuery(
                    "UPDATE [" + TABELLE + "] SET [" + spalte +
                    SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX + "] = 0 " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                    new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });
        }

        /// <summary>
        /// Schreibt den Modustext. Die Spalte rechnet ab Schritt 83 nicht mehr mit -
        /// sie bleibt stehen, damit eine aeltere Programmfassung auf derselben Datei
        /// nicht ploetzlich einen Aufschlag rechnet, den es nicht mehr gibt.
        /// </summary>
        private static void ModusSetzen(int projekt, int traeger, string modus)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [" + TABELLE + "] SET [" + StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS + "] = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@m", DbParamTyp.VarWChar) { Wert = modus },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });
        }

        /// <summary>
        /// Der bisher wirksame Arbeitspreis [ct/kWh] nach der Vorrangkette von
        /// <c>StromPreisCtrl.ArbeitspreisCtKwh</c>: juengste Preisversion mit einem Wert
        /// &gt; 0, sonst Projekteinstellung, sonst Katalogpreis.
        ///
        /// <para><b>Ohne Stichtag.</b> Ein Migrationsschritt hat kein Simulationsjahr;
        /// er nimmt die juengste Version ueberhaupt. Fachlich ist das dieselbe Zahl,
        /// solange der Aufschlag je (Projekt, Traeger) nur EINMAL existiert - und das
        /// tut er.</para>
        /// </summary>
        private static double WirksamerArbeitspreisCtKwh(int projekt, int traeger,
                                                         double? projekteinstellung,
                                                         double? katalog, double hi)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT arbeitspreis FROM " + TABELLE_PREIS + " " +
                "WHERE ID_Projekt = ? AND carrier_id = ? AND arbeitspreis > 0 " +
                "ORDER BY valid_from DESC LIMIT 1",
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });

            double jeEinheit;
            if (v != null && v != DBNull.Value) jeEinheit = Convert.ToDouble(v);
            else if (projekteinstellung.HasValue && projekteinstellung.Value > 0.0)
                jeEinheit = projekteinstellung.Value;
            else if (katalog.HasValue && katalog.Value > 0.0) jeEinheit = katalog.Value;
            else return 0.0;

            return hi > 0.0 ? jeEinheit / hi * CT_IN_EUR : jeEinheit * CT_IN_EUR;
        }

        private static bool AktivIrgendwo(Zeile z)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABELLE + "] " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ? AND (" +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT + "_Aktiv] = 1 OR " +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN + "_Aktiv] = 1 OR " +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER + "_Aktiv] = 1 OR " +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_KONZESSION + "_Aktiv] = 1 OR " +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_VERTRIEB + "_Aktiv] = 1)",
                new DbParam("@p", DbParamTyp.Integer) { Wert = z.Projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = z.Traeger });

            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        // =================================================================
        //  Kleinigkeiten
        // =================================================================

        private static double? Zahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return null;
            object v = r[spalte];
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v);
        }

        private static int Ganzzahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return 0;
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static bool Schalter(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return false;
            object v = r[spalte];
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
