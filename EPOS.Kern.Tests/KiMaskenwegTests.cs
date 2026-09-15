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
        /// Der Fall aus dem Befund, wörtlich: zwei Felder der Heizkesselmaske, keine
        /// Maske offen.
        /// </summary>
        [Fact]
        public void Die_Absage_nennt_die_Maske_zu_den_Feldern()
        {
            string text = Grund("formular_ausfuellen",
                new Dictionary<string, object> { ["werte"] = "vorlauf=65; ruecklauf=55" });

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
                new Dictionary<string, object> { ["feld"] = "vorlauf", ["wert"] = "65" });

            Assert.NotNull(text);
            Assert.Contains(KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL).Anzeigename,
                            text, StringComparison.Ordinal);
        }

        // ===================================================== Die Grenzen

        /// <summary>
        /// <b>Mehrdeutig heisst schweigen.</b> <c>nutzungsdauer</c> steht in mehr als
        /// einer Katalogmaske; dann darf die Absage keine davon nennen, sondern faellt auf
        /// die Liste zurueck.
        /// </summary>
        [Fact]
        public void Ein_mehrdeutiges_Feld_wird_nicht_geraten()
        {
            // Vorbedingung des Falles: Das Feld steht wirklich mehrfach im Katalog.
            int masken = 0;
            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                if (d.KenntFeld("nutzungsdauer")) masken++;
            Assert.True(masken > 1, "Der Fall braucht ein Feld in mehreren Masken.");

            string text = Grund("feld_setzen",
                new Dictionary<string, object> { ["feld"] = "nutzungsdauer", ["wert"] = "15" });

            Assert.NotNull(text);
            Assert.DoesNotContain("dialog_oeffnen", text, StringComparison.Ordinal);
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
