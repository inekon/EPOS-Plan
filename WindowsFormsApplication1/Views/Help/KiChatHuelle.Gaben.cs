using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Hilfe;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <see cref="KiChatHuelle"/> — der PARAMETERSATZ der Komponente und die vier
    /// Wege nach draußen.
    ///
    /// <para>Eigene Teildatei, weil das ein eigener Gegenstand ist: Hier — und nur
    /// hier — treffen die Razor-Komponente und der Dienst aufeinander. Die
    /// Komponente kennt weder <c>KiChatService</c> noch <c>KiAusfuehrer</c> noch das
    /// Netz; sie bekommt Delegaten (§ 15.3 der Vermessung).</para>
    ///
    /// <para><b>Die zwei Listen (H8, Risiko W15b-R3).</b> Was hier zurückgegeben
    /// wird, sind ANZEIGE-Zeilen: Klarnamen aufgelöst. In <c>_verlauf</c> — die
    /// zweite Liste, die in den nächsten Prompt geht — landet die PLATZGEHALTENE
    /// Fassung. Stünde dort der Klarname, wäre er ab der zweiten Frage beim
    /// Modellanbieter.</para>
    /// </summary>
    internal sealed partial class KiChatHuelle
    {
        /// <summary>Zeilen des Aktionsprotokolls, die höchstens gezeigt werden.</summary>
        private const int PROTOKOLL_ZEILEN = 400;

        /// <summary>Der PARAMETERSATZ der Komponente.</summary>
        private IDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                // ---- Zustand beim Oeffnen -------------------------------------
                ["Kontext"] = HilfeKontext.Beschreibung(),
                ["HilfeBetrieb"] = _hilfeBetrieb,
                ["Eingerichtet"] = KiChatService.IstEingerichtet,
                ["AnfragenHeute"] = KiChatService.AnfragenHeute,
                ["Tageslimit"] = KiChatService.Tageslimit,
                ["FeldsicherungHinweis"] = KiFeldsicherung.Chathinweis() ?? "",
                ["AktionenVorbelegt"] = KiChatService.AktionenZulassen && KiEinwilligung.Erteilt,
                ["Begruessung"] = Umsetzen(KiVerlaufstexte.Begruessung(
                    _hilfeBetrieb, KiChatService.IstEingerichtet,
                    KiChatService.AnfragenHeute, KiChatService.Tageslimit)),

                // ---- Die vier Wege nach draussen ------------------------------
                ["Fragen"] = (Func<string, bool, Task<IReadOnlyList<Gespraechszeile>>>)FragenAsync,
                ["Suchen"] = (Func<string, Task<IReadOnlyList<Gespraechszeile>>>)SuchenAsync,
                ["Einwilligen"] = (Func<Task<bool>>)KiEinwilligung.SicherstellenAsync,
                ["Ausfuehren"] = (Func<string, IReadOnlyDictionary<string, object>,
                                       Task<IReadOnlyList<Gespraechszeile>>>)AusfuehrenAsync,

                // ---- Die vier Nebenwege ---------------------------------------
                ["Vorschau"] = (Func<Task<string>>)VorschauAsync,
                ["Protokoll"] = (Func<Task<string>>)ProtokollAsync,
                ["Einstellungen"] = (Func<Task<bool>>)EinstellungenAsync,
                ["Rechtshinweis"] = (Func<Task>)RechtshinweisAsync,

                // ---- Zustandsanzeige ------------------------------------------
                ["Belegt"] = (Func<bool>)(() => KiAusfuehrer.Belegt),
                ["SemantikZeile"] = (Func<string>)Semantikzeile,
                ["Aktionen"] = KiAusfuehrungsweg.Aktuell.Register.Alle,
                ["Beschreiben"] = (Func<KiAktion, string>)KiBestaetigung.Beschreibe,

                // ---- Rueckwege -------------------------------------------------
                ["AdresseGewaehlt"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create<string>(new object(), AdresseOeffnen),
                ["Kopieren"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create<string>(new object(), InZwischenablage),
                ["Geschlossen"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create(new object(), Schliessen),
                ["UeberlagerungGeaendert"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create<bool>(new object(), UeberlagerungGemeldet),
                ["Anmelden"] = (Action<KiChatSteuerung>)(s => _steuerung = s),

                ["Texte"] = Texte()
            };
        }

        // ==================================================================
        //  Die vier Wege nach draussen
        // ==================================================================

        /// <summary>
        /// Frage MIT Modell. Der Riegel steht im Kern, vor allem anderen (Regel S-4) —
        /// diese Hülle nimmt ihn nicht vorweg.
        /// </summary>
        private async Task<IReadOnlyList<Gespraechszeile>> FragenAsync(string frage, bool mitAktionen)
        {
            // Die PLATZGEHALTENE Fassung in die zweite Liste (H8) - sie geht bei der
            // naechsten Frage wieder in den Prompt.
            _verlauf.Add(KiVerlaufstexte.PromptEintragFrage(frage));

            KiAntwort antwort = mitAktionen
                ? await KiChatService.FrageMitAktionenAsync(frage, HilfeKontext.Beschreibung(),
                                                            _verlauf, _platzhalter)
                                     .ConfigureAwait(true)
                : await KiChatService.FrageAsync(frage, HilfeKontext.Beschreibung(), _verlauf)
                                     .ConfigureAwait(true);

            if (antwort.Erfolg)
                _verlauf.Add(KiVerlaufstexte.PromptEintragAntwort(antwort.Text));

            // Kam der Dienst gar nicht erst zum Zug (Riegel, Tageslimit), hat er auch
            // nichts beschafft - dann wird lokal gesucht, ohne Netz.
            IReadOnlyList<WissensAbschnitt> ersatz =
                antwort.Erfolg || (antwort.Abschnitte != null && antwort.Abschnitte.Count > 0)
                    ? null
                    : HilfeWissen.Suchen(frage, HilfeKontext.Beschreibung(), 4);

            return Umsetzen(KiVerlaufstexte.Antwort(antwort, _platzhalter, ersatz));
        }

        /// <summary>
        /// Suche OHNE Modell (Entscheid 7.4). Beschafft wird ausschließlich über
        /// <c>AbschnitteBeschaffenAsync</c> — das ist die Wiki-Kette plus das lokale
        /// Einbauwissen; der Modellanbieter kommt darin nicht vor.
        /// </summary>
        private async Task<IReadOnlyList<Gespraechszeile>> SuchenAsync(string frage)
        {
            List<WissensAbschnitt> treffer = await KiChatService
                .AbschnitteBeschaffenAsync(frage, HilfeKontext.Beschreibung(), CancellationToken.None)
                .ConfigureAwait(true);

            return Umsetzen(KiVerlaufstexte.Suchtreffer(treffer));
        }

        /// <summary>Eine von Hand gewählte Aktion ausführen.</summary>
        private async Task<IReadOnlyList<Gespraechszeile>> AusfuehrenAsync(
            string name, IReadOnlyDictionary<string, object> werte)
        {
            KiAktion aktion = KiAusfuehrungsweg.Aktuell.Register.Finde(name);
            if (aktion == null) return Array.Empty<Gespraechszeile>();

            KiPruefErgebnis geprueft = KiPruefung.Pruefe(aktion, werte);
            if (!geprueft.Gueltig)
                return Umsetzen(new[]
                {
                    new KiVerlaufszeile(KiVerlaufsrolle.Fehler, geprueft.FehlerText())
                });

            KiErgebnis ergebnis = await KiAusfuehrungsweg.Aktuell
                .AusfuehrenAsync(geprueft.Aufruf, CancellationToken.None).ConfigureAwait(true);

            var schritt = new KiSchritt
            {
                Aktion = name,
                Kurzfassung = name,
                Ausgefuehrt = ergebnis.Erfolg,
                Grund = ergebnis.Erfolg ? "" : ergebnis.Text,
                Ergebnis = ergebnis,
                Protokollzeile = KiAusfuehrungsweg.Aktuell.LetzteProtokollzeile
            };

            var anzeige = new KiAntwort();
            anzeige.Schritte.Add(schritt);

            var zeilen = new List<KiVerlaufszeile>(KiVerlaufstexte.Schritte(anzeige));
            if (ergebnis.Erfolg && ergebnis.Text.Length > 0)
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Assistent, ergebnis.Text));
            zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leerzeile, ""));

            return Umsetzen(zeilen);
        }

        // ==================================================================
        //  Die vier Nebenwege
        // ==================================================================

        /// <summary>
        /// „Was wird gesendet?" — der vollständige Anfragerumpf. Er zeigt die
        /// PLATZGEHALTENE Fassung: Er dokumentiert, was tatsächlich übertragen wird,
        /// und darf deshalb nicht geschönt werden.
        /// </summary>
        private Task<string> VorschauAsync()
            => KiChatService.SendeVorschau("", HilfeKontext.Beschreibung(), _verlauf,
                                           KiChatService.AktionenZulassen);

        /// <summary>
        /// Das Aktionsprotokoll. Es liegt neben der Datenbank, damit Protokoll und
        /// Datenstand zusammen gesichert werden (Fachkonzept 3.6).
        /// </summary>
        private Task<string> ProtokollAsync()
        {
            string pfad = KiAusfuehrer.ProtokollPfad();

            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad))
                return Task.FromResult(
                    string.Format(MyResource.Resource.KI_AKT_PROTOKOLL_FEHLT, pfad ?? "?"));

            try
            {
                string[] alle = File.ReadAllLines(pfad);
                int ab = Math.Max(0, alle.Length - PROTOKOLL_ZEILEN);
                return Task.FromResult(pfad + Environment.NewLine + Environment.NewLine +
                                       string.Join(Environment.NewLine, alle, ab, alle.Length - ab));
            }
            catch (Exception ex)
            {
                return Task.FromResult(pfad + Environment.NewLine + Environment.NewLine + ex.Message);
            }
        }

        private Task<bool> EinstellungenAsync()
            => Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster));

        private Task RechtshinweisAsync()
        {
            KiHinweisHuelle.Anzeigen(_fenster);
            return Task.CompletedTask;
        }

        // ==================================================================
        //  Rueckwege
        // ==================================================================

        /// <summary>
        /// Öffnet eine Adresse — <b>nur http und https</b> (Bestand <c>:1554-1555</c>).
        /// In derselben Anzeige landet Antworttext des Modells, und der ist Fremdtext.
        /// </summary>
        private void AdresseOeffnen(string adresse)
        {
            if (string.IsNullOrWhiteSpace(adresse)) return;
            if (!adresse.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !adresse.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;

            try { Dienste.Datei.MitSystemOeffnen(DokuUebersetzung.FuerAnzeige(adresse)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[KI] Adresse: " + ex.Message); }
        }

        /// <summary>
        /// Schreibt den Verlauf in die Zwischenablage (Entscheid E-11, Neuerung). Die
        /// Komponente liefert den Text — <c>EPOS.UI</c> kennt keine Plattform.
        /// </summary>
        private void InZwischenablage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { System.Windows.Forms.Clipboard.SetText(text); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[KI] Kopieren: " + ex.Message); }
        }

        private void Schliessen()
        {
            if (_fenster != null && !_fenster.IsDisposed) _fenster.Close();
        }

        /// <summary>
        /// Der Überlagerungszustand der Komponente geht an denselben Haken wie die
        /// Modalitätsprüfung (W15b.0d, Entscheid E-8): Solange eine Überlagerung offen
        /// steht, weist der Ausführer Aktionen ab.
        /// </summary>
        private void UeberlagerungGemeldet(bool offen)
        {
            KiAusfuehrer.Ueberlagerung = offen ? (Func<bool>)(() => true) : null;
        }

        /// <summary>
        /// Der Zustand der semantischen Suche (H10) — dezent, in derselben Zeile wie
        /// jeder andere Status.
        /// </summary>
        /// <remarks>
        /// Gezeigt werden nur ZWEI der vier Zustände: „wird vorbereitet" während des
        /// einmaligen Bezugs und „aktiv", sobald gerechnet werden kann. Ein
        /// FEHLGESCHLAGENER Bezug bleibt stumm — er ist kein Ereignis, über das der
        /// Anwender etwas erfahren müsste: Die Hilfe sucht dann genau wie vorher, nur
        /// ohne die zweite Stufe (Bestand <c>:901-919</c>).
        /// </remarks>
        private static string Semantikzeile()
        {
            try
            {
                SemantikModell.Lage lage = SemantikModell.Zustand;
                if (lage == SemantikModell.Lage.Laedt)
                    return MyResource.Resource.KI_SEMANTIK_VORBEREITUNG;
                if (lage == SemantikModell.Lage.Bereit)
                    return MyResource.Resource.KI_SEMANTIK_AKTIV;
                return "";
            }
            catch (Exception) { return ""; }
        }

        // ==================================================================
        //  Umsetzung Kern -> Oberflaeche
        // ==================================================================

        /// <summary>
        /// Bildet die Kern-Rollen auf die Rollen des Bausteins ab — eins zu eins.
        /// </summary>
        /// <remarks>
        /// Zwei Aufzählungen mit denselben zehn Namen: Der Kern sagt, WAS eine Zeile
        /// ist; <c>EPOS.UI</c> entscheidet, wie sie aussieht. Der Kern kennt die
        /// Oberflächenbibliothek nicht und soll sie nicht kennen.
        /// </remarks>
        private static IReadOnlyList<Gespraechszeile> Umsetzen(IReadOnlyList<KiVerlaufszeile> zeilen)
        {
            var liste = new List<Gespraechszeile>();
            if (zeilen == null) return liste;

            foreach (KiVerlaufszeile z in zeilen)
                liste.Add(new Gespraechszeile(Rolle(z.Rolle), z.Text,
                                              z.Adresse.Length == 0 ? null : z.Adresse));
            return liste;
        }

        private static Gespraechsrolle Rolle(KiVerlaufsrolle rolle)
        {
            switch (rolle)
            {
                case KiVerlaufsrolle.Anwender: return Gespraechsrolle.Anwender;
                case KiVerlaufsrolle.Assistent: return Gespraechsrolle.Assistent;
                case KiVerlaufsrolle.AssistentKopf: return Gespraechsrolle.AssistentKopf;
                case KiVerlaufsrolle.Ueberschrift: return Gespraechsrolle.Ueberschrift;
                case KiVerlaufsrolle.Leise: return Gespraechsrolle.Leise;
                case KiVerlaufsrolle.Erfolg: return Gespraechsrolle.Erfolg;
                case KiVerlaufsrolle.Warnung: return Gespraechsrolle.Warnung;
                case KiVerlaufsrolle.Fehler: return Gespraechsrolle.Fehler;
                case KiVerlaufsrolle.Bestaetigung: return Gespraechsrolle.Bestaetigung;
                default: return Gespraechsrolle.Leerzeile;
            }
        }

        // ==================================================================
        //  Texte
        // ==================================================================

        /// <summary>Alle Anzeigetexte, einmal aus <c>MyResource</c> gebaut.</summary>
        private KiChatTexte Texte()
        {
            return new KiChatTexte
            {
                Verlauf = MyResource.Resource.KI_CHAT_TITEL,
                KontextFormat = MyResource.Resource.KI_CHAT_KONTEXT,
                KontextLeer = MyResource.Resource.KI_CHAT_KONTEXT_LEER,
                Denkt = MyResource.Resource.KI_CHAT_DENKT,
                VerbrauchFormat = MyResource.Resource.KI_CHAT_VERBRAUCH,
                Eingabe = MyResource.Resource.KI_CHAT_TITEL,
                Fragen = MyResource.Resource.KI_CHAT_BTN_FRAGEN,
                Suchen = _hilfeBetrieb
                    ? MyResource.Resource.KI_HILFEBETRIEB_SUCHEN_BTN
                    : MyResource.Resource.KI_CHAT_BTN_SUCHEN,
                Aktionen = MyResource.Resource.KI_AKT_SCHALTER,
                AktionenEin = MyResource.Resource.KI_AKT_DATENSCHUTZ_EIN,
                AktionenAus = MyResource.Resource.KI_AKT_DATENSCHUTZ_AUS,
                EinwilligungFehlt = KiEinwilligung.Abgeschaltet
                    ? MyResource.Resource.KI_ABSCHALTER_MELDUNG
                    : MyResource.Resource.KI_HINWEIS_ABGELEHNT,
                Werkzeuge = MyResource.Resource.KI_AKT_WERKZEUGE_BTN,
                WerkzeugeTitel = MyResource.Resource.KI_AKT_WERKZEUGE_TITEL,
                Beschreibung = MyResource.Resource.KI_AKT_WERKZEUGE_TITEL,
                Ausfuehren = MyResource.Resource.KI_AKT_BESTAETIGUNG_AUSFUEHREN,
                AktionWaehlen = MyResource.Resource.KI_AKT_AKTION_WAEHLEN,
                HinweisVorn = _hilfeBetrieb
                    ? MyResource.Resource.KI_WIKI_HINWEIS_ZEILE
                    : MyResource.Resource.KI_HINWEIS_ZEILE,
                HinweisLink = _hilfeBetrieb ? "" : MyResource.Resource.KI_HINWEIS_ZEILE_LINK,
                RechtshinweisTitel = MyResource.Resource.KI_HINWEIS_FENSTER,
                Doku = MyResource.Resource.HILFE_POPUP_LINK,
                DokuAdresse = WikiWissen.Basis(),
                Vorschau = MyResource.Resource.KI_VORSCHAU_LINK,
                VorschauTitel = MyResource.Resource.KI_VORSCHAU_TITEL,
                VorschauKopf = string.Format(MyResource.Resource.KI_VORSCHAU_HINWEIS,
                                             KiChatService.MODELL, KiChatService.Endpunkt()),
                Protokoll = MyResource.Resource.KI_AKT_PROTOKOLL_LINK,
                ProtokollTitel = MyResource.Resource.KI_AKT_PROTOKOLL_TITEL,
                Einstellungen = MyResource.Resource.KI_CHAT_BTN_EINSTELLUNGEN,
                Gespeichert = KiChatService.IstEingerichtet
                    ? MyResource.Resource.KI_EINST_MSG_GESPEICHERT
                    : MyResource.Resource.KI_EINST_MSG_GESPEICHERT_OHNE_SCHLUESSEL,
                Schliessen = MyResource.Resource.KI_VORSCHAU_SCHLIESSEN,
                Kopieren = MyResource.Resource.KI_CHAT_KOPIEREN,
                BestaetigungTitel = MyResource.Resource.KI_AKT_BESTAETIGUNG_TITEL,
                BestaetigungAusfuehren = MyResource.Resource.KI_AKT_BESTAETIGUNG_AUSFUEHREN,
                BestaetigungAbbrechen = MyResource.Resource.KI_AKT_BESTAETIGUNG_ABBRECHEN
            };
        }
    }
}
