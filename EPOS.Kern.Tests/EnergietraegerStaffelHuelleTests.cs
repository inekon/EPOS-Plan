using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7b — <b>die Leistungspreis-Staffel in der Kostenverwaltung</b>: die HÜLLE
    /// der Energieträgerverwaltung gegen die Testdatenbank (Entscheid Q11, Anwender
    /// 22.09.2026; der Rest nach Empfehlung: die zweistufige Staffel zieht in die
    /// Kostenverwaltung neben die Energiepreisstruktur, Weg 2 aus Nach #291).
    ///
    /// <para>Geprüft wird der Kreis öffnen–eintragen–speichern–öffnen am Stromträger 60
    /// des Projekts 1030: Die Hülle gibt die Staffel nur beim STROMträger im
    /// PROJEKTkontext frei, liest sie aus der Projektübersteuerung, schreibt sie über
    /// den Kern-Controller (<see cref="EnergietraegerPreisCtrl.StaffelSchreiben"/>) in
    /// dieselbe Zeile, und ein geleertes Feld schreibt NULL. Eine Oberfläche mit
    /// Datenbank gibt es dabei nicht — die Hülle ist der einzige Schreibweg.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerStaffelHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int STROM = 60;       // „Elektrische Energie", ELECTRICITY
        private const int ERDGAS_E = 63;    // Brennstoff desselben Projekts

        private static IReadOnlyDictionary<string, object> Geladen(
            EnergietraegerHuelle h, int traegerId, out EnergietraegerStand stand)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(traegerId);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            EnergietraegerAnsicht a = laden(traegerId);
            Assert.NotNull(a.Stand);
            stand = a.Stand;
            return gaben;
        }

        private static bool Speichern(IReadOnlyDictionary<string, object> gaben) =>
            ((Func<bool>)gaben["Speichern"])();

        [Fact]
        public void Der_Stromtraeger_im_Projekt_pflegt_die_Staffel_ueber_die_Huelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), STROM, out EnergietraegerStand stand);
            Assert.True(stand.MitStaffel);
            Assert.Null(stand.StaffelGrenze);                 // Vorbestand: keine Staffel
            Assert.Null(stand.StaffelPreis1);
            Assert.Null(stand.StaffelPreis2);

            stand.StaffelGrenze = 1500;
            stand.StaffelPreis1 = 60;
            stand.StaffelPreis2 = 90;
            Assert.True(Speichern(gaben));

            LeistungspreisStaffel geschrieben = EnergietraegerPreisCtrl.StaffelLesen(PROJEKT, STROM);
            Assert.Equal(1500.0, geschrieben.GrenzeKW);
            Assert.Equal(60.0, geschrieben.Preis1EurKWa);
            Assert.Equal(90.0, geschrieben.Preis2EurKWa);

            // Wieder öffnen: Die Karte zeigt, was in der Datenbank steht.
            Geladen(new EnergietraegerHuelle(PROJEKT), STROM, out EnergietraegerStand wieder);
            Assert.Equal(1500.0, wieder.StaffelGrenze);
            Assert.Equal(60.0, wieder.StaffelPreis1);
            Assert.Equal(90.0, wieder.StaffelPreis2);
        }

        [Fact]
        public void Ein_geleertes_Feld_schaltet_die_Staffel_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT, STROM,
                new LeistungspreisStaffel { GrenzeKW = 1500, Preis1EurKWa = 60, Preis2EurKWa = 90 });

            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), STROM, out EnergietraegerStand stand);
            Assert.Equal(90.0, stand.StaffelPreis2);

            stand.StaffelGrenze = null;
            stand.StaffelPreis1 = null;
            stand.StaffelPreis2 = null;
            Assert.True(Speichern(gaben));

            LeistungspreisStaffel danach = EnergietraegerPreisCtrl.StaffelLesen(PROJEKT, STROM);
            Assert.False(danach.Gepflegt);
            Assert.Null(danach.GrenzeKW);
        }

        [Fact]
        public void Brennstoff_und_Katalog_fuehren_keine_Staffel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Ein Brennstoffträger desselben Projekts: keine Staffel, und sein Speichern
            // schreibt keine.
            IReadOnlyDictionary<string, object> gas =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out EnergietraegerStand gasStand);
            Assert.False(gasStand.MitStaffel);
            gasStand.StaffelPreis1 = 60;                      // nicht auf der Maske - wirkt nicht
            Assert.True(Speichern(gas));
            Assert.False(EnergietraegerPreisCtrl.StaffelLesen(PROJEKT, ERDGAS_E).Gepflegt);

            // Der Katalogkontext: Die Staffel ist eine Angabe des Projekts.
            Geladen(new EnergietraegerHuelle(0), STROM, out EnergietraegerStand katalog);
            Assert.False(katalog.MitStaffel);
        }

        [Fact]
        public void Die_Kartentexte_tragen_die_Staffel_in_beiden_Sprachen()
        {
            using var db = new TestDatenbank();      // Gaben() liest die Trägerliste
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> texte =
                (IReadOnlyDictionary<string, object>)new EnergietraegerHuelle(0).Gaben()["KarteTexte"];
            Assert.Equal("Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)", texte["TitelStaffel"]);
            Assert.Equal("Staffelgrenze", texte["LabelStaffelGrenze"]);
            Assert.Equal("Preis bis zur Grenze", texte["LabelStaffelPreis1"]);
            Assert.Equal("Preis über der Grenze", texte["LabelStaffelPreis2"]);
            Assert.Contains("Viertelstundenspitze", (string)texte["HinweisStaffel"], StringComparison.Ordinal);

            System.Resources.ResourceManager rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;
            foreach (string schluessel in new[] { "ETV_STAFFEL_TITEL", "ETV_STAFFEL_GRENZE", "ETV_STAFFEL_PREIS1",
                                                  "ETV_STAFFEL_PREIS2", "ETV_STAFFEL_HINWEIS",
                                                  "KI_DLG_ET_STAFFEL_GRENZE_ERL" })
            {
                string de = rm.GetString(schluessel, new System.Globalization.CultureInfo("de-DE"));
                string en = rm.GetString(schluessel, new System.Globalization.CultureInfo("en-US"));
                Assert.False(string.IsNullOrEmpty(de), schluessel + " (de)");
                Assert.False(string.IsNullOrEmpty(en), schluessel + " (en)");
                Assert.NotEqual(de, en);
            }
        }
    }
}
