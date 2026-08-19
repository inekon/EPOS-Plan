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
    /// </description></item>
    /// </list>
    /// <para>
    /// TODO(B5): Die sichtbaren Texte dieser Klasse stehen als deutschsprachige Konstanten
    /// in <see cref="KiAusfuehrerTexte"/> und sind mit Paket B5 auf <c>MyResource</c>
    /// umzustellen.
    /// </para>
    /// </remarks>
    public static class KiAusfuehrer
    {
        /// <summary>0 = frei, 1 = eine Aktion laeuft.</summary>
        private static int _laeuft;

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
        /// Steuerelement, ueber das auf den UI-Thread gewechselt wird.
        /// </summary>
        /// <remarks>
        /// Paket B5 setzt hier das Chatfenster. Bleibt der Anker leer, sucht
        /// <see cref="UiAnker"/> das erste offene Formular; gibt es auch das nicht
        /// (Aktionsharnisch), laeuft die Aktion auf dem rufenden Thread.
        /// </remarks>
        public static Control Anker { get; set; }

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
            return AusfuehrenAsync(pruefung.Aufruf, abbruch);
        }

        /// <summary>Fuehrt einen bereits gepruefen Aufruf aus.</summary>
        public static async Task<KiErgebnis> AusfuehrenAsync(KiAufruf aufruf,
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
                if (aktion.Stufe != Schutzstufe.Lesen && ModalerDialogOffen())
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

                // ---- Der eigentliche Lauf, auf dem UI-Thread.
                KiErgebnis ergebnis = await AufUiThread(() => LaufMitEngineModus(aufruf, abbruch))
                                            .ConfigureAwait(true);

                Vermerken(beginn, aktion.Name, aktion.Stufe, aufruf.AlsJson(), projektId, ergebnis);
                return ergebnis;
            }
            finally
            {
                Volatile.Write(ref _laeuft, 0);
            }
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
        private static Task<KiErgebnis> AufUiThread(Func<KiErgebnis> arbeit)
        {
            Control anker = UiAnker();

            if (anker == null || !anker.InvokeRequired)
                return Task.FromResult(arbeit());

            var quelle = new TaskCompletionSource<KiErgebnis>();
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

        /// <summary>Ist gerade ein modaler Dialog offen?</summary>
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
        /// Schreibt Protokollzeile und Sitzungseintrag - die EINE Stelle, an der ein
        /// Versuch vermerkt wird.
        /// </summary>
        private static void Vermerken(DateTime zeitpunkt, string aktion, Schutzstufe stufe,
                                      string parameterJson, int projektId, KiErgebnis ergebnis)
        {
            string zeile = KiProtokoll.Zeile(zeitpunkt, aktion, stufe, parameterJson, projektId,
                                             ergebnis.Status, ergebnis.Kurzfassung(), ergebnis.Dauer);
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
    /// TODO(B5): auf <c>MyResource.Resource</c> umstellen. Bis Paket B5 die
    /// Ressourcendateien wieder freigibt, stehen sie hier als deutschsprachige Konstanten.
    /// </remarks>
    internal static class KiAusfuehrerTexte
    {
        /// <summary>Einlaeufigkeit: es laeuft bereits etwas.</summary>
        internal const string LaeuftBereits =
            "Es läuft gerade eine andere Aktion. Bitte warten Sie, bis sie fertig ist.";

        /// <summary>Modalitaetsprüfung.</summary>
        internal const string ModalerDialog =
            "Es ist ein Dialogfenster geöffnet. Bitte schließen Sie es zuerst.";

        /// <summary>Abbruch durch den Anwender.</summary>
        internal const string Abgebrochen = "Die Aktion wurde abgebrochen.";

        /// <summary>{0} = Ausnahmetyp, {1} = Meldung.</summary>
        internal const string Ausnahme = "Die Aktion ist fehlgeschlagen ({0}): {1}";

        /// <summary>{0} = Aktionsname.</summary>
        internal const string KeinErgebnis = "Die Aktion „{0}“ hat kein Ergebnis geliefert.";
    }
}
