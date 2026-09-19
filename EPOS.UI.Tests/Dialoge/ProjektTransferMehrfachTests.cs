using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der MEHRFACHTRANSFER (Auftrag PI-1, Anwenderentscheid PI-Q1 vom 19.09.2026):
/// Export mit Mehrfachauswahl, Import mehrerer Pakete in einem Lauf.
///
/// <para><b>Was hier geprüft wird und in <c>ProjektTransferDialogTests</c> nicht.</b>
/// Dort steht die Feldkarte des Vorläufers — zwei Blätter, drei Konfliktmodi, zwei
/// Rückfragen, „kein Delegat, kein Knopf". Hier steht, was PI-1 hinzufügt: die
/// Wahlliste statt der Klappliste, der Stamm, der einer gewählten Variante folgt,
/// die Häkchenblöcke je Gruppe samt gesperrtem Häkchen, die Paketvorschau und die
/// zwei Bilanzen.</para>
///
/// <para><b>Ohne Gaben zeichnet der Dialog</b> — die Hausregel gilt auch für die
/// neuen Teile: jeder Delegat <c>null</c>, jede Liste leer.</para>
/// </summary>
public class ProjektTransferMehrfachTests : EposBunitContext
{
    /// <summary>Zwei Stammgruppen und ein Projekt ohne Varianten.</summary>
    private static readonly ProjektKopfZeile[] BESTAND =
    {
        new ProjektKopfZeile(1019, "Wöhler", Kunde: "Kunde A"),
        new ProjektKopfZeile(1023, "Wöhler - Test1", StammId: 1019,
                             Bezeichner: "Test1", StammName: "Wöhler"),
        new ProjektKopfZeile(1024, "Wöhler - Test2", StammId: 1019,
                             Bezeichner: "Test2", StammName: "Wöhler"),
        new ProjektKopfZeile(1026, "Beispiel WP WG 1", Kunde: "Kunde B"),
        new ProjektKopfZeile(1027, "Beispiel WP WG 1 - Andere WP", StammId: 1026,
                             Bezeichner: "Andere WP", StammName: "Beispiel WP WG 1"),
        new ProjektKopfZeile(1030, "Referenz BHKW")
    };

    public ProjektTransferMehrfachTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Der Kern-Ersatz
    // =====================================================================

    /// <summary>Er merkt sich, womit er gerufen wurde.</summary>
    private sealed class Kern
    {
        public IReadOnlyList<Transfergruppe> ExportGruppen = Array.Empty<Transfergruppe>();
        public string ExportOrdner = "";
        public string ExportZiel = "";
        public ExportBilanz ExportErgebnis = Fertig(2, 2, 0);

        public IReadOnlyList<string> ImportPfade = Array.Empty<string>();
        public SammelImportBilanz ImportErgebnis = new SammelImportBilanz
        { Pakete = 3, Importiert = 2, Uebersprungen = 1, Umbenannt = 0, Fehler = 0 };
        public string BerichtPfad = "";

        public IReadOnlyList<string> Dateien = Array.Empty<string>();
        public string Ordner = "D:\\Ausgabe";

        public ExportBilanz Exportieren(IReadOnlyList<Transfergruppe> gruppen, string ordner,
                                        IProgress<ImportFortschritt>? melder)
        {
            ExportGruppen = gruppen;
            ExportOrdner = ordner;
            melder?.Report(new ImportFortschritt(0.5, "TRANSFER_LAUF_PAKET", "1", "2", "Tab_Projekt"));
            return ExportErgebnis;
        }

        public SammelImportBilanz Importieren(IReadOnlyList<string> pfade,
                                              ProjektExportImportCtrl.BeiVorhandenem modus,
                                              IProgress<ImportFortschritt>? melder)
        {
            ImportPfade = pfade;
            melder?.Report(new ImportFortschritt(0.5, "TRANSFER_LAUF_PAKET", "2", "3", "Tab_Projekt"));
            return ImportErgebnis;
        }

        public static ExportBilanz Fertig(int pakete, int geschrieben, int fehler)
        {
            var b = new ExportBilanz { Pakete = pakete, Geschrieben = geschrieben, Fehler = fehler };
            b.Anfuegen(new ExportZeile("Beispiel WP WG 1", "D:\\Ausgabe\\Beispiel WP WG 1.wpx", 2, true, ""));
            b.Anfuegen(new ExportZeile("Wöhler", "D:\\Ausgabe\\Wöhler.wpx", 3, true, ""));
            return b;
        }
    }

