using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Erdreichprüfung im gespeicherten Ergebnis</b> (Entscheidungsvorlage Modellgrenzen EQ1):
    /// <c>Tab_ErgebnisErdreich</c> wird mit dem Ergebnis geschrieben, gelesen, ersetzt und gelöscht, und
    /// der Dialog bekommt daraus dasselbe Laufergebnis samt Laufstempel.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErdreichErgebnisSpeicherTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static ErdreichAuswertung.AnlageErgebnis Beispiel(int idAnlage, bool belastbar)
        {
            var a = new ErdreichAuswertung.AnlageErgebnis
            {
                ID_Anlage = idAnlage,
                Modul = "WP 1",
                JahresentzugKWh = 12345.5,
                VolllastStunden = 1800,
                BetriebsStunden = 2000,
                MaxEntzugW = 6858.6,
                MaxEntzugBelastbar = belastbar,
                FrostStunden = 150,
                FrostWarnung = true,
                Grenze = belastbar ? "" : "nicht trennbar",
            };
            a.Pruefung = new VDI4640Pruefung.Ergebnis
            {
                Moeglich = belastbar,
                Grundlage = "Klimazone 6, Bodenart Sand",
                Zeilen = new List<VDI4640Pruefung.Pruefzeile>
                {
                    new VDI4640Pruefung.Pruefzeile { Bezeichnung = "spez. Entzug", IstText = "27,4 W/m²", Istwert = 27.4,
                                                     Grenzwert = 25, Einheit = "W/m²", Ueberschritten = true },
                },
            };
            return a;
        }

        /// <summary>Die Prüfzeilen ohne Datenbank: vier Kennzeilen und die Zeilen der Auslegungsprüfung.</summary>
        [Fact]
        public void Zeilen_tragen_Kennwerte_Pruefzeilen_und_Laufstempel()
        {
            var lauf = new DateTime(2026, 10, 3, 14, 5, 9);
            List<ErdreichErgebnisSpeicher.Zeile> z = ErdreichErgebnisSpeicher.Zeilen(new[] { Beispiel(7, true) }, lauf);
            Assert.Equal(new[] { "ENTZUGSLEISTUNG", "JAHRESENTZUG", "VOLLLASTSTUNDEN", "FROST", "VDI4640:spez. Entzug" },
                         z.Select(x => x.Pruefzeile).ToArray());
            Assert.All(z, x => Assert.Equal("2026-10-03 14:05:09", x.Laufstempel));
            Assert.Equal(6858.6, z[0].Istwert);
            Assert.Equal(100.0, z[3].Grenzwert.Value, 6);
            Assert.Equal(25.0, z[4].Grenzwert);
            Assert.Empty(ErdreichErgebnisSpeicher.Zeilen(null, lauf));
        }

        /// <summary>
        /// Schreiben im Vorgang des Ergebnisses, Lesen, Wiederaufbau des Laufergebnisses mit Laufstempel,
        /// Ersetzen beim nächsten Lauf und Löschen mit dem Ergebnis.
        /// </summary>
        [Fact]
        public void Tab_ErgebnisErdreich_wird_geschrieben_gelesen_ersetzt_und_geloescht()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ErdreichErgebnisSpeicher.Vorhanden());

            var anlage = DataRepository.GetDataTable("SELECT ID, ID_Projekt FROM Tab_Energieanlagen ORDER BY ID LIMIT 1").Rows[0];
            int idAnlage = Convert.ToInt32(anlage["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(anlage["ID_Projekt"], CultureInfo.InvariantCulture);
            var lauf = new DateTime(2026, 10, 3, 9, 30, 0);

            ErdreichAuswertung.AnlageErgebnis a = Beispiel(idAnlage, true);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                ErdreichErgebnisSpeicher.Schreiben(v, idProjekt, ErdreichErgebnisSpeicher.Zeilen(new[] { a }, lauf), true);
                v.Commit();
            }
            Assert.Equal(5, ErdreichErgebnisSpeicher.Lesen(idProjekt).Count);

            ErdreichAuswertung.ErdreichLaufErgebnis frisch = ErdreichAuswertung.ErgebnisZuordnen(a);
            ErdreichAuswertung.ErdreichLaufErgebnis gespeichert = ErdreichErgebnisSpeicher.Gespeichert(idProjekt, idAnlage);
            Assert.True(gespeichert.Vorhanden);
            Assert.True(gespeichert.ErgebnisseVorhanden);
            Assert.Equal(frisch.MaxEntzugW, gespeichert.MaxEntzugW);
            Assert.Equal(frisch.JahresentzugKWh, gespeichert.JahresentzugKWh);
            Assert.Equal(frisch.VolllastStunden, gespeichert.VolllastStunden);
            Assert.Equal(frisch.HinweisFrost, gespeichert.HinweisFrost);
            Assert.Equal("2026-10-03 09:30:00", gespeichert.Laufstempel);
            Assert.Equal("", frisch.Laufstempel);
            Assert.Equal(gespeichert.MaxEntzugW, ErdreichErgebnisSpeicher.Gespeichert(idProjekt, 0).MaxEntzugW);
            Assert.False(ErdreichErgebnisSpeicher.Gespeichert(idProjekt, idAnlage + 999999).Vorhanden);

            // Der nächste Lauf ersetzt: nicht belastbar - der Hinweis steht anstelle der Prüfung.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                ErdreichErgebnisSpeicher.Schreiben(v, idProjekt, ErdreichErgebnisSpeicher.Zeilen(new[] { Beispiel(idAnlage, false) }, lauf), true);
                v.Commit();
            }
            Assert.Equal(4, ErdreichErgebnisSpeicher.Lesen(idProjekt).Count);
            ErdreichAuswertung.ErdreichLaufErgebnis ohne = ErdreichErgebnisSpeicher.Gespeichert(idProjekt, idAnlage);
            Assert.False(ohne.ErgebnisseVorhanden);
            Assert.NotEqual("", ohne.HinweisErgebnis);

            // Löschen der Ergebnisse des Projekts nimmt die Prüfung mit.
            Assert.Equal(0, new ErgebnisCtrl().Delete(idProjekt));
            Assert.Empty(ErdreichErgebnisSpeicher.Lesen(idProjekt));
            Assert.False(ErdreichErgebnisSpeicher.Gespeichert(idProjekt, idAnlage).Vorhanden);
        }

        /// <summary>Save schreibt die Prüfung des Modells mit; ohne Erdreichquelle bleibt das Projekt leer.</summary>
        [Fact]
        public void ErgebnisCtrl_Save_schreibt_die_Pruefung_mit()
        {
            if (!_db.Vorhanden) return;

            var anlage = DataRepository.GetDataTable("SELECT ID, ID_Projekt FROM Tab_Energieanlagen ORDER BY ID LIMIT 1").Rows[0];
            int idAnlage = Convert.ToInt32(anlage["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(anlage["ID_Projekt"], CultureInfo.InvariantCulture);

            var m = new ErgebnisModel { ID_Projekt = idProjekt, Bezeichner = "Probe EQ1" };
            m.Erdreich.AddRange(ErdreichErgebnisSpeicher.Zeilen(new[] { Beispiel(idAnlage, true) }, m.Zeitstempel));
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            Assert.Equal(5, ErdreichErgebnisSpeicher.Lesen(idProjekt).Count);

            var leer = new ErgebnisModel { ID_Projekt = idProjekt, Bezeichner = "Probe EQ1 ohne Erdreich" };
            Assert.True(new ErgebnisCtrl().Save(leer) > 0);
            Assert.Empty(ErdreichErgebnisSpeicher.Lesen(idProjekt));
        }
    }
}
