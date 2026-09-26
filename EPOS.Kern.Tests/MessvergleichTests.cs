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

            // (b) Das Band ist NICHT neutral, und das ist der Kern von Lehre 1 (Konzept 3.6): Die
            // Messspitze soll bei etwa P90 der synthetischen DAUERLINIE liegen, nicht auf ihrem
            // Maximum. Eine Messung, die genau die gerechnete Reihe ist, trifft das Maximum - sie
            // liegt damit OBERHALB des Bandes. Das Tagesmuster wiederholt sich hier an allen 365
            // Tagen, seine Dauerlinie hat darum nur fuenf Stufen (0 / 0,5 / 1,0 / 1,5 / 2,0 kWh mit
            // 2 920 / 4 015 / 730 / 730 / 365 Stunden, also Grenzen bei Rang 2 920, 6 935, 7 665,
            // 8 395 und 8 760): Rang 7 446 (P85) fällt in die Stufe 1,0, Rang 8 322 (P95) in die
            // Stufe 1,5 - das Band ist 0,5 bis 0,75 der gerechneten Spitze 2,0.
            Assert.Equal(Spitzenlage.Oberhalb, e.Band.Lage);
            Assert.Equal(1.0, e.Band.Spitzenverhaeltnis.Value, 9);
            Assert.Equal(0.50, e.Band.BandUnten.Value, 9);
            Assert.Equal(0.75, e.Band.BandOben.Value, 9);
            Assert.Equal(Bilanzreihe.STUNDEN, e.Band.Dauerlinienwerte);
            Assert.Equal(0.85, e.Band.PerzentilUnten, 9);
            Assert.Equal(0.95, e.Band.PerzentilOben, 9);

            // Die Streuung der Realisierungen dagegen IST neutral: alle tragen die gerechnete Spitze.
            Assert.Equal(1.0, e.Streuung.Unten, 9);
            Assert.Equal(1.0, e.Streuung.Oben, 9);
            Assert.Equal(1.0, e.Streuung.Streubreite, 9);
            Assert.Equal(10, e.Streuung.Realisierungen);

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
        /// <b>(b) Das Band P85–P95 ist ein Quantil der DAUERLINIE</b> (Konzept 3.6 / Lehre 1: „Die
        /// Messspitze liegt bei etwa P90 der synthetischen Dauerlinie"). Die Probe rechnet mit einer
        /// <b>physikalisch plausiblen Dauerlinie</b> — dem Formvektor über einem erfundenen
        /// Jahresgang (Winter 1,3 / Sommer 0,7) —, nicht mit einer künstlichen Stichprobe: So hat die
        /// Dauerlinie viele Stufen, und die Grenzen liegen echt auseinander.
        ///
        /// <para>Geprüft wird (1) die Rechenregel gegen eine unabhängige Nachrechnung derselben
        /// Definition samt von Hand gerechnetem Anker, (2) dass beide Grenzen unter 1 liegen und
        /// aufsteigen, (3) dass eine Messung AUF dem gerechneten Maximum oberhalb des Bandes liegt
        /// und eine Messung auf dem P90 der Dauerlinie darin, (4) dass das Band <b>ohne Ensemble
        /// dasselbe</b> ist, und (5) dass die Parameter die Grenzen bewegen.</para>
        /// </summary>
        [Fact]
        public void Die_Spitze_wird_gegen_das_Band_der_Dauerlinie_gehalten()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            double spitzeRech = gerechnet.GroessterStundenwertKw;

            // (1) Die unabhaengige Nachrechnung: sortieren, Rang aufrunden, durch die Spitze teilen.
            double[] dauerlinie = gerechnet.StundenKwh.OrderBy(x => x).ToArray();
            double erwartetUnten = dauerlinie[Aufgerundet(dauerlinie.Length, 0.85) - 1] / spitzeRech;
            double erwartetOben = dauerlinie[Aufgerundet(dauerlinie.Length, 0.95) - 1] / spitzeRech;

            Messvergleichsergebnis mitte = MitSpitze(gerechnet, null, 0.5);
            Assert.Equal(erwartetUnten, mitte.Band.BandUnten.Value, 12);
            Assert.Equal(erwartetOben, mitte.Band.BandOben.Value, 12);
            Assert.Equal(Bilanzreihe.STUNDEN, mitte.Band.Dauerlinienwerte);
            // Die von Hand gerechnete Probe derselben Zahlen (Anker, damit die Nachrechnung nicht
            // einfach die Umsetzung spiegelt): P85 = 0,4146, P95 = 0,6828 der gerechneten Spitze.
            Assert.Equal(0.4146, mitte.Band.BandUnten.Value, 4);
            Assert.Equal(0.6828, mitte.Band.BandOben.Value, 4);

            // (2) Beide Grenzen liegen UNTER 1 und steigen auf - das ist die Aussage von Lehre 1.
            Assert.True(mitte.Band.BandUnten.Value < mitte.Band.BandOben.Value);
            Assert.True(mitte.Band.BandOben.Value < 1.0);

            // (3a) Eine Messung, die genau die gerechnete Reihe ist, trifft das Maximum: oberhalb.
            Messvergleichsergebnis aufMaximum = MitSpitze(gerechnet, null, 1.0);
            Assert.Equal(Spitzenlage.Oberhalb, aufMaximum.Band.Lage);
            Assert.Contains(aufMaximum.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZE_UEBER_BAND");

            // (3b) Eine Messung auf dem P90 der Dauerlinie liegt im Band - die Lehre selbst.
            double p90 = dauerlinie[Aufgerundet(dauerlinie.Length, 0.90) - 1] / spitzeRech;
            Messvergleichsergebnis aufP90 = MitSpitze(gerechnet, null, p90);
            Assert.Equal(Spitzenlage.ImBand, aufP90.Band.Lage);
            Assert.Contains(aufP90.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZE_IM_BAND");

            // (3c) Deutlich darunter: die Rechnung ueberschaetzt die Spitze staerker als erwartet.
            Messvergleichsergebnis darunter = MitSpitze(gerechnet, null, 0.2);
            Assert.Equal(Spitzenlage.Unterhalb, darunter.Band.Lage);
            Assert.Contains(darunter.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZE_UNTER_BAND");

            // (4) OHNE Ensemble ist das Band dasselbe - es braucht nur die gerechnete Reihe.
            Messvergleichsergebnis ohneEnsemble = Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(p90, jahresgang: true), gerechnet, einheiten: 1,
                        stichprobe: new double[0]));
            Assert.Equal(mitte.Band.BandUnten.Value, ohneEnsemble.Band.BandUnten.Value, 12);
            Assert.Equal(mitte.Band.BandOben.Value, ohneEnsemble.Band.BandOben.Value, 12);
            Assert.Equal(Spitzenlage.ImBand, ohneEnsemble.Band.Lage);

            // (5) Die Grenzen sind Parameter: mit 0,50 und 0,60 sinkt das Band, und dieselbe Messung
            //     liegt darueber.
            Messvergleichsergebnis anders = Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(p90, jahresgang: true), gerechnet, einheiten: 1)
                    with { BandUnten = 0.50, BandOben = 0.60 });
            Assert.Equal(dauerlinie[Aufgerundet(dauerlinie.Length, 0.50) - 1] / spitzeRech,
                         anders.Band.BandUnten.Value, 12);
            Assert.Equal(dauerlinie[Aufgerundet(dauerlinie.Length, 0.60) - 1] / spitzeRech,
                         anders.Band.BandOben.Value, 12);
            Assert.Equal(Spitzenlage.Oberhalb, anders.Band.Lage);
        }

        /// <summary>
        /// <b>Die Streuung der Realisierungsspitzen ist die EIGENE Kennzahl des Ensembles</b> und
        /// nicht mehr das Band: Dieselbe Stichprobe 1,00 … 1,99 der gerechneten Spitze ergibt die
        /// Grenzen 1,84 (Rang 85) und 1,94 (Rang 95) und die Streubreite 1,94/1,84 — und sie
        /// verschiebt das Band der Dauerlinie um keine Stelle. Ohne Ensemble ist sie <c>null</c>
        /// („unbestimmt") samt benanntem Hinweis.
        /// </summary>
        [Fact]
        public void Die_Streuung_der_Realisierungsspitzen_steht_neben_dem_Band()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            double spitzeRech = gerechnet.GroessterStundenwertKw;
            double[] stichprobe = Enumerable.Range(0, 100).Select(i => spitzeRech * (1.0 + i / 100.0)).ToArray();

            Messvergleichsergebnis mit = MitSpitze(gerechnet, stichprobe, 0.5);
            Assert.Equal(1.84, mit.Streuung.Unten, 9);
            Assert.Equal(1.94, mit.Streuung.Oben, 9);
            Assert.Equal(1.94 / 1.84, mit.Streuung.Streubreite, 9);
            Assert.Equal(100, mit.Streuung.Realisierungen);
            Assert.Equal(0.85, mit.Streuung.PerzentilUnten, 9);
            Assert.Equal(0.95, mit.Streuung.PerzentilOben, 9);
            Assert.Contains(mit.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZENSTREUUNG");

            // Das Band bleibt, wo es war - die Stichprobe beruehrt es nicht.
            Messvergleichsergebnis ohne = MitSpitze(gerechnet, new double[0], 0.5);
            Assert.Equal(ohne.Band.BandUnten.Value, mit.Band.BandUnten.Value, 12);
            Assert.Equal(ohne.Band.BandOben.Value, mit.Band.BandOben.Value, 12);

            // Ohne Ensemble: unbestimmt, benannt.
            Assert.Null(ohne.Streuung);
            Assert.Contains(ohne.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_ENSEMBLE");
            Assert.DoesNotContain(ohne.Hinweise, h => h.Kennung == "MESSVERGLEICH_SPITZENSTREUUNG");
        }

        /// <summary>Der Rang <c>k = ⌈q · n⌉</c> der Probe selbst — unabhängig von der Umsetzung.</summary>
        private static int Aufgerundet(int anzahl, double quantil)
        {
            int k = (int)Math.Ceiling(quantil * anzahl);
            if (k < 1) k = 1;
            return k > anzahl ? anzahl : k;
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
            Assert.Null(e.Band.BandUnten);                       // ohne Messspitze kein Abgleich
            Assert.NotNull(e.Streuung);                          // die Streuung braucht die Messung nicht
            Assert.Null(e.WurzelN);                              // (c) offen
            Assert.Null(e.Form.Formmass);                        // (d) offen
            Assert.False(e.Form.ImRahmen);                       // "nicht entschieden", nicht "in Ordnung"
            Assert.Contains(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_STUNDENWERTE");
        }

        /// <summary>
        /// <b>Ohne Ensemble rechnet das Band weiter</b> — es ist ein Quantil der gerechneten
        /// Dauerlinie und braucht keine Realisierungen. Allein die <b>Streuung</b> der
        /// Realisierungsspitzen bleibt benannt unbestimmt (<c>null</c> samt
        /// <c>MESSVERGLEICH_OHNE_ENSEMBLE</c>); die übrigen Kennzahlen rechnen ebenfalls weiter.
        /// </summary>
        [Fact]
        public void Ohne_Ensemble_bleibt_allein_die_Streuung_offen()
        {
            Messvergleichsergebnis e = Messvergleich.Vergleichen(
                Eingang(Jahresmessreihe(1.0), Jahresreihe(1.0), einheiten: 4, stichprobe: new double[0]));
            Assert.True(e.Ok);
            Assert.Equal(0.50, e.Band.BandUnten.Value, 9);           // das Band steht
            Assert.Equal(0.75, e.Band.BandOben.Value, 9);
            Assert.Equal(Spitzenlage.Oberhalb, e.Band.Lage);         // die Messung trifft das Maximum
            Assert.Equal(1.0, e.Band.Spitzenverhaeltnis.Value, 9);
            Assert.Null(e.Streuung);                                 // die Streuung nicht
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

        /// <summary>
        /// Der Rang eines Quantils: ⌈q · n⌉, mindestens 1, höchstens n — und es ist die EINE
        /// Rangregel des Zapfprofilgenerators (<c>Perzentilwerte.Rang</c>), keine zweite im
        /// Messvergleich. Die ganzzahlige Form desselben Rangs liefert dieselben Werte.
        /// </summary>
        [Fact]
        public void Der_Rang_eines_Quantils_folgt_der_Aufrundung()
        {
            Assert.Equal(1, Perzentilwerte.Rang(100, 0.0));
            Assert.Equal(1, Perzentilwerte.Rang(100, 0.001));
            Assert.Equal(85, Perzentilwerte.Rang(100, 0.85));
            Assert.Equal(95, Perzentilwerte.Rang(100, 0.95));
            Assert.Equal(100, Perzentilwerte.Rang(100, 1.0));
            Assert.Equal(100, Perzentilwerte.Rang(100, 2.0));
            Assert.Equal(9, Perzentilwerte.Rang(10, 0.85));
            Assert.Equal(1, Perzentilwerte.Rang(1, 0.85));

            // Die ganzzahlige Form ist dieselbe Regel - fuer jede Groesse und die Quantile, mit denen
            // der Bestand rechnet. Sie ist die genauere: q = p/100 ist im Binaersystem meist nicht
            // darstellbar, und bei n = 100 verschiebt das den aufgerundeten Rang fuer p = 7, 14, 28,
            // 55 und 56 um eins. Deshalb rechnet das Ensemble weiter ganzzahlig, und die
            // Bandgrenzen - runde Anteile aus dem Parametersatz - mit der Bruchform.
            foreach (int n in new[] { 1, 7, 10, 100, 365, 8760 })
                foreach ((int p, double q) in new[] { (50, 0.50), (60, 0.60), (85, 0.85), (90, 0.90),
                                                      (95, 0.95), (99, 0.99), (100, 1.00) })
                    Assert.Equal(Perzentilwerte.Rang(n, p), Perzentilwerte.Rang(n, q));
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
        //  Das Teiljahr, der Schalttag und die Feiertage
        // =================================================================================

        /// <summary>
        /// <b>Ein Teiljahr ist kein Fehler</b> (Befund 4): Eine Messung über den Januar wird gegen
        /// die JANUARTAGE der Rechnung gehalten, nicht gegen ihr Jahr. Das Energieverhältnis ist
        /// deshalb 1 (die Reihe IST die Rechnung dieser Tage), <c>MESSVERGLEICH_TEILJAHR</c> nennt die
        /// Zahl der abgedeckten Tage, und die Monatsanteile stehen auf beiden Seiten im Januar auf 1.
        ///
        /// <para>Ohne diese Regel wäre das Verhältnis 31/365 ≈ 0,085 — eine Zahl, die nichts über die
        /// Rechnung sagt, sondern nur über die Länge der Messung.</para>
        /// </summary>
        [Fact]
        public void Ein_Teiljahr_wird_gegen_dieselben_Tage_gehalten()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            // 31 Tage ab 01.01. mit GENAU den Werten der gerechneten Reihe.
            Messreihe januar = Teilmessreihe(gerechnet, BEGINN, 31);

            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(januar, gerechnet, einheiten: 1));
            Assert.True(e.Ok, e.Abbruch?.Klartext);
            Assert.Equal(1.0, e.Energie.Verhaeltnis, 9);
            Assert.Equal(0.0, e.Energie.Abweichung, 9);
            ZapfSatz teil = Assert.Single(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_TEILJAHR");
            Assert.NotEmpty(teil.Klartext);

            // Die Monate: Januar 1 auf beiden Seiten, alle uebrigen 0; keine Abweichung.
            Assert.Equal(1.0, e.Monate.AnteileGemessen[0], 12);
            Assert.Equal(1.0, e.Monate.AnteileGerechnet[0], 12);
            for (int m = 1; m < Zapfkalender.MONATE; m++)
            {
                Assert.Equal(0.0, e.Monate.AnteileGemessen[m], 12);
                Assert.Equal(0.0, e.Monate.AnteileGerechnet[m], 12);
            }
            Assert.Equal(0.0, e.Monate.GroessteAbweichung, 12);
            Assert.Equal(1, e.Monate.GroessterMonat);

            // Die alte Regel haette 31/365 ergeben - die Probe haelt fest, dass sie es NICHT tut.
            Assert.True(e.Energie.Verhaeltnis > 0.5,
                        "Das Verhaeltnis folgt noch der Jahressumme (" + e.Energie.Verhaeltnis + ").");

            // Ein ganzes Jahr traegt den Teiljahr-Hinweis NICHT.
            Messvergleichsergebnis ganz = Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(1.0, jahresgang: true), gerechnet, einheiten: 1));
            Assert.DoesNotContain(ganz.Hinweise, h => h.Kennung == "MESSVERGLEICH_TEILJAHR");
            Assert.Equal(1.0, ganz.Energie.Verhaeltnis, 9);
        }

        /// <summary>
        /// <b>Ein Sommermonat wird ebenfalls gegen seine eigenen Tage gehalten</b> — und zwar gegen
        /// die JULITAGE der Rechnung, die im Jahresgang niedriger liegen als die Januartage. Eine
        /// Messung, die dem gerechneten Juli genau folgt, ergibt deshalb auch im Juli das Verhältnis
        /// 1; gegen die Jahressumme wären es nur wenige Prozent, und die Monatsanteile lägen im
        /// falschen Monat.
        /// </summary>
        [Fact]
        public void Ein_Sommermonat_bezieht_sich_auf_die_Sommertage_der_Rechnung()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            var julibeginn = new DateTime(2025, 7, 1);
            Messreihe juli = Teilmessreihe(gerechnet, julibeginn, 31);

            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(juli, gerechnet, einheiten: 1));
            Assert.True(e.Ok, e.Abbruch?.Klartext);
            Assert.Equal(1.0, e.Energie.Verhaeltnis, 9);
            Assert.Single(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_TEILJAHR");
            Assert.Equal(1.0, e.Monate.AnteileGemessen[6], 12);      // Juli = Index 6
            Assert.Equal(1.0, e.Monate.AnteileGerechnet[6], 12);
            Assert.Equal(7, e.Monate.GroessterMonat);
            Assert.Equal(0.0, e.Monate.GroessteAbweichung, 12);
        }

        /// <summary>
        /// <b>Der 29. Februar hat im Rechenjahr keinen Gegentag</b> (Befund 10): Seine Zeitschritte
        /// fallen aus BEIDEN Seiten heraus, <c>MESSVERGLEICH_SCHALTTAG</c> nennt ihre Zahl — und die
        /// Tage NACH ihm treffen trotzdem ihren richtigen Jahrestag, sonst läge der ganze Rest des
        /// Jahres um einen Tag verschoben.
        /// </summary>
        [Fact]
        public void Ein_Schalttag_bleibt_benannt_ausser_Vergleich()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            // 2024 ist ein Schaltjahr: 20.02. bis 10.03. sind 20 Tage, einer davon der 29.02.
            Messreihe reihe = Teilmessreihe(gerechnet, new DateTime(2024, 2, 20), 20, schaltjahr: true);

            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(reihe, gerechnet, einheiten: 1));
            Assert.True(e.Ok, e.Abbruch?.Klartext);
            ZapfSatz schalt = Assert.Single(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_SCHALTTAG");
            Assert.NotEmpty(schalt.Klartext);
            Assert.Single(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_TEILJAHR");
            // Die 19 vergleichbaren Tage tragen genau die gerechneten Werte: Verhaeltnis 1.
            Assert.Equal(1.0, e.Energie.Verhaeltnis, 9);
        }

        /// <summary>
        /// <b>Der Feiertagsvermerk hängt an jedem vollen Tag</b> (Befund 9), nicht nur an den
        /// Sonntagen: Ein Feiertag am Dienstag zählt hier als Werktag, und die Messung sagt nicht,
        /// dass er einer war. Die Probe nimmt eine Messung über fünf Werktage — ohne einen einzigen
        /// Sonntag — und erwartet den Vermerk.
        /// </summary>
        [Fact]
        public void Der_Feiertagsvermerk_haengt_an_jedem_vollen_Tag()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            // 01.01.2025 ist ein Mittwoch; fuenf Tage ab da sind Mi bis So - deshalb ab dem 06.01.
            // (Montag) fuenf Tage: Montag bis Freitag, kein Samstag, kein Sonntag.
            Messreihe werktage = Teilmessreihe(gerechnet, new DateTime(2025, 1, 6), 5);
            Assert.Equal(DayOfWeek.Monday, werktage.Beginn.DayOfWeek);

            Messvergleichsergebnis e = Messvergleich.Vergleichen(Eingang(werktage, gerechnet, einheiten: 1));
            Assert.True(e.Ok, e.Abbruch?.Klartext);
            Assert.Single(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_FEIERTAGE");
            // Und der Tagtyp eines Datums ist der WERKTAG seines Wochentags, nie ein Sonntag.
            Assert.Equal(ZapfTagtyp.Werktag, Messvergleich.Tagtyp(new DateTime(2025, 5, 1)));    // 1. Mai, Donnerstag
            Assert.Equal(ZapfTagtyp.Werktag, Messvergleich.Tagtyp(new DateTime(2025, 10, 3)));   // 3. Oktober, Freitag
            Assert.Equal(ZapfTagtyp.Samstag, Messvergleich.Tagtyp(new DateTime(2025, 1, 4)));
            Assert.Equal(ZapfTagtyp.SonnFeiertag, Messvergleich.Tagtyp(new DateTime(2025, 1, 5)));
        }

        /// <summary>
        /// <b>Genannte Feiertage zählen im Formabgleich als Sonn-/Feiertag</b> — wie derselbe Tag in
        /// der Rechnung. Fünf Werktage (Mo 06.01. bis Fr 10.01.2025), davon der Mittwoch 08.01.
        /// (Jahrestag 8) als Feiertag genannt: vier Werktage und ein Sonn-/Feiertag der Messung, und
        /// der Vermerk „ohne Feiertage" entfällt. Dazu die Regeln des Tagtyps: ein Feiertag am
        /// Samstag bleibt Samstag, nach dem 29. Februar rückt der Jahrestag zurück, ohne Angabe gilt
        /// der Wochentag (Gegenprobe).
        /// </summary>
        [Fact]
        public void Genannte_Feiertage_zaehlen_im_Formabgleich_als_Sonntag()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            Messreihe werktage = Teilmessreihe(gerechnet, new DateTime(2025, 1, 6), 5);
            Messvergleichseingang eingang = Eingang(werktage, gerechnet, einheiten: 1) with { MessFeiertage = new[] { 8 } };

            Messvergleichsergebnis e = Messvergleich.Vergleichen(eingang);
            Assert.True(e.Ok, e.Abbruch?.Klartext);
            Assert.DoesNotContain(e.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_FEIERTAGE");
            Assert.Equal(4, e.Form.JeTagtyp.Single(t => t.Tagtyp == ZapfTagtyp.Werktag).TageGemessen);
            Assert.Equal(1, e.Form.JeTagtyp.Single(t => t.Tagtyp == ZapfTagtyp.SonnFeiertag).TageGemessen);

            // Gegenprobe: ohne Angabe fuenf Werktage und der Vermerk.
            Messvergleichsergebnis ohne = Messvergleich.Vergleichen(Eingang(werktage, gerechnet, einheiten: 1));
            Assert.Equal(5, ohne.Form.JeTagtyp.Single(t => t.Tagtyp == ZapfTagtyp.Werktag).TageGemessen);
            Assert.Contains(ohne.Hinweise, h => h.Kennung == "MESSVERGLEICH_OHNE_FEIERTAGE");

            Assert.Equal(ZapfTagtyp.SonnFeiertag, Messvergleich.Tagtyp(new DateTime(2025, 5, 1), new[] { 121 }));
            Assert.Equal(ZapfTagtyp.Werktag, Messvergleich.Tagtyp(new DateTime(2025, 5, 1), null));
            Assert.Equal(ZapfTagtyp.Samstag, Messvergleich.Tagtyp(new DateTime(2025, 1, 4), new[] { 4 }));
            // 01.03.2024 (Freitag) ist im Raster des Kerns der Jahrestag 60.
            Assert.Equal(ZapfTagtyp.SonnFeiertag, Messvergleich.Tagtyp(new DateTime(2024, 3, 1), new[] { 60 }));
            Assert.Equal(ZapfTagtyp.Werktag, Messvergleich.Tagtyp(new DateTime(2024, 3, 1), new[] { 61 }));
        }

        /// <summary>
        /// <b>Die Stundenanteile sind die kleinsten Quadrate</b> (Befund 5), nicht der gepoolte
        /// Quotient <c>Σ x / Σ Q</c>: Bei UNGLEICHEN Tagesmengen wiegen sie einen Tag mit
        /// <c>Q_t²</c>, ein großer Tag bestimmt die Form also stärker. Die Probe legt zwei
        /// Werktagsformen übereinander — einen großen Tag mit der einen, einen kleinen mit der
        /// anderen — und rechnet beide Formeln von Hand nach: Sie ergeben verschiedene Zahlen, und
        /// das Ergebnis ist die Lösung der kleinsten Quadrate.
        /// </summary>
        [Fact]
        public void Die_Stundenanteile_sind_die_kleinsten_Quadrate()
        {
            // Zwei Formen, beide mit Tagesmenge 1 je Einheit: Form A traegt alles um 08:00,
            // Form B alles um 18:00. Der grosse Tag traegt 10 kWh, der kleine 1 kWh.
            var gross = new double[Zapfkalender.STUNDEN_TAG];
            var klein = new double[Zapfkalender.STUNDEN_TAG];
            gross[8] = 10.0;
            klein[18] = 1.0;

            // 40 Tage: gerade Tage gross (Form A), ungerade klein (Form B). Ab Montag, damit jeder
            // Wochentag vorkommt; hier zaehlt allein der Werktagsgang.
            var werte = new List<double>();
            var beginn = new DateTime(2025, 1, 6);          // Montag
            for (int i = 0; i < 40; i++)
                werte.AddRange(i % 2 == 0 ? gross : klein);
            var reihe = new Messreihe("Zaehler (erfunden)", ZapfMessgroesse.Energie, 60, beginn,
                                      werte.ToArray(), "Probe (erfunden)");

            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(reihe, 0.0, 1.0, 20, out ZapfSatz fehler);
            Assert.Null(fehler);
            IReadOnlyList<double> a = v.Tagesgaenge.Single(t => t.Tagtyp == ZapfTagtyp.Werktag).Anteile;

            // Von Hand: die Werktage zaehlen. Ueber 40 Tage ab Montag sind 40/7 Wochen; die Probe
            // rechnet die beiden Summen selbst nach, statt die Zahl zu raten.
            double summeQQ = 0.0, qx8 = 0.0, qx18 = 0.0;
            for (int i = 0; i < 40; i++)
            {
                DateTime t = beginn.AddDays(i);
                if (Messvergleich.Tagtyp(t) != ZapfTagtyp.Werktag) continue;
                double[] gang = i % 2 == 0 ? gross : klein;
                double q = gang.Sum();
                summeQQ += q * q;
                qx8 += q * gang[8];
                qx18 += q * gang[18];
            }
            Assert.Equal(qx8 / summeQQ, a[8], 12);
            Assert.Equal(qx18 / summeQQ, a[18], 12);

            // Die Nebenbedingung erfuellt sich von selbst, und nichts ist negativ.
            Assert.Equal(1.0, a.Sum(), 12);
            Assert.All(a, x => Assert.True(x >= 0.0, "Ein Stundenanteil ist negativ: " + x));

            // UND sie ist etwas ANDERES als der gepoolte Quotient - sonst waere der Befund gegenstandslos.
            double summeQ = 0.0, x8 = 0.0;
            for (int i = 0; i < 40; i++)
            {
                DateTime t = beginn.AddDays(i);
                if (Messvergleich.Tagtyp(t) != ZapfTagtyp.Werktag) continue;
                double[] gang = i % 2 == 0 ? gross : klein;
                summeQ += gang.Sum();
                x8 += gang[8];
            }
            Assert.NotEqual(x8 / summeQ, a[8], 6);

            // Die kleinsten Quadrate sind wirklich das Minimum: eine Stoerung in beide Richtungen
            // (auf Summe 1 gehalten) vergroessert die Fehlersumme.
            Assert.True(Fehlersumme(reihe, beginn, gross, klein, a, 0.0) <= Fehlersumme(reihe, beginn, gross, klein, a, +0.01));
            Assert.True(Fehlersumme(reihe, beginn, gross, klein, a, 0.0) <= Fehlersumme(reihe, beginn, gross, klein, a, -0.01));
        }

        /// <summary>
        /// Die Fehlersumme <c>Σ_t Σ_h (x_{t,h} − a_h · Q_t)²</c> über die Werktage, mit
        /// <paramref name="stoerung"/> von Stunde 8 nach Stunde 18 verschoben (Σ a bleibt 1).
        /// </summary>
        private static double Fehlersumme(Messreihe reihe, DateTime beginn, double[] gross, double[] klein,
                                          IReadOnlyList<double> anteile, double stoerung)
        {
            double[] a = anteile.ToArray();
            a[8] += stoerung;
            a[18] -= stoerung;
            double summe = 0.0;
            for (int i = 0; i < 40; i++)
            {
                DateTime t = beginn.AddDays(i);
                if (Messvergleich.Tagtyp(t) != ZapfTagtyp.Werktag) continue;
                double[] gang = i % 2 == 0 ? gross : klein;
                double q = gang.Sum();
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                {
                    double d = gang[h] - a[h] * q;
                    summe += d * d;
                }
            }
            return summe;
        }

        /// <summary>
        /// <b>Eine kurze Reihe wird mit dem JAHRESGANG der Rechnung hochgerechnet</b> (Befund 6),
        /// nicht flach mit <c>· 365 / Tage</c>: Eine Januarmessung wird auf das Jahr geschrieben, indem
        /// sie durch den Anteil geteilt wird, den die Januartage am gerechneten Jahr haben — bei einem
        /// Winterprofil ist das MEHR als 31/365, der Jahreswert also KLEINER als die flache
        /// Hochrechnung. Beide Wege sind benannt, und der Bias steht im Hinweis.
        /// </summary>
        [Fact]
        public void Eine_kurze_Reihe_wird_mit_dem_Jahresgang_hochgerechnet()
        {
            Bilanzreihe gerechnet = Jahresreihe(1.0, jahresgang: true);
            Messreihe januar = Teilmessreihe(gerechnet, BEGINN, 31);

            // Der Anteil der Januartage am gerechneten Jahr - unabhaengig nachgerechnet.
            double anteil = Messkalibrierung.Jahresanteil(januar, gerechnet);
            Assert.True(anteil > 31.0 / Zapfkalender.TAGE,
                        "Der Januar traegt im Winterprofil mehr als seinen Kalenderanteil (" + anteil + ").");

            var mitGang = new List<ZapfSatz>();
            Messwert m = Messkalibrierung.Jahresmesswert(januar, 0.0, ZapfBilanzgrenze.Zapfstelle, null, 30,
                                                         out ZapfSatz fehler, mitGang, gerechnet);
            Assert.Null(fehler);
            Assert.Equal(januar.Menge / anteil, m.WertKwh, 6);
            // Die Messung IST die Rechnung dieser Tage - hochgerechnet ergibt sie deren Jahressumme.
            Assert.Equal(gerechnet.JahressummeKwh, m.WertKwh, 6);
            ZapfSatz hinweis = Assert.Single(mitGang, h => h.Kennung == "MESSKALIBRIERUNG_HOCHGERECHNET_JAHRESGANG");
            Assert.NotEmpty(hinweis.Klartext);
            Assert.DoesNotContain(mitGang, h => h.Kennung == "MESSKALIBRIERUNG_HOCHGERECHNET");

            // OHNE gerechnete Reihe: die flache Hochrechnung, benannt - und sie liegt HOEHER.
            var flach = new List<ZapfSatz>();
            Messwert f = Messkalibrierung.Jahresmesswert(januar, 0.0, ZapfBilanzgrenze.Zapfstelle, null, 30,
                                                         out _, flach);
            Assert.Equal(januar.Menge * Zapfkalender.TAGE / januar.Tage, f.WertKwh, 6);
            Assert.True(f.WertKwh > m.WertKwh,
                        "Die flache Hochrechnung liegt nicht ueber der mit Jahresgang.");
            Assert.Single(flach, h => h.Kennung == "MESSKALIBRIERUNG_HOCHGERECHNET");

            // Ein ganzes Jahr: der Anteil ist 1, und es wird nicht hochgerechnet.
            Messreihe ganz = MitSpitzenfaktor(1.0, jahresgang: true);
            Assert.Equal(1.0, Messkalibrierung.Jahresanteil(ganz, gerechnet), 9);
            var ohne = new List<ZapfSatz>();
            Messwert j = Messkalibrierung.Jahresmesswert(ganz, 0.0, ZapfBilanzgrenze.Zapfstelle, null, 30,
                                                         out _, ohne, gerechnet);
            Assert.Equal(ganz.Menge, j.WertKwh, 9);
            Assert.Empty(ohne);
        }

        // =================================================================================
        //  Helfer
        // =================================================================================

        /// <summary>
        /// Eine Messreihe, die GENAU die gerechneten Stundenwerte der abgedeckten Tage trägt: ab
        /// <paramref name="beginn"/> über <paramref name="tage"/> Kalendertage, Stundenraster. Die
        /// gerechnete Reihe kennt keinen 29. Februar; ein Schaltjahr überspringt ihn in der Rechnung
        /// und trägt an seiner Stelle die Werte des Folgetags — so steht in der Datei ein echter
        /// 29. Februar, den der Vergleich außen vor lassen muss.
        /// </summary>
        private static Messreihe Teilmessreihe(Bilanzreihe gerechnet, DateTime beginn, int tage,
                                               bool schaltjahr = false)
        {
            IReadOnlyList<double> stunden = gerechnet.StundenKwh;
            var werte = new List<double>(tage * Zapfkalender.STUNDEN_TAG);
            for (int i = 0; i < tage; i++)
            {
                DateTime t = beginn.AddDays(i);
                int jahrestag = t.DayOfYear;
                if (schaltjahr && DateTime.IsLeapYear(t.Year) && jahrestag > 60) jahrestag--;
                int d = Math.Min(jahrestag, Zapfkalender.TAGE) - 1;
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte.Add(stunden[d * Zapfkalender.STUNDEN_TAG + h]);
            }
            return new Messreihe("Waermemengenzaehler (erfunden)", ZapfMessgroesse.Energie, 60, beginn,
                                 werte.ToArray(), "Probe (erfunden)");
        }


        private static string Kein(object ergebnis, ZapfSatz satz)
        {
            Assert.Null(ergebnis);
            Assert.NotNull(satz);
            Assert.NotEmpty(satz.Klartext);
            return satz.Kennung;
        }

        /// <summary>Die gerechnete Jahresreihe: 365-mal dasselbe Tagesmuster, mit einem Faktor.</summary>
        /// <summary>
        /// Ein erfundener Jahresgang: 365 Tagesfaktoren, Winter 1,3 und Sommer 0,7, glatt dazwischen
        /// (<c>1 + 0,3 · cos(2π d / 365)</c>). Er dient allein dazu, die synthetische DAUERLINIE mit
        /// vielen verschiedenen Stufen zu versehen — ohne ihn wiederholt sich das Tagesmuster an
        /// allen 365 Tagen, und die Dauerlinie hat nur fünf Stufen. <b>Keine Normzahl</b>: eine
        /// runde Amplitude über einem Kosinus.
        /// </summary>
        private static double Tagesfaktor(int tag) => 1.0 + 0.3 * Math.Cos(2.0 * Math.PI * tag / 365.0);

        private static Bilanzreihe Jahresreihe(double faktor, double[] muster = null, bool jahresgang = false)
        {
            double[] m = muster ?? TAGESMUSTER;
            var werte = new double[Bilanzreihe.STUNDEN];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                double tag = faktor * (jahresgang ? Tagesfaktor(d) : 1.0);
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte[d * Zapfkalender.STUNDEN_TAG + h] = m[h] * tag;
            }
            return new Bilanzreihe(werte);
        }

        /// <summary>Die gemessene Jahresreihe: 365 Tage Stundenwerte desselben Musters.</summary>
        private static Messreihe Jahresmessreihe(double faktor, double[] muster = null, bool jahresgang = false)
            => Messreihenbau(Zapfkalender.TAGE, faktor, muster ?? TAGESMUSTER, jahresgang);

        private static Messreihe Messreihenbau(int tage, double faktor, double[] muster, bool jahresgang = false)
        {
            var werte = new double[tage * Zapfkalender.STUNDEN_TAG];
            for (int d = 0; d < tage; d++)
            {
                double tag = faktor * (jahresgang ? Tagesfaktor(d) : 1.0);
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte[d * Zapfkalender.STUNDEN_TAG + h] = muster[h] * tag;
            }
            return new Messreihe("Waermemengenzaehler (erfunden)", ZapfMessgroesse.Energie, 60, BEGINN, werte,
                                 "Probe (erfunden)");
        }

        /// <summary>
        /// Eine Messreihe wie die Rechnung, deren Spitze um genau <paramref name="faktor"/> abweicht:
        /// Die GANZE Reihe wird skaliert. Eine einzelne Stunde zu skalieren genügte nicht — bei
        /// einem Faktor unter 1 würde eine andere Stunde zur Spitze, und das Verhältnis wäre nicht
        /// mehr der Faktor. Die Form bleibt dabei unverändert (ein Faktor verschiebt keine Stunde).
        /// </summary>
        private static Messreihe MitSpitzenfaktor(double faktor, bool jahresgang = false)
            => Jahresmessreihe(faktor, jahresgang: jahresgang);

        /// <summary>
        /// Der Vergleich mit einer um <paramref name="faktor"/> abweichenden Messspitze. Der
        /// Jahresgang der Messreihe folgt dem der gerechneten Reihe — sonst verschöbe die Probe
        /// neben der Spitze auch die Form.
        /// </summary>
        private static Messvergleichsergebnis MitSpitze(Bilanzreihe gerechnet, double[] stichprobe, double faktor)
        {
            // Die Spitzenstunde (7) des ersten Tages gegen die des Tages 182: Gleich heisst
            // "kein Jahresgang".
            bool jahresgang = gerechnet.StundenKwh[7] != gerechnet.StundenKwh[182 * 24 + 7];
            return Messvergleich.Vergleichen(
                Eingang(MitSpitzenfaktor(faktor, jahresgang), gerechnet, einheiten: 1, stichprobe: stichprobe));
        }

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
