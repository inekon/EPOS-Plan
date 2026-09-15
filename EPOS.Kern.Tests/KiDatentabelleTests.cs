using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die ERGEBNISTABELLE im Gespraechsverlauf (Anwenderbefund vom 14.09.2026:
    /// „KI-Assistent zeigt keine Datenwerte").
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was der Befund war.</b> Jede Leseaktion endete mit der Zeile
    /// „Ergebniszeilen: 12" - die Werte selbst standen nirgends. Wer sie sehen wollte,
    /// war darauf angewiesen, dass das MODELL sie in seinem Antwortsatz nacherzaehlte;
    /// und das Modell sieht sie nur platzgehalten und auf zwanzig Zeilen gekuerzt
    /// (<see cref="KiRueckmeldung"/>).
    /// </para>
    /// <para>
    /// <b>Was diese Faelle halten.</b> Dass die Zeilen als Tabelle im Verlauf stehen,
    /// mit Klarnamen, ausgerichtet und vollstaendig - und dass sie es auch dann tun,
    /// wenn eine Zeile ein Feld nicht fuehrt oder ein Wert einen Umbruch enthaelt.
    /// </para>
    /// </remarks>
    public class KiDatentabelleTests
    {
        private static IReadOnlyDictionary<string, object> Zeile(params object[] paare)
        {
            var z = new Dictionary<string, object>();
            for (int i = 0; i + 1 < paare.Length; i += 2)
                z[(string)paare[i]] = paare[i + 1];
            return z;
        }

        private static IReadOnlyList<IReadOnlyDictionary<string, object>> Kessel()
            => new[]
            {
                Zeile("name", "Vitocrossal 200", "leistung_kw", 80.0, "aktiv", true),
                Zeile("name", "Logano plus", "leistung_kw", 120.5, "aktiv", false)
            };

        private static IReadOnlyList<string> Texte(IReadOnlyList<KiVerlaufszeile> zeilen)
            => zeilen.Select(z => z.Text).ToList();

        // ===================================================== Der Grundfall

        [Fact]
        public void EineKopfzeileUndJeDatensatzEineZeile()
        {
            IReadOnlyList<KiVerlaufszeile> aus = KiVerlaufstexte.Datentabelle(Kessel());

            Assert.Equal(3, aus.Count);                       // Kopf + zwei Saetze
            Assert.All(aus, z => Assert.Equal(KiVerlaufsrolle.Datenzeile, z.Rolle));
        }

        [Fact]
        public void DieKopfzeileNenntDieFeldnamen()
        {
            string kopf = KiVerlaufstexte.Datentabelle(Kessel())[0].Text;

            Assert.Contains("name", kopf);
            Assert.Contains("leistung_kw", kopf);
            Assert.Contains("aktiv", kopf);
        }

        [Fact]
        public void DieWERTEStehenDa_UndZwarImKlartext()
        {
            // Der Kern des Befunds: nicht die Anzahl, sondern der Inhalt.
            IReadOnlyList<string> texte = Texte(KiVerlaufstexte.Datentabelle(Kessel()));

            Assert.Contains(texte, t => t.Contains("Vitocrossal 200"));
            Assert.Contains(texte, t => t.Contains("Logano plus"));
        }

        [Fact]
        public void ZahlenStehenInDerKulturDesAnwenders()
        {
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                IReadOnlyList<string> texte = Texte(KiVerlaufstexte.Datentabelle(Kessel()));

                // 120,5 - nicht 120.5. Invariant geschrieben wird nur, was an das
                // Modell geht.
                Assert.Contains(texte, t => t.Contains("120,5"));
            }
            finally { CultureInfo.CurrentCulture = vorher; }
        }

        [Fact]
        public void WahrheitswerteWerdenUebersetztUndNichtAlsTrueGezeigt()
        {
            IReadOnlyList<string> texte = Texte(KiVerlaufstexte.Datentabelle(Kessel()));

            Assert.DoesNotContain(texte, t => t.Contains("True"));
            Assert.Contains(texte, t => t.Contains(WindowsFormsApplication1.MyResource.Resource.KI_AKT_WERT_JA));
            Assert.Contains(texte, t => t.Contains(WindowsFormsApplication1.MyResource.Resource.KI_AKT_WERT_NEIN));
        }

        // ===================================================== Die Ausrichtung

        [Fact]
        public void DieSpaltenStehenUntereinander()
        {
            // Ohne feste Breiten waere es keine Tabelle. Geprueft wird an der Stelle,
            // an der die ZWEITE Spalte beginnt: Sie muss in jeder Zeile dieselbe sein.
            IReadOnlyList<string> texte = Texte(KiVerlaufstexte.Datentabelle(Kessel()));

            int kante = texte[0].IndexOf("leistung_kw", StringComparison.Ordinal);
            Assert.True(kante > 0);

            // An DIESER Stelle beginnt die zweite Spalte in jeder Zeile: davor ein
            // Trennzeichen, dahinter der Wert. Ueber den ersten Leerraum laesst sich das
            // nicht pruefen - „Vitocrossal 200" fuehrt selbst eines.
            foreach (string zeile in texte)
            {
                Assert.True(zeile.Length > kante, zeile);
                Assert.Equal(' ', zeile[kante - 1]);
                Assert.NotEqual(' ', zeile[kante]);
            }
        }

        [Fact]
        public void EinUmbruchImWertZerreisstDieTabelleNicht()
        {
            var mitUmbruch = new[]
            {
                Zeile("hinweis", "Erste Zeile\r\nZweite Zeile", "id", 7)
            };

            IReadOnlyList<KiVerlaufszeile> aus = KiVerlaufstexte.Datentabelle(mitUmbruch);

            Assert.Equal(2, aus.Count);
            Assert.All(aus, z => Assert.DoesNotContain("\n", z.Text));
            Assert.Contains("Erste Zeile Zweite Zeile", aus[1].Text);
        }

        // ===================================================== Die Raender

        [Fact]
        public void OhneZeilenGibtEsKeineTabelle()
        {
            Assert.Empty(KiVerlaufstexte.Datentabelle(null));
            Assert.Empty(KiVerlaufstexte.Datentabelle(
                Array.Empty<IReadOnlyDictionary<string, object>>()));
        }

        [Fact]
        public void EinFehlendesFeldBleibtLeerUndVerschiebtNichts()
        {
            var ungleich = new[]
            {
                Zeile("name", "Mit Leistung", "leistung_kw", 80.0),
                Zeile("name", "Ohne Leistung")
            };

            IReadOnlyList<KiVerlaufszeile> aus = KiVerlaufstexte.Datentabelle(ungleich);

            // Die Spalte stammt aus der Vereinigung ALLER Zeilen, nicht nur der ersten.
            Assert.Contains("leistung_kw", aus[0].Text);
            Assert.Equal(3, aus.Count);
            Assert.Contains("Ohne Leistung", aus[2].Text);
        }

        [Fact]
        public void EineNeueSpalteInEinerSpaeterenZeileGehtNichtVerloren()
        {
            var ungleich = new[]
            {
                Zeile("name", "Erster"),
                Zeile("name", "Zweiter", "zusatz", "kommt spaeter")
            };

            Assert.Contains("zusatz", KiVerlaufstexte.Datentabelle(ungleich)[0].Text);
        }

        [Fact]
        public void UeberDemDeckelWirdDerRestGezaehltUndNichtVerschwiegen()
        {
            var viele = new List<IReadOnlyDictionary<string, object>>();
            for (int i = 0; i < KiVerlaufstexte.MaxDatenzeilen + 5; i++)
                viele.Add(Zeile("nummer", i));

            IReadOnlyList<KiVerlaufszeile> aus = KiVerlaufstexte.Datentabelle(viele);

            // Kopf + Deckel + die Zeile „… und n weitere"
            Assert.Equal(KiVerlaufstexte.MaxDatenzeilen + 2, aus.Count);
            Assert.Equal(KiVerlaufsrolle.Leise, aus[aus.Count - 1].Rolle);
            Assert.Contains("5", aus[aus.Count - 1].Text);
        }

        [Fact]
        public void DerAnzeigedeckelIstGROESSERAlsDerUebertragungsdeckel()
        {
            // Zwei verschiedene Zwecke: KiRueckmeldung schuetzt die UEBERTRAGUNG an den
            // Modellanbieter, diese Grenze nur die Lesbarkeit des Fensters. Der Anwender
            // darf sehen, was sein eigenes Programm gelesen hat.
            Assert.True(KiVerlaufstexte.MaxDatenzeilen > KiRueckmeldung.MaxZeilen);
        }

        // ===================================================== Der Weg in den Verlauf

        [Fact]
        public void EinAusgefuehrterSchrittZeigtSeineZeilenUndNichtNurIhreZahl()
        {
            var antwort = new KiAntwort();
            antwort.Schritte.Add(new KiSchritt
            {
                Aktion = "heizkessel_auflisten",
                Kurzfassung = "Heizkessel auflisten",
                Ausgefuehrt = true,
                Ergebnis = KiErgebnis.Ok("Zwei Kessel gefunden.", Kessel())
            });

            IReadOnlyList<string> texte = Texte(KiVerlaufstexte.Schritte(antwort));

            // Die Zahl bleibt - sie ordnet ein. Aber die Werte stehen jetzt darunter.
            Assert.Contains(texte, t => t.Contains("2"));
            Assert.Contains(texte, t => t.Contains("Vitocrossal 200"));
            Assert.Contains(texte, t => t.Contains("Logano plus"));
        }
    }
}
