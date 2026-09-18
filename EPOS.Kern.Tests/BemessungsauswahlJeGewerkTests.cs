using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bemessungsauswahl folgt an ALLEN ZEHN Gewerken der Bezugsgröße</b> —
    /// Auftrag #287, Anwenderentscheid vom 15.09.2026 („Bemessungsarten-Filterung auf die
    /// übrigen neun Gewerke ausweiten: ok").
    ///
    /// <para><b>Was hier bewiesen wird.</b> Je Gewerk steht die Auswahlliste beider Raster
    /// Wort für Wort da — was angeboten wird UND was nicht. Die Liste ist keine Meinung:
    /// Sie fällt aus <see cref="WirtschaftlichkeitCtrl.BasisGrund"/>, derselben Landkarte,
    /// aus der der Dialog seinen Grundtext holt. Eine Art, die an ihrem Gewerk keine
    /// Bezugsgröße führt, ergäbe über den Anwenderentscheid I-2 den erfassten Betrag — bei
    /// einer reinen Satzzeile also 0, und zwar ohne Warnung.</para>
    ///
    /// <para><b>Vor dem Scharfschalten gemessen.</b> Kein Gewerk verliert seine Auswahl.
    /// Am dünnsten bleibt das BETRIEBSRASTER der vier Gewerke ohne Geräte- und
    /// Laufgrößen — Pufferspeicher, Wärmezentrale, Bauliche Anlagen, Stromeinspeisung —:
    /// fester Jahresbetrag und „% der Investition". Beide tragen dort wirklich, die zweite
    /// über die Investitionskaskade; es bleibt also nicht die Pauschale allein.</para>
    ///
    /// <para><b>Der Bestand ist ausgezählt worden</b> (Auslieferungsvorlagen und
    /// <c>Tab_ProjektWerte</c> der Messlatte): Genau EINE Zeile trug eine Art, die an
    /// ihrem Gewerk keine Bezugsgröße führt — die PV-Vorlagenposition „Batteriespeicher"
    /// mit „je kWh Kapazität". Sie ist eigens umgestellt worden (Schemaschritt 78,
    /// <c>PvVorlageBatteriespeicher</c>, Nachweis in
    /// <c>PvBatteriespeicherBemessungTests</c>). Keine Projektzeile ist betroffen.</para>
    ///
    /// <para><b>Die Leistungsarten stehen in BEIDEN Rastern.</b> „je kW Leistung",
    /// „je kW Heizleistung" und „je kW elektrisch" werden auch im Betriebsraster
    /// angeboten — Wartung und Instandhaltung werden branchenüblich je installierter
    /// Leistung bemessen. Gewachsen ist die Auswahl allein an den SIEBEN Gewerken mit
    /// einer Leistungsgröße; Wärmezentrale, Bauliche Anlagen und Stromeinspeisung
    /// behalten ihre Kostenwelt, weil kein Gerät hinter ihnen steht.</para>
    ///
    /// <para>Der Schutz für den Bestand bleibt davon unberührt: Eine Art, die eine
    /// VORHANDENE Zeile trägt, steht weiter in der Liste — sonst verlöre ein gepflegter
    /// Wert seine Auswahl und ließe sich nicht mehr ändern.</para>
    /// </summary>
    public class BemessungsauswahlJeGewerkTests
    {
        private const int K_WAERMEPUMPE = 1;
        private const int K_HEIZKESSEL = 2;
        private const int K_PHOTOVOLTAIK = 3;
        private const int K_SOLARTHERMIE = 4;
        private const int K_STROMSPEICHER = 5;
        private const int K_PUFFERSPEICHER = 6;
        private const int K_BHKW = 7;
        private const int K_WAERMEZENTRALE = 8;
        private const int K_BAULICHE_ANLAGEN = 9;
        private const int K_STROMEINSPEISUNG = 10;

        /// <summary>Alle zehn Kostenkomponenten (<c>Tab_KostenKomponente.ID</c>).</summary>
        private static readonly int[] ALLE_GEWERKE = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // =====================================================================
        //  Je Gewerk: was angeboten wird und was nicht
        // =====================================================================

        /// <summary>Wärmepumpe: die EINE Nennleistung — „je kW Leistung" und „je kW
        /// Heizleistung" meinen sie beide, und zwar in BEIDEN Rastern (Wartung je kW).
        /// Strom- und Flächengrößen kennt sie nicht; ihre Mengen kommen aus dem
        /// Lauf.</summary>
        [Fact]
        public void Waermepumpe_bietet_die_Heizleistung_und_die_Laufgroessen()
        {
            Pruefe(K_WAERMEPUMPE,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,
                    DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                });
        }

        /// <summary>Heizkessel: nur <c>Ptherm</c>, also die thermische Leistung unter
        /// beiden Namen. Strom erzeugt er nicht — „je kWh elektrisch" fällt im Betrieb
        /// heraus.</summary>
        [Fact]
        public void Heizkessel_bietet_die_thermische_Leistung_und_keinen_Strom()
        {
            Pruefe(K_HEIZKESSEL,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,
                    DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                });
        }

        /// <summary>Photovoltaik: die installierte Leistung, unter beiden Namen („je kWp"
        /// und „je kW elektrisch" sind DIESELBE Zahl). Eine kWh-Kapazität führt sie
        /// nicht — genau daran hing die Vorlagenposition „Batteriespeicher".
        /// <para>U33: „je kWp Leistung" steht auch im BETRIEBSRASTER — die
        /// branchenübliche Wartungskennzahl €/kWp·a. Nur hier: Kein anderes Gewerk
        /// führt eine kWp-Größe, und die Auswahl fragt die Landkarte, nicht eine
        /// zweite Liste.</para></summary>
        [Fact]
        public void Photovoltaik_bietet_die_installierte_Leistung_und_keine_Kapazitaet()
        {
            Pruefe(K_PHOTOVOLTAIK,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KWP,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KWP,
                });
        }

        /// <summary>
        /// U33: Die Einheit des Satzes folgt dem RASTER. Auf der Investitionsseite ist
        /// „je kWp Leistung" ein €/kWp-Satz, auf der Betriebsseite ein JAHRESsatz —
        /// 12,00 €/kWp·a × 300,00 kWp = 3.600,00 €/a. Ohne das Jahr im Zeichen stünde
        /// hinter dem Satz dieselbe Einheit wie bei einer einmaligen Investition.
        /// </summary>
        [Fact]
        public void Je_kWp_traegt_im_Betriebsraster_die_Jahreseinheit()
        {
            Assert.Equal("€/kWp",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KWP, K_PHOTOVOLTAIK, false));
            Assert.Equal("€/kWp·a",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KWP, K_PHOTOVOLTAIK, true));

            // Gegenprobe: Die Mengenarten tragen ihr Jahr in der BEZUGSGRÖSSE [kWh/a]
            // und heißen deshalb in beiden Rastern gleich.
            Assert.Equal("€/kWh",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, K_PHOTOVOLTAIK, true));
        }

        /// <summary>
        /// U33, die Zahlenprobe des Mockups (Abschnitt 3): Der Rechenweg der
        /// Betriebskosten kennt die Art längst — Menge × Satz. Eine neue Formel gibt es
        /// nicht, und deshalb ändert sich auch keine bestehende Zeile.
        /// </summary>
        [Fact]
        public void Ein_kWp_Satz_im_Betriebsraster_rechnet_Satz_mal_kWp()
        {
            double betrag = BetriebskostenCtrl.Betrag(
                DbWerte.BEMESSUNG_EUR_PRO_KWP, 0.0, 300.0, 12.0, false);

            Assert.Equal(3600.00, betrag, 2);
        }

        /// <summary>Solarthermie: die Kollektorfläche und die daraus GERECHNETE thermische
        /// Leistung (0,7 kW/m²). Strom erzeugt ein Kollektor nicht.</summary>
        [Fact]
        public void Solarthermie_bietet_Flaeche_und_thermische_Leistung()
        {
            Pruefe(K_SOLARTHERMIE,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                });
        }

        /// <summary>Stromspeicher: Kapazität UND Leistung — seine Leistung ist elektrisch,
        /// deshalb gilt sie unter beiden Namen. Wärme liefert er nicht.</summary>
        [Fact]
        public void Stromspeicher_bietet_Kapazitaet_und_elektrische_Leistung()
        {
            Pruefe(K_STROMSPEICHER,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                });
        }

        /// <summary>Pufferspeicher: allein das Volumen („je kW Leistung" heißt dort „je
        /// Liter"). Eine kWh-Kapazität lässt sich ohne Temperaturpaar nicht bilden.</summary>
        [Fact]
        public void Pufferspeicher_bietet_allein_das_Volumen()
        {
            Pruefe(K_PUFFERSPEICHER,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                });
        }

        /// <summary>BHKW: das Gewerk mit den meisten Größen — <c>Pel</c> und
        /// <c>Ptherm</c>, dazu beide Energieseiten aus dem Lauf. „je kW Leistung" meint
        /// dort die ELEKTRISCHE Leistung.</summary>
        [Fact]
        public void Bhkw_bietet_beide_Leistungen_und_beide_Energieseiten()
        {
            Pruefe(K_BHKW,
                new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                },
                new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,
                    DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                });
        }

        /// <summary>Wärmezentrale: kein Gerät, kein Lauf — die Kostenwelt allein.</summary>
        [Fact]
        public void Waermezentrale_bietet_die_Kostenwelt()
        {
            Pruefe(K_WAERMEZENTRALE, KostenweltInvest(), KostenweltBetrieb());
        }

        /// <summary>Bauliche Anlagen: dasselbe Bild — hier steht kein Gerät hinter der
        /// Position, an dem sich etwas bemessen ließe.</summary>
        [Fact]
        public void Bauliche_Anlagen_bieten_die_Kostenwelt()
        {
            Pruefe(K_BAULICHE_ANLAGEN, KostenweltInvest(), KostenweltBetrieb());
        }

        /// <summary>Stromeinspeisung: dasselbe Bild.</summary>
        [Fact]
        public void Stromeinspeisung_bietet_die_Kostenwelt()
        {
            Pruefe(K_STROMEINSPEISUNG, KostenweltInvest(), KostenweltBetrieb());
        }

        // =====================================================================
        //  Die Zusagen, die über allen zehn Gewerken stehen
        // =====================================================================

        /// <summary>Der Geltungsbereich umfasst alle zehn Gewerke — und nur sie.</summary>
        [Fact]
        public void Der_Geltungsbereich_umfasst_alle_zehn_Gewerke()
        {
            foreach (int k in ALLE_GEWERKE)
                Assert.True(BemessungKatalog.AuswahlWirdGefiltert(k),
                            "Gewerk " + k + " fehlt im Geltungsbereich.");

            // 0 = Gewerk unbekannt, 11 = kein Gewerk der Datenbank: nicht gefiltert.
            Assert.False(BemessungKatalog.AuswahlWirdGefiltert(0));
            Assert.False(BemessungKatalog.AuswahlWirdGefiltert(11));
        }

        /// <summary>
        /// KEIN GEWERK WIRD LEERGERÄUMT, und keines behält nur die Pauschale: Jede der
        /// zwanzig Listen (zehn Gewerke × zwei Raster) trägt mindestens zwei Arten, und
        /// mindestens eine davon ist keine absolute.
        /// </summary>
        [Fact]
        public void Kein_Gewerk_bleibt_ohne_Auswahl()
        {
            foreach (int k in ALLE_GEWERKE)
                foreach (bool invest in new[] { true, false })
                {
                    List<BemessungKatalog.Info> liste = BemessungKatalog.Auswahl(k, invest, null);
                    Assert.True(liste.Count >= 2,
                                "Gewerk " + k + (invest ? " (Invest)" : " (Betrieb)") +
                                " bietet nur " + liste.Count + " Art(en).");

                    bool bemessen = false;
                    foreach (BemessungKatalog.Info i in liste) if (!i.Absolut) bemessen = true;
                    Assert.True(bemessen,
                                "Gewerk " + k + (invest ? " (Invest)" : " (Betrieb)") +
                                " behielte nur die Pauschale.");
                }
        }

        /// <summary>
        /// DER SCHUTZ DES BESTANDS, an JEDEM Gewerk: Trägt eine vorhandene Zeile eine Art,
        /// die dort keine Bezugsgröße führt, bleibt die Art in der Liste — sonst verlöre
        /// die Zeile beim Anzeigen ihren Wert, und der Anwender könnte ihn nicht mehr
        /// ändern.
        /// </summary>
        [Fact]
        public void Eine_Art_mit_gepflegter_Zeile_bleibt_an_jedem_Gewerk_in_der_Liste()
        {
            foreach (int k in ALLE_GEWERKE)
                foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                {
                    if (BemessungKatalog.PasstZuGewerk(i.Persistenz, k)) continue;

                    var benutzt = new HashSet<string>(StringComparer.Ordinal) { i.Persistenz };
                    Assert.Contains(i.Persistenz, Persistenzwerte(k, true, benutzt));
                    Assert.Contains(i.Persistenz, Persistenzwerte(k, false, benutzt));
                }
        }

        /// <summary>
        /// Auch die beiden ALTWERTE des Katalogs („je kWh", „je Stunde") bleiben über
        /// diesen Weg erreichbar: Sie stehen in keinem Raster, aber eine Bestandszeile,
        /// die einen von ihnen trägt, behält ihn.
        /// </summary>
        [Fact]
        public void Die_Altwerte_bleiben_ueber_den_Bestand_erreichbar()
        {
            var benutzt = new HashSet<string>(StringComparer.Ordinal)
                { DbWerte.BEMESSUNG_EUR_PRO_KWH, DbWerte.BEMESSUNG_EUR_PRO_H };

            foreach (int k in ALLE_GEWERKE)
            {
                Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KWH, Persistenzwerte(k, true, benutzt));
                Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_H, Persistenzwerte(k, false, benutzt));
            }
        }

        /// <summary>Ohne bekanntes Gewerk (0) wird nicht gefiltert — die Liste ist dann
        /// die vollständige Rasterliste des Katalogs.</summary>
        [Fact]
        public void Ohne_Gewerk_bleibt_die_volle_Rasterliste()
        {
            Assert.Equal(Rasterliste(true), Persistenzwerte(0, true));
            Assert.Equal(Rasterliste(false), Persistenzwerte(0, false));
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        /// <summary>Die Arten der reinen Kostenwelt im Investitionsraster — sie brauchen
        /// keine Baugröße: der feste Betrag und die beiden Prozentarten, deren Basis ein
        /// Eurobetrag der Investseite ist.</summary>
        private static string[] KostenweltInvest()
        {
            return new[]
            {
                DbWerte.BEMESSUNG_BETRAG,
                DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
            };
        }

        /// <summary>Dieselbe Kostenwelt im Betriebsraster — der feste Jahresbetrag und
        /// „% der Investition". Das ist die DÜNNSTE Liste überhaupt, und sie trägt.</summary>
        private static string[] KostenweltBetrieb()
        {
            return new[]
            {
                DbWerte.BEMESSUNG_JAHRESBETRAG,
                DbWerte.BEMESSUNG_PROZENT_INVESTITION,
            };
        }

        /// <summary>
        /// Hält beide Raster eines Gewerks Wort für Wort gegen die erwartete Liste — und
        /// prüft dazu, dass JEDE nicht genannte Art des Rasters auch wirklich fehlt und
        /// dass ihr Fehlen aus <see cref="WirtschaftlichkeitCtrl.BasisGrund"/> stammt.
        /// </summary>
        private static void Pruefe(int komponentenId, string[] investErwartet,
                                   string[] betriebErwartet)
        {
            Assert.True(BemessungKatalog.AuswahlWirdGefiltert(komponentenId));

            Assert.Equal(investErwartet, Persistenzwerte(komponentenId, true));
            Assert.Equal(betriebErwartet, Persistenzwerte(komponentenId, false));

            PruefeFehlende(komponentenId, true, investErwartet);
            PruefeFehlende(komponentenId, false, betriebErwartet);
        }

        /// <summary>Was NICHT angeboten wird: jede Art des Rasters, die nicht in der
        /// erwarteten Liste steht, fehlt — und zwar mit dem Grund
        /// <c>BASISGRUND_GEWERK</c>, nicht aus Versehen.</summary>
        private static void PruefeFehlende(int komponentenId, bool invest, string[] erwartet)
        {
            var genannt = new HashSet<string>(erwartet, StringComparer.Ordinal);
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
            {
                if (!(invest ? i.FuerInvest : i.FuerBetrieb)) continue;
                if (genannt.Contains(i.Persistenz)) continue;

                Assert.DoesNotContain(i.Persistenz, Persistenzwerte(komponentenId, invest));
                Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                             WirtschaftlichkeitCtrl.BasisGrund(i.Persistenz, komponentenId));
            }
        }

        private static string[] Persistenzwerte(int komponentenId, bool invest,
                                                ICollection<string> benutzt = null)
        {
            List<BemessungKatalog.Info> liste =
                BemessungKatalog.Auswahl(komponentenId, invest, benutzt);
            var werte = new string[liste.Count];
            for (int n = 0; n < liste.Count; n++) werte[n] = liste[n].Persistenz;
            return werte;
        }

        /// <summary>Die ungefilterte Rasterliste des Katalogs.</summary>
        private static string[] Rasterliste(bool invest)
        {
            var werte = new List<string>();
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                if (invest ? i.FuerInvest : i.FuerBetrieb) werte.Add(i.Persistenz);
            return werte.ToArray();
        }
    }
}
