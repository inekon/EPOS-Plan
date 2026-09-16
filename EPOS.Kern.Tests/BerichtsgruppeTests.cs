using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// DER ANWENDERBEFUND VOM 16.09.2026, als Prüfung: Im Bereich „Berichte &amp;
    /// Kosten" zeigte die Seite „Kosten" nach dem Weg Stammprojekt → Variante →
    /// Stammprojekt „Kein Projekt gewählt." — leere Kacheln, leere Tabellen,
    /// gesperrte Knöpfe. Erst das Verlassen und erneute Betreten des Bereichs
    /// brachte die Kosten des Stammprojekts zurück.
    ///
    /// <para><b>Zwei Regeln waren verletzt</b>, beide hüte nun
    /// <see cref="Berichtsgruppe"/>: Der Rückwechsel auf ein Stammprojekt markierte
    /// nichts (nur eine Variante markierte sich selbst), und die Kostenseite ließ
    /// sich die Rettung „dann eben das Stammprojekt" eine Zeile später von genau
    /// dieser leeren Markierung wieder überschreiben.</para>
    ///
    /// <para>Geprüft wird die Klasse und nicht die Windows-Hülle, weil die Schale
    /// kein Testprojekt führt (<c>WP-Plan.Kern.slnf</c> sieht sie nicht) — deshalb
    /// steht die Entscheidung überhaupt hier und nicht mehr dort.</para>
    /// </summary>
    public class BerichtsgruppeTests
    {
        private const int Stamm = 1030;
        private const int Variante = 1047;
        private const string StammName = "Booster-Kette mit Kombi-Speicher";
        private const string VariantenName = "Booster-Kette mit Kombi-Speicher - Schichtspeicher";

        /// <summary>Der Weg des Anwenders: Stamm öffnen, Variante wählen, zurück auf den Stamm.</summary>
        private static Berichtsgruppe Gruppe()
        {
            var g = new Berichtsgruppe();
            g.StammSetzen(Stamm, StammName);
            return g;
        }

        [Fact]
        public void Der_Rueckwechsel_auf_das_Stammprojekt_laesst_die_Kostenseite_nicht_leer()
        {
            Berichtsgruppe g = Gruppe();

            // 1. Das Stammprojekt ist offen.
            Assert.Equal(Stamm, g.KontextGewechselt(Stamm, StammName, 0));
            Assert.Equal(Stamm, g.KostenId);
            Assert.Equal(StammName, g.KostenName);

            // 2. Die Variante wird zum aktiven Projekt - ihr Stamm bleibt die Gruppe.
            Assert.Equal(Stamm, g.KontextGewechselt(Variante, VariantenName, Stamm));
            Assert.Equal(Variante, g.KostenId);
            Assert.Equal(VariantenName, g.KostenName);

            // 3. Zurueck auf das Stammprojekt - HIER stand "Kein Projekt gewaehlt.".
            Assert.Equal(Stamm, g.KontextGewechselt(Stamm, StammName, 0));
            Assert.Equal(Stamm, g.KostenId);
            Assert.Equal(StammName, g.KostenName);
        }

        [Fact]
        public void Der_Kontext_markiert_sich_selbst_gleich_ob_Stamm_oder_Variante()
        {
            Berichtsgruppe g = Gruppe();

            g.KontextGewechselt(Variante, VariantenName, Stamm);
            Assert.Equal(Variante, g.IdMarkiert);
            Assert.Equal(VariantenName, g.NameMarkiert);

            g.KontextGewechselt(Stamm, StammName, 0);
            Assert.Equal(Stamm, g.IdMarkiert);
            Assert.Equal(StammName, g.NameMarkiert);
        }

        [Fact]
        public void Ohne_Markierung_zeigt_die_Kostenseite_das_Stammprojekt()
        {
            Berichtsgruppe g = Gruppe();
            g.MarkierungVerwerfen();

            Assert.Equal(Berichtsgruppe.KEINS, g.IdMarkiert);
            Assert.Equal(Stamm, g.KostenId);
            Assert.Equal(StammName, g.KostenName);
        }

        [Fact]
        public void Ohne_Stammprojekt_und_ohne_Markierung_gibt_es_wirklich_kein_Projekt()
        {
            var g = new Berichtsgruppe();

            Assert.Equal(0, g.KontextGewechselt(0, "", 0));
            Assert.Equal(0, g.KostenId);
            Assert.Equal("", g.KostenName);
        }

        [Fact]
        public void Ein_anderes_Stammprojekt_verwirft_die_Markierung_der_alten_Gruppe()
        {
            Berichtsgruppe g = Gruppe();
            g.KontextGewechselt(Variante, VariantenName, Stamm);

            g.StammGewaehlt(2000, "Fremde Gruppe");

            Assert.Equal(Berichtsgruppe.KEINS, g.IdMarkiert);
            Assert.Equal(2000, g.KostenId);
            Assert.Equal("Fremde Gruppe", g.KostenName);
        }

        [Fact]
        public void Die_Vorauswahl_des_Stammprojekts_laesst_die_Markierung_der_Gruppe_stehen()
        {
            Berichtsgruppe g = Gruppe();
            g.KontextGewechselt(Variante, VariantenName, Stamm);

            // Das Laden der Liste bestaetigt denselben Stamm - die Markierung bleibt.
            g.StammSetzen(Stamm, StammName);

            Assert.Equal(Variante, g.IdMarkiert);
            Assert.Equal(Variante, g.KostenId);
        }

        [Fact]
        public void Die_Markierung_ohne_Namen_holt_ihn_beim_naechsten_Laden_nach()
        {
            Berichtsgruppe g = Gruppe();
            g.KontextGewechselt(Stamm, StammName, 0);

            // Der Klick auf eine Listenzeile kennt nur die Id ...
            g.MarkierungSetzen(Variante);
            Assert.Equal(Variante, g.IdMarkiert);

            // ... das Laden der Liste traegt den Namen nach.
            g.Markieren(Variante, VariantenName);
            Assert.Equal(VariantenName, g.NameMarkiert);
            Assert.Equal(VariantenName, g.KostenName);
        }
    }
}
