using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E19/1 (Konzept § 6.3 Nr. 15, E19‑Q1 a) — die WACHE zu „Bilanzjahr und
    /// Unternehmensart wirken erst beim nächsten Dialog-Öffnen".
    ///
    /// <para><b>Der Punkt ist überholt.</b> Er stammt aus Grenze 2 des B4-Protokolls:
    /// Der WinForms-Trägerdialog las beide Werte beim Blockaufbau, eine Änderung im
    /// Parameterdialog wirkte erst beim nächsten Öffnen. Heute liest die Hülle der
    /// Energieträgerverwaltung beide Werte in <c>Gaben</c> — je ÖFFNUNG, denn jede
    /// Öffnung baut eine neue Hülle (modales Fenster <c>EnergietraegerFenster</c>, Aufruf
    /// <c>KostenSeiteGaben.TraegerGaben</c>), und aus dem Trägerdialog führt kein Weg in
    /// den Parameter- oder BHKW-Dialog. Die Wache hält genau das fest: Jede Öffnung
    /// sieht den gespeicherten Stand.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogjahrJeOeffnungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;   // Stammprojekt, Unternehmensart KEIN_PROD_GEWERBE
        private const int STROM = 60;       // „Elektrische Energie" des Projekts

        private static EnergietraegerAnsicht Oeffnen(EnergietraegerHuelle h)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(STROM);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            EnergietraegerAnsicht a = laden(STROM);
            Assert.NotNull(a.Stand);
            Assert.NotNull(a.SatzRegelfall);
            Assert.NotNull(a.SatzReduziert);
            return a;
        }

        [Fact]
        public void Jede_Oeffnung_liest_Unternehmensart_und_Bilanzjahr_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Vorbestand: kein produzierendes Gewerbe, kein Bilanzjahr — der Regelsatz
            // ist hervorgehoben, das Katalogjahr ist die Bilanzkonvention (2026).
            var offen = new EnergietraegerHuelle(PROJEKT);
            EnergietraegerAnsicht vorher = Oeffnen(offen);
            Assert.True(vorher.SatzRegelfall!.Empfohlen);
            Assert.False(vorher.SatzReduziert!.Empfohlen);
            Assert.Contains("(ab 2026", vorher.SatzRegelfall.Herkunft);   // Katalogzeile 2026

            // Gepflegt wird im Parameter- bzw. BHKW-Dialog — hier derselbe Schreibweg.
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            p.Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE;
            p.BilanzJahr = 2025;               // vor der ersten Katalogzeile (2026)
            Assert.True(ctrl.SpeichereParameter(p));

            // Die NEUE Öffnung sieht beides: Hervorhebung reduziert, Herkunft nennt 2025.
            EnergietraegerAnsicht neu = Oeffnen(new EnergietraegerHuelle(PROJEKT));
            Assert.False(neu.SatzRegelfall!.Empfohlen);
            Assert.True(neu.SatzReduziert!.Empfohlen);
            Assert.DoesNotContain("(ab 2026", neu.SatzRegelfall.Herkunft);
            Assert.Contains(" 2025.", neu.SatzRegelfall.Herkunft);   // „… keinen Satz im Jahr 2025."

            // „Gaben" IST die Öffnung: Auch dieselbe Instanz liest beim erneuten
            // Parametersatz neu — es gibt keinen Zwischenspeicher über Öffnungen hinweg.
            EnergietraegerAnsicht wieder = Oeffnen(offen);
            Assert.True(wieder.SatzReduziert!.Empfohlen);
            Assert.Contains(" 2025.", wieder.SatzRegelfall!.Herkunft);
        }
    }
}
