using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Neu berechnen" auf dem Kosten-Reiter</b> (Anwenderauftrag: Die Energiekosten der
    /// Kostenseite blieben auf dem Stand eines alten Laufs stehen, bis im Reiter
    /// Wirtschaftlichkeit neu gerechnet wurde).
    ///
    /// <para>Die Rahmenhülle <c>BerichteKostenHuelle</c> reicht der Kostenseite den Rechenweg
    /// DERSELBEN Wirtschaftlichkeitshülle der Gruppe — keine zweite Rechnung. Ohne Rahmen oder
    /// ohne Gruppe bleibt der Parameter aus dem Satz, und die Seite zeichnet keinen Knopf. Die
    /// Stände des Laufs sind die gewählten Versionen ohne Stamm, dazu die wirksame Referenz der
    /// Gruppe, die auf der Wirtschaftlichkeitsseite nicht abwählbar ist.</para>
    ///
    /// <para>Geschrieben wird nur an einer Arbeitskopie der Testdatenbank
    /// (<see cref="TestDatenbank"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KostenNeuBerechnenHuelleTests : IDisposable
    {
        /// <summary>Vergleichsgruppe mit gespeicherten Ergebnissen: Stamm 1019, Varianten „Test1“ (1023) und „Test2“ (1024).</summary>
        private const int GRUPPE = 1019;
        private const int TEST1 = 1023;
        private const int TEST2 = 1024;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private static Func<string, IReadOnlyDictionary<string, object>> Seiten(BerichteKostenHuelle huelle)
            => (Func<string, IReadOnlyDictionary<string, object>>)huelle.Gaben()["SeitenGaben"];

        /// <summary>Die Hülle mit der Gruppe 1019 — wie beim Öffnen des Reiters über die Übersicht.</summary>
        private static BerichteKostenHuelle GruppeGeladen()
        {
            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(GRUPPE, "");
            ((Func<UebersichtStand>)Seiten(huelle)(BerichteKostenSeite.SEITE_UEBERSICHT)["Laden"])();
            return huelle;
        }

        // =====================================================================
        //  Kein Delegat, kein Knopf
        // =====================================================================

        [Fact]
        public void Ohne_Stammprojekt_traegt_die_Kostenseite_keinen_Rechenweg()
        {
            IReadOnlyDictionary<string, object> kosten =
                Seiten(new BerichteKostenHuelle())(BerichteKostenSeite.SEITE_KOSTEN);

            Assert.False(kosten.ContainsKey("Berechnen"));
            Assert.False(kosten.ContainsKey("Abbrechen"));
        }

        /// <summary>Eine Kostenseite ohne Rahmenhülle (Prüfstand) bekommt den Knopf auch mit Gruppe nicht.</summary>
        [Fact]
        public void Ohne_Rahmenhuelle_traegt_die_Kostenseite_keinen_Rechenweg()
        {
            var kosten = new KostenSeiteGaben();
            kosten.SetzeGruppe(GRUPPE, "");
            kosten.SetzeProjekt(GRUPPE, "");

            Assert.False(kosten.Gaben().ContainsKey("Berechnen"));
        }

        [Fact]
        public void Mit_Gruppe_bekommt_die_Kostenseite_Rechenweg_und_Abbrechen()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyDictionary<string, object> kosten =
                Seiten(GruppeGeladen())(BerichteKostenSeite.SEITE_KOSTEN);

            Assert.IsType<Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>>>(kosten["Berechnen"]);
            Assert.IsType<Action>(kosten["Abbrechen"]);
        }

        // =====================================================================
        //  Derselbe Lauf wie auf der Wirtschaftlichkeitsseite
        // =====================================================================

        /// <summary>
        /// Der Knopf rechnet über die Wirtschaftlichkeitshülle der Gruppe: frisch gespeicherte
        /// Ergebnisse je Stand, passend zum Simulationslauf, der Kurzstand der Reiterzeile wird
        /// aufgefrischt, die Kostenseite liest danach „Stand: …" des Laufs ohne Band — und die
        /// Wirtschaftlichkeitsseite behält dieselbe Hülle.
        /// </summary>
        [Fact]
        public async Task Neu_berechnen_rechnet_und_speichert_ueber_die_Wirtschaftlichkeitshuelle()
        {
            if (!_db.Vorhanden) return;
            BerichteKostenHuelle huelle = GruppeGeladen();
            var zustand = (SeitenZustand)huelle.Gaben()[SeitenZustand.PARAMETER];
            int gemeldet = 0;
            zustand.KurzstandGeaendert += () => gemeldet++;

            object wirtschaftVorher =
                ((Delegate)Seiten(huelle)(BerichteKostenSeite.SEITE_WIRTSCHAFT)["Berechnen"]).Target;

            IReadOnlyDictionary<string, object> kosten = Seiten(huelle)(BerichteKostenSeite.SEITE_KOSTEN);
            var berechnen = (Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>>)kosten["Berechnen"];
            DateTime vorher = DateTime.Now.AddMinutes(-1);

            LaufErgebnis erg = await berechnen(new List<int> { TEST1, TEST2 }, _ => { });

            Assert.True(erg.Erfolg, erg.Fehler);
            Assert.True(gemeldet >= 1, "Der Kurzstand der Reiterzeile wurde nicht aufgefrischt.");

            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> gespeichert =
                ctrl.LadeErgebnisse(new List<int> { GRUPPE, TEST1, TEST2 });
            foreach (int id in new[] { GRUPPE, TEST1, TEST2 })
            {
                WirtschaftlichkeitErgebnis e = gespeichert.First(
                    x => x.IdProjekt == id && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                Assert.True(e.Zeitstempel >= vorher, id + ": " + e.Zeitstempel.ToString("O"));
                Assert.True(ctrl.ErgebnisAktuell(e), id + ": passt nicht zum Simulationslauf");
            }

            KostenStand stand = ((Func<KostenStand>)kosten["Laden"])();
            Assert.False(stand.Nachrechnen);
            WirtschaftlichkeitErgebnis stamm = gespeichert.First(
                x => x.IdProjekt == GRUPPE && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Contains(string.Format(R.BK_KOSTEN_STAND, stamm.Zeitstempel.ToString("dd.MM.yyyy HH:mm")),
                            stand.Statuszeile, StringComparison.Ordinal);

            object wirtschaftNachher =
                ((Delegate)Seiten(huelle)(BerichteKostenSeite.SEITE_WIRTSCHAFT)["Berechnen"]).Target;
            Assert.Same(wirtschaftVorher, wirtschaftNachher);
        }

        // =====================================================================
        //  Das Band „… bitte neu berechnen"
        // =====================================================================

        /// <summary>
        /// Das Band folgt DERSELBEN Frage wie die Wirtschaftlichkeitsseite
        /// (<see cref="WirtschaftlichkeitCtrl.ErgebnisAktuell"/>) — und nur für Stände, deren
        /// Energiekosten die Seite zeigt: das Projekt der Kacheln und die Versionen im Vergleich.
        /// </summary>
        [Fact]
        public void Das_Band_folgt_der_Frage_der_Wirtschaftlichkeit_fuer_die_angezeigten_Staende()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyDictionary<string, object> kosten =
                Seiten(GruppeGeladen())(BerichteKostenSeite.SEITE_KOSTEN);
            var laden = (Func<KostenStand>)kosten["Laden"];

            Assert.False(laden().Nachrechnen, "Die gesäten Ergebnisse passen zu ihrem Simulationslauf.");

            // Ein jüngerer Simulationslauf von „Test2“: Sein gespeichertes Ergebnis passt nicht mehr.
            DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_Ergebnis (ID_Projekt, Bezeichner, Zeitstempel) VALUES (?, ?, ?)",
                new[]
                {
                    new DbParam("@p", TEST2), new DbParam("@b", "Probe Kosten neu berechnen"),
                    new DbParam("@z", "2026-09-30 10:00:00")
                });
            Assert.True(laden().Nachrechnen);

            // Abgewählt steht „Test2“ in keiner Zahl der Seite — dann ist sein Lauf kein Anlass.
            ((Action<IReadOnlyList<int>>)kosten["VergleichGewaehlt"])(new List<int> { GRUPPE, TEST1 });
            Assert.False(laden().Nachrechnen);
        }

        /// <summary>
        /// Die Stände des Laufs: ohne Stamm, die wirksame Referenz der Gruppe kommt dazu, auch
        /// wenn sie abgewählt ist — wie auf der Wirtschaftlichkeitsseite, wo sie nicht abwählbar ist.
        /// </summary>
        [Fact]
        public void Die_abgewaehlte_Referenz_rechnet_mit_und_der_Stamm_faellt_heraus()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(GRUPPE);
            var wirt = new WirtschaftlichkeitSeiteGaben(GRUPPE, "");

            p.IdReferenzprojekt = TEST1;
            Assert.True(ctrl.SpeichereParameter(p), ctrl.Speicherfehler);
            Assert.Equal(new[] { TEST2, TEST1 }, wirt.MitReferenz(new[] { GRUPPE, TEST2 }));
            Assert.Equal(new[] { TEST1 }, wirt.MitReferenz(new[] { TEST1 }));
            Assert.Equal(new[] { TEST1 }, wirt.MitReferenz(Array.Empty<int>()));

            // Die Referenz ist der Stamm: Es kommt nichts dazu.
            p.IdReferenzprojekt = 0;
            Assert.True(ctrl.SpeichereParameter(p), ctrl.Speicherfehler);
            Assert.Equal(new[] { TEST2 }, wirt.MitReferenz(new[] { GRUPPE, TEST2 }));
            Assert.Empty(wirt.MitReferenz(null));
        }
    }
}
