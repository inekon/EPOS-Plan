using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Alle Modulparameter eines PV-Katalogsatzes</b> — Anwenderwunsch <b>W6‑E‑1</b>
    /// der Windows-Abnahme vom 05.09.2026: „optional sollten beim ausgewählten PV-Modul
    /// alle Eigenschaften/Parameter angezeigt werden."
    ///
    /// <para><b>Was hier geprüft wird.</b> <c>PhotovoltaikStammCtrl.Detail</c> liest
    /// seither ALLE Katalogspalten (ein Lesevorgang, kein zweiter daneben), und
    /// <c>PhotovoltaikStammCtrl.Parameterzeilen</c> macht daraus die dreizehn
    /// Anzeigezeilen des Aufklappers. Der Rechenweg ist unberührt — der Referenzlauf
    /// sieht davon nichts, und genau deshalb steht der Nachweis hier.</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN</b> aus
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>. „Ablytek 6MN6A275" ist das Modul des
    /// Anwenderfotos (275,1912 in der Spalte <c>Leistung</c>); „LG Electronics
    /// LG 320 N1K-A5" ist der Satz, der drei Spalten als NULL führt und damit den Strich
    /// belegt.</para>
    ///
    /// <para><b>Die Sprache wird im Rumpf gepinnt, mit <c>finally</c></b> (Regel seit
    /// W8): Beschriftungen kommen aus <c>MyResource</c> (<c>CurrentUICulture</c>), die
    /// Zahlen aus der Kultur des Anwenders (<c>CurrentCulture</c>) — ein Gesamtlauf unter
    /// <c>LANG=en_US</c> fände sonst andere Texte und andere Trennzeichen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvModulparameterTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public PvModulparameterTests(TestDatenbank db) { _db = db; }

        private const string MODUL = "Ablytek 6MN6A275";
        private const string MODUL_MIT_NULL = "LG Electronics LG 320 N1K-A5";
        private const string STRICH = PhotovoltaikStammCtrl.PARAMETER_LEER;

        private static void MitSprache(string kuerzel, Action fall)
        {
            CultureInfo vorherUi = CultureInfo.CurrentUICulture;
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(kuerzel);
                CultureInfo.CurrentCulture = new CultureInfo(kuerzel);
                fall();
            }
            finally
            {
                CultureInfo.CurrentUICulture = vorherUi;
                CultureInfo.CurrentCulture = vorher;
            }
        }

        private static string Wert(IReadOnlyList<PhotovoltaikStammCtrl.ModulParameter> zeilen,
                                   string schluessel)
            => zeilen.First(z => z.Schluessel == schluessel).Wert;

        private static PhotovoltaikStammCtrl.ModulParameter Zeile(
            IReadOnlyList<PhotovoltaikStammCtrl.ModulParameter> zeilen, string schluessel)
            => zeilen.First(z => z.Schluessel == schluessel);

        // =================================================================================
        // 1 - Der Lesevorgang traegt jetzt alle Spalten
        // =================================================================================

        /// <summary>
        /// <c>Detail</c> lieferte vier Werte (Name, Beschreibung, Firma, Leistung); seit
        /// W6‑E‑1 sind es alle. Ohne diese Erweiterung bräuchte der Aufklapper einen
        /// ZWEITEN <c>SELECT</c> auf dieselbe Zeile.
        /// </summary>
        [Fact]
        public void Der_Detailsatz_traegt_alle_Katalogspalten()
        {
            if (!_db.Vorhanden) return;

            PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(MODUL);
            Assert.NotNull(d);

            // Die vier Felder des Bestands - unveraendert.
            Assert.Equal(MODUL, d.Bezeichner);
            Assert.Equal("Ablytek", d.Firma);
            Assert.Equal(275.1912, d.Leistung, 6);

            // Die dreizehn neuen.
            Assert.Equal(16.9140135218193, d.Wirkungsgrad!.Value, 9);
            Assert.Equal(30.99, d.UMpp!.Value, 6);
            Assert.Equal(38.97, d.ULeerlauf!.Value, 6);
            Assert.Equal(8.88, d.IMpp!.Value, 6);
            Assert.Equal(9.42, d.IKurzschluss!.Value, 6);
            Assert.Equal(-0.4509, d.GammaPmp!.Value, 6);
            Assert.Equal(1.64, d.Laenge!.Value, 6);
            Assert.Equal(0.992, d.Breite!.Value, 6);
        }

        /// <summary>
        /// NULL bleibt NULL. Der Katalog führt für „LG 320 N1K-A5" drei Spalten ohne
        /// Wert; als 0 gelesen wären sie eine gemessene Null.
        /// </summary>
        [Fact]
        public void Eine_NULL_Spalte_kommt_als_null_und_nicht_als_null_Komma_null()
        {
            if (!_db.Vorhanden) return;

            PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(MODUL_MIT_NULL);
            Assert.NotNull(d);

            Assert.Null(d.AlphaSc);
            Assert.Null(d.BetaOc);
            Assert.Null(d.TNoct);
            Assert.Equal(18.68, d.Wirkungsgrad!.Value, 6);
        }

        // =================================================================================
        // 2 - Die dreizehn Anzeigezeilen
        // =================================================================================

        /// <summary>
        /// Dreizehn Zeilen in der Reihenfolge des Anwenderwunsches — und keine davon
        /// doppelt zu dem, was der Block darüber ohnehin zeigt (Name, Hersteller,
        /// Beschreibung, Modulleistung, Gesamtleistung).
        /// </summary>
        [Fact]
        public void Der_Block_fuehrt_dreizehn_Zeilen_in_fester_Reihenfolge()
        {
            if (!_db.Vorhanden) return;

            MitSprache("de-DE", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(
                    PhotovoltaikStammCtrl.Detail(MODUL));

                Assert.Equal(
                    new[]
                    {
                        ModulKatalogProfil.FeldWirkungsgrad, ModulKatalogProfil.FeldUMpp,
                        ModulKatalogProfil.FeldULeerlauf, ModulKatalogProfil.FeldIMpp,
                        ModulKatalogProfil.FeldIKurzschluss, "ALPHA_SC", "BETA_OC",
                        ModulKatalogProfil.FeldTempKoeff, ModulKatalogProfil.FeldTNoct,
                        ModulKatalogProfil.FeldLaenge, ModulKatalogProfil.FeldBreite,
                        ModulKatalogProfil.FeldModulkosten, ModulKatalogProfil.FeldTechnologie
                    },
                    zeilen.Select(z => z.Schluessel).ToArray());
            });
        }

        /// <summary>
        /// Ohne Katalogsatz keine Zeile — der Aufklapper erscheint dann gar nicht.
        /// </summary>
        [Fact]
        public void Ohne_Satz_gibt_es_keine_Zeile()
        {
            Assert.Empty(PhotovoltaikStammCtrl.Parameterzeilen(null));
        }

        /// <summary>
        /// Die eingefrorenen Werte des Anwendermoduls, in der Kultur des Anwenders.
        /// Der Wirkungsgrad trägt zwei Nachkommastellen wie im Katalogdialog
        /// (<c>PvAdminHuelle.Anzeige</c>), alles Übrige steht roh da — wer beide Masken
        /// nebeneinanderlegt, liest dieselben Ziffern.
        /// </summary>
        [Fact]
        public void Die_Werte_stehen_so_da_wie_im_Katalogdialog()
        {
            if (!_db.Vorhanden) return;

            MitSprache("de-DE", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(
                    PhotovoltaikStammCtrl.Detail(MODUL));

                Assert.Equal("16,91", Wert(zeilen, ModulKatalogProfil.FeldWirkungsgrad));
                Assert.Equal("30,99", Wert(zeilen, ModulKatalogProfil.FeldUMpp));
                Assert.Equal("38,97", Wert(zeilen, ModulKatalogProfil.FeldULeerlauf));
                Assert.Equal("8,88", Wert(zeilen, ModulKatalogProfil.FeldIMpp));
                Assert.Equal("9,42", Wert(zeilen, ModulKatalogProfil.FeldIKurzschluss));
                Assert.Equal("-0,4509", Wert(zeilen, ModulKatalogProfil.FeldTempKoeff));
                Assert.Equal("1,64", Wert(zeilen, ModulKatalogProfil.FeldLaenge));
                Assert.Equal("0,992", Wert(zeilen, ModulKatalogProfil.FeldBreite));
            });
        }

        /// <summary>
        /// <b>Nicht gepflegt heißt „–", nicht 0.</b> Der Katalog führt beides — NULL
        /// (LG 320: alpha_SC, beta_OC, T_NOCT) und die 0 des Bestands (Modulkosten,
        /// Technologie leer) —, und beides ist dieselbe Aussage: Der Wert steht nicht in
        /// der Datenbank. Eine angezeigte 0 wäre eine Behauptung.
        /// </summary>
        [Fact]
        public void Ein_nicht_gepflegter_Wert_zeigt_den_Strich()
        {
            if (!_db.Vorhanden) return;

            MitSprache("de-DE", () =>
            {
                var lg = PhotovoltaikStammCtrl.Parameterzeilen(
                    PhotovoltaikStammCtrl.Detail(MODUL_MIT_NULL));

                // NULL in der Datenbank
                Assert.Equal(STRICH, Wert(lg, "ALPHA_SC"));
                Assert.Equal(STRICH, Wert(lg, "BETA_OC"));
                Assert.Equal(STRICH, Wert(lg, ModulKatalogProfil.FeldTNoct));

                // 0 in der Datenbank - dieselbe Aussage
                Assert.Equal(STRICH, Wert(lg, ModulKatalogProfil.FeldModulkosten));

                // leere Technologie
                Assert.Equal(STRICH, Wert(lg, ModulKatalogProfil.FeldTechnologie));

                // Und was gepflegt ist, steht da.
                Assert.Equal("18,68", Wert(lg, ModulKatalogProfil.FeldWirkungsgrad));
            });
        }

        // =================================================================================
        // 3 - Eine Wahrheit: Beschriftung und Einheit
        // =================================================================================

        /// <summary>
        /// <b>Beschriftung und Einheit kommen aus DEM Katalogprofil</b>, aus dem sie auch
        /// der Katalogdialog nimmt — es gibt für einen Modulwert genau einen Text im
        /// Haus. Geprüft wird nicht der Wortlaut, sondern die HERKUNFT: Jede Zeile ist
        /// zeichengleich mit ihrem Feld in <see cref="ModulKatalogProfil"/>.
        /// </summary>
        [Fact]
        public void Beschriftung_und_Einheit_stammen_aus_dem_Katalogprofil()
        {
            if (!_db.Vorhanden) return;

            MitSprache("de-DE", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(
                    PhotovoltaikStammCtrl.Detail(MODUL));

                ModulKatalogProfil profil = ModulKatalogProfil.Finde(
                    ModulKatalogArt.Photovoltaik, Uebersetzt);

                foreach (var z in zeilen)
                {
                    ModulKatalogFeld feld = profil.Felder
                        .FirstOrDefault(f => f.Schluessel == z.Schluessel);
                    if (feld == null) continue;   // alpha_SC / beta_OC - siehe naechster Fall

                    Assert.Equal(feld.Bezeichnung, z.Bezeichnung);
                    Assert.Equal(feld.Einheit, z.Einheit);
                }

                // Stichprobe, damit der Fall nicht leer durchliefe.
                Assert.Equal("Spannung im MPP (Umpp):", Zeile(zeilen, ModulKatalogProfil.FeldUMpp).Bezeichnung);
                Assert.Equal("V", Zeile(zeilen, ModulKatalogProfil.FeldUMpp).Einheit);
            });
        }

        /// <summary>
        /// Die zwei Temperaturkoeffizienten führt der Katalogdialog NICHT — er kann sie
        /// nicht pflegen (<c>PhotovoltaikStammCtrl.SpeichernAus</c> trägt sie deshalb
        /// unverändert weiter). Ihre Beschriftungen stehen dort, wo der Bestand sie
        /// führt: im Modulimport (W13), samt Einheit im Text.
        /// </summary>
        [Fact]
        public void Die_zwei_Koeffizienten_tragen_die_Beschriftung_des_Modulimports()
        {
            if (!_db.Vorhanden) return;

            MitSprache("de-DE", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(
                    PhotovoltaikStammCtrl.Detail(MODUL));

                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PVIMP_LBL_ALPHA_ISC,
                             Zeile(zeilen, "ALPHA_SC").Bezeichnung);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PVIMP_LBL_BETA_VOC,
                             Zeile(zeilen, "BETA_OC").Bezeichnung);

                // Die Einheit steckt im Text - hinter dem Feld steht dann nichts.
                Assert.Equal("", Zeile(zeilen, "ALPHA_SC").Einheit);
                Assert.Equal("", Zeile(zeilen, "BETA_OC").Einheit);
            });
        }

        /// <summary>
        /// Die Zelltechnologie ist ein CODE in der Datenbank und ein Klartext auf dem
        /// Schirm — dieselben fünf Texte wie im Katalogdialog.
        /// </summary>
        [Fact]
        public void Die_Zelltechnologie_erscheint_im_Klartext()
        {
            var mit = new PhotovoltaikStammCtrl.ModulDetail(
                "Probe", "", "Werk", 300, Technologie: DbWerte.PV_TECHNOLOGIE_C_SI);

            MitSprache("de-DE", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(mit);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PVM_TECHNOLOGIE_C_SI,
                             Wert(zeilen, ModulKatalogProfil.FeldTechnologie));
            });
        }

        /// <summary>
        /// Ein unbekannter Code wird GEZEIGT, nicht verschluckt: Er steht so in der
        /// Datenbank, und wer ihn sucht, muss ihn lesen können.
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Technologiecode_bleibt_sichtbar()
        {
            var seltsam = new PhotovoltaikStammCtrl.ModulDetail(
                "Probe", "", "Werk", 300, Technologie: "Perowskit");

            MitSprache("de-DE", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(seltsam);
                Assert.Equal("Perowskit", Wert(zeilen, ModulKatalogProfil.FeldTechnologie));
            });
        }

        // =================================================================================
        // 4 - Englisch
        // =================================================================================

        /// <summary>
        /// Unter <c>en-US</c> stehen die englischen Beschriftungen da und die Zahlen
        /// tragen den Punkt — dieselbe Quelle, dieselbe Regel.
        /// </summary>
        [Fact]
        public void Auf_Englisch_stehen_die_englischen_Texte_und_der_Punkt()
        {
            if (!_db.Vorhanden) return;

            MitSprache("en-US", () =>
            {
                var zeilen = PhotovoltaikStammCtrl.Parameterzeilen(
                    PhotovoltaikStammCtrl.Detail(MODUL));

                Assert.Equal("Efficiency:", Zeile(zeilen, ModulKatalogProfil.FeldWirkungsgrad).Bezeichnung);
                Assert.Equal("Voltage at MPP (Umpp):", Zeile(zeilen, ModulKatalogProfil.FeldUMpp).Bezeichnung);
                Assert.Equal("Cell technology:", Zeile(zeilen, ModulKatalogProfil.FeldTechnologie).Bezeichnung);

                Assert.Equal("16.91", Wert(zeilen, ModulKatalogProfil.FeldWirkungsgrad));
                Assert.Equal("30.99", Wert(zeilen, ModulKatalogProfil.FeldUMpp));
                Assert.Equal("-0.4509", Wert(zeilen, ModulKatalogProfil.FeldTempKoeff));

                // Der Strich ist sprachneutral.
                Assert.Equal(STRICH, Wert(zeilen, ModulKatalogProfil.FeldModulkosten));
            });
        }

        /// <summary>
        /// Der Aufklapper trägt in beiden Sprachen einen Text — der Schlüssel
        /// <c>PVD_AUFKLAPP_PARAMETER</c> ist in beiden Katalogen gepflegt.
        /// </summary>
        [Fact]
        public void Die_Beschriftung_des_Aufklappers_steht_in_beiden_Sprachen()
        {
            MitSprache("de-DE", () =>
                Assert.Equal("Alle Modulparameter anzeigen",
                             WindowsFormsApplication1.MyResource.Resource.PVD_AUFKLAPP_PARAMETER));

            MitSprache("en-US", () =>
                Assert.Equal("Show all module parameters",
                             WindowsFormsApplication1.MyResource.Resource.PVD_AUFKLAPP_PARAMETER));
        }

        // =================================================================================
        // 5 - W6-O-5: „Gesamtleistung in kW"
        // =================================================================================

        /// <summary>
        /// <b>Anwenderentscheid W6‑O‑5 vom 05.09.2026.</b> Der Projektdialog zeigte
        /// „Gesamtleistung [KW]" über einer Zahl in WATT: <c>Tab_PV.Leistung</c> ist die
        /// Modulleistung in Watt (der Katalogdialog nennt sie „Nennleistung (Pmax)" mit
        /// der Einheit W), und der Dialog zeigte Anzahl mal Leistung roh an. Zehn Module
        /// des Anwenderfotos ergaben so „2751,912" unter der Beschriftung „KW".
        ///
        /// <para>Seither wandelt <c>PhotovoltaikCtrl.GesamtleistungText</c> — dieselbe
        /// Wandlung wie <c>KwpSumme</c> (Summe / 1000), drei Nachkommastellen.</para>
        /// </summary>
        [Fact]
        public void Zehn_Module_ergeben_zwei_Komma_sieben_fuenf_zwei_kW()
        {
            MitSprache("de-DE", () =>
            {
                // 10 x 275,1912 W = 2751,912 W = 2,752 kW.
                Assert.Equal("2,752", PhotovoltaikCtrl.GesamtleistungText(10 * 275.1912));

                // Und mit dem gerundeten Katalogwert der Anzeige dasselbe Bild.
                Assert.Equal("2,752", PhotovoltaikCtrl.GesamtleistungText(10 * 275.19));
            });
        }

        /// <summary>
        /// Die Zahl folgt der Kultur des Anwenders — Punkt statt Komma, und der
        /// Tausenderpunkt wird zum Tausenderkomma. Drei Nachkommastellen bleiben.
        /// </summary>
        [Fact]
        public void Die_Gesamtleistung_folgt_der_Kultur()
        {
            MitSprache("en-US", () =>
            {
                Assert.Equal("2.752", PhotovoltaikCtrl.GesamtleistungText(10 * 275.1912));
                Assert.Equal("1,200.000", PhotovoltaikCtrl.GesamtleistungText(1200000.0));
            });

            MitSprache("de-DE", () =>
            {
                Assert.Equal("1.200,000", PhotovoltaikCtrl.GesamtleistungText(1200000.0));
                Assert.Equal("0,000", PhotovoltaikCtrl.GesamtleistungText(0.0));
            });
        }

        /// <summary>
        /// <b>Die zwei Beschriftungen tragen ihre wahre Einheit</b> (W6‑O‑5): die
        /// Modulleistung Watt, die Gesamtleistung Kilowatt — in beiden Sprachkatalogen.
        /// Der englische Text der Gesamtleistung sagte „[kW]" schon immer; er hat als
        /// einziger nichts zu ändern gehabt.
        /// </summary>
        [Fact]
        public void Die_zwei_Leistungsbeschriftungen_tragen_W_und_kW()
        {
            MitSprache("de-DE", () =>
            {
                Assert.Equal("Modul Leistung [W]:",
                             WindowsFormsApplication1.MyResource.Resource.PVD_LBL_LEISTUNG);
                Assert.Equal("Gesamtleistung [kW]:",
                             WindowsFormsApplication1.MyResource.Resource.PVD_LBL_GESAMTLEISTUNG);
            });

            MitSprache("en-US", () =>
            {
                Assert.Equal("Module power [W]:",
                             WindowsFormsApplication1.MyResource.Resource.PVD_LBL_LEISTUNG);
                Assert.Equal("Total power [kW]:",
                             WindowsFormsApplication1.MyResource.Resource.PVD_LBL_GESAMTLEISTUNG);
            });
        }

        /// <summary>
        /// <b>Die Modulleistung selbst bleibt ROH.</b> Nur die Beschriftung hat sich
        /// geändert: Der Katalogsatz führt Watt, und genau die Zahl steht im Feld — der
        /// Wert ist derselbe wie im Katalogdialog (Anwenderentscheid W6‑O‑5, Punkt 2).
        /// </summary>
        [Fact]
        public void Die_Modulleistung_bleibt_der_rohe_Wattwert()
        {
            if (!_db.Vorhanden) return;

            PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(MODUL);
            Assert.NotNull(d);
            Assert.Equal(275.1912, d.Leistung, 6);
        }

        private static string Uebersetzt(string schluessel)
        {
            string t = null;
            try { t = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }
    }
}
