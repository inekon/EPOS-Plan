using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Vergleichsbericht „synthetisch gegen gemessen" und die Kalibrierung</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 4.1, 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 1,
    /// Punkte 3 und 4).
    ///
    /// <para><b>Alle Reihen sind erfunden</b> — ein rundes Tagesmuster, gleiche Tage; keine
    /// Objektdaten (Kapitel 9 K5), keine Normzahl. Die Proben rechnen ohne Datenbank und ohne
    /// Dienste.</para>
    ///
    /// <para><b>Die Messlatte der Stufe Z5</b>: (a) Energieabweichung exakt, (b) Messspitze im
    /// P85–P95-Band, (c) √N-Skalierung, (d) Formabgleich mit Schwelle als Parameter,
    /// (e) Monatsverteilung — und die Kalibrierung, nach der die Jahresenergie exakt stimmt.</para>
    /// </summary>
    public sealed class MessvergleichTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <inheritdoc />
        public void Dispose() => _kultur.Dispose();

        /// <summary>Der 1. Januar 2025 ist ein Mittwoch — ein voller Wochenzyklus liegt im Jahr.</summary>
        private static readonly DateTime BEGINN = new DateTime(2025, 1, 1);

        /// <summary>Wochentag des 1. Januar 2025 in der Zählung des Kerns (Montag = 0): Mittwoch = 2.</summary>
        private const int WOCHENTAG_JAN1 = 2;

        /// <summary>
        /// Ein erfundenes Tagesmuster: 24 Stundenwerte [kWh], Summe 12 — zwei Spitzen (Morgen und
        /// Abend), Nachtruhe. Keine Normzahl, runde Werte.
        /// </summary>
        private static readonly double[] TAGESMUSTER =
        {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.5, 2.0, 1.0, 0.5, 0.5, 0.5,
            0.5, 0.5, 0.5, 0.5, 0.5, 1.0, 1.5, 0.5, 0.5, 0.0, 0.0, 0.0
        };

        // =================================================================================
        //  (a) bis (e): dieselbe Reihe ist überall neutral
        // =================================================================================

        /// <summary>
        /// <b>Die Gegenprobe des ganzen Berichts:</b> Ist die gemessene Reihe GLEICH der gerechneten,
        /// sind alle fünf Kennzahlen neutral — Energieverhältnis 1, Abweichung 0, Spitze im Band,
        /// √N-Maß = √N · 1, Formmaß 0 und die Monatsanteile gleich. Fällt eine davon aus, ist es der
        /// Vergleich und nicht die Reihe.
        /// </summary>
        [Fact]
        public void Eine_Reihe_gleich_der_Rechnung_ist_ueberall_neutral()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);
            Messreihe gemessen = Jahresmessreihe(1.0);
            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(gemessen, gerechnet, einheiten: 4));

            Assert.True(e.Ok, e.Abbruch?.Klartext);
            Assert.Equal(1.0, e.Energie.Verhaeltnis, 9);
            Assert.Equal(0.0, e.Energie.Abweichung, 9);

            // (b) Das Band: alle Realisierungen tragen genau die gerechnete Spitze.
            Assert.Equal(Spitzenlage.ImBand, e.Band.Lage);
            Assert.Equal(1.0, e.Band.Spitzenverhaeltnis.Value, 9);
            Assert.Equal(1.0, e.Band.BandUnten.Value, 9);
            Assert.Equal(1.0, e.Band.BandOben.Value, 9);
            Assert.Equal(0.85, e.Band.PerzentilUnten, 9);
            Assert.Equal(0.95, e.Band.PerzentilOben, 9);

            // (c) Spitze je Einheit gleich: das Verhaeltnis ist 1, das Wurzel-N-Mass damit sqrt(4).
            Assert.Equal(4, e.WurzelN.Einheiten);
            Assert.Equal(1.0, e.WurzelN.Spitzenverhaeltnis, 9);
            Assert.Equal(0.5, e.WurzelN.WurzelNVerhaeltnis, 9);
            Assert.Equal(2.0, e.WurzelN.Skalierungsmass, 9);

            // (d) Die Form: je Tagtyp Abweichung 0, das Formmass 0, im Rahmen.
            Assert.Equal(3, e.Form.JeTagtyp.Count);
            Assert.All(e.Form.JeTagtyp, t => Assert.Equal(0.0, t.MittlereAbweichung, 12));
            Assert.All(e.Form.JeTagtyp, t => Assert.Equal(0.0, t.VerschobenerAnteil, 12));
            Assert.Equal(0.0, e.Form.Formmass.Value, 12);
            Assert.True(e.Form.ImRahmen);
            Assert.Contains(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_FORM_IM_RAHMEN");
            Assert.All(e.Form.JeTagtyp, t => Assert.Equal(1.0, t.AnteileGemessen.Sum(), 12));
            Assert.All(e.Form.JeTagtyp, t => Assert.Equal(1.0, t.AnteileGerechnet.Sum(), 12));

            // (e) Die Monate: dieselben Anteile, Abweichung 0; jede Seite Summe 1.
            Assert.Equal(1.0, e.Monate.AnteileGemessen.Sum(), 12);
            Assert.Equal(1.0, e.Monate.AnteileGerechnet.Sum(), 12);
            Assert.Equal(0.0, e.Monate.GroessteAbweichung, 12);
        }

        /// <summary>
        /// <b>(a) Energie:</b> Eine um einen festen Faktor skalierte Reihe ergibt GENAU diesen Faktor
        /// als Verhältnis und Faktor − 1 als Abweichung — kein Rundungsspiel. Die Form bleibt
        /// dabei unberührt: Ein Faktor verschiebt keine Stunde.
        /// </summary>
        [Theory]
        [InlineData(1.25)]
        [InlineData(0.8)]
        [InlineData(2.0)]
        public void Eine_skalierte_Reihe_ergibt_die_Energieabweichung_exakt(double faktor)
        {
            Messvergleichsergebnis e = Messvergleich.Vergleichen(
                Eingang(Jahresmessreihe(faktor), Jahresreihe(1.0), einheiten: 9));

            Assert.True(e.Ok);
            Assert.Equal(faktor, e.Energie.Verhaeltnis, 9);
            Assert.Equal(faktor - 1.0, e.Energie.Abweichung, 9);
            // Die Form ist unberuehrt - ein Faktor verschiebt keine Stunde.
            Assert.Equal(0.0, e.Form.Formmass.Value, 12);
            // Auch die Monatsanteile bleiben gleich.
            Assert.Equal(0.0, e.Monate.GroessteAbweichung, 12);
            // Die Spitze folgt dem Faktor.
            Assert.Equal(faktor, e.Band.Spitzenverhaeltnis.Value, 9);
        }

        /// <summary>
        /// <b>(b) Das Band P85–P95:</b> Die Bandgrenzen sind die Perzentile der Stundenspitzen der
        /// Realisierungen. Die Probe legt eine Stichprobe von 100 Werten 1,00 … 1,99 (bezogen auf die
        /// gerechnete Spitze) darüber und dreht die gemessene Spitze über, in und unter das Band —
        /// jedes Mal mit benanntem Vermerk.
        /// </summary>
        [Fact]
        public void Die_Spitze_wird_gegen_das_P85_P95_Band_gehalten()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);
            double spitzeRech = gerechnet.GroessterStundenwertKw;
            // 100 Realisierungen: Spitze 1,00 bis 1,99 der gerechneten. Rang 85 -> 1,84; Rang 95 -> 1,94.
            double[] stichprobe = Enumerable.Range(0, 100).Select(i => spitzeRech * (1.0 + i / 100.0)).ToArray();

            // Im Band: die gemessene Spitze bei 1,9 der gerechneten.
            Messvergleichsergebnis mitte = MitSpitze(gerechnet, stichprobe, 1.9);
            Assert.Equal(1.84, mitte.Band.BandUnten.Value, 9);
            Assert.Equal(1.94, mitte.Band.BandOben.Value, 9);
            Assert.Equal(100, mitte.Band.Realisierungen);
            Assert.Equal(Spitzenlage.ImBand, mitte.Band.Lage);
            Assert.Contains(mitte.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZE_IM_BAND");

            // Oberhalb: die Rechnung unterschaetzt die Spitze.
            Messvergleichsergebnis oben = MitSpitze(gerechnet, stichprobe, 2.5);
            Assert.Equal(Spitzenlage.Oberhalb, oben.Band.Lage);
            Assert.Contains(oben.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZE_UEBER_BAND");

            // Unterhalb: die Rechnung ueberschaetzt sie staerker als erwartet.
            Messvergleichsergebnis unten = MitSpitze(gerechnet, stichprobe, 1.2);
            Assert.Equal(Spitzenlage.Unterhalb, unten.Band.Lage);
            Assert.Contains(unten.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZE_UNTER_BAND");

            // Die Bandgrenzen sind Parameter: mit 0,50 und 0,60 liegt 1,2 mitten im Band.
            Messvergleichsergebnis anders = Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(1.2), gerechnet, einheiten: 1, stichprobe: stichprobe)
                    with { BandUnten = 0.50, BandOben = 0.60 });
            Assert.Equal(1.49, anders.Band.BandUnten.Value, 9);
            Assert.Equal(1.59, anders.Band.BandOben.Value, 9);
            Assert.Equal(Spitzenlage.Unterhalb, anders.Band.Lage);
        }

        /// <summary>
        /// <b>(c) Die √N-Skalierung:</b> Fällt die gemessene Spitze genau um <c>1/√N</c> unter die
        /// gerechnete, ist das Skalierungsmaß exakt 1 — die Überschätzung folgt dem √N-Gesetz. N
        /// kürzt sich im Verhältnis der Spitzen JE EINHEIT heraus; das prüft die Probe an drei
        /// Einheitenzahlen mit.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(25)]
        public void Die_WurzelN_Skalierung_ist_eins_wenn_die_Spitze_dem_Gesetz_folgt(int einheiten)
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);
            double wurzel = Math.Sqrt(einheiten);

            Messvergleichsergebnis e = Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(1.0 / wurzel), gerechnet, einheiten));
            Assert.Equal(einheiten, e.WurzelN.Einheiten);
            Assert.Equal(1.0 / wurzel, e.WurzelN.WurzelNVerhaeltnis, 9);
            Assert.Equal(1.0 / wurzel, e.WurzelN.Spitzenverhaeltnis, 9);
            Assert.Equal(1.0, e.WurzelN.Skalierungsmass, 9);

            // Das Verhaeltnis der Spitzen JE EINHEIT ist dasselbe - N kuerzt sich heraus.
            double jeEinheitMess = e.Band.Spitzenverhaeltnis.Value * gerechnet.GroessterStundenwertKw / einheiten;
            double jeEinheitRech = gerechnet.GroessterStundenwertKw / einheiten;
            Assert.Equal(e.WurzelN.Spitzenverhaeltnis, jeEinheitMess / jeEinheitRech, 9);

            // Ohne Einheitenzahl bleibt die Kennzahl aus - benannt.
            Messvergleichsergebnis ohne = Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(1.0), gerechnet, einheiten: 0));
            Assert.Null(ohne.WurzelN);
            Assert.Contains(ohne.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_EINHEITEN");
        }

        /// <summary>
        /// <b>(d) Der Formabgleich:</b> Eine Messung, die Energie von einer Stunde in eine andere
        /// verschiebt, ergibt ein Formmaß, das die Schwelle überschreitet — und mit einer weiteren
        /// Schwelle liegt dieselbe Messung im Rahmen. <b>Die Schwelle ist der Parameter</b>
        /// (<c>Zapfprofil.Validierung.Formschwelle</c>): Die Probe dreht allein an ihm.
        ///
        /// <para>Gerechnet ist der Abstand nachgemessen: Werden <c>v</c> kWh von Stunde 7 auf
        /// Stunde 12 verschoben, verschieben sich zwei Anteile um je <c>v/Q_d</c> →
        /// <c>Σ|Δ| = 2v/Q_d</c>, mittlere Abweichung <c>2v/(24·Q_d)</c> und verschobener Anteil
        /// <c>v/Q_d</c> — mit der Tagessumme <c>Q_d</c> des Musters. Die Zahlen stehen als Formel,
        /// nicht als Literal.</para>
        /// </summary>
        [Fact]
        public void Der_Formabgleich_faellt_bei_verschobener_Stunde_auf_und_die_Schwelle_entscheidet()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);

            const double v = 2.0;                       // von Stunde 7 nach Stunde 12
            var verschoben = (double[])TAGESMUSTER.Clone();
            verschoben[7] -= v;
            verschoben[12] += v;
            Messreihe gemessen = Jahresmessreihe(1.0, verschoben);

            double tagessumme = TAGESMUSTER.Sum();
            double erwartet = 2.0 * v / tagessumme / Zapfkalender.STUNDEN_TAG;
            Assert.True(erwartet > 0.01 && erwartet < 0.02, "Die Probe trifft die beiden Schwellen nicht.");

            Messvergleichseingang basis = Eingang(gemessen, gerechnet, einheiten: 1);
            Messvergleichsergebnis ueber = Messvergleich.Vergleichen(basis with { Formschwelle = 0.01 });
            Assert.True(ueber.Ok);
            Assert.Equal(erwartet, ueber.Form.Formmass.Value, 9);
            Assert.False(ueber.Form.ImRahmen);
            Assert.Contains(ueber.Hinweise, h => h.Kennung == "MESSVERGLEICH_FORM_UEBER_SCHWELLE");
            Assert.All(ueber.Form.JeTagtyp, t => Assert.Equal(v / tagessumme, t.VerschobenerAnteil, 9));

            // Dieselbe Messung, weitere Schwelle: im Rahmen.
            Messvergleichsergebnis drin = Messvergleich.Vergleichen(basis with { Formschwelle = 0.02 });
            Assert.Equal(erwartet, drin.Form.Formmass.Value, 9);
            Assert.True(drin.Form.ImRahmen);
            Assert.Contains(drin.Hinweise, h => h.Kennung == "MESSVERGLEICH_FORM_IM_RAHMEN");

            // Die Energie ist unberuehrt - verschoben, nicht verloren.
            Assert.Equal(1.0, ueber.Energie.Verhaeltnis, 9);
        }

        /// <summary>
        /// <b>(e) Die Monatsverteilung:</b> Verdoppelt die Messung allein den Januar, weicht dessen
        /// Anteil benannt ab, und der Monat der größten Abweichung ist der Januar. Beide Seiten
        /// tragen Anteile (Summe 1), keine Mengen (K5).
        /// </summary>
        [Fact]
        public void Die_Monatsverteilung_nennt_den_Monat_der_groessten_Abweichung()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);

            // Die Messung verdoppelt den Januar (31 Tage), der Rest bleibt.
            var werte = new List<double>();
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                double f = d < 31 ? 2.0 : 1.0;
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) werte.Add(TAGESMUSTER[h] * f);
            }
            var gemessen = new Messreihe("Probe (erfunden)", ZapfMessgroesse.Energie, 60, BEGINN, werte,
                                         "Probe (erfunden)");

            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(gemessen, gerechnet, einheiten: 1));
            Assert.True(e.Ok);
            Assert.Equal(1, e.Monate.GroessterMonat);
            Assert.True(e.Monate.AnteileGemessen[0] > e.Monate.AnteileGerechnet[0]);
            Assert.Equal(1.0, e.Monate.AnteileGemessen.Sum(), 12);
            Assert.Equal(1.0, e.Monate.AnteileGerechnet.Sum(), 12);

            // Der Januar traegt gemessen 62 von 396 Tagesmengen, gerechnet 31 von 365.
            Assert.Equal(62.0 / 396.0, e.Monate.AnteileGemessen[0], 9);
            Assert.Equal(31.0 / 365.0, e.Monate.AnteileGerechnet[0], 9);
            Assert.Equal(62.0 / 396.0 - 31.0 / 365.0, e.Monate.GroessteAbweichung, 9);
            // Die Energie: 396 statt 365 Tagesmengen.
            Assert.Equal(396.0 / 365.0, e.Energie.Verhaeltnis, 9);
        }

        // =================================================================================
        //  Volumenreihe, Ablehnungen, Parameter
        // =================================================================================

        /// <summary>
        /// Eine Volumenreihe wird über die Spreizung zur Energie — dieselbe Umrechnung wie beim
        /// Jahresmesswert in m³/a (4.1); ohne Spreizung ist das eine benannte Ablehnung, keine 0.
        /// </summary>
        [Fact]
        public void Eine_Volumenreihe_braucht_die_Spreizung()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);
            const double spreizung = 35.0;
            double kwhJeM3 = Mengengeruest.EnergieKwh(Mengengeruest.LITER_JE_M3, spreizung);

            // Dieselbe Energie wie die Rechnung, in m³ ausgedrückt.
            var werte = new List<double>();
            for (int d = 0; d < Zapfkalender.TAGE; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) werte.Add(TAGESMUSTER[h] / kwhJeM3);
            var volumen = new Messreihe("Wasserzaehler (erfunden)", ZapfMessgroesse.Volumen, 60, BEGINN, werte,
                                        "Probe (erfunden)");

            Messvergleichsergebnis e = Messvergleich.Vergleichen(
                Eingang(volumen, gerechnet, einheiten: 1) with { SpreizungK = spreizung });
            Assert.True(e.Ok, e.Abbruch?.Klartext);
            Assert.Equal(1.0, e.Energie.Verhaeltnis, 9);
            Assert.Equal(0.0, e.Form.Formmass.Value, 12);

            Messvergleichsergebnis ohne = Messvergleich.Vergleichen(
                Eingang(volumen, gerechnet, einheiten: 1) with { SpreizungK = 0.0 });
            Assert.False(ohne.Ok);
            Assert.NotEmpty(ohne.Abbruch.Klartext);
        }

        /// <summary>Jede Ablehnung des Vergleichs ist benannt — nie eine stille 0.</summary>
        [Fact]
        public void Jede_Ablehnung_des_Vergleichs_ist_benannt()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);
            Messreihe gemessen = Jahresmessreihe(1.0);

            Assert.Equal("MESSVERGLEICH_OHNE_MESSREIHE", Messvergleich.Vergleichen(null).Abbruch.Kennung);
            Assert.Equal("MESSVERGLEICH_OHNE_MESSREIHE",
                         Messvergleich.Vergleichen(new Messvergleichseingang()).Abbruch.Kennung);
            Assert.Equal("MESSVERGLEICH_OHNE_RECHNUNG",
                         Messvergleich.Vergleichen(new Messvergleichseingang { Reihe = gemessen }).Abbruch.Kennung);
            Assert.Equal("MESSVERGLEICH_RECHNUNG_OHNE_MENGE",
                         Messvergleich.Vergleichen(Eingang(gemessen, Bilanzreihe.Null(), 1)).Abbruch.Kennung);
            Assert.Equal("MESSVERGLEICH_KALENDER_RASTER",
                         Messvergleich.Vergleichen(Eingang(gemessen, gerechnet, 1) with { Kalender = new ZapfTagtyp[3] })
                             .Abbruch.Kennung);
            Assert.Equal("MESSVERGLEICH_BAND_UNGUELTIG",
                         Messvergleich.Vergleichen(Eingang(gemessen, gerechnet, 1) with { BandOben = 0.5 })
                             .Abbruch.Kennung);
        }

        /// <summary>
        /// Eine Tagesreihe trägt keine Stundenwerte: (a) und (e) rechnen, (b), (c) und (d) bleiben
        /// benannt offen — <see cref="Spitzenlage.Unbestimmt"/> statt einer erfundenen Antwort.
        /// </summary>
        [Fact]
        public void Eine_Tagesreihe_laesst_Spitze_und_Form_benannt_offen()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0);
            double tagesmenge = TAGESMUSTER.Sum();
            var tage = new Messreihe("Tageszaehler (erfunden)", ZapfMessgroesse.Energie, 1440, BEGINN,
                                     Enumerable.Repeat(tagesmenge, Zapfkalender.TAGE).ToArray(), "Probe (erfunden)");

            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(tage, gerechnet, einheiten: 4));
            Assert.True(e.Ok);
            Assert.Equal(1.0, e.Energie.Verhaeltnis, 9);          // (a) rechnet
            Assert.Equal(0.0, e.Monate.GroessteAbweichung, 12);   // (e) rechnet
            Assert.Equal(Spitzenlage.Unbestimmt, e.Band.Lage);    // (b) offen
            Assert.Null(e.Band.Spitzenverhaeltnis);
            Assert.Null(e.WurzelN);                              // (c) offen
            Assert.Null(e.Form.Formmass);                        // (d) offen
            Assert.False(e.Form.ImRahmen);                       // "nicht entschieden", nicht "in Ordnung"
            Assert.Contains(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_STUNDENWERTE");
        }

        /// <summary>
        /// Ohne Ensemble bleibt das Band benannt offen; die übrigen Kennzahlen rechnen weiter.
        /// </summary>
        [Fact]
        public void Ohne_Ensemble_bleibt_das_Band_offen()
        {
            Messvergleichsergebnis e = Messvergleich.Vergleichen(
                Eingang(Jahresmessreihe(1.0), Jahresreihe(1.0), einheiten: 4, stichprobe: new double[0]));
            Assert.True(e.Ok);
            Assert.Equal(Spitzenlage.Unbestimmt, e.Band.Lage);
            Assert.Null(e.Band.BandUnten);
            Assert.Equal(1.0, e.Band.Spitzenverhaeltnis.Value, 9);   // die Spitze selbst steht
            Assert.NotNull(e.WurzelN);
            Assert.Contains(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_ENSEMBLE");
        }

        /// <summary>
        /// <b>Die drei Schwellen kommen aus dem Parametersatz</b> (freier Paketteil): Ein Satz mit
        /// den drei Schlüsseln belegt sie; ein fehlender Schlüssel lässt die Vorgabe stehen — der
        /// Vergleich fällt nicht aus, weil eine Setzung fehlt.
        /// </summary>
        [Fact]
        public void Die_Schwellen_kommen_aus_dem_Parametersatz()
        {
            Messvergleichseingang roh = Eingang(Jahresmessreihe(1.0), Jahresreihe(1.0), einheiten: 1);
            Assert.Same(roh, Messvergleich.AusParametern(roh, null));

            Parametersatz voll = Satz(
                (ZapfParameter.VALIDIERUNG_BAND_UNTEN, 0.7),
                (ZapfParameter.VALIDIERUNG_BAND_OBEN, 0.9),
                (ZapfParameter.VALIDIERUNG_FORMSCHWELLE, 0.05));
            Messvergleichseingang belegt = Messvergleich.AusParametern(roh, voll);
            Assert.Equal(0.7, belegt.BandUnten, 12);
            Assert.Equal(0.9, belegt.BandOben, 12);
            Assert.Equal(0.05, belegt.Formschwelle, 12);

            // Ein Satz ohne die Schluessel laesst jede Vorgabe stehen.
            Messvergleichseingang leer = Messvergleich.AusParametern(roh, Satz((ZapfParameter.WOHNEN_FLAECHE_JE_WE, 70.0)));
            Assert.Equal(roh.BandUnten, leer.BandUnten, 12);
            Assert.Equal(roh.BandOben, leer.BandOben, 12);
            Assert.Equal(roh.Formschwelle, leer.Formschwelle, 12);
        }

        /// <summary>Der Rang eines Quantils: ⌈q · n⌉, mindestens 1, höchstens n.</summary>
        [Fact]
        public void Der_Rang_eines_Quantils_folgt_der_Aufrundung()
        {
            Assert.Equal(1, Messvergleich.Rang(100, 0.0));
            Assert.Equal(1, Messvergleich.Rang(100, 0.001));
            Assert.Equal(85, Messvergleich.Rang(100, 0.85));
            Assert.Equal(95, Messvergleich.Rang(100, 0.95));
            Assert.Equal(100, Messvergleich.Rang(100, 1.0));
            Assert.Equal(100, Messvergleich.Rang(100, 2.0));
            Assert.Equal(9, Messvergleich.Rang(10, 0.85));
            Assert.Equal(1, Messvergleich.Rang(1, 0.85));
        }

        // =================================================================================
        //  Die Kalibrierung
        // =================================================================================

        /// <summary>
        /// <b>Kalibrierung (a):</b> Der Jahreswert der Reihe kalibriert wie ein von Hand gepflegter
        /// Messwert. <b>Nach der Kalibrierung ist die Jahresenergie exakt der Nettomesswert</b> —
        /// bei Grenze 1 die Zapfung, bei Grenze 2 Zapfung plus Zirkulation, bei Grenze 3 abzüglich
        /// Speicherverlust. Der ausgewiesene Faktor passt zum Bezug.
        /// </summary>
        [Fact]
        public void Nach_der_Kalibrierung_stimmt_die_Jahresenergie_exakt()
        {
            Messreihe reihe = Jahresmessreihe(1.0);
            double gemessen = reihe.Menge;
            const double zapfung = 3000.0;
            const double zirkulation = 1000.0;

            // Grenze 1: die Zapfung IST der Messwert, die Zirkulation bleibt.
            Kalibrierergebnis g1 = Messkalibrierung.Kalibrieren(reihe, 0.0, ZapfBilanzgrenze.Zapfstelle, null,
                                                                zapfung, zirkulation, 30, "Zone",
                                                                out ZapfSatz f1);
            Assert.Null(f1);
            Assert.Equal(gemessen, g1.ZapfungKwh, 9);
            Assert.Equal(zirkulation, g1.ZirkulationKwh, 9);
            Assert.Equal(gemessen, g1.MesswertNettoKwh, 9);
            Assert.Equal(gemessen / zapfung, g1.Faktor, 12);

            // Grenze 2: Zapfung UND Zirkulation zusammen ergeben genau den Messwert.
            Kalibrierergebnis g2 = Messkalibrierung.Kalibrieren(reihe, 0.0, ZapfBilanzgrenze.MitVerteilung, null,
                                                                zapfung, zirkulation, 30, "Zone", out ZapfSatz f2);
            Assert.Null(f2);
            Assert.Equal(gemessen, g2.ZapfungKwh + g2.ZirkulationKwh, 9);
            Assert.Equal(gemessen / (zapfung + zirkulation), g2.Faktor, 12);

            // Grenze 3: derselbe Weg mit dem Messwert abzueglich Speicherverlust.
            Kalibrierergebnis g3 = Messkalibrierung.Kalibrieren(reihe, 0.0, ZapfBilanzgrenze.MitSpeicher, 200.0,
                                                                zapfung, zirkulation, 30, "Zone", out ZapfSatz f3);
            Assert.Null(f3);
            Assert.Equal(gemessen - 200.0, g3.ZapfungKwh + g3.ZirkulationKwh, 9);
        }

        /// <summary>
        /// Eine kürzere Reihe wird auf 365 Tage <b>hochgerechnet</b> und das benannt — nie still; eine
        /// Reihe unter der Mindestzahl von Tagen wird benannt abgelehnt. Die Mindestzahl ist ein
        /// Parameter.
        /// </summary>
        [Fact]
        public void Eine_kuerzere_Reihe_wird_benannt_hochgerechnet_eine_zu_kurze_abgelehnt()
        {
            // 100 Tage des Musters.
            Messreihe kurz = Messreihenbau(100, 1.0, TAGESMUSTER);
            var hinweise = new List<ZapfSatz>();
            Messwert m = Messkalibrierung.Jahresmesswert(kurz, 0.0, ZapfBilanzgrenze.Zapfstelle, null, 30,
                                                         out ZapfSatz fehler, hinweise);
            Assert.Null(fehler);
            Assert.Equal(kurz.Menge * Zapfkalender.TAGE / 100.0, m.WertKwh, 9);
            Assert.Contains(hinweise, h => h.Kennung == "MESSKALIBRIERUNG_HOCHGERECHNET");
            Assert.Contains("2025-01-01", m.Zeitraum);

            // Ein ganzes Jahr wird NICHT hochgerechnet.
            var ohne = new List<ZapfSatz>();
            Messwert jahr = Messkalibrierung.Jahresmesswert(Jahresmessreihe(1.0), 0.0, ZapfBilanzgrenze.Zapfstelle,
                                                            null, 30, out _, ohne);
            Assert.Equal(Jahresmessreihe(1.0).Menge, jahr.WertKwh, 9);
            Assert.Empty(ohne);

            // Zu kurz: benannte Ablehnung.
            Assert.Null(Messkalibrierung.Jahresmesswert(Messreihenbau(10, 1.0, TAGESMUSTER), 0.0,
                                                        ZapfBilanzgrenze.Zapfstelle, null, 30, out ZapfSatz kurzab));
            Assert.Equal("MESSKALIBRIERUNG_REIHE_ZU_KURZ", kurzab.Kennung);

            // Die Mindestzahl kommt aus dem Parametersatz; ohne Satz gilt die Vorgabe.
            Assert.Equal(Messkalibrierung.MINDESTTAGE_VORGABE, Messkalibrierung.Mindesttage(null));
            Assert.Equal(60, Messkalibrierung.Mindesttage(Satz((ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE, 60.0))));
            Assert.Equal(1, Messkalibrierung.Mindesttage(Satz((ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE, 0.0))));
        }

        /// <summary>
        /// <b>Kalibrierung (b):</b> Der Vorschlag für eine Nichtwohn-Zone ist normiert
        /// (Wochenfaktoren Σ 1, Tagesgänge je Σ 1), deterministisch (zweimal dieselben Bits) und er
        /// <b>verringert die Abweichung</b>: Der Formabgleich gegen eine Rechnung, die den
        /// vorgeschlagenen Tagesgang fährt, ergibt 0 — vorher lag er über der Schwelle.
        /// </summary>
        [Fact]
        public void Der_Nichtwohn_Vorschlag_ist_normiert_deterministisch_und_verringert_die_Abweichung()
        {
            // Die Messung hat ein ANDERES Tagesmuster als die Rechnung: die Spitze liegt mittags.
            var mittags = new double[Zapfkalender.STUNDEN_TAG];
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) mittags[h] = h >= 10 && h < 16 ? 2.0 : 0.0;
            Messreihe gemessen = Jahresmessreihe(1.0, mittags);
            Bilanzreihe gerechnet = Jahresreihe(1.0);

            // Vorher: die Form weicht deutlich ab.
            Messvergleichsergebnis vorher = Messvergleich.Vergleichen(Eingang(gemessen, gerechnet, einheiten: 1));
            Assert.True(vorher.Form.Formmass.Value > 0.01, "Die Ausgangsform weicht nicht ab.");
            Assert.False(vorher.Form.ImRahmen);

            var hinweise = new List<ZapfSatz>();
            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(gemessen, 0.0, 20.0, 30,
                                                                       out ZapfSatz fehler, hinweise);
            Assert.Null(fehler);
            Assert.Equal(Zapfkalender.TAGE, v.VolleTage);
            Assert.Equal(20.0, v.Bezugsmenge, 12);

            // Normiert: Wochenfaktoren Σ 1, je Tagesgang Σ 1.
            Assert.Equal(Zapfkalender.WOCHENTAGE, v.Wochenfaktoren.Count);
            Assert.Equal(1.0, v.Wochenfaktoren.Sum(), 12);
            Assert.All(v.Wochenfaktoren, f => Assert.Equal(1.0 / Zapfkalender.WOCHENTAGE, f, 12));  // gleiche Tage
            Assert.Equal(3, v.Tagesgaenge.Count);
            Assert.All(v.Tagesgaenge, t => Assert.Equal(1.0, t.Anteile.Sum(), 12));
            Assert.Empty(hinweise);

            // Der Tagesbedarf: die Tagesmenge des Musters, je Einheit geteilt.
            Assert.Equal(mittags.Sum(), v.TagesbedarfKwh, 9);
            Assert.Equal(mittags.Sum() / 20.0, v.TagesbedarfJeEinheitKwh, 9);

            // Deterministisch: zweimal dieselben Zahlen.
            Nichtwohnvorschlag zweit = Messkalibrierung.Nichtwohnparameter(gemessen, 0.0, 20.0, 30, out _);
            Assert.Equal(v.TagesbedarfKwh, zweit.TagesbedarfKwh);
            Assert.Equal(v.Wochenfaktoren, zweit.Wochenfaktoren);
            for (int i = 0; i < v.Tagesgaenge.Count; i++)
                Assert.Equal(v.Tagesgaenge[i].Anteile, zweit.Tagesgaenge[i].Anteile);

            // UND er verringert die Abweichung: Faehrt die Rechnung den vorgeschlagenen
            // Tagesgang, ist das Formmass 0 - der Vorschlag ist die Loesung der kleinsten Quadrate.
            double[] vorgeschlagen = v.Tagesgaenge.Single(t => t.Tagtyp == ZapfTagtyp.Werktag).Anteile
                                      .Select(a => a * v.TagesbedarfKwh).ToArray();
            Messvergleichsergebnis nachher = Messvergleich.Vergleichen(
                Eingang(gemessen, Jahresreihe(1.0, vorgeschlagen), einheiten: 1));
            Assert.Equal(0.0, nachher.Form.Formmass.Value, 12);
            Assert.True(nachher.Form.ImRahmen);
            Assert.True(nachher.Form.Formmass.Value < vorher.Form.Formmass.Value);
        }

        /// <summary>Jede Ablehnung des Vorschlags ist benannt; ein fehlender Wochentag ist ein Hinweis.</summary>
        [Fact]
        public void Jede_Ablehnung_des_Vorschlags_ist_benannt()
        {
            Assert.Equal("MESSKALIBRIERUNG_OHNE_MESSREIHE",
                         Kein(Messkalibrierung.Nichtwohnparameter(null, 0.0, 1.0, 30, out ZapfSatz a), a));
            Assert.Equal("MESSKALIBRIERUNG_OHNE_BEZUGSMENGE",
                         Kein(Messkalibrierung.Nichtwohnparameter(Jahresmessreihe(1.0), 0.0, 0.0, 30, out ZapfSatz b), b));

            var tage = new Messreihe("Tageszaehler (erfunden)", ZapfMessgroesse.Energie, 1440, BEGINN,
                                     Enumerable.Repeat(12.0, Zapfkalender.TAGE).ToArray(), "Probe (erfunden)");
            Assert.Equal("MESSKALIBRIERUNG_OHNE_STUNDENWERTE",
                         Kein(Messkalibrierung.Nichtwohnparameter(tage, 0.0, 1.0, 30, out ZapfSatz c), c));

            Assert.Equal("MESSKALIBRIERUNG_REIHE_ZU_KURZ",
                         Kein(Messkalibrierung.Nichtwohnparameter(Messreihenbau(10, 1.0, TAGESMUSTER), 0.0, 1.0, 30,
                                                                  out ZapfSatz d), d));

            // Vier Tage: die drei fehlenden Wochentage bekommen das Mittel - benannt.
            var hinweise = new List<ZapfSatz>();
            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(Messreihenbau(4, 1.0, TAGESMUSTER), 0.0, 1.0, 4,
                                                                       out ZapfSatz e, hinweise);
            Assert.Null(e);
            Assert.Equal(4, v.VolleTage);
            Assert.Equal(1.0, v.Wochenfaktoren.Sum(), 12);
            Assert.Contains(hinweise, h => h.Kennung == "MESSKALIBRIERUNG_WOCHENTAG_FEHLT");
            // Mittwoch bis Samstag: Sonntag fehlt, also kein Sonntagsgang - benannt.
            Assert.Contains(hinweise, h => h.Kennung == "MESSKALIBRIERUNG_TAGTYP_FEHLT");
            Assert.Equal(2, v.Tagesgaenge.Count);
        }

        // =================================================================================
        //  Helfer
        // =================================================================================

        private static string Kein(object ergebnis, ZapfSatz satz)
        {
            Assert.Null(ergebnis);
            Assert.NotNull(satz);
            Assert.NotEmpty(satz.Klartext);
            return satz.Kennung;
        }

        /// <summary>Die gerechnete Jahresreihe: 365-mal dasselbe Tagesmuster, mit einem Faktor.</summary>
        private static Bilanzreihe Jahresreihe(double faktor, double[] muster = null)
        {
            double[] m = muster ?? TAGESMUSTER;
            var werte = new double[Bilanzreihe.STUNDEN];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte[d * Zapfkalender.STUNDEN_TAG + h] = m[h] * faktor;
            return new Bilanzreihe(werte);
        }

        /// <summary>Die gemessene Jahresreihe: 365 Tage Stundenwerte desselben Musters.</summary>
        private static Messreihe Jahresmessreihe(double faktor, double[] muster = null)
            => Messreihenbau(Zapfkalender.TAGE, faktor, muster ?? TAGESMUSTER);

        private static Messreihe Messreihenbau(int tage, double faktor, double[] muster)
        {
            var werte = new double[tage * Zapfkalender.STUNDEN_TAG];
            for (int d = 0; d < tage; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte[d * Zapfkalender.STUNDEN_TAG + h] = muster[h] * faktor;
            return new Messreihe("Waermemengenzaehler (erfunden)", ZapfMessgroesse.Energie, 60, BEGINN, werte,
                                 "Probe (erfunden)");
        }

        /// <summary>
        /// Eine Messreihe wie die Rechnung, deren Spitze um genau <paramref name="faktor"/> abweicht:
        /// Die GANZE Reihe wird skaliert. Eine einzelne Stunde zu skalieren genügte nicht — bei
        /// einem Faktor unter 1 würde eine andere Stunde zur Spitze, und das Verhältnis wäre nicht
        /// mehr der Faktor. Die Form bleibt dabei unverändert (ein Faktor verschiebt keine Stunde).
        /// </summary>
        private static Messreihe MitSpitzenfaktor(double faktor) => Jahresmessreihe(faktor);

        private static Messvergleichsergebnis MitSpitze(Bilanzreihe gerechnet, double[] stichprobe, double faktor)
            => Messvergleich.Vergleichen(Eingang(MitSpitzenfaktor(faktor), gerechnet, einheiten: 1,
                                                 stichprobe: stichprobe));

        /// <summary>
        /// Der Eingang: Kalender aus <see cref="WOCHENTAG_JAN1"/> ohne Wochenendkennzeichen (der
        /// Kalender des Kerns setzt Samstag und Sonntag selbst) und — sofern nicht anders gesagt —
        /// eine Stichprobe aus zehn Realisierungen, die alle genau die gerechnete Spitze tragen.
        /// </summary>
        private static Messvergleichseingang Eingang(Messreihe reihe, Bilanzreihe gerechnet, int einheiten,
                                                     double[] stichprobe = null)
        {
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(WOCHENTAG_JAN1, Wochenende(), new Ferienfenster[0]);
            return new Messvergleichseingang
            {
                Reihe = reihe,
                Gerechnet = gerechnet,
                Kalender = kalender,
                Einheiten = einheiten,
                SynthetischeStundenspitzenKw = stichprobe
                    ?? Enumerable.Repeat(gerechnet.GroessterStundenwertKw, 10).ToArray()
            };
        }

        /// <summary>365 Kennzeichen: Samstag und Sonntag nach dem Wochentag, kein Feiertag (erfunden).</summary>
        private static bool[] Wochenende()
        {
            var we = new bool[Zapfkalender.TAGE];
            for (int d = 1; d <= Zapfkalender.TAGE; d++)
            {
                int wt = Zapfkalender.Wochentag(WOCHENTAG_JAN1, d);
                we[d - 1] = wt == Zapfkalender.SAMSTAG || wt == Zapfkalender.SONNTAG;
            }
            return we;
        }

        /// <summary>Ein Parametersatz aus Schlüssel und Wert — erfundene Werte, kein Katalog.</summary>
        private static Parametersatz Satz(params (string Schluessel, double Wert)[] werte)
            => Parametersatz.Aus("TEST-Z5", werte.Select(w => new ZapfParameterwert(
                w.Schluessel, w.Wert, "-", new Provenienz("Probe (erfunden)", null, "TEST-Z5", Herkunftsart.Frei))));
    }
}
