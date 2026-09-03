using System;
using System.IO;
using Xunit;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die neun Umgebungsdienste (Umsetzungskonzept iU5).
    ///
    /// <para>Geprüft wird zweierlei: dass die VORBELEGUNGEN sich so verhalten, wie der
    /// Kern es ohne Oberfläche erwarten darf, und dass jeder Dienst AUSTAUSCHBAR ist —
    /// Feld setzen, prüfen, zurücksetzen. Beides ist ohne Datenbank und ohne
    /// WinForms entscheidbar.</para>
    ///
    /// <para>Jeder Test, der einen Dienst tauscht, setzt ihn im <c>finally</c> zurück:
    /// <see cref="Dienste"/> ist prozessweiter Zustand, und xunit gibt keine Reihenfolge
    /// vor.</para>
    /// </summary>
    public class DiensteTests
    {
        // ==================================================================
        //  Vorbelegungen
        // ==================================================================

        [Fact]
        public void Vorbelegungen_sind_gesetzt()
        {
            Assert.NotNull(Dienste.Dialog);
            Assert.NotNull(Dienste.Datei);
            Assert.NotNull(Dienste.Pfade);
            Assert.NotNull(Dienste.Einstellungen);
            Assert.NotNull(Dienste.Lizenzablage);
            Assert.NotNull(Dienste.GeraeteId);
            Assert.NotNull(Dienste.Sprache);
            Assert.NotNull(Dienste.Navigation);
            Assert.NotNull(Dienste.Projekt);
        }

        /// <summary>
        /// Ohne Bedienung wird die Variante mit dem kleineren Schaden gewählt: Die
        /// Rückfrage wird VERNEINT, die Dreifachwahl abgebrochen. Der abweichende
        /// Engine-Modus von <c>AnlagenEindeutigkeit</c> („Ja", weil dort „Nein" eine
        /// Anlagenzeile verwürfe) entscheidet vor dieser Vorbelegung und ist damit nicht
        /// betroffen — siehe den Klassenkommentar von <see cref="StilleDialoge"/>.
        /// </summary>
        [Fact]
        public void StilleDialoge_verneinen_und_brechen_ab()
        {
            IDialogDienst still = new StilleDialoge();

            Assert.False(still.Frage("Wirklich löschen?"));
            Assert.False(still.Frage("Wirklich löschen?", "Titel", true, true));
            Assert.Equal(JaNeinAbbruch.Abbruch, still.Wahl("Speichern?"));

            // Die drei Meldungsformen und die Wartekurve dürfen nichts werfen.
            still.Meldung("Text");
            still.Meldung("Text", "Titel");
            still.Warnung("Text", "Titel");
            still.Fehler("Text", "Titel");
            still.Warten(true);
            still.Warten(false);
        }

        [Fact]
        public void KeineDateiwahl_liefert_leer()
        {
            IDateiDienst datei = new KeineDateiwahl();

            Assert.Equal("", datei.DateiOeffnen("Titel", "*.*", ""));
            Assert.Equal("", datei.DateiSpeichern("Titel", "*.*", "vorschlag.csv"));
            Assert.Equal("", datei.OrdnerWaehlen("Titel", ""));
            Assert.False(datei.MitSystemOeffnen("beliebig.txt"));
        }

        [Fact]
        public void FluechtigeEinstellungen_lesen_schreiben_loeschen()
        {
            IEinstellungen e = new FluechtigeEinstellungen();

            Assert.Null(e.Lies("Fehlt"));
            Assert.Equal("Vorgabe", e.Lies("Fehlt", "Vorgabe"));
            Assert.Equal(7, e.LiesZahl("Fehlt", 7));

            e.Schreib("Wort", "Wert");
            Assert.Equal("Wert", e.Lies("Wort", "Vorgabe"));

            e.SchreibZahl("Zahl", 42);
            Assert.Equal(42, e.LiesZahl("Zahl", 7));
            Assert.Equal("42", e.Lies("Zahl"));

            e.Loesche("Wort");
            Assert.Equal("Vorgabe", e.Lies("Wort", "Vorgabe"));

            // Ein maschinenweiter Wert ist ohne Betriebssystemablage nicht darstellbar.
            Assert.Equal("aus", e.LiesMaschine("Abschalter", "aus"));
        }

        [Fact]
        public void KeineAblage_merkt_sich_nichts()
        {
            ILizenzAblage ablage = new KeineAblage();

            ablage.Schreiben("lizenz.dat", new byte[] { 1, 2, 3 }, true);

            Assert.Null(ablage.Lesen("lizenz.dat", true));
            Assert.False(ablage.Vorhanden("lizenz.dat"));
            Assert.EndsWith("lizenz.dat", ablage.Ablageort("lizenz.dat"));

            ablage.Loeschen("lizenz.dat");
        }

        [Fact]
        public void KeineGeraeteId_liefert_leere_Merkmale()
        {
            IGeraeteId id = new KeineGeraeteId();

            Assert.Equal("", id.Kennung);
            Assert.Equal("", id.Anzeige);
        }

        [Fact]
        public void KeineNavigation_oeffnet_nichts()
        {
            INavigation nav = new KeineNavigation();

            Assert.False(nav.OeffneMaske(Masken.WpAdministration));
            Assert.False(nav.OeffneMaske("gibt-es-nicht", 1, "zwei"));

            nav.OeffneGewerk(Gewerke.Bhkw, 1030, "Projekt");
            nav.MenueAktualisieren();
            nav.AnsichtAktualisieren(Ansichten.Varianten);
        }

        [Fact]
        public void LeererProjektKontext_haelt_und_meldet()
        {
            IProjektKontext kontext = new LeererProjektKontext();
            int gewechselt = 0;
            kontext.Gewechselt += () => gewechselt++;

            Assert.Equal(0, kontext.Id);
            Assert.Equal("", kontext.Name);
            Assert.Equal("", kontext.Klimazone);

            Assert.False(kontext.Uebernehmen(0, ""));
            Assert.Equal(0, gewechselt);

            Assert.True(kontext.Uebernehmen(1030, "Kesselhaus Nord"));
            Assert.Equal(1030, kontext.Id);
            Assert.Equal("Kesselhaus Nord", kontext.Name);
            Assert.Equal(1, gewechselt);
        }

        // ==================================================================
        //  Pfade
        // ==================================================================

        /// <summary>
        /// Die Wurzeln müssen zeichengleich zum Bestand bleiben — der Lizenztoken, der
        /// KI-Schlüssel und die Zwischenspeicher liegen darunter. Geprüft wird der
        /// AUFBAU (Wurzel + genau dieser Unterordner), nicht der Betriebssystempfad
        /// selbst.
        /// </summary>
        [Fact]
        public void StandardPfade_bilden_die_Bestandsordner()
        {
            IPfade pfade = new StandardPfade();

            Assert.Equal(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wp-plan"),
                pfade.Anwendungsdaten);
            Assert.Equal(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WP-Plan"),
                pfade.Produktdaten);
            Assert.Equal(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WP-Plan"),
                pfade.BenutzerLokal);
            Assert.Equal(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                pfade.BenutzerLokalBasis);
            Assert.Equal(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WP-Plan"),
                pfade.Gemeinsam);
            Assert.Equal(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                pfade.Dokumente);
        }

        /// <summary>
        /// <c>Verbinde</c> legt NICHTS an, <c>Unterordner</c> schon. Der Unterschied ist
        /// der Grund, warum es beide gibt: Ein Teil der Fundstellen erzeugte den Ordner
        /// beim Bilden des Pfades, der andere nicht.
        /// </summary>
        [Fact]
        public void Verbinde_legt_nichts_an_Unterordner_schon()
        {
            IPfade pfade = new StandardPfade();
            string wurzel = Path.Combine(Path.GetTempPath(), "epos-iu5-" + Guid.NewGuid().ToString("N"));

            try
            {
                string nurPfad = pfade.Verbinde(wurzel, "a", "b");
                Assert.Equal(Path.Combine(wurzel, "a", "b"), nurPfad);
                Assert.False(Directory.Exists(nurPfad));

                string angelegt = pfade.Unterordner(wurzel, "a", "b");
                Assert.Equal(nurPfad, angelegt);
                Assert.True(Directory.Exists(angelegt));

                // Leere Bestandteile werden übergangen, nicht als Trenner gewertet.
                Assert.Equal(Path.Combine(wurzel, "a"), pfade.Verbinde(wurzel, "", "a", null));
            }
            finally
            {
                try { if (Directory.Exists(wurzel)) Directory.Delete(wurzel, true); } catch { }
            }
        }

        // ==================================================================
        //  Sprache
        // ==================================================================

        [Fact]
        public void StandardSprache_setzt_Nummer_und_Kultur()
        {
            int vorher = Sprache.Nummer;
            var kulturVorher = System.Globalization.CultureInfo.DefaultThreadCurrentUICulture;
            ISprache sprache = new StandardSprache();

            try
            {
                sprache.Setzen("en");
                Assert.Equal(1, Sprache.Nummer);
                Assert.True(sprache.IstEnglisch);
                Assert.Equal("en", sprache.Kuerzel);
                Assert.Equal("en-US", System.Threading.Thread.CurrentThread.CurrentUICulture.Name);
                Assert.Equal("en-US", System.Globalization.CultureInfo.DefaultThreadCurrentUICulture.Name);

                sprache.Setzen("de");
                Assert.Equal(0, Sprache.Nummer);
                Assert.False(sprache.IstEnglisch);
                Assert.Equal("de", sprache.Kuerzel);
                Assert.Equal("de-DE", System.Threading.Thread.CurrentThread.CurrentUICulture.Name);

                // Alles Unbekannte gilt als Deutsch - wie der bisherige nLanguage-Zweig.
                sprache.Setzen(null);
                Assert.Equal(0, Sprache.Nummer);
                sprache.Setzen("kl");
                Assert.Equal(0, Sprache.Nummer);

                // Auch die lange Form wird erkannt.
                sprache.Setzen("en-US");
                Assert.Equal(1, Sprache.Nummer);
            }
            finally
            {
                Sprache.Nummer = vorher;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = kulturVorher;
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo(vorher == 0 ? "de-DE" : "en-US");
            }
        }

        // ==================================================================
        //  Austauschbarkeit
        // ==================================================================

        /// <summary>
        /// Der Kern des Hausmusters: Ein Prüfstand legt eine eigene Fassung ein, fährt
        /// seinen Fall und legt die Vorbelegung zurück. Geprüft am Dialogdienst, weil
        /// über ihn auch die vier <see cref="Meldung"/>-Haken laufen.
        /// </summary>
        [Fact]
        public void Dialogdienst_ist_austauschbar_und_traegt_die_Meldehaken()
        {
            IDialogDienst vorher = Dienste.Dialog;
            var mitschrift = new MitschreibendeDialoge();

            try
            {
                Dienste.Dialog = mitschrift;

                Meldung.Zeigen("ohne Titel");
                Meldung.Hinweis("mit Titel", "Titel");
                Meldung.Warnung("gewarnt", "Achtung");
                Meldung.Warten(true);

                Assert.Equal("Meldung|ohne Titel|", mitschrift.Zeilen[0]);
                Assert.Equal("Meldung|mit Titel|Titel", mitschrift.Zeilen[1]);
                Assert.Equal("Warnung|gewarnt|Achtung", mitschrift.Zeilen[2]);
                Assert.Equal("Warten|True", mitschrift.Zeilen[3]);

                Assert.True(Dienste.Dialog.Frage("Weiter?"));
            }
            finally
            {
                Dienste.Dialog = vorher;
            }

            Assert.Same(vorher, Dienste.Dialog);
        }

        [Fact]
        public void Alle_neun_Dienste_sind_austauschbar()
        {
            IDialogDienst dialog = Dienste.Dialog;
            IDateiDienst datei = Dienste.Datei;
            IPfade pfade = Dienste.Pfade;
            IEinstellungen einstellungen = Dienste.Einstellungen;
            ILizenzAblage ablage = Dienste.Lizenzablage;
            IGeraeteId geraet = Dienste.GeraeteId;
            ISprache sprache = Dienste.Sprache;
            INavigation navigation = Dienste.Navigation;
            IProjektKontext projekt = Dienste.Projekt;

            try
            {
                Dienste.Dialog = new StilleDialoge();
                Dienste.Datei = new KeineDateiwahl();
                Dienste.Pfade = new StandardPfade();
                Dienste.Einstellungen = new FluechtigeEinstellungen();
                Dienste.Lizenzablage = new KeineAblage();
                Dienste.GeraeteId = new KeineGeraeteId();
                Dienste.Sprache = new StandardSprache();
                Dienste.Navigation = new KeineNavigation();
                Dienste.Projekt = new LeererProjektKontext();

                Assert.IsType<StilleDialoge>(Dienste.Dialog);
                Assert.IsType<KeineDateiwahl>(Dienste.Datei);
                Assert.IsType<StandardPfade>(Dienste.Pfade);
                Assert.IsType<FluechtigeEinstellungen>(Dienste.Einstellungen);
                Assert.IsType<KeineAblage>(Dienste.Lizenzablage);
                Assert.IsType<KeineGeraeteId>(Dienste.GeraeteId);
                Assert.IsType<StandardSprache>(Dienste.Sprache);
                Assert.IsType<KeineNavigation>(Dienste.Navigation);
                Assert.IsType<LeererProjektKontext>(Dienste.Projekt);
            }
            finally
            {
                Dienste.Dialog = dialog;
                Dienste.Datei = datei;
                Dienste.Pfade = pfade;
                Dienste.Einstellungen = einstellungen;
                Dienste.Lizenzablage = ablage;
                Dienste.GeraeteId = geraet;
                Dienste.Sprache = sprache;
                Dienste.Navigation = navigation;
                Dienste.Projekt = projekt;
            }
        }

        /// <summary>Ein Dialogdienst, der nur mitschreibt und jede Frage bejaht.</summary>
        private sealed class MitschreibendeDialoge : IDialogDienst
        {
            public readonly System.Collections.Generic.List<string> Zeilen =
                new System.Collections.Generic.List<string>();

            public void Meldung(string text, string titel = null) { Zeilen.Add("Meldung|" + text + "|" + (titel ?? "")); }
            public void Warnung(string text, string titel = null) { Zeilen.Add("Warnung|" + text + "|" + (titel ?? "")); }
            public void Fehler(string text, string titel = null) { Zeilen.Add("Fehler|" + text + "|" + (titel ?? "")); }
            public bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false)
            {
                Zeilen.Add("Frage|" + text + "|" + (titel ?? ""));
                return true;
            }
            public JaNeinAbbruch Wahl(string text, string titel = null)
            {
                Zeilen.Add("Wahl|" + text + "|" + (titel ?? ""));
                return JaNeinAbbruch.Ja;
            }
            public void Warten(bool an) { Zeilen.Add("Warten|" + an); }
        }
    }
}
