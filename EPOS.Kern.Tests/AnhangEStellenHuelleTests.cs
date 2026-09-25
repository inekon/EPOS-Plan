using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <b>Die Hülle der Anhang-E-Stellen</b> (Etappe BV-E2, Teil H; Konzept Berichtsvorlagen 9.5, 11 Nr. 3):
    /// die Zuordnung Punkt → Kapitel für jeden Punkt der Checkliste, die Stelle aus den Kapitelstellen der
    /// Vorlage (Überschrift, Titel eines Kapitels ohne Überschrift, „nicht im Bericht"), beide Sprachen,
    /// der Delegat <see cref="BerichtsvorlagenGaben.Kapitelstellen"/> — ohne ihn, mit der Standardvorlage,
    /// mit einer eigenen Vorlage, wenn der Kern wirft oder schweigt — und der Weg über die Rahmenhülle an
    /// die Wirtschaftlichkeitsseite.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnhangEStellenHuelleTests : IDisposable
    {
        private const int GRUPPE = 1040;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-anhang-e");
        private readonly string _quellen;
        private readonly BerichtsvorlagenCtrl _vorlagen;
        private readonly byte[] _standard;

        public AnhangEStellenHuelleTests()
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

        /// <summary>Kapitelstellen einer erfundenen Vorlage: mit, ohne und ganz ohne Überschrift.</summary>
        private static Dictionary<string, string> Kapitel() => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [BerichtsKonfiguration.B_DECKBLATT] = "",
            [BerichtsKonfiguration.B_PROJEKT] = " Das Projekt ",
            [BerichtsKonfiguration.B_KOMPONENTEN] = null,
            [BerichtsKonfiguration.B_ERGEBNISSE] = null,
            [BerichtsKonfiguration.B_VERGLEICH] = null,
            [BerichtsKonfiguration.B_WIRTSCHAFT] = "5 Wirtschaftliche Bewertung"
            // B_ANHANG fehlt ganz - wie null: nicht im Bericht
        };

        private static string Titel(string baustein) => "T:" + baustein;

        // =====================================================================
        //  Die Zuordnung und die Stelle
        // =====================================================================

        [Fact]
        public void Jeder_Punkt_der_Checkliste_hat_eine_Zuordnung_auf_Bausteine_des_Katalogs()
        {
            List<string> punkte = AnhangECheckliste.Punkte(new ChecklistenLage()).Select(p => p.Nummer).ToList();
            Assert.Equal(AnhangECheckliste.ANZAHL, punkte.Count);
            Assert.Equal(punkte.OrderBy(p => p, StringComparer.Ordinal), AnhangEKapitel.Zuordnung.Keys.OrderBy(p => p, StringComparer.Ordinal));

            var katalog = new HashSet<string>(BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel), StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> z in AnhangEKapitel.Zuordnung)
            {
                Assert.NotEmpty(z.Value);
                Assert.All(z.Value, b => Assert.Contains(b, katalog));
            }
        }

        [Fact]
        public void Die_Stelle_nennt_die_Ueberschriften_der_Vorlage_und_was_fehlt()
        {
            IReadOnlyDictionary<string, string> stellen = AnhangEKapitel.Stellen(Kapitel(), Titel);

            Assert.Equal("„5 Wirtschaftliche Bewertung“", stellen["1"]);
            Assert.Equal("„5 Wirtschaftliche Bewertung“", stellen["8"]);
            Assert.Equal("„T:deckblatt“", stellen["0.1"]);                                     // ohne Überschrift: der Titel
            Assert.Equal("„Das Projekt“, „T:komponenten“ nicht im Bericht", stellen["0.2"]);
            Assert.Equal("nicht im Bericht", stellen["2a"]);                                   // keines der Kapitel
            Assert.Equal("„5 Wirtschaftliche Bewertung“, „T:anhang“ nicht im Bericht", stellen["11"]);
            Assert.Equal(AnhangECheckliste.ANZAHL, stellen.Count);

            // Ohne Prüfstand stehen die Titel der Berichtsseite da.
            Assert.Equal(Format(R.WIRT_AE_STELLE_FEHLT, BerichtSeiteGaben.Bausteintitel(BerichtsKonfiguration.B_ANHANG)),
                         AnhangEKapitel.Stelle(new[] { BerichtsKonfiguration.B_ANHANG, BerichtsKonfiguration.B_WIRTSCHAFT },
                                               Kapitel()).Split(new[] { ", " }, StringSplitOptions.None)[0]);
        }

        [Fact]
        public void Auf_Englisch_stehen_die_englischen_Anfuehrungszeichen()
        {
            using var englisch = new Kulturvorrichtung("en-US");
            IReadOnlyDictionary<string, string> stellen = AnhangEKapitel.Stellen(Kapitel(), Titel);

            Assert.Equal("“5 Wirtschaftliche Bewertung”", stellen["1"]);
            Assert.Equal("not in the report", stellen["2a"]);
            Assert.Equal("“5 Wirtschaftliche Bewertung”, “T:anhang” not in the report", stellen["11"]);
        }

        // =====================================================================
        //  Der Delegat des Kerns
        // =====================================================================

        /// <summary>Ohne Delegat — der Stand vor dem Zusammenführen — gilt die Standardvorlage.</summary>
        [Fact]
        public void Ohne_Delegat_bezieht_sich_die_Ueberlagerung_auf_die_Standardvorlage()
        {
            var gruppe = new BerichtsvorlagenGaben(GRUPPE, new BerichtCtrl(_vorlagen), _vorlagen, new Berichtsvorlagenwege());
            Assert.Null(gruppe.Kapitelstellen);

            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);
            Assert.Empty(stellen.Stellen);
        }

        /// <summary>Mit Delegat, aber ohne eigene Vorlage (die Standardvorlage ist gewählt) fragt die Hülle den Kern nicht.</summary>
        [Fact]
        public void Mit_der_Standardvorlage_bleibt_es_bei_der_Standardvorlage()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();

            int gefragt = 0;
            BerichtsvorlagenGaben gruppe = Gruppe((k, e) => { gefragt++; return Kapitel(); });

            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();
            Assert.Equal(0, gefragt);
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);
            Assert.Empty(stellen.Stellen);
        }

        /// <summary>
        /// Mit einer eigenen Vorlage fragt die Hülle den Kern mit der Konfiguration der Gruppe (samt
        /// Abweichung) und der Sprache des Berichts; die leise Zeile nennt die Vorlage, je Punkt steht
        /// die Stelle ihrer Kapitel.
        /// </summary>
        [Fact]
        public async Task Mit_eigener_Vorlage_nennt_die_Ueberlagerung_die_Kapitel_der_Vorlage()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Kurzbericht.docx", Probevorlagen.AusAbsaetzen("Kurzbericht", "{{bericht.inhalt}}"));

            BerichtsKonfiguration gefragt = null;
            bool? englisch = null;
            BerichtsvorlagenGaben gruppe = Gruppe((k, e) => { gefragt = k; englisch = e; return Kapitel(); });
            await gruppe.VorlageGewaehlt(Assert.Single(gruppe.Stand().Vorlagen, z => z.Text == "Kurzbericht").Id);

            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();

            Assert.NotNull(gefragt);
            Assert.Equal(BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN, gefragt.VorlageWordQuelle);
            Assert.Equal("Kurzbericht.docx", gefragt.VorlageWordDatei);
            Assert.Equal(BerichtTexte.Englisch, englisch);
            Assert.Equal(Format(R.WIRT_AE_BEZUG_VORLAGE, "Kurzbericht"), stellen.Bezug);
            Assert.Equal("„5 Wirtschaftliche Bewertung“", stellen.Stellen["1"]);
            Assert.Equal("nicht im Bericht", stellen.Stellen["2a"]);
            Assert.Equal(AnhangECheckliste.ANZAHL, stellen.Stellen.Count);
        }

        /// <summary>Wirft der Kern oder antwortet er nicht, gilt die Standardvorlage — die Überlagerung geht trotzdem auf.</summary>
        [Fact]
        public async Task Wirft_der_Kern_oder_schweigt_er_gilt_die_Standardvorlage()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Kurzbericht.docx", Probevorlagen.AusAbsaetzen("Kurzbericht", "{{bericht.inhalt}}"));

            BerichtsvorlagenGaben gruppe = Gruppe((k, e) => throw new InvalidOperationException("kaputt"));
            await gruppe.VorlageGewaehlt(Assert.Single(gruppe.Stand().Vorlagen, z => z.Text == "Kurzbericht").Id);
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, gruppe.AnhangEStellenDerVorlage().Bezug);

            gruppe.Kapitelstellen = (k, e) => null;
            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);
            Assert.Empty(stellen.Stellen);
        }

        // =====================================================================
        //  Der Weg an die Wirtschaftlichkeitsseite
        // =====================================================================

        /// <summary>
        /// Im Rahmen trägt die Wirtschaftlichkeitsseite den Delegaten <c>AnhangEStellenLaden</c> (ein
        /// <c>[Parameter]</c> der Seite); er fragt die Berichtshülle derselben Gruppe. Allein, ohne Rahmen,
        /// fehlt der Schlüssel.
        /// </summary>
        [Fact]
        public void Die_Rahmenhuelle_reicht_die_Stellen_an_die_Wirtschaftlichkeitsseite()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(1030, "");
            var seiten = (Func<string, IReadOnlyDictionary<string, object>>)huelle.Gaben()["SeitenGaben"];
            IReadOnlyDictionary<string, object> wirtschaft = seiten(BerichteKostenSeite.SEITE_WIRTSCHAFT);

            Assert.True(wirtschaft.ContainsKey("AnhangEStellenLaden"));
            PropertyInfo parameter = typeof(WirtschaftlichkeitSeite).GetProperty("AnhangEStellenLaden");
            Assert.NotNull(parameter);
            Assert.NotNull(parameter.GetCustomAttribute<ParameterAttribute>());
            Assert.True(parameter.PropertyType.IsInstanceOfType(wirtschaft["AnhangEStellenLaden"]));

            AnhangEStellen stellen = ((Func<AnhangEStellen>)wirtschaft["AnhangEStellenLaden"])();
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);   // vor dem Einhängen des Kerns

            Assert.False(new WirtschaftlichkeitSeiteGaben(1030, "").Gaben().ContainsKey("AnhangEStellenLaden"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private BerichtsvorlagenGaben Gruppe(Func<BerichtsKonfiguration, bool, IReadOnlyDictionary<string, string>> kapitelstellen)
        {
            return new BerichtsvorlagenGaben(GRUPPE, new BerichtCtrl(_vorlagen), _vorlagen, new Berichtsvorlagenwege())
            {
                Kapitelstellen = kapitelstellen
            };
        }

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

        private static string Format(string muster, string wert)
        {
            return string.Format(CultureInfo.CurrentCulture, muster, wert);
        }

        private static byte[] Repovorlage(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            Assert.True(File.Exists(pfad), "Die Vorlage fehlt: " + pfad);
            return File.ReadAllBytes(pfad);
        }

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
