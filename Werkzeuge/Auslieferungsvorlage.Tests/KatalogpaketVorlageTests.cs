using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// <b>Das Katalogpaket neben der Vorlage</b> (Entscheidungsvorlage Modellgrenzen KU1 Stufe 1) —
    /// am Ergebnis desselben Laufs wie <see cref="VorlageTests"/>.
    ///
    /// <para><b>Die Probe</b> <c>Referenzlaeufe/Importproben/Katalogpaket_Probe.json</c> ist der
    /// Prozesswärme-Teil des Pakets der Testdatenbank (Kopfsätze und Wochenprofile der acht
    /// ausgelieferten Betriebsweisen, Fassung 1) — neutrale Namen, runde Werte, kein Herstellerdatum.
    /// Sie muss mit dem Teil des geschriebenen Pakets byte-gleich sein; dieselbe Probe nehmen die
    /// Abgleichsfälle in <c>EPOS.Kern.Tests/KatalogabgleichTests</c>.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class KatalogpaketVorlageTests : IClassFixture<Vorlage>
    {
        /// <summary>Die Tabellen der Probe.</summary>
        internal static readonly string[] PROBENTABELLEN = { "Tab_Prozesswaerme_STAMM", "Tab_Prozesstyp_STAMM" };

        private readonly Vorlage _v;
        public KatalogpaketVorlageTests(Vorlage v) { _v = v; }

        private string Paketdatei => Katalogpaket.Pfad(_v.Ziel);

        private static string Probe => Path.Combine(Werkzeuglauf.Repowurzel, "Referenzlaeufe", "Importproben",
                                                    "Katalogpaket_Probe.json");

        /// <summary>Das Paket entsteht neben der Vorlage, ist lesbar und in seiner kanonischen Form geschrieben.</summary>
        [Fact]
        public void K1_Das_Paket_liegt_neben_der_Vorlage_und_ist_kanonisch()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Assert.True(File.Exists(Paketdatei), "Das Katalogpaket fehlt neben der Vorlage: " + Paketdatei);
            byte[] bytes = File.ReadAllBytes(Paketdatei);
            Katalogpaket paket = Katalogpaket.AusBytes(bytes);
            Assert.True(paket.Fassung >= 1);
            Assert.Equal(bytes, paket.Bytes());
            Assert.Contains("Schritt 4b — Katalogpaket", File.ReadAllText(_v.Ziel + ".bericht.txt"));
        }

        /// <summary>Der Prozesswärme-Teil des Pakets ist byte-gleich mit der Probe im Repositorium.</summary>
        [Fact]
        public void K2_Der_Prozesswaermeteil_ist_byte_gleich_mit_der_Probe()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Katalogpaket paket = Katalogpaket.Lesen(Paketdatei);
            var teil = new Katalogpaket { Fassung = 1 };
            teil.Tabellen.AddRange(paket.Tabellen.Where(t => PROBENTABELLEN.Contains(t.Tabelle)));
            Assert.Equal(2, teil.Tabellen.Count);
            Assert.Equal(File.ReadAllBytes(Probe), teil.Bytes());
        }

        /// <summary>
        /// Die Vorlage trägt genau den Stand des Pakets: dieselbe Katalogfassung, und jeder gesperrte
        /// Satz der Stufe 1 mit Schlüssel und der Prüfsumme des Pakets. Eine Neuinstallation gleicht
        /// deshalb beim ersten Start nichts ab.
        /// </summary>
        [Fact]
        public void K3_Die_Vorlage_traegt_Fassung_Schluessel_und_Pruefsummen_des_Pakets()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Katalogpaket paket = Katalogpaket.Lesen(Paketdatei);
            var befund = _v.Lesen(() =>
            {
                int? fassung = Katalogabgleich.FassungDerDatenbank();
                KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(paket, nurPruefen: true, erzwingen: true);
                var ohneSchluessel = new List<string>();
                foreach (Katalogtabelle t in Katalogfassung.Stufe1)
                {
                    object n = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + t.Tabelle +
                                                            "\" WHERE \"ReadOnly\" = 1 AND \"Katalog_Schluessel\" IS NULL");
                    if (Convert.ToInt64(n) > 0) ohneSchluessel.Add(t.Tabelle);
                }
                return (Fassung: fassung, Plan: plan, Ohne: ohneSchluessel);
            });

            Assert.Equal(paket.Fassung, befund.Fassung);
            Assert.Empty(befund.Ohne);
            Assert.Equal(0, befund.Plan.Neu + befund.Plan.Aktualisiert + befund.Plan.Behalten + befund.Plan.Ausgelaufen);
        }
    }
}
