using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Parameterpruefung (Fachkonzept 3.2, Verwendung b): Pflichtfelder, Wertebereiche,
    /// falsche Typen, unbekannte Namen.
    /// </summary>
    public class KiPruefungTests
    {
        private static readonly KiAktion Vielerlei = Beispielregister.MitAllenTypen();
        private static readonly KiAktion MitId = Beispielregister.MitId();

        // ----------------------------------------------------------- Unbekannte Aktion

        [Fact]
        public void UnbekannteAktion_WirdAbgewiesenUndNenntDieBekannten()
        {
            KiRegister register = Beispielregister.Erzeuge();

            KiPruefErgebnis p = KiPruefung.Pruefe(register, "datenbank_leeren", null);

            Assert.False(p.Gueltig);
            Assert.Null(p.Aufruf);
            Assert.Contains("datenbank_leeren", p.FehlerText());
            Assert.Contains("projekte_auflisten", p.FehlerText());
        }

        [Fact]
        public void UnbekannteAktion_AuchUeberDenJsonWeg()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Beispielregister.Erzeuge(),
                                                      "sql_ausfuehren", "{\"sql\":\"DROP TABLE\"}");
            Assert.False(p.Gueltig);
            Assert.Contains("sql_ausfuehren", p.FehlerText());
        }

        // ---------------------------------------------------------------- Pflichtfelder

        [Fact]
        public void FehlendesPflichtfeld_WirdGemeldet()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(MitId, null);

            Assert.False(p.Gueltig);
            Assert.Single(p.Fehler);
            Assert.Contains("Projekt (ID)", p.Fehler[0]);
            Assert.Contains("projekt_id", p.Fehler[0]);
        }

        [Fact]
        public void NullWert_ZaehltAlsFehlend()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(MitId, Beispielregister.Werte("projekt_id", null));

            Assert.False(p.Gueltig);
            Assert.Contains("Projekt (ID)", p.FehlerText());
        }

        [Fact]
        public void FehlendesOptionalesFeld_IstKeinFehler()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei, Beispielregister.Werte("projekt_id", 1007));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.False(p.Aufruf!.Hat("schwelle_kw"));
            Assert.Equal(1007, p.Aufruf.Id("projekt_id"));
        }

        [Fact]
        public void AktionOhneParameter_IstMitLeerenWertenGueltig()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Beispielregister.OhneParameter(), null);

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Empty(p.Aufruf!.Werte);
        }

        // ------------------------------------------------------------ Unbekannter Name

        [Fact]
        public void UnbekannterParameter_WirdAbgewiesenStattVerworfen()
        {
            // Ein erfundener Parametername des Modells muss eine Korrekturrunde ausloesen,
            // nicht stillschweigend verschwinden.
            KiPruefErgebnis p = KiPruefung.Pruefe(MitId,
                Beispielregister.Werte("projekt_id", 1007, "tabelle", "Tab_Projekt"));

            Assert.False(p.Gueltig);
            Assert.Contains("tabelle", p.FehlerText());
            Assert.Contains("projekt_id", p.FehlerText());
        }

        // ------------------------------------------------------------------ Falsche Typen

        [Theory]
        [InlineData("keine Zahl")]
        [InlineData(true)]
        [InlineData(3.5)]
        public void GanzzahlfeldMitFalschemWert_WirdAbgewiesen(object wert)
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(MitId, Beispielregister.Werte("projekt_id", wert));

            Assert.False(p.Gueltig);
            Assert.Contains("Projekt (ID)", p.FehlerText());
        }

        [Fact]
        public void GanzzahlAlsGleitkommaOhneNachkommastellen_WirdAngenommen()
        {
            // JSON kennt nur "number": 1007 kommt haeufig als 1007.0 herein.
            KiPruefErgebnis p = KiPruefung.Pruefe(MitId, Beispielregister.Werte("projekt_id", 1007.0));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(1007, p.Aufruf!.Id("projekt_id"));
        }

        [Fact]
        public void TextfeldMitZahl_WirdAbgewiesen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "bezeichner", 42));

            Assert.False(p.Gueltig);
            Assert.Contains("Bezeichner", p.FehlerText());
        }

        [Fact]
        public void LeererText_WirdAbgewiesen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "bezeichner", "   "));

            Assert.False(p.Gueltig);
            Assert.Contains("Bezeichner", p.FehlerText());
        }

        [Fact]
        public void ZuLangerText_WirdAbgewiesen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "bezeichner", new string('x', 11)));

            Assert.False(p.Gueltig);
            Assert.Contains("10", p.FehlerText());
        }

        [Fact]
        public void TextWirdGetrimmt()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "bezeichner", "  Nord  "));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal("Nord", p.Aufruf!.Text("bezeichner"));
        }

        [Theory]
        [InlineData("ja")]
        [InlineData(1)]
        public void WahrheitswertMitFalschemWert_WirdAbgewiesen(object wert)
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "speichern", wert));

            Assert.False(p.Gueltig);
            Assert.Contains("Speichern", p.FehlerText());
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("False", false)]
        public void WahrheitswertAlsText_WirdInvariantGelesen(string text, bool erwartet)
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "speichern", text));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(erwartet, p.Aufruf!.Wahrheit("speichern"));
        }

        // ------------------------------------------------------------------ Wertebereich

        [Fact]
        public void UntergrenzeVerletzt_WirdGemeldet()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(MitId, Beispielregister.Werte("projekt_id", 0));

            Assert.False(p.Gueltig);
            Assert.Contains("Projekt (ID)", p.FehlerText());
            Assert.Contains("1", p.FehlerText());
        }

        [Fact]
        public void ObergrenzeVerletzt_WirdGemeldet()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "schwelle_kw", 100001.0));

            Assert.False(p.Gueltig);
            Assert.Contains("Zielschwelle", p.FehlerText());
            Assert.Contains("100000", p.FehlerText());
        }

        [Fact]
        public void GrenzwerteSelbst_SindZulaessig()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "schwelle_kw", 0.0));

            Assert.True(p.Gueltig, p.FehlerText());
        }

        // ------------------------------------------------------------------- Aufzaehlung

        [Fact]
        public void UnbekannterAufzaehlungswert_WirdAbgewiesenUndNenntDieErlaubten()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "gewerk", "Kernkraftwerk"));

            Assert.False(p.Gueltig);
            Assert.Contains(Beispielregister.Gewerk1, p.FehlerText());
            Assert.Contains(Beispielregister.Gewerk2, p.FehlerText());
        }

        [Fact]
        public void AufzaehlungswertKommtInDerSchreibweiseDerDeklarationZurueck()
        {
            // Der GESPEICHERTE Wert stammt aus DbWerte und ist eingefroren - eine andere
            // Schreibweise des Modells darf nicht in die Datenbank durchschlagen.
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "gewerk", "bhkw"));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(Beispielregister.Gewerk2, p.Aufruf!.Text("gewerk"));
        }

        // ------------------------------------------------------------------------ Listen

        [Fact]
        public void Zahlenliste_WirdUebernommen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "projekt_ids", new object[] { 1007, 1009.0, "1011" }));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(new[] { 1007, 1009, 1011 }, p.Aufruf!.IdListe("projekt_ids"));
        }

        [Fact]
        public void EinzelwertStattListe_WirdAlsEinelementigeListeAngenommen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "projekt_ids", 1007));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(new[] { 1007 }, p.Aufruf!.IdListe("projekt_ids"));
        }

        [Fact]
        public void LeereListe_WirdAbgewiesen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "projekt_ids", new object[0]));

            Assert.False(p.Gueltig);
            Assert.Contains("Projekte", p.FehlerText());
        }

        [Fact]
        public void ListeMitUnbrauchbaremGlied_WirdAbgewiesen()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "projekt_ids", new object[] { 1007, "Nord" }));

            Assert.False(p.Gueltig);
            Assert.Contains("Projekte", p.FehlerText());
        }

        [Fact]
        public void ListenGrenzeGiltFuerJedesGlied()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("projekt_id", 1, "projekt_ids", new object[] { 1007, 0 }));

            Assert.False(p.Gueltig);
            Assert.Contains("Projekte", p.FehlerText());
        }

        // --------------------------------------------------------------------- JSON-Weg

        [Fact]
        public void JsonAufruf_WirdVollstaendigUebernommen()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Beispielregister.Erzeuge(), "vielerlei",
                "{\"projekt_id\":1007,\"schwelle_kw\":565.76,\"speichern\":true," +
                "\"gewerk\":\"BHKW\",\"projekt_ids\":[1007,1009]}");

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(1007, p.Aufruf!.Id("projekt_id"));
            Assert.Equal(565.76, p.Aufruf.Zahl("schwelle_kw"), 6);
            Assert.True(p.Aufruf.Wahrheit("speichern"));
            Assert.Equal(Beispielregister.Gewerk2, p.Aufruf.Text("gewerk"));
            Assert.Equal(new[] { 1007, 1009 }, p.Aufruf.IdListe("projekt_ids"));
        }

        [Fact]
        public void KaputtesJson_WirdAbgewiesenStattZuWerfen()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Beispielregister.Erzeuge(), "projekt_lesen",
                                                      "{\"projekt_id\": 1007");
            Assert.False(p.Gueltig);
            Assert.NotEmpty(p.Fehler);
        }

        [Fact]
        public void JsonFeldStattObjekt_WirdAbgewiesen()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Beispielregister.Erzeuge(), "projekt_lesen", "[1007]");

            Assert.False(p.Gueltig);
            Assert.NotEmpty(p.Fehler);
        }

        [Fact]
        public void JsonNull_ZaehltAlsFehlendesPflichtfeld()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Beispielregister.Erzeuge(), "projekt_lesen",
                                                      "{\"projekt_id\":null}");
            Assert.False(p.Gueltig);
            Assert.Contains("Projekt (ID)", p.FehlerText());
        }

        // ------------------------------------------------------------------- Kulturregel

        [Fact]
        public void ZahlAlsTextWirdInvariantGelesen_AuchUnterDeutscherKultur()
        {
            // Unter de-DE waere "1.5" sonst 15 - genau die Falle, die Fachkonzept 3.2 nennt.
            CultureInfo vorher = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                    Beispielregister.Werte("projekt_id", 1, "schwelle_kw", "1.5"));

                Assert.True(p.Gueltig, p.FehlerText());
                Assert.Equal(1.5, p.Aufruf!.Zahl("schwelle_kw"), 9);
            }
            finally { Thread.CurrentThread.CurrentCulture = vorher; }
        }

        // ------------------------------------------------------------- Mehrere Fehler

        [Fact]
        public void MehrereFehler_WerdenAlleGemeldet()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Vielerlei,
                Beispielregister.Werte("gewerk", "Kernkraftwerk", "unfug", 1));

            Assert.False(p.Gueltig);
            Assert.Equal(3, p.Fehler.Count);   // unbekannter Parameter, Pflichtfeld, Aufzaehlung
        }

        [Fact]
        public void GepruefterAufruf_KenntSeineAktion()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Beispielregister.Erzeuge(), "projekt_lesen",
                                                  Beispielregister.Werte("projekt_id", 1007));

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal("projekt_lesen", p.Aufruf!.Name);
            Assert.Equal(Schutzstufe.Lesen, p.Aufruf.Aktion.Stufe);
            Assert.Equal("ProjektCtrl.ReadSingle(int)", p.Aufruf.Aktion.Andockpunkt);
        }
    }
}
