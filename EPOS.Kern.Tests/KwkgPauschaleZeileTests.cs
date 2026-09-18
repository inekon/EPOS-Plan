using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG U17 — die <b>Pauschale nach § 9 KWKG</b> als eigene Zeile.
    ///
    /// <para><b>Was sie ist.</b> Für ein BHKW bis 2 kW<sub>el</sub> zahlt das Gesetz
    /// einmalig <c>0,04 €/kWh × 60.000 Vbh × P_el</c> aus, binnen zwei Monaten nach der
    /// Zulassung, und ersetzt damit die laufende Abrechnung. Der Rechenkern führt sie
    /// deshalb als Erlösreihe <c>KWKG_PAUSCHALE</c>, deren Betrag im <b>Index 0</b>
    /// steht — die einzige Reihe des Programms, die das tut.</para>
    ///
    /// <para><b>Was hier gemessen wird.</b> Vier Dinge: Die Reihe geht unabgezinst und
    /// in voller Höhe in den Kapitalwert (und zwar so, dass die Zeile daran nichts
    /// ändert — der Betrag war längst drin). Die Zeile erscheint genau dann, wenn die
    /// Pauschale greift, steht im Block A und <b>nicht</b> in dessen €/a-Summe. Die
    /// Mehrjahrestabelle führt sie im Jahr 0, und damit stimmt dort die Selbstprüfung
    /// „Summe der Positionsspalten = Netto nominal" auch in der Zeile 0. Und der
    /// Nachweisumschlag trägt den Betrag über den gebuchten Stand.</para>
    ///
    /// <para><b>Die Zahlen sind hergeleitet:</b> 1,8 kW<sub>el</sub> × 60.000 h ×
    /// 0,04 €/kWh = 4.320 €. Die Leistungsgrenze selbst prüft
    /// <c>WirtschaftlichkeitCtrl.PauschaleReihe</c> gegen den Gesetzeskatalog; hier
    /// steht, was aus dem Ergebnis dieser Prüfung folgt.</para>
    /// </summary>
    public class KwkgPauschaleZeileTests
    {
        /// <summary>Die Fälle halten deutsche Ressourcentexte gegen <c>Contains</c> —
        /// ohne Pinnung wären sie auf dem Windows-Läufer (en-US) rot.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>1,8 kW × 60.000 h × 0,04 €/kWh — eine Anlage unter der Grenze.</summary>
        private const double PAUSCHALE_EUR = 4320.0;

        private const int T = 20;

        // =================================================================
        //  1 — Der Kapitalwert: Index 0, unabgezinst, in voller Höhe
        // =================================================================

        /// <summary>
        /// Die Reihe wirkt zum Zeitpunkt 0 und wird NICHT abgezinst: Der Kapitalwert
        /// steigt um genau den Betrag, egal bei welchem Zinssatz. Wäre sie irrtümlich
        /// als Jahresreihe gebucht, stünde hier der abgezinste Wert — und genau das
        /// fiele bei einem einzigen Zinssatz nicht auf, deshalb zwei.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(3.0)]
        [InlineData(7.5)]
        public void Die_Pauschale_geht_unabgezinst_in_den_Kapitalwert(double zins)
        {
            KapitalwertRechner.Zahlungsbild ohne = Bild(zins, null);
            KapitalwertRechner.Zahlungsbild mit = Bild(zins, PauschalReihe());

            Assert.Equal(PAUSCHALE_EUR, mit.Kapitalwert - ohne.Kapitalwert, 6);

            // Und sie verschiebt KEIN anderes Jahr.
            Assert.Equal(ohne.NominalReihe[0] + PAUSCHALE_EUR, mit.NominalReihe[0], 6);
            for (int t = 1; t <= T; t++)
                Assert.Equal(ohne.NominalReihe[t], mit.NominalReihe[t], 6);
        }

        /// <summary>
        /// Sie mindert NICHT die Investition: I₀ bleibt, was die Anlage kostet, und die
        /// Zahlung steht als Einnahme daneben. Die Altanwendung rechnete andersherum —
        /// mit demselben Kapitalwert, aber einer falschen Investitionssumme in jeder
        /// Tabelle darüber.
        /// </summary>
        [Fact]
        public void Sie_mindert_die_Investition_nicht()
        {
            KapitalwertRechner.Zahlungsbild ohne = Bild(3.0, null);
            KapitalwertRechner.Zahlungsbild mit = Bild(3.0, PauschalReihe());

            Assert.Equal(ohne.Investition, mit.Investition, 6);
            Assert.Equal(ohne.BarwertEinnahmen + PAUSCHALE_EUR, mit.BarwertEinnahmen, 6);
        }

        // =================================================================
        //  2 — Die Zeile im Block A
        // =================================================================

        /// <summary>Greift die Pauschale, steht sie als eigene Zeile im Block A — und
        /// zwar HINTER dem KWK-Zuschlag, wo der Anwender sie sucht.</summary>
        [Fact]
        public void Mit_Pauschale_steht_die_Zeile_im_Block_A()
        {
            List<WirtZeile> zeilen = Rubrik(Lauf(PAUSCHALE_EUR));

            WirtZeile z = Zeile(zeilen, "ERL_A_KWKG_PAUSCHALE");
            Assert.NotNull(z);
            Assert.Equal(WirtZeile.BLOCK_A, z.Block);
            Assert.Equal(0, z.Einzug);
            Assert.False(z.IstSumme);
            Assert.False(z.IstUeberschrift);
            Assert.Contains("§ 9", z.Titel);

            Assert.True(zeilen.IndexOf(Zeile(zeilen, "ERL_A_KWKG")) <
                        zeilen.IndexOf(z),
                        "Die Pauschale steht vor dem KWK-Zuschlag statt darunter.");
        }

        /// <summary>
        /// Der Titel nennt die EINHEIT € und die Einmaligkeit — der Betrag steht nicht
        /// in einer €/a-Spalte, und wer die Zeile liest, muss das sehen, ohne das Gesetz
        /// zu kennen.
        /// </summary>
        [Fact]
        public void Der_Titel_nennt_Euro_und_das_Jahr_0()
        {
            WirtZeile z = Zeile(Rubrik(Lauf(PAUSCHALE_EUR)), "ERL_A_KWKG_PAUSCHALE");

            Assert.NotNull(z);
            Assert.DoesNotContain("€/a", z.Titel);
            Assert.Contains("€", z.Titel);
            Assert.Contains("einmalig", z.Titel);
            Assert.Contains("Jahr 0", z.Titel);
        }

        /// <summary>
        /// OHNE Pauschale gibt es die Zeile nicht. Sie greift nur bis 2 kW<sub>el</sub>
        /// und nur bei gesetztem Schalter; über der Grenze bleibt der Schalter ohne
        /// Wirkung, der laufende Zuschlag rechnet weiter. Eine Nullzeile über eine
        /// Vorschrift, die nicht greift, wäre eine Behauptung — anders als beim
        /// KWK-Zuschlag, wo die 0 samt Begründung die Auskunft IST.
        /// </summary>
        [Fact]
        public void Ohne_Pauschale_gibt_es_die_Zeile_nicht()
        {
            Assert.Null(Zeile(Rubrik(Lauf(0.0)), "ERL_A_KWKG_PAUSCHALE"));

            // Der KWK-Zuschlag steht daneben trotzdem — sonst misst der Fall nur,
            // dass die Rubrik überhaupt leer ist.
            Assert.NotNull(Zeile(Rubrik(Lauf(0.0)), "ERL_A_KWKG"));
        }

        /// <summary>
        /// DIE ABNAHME DES AUFTRAGS: Die Zeile ändert die Summe des Blocks A nicht. Die
        /// Summe ist eine €/a-Summe des Jahres 1, die Pauschale ein Einmalbetrag in € zum
        /// Zeitpunkt 0 — beides zu addieren wäre derselbe Einheitenfehler, aus dem auch
        /// der Restwert-Barwert dort nicht steht. Im KAPITALWERT ist sie längst
        /// enthalten; die Zeile macht sie sichtbar, sie bucht nichts.
        /// </summary>
        [Fact]
        public void Die_Pauschale_steht_nicht_in_der_Summe_des_Blocks_A()
        {
            WirtschaftlichkeitErgebnis ohne = Lauf(0.0);
            WirtschaftlichkeitErgebnis mit = Lauf(PAUSCHALE_EUR);

            WirtZeile summeOhne = Zeile(Rubrik(ohne), "ERL_A_SUMME");
            WirtZeile summeMit = Zeile(Rubrik(mit), "ERL_A_SUMME");

            Assert.NotNull(summeOhne);
            Assert.NotNull(summeMit);
            Assert.Equal(summeOhne.Wert(ohne).Value, summeMit.Wert(mit).Value, 6);

            // Die Gegenprobe: MIT der Pauschale wäre es eine um 4.320 größere Zahl —
            // sie gehörte zu keinem Kapitalwert und zu keiner Jahresrechnung.
            Assert.NotEqual(summeMit.Wert(mit).Value + PAUSCHALE_EUR,
                            summeMit.Wert(mit).Value);
        }

        // =================================================================
        //  3 — Die Mehrjahrestabelle führt sie im Jahr 0
        // =================================================================

        /// <summary>
        /// Die Spalte steht in der Tabelle und trägt ihren Betrag im JAHR 0 — und nur
        /// dort. Die gewöhnliche Reihenübernahme beginnt bei t = 1; ohne den eigenen
        /// Weg wäre die Spalte leer und fiele als „Spalte aus lauter Nullen" weg.
        /// </summary>
        [Fact]
        public void Die_Mehrjahrestabelle_fuehrt_die_Pauschale_im_Jahr_0()
        {
            Mehrjahresbild m = Tabelle(PauschalReihe());
            Assert.NotNull(m);

            MehrjahresSpalte s = Spalte(m, KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE);
            Assert.NotNull(s);
            Assert.Equal(PAUSCHALE_EUR, s.Wert(0), 6);
            for (int t = 1; t <= T; t++) Assert.Equal(0.0, s.Wert(t), 6);
        }

        /// <summary>Ohne Pauschale gibt es die Spalte nicht — dieselbe Konvention wie
        /// bei jeder anderen Positionsspalte ohne einen einzigen Betrag.</summary>
        [Fact]
        public void Ohne_Pauschale_gibt_es_die_Spalte_nicht()
        {
            Assert.Null(Spalte(Tabelle(null), KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE));
        }

        /// <summary>
        /// DIE SELBSTPRÜFUNG DER TABELLE, Zeile 0: Die Summe der Positionsspalten ist
        /// die Spalte „Netto nominal". Ohne die Pauschalspalte stimmte sie im Jahr 0
        /// nicht — dort trug „Netto nominal" bereits −I₀ + Pauschale, die Summe der
        /// Positionen aber nur −I₀.
        /// </summary>
        [Fact]
        public void Im_Jahr_0_ist_die_Summe_der_Positionen_das_Netto()
        {
            Mehrjahresbild m = Tabelle(PauschalReihe());

            double summe = 0;
            foreach (MehrjahresSpalte s in m.Spalten)
                if (!s.IstSumme) summe += s.Wert(0);

            MehrjahresSpalte netto = Spalte(m, "NETTO");
            Assert.NotNull(netto);
            Assert.Equal(netto.Wert(0), summe, 6);
        }

        // =================================================================
        //  4 — Der Nachweisumschlag
        // =================================================================

        /// <summary>Der Betrag reist im Umschlag mit und kommt in derselben Höhe
        /// zurück — sonst zeigte der gebuchte Stand die Zeile nicht.</summary>
        [Fact]
        public void Der_Umschlag_traegt_den_Betrag_hin_und_zurueck()
        {
            var e = new WirtschaftlichkeitErgebnis { KwkgPauschaleEur = PAUSCHALE_EUR };

            string grund;
            string json = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);

            var zurueck = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(json).Uebernimm(zurueck);

            Assert.Equal(PAUSCHALE_EUR, zurueck.KwkgPauschaleEur, 6);
        }

        /// <summary>
        /// DER ALTFALL: Ein Umschlag der Fassung 1 kennt das Feld nicht. Er lädt
        /// trotzdem — mit allem, was er führt, und mit 0 für die Pauschale. Würde die
        /// Fassungserhöhung ihn verwerfen, verlöre jeder vor diesem Auftrag gerechnete
        /// Lauf seine Unterzeilen, seine Modultabelle und seine Kohärenzhinweise.
        /// </summary>
        [Fact]
        public void Ein_Umschlag_der_Fassung_1_laedt_weiter()
        {
            const string alt =
                "nw1:{\"Version\":1,\"KwkgModule\":[{\"Bezeichner\":\"Modul 1\",\"PelKW\":1.8}]," +
                "\"EnergiekostenJeAnlage\":[],\"Betriebskosten\":[],\"KohaerenzHinweise\":[]," +
                "\"VermiedenMengeMWh\":20,\"ProduzierendesGewerbe\":true,\"BezugsspitzeKW\":412.5}";

            ErgebnisNachweisUmschlag u = ErgebnisNachweisUmschlag.Lesen(alt);

            Assert.NotNull(u);
            Assert.Equal(1, u.Version);
            Assert.Single(u.KwkgModule);
            Assert.Equal("Modul 1", u.KwkgModule[0].Bezeichner);
            Assert.Equal(20.0, u.VermiedenMengeMWh, 6);
            Assert.True(u.ProduzierendesGewerbe);
            Assert.Equal(412.5, u.BezugsspitzeKW.Value, 6);

            // Das neue Feld fehlt ihm — und 0 ist die richtige Antwort, nicht ein Wurf.
            Assert.Equal(0.0, u.KwkgPauschaleEur, 6);

            var e = new WirtschaftlichkeitErgebnis { KwkgPauschaleEur = 999 };
            u.Uebernimm(e);
            Assert.Equal(0.0, e.KwkgPauschaleEur, 6);
        }

        /// <summary>Eine HÖHERE Fassung bleibt verworfen — was sie bedeutet, weiß
        /// dieser Stand nicht.</summary>
        [Fact]
        public void Eine_hoehere_Fassung_bleibt_verworfen()
        {
            Assert.Null(ErgebnisNachweisUmschlag.Lesen(
                "nw1:{\"Version\":" + (ErgebnisNachweisUmschlag.FASSUNG + 1) + "}"));
        }

        // =================================================================
        //  Prüfstand
        // =================================================================

        /// <summary>Die Erlösreihe, wie sie <c>PauschaleReihe</c> bildet: der Betrag im
        /// Index 0, sonst nichts.</summary>
        private static List<KapitalwertRechner.ErloesReihe> PauschalReihe()
        {
            var werte = new double[T + 1];
            werte[0] = PAUSCHALE_EUR;
            return new List<KapitalwertRechner.ErloesReihe>
            {
                new KapitalwertRechner.ErloesReihe(
                    KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, werte)
            };
        }

        /// <summary>Ein Zahlungsbild mit runden Zahlen: 100.000 € Investition,
        /// 4.000 €/a Betrieb, 12.000 €/a Energie, 1.000 €/a Einspeiseerlös.</summary>
        private static KapitalwertRechner.Zahlungsbild Bild(
            double zins, List<KapitalwertRechner.ErloesReihe> reihen)
        {
            var invest = new List<KapitalwertRechner.InvestPosition>
            {
                new KapitalwertRechner.InvestPosition { Betrag = 100000, Nutzungsdauer = T }
            };
            return KapitalwertRechner.Rechne(invest, 4000, 12000, 1000, zins, T, 0, 0,
                                             0, reihen);
        }

        /// <summary>Die Mehrjahrestabelle zu <see cref="Bild"/>.</summary>
        private static Mehrjahresbild Tabelle(List<KapitalwertRechner.ErloesReihe> reihen)
        {
            KapitalwertRechner.Zahlungsbild b = Bild(3.0, reihen);
            var kumuliert = new double[T + 1];
            double summe = 0;
            for (int t = 0; t <= T; t++) { summe += b.BarwertReihe[t]; kumuliert[t] = summe; }

            return Mehrjahresbild.Baue(new VerlaufSerie
            {
                IdProjekt = 1,
                Anzeige = "Variante",
                Bild = b,
                Kumuliert = kumuliert,
                RestwertBarwert = b.RestwertBarwert
            });
        }

        private static MehrjahresSpalte Spalte(Mehrjahresbild m, string schluessel)
        {
            if (m == null) return null;
            foreach (MehrjahresSpalte s in m.Spalten)
                if (s.Schluessel == schluessel) return s;
            return null;
        }

        /// <summary>Ein Ergebnis mit BHKW-Positionen, damit die Rubrik überhaupt
        /// entsteht; die Pauschale ist der einzige Unterschied zwischen den Fällen.</summary>
        private static WirtschaftlichkeitErgebnis Lauf(double pauschaleEur)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                IstStamm = false,
                Anzeige = "Variante",
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                KwkgErloesJahr1 = 7316,
                KwkgVbhElektrisch = 4200,
                EnergiesteuerJahr1 = 5119,
                StromsteuerEntlastungJahr1 = 906,
                EinspeiseerloesJahr = 1234,
                EinspeiseerloesKwkJahr = 1234,
                KwkgPauschaleEur = pauschaleEur,
                Kapitalwert = 123456
            };
        }

        private static List<WirtZeile> Rubrik(WirtschaftlichkeitErgebnis e)
        {
            var menge = new List<WirtschaftlichkeitErgebnis> { e };
            return WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null), menge);
        }

        private static WirtZeile Zeile(List<WirtZeile> zeilen, string schluessel)
        {
            foreach (WirtZeile z in zeilen) if (z.Schluessel == schluessel) return z;
            return null;
        }
    }
}
