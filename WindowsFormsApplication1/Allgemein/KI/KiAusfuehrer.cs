using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Eintrag des Sitzungsgedaechtnisses (Fachkonzept 7.3).
    /// </summary>
    /// <remarks>
    /// VORLAEUFIG. Paket B6 loest den Speicher hier durch das gemeinsame
    /// Sitzungsgedaechtnis des Chats ab oder uebernimmt ihn. Bis dahin genuegt eine
    /// schlanke Liste im Ausfuehrer: sie traegt die Aktion <c>letzte_aktionen</c> und
    /// beantwortet die Frage „was hast du gemacht?".
    /// </remarks>
    public sealed class KiSitzungseintrag
    {
        /// <summary>Zeitpunkt der Ausfuehrung.</summary>
        public DateTime Zeitpunkt;

        /// <summary>Name der Aktion.</summary>
        public string Aktion = "";

        /// <summary>Schutzstufe.</summary>
        public Schutzstufe Stufe;

        /// <summary>Parameter als kompaktes JSON (invariant).</summary>
        public string Parameter = "{}";

        /// <summary>Betroffenes Projekt; 0 = keines.</summary>
        public int ProjektId;

        /// <summary>Ausgang des Versuchs.</summary>
        public KiStatus Status;

        /// <summary>Kurzfassung des Ergebnisses.</summary>
        public string Ergebnis = "";

        /// <summary>Laufzeit in Millisekunden.</summary>
        public long DauerMs;
    }

    /// <summary>
    /// Das Ergebnis der VORBEREITUNG einer bestaetigungspflichtigen Aktion
    /// (Fachkonzept 3.5, Punkte 1 und 2).
    /// </summary>
    /// <remarks>
    /// Entweder liegt eine offene <see cref="Freigabe"/> vor - dann ist alles geprueft,
    /// die Vorschau erzeugt und der Sicherungspunkt angelegt, und es fehlt nur noch der
    /// Klick. Oder es liegt eine <see cref="Ablehnung"/> vor; die ist dann bereits
    /// protokolliert und geht als <c>functionResponse</c> an das Modell zurueck.
    /// </remarks>
    public sealed class KiVorbereitung
    {
        internal KiVorbereitung(KiFreigabe freigabe, KiErgebnis ablehnung)
        {
            Freigabe = freigabe;
            Ablehnung = ablehnung;
        }

        /// <summary>Die offene Freigabe; <c>null</c>, wenn abgelehnt wurde.</summary>
        public KiFreigabe Freigabe { get; }

        /// <summary>Die Ablehnung; <c>null</c>, wenn die Vorbereitung gelungen ist.</summary>
        public KiErgebnis Ablehnung { get; }

        /// <summary>Liegt eine offene Freigabe vor?</summary>
        public bool Bereit => Freigabe != null;
    }

    /// <summary>
    /// Die Ausfuehrungsschicht des KI-Assistenten - der EINZIGE Ort, an dem eine
    /// Assistentenaktion den Bestand beruehrt (Fachkonzept 3.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Alle Hausfallen sind hier gebuendelt, damit keine einzelne Aktion sie kennen muss:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>UI-Thread.</b> Die Bestandscontroller sind nicht threadsicher, und
    /// <see cref="DataRepository"/> haelt seinen dialogfreien Modus PROZESSWEIT
    /// (<c>Allgemein\DataRepository.cs:48-58</c>). Jeder Datenbankzugriff laeuft deshalb
    /// auf dem UI-Thread; besteht keine Oberflaeche (Aktionsharnisch, Konsolenlauf), laeuft
    /// er auf dem rufenden Thread - dann gibt es keinen zweiten.
    /// </description></item>
    /// <item><description>
    /// <b>Einlaeufigkeit.</b> Immer nur EINE Aktion gleichzeitig. Ein zweiter Aufruf wird
    /// ABGEWIESEN, nicht eingereiht - sonst stauten sich Anfragen hinter einem langen Lauf,
    /// und der Anwender bekaeme Antworten auf Fragen, die er laengst vergessen hat.
    /// Die Sperre gilt fuer die Vorbereitung UND fuer den Lauf, aber ausdruecklich NICHT
    /// fuer die Wartezeit dazwischen: eine Minute Bedenkzeit darf den Assistenten nicht
    /// eine Minute lang lahmlegen (Fachkonzept 3.5, Punkt 5).
    /// </description></item>
    /// <item><description>
    /// <b>Dialogfreiheit.</b> Jede Aktion laeuft in <c>DataRepository.EngineModus()</c>
    /// (<c>:77</c>); die still gesammelten Meldungen holt <c>StilleFehlerAbholen()</c>
    /// (<c>:86</c>) unmittelbar danach ab und legt sie ins <see cref="KiErgebnis"/>. So
    /// erscheint keine MessageBox hinter dem Chatfenster, und die Meldungen gehen nicht
    /// verloren. WICHTIG: Weil Schalter und Sammelliste prozessweit sind, wird der
    /// Datenzugriff je Aktion ABGESCHLOSSEN, bevor irgendetwas parallelisiert wird.
    /// </description></item>
    /// <item><description>
    /// <b>Abbruch.</b> Ein <see cref="CancellationToken"/> geht durch; Stufe 1 ist zu kurz,
    /// um ihn auszuwerten, aber der Weg steht fuer die Rechenaktionen der Etappe 4.
    /// </description></item>
    /// <item><description>
    /// <b>Protokoll.</b> GENAU EINE Zeile je Ausfuehrungsversuch - auch fuer abgewiesene
    /// (Fachkonzept 3.6). Format und Leser stehen im Kern (<see cref="KiProtokoll"/>).
    /// Die Vorbereitung schreibt NUR dann eine Zeile, wenn sie abweist; gelingt sie,
    /// gehoert die Zeile zum spaeteren Lauf - sonst haette ein Versuch zwei Zeilen.
    /// </description></item>
    /// </list>
    /// <para>
    /// <b>Seit Etappe 3: keine Schreibaktion ohne Freigabe.</b> Der Riegel gegen
    /// unbestaetigtes Schreiben sitzt HIER und nicht (nur) in der Werkzeugrunde. Wer
    /// <see cref="AusfuehrenAsync(KiAufruf, CancellationToken)"/> ohne
    /// <see cref="KiFreigabe"/> mit einer Aktion der Stufe 2 ruft - aus der Oberflaeche,
    /// aus einem Prueflauf, aus kuenftigem Code -, bekommt eine Ablehnung. Damit haengt
    /// die Zusage „ohne Klick wird nichts geschrieben" nicht daran, dass jeder kuenftige
    /// Aufrufer daran denkt.
    /// </para>
    /// <para>
    /// Die sichtbaren Texte dieser Klasse stehen in <see cref="KiAusfuehrerTexte"/> und
    /// kommen seit Paket B5 aus <c>MyResource.Resource</c> - in beiden Sprachen.
    /// </para>
    /// </remarks>
    public static class KiAusfuehrer
    {
        /// <summary>0 = frei, 1 = eine Aktion laeuft.</summary>
        private static int _laeuft;

        /// <summary>Zaehler der tatsaechlich gelaufenen Aktionen (Fachkonzept 3.5, Punkt 5).</summary>
        private static long _laufmarke;

        private static readonly object _sitzungSperre = new object();
        private static readonly List<KiSitzungseintrag> _sitzung = new List<KiSitzungseintrag>();

        /// <summary>Hoechstzahl der Eintraege im Sitzungsgedaechtnis - gegen unbegrenztes Wachsen.</summary>
        private const int MAX_SITZUNG = 200;

        private static KiRegister _register;
        private static readonly object _registerSperre = new object();

        /// <summary>Parameternamen, aus denen die Projekt-ID der Protokollzeile stammt.</summary>
        private static readonly string[] PROJEKT_PARAMETER =
            { "projekt_id", "stamm_id", "nach_projekt", "von_projekt", "ganglinie_id" };

        // =================================================================== Register

        /// <summary>Das gefuellte Aktionsregister (einmal gebaut, dann fest).</summary>
        public static KiRegister Register
        {
            get
            {
                if (_register != null) return _register;
                lock (_registerSperre)
                {
                    if (_register == null) _register = KiAktionen.Erzeuge();
                }
                return _register;
            }
        }

        /// <summary>true, solange eine Assistentenaktion laeuft.</summary>
        public static bool Belegt => Volatile.Read(ref _laeuft) != 0;

        /// <summary>
        /// Stand des Aktionszaehlers. Jede tatsaechlich gelaufene Aktion erhoeht ihn; eine
        /// Freigabe, die einen aelteren Stand traegt, gilt als ueberholt
        /// (Fachkonzept 3.5, Punkt 5: „oder auf die eine andere Aktion folgte").
        /// </summary>
        public static long Laufmarke => Interlocked.Read(ref _laufmarke);

        /// <summary>
        /// Zeitquelle der Freigaben. Im Betrieb <see cref="DateTime.Now"/>; der
        /// Aktionsharnisch rueckt sie vor, um den Verfall nachzuweisen, ohne eine Minute
        /// zu warten.
        /// </summary>
        public static Func<DateTime> Uhr { get; set; } = () => DateTime.Now;

        /// <summary>
        /// Die Schreibrechtsfrage. Im Betrieb <c>LizenzManager.DarfSchreiben()</c>
        /// (<c>Allgemein\Lizenz\LizenzManager.cs:140</c>) - der Assistent ist deren
        /// erster Aufrufer ueberhaupt (Fachkonzept 4.5).
        /// </summary>
        /// <remarks>
        /// Austauschbar aus genau einem Grund: Der Aktionsharnisch muss BEIDE Antworten
        /// pruefen koennen, und ein echter Lizenzwechsel waere dafuer weder herstellbar
        /// noch zurueckdrehbar. Dieselbe Bauart wie <c>KiChatService.Modellkanal</c>: ein
        /// Prueffaden, kein Schalter fuer den Betrieb - das Modell kann ihn nicht
        /// erreichen, weil er kein Parameter irgendeiner Aktion ist.
        /// </remarks>
        public static Func<bool> Schreibrecht { get; set; } = LizenzManager.DarfSchreiben;

        /// <summary>
        /// Die Modalitaetsfrage: Ist gerade ein modaler Dialog offen? Alles ausser reinem
        /// Lesen wird dann abgewiesen (Fachkonzept 3.4, Pflicht 2).
        /// </summary>
        /// <remarks>
        /// Austauschbar aus demselben Grund wie <see cref="Schreibrecht"/>: Der
        /// Aktionsharnisch laeuft ohne Oberflaeche und koennte den Zustand „modaler Dialog
        /// offen" sonst gar nicht herstellen - ein Fenster, das er oeffnete, wuerde sein
        /// eigener <c>DialogWaechter</c> sofort wieder schliessen. Die Vorgabe ist die
        /// echte Abfrage ueber <c>Form.ActiveForm.Modal</c>.
        /// </remarks>
        public static Func<bool> ModalerDialog { get; set; } = ModalerDialogOffen;

        /// <summary>Pfad des Sicherungspunkts dieser Sitzung; leer, solange keiner noetig war.</summary>
        public static string SicherungPfad => KiSicherungspunkt.Pfad;

        /// <summary>Zusatzhinweis zum Sicherungspunkt (z. B. „Datenbank geoeffnet"); kann leer sein.</summary>
        public static string SicherungHinweis => KiSicherungspunkt.Hinweis;

        /// <summary>Vergisst den Sicherungspunkt der Sitzung (Sitzungswechsel, Prueflaeufe).</summary>
        public static void SicherungZuruecksetzen() => KiSicherungspunkt.Zuruecksetzen();

        /// <summary>
        /// Steuerelement, ueber das auf den UI-Thread gewechselt wird.
        /// </summary>
        /// <remarks>
        /// Paket B5 setzt hier das Chatfenster. Bleibt der Anker leer, sucht
        /// <see cref="UiAnker"/> das erste offene Formular; gibt es auch das nicht
        /// (Aktionsharnisch), laeuft die Aktion auf dem rufenden Thread.
        /// </remarks>
        public static Control Anker { get; set; }

        // ============================================================== Vorbereiten

        /// <summary>
        /// Bereitet eine bestaetigungspflichtige Aktion vor: Rechte, Sicherungspunkt,
        /// Vorbedingung, Vorschau, Bestaetigungstext - und liefert die offene Freigabe
        /// (Fachkonzept 3.5).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Es wird dabei nichts geschrieben</b> - ausser dem Sicherungspunkt, und der
        /// ist eine Kopie und keine Aenderung. Vorbedingung und Vorschau laufen im
        /// dialogfreien Modus auf dem UI-Thread, genau wie ein Lauf.
        /// </para>
        /// <para>
        /// <b>Warum der Sicherungspunkt schon hier entsteht</b> und nicht erst nach dem
        /// Klick: Er gehoert in den Bestaetigungstext (der Anwender soll VOR der
        /// Entscheidung sehen, wohin der Vorzustand gesichert ist), und ein Fehlschlag
        /// muss vor der Entscheidung auffallen, nicht danach. Weil er nur EINMAL je
        /// Sitzung entsteht, kostet eine abgelehnte Vorschau hoechstens beim ersten Mal
        /// eine Kopie.
        /// </para>
        /// </remarks>
        public static async Task<KiVorbereitung> VorbereitenAsync(KiAufruf aufruf,
                                                                  CancellationToken abbruch = default)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));

            KiAktion aktion = aufruf.Aktion;
            int projektId = ProjektAus(aufruf);
            DateTime beginn = DateTime.Now;

            // ---- Stufe ueberhaupt freigegeben? (Etappe 3: bis Stufe 2.)
            string gesperrt = KiRiegel.PruefeStufe(aktion);
            if (gesperrt != null)
                return Abweisen(beginn, aufruf, projektId, KiErgebnis.Abgelehnt(gesperrt));

            // ---- Eine Vorbereitung gibt es nur fuer das, was bestaetigt werden muss.
            if (!KiRiegel.BrauchtBestaetigung(aktion))
                return new KiVorbereitung(null, KiErgebnis.Abgelehnt(
                    string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.OhneBestaetigungspflicht,
                                  aktion.Name)));

            if (Interlocked.CompareExchange(ref _laeuft, 1, 0) != 0)
                return Abweisen(beginn, aufruf, projektId,
                                KiErgebnis.Abgelehnt(KiAusfuehrerTexte.LaeuftBereits));

            try
            {
                if (ModalitaetSperrt())
                    return Abweisen(beginn, aufruf, projektId,
                                    KiErgebnis.Abgelehnt(KiAusfuehrerTexte.ModalerDialog));

                if (abbruch.IsCancellationRequested)
                    return Abweisen(beginn, aufruf, projektId,
                                    KiErgebnis.Abgebrochen(KiAusfuehrerTexte.Abgebrochen));

                if (aktion.Ausfuehren == null)
                    return Abweisen(beginn, aufruf, projektId, KiErgebnis.Abgelehnt(
                        string.Format(CultureInfo.CurrentCulture, KiTexte.AktionOhneAusfuehrung, aktion.Name)));

                // Deklarationsfehler, der nicht bis zum Klick warten darf: Stufe 2 ohne
                // Vorschau kaeme gar nicht erst durch den Aktionskonstruktor - diese
                // Wache faengt nur den Fall ab, dass jemand sie kuenftig aufweicht.
                if (aktion.Vorschau == null)
                    return Abweisen(beginn, aufruf, projektId, KiErgebnis.Abgelehnt(
                        string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.VorschauFehlt, aktion.Name)));

                // ---- Lizenz (Fachkonzept 4.5). Erster Aufrufer von DarfSchreiben().
                string lizenz = SchreibrechtPruefen();
                if (lizenz != null)
                    return Abweisen(beginn, aufruf, projektId, KiErgebnis.Abgelehnt(lizenz));

                // ---- Sicherungspunkt (Fachkonzept 4.4, Punkt 1). Fehlschlag SPERRT.
                string sicherung;
                string sicherungsgrund = KiSicherungspunkt.Sicherstellen(out sicherung);
                if (sicherungsgrund != null)
                    return Abweisen(beginn, aufruf, projektId, KiErgebnis.Abgelehnt(sicherungsgrund));

                // ---- Vorbedingung und Vorschau, dialogfrei auf dem UI-Thread.
                Vorschaubefund befund = await AufUiThread(() => VorschauLauf(aufruf)).ConfigureAwait(true);

                if (befund.Grund != null)
                    return Abweisen(beginn, aufruf, projektId,
                                    KiErgebnis.Abgelehnt(befund.Grund).MitMeldungen(befund.Meldungen));

                // ---- Der Bestaetigungstext stammt AUSSCHLIESSLICH aus dem Kern
                //      (Fachkonzept 3.5, Punkt 2) - nie aus Modelltext.
                DateTime jetzt = Uhr();
                TimeSpan frist = TimeSpan.FromSeconds(KiFreigabe.VerfallSekunden);
                string text = KiBestaetigung.Erzeuge(aufruf, befund.Vorschau, CultureInfo.CurrentCulture,
                                                     sicherung, jetzt + frist);

                KiFreigabe freigabe = KiFreigabe.Erzeuge(aufruf, text, Uhr, frist, Laufmarke);
                return new KiVorbereitung(freigabe, null);
            }
            finally
            {
                // Die Sperre faellt VOR der Bedenkzeit - sonst waere der Assistent eine
                // Minute lang fuer alles andere blockiert.
                Volatile.Write(ref _laeuft, 0);
            }
        }

        // ================================================================ Ausfuehren

        /// <summary>
        /// Prueft die Rohwerte gegen das Register und fuehrt die Aktion aus.
        /// Das ist der Einstieg fuer Oberflaeche und Modellantwort.
        /// </summary>
        public static Task<KiErgebnis> AusfuehrenAsync(string aktionsname,
                                                         IReadOnlyDictionary<string, object> rohwerte,
                                                         CancellationToken abbruch = default)
        {
            KiPruefErgebnis pruefung = KiPruefung.Pruefe(Register, aktionsname, rohwerte);
            if (!pruefung.Gueltig)
            {
                // Auch der abgewiesene Versuch bekommt seine Protokollzeile.
                KiErgebnis abgelehnt = KiErgebnis.Abgelehnt(pruefung.FehlerText());
                KiAktion bekannt = Register.Finde(aktionsname);
                Vermerken(DateTime.Now, aktionsname ?? "", bekannt != null ? bekannt.Stufe : Schutzstufe.Lesen,
                          "{}", 0, abgelehnt);
                return Task.FromResult(abgelehnt);
            }
            return AusfuehrenAsync(pruefung.Aufruf, null, abbruch);
        }

        /// <summary>
        /// Fuehrt einen bereits gepruefen Aufruf OHNE Freigabe aus. Zulaessig ist damit
        /// nur, was keine Bestaetigung braucht (Stufe 1).
        /// </summary>
        public static Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf,
                                                         CancellationToken abbruch = default)
            => AusfuehrenAsync(aufruf, null, abbruch);

        /// <summary>Fuehrt einen bereits gepruefen Aufruf aus - mit der Freigabe des Anwenders.</summary>
        /// <param name="aufruf">Der gepruefte Aufruf.</param>
        /// <param name="freigabe">
        /// Die Freigabe aus <see cref="VorbereitenAsync"/>; <c>null</c> ist nur fuer
        /// Stufe 1 zulaessig.
        /// </param>
        /// <param name="abbruch">Abbruchmarke.</param>
        public static async Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, KiFreigabe freigabe,
                                                               CancellationToken abbruch = default)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));

            KiAktion aktion = aufruf.Aktion;
            int projektId = ProjektAus(aufruf);
            DateTime beginn = DateTime.Now;

            // ---- Einlaeufigkeit: abweisen statt einreihen (Fachkonzept 3.4, Pflicht 1).
            if (Interlocked.CompareExchange(ref _laeuft, 1, 0) != 0)
            {
                KiErgebnis belegt = KiErgebnis.Abgelehnt(KiAusfuehrerTexte.LaeuftBereits);
                Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, belegt);
                return belegt;
            }

            try
            {
                // ---- Modalitaet: ein offener modaler Dialog blockiert alles, was Fenster
                //      oeffnet oder schreibt (Fachkonzept 3.4, Pflicht 2). Reines Lesen
                //      bleibt zulaessig - es beruehrt weder Fenster noch Datenstand.
                if (aktion.Stufe != Schutzstufe.Lesen && ModalitaetSperrt())
                {
                    KiErgebnis modal = KiErgebnis.Abgelehnt(KiAusfuehrerTexte.ModalerDialog);
                    Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, modal);
                    return modal;
                }

                if (abbruch.IsCancellationRequested)
                {
                    KiErgebnis weg = KiErgebnis.Abgebrochen(KiAusfuehrerTexte.Abgebrochen);
                    Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, weg);
                    return weg;
                }

                if (aktion.Ausfuehren == null)
                {
                    KiErgebnis ohne = KiErgebnis.Abgelehnt(
                        string.Format(CultureInfo.CurrentCulture, KiTexte.AktionOhneAusfuehrung, aktion.Name));
                    Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ohne);
                    return ohne;
                }

                // ---- DER RIEGEL DER ETAPPE 3. Alles, was ueber Stufe 1 hinausgeht,
                //      braucht eine gueltige, eigens fuer DIESEN Aufruf erteilte und noch
                //      nicht eingeloeste Freigabe - sonst wird nichts geschrieben.
                if (KiRiegel.BrauchtBestaetigung(aktion))
                {
                    string sperre = FreigabeEinloesen(aufruf, freigabe);
                    if (sperre != null)
                    {
                        KiErgebnis ohneKlick = KiErgebnis.Abgelehnt(sperre);
                        Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ohneKlick);
                        return ohneKlick;
                    }
                }

                // ---- Der eigentliche Lauf, auf dem UI-Thread.
                KiErgebnis ergebnis = await AufUiThread(() => LaufMitEngineModus(aufruf, abbruch))
                                            .ConfigureAwait(true);

                // Der Zaehler steigt fuer JEDE gelaufene Aktion, auch fuer lesende: eine
                // Vorschau, auf die inzwischen irgendetwas gefolgt ist, beschreibt nicht
                // mehr den Zustand, den der Anwender gesehen hat.
                Interlocked.Increment(ref _laufmarke);

                Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ergebnis);
                return ergebnis;
            }
            finally
            {
                Volatile.Write(ref _laeuft, 0);
            }
        }

        /// <summary>
        /// Prueft und VERBRAUCHT die Freigabe. Rueckgabe <c>null</c> = darf laufen, sonst
        /// der Klartextgrund fuer Chat, Modell und Protokoll.
        /// </summary>
        /// <remarks>
        /// Hier stehen die drei Fragen der Bestaetigungsschicht beieinander: Gibt es
        /// ueberhaupt eine Freigabe? Gehoert sie zu GENAU DIESEM Aufruf (Verweisvergleich,
        /// nicht Namensvergleich)? Ist sie erteilt, unverfallen, unueberholt und noch
        /// nicht eingeloest? Danach werden Lizenz und Sicherungspunkt ein ZWEITES Mal
        /// geprueft - zwischen Vorschau und Klick kann eine Minute liegen, und in dieser
        /// Minute kann eine Lizenz ablaufen oder die Sicherungsdatei verschwinden.
        /// </remarks>
        private static string FreigabeEinloesen(KiAufruf aufruf, KiFreigabe freigabe)
        {
            if (freigabe == null)
                return string.Format(CultureInfo.CurrentCulture, KiTexte.FreigabeFehlt, aufruf.Name);

            if (!freigabe.GiltFuer(aufruf)) return KiTexte.FreigabeFremd;

            string lizenz = SchreibrechtPruefen();
            if (lizenz != null) return lizenz;

            string sicherung;
            string sicherungsgrund = KiSicherungspunkt.Sicherstellen(out sicherung);
            if (sicherungsgrund != null) return sicherungsgrund;

            return freigabe.Verbrauchen(Laufmarke);
        }

        /// <summary>Klartextgrund, wenn die Lizenz kein Schreiben erlaubt; sonst <c>null</c>.</summary>
        private static string SchreibrechtPruefen()
        {
            bool darf;
            try
            {
                Func<bool> frage = Schreibrecht;
                darf = frage == null || frage();
            }
            catch (Exception ex)
            {
                // Im Zweifel NICHT schreiben.
                return string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.KeinSchreibrecht, ex.Message);
            }

            if (darf) return null;

            string status;
            try { status = LizenzManager.StatusText(); }
            catch { status = ""; }

            return string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.KeinSchreibrecht, status);
        }

        /// <summary>Ergebnis eines Vorschaulaufs.</summary>
        private sealed class Vorschaubefund
        {
            internal string Vorschau = "";
            internal string Grund;
            internal string[] Meldungen = Array.Empty<string>();
        }

        /// <summary>
        /// Vorbedingung und Vorschau - beides LESEND, im dialogfreien Modus, auf dem
        /// UI-Thread.
        /// </summary>
        private static Vorschaubefund VorschauLauf(KiAufruf aufruf)
        {
            KiAktion aktion = aufruf.Aktion;
            var befund = new Vorschaubefund();

            using (DataRepository.EngineModus())
            {
                try
                {
                    string grund = aktion.Vorbedingung != null ? aktion.Vorbedingung(aufruf) : null;
                    if (!string.IsNullOrWhiteSpace(grund))
                    {
                        befund.Grund = grund;
                    }
                    else
                    {
                        string text = aktion.Vorschau(aufruf);
                        if (string.IsNullOrWhiteSpace(text))
                            befund.Grund = string.Format(CultureInfo.CurrentCulture,
                                                         KiAusfuehrerTexte.VorschauLeer, aktion.Name);
                        else
                            befund.Vorschau = text;
                    }
                }
                catch (OperationCanceledException)
                {
                    befund.Grund = KiAusfuehrerTexte.Abgebrochen;
                }
                catch (Exception ex)
                {
                    befund.Grund = string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.Ausnahme,
                                                 ex.GetType().Name, ex.Message);
                }
            }

            befund.Meldungen = DataRepository.StilleFehlerAbholen();
            return befund;
        }

        /// <summary>
        /// Vorbedingung, dialogfreier Modus, Aufruf des Bestands, stille Fehler abholen.
        /// Laeuft immer auf dem UI-Thread.
        /// </summary>
        private static KiErgebnis LaufMitEngineModus(KiAufruf aufruf, CancellationToken abbruch)
        {
            KiAktion aktion = aufruf.Aktion;
            var uhr = Stopwatch.StartNew();
            KiErgebnis ergebnis;
            string[] stilleFehler;

            // Der dialogfreie Modus umschliesst AUCH die Vorbedingung: sie liest ebenfalls
            // aus der Datenbank und wuerde sonst ihre eigene MessageBox zeigen.
            using (DataRepository.EngineModus())
            {
                try
                {
                    string grund = aktion.Vorbedingung != null ? aktion.Vorbedingung(aufruf) : null;
                    ergebnis = !string.IsNullOrWhiteSpace(grund)
                        ? KiErgebnis.Abgelehnt(grund)
                        : aktion.Ausfuehren(aufruf);

                    if (ergebnis == null)
                        ergebnis = KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.KeinErgebnis, aktion.Name));
                }
                catch (OperationCanceledException)
                {
                    ergebnis = KiErgebnis.Abgebrochen(KiAusfuehrerTexte.Abgebrochen);
                }
                catch (Exception ex)
                {
                    // KEINE Ausnahme nach aussen: der Chat bekommt einen Klartextgrund, das
                    // Protokoll die Zeile - ein Assistentenfehler darf die Anwendung nicht
                    // beenden.
                    ergebnis = KiErgebnis.Fehlgeschlagen(
                        string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.Ausnahme,
                                      ex.GetType().Name, ex.Message));
                }
            }

            // Erst NACH dem Bereich abholen - dann ist der Datenzugriff der Aktion
            // abgeschlossen und die prozessweite Sammlung gehoert eindeutig diesem Lauf.
            stilleFehler = DataRepository.StilleFehlerAbholen();
            uhr.Stop();

            if (abbruch.IsCancellationRequested && ergebnis.Status == KiStatus.Ausgefuehrt)
                ergebnis = KiErgebnis.Abgebrochen(KiAusfuehrerTexte.Abgebrochen);

            return ergebnis.MitMeldungen(stilleFehler).MitDauer(uhr.Elapsed);
        }

        // ================================================================== Threading

        /// <summary>
        /// Fuehrt <paramref name="arbeit"/> auf dem UI-Thread aus. Gibt es keine
        /// Oberflaeche (Aktionsharnisch), laeuft sie auf dem rufenden Thread.
        /// </summary>
        private static Task<T> AufUiThread<T>(Func<T> arbeit)
        {
            Control anker = UiAnker();

            if (anker == null || !anker.InvokeRequired)
                return Task.FromResult(arbeit());

            var quelle = new TaskCompletionSource<T>();
            anker.BeginInvoke((MethodInvoker)delegate
            {
                try { quelle.SetResult(arbeit()); }
                catch (Exception ex) { quelle.SetException(ex); }
            });
            return quelle.Task;
        }

        /// <summary>Das Steuerelement, ueber das der Wechsel auf den UI-Thread laeuft.</summary>
        private static Control UiAnker()
        {
            Control anker = Anker;
            if (anker != null && !anker.IsDisposed && anker.IsHandleCreated) return anker;

            // Hausmuster: ueber Application.OpenForms (u. a. Form_Stromspeicher.cs:103).
            try
            {
                foreach (Form f in Application.OpenForms)
                    if (f != null && !f.IsDisposed && f.IsHandleCreated) return f;
            }
            catch (InvalidOperationException)
            {
                // OpenForms kann sich waehrend des Durchlaufs aendern - dann eben ohne Anker.
            }
            return null;
        }

        /// <summary>Fragt die Modalitaet ueber den eingestellten Weg; im Zweifel frei.</summary>
        private static bool ModalitaetSperrt()
        {
            try
            {
                Func<bool> frage = ModalerDialog;
                return frage != null && frage();
            }
            catch { return false; }
        }

        /// <summary>Ist gerade ein modaler Dialog offen? (Die echte Abfrage.)</summary>
        private static bool ModalerDialogOffen()
        {
            try
            {
                Form aktiv = Form.ActiveForm;
                return aktiv != null && aktiv.Modal;
            }
            catch { return false; }
        }

        // ============================================================ Sitzung/Protokoll

        /// <summary>
        /// Die zuletzt geschriebene Protokollzeile - damit der Chat sie zeigen kann, ohne
        /// die Datei erneut zu lesen (Fachkonzept 3.6: die Zeile gehoert zum Ergebnis).
        /// </summary>
        public static string LetzteProtokollzeile { get; private set; } = "";

        /// <summary>Die Aktionen dieser Sitzung, juengste zuerst (Fachkonzept 7.3).</summary>
        public static IReadOnlyList<KiSitzungseintrag> LetzteAktionen(int anzahl)
        {
            lock (_sitzungSperre)
            {
                var treffer = new List<KiSitzungseintrag>();
                for (int i = _sitzung.Count - 1; i >= 0 && treffer.Count < anzahl; i--)
                    treffer.Add(_sitzung[i]);
                return treffer;
            }
        }

        /// <summary>Leert das Sitzungsgedaechtnis (Sitzungswechsel, Tests).</summary>
        public static void SitzungLeeren()
        {
            lock (_sitzungSperre) _sitzung.Clear();
        }

        /// <summary>
        /// Weist einen Versuch ab, OHNE ihn zu starten - und schreibt dabei die eine
        /// Protokollzeile, die jedem Versuch zusteht (Fachkonzept 3.6).
        /// </summary>
        /// <remarks>
        /// Gebraucht wird das genau dort, wo eine Aktion gar nicht erst vorbereitet werden
        /// darf: wenn es keinen Weg gibt, den Anwender zu fragen. Ohne diese Stelle bliebe
        /// der einfachste aller Faelle - „niemand da, der bestaetigen koennte" -
        /// unprotokolliert.
        /// </remarks>
        public static KiErgebnis AbweisenUndVermerken(KiAufruf aufruf, string grund)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));

            KiErgebnis ergebnis = KiErgebnis.Abgelehnt(grund ?? "");
            Vermerken(DateTime.Now, aufruf.Name, aufruf.Aktion.Stufe, aufruf.AlsJson(),
                      ProjektAus(aufruf), ergebnis);
            return ergebnis;
        }

        /// <summary>Protokolliert eine Abweisung der Vorbereitung und verpackt sie.</summary>
        private static KiVorbereitung Abweisen(DateTime beginn, KiAufruf aufruf, int projektId,
                                               KiErgebnis ergebnis)
        {
            Vermerken(beginn, aufruf.Name, aufruf.Aktion.Stufe, aufruf.AlsJson(), projektId, ergebnis);
            return new KiVorbereitung(null, ergebnis);
        }

        /// <summary>
        /// Schreibt Protokollzeile und Sitzungseintrag - die EINE Stelle, an der ein
        /// Versuch vermerkt wird.
        /// </summary>
        private static void Vermerken(DateTime zeitpunkt, string aktion, Schutzstufe stufe,
                                      string parameterJson, int projektId, KiErgebnis ergebnis)
        {
            string zeile = KiProtokoll.Zeile(zeitpunkt, aktion, stufe, parameterJson, projektId,
                                             ergebnis.Status, ergebnis.Kurzfassung(), ergebnis.Dauer);
            LetzteProtokollzeile = zeile;
            Schreibe(zeile);

            var eintrag = new KiSitzungseintrag
            {
                Zeitpunkt = zeitpunkt,
                Aktion = aktion,
                Stufe = stufe,
                Parameter = parameterJson,
                ProjektId = projektId,
                Status = ergebnis.Status,
                Ergebnis = ergebnis.Kurzfassung(),
                DauerMs = (long)Math.Round(ergebnis.Dauer.TotalMilliseconds)
            };

            lock (_sitzungSperre)
            {
                _sitzung.Add(eintrag);
                if (_sitzung.Count > MAX_SITZUNG) _sitzung.RemoveRange(0, _sitzung.Count - MAX_SITZUNG);
            }
        }

        /// <summary>Pfad der Protokolldatei - neben der Datenbank (Fachkonzept 3.6).</summary>
        public static string ProtokollPfad()
        {
            try
            {
                string ordner = Path.GetDirectoryName(DataRepository.GetDBPath());
                if (string.IsNullOrEmpty(ordner)) return null;
                return Path.Combine(ordner, KiProtokoll.Dateiname);
            }
            catch { return null; }
        }

        /// <summary>
        /// Haengt eine Zeile an die Protokolldatei. Schreibfehler werden STILL verschluckt -
        /// dasselbe Verhalten wie beim Migrationsprotokoll
        /// (<c>Allgemein\Update\SchemaMigration.cs:3465-3488</c>): ein nicht beschreibbarer
        /// Ordner darf die Aktion nicht scheitern lassen.
        /// </summary>
        private static void Schreibe(string zeile)
        {
            try
            {
                string pfad = ProtokollPfad();
                if (string.IsNullOrEmpty(pfad)) return;

                bool neu = !File.Exists(pfad);
                var text = new StringBuilder();
                if (neu) text.Append(KiProtokoll.Vorspann().Replace("\n", Environment.NewLine));
                text.Append(zeile).Append(Environment.NewLine);

                File.AppendAllText(pfad, text.ToString(), new UTF8Encoding(false));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
        }

        // ===================================================================== Hilfen

        /// <summary>Die Projekt-ID der Protokollzeile, aus den bekannten Parameternamen.</summary>
        private static int ProjektAus(KiAufruf aufruf)
        {
            foreach (string name in PROJEKT_PARAMETER)
                if (aufruf.Hat(name)) return aufruf.Id(name);

            int[] liste = aufruf.IdListe("projekt_ids");
            return liste.Length > 0 ? liste[0] : 0;
        }
    }

    /// <summary>
    /// Sichtbare Texte der Ausfuehrungsschicht.
    /// </summary>
    /// <remarks>
    /// Erledigt mit Paket B5: die Texte kommen aus <c>MyResource.Resource</c>. Die Klasse
    /// bleibt als EINE Fundstelle stehen und bildet den Fall auf den Schluessel ab.
    /// </remarks>
    internal static class KiAusfuehrerTexte
    {
        /// <summary>Einlaeufigkeit: es laeuft bereits etwas.</summary>
        internal static string LaeuftBereits => MyResource.Resource.KI_AUS_LAEUFT_BEREITS;

        /// <summary>Modalitaetsprüfung.</summary>
        internal static string ModalerDialog => MyResource.Resource.KI_AUS_MODALER_DIALOG;

        /// <summary>Abbruch durch den Anwender.</summary>
        internal static string Abgebrochen => MyResource.Resource.KI_AUS_ABGEBROCHEN;

        /// <summary>{0} = Ausnahmetyp, {1} = Meldung.</summary>
        internal static string Ausnahme => MyResource.Resource.KI_AUS_AUSNAHME;

        /// <summary>{0} = Aktionsname.</summary>
        internal static string KeinErgebnis => MyResource.Resource.KI_AUS_KEIN_ERGEBNIS;

        /// <summary>{0} = Lizenzstatus im Klartext.</summary>
        internal static string KeinSchreibrecht => MyResource.Resource.KI_AUS_KEIN_SCHREIBRECHT;

        /// <summary>{0} = Aktionsname.</summary>
        internal static string OhneBestaetigungspflicht => MyResource.Resource.KI_AUS_OHNE_BESTAETIGUNGSPFLICHT;

        /// <summary>{0} = Aktionsname.</summary>
        internal static string VorschauFehlt => MyResource.Resource.KI_AUS_VORSCHAU_FEHLT;

        /// <summary>{0} = Aktionsname.</summary>
        internal static string VorschauLeer => MyResource.Resource.KI_AUS_VORSCHAU_LEER;
    }
}
