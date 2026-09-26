using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Der Lauf des Werkzeugs am mitgelieferten Beispiel</b> (Stufe Z5, offener Punkt K5). Das
    /// Beispiel ist <b>durch Konstruktion</b> grün: Seine Messreihe stammt aus derselben Rechnung und
    /// ist so gestreckt, dass die Spitze in der Mitte des Bands liegt
    /// (<see cref="Beispielreihe"/>). Geprüft wird damit der Weg des Werkzeugs — Katalog, Eingang,
    /// Vergleich, Kalibrierung, Ampel, Bericht —, nicht der Rechenweg des Generators; den halten die
    /// Kern-Tests.
    /// </summary>
    public sealed class BeispiellaufTests
    {
        [Fact]
        public void Das_Beispiel_laeuft_durch_und_ist_gruen()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string ziel = v.Neu("berichte");

            (int code, string aus, string fehler) = v.Lauf(v.Beispiel, "--katalog", v.Katalog, "--ziel", ziel);

            Assert.Equal(Einstieg.OK, code);
            Assert.True(string.IsNullOrWhiteSpace(fehler), "stderr traegt: " + fehler);
            Assert.Contains("BSP-WOHNEN-01: gruen", aus, StringComparison.Ordinal);
            Assert.Contains("BSP-PFLEGE-01: gruen", aus, StringComparison.Ordinal);

            foreach (string datei in new[] { "BSP-WOHNEN-01.md", "BSP-WOHNEN-01.csv", "BSP-PFLEGE-01.md",
                                             "BSP-PFLEGE-01.csv", Einstieg.SAMMEL_MD, Einstieg.SAMMEL_CSV })
                Assert.True(File.Exists(Path.Combine(ziel, datei)), datei + " fehlt im Zielordner.");

            string sammel = File.ReadAllText(Path.Combine(ziel, Einstieg.SAMMEL_MD));
            Assert.Contains("| gruen | 2 |", sammel, StringComparison.Ordinal);
            Assert.DoesNotContain("| rot | 1 |", sammel, StringComparison.Ordinal);
        }

        [Fact]
        public void Das_Wohnobjekt_erfuellt_die_drei_Kriterien_mit_Mass()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            Objektbefund b = Befund(v, v.Beispiel, "BSP-WOHNEN-01");

            Assert.Null(b.Abbruch);
            Assert.Equal(Ampel.Gruen, b.Gesamt);
            Assert.Equal(3, b.Kriterien.Count);
            Assert.All(b.Kriterien, k => Assert.Equal(Ampel.Gruen, k.Ampel));

            // (b) Die Spitze liegt im Band, und das Band liegt unter 1 (Konzept 3.6, Lehre 1).
            Assert.Equal(Spitzenlage.ImBand, b.Lage);
            Assert.True(b.BandOben < 1.0, "Das Band der Dauerlinie muss unter 1 liegen.");
            Assert.InRange(b.Spitzenverhaeltnis.Value, b.BandUnten.Value, b.BandOben.Value);

            // (d) Das Formmass haelt die Schwelle deutlich: Die Reihe traegt die Form der Rechnung;
            // was sie von ihr trennt, ist das Rauschen und die Kappung der Spitzen.
            Assert.True(b.Formmass < b.Formschwelle / 2.0,
                "Das Formmass sollte deutlich unter der Schwelle liegen, ist aber " + b.Formmass + ".");

            // Der Vergleich lief gegen die kalibrierte Reihe; das Niveau stimmt dann von selbst.
            Assert.True(b.GegenKalibrierteReihe);
            Assert.Equal(1.0, b.Kalibrierfaktor.Value, 6);
            Assert.Equal(1.0, b.EnergieVerhaeltnis.Value, 6);

            // (4) Nach der Kalibrierung ist die Energie exakt.
            Assert.True(b.EnergieResiduum <= Objektbefund.ENERGIE_GENAU,
                "Residuum " + b.EnergieResiduum + " ueber der Schranke.");

            // Ein volles Jahr, keine Luecke, keine Stochastik.
            Assert.False(b.Teiljahr);
            Assert.Equal(0.0, b.Lueckenanteil);
            Assert.False(b.Stochastisch);
            Assert.Equal(48, b.Einheiten);
        }

        [Fact]
        public void Das_Nichtwohnobjekt_bekommt_einen_Kalibriervorschlag()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            Objektbefund b = Befund(v, v.Beispiel, "BSP-PFLEGE-01");

            Assert.Null(b.Abbruch);
            Assert.NotEqual(ZapfKalenderart.Wohnen, b.Kalenderart);
            Assert.NotNull(b.Vorschlag);
            Assert.True(b.Vorschlag.VolleTage > 300,
                "Aus einem vollen Jahr sollten fast alle Tage vollstaendig sein, sind aber "
                + b.Vorschlag.VolleTage + ".");
            Assert.Equal(7, b.Vorschlag.Wochenfaktoren.Count);
            Assert.Equal(1.0, b.Vorschlag.Wochenfaktoren.Sum(), 9);
            Assert.NotNull(b.Vorschlag.TagesbedarfVerhaeltnis);
            Assert.True(b.Vorschlag.TagesbedarfVerhaeltnis > 0.0);
        }

        [Fact]
        public void Das_Wohnobjekt_bekommt_keinen_Kalibriervorschlag()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            Objektbefund b = Befund(v, v.Beispiel, "BSP-WOHNEN-01");
            Assert.Equal(ZapfKalenderart.Wohnen, b.Kalenderart);
            Assert.Null(b.Vorschlag);
        }

        [Fact]
        public void Ein_Teiljahr_wird_benannt_hochgerechnet_und_bleibt_energiegenau()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string quelle = v.ObjektKopie("BSP-WOHNEN-01", "teiljahr");
            string reihe = Vorrichtung.Messdatei(quelle, "BSP-WOHNEN-01");
            List<(DateTime Zeit, double Wert)> alle = Vorrichtung.ReiheLesen(reihe);
            // 60 Tage ab dem 1. Februar - deutlich ueber den 30 Mindesttagen, klar ein Teiljahr.
            Vorrichtung.ReiheSchreiben(reihe, Beispielreihe.KOPF, alle.Skip(31 * 24).Take(60 * 24));

            Objektbefund b = Befund(v, quelle, "BSP-WOHNEN-01");

            Assert.Null(b.Abbruch);
            Assert.True(b.Teiljahr, "Eine Reihe von 60 Tagen ist ein Teiljahr.");
            Assert.InRange(b.MesstageGesamt, 59.9, 60.1);
            Assert.InRange(b.Jahresanteil, 0.1, 0.2);
            Assert.Contains(b.Hinweise, h => h.Contains("MESSKALIBRIERUNG_HOCHGERECHNET", StringComparison.Ordinal));
            Assert.True(b.EnergieResiduum <= Objektbefund.ENERGIE_GENAU,
                "Auch aus einem Teiljahr ist die Energie nach der Kalibrierung exakt; Residuum "
                + b.EnergieResiduum + ".");
        }

        [Fact]
        public void Eine_Volumenreihe_wird_ueber_die_Spreizung_gerechnet()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;

            Objektbefund energie = Befund(v, v.Beispiel, "BSP-WOHNEN-01");

            // Dieselbe Reihe als Volumen: V[m³] = Q[kWh] / (1,163 · Δθ) mit Δθ = 60 − 12 = 48 K
            // (Mengengeruest.EnergieKwh). Der Vergleich muss dieselben Verhaeltnisse liefern.
            const double spreizung = 48.0;
            double faktor = 1.0 / (1.163 * spreizung);
            string quelle = v.ObjektKopie("BSP-WOHNEN-01", "volumen");
            string reihe = Vorrichtung.Messdatei(quelle, "BSP-WOHNEN-01");
            List<(DateTime Zeit, double Wert)> alle = Vorrichtung.ReiheLesen(reihe);
            Vorrichtung.ReiheSchreiben(reihe, "Zeitstempel;Volumen (m³)",
                                       alle.Select(z => (z.Zeit, z.Wert * faktor)));
            Vorrichtung.JsonErsetzen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                     "\"groesse\": \"Energie\"", "\"groesse\": \"Volumen\"");

            Objektbefund volumen = Befund(v, quelle, "BSP-WOHNEN-01");

            Assert.Null(volumen.Abbruch);
            Assert.Equal(Ampel.Gruen, volumen.Gesamt);
            // Die Umrechnung ist linear; nur die sechs geschriebenen Stellen trennen beide Wege.
            Assert.Equal(energie.EnergieVerhaeltnis.Value, volumen.EnergieVerhaeltnis.Value, 4);
            Assert.Equal(energie.Spitzenverhaeltnis.Value, volumen.Spitzenverhaeltnis.Value, 4);
            Assert.Equal(energie.Kalibrierfaktor.Value, volumen.Kalibrierfaktor.Value, 4);
        }

        [Fact]
        public void Mit_Ensemble_steht_die_Spitzenstreuung()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string quelle = v.ObjektKopie("BSP-WOHNEN-01", "ensemble");
            Vorrichtung.JsonErsetzen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                     "\"jahresreihe_stochastisch\": false", "\"jahresreihe_stochastisch\": true");

            Katalog katalog = Katalogquelle.Lesen(v.Katalog, out string fehler);
            Assert.Null(fehler);
            Objektbeschreibung o = Objektbeschreibung.Lesen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                                           out string lesefehler);
            Assert.Null(lesefehler);
            Objektbefund b = Objektlauf.Rechnen(Path.Combine(quelle, "BSP-WOHNEN-01"), o, katalog, 8, 4711);

            Assert.Null(b.Abbruch);
            Assert.True(b.Stochastisch, "Mit acht Realisierungen rechnet die Jahresreihe stochastisch.");
            Assert.Equal(8, b.StreuungRealisierungen);
            Assert.NotNull(b.Streubreite);
            Assert.True(b.Streubreite >= 1.0, "Die Streubreite ist oben/unten und damit mindestens 1.");
            Assert.DoesNotContain(b.Hinweise,
                h => h.Contains("MESSVERGLEICH_OHNE_ENSEMBLE", StringComparison.Ordinal));
            // Die Energie bleibt auch stochastisch nach der Kalibrierung exakt.
            Assert.True(b.EnergieResiduum <= Objektbefund.ENERGIE_GENAU);
        }

        /// <summary>
        /// <b>Folge V7:</b> Die Spitzenstreuung bezieht die Realisierungsspitzen auf dieselbe Stufe
        /// wie die verglichene, kalibrierte Reihe. Dieselbe Messreihe mal drei ergibt den dreifachen
        /// Kalibrierfaktor — Spitzenverhältnis und Streuung bleiben gleich. Ohne die Skalierung der
        /// Spitzen fiele die Streuung auf ein Drittel.
        /// </summary>
        [Fact]
        public void Die_Spitzenstreuung_haengt_nicht_am_Kalibrierfaktor()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            Objektbefund einfach = Ensemblebefund(v, "streuung-1", 1.0);
            Objektbefund dreifach = Ensemblebefund(v, "streuung-3", 3.0);

            Assert.Null(einfach.Abbruch);
            Assert.Null(dreifach.Abbruch);
            Assert.True(einfach.GegenKalibrierteReihe && dreifach.GegenKalibrierteReihe);
            Assert.Equal(3.0, dreifach.Kalibrierfaktor.Value / einfach.Kalibrierfaktor.Value, 4);
            Assert.NotEqual(1.0, dreifach.Kalibrierfaktor.Value, 2);
            Assert.Equal(einfach.Spitzenverhaeltnis.Value, dreifach.Spitzenverhaeltnis.Value, 4);
            Assert.Equal(einfach.StreuungRealisierungen, dreifach.StreuungRealisierungen);
            Assert.Equal(einfach.StreuungUnten.Value, dreifach.StreuungUnten.Value, 4);
            Assert.Equal(einfach.StreuungOben.Value, dreifach.StreuungOben.Value, 4);
            Assert.Equal(einfach.Streubreite.Value, dreifach.Streubreite.Value, 4);
        }

        /// <summary>Das Wohnobjekt mit Ensemble (acht Realisierungen), die Messreihe mal <paramref name="faktor"/>.</summary>
        private static Objektbefund Ensemblebefund(Vorrichtung v, string name, double faktor)
        {
            string quelle = v.ObjektKopie("BSP-WOHNEN-01", name);
            Vorrichtung.JsonErsetzen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                     "\"jahresreihe_stochastisch\": false", "\"jahresreihe_stochastisch\": true");
            string reihe = Vorrichtung.Messdatei(quelle, "BSP-WOHNEN-01");
            List<(DateTime Zeit, double Wert)> alle = Vorrichtung.ReiheLesen(reihe);
            Vorrichtung.ReiheSchreiben(reihe, Beispielreihe.KOPF, alle.Select(z => (z.Zeit, z.Wert * faktor)));

            Katalog katalog = Katalogquelle.Lesen(v.Katalog, out string fehler);
            Assert.Null(fehler);
            Objektbeschreibung o = Objektbeschreibung.Lesen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                                           out string lesefehler);
            Assert.Null(lesefehler);
            return Objektlauf.Rechnen(Path.Combine(quelle, "BSP-WOHNEN-01"), o, katalog, 8, 4711);
        }

        [Fact]
        public void Ohne_Ensemble_wird_die_fehlende_Streuung_benannt()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            Objektbefund b = Befund(v, v.Beispiel, "BSP-WOHNEN-01");
            Assert.Null(b.Streubreite);
            Assert.Contains(b.Hinweise, h => h.Contains("MESSVERGLEICH_OHNE_ENSEMBLE", StringComparison.Ordinal));
        }

        [Fact]
        public void Der_Trockenlauf_rechnet_und_schreibt_nichts()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string ziel = Path.Combine(v.Arbeitsordner, "trocken-ziel");

            (int code, string aus, _) = v.Lauf(v.Beispiel, "--katalog", v.Katalog, "--ziel", ziel, "--trocken");

            Assert.Equal(Einstieg.OK, code);
            Assert.Contains("Trockenlauf", aus, StringComparison.Ordinal);
            Assert.Contains("BSP-WOHNEN-01: gruen", aus, StringComparison.Ordinal);
            Assert.False(Directory.Exists(ziel), "Der Trockenlauf darf den Zielordner nicht anlegen.");
        }

        [Theory]
        [InlineData("Der Quellordner fehlt.")]
        public void Ohne_Argumente_kommt_die_Hilfe(string _)
        {
            using var v = new Vorrichtung();
            (int code, string aus, _) = v.Lauf();
            Assert.Equal(Einstieg.AUFRUF, code);
            Assert.Contains("--ziel", aus, StringComparison.Ordinal);
            Assert.Contains("Verhaeltniszahlen", aus, StringComparison.Ordinal);
        }

        [Fact]
        public void Ein_fehlerhafter_Aufruf_wird_benannt_abgelehnt()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;

            Assert.Equal(Einstieg.AUFRUF, v.Lauf(v.Beispiel, "--katalog", v.Katalog).Code);        // kein Ziel
            Assert.Equal(Einstieg.AUFRUF, v.Lauf(v.Beispiel, "--quatsch").Code);                   // Schalter
            Assert.Equal(Einstieg.AUFRUF, v.Lauf("gibt-es-nicht", "--ziel", v.Neu("z1")).Code);     // Quelle
            Assert.Equal(Einstieg.AUFRUF,
                v.Lauf(v.Beispiel, "--katalog", "gibt-es-nicht", "--ziel", v.Neu("z2")).Code);      // Katalog
            Assert.Equal(Einstieg.AUFRUF,
                v.Lauf(v.Neu("leer"), "--katalog", v.Katalog, "--ziel", v.Neu("z3")).Code);        // kein Objekt
            Assert.Equal(Einstieg.AUFRUF,
                v.Lauf(v.Beispiel, "--realisierungen", "viele", "--ziel", v.Neu("z4")).Code);      // keine Zahl
        }

        [Fact]
        public void Ein_unbekannter_Nutzungsartbezeichner_ist_ein_benannter_Abbruch()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string quelle = v.ObjektKopie("BSP-WOHNEN-01", "fremdeart");
            Vorrichtung.JsonErsetzen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                     "\"Beispiel Wohnen (erfunden)\"", "\"Gibt es nicht\"");
            string ziel = v.Neu("fremdeart-ziel");

            (int code, _, _) = v.Lauf(quelle, "--katalog", v.Katalog, "--ziel", ziel);

            Assert.Equal(Einstieg.NICHT_ABGENOMMEN, code);
            string bericht = File.ReadAllText(Path.Combine(ziel, "BSP-WOHNEN-01.md"));
            Assert.Contains("Nicht ausgewertet", bericht, StringComparison.Ordinal);
            Assert.Contains("Gibt es nicht", bericht, StringComparison.Ordinal);
        }

        /// <summary>Rechnet ein Objekt des Beispiels (oder einer Kopie) ohne Bericht.</summary>
        private static Objektbefund Befund(Vorrichtung v, string quelle, string kennung)
        {
            Katalog katalog = Katalogquelle.Lesen(v.Katalog, out string fehler);
            Assert.Null(fehler);
            Objektbeschreibung o = Objektbeschreibung.Lesen(Vorrichtung.Objektdatei(quelle, kennung),
                                                           out string lesefehler);
            Assert.Null(lesefehler);
            return Objektlauf.Rechnen(Path.Combine(quelle, kennung), o, katalog, null, null);
        }
    }
}
