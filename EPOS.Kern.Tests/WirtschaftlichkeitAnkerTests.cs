using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E1 — die <b>Ankertests</b> der Wirtschaftlichkeit (Analysepapier
    /// 2026-09-19, § 3.5 Befund N2).
    ///
    /// <para><b>Warum es sie gibt.</b> Bis E1 sicherte kein einziger Test einen
    /// ABSOLUTEN Kapitalwert; alle Zusicherungen dieses Feldes waren Differenz- oder
    /// Bitgleichheitsproben. Der Referenzlauf friert ausschließlich Simulationsgrößen
    /// ein (`aggregate.csv`, 160 Größen, keine aus der Wirtschaftlichkeit) — jede
    /// Änderung an Kaskade, Betriebs-, Energiekosten oder Kapitalwert lief dort
    /// unbemerkt durch (Befund N1). Diese Klasse ist das Gegenstück: sie pinnt
    /// gemessene Absolutwerte auf 0,01 € genau.</para>
    ///
    /// <para><b>Der Rechenweg ist der des Kerns</b> — kein Nachbau. Die Kette
    /// <c>LadeParameter</c> → <c>ErgebnisCtrl.Load</c> → <c>KostenEmissionRechner</c>
    /// → <c>WirtschaftlichkeitCtrl.Berechne</c> ist dieselbe, die die Schale über
    /// <c>BerichtsDatenSammler</c> fährt; ein Kern-Weg, <see cref="BerichtsDaten"/>
    /// eines echten Projekts zu füllen, ist damit vorhanden und wird hier benutzt.
    /// Ein eigener Sammler war nicht nötig.</para>
    ///
    /// <para><b>ACHTUNG — die Zahlen sind GEMESSEN, nicht abgeschrieben.</b> Sie
    /// stammen vom Stand der Testdatenbank am 19.09.2026 (Schemastand 96). Wo das
    /// Konzept (§ 6.2) eine andere Zahl nennt, steht die Abweichung am Fall; die
    /// Konzeptzahlen stammen von einem älteren Datenstand.</para>
    ///
    /// <para><b>ETAPPE E7, Teil a — gemessen, kein Anker bewegt sich</b> (alt = neu:
    /// 1024 −2.896.359,13 €, 1030 −21.895.377,28 €, 99,00 €/a, Kaskade 13.000,00 €).
    /// Die Gründe: Der Brennwert am CO₂-Grenzwert (Konzept § 6.3 Nr. 29) wirkt nur auf
    /// die Stromsteuerbefreiung, und kein Ankerprojekt erreicht sie (Hocheffizienz und
    /// räumlicher Zusammenhang ungepflegt); Schemaschritt 101 (Nr. 30) setzt allein die
    /// leere Anlagenart von Nicht-BHKW-Anlagen auf NULL; die vermiedene Menge ohne jede
    /// Eigenerzeugung (Nr. 32) ist Ausweis im Rollentarif, und diese Kette hat weder
    /// Stundenreihen noch einen Tarif.</para>
    ///
    /// <para><b>ETAPPE E7b (Q11, „kein HT/NT") — gemessen, kein Anker bewegt sich</b>
    /// (alt = neu: 1024 −2.896.359,13 € mit Energiekosten 188.167,18 €/a, 1030
    /// −21.895.377,28 € mit Energiekosten 1.176.906,60 €/a, 99,00 €/a, Kaskade
    /// 13.000,00 €; Messung auf der nach Schritt 103 migrierten Kopie, alle Größen der
    /// Kette bitgleich). Die Gründe: Die Testdatenbank führt keinen Tarifsatz — kein
    /// Anker rechnete je mit Zonenpreisen oder der Staffel des Tarifsatzes; Schritt 103
    /// übernimmt deshalb keine Staffel, und die drei Staffelspalten am Stromträger
    /// bleiben leer; und diese Kette hat keine Stundenreihen, also keine Strommatrix,
    /// deren Summen jetzt in einer statt in vier Teilsummen entstehen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WirtschaftlichkeitAnkerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — zwei Gasmodule, vollständige Kostenseite.</summary>
        private const int PROJEKT_BHKW = 1030;

        /// <summary>Das Projekt des Konzeptbeispiels § 6.2 (Betriebskosten 99,00 €/a).</summary>
        private const int PROJEKT_KONZEPT = 1024;

        /// <summary>Das Projekt der Kaskadenregression des Konzepts § 6.2.</summary>
        private const int PROJEKT_KASKADE = 1042;

        /// <summary>
        /// Die fünf Projekte, die die CI im Referenzlauf rechnet. Sie tragen in der
        /// Testdatenbank KEINEN gebuchten Ergebnisstand (der Referenzlauf simuliert
        /// sie je Lauf frisch) und keine Kostenzeilen — siehe
        /// <see cref="Referenzprojekte_der_CI_tragen_keinen_gebuchten_Stand"/>.
        /// </summary>
        private static readonly int[] CI_PROJEKTE = { 1030, 1007, 1017, 1045, 1046 };

        // =====================================================================
        //  Der Rechenweg — dieselbe Kette wie in der Schale
        // =====================================================================

        /// <summary>
        /// Füllt <see cref="BerichtsDaten"/> für EIN echtes Projekt aus der
        /// Datenbank und rechnet die Wirtschaftlichkeit im Szenario „Erwartet".
        /// Das Projekt ist sein eigener Stamm, die Referenz also der Stamm selbst
        /// (<c>IdReferenzprojekt = 0</c>).
        ///
        /// <para>Gibt <c>null</c> zurück, wenn das Projekt keinen gebuchten
        /// Ergebnisstand trägt — dann gibt es nichts zu rechnen.</para>
        /// </summary>
        private static WirtschaftlichkeitErgebnis Rechne(int idProjekt)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(idProjekt);

            var v = new VariantenDaten
            {
                IdProjekt = idProjekt,
                IstStamm = true,
                Projektname = "Anker " + idProjekt,
                Ergebnis = new ErgebnisCtrl().Load(idProjekt)
            };
            KostenEmissionRechner.Berechne(v);

            var daten = new BerichtsDaten { IdStamm = idProjekt, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);

            return new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == idProjekt);
        }

        // =====================================================================
        //  (a) Betriebskosten — Konzept § 6.2
        // =====================================================================

        /// <summary>
        /// Konzept § 6.2 nennt für dieses Projekt 99,00 €/a Betriebskosten.
        /// <b>Gemessen: 99,00 €/a — der Konzeptwert trifft.</b>
        /// </summary>
        [Fact]
        public void Betriebskosten_des_Konzeptbeispiels_sind_99_Euro_im_Jahr()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double bk = WirtschaftlichkeitCtrl.LiesBetriebskosten(
                PROJEKT_KONZEPT, WirtschaftlichkeitSzenario.ERWARTET);

            Assert.Equal(99.00, bk, 2);
        }

        // =====================================================================
        //  (b) Kaskadenregression — Konzept § 6.2
        // =====================================================================

        /// <summary>
        /// Die Kaskade gegen die ROHE Summe <c>SUM(EingegebenerWert)</c> — die
        /// Gegenüberstellung, die den Anwenderbefund W5‑B‑7 ausgelöst hat.
        ///
        /// <para><b>ABWEICHUNG ZUM KONZEPT.</b> Konzept § 6.2 nennt für dieses
        /// Projekt einen Kaskadenaufschlag von <b>+20.927,61 €</b>. Gemessen am
        /// Stand vom 19.09.2026 ist der Aufschlag <b>0,00 €</b>: Beide Zahlen sind
        /// 13.000,00 €. Ursache ist der heutige Datenstand, nicht der Rechenweg —
        /// von den acht Kategorie-1-Zeilen des Projekts trägt genau EINE einen Wert
        /// (13.000,00 € BETRAG); die drei Prozentzeilen (eine
        /// „% der Erzeugerkosten", zwei „% der Investition") haben KEINEN
        /// Einheitpreis und rechnen deshalb 0. Ohne Satz keine Ableitung, ohne
        /// Ableitung kein Aufschlag.</para>
        ///
        /// <para>Gepinnt wird der GEMESSENE Stand. Bekommt eine der Prozentzeilen
        /// wieder einen Satz, fällt dieser Fall und benennt die Änderung.</para>
        /// </summary>
        [Fact]
        public void Kaskade_und_rohe_Summe_des_Kaskadenprojekts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double kaskade = 0;
            foreach (KeyValuePair<KeyValuePair<int, int>, double> e in
                     InvestKaskade.Summen(PROJEKT_KASKADE, WirtschaftlichkeitSzenario.ERWARTET))
                kaskade += e.Value;

            object roh = DataRepository.GetDataTable(
                "SELECT SUM(EingegebenerWert) AS s FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KategorieID = 1",
                new DbParam("@p", PROJEKT_KASKADE)).Rows[0]["s"];
            double roheSumme = roh == DBNull.Value ? 0.0 : Convert.ToDouble(roh);

            Assert.Equal(13000.00, kaskade, 2);
            Assert.Equal(13000.00, roheSumme, 2);

            // Der Aufschlag der Kaskade gegenüber der rohen Summe — heute 0,00 €.
            Assert.Equal(0.00, kaskade - roheSumme, 2);
        }

        // =====================================================================
        //  (c) Absolute Kapitalwerte
        // =====================================================================

        /// <summary>
        /// <b>Der absolute Kapitalwert des Konzeptbeispiels.</b>
        ///
        /// <para><b>ABWEICHUNG ZUM KONZEPT.</b> Konzept § 6.2 nennt
        /// <b>−2.220.322,32 €</b>; gemessen am Stand vom 19.09.2026 sind es
        /// <b>−2.896.359,13 €</b>, also <b>676.036,81 € niedriger</b>. Die
        /// Konzeptzahl stammt von einem älteren Stand; als Ursachen kommen die
        /// Schemaschritte 93–96 und die seither geänderten Rechenwege Kesselbrennstoff
        /// (B‑1/#331) und Hilfsstrom (#365/#366) in Betracht. Die Differenz ist NICHT
        /// nachgerechnet — sie wird hier nur benannt; der gemessene Wert ist der
        /// Anker.</para>
        /// </summary>
        [Fact]
        public void Kapitalwert_des_Konzeptbeispiels_ist_absolut_gepinnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT_KONZEPT);

            Assert.NotNull(e);
            Assert.True(e.Kapitalwert.HasValue, "Kapitalwert fehlt.");
            Assert.Equal(-2896359.13, e.Kapitalwert.Value, 2);   // E7b: alt = neu (kein Tarifsatz, keine Staffel)

            // Die beiden Größen, aus denen er im Wesentlichen entsteht — damit eine
            // Abweichung sofort eingrenzbar ist.
            Assert.Equal(12001.00, e.Investition, 2);
            Assert.Equal(99.00, e.BetriebskostenJahr.Value, 2);
            Assert.Equal(188167.18, e.EnergiekostenJahr.Value, 2);
        }

        /// <summary>
        /// <b>Der absolute Kapitalwert des BHKW-Referenzprojekts</b> — das einzige
        /// Projekt der CI-Fünf, das eine vollständige Kostenseite UND einen
        /// gebuchten Ergebnisstand trägt. Konzept § 6.2 nennt für dieses Projekt
        /// keinen Kapitalwert; der Wert ist reine Messung.
        /// </summary>
        [Fact]
        public void Kapitalwert_des_BHKW_Referenzprojekts_ist_absolut_gepinnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT_BHKW);

            Assert.NotNull(e);
            Assert.True(e.Kapitalwert.HasValue, "Kapitalwert fehlt.");
            Assert.Equal(-21895377.28, e.Kapitalwert.Value, 2);  // E7b: alt = neu (kein Tarifsatz, keine Staffel)

            Assert.Equal(410000.00, e.Investition, 2);
            Assert.Equal(20000.00, e.BetriebskostenJahr.Value, 2);
            Assert.Equal(1176906.60, e.EnergiekostenJahr.Value, 2);
        }

        // =====================================================================
        //  (d) Die Grenze — warum die übrigen vier CI-Projekte keinen Anker tragen
        // =====================================================================

        /// <summary>
        /// <b>Die Grenze dieses Nachweises, als Fall festgehalten.</b>
        ///
        /// <para>Der Auftrag verlangt einen absoluten Kapitalwert für die fünf
        /// CI-Projekte. Vier davon können keinen tragen, und zwar aus zwei Gründen
        /// zugleich: Sie führen in der Testdatenbank KEINEN gebuchten Ergebnisstand
        /// (<c>ErgebnisCtrl.Load</c> liefert <c>null</c> — der Referenzlauf simuliert
        /// sie je Lauf frisch und schreibt in eine Arbeitskopie), und sie haben
        /// KEINE Kategorie-1-Kostenzeilen (Investitionssumme 0,00 €). Ohne
        /// Mengengerüst gibt es keine Energiekosten und damit keinen Kapitalwert.</para>
        ///
        /// <para>Dieser Fall pinnt genau das. Bekommt eines der vier Projekte einen
        /// gebuchten Stand oder eine Kostenseite, fällt er — und der Anker kann
        /// nachgezogen werden. Er ist damit die Quittung für die Lücke, nicht ihre
        /// Verschleierung.</para>
        /// </summary>
        [Theory]
        [InlineData(1007)]
        [InlineData(1017)]
        [InlineData(1045)]
        [InlineData(1046)]
        public void Referenzprojekte_der_CI_tragen_keinen_gebuchten_Stand(int idProjekt)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(new ErgebnisCtrl().Load(idProjekt));

            double invest = 0;
            foreach (KeyValuePair<KeyValuePair<int, int>, double> e in
                     InvestKaskade.Summen(idProjekt, WirtschaftlichkeitSzenario.ERWARTET))
                invest += e.Value;
            Assert.Equal(0.00, invest, 2);

            WirtschaftlichkeitErgebnis w = Rechne(idProjekt);
            Assert.NotNull(w);
            Assert.False(w.Kapitalwert.HasValue);
        }

        /// <summary>
        /// Das Kaskadenprojekt trägt einen gebuchten Stand und eine Investition,
        /// aber keinen Kapitalwert: Seiner elektrischen Erzeugung ist kein
        /// Energieträger zugeordnet, deshalb sind die Energiekosten nicht
        /// bestimmbar. Der Kern sagt das BENANNT statt still 0 zu rechnen — genau
        /// das wird hier festgehalten (ohne den Wortlaut zu pinnen, der zur
        /// Textschicht gehört).
        /// </summary>
        [Fact]
        public void Ohne_Stromtraeger_nennt_der_Kern_den_Grund_statt_still_zu_rechnen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT_KASKADE,
                IstStamm = true,
                Projektname = "Anker " + PROJEKT_KASKADE,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT_KASKADE)
            };
            Assert.NotNull(v.Ergebnis);

            KostenEmissionRechner.Berechne(v);

            Assert.Null(v.Energiekosten);
            Assert.False(string.IsNullOrWhiteSpace(v.EnergiekostenGrund));

            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT_KASKADE);
            Assert.NotNull(e);
            Assert.Equal(13000.00, e.Investition, 2);
            Assert.False(e.Kapitalwert.HasValue);
        }
    }
}
