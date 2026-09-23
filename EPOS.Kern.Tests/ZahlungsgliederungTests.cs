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

            // Ein gerechneter Stand, zu dem der Verlauf gar keine Reihe trägt (nach dem Lauf
            // angehakt), fehlt ebenso benannt; einer ohne Kapitalwert (Fehlgrund) nicht.
            gespeichert.Add(new WirtschaftlichkeitErgebnis { IdProjekt = 902, Szenario = ERWARTET, Kapitalwert = 5.0 });
            gespeichert.Add(new WirtschaftlichkeitErgebnis { IdProjekt = 903, Szenario = ERWARTET, Fehlgrund = "ohne Preise" });
            Zahlungsgliederungen mitNeuem = Zahlungsgliederungen.Aus(verlauf, p, gespeichert);
            Assert.Equal(new[] { 901, 902 }, mitNeuem.Abweichend.OrderBy(x => x).ToArray());
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
        //  (4) U46 — Differenzspalte und Leitversion
        // =====================================================================

        /// <summary>
        /// <b>Die Differenz geht in der Kapitalwertdifferenz auf</b>: Bestandteil für
        /// Bestandteil die Differenz der Barwerte und der Jahresbeträge; die Summe der
        /// Barwertdifferenzen ist die Differenz der Kapitalwerte.
        /// </summary>
        [Fact]
        public void Die_Differenz_geht_in_der_Kapitalwertdifferenz_auf()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);
            Zahlungsgliederung variante = satz.Von(901, ERWARTET), stamm = satz.Von(900, ERWARTET);

            Zahlungsgliederung d = Zahlungsgliederung.Differenz(variante, stamm);

            Assert.Equal(variante.Kapitalwert - stamm.Kapitalwert, d.Kapitalwert);
            Assert.Equal(d.Kapitalwert, d.SummeBarwerte, 6);
            Assert.True(d.Stimmig);
            foreach (string s in Zahlungsgliederung.Reihenfolge)
            {
                Assert.Equal(variante.Bestandteil(s).Barwert - stamm.Bestandteil(s).Barwert, d.Bestandteil(s).Barwert);
                for (int t = 0; t <= 20; t++)
                    Assert.Equal(variante.Bestandteil(s).Wert(t) - stamm.Bestandteil(s).Wert(t), d.Bestandteil(s).Wert(t));
            }
            Assert.Null(Zahlungsgliederung.Differenz(variante, null));
        }

        /// <summary>
        /// Die <b>Leitversion</b>: der Stand mit der größten Kapitalwertdifferenz im
        /// Erwartungsfall — die anderen Szenarien zählen nicht, die Referenz nie; ohne
        /// Differenz der erste Stand außer der Referenz. In Sicht 2 (Stände A, B, Referenz A)
        /// ist es B.
        /// </summary>
        [Fact]
        public void Die_Leitversion_ist_die_groesste_Differenz_im_Erwartungsfall()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis { IdProjekt = 1, Szenario = ERWARTET, IstStamm = true },
                new WirtschaftlichkeitErgebnis { IdProjekt = 2, Szenario = ERWARTET, KapitalwertDiff = 500.0 },
                new WirtschaftlichkeitErgebnis { IdProjekt = 2, Szenario = BEST, KapitalwertDiff = 5000.0 },
                new WirtschaftlichkeitErgebnis { IdProjekt = 3, Szenario = ERWARTET, KapitalwertDiff = 900.0 },
                new WirtschaftlichkeitErgebnis { IdProjekt = 4, Szenario = ERWARTET, KapitalwertDiff = -100.0 }
            };

            Assert.Equal(3, Zahlungsgliederungen.Leitversion(alle, new[] { 1, 2, 3, 4 }, 1));
            Assert.Equal(2, Zahlungsgliederungen.Leitversion(alle, new[] { 1, 2, 4 }, 1));
            Assert.Equal(4, Zahlungsgliederungen.Leitversion(alle, new[] { 2, 4 }, 2));   // Sicht 2: A = 2, B = 4
            Assert.Equal(5, Zahlungsgliederungen.Leitversion(alle, new[] { 1, 5 }, 1));   // ohne Differenz: der erste
            Assert.Equal(0, Zahlungsgliederungen.Leitversion(alle, new[] { 1 }, 1));
            Assert.Equal(0, Zahlungsgliederungen.Leitversion(alle, null, 1));
        }

        /// <summary>
        /// U46 in der Hülle: die Gliederung des Kapitalwerts — je Bestandteil und Stand der
        /// Barwert und darunter die Nominalsumme (die Investition ohne, sie fließt im Jahr 0),
        /// die Differenzspalte Leitversion − Referenz und der Nettobarwert. Die Differenzspalte
        /// ergibt in der Summe die Kapitalwertdifferenz (bis auf die Rundung der Zellen).
        /// </summary>
        [Fact]
        public void Die_Gliederung_der_Seite_traegt_Nominalsumme_und_Differenzspalte()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);
            var staende = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(900, "Stamm"), new KeyValuePair<int, string>(901, "Variante")
            };

            ErgebnisMatrix m = ZahlungsreihenAnsicht.Bestandteile(satz, ERWARTET, staende, 900, 901, DE);

            Assert.Equal(new[] { "Bestandteil", "Stamm", "Variante", "Differenz Variante − Stamm" }, m.Spalten.ToArray());
            Assert.Equal(7, m.Zeilen.Count);
            Assert.Equal("Investition I₀", m.Zeilen[0].Titel);
            Assert.Equal("nach Zuschussabzug", m.Zeilen[0].Kennzeichen);
            Assert.Equal("", m.Zeilen[0].Unterwert(1));                       // Investition ohne Nominalsumme
            Zahlungsgliederung v = satz.Von(901, ERWARTET), s = satz.Von(900, ERWARTET);
            for (int r = 1; r < 6; r++)
            {
                Zahlungsbestandteil b = v.Bestandteil(Zahlungsgliederung.Reihenfolge[r]);
                Assert.Equal(b.Barwert.ToString(ZahlungsreihenAnsicht.GELD, DE), m.Zeilen[r].Zellen[1]);
                Assert.Equal("nominal " + Math.Abs(b.Nominal).ToString("N0", DE), m.Zeilen[r].Unterwert(1));
                Assert.StartsWith("nominal ", m.Zeilen[r].Unterwert(0));
                Assert.Equal("", m.Zeilen[r].Unterwert(2));                   // die Differenzspalte ohne
            }

            MatrixZeile netto = m.Zeilen[6];
            Assert.True(netto.IstSumme);
            Assert.Equal("Nettobarwert", netto.Titel);
            double dkw = v.Kapitalwert - s.Kapitalwert;
            Assert.Equal(dkw.ToString(ZahlungsreihenAnsicht.GELD, DE), netto.Zellen[2]);

            // Die Differenzspalte geht in der Kapitalwertdifferenz auf.
            double summe = 0;
            for (int r = 0; r < 6; r++) summe += Betrag(m.Zeilen[r].Zellen[2]);
            Assert.True(Math.Abs(summe - Math.Round(dkw)) <= 3, "Summe " + summe + " statt " + dkw);

            // Ohne Leitversion keine Differenzspalte; ein Stand ohne Gliederung trägt „—".
            ErgebnisMatrix ohne = ZahlungsreihenAnsicht.Bestandteile(satz, ERWARTET, staende, 900, 0, DE);
            Assert.Equal(3, ohne.Spalten.Count);
            var mitFremdem = new List<KeyValuePair<int, string>>(staende) { new KeyValuePair<int, string>(999, "Fremd") };
            ErgebnisMatrix fremd = ZahlungsreihenAnsicht.Bestandteile(satz, ERWARTET, mitFremdem, 900, 901, DE);
            Assert.Equal("—", fremd.Zeilen[1].Zellen[2]);
            Assert.Empty(ZahlungsreihenAnsicht.Bestandteile(null, ERWARTET, staende, 900, 901, DE).Zeilen);
            Assert.Contains("i = 3,0 %", ZahlungsreihenAnsicht.Unterzeile(satz, ERWARTET, staende, DE));
            Assert.Contains("i = 4,0 %", ZahlungsreihenAnsicht.Unterzeile(satz, WORST, staende, DE));
        }

        // =====================================================================
        //  (4b) U41 — das Brückenbild
        // =====================================================================

        /// <summary>
        /// U41: <b>Die Brücke stapelt die Differenzspalte</b> — sechs Schritte in der
        /// Reihenfolge der Gliederung mit den Barwertdifferenzen, die Ergebnissäule ist ihre
        /// Summe, die Kapitalwertdifferenz. Das Bild ist ein reines Pixelbild mit festem Maß,
        /// deterministisch; jede Säule nennt ihren Betrag am Element.
        /// </summary>
        [Fact]
        public void Die_Bruecke_stapelt_die_Differenzspalte()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);
            Zahlungsgliederung v = satz.Von(901, ERWARTET), s = satz.Von(900, ERWARTET);

            List<ChartRenderer.Brueckenschritt> schritte = ChartRenderer.Brueckenschritt.Aus(v, s);

            Zahlungsgliederung d = Zahlungsgliederung.Differenz(v, s);
            Assert.Equal(Zahlungsgliederung.Reihenfolge.Select(Zahlungsgliederung.Titel), schritte.Select(x => x.Name));
            Assert.Equal(Zahlungsgliederung.Reihenfolge.Select(k => d.Bestandteil(k).Barwert), schritte.Select(x => x.Wert));
            Assert.Equal(v.Kapitalwert - s.Kapitalwert, schritte.Sum(x => x.Wert), 6);
            Assert.Empty(ChartRenderer.Brueckenschritt.Aus(v, null));

            ChartRenderer.BrueckenTexte texte = ChartRenderer.BrueckenTexte.Fuer("Variante", "Stamm", "Erwartet", v, DE);
            Assert.Equal("Variante gegenüber Stamm · Barwerte · Szenario Erwartet", texte.Unterzeile);
            Assert.Equal("Barwerte gegenüber Stamm, Szenario Erwartet — i = 3,0 %, T = 20 a", texte.Fuss);
            Assert.Equal("Von der Investition zur Kapitalwertdifferenz", texte.Titel);

            WindowsFormsApplication1.Zeichnung.Zeichenmodell m = ChartRenderer.KapitalwertBrueckeModell(schritte, texte);
            Assert.Equal(ChartRenderer.BRUECKE_BREITE, m.Breite);
            Assert.Equal(ChartRenderer.BRUECKE_HOEHE, m.Hoehe);
            Assert.Null(m.Flaeche);
            Assert.Empty(m.Reihen);
            Assert.True(m.Gleicht(ChartRenderer.KapitalwertBrueckeModell(schritte, texte)));
            foreach (ChartRenderer.Brueckenschritt x in schritte)
                Assert.Contains(m.Befehle, b => b.Marke == "reihe:" + x.Name && b.Wert != null &&
                                                b.Wert.StartsWith(x.Name + ": ", StringComparison.Ordinal));
            string ergebnis = (v.Kapitalwert - s.Kapitalwert).ToString("#,##0;−#,##0;0", DE);
            Assert.Contains(m.Befehle, b => b.Marke == "reihe:ΔKW" && b.Wert == "ΔKW: " + ergebnis + " €");
            Assert.Contains(m.Befehle, b => b.Marke == "nulllinie");
            Assert.Contains(m.Befehle, b => b.Marke == "legende");

            // Ohne zeichenbaren Schritt: der Leerhinweis, 1240 × 200.
            var leer = ChartRenderer.KapitalwertBrueckeModell(
                new List<ChartRenderer.Brueckenschritt> { new ChartRenderer.Brueckenschritt { Name = "x", Wert = double.NaN } },
                texte);
            Assert.Equal(200, leer.Hoehe);
            Assert.Contains(leer.Befehle, b => b.Marke == "leerhinweis");
        }

        /// <summary>
        /// U41 in der Hülle: das Bild der Leitversion gegen die Referenz — keines, wenn die
        /// Leitversion die Referenz ist, fehlt oder eine Gliederung fehlt.
        /// </summary>
        [Fact]
        public void Die_Huelle_baut_die_Bruecke_nur_mit_Leitversion_und_Referenz()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);
            var staende = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(900, "Stamm"), new KeyValuePair<int, string>(901, "Variante")
            };

            WindowsFormsApplication1.Zeichnung.Zeichenmodell m =
                ZahlungsreihenAnsicht.Bruecke(satz, ERWARTET, 901, 900, staende, "Erwartet", DE);
            Assert.NotNull(m);
            Assert.Contains(m.Befehle, b => b.Marke == "reihe:Energiekosten");

            Assert.Null(ZahlungsreihenAnsicht.Bruecke(satz, ERWARTET, 900, 900, staende, "Erwartet", DE));
            Assert.Null(ZahlungsreihenAnsicht.Bruecke(satz, ERWARTET, 0, 900, staende, "Erwartet", DE));
            Assert.Null(ZahlungsreihenAnsicht.Bruecke(satz, ERWARTET, 777, 900, staende, "Erwartet", DE));
            Assert.Null(ZahlungsreihenAnsicht.Bruecke(null, ERWARTET, 901, 900, staende, "Erwartet", DE));
        }

        // =====================================================================
        //  (5) U47 — „Was daraus im Lauf wird"
        // =====================================================================

        /// <summary>
        /// U47 in der Hülle: je Szenario (Ungünstig · Erwartet · Günstig) die Investition I₀,
        /// die Jahre der fälligen Ersatzbeschaffungen und der Restwert am Ende, nominal. Ohne
        /// Ersatz „keine", ohne Leitversion oder ohne Gliederung keine Tafel.
        /// </summary>
        [Fact]
        public void Was_daraus_im_Lauf_wird_nennt_I0_Ersatzjahre_und_Restwert_je_Szenario()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(Probeverlauf(p), p, null);

            ErgebnisMatrix m = ZahlungsreihenAnsicht.Laufwirkung(satz, 901, "Variante", DE);

            Assert.Equal(new[] { "Wirkung auf Variante", "Ungünstig", "Erwartet", "Günstig" }, m.Spalten.ToArray());
            Assert.Equal(new[] { "Investition I₀", "Ersatzbeschaffungen fällig im Jahr", "Restwert am Ende, nominal" },
                         m.Zeilen.Select(z => z.Titel).ToArray());
            string[] reihenfolge = { WORST, ERWARTET, BEST };
            for (int i = 0; i < 3; i++)
            {
                Zahlungsgliederung g = satz.Von(901, reihenfolge[i]);
                Assert.Equal("140.000", m.Zeilen[0].Zellen[i]);
                Assert.Equal("3 · 8 · 13 · 15 · 16", m.Zeilen[1].Zellen[i]);
                Assert.Equal(g.Bestandteil(Zahlungsgliederung.RESTWERT).Nominal.ToString("N0", DE), m.Zeilen[2].Zellen[i]);
            }

            Assert.Equal("keine", ZahlungsreihenAnsicht.Laufwirkung(satz, 900, "Stamm", DE).Zeilen[1].Zellen[1]);
            Assert.Empty(ZahlungsreihenAnsicht.Laufwirkung(satz, 0, "", DE).Zeilen);
            Assert.Empty(ZahlungsreihenAnsicht.Laufwirkung(satz, 777, "Fremd", DE).Zeilen);
            Assert.Empty(ZahlungsreihenAnsicht.Laufwirkung(null, 901, "Variante", DE).Zeilen);
        }

        /// <summary>
        /// <b>Der Prüffall von U47: Die drei Spalten gleichen den Szenarioläufen der
        /// Bandbreite.</b> Aus denselben drei Läufen, deren Zahlungsbilder die Tafel trägt,
        /// entstehen die Kapitalwertdifferenzen Ungünstig · Erwartet · Günstig der Bandbreite
        /// (<see cref="WirtschaftlichkeitBandbreite"/>) und die Restwerte der gerechneten
        /// Ergebnisse — Spalte für Spalte in dieser Reihenfolge.
        /// </summary>
        [Fact]
        public void Die_drei_Spalten_gleichen_den_Szenariolaeufen_der_Bandbreite()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = Parametersatz1040();
            List<WirtschaftlichkeitErgebnis> lauf =
                new WirtschaftlichkeitCtrl().Berechne(Gruppe1040(), Parametersatz1040(), 0, false);
            var staende = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(1040, "Stammprojekt"),
                new KeyValuePair<int, string>(1041, "Variante A"),
                new KeyValuePair<int, string>(1042, "Variante B")
            };
            WirtschaftlichkeitBandbreite bandbreite =
                WirtschaftlichkeitBandbreite.Bilde(staende, lauf, 1040, "Stammprojekt");
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(Gruppe1040(), p, 20), p, lauf);
            int leit = Zahlungsgliederungen.Leitversion(lauf, staende.Select(s => s.Key), 1040);
            Assert.True(leit == 1041 || leit == 1042);

            ErgebnisMatrix m = ZahlungsreihenAnsicht.Laufwirkung(satz, leit, "Leitversion", DE);
            BandbreitenZeile zeile = bandbreite.Zeile(leit);
            string[] reihenfolge = { WORST, ERWARTET, BEST };
            double?[] differenz = { zeile.Worst, zeile.Erwartet, zeile.Best };
            for (int i = 0; i < 3; i++)
            {
                Zahlungsgliederung g = satz.Von(leit, reihenfolge[i]), r = satz.Von(1040, reihenfolge[i]);
                Assert.NotNull(g);
                Assert.True(differenz[i].HasValue);
                Assert.Equal(differenz[i].Value, g.Kapitalwert - r.Kapitalwert, 6);
                WirtschaftlichkeitErgebnis e = lauf.Single(x => x.IdProjekt == leit && x.Szenario == reihenfolge[i]);
                Assert.Equal(e.RestwertBarwert, g.Bestandteil(Zahlungsgliederung.RESTWERT).Barwert, 6);
                Assert.Equal((-g.Bestandteil(Zahlungsgliederung.INVESTITION).Wert(0)).ToString("N0", DE), m.Zeilen[0].Zellen[i]);
                Assert.Equal(g.Bestandteil(Zahlungsgliederung.RESTWERT).Nominal.ToString("N0", DE), m.Zeilen[2].Zellen[i]);
            }
        }

        // =====================================================================
        //  (6) U48 — die Fußzeile „Drei Szenarien gerechnet · …"
        // =====================================================================

        /// <summary>
        /// U48: Die Fußzeile nennt, wie viele Szenarien gerechnet sind, und die Herkunft der
        /// Annahmen — ohne Pflege „Annahmen aus Vorgaben, nichts gepflegt", mit gepflegtem Feld
        /// die gepflegten Größen mit den Namen der Annahmentafel (dieselbe Regel).
        /// </summary>
        [Fact]
        public void Die_Fusszeile_nennt_Szenarien_und_Herkunft_der_Annahmen()
        {
            WirtschaftlichkeitParameter p = Parameter();
            Assert.Equal("Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt",
                         ValeriAusweis.Szenarienfuss(3, p, DE));
            Assert.StartsWith("1 von drei Szenarien gerechnet · ", ValeriAusweis.Szenarienfuss(1, p, DE));
            Assert.StartsWith("Noch kein Szenario gerechnet · ", ValeriAusweis.Szenarienfuss(0, p, DE));
            Assert.Equal("Drei Szenarien gerechnet", ValeriAusweis.Szenarienfuss(3, null, DE));

            p.SatzWorst.Zinssatz = 5.0;
            p.SatzBest.NutzungsdauerAenderung = 3.0;
            string gepflegt = ValeriAusweis.Szenarienfuss(3, p, DE);
            Assert.StartsWith("Drei Szenarien gerechnet · Annahmen gepflegt: ", gepflegt);
            List<AnnahmeZeile> tafel = ValeriAusweis.Annahmen(p, DE);
            Assert.Contains(tafel.Single(z => z.Schluessel == AnnahmeZeile.ZINS).Groesse, gepflegt);
            Assert.Contains(tafel.Single(z => z.Schluessel == AnnahmeZeile.DAUER).Groesse, gepflegt);
            Assert.DoesNotContain(tafel.Single(z => z.Schluessel == AnnahmeZeile.INVEST).Groesse, gepflegt);
            Assert.Equal(2, tafel.Count(z => z.Gepflegt));
            Assert.All(tafel.Where(z => z.Gepflegt), z => Assert.Equal("gepflegt", z.Herkunft));
        }

        /// <summary>Ein Betrag der Seite („+1.234", „−567", „0") als Zahl.</summary>
        private static double Betrag(string text)
            => double.Parse(text.Replace("−", "-").Replace("+", ""), NumberStyles.Number, DE);

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
