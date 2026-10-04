using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KU3-1, Teil 2 — die Hülle der Verwaltung „Kältemaschinen"</b> (<c>KaeltemaschineKatalogHuelle</c>)
    /// gegen die Arbeitskopie der Testdatenbank: Laden der Saat, Speichern einer Kopie samt Kennlinie,
    /// Löschen, Rückkühlart als Listenplatz. Ohne Datenbank (oder auf einem Stand vor dem Schemaschritt
    /// der Kältemaschine) schweigen die Datenbankfälle; die Abbildung prüft der erste Fall ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineKatalogHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool MitKatalog => _db.Vorhanden && KaeltemaschineSchema.Vollstaendig();

        [Fact]
        public void Die_Abbildung_fuehrt_den_Listenplatz_nie_den_Anzeigetext()
        {
            IReadOnlyList<string> texte = KaeltemaschineKatalogHuelle.Rueckkuehlarten();
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLARTEN.Count, texte.Count);
            Assert.DoesNotContain(texte, t => KaeltemaschineSchema.RUECKKUEHLARTEN.Contains(t));

            for (int i = 0; i < texte.Count; i++)
            {
                var d = new KaeltemaschineDaten { Bezeichner = "X", RueckkuehlartIndex = i };
                KaeltemaschineModel m = KaeltemaschineKatalogHuelle.AlsModell(d);
                Assert.Equal(KaeltemaschineSchema.RUECKKUEHLARTEN[i], m.Rueckkuehlart);
                Assert.Equal(i, KaeltemaschineKatalogHuelle.AlsDaten(m).RueckkuehlartIndex);
            }
            Assert.Null(KaeltemaschineKatalogHuelle.AlsModell(new KaeltemaschineDaten { Bezeichner = "X", RueckkuehlartIndex = 9 }).Rueckkuehlart);

            var doppelt = new KaeltemaschineDaten { Bezeichner = "X" };
            doppelt.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = 35, Kaltwassertemperatur = 7, Eer = 3 });
            doppelt.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = 35, Kaltwassertemperatur = 7, Eer = 4 });
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KM_MSG_KENNLINIE_DOPPELT, KaeltemaschineKatalogHuelle.Pruefen(doppelt));
            doppelt.Kennlinie[1].Kaltwassertemperatur = null;
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KM_MSG_KENNLINIE_TEMPERATUR_LEER, KaeltemaschineKatalogHuelle.Pruefen(doppelt));
        }

        [Fact]
        public void Die_Gaben_laden_die_Saat_speichern_eine_Kopie_und_loeschen_sie()
        {
            if (!MitKatalog) return;
            IReadOnlyDictionary<string, object> g = KaeltemaschineKatalogHuelle.Gaben();

            var zeilen = ((Func<IReadOnlyList<Katalogfilterzeile>>)g["Katalogzeilen"])();
            Assert.Equal(3, zeilen.Count);
            Assert.All(zeilen, z => Assert.True(z.Geschuetzt));
            Assert.Contains(zeilen, z => z.Text(Katalogfilterprofil.SpRueckkuehlart) == WindowsFormsApplication1.MyResource.Resource.KM_RUECKKUEHLART_TROCKENKUEHLER);

            var lies = (Func<int, KaeltemaschineDaten>)g["Lies"];
            var speichern = (Func<KaeltemaschineDaten, KaeltemaschineSpeicherErgebnis>)g["Speichern"];
            var loeschen = (Func<int, KaeltemaschineSpeicherErgebnis>)g["Loeschen"];
            var duplizieren = (Func<int, string, KaeltemaschineSpeicherErgebnis>)g["Duplizieren"];

            Katalogfilterzeile trocken = zeilen.Single(z => z.Bezeichner.Contains("Trockenkühler", StringComparison.Ordinal));
            KaeltemaschineDaten saat = lies(trocken.Id);
            Assert.True(saat.Auslieferung);
            Assert.Equal(6, saat.Kennlinie.Count);
            Assert.Equal(2, saat.RueckkuehlartIndex);

            saat.NennEer = 9;
            Assert.False(speichern(saat).Ok);                                         // ausgeliefert: nie überschrieben

            KaeltemaschineSpeicherErgebnis kopie = duplizieren(trocken.Id, "Kältemaschine Hüllenkopie");
            Assert.True(kopie.Ok, kopie.Meldung);
            KaeltemaschineDaten k = lies(kopie.Id);
            Assert.False(k.Auslieferung);
            Assert.Equal(6, k.Kennlinie.Count);

            k.RueckkuehlartIndex = 3;
            k.Hilfsstrom = null;
            k.Kennlinie.RemoveAt(0);
            k.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = 30, Kaltwassertemperatur = 9, Eer = 4.4, Kaelteleistung = 210 });
            KaeltemaschineSpeicherErgebnis e = speichern(k);
            Assert.True(e.Ok, e.Meldung);

            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(kopie.Id);
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER, m.Rueckkuehlart);
            Assert.Null(m.Hilfsstrom_Rueckkuehlung_kW);
            Assert.Equal(6, m.Kennlinie.Count);
            Assert.Contains(m.Kennlinie, p => p.Rueckkuehltemperatur == 30 && p.Kaltwassertemperatur == 9);

            Assert.False(loeschen(trocken.Id).Ok);
            Assert.True(loeschen(kopie.Id).Ok);
            Assert.Null(KaeltemaschineStammCtrl.Laden(kopie.Id));
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + KaeltemaschineSchema.TAB_KENNDATEN_STAMM + " WHERE " +
                KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", kopie.Id)), CultureInfo.InvariantCulture));
        }
    }
}
