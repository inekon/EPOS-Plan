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
    /// </remarks>
    public class KiMaskenwegTests
    {
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
        /// <b>Die zwei Felder sind <c>th_leistung</c> und <c>bereitschaftsverlust</c>.</b>
        /// Sie stehen an genau EINER Maske, und nur dann darf die Absage sie zuordnen.
        /// <c>vorlauf</c> und <c>ruecklauf</c> taugen dafür nicht mehr: Mit der Freigabe
        /// der Erzeugermasken des Projekts führen sie fünf Masken (Kessel im Katalog und
        /// im Projekt, BHKW, Solarkollektoren, Wärmepumpen-Anlage). Was dann geschieht,
        /// hält <see cref="Ein_mehrdeutiges_Feld_wird_nicht_geraten"/> fest.
        /// </remarks>
        [Fact]
        public void Die_Absage_nennt_die_Maske_zu_den_Feldern()
        {
            string text = Grund("formular_ausfuellen",
                new Dictionary<string, object>
                { ["werte"] = "th_leistung=120; bereitschaftsverlust=1,5" });

            Assert.NotNull(text);

            // Der ANZEIGENAME der Heizkesselmaske steht im Satz ...
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL).Anzeigename,
                            text, StringComparison.Ordinal);

            // ... und der Weg dorthin wird benannt.
            Assert.Contains("dialog_oeffnen", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Auch_feld_setzen_bekommt_den_Weg_genannt()
        {
            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = "th_leistung", ["wert"] = "120" });

            Assert.NotNull(text);
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL).Anzeigename,
                            text, StringComparison.Ordinal);
            Assert.Contains("dialog_oeffnen", text, StringComparison.Ordinal);
        }

        // ===================================================== Die Grenzen

        /// <summary>
        /// <b>Mehrdeutig heisst schweigen.</b> <c>schritt</c> steht in mehr als einer
        /// Katalogmaske (Stromspeicher-Auslegung und Simulation); dann darf die Absage
        /// keine davon nennen, sondern faellt auf die Liste zurueck.
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
            Assert.DoesNotContain("dialog_oeffnen", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Der Vorlauf ist ein mehrdeutiges Feld</b> — er steht an fuenf Masken der
        /// Erzeugerfamilien. Die Absage nennt deshalb keine von ihnen, sondern die
        /// Liste; welche gemeint ist, sagt erst die OFFENE Maske.
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
            Assert.DoesNotContain("dialog_oeffnen", text, StringComparison.Ordinal);

            // Die Liste nennt weiterhin ANZEIGENAMEN und keine Typnamen.
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
    }
}
