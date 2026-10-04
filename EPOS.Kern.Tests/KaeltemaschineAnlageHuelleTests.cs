using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KU3-4c — die Hülle des Erzeugerdialogs „Kältemaschinen im Projekt"</b> (<c>KaeltemaschineAnlageHuelle</c>)
    /// gegen die Arbeitskopie der Testdatenbank: Anlegen über die Gabe (Projektkopie und Anlagenzeile Typ 13),
    /// Laden samt Kennwerten der Kopie, Speichern der Eingaben in einem Vorgang, Löschen. Ohne Datenbank schweigen
    /// die Datenbankfälle; die Abbildung und die Prüfregeln prüft der erste Fall ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineAnlageHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        [Fact]
        public void Die_Pruefung_nimmt_die_Regeln_des_Kerns_und_die_Abbildung_haelt_jedes_Feld()
        {
            var d = new KaeltemaschineAnlageDaten { AnlagenId = 5, GeraetId = 50, Bezeichner = "KM", Anzahl = 2,
                                                    KuehlVorlauf = 9, KuehlHilfsstromanteil = 0.05, KuehlCarrierId = 3,
                                                    KuehlEigenerZaehler = true };
            Assert.Null(KaeltemaschineAnlageHuelle.Pruefen(d));
            KaeltemaschineAnlageModel m = KaeltemaschineAnlageHuelle.AlsModell(d, null);
            Assert.Equal((5, 50, "KM", 2, 9.0, 0.05, 3, true),
                         (m.AnlagenId, m.IdKaeltemaschine.Value, m.Bezeichner, m.Anzahl, m.KuehlVorlauf.Value,
                          m.KuehlHilfsstromanteil.Value, m.KuehlIdCarrier.Value, m.KuehlEigenerZaehler.Value));

            d.Anzahl = 0;
            Assert.Equal(R.KM_ANLAGE_ANZAHL_UNGUELTIG, KaeltemaschineAnlageHuelle.Pruefen(d));
            d.Anzahl = null;
            Assert.Equal(R.KM_ANLAGE_ANZAHL_UNGUELTIG, KaeltemaschineAnlageHuelle.Pruefen(d));
            d.Anzahl = 1;
            d.KuehlHilfsstromanteil = 1;
            Assert.Equal(R.KM_ANLAGE_HILFSSTROM_UNGUELTIG, KaeltemaschineAnlageHuelle.Pruefen(d));
            d.KuehlHilfsstromanteil = null;
            d.Bezeichner = " ";
            Assert.Equal(R.KM_ANLAGE_NAME_LEER, KaeltemaschineAnlageHuelle.Pruefen(d));
            Assert.Null(KaeltemaschineAnlageHuelle.Gaben(0));
        }

        [Fact]
        public void Die_Gaben_legen_an_laden_speichern_und_loeschen_in_der_Arbeitskopie()
        {
            if (!_db.Vorhanden || !KaeltemaschineSchema.Vollstaendig()) return;
            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", LUFTGEKUEHLT)),
                CultureInfo.InvariantCulture);

            IReadOnlyDictionary<string, object> g = KaeltemaschineAnlageHuelle.Gaben(PROJEKT);
            var anlagen = (Func<IReadOnlyList<KaeltemaschineAnlageDaten>>)g["Anlagen"];
            int vorher = anlagen().Count;

            // Die Katalogwahl: Kennwerte des Stammgeräts, dann Anlegen über die Gabe.
            var werte = ((Func<int, KaeltemaschineGeraetwerte>)g["Katalogwerte"])(stamm);
            Assert.Equal(LUFTGEKUEHLT, werte.Bezeichner);
            Assert.NotEmpty(werte.Kaltwasserstuetzstellen);
            Assert.Contains(((Func<IReadOnlyList<Katalogfilterzeile>>)g["Katalogzeilen"])(), z => z.Id == stamm);

            int id = ((Func<int, string, int>)g["Anlegen"])(stamm, "KM Halle");
            Assert.True(id > 0);
            KaeltemaschineAnlageDaten neu = anlagen().Single(a => a.AnlagenId == id);
            Assert.Equal(vorher + 1, anlagen().Count);
            Assert.Equal(("KM Halle", 1), (neu.Bezeichner, neu.Anzahl.Value));
            Assert.True(neu.GeraetId > 0);
            Assert.Equal(werte.Nennkaelteleistung, neu.Geraet.Nennkaelteleistung);
            Assert.Equal(werte.Rueckkuehlart, neu.Geraet.Rueckkuehlart);

            // Speichern: Anlagenzeile und Kühleingaben der Kopie in einem Vorgang.
            neu.Anzahl = 3;
            neu.KuehlVorlauf = 9;
            neu.KuehlHilfsstromanteil = 0.04;
            var speichern = (Func<KaeltemaschineAnlageDaten, string>)g["Speichern"];
            Assert.Null(speichern(neu));
            KaeltemaschineAnlageDaten gelesen = anlagen().Single(a => a.AnlagenId == id);
            Assert.Equal((3, 9.0, 0.04), (gelesen.Anzahl.Value, gelesen.KuehlVorlauf.Value, gelesen.KuehlHilfsstromanteil.Value));

            // Eine abgelehnte Eingabe schreibt nichts.
            gelesen.Anzahl = 0;
            Assert.Equal(R.KM_ANLAGE_ANZAHL_UNGUELTIG, speichern(gelesen));
            Assert.Equal(3, anlagen().Single(a => a.AnlagenId == id).Anzahl);

            // Löschen nimmt Anlagenzeile und verwaiste Projektkopie.
            ((Action<int>)g["Loeschen"])(id);
            Assert.Equal(vorher, anlagen().Count);
            Assert.Null(KaeltemaschineCtrl.LadenStill(neu.GeraetId.Value));
            Assert.Equal(R.KM_ANLAGE_FEHLT, speichern(neu));
        }
    }
}
