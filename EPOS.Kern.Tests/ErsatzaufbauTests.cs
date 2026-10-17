using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>Die Aufbauten eines Vorschlags aus der Datei — ohne die Ersatzaufbauten der Stufen B und C (BA-2).</summary>
    internal static class ErsatzaufbauProbe
    {
        internal static List<GebaeudeAufbauzeile> Dateiaufbauten(this GebaeudeBauteilvorschlag v)
            => v.Aufbauten.Where(x => x.Ersatz == null).ToList();
    }

    /// <summary>
    /// <b>BA-2 — der Ersatzaufbau</b> (Konzept Bauteilaufbau beim Import 5.3, E95-3/4/5): Wahl des Typs aus Bauart,
    /// Baualtersklasse und Bauteilart, Abgleich der Dämmdicke bzw. des λ auf das Ziel-U (geschlossen über R, trifft auf
    /// 1e-3), Bandgrenzen mit Vermerk, Stufe B/C statt A, keine masselosen Außenbauteile mehr im Vorschlag.
    /// </summary>
    public sealed class ErsatzaufbauTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static double Neigung(TypaufbauSaat t)
            => t.Bauteilart == DbWerte.BAUTEILART_DACH ? 0.0 : t.Bauteilart == DbWerte.BAUTEILART_BODENPLATTE ? 180.0 : 90.0;

        private static Bauteilrand Rand(TypaufbauSaat t)
            => t.Code == TypaufbauSaattabelle.BO_DAEMMUNG_OBEN ? Bauteilrand.Erdreich
             : t.Code == TypaufbauSaattabelle.BO_DAEMMUNG_UNTEN ? Bauteilrand.Unbeheizt : Bauteilrand.Aussenluft;

        /// <summary>Jeder gedämmte Typ trifft das Ziel-U über die Dämmdicke im Band, die übrigen Schichten bleiben.</summary>
        [Fact]
        public void Die_Daemmdicke_trifft_das_U_der_Datei()
        {
            foreach (TypaufbauSaat t in TypaufbauSaattabelle.Alle.Where(x => x.Gedaemmt))
            {
                foreach (double uZiel in new[] { 0.15, 0.24, 0.35, 0.5 })
                {
                    Ersatzergebnis e = Ersatzaufbau.Abgleichen(t, Rand(t), Neigung(t), uZiel);
                    string wer = t.Code + " U " + uZiel.ToString(CultureInfo.InvariantCulture);
                    Assert.Equal(Ersatzvermerk.Keiner, e.Vermerk);
                    Assert.NotNull(e.Wert);
                    Assert.InRange(e.Wert.Value, 0.0, TypaufbauSaat.DAEMMDICKE_MAX_M);
                    Assert.True(Math.Abs(e.USchichten.Value - uZiel) < 1e-3, wer + ": " + e.USchichten);
                    Assert.Equal(e.Wert.Value, e.Aufbau.Schichten[t.Abgleichschicht].Dicke, 12);
                    for (int i = 0; i < t.Schichten.Count; i++)
                        if (i != t.Abgleichschicht) Assert.Equal(t.Schichten[i].Dicke_M, e.Aufbau.Schichten[i].Dicke);
                    Assert.Equal(t.Code, e.Aufbau.Typaufbau);
                }
            }
        }

        /// <summary>Der ungedämmte Typ trifft das Ziel-U über das λ der tragenden Schicht; die Masse bleibt.</summary>
        [Theory]
        [InlineData(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, 1.2)]
        [InlineData(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, 1.8)]
        [InlineData(TypaufbauSaattabelle.AW_MONOLITHISCH, 0.6)]
        public void Das_Lambda_der_tragenden_Schicht_trifft_das_U(string code, double uZiel)
        {
            TypaufbauSaat t = TypaufbauSaattabelle.Zu(code);
            Ersatzergebnis e = Ersatzaufbau.Abgleichen(t, Bauteilrand.Aussenluft, 90.0, uZiel);
            Assert.Equal(Ersatzvermerk.Keiner, e.Vermerk);
            Assert.True(Math.Abs(e.USchichten.Value - uZiel) < 1e-3, e.USchichten.ToString());
            BauteilschichtModel s = e.Aufbau.Schichten[t.Abgleichschicht];
            Assert.Equal(e.Wert.Value, s.Lambda.Value, 12);
            Assert.Equal(t.Schichten[t.Abgleichschicht].Baustoff.Rho, s.Rho);
            Assert.Equal(t.Schichten[t.Abgleichschicht].Dicke_M, s.Dicke);
        }

        /// <summary>Außerhalb des Bands bleibt der Typ ohne Abgleich (mit Vermerk); über dem U ohne Dämmung entfällt sie.</summary>
        [Fact]
        public void Bandgrenzen_mit_Vermerk()
        {
            TypaufbauSaat wdvs = TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT);
            Ersatzergebnis dick = Ersatzaufbau.Abgleichen(wdvs, Bauteilrand.Aussenluft, 90.0, 0.08);   // bräuchte rund 44 cm
            Assert.Equal(Ersatzvermerk.AusserhalbBand, dick.Vermerk);
            Assert.Null(dick.Wert);
            Assert.True(dick.Bedarf > TypaufbauSaat.DAEMMDICKE_MAX_M);
            Assert.Equal(0.14, dick.Aufbau.Schichten[2].Dicke);                                     // Vorgabedicke

            TypaufbauSaat dach = TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.DA_STAHLBETON_GEDAEMMT);
            Ersatzergebnis ohne = Ersatzaufbau.Abgleichen(dach, Bauteilrand.Aussenluft, 0.0, 5.0);
            Assert.Equal(Ersatzvermerk.DaemmungEntfaellt, ohne.Vermerk);
            Assert.Equal(3, ohne.Aufbau.Schichten.Count);
            Assert.DoesNotContain(ohne.Aufbau.Schichten, s => s.Lambda <= Bauteilzuordnung.DAEMMUNG_LAMBDA_MAX);
            Assert.Equal(new[] { 1, 2, 3 }, ohne.Aufbau.Schichten.Select(s => s.Reihenfolge));

            TypaufbauSaat alt = TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT);
            Ersatzergebnis hoch = Ersatzaufbau.Abgleichen(alt, Bauteilrand.Aussenluft, 90.0, 5.0);   // λ über dem 3-Fachen
            Assert.Equal(Ersatzvermerk.AusserhalbBand, hoch.Vermerk);
            Assert.Equal(0.81, hoch.Aufbau.Schichten[1].Lambda);
        }

        /// <summary>Wahl des Typs (E95-4): Bauart leicht, bis Klasse F ungedämmt, ab G gedämmt; Wechsel nach dem U.</summary>
        [Theory]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Aussenluft, 90.0, "SCHWER", 'E', 1.4, TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, false)]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Aussenluft, 90.0, "SCHWER", 'E', 0.3, TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT, true)]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Aussenluft, 90.0, "SCHWER", 'H', 0.3, TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT, false)]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Aussenluft, 90.0, "SEHR_SCHWER", 'K', 3.0, TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, true)]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Aussenluft, 90.0, "LEICHT", 'E', 0.3, TypaufbauSaattabelle.AW_HOLZLEICHTBAU, false)]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Erdreich, 90.0, "SCHWER", null, 2.0, TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, false)]
        [InlineData((int)Bauteilart.Aussenwand, (int)Bauteilrand.Aussenluft, 90.0, null, null, 0.5, TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT, false)]
        [InlineData((int)Bauteilart.Dach, (int)Bauteilrand.Aussenluft, 30.0, "SCHWER", 'C', 1.0, TypaufbauSaattabelle.DA_STAHLBETON_GEDAEMMT, false)]
        [InlineData((int)Bauteilart.Dach, (int)Bauteilrand.Aussenluft, 30.0, "LEICHT", 'M', 0.14, TypaufbauSaattabelle.DA_SPARRENDACH, false)]
        [InlineData((int)Bauteilart.Decke, (int)Bauteilrand.Unbeheizt, 0.0, "SCHWER", 'G', 0.4, TypaufbauSaattabelle.DA_STAHLBETON_GEDAEMMT, false)]
        [InlineData((int)Bauteilart.Decke, (int)Bauteilrand.Unbeheizt, 180.0, "SCHWER", 'G', 0.5, TypaufbauSaattabelle.BO_DAEMMUNG_UNTEN, false)]
        [InlineData((int)Bauteilart.Bodenplatte, (int)Bauteilrand.Erdreich, 180.0, "SCHWER", 'D', 1.0, TypaufbauSaattabelle.BO_DAEMMUNG_OBEN, false)]
        public void Der_Typ_folgt_Bauart_Klasse_und_Bauteilart(int art, int rand, double neigung, string bauart, object klasse,
                                                              double uZiel, string code, bool wechsel)
        {
            char? k = klasse is char c ? c : (char?)null;
            Ersatzergebnis e = Ersatzaufbau.Bilden((Bauteilart)art, (Bauteilrand)rand, neigung, bauart, k, uZiel);
            Assert.Equal(code, e.Typ.Code);
            Assert.Equal(wechsel, (e.Vermerk & Ersatzvermerk.TypNachU) != 0);
            if ((e.Vermerk & Ersatzvermerk.AusserhalbBand) == 0)
                Assert.True(Math.Abs(e.USchichten.Value - uZiel) < 1e-3, e.Typ.Code + ": " + e.USchichten);
        }

        [Fact]
        public void Innenbauteile_Tueren_und_Fenster_bekommen_keinen_die_Huelle_gegen_unbeheizt_schon()
        {
            Assert.False(Ersatzaufbau.Erhaelt(Bauteilart.Innenwand, Bauteilrand.Innen));
            Assert.False(Ersatzaufbau.Erhaelt(Bauteilart.Decke, Bauteilrand.Innen));
            Assert.False(Ersatzaufbau.Erhaelt(Bauteilart.Aussenwand, Bauteilrand.Innen));
            Assert.False(Ersatzaufbau.Erhaelt(Bauteilart.Tuer, Bauteilrand.Aussenluft));
            Assert.False(Ersatzaufbau.Erhaelt(Bauteilart.Fenster, Bauteilrand.Aussenluft));
            Assert.True(Ersatzaufbau.Erhaelt(Bauteilart.Aussenwand, Bauteilrand.Aussenluft));
            Assert.True(Ersatzaufbau.Erhaelt(Bauteilart.Bodenplatte, Bauteilrand.Erdreich));
            Assert.True(Ersatzaufbau.Erhaelt(Bauteilart.Decke, Bauteilrand.Unbeheizt));
            Assert.True(Ersatzaufbau.Erhaelt(Bauteilart.Innenwand, Bauteilrand.Unbeheizt));   // Hülle gegen unbeheizt
        }

        /// <summary>
        /// Im Vorschlag: Stufe B und C tragen einen Ersatzaufbau und bleiben B bzw. C (nicht A); kein opakes Außenbauteil
        /// bleibt ohne Aufbau (die gemischte Gruppe rechnet keines masselos); je Typ und U ein Aufbau; das Protokoll zählt.
        /// </summary>
        [Fact]
        public void Stufe_B_und_C_mit_Ersatzaufbau_bleiben_B_und_C()
        {
            GebaeudeBauteilvorschlag v = ZuordnungsstufeTests.SyntheseMitVierWaenden();
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Select(m => m.ToString())));
            IReadOnlyDictionary<int, BauteilaufbauModel> jeId = v.AufbautenJeId;

            GebaeudeBauteilzeile a = BauteilvorschlagProbe.Zeile(v, "w-a");
            Assert.Null(a.Typaufbau);
            Assert.Equal(Bauteilzuordnungsstufe.A, a.Stufe);
            Assert.Equal(Bauteilzuordnungsstufe.A, Bauteilzuordnung.Stufe(a.Bauteil, jeId));

            foreach (string kennung in new[] { "w-b", "w-b2", "dach" })
            {
                GebaeudeBauteilzeile z = BauteilvorschlagProbe.Zeile(v, kennung);
                Assert.NotNull(z.Typaufbau);
                Assert.NotNull(z.Bauteil.ID_Aufbau);
                Assert.Equal(Bauteilzuordnungsstufe.B, z.Stufe);
                Assert.Equal(Bauteilzuordnungsstufe.B, Bauteilzuordnung.Stufe(z.Bauteil, jeId));
                Assert.True(z.Fehlt.HasFlag(Aufbauluecke.Ersatzaufbau), kennung);
                Assert.Equal(Importherkunft.Vorgabe, z.HerkunftAufbau);
                Assert.True(Math.Abs(z.USchichten.Value - z.Bauteil.U_Wert.Value) < 1e-3, kennung);   // U der Datei getroffen
                Assert.False(z.UAbweichungHinweis);
            }
            GebaeudeBauteilzeile c = BauteilvorschlagProbe.Zeile(v, "w-c");
            Assert.Equal(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT, c.Typaufbau);              // Klasse E, U der Vorgabe
            Assert.Equal(Bauteilzuordnungsstufe.C, c.Stufe);
            Assert.Equal(Bauteilzuordnungsstufe.C, Bauteilzuordnung.Stufe(c.Bauteil, jeId));
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, c.Bauteil.Herkunft);

            // Keine opake Außenfläche ohne Aufbau: die gemischte Gruppe rechnet kein Bauteil masselos.
            Assert.DoesNotContain(v.Zeilen, z => z.Summenfeld != null && !Bauteilzuordnung.IstTransparent(z.Bauteil.Bauteilart)
                                                 && z.Bauteil.Bauteilart != DbWerte.BAUTEILART_TUER && !z.Bauteil.ID_Aufbau.HasValue);

            // Ersatzaufbauten tragen Code, VORGABE und keine Quellentität; gleiche Kombination teilt einen Aufbau.
            List<GebaeudeAufbauzeile> ersatz = v.Aufbauten.Where(x => x.Ersatz != null).ToList();
            Assert.NotEmpty(ersatz);
            Assert.All(ersatz, x =>
            {
                Assert.Null(x.Quelltyp);
                Assert.Equal(DbWerte.HERKUNFT_VORGABE, x.Aufbau.Herkunft);
                Assert.Equal(x.Ersatz.Typ.Code, x.Aufbau.Typaufbau);
                Assert.Equal(x.Aufbau.Schichten.Select(s => s.ID_Baustoff), x.Stammbaustoffe);
                Assert.False(string.IsNullOrEmpty(x.Aufbau.Beschreibung));
            });
            Assert.Equal(ersatz.Count, ersatz.Select(x => x.Aufbau.Bezeichner).Distinct().Count());

            // Summen zählen den Ersatz in B und C, das Protokoll nennt ihn.
            IReadOnlyList<Zuordnungssumme> s = Bauteilzuordnung.Summen(v.Zeilen);
            Assert.Equal(1, s[0].Zahl);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.ERSATZAUFBAU);
            int mitErsatz = v.Zeilen.Count(z => z.Typaufbau != null);
            Assert.Equal(mitErsatz.ToString(CultureInfo.InvariantCulture), m.Werte[0]);
            Assert.Equal(ersatz.Count.ToString(CultureInfo.InvariantCulture), m.Werte[2]);
            Assert.Equal(s[1].Zahl + s[2].Zahl, mitErsatz);
        }
    }
}
