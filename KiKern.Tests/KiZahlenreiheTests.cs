using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die ZAHLENREIHE im Kern des Assistenten (Welle #458 Stufe 3b): Form und Text der
    /// Reihe, die Deklaration am Maskenfeld, der Parametertyp <c>ZahlListe</c> in Schema
    /// und Pruefung und der gekuerzte Reihenblock der Bestaetigung.
    /// </summary>
    public class KiZahlenreiheTests : IDisposable
    {
        private static readonly CultureInfo De = new CultureInfo("de-DE");

        public void Dispose() => KiTexte.Lieferant = null;

        private static KiZahlenreihe Monate() => new KiZahlenreihe(new[]
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        });

        private static KiZahlenreihe Stellen(int laenge)
        {
            var namen = new string[laenge];
            for (int i = 0; i < laenge; i++) namen[i] = "S" + (i + 1).ToString(CultureInfo.InvariantCulture);
            return new KiZahlenreihe(namen);
        }

        // =================================================================== Die Form

        [Fact]
        public void DieFormNenntLaengeStellenUndUmfang()
        {
            KiZahlenreihe reihe = Monate();

            Assert.Equal(12, reihe.Laenge);
            Assert.Equal("März", reihe.Stellenname(3));
            Assert.Equal("13", reihe.Stellenname(13));          // ausserhalb: die Nummer
            Assert.Equal("12 Werte, Januar bis Dezember", reihe.Umfang());
        }

        [Fact]
        public void EineFormOhneStellennamenOderZuKurzOderZuLangWirdAbgewiesen()
        {
            Assert.Throws<ArgumentNullException>(() => new KiZahlenreihe(null!));
            Assert.Throws<ArgumentException>(() => new KiZahlenreihe(new[] { "nur eine" }));
            Assert.Throws<ArgumentException>(() => new KiZahlenreihe(new[] { "a", " " }));
            Assert.Throws<ArgumentException>(() => Stellen(KiZahlenreihe.MaxLaenge + 1));
        }

        // =================================================================== Die Werte

        [Fact]
        public void DieWerteKommenAusJederZahlenfolgeUndLeerBleibtLeer()
        {
            Assert.Equal(new double?[] { 1.5, null, 3 }, KiZahlenreihe.Werte(new double?[] { 1.5, null, 3 }));
            Assert.Equal(new double?[] { 1, 2 }, KiZahlenreihe.Werte(new[] { 1.0, 2.0 }));
            Assert.Equal(new double?[] { 4, 5 }, KiZahlenreihe.Werte(new List<int> { 4, 5 }));
        }

        [Fact]
        public void EinText_EinSkalarUndEineFolgeAusTextSindKeineZahlenreihe()
        {
            Assert.Null(KiZahlenreihe.Werte("1;2;3"));
            Assert.Null(KiZahlenreihe.Werte(42.0));
            Assert.Null(KiZahlenreihe.Werte(new[] { "1", "2" }));
            Assert.Null(KiZahlenreihe.Werte(null));
        }

        [Fact]
        public void DieListeTrenntMitStrichpunktUndBenenntDasLeereGlied()
        {
            // Das Komma ist in de-DE das Dezimalzeichen - getrennt wird mit Strichpunkt.
            Assert.Equal("1,5; (leer); 3", KiZahlenreihe.Liste(new double?[] { 1.5, null, 3 }, De));
        }

        [Fact]
        public void DieKurzfassungZeigtDenAnfangUndDieGesamtzahl()
        {
            var werte = new double?[168];
            for (int i = 0; i < werte.Length; i++) werte[i] = i + 1;

            string kurz = KiZahlenreihe.Kurz(werte, De);

            Assert.StartsWith("1; 2; 3", kurz);
            Assert.Contains("12", kurz);
            Assert.DoesNotContain("13;", kurz);
            Assert.EndsWith("… (168 Werte)", kurz);

            // Eine kurze Reihe bleibt ganz.
            Assert.Equal("1; 2", KiZahlenreihe.Kurz(new double?[] { 1, 2 }, De));
        }

        // =================================================================== Die Deklaration

        [Fact]
        public void EinMaskenfeldTraegtDieZahlenreiheMitIhrerForm()
        {
            var f = new KiDialogFeld("monatswerte", "Daten.Monat", "Monatswerte", KiParameterTyp.ZahlListe,
                                     "Zwölf Werte.", einheit: "MWh", reihe: Monate(), min: 0, max: 100);

            Assert.True(f.IstReihe);
            Assert.Equal(12, f.Reihe!.Laenge);
            Assert.True(f.HatBereich);
            Assert.Equal(0, f.Min);
            Assert.Equal(100, f.Max);
        }

        [Fact]
        public void ArtUndFormGehoerenZusammen()
        {
            // Zahlenreihe ohne Form: eine Liste unbekannter Laenge.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "werte", "Daten.Werte", "Werte", KiParameterTyp.ZahlListe, "Werte."));

            // Form ohne Zahlenreihe: eine Angabe ohne Gegenstand.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "wert", "Daten.Wert", "Wert", KiParameterTyp.Zahl, "Ein Wert.", reihe: Monate()));

            // Eine Zahlenreihe ist EIN Feld - keine Spalte.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "werte", "Daten.Zeilen[].Werte", "Werte", KiParameterTyp.ZahlListe, "Werte.", reihe: Monate()));

            // Die Liste von IDs bleibt ausgeschlossen.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "ids", "Daten.Ids", "Ids", KiParameterTyp.GanzzahlListe, "Ids."));
        }

        [Fact]
        public void GrenzenGibtEsNurFuerZahlenUndInDerRichtigenReihenfolge()
        {
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "name", "Daten.Name", "Name", KiParameterTyp.Text, "Ein Name.", min: 0));
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "tag", "Daten.Tag", "Tag", KiParameterTyp.Ganzzahl, "Ein Tag.", min: 31, max: 1));
        }

        [Fact]
        public void EineSpaltenzeileBehaeltIhreGrenzen()
        {
            var spalte = new KiDialogFeld("tag", "Daten.Zeilen[].Tag", "Tag", KiParameterTyp.Ganzzahl,
                                          "Ein Tag.", zeilenkennzeichen: "Zeitraum", min: 1, max: 31);

            KiDialogFeld zeile = spalte.FuerZeile(3, "Sommer");

            Assert.Equal("tag_3", zeile.Name);
            Assert.Equal(1, zeile.Min);
            Assert.Equal(31, zeile.Max);
            Assert.False(zeile.IstReihe);
        }

        // =================================================================== Parameter

        private static KiAktion Reihenaktion() => new KiAktion(
            name: "reihe_pruefen",
            zweck: "Prüffall einer Zahlenreihe.",
            stufe: Schutzstufe.Lesen,
            andockpunkt: "Testfall",
            parameter: new[]
            {
                new KiParameter("werte", KiParameterTyp.ZahlListe, "Die Werte.", anzeigename: "Werte",
                                min: 0, max: 100),
                new KiParameter("ab", KiParameterTyp.Ganzzahl, "Erste Stelle.", pflicht: false,
                                anzeigename: "ab Stelle", min: 1)
            });

        [Fact]
        public void DerParameterGehtAlsZahlenfeldInsSchema()
        {
            JsonObject schema = KiSchema.SchemaKnoten(Reihenaktion());
            JsonNode werte = schema["properties"]!["werte"]!;

            Assert.Equal("array", werte["type"]!.GetValue<string>());
            Assert.Equal("number", werte["items"]!["type"]!.GetValue<string>());
        }

        [Fact]
        public void DiePruefungLiefertEinDoubleFeld()
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(new KiRegister().Aufnehmen(Reihenaktion()),
                                                      "reihe_pruefen", "{\"werte\":[1.5, 2, 3.25],\"ab\":4}");

            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(new[] { 1.5, 2.0, 3.25 }, p.Aufruf!.ZahlListe("werte"));
            Assert.Equal(4, p.Aufruf.Id("ab"));
        }

        [Fact]
        public void EinSkalarGiltAlsEinelementigeListe_TextGliederInvariant()
        {
            KiPruefErgebnis skalar = KiPruefung.Pruefe(Reihenaktion(), Beispielregister.Werte("werte", 7.0));
            Assert.True(skalar.Gueltig, skalar.FehlerText());
            Assert.Equal(new[] { 7.0 }, skalar.Aufruf!.ZahlListe("werte"));

            KiPruefErgebnis text = KiPruefung.Pruefe(Reihenaktion(),
                                                     Beispielregister.Werte("werte", new[] { "1.5", "2" }));
            Assert.True(text.Gueltig, text.FehlerText());
            Assert.Equal(new[] { 1.5, 2.0 }, text.Aufruf!.ZahlListe("werte"));
        }

        [Fact]
        public void LeereListe_KeineZahl_UndGrenzenJeGliedWerdenAbgewiesen()
        {
            KiRegister register = new KiRegister().Aufnehmen(Reihenaktion());

            Assert.False(KiPruefung.PruefeJson(register, "reihe_pruefen", "{\"werte\":[]}").Gueltig);
            Assert.False(KiPruefung.PruefeJson(register, "reihe_pruefen", "{\"werte\":[1,\"x\"]}").Gueltig);

            KiPruefErgebnis zuGross = KiPruefung.PruefeJson(register, "reihe_pruefen", "{\"werte\":[1,101]}");
            Assert.False(zuGross.Gueltig);
            Assert.Contains("101", zuGross.FehlerText());
        }

        [Fact]
        public void DasProtokollTraegtDieVolleListe_DieBestaetigungDieGekuerzte()
        {
            var zahlen = new List<object?>();
            for (int i = 0; i < 30; i++) zahlen.Add((double)i);

            KiPruefErgebnis p = KiPruefung.Pruefe(Reihenaktion(), Beispielregister.Werte("werte", zahlen));
            Assert.True(p.Gueltig, p.FehlerText());

            string json = p.Aufruf!.AlsJson();
            Assert.Contains("29", json);                              // jeder Wert im Protokoll

            string angaben = string.Join("\n", p.Aufruf.AlsKlartext(De));
            Assert.Contains("… (30 Werte)", angaben);                 // gekuerzt in der Anzeige
            Assert.DoesNotContain("29", angaben);
        }

        // =================================================================== Der Reihenblock

        [Fact]
        public void DerReihenblockNenntNurDieGeaendertenStellenMitIhrenNamen()
        {
            var alt = new double?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var neu = new double?[] { 1, 2, 30, 4, 5, 6, 7, 8, 9, 10, 11, 12.5 };

            string block = KiFeldBlock.Reihe("Typprofil", "Monatswerte", Monate(), alt, neu, De);

            Assert.Equal("Maske: Typprofil\n" +
                         "Monatswerte · 2 von 12 Werten ändern sich\n" +
                         "Monatswerte (März) · 3 → 30\n" +
                         "Monatswerte (Dezember) · 12 → 12,5\n", block);
        }

        [Fact]
        public void DerReihenblockKuerztUndNenntDenRest()
        {
            KiZahlenreihe reihe = Stellen(168);
            var alt = new double?[168];
            var neu = new double?[168];
            for (int i = 0; i < 168; i++) { alt[i] = 0; neu[i] = i < 24 ? 1 : 0; }

            string block = KiFeldBlock.Reihe("Kostenprofil", "Wochenwerte", reihe, alt, neu, De);

            Assert.Contains("Wochenwerte · 24 von 168 Werten ändern sich", block);
            Assert.Contains("Wochenwerte (S12) · 0 → 1", block);
            Assert.DoesNotContain("(S13)", block);
            Assert.EndsWith("… und 12 weitere\n", block);
        }

        [Fact]
        public void EinLeeresGliedWirdBenanntUndOhneAenderungGibtEsKeinenBlock()
        {
            string block = KiFeldBlock.Reihe("Maske", "Werte", Stellen(2),
                                             new double?[] { null, 2 }, new double?[] { 1, 2 }, De);
            Assert.Contains("Werte (S1) · (leer) → 1", block);

            Assert.Throws<ArgumentException>(() => KiFeldBlock.Reihe(
                "Maske", "Werte", Stellen(2), new double?[] { 1, 2 }, new double?[] { 1, 2 }, De));
        }
    }
}
