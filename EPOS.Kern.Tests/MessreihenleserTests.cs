using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Leser einer gemessenen Reihe</b> (Umsetzungskonzept Zapfprofilgenerator 4.8 und
    /// Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 1): Formate, Auflösungen, Lücken und jede benannte
    /// Ablehnung. <b>Alle Reihen sind erfunden</b> — runde Werte, ein erfundener Zähler; keine
    /// Objektdaten (Kapitel 9 K5), keine Normzahl.
    /// </summary>
    public sealed class MessreihenleserTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <inheritdoc />
        public void Dispose() => _kultur.Dispose();

        private static readonly Messreihenoptionen Vorgabe = new Messreihenoptionen
        {
            Bezeichnung = "Waermemengenzaehler (erfunden)",
            Quelle = "Probe (erfunden)"
        };

        // =================================================================================
        //  Formate
        // =================================================================================

        /// <summary>
        /// Die vier zugelassenen Schreibweisen des Zeitbezugs und beide Dezimalzeichen führen auf
        /// DIESELBE Reihe: ISO mit T, ISO mit Leerzeichen, deutsches Datum mit Uhrzeit und Datum
        /// plus Uhrzeit in getrennten Spalten. Der Trenner wird aus der Kopfzeile gemessen.
        /// </summary>
        [Fact]
        public void Vier_Schreibweisen_ergeben_dieselbe_Reihe()
        {
            string iso = "Zeitstempel;Wert [kWh]\n2025-03-03T00:00;1.5\n2025-03-03T01:00;2,5\n2025-03-03T02:00;0\n";
            string leer = "zeitstempel;wert (kWh)\n2025-03-03 00:00;1.5\n2025-03-03 01:00;2.5\n2025-03-03 02:00;0.0\n";
            string deutsch = "Zeitpunkt\tWert [kWh]\n03.03.2025 00:00\t1,5\n03.03.2025 01:00\t2,5\n03.03.2025 02:00\t0\n";
            string getrennt = "Datum,Uhrzeit,Wert [kWh]\n2025-03-03,00:00,1.5\n2025-03-03,01:00,2.5\n2025-03-03,02:00,0\n";

            foreach (string text in new[] { iso, leer, deutsch, getrennt })
            {
                Messreihe r = Lesen(text, out ZapfSatz fehler, out _);
                Assert.Null(fehler);
                Assert.NotNull(r);
                Assert.Equal(ZapfMessgroesse.Energie, r.Groesse);
                Assert.Equal(60, r.AufloesungMin);
                Assert.Equal(new DateTime(2025, 3, 3, 0, 0, 0), r.Beginn);
                Assert.Equal(new[] { 1.5, 2.5, 0.0 }, r.Werte);
                Assert.Equal(4.0, r.Menge, 12);
                Assert.Equal(0, r.Luecken);
                Assert.Equal("Waermemengenzaehler (erfunden)", r.Bezeichnung);
                Assert.Equal("Probe (erfunden)", r.Quelle);
            }
        }

        /// <summary>
        /// Ist der Trenner das Komma, ist ein Komma KEIN Dezimalzeichen — „1,5" wären zwei Felder.
        /// Die Gegenprobe: derselbe Text mit Semikolon liest 1,5 als eine Zahl.
        /// </summary>
        [Fact]
        public void Bei_Komma_als_Trenner_gilt_nur_der_Punkt()
        {
            // Mit Komma als Trenner traegt die Zeile ein Feld zu viel - benannte Ablehnung statt
            // eines still gelesenen Vorkommateils.
            Lesen("Zeitstempel,Wert [kWh]\n2025-01-01T00:00,1,5\n2025-01-01T01:00,2,5\n",
                  out ZapfSatz fehler, out _);
            Assert.NotNull(fehler);
            Assert.Equal("MESSREIHE_FELDZAHL", fehler.Kennung);

            Messreihe r = Lesen("Zeitstempel;Wert [kWh]\n2025-01-01T00:00;1,5\n2025-01-01T01:00;2,5\n",
                                out ZapfSatz ohne, out _);
            Assert.Null(ohne);
            Assert.Equal(new[] { 1.5, 2.5 }, r.Werte);
        }

        /// <summary>
        /// Die Einheit im Kopf der Wertspalte wählt die Größe; die Optionen schlagen sie. Die Menge
        /// einer Leistungsreihe ist die Energie (kW · Δt/60), die einer Volumenreihe das Volumen.
        /// </summary>
        [Fact]
        public void Die_Einheit_im_Kopf_waehlt_die_Groesse_die_Option_schlaegt_sie()
        {
            Messreihe energie = Lesen("Zeitstempel;Wert [kWh]\n2025-01-01T00:00;2\n2025-01-01T00:15;2\n", out _, out _);
            Assert.Equal(ZapfMessgroesse.Energie, energie.Groesse);
            Assert.Equal(15, energie.AufloesungMin);
            Assert.Equal(4.0, energie.Menge, 12);

            Messreihe leistung = Lesen("Zeitstempel;Leistung [kW]\n2025-01-01T00:00;2\n2025-01-01T00:15;2\n", out _, out _);
            Assert.Equal(ZapfMessgroesse.Leistung, leistung.Groesse);
            Assert.Equal(1.0, leistung.Menge, 12);          // 2 kW * 0,25 h zweimal
            Assert.Equal(4.0, leistung.Werte.Sum(), 12);    // die rohen kW - eine Summe ohne Sinn

            Messreihe volumen = Lesen("Zeitstempel;Volumen [m³]\n2025-01-01T00:00;0.1\n2025-01-01T01:00;0.1\n", out _, out _);
            Assert.Equal(ZapfMessgroesse.Volumen, volumen.Groesse);
            Assert.True(volumen.IstVolumen);
            Assert.Equal(0.2, volumen.Menge, 12);
            // 200 l mal c_w mal 35 K: dieselbe Umrechnung wie beim Jahresmesswert in m³/a (4.1).
            Assert.Equal(Mengengeruest.EnergieKwh(200.0, 35.0), volumen.EnergieKwh(35.0), 9);

            // Die Option schlaegt die Einheit des Kopfes.
            Messreihe gewaehlt = Lesen("Zeitstempel;Wert [kWh]\n2025-01-01T00:00;2\n2025-01-01T01:00;2\n", out _, out _,
                                       Vorgabe with { Groesse = ZapfMessgroesse.Leistung });
            Assert.Equal(ZapfMessgroesse.Leistung, gewaehlt.Groesse);
        }

        /// <summary>
        /// Jedes Raster von <see cref="Messreihenleser.RASTER"/> wird gemessen; eine Tagesreihe
        /// (1440 min) trägt keine Stundenwerte und sagt das benannt, statt still zu rechnen.
        /// </summary>
        [Fact]
        public void Jedes_zugelassene_Raster_wird_gemessen()
        {
            foreach (int raster in Messreihenleser.RASTER)
            {
                Messreihe r = Lesen(Reihe(new DateTime(2025, 1, 1), raster, Enumerable.Repeat(1.0, 4).ToArray()),
                                    out ZapfSatz fehler, out List<ZapfSatz> hinweise);
                Assert.Null(fehler);
                Assert.Equal(raster, r.AufloesungMin);
                Assert.Equal(4.0 * raster / 1440.0, r.Tage, 12);
                if (raster == 1440)
                {
                    Assert.False(r.StundenweiseTauglich);
                    Assert.Empty(r.Stundenwerte(35.0));
                    Assert.Contains(hinweise, h => h.Kennung == "MESSREIHE_OHNE_STUNDENWERTE");
                }
                else
                {
                    Assert.True(r.StundenweiseTauglich);
                    Assert.DoesNotContain(hinweise, h => h.Kennung == "MESSREIHE_OHNE_STUNDENWERTE");
                }
            }
        }

        /// <summary>
        /// <b>Stundenwerte nur aus vollständigen Stunden</b>: Eine Reihe, die 00:15 beginnt, liefert
        /// die Stunde 01:00 als erste — die angeschnittene erste Stunde täuschte sonst einen kleinen
        /// Stundenwert vor und verschöbe die Spitze. Der Versatz ist benannt.
        /// </summary>
        [Fact]
        public void Stundenwerte_zaehlen_nur_vollstaendige_Stunden()
        {
            // 00:15 bis 02:00, Viertelstunden: nur die Stunde 01:00 ist vollstaendig.
            var werte = new[] { 9.0, 9.0, 9.0, 1.0, 2.0, 3.0, 4.0, 9.0 };
            Messreihe r = Lesen(Reihe(new DateTime(2025, 1, 1, 0, 15, 0), 15, werte), out ZapfSatz fehler,
                                out List<ZapfSatz> hinweise);
            Assert.Null(fehler);
            Assert.Contains(hinweise, h => h.Kennung == "MESSREIHE_BEGINN_NICHT_STUNDE");

            IReadOnlyList<Messstunde> stunden = r.Stundenwerte(35.0);
            Assert.Single(stunden);
            Assert.Equal(new DateTime(2025, 1, 1, 1, 0, 0), stunden[0].Beginn);
            Assert.Equal(10.0, stunden[0].Kwh, 12);
        }

        // =================================================================================
        //  Lücken
        // =================================================================================

        /// <summary>
        /// Eine Lücke wird mit 0 gefüllt, gezählt und benannt; über der Schwelle lehnt der Leser die
        /// Datei ab. Ein leeres Wertfeld ist ebenfalls eine Lücke, kein Fehler. Die Schwelle ist ein
        /// Parameter (<see cref="Messreihenoptionen.LueckenanteilHoechstens"/>) — die Probe dreht
        /// allein an ihm und bekommt beide Antworten aus DERSELBEN Datei.
        /// </summary>
        [Fact]
        public void Eine_Luecke_wird_gefuellt_gezaehlt_und_ueber_der_Schwelle_abgelehnt()
        {
            // Sechs Stunden, die Stunden 02:00 und 03:00 fehlen, 04:00 ist leer: 3 von 6 Luecken.
            const string text = "Zeitstempel;Wert [kWh]\n"
                                + "2025-01-01T00:00;1\n2025-01-01T01:00;1\n2025-01-01T04:00;\n2025-01-01T05:00;1\n";

            Messreihe weit = Lesen(text, out ZapfSatz ohne, out List<ZapfSatz> hinweise,
                                   Vorgabe with { LueckenanteilHoechstens = 0.6 });
            Assert.Null(ohne);
            Assert.Equal(6, weit.Schritte);
            Assert.Equal(new[] { 1.0, 1.0, 0.0, 0.0, 0.0, 1.0 }, weit.Werte);
            Assert.Equal(3, weit.Luecken);
            Assert.Equal(0.5, weit.Lueckenanteil, 12);
            ZapfSatz hinweis = Assert.Single(hinweise, h => h.Kennung == "MESSREIHE_LUECKEN");
            Assert.Contains("3", hinweis.Klartext);

            Lesen(text, out ZapfSatz fehler, out _, Vorgabe with { LueckenanteilHoechstens = 0.4 });
            Assert.NotNull(fehler);
            Assert.Equal("MESSREIHE_LUECKEN_ZU_GROSS", fehler.Kennung);
        }

        /// <summary>
        /// Ein Schalttag und eine Reihe über ein Jahr hinaus sind Hinweise, keine Ablehnungen: Die
        /// Messung kennt ihren echten Kalender, der Rechenkern rechnet 365 Tage ohne Schaltjahr.
        /// </summary>
        [Fact]
        public void Schalttag_und_Ueberlaenge_sind_Hinweise()
        {
            Messreihe schalt = Lesen(Reihe(new DateTime(2024, 2, 27), 1440, Enumerable.Repeat(1.0, 5).ToArray()),
                                     out ZapfSatz fehler, out List<ZapfSatz> hinweise);
            Assert.Null(fehler);
            Assert.Equal(1, schalt.Schalttage);
            Assert.Contains(hinweise, h => h.Kennung == "MESSREIHE_SCHALTTAG");
            Assert.DoesNotContain(hinweise, h => h.Kennung == "MESSREIHE_UEBER_EIN_JAHR");

            Messreihe lang = Lesen(Reihe(new DateTime(2025, 1, 1), 1440, Enumerable.Repeat(1.0, 400).ToArray()),
                                   out _, out List<ZapfSatz> h2);
            Assert.True(lang.Tage > Zapfkalender.TAGE);
            Assert.Contains(h2, h => h.Kennung == "MESSREIHE_UEBER_EIN_JAHR");
        }

        // =================================================================================
        //  Die benannten Ablehnungen
        // =================================================================================

        /// <summary>
        /// <b>Jede Ablehnung ist benannt</b> — mit Kennung, Datei und, wo es eine gibt, der Zeile.
        /// Die Liste deckt jeden Ablehnungsgrund des Lesers; ein neuer Grund ohne Probe fällt auf
        /// (die Gegenzählung am Ende).
        /// </summary>
        [Theory]
        // Kopfzeile und Trenner
        [InlineData("MESSREIHE_KOPFZEILE_FEHLT", "")]
        [InlineData("MESSREIHE_TRENNER_UNBEKANNT", "Zeitstempel und Wert\n")]
        [InlineData("MESSREIHE_SPALTE_ZEIT_FEHLT", "Nummer;Wert [kWh]\n1;1\n2;1\n")]
        [InlineData("MESSREIHE_SPALTE_WERT_FEHLT", "Zeitstempel;Zaehlerstand\n2025-01-01T00:00;1\n")]
        [InlineData("MESSREIHE_EINHEIT_UNBEKANNT", "Zeitstempel;Wert [Liter]\n2025-01-01T00:00;1\n")]
        // Zeilen
        [InlineData("MESSREIHE_FELDZAHL", "Datum;Uhrzeit;Wert [kWh]\n2025-01-01;00:00;1\n2025-01-01;01:00\n")]
        [InlineData("MESSREIHE_KEINE_ZAHL", "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;viel\n2025-01-01T01:00;1\n")]
        [InlineData("MESSREIHE_WERT_NEGATIV", "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;1\n2025-01-01T01:00;-0.5\n")]
        [InlineData("MESSREIHE_ZEITSTEMPEL_UNGUELTIG", "Zeitstempel;Wert [kWh]\n01/01/2025 00:00;1\n2025-01-01T01:00;1\n")]
        [InlineData("MESSREIHE_ZEITSTEMPEL_FOLGE", "Zeitstempel;Wert [kWh]\n2025-01-01T01:00;1\n2025-01-01T00:00;1\n")]
        // Raster und Menge
        [InlineData("MESSREIHE_AUFLOESUNG_UNBEKANNT", "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;1\n2025-01-01T00:30;1\n")]
        [InlineData("MESSREIHE_ZEITSCHRITT_UNGLEICH",
                    "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;1\n2025-01-01T00:15;1\n2025-01-01T00:35;1\n")]
        [InlineData("MESSREIHE_ZU_KURZ", "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;1\n")]
        [InlineData("MESSREIHE_OHNE_WERTE", "Zeitstempel;Wert [kWh]\n")]
        [InlineData("MESSREIHE_OHNE_MENGE", "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;0\n2025-01-01T01:00;0\n")]
        public void Jede_Ablehnung_ist_benannt(string kennung, string text)
        {
            Messreihe r = Lesen(text, out ZapfSatz fehler, out _);
            Assert.Null(r);
            Assert.NotNull(fehler);
            Assert.Equal(kennung, fehler.Kennung);
            Assert.NotEmpty(fehler.Klartext);
            Assert.DoesNotContain(ZapfSatz.PRAEFIX, fehler.Klartext, StringComparison.Ordinal);
        }

        /// <summary>Ein fehlender Strom und ein fehlender Text sind benannt, nicht null-Ausnahmen.</summary>
        [Fact]
        public void Kein_Strom_und_kein_Text_sind_benannt()
        {
            Assert.Null(Messreihenleser.AusStrom(null, "probe.csv", Vorgabe, out ZapfSatz a));
            Assert.Equal("MESSREIHE_DATEI_UNLESBAR", a.Kennung);
            Assert.Null(Messreihenleser.AusText(null, "probe.csv", Vorgabe, out ZapfSatz b));
            Assert.Equal("MESSREIHE_DATEI_UNLESBAR", b.Kennung);
        }

        /// <summary>
        /// Der Weg der Hülle: ein <see cref="Stream"/>, wie <c>Dienste.Datei</c> ihn liefert — mit
        /// BOM und CRLF, und der Strom bleibt offen (der Aufrufer schließt ihn).
        /// </summary>
        [Fact]
        public void Aus_einem_Strom_mit_BOM_und_CRLF()
        {
            byte[] roh = new UTF8Encoding(true).GetBytes(
                "Zeitstempel;Wert [kWh]\r\n2025-06-01T00:00;3\r\n2025-06-01T01:00;1\r\n");
            using var strom = new MemoryStream(roh);
            Messreihe r = Messreihenleser.AusStrom(strom, "C:\\egal\\messung.csv", Vorgabe, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(new[] { 3.0, 1.0 }, r.Werte);
            Assert.True(strom.CanRead, "Der Leser hat den Strom geschlossen.");

            // Ohne Bezeichnung und Quelle traegt die Reihe den Dateinamen - OHNE Ordneranteil:
            // Der Kern gibt keinen Pfad des Anwenders weiter (er kennt auch keinen).
            strom.Position = 0;
            Messreihe ohne = Messreihenleser.AusStrom(strom, "C:\\egal\\messung.csv", new Messreihenoptionen(), out _);
            Assert.Equal("messung.csv", ohne.Bezeichnung);
            Assert.Equal("messung.csv", ohne.Quelle);
        }

        /// <summary>Eine zu große Datei wird benannt abgelehnt, ohne gelesen zu werden.</summary>
        [Fact]
        public void Eine_zu_grosse_Datei_wird_benannt_abgelehnt()
        {
            using var strom = new LangerStrom(Messreihenleser.HOECHSTENS_BYTE + 1);
            Assert.Null(Messreihenleser.AusStrom(strom, "gross.csv", Vorgabe, out ZapfSatz fehler));
            Assert.Equal("MESSREIHE_ZU_GROSS", fehler.Kennung);
        }

        /// <summary>
        /// Ein Feld in Anführungszeichen darf den Trenner tragen; eine Leerzeile trennt nur.
        /// </summary>
        [Fact]
        public void Anfuehrungszeichen_und_Leerzeilen()
        {
            Messreihe r = Lesen("\"Zeitstempel\";\"Wert [kWh]\";\"Ort; Raum\"\n"
                                + "2025-01-01T00:00;1;\"Keller; links\"\n"
                                + "\n"
                                + "2025-01-01T01:00;2;\"Keller; rechts\"\n", out ZapfSatz fehler, out _);
            Assert.Null(fehler);
            Assert.Equal(new[] { 1.0, 2.0 }, r.Werte);
        }

        // =================================================================================
        //  Die freie Messreihe aus OpenDHW
        // =================================================================================

        /// <summary>
        /// <b>Eine freie Messreihe als Gegenprobe des Lesers</b> (Kapitel 9 K5: bis zur Freigabe
        /// eigener Messdaten allein freie Reihen): Die DHWcalc-Referenzdatei aus OpenDHW (MIT,
        /// Attribution in <c>Proben/Zapfprofil/OpenDHW/LIESMICH.md</c>) trägt ein Jahr
        /// Minutenwerte. Der Test schreibt daraus eine CSV im Format des Lesers, liest sie zurück
        /// und hält die <b>Verteilungsgrößen</b> gegeneinander — kein Bitvergleich: Menge, Zahl der
        /// Zeitschritte, Raster und der größte Wert müssen dieselben sein, die Reihe lückenlos.
        ///
        /// <para>Nur ein Ausschnitt von 14 Tagen — der Leseweg ist derselbe, und eine
        /// Jahres-CSV in Minutenschritten wäre 4 MB Testdaten je Lauf.</para>
        /// </summary>
        [Fact]
        public void Die_freie_OpenDHW_Reihe_liest_sich_als_Minutenreihe()
        {
            string ordner = Path.Combine(ZapfZufallTests.Probenordner(), "OpenDHW");
            string gepackt = Path.Combine(ordner, "200L_1min_4cat_sf_nods_max1200.txt.gz");
            if (!File.Exists(gepackt)) return;           // ohne die freie Datei schweigt der Fall

            double[] lJeMinute = MinutenwerteLesen(gepackt, 14 * 1440);
            var beginn = new DateTime(2025, 1, 1);
            var bau = new StringBuilder("Zeitstempel;Volumen [m³]\n");
            for (int i = 0; i < lJeMinute.Length; i++)
                bau.Append(beginn.AddMinutes(i).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture))
                   .Append(';').Append((lJeMinute[i] / 1000.0).ToString("0.############", CultureInfo.InvariantCulture))
                   .Append('\n');

            Messreihe r = Lesen(bau.ToString(), out ZapfSatz fehler, out List<ZapfSatz> hinweise);
            Assert.Null(fehler);
            Assert.Equal(1, r.AufloesungMin);
            Assert.Equal(lJeMinute.Length, r.Schritte);
            Assert.Equal(0, r.Luecken);
            Assert.Empty(hinweise);
            Assert.Equal(ZapfMessgroesse.Volumen, r.Groesse);

            // Verteilungsgroessen statt Bitvergleich: Menge, groesster Wert, Tage, Stundenzahl.
            double summeL = lJeMinute.Sum();
            Assert.Equal(summeL / 1000.0, r.Menge, 6);
            Assert.Equal(lJeMinute.Max() / 1000.0, r.GroessterWert, 9);
            Assert.Equal(14.0, r.Tage, 9);
            Assert.Equal(14 * 24, r.Stundenwerte(35.0).Count);
            // Eine Tagesmenge in der Groessenordnung eines Einfamilienhauses (die Datei nennt 200 l/d).
            Assert.InRange(summeL / 14.0, 100.0, 400.0);
        }

        // =================================================================================
        //  Die Sommerzeit
        // =================================================================================

        /// <summary>
        /// <b>Die Herbstumstellung:</b> Die Stunde 02:00 steht zweimal in der Datei (Sommer- und
        /// Normalzeit tragen denselben Namen). Der Leser nimmt den wiederholten Zeitstempel EINMAL
        /// als Folgeschritt, nennt es mit <c>MESSREIHE_SOMMERZEIT</c> — und zählt ihn NICHT als
        /// Lücke: Die Stunde ist gemessen, sie heißt nur wie ihre Vorgängerin.
        /// </summary>
        [Fact]
        public void Die_Herbstumstellung_wird_als_Folgeschritt_gelesen()
        {
            // 26.10.2025, der Tag der Herbstumstellung in Mitteleuropa: 01:00, 02:00, 02:00, 03:00 ...
            const string text = "Zeitstempel;Wert [kWh]\n" +
                                "2025-10-26T01:00;1\n" +
                                "2025-10-26T02:00;2\n" +
                                "2025-10-26T02:00;3\n" +
                                "2025-10-26T03:00;4\n" +
                                "2025-10-26T04:00;5\n";

            Messreihe r = Lesen(text, out ZapfSatz fehler, out List<ZapfSatz> hinweise);
            Assert.Null(fehler);
            Assert.NotNull(r);
            Assert.Equal(60, r.AufloesungMin);
            Assert.Equal(5, r.Werte.Count);                                   // fuenf Schritte, kein sechster
            Assert.Equal(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, r.Werte);
            Assert.Equal(0, r.Luecken);                                       // KEINE Luecke
            ZapfSatz hinweis = Assert.Single(hinweise, h => h.Kennung == "MESSREIHE_SOMMERZEIT");
            Assert.NotEmpty(hinweis.Klartext);
            Assert.DoesNotContain(hinweise, h => h.Kennung == "MESSREIHE_LUECKEN");
        }

        /// <summary>
        /// <b>Die Frühjahrsumstellung:</b> Die Stunde 02:00 fehlt (auf 01:00 folgt 03:00). Der Leser
        /// füllt sie als Lücke mit 0 und zählt sie — wie jede andere Lücke, mit
        /// <c>MESSREIHE_LUECKEN</c>; der Sommerzeit-Hinweis bleibt aus, weil kein Zeitstempel
        /// wiederholt wurde.
        /// </summary>
        [Fact]
        public void Die_Fruehjahrsumstellung_wird_als_Luecke_gefuellt()
        {
            // 30.03.2025, der Tag der Fruehjahrsumstellung: 00:00, 01:00, 03:00, 04:00 ...
            const string text = "Zeitstempel;Wert [kWh]\n" +
                                "2025-03-30T00:00;1\n" +
                                "2025-03-30T01:00;2\n" +
                                "2025-03-30T03:00;3\n" +
                                "2025-03-30T04:00;4\n";

            // Eine von fuenf Stunden ist gefuellt (20 %) - die Probe hebt die Schwelle, weil eine
            // Reihe von vier Zeilen zwangslaeufig einen hohen Lueckenanteil hat.
            Messreihe r = Lesen(text, out ZapfSatz fehler, out List<ZapfSatz> hinweise,
                                Vorgabe with { LueckenanteilHoechstens = 0.25 });
            Assert.Null(fehler);
            Assert.NotNull(r);
            Assert.Equal(60, r.AufloesungMin);
            Assert.Equal(5, r.Werte.Count);                                   // vier gemessene, eine gefuellte
            Assert.Equal(new[] { 1.0, 2.0, 0.0, 3.0, 4.0 }, r.Werte);
            Assert.Equal(1, r.Luecken);
            Assert.Single(hinweise, h => h.Kennung == "MESSREIHE_LUECKEN");
            Assert.DoesNotContain(hinweise, h => h.Kennung == "MESSREIHE_SOMMERZEIT");
        }

        /// <summary>
        /// <b>Eine ganze Jahresreihe in Ortszeit</b> — 2025, Stundenwerte, mit BEIDEN Umstellungen:
        /// Die Datei trägt 8 760 Zeilen (der Herbstsprung gibt eine Stunde her, der Frühjahrssprung
        /// nimmt eine), und daraus werden 8 761 Zeitschritte mit genau EINER gefüllten Lücke (die
        /// fehlende Märzstunde) und EINEM wiederholten Zeitstempel (die doppelte Oktoberstunde).
        /// Beides ist benannt, die Menge stimmt.
        /// </summary>
        [Fact]
        public void Eine_Jahresreihe_in_Ortszeit_traegt_beide_Umstellungen()
        {
            string text = Jahresreihe_Ortszeit(2025, out int zeilen);
            Assert.Equal(8760, zeilen);

            Messreihe r = Lesen(text, out ZapfSatz fehler, out List<ZapfSatz> hinweise);
            Assert.Null(fehler);
            Assert.NotNull(r);
            Assert.Equal(60, r.AufloesungMin);
            // 8 760 Zeilen, EINE gefuellte Luecke: 8 761 Zeitschritte. Der Leser rechnet auf der
            // Wanduhr und kann nicht wissen, dass es die Maerzstunde nicht gibt - er fuellt sie
            // benannt, statt still eine Stunde zu verschieben.
            Assert.Equal(8761, r.Werte.Count);
            Assert.Equal(1, r.Luecken);                                       // die fehlende Maerzstunde
            Assert.Single(hinweise, h => h.Kennung == "MESSREIHE_SOMMERZEIT");
            Assert.Single(hinweise, h => h.Kennung == "MESSREIHE_LUECKEN");
            Assert.Equal(0, r.Schalttage);                                     // 2025 ist kein Schaltjahr
            // 8 760 gemessene Zeilen tragen je 1 kWh, die gefuellte traegt 0.
            Assert.Equal(8760.0, r.Menge, 9);
        }

        /// <summary>
        /// <b>Mit <see cref="Messzeitstempel.Normalzeit"/> ist keine Umstellung zu erwarten:</b>
        /// Derselbe wiederholte Zeitstempel ist dann eine doppelte Zeile und eine benannte
        /// Ablehnung. Ein ZWEITER wiederholter Zeitstempel ist auch in Ortszeit eine Ablehnung — ein
        /// Jahr hat eine Herbstumstellung.
        /// </summary>
        [Fact]
        public void Ohne_erwartete_Umstellung_ist_der_doppelte_Zeitstempel_eine_Ablehnung()
        {
            const string einmal = "Zeitstempel;Wert [kWh]\n" +
                                  "2025-10-26T01:00;1\n" +
                                  "2025-10-26T02:00;2\n" +
                                  "2025-10-26T02:00;3\n" +
                                  "2025-10-26T03:00;4\n";

            // Normalzeit: schon der erste wiederholte Zeitstempel faellt - benannt, mit der Zeile.
            Assert.Null(Lesen(einmal, out ZapfSatz normal, out _,
                              Vorgabe with { Zeitstempel = Messzeitstempel.Normalzeit }));
            Assert.Equal("MESSREIHE_ZEITSTEMPEL_FOLGE", normal.Kennung);
            Assert.Contains(", Zeile 4", normal.Klartext, StringComparison.Ordinal);   // die Zeile der Datei

            // Ortszeit: derselbe Text geht durch.
            Assert.NotNull(Lesen(einmal, out ZapfSatz ort, out _));
            Assert.Null(ort);

            // Zwei wiederholte Zeitstempel: auch in Ortszeit eine Ablehnung.
            const string zweimal = "Zeitstempel;Wert [kWh]\n" +
                                   "2025-10-26T01:00;1\n" +
                                   "2025-10-26T02:00;2\n" +
                                   "2025-10-26T02:00;3\n" +
                                   "2025-10-26T03:00;4\n" +
                                   "2025-10-26T03:00;5\n";
            Assert.Null(Lesen(zweimal, out ZapfSatz zwei, out _));
            Assert.Equal("MESSREIHE_ZEITSTEMPEL_FOLGE", zwei.Kennung);

            // Und eine Reihe, die NUR aus wiederholten Zeitstempeln besteht, hat keine Aufloesung.
            const string nurGleich = "Zeitstempel;Wert [kWh]\n" +
                                     "2025-10-26T02:00;1\n" +
                                     "2025-10-26T02:00;2\n";
            Assert.Null(Lesen(nurGleich, out ZapfSatz ohne, out _));
            Assert.Equal("MESSREIHE_AUFLOESUNG_UNBEKANNT", ohne.Kennung);
        }

        // =================================================================================
        //  Helfer
        // =================================================================================
        /// <summary>
        /// Eine Jahresreihe in ORTSZEIT: Stundenwerte je 1 kWh von 01.01. 00:00 bis 31.12. 23:00, mit
        /// beiden Sprüngen der Sommerzeit nach der mitteleuropäischen Regel (letzter Sonntag im März
        /// 02:00 fehlt, letzter Sonntag im Oktober 02:00 steht zweimal). Sie ist ERFUNDEN — jede
        /// Stunde trägt denselben Wert; geprüft wird die Zeitachse, nicht ein Verbrauch.
        /// </summary>
        private static string Jahresreihe_Ortszeit(int jahr, out int zeilen)
        {
            DateTime fruehjahr = LetzterSonntag(jahr, 3).AddHours(2);   // diese Stunde gibt es nicht
            DateTime herbst = LetzterSonntag(jahr, 10).AddHours(2);     // diese Stunde gibt es zweimal

            var bau = new StringBuilder("Zeitstempel;Wert [kWh]\n");
            zeilen = 0;
            var ende = new DateTime(jahr + 1, 1, 1);
            for (DateTime t = new DateTime(jahr, 1, 1); t < ende; t = t.AddHours(1))
            {
                if (t == fruehjahr) continue;
                Stundenzeile(bau, t);
                zeilen++;
                if (t == herbst) { Stundenzeile(bau, t); zeilen++; }
            }
            return bau.ToString();
        }

        private static void Stundenzeile(StringBuilder bau, DateTime t)
            => bau.Append(t.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)).Append(";1\n");

        /// <summary>Der letzte Sonntag eines Monats, 00:00 — die Regel beider Umstellungen.</summary>
        private static DateTime LetzterSonntag(int jahr, int monat)
        {
            var t = new DateTime(jahr, monat, DateTime.DaysInMonth(jahr, monat));
            while (t.DayOfWeek != DayOfWeek.Sunday) t = t.AddDays(-1);
            return t;
        }


        private static Messreihe Lesen(string text, out ZapfSatz fehler, out List<ZapfSatz> hinweise,
                                       Messreihenoptionen optionen = null)
        {
            hinweise = new List<ZapfSatz>();
            return Messreihenleser.AusText(text, "messung.csv", optionen ?? Vorgabe, out fehler, hinweise);
        }

        /// <summary>Eine CSV im ISO-Format über ein Raster und eine Werteliste.</summary>
        private static string Reihe(DateTime beginn, int rasterMin, IReadOnlyList<double> werte)
        {
            var bau = new StringBuilder("Zeitstempel;Wert [kWh]\n");
            for (int i = 0; i < werte.Count; i++)
                bau.Append(beginn.AddMinutes((double)i * rasterMin).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture))
                   .Append(';').Append(werte[i].ToString("0.#####", CultureInfo.InvariantCulture)).Append('\n');
            return bau.ToString();
        }

        /// <summary>Die ersten <paramref name="anzahl"/> Minutenwerte [l/min] der gepackten OpenDHW-Datei (l/h je Zeile).</summary>
        private static double[] MinutenwerteLesen(string gepackt, int anzahl)
        {
            var werte = new List<double>(anzahl);
            using var datei = File.OpenRead(gepackt);
            using var strom = new System.IO.Compression.GZipStream(datei, System.IO.Compression.CompressionMode.Decompress);
            using var leser = new StreamReader(strom);
            for (string z = leser.ReadLine(); z != null && werte.Count < anzahl; z = leser.ReadLine())
            {
                string t = z.Trim();
                if (t.Length == 0) continue;
                werte.Add(double.Parse(t, CultureInfo.InvariantCulture) / 60.0);
            }
            return werte.ToArray();
        }

        /// <summary>Ein Strom, der nur seine Länge kennt — für die Größengrenze, ohne Bytes zu erzeugen.</summary>
        private sealed class LangerStrom : Stream
        {
            private readonly long _laenge;

            internal LangerStrom(long laenge) { _laenge = laenge; }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => _laenge;

            public override long Position { get; set; }

            public override void Flush() { }

            public override int Read(byte[] puffer, int versatz, int zahl) => 0;

            public override long Seek(long versatz, SeekOrigin ursprung) => Position;

            public override void SetLength(long wert) => throw new NotSupportedException();

            public override void Write(byte[] puffer, int versatz, int zahl) => throw new NotSupportedException();
        }
    }
}
