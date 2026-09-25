using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Fassade des Bilanzrechenwegs</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 2.3,
    /// 2.4, 4.6): Energieerhaltung über die Zonen, getrennte Zirkulation, Kalibrierung im Ablauf,
    /// Kennzahlen, Herkunftsprotokoll, benannte Ablehnungen, Determinismus, Bilanzreihe.
    /// Erfundene Katalog- und Parameterwerte.
    /// </summary>
    public sealed class ZapfprofilRechnerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Nutzungsart[] Katalog =
        {
            Art(1),
            Art(2, bezug: ZapfBezugsart.Beschaeftigte, bedarf: new[] { 0.5, 1.0, 2.0 },
                woche: new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0 }, ferienfaktor: 0.5,
                monate: new[] { 0.5, 0.5, 1.0, 1.0, 1.5, 1.5, 1.5, 1.5, 1.0, 1.0, 0.5, 0.5 }),
            Art(3, grenze: ZapfBilanzgrenze.MitVerteilung),
        };

        private static ZapfprofilErgebnis Rechnen(ProjektStand p, params ZonenStand[] zonen)
            => ZapfprofilRechner.Rechnen(Eingang(p, Parameter(), zonen), Katalog);

        [Fact]
        public void Die_Summenreihe_erhaelt_die_Jahresmengen_der_Zonen()
        {
            ZonenStand a = Zone("Zone A", 1, 20.0, 1);
            ZonenStand b = Zone("Zone B", 2, 30.0, 2) with { ZapftemperaturC = 55.0 };
            ZapfprofilErgebnis e = Rechnen(Projekt() with { ZirkFlaecheM2 = 500.0 }, a, b);

            Assert.True(e.Vollstaendig);
            double qa = 20.0 * 2.0 * 365.0 * (50.0 - 11.0) / 38.0;
            double qb = 30.0 * 1.0 * 365.0 * (55.0 - 11.0) / 38.0;
            Assert.True(Relativ(e.JeZone[0].Zapfung.JahressummeKwh, qa) < 1e-9);
            Assert.True(Relativ(e.JeZone[1].Zapfung.JahressummeKwh, qb) < 1e-9);
            Assert.True(Relativ(e.Zapfung.JahressummeKwh, qa + qb) < 1e-9);
            Assert.True(Relativ(e.Zapfung.MonatssummenKwh.Sum(), qa + qb) < 1e-9);
            Assert.True(Relativ(e.Zirkulation.JahressummeKwh, e.Zirkulationsansatz.JahresverlustVorKalibrierungKwh) < 1e-9);
            Assert.True(Relativ(e.Zirkulation.JahressummeKwh, e.JeZone.Sum(z => z.Zirkulation.JahressummeKwh)) < 1e-12);
        }

        [Fact]
        public void Die_Zirkulation_wird_nie_in_die_Zapfreihe_eingerechnet()
        {
            ZonenStand a = Zone();
            ZapfprofilErgebnis ohne = Rechnen(Projekt() with { ZirkAuto = false, ZirkManuellKw = 0.0 }, a);
            ZapfprofilErgebnis mit = Rechnen(Projekt() with { ZirkAuto = false, ZirkManuellKw = 2.0 }, a);
            Assert.Equal(ohne.Zapfung.StundenKwh, mit.Zapfung.StundenKwh);
            Assert.Equal(0.0, ohne.Zirkulation.JahressummeKwh);
            Assert.True(Relativ(mit.Zirkulation.JahressummeKwh, 2.0 * 18.0 * 365.0) < 1e-12);
        }

        [Fact]
        public void Die_Kalibrierung_greift_nach_dem_Zirkulationsanteil()
        {
            // Grenze 2: Zapfung + Zirkulation der Zone = Messwert.
            ZonenStand a = Zone() with
            {
                Jahresmesswert = 10000.0, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
                JahresmesswertBilanzgrenze = ZapfBilanzgrenze.MitVerteilung,
                JahresmesswertQuelle = "Zähler (fiktiv)", JahresmesswertZeitraum = "Jahr 1"
            };
            ZapfprofilErgebnis e = Rechnen(Projekt() with { ZirkAuto = false, ZirkManuellKw = 0.5 }, a);
            ZonenErgebnis z = e.JeZone[0];
            Assert.True(Relativ(z.Zapfung.JahressummeKwh + z.Zirkulation.JahressummeKwh, 10000.0) < 1e-9);
            Assert.NotNull(z.Kalibrierfaktor);
            Herkunftseintrag k = e.Herkunft.Last(x => x.Feld == ZapfFeld.KALIBRIERFAKTOR);
            Assert.Equal(Wertstatus.Kalibriert, k.Status);
            // Der Vermerk ist ein Satz mit Kennung und Werten (N13 (b)), kein deutscher Klartext:
            // geprüft wird die Kennung und die Messwertquelle als WERT, nicht der fertige Satz.
            Assert.Equal("HERKUNFT_MESSWERT_KALIBRIERUNG", k.Vermerk.Kennung);
            Assert.Contains("Zähler (fiktiv)", k.Vermerk.Werte);
            Assert.Equal(Wertstatus.Kalibriert, e.Herkunft.Last(x => x.Feld == ZapfFeld.JAHRESENERGIE).Status);
        }

        [Fact]
        public void Die_Kennzahlen_folgen_aus_den_Reihen()
        {
            ZonenStand a = Zone(menge: 10.0);
            Zapfprofileingang ein = Eingang(Projekt() with { ZirkFlaecheM2 = 200.0 }, Parameter(), a) with
            {
                AnzeigetemperaturC = 42.0, SchwelleKw = 1.0
            };
            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(ein, Katalog);
            Zapfkennzahlen k = e.Kennzahlen;

            double zapf = e.Zapfung.JahressummeKwh, zirk = e.Zirkulation.JahressummeKwh;
            Assert.Equal(zapf, k.JahresbedarfZapfungKwh);
            Assert.Equal(zirk, k.JahresverlustZirkulationKwh);
            Assert.Equal(zapf + zirk, k.JahresbedarfGesamtKwh);
            Assert.Equal(zirk / (zapf + zirk), k.Zirkulationsanteil, 15);
            Assert.Equal(zapf / 365.0, k.TagesmittelZapfungKwh, 12);
            double max = Enumerable.Range(0, 8760).Max(h => e.Zapfung.StundenKwh[h] + e.Zirkulation.StundenKwh[h]);
            Assert.Equal(max, k.GroessterStundenwertKw);
            Assert.Equal((zapf + zirk) / max, k.VolllaststundenH, 9);
            Assert.Equal("Bilanzwert, keine Auslegungsgröße", k.VermerkGroessterStundenwert.Klartext);
            Assert.Equal(Enumerable.Range(0, 8760).Count(h => e.Zapfung.StundenKwh[h] + e.Zirkulation.StundenKwh[h] > 1.0),
                         k.StundenUeberSchwelle);
            Assert.Equal(1.0, k.SchwelleKw);
            double liter = zapf / 365.0 * 1000.0 / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * (42.0 - 11.0));
            Assert.Equal(liter, k.ZapfungLiterJeTag.Value, 9);
            Assert.Equal(zapf / 10.0, e.JeZone[0].SpezifischKwhJeEinheitJahr, 9);

            ZapfprofilErgebnis ohneAnzeige = Rechnen(Projekt(), a);
            Assert.Null(ohneAnzeige.Kennzahlen.ZapfungLiterJeTag);
            Assert.Null(ohneAnzeige.Kennzahlen.StundenUeberSchwelle);
            Assert.Null(ohneAnzeige.Kennzahlen.SchwelleKw);
        }

        [Fact]
        public void Eine_nicht_rechenbare_Zone_traegt_null_und_ist_benannt()
        {
            ZonenStand gut = Zone("Zone A", 1, 10.0, 1);
            ZonenStand fremd = Zone("Zone X", 99, 10.0, 2);
            ZonenStand leer = Zone("Zone Y", 1, 0.0, 3);
            ZapfprofilErgebnis e = Rechnen(Projekt(), gut, fremd, leer);

            Assert.False(e.Vollstaendig);
            Assert.Equal(2, e.Ablehnungen.Count);
            Assert.Contains(e.Ablehnungen, x => x.Zone == "Zone X" && x.Grund == ZapfEingabefehler.NutzungsartFehlt);
            Assert.Contains(e.Ablehnungen, x => x.Zone == "Zone Y" && x.Grund == ZapfEingabefehler.BezugsmengeFehlt);
            Assert.True(e.JeZone[1].Abgelehnt);
            Assert.Equal(0.0, e.JeZone[1].Zapfung.JahressummeKwh);
            Assert.Equal(e.JeZone[0].Zapfung.JahressummeKwh, e.Zapfung.JahressummeKwh);
        }

        [Fact]
        public void Ein_fehlender_Parameter_lehnt_die_Zone_bzw_die_Zirkulation_benannt_ab()
        {
            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(
                Eingang(Projekt(), Parameter(null, ZapfParameter.KALTWASSER_MITTEL), Zone()), Katalog);
            Assert.Contains(e.Ablehnungen, x => x.Zone == "Zone A" && x.Grund == ZapfEingabefehler.ParameterFehlt
                                                && x.Klartext.Contains(ZapfParameter.KALTWASSER_MITTEL));

            ZapfprofilErgebnis z = ZapfprofilRechner.Rechnen(
                Eingang(Projekt(), Parameter(null, ZapfParameter.ZIRKULATION_LAUFZEIT), Zone()), Katalog);
            Assert.Contains(z.Ablehnungen, x => x.Zone == "" && x.Grund == ZapfEingabefehler.ParameterFehlt);
            Assert.Equal(0.0, z.Zirkulation.JahressummeKwh);
            Assert.True(z.Zapfung.JahressummeKwh > 0);
        }

        [Fact]
        public void Kalender_Projekt_und_Parametersatz_sind_Pflicht()
        {
            Zapfprofileingang ein = Eingang(Projekt(), Parameter(), Zone());
            Assert.Equal(ZapfEingabefehler.KalenderUngueltig,
                Assert.Throws<ZapfprofilEingabeException>(() => ZapfprofilRechner.Rechnen(ein with { We = new bool[10] }, Katalog)).Fehler);
            Assert.Equal(ZapfEingabefehler.ProjektFehlt,
                Assert.Throws<ZapfprofilEingabeException>(() => ZapfprofilRechner.Rechnen(ein with { Projekt = null }, Katalog)).Fehler);
            Assert.Equal(ZapfEingabefehler.ParameterFehlt,
                Assert.Throws<ZapfprofilEingabeException>(() => ZapfprofilRechner.Rechnen(ein with { Parameter = null }, Katalog)).Fehler);
        }

        [Fact]
        public void Der_eigene_Tagesgangsatz_der_Zone_wird_benutzt_und_ein_fehlender_benannt()
        {
            Tagesgangsatz eigen = Satz(7, Gang((12, 1.0)), Gang((12, 1.0)), Gang((12, 1.0)), Gang((12, 1.0)));
            ZonenStand z = Zone() with { IdTagesgangsatz = 7 };
            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(
                Eingang(Projekt(), Parameter(), z) with { Tagesgangsaetze = new[] { eigen } }, Katalog);
            Assert.True(e.Vollstaendig);
            for (int h = 0; h < 8760; h++)
                if (h % 24 != 12) Assert.Equal(0.0, e.Zapfung.StundenKwh[h]);
            Assert.Equal(Wertstatus.Ueberschrieben, e.Herkunft.Last(x => x.Feld == ZapfFeld.TAGESGANGSATZ).Status);

            ZapfprofilErgebnis f = Rechnen(Projekt(), z);
            Assert.Contains(f.Ablehnungen, x => x.Grund == ZapfEingabefehler.TagesgangsatzFehlt);
        }

        [Fact]
        public void Die_Rechnung_ist_deterministisch()
        {
            ZonenStand a = Zone("Zone A", 1, 12.0, 1) with { Ferienbeginn = new int?[] { 200, null, null, null }, Ferienende = new int?[] { 215, null, null, null } };
            ZonenStand b = Zone("Zone B", 2, 7.0, 2);
            ZapfprofilErgebnis e1 = Rechnen(Projekt(), a, b);
            ZapfprofilErgebnis e2 = Rechnen(Projekt(), a, b);
            Assert.Equal(e1.Zapfung.StundenKwh, e2.Zapfung.StundenKwh);
            Assert.Equal(e1.Zirkulation.StundenKwh, e2.Zirkulation.StundenKwh);
            Assert.Equal(e1.Herkunft.Count, e2.Herkunft.Count);
        }

        [Fact]
        public void Eine_Zone_mit_Grenze_zwei_traegt_keine_Zirkulation()
        {
            ZonenStand a = Zone("Zone A", 1, 10.0, 1);
            ZonenStand c = Zone("Zone C", 3, 10.0, 2);
            ZapfprofilErgebnis e = Rechnen(Projekt() with { ZirkAuto = false, ZirkManuellKw = 1.0 }, a, c);
            Assert.Equal(0.0, e.JeZone[1].Zirkulation.JahressummeKwh);
            Assert.True(Relativ(e.JeZone[0].Zirkulation.JahressummeKwh, 1.0 * 18.0 * 365.0) < 1e-12);
            Assert.Contains(e.Hinweise, h => h.Code == "ZIRKULATION_NICHT_IN_Z1" && h.Zone == "Zone C");
        }

        // =================================================================================
        // Messwerte, Restpfad der Zirkulation, fehlende Schwellen (4.1, 4.3, N7)
        // =================================================================================

        /// <summary>Eine Zone mit f_θ = 1 (Kaltwasser = Bezug des fiktiven Katalogs): 10 · 2 · 365 = 7300 kWh/a.</summary>
        private static ZonenStand ZoneFlach(string name = "Zone A", double menge = 10.0, int id = 1)
            => Zone(name, 1, menge, id) with { KaltwasserMittelC = 12.0 };

        private static ZonenStand MitMesswert(ZonenStand z, double kwh, ZapfBilanzgrenze grenze, double? speicherverlust = null)
            => z with
            {
                Jahresmesswert = kwh, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
                JahresmesswertBilanzgrenze = grenze, SpeicherverlustKwhJeJahr = speicherverlust
            };

        [Fact]
        public void Ein_Messwert_ueber_der_Rueckfrageschwelle_gibt_einen_Hinweis()
        {
            // Rückfrageschwelle 0,5 (erfunden): Faktor 2 -> Hinweis, Faktor 1,1 -> keiner.
            ProjektStand ohneZirk = Projekt() with { ZirkAuto = false, ZirkManuellKw = 0.0 };
            ZapfprofilErgebnis doppelt = Rechnen(ohneZirk, MitMesswert(ZoneFlach(), 14600.0, ZapfBilanzgrenze.Zapfstelle));
            Assert.Equal(2.0, doppelt.JeZone[0].Kalibrierfaktor.Value, 12);
            Assert.Contains(doppelt.Hinweise, h => h.Code == "MESSWERT_ABWEICHUNG" && h.Zone == "Zone A");

            ZapfprofilErgebnis nah = Rechnen(ohneZirk, MitMesswert(ZoneFlach(), 8030.0, ZapfBilanzgrenze.Zapfstelle));
            Assert.Equal(1.1, nah.JeZone[0].Kalibrierfaktor.Value, 12);
            Assert.DoesNotContain(nah.Hinweise, h => h.Code == "MESSWERT_ABWEICHUNG");
        }

        [Fact]
        public void Grenze_drei_zieht_den_Speicherverlust_ab_und_nennt_ihn()
        {
            // Messwert 11000, Speicherverlust 1000 -> netto 10000 = Zapfung + Zirkulation der Zone.
            ProjektStand p = Projekt() with { ZirkAuto = false, ZirkManuellKw = 0.5 };   // 0,5 · 18 · 365 = 3285
            ZapfprofilErgebnis e = Rechnen(p, MitMesswert(ZoneFlach(), 11000.0, ZapfBilanzgrenze.MitSpeicher, 1000.0));
            ZonenErgebnis z = e.JeZone[0];
            Assert.True(e.Vollstaendig);
            Assert.True(Relativ(z.Zapfung.JahressummeKwh + z.Zirkulation.JahressummeKwh, 10000.0) < 1e-12);
            Assert.True(Relativ(z.Kalibrierfaktor.Value, 10000.0 / (7300.0 + 3285.0)) < 1e-12);
            Assert.Contains(e.Hinweise, h => h.Code == "MESSWERT_SPEICHERVERLUST" && h.Zone == "Zone A");

            // Der Ansatz bleibt der Wert vor der Kalibrierung; verbucht ist der kalibrierte (N7).
            Assert.True(Relativ(e.Zirkulationsansatz.JahresverlustVorKalibrierungKwh, 3285.0) < 1e-12);
            Assert.True(Relativ(e.Kennzahlen.JahresverlustZirkulationKwh, z.Zirkulation.JahressummeKwh) < 1e-12);
            Assert.True(Relativ(e.Kennzahlen.JahresverlustZirkulationKwh, 3285.0 * z.Kalibrierfaktor.Value) < 1e-12);
        }

        [Fact]
        public void Grenze_zwei_kalibriert_ueber_mehrere_Zonen_nur_die_eigene()
        {
            // Q_a: A 7300, B 21900; manuell 1 kW -> 6570 kWh/a, Anteile 1642,5 und 4927,5.
            ZonenStand a = MitMesswert(ZoneFlach("Zone A", 10.0, 1), 10000.0, ZapfBilanzgrenze.MitVerteilung);
            ZonenStand b = ZoneFlach("Zone B", 30.0, 2);
            ZapfprofilErgebnis e = Rechnen(Projekt() with { ZirkAuto = false, ZirkManuellKw = 1.0 }, a, b);
            Assert.True(e.Vollstaendig);

            double f = 10000.0 / (7300.0 + 1642.5);
            Assert.True(Relativ(e.JeZone[0].Kalibrierfaktor.Value, f) < 1e-12);
            Assert.True(Relativ(e.JeZone[0].Zapfung.JahressummeKwh + e.JeZone[0].Zirkulation.JahressummeKwh, 10000.0) < 1e-12);
            Assert.Null(e.JeZone[1].Kalibrierfaktor);
            Assert.True(Relativ(e.JeZone[1].Zapfung.JahressummeKwh, 21900.0) < 1e-12);
            Assert.True(Relativ(e.JeZone[1].Zirkulation.JahressummeKwh, 4927.5) < 1e-12);
            Assert.True(Relativ(e.Zirkulation.JahressummeKwh, f * 1642.5 + 4927.5) < 1e-12);
            Assert.True(Relativ(e.Zirkulationsansatz.JahresverlustVorKalibrierungKwh, 6570.0) < 1e-12);
        }

        [Fact]
        public void Manuelle_Zirkulation_ohne_Zone_in_Z1_wird_gebaeudeweit_ausgewiesen()
        {
            // Nur eine Zone mit Grenze 2: 1 kW · 18 h · 365 = 6570 kWh/a als Restreihe.
            ZapfprofilErgebnis e = Rechnen(Projekt() with { ZirkAuto = false, ZirkManuellKw = 1.0 }, Zone("Zone C", 3, 10.0, 1));
            Assert.True(e.Vollstaendig);
            Assert.Equal(0.0, e.JeZone[0].Zirkulation.JahressummeKwh);
            Assert.True(Relativ(e.Zirkulationsansatz.RestKwh, 6570.0) < 1e-12);
            Assert.True(Relativ(e.Zirkulation.JahressummeKwh, 6570.0) < 1e-12);
            Assert.True(Relativ(e.Zirkulation.StundenKwh.Sum(), 6570.0) < 1e-12);
            Assert.Equal(18.0, e.Laufzeitfenster.Sum(), 12);
            Assert.Contains(e.Hinweise, h => h.Code == "ZIRKULATION_OHNE_ZONE");
            Assert.Contains(e.Hinweise, h => h.Code == "ZIRKULATION_NICHT_IN_Z1" && h.Zone == "Zone C");
        }

        [Fact]
        public void Eine_Personenzone_mit_Wohnungstabelle_traegt_eine_Flaeche()
        {
            // 3 WE · 80 m² (Parameter, erfunden) = 240 m²; Flächenkennwert 5 -> 1200 kWh/a, ohne Rückfall.
            ZonenStand z = ZoneFlach() with { Wohnungen = new[] { new WohnungstypStand { Anzahl = 3, Personen = 2.0 } } };
            ZapfprofilErgebnis e = Rechnen(Projekt(), z);
            Assert.Equal(ZapfZirkulationsmethode.Flaechenkennwert, e.Zirkulationsansatz.Methode);
            Assert.True(Relativ(e.Zirkulationsansatz.JahresverlustVorKalibrierungKwh, 5.0 * 240.0) < 1e-12);
            Assert.DoesNotContain(e.Hinweise, h => h.Code == "ZIRKULATION_OHNE_FLAECHE");
            Assert.Equal(240.0, e.Herkunft.Last(x => x.Feld == ZapfFeld.ZONENFLAECHE).Wert.Value);
        }

        [Fact]
        public void Fehlende_Schwellen_werden_als_Parameter_fehlt_genannt_ohne_Rueckfallwert()
        {
            // Wochenfaktoren mit Summe 1,4 (erfunden): mit Warnschwelle ein Hinweis zur Summe.
            var katalog = new[] { Art(1, woche: new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.2, 0.2 }) };
            ProjektStand p = Projekt() with { ZirkAuto = false, ZirkManuellKw = 0.0 };
            ZonenStand a = MitMesswert(ZoneFlach("Zone A", 10.0, 1), 14600.0, ZapfBilanzgrenze.Zapfstelle);
            ZonenStand b = ZoneFlach("Zone B", 10.0, 2);

            ZapfprofilErgebnis mit = ZapfprofilRechner.Rechnen(Eingang(p, Parameter(), a, b), katalog);
            Assert.Contains(mit.Hinweise, h => h.Code == "WOCHENFAKTOREN_SUMME");
            Assert.DoesNotContain(mit.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT);

            ZapfprofilErgebnis ohne = ZapfprofilRechner.Rechnen(
                Eingang(p, Parameter(null, ZapfParameter.FORMVEKTOR_WARNSCHWELLE, ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE), a, b),
                katalog);
            Assert.True(ohne.Vollstaendig);
            Assert.DoesNotContain(ohne.Hinweise, h => h.Code == "WOCHENFAKTOREN_SUMME");
            Assert.DoesNotContain(ohne.Hinweise, h => h.Code == "MESSWERT_ABWEICHUNG");
            Assert.Single(ohne.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT
                                              && h.Text.Contains(ZapfParameter.FORMVEKTOR_WARNSCHWELLE));
            Assert.Single(ohne.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT
                                              && h.Text.Contains(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE));
            // Die Rechnung selbst ist dieselbe: die Schwellen entscheiden sie nicht.
            Assert.Equal(mit.Zapfung.StundenKwh, ohne.Zapfung.StundenKwh);

            // Ohne Messwert kein Hinweis zur Rückfrageschwelle.
            ZapfprofilErgebnis ohneMesswert = ZapfprofilRechner.Rechnen(
                Eingang(p, Parameter(null, ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE), b), katalog);
            Assert.DoesNotContain(ohneMesswert.Hinweise, h => h.Code == ZapfHinweis.PARAMETER_FEHLT);
        }

        // =================================================================================
        // Bilanzreihe
        // =================================================================================

        [Fact]
        public void Die_Bilanzreihe_ist_unveraenderlich_und_summiert_Monate_und_Jahr()
        {
            var werte = new double[8760];
            for (int h = 0; h < 8760; h++) werte[h] = (h % 24) * 0.125;
            var r = new Bilanzreihe(werte);
            werte[0] = 999.0;
            Assert.Equal(0.0, r.StundenKwh[0]);
            double[] kopie = r.KopieStundenKwh();
            kopie[1] = 999.0;
            Assert.Equal(0.125, r.StundenKwh[1]);
            Assert.Throws<NotSupportedException>(() => ((IList<double>)r.StundenKwh)[2] = 1.0);

            Assert.Equal(12, r.MonatssummenKwh.Count);
            Assert.Equal(31 * 24 * 1.4375, r.MonatssummenKwh[0], 9);   // Mittel über 0 … 23 · 0,125
            Assert.Equal(28 * 24 * 1.4375, r.MonatssummenKwh[1], 9);
            Assert.Equal(r.MonatssummenKwh.Sum(), r.JahressummeKwh, 9);
            Assert.Equal(23 * 0.125, r.GroessterStundenwertKw);
            Assert.Equal(365, r.StundenUeber(23 * 0.125 - 0.01));

            Assert.Throws<ArgumentException>(() => new Bilanzreihe(new double[8784]));
            var nan = new double[8760];
            nan[5] = double.NaN;
            Assert.Throws<ArgumentException>(() => new Bilanzreihe(nan));
            Assert.Equal(2.0 * r.JahressummeKwh, Bilanzreihe.Summe(new[] { r, r }).JahressummeKwh, 9);
        }

        /// <summary>
        /// ZU5 (Konzept 9, Risiko 8): Netzverluste des Projekts und eine gerechnete Zirkulation
        /// zugleich ergeben den nicht blockierenden Hinweis NETZVERLUST_UND_ZIRKULATION — ohne
        /// Netzverluste, ohne Zone mit Zirkulation oder ohne Zirkulationsverlust nicht. Die Reihen
        /// bleiben gleich (die Netzverlustverteilung ist nicht Sache des Generators).
        /// </summary>
        [Fact]
        public void Netzverluste_und_Zirkulation_zugleich_ergeben_den_Hinweis_ZU5()
        {
            const string KENNUNG = "NETZVERLUST_UND_ZIRKULATION";
            ProjektStand p = Projekt() with { ZirkFlaecheM2 = 200.0 };
            Zapfprofileingang ohne = Eingang(p, Parameter(), Zone());
            Zapfprofileingang mit = ohne with { NetzverlusteProjekt = 5.0 };

            ZapfprofilErgebnis a = ZapfprofilRechner.Rechnen(ohne, Katalog);
            ZapfprofilErgebnis b = ZapfprofilRechner.Rechnen(mit, Katalog);

            Assert.DoesNotContain(a.Hinweise, h => h.Code == KENNUNG);
            ZapfHinweis h = Assert.Single(b.Hinweise, x => x.Code == KENNUNG);
            Assert.Equal("", h.Zone);
            Assert.True(b.Vollstaendig);
            Assert.Equal(a.Zapfung.StundenKwh, b.Zapfung.StundenKwh);
            Assert.Equal(a.Zirkulation.StundenKwh, b.Zirkulation.StundenKwh);

            // Keine Zone mit Zirkulation, bzw. kein Zirkulationsverlust: kein Hinweis.
            Zapfprofileingang ohneZone = Eingang(p, Parameter(), Zone() with { Zirkulation = false }) with { NetzverlusteProjekt = 5.0 };
            Assert.DoesNotContain(ZapfprofilRechner.Rechnen(ohneZone, Katalog).Hinweise, x => x.Code == KENNUNG);
            Zapfprofileingang ohneVerlust = Eingang(Projekt() with { ZirkAuto = false, ZirkManuellKw = 0.0 }, Parameter(), Zone())
                                            with { NetzverlusteProjekt = 5.0 };
            Assert.DoesNotContain(ZapfprofilRechner.Rechnen(ohneVerlust, Katalog).Hinweise, x => x.Code == KENNUNG);
        }
    }
}
