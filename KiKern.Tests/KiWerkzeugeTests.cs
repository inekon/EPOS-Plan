using System.Text.Json;
using System.Text.Json.Nodes;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Der Werkzeugkatalog auf der Leitung: Drahtform, Modus und Datenschutz
    /// (Fachkonzept 3.3 und 4.2).
    /// </summary>
    public class KiWerkzeugeTests
    {
        private static KiRegister R() => Registerabbild.Erzeuge();

        [Fact]
        public void ToolsFeldHatGenauEinenEintragMitAllenDeklarationen()
        {
            JsonNode? feld = JsonNode.Parse(KiWerkzeuge.Tools(R()));

            var liste = Assert.IsType<JsonArray>(feld);
            Assert.Single(liste);

            JsonArray? deklarationen = liste[0]!["functionDeclarations"] as JsonArray;
            Assert.NotNull(deklarationen);
            Assert.Equal(R().Anzahl, deklarationen!.Count);
        }

        [Fact]
        public void JedeDeklarationFuehrtNameUndZweck()
        {
            JsonArray deklarationen =
                (JsonArray)JsonNode.Parse(KiWerkzeuge.Tools(R()))![0]!["functionDeclarations"]!;

            foreach (JsonNode? d in deklarationen)
            {
                Assert.False(string.IsNullOrWhiteSpace(d!["name"]!.GetValue<string>()));
                Assert.False(string.IsNullOrWhiteSpace(d["description"]!.GetValue<string>()));
            }
        }

        [Fact]
        public void ParameterloseAktionBekommtKeinParametersFeld()
        {
            // Ein leeres properties-Objekt ist in der OpenAPI-Teilmenge des Anbieters nicht
            // vorgesehen; die dokumentierte Form ist das Weglassen.
            JsonArray deklarationen =
                (JsonArray)JsonNode.Parse(KiWerkzeuge.Tools(R()))![0]!["functionDeclarations"]!;

            JsonNode ohne = Finde(deklarationen, "projekte_auflisten");
            JsonNode mit = Finde(deklarationen, "projekt_lesen");

            Assert.Null(ohne["parameters"]);
            Assert.NotNull(mit["parameters"]);
            Assert.Equal("object", mit["parameters"]!["type"]!.GetValue<string>());
        }

        [Fact]
        public void PflichtangabenStehenImSchema()
        {
            JsonArray deklarationen =
                (JsonArray)JsonNode.Parse(KiWerkzeuge.Tools(R()))![0]!["functionDeclarations"]!;

            JsonNode spitze = Finde(deklarationen, "minimale_spitze_ermitteln");
            var pflicht = (JsonArray)spitze["parameters"]!["required"]!;

            Assert.Equal(3, pflicht.Count);          // ganglinie_id, kapazitaet_kwh, leistung_kw
            Assert.Contains(pflicht, k => k!.GetValue<string>() == "ganglinie_id");
        }

        [Fact]
        public void AufzaehlungGehtMitIhrenWertenHinaus()
        {
            JsonArray deklarationen =
                (JsonArray)JsonNode.Parse(KiWerkzeuge.Tools(R()))![0]!["functionDeclarations"]!;

            var werte = (JsonArray)Finde(deklarationen, "kostenlage_pruefen")
                                   ["parameters"]!["properties"]!["komponente"]!["enum"]!;

            Assert.Equal(Registerabbild.Komponenten.Length, werte.Count);
        }

        [Fact]
        public void DerModusIstAutoUndNiemalsAny()
        {
            Assert.Equal("AUTO", KiWerkzeuge.ModusSchluessel(KiWerkzeugmodus.Auto));
            Assert.Equal("NONE", KiWerkzeuge.ModusSchluessel(KiWerkzeugmodus.Aus));

            // Es gibt schlicht keinen Wert fuer "erzwungen" - Fachkonzept 3.3, Festlegung 1.
            Assert.Equal(2, System.Enum.GetValues(typeof(KiWerkzeugmodus)).Length);
        }

        [Fact]
        public void ToolConfigHatDieErwarteteForm()
        {
            JsonNode? k = JsonNode.Parse(KiWerkzeuge.ToolConfig(KiWerkzeugmodus.Auto));

            Assert.Equal("AUTO", k!["functionCallingConfig"]!["mode"]!.GetValue<string>());
        }

        [Fact]
        public void AntwortteilTraegtNamenUndErgebnis()
        {
            JsonObject k = KiWerkzeuge.AntwortteilKnoten("projekt_lesen", "{\"anzahl\":1}");

            Assert.Equal("projekt_lesen", k["functionResponse"]!["name"]!.GetValue<string>());
            Assert.Equal(1, k["functionResponse"]!["response"]!["anzahl"]!.GetValue<int>());
        }

        [Fact]
        public void AntwortteilErtraegtUnbrauchbaresJson()
        {
            JsonObject k = KiWerkzeuge.AntwortteilKnoten("projekt_lesen", "kein json");

            Assert.Equal("kein json", k["functionResponse"]!["response"]!["text"]!.GetValue<string>());
        }

        [Fact]
        public void AufrufteilSpiegeltDieRundeDesModellsZurueck()
        {
            JsonObject k = KiWerkzeuge.AufrufteilKnoten("projekt_lesen", "{\"projekt_id\":5}");

            Assert.Equal("projekt_lesen", k["functionCall"]!["name"]!.GetValue<string>());
            Assert.Equal(5, k["functionCall"]!["args"]!["projekt_id"]!.GetValue<int>());
        }

        [Fact]
        public void VerlaufseintragKenntNurAnwenderUndModell()
        {
            Assert.Equal("user", KiWerkzeuge.RolleAnwender);
            Assert.Equal("model", KiWerkzeuge.RolleModell);

            JsonObject e = KiWerkzeuge.VerlaufseintragKnoten(
                KiWerkzeuge.RolleAnwender, KiWerkzeuge.TextteilKnoten("Hallo"));

            Assert.Equal("user", e["role"]!.GetValue<string>());
            Assert.Single((JsonArray)e["parts"]!);
        }

        // ================================================================ Weg B

        [Fact]
        public void WegBAnweisungNenntFormUndKatalog()
        {
            string text = KiWerkzeuge.WegBAnweisung(R());

            Assert.Contains("\"aktion\"", text);
            Assert.Contains("\"parameter\"", text);
            Assert.Contains("projekte_auflisten", text);
            Assert.Contains("minimale_spitze_ermitteln", text);
            Assert.Contains("Punkt als Dezimaltrennzeichen", text);
        }

        [Fact]
        public void BeideWegeSpeisenSichAusDerselbenDeklaration()
        {
            // Was Weg A als functionDeclarations sendet, steht bei Weg B im Prompt - sonst
            // koennten die Wege fuer dieselbe Aeusserung verschiedene Aufrufe erzeugen.
            string wegA = KiWerkzeuge.Tools(R());
            string wegB = KiWerkzeuge.WegBAnweisung(R());

            foreach (KiAktion a in R().Alle)
            {
                Assert.Contains("\"" + a.Name + "\"", wegA);
                Assert.Contains("\"" + a.Name + "\"", wegB);
            }
        }

        // ============================================================ Datenschutz

        [Fact]
        public void DerKatalogTraegtKeinenProjektKundenOderAnlagennamen()
        {
            // Abnahme Etappe 2: im Fenster "Was wird gesendet?" darf kein Klarname stehen.
            // Der Katalog besteht ausschliesslich aus Deklarationen - er kann gar keinen
            // fuehren, weil er nie eine Datenbank sieht. Das wird hier festgeschrieben.
            string katalog = KiWerkzeuge.Tools(R()) + KiWerkzeuge.WegBAnweisung(R());

            string[] verboten =
            {
                "Musterstra", "Muster GmbH", "Kunde:", "Projektname:", "Beispiel WP WG",
                "@", "C:\\", "D:\\"
            };

            foreach (string v in verboten)
                Assert.DoesNotContain(v, katalog);
        }

        [Fact]
        public void DerKatalogIstJsonUndBleibtUeberschaubar()
        {
            string katalog = KiWerkzeuge.Tools(R());

            using JsonDocument doc = JsonDocument.Parse(katalog);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

            // Kostenzeile aus Fachkonzept 3.3: rund 1500-2500 Zeichen bei 20 Aktionen.
            Assert.InRange(katalog.Length, 1000, 12000);
        }

        private static JsonNode Finde(JsonArray deklarationen, string name)
        {
            foreach (JsonNode? d in deklarationen)
                if (d!["name"]!.GetValue<string>() == name) return d;
            throw new Xunit.Sdk.XunitException("Deklaration fehlt: " + name);
        }
    }
}
