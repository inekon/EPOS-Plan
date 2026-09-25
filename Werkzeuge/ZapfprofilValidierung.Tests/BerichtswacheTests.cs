using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Die Wache, die Absolutwerte der Messung aus dem Bericht hält</b> (Konzept Kapitel 9 K5).
    /// Ohne sie dürfte kein Bericht dieses Werkzeugs ins Repositorium; sie ist deshalb die wichtigste
    /// Probe hier — mit Gegenproben, die zeigen, dass sie nicht blind zuschlägt.
    /// </summary>
    public sealed class BerichtswacheTests
    {
        [Fact]
        public void Die_Berichte_des_Beispiels_sind_frei_von_Absolutwerten()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string ziel = v.Neu("wache");
            Assert.Equal(Einstieg.OK, v.Lauf(v.Beispiel, "--katalog", v.Katalog, "--ziel", ziel).Code);

            // Dieselbe Prüfung, die der Lauf vor dem Schreiben gemacht hat — hier gegen die Dateien,
            // die wirklich auf der Platte liegen, und gegen die Kennzahlen BEIDER Objekte.
            List<string> verboten = new[] { "BSP-WOHNEN-01", "BSP-PFLEGE-01" }
                .SelectMany(k => Kennzahlen(v, k)).Distinct().ToList();
            Assert.NotEmpty(verboten);

            foreach (string datei in Directory.EnumerateFiles(ziel))
            {
                List<string> funde = Berichtswache.Pruefen(File.ReadAllText(datei), verboten);
                Assert.True(funde.Count == 0, Berichtswache.Meldung(Path.GetFileName(datei), funde));
            }
        }

        [Fact]
        public void Eine_Einheit_einer_Menge_haelt_den_Bericht_zurueck()
        {
            foreach (string einheit in new[] { "kWh", "MWh", "m³", "Btu", "Liter" })
            {
                List<string> funde = Berichtswache.Pruefen("Die Jahresmenge war 12 " + einheit + ".",
                                                           new string[0]);
                Assert.True(funde.Count > 0, "Die Einheit \"" + einheit + "\" muss auffallen.");
            }
            Assert.Empty(Berichtswache.Pruefen("Energieverhaeltnis 0,942 [-], Anteil 0,18 [-].", new string[0]));
        }

        [Fact]
        public void Eine_Kennzahl_der_Messung_haelt_den_Bericht_zurueck()
        {
            var verboten = new[] { "12345.68", "12345.679" };   // je sieben bzw. acht Ziffern
            Assert.NotEmpty(Berichtswache.Pruefen("Der Zaehler stand bei 12345.68 am Jahresende.", verboten));
            Assert.Empty(Berichtswache.Pruefen("Verhaeltnis 0,94; Abweichung -0,06.", verboten));
        }

        [Fact]
        public void Die_Wache_findet_nur_ganze_Zahlen_kein_Stueck_einer_anderen()
        {
            // Die Gegenprobe zum Fehlalarm: "37.25" steckt in "137.256" und in "0.37.25" - beides
            // ist eine ANDERE Zahl.
            Assert.True(Berichtswache.Steht("Wert 37.25 im Bericht", "37.25"));
            Assert.True(Berichtswache.Steht("(37.25)", "37.25"));
            Assert.True(Berichtswache.Steht("37.25", "37.25"));
            Assert.True(Berichtswache.Steht("Die Menge war 37.25.", "37.25"));      // Satzende ist Grenze
            Assert.True(Berichtswache.Steht("Werte: 1.5, 37.25, 9.0", "37.25"));    // Komma als Trenner
            Assert.False(Berichtswache.Steht("Wert 137.256 im Bericht", "37.25"));
            Assert.False(Berichtswache.Steht("Wert 1137.25 im Bericht", "37.25"));
            Assert.False(Berichtswache.Steht("Wert 37.2500 im Bericht", "37.25"));
            Assert.False(Berichtswache.Steht("Wert 0.37.25 im Bericht", "37.25"));
            Assert.False(Berichtswache.Steht("", "37.25"));
            Assert.False(Berichtswache.Steht("37.25", ""));
        }

        [Fact]
        public void Eine_Stelle_Nachkomma_ist_kein_Fingerabdruck()
        {
            // Der Grund für STELLEN_VON und MINDESTZIFFERN: "-37,2 %" ist eine Abweichung, keine
            // Menge, und "0,621" ist ebenso gut eine Bandgrenze. Würde die Wache auf so kurzen Zahlen
            // suchen, hielte sie jeden Bericht zurück, in dem eine solche Verhältniszahl steht.
            Assert.True(Berichtswache.STELLEN_VON >= 2);
            var reihe = new Messreihe("Probe", ZapfMessgroesse.Energie, 60, new DateTime(2025, 1, 1),
                                      new[] { 37.2, 10.0, 5.0 }, "Probe");
            IReadOnlyList<string> verboten = Berichtswache.Verbotene(reihe, 0.0);
            Assert.DoesNotContain("37.2", verboten);
            Assert.DoesNotContain("37.20", verboten);        // vier Ziffern sind kein Fingerabdruck
            Assert.Contains("37.200000", verboten);
            Assert.Contains("37,200000", verboten);
        }

        [Fact]
        public void Die_verbotenen_Schreibweisen_umfassen_Menge_Spitze_und_Tagesmittel()
        {
            var reihe = new Messreihe("Probe", ZapfMessgroesse.Energie, 60, new DateTime(2025, 1, 1),
                                      new[] { 1111.11, 2222.22, 3333.33 }, "Probe");
            IReadOnlyList<string> verboten = Berichtswache.Verbotene(reihe, 0.0);
            Assert.Contains("6666.66", verboten);        // Menge, sechs Ziffern
            Assert.Contains("3333.33", verboten);        // groesster Wert
            Assert.Contains(verboten, z => z.Contains(','));      // auch die Kommafassung
            Assert.All(verboten, z => Assert.True(z.Count(char.IsDigit) >= Berichtswache.MINDESTZIFFERN));
        }

        [Fact]
        public void Ohne_Reihe_prueft_die_Wache_nur_die_Einheiten()
        {
            Assert.Empty(Berichtswache.Verbotene(null, 48.0));
            Assert.Empty(Berichtswache.Pruefen("Alles Verhaeltnisse.", null, 48.0));
            Assert.NotEmpty(Berichtswache.Pruefen("Jahresmenge in kWh.", null, 48.0));
        }

        [Fact]
        public void Ein_Bericht_mit_einem_Absolutwert_wird_nicht_geschrieben()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;

            // Die Gegenprobe zum Ernstfall: Ein Objekt, dessen VERMERK die Jahresmenge der eigenen
            // Messreihe traegt. Der Vermerk geht nicht in den Bericht - deshalb wird hier der
            // Berichtstext selbst geprueft, mit der Zahl eingesetzt.
            Katalog katalog = Katalogquelle.Lesen(v.Katalog, out string fehler);
            Assert.Null(fehler);
            Objektbeschreibung o = Objektbeschreibung.Lesen(
                Vorrichtung.Objektdatei(v.Beispiel, "BSP-WOHNEN-01"), out string lesefehler);
            Assert.Null(lesefehler);
            Objektbefund b = Objektlauf.Rechnen(Path.Combine(v.Beispiel, "BSP-WOHNEN-01"), o, katalog, null, null);

            Assert.NotEmpty(b.Verbotene);
            string echt = Bericht.ObjektMarkdown(b);
            Assert.Empty(Berichtswache.Pruefen(echt, b.Verbotene));

            string verdorben = echt + Environment.NewLine + "Jahresmenge der Messung: " + b.Verbotene[0] + ".";
            List<string> funde = Berichtswache.Pruefen(verdorben, b.Verbotene);
            Assert.NotEmpty(funde);
            Assert.Contains("K5", funde[0], StringComparison.Ordinal);
            Assert.Contains("Jahresmenge der Messung", Berichtswache.Umfeld(verdorben, b.Verbotene[0]),
                            StringComparison.Ordinal);
        }

        /// <summary>Die verbotenen Schreibweisen eines Beispielobjekts.</summary>
        private static IReadOnlyList<string> Kennzahlen(Vorrichtung v, string kennung)
        {
            Katalog katalog = Katalogquelle.Lesen(v.Katalog, out _);
            Objektbeschreibung o = Objektbeschreibung.Lesen(Vorrichtung.Objektdatei(v.Beispiel, kennung), out _);
            Objektbefund b = Objektlauf.Rechnen(Path.Combine(v.Beispiel, kennung), o, katalog, null, null);
            return b.Verbotene;
        }
    }
}
