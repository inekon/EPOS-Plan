using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E8a — <b>die Gliederung des Zahlungsbilds</b> (Konzept Wirtschaftlichkeit
    /// § 2.11.4 V‑C; Mockup Kategorie 8: ValERI-Block 2 „Zahlungsreihen").
    ///
    /// <para><b>Was diese Fälle festhalten.</b> Die sechs Bestandteile nehmen jede Zahlung
    /// des Bildes genau einmal; ihre Barwerte summieren sich zum Kapitalwert des Rechners,
    /// das Netto je Jahr ist seine Nominalreihe; ein falscher Zins fällt in der
    /// Selbstprüfung auf. Die Gliederungen der drei Läufe des Verlaufs sind Zahl für Zahl
    /// die des gespeicherten Laufs — und eine, die nicht passt, zeigt niemand. Die Hülle
    /// liefert sie erst nach einem Lauf und auch dann über T, wenn der Verlauf auf einem
    /// anderen Horizont steht; die Tafeln der Seite tragen dieselben Barwerte.</para>
    ///
    /// <para>Rechenwirkung hat nichts davon: Die Gliederung liest das fertige Zahlungsbild.
    /// Wer die Testdatenbank braucht, steht in der seriellen Sammlung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZahlungsgliederungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly string WORST = WirtschaftlichkeitSzenario.WORST;
        private static readonly string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private static readonly string BEST = WirtschaftlichkeitSzenario.BEST;

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        // =====================================================================
        //  (1) Die Gliederung EINES Zahlungsbilds
        // =====================================================================

        /// <summary>
        /// <b>Die Barwerte summieren sich zum Kapitalwert des Rechners</b> — mit Zuschuss,
        /// verschobener Erstbeschaffung, Ersatz, Restwert, CO₂-Abgabe, Endenergie-Topf,
        /// KWK-Zuschlag samt Pauschale im Jahr 0 und Preisindex der Ersatzbeschaffung. Die
        /// Gliederung ist stimmig.
        /// </summary>
        [Fact]
        public void Die_Barwerte_der_Bestandteile_summieren_sich_zum_Kapitalwert()
        {
            KapitalwertRechner.Zahlungsbild bild = Bild(3.0);
            Zahlungsgliederung g = Zahlungsgliederung.Aus(bild, 3.0);

            Assert.NotNull(g);
            Assert.Equal(20, g.Jahre);
            Assert.Equal(Zahlungsgliederung.Reihenfolge, g.Bestandteile.Select(b => b.Schluessel).ToArray());
            Assert.Equal(bild.Kapitalwert, g.Kapitalwert);
            Assert.Equal(bild.Kapitalwert, g.SummeBarwerte, 6);
            Assert.True(g.Stimmig);
            Assert.Equal(bild.Kapitalwert, g.BarwertJeJahr().Sum(), 6);
        }

        /// <summary>
        /// Jede Zahlung genau einmal: Die Investition steht nur im Jahr 0 und ist I₀ nach
        /// Zuschussabzug, die Pauschale nach § 9 KWKG ist ein Erlös im Jahr 0, der Restwert
        /// steht nur im Jahr T und trägt den Barwert des Rechners; die Ersatzreihe hat die
        /// Jahre der Ersatzbeschaffungen samt der verschobenen Erstbeschaffung (KD6).
        /// </summary>
        [Fact]
        public void Die_Bestandteile_nehmen_jede_Zahlung_genau_einmal()
        {
            KapitalwertRechner.Zahlungsbild bild = Bild(3.0);
            Zahlungsgliederung g = Zahlungsgliederung.Aus(bild, 3.0);

            Zahlungsbestandteil investition = g.Bestandteil(Zahlungsgliederung.INVESTITION);
            Assert.Equal(new[] { 0 }, investition.Jahre().ToArray());
            Assert.Equal(-(100000.0 + 20000.0 + 30000.0 - 10000.0), investition.Wert(0));
            Assert.Equal(-bild.Investition, investition.Barwert);

            Zahlungsbestandteil erloese = g.Bestandteil(Zahlungsgliederung.ERLOESE);
            Assert.Equal(800.0, erloese.Wert(0));
            Assert.Equal(3000.0 + 2500.0, erloese.Wert(1));
            Assert.Equal(3000.0, erloese.Wert(13));

            Zahlungsbestandteil restwert = g.Bestandteil(Zahlungsgliederung.RESTWERT);
            Assert.Equal(new[] { 20 }, restwert.Jahre().ToArray());
            Assert.Equal(bild.RestwertNominal, restwert.Wert(20));
            Assert.Equal(bild.RestwertBarwert, restwert.Barwert);

            Zahlungsbestandteil ersatz = g.Bestandteil(Zahlungsgliederung.ERSATZ);
            Assert.Equal(new[] { 3, 8, 13, 15, 16 }, ersatz.Jahre().ToArray());
            for (int t = 1; t <= 20; t++) Assert.Equal(-bild.ErsatzJeJahr[t], ersatz.Wert(t));

            Zahlungsbestandteil energie = g.Bestandteil(Zahlungsgliederung.ENERGIE);
            Assert.Equal(-(bild.EnergieJeJahr[5] + bild.BehgJeJahr[5]), energie.Wert(5));
            Assert.Equal(-bild.BetriebJeJahr[5], g.Bestandteil(Zahlungsgliederung.BETRIEB).Wert(5));

            // Die Nominalsumme ist die Summe über die Jahre, mit Vorzeichen.
            Assert.Equal(ersatz.JeJahr.Sum(), ersatz.Nominal);
            Assert.True(ersatz.Nominal < 0);
        }

        /// <summary>
        /// Das Netto je Jahr ist die Nominalreihe des Bildes — im Jahr T zuzüglich des
        /// nominalen Restwerts, der dort nicht steht.
        /// </summary>
        [Fact]
        public void Das_Netto_je_Jahr_ist_die_Nominalreihe_des_Bildes()
        {
            KapitalwertRechner.Zahlungsbild bild = Bild(3.0);
            double[] netto = Zahlungsgliederung.Aus(bild, 3.0).NettoJeJahr();

            Assert.Equal(21, netto.Length);
            for (int t = 0; t < 20; t++) Assert.Equal(bild.NominalReihe[t], netto[t], 6);
            Assert.Equal(bild.NominalReihe[20] + bild.RestwertNominal, netto[20], 6);
        }

        /// <summary>
        /// GEGENPROBE: Mit einem anderen Zins abgezinst, summieren sich die Barwerte nicht zum
        /// Kapitalwert — die Gliederung ist nicht stimmig, und niemand zeigt sie.
        /// </summary>
        [Fact]
        public void Ein_anderer_Zins_ist_nicht_stimmig()
        {
            Assert.False(Zahlungsgliederung.Aus(Bild(3.0), 4.0).Stimmig);
            Assert.Null(Zahlungsgliederung.Aus(null, 3.0));
        }

        // =====================================================================
        //  (2) Die Gliederungen der drei Läufe — dieselben Zahlen wie der Lauf
        // =====================================================================

        /// <summary>
        /// Aus einem Verlauf mit den drei Szenarien: je Szenario und Stand eine stimmige
        /// Gliederung mit dem Zins DIESES Szenarios; ohne Abgleich fehlt keine.
        /// </summary>
        [Fact]
        public void Der_Satz_nimmt_je_Szenario_den_Zins_des_Szenarios()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);

            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                foreach (int id in new[] { 900, 901 })
                {
                    Zahlungsgliederung g = satz.Von(id, s);
                    Assert.NotNull(g);
                    Assert.True(g.Stimmig);
                    Assert.Equal(p.FuerSzenario(s).Zinssatz, g.ZinsProzent);
                }
            Assert.Empty(satz.Abweichend);
            Assert.True(satz.Vollstaendig(901));
            Assert.Equal(4.0, satz.Von(901, WORST).ZinsProzent);
            Assert.Equal(2.0, satz.Von(901, BEST).ZinsProzent);
        }

        /// <summary>
        /// <b>Keine eigene Rechnung</b>: Die Gliederungen aus den drei Läufen des Verlaufs
        /// tragen Zahl für Zahl den Kapitalwert des Laufs, den die Seite zeigt
        /// (<c>Berechne</c> ohne Speichern) — in allen drei Szenarien, für jeden Stand.
        /// </summary>
        [Fact]
        public void Die_Gliederungen_der_drei_Laeufe_sind_die_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = Parametersatz1040();
            WirtschaftlichkeitVerlaufSzenarien verlauf =
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(Gruppe1040(), p, 20);
            List<WirtschaftlichkeitErgebnis> lauf =
                new WirtschaftlichkeitCtrl().Berechne(Gruppe1040(), Parametersatz1040(), 0, false);

            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(verlauf, p, lauf);

            Assert.Empty(satz.Abweichend);
            int geprueft = 0;
            foreach (WirtschaftlichkeitErgebnis e in lauf.Where(x => x.Kapitalwert.HasValue))
            {
                Zahlungsgliederung g = satz.Von(e.IdProjekt, e.Szenario);
                Assert.NotNull(g);
                Assert.Equal(e.Kapitalwert.Value, g.Kapitalwert, 6);
                Assert.Equal(e.Kapitalwert.Value, g.SummeBarwerte, 6);
                Assert.Equal(e.RestwertBarwert, g.Bestandteil(Zahlungsgliederung.RESTWERT).Barwert, 6);
                geprueft++;
            }
            Assert.True(geprueft >= 3, "Nur " + geprueft + " gerechnete Ergebnisse in der Prüfgruppe.");
        }

        /// <summary>
        /// GEGENPROBE: Passt der gespeicherte Kapitalwert eines Standes nicht (die Parameter sind
        /// seither gespeichert, der Verlauf hat neu simuliert), fehlt seine Gliederung, und er
        /// steht in <see cref="Zahlungsgliederungen.Abweichend"/>.
        /// </summary>
        [Fact]
        public void Eine_Gliederung_die_nicht_zum_Lauf_passt_fehlt()
        {
            WirtschaftlichkeitParameter p = Parameter();
            WirtschaftlichkeitVerlaufSzenarien verlauf = Probeverlauf(p);
            var gespeichert = new List<WirtschaftlichkeitErgebnis>();
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                foreach (VerlaufSerie a in verlauf.Lauf(s).Absolut)
                    gespeichert.Add(new WirtschaftlichkeitErgebnis
                    {
                        IdProjekt = a.IdProjekt, Szenario = s,
                        Kapitalwert = a.Bild.Kapitalwert + (a.IdProjekt == 901 && s == ERWARTET ? 250.0 : 0.0)
                    });

            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(verlauf, p, gespeichert);

            Assert.Null(satz.Von(901, ERWARTET));
            Assert.NotNull(satz.Von(901, WORST));
            Assert.NotNull(satz.Von(900, ERWARTET));
            Assert.Equal(new[] { 901 }, satz.Abweichend.ToArray());
            Assert.False(satz.Vollstaendig(901));
        }

        // =====================================================================
        //  (3) Die Hülle und die Tafeln der Seite (Block 2)
        // =====================================================================

        /// <summary>
        /// Die Hülle des Verlaufs liefert die Gliederungen erst nach einem Lauf — und dann über
        /// den Betrachtungszeitraum, auch wenn der Verlauf auf einem anderen Horizont steht.
        /// </summary>
        [Fact]
        public async System.Threading.Tasks.Task Die_Huelle_liefert_die_Gliederungen_erst_nach_einem_Lauf_und_ueber_T()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var huelle = new KapitalwertVerlaufHuelle(1040, "Stammprojekt",
                () => new VerlaufKontext { Gewaehlt = new List<int> { 1040, 1041, 1042 } });
            Assert.Null(huelle.GliederungenUeberT(null));

            huelle.DatenUebernehmen(Gruppe1040());
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(1040);
            Zahlungsgliederungen ueberT = huelle.GliederungenUeberT(null);
            Assert.NotNull(ueberT);
            Assert.Equal(p.Betrachtungszeitraum, ueberT.Jahre);
            Assert.False(ueberT.Leer);

            // Der Verlauf auf einem anderen Horizont — die Gliederungen bleiben über T.
            int anders = p.Betrachtungszeitraum == 25 ? 30 : 25;
            await huelle.Berechnen(anders, VerlaufWahl.Alle, System.Threading.CancellationToken.None);
            Zahlungsgliederungen nachher = huelle.GliederungenUeberT(null);
            Assert.Equal(p.Betrachtungszeitraum, nachher.Jahre);
            foreach (int id in new[] { 1040, 1041, 1042 })
            {
                Zahlungsgliederung vor = ueberT.Von(id, ERWARTET), nach = nachher.Von(id, ERWARTET);
                Assert.Equal(vor == null, nach == null);
                if (vor != null) Assert.Equal(vor.Kapitalwert, nach.Kapitalwert);
            }
        }

        /// <summary>
        /// Block 2: je Stand und Szenario eine Tafel — Jahre 0 … T, die sechs Bestandteile,
        /// Netto und Barwert; darunter die Summe nominal und die Barwerte, deren Summe der
        /// Nettobarwert ist. Ein Jahr ohne Zahlung trägt den Strich.
        /// </summary>
        [Fact]
        public void Die_Tafeln_von_Block_2_tragen_die_Jahresreihen_und_die_Barwerte()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);
            var staende = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(900, "Stamm"), new KeyValuePair<int, string>(901, "Variante")
            };
            string[] szenarien = { ERWARTET, BEST, WORST };

            List<ZahlungsreihenTafel> tafeln = ZahlungsreihenAnsicht.Jahrestafeln(satz, staende, szenarien, DE);

            Assert.Equal(6, tafeln.Count);
            Assert.Equal(new[] { (900, "Stamm"), (901, "Variante") }, ZahlungsreihenAnsicht.Staende(tafeln, staende));
            ZahlungsreihenTafel t = tafeln.Single(x => x.IdStand == 901 && x.Szenario == 0);
            Zahlungsgliederung g = satz.Von(901, ERWARTET);

            Assert.Equal(new[] { "Jahr", "Investition I₀", "Betriebskosten", "Energiekosten", "Erlöse",
                                 "Ersatzbeschaffungen", "Restwert am Ende", "Netto nominal", "Barwert" },
                         t.Tafel.Spalten.ToArray());
            Assert.Equal(21 + 2, t.Tafel.Zeilen.Count);
            Assert.Equal("0", t.Tafel.Zeilen[0].Titel);
            Assert.Equal((-g.Bestandteil(Zahlungsgliederung.INVESTITION).Barwert).ToString("#,##0", DE),
                         t.Tafel.Zeilen[0].Zellen[0].TrimStart('−'));
            Assert.Equal("—", t.Tafel.Zeilen[0].Zellen[1]);            // keine Betriebskosten im Jahr 0

            MatrixZeile summe = t.Tafel.Zeilen[21], barwert = t.Tafel.Zeilen[22];
            Assert.True(summe.IstSumme && barwert.IstSumme);
            Assert.Equal("Summe nominal", summe.Titel);
            Assert.Equal("Barwert", barwert.Titel);
            for (int i = 0; i < 6; i++)
            {
                Zahlungsbestandteil b = g.Bestandteil(Zahlungsgliederung.Reihenfolge[i]);
                Assert.Equal(b.Nominal.ToString(ZahlungsreihenAnsicht.GELD, DE), summe.Zellen[i]);
                Assert.Equal(b.Barwert.ToString(ZahlungsreihenAnsicht.GELD, DE), barwert.Zellen[i]);
            }
            Assert.Equal("", barwert.Zellen[6]);
            Assert.Equal(g.SummeBarwerte.ToString(ZahlungsreihenAnsicht.GELD, DE), barwert.Zellen[7]);
            Assert.Contains("i = 3,00 %", t.Unterzeile);

            // Ohne Gliederungen keine Tafel, kein Hinweis — den Fall sagt die Seite.
            Assert.Empty(ZahlungsreihenAnsicht.Jahrestafeln(null, staende, szenarien, DE));
            Assert.Equal("", ZahlungsreihenAnsicht.Hinweis(null, staende, DE));
        }

        /// <summary>Der Hinweis nennt die Stände, deren Reihen nicht zum gespeicherten Lauf passen.</summary>
        [Fact]
        public void Der_Hinweis_nennt_die_abweichenden_Staende()
        {
            WirtschaftlichkeitParameter p = Parameter();
            WirtschaftlichkeitVerlaufSzenarien verlauf = Probeverlauf(p);
            var gespeichert = verlauf.Lauf(ERWARTET).Absolut
                .Select(a => new WirtschaftlichkeitErgebnis { IdProjekt = a.IdProjekt, Szenario = ERWARTET,
                                                              Kapitalwert = a.Bild.Kapitalwert + 1000.0 })
                .ToList();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(verlauf, p, gespeichert);
            var staende = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(900, "Stamm"), new KeyValuePair<int, string>(901, "Variante")
            };

            string hinweis = ZahlungsreihenAnsicht.Hinweis(satz, staende, DE);

            Assert.StartsWith("Stamm, Variante: ", hinweis);
            Assert.Contains("„Berechnen“", hinweis);
            Assert.Empty(ZahlungsreihenAnsicht.Jahrestafeln(satz, staende, new[] { ERWARTET, BEST, WORST }, DE));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>
        /// Ein Zahlungsbild mit allem, was die Gliederung ordnen muss: vier Positionen (eine mit
        /// Startjahr 3), Zuschuss, Betrieb mit Endenergie-Topf, Energie, CO₂-Abgabe,
        /// Einspeisung, KWK-Zuschlag bis Jahr 12 samt Pauschale im Jahr 0, Preisindex p_I.
        /// </summary>
        private static KapitalwertRechner.Zahlungsbild Bild(double zins)
        {
            var positionen = new List<KapitalwertRechner.InvestPosition>
            {
                new KapitalwertRechner.InvestPosition { Betrag = 100000.0, Nutzungsdauer = 15 },
                new KapitalwertRechner.InvestPosition { Betrag = 20000.0, Nutzungsdauer = 8 },
                new KapitalwertRechner.InvestPosition { Betrag = 30000.0, Nutzungsdauer = 25 },
                new KapitalwertRechner.InvestPosition { Betrag = 5000.0, Nutzungsdauer = 10, StartJahr = 3 }
            };
            var kwkg = new double[21];
            for (int t = 1; t <= 12; t++) kwkg[t] = 2500.0;
            var pauschale = new double[21];
            pauschale[0] = 800.0;
            var reihen = new List<KapitalwertRechner.ErloesReihe>
            {
                new KapitalwertRechner.ErloesReihe(KapitalwertRechner.ErloesReihe.KWKG, kwkg),
                new KapitalwertRechner.ErloesReihe(KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, pauschale)
            };
            return KapitalwertRechner.Rechne(positionen, 4000.0, 20000.0, 3000.0, zins, 20, 2.0, 3.0,
                                             behgJahr: 1500.0, zusatzErloesReihen: reihen, zuschuss: 10000.0,
                                             endenergieJahr: 500.0, preisstInvestProzent: 1.5);
        }

        private static WirtschaftlichkeitParameter Parameter() => new WirtschaftlichkeitParameter
        {
            IdStamm = 900, IdReferenzprojekt = 0, Zinssatz = 3.0, Betrachtungszeitraum = 20,
            PreissteigerungEnergie = 0.0, PreissteigerungBetrieb = 0.0
        };

        /// <summary>
        /// Ein Verlauf mit drei Läufen: Stamm 900 (nur Energie) und Variante 901 (das volle
        /// Bild), je Szenario mit dem Zins des Szenarios gerechnet — so, wie
        /// <c>BerechneVerlauf</c> es tut.
        /// </summary>
        private static WirtschaftlichkeitVerlaufSzenarien Probeverlauf(WirtschaftlichkeitParameter p)
        {
            var verlauf = new WirtschaftlichkeitVerlaufSzenarien { Jahre = 20 };
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                double zins = p.FuerSzenario(s).Zinssatz;
                var lauf = new WirtschaftlichkeitVerlauf { Jahre = 20, Szenario = s };
                KapitalwertRechner.Zahlungsbild stamm = KapitalwertRechner.Rechne(
                    new List<KapitalwertRechner.InvestPosition>(), 0.0, 30000.0, 0.0, zins, 20, 0.0, 3.0);
                lauf.Absolut.Add(new VerlaufSerie { IdProjekt = 900, Anzeige = "Stamm", IstStamm = true, Bild = stamm,
                                                    Kumuliert = Kumuliert(stamm) });
                KapitalwertRechner.Zahlungsbild variante = Bild(zins);
                lauf.Absolut.Add(new VerlaufSerie { IdProjekt = 901, Anzeige = "Variante", Bild = variante,
                                                    Kumuliert = Kumuliert(variante) });
                verlauf.Laeufe[s] = lauf;
            }
            return verlauf;
        }

        private static double[] Kumuliert(KapitalwertRechner.Zahlungsbild b)
        {
            var k = new double[b.BarwertReihe.Length];
            double summe = 0;
            for (int t = 0; t < k.Length; t++) { summe += b.BarwertReihe[t]; k[t] = summe; }
            return k;
        }

        /// <summary>Die Prüfgruppe 1040 bis 1042 — dieselbe wie in den Verlaufsfällen (E6).</summary>
        private static BerichtsDaten Gruppe1040()
        {
            var daten = new BerichtsDaten { IdStamm = 1040, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(1040, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(1041, false, "Variante A", 9000.0));
            daten.Varianten.Add(Stand(1042, false, "Variante B", 7000.0));
            return daten;
        }

        private static WirtschaftlichkeitParameter Parametersatz1040() => new WirtschaftlichkeitParameter
        {
            IdStamm = 1040, IdReferenzprojekt = 0, Zinssatz = 3.0, Betrachtungszeitraum = 20,
            PreissteigerungEnergie = 0.0, PreissteigerungBetrieb = 0.0
        };

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energie)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = new ErgebnisModel(),
                Energiekosten = energie
            };
        }
    }
}