    /// <summary>Die drei Paketköpfe der Vorschauproben.</summary>
    private static Paketkopf Kopf(string datei, string projekt, string stamm = "",
                                  int schema = -1, params string[] varianten)
        => new Paketkopf(datei, projekt, "19.09.2026 10:00",
                         schema < 0 ? SchemaStand.Zielversion : schema, 2,
                         varianten, stamm, "");

    private static ProjektTransferDaten Daten(
        Kern kern,
        bool mitOrdner = true,
        bool mitDateien = true,
        IReadOnlyList<Paketkopf>? koepfe = null)
    {
        var nachPfad = (koepfe ?? Array.Empty<Paketkopf>()).ToDictionary(k => k.Datei, k => k);

        return new ProjektTransferDaten(
            Projekte: BESTAND,
            Varianten: _ => Array.Empty<string>(),
            Exportieren: (_, _, _, _) => true,
            PaketLesen: () => "C:\\pakete\\eins.wpx",
            PaketSchreiben: v => "C:\\ziel\\" + v,
            Vorschau: pfad => nachPfad.TryGetValue(pfad, out Paketkopf? k)
                ? new PaketVorschau(k.Quellprojekt, k.Exportdatum, k.Schemastand,
                                    k.Varianten, k.Fehler, k.StammQuelle)
                : new PaketVorschau("Wöhler", "19.09.2026 10:00", SchemaStand.Zielversion,
                                    Array.Empty<string>(), ""),
            Importieren: (_, name, _, _) => new ImportErgebnis(1, name, new[] { "Zeile eins" }, ""),
            SicherungAnlegen: () => "sicherung.sqlite",
            BerichtSchreiben: (pfad, _) => { kern.BerichtPfad = pfad; return "bericht.txt"; },
            ExportierenMehrere: kern.Exportieren,
            ZielordnerWaehlen: mitOrdner ? () => Task.FromResult<string?>(kern.Ordner) : null,
            PaketeLesen: mitDateien ? () => Task.FromResult(kern.Dateien) : null,
            ImportierenMehrere: kern.Importieren,
            Paketkopf: pfad => nachPfad.TryGetValue(pfad, out Paketkopf? k)
                ? k
                : Kopf(pfad, "Unbekannt"),
            NameVergeben: _ => false);
    }

    private IRenderedComponent<ProjektTransferDialog> Aufbauen(
        Kern kern, Action<ComponentParameterCollectionBuilder<ProjektTransferDialog>>? mehr = null,
        bool mitOrdner = true, bool mitDateien = true,
        IReadOnlyList<Paketkopf>? koepfe = null)
        => Render<ProjektTransferDialog>(p =>
        {
            p.Add(x => x.Daten, Daten(kern, mitOrdner, mitDateien, koepfe));
            mehr?.Invoke(p);
        });

    private static void Anhaken(IRenderedComponent<ProjektTransferDialog> cut, int zeile)
        => cut.FindAll(".epos-transfer-liste .epos-anlagenwahl")[zeile].Click();

    private static void ZumImport(IRenderedComponent<ProjektTransferDialog> cut)
        => cut.FindAll(".epos-reiter-knopf")[1].Click();

    // =====================================================================
    //  Exportblatt
    // =====================================================================

    [Fact]
    public void Das_Exportblatt_zeigt_die_Mehrfachliste_statt_der_Klappliste()
    {
        var cut = Aufbauen(new Kern());

        Assert.Empty(cut.FindAll(".epos-projekttransfer select"));
        Assert.Single(cut.FindAll(".epos-transfer-liste"));

        // Sechs Spalten plus die Wahlspalte, und je Projekt eine Zeile.
        Assert.Equal(BESTAND.Length, cut.FindAll(".epos-transfer-liste .epos-anlagenwahl").Count);

        // Die Spaltenköpfe der Ausprägung stehen da — mit Suche und Trichtern.
        string kopf = cut.Find(".epos-transfer-liste thead").TextContent;
        Assert.Contains("Projekt", kopf);
        Assert.Contains("Art", kopf);
        Assert.Contains("Varianten", kopf);
        Assert.Contains("mitgenommen", kopf);

        // Die Herkunftsspalte unterscheidet Stamm und Variante.
        string koerper = cut.Find(".epos-transfer-liste tbody").TextContent;
        Assert.Contains("Variante von Wöhler", koerper);
        Assert.Contains("Stamm", koerper);
    }

