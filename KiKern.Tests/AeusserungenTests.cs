using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Der Datensatz der 20 Anwenderaeusserungen (Fachkonzept 8, Etappe 2).
    /// </summary>
    /// <remarks>
    /// Geprueft wird ausschliesslich die Zuordnung Modellantwort auf <see cref="KiAufruf"/>.
    /// Es entsteht kein Netzverkehr und es wird kein API-Schluessel gebraucht.
    /// </remarks>
    public class AeusserungenTests
    {
        private static KiRegister R() => Registerabbild.Erzeuge();

        /// <summary>Die 20 Aeusserungen als xunit-Datenquelle.</summary>
        public static IEnumerable<object[]> Datensatz()
        {
            foreach (Aeusserung a in Aeusserungen.Alle) yield return new object[] { a.Text };
        }

        private static Aeusserung Hole(string text)
        {
            foreach (Aeusserung a in Aeusserungen.Alle)
                if (a.Text == text) return a;
            throw new Xunit.Sdk.XunitException("Aeusserung fehlt: " + text);
        }

        [Fact]
        public void EsSindGenauZwanzigAeusserungen()
            => Assert.Equal(20, Aeusserungen.Alle.Count);

        [Fact]
        public void JedeErwarteteAktionStehtImRegister()
        {
            KiRegister r = R();
            foreach (Aeusserung a in Aeusserungen.Alle)
                Assert.True(r.Kennt(a.Aktion), "Unbekannte Aktion im Datensatz: " + a.Aktion);
        }

        [Fact]
        public void DerDatensatzDecktDasGanzeRegisterAb()
        {
            // Jede der 13 Aktionen kommt mindestens einmal vor - sonst prueft der Datensatz
            // nur die Haelfte des Registers.
            var genannt = new HashSet<string>();
            foreach (Aeusserung a in Aeusserungen.Alle) genannt.Add(a.Aktion);

            foreach (KiAktion a in R().Alle)
                Assert.Contains(a.Name, genannt);
        }

        [Fact]
        public void JedeRahmungKommtVor()
        {
            var gesehen = new HashSet<Rahmen>();
            foreach (Aeusserung a in Aeusserungen.Alle) gesehen.Add(a.Rahmen);

            Assert.Equal(5, gesehen.Count);
        }

        [Theory]
        [MemberData(nameof(Datensatz))]
        public void WegA_ErgibtDenErwartetenAufruf(string text)
        {
            Aeusserung f = Hole(text);

            KiAbsichtBefund b = KiAbsicht.AusWerkzeugantwort(R(),
                KiModellantwort.Lesen(Aeusserungen.RumpfA(f.Aktion, f.Argumente)));

            Assert.True(b.Gueltig, f.Text + ": " + b.FehlerText());
            Assert.Equal(f.Aktion, b.Aufruf!.Name);
            Assert.Equal(f.ErwartetesJson, b.Aufruf.AlsJson());
        }

        [Theory]
        [MemberData(nameof(Datensatz))]
        public void WegB_ErgibtDenErwartetenAufruf(string text)
        {
            Aeusserung f = Hole(text);

            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                Aeusserungen.TextB(f.Aktion, f.Argumente, f.Rahmen));

            Assert.True(b.Gueltig, f.Text + ": " + b.FehlerText());
            Assert.Equal(f.Aktion, b.Aufruf!.Name);
            Assert.Equal(f.ErwartetesJson, b.Aufruf.AlsJson());
        }

        [Theory]
        [MemberData(nameof(Datensatz))]
        public void BeideWegeErgebenDenselbenAufruf(string text)
        {
            // Die Kernzusage der Etappe 2: bei abgeschaltetem Werkzeugpfad liefert Weg B
            // dieselben Aufrufe (Fachkonzept 8, Etappe 2, Abnahme).
            Aeusserung f = Hole(text);
            KiRegister r = R();

            KiAbsichtBefund a = KiAbsicht.AusWerkzeugantwort(r,
                KiModellantwort.Lesen(Aeusserungen.RumpfA(f.Aktion, f.Argumente)));
            KiAbsichtBefund b = KiAbsicht.AusText(r,
                Aeusserungen.TextB(f.Aktion, f.Argumente, f.Rahmen));

            Assert.True(a.Gueltig && b.Gueltig);
            Assert.Equal(a.Aufruf!.Name, b.Aufruf!.Name);
            Assert.Equal(a.Aufruf.AlsJson(), b.Aufruf.AlsJson());
        }

        [Theory]
        [MemberData(nameof(Datensatz))]
        public void JederAufrufDarfOhneBestaetigungLaufen(string text)
        {
            // Alle 20 Aeusserungen fuehren auf Stufe 1 - null Ausfuehrungen ohne
            // Bestaetigung ist damit nicht Zufall, sondern vom Riegel gedeckt.
            Aeusserung f = Hole(text);

            KiAbsichtBefund b = KiAbsicht.AusText(R(),
                Aeusserungen.TextB(f.Aktion, f.Argumente, f.Rahmen));

            Assert.Equal(Schutzstufe.Lesen, b.Aufruf!.Aktion.Stufe);
            Assert.True(KiRiegel.DarfDirektLaufen(b.Aufruf));
        }

        [Fact]
        public void DieGegenprobeLoestKeineAktionAus()
        {
            KiRegister r = R();
            foreach (string satz in Aeusserungen.OhneAktion)
            {
                Assert.False(KiAbsicht.AusText(r, satz).HatAbsicht);
                Assert.False(KiAbsicht.AusWerkzeugantwort(r,
                    KiModellantwort.Lesen(Aeusserungen.RumpfAOhneAufruf(satz))).HatAbsicht);
            }
        }
    }
}
