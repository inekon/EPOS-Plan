using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6a, Welle 1 — die Zonenliste ohne Datenbank:</b> die Prüfregeln über die ganze
    /// Liste (Höchstzahl samt Freigabeschalter, Nutzfläche ab zwei Zonen, doppelte Ids), die Hinweise
    /// (Flächensumme, Zone ohne Außenbauteil), die EINE Formel der Kennwerte (<see cref="Zonenkennwerte"/>)
    /// — bei einer Zone bitgleich zur Anzeige vor G6a — und der Arbeitsstand über Ids (neue Ids unter
    /// allen vergebenen, Ersetzen, Entfernen, Duplizieren, Umordnen).
    /// </summary>
    public class GebaeudeZonenlistePruefregelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Eine gültige Zone mit eigener Nutzfläche und einer Außenwand.</summary>
        internal static ZoneModel Zone(int id, string name, double? flaeche = 50.0, int bauteilId = -1)
            => new ZoneModel
            {
                ID = id,
                Bezeichner = name,
                Nutzflaeche = flaeche,
                Bauteile =
                {
                    new BauteilModel { ID = bauteilId, Bezeichner = "Wand " + name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                       Flaeche = 20, Azimut = 180, U_Wert = 0.3 }
                }
            };

        // =================================================================================
        //  Pruefen ueber die ganze Liste
        // =================================================================================

        [Fact]
        public void Hoechstens_fuenfzig_Zonen_und_der_Lauf_rechnet_sie_alle()
        {
            Assert.Equal(50, GebaeudeZonenregeln.PFLEGEGRENZE);
            Assert.Equal(50, GebaeudeZonenregeln.Hoechstzahl());
            Assert.True(GebaeudeZonenregeln.Rechenbar(1));
            Assert.True(GebaeudeZonenregeln.Rechenbar(2));
            Assert.True(GebaeudeZonenregeln.Rechenbar(50));
            Assert.False(GebaeudeZonenregeln.Rechenbar(51));

            List<ZoneModel> fuenfzig = Enumerable.Range(1, 50).Select(i => Zone(-i, "Zone " + i)).ToList();
            Assert.Null(GebaeudeZonenCtrl.Pruefen(fuenfzig));

            List<ZoneModel> einundfuenfzig = Enumerable.Range(1, 51).Select(i => Zone(-i, "Zone " + i)).ToList();
            Assert.Equal(string.Format(R.ZONE_MSG_ZU_VIELE, 50, 51), GebaeudeZonenCtrl.Pruefen(einundfuenfzig));
        }

        [Fact]
        public void Ab_zwei_Zonen_ist_die_Nutzflaeche_Pflicht()
        {
            // Eine Zone ohne Fläche heißt „die Fläche des Gebäudes" - wie bisher gültig.
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { Zone(-1, "Allein", null) }));

            Assert.Equal(string.Format(R.ZONE_MSG_NUTZFLAECHE_PFLICHT, "Anbau"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { Zone(-1, "Haus"), Zone(-2, "Anbau", null) }));
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { Zone(-1, "Haus"), Zone(-2, "Anbau", 12.5) }));
        }

        [Fact]
        public void Eine_doppelte_positive_Id_ist_ein_Fehler_eine_vorlaeufige_nicht()
        {
            Assert.Equal(string.Format(R.ZONE_MSG_ID_DOPPELT, "B", 7),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { Zone(7, "A", bauteilId: 1), Zone(7, "B", bauteilId: 2) }));
            Assert.Equal(string.Format(R.BAUTEIL_MSG_ID_DOPPELT, "Wand B", 4, "B"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { Zone(1, "A", bauteilId: 4), Zone(2, "B", bauteilId: 4) }));

            // Vorläufige Ids dürfen sich wiederholen: Sie werden angelegt, nicht geändert.
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { Zone(-1, "A", bauteilId: -1), Zone(-1, "B", bauteilId: -1) }));
        }

        [Fact]
        public void Die_Hinweise_nennen_Flaechensumme_und_Zone_ohne_Aussenbauteil()
        {
            var innen = new ZoneModel
            {
                ID = -3, Bezeichner = "Flur", Nutzflaeche = 20,
                Bauteile = { new BauteilModel { ID = -1, Bezeichner = "Trennwand", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 10 } }
            };
            var keller = new ZoneModel
            {
                ID = -4, Bezeichner = "Keller", Nutzflaeche = 20,
                Bauteile = { new BauteilModel { ID = -1, Bezeichner = "Boden", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Flaeche = 20,
                                                Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH } }
            };

            // 60 + 20 + 20 = 100 gegen 100: kein Flächenhinweis; der Flur hat kein Außenbauteil, der Keller schon (Erdreich).
            IReadOnlyList<string> h = GebaeudeZonenCtrl.Hinweise(new List<ZoneModel> { Zone(-1, "Haus", 60), innen, keller }, 100);
            Assert.Equal(new[] { string.Format(R.ZONE_HINWEIS_OHNE_AUSSEN, "Flur") }, h);

            // 60 + 60 gegen 100: +20 % - benannt mit beiden Summen.
            h = GebaeudeZonenCtrl.Hinweise(new List<ZoneModel> { Zone(-1, "A", 60), Zone(-2, "B", 60) }, 100);
            Assert.Equal(string.Format(R.ZONE_HINWEIS_FLAECHE, "120", "100", "+20"), Assert.Single(h));

            // Genau 5 % zählt, knapp darunter nicht; eine einzelne (hochgerechnete) Zone nie.
            Assert.Single(GebaeudeZonenCtrl.Hinweise(new List<ZoneModel> { Zone(-1, "A", 50), Zone(-2, "B", 45) }, 100));
            Assert.Empty(GebaeudeZonenCtrl.Hinweise(new List<ZoneModel> { Zone(-1, "A", 50), Zone(-2, "B", 45.5) }, 100));
            Assert.Empty(GebaeudeZonenCtrl.Hinweise(new List<ZoneModel> { Zone(-1, "A", 150) }, 100));
            Assert.Empty(GebaeudeZonenCtrl.Hinweise(new List<ZoneModel>(), 100));
        }

        // =================================================================================
        //  Zonenkennwerte - die EINE Formel
        // =================================================================================

        private static GebaeudeKatalogDaten Satz(string modell) => new()
        {
            Name = "Haus",
            WohnflaecheGesamt = 143.7,
            Raumhoehe = 2.63,
            Luftwechselrate = 0.55,
            LuftwechselInfiltration = modell == DbWerte.GEBAEUDE_MODELL_VDI6007 ? 0.17 : null,
            Modell = modell
        };

        private static ZoneDaten Datenzone(double? flaeche) => new()
        {
            Id = 5,
            Bezeichner = "Haus",
            Nutzflaeche = flaeche,
            Bauteile =
            {
                new BauteilDaten { Id = 1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 123.7, UWert = 0.283, Azimut = 180, PsiL = 1.37 },
                new BauteilDaten { Id = 2, Bezeichner = "Fenster", Bauteilart = DbWerte.BAUTEILART_FENSTER, Flaeche = 17.3, UWert = 1.13, Azimut = 90, PsiL = 0.71 },
                new BauteilDaten { Id = 3, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 81.1, UAufbau = 0.197 },
                new BauteilDaten { Id = 4, Bezeichner = "Innen", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 40, UWert = 1.9 },
                new BauteilDaten { Id = 5, Bezeichner = "Boden", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE, Flaeche = 81.1, UWert = 0.31,
                                   Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH, PsiL = 0.09 }
            }
        };

        private static void Bitgleich(double erwartet, double wert)
            => Assert.Equal(BitConverter.DoubleToInt64Bits(erwartet), BitConverter.DoubleToInt64Bits(wert));

        /// <summary>
        /// Bei EINER Zone rechnen <see cref="GebaeudeArbeitsstand.HTAnzeige"/> und
        /// <see cref="GebaeudeArbeitsstand.HVeAnzeige"/> bitgleich zur Formel vor G6a: H_T = Σ U·A der
        /// Zonengruppen + Σ ψ·L, H_ve = n · A_Zone (sonst A_Gebäude) · H_Gebäude · 0,34.
        /// </summary>
        [Theory]
        [InlineData(DbWerte.GEBAEUDE_MODELL_VDI6007, 97.3)]
        [InlineData(DbWerte.GEBAEUDE_MODELL_VDI6007, null)]
        [InlineData(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, 97.3)]
        [InlineData(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, null)]
        public void Eine_Zone_rechnet_bitgleich_zur_Anzeige_vor_G6a(string modell, double? flaeche)
        {
            GebaeudeKatalogDaten satz = Satz(modell);
            ZoneDaten zone = Datenzone(flaeche);
            var a = new GebaeudeArbeitsstand();
            a.Laden(satz, false);
            a.ZonenLaden(new[] { zone }, true);

            // Die Formel vor G6a, abgeschrieben.
            double htAlt = Gebaeudehuellbilanz.TransmissionWK(Gebaeudehuellbilanz.Zonenzeilen(
                               zone.Bauteile.Select(b => (b.Bauteilart, b.Randbedingung!, b.Flaeche ?? 0.0, b.UWirksam))))
                           + zone.Bauteile.Sum(b => b.PsiL ?? 0.0);
            double n = Gebaeuderechenweg.IstVdi6007(modell)
                ? Gebaeudemodellvorgaben.WirksamerLuftwechsel(satz.Luftwechselrate, satz.LuftwechselInfiltration, satz.LuftwechselNutzer)
                : satz.Luftwechselrate!.Value;
            double hveAlt = Gebaeudehuellbilanz.LueftungWK(n, zone.Nutzflaeche ?? satz.WohnflaecheGesamt, satz.Raumhoehe);

            Bitgleich(htAlt, a.HTAnzeige);
            Bitgleich(hveAlt, a.HVeAnzeige);
            Bitgleich(htAlt, Zonensummen.HT(zone));
            Assert.Equal(flaeche ?? satz.WohnflaecheGesamt, a.NutzflaecheWirksam);

            Zonenkennwerte k = Assert.Single(a.Kennwerte);
            Bitgleich(htAlt, k.HT);
            Bitgleich(hveAlt, k.HVe);
            Assert.Equal(5, k.Bauteile);
            Assert.Equal(!flaeche.HasValue, k.FlaecheVomGebaeude);
            Assert.True(k.VolumenAbgeleitet);
            Bitgleich((flaeche ?? satz.WohnflaecheGesamt!.Value) * satz.Raumhoehe!.Value, k.Volumen!.Value);
        }

        [Fact]
        public void Mehrere_Zonen_summieren_Flaeche_HT_und_HVe()
        {
            var a = new GebaeudeArbeitsstand();
            a.Laden(Satz(DbWerte.GEBAEUDE_MODELL_VDI6007), false);
            ZoneDaten eins = Datenzone(60), zwei = Datenzone(40);
            zwei.Id = 6;
            a.ZonenLaden(new[] { eins, zwei }, true);

            Assert.Equal(100.0, a.NutzflaecheWirksam);
            Assert.Equal(a.Kennwerte[0].HT + a.Kennwerte[1].HT, a.HTAnzeige, 12);
            Assert.Equal(a.Kennwerte[0].HVe + a.Kennwerte[1].HVe, a.HVeAnzeige, 12);
            Assert.Equal(2 * Zonensummen.HT(eins), a.HTAnzeige, 9);
        }

        [Fact]
        public void Das_Volumen_ist_das_der_Zone_sonst_abgeleitet()
        {
            var bauteile = new[] { new Zonenbauteil(DbWerte.BAUTEILART_AUSSENWAND, null!, 10, 0.5, null) };
            Zonenkennwerte eigen = Zonenkennwerte.Bilden(40, 3.0, 150, bauteile, 100, 2.5, 0.5);
            Assert.Equal(150, eigen.Volumen);
            Assert.False(eigen.VolumenAbgeleitet);
            Assert.False(eigen.FlaecheVomGebaeude);

            Zonenkennwerte hoehe = Zonenkennwerte.Bilden(40, 3.0, null, bauteile, 100, 2.5, 0.5);
            Assert.Equal(120, hoehe.Volumen!.Value, 12);
            Assert.True(hoehe.VolumenAbgeleitet);
            // H_ve nach der Regel des Laufs: Fläche der Zone × Raumhöhe des GEBÄUDES.
            Assert.Equal(Gebaeudehuellbilanz.LueftungWK(0.5, 40, 2.5), hoehe.HVe);

            Zonenkennwerte gebaeude = Zonenkennwerte.Bilden(null, null, null, bauteile, 100, 2.5, 0.5);
            Assert.Equal(100, gebaeude.Nutzflaeche);
            Assert.True(gebaeude.FlaecheVomGebaeude);
            Assert.Equal(250, gebaeude.Volumen!.Value, 12);
            Assert.Equal(5.0, gebaeude.HT, 12);
        }

        [Fact]
        public void Eine_gespeicherte_Zone_nimmt_U_aus_dem_Aufbau()
        {
            var zone = new ZoneModel
            {
                ID = 1, Bezeichner = "Z", Nutzflaeche = 30,
                Bauteile =
                {
                    new BauteilModel { ID = 1, Bezeichner = "W", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 10, ID_Aufbau = 9 },
                    new BauteilModel { ID = 2, Bezeichner = "D", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 10, U_Wert = 0.2, ID_Aufbau = 9, Psi_L = 1.5 }
                }
            };
            Zonenkennwerte k = Zonenkennwerte.Bilden(zone, id => id == 9 ? 0.4 : null, 100, 2.5,
                                                     Zonenkennwerte.Luftwechsel(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, 0.6, null, null));
            Assert.Equal(10 * 0.4 + 10 * 0.2 + 1.5, k.HT, 12);
            Assert.Equal(Gebaeudehuellbilanz.LueftungWK(0.6, 30, 2.5), k.HVe);
            Assert.Equal(2, k.Bauteile);
            Assert.Equal(Gebaeudemodellvorgaben.WirksamerLuftwechsel(0.6, null, null),
                         Zonenkennwerte.Luftwechsel(DbWerte.GEBAEUDE_MODELL_VDI6007, 0.6, null, null));
        }

        // =================================================================================
        //  Der Arbeitsstand ueber Ids
        // =================================================================================

        private static GebaeudeArbeitsstand Arbeitsstand(params ZoneDaten[] zonen)
        {
            var a = new GebaeudeArbeitsstand();
            a.Laden(Satz(DbWerte.GEBAEUDE_MODELL_VDI6007), false);
            a.ZonenLaden(zonen, true);
            return a;
        }

        private static ZoneDaten Z(int id, string name) => new() { Id = id, Bezeichner = name, Nutzflaeche = 10 };

        [Fact]
        public void Neue_Ids_liegen_unter_allen_vergebenen()
        {
            GebaeudeArbeitsstand a = Arbeitsstand();

            // Die Übernahmezone trägt -1; eine neue Zone bekommt -2, auch wenn die Übernahme wieder fällt.
            ZoneDaten uebernahme = a.ZoneAnlegen(Z(-1, "Übernahme"));
            Assert.Equal(-1, uebernahme.Id);
            Assert.True(a.ZoneEntfernen(-1));
            ZoneDaten neu = a.NeueZone("Neu");
            Assert.Equal(-2, neu.Id);
            a.ZoneAnlegen(neu);
            Assert.Equal(-3, a.NeueZone("Zwei").Id);

            // Eine Zone mit schon vergebener Id bekommt eine neue.
            ZoneDaten doppelt = a.ZoneAnlegen(Z(-2, "Doppelt"));
            Assert.Equal(-4, doppelt.Id);
            Assert.Equal(new[] { -2, -4 }, a.Zonen.Select(z => z.Id));

            // Geladene Zonen mit vorläufigen Ids schieben die Grenze.
            GebaeudeArbeitsstand b = Arbeitsstand(Z(12, "A"), Z(-7, "B"));
            Assert.Equal(-8, b.NeueZone("C").Id);
        }

        [Fact]
        public void Ersetzen_trifft_nur_die_Zone_gleicher_Id()
        {
            GebaeudeArbeitsstand a = Arbeitsstand(Z(11, "Eins"), Z(12, "Zwei"), Z(13, "Drei"));
            ZoneDaten zwei = a.ZoneMitId(12)!.Kopie();
            zwei.Bezeichner = "Zwei neu";
            zwei.Bauteile.Add(new BauteilDaten { Id = -1, Bezeichner = "Wand", Flaeche = 5 });

            Assert.True(a.ZoneErsetzen(zwei));
            Assert.Equal(new[] { "Eins", "Zwei neu", "Drei" }, a.Zonen.Select(z => z.Bezeichner));
            Assert.Equal(new[] { 11, 12, 13 }, a.Zonen.Select(z => z.Id));
            Assert.True(a.ZonenGeaendert);
            Assert.False(a.ZoneErsetzen(Z(99, "fremd")));

            Assert.True(a.ZoneEntfernen(12));
            Assert.Equal(new[] { 11, 13 }, a.Zonen.Select(z => z.Id));
            Assert.False(a.ZoneEntfernen(12));
        }

        [Fact]
        public void Duplizieren_gibt_neue_Ids_und_nennt_die_Vorlage()
        {
            ZoneDaten quelle = Z(21, "Büro");
            quelle.Bauteile.Add(new BauteilDaten { Id = 5, Bezeichner = "Wand", Flaeche = 5, Herkunft = DbWerte.HERKUNFT_IFC, Quellkennung = "wand-1" });
            quelle.Bauteile.Add(new BauteilDaten { Id = 6, Bezeichner = "Fenster", Flaeche = 2, Herkunft = DbWerte.HERKUNFT_IFC, Quellkennung = "fen-1" });
            GebaeudeArbeitsstand a = Arbeitsstand(quelle, Z(22, "Lager"));

            ZoneDaten kopie = a.ZoneDuplizieren(21, "Büro (Kopie)")!;
            Assert.Equal(new[] { 21, kopie.Id, 22 }, a.Zonen.Select(z => z.Id));
            Assert.True(kopie.Id < -1);
            Assert.Equal(21, kopie.VorlageId);
            Assert.Equal("Büro (Kopie)", kopie.Bezeichner);
            Assert.Equal(10, kopie.Nutzflaeche);
            Assert.Equal(new[] { -1, -2 }, kopie.Bauteile.Select(b => b.Id));
            Assert.All(kopie.Bauteile, b => Assert.Equal(DbWerte.HERKUNFT_MANUELL, b.Herkunft));
            Assert.All(kopie.Bauteile, b => Assert.Null(b.Quellkennung));
            // Die Vorlage bleibt, wie sie war.
            Assert.Equal(new[] { "wand-1", "fen-1" }, a.ZoneMitId(21)!.Bauteile.Select(b => b.Quellkennung));

            // Ein Duplikat eines Duplikats nennt die ursprüngliche Vorlage.
            ZoneDaten enkel = a.ZoneDuplizieren(kopie.Id, "Enkel")!;
            Assert.Equal(21, enkel.VorlageId);
            Assert.True(enkel.Id < kopie.Id);
            Assert.Null(a.ZoneDuplizieren(999, "x"));
        }

        [Fact]
        public void Verschieben_tauscht_mit_dem_Nachbarn_und_haelt_am_Rand()
        {
            GebaeudeArbeitsstand a = Arbeitsstand(Z(1, "A"), Z(2, "B"), Z(3, "C"));
            Assert.False(a.ZoneVerschieben(1, -1));
            Assert.False(a.ZoneVerschieben(3, +1));
            Assert.False(a.ZonenGeaendert);
            Assert.True(a.ZoneVerschieben(3, -1));
            Assert.Equal(new[] { 1, 3, 2 }, a.Zonen.Select(z => z.Id));
            Assert.True(a.ZonenGeaendert);
            Assert.True(a.ZoneVerschieben(1, +1));
            Assert.Equal(new[] { 3, 1, 2 }, a.Zonen.Select(z => z.Id));
        }

        [Fact]
        public void Die_Huellwegzeile_nennt_die_Zahl_der_Zonen()
        {
            var t = new GebaeudeZonenTexte();
            ZoneDaten eins = Z(1, "A");
            eins.Bauteile.Add(new BauteilDaten { Id = 1, Bezeichner = "W", Flaeche = 1 });
            ZoneDaten zwei = Z(2, "B");
            zwei.Bauteile.Add(new BauteilDaten { Id = 2, Bezeichner = "W", Flaeche = 1 });
            zwei.Bauteile.Add(new BauteilDaten { Id = 3, Bezeichner = "D", Flaeche = 1 });

            Assert.Equal(string.Format(R.GEBZ_ZEILE_BAUTEILWEG, "A", "1"), Arbeitsstand(eins).Huellwegzeile(t));
            Assert.Equal(string.Format(R.GEBZ_ZEILE_BAUTEILWEG_ZONEN, "2", "3"), Arbeitsstand(eins, zwei).Huellwegzeile(t));
            Assert.Equal(R.GEBZ_ZEILE_KLASSENWEG, Arbeitsstand().Huellwegzeile(t));
        }
    }

    /// <summary>
    /// <b>Stufe G6a, Welle 1 — die Zonenliste über Hülle und Datenbank:</b> Umordnen mit
    /// lückenlosem Rang, Entfernen der mittleren Zone samt Bauteilen, Duplikat mit
    /// <see cref="ZoneDaten.VorlageId"/> (übernimmt die Spalten der Vorlage, nicht ihre Herkunft), die
    /// Regressionsprobe gegen die stille Löschung (drei Zonen, die zweite ersetzen → alle drei bleiben)
    /// und die Auskunft mit zwei Zonen, die sie je Zone rechnet (Stufe G6b).
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeZonenlisteDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;

        private static int IdZ => Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0].ID_Z;

        private static int Gebaeude => GebaeudeBedarfCtrl.TabGebaeudeId(IdZ);

        /// <summary>Drei Zonen mit Sollwerten und Kühlwerten; die erste aus einem Import (Herkunft, Quellkennung).</summary>
        private static List<ZoneModel> DreiZonen()
        {
            ZoneModel eins = GebaeudeZonenlistePruefregelTests.Zone(-1, "Erste", 60);
            eins.Raumsolltemperatur_Tag = 21.5;
            eins.Kuehl_Sollwert = 26;
            eins.Kuehlung_Aktiv = true;
            eins.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            eins.Herkunft = DbWerte.HERKUNFT_IFC;
            eins.Quellkennung = "raum-1";
            eins.Bauteile[0].Herkunft = DbWerte.HERKUNFT_IFC;
            eins.Bauteile[0].Quellkennung = "wand-1";
            ZoneModel zwei = GebaeudeZonenlistePruefregelTests.Zone(-2, "Mitte", 30);
            zwei.Raumsolltemperatur_Tag = 19;
            zwei.Bauteile.Add(new BauteilModel { ID = -2, Bezeichner = "Dach Mitte", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 30, U_Wert = 0.2 });
            ZoneModel drei = GebaeudeZonenlistePruefregelTests.Zone(-3, "Dritte", 20);
            return new List<ZoneModel> { eins, zwei, drei };
        }

        private static (GebaeudeZonenweg Weg, GebaeudeArbeitsstand Stand) Oeffnen()
        {
            GebaeudeZonenweg weg = GebaeudeKatalogHuelle.Zonenweg(PROJEKT, IdZ, Gebaeude);
            var a = new GebaeudeArbeitsstand();
            a.Laden(new GebaeudeKatalogDaten { WohnflaecheGesamt = 110, Raumhoehe = 2.5 }, false);
            a.ZonenLaden(weg.Zonen, true);
            return (weg, a);
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        /// <summary>
        /// <b>Die Regressionsprobe gegen die stille Löschung</b> (G6a, Risiko 2): drei Zonen in der
        /// Datenbank, die zweite geöffnet und ersetzt → alle drei bleiben, samt Schlüsseln und den
        /// Spalten, die die Oberfläche nicht führt; die zweite entfernt → erste und dritte bleiben,
        /// ihre Bauteile auch, die der zweiten fallen.
        /// </summary>
        [Fact]
        public void Drei_Zonen_die_zweite_ersetzen_und_entfernen()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.SpeichernJeGebaeude(Gebaeude, DreiZonen()).Ok);
            List<ZoneModel> vorher = ctrl.LesenJeGebaeude(Gebaeude);
            int[] ids = vorher.Select(z => z.ID).ToArray();

            (GebaeudeZonenweg weg, GebaeudeArbeitsstand a) = Oeffnen();
            Assert.Equal(3, a.Zonen.Count);
            ZoneDaten zweite = a.ZoneMitId(ids[1])!.Kopie();
            zweite.Bezeichner = "Mitte neu";
            zweite.Bauteile[0].Flaeche = 33;
            Assert.True(a.ZoneErsetzen(zweite));
            Assert.Equal("", weg.Pruefen!(a.Zonenstand(false)));
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));

            List<ZoneModel> nachher = ctrl.LesenJeGebaeude(Gebaeude);
            Assert.Equal(ids, nachher.Select(z => z.ID));
            Assert.Equal(new[] { "Erste", "Mitte neu", "Dritte" }, nachher.Select(z => z.Bezeichner));
            Assert.Equal(new[] { 1, 2, 3 }, nachher.Select(z => z.Rang));
            Assert.Equal(33, nachher[1].Bauteile[0].Flaeche);
            Assert.Equal(19, nachher[1].Raumsolltemperatur_Tag);                  // ungelesene Spalte bleibt
            Assert.Equal(21.5, nachher[0].Raumsolltemperatur_Tag);
            Assert.Equal("raum-1", nachher[0].Quellkennung);

            (weg, a) = Oeffnen();
            Assert.True(a.ZoneEntfernen(ids[1]));
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));
            List<ZoneModel> rest = ctrl.LesenJeGebaeude(Gebaeude);
            Assert.Equal(new[] { ids[0], ids[2] }, rest.Select(z => z.ID));
            Assert.Equal(new[] { 1, 2 }, rest.Select(z => z.Rang));
            Assert.All(rest, z => Assert.Single(z.Bauteile));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Zone = ?", new DbParam("@z", ids[1])));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil"));
        }

        [Fact]
        public void Umordnen_schreibt_den_Rang_lueckenlos_und_haelt_die_Schluessel()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.SpeichernJeGebaeude(Gebaeude, DreiZonen()).Ok);
            int[] ids = ctrl.LesenJeGebaeude(Gebaeude).Select(z => z.ID).ToArray();

            (GebaeudeZonenweg weg, GebaeudeArbeitsstand a) = Oeffnen();
            Assert.True(a.ZoneVerschieben(ids[2], -1));
            Assert.True(a.ZoneVerschieben(ids[2], -1));
            Assert.True(a.ZonenGeaendert);
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));

            List<ZoneModel> g = ctrl.LesenJeGebaeude(Gebaeude);
            Assert.Equal(new[] { ids[2], ids[0], ids[1] }, g.Select(z => z.ID));
            Assert.Equal(new[] { 1, 2, 3 }, g.Select(z => z.Rang));
            Assert.Equal(new[] { "Dritte", "Erste", "Mitte" }, g.Select(z => z.Bezeichner));
        }

        /// <summary>
        /// Das Duplikat übernimmt über <see cref="ZoneDaten.VorlageId"/> die Spalten der Vorlage, die die
        /// Oberfläche nicht führt (Sollwerte, Kühl- und Übergabewerte) — nicht aber ihre Herkunft, ihre
        /// Quellkennung und ihre Importpaarung; seine Bauteile sind manuell und neu.
        /// </summary>
        [Fact]
        public void Das_Duplikat_uebernimmt_die_Spalten_der_Vorlage_nicht_ihre_Herkunft()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.SpeichernJeGebaeude(Gebaeude, DreiZonen()).Ok);
            List<ZoneModel> vorher = ctrl.LesenJeGebaeude(Gebaeude);

            (GebaeudeZonenweg weg, GebaeudeArbeitsstand a) = Oeffnen();
            ZoneDaten kopie = a.ZoneDuplizieren(vorher[0].ID, "Erste (Kopie)")!;
            kopie.Nutzflaeche = 10;
            Assert.Equal("", weg.Speichern!(a.Zonenstand(true)));

            List<ZoneModel> g = ctrl.LesenJeGebaeude(Gebaeude);
            Assert.Equal(new[] { "Erste", "Erste (Kopie)", "Mitte", "Dritte" }, g.Select(z => z.Bezeichner));
            Assert.Equal(new[] { 1, 2, 3, 4 }, g.Select(z => z.Rang));
            ZoneModel d = g[1];
            Assert.True(d.ID > 0);
            Assert.DoesNotContain(d.ID, vorher.Select(z => z.ID));
            Assert.Equal(21.5, d.Raumsolltemperatur_Tag);
            Assert.Equal(26, d.Kuehl_Sollwert);
            Assert.True(d.Kuehlung_Aktiv);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, d.Uebergabe_Art);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, d.Herkunft);
            Assert.Null(d.Quellkennung);
            BauteilModel b = Assert.Single(d.Bauteile);
            Assert.DoesNotContain(b.ID, vorher.SelectMany(z => z.Bauteile).Select(x => x.ID));
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, b.Herkunft);
            Assert.Null(b.Quellkennung);
            // Die Vorlage bleibt unberührt.
            Assert.Equal("raum-1", g[0].Quellkennung);
            Assert.Equal("wand-1", g[0].Bauteile[0].Quellkennung);
        }

        /// <summary>Die Prüfregeln laufen über die Hülle schon vor dem Schreiben: eine zweite Zone ohne Fläche.</summary>
        [Fact]
        public void Die_Huelle_prueft_die_Liste_vor_dem_Schreiben()
        {
            if (!_db.Vorhanden) return;
            (GebaeudeZonenweg weg, GebaeudeArbeitsstand a) = Oeffnen();
            Assert.Empty(a.Zonen);
            a.ZoneAnlegen(new ZoneDaten { Id = -1, Bezeichner = "Haus", Nutzflaeche = 80 });
            a.ZoneAnlegen(a.NeueZone("Anbau"));
            Assert.Equal(string.Format(R.ZONE_MSG_NUTZFLAECHE_PFLICHT, "Anbau"), weg.Pruefen!(a.Zonenstand(false)));
            Assert.Equal(string.Format(R.ZONE_MSG_NUTZFLAECHE_PFLICHT, "Anbau"), weg.Speichern!(a.Zonenstand(true)));
            Assert.Empty(new GebaeudeZonenCtrl().LesenJeGebaeude(Gebaeude));
        }

        /// <summary>
        /// Die Auskunft des Bedarfsdialogs mit zwei Zonen nennt den benannten Grund der Fassade statt der
        /// allgemeinen Meldung — der Lauf rechnet mehrere Zonen erst mit Stufe G6b.
        /// </summary>
        [Fact]
        public void Die_Auskunft_mit_zwei_Zonen_rechnet_sie()
        {
            if (!_db.Vorhanden) return;
            List<ZoneModel> zwei = DreiZonen().Take(2).ToList();
            zwei[1].IstBeheizt = false;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(Gebaeude, zwei).Ok);
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);

            SimulationProtokoll.NeuStarten();
            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, IdZ);
            Assert.True(e.Erfolgreich, e.Befund);

            // Stufe G6b (A2): je Zone eine Zeile, die unbeheizte ohne Energie; die beheizte traegt
            // die ganze Heizwaerme des Gebaeudes (Skalierungsfaktor 1, Festlegung 11).
            Assert.Equal(new[] { "Erste", "Mitte" }, e.Zonen.Select(z => z.Name));
            GebaeudeBedarfZone beheizt = e.Zonen[0], frei = e.Zonen[1];
            Assert.True(beheizt.IstBeheizt);
            Assert.False(frei.IstBeheizt);
            Assert.Null(frei.HeizwaermeMwh);
            Assert.Null(frei.MaxLastKw);
            Assert.Null(frei.HeizlastKw);
            Assert.Null(frei.HeizsollwertC);
            Assert.NotNull(frei.RaumtemperaturC);
            Assert.Equal(e.HeizwaermeMwh, beheizt.HeizwaermeMwh.Value, 1e-9 * Math.Max(1.0, e.HeizwaermeMwh));
            Assert.Equal(e.MaxLastKw, beheizt.MaxLastKw.Value, 1e-9 * Math.Max(1.0, e.MaxLastKw));
            // Die Gebaeudekennzahlen nach Festlegung 10: Temperatur und Ueberhitzung der beheizten Zone.
            Assert.Equal(e.MittlereRaumtemperaturC.Value, beheizt.MittlereRaumtemperaturC.Value, 1e-9);
            Assert.Equal(e.UeberhitzungsstundenH, beheizt.UeberhitzungsstundenH);

            IReadOnlyDictionary<string, object> gaben =
                GebaeudeBedarfHuelle.Gaben(new GebaeudeProjektZeile { IdZ = IdZ }, PROJEKT, out string befund);
            Assert.NotNull(gaben);
            Assert.True(string.IsNullOrEmpty(befund), befund);
            var daten = (GebaeudeBedarfDaten)gaben["Daten"];
            Assert.Equal(2, daten.Zonen.Count);
            Assert.Null(daten.Zonen[1].HeizwaermeMwh);
            var zonenbild = (Func<int, bool, WindowsFormsApplication1.Zeichnung.Zeichenmodell>)gaben["BildauftragZone"];
            var zonenraum = (Func<int, WindowsFormsApplication1.Zeichnung.Zeichenmodell>)gaben["BildauftragRaumtemperaturZone"];
            Assert.NotNull(zonenbild(0, false));
            Assert.Null(zonenbild(1, false));                    // unbeheizt: keine Waermelast
            Assert.NotNull(zonenraum(1));
        }
    }
}
