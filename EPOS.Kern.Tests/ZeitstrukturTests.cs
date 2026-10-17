using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Zeitstruktur übernehmen"</b> (Entwurf KP2, Welle K1, Festlegung 11; Teilkonzept
    /// Konditionierungsprofile 3.5): Kühlen, Lüftung oder Geräte übernehmen die Zeitstruktur „wie
    /// Heizung" (Stunden der Standardwoche mit endlichem Wert ≥ Tagwert der Heizspalte) oder „wie
    /// Anwesenheit" (Anteil &gt; 0): Diese Stunden bekommen den Tagwert des Ziels, die übrigen dessen
    /// Nachtwert. Ersetzt wird nur die Standardwoche; Perioden und Nennwert bleiben.
    ///
    /// <para>Ohne Datenbank — <see cref="Kalenderwerkzeuge.ZeitstrukturUebernehmen"/> ist eine reine
    /// Funktion. Die Kultur ist auf de-DE gepinnt: Vermerk und Meldungen sind deutsche Ressourcentexte.</para>
    /// </summary>
    public sealed class ZeitstrukturTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>
        /// „wie Heizung": Mo–Fr 7–18 Uhr 20 °C (dazu Di 18 Uhr 21 °C), Mi 19 Uhr 19,5 °C, sonst 16 °C und
        /// So 12 Uhr „aus". Stunden ≥ Tagwert 20 °C: 55 + 1. Kühlen Tag 26 °C, Nacht „aus"; die Ferienperiode
        /// des Kühlkalenders bleibt.
        /// </summary>
        [Fact]
        public void Wie_Heizung_bekommen_die_Heizstunden_den_Tagwert_des_Ziels_die_uebrigen_den_Nachtwert()
        {
            Konditionierungskalender heizen = Woche(Konditionierungsgroesse.Heizsoll, (t, s) =>
                t == 1 && s == 18 ? 21.0
                : t == 2 && s == 19 ? 19.5
                : t == 6 && s == 12 ? double.NaN
                : t < 5 && s >= 7 && s < 18 ? 20.0 : 16.0);
            var ferien = Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN, "Ferien 1", 180, 200,
                                                Kalenderangabe.Abgeschaltet);
            var kuehlen = new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll, Kalenderangabe.AusWert(24.0), null,
                                                       new[] { ferien });

            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                kuehlen, Zeitstrukturquelle.WieHeizung, heizen, 20.0,
                Matrixzelle.AusWert(26.0), Matrixzelle.Abgeschaltet());

            Assert.True(b.Ok, b.Meldung);
            IReadOnlyList<double> w = b.Kalender.Standardwoche;
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                {
                    bool heizt = (t < 5 && s >= 7 && s < 18) || (t == 1 && s == 18);
                    double v = w[Kalenderwoche.Stelle(t, s)];
                    if (heizt) Assert.Equal(26.0, v);
                    else Assert.True(double.IsNaN(v), "Stunde " + t + "/" + s + " = " + v);
                }
            Assert.Equal(Konditionierungsgroesse.Kuehlsoll, b.Kalender.Groesse);
            Assert.Same(ferien, Assert.Single(b.Kalender.Perioden));
            Assert.Null(b.Kalender.Nennwert);
            Assert.Equal("Zeitstruktur wie Heizung: 56 Wochenstunden = 26, sonst aus", b.Vermerk);
        }

        /// <summary>
        /// „wie Anwesenheit": Stunden mit Anteil &gt; 0 (auch 0,5); 0 und „aus" zählen nicht. Geräte Tag 100 %,
        /// Nacht 10 % — der Nennwert bleibt.
        /// </summary>
        [Fact]
        public void Wie_Anwesenheit_nimmt_die_Stunden_mit_Anteil_ueber_null()
        {
            Konditionierungskalender personen = Woche(Konditionierungsgroesse.Personen, (t, s) =>
                t < 5 && s >= 8 && s < 17 ? 1.0 : t < 5 && s == 17 ? 0.5 : t == 5 && s == 10 ? double.NaN : 0.0);
            var geraete = new Konditionierungskalender(Konditionierungsgroesse.Geraete, Kalenderangabe.AusWert(1.0), 500.0, null);

            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                geraete, Zeitstrukturquelle.WieAnwesenheit, personen, null,
                Matrixzelle.AusWert(1.0), Matrixzelle.AusWert(0.1));

            Assert.True(b.Ok, b.Meldung);
            IReadOnlyList<double> w = b.Kalender.Standardwoche;
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                    Assert.Equal(t < 5 && s >= 8 && s < 18 ? 1.0 : 0.1, w[Kalenderwoche.Stelle(t, s)]);
            Assert.Equal(500.0, b.Kalender.Nennwert);
            Assert.Empty(b.Kalender.Perioden);
            // Die Werte stehen invariant im Vermerk, wie beim Zeitfenster (Punkt als Dezimalzeichen).
            Assert.Equal("Zeitstruktur wie Anwesenheit: 50 Wochenstunden = 1, sonst 0.1", b.Vermerk);
        }

        /// <summary>Die Lüftung „wie Heizung" aus einer Quelle ohne Standardwoche: Die Grundangabe zählt.</summary>
        [Fact]
        public void Eine_Quelle_ohne_Standardwoche_zaehlt_ihre_Grundangabe()
        {
            var lueftung = new Konditionierungskalender(Konditionierungsgroesse.Lueftung, Kalenderangabe.AusWert(0.4), null, null);
            var konstant = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, null);
            var aus = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.Abgeschaltet, null, null);

            Kalenderwerkzeuge.Werkzeugbefund alle = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                lueftung, Zeitstrukturquelle.WieHeizung, konstant, 20.0, Matrixzelle.AusWert(0.5), Matrixzelle.AusWert(0.1));
            Assert.True(alle.Ok, alle.Meldung);
            Assert.All(alle.Kalender.Standardwoche, v => Assert.Equal(0.5, v));

            Kalenderwerkzeuge.Werkzeugbefund keine = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                lueftung, Zeitstrukturquelle.WieHeizung, aus, 20.0, Matrixzelle.AusWert(0.5), Matrixzelle.AusWert(0.1));
            Assert.True(keine.Ok, keine.Meldung);
            Assert.All(keine.Kalender.Standardwoche, v => Assert.Equal(0.1, v));
        }

        /// <summary>Fehlt der Tag- oder der Nachtwert des Ziels, lehnt das Werkzeug benannt ab und nennt die Spalte.</summary>
        [Fact]
        public void Ein_fehlender_Tag_oder_Nachtwert_wird_benannt_abgelehnt()
        {
            Konditionierungskalender heizen = Konstant(Konditionierungsgroesse.Heizsoll, 20.0);
            Konditionierungskalender kuehlen = Konstant(Konditionierungsgroesse.Kuehlsoll, 24.0);

            Kalenderwerkzeuge.Werkzeugbefund ohneTag = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                kuehlen, Zeitstrukturquelle.WieHeizung, heizen, 20.0, Matrixzelle.Leer, Matrixzelle.AusWert(28.0));
            Kalenderwerkzeuge.Werkzeugbefund ohneNacht = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                kuehlen, Zeitstrukturquelle.WieHeizung, heizen, 20.0, Matrixzelle.AusWert(26.0), Matrixzelle.NurZeiten(18, 7));

            foreach (Kalenderwerkzeuge.Werkzeugbefund b in new[] { ohneTag, ohneNacht })
            {
                Assert.False(b.Ok);
                Assert.Null(b.Kalender);
                Assert.Equal("„Zeitstruktur übernehmen“ braucht in der Spalte KUEHLSOLL einen Tag- und einen Nachtwert — "
                             + "als Zahl, beim Kühlen auch „aus“.", b.Meldung);
            }
        }

        /// <summary>„aus" gilt nur beim Kühlen; Lüftung und Geräte brauchen Zahlen.</summary>
        [Fact]
        public void Aus_ist_nur_beim_Kuehlen_ein_Tag_oder_Nachtwert()
        {
            Konditionierungskalender heizen = Konstant(Konditionierungsgroesse.Heizsoll, 20.0);

            Kalenderwerkzeuge.Werkzeugbefund kuehlen = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Kuehlsoll, 24.0), Zeitstrukturquelle.WieHeizung, heizen, 20.0,
                Matrixzelle.Abgeschaltet(), Matrixzelle.AusWert(28.0));
            Assert.True(kuehlen.Ok, kuehlen.Meldung);
            Assert.All(kuehlen.Kalender.Standardwoche, v => Assert.True(double.IsNaN(v)));
            Assert.Equal("Zeitstruktur wie Heizung: 168 Wochenstunden = aus, sonst 28", kuehlen.Vermerk);

            Kalenderwerkzeuge.Werkzeugbefund lueftung = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Lueftung, 0.4), Zeitstrukturquelle.WieHeizung, heizen, 20.0,
                Matrixzelle.AusWert(0.5), Matrixzelle.Abgeschaltet());
            Assert.False(lueftung.Ok);
            Assert.Contains("LUEFTUNG", lueftung.Meldung);

            Kalenderwerkzeuge.Werkzeugbefund geraete = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Geraete, 1.0), Zeitstrukturquelle.WieHeizung, heizen, 20.0,
                Matrixzelle.Abgeschaltet(), Matrixzelle.AusWert(0.1));
            Assert.False(geraete.Ok);
            Assert.Contains("GERAETE", geraete.Meldung);
        }

        /// <summary>Heizen und Personen sind kein Ziel des Werkzeugs.</summary>
        [Theory]
        [InlineData(Konditionierungsgroesse.Heizsoll, "HEIZSOLL")]
        [InlineData(Konditionierungsgroesse.Personen, "PERSONEN")]
        public void Heizen_und_Personen_sind_kein_Ziel(Konditionierungsgroesse ziel, string kennwort)
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(ziel, 0.5), Zeitstrukturquelle.WieAnwesenheit, Konstant(Konditionierungsgroesse.Personen, 1.0), null,
                Matrixzelle.AusWert(0.5), Matrixzelle.AusWert(0.1));
            Assert.False(b.Ok);
            Assert.Equal("„Zeitstruktur übernehmen“ wirkt auf Kühlen, Lüftung oder Geräte, nicht auf " + kennwort + ".", b.Meldung);
        }

        /// <summary>„wie Heizung" braucht den Tagwert der Heizspalte als endliche Zahl.</summary>
        [Fact]
        public void Wie_Heizung_braucht_den_Tagwert_der_Heizspalte()
        {
            foreach (double? tag in new double?[] { null, double.NaN, double.PositiveInfinity })
            {
                Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                    Konstant(Konditionierungsgroesse.Lueftung, 0.4), Zeitstrukturquelle.WieHeizung,
                    Konstant(Konditionierungsgroesse.Heizsoll, 20.0), tag, Matrixzelle.AusWert(0.5), Matrixzelle.AusWert(0.1));
                Assert.False(b.Ok);
                Assert.Equal("„Zeitstruktur wie Heizung“ braucht den Tagwert der Heizspalte.", b.Meldung);
            }
        }

        /// <summary>Ein Wert außerhalb der Grenzen des Ziels oder ohne bitgleichen Rundlauf wird benannt abgelehnt.</summary>
        [Fact]
        public void Ein_Wert_ausserhalb_der_Grenzen_oder_ohne_Rundlauf_wird_abgelehnt()
        {
            Konditionierungskalender heizen = Konstant(Konditionierungsgroesse.Heizsoll, 20.0);

            Kalenderwerkzeuge.Werkzeugbefund zuHoch = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Lueftung, 0.4), Zeitstrukturquelle.WieHeizung, heizen, 20.0,
                Matrixzelle.AusWert(25.0), Matrixzelle.AusWert(0.1));
            Assert.False(zuHoch.Ok);
            Assert.StartsWith("Der Wert 25 liegt außerhalb", zuHoch.Meldung, StringComparison.Ordinal);

            Kalenderwerkzeuge.Werkzeugbefund krumm = Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Geraete, 1.0), Zeitstrukturquelle.WieHeizung, heizen, 20.0,
                Matrixzelle.AusWert(1.0), Matrixzelle.AusWert(0.123456));
            Assert.False(krumm.Ok);
            Assert.Contains("0.123456", krumm.Meldung);
        }

        /// <summary>Die Quelle muss die Größe ihrer Art tragen — ein Aufruferfehler, keine Anwendermeldung.</summary>
        [Fact]
        public void Eine_Quelle_der_falschen_Groesse_ist_ein_Aufruferfehler()
        {
            Assert.Throws<ArgumentException>(() => Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Kuehlsoll, 24.0), Zeitstrukturquelle.WieHeizung,
                Konstant(Konditionierungsgroesse.Personen, 1.0), 20.0, Matrixzelle.AusWert(26.0), Matrixzelle.AusWert(28.0)));
            Assert.Throws<ArgumentException>(() => Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                Konstant(Konditionierungsgroesse.Kuehlsoll, 24.0), Zeitstrukturquelle.WieAnwesenheit,
                Konstant(Konditionierungsgroesse.Heizsoll, 20.0), null, Matrixzelle.AusWert(26.0), Matrixzelle.AusWert(28.0)));
            Assert.Throws<ArgumentNullException>(() => Kalenderwerkzeuge.ZeitstrukturUebernehmen(
                null, Zeitstrukturquelle.WieHeizung, Konstant(Konditionierungsgroesse.Heizsoll, 20.0), 20.0,
                Matrixzelle.AusWert(26.0), Matrixzelle.AusWert(28.0)));
        }

        // =================================================================================
        // Handwerkszeug
        // =================================================================================

        private static Konditionierungskalender Konstant(Konditionierungsgroesse g, double wert)
            => new Konditionierungskalender(g, Kalenderangabe.AusWert(wert),
                                            Konditionierungsgroessen.HatNennwert(g) ? 300.0 : (double?)null, null);

        /// <summary>Ein Kalender der Größe <paramref name="g"/> mit der Woche <paramref name="wert"/>(Tag, Stunde); NaN = „aus".</summary>
        private static Konditionierungskalender Woche(Konditionierungsgroesse g, Func<int, int, double> wert)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                    woche[Kalenderwoche.Stelle(t, s)] = wert(t, s);
            return new Konditionierungskalender(g, Kalenderangabe.AusWoche(woche), null, null);
        }
    }
}
