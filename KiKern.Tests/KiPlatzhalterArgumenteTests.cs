using System.Text.Json.Nodes;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Der Rueckweg der Bezeichner in den ARGUMENTEN eines Werkzeugaufrufs
    /// (Fachkonzept 4.2, zweite Haelfte).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Anlass ist ein echter Fehlerfall (23.08.2026).</b> Auf die Frage „liste
    /// investitionskosten aller varianten in projekt woehler auf" listete das Modell
    /// zuerst die Projekte, sah in den zwanzig Zeilen nur „Name 1" bis „Name 20" und
    /// schloss daraus, das Projekt gebe es nicht. Ein Namensvergleich in Ergebniszeilen
    /// ist prinzipiell unmoeglich - der Name muss unveraendert als PARAMETER hinausgehen.
    /// Gibt das Modell stattdessen den Platzhalter zurueck, den es kennt, muss dieser VOR
    /// der Pruefung wieder zum Klarnamen werden.
    /// </para>
    /// <para>
    /// Geprueft wird beides: die reine Zeichenkettenlogik an <see cref="KiPlatzhalter"/>
    /// und ihre Wirkung in der Absichtserkennung, wo aus dem Modellaufruf ein
    /// <see cref="KiAufruf"/> entsteht.
    /// </para>
    /// </remarks>
    public class KiPlatzhalterArgumenteTests
    {
        private const string Woehler = "Woehler Wohnbau GmbH";
        private const string Muster = "Musterstrasse 7";

        /// <summary>Eine Tabelle, wie sie nach einem projekte_auflisten aussieht.</summary>
        private static KiPlatzhalter Tabelle()
        {
            var p = new KiPlatzhalter();
            p.Fuer("Alpha AG");        // Name 1
            p.Fuer("Beta KG");         // Name 2
            p.Fuer(Woehler);           // Name 3
            p.Fuer("Gamma GmbH");      // Name 4
            p.Fuer(Muster);            // Name 5
            return p;
        }

        /// <summary>
        /// Ein Register mit genau den beiden Formen, um die es geht: ein TEXT-Parameter,
        /// der eine Namensaufloesung fuettert, und eine Zahl, die unberuehrt bleiben muss.
        /// </summary>
        private static KiRegister Register()
        {
            return new KiRegister().Aufnehmen(new KiAktion(
                "ergebnisse_lesen",
                "Liest die gespeicherten Wirtschaftlichkeitsergebnisse mehrerer Projekte.",
                Schutzstufe.Lesen, "WirtschaftlichkeitCtrl.LadeErgebnisse",
                new[]
                {
                    new KiParameter("projekte", KiParameterTyp.Text,
                                    "Namen der Projekte; mehrere durch Semikolon trennen.",
                                    anzeigename: "Projekte", maxLaenge: 600),
                    new KiParameter("anzahl", KiParameterTyp.Ganzzahl,
                                    "Wie viele Zeilen genannt werden sollen.",
                                    pflicht: false, anzeigename: "Anzahl", min: 1)
                }));
        }

        // ================================================= Einzelwert (KlarnameOderText)

        [Fact]
        public void EinExakterPlatzhalterWirdZumKlarnamen()
        {
            Assert.Equal(Woehler, Tabelle().KlarnameOderText("Name 3"));
        }

        [Fact]
        public void FuehrendeLeerzeichenStoerenDieZuordnungNicht()
        {
            // Modelle setzen gern ein Leerzeichen hinter das Trennzeichen einer Aufzaehlung.
            Assert.Equal(Woehler, Tabelle().KlarnameOderText("  Name 3 "));
        }

        [Fact]
        public void EingebettetePlatzhalterWerdenImSatzErsetzt()
        {
            Assert.Equal("Alpha AG und " + Woehler,
                         Tabelle().KlarnameOderText("Name 1 und Name 3"));
        }

        [Fact]
        public void EineSemikolonlisteBleibtEineSemikolonliste()
        {
            Assert.Equal(Woehler + "; " + Muster,
                         Tabelle().KlarnameOderText("Name 3; Name 5"));
        }

        [Fact]
        public void UnbekannterTextGehtUnveraendertDurch()
        {
            // Der Regelfall: der Anwender hat den Klarnamen selbst getippt. Er muss die
            // lokale Namensaufloesung unbeschadet erreichen - auch als Teilname.
            KiPlatzhalter p = Tabelle();

            Assert.Equal("woehler", p.KlarnameOderText("woehler"));
            Assert.Equal("Name 9", p.KlarnameOderText("Name 9"));
            Assert.Equal("", p.KlarnameOderText(""));
            Assert.Equal("", p.KlarnameOderText(null));
        }

        [Fact]
        public void Name12WirdNichtAlsName1MitAngehaengterZwoelfGelesen()
        {
            var p = new KiPlatzhalter();
            for (int i = 1; i <= 12; i++) p.Fuer("Projekt " + i);

            Assert.Equal("Projekt 12", p.KlarnameOderText("Name 12"));
            Assert.Equal("Projekt 1", p.KlarnameOderText("Name 1"));
            Assert.Equal("Projekt 12 und Projekt 1", p.KlarnameOderText("Name 12 und Name 1"));
        }

        [Fact]
        public void OhneTabelleBleibtDerTextStehen()
        {
            Assert.Equal("Name 3", new KiPlatzhalter().KlarnameOderText("Name 3"));
        }

        // ======================================================= Argumente (JSON-Objekt)

        [Fact]
        public void NurTextwerteWerdenUebersetztZahlenNicht()
        {
            string json = Tabelle().ArgumenteAufloesen(
                "{\"projekte\":\"Name 3; Name 5\",\"anzahl\":12,\"aktiv\":true}");

            JsonNode k = JsonNode.Parse(json)!;
            Assert.Equal(Woehler + "; " + Muster, k["projekte"]!.GetValue<string>());
            Assert.Equal(12, k["anzahl"]!.GetValue<int>());
            Assert.True(k["aktiv"]!.GetValue<bool>());
        }

        [Fact]
        public void IdListenBleibenZahlenlisten()
        {
            // Wichtig ist, dass die Zahlen die Uebersetzung unbeschadet ueberstehen -
            // sie waren nie platzgehalten und meinen etwas anderes als ein Bezeichner.
            string json = Tabelle().ArgumenteAufloesen("{\"projekt_ids\":[1042,1043]}");

            JsonArray f = (JsonArray)JsonNode.Parse(json)!["projekt_ids"]!;
            Assert.Equal(1042, f[0]!.GetValue<int>());
            Assert.Equal(1043, f[1]!.GetValue<int>());
        }

        [Fact]
        public void TexteInListenWerdenEbenfallsUebersetzt()
        {
            string json = Tabelle().ArgumenteAufloesen("{\"werte\":[\"Name 3\",\"frei\"]}");

            JsonArray f = (JsonArray)JsonNode.Parse(json)!["werte"]!;
            Assert.Equal(Woehler, f[0]!.GetValue<string>());
            Assert.Equal("frei", f[1]!.GetValue<string>());
        }

        [Fact]
        public void OhneTabelleBleibenDieArgumenteUnveraendert()
        {
            const string json = "{\"projekte\":\"Name 3\"}";
            Assert.Equal(json, new KiPlatzhalter().ArgumenteAufloesen(json));
        }

        [Fact]
        public void UnbrauchbarerArgumenttextGehtUnveraendertWeiter()
        {
            // Die Beanstandung gehoert der Parameterpruefung, die dem Modell einen
            // Klartextgrund nennen kann - hier waere sie eine zweite, stille Fehlerquelle.
            KiPlatzhalter p = Tabelle();

            Assert.Equal("kein json", p.ArgumenteAufloesen("kein json"));
            Assert.Equal("[1,2]", p.ArgumenteAufloesen("[1,2]"));
            Assert.Equal("", p.ArgumenteAufloesen(null));
        }

        // ======================================================== Wirkung in der Absicht

        [Fact]
        public void DerAufrufTrifftDasProjektObwohlDasModellNurPlatzhalterKennt()
        {
            KiAbsichtBefund b = KiAbsicht.AusWerkzeugantwort(
                Register(),
                KiModellantwort.Lesen(Aeusserungen.RumpfA(
                    "ergebnisse_lesen", "{\"projekte\":\"Name 3; Name 5\"}")),
                Tabelle());

            Assert.True(b.Gueltig, b.FehlerText());
            Assert.Equal(Woehler + "; " + Muster, b.Aufruf!.Text("projekte"));
        }

        [Fact]
        public void OhnePlatzhaltertabelleErreichtDerPlatzhalterDieAktion()
        {
            // Der Aktionsharnisch laeuft ohne Datenschutzschicht - dort darf sich nichts
            // aendern, sonst pruefte er etwas anderes, als die Anwendung tut.
            KiAbsichtBefund b = KiAbsicht.AusWerkzeugantwort(
                Register(),
                KiModellantwort.Lesen(Aeusserungen.RumpfA(
                    "ergebnisse_lesen", "{\"projekte\":\"Name 3\"}")));

            Assert.True(b.Gueltig, b.FehlerText());
            Assert.Equal("Name 3", b.Aufruf!.Text("projekte"));
        }

        [Fact]
        public void WegBLoestDieselbenPlatzhalterAufWieWegA()
        {
            KiRegister r = Register();
            const string argumente = "{\"projekte\":\"Name 3\"}";

            KiAbsichtBefund a = KiAbsicht.AusWerkzeugantwort(
                r, KiModellantwort.Lesen(Aeusserungen.RumpfA("ergebnisse_lesen", argumente)),
                Tabelle());
            KiAbsichtBefund b = KiAbsicht.AusText(
                r, "{\"aktion\":\"ergebnisse_lesen\",\"parameter\":" + argumente + "}",
                Tabelle());

            Assert.True(a.Gueltig, a.FehlerText());
            Assert.True(b.Gueltig, b.FehlerText());
            Assert.Equal(a.Aufruf!.AlsJson(), b.Aufruf!.AlsJson());
            Assert.Equal(Woehler, b.Aufruf!.Text("projekte"));
        }

        [Fact]
        public void DerBegleittextDesWegesBBleibtUnangetastet()
        {
            // Er wird erst unmittelbar vor der Anzeige aufgeloest (KiChatService); wuerde
            // hier schon uebersetzt, stuende der Klarname doppelt im Verlauf.
            KiAbsichtBefund b = KiAbsicht.AusText(
                Register(),
                "Ich sehe nach.\n{\"aktion\":\"ergebnisse_lesen\"," +
                "\"parameter\":{\"projekte\":\"Name 3\"}}",
                Tabelle());

            Assert.True(b.Gueltig, b.FehlerText());
            Assert.Equal("Ich sehe nach.", b.Text);
            Assert.Equal(Woehler, b.Aufruf!.Text("projekte"));
        }
    }
}
