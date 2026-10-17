using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die früheren Stände der ausgelieferten Paketzeilen</b> (<see cref="PaketteilNachfuehrung"/>;
    /// Umsetzungskonzept Zapfprofilgenerator N34 (d), Auftrag A2, Entscheid E-A2-4): die Regel selbst
    /// samt ihrer Grenze, die Saatliste der Testdatenbank als ihr Spiegel, der Paketteil im heutigen
    /// Stand und der Katalogimport, der eine Zeile eines älteren Pakets VOR dem Dublettenscan als die
    /// heutige liest. Die gespeicherten Zeilen hält <see cref="TwwBezugsartSchemaTests"/>, den
    /// Projektimport <see cref="TwwKopierstellenTests"/>. Alle Werte außer denen des Paketteils erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PaketteilNachfuehrungTests
    {
        private const string HOTEL_FRUEHER = "Hotel (aus Messung)";
        private const string HOTEL = "Hotel (aus Messung, je Zimmer)";
        private const string AUS = TwwSchema.STATUS_AUSLIEFERUNG;
        private const string EK = TwwSchema.HERKUNFT_EIGENKONSTRUKTION;

        // =============================================================================
        //  Die Regel
        // =============================================================================

        [Theory]
        [InlineData(HOTEL_FRUEHER, 3, AUS, "FREI-1", EK, true)]
        [InlineData(HOTEL, 3, AUS, "FREI-1", EK, true)]              // der Zwischenstand #579 bis A2
        [InlineData(" " + HOTEL_FRUEHER + " ", 3, AUS, " FREI-1 ", EK, true)]
        [InlineData(HOTEL, 8, AUS, "FREI-1", EK, false)]             // der heutige Stand
        [InlineData(HOTEL_FRUEHER, 1, AUS, "FREI-1", EK, false)]     // andere Bezugsart
        [InlineData(HOTEL_FRUEHER, 3, TwwSchema.STATUS_EIGEN, "FREI-1", EK, false)]    // Anwenderzeile
        [InlineData(HOTEL_FRUEHER, 3, TwwSchema.STATUS_IMPORT, "FREI-1", EK, false)]   // eingespielte Zeile
        [InlineData(HOTEL_FRUEHER, 3, "", "FREI-1", EK, false)]      // Paket ohne Status
        [InlineData(HOTEL_FRUEHER, 3, AUS, "FREI-2", EK, false)]     // anderer Stand der Provenienz
        [InlineData(HOTEL_FRUEHER, 3, AUS, "FREI-1", TwwSchema.HERKUNFT_IMPORT, false)]
        [InlineData(HOTEL_FRUEHER, 3, AUS, "FREI-1", TwwSchema.HERKUNFT_VERFAHREN, false)]
        [InlineData("hotel (aus messung)", 3, AUS, "FREI-1", EK, false)]               // Name genau
        [InlineData("Krankenhaus (abgeleitet)", 3, AUS, "FREI-1", TwwSchema.HERKUNFT_VERFAHREN, false)]
        public void Die_Regel_trifft_nur_Auslieferungszeilen_des_Paketteils_im_frueheren_Stand(
            string name, int bezugsart, string status, string version, string herkunft, bool trifft)
        {
            PaketteilNachfuehrung.Eintrag e = PaketteilNachfuehrung.Finden(name, bezugsart, status, version, herkunft);
            Assert.Equal(trifft, e != null);
            if (!trifft) return;
            Assert.Equal(HOTEL, e.Bezeichner);
            Assert.Equal(ZapfBezugsart.Zimmer, e.Bezugsart);
            Assert.Equal(ZapfBezugsart.Betten, e.FruehereBezugsart);
        }

        [Fact]
        public void Ohne_Bezugsart_trifft_die_Regel_nichts()
            => Assert.Null(PaketteilNachfuehrung.Finden(HOTEL_FRUEHER, null, AUS, "FREI-1", EK));

        /// <summary>Der heutige Stand ist ein Fixpunkt: Kein Eintrag führt auf einen Stand, den ein anderer wieder trifft.</summary>
        [Fact]
        public void Jeder_heutige_Stand_ist_ein_Fixpunkt()
        {
            foreach (PaketteilNachfuehrung.Eintrag e in PaketteilNachfuehrung.NUTZUNGSARTEN)
            {
                Assert.Null(PaketteilNachfuehrung.Finden(e.Bezeichner, (long)e.Bezugsart, AUS,
                                                         PaketteilNachfuehrung.VERSION_PAKETTEIL, TwwWertemengen.Text(e.Herkunft)));
                Assert.NotEqual(e.FruehereBezugsart, e.Bezugsart);
            }
        }

        /// <summary>
        /// Die Saatliste der Testdatenbank (<c>UMBENANNTE_NUTZUNGSARTEN</c> in
        /// <c>tww_testkatalog_fiktiv.py</c>) spiegelt die Einträge des Kerns — Name und Bezugsart vorher
        /// und nachher, in derselben Reihenfolge.
        /// </summary>
        [Fact]
        public void Die_Saatliste_spiegelt_die_Regel_des_Kerns()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string skript = File.ReadAllText(Path.Combine(wurzel, "Referenzlaeufe", "Skripte", "tww_testkatalog_fiktiv.py"));
            Match liste = Regex.Match(skript, @"^UMBENANNTE_NUTZUNGSARTEN = \[(.*?)^\]", RegexOptions.Multiline | RegexOptions.Singleline);
            Assert.True(liste.Success, "UMBENANNTE_NUTZUNGSARTEN fehlt im Saatskript.");
            var saat = Regex.Matches(liste.Groups[1].Value, "\\(\"([^\"]+)\", (\\d+), \"([^\"]+)\", (\\d+)\\)")
                            .Select(m => (m.Groups[1].Value, int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                                          m.Groups[3].Value, int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)))
                            .ToList();
            Assert.Equal(PaketteilNachfuehrung.NUTZUNGSARTEN
                             .Select(e => (e.FruehererBezeichner, (int)e.FruehereBezugsart, e.Bezeichner, (int)e.Bezugsart)).ToList(),
                         saat);
        }

        /// <summary>
        /// Der freie Paketteil steht im heutigen Stand: Die Hotelzeile heißt „Hotel (aus Messung, je
        /// Zimmer)", trägt die Bezugsart Zimmer und die Provenienz des Paketteils — die Regel trifft sie
        /// nicht (sonst liefe sie im Kreis).
        /// </summary>
        [Fact]
        public void Der_Paketteil_fuehrt_den_heutigen_Stand()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            Dictionary<string, string> z = Paketteilzeile(wurzel);
            Assert.Equal(((int)ZapfBezugsart.Zimmer).ToString(CultureInfo.InvariantCulture), z["Bezugsart"]);
            Assert.Equal(PaketteilNachfuehrung.VERSION_PAKETTEIL, z["Bedarf_Version"]);
            Assert.Equal(EK, z["Bedarf_Herkunftsart"]);
            Assert.Equal(AUS, z["Status"]);
            Assert.Null(PaketteilNachfuehrung.Finden(z["Bezeichner"], long.Parse(z["Bezugsart"], CultureInfo.InvariantCulture),
                                                     z["Status"], z["Bedarf_Version"], z["Bedarf_Herkunftsart"]));
        }

        // =============================================================================
        //  Der Katalogimport (E-A2-4 b)
        // =============================================================================

        /// <summary>
        /// <b>Ein älteres Paket in eine nachgeführte Datenbank:</b> Die Zeile „Hotel (aus Messung)" mit
        /// Betten wird als die heutige gelesen, trifft den natürlichen Schlüssel der Auslieferungszeile und
        /// wird übersprungen — keine zweite Zeile; die Zeile des Berichts nennt den früheren Stand vor
        /// ihrem Grund, in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Ein_aelteres_Paket_trifft_die_nachgefuehrte_Zeile_und_legt_keine_zweite_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Probesatz Hotel", "FREI-1");
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen(HOTEL, "FREI-1", satz, AUS, readOnly: true);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwNutzungsart_STAMM SET Bezugsart = 8, Bedarf_Version = 'FREI-1', Bedarf_Herkunftsart = ? WHERE ID = ?",
                new DbParam("?", EK), new DbParam("?", geliefert)));
            long vorher = Nutzungsarten();

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(Hotelpaket(wurzel, HOTEL_FRUEHER, 3, AUS));
            Assert.Null(b.Abbruch);
            TwwImportzeile z = Assert.Single(b.ZeilenVon(TwwImportbereich.Nutzungsart));
            Assert.Equal(TwwImportausgang.Uebersprungen, z.Ausgang);
            Assert.Equal(HOTEL, z.Katalogname);
            Assert.Equal("KATALOGIMPORT_FRUEHERER_STAND_UND", z.Grund.Kennung);
            Assert.Contains("„" + HOTEL_FRUEHER + "“, Bezugsart Betten", z.Grund.Klartext, StringComparison.Ordinal);
            Assert.Contains("„" + HOTEL + "“ mit der Bezugsart Zimmer", z.Grund.Klartext, StringComparison.Ordinal);
            Assert.Contains("unveränderlich", z.Grund.Klartext, StringComparison.Ordinal);
            Assert.Equal(vorher, Nutzungsarten());

            using (new Kulturvorrichtung("en-US"))
            {
                TwwImportberichtDaten d = ZapfprofilHuelle.AlsBericht(b, ZapfprofilHuelle.KatalogTexte());
                Assert.Contains("earlier state (“" + HOTEL_FRUEHER + "”, reference type beds)",
                                Assert.Single(d.Zeilen, x => x.Bereich == TwwImportbereichDaten.Nutzungsart).Grund, StringComparison.Ordinal);
            }

            // Das Paket im heutigen Stand trifft dieselbe Zeile - ohne Satz zum früheren Stand.
            TwwImportzeile heute = Assert.Single(TwwNutzungsartCtrl.Importieren(Hotelpaket(wurzel, HOTEL, 8, AUS))
                                                                    .ZeilenVon(TwwImportbereich.Nutzungsart));
            Assert.Equal(TwwImportausgang.Uebersprungen, heute.Ausgang);
            Assert.Equal("KATALOGIMPORT_AUSLIEFERUNG", heute.Grund.Kennung);
            Assert.Equal(vorher, Nutzungsarten());
        }

        /// <summary>
        /// <b>Ein älteres Paket in eine Datenbank ohne die Zeile:</b> Sie entsteht unter dem heutigen
        /// Namen mit der Bezugsart Zimmer (eine Anwenderzeile, Status IMPORT), und ein zweites Einspielen
        /// — älteres wie heutiges Paket — findet sie wieder.
        /// </summary>
        [Fact]
        public void Ein_aelteres_Paket_legt_die_Zeile_im_heutigen_Stand_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            long vorher = Nutzungsarten();

            TwwImportzeile z = Assert.Single(TwwNutzungsartCtrl.Importieren(Hotelpaket(wurzel, HOTEL_FRUEHER, 3, AUS))
                                                                .ZeilenVon(TwwImportbereich.Nutzungsart));
            Assert.Equal(TwwImportausgang.Angelegt, z.Ausgang);
            Assert.Equal("KATALOGIMPORT_FRUEHERER_STAND", z.Grund.Kennung);
            DataRow r = Zeile(z.IdNeu);
            Assert.Equal(HOTEL, Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
            Assert.Equal("FREI-1", Convert.ToString(r["Katalogversion"], CultureInfo.InvariantCulture));
            Assert.Equal((long)ZapfBezugsart.Zimmer, Convert.ToInt64(r["Bezugsart"], CultureInfo.InvariantCulture));
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(r["Status"], CultureInfo.InvariantCulture));
            Assert.Equal(vorher + 1, Nutzungsarten());

            foreach ((string name, int bezug) in new[] { (HOTEL_FRUEHER, 3), (HOTEL, 8) })
            {
                TwwImportzeile wieder = Assert.Single(TwwNutzungsartCtrl.Importieren(Hotelpaket(wurzel, name, bezug, AUS))
                                                                         .ZeilenVon(TwwImportbereich.Nutzungsart));
                Assert.Equal(TwwImportausgang.Uebersprungen, wieder.Ausgang);
                Assert.Equal(vorher + 1, Nutzungsarten());
            }
        }

        /// <summary>
        /// <b>Die Grenze im Import:</b> Eine Zeile gleichen Namens, die nicht zum ausgelieferten Paketteil
        /// gehört (Status EIGEN im Paket), bleibt, wie sie ist — unter ihrem Namen, mit Betten, ohne Satz
        /// zum früheren Stand.
        /// </summary>
        [Fact]
        public void Eine_Anwenderzeile_gleichen_Namens_bleibt_wie_sie_ist()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            TwwImportzeile z = Assert.Single(TwwNutzungsartCtrl.Importieren(
                Hotelpaket(wurzel, HOTEL_FRUEHER, 3, TwwSchema.STATUS_EIGEN)).ZeilenVon(TwwImportbereich.Nutzungsart));
            Assert.Equal(TwwImportausgang.Angelegt, z.Ausgang);
            Assert.Null(z.Grund);
            DataRow r = Zeile(z.IdNeu);
            Assert.Equal(HOTEL_FRUEHER, Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
            Assert.Equal((long)ZapfBezugsart.Betten, Convert.ToInt64(r["Bezugsart"], CultureInfo.InvariantCulture));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Die Hotelzeile des freien Paketteils als Feldname → Text.</summary>
        private static Dictionary<string, string> Paketteilzeile(string wurzel)
        {
            string[] zeilen = File.ReadAllLines(Path.Combine(wurzel, "Referenzlaeufe", "Katalogpaket_frei",
                                                             TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"));
            string[] kopf = zeilen[0].Split(';');
            string[] felder = zeilen.Single(z => z.StartsWith(HOTEL + ";", StringComparison.Ordinal)).Split(';');
            Assert.Equal(kopf.Length, felder.Length);
            return kopf.Select((k, i) => (k, felder[i])).ToDictionary(x => x.k, x => x.Item2, StringComparer.Ordinal);
        }

        /// <summary>
        /// Ein Katalogpaket mit der Hotelzeile des freien Paketteils in Katalogversion <c>FREI-1</c> — unter
        /// <paramref name="name"/>, mit <paramref name="bezugsart"/> und <paramref name="status"/> — samt
        /// ihrem Tagesgangsatz und seinen vier Tagesgängen aus dem Paketteil.
        /// </summary>
        private static List<TwwPaketdatei> Hotelpaket(string wurzel, string name, int bezugsart, string status)
        {
            string ordner = Path.Combine(wurzel, "Referenzlaeufe", "Katalogpaket_frei");
            Dictionary<string, string> z = Paketteilzeile(wurzel);
            z["Bezeichner"] = name;
            z["Bezugsart"] = bezugsart.ToString(CultureInfo.InvariantCulture);
            z["Status"] = status;
            string satz = z["ID_Tagesgangsatz"];
            string arten = "Katalogversion;" + string.Join(";", z.Keys) + "\n" + "FREI-1;" + string.Join(";", z.Values) + "\n";

            string[] saetze = File.ReadAllLines(Path.Combine(ordner, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv"));
            string satzzeile = saetze.Single(s => s.StartsWith(satz + ";", StringComparison.Ordinal));
            string[] sf = satzzeile.Split(';');
            string saetzeText = "ID;Bezeichner;Katalogversion;Status;ReadOnly\n" + sf[0] + ";" + sf[1] + ";FREI-1;" + sf[2] + ";" + sf[3] + "\n";

            string[] gaenge = File.ReadAllLines(Path.Combine(ordner, TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv"));
            string gaengeText = gaenge[0] + "\n" +
                                string.Join("\n", gaenge.Skip(1).Where(g => g.StartsWith(satz + ";", StringComparison.Ordinal))) + "\n";

            return new List<TwwPaketdatei>
            {
                new TwwPaketdatei(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv", saetzeText),
                new TwwPaketdatei(TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv", gaengeText),
                new TwwPaketdatei(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv", arten)
            };
        }

        private static long Nutzungsarten()
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM"), CultureInfo.InvariantCulture);

        private static DataRow Zeile(int id)
            => Assert.Single(DataRepository.GetDataTable("SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?",
                                                         new DbParam("?", id)).Rows.Cast<DataRow>());

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
