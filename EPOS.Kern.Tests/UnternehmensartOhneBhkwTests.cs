using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E19 (Konzept § 6.3 Nr. 33, E19‑Q2/Q3 a) — <b>die Unternehmensart ohne BHKW</b>.
    ///
    /// <para>Der Parameterdialog pflegt sie in der Gruppe „Strom", sobald die
    /// Vergleichsgruppe kein BHKW führt; der Rechenweg dafür besteht seit E5
    /// (<c>WirtschaftlichkeitCtrl.BaueSteuerEingabe</c>: § 9b hängt an keiner KWK-Anlage).
    /// Diese Klasse hält beide Seiten fest: Die Hülle reicht Stromsteueranteil und Katalog
    /// als Parameter des Dialogs herein, und der Lauf eines Projekts ohne BHKW rechnet § 9b
    /// genau dann, wenn die Unternehmensart es trägt. Die Fälle arbeiten auf der
    /// ARBEITSKOPIE der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class UnternehmensartOhneBhkwTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Referenzprojekt ohne BHKW (Kessel), mit gespeichertem Simulationsergebnis.</summary>
        private const int PROJEKT_OHNE_BHKW = 1041;

        [Fact]
        public void Die_Huelle_reicht_Stromsteueranteil_und_Katalog_als_Parameter_des_Dialogs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                WirtschaftlichkeitParameterHuelle.Gaben(PROJEKT_OHNE_BHKW);

            Assert.False((bool)gaben["HatBhkw"]);
            Assert.IsType<StromsteueranteilStand>(gaben["Stromsteueranteil"]);
            var katalog = Assert.IsType<Func<string, int, GesetzParameter>>(gaben["Katalog"]);
            GesetzParameter regel = katalog(DbWerte.GESETZ_STROMST_REGELSATZ, 2026);
            Assert.NotNull(regel);
            Assert.Equal(20.5, regel.Wert);

            // Jeder Schlüssel trifft einen [Parameter] des Dialogs — ein fremder bräche
            // beim ersten Zeichnen im Blazor-Verteiler, ohne Namen.
            HashSet<string> parameter = typeof(WirtschaftlichkeitParameterDialog)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
                .Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            foreach (string schluessel in gaben.Keys)
                Assert.True(parameter.Contains(schluessel), "Kein [Parameter] für „" + schluessel + "\".");
        }

        [Fact]
        public void Ohne_BHKW_rechnet_Paragraf_9b_nur_mit_entlastungsberechtigter_Unternehmensart()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            Assert.False(ctrl.ErzeugerDerGruppe(PROJEKT_OHNE_BHKW).Bhkw);

            // Vorbestand: kein produzierendes Gewerbe — keine Entlastung, keine Steuereingabe.
            WirtschaftlichkeitErgebnis ohne = Lauf(DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE);
            Assert.Equal(0.0, ohne.StromsteuerEntlastungJahr1, 6);
            Assert.False(ohne.ProduzierendesGewerbe);

            // Dieselbe Rechnung als produzierendes Gewerbe: § 9b entlastet den Netzbezug.
            WirtschaftlichkeitErgebnis mit = Lauf(DbWerte.UNTERNEHMENSART_PROD_GEWERBE);
            Assert.True(mit.ProduzierendesGewerbe);
            Assert.True(mit.StromsteuerEntlastungJahr1 > 0,
                        "§ 9b ohne BHKW: erwartet > 0, gerechnet " + mit.StromsteuerEntlastungJahr1);
        }

        private static WirtschaftlichkeitErgebnis Lauf(string unternehmensart)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_OHNE_BHKW);
            p.Unternehmensart = unternehmensart;
            Assert.True(ctrl.SpeichereParameter(p));

            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT_OHNE_BHKW);
            Assert.NotNull(erg);
            var stamm = new VariantenDaten { IdProjekt = PROJEKT_OHNE_BHKW, Ergebnis = erg, IstStamm = true };
            KostenEmissionRechner.Berechne(stamm);
            var daten = new BerichtsDaten { IdStamm = PROJEKT_OHNE_BHKW };
            daten.Varianten.Add(stamm);

            WirtschaftlichkeitErgebnis treffer = null;
            foreach (WirtschaftlichkeitErgebnis e in new WirtschaftlichkeitCtrl().Berechne(daten, p))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET && e.IdProjekt == PROJEKT_OHNE_BHKW)
                    treffer = e;
            Assert.NotNull(treffer);
            return treffer;
        }
    }
}
