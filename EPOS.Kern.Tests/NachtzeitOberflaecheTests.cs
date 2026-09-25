using System;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using KiKern;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nachtzeit an der Oberfläche</b> (Entscheid E43, Schemaschritt <see cref="NachtzeitSchema.SCHRITT"/>):
    /// Der Katalogeditor bildet Beginn und Ende NULL-erhaltend ab, die Prüfung im Arbeitsstand ist die
    /// Regel des Kerns (<see cref="Nachtzeit.Pruefen"/>), die Bestandswoche folgt der Nachtzeit des
    /// Satzes, der Lesemodus der Verwaltung zeigt leer die Vorgabe, und der Assistent führt beide Felder
    /// in beiden Gebäudemasken.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class NachtzeitOberflaecheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Katalogsatz → Feldsatz → Katalogsatz: die Stunden reisen mit, leer bleibt leer — nie 0.</summary>
        [Fact]
        public void Der_Katalogeditor_bildet_die_Nachtzeit_NULL_erhaltend_ab()
        {
            var satz = new GebaeudeModel { Gebaeudename = "Nachtprobe", Nachtabsenkung_Beginn = 23, Nachtabsenkung_Ende = 5 };

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(satz);
            Assert.Equal(23, d.NachtBeginn);
            Assert.Equal(5, d.NachtEnde);
            Assert.Equal(23, d.Kopie().NachtBeginn);
            GebaeudeModel zurueck = GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel());
            Assert.Equal(23, zurueck.Nachtabsenkung_Beginn);
            Assert.Equal(5, zurueck.Nachtabsenkung_Ende);

            // "Speichern unter" auf einen leeren Satz: die leeren Felder überschreiben die alten Werte mit NULL.
            GebaeudeModel leer = GebaeudeKatalogHuelle.NachModell(GebaeudeKatalogHuelle.AusModell(new GebaeudeModel()), satz);
            Assert.Null(leer.Nachtabsenkung_Beginn);
            Assert.Null(leer.Nachtabsenkung_Ende);
        }

        /// <summary>
        /// Die Regel steht einmal: beide leer und ein gültiges Paar gehen durch, nur eine Grenze, eine
        /// Stunde außerhalb 0 … 23 und Beginn = Ende melden — mit den Texten der Ressourcen.
        /// </summary>
        [Theory]
        [InlineData(null, null, null)]
        [InlineData(22, 6, null)]
        [InlineData(0, 6, null)]
        [InlineData(22, null, "Bitte Beginn und Ende der Nachtabsenkung beide eingeben oder beide leer lassen (leer = 22 bis 6 Uhr).")]
        [InlineData(null, 6, "Bitte Beginn und Ende der Nachtabsenkung beide eingeben oder beide leer lassen (leer = 22 bis 6 Uhr).")]
        [InlineData(5, 5, "Beginn und Ende der Nachtabsenkung dürfen nicht gleich sein.")]
        [InlineData(24, 6, "Die Nachtabsenkung beginnt und endet zu einer vollen Stunde von 0 bis 23.")]
        public void Der_Arbeitsstand_prueft_die_Nachtzeit_mit_der_Regel_des_Kerns(int? beginn, int? ende, string meldung)
        {
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(new GebaeudeKatalogDaten { Name = "Probe", NachtBeginn = beginn, NachtEnde = ende }, neu: false);

            GebaeudePruefbefund befund = arbeit.Pruefen(false, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());

            if (meldung != null)
            {
                Assert.Equal(meldung, befund?.Meldung);
                Assert.True(befund.Temperaturen, "Die Regel hängt am zweiten Reiter.");
            }
            else
                Assert.False(befund?.Meldung?.Contains("Nachtabsenkung", StringComparison.Ordinal) ?? false, befund?.Meldung);
        }

        /// <summary>Eine geänderte Stunde zählt als Abweichung des Arbeitsstands.</summary>
        [Fact]
        public void Eine_geaenderte_Nachtzeit_zaehlt_als_Abweichung()
        {
            var geladen = new GebaeudeKatalogDaten { Name = "Probe" };
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(geladen, neu: false);
            int vorher = arbeit.Abweichungen(geladen);
            arbeit.Stand.NachtBeginn = 23;
            arbeit.Stand.NachtEnde = 5;
            Assert.Equal(vorher + 2, arbeit.Abweichungen(geladen));
        }

        /// <summary>Die Bestandswoche des Editors (Wochenraster ohne Zeitprogramm) folgt der Nachtzeit des Satzes.</summary>
        [Fact]
        public void Die_Bestandswoche_des_Editors_folgt_der_Nachtzeit_des_Satzes()
        {
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(new GebaeudeKatalogDaten { Name = "Probe", SollTag = 20, NachtAbsenkung = 18 }, neu: false);
            double[] vorgabe = arbeit.Bestandswoche;
            Assert.Equal(18.0, vorgabe[5]);
            Assert.Equal(20.0, vorgabe[6]);
            Assert.Equal(20.0, vorgabe[21]);
            Assert.Equal(18.0, vorgabe[22]);

            arbeit.Stand.NachtBeginn = 23;
            arbeit.Stand.NachtEnde = 5;
            double[] eigen = arbeit.Bestandswoche;
            Assert.Equal(20.0, eigen[5]);
            Assert.Equal(20.0, eigen[22]);
            Assert.Equal(18.0, eigen[23]);
            Assert.Equal(18.0, eigen[4]);
        }

        /// <summary>Der Lesemodus der Verwaltung zeigt gesetzte Stunden, leer die Vorgabe — wie der Platzhalter.</summary>
        [Fact]
        public void Der_Lesemodus_der_Verwaltung_zeigt_leer_die_Vorgabe()
        {
            var leer = GebaeudeAdminHuelle.AlleDaten(new GebaeudeModel());
            Assert.Contains(leer, w => w.Name.StartsWith("Nachtabsenkung von", StringComparison.Ordinal) && w.Wert == "Vorgabe 22" && w.Einheit == "h");
            Assert.Contains(leer, w => w.Name.StartsWith("Nachtabsenkung bis", StringComparison.Ordinal) && w.Wert == "Vorgabe 6");

            var gesetzt = GebaeudeAdminHuelle.AlleDaten(new GebaeudeModel { Nachtabsenkung_Beginn = 23, Nachtabsenkung_Ende = 5 });
            Assert.Contains(gesetzt, w => w.Name.StartsWith("Nachtabsenkung von", StringComparison.Ordinal) && w.Wert == "23");
            Assert.Contains(gesetzt, w => w.Name.StartsWith("Nachtabsenkung bis", StringComparison.Ordinal) && w.Wert == "5");
        }

        /// <summary>Die Texte kommen aus den Ressourcen, in beiden Sprachen.</summary>
        [Fact]
        public void Die_Texte_stehen_in_beiden_Sprachen()
        {
            Assert.Equal("Nachtabsenkung von :", R.GEBK_LBL_NACHT_BEGINN);
            Assert.Equal("Nachtabsenkung bis :", R.GEBK_LBL_NACHT_ENDE);

            GebaeudePrueftexte p = GebaeudeKatalogHuelle.Prueftexte();
            Assert.Equal("Nachtabsenkung von", p.FeldNachtBeginn);
            Assert.Equal("Nachtabsenkung bis", p.FeldNachtEnde);
            Assert.Equal(R.GEBK_MSG_NACHTZEIT_NUR_EINE, p.MeldungNachtzeitNurEine);
            Assert.Equal(R.GEBK_MSG_NACHTZEIT_GLEICH, p.MeldungNachtzeitGleich);
            Assert.Equal(R.GEBK_MSG_NACHTZEIT_BEREICH, p.MeldungNachtzeitBereich);

            var en = CultureInfo.GetCultureInfo("en-US");
            foreach (string k in new[] { "GEBK_LBL_NACHT_BEGINN", "GEBK_LBL_NACHT_ENDE", "GEBK_MSG_NACHTZEIT_NUR_EINE",
                                         "GEBK_MSG_NACHTZEIT_GLEICH", "GEBK_MSG_NACHTZEIT_BEREICH",
                                         "KI_DLG_GEBK_NACHT_BEGINN_ERL", "KI_DLG_GEBK_NACHT_ENDE_ERL",
                                         "SIMENG_NACHTZEIT_NUR_EINE", "SIMENG_NACHTZEIT_BEREICH", "SIMENG_NACHTZEIT_GLEICH" })
            {
                string de = R.ResourceManager.GetString(k, CultureInfo.GetCultureInfo("de-DE"));
                string e = R.ResourceManager.GetString(k, en);
                Assert.False(string.IsNullOrWhiteSpace(de), k);
                Assert.False(string.IsNullOrWhiteSpace(e), k);
                Assert.NotEqual(de, e);
            }
            Assert.Equal("Night setback from:", R.ResourceManager.GetString("GEBK_LBL_NACHT_BEGINN", en));
        }

        /// <summary>
        /// Der Assistent führt Beginn und Ende in Katalogeditor und Verwaltung als Ganzzahl 0 … 23, leer
        /// erlaubt, gleich hinter der Nachtabsenkung.
        /// </summary>
        [Theory]
        [InlineData(KiMaskennamen.GEBAEUDE_KATALOG)]
        [InlineData(KiMaskennamen.GEBAEUDE_ADMIN)]
        public void Der_Assistent_fuehrt_die_Nachtzeit_hinter_der_Nachtabsenkung(string maske)
        {
            KiDialog dialog = KiDialoge.Katalog.Finde(maske);
            Assert.NotNull(dialog);

            foreach ((string name, string pfad, string anzeige) in new[]
                     {
                         ("nacht_beginn", "GebaeudeKatalogKiSicht.NachtBeginn", R.GEBK_LBL_NACHT_BEGINN),
                         ("nacht_ende", "GebaeudeKatalogKiSicht.NachtEnde", R.GEBK_LBL_NACHT_ENDE),
                     })
            {
                KiDialogFeld f = dialog.FindeFeld(name);
                Assert.NotNull(f);
                Assert.Equal(pfad, f.Eigenschaftspfad);
                Assert.Equal(KiParameterTyp.Ganzzahl, f.Typ);
                Assert.True(f.LeerErlaubt);
                Assert.Equal(0.0, f.Min);
                Assert.Equal(23.0, f.Max);
                Assert.Equal(anzeige, f.Anzeigename);
            }

            var namen = dialog.Felder.Select(f => f.Name).ToList();
            int nacht = namen.IndexOf("nachtabsenkung");
            Assert.Equal(nacht + 1, namen.IndexOf("nacht_beginn"));
            Assert.Equal(nacht + 2, namen.IndexOf("nacht_ende"));
        }
    }
}
