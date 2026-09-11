using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Weg aus einem Dialog in den Hilfe-Assistenten (Auftrag #199, Stufe S1 des
    /// Konzepts „Der Hilfe-Assistent im Dialog").
    ///
    /// <para>Geprüft wird die KERNSEITE der zwei Wege: die Bereichstabelle
    /// (Hilfeschlüssel → Bereich), der gemeldete Aufruf samt seiner Wirkung auf
    /// <c>KiChatKontext.AktuellerBereich</c>, das Aktionswissen je Meldungskennung und
    /// die vorbelegte Frage. Die Oberfläche kommt darin nicht vor.</para>
    ///
    /// <para><b>Serielle Sammlung</b> (Regel iU5‑O‑1): Diese Fälle setzen
    /// PROZESSWEITEN Zustand — <c>KiChatKontext.Aufruf</c>,
    /// <c>KiChatKontext.AktiverBereich</c> und <c>KiVerfuegbarkeit.Haken</c>. Zwei
    /// verschiedene Sammlungen liefen nebeneinander; es gibt genau eine serielle.</para>
    ///
    /// <para>Pinnt die Kultur (Auftrag #230, Befund „Windows-CI rot seit Lauf 262"):
    /// <c>Die_Frage_zur_Kennung_schlaegt_den_allgemeinen_Satz</c> hält den deutschen
    /// Fragetext („Speicherflotte") fest, den <see cref="KiMeldungskennung.Frage"/> aus
    /// Ressourcen baut, die <c>CurrentUICulture</c> folgen — die Klasse pinnte bis dahin
    /// gar keine Kultur.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KiDialogaufrufTests : IDisposable
    {
        private readonly Func<string> _bereichVorher = KiChatKontext.AktiverBereich;
        private readonly Func<bool> _verfuegbarVorher = KiVerfuegbarkeit.Haken;
        private readonly Kulturvorrichtung _kultur = new();

        /// <summary>Stellt den prozessweiten Zustand zurück — jeder Fall, jedes Mal.</summary>
        public void Dispose()
        {
            KiChatKontext.AufrufMelden(null);
            KiChatKontext.AktiverBereich = _bereichVorher;
            KiVerfuegbarkeit.Haken = _verfuegbarVorher;
            _kultur.Dispose();
        }

        // =================================================================
        //  Die Bereichstabelle (Weg 1)
        // =================================================================

        /// <summary>
        /// DER WÄCHTER: Jeder Hilfeschlüssel des Bestands findet einen Bereich.
        /// </summary>
        /// <remarks>
        /// <para>Die Liste kommt aus <c>help_mapping.txt</c> — der einzigen Stelle, an
        /// der Hilfe gepflegt wird (ihr Kopf sagt es wörtlich). Sie ist die schärfere
        /// Quelle als die <c>.razor</c>-Dateien: Über die Hälfte der
        /// <c>InfoKnopf</c>-Einbaustellen setzt ihren Schlüssel als Ausdruck
        /// (<c>Schluessel="@HilfeSchluessel"</c>), und was dort zur Laufzeit
        /// hineinläuft, sieht kein Regex.</para>
        /// <para>Fällt er rot aus, nennt er die fehlenden Präfixe — jedes braucht eine
        /// Zeile in <c>KiChatKontext.BEREICH_JE_HILFEPRAEFIX</c>.</para>
        /// </remarks>
        [Fact]
        public void Jeder_Hilfeschluessel_des_Bestands_findet_einen_Bereich()
        {
            IReadOnlyList<string> schluessel = Hilfeschluessel();
            Assert.True(schluessel.Count > 150,
                        "Nur " + schluessel.Count + " Hilfeschluessel gefunden — help_mapping.txt gelesen?");

            var fehlend = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string s in schluessel)
            {
                string bereich = KiChatKontext.BereichFuerHilfeschluessel(s);
                if (string.Equals(bereich, KiChatKontext.BEREICH_UNBEKANNT, StringComparison.Ordinal))
                    fehlend.Add(Maskenpraefix(s));
            }

            Assert.True(fehlend.Count == 0,
                        "Diese Maskenpraefixe kennen keinen Bereich (je eine Zeile in "
                        + "KiChatKontext.BEREICH_JE_HILFEPRAEFIX ergaenzen):"
                        + Environment.NewLine + string.Join(Environment.NewLine, fehlend));
        }

        /// <summary>Jeder Wert der Tabelle steht in der Positivliste — sonst käme er nie hinaus.</summary>
        [Fact]
        public void Jeder_Eintrag_der_Tabelle_steht_in_der_Positivliste()
        {
            var erlaubt = new HashSet<string>(KiChatKontext.Bereiche, StringComparer.Ordinal);

            foreach (KeyValuePair<string, string> paar in KiChatKontext.Hilfepraefixe)
                Assert.True(erlaubt.Contains(paar.Value),
                            paar.Key + " zeigt auf „" + paar.Value + "“ — das steht nicht in der Positivliste.");
        }

        [Theory]
        [InlineData("Form_Heizkessel.btn_Help", KiChatKontext.B_HEIZKESSEL)]
        [InlineData("Form_Heizkessel_Bearbeiten.btn_Help", KiChatKontext.B_HEIZKESSEL)]
        [InlineData("Form_Kosten_Auswahl.btn_Help", KiChatKontext.B_KOSTEN)]
        [InlineData("Form_Simulation_Config.btn_Help", KiChatKontext.B_SIM_KONFIG)]
        [InlineData("Form_Simulation_Detail.btn_Help", KiChatKontext.B_SIM_DETAIL)]
        [InlineData("Form_QuelleErdreich.btn_Help", KiChatKontext.B_QUELLE_ERDREICH)]
        [InlineData("Form_KiChat.btn_Help", KiChatKontext.B_HILFE)]
        [InlineData("Wizard_WPItem.btn_Help", KiChatKontext.B_WAERMEPUMPE)]
        public void Der_Bereich_folgt_aus_dem_Maskenpraefix(string schluessel, string erwartet)
        {
            Assert.Equal(erwartet, KiChatKontext.BereichFuerHilfeschluessel(schluessel));
        }

        /// <summary>
        /// „Form_Stromspeicher" und „Form_Stromverbraucher" sind zwei Bereiche — unter
        /// mehreren passenden Präfixen gewinnt das längste.
        /// </summary>
        [Fact]
        public void Unter_mehreren_Praefixen_gewinnt_das_laengste()
        {
            Assert.Equal(KiChatKontext.B_STROMSPEICHER,
                         KiChatKontext.BereichFuerHilfeschluessel("Form_Stromspeicher_einlesen.btn_Help"));
            Assert.Equal(KiChatKontext.B_STROMVERBRAUCHER,
                         KiChatKontext.BereichFuerHilfeschluessel("Form_Stromverbraucher_Admin.btn_Help"));
            Assert.Equal(KiChatKontext.B_SOLARTHERMIE,
                         KiChatKontext.BereichFuerHilfeschluessel("Form_Solarganglinie_Admin.btn_Help"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("Gibt_Es_Nicht.btn_Help")]
        [InlineData(".btn_Help")]
        public void Ein_unbekannter_Schluessel_bleibt_unbekannt(string schluessel)
        {
            Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT,
                         KiChatKontext.BereichFuerHilfeschluessel(schluessel));
        }

        // =================================================================
        //  Der gemeldete Aufruf
        // =================================================================

        [Fact]
        public void Der_gemeldete_Aufruf_setzt_den_aktiven_Bereich()
        {
            KiChatKontext.AktiverBereich = () => KiChatKontext.B_HAUPTFENSTER;
            Assert.Equal(KiChatKontext.B_HAUPTFENSTER, KiChatKontext.AktuellerBereich());

            KiChatKontext.AufrufMelden(
                KiAufrufkontext.AusHilfeschluessel("Form_Heizkessel.btn_Help", "Heizkessel bearbeiten"));

            Assert.Equal(KiChatKontext.B_HEIZKESSEL, KiChatKontext.AktuellerBereich());
            Assert.Equal("Heizkessel bearbeiten", KiChatKontext.Aufruf.Dialogname);
            Assert.Equal("Form_Heizkessel.btn_Help", KiChatKontext.Aufruf.Hilfeschluessel);
        }

        /// <summary>Ohne Aufruf trägt wieder der Haken — die Hülle ist der zweite Lieferant.</summary>
        [Fact]
        public void Nach_dem_Abmelden_traegt_wieder_der_Haken()
        {
            KiChatKontext.AktiverBereich = () => KiChatKontext.B_HAUPTFENSTER;
            KiChatKontext.AufrufMelden(KiAufrufkontext.AusHilfeschluessel("Form_PV.btn_Help"));
            Assert.Equal(KiChatKontext.B_PHOTOVOLTAIK, KiChatKontext.AktuellerBereich());

            KiChatKontext.AufrufMelden(null);

            Assert.Null(KiChatKontext.Aufruf);
            Assert.Equal(KiChatKontext.B_HAUPTFENSTER, KiChatKontext.AktuellerBereich());
        }

        /// <summary>
        /// Ein Aufruf mit unbekanntem Bereich verdrängt den Haken NICHT — sonst
        /// verschlechterte der Assistent seine eigene Auskunft.
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Aufrufbereich_verdraengt_den_Haken_nicht()
        {
            KiChatKontext.AktiverBereich = () => KiChatKontext.B_HAUPTFENSTER;
            KiChatKontext.AufrufMelden(KiAufrufkontext.AusHilfeschluessel("Gibt_Es_Nicht.btn_Help"));

            Assert.Equal(KiChatKontext.B_HAUPTFENSTER, KiChatKontext.AktuellerBereich());
        }

        /// <summary>Auch ein frei gesetzter Bereich geht durch die Freigabeschranke.</summary>
        [Fact]
        public void Ein_freier_Bereich_im_Aufruf_kommt_nicht_hinaus()
        {
            KiChatKontext.AktiverBereich = null;
            KiChatKontext.AufrufMelden(new KiAufrufkontext { Bereich = "Projekt: Muster GmbH" });

            Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, KiChatKontext.AktuellerBereich());
        }

        // =================================================================
        //  Verfügbarkeit (KI‑D‑Q1)
        // =================================================================

        [Fact]
        public void Der_Haken_entscheidet_ueber_die_Verfuegbarkeit()
        {
            KiVerfuegbarkeit.Haken = () => false;
            Assert.False(KiVerfuegbarkeit.Moeglich);

            KiVerfuegbarkeit.Haken = () => true;
            Assert.True(KiVerfuegbarkeit.Moeglich);
        }

        /// <summary>Eine gescheiterte Auskunft zeichnet keinen Knopf, der ins Leere führt.</summary>
        [Fact]
        public void Ein_werfender_Haken_gilt_als_nicht_moeglich()
        {
            KiVerfuegbarkeit.Haken = () => throw new InvalidOperationException("kaputt");
            Assert.False(KiVerfuegbarkeit.Moeglich);
        }

        // =================================================================
        //  Aktionswissen je Kennung (Weg 2)
        // =================================================================

        /// <summary>Jede Kennung führt genau EINEN Abschnitt — Bedeutung, Ursache, Abhilfe, Wiki.</summary>
        [Fact]
        public void Jede_Kennung_hat_einen_Wissensabschnitt()
        {
            Assert.Equal(15, KiMeldungskennung.Alle.Length);

            foreach (string kennung in KiMeldungskennung.Alle)
            {
                List<WissensAbschnitt> treffer = HilfeWissen.Abschnitte
                    .Where(a => string.Equals(a.Kennung, kennung, StringComparison.Ordinal))
                    .ToList();

                Assert.True(treffer.Count == 1,
                            kennung + ": " + treffer.Count + " Abschnitte (erwartet genau einer).");

                WissensAbschnitt a = treffer[0];
                Assert.Contains(kennung, a.Titel, StringComparison.Ordinal);
                Assert.Contains("BEDEUTUNG", a.Inhalt, StringComparison.Ordinal);
                Assert.Contains("URSACHE", a.Inhalt, StringComparison.Ordinal);
                Assert.Contains("ABHILFE", a.Inhalt, StringComparison.Ordinal);
                Assert.Contains("WIKI", a.Inhalt, StringComparison.Ordinal);
                Assert.StartsWith("https://wiki.epos-plan.de/", a.QuellUrl, StringComparison.Ordinal);
                Assert.Contains(a.Bereich, KiChatKontext.Bereiche);
            }
        }

        /// <summary>
        /// Die Stichwortsuche findet den Abschnitt über die Kennung — auch mit einer
        /// Frage, die kein Wort des Abschnitts trägt.
        /// </summary>
        [Fact]
        public void Suchen_findet_den_Abschnitt_ueber_die_Kennung()
        {
            List<WissensAbschnitt> treffer = HilfeWissen.Suchen(
                "Bitte um Auskunft", KiChatKontext.BEREICH_UNBEKANNT, 4,
                KiMeldungskennung.FLOTTE_ARBEITSLOS);

            Assert.NotEmpty(treffer);
            Assert.Equal(KiMeldungskennung.FLOTTE_ARBEITSLOS, treffer[0].Kennung);
        }

        /// <summary>Ohne Kennung gewinnt der Abschnitt nicht — die Gegenprobe zum Zuschlag.</summary>
        [Fact]
        public void Ohne_Kennung_gewinnt_der_Abschnitt_nicht()
        {
            List<WissensAbschnitt> treffer =
                HilfeWissen.Suchen("Bitte um Auskunft", KiChatKontext.BEREICH_UNBEKANNT, 4, "");

            Assert.True(treffer.Count == 0 || treffer[0].Kennung != KiMeldungskennung.FLOTTE_ARBEITSLOS);
        }

        /// <summary>
        /// Ohne ausdrückliche Angabe zieht die Suche die Kennung aus dem gemeldeten
        /// Aufruf — so trägt „erklären lassen" durch die ganze Kette.
        /// </summary>
        [Fact]
        public void Ohne_Angabe_gilt_die_Kennung_des_gemeldeten_Aufrufs()
        {
            KiChatKontext.AufrufMelden(new KiAufrufkontext
            {
                Bereich = KiChatKontext.B_PHOTOVOLTAIK,
                Kennung = KiMeldungskennung.PV_STRANG_P4
            });

            List<WissensAbschnitt> treffer =
                HilfeWissen.Suchen("Bitte um Auskunft", KiChatKontext.B_PHOTOVOLTAIK);

            Assert.NotEmpty(treffer);
            Assert.Equal(KiMeldungskennung.PV_STRANG_P4, treffer[0].Kennung);
        }

        [Fact]
        public void Der_Abschnitt_laesst_sich_unmittelbar_nachschlagen()
        {
            WissensAbschnitt a = HilfeWissen.AbschnittFuerKennung(KiMeldungskennung.PV_STRANG_P8);

            Assert.NotNull(a);
            Assert.Equal(KiMeldungskennung.PV_STRANG_P8, a.Kennung);
            Assert.Null(HilfeWissen.AbschnittFuerKennung("GIBT_ES_NICHT"));
            Assert.Null(HilfeWissen.AbschnittFuerKennung(""));
        }

        /// <summary>Die fünf Prüfhinweise der Flotte bilden auf ihre Kennungen ab.</summary>
        [Fact]
        public void Jeder_Pruefhinweis_der_Flotte_hat_eine_Kennung()
        {
            foreach (FlottenHinweisKennung k in Enum.GetValues(typeof(FlottenHinweisKennung)))
            {
                string kennung = KiMeldungskennung.Fuer(k);
                Assert.False(string.IsNullOrEmpty(kennung), k + " hat keine Kennung.");
                Assert.Contains(kennung, KiMeldungskennung.Alle);
            }

            Assert.Equal("", KiMeldungskennung.Fuer((FlottenHinweisKennung)99));
        }

        [Fact]
        public void Die_acht_Strangregeln_tragen_ihre_Nummer()
        {
            for (int regel = 1; regel <= 8; regel++)
                Assert.Equal("PV_STRANG_P" + regel, KiMeldungskennung.FuerStrangregel(regel));

            Assert.Equal("", KiMeldungskennung.FuerStrangregel(0));
            Assert.Equal("", KiMeldungskennung.FuerStrangregel(9));
        }

        // =================================================================
        //  Die vorbelegte Frage
        // =================================================================

        /// <summary>Jede Kennung hat eine eigene Frage — in beiden Sprachen.</summary>
        [Fact]
        public void Jede_Kennung_hat_eine_Frage_in_beiden_Sprachen()
        {
            var fehlend = new List<string>();

            foreach (string kennung in KiMeldungskennung.Alle)
                foreach (string kultur in new[] { "de-DE", "en-US" })
                {
                    string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                        "KI_FRAGE_" + kennung, new System.Globalization.CultureInfo(kultur));

                    if (string.IsNullOrWhiteSpace(text)) fehlend.Add(kennung + " (" + kultur + ")");
                }

            Assert.True(fehlend.Count == 0,
                        "Fehlende Ressourcen KI_FRAGE_*:" + Environment.NewLine
                        + string.Join(Environment.NewLine, fehlend));
        }

        /// <summary>
        /// AUFTRAG #221 (KI-D-E-1): Jede ANSICHT mit eigener Startfrage hat sie in
        /// beiden Sprachen — sonst stuende auf der englischen Oberflaeche ein deutscher
        /// Satz in der Eingabezeile.
        /// </summary>
        [Fact]
        public void Jede_Startfrage_einer_Ansicht_steht_in_beiden_Sprachen()
        {
            var fehlend = new List<string>();

            foreach (KeyValuePair<string, string> paar in KiChatKontext.Startfragepraefixe)
                foreach (string kultur in new[] { "de-DE", "en-US" })
                {
                    string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                        paar.Value, new System.Globalization.CultureInfo(kultur));

                    if (string.IsNullOrWhiteSpace(text))
                        fehlend.Add(paar.Key + " → " + paar.Value + " (" + kultur + ")");
                }

            Assert.True(fehlend.Count == 0,
                        "Fehlende Startfragen:" + Environment.NewLine
                        + string.Join(Environment.NewLine, fehlend));
        }

        /// <summary>
        /// Die Startfrage folgt dem MASKENPRAEFIX des Hilfeschluessels — und ein
        /// unbekanntes Praefix liefert nichts, statt eine Frage zu erfinden.
        /// </summary>
        [Fact]
        public void Die_Startfrage_folgt_dem_Maskenpraefix()
        {
            string konfig =
                KiChatKontext.StartfrageFuerHilfeschluessel("Form_Simulation_Config.btn_Help");
            string ergebnis =
                KiChatKontext.StartfrageFuerHilfeschluessel("Form_Simulation_Detail.btn_Help");

            Assert.NotEqual("", konfig);
            Assert.NotEqual("", ergebnis);
            Assert.NotEqual(konfig, ergebnis);

            Assert.Equal("", KiChatKontext.StartfrageFuerHilfeschluessel("Form_PV.btn_Help"));
            Assert.Equal("", KiChatKontext.StartfrageFuerHilfeschluessel(""));
            Assert.Equal("", KiChatKontext.StartfrageFuerHilfeschluessel(null));
        }

        /// <summary>
        /// AUFTRAG #221: Der Aufruf MELDET seinen Wechsel — daran haengt die Pille, die
        /// leuchtet, solange der Assistent fuer eine Ansicht steht (#218).
        /// </summary>
        [Fact]
        public void Ein_Aufrufwechsel_wird_gemeldet_und_die_Pille_weiss_fuer_wen()
        {
            int gemeldet = 0;
            Action hoerer = () => gemeldet++;

            KiChatKontext.AufrufGeaendert += hoerer;
            try
            {
                KiChatKontext.AufrufMelden(
                    KiAufrufkontext.AusHilfeschluessel("Form_Simulation_Detail.btn_Help"));

                Assert.Equal(1, gemeldet);
                Assert.True(KiChatKontext.AssistentStehtFuer("Form_Simulation_Detail.btn_Help"));
                Assert.False(KiChatKontext.AssistentStehtFuer("Form_Simulation_Config.btn_Help"));
                Assert.False(KiChatKontext.AssistentStehtFuer(""));

                KiChatKontext.AufrufMelden(null);

                Assert.Equal(2, gemeldet);
                Assert.False(KiChatKontext.AssistentStehtFuer("Form_Simulation_Detail.btn_Help"));
            }
            finally
            {
                KiChatKontext.AufrufGeaendert -= hoerer;
                KiChatKontext.AufrufMelden(null);
            }
        }

        [Fact]
        public void Die_Frage_zur_Kennung_schlaegt_den_allgemeinen_Satz()
        {
            string besondere = KiMeldungskennung.Frage(KiMeldungskennung.FLOTTE_ARBEITSLOS,
                                                       "Irgendein Bannertext");

            Assert.Contains("Speicherflotte", besondere, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Irgendein Bannertext", besondere, StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_eigene_Frage_gilt_der_allgemeine_Satz_mit_dem_Bannertext()
        {
            string allgemein = KiMeldungskennung.Frage("GIBT_ES_NICHT", "Der Wirkungsgrad liegt über 100 %.");

            Assert.Contains("Der Wirkungsgrad liegt über 100 %.", allgemein, StringComparison.Ordinal);
        }

        /// <summary>Ohne Kennung UND ohne Text bleibt die Eingabezeile leer.</summary>
        [Fact]
        public void Ohne_alles_bleibt_die_Frage_leer()
        {
            Assert.Equal("", KiMeldungskennung.Frage("", ""));
            Assert.Equal("", KiMeldungskennung.Frage(null, null));
        }

        // =================================================================
        //  Der Maskenschlüssel
        // =================================================================

        /// <summary>
        /// <c>Masken.KiAssistent</c> und der Seitenschlüssel der Oberfläche sind
        /// DIESELBE Zeichenkette — zwei wären zwei Wahrheiten.
        /// </summary>
        [Fact]
        public void Der_Maskenschluessel_ist_der_Seitenschluessel()
        {
            Assert.Equal("KI_ASSISTENT", Masken.KiAssistent);
            Assert.Equal(KiChatKontext.B_HILFE, KiChatKontext.BereichFuerSeite(Masken.KiAssistent));
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        /// <summary>Eine Zuordnungszeile <c>Praefix.Control = Ziel</c> aus help_mapping.txt.</summary>
        private static readonly Regex Zuordnung =
            new Regex(@"^([A-Za-z_][A-Za-z0-9_.]*)\s*=", RegexOptions.Compiled);

        /// <summary>Alle Hilfeschlüssel des Bestands (help_mapping.txt, linke Seite).</summary>
        private static IReadOnlyList<string> Hilfeschluessel()
        {
            string pfad = Path.Combine(Arbeitsbaum(), "WindowsFormsApplication1", "Allgemein",
                                       "Hilfe", "help_mapping.txt");
            Assert.True(File.Exists(pfad), "help_mapping.txt nicht gefunden: " + pfad);

            var schluessel = new List<string>();
            foreach (string rohzeile in File.ReadAllLines(pfad, Encoding.UTF8))
            {
                string zeile = rohzeile.Trim('﻿', ' ', '\t');
                if (zeile.Length == 0 || zeile.StartsWith("#", StringComparison.Ordinal)) continue;

                Match m = Zuordnung.Match(zeile);
                if (m.Success) schluessel.Add(m.Groups[1].Value);
            }

            return schluessel;
        }

        private static string Maskenpraefix(string schluessel)
        {
            int punkt = (schluessel ?? "").IndexOf('.');
            return punkt < 0 ? (schluessel ?? "") : schluessel.Substring(0, punkt);
        }

        /// <summary>
        /// Die Wurzel des Arbeitsbaums — über <see cref="CallerFilePathAttribute"/>,
        /// sonst vom Ausgabeordner aufwärts (Muster <c>EinheitenWacheTests</c>).
        /// </summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
