using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Paketvorlage „Typtage nach VDI 4655“</b> (ZB11,
    /// <c>Referenzlaeufe/Typtagpaket_Vorlage_VDI4655</c>): Ihre Kopfzeilen sind genau die des
    /// Paketlesers (<see cref="Normformvektorleser"/>), sie trägt keinen Wert, und unverändert lehnt
    /// schon die Prüfung sie benannt ab — das Einspielen auch, und die Datenbank bleibt unverändert.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwTyptagVorlageTests
    {
        /// <summary>Die Spalten jeder Datei — dieselben, die der Leser verlangt bzw. erlaubt.</summary>
        private static readonly Dictionary<string, string> KOEPFE = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Normformvektorleser.DATEI_TYPTAGE] = "code;jahreszeit;tagart;bewoelkung",
            [Normformvektorleser.DATEI_KLIMAZONEN] = "zone;bezeichnung",
            [Normformvektorleser.DATEI_ANZAHL] = "zone;gebaeudeart;typtag;anzahl",
            [Normformvektorleser.DATEI_FAKTOREN] = "gebaeudeart;zone;typtag;faktor",
            [Normformvektorleser.DATEI_KENNWERTE] = "schluessel;wert;text",
            [Normformvektorleser.DATEI_GAENGE] = "gebaeudeart;typtag;aufloesung_min;index;anteil"
        };

        /// <summary>Die Spalten, die einen Wert der Richtlinie tragen würden — in der Vorlage leer.</summary>
        private static readonly string[] WERTSPALTEN = { "anzahl", "faktor", "wert", "anteil" };

        [Fact]
        public void Die_Vorlage_fuehrt_die_sechs_Dateien_mit_den_Koepfen_des_Lesers()
        {
            Assert.Equal(KOEPFE.Keys.OrderBy(x => x, StringComparer.Ordinal),
                         Directory.GetFiles(Vorlage(), "*.csv").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal));
            foreach (KeyValuePair<string, string> k in KOEPFE)
                Assert.Equal(k.Value, Zeilen(k.Key)[0]);

            // Gegenprobe am Leser: Das erfundene Paket des Testhelfers trägt dieselben Köpfe.
            foreach (TwwPaketdatei d in Typtagpaketbauer.Erfunden().Dateien())
                Assert.Equal(KOEPFE[d.Name], d.Inhalt.Split('\n')[0]);
        }

        /// <summary>Jede Wertspalte ist leer; Ziffern stehen nur in Zone, Code, Raster und Index.</summary>
        [Fact]
        public void Die_Vorlage_traegt_keinen_Wert()
        {
            var funde = new List<string>();
            foreach (string datei in KOEPFE.Keys)
            {
                string[] zeilen = Zeilen(datei);
                string[] kopf = zeilen[0].Split(';');
                if (zeilen.Length < 2) funde.Add(datei + ": keine Beispielzeile");
                for (int i = 1; i < zeilen.Length; i++)
                {
                    string[] f = zeilen[i].Split(';');
                    for (int s = 0; s < kopf.Length; s++)
                        if (WERTSPALTEN.Contains(kopf[s]) && f[s].Trim().Length > 0)
                            funde.Add(datei + " Zeile " + (i + 1) + ": " + kopf[s] + " = " + f[s]);
                }
            }
            Assert.True(funde.Count == 0, "Die Typtag-Vorlage traegt Werte statt Platzhaltern:\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// Unverändert lehnt die Prüfung die Vorlage benannt ab — die Zahl der Kalendertage fehlt, mit
        /// Datei, Zeile und Spalte —, und das Einspielen schreibt nichts.
        /// </summary>
        [Fact]
        public void Die_unveraenderte_Vorlage_ist_benannt_abgelehnt_und_schreibt_nichts()
        {
            IReadOnlyList<TwwPaketdatei> dateien = TwwTyptagCtrl.PaketLesen(Vorlage(), out ZapfSatz lesefehler);
            Assert.Null(lesefehler);
            Assert.Equal(6, dateien.Count);

            TwwTyptagpruefung p = TwwTyptagCtrl.Pruefen(dateien);
            Assert.False(p.Ok);
            Assert.Equal("NORMVEKTOR_FELD_LEER", p.Abbruch.Kennung);
            Assert.Equal(Normformvektorleser.DATEI_ANZAHL, p.Abbruch.Werte[0]);
            Assert.Equal(2, Convert.ToInt32(p.Abbruch.Werte[1], CultureInfo.InvariantCulture));
            Assert.Equal("anzahl", p.Abbruch.Werte[2]);

            using var db = new TestDatenbank();
            if (!db.Vorhanden || !TwwTyptagCtrl.TabelleVorhanden()) return;
            long vorher = Zahl();
            TwwTyptagimportBericht b = TwwTyptagCtrl.Importieren(dateien);
            Assert.Equal("NORMVEKTOR_FELD_LEER", b.Abbruch?.Kennung);
            Assert.Equal(vorher, Zahl());
        }

        private static long Zahl()
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TYPTAG_IMPORT),
                               CultureInfo.InvariantCulture);

        private static string[] Zeilen(string datei)
            => File.ReadAllLines(Path.Combine(Vorlage(), datei), Encoding.UTF8).Where(x => x.Trim().Length > 0).ToArray();

        private static string Vorlage() => Path.Combine(Wurzel(), "Referenzlaeufe", "Typtagpaket_Vorlage_VDI4655");

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
