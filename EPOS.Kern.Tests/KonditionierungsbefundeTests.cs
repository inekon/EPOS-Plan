using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.KonditionierungsarbeitTests;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Befunde B4–B8, F2 (a) und F5 (a) in den reinen Schritten</b> (Stufe KP2, Welle K2,
    /// Teilschritt 2; Entwurf KP2 Abschnitt 1 und Festlegung 5, Entscheid E56). Jede Probe war am
    /// Stand von Teilschritt 1 rot.
    ///
    /// <list type="bullet">
    /// <item><b>B4</b> — Anlegen am Gebäude legt die Kalender der Zonen mit eigenen Werten mit an; „Anlegen
    /// ändert keine Reihe".</item>
    /// <item><b>B5</b> — P1 beim Übergang der Personenspalte: Vorschlag Bewohner × 70 W, Geräte minus
    /// Jahresmittel, zurück plus Jahresmittel.</item>
    /// <item><b>B6</b> — Heiz-Nachtzeiten am Gebäude nur in den Bestandsspalten; an der Zone macht die
    /// eigene Nachtzeile die Heizspalte wirksam.</item>
    /// <item><b>B7</b> — Merker nach der Regel des Dialogs.</item>
    /// <item><b>B8</b> — Bemerkung = Herkunft · letzter Werkzeugvermerk.</item>
    /// <item><b>F2 (a)</b> — ein angelegter, nicht von Hand geänderter Kalender folgt der Matrix.</item>
    /// <item><b>F5 (a)</b> — Rückfrage „aufteilen" über der Gesamtangabe, die Summe bleibt.</item>
    /// </list>
    /// </summary>
    public class KonditionierungsbefundeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt die Kultur zurück.</summary>
        public void Dispose() => _kultur.Dispose();

        private const Konditionierungsgroesse HEIZ = Konditionierungsgroesse.Heizsoll;

        private static double[] Reihe(Konditionierungskalender k, Konditionierungsarbeitsstand a)
            => k.Auswerten(a.W0, a.Referenzjahr);

        private static Konditionierungszone Unbeheizt(long id, string name)
        {
            var b = new Matrixeingang { SollTag = 12.0 };
            return new Konditionierungszone(id, name, null, false, Konditionierungsstand.Leer(Kalendereigentuemer.Zone, b));
        }

        private static Konditionierungsarbeitsstand MitGesamtangabe(double rate)
        {
            Matrixeingang b = Bestand();
            b.LuftwechselInfiltration = null;
            b.LuftwechselNutzer = null;
            b.Luftwechselrate = rate;
            Konditionierungsarbeit.HerkunftDesLuftwechsels(b);
            return new Konditionierungsarbeitsstand(Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, b), null, 201.0, 2025);
        }

        // =============================================================================
        //  B4 — F2 Regel 2: Anlegen am Gebäude legt die Zonenkalender mit an
        // =============================================================================

        [Fact]
        public void B4_Anlegen_am_Gebaeude_legt_den_Kalender_der_Zone_mit_eigenem_Sollwert_mit_an()
        {
            Konditionierungsarbeitsstand a = Stand(Zone(-1, "Anbau", b => b.SollTag = 22.0), Zone(-2, "Halle"));
            double[] anbau = Reihe(a.Ansichtskalender(HEIZ, -1), a);
            double[] halle = Reihe(a.Ansichtskalender(HEIZ, -2), a);

            Konditionierungsschritt s = Konditionierungsarbeit.Anlegen(a, Ort(HEIZ));
            Konditionierungsarbeitsstand b = Gut(s);

            Assert.NotNull(b.Zone(-1).Stand.Kalender(HEIZ));
            Assert.Null(b.Zone(-2).Stand.Kalender(HEIZ));          // die Halle erbt den des Gebäudes
            Assert.Equal(anbau, Reihe(b.GeltenderKalender(HEIZ, -1), b));   // Anlegen ändert keine Reihe
            Assert.Equal(halle, Reihe(b.GeltenderKalender(HEIZ, -2), b));
            Assert.Equal(22.0, b.GeltenderKalender(HEIZ, -1).Standardwoche[Kalenderwoche.Stelle(0, 12)]);
            Assert.Equal(new[] { "Anbau" }, s.Bilanz.Zonen);
            Assert.Equal(1, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Zonenkalender));
        }

        [Fact]
        public void B4_Die_Geraete_einer_Zone_behalten_ihren_Flaechenanteil()
        {
            var z = new Konditionierungszone(-1, "Anbau", 67.0, true, Konditionierungsstand.Leer(Kalendereigentuemer.Zone, new Matrixeingang()));
            Konditionierungsarbeitsstand a = Stand(z);
            a = Gut(Setzen(a, Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_NACHT, 0.5));
            Konditionierungskalender vorher = a.Ansichtskalender(Konditionierungsgroesse.Geraete, -1);

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Geraete)));
            Konditionierungskalender k = b.GeltenderKalender(Konditionierungsgroesse.Geraete, -1);
            Assert.Same(k, b.Zone(-1).Stand.Kalender(Konditionierungsgroesse.Geraete));
            Assert.Equal(Reihe(vorher, a), Reihe(k, b));
            Assert.Equal(vorher.Nennwert, k.Nennwert);
            Assert.True(k.Nennwert < b.Gebaeude.Kalender(Konditionierungsgroesse.Geraete).Nennwert);
        }

        private static Konditionierungsschritt Setzen(Konditionierungsarbeitsstand a, Konditionierungsgroesse g,
                                                                        string zeile, double wert, long? zone = null)
            => Konditionierungsarbeit.ZelleSetzen(a, Ort(g, zone), zeile, Matrixzelle.AusWert(wert));

        [Fact]
        public void B4_Eine_unbeheizte_Zone_traegt_weder_Heiz_noch_Kuehlzellen_und_die_Zone_kuehlt_wie_das_Gebaeude()
        {
            Konditionierungsarbeitsstand a = Stand(Unbeheizt(-3, "Lager"), Zone(-1, "Anbau"));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Anlegen(a, Ort(HEIZ)));
            Assert.Null(b.Zone(-3).Stand.Kalender(HEIZ));

            Konditionierungsschritt heiz = Setzen(a, HEIZ, DbWerte.KOND_ZEILE_TAG, 18.0, -3);
            Assert.False(heiz.Ok);
            Assert.Contains("Lager", heiz.Meldung);
            Assert.False(Konditionierungsarbeit.Anlegen(a, Ort(HEIZ, -3)).Ok);

            Konditionierungsschritt kuehl = Setzen(a, Konditionierungsgroesse.Kuehlsoll,
                                                                        DbWerte.KOND_ZEILE_TAG, 26.0, -1);
            Assert.False(kuehl.Ok);
            Assert.Contains("Anbau", kuehl.Meldung);

            // Lüftung, Geräte und Personen trägt auch die unbeheizte Zone.
            Gut(Setzen(a, Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_TAG, 0.5, -3));
        }

        // =============================================================================
        //  B5 — P1 beim Übergang der Personenspalte
        // =============================================================================

        [Fact]
        public void B5_Ein_Personenanteil_schlaegt_den_Nennwert_vor_und_senkt_die_Geraete_energieerhaltend()
        {
            Konditionierungsarbeitsstand a = Stand();          // Bewohner 5, Interne_Waermegewinne 462 W
            Assert.Null(a.GeltenderKalender(Konditionierungsgroesse.Personen, null));

            Konditionierungsarbeitsstand b = Gut(Setzen(a, Konditionierungsgroesse.Personen,
                                                                             DbWerte.KOND_ZEILE_TAG, 0.5));
            Assert.Equal(350.0, b.Gebaeude.Vorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_NENNWERT).Wert);
            Konditionierungskalender p = b.GeltenderKalender(Konditionierungsgroesse.Personen, null);
            Assert.NotNull(p);
            double mittel = Konditionierungsarbeit.PersonenJahresmittelW(p, b.W0, b.Referenzjahr);
            Assert.True(mittel > 0.0);
            Assert.Equal(462.0, b.Gebaeude.Bestand.InterneWaermegewinne.Value + mittel, 3);

            // Ein zweiter Anteil ändert nichts mehr an den Geräten — P1 gilt beim Übergang.
            Konditionierungsarbeitsstand c = Gut(Setzen(b, Konditionierungsgroesse.Personen,
                                                                             DbWerte.KOND_ZEILE_NACHT, 0.1));
            Assert.Equal(b.Gebaeude.Bestand.InterneWaermegewinne, c.Gebaeude.Bestand.InterneWaermegewinne);

            // Zurück: ohne Nennwert gilt kein Personenkalender, die Geräte bekommen das Jahresmittel wieder.
            double mittelC = Konditionierungsarbeit.PersonenJahresmittelW(
                c.GeltenderKalender(Konditionierungsgroesse.Personen, null), c.W0, c.Referenzjahr);
            Konditionierungsarbeitsstand d = Gut(Konditionierungsarbeit.ZelleSetzen(
                c, Ort(Konditionierungsgroesse.Personen), DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.Leer));
            Assert.Null(d.GeltenderKalender(Konditionierungsgroesse.Personen, null));
            Assert.Equal(c.Gebaeude.Bestand.InterneWaermegewinne.Value + mittelC,
                         d.Gebaeude.Bestand.InterneWaermegewinne.Value, 3);
        }

        [Fact]
        public void B5_Ein_angelegter_Personenkalender_bringt_Vorschlag_und_Energieerhalt_mit()
        {
            // Ein Anteil ohne Nennwert (etwa aus einem älteren Stand) ist noch keine wirksame Personenspalte.
            Konditionierungsarbeitsstand a = Stand();
            a = a.MitGebaeude(a.Gebaeude.MitVorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5)));
            Assert.Null(a.GeltenderKalender(Konditionierungsgroesse.Personen, null));

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Personen)));
            Konditionierungskalender p = b.Gebaeude.Kalender(Konditionierungsgroesse.Personen);
            Assert.Equal(350.0, p.Nennwert);
            double mittel = Konditionierungsarbeit.PersonenJahresmittelW(p, b.W0, b.Referenzjahr);
            Assert.Equal(462.0, b.Gebaeude.Bestand.InterneWaermegewinne.Value + mittel, 3);
        }

        // =============================================================================
        //  B6 — die Heiz-Nachtzeit an einem Ort (Festlegung 5)
        // =============================================================================

        [Fact]
        public void B6_Heiz_Nachtzeiten_stehen_am_Gebaeude_nur_in_den_Bestandsspalten()
        {
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(), Ort(HEIZ), DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(17.0, 21, 5)));
            Matrixeingang bestand = b.Gebaeude.Bestand;
            Assert.Equal(17.0, bestand.SollNacht);
            Assert.Equal(21, bestand.NachtBeginn);
            Assert.Equal(5, bestand.NachtEnde);
            Assert.Empty(b.Gebaeude.Vorgabezeilen());
            Assert.Equal(21, b.Matrix(null).Heizsoll.Nacht.Von);
            Assert.Equal(5, b.Matrix(null).Heizsoll.Nacht.Bis);

            // Leere Zeiten lassen die Nachtzeit stehen (eine Vorlage ohne Zeiten nimmt sie nicht).
            Konditionierungsarbeitsstand c = Gut(Setzen(b, HEIZ, DbWerte.KOND_ZEILE_NACHT, 16.0));
            Assert.Equal(21, c.Gebaeude.Bestand.NachtBeginn);
            Assert.Equal(5, c.Gebaeude.Bestand.NachtEnde);

            // Die Vorlage „Büro" (Nacht 18–7) setzt die Nachtzeit des Gebäudes.
            Konditionierungsarbeitsstand d = Gut(Konditionierungsarbeit.VorlageUebernehmen(Stand(), Ort(HEIZ), Buero()));
            Assert.Equal(18, d.Gebaeude.Bestand.NachtBeginn);
            Assert.Equal(7, d.Gebaeude.Bestand.NachtEnde);
            Assert.Null(d.Gebaeude.Vorgabe(HEIZ, DbWerte.KOND_ZEILE_NACHT).Von);
        }

        [Fact]
        public void B6_Eine_eigene_Heiz_Nachtzeile_der_Zone_macht_die_Heizspalte_wirksam()
        {
            Konditionierungsarbeitsstand a = Stand(Zone(-1, "Anbau"));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(HEIZ, -1), DbWerte.KOND_ZEILE_NACHT, Matrixzelle.NurZeiten(20, 7)));
            Assert.Equal(20, b.Zone(-1).Stand.Vorgabe(HEIZ, DbWerte.KOND_ZEILE_NACHT).Von);

            // Matrix und Lauf zeigen dieselbe Nachtzeit: um 20 Uhr gilt der Nachtwert, um 19 Uhr der Tagwert.
            Konditionierungskalender k = b.GeltenderKalender(HEIZ, -1);
            Assert.NotNull(k);
            Assert.Equal(18.0, k.Standardwoche[Kalenderwoche.Stelle(0, 20)]);
            Assert.Equal(20.0, k.Standardwoche[Kalenderwoche.Stelle(0, 19)]);
            Konditionierungssatz satz = Konditionierungdatenweg.Satz(b, -1, Vdi6007Probe.Wochenende(), 2025, false, false);
            Assert.NotNull(satz);
            Assert.Equal(Reihe(k, b), satz.Reihe(HEIZ));

            // Das Gebäude selbst bleibt im Bestandszweig.
            Assert.Null(b.GeltenderKalender(HEIZ, null));
            Assert.Null(Konditionierungdatenweg.Satz(b, null, Vdi6007Probe.Wochenende(), 2025, false, false));
        }

        // =============================================================================
        //  B7 — Merker nach der Regel des Dialogs
        // =============================================================================

        [Fact]
        public void B7_Heizwerte_Ferien_und_Wochenende_setzen_den_Merker_nach_der_Regel_des_Dialogs()
        {
            Matrixeingang bestand = Bestand();
            bestand.Ferienmerker = 0.0;
            bestand.Wochenendmerker = 0.0;
            var a = new Konditionierungsarbeitsstand(Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, bestand), null, 201.0, 2025);

            Konditionierungsarbeitsstand b = Gut(Setzen(a, HEIZ, DbWerte.KOND_ZEILE_FERIEN, 15.0));
            Assert.Equal(1.0, b.Gebaeude.Bestand.Ferienmerker);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(b.Matrix(null), HEIZ, rundlaufPruefen: true);
            Assert.Contains(l.Kalender.Perioden, p => p.Art == DbWerte.KOND_ART_FERIEN);
            Konditionierungsarbeitsstand c = Gut(Setzen(b, HEIZ, DbWerte.KOND_ZEILE_FERIEN, 0.5));
            Assert.Equal(0.0, c.Gebaeude.Bestand.Ferienmerker);

            Konditionierungsarbeitsstand d = Gut(Setzen(a, HEIZ, DbWerte.KOND_ZEILE_WOCHENENDE, 17.0));
            Assert.Equal(1.0, d.Gebaeude.Bestand.Wochenendmerker);
            Konditionierungsarbeitsstand e = Gut(Setzen(d, HEIZ, DbWerte.KOND_ZEILE_WOCHENENDE, 4.0));
            Assert.Equal(0.0, e.Gebaeude.Bestand.Wochenendmerker);
        }

        // =============================================================================
        //  B8 — Herkunft · letzter Werkzeugvermerk
        // =============================================================================

        [Fact]
        public void B8_Ein_Werkzeugvermerk_laesst_die_Herkunft_der_Vorlage_stehen()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.VorlageUebernehmen(Stand(), Ort(HEIZ), Buero()));
            Assert.Equal("Büro", a.Gebaeude.Herkunft(HEIZ).Vorlage);

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Zeitfenster(a, Ort(HEIZ), new[] { 0, 1, 2, 3, 4 }, 6, 8, 22.0));
            Kalenderherkunft h = b.Gebaeude.Herkunft(HEIZ);
            Assert.Equal("Büro", h.Vorlage);
            Assert.False(string.IsNullOrEmpty(h.Vermerk));
            Assert.StartsWith("aus Vorlage Büro" + Kalenderherkunft.TRENNER, h.Bemerkung());
            Assert.Equal(h, Kalenderherkunft.AusBemerkung(h.Bemerkung()));

            // „Matrix erneut anwenden": die Vorlage bleibt, der Vermerk beschreibt die Woche nicht mehr.
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.MatrixErneut(b, Ort(HEIZ)));
            Assert.Equal("Büro", c.Gebaeude.Herkunft(HEIZ).Vorlage);
            Assert.Null(c.Gebaeude.Herkunft(HEIZ).Vermerk);
        }

        // =============================================================================
        //  E56 F2 (a) — der angelegte Kalender folgt der Matrix
        // =============================================================================

        [Fact]
        public void F2_Ein_angelegter_unveraenderter_Kalender_folgt_der_Matrix_und_behaelt_eigene_Perioden()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.Anlegen(Stand(), Ort(HEIZ)));
            a = Gut(Konditionierungsarbeit.Feiertage(a, Ort(HEIZ), 7));
            int feiertage = a.Gebaeude.Kalender(HEIZ).Perioden.Count(p => p.IstFeiertag);
            Assert.True(feiertage > 0);

            Konditionierungsarbeitsstand b = Gut(Setzen(a, HEIZ, DbWerte.KOND_ZEILE_TAG, 21.0));
            Konditionierungskalender k = b.Gebaeude.Kalender(HEIZ);
            Assert.Equal(21.0, k.Standardwoche[Kalenderwoche.Stelle(0, 12)]);
            Assert.Equal(feiertage, k.Perioden.Count(p => p.IstFeiertag));
            Assert.True(Kalendervergleich.MatrixbereichGleich(
                k, Standardfahrplan.Erzeugen(b.Matrix(null), HEIZ, rundlaufPruefen: true).Kalender));
        }

        [Fact]
        public void F2_Nach_einer_Handaenderung_folgt_der_Kalender_erst_nach_Matrix_erneut_anwenden()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.Anlegen(Stand(), Ort(HEIZ)));
            a = Gut(Konditionierungsarbeit.Zeitfenster(a, Ort(HEIZ), new[] { 0 }, 6, 8, 22.0));
            Konditionierungskalender vor = a.Gebaeude.Kalender(HEIZ);

            Konditionierungsarbeitsstand b = Gut(Setzen(a, HEIZ, DbWerte.KOND_ZEILE_TAG, 21.0));
            Assert.True(Kalendervergleich.KalenderGleich(vor, b.Gebaeude.Kalender(HEIZ)));

            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.MatrixErneut(b, Ort(HEIZ)));
            Assert.Equal(21.0, c.Gebaeude.Kalender(HEIZ).Standardwoche[Kalenderwoche.Stelle(1, 12)]);
        }

        [Fact]
        public void F2_Der_mit_angelegte_Zonenkalender_folgt_dem_Gebaeude_und_behaelt_den_Wert_der_Zone()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.Anlegen(
                Stand(Zone(-1, "Anbau", b => b.SollTag = 22.0)), Ort(HEIZ)));
            Konditionierungsarbeitsstand b = Gut(Setzen(a, HEIZ, DbWerte.KOND_ZEILE_NACHT, 15.0));
            Konditionierungskalender z = b.Zone(-1).Stand.Kalender(HEIZ);
            Assert.NotNull(z);
            Assert.Equal(15.0, z.Standardwoche[Kalenderwoche.Stelle(0, 2)]);     // die Nacht des Gebäudes
            Assert.Equal(22.0, z.Standardwoche[Kalenderwoche.Stelle(0, 12)]);    // der Tag der Zone
        }

        // =============================================================================
        //  E56 F5 (a) / B3 — „aufteilen", die Summe bleibt
        // =============================================================================

        [Fact]
        public void F5_Eine_Lueftungsvorgabe_ueber_der_Gesamtangabe_fragt_nach_dem_Aufteilen()
        {
            Konditionierungsarbeitsstand a = MitGesamtangabe(0.7);
            Konditionierungsschritt s = Setzen(a, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 1.5);
            Assert.False(s.Ok);
            Assert.True(s.Rueckfrage);
            Assert.Null(s.Stand);
            Assert.Equal(1, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Luftwechsel));

            // Auch eine Zone, die die Gesamtangabe des Gebäudes erbt, fragt.
            Konditionierungsarbeitsstand z = new Konditionierungsarbeitsstand(a.Gebaeude, new[] { Zone(-1, "Anbau") }, 201.0, 2025);
            Assert.True(Setzen(z, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_WOCHENENDE, 0.2, -1).Rueckfrage);
        }

        [Theory]
        [InlineData(0.7, 0.3, 0.4)]
        [InlineData(0.2, 0.2, 0.0)]
        [InlineData(1.15, 0.3, 0.85)]
        public void F5_Aufteilen_haelt_die_Summe_und_die_Vorgabe_greift_danach(double rate, double infiltration, double nutzer)
        {
            Konditionierungsarbeitsstand a = MitGesamtangabe(rate);
            double vorher = Gebaeudemodellvorgaben.WirksamerLuftwechsel(rate, null, null);

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.LuftwechselAufteilen(a));
            Matrixeingang bestand = b.Gebaeude.Bestand;
            Assert.Equal(infiltration, bestand.LuftwechselInfiltration.Value, 12);
            Assert.Equal(nutzer, bestand.LuftwechselNutzer.Value, 12);
            Assert.False(bestand.LuftwechselAusGesamtangabe);
            Assert.Equal(vorher, Gebaeudemodellvorgaben.WirksamerLuftwechsel(
                bestand.Luftwechselrate, bestand.LuftwechselInfiltration, bestand.LuftwechselNutzer), 12);

            Konditionierungsarbeitsstand c = Gut(Setzen(b, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 1.5));
            Gut(Konditionierungsarbeit.Anlegen(c, Ort(Konditionierungsgroesse.Lueftung)));
        }

        [Fact]
        public void F15_Am_Datenweg_bleibt_die_Ablehnung_ohne_Trennung()
        {
            Konditionierungsarbeitsstand a = MitGesamtangabe(0.7);
            Konditionierungsarbeitsstand b = a.MitGebaeude(a.Gebaeude.MitVorgabe(
                Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(1.5)));
            GebaeudeModellException ex = Assert.Throws<GebaeudeModellException>(
                () => Konditionierungdatenweg.Satz(b, null, Vdi6007Probe.Wochenende(), 2025, false, false, "Probe"));
            Assert.Contains(nameof(Fahrplanbefund.LuftwechselOhneTrennung), ex.Message);
        }
    }
}
