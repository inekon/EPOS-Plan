using System.Globalization;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die Abbildung Modellantwort auf <see cref="KiAufruf"/> - fuer beide Wege der
    /// Absichtserkennung (Fachkonzept 3.3).
    /// </summary>
    public class KiAbsichtTests
    {
        private static KiRegister R() => Registerabbild.Erzeuge();

        // ================================================== Weg A: Antwort zerlegen

        [Fact]
        public void WegA_LiestNameUndArgumente()
        {
            KiModellantwort m = KiModellantwort.Lesen(
                Aeusserungen.RumpfA("projekt_lesen", "{\"projekt_id\":1042}"));

            Assert.True(m.HatWerkzeugruf);
            Assert.Equal("projekt_lesen", m.Werkzeugrufe[0].Name);
            Assert.Contains("1042", m.Werkzeugrufe[0].ArgumenteJson);
            Assert.Equal("STOP", m.Abschlussgrund);
            Assert.NotEqual("", m.InhaltJson);
        }

        [Fact]
        public void WegA_ReineTextantwortHatKeinenAufruf()
        {
            KiModellantwort m = KiModellantwort.Lesen(
                Aeusserungen.RumpfAOhneAufruf("Der Kapitalwert steht im Reiter Wirtschaftlichkeit."));

            Assert.False(m.HatWerkzeugruf);
            Assert.Contains("Kapitalwert", m.Text);

            KiAbsichtBefund b = KiAbsicht.AusWerkzeugantwort(R(), m);
            Assert.False(b.HatAbsicht);
            Assert.Null(b.Aufruf);
        }

        [Fact]
        public void WegA_BegleittextUndAufrufZugleich()
        {
            KiModellantwort m = KiModellantwort.Lesen(
                Aeusserungen.RumpfA("projekte_auflisten", "{}", "Ich sehe kurz nach."));

            Assert.True(m.HatWerkzeugruf);
            Assert.Equal("Ich sehe kurz nach.", m.Text);
        }

        [Fact]
        public void WegA_UnbrauchbarerRumpfErgibtLeereAntwort()
        {
            Assert.False(KiModellantwort.Lesen("kein json").HatWerkzeugruf);
            Assert.False(KiModellantwort.Lesen("").HatWerkzeugruf);
            Assert.False(KiModellantwort.Lesen(null).HatWerkzeugruf);
            Assert.False(KiModellantwort.Lesen("{\"candidates\":[]}").HatWerkzeugruf);
        }

        [Fact]
        public void WegA_MissglueckterAufrufWirdErkannt()
        {
            const string rumpf =
                "{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[]}," +
                "\"finishReason\":\"MALFORMED_FUNCTION_CALL\"}]}";

            Assert.True(KiModellantwort.Lesen(rumpf).WerkzeugrufMissglueckt);
        }

        [Fact]
        public void WegA_NurDerErsteAufrufWirdGenommen()
        {
            // Fachkonzept 3.3, Festlegung 4: hoechstens EINE Aktion je Aeusserung.
            KiModellantwort m = KiModellantwort.Lesen(
                Aeusserungen.RumpfAMehrfach("projekte_auflisten", "letzte_aktionen"));

            KiAbsichtBefund b = KiAbsicht.AusWerkzeugantwort(R(), m);

            Assert.Equal("projekte_auflisten", b.Werkzeugname);
            Assert.True(b.Gueltig);
            Assert.Single(b.Uebergangen);
            Assert.Equal("letzte_aktionen", b.Uebergangen[0]);
            Assert.Contains("letzte_aktionen", b.Text);
        }

        // ================================================== Weg B: Toleranzparser

        [Theory]
        [InlineData("{\"a\":1}", "{\"a\":1}")]
        [InlineData("```json\n{\"a\":1}\n```", "{\"a\":1}")]
        [InlineData("```\n{\"a\":1}\n```", "{\"a\":1}")]
        [InlineData("Ich sehe nach:\n{\"a\":1}", "{\"a\":1}")]
        [InlineData("Klar.\n```json\n{\"a\":1}\n```\nDanach melde ich mich.", "{\"a\":1}")]
        [InlineData("  {\"a\":{\"b\":2}}  ", "{\"a\":{\"b\":2}}")]
        public void WegB_FindetDasJsonInJederRahmung(string text, string erwartet)
            => Assert.Equal(erwartet, KiAbsicht.JsonAusText(text));

        [Fact]
        public void WegB_GeschweifteKlammerInEinemWertBeendetDasObjektNicht()
        {
            const string text = "Antwort: {\"pfad\":\"C:\\\\{Ordner}\\\\x.csv\",\"n\":2} - fertig";
            string? json = KiAbsicht.JsonAusText(text);

            Assert.NotNull(json);
            Assert.EndsWith("\"n\":2}", json);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Dazu steht nichts in der Hilfe.")]
        [InlineData("Eine offene Klammer { ohne Ende")]
        public void WegB_OhneJsonKommtNichts(string? text)
            => Assert.Null(KiAbsicht.JsonAusText(text));

        [Fact]
        public void WegB_ErstesUnbrauchbaresObjektWirdUebersprungen()
        {
            // Ein Modell schreibt gern erst eine Formel oder ein Beispiel hin.
            string? json = KiAbsicht.JsonAusText("Beispiel {nicht json} und dann {\"aktion\":\"x\"}");

            Assert.Equal("{\"aktion\":\"x\"}", json);
        }

        [Fact]
        public void WegB_ReineProsaIstKeineAbsicht()
        {
            foreach (string satz in Aeusserungen.OhneAktion)
            {
                KiAbsichtBefund b = KiAbsicht.AusText(R(), satz);
                Assert.False(b.HatAbsicht);
                Assert.Equal(satz, b.Text);
            }
        }

        [Theory]
        [InlineData("keine")]
        [InlineData("none")]
        [InlineData("nichts")]
        public void WegB_AusdrueckenFuerKeineAktion(string name)
        {
            KiAbsichtBefund b = KiAbsicht.AusText(R(), "{\"aktion\":\"" + name + "\"}");
            Assert.False(b.HatAbsicht);
        }

        [Theory]
        [InlineData("aktion", "parameter")]
        [InlineData("name", "args")]
        [InlineData("function", "arguments")]
        [InlineData("werkzeug", "werte")]
        public void WegB_ErtraegtAbweichendeFeldnamen(string namensfeld, string parameterfeld)
        {
            string text = "{\"" + namensfeld + "\":\"projekt_lesen\",\""
                          + parameterfeld + "\":{\"projekt_id\":9}}";

            KiAbsichtBefund b = KiAbsicht.AusText(R(), text);

            Assert.True(b.Gueltig);
            Assert.Equal("projekt_lesen", b.Aufruf!.Name);
            Assert.Equal(9, b.Aufruf.Id("projekt_id"));
        }

        [Fact]
        public void WegB_ParameterAlsJsonTextStattObjekt()
        {
            // Haeufiger Fehlgriff: das Modell verschachtelt den Parameterblock als Text.
            const string text = "{\"aktion\":\"projekt_lesen\",\"parameter\":\"{\\\"projekt_id\\\":5}\"}";

            KiAbsichtBefund b = KiAbsicht.AusText(R(), text);

            Assert.True(b.Gueltig);
            Assert.Equal(5, b.Aufruf!.Id("projekt_id"));
        }

        [Fact]
        public void WegB_TextNebenDemJsonBleibtErhalten()
        {
            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                "Ich sehe nach:\n{\"aktion\":\"projekte_auflisten\",\"parameter\":{}}");

            Assert.True(b.Gueltig);
            Assert.Equal("Ich sehe nach:", b.Text);
        }

        // ================================================== Pruefung greift auf beiden Wegen

        [Fact]
        public void UnbekannteAktionWirdMitDerListeBeantwortet()
        {
            KiAbsichtBefund a = KiAbsicht.AusWerkzeugantwort(R(),
                KiModellantwort.Lesen(Aeusserungen.RumpfA("projekt_loeschen", "{}")));
            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                "{\"aktion\":\"projekt_loeschen\",\"parameter\":{}}");

            Assert.True(a.HatAbsicht);
            Assert.False(a.Gueltig);
            Assert.Contains("projekte_auflisten", a.FehlerText());
            Assert.Equal(a.FehlerText(), b.FehlerText());
        }

        [Fact]
        public void FehlendePflichtangabeWirdAufBeidenWegenGleichBeanstandet()
        {
            KiAbsichtBefund a = KiAbsicht.AusWerkzeugantwort(R(),
                KiModellantwort.Lesen(Aeusserungen.RumpfA("projekt_lesen", "{}")));
            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                "{\"aktion\":\"projekt_lesen\",\"parameter\":{}}");

            Assert.False(a.Gueltig);
            Assert.False(b.Gueltig);
            Assert.Contains("projekt_id", a.FehlerText());
            Assert.Equal(a.FehlerText(), b.FehlerText());
        }

        [Fact]
        public void ErfundenerParameterLoestEineKorrekturrundeAus()
        {
            KiAbsichtBefund b = KiAbsicht.AusWerkzeugantwort(R(),
                KiModellantwort.Lesen(Aeusserungen.RumpfA(
                    "projekt_lesen", "{\"projekt_id\":3,\"tiefe\":2}")));

            Assert.False(b.Gueltig);
            Assert.Contains("tiefe", b.FehlerText());
        }

        [Fact]
        public void WertAusserhalbDesBereichsWirdAbgewiesen()
        {
            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                "{\"aktion\":\"letzte_aktionen\",\"parameter\":{\"anzahl\":500}}");

            Assert.False(b.Gueltig);
            Assert.Contains("50", b.FehlerText());
        }

        [Fact]
        public void AufzaehlungKommtAlsPersistenzwertZurueck()
        {
            // Gross-/Kleinschreibung darf keine Korrekturrunde kosten; gespeichert wird
            // aber der Wert aus DbWerte.
            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                "{\"aktion\":\"kostenlage_pruefen\",\"parameter\":{\"projekt_id\":4,\"komponente\":\"bhkw\"}}");

            Assert.True(b.Gueltig);
            Assert.Equal("BHKW", b.Aufruf!.Text("komponente"));
        }

        [Fact]
        public void ZahlenWerdenInvariantGelesen()
        {
            // "0.92" ist auch unter de-DE 0,92 und nicht 92.
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                KiAbsichtBefund b = KiAbsicht.AusText(R(),
                    "{\"aktion\":\"minimale_spitze_ermitteln\",\"parameter\":" +
                    "{\"ganglinie_id\":1,\"kapazitaet_kwh\":200,\"leistung_kw\":100,\"wirkungsgrad_rt\":0.92}}");

                Assert.True(b.Gueltig);
                Assert.Equal(0.92, b.Aufruf!.Zahl("wirkungsgrad_rt"), 6);
            }
            finally
            {
                CultureInfo.CurrentCulture = vorher;
            }
        }
    }
}
