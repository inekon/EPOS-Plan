using System.Collections.Generic;
using System.Text.Json.Nodes;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Was von einem Aktionsergebnis an das Modell zurueckgeht (Fachkonzept 3.6 und 4.2).
    /// </summary>
    public class KiRueckmeldungTests
    {
        private static KiAufruf Aufruf(string aktion, string argumente)
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Registerabbild.Erzeuge(), aktion, argumente);
            Assert.True(p.Gueltig, p.FehlerText());
            return p.Aufruf!;
        }

        private static IReadOnlyDictionary<string, object?> Zeile(params object?[] paare)
        {
            var d = new Dictionary<string, object?>();
            for (int i = 0; i + 1 < paare.Length; i += 2) d[(string)paare[i]!] = paare[i + 1];
            return d;
        }

        // ============================================================= Platzhalter

        [Fact]
        public void DerselbeKlarnameBekommtDenselbenPlatzhalter()
        {
            var p = new KiPlatzhalter();

            Assert.Equal("Name 1", p.Fuer("Musterstraße 7"));
            Assert.Equal("Name 2", p.Fuer("Muster GmbH"));
            Assert.Equal("Name 1", p.Fuer("Musterstraße 7"));
            Assert.Equal(2, p.Anzahl);
        }

        [Fact]
        public void LeereTexteBleibenLeer()
        {
            var p = new KiPlatzhalter();

            Assert.Equal("", p.Fuer(null));
            Assert.Equal("", p.Fuer("   "));
            Assert.Equal(0, p.Anzahl);
        }

        [Fact]
        public void DerRueckwegLiefertDenKlarnamen()
        {
            var p = new KiPlatzhalter();
            p.Fuer("Musterstraße 7");

            Assert.Equal("Musterstraße 7", p.Klarname("Name 1"));
            Assert.Null(p.Klarname("Name 9"));
        }

        [Fact]
        public void AufloesenErsetztImModelltext()
        {
            var p = new KiPlatzhalter();
            p.Fuer("Musterstraße 7");
            p.Fuer("Muster GmbH");

            Assert.Equal("Musterstraße 7 gehört zu Muster GmbH.",
                         p.Aufloesen("Name 1 gehört zu Name 2."));
        }

        [Fact]
        public void AufloesenVerwechseltNichtName1MitName12()
        {
            var p = new KiPlatzhalter();
            for (int i = 1; i <= 12; i++) p.Fuer("Projekt " + i);

            Assert.Equal("Projekt 12", p.Aufloesen("Name 12"));
            Assert.Equal("Projekt 1", p.Aufloesen("Name 1"));
        }

        [Fact]
        public void LeerenSetztDieTabelleZurueck()
        {
            var p = new KiPlatzhalter();
            p.Fuer("Muster GmbH");
            p.Leeren();

            Assert.Equal(0, p.Anzahl);
            Assert.Equal("Name 1", p.Fuer("Andere GmbH"));
        }

        // ============================================================ Verdichtung

        [Fact]
        public void ZahlenGehenDurchTexteWerdenPlatzgehalten()
        {
            var p = new KiPlatzhalter();
            KiErgebnis e = KiErgebnis.Ok("2 Projekte gefunden.", new[]
            {
                Zeile("id", 1042L, "name", "Musterstraße 7", "leistung_kw", 12.5),
                Zeile("id", 1043L, "name", "Muster GmbH", "leistung_kw", 8.0)
            });

            JsonNode k = JsonNode.Parse(
                KiRueckmeldung.Erzeuge(Aufruf("projekte_auflisten", "{}"), e, p))!;

            var zeilen = (JsonArray)k["zeilen"]!;
            Assert.Equal(1042, zeilen[0]!["id"]!.GetValue<int>());
            Assert.Equal(12.5, zeilen[0]!["leistung_kw"]!.GetValue<double>());
            Assert.Equal("Name 1", zeilen[0]!["name"]!.GetValue<string>());
            Assert.Equal("Name 2", zeilen[1]!["name"]!.GetValue<string>());
        }

        [Fact]
        public void KeinKlarnameGehtHinaus()
        {
            var p = new KiPlatzhalter();
            KiErgebnis e = KiErgebnis.Ok("Projekt Musterstraße 7 gelesen.", new[]
            {
                Zeile("name", "Musterstraße 7", "kunde", "Muster GmbH")
            });

            string json = KiRueckmeldung.Erzeuge(Aufruf("projekt_lesen", "{\"projekt_id\":1}"), e, p);

            Assert.DoesNotContain("Musterstraße", json);
            Assert.DoesNotContain("Muster GmbH", json);
            Assert.Contains("Name 1", json);
        }

        [Fact]
        public void DerErgebnissatzBehaeltSeineFachsprache()
        {
            var p = new KiPlatzhalter();
            KiErgebnis e = KiErgebnis.Ok("Kleinste haltbare Spitze: 120 kW.");

            string json = KiRueckmeldung.Erzeuge(
                Aufruf("minimale_spitze_ermitteln",
                       "{\"ganglinie_id\":1,\"kapazitaet_kwh\":200,\"leistung_kw\":100}"), e, p);

            Assert.Contains("Kleinste haltbare Spitze", json);
            Assert.Contains("120 kW", json);
        }

        [Fact]
        public void NieGanzeReihenSondernHoechstensZwanzigZeilen()
        {
            var zeilen = new List<IReadOnlyDictionary<string, object?>>();
            for (int i = 0; i < 57; i++) zeilen.Add(Zeile("id", (long)i));

            JsonNode k = JsonNode.Parse(KiRueckmeldung.Erzeuge(
                Aufruf("projekte_auflisten", "{}"),
                KiErgebnis.Ok("57 Projekte gefunden.", zeilen), null))!;

            Assert.Equal(KiRueckmeldung.MaxZeilen, ((JsonArray)k["zeilen"]!).Count);
            Assert.Equal(37, k["weitere_zeilen"]!.GetValue<int>());
            Assert.Equal(57, k["anzahl"]!.GetValue<int>());
        }

        [Fact]
        public void StatusUndAktionStehenImmerDrin()
        {
            JsonNode k = JsonNode.Parse(KiRueckmeldung.Erzeuge(
                Aufruf("projekte_auflisten", "{}"), KiErgebnis.Ok("nichts"), null))!;

            Assert.Equal("projekte_auflisten", k["aktion"]!.GetValue<string>());
            Assert.Equal("ausgefuehrt", k["status"]!.GetValue<string>());
        }

        [Fact]
        public void StilleFehlerGehenAlsMeldungenMit()
        {
            KiErgebnis e = KiErgebnis.Ok("gelesen").MitMeldungen(new[] { "Tabelle fehlt." });

            JsonNode k = JsonNode.Parse(KiRueckmeldung.Erzeuge(
                Aufruf("projekte_auflisten", "{}"), e, null))!;

            Assert.Equal("Tabelle fehlt.", ((JsonArray)k["meldungen"]!)[0]!.GetValue<string>());
        }

        [Fact]
        public void AbgelehntNenntDenGrund()
        {
            JsonNode k = JsonNode.Parse(
                KiRueckmeldung.Abgelehnt("variante_anlegen", "Kommt mit der Bestätigungsschicht."))!;

            Assert.Equal("abgelehnt", k["status"]!.GetValue<string>());
            Assert.Contains("Bestätigungsschicht", k["grund"]!.GetValue<string>());
        }

        [Fact]
        public void OhnePlatzhaltertabelleBleibtAllesStehen()
        {
            // Fuer den Aktionsharnisch und die Anzeige im Chat, wo nichts verborgen wird.
            KiErgebnis e = KiErgebnis.Ok("gelesen", new[] { Zeile("name", "Musterstraße 7") });

            string json = KiRueckmeldung.Erzeuge(Aufruf("projekte_auflisten", "{}"), e, null);

            Assert.Contains("Musterstraße 7", json);
        }
    }
}
