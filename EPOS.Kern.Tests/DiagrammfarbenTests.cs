using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// EINE Farbrolle setzen und zurücksetzen (Konzept Diagramme, Farbrollen
    /// Bedienung Teil 2): der Weg, den der Klick auf das Farbfeld eines
    /// Legendeneintrags nimmt.
    ///
    /// <para><b>Geprüft wird die TEXTEBENE</b> — <see cref="Diagrammfarben.MitRolle"/>,
    /// die reine Rechnung hinter <c>Setze</c> und <c>Zuruecksetzen</c>: dass nur
    /// ABWEICHUNGEN im Einstellungstext stehen, dass eine Farbe gleich der Hausfarbe
    /// den Eintrag ENTFERNT (sonst fröre eine später geänderte Hausfarbe bei jedem
    /// ein, der sie einmal ausdrücklich gewählt hat), dass ein nicht lesbarer Eintrag
    /// wegfällt und dass die Reihenfolge die der Rollenliste bleibt.</para>
    ///
    /// <para><b>Was hier bewusst NICHT läuft: das Speichern selbst.</b>
    /// <c>Setze</c> und <c>Zuruecksetzen</c> gehen über
    /// <see cref="EinstellungenCtrl"/> — denselben Weg wie der Einstellungsdialog,
    /// und der schreibt in <c>Properties.Settings</c> und legt dabei die fünf Ordner
    /// des Anwenders an. Ein Testfall, der das ausführte, veränderte die
    /// Benutzereinstellungen des Rechners und legte Verzeichnisse an; geprüft wird
    /// deshalb, dass eine unbekannte Rolle VOR dem Lesen abgewiesen wird — damit
    /// bleibt nichts unbeobachtet, was schreibt.</para>
    /// </summary>
    public class DiagrammfarbenTests
    {
        /// <summary>Die Hausfarbe einer Rolle als <c>#RRGGBB</c>.</summary>
        private static string Vorgabe(Farbrolle rolle)
            => Diagrammfarben.Hex(Farbpalette.Vorgabe[rolle]);

        /// <summary>Eine Farbe, die sicher NICHT die Hausfarbe dieser Rolle ist.</summary>
        private static Farbe Andere(Farbrolle rolle)
        {
            Farbe haus = Farbpalette.Vorgabe[rolle];
            return new Farbe((byte)(haus.R ^ 0xFF), haus.G, haus.B, haus.A);
        }

        // =====================================================================
        // 1 — Setzen und Entfernen im Text
        // =====================================================================

        /// <summary>Eine gesetzte Rolle steht danach als <c>ROLLE=#RRGGBB</c> im Text.</summary>
        [Fact]
        public void EineGesetzteRolleStehtImText()
        {
            string text = Diagrammfarben.MitRolle("", Farbrolle.WAERME_WP,
                                                  new Farbe(0xFF, 0x00, 0x00));

            Assert.Equal("WAERME_WP=#FF0000", text);
        }

        /// <summary>
        /// <b>Nur Abweichungen.</b> Wer die Hausfarbe wählt, bekommt keinen Eintrag —
        /// der Text bleibt leer, und leer heißt Hausfarben.
        /// </summary>
        [Fact]
        public void DieHausfarbeEntferntDenEintrag()
        {
            Farbe haus = Farbpalette.Vorgabe[Farbrolle.WAERME_WP];

            Assert.Equal("", Diagrammfarben.MitRolle("", Farbrolle.WAERME_WP, haus));
            Assert.Equal("", Diagrammfarben.MitRolle("WAERME_WP=#FF0000",
                                                     Farbrolle.WAERME_WP, haus));
        }

        /// <summary><c>null</c> als Farbe ist „Hausfarbe": Der Eintrag fällt.</summary>
        [Fact]
        public void OhneFarbeFaelltDerEintrag()
        {
            string text = Diagrammfarben.MitRolle("WAERME_WP=#FF0000;STROM_PV=#00A000",
                                                  Farbrolle.WAERME_WP, null);

            Assert.Equal("STROM_PV=#00A000", text);
        }

        /// <summary>
        /// Die Nachbarn bleiben unberührt, und die Reihenfolge ist die der
        /// Rollenliste — nicht die des Eingabetextes. Sonst hinge der gespeicherte
        /// Text von der Reihenfolge der Klicks ab.
        /// </summary>
        [Fact]
        public void DieNachbarnBleibenUndDieReihenfolgeIstDieDerListe()
        {
            // STROM_PV steht in der Rollenliste HINTER WAERME_WP.
            string text = Diagrammfarben.MitRolle("STROM_PV=#00A000",
                                                  Farbrolle.WAERME_WP,
                                                  new Farbe(0xFF, 0x00, 0x00));

            Assert.Equal("WAERME_WP=#FF0000;STROM_PV=#00A000", text);
        }

        /// <summary>
        /// Ein zweites Setzen derselben Rolle ERSETZT den Eintrag; er steht nie
        /// zweimal da.
        /// </summary>
        [Fact]
        public void EinZweitesSetzenErsetztDenEintrag()
        {
            string text = Diagrammfarben.MitRolle("WAERME_WP=#FF0000",
                                                  Farbrolle.WAERME_WP,
                                                  new Farbe(0x00, 0x00, 0xFF));

            Assert.Equal("WAERME_WP=#0000FF", text);
        }

        // =====================================================================
        // 2 — Ungültiges fällt weg, Unbekanntes ändert nichts
        // =====================================================================

        /// <summary>
        /// <b>Eine ungültige Farbe wird abgewiesen.</b> Sie ist beim Lesen schon
        /// benannt verworfen; ein Text, der sie mitschleppte, verwürfe sie beim
        /// nächsten Mal erneut — sie fällt deshalb hier heraus.
        /// </summary>
        [Fact]
        public void EinUnlesbarerEintragFaelltWeg()
        {
            string text = Diagrammfarben.MitRolle(
                "WAERME_WP=#XYZ;GIBTESNICHT=#FF0000;ohnegleich;STROM_PV=#00A000",
                Farbrolle.WAERME_KESSEL, new Farbe(0x11, 0x22, 0x33));

            Assert.Equal("WAERME_KESSEL=#112233;STROM_PV=#00A000", text);
        }

        /// <summary>
        /// Eine Rolle, die die Liste nicht führt (<c>UNBENANNT</c>, ein Tippfehler),
        /// lässt die Einträge, wie sie sind.
        /// </summary>
        [Fact]
        public void EineUnbekannteRolleAendertDieEintraegeNicht()
        {
            Assert.Equal("STROM_PV=#00A000",
                Diagrammfarben.MitRolle("STROM_PV=#00A000", Farbrolle.UNBENANNT,
                                        new Farbe(0xFF, 0x00, 0x00)));

            Assert.Equal("STROM_PV=#00A000",
                Diagrammfarben.MitRolle("STROM_PV=#00A000", null, null));
        }

        // =====================================================================
        // 3 — Die Abweisung vor dem Schreiben
        // =====================================================================

        /// <summary>
        /// <b>Ohne einstellbare Rolle wird gar nicht erst gelesen und geschrieben.</b>
        /// Es gibt nichts, worauf die Einstellung zeigen könnte — eine Reihe mit fest
        /// gerechneter Farbe bekommt im Bild deshalb auch keinen Farbwähler.
        /// </summary>
        [Fact]
        public void EineNichtEinstellbareRolleWirdAbgewiesen()
        {
            Assert.False(Diagrammfarben.Setze(null, new Farbe(0xFF, 0x00, 0x00)));
            Assert.False(Diagrammfarben.Setze(Farbrolle.UNBENANNT, new Farbe(0xFF, 0x00, 0x00)));
            Assert.False(Diagrammfarben.Zuruecksetzen(null));
            Assert.False(Diagrammfarben.Zuruecksetzen(Farbrolle.UNBENANNT));
        }

        // =====================================================================
        // 4 — Die Runde über den Text und zurück
        // =====================================================================

        /// <summary>
        /// Was <see cref="Diagrammfarben.MitRolle"/> schreibt, liest
        /// <see cref="Diagrammfarben.AusText"/> wieder — mit der DECKUNG der
        /// Hausfarbe: Der Anwender wählt den Farbton, die Durchsichtigkeit gehört zum
        /// Bildaufbau.
        /// </summary>
        [Fact]
        public void DerTextGehtAlsPaletteZurueck()
        {
            // KOSTENPROFIL ist eine der fuenf halbdurchsichtigen Rollen.
            Farbe gewaehlt = Andere(Farbrolle.KOSTENPROFIL);
            string text = Diagrammfarben.MitRolle("", Farbrolle.KOSTENPROFIL, gewaehlt);

            Farbpalette palette = Diagrammfarben.AusText(text);
            Farbe gelesen = palette[Farbrolle.KOSTENPROFIL];

            Assert.Equal(gewaehlt.R, gelesen.R);
            Assert.Equal(gewaehlt.G, gelesen.G);
            Assert.Equal(gewaehlt.B, gelesen.B);
            Assert.Equal(Farbpalette.Vorgabe[Farbrolle.KOSTENPROFIL].A, gelesen.A);

            // Jede andere Rolle bleibt die Hausfarbe.
            foreach (Farbrolle rolle in Diagrammfarben.Rollen)
                if (rolle != Farbrolle.KOSTENPROFIL)
                    Assert.Equal(Vorgabe(rolle), Diagrammfarben.Hex(palette[rolle]));
        }

        /// <summary>
        /// Und die Gegenrichtung: Zurücksetzen macht den Text wieder leer, also
        /// wieder Hausfarben für alle.
        /// </summary>
        [Fact]
        public void ZuruecksetzenMachtDenTextWiederLeer()
        {
            string gesetzt = Diagrammfarben.MitRolle("", Farbrolle.SPEICHER_3,
                                                     Andere(Farbrolle.SPEICHER_3));
            Assert.NotEqual("", gesetzt);

            string zurueck = Diagrammfarben.MitRolle(gesetzt, Farbrolle.SPEICHER_3, null);

            Assert.Equal("", zurueck);
            Farbpalette palette = Diagrammfarben.AusText(zurueck);
            foreach (Farbrolle rolle in Diagrammfarben.Rollen)
                Assert.Equal(Vorgabe(rolle), Diagrammfarben.Hex(palette[rolle]));
        }
    }
}
