using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>Was ein Einspielen des freien Paketteils ergeben hat (<see cref="TwwPaketteilCtrl.Einspielen"/>).</summary>
    internal sealed class TwwPaketteilZahlen
    {
        /// <summary>Die Katalogversion, der die Zeilen des Paketteils beigetreten sind.</summary>
        internal string Katalogversion { get; set; } = "";

        /// <summary>
        /// <c>true</c>, wenn nichts eingespielt ist, weil der Katalog schon eine Katalogversion führt
        /// (nur bei <c>nurOhneKatalogversion</c>, dem Weg des Nachladens).
        /// </summary>
        internal bool Uebergangen { get; set; }

        internal int Tagesgangsaetze { get; set; }
        internal int Tagesgaenge { get; set; }
        internal int Nutzungsarten { get; set; }
        internal int Parameter { get; set; }
        internal int Bedarfstage { get; set; }
        internal int Ereignisse { get; set; }

        /// <summary>Die Nutzungsarten, an die ein Vorgabesatz der Zapfkategorien gebunden ist.</summary>
        internal int KategorienGebunden { get; set; }
    }

    /// <summary>
    /// Der Ausgang des selbsttätigen Nachladens (<see cref="TwwPaketteilCtrl.Nachladen"/>):
    /// <see cref="Versucht"/> = <c>false</c>, wenn nichts zu tun war (Tabellen fehlen oder eine
    /// Katalogversion steht); sonst nennt <see cref="Satz"/> das Ergebnis — den Hinweis
    /// <c>PAKETTEIL_NACHGELADEN</c> oder den benannten Fehler <c>PAKETTEIL_NACHLADEN_FEHLGESCHLAGEN</c>.
    /// <see cref="Bericht"/> trägt die Zeilen des Einspielens (Datei, Zeilenzahl, Meldungen).
    /// </summary>
    internal sealed record TwwPaketteilNachladen(bool Versucht, bool Erfolg, ZapfSatz Satz, IReadOnlyList<string> Bericht)
    {
        /// <summary>Nichts zu tun: ohne Tww-Tabellen oder mit Katalogversion.</summary>
        internal static readonly TwwPaketteilNachladen Nichts = new TwwPaketteilNachladen(false, false, null, new string[0]);
    }

    /// <summary>
    /// <b>Der freie Paketteil des Zapfprofilgenerators</b> (<c>Referenzlaeufe/Katalogpaket_frei/</c>,
    /// Umsetzungskonzept Zapfprofilgenerator Kapitel 6 (b), Stufe Z3): die Katalogdaten, die im
    /// Repositorium stehen dürfen — Parameter der Stochastik und der Speicherauslegung,
    /// Ecodesign-Zapfprofile samt Ereignissen, Zapfkategorien nach Jordan/Vajen und die aus VDI 6002
    /// <b>abgeleiteten</b> Nutzungsarten samt Tagesgangsätzen und Tagesgängen (ZU20) —, ohne die eine
    /// Datenbank weder stochastisch rechnet noch die Bedarfstag-Quelle (5) anbietet noch einen Katalog
    /// der Nutzungsarten führt.
    ///
    /// <para><b>EIN Einspielweg für zwei Aufrufer.</b> <see cref="Einspielen"/> ist derselbe Weg für
    /// <c>Werkzeuge/Auslieferungsvorlage</c> (die Vorlage einer Neuinstallation, gelesen aus dem Ordner
    /// über <see cref="TwwNutzungsartCtrl.PaketLesen"/>, dem Leser des Katalogimports) und für das
    /// <b>Nachladen</b> einer älteren Datenbank (<see cref="Nachladen"/>, gelesen aus den eingebetteten
    /// Ressourcen): dieselbe Prüfung jeder Datei, dieselbe Regel für eine gleiche Zeile, derselbe
    /// Status (<c>AUSLIEFERUNG</c>, <c>ReadOnly</c> 1) und dieselbe Bindung der Vorgabesätze. Die
    /// Katalogversion der Zeilen kommt aus der EINEN Regel des Kerns,
    /// <see cref="ZapfprofilCtrl.Zielkatalogversion"/> — derselben, die der Katalogimport für ein Paket
    /// ohne Katalogversion nimmt (Umsetzungskonzept Zapfprofilgenerator, N38): Vorlage, Nachladen und
    /// Import landen bei derselben Version.</para>
    ///
    /// <para><b>Warum das Nachladen nicht den Katalogimport nimmt.</b> Der Katalogimport des Anwenders
    /// („Import…" im Katalog der Brauchwasser-Nutzungsarten, <see cref="TwwNutzungsartCtrl.Importieren"/>)
    /// nimmt den Ordner des Paketteils unverändert an (N38), legt aber <b>Anwenderzeilen</b> an —
    /// <c>IMPORT</c>, <c>ReadOnly</c> 0, Herkunftsart <c>IMPORT</c> statt <c>VERFAHREN</c> bzw.
    /// <c>EIGENKONSTRUKTION</c> — und ersetzt einen vorhandenen Bedarfstag oder Parameter am Platz. Das
    /// Nachladen gibt einer älteren Datenbank dagegen den Auslieferungskatalog, den eine Neuinstallation
    /// mit der Vorlage bekommt: gesperrt wie jede Auslieferungszeile und mit der Herkunft des Pakets
    /// (nur daran erkennt <see cref="PaketteilNachfuehrung"/> eine Zeile des Paketteils in einem früheren
    /// Stand), ohne eine Zeile der Datenbank anzufassen.</para>
    ///
    /// <para><b>Eine Quelle.</b> Die eingebetteten Ressourcen sind die CSV-Dateien des Ordners
    /// selbst (<c>EPOS.Kern.csproj</c> bindet <c>..\Referenzlaeufe\Katalogpaket_frei\*.csv</c> mit dem
    /// festen Namen <see cref="RESSOURCE_PRAEFIX"/> + Dateiname ein) — keine Kopie. Die Wache
    /// <c>EPOS.Kern.Tests/TwwPaketteilNachladenTests</c> hält Ressourcen und Ordner byte-gleich.</para>
    /// </summary>
    internal static class TwwPaketteilCtrl
    {
        /// <summary>Der Präfix der eingebetteten Dateien des Paketteils (fester <c>LogicalName</c>).</summary>
        internal const string RESSOURCE_PRAEFIX = "EPOS.Kern.Katalogpaket_frei.";

        /// <summary>Wer vorgeht, wenn der Katalog eine gleiche Zeile führt: das externe Katalogpaket der Auslieferungsvorlage.</summary>
        internal const string VORRANG_KATALOGPAKET = "das Katalogpaket";

        /// <summary>Wer vorgeht, wenn der Katalog eine gleiche Zeile führt: die Datenbank, in die nachgeladen wird.</summary>
        internal const string VORRANG_DATENBANK = "die Datenbank";

        /// <summary>Die Quelle des Nachladens in Meldungen.</summary>
        internal const string QUELLE_EINGEBETTET = "(eingebettet in EPOS.Kern)";

        /// <summary>Die Tabellen des Paketteils in Einspielreihenfolge (Verwiesene zuerst).</summary>
        internal static readonly string[] TABELLEN =
        {
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM,
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM,
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM
        };

        /// <summary>
        /// Die Herkunftsarten, die eine Zeile des freien Paketteils tragen darf: <c>FREI</c> für eine
        /// frei verfügbare Quelle, <c>VERFAHREN</c> für einen aus einem Verfahren gerechneten Wert
        /// (die aus VDI 6002 abgeleiteten Nutzungsarten, ZU19/ZU20 — ihre Zahl steht in keiner
        /// Richtlinie, und ihre Quelle ist keine freie) und <c>EIGENKONSTRUKTION</c> für eine Setzung
        /// von INEKON aus einer eigenen Unterlage (die Setzungen der Speicherauslegung aus der Vorlage
        /// TWW-Auslegung V4, N28 — keine frei verfügbare Quelle, kein Verfahren).
        /// </summary>
        internal static readonly string[] HERKUNFT =
        {
            TwwSchema.HERKUNFT_FREI, TwwSchema.HERKUNFT_VERFAHREN, TwwSchema.HERKUNFT_EIGENKONSTRUKTION
        };

        /// <summary>Hält zwei gleichzeitige Nachladen desselben Prozesses auseinander.</summary>
        private static readonly object _sperre = new object();

        // =================================================================================
        //  Das Nachladen (Anwenderentscheid 29.09.2026)
        // =================================================================================

        /// <summary>
        /// <b>Lädt den freien Paketteil in eine Datenbank, der er fehlt</b> — benannt und wiederholbar.
        /// Eine ältere, über die Schemaschritte angehobene Datenbank führt die Tww-Tabellen, aber
        /// keinen Parameter und damit keine Katalogversion (als Auslieferung kommt der Paketteil sonst nur
        /// mit der Vorlage einer Neuinstallation); ohne Katalogversion ist der Generator nicht verfügbar.
        ///
        /// <para><b>Wann.</b> Nur, wenn alle Tww-Tabellen stehen UND
        /// <see cref="ZapfprofilCtrl.AktuelleKatalogversion"/> <c>null</c> ist — geprüft vorher und noch
        /// einmal im Vorgang. Eine vorhandene Katalogversion (auch eine eigene oder abweichende) wird nie
        /// angefasst; ohne Tww-Tabellen geschieht nichts. Gerufen von
        /// <see cref="ZapfprofilCtrl.Verfuegbar"/> vor der Ablehnung — der einen Stelle, die beide Schalen
        /// auf jedem Weg des Generators durchlaufen (Dialog, Auslegung, Messvergleich, Lauf).</para>
        ///
        /// <para><b>Wie.</b> Derselbe Weg wie die Auslieferungsvorlage (<see cref="Einspielen"/>), aus den
        /// eingebetteten Dateien, mit einem Unterschied: Führt die Datenbank eine gleiche Zeile (gleicher
        /// natürlicher Schlüssel in derselben Katalogversion), <b>bleibt ihre</b> — die Zeile des
        /// Paketteils tritt zurück (<see cref="VORRANG_DATENBANK"/>), keine Zeile der Datenbank wird
        /// gelöscht. Die Katalogversion ist die der Regel <see cref="ZapfprofilCtrl.Zielkatalogversion"/>
        /// — hier stets ihr Rückfall <see cref="ZapfprofilCtrl.KATALOGVERSION_RUECKFALL"/>, weil die
        /// Datenbank keinen Parameter führt. Geschrieben wird in der Freigabe
        /// <see cref="Schreibnaht.GRUND_BEREITSTELLUNG"/>: Es ist der Auslieferungskatalog, den die
        /// Vorlage einer Neuinstallation mitbringt, keine Anwenderänderung — deshalb der Einspielweg der
        /// Vorlage und nicht der Katalogimport (Klassenkommentar).</para>
        ///
        /// <para><b>Nie still.</b> Das Ergebnis geht als Hinweis (nachgeladen) bzw. als Warnung (Fehler)
        /// in das Laufprotokoll (<see cref="SimulationProtokoll.Aktuell"/>, das auch auf die Konsole
        /// schreibt) und kommt als <see cref="TwwPaketteilNachladen.Satz"/> zurück; ein Fehler rollt den
        /// ganzen Paketteil zurück.</para>
        /// </summary>
        internal static TwwPaketteilNachladen Nachladen()
        {
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return TwwPaketteilNachladen.Nichts;
            if (ZapfprofilCtrl.AktuelleKatalogversion() != null) return TwwPaketteilNachladen.Nichts;

            lock (_sperre)
            {
                if (ZapfprofilCtrl.AktuelleKatalogversion() != null) return TwwPaketteilNachladen.Nichts;

                var bericht = new List<string>();
                bool ok;
                string fehler;
                TwwPaketteilZahlen zahlen;
                try
                {
                    IReadOnlyList<TwwPaketdatei> dateien = Eingebettet();
                    using (Schreibnaht.Freigabe(Schreibnaht.GRUND_BEREITSTELLUNG))
                        ok = Einspielen(dateien, QUELLE_EINGEBETTET, VORRANG_DATENBANK, true, bericht.Add, out fehler, out zahlen);
                }
                catch (Exception ex)
                {
                    ok = false;
                    fehler = ex.Message;
                    zahlen = null;
                }

                if (!ok)
                {
                    ZapfSatz f = ZapfSatz.Neu("PAKETTEIL_NACHLADEN_FEHLGESCHLAGEN", fehler ?? "");
                    SimulationProtokoll.Aktuell.Warnung(f.Klartext);
                    return new TwwPaketteilNachladen(true, false, f, bericht);
                }
                if (zahlen == null || zahlen.Uebergangen) return TwwPaketteilNachladen.Nichts;

                ZapfSatz s = ZapfSatz.Neu("PAKETTEIL_NACHGELADEN", zahlen.Katalogversion, zahlen.Parameter,
                                          zahlen.Nutzungsarten, zahlen.Bedarfstage);
                SimulationProtokoll.Aktuell.Hinweis(s.Klartext);
                return new TwwPaketteilNachladen(true, true, s, bericht);
            }
        }

        /// <summary>
        /// Die eingebetteten Dateien des Paketteils (Name ohne Präfix, Text in UTF-8), geordnet nach
        /// Namen — dieselbe Form wie <see cref="TwwNutzungsartCtrl.PaketLesen"/> sie aus einem Ordner liest.
        /// </summary>
        internal static IReadOnlyList<TwwPaketdatei> Eingebettet()
        {
            Assembly kern = typeof(TwwPaketteilCtrl).Assembly;
            var dateien = new List<TwwPaketdatei>();
            foreach (string name in kern.GetManifestResourceNames()
                                        .Where(n => n.StartsWith(RESSOURCE_PRAEFIX, StringComparison.Ordinal))
                                        .OrderBy(n => n, StringComparer.Ordinal))
            {
                using (Stream strom = kern.GetManifestResourceStream(name))
                using (var leser = new StreamReader(strom, Encoding.UTF8, true))
                    dateien.Add(new TwwPaketdatei(name.Substring(RESSOURCE_PRAEFIX.Length), leser.ReadToEnd()));
            }
            return dateien;
        }

        // =================================================================================
        //  Das Einspielen (Auslieferungsvorlage und Nachladen)
        // =================================================================================

        /// <summary>
        /// Jeder Parameter des Paketteils ist ein Schlüssel, den das Programm liest
        /// (<see cref="TwwParameterkatalog"/>, ZU31), in seiner Einheit und in seinem Bereich — sonst
        /// lehnte ihn der Katalogimport ab, und die Auslieferung trüge eine stille oder verrutschte
        /// Zeile. Ein Verstoß nennt Datei, Zeile und Grund; Rückgabe: die Zahl der geprüften Zeilen.
        /// </summary>
        internal static int ParameterPruefen(List<Dictionary<string, object>> zeilen, List<string> orte)
        {
            for (int i = 0; i < zeilen.Count; i++)
            {
                string s = Convert.ToString(zeilen[i]["Schluessel"], CultureInfo.InvariantCulture);
                TwwParameterschluessel k = TwwParameterkatalog.Finden(s);
                if (k == null)
                    throw new InvalidDataException(orte[i] + ": den Parameter \"" + s + "\" liest das Programm nicht.");
                string einheit = zeilen[i].TryGetValue("Einheit", out object e) ? Convert.ToString(e, CultureInfo.InvariantCulture) : null;
                if (!k.EinheitPasst(einheit))
                    throw new InvalidDataException(orte[i] + ": \"" + s + "\" in der Einheit \"" + einheit + "\" statt \"" + k.Einheit + "\".");
                double w = Convert.ToDouble(zeilen[i]["Wert"], CultureInfo.InvariantCulture);
                if (!k.ImBereich(w))
                    throw new InvalidDataException(orte[i] + ": \"" + s + "\" = " + w.ToString("R", CultureInfo.InvariantCulture) +
                                                   " liegt ausserhalb " + TwwParameterkatalog.Bereichstext(k) + ".");
            }
            return zeilen.Count;
        }

        /// <summary>
        /// <b>Spielt den freien Paketteil ein</b> (Stufe Z3): die Dateien <paramref name="dateien"/>
        /// (Name samt <c>.csv</c>, Text) in den Katalog der geöffneten Datenbank, in EINEM Vorgang.
        ///
        /// <para><b>Format</b> wie das Katalogpaket (N2), mit drei Regeln des Paketteils: Jede Zeile
        /// trägt Status <c>AUSLIEFERUNG</c>, <c>ReadOnly</c> 1 (oder keine Spalte) und in jeder
        /// Provenienzgruppe eine Herkunftsart aus <see cref="HERKUNFT"/> — <c>FREI</c> für
        /// eine frei verfügbare Quelle, <c>VERFAHREN</c> für einen gerechneten Wert,
        /// <c>EIGENKONSTRUKTION</c> für eine Setzung von INEKON. Der Paketteil führt <b>keine Katalogversion</b>: Seine Zeilen treten der
        /// Katalogversion des Katalogs bei — <see cref="ZapfprofilCtrl.Zielkatalogversion"/>, dieselbe
        /// Regel wie im Katalogimport (N38) — sonst sähe der Parametersatz die
        /// Parameter der Stochastik nicht. Die <c>ID</c> eines Bedarfstags ist nur Schlüssel des
        /// Pakets (die Ereignisse verweisen über <c>ID_Bedarfstag</c> darauf); die Datenbank vergibt
        /// die echte; ebenso die <c>ID</c> eines Tagesgangsatzes, auf die Tagesgänge und Nutzungsarten
        /// über <c>ID_Tagesgangsatz</c> verweisen. Die <b>Zapfkategorien</b> führen keine <c>ID_Nutzungsart</c>: Sie sind
        /// Vorgabesätze, von denen jede Nutzungsart mit Status <c>AUSLIEFERUNG</c> ohne eigene
        /// Kategorien <b>den Satz ihrer Gruppe</b> bekommt (Stufe Z5). Die Gruppe steht in der
        /// Steuerspalte <see cref="TwwSchema.STEUERSPALTE_GRUPPE"/> — der einzigen Spalte des
        /// Paketteils, die keine Spalte der Tabelle ist; sie wird nicht geschrieben. Welche Gruppe
        /// eine Nutzungsart trägt, sagt <see cref="TwwSchema.Kategoriengruppe"/> (Kalenderart
        /// „Wohnen" oder Nichtwohnen). Ein Paketteil ohne die Spalte bindet seinen einen Satz an
        /// jede Nutzungsart; fehlt der Satz einer Gruppe, bleiben ihre Nutzungsarten ohne
        /// Kategorien — der Bericht meldet es, und sie rechnen nicht stochastisch.</para>
        ///
        /// <para><b>Schlüsselgleichheit.</b> Führt der Katalog dieselbe Zeile (Parameter: Schlüssel und
        /// Katalogversion; Bedarfstag, Tagesgangsatz und Nutzungsart: Bezeichner und Katalogversion),
        /// entscheidet <paramref name="vorrang"/>: <c>null</c> — der Paketteil ersetzt die Zeile (die
        /// Auslieferungsvorlage ohne Katalogpaket); sonst geht die Zeile des Katalogs vor
        /// (<see cref="VORRANG_KATALOGPAKET"/>, <see cref="VORRANG_DATENBANK"/>), und die Zeile des
        /// Paketteils tritt zurück. Beides meldet der Bericht.</para>
        ///
        /// <para><paramref name="nurOhneKatalogversion"/> = <c>true</c> (das Nachladen): Führt der Katalog
        /// im Vorgang schon eine Katalogversion, bleibt alles, wie es ist
        /// (<see cref="TwwPaketteilZahlen.Uebergangen"/>).</para>
        ///
        /// <para>Ein Fehler nennt Datei, Zeile und Grund (<paramref name="quelle"/> vorn) und rollt den
        /// ganzen Paketteil zurück. <paramref name="zeile"/> nimmt die Zeilen des Berichts auf.</para>
        /// </summary>
        internal static bool Einspielen(IReadOnlyList<TwwPaketdatei> dateien, string quelle, string vorrang,
                                        bool nurOhneKatalogversion, Action<string> zeile,
                                        out string fehler, out TwwPaketteilZahlen zahlen)
        {
            fehler = null;
            zahlen = null;
            void Z(string text) => zeile?.Invoke(text);

            // Schemaauskunft VOR der Transaktion (eigene Verbindung).
            var vorhanden = new HashSet<string>(TABELLEN.Where(DataRepository.TabelleVorhanden), StringComparer.Ordinal);
            if (vorhanden.Count == 0)
            {
                Z("keine Tww-Tabelle im Schema — nichts einzuspielen");
                zahlen = new TwwPaketteilZahlen { Uebergangen = true };
                return true;
            }
            bool mitNutzungsarten = DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM);

            var zeilen = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.Ordinal);
            var orte = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            try
            {
                IReadOnlyList<TwwPaketdatei> liste = dateien ?? new TwwPaketdatei[0];
                var bekannt = new HashSet<string>(TABELLEN.Select(t => t + ".csv"), StringComparer.OrdinalIgnoreCase);
                foreach (TwwPaketdatei d in liste.OrderBy(x => x.Name, StringComparer.Ordinal))
                    if (!bekannt.Contains(d.Name))
                        throw new InvalidDataException(d.Name + " gehoert nicht zum Paketteil (erlaubt: " +
                                                       string.Join(", ", TABELLEN) + ").");
                foreach (string t in TABELLEN)
                {
                    TwwPaketdatei d = liste.FirstOrDefault(x => string.Equals(x.Name, t + ".csv", StringComparison.OrdinalIgnoreCase));
                    if (d == null) throw new InvalidDataException(t + ".csv fehlt im Paketteil.");
                    if (!vorhanden.Contains(t))
                    {
                        // Etwa die Zapfkategorien in einer Quelle vor Schritt 115: benannt uebergangen.
                        Z("uebergangen: " + t + ".csv — die Tabelle fehlt im Schema der Quelle");
                        zeilen[t] = new List<Dictionary<string, object>>();
                        orte[t] = new List<string>();
                        continue;
                    }
                    (zeilen[t], orte[t]) = PaketteilLesen(t, d, Spaltentypen(t));
                }
                int bekannteParameter = ParameterPruefen(zeilen[TwwSchema.TAB_TWW_PARAMETER_STAMM],
                                                         orte[TwwSchema.TAB_TWW_PARAMETER_STAMM]);
                Z("ok      jeder Parameter des Paketteils ist ein Schluessel des Programms, in Einheit und Bereich (" +
                  bekannteParameter.ToString(CultureInfo.InvariantCulture) + ")");
            }
            catch (InvalidDataException ex)
            {
                fehler = "Freier Paketteil " + quelle + ": " + ex.Message;
                Z("FEHLER  " + fehler);
                return false;
            }

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    string eigeneVersion = ZapfprofilCtrl.AktuelleKatalogversion(v);
                    if (nurOhneKatalogversion && eigeneVersion != null)
                    {
                        // Ein anderer Weg war schneller (oder die Datenbank trug sie schon): nichts anfassen.
                        Z("uebergangen: der Katalog fuehrt die Katalogversion " + eigeneVersion + " — nichts eingespielt");
                        zahlen = new TwwPaketteilZahlen { Katalogversion = eigeneVersion, Uebergangen = true };
                        return true;
                    }
                    // Die EINE Regel des Kerns (ZapfprofilCtrl.Zielkatalogversion, N38): die Version der
                    // zuletzt angelegten Parameterzeile — genau die, die der Parametersatz liest —, sonst
                    // der Rueckfall. Derselbe Aufruf steht im Katalogimport.
                    string version = ZapfprofilCtrl.Zielkatalogversion(v);
                    Z("Katalogversion der Paketteil-Zeilen: " + version +
                      (string.IsNullOrWhiteSpace(eigeneVersion) ? " (der Katalog fuehrt keine eigene)" : " (die des Katalogs)"));
                    var meldungen = new List<string>();

                    // --- Tagesgangsätze, Tagesgänge und Nutzungsarten (ZU20) --------------------
                    // Die ID des Satzes ist Schlüssel des Pakets: Tagesgänge und Nutzungsarten
                    // verweisen darauf, die Datenbank vergibt die echte. Tritt ein Satz zurück
                    // (der Katalog führt ihn), treten seine Tagesgänge und die Nutzungsarten,
                    // die auf ihn zeigen, mit ihm zurück — benannt, nie still.
                    var satzIds = new Dictionary<long, long?>();
                    int nSaetze = 0, nGaenge = 0, nArten = 0;
                    List<Dictionary<string, object>> saetzeZ = zeilen[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM];
                    for (int i = 0; i < saetzeZ.Count; i++)
                    {
                        if (!(saetzeZ[i].TryGetValue("ID", out object roh) && roh is long schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM][i] + ": die Spalte ID " +
                                                           "(Schluessel des Pakets fuer Tagesgaenge und Nutzungsarten) fehlt.");
                        if (satzIds.ContainsKey(schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM][i] + ": ID " + schluesselId + " doppelt.");
                        string satzname = Convert.ToString(saetzeZ[i]["Bezeichner"], CultureInfo.InvariantCulture);
                        if (Gleich(v, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "Bezeichner", satzname, version, vorrang, meldungen))
                        {
                            satzIds[schluesselId] = null;
                            continue;
                        }
                        satzIds[schluesselId] = Einfuegen(v, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, saetzeZ[i], version);
                        nSaetze++;
                    }
                    List<Dictionary<string, object>> gaengeZ = zeilen[TwwSchema.TAB_TWW_TAGESGANG_STAMM];
                    for (int i = 0; i < gaengeZ.Count; i++)
                    {
                        if (!(gaengeZ[i].TryGetValue("ID_Tagesgangsatz", out object roh) && roh is long kopf) ||
                            !satzIds.TryGetValue(kopf, out long? neu))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_TAGESGANG_STAMM][i] +
                                                           ": ID_Tagesgangsatz verweist auf keinen Tagesgangsatz des Paketteils (Waise).");
                        if (neu == null) continue;                       // der Satz tritt zurueck, seine Gaenge mit ihm
                        Einfuegen(v, TwwSchema.TAB_TWW_TAGESGANG_STAMM,
                                  new Dictionary<string, object>(gaengeZ[i], StringComparer.Ordinal) { ["ID_Tagesgangsatz"] = neu.Value }, null);
                        nGaenge++;
                    }
                    List<Dictionary<string, object>> artenZ = zeilen[TwwSchema.TAB_TWW_NUTZUNGSART_STAMM];
                    for (int i = 0; i < artenZ.Count; i++)
                    {
                        if (!(artenZ[i].TryGetValue("ID_Tagesgangsatz", out object roh) && roh is long kopf) ||
                            !satzIds.TryGetValue(kopf, out long? neu))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_NUTZUNGSART_STAMM][i] +
                                                           ": ID_Tagesgangsatz verweist auf keinen Tagesgangsatz des Paketteils (Waise).");
                        string artname = Convert.ToString(artenZ[i]["Bezeichner"], CultureInfo.InvariantCulture);
                        if (neu == null)
                        {
                            meldungen.Add(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " \"" + artname + "\": ihr Tagesgangsatz tritt " +
                                          "zurueck — die Nutzungsart des Paketteils mit ihm");
                            continue;
                        }
                        if (Gleich(v, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bezeichner", artname, version, vorrang, meldungen))
                            continue;
                        Einfuegen(v, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
                                  new Dictionary<string, object>(artenZ[i], StringComparer.Ordinal) { ["ID_Tagesgangsatz"] = neu.Value }, version);
                        nArten++;
                    }

                    // --- Parameter ------------------------------------------------------------
                    int parameter = 0;
                    List<Dictionary<string, object>> p = zeilen[TwwSchema.TAB_TWW_PARAMETER_STAMM];
                    for (int i = 0; i < p.Count; i++)
                    {
                        string schluessel = Convert.ToString(p[i]["Schluessel"], CultureInfo.InvariantCulture);
                        if (Gleich(v, TwwSchema.TAB_TWW_PARAMETER_STAMM, "Schluessel", schluessel, version, vorrang, meldungen))
                            continue;
                        Einfuegen(v, TwwSchema.TAB_TWW_PARAMETER_STAMM, p[i], version);
                        parameter++;
                    }

                    // --- Bedarfstage samt Ereignissen -------------------------------------------
                    var ids = new Dictionary<long, long?>();
                    int tage = 0, ereignisse = 0;
                    List<Dictionary<string, object>> b = zeilen[TwwSchema.TAB_TWW_BEDARFSTAG_STAMM];
                    for (int i = 0; i < b.Count; i++)
                    {
                        if (!(b[i].TryGetValue("ID", out object roh) && roh is long schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_BEDARFSTAG_STAMM][i] + ": die Spalte ID " +
                                                           "(Schluessel des Pakets fuer die Ereignisse) fehlt.");
                        if (ids.ContainsKey(schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_BEDARFSTAG_STAMM][i] + ": ID " + schluesselId + " doppelt.");
                        string bezeichner = Convert.ToString(b[i]["Bezeichner"], CultureInfo.InvariantCulture);
                        if (Gleich(v, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Bezeichner", bezeichner, version, vorrang, meldungen))
                        {
                            ids[schluesselId] = null;
                            continue;
                        }
                        ids[schluesselId] = Einfuegen(v, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, b[i], version);
                        tage++;
                    }
                    List<Dictionary<string, object>> e = zeilen[TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM];
                    for (int i = 0; i < e.Count; i++)
                    {
                        if (!(e[i].TryGetValue("ID_Bedarfstag", out object roh) && roh is long kopf) || !ids.TryGetValue(kopf, out long? neu))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM][i] +
                                                           ": ID_Bedarfstag verweist auf keinen Bedarfstag des Paketteils (Waise).");
                        if (neu == null) continue;                        // der Kopf tritt zurueck, seine Ereignisse mit ihm
                        var z = new Dictionary<string, object>(e[i], StringComparer.Ordinal) { ["ID_Bedarfstag"] = neu.Value };
                        Einfuegen(v, TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, z, null);
                        ereignisse++;
                    }

                    // --- Zapfkategorien: der Vorgabesatz SEINER GRUPPE an jeder Nutzungsart ohne eigene --
                    List<Dictionary<string, object>> k = zeilen[TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM];
                    var arten = new List<(long Id, string Gruppe)>();
                    long eigene = 0;
                    if (mitNutzungsarten && k.Count > 0)
                    {
                        DataTable dt = v.Lese("SELECT ID, Kalenderart FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                              " WHERE Status = ? AND ID NOT IN " +
                                              "(SELECT ID_Nutzungsart FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ") ORDER BY ID",
                                              new DbParam("?", TwwSchema.STATUS_AUSLIEFERUNG));
                        foreach (DataRow r in dt.Rows)
                            arten.Add((Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture),
                                       TwwSchema.Kategoriengruppe(Convert.ToInt64(r["Kalenderart"], CultureInfo.InvariantCulture))));
                        eigene = Convert.ToInt64(v.Skalar("SELECT COUNT(DISTINCT ID_Nutzungsart) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM),
                                                 CultureInfo.InvariantCulture);
                    }
                    var ohneSatz = new List<string>();
                    var jeGruppe = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach ((long art, string gruppe) in arten)
                    {
                        List<Dictionary<string, object>> satz = Vorgabesatz(k, gruppe);
                        if (satz.Count == 0)
                        {
                            if (!ohneSatz.Contains(gruppe)) ohneSatz.Add(gruppe);
                            continue;
                        }
                        jeGruppe[gruppe] = jeGruppe.TryGetValue(gruppe, out int n) ? n + 1 : 1;
                        foreach (Dictionary<string, object> z in satz)
                            Einfuegen(v, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM,
                                      new Dictionary<string, object>(z, StringComparer.Ordinal) { ["ID_Nutzungsart"] = art }, null);
                    }

                    v.Commit();

                    Z("eingespielt: " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv  ->  " + nSaetze + " von " + saetzeZ.Count + " Zeile(n)");
                    Z("eingespielt: " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv  ->  " + nGaenge + " von " + gaengeZ.Count + " Zeile(n)");
                    Z("eingespielt: " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv  ->  " + nArten + " von " + artenZ.Count + " Zeile(n)");
                    Z("eingespielt: " + TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv  ->  " + parameter + " von " + p.Count + " Zeile(n)");
                    Z("eingespielt: " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + ".csv  ->  " + tage + " von " + b.Count + " Zeile(n)");
                    Z("eingespielt: " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM + ".csv  ->  " + ereignisse + " von " + e.Count + " Zeile(n)");
                    string gruppen = string.Join(", ", jeGruppe.OrderBy(g => g.Key, StringComparer.Ordinal)
                                                               .Select(g => g.Value + " x " + g.Key +
                                                                            " (" + Vorgabesatz(k, g.Key).Count + " Kategorien)"));
                    Z("Zapfkategorien (Vorgabesaetze, " + k.Count + " Zeile(n) in " +
                      Gruppen(k).Count + " Gruppe(n)): an " + jeGruppe.Sum(g => g.Value) +
                      " Nutzungsart(en) mit Status AUSLIEFERUNG ohne eigene Kategorien gebunden" +
                      (gruppen.Length > 0 ? " — " + gruppen : "") +
                      (eigene > 0 ? "; " + eigene + " Nutzungsart(en) fuehren eigene Kategorien des Katalogs" : "") +
                      (arten.Count == 0 ? " — der Katalog fuehrt keine solche Nutzungsart" : ""));
                    foreach (string g in ohneSatz)
                        Z("MELDUNG Zapfkategorien: der Paketteil fuehrt keinen Vorgabesatz der Gruppe \"" + g +
                          "\" — die Nutzungsarten dieser Gruppe bleiben ohne Kategorien und rechnen nicht stochastisch");
                    foreach (string m in meldungen) Z("MELDUNG " + m);

                    zahlen = new TwwPaketteilZahlen
                    {
                        Katalogversion = version,
                        Tagesgangsaetze = nSaetze,
                        Tagesgaenge = nGaenge,
                        Nutzungsarten = nArten,
                        Parameter = parameter,
                        Bedarfstage = tage,
                        Ereignisse = ereignisse,
                        KategorienGebunden = jeGruppe.Sum(g => g.Value)
                    };
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    fehler = "Freier Paketteil " + quelle + ": " + ex.Message;
                    Z("FEHLER  " + fehler);
                    return false;
                }
            }
        }

        /// <summary>
        /// Liest eine Datei des Paketteils: Spalten der Tabelle, getypt; die Regeln des Paketteils
        /// (keine Katalogversion, Kategorien ohne Nutzungsart, FREI/AUSLIEFERUNG/ReadOnly 1) geprüft.
        /// Rückgabe: die Zeilen und je Zeile ihr Ort (Datei, Zeile) für Fehlermeldungen.
        /// </summary>
        private static (List<Dictionary<string, object>>, List<string>) PaketteilLesen(string tabelle, TwwPaketdatei datei,
                                                                                      Dictionary<string, string> typen)
        {
            string name = datei.Name;
            List<List<string>> roh = CsvLesen(datei.Inhalt);
            if (roh.Count < 2) throw new InvalidDataException(name + ": keine Datenzeile.");
            List<string> kopf = roh[0].Select(s => s.Trim()).ToList();
            bool mitGruppe = tabelle == TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM;
            foreach (string s in kopf)
                if (!typen.ContainsKey(s) && !(mitGruppe && s == TwwSchema.STEUERSPALTE_GRUPPE))
                    throw new InvalidDataException(name + ": die Spalte \"" + s + "\" gibt es in " + tabelle + " nicht.");
            if (kopf.Contains("Katalogversion"))
                throw new InvalidDataException(name + ": der Paketteil fuehrt keine Katalogversion — seine Zeilen treten der " +
                                               "Katalogversion des Katalogs bei.");
            if (tabelle == TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM && kopf.Contains("ID_Nutzungsart"))
                throw new InvalidDataException(name + ": die Kategorien des Paketteils sind ein Vorgabesatz ohne ID_Nutzungsart.");
            bool kopfzeile = typen.ContainsKey("Status");
            // Die Herkunftsspalten der Tabelle: "Herkunftsart" oder — bei den Nutzungsarten — je
            // Provenienzgruppe eine "<Gruppe>_Herkunftsart". Der Tagesgangsatz führt keine (seine
            // Herkunft steht an seinen Tagesgängen), die Ereignisse weder sie noch Status.
            List<string> herkunft = typen.Keys.Where(s => s == "Herkunftsart" ||
                                                          s.EndsWith("_Herkunftsart", StringComparison.Ordinal))
                                              .OrderBy(s => s, StringComparer.Ordinal).ToList();
            if (kopfzeile && !kopf.Contains("Status"))
                throw new InvalidDataException(name + ": Status gehoert in jede Zeile des Paketteils.");
            foreach (string h in herkunft)
                if (!kopf.Contains(h))
                    throw new InvalidDataException(name + ": " + h + " gehoert in jede Zeile des Paketteils.");

            var zeilen = new List<Dictionary<string, object>>();
            var orte = new List<string>();
            for (int i = 1; i < roh.Count; i++)
            {
                List<string> z = roh[i];
                if (z.Count == 1 && string.IsNullOrWhiteSpace(z[0])) continue;
                string ort = name + " Zeile " + (i + 1).ToString(CultureInfo.InvariantCulture);
                if (z.Count != kopf.Count)
                    throw new InvalidDataException(ort + ": " + z.Count + " Felder, die Kopfzeile nennt " + kopf.Count + ".");
                var w = new Dictionary<string, object>(StringComparer.Ordinal);
                for (int c = 0; c < kopf.Count; c++)
                    w[kopf[c]] = kopf[c] == TwwSchema.STEUERSPALTE_GRUPPE && !typen.ContainsKey(kopf[c])
                        ? Gruppe(z[c], ort)
                        : Feldwert(z[c], typen[kopf[c]], ort + ", Spalte " + kopf[c]);
                foreach (string h in herkunft)
                    if (!HERKUNFT.Contains(Convert.ToString(w[h]), StringComparer.Ordinal))
                        throw new InvalidDataException(ort + ": " + h + " \"" + Convert.ToString(w[h]) + "\" — der Paketteil " +
                                                       "fuehrt nur " + string.Join(", ", HERKUNFT) + ".");
                if (kopfzeile)
                {
                    if (!string.Equals(Convert.ToString(w["Status"]), TwwSchema.STATUS_AUSLIEFERUNG, StringComparison.Ordinal))
                        throw new InvalidDataException(ort + ": Status \"" + Convert.ToString(w["Status"]) + "\" — der Paketteil fuehrt nur AUSLIEFERUNG.");
                    if (w.TryGetValue("ReadOnly", out object ro) && !(ro is long l && l == 1))
                        throw new InvalidDataException(ort + ": ReadOnly muss 1 sein (Auslieferung).");
                    if (typen.ContainsKey("ReadOnly")) w["ReadOnly"] = 1L;
                }
                zeilen.Add(w);
                orte.Add(ort);
            }
            return (zeilen, orte);
        }

        /// <summary>
        /// Führt der Katalog die Zeile mit diesem natürlichen Schlüssel schon (<paramref name="spalte"/>
        /// und Katalogversion)? Mit <paramref name="vorrang"/> gilt die Zeile des Katalogs (<c>true</c>,
        /// gemeldet); ohne ersetzt der Paketteil sie (sie fällt, <c>false</c>, gemeldet).
        /// </summary>
        private static bool Gleich(DbVorgang v, string tabelle, string spalte, string wert, string version, string vorrang,
                                   List<string> meldungen)
        {
            object id = v.Skalar("SELECT ID FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" = ? AND Katalogversion = ?",
                                 new DbParam("?", wert), new DbParam("?", version));
            if (id == null || id == DBNull.Value) return false;
            if (vorrang != null)
            {
                meldungen.Add(tabelle + " \"" + wert + "\" (" + version + "): " + vorrang + " fuehrt dieselbe Zeile — " +
                              "die Zeile des Paketteils tritt zurueck");
                return true;
            }
            v.Ausfuehren("DELETE FROM \"" + tabelle + "\" WHERE ID = ?", new DbParam("?", id));
            meldungen.Add(tabelle + " \"" + wert + "\" (" + version + "): die Zeile der Quelle ist durch die des Paketteils ersetzt");
            return false;
        }

        /// <summary>
        /// <b>Der Vorgabesatz einer Gruppe</b> aus den Kategoriezeilen des Paketteils: die Zeilen mit
        /// dieser Gruppe in ihrer Reihenfolge; führt der Paketteil keine (etwa ein älteres Paket ohne
        /// Steuerspalte), gelten die Zeilen ohne Gruppe für jede Nutzungsart.
        /// </summary>
        private static List<Dictionary<string, object>> Vorgabesatz(List<Dictionary<string, object>> zeilen, string gruppe)
        {
            var satz = zeilen.Where(z => Gruppe(z) == gruppe).ToList();
            return satz.Count > 0 ? satz : zeilen.Where(z => Gruppe(z) == null).ToList();
        }

        /// <summary>Die Gruppen, die die Kategoriezeilen des Paketteils führen (<c>null</c> = ohne Gruppe).</summary>
        private static List<string> Gruppen(List<Dictionary<string, object>> zeilen)
            => zeilen.Select(Gruppe).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>Die Gruppe einer gelesenen Kategoriezeile (<c>null</c>, wenn sie keine trägt).</summary>
        private static string Gruppe(Dictionary<string, object> zeile)
            => zeile.TryGetValue(TwwSchema.STEUERSPALTE_GRUPPE, out object g) && g != null
                ? Convert.ToString(g, CultureInfo.InvariantCulture)
                : null;

        /// <summary>
        /// Die Gruppe einer Paketzeile der Zapfkategorien (Steuerspalte <c>Gruppe</c>): „Wohnen",
        /// „Nichtwohnen" oder leer (dann bindet der Satz an jede Nutzungsart — Rückfall eines
        /// Paketteils ohne Gruppen). Jeder andere Text ist ein Fehler.
        /// </summary>
        private static object Gruppe(string roh, string ort)
        {
            string g = (roh ?? "").Trim();
            if (g.Length == 0) return null;
            if (g != TwwSchema.KATEGORIENGRUPPE_WOHNEN && g != TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN)
                throw new InvalidDataException(ort + ": Gruppe \"" + g + "\" — erlaubt sind \"" +
                                               TwwSchema.KATEGORIENGRUPPE_WOHNEN + "\", \"" +
                                               TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN + "\" und leer.");
            return g;
        }

        private static long Einfuegen(DbVorgang v, string tabelle, Dictionary<string, object> zeile, string version)
        {
            var spalten = zeile.Keys.Where(s => s != "ID" && s != TwwSchema.STEUERSPALTE_GRUPPE).ToList();
            var werte = spalten.Select(s => new DbParam("?", zeile[s])).ToList();
            if (version != null)
            {
                spalten.Add("Katalogversion");
                werte.Add(new DbParam("?", version));
            }
            return v.EinfuegenUndId("INSERT INTO \"" + tabelle + "\" (" + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                    ") VALUES (" + string.Join(", ", spalten.Select(_ => "?")) + ")", werte.ToArray());
        }

        // =================================================================================
        //  Lesen: CSV, Feldwerte, Spaltentypen (auch das Katalogpaket der Auslieferungsvorlage)
        // =================================================================================

        /// <summary>
        /// Ein Feld einer Paketzeile im Typ seiner Spalte: leer = <c>NULL</c>, <c>INT…</c> als ganze
        /// Zahl, <c>REAL…</c> als Zahl mit Dezimalpunkt, sonst Text. Keine Zahl, wo eine stehen muss,
        /// ist ein Fehler mit Ort.
        /// </summary>
        internal static object Feldwert(string roh, string typ, string ort)
        {
            if (roh == null || roh.Length == 0) return DBNull.Value;
            string t = (typ ?? "").ToUpperInvariant();
            if (t.StartsWith("INT", StringComparison.Ordinal))
            {
                if (long.TryParse(roh.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
                throw new InvalidDataException(ort + ": \"" + roh + "\" ist keine ganze Zahl.");
            }
            if (t.StartsWith("REAL", StringComparison.Ordinal))
            {
                if (double.TryParse(roh.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                throw new InvalidDataException(ort + ": \"" + roh + "\" ist keine Zahl (Dezimalpunkt).");
            }
            return roh;
        }

        /// <summary>Spaltenname → deklarierter Typ der Tabelle (<c>pragma_table_info</c>).</summary>
        internal static Dictionary<string, string> Spaltentypen(string tabelle)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name, type FROM pragma_table_info(?)", new DbParam("?", tabelle));
            foreach (DataRow r in dt.Rows) d[Convert.ToString(r["name"])] = Convert.ToString(r["type"]);
            return d;
        }

        /// <summary>
        /// CSV nach RFC 4180: Felder in doppelten Anführungszeichen dürfen Trenner,
        /// Zeilenumbrüche und verdoppelte Anführungszeichen tragen. Der Trenner ist
        /// <c>;</c>, wenn die Kopfzeile einen enthält, sonst <c>,</c>.
        /// </summary>
        internal static List<List<string>> CsvLesen(string text)
        {
            var zeilen = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return zeilen;
            if (text[0] == '﻿') text = text.Substring(1);

            int kopfEnde = text.IndexOf('\n');
            string kopf = kopfEnde < 0 ? text : text.Substring(0, kopfEnde);
            char trenner = kopf.IndexOf(';') >= 0 ? ';' : ',';

            var zeile = new List<string>();
            var feld = new StringBuilder();
            bool inAnf = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inAnf)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { feld.Append('"'); i++; }
                        else inAnf = false;
                    }
                    else feld.Append(c);
                    continue;
                }
                if (c == '"') { inAnf = true; continue; }
                if (c == trenner) { zeile.Add(feld.ToString()); feld.Clear(); continue; }
                if (c == '\r') continue;
                if (c == '\n')
                {
                    zeile.Add(feld.ToString()); feld.Clear();
                    zeilen.Add(zeile); zeile = new List<string>();
                    continue;
                }
                feld.Append(c);
            }
            if (feld.Length > 0 || zeile.Count > 0) { zeile.Add(feld.ToString()); zeilen.Add(zeile); }
            return zeilen;
        }
    }
}
