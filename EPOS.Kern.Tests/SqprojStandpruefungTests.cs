using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Standprüfung IFC gegen Projektdatei und Wahl der Aufbauquelle</b> (Anwenderentscheid vom 08.10.2026): Das Standpaar
    /// (<see cref="SqprojProbenErzeuger.Standpaar"/> / <see cref="SqprojProbenErzeuger.StandIfc"/>) trägt dieselben GUIDs, aber
    /// andere Aufbauten, eine Kopie im Journal nach dem Modellstand, ein anderes Baujahr und Dicken ungleich der Schichtsumme.
    /// </summary>
    public class SqprojStandpruefungTests
    {
        private static readonly DateTime MODELLSTAND = Standpruefung.Zeitpunkt(SqprojProbenErzeuger.STAND_MODELLSTAND).Value;

        private static SqprojAbbild Projektdatei(bool mitKopie = true)
            => SqprojAufbauProbe.Lesen(SqprojProbenErzeuger.Standpaar(mitKopie), "stand_" + Guid.NewGuid().ToString("N") + ".sqproj");

        /// <summary>Ein Ablauf beim Weg „IFC + Projektdatei“: das IFC-Abbild samt STEP-Puffer, dann die Projektdatei dazugeladen.</summary>
        internal static GebaeudeImportAblauf Ablauf(bool stimmig = false)
        {
            var ablauf = new GebaeudeImportAblauf();
            Assert.Equal(1, ablauf.AbbildVorgeben(SqprojProbenErzeuger.StandIfc(stimmig), new IfcImportProfil(), "stand.ifc",
                                                  SqprojProbenErzeuger.StandIfcText()));
            Dazuladen(ablauf);
            return ablauf;
        }

        internal static SqprojStand Dazuladen(GebaeudeImportAblauf ablauf)
        {
            string pfad = SqprojProbenErzeuger.Standpaar().Schreiben(SqprojProbenErzeuger.TempPfad("stand_" + Guid.NewGuid().ToString("N")));
            try
            {
                using (FileStream s = File.OpenRead(pfad))
                    return ablauf.ProjektdateiLesen(s, "stand.sqproj", 0);
            }
            finally { File.Delete(pfad); }
        }

        private static GebaeudeBauteilvorschlag Vorschlag(GebaeudeImportAblauf a, SqprojStand stand = null)
            => GebaeudeBauteilvorschlag.Bilden(a.Abbild, 0, 'F', a.Quelle, a.Profil, projektdatei: stand ?? a.Projektdatei);

        private static GebaeudeBauteilzeile Zeile(GebaeudeBauteilvorschlag v, string kennung) => v.Zeilen.Single(z => z.Kennung == kennung);

        [Fact]
        public void Die_Pruefung_schlaegt_beim_Standpaar_an_mit_allen_drei_Anzeichen()
        {
            Standpruefung p = Standpruefung.Pruefen(Projektdatei(), SqprojProbenErzeuger.StandIfc().Gebaeude[0], MODELLSTAND);
            Assert.True(p.Angeschlagen);
            Assert.Equal(4, p.Verglichen);
            Assert.Equal(4, p.Abweichend);
            Assert.Equal(140.0, p.HuellflaecheM2, 9);
            Assert.Equal(1.0, p.Anteil, 9);
            Assert.Equal(new[] { Standanzeichen.KopieNachModellstand, Standanzeichen.BaujahrAbweichend, Standanzeichen.DickePasstNicht }, p.Anzeichen);
            Assert.Equal(new DateTime(2026, 6, 23, 14, 45, 44), p.Kopiezeitpunkt);
            Assert.Equal((1995, 1970), (p.BaujahrIfc.Value, p.BaujahrProjektdatei.Value));
            Assert.Equal((3, 2), (p.DickeGeprueft, p.DickeAbweichend));

            StandpruefungArt wand = p.JeArt.Single(a => a.Art == Bauteilart.Aussenwand);
            Assert.Equal((2, 2), (wand.Verglichen, wand.Abweichend));
            Assert.Equal(0.148, wand.MedianUIfc, 9);
            Assert.Equal(SqprojProbenErzeuger.U_AW_BESTAND, wand.MedianUProjektdatei, 9);
            Assert.Equal(4, p.Beispiele.Count);
            Assert.Equal(("Dach", "Dach saniert", "Dach Bestand"), (p.Beispiele[0].Bauteil, p.Beispiele[0].AufbauIfc, p.Beispiele[0].AufbauProjektdatei));
            Assert.Equal(SqprojProbenErzeuger.U_FENSTER_BESTAND, p.Beispiele.Single(b => b.Art == Bauteilart.Fenster).UProjektdatei, 9);

            PruefMeldung m = p.Meldung();
            Assert.Equal((PruefStufe.Warnung, SqprojProtokoll.STAND_ABWEICHEND), (m.Stufe, m.Schluessel));
            Assert.Equal(3, p.Anzeichentext().Split("; ").Length);
        }

        [Fact]
        public void Ein_Anzeichen_gilt_nur_belegt()
        {
            // Ohne Kopie im Journal und ohne Modellstand fällt (a) weg; die Abweichung bleibt.
            Standpruefung p = Standpruefung.Pruefen(Projektdatei(mitKopie: false), SqprojProbenErzeuger.StandIfc().Gebaeude[0], null);
            Assert.True(p.Angeschlagen);
            Assert.DoesNotContain(Standanzeichen.KopieNachModellstand, p.Anzeichen);
            Assert.Null(p.Kopiezeitpunkt);
            // Eine Kopie VOR dem Modellstand ist kein Anzeichen.
            Standpruefung vorher = Standpruefung.Pruefen(Projektdatei(), SqprojProbenErzeuger.StandIfc().Gebaeude[0], MODELLSTAND.AddHours(1));
            Assert.DoesNotContain(Standanzeichen.KopieNachModellstand, vorher.Anzeichen);
        }

        [Fact]
        public void Ein_stimmiges_Paar_loest_keine_Pruefung_aus()
        {
            Standpruefung p = Standpruefung.Pruefen(Projektdatei(), SqprojProbenErzeuger.StandIfc(stimmig: true).Gebaeude[0], MODELLSTAND);
            Assert.False(p.Angeschlagen);
            Assert.Equal((4, 0), (p.Verglichen, p.Abweichend));
            Assert.Null(p.Meldung());
            Assert.DoesNotContain(Standanzeichen.BaujahrAbweichend, p.Anzeichen);

            GebaeudeImportAblauf a = Ablauf(stimmig: true);
            Assert.Equal(Aufbauquelle.Ifc, a.Projektdatei.Aufbauquelle);
            Assert.Null(a.Aufbauquellenpruefung());
            Assert.DoesNotContain(a.Projektdatei.Meldungen, x => x.Schluessel == SqprojProtokoll.STAND_ABWEICHEND);
        }

        [Fact]
        public void Der_Modellstand_ist_der_StampEdit_des_Gebaeudes_und_die_Kopie_der_juengste_COPY_Eintrag()
        {
            Assert.Equal(MODELLSTAND, Standpruefung.IfcModellstand(SqprojProbenErzeuger.StandIfcText(), "0000000000000000STAND0"));
            Assert.Equal(new DateTime(2026, 6, 23, 14, 37, 59), MODELLSTAND);
            Assert.Null(Standpruefung.IfcModellstand(Array.Empty<byte>(), "x"));
            Assert.Equal(new DateTime(2026, 6, 24, 9, 0, 0), Standpruefung.Kopie(new[]
            {
                "23.06.2026 14:45:44 >DBL:COPY {}", "24.06.2026 09:00:00 >DBL:COPY {}", "25.06.2026 10:00:00 >DBL:LOAD {}", null,
            }));
            Assert.Null(Standpruefung.Kopie(new[] { "24.06.2026 13:08:45 >DBL:EXPORT" }));
        }

        [Fact]
        public void Offen_sperrt_Uebernehmen_mit_benanntem_Grund()
        {
            GebaeudeImportAblauf a = Ablauf();
            SqprojStand stand = a.Projektdatei;
            Assert.True(stand.Standpruefung.Angeschlagen);
            Assert.Equal(Aufbauquelle.Offen, stand.Aufbauquelle);
            Assert.Contains(stand.Meldungen, m => m.Schluessel == SqprojProtokoll.STAND_ABWEICHEND && m.Stufe == PruefStufe.Warnung);
            Assert.Contains(Standanzeichen.KopieNachModellstand, stand.Standpruefung.Anzeichen);

            PruefMeldung sperre = a.Aufbauquellenpruefung();
            Assert.Equal((PruefStufe.Fehler, SqprojProtokoll.AUFBAUQUELLE_OFFEN), (sperre.Stufe, sperre.Schluessel));
            GebaeudeBauteilvorschlag v = Vorschlag(a);
            Assert.True(v.Abgelehnt);
            Assert.Contains(SqprojProtokoll.AUFBAUQUELLE_OFFEN, BauteilvorschlagProbe.Schluessel(v, PruefStufe.Fehler));
        }

        [Fact]
        public void Wahl_Projektdatei_liefert_U_und_Aufbau_der_Projektdatei()
        {
            GebaeudeImportAblauf a = Ablauf();
            Assert.True(a.AufbauquelleWaehlen(Aufbauquelle.Projektdatei));
            Assert.Null(a.Aufbauquellenpruefung());
            GebaeudeBauteilvorschlag v = Vorschlag(a);
            Assert.DoesNotContain(SqprojProtokoll.AUFBAUQUELLE_OFFEN, BauteilvorschlagProbe.Schluessel(v, PruefStufe.Fehler));

            GebaeudeBauteilzeile wand = Zeile(v, "Wand 1");
            Assert.Equal(SqprojProbenErzeuger.U_AW_BESTAND, wand.Bauteil.U_Wert.Value, 9);
            Assert.Equal(Importherkunft.Sqproj, wand.HerkunftU);
            Assert.Equal(Aufbaurang.Projektdatei, wand.Aufbaurang);
            GebaeudeAufbauzeile aw = BauteilvorschlagProbe.AufbauZeile(v, wand);
            Assert.True(aw.AusProjektdatei);
            Assert.StartsWith("AW Bestand", aw.Aufbau.Bezeichner, StringComparison.Ordinal);
            Assert.Equal(SqprojProbenErzeuger.U_DACH_BESTAND, Zeile(v, "Dach").Bauteil.U_Wert.Value, 9);
            Assert.Equal(SqprojProbenErzeuger.U_FENSTER_BESTAND, Zeile(v, "Fenster").Bauteil.U_Wert.Value, 9);
            Assert.Contains(GebaeudeBauteilvorschlag.PD_QUELLE_PROJEKTDATEI, BauteilvorschlagProbe.Schluessel(v, PruefStufe.Info));
            Assert.DoesNotContain(GebaeudeBauteilvorschlag.PD_U_ABWEICHUNG, BauteilvorschlagProbe.Schluessel(v, PruefStufe.Warnung));

            // Der Satz der Zuordnung trägt das U der Projektdatei.
            GebaeudeImportSatz satz = a.Zuordnen(0, 'F');
            Assert.Equal(SqprojProbenErzeuger.U_AW_BESTAND, satz.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert.Value, 6);
        }

        [Fact]
        public void Wahl_Ifc_liefert_den_heutigen_Stand()
        {
            GebaeudeImportAblauf a = Ablauf();
            Assert.True(a.AufbauquelleWaehlen(Aufbauquelle.Ifc));
            GebaeudeBauteilvorschlag v = Vorschlag(a);
            // Der heutige Stand: derselbe Vorschlag mit einem Stand ohne Standprüfung (E98).
            SqprojStand p = a.Projektdatei;
            GebaeudeBauteilvorschlag heute = Vorschlag(a, new SqprojStand(p.Dateiname, p.Hash, p.Bytes, p.Abbild, p.Abgleich, p.Meldungen, null));
            Assert.Equal(heute.Zeilen.Select(z => (z.Kennung, z.Bauteil.U_Wert, z.Aufbaurang, z.HerkunftU)),
                         v.Zeilen.Select(z => (z.Kennung, z.Bauteil.U_Wert, z.Aufbaurang, z.HerkunftU)));
            Assert.Equal(0.148, Zeile(v, "Wand 1").Bauteil.U_Wert.Value, 9);
            Assert.NotEqual(Aufbaurang.Projektdatei, Zeile(v, "Wand 1").Aufbaurang);
            Assert.Contains(GebaeudeBauteilvorschlag.PD_QUELLE_IFC, BauteilvorschlagProbe.Schluessel(v, PruefStufe.Info));
            Assert.Equal(0.148, a.Zuordnen(0, 'F').Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert.Value, 6);
        }

        [Fact]
        public void Neulesen_setzt_die_Wahl_wieder_auf_offen()
        {
            GebaeudeImportAblauf a = Ablauf();
            a.AufbauquelleWaehlen(Aufbauquelle.Projektdatei);
            Assert.Equal(Aufbauquelle.Projektdatei, a.Projektdatei.Aufbauquelle);
            // Datei erneut lesen: IFC und Projektdatei neu — die Wahl ist nicht gespeichert.
            a.AbbildVorgeben(SqprojProbenErzeuger.StandIfc(), new IfcImportProfil(), "stand.ifc", SqprojProbenErzeuger.StandIfcText());
            Assert.Null(a.Projektdatei);
            Dazuladen(a);
            Assert.Equal(Aufbauquelle.Offen, a.Projektdatei.Aufbauquelle);
            Assert.NotNull(a.Aufbauquellenpruefung());
        }

        [Fact]
        public void Der_Weg_nur_Projektdatei_prueft_nicht()
        {
            string pfad = SqprojProbenErzeuger.Standpaar().Schreiben(SqprojProbenErzeuger.TempPfad("nur_" + Guid.NewGuid().ToString("N")));
            var a = new GebaeudeImportAblauf();
            try
            {
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, "stand.sqproj", new SqprojImportProfil());
            }
            finally { File.Delete(pfad); }
            Assert.NotNull(a.Projektdatei);
            Assert.Null(a.Projektdatei.Standpruefung);
            Assert.Equal(Aufbauquelle.Ifc, a.Projektdatei.Aufbauquelle);
            Assert.Null(a.Aufbauquellenpruefung());
            Assert.False(a.AufbauquelleWaehlen(Aufbauquelle.Projektdatei));
            Assert.DoesNotContain(a.Projektdatei.Meldungen, m => m.Schluessel == SqprojProtokoll.STAND_ABWEICHEND);
        }

        [Fact]
        public void Die_Huelle_reicht_Pruefung_Wahl_und_Methode_durch()
        {
            var h = new GebaeudeImportHuelle();
            h.Ablauf.AbbildVorgeben(SqprojProbenErzeuger.StandIfc(), new IfcImportProfil(), "stand.ifc", SqprojProbenErzeuger.StandIfcText());
            string pfad = SqprojProbenErzeuger.Standpaar().Schreiben(SqprojProbenErzeuger.TempPfad("huelle_" + Guid.NewGuid().ToString("N")));
            EPOS.UI.Dialoge.Import.GebaeudeProjektdateiDaten d;
            try
            {
                using (FileStream s = File.OpenRead(pfad))
                    d = h.ProjektdateiLesen(s, "stand.sqproj", 0);
            }
            finally { File.Delete(pfad); }
            Assert.True(d.AufbauquelleOffen);
            Assert.True(d.Standpruefung.Angeschlagen);
            Assert.Equal(100.0, d.Standpruefung.AnteilProzent, 9);
            Assert.Equal(new[] { "kopie", "baujahr", "dicke" }, d.Standpruefung.Anzeichen.Select(x => x.Schluessel));
            Assert.Equal(4, d.Standpruefung.Beispiele.Count);
            Assert.NotEmpty(d.Standpruefung.Meldung);

            EPOS.UI.Dialoge.Import.GebaeudeProjektdateiDaten nach = h.AufbauquelleWaehlen(EPOS.UI.Dialoge.Import.GebaeudeAufbauquelleSchluessel.PROJEKTDATEI);
            Assert.Equal(EPOS.UI.Dialoge.Import.GebaeudeAufbauquelleSchluessel.PROJEKTDATEI, nach.Aufbauquelle);
            Assert.False(nach.AufbauquelleOffen);
            Assert.Equal(EPOS.UI.Dialoge.Import.GebaeudeAufbauquelleSchluessel.PROJEKTDATEI,
                         h.AufbauquelleWaehlen("unbekannt").Aufbauquelle);
        }
    }
}
