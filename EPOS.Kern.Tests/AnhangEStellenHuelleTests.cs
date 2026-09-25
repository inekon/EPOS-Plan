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
    /// <b>Die Hülle der Anhang-E-Stellen</b> (Etappe BV-E2; Konzept Berichtsvorlagen 9.5, 11 Nr. 3): EIN
    /// Codeweg für die Spalte „Stelle" — die Kapitelstellen der gewählten Vorlage aus dem Kern
    /// (<see cref="BerichtCtrl.KapitelstellenDerVorlage"/>, im Konstruktor eingehängt), die Stelle je Punkt
    /// aus der Checkliste des Kerns (<see cref="AnhangECheckliste.Punkte(ChecklistenLage, IReadOnlyDictionary{string, string})"/>),
    /// beide Sprachen; mit dem echten Kern die Standardvorlage (Deckblatt aus Platzhaltern, Kapitel unter
    /// ihren Kapitelköpfen) und eine eigene Vorlage ohne Wirtschaftlichkeit („nicht im Bericht"); die
    /// Häkchen des zweiten Einstiegs; ohne Delegat oder wenn der Kern wirft oder schweigt die
    /// Standardvorlage; der Weg über die Rahmenhülle an die Wirtschaftlichkeitsseite.
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

        /// <summary>Kapitelstellen einer erfundenen Vorlage, wie der Kern sie meldet: Überschrift oder <c>null</c>.</summary>
        private static Dictionary<string, string> Kapitel() => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [BerichtsKonfiguration.B_DECKBLATT] = null,
            [BerichtsKonfiguration.B_PROJEKT] = "Das Projekt",
            [BerichtsKonfiguration.B_KOMPONENTEN] = null,
            [BerichtsKonfiguration.B_ERGEBNISSE] = null,
            [BerichtsKonfiguration.B_VERGLEICH] = null,
            [BerichtsKonfiguration.B_WIRTSCHAFT] = "5 Wirtschaftliche Bewertung"
            // B_ANHANG fehlt ganz - wie null: nicht im Bericht
        };

        // =====================================================================
        //  Die Stelle kommt aus der Checkliste des Kerns
        // =====================================================================

        /// <summary>
        /// Je Punkt der Checkliste genau die Spalte „Stelle", die die Checkliste des Kerns zu diesen
        /// Kapitelstellen baut — kein zweiter Weg in der Hülle.
        /// </summary>
        [Fact]
        public void Die_Stelle_je_Punkt_ist_die_Spalte_der_Checkliste_des_Kerns()
        {
            IReadOnlyDictionary<string, string> stellen = BerichtsvorlagenGaben.Stellen(Kapitel());

            List<ChecklistenPunkt> punkte = AnhangECheckliste.Punkte(new ChecklistenLage(), Kapitel());
            Assert.Equal(AnhangECheckliste.ANZAHL, stellen.Count);
            Assert.All(punkte, p => Assert.Equal(p.Stelle, stellen[p.Nummer]));

            Assert.Equal("Wortbericht: „5 Wirtschaftliche Bewertung“ › „Kennzahlen im Szenario „Erwartet““ · " +
                         "Tabellenbericht: Blatt „Wirtschaftlichkeit“, Block „Erwartet“", stellen["1"]);
            Assert.Equal("Wortbericht: „Das Projekt“ · Tabellenbericht: Blatt „Übersicht“", stellen["0.2"]);
            Assert.Equal("Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Übersicht“", stellen["0.1"]);
            Assert.StartsWith("Wortbericht: nicht im Bericht · ", stellen["2a"]);
            Assert.StartsWith("Wortbericht: „5 Wirtschaftliche Bewertung“ · ", stellen["11"]);   // der Anhang fehlt
        }

        [Fact]
        public void Auf_Englisch_steht_die_Stelle_englisch()
        {
            using var englisch = new Kulturvorrichtung("en-US");
            IReadOnlyDictionary<string, string> stellen = BerichtsvorlagenGaben.Stellen(Kapitel());

            Assert.StartsWith("Word report: “5 Wirtschaftliche Bewertung” › ", stellen["1"]);
            Assert.StartsWith("Word report: not in the report · ", stellen["2a"]);
        }

        // =====================================================================
        //  Der Delegat des Kerns
        // =====================================================================

        /// <summary>Der Konstruktor hängt <see cref="BerichtCtrl.KapitelstellenDerVorlage"/> des hereingereichten Controllers ein.</summary>
        [Fact]
        public void Der_Delegat_ist_der_Kern()
        {
            var bericht = new BerichtCtrl(_vorlagen);
            var gruppe = new BerichtsvorlagenGaben(GRUPPE, bericht, _vorlagen, new Berichtsvorlagenwege());

            Assert.NotNull(gruppe.Kapitelstellen);
            Assert.Same(bericht, gruppe.Kapitelstellen.Target);
            Assert.Equal(nameof(BerichtCtrl.KapitelstellenDerVorlage), gruppe.Kapitelstellen.Method.Name);
        }

        /// <summary>Ohne Delegat gilt die Standardvorlage: keine Stellen, die Überlagerung nimmt ihre eigenen.</summary>
        [Fact]
        public void Ohne_Delegat_bezieht_sich_die_Ueberlagerung_auf_die_Standardvorlage()
        {
            var gruppe = new BerichtsvorlagenGaben(GRUPPE, new BerichtCtrl(_vorlagen), _vorlagen, new Berichtsvorlagenwege())
            {
                Kapitelstellen = null
            };

            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);
            Assert.Empty(stellen.Stellen);
        }

        /// <summary>
        /// Mit der Standardvorlage und dem echten Kern: Das Deckblatt aus Platzhaltern ist die Stelle
        /// „Deckblatt", die Kapitel stehen unter ihren Kapitelköpfen. Die Wirtschaftlichkeit steht im
        /// Bericht, obwohl ihr Häkchen im Neuzustand aus ist — die Häkchen des zweiten Einstiegs —, und
        /// die gespeicherte Konfiguration bleibt unberührt. Die leise Zeile bezieht sich auf die
        /// Standardvorlage.
        /// </summary>
        [Fact]
        public void Mit_der_Standardvorlage_nennt_der_Kern_Deckblatt_und_Kapitelkoepfe()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();

            AnhangEStellen stellen = Gruppe().AnhangEStellenDerVorlage();

            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);
            Assert.Equal(AnhangECheckliste.ANZAHL, stellen.Stellen.Count);
            Assert.Equal("Wortbericht: Deckblatt · Tabellenbericht: Blatt „Übersicht“", stellen.Stellen["0.1"]);
            Assert.Equal("Wortbericht: „Projektbeschreibung“, „Komponenten & Varianten“ · Tabellenbericht: Blatt „Übersicht“",
                         stellen.Stellen["0.2"]);
            Assert.StartsWith("Wortbericht: „Berechnungsergebnisse je Variante“, „Variantenvergleich“ · ", stellen.Stellen["2a"]);
            Assert.StartsWith("Wortbericht: „Wirtschaftlichkeit“ › „Kennzahlen im Szenario „Erwartet““ · ", stellen.Stellen["1"]);
            Assert.StartsWith("Wortbericht: Kapitel „Wirtschaftlichkeit“ und „Anhang“ · ", stellen.Stellen["11"]);

            Assert.False(new BerichtCtrl(_vorlagen).Lade(GRUPPE).IstAktiv(BerichtsKonfiguration.B_WIRTSCHAFT));
        }

        /// <summary>
        /// Eine eigene Vorlage ohne Kapitel Wirtschaftlichkeit mit dem echten Kern: Die Punkte der
        /// Wirtschaftlichkeit stehen „nicht im Bericht", die Projektbeschreibung unter ihrer eigenen
        /// Überschrift, die leise Zeile nennt die Vorlage. Fehlt danach ihre Datei, gilt die Standardvorlage.
        /// </summary>
        [Fact]
        public async Task Eine_eigene_Vorlage_ohne_Wirtschaftlichkeit_nennt_nicht_im_Bericht()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Kurzbericht.docx", Probevorlagen.AusAbsaetzen("Kurzbericht", "{{kapitel.projekt}}"));

            BerichtsvorlagenGaben gruppe = Gruppe();
            await gruppe.VorlageGewaehlt(Id(gruppe, "Kurzbericht"));
            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();

            Assert.Equal(Format(R.WIRT_AE_BEZUG_VORLAGE, "Kurzbericht"), stellen.Bezug);
            Assert.Equal("Die Stellen im Bericht nennen die Kapitel der Vorlage „Kurzbericht“.", stellen.Bezug);
            Assert.Equal("Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Wirtschaftlichkeit“, Block „Erwartet“",
                         stellen.Stellen["1"]);
            Assert.StartsWith("Wortbericht: nicht im Bericht · ", stellen.Stellen["8"]);
            Assert.Equal("Wortbericht: „Projektbeschreibung“ · Tabellenbericht: Blatt „Übersicht“", stellen.Stellen["0.2"]);
            Assert.Equal("Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Übersicht“", stellen.Stellen["0.1"]);

            File.Delete(_vorlagen.Liste().Single(e => e.Name == "Kurzbericht").Pfad);
            AnhangEStellen ersatz = gruppe.AnhangEStellenDerVorlage();
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, ersatz.Bezug);
            Assert.StartsWith("Wortbericht: „Wirtschaftlichkeit“ › ", ersatz.Stellen["1"]);
        }

        /// <summary>
        /// Gefragt wird der Kern mit der Konfiguration der Gruppe samt Vorlagenwahl, den gespeicherten
        /// Häkchen und der Wirtschaftlichkeit (zweiter Einstieg) und in der Sprache des Berichts; je Punkt
        /// steht, was die Checkliste des Kerns aus seiner Antwort macht.
        /// </summary>
        [Fact]
        public async Task Die_Huelle_fragt_mit_Vorlagenwahl_und_Haekchen_des_zweiten_Einstiegs()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Kurzbericht.docx", Probevorlagen.AusAbsaetzen("Kurzbericht", "{{bericht.inhalt}}"));

            BerichtsKonfiguration gefragt = null;
            bool? englisch = null;
            BerichtsvorlagenGaben gruppe = Gruppe((k, e) => { gefragt = k; englisch = e; return Kapitel(); });
            await gruppe.VorlageGewaehlt(Id(gruppe, "Kurzbericht"));

            AnhangEStellen stellen = gruppe.AnhangEStellenDerVorlage();

            Assert.NotNull(gefragt);
            Assert.Equal(BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN, gefragt.VorlageWordQuelle);
            Assert.Equal("Kurzbericht.docx", gefragt.VorlageWordDatei);
            Assert.True(gefragt.IstAktiv(BerichtsKonfiguration.B_WIRTSCHAFT));
            Assert.True(gefragt.IstAktiv(BerichtsKonfiguration.B_PROJEKT));
            Assert.Equal(BerichtTexte.Englisch, englisch);
            Assert.Equal(Format(R.WIRT_AE_BEZUG_VORLAGE, "Kurzbericht"), stellen.Bezug);
            Assert.Equal(BerichtsvorlagenGaben.Stellen(Kapitel()), stellen.Stellen);
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
            await gruppe.VorlageGewaehlt(Id(gruppe, "Kurzbericht"));
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
            Assert.Equal(R.WIRT_AE_BEZUG_STANDARD, stellen.Bezug);   // Projekt 1030: die Standardvorlage
            Assert.Equal(AnhangECheckliste.ANZAHL, stellen.Stellen.Count);

            Assert.False(new WirtschaftlichkeitSeiteGaben(1030, "").Gaben().ContainsKey("AnhangEStellenLaden"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private BerichtsvorlagenGaben Gruppe()
        {
            return new BerichtsvorlagenGaben(GRUPPE, new BerichtCtrl(_vorlagen), _vorlagen, new Berichtsvorlagenwege());
        }

        private BerichtsvorlagenGaben Gruppe(Func<BerichtsKonfiguration, bool, IReadOnlyDictionary<string, string>> kapitelstellen)
        {
            BerichtsvorlagenGaben gruppe = Gruppe();
            gruppe.Kapitelstellen = kapitelstellen;
            return gruppe;
        }

        private static int Id(BerichtsvorlagenGaben gruppe, string text)
        {
            return Assert.Single(gruppe.Stand().Vorlagen, z => z.Text == text).Id;
        }

        /// <summary>Eine frische Konfiguration der Gruppe: Word, die Häkchen des Neuzustands (ohne Wirtschaftlichkeit).</summary>
        private void Konfig()
        {
            BerichtsKonfiguration k = BerichtsKonfiguration.Standard();
            k.Ausgabe = "Word";
            k.ZielOrdner = _quellen;
            Assert.False(k.IstAktiv(BerichtsKonfiguration.B_WIRTSCHAFT));
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
