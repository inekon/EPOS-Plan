using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Nachzüge der ausführlichen Vorlage</b> (Etappe BV-E9, Befunde aus BV-E8-4): Der Kapitelkopf
    /// <c>{{text.kapitel_&lt;name&gt;}}</c> ist in Prüfer und Engine die Stelle seines Kapitels, wenn die Vorlage
    /// <c>{{kapitel.&lt;name&gt;}}</c> nicht führt (Spalte „Stelle“ der Anhang-E-Checkliste); <c>gebaeude.baualtersklasse</c>
    /// liefert den Klartext wie das Kapitel; die Eigenschaftstafeln der Projektbeschreibung sind im englischen Bericht
    /// englisch beschriftet. Die Überschrift vor einem entfallenden Block prüft <c>WordVorlagenBloeckeTests</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtsvorlagenNachzuegeE9Tests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e9n-");

        public BerichtsvorlagenNachzuegeE9Tests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  Anhang-E-Stelle aus dem Kapitelkopf
        // =====================================================================

        /// <summary>
        /// Eine Vorlage aus Einzelelementen: Der Kapitelkopf <c>{{text.kapitel_projekt}}</c> nennt in Prüfer und Engine die
        /// Stelle des Kapitels „Projekt“ — der aufgelöste Kopftext —, ohne Häkchen bleibt sie in der Engine leer; ein Kapitel
        /// ohne Kopf und ohne Platzhalter hat keine Stelle.
        /// </summary>
        [Fact]
        public void Kapitelkopf_ist_die_Stelle_seines_Kapitels()
        {
            byte[] vorlage = Probevorlagen.AusAbsaetzen("{{bericht.titel}}", "{{text.kapitel_projekt}}", "{{projekt.name}}");
            string kopf = Vorlagenfeldkatalog.LoeseImText("{{text.kapitel_projekt}}",
                                                           Berichtswerte.Aus(new BerichtsDaten(), null, false, null));
            Assert.False(string.IsNullOrWhiteSpace(kopf));

            Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell, new Pruefkontext());
            Assert.Equal(kopf, befund.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Null(befund.Kapitelstellen[BerichtsKonfiguration.B_VERGLEICH]);

            string ziel = Path.Combine(_ordner, "kopf.docx");
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(Berichtsdatenproben.Gruppendaten(2),
                Berichtsdatenproben.VolleKonfiguration(), vorlage, new Erstellerangaben(), ziel);
            Assert.Equal(kopf, e.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
            Assert.Null(e.Kapitelstellen[BerichtsKonfiguration.B_VERGLEICH]);

            var ohneProjekt = new BerichtsKonfiguration();
            ohneProjekt.AktiveBausteine.Add(BerichtsKonfiguration.B_VERGLEICH);
            Fuellergebnis ohne = new WordBerichtGenerator().ErzeugeMitVorlage(Berichtsdatenproben.Gruppendaten(2), ohneProjekt,
                vorlage, new Erstellerangaben(), Path.Combine(_ordner, "ohne.docx"));
            Assert.Null(ohne.Kapitelstellen[BerichtsKonfiguration.B_PROJEKT]);
        }

        /// <summary>
        /// Die ausführliche Vorlage (je Sprache) führt jedes Kapitel über seinen Kapitelkopf: Der Prüfer nennt für jedes Kapitel
        /// mit Kopf die Stelle, und die Anhang-E-Warnung „Kapitel nicht in der Vorlage“ bleibt aus.
        /// </summary>
        [Theory]
        [InlineData(AusfuehrlichRundlaufTests.AUSFUEHRLICH, false)]
        [InlineData(AusfuehrlichRundlaufTests.AUSFUEHRLICH_EN, true)]
        public void Die_ausfuehrliche_Vorlage_nennt_jede_Kapitelstelle(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            if (pfad == null || !File.Exists(pfad)) return;
            Pruefbefund befund = Vorlagenpruefer.Pruefe(File.ReadAllBytes(pfad), Pruefstufe.Voll,
                Pruefkontext.Aus(Berichtsdatenproben.VolleKonfiguration(), englisch, 1, datei));
            foreach (KeyValuePair<string, string> s in befund.Kapitelstellen) _ausgabe.WriteLine(s.Key + ": " + (s.Value ?? "—"));
            foreach (Berichtskapitel k in Berichtskapitel.Alle.Where(k => k.Kopfschluessel != null))
                Assert.False(string.IsNullOrWhiteSpace(befund.Kapitelstellen[k.Stellenschluessel]), k.Name);
            Assert.Empty(Probevorlagen.Mit(befund, "VF_PRUEF_ANHANG_E_STELLE"));
        }

        // =====================================================================
        //  Baualtersklasse als Klartext
        // =====================================================================

        /// <summary>
        /// <c>gebaeude.baualtersklasse</c> zeigt den Bauzeitraum wie das Kapitel „Projekt“ („1979 bis 1983“ bzw. „1979 to
        /// 1983“ in der Sprache des Berichts), nicht den gespeicherten Kennbuchstaben; ohne Klasse leer.
        /// </summary>
        [Fact]
        public void Baualtersklasse_ist_der_Klartext()
        {
            var t = new DataTable();
            t.Columns.Add("ID", typeof(long));
            t.Columns.Add("Gebaeudename", typeof(string));
            t.Columns.Add("Baualtersklasse", typeof(string));
            t.Rows.Add(1L, "Haus 1", "G");
            t.Rows.Add(2L, "Haus 2", "");
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde("gebaeude.baualtersklasse");

            Berichtswerte de = Berichtswerte.Aus(Berichtsdatenproben.Gruppendaten(1), null, false, null);
            Berichtswerte en = Berichtswerte.Aus(Berichtsdatenproben.Gruppendaten(1), null, true, null);
            Assert.Equal("1979 bis 1983", Vorlagenfeldkatalog.Loese(f, de.MitGebaeude(t.Rows[0]), null).Text);
            Assert.Equal("1979 to 1983", Vorlagenfeldkatalog.Loese(f, en.MitGebaeude(t.Rows[0]), null).Text);
            Assert.True(Vorlagenfeldkatalog.Loese(f, de.MitGebaeude(t.Rows[1]), null).IstLeer);
            Assert.Equal(Gebaeudeklassen.Text("G", BerichtTexte.KulturFuer(false)), "1979 bis 1983");
        }

        // =====================================================================
        //  Englische Beschriftungen der Projektbeschreibung
        // =====================================================================

        /// <summary>
        /// Die Zeilen der Eigenschaftstafel „Energiebedarf (Simulationsergebnis Stamm)“ tragen im englischen Bericht die
        /// englische Beschriftung ihrer Kennzahl — dieselbe, die die ausführliche Vorlage über <c>kennzahl.&lt;k&gt;.beschriftung</c>
        /// zeigt; die Tafel je Gebäude die Beschriftungen der ausführlichen Vorlage. Deutsch bleibt alles, wie es war.
        /// </summary>
        [Fact]
        public void Eigenschaftstafeln_der_Projektbeschreibung_sind_englisch_beschriftet()
        {
            var kennzahlen = KennzahlenKatalog.Alle().ToDictionary(k => k.Schluessel, StringComparer.Ordinal);
            foreach ((string de, string schluessel) in new[]
                     {
                         ("Wärmebedarf gesamt", "energie.waermebedarf"), ("davon Heizung", "energie.waermebedarf_heizung"),
                         ("davon Brauchwasser", "energie.waermebedarf_brauchwasser"), ("davon Prozesswärme", "energie.waermebedarf_prozess"),
                         ("Wärmelast max.", "energie.waermelast"), ("Strombedarf gesamt", "energie.strombedarf"),
                         ("Strombedarf max.", "energie.strommax"),
                     })
            {
                Assert.Equal(kennzahlen[schluessel].LabelDe, de);
                Assert.Equal(kennzahlen[schluessel].LabelEn, BerichtTexte.T(de, true));
                Assert.Equal(de, BerichtTexte.T(de, false));
            }
            foreach ((string de, string en) in new[]
                     {
                         ("Gebäudeart", "Building type"), ("Baualtersklasse", "Construction period"),
                         ("Wohn-/Nutzfläche", "Living/usable area"), ("Bewohner/Nutzer", "Occupants/users"),
                         ("Wärmebedarf", "Heat demand"), ("spez. Wärmeverbrauch", "Specific heat consumption"),
                         ("Warmwasserbedarf", "Hot water demand"), ("Raumhöhe", "Room height"),
                     })
                Assert.Equal(en, BerichtTexte.T(de, true));
        }
    }
}
