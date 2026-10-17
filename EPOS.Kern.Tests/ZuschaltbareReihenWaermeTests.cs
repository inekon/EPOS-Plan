using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle RZ, Teil b — zuschaltbare Reihen in drei Wärmebildern</b>, gemessen an den Hüllen:
    /// die Außentemperatur im Raumtemperaturbild des Gebäudes und im Jahresbild des Quellprofils,
    /// die Auswahl je Speicher im Bild der Speichertemperaturen. Je Bild: der Schalter an und aus,
    /// die Spalten der „CSV…“ am Bild (<see cref="ZeitreihenCsv.AusModell"/>) folgen der Wahl, und
    /// ohne Wahl ist das Bild das Bestandsbild (PNG byte-gleich).
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZuschaltbareReihenWaermeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyList<string> MIT_AUSSEN = new[] { Zusatzreihen.Aussentemperatur };

        private static string[] Namen(Zeichenmodell m) => m.Reihen.Select(r => r.Name).ToArray();

        private static string[] Spalten(Zeichenmodell m) => ZeitreihenCsv.AusModell(m).Select(s => s.Name).ToArray();

        // =================================================================
        //  Kandidat 4: Gebäude-Raumtemperatur
        // =================================================================

        [Fact]
        public void Raumtemperatur_Aussentemperatur_zuschaltbar_und_ab_Werk_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1007;
            int idZ = Z_ProjGebCtrl.LiesProjekt(projekt)[0].ID_Z;
            GebaeudeProjektZeile zeile = GebaeudeHuelle.AusModell(
                Z_ProjGebCtrl.LiesProjekt(projekt).First(m => m.ID_Z == idZ));

            SimulationProtokoll.NeuStarten();
            IReadOnlyDictionary<string, object> gaben = GebaeudeBedarfHuelle.Gaben(zeile, projekt, out string befund);
            Assert.NotNull(gaben);
            Assert.True(string.IsNullOrEmpty(befund), befund);

            var bestand = (Func<Zeichenmodell>)gaben["BildauftragRaumtemperatur"];
            var mitReihen = (Func<int, IReadOnlyList<string>, Zeichenmodell>)gaben["BildauftragRaumtemperaturReihen"];
            Assert.NotNull(bestand);
            Assert.NotNull(mitReihen);
            Assert.Equal(R.CHART_SERIE_AUSSENTEMPERATUR, (string)gaben["LabelAussentemperatur"]);

            Zeichenmodell alt = bestand();
            Zeichenmodell aus = mitReihen(-1, Array.Empty<string>());
            Zeichenmodell an = mitReihen(-1, MIT_AUSSEN);

            // Aus: Bild für Bild das Bestandsbild.
            Assert.Equal(SkiaMaler.Png(alt), SkiaMaler.Png(aus));
            Assert.DoesNotContain(R.CHART_SERIE_AUSSENTEMPERATUR, Namen(alt));

            // An: dieselben Reihen und dahinter die Außentemperatur - Bild und CSV gleich.
            Assert.Equal(Namen(alt).Append(R.CHART_SERIE_AUSSENTEMPERATUR).ToArray(), Namen(an));
            Assert.Equal(Namen(an), Spalten(an));
            Assert.Equal(Namen(alt), Spalten(alt));
            Assert.Equal(8760, an.Reihen.Last().Werte.Length);
        }

        // =================================================================
        //  Kandidat 5b: Quellprofil
        // =================================================================

        [Fact]
        public async Task Quellprofil_Aussentemperatur_zuschaltbar_und_ab_Werk_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var aussen = new double[8760];
            for (int i = 0; i < aussen.Length; i++) aussen[i] = 10.0 - 12.0 * Math.Cos(2 * Math.PI * i / 8760.0);
            var daten = new QuellprofilDaten { WPName = "WP 1", IdProjekt = 0 };
            var monate = new double[] { 2, 3, 5, 8, 11, 13, 14, 13, 11, 8, 5, 3 };

            IReadOnlyDictionary<string, object> mit = QuellprofilHuelle.Gaben(daten, aussen);
            var bestand = (Func<string, double[], Task<Zeichenmodell>>)mit["Jahresbild"];
            var mitReihen = (Func<string, double[], IReadOnlyList<string>, Task<Zeichenmodell>>)mit["JahresbildReihen"];
            Assert.NotNull(mitReihen);

            Zeichenmodell alt = await bestand(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT, monate);
            Zeichenmodell aus = await mitReihen(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT, monate, Array.Empty<string>());
            Zeichenmodell an = await mitReihen(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT, monate, MIT_AUSSEN);

            Assert.Equal(SkiaMaler.Png(alt), SkiaMaler.Png(aus));
            Assert.Equal(1, alt.Reihen.Count);
            Assert.Equal(new[] { R.CHART_SERIE_QUELLTEMPERATUR, R.CHART_SERIE_AUSSENTEMPERATUR }, Namen(an));
            Assert.Equal(Namen(an), Spalten(an));
            Assert.Equal(aussen, an.Reihen[1].Werte);

            // Ohne Klimareihe kein Delegat - und damit kein Schalter im Dialog.
            IReadOnlyDictionary<string, object> ohne = QuellprofilHuelle.Gaben(daten);
            Assert.Null(ohne["JahresbildReihen"]);
        }

        // =================================================================
        //  Kandidat 5a: Speichertemperaturen
        // =================================================================

        [Fact]
        public async Task Speichertemperaturen_Auswahl_je_Speicher()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1045;

            SimulationErgebnisHuelle huelle = SimulationErgebnisHuelle.Erzeugen(null, projekt, new BedarfsZustand());
            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Rueckmeldung lauf = await dienste.Laufen!((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            SimulationErgebnisDaten d = dienste.Laden!(projekt);
            Assert.True(d.Speichertemperaturen);
            Assert.NotEmpty(d.Speicherwahl);

            Zeichenmodell alle = dienste.Modell!(new Bildauftrag(Bilder.Speichertemperaturen));
            var volleListe = d.Speicherwahl.Select(s => s.Schluessel).ToList();
            Zeichenmodell vollGewaehlt = dienste.Modell!(new Bildauftrag(Bilder.Speichertemperaturen, false, -1, volleListe));
            Zeichenmodell keiner = dienste.Modell!(new Bildauftrag(Bilder.Speichertemperaturen, false, -1,
                                                                      Array.Empty<string>()));

            // Alle gewählt = ohne Wahl: das Bestandsbild.
            Assert.Equal(SkiaMaler.Png(alle), SkiaMaler.Png(vollGewaehlt));

            // Je Speicher zwei Reihen (oben, unten); ohne Speicher bleiben nur die Quelltemperaturen.
            Assert.Equal(alle.Reihen.Count - 2 * d.Speicherwahl.Count, keiner.Reihen.Count);
            foreach (var sp in d.Speicherwahl)
                Assert.DoesNotContain(Namen(keiner), n => n.StartsWith(sp.Name + " ", StringComparison.Ordinal));

            // Nur der erste Speicher: genau seine zwei Reihen, in Bild und CSV.
            var erster = d.Speicherwahl[0];
            Zeichenmodell einer = dienste.Modell!(new Bildauftrag(Bilder.Speichertemperaturen, false, -1,
                                                                     new[] { erster.Schluessel }));
            Assert.Equal(keiner.Reihen.Count + 2, einer.Reihen.Count);
            Assert.Equal(2, Namen(einer).Count(n => n.StartsWith(erster.Name + " ", StringComparison.Ordinal)));
            Assert.Equal(Namen(einer), Spalten(einer));
            Assert.Equal(Namen(alle), Spalten(alle));
            // Die Farbe eines Speichers bleibt seine, auch wenn andere abgewählt sind.
            Assert.Equal(alle.Reihen.First(r => r.Name == einer.Reihen[0].Name).Ton, einer.Reihen[0].Ton);
        }
    }
}
