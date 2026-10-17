using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>K-F5a</b> — die Pflege der Verwaltung „Rückkühlwerke" über <see cref="RueckkuehlwerkStammCtrl"/> an einer Testkopie:
    /// Anlegen, Ändern, Sperre des Auslieferungssatzes, Löschen nur ohne Verwendung, Wertemengen samt benannter Meldung,
    /// die Zeilen der Katalogliste und das Profil.
    /// </summary>
    [Collection("Testdatenbank")]
    public class RueckkuehlwerkStammCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;

        private static RueckkuehlwerkModel Satz(string name) => new RueckkuehlwerkModel
        {
            Bezeichner = name, Bauart = RueckkuehlwerkSchema.BAUART_HYBRID, Nennleistung_kW = 300, Annaeherung_Nenn_K = 6,
            Ventilator_Regelung = RueckkuehlwerkSchema.REGELUNG_DREHZAHL, Ventilator_Drehzahl_Min = 0.3,
            Befeuchtung_Ab_C = 24, Eindickung = 4
        };

        [Fact]
        public void Anlegen_und_Aendern_schreiben_jeden_Wert_und_halten_den_Namen_eindeutig()
        {
            if (!_db.Vorhanden) return;
            RueckkuehlwerkStammCtrl.SpeicherErgebnis e = RueckkuehlwerkStammCtrl.Speichern(Satz("Rückkühlwerk 300 kW hybrid"));
            Assert.True(e.Ok, e.Meldung);

            RueckkuehlwerkModel m = RueckkuehlwerkStammCtrl.Laden(e.Id);
            Assert.Equal(("HYBRID", 300.0, 6.0, "DREHZAHL", 0.3, 24.0, 4.0),
                         (m.Bauart, m.Nennleistung_kW.Value, m.Annaeherung_Nenn_K.Value, m.Ventilator_Regelung,
                          m.Ventilator_Drehzahl_Min.Value, m.Befeuchtung_Ab_C.Value, m.Eindickung.Value));

            m.Nennleistung_kW = 350;
            m.Annaeherung_Nenn_K = null;
            m.Freikuehlung_Schaltung = RueckkuehlwerkSchema.SCHALTUNG_REIHE;
            RueckkuehlwerkStammCtrl.SpeicherErgebnis g = RueckkuehlwerkStammCtrl.Speichern(m);
            Assert.True(g.Ok, g.Meldung);
            Assert.Equal(e.Id, g.Id);
            RueckkuehlwerkModel n = RueckkuehlwerkStammCtrl.Laden(e.Id);
            Assert.Equal((350.0, (double?)null, "REIHE"), (n.Nennleistung_kW.Value, n.Annaeherung_Nenn_K, n.Freikuehlung_Schaltung));

            RueckkuehlwerkStammCtrl.SpeicherErgebnis doppelt = RueckkuehlwerkStammCtrl.Speichern(Satz("Rückkühlwerk 300 kW hybrid"));
            Assert.False(doppelt.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, doppelt.Meldung);
        }

        [Fact]
        public void Ein_Auslieferungssatz_bleibt_bis_das_Schloss_faellt()
        {
            if (!_db.Vorhanden) return;
            int id = RueckkuehlwerkStammCtrl.Speichern(Satz("RK ausgeliefert")).Id;
            Assert.True(RueckkuehlwerkStammCtrl.SchlossSetzen(new[] { id }, true).Ok);
            Assert.True(RueckkuehlwerkStammCtrl.Laden(id).ReadOnly);
            Assert.True(RueckkuehlwerkStammCtrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);

            RueckkuehlwerkModel m = RueckkuehlwerkStammCtrl.Laden(id);
            m.Nennleistung_kW = 1;
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_AUSGELIEFERT, RueckkuehlwerkStammCtrl.Speichern(m).Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_AUSGELIEFERT, RueckkuehlwerkStammCtrl.Loeschen(id).Meldung);
            Assert.Equal(300.0, RueckkuehlwerkStammCtrl.Laden(id).Nennleistung_kW);

            Assert.True(RueckkuehlwerkStammCtrl.SchlossSetzen(new[] { id }, false).Ok);
            Assert.True(RueckkuehlwerkStammCtrl.Speichern(m).Ok);
            Assert.True(RueckkuehlwerkStammCtrl.Loeschen(id).Ok);
            Assert.Null(RueckkuehlwerkStammCtrl.Laden(id));
        }

        [Fact]
        public void Loeschen_lehnt_einen_verwendeten_Satz_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            int id = RueckkuehlwerkStammCtrl.Speichern(Satz("RK verwendet")).Id;
            int frei = RueckkuehlwerkStammCtrl.Speichern(Satz("RK frei")).Id;
            int kopie1 = RueckkuehlwerkCtrl.AusKatalogUebernehmen(id, PROJEKT);
            int kopie2 = RueckkuehlwerkCtrl.AusKatalogUebernehmen(id, PROJEKT);
            Assert.True(kopie1 > 0 && kopie2 > 0);

            Assert.Equal(2, RueckkuehlwerkStammCtrl.Verwendungen(id));
            Assert.Equal(0, RueckkuehlwerkStammCtrl.Verwendungen(frei));
            IReadOnlyDictionary<int, int> verwendung = RueckkuehlwerkStammCtrl.KatalogVerwendung();
            Assert.Equal(2, verwendung[id]);
            Assert.False(verwendung.ContainsKey(frei));

            RueckkuehlwerkStammCtrl.SpeicherErgebnis e = RueckkuehlwerkStammCtrl.Loeschen(id);
            Assert.False(e.Ok);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.RKW_MSG_VERWENDET, 2), e.Meldung);
            Assert.NotNull(RueckkuehlwerkStammCtrl.Laden(id));
            Assert.True(RueckkuehlwerkStammCtrl.Loeschen(frei).Ok);

            RueckkuehlwerkCtrl.Loeschen(kopie1);
            RueckkuehlwerkCtrl.Loeschen(kopie2);
            Assert.True(RueckkuehlwerkStammCtrl.Loeschen(id).Ok);
        }

        [Fact]
        public void Wertemengen_und_Bereiche_melden_Feld_und_Grenze()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_BAUART_UNGUELTIG,
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Bauart = "VERDUNSTER" }));
            Assert.Equal("Weg der Annäherung: Die Auswahl „STUFE“ ist unbekannt.",
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Annaeherung_Weg = "STUFE" }));
            Assert.Equal("Ventilatorregelung: Die Auswahl „FREQUENZ“ ist unbekannt.",
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Ventilator_Regelung = "FREQUENZ" }));
            Assert.Equal("Annäherung im Nennpunkt: Der Wert liegt außerhalb seines Bereichs (0 bis 30).",
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Annaeherung_Nenn_K = -1 }));
            Assert.Equal("Eindickung: Der Wert liegt außerhalb seines Bereichs (über 1 bis 20).",
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Eindickung = 25 }));
            Assert.Equal("Befeuchtung ab Außentemperatur: Der Wert liegt außerhalb seines Bereichs (-30 bis 50).",
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Befeuchtung_Ab_C = 60 }));
            Assert.Equal("Nennleistung (abzuführende Wärme): Der Wert liegt außerhalb seines Bereichs (größer 0).",
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Nennleistung_kW = 0 }));
            Assert.Null(RueckkuehlwerkStammCtrl.Pruefen(Satz("RK")));

            // Jeder Steuerwert der Wertemengen hat einen eigenen Anzeigetext; leer zeigt die Vorgabe.
            Assert.Equal(RueckkuehlwerkSchema.BAUARTEN.Count,
                         RueckkuehlwerkSchema.BAUARTEN.Select(RueckkuehlwerkStammCtrl.BauartText).Distinct().Count());
            Assert.Equal(RueckkuehlwerkStammCtrl.BauartText(RueckkuehlwerkSchema.BAUART_TROCKEN), RueckkuehlwerkStammCtrl.BauartText(null));
            Assert.Equal(RueckkuehlwerkStammCtrl.WegText(RueckkuehlwerkSchema.WEG_FEST), RueckkuehlwerkStammCtrl.WegText(null));
            Assert.Equal(RueckkuehlwerkStammCtrl.RegelungText(RueckkuehlwerkSchema.REGELUNG_EIN_AUS), RueckkuehlwerkStammCtrl.RegelungText(null));
            Assert.Equal(RueckkuehlwerkStammCtrl.SchaltungText(RueckkuehlwerkSchema.SCHALTUNG_PARALLEL), RueckkuehlwerkStammCtrl.SchaltungText(null));
            foreach (string w in RueckkuehlwerkSchema.ANNAEHERUNG_WEGE.Concat(RueckkuehlwerkSchema.VENTILATOR_REGELUNGEN)
                                                    .Concat(RueckkuehlwerkSchema.FREIKUEHLUNG_SCHALTUNGEN))
            {
                string text = RueckkuehlwerkStammCtrl.WegText(w) != w ? RueckkuehlwerkStammCtrl.WegText(w)
                            : RueckkuehlwerkStammCtrl.RegelungText(w) != w ? RueckkuehlwerkStammCtrl.RegelungText(w)
                            : RueckkuehlwerkStammCtrl.SchaltungText(w);
                Assert.NotEqual(w, text);
            }
        }

        [Fact]
        public void Die_Katalogliste_fuehrt_die_sechs_Spalten_des_Profils()
        {
            Katalogfilterprofil p = Katalogfilterprofil.FuerRueckkuehlwerk();
            Assert.Equal(Katalogfilterprofil.SCHLUESSEL_RUECKKUEHLWERK, p.Schluessel);
            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpBezeichner, Katalogfilterprofil.SpBauart, Katalogfilterprofil.SpNennleistung,
                Katalogfilterprofil.SpAnnaeherung, Katalogfilterprofil.SpVentilator, Katalogfilterprofil.SpFreikuehlung
            }, p.Spalten.Select(s => s.Schluessel).ToArray());

            if (!_db.Vorhanden) return;
            int id = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "RK Liste", Nennleistung_kW = 120, Ventilator_Regelung = RueckkuehlwerkSchema.REGELUNG_STUFEN,
                Ventilator_Nenn_kW = 2.5
            }).Id;
            Katalogfilterzeile z = RueckkuehlwerkStammCtrl.Katalogfilterzeilen().Single(x => x.Id == id);
            Assert.Equal(id.ToString(CultureInfo.InvariantCulture), z.Schluessel);
            Assert.False(z.Geschuetzt);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_BAUART_TROCKEN, z.Text(Katalogfilterprofil.SpBauart));
            Assert.Equal("Stufen · 2,5 kW", z.Text(Katalogfilterprofil.SpVentilator));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_SCHALTUNG_PARALLEL, z.Text(Katalogfilterprofil.SpFreikuehlung));
        }
    }
}
