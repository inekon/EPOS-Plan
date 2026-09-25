using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE B7 — die EINE Emissionsspalte der Energieträgertabelle
    /// (Konzept § 2.5, Entscheidung E-1).
    ///
    /// <para><b>Was hier gemessen wird.</b> Nicht die Zahl — die entsteht in
    /// <c>EmissionenCtrl.SummeCo2eGKwh</c> und hat dort ihre eigenen Prüfstände —,
    /// sondern die AUSKUNFT über die Zahl. Im Modus CO2E kann derselbe Wert auf drei
    /// Weisen zustande kommen, und der dritte Fall (der Wert IST bereits ein
    /// Äquivalent) liest sich ohne Hinweis wie ein Fehler: Die Spalte summiert
    /// sichtbar nicht auf.</para>
    ///
    /// <para><b>Und die vierte Lage, die keine ist:</b> Fehlt der Artenkatalog, liefert
    /// <c>Wirksam(CO2E)</c> den reinen CO₂-Faktor. Das darf nicht still geschehen —
    /// sonst stünde über einer CO₂-Zahl der Kopf „CO₂-Äquivalent".</para>
    /// </summary>
    public class EmissionsspalteTests : System.IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static EmissionsartModel Art(string kuerzel, double gwp, string einheit)
            => new EmissionsartModel
            {
                Kuerzel = kuerzel,
                Einheit = einheit,
                Co2Aequivalent = gwp
            };

        private static EmissionsZeile Zeile(EmissionsartModel art, double? wert, bool istCo2e = false)
            => new EmissionsZeile { Art = art, Wert = wert, IstCo2e = istCo2e };

        private static readonly string G_KWH = DbWerte.EMISSION_EINHEIT_G_KWH;

        // =================================================================
        //  Der Spaltenkopf folgt dem Modus
        // =================================================================

        [Fact]
        public void Der_Spaltenkopf_folgt_dem_Berechnungsmodus()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BK_KOSTEN_SP_CO2,
                         EmissionsAusweis.SpaltenkopfEmission(DbWerte.EMISSION_MODUS_CO2));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BK_KOSTEN_SP_CO2E,
                         EmissionsAusweis.SpaltenkopfEmission(DbWerte.EMISSION_MODUS_CO2E));

            // Und die beiden Köpfe sind verschieden — sonst wäre die ganze
            // Unterscheidung folgenlos.
            Assert.NotEqual(EmissionsAusweis.SpaltenkopfEmission(DbWerte.EMISSION_MODUS_CO2),
                            EmissionsAusweis.SpaltenkopfEmission(DbWerte.EMISSION_MODUS_CO2E));
        }

        // =================================================================
        //  Die drei Fälle der Entscheidung E-1
        // =================================================================

        /// <summary>
        /// REGELFALL: mehrere Arten gepflegt — der Kurztext zeigt die Aufschlüsselung
        /// Summand für Summand, samt Äquivalenzfaktoren und Ergebnis.
        /// </summary>
        [Fact]
        public void Fall_1_Der_Regelfall_zeigt_die_gewichtete_Summe()
        {
            EmissionsartModel co2 = Art(DbWerte.EMISSIONSART_CO2, 1.0, G_KWH);
            EmissionsartModel ch4 = Art("CH4", 28.0, G_KWH);

            var satz = new EmissionsFaktorSatz
            {
                Co2GKwh = 240.0,
                Co2eGKwh = 254.0,
                Co2Ebene = EmissionsFaktorLader.EBENE_KATALOG,
                Zeilen = new List<EmissionsZeile> { Zeile(co2, 240.0), Zeile(ch4, 0.5) }
            };

            string text = EmissionsAusweis.HerleitungEmission(
                satz, DbWerte.EMISSION_MODUS_CO2E, CultureInfo.GetCultureInfo("de-DE"));

            Assert.Contains("CO2", text);
            Assert.Contains("CH4", text);
            Assert.Contains("28", text);       // der GWP100-Faktor steht da
            Assert.Contains("GWP100", text);
            Assert.Contains("KATALOG", text);
        }

        /// <summary>
        /// NUR CO₂ GEPFLEGT: Der Äquivalentwert entspricht dem CO₂-Faktor — und der
        /// Kurztext sagt genau das, statt eine Summe aus einem Summanden zu zeigen.
        /// </summary>
        [Fact]
        public void Fall_2_Ohne_zweite_Art_nennt_der_Kurztext_den_Grund()
        {
            var satz = new EmissionsFaktorSatz
            {
                Co2GKwh = 240.0,
                Co2eGKwh = 240.0,
                Co2Ebene = EmissionsFaktorLader.EBENE_STAMM,
                Zeilen = new List<EmissionsZeile>
                {
                    Zeile(Art(DbWerte.EMISSIONSART_CO2, 1.0, G_KWH), 240.0),
                    Zeile(Art("CH4", 28.0, G_KWH), null)        // nicht gepflegt
                }
            };

            string text = EmissionsAusweis.HerleitungEmission(
                satz, DbWerte.EMISSION_MODUS_CO2E, CultureInfo.GetCultureInfo("de-DE"));

            Assert.Contains("keine weitere Emissionsart", text);
            Assert.Contains("STAMM", text);
            Assert.DoesNotContain("GWP100", text);     // es gibt nichts zu gewichten
        }

        /// <summary>
        /// WERT IST BEREITS ÄQUIVALENT (F3): Er wird NICHT aufsummiert — und das steht
        /// da. Ohne diesen Satz läse sich die fehlende Aufsummierung wie ein Fehler;
        /// genau das ist der heikelste der drei Fälle.
        /// </summary>
        [Fact]
        public void Fall_3_Ein_hinterlegtes_Aequivalent_wird_nicht_aufsummiert_und_sagt_es()
        {
            var satz = new EmissionsFaktorSatz
            {
                Co2GKwh = 256.7,
                Co2eGKwh = 256.7,
                Co2IstAequivalent = true,
                Co2Ebene = EmissionsFaktorLader.EBENE_KATALOG,
                Zeilen = new List<EmissionsZeile>
                {
                    Zeile(Art(DbWerte.EMISSIONSART_CO2, 1.0, G_KWH), 256.7, istCo2e: true),
                    Zeile(Art("CH4", 28.0, G_KWH), 0.5)
                }
            };

            string text = EmissionsAusweis.HerleitungEmission(
                satz, DbWerte.EMISSION_MODUS_CO2E, CultureInfo.GetCultureInfo("de-DE"));

            Assert.Contains("bereits ein CO₂-Äquivalent", text);
            Assert.Contains("nicht aufsummiert", text);
        }

        // =================================================================
        //  Kein stiller Rückfall — die Gegenprobe der Etappe
        // =================================================================

        /// <summary>
        /// DIE GEGENPROBE zu „kein stiller Rückfall" (Konzept § 2.5): Ohne
        /// Artenkatalog trägt die Spalte den reinen CO₂-Faktor, obwohl ihr Kopf
        /// „CO₂-Äquivalent" heißt. Das ist hinnehmbar — aber nur, WEIL es dasteht.
        /// Wer diesen Zweig entfernt, macht aus einer benannten Einschränkung eine
        /// falsche Beschriftung, und dieser Fall wird rot.
        /// </summary>
        [Fact]
        public void Ohne_Artenkatalog_wird_der_Rueckfall_auf_CO2_benannt()
        {
            var satz = new EmissionsFaktorSatz
            {
                Co2GKwh = 240.0,
                Co2eGKwh = 240.0,
                ArtenkatalogFehlt = true,
                Co2Ebene = EmissionsFaktorLader.EBENE_CARRIER,
                Zeilen = new List<EmissionsZeile>
                {
                    Zeile(Art(DbWerte.EMISSIONSART_CO2, 1.0, G_KWH), 240.0)
                }
            };

            string text = EmissionsAusweis.HerleitungEmission(
                satz, DbWerte.EMISSION_MODUS_CO2E, CultureInfo.GetCultureInfo("de-DE"));

            Assert.Contains("keine Äquivalenzfaktoren", text);
            Assert.Contains("nicht ein Äquivalent", text);

            // Der WERT selbst bleibt, was er ist — gezeigt wird immer eine Zahl.
            Assert.Equal(240.0, satz.Wirksam(DbWerte.EMISSION_MODUS_CO2E).Value, 6);
        }

        /// <summary>Im Modus CO2 gibt es nichts herzuleiten — nur die Quelle zu nennen.
        /// Eine Summenformel dort wäre eine Aussage über eine Rechnung, die nicht
        /// stattgefunden hat.</summary>
        [Fact]
        public void Im_Modus_CO2_nennt_der_Kurztext_nur_die_Herkunft()
        {
            var satz = new EmissionsFaktorSatz
            {
                Co2GKwh = 240.0,
                Co2eGKwh = 254.0,
                Co2Ebene = EmissionsFaktorLader.EBENE_PROJEKT,
                Zeilen = new List<EmissionsZeile>
                {
                    Zeile(Art(DbWerte.EMISSIONSART_CO2, 1.0, G_KWH), 240.0),
                    Zeile(Art("CH4", 28.0, G_KWH), 0.5)
                }
            };

            string text = EmissionsAusweis.HerleitungEmission(
                satz, DbWerte.EMISSION_MODUS_CO2, CultureInfo.GetCultureInfo("de-DE"));

            Assert.Contains("PROJEKT", text);
            Assert.DoesNotContain("GWP100", text);
            Assert.Equal(240.0, satz.Wirksam(DbWerte.EMISSION_MODUS_CO2).Value, 6);
        }

        // =================================================================
        //  Beide Sprachen
        // =================================================================

        /// <summary>
        /// Kopf und Herleitung gibt es in beiden Sprachen — und sie sind verschieden.
        /// Ein fehlender englischer Schlüssel fiele sonst erst dem Anwender auf.
        /// </summary>
        [Fact]
        public void Kopf_und_Herleitung_stehen_in_beiden_Sprachen()
        {
            var satz = new EmissionsFaktorSatz
            {
                Co2GKwh = 240.0,
                Co2eGKwh = 240.0,
                Co2IstAequivalent = true,
                Co2Ebene = EmissionsFaktorLader.EBENE_KATALOG
            };

            string kopfDe = EmissionsAusweis.SpaltenkopfEmission(DbWerte.EMISSION_MODUS_CO2E);
            string textDe = EmissionsAusweis.HerleitungEmission(
                satz, DbWerte.EMISSION_MODUS_CO2E, CultureInfo.GetCultureInfo("de-DE"));

            using (new Sprachumschaltung("en-US"))
            {
                string kopfEn = EmissionsAusweis.SpaltenkopfEmission(DbWerte.EMISSION_MODUS_CO2E);
                string textEn = EmissionsAusweis.HerleitungEmission(
                    satz, DbWerte.EMISSION_MODUS_CO2E, CultureInfo.GetCultureInfo("en-US"));

                Assert.Contains("equivalent", kopfEn.ToLowerInvariant());
                Assert.Contains("already is a CO₂ equivalent", textEn);
                Assert.NotEqual(textDe, textEn);
            }

            Assert.Contains("Äquivalent", kopfDe);
            Assert.Contains("bereits ein CO₂-Äquivalent", textDe);
        }

        /// <summary>Schaltet <c>CurrentUICulture</c> um und stellt sie wieder her —
        /// <c>MyResource</c> folgt ihr, nicht <c>CurrentCulture</c>.</summary>
        private sealed class Sprachumschaltung : System.IDisposable
        {
            private readonly CultureInfo _vorher = CultureInfo.CurrentUICulture;
            private readonly CultureInfo _vorherThread =
                CultureInfo.DefaultThreadCurrentUICulture;

            public Sprachumschaltung(string name)
            {
                CultureInfo k = CultureInfo.GetCultureInfo(name);
                CultureInfo.CurrentUICulture = k;
                CultureInfo.DefaultThreadCurrentUICulture = k;
            }

            public void Dispose()
            {
                CultureInfo.CurrentUICulture = _vorher;
                CultureInfo.DefaultThreadCurrentUICulture = _vorherThread;
            }
        }
    }
}
