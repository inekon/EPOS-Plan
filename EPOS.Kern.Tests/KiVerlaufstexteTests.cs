using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="KiVerlaufstexte"/> und <see cref="KiWerkzeugWerte"/> nach
    /// iU9-W15b.7 — die ~150 Zeilen Anzeigelogik, die bis dahin quer durch
    /// <c>Form_KiChat</c> verstreut lagen und ohne Bildschirm nicht nachrechenbar
    /// waren.
    ///
    /// <para><b>Zwei Zusagen tragen hier Gewicht.</b></para>
    ///
    /// <para><b>H8 — die zwei Listen</b> (Risiko R-W15b-3). In die ANZEIGE geht die
    /// aufgelöste Fassung, in den PROMPT die platzgehaltene. Stünde im Prompt der
    /// Klarname, wäre er ab der zweiten Frage beim Modellanbieter — genau das, was
    /// die Platzhalterung verhindern soll.</para>
    ///
    /// <para><b>Die Kulturregel</b> (Risiko R-W15b-6). „12,5" wird zu „12.5" —
    /// diese eine Zeile ist die Kulturgrenze der Werkzeugliste (Fachkonzept 3.2).
    /// Geht sie verloren, schickt ein deutscher Arbeitsplatz „12,5" an eine Aktion,
    /// die invariant parst.</para>
    ///
    /// <para>Die Klasse pinnt die Sprache im Rumpf mit <c>finally</c> (Regel seit
    /// W8) — sie prüft deutsche Texte.</para>
    /// </summary>
    public class KiVerlaufstexteTests
    {
        /// <summary>Führt einen Fall unter <c>de-DE</c> aus und stellt die Kultur zurück.</summary>
        private static void AufDeutsch(Action fall)
        {
            CultureInfo kultur = CultureInfo.CurrentCulture;
            CultureInfo ui = CultureInfo.CurrentUICulture;
            try
            {
                var de = new CultureInfo("de-DE");
                CultureInfo.CurrentCulture = de;
                CultureInfo.CurrentUICulture = de;
                fall();
            }
            finally
            {
                CultureInfo.CurrentCulture = kultur;
                CultureInfo.CurrentUICulture = ui;
            }
        }

        // ==================================================================
        //  H8 - die zwei Listen
        // ==================================================================

        /// <summary>
        /// <b>Der Kernnachweis H8.</b> Die Antwort erscheint mit KLARNAMEN, der
        /// Prompt-Eintrag bleibt PLATZGEHALTEN.
        /// </summary>
        [Fact]
        public void Anzeige_loest_Klarnamen_auf_der_Prompteintrag_nicht()
        {
            AufDeutsch(() =>
            {
                var tabelle = new KiPlatzhalter();
                string marke = tabelle.Fuer("Muster GmbH");

                var antwort = new KiAntwort
                {
                    Erfolg = true,
                    Text = "Das Projekt " + marke + " ist geöffnet.",
                    Platzhalter = tabelle
                };

                IReadOnlyList<KiVerlaufszeile> anzeige =
                    KiVerlaufstexte.Antwort(antwort, tabelle);

                // In der ANZEIGE steht der Klarname ...
                Assert.Contains(anzeige, z => z.Text.Contains("Muster GmbH", StringComparison.Ordinal));
                Assert.DoesNotContain(anzeige, z => z.Rolle == KiVerlaufsrolle.Assistent
                                                    && z.Text.Contains(marke, StringComparison.Ordinal));

                // ... im PROMPT-Eintrag der Platzhalter.
                string prompt = KiVerlaufstexte.PromptEintragAntwort(antwort.Text);
                Assert.Contains(marke, prompt, StringComparison.Ordinal);
                Assert.DoesNotContain("Muster GmbH", prompt, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// „Benutzer: " und „Assistent: " sind PROMPTFORMAT, kein Anzeigetext
        /// (Befund W15b-B15). <c>PromptBauen</c> schreibt den Verlauf wörtlich in den
        /// Prompt; eine Übersetzung machte ihn sprachabhängig.
        /// </summary>
        [Fact]
        public void Das_Promptformat_ist_sprachunabhaengig()
        {
            Assert.Equal("Benutzer: Wie geht das?",
                         KiVerlaufstexte.PromptEintragFrage("Wie geht das?"));
            Assert.StartsWith("Assistent: ", KiVerlaufstexte.PromptEintragAntwort("So."));
        }

        /// <summary>Der Prompt-Eintrag wird auf 400 Zeichen gekürzt (Bestand <c>:1047</c>).</summary>
        [Fact]
        public void Der_Prompteintrag_wird_auf_vierhundert_Zeichen_gekuerzt()
        {
            string lang = new string('x', 900);
            string prompt = KiVerlaufstexte.PromptEintragAntwort(lang);

            Assert.Equal("Assistent: " + new string('x', 400) + "...", prompt);
        }

        /// <summary>
        /// Ersetzt wird von der HÖCHSTEN Nummer abwärts und nur an Wortgrenzen —
        /// sonst träfe „Name 1" den Anfang von „Name 12".
        /// </summary>
        [Fact]
        public void Klarnamen_werden_von_hinten_und_an_Wortgrenzen_ersetzt()
        {
            var tabelle = new KiPlatzhalter();
            string eins = tabelle.Fuer("Alpha");
            string zwoelf = "";
            for (int i = 2; i <= 12; i++) zwoelf = tabelle.Fuer("Nr" + i);

            string text = eins + " und " + zwoelf;
            string aufgeloest = KiVerlaufstexte.KlarnamenFuerAnzeige(text, tabelle);

            Assert.Equal("Alpha und Nr12", aufgeloest);
        }

        /// <summary>Ohne Tabelle bleibt der Text unangetastet.</summary>
        [Theory]
        [InlineData("")]
        [InlineData("Ein Text ohne Platzhalter.")]
        public void Ohne_Tabelle_bleibt_der_Text_unveraendert(string text)
        {
            Assert.Equal(text, KiVerlaufstexte.KlarnamenFuerAnzeige(text, null));
        }

        // ==================================================================
        //  Die Werkzeugrunde
        // ==================================================================

        /// <summary>
        /// Eine ausgeführte Aktion erscheint als ERFOLG, Ergebniszeilen und
        /// Protokollzeile LEISE — die fünf Farbrollen des Bestands.
        /// </summary>
        [Fact]
        public void Ein_ausgefuehrter_Schritt_erscheint_als_Erfolg()
        {
            AufDeutsch(() =>
            {
                var antwort = new KiAntwort();
                antwort.Schritte.Add(new KiSchritt
                {
                    Aktion = "projekte_auflisten",
                    Kurzfassung = "Projekte auflisten",
                    Ausgefuehrt = true,
                    Ergebnis = KiErgebnis.Ok("2 Projekte", null, 2),
                    Protokollzeile = "2026-09-04 10:00 projekte_auflisten OK"
                });

                IReadOnlyList<KiVerlaufszeile> zeilen = KiVerlaufstexte.Schritte(antwort);

                Assert.Contains(zeilen, z => z.Rolle == KiVerlaufsrolle.Erfolg
                                             && z.Text.Contains("Projekte auflisten", StringComparison.Ordinal));
                Assert.Contains(zeilen, z => z.Rolle == KiVerlaufsrolle.Leise
                                             && z.Text.Contains("2026-09-04", StringComparison.Ordinal));
                Assert.DoesNotContain(zeilen, z => z.Rolle == KiVerlaufsrolle.Fehler);
            });
        }

        /// <summary>Ein NICHT ausgeführter Schritt erscheint als FEHLER, mit Grund.</summary>
        [Fact]
        public void Ein_nicht_ausgefuehrter_Schritt_erscheint_als_Fehler()
        {
            AufDeutsch(() =>
            {
                var antwort = new KiAntwort();
                antwort.Schritte.Add(new KiSchritt
                {
                    Aktion = "projekt_umbenennen",
                    Kurzfassung = "Projekt umbenennen",
                    Ausgefuehrt = false,
                    Grund = "nicht bestätigt"
                });

                IReadOnlyList<KiVerlaufszeile> zeilen = KiVerlaufstexte.Schritte(antwort);

                KiVerlaufszeile fehler = zeilen.Single(z => z.Rolle == KiVerlaufsrolle.Fehler);
                Assert.Contains("Projekt umbenennen", fehler.Text, StringComparison.Ordinal);
                Assert.Contains("nicht bestätigt", fehler.Text, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// <b>Der Sicherungspunkt gehört sichtbar in den Verlauf</b>, nicht nur ins
        /// Protokoll (Fachkonzept 4.4, Punkt 1).
        /// </summary>
        [Fact]
        public void Der_Sicherungspunkt_steht_im_Verlauf()
        {
            AufDeutsch(() =>
            {
                var antwort = new KiAntwort();
                antwort.Schritte.Add(new KiSchritt
                {
                    Aktion = "projekt_umbenennen",
                    Ausgefuehrt = true,
                    Sicherungspunkt = @"C:\Sicherung\Kenndaten_2026-09-04.sqlite"
                });

                Assert.Contains(KiVerlaufstexte.Schritte(antwort),
                                z => z.Text.Contains("Kenndaten_2026-09-04", StringComparison.Ordinal));
            });
        }

        // ==================================================================
        //  Quellen
        // ==================================================================

        /// <summary>
        /// Nur Abschnitte MIT Adresse werden zu Verweisen, und die Adresse geht MIT —
        /// die Oberfläche soll nicht raten müssen, was ein Link ist (Regel G-5).
        /// </summary>
        [Fact]
        public void Nur_Abschnitte_mit_Adresse_werden_Verweise()
        {
            AufDeutsch(() =>
            {
                var abschnitte = new List<WissensAbschnitt>
                {
                    new WissensAbschnitt { Titel = "Ohne Quelle", Inhalt = "x", QuellUrl = "" },
                    new WissensAbschnitt { Titel = "Mit Quelle", Inhalt = "y",
                                           QuellUrl = "https://wiki.epos-plan.de/Projekt" }
                };

                IReadOnlyList<KiVerlaufszeile> zeilen = KiVerlaufstexte.Quellen(abschnitte);

                KiVerlaufszeile verweis = zeilen.Single(z => z.Adresse.Length > 0);
                Assert.Equal("https://wiki.epos-plan.de/Projekt", verweis.Adresse);
                Assert.Contains("Mit Quelle", verweis.Text, StringComparison.Ordinal);
            });
        }

        /// <summary>Ohne Quelle gibt es gar keine Quellenzeilen — auch keine Überschrift.</summary>
        [Fact]
        public void Ohne_Quellen_entstehen_keine_Zeilen()
        {
            Assert.Empty(KiVerlaufstexte.Quellen(null));
            Assert.Empty(KiVerlaufstexte.Quellen(new List<WissensAbschnitt>
            {
                new WissensAbschnitt { Titel = "Ohne", Inhalt = "x", QuellUrl = "" }
            }));
        }

        // ==================================================================
        //  Die Begruessung - vier Faelle
        // ==================================================================

        /// <summary>
        /// Im Hilfe-Betrieb gibt es weder Schlüssel noch Tageskontingent, über die zu
        /// berichten wäre — nur die lokale Suche.
        /// </summary>
        [Fact]
        public void Hilfebetrieb_begruesst_ohne_Kontingent()
        {
            AufDeutsch(() =>
            {
                IReadOnlyList<KiVerlaufszeile> zeilen =
                    KiVerlaufstexte.Begruessung(true, false, 0, 50);

                Assert.DoesNotContain(zeilen, z => z.Text.Contains("50", StringComparison.Ordinal));
                Assert.DoesNotContain(zeilen, z => z.Rolle == KiVerlaufsrolle.Warnung);
            });
        }

        /// <summary>Ohne Schlüssel steht die Begrüßung als WARNUNG da (im Bestand orange).</summary>
        [Fact]
        public void Ohne_Schluessel_begruesst_der_Assistent_warnend()
        {
            AufDeutsch(() =>
            {
                IReadOnlyList<KiVerlaufszeile> zeilen =
                    KiVerlaufstexte.Begruessung(false, false, 0, 50);

                Assert.Contains(zeilen, z => z.Rolle == KiVerlaufsrolle.Warnung);
            });
        }

        /// <summary>Eingerichtet: Datenschutzsatz und Tageszähler stehen leise dabei.</summary>
        [Fact]
        public void Eingerichtet_nennt_Datenschutz_und_Zaehler()
        {
            AufDeutsch(() =>
            {
                IReadOnlyList<KiVerlaufszeile> zeilen =
                    KiVerlaufstexte.Begruessung(false, true, 7, 50);

                Assert.Contains(zeilen, z => z.Rolle == KiVerlaufsrolle.Leise
                                             && z.Text.Contains("7", StringComparison.Ordinal)
                                             && z.Text.Contains("50", StringComparison.Ordinal));
            });
        }

        // ==================================================================
        //  Die Kulturregel (R-W15b-6)
        // ==================================================================

        private static KiAktion Aktion()
        {
            return new KiAktion(
                name: "pruefen",
                zweck: "Prüffall mit jedem Parametertyp.",
                stufe: Schutzstufe.Lesen,
                andockpunkt: "Testfall",
                parameter: new[]
                {
                    new KiParameter("schwelle_kw", KiParameterTyp.Zahl, "Zielschwelle.",
                                    pflicht: false, anzeigename: "Schwelle"),
                    new KiParameter("projekt_id", KiParameterTyp.Ganzzahl, "Projekt.",
                                    pflicht: false, anzeigename: "Projekt"),
                    new KiParameter("bezeichner", KiParameterTyp.Text, "Name.",
                                    pflicht: false, anzeigename: "Bezeichner"),
                    new KiParameter("projekt_ids", KiParameterTyp.GanzzahlListe, "Projekte.",
                                    pflicht: false, anzeigename: "Projekte")
                },
                ausfuehren: _ => KiErgebnis.Ok("ok"));
        }

        /// <summary>
        /// <b>Die Kulturgrenze.</b> „12,5" wird zu „12.5" — und nur bei Zahlen. Ein
        /// TEXT mit Komma bleibt, wie er ist: Ein Bezeichner „Muster, GmbH" darf nicht
        /// zu „Muster. GmbH" werden.
        /// </summary>
        [Fact]
        public void Zahlen_gehen_invariant_hinaus_Text_nicht()
        {
            var roh = new Dictionary<string, string>
            {
                ["schwelle_kw"] = "12,5",
                ["projekt_id"] = "1042",
                ["bezeichner"] = "Muster, GmbH"
            };

            IReadOnlyDictionary<string, object> werte = KiWerkzeugWerte.Sammeln(Aktion(), roh);

            Assert.Equal("12.5", werte["schwelle_kw"]);
            Assert.Equal("1042", werte["projekt_id"]);
            Assert.Equal("Muster, GmbH", werte["bezeichner"]);
        }

        /// <summary>Ein LEERES Feld heißt „nicht angegeben", nicht „leerer Text".</summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Ein_leeres_Feld_ist_nicht_angegeben(string roh)
        {
            var werte = KiWerkzeugWerte.Sammeln(Aktion(),
                new Dictionary<string, string> { ["bezeichner"] = roh });

            Assert.False(werte.ContainsKey("bezeichner"));
        }

        /// <summary>
        /// Eine Ganzzahlliste wird an Komma, Strichpunkt, Leerzeichen und Tabulator
        /// zerlegt — leere Stücke fallen weg.
        /// </summary>
        [Fact]
        public void Eine_Ganzzahlliste_wird_zerlegt()
        {
            var werte = KiWerkzeugWerte.Sammeln(Aktion(),
                new Dictionary<string, string> { ["projekt_ids"] = "1030, 1007; 1017\t1042" });

            Assert.Equal(new[] { "1030", "1007", "1017", "1042" }, (string[])werte["projekt_ids"]);
        }

        /// <summary>Was die Aktion nicht kennt, kommt nicht durch.</summary>
        [Fact]
        public void Unbekannte_Rohwerte_kommen_nicht_durch()
        {
            var werte = KiWerkzeugWerte.Sammeln(Aktion(),
                new Dictionary<string, string> { ["gibt_es_nicht"] = "x" });

            Assert.Empty(werte);
        }
    }
}