    [Fact]
    public void Ohne_Wahl_meldet_der_Export_statt_zu_laufen()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);

        Assert.Empty(cut.Instance.Gruppen);
        cut.Find(".epos-transfer-export").Click();

        Assert.Contains("mindestens ein Projekt", cut.Find(".epos-dialog > .epos-warnbanner").TextContent);
        Assert.Empty(kern.ExportGruppen);
    }

    [Fact]
    public void Eine_gewaehlte_Variante_zieht_ihren_Stamm_sichtbar_nach()
    {
        var cut = Aufbauen(new Kern());

        Anhaken(cut, 1);                                   // „Wöhler - Test1"

        // (a) Die Gruppe trägt den STAMM als Hauptprojekt.
        Assert.Single(cut.Instance.Gruppen);
        Assert.Equal("Wöhler", cut.Instance.Gruppen[0].Stamm);
        Assert.Equal(new[] { 1023 }, cut.Instance.Gewaehlt.ToArray());

        // (b) Die Hinweiszeile nennt beide Zahlen — ein Stamm kommt zu einer Variante.
        string hinweis = cut.FindAll(".epos-warnbanner")[0].TextContent;
        Assert.Contains("Stammprojekt", hinweis);

        // (c) Die Spalte „mitgenommen" steht beim STAMM auf Ja.
        var zellen = cut.FindAll(".epos-transfer-liste tbody tr")[0].QuerySelectorAll("td");
        Assert.Equal("Ja", zellen[zellen.Length - 1].TextContent.Trim());
    }

    [Fact]
    public void Je_Stammgruppe_steht_ein_Haekchenblock_mit_gesperrtem_Eintrag()
    {
        var cut = Aufbauen(new Kern());

        Anhaken(cut, 1);                                   // „Wöhler - Test1" ausdrücklich

        var bloecke = cut.FindAll(".epos-transfer-gruppen .epos-mehrfachauswahl");
        Assert.Single(bloecke);
        Assert.Contains("Wöhler", bloecke[0].TextContent);

        // Beide Varianten stehen drin, beide an (TF1).
        var kaesten = cut.FindAll(".epos-transfer-gruppen input[type=checkbox]");
        Assert.Equal(2, kaesten.Count);
        Assert.Equal(2, cut.FindAll(".epos-transfer-gruppen input[type=checkbox]:checked").Count);

        // Die AUSDRÜCKLICH gewählte ist weich gesperrt und nennt ihren Grund.
        var gesperrt = cut.FindAll(".epos-transfer-gruppen input[aria-disabled=true]");
        Assert.Single(gesperrt);
        Assert.Null(gesperrt[0].GetAttribute("disabled"));
        Assert.False(string.IsNullOrEmpty(gesperrt[0].ParentElement!.GetAttribute("title")));

        // Ein Klick darauf nimmt sie NICHT aus der Gruppe.
        gesperrt[0].Click();
        Assert.Contains("Wöhler - Test1", cut.Instance.Gruppen[0].Varianten);

        // Die andere lässt sich abwählen.
        cut.FindAll(".epos-transfer-gruppen input[type=checkbox]")
           .First(k => k.GetAttribute("aria-disabled") is null).Change(false);
        Assert.Equal(new[] { "Wöhler - Test1" }, cut.Instance.Gruppen[0].Varianten.ToArray());
    }

    [Fact]
    public void Bei_einer_Gruppe_bleibt_der_Speichern_Dialog()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);

        Anhaken(cut, 0);                                   // nur „Wöhler"

        // Die Zielordnerzeile erscheint erst ab zwei Gruppen.
        Assert.Empty(cut.FindAll(".epos-transfer-ordner"));

        cut.Find(".epos-transfer-export").Click();
        cut.WaitForAssertion(() =>
            Assert.Contains("Export abgeschlossen.", cut.Find(".epos-dialog > .epos-warnbanner").TextContent));

        // Der Sammelweg ist NICHT gelaufen.
        Assert.Empty(kern.ExportGruppen);
    }

    [Fact]
    public void Ab_zwei_Gruppen_reicht_der_Export_Gruppen_und_Zielordner_durch()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);

        Anhaken(cut, 1);                                   // „Wöhler - Test1" → Gruppe Wöhler
        Anhaken(cut, 3);                                   // „Beispiel WP WG 1"
        Assert.Equal(2, cut.Instance.Gruppen.Count);

        // Ohne Zielordner läuft nichts.
        cut.Find(".epos-transfer-export").Click();
        Assert.Contains("Zielordner", cut.Find(".epos-dialog > .epos-warnbanner").TextContent);
        Assert.Empty(kern.ExportGruppen);

        // Der Ordnerknopf steht da und füllt die Zeile.
        cut.Find(".epos-transfer-ordner-knopf").Click();
        cut.WaitForAssertion(() => Assert.Equal("D:\\Ausgabe", cut.Instance.Zielordner));

        cut.Find(".epos-transfer-export").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, kern.ExportGruppen.Count));
        Assert.Equal("D:\\Ausgabe", kern.ExportOrdner);
        Assert.Equal(new[] { "Beispiel WP WG 1", "Wöhler" },
                     kern.ExportGruppen.Select(g => g.Stamm).ToArray());

        // Die Bilanz steht in der Statuszeile, die Zeilen im Berichtsfeld.
        cut.WaitForAssertion(() =>
            Assert.Contains("2", cut.Find(".epos-dialog > .epos-warnbanner").TextContent));
    }

    [Fact]
    public void Ohne_Ordnerdelegat_erscheint_kein_Ordnerknopf()
    {
        // Hausregel A-18: auf iOS gibt es keinen Ordnerwähler (Entscheid E-5).
        var cut = Aufbauen(new Kern(), mitOrdner: false);

        Anhaken(cut, 0);
        Anhaken(cut, 3);
        Assert.Equal(2, cut.Instance.Gruppen.Count);

        Assert.Empty(cut.FindAll(".epos-transfer-ordner"));
    }

    // =====================================================================
    //  Importblatt
    // =====================================================================

    [Fact]
    public void Die_Mehrfachdateiwahl_fuellt_die_Vorschauliste_in_Laufreihenfolge()
    {
        var kern = new Kern { Dateien = new[] { "C:\\p\\variante.wpx", "C:\\p\\fremd.wpx", "C:\\p\\stamm.wpx" } };
        var cut = Aufbauen(kern, koepfe: new[]
        {
            Kopf("C:\\p\\variante.wpx", "Wöhler - Test1", stamm: "Wöhler"),
            Kopf("C:\\p\\fremd.wpx", "Referenz BHKW", schema: SchemaStand.Zielversion - 3),
            Kopf("C:\\p\\stamm.wpx", "Wöhler", varianten: "Wöhler - Test2")
        });
        ZumImport(cut);

        cut.Find(".epos-transfer-dateien").Click();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.Instance.Pakete.Count));

        // STAMM VOR VARIANTE — der Lauf sortiert, nicht der Dateiwähler: Die
        // Stammpakete behalten die Reihenfolge der Wahl, jedes Variantenpaket
        // rückt HINTER das Paket seines Stamms. Der Anwender hatte die Variante
        // zuerst gewählt; sie steht jetzt hinter „Wöhler".
        Assert.Equal(new[] { "Referenz BHKW", "Wöhler", "Wöhler - Test1" },
                     cut.Instance.Pakete.Select(z => z.Hauptprojekt).ToArray());

        // Der Dateiname steht ohne Pfad da.
        Assert.Equal(new[] { "fremd.wpx", "stamm.wpx", "variante.wpx" },
                     cut.Instance.Pakete.Select(z => z.Datei).ToArray());

        // Die Variantenherkunft steht im Hinweis.
        Assert.Contains("Wöhler", cut.Instance.Pakete[2].Hinweis);

        // Der abweichende Schemastand ist markiert, der passende nicht.
        Assert.Contains("abweichend", cut.Instance.Pakete[0].Schema);
        Assert.DoesNotContain("abweichend", cut.Instance.Pakete[1].Schema);

        // Und das alles steht auch im Raster.
        string raster = cut.Find(".epos-transfer-pakete").TextContent;
        Assert.Contains("stamm.wpx", raster);
        Assert.Contains("Wöhler - Test2", raster);
    }

    [Fact]
    public void Derselbe_Stamm_in_zwei_Paketen_wird_als_Dublette_benannt()
    {
        var kern = new Kern { Dateien = new[] { "C:\\p\\a.wpx", "C:\\p\\b.wpx" } };
        var cut = Aufbauen(kern, koepfe: new[]
        {
            Kopf("C:\\p\\a.wpx", "Wöhler", varianten: "Wöhler - Test1"),
            Kopf("C:\\p\\b.wpx", "Wöhler", varianten: "Wöhler - Test2")
        });
        ZumImport(cut);

        cut.Find(".epos-transfer-dateien").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Pakete.Count));

        Assert.Equal("", cut.Instance.Pakete[0].Hinweis);
        Assert.Contains("Paket 1", cut.Instance.Pakete[1].Hinweis);
        Assert.Contains("übersprungen", cut.Instance.Pakete[1].Hinweis);
    }

    [Fact]
    public void Ab_zwei_Paketen_weicht_das_Zielnamefeld_der_leisen_Zeile()
    {
        var kern = new Kern { Dateien = new[] { "C:\\p\\a.wpx", "C:\\p\\b.wpx" } };
        var cut = Aufbauen(kern, koepfe: new[]
        {
            Kopf("C:\\p\\a.wpx", "Wöhler"),
            Kopf("C:\\p\\b.wpx", "Beispiel WP WG 1")
        });
        ZumImport(cut);

        // Vor der Wahl steht das Feld da.
        Assert.NotEmpty(cut.FindAll(".epos-projekttransfer input[type=text]"));

        cut.Find(".epos-transfer-dateien").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Pakete.Count));

        Assert.Empty(cut.FindAll(".epos-projekttransfer input[type=text]"));
        Assert.Contains("Zielname", cut.Find(".epos-herleitung").TextContent);
    }

    [Fact]
    public void Der_Sammelimport_laeuft_und_meldet_seine_Bilanz()
    {
        var kern = new Kern { Dateien = new[] { "C:\\p\\a.wpx", "C:\\p\\b.wpx" } };
        kern.ImportErgebnis = Bilanz();
        var cut = Aufbauen(kern, koepfe: new[]
        {
            Kopf("C:\\p\\a.wpx", "Wöhler"),
            Kopf("C:\\p\\b.wpx", "Beispiel WP WG 1")
        });
        ZumImport(cut);

        cut.Find(".epos-transfer-dateien").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Pakete.Count));

        cut.Find(".epos-transfer-import").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Wöhler", cut.Find(".epos-projekttransfer textarea").TextContent));

        // Die Pfade reisen in der LAUFREIHENFOLGE durch.
        Assert.Equal(new[] { "C:\\p\\a.wpx", "C:\\p\\b.wpx" }, kern.ImportPfade.ToArray());

        // Die Bilanz steht in der Statuszeile.
        string status = cut.Find(".epos-dialog > .epos-warnbanner").TextContent;
        Assert.Contains("2", status);

        // Der SAMMELbericht landet neben dem ERSTEN Paket, nicht je Paket.
        Assert.Equal("C:\\p\\a.wpx", kern.BerichtPfad);
    }

    // =====================================================================
    //  Ohne Gaben
    // =====================================================================

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<ProjektTransferDialog>();

        Assert.Equal(2, cut.FindAll(".epos-reiter-knopf").Count);
        Assert.Single(cut.FindAll(".epos-transfer-liste"));
        Assert.Empty(cut.FindAll(".epos-transfer-liste .epos-anlagenwahl"));
        Assert.Empty(cut.FindAll(".epos-transfer-gruppen"));
        Assert.Empty(cut.FindAll(".epos-transfer-ordner"));

        ZumImport(cut);

        // Ohne PaketeLesen bleibt die Einzelwahl stehen (kein Delegat, kein Knopf).
        Assert.Empty(cut.FindAll(".epos-transfer-dateien"));
        Assert.NotEmpty(cut.FindAll(".epos-dateiwahl"));
        Assert.Empty(cut.FindAll(".epos-transfer-pakete"));
    }

    /// <summary>Die Bilanz, mit der die Importprobe rechnet.</summary>
    private static SammelImportBilanz Bilanz()
    {
        var b = new SammelImportBilanz { Pakete = 2, Importiert = 2 };
        b.Anfuegen(new SammelImportZeile("C:\\p\\a.wpx", "Wöhler", "Wöhler (2)",
                                         Paketbefund.Umbenannt, "",
                                         new[] { "Projekt \u201EWöhler (2)\u201C importiert." }));
        b.Anfuegen(new SammelImportZeile("C:\\p\\b.wpx", "Beispiel WP WG 1", "Beispiel WP WG 1 (2)",
                                         Paketbefund.Importiert, "",
                                         new[] { "Projekt importiert." }));
        return b;
    }
}
