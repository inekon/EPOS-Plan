using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Regel der Wärmegestehungskosten „nur Wärmeerzeuger" (Anwenderentscheid 30.09.2026) und
    /// die Herleitung „Menge × Preis" je Energieträger — ohne Datenbank: Zuordnung der Positionen,
    /// Erlösreihen, Anteil an Grund- und Leistungspreis, Energiekosten der Wärmeerzeuger,
    /// Stromgutschrift und die entgangene § 9b-Entlastung, die sie mindert (Anwenderentscheid
    /// 02.10.2026), Kennzahl, Aufteilung der CO₂-Abgabe, Klartext der Herleitung, die
    /// Zeilen der Tafel und der Nachweisumschlag.
    /// </summary>
    public class WaermegestehungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // ------------------------------------------------------------ Zuordnung

        [Theory]
        [InlineData(Waermegestehung.KOMPONENTE_WAERMEPUMPE, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_HEIZKESSEL, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_SOLARTHERMIE, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_PUFFERSPEICHER, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_BHKW, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_PHOTOVOLTAIK, 0, true, false)]
        [InlineData(Waermegestehung.KOMPONENTE_STROMSPEICHER, 0, true, false)]
        // Eine Stromanlage bleibt draußen, auch wenn die Position eine Wärmeanlage nennt.
        [InlineData(Waermegestehung.KOMPONENTE_PHOTOVOLTAIK, Waermegestehung.TYP_BHKW, true, false)]
        // Allgemeine Positionen: ohne Anlage zählen sie, mit Anlage entscheidet deren Typ.
        [InlineData(Waermegestehung.KOMPONENTE_WAERMEZENTRALE, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_BAULICHE_ANLAGEN, 0, false, true)]
        [InlineData(0, 0, false, true)]
        [InlineData(42, 0, false, true)]
        [InlineData(Waermegestehung.KOMPONENTE_WAERMEZENTRALE, Waermegestehung.TYP_PHOTOVOLTAIK, false, false)]
        [InlineData(Waermegestehung.KOMPONENTE_BAULICHE_ANLAGEN, Waermegestehung.TYP_BATTERIESPEICHER, false, false)]
        [InlineData(0, Waermegestehung.TYP_HEIZKESSEL, false, true)]
        // Die Stromeinspeisung zählt nur mit BHKW — oder nach der Anlage, der sie gehört.
        [InlineData(Waermegestehung.KOMPONENTE_STROMEINSPEISUNG, 0, false, false)]
        [InlineData(Waermegestehung.KOMPONENTE_STROMEINSPEISUNG, 0, true, true)]
        [InlineData(Waermegestehung.KOMPONENTE_STROMEINSPEISUNG, Waermegestehung.TYP_PHOTOVOLTAIK, true, false)]
        [InlineData(Waermegestehung.KOMPONENTE_STROMEINSPEISUNG, Waermegestehung.TYP_BHKW, false, true)]
        public void Die_Zuordnung_einer_Kostenposition(int komponente, int anlagentyp, bool bhkw, bool zaehlt)
        {
            Assert.Equal(zaehlt, Waermegestehung.PositionZaehlt(komponente, anlagentyp, bhkw));
        }

        [Fact]
        public void Die_Erloesreihen_der_Waermeerzeuger()
        {
            Assert.True(Waermegestehung.ErloesReiheZaehlt(KapitalwertRechner.ErloesReihe.KWKG));
            Assert.True(Waermegestehung.ErloesReiheZaehlt(KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE));
            Assert.True(Waermegestehung.ErloesReiheZaehlt(KapitalwertRechner.ErloesReihe.ENERGIESTEUER));
            // Register EZ‑21: Die Befreiung nach § 9 Abs. 1 Nr. 3 StromStG (Modus ERLOES) steht schon in
            // der Stromgutschrift — der Arbeitspreis, zu dem sie den Eigenstrom gutschreibt, enthält die
            // Stromsteuer. Die Stromsteuer zählt einmal.
            Assert.False(Waermegestehung.ErloesReiheZaehlt(KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG));
            Assert.False(Waermegestehung.ErloesReiheZaehlt(KapitalwertRechner.ErloesReihe.PV_VERGUETUNG));
            Assert.False(Waermegestehung.ErloesReiheZaehlt(KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG));
            Assert.False(Waermegestehung.ErloesReiheZaehlt(null));
        }

        // ------------------------------------------------------------ Energie

        [Theory]
        [InlineData(0.0, 100.0, 0.0)]
        [InlineData(10.0, 0.0, 1.0)]
        [InlineData(10.0, 10.0, 1.0)]
        [InlineData(25.0, 100.0, 0.25)]
        [InlineData(50.0, 40.0, 1.0)]
        public void Der_Anteil_an_Grund_und_Leistungspreis_folgt_der_Jahresmenge(double waerme, double gesamt, double anteil)
        {
            Assert.Equal(anteil, Waermegestehung.Anteil(waerme, gesamt), 12);
        }

        [Fact]
        public void Die_Energiekosten_der_Waermeerzeuger_nehmen_je_Traeger_Arbeit_und_Anteil()
        {
            var traeger = new List<EnergieTraegerNachweis>
            {
                // Brennstoff: gehört ganz den Wärmeerzeugern.
                new EnergieTraegerNachweis { Traeger = "Gas", ArbeitEur = 1000, WaermeArbeitEur = 1000,
                                             GrundpreisEur = 100, WaermeMengeMWh = 10, VerbrauchGesamtMWh = 10 },
                // Strom: 25 von 100 MWh sind Wärmestrom — Arbeit des Wärmestroms, ein Viertel der Festbeträge.
                new EnergieTraegerNachweis { Traeger = "Strom", Netzstrom = true, ArbeitEur = 1400, WaermeArbeitEur = 500,
                                             GrundpreisEur = 200, LeistungEur = 100,
                                             WaermeMengeMWh = 25, VerbrauchGesamtMWh = 100 },
                // Kältestrom eines abweichenden Kühlträgers: zählt nicht.
                new EnergieTraegerNachweis { Traeger = "Kühlung", ArbeitEur = 300, GrundpreisEur = 50 }
            };
            Assert.Equal(1000 + 100 + 500 + 300 * 0.25, Waermegestehung.Energiekosten(traeger).Value, 9);
            Assert.Null(Waermegestehung.Energiekosten(null));
        }

        [Fact]
        public void Waermestrom_und_Stromverbrauch_kommen_aus_den_Erzeugerzeilen()
        {
            var m = new ErgebnisModel
            {
                Energiebedarf = new ErgebnisEnergiebedarfModel { Strombedarf_Gesamt = 365.0, Stromrestbedarf = 400.0 },
                Waermepumpe = new ErgebnisWaermepumpeModel { Stromverbrauch_WP = 25.0, Stromverbrauch_Heizstab = 5.0,
                                                             Stromverbrauch_Kuehlung = 4.0 },
                Heizkessel = new ErgebnisHeizkesselModel { Stromverbrauch = 10.0 }
            };
            Assert.Equal(40.0, Waermegestehung.WaermestromMWh(m), 12);          // WP + Heizstab + Elektrokessel
            Assert.Equal(409.0, Waermegestehung.StromverbrauchGesamtMWh(m), 12); // + Grundstrom + Kältestrom
            Assert.Equal(0.0, Waermegestehung.WaermestromMWh(null));
        }

        // ------------------------------------------------------------ EZ‑6: Strompreis des eigenen Trägers (P646)

        [Fact]
        public void Der_Waermestrom_je_Erzeugerzeile()
        {
            var m = new ErgebnisModel
            {
                Waermepumpe = new ErgebnisWaermepumpeModel
                {
                    Module =
                    {
                        new ErgebnisWaermepumpeModulModel { Modul = "WP 1 ", Stromverbrauch = 20.0, Heizstab = 5.0,
                                                            Stromverbrauch_Kuehlung = 3.0 },
                        new ErgebnisWaermepumpeModulModel { Modul = "WP 2", Stromverbrauch = 0.0, Heizstab = 0.0 }
                    }
                },
                Heizkessel = new ErgebnisHeizkesselModel
                {
                    Module =
                    {
                        new ErgebnisHeizkesselModulModel { Modul = "E-Kessel", Waerme_Gas = 10.0 },
                        new ErgebnisHeizkesselModulModel { Modul = "Gaskessel", Waerme_Gas = 80.0 }
                    }
                }
            };
            List<KeyValuePair<string, double>> zeilen =
                Waermegestehung.WaermestromJeModul(m, new HashSet<string> { "E-Kessel" });
            // WP 1: Strom + Heizstab, ohne Kältestrom, Name getrimmt; WP 2 ohne Strom fehlt; der
            // Gaskessel ist kein Elektrokessel.
            Assert.Equal(2, zeilen.Count);
            Assert.Equal("WP 1", zeilen[0].Key);
            Assert.Equal(25.0, zeilen[0].Value, 12);
            Assert.Equal("E-Kessel", zeilen[1].Key);
            Assert.Equal(10.0, zeilen[1].Value, 12);
            Assert.Single(Waermegestehung.WaermestromJeModul(m, null));
            Assert.Empty(Waermegestehung.WaermestromJeModul(null, null));
        }

        /// <summary>
        /// Der Wärmestrom einer Anlage mit eigenem Stromträger zählt zu dessen Arbeitspreis, der übrige
        /// zum Preis des Trägers, der den Netzbezug bepreist — zugeordnet oder Rückfallträger. Ohne
        /// eigenen Träger ist es die bisherige Zahl Zeichen für Zeichen.
        /// </summary>
        [Fact]
        public void Der_Waermestrom_nimmt_den_Preis_des_eigenen_Traegers()
        {
            // Ohne Zuordnung: alles zum Netz- bzw. Rückfallpreis — bitgleich.
            Assert.Equal(40.0 * 1000.0 * 0.35, Waermegestehung.WaermestromArbeitEur(40.0, 0.35, null));
            Assert.Equal(40.0 * 1000.0 * 0.35,
                         Waermegestehung.WaermestromArbeitEur(40.0, 0.35, new List<Waermegestehung.EigenerStrom>()));

            // Die Wärmepumpe (25 MWh) mit eigenem Träger zu 0,20 €/kWh, der Rest (15 MWh) zum Netzpreis.
            var eigen = new List<Waermegestehung.EigenerStrom>
            {
                new Waermegestehung.EigenerStrom { Modul = "WP 1", CarrierId = 58, MengeMWh = 25.0, PreisJeKwh = 0.20 }
            };
            Assert.Equal(25.0 * 1000.0 * 0.20 + 15.0 * 1000.0 * 0.35,
                         Waermegestehung.WaermestromArbeitEur(40.0, 0.35, eigen), 9);   // 5.000 + 5.250

            // Ein eigener Träger ohne Arbeitspreis rechnet zum Netzpreis (benannte Grenze).
            eigen[0].PreisJeKwh = null;
            Assert.Equal(40.0 * 1000.0 * 0.35, Waermegestehung.WaermestromArbeitEur(40.0, 0.35, eigen), 9);

            // Mehr eigene Menge als Wärmestrom: höchstens der Wärmestrom des Laufs.
            eigen[0].PreisJeKwh = 0.20;
            eigen[0].MengeMWh = 60.0;
            Assert.Equal(40.0 * 1000.0 * 0.20, Waermegestehung.WaermestromArbeitEur(40.0, 0.35, eigen), 9);
        }

        [Fact]
        public void Der_im_Projekt_verbrauchte_BHKW_Strom()
        {
            var bhkw = new ErgebnisBHKWModel { Stromproduktion = 100.0, Strombedarf = 80.0, Reststrombedarf = 20.0 };
            // Mit Stundenreihen: die Strommatrix (min-Regel).
            Assert.Equal(55.0, Waermegestehung.BhkwEigenstromMWh(new StromMatrix { KwkEigenGesamtMWh = 55.0 }, bhkw), 12);
            // Ohne: der an der BHKW-Stufe gedeckte Bedarf, höchstens die Erzeugung.
            Assert.Equal(60.0, Waermegestehung.BhkwEigenstromMWh(null, bhkw), 12);
            Assert.Equal(30.0, Waermegestehung.BhkwEigenstromMWh(null,
                new ErgebnisBHKWModel { Stromproduktion = 30.0, Strombedarf = 80.0, Reststrombedarf = 20.0 }), 12);
            Assert.Equal(0.0, Waermegestehung.BhkwEigenstromMWh(null, null));
        }

        [Fact]
        public void Die_Stromgutschrift_bewertet_den_Eigenstrom_zum_Arbeitspreis()
        {
            Assert.Equal(60.0 * 1000.0 * 0.25, Waermegestehung.StromgutschriftEur(60.0, 0.25), 9);
            Assert.Equal(0.0, Waermegestehung.StromgutschriftEur(60.0, null));
            Assert.Equal(0.0, Waermegestehung.StromgutschriftEur(0.0, 0.25));
        }

        // ------------------------------------------------------------ § 9b StromStG (Anwenderentscheid 02.10.2026)

        private static SteuerEingabe Steuer(string unternehmensart)
            => new SteuerEingabe { Unternehmensart = unternehmensart };

        [Fact]
        public void Die_entgangene_9b_Entlastung_ist_Eigenstrom_mal_Satz()
        {
            Assert.Equal(73.91 * 20.0, Waermegestehung.Entgangene9bEntlastungEur(73.91, 20.0), 9);
            Assert.Equal(0.0, Waermegestehung.Entgangene9bEntlastungEur(73.91, null));
            Assert.Equal(0.0, Waermegestehung.Entgangene9bEntlastungEur(73.91, 0.0));
            Assert.Equal(0.0, Waermegestehung.Entgangene9bEntlastungEur(73.91, -20.0));
            Assert.Equal(0.0, Waermegestehung.Entgangene9bEntlastungEur(0.0, 20.0));
        }

        /// <summary>
        /// Produzierendes Gewerbe (oder Land- und Forstwirtschaft) mit gepflegtem Satz: je Jahr
        /// −Satz × Eigenstrom, eine NEGATIVE Erlösreihe; Jahr 0 bleibt frei.
        /// </summary>
        [Theory]
        [InlineData(DbWerte.UNTERNEHMENSART_PROD_GEWERBE)]
        [InlineData(DbWerte.UNTERNEHMENSART_LAND_FORST)]
        public void Die_entgangene_9b_Entlastung_ist_eine_negative_Erloesreihe(string art)
        {
            KapitalwertRechner.ErloesReihe r = Waermegestehung.Entgangene9bReihe(
                Steuer(art), 50.0, 12500.0, 20, 2027, jahr => 20.0);
            Assert.NotNull(r);
            Assert.Equal(KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG_ENTGANGEN, r.Name);
            Assert.Equal(21, r.JeJahr.Length);
            Assert.Equal(0.0, r.Wert(0));
            for (int t = 1; t <= 20; t++) Assert.Equal(-1000.0, r.Wert(t), 9);
            Assert.Equal(-1000.0, r.Jahr1, 9);
        }

        /// <summary>
        /// Ohne produzierendes Gewerbe, ohne Steuerpfad, ohne Eigenstrom, ohne Stromgutschrift
        /// (kein Arbeitspreis — nichts zu mindern) oder ohne Satz &gt; 0 entsteht keine Reihe; ohne
        /// Gewerbe wird der Katalog nicht einmal gefragt.
        /// </summary>
        [Fact]
        public void Ohne_Gewerbe_Gutschrift_oder_Satz_entsteht_keine_Reihe()
        {
            int gefragt = 0;
            Func<int, double?> satz = jahr => { gefragt++; return 20.0; };
            Assert.Null(Waermegestehung.Entgangene9bReihe(
                Steuer(DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE), 50.0, 12500.0, 20, 2027, satz));
            Assert.Null(Waermegestehung.Entgangene9bReihe(Steuer(""), 50.0, 12500.0, 20, 2027, satz));
            Assert.Null(Waermegestehung.Entgangene9bReihe(null, 50.0, 12500.0, 20, 2027, satz));
            Assert.Equal(0, gefragt);

            SteuerEingabe prod = Steuer(DbWerte.UNTERNEHMENSART_PROD_GEWERBE);
            Assert.Null(Waermegestehung.Entgangene9bReihe(prod, 0.0, 12500.0, 20, 2027, satz));
            Assert.Null(Waermegestehung.Entgangene9bReihe(prod, 50.0, 0.0, 20, 2027, satz));
            Assert.Null(Waermegestehung.Entgangene9bReihe(prod, 50.0, 12500.0, 20, 2027, null));
            Assert.Null(Waermegestehung.Entgangene9bReihe(prod, 50.0, 12500.0, 20, 2027, jahr => null));
            Assert.Null(Waermegestehung.Entgangene9bReihe(prod, 50.0, 12500.0, 20, 2027, jahr => 0.0));
        }

        /// <summary>
        /// Jahresscharf wie die Steuerreihen: im Jahr t der Satz des Kalenderjahres
        /// Förderbeginn + t − 1; ein Jahr ohne Satz trägt keinen Abzug.
        /// </summary>
        [Fact]
        public void Die_entgangene_9b_Entlastung_ist_jahresscharf()
        {
            var jahre = new List<int>();
            KapitalwertRechner.ErloesReihe r = Waermegestehung.Entgangene9bReihe(
                Steuer(DbWerte.UNTERNEHMENSART_PROD_GEWERBE), 10.0, 2500.0, 10, 2028, jahr =>
                {
                    jahre.Add(jahr);
                    if (jahr == 2033) return null;
                    return jahr < 2030 ? 20.0 : 15.0;
                });
            Assert.Equal(Enumerable.Range(2028, 10), jahre);
            Assert.Equal(-200.0, r.Wert(1), 9);    // 2028
            Assert.Equal(-200.0, r.Wert(2), 9);    // 2029
            Assert.Equal(-150.0, r.Wert(3), 9);    // 2030
            Assert.Equal(0.0, r.Wert(6));          // 2033 ohne Satz
            Assert.Equal(-150.0, r.Wert(10), 9);   // 2037
        }

        // ------------------------------------------------------------ § 9b: Sockelbetrag (EZ‑22, § 6.3 Nr. 40)

        /// <summary>
        /// Der Sockelbetrag wirkt bei kleinem Netzbezug: Die Entlastung OHNE BHKW wäre
        /// (5 + 50) MWh × 20,00 €/MWh − 250 € = 850 €/a, MIT BHKW 5 × 20 − 250 &lt; 0 → 0. Entgangen
        /// sind 850 €/a, nicht 50 × 20 = 1.000 €/a — dieselbe Regel wie die Entlastung des Projekts
        /// (<see cref="SteuerGutschriftRechner.Entlastung9bEur"/>). Reicht auch der Netzbezug ohne
        /// BHKW nicht an den Sockel (0 + 10 MWh × 20 = 200 € &lt; 250 €), entgeht nichts.
        /// </summary>
        [Fact]
        public void Der_Sockelbetrag_mindert_den_9b_Abzug_bei_kleinem_Netzbezug()
        {
            Assert.Equal(850.0, Waermegestehung.Entgangene9bEntlastungEur(50.0, 20.0, 5.0, 250.0), 9);
            Assert.True(Waermegestehung.Entgangene9bEntlastungEur(50.0, 20.0, 5.0, 250.0) < 50.0 * 20.0);
            Assert.Equal(0.0, Waermegestehung.Entgangene9bEntlastungEur(10.0, 20.0, 0.0, 250.0));
            // Genau an der Schwelle: N × s = S — die Entlastung mit BHKW ist 0, ohne BHKW E × s.
            Assert.Equal(1000.0, Waermegestehung.Entgangene9bEntlastungEur(50.0, 20.0, 12.5, 250.0), 9);

            // Dieselbe Größe wie die Differenz der Entlastung des Projekts ohne und mit Eigenstrom.
            foreach (double n in new[] { 0.0, 3.0, 12.0, 12.5, 13.0, 400.0 })
                foreach (double e in new[] { 0.5, 6.0, 50.0 })
                    Assert.Equal(SteuerGutschriftRechner.Entlastung9bEur(n + e, 20.0, 250.0) -
                                 SteuerGutschriftRechner.Entlastung9bEur(n, 20.0, 250.0),
                                 Waermegestehung.Entgangene9bEntlastungEur(e, 20.0, n, 250.0), 9);
        }

        /// <summary>
        /// Trägt der Netzbezug den Sockel schon (387,12 MWh × 20 € &gt; 250 €), ist der Abzug Zeichen
        /// für Zeichen Eigenstrom × Satz — wie ohne Sockel im Katalog (<c>null</c> oder 0), auch bei
        /// Netzbezug 0. Die Gegenprobe der Bitgleichheit: kein Toleranzband.
        /// </summary>
        [Fact]
        public void Ohne_Sockel_oder_mit_getragenem_Sockel_ist_der_9b_Abzug_bitgleich()
        {
            double heute = 73.91 * 20.0;
            Assert.Equal(heute, Waermegestehung.Entgangene9bEntlastungEur(73.91, 20.0));
            Assert.Equal(heute, Waermegestehung.Entgangene9bEntlastungEur(73.91, 20.0, 387.12, 250.0));
            Assert.Equal(heute, Waermegestehung.Entgangene9bEntlastungEur(73.91, 20.0, 0.0, null));
            Assert.Equal(heute, Waermegestehung.Entgangene9bEntlastungEur(73.91, 20.0, 0.0, 0.0));
            Assert.Equal(heute, Waermegestehung.Entgangene9bEntlastungEur(73.91, 20.0, 5.0, null));
        }

        /// <summary>
        /// Die Reihe nimmt den Sockel jahresscharf (Kalenderjahr Förderbeginn + t − 1) gegen den
        /// Netzbezug der Steuereingabe: 2027 und 2028 mit 250 € → (5 + 50) × 20 − 250 = 850 €/a,
        /// 2029 ohne Sockel → 50 × 20 = 1.000 €/a. Ohne Sockelfunktion die Reihe von heute.
        /// </summary>
        [Fact]
        public void Die_9b_Reihe_nimmt_den_Sockel_des_Kalenderjahres()
        {
            var steuer = new SteuerEingabe
            {
                Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                NetzbezugMWh = 5.0
            };
            KapitalwertRechner.ErloesReihe r = Waermegestehung.Entgangene9bReihe(
                steuer, 50.0, 12500.0, 3, 2027, jahr => 20.0, jahr => jahr < 2029 ? 250.0 : (double?)null);
            Assert.Equal(-850.0, r.Wert(1), 9);
            Assert.Equal(-850.0, r.Wert(2), 9);
            Assert.Equal(-1000.0, r.Wert(3), 9);

            KapitalwertRechner.ErloesReihe ohne = Waermegestehung.Entgangene9bReihe(
                steuer, 50.0, 12500.0, 3, 2027, jahr => 20.0);
            for (int t = 1; t <= 3; t++) Assert.Equal(-50.0 * 20.0, ohne.Wert(t));

            // Ganz vom Sockel geschluckt: keine Reihe.
            Assert.Null(Waermegestehung.Entgangene9bReihe(
                new SteuerEingabe { Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE },
                10.0, 2500.0, 3, 2027, jahr => 20.0, jahr => 250.0));
        }

        /// <summary>
        /// Der Rechenkern summiert eine negative Erlösreihe richtig: Kapitalwert und Barwert der
        /// Einnahmen sinken um ihren Barwert, die Gliederung bleibt stimmig. Die Zerlegung des
        /// Zählers führt sie unter Energie (sie mindert die Stromgutschrift) — die Erlöse bleiben
        /// die ohne Reihe, der Zähler geht weiter in der Kennzahl auf, und bei einem festen Satz ist
        /// ihre Annuität genau Satz × Eigenstrom.
        /// </summary>
        [Fact]
        public void Eine_negative_Erloesreihe_mindert_den_Kapitalwert_und_zaehlt_in_der_Zerlegung_zur_Energie()
        {
            KapitalwertRechner.Zahlungsbild Bild(params KapitalwertRechner.ErloesReihe[] reihen)
                => KapitalwertRechner.Rechne(
                    new List<KapitalwertRechner.InvestPosition>
                    { new KapitalwertRechner.InvestPosition { Betrag = 10000, Nutzungsdauer = 20 } },
                    500.0, 8000.0, 1000.0, 3.0, 20, 2.0, 2.0, 0.0,
                    new List<KapitalwertRechner.ErloesReihe>(reihen), 0.0, null);

            KapitalwertRechner.ErloesReihe r9b = Waermegestehung.Entgangene9bReihe(
                Steuer(DbWerte.UNTERNEHMENSART_PROD_GEWERBE), 60.0, 15000.0, 20, 2027, jahr => 20.0);
            KapitalwertRechner.Zahlungsbild ohne = Bild();
            KapitalwertRechner.Zahlungsbild mit = Bild(r9b);

            double barwert = 0;
            for (int t = 1; t <= 20; t++) barwert += -1200.0 * Math.Pow(1.03, -t);
            Assert.Equal(ohne.Kapitalwert + barwert, mit.Kapitalwert, 6);
            Assert.Equal(ohne.BarwertEinnahmen + barwert, mit.BarwertEinnahmen, 6);
            Assert.Equal(ohne.BarwertAusgaben, mit.BarwertAusgaben, 9);
            Zahlungsgliederung g = Zahlungsgliederung.Aus(mit, 3.0);
            Assert.True(g.Stimmig);
            Assert.Equal(1000.0 - 1200.0, g.Bestandteil(Zahlungsgliederung.ERLOESE).Wert(1), 9);

            Waermegestehung.Zerlegung zo = Waermegestehung.Zerlegung.Aus(Zahlungsgliederung.Aus(ohne, 3.0), 100.0, 15000.0);
            Waermegestehung.Zerlegung zm = Waermegestehung.Zerlegung.Aus(g, 100.0, 15000.0, r9b);
            Assert.Equal(0.0, zo.Entgangene9bJahr1);
            Assert.Equal(1200.0, zm.Entgangene9bJahr1, 9);
            Assert.Equal(15000.0, zm.StromgutschriftJahr1);
            Assert.Equal(zo.AnlagenEurJahr, zm.AnlagenEurJahr, 9);
            Assert.Equal(zo.ErloeseEurJahr, zm.ErloeseEurJahr, 6);
            Assert.Equal(zo.EnergieEurJahr + 1200.0, zm.EnergieEurJahr, 6);
            Assert.Equal(Waermegestehung.Kennzahl(mit.Kapitalwert, 3.0, 20, 100.0).Value,
                         zm.ZaehlerEurJahr / zm.WaermeKwh, 9);
        }

        [Fact]
        public void Die_Kennzahl_annuisiert_den_Kapitalwert_je_kWh_Waermebedarf()
        {
            double a = KapitalwertRechner.Annuitaet(0.03, 20);
            Assert.Equal(100000.0 * a / 500000.0, Waermegestehung.Kennzahl(-100000.0, 3.0, 20, 500.0).Value, 12);
            Assert.Null(Waermegestehung.Kennzahl(-100000.0, 3.0, 20, 0.0));
        }

        [Fact]
        public void Die_Zerlegung_geht_in_der_Kennzahl_auf()
        {
            var bild = KapitalwertRechner.Rechne(
                new List<KapitalwertRechner.InvestPosition>
                { new KapitalwertRechner.InvestPosition { Betrag = 10000, Nutzungsdauer = 20 } },
                500.0, 8000.0, 1000.0, 3.0, 20, 2.0, 2.0, 0.0,
                new List<KapitalwertRechner.ErloesReihe>(), 0.0, null);
            Waermegestehung.Zerlegung z = Waermegestehung.Zerlegung.Aus(Zahlungsgliederung.Aus(bild, 3.0), 100.0, 0.0);
            Assert.NotNull(z);
            Assert.True(z.AnlagenEurJahr > 0 && z.EnergieEurJahr > 0 && z.ErloeseEurJahr > 0);
            Assert.Equal(Waermegestehung.Kennzahl(bild.Kapitalwert, 3.0, 20, 100.0).Value,
                         z.ZaehlerEurJahr / z.WaermeKwh, 9);
        }

        // ------------------------------------------------------------ CO₂-Abgabe je Träger

        [Fact]
        public void Die_CO2_Abgabe_wird_nach_der_abgabepflichtigen_Menge_aufgeteilt()
        {
            var quelle = new List<EnergieTraegerNachweis>
            {
                new EnergieTraegerNachweis { Traeger = "Gas", BehgT = 3.0 },
                new EnergieTraegerNachweis { Traeger = "Heizöl", BehgT = 1.0 },
                new EnergieTraegerNachweis { Traeger = "Strom" }
            };
            List<EnergieTraegerNachweis> mit = EnergieTraegerNachweis.MitCo2Abgabe(quelle, 400.0, 0.0);
            Assert.Equal(new[] { 300.0, 100.0, 0.0 }, mit.Select(t => t.Co2AbgabeEur).ToArray());
            // Die Aufstellung der Variante bleibt unberührt — geteilt wird auf Kopien.
            Assert.All(quelle, t => Assert.Equal(0.0, t.Co2AbgabeEur));

            // L13: flüssige Biomasse ohne Nachweis trägt mit Menge × Standardwert.
            var bio = new List<EnergieTraegerNachweis>
            {
                new EnergieTraegerNachweis { Traeger = "Gas", BehgT = 2.0 },
                new EnergieTraegerNachweis { Traeger = "Rapsöl", BiogenBehgMWh = 10.0 }
            };
            List<EnergieTraegerNachweis> l13 = EnergieTraegerNachweis.MitCo2Abgabe(bio, 400.0, 200.0);
            Assert.Equal(200.0, l13[0].Co2AbgabeEur, 9);   // 2 t von 4 t
            Assert.Equal(200.0, l13[1].Co2AbgabeEur, 9);   // 10 MWh × 200 g/kWh = 2 t
        }

        // ------------------------------------------------------------ Herleitung „Menge × Preis"

        [Fact]
        public void Die_Herleitung_nennt_Menge_Arbeitspreis_Festbetraege_und_CO2()
        {
            var de = CultureInfo.GetCultureInfo("de-DE");
            var t = new EnergieTraegerNachweis
            {
                Traeger = "Strom", MengeAbrechnung = 7850.0, Einheit = "kWh", PreisJeEinheit = 0.35,
                GrundpreisEur = 120.0, LeistungEur = 0.0, Co2AbgabeEur = 450.0
            };
            Assert.Equal("7.850 kWh × 0,35 €/kWh + 120 €/a Grundpreis + CO₂ 450 €/a",
                         WirtschaftlichkeitZeilen.TraegerHerleitung(t, de));

            var gas = new EnergieTraegerNachweis
            { Traeger = "Gas", MengeAbrechnung = 99835.2, Einheit = "Nm³", PreisJeEinheit = 0.0812, LeistungEur = 1500.0 };
            Assert.Equal("99.835 Nm³ × 0,0812 €/Nm³ + 1.500 €/a Leistungspreis",
                         WirtschaftlichkeitZeilen.TraegerHerleitung(gas, de));

            // Ohne Arbeitsmenge (eigener Zähler ohne Kältestrom) beginnt die Zeile ohne Plus.
            Assert.Equal("80 €/a Grundpreis", WirtschaftlichkeitZeilen.TraegerHerleitung(
                new EnergieTraegerNachweis { Traeger = "Kühlung", GrundpreisEur = 80.0 }, de));
        }

        private static WirtschaftlichkeitErgebnis Stand(int id, double? energie, params EnergieTraegerNachweis[] traeger)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = -id, IstStamm = id == 1, Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                EnergiekostenJahr = energie, Gestehungskosten = 0.1,
                EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>(traeger)
            };
        }

        [Fact]
        public void Unter_den_Energiekosten_steht_je_Traeger_eine_leise_Herleitungszeile()
        {
            var strom = new EnergieTraegerNachweis
            { Traeger = "Strom", MengeAbrechnung = 1000.0, Einheit = "kWh", PreisJeEinheit = 0.30, ArbeitEur = 300.0 };
            var gas = new EnergieTraegerNachweis
            { Traeger = "Gas", MengeAbrechnung = 2000.0, Einheit = "kWh", PreisJeEinheit = 0.10, ArbeitEur = 200.0 };
            var menge = new List<WirtschaftlichkeitErgebnis>
            {
                Stand(1, 300.0, strom),
                Stand(2, 500.0, strom, gas),
                // Ein gespeicherter Lauf ohne Aufstellung: benannt, nicht still.
                Stand(3, 400.0)
            };
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null, 0, true), menge);
            // Die Berichte (ohne Herleitung) führen die Zeilen nicht — ihre Tafeln bleiben.
            Assert.DoesNotContain(WirtschaftlichkeitZeilen.Kennzahlen(menge, null),
                                  z => z.Schluessel.StartsWith("ENERGIEKOSTEN_TRAEGER_", StringComparison.Ordinal));

            int energie = zeilen.FindIndex(z => z.Schluessel == "ENERGIEKOSTEN");
            WirtZeile zs = zeilen.Single(z => z.Schluessel == "ENERGIEKOSTEN_TRAEGER_STROM");
            WirtZeile zg = zeilen.Single(z => z.Schluessel == "ENERGIEKOSTEN_TRAEGER_GAS");
            Assert.True(zeilen.IndexOf(zs) > energie && zeilen.IndexOf(zg) > zeilen.IndexOf(zs));
            Assert.True(zs.Herleitung && zs.IstText && zs.Einzug == 1);
            Assert.Null(zs.ExcelWert(menge[0]));

            var de = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("1.000 kWh × 0,30 €/kWh", zs.Anzeige(menge[0], de));
            Assert.Equal("—", zg.Anzeige(menge[0], de));                        // Stand 1 führt kein Gas
            Assert.Equal("2.000 kWh × 0,10 €/kWh", zg.Anzeige(menge[1], de));
            Assert.Equal(R.WIRT_ENK_TRAEGER_FEHLT, zs.Anzeige(menge[2], de));
            Assert.Equal("—", zg.Anzeige(menge[2], de));                        // einmal genannt genügt
        }

        [Fact]
        public void Ohne_jede_Aufstellung_sagt_eine_Zeile_dass_die_Herleitung_nachkommt()
        {
            var menge = new List<WirtschaftlichkeitErgebnis> { Stand(1, 400.0), Stand(2, null) };
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null, 0, true), menge);
            WirtZeile z = zeilen.Single(x => x.Schluessel == "ENERGIEKOSTEN_TRAEGER__ALLE");
            var de = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal(R.WIRT_ENK_TRAEGER_FEHLT, z.Anzeige(menge[0], de));
            Assert.Equal("—", z.Anzeige(menge[1], de));                          // ohne Energiekosten nichts
        }

        [Fact]
        public void Die_Waermegestehungskosten_tragen_ihren_Kurztext()
        {
            var menge = new List<WirtschaftlichkeitErgebnis> { Stand(1, 100.0) };
            WirtZeile z = WirtschaftlichkeitZeilen.Kennzahlen(menge, null).Single(x => x.Schluessel == "GESTEHUNGSKOSTEN");
            Assert.Equal(R.WIRT_GESTEHUNG_KURZTEXT, z.Kurztext);
            Assert.Contains("Haushaltsstrom, PV und Stromspeicher zählen nicht", z.Kurztext);
            Assert.Contains("Stromgutschrift für BHKW-Eigenstrom zum Arbeitspreis", z.Kurztext);
            Assert.Contains("entgangene Entlastung nach § 9b StromStG", z.Kurztext);
        }

        // ------------------------------------------------------------ Nachweisumschlag

        [Fact]
        public void Der_Umschlag_traegt_die_Aufstellung_je_Traeger()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>
                {
                    new EnergieTraegerNachweis { CarrierId = 60, Traeger = "Strom", Netzstrom = true, MengeMWh = 7.85,
                                                 MengeAbrechnung = 7850.0, Einheit = "kWh", PreisJeEinheit = 0.35,
                                                 ArbeitEur = 2747.5, GrundpreisEur = 120.0, Co2AbgabeEur = 0.0 }
                }
            };
            string grund;
            string text = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);

            var zurueck = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(text).Uebernimm(zurueck);
            EnergieTraegerNachweis t = Assert.Single(zurueck.EnergiekostenJeTraeger);
            Assert.Equal("Strom", t.Traeger);
            Assert.True(t.Netzstrom);
            Assert.Equal(7850.0, t.MengeAbrechnung);
            Assert.Equal(120.0, t.GrundpreisEur);

            // Eine ältere Fassung kennt die Aufstellung nicht: leere Liste, nie null.
            var alt = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen("nw1:{\"Version\":11}").Uebernimm(alt);
            Assert.NotNull(alt.EnergiekostenJeTraeger);
            Assert.Empty(alt.EnergiekostenJeTraeger);
        }

        /// <summary>
        /// P646 (Befund 6): Ein gespeicherter Lauf mit Nachweisumschlag vor Fassung 13 trägt die
        /// Wärmegestehungskosten nach einer früheren Regel — Fassung 11 mit dem Kapitalwert des ganzen
        /// Projekts, Fassung 12 (#642) mit doppelter Stromsteuer im Modus ERLOES und dem Wärmestrom zum
        /// Netzpreis; die Kennzahl sagt es an der Zelle („… mit der nächsten Rechnung"). Fassung 13 und
        /// ein frischer Lauf nicht, eine Zelle ohne Zahl auch nicht.
        /// </summary>
        [Fact]
        public void Ein_Lauf_vor_Fassung_13_traegt_den_Vermerk_an_der_Kennzahl()
        {
            Assert.Equal(13, ErgebnisNachweisUmschlag.FASSUNG_WAERMEGESTEHUNG);

            var alt = new WirtschaftlichkeitErgebnis { IdProjekt = 1, IstStamm = true, Gestehungskosten = 0.12 };
            ErgebnisNachweisUmschlag.Lesen("nw1:{\"Version\":11}").Uebernimm(alt);
            Assert.True(alt.GestehungAlteFormel);

            var w642 = new WirtschaftlichkeitErgebnis { IdProjekt = 5, Gestehungskosten = 0.115 };
            ErgebnisNachweisUmschlag.Lesen("nw1:{\"Version\":12}").Uebernimm(w642);
            Assert.True(w642.GestehungAlteFormel);

            var neu = new WirtschaftlichkeitErgebnis { IdProjekt = 2, Gestehungskosten = 0.11 };
            ErgebnisNachweisUmschlag.Lesen("nw1:{\"Version\":13}").Uebernimm(neu);
            Assert.False(neu.GestehungAlteFormel);

            var frisch = new WirtschaftlichkeitErgebnis { IdProjekt = 3, Gestehungskosten = 0.10 };
            string grund;
            ErgebnisNachweisUmschlag.Lesen(ErgebnisNachweisUmschlag.Schreiben(frisch, out grund)).Uebernimm(frisch);
            Assert.False(frisch.GestehungAlteFormel);

            var ohneZahl = new WirtschaftlichkeitErgebnis { IdProjekt = 4, GestehungAlteFormel = true };

            var menge = new List<WirtschaftlichkeitErgebnis> { alt, w642, neu, frisch, ohneZahl };
            WirtZeile geste = WirtschaftlichkeitZeilen.Kennzahlen(menge, new TarifParameter(), 0, true)
                                                      .Single(z => z.Schluessel == "GESTEHUNGSKOSTEN");
            Assert.Equal(R.WIRT_GESTEHUNG_ALTER_LAUF, geste.Warnung(alt));
            Assert.Contains("nächsten Rechnung", geste.Warnung(alt));
            Assert.Equal(R.WIRT_GESTEHUNG_ALTER_LAUF, geste.Warnung(w642));
            Assert.Equal("", geste.Warnung(neu));
            Assert.Equal("", geste.Warnung(frisch));
            Assert.Equal("", geste.Warnung(ohneZahl));
        }
    }
}
