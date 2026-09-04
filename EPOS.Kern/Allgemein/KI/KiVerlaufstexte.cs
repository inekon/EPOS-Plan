// Was im Gespraechsverlauf des Assistenten steht - plattformfrei (iU9-W15b.7).
//
// WARUM ES DIESE DATEI GIBT. Die Anzeigelogik des Chatfensters lag in rund 150
// Zeilen quer durch Form_KiChat verstreut: SchritteZeigen (:1145-1192),
// QuellenZeigen (:1578-1591), der Erfolgs- und der Fehlerzweig von FrageStellen
// (:1022-1072), DokuSucheZeigen (:1095-1135), Begruessung (:916-951) und die
// Rueckuebersetzung der Klarnamen (:1643-1665). Jede dieser Stellen rief
// SchreibeZeile(text, farbe, fett) - also Farbe und Schriftschnitt IM FORMULAR.
//
// Hier steht dieselbe Arbeit einmal und ohne Bildschirm: Eine Antwort des
// Dienstes wird zu einer Liste von Zeilen mit ROLLE. Was eine Rolle FARBLICH
// bedeutet, entscheidet EPOS.UI (Gespraechsrolle und epos-ui.css) - der Kern
// sagt nur, WAS eine Zeile ist.
//
// DAS IST DER EIGENTLICHE UMBAU DER WELLE. Aus 150 nicht pruefbaren Zeilen
// Formularcode wird ein Helfer, den man ohne Oberflaeche nachrechnen kann.
//
// H8 - DIE ZWEI LISTEN. Klarnamen werden NUR fuer die Anzeige aufgeloest. Der
// Antworttext selbst bleibt platzgehalten; er geht ueber den Prompt-Verlauf in
// die naechste Anfrage. Stuende dort der Klarname, waere er ab der zweiten
// Frage beim Modellanbieter - genau das, was die Platzhalterung verhindert.
// Deshalb liefert diese Klasse die ANZEIGE-Zeilen, und der Prompt-Verlauf wird
// getrennt gefuehrt (PromptEintrag).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Rolle einer Verlaufszeile - was sie BEDEUTET, nicht wie sie aussieht.
    /// </summary>
    /// <remarks>
    /// Abgeleitet aus den acht Farben und zwei Schriftschnitten, die
    /// <c>Form_KiChat.SchreibeZeile</c> je bekommen hat. Die Namen sind dieselben
    /// wie in <c>EPOS.UI.Bausteine.Gespraechsrolle</c>; die Oberflaechenbibliothek
    /// bildet eins zu eins ab. Zwei Aufzaehlungen sind es, weil der Kern die
    /// Oberflaechenbibliothek nicht kennt - und nicht kennen soll.
    /// </remarks>
    public enum KiVerlaufsrolle
    {
        /// <summary>Die Frage des Anwenders.</summary>
        Anwender,
        /// <summary>Der Antworttext.</summary>
        Assistent,
        /// <summary>Die Zeile „Assistent:" ueber der Antwort.</summary>
        AssistentKopf,
        /// <summary>Eine Zwischenueberschrift.</summary>
        Ueberschrift,
        /// <summary>Quellen, Zaehler, Cache-Vermerk, Ergebniszeilen, Protokollzeile.</summary>
        Leise,
        /// <summary>Ausgefuehrt, Bestaetigung erteilt, gespeichert.</summary>
        Erfolg,
        /// <summary>Hinweise, Verfall, „kein Schluessel hinterlegt".</summary>
        Warnung,
        /// <summary>Fehler, abgelehnt, nicht ausgefuehrt.</summary>
        Fehler,
        /// <summary>Der Titel des Bestaetigungsblocks.</summary>
        Bestaetigung,
        /// <summary>Ein Absatztrenner.</summary>
        Leerzeile
    }

    /// <summary>Eine Zeile des Gespraechsverlaufs, wie der Kern sie sieht.</summary>
    /// <param name="Rolle">Was die Zeile bedeutet.</param>
    /// <param name="Text">Der Inhalt.</param>
    /// <param name="Adresse">Eine Adresse, falls die Zeile ein Verweis ist; sonst leer.</param>
    public sealed class KiVerlaufszeile
    {
        /// <summary>Legt eine Zeile an.</summary>
        public KiVerlaufszeile(KiVerlaufsrolle rolle, string text, string adresse = null)
        {
            Rolle = rolle;
            Text = text ?? "";
            Adresse = adresse ?? "";
        }

        /// <summary>Was die Zeile bedeutet.</summary>
        public KiVerlaufsrolle Rolle { get; }

        /// <summary>Der Inhalt.</summary>
        public string Text { get; }

        /// <summary>Die Adresse; leer = kein Verweis.</summary>
        public string Adresse { get; }
    }

    /// <summary>
    /// Uebersetzt die Antworten des Assistenten in Verlaufszeilen.
    /// </summary>
    public static class KiVerlaufstexte
    {
        /// <summary>Der Absatztrenner - im Bestand eine leere Zeile.</summary>
        private static readonly KiVerlaufszeile LEER =
            new KiVerlaufszeile(KiVerlaufsrolle.Leerzeile, "");

        // ==================================================================
        //  Die Frage des Anwenders
        // ==================================================================

        /// <summary>
        /// Die Zeile „Sie: …" (<c>Form_KiChat:995</c>).
        /// </summary>
        public static KiVerlaufszeile Frage(string frage)
        {
            return new KiVerlaufszeile(KiVerlaufsrolle.Anwender,
                                       MyResource.Resource.KI_CHAT_ROLLE_ANWENDER + (frage ?? ""));
        }

        /// <summary>
        /// Der Eintrag, der in den PROMPT-Verlauf geht - nicht in die Anzeige.
        /// </summary>
        /// <remarks>
        /// <b>„Benutzer: " ist KEIN Anzeigetext</b> (Befund W15b-B15). Es ist das
        /// Format des Verlaufsblocks im Prompt: <c>PromptBauen</c> schreibt die
        /// letzten vier Eintraege woertlich unter „Bisheriger Verlauf (Auszug):".
        /// Eine Uebersetzung machte den Prompt sprachabhaengig - deshalb steht der
        /// Text hier fest und nicht in <c>MyResource</c>.
        /// </remarks>
        public static string PromptEintragFrage(string frage) => "Benutzer: " + (frage ?? "");

        /// <summary>
        /// Der Antworteintrag fuer den PROMPT-Verlauf - <b>platzgehalten</b> und auf
        /// 400 Zeichen gekuerzt (<c>Form_KiChat:1047</c>). Siehe H8 im Dateikopf.
        /// </summary>
        public static string PromptEintragAntwort(string antworttext)
            => "Assistent: " + Kuerzen(antworttext, 400);

        // ==================================================================
        //  Die Antwort mit Modell (FrageStellen, :1022-1072)
        // ==================================================================

        /// <summary>
        /// Die Zeilen zu einer Antwort des Dienstes - Erfolgs- und Fehlerzweig.
        /// </summary>
        /// <param name="antwort">Die Antwort aus <c>KiChatService</c>.</param>
        /// <param name="platzhalter">
        /// Die Bezeichnertabelle der Sitzung; sie loest die Klarnamen NUR FUER DIE
        /// ANZEIGE auf (H8).
        /// </param>
        /// <param name="ersatzabschnitte">
        /// Was im Fehlerfall statt einer Antwort gezeigt wird. Kam der Dienst gar
        /// nicht erst zum Zug (Riegel, Tageslimit), hat er auch nichts beschafft -
        /// dann sucht der Aufrufer lokal und reicht das Ergebnis hier herein.
        /// </param>
        public static IReadOnlyList<KiVerlaufszeile> Antwort(
            KiAntwort antwort, KiPlatzhalter platzhalter,
            IReadOnlyList<WissensAbschnitt> ersatzabschnitte = null)
        {
            var zeilen = new List<KiVerlaufszeile>();
            if (antwort == null) return zeilen;

            if (antwort.Schritte.Count > 0 || antwort.Hinweise.Count > 0)
                zeilen.AddRange(Schritte(antwort));

            if (antwort.Erfolg)
            {
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.AssistentKopf,
                                               MyResource.Resource.KI_CHAT_ROLLE_ASSISTENT));

                // Hier - und NUR hier - werden aus "Name 3" wieder Klarnamen (H8).
                zeilen.Add(new KiVerlaufszeile(
                    KiVerlaufsrolle.Assistent,
                    KlarnamenFuerAnzeige(antwort.Text, antwort.Platzhalter ?? platzhalter)));

                if (antwort.Quellen.Count > 0)
                    zeilen.Add(new KiVerlaufszeile(
                        KiVerlaufsrolle.Leise,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_CHAT_QUELLEN,
                                      string.Join(", ", antwort.Quellen))));

                zeilen.AddRange(Quellen(antwort.Abschnitte));

                if (antwort.AusCache)
                    zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise,
                                                   MyResource.Resource.KI_CHAT_AUS_CACHE));
            }
            else
            {
                zeilen.Add(new KiVerlaufszeile(
                    KiVerlaufsrolle.Fehler,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_CHAT_FEHLER, antwort.Fehler ?? "")));

                IReadOnlyList<WissensAbschnitt> treffer =
                    antwort.Abschnitte != null && antwort.Abschnitte.Count > 0
                        ? antwort.Abschnitte
                        : ersatzabschnitte;

                if (treffer != null && treffer.Count > 0)
                {
                    zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Ueberschrift,
                                                   MyResource.Resource.KI_CHAT_ABSCHNITTE_ERSATZ));
                    foreach (WissensAbschnitt a in treffer)
                        zeilen.Add(new KiVerlaufszeile(
                            KiVerlaufsrolle.Leise,
                            "• " + a.Titel + ": " + Kuerzen(a.Inhalt, 200)));

                    zeilen.AddRange(Quellen(treffer));
                }
            }

            zeilen.Add(LEER);
            return zeilen;
        }

        // ==================================================================
        //  Die Suche ohne Modell (DokuSucheZeigen, :1095-1135)
        // ==================================================================

        /// <summary>
        /// Die Trefferliste der Online-Doku-Suche - <b>ohne jeden Modellaufruf</b>
        /// (Entscheid 7.4).
        /// </summary>
        public static IReadOnlyList<KiVerlaufszeile> Suchtreffer(
            IReadOnlyList<WissensAbschnitt> treffer)
        {
            var zeilen = new List<KiVerlaufszeile>();

            if (treffer == null || treffer.Count == 0)
            {
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise,
                                               MyResource.Resource.KI_CHAT_KEINE_TREFFER));
            }
            else
            {
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Ueberschrift,
                                               MyResource.Resource.KI_CHAT_ABSCHNITTE));
                foreach (WissensAbschnitt a in treffer)
                {
                    zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Assistent,
                                                   "• " + a.Titel + " (" + a.Bereich + ")"));
                    zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise,
                                                   "   " + Kuerzen(a.Inhalt, 220)));
                }

                zeilen.AddRange(Quellen(treffer));
            }

            zeilen.Add(LEER);
            return zeilen;
        }

        // ==================================================================
        //  Die Werkzeugrunde (SchritteZeigen, :1145-1192)
        // ==================================================================

        /// <summary>
        /// Was in den Runden geschehen ist: Hinweise zum gewaehlten Weg, jede Aktion
        /// mit Angaben und die zugehoerige Protokollzeile.
        /// </summary>
        public static IReadOnlyList<KiVerlaufszeile> Schritte(KiAntwort antwort)
        {
            var zeilen = new List<KiVerlaufszeile>();
            if (antwort == null) return zeilen;

            foreach (string hinweis in antwort.Hinweise)
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Warnung, hinweis));

            foreach (KiSchritt schritt in antwort.Schritte)
            {
                string bezeichnung = schritt.Kurzfassung.Length > 0
                    ? schritt.Kurzfassung
                    : schritt.Aktion;

                if (schritt.Ausgefuehrt)
                {
                    zeilen.Add(new KiVerlaufszeile(
                        KiVerlaufsrolle.Erfolg,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_AKT_AUSGEFUEHRT, bezeichnung)));

                    if (schritt.Ergebnis != null && schritt.Ergebnis.Zeilen.Count > 0)
                        zeilen.Add(new KiVerlaufszeile(
                            KiVerlaufsrolle.Leise,
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKT_ERGEBNISZEILEN,
                                          schritt.Ergebnis.Zeilen.Count)));
                }
                else
                {
                    zeilen.Add(new KiVerlaufszeile(
                        KiVerlaufsrolle.Fehler,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_AKT_NICHT_AUSGEFUEHRT,
                                      bezeichnung, schritt.Grund)));
                }

                if (schritt.Ergebnis != null)
                    foreach (string meldung in schritt.Ergebnis.Meldungen)
                        zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise, "   " + meldung));

                // Der Sicherungspunkt gehoert sichtbar in den Verlauf, nicht nur ins
                // Protokoll (Fachkonzept 4.4, Punkt 1).
                if (schritt.Sicherungspunkt.Length > 0)
                    zeilen.Add(new KiVerlaufszeile(
                        KiVerlaufsrolle.Leise,
                        "   " + KiTexte.FeldSicherung + ": " + schritt.Sicherungspunkt));

                if (schritt.Protokollzeile.Length > 0)
                    zeilen.Add(new KiVerlaufszeile(
                        KiVerlaufsrolle.Leise,
                        string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.KI_AKT_PROTOKOLLZEILE,
                                      schritt.Protokollzeile)));
            }

            if (antwort.Runden > 0)
            {
                string weg = antwort.WegB
                    ? MyResource.Resource.KI_AKT_WEG_B
                    : MyResource.Resource.KI_AKT_WEG_A;

                zeilen.Add(new KiVerlaufszeile(
                    KiVerlaufsrolle.Leise,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_AKT_RUNDEN, antwort.Runden, weg)));
            }

            return zeilen;
        }

        // ==================================================================
        //  Quellen (QuellenZeigen, :1578-1591)
        // ==================================================================

        /// <summary>
        /// Je Abschnitt mit Adresse eine Zeile „• Titel — URL". Die Adresse wird
        /// MITGEGEBEN, damit die Oberflaeche nicht raten muss, was ein Verweis ist
        /// (Regel G-5 des Bausteins).
        /// </summary>
        public static IReadOnlyList<KiVerlaufszeile> Quellen(
            IReadOnlyList<WissensAbschnitt> abschnitte)
        {
            var zeilen = new List<KiVerlaufszeile>();
            if (abschnitte == null) return zeilen;

            var mitQuelle = new List<WissensAbschnitt>();
            foreach (WissensAbschnitt a in abschnitte)
                if (a != null && !string.IsNullOrWhiteSpace(a.QuellUrl)) mitQuelle.Add(a);

            if (mitQuelle.Count == 0) return zeilen;

            zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Ueberschrift,
                                           MyResource.Resource.KI_WIKI_QUELLEN));

            foreach (WissensAbschnitt a in mitQuelle)
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise,
                                               "• " + a.Titel + " — " + a.QuellUrl,
                                               a.QuellUrl));

            return zeilen;
        }

        // ==================================================================
        //  Die Begruessung (Begruessung, :916-951)
        // ==================================================================

        /// <summary>
        /// Die Begruessung in ihren vier Faellen: Hilfe-Betrieb, eingerichtet, ohne
        /// Schluessel - und in jedem Fall mit dem Kopf „Hilfe-Assistent".
        /// </summary>
        /// <param name="hilfeBetrieb">Ist die KI abgeschaltet?</param>
        /// <param name="eingerichtet">Liegt ein API-Schluessel vor?</param>
        /// <param name="anfragenHeute">Verbrauchte Anfragen des Tages.</param>
        /// <param name="tageslimit">Das feste Tageslimit.</param>
        public static IReadOnlyList<KiVerlaufszeile> Begruessung(
            bool hilfeBetrieb, bool eingerichtet, int anfragenHeute, int tageslimit)
        {
            var zeilen = new List<KiVerlaufszeile>
            {
                new KiVerlaufszeile(KiVerlaufsrolle.AssistentKopf,
                                    MyResource.Resource.KI_CHAT_TITEL)
            };

            if (hilfeBetrieb)
            {
                // Im Hilfe-Betrieb gibt es weder Schluessel noch Tageskontingent, ueber
                // die zu berichten waere - nur die lokale Suche. Der Satz sagt zugleich,
                // dass dabei nichts diesen Rechner verlaesst.
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Assistent,
                                               MyResource.Resource.KI_HILFEBETRIEB_BEGRUESSUNG));
                zeilen.Add(LEER);
                return zeilen;
            }

            if (eingerichtet)
            {
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Assistent,
                                               MyResource.Resource.KI_CHAT_BEGRUESSUNG));
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise,
                                               MyResource.Resource.KI_CHAT_BEGRUESSUNG_DATEN));
                zeilen.Add(new KiVerlaufszeile(
                    KiVerlaufsrolle.Leise,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.KI_CHAT_VERBRAUCH_LANG,
                                  anfragenHeute, tageslimit)));
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Leise,
                                               MyResource.Resource.KI_AKT_STUFE1_HINWEIS));
            }
            else
            {
                zeilen.Add(new KiVerlaufszeile(KiVerlaufsrolle.Warnung,
                                               MyResource.Resource.KI_CHAT_OHNE_SCHLUESSEL));
            }

            zeilen.Add(LEER);
            return zeilen;
        }

        // ==================================================================
        //  Helfer
        // ==================================================================

        /// <summary>Kuerzt und haengt „..." an (<c>Form_KiChat.Kuerzen</c>).</summary>
        public static string Kuerzen(string text, int laenge)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= laenge) return text ?? "";
            return text.Substring(0, laenge) + "...";
        }

        /// <summary>
        /// Der Rueckweg fuer die BILDSCHIRMAUSGABE (H8): aus „Name 3" wird wieder der
        /// Klarname. Unbekannte Platzhalter und jeder andere Text bleiben unangetastet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nur die Anzeige.</b> Der Antworttext selbst bleibt platzgehalten - er
        /// geht ueber den Prompt-Verlauf in die naechste Anfrage. Sendevorschau,
        /// Protokollzeilen und „Was wird gesendet?" zeigen weiterhin den Platzhalter:
        /// Sie dokumentieren, was tatsaechlich uebertragen wurde, und duerfen deshalb
        /// nicht geschoent werden.
        /// </para>
        /// <para>
        /// <b>Zwei Regeln.</b> Ersetzt wird von der hoechsten Nummer abwaerts, sonst
        /// traefe „Name 1" den Anfang von „Name 12". Und nur ganze Vorkommen: Die
        /// Wortgrenze hinter der Ziffer verhindert, dass ein dem Programm unbekanntes
        /// „Name 15" als „Name 1" mit angehaengter Fuenf missdeutet wird.
        /// </para>
        /// </remarks>
        public static string KlarnamenFuerAnzeige(string text, KiPlatzhalter tabelle)
        {
            if (string.IsNullOrEmpty(text) || tabelle == null || tabelle.Anzahl == 0)
                return text ?? "";

            string ergebnis = text;
            for (int i = tabelle.Anzahl; i >= 1; i--)
            {
                string marke = KiPlatzhalter.Stamm + " " +
                               i.ToString(CultureInfo.InvariantCulture);
                string klarname = tabelle.Klarname(marke);
                if (string.IsNullOrEmpty(klarname)) continue;

                // MatchEvaluator statt Ersatzzeichenkette: In einem Klarnamen darf ein
                // "$" stehen, ohne als Rueckverweis gelesen zu werden.
                string treffer = klarname;
                ergebnis = Regex.Replace(ergebnis,
                                         @"(?<!\w)" + Regex.Escape(marke) + @"\b",
                                         m => treffer);
            }
            return ergebnis;
        }
    }
}
