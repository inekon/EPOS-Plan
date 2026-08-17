using System;
using System.Collections.Generic;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Spotreihen-Aufbereitung (Arbeitspaket AP4, Fachkonzept 4.1 a):
    /// Zeitzonenumstellung im Fruehjahr und im Herbst, Schaltjahr und
    /// Kalenderausrichtung.
    /// </summary>
    /// <remarks>
    /// Der Testfall bildet das Jahr 2024 nach - dasselbe Jahr wie die Abnahmedatei
    /// "Spotmarktpreise 2024.csv": Schaltjahr (366 Tage), Fruehjahrsumstellung am
    /// 31.03. (23 Stunden), Herbstumstellung am 27.10. (25 Stunden), 8.784 Zeilen.
    /// Die Werte sind synthetisch und aus der Kalenderstelle ableitbar, damit jede
    /// Stunde eindeutig ihrem Zielplatz zugeordnet werden kann.
    /// </remarks>
    public sealed class SpotreihenAufbereitungTests
    {
        private static readonly int[] TageProMonatSchaltjahr = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static readonly int[] TageProMonatNormaljahr = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Fruehjahrsumstellung 2024: letzter Sonntag im Maerz.</summary>
        private const int UmstellungFruehjahrTag = 31;

        /// <summary>Herbstumstellung 2024: letzter Sonntag im Oktober.</summary>
        private const int UmstellungHerbstTag = 27;

        /// <summary>
        /// Eindeutiger Wert je Kalenderstelle: <c>Monat*10000 + Tag*100 + Stunde</c>,
        /// auf ct/kWh skaliert. Damit laesst sich aus jedem Reihenwert zurueckrechnen,
        /// welche Quellzeile ihn erzeugt hat.
        /// </summary>
        private static double Kennwert(int monat, int tag, int stunde, int lauf = 0)
        {
            return (monat * 10000 + tag * 100 + stunde) / 1000.0 + lauf * 0.0005;
        }

        /// <summary>Zielindex im Normaljahr-Raster (365 x 24).</summary>
        private static int Zielindex(int monat, int tag, int stunde)
        {
            int tageVorher = 0;
            for (int m = 0; m < monat - 1; m++) tageVorher += TageProMonatNormaljahr[m];
            return (tageVorher + tag - 1) * 24 + stunde;
        }

        /// <summary>
        /// Erzeugt die 8.784 Zeilen des Jahres 2024 mit exakt der Zeitzonenfuehrung
        /// der echten Datei.
        /// </summary>
        private static List<SpotStundenwert> Jahr2024()
        {
            List<SpotStundenwert> zeilen = new List<SpotStundenwert>();

            for (int monat = 1; monat <= 12; monat++)
            {
                for (int tag = 1; tag <= TageProMonatSchaltjahr[monat - 1]; tag++)
                {
                    // --- Fruehjahrsumstellung: 23 Stunden, 02:00 existiert nicht ---
                    if (monat == 3 && tag == UmstellungFruehjahrTag)
                    {
                        zeilen.Add(new SpotStundenwert(3, tag, 0, false, false, Kennwert(3, tag, 0)));
                        // 01:00 CET -> 03:00 CEST: hier springt die Uhr
                        zeilen.Add(new SpotStundenwert(3, tag, 1, false, true, Kennwert(3, tag, 1)));
                        for (int h = 3; h < 24; h++)
                            zeilen.Add(new SpotStundenwert(3, tag, h, true, true, Kennwert(3, tag, h)));
                        continue;
                    }

                    // --- Herbstumstellung: 25 Stunden, 02:00 kommt zweimal vor ---
                    if (monat == 10 && tag == UmstellungHerbstTag)
                    {
                        zeilen.Add(new SpotStundenwert(10, tag, 0, true, true, Kennwert(10, tag, 0)));
                        zeilen.Add(new SpotStundenwert(10, tag, 1, true, true, Kennwert(10, tag, 1)));
                        // 02:00 CEST -> 02:00 CET (erste Zaehlung), dann 02:00 CET -> 03:00 CET
                        zeilen.Add(new SpotStundenwert(10, tag, 2, true, false, Kennwert(10, tag, 2, 0)));
                        zeilen.Add(new SpotStundenwert(10, tag, 2, false, false, Kennwert(10, tag, 2, 1)));
                        for (int h = 3; h < 24; h++)
                            zeilen.Add(new SpotStundenwert(10, tag, h, false, false, Kennwert(10, tag, h)));
                        continue;
                    }

                    bool sommerzeit = IstSommerzeit(monat, tag);
                    for (int h = 0; h < 24; h++)
                        zeilen.Add(new SpotStundenwert(monat, tag, h, sommerzeit, sommerzeit,
                                                       Kennwert(monat, tag, h)));
                }
            }

            return zeilen;
        }

        /// <summary>Voller Sommerzeittag: nach dem 31.03. und vor dem 27.10.</summary>
        private static bool IstSommerzeit(int monat, int tag)
        {
            if (monat < 3 || monat > 10) return false;
            if (monat == 3) return tag > UmstellungFruehjahrTag;
            if (monat == 10) return tag < UmstellungHerbstTag;
            return true;
        }

        // =================================================================
        // Gesamtbild
        // =================================================================

        [Fact]
        public void Testdatei_2024_Hat_8784_Zeilen()
        {
            Assert.Equal(SpotreihenAufbereitung.StundenSchaltjahr, Jahr2024().Count);
        }

        [Fact]
        public void Aufbereitung_Liefert_Genau_8760_Werte_Und_Ist_Vollstaendig()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());

            Assert.Equal(RasterAdapter.StundenJahr, e.StundenreiheCtKwh.Length);
            Assert.Equal(8784, e.ZeilenGelesen);
            Assert.Equal(24, e.ZeilenSchaltjahr);      // 29.02. = 24 Zeilen
            Assert.Equal(1, e.StundenGemittelt);       // Herbst-Doppelstunde
            Assert.Equal(1, e.StundenErgaenzt);        // Fruehjahrsluecke
            Assert.Equal(0, e.StundenOhneWert);
            Assert.Equal(0, e.StundenMehrfach);
            Assert.Equal(0, e.ZeilenUnbrauchbar);
            Assert.True(e.Vollstaendig);
        }

        /// <summary>
        /// Die Mengenbilanz muss aufgehen: 8.784 gelesen minus 24 Schalttagszeilen
        /// minus 1 gemittelte Doppelstunde ergibt 8.759 belegte Rasterstunden; die
        /// eine ergaenzte Fruehjahrsstunde fuellt den 8.760. Platz.
        /// </summary>
        [Fact]
        public void Mengenbilanz_Geht_Auf()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());

            int ausDatei = e.ZeilenGelesen - e.ZeilenSchaltjahr - e.StundenGemittelt;
            Assert.Equal(8759, ausDatei);
            Assert.Equal(RasterAdapter.StundenJahr, ausDatei + e.StundenErgaenzt);
        }

        // =================================================================
        // Umstellungstermine
        // =================================================================

        [Fact]
        public void Herbst_Doppelstunde_Wird_Gemittelt()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());

            double erwartet = (Kennwert(10, UmstellungHerbstTag, 2, 0) + Kennwert(10, UmstellungHerbstTag, 2, 1)) / 2.0;
            Assert.Equal(erwartet, e.StundenreiheCtKwh[Zielindex(10, UmstellungHerbstTag, 2)], 12);

            SpotBefund b = Befund(e, SpotBefundArt.DoppelstundeGemittelt);
            Assert.Equal(10, b.Monat);
            Assert.Equal(UmstellungHerbstTag, b.Tag);
            Assert.Equal(2, b.Stunde);
            Assert.Equal(2, b.Anzahl);
        }

        [Fact]
        public void Fruehjahrsluecke_Wird_Aus_Den_Nachbarn_Ergaenzt()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());

            double erwartet = (Kennwert(3, UmstellungFruehjahrTag, 1) + Kennwert(3, UmstellungFruehjahrTag, 3)) / 2.0;
            Assert.Equal(erwartet, e.StundenreiheCtKwh[Zielindex(3, UmstellungFruehjahrTag, 2)], 12);

            SpotBefund b = Befund(e, SpotBefundArt.FehlendeStundeErgaenzt);
            Assert.Equal(3, b.Monat);
            Assert.Equal(UmstellungFruehjahrTag, b.Tag);
            Assert.Equal(2, b.Stunde);
        }

        /// <summary>
        /// Die Umstellungen duerfen den Kalender NICHT verschieben: Der Wert einer
        /// Stunde steht nach der Aufbereitung an genau ihrer Kalenderstelle - auch
        /// zwischen den beiden Umstellungsterminen.
        /// </summary>
        [Fact]
        public void Kalenderausrichtung_Bleibt_Ueber_Beide_Umstellungen_Erhalten()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());

            (int monat, int tag, int stunde)[] proben =
            {
                (1, 1, 0), (2, 28, 23), (3, 1, 0), (3, 30, 12),
                (3, 31, 3), (4, 1, 0), (7, 15, 18), (10, 26, 12),
                (10, 27, 3), (10, 28, 0), (12, 31, 23)
            };

            foreach (var p in proben)
                Assert.Equal(Kennwert(p.monat, p.tag, p.stunde),
                             e.StundenreiheCtKwh[Zielindex(p.monat, p.tag, p.stunde)], 12);
        }

        /// <summary>
        /// Der 1. Maerz steht auf Index (31+28)*24 = 1416 - der Schalttag wurde
        /// ausgelassen, nicht der 1. Maerz nach vorn gezogen.
        /// </summary>
        [Fact]
        public void Schalttag_Wird_Ausgelassen_Ohne_Den_Maerz_Zu_Verschieben()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());

            Assert.Equal(1416, Zielindex(3, 1, 0));
            Assert.Equal(Kennwert(2, 28, 23), e.StundenreiheCtKwh[1415], 12);
            Assert.Equal(Kennwert(3, 1, 0), e.StundenreiheCtKwh[1416], 12);

            SpotBefund b = Befund(e, SpotBefundArt.SchaltjahrTagAusgelassen);
            Assert.Equal(2, b.Monat);
            Assert.Equal(29, b.Tag);
            Assert.Equal(24, b.Anzahl);
        }

        // =================================================================
        // Normaljahr, Reihenfolge, Fehlerfaelle
        // =================================================================

        [Fact]
        public void Normaljahr_Ohne_Umstellung_Laeuft_Ohne_Befund_Durch()
        {
            List<SpotStundenwert> zeilen = new List<SpotStundenwert>();
            for (int monat = 1; monat <= 12; monat++)
                for (int tag = 1; tag <= TageProMonatNormaljahr[monat - 1]; tag++)
                    for (int h = 0; h < 24; h++)
                        zeilen.Add(new SpotStundenwert(monat, tag, h, false, false, Kennwert(monat, tag, h)));

            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(zeilen);

            Assert.Equal(8760, e.ZeilenGelesen);
            Assert.Empty(e.Befunde);
            Assert.True(e.Vollstaendig);
        }

        /// <summary>Eine unsortierte Datei ergibt dieselbe Reihe - einsortiert wird nach Kalenderstelle.</summary>
        [Fact]
        public void Reihenfolge_Der_Zeilen_Ist_Ohne_Bedeutung()
        {
            List<SpotStundenwert> vorwaerts = Jahr2024();
            List<SpotStundenwert> rueckwaerts = new List<SpotStundenwert>(vorwaerts);
            rueckwaerts.Reverse();

            double[] a = SpotreihenAufbereitung.AusStundenwerten(vorwaerts).StundenreiheCtKwh;
            double[] b = SpotreihenAufbereitung.AusStundenwerten(rueckwaerts).StundenreiheCtKwh;

            Assert.Equal(a, b);
        }

        /// <summary>
        /// Eine echte Luecke (kein Umstellungstermin) wird als solche gezaehlt und
        /// macht den Import unvollstaendig - aber sie bricht ihn nicht ab.
        /// </summary>
        [Fact]
        public void Echte_Luecke_Macht_Den_Import_Unvollstaendig()
        {
            List<SpotStundenwert> zeilen = new List<SpotStundenwert>();
            for (int monat = 1; monat <= 12; monat++)
                for (int tag = 1; tag <= TageProMonatNormaljahr[monat - 1]; tag++)
                    for (int h = 0; h < 24; h++)
                    {
                        if (monat == 6 && tag == 15 && h == 10) continue;   // Luecke
                        zeilen.Add(new SpotStundenwert(monat, tag, h, false, false, Kennwert(monat, tag, h)));
                    }

            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(zeilen);

            Assert.Equal(1, e.StundenOhneWert);
            Assert.Equal(0, e.StundenErgaenzt);
            Assert.False(e.Vollstaendig);

            // Ergaenzt wird sie trotzdem - die Reihe bleibt 8.760 lang und rechenbar.
            double erwartet = (Kennwert(6, 15, 9) + Kennwert(6, 15, 11)) / 2.0;
            Assert.Equal(erwartet, e.StundenreiheCtKwh[Zielindex(6, 15, 10)], 12);
        }

        /// <summary>
        /// Zwei gleiche Eintraege OHNE Zeitzonenwechsel sind ein Datenfehler und
        /// werden getrennt von der Herbst-Doppelstunde gezaehlt.
        /// </summary>
        [Fact]
        public void Doppelter_Eintrag_Ohne_Zeitzonenwechsel_Ist_Ein_Datenfehler()
        {
            List<SpotStundenwert> zeilen = new List<SpotStundenwert>
            {
                new SpotStundenwert(1, 1, 0, false, false, 10.0),
                new SpotStundenwert(1, 1, 0, false, false, 20.0)
            };

            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(zeilen);

            Assert.Equal(0, e.StundenGemittelt);
            Assert.Equal(1, e.StundenMehrfach);
            Assert.Equal(15.0, e.StundenreiheCtKwh[0], 12);
            Assert.Equal(SpotBefundArt.MehrfachEintrag, Befund(e, SpotBefundArt.MehrfachEintrag).Art);
        }

        [Fact]
        public void Unbrauchbare_Zeilen_Werden_Verworfen_Und_Gezaehlt()
        {
            List<SpotStundenwert> zeilen = new List<SpotStundenwert>
            {
                new SpotStundenwert(0, 1, 0, false, false, 1.0),    // Monat 0
                new SpotStundenwert(13, 1, 0, false, false, 1.0),   // Monat 13
                new SpotStundenwert(1, 0, 0, false, false, 1.0),    // Tag 0
                new SpotStundenwert(4, 31, 0, false, false, 1.0),   // April hat 30 Tage
                new SpotStundenwert(1, 1, 24, false, false, 1.0),   // Stunde 24
                new SpotStundenwert(1, 1, 0, false, false, 7.0)     // gueltig
            };

            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(zeilen);

            Assert.Equal(6, e.ZeilenGelesen);
            Assert.Equal(5, e.ZeilenUnbrauchbar);
            Assert.Equal(7.0, e.StundenreiheCtKwh[0], 12);
            Assert.False(e.Vollstaendig);
        }

        /// <summary>Negative Preise sind eine Information, kein Fehler (Fachkonzept 4.1).</summary>
        [Fact]
        public void Negative_Preise_Werden_Gezaehlt_Nicht_Beanstandet()
        {
            List<SpotStundenwert> zeilen = new List<SpotStundenwert>();
            for (int monat = 1; monat <= 12; monat++)
                for (int tag = 1; tag <= TageProMonatNormaljahr[monat - 1]; tag++)
                    for (int h = 0; h < 24; h++)
                        zeilen.Add(new SpotStundenwert(monat, tag, h, false, false, h == 13 ? -2.5 : 8.0));

            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(zeilen);

            Assert.Equal(365, e.NegativeWerte);
            Assert.Equal(-2.5, e.MinCtKwh, 12);
            Assert.Equal(8.0, e.MaxCtKwh, 12);
            Assert.True(e.Vollstaendig);
        }

        [Fact]
        public void Null_Eingabe_Wird_Zurueckgewiesen()
        {
            Assert.Throws<ArgumentNullException>(() => SpotreihenAufbereitung.AusStundenwerten(null!));
        }

        // =================================================================
        // Kette bis zur Engine
        // =================================================================

        /// <summary>
        /// Die aufbereitete Reihe passt ohne weitere Umformung in einen
        /// <see cref="SpeicherEingang"/> - 35.040 Viertelstunden nach der Expansion.
        /// </summary>
        [Fact]
        public void Aufbereitete_Reihe_Passt_In_Den_Engine_Eingang()
        {
            SpotreihenErgebnis e = SpotreihenAufbereitung.AusStundenwerten(Jahr2024());
            double[] viertel = PreisModell.ZuViertelstunden(e.StundenreiheCtKwh);

            Assert.Equal(RasterAdapter.ViertelstundenJahr, viertel.Length);

            SpeicherEingang eingang = new SpeicherEingang(
                new double[RasterAdapter.ViertelstundenJahr],
                new double[RasterAdapter.ViertelstundenJahr],
                viertel);

            Assert.Equal(RasterAdapter.ViertelstundenJahr, eingang.Anzahl);
            Assert.Equal(e.StundenreiheCtKwh[0], eingang.PreisCtKwh[0], 12);
            Assert.Equal(e.StundenreiheCtKwh[0], eingang.PreisCtKwh[3], 12);
        }

        private static SpotBefund Befund(SpotreihenErgebnis e, SpotBefundArt art)
        {
            foreach (SpotBefund b in e.Befunde) if (b.Art == art) return b;
            throw new InvalidOperationException("Befund " + art + " nicht gefunden.");
        }
    }
}
