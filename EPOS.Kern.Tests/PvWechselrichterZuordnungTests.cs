using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die HÜLLENSEITE der Wechselrichterzuordnung</b> — der Wächter zum Befund
    /// <b>W6‑B‑3</b> der Windows-Abnahme vom 07.09.2026 (wörtlich: „die gesamte
    /// Zuordnung Wechselrichter zum PV-Modul und Strang funktioniert nicht — Auswahl
    /// Wechselrichter nicht vorhanden").
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Der Befund hatte fünf Verdächtige, und
    /// einer davon war die Hülle: Vielleicht kamen die Listen gar nicht erst an
    /// (<c>PhotovoltaikHuelle.Gaben</c> zieht sie beim Aufmachen aus
    /// <c>WechselrichterStammCtrl</c>). Das ist HIER zu messen und nirgends sonst —
    /// <c>EPOS.UI.Tests</c> kennt weder Controller noch Datenbank, und
    /// <c>PhotovoltaikHuelle</c> selbst liegt in einem <c>net10.0-windows</c>-Projekt,
    /// das auf dem ubuntu-Läufer nicht baut. Die Fälle bauen deshalb die drei
    /// Delegatenkörper der Hülle NACH — Zeile für Zeile wie dort — und halten sie gegen
    /// die Testdatenbank.</para>
    ///
    /// <para><b>Das Messergebnis vom 07.09.2026:</b> Die Listen kommen an. Der Katalog
    /// der Testdatenbank führt genau ein Gerät („Muster 2500TL", Hersteller „Muster"),
    /// und beide Gaben sind gefüllt — die Ursache lag also in der KOMPONENTE
    /// (die weiche Sperre auf der Option „mit Wechselrichter", siehe
    /// <c>PvStraengeFelderTests.W6B3_*</c>), nicht in der Hülle.</para>
    ///
    /// <para><b>Der zweite Fall ist der Auslieferungszustand:</b> Schritt 65 legt den
    /// Wechselrichterkatalog LEER an (W6‑O‑3), gefüllt wird er über den CEC/OND-Import.
    /// Dann sind beide Listen leer — und genau dafür zeigt der Abschnitt seither den
    /// Weg zum Import statt eines stummen leeren Feldes.</para>
    ///
    /// <para><b>Eine Arbeitskopie je Klasse</b> (Regel seit iU9‑W11a); fehlt die Datei,
    /// schweigen die Fälle. Wer schreibt, nimmt eine EIGENE Kopie je Fall.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvWechselrichterZuordnungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public PvWechselrichterZuordnungTests(TestDatenbank db) { _db = db; }

        /// <summary>Das Prüfprojekt mit den zwei Ost/West-Strängen (W6‑O‑7).</summary>
        private const int ID_1045 = 1045;

        // =================================================================================
        //  Die drei Delegatenkörper der Hülle, Zeile für Zeile wie in
        //  WindowsFormsApplication1/Views/Photovoltaik/PhotovoltaikHuelle.cs
        // =================================================================================

        /// <summary>
        /// <c>PhotovoltaikHuelle.WechselrichterEintraege</c> (Z. 451‑458) — die Gabe
        /// <c>Wechselrichter</c> beim Aufmachen und der <c>WechselrichterFiltern</c>-Delegat.
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> Eintraege(string hersteller)
        {
            var liste = new List<(int, string)>();
            foreach (WechselrichterStammCtrl.KatalogZeile z in
                     new WechselrichterStammCtrl().Filtern(hersteller))
                liste.Add((z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>
        /// <c>PhotovoltaikHuelle.WechselrichterHersteller</c> (Z. 464‑469) — die Gabe
        /// <c>WechselrichterHersteller</c>, „Alle" voran.
        /// </summary>
        private static IReadOnlyList<string> Hersteller()
        {
            var liste = new List<string> { "Alle" };
            foreach (string h in WechselrichterStammCtrl.Hersteller()) liste.Add(h);
            return liste;
        }

        /// <summary>
        /// <c>PhotovoltaikHuelle["Modulhersteller"]</c> (Z. 197‑202) — die Vorauswahl des
        /// Herstellerfilters (W6‑E‑6).
        /// </summary>
        private static string Modulhersteller(string bezeichner)
        {
            PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(bezeichner);
            return d == null ? "" : (d.Firma ?? "");
        }

        // =================================================================================
        //  1 — Der Katalog ist gefüllt: die Hülle liefert BEIDE Listen
        // =================================================================================

        /// <summary>
        /// <b>H4 ist widerlegt.</b> Die zwei Gaben, an denen Herstellerfilter und
        /// Klappliste hängen, sind gefüllt — der Abschnitt hätte sie zeigen können.
        /// </summary>
        [Fact]
        public void Die_Huelle_liefert_Herstellerliste_und_Geraeteliste()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<string> hersteller = Hersteller();
            Assert.True(hersteller.Count > 1, "Der Wechselrichterkatalog führt keinen Hersteller.");
            Assert.Equal("Alle", hersteller[0]);
            Assert.Contains("Muster", hersteller);

            IReadOnlyList<(int Id, string Text)> geraete = Eintraege("");
            Assert.NotEmpty(geraete);
            Assert.Contains(geraete, g => g.Text == "Muster 2500TL");
            Assert.All(geraete, g => Assert.True(g.Id > 0));
        }

        /// <summary>
        /// Der <c>WechselrichterFiltern</c>-Delegat engt auf den Hersteller ein; „Alle"
        /// und der leere Text heben die Einengung auf — derselbe Steuerwert wie beim
        /// Modulfilter.
        /// </summary>
        [Fact]
        public void Der_Filterdelegat_engt_auf_den_Hersteller_ein()
        {
            if (!_db.Vorhanden) return;

            Assert.NotEmpty(Eintraege("Muster"));
            Assert.Equal(Eintraege("").Count, Eintraege("Alle").Count);
            Assert.Empty(Eintraege("Gibt es nicht"));
        }

        /// <summary>
        /// <b>W6‑E‑6:</b> Die Vorauswahl braucht den Hersteller des ANLAGENmoduls — die
        /// Hülle liest ihn aus demselben Katalogsatz, aus dem auch die kWp kommen.
        /// </summary>
        [Fact]
        public void Die_Huelle_liefert_den_Modulhersteller_fuer_die_Vorauswahl()
        {
            if (!_db.Vorhanden) return;

            IReadOnlyList<(int Id, string Text)> module = Modulliste();
            Assert.NotEmpty(module);

            string firma = Modulhersteller(module[0].Text);
            Assert.False(string.IsNullOrWhiteSpace(firma),
                         "Ohne Herstellernamen am Modul gibt es keine Vorauswahl.");

            // Ein unbekannter Bezeichner bleibt still - der Filter steht dann auf "Alle".
            Assert.Equal("", Modulhersteller("Gibt es nicht"));
        }

        /// <summary><c>PhotovoltaikHuelle.ModulEintraege</c> (Z. 486‑493).</summary>
        private static IReadOnlyList<(int Id, string Text)> Modulliste()
        {
            var liste = new List<(int, string)>();
            foreach (PhotovoltaikStammCtrl.KatalogZeile z in new PhotovoltaikStammCtrl().Filtern(""))
                liste.Add((z.Id, z.Bezeichner));
            return liste;
        }

        // =================================================================================
        //  2 — „Strang anlegen" mit Gerät: der Weg bis in die Projekttabelle
        // =================================================================================

        /// <summary>
        /// <b>W6‑B‑3:</b> Seit dem Umbau nimmt „Strang anlegen" das Gerät der Katalogwahl
        /// mit; dahinter steht der Delegat <c>WechselrichterUebernehmen</c> mit
        /// <c>CopyFromStamm</c>. Der Fall geht den Weg bis in die Projekttabelle und
        /// zurück: Die Kopie trägt den Bezeichner des Katalogsatzes — genau das Band, über
        /// das die Klappliste ihren Eintrag wiederfindet.
        /// </summary>
        [Fact]
        public void Uebernehmen_legt_die_Projektkopie_an_und_findet_sie_wieder()
        {
            using (var eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                IReadOnlyList<(int Id, string Text)> geraete = Eintraege("");
                Assert.NotEmpty(geraete);
                int stammId = geraete[0].Id;

                var kopien = new WechselrichterCtrl();
                int id = kopien.CopyFromStamm(stammId, ID_1045);
                Assert.True(id > 0, "CopyFromStamm hat keine Projektkopie geliefert.");

                Assert.Equal(geraete[0].Text, WechselrichterStammCtrl.BezeichnerZu(stammId));

                // Zweimal dasselbe Gerät ergibt EINE Kopie - der zweite Strang hängt sich
                // an dieselbe (das Band ist der Bezeichner).
                Assert.Equal(id, kopien.CopyFromStamm(stammId, ID_1045));

                // Und die Ampel findet die Kopie über ReadAll wieder.
                kopien.ReadAll(ID_1045);
                Assert.Contains(kopien.items, g => g != null && g.m_ID == id);
            }
        }

        // =================================================================================
        //  3 — Der AUSLIEFERUNGSZUSTAND: leerer Katalog
        // =================================================================================

        /// <summary>
        /// <b>Der leere Katalog ist der Auslieferungszustand</b> (W6‑O‑3: Schritt 65 legt
        /// die Tabellen ohne DML an). Beide Gaben sind dann leer — der Abschnitt zeigt
        /// weder Filterzeile noch Klappliste, und genau deshalb steht dort seit W6‑B‑3
        /// der Weg zum Import statt eines stummen leeren Feldes.
        /// </summary>
        [Fact]
        public void Ohne_Katalog_bleiben_beide_Listen_leer()
        {
            using (var eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                // Geleert wird mit SQL und nicht über WechselrichterStammCtrl.Delete: Der
                // Satz der Testdatenbank trägt ReadOnly = 1 („gehört zur Auslieferung"),
                // und genau das lehnt der Controller bestimmungsgemäss ab. Hier geht es
                // nicht um die Löschregel, sondern um den ZUSTAND leerer Katalog.
                Assert.True(DataRepository.ExecuteSQL(
                    "DELETE FROM [" + WechselrichterStammCtrl.TABLE + "]"));

                Assert.Empty(Eintraege(""));

                IReadOnlyList<string> hersteller = Hersteller();
                Assert.Single(hersteller);          // nur noch "Alle"
                Assert.Equal("Alle", hersteller[0]);
            }
        }
    }
}
