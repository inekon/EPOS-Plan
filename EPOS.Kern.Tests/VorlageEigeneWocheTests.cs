using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Eine Vorlage mit eigener Standardwoche</b> (Befund NP4c, Auftrag NP4d): Bringt die Vorlage — oder das Profil, aus
    /// dem der <see cref="Raumnutzungsgenerator"/> sie bildet (Stundenprofil, NP-F9) — eine eigene Woche der Größe mit, bildet
    /// <see cref="Konditionierungsarbeit.VorlageUebernehmen"/> den Fahrplan daraus und braucht keine Zeile des Ziels; Ferien,
    /// Saison und Nennwert der Spalte des Ziels gelten weiter, wo es sie gibt. Ohne eigene Woche bleibt der Weg, wie er ist
    /// (bitgleich): Trägt das Ziel keine Angabe der Größe, wird benannt abgelehnt. P1 nach E93 bleibt unverändert.
    ///
    /// <para>Ohne Datenbank; die Werte sind runde Phantasiewerte, keine Normangaben. Die Klasse pinnt de-DE, weil
    /// Herkunft und Ablehnung Ressourcentexte sind.</para>
    /// </summary>
    public sealed class VorlageEigeneWocheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt die Kultur zurück.</summary>
        public void Dispose() => _kultur.Dispose();

        private const Konditionierungsgroesse P = Konditionierungsgroesse.Personen;

        /// <summary>Anwesenheit Montag bis Freitag 8 bis 18 Uhr, sonst null.</summary>
        private static double[] Anwesenheit()
            => Enumerable.Range(0, 168).Select(i => i / 24 < 5 && i % 24 is >= 8 and < 18 ? 1.0 : 0.0).ToArray();

        /// <summary>Eine Personenvorlage „Profil 1" aus einem Profil: die Woche (oder ein fester Wert) und Neujahr wie Sonntag.</summary>
        private static Konditionierungsvorlage Personenvorlage(bool mitWoche = true, double? nennwert = null)
        {
            Kalenderangabe grund = mitWoche ? Kalenderangabe.AusWoche(Anwesenheit()) : Kalenderangabe.AusWert(0.5);
            Kalenderregel neujahr = Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr", DbWerte.KOND_FEIERTAGE[0],
                                                           Kalenderangabe.AlsWochentag(6));
            Konditionierungsstand s = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitKalender(P, new Konditionierungskalender(P, grund, null, new[] { neujahr }), Kalenderherkunft.Keine);
            if (nennwert.HasValue)
                s = s.MitVorgabe(P, DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(nennwert.Value));
            return new Konditionierungsvorlage(11, "Profil 1", P, s, AusProfil: true);
        }

        private static void WocheGleich(double[] erwartet, Kalenderangabe ist)
        {
            Assert.Equal(Angabeart.Woche, ist.Art);
            Assert.Equal(erwartet.Length, ist.Woche.Count);
            for (int i = 0; i < erwartet.Length; i++)
                Assert.True(BitConverter.DoubleToInt64Bits(erwartet[i]) == BitConverter.DoubleToInt64Bits(ist.Woche[i]),
                            "Stunde " + i);
        }

        [Fact]
        public void Eine_eigene_Woche_der_Personen_braucht_keine_Personenzeile_des_Ziels()
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand();
            Assert.Equal(Fahrplanbefund.KeineAngabe, Standardfahrplan.Erzeugen(a.Matrix(null), P, rundlaufPruefen: true).Befund);

            Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(P), Personenvorlage());
            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(s);

            Konditionierungskalender k = b.Gebaeude.Kalender(P);
            Assert.NotNull(k);
            WocheGleich(Anwesenheit(), k.Grundangabe);
            Kalenderregel neujahr = Assert.Single(k.Perioden);           // die Personenspalte des Ziels trägt keine Ferien
            Assert.True(neujahr.IstFeiertag);
            Assert.Equal(Standardfahrplan.RANG_FEIERTAG, neujahr.Rang);
            Assert.Equal("Profil 1", b.Gebaeude.Herkunft(P).Vorlage);
            Assert.Equal(1, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Kalender));

        }

        /// <summary>
        /// Trägt das Ziel nur den Nennwert der Personen (keine Zeile Tag), geht er in den Fahrplan der eigenen Woche, und
        /// P1 gilt ohne eigenen Personennennwert der Vorlage: der Gerätewert sinkt um das Jahresmittel der Personenwärme.
        /// </summary>
        [Fact]
        public void Der_Nennwert_des_Ziels_gilt_und_P1_senkt_den_Geraetewert()
        {
            Konditionierungsarbeitsstand a = MitGeraeten(KonditionierungsarbeitTests.Stand());
            a = a.MitGebaeude(a.Gebaeude.MitVorgabe(P, DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(100.0)));
            Assert.Equal(Fahrplanbefund.KeineAngabe, Standardfahrplan.Erzeugen(a.Matrix(null), P, rundlaufPruefen: true).Befund);

            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(P), Personenvorlage()));
            Konditionierungskalender k = b.Gebaeude.Kalender(P);
            WocheGleich(Anwesenheit(), k.Grundangabe);
            Assert.Equal(100.0, k.Nennwert);

            double erwartet = Math.Round(Konditionierungsarbeit.GeraeteNennwertNachPersonen(500.0, k, b.Kalender), 4,
                                         MidpointRounding.AwayFromZero);
            Assert.True(erwartet < 500.0);
            Assert.Equal(erwartet, b.Gebaeude.Bestand.InterneWaermegewinne);
        }

        /// <summary>Das Probegebäude mit einer Gerätelast von 500 W (runder Phantasiewert).</summary>
        private static Konditionierungsarbeitsstand MitGeraeten(Konditionierungsarbeitsstand a)
            => a.MitGebaeude(a.Gebaeude.MitBestand(x => x.InterneWaermegewinne = 500.0));

        [Fact]
        public void Mit_eigenem_Personennennwert_bleibt_der_Geraetewert_des_Ziels()
        {
            Konditionierungsarbeitsstand a = MitGeraeten(KonditionierungsarbeitTests.Stand());
            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(P), Personenvorlage(nennwert: 200.0)));

            Konditionierungskalender k = b.Gebaeude.Kalender(P);
            WocheGleich(Anwesenheit(), k.Grundangabe);
            Assert.Equal(200.0, k.Nennwert);                             // der Nennwert der Spalte geht in den Fahrplan
            Assert.Equal(500.0, b.Gebaeude.Bestand.InterneWaermegewinne);  // E93: P1 gilt nicht
        }

        [Fact]
        public void Ohne_eigene_Woche_lehnt_die_Uebernahme_am_Ziel_ohne_Personenzeilen_benannt_ab()
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand();
            Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(P),
                                                                                  Personenvorlage(mitWoche: false));
            Assert.False(s.Ok);
            Assert.Contains("PERSONEN", s.Meldung);
            Assert.Contains(Fahrplanbefund.KeineAngabe.ToString(), s.Meldung);
        }

        [Fact]
        public void An_einer_Zone_ohne_Personenzeilen_und_am_Gebaeude_mit_Zonen_gilt_die_eigene_Woche()
        {
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand(KonditionierungsarbeitTests.Zone(1, "Zone 1"));

            Konditionierungsarbeitsstand z = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(P, 1), Personenvorlage()));
            WocheGleich(Anwesenheit(), z.Zone(1).Stand.Kalender(P).Grundangabe);
            Assert.Null(z.Gebaeude.Kalender(P));

            // Am Gebäude: die Zone ohne eigene Angabe bekommt keinen eigenen Kalender, sie folgt dem des Gebäudes (B4).
            Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(P), Personenvorlage());
            Konditionierungsarbeitsstand g = KonditionierungsarbeitTests.Gut(s);
            Assert.Null(g.Zone(1).Stand.Kalender(P));
            Assert.Equal(0, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Zonenkalender));
            Assert.True(Kalendervergleich.KalenderGleich(g.Gebaeude.Kalender(P), g.GeltenderKalender(P, 1)));
        }

        [Fact]
        public void Am_Ziel_mit_Angabe_bleibt_der_heutige_Weg_samt_Ferien_des_Ziels()
        {
            // Heizen mit eigener Woche am Probegebäude (Sollwert, Ferien 200 bis 214): Fahrplan aus der Matrix des Ziels,
            // darüber die Woche der Vorlage (Zusammenfuehren) — wie ohne diese Änderung.
            Konditionierungsgroesse h = Konditionierungsgroesse.Heizsoll;
            double[] woche = Enumerable.Range(0, 168).Select(i => i % 24 is >= 6 and < 22 ? 21.0 : 17.0).ToArray();
            Konditionierungsstand inhalt = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitKalender(h, new Konditionierungskalender(h, Kalenderangabe.AusWoche(woche), null, Array.Empty<Kalenderregel>()),
                             Kalenderherkunft.Keine);
            var vorlage = new Konditionierungsvorlage(12, "Profil 2", h, inhalt, AusProfil: true);
            Konditionierungsarbeitsstand a = KonditionierungsarbeitTests.Stand();

            Fahrplanlesung l = Standardfahrplan.Erzeugen(a.Matrix(null), h, rundlaufPruefen: true);
            Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
            Konditionierungskalender erwartet = Konditionierungsarbeit.Zusammenfuehren(l.Kalender, inhalt.Kalender(h), null,
                                                                                       out string fehler);
            Assert.Null(fehler);

            Konditionierungsarbeitsstand b = KonditionierungsarbeitTests.Gut(
                Konditionierungsarbeit.VorlageUebernehmen(a, KonditionierungsarbeitTests.Ort(h), vorlage));
            Konditionierungskalender k = b.Gebaeude.Kalender(h);
            Assert.True(Kalendervergleich.KalenderGleich(erwartet, k));
            WocheGleich(woche, k.Grundangabe);
            Kalenderregel ferien = k.Perioden.Single(r => r.Art == DbWerte.KOND_ART_FERIEN);
            Assert.Equal(200, ferien.Beginn);
            Assert.Equal(214, ferien.Ende);
        }
    }
}
