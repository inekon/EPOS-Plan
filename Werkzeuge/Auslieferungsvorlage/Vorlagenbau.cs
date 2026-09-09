using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Die fuenf Schritte des Auslieferungsstands, ausgefuehrt auf der ARBEITSKOPIE.
    /// Der Aufrufer (<see cref="Program"/>) stellt <see cref="DataRepository.PfadUeberschreibung"/>
    /// vorher auf diese Kopie; hier wird nur noch gearbeitet und protokolliert.
    /// </summary>
    internal sealed class Vorlagenbau
    {
        private readonly Argumente _arg;
        private readonly Bericht _bericht;

        internal Vorlagenbau(Argumente arg, Bericht bericht) { _arg = arg; _bericht = bericht; }

        /// <summary>Traf der Katalogwaechter zu — eine <c>*_STAMM</c>-Tabelle wurde leer?</summary>
        internal List<string> GeleerteKataloge { get; } = new List<string>();

        // =================================================================================
        //  SCHRITT 2 - Projektdaten entfernen
        // =================================================================================

        /// <summary>
        /// Raeumt alles ab, was an einem Projekt haengt: die Kopftabelle, die Tabellen mit
        /// eigener Projektspalte und die Folgetabellen darunter.
        ///
        /// <para><b>In EINER Transaktion mit aufgeschobener Fremdschluesselpruefung.</b>
        /// Woertlich wie <c>sql/tools/Reduziere-Testdatenbank.sql</c>: Die Kaskaden laufen
        /// weiter, nur der Zeitpunkt der Pruefung wandert an den Commit. Ohne das koennte
        /// ein <c>ON DELETE NO ACTION</c> - etwa
        /// <c>Tab_Energieanlagen.ID_PUFFER -&gt; Tab_Pufferspeicher</c> - eine Anweisung
        /// scheitern lassen, obwohl eine spaetere die verletzende Zeile ohnehin entfernt.
        /// Am Commit ist nichts Projektbezogenes mehr da, also ist auch nichts mehr zu
        /// verletzen.</para>
        ///
        /// <para><b>Die Null bleibt stehen.</b> <c>Tab_Projekt.ID</c> ist
        /// <c>INTEGER PRIMARY KEY AUTOINCREMENT</c> und damit nie 0 und nie NULL. Eine
        /// Zeile mit <c>ID_Projekt = 0</c> oder NULL gehoert deshalb KEINEM Projekt - sie
        /// ist Vorgabe- oder Katalogzeile (elf Katalogtabellen fuehren ihre Vorlagen so)
        /// und bleibt. Auch das ist die Regel des Vorbildskripts.</para>
        /// </summary>
        internal void ProjektdatenEntfernen(Projektsicht sicht)
        {
            _bericht.Abschnitt("Schritt 2 — Projektdaten entfernen");

            Dictionary<string, long> vorher = Zeilenzahlen(sicht.Projekttabellen);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("PRAGMA defer_foreign_keys = ON");

                // Stufe 1 - die Kopftabelle. Loest die Kaskaden aus.
                v.Ausfuehren("DELETE FROM \"" + Projektsicht.TAB_PROJEKT + "\"");

                // Stufe 1b/2 - jede Tabelle mit eigener Projektspalte. Nach der Kaskade
                // trifft das in einer intakten Datei nur noch die Tabellen OHNE
                // Fremdschluessel; die uebrigen sind bereits leer.
                foreach (Projektsicht.Projekttabelle p in sicht.Stufe1)
                    v.Ausfuehren("DELETE FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" IS NOT NULL " +
                                 "AND \"" + p.Spalte + "\" <> 0");

                // Stufe 3 - die Folgetabellen, Eltern vor Kindern. Bei eingeschalteten
                // Fremdschluesseln sind sie ueber die Kaskade schon leer; die Anweisungen
                // raeumen zusaetzlich Waisen aus der Access-Zeit ab, die
                // PRAGMA foreign_key_check sonst meldet.
                foreach (Projektsicht.Folgetabelle f in sicht.Stufe2)
                    v.Ausfuehren("DELETE FROM \"" + f.Tabelle + "\" WHERE \"" + f.Spalte + "\" IS NOT NULL " +
                                 "AND \"" + f.Spalte + "\" <> 0 AND \"" + f.Spalte + "\" NOT IN " +
                                 "(SELECT \"" + f.ElternSpalte + "\" FROM \"" + f.ElternTabelle + "\")");

                v.Commit();
            }

            Dictionary<string, long> nachher = Zeilenzahlen(sicht.Projekttabellen);

            _bericht.Zeile("Tabellen mit Projektbezug: " + sicht.Projekttabellen.Count +
                           "  (1 Kopftabelle, " + sicht.Stufe1.Count + " mit eigener Projektspalte, " +
                           sicht.Stufe2.Count + " Folgetabellen)");
            _bericht.Leer();
            _bericht.Tabellenkopf("Tabelle", "vorher", "nachher");
            long summeVor = 0, summeNach = 0;
            foreach (string t in sicht.Projekttabellen.OrderBy(x => x, StringComparer.Ordinal))
            {
                summeVor += vorher[t];
                summeNach += nachher[t];
                _bericht.Tabellenzeile(t, vorher[t], nachher[t]);
            }
            _bericht.Tabellenzeile("SUMME", summeVor, summeNach);
            _bericht.Leer();
            _bericht.Zeile("Verbleibende Zeilen sind ausnahmslos Vorgabe- und Katalogzeilen " +
                           "(Projektspalte 0 oder NULL).");
        }

        // =================================================================================
        //  SCHRITT 3a - Kataloge auf den Auslieferungsstand bringen
        // =================================================================================

        /// <summary>
        /// Setzt die Regel aus <c>Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md</c> 6.1
        /// Schritt 3 um: „In den <c>*_STAMM</c>-Tabellen behalten, was <c>ReadOnly = TRUE</c>
        /// traegt; das ist laut Namenskonvention genau der Auslieferungskatalog."
        ///
        /// <para><b>Eine Tabelle ohne Spalte <c>ReadOnly</c> bleibt vollstaendig.</b> Drei
        /// Kataloge fuehren die Spalte nicht (<c>Tab_Klimadaten_STAMM</c>,
        /// <c>Tab_Solar_STAMM</c>, <c>Tab_Kenndaten_Kuehlung_STAMM</c>) - dort gibt es
        /// nichts zu entscheiden, und ein „im Zweifel loeschen" waere der falsche Zweifel.</para>
        ///
        /// <para><b>Der Katalogwaechter.</b> Faellt eine Tabelle dabei von Zeilen auf NULL,
        /// wird das vermerkt; <see cref="Program"/> bricht dann ab. Grund: In der
        /// Entwicklungsdatenbank ist <c>ReadOnly</c> faktisch ein SCHREIBSCHUTZ der
        /// Oberflaeche (siehe <c>HeizkesselStammCtrl</c>, <c>GebaeudeStammCtrl</c>) und
        /// nicht die Auslieferungsmarke, als die die Namenskonvention sie beschreibt. Wer
        /// die Regel unbesehen anwendet, liefert einen LEEREN Katalog aus - und das faellt
        /// erst beim Kunden auf. Der Weg an der Sperre vorbei ist
        /// <c>--kataloge alle</c> oder <c>--katalogleerung-zulassen</c>, beides
        /// ausdruecklich.</para>
        /// </summary>
        internal void KatalogeBereinigen(Projektsicht sicht)
        {
            _bericht.Abschnitt("Schritt 3 — Auslieferungskataloge (*_STAMM)");
            _bericht.Zeile("Modus: " + (_arg.KatalogeVollstaendig
                ? "alle — jede Katalogzeile bleibt (--kataloge alle)"
                : "readonly — es bleibt, was ReadOnly = TRUE traegt (Setup-Konzept 6.1, Schritt 3)"));
            _bericht.Leer();
            // VORHER und NACHHER werden je einmal fuer ALLE Kataloge gezaehlt, nicht je
            // Tabelle um ihre eigene Anweisung herum. Der Grund ist eine Falle, die sonst
            // im Bericht unsichtbar bliebe: Die Kataloge haengen untereinander mit
            // ON DELETE CASCADE zusammen. Faellt Tab_Klimaregion_STAMM (32 Zeilen, keine
            // davon ReadOnly), nimmt die Kaskade Tab_Klimadaten_STAMM und Tab_Solar_STAMM
            // mit - obwohl beide gar keine Spalte ReadOnly haben und deshalb NIE
            // angefasst werden. Wer je Tabelle misst, liest fuer die zuerst gezaehlte
            // Tabelle noch den alten Stand und meldet einen Bestand, den es nicht mehr gibt.
            Dictionary<string, long> vorher = Zeilenzahlen(sicht.Stammtabellen);

            // Anweisung fuer Anweisung, NICHT in einer gemeinsamen Transaktion. Anders als
            // in Schritt 2 braucht es hier keine Klammer: Jede Anweisung steht fuer sich,
            // und die Kaskaden innerhalb EINER Anweisung sind ohnehin atomar. Eine
            // gemeinsame Transaktion ueber alle Kataloge zwaenge dagegen rund 400 000
            // Zeilen samt Kaskade in EIN Rollback-Journal - gemessen ueber zehnmal
            // langsamer als derselbe Lauf in Einzelschritten.
            if (!_arg.KatalogeVollstaendig)
                foreach (string t in sicht.Stammtabellen)
                    if (sicht.Hat(t, "ReadOnly"))
                        DataRepository.ExecuteNonQuery(
                            "DELETE FROM \"" + t + "\" WHERE \"ReadOnly\" IS NULL OR \"ReadOnly\" = 0");

            Dictionary<string, long> nachher = Zeilenzahlen(sicht.Stammtabellen);

            _bericht.Tabellenkopf("Katalogtabelle", "vorher", "nachher");
            long summeVor = 0, summeNach = 0;
            foreach (string t in sicht.Stammtabellen)
            {
                summeVor += vorher[t];
                summeNach += nachher[t];
                _bericht.Tabellenzeile(t + (sicht.Hat(t, "ReadOnly") ? "" : "  (ohne Spalte ReadOnly)"),
                                       vorher[t], nachher[t]);
                if (vorher[t] > 0 && nachher[t] == 0) GeleerteKataloge.Add(t);
            }
            _bericht.Tabellenzeile("SUMME", summeVor, summeNach);

            if (GeleerteKataloge.Count > 0)
            {
                _bericht.Leer();
                _bericht.Zeile("WARNUNG — diese Katalogtabellen sind durch die ReadOnly-Regel LEER geworden:");
                foreach (string t in GeleerteKataloge)
                    _bericht.Zeile("    " + t + (Hat(sicht, t) ? "" : "   (ohne Spalte ReadOnly — ueber eine Kaskade mitgerissen)"));
                _bericht.Zeile("In der Quelle traegt dort keine Zeile ReadOnly = TRUE. Entweder ist die Marke");
                _bericht.Zeile("im Bestand nicht gepflegt (sie wirkt dort als Schreibschutz der Oberflaeche —");
                _bericht.Zeile("siehe HeizkesselStammCtrl, GebaeudeStammCtrl), oder die Tabelle gehoert wirklich");
                _bericht.Zeile("nicht zur Auslieferung. Zu entscheiden ist das am Bestand, nicht vom Werkzeug.");
            }
        }

        // =================================================================================
        //  SCHRITT 3b - Personen- und rechnerbezogene Reste
        // =================================================================================

        /// <summary>
        /// Leert die Felder, die einen Menschen oder einen Rechner benennen und die die
        /// Projektbereinigung nicht mitnimmt.
        ///
        /// <para><b>Das ist genau eine Tabelle:</b> <c>Tab_Applikation</c>, die
        /// anwendungsweite Einzelzeilen-Statustabelle. Sie haengt an keinem Projekt und
        /// wuerde sonst den Namen des zuletzt geoeffneten KUNDENPROJEKTS mit ausliefern
        /// (in der Testdatenbank steht dort „Wöhler"). Geleert werden
        /// <c>Projektname</c>, <c>Beschreibung</c> und <c>Icon</c>;
        /// <c>ID_Projekt</c> geht auf 0. Genau diesen Zustand schreibt der Kern selbst,
        /// wenn das gemerkte Projekt geloescht wird
        /// (<c>ProjektCtrl.LoeschenMitVorarbeiten</c>): „kein Projekt geoeffnet". Der
        /// Schemastand, die Programmversion und der Emissionsmodus bleiben — sie sind
        /// Programm-, nicht Anwenderzustand.</para>
        ///
        /// <para><b>Lizenz-, KI- und Einstellungsdaten liegen NICHT in der Datenbank.</b>
        /// Das Lizenztoken, der Zeitanker und der KI-Schluessel gehen ueber
        /// <c>Dienste.Lizenzablage</c> in den Windows-Anmeldeinformationsspeicher bzw. den
        /// Schluesselbund, die Oberflaecheneinstellungen ueber <c>Dienste.Einstellungen</c>
        /// in die Registry; im Schema gibt es dafuer keine Tabelle. Statt das zu behaupten,
        /// SUCHT <see cref="Prueflauf"/> danach und fuehrt den Befund im Prueflauf mit —
        /// legt ein kuenftiger Schemaschritt eine solche Tabelle an, faellt sie dort auf.</para>
        ///
        /// <para><b>Die projektbezogenen Einstellungen sind schon weg.</b>
        /// <c>Tab_Einstellungen</c> und <c>Berichtskonfiguration</c> haengen an einem
        /// Projekt und fallen in Schritt 2 - letztere traegt in ihrer <c>KonfigJson</c>
        /// den zuletzt gewaehlten Ausgabeordner und damit einen Windows-Benutzernamen
        /// („C:\Users\…"). Deshalb prueft der Prueflauf zusaetzlich den ganzen Restbestand
        /// auf Pfadangaben.</para>
        /// </summary>
        internal void PersonenbezugAbraeumen(Projektsicht sicht)
        {
            _bericht.Abschnitt("Schritt 3b — personen- und rechnerbezogene Felder");

            var geleert = new List<string>();
            var zuweisungen = new List<string>();

            foreach (string s in new[] { "Projektname", "Beschreibung", "Icon" })
                if (sicht.Hat(Projektsicht.TAB_APPLIKATION, s)) { zuweisungen.Add("\"" + s + "\" = ''"); geleert.Add(Projektsicht.TAB_APPLIKATION + "." + s); }

            if (sicht.Hat(Projektsicht.TAB_APPLIKATION, "ID_Projekt"))
            { zuweisungen.Add("\"ID_Projekt\" = 0"); geleert.Add(Projektsicht.TAB_APPLIKATION + ".ID_Projekt (auf 0)"); }

            if (zuweisungen.Count > 0)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren("UPDATE \"" + Projektsicht.TAB_APPLIKATION + "\" SET " + string.Join(", ", zuweisungen));
                    v.Commit();
                }
            }

            foreach (string g in geleert) _bericht.Zeile("geleert: " + g);
            _bericht.Zeile("belassen: " + Projektsicht.TAB_APPLIKATION +
                           ".SchemaVersion / .Version_Major / .Version_Minor / .Emission_Berechnungsmodus " +
                           "(Programmzustand, kein Anwenderdatum)");
        }

        // =================================================================================
        //  SCHRITT 4 - Beispielprojekte einspielen
        // =================================================================================

        /// <summary>
        /// Spielt die uebergebenen Projektpakete ein.
        ///
        /// <para><b>Der Weg ist der des Beispielkonzepts.</b>
        /// <c>Konzept_Projektbeispiele_Dokumentation.md</c> 6.2 nennt als Voraussetzung
        /// einen „Projektexport ueber Datenbankgrenzen hinweg" und stellt fest, ein solcher
        /// existiere nicht. Inzwischen gibt es ihn: <c>ProjektExportImportCtrl</c> schreibt
        /// und liest <c>.wpx</c> (ZIP mit <c>manifest.json</c> und den Projektzeilen; die
        /// Kataloge reisen NICHT mit, sondern werden im Ziel ueber ihren fachlichen
        /// Schluessel wiedergefunden - genau die Zuordnung „ueber die fachlichen Schluessel,
        /// nicht ueber IDs", die das Konzept verlangt). Damit ist der offene Punkt des
        /// Konzepts geschlossen, und die Beispiele reisen als <c>.wpx</c>.</para>
        ///
        /// <para><b>Warum nicht „ausgewaehlte Projekte aus der Quelle behalten".</b> Das
        /// waere der zweite denkbare Weg und ist der schlechtere: Er bindet die Beispiele an
        /// die Entwicklungsdatenbank, macht sie unversionierbar und laesst sich weder im
        /// Beispiel-Repository noch im Setup nachvollziehen. Ein <c>.wpx</c> ist eine Datei,
        /// die neben dem Beispieltext liegt und mit ihm versioniert wird - der Aufbau, den
        /// das Beispielkonzept in seiner Ordnertabelle bereits vorsieht
        /// (<c>projekt.epx</c> „Export aus der Beispieldatenbank", versioniert).</para>
        ///
        /// <para><b>Der Name kommt aus dem Paket</b> (<c>manifest.sourceProject</c>), und
        /// ein bereits vorhandener Name bricht ab statt still umzubenennen: In eine frisch
        /// bereinigte Vorlage kann ein Name nur dann schon stehen, wenn zwei Pakete
        /// dasselbe Projekt fuehren - das ist ein Fehler des Aufrufers, kein Sonderfall.</para>
        /// </summary>
        internal bool BeispieleEinspielen(IReadOnlyList<string> pakete, out string fehler)
        {
            fehler = null;
            _bericht.Abschnitt("Schritt 4 — Beispielprojekte");

            if (pakete.Count == 0)
            {
                _bericht.Zeile("keine Pakete angegeben (--beispiele fehlt) — die Vorlage bleibt projektfrei");
                return true;
            }

            var io = new ProjektExportImportCtrl();
            foreach (string paket in pakete)
            {
                int id = io.Importieren(paket, null, ProjektExportImportCtrl.BeiVorhandenem.Abbrechen,
                                        null, out string meldung);
                if (id <= 0)
                {
                    fehler = "Paket " + Path.GetFileName(paket) + " liess sich nicht einspielen: " + meldung;
                    _bericht.Zeile("FEHLER: " + fehler);
                    return false;
                }
                _bericht.Zeile("eingespielt: " + Path.GetFileName(paket) + "  ->  Projekt-Id " +
                               id.ToString(CultureInfo.InvariantCulture));
                foreach (string z in io.LetzterBericht) _bericht.Zeile("    " + z);
            }
            return true;
        }

        // =================================================================================
        //  SCHRITT 5 - Verdichten
        // =================================================================================

        /// <summary>
        /// <c>VACUUM</c> und der Journalmodus der Auslieferung.
        ///
        /// <para><b>WAL, weil der Betrieb WAL erwartet.</b> <c>BETRIEB_SQLITE.md</c> nennt
        /// als Kenngroesse „Journalmodus: WAL, dateipersistent (einmalig vom Migrator
        /// gesetzt)". Die Arbeitskopie entsteht ueber <c>VACUUM INTO</c> und damit im
        /// Standardmodus <c>delete</c> - ohne diese Zeile lieferte die Vorlage eine Datei
        /// aus, die beim Kunden in einem anderen Journalmodus liefe als jede migrierte.
        /// Der Modus steht im Dateikopf und uebersteht das Kopieren beim Erststart.</para>
        ///
        /// <para><b>Die Beidateien bleiben nicht liegen.</b> Nach dem Umschalten wird der
        /// Verbindungspool geleert; SQLite entfernt <c>-wal</c> und <c>-shm</c> beim
        /// ordentlichen Schliessen der letzten Verbindung. Der Prueflauf sieht nach.</para>
        /// </summary>
        internal void Verdichten()
        {
            _bericht.Abschnitt("Schritt 5 — verdichten");

            long vorher = new FileInfo(DataRepository.PfadUeberschreibung).Length;
            DataRepository.ExecuteNonQuery("VACUUM");
            object modus = DataRepository.ExecuteScalar("PRAGMA journal_mode = WAL");
            long nachher = new FileInfo(DataRepository.PfadUeberschreibung).Length;

            _bericht.Zeile("VACUUM: " + Mb(vorher) + " -> " + Mb(nachher));
            _bericht.Zeile("Journalmodus: " + Convert.ToString(modus) + "   (Betriebserwartung: wal)");
        }

        // =================================================================================
        //  Helfer
        // =================================================================================

        private static bool Hat(Projektsicht sicht, string tabelle) => sicht.Hat(tabelle, "ReadOnly");

        private static Dictionary<string, long> Zeilenzahlen(IEnumerable<string> tabellen)
        {
            var d = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string t in tabellen) d[t] = Zaehle(t);
            return d;
        }

        internal static long Zaehle(string tabelle) => Zaehle2("SELECT COUNT(*) FROM \"" + tabelle + "\"");

        /// <summary>Eine Zaehlabfrage; <c>null</c> (kein Ergebnis) zaehlt als 0.</summary>
        internal static long Zaehle2(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null ? 0L : Convert.ToInt64(o);
        }

        internal static string Mb(long bytes) =>
            (bytes / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
    }
}
