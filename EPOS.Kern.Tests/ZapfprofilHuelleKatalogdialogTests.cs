using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Katalogdialogs „Brauchwasser-Nutzungsarten"</b>
    /// (<c>EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Katalogdialog.cs</c>; Umsetzungskonzept
    /// Zapfprofilgenerator 5.4, 5.5; Stufe Z4, Gruppe 3): Parametersatz gegen die Komponente, Liste
    /// mit übersetzten Wertwörtern, Stammblatt mit Sperrgründen und Vorschau, Editor in den drei
    /// Modi samt Normierung der Monatsfaktoren, Löschen mit benannter Sperre, der Katalogimport über
    /// die Hülle und die Gründe jedes Ausgangs in beiden Sprachen — mit leerer Tww-Datenbank
    /// (<see cref="TwwTestdatenbank"/>) bzw. der Arbeitskopie der Testdatenbank. Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleKatalogdialogTests : IDisposable
    {
        private const string VERSION = "T1";
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static string Text(string k, CultureInfo c) => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(k, c) ?? "";

        [Fact]
        public void Der_Parametersatz_trifft_die_Parameter_der_Komponente()
        {
            using var db = new TwwTestdatenbank();
            IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.KatalogGaben();
            var parameter = typeof(TwwNutzungsartAdminDialog).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null).ToDictionary(p => p.Name, p => p.PropertyType);
            foreach (KeyValuePair<string, object> g in gaben)
            {
                Assert.True(parameter.ContainsKey(g.Key), "Kein [Parameter] " + g.Key);
                Assert.True(parameter[g.Key].IsInstanceOfType(g.Value), g.Key + ": " + g.Value.GetType().Name);
            }
            // DREIZEHN seit der Stufe Z4b: "TyptagGaben" bringt den Dialog der eingespielten
            // VDI-4655-Typtage als Ueberlagerung des Katalogdialogs herein. VIERZEHN mit ZU26:
            // "OrdnerwahlVerfuegbar" sagt, ob die Plattform einen Paketordner waehlen laesst.
            Assert.Equal(14, gaben.Count);
            Assert.Equal(ZapfprofilHuelle.HILFE_KATALOG, gaben["HilfeSchluessel"]);
        }

        [Fact]
        public void Jeder_Ausgang_der_Katalogpflege_hat_seinen_Grund_in_beiden_Sprachen()
        {
            string[] ohne = Enum.GetValues(typeof(TwwKatalogAusgang)).Cast<TwwKatalogAusgang>()
                .Where(a => a != TwwKatalogAusgang.Ausgefuehrt).Select(ZapfprofilHuelle.KatalogSchluessel)
                .Where(k => Text(k, DE).Length == 0 || Text(k, EN).Length == 0).ToArray();
            Assert.True(ohne.Length == 0, "Ohne Grund: " + string.Join(", ", ohne));
        }

        [Fact]
        public void Liste_und_Stammblatt_nennen_Werte_Sperrgruende_und_Vorschau()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", VERSION);
            int halb = TwwTestdatenbank.TagesgangsatzAnlegen("Satz H", VERSION, false, 1, 2);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", VERSION, satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", VERSION, satz,
                                                                status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true, beleg: "intern");
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung benutzt", VERSION, halb);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone 1", 5.0);
            TwwTestdatenbank.KategorieAnlegen(frei, "Kurz", 1, 2.0, 1, 1.0, 0.5, kappung: 6.0);

            IReadOnlyList<Katalogfilterzeile> zeilen = ZapfprofilHuelle.KatalogZeilen();
            Assert.Equal(3, zeilen.Count);
            Katalogfilterzeile z = zeilen.Single(x => x.Id == frei);
            Assert.Equal(Text("ZPG_BEZUG_PERSONEN", DE), z.Text(Katalogfilterprofil.SpBezugsart));
            Assert.Equal(Text("ZPG_KALENDER_WOHNEN", DE), z.Text(Katalogfilterprofil.SpKalender));
            Assert.Equal(Text("ZPG_STATUS_EIGEN", DE), z.Text(Katalogfilterprofil.SpStatus));
            Assert.Equal(Text("ZPG_HERKUNFT_FIKTIV", DE), z.Text(Katalogfilterprofil.SpHerkunft));
            Assert.True(zeilen.Single(x => x.Id == geliefert).Geschuetzt);

            TwwNutzungsartDetailDaten d = ZapfprofilHuelle.KatalogDetail(frei);
            Assert.Equal("Nutzung frei", d.Name);
            Assert.Equal("", d.AendernGrund);
            Assert.Equal("", d.LoeschGrund);
            Assert.Contains(d.Kennwerte, w => w.Name == "Bedarf mittel" && w.Wert.StartsWith("2 (1,5 … 2,5)", StringComparison.Ordinal));
            Assert.Contains(d.Kennwerte, w => w.Name == "Bilanzgrenze" && w.Wert == "an der Zapfstelle");
            Assert.Equal(12 + 7 + 2, d.Gaenge.Count);
            Assert.Contains(d.Kategorien, w => w.Name == "Kurz" && w.Wert.Contains("höchstens 6 l/min", StringComparison.Ordinal));
            Assert.Contains(d.Herkunft, w => w.Name == "Verwendet in" && w.Wert == "in keinem Projekt");
            Assert.DoesNotContain(d.Herkunft, w => w.Wert.Contains("intern", StringComparison.Ordinal));
            Assert.NotNull(d.TagesgangModell);
            Assert.NotNull(d.JahresgangModell);
            Assert.Equal("", d.VorschauGrund);

            TwwNutzungsartDetailDaten g = ZapfprofilHuelle.KatalogDetail(geliefert);
            Assert.True(g.Auslieferung);
            Assert.StartsWith("Die Nutzungsart gehört zur Auslieferung", g.AendernGrund, StringComparison.Ordinal);
            Assert.Equal("Eine Nutzungsart der Auslieferung wird nicht gelöscht.", g.LoeschGrund);
            Assert.DoesNotContain(g.Herkunft, w => w.Wert.Contains("intern", StringComparison.Ordinal));

            TwwNutzungsartDetailDaten b = ZapfprofilHuelle.KatalogDetail(benutzt);
            Assert.True(b.Benutzt);
            Assert.Equal(new[] { "#1" }, b.Projekte);
            Assert.Equal("In #1 benutzt — nicht löschbar.", b.LoeschGrund);
            Assert.Contains("#1", b.AendernGrund, StringComparison.Ordinal);
            Assert.Null(b.TagesgangModell);
            Assert.Equal("Keine Vorschau — der Tagesgangsatz ist unvollständig.", b.VorschauGrund);
            Assert.Contains(b.Kategorien, w => w.Wert.StartsWith("keine", StringComparison.Ordinal));

            Assert.Null(ZapfprofilHuelle.KatalogDetail(999));
        }

        [Fact]
        public void Der_Editor_oeffnet_eine_gesperrte_Zeile_als_Speichern_unter_mit_freier_Version()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", VERSION);
            int halb = TwwTestdatenbank.TagesgangsatzAnlegen("Satz H", VERSION, false, 1);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", VERSION, satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", VERSION, satz,
                                                                status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);

            TwwNutzungsartEditorDaten a = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Aendern);
            Assert.Equal(TwwEditorModus.Aendern, a.Modus);
            Assert.Equal("Nutzung frei", a.Entwurf.Bezeichner);
            Assert.Equal(VERSION, a.Entwurf.Katalogversion);
            Assert.Equal(new double?[] { 1, 2, 3 }, a.Entwurf.Bedarf);
            Assert.Equal(1.5, a.Entwurf.BedarfMin[1]);
            Assert.Equal(12, a.Monatsnamen.Count);
            Assert.Equal(7, a.Bezugsarten.Count);
            Assert.Equal(new[] { satz }, a.Tagesgangsaetze.Select(s => s.Id).ToArray());   // der halbe Satz fehlt
            Assert.NotEqual(halb, a.Entwurf.IdTagesgangsatz);
            Assert.Contains("20 %", a.Wochenfaktoren, StringComparison.Ordinal);

            TwwNutzungsartEditorDaten s = ZapfprofilHuelle.EditorLaden(geliefert, TwwEditorModus.Aendern);
            Assert.Equal(TwwEditorModus.SpeichernUnter, s.Modus);
            Assert.StartsWith("Die Nutzungsart gehört zur Auslieferung", s.Hinweis, StringComparison.Ordinal);
            Assert.Equal(VERSION + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "1", s.Entwurf.Katalogversion);

            TwwNutzungsartEditorDaten n = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Neu);
            Assert.Equal("", n.Entwurf.Bezeichner);
            Assert.Equal(frei, n.Entwurf.IdBezug);

            TwwNutzungsartEditorDaten ohne = ZapfprofilHuelle.EditorLaden(0, TwwEditorModus.Neu);
            Assert.Equal(12, ohne.Entwurf.Monatsfaktoren.Count(m => m == 1.0));
            Assert.Equal(satz, ohne.Entwurf.IdTagesgangsatz);

            Assert.False(ZapfprofilHuelle.EditorLaden(999, TwwEditorModus.Aendern).Verfuegbar);
        }

        [Fact]
        public void Neu_Aendern_und_Speichern_unter_schreiben_und_normieren_die_Monatsfaktoren()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", VERSION);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", VERSION, satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", VERSION, satz,
                                                                status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);

            // Neu: alle Monatsfaktoren 2 - der Kern normiert auf das Mittel 1.
            TwwNutzungsartEntwurfDaten e = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Neu).Entwurf;
            e.Bezeichner = "Nutzung neu";
            e.Monatsfaktoren = Enumerable.Repeat((double?)2.0, 12).ToArray();
            TwwNutzungsartSpeicherErgebnis neu = ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Neu, e);
            Assert.True(neu.Ok, neu.Meldung);
            Nutzungsart nn = TwwNutzungsartCtrl.Lies(neu.Id);
            Assert.All(nn.Monatsfaktoren, m => Assert.Equal(1.0, m, 12));
            Assert.Equal(Herkunftsart.Eigenkonstruktion, nn.Herkunft.Bedarf.Art);
            Assert.Equal(ZapfKatalogstatus.Eigen, nn.Status);
            Assert.Equal(0.2, nn.Wochenfaktoren[0], 12);   // die Wochenfaktoren der Vorlage

            // Ändern an Ort und Stelle: der Bedarf mittel 2 -> 4.
            TwwNutzungsartEntwurfDaten a = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Aendern).Entwurf;
            a.Bedarf[1] = 4.0;
            TwwNutzungsartSpeicherErgebnis ae = ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Aendern, a);
            Assert.True(ae.Ok, ae.Meldung);
            Assert.Equal(frei, ae.Id);
            Assert.Equal(4.0, TwwNutzungsartCtrl.Lies(frei).BedarfJeNiveauKwhJeEinheitTag[1]);

            // Speichern unter aus der Auslieferung: neue Zeile, Vorlage bleibt.
            TwwNutzungsartEntwurfDaten s = ZapfprofilHuelle.EditorLaden(geliefert, TwwEditorModus.SpeichernUnter).Entwurf;
            s.Bedarf[0] = 0.5;
            TwwNutzungsartSpeicherErgebnis se = ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.SpeichernUnter, s);
            Assert.True(se.Ok, se.Meldung);
            Assert.NotEqual(geliefert, se.Id);
            Nutzungsart kopie = TwwNutzungsartCtrl.Lies(se.Id);
            Assert.Equal(geliefert, kopie.IdVorlage);
            Assert.Equal(1.0, TwwNutzungsartCtrl.Lies(geliefert).BedarfJeNiveauKwhJeEinheitTag[0]);

            // Ändern der Auslieferung selbst lehnt der Kern benannt ab.
            TwwNutzungsartEntwurfDaten gesperrt = ZapfprofilHuelle.EditorLaden(geliefert, TwwEditorModus.SpeichernUnter).Entwurf;
            gesperrt.Katalogversion = VERSION;
            TwwNutzungsartSpeicherErgebnis abgelehnt = ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Aendern, gesperrt);
            Assert.False(abgelehnt.Ok);
            Assert.Equal("ZPGK_GRUND_READ_ONLY_GESPERRT", abgelehnt.Kennung);
            Assert.StartsWith("Die Nutzungsart wurde nicht gespeichert — Sie gehört zur Auslieferung", abgelehnt.Meldung, StringComparison.Ordinal);
        }

        [Fact]
        public void Fehlende_Angaben_und_ein_vergebener_Name_werden_benannt_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", VERSION);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", VERSION, satz);

            TwwNutzungsartEntwurfDaten e = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Neu).Entwurf;
            e.Bedarf[2] = null;
            e.Kaltwasser = null;
            TwwNutzungsartSpeicherErgebnis luecke = ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Neu, e);
            Assert.False(luecke.Ok);
            Assert.Equal("ZPGK_GRUND_ENTWURF_UNVOLLSTAENDIG", luecke.Kennung);
            Assert.Contains("Nutzungsart, Bedarf hoch, Kaltwassertemperatur (Bezug)", luecke.Meldung, StringComparison.Ordinal);

            TwwNutzungsartEntwurfDaten b = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Neu).Entwurf;
            b.Bezeichner = "Nutzung frei";
            TwwNutzungsartSpeicherErgebnis belegt = ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Neu, b);
            Assert.False(belegt.Ok);
            Assert.Equal("ZPGK_GRUND_NAME_BELEGT", belegt.Kennung);

            TwwNutzungsartEntwurfDaten m = ZapfprofilHuelle.EditorLaden(frei, TwwEditorModus.Neu).Entwurf;
            m.Bezeichner = "Nutzung null";
            m.Monatsfaktoren = Enumerable.Repeat((double?)0.0, 12).ToArray();
            Assert.Equal("ZPGK_GRUND_RASTER_UNGUELTIG", ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Neu, m).Kennung);
        }

        [Fact]
        public void Loeschen_sperrt_Auslieferung_und_Verwendung_und_loescht_eine_freie_Zeile()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", VERSION);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", VERSION, satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", VERSION, satz,
                                                                status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung benutzt", VERSION, satz);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone 1", 5.0);

            Assert.Equal("ZPGK_GRUND_READ_ONLY_GESPERRT", ZapfprofilHuelle.KatalogLoeschen(geliefert).Kennung);
            Assert.Equal("ZPGK_GRUND_BENUTZT_GESPERRT", ZapfprofilHuelle.KatalogLoeschen(benutzt).Kennung);
            TwwNutzungsartSpeicherErgebnis ok = ZapfprofilHuelle.KatalogLoeschen(frei);
            Assert.True(ok.Ok);
            Assert.Equal("„Nutzung frei · T1“ ist gelöscht.", ok.Meldung);
            Assert.Null(TwwNutzungsartCtrl.Lies(frei));
            Assert.NotNull(TwwNutzungsartCtrl.Lies(geliefert));
            Assert.NotNull(TwwNutzungsartCtrl.Lies(benutzt));
        }

        [Fact]
        public void Der_Import_ueber_die_Huelle_liefert_den_Bericht_in_der_Oberflaechensprache()
        {
            using var db = new TwwTestdatenbank();
            string paket = Path.Combine(ZapfZufallTests.Probenordner(), "Katalogpaket", TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv");

            // Der gewaehlte Pfad ist EINE Datei des Paketordners - gelesen wird der ganze Ordner, also
            // auch die drei wahlfreien Dateien (zwei Bedarfstage, zwei Parameter; ZU30 bis ZU32).
            TwwImportberichtDaten b = ZapfprofilHuelle.KatalogImportieren(paket);
            Assert.False(b.Abgebrochen, b.Abbruch);
            Assert.False(b.Pruefmodus);
            Assert.Equal("6 angelegt · 0 ersetzt · 0 übersprungen · 0 abgelehnt", b.Zusammenfassung);
            Assert.All(b.Zeilen, z => Assert.Equal("angelegt", z.Ausgangstext));
            Assert.Equal("Probenutzung A (erfunden) · PROBE-1",
                         b.ZeilenVon(TwwImportbereichDaten.Nutzungsart)[0].Nutzungsart);
            Assert.Equal("Probetag A (erfunden) · PROBE-1",
                         b.ZeilenVon(TwwImportbereichDaten.Bedarfstag)[0].Nutzungsart);
            Assert.Equal(2, b.ZeilenVon(TwwImportbereichDaten.Parameter).Count);
            Assert.Equal(2, b.NeueIds.Count);      // allein die Nutzungsarten werden gewaehlt

            TwwImportberichtDaten zweit = ZapfprofilHuelle.KatalogImportieren(paket);
            Assert.Equal("0 angelegt · 0 ersetzt · 6 übersprungen · 0 abgelehnt", zweit.Zusammenfassung);
            Assert.Equal("Der Katalog führt sie schon mit gleichem Inhalt.",
                         zweit.ZeilenVon(TwwImportbereichDaten.Nutzungsart)[0].Grund);

            // Der Pruefmodus: dieselben Zeilen, nichts geschrieben - die Texte sagen „würde …".
            TwwImportberichtDaten pruef = ZapfprofilHuelle.KatalogImportieren(paket, true);
            Assert.True(pruef.Pruefmodus);
            Assert.All(pruef.Zeilen, z => Assert.Equal("würde überspringen", z.Ausgangstext));

            TwwImportberichtDaten leer = ZapfprofilHuelle.KatalogImportieren("");
            Assert.True(leer.Abgebrochen);
            Assert.Equal("Bitte zuerst ein Paket wählen.", leer.Abbruch);

            TwwImportberichtDaten fort = ZapfprofilHuelle.KatalogImportieren(Path.Combine(Path.GetTempPath(), "epos-fehlt-" + Guid.NewGuid().ToString("N") + ".zip"));
            Assert.True(fort.Abgebrochen);
            Assert.StartsWith("Das Paket ist abgelehnt — Die Datei „", fort.Abbruch, StringComparison.Ordinal);
        }

        [Fact]
        public void Mit_der_Testdatenbank_zeigt_der_Katalog_den_fiktiven_Testkatalog()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = ZapfprofilHuelle.KatalogZeilen();
            Assert.Equal(9, zeilen.Count);                 // drei fiktive, fünf abgeleitete (Katalogausbau Z5), das Hotel aus Messung (ZU36)
            Katalogfilterzeile a = zeilen.Single(z => z.Bezeichner == "Testnutzung A (fiktiv)");
            TwwNutzungsartDetailDaten d = ZapfprofilHuelle.KatalogDetail(a.Id);
            Assert.Equal(4, d.Kategorien.Count(k => k.Name.Length > 0));
            Assert.NotNull(d.TagesgangModell);
            Assert.Equal("", d.LoeschGrund);
            Assert.Contains(d.Herkunft, w => w.Wert.Contains("Testkatalog (fiktiv)", StringComparison.Ordinal));
        }
    }
}
