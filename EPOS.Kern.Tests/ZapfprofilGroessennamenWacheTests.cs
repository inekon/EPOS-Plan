using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Namenstafel der Größen des Herkunftsprotokolls</b> (Anwenderentscheid ZU25,
    /// Nachtrag N21): Die Spalte „Größe" der Karte „Herkunft" trug den Feldnamen des Kerns als
    /// Daten — in beiden Sprachen dasselbe Wort. Jetzt trägt jede Größe einen Ressourcenschlüssel
    /// <c>ZPG_GROESSE_&lt;KONSTANTE&gt;</c> in beiden Sprachen, und diese Wache hält beide Seiten
    /// zusammen:
    ///
    /// <para>(a) Jede Größe, die der Rechenweg vermerkt, ist eine <b>Konstante</b> in
    /// <c>ZapfFeld</c> — der Quelltext der Rechenklassen trägt keinen Feldnamen mehr als
    /// Zeichenkette. (b) Jede Konstante hat einen Ressourcentext in <b>beiden</b> Sprachen, und
    /// die beiden sind nicht dasselbe Wort. (c) Fehlt ein Schlüssel, steht der Feldname da —
    /// benannter Rückfall, keine leere Zelle.</para>
    ///
    /// <para>Die Wache zählt die Namen aus dem Kern, nicht aus einer zweiten Liste: Wer eine Größe
    /// ergänzt, bekommt ihren Schlüssel aus dem Namen der Konstanten und muss den Text nachziehen.</para>
    /// </summary>
    public sealed class ZapfprofilGroessennamenWacheTests
    {
        /// <summary>
        /// Jede Größe des Kerns hat einen Schlüssel, und jeder Schlüssel einen Text in beiden
        /// Sprachen — deutsch und englisch verschieden (sonst wäre die Tafel nur abgeschrieben).
        /// </summary>
        [Fact]
        public void Jede_Groesse_hat_einen_Schluessel_und_Text_in_beiden_Sprachen()
        {
            IReadOnlyCollection<string> namen = ZapfFeld.Namen;
            Assert.True(namen.Count >= 42, "Die Namenstafel trägt nur " + namen.Count + " Größen.");

            var ohneSchluessel = new List<string>();
            var ohneDeutsch = new List<string>();
            var ohneEnglisch = new List<string>();
            var gleich = new List<string>();
            foreach (string groesse in namen)
            {
                string schluessel = ZapfFeld.Ressourcenschluessel(groesse);
                if (schluessel == null) { ohneSchluessel.Add(groesse); continue; }
                Assert.StartsWith(ZapfFeld.SCHLUESSEL_VORSATZ, schluessel, StringComparison.Ordinal);

                string de = R.ResourceManager.GetString(schluessel, new CultureInfo("de-DE"));
                string en = R.ResourceManager.GetString(schluessel, new CultureInfo("en-US"));
                if (string.IsNullOrWhiteSpace(de)) ohneDeutsch.Add(schluessel);
                if (string.IsNullOrWhiteSpace(en)) ohneEnglisch.Add(schluessel);
                else if (string.Equals(de, en, StringComparison.Ordinal)) gleich.Add(schluessel);
            }
            Assert.Empty(ohneSchluessel);
            Assert.Empty(ohneDeutsch);
            Assert.Empty(ohneEnglisch);
            Assert.Empty(gleich);
        }

        /// <summary>
        /// <b>Kein Feldname mehr als Zeichenkette:</b> Jeder Aufruf von
        /// <c>Herkunftsprotokoll.Vermerken</c> und jede Übergabe an einen Vermerkweg nimmt die
        /// Größe aus <c>ZapfFeld</c>. Zwölf Namen der Auslegung standen früher als Literale im
        /// Quelltext — ohne Konstante gibt es keinen Schlüssel und damit keine Beschriftung.
        /// </summary>
        [Fact]
        public void Kein_Rechenweg_traegt_einen_Groessennamen_als_Zeichenkette()
        {
            string ordner = Zapfprofilordner();
            if (ordner == null) return;

            // Zweites Argument von Vermerken(...) als Zeichenkette - genau das ist die Groesse.
            var muster = new Regex("Vermerken\\([^,()]*,\\s*\"", RegexOptions.CultureInvariant);
            var treffer = new List<string>();
            foreach (string datei in Directory.GetFiles(ordner, "*.cs").OrderBy(x => x, StringComparer.Ordinal))
            {
                string[] zeilen = File.ReadAllLines(datei);
                for (int i = 0; i < zeilen.Length; i++)
                    if (muster.IsMatch(zeilen[i]))
                        treffer.Add(Path.GetFileName(datei) + ":" + (i + 1).ToString(CultureInfo.InvariantCulture));
            }
            Assert.Empty(treffer);

            // Und die zwoelf Groessen der Auslegung stehen als Konstanten in der Tafel.
            foreach (string groesse in new[]
            {
                ZapfFeld.AUSLEGUNG_SPEICHER_C, ZapfFeld.AUSLEGUNG_KALTWASSER_C, ZapfFeld.AUSLEGUNG_SENSORHOEHE,
                ZapfFeld.AUSLEGUNG_UEBERTRAGERFLAECHE, ZapfFeld.AUSLEGUNG_ERZEUGER_KW, ZapfFeld.AUSLEGUNG_UEBERTRAGER_U,
                ZapfFeld.AUSLEGUNG_KALTWASSERFAKTOR, ZapfFeld.AUSLEGUNG_NUTZANTEIL, ZapfFeld.AUSLEGUNG_ZUSCHLAG,
                ZapfFeld.AUSLEGUNG_BEDARFSTAGFAKTOR, ZapfFeld.AUSLEGUNG_LADEFENSTER_BEGINN, ZapfFeld.AUSLEGUNG_LADEFENSTER
            })
                Assert.NotNull(ZapfFeld.Ressourcenschluessel(groesse));
        }

        /// <summary>
        /// Die Hülle setzt die Tafel um: In deutscher Kultur steht der deutsche Text, in
        /// englischer der englische, und ein Name, den der Kern nicht als Größe kennt, steht
        /// unverändert da (benannter Rückfall).
        /// </summary>
        [Fact]
        public void Die_Huelle_beschriftet_die_Groesse_und_faellt_benannt_zurueck()
        {
            using (var _ = new Kulturvorrichtung("de-DE"))
            {
                Assert.Equal("Bezugsmenge", ZapfprofilHuelle.Groessenname(ZapfFeld.BEZUGSMENGE));
                Assert.Equal("Zirkulation, Verfahren", ZapfprofilHuelle.Groessenname(ZapfFeld.ZIRKULATION_METHODE));
                Assert.Equal("Auslegung, Speichertemperatur", ZapfprofilHuelle.Groessenname(ZapfFeld.AUSLEGUNG_SPEICHER_C));
            }
            using (var _ = new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Reference quantity", ZapfprofilHuelle.Groessenname(ZapfFeld.BEZUGSMENGE));
                Assert.Equal("Circulation, method", ZapfprofilHuelle.Groessenname(ZapfFeld.ZIRKULATION_METHODE));
            }

            // Rueckfall: ein Name ohne Konstante bleibt, wie er ist - und leer bleibt leer.
            Assert.Equal("Erfundene.Groesse", ZapfprofilHuelle.Groessenname("Erfundene.Groesse"));
            Assert.Equal("", ZapfprofilHuelle.Groessenname(null));
            Assert.Equal("", ZapfprofilHuelle.Groessenname(""));
        }

        /// <summary><c>EPOS.Kern/Allgemein/Zapfprofil</c>, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Zapfprofilordner([CallerFilePath] string eigeneDatei = null)
        {
            var d = new DirectoryInfo(Path.GetDirectoryName(eigeneDatei) ?? ".");
            while (d != null)
            {
                string ziel = Path.Combine(d.FullName, "EPOS.Kern", "Allgemein", "Zapfprofil");
                if (Directory.Exists(ziel)) return ziel;
                d = d.Parent;
            }
            return null;
        }
    }
}
