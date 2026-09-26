using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Stufen Erweitert und Experte</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 5.1, 5.3, 5.5; Stufe Z4, Gruppe 2a): die Angaben je Zone und die gebäudeweiten Größen hin und
    /// zurück, Ferien als Tag und Monat des Rechenjahrs, die Pflichtprüfung der höheren Stufen, der
    /// Stand beim Öffnen (Vorgaben, Tagesgangsätze, Ausstattungen, Gebäude) und die Spalten der
    /// Zonenliste aus der Vorschau. Mit Datenbank: die Arbeitskopie der Testdatenbank (Projekt 1007);
    /// ohne Testdatenbank schweigen diese Fälle. Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleStufenTests : IDisposable
    {
        private const int PROJEKT = 1007;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Arbeitsstand <-> DTO
        // =================================================================================

        private static ZonenStand Reich() => new ZonenStand
        {
            Id = 4,
            IdNutzungsart = 11,
            IdTagesgangsatz = 5,
            IdGebaeude = 77,
            Name = "Zone Nord",
            Reihenfolge = 1,
            Bezugsmenge = 20.0,
            PersonenJeWe = 2.5,
            WohnflaecheJeWeM2 = 70.0,
            Topologie = ZapfTopologie.Wohnungsstation,
            Zirkulation = false,
            Ferienbeginn = new int?[] { 335, null, 182, 0 },
            Ferienende = new int?[] { 31, null, 243, 366 },
            Jahresmesswert = 30000.0,
            JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
            JahresmesswertBilanzgrenze = ZapfBilanzgrenze.MitSpeicher,
            JahresmesswertQuelle = "Wärmemengenzähler",
            JahresmesswertZeitraum = "2025",
            SpeicherverlustKwhJeJahr = 900.0,
            TagesbedarfAuto = false,
            TagesbedarfManuellKwh = 90.0,
            BedarfSpezKwhJeEinheitTag = 5.5,
            ZapftemperaturC = 55.0,
            KaltwasserMittelC = 10.0,
            KaltwasserAmplitudeK = 4.0,
            Auslastung = new double?[] { 1.1, null, null, null, null, null, null, 0.5, null, null, null, null },
            Wohnungen = new[]
            {
                new WohnungstypStand { Id = 31, Anzahl = 4, Raumzahl = 3, Personen = 2.0, IdAusstattung = 9, Reihenfolge = 2 },
                new WohnungstypStand { Id = 30, Anzahl = 6, Raumzahl = 2, Reihenfolge = 1 }
            }
        };

        private static ProjektStand Projekt() => new ProjektStand
        {
            Id = 3,
            Weg = BrauchwasserWeg.Generator,
            Seed = 1,
            Realisierungen = 10,
            Perzentil = 99,
            ZirkAuto = false,
            ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge,
            ZirkLage = ZapfLeitungslage.AusserhalbHuelle,
            ZirkLaengeM = 137.0,
            ZirkVerlustWJeM = 10.0,
            ZirkAnteil = 0.25,
            ZirkKennwert = 8.0,
            ZirkFlaecheM2 = 1500.0,
            ZirkLaufzeitH = 18.0,
            ZirkManuellKw = 1.0,
            LeitungsinhaltL = 4.0,
            LadeAuto = false,
            LadeManuellKw = 15.0,
            LadefensterH = 16.0,
            LadefensterBeginnH = 6.0,
            SpeicherC = 60.0,
            KaltwasserAuslegungC = 10.0,
            Speicherart = ZapfSpeicherart.Ladespeicher
        };

        [Fact]
        public void Die_Angaben_der_hoeheren_Stufen_gehen_hin_und_unveraendert_zurueck()
        {
            ProjektStand p = Projekt();
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Reich() }, p);

            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            ZapfprofilZonenangabenDaten a = e.Zonen[0].Angaben;
            Assert.NotNull(a);
            Assert.Equal(77, a.IdGebaeude);
            Assert.Equal(5, a.IdTagesgangsatz);
            Assert.Equal(ZapfprofilTopologie.Wohnungsstation, a.Topologie);
            Assert.False(a.Zirkulation);
            Assert.Equal(ZapfprofilBilanzgrenze.MitSpeicher, a.JahresmesswertBilanzgrenze);
            Assert.Equal("Wärmemengenzähler", a.JahresmesswertQuelle);
            Assert.False(a.TagesbedarfAuto);
            // Ferien als Tag und Monat: 335 = 1. Dezember, 31 = 31. Januar; 0 und 366 = keine Angabe.
            Assert.Equal((1, 12), (a.Ferien[0].BeginnTag.Value, a.Ferien[0].BeginnMonat.Value));
            Assert.Equal((31, 1), (a.Ferien[0].EndeTag.Value, a.Ferien[0].EndeMonat.Value));
            Assert.False(a.Ferien[1].Belegt);
            Assert.Equal((1, 7), (a.Ferien[2].BeginnTag.Value, a.Ferien[2].BeginnMonat.Value));
            Assert.False(a.Ferien[3].Belegt);
            // Die Wohnungstabelle in ihrer Reihenfolge.
            Assert.Equal(new[] { 30, 31 }, a.Wohnungen.Select(w => w.Id).ToArray());
            Assert.Null(a.Wohnungen[0].Personen);
            Assert.Equal(9, a.Wohnungen[1].IdAusstattung);
            // Der Zähler: Tagesgangsatz, Personen, Fläche, Topologie, Zirkulation, zwei Ferienzeiträume,
            // Messwert, Speicherverlust (Grenze 3 mit Messwert), manuell (das Paar auto/manuell zählt
            // einmal), spezifischer Bedarf, Zapftemperatur, Kaltwasser Mittel und Amplitude, zwei
            // Auslastungsmonate, Wohnungstabelle.
            Assert.Equal(17, a.Ueberschrieben());
            Assert.Equal(17, e.Zonen[0].UeberschriebenZahl());
            Assert.Equal(ZapfprofilHuelle.Ueberschrieben(stand.Zonen[0]), e.Zonen[0].UeberschriebenZahl());

            ZapfprofilGebaeudeDaten g = e.Gebaeude;
            Assert.NotNull(g);
            Assert.Equal(ZapfprofilZirkulationsmethode.Leitungslaenge, g.ZirkMethode);
            Assert.Equal(ZapfprofilLeitungslage.AusserhalbHuelle, g.ZirkLage);
            Assert.Equal(137.0, g.ZirkLaengeM);
            Assert.Equal(4.0, g.LeitungsinhaltL);
            Assert.Equal(16.0, g.LadefensterH);

            // Zurück: dieselben Größen, die Projektzeile bleibt dieselbe Instanz.
            ZapfprofilStand zurueck = ZapfprofilHuelle.AlsStand(e, stand);
            Assert.Same(p, zurueck.Projekt);
            ZonenStand z = zurueck.Zonen[0];
            ZonenStand r = Reich();
            Assert.Equal(r.IdGebaeude, z.IdGebaeude);
            Assert.Equal(r.Topologie, z.Topologie);
            Assert.Equal(new int?[] { 335, null, 182, null }, z.Ferienbeginn);
            Assert.Equal(new int?[] { 31, null, 243, null }, z.Ferienende);
            Assert.Equal(r.Jahresmesswert, z.Jahresmesswert);
            Assert.Equal(r.JahresmesswertBilanzgrenze, z.JahresmesswertBilanzgrenze);
            Assert.Equal(r.SpeicherverlustKwhJeJahr, z.SpeicherverlustKwhJeJahr);
            Assert.Equal(r.TagesbedarfManuellKwh, z.TagesbedarfManuellKwh);
            Assert.Equal(r.Auslastung, z.Auslastung);
            Assert.Equal(new[] { 30, 31 }, z.Wohnungen.Select(w => w.Id).ToArray());
            Assert.Equal(new[] { 1, 2 }, z.Wohnungen.Select(w => w.Reihenfolge).ToArray());
        }

        /// <summary>
        /// Z4, Gruppe 2a Punkt 5: Nur wirksame Abweichungen zählen — ein manueller Wert bei „auto"
        /// nicht, der Speicherverlust ohne Grenze 3 oder ohne Messwert nicht, eine Wohnungstabelle
        /// nur, wenn die Nutzungsart sie führt.
        /// </summary>
        [Fact]
        public void Der_Zaehler_zaehlt_nur_wirksame_Abweichungen()
        {
            var a = new ZapfprofilZonenangabenDaten
            {
                TagesbedarfAuto = true,
                TagesbedarfManuellKwh = 42.0,                          // "auto": der Wert ist wirkungslos
                Jahresmesswert = 900.0,
                JahresmesswertBilanzgrenze = ZapfprofilBilanzgrenze.MitVerteilung,   // nicht Grenze 3
                SpeicherverlustKwhJeJahr = 120.0
            };
            a.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 2 });

            // Nur der Messwert zählt: der Speicherverlust ohne Grenze 3 nicht, "auto" lässt den
            // manuellen Wert wirkungslos, die Wohnungstabelle bleibt hier bewusst ausgeklammert.
            Assert.Equal(1, a.Ueberschrieben(wohnungstabelleWirksam: false));
            Assert.Equal(2, a.Ueberschrieben());                                     // + Wohnungstabelle (Vorgabe: wirksam)

            a.JahresmesswertBilanzgrenze = ZapfprofilBilanzgrenze.MitSpeicher;        // Grenze 3 mit Messwert: zählt jetzt mit
            Assert.Equal(2, a.Ueberschrieben(wohnungstabelleWirksam: false));
            a.Jahresmesswert = null;                                                 // Grenze 3 OHNE Messwert: zählt wieder nicht
            Assert.Equal(0, a.Ueberschrieben(wohnungstabelleWirksam: false));

            a.TagesbedarfAuto = false;                                               // das Paar auto/manuell zählt einmal
            Assert.Equal(1, a.Ueberschrieben(wohnungstabelleWirksam: false));
            Assert.Equal(2, a.Ueberschrieben());                                     // + Wohnungstabelle
        }

        /// <summary>Z4, Gruppe 2a Punkt 5: die Zirkulationsangaben einer NICHT gewählten Methode zählen nicht.</summary>
        [Fact]
        public void Der_Gebaeudezaehler_zaehlt_nur_die_gewaehlte_Zirkulationsmethode()
        {
            var vorgabe = new ZapfprofilGebaeudeDaten();
            var g = new ZapfprofilGebaeudeDaten
            {
                ZirkMethode = ZapfprofilZirkulationsmethode.Flaechenkennwert,
                ZirkLaengeM = 50.0,               // gehört zu Leitungslänge — nicht die gewählte Methode
                ZirkKennwert = 8.0                // gehört zu Flächenkennwert — die gewählte Methode
            };
            Assert.Equal(1, g.Ueberschrieben(vorgabe));

            g.ZirkAuto = false;                    // manuelle Zirkulation: keine Methodenangabe zählt
            g.ZirkManuellKw = 1.0;
            Assert.Equal(1, g.Ueberschrieben(vorgabe));   // das Paar auto/manuell statt der Methode
        }

        [Fact]
        public void Geaenderte_Angaben_und_Gebaeudegroessen_gehen_in_den_Stand()
        {
            ProjektStand p = Projekt();
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Reich() }, p);
            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);

            ZapfprofilZonenangabenDaten a = e.Zonen[0].Angaben;
            a.Topologie = ZapfprofilTopologie.Speicher;
            a.Ferien[1] = new ZapfprofilFerienDaten { BeginnTag = 1, BeginnMonat = 4, EndeTag = 14, EndeMonat = 4 };
            a.Wohnungen.RemoveAt(0);
            a.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 2, Raumzahl = 4 });
            a.JahresmesswertQuelle = "  ";
            e.Gebaeude.LadefensterH = 8.0;
            e.Gebaeude.ZirkAuto = true;

            ZapfprofilStand zurueck = ZapfprofilHuelle.AlsStand(e, stand);
            Assert.NotSame(p, zurueck.Projekt);
            Assert.Equal(8.0, zurueck.Projekt.LadefensterH);
            Assert.True(zurueck.Projekt.ZirkAuto);
            Assert.Equal(p.Seed, zurueck.Projekt.Seed);
            ZonenStand z = zurueck.Zonen[0];
            Assert.Equal(ZapfTopologie.Speicher, z.Topologie);
            Assert.Equal(91, z.Ferienbeginn[1]);   // 1. April im Rechenjahr ohne Schaltjahr
            Assert.Equal(104, z.Ferienende[1]);
            Assert.Null(z.JahresmesswertQuelle);
            Assert.Equal(new[] { 31, 0 }, z.Wohnungen.Select(w => w.Id).ToArray());
            Assert.Equal(new[] { 1, 2 }, z.Wohnungen.Select(w => w.Reihenfolge).ToArray());
        }

        [Fact]
        public void Ein_Duplikat_traegt_die_Angaben_mit_neuen_Wohnungstypen()
        {
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Reich() }, Projekt());
            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            ZapfprofilZoneDaten kopie = e.Zonen[0].Kopie();
            kopie.Id = 0;
            kopie.IdVorlage = 4;
            kopie.Name = "Zone Nord (Kopie)";
            kopie.Angaben.PersonenJeWe = 3.0;
            e.Zonen.Add(kopie);

            ZapfprofilStand zurueck = ZapfprofilHuelle.AlsStand(e, stand);
            ZonenStand d = zurueck.Zonen[1];
            Assert.Equal(0, d.Id);
            Assert.Equal(3.0, d.PersonenJeWe);
            Assert.Equal(2.5, zurueck.Zonen[0].PersonenJeWe);          // das Original bleibt
            Assert.All(d.Wohnungen, w => Assert.Equal(0, w.Id));
            Assert.Equal(2, d.Wohnungen.Count);
            Assert.NotSame(e.Zonen[0].Angaben.Wohnungen, kopie.Angaben.Wohnungen);
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(28, 2, 59)]
        [InlineData(1, 3, 60)]
        [InlineData(31, 12, 365)]
        public void Tag_und_Monat_sind_ein_Jahrestag_des_Rechenjahrs(int tag, int monat, int jahrestag)
        {
            Assert.Equal(jahrestag, ZapfprofilHuelle.Jahrestag(tag, monat));
            Assert.Equal((tag, monat), ((int, int))(ZapfprofilHuelle.AlsDatum(jahrestag).Tag.Value, ZapfprofilHuelle.AlsDatum(jahrestag).Monat.Value));
        }

        [Fact]
        public void Ein_unvollstaendiges_oder_unmoegliches_Datum_ist_keine_Angabe()
        {
            Assert.Null(ZapfprofilHuelle.Jahrestag(29, 2));
            Assert.Null(ZapfprofilHuelle.Jahrestag(null, 5));
            Assert.Null(ZapfprofilHuelle.Jahrestag(5, 13));
            Assert.Equal((null, null), ZapfprofilHuelle.AlsDatum(0));
            Assert.Equal((null, null), ZapfprofilHuelle.AlsDatum(366));
            Assert.Equal((null, null), ZapfprofilHuelle.AlsDatum(null));
        }

        [Fact]
        public void Die_Pflichtpruefung_nennt_die_Luecken_der_hoeheren_Stufen()
        {
            var a = new ZapfprofilZonenangabenDaten
            {
                Jahresmesswert = 1000.0,
                Auslastung = new double?[] { -0.1, null, null, null, null, null, null, null, null, null, null, null }
            };
            a.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 3 });
            a.Wohnungen.Add(new ZapfprofilWohnungDaten());
            a.Ferien[0] = new ZapfprofilFerienDaten { BeginnTag = 1, BeginnMonat = 7 };
            a.Ferien[1] = new ZapfprofilFerienDaten { EndeTag = 6, EndeMonat = 1 };        // über den Jahreswechsel — gültig
            a.Ferien[2] = new ZapfprofilFerienDaten { BeginnTag = 30, BeginnMonat = 2, EndeTag = 5, EndeMonat = 3 };
            var e = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = 1, Bezugsmenge = 10, Angaben = a } }
            };
            // Nutzungsart 1 mit Bezugsart Personen: ihre Wohnungstabelle ist wirksam (Z4, Gruppe 2a
            // Punkt 6) — die Pflichtprüfung braucht den beim Öffnen geladenen Katalog dafür.
            var katalog = new List<ZapfprofilNutzungsartDaten> { new() { Id = 1, Bezugsart = (int)ZapfBezugsart.Personen } };

            string[] kennungen = ZapfprofilHuelle.Pruefen(e, katalog).Select(m => m.Kennung).ToArray();
            Assert.Equal(new[]
            {
                "ZPG_MSG_WOHNUNG_OHNE_ANZAHL", "ZPG_MSG_FERIEN_UNGUELTIG", "ZPG_MSG_FERIEN_UNGUELTIG",
                "ZPG_MSG_MESSWERT_OHNE_EINHEIT", "ZPG_MSG_AUSLASTUNG_NEGATIV"
            }, kennungen);
            Assert.Contains("Wohnungstyp 2", ZapfprofilHuelle.Pruefen(e, katalog)[0].Text);
            Assert.Contains("Ferienzeitraum 3", ZapfprofilHuelle.Pruefen(e, katalog)[2].Text);

            // Ohne Katalog (die Wohnungstabelle gilt dann als nicht wirksam) bleibt die Zeile ungeprüft.
            Assert.DoesNotContain("ZPG_MSG_WOHNUNG_OHNE_ANZAHL", ZapfprofilHuelle.Pruefen(e).Select(m => m.Kennung));

            a.JahresmesswertEinheit = ZapfprofilMesswerteinheit.KwhJeJahr;
            Assert.Contains("ZPG_MSG_MESSWERT_OHNE_GRENZE", ZapfprofilHuelle.Pruefen(e, katalog).Select(m => m.Kennung));
            a.JahresmesswertBilanzgrenze = ZapfprofilBilanzgrenze.MitSpeicher;
            Assert.Contains("ZPG_MSG_MESSWERT_OHNE_SPEICHERVERLUST", ZapfprofilHuelle.Pruefen(e, katalog).Select(m => m.Kennung));
            a.JahresmesswertEinheit = ZapfprofilMesswerteinheit.KubikmeterJeJahr;
            Assert.DoesNotContain("ZPG_MSG_MESSWERT_OHNE_SPEICHERVERLUST", ZapfprofilHuelle.Pruefen(e, katalog).Select(m => m.Kennung));
            a.Jahresmesswert = 0.0;
            Assert.Contains("ZPG_MSG_MESSWERT_NICHT_POSITIV", ZapfprofilHuelle.Pruefen(e, katalog).Select(m => m.Kennung));
        }

        /// <summary>
        /// Z4, Gruppe 2a Punkt 7: „Tagesbedarf manuell" und „Zirkulation manuell" ohne gültigen Wert
        /// werden schon am OK des Zapfprofils benannt abgelehnt — dieselbe Regel wie der Rechenweg
        /// (Mengengeruest.TagesbedarfManuellGueltig, Zirkulationskanal.ManuellGueltig), keine zweite
        /// Regelsammlung in der Hülle.
        /// </summary>
        [Fact]
        public void Manueller_Tagesbedarf_und_manuelle_Zirkulation_ohne_gueltigen_Wert_werden_abgelehnt()
        {
            var a = new ZapfprofilZonenangabenDaten { TagesbedarfAuto = false };
            var e = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone", IdNutzungsart = 1, Bezugsmenge = 10, Angaben = a } },
                Gebaeude = new ZapfprofilGebaeudeDaten { ZirkAuto = false }
            };

            string[] kennungen = ZapfprofilHuelle.Pruefen(e).Select(m => m.Kennung).ToArray();
            Assert.Contains("ZPG_MSG_TAGESBEDARF_MANUELL_UNGUELTIG", kennungen);
            Assert.Contains("ZPG_MSG_ZIRKULATION_MANUELL_UNGUELTIG", kennungen);

            a.TagesbedarfManuellKwh = 5.0;
            e.Gebaeude.ZirkManuellKw = 1.0;
            string[] frei = ZapfprofilHuelle.Pruefen(e).Select(m => m.Kennung).ToArray();
            Assert.DoesNotContain("ZPG_MSG_TAGESBEDARF_MANUELL_UNGUELTIG", frei);
            Assert.DoesNotContain("ZPG_MSG_ZIRKULATION_MANUELL_UNGUELTIG", frei);

            a.TagesbedarfManuellKwh = -1.0;
            e.Gebaeude.ZirkManuellKw = double.NaN;
            kennungen = ZapfprofilHuelle.Pruefen(e).Select(m => m.Kennung).ToArray();
            Assert.Contains("ZPG_MSG_TAGESBEDARF_MANUELL_UNGUELTIG", kennungen);
            Assert.Contains("ZPG_MSG_ZIRKULATION_MANUELL_UNGUELTIG", kennungen);

            // "auto" braucht keinen Wert.
            a.TagesbedarfAuto = true;
            e.Gebaeude.ZirkAuto = true;
            kennungen = ZapfprofilHuelle.Pruefen(e).Select(m => m.Kennung).ToArray();
            Assert.DoesNotContain("ZPG_MSG_TAGESBEDARF_MANUELL_UNGUELTIG", kennungen);
            Assert.DoesNotContain("ZPG_MSG_ZIRKULATION_MANUELL_UNGUELTIG", kennungen);
        }

        /// <summary>
        /// Z4, Gruppe 2a Punkt 6: Eine Zone einer Nicht-Wohnen-Nutzungsart (Bezugsart weder
        /// Wohneinheiten noch Personen) zeigt keine Wohnungstabelle — trägt sie dennoch (verdeckte)
        /// Zeilen ohne Anzahl, hält „OK" nicht an: Außerhalb der wirksamen Bezugsart wird die
        /// Tabelle weder gerechnet noch geprüft.
        /// </summary>
        [Fact]
        public void Eine_verdeckte_Wohnungstabelle_einer_Nicht_Wohnen_Zone_haelt_OK_nicht_an()
        {
            var a = new ZapfprofilZonenangabenDaten();
            a.Wohnungen.Add(new ZapfprofilWohnungDaten());               // keine Anzahl — verdeckt, da nicht Wohnen
            var e = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Büro", IdNutzungsart = 2, Bezugsmenge = 10, Angaben = a } }
            };
            var katalog = new List<ZapfprofilNutzungsartDaten> { new() { Id = 2, Bezugsart = (int)ZapfBezugsart.Beschaeftigte } };

            Assert.Empty(ZapfprofilHuelle.Pruefen(e, katalog));
        }

        [Fact]
        public void Die_geteilten_Groessen_gleichen_sich_mit_der_Auslegung_an()
        {
            var g = new ZapfprofilGebaeudeDaten { LadeAuto = false, LadeManuellKw = 12.0, LadefensterH = 8.0, SpeicherC = 58.0, ZirkLaengeM = 50.0 };
            var a = new ZapfprofilAuslegungEingabeDaten();
            g.InAuslegung(a);
            Assert.False(a.LadeAuto);
            Assert.Equal(12.0, a.LadeManuellKw);
            Assert.Equal(8.0, a.LadefensterH);
            Assert.Equal(58.0, a.SpeicherC);

            a.LadefensterH = 10.0;
            a.SpeicherC = null;
            g.AusAuslegung(a);
            Assert.Equal(10.0, g.LadefensterH);
            Assert.Null(g.SpeicherC);
            Assert.Equal(50.0, g.ZirkLaengeM);   // nicht geteilt: bleibt

            Assert.Equal(0, new ZapfprofilGebaeudeDaten().Ueberschrieben(null));
            // Das Paar Ladeleistung auto/manuell zählt einmal, dazu das Fenster; die Zirkulationslänge
            // gehört zur Methode Leitungslänge, nicht zur gewählten (Vorgabe Flächenkennwert) und zählt nicht.
            Assert.Equal(2, g.Ueberschrieben(new ZapfprofilGebaeudeDaten()));
        }

        // =================================================================================
        // Auf der Testdatenbank
        // =================================================================================

        [Fact]
        public void Ohne_Projektzeile_entsteht_eine_nur_wenn_das_Gebaeude_abweicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilGebaeudeDaten vorgabe = ZapfprofilHuelle.AlsGebaeude(ZapfprofilCtrl.ProjektVorgabe());
            Assert.NotNull(vorgabe);
            Assert.True(vorgabe.ZirkAuto);
            Assert.True(vorgabe.LadeAuto);
            Assert.Equal(ZapfprofilZirkulationsmethode.Flaechenkennwert, vorgabe.ZirkMethode);

            Assert.Null(ZapfprofilHuelle.MitGebaeude(null, vorgabe.Kopie()));
            ZapfprofilGebaeudeDaten anders = vorgabe.Kopie();
            anders.LeitungsinhaltL = 5.0;
            ProjektStand neu = ZapfprofilHuelle.MitGebaeude(null, anders);
            Assert.NotNull(neu);
            Assert.Equal(5.0, neu.LeitungsinhaltL);
            Assert.Equal(ZapfprofilCtrl.ProjektVorgabe().Seed, neu.Seed);
        }

        [Fact]
        public void Der_Stand_beim_Oeffnen_traegt_Vorgaben_Kataloge_und_Gebaeude()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilDaten d = ZapfprofilHuelle.Laden(PROJEKT, null);
            Assert.True(d.Verfuegbar);

            // Ohne Projektzeile beginnt der Dialog mit den Vorgaben der DDL — und zählt sie nicht.
            Assert.NotNull(d.GebaeudeVorgabe);
            Assert.NotNull(d.Eingabe.Gebaeude);
            Assert.Equal(0, d.Eingabe.Gebaeude.Ueberschrieben(d.GebaeudeVorgabe));

            // Die Vorgaben des Parametersatzes (Testkatalog).
            Assert.Equal(11.0, d.Vorgaben.KaltwasserMittelC);
            Assert.Equal(3.0, d.Vorgaben.KaltwasserAmplitudeK);
            Assert.Equal(80.0, d.Vorgaben.WohnflaecheJeWeM2);
            Assert.Equal(20.0, d.Vorgaben.ZirkLaufzeitH);
            Assert.Equal(1, d.Vorgaben.ZirkLage);
            Assert.Equal(8.0, d.Vorgaben.LadefensterH);                       // Vorlage V4, freier Paketteil (N28)
            Assert.Equal(12.0, d.Vorgaben.KaltwasserAuslegungC);

            Assert.Equal(12, d.Monatsnamen.Count);
            Assert.Equal("Januar", d.Monatsnamen[0]);
            Assert.True(d.Tagesgangsaetze.Count >= 1);
            Assert.All(d.Tagesgangsaetze, s => Assert.Contains(" · ", s.Name));
            Assert.Contains(d.Ausstattungen, x => x.Name == "Testklasse A");
            ZapfprofilKatalogeintragDaten haus = Assert.Single(d.Gebaeude);
            Assert.Equal("EFH-A-TS-212", haus.Name);

            // Der Katalog nennt Kalender und ob die Nutzungsart eine Wohnungstabelle führt.
            ZapfprofilNutzungsartDaten a = d.Katalog.Single(n => n.Name == "Testnutzung A (fiktiv)");
            Assert.Equal("Wohnen", a.Kalender);
            Assert.True(a.Wohnen);
            Assert.Equal(50.0, a.ZapftemperaturC);
            Assert.Equal(12, a.Monatsfaktoren.Length);
            Assert.NotNull(a.IdTagesgangsatz);
            Assert.False(d.Katalog.Single(n => n.Name == "Testnutzung B (fiktiv)").Wohnen);
        }

        [Fact]
        public void Die_Zonenliste_nennt_wirksame_Menge_Anteil_und_Rechenweg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilDaten d = ZapfprofilHuelle.Laden(PROJEKT, null);
            int a = d.Katalog.Single(n => n.Name == "Testnutzung A (fiktiv)").Id;
            int b = d.Katalog.Single(n => n.Name == "Testnutzung B (fiktiv)").Id;

            var wohnen = new ZapfprofilZonenangabenDaten();
            wohnen.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 4, Personen = 2.0 });
            wohnen.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 2, Personen = 3.0 });
            var manuell = new ZapfprofilZonenangabenDaten { TagesbedarfAuto = false, TagesbedarfManuellKwh = 20.0 };
            var eingabe = new ZapfprofilEingabeDaten
            {
                Zonen =
                {
                    new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = a, Bezugsmenge = 1, Angaben = wohnen },
                    new ZapfprofilZoneDaten { Name = "Büro", IdNutzungsart = b, Bezugsmenge = 50, Angaben = manuell }
                }
            };
            ZapfprofilVorschauDaten v = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, v.Zustand);

            ZapfprofilZonenwertDaten w = v.Zonen[0];
            Assert.Equal(14.0, w.BezugsmengeWirksam);                     // 4 · 2 + 2 · 3 Personen
            Assert.Equal(ZapfprofilZonenrechenweg.Katalog, w.Rechenweg);
            Assert.Equal(ZapfprofilZonenrechenweg.Manuell, v.Zonen[1].Rechenweg);
            Assert.Equal(1.0, v.Zonen.Sum(z => z.Anteil ?? 0.0), 9);
            Assert.Contains(v.Warnliste, x => x.Kennung == "ZPG_WARN_BEZUGSMENGE_WOHNUNGSTABELLE");
        }

        [Fact]
        public void Die_Auslegung_beginnt_mit_den_Gebaeudegroessen_und_traegt_die_Stufe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilDaten d = ZapfprofilHuelle.Laden(PROJEKT, null);
            int a = d.Katalog.Single(n => n.Name == "Testnutzung A (fiktiv)").Id;
            ZapfprofilEingabeDaten e = d.Eingabe;
            e.Zonen.Add(new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = a, Bezugsmenge = 20 });
            e.Gebaeude.LadefensterH = 7.0;
            e.Gebaeude.SpeicherC = 61.0;
            e.Stufe = ZapfprofilStufe.Erweitert;

            var gaben = (Func<ZapfprofilEingabeDaten, bool, IReadOnlyDictionary<string, object>>)
                        ZapfprofilHuelle.Gaben(PROJEKT, ZapfprofilCtrl.Lies(PROJEKT))["AuslegungGaben"];
            var start = (ZapfprofilAuslegungStartDaten)gaben(e, false)["Daten"];
            Assert.Equal(ZapfprofilStufe.Erweitert, start.Stufe);
            Assert.Equal(7.0, start.Eingabe.LadefensterH);
            Assert.Equal(61.0, start.Eingabe.SpeicherC);
            Assert.NotNull(start.Ergebnis);
            Assert.All(start.Ergebnis.Gruppen, g => Assert.False(g.Empfehlung.Schnellauslegung));
        }
    }
}
