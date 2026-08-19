using System.Text.Json;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Schemaerzeugung (Fachkonzept 3.2, Verwendung a).
    ///
    /// Geprueft wird gegen die TEILMENGE, die der Werkzeugkatalog des Anbieters annimmt:
    /// type, description, enum, items, properties, required - und ausdruecklich NICHTS
    /// darueber hinaus.
    /// </summary>
    public class KiSchemaTests
    {
        private static JsonElement Wurzel(string json) => JsonDocument.Parse(json).RootElement;

        [Fact]
        public void AktionOhneParameter_LiefertLeeresObjektschemaOhneRequired()
        {
            string json = KiSchema.Erzeuge(Beispielregister.OhneParameter());
            JsonElement s = Wurzel(json);

            Assert.Equal("object", s.GetProperty("type").GetString());
            Assert.Empty(s.GetProperty("properties").EnumerateObject());
            Assert.False(s.TryGetProperty("required", out _));
        }

        [Fact]
        public void Pflichtparameter_StehtInRequired_OptionalerNicht()
        {
            JsonElement s = Wurzel(KiSchema.Erzeuge(Beispielregister.MitAllenTypen()));

            JsonElement pflicht = s.GetProperty("required");
            Assert.Equal(1, pflicht.GetArrayLength());
            Assert.Equal("projekt_id", pflicht[0].GetString());
        }

        [Theory]
        [InlineData("projekt_id", "integer")]
        [InlineData("schwelle_kw", "number")]
        [InlineData("bezeichner", "string")]
        [InlineData("speichern", "boolean")]
        [InlineData("gewerk", "string")]
        [InlineData("projekt_ids", "array")]
        public void JederParametertyp_WirdAufDenErwartetenJsonTypAbgebildet(string name, string erwartet)
        {
            JsonElement e = Wurzel(KiSchema.Erzeuge(Beispielregister.MitAllenTypen()))
                            .GetProperty("properties").GetProperty(name);

            Assert.Equal(erwartet, e.GetProperty("type").GetString());
        }

        [Fact]
        public void Aufzaehlung_TraegtIhreWerte_UndZwarDieDerDeklaration()
        {
            JsonElement e = Wurzel(KiSchema.Erzeuge(Beispielregister.MitAllenTypen()))
                            .GetProperty("properties").GetProperty("gewerk");

            JsonElement werte = e.GetProperty("enum");
            Assert.Equal(2, werte.GetArrayLength());
            Assert.Equal(Beispielregister.Gewerk1, werte[0].GetString());
            Assert.Equal(Beispielregister.Gewerk2, werte[1].GetString());
        }

        [Fact]
        public void ZahlenListe_BeschreibtIhreGliederAlsGanzzahl()
        {
            JsonElement e = Wurzel(KiSchema.Erzeuge(Beispielregister.MitAllenTypen()))
                            .GetProperty("properties").GetProperty("projekt_ids");

            Assert.Equal("integer", e.GetProperty("items").GetProperty("type").GetString());
        }

        [Fact]
        public void Wertebereich_StehtImBeschreibungstext_NichtAlsSchluesselwort()
        {
            // Begruendung siehe KiParameter.SchemaBeschreibung: der Werkzeugkatalog des
            // Anbieters nimmt minimum/maximum nicht zuverlaessig an; geprueft wird die
            // Grenze ohnehin in C# - aus derselben Deklaration.
            JsonElement e = Wurzel(KiSchema.Erzeuge(Beispielregister.MitAllenTypen()))
                            .GetProperty("properties").GetProperty("schwelle_kw");

            Assert.False(e.TryGetProperty("minimum", out _));
            Assert.False(e.TryGetProperty("maximum", out _));
            Assert.Contains("0 bis 100000", e.GetProperty("description").GetString());
            Assert.Contains("[kW]", e.GetProperty("description").GetString());
        }

        [Fact]
        public void Schema_EnthaeltKeineSchluesselwoerterAusserhalbDerTeilmenge()
        {
            string json = KiSchema.Erzeuge(Beispielregister.MitAllenTypen());

            Assert.DoesNotContain("$schema", json);
            Assert.DoesNotContain("$defs", json);
            Assert.DoesNotContain("additionalProperties", json);
        }

        [Fact]
        public void Werkzeugkatalog_FuehrtJedeAktionMitNameZweckUndSchema()
        {
            KiRegister register = Beispielregister.Erzeuge();
            JsonElement katalog = Wurzel(KiSchema.Werkzeugkatalog(register));

            Assert.Equal(register.Anzahl, katalog.GetArrayLength());

            JsonElement erste = katalog[0];
            Assert.Equal("projekte_auflisten", erste.GetProperty("name").GetString());
            Assert.Equal(register.Alle[0].Zweck, erste.GetProperty("description").GetString());
            Assert.Equal("object", erste.GetProperty("parameters").GetProperty("type").GetString());
        }

        [Fact]
        public void Werkzeugkatalog_BleibtBeiZweiAufrufenGleich()
        {
            // Damit ein Zwischenspeicher und ein Diff im Protokoll ueberhaupt tragen.
            KiRegister register = Beispielregister.Erzeuge();
            Assert.Equal(KiSchema.Werkzeugkatalog(register), KiSchema.Werkzeugkatalog(register));
        }

        [Fact]
        public void SchemaBeschreibung_NenntDieHoechstlaengeEinesTextes()
        {
            JsonElement e = Wurzel(KiSchema.Erzeuge(Beispielregister.MitAllenTypen()))
                            .GetProperty("properties").GetProperty("bezeichner");

            Assert.Contains("10 Zeichen", e.GetProperty("description").GetString());
        }

        [Fact]
        public void WerteAlsJson_SchreibtZahlenInvariant_UndInDeklarationsreihenfolge()
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Beispielregister.MitAllenTypen(),
                Beispielregister.Werte("schwelle_kw", 1.5, "projekt_id", 1007));

            Assert.True(p.Gueltig, p.FehlerText());
            // Reihenfolge folgt der Deklaration, nicht der Reihenfolge der Uebergabe.
            Assert.Equal("{\"projekt_id\":1007,\"schwelle_kw\":1.5}", p.Aufruf!.AlsJson());
        }
    }
}
