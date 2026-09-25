using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Berichte;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Häkchen</b> (Etappe BV-E2, Teil H; Konzept Berichtsvorlagen 10.2 „Häkchen
    /// (BV-Q1 c)", 13 „Bausteintitel nach MyResource"): der Kapitelstand aus der Schnellprüfung der
    /// gewählten Vorlage (<see cref="Pruefbefund.Bausteine"/>, <see cref="Pruefbefund.HatKapitel"/>) —
    /// als Regel und über echte Vorlagen —, sein Platz im Parametersatz und im Nachladen, und die
    /// Einträge der Seite mit Titel aus <c>MyResource</c> (mit Rückfall) und Excel-Kennung.
    ///
    /// <para><b>Rahmen</b> wie <see cref="BerichtsvorlagenHuelleTests"/>: die Standardvorlage als Kopie
    /// aus dem Repositorium in einem Temp-Ordner, Pfade und Einstellungen hereingereicht, die
    /// Konfiguration der Gruppe 1040 in einer Kopie der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BerichtsvorlagenHaekchenHuelleTests : IDisposable
    {
        private const int GRUPPE = 1040;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-haekchen");
        private readonly string _quellen;
        private readonly BerichtsvorlagenCtrl _vorlagen;
        private readonly byte[] _standard;

        public BerichtsvorlagenHaekchenHuelleTests()
        {
            string dokumente = Directory.CreateDirectory(Path.Combine(_wurzel, "Dokumente")).FullName;
            string app = Directory.CreateDirectory(Path.Combine(_wurzel, "App", "Vorlagen")).FullName;
            _quellen = Directory.CreateDirectory(Path.Combine(_wurzel, "Quellen")).FullName;
            _standard = Repovorlage(BerichtsvorlageDateiWacheTests.STANDARD);
            if (_standard != null) File.WriteAllBytes(Path.Combine(app, BerichtsvorlagenCtrl.DATEI_STANDARD), _standard);
            _vorlagen = new BerichtsvorlagenCtrl(new Probepfade(dokumente, app), new FluechtigeEinstellungen(), () => "Probe GmbH");
        }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_wurzel);
        }

        private static readonly string[] ALLE = BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel).ToArray();

        // =====================================================================
        //  Die Regel des Kapitelstands
        // =====================================================================

        /// <summary>
        /// Führt die Vorlage Kapitel, ist gesperrt, was keines ihrer Kapitel einsetzt — in der Folge des
        /// Katalogs; führt sie alle (<c>{{bericht.inhalt}}</c>), ist nichts gesperrt.
        /// </summary>
        [Fact]
        public void Mit_Kapiteln_ist_gesperrt_was_kein_Kapitel_der_Vorlage_einsetzt()
        {
            Kapitelstand teil = BerichtsvorlagenGaben.Kapitel(true, 3, true,
                new[] { BerichtsKonfiguration.B_WIRTSCHAFT, BerichtsKonfiguration.B_PROJEKT });
            Assert.NotNull(teil);
            Assert.False(teil.InhaltAusVorlage);
            Assert.Equal(ALLE.Where(s => s != BerichtsKonfiguration.B_WIRTSCHAFT && s != BerichtsKonfiguration.B_PROJEKT),
                         teil.NichtEnthalten);

            Kapitelstand alle = BerichtsvorlagenGaben.Kapitel(true, 1, true, ALLE);
            Assert.Empty(alle.NichtEnthalten);
            Assert.False(alle.InhaltAusVorlage);
        }

        /// <summary>
        /// Ohne Kapitel bestimmt die Vorlage den Inhalt allein: Kein Baustein ist geführt — auch keiner,
        /// den ein Einzelplatzhalter berührt —, und die Seite zeigt die leise Zeile.
        /// </summary>
        [Fact]
        public void Ohne_Kapitel_fuehrt_die_Vorlage_keinen_Baustein()
        {
            Kapitelstand k = BerichtsvorlagenGaben.Kapitel(true, 2, false, new[] { BerichtsKonfiguration.B_WIRTSCHAFT });
            Assert.NotNull(k);
            Assert.True(k.InhaltAusVorlage);
            Assert.Equal(ALLE, k.NichtEnthalten);
        }

        /// <summary>
        /// Keine Sperre, wo der Lauf die Häkchen ohnehin nimmt: ohne Befund, bei einer nicht lesbaren
        /// Vorlage (Rückfall auf die Standardvorlage) und bei einer Vorlage ohne Platzhalter (der
        /// Bericht kommt an ihr Ende).
        /// </summary>
        [Fact]
        public void Ohne_Befund_unlesbar_oder_ohne_Platzhalter_ist_jeder_Eintrag_frei()
        {
            Assert.Null(BerichtsvorlagenGaben.Kapitel(null));
            Assert.Null(BerichtsvorlagenGaben.Kapitel(false, 0, false, null));
            Assert.Null(BerichtsvorlagenGaben.Kapitel(false, 4, true, ALLE));
            Assert.Null(BerichtsvorlagenGaben.Kapitel(true, 0, false, null));
        }

        // =====================================================================
        //  Über echte Vorlagen: Stand, Nachladen, Parametersatz
        // =====================================================================

        /// <summary>
        /// Die Standardvorlage führt alle Kapitel (<c>{{bericht.inhalt}}</c>): Der Kapitelstand nennt
        /// keinen Baustein und steht im Parametersatz der Seite als <c>[Parameter] Kapitelstand</c>.
        /// </summary>
        [Fact]
        public void Die_Standardvorlage_fuehrt_alle_Kapitel_und_der_Stand_steht_im_Parametersatz()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();

            BerichtsvorlagenGaben gruppe = Gruppe();
            Vorlagenstand stand = gruppe.Stand();
            Assert.NotNull(stand.Kapitelstand);
            Assert.Empty(stand.Kapitelstand.NichtEnthalten);
            Assert.False(stand.Kapitelstand.InhaltAusVorlage);

            var gaben = new Dictionary<string, object>();
            gruppe.Belegen(gaben);
            Assert.True(gaben.ContainsKey("Kapitelstand"), "Es fehlt Kapitelstand");
            PropertyInfo parameter = typeof(BerichtSeite).GetProperty("Kapitelstand");
            Assert.NotNull(parameter);
            Assert.NotNull(parameter.GetCustomAttribute<ParameterAttribute>());
            Assert.IsType<Kapitelstand>(gaben["Kapitelstand"]);
        }

        /// <summary>
        /// Der Wechsel auf eine Vorlage mit Einzelplatzhaltern bringt beim Nachladen den Stand „ohne
        /// Kapitel" (alles gesperrt, die leise Zeile), der auf eine mit <c>{{bericht.inhalt}}</c> einen
        /// freien, der auf eine ohne Platzhalter keinen — dann gilt die Liste wie bisher.
        /// </summary>
        [Fact]
        public async Task Das_Nachladen_folgt_der_gewaehlten_Vorlage()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Einzeln.docx", Probevorlagen.AusAbsaetzen("Angebot für {{projekt.kunde}}", "Stand {{bericht.datum}}"));
            Hinzu("Sammel.docx", Probevorlagen.AusAbsaetzen("Einleitung", "{{bericht.inhalt}}"));
            Hinzu("Leer.docx", Probevorlagen.AusAbsaetzen("Nur Text ohne Platzhalter"));
            BerichtsvorlagenGaben gruppe = Gruppe();
            Vorlagenstand stand = gruppe.Stand();

            await gruppe.VorlageGewaehlt(Id(stand, "Einzeln"));
            Kapitelstand einzeln = gruppe.Stand().Kapitelstand;
            Assert.NotNull(einzeln);
            Assert.True(einzeln.InhaltAusVorlage);
            Assert.Equal(ALLE, einzeln.NichtEnthalten);

            await gruppe.VorlageGewaehlt(Id(stand, "Sammel"));
            Kapitelstand sammel = gruppe.Stand().Kapitelstand;
            Assert.NotNull(sammel);
            Assert.False(sammel.InhaltAusVorlage);
            Assert.Empty(sammel.NichtEnthalten);

            await gruppe.VorlageGewaehlt(Id(stand, "Leer"));
            Assert.Null(gruppe.Stand().Kapitelstand);

            var gaben = new Dictionary<string, object>();
            gruppe.Belegen(gaben);
            Assert.False(gaben.ContainsKey("Kapitelstand"));   // null bleibt weg - die Seite hat ihre Vorgabe
        }

        // =====================================================================
        //  Die Einträge: Titel aus MyResource, Excel-Kennung
        // =====================================================================

        /// <summary>
        /// Der Titel eines Bausteins ist die Ressource <c>BK_BER_BAUSTEIN_&lt;SCHLÜSSEL&gt;</c>; fehlt sie,
        /// ist sie leer oder wirft das Lesen, gilt der Titel des Katalogs.
        /// </summary>
        [Fact]
        public void Der_Titel_kommt_aus_der_Ressource_mit_dem_Katalogtitel_als_Rueckfall()
        {
            var gefragt = new List<string>();
            string Lies(string schluessel)
            {
                gefragt.Add(schluessel);
                if (schluessel == "BK_BER_BAUSTEIN_WIRTSCHAFTLICHKEIT") return "Wirtschaftliche Bewertung";
                if (schluessel == "BK_BER_BAUSTEIN_ANHANG") return "   ";
                if (schluessel == "BK_BER_BAUSTEIN_DECKBLATT") throw new InvalidOperationException("kaputt");
                return null;
            }

            foreach (BerichtsKonfiguration.BausteinDef b in BerichtsKonfiguration.AlleBausteine)
            {
                string titel = BerichtSeiteGaben.Bausteintitel(b, Lies);
                Assert.Equal(b.Schluessel == BerichtsKonfiguration.B_WIRTSCHAFT ? "Wirtschaftliche Bewertung" : b.Titel, titel);
            }

            Assert.Equal(new[]
            {
                "BK_BER_BAUSTEIN_DECKBLATT", "BK_BER_BAUSTEIN_INHALTSVERZEICHNIS", "BK_BER_BAUSTEIN_PROJEKTBESCHREIBUNG",
                "BK_BER_BAUSTEIN_KOMPONENTEN", "BK_BER_BAUSTEIN_ERGEBNISSE", "BK_BER_BAUSTEIN_VERGLEICH",
                "BK_BER_BAUSTEIN_WIRTSCHAFTLICHKEIT", "BK_BER_BAUSTEIN_ANHANG"
            }, gefragt);

            Assert.Equal("Wirtschaftliche Bewertung", BerichtSeiteGaben.Bausteintitel(BerichtsKonfiguration.B_WIRTSCHAFT, Lies));
            Assert.Equal("gibt-es-nicht", BerichtSeiteGaben.Bausteintitel("gibt-es-nicht", Lies));
        }

        /// <summary>
        /// Ohne Prüfstand liest der Titel <c>MyResource</c>: mit Schlüssel dessen Text, ohne ihn der
        /// Titel des Katalogs — beides gilt vor und nach dem Anlegen der Schlüssel.
        /// </summary>
        [Fact]
        public void Ohne_Pruefstand_liest_der_Titel_MyResource()
        {
            foreach (BerichtsKonfiguration.BausteinDef b in BerichtsKonfiguration.AlleBausteine)
            {
                string text = R.ResourceManager.GetString(BerichtSeiteGaben.BausteintitelSchluessel(b.Schluessel));
                Assert.Equal(string.IsNullOrWhiteSpace(text) ? b.Titel : text, BerichtSeiteGaben.Bausteintitel(b));
            }
        }

        /// <summary>
        /// <c>Laden</c> liefert je Baustein des Katalogs einen Eintrag mit Titel aus der Ressource und
        /// der Excel-Kennung (<c>NurWord</c> = nein).
        /// </summary>
        [Fact]
        public void Laden_liefert_die_Eintraege_mit_Titel_und_Excel_Kennung()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();

            var seite = new BerichtSeiteGaben(GRUPPE, "Stamm", _vorlagen, new Berichtsvorlagenwege());
            var laden = (Func<BerichtStand>)seite.Gaben()["Laden"];
            BerichtStand stand = laden();

            Assert.Equal(ALLE, stand.Bausteine.Select(b => b.Schluessel));
            foreach (BerichtsKonfiguration.BausteinDef b in BerichtsKonfiguration.AlleBausteine)
            {
                BausteinZeile z = stand.Bausteine.Single(x => x.Schluessel == b.Schluessel);
                Assert.Equal(BerichtSeiteGaben.Bausteintitel(b), z.Titel);
                Assert.Equal(!b.NurWord, z.InExcel);
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private BerichtsvorlagenGaben Gruppe()
        {
            return new BerichtsvorlagenGaben(GRUPPE, new BerichtCtrl(_vorlagen), _vorlagen, new Berichtsvorlagenwege());
        }

        /// <summary>Eine frische Konfiguration der Gruppe: Word, alle Bausteine des Neuzustands.</summary>
        private void Konfig()
        {
            BerichtsKonfiguration k = BerichtsKonfiguration.Standard();
            k.Ausgabe = "Word";
            k.ZielOrdner = _quellen;
            Assert.True(new BerichtCtrl(_vorlagen).Speichere(GRUPPE, k));
        }

        private void Hinzu(string name, byte[] inhalt)
        {
            string quelle = Path.Combine(_quellen, name);
            File.WriteAllBytes(quelle, inhalt);
            Vorlagenergebnis r = _vorlagen.Hinzufuegen(quelle);
            Assert.True(r.Erfolg, r.Meldung);
        }

        private static int Id(Vorlagenstand stand, string text)
        {
            return Assert.Single(stand.Vorlagen, z => z.Text == text).Id;
        }

        private static byte[] Repovorlage(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            Assert.True(File.Exists(pfad), "Die Vorlage fehlt: " + pfad);
            return File.ReadAllBytes(pfad);
        }

        /// <summary>Pfade, deren Dokumente- und Auslieferungsordner der Fall bestimmt.</summary>
        private sealed class Probepfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Probepfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }
    }
}
