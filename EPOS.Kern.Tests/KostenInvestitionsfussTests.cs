using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// U29 — DER NACHWEIS: I₀ des dreiteiligen Summenfußes ist genau die
    /// Investition, mit der die Wirtschaftlichkeit rechnet.
    ///
    /// <para><b>Warum dieser Nachweis nötig ist.</b> Der Summenfuß des Dialogs bildet
    /// seine Zahlen aus den Zeilen im Arbeitsstand, die Kapitalwertrechnung aus
    /// <c>WirtschaftlichkeitCtrl.LiesInvestitionen</c>. Zwei Wege, eine Zahl — sonst
    /// nennt der Dialog eine Investition, mit der niemand rechnet. Geprüft wird über
    /// alle Komponenten des Projekts, weil der Fuß je Komponente gebildet wird und
    /// die Kapitalwertrechnung projektweit liest.</para>
    ///
    /// <para><b>Die Messlatte führt keine Zuschusszeile</b> (gemessen:
    /// <c>Tab_ProjektWerte</c> hat in Kategorie 1 keine Zeile mit Kostenart
    /// „Zuschuss" oder gesetztem Erlöskennzeichen). Der Fall wird deshalb hier
    /// angelegt — auf der ARBEITSKOPIE der Testdatenbank
    /// (<see cref="TestDatenbank"/>), die am Ende der Klasse verfällt; die Datei im
    /// Repository bleibt unberührt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenInvestitionsfussTests
    {
        /// <summary>Projekt 1018 der Messlatte — sein Blockheizkraftwerk fuehrt alle
        /// drei Runden der Kaskade: eine Zeile je kW elektrisch (Runde 1), eine
        /// „% der Erzeugerkosten“ (Runde 2) und zwei „% der Investition“ (Runde 3).</summary>
        private const int PROJEKT = 1018;

        private const int BHKW = 7;
        private const double ZUSCHUSS = 6000.0;

        /// <summary>Die Komponenten, die in Kategorie 1 überhaupt Zeilen führen.</summary>
        private static List<int> Komponenten()
        {
            var liste = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT DISTINCT KomponentenID FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KategorieID = ? ORDER BY KomponentenID",
                new DbParam("@p", PROJEKT),
                new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                if (r[0] != DBNull.Value) liste.Add(Convert.ToInt32(r[0]));
            return liste;
        }

        /// <summary>Legt die fehlende Zuschusszeile am BHKW an — Kostenart
        /// „Zuschuss" UND Erlöskennzeichen, so wie der Zeileneditor sie schreibt.</summary>
        private static void ZuschussAnlegen()
        {
            int id = KostenProjektPositionenCtrl.Neu(
                PROJEKT, BHKW, DbWerte.KOSTEN_KATEGORIE_INVESTITION,
                "Zuschuss Prüffall U29", DbWerte.KOSTENART_ZUSCHUSS, DbWerte.BEMESSUNG_BETRAG, 0);
            Assert.True(id > 0);

            foreach (KostenProjektPositionenCtrl.Zeile z in
                     KostenProjektPositionenCtrl.Lies(PROJEKT, BHKW,
                                                      DbWerte.KOSTEN_KATEGORIE_INVESTITION, -1))
            {
                if (z.Raster.Id != id) continue;
                z.Raster.Satz = ZUSCHUSS;
                z.Raster.IstErloes = true;
                Assert.True(KostenProjektPositionenCtrl.Speichern(z));
                return;
            }
            Assert.Fail("Die angelegte Zuschusszeile wurde nicht wiedergefunden.");
        }

        /// <summary>Der Summenfuß aller Komponenten des Projekts, aufsummiert.</summary>
        private static KostenSummenCtrl.Investitionsfuss FussDesProjekts()
        {
            var gesamt = new KostenSummenCtrl.Investitionsfuss();
            foreach (int komponente in Komponenten())
            {
                var positionen = new List<KostenVorlagenPosition>();
                foreach (KostenProjektPositionenCtrl.Zeile z in
                         KostenProjektPositionenCtrl.Lies(PROJEKT, komponente,
                                                          DbWerte.KOSTEN_KATEGORIE_INVESTITION, -1))
                    positionen.Add(z.Raster);

                KostenSummenCtrl.Investitionsfuss f = KostenSummenCtrl.Fuss(positionen);
                gesamt.Brutto += f.Brutto;
                gesamt.Zuschuss += f.Zuschuss;
                gesamt.Investition += f.Investition;
                gesamt.MitZuschuss |= f.MitZuschuss;
            }
            return gesamt;
        }

        [Fact]
        public void I0_des_Summenfusses_ist_die_Investition_der_Wirtschaftlichkeit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZuschussAnlegen();

            KostenSummenCtrl.Investitionsfuss fuss = FussDesProjekts();

            double zuschussDerRechnung;
            List<KapitalwertRechner.InvestPosition> invest =
                WirtschaftlichkeitCtrl.LiesInvestitionen(
                    PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, out zuschussDerRechnung);
            double bruttoDerRechnung = 0;
            foreach (KapitalwertRechner.InvestPosition p in invest) bruttoDerRechnung += p.Betrag;

            Assert.True(fuss.MitZuschuss);
            Assert.Equal(ZUSCHUSS, fuss.Zuschuss, 2);
            Assert.Equal(zuschussDerRechnung, fuss.Zuschuss, 2);
            Assert.Equal(bruttoDerRechnung, fuss.Brutto, 2);
            Assert.Equal(bruttoDerRechnung - zuschussDerRechnung, fuss.Investition, 2);
        }

        /// <summary>Gegenprobe: OHNE die Zuschusszeile fallen brutto und I₀ zusammen,
        /// und die dritte Zeile entfällt.</summary>
        [Fact]
        public void Ohne_Zuschusszeile_fallen_brutto_und_I0_zusammen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenSummenCtrl.Investitionsfuss fuss = FussDesProjekts();

            Assert.False(fuss.MitZuschuss);
            Assert.Equal(0.0, fuss.Zuschuss, 6);
            Assert.Equal(fuss.Brutto, fuss.Investition, 6);
            Assert.Equal("", KostenSummenCtrl.FussText(fuss));
        }

        /// <summary>
        /// U28: Die Kaskade schreibt Runde und Herkunft mit — an einer ECHTEN
        /// Projektzeile, nicht nur im Textbaukasten. Projekt 1018 führt am BHKW eine
        /// Zeile mit Bezugsgröße; sie muss ihre Runde und ihre Herkunft kennen.
        /// </summary>
        [Fact]
        public void Eine_Projektzeile_mit_Bezugsgroesse_kennt_Runde_und_Herkunft()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            bool geprueft = false;
            foreach (int komponente in Komponenten())
                foreach (KostenProjektPositionenCtrl.Zeile z in
                         KostenProjektPositionenCtrl.Lies(PROJEKT, komponente,
                                                          DbWerte.KOSTEN_KATEGORIE_INVESTITION, -1))
                {
                    // Jede Zeile der Investitionsseite läuft durch die Kaskade und
                    // trägt deshalb eine Runde zwischen 1 und 3.
                    Assert.InRange(z.Runde, 1, 3);
                    if (!z.Basis.HasValue) continue;

                    Assert.NotEqual("", z.BasisHerkunft);
                    KostenHerleitung.Angabe a = KostenHerleitung.Bilde(
                        z.Raster, komponente, z, true);
                    Assert.NotEqual("", a.Zeile);
                    Assert.Contains(a.BasisText, a.Zeile);
                    geprueft = true;
                }

            Assert.True(geprueft, "Projekt 1018 führt keine Investitionszeile mit Bezugsgröße.");
        }
    }
}
