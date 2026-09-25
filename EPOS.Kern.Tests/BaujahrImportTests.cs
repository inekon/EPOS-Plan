using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Baujahr als Importziel</b> (Stufe G4a, Welle 3; Umsetzungskonzept 3.4, Zeile „Baujahr
    /// (neue Spalte)"): ein Zielfeld der Gruppe Kenngrößen, vom IFC-Leser aus
    /// <c>Pset_BuildingCommon.YearOfConstruction</c> gefüllt (Herkunft IFC, Beleg mit dem gelesenen
    /// Text), beim gbXML-Weg leer; die Prüfung am OK lässt nur ganze Jahre 1500 … 2100 durch, und
    /// <c>GebaeudeImportHuelle.NachKatalogdaten</c> legt die Zahl auf den Feldsatz des Editors.
    /// Ohne Datenbank; die Kultur ist de-DE gepinnt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BaujahrImportTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Das Zielfeld steht in den Kenngrößen gleich hinter der Baualtersklasse, eingebbar, ohne Einheit.</summary>
        [Fact]
        public void Das_Zielfeld_Baujahr_steht_hinter_der_Baualtersklasse()
        {
            GebaeudeZielfeld f = GebaeudeZielfelder.Finde(GebaeudeZielfelder.BAUJAHR);
            Assert.NotNull(f);
            Assert.Equal(GebaeudeZielfelder.GRUPPE_KENNGROESSEN, f.Gruppe);
            Assert.Equal("", f.Einheit);
            Assert.False(f.IstText);
            Assert.True(f.Eingebbar);
            Assert.True(f.HakenSetzbar);
            Assert.Equal(GebaeudeZielfelder.Finde(GebaeudeZielfelder.BAUALTERSKLASSE).Reihenfolge + 1, f.Reihenfolge);
            Assert.Equal("Baujahr", GebaeudeZuordnungsModell.FeldText(GebaeudeZielfelder.BAUJAHR));
        }

        /// <summary>
        /// IFC: die gezogene Jahreszahl, Herkunft IFC, der Beleg nennt den gelesenen Text; mit Haken.
        /// Ohne Jahr in der Datei bleibt die Zeile leer und ohne Haken.
        /// </summary>
        [Fact]
        public void Der_IFC_Weg_fuellt_das_Baujahr_mit_Beleg()
        {
            IfcGebaeudeAbbild a = Synthetisch();
            a.Gebaeude[0].Baujahr = 1975;
            a.Gebaeude[0].BaujahrText = "erbaut 1975";

            GebaeudeFeldzeile z = GebaeudeAggregation.Bilden(a, 0, null, null, new IfcImportProfil())
                                                     .Zeile(GebaeudeZielfelder.BAUJAHR);
            Assert.Equal(1975.0, z.Wert);
            Assert.Equal(Importherkunft.Ifc, z.Herkunft);
            Assert.Equal("GIMP_BELEG_BAUJAHR", z.Beleg.Schluessel);
            Assert.Equal(new[] { "erbaut 1975" }, z.Beleg.Werte);
            Assert.True(z.Uebernehmen);
            Assert.Equal("gelesen aus „erbaut 1975“", GebaeudeZuordnungsModell.BelegText(z.Beleg));
            Assert.Equal("1975", GebaeudeZuordnungsModell.WertText(z));   // ohne Tausendertrennzeichen

            GebaeudeFeldzeile ohne = GebaeudeAggregation.Bilden(Synthetisch(), 0, 'E', null, new IfcImportProfil())
                                                        .Zeile(GebaeudeZielfelder.BAUJAHR);
            Assert.Null(ohne.Wert);
            Assert.Equal(Importherkunft.Leer, ohne.Herkunft);
            Assert.False(ohne.Uebernehmen);
        }

        /// <summary>gbXML trägt kein Baujahr: die Zeile bleibt leer, Herkunft Leer, ohne Haken.</summary>
        [Fact]
        public void Der_gbXML_Weg_laesst_das_Baujahr_leer()
        {
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(Klein.Wand("aw-1", 0,
                "<Width>10</Width><Height>2.5</Height>")));
            Assert.True(a.Gebaeude.Count == 1, string.Join(" | ", a.Meldungen));

            GebaeudeFeldzeile z = a.Zuordnen(0, 'E').Zeile(GebaeudeZielfelder.BAUJAHR);
            Assert.Null(z.Wert);
            Assert.Equal(Importherkunft.Leer, z.Herkunft);
            Assert.Null(z.Beleg);
            Assert.False(z.Uebernehmen);
        }

        /// <summary>Am OK: eine Handänderung mit Nachkommastellen oder außerhalb 1500 … 2100 sperrt die Übernahme.</summary>
        [Theory]
        [InlineData(1965.0, false)]
        [InlineData(1500.0, false)]
        [InlineData(2100.0, false)]
        [InlineData(1965.5, true)]
        [InlineData(1499.0, true)]
        [InlineData(2101.0, true)]
        public void Die_Pruefung_laesst_nur_ganze_Jahre_im_Bereich_durch(double jahr, bool gesperrt)
        {
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(Synthetisch(), 0, 'E', null, new IfcImportProfil());
            Assert.True(s.ManuellSetzen(GebaeudeZielfelder.BAUJAHR, jahr));

            IReadOnlyList<PruefMeldung> p = GebaeudeImportAblauf.Pruefen(s);
            PruefMeldung m = p.FirstOrDefault(x => x.Schluessel == GebaeudeImportAblauf.MELDUNG + "BAUJAHR_UNGUELTIG");
            Assert.Equal(gesperrt, m != null);
            if (gesperrt)
            {
                Assert.Equal(PruefStufe.Fehler, m.Stufe);
                Assert.Contains("1500 bis 2100", GebaeudeZuordnungsModell.MeldungText(m));
            }
        }

        /// <summary>
        /// <c>NachKatalogdaten</c>: eine Zeile mit Haken setzt das Baujahr, leer mit Haken setzt NULL,
        /// ohne Haken bleibt die Grundlage.
        /// </summary>
        [Fact]
        public void NachKatalogdaten_setzt_das_Baujahr_des_Editors()
        {
            var grundlage = new GebaeudeKatalogDaten { Name = "Alt", Baujahr = 1900 };

            Assert.Equal(1975, GebaeudeImportHuelle.NachKatalogdaten(grundlage, Ergebnis(1975.0, haken: true)).Baujahr);
            Assert.Null(GebaeudeImportHuelle.NachKatalogdaten(grundlage, Ergebnis(null, haken: true)).Baujahr);
            Assert.Equal(1900, GebaeudeImportHuelle.NachKatalogdaten(grundlage, Ergebnis(1975.0, haken: false)).Baujahr);
            Assert.Equal(1900, grundlage.Baujahr);   // die Grundlage bleibt unberührt
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static GebaeudeImportErgebnis Ergebnis(double? jahr, bool haken)
            => new GebaeudeImportErgebnis(0, null, "Neu", new Dictionary<string, bool>(),
                new List<GebaeudeFeldzeileDaten>
                {
                    new GebaeudeFeldzeileDaten { Zielfeld = GebaeudeZielfelder.BAUJAHR, Wert = jahr, Haken = haken }
                });

        /// <summary>Ein IFC-Abbild ohne Datei: ein beheizter Raum (100 m², 250 m³, 2,5 m) und eine Außenwand.</summary>
        private static IfcGebaeudeAbbild Synthetisch()
        {
            var a = new IfcGebaeudeAbbild();
            var g = new AbbildGebaeude { Kennung = "G1", Name = "Synthetisch", Quelltyp = "IfcBuilding" };
            g.Raeume.Add(new AbbildRaum { Kennung = "R1", Quelltyp = "IfcSpace", Name = "Raum", FlaecheM2 = 100, VolumenM3 = 250 });
            g.Bauteile.Add(new AbbildBauteil
            {
                Kennung = "W1", Name = "W1", Quelltyp = "IfcWall", Art = Bauteilart.Aussenwand,
                Randbedingung = Randbedingung.Aussenluft, BruttoflaecheM2 = 40, UWertWm2K = 0.3,
                NeigungGrad = 90, AzimutGrad = 180, HuelleOhneNachbar = true,
            });
            a.Gebaeude.Add(g);
            return a;
        }
    }
}
