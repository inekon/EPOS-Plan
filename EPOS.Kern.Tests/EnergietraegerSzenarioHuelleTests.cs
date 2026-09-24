using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E9b (Konzept § 2.11.5 „Pflege"; Entscheid E9b‑Q1, Lesart a) — <b>die
    /// Trägerpreise je Szenario über die Trägerkarte</b>: die HÜLLE der
    /// Energieträgerverwaltung gegen die Testdatenbank, dazu die Kohärenzzeilen, die die
    /// Hüllen der zwei Einspeisevergütungen an ihre Dialoge reichen.
    ///
    /// <para>Geprüft wird der Kreis öffnen–eintragen–speichern–öffnen an Erdgas E (63) des
    /// Projekts 1030 (Hi 10,5 kWh/Nm³, Erwartet 0,84 €/Nm³): Die Karte zeigt den
    /// Szenario-Arbeitspreis in DERSELBEN Preisbasis wie das Feld daneben und schreibt ihn
    /// als Basiswert je Abrechnungseinheit über
    /// <see cref="EnergietraegerPreisCtrl.SzenarioSchreiben"/> in die Projektübersteuerung;
    /// Grund- und Leistungspreis kennen keine Preisbasis. Ein Speichern der Karte, das die
    /// Szenariopreise nicht anfasst, lässt sie stehen. Im Katalog gibt es keine
    /// Szenariopreise.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerSzenarioHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int STROM = 60;       // „Elektrische Energie", ELECTRICITY
        private const int ERDGAS_E = 63;    // Brennstoff 3, Abrechnungseinheit Nm³, Hi 10,5

        private static IReadOnlyDictionary<string, object> Geladen(
            EnergietraegerHuelle h, int traegerId, out EnergietraegerStand stand)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(traegerId);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            EnergietraegerAnsicht a = laden(traegerId);
            Assert.NotNull(a.Stand);
            stand = a.Stand;
            return gaben;
        }

        private static bool Speichern(IReadOnlyDictionary<string, object> gaben) =>
            ((Func<bool>)gaben["Speichern"])();

        /// <summary>Eine Eingabe, wie die Oberfläche sie macht: setzen, dann nachrechnen
        /// lassen (der Wirt ruft nach jeder Änderung <c>Nachrechnen</c>).</summary>
        private static void Feld(IReadOnlyDictionary<string, object> gaben, Action eingabe)
        {
            eingabe();
            ((Func<EnergietraegerAnsicht>)gaben["Nachrechnen"])();
        }

        private static Task PreisbasisSetzen(IReadOnlyDictionary<string, object> gaben, int index) =>
            ((EventCallback<int>)gaben["PreisbasisGewechselt"]).InvokeAsync(index);

        /// <summary>Der Listenplatz einer Preisbasis, gefragt mit der Mengeneinheit.</summary>
        private static int IndexDerEinheit(EnergietraegerStand stand, string einheit)
        {
            string gesucht = EnergietraegerPreisCtrl.EinheitSchluessel(
                EnergietraegerPreiskarte.ArbeitspreisEinheit(einheit, true));
            for (int i = 0; i < stand.Preisbasen.Count; i++)
                if (EnergietraegerPreisCtrl.EinheitSchluessel(stand.Preisbasen[i].Text) == gesucht)
                    return i;
            return -1;
        }

        [Fact]
        public async Task Die_Szenariopreise_gehen_ueber_die_Karte_hin_und_zurueck_in_jeder_Preisbasis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out EnergietraegerStand stand);
            Assert.True(stand.MitSzenario);
            Assert.Equal("Erdgas E", stand.TraegerName);
            Assert.Null(stand.SzenarioArbeitBest);
            Assert.Null(stand.SzenarioArbeitWorst);
            Assert.Null(stand.SzenarioGrundBest);
            Assert.Null(stand.SzenarioGrundWorst);
            Assert.Null(stand.SzenarioLeistungBest);
            Assert.Null(stand.SzenarioLeistungWorst);
            Assert.False(stand.SzenarioArbeitOhneErwartet);           // 0,84 €/Nm³ gepflegt
            int nm3 = IndexDerEinheit(stand, "Nm³"), kwh = IndexDerEinheit(stand, "kWh");
            Assert.True(nm3 >= 0 && kwh >= 0, "Erdgas E muss Nm³ und kWh als Preisbasis anbieten.");

            // (1) In der Abrechnungseinheit steht der Basiswert.
            await PreisbasisSetzen(gaben, nm3);
            Assert.Equal(0.84, stand.Arbeitspreis, 9);
            Feld(gaben, () =>
            {
                stand.SzenarioArbeitBest = 0.74;
                stand.SzenarioGrundWorst = 1500;
                stand.SzenarioLeistungBest = 12.5;
            });
            Assert.True(stand.SzenarioArbeitGepflegt);
            Assert.True(stand.SzenarioGrundGepflegt);
            Assert.True(Speichern(gaben));

            TraegerpreisSzenario z = EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT, ERDGAS_E);
            Assert.Equal(0.74, z.ArbeitspreisBest!.Value, 9);
            Assert.Null(z.ArbeitspreisWorst);
            Assert.Null(z.GrundpreisBest);
            Assert.Equal(1500.0, z.GrundpreisWorst);
            Assert.Equal(12.5, z.LeistungspreisBest);
            Assert.Null(z.LeistungspreisWorst);
            Assert.Equal(0.84, EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT, ERDGAS_E).Arbeitspreis);

            // (2) In €/kWh folgt der Szenariopreis dem Feld daneben (÷ Hi) — und geht als
            //     Basiswert je Nm³ zurück in die Datenbank (× Hi).
            await PreisbasisSetzen(gaben, kwh);
            Assert.Equal(0.08, stand.Arbeitspreis, 9);                  // 0,84 ÷ 10,5
            Assert.Equal(0.74 / 10.5, stand.SzenarioArbeitBest!.Value, 9);
            Assert.Equal(1500.0, stand.SzenarioGrundWorst);             // ohne Preisbasis
            Feld(gaben, () => stand.SzenarioArbeitWorst = 0.09);        // €/kWh
            Assert.True(Speichern(gaben));

            z = EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT, ERDGAS_E);
            Assert.Equal(0.945, z.ArbeitspreisWorst!.Value, 9);         // 0,09 × 10,5 €/Nm³
            Assert.Equal(0.74, z.ArbeitspreisBest!.Value, 9);

            // (3) Wieder öffnen: Die Karte zeigt, was in der Datenbank steht — in ihrer
            //     (mitgespeicherten) Preisbasis, im selben Verhältnis zum Erwartet-Preis.
            IReadOnlyDictionary<string, object> wiederGaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out EnergietraegerStand wieder);
            Assert.Equal(wieder.Arbeitspreis * 0.74 / 0.84, wieder.SzenarioArbeitBest!.Value, 9);
            Assert.Equal(wieder.Arbeitspreis * 0.945 / 0.84, wieder.SzenarioArbeitWorst!.Value, 9);
            Assert.Equal(1500.0, wieder.SzenarioGrundWorst);
            Assert.Equal(12.5, wieder.SzenarioLeistungBest);
            Assert.True(wieder.SzenarioArbeitGepflegt);

            // (4) Leeren schreibt NULL — „wie Erwartet".
            Feld(wiederGaben, () =>
            {
                wieder.SzenarioArbeitBest = null;
                wieder.SzenarioArbeitWorst = null;
                wieder.SzenarioGrundWorst = null;
                wieder.SzenarioLeistungBest = null;
            });
            Assert.True(Speichern(wiederGaben));
            Assert.True(EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT, ERDGAS_E).Leer);
        }

        /// <summary>
        /// Ein Speichern der Karte, das nur den Erwartet-Preis ändert, lässt die gepflegten
        /// Szenariopreise stehen — die Karte schreibt zurück, was sie geladen hat.
        /// </summary>
        [Fact]
        public void Ein_Speichern_ohne_Szenariopflege_laesst_die_Szenariopreise_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT, ERDGAS_E,
                new TraegerpreisSzenario { ArbeitspreisBest = 0.74, GrundpreisWorst = 1500 }));

            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out EnergietraegerStand stand);
            Assert.Equal(1500.0, stand.SzenarioGrundWorst);
            Feld(gaben, () => stand.Grundpreis = 1300);
            Assert.True(Speichern(gaben));

            TraegerpreisSzenario z = EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT, ERDGAS_E);
            Assert.Equal(0.74, z.ArbeitspreisBest!.Value, 9);
            Assert.Equal(1500.0, z.GrundpreisWorst);
            Assert.Equal(1300.0, EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT, ERDGAS_E).Grundpreis);
        }

        /// <summary>Im Katalog gibt es keine Szenariopreise — sie stehen an der Projektübersteuerung.</summary>
        [Fact]
        public void Im_Katalog_fuehrt_die_Karte_keine_Szenariopreise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Geladen(new EnergietraegerHuelle(0), ERDGAS_E, out EnergietraegerStand katalog);
            Assert.False(katalog.MitSzenario);
            Assert.Null(katalog.SzenarioArbeitBest);
        }

        /// <summary>
        /// E9a‑Q3 (Lesart a): Neben einer gepflegten Leistungspreis-Staffel bleibt ein
        /// Szenario-Leistungspreis ohne Wirkung — die Karte trägt den Satz des Kerns mit dem
        /// Namen des Trägers, sobald die Staffel steht.
        /// </summary>
        [Fact]
        public void Neben_der_Staffel_meldet_die_Karte_den_Szenario_Leistungspreis_ohne_Wirkung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), STROM, out EnergietraegerStand stand);
            Assert.True(stand.MitSzenario);
            Assert.True(stand.MitStaffel);

            Feld(gaben, () =>
            {
                stand.StaffelGrenze = 1500;
                stand.StaffelPreis1 = 60;
                stand.StaffelPreis2 = 90;
            });

            Assert.Equal(string.Format(R.WIRT_SZ_LEISTUNGSPREIS_OHNE_WIRKUNG, stand.TraegerName),
                         stand.SzenarioLeistungOhneWirkung);
            Assert.Contains("ohne Wirkung", stand.SzenarioLeistungOhneWirkung);
            Assert.False(stand.SzenarioLeistungOhneErwartet);           // die Staffel ist ein Preis
        }

        /// <summary>
        /// Die Texte der ±-Knöpfe kommen mit den Kartentexten — und jeder Schlüssel des
        /// Satzes trifft einen <c>[Parameter]</c> der Karte (sonst bräche das erste Zeichnen
        /// im Blazor-Verteiler, ohne Namen).
        /// </summary>
        [Fact]
        public void Die_Kartentexte_tragen_die_Szenarioknoepfe_und_treffen_Parameter_der_Karte()
        {
            using var db = new TestDatenbank();      // Gaben() liest die Trägerliste
            if (!db.Vorhanden) return;

            var texte = (IReadOnlyDictionary<string, object>)new EnergietraegerHuelle(0).Gaben()["KarteTexte"];
            Assert.Equal("Szenariopreise Best/Worst", texte["TitelSzenario"]);
            Assert.StartsWith("± je Preis: Best (Günstig) und Worst (Ungünstig)", (string)texte["HinweisSzenario"]);
            Assert.Contains("{0}", (string)texte["VorlageSzenarioOhneErwartet"]);
            Assert.Equal("gepflegt", texte["SzenarioGepflegtText"]);
            Assert.Equal("ohne Erwartet-Wert", texte["SzenarioWarnungText"]);

            List<string> fremd = texte.Keys
                .Where(k => typeof(EnergietraegerEinstellungen).GetProperty(k)?
                                .GetCustomAttribute<ParameterAttribute>() is null)
                .ToList();
            Assert.True(fremd.Count == 0, "Kein [Parameter] der Trägerkarte: " + string.Join(", ", fremd));
        }

        // =================================================================
        //  Die Kohärenzzeilen der zwei Einspeisevergütungen (E9a‑Q7)
        // =================================================================

        /// <summary>
        /// Die Parameterhülle reicht dem ±-Knopf der Einspeisevergütung PV die Fälle, in denen
        /// die flache Vergütung im Lauf nicht wirkt — das wirksame Rollenmodell, sonst der
        /// aktive PV-Vergütungsdialog; die BHKW-Hülle dem der Einspeisevergütung KWK das
        /// Rollenmodell. Dieselben Sätze wie am Ergebnis.
        /// </summary>
        [Fact]
        public void Die_Huellen_reichen_die_Kohaerenzzeilen_der_Einspeiseverguetungen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            bool rollen = new WirtschaftlichkeitCtrl().LadeTarif(PROJEKT).Wirksam;
            PvVerguetungStand pvStand = new ProjektPhotovoltaikCtrl().LiesAufgeloest(PROJEKT);
            bool pv = pvStand != null && pvStand.Aktiv;

            var pvHinweise = (IReadOnlyList<string>)WirtschaftlichkeitParameterHuelle.Gaben(PROJEKT)
                                                                                  ["EinspeisungSzenarioHinweise"];
            var kwkHinweise = (IReadOnlyList<string>)BhkwWirtschaftlichkeitHuelle.Gaben(PROJEKT, null, out _)
                                                                                   ["EinspeisungKwkSzenarioHinweise"];

            if (rollen)
            {
                Assert.Equal(new[] { R.WIRT_SZ_ROLLEN_EINSPEISUNG }, pvHinweise);
                Assert.Equal(new[] { R.WIRT_SZ_ROLLEN_EINSPEISUNG }, kwkHinweise);
            }
            else
            {
                Assert.Equal(pv ? new[] { R.WIRT_SZ_PV_DIALOG_EINSPEISUNG } : Array.Empty<string>(), pvHinweise);
                Assert.Empty(kwkHinweise);
            }
        }
    }
}
