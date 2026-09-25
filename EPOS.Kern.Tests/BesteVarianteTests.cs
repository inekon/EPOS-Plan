using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Regel der besten Variante</b> (Konzept Berichtsvorlagen 5.1 und 9.5, Etappe BV-E3)
    /// — mit synthetischen Ergebnissen, ohne Datenbank. Die Regel steht in
    /// <see cref="BesteVariante"/>; die Kennzahlkarten der Wirtschaftlichkeitsseite zeigen den
    /// Stand, den sie wählt, und der Bericht füllt daraus <c>wirtschaft.beste.*</c> (BV-E4).
    ///
    /// <para><b>Die Fälle.</b> Stamm ohne Varianten; eine Variante besser und eine schlechter
    /// als der Stamm; die größte Differenz; Gleichstand; Stand ohne Ergebnis; nur der Stamm mit
    /// Ergebnis; gar kein Ergebnis; die Szenariowahl; der Stamm mit Differenz; nur die
    /// übergebenen Stände; je Stand das erste Ergebnis; der Merker entscheidet. Die Kennungen
    /// sind synthetisch (Muster <see cref="Berichtsdatenproben"/>) und stehen in keiner
    /// Datenbank.</para>
    /// </summary>
    public class BesteVarianteTests
    {
        private const int STAMM = 9301;
        private const int VARIANTE_A = 9302;
        private const int VARIANTE_B = 9303;
        private const int VARIANTE_C = 9304;

        private const string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private const string BEST = WirtschaftlichkeitSzenario.BEST;
        private const string WORST = WirtschaftlichkeitSzenario.WORST;

        /// <summary>Das Ergebnis des Stamms — ohne Differenz, solange die Referenz der Stamm ist.</summary>
        private static WirtschaftlichkeitErgebnis Stamm(double? kapitalwert, string szenario = ERWARTET,
                                                        double? differenz = null)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM,
                IstStamm = true,
                Szenario = szenario,
                Anzeige = "Stammprojekt",
                Kapitalwert = kapitalwert,
                KapitalwertDiff = differenz
            };
        }

        /// <summary>Das Ergebnis einer Variante; <c>null</c> = keine Differenz (Referenz oder nicht bestimmbar).</summary>
        private static WirtschaftlichkeitErgebnis Variante(int id, double? differenz, string szenario = ERWARTET)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = id,
                IstStamm = false,
                Szenario = szenario,
                Anzeige = "Variante " + (char)('A' + id - VARIANTE_A),
                Kapitalwert = differenz.HasValue ? -250000.0 + differenz.Value : (double?)null,
                KapitalwertDiff = differenz
            };
        }

        private static List<int> Staende(params int[] ids) => new List<int>(ids);

        // =====================================================================
        //  Stamm und eine Variante
        // =====================================================================

        /// <summary>Ohne Variante steht der Stamm — sein Ergebnis, nicht bloß seine Kennung.</summary>
        [Fact]
        public void Stamm_ohne_Varianten_zeigt_den_Stamm()
        {
            WirtschaftlichkeitErgebnis stamm = Stamm(-250000.0);

            BesteVariante.Auswahl a = BesteVariante.Waehle(new List<WirtschaftlichkeitErgebnis> { stamm }, STAMM,
                                                           Staende(STAMM));

            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, a.Grund);
            Assert.Equal(STAMM, a.IdProjekt);
            Assert.Same(stamm, a.Ergebnis);
            Assert.False(a.IstVariante);
            Assert.Equal(ERWARTET, a.Szenario);
        }

        /// <summary>Eine Variante mit positiver Differenz ist die beste.</summary>
        [Fact]
        public void Eine_Variante_besser_als_der_Stamm_ist_die_beste()
        {
            WirtschaftlichkeitErgebnis a = Variante(VARIANTE_A, 5000.0);
            var ergebnisse = new List<WirtschaftlichkeitErgebnis> { Stamm(-250000.0), a };

            BesteVariante.Auswahl wahl = BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM, VARIANTE_A));

            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, wahl.Grund);
            Assert.Equal(VARIANTE_A, wahl.IdProjekt);
            Assert.Same(a, wahl.Ergebnis);
            Assert.True(wahl.IstVariante);
        }

        /// <summary>
        /// Eine Variante mit NEGATIVER Differenz bleibt die beste Variante: Die Regel vergleicht
        /// die Varianten untereinander, nicht mit dem Stamm — die Karte zeigt dann, um wie viel
        /// die beste Variante schlechter ist.
        /// </summary>
        [Fact]
        public void Eine_Variante_schlechter_als_der_Stamm_bleibt_die_beste_Variante()
        {
            WirtschaftlichkeitErgebnis a = Variante(VARIANTE_A, -3000.0);
            var ergebnisse = new List<WirtschaftlichkeitErgebnis> { Stamm(-250000.0), a };

            BesteVariante.Auswahl wahl = BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM, VARIANTE_A));

            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, wahl.Grund);
            Assert.Same(a, wahl.Ergebnis);
            Assert.Equal(-3000.0, wahl.Ergebnis.KapitalwertDiff.Value, 9);
        }

        // =====================================================================
        //  Mehrere Varianten
        // =====================================================================

        /// <summary>Kriterium ist die größte Kapitalwertdifferenz.</summary>
        [Fact]
        public void Die_groesste_Kapitalwertdifferenz_gewinnt()
        {
            var ergebnisse = new List<WirtschaftlichkeitErgebnis>
            {
                Stamm(-250000.0), Variante(VARIANTE_A, 2000.0), Variante(VARIANTE_B, 7000.0),
                Variante(VARIANTE_C, -1000.0)
            };

            BesteVariante.Auswahl wahl = BesteVariante.Waehle(
                ergebnisse, STAMM, Staende(STAMM, VARIANTE_A, VARIANTE_B, VARIANTE_C));

            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, wahl.Grund);
            Assert.Equal(VARIANTE_B, wahl.IdProjekt);
        }

        /// <summary>Bei gleicher Differenz bleibt der Stand, der in der Reihenfolge zuerst kommt.</summary>
        [Fact]
        public void Gleichstand_entscheidet_die_Reihenfolge_der_Staende()
        {
            var ergebnisse = new List<WirtschaftlichkeitErgebnis>
            {
                Stamm(-250000.0), Variante(VARIANTE_A, 4000.0), Variante(VARIANTE_B, 4000.0)
            };

            Assert.Equal(VARIANTE_A,
                BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM, VARIANTE_A, VARIANTE_B)).IdProjekt);
            Assert.Equal(VARIANTE_B,
                BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM, VARIANTE_B, VARIANTE_A)).IdProjekt);
            // Ohne Standliste entscheidet die Listenfolge der Ergebnisse.
            Assert.Equal(VARIANTE_A, BesteVariante.Waehle(ergebnisse, STAMM).IdProjekt);
        }

        /// <summary>
        /// Ein Stand ohne Ergebnis nimmt nicht teil — er zählt nicht als Null und verdrängt
        /// niemanden. Dasselbe gilt für ein Ergebnis ohne Differenz (die Referenz selbst oder
        /// eine nicht bestimmbare Rechnung).
        /// </summary>
        [Fact]
        public void Ein_Stand_ohne_Ergebnis_nimmt_nicht_teil()
        {
            WirtschaftlichkeitErgebnis b = Variante(VARIANTE_B, -500.0);

            BesteVariante.Auswahl ohneErgebnis = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis> { Stamm(-250000.0), b }, STAMM,
                Staende(STAMM, VARIANTE_A, VARIANTE_B));
            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, ohneErgebnis.Grund);
            Assert.Same(b, ohneErgebnis.Ergebnis);

            BesteVariante.Auswahl ohneDifferenz = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis> { Stamm(-250000.0), Variante(VARIANTE_A, null), b }, STAMM,
                Staende(STAMM, VARIANTE_A, VARIANTE_B));
            Assert.Same(b, ohneDifferenz.Ergebnis);
        }

        // =====================================================================
        //  Rückfall auf den Stamm und kein Ergebnis
        // =====================================================================

        /// <summary>
        /// Tragen die gewählten Varianten kein Ergebnis oder keine Differenz, steht der Stamm —
        /// auch dann, wenn sein Kapitalwert selbst nicht bestimmbar ist.
        /// </summary>
        [Fact]
        public void Nur_der_Stamm_mit_Ergebnis_zeigt_den_Stamm()
        {
            WirtschaftlichkeitErgebnis stamm = Stamm(-250000.0);
            BesteVariante.Auswahl nurStamm = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis> { stamm }, STAMM, Staende(STAMM, VARIANTE_A, VARIANTE_B));
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, nurStamm.Grund);
            Assert.Same(stamm, nurStamm.Ergebnis);

            WirtschaftlichkeitErgebnis stammOhneWert = Stamm(null);
            BesteVariante.Auswahl ohneDifferenzen = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis>
                {
                    stammOhneWert, Variante(VARIANTE_A, null), Variante(VARIANTE_B, null)
                },
                STAMM, Staende(STAMM, VARIANTE_A, VARIANTE_B));
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, ohneDifferenzen.Grund);
            Assert.Same(stammOhneWert, ohneDifferenzen.Ergebnis);
            Assert.False(ohneDifferenzen.Ergebnis.Kapitalwert.HasValue);
        }

        /// <summary>
        /// Ohne jedes zeigbare Ergebnis nennt die Auswahl den übergebenen Stamm ohne Ergebnis —
        /// für eine leere oder fehlende Liste, für Stände ohne Ergebnis und für Varianten ohne
        /// Differenz, wenn der Stamm kein Ergebnis hat.
        /// </summary>
        [Fact]
        public void Ohne_jedes_Ergebnis_nennt_die_Auswahl_den_Stamm_ohne_Ergebnis()
        {
            var faelle = new List<BesteVariante.Auswahl>
            {
                BesteVariante.Waehle(null, STAMM),
                BesteVariante.Waehle(new List<WirtschaftlichkeitErgebnis>(), STAMM, Staende(STAMM)),
                BesteVariante.Waehle(new List<WirtschaftlichkeitErgebnis> { Variante(VARIANTE_B, 800.0) }, STAMM,
                                     Staende(STAMM, VARIANTE_A)),
                BesteVariante.Waehle(new List<WirtschaftlichkeitErgebnis> { Variante(VARIANTE_A, null) }, STAMM,
                                     Staende(STAMM, VARIANTE_A)),
                BesteVariante.Waehle(new List<WirtschaftlichkeitErgebnis> { null }, STAMM)
            };

            foreach (BesteVariante.Auswahl a in faelle)
            {
                Assert.Equal(BesteVariante.Auswahlgrund.KeinErgebnis, a.Grund);
                Assert.Equal(STAMM, a.IdProjekt);
                Assert.Null(a.Ergebnis);
                Assert.False(a.IstVariante);
            }
        }

        // =====================================================================
        //  Szenario, Stamm mit Differenz, Standliste, Merker
        // =====================================================================

        /// <summary>
        /// Gewählt wird im Erwartungsfall, gleich was die übrigen Szenarien sagen; ein anderes
        /// Szenario nur ausdrücklich. Ein Stand, der nur in einem anderen Szenario ein Ergebnis
        /// trägt, nimmt im Erwartungsfall nicht teil.
        /// </summary>
        [Fact]
        public void Gewaehlt_wird_im_Erwartungsfall()
        {
            var ergebnisse = new List<WirtschaftlichkeitErgebnis>
            {
                Stamm(-250000.0, ERWARTET), Variante(VARIANTE_A, 1000.0, ERWARTET), Variante(VARIANTE_B, 500.0, ERWARTET),
                Stamm(-240000.0, BEST), Variante(VARIANTE_A, 1000.0, BEST), Variante(VARIANTE_B, 9000.0, BEST),
                Stamm(-260000.0, WORST), Variante(VARIANTE_A, -8000.0, WORST), Variante(VARIANTE_B, -100.0, WORST),
                Variante(VARIANTE_C, 50000.0, BEST)
            };
            List<int> staende = Staende(STAMM, VARIANTE_A, VARIANTE_B, VARIANTE_C);

            BesteVariante.Auswahl erwartet = BesteVariante.Waehle(ergebnisse, STAMM, staende);
            Assert.Equal(VARIANTE_A, erwartet.IdProjekt);
            Assert.Equal(ERWARTET, erwartet.Szenario);
            Assert.Equal(ERWARTET, erwartet.Ergebnis.Szenario);
            Assert.Equal(VARIANTE_A, BesteVariante.Waehle(ergebnisse, STAMM, staende, null).IdProjekt);

            BesteVariante.Auswahl best = BesteVariante.Waehle(ergebnisse, STAMM, staende, BEST);
            Assert.Equal(VARIANTE_C, best.IdProjekt);
            Assert.Equal(BEST, best.Szenario);
            Assert.Equal(BEST, best.Ergebnis.Szenario);

            BesteVariante.Auswahl worst = BesteVariante.Waehle(ergebnisse, STAMM, staende, WORST);
            Assert.Equal(VARIANTE_B, worst.IdProjekt);
            Assert.Equal(WORST, worst.Ergebnis.Szenario);
        }

        /// <summary>
        /// Ist eine Variante die Referenz, trägt der Stamm eine Differenz — und ist trotzdem nie
        /// die beste Variante. Trägt nur er eine, steht er als Stamm.
        /// </summary>
        [Fact]
        public void Der_Stamm_ist_nie_die_beste_Variante()
        {
            WirtschaftlichkeitErgebnis stamm = Stamm(-242000.0, ERWARTET, 8000.0);
            WirtschaftlichkeitErgebnis referenz = Variante(VARIANTE_A, null);
            WirtschaftlichkeitErgebnis b = Variante(VARIANTE_B, -2000.0);

            BesteVariante.Auswahl mitB = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis> { stamm, referenz, b }, STAMM,
                Staende(STAMM, VARIANTE_A, VARIANTE_B));
            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, mitB.Grund);
            Assert.Same(b, mitB.Ergebnis);

            BesteVariante.Auswahl ohneB = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis> { stamm, referenz }, STAMM, Staende(STAMM, VARIANTE_A));
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, ohneB.Grund);
            Assert.Same(stamm, ohneB.Ergebnis);
        }

        /// <summary>Nur die übergebenen Stände zählen — auf der Seite die gewählten Spalten.</summary>
        [Fact]
        public void Nur_die_uebergebenen_Staende_zaehlen()
        {
            var ergebnisse = new List<WirtschaftlichkeitErgebnis>
            {
                Stamm(-250000.0), Variante(VARIANTE_A, 1000.0), Variante(VARIANTE_B, 9000.0)
            };

            Assert.Equal(VARIANTE_A, BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM, VARIANTE_A)).IdProjekt);
            Assert.Equal(VARIANTE_B, BesteVariante.Waehle(ergebnisse, STAMM).IdProjekt);
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten,
                         BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM)).Grund);
        }

        /// <summary>
        /// Trägt ein Stand zwei Ergebnisse im selben Szenario, zählt das erste — mit wie ohne
        /// Standliste.
        /// </summary>
        [Fact]
        public void Je_Stand_zaehlt_sein_erstes_Ergebnis()
        {
            WirtschaftlichkeitErgebnis b = Variante(VARIANTE_B, 5000.0);
            var ergebnisse = new List<WirtschaftlichkeitErgebnis>
            {
                Stamm(-250000.0), Variante(VARIANTE_A, 1000.0), Variante(VARIANTE_A, 9000.0), b
            };

            Assert.Same(b, BesteVariante.Waehle(ergebnisse, STAMM, Staende(STAMM, VARIANTE_A, VARIANTE_B)).Ergebnis);
            Assert.Same(b, BesteVariante.Waehle(ergebnisse, STAMM).Ergebnis);
        }

        /// <summary>
        /// Ob ein Ergebnis der Stamm ist, entscheidet sein Merker <c>IstStamm</c>, nicht die
        /// Kennung: Ein als Stamm gemerktes Ergebnis nimmt nie als Variante teil.
        /// </summary>
        [Fact]
        public void Der_Merker_entscheidet_ueber_den_Stamm()
        {
            WirtschaftlichkeitErgebnis gemerkt = Variante(VARIANTE_B, 9999.0);
            gemerkt.IstStamm = true;
            WirtschaftlichkeitErgebnis a = Variante(VARIANTE_A, 100.0);

            BesteVariante.Auswahl wahl = BesteVariante.Waehle(
                new List<WirtschaftlichkeitErgebnis> { Stamm(-250000.0), a, gemerkt }, STAMM,
                Staende(STAMM, VARIANTE_A, VARIANTE_B));

            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, wahl.Grund);
            Assert.Same(a, wahl.Ergebnis);
        }
    }

    /// <summary>
    /// <b>Die Karten der Wirtschaftlichkeitsseite zeigen die Wahl des Kerns</b> (Etappe BV-E3):
    /// Die Hülle wählt nicht mehr selbst, sie ruft <see cref="BesteVariante.Waehle"/>. Geprüft
    /// wird an der Testdatenbank, dass die vier Karten genau den Stand zeigen, den die Regel
    /// aus den gespeicherten Ergebnissen wählt — in allen drei Ausgängen —, und an der
    /// gerechneten synthetischen Gruppe (<see cref="Berichtsdatenproben.Gruppendaten"/>), dass
    /// die Regel auf dem Ergebnisbestand des Berichts dieselbe Variante findet.
    ///
    /// <para><b>Eine Arbeitskopie je Klasse</b>; fehlt die Datei, schweigen die Fälle. Die
    /// Klasse liest nur.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BesteVarianteHuellenTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public BesteVarianteHuellenTests(TestDatenbank db) { _db = db; }

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int WOEHLER = 1019;
        private const int WOEHLER_TEST1 = 1023;
        private const int WOEHLER_TEST2 = 1024;
        private const int REFERENZPROJEKT = 1030;

        /// <summary>
        /// Gruppe „Wöhler“: mit beiden Varianten Test1 (+131.844 €), mit Test2 allein die
        /// schlechtere Variante, mit dem Stamm allein sein Nettobarwert — jedes Mal zeigen die
        /// Karten den Stand der Kernregel.
        /// </summary>
        [Fact]
        public void Die_Karten_zeigen_die_Wahl_des_Kerns()
        {
            if (!_db.Vorhanden) return;

            var seite = new WirtschaftlichkeitSeiteGaben(WOEHLER, "Wöhler");
            IReadOnlyDictionary<string, object> gaben = seite.Gaben();
            var laden = (Func<WirtschaftlichkeitStand>)gaben["Laden"];
            var vergleich = (Action<IReadOnlyList<int>>)gaben["VergleichGewaehlt"];
            List<WirtschaftlichkeitErgebnis> gespeichert = new WirtschaftlichkeitCtrl().LadeErgebnisse(
                new List<int> { WOEHLER, WOEHLER_TEST1, WOEHLER_TEST2 });

            WirtschaftlichkeitStand stand = laden();
            BesteVariante.Auswahl alle = BesteVariante.Waehle(gespeichert, WOEHLER, stand.GewaehlteVarianten);
            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, alle.Grund);
            Assert.Equal(WOEHLER_TEST1, alle.IdProjekt);
            Assert.Equal(131844.18.ToString("N0", DE) + " €", stand.Ansicht.Kacheln[0].Wert);
            KartenZeigen(stand.Ansicht, alle, "Test1");

            vergleich(new List<int> { WOEHLER, WOEHLER_TEST2 });
            stand = laden();
            BesteVariante.Auswahl nurTest2 = BesteVariante.Waehle(gespeichert, WOEHLER, stand.GewaehlteVarianten);
            Assert.Equal(WOEHLER_TEST2, nurTest2.IdProjekt);
            Assert.True(nurTest2.Ergebnis.KapitalwertDiff.Value < 0);
            KartenZeigen(stand.Ansicht, nurTest2, "Test2");

            vergleich(new List<int> { WOEHLER });
            stand = laden();
            BesteVariante.Auswahl nurStamm = BesteVariante.Waehle(gespeichert, WOEHLER, stand.GewaehlteVarianten);
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, nurStamm.Grund);
            Assert.Equal(WOEHLER, nurStamm.IdProjekt);
            KartenZeigen(stand.Ansicht, nurStamm, "");
        }

        /// <summary>
        /// Das Referenzprojekt 1030 trägt in der Testdatenbank kein Ergebnis der
        /// Wirtschaftlichkeit: Die Regel nennt den Stamm ohne Ergebnis, die Karten zeigen den
        /// Strich ohne Quelle.
        /// </summary>
        [Fact]
        public void Ohne_Ergebnis_zeigen_die_Karten_den_Strich()
        {
            if (!_db.Vorhanden) return;

            var seite = new WirtschaftlichkeitSeiteGaben(REFERENZPROJEKT, "Referenzprojekt 1030");
            WirtschaftlichkeitStand stand = ((Func<WirtschaftlichkeitStand>)seite.Gaben()["Laden"])();
            BesteVariante.Auswahl wahl = BesteVariante.Waehle(
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { REFERENZPROJEKT }),
                REFERENZPROJEKT, stand.GewaehlteVarianten);

            Assert.Equal(BesteVariante.Auswahlgrund.KeinErgebnis, wahl.Grund);
            Assert.Equal(REFERENZPROJEKT, wahl.IdProjekt);
            KartenZeigen(stand.Ansicht, wahl, "");
        }

        /// <summary>
        /// Die gerechnete synthetische Gruppe (Energiekosten 12 000, 11 000, 10 000 €/a): Die
        /// Regel wählt auf dem Ergebnisbestand des Berichts (<c>BerichtsDaten.Wirtschaftlichkeit</c>,
        /// alle Szenarien) die Variante B — mit wie ohne Standliste. Ohne Variante steht der Stamm.
        /// </summary>
        [Fact]
        public void Die_Regel_findet_in_der_gerechneten_Gruppe_die_guenstigste_Variante()
        {
            if (!_db.Vorhanden) return;

            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(3);
            Berichtsdatenproben.MitWirtschaftlichkeit(daten, Berichtsdatenproben.Parametersatz(daten.IdStamm));

            BesteVariante.Auswahl wahl = BesteVariante.Waehle(daten.Wirtschaftlichkeit, daten.IdStamm);
            Assert.Equal(BesteVariante.Auswahlgrund.BestesKriterium, wahl.Grund);
            Assert.Equal(Berichtsdatenproben.STAMM + 2, wahl.IdProjekt);
            Assert.Equal(WirtschaftlichkeitSzenario.ERWARTET, wahl.Ergebnis.Szenario);
            double groesste = daten.Wirtschaftlichkeit
                .Where(e => !e.IstStamm && e.Szenario == WirtschaftlichkeitSzenario.ERWARTET && e.KapitalwertDiff.HasValue)
                .Max(e => e.KapitalwertDiff.Value);
            Assert.Equal(groesste, wahl.Ergebnis.KapitalwertDiff.Value, 9);

            List<int> staende = daten.Varianten.Select(v => v.IdProjekt).ToList();
            Assert.Same(wahl.Ergebnis, BesteVariante.Waehle(daten.Wirtschaftlichkeit, daten.IdStamm, staende).Ergebnis);

            BerichtsDaten allein = Berichtsdatenproben.Gruppendaten(1);
            Berichtsdatenproben.MitWirtschaftlichkeit(allein, Berichtsdatenproben.Parametersatz(allein.IdStamm));
            BesteVariante.Auswahl stamm = BesteVariante.Waehle(allein.Wirtschaftlichkeit, allein.IdStamm);
            Assert.Equal(BesteVariante.Auswahlgrund.StammOhneVarianten, stamm.Grund);
            Assert.Equal(Berichtsdatenproben.STAMM, stamm.IdProjekt);
        }

        /// <summary>
        /// Die vier Karten gegen die Auswahl: bei einer Variante Differenz und Annuität samt
        /// „beste Variante: ‹Name›“, beim Stamm sein Nettobarwert, ohne Ergebnis der Strich.
        /// </summary>
        private static void KartenZeigen(ErgebnisAnsicht ansicht, BesteVariante.Auswahl wahl, string name)
        {
            Assert.Equal(4, ansicht.Kacheln.Count);
            KachelZeile kw = ansicht.Kacheln[0];
            KachelZeile an = ansicht.Kacheln[1];

            switch (wahl.Grund)
            {
                case BesteVariante.Auswahlgrund.BestesKriterium:
                    string quelle = string.Format(R.WIRT_KACHEL_BESTE, name);
                    Assert.Equal(wahl.Ergebnis.KapitalwertDiff.Value.ToString("N0", DE) + " €", kw.Wert);
                    Assert.Equal(wahl.Ergebnis.AnnuitaetKW.HasValue
                                     ? wahl.Ergebnis.AnnuitaetKW.Value.ToString("N0", DE) + " €/a" : "—", an.Wert);
                    Assert.All(ansicht.Kacheln, k => Assert.Equal(quelle, k.Quelle));
                    break;
                case BesteVariante.Auswahlgrund.StammOhneVarianten:
                    Assert.Equal(wahl.Ergebnis.Kapitalwert.HasValue
                                     ? wahl.Ergebnis.Kapitalwert.Value.ToString("N0", DE) + " €" : "—", kw.Wert);
                    Assert.Equal(R.WIRT_KACHEL_STAMM_KW, kw.Quelle);
                    Assert.All(ansicht.Kacheln.Skip(1), k => Assert.Equal("—", k.Wert));
                    Assert.All(ansicht.Kacheln.Skip(1), k => Assert.Equal(R.WIRT_KACHEL_NUR_STAMM, k.Quelle));
                    break;
                default:
                    Assert.Equal("—", kw.Wert);
                    Assert.Equal("", kw.Quelle);
                    Assert.All(ansicht.Kacheln.Skip(1), k => Assert.Equal("—", k.Wert));
                    Assert.All(ansicht.Kacheln.Skip(1), k => Assert.Equal(R.WIRT_KACHEL_NUR_STAMM, k.Quelle));
                    break;
            }
        }
    }
}
