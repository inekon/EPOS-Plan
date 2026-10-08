using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Fensterrichtung aus dem eigenen Körper</b>: Hat die Wand bzw. das Dach keine Richtung, nimmt ein Fenster bzw. eine
    /// Tür die Richtung des eigenen Körpers — außen über den Raum, an dem die Öffnung liegt, sonst über den
    /// Gebäudeschwerpunkt; ist die Seite nicht bestimmbar, bleibt die Öffnung ohne Azimut. Dazu der Einheitenhinweis am U-Wert:
    /// Er bleibt aus, wenn ein anderer Satz denselben Wert in richtiger Einheit trägt. Proben:
    /// <see cref="IfcProbenErzeuger.Fensterrichtungsproben"/>.
    /// </summary>
    public sealed class IfcFensterrichtungTests : IDisposable
    {
        private const string HAUS = "ifc4_fensterrichtung.ifc", UNBESTIMMT = "ifc4_fensterrichtung_unbestimmt.ifc";
        private static readonly double DACHNEIGUNG = Math.Acos(0.8) * 180.0 / Math.PI;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public IfcFensterrichtungTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string F(double? w) => w.HasValue ? w.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";

        private List<(AbbildBauteil Wirt, AbbildBauteil Oeffnung)> Oeffnungen(GebaeudeImportAblauf a)
        {
            var liste = a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).SelectMany(b => b.Oeffnungen.Select(o => (b, o))).ToList();
            foreach ((AbbildBauteil w, AbbildBauteil o) in liste)
                _aus.WriteLine(o.Name + " in " + w.Name + " (" + w.Art + ", N " + F(w.NeigungGrad) + ", A " + F(w.AzimutGrad) + "): N "
                               + F(o.NeigungGrad) + ", A " + F(o.AzimutGrad));
            return liste;
        }

        [Fact]
        public void Fenster_in_der_Wand_ohne_Richtung_nimmt_den_Azimut_des_eigenen_Koerpers_aussen_ueber_den_Raum()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(HAUS);
            var oeffnungen = Oeffnungen(a);
            (AbbildBauteil wand, AbbildBauteil fenster) = oeffnungen.Single(x => x.Oeffnung.Name == "Fenster Süd");
            Assert.Null(wand.AzimutGrad);
            // Der Gebäudeschwerpunkt liegt südlich der Wand und gäbe Nord; der Raum des Fensters gibt Süd.
            Assert.Equal(180.0, fenster.AzimutGrad.Value, 6);
            Assert.Equal(90.0, fenster.NeigungGrad.Value, 6);
            (_, AbbildBauteil tuer) = oeffnungen.Single(x => x.Oeffnung.Name == "Tür West");
            Assert.Equal(270.0, tuer.AzimutGrad.Value, 6);

            PruefMeldung info = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT");
            _aus.WriteLine("Info: " + string.Join("; ", info.Werte));
            Assert.Equal(PruefStufe.Info, info.Stufe);
            Assert.Equal("3", info.Werte[0]);
            Assert.Equal("3", info.Werte[4]);
            // Die Wände selbst bleiben ohne Richtung, ihre Warnung bleibt.
            Assert.Contains(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SEITE_UNBESTIMMT");
        }

        [Fact]
        public void Ohne_bestimmbare_Seite_bleibt_das_Fenster_ohne_Azimut()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(UNBESTIMMT);
            (AbbildBauteil wand, AbbildBauteil fenster) = Assert.Single(Oeffnungen(a));
            Assert.Null(wand.AzimutGrad);
            Assert.Null(fenster.AzimutGrad);
            Assert.DoesNotContain(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT");
            Assert.Contains(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SEITE_UNBESTIMMT");
        }

        [Fact]
        public void Dachfenster_im_Dach_ohne_Koerper_nimmt_Neigung_und_Azimut_des_eigenen_Koerpers()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(HAUS);
            (AbbildBauteil dach, AbbildBauteil fenster) = Oeffnungen(a).Single(x => x.Oeffnung.Name == "Dachfenster");
            Assert.Equal(Bauteilart.Dach, dach.Art);
            Assert.Null(dach.Koerperflaeche);
            Assert.Equal(DACHNEIGUNG, fenster.NeigungGrad.Value, 6);
            Assert.Equal(180.0, fenster.AzimutGrad.Value, 6);
        }

        [Fact]
        public void Einheitenhinweis_bleibt_aus_wenn_ein_anderer_Satz_denselben_Wert_traegt()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(HAUS);
            List<PruefMeldung> hinweise = a.Abbild.Meldungen.Where(m => m.Schluessel == "IMP_IFC_PROT_UWERT_EINHEIT").ToList();
            foreach (PruefMeldung m in hinweise) _aus.WriteLine(string.Join("; ", m.Werte));
            // Fenster: derselbe Wert im Herstellersatz — kein Hinweis; verworfen wird trotzdem, es gilt der Herstellersatz.
            Assert.DoesNotContain(hinweise, m => m.Werte[1] == "Pset_WindowCommon");
            AbbildBauteil fenster = Oeffnungen(a).Single(x => x.Oeffnung.Name == "Fenster Süd").Oeffnung;
            Assert.Equal(1.1, fenster.UWertWm2K.Value, 9);
            Assert.Equal("Herstellerdaten", fenster.UWertQuelle);
        }

        [Fact]
        public void Einheitenhinweis_bleibt_bei_abweichendem_Wert()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(HAUS);
            PruefMeldung hinweis = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_UWERT_EINHEIT");
            Assert.Equal("1", hinweis.Werte[0]);
            Assert.Equal("Pset_DoorCommon", hinweis.Werte[1]);
            AbbildBauteil tuer = Oeffnungen(a).Single(x => x.Oeffnung.Name == "Tür West").Oeffnung;
            Assert.Equal(1.6, tuer.UWertWm2K.Value, 9);
        }

        // ==================================================================
        //  Probenbytes
        // ==================================================================

        [Fact(Skip = "Erzeuger: schreibt die Proben der Fensterrichtung nach Referenzlaeufe/Importproben — nur von Hand, siehe IfcProbenTests.")]
        public void Erzeuger_schreibt_die_Fensterrichtungsproben()
        {
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Fensterrichtungsproben())
                File.WriteAllBytes(Path.Combine(IfcProbenTests.Ordner(), p.Key), p.Value);
        }

        [Fact]
        public void Die_abgelegten_Fensterrichtungsproben_sind_byte_gleich_neu_erzeugbar()
        {
            var funde = new List<string>();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Fensterrichtungsproben())
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), p.Key);
                if (!File.Exists(pfad)) funde.Add(p.Key + ": fehlt");
                else if (!File.ReadAllBytes(pfad).SequenceEqual(p.Value)) funde.Add(p.Key + ": weicht ab");
                else Assert.True(p.Value.Length < 32 * 1024, p.Key + " ist keine Kleinstdatei");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }
    }
}
