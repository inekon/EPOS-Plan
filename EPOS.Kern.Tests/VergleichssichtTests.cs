using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KONZEPT § 2.15 — die Vergleichssicht der Ergebnisansicht.</b> Alle Varianten
    /// gegen die Referenz (Sicht 1, der Bestand) oder zwei Stände A und B (Sicht 2).
    ///
    /// <para>Geprüft werden die Regeln der Bedienung und ihre Randfälle: Vorbelegung
    /// beim Wechsel, A ≠ B, Tausch, Sperre bei nur einem Stand, Rückfall bei
    /// abgehakter Wahl und Zurücksetzen beim Gruppenwechsel. Die Wahl liegt in der
    /// SITZUNG (VG‑Q3) — deshalb braucht kein Fall die Datenbank.</para>
    /// </summary>
    public class VergleichssichtTests
    {
        private static readonly List<int> GRUPPE = new List<int> { 1030, 1031, 1032 };
        private const int STAMM = 1030;

        // =================================================================
        // Die Sicht selbst
        // =================================================================

        /// <summary>Vorgabe ist Sicht 1, und sie gibt die Gruppenreferenz frei (0).</summary>
        [Fact]
        public void Vorgabe_ist_Sicht_1()
        {
            var s = new Vergleichssicht();

            Assert.Equal(Vergleichssicht.ALLE, s.Sicht);
            Assert.False(s.IstPaar);
            Assert.Equal(0, s.Referenz);
            Assert.Equal(GRUPPE, s.Spalten(GRUPPE));
        }

        /// <summary>In Sicht 2 ist A die Referenz, und die Spalten sind genau A und B.</summary>
        [Fact]
        public void In_Sicht_2_ist_A_die_Referenz()
        {
            var s = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = 1031, IdB = 1032 };

            Assert.True(s.IstPaar);
            Assert.Equal(1031, s.Referenz);
            Assert.Equal(new[] { 1031, 1032 }, s.Spalten(GRUPPE));
        }

        /// <summary>Eine Paarwahl mit demselben Stand zweimal ist keine — sie wäre eine
        /// Differenz gegen sich selbst.</summary>
        [Fact]
        public void Ein_Stand_zweimal_ist_keine_Paarwahl()
        {
            var s = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = 1031, IdB = 1031 };

            Assert.False(s.IstPaar);
            Assert.Equal(0, s.Referenz);
        }

        // =================================================================
        // Die Sitzungswahl
        // =================================================================

        /// <summary>
        /// Beim Wechsel in Sicht 2 ist A mit der REFERENZ DER GRUPPE vorbelegt und B
        /// mit der ersten anderen Variante in Listenreihenfolge.
        /// </summary>
        [Fact]
        public void Der_Wechsel_in_Sicht_2_belegt_A_und_B_vor()
        {
            var w = new Vergleichsauswahl();
            Assert.True(w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 0, STAMM));

            Assert.True(w.Sicht.IstPaar);
            Assert.Equal(STAMM, w.Sicht.IdA);
            Assert.Equal(1031, w.Sicht.IdB);
        }

        /// <summary>Ist eine Variante die Gruppenreferenz (§ 2.9), belegt sie A vor.</summary>
        [Fact]
        public void Die_Gruppenreferenz_belegt_A_vor()
        {
            var w = new Vergleichsauswahl();
            w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 1032, STAMM);

            Assert.Equal(1032, w.Sicht.IdA);
            Assert.Equal(STAMM, w.Sicht.IdB);
        }

        /// <summary>
        /// RANDFALL: Eine Gruppe mit nur dem Stamm sperrt Sicht 2 — die Wahl bleibt
        /// Sicht 1, ohne Meldung.
        /// </summary>
        [Fact]
        public void Eine_Gruppe_mit_einem_Stand_sperrt_Sicht_2()
        {
            var einer = new List<int> { STAMM };
            Assert.False(Vergleichsauswahl.PaarMoeglich(einer));

            var w = new Vergleichsauswahl();
            w.SichtWaehlen(Vergleichssicht.PAAR, einer, 0, STAMM);

            Assert.Equal(Vergleichssicht.ALLE, w.Sicht.Sicht);
        }

        /// <summary>A ≠ B ist gesichert: Dieselbe Id zweimal wird nicht übernommen.</summary>
        [Fact]
        public void A_und_B_sind_nie_derselbe_Stand()
        {
            var w = new Vergleichsauswahl();
            w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 0, STAMM);

            Assert.False(w.PaarWaehlen(1031, 1031));
            Assert.Equal(STAMM, w.Sicht.IdA);
            Assert.Equal(1031, w.Sicht.IdB);

            Assert.True(w.PaarWaehlen(1031, 1032));
            Assert.Equal(1031, w.Sicht.IdA);
            Assert.Equal(1032, w.Sicht.IdB);
        }

        /// <summary>Der Tauschknopf (VG‑Q7) dreht A und B — und damit das Vorzeichen.</summary>
        [Fact]
        public void Der_Tausch_dreht_A_und_B()
        {
            var w = new Vergleichsauswahl();
            w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 0, STAMM);
            int a = w.Sicht.IdA, b = w.Sicht.IdB;

            Assert.True(w.Tauschen());
            Assert.Equal(b, w.Sicht.IdA);
            Assert.Equal(a, w.Sicht.IdB);
        }

        /// <summary>
        /// RANDFALL: Wird A oder B abgehakt, fällt die Ansicht auf Sicht 1 zurück — und
        /// die Warnzeile benennt den Rückfall. Ein stiller Rückfall wäre eine andere
        /// Tafel ohne Auskunft.
        /// </summary>
        [Fact]
        public void Eine_abgehakte_Wahl_faellt_benannt_auf_Sicht_1_zurueck()
        {
            var w = new Vergleichsauswahl();
            w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 0, STAMM);
            w.PaarWaehlen(1031, 1032);

            Assert.Null(w.Nachziehen(GRUPPE));

            string warnung = w.Nachziehen(new List<int> { STAMM, 1032 });
            Assert.False(string.IsNullOrEmpty(warnung));
            Assert.Equal(Vergleichssicht.ALLE, w.Sicht.Sicht);
            Assert.Null(w.Nachziehen(new List<int> { STAMM, 1032 }));
        }

        /// <summary>Beim Wechsel der Vergleichsgruppe gilt wieder Sicht 1.</summary>
        [Fact]
        public void Der_Gruppenwechsel_setzt_auf_Sicht_1_zurueck()
        {
            var w = new Vergleichsauswahl();
            w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 0, STAMM);

            w.GruppeGewechselt();

            Assert.Equal(Vergleichssicht.ALLE, w.Sicht.Sicht);
            Assert.Equal(0, w.Sicht.IdA);
            Assert.Equal(0, w.Sicht.IdB);
        }

        /// <summary>Jede Änderung meldet sich — die Seite frischt daran auf.</summary>
        [Fact]
        public void Jede_Aenderung_meldet_sich()
        {
            var w = new Vergleichsauswahl();
            int meldungen = 0;
            w.Geaendert += () => meldungen++;

            w.SichtWaehlen(Vergleichssicht.PAAR, GRUPPE, 0, STAMM);
            w.PaarWaehlen(1031, 1032);
            w.Tauschen();
            w.GruppeGewechselt();

            Assert.Equal(4, meldungen);
        }
    }
}
