using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public sealed class KiAusfuehrung : IKiAusfuehrung
    {
        /// <summary>0 = frei, 1 = eine Aktion laeuft.</summary>
        private int _laeuft;

        /// <summary>Zaehler der tatsaechlich gelaufenen Aktionen (Fachkonzept 3.5, Punkt 5).</summary>
        private long _laufmarke;

        private readonly object _sitzungSperre = new object();
        private readonly List<KiSitzungseintrag> _sitzung = new List<KiSitzungseintrag>();

        /// <summary>Hoechstzahl der Eintraege im Sitzungsgedaechtnis - gegen unbegrenztes Wachsen.</summary>
        private const int MAX_SITZUNG = 200;

        private KiRegister _register;
        private readonly object _registerSperre = new object();

        /// <summary>Parameternamen, aus denen die Projekt-ID der Protokollzeile stammt.</summary>
        private readonly string[] PROJEKT_PARAMETER =
            { "projekt_id", "stamm_id", "nach_projekt", "von_projekt", "ganglinie_id" };

        // =================================================================== Register

        /// <summary>Das gefuellte Aktionsregister (einmal gebaut, dann fest).</summary>
        public KiRegister Register
        {
            get
            {
                if (_register != null) return _register;
                lock (_registerSperre)
                {
                    if (_register == null) _register = KiAktionen.Erzeuge(this);
                }
                return _register;
            }
        }

        /// <summary>true, solange eine Assistentenaktion laeuft.</summary>
        public bool Belegt => Volatile.Read(ref _laeuft) != 0;

        /// <summary>
        /// Stand des Aktionszaehlers. Jede tatsaechlich gelaufene Aktion erhoeht ihn; eine
        /// Freigabe, die einen aelteren Stand traegt, gilt als ueberholt
        /// (Fachkonzept 3.5, Punkt 5: „oder auf die eine andere Aktion folgte").
        /// </summary>
        public long Laufmarke => Interlocked.Read(ref _laufmarke);

        /// <summary>
        /// Zeitquelle der Freigaben. Im Betrieb <see cref="DateTime.Now"/>; der
        /// Aktionsharnisch rueckt sie vor, um den Verfall nachzuweisen, ohne eine Minute
        /// zu warten.
        /// </summary>
        public Func<DateTime> Uhr { get; set; } = () => DateTime.Now;

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
        public Func<bool> Schreibrecht { get; set; } = LizenzManager.DarfSchreiben;

        /// <summary>
        /// Die Modalitaetsfrage: Ist gerade ein modaler Dialog offen? Alles ausser reinem
        /// Lesen wird dann abgewiesen (Fachkonzept 3.4, Pflicht 2) - mit EINER Ausnahme:
        /// Formularaktionen (<see cref="KiAktion.Formularaktion"/>) verlangen die offene
        /// Maske, statt an ihr zu scheitern; die Begruendung steht bei der Weiche in
        /// <see cref="AusfuehrenAsync(KiAufruf, KiFreigabe, CancellationToken)"/>.
        /// </summary>
        /// <remarks>
        /// Austauschbar aus demselben Grund wie <see cref="Schreibrecht"/>: Der
        /// Aktionsharnisch laeuft ohne Oberflaeche und koennte den Zustand „modaler Dialog
        /// offen" sonst gar nicht herstellen - ein Fenster, das er oeffnete, wuerde sein
        /// eigener <c>DialogWaechter</c> sofort wieder schliessen.
        /// <para>
        /// <b>Seit Auftrag #201 OHNE Vorgabe.</b> Die echte Abfrage
        /// <c>Form.ActiveForm.Modal</c> ist WinForms und hat im Kern nichts zu suchen;
        /// sie steht seither in der Windows-Huelle
        /// (<c>KiAusfuehrungWindows.ModalerDialogOffen</c>) und wird von
        /// <c>Program.Main</c> hier eingelegt. Bleibt der Haken leer - iOS, Pruefstand,
        /// Konsolenlauf -, sperrt keine Modalitaet; das ist richtig, denn dort gibt es
        /// auch keine.
        /// </para>
        /// </remarks>
        public Func<bool> ModalerDialog { get; set; }

        /// <summary>
        /// Zweiter Haken derselben Frage: Steht in einer Razor-Oberflaeche eine
        /// UEBERLAGERUNG offen? (iU9-W15b.0d, Befund W15b-B17, Entscheid E-8.)
        /// </summary>
        /// <remarks>
        /// <para>
        /// In Blazor gibt es keine Modalitaet im WinForms-Sinn - eine
        /// <c>Ueberlagerung</c> ist ein <c>div</c>, und <c>Form.ActiveForm.Modal</c>
        /// meldet fuer das Chatfenster weiterhin <c>false</c>. Die Zusage des
        /// Fachkonzepts 3.4 (Pflicht 2) waere damit still verlorengegangen: Solange der
        /// Anwender in der Werkzeugliste steht, darf keine Assistentenaktion
        /// dazwischenfahren.
        /// </para>
        /// <para>
        /// Deshalb meldet die Chatkomponente ihren eigenen Ueberlagerungszustand hierher.
        /// Beide Haken werden ODER-verknuepft (<see cref="ModalitaetSperrt"/>) - der
        /// WinForms-Weg bleibt woertlich, wie er war, und der Aktionsharnisch tauscht
        /// weiterhin nur <see cref="ModalerDialog"/>.
        /// </para>
        /// </remarks>
        public Func<bool> Ueberlagerung { get; set; }

        /// <summary>Pfad des Sicherungspunkts dieser Sitzung; leer, solange keiner noetig war.</summary>
        public string SicherungPfad => KiSicherungspunkt.Pfad;

        /// <summary>Zusatzhinweis zum Sicherungspunkt (z. B. „Datenbank geoeffnet"); kann leer sein.</summary>
        public string SicherungHinweis => KiSicherungspunkt.Hinweis;

        /// <summary>Vergisst den Sicherungspunkt der Sitzung (Sitzungswechsel, Prueflaeufe).</summary>
        public void SicherungZuruecksetzen() => KiSicherungspunkt.Zuruecksetzen();

        /// <summary>
        /// Der Wechsel auf den Oberflaechenfaden - seit iU9-W15b.0c plattformfrei
        /// (Befund W15b-B16, Entscheid E-8).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bis W15b stand hier ein <c>Control</c>, das <c>Form_KiChat</c> im Konstruktor
        /// auf sich selbst setzte. Ein Blazor-Chat hat kein Steuerelement - er hat
        /// <c>ComponentBase.InvokeAsync</c>. Beide erfuellen dieselbe Zusage: "fuehre
        /// diese Arbeit dort aus, wo die Oberflaeche lebt, und melde dich, wenn sie
        /// fertig ist". Genau das ist die Form <c>Func&lt;Func&lt;Task&gt;, Task&gt;</c>.
        /// </para>
        /// <para>
        /// Bleibt der Weg leer (Aktionsharnisch, Konsolenlauf, iOS-Pruefmodus), laeuft die
        /// Aktion auf dem RUFENDEN Faden. Der fruehere Rueckfall ueber
        /// <c>Application.OpenForms</c> ist mit Auftrag #201 entfallen - er war die letzte
        /// WinForms-Stelle dieser Klasse, und er half nur dort, wo die Huelle ihren Weg
        /// ohnehin einlegt.
        /// </para>
        /// <para>
        /// <b>Warum das noetig ist.</b> Die Bestandscontroller sind nicht threadsicher,
        /// und <c>DataRepository</c> haelt seinen dialogfreien Modus PROZESSWEIT. Jeder
        /// Datenbankzugriff einer Assistentenaktion muss deshalb auf denselben Faden
        /// (Fachkonzept 3.4).
        /// </para>
        /// </remarks>
        public Func<Func<Task>, Task> AufOberflaeche { get; set; }

        /// <summary>
        /// Der Empfaenger der Fortschrittsschritte lang laufender Aktionen (Stufe 3,
        /// Etappe S3). <c>null</c> = niemand hoert zu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Er gehoert der Huelle, nicht der Aktion.</b> Das Chatfenster legt ihn ein,
        /// solange es steht, und nimmt ihn beim Schliessen wieder heraus - dieselbe
        /// Bauart wie <see cref="AufOberflaeche"/> und <see cref="Ueberlagerung"/>. Eine
        /// Aktion meldet immer, ob jemand zuhoert oder nicht
        /// (<see cref="KiLaufumgebung.Melde"/> schluckt den fehlenden Empfaenger).
        /// </para>
        /// <para>
        /// <b>Der Faden.</b> Ein <c>Progress&lt;T&gt;</c>, das auf dem Oberflaechenfaden
        /// erzeugt wurde, marshallt selbst (Fachkonzept 3.4) - hier wird nichts von Hand
        /// gewechselt.
        /// </para>
        /// </remarks>
        public IProgress<KiFortschritt> Fortschritt { get; set; }

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
        public async Task<KiVorbereitung> VorbereitenAsync(KiAufruf aufruf,
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
            //      Gefragt wird ueber KiBestaetigungspflicht und nicht direkt am Riegel:
            //      Bei abgeschalteter Feldsicherung braucht eine Formularaktion keine
            //      Bestaetigung mehr - dann gibt es fuer sie auch nichts vorzubereiten
            //      (Fachkonzept 11.5). Wuerde hier weiter der Riegel allein antworten,
            //      liefen Chat und Ausfuehrer auseinander.
            if (!KiBestaetigungspflicht.Gilt(aktion))
                return new KiVorbereitung(null, KiErgebnis.Abgelehnt(
                    string.Format(CultureInfo.CurrentCulture, KiAusfuehrerTexte.OhneBestaetigungspflicht,
                                  aktion.Name)));

            if (Interlocked.CompareExchange(ref _laeuft, 1, 0) != 0)
                return Abweisen(beginn, aufruf, projektId,
                                KiErgebnis.Abgelehnt(KiAusfuehrerTexte.LaeuftBereits));

            try
            {
                // Modalitaetsweiche wie im Lauf (siehe AusfuehrenAsync): eine
                // Formularaktion VERLANGT die offene Zielmaske und wird deshalb nicht
                // deswegen abgewiesen, dass ein Dialog offen ist.
                if (!aktion.Formularaktion && ModalitaetSperrt())
                    return Abweisen(beginn, aufruf, projektId,
                                    KiErgebnis.Abgelehnt(KiAusfuehrerTexte.ModalerDialog));

                if (abbruch.IsCancellationRequested)
                    return Abweisen(beginn, aufruf, projektId,
                                    KiErgebnis.Abgebrochen(KiAusfuehrerTexte.Abgebrochen));

                if (!aktion.Ausfuehrbar)
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
                //
                //      NICHT fuer reine Oberflaechen-Eintraege (Festlegung Paket F4):
                //      feld_setzen und formular_ausfuellen tragen Text in ein Eingabefeld
                //      ein und fassen die Datenbank nie an - eine 90-MB-Kopie sicherte dort
                //      einen Zustand, den die Aktion gar nicht verlassen kann. Welche
                //      Aktion den Sicherungspunkt braucht, steht an IHRER Deklaration
                //      (KiAktion.BrauchtSicherungspunkt) und nicht hier in einer Namensliste.
                //      dialog_aktion_ausfuehren behaelt ihn: Der ausgeloeste Knopf schreibt
                //      ueber den Bestand sehr wohl in die Datenbank.
                string sicherung = "";
                if (aktion.BrauchtSicherungspunkt)
                {
                    string sicherungsgrund = KiSicherungspunkt.Sicherstellen(out sicherung);
                    if (sicherungsgrund != null)
                        return Abweisen(beginn, aufruf, projektId, KiErgebnis.Abgelehnt(sicherungsgrund));
                }

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
        public Task<KiErgebnis> AusfuehrenAsync(string aktionsname,
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
                          "{}", 0, abgelehnt, bekannt != null && bekannt.Formularaktion);
                return Task.FromResult(abgelehnt);
            }
            return AusfuehrenAsync(pruefung.Aufruf, null, abbruch);
        }

        /// <summary>
        /// Fuehrt einen bereits gepruefen Aufruf OHNE Freigabe aus. Zulaessig ist damit
        /// nur, was keine Bestaetigung braucht (Stufe 1).
        /// </summary>
        public Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf,
                                                         CancellationToken abbruch = default)
            => AusfuehrenAsync(aufruf, null, abbruch);

        /// <summary>Fuehrt einen bereits gepruefen Aufruf aus - mit der Freigabe des Anwenders.</summary>
        /// <param name="aufruf">Der gepruefte Aufruf.</param>
        /// <param name="freigabe">
        /// Die Freigabe aus <see cref="VorbereitenAsync"/>; <c>null</c> ist nur fuer
        /// Stufe 1 zulaessig.
        /// </param>
        /// <param name="abbruch">Abbruchmarke.</param>
        public async Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf, KiFreigabe freigabe,
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
                Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, belegt,
                          aktion.Formularaktion);
                return belegt;
            }

            try
            {
                // ---- Modalitaet: ein offener modaler Dialog blockiert alles, was Fenster
                //      oeffnet oder schreibt (Fachkonzept 3.4, Pflicht 2). Reines Lesen
                //      bleibt zulaessig - es beruehrt weder Fenster noch Datenstand.
                //
                //      MODALITAETSWEICHE (Etappe 3b, Fachkonzept 11.4): Fuer eine
                //      FORMULARAKTION wird die Sperre umgedreht. Die Sperre gibt es, weil
                //      eine Aktion sonst hinter einem offenen Dialog in Daten oder Fenster
                //      griffe, die der Anwender gerade in der Hand hat. Eine Formularaktion
                //      tut das Gegenteil: Sie wirkt AUSSCHLIESSLICH in genau die Maske, vor
                //      der der Anwender sitzt - der ganze Sinn der Formularsteuerung. Waere
                //      sie hier gesperrt, koennte sie nie laufen, denn die vier Startmasken
                //      werden modal geoeffnet (ShowDialog).
                //
                //      Was an ihre Stelle tritt, steht NICHT hier, sondern in der
                //      Vorbedingung jeder Formularaktion (KiDialogZugriff.Aufloesen mit
                //      mussAktivSein): Die Zielmaske muss im Katalog stehen, genau einmal
                //      offen und das aktive Fenster sein - sonst Ablehnung im Klartext. Der
                //      Ausfuehrer bleibt damit frei von Maskenwissen, und die Bedingung
                //      steht bei der Aktion, die sie braucht.
                //
                //      Am Riegel aendert das nichts: Eine Formularaktion gehoert zu
                //      Schutzstufe.Schreiben und braucht dieselbe Bestaetigung wie jede
                //      andere Schreibaktion (KiRiegel haengt allein an der Stufe).
                if (aktion.Stufe != Schutzstufe.Lesen && !aktion.Formularaktion && ModalitaetSperrt())
                {
                    KiErgebnis modal = KiErgebnis.Abgelehnt(KiAusfuehrerTexte.ModalerDialog);
                    Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, modal,
                              aktion.Formularaktion);
                    return modal;
                }

                if (abbruch.IsCancellationRequested)
                {
                    KiErgebnis weg = KiErgebnis.Abgebrochen(KiAusfuehrerTexte.Abgebrochen);
                    Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, weg,
                              aktion.Formularaktion);
                    return weg;
                }

                if (!aktion.Ausfuehrbar)
                {
                    KiErgebnis ohne = KiErgebnis.Abgelehnt(
                        string.Format(CultureInfo.CurrentCulture, KiTexte.AktionOhneAusfuehrung, aktion.Name));
                    Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ohne,
                              aktion.Formularaktion);
                    return ohne;
                }

                // ---- DER RIEGEL DER ETAPPE 3. Alles, was ueber Stufe 1 hinausgeht,
                //      braucht eine gueltige, eigens fuer DIESEN Aufruf erteilte und noch
                //      nicht eingeloeste Freigabe - sonst wird nichts geschrieben.
                //
                //      Gefragt wird ueber KiBestaetigungspflicht, also unter
                //      Beruecksichtigung der Feldsicherung (Fachkonzept 11.5, Paket F4).
                //      Fuer JEDE gewoehnliche Schreibaktion aendert das nichts; nur eine
                //      Formularaktion kann bei abgeschalteter Sicherung ohne Freigabe
                //      laufen. Der Zweig darunter faengt genau diesen Fall auf.
                if (KiBestaetigungspflicht.Gilt(aktion))
                {
                    string sperre = FreigabeEinloesen(aufruf, freigabe);
                    if (sperre != null)
                    {
                        KiErgebnis ohneKlick = KiErgebnis.Abgelehnt(sperre);
                        Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ohneKlick,
                                  aktion.Formularaktion);
                        return ohneKlick;
                    }
                }
                else if (aktion.Stufe != Schutzstufe.Lesen)
                {
                    // ABGESCHALTETE FELDSICHERUNG - und nur die. Was hier entfaellt, ist
                    // ausschliesslich der KLICK des Anwenders; alles Uebrige der
                    // Schreibvorbedingung bleibt stehen. Ohne diesen Zweig fielen mit der
                    // Freigabe stillschweigend auch die Schreibrechtsfrage (Fachkonzept 4.5)
                    // und der Sicherungspunkt weg - beides gehoert nicht zum Schalter
                    // (Fachkonzept 11.5: „Der Schalter wirkt NUR auf die Feldbestaetigung").
                    string vorbedingung = Schreibvorbedingung(aktion);
                    if (vorbedingung != null)
                    {
                        KiErgebnis gesperrt2 = KiErgebnis.Abgelehnt(vorbedingung);
                        Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, gesperrt2,
                                  aktion.Formularaktion);
                        return gesperrt2;
                    }
                }

                // ---- Der eigentliche Lauf, auf dem UI-Thread.
                KiErgebnis ergebnis = await AufUiThread(() => LaufMitEngineModus(aufruf, abbruch))
                                            .ConfigureAwait(true);

                // Der Zaehler steigt fuer JEDE gelaufene Aktion, auch fuer lesende: eine
                // Vorschau, auf die inzwischen irgendetwas gefolgt ist, beschreibt nicht
                // mehr den Zustand, den der Anwender gesehen hat.
                Interlocked.Increment(ref _laufmarke);

                Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ergebnis,
                          aktion.Formularaktion);
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
        private string FreigabeEinloesen(KiAufruf aufruf, KiFreigabe freigabe)
        {
            if (freigabe == null)
                return string.Format(CultureInfo.CurrentCulture, KiTexte.FreigabeFehlt, aufruf.Name);

            if (!freigabe.GiltFuer(aufruf)) return KiTexte.FreigabeFremd;

            string vorbedingung = Schreibvorbedingung(aufruf.Aktion);
            if (vorbedingung != null) return vorbedingung;

            return freigabe.Verbrauchen(Laufmarke);
        }

        /// <summary>
        /// Die Vorbedingungen jeder schreibenden Aktion, die NICHT am Klick des Anwenders
        /// haengen: Schreibrecht und - wo die Aktion ihn braucht - der Sicherungspunkt.
        /// </summary>
        /// <returns>Der Klartextgrund, oder <c>null</c>, wenn nichts dagegen spricht.</returns>
        /// <remarks>
        /// <para>
        /// Zusammengezogen, weil es zwei Wege in den Lauf gibt: den gewoehnlichen ueber die
        /// eingeloeste Freigabe und - seit Paket F4 - den einer Formularaktion bei
        /// abgeschalteter Feldsicherung. Beide muessen dieselben Fragen stellen; zwei
        /// Fassungen wuerden auseinanderlaufen, und die zweite waere die laschere.
        /// </para>
        /// <para>
        /// Gefragt wird auch auf dem Freigabeweg ein ZWEITES Mal (die Vorbereitung hat es
        /// schon getan): Zwischen Vorschau und Klick kann eine Minute liegen, und in dieser
        /// Minute kann eine Lizenz ablaufen oder die Sicherungsdatei verschwinden.
        /// </para>
        /// </remarks>
        private string Schreibvorbedingung(KiAktion aktion)
        {
            if (aktion == null || aktion.Stufe == Schutzstufe.Lesen) return null;

            string lizenz = SchreibrechtPruefen();
            if (lizenz != null) return lizenz;

            // Begruendung der Regel steht bei VorbereitenAsync.
            if (!aktion.BrauchtSicherungspunkt) return null;

            string sicherung;
            return KiSicherungspunkt.Sicherstellen(out sicherung);
        }

        /// <summary>Klartextgrund, wenn die Lizenz kein Schreiben erlaubt; sonst <c>null</c>.</summary>
        private string SchreibrechtPruefen()
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
        private Vorschaubefund VorschauLauf(KiAufruf aufruf)
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
        /// <remarks>
        /// <b>Die Weiche zwischen kurz und lang steht HIER und nur hier</b> (Etappe S3):
        /// Eine Aktion mit <see cref="KiAktion.AusfuehrenLang"/> bekommt die
        /// <see cref="KiLaufumgebung"/> mit Fortschritt und Abbruchmarke; die 19 uebrigen
        /// behalten ihre Signatur woertlich.
        /// </remarks>
        private KiErgebnis LaufMitEngineModus(KiAufruf aufruf, CancellationToken abbruch)
        {
            var umgebung = new KiLaufumgebung(Fortschritt, abbruch);
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
                        : aktion.AusfuehrenLang != null
                            ? aktion.AusfuehrenLang(aufruf, umgebung)
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
        private async Task<T> AufUiThread<T>(Func<T> arbeit)
        {
            Func<Func<Task>, Task> weg = AufOberflaeche;
            if (weg != null)
            {
                T ergebnis = default(T);
                Exception fehler = null;

                // Die Ausnahme wird NICHT im Rueckruf weitergereicht: Ein Blazor-
                // InvokeAsync, dem eine Ausnahme entkommt, reisst den Renderer mit. Sie
                // wird hier gefangen und danach mit ihrem urspruenglichen Stapel wieder
                // geworfen - der Aufrufer sieht denselben Fehler wie zuvor.
                await weg(() =>
                {
                    try { ergebnis = arbeit(); }
                    catch (Exception ex) { fehler = ex; }
                    return Task.CompletedTask;
                }).ConfigureAwait(true);

                if (fehler != null) ExceptionDispatchInfo.Capture(fehler).Throw();
                return ergebnis;
            }

            // OHNE eingelegten Weg laeuft die Arbeit auf dem RUFENDEN Faden. Genau so
            // verhaelt sich der Aktionsharnisch, der Konsolenlauf und der iOS-Pruefmodus:
            // Wo keine Oberflaeche lebt, gibt es auch keinen zweiten Faden, auf den zu
            // wechseln waere.
            //
            // Der frueher hier stehende Rueckfall ueber Application.OpenForms ist mit
            // Auftrag #201 entfallen (Umzug in den Kern). Er half nur unter Windows, und
            // dort legt die Huelle AufOberflaeche ohnehin ein.
            await Task.CompletedTask.ConfigureAwait(true);
            return arbeit();
        }

        /// <summary>Fragt die Modalitaet ueber den eingestellten Weg; im Zweifel frei.</summary>
        private bool ModalitaetSperrt()
        {
            try
            {
                Func<bool> frage = ModalerDialog;
                if (frage != null && frage()) return true;
            }
            catch { /* im Zweifel frei - eine werfende Abfrage darf nicht sperren */ }

            try
            {
                Func<bool> ueber = Ueberlagerung;
                return ueber != null && ueber();
            }
            catch { return false; }
        }

        // ============================================================ Sitzung/Protokoll

        /// <summary>
        /// Die zuletzt geschriebene Protokollzeile - damit der Chat sie zeigen kann, ohne
        /// die Datei erneut zu lesen (Fachkonzept 3.6: die Zeile gehoert zum Ergebnis).
        /// </summary>
        public string LetzteProtokollzeile { get; private set; } = "";

        /// <summary>Die Aktionen dieser Sitzung, juengste zuerst (Fachkonzept 7.3).</summary>
        public IReadOnlyList<KiSitzungseintrag> LetzteAktionen(int anzahl)
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
        public void SitzungLeeren()
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
        public KiErgebnis AbweisenUndVermerken(KiAufruf aufruf, string grund)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));

            KiErgebnis ergebnis = KiErgebnis.Abgelehnt(grund ?? "");
            Vermerken(DateTime.Now, aufruf.Name, aufruf.Aktion.Stufe, aufruf.AlsJson(),
                      ProjektAus(aufruf), ergebnis, aufruf.Aktion.Formularaktion);
            return ergebnis;
        }

        /// <summary>Protokolliert eine Abweisung der Vorbereitung und verpackt sie.</summary>
        private KiVorbereitung Abweisen(DateTime beginn, KiAufruf aufruf, int projektId,
                                               KiErgebnis ergebnis)
        {
            Vermerken(beginn, aufruf.Name, aufruf.Aktion.Stufe, aufruf.AlsJson(), projektId, ergebnis,
                      aufruf.Aktion.Formularaktion);
            return new KiVorbereitung(null, ergebnis);
        }

        /// <summary>
        /// Schreibt Protokollzeile und Sitzungseintrag - die EINE Stelle, an der ein
        /// Versuch vermerkt wird.
        /// </summary>
        /// <param name="formularaktion">
        /// Ist die vermerkte Aktion eine Formularaktion? Nur sie traegt den Vermerk der
        /// abgeschalteten Feldsicherung - siehe unten.
        /// </param>
        private void Vermerken(DateTime zeitpunkt, string aktion, Schutzstufe stufe,
                                      string parameterJson, int projektId, KiErgebnis ergebnis,
                                      bool formularaktion = false)
        {
            // Der Vermerk der abgeschalteten Feldsicherung (Fachkonzept 11.5) gehoert in
            // JEDE Zeile einer Formularaktion - und nur dorthin: Der Schalter hat auf
            // gewoehnliche Schreibaktionen keine Wirkung, ein Vermerk an ihrer Zeile
            // behauptete also etwas Falsches. Ist die Sicherung an, liefert
            // Protokollvermerk() leeren Text und die Zeile bleibt unveraendert.
            string ergebnistext = ergebnis.Kurzfassung();
            string vermerk = formularaktion ? KiFeldsicherung.Protokollvermerk() : "";
            if (vermerk.Length > 0) ergebnistext = ergebnistext + " [" + vermerk + "]";

            string zeile = KiProtokoll.Zeile(zeitpunkt, aktion, stufe, parameterJson, projektId,
                                             ergebnis.Status, ergebnistext, ergebnis.Dauer);
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
                Ergebnis = ergebnistext,
                DauerMs = (long)Math.Round(ergebnis.Dauer.TotalMilliseconds)
            };

            lock (_sitzungSperre)
            {
                _sitzung.Add(eintrag);
                if (_sitzung.Count > MAX_SITZUNG) _sitzung.RemoveRange(0, _sitzung.Count - MAX_SITZUNG);
            }
        }

        /// <summary>
        /// Haengt eine FERTIGE Protokollzeile an die Datei (Auftrag #200).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der einzige Weg von aussen in diese Datei — und er nimmt nur ZEILEN, keine
        /// Freitexte.</b> Gebaut wird die Zeile im Kern
        /// (<see cref="KiMaskenbruecke.Vermerken"/>) ueber <c>KiProtokoll.Zeile</c>, also
        /// im selben Format wie jeder Ausfuehrungsversuch (Fachkonzept 3.6). Waere hier
        /// ein Freitext moeglich, zerstoerte die erste solche Zeile das Format, das der
        /// Leser erwartet.
        /// </para>
        /// <para>
        /// <b>Warum der Kern die Zeile baut und nicht diese Huelle.</b> iOS hat keine
        /// Huelle, die es koennte — und zwei Erzeugungsstellen fuer dieselbe Zeile waeren
        /// zwei Formate.
        /// </para>
        /// </remarks>
        public void ProtokollzeileAnhaengen(string zeile)
        {
            if (string.IsNullOrEmpty(zeile)) return;
            Schreibe(zeile);
        }

        /// <summary>Pfad der Protokolldatei - neben der Datenbank (Fachkonzept 3.6).</summary>
        public string ProtokollPfad()
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
        private void Schreibe(string zeile)
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

        /// <summary>
        /// Meldet Klarnamen an, die in freien Texten stehen koennen (H8) - der
        /// Schnittstellenteil, der bis Auftrag #201 in der Windows-Huelle lag.
        /// </summary>
        /// <remarks>
        /// Die Namensquellen sind Projekte und Kunden aus der Datenbank
        /// (<see cref="KiHilfe.KlarnamenAnmelden"/>); der Kern erreicht sie seit dem
        /// Umzug des Registers selbst. Ein Adapter in der Huelle waere nur noch eine
        /// Weiterleitung.
        /// </remarks>
        public void KlarnamenAnmelden(KiPlatzhalter platzhalter, params string[] texte)
            => KiHilfe.KlarnamenAnmelden(platzhalter, texte);

        /// <summary>Die Projekt-ID der Protokollzeile, aus den bekannten Parameternamen.</summary>
        private int ProjektAus(KiAufruf aufruf)
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
