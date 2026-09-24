using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der WEG ZUR MASKE: Was die Absage sagt, wenn eine Formularaktion gerufen wird und
    /// keine Maske offen ist (Anwenderbefund vom 15.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was der Befund war.</b> Gefragt war „setze die Vorlauftemperatur Heizkessel auf
    /// 65°C und die Rücklauftemperatur auf 55°C", und der Assistent rief richtig
    /// <c>formular_ausfuellen</c> mit <c>vorlauf=65; ruecklauf=55</c>. Beide Felder gibt
    /// es im Katalog. Die Absage lautete trotzdem nur „Es ist keine steuerbare Maske
    /// geöffnet. Steuerbar sind: Form_Heizkessel_Bearbeiten, Form_PV, …" — eine Liste von
    /// TYPNAMEN, aus der niemand einen Menüweg ableiten kann.
    /// </para>
    /// <para>
    /// <b>Was diese Fälle halten.</b> Dass die Absage die gemeinte Maske NENNT, sobald sie
    /// sich aus den Feldnamen erschließen lässt — und dass sie dort schweigt, wo die
    /// Zuordnung mehrdeutig wäre. Geraten wird nichts.
    /// </para>
    /// <para>
    /// <b>Die Kultur ist gepinnt</b> (<see cref="Kulturvorrichtung"/>, Hausregel
    /// „Kulturpinnung"): Die Fälle halten Anzeigenamen und Absagetexte aus
    /// <c>MyResource.Resource</c>, die über <c>CurrentUICulture</c> auflösen. Die Fälle
    /// des Weges zur Verwaltung laufen zusätzlich als Theorie in <c>de-DE</c> UND
    /// <c>en-US</c>: Auf dem Windows-Läufer (<c>en-US</c>) nannte die Absage für
    /// <c>bereitschaftsverlust</c> den Editor statt der Verwaltung, weil die Zielmaske
    /// über ihre deutsche Beschriftung gefunden wurde - der Katalog entscheidet das jetzt
    /// über Schlüssel (<see cref="KiDialoge.Zielfeldname"/>).
    /// </para>
    /// </remarks>
    public class KiMaskenwegTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static KiAusfuehrung Frisch() => new KiAusfuehrung { Schreibrecht = () => true };

        /// <summary>
        /// Der Klartextgrund der VORBEDINGUNG - genau die Stelle, die den Weg nennt.
        /// </summary>
        /// <remarks>
        /// <b>Nicht ueber <c>AusfuehrenAsync</c>.</b> Eine Formularaktion ist Stufe 2 und
        /// wird ohne eingeloeste Freigabe schon vom Ausfuehrer abgewiesen („ohne
        /// Bestaetigung") - die Vorbedingung liefe dann gar nicht. Die Bestaetigungsschicht
        /// ruft sie ueber die Vorbereitung; hier wird sie unmittelbar befragt, weil genau
        /// sie der Gegenstand dieser Faelle ist.
        /// </remarks>
        private static string Grund(string aktion, Dictionary<string, object> werte)
        {
            KiAktion a = Frisch().Register.Finde(aktion);
            Assert.NotNull(a);

            KiPruefErgebnis p = KiPruefung.Pruefe(a, werte);
            Assert.True(p.Gueltig, p.FehlerText());

            return a.Vorbedingung(p.Aufruf);
        }

        // ===================================================== Der Befund selbst

        /// <summary>
        /// Der Fall aus dem Befund: zwei Felder der Heizkesselmaske, keine Maske offen.
        /// </summary>
        /// <remarks>
        /// <para><b>Die zwei Felder sind <c>wirkungsgrad_gas</c> und
        /// <c>bereitschaftsverlust</c>.</b> Sie stehen an genau EINER Maske, und nur
        /// dann darf die Absage sie zuordnen.</para>
        /// <para><b>Welche Felder das sind, hat sich zweimal geändert — und beide Male
        /// aus demselben Grund.</b> <c>vorlauf</c> und <c>ruecklauf</c> taugten nach der
        /// Freigabe der Erzeugermasken des Projekts nicht mehr (sie führen fünf Masken),
        /// <c>th_leistung</c> nicht mehr seit der Freigabe des BHKW-Katalogeditors: Die
        /// thermische Leistung steht seither am Heizkessel UND am BHKW. Was bei einem
        /// mehrdeutigen Feld geschieht, hält
        /// <see cref="Ein_mehrdeutiges_Feld_wird_nicht_geraten"/> fest.</para>
        /// <para><b>Genannt wird die VERWALTUNG</b> (Welle #456): <c>wirkungsgrad_gas</c>
        /// steht am Katalogeditor UND an der „Administration Heizkessel" — beide führen
        /// an dieselbe Stelle, und dort, in der Verwaltung, lassen sich die Werte setzen.
        /// Der Editor ist nur noch die Überlagerung von „Neu…".</para>
        /// </remarks>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Die_Absage_nennt_die_Maske_zu_den_Feldern(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            string text = Grund("formular_ausfuellen",
                new Dictionary<string, object>
                { ["werte"] = "wirkungsgrad_gas=0,95; bereitschaftsverlust=1,5" });

            Assert.NotNull(text);

            // Der ANZEIGENAME der Heizkesselverwaltung steht im Satz ...
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_ADMIN).Anzeigename,
                            text, StringComparison.Ordinal);
            Assert.Contains(KiMaskennamen.HEIZKESSEL_ADMIN, text, StringComparison.Ordinal);

            // ... und der Weg dorthin wird benannt.
            Assert.Contains("dialog_oeffnen", text, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Auch_feld_setzen_bekommt_den_Weg_genannt(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            string text = Grund("feld_setzen",
                new Dictionary<string, object>
                { ["feld"] = "bereitschaftsverlust", ["wert"] = "1,5" });

            Assert.NotNull(text);
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_ADMIN).Anzeigename,
                            text, StringComparison.Ordinal);
            Assert.Contains("dialog_oeffnen", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Editor und Verwaltung sind EIN Weg</b> (Welle #456): Ein Feld, das der
        /// Katalogeditor und seine Verwaltung beide führen, ist nicht mehrdeutig — sie
        /// öffnen an derselben Stelle. Die Absage nennt die Verwaltung.
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Editor_und_Verwaltung_sind_ein_Weg_die_Absage_nennt_die_Verwaltung(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            // Vorbedingung: Das Feld steht wirklich an beiden, und beide haben dasselbe Ziel.
            KiDialog editor = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL);
            KiDialog verwaltung = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_ADMIN);
            Assert.True(editor.KenntFeld("wirkungsgrad_gas"));
            Assert.True(verwaltung.KenntFeld("wirkungsgrad_gas"));
            Assert.Equal(KiMaskenziele.Ziel(editor.Maskenname), KiMaskenziele.Ziel(verwaltung.Maskenname));

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = "wirkungsgrad_gas", ["wert"] = "0,95" });

            Assert.NotNull(text);
            Assert.Contains(verwaltung.Anzeigename, text, StringComparison.Ordinal);
            Assert.Contains("dialog_oeffnen", text, StringComparison.Ordinal);
            Assert.DoesNotContain(editor.Maskenname, text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Dasselbe Ziel allein macht nichts eindeutig</b>: Mehrere Masken, die an
        /// dieselbe Stelle führen (etwa die Masken der Startseite), sind trotzdem
        /// verschiedene Masken. Steht die Maske des Ziels selbst nicht unter den Treffern,
        /// bleibt ein Feld, das sie alle führen, mehrdeutig — geraten wird nicht.
        /// </summary>
        /// <remarks>
        /// Das Feld wird im Katalog GESUCHT und nicht festgeschrieben: Welche Felder
        /// genau diese Lage haben, ändert sich mit jeder Welle; dass es eines gibt, prüft
        /// die Vorbedingung.
        /// </remarks>
        [Fact]
        public void Masken_desselben_Ziels_ohne_die_Zielmaske_bleiben_mehrdeutig()
        {
            string gesucht = null;
            var namen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                foreach (string n in d.Feldnamen()) namen.Add(n);

            foreach (string name in namen)
            {
                var treffer = new List<KiDialog>();
                foreach (KiDialog d in KiDialoge.Katalog.Alle)
                    if (d.KenntFeld(name)) treffer.Add(d);
                if (treffer.Count < 2) continue;

                string ziel = KiMaskenziele.Ziel(treffer[0].Maskenname);
                if (ziel.Length == 0) continue;

                bool einZiel = true, zielmaskeDabei = false;
                foreach (KiDialog d in treffer)
                {
                    if (KiMaskenziele.Ziel(d.Maskenname) != ziel) einZiel = false;
                    if (string.Equals(d.Maskenname, ziel, StringComparison.OrdinalIgnoreCase)) zielmaskeDabei = true;
                }

                if (einZiel && !zielmaskeDabei) { gesucht = name; break; }
            }

            Assert.True(gesucht != null, "Der Fall braucht ein Feld mehrerer Masken desselben Ziels.");

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = gesucht, ["wert"] = "1" });

            // Keine EINE Maske samt Weg, sondern die Kandidaten (Welle #472).
            Assert.NotNull(text);
            Assert.DoesNotContain("dialog_oeffnen (", text, StringComparison.Ordinal);
            AssertNenntDieKandidaten(gesucht, text);
        }

        /// <summary>
        /// Die benannte Absage einer Mehrdeutigkeit (Welle #472): Sie nennt JEDE Maske,
        /// die das Feld unter seinem Schluessel fuehrt, mit Anzeige- und Maskennamen - und
        /// nicht die Liste aller Masken.
        /// </summary>
        private static void AssertNenntDieKandidaten(string feld, string text)
        {
            int genannt = 0, alle = 0;
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
            {
                alle++;
                if (d.KenntFeld(feld))
                {
                    genannt++;
                    Assert.Contains(d.Anzeigename + " (" + d.Maskenname + ")", text, StringComparison.Ordinal);
                }
            }

            Assert.True(genannt > 1 && genannt < alle, "Der Fall braucht ein Feld mehrerer, nicht aller Masken.");
            Assert.StartsWith(KiDialogTexte.MaskeMehrdeutig.Substring(0, 20), text, StringComparison.Ordinal);
        }

        // ============================ Der Schutz des Satzes (Welle #456)

        /// <summary>
        /// <b>Ein Auslieferungssatz lehnt das Setzen ab und nennt den Weg</b> — mit dem
        /// Schutzgrund, den der Dialog anmeldet. Die WAHL DES SATZES bleibt frei: Aus
        /// einem geschützten Satz heraus lässt sich der eigene wählen.
        /// </summary>
        [Fact]
        public void Der_Schutzgrund_nennt_den_Weg_und_die_Satzwahl_bleibt_frei()
        {
            KiDialog eintrag = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_ADMIN);
            KiDialogFeld satzfeld = eintrag.FindeFeld("satz");
            KiDialogFeld vorlauffeld = eintrag.FindeFeld("vorlauf");
            Assert.True(satzfeld.Satzwahl);

            string satz = "Kessel A";
            int? vorlauf = 70;
            var haken = new KiMaskenhaken
            {
                Schreibgeschuetzt = () => satz == "Kessel A",
                Schreibschutzgrund = () => "Mit „Duplizieren…“ einen eigenen Satz anlegen."
            };

            Func<bool> schreibrechtVorher = Schreibnaht.Schreibrecht;
            Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
            KiMaskenbruecke.Leeren();
            try
            {
                KiMaskenbruecke.Anmelden(
                    eintrag.Maskenname, eintrag,
                    new[]
                    {
                        new KiFeldzugang(satzfeld, () => satz, w => satz = (string)w, typeof(string),
                                         () => new[] { new KiWahleintrag("Kessel A", "Kessel A"),
                                                       new KiWahleintrag("Kessel B", "Kessel B") }),
                        new KiFeldzugang(vorlauffeld, () => vorlauf, w => vorlauf = (int?)w, typeof(int?))
                    },
                    haken);

                string gesperrt = Grund("feld_setzen",
                    new Dictionary<string, object> { ["feld"] = "vorlauf", ["wert"] = "55" });

                Assert.NotNull(gesperrt);
                Assert.Contains(eintrag.Anzeigename, gesperrt, StringComparison.Ordinal);
                Assert.Contains("Duplizieren", gesperrt, StringComparison.Ordinal);
                Assert.Equal(70, vorlauf);

                // Die Satzwahl geht durch - trotz Schutz des aktuellen Satzes.
                Assert.Null(Grund("feld_setzen",
                    new Dictionary<string, object> { ["feld"] = "satz", ["wert"] = "Kessel B" }));

                // Und auch dialog_speichern lehnt VOR der Bestätigung ab.
                string speichern = Grund("dialog_speichern", new Dictionary<string, object>());
                Assert.NotNull(speichern);
                Assert.Contains("Duplizieren", speichern, StringComparison.Ordinal);
            }
            finally
            {
                KiMaskenbruecke.Leeren();
                Schreibnaht.Schreibrecht = schreibrechtVorher;
            }
        }

        // ===================================================== Die Grenzen

        /// <summary>
        /// <b>Mehrdeutig heisst nicht raten.</b> <c>schritt</c> steht in mehr als einer
        /// Katalogmaske (Stromspeicher-Auslegung und Simulation); dann nennt die Absage
        /// keine davon als DEN Weg, sondern beide als Kandidaten (Welle #472).
        /// </summary>
        /// <remarks>
        /// Die Vorbedingung im Fall prueft, dass er seinen Gegenstand wirklich hat —
        /// ein Feld, das nur noch an einer Maske steht, bewiese nichts.
        /// </remarks>
        [Fact]
        public void Ein_mehrdeutiges_Feld_wird_nicht_geraten()
        {
            // Vorbedingung des Falles: Das Feld steht wirklich mehrfach im Katalog.
            int masken = 0;
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                if (d.KenntFeld("schritt")) masken++;
            Assert.True(masken > 1, "Der Fall braucht ein Feld in mehreren Masken.");

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = "schritt", ["wert"] = "1" });

            Assert.NotNull(text);
            Assert.DoesNotContain("dialog_oeffnen (", text, StringComparison.Ordinal);
            AssertNenntDieKandidaten("schritt", text);
        }

        /// <summary>
        /// <b>Der Vorlauf ist ein mehrdeutiges Feld</b> — er steht an fuenf Masken der
        /// Erzeugerfamilien. Die Absage nennt deshalb keine von ihnen als DEN Weg, sondern
        /// alle als Kandidaten; welche gemeint ist, sagt erst die OFFENE Maske.
        /// </summary>
        [Fact]
        public void Der_Vorlauf_steht_an_mehreren_Masken_und_wird_nicht_geraten()
        {
            int masken = 0;
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                if (d.KenntFeld("vorlauf")) masken++;
            Assert.True(masken > 1, "Der Fall braucht den Vorlauf in mehreren Masken.");

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = "vorlauf", ["wert"] = "55" });

            Assert.NotNull(text);
            Assert.DoesNotContain("dialog_oeffnen (", text, StringComparison.Ordinal);
            AssertNenntDieKandidaten("vorlauf", text);

            // Die Liste nennt ANZEIGENAMEN - den Typnamen nur in Klammern dahinter.
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_PROJEKT).Anzeigename,
                            text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein Feld, das es nirgends gibt, fuehrt ebenfalls auf die Liste - und die nennt
        /// ANZEIGENAMEN, keine Typnamen.
        /// </summary>
        [Fact]
        public void Ein_unbekanntes_Feld_fuehrt_auf_die_Liste_mit_Anzeigenamen()
        {
            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = "gibtesnicht", ["wert"] = "1" });

            Assert.NotNull(text);
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL).Anzeigename,
                            text, StringComparison.Ordinal);

            // Der Typname gehoert ins Protokoll, nicht in einen Satz fuer den Anwender.
            Assert.DoesNotContain(KiMaskennamen.HEIZKESSEL, text, StringComparison.Ordinal);
        }

        // ============================ Der tolerante Feldname (KI-F1b, KI-D-Q6)

        /// <summary>
        /// <b>Der Anwenderbefund vom 20.09.2026:</b> „vorlauftemperatur" wurde
        /// abgelehnt, weil der Schluessel <c>vorlauf</c> heisst. Bei OFFENER Maske
        /// loest die Bruecke den Namen jetzt auf — und die Ergebniszeile sagt, wie.
        /// </summary>
        /// <remarks>
        /// <b>Der Vermerk haengt am Ergebnistext</b> und geht damit ueber
        /// <c>KiErgebnis.Kurzfassung</c> in die Protokollzeile: Wer spaeter nachliest,
        /// welches Feld gesetzt wurde, sieht auch, unter welchem Namen es gemeint war.
        /// </remarks>
        [Fact]
        public void Vorlauftemperatur_wird_aufgeloest_und_der_Vermerk_steht_im_Ergebnis()
        {
            KiDialog eintrag = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_PROJEKT);
            KiDialogFeld feld = eintrag.FindeFeld("vorlauf");
            int gesetzt = 40;

            Func<bool> schreibrechtVorher = Schreibnaht.Schreibrecht;
            Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
            KiMaskenbruecke.Leeren();
            try
            {
                KiMaskenbruecke.Anmelden(
                    eintrag.Maskenname, eintrag,
                    new[]
                    {
                        new KiFeldzugang(feld, () => gesetzt, w => gesetzt = (int)w, typeof(int))
                    });

                var werte = new Dictionary<string, object>
                { ["feld"] = "vorlauftemperatur", ["wert"] = "55" };

                KiAktion a = Frisch().Register.Finde("feld_setzen");
                KiPruefErgebnis p = KiPruefung.Pruefe(a, werte);
                Assert.True(p.Gueltig, p.FehlerText());

                // Die Vorbedingung laesst den toleranten Namen durch ...
                Assert.Null(a.Vorbedingung(p.Aufruf));

                KiErgebnis e = a.Ausfuehren(p.Aufruf);

                Assert.Equal(KiStatus.Ausgefuehrt, e.Status);
                Assert.Equal(55, gesetzt);

                // ... und das Ergebnis - also auch die Protokollzeile - vermerkt sie.
                Assert.Contains("vorlauftemperatur", e.Kurzfassung(), StringComparison.Ordinal);
                Assert.Contains("vorlauf", e.Kurzfassung(), StringComparison.Ordinal);
            }
            finally
            {
                KiMaskenbruecke.Leeren();
                Schreibnaht.Schreibrecht = schreibrechtVorher;
            }
        }

        /// <summary>
        /// <b>Ein mehrdeutiger Name bleibt eine Absage</b> — und sie nennt die
        /// Kandidaten, statt eine Liste aller Felder zu zeigen.
        /// </summary>
        [Fact]
        public void Ein_mehrdeutiger_Feldname_an_offener_Maske_nennt_die_Kandidaten()
        {
            KiDialog eintrag = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_PROJEKT);
            int wert = 40;

            Func<bool> schreibrechtVorher = Schreibnaht.Schreibrecht;
            Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
            KiMaskenbruecke.Leeren();
            try
            {
                var zugaenge = new List<KiFeldzugang>();
                foreach (string name in new[] { "vorlauf", "ruecklauf" })
                    zugaenge.Add(new KiFeldzugang(eintrag.FindeFeld(name), () => wert,
                                                  w => wert = (int)w, typeof(int)));

                KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, zugaenge);

                string grund = Grund("feld_setzen",
                    new Dictionary<string, object> { ["feld"] = "lauf", ["wert"] = "55" });

                Assert.NotNull(grund);
                Assert.Contains("vorlauf", grund, StringComparison.Ordinal);
                Assert.Contains("ruecklauf", grund, StringComparison.Ordinal);
                Assert.Equal(40, wert);
            }
            finally
            {
                KiMaskenbruecke.Leeren();
                Schreibnaht.Schreibrecht = schreibrechtVorher;
            }
        }

        // ===================================================== Die Voraussetzung

        /// <summary>
        /// Der Befund beruhte darauf, dass es die Felder WIRKLICH gibt - sonst waere die
        /// Absage richtig gewesen. Dieser Fall friert das ein.
        /// </summary>
        [Fact]
        public void Die_Heizkesselmaske_fuehrt_Vorlauf_und_Ruecklauf()
        {
            KiDialog hk = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL);

            Assert.NotNull(hk);
            Assert.True(hk.KenntFeld("vorlauf"));
            Assert.True(hk.KenntFeld("ruecklauf"));
        }

        // ============== Die benannte Absage einer ausgenommenen Maske (Welle #458)

        /// <summary>
        /// Wird der Assistent aus einer Maske der AUSNAHMELISTE gerufen, sagt die Absage
        /// „bewusst nicht steuerbar" mit dem Grund — statt der Liste aller Masken.
        /// </summary>
        [Fact]
        public void Aus_einer_ausgenommenen_Maske_nennt_die_Absage_ihren_Grund()
        {
            string text = MitAufruf("Form_LizenzVerwaltung.btn_Help",
                () => Grund("feld_setzen",
                            new Dictionary<string, object> { ["feld"] = "gibtesnicht", ["wert"] = "1" }));

            Assert.Equal(KiDialogAusnahmen.Absage("Form_LizenzVerwaltung.btn_Help"), text);
            Assert.Contains(KiDialogAusnahmen.Grundtext(KiAusnahmegrund.LizenzOderSchluessel),
                            text, StringComparison.Ordinal);

            // Die Liste der steuerbaren Masken faellt weg - sie beantwortet eine andere Frage.
            Assert.DoesNotContain(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL).Anzeigename,
                                  text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Eine OFFENE Ausnahme sagt „noch nicht", nicht „bewusst nicht" — sie ist nicht
        /// ausgenommen, sondern noch nicht angebunden.
        /// </summary>
        /// <remarks>
        /// Die Liste muss keinen offenen Eintrag führen (Welle #458 hat die letzten
        /// angebunden); der Fall baut ihn deshalb selbst und nimmt denselben Satzweg wie
        /// der Hilfeschlüssel.
        /// </remarks>
        [Fact]
        public void Eine_offene_Ausnahme_sagt_noch_nicht_statt_bewusst_nicht()
        {
            string offen = KiDialogAusnahmen.AbsageFuer(
                new KiAusnahme("GibtEsNicht", KiAusnahmegrund.Offen, "Probe.", "#0", "GibtEsNicht.btn_Help"));
            string bewusst = KiDialogAusnahmen.Absage("Form_KiChat.btn_Help");
            Assert.Equal(bewusst, KiDialogAusnahmen.AbsageFuer(KiDialogAusnahmen.FuerHilfeschluessel("Form_KiChat.btn_Help")));

            Assert.NotNull(offen);
            Assert.NotNull(bewusst);
            Assert.Contains(KiDialogAusnahmen.Grundtext(KiAusnahmegrund.Offen), offen, StringComparison.Ordinal);
            Assert.NotEqual(offen.Replace(KiDialogAusnahmen.Grundtext(KiAusnahmegrund.Offen), ""),
                            bewusst.Replace(KiDialogAusnahmen.Grundtext(KiAusnahmegrund.Assistent), ""));
        }

        /// <summary>
        /// Lassen die Felder eine Maske erkennen, steht ihr Weg HINTER dem Grund — der
        /// Anwender erfährt beides: warum hier nicht, und wo sonst.
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Aus_einer_ausgenommenen_Maske_folgt_der_Weg_zur_gemeinten_Maske(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            string text = MitAufruf("Form_KiChat.btn_Help",
                () => Grund("feld_setzen",
                            new Dictionary<string, object>
                            { ["feld"] = "bereitschaftsverlust", ["wert"] = "1,5" }));

            Assert.StartsWith(KiDialogAusnahmen.Absage("Form_KiChat.btn_Help"), text, StringComparison.Ordinal);
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL_ADMIN).Anzeigename,
                            text, StringComparison.Ordinal);
            Assert.Contains("dialog_oeffnen", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die GEGENPROBEN: Ein Aufruf aus einer steuerbaren Maske, ohne Hilfeschlüssel
        /// oder mit genannter Maske ändert an der Absage nichts.
        /// </summary>
        [Fact]
        public void Ohne_Ausnahme_im_Aufruf_bleibt_die_Absage_wie_sie_war()
        {
            var werte = new Dictionary<string, object> { ["feld"] = "gibtesnicht", ["wert"] = "1" };
            string ohneAufruf = MitAufruf(null, () => Grund("feld_setzen", werte));

            Assert.Equal(ohneAufruf, MitAufruf("Form_Heizkessel.btn_Help", () => Grund("feld_setzen", werte)));
            Assert.Equal(ohneAufruf, MitAufruf("", () => Grund("feld_setzen", werte)));
            Assert.DoesNotContain(KiDialogAusnahmen.Grundtext(KiAusnahmegrund.LizenzOderSchluessel),
                                  ohneAufruf, StringComparison.Ordinal);

            // Wer die Maske NENNT, meint sie - auch aus einer ausgenommenen Maske heraus.
            var genannt = new Dictionary<string, object>
            { ["maske"] = KiMaskennamen.HEIZKESSEL, ["feld"] = "vorlauf", ["wert"] = "60" };
            string text = MitAufruf("Form_LizenzVerwaltung.btn_Help", () => Grund("feld_setzen", genannt));
            Assert.DoesNotContain(KiDialogAusnahmen.Grundtext(KiAusnahmegrund.LizenzOderSchluessel),
                                  text ?? "", StringComparison.Ordinal);
        }

        // ============================ Nur erklärte Gegenstücke (Welle #469)

        /// <summary>
        /// <b>Ein ähnlicher Schlüssel der Zielmaske ist nicht dasselbe Feld.</b> Führt die
        /// gemeinte Maske auf eine andere Katalogmaske (Pufferverwaltung → Ansicht
        /// „Simulation", Photovoltaik → Modulkatalog, Vorlagenposition → Kostenverwaltung),
        /// nennt die Absage diese nur, wenn sie das Feld unter demselben Schlüssel oder dem
        /// ERKLÄRTEN Gegenstück führt — nie über eine Anfangs- oder Teilstringsuche.
        /// </summary>
        /// <remarks>
        /// Die drei Fälle des Befunds aus dem Kulturfix (<c>ladeleistung</c> traf die
        /// <c>speicher_ladeleistung</c> der Batterie, <c>wr_wirkungsgrad</c> den
        /// Modulwirkungsgrad, <c>ersatz_fuehren</c> den <c>satz</c>) und zwei weitere
        /// derselben Regel (<c>quelltemperatur</c> traf <c>quelltemperatur_konstant</c>,
        /// <c>positionsart</c> die <c>position</c>). Jeder nennt jetzt die Maske, die das
        /// Feld führt, samt ihrem Weg.
        /// </remarks>
        [Theory]
        [InlineData("ladeleistung", KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, KiMaskennamen.SIMULATION, "de-DE")]
        [InlineData("ladeleistung", KiMaskennamen.PUFFERSPEICHER_VERWALTUNG, KiMaskennamen.SIMULATION, "en-US")]
        [InlineData("wr_wirkungsgrad", KiMaskennamen.PHOTOVOLTAIK, KiMaskennamen.PV_MODULKATALOG, "de-DE")]
        [InlineData("wr_wirkungsgrad", KiMaskennamen.PHOTOVOLTAIK, KiMaskennamen.PV_MODULKATALOG, "en-US")]
        [InlineData("ersatz_fuehren", KiMaskennamen.VORLAGENPOSITION, KiMaskennamen.KOSTENVERWALTUNG, "de-DE")]
        [InlineData("ersatz_fuehren", KiMaskennamen.VORLAGENPOSITION, KiMaskennamen.KOSTENVERWALTUNG, "en-US")]
        [InlineData("quelltemperatur", KiMaskennamen.QUELLE_PUFFERSPEICHER, KiMaskennamen.SIMULATION, "de-DE")]
        [InlineData("quelltemperatur", KiMaskennamen.QUELLE_PUFFERSPEICHER, KiMaskennamen.SIMULATION, "en-US")]
        [InlineData("positionsart", KiMaskennamen.VORLAGENPOSITION, KiMaskennamen.KOSTENVERWALTUNG, "de-DE")]
        [InlineData("positionsart", KiMaskennamen.VORLAGENPOSITION, KiMaskennamen.KOSTENVERWALTUNG, "en-US")]
        public void Ein_aehnlicher_Schluessel_der_Zielmaske_ist_nicht_dasselbe_Feld(
            string feld, string traeger, string zielmaske, string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            // Vorbedingung: Die tragende Maske führt das Feld, sie führt auf die Zielmaske,
            // und die Zielmaske führt es NICHT - nur einen ähnlich benannten Schlüssel.
            KiDialog eigene = KiDialoge.Katalog.Finde(traeger);
            KiDialog ziel = KiDialoge.Katalog.Finde(zielmaske);
            Assert.True(eigene.KenntFeld(feld), traeger + " führt " + feld + " nicht mehr.");
            Assert.Equal(zielmaske, KiDialoge.Katalog.Finde(KiMaskenziele.Ziel(traeger))?.Maskenname);
            Assert.False(ziel.KenntFeld(KiDialoge.Zielfeldname(traeger, feld)),
                         zielmaske + " führt " + feld + " - der Fall hat seinen Gegenstand verloren.");

            Assert.Equal(traeger, KiAktionenDialog.GemeinteMaske("", new[] { feld })?.Maskenname);

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = feld, ["wert"] = "1" });

            Assert.Contains("dialog_oeffnen (" + traeger + ")", text, StringComparison.Ordinal);
            Assert.DoesNotContain("(" + zielmaske + ")", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Gegenprobe der erklärten Vorsilbe: Ein Feld des Aufklappers „Alle Daten"
        /// (<c>katalog_breite</c>) steht im Modulkatalog unter dem Profilschlüssel
        /// (<c>breite</c>) — die Absage nennt weiter den Modulkatalog.
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Ein_Feld_aus_Alle_Daten_fuehrt_weiter_in_den_Modulkatalog(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            Assert.Equal("breite", KiDialoge.Zielfeldname(KiMaskennamen.PHOTOVOLTAIK, "katalog_breite"));
            Assert.Equal(KiMaskennamen.PV_MODULKATALOG,
                         KiAktionenDialog.GemeinteMaske("", new[] { "katalog_breite" })?.Maskenname);
        }

        /// <summary>
        /// <b>Wächter über den ganzen Katalog:</b> Die Maske, die die Absage für einen
        /// Feldschlüssel nennt, führt ihn — unter demselben Schlüssel oder unter dem
        /// erklärten Gegenstück einer Maske, die ihn führt (<see cref="KiDialoge.Zielfeldname"/>).
        /// Eine Absage, die in eine Maske ohne das Feld schickt, ist schlimmer als die Liste.
        /// </summary>
        [Fact]
        public void Die_genannte_Maske_fuehrt_das_Feld_unter_seinem_Schluessel_oder_dem_erklaerten()
        {
            var traeger = new SortedDictionary<string, List<KiDialog>>(StringComparer.Ordinal);
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                foreach (KiDialogFeld f in d.Felder)
                {
                    if (!traeger.TryGetValue(f.Name, out List<KiDialog> liste))
                        traeger[f.Name] = liste = new List<KiDialog>();
                    liste.Add(d);
                }

            var falsch = new List<string>();
            foreach (KeyValuePair<string, List<KiDialog>> t in traeger)
            {
                KiDialog genannt = KiAktionenDialog.GemeinteMaske("", new[] { t.Key });
                if (genannt == null || genannt.KenntFeld(t.Key)) continue;

                bool erklaert = false;
                foreach (KiDialog d in t.Value)
                    if (genannt.KenntFeld(KiDialoge.Zielfeldname(d.Maskenname, t.Key))) erklaert = true;

                if (!erklaert) falsch.Add(t.Key + " → " + genannt.Maskenname);
            }

            Assert.True(falsch.Count == 0,
                "Diese Feldschlüssel schickt die Absage in eine Maske, die sie nicht führt:\n" +
                string.Join("\n", falsch));
        }

        // ============================ Die beste Stufe entscheidet (Welle #472)

        /// <summary>
        /// <b>Über alle Masken entscheidet die beste Stufe der Namensregel.</b> Ein
        /// Anzeigename trifft die Maske, die ihn trägt - nicht eine andere, in der er nur
        /// als Wortanfang oder enthaltener Teil vorkommt.
        /// </summary>
        /// <remarks>
        /// Die vier Fälle des Befunds aus #469 und drei derselben Regel. Vorher: „With PV
        /// surplus" → Simulation (enthält „PV surplus"), „Position ist ein Erlös" →
        /// Kostenverwaltung („Position"), „Außenwand" → Wirtschaftlichkeitsseite (Spalte
        /// „A"), „Fensterfläche Ost + West" → Quelle Erdreich („Fläche"). Die Gebäudefälle
        /// nennen die Verwaltung: Katalogmaske und Verwaltung führen an denselben Ort.
        /// </remarks>
        [Theory]
        [InlineData("With PV surplus", KiMaskennamen.WAERMESENKE, "en-US")]
        [InlineData("With PV surplus:", KiMaskennamen.WAERMESENKE, "en-US")]
        [InlineData("Position ist ein Erlös", KiMaskennamen.VORLAGENPOSITION, "de-DE")]
        [InlineData("Position is a revenue", KiMaskennamen.VORLAGENPOSITION, "en-US")]
        [InlineData("Außenwand", KiMaskennamen.GEBAEUDE_ADMIN, "de-DE")]
        [InlineData("Fensterfläche Ost + West", KiMaskennamen.GEBAEUDE_ADMIN, "de-DE")]
        [InlineData("Position ist ein Zuschuss", KiMaskennamen.CASE_EINGABE, "de-DE")]
        [InlineData("eigene Einspeisehöhe", KiMaskennamen.WAERMESENKE, "de-DE")]
        public void Ein_Anzeigename_meint_die_Maske_die_ihn_traegt(string genannt, string maske, string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            Assert.Equal(maske, KiAktionenDialog.GemeinteMaske("", new[] { genannt })?.Maskenname);

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = genannt, ["wert"] = "1" });
            Assert.Contains("dialog_oeffnen (" + maske + ")", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Ist die beste Stufe mehrdeutig, wird benannt abgesagt</b> - mit den Masken
        /// dieser Stufe, nicht mit der Liste aller und nicht mit einer geratenen.
        /// </summary>
        /// <remarks>
        /// „Operating cost" beginnt die Betriebskosten der Stromspeicherauslegung und die
        /// Preissteigerung der Betriebskosten in den Wirtschaftlichkeitsparametern; beide
        /// auf der Stufe Wortanfang. Vorher nannte die Absage die Parameter allein.
        /// </remarks>
        [Theory]
        [InlineData("Operating cost", "en-US", KiMaskennamen.STROMSPEICHER_AUSLEGUNG, KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER)]
        [InlineData("Energy tax relief", "en-US", KiMaskennamen.BHKW_WIRTSCHAFTLICHKEIT, KiMaskennamen.ENERGIETRAEGER)]
        public void Eine_mehrdeutige_beste_Stufe_nennt_ihre_Kandidaten(string genannt, string kultur,
                                                                       string eine, string andere)
        {
            using var k = new Kulturvorrichtung(kultur);

            Assert.Null(KiAktionenDialog.GemeinteMaske("", new[] { genannt }, out IReadOnlyList<KiDialog> kandidaten));

            var namen = new List<string>();
            foreach (KiDialog d in kandidaten) namen.Add(d.Maskenname);
            Assert.Contains(eine, namen);
            Assert.Contains(andere, namen);

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = genannt, ["wert"] = "1" });
            Assert.DoesNotContain("dialog_oeffnen (", text, StringComparison.Ordinal);
            Assert.Contains("(" + eine + ")", text, StringComparison.Ordinal);
            Assert.Contains("(" + andere + ")", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Wächter über alle Anzeigenamen (de-DE, en-US):</b> Die gemeinte Maske eines
        /// Anzeigenamens - so wie er auf der Maske steht, und ohne den Doppelpunkt - ist
        /// keine oder eine, die ihn führt: die Maske des Feldes, ihre Verwaltung (sofern sie
        /// das Feld unter seinem Schlüssel führt) oder eine Maske, deren Feldschlüssel der
        /// Name selbst ist.
        /// </summary>
        /// <remarks>
        /// Die dritte Möglichkeit ist kein Raten: „Bezugspreis" steht als Beschriftung in
        /// der Stromspeicherauslegung („Bezugspreis:") und als Schlüssel <c>bezugspreis</c>
        /// im Peak-Shaving - der gleichnamige Schlüssel ist der genauere Treffer.
        /// </remarks>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Kein_Anzeigename_fuehrt_in_eine_fremde_Maske(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            var traeger = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                foreach (KiDialogFeld f in d.Felder)
                {
                    string roh = f.Anzeigename ?? "";
                    foreach (string name in new[] { roh.Trim(), roh.Trim().TrimEnd(':').Trim() })
                    {
                        if (name.Length == 0) continue;
                        if (!traeger.TryGetValue(name, out HashSet<string> menge))
                            traeger[name] = menge = new HashSet<string>(StringComparer.Ordinal);
                        menge.Add(d.Maskenname);

                        KiDialog ziel = KiDialoge.Katalog.Finde(KiMaskenziele.Ziel(d.Maskenname));
                        if (ziel != null && ziel.KenntFeld(KiDialoge.Zielfeldname(d.Maskenname, f.Name)))
                            menge.Add(ziel.Maskenname);
                    }
                }

            var fremd = new List<string>();
            foreach (KeyValuePair<string, HashSet<string>> t in traeger)
            {
                KiDialog gemeint = KiAktionenDialog.GemeinteMaske("", new[] { t.Key });
                if (gemeint == null || t.Value.Contains(gemeint.Maskenname)) continue;

                bool schluessel = false;
                foreach (KiDialogFeld f in gemeint.Felder)
                    if (KiWahl.Falte(f.Name) == KiWahl.Falte(t.Key)) schluessel = true;

                if (!schluessel) fremd.Add("„" + t.Key + "“ (" + string.Join(", ", t.Value) + ") → " + gemeint.Maskenname);
            }

            Assert.True(fremd.Count == 0,
                "Diese Anzeigenamen schickt die Absage in eine fremde Maske:\n" + string.Join("\n", fremd));
        }

        /// <summary>Führt <paramref name="aktion"/> unter einem Aufruf aus und räumt ihn danach weg.</summary>
        private static string MitAufruf(string hilfeschluessel, Func<string> aktion)
        {
            KiChatKontext.AufrufMelden(hilfeschluessel == null
                                           ? null
                                           : KiAufrufkontext.AusHilfeschluessel(hilfeschluessel));
            try { return aktion(); }
            finally { KiChatKontext.AufrufMelden(null); }
        }
    }
}
