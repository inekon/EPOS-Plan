using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Arbeitsstand der Konditionierung, rein</b> (Stufe KP2, Welle K2, Teilschritt 1; Entwurf
    /// KP2 Abschnitt 2): Zelle setzen samt Weiche, Anlegen, Verwerfen, Matrix erneut anwenden (P12),
    /// Vorlage übernehmen, Als-Vorlage-Inhalt (E54), die drei Werkzeuge, der Abdruck, die Herkunft
    /// und der Satz aus dem Speicher — alles <b>ohne Datenbank</b>.
    ///
    /// <para>Jeder Schritt liefert einen NEUEN Stand; der alte bleibt, wie er war („Zurücknehmen"
    /// über den vorigen Stand). Die Klasse pinnt de-DE, weil Herkunft und Vermerk Ressourcentexte
    /// sind.</para>
    /// </summary>
    public class KonditionierungsarbeitTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary>Stellt die Kultur zurück.</summary>
        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Das Probegebäude
        // =============================================================================

        /// <summary>Das Probegebäude mit getrennter Lüftung, Wochenendwert 16 °C und einem Sommerferienzeitraum.</summary>
        internal static Matrixeingang Bestand()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Luftwechsel_Infiltration = 0.3;
            g.Luftwechsel_Nutzer = 0.4;
            g.Raumsolltemperatur_Wochenende = 16.0;
            g.Wochenende = 1;
            g.Ferien = 1;
            g.Raumsolltemperatur_Ferien = 16.0;
            g.Ferienbeginn_1 = 200.0;
            g.Ferienende_1 = 214.0;
            g.Bewohner = 5.0;
            return Konditionierungseingang.Bestand(g, false, false);
        }

        internal static Konditionierungsarbeitsstand Stand(params Konditionierungszone[] zonen)
            => new Konditionierungsarbeitsstand(Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, Bestand()),
                                                zonen, 201.0, 2025);

        internal static Konditionierungszone Zone(long id, string name, Action<Matrixeingang> eigene = null)
        {
            var b = new Matrixeingang();
            eigene?.Invoke(b);
            return new Konditionierungszone(id, name, null, true, Konditionierungsstand.Leer(Kalendereigentuemer.Zone, b));
        }

        internal static Konditionierungsort Ort(Konditionierungsgroesse g, long? zone = null) => new Konditionierungsort(g, zone);

        internal static Konditionierungsarbeitsstand Gut(Konditionierungsschritt s)
        {
            Assert.True(s.Ok, s.Meldung);
            Assert.NotNull(s.Stand);
            return s.Stand;
        }

        // =============================================================================
        //  Zelle setzen — die Weiche (Konzept 5.6)
        // =============================================================================

        [Fact]
        public void Eine_Bestandszelle_landet_im_Bestand_und_die_Vorgabezelle_traegt_nur_aus_und_Zeiten()
        {
            Konditionierungsarbeitsstand a = Stand();
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(21.0)));

            Assert.Equal(21.0, b.Gebaeude.Bestand.SollTag);
            Assert.False(Konditionierungsstand.Traegt(b.Gebaeude.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG)));
            Assert.Equal(21.0, b.Matrix(null).Heizsoll.Tag.Wert);

            // „aus" steht in der Vorgabezelle und schlägt den Wert; der Wert bleibt in der Spalte.
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.ZelleSetzen(
                b, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_TAG, Matrixzelle.Abgeschaltet()));
            Assert.Equal(21.0, c.Gebaeude.Bestand.SollTag);
            Assert.True(c.Gebaeude.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG).Aus);
            Assert.True(c.Matrix(null).Heizsoll.Tag.Aus);
        }

        [Fact]
        public void Eine_neue_Zelle_traegt_ihren_Wert_und_eine_leere_Zelle_faellt()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(), Ort(Konditionierungsgroesse.Lueftung), DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(2.0, 22, 6, 3.0)));
            Matrixzelle n = a.Gebaeude.Vorgabe(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT);
            Assert.Equal(2.0, n.Wert);
            Assert.Equal(22, n.Von);
            Assert.Equal(6, n.Bis);
            Assert.Equal(3.0, n.BedingtK);
            Assert.Single(a.Gebaeude.Vorgabezeilen());

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Lueftung), DbWerte.KOND_ZEILE_NACHT, Matrixzelle.Leer));
            Assert.Empty(b.Gebaeude.Vorgabezeilen());
        }

        [Fact]
        public void Jeder_Schritt_liefert_einen_neuen_Stand_und_der_alte_bleibt()
        {
            Konditionierungsarbeitsstand a = Stand();
            string vorher = Konditionierungsarbeit.Abdruck(a);
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)));
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.Anlegen(b, Ort(Konditionierungsgroesse.Heizsoll)));

            // „Zurücknehmen" ist der vorige Stand — er ist unberührt.
            Assert.Equal(vorher, Konditionierungsarbeit.Abdruck(a));
            Assert.Null(b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll));
            Assert.NotNull(c.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll));
            Assert.NotEqual(Konditionierungsarbeit.Abdruck(b), Konditionierungsarbeit.Abdruck(c));

            // Der Bestand kommt als Kopie heraus: eine Änderung daran ändert den Stand nicht.
            Matrixeingang kopie = a.Gebaeude.Bestand;
            kopie.SollTag = 99.0;
            Assert.Equal(20.0, a.Gebaeude.Bestand.SollTag);
        }

        [Fact]
        public void Eine_Zone_erbt_eine_geleerte_Bestandszelle()
        {
            Konditionierungsarbeitsstand a = Stand(Zone(-1, "Anbau", b => b.SollTag = 22.0));
            Assert.Equal(22.0, a.Matrix(-1).Heizsoll.Tag.Wert);

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Heizsoll, -1), DbWerte.KOND_ZEILE_TAG, Matrixzelle.Leer));
            Assert.Null(b.Zone(-1).Stand.Bestand.SollTag);
            Assert.Equal(20.0, b.Matrix(-1).Heizsoll.Tag.Wert);          // wie das Gebäude
        }

        [Fact]
        public void Eine_Zelle_ausserhalb_der_Grenzen_oder_mit_falschen_Zeiten_wird_benannt_abgelehnt()
        {
            Konditionierungsarbeitsstand a = Stand();
            foreach ((Konditionierungsgroesse g, string zeile, Matrixzelle zelle) in new[]
                     {
                         (Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(25.0)),
                         (Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(1.5)),
                         (Konditionierungsgroesse.Heizsoll, "MITTAG", Matrixzelle.AusWert(20.0)),
                         (Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(18.0, 24, 6)),
                         (Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, null)),
                         (Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(20.0, 1, 2)),
                         (Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(18.0, 22, 6, 2.0)),
                         (Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(1.0, 22, 6, 6.0)),
                     })
            {
                Konditionierungsschritt s = Konditionierungsarbeit.ZelleSetzen(a, Ort(g), zeile, zelle);
                Assert.False(s.Ok, g + "/" + zeile + ": angenommen");
                Assert.Null(s.Stand);
                Assert.False(string.IsNullOrEmpty(s.Meldung));
            }
            Assert.False(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Heizsoll, 99),
                                                             DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(20.0)).Ok);
        }

        // =============================================================================
        //  Anlegen, Verwerfen, erneut anwenden (P12)
        // =============================================================================

        [Fact]
        public void Anlegen_legt_den_Kalender_des_Generators_an()
        {
            Konditionierungsarbeitsstand a = Stand();
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            Fahrplanlesung l = Standardfahrplan.Erzeugen(a.Matrix(null), Konditionierungsgroesse.Heizsoll, true);
            Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
            Assert.True(Kalendervergleich.KalenderGleich(l.Kalender, b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll)));
            Assert.True(b.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll).IstLeer);

            // „Anlegen ändert keine Reihe": die Reihe aus dem Speicher ist dieselbe wie abgeleitet.
            double[] abgeleitet = l.Kalender.Auswerten(a.W0, a.Referenzjahr);
            double[] angelegt = b.GeltenderKalender(Konditionierungsgroesse.Heizsoll, null).Auswerten(a.W0, a.Referenzjahr);
            Assert.Equal(abgeleitet, angelegt);

            // Zweimal anlegen ersetzt — derselbe Kalender.
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.Anlegen(b, Ort(Konditionierungsgroesse.Heizsoll)));
            Assert.True(c.Gebaeude.Gleich(b.Gebaeude));
        }

        [Fact]
        public void Ein_Wert_ohne_Rundlauf_wird_beim_Anlegen_benannt_abgelehnt()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(), Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(20.00001)));
            Konditionierungsschritt s = Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll));
            Assert.False(s.Ok);
            Assert.Contains(nameof(Fahrplanbefund.RundlaufVerletzt), s.Meldung, StringComparison.Ordinal);
        }

        [Fact]
        public void Verwerfen_nimmt_den_Kalender_und_laesst_die_Matrix()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(), Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            Konditionierungsschritt v = Konditionierungsarbeit.Verwerfen(b, Ort(Konditionierungsgroesse.Heizsoll));
            Konditionierungsarbeitsstand c = Gut(v);
            Assert.Null(c.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll));
            Assert.Equal(274, c.Gebaeude.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON).Von);
            Assert.Equal(1, v.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Kalender));

            // Ohne angelegten Kalender ist Verwerfen folgenlos.
            Assert.Same(c, Gut(Konditionierungsarbeit.Verwerfen(c, Ort(Konditionierungsgroesse.Heizsoll))));
        }

        [Fact]
        public void Matrix_erneut_anwenden_ersetzt_nur_den_Matrixbereich()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(), Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)));
            a = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            a = MitEigenerPeriode(a);
            Assert.Equal(4, a.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll).Perioden.Count);   // Ferien, Saison, zwei eigene

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(250, 100)));
            Konditionierungsschritt s = Konditionierungsarbeit.MatrixErneut(b, Ort(Konditionierungsgroesse.Heizsoll));
            Konditionierungskalender neu = Gut(s).Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll);

            Assert.Equal(4, neu.Perioden.Count);
            Kalenderregel pause = neu.Perioden.Single(r => r.Art == DbWerte.KOND_ART_BETRIEBSPAUSE);
            Assert.Equal(101, pause.Beginn);
            Assert.Equal(249, pause.Ende);
            Assert.Contains(neu.Perioden, r => r.Art == DbWerte.KOND_ART_ZEITRAUM && r.Rang == 500);
            Assert.Contains(neu.Perioden, r => r.Art == DbWerte.KOND_ART_FEIERTAG && r.Rang == 600);

            Assert.Equal(1, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Saisonperioden));
            Assert.Equal(1, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Ferienperioden));
            Assert.Equal(1, s.Bilanz.BleibtAnzahl(Konditionierungspostenart.EigenePerioden));
            Assert.Equal(1, s.Bilanz.BleibtAnzahl(Konditionierungspostenart.Feiertage));
        }

        [Fact]
        public void Matrix_erneut_anwenden_ohne_Kalender_legt_ihn_an_und_eine_Rangkollision_wird_abgelehnt()
        {
            Konditionierungsarbeitsstand a = Stand();
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.MatrixErneut(a, Ort(Konditionierungsgroesse.Heizsoll)));
            Assert.NotNull(b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll));

            // Eine eigene Periode auf dem Rang der Saison: die neue Saison kollidiert — benannt.
            Konditionierungskalender k = b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll);
            var perioden = new List<Kalenderregel>(k.Perioden)
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON, DbWerte.KOND_ART_ZEITRAUM, "Umbau", 10, 20,
                                       Kalenderangabe.AusWert(12.0)),
            };
            Konditionierungsarbeitsstand c = b.MitGebaeude(b.Gebaeude.MitKalender(Konditionierungsgroesse.Heizsoll,
                new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, k.Grundangabe, null, perioden), null));
            // Der Kalender folgt der Matrix (E56 F2 (a)) — die Kollision fällt schon beim Setzen der Saison auf.
            Konditionierungsschritt folgen = Konditionierungsarbeit.ZelleSetzen(
                c, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120));
            Assert.False(folgen.Ok);
            Assert.Contains("900", folgen.Meldung, StringComparison.Ordinal);

            // Steht die Saison schon in der Matrix, lehnt „Matrix erneut anwenden" ebenso benannt ab.
            c = c.MitGebaeude(c.Gebaeude.MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON,
                                                    Matrixzelle.NurZeiten(274, 120)));
            Konditionierungsschritt s = Konditionierungsarbeit.MatrixErneut(c, Ort(Konditionierungsgroesse.Heizsoll));
            Assert.False(s.Ok);
            Assert.Contains("900", s.Meldung, StringComparison.Ordinal);
        }

        /// <summary>Der angelegte Heizkalender mit einer eigenen Periode (Rang 500) und einem Feiertag (Rang 600).</summary>
        internal static Konditionierungsarbeitsstand MitEigenerPeriode(Konditionierungsarbeitsstand a)
        {
            Konditionierungskalender k = a.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll);
            var perioden = new List<Kalenderregel>(k.Perioden)
            {
                Kalenderregel.Zeitraum(500, DbWerte.KOND_ART_ZEITRAUM, "Betriebsurlaub", 100, 110, Kalenderangabe.AusWert(14.0)),
                Kalenderregel.Feiertag(600, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AusWert(15.0)),
            };
            return a.MitGebaeude(a.Gebaeude.MitKalender(Konditionierungsgroesse.Heizsoll,
                new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, k.Grundangabe, k.Nennwert, perioden),
                a.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll)));
        }

        // =============================================================================
        //  Vorlagen (P11, P12, E54)
        // =============================================================================

        /// <summary>Eine Heizvorlage „Büro" nach der Saat: Tag 20, Nacht 16 (18–7), Wochenende 16, Ferien 16.</summary>
        internal static Konditionierungsvorlage Buero()
        {
            Konditionierungsstand v = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(20.0))
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(16.0, 18, 7))
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_WOCHENENDE, Matrixzelle.AusWert(16.0))
                .MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_FERIEN, Matrixzelle.AusWert(16.0));
            return new Konditionierungsvorlage(7, "Büro", Konditionierungsgroesse.Heizsoll, v);
        }

        [Fact]
        public void Vorlage_uebernehmen_setzt_die_Zellen_und_legt_den_Kalender_mit_den_Ferien_des_Ziels_an()
        {
            Konditionierungsarbeitsstand a = Stand();
            Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(a, Ort(Konditionierungsgroesse.Heizsoll), Buero());
            Konditionierungsarbeitsstand b = Gut(s);

            Matrixeingang bestand = b.Gebaeude.Bestand;
            Assert.Equal(20.0, bestand.SollTag);
            Assert.Equal(16.0, bestand.SollNacht);
            Assert.Equal(16.0, bestand.SollWochenende);
            Assert.Equal(16.0, bestand.SollFerien);

            Konditionierungskalender k = b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll);
            Assert.NotNull(k);
            Kalenderregel ferien = k.Perioden.Single(r => r.Art == DbWerte.KOND_ART_FERIEN);
            Assert.Equal(200, ferien.Beginn);                  // der Ferienzeitraum des ZIELS
            Assert.Equal(214, ferien.Ende);
            Assert.Equal("Büro", b.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll).Vorlage);
            Assert.Equal("aus Vorlage Büro", b.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll).Bemerkung());
            Assert.Equal(4, s.Bilanz.ErsetztAnzahl(Konditionierungspostenart.Matrixzellen));

            // Eine Vorlage der falschen Größe wird benannt abgelehnt.
            Assert.False(Konditionierungsarbeit.VorlageUebernehmen(a, Ort(Konditionierungsgroesse.Kuehlsoll), Buero()).Ok);
        }

        [Fact]
        public void Vorlage_uebernehmen_laesst_eigene_Perioden_und_Nennwert_und_Saison_des_Ziels_stehen()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.ZelleSetzen(
                Stand(), Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)));
            a = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            a = MitEigenerPeriode(a);

            Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(a, Ort(Konditionierungsgroesse.Heizsoll), Buero());
            Konditionierungskalender k = Gut(s).Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll);
            Assert.Contains(k.Perioden, r => r.Rang == 500 && r.Art == DbWerte.KOND_ART_ZEITRAUM);
            Assert.Contains(k.Perioden, r => r.Rang == 600 && r.IstFeiertag);
            Kalenderregel pause = k.Perioden.Single(r => r.Art == DbWerte.KOND_ART_BETRIEBSPAUSE);
            Assert.Equal(121, pause.Beginn);                 // die Saison des Ziels bleibt
            Assert.Equal(273, pause.Ende);
            Assert.Equal(1, s.Bilanz.BleibtAnzahl(Konditionierungspostenart.EigenePerioden));
        }

        [Fact]
        public void Als_Vorlage_nimmt_die_Nutzungszeilen_ohne_Nennwert_und_Saison()
        {
            Konditionierungsarbeitsstand a = Stand();
            a = Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON,
                                                        Matrixzelle.NurZeiten(274, 120)));
            a = Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_NENNWERT,
                                                        Matrixzelle.AusWert(21.0)));
            a = Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_WOCHENENDE,
                                                        Matrixzelle.Abgeschaltet()));
            a = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            a = MitEigenerPeriode(a);

            Ebenenergebnis r = Konditionierungsarbeit.AlsVorlage(a, Ort(Konditionierungsgroesse.Heizsoll));
            Assert.True(r.Ok, r.Meldung);
            Konditionierungsstand v = r.Stand;
            Assert.Equal(Kalendereigentuemer.Vorlage, v.Art);
            Assert.False(Konditionierungsstand.Traegt(v.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NENNWERT)));
            Assert.False(Konditionierungsstand.Traegt(v.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON)));
            Assert.Equal(20.0, v.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG).Wert);
            Assert.True(v.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_WOCHENENDE).Aus);   // „aus" schlägt den Wert
            Assert.Equal(16.0, v.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_FERIEN).Wert);

            Konditionierungskalender k = v.Kalender(Konditionierungsgroesse.Heizsoll);
            Assert.Null(k.Nennwert);
            Assert.DoesNotContain(k.Perioden, p => Konditionierungsarbeit.IstMatrixbereich(p));
            Assert.Equal(2, k.Perioden.Count);
            Assert.Empty(v.Vorgabezeilen().Where(z => z.Groesse != DbWerte.KOND_GROESSE_HEIZSOLL));
        }

        // =============================================================================
        //  Die Werkzeuge der Karte
        // =============================================================================

        [Fact]
        public void Das_Zeitfenster_braucht_einen_angelegten_Kalender_und_setzt_seine_Stunden()
        {
            Konditionierungsarbeitsstand a = Stand();
            Assert.False(Konditionierungsarbeit.Zeitfenster(a, Ort(Konditionierungsgroesse.Heizsoll), new[] { 0 }, 6, 8, 22.0).Ok);

            a = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Heizsoll)));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Zeitfenster(
                a, Ort(Konditionierungsgroesse.Heizsoll), new[] { 0, 1, 2, 3, 4 }, 6, 8, 22.0));
            IReadOnlyList<double> woche = b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll).Standardwoche;
            Assert.Equal(22.0, woche[Kalenderwoche.Stelle(0, 6)]);
            Assert.Equal(22.0, woche[Kalenderwoche.Stelle(4, 7)]);
            Assert.NotEqual(22.0, woche[Kalenderwoche.Stelle(5, 6)]);
            Assert.StartsWith("Zeitfenster", b.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll).Vermerk, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Feiertage_legen_neun_Regeln_einmal_an()
        {
            Konditionierungsarbeitsstand a = Gut(Konditionierungsarbeit.Anlegen(Stand(), Ort(Konditionierungsgroesse.Heizsoll)));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Feiertage(a, Ort(Konditionierungsgroesse.Heizsoll), 7));
            Assert.Equal(9, b.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll).Perioden.Count(p => p.IstFeiertag));
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.Feiertage(b, Ort(Konditionierungsgroesse.Heizsoll), 7));
            Assert.Equal(9, c.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll).Perioden.Count(p => p.IstFeiertag));
        }

        [Fact]
        public void Zeitstruktur_wie_Heizung_setzt_Tag_und_Nachtwert_des_Ziels()
        {
            Konditionierungsarbeitsstand a = Stand();
            a = Gut(Konditionierungsarbeit.ZelleSetzen(a, Ort(Konditionierungsgroesse.Geraete), DbWerte.KOND_ZEILE_NACHT,
                                                        Matrixzelle.AusWert(0.1)));
            a = Gut(Konditionierungsarbeit.Anlegen(a, Ort(Konditionierungsgroesse.Geraete)));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.Zeitstruktur(
                a, Ort(Konditionierungsgroesse.Geraete), Zeitstrukturquelle.WieHeizung));
            IReadOnlyList<double> woche = b.Gebaeude.Kalender(Konditionierungsgroesse.Geraete).Standardwoche;
            Assert.Equal(1.0, woche[Kalenderwoche.Stelle(0, 12)]);     // Heizung am Tag: 20 °C ≥ Tagwert → Tagwert 100 %
            Assert.Equal(0.1, woche[Kalenderwoche.Stelle(0, 2)]);      // Nacht: 18 °C < 20 °C → Nachtwert 10 %
        }

        // =============================================================================
        //  Abdruck, Herkunft, Satz aus dem Speicher
        // =============================================================================

        [Fact]
        public void Der_Abdruck_folgt_jeder_Aenderung_und_nur_ihr()
        {
            Konditionierungsarbeitsstand a = Stand(Zone(-1, "Anbau"));
            Assert.Equal(Konditionierungsarbeit.Abdruck(a), Konditionierungsarbeit.Abdruck(Stand(Zone(-1, "Anbau"))));
            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Geraete, -1), DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5)));
            Assert.NotEqual(Konditionierungsarbeit.Abdruck(a), Konditionierungsarbeit.Abdruck(b));
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.ZelleSetzen(
                b, Ort(Konditionierungsgroesse.Geraete, -1), DbWerte.KOND_ZEILE_TAG, Matrixzelle.Leer));
            Assert.Equal(Konditionierungsarbeit.Abdruck(a), Konditionierungsarbeit.Abdruck(c));
        }

        [Fact]
        public void Die_Herkunft_liest_sich_aus_der_Bemerkung_in_beiden_Sprachen()
        {
            var h = new Kalenderherkunft("Büro", "Zeitfenster Mo, 6–8 Uhr = 22");
            Assert.Equal("aus Vorlage Büro · Zeitfenster Mo, 6–8 Uhr = 22", h.Bemerkung());
            Assert.Equal(h, Kalenderherkunft.AusBemerkung(h.Bemerkung()));
            Assert.Equal(new Kalenderherkunft("Büro", null), Kalenderherkunft.AusBemerkung("from template Büro"));
            Assert.Equal(new Kalenderherkunft(null, "Feiertage als Regel (9 von 9 angelegt)"),
                         Kalenderherkunft.AusBemerkung("Feiertage als Regel (9 von 9 angelegt)"));
            Assert.True(Kalenderherkunft.AusBemerkung(null).IstLeer);
        }

        [Fact]
        public void Der_Satz_aus_dem_Speicher_ist_leer_ohne_Angabe_und_rechnet_die_Heizperiode()
        {
            Konditionierungsarbeitsstand a = Stand();
            Assert.Null(Konditionierungdatenweg.Satz(a, null, Vdi6007Probe.Wochenende(), 2025, false, false));

            Konditionierungsarbeitsstand b = Gut(Konditionierungsarbeit.ZelleSetzen(
                a, Ort(Konditionierungsgroesse.Heizsoll), DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(274, 120)));
            Konditionierungssatz satz = Konditionierungdatenweg.Satz(b, null, Vdi6007Probe.Wochenende(), 2025, false, false);
            Assert.NotNull(satz);
            Assert.Equal(153 * 24, satz.StundenOhneHeizung());

            // Angelegt statt abgeleitet: dieselbe Reihe.
            Konditionierungsarbeitsstand c = Gut(Konditionierungsarbeit.Anlegen(b, Ort(Konditionierungsgroesse.Heizsoll)));
            Konditionierungssatz angelegt = Konditionierungdatenweg.Satz(c, null, Vdi6007Probe.Wochenende(), 2025, false, false);
            Assert.Equal(satz.Reihe(Konditionierungsgroesse.Heizsoll), angelegt.Reihe(Konditionierungsgroesse.Heizsoll));

            // Die Zone ohne eigene Zelle nimmt den Kalender des Gebäudes.
            Konditionierungsarbeitsstand d = new Konditionierungsarbeitsstand(c.Gebaeude, new[] { Zone(-1, "Anbau") }, 201.0, 2025);
            Konditionierungssatz zone = Konditionierungdatenweg.Satz(d, -1, Vdi6007Probe.Wochenende(), 2025, false, false);
            Assert.Equal(satz.Reihe(Konditionierungsgroesse.Heizsoll), zone.Reihe(Konditionierungsgroesse.Heizsoll));
        }
    }
}
