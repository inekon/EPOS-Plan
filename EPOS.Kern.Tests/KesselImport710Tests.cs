using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Satz 710.01 im Import von VDI 3805 Blatt 3</b> (Konzept Kesselkennlinie 3.4, Etappe E1;
    /// Anwenderentscheide F2 und F3 vom 29.09.2026).
    ///
    /// <para><b>Geprüft wird:</b> η₁₀₀ aus Spalte 6 (Rückfall Satz 700 Spalte 26), η₃₀ aus Spalte 7,
    /// die kleinste Leistung aus Spalte 3/4 (auch vertauscht), das Paar mit dem niedrigsten Rücklauf
    /// zuerst; im Katalogmodell die Prozentregel, <c>Brennwert</c> aus der Bauart, die
    /// Brennwertkennlinie bleibt aus, Unplausibles bleibt leer. Die Proben liegen unter
    /// <c>Referenzlaeufe/Importproben/</c> (auch in der CI); dazu eine Gegenprobe gegen ALLE
    /// Herstellerdateien unter <c>VDI-3805-Daten/SPK-Daten/</c>, die nur läuft, wo Git LFS die
    /// Dateien geholt hat.</para>
    /// </summary>
    public class KesselImport710Tests
    {
        // =============================================================================
        //  Teil 1 - der Parser an den Proben
        // =============================================================================

        /// <summary>Fünf Brennwertkessel mit je einem Paar 40/30: η₁₀₀, η₃₀ und kleinste Leistung.</summary>
        [Fact]
        public void Satz_710_01_liefert_Nennlast_Teillast_und_kleinste_Leistung()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_vaillant.vdi"));

            Assert.Equal("96", c._list[0].m_szWirkungsgrad);
            Assert.Equal("107.9", c._list[0].m_szWirkungsgrad30);
            Assert.Equal("6", c._list[0].m_szMindestleistung);
            Assert.Equal("40/30", c._list[0].m_szTemperaturpaar);

            Assert.Equal("98", c._list[3].m_szWirkungsgrad);
            Assert.Equal("108.4", c._list[3].m_szWirkungsgrad30);
            Assert.Equal("14.7", c._list[3].m_szMindestleistung);
            Assert.Equal("", c._list[3].m_szWirkungsgradSatz700);
        }

        /// <summary>Zwei Paare je Kessel (40/30 und 75/60): Es zählt das Paar mit dem niedrigsten Rücklauf.</summary>
        [Fact]
        public void Das_Paar_mit_dem_niedrigsten_Ruecklauf_zaehlt()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_buderus.vdi"));

            Assert.Equal("40/30", c._list[0].m_szTemperaturpaar);
            Assert.Equal("97", c._list[0].m_szWirkungsgrad);
            Assert.Equal("104.6", c._list[0].m_szWirkungsgrad30);
            Assert.Equal("10", c._list[0].m_szMindestleistung);
        }

        /// <summary>
        /// Die Sonderfälle: ohne Teillastwert, kleinste und größte Leistung vertauscht,
        /// Niedertemperaturkessel, Elektrokessel ohne 710.01-Wirkungsgrad (Rückfall auf Satz 700).
        /// </summary>
        [Fact]
        public void Sonderfaelle_des_Satzes_710_01()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_sonderfaelle.vdi"));
            Assert.Equal(4, c._list.Count);

            Attrribute_hk ohneTeillast = c._list[0];
            Assert.Equal("97.8", ohneTeillast.m_szWirkungsgrad);
            Assert.Equal("", ohneTeillast.m_szWirkungsgrad30);
            Assert.Equal("4", ohneTeillast.m_szMindestleistung);
            Assert.Equal("80/60", ohneTeillast.m_szTemperaturpaar);

            Attrribute_hk vertauscht = c._list[1];
            Assert.Equal("3.4", vertauscht.m_szMindestleistung);
            Assert.Equal("108", vertauscht.m_szWirkungsgrad30);

            Attrribute_hk niedertemperatur = c._list[2];
            Assert.Equal("Niedertemperatur-Kessel", niedertemperatur.m_szBauart);
            Assert.Equal("90", niedertemperatur.m_szWirkungsgrad);
            Assert.Equal("89", niedertemperatur.m_szWirkungsgrad30);

            Attrribute_hk elektro = c._list[3];
            Assert.Equal("99.5", elektro.m_szWirkungsgradSatz700);
            Assert.Equal("99.5", elektro.m_szWirkungsgrad);
            Assert.Equal("", elektro.m_szWirkungsgrad30);
            Assert.Equal("2.34", elektro.m_szMindestleistung);
            Assert.Equal("", elektro.m_szTemperaturpaar);
        }

        // =============================================================================
        //  Teil 2 - das Katalogmodell
        // =============================================================================

        /// <summary>
        /// Das Katalogmodell: Faktoren nach der Prozentregel, <c>Brennwert</c> aus der Bauart, die
        /// Brennwertkennlinie bleibt aus, Anfahrverlust und Mindestlaufzeit bleiben leer.
        /// </summary>
        [Fact]
        public void Das_Katalogmodell_traegt_die_Kennlinie_aus_Satz_710_01()
        {
            var c = new HeizkesselImport();
            c.Import(Probe("heizkessel_sonderfaelle.vdi"));
            HeizkesselModel[] m = c._list.Select(a => new HeizkesselImportSatz(a).NachModell(a.m_szName, 25)).ToArray();

            Assert.True(m[0].Brennwert);
            Assert.Equal(0.978, m[0].Wirkungsgrad_Gas, 9);
            Assert.Null(m[0].Wirkungsgrad_Teillast30);
            Assert.Equal(4.0, m[0].Mindestleistung);

            Assert.True(m[1].Brennwert);                       // Brennwert-Kombi-Kessel
            Assert.Equal(1.08, m[1].Wirkungsgrad_Teillast30.Value, 9);
            Assert.Equal(3.4, m[1].Mindestleistung);

            Assert.False(m[2].Brennwert);
            Assert.Equal(0.9, m[2].Wirkungsgrad_Gas, 9);
            Assert.Equal(0.89, m[2].Wirkungsgrad_Teillast30.Value, 9);

            Assert.False(m[3].Brennwert);                      // Elektrokessel
            Assert.Equal(13, m[3].Brennstoff);
            Assert.Equal(0.995, m[3].Wirkungsgrad_Gas, 9);
            Assert.Null(m[3].Wirkungsgrad_Teillast30);

            Assert.All(m, k => Assert.False(k.Kennlinie_Brennwert));
            Assert.All(m, k => Assert.Null(k.Anfahrverlust_kWh));
            Assert.All(m, k => Assert.Null(k.Mindestlaufzeit_min));
            Assert.All(m, k => Assert.Null(KesselKennlinieWerte.Verstoss(k)));
        }

        /// <summary>Unplausibles bleibt leer: ein Tippfehler der Datei („9.5") und eine 0.</summary>
        [Theory]
        [InlineData("9.5", null)]
        [InlineData("0", null)]
        [InlineData("", null)]
        [InlineData("125", null)]
        [InlineData("104.6", 1.046)]
        [InlineData("1.05", 1.05)]
        public void Unplausible_Teillastwerte_bleiben_leer(string text, double? erwartet)
        {
            double? eta = HeizkesselImportSatz.Teillast30(text);
            if (erwartet == null) Assert.Null(eta);
            else Assert.Equal(erwartet.Value, eta.Value, 9);
        }

        [Theory]
        [InlineData("Brennwert-Kessel", true)]
        [InlineData("Brennwert-Kombi-Kessel", true)]
        [InlineData("Niedertemperatur-Kessel", false)]
        [InlineData("Standard-Kessel", false)]
        [InlineData("Elektro-Zentralheizung", false)]
        [InlineData("", false)]
        public void Die_Bauart_setzt_Brennwert(string bauart, bool brennwert)
        {
            Assert.Equal(brennwert, HeizkesselImportSatz.IstBrennwert(bauart));
        }

        // =============================================================================
        //  Teil 3 - Gegenprobe an allen Herstellerdateien (nur mit Git LFS)
        // =============================================================================

        /// <summary>
        /// Jede Kesseldatei unter <c>VDI-3805-Daten/SPK-Daten/</c>: Jeder Satz mit der Bauart
        /// „Brennwert…" wird Brennwertkessel, jedes η₃₀ ist leer oder plausibel und bei
        /// Brennwertkesseln über η₁₀₀, jede kleinste Leistung höchstens die Nennleistung, und kein
        /// Modell verletzt die Plausibilität des Editors. Ohne geholte Dateien (CI, LFS-Zeiger)
        /// schweigt der Fall.
        /// </summary>
        [Fact]
        public void Alle_Herstellerdateien_liefern_plausible_Kennlinien()
        {
            List<string> dateien = Herstellerdateien();
            if (dateien.Count == 0) return;

            int saetze = 0, brennwert = 0, mitTeillast = 0;
            foreach (string datei in dateien)
            {
                var c = new HeizkesselImport();
                c.Import(datei);
                foreach (Attrribute_hk a in c._list)
                {
                    HeizkesselModel m = new HeizkesselImportSatz(a).NachModell(a.m_szName, 25);
                    saetze++;
                    Assert.Equal(a.m_szBauart.IndexOf("Brennwert", StringComparison.OrdinalIgnoreCase) >= 0, m.Brennwert);
                    if (m.Brennwert) brennwert++;
                    Assert.False(m.Kennlinie_Brennwert);
                    Assert.True(KesselKennlinieWerte.Verstoss(m) == null,
                                Path.GetFileName(datei) + ", Satz " + saetze + ": " + KesselKennlinieWerte.Verstoss(m));
                    if (m.Wirkungsgrad_Teillast30.HasValue) mitTeillast++;
                }
            }

            Assert.True(saetze > 500, "Nur " + saetze + " Kesselsaetze gelesen.");
            Assert.True(brennwert > saetze / 2, brennwert + " von " + saetze + " Saetzen als Brennwertkessel.");
            Assert.True(mitTeillast > saetze / 2, mitTeillast + " von " + saetze + " Saetzen mit eta30.");
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static string Probe(string name)
        {
            string wurzel = Repowurzel();
            Assert.True(wurzel != null, "Die Repowurzel wurde nicht gefunden.");
            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Importproben", name);
            Assert.True(File.Exists(pfad), "Die Probe fehlt: " + pfad);
            return pfad;
        }

        /// <summary>Die entpackten Kesseldateien, ohne Git-LFS-Zeiger; leer, wenn keine geholt ist.</summary>
        internal static List<string> Herstellerdateien()
        {
            var liste = new List<string>();
            string wurzel = Repowurzel();
            if (wurzel == null) return liste;
            string ordner = Path.Combine(wurzel, "VDI-3805-Daten", "SPK-Daten");
            if (!Directory.Exists(ordner)) return liste;
            foreach (string d in Directory.GetFiles(ordner, "*", SearchOption.AllDirectories)
                                          .Where(p => p.EndsWith(".vdi", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(p => p, StringComparer.Ordinal))
                if (!IstLfsZeiger(d)) liste.Add(d);
            return liste;
        }

        private static bool IstLfsZeiger(string pfad)
        {
            // Der Kopf ist genau so lang wie die Kennung - ein Byte mehr ('.' aus
            // „git-lfs.github.com") liesse keinen Zeiger je gleich sein.
            const string kennung = "version https://git-lfs";
            byte[] kopf = new byte[kennung.Length];
            using (FileStream s = File.OpenRead(pfad))
            {
                int n = s.Read(kopf, 0, kopf.Length);
                return n == kopf.Length && Encoding.ASCII.GetString(kopf) == kennung;
            }
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
