using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ÄNDERUNGSSTEMPEL FÜR KOSTEN, PREISE UND KOSTENKATALOG - Anwenderauftrag „Änderungszeitpunkt
    // für Kosten und Preise" (Folge von #637).
    //
    // WOZU. Das Band „bitte neu berechnen" der Wirtschaftlichkeit fragt
    // WirtschaftlichkeitCtrl.ErgebnisAktuell. Die Frage kannte nur den Simulationslauf: Wer nach
    // der Rechnung einen Preis, eine Kostenposition oder einen Wirtschaftlichkeitsparameter
    // änderte, sah weiter die alten Zahlen ohne Hinweis. Ab diesem Schritt stempelt die DATENBANK
    // selbst jede solche Änderung - über Trigger, damit kein Dialog und kein Schreibweg vergessen
    // werden kann. Der Vergleich gegen den Zeitstempel des Ergebnisses steht in
    // KostenAenderungsstempel.
    //
    // ZWEI SPALTEN, beide TEXT und nullbar (die Tabellen sind STRICT):
    //   Tab_Projekt.Kosten_Geaendert            - der Projektstempel,
    //   Tab_Applikation.Kostenkatalog_Geaendert - der globale Stempel des Kostenkatalogs.
    // NULL heißt „seit dem Schritt nichts geändert". Kein DML: Die Spalten entstehen leer, kein
    // gespeichertes Ergebnis wird durch den Schritt veraltet.
    //
    // DAS FORMAT. Die Trigger schreiben datetime('now','localtime') - Ortszeit
    // „yyyy-MM-dd HH:mm:ss", sekundengenau, genau das Format, in dem
    // Tab_ErgebnisWirtschaftlichkeit.Zeitstempel steht (SqliteDatenzugriff.NormalisiereWert).
    //
    // WAS STEMPELT (Trigger AFTER INSERT/UPDATE/DELETE, FOR EACH ROW):
    //   Projektstempel - Tab_ProjektWerte, Tab_ProjektWirtschaftlichkeit, Tab_ProjektTarif,
    //     energy_project_settings, energy_price, Tab_ProjektPhotovoltaik je am Projekt der Zeile;
    //     Tab_Variante an Variante UND Stamm; Tab_Energieanlagen beim Anlegen und Löschen, beim
    //     Ändern nur über die Kostenspalten (SPALTEN_ENERGIEANLAGEN, UPDATE OF); Tab_Projekt nur
    //     bei geändertem Emission_Berechnungsmodus.
    //   Tab_Preisreihe und Tab_PreisreiheDaten - eine Reihe MIT Projekt stempelt ihr Projekt, eine
    //     Stammreihe (ID_Projekt leer) den Katalog: Sie gilt in jedem Projekt
    //     (PreisreiheCtrl.ReadVerfuegbare).
    //   Katalogstempel - energy_carrier, pricing_model, energy_conversion, Tab_Brennstoff_Stamm,
    //     Tab_BrennstoffKategorien, emissionswert, Tab_Gesetzesparameter, Tab_Kostenfaktor,
    //     Tab_KostenKomponente, Tab_Nutzungsdauer; emissionsart nur bei geändertem
    //     co2_aequivalent, Tab_Applikation nur bei geändertem Emission_Berechnungsmodus.
    //
    // OHNE STEMPEL, mit Absicht: Geräte-, Gebäude- und Einstellungstabellen (sie wirken über die
    // Simulation, deren Lauf ErgebnisAktuell schon prüft), Tab_Kraftwerkspark, Tab_ProjektWirkung,
    // die Kostenvorlagen, Tab_KostenGruppenKatalog und alle Ergebnistabellen.
    //
    // KEINE REKURSION. Die Trigger an Tab_Projekt und Tab_Applikation feuern nur
    // „UPDATE OF Emission_Berechnungsmodus"; ihr eigenes UPDATE setzt allein die Stempelspalte und
    // nennt den Modus nicht - es löst sie also nicht wieder aus, auch nicht unter
    // PRAGMA recursive_triggers = ON. Ihr WHEN verlangt einen geänderten Wert, ebenso am
    // co2_aequivalent von emissionsart: Der Pflegedialog der Emissionsarten schreibt die ganze
    // Zeile (EmissionskatalogCtrl.ArtAendern), eine Umbenennung ist keine Kostenänderung.
    //
    // KEIN WHEN AN Tab_Energieanlagen, mit Absicht: Der Speicherweg der Anlagen löscht und legt neu
    // an (WizardCtrl, AnlagenSql) - er stempelt über INSERT/DELETE ohnehin. Ein WHEN über die
    // siebzehn Spalten brächte daneben wenig und sperrte ein späteres DROP COLUMN einer davon
    // („no such column: OLD.…"); die Spaltenliste von UPDATE OF tut das nicht.
    //
    // TRIGGER SIND SCHEMA. Sie stehen in sqlite_master neben Tabellen und Indizes, reisen mit
    // VACUUM INTO und jeder Dateikopie und kommen mit der Auslieferungsvorlage. ABER: Ein
    // Tabellenneubau (umbenennen, neu anlegen, umkopieren, löschen) verwirft die Trigger der neu
    // gebauten Tabelle. Wer eine der Tabellen oben neu baut, ruft danach Anweisungen bzw.
    // Ausfuehren dieser Klasse (CREATE TRIGGER IF NOT EXISTS) - die Wache
    // KostenStempelSchemaTests hält die Testdatenbank gegen die volle Liste.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs,
    // das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Ddl - ein Paket führt keine Trigger, sein Import stempelt im Ziel).
    // ====================================================================================

    /// <summary>
    /// Die Änderungsstempel für Kosten, Preise und Kostenkatalog — zwei Spalten und ihre Trigger,
    /// EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass,
    /// Liste und Grenzen stehen im Kopf der Datei; den Vergleich führt
    /// <see cref="KostenAenderungsstempel"/>.
    /// </summary>
    public static class KostenStempelSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: lückenlos
        /// hinter <see cref="KesselBrennwertNachzug"/>.
        /// </summary>
        public const int SCHRITT = KesselBrennwertNachzug.SCHRITT + 1;

        /// <summary>Die Tabelle des Projektstempels.</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_PROJEKT;

        /// <summary>
        /// <c>Tab_Projekt.Kosten_Geaendert</c>: der Zeitpunkt der letzten Änderung an Kosten,
        /// Preisen oder Wirtschaftlichkeitsparametern DIESES Projekts; NULL = keine seit dem Schritt.
        /// </summary>
        public const string SPALTE_PROJEKT = "Kosten_Geaendert";

        /// <summary>Die Tabelle des globalen Stempels (eine Zeile).</summary>
        public const string TAB_APPLIKATION = SchemaKatalog.TAB_APPLIKATION;

        /// <summary>
        /// <c>Tab_Applikation.Kostenkatalog_Geaendert</c>: der Zeitpunkt der letzten Änderung am
        /// Kostenkatalog, der in jedem Projekt gilt; NULL = keine seit dem Schritt.
        /// </summary>
        public const string SPALTE_KATALOG = "Kostenkatalog_Geaendert";

        /// <summary>Der Typ beider Spalten: nullbarer Text ohne Vorgabe.</summary>
        public const string TYP = "TEXT";

        /// <summary>Der Ausdruck, mit dem die Trigger stempeln — Ortszeit, sekundengenau.</summary>
        public const string JETZT = "datetime('now','localtime')";

        /// <summary>Das Textformat der Stempel und des Ergebnis-Zeitstempels.</summary>
        public const string FORMAT = "yyyy-MM-dd HH:mm:ss";

        /// <summary>Welcher Stempel gesetzt wird.</summary>
        public enum Stempelart
        {
            /// <summary><see cref="SPALTE_PROJEKT"/> am Projekt der Zeile.</summary>
            Projekt,

            /// <summary><see cref="SPALTE_KATALOG"/> — gilt für jedes Projekt.</summary>
            Katalog,
        }

        /// <summary>Ein Trigger des Schritts.</summary>
        public sealed class Stempeltrigger
        {
            internal Stempeltrigger(string name, string tabelle, Stempelart art, string ereignis,
                                    IReadOnlyList<string> spalten, string sql)
            {
                Name = name;
                Tabelle = tabelle;
                Art = art;
                Ereignis = ereignis;
                Spalten = spalten;
                Sql = sql;
            }

            /// <summary>Der Name in <c>sqlite_master</c>.</summary>
            public string Name { get; }

            /// <summary>Die Tabelle, an der er hängt.</summary>
            public string Tabelle { get; }

            /// <summary>Welchen Stempel er setzt (bei den Preisreihen: Projekt, für Stammreihen dazu Katalog).</summary>
            public Stempelart Art { get; }

            /// <summary><c>INSERT</c>, <c>UPDATE</c> oder <c>DELETE</c>.</summary>
            public string Ereignis { get; }

            /// <summary>Die Spalten von <c>UPDATE OF</c>; leer = jede Änderung der Zeile.</summary>
            public IReadOnlyList<string> Spalten { get; }

            /// <summary>Die Anweisung — <c>CREATE TRIGGER IF NOT EXISTS …</c>.</summary>
            public string Sql { get; }
        }

        /// <summary>
        /// Die Projekttabellen mit ihrer Projektspalte, die bei JEDER Änderung einer Zeile
        /// stempeln (INSERT, UPDATE, DELETE). <c>Tab_ProjektWerte</c> führt das Projekt als
        /// <c>ProjektID</c>.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> PROJEKTTABELLEN = new[]
        {
            new KeyValuePair<string, string>(SchemaKatalog.TAB_PROJEKTWERTE, "ProjektID"),
            new KeyValuePair<string, string>("Tab_ProjektWirtschaftlichkeit", "ID_Projekt"),
            new KeyValuePair<string, string>("Tab_ProjektTarif", "ID_Projekt"),
            new KeyValuePair<string, string>("energy_project_settings", "ID_Projekt"),
            new KeyValuePair<string, string>("energy_price", "ID_Projekt"),
            new KeyValuePair<string, string>("Tab_ProjektPhotovoltaik", "ID_Projekt"),
        };

        /// <summary>Die Anlagentabelle: stempelt beim Anlegen und Löschen, beim Ändern nur über <see cref="SPALTEN_ENERGIEANLAGEN"/>.</summary>
        public const string TAB_ENERGIEANLAGEN = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary>
        /// Die Kostenspalten einer Anlagenzeile: Energieträger, KWKG je Anlage (alle elf
        /// <c>KWKG_*</c>), Steuerwahl, Aufteilung, Hilfsenergie und Kältestrom. Die Wache
        /// <c>KostenStempelSchemaTests</c> hält die <c>KWKG_*</c>-Liste gegen die Testdatenbank.
        /// </summary>
        public static readonly IReadOnlyList<string> SPALTEN_ENERGIEANLAGEN = new[]
        {
            "ID_Carrier",
            "KWKG_Stichtag", "KWKG_Inbetriebnahme", "KWKG_Anlagenart", "KWKG_Eigenstromfall",
            "KWKG_Satz_Einspeisung", "KWKG_Satz_Eigen", "KWKG_Vbh_Kontingent", "KWKG_Vbh_Jahresdeckel",
            "KWKG_Kostenanteil", "KWKG_Abwaermeabfuhr", "KWKG_Stromkennzahl",
            "Energiesteuer_Wahl", "Aufteilung_Methode", "Hilfsenergie_Anteil",
            "Kuehl_ID_Carrier", "Kuehl_EigenerZaehler",
        };

        /// <summary>Die Variantentabelle: stempelt Variante (<c>ID_Projekt</c>) UND Stamm (<c>ID_ProjektRef</c>).</summary>
        public const string TAB_VARIANTE = "Tab_Variante";

        /// <summary>Der Kopf der Preisreihen: mit Projekt der Projektstempel, ohne der Katalogstempel.</summary>
        public const string TAB_PREISREIHE = "Tab_Preisreihe";

        /// <summary>Die Werte der Preisreihen — stempeln über den Kopf ihrer Reihe.</summary>
        public const string TAB_PREISREIHEDATEN = "Tab_PreisreiheDaten";

        /// <summary>Die Spalte des Emissionsmodus an <c>Tab_Projekt</c> und <c>Tab_Applikation</c>.</summary>
        public const string SPALTE_EMISSIONSMODUS = SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS;

        /// <summary>Die Katalogtabellen, die bei JEDER Änderung einer Zeile den Katalog stempeln.</summary>
        public static readonly IReadOnlyList<string> KATALOGTABELLEN = new[]
        {
            "energy_carrier",
            "pricing_model",
            "energy_conversion",
            "Tab_Brennstoff_Stamm",
            "Tab_BrennstoffKategorien",
            SchemaKatalog.TAB_EMISSIONSWERT,
            "Tab_Gesetzesparameter",
            "Tab_Kostenfaktor",
            "Tab_KostenKomponente",
            "Tab_Nutzungsdauer",
        };

        /// <summary>Die Emissionsarten — stempeln den Katalog nur über <see cref="SPALTE_CO2_AEQUIVALENT"/>.</summary>
        public const string TAB_EMISSIONSART = SchemaKatalog.TAB_EMISSIONSART;

        /// <summary>Der Umrechnungsfaktor einer Emissionsart in CO₂-Äquivalente.</summary>
        public const string SPALTE_CO2_AEQUIVALENT = "co2_aequivalent";

        /// <summary>Alle Trigger des Schritts, in fester Reihenfolge.</summary>
        public static readonly IReadOnlyList<Stempeltrigger> Trigger = Baue();

        /// <summary>Alle Tabellen, an denen ein Trigger hängt oder auf die ein Rumpf schreibt.</summary>
        public static IReadOnlyList<string> Tabellen()
        {
            var liste = new List<string> { TAB_PROJEKT, TAB_APPLIKATION };
            foreach (Stempeltrigger t in Trigger)
                if (!liste.Contains(t.Tabelle, StringComparer.Ordinal)) liste.Add(t.Tabelle);
            return liste;
        }

        // =================================================================================
        //  Stand und Ausführung
        // =================================================================================

        /// <summary>Stehen beide Spalten und alle Trigger? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            return DataRepository.SpalteVorhanden(TAB_PROJEKT, SPALTE_PROJEKT) &&
                   DataRepository.SpalteVorhanden(TAB_APPLIKATION, SPALTE_KATALOG) &&
                   FehlendeTrigger().Count == 0;
        }

        /// <summary>Die Namen der Trigger dieses Schritts, die in der Datenbank fehlen.</summary>
        public static IReadOnlyList<string> FehlendeTrigger()
        {
            HashSet<string> da = VorhandeneTrigger();
            return Trigger.Where(t => !da.Contains(t.Name)).Select(t => t.Name).ToList();
        }

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL; leer, wenn alles steht
        /// (<b>wiederholbar</b>): erst die fehlenden Spalten, dann die fehlenden Trigger.
        /// Fehlt eine Tabelle ganz, scheitert ihre Anweisung benannt.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!DataRepository.SpalteVorhanden(TAB_PROJEKT, SPALTE_PROJEKT))
                    yield return new KeyValuePair<string, string>(
                        TAB_PROJEKT + "." + SPALTE_PROJEKT + " anlegen", SpalteAnlegen(TAB_PROJEKT, SPALTE_PROJEKT));
                if (!DataRepository.SpalteVorhanden(TAB_APPLIKATION, SPALTE_KATALOG))
                    yield return new KeyValuePair<string, string>(
                        TAB_APPLIKATION + "." + SPALTE_KATALOG + " anlegen", SpalteAnlegen(TAB_APPLIKATION, SPALTE_KATALOG));

                HashSet<string> da = VorhandeneTrigger();
                foreach (Stempeltrigger t in Trigger)
                    if (!da.Contains(t.Name))
                        yield return new KeyValuePair<string, string>("Trigger " + t.Name + " anlegen", t.Sql);
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre
        /// eigenen Helfer, aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der ausgeführten Anweisungen (Spalten und Trigger).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0)
                bericht?.Add("Stempelspalten und " + Trigger.Count.ToString(CultureInfo.InvariantCulture) +
                             " Trigger: vorhanden");
            return n;
        }

        // =================================================================================
        //  Bau der Trigger
        // =================================================================================

        /// <summary>
        /// Die Anweisung einer Stempelspalte. Tabelle und Spalte stehen als ARGUMENT, nicht als
        /// fertiger Text: Gegen die migrierte Testdatenbank gehalten wäre eine ausgeschriebene
        /// Anweisung zwangsläufig „duplicate column" (Muster <see cref="KesselHeizgrenzeSchema"/>).
        /// </summary>
        private static string SpalteAnlegen(string tabelle, string spalte)
        {
            return "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + spalte + "\" " + TYP;
        }

        private static HashSet<string> VorhandeneTrigger()
        {
            var da = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DataTable dt = DataRepository.GetDataTable("SELECT name FROM sqlite_master WHERE type = 'trigger'");
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    da.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));
            return da;
        }

        private static List<Stempeltrigger> Baue()
        {
            var liste = new List<Stempeltrigger>();

            // ---- Projekttabellen: jede Änderung einer Zeile stempelt ihr Projekt ----------------
            foreach (KeyValuePair<string, string> pt in PROJEKTTABELLEN)
            {
                string spalte = Q(pt.Value);
                liste.Add(Neu(pt.Key, Stempelart.Projekt, "INSERT", null, null,
                              ProjektStempel(Q("ID") + " = NEW." + spalte)));
                liste.Add(Neu(pt.Key, Stempelart.Projekt, "UPDATE", null, null,
                              ProjektStempel(Q("ID") + " IN (OLD." + spalte + ", NEW." + spalte + ")")));
                liste.Add(Neu(pt.Key, Stempelart.Projekt, "DELETE", null, null,
                              ProjektStempel(Q("ID") + " = OLD." + spalte)));
            }

            // ---- Anlagen: anlegen und löschen immer, ändern nur über die Kostenspalten ---------
            {
                string spalte = Q("ID_Projekt");
                liste.Add(Neu(TAB_ENERGIEANLAGEN, Stempelart.Projekt, "INSERT", null, null,
                              ProjektStempel(Q("ID") + " = NEW." + spalte)));
                liste.Add(Neu(TAB_ENERGIEANLAGEN, Stempelart.Projekt, "UPDATE", SPALTEN_ENERGIEANLAGEN, null,
                              ProjektStempel(Q("ID") + " IN (OLD." + spalte + ", NEW." + spalte + ")")));
                liste.Add(Neu(TAB_ENERGIEANLAGEN, Stempelart.Projekt, "DELETE", null, null,
                              ProjektStempel(Q("ID") + " = OLD." + spalte)));
            }

            // ---- Varianten: Variante UND Stamm -----------------------------------------------
            {
                string v = Q("ID_Projekt"), s = Q("ID_ProjektRef");
                liste.Add(Neu(TAB_VARIANTE, Stempelart.Projekt, "INSERT", null, null,
                              ProjektStempel(Q("ID") + " IN (NEW." + v + ", NEW." + s + ")")));
                liste.Add(Neu(TAB_VARIANTE, Stempelart.Projekt, "UPDATE", null, null,
                              ProjektStempel(Q("ID") + " IN (OLD." + v + ", OLD." + s + ", NEW." + v + ", NEW." + s + ")")));
                liste.Add(Neu(TAB_VARIANTE, Stempelart.Projekt, "DELETE", null, null,
                              ProjektStempel(Q("ID") + " IN (OLD." + v + ", OLD." + s + ")")));
            }

            // ---- Preisreihen: mit Projekt das Projekt, als Stammreihe den Katalog -----------------
            {
                string p = Q("ID_Projekt");
                liste.Add(Neu(TAB_PREISREIHE, Stempelart.Projekt, "INSERT", null, null,
                              ProjektStempel(Q("ID") + " = NEW." + p) + " " +
                              KatalogStempel(OhneProjekt("NEW." + p))));
                liste.Add(Neu(TAB_PREISREIHE, Stempelart.Projekt, "UPDATE", null, null,
                              ProjektStempel(Q("ID") + " IN (OLD." + p + ", NEW." + p + ")") + " " +
                              KatalogStempel(OhneProjekt("OLD." + p) + " OR " + OhneProjekt("NEW." + p))));
                liste.Add(Neu(TAB_PREISREIHE, Stempelart.Projekt, "DELETE", null, null,
                              ProjektStempel(Q("ID") + " = OLD." + p) + " " +
                              KatalogStempel(OhneProjekt("OLD." + p))));
            }
            {
                string r = Q("ID_Preisreihe");
                liste.Add(Neu(TAB_PREISREIHEDATEN, Stempelart.Projekt, "INSERT", null, null,
                              ReiheStempel("= NEW." + r)));
                liste.Add(Neu(TAB_PREISREIHEDATEN, Stempelart.Projekt, "UPDATE", null, null,
                              ReiheStempel("IN (OLD." + r + ", NEW." + r + ")")));
                liste.Add(Neu(TAB_PREISREIHEDATEN, Stempelart.Projekt, "DELETE", null, null,
                              ReiheStempel("= OLD." + r)));
            }

            // ---- das Projekt selbst: nur der Emissionsmodus, nur bei geändertem Wert ------------
            liste.Add(Neu(TAB_PROJEKT, Stempelart.Projekt, "UPDATE", new[] { SPALTE_EMISSIONSMODUS },
                          Geaendert(SPALTE_EMISSIONSMODUS),
                          ProjektStempel(Q("ID") + " = NEW." + Q("ID"))));

            // ---- Katalogtabellen: jede Änderung einer Zeile stempelt den Katalog ----------------
            foreach (string tabelle in KATALOGTABELLEN)
            {
                liste.Add(Neu(tabelle, Stempelart.Katalog, "INSERT", null, null, KatalogStempel(null)));
                liste.Add(Neu(tabelle, Stempelart.Katalog, "UPDATE", null, null, KatalogStempel(null)));
                liste.Add(Neu(tabelle, Stempelart.Katalog, "DELETE", null, null, KatalogStempel(null)));
            }

            // ---- Emissionsarten: nur der CO2-Äquivalenzfaktor ----------------------------------
            liste.Add(Neu(TAB_EMISSIONSART, Stempelart.Katalog, "UPDATE", new[] { SPALTE_CO2_AEQUIVALENT },
                          Geaendert(SPALTE_CO2_AEQUIVALENT), KatalogStempel(null)));

            // ---- die Vorgabe des Emissionsmodus: nur bei geändertem Wert ------------------------
            liste.Add(Neu(TAB_APPLIKATION, Stempelart.Katalog, "UPDATE", new[] { SPALTE_EMISSIONSMODUS },
                          Geaendert(SPALTE_EMISSIONSMODUS),
                          KatalogStempel(Q("ID") + " = NEW." + Q("ID"))));

            return liste;
        }

        /// <summary>
        /// Ein Trigger. Name: <c>trg_Kostenstempel_&lt;Tabelle&gt;_I|U|D</c> für den Projekt-,
        /// <c>trg_Katalogstempel_…</c> für den Katalogstempel.
        /// </summary>
        private static Stempeltrigger Neu(string tabelle, Stempelart art, string ereignis,
                                          IReadOnlyList<string> spalten, string wann, string rumpf)
        {
            string name = (art == Stempelart.Projekt ? "trg_Kostenstempel_" : "trg_Katalogstempel_") +
                          tabelle + "_" + ereignis.Substring(0, 1);
            string ausloeser = ereignis;
            if (spalten != null && spalten.Count > 0)
                ausloeser += " OF " + string.Join(", ", spalten.Select(Q));
            string sql = "CREATE TRIGGER IF NOT EXISTS " + Q(name) + " AFTER " + ausloeser + " ON " + Q(tabelle) +
                         " FOR EACH ROW" + (wann == null ? "" : " WHEN " + wann) +
                         " BEGIN " + rumpf + " END";
            return new Stempeltrigger(name, tabelle, art, ereignis,
                                      spalten ?? Array.Empty<string>(), sql);
        }

        /// <summary>Setzt den Projektstempel der Projekte, die die Bedingung trifft.</summary>
        private static string ProjektStempel(string bedingung)
        {
            return "UPDATE " + Q(TAB_PROJEKT) + " SET " + Q(SPALTE_PROJEKT) + " = " + JETZT +
                   " WHERE " + bedingung + ";";
        }

        /// <summary>Setzt den Katalogstempel — ohne Bedingung die (einzige) Zeile.</summary>
        private static string KatalogStempel(string bedingung)
        {
            return "UPDATE " + Q(TAB_APPLIKATION) + " SET " + Q(SPALTE_KATALOG) + " = " + JETZT +
                   (bedingung == null ? "" : " WHERE " + bedingung) + ";";
        }

        /// <summary>Eine Preisreihe ohne Projekt ist eine Stammreihe (<c>ID_Projekt</c> leer).</summary>
        private static string OhneProjekt(string ausdruck)
        {
            return "COALESCE(" + ausdruck + ", 0) <= 0";
        }

        /// <summary>
        /// Der Stempel einer Wertezeile: das Projekt ihrer Reihe, für eine Stammreihe der Katalog.
        /// Gefragt wird nach dem KOPF — steht er nicht mehr (Kaskade beim Löschen einer Reihe),
        /// hat dessen eigener Trigger schon gestempelt.
        /// </summary>
        private static string ReiheStempel(string vergleich)
        {
            string kopf = " FROM " + Q(TAB_PREISREIHE) + " AS r WHERE r." + Q("ID") + " " + vergleich;
            return ProjektStempel(Q("ID") + " IN (SELECT r." + Q("ID_Projekt") + kopf + ")") + " " +
                   KatalogStempel("EXISTS (SELECT 1" + kopf + " AND " + OhneProjekt("r." + Q("ID_Projekt")) + ")");
        }

        /// <summary>Die Bedingung „Wert geändert" — NULL-fest über <c>IS NOT</c>.</summary>
        private static string Geaendert(string spalte)
        {
            return "OLD." + Q(spalte) + " IS NOT NEW." + Q(spalte);
        }

        /// <summary>Ein Bezeichner in doppelten Anführungszeichen.</summary>
        private static string Q(string bezeichner)
        {
            return "\"" + bezeichner + "\"";
        }
    }
}
