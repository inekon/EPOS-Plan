using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Kopieren nach …" ohne Datenbank</b> (Teilkonzept Konditionierungsprofile 3.5, 7.4): die reine Regel
    /// <see cref="Vorlagenkopierregel"/> — welche Richtungen es gibt, Geräte ↔ Personen unverändert, Heizen →
    /// Kühlen mit Zeitstruktur, Aus-Zeiten, Komfortsollwert in Höhe des Tagwerts und Absenksollwert bzw. „aus" in
    /// den Absenkzeiten, nichts außerhalb der Zielgröße und nichts nach E54 — und dieselbe Regel in der Ablage
    /// ohne Datenbank (<see cref="Konditionierungsvorlagenablage"/>).
    /// </summary>
    /// <remarks>Die Kultur ist auf de-DE gepinnt: Zwei Fälle lesen den Satz der Herkunft.</remarks>
    public sealed class VorlagenkopierregelTests : IDisposable
    {
        private const Konditionierungsgroesse H = Konditionierungsgroesse.Heizsoll;
        private const Konditionierungsgroesse K = Konditionierungsgroesse.Kuehlsoll;
        private const Konditionierungsgroesse L = Konditionierungsgroesse.Lueftung;
        private const Konditionierungsgroesse G = Konditionierungsgroesse.Geraete;
        private const Konditionierungsgroesse P = Konditionierungsgroesse.Personen;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Vorrichtung
        // =============================================================================

        /// <summary>Eine Woche aus einer Regel je Tag (0 = Montag) und Stunde; NaN = „aus".</summary>
        internal static double[] Woche(Func<int, int, double> wert)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < Kalenderwoche.TAGESSTUNDEN; s++)
                    w[Kalenderwoche.Stelle(t, s)] = wert(t, s);
            return w;
        }

        /// <summary>
        /// Eine Heizvorlage mit allem, was eine Zeitstruktur trägt: Tag, Nacht mit Zeiten, Wochenende „aus",
        /// Ferien, eine Standardwoche mit Aus-Stunden, Perioden mit Wert, „aus", eigener Woche und „wie Sonntag".
        /// Ohne <paramref name="rein"/> dazu, was eine Vorlage nach E54 nicht trägt (Saisonzeile, Ferien- und
        /// Saisonperiode), und eine Zelle einer fremden Größe.
        /// </summary>
        internal static Konditionierungsstand Heizvorlage(bool rein = false)
        {
            double[] woche = Woche((t, s) => t >= 5 ? double.NaN : s >= 7 && s < 18 ? 21.0 : 17.0);
            var perioden = new List<Kalenderregel>
            {
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr", DbWerte.KOND_FEIERTAGE[0],
                                       Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Betriebsurlaub", 220, 234,
                                       Kalenderangabe.Abgeschaltet),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 1, DbWerte.KOND_ART_ZEITRAUM, "Inventur", 2, 3,
                                       Kalenderangabe.AusWert(15.0)),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 2, DbWerte.KOND_ART_ZEITRAUM, "Messe", 100, 104,
                                       Kalenderangabe.AusWoche(Woche((t, s) => s < 12 ? 22.0 : double.NaN))),
            };
            if (!rein)
            {
                perioden.Add(Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN, "Ferien 1", 180, 200,
                                                    Kalenderangabe.AusWert(16.0)));
                perioden.Add(Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON, DbWerte.KOND_ART_BETRIEBSPAUSE, "Saison", 121, 273,
                                                    Kalenderangabe.Abgeschaltet));
            }
            Konditionierungsstand v = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(H, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(21.0))
                .MitVorgabe(H, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(17.0, 18, 7))
                .MitVorgabe(H, DbWerte.KOND_ZEILE_WOCHENENDE, Matrixzelle.Abgeschaltet())
                .MitVorgabe(H, DbWerte.KOND_ZEILE_FERIEN, Matrixzelle.AusWert(16.0))
                .MitKalender(H, new Konditionierungskalender(H, Kalenderangabe.AusWoche(woche), null, perioden),
                             new Kalenderherkunft("Büro", "Zeitfenster Mo–Fr 7–18 Uhr = 21"));
            return rein
                ? v
                : v.MitVorgabe(H, DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120))
                   .MitVorgabe(L, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5));
        }

        /// <summary>Eine Gerätevorlage mit Woche, Perioden, Feiertagsregeln, Nachtzeiten, Herkunft und Vermerk.</summary>
        internal static Konditionierungsstand Geraetevorlage()
        {
            double[] woche = Woche((t, s) => t >= 5 ? 0.1 : s >= 8 && s < 17 ? 1.0 : 0.25);
            var perioden = new List<Kalenderregel>();
            for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                perioden.Add(Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG + k, "Feiertag " + k,
                                                    DbWerte.KOND_FEIERTAGE[k], Kalenderangabe.AlsWochentag(7)));
            perioden.Add(Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Revision", 360, 5,
                                                Kalenderangabe.AusWert(0.05)));
            perioden.Add(Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 1, DbWerte.KOND_ART_ZEITRAUM, "Umbau", 40, 41,
                                                Kalenderangabe.Abgeschaltet));
            return Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(G, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(1.0))
                .MitVorgabe(G, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(0.1, 18, 7))
                .MitVorgabe(G, DbWerte.KOND_ZEILE_WOCHENENDE, Matrixzelle.AusWert(0.1))
                .MitVorgabe(G, DbWerte.KOND_ZEILE_FERIEN, Matrixzelle.Abgeschaltet())
                .MitKalender(G, new Konditionierungskalender(G, Kalenderangabe.AusWoche(woche), null, perioden),
                             new Kalenderherkunft("Büro", "Zeitfenster Mo–Fr 8–17 Uhr = 1"));
        }

        /// <summary>Trägt die Ebene in einer anderen Größe als <paramref name="groesse"/> irgendetwas?</summary>
        private static bool TraegtAusserhalb(Konditionierungsstand s, Konditionierungsgroesse groesse)
            => KonditionierungsvorlageCtrl.Groessenregel(s, groesse) != null;

        // =============================================================================
        //  Die Richtungen
        // =============================================================================

        [Fact]
        public void Erlaubt_sind_nur_Heizen_nach_Kuehlen_und_Geraete_mit_Personen()
        {
            Assert.Equal(new[] { K }, Vorlagenkopierregel.Ziele(H));
            Assert.Empty(Vorlagenkopierregel.Ziele(K));
            Assert.Empty(Vorlagenkopierregel.Ziele(L));
            Assert.Equal(new[] { P }, Vorlagenkopierregel.Ziele(G));
            Assert.Equal(new[] { G }, Vorlagenkopierregel.Ziele(P));

            foreach (Konditionierungsgroesse von in Konditionierungsgroessen.Alle)
                foreach (Konditionierungsgroesse nach in Konditionierungsgroessen.Alle)
                {
                    Vorlagenkopierweg weg = Vorlagenkopierregel.Weg(von, nach);
                    Vorlagenkopierweg soll = (von, nach) switch
                    {
                        (H, K) => Vorlagenkopierweg.Zeitstruktur,
                        (G, P) or (P, G) => Vorlagenkopierweg.Direkt,
                        _ => Vorlagenkopierweg.Keiner,
                    };
                    Assert.Equal(soll, weg);
                    Assert.Equal(soll == Vorlagenkopierweg.Keiner, Vorlagenkopierregel.Richtungspruefung(von, nach) != null);
                    Assert.Equal(soll == Vorlagenkopierweg.Zeitstruktur, Vorlagenkopierregel.MitKomfortsollwert(von, nach));
                }
        }

        [Fact]
        public void Eine_andere_Richtung_wird_benannt_abgelehnt_statt_umgerechnet()
        {
            Konditionierungsstand kuehlen = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(K, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(26.0));
            Ebenenergebnis zurueck = Vorlagenkopierregel.Umsetzen(kuehlen, K, H, 20.0, 28.0);
            Assert.False(zurueck.Ok);
            Assert.Null(zurueck.Stand);
            Assert.Contains("Kühlen", zurueck.Meldung);
            Assert.Contains("Heizen", zurueck.Meldung);

            Assert.False(Vorlagenkopierregel.Umsetzen(Heizvorlage(), H, L, 26.0, 28.0).Ok);
            Assert.False(Vorlagenkopierregel.Umsetzen(Heizvorlage(), H, H, 26.0, 28.0).Ok);
            Assert.False(Vorlagenkopierregel.Umsetzen(Geraetevorlage(), G, G, null, null).Ok);
            Assert.False(Vorlagenkopierregel.Umsetzen(Geraetevorlage(), L, P, null, null).Ok);
        }

        // =============================================================================
        //  Geräte ↔ Personen: unverändert
        // =============================================================================

        [Fact]
        public void Geraete_nach_Personen_und_zurueck_tragen_Werte_und_Zeitstruktur_bitgleich()
        {
            Konditionierungsstand quelle = Geraetevorlage();
            Ebenenergebnis personen = Vorlagenkopierregel.Umsetzen(quelle, G, P, null, null);
            Assert.True(personen.Ok, personen.Meldung);
            Assert.Equal(Kalendereigentuemer.Vorlage, personen.Stand.Art);
            Assert.False(TraegtAusserhalb(personen.Stand, P));

            foreach (string zeile in DbWerte.KOND_ZEILEN)
                Assert.True(Kalendervergleich.ZelleGleich(quelle.Vorgabe(G, zeile), personen.Stand.Vorgabe(P, zeile)), zeile);
            Konditionierungskalender a = quelle.Kalender(G), b = personen.Stand.Kalender(P);
            Assert.Equal(P, b.Groesse);
            Assert.Null(b.Nennwert);
            Assert.True(Kalendervergleich.AngabeGleich(a.Grundangabe, b.Grundangabe));
            Assert.Equal(a.Perioden.Count, b.Perioden.Count);
            for (int i = 0; i < a.Perioden.Count; i++)
                Assert.True(Kalendervergleich.RegelGleich(a.Perioden[i], b.Perioden[i]), a.Perioden[i].ToString());
            Assert.Equal(quelle.Herkunft(G), personen.Stand.Herkunft(P));

            // Und zurück: dieselbe Ebene wie die Quelle, Zeichen für Zeichen.
            Ebenenergebnis zurueck = Vorlagenkopierregel.Umsetzen(personen.Stand, P, G, null, null);
            Assert.True(zurueck.Ok, zurueck.Meldung);
            Assert.True(zurueck.Stand.Gleich(quelle, mitBestand: true));

            // Komfort- und Absenksollwert spielen auf diesem Weg keine Rolle.
            Assert.True(Vorlagenkopierregel.Umsetzen(quelle, G, P, 99.0, 10.0).Stand.Gleich(personen.Stand));
        }

        [Fact]
        public void Was_die_Quelle_ausserhalb_ihrer_Groesse_traegt_reist_nicht_mit()
        {
            Konditionierungsstand quelle = Geraetevorlage()
                .MitVorgabe(L, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(2.0, 22, 6))
                .MitKalender(K, new Konditionierungskalender(K, Kalenderangabe.AusWert(26.0), null, null), Kalenderherkunft.Keine);
            Assert.True(TraegtAusserhalb(quelle, G));

            Ebenenergebnis e = Vorlagenkopierregel.Umsetzen(quelle, G, P, null, null);
            Assert.True(e.Ok, e.Meldung);
            Assert.Null(KonditionierungsvorlageCtrl.Groessenregel(e.Stand, P));
            Assert.Equal(1, e.Stand.KalenderAnzahl);
            Assert.Equal(4, e.Stand.VorgabenAnzahl);
        }

        // =============================================================================
        //  Heizen → Kühlen: Zeitstruktur, Aus-Zeiten, Komfort- und Absenksollwert
        // =============================================================================

        [Fact]
        public void Heizen_nach_Kuehlen_haelt_die_Zeitstruktur_und_die_Aus_Zeiten_und_setzt_Komfort_und_Absenksollwert()
        {
            Konditionierungsstand quelle = Heizvorlage();
            Assert.Equal(21.0, Vorlagenkopierregel.Tagwert(quelle, H));
            Ebenenergebnis e = Vorlagenkopierregel.Umsetzen(quelle, H, K, 25.5, 28.0);
            Assert.True(e.Ok, e.Meldung);
            Konditionierungsstand k = e.Stand;
            Assert.False(TraegtAusserhalb(k, K));

            // Die Zeilen: Tagwert -> Komfortsollwert, darunter (Nacht 17, Ferien 16) -> Absenksollwert, Zeiten
            // bleiben, „aus" bleibt, keine Saison (E54).
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(25.5), k.Vorgabe(K, DbWerte.KOND_ZEILE_TAG)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(28.0, 18, 7), k.Vorgabe(K, DbWerte.KOND_ZEILE_NACHT)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.Abgeschaltet(), k.Vorgabe(K, DbWerte.KOND_ZEILE_WOCHENENDE)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(28.0), k.Vorgabe(K, DbWerte.KOND_ZEILE_FERIEN)));
            Assert.False(Konditionierungsstand.Traegt(k.Vorgabe(K, DbWerte.KOND_ZEILE_SAISON)));
            Assert.False(Konditionierungsstand.Traegt(k.Vorgabe(K, DbWerte.KOND_ZEILE_NENNWERT)));

            // Die Woche: jede Stunde mit 21 °C wird 25,5 °C, jede mit 17 °C (Absenkzeit) 28 °C, jede Aus-Stunde
            // bleibt „aus".
            Konditionierungskalender kal = k.Kalender(K);
            Assert.Equal(Angabeart.Woche, kal.Grundangabe.Art);
            double[] heiz = Kalenderwerkzeuge.WocheAus(quelle.Kalender(H));
            for (int i = 0; i < Kalenderwoche.WOCHENWERTE; i++)
                Assert.Equal(double.IsNaN(heiz[i]) ? double.NaN : heiz[i] < 21.0 ? 28.0 : 25.5, kal.Standardwoche[i]);
            Assert.Null(kal.Nennwert);

            // Die Perioden: Rang, Art, Name und Tage bleiben, ebenso „aus" und „wie Sonntag"; Ferien- und
            // Saisonperiode reisen nicht (E54).
            Assert.Equal(4, kal.Perioden.Count);
            Assert.DoesNotContain(kal.Perioden, Konditionierungsarbeit.IstMatrixbereich);
            Kalenderregel neujahr = Assert.Single(kal.Perioden, r => r.IstFeiertag);
            Assert.Equal(Standardfahrplan.RANG_FEIERTAG, neujahr.Rang);
            Assert.Equal(DbWerte.KOND_FEIERTAGE[0], neujahr.Feiertagsregel);
            Assert.Equal(Angabeart.WieWochentag, neujahr.Angabe.Art);
            Assert.Equal(7, neujahr.Angabe.WieWochentag);
            Kalenderregel urlaub = Assert.Single(kal.Perioden, r => r.Bezeichner == "Betriebsurlaub");
            Assert.Equal((Standardfahrplan.RANG_EIGEN, 220, 234, Angabeart.Aus),
                         (urlaub.Rang, urlaub.Beginn, urlaub.Ende, urlaub.Angabe.Art));
            Kalenderregel inventur = Assert.Single(kal.Perioden, r => r.Bezeichner == "Inventur");
            Assert.Equal((DbWerte.KOND_ART_ZEITRAUM, 2, 3, Angabeart.Wert, 28.0),
                         (inventur.Art, inventur.Beginn, inventur.Ende, inventur.Angabe.Art, inventur.Angabe.Wert));
            // Die Messe heizt mit 22 °C über dem Tagwert: Komfortsollwert.
            Kalenderregel messe = Assert.Single(kal.Perioden, r => r.Bezeichner == "Messe");
            Assert.Equal(Angabeart.Woche, messe.Angabe.Art);
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < Kalenderwoche.TAGESSTUNDEN; s++)
                    Assert.Equal(s < 12 ? 25.5 : double.NaN, messe.Angabe.Woche[Kalenderwoche.Stelle(t, s)]);

            // Die Herkunft fällt - ihr Vermerk nennte Heizwerte.
            Assert.Equal(Kalenderherkunft.Keine, k.Herkunft(K));

            // Der Kalender läuft rund und ist für den Lauf gültig.
            Assert.True(Kalenderleser.Rundlaeuft(kal, out _, out _));
        }

        [Fact]
        public void Eine_Zelle_nur_mit_Zeiten_und_eine_Grundangabe_folgen_derselben_Regel()
        {
            Konditionierungsstand quelle = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(H, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.NurZeiten(22, 6))
                .MitVorgabe(H, DbWerte.KOND_ZEILE_FERIEN, Matrixzelle.AusWert(12.0))
                .MitKalender(H, new Konditionierungskalender(H, Kalenderangabe.AusWert(18.0), null, null), Kalenderherkunft.Keine);
            // Ohne Tagzeile ist der Tagwert der höchste Heizsollwert: die Grundangabe 18 °C. Die Ferien mit 12 °C
            // liegen darunter und bekommen den Absenksollwert.
            Assert.Equal(18.0, Vorlagenkopierregel.Tagwert(quelle, H));
            Konditionierungsstand k = Vorlagenkopierregel.Umsetzen(quelle, H, K, Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE,
                                                                  Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE).Stand;
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.NurZeiten(22, 6), k.Vorgabe(K, DbWerte.KOND_ZEILE_NACHT)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(28.0), k.Vorgabe(K, DbWerte.KOND_ZEILE_FERIEN)));
            Assert.Equal(26.0, k.Kalender(K).Grundangabe.Wert);

            Konditionierungsstand aus = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitKalender(H, new Konditionierungskalender(H, Kalenderangabe.Abgeschaltet, null, null), Kalenderherkunft.Keine);
            Assert.True(double.IsNaN(Vorlagenkopierregel.Tagwert(aus, H)));
            Assert.Equal(Angabeart.Aus, Vorlagenkopierregel.Umsetzen(aus, H, K, 26.0, 28.0).Stand.Kalender(K).Grundangabe.Art);

            // Eine leere Heizvorlage ergibt eine leere Kühlvorlage.
            Assert.True(Vorlagenkopierregel.Umsetzen(Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null), H, K, 26.0, 28.0)
                                           .Stand.TabellenLeer);
        }

        [Fact]
        public void Der_Komfortsollwert_haelt_die_Grenzen_der_Kuehlspalte()
        {
            // Wirksam nimmt die Kühlspalte der Matrix 15 … 30 °C an: das Feld 15 … 35 °C, die Zelle der Größe
            // Kühlen 0 … 30 °C - die engere Grenze gilt.
            Assert.Equal(15.0, Vorlagenkopierregel.KomfortsollwertMin);
            Assert.Equal(30.0, Vorlagenkopierregel.KomfortsollwertMax);
            Assert.Equal(26.0, Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE);
            Assert.Null(Vorlagenkopierregel.KomfortsollwertPruefen(Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE));

            foreach (double gut in new[] { 15.0, 22.5, 30.0, 25.1234 })
                Assert.Null(Vorlagenkopierregel.KomfortsollwertPruefen(gut));
            foreach (double? schlecht in new double?[] { null, double.NaN, double.PositiveInfinity, 14.99, 30.01, 35.0, 26.12345 })
            {
                Assert.NotNull(Vorlagenkopierregel.KomfortsollwertPruefen(schlecht));
                Ebenenergebnis e = Vorlagenkopierregel.Umsetzen(Heizvorlage(), H, K, schlecht, 28.0);
                Assert.False(e.Ok);
                Assert.Equal(Vorlagenkopierregel.KomfortsollwertPruefen(schlecht), e.Meldung);
            }
            Assert.Contains("15", Vorlagenkopierregel.KomfortsollwertPruefen(40.0));
            Assert.Contains("30", Vorlagenkopierregel.KomfortsollwertPruefen(40.0));
        }

        // =============================================================================
        //  Der Absenksollwert und der Tagwert
        // =============================================================================

        [Fact]
        public void Der_Tagwert_ist_die_Tagzeile_sonst_der_hoechste_Heizsollwert()
        {
            // Die Tagzeile gilt, auch wenn eine Periode höher heizt (Messe 22 °C bei Tag 21 °C).
            Assert.Equal(21.0, Vorlagenkopierregel.Tagwert(Heizvorlage(), H));

            // Ohne Tagzeile - und mit Tagzeile „aus" - der höchste Heizsollwert über Zeilen, Grundangabe,
            // Woche und Perioden; Ferien- und Saisonperioden zählen nicht (E54).
            Konditionierungsstand ohneTag = Heizvorlage(rein: true)
                .MitVorgabe(H, DbWerte.KOND_ZEILE_TAG, Matrixzelle.Leer);
            Assert.Equal(22.0, Vorlagenkopierregel.Tagwert(ohneTag, H));
            Assert.Equal(22.0, Vorlagenkopierregel.Tagwert(
                Heizvorlage(rein: true).MitVorgabe(H, DbWerte.KOND_ZEILE_TAG, Matrixzelle.Abgeschaltet()), H));

            Konditionierungsstand nurFerienperiode = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(H, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(17.0, 22, 6))
                .MitKalender(H, new Konditionierungskalender(H, Kalenderangabe.Abgeschaltet, null, new[]
                {
                    Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN, "Ferien 1", 180, 200,
                                           Kalenderangabe.AusWert(23.0)),
                }), Kalenderherkunft.Keine);
            Assert.Equal(17.0, Vorlagenkopierregel.Tagwert(nurFerienperiode, H));

            // Ohne Heizsollwert gibt es keinen Tagwert - und keine Absenkzeit.
            Assert.True(double.IsNaN(Vorlagenkopierregel.Tagwert(Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null), H)));
        }

        [Fact]
        public void Der_Absenksollwert_haelt_die_Grenzen_nimmt_aus_und_liegt_nicht_unter_dem_Komfortsollwert()
        {
            Assert.Equal(28.0, Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE);
            Assert.True(double.IsNaN(Vorlagenkopierregel.ABSENKSOLLWERT_AUS));
            Assert.Null(Vorlagenkopierregel.AbsenksollwertPruefen(Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE,
                                                                  Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE));
            Assert.Null(Vorlagenkopierregel.AbsenksollwertPruefen(Vorlagenkopierregel.ABSENKSOLLWERT_AUS, 26.0));

            // Dieselben Grenzen wie der Komfortsollwert; gleich dem Komfortsollwert ist erlaubt.
            foreach (double gut in new[] { 26.0, 27.5, 30.0, 28.1234 })
                Assert.Null(Vorlagenkopierregel.AbsenksollwertPruefen(gut, 26.0));
            foreach (double? schlecht in new double?[] { null, double.PositiveInfinity, double.NegativeInfinity, 30.01, 35.0, 28.12345 })
            {
                string meldung = Vorlagenkopierregel.AbsenksollwertPruefen(schlecht, 26.0);
                Assert.NotNull(meldung);
                Ebenenergebnis e = Vorlagenkopierregel.Umsetzen(Heizvorlage(), H, K, 26.0, schlecht);
                Assert.False(e.Ok);
                Assert.Equal(meldung, e.Meldung);
            }
            Assert.Equal("Für die Kopie von Heizen nach Kühlen fehlt der Absenksollwert – eine Zahl oder „aus“.",
                         Vorlagenkopierregel.AbsenksollwertPruefen(null, 26.0));
            Assert.Equal("Der Absenksollwert 40 °C liegt außerhalb der Grenzen der Kühlspalte 15 … 30 °C.",
                         Vorlagenkopierregel.AbsenksollwertPruefen(40.0, 26.0));

            // Unter dem Komfortsollwert: benannt abgelehnt - beim Kühlen ist die Absenkung höher.
            string unter = Vorlagenkopierregel.AbsenksollwertPruefen(24.0, 26.0);
            Assert.Equal("Der Absenksollwert 24 °C liegt unter dem Komfortsollwert 26 °C – beim Kühlen ist die Absenkung " +
                         "ein höherer Sollwert oder „aus“.", unter);
            Assert.Equal(unter, Vorlagenkopierregel.Pruefen(H, K, 26.0, 24.0));
            Assert.Equal(unter, Vorlagenkopierregel.Umsetzen(Heizvorlage(), H, K, 26.0, 24.0).Meldung);

            // Ein ungültiger Komfortsollwert geht vor - der Vergleich gehört dann nicht an das Absenkfeld.
            Assert.Null(Vorlagenkopierregel.AbsenksollwertPruefen(24.0, 40.0));
            Assert.Equal(Vorlagenkopierregel.KomfortsollwertPruefen(40.0), Vorlagenkopierregel.Pruefen(H, K, 40.0, 24.0));

            // Auf dem direkten Weg spielt der Absenksollwert keine Rolle.
            Assert.Null(Vorlagenkopierregel.Pruefen(G, P, null, null));

            using (new Kulturvorrichtung("en-US"))
                Assert.Contains("setback setpoint", Vorlagenkopierregel.AbsenksollwertPruefen(24.0, 26.0));
        }

        [Fact]
        public void Absenksollwert_aus_schaltet_die_Kuehlung_in_den_Absenkzeiten_ab()
        {
            Konditionierungsstand quelle = Heizvorlage();
            Ebenenergebnis e = Vorlagenkopierregel.Umsetzen(quelle, H, K, 26.0, Vorlagenkopierregel.ABSENKSOLLWERT_AUS);
            Assert.True(e.Ok, e.Meldung);
            Konditionierungsstand k = e.Stand;

            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(26.0), k.Vorgabe(K, DbWerte.KOND_ZEILE_TAG)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.Abgeschaltet(18, 7), k.Vorgabe(K, DbWerte.KOND_ZEILE_NACHT)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.Abgeschaltet(), k.Vorgabe(K, DbWerte.KOND_ZEILE_WOCHENENDE)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.Abgeschaltet(), k.Vorgabe(K, DbWerte.KOND_ZEILE_FERIEN)));

            Konditionierungskalender kal = k.Kalender(K);
            double[] heiz = Kalenderwerkzeuge.WocheAus(quelle.Kalender(H));
            for (int i = 0; i < Kalenderwoche.WOCHENWERTE; i++)
                Assert.Equal(!double.IsNaN(heiz[i]) && heiz[i] >= 21.0 ? 26.0 : double.NaN, kal.Standardwoche[i]);
            Assert.Equal(Angabeart.Aus, Assert.Single(kal.Perioden, r => r.Bezeichner == "Inventur").Angabe.Art);
            Assert.Equal(Angabeart.Woche, Assert.Single(kal.Perioden, r => r.Bezeichner == "Messe").Angabe.Art);
            Assert.True(Kalenderleser.Rundlaeuft(kal, out _, out _));
        }

        // =============================================================================
        //  Beschreibung und Größenregel
        // =============================================================================

        [Fact]
        public void Die_Beschreibung_nennt_die_Herkunft_und_bleibt_in_ihrer_Laenge()
        {
            Assert.Equal("EPOS-Muster mit runden Werten, weder Norm- noch Messwerte — aus Vorlage ‚Büro‘ (Heizen)",
                         Vorlagenkopierregel.Beschreibung(KonditionierungsvorlagenSaattabelle.BESCHREIBUNG, "Büro", H));
            Assert.Equal("aus Vorlage ‚Werkstatt‘ (Geräte)", Vorlagenkopierregel.Beschreibung(null, " Werkstatt ", G));
            Assert.Equal("aus Vorlage ‚Werkstatt‘ (Personen)", Vorlagenkopierregel.Beschreibung("  ", "Werkstatt", P));

            string lang = Vorlagenkopierregel.Beschreibung(new string('x', KonditionierungVorlagenSchema.BESCHREIBUNG_MAX_ZEICHEN),
                                                           "Büro", H);
            Assert.Equal(KonditionierungVorlagenSchema.BESCHREIBUNG_MAX_ZEICHEN, lang.Length);
            Assert.EndsWith("… — aus Vorlage ‚Büro‘ (Heizen)", lang);

            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("from template ‘Büro’ (Heating)", Vorlagenkopierregel.Beschreibung(null, "Büro", H));
        }

        [Fact]
        public void Die_Groessenregel_lehnt_Inhalt_einer_fremden_Groesse_benannt_ab()
        {
            Konditionierungsstand heizen = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(H, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(20.0));
            Assert.Null(KonditionierungsvorlageCtrl.Groessenregel(heizen, H));
            Assert.Null(KonditionierungsvorlageCtrl.Groessenregel(Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null), K));

            string fremd = KonditionierungsvorlageCtrl.Groessenregel(heizen, K);
            Assert.Contains("Kühlen", fremd);
            Assert.Contains("Heizen", fremd);
            Assert.NotNull(KonditionierungsvorlageCtrl.Groessenregel(
                heizen.MitKalender(P, new Konditionierungskalender(P, Kalenderangabe.AusWert(1.0), null, null), Kalenderherkunft.Keine), H));
        }

        // =============================================================================
        //  Die Ablage ohne Datenbank folgt derselben Regel
        // =============================================================================

        [Fact]
        public void Die_Ablage_kopiert_Heizen_nach_Kuehlen_als_eigene_Vorlage_mit_Herkunft()
        {
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            KonditionierungsvorlageCtrl.Vorlage buero = ablage.Liste(H).Single(v => v.Bezeichner == "Büro");
            Konditionierungsstand vorher = ablage.Inhalt(buero.Id, out _).Inhalt;
            int kuehlen = ablage.Liste(K).Count;

            // Der Vorschlag „Büro" steht in der Kühlliste schon: benannt abgelehnt, nichts angelegt.
            KonditionierungCtrl.Ergebnis doppelt = ablage.KopierenNach(buero.Id, K, " büro ", 26.0, 28.0, out long keine);
            Assert.False(doppelt.Ok);
            Assert.Equal(0, keine);
            Assert.Contains("„büro“", doppelt.Meldung);
            Assert.Contains("Kühlen", doppelt.Meldung);
            Assert.Equal(kuehlen, ablage.Liste(K).Count);

            Assert.False(ablage.KopierenNach(buero.Id, K, "Büro Heizung", null, 28.0, out _).Ok);
            Assert.False(ablage.KopierenNach(buero.Id, K, "Büro Heizung", 26.0, null, out _).Ok);
            Assert.False(ablage.KopierenNach(buero.Id, K, "Büro Heizung", 26.0, 25.0, out _).Ok);
            Assert.False(ablage.KopierenNach(buero.Id, L, "Büro Heizung", 26.0, 28.0, out _).Ok);
            Assert.False(ablage.KopierenNach(999, K, "Büro Heizung", 26.0, 28.0, out _).Ok);
            Assert.Equal(kuehlen, ablage.Liste(K).Count);

            KonditionierungCtrl.Ergebnis e = ablage.KopierenNach(buero.Id, K, "Büro Heizung", 26.0, 28.0, out long kopie);
            Assert.True(e.Ok, e.Meldung);
            KonditionierungsvorlageCtrl.Vorlage neu = ablage.Lesen(kopie);
            Assert.Equal((K, "Büro Heizung", DbWerte.KOND_NUTZUNG_BUERO, false),
                         (neu.Groesse, neu.Bezeichner, neu.Nutzung, neu.Ausgeliefert));
            Assert.Equal(KonditionierungsvorlagenSaattabelle.BESCHREIBUNG + " — aus Vorlage ‚Büro‘ (Heizen)", neu.Beschreibung);
            Assert.Equal("Büro Heizung", ablage.Liste(K).Last().Bezeichner);

            // Büro heizt Tag 20, Nacht 16 (18–7), Wochenende 16, Ferien 16, Grundangabe 16 - die Kopie kühlt am
            // Tag auf 26, in allen Absenkzeiten auf 28.
            Konditionierungsstand k = ablage.Inhalt(kopie, out _).Inhalt;
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(26.0), k.Vorgabe(K, DbWerte.KOND_ZEILE_TAG)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(28.0, 18, 7), k.Vorgabe(K, DbWerte.KOND_ZEILE_NACHT)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(28.0), k.Vorgabe(K, DbWerte.KOND_ZEILE_WOCHENENDE)));
            Assert.True(Kalendervergleich.ZelleGleich(Matrixzelle.AusWert(28.0), k.Vorgabe(K, DbWerte.KOND_ZEILE_FERIEN)));
            Assert.Equal(28.0, k.Kalender(K).Grundangabe.Wert);
            Assert.Equal(9, k.Kalender(K).Perioden.Count);
            Assert.All(k.Kalender(K).Perioden, r => Assert.Equal(7, r.Angabe.WieWochentag));
            Assert.Null(KonditionierungsvorlageCtrl.Groessenregel(k, K));

            // Die ausgelieferte Quelle bleibt, wie sie war.
            Assert.True(ablage.Lesen(buero.Id).Ausgeliefert);
            Assert.True(ablage.Inhalt(buero.Id, out _).Inhalt.Gleich(vorher));

            // Die Kopie ist eigen: umbenennen und löschen gehen.
            Assert.True(ablage.Umbenennen(kopie, "Kontor Kühlung").Ok);
            Assert.True(ablage.Loeschen(kopie).Ok);
        }

        [Fact]
        public void Die_Heizvorlage_Buero_mit_Absenksollwert_aus_kuehlt_wie_die_ausgelieferte_Kuehlvorlage_Buero()
        {
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            KonditionierungsvorlageCtrl.Vorlage heizen = ablage.Liste(H).Single(v => v.Bezeichner == "Büro");
            KonditionierungsvorlageCtrl.Vorlage kuehlen = ablage.Liste(K).Single(v => v.Bezeichner == "Büro");

            KonditionierungCtrl.Ergebnis e = ablage.KopierenNach(heizen.Id, K, "Büro aus Heizen", 26.0,
                                                                 Vorlagenkopierregel.ABSENKSOLLWERT_AUS, out long kopie);
            Assert.True(e.Ok, e.Meldung);
            Konditionierungsstand k = ablage.Inhalt(kopie, out _).Inhalt;
            Konditionierungsstand soll = ablage.Inhalt(kuehlen.Id, out _).Inhalt;

            // Tag 26, nachts (18–7), am Wochenende, in den Ferien und in der Grundangabe „aus" - Zeile für Zeile
            // wie die ausgelieferte Kühlvorlage.
            foreach (string zeile in new[] { DbWerte.KOND_ZEILE_TAG, DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_WOCHENENDE,
                                             DbWerte.KOND_ZEILE_FERIEN })
                Assert.True(Kalendervergleich.ZelleGleich(soll.Vorgabe(K, zeile), k.Vorgabe(K, zeile)), zeile);
            Assert.True(Kalendervergleich.AngabeGleich(soll.Kalender(K).Grundangabe, k.Kalender(K).Grundangabe));
        }

        [Fact]
        public void Die_Ablage_kopiert_Geraete_und_Personen_unveraendert_in_die_andere_Liste()
        {
            Konditionierungsvorlagenablage ablage = Konditionierungsvorlagenablage.AusSaat();
            foreach ((Konditionierungsgroesse von, Konditionierungsgroesse nach) in new[] { (G, P), (P, G) })
                foreach (KonditionierungsvorlageCtrl.Vorlage quelle in ablage.Liste(von).Where(v => v.Ausgeliefert).ToList())
                {
                    Assert.False(ablage.KopierenNach(quelle.Id, nach, quelle.Bezeichner, null, null, out _).Ok,
                                 quelle.Bezeichner + ": der Name steht in der Zielliste schon");
                    KonditionierungCtrl.Ergebnis e = ablage.KopierenNach(quelle.Id, nach, quelle.Bezeichner + " (" + von + ")", null,
                                                                         null, out long kopie);
                    Assert.True(e.Ok, e.Meldung);
                    Konditionierungsstand a = ablage.Inhalt(quelle.Id, out _).Inhalt, b = ablage.Inhalt(kopie, out _).Inhalt;
                    foreach (string zeile in DbWerte.KOND_ZEILEN)
                        Assert.True(Kalendervergleich.ZelleGleich(a.Vorgabe(von, zeile), b.Vorgabe(nach, zeile)), quelle.Bezeichner + "/" + zeile);
                    Assert.Equal(a.Kalender(von) == null, b.Kalender(nach) == null);
                    if (a.Kalender(von) != null)
                    {
                        Assert.True(Kalendervergleich.AngabeGleich(a.Kalender(von).Grundangabe, b.Kalender(nach).Grundangabe));
                        Assert.Equal(a.Kalender(von).Perioden.Count, b.Kalender(nach).Perioden.Count);
                        for (int i = 0; i < a.Kalender(von).Perioden.Count; i++)
                            Assert.True(Kalendervergleich.RegelGleich(a.Kalender(von).Perioden[i], b.Kalender(nach).Perioden[i]));
                    }
                    Assert.Null(KonditionierungsvorlageCtrl.Groessenregel(b, nach));
                    Assert.False(ablage.Lesen(kopie).Ausgeliefert);
                }
        }
    }
}
