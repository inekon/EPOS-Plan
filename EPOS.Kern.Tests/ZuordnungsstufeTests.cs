using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BA-1 — die Zuordnungsstufe je Bauteil</b> (Konzept Bauteilaufbau beim Import 5.2): A vollständiger relevanter
    /// Aufbau, B U-Wert der Datei ohne vollständigen Aufbau, C nur Geometrie; transparent nicht bewertet. Dazu, was
    /// einem Aufbau fehlt, und die Summenzeilen des Protokolls — der Vertrag für die Oberfläche (BA-3).
    /// </summary>
    public sealed class ZuordnungsstufeTests
    {
        internal static GebaeudeBauteilvorschlag SyntheseMitVierWaenden()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();   // Dach und Boden mit U der Datei, ohne Schichten
            AbbildBauteil voll = BauteilvorschlagProbe.Flaeche("w-a", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 20, null, 90, 180, "R1");
            voll.Aufbau = BauteilvorschlagProbe.Massiv("kon-massiv");
            a.Gebaeude[0].Bauteile.Add(voll);

            AbbildBauteil nurU = BauteilvorschlagProbe.Flaeche("w-b", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, 0.28, 90, 90, "R1");
            a.Gebaeude[0].Bauteile.Add(nurU);

            // Schichtsatz ohne λ der Dämmung — unvollständig, U der Datei.
            var luecke = new AbbildAufbau { Kennung = "kon-luecke", Name = "Wand ohne Lambda", Status = Aufbaustatus.Unvollstaendig };
            luecke.Schichten.Add(new AbbildSchicht { Name = "Dämmung", DickeM = 0.14, RhoKgM3 = 30, CpJkgK = 1030 });
            luecke.Schichten.Add(new AbbildSchicht { Name = "Beton", DickeM = 0.2, LambdaWmK = 2.0, RhoKgM3 = 2400, CpJkgK = 1000 });
            AbbildBauteil halb = BauteilvorschlagProbe.Flaeche("w-b2", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 12, 0.24, 90, 0, "R1");
            halb.Aufbau = luecke;
            a.Gebaeude[0].Bauteile.Add(halb);

            AbbildBauteil nichts = BauteilvorschlagProbe.Flaeche("w-c", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 8, null, 90, 270, "R1");
            a.Gebaeude[0].Bauteile.Add(nichts);

            AbbildBauteil fenster = BauteilvorschlagProbe.Flaeche("f-1", Bauteilart.Fenster, Randbedingung.Aussenluft, 2, 1.1, 90, 180, "R1");
            a.Gebaeude[0].Bauteile.Add(fenster);
            return BauteilvorschlagProbe.Bilden(a, 'E');
        }

        [Fact]
        public void Synthetische_Bauteile_tragen_die_Stufen_A_B_C_und_was_fehlt()
        {
            GebaeudeBauteilvorschlag v = SyntheseMitVierWaenden();
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Select(m => m.ToString())));

            GebaeudeBauteilzeile a = BauteilvorschlagProbe.Zeile(v, "w-a");
            Assert.Equal(Bauteilzuordnungsstufe.A, a.Stufe);
            Assert.Equal(Aufbauluecke.Keine, a.Fehlt);

            GebaeudeBauteilzeile b = BauteilvorschlagProbe.Zeile(v, "w-b");
            Assert.Equal(Bauteilzuordnungsstufe.B, b.Stufe);
            Assert.Equal(Aufbauluecke.KeineSchichten | Aufbauluecke.Ersatzaufbau, b.Fehlt);
            Assert.Equal(0.28, b.Bauteil.U_Wert);

            GebaeudeBauteilzeile b2 = BauteilvorschlagProbe.Zeile(v, "w-b2");
            Assert.Equal(Bauteilzuordnungsstufe.B, b2.Stufe);
            Assert.True(b2.Fehlt.HasFlag(Aufbauluecke.Lambda));
            Assert.True(b2.Fehlt.HasFlag(Aufbauluecke.Daemmung));            // λ der Dämmung unbekannt: keine erkennbar
            Assert.False(b2.Fehlt.HasFlag(Aufbauluecke.SpeicherndeSchicht)); // der Beton speichert
            Assert.False(b2.Fehlt.HasFlag(Aufbauluecke.KeineSchichten));

            GebaeudeBauteilzeile c = BauteilvorschlagProbe.Zeile(v, "w-c");
            Assert.Equal(Bauteilzuordnungsstufe.C, c.Stufe);
            Assert.Equal(Aufbauluecke.KeineSchichten | Aufbauluecke.Ersatzaufbau, c.Fehlt);
            Assert.True(ImportherkunftWerte.IstVorgabe(c.HerkunftU));

            Assert.Equal(Bauteilzuordnungsstufe.Transparent, BauteilvorschlagProbe.Zeile(v, "f-1").Stufe);
            Assert.Equal(Bauteilzuordnungsstufe.B, BauteilvorschlagProbe.Zeile(v, "dach").Stufe);
        }

        [Fact]
        public void Summen_je_Stufe_und_die_Protokollzeilen()
        {
            GebaeudeBauteilvorschlag v = SyntheseMitVierWaenden();
            IReadOnlyList<Zuordnungssumme> s = Bauteilzuordnung.Summen(v.Zeilen);
            Assert.Equal(new[] { Bauteilzuordnungsstufe.A, Bauteilzuordnungsstufe.B, Bauteilzuordnungsstufe.C }, s.Select(x => x.Stufe));
            Assert.Equal(v.Zeilen.Count(z => z.Stufe != Bauteilzuordnungsstufe.Transparent), s.Sum(x => x.Zahl));
            Zuordnungssumme sa = s[0], sb = s[1], sc = s[2];
            Assert.Equal(1, sa.Zahl);
            Assert.Equal(4, sb.Zahl);   // Dach, Boden, w-b, w-b2
            Assert.Equal(1, sc.Zahl);
            BauteilvorschlagProbe.Nah(v.Zeilen.Where(z => z.Stufe == Bauteilzuordnungsstufe.B).Sum(z => z.Bauteil.Flaeche), sb.Flaeche_M2, 1e-12);

            PruefMeldung ma = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STUFE_A);
            Assert.Equal(PruefStufe.Info, ma.Stufe);
            Assert.Equal(new[] { "1", sa.Flaeche_M2.ToString("0.######", CultureInfo.InvariantCulture) }, ma.Werte);
            Assert.Equal("4", Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STUFE_B).Werte[0]);
            Assert.Equal("1", Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STUFE_C).Werte[0]);
        }

        [Fact]
        public void Gespeicherte_Zeilen_bekommen_ihre_Stufe_aus_Aufbau_U_und_Herkunft()
        {
            Assert.Equal(Bauteilzuordnungsstufe.A, Bauteilzuordnung.Stufe(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, ID_Aufbau = 5, U_Wert = 0.3 }, null));
            Assert.Equal(Bauteilzuordnungsstufe.B, Bauteilzuordnung.Stufe(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DACH, U_Wert = 0.2, Herkunft = DbWerte.HERKUNFT_IFC }, null));
            Assert.Equal(Bauteilzuordnungsstufe.C, Bauteilzuordnung.Stufe(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_DACH, U_Wert = 0.2, Herkunft = DbWerte.HERKUNFT_VORGABE }, null));
            Assert.Equal(Bauteilzuordnungsstufe.C, Bauteilzuordnung.Stufe(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_INNENWAND }, null));
            Assert.Equal(Bauteilzuordnungsstufe.Transparent, Bauteilzuordnung.Stufe(new BauteilModel { Bauteilart = DbWerte.BAUTEILART_FENSTER, U_Wert = 1.1 }, null));
        }

        [Fact]
        public void Was_fehlt_Daemmung_speichernde_Schicht_und_Stoffwerte()
        {
            Assert.Equal(Aufbauluecke.KeineSchichten, Bauteilzuordnung.Luecken(Array.Empty<Schichtwerte>(), true));
            // Holzständer mit Gipskarton: gedämmt, aber ohne speichernde Schicht (11 kJ/(m²K) < 20).
            var leicht = new[] { new Schichtwerte(0.0125, 0.25, 900, 1000), new Schichtwerte(0.16, 0.035, 30, 1030) };
            Assert.Equal(Aufbauluecke.SpeicherndeSchicht, Bauteilzuordnung.Luecken(leicht, true));
            // Ungedämmtes Mauerwerk: als Hüllbauteil ohne Dämmung, innen ohne Befund.
            var massiv = new[] { new Schichtwerte(0.015, 0.7, 1400, 1000), new Schichtwerte(0.24, 0.99, 1800, 1000) };
            Assert.Equal(Aufbauluecke.Daemmung, Bauteilzuordnung.Luecken(massiv, true));
            Assert.Equal(Aufbauluecke.Keine, Bauteilzuordnung.Luecken(massiv, false));
            // Fehlende Stoffwerte je Größe; eine ruhende Luftschicht braucht keine.
            var luecken = new[] { new Schichtwerte(null, 0.7, null, 1000), new Schichtwerte(0.24, 0.99, 1800, null),
                                  new Schichtwerte(0.04, null, null, null, true) };
            Aufbauluecke l = Bauteilzuordnung.Luecken(luecken, false);
            Assert.Equal(Aufbauluecke.Dicke | Aufbauluecke.Rohdichte | Aufbauluecke.Waermekapazitaet | Aufbauluecke.SpeicherndeSchicht, l);
        }

        [Fact]
        public void Importproben_mit_Schichtsaetzen_A_B_und_C()
        {
            // IFC4 mit vollständigen Stoffwerten (IfcMaterialLayerSet, Pset_MaterialThermal): opake Bauteile mit Aufbau → A.
            GebaeudeBauteilvorschlag voll = BauteilvorschlagProbe.Vorschlag("ifc4_schichten.ifc", 0, 'E');
            Assert.False(voll.Abgelehnt);
            GebaeudeBauteilzeile[] opak = voll.Zeilen.Where(z => z.Stufe != Bauteilzuordnungsstufe.Transparent).ToArray();
            Assert.Contains(opak, z => z.Stufe == Bauteilzuordnungsstufe.A);
            Assert.All(opak.Where(z => z.Kennung != null), z => Assert.Equal(Bauteilzuordnungsstufe.A, z.Stufe));
            Assert.All(opak.Where(z => z.Stufe == Bauteilzuordnungsstufe.A), z => Assert.Equal(Aufbauluecke.Keine, z.Fehlt));

            // Dieselben Schichten mit ρ = 0 und c = 0 (außerhalb des Bandes): U aus der masselosen Schichtung → B, es fehlen ρ und c.
            GebaeudeBauteilvorschlag null0 = BauteilvorschlagProbe.Vorschlag("ifc4_schichten_nullwerte.ifc", 0, 'E');
            GebaeudeBauteilzeile[] opakN = null0.Zeilen.Where(z => z.Stufe != Bauteilzuordnungsstufe.Transparent && z.Kennung != null).ToArray();
            Assert.NotEmpty(opakN);
            Assert.All(opakN, z => Assert.Equal(Bauteilzuordnungsstufe.B, z.Stufe));
            Assert.Contains(opakN, z => z.Fehlt.HasFlag(Aufbauluecke.Rohdichte) && z.Fehlt.HasFlag(Aufbauluecke.Waermekapazitaet));

            // IFC2X3: Stoffwerte werden nicht gelesen, die Datei trägt keinen U-Wert → Vorgabe der Klasse, C.
            GebaeudeBauteilvorschlag x3 = BauteilvorschlagProbe.Vorschlag("ifc2x3_schichten.ifc", 0, 'E');
            GebaeudeBauteilzeile[] opakX = x3.Zeilen.Where(z => z.Stufe != Bauteilzuordnungsstufe.Transparent && z.Kennung != null).ToArray();
            Assert.NotEmpty(opakX);
            Assert.All(opakX, z => Assert.Equal(Bauteilzuordnungsstufe.C, z.Stufe));
            Assert.All(opakX, z => Assert.True(z.Fehlt.HasFlag(Aufbauluecke.Lambda), z.ToString()));

            // Je Probe stehen die Summenzeilen im Protokoll und decken alle bewerteten Zeilen.
            foreach (GebaeudeBauteilvorschlag v in new[] { voll, null0, x3 })
            {
                int imProtokoll = v.Meldungen.Where(m => m.Schluessel == GebaeudeBauteilvorschlag.STUFE_A || m.Schluessel == GebaeudeBauteilvorschlag.STUFE_B
                                                         || m.Schluessel == GebaeudeBauteilvorschlag.STUFE_C)
                                             .Sum(m => int.Parse(m.Werte[0], CultureInfo.InvariantCulture));
                Assert.Equal(v.Zeilen.Count(z => z.Stufe != Bauteilzuordnungsstufe.Transparent), imProtokoll);
            }
        }
    }
}
