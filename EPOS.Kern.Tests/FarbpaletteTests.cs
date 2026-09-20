using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Diagrammfarben als Anwendungseinstellung (Auftrag DF-1, Anwenderentscheid
    /// 20.09.2026 „Die Farben der Diagramme sollen jeweils änderbar sein").
    ///
    /// <para>Geprüft wird, was die Einstellung tragfähig macht: die VORGABE ist Wert
    /// für Wert die Hausfarbe, der Einstellungstext wird gelesen und geschrieben,
    /// jeder ungültige Eintrag wird BENANNT verworfen statt still übergangen, der
    /// Rückfall je Rolle ist die Hausfarbe, <c>Zuruecksetzen</c> stellt sie wieder
    /// her — und der Maler nimmt wirklich die eingestellte Palette, sonst hätte der
    /// Dialog keine Wirkung.</para>
    ///
    /// <para>Die Klasse steht in der Sammlung „Testdatenbank": Sie tauscht
    /// <see cref="Dienste.Einstellungen"/> und setzt <c>Farbpalette.Aktuell</c> —
    /// beides prozessweiter Zustand, und xunit trennt nur INNERHALB einer
    /// Sammlung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class FarbpaletteTests
    {
        /// <summary>
        /// Eine flüchtige Einstellungsablage mit EINEM Wert — und die Zusicherung,
        /// dass Dienst und Palette danach wieder stehen, wie sie standen.
        /// </summary>
        private sealed class Einstellung : IDisposable
        {
            private readonly IEinstellungen _vorher = Dienste.Einstellungen;

            public Einstellung(string text)
            {
                var ablage = new FluechtigeEinstellungen();
                if (text != null) ablage.Schreib(Diagrammfarben.SCHLUESSEL, text);
                Dienste.Einstellungen = ablage;
            }

            public void Dispose()
            {
                Dienste.Einstellungen = _vorher;
                Farbpalette.Zuruecksetzen();
            }
        }

        // =====================================================================
        // 1 — Die Vorgabe ist die Hausfarbe, Wert für Wert
        // =====================================================================

        /// <summary>
        /// Jede der vierzig Rollen hat in der Vorgabe eine Farbe — eine Rolle ohne
        /// Palettenfarbe wäre im Bild schwarz, und im Einstellungsdialog stünde ein
        /// leeres Muster.
        /// </summary>
        [Fact]
        public void JedeAenderbareRolleHatEineHausfarbe()
        {
            Assert.Equal(40, Diagrammfarben.Rollen.Count);
            foreach (Farbrolle rolle in Diagrammfarben.Rollen)
                Assert.True(Farbpalette.Vorgabe.Kennt(rolle), "ohne Hausfarbe: " + rolle.Name);
        }

        /// <summary>
        /// Die Hausfarben stehen Wert für Wert so da, wie sie der Bestand malte — das
        /// ist die Messlatte der ChartProben. Eine Auswahl über alle sechs Gruppen,
        /// samt den zwei halbdurchsichtigen Rollen.
        /// </summary>
        [Fact]
        public void DieVorgabeTraegtDieHausfarbenWertFuerWert()
        {
            Assert.Equal(new Farbe(0xFF, 0xFF, 0xFF), Farbpalette.Vorgabe[Farbrolle.HINTERGRUND]);
            Assert.Equal(new Farbe(0x69, 0x69, 0x69), Farbpalette.Vorgabe[Farbrolle.ACHSE]);
            Assert.Equal(new Farbe(0x41, 0x72, 0xC4), Farbpalette.Vorgabe[Farbrolle.WAERME_WP]);
            Assert.Equal(new Farbe(0xED, 0x7D, 0x31), Farbpalette.Vorgabe[Farbrolle.WAERME_BHKW]);
            Assert.Equal(new Farbe(0x70, 0xAD, 0x47), Farbpalette.Vorgabe[Farbrolle.STROM_PV]);
            Assert.Equal(new Farbe(0xC7, 0x15, 0x85), Farbpalette.Vorgabe[Farbrolle.SPEICHER_1]);
            Assert.Equal(new Farbe(0x00, 0x64, 0x00, 180), Farbpalette.Vorgabe[Farbrolle.KOSTENPROFIL]);
            Assert.Equal(new Farbe(0x46, 0x82, 0xB4, 90), Farbpalette.Vorgabe[Farbrolle.AUSSENTEMPERATUR]);
            Assert.Equal(new Farbe(0xE0, 0x8A, 0x00), Farbpalette.Vorgabe[Farbrolle.FEINRASTER]);
        }

        /// <summary>Die sechs Gruppen ergeben zusammen genau die Rollenliste, ohne Doppelung.</summary>
        [Fact]
        public void DieSechsGruppenTragenJedeRolleGenauEinmal()
        {
            Assert.Equal(6, Diagrammfarben.Gruppen.Count);

            var ausGruppen = Diagrammfarben.Gruppen.SelectMany(g => g.Rollen).ToList();
            Assert.Equal(Diagrammfarben.Rollen.Count, ausGruppen.Count);
            Assert.Equal(ausGruppen.Count, ausGruppen.Distinct().Count());

            // UNBENANNT ist der Rueckfall fuer eine Farbe OHNE Rolle - sie hat keine
            // Palettenfarbe und darf deshalb nicht in der Einstellungsliste stehen.
            Assert.DoesNotContain(Farbrolle.UNBENANNT, ausGruppen);
        }

        // =====================================================================
        // 2 — Das Textformat
        // =====================================================================

        [Fact]
        public void HexSchreibtUndLiestDieselbeFarbe()
        {
            Assert.Equal("#4172C4", Diagrammfarben.Hex(new Farbe(0x41, 0x72, 0xC4)));

            // Die DECKUNG steht nicht im Text - sie gehoert zum Bildaufbau.
            Assert.Equal("#006400", Diagrammfarben.Hex(new Farbe(0x00, 0x64, 0x00, 180)));

            Farbe farbe;
            Assert.True(Diagrammfarben.Lies(" #ff0000 ", 255, out farbe));
            Assert.Equal(new Farbe(0xFF, 0x00, 0x00), farbe);
        }

        [Theory]
        [InlineData("#F00")]          // Kurzform
        [InlineData("#4172C4FF")]     // acht Stellen
        [InlineData("4172C4")]        // ohne Raute
        [InlineData("#41 2C4")]       // Luecke
        [InlineData("rot")]
        [InlineData("")]
        [InlineData(null)]
        public void NurDieSchreibweiseMitSechsStellenGiltAlsFarbe(string text)
        {
            Assert.False(Diagrammfarben.IstHex(text));
        }

        /// <summary>Ein Einstellungstext wird Rolle für Rolle gelesen.</summary>
        [Fact]
        public void DerEinstellungstextWirdGelesen()
        {
            IReadOnlyList<string> verworfen;
            IReadOnlyDictionary<Farbrolle, Farbe> farben =
                Diagrammfarben.Abweichungen("WAERME_WP=#FF0000;STROM_PV=#00a000", out verworfen);

            Assert.Empty(verworfen);
            Assert.Equal(2, farben.Count);
            Assert.Equal(new Farbe(0xFF, 0x00, 0x00), farben[Farbrolle.WAERME_WP]);
            Assert.Equal(new Farbe(0x00, 0xA0, 0x00), farben[Farbrolle.STROM_PV]);
        }

        /// <summary>
        /// <b>Die Deckung bleibt die der Hausfarbe.</b> Der Anwender wählt den Farbton;
        /// die Durchsichtigkeit gehört zum Bildaufbau — die Kostenprofil-Fläche würde
        /// sonst deckend und verdeckte, was unter ihr liegt.
        /// </summary>
        [Fact]
        public void DieDeckungDerHausfarbeBleibt()
        {
            Farbpalette p = Diagrammfarben.AusText("KOSTENPROFIL=#FF0000");
            Assert.Equal(new Farbe(0xFF, 0x00, 0x00, 180), p[Farbrolle.KOSTENPROFIL]);
        }

        /// <summary>Nicht genannte Rollen bleiben die Hausfarbe — der Rückfall je Rolle.</summary>
        [Fact]
        public void NichtGenannteRollenBleibenHausfarbe()
        {
            Farbpalette p = Diagrammfarben.AusText("WAERME_WP=#FF0000");

            Assert.Equal(new Farbe(0xFF, 0x00, 0x00), p[Farbrolle.WAERME_WP]);
            foreach (Farbrolle rolle in Diagrammfarben.Rollen)
                if (rolle != Farbrolle.WAERME_WP)
                    Assert.Equal(Farbpalette.Vorgabe[rolle], p[rolle]);
        }

        /// <summary>Ein leerer Text ist die Vorgabe — und kein Sonderfall im Aufrufer.</summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void LeerHeisstHausfarben(string text)
        {
            Farbpalette p = Diagrammfarben.AusText(text);
            foreach (Farbrolle rolle in Diagrammfarben.Rollen)
                Assert.Equal(Farbpalette.Vorgabe[rolle], p[rolle]);
        }

        /// <summary>
        /// <b>Ungültiges wird BENANNT verworfen, nie still.</b> Vier Fälle in einem
        /// Text: unbekannte Rolle, fehlendes Gleichheitszeichen, kein gültiges Hex,
        /// doppelte Rolle. Der gültige Eintrag steht danach trotzdem, und jeder
        /// verworfene Eintrag ist im Wortlaut nachlesbar.
        /// </summary>
        [Fact]
        public void UngueltigesWirdBenanntVerworfen()
        {
            IReadOnlyList<string> verworfen;
            IReadOnlyDictionary<Farbrolle, Farbe> farben = Diagrammfarben.Abweichungen(
                "WAERME_WP=#FF0000;GIBTESNICHT=#112233;STROM_PV;BEDARF=rot;WAERME_WP=#00FF00",
                out verworfen);

            Assert.Single(farben);
            Assert.Equal(new Farbe(0xFF, 0x00, 0x00), farben[Farbrolle.WAERME_WP]);

            Assert.Equal(new[] { "GIBTESNICHT=#112233", "STROM_PV", "BEDARF=rot",
                                 "WAERME_WP=#00FF00" },
                         verworfen);
        }

        /// <summary>
        /// Geschrieben werden NUR die abweichenden Rollen, in der Reihenfolge der
        /// Rollenliste. Stimmt alles mit der Hausfarbe überein, kommt der leere Text
        /// heraus — so erreicht eine später geänderte Hausfarbe jeden, der sie nicht
        /// selbst gesetzt hat.
        /// </summary>
        [Fact]
        public void GeschriebenWerdenNurDieAbweichungen()
        {
            IReadOnlyList<Farbrollengabe> rollen = Diagrammfarben.Gaben();
            var gewaehlt = rollen.ToDictionary(g => g.Schluessel, g => g.Vorgabe);

            Assert.Equal("", Diagrammfarben.AlsText(rollen, gewaehlt));

            gewaehlt["STROM_PV"] = "#00A000";
            gewaehlt["WAERME_WP"] = "#ff0000";          // Kleinbuchstaben gehen durch
            gewaehlt["WAERME_BHKW"] = "keine Farbe";    // ungueltig: faellt weg

            // Die Reihenfolge ist die der Rollenliste: WAERME_WP steht vor STROM_PV.
            Assert.Equal("WAERME_WP=#FF0000;STROM_PV=#00A000",
                         Diagrammfarben.AlsText(rollen, gewaehlt));
        }

        /// <summary>Schreiben und Lesen ergeben denselben Stand.</summary>
        [Fact]
        public void SchreibenUndLesenSindDieselbeAussage()
        {
            IReadOnlyList<Farbrollengabe> rollen = Diagrammfarben.Gaben();
            var gewaehlt = rollen.ToDictionary(g => g.Schluessel, g => g.Vorgabe);
            gewaehlt["SPEICHER_3"] = "#123456";

            string text = Diagrammfarben.AlsText(rollen, gewaehlt);
            IReadOnlyDictionary<string, string> zurueck = Diagrammfarben.Werte(text);

            Assert.Single(zurueck);
            Assert.Equal("#123456", zurueck["SPEICHER_3"]);
        }

        // =====================================================================
        // 3 — Lesen aus der Anwendungseinstellung, Zurücksetzen
        // =====================================================================

        [Fact]
        public void DiePaletteKommtAusDerAnwendungseinstellung()
        {
            using (new Einstellung("WAERME_WP=#FF0000"))
            {
                Diagrammfarben.Uebernehmen();

                Assert.Equal(new Farbe(0xFF, 0x00, 0x00), Farbpalette.Aktuell[Farbrolle.WAERME_WP]);
                Assert.Equal(Farbpalette.Vorgabe[Farbrolle.STROM_PV],
                             Farbpalette.Aktuell[Farbrolle.STROM_PV]);
                Assert.Empty(Diagrammfarben.Verworfen);
            }
        }

        /// <summary>
        /// Auch beim Übernehmen aus der Einstellung wird Ungültiges benannt verworfen —
        /// die Palette steht danach trotzdem und trägt die lesbaren Werte.
        /// </summary>
        [Fact]
        public void UngueltigesAusDerEinstellungStehtInVerworfen()
        {
            using (new Einstellung("WAERME_WP=#FF0000;UNSINN"))
            {
                Diagrammfarben.Uebernehmen();

                Assert.Equal(new Farbe(0xFF, 0x00, 0x00), Farbpalette.Aktuell[Farbrolle.WAERME_WP]);
                Assert.Equal(new[] { "UNSINN" }, Diagrammfarben.Verworfen);
            }
        }

        [Fact]
        public void ZuruecksetzenStelltDieHausfarbenWiederHer()
        {
            using (new Einstellung("WAERME_WP=#FF0000"))
            {
                Diagrammfarben.Uebernehmen();
                Assert.NotEqual(Farbpalette.Vorgabe[Farbrolle.WAERME_WP],
                                Farbpalette.Aktuell[Farbrolle.WAERME_WP]);

                Farbpalette.Zuruecksetzen();
                Assert.Same(Farbpalette.Vorgabe, Farbpalette.Aktuell);
            }
        }

        /// <summary>Ohne Eintrag bleibt es bei den Hausfarben — der Fall jeder frischen Installation.</summary>
        [Fact]
        public void OhneEintragBleibenDieHausfarben()
        {
            using (new Einstellung(null))
            {
                Diagrammfarben.Uebernehmen();
                foreach (Farbrolle rolle in Diagrammfarben.Rollen)
                    Assert.Equal(Farbpalette.Vorgabe[rolle], Farbpalette.Aktuell[rolle]);
            }
        }

        // =====================================================================
        // 4 — Der Maler nimmt die eingestellte Palette
        // =====================================================================

        /// <summary>
        /// <b>Die eigentliche Aussage des Auftrags:</b> Ein Bild, das über
        /// <c>Farbpalette.Aktuell</c> gemalt wird, ändert sich, wenn die Einstellung
        /// eine Rolle tauscht — und ist mit leerer Einstellung byte-gleich zum Bild
        /// der Vorgabe. Ohne diesen Fall wäre der ganze Einstellungsweg eine
        /// Behauptung.
        /// </summary>
        [Fact]
        public void DerMalerNimmtDieEingestelltePalette()
        {
            var m = new Zeichenmodell(40, 20, new Farbton(Farbrolle.HINTERGRUND));
            m.Rechteck(0f, 0f, 40f, 20f,
                       fuellung: new Fuellung(new Farbton(Farbrolle.WAERME_WP)));

            using (new Einstellung(""))
            {
                Diagrammfarben.Uebernehmen();
                Assert.Equal(SkiaMaler.Png(m, Farbpalette.Vorgabe), SkiaMaler.Png(m));
            }

            using (new Einstellung("WAERME_WP=#FF0000"))
            {
                Diagrammfarben.Uebernehmen();
                Assert.NotEqual(SkiaMaler.Png(m, Farbpalette.Vorgabe), SkiaMaler.Png(m));
            }
        }

        // =====================================================================
        // 5 — Die Gaben für den Einstellungsdialog
        // =====================================================================

        /// <summary>
        /// Jede Zeile der Einstellungsliste trägt Schlüssel, Anzeigename, Gruppe und
        /// die Hausfarbe als <c>#RRGGBB</c> — die Komponente führt Text und kennt
        /// weder Rolle noch Farbe.
        /// </summary>
        [Fact]
        public void JedeGabeTraegtSchluesselNamenGruppeUndVorgabe()
        {
            IReadOnlyList<Farbrollengabe> gaben = Diagrammfarben.Gaben();

            Assert.Equal(Diagrammfarben.Rollen.Count, gaben.Count);
            foreach (Farbrollengabe g in gaben)
            {
                Assert.False(string.IsNullOrWhiteSpace(g.Schluessel));
                Assert.False(string.IsNullOrWhiteSpace(g.Name));
                Assert.False(string.IsNullOrWhiteSpace(g.Gruppe));
                Assert.True(Diagrammfarben.IstHex(g.Vorgabe), g.Schluessel + ": " + g.Vorgabe);
            }

            // Der Anzeigename ist nicht der Schluessel: Er kommt aus der Ressource.
            Farbrollengabe wp = gaben.First(x => x.Schluessel == "WAERME_WP");
            Assert.NotEqual("WAERME_WP", wp.Name);
            Assert.Equal("#4172C4", wp.Vorgabe);
        }

        /// <summary>Die Rückrichtung: der sprachneutrale Name findet seine Rolle.</summary>
        [Fact]
        public void DerSprachneutraleNameFindetSeineRolle()
        {
            Assert.Equal(Farbrolle.WAERME_WP, Diagrammfarben.Rolle("WAERME_WP"));
            Assert.Equal(Farbrolle.WAERME_WP, Diagrammfarben.Rolle(" waerme_wp "));
            Assert.Null(Diagrammfarben.Rolle("GIBTESNICHT"));
            Assert.Null(Diagrammfarben.Rolle(null));

            // UNBENANNT ist keine einstellbare Rolle.
            Assert.Null(Diagrammfarben.Rolle("UNBENANNT"));
        }
    }
}
