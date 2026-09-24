using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Katalogdialog „Brauchwasser-Nutzungsarten" und sein Editor (Umsetzungskonzept
/// Zapfprofilgenerator 5.4, 5.7; Stufe Z4, Gruppe 3): Liste und Stammblatt, die Wahl einer Zeile,
/// die benannten Sperrgründe (Auslieferung, in Projekten benutzt), Neu/Ändern/Speichern unter über
/// den Editor, Löschen mit Rückfrage, der Katalogimport mit Bericht, der Fuß nach DL-2, Esc wie
/// Beenden, der Fall ohne Gaben und die Sicht des Assistenten — dazu die Wache über das Textbündel.
///
/// <para>Die Datenseite kommt aus Prüfdelegaten; der Dialog schreibt nie selbst. Kultur de-DE,
/// alle Werte erfunden.</para>
/// </summary>
public class TwwNutzungsartAdminDialogTests : EposBunitContext
{
    private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

    public TwwNutzungsartAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfstand (erfunden)
    // =================================================================================

    /// <summary>Ein kleiner Katalog: eine freie, eine ausgelieferte, eine benutzte Nutzungsart.</summary>
    private sealed class Pruefkatalog
    {
        internal readonly List<(int Id, string Name, bool Auslieferung, string Projekte)> Zeilen = new()
        {
            (1, "Nutzung frei", false, ""),
            (2, "Nutzung geliefert", true, ""),
            (3, "Nutzung benutzt", false, "Projekt X")
        };

        internal readonly List<(TwwEditorModus Modus, TwwNutzungsartEntwurfDaten Entwurf)> Gespeichert = new();
        internal readonly List<int> Geloescht = new();
        internal readonly List<string> Importiert = new();
        internal int NaechsteId = 10;

        internal IReadOnlyList<Katalogfilterzeile> Liste()
            => Zeilen.Select(z => new Katalogfilterzeile(z.Id, z.Name)
            {
                Schluessel = z.Id.ToString(CultureInfo.InvariantCulture),
                Geschuetzt = z.Auslieferung
            }.MitText(Katalogfilterprofil.SpBezeichner, z.Name)
             .MitText(Katalogfilterprofil.SpKatalogversion, "T-1")).ToList();

        internal TwwNutzungsartDetailDaten? Detail(int id)
        {
            var z = Zeilen.FirstOrDefault(x => x.Id == id);
            if (z.Id == 0) return null;
            return new TwwNutzungsartDetailDaten
            {
                Id = id,
                Name = z.Name,
                Katalogversion = "T-1",
                Unterzeile = "T-1 · eigen · Personen",
                Auslieferung = z.Auslieferung,
                Benutzt = z.Projekte.Length > 0,
                AendernGrund = z.Auslieferung ? "gehört zur Auslieferung" : z.Projekte.Length > 0 ? "benutzt in " + z.Projekte : "",
                LoeschGrund = z.Auslieferung ? "Auslieferung wird nicht gelöscht" : z.Projekte.Length > 0 ? "In " + z.Projekte + " benutzt" : "",
                Kennzahlen = new[] { new Stammblattkennzahl("2 kWh/(Einheit·d)", "Bedarf mittel") },
                Kennwerte = new[] { new Stammblattwert("Bezugsart", "Personen"), new Stammblattwert("Bedarf mittel", "2") },
                Tagesgang = new[] { new Stammblattwert("Tagesgangsatz", "Satz A · T-1") },
                Gaenge = new[] { Stammblattwert.Abschnitt("Monatsfaktoren"), new Stammblattwert("Januar", "1") },
                Kategorien = new[] { new Stammblattwert("Kurz", "2 l/min") },
                Herkunft = new[] { new Stammblattwert("Verwendet in", z.Projekte.Length > 0 ? z.Projekte : "in keinem Projekt") },
                VorschauGrund = "Keine Vorschau — der Tagesgangsatz ist unvollständig."
            };
        }

        internal TwwNutzungsartEditorDaten Editor(int id, TwwEditorModus modus)
        {
            var z = Zeilen.FirstOrDefault(x => x.Id == id);
            bool gesperrt = z.Auslieferung || z.Projekte.Length > 0;
            TwwEditorModus m = modus == TwwEditorModus.Aendern && gesperrt ? TwwEditorModus.SpeichernUnter : modus;
            return new TwwNutzungsartEditorDaten
            {
                Modus = m,
                Hinweis = m == TwwEditorModus.SpeichernUnter && modus == TwwEditorModus.Aendern ? "gesperrt: " + z.Name : "",
                Bezugsarten = new[] { (1, "Personen"), (2, "Wohneinheiten") },
                Bilanzgrenzen = new[] { (1, "an der Zapfstelle") },
                Kalenderarten = new[] { (1, "Wohnen") },
                Tagesgangsaetze = new[] { (5, "Satz A · T-1") },
                Monatsnamen = Enumerable.Range(1, 12).Select(i => "M" + i).ToList(),
                Wochenfaktoren = "Mo 20 %",
                Entwurf = new TwwNutzungsartEntwurfDaten
                {
                    IdBezug = id,
                    Bezeichner = m == TwwEditorModus.Neu ? "" : z.Name ?? "",
                    Katalogversion = m == TwwEditorModus.SpeichernUnter ? "T-1-E1" : "T-1",
                    Bezugsart = 1,
                    Bedarf = new double?[] { 1, 2, 3 },
                    Zapftemperatur = 60,
                    Kaltwasser = 10,
                    Bilanzgrenze = 1,
                    Kalenderart = 1,
                    Monatsfaktoren = Enumerable.Repeat((double?)1.0, 12).ToArray(),
                    IdTagesgangsatz = 5
                }
            };
        }

        internal TwwNutzungsartSpeicherErgebnis Speichern(TwwEditorModus modus, TwwNutzungsartEntwurfDaten e)
        {
            Gespeichert.Add((modus, e));
            if (e.Bezeichner == "belegt") return new(false, e.IdBezug, "Die Nutzungsart wurde nicht gespeichert — vergeben", "ZPGK_GRUND_NAME_BELEGT");
            int id = modus == TwwEditorModus.Aendern ? e.IdBezug : NaechsteId++;
            if (modus != TwwEditorModus.Aendern) Zeilen.Add((id, e.Bezeichner, false, ""));
            return new(true, id, "„" + e.Bezeichner + "“ ist gespeichert.", "");
        }

        internal TwwNutzungsartSpeicherErgebnis Loeschen(int id)
        {
            Geloescht.Add(id);
            Zeilen.RemoveAll(z => z.Id == id);
            return new(true, id, "gelöscht", "");
        }

        internal TwwImportberichtDaten Importieren(string pfad)
        {
            Importiert.Add(pfad);
            Zeilen.Add((20, "Import A", false, ""));
            return new TwwImportberichtDaten
            {
                Zusammenfassung = "1 angelegt · 1 übersprungen · 1 abgelehnt",
                Zeilen =
                {
                    new("Import A · P-1", TwwImportausgangDaten.Angelegt, "angelegt", "", 20),
                    new("Import B · P-1", TwwImportausgangDaten.Uebersprungen, "übersprungen", "Der Katalog führt sie schon mit gleichem Inhalt.", 0),
                    new("Import C · P-1", TwwImportausgangDaten.Abgelehnt, "abgelehnt", "Die Angabe Bezug_Kaltwasser fehlt.", 0)
                },
                Hinweise = { "Die Datei „notizen.csv“ gehört nicht zum Katalog der Nutzungsarten und ist übergangen." }
            };
        }
    }

    private IRenderedComponent<TwwNutzungsartAdminDialog> Aufbauen(Pruefkatalog k, Action<bool>? geschlossen = null,
                                                                   string paket = "C:/paket/Tab_TwwNutzungsart_STAMM.csv",
                                                                   Func<IReadOnlyDictionary<string, object>>? typtagGaben = null)
        => Render<TwwNutzungsartAdminDialog>(p => p
            .Add(x => x.TyptagGaben, typtagGaben)
            .Add(x => x.Katalogzeilen, k.Liste)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerTwwNutzungsart(s => Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Detail, k.Detail)
            .Add(x => x.EditorLaden, k.Editor)
            .Add(x => x.Speichern, k.Speichern)
            .Add(x => x.Loeschen, k.Loeschen)
            .Add(x => x.TagesgangGaben, id => new Dictionary<string, object> { ["Daten"] = new ZapfprofilTagesgangDaten { IdNutzungsart = id } })
            .Add(x => x.KategorienGaben, id => new Dictionary<string, object> { ["Daten"] = new ZapfprofilKategorienEditorDaten() })
            .Add(x => x.PaketWaehlen, _ => Task.FromResult(paket))
            .Add(x => x.Importieren, k.Importieren)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung(IRenderedComponent<TwwNutzungsartAdminDialog> cut, string text)
        => cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Liste, Stammblatt, Fuß
    // =================================================================================

    [Fact]
    public void Liste_und_Stammblatt_zeigen_die_erste_Nutzungsart_und_der_Fuss_folgt_DL_2()
    {
        var cut = Aufbauen(new Pruefkatalog());

        Assert.Equal("Brauchwasser-Nutzungsarten", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Contains("epos-katalog-dialog", cut.Find(".epos-dialog").ClassName);
        Assert.Equal(3, Zeilenklick.Zeilen(cut).Count);
        Assert.Equal(1, cut.Instance.GewaehltId);
        Assert.Equal("Nutzung frei", cut.Find(".epos-stammblatt-name .epos-stammblatt-nametext").TextContent);
        string[] gruppen = cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Kennwerte", "Tagesgang", "Jahres- und Wochengang", "Zapfkategorien", "Herkunft" }, gruppen);
        Assert.Contains("Keine Vorschau", cut.Markup);

        // Die Handlungen der Gruppenköpfe: Ändern…, Grafik…, Tagesgang…, Kategorien….
        Assert.Single(cut.FindAll("button.epos-tww-aendern"));
        Assert.Single(cut.FindAll("button.epos-tww-grafik"));
        Assert.Single(cut.FindAll("button.epos-tww-tagesgang"));
        Assert.Single(cut.FindAll("button.epos-tww-kategorien"));

        // Der Fuß: Statuszeile · Import… · Neu… · Beenden (primär, zuletzt).
        IElement leiste = cut.FindAll(".epos-dialog > .epos-leiste").Last();
        string[] knoepfe = leiste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Import…", "Neu…", "Beenden" }, knoepfe);
        Assert.Contains("epos-knopf--primaer", leiste.QuerySelectorAll("button").Last().ClassName);
        Assert.Contains("epos-importknopf", leiste.QuerySelectorAll("button")[0].ClassName);

        // Die Auswahlleiste: Speichern unter… und Löschen.
        Assert.Equal(new[] { "Speichern unter…", "Löschen" },
                     cut.FindAll(".epos-auswahlleiste button").Select(b => b.TextContent.Trim())
                        .Where(t => t == "Speichern unter…" || t == "Löschen").ToArray());
    }

    [Fact]
    public void Ein_Klick_waehlt_die_Zeile_und_das_Stammblatt_zieht_nach()
    {
        var cut = Aufbauen(new Pruefkatalog());
        Zeilenklick.Zeile(cut, 1);

        Assert.Equal(2, cut.Instance.GewaehltId);
        Assert.Equal("Nutzung geliefert", cut.Instance.Stamm!.Name);
        Assert.NotEmpty(cut.FindAll(".epos-stammblatt-name .epos-kennzeichen, .epos-stammblatt-name [aria-label]"));
        Assert.Contains("gehört zur Auslieferung", cut.Find("button.epos-tww-aendern").GetAttribute("title"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_leer_ohne_Import_und_Neu()
    {
        bool? geschlossen = null;
        var cut = Render<TwwNutzungsartAdminDialog>(p => p.Add(x => x.Geschlossen, b => geschlossen = b));

        Assert.Equal("Brauchwasser-Nutzungsarten", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Contains("noch keine Nutzungsart", cut.Find(".epos-tww-katalog-leer").TextContent);
        Assert.Empty(cut.FindAll("button.epos-importknopf"));
        Assert.Empty(cut.FindAll("button.epos-tww-neu"));
        Assert.Null(cut.Instance.Stamm);
        Knopf(cut, "Beenden").Click();
        Assert.True(geschlossen);
    }

    // =================================================================================
    // Sperrgründe und Löschen
    // =================================================================================

    [Theory]
    [InlineData(1, "Auslieferung wird nicht gelöscht")]
    [InlineData(2, "In Projekt X benutzt")]
    public void Loeschen_ist_bei_Auslieferung_und_Verwendung_weich_gesperrt_und_nennt_den_Grund(int zeile, string grund)
    {
        var k = new Pruefkatalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, zeile);

        IElement loeschen = Handlung(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains(grund, loeschen.GetAttribute("title"));
        loeschen.Click();
        Assert.Contains(grund, cut.Instance.Meldung);
        Assert.Empty(k.Geloescht);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Loeschen_einer_freien_Zeile_fragt_nach_und_liest_neu()
    {
        var k = new Pruefkatalog();
        var cut = Aufbauen(k);

        Assert.Null(Handlung(cut, "Löschen").GetAttribute("aria-disabled"));
        Handlung(cut, "Löschen").Click();
        Assert.Contains("Nutzung frei · T-1", cut.Markup);
        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { 1 }, k.Geloescht);
        Assert.Equal(2, Zeilenklick.Zeilen(cut).Count);
        Assert.Equal(2, cut.Instance.GewaehltId);
        Assert.Equal("gelöscht", cut.Instance.Status);
    }

    // =================================================================================
    // Neu, Ändern, Speichern unter
    // =================================================================================

    [Fact]
    public void Neu_oeffnet_den_Editor_und_OK_legt_an_und_waehlt_die_neue_Zeile()
    {
        var k = new Pruefkatalog();
        var cut = Aufbauen(k);

        Knopf(cut, "Neu…").Click();
        Assert.Equal(TwwEditorModus.Neu, cut.Instance.Editor!.Modus);
        Assert.Contains("Neue Nutzungsart", cut.Find(".epos-ueberlagerung").TextContent);
        IRenderedComponent<TwwNutzungsartEditor> ed = cut.FindComponent<TwwNutzungsartEditor>();

        // Ohne Bezeichner hält die Prüfung das OK an.
        Knopf(ed, "OK").Click();
        Assert.Contains("Es fehlen Angaben: Nutzungsart", ed.Instance.Meldung);
        Assert.Empty(k.Gespeichert);

        ed.FindAll("input").First().Input("Nutzung neu");
        Knopf(ed, "OK").Click();

        Assert.Single(k.Gespeichert);
        Assert.Equal(TwwEditorModus.Neu, k.Gespeichert[0].Modus);
        Assert.Equal("Nutzung neu", k.Gespeichert[0].Entwurf.Bezeichner);
        Assert.Null(cut.Instance.Editor);
        Assert.Equal(10, cut.Instance.GewaehltId);
        Assert.Contains("ist gespeichert", cut.Instance.Status);
    }

    [Fact]
    public void Aendern_einer_benutzten_Zeile_geht_nur_als_Speichern_unter()
    {
        var k = new Pruefkatalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 2);

        cut.Find("button.epos-tww-aendern").Click();
        Assert.Equal(TwwEditorModus.SpeichernUnter, cut.Instance.Editor!.Modus);
        Assert.Contains("Nutzungsart speichern unter", cut.Find(".epos-ueberlagerung").TextContent);
        Assert.Contains("gesperrt: Nutzung benutzt", cut.Markup);

        IRenderedComponent<TwwNutzungsartEditor> ed = cut.FindComponent<TwwNutzungsartEditor>();
        Assert.Equal("T-1-E1", ed.Instance.Entwurf.Katalogversion);
        Knopf(ed, "OK").Click();
        Assert.Equal(TwwEditorModus.SpeichernUnter, k.Gespeichert.Single().Modus);
        Assert.Equal(3, k.Gespeichert.Single().Entwurf.IdBezug);
        Assert.Equal(10, cut.Instance.GewaehltId);
    }

    [Fact]
    public void Eine_Ablehnung_des_Kerns_bleibt_als_Banner_im_offenen_Editor()
    {
        var k = new Pruefkatalog();
        var cut = Aufbauen(k);
        Handlung(cut, "Speichern unter…").Click();
        IRenderedComponent<TwwNutzungsartEditor> ed = cut.FindComponent<TwwNutzungsartEditor>();
        ed.FindAll("input").First().Input("belegt");
        Knopf(ed, "OK").Click();

        Assert.NotNull(cut.Instance.Editor);
        Assert.Contains("nicht gespeichert", ed.Instance.Meldung);
        Assert.Contains("nicht gespeichert — vergeben", ed.Markup);

        Knopf(ed, "Abbrechen").Click();
        Assert.Null(cut.Instance.Editor);
    }

    [Fact]
    public void Der_Editor_nennt_das_Mittel_der_Monatsfaktoren_und_hat_dreizehn_Eingabestellen()
    {
        var k = new Pruefkatalog();
        var cut = Render<TwwNutzungsartEditor>(p => p.Add(x => x.Daten, k.Editor(1, TwwEditorModus.Aendern)));

        // 2 Textfelder, 4 Wahlen, 3 × 3 Bedarfsfelder, 2 Temperaturen, Ferienfaktor, 12 Monate.
        Assert.Equal(2 + 9 + 2 + 1 + 12, cut.FindAll("input").Count);
        Assert.Equal(4, cut.FindAll("select").Count);
        Assert.Contains("Die Monatsfaktoren haben das Mittel 1.", cut.Markup);
        Assert.Contains("Mo 20 %", cut.Markup);

        cut.FindAll(".epos-tww-editor input").Last().Input("2,2");
        Assert.Contains("Mittel der Monatsfaktoren 1,1", cut.Markup);
        Assert.Contains("M12", cut.Markup);
    }

    [Fact]
    public void Die_Sicht_des_Assistenten_setzt_ueber_die_Wege_der_Felder()
    {
        KiMaskenbruecke.Leeren();
        var k = new Pruefkatalog();
        var cut = Render<TwwNutzungsartEditor>(p => p.Add(x => x.Daten, k.Editor(1, TwwEditorModus.Aendern)));
        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.TWW_NUTZUNGSART_EDITOR));
        KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.TWW_NUTZUNGSART_EDITOR, feld)!;

        Zugang("bedarf_mittel").Setzen(4.0);
        Zugang("bezugsart").Setzen(2);
        Zugang("monatsfaktoren").Setzen(Enumerable.Repeat((double?)2.0, 12).ToArray());
        Zugang("bedarf_hoch_max").Setzen(5.0);
        Assert.Equal(4, cut.Instance.Entwurf.Bedarf[1]);
        Assert.Equal(2, cut.Instance.Entwurf.Bezugsart);
        Assert.Equal(2.0, cut.Instance.Entwurf.Monatsfaktoren[11]);
        Assert.Equal(5, cut.Instance.Entwurf.BedarfMax[2]);

        Assert.Throws<InvalidOperationException>(() => Zugang("bedarf_hoch").Setzen(-1.0));
        Assert.Throws<InvalidOperationException>(() => Zugang("bezugsart").Setzen(9));

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.TWW_NUTZUNGSART_EDITOR));
    }

    // =================================================================================
    // Import
    // =================================================================================

    [Fact]
    public void Import_zeigt_den_Bericht_und_waehlt_die_erste_neue_Nutzungsart()
    {
        var k = new Pruefkatalog();
        var cut = Aufbauen(k);

        Knopf(cut, "Import…").Click();
        Assert.True(cut.Instance.ImportOffen);
        Assert.Contains("Tab_TwwNutzungsart_STAMM.csv", cut.Find(".epos-tww-import").TextContent);

        // Ohne Paket ist „Importieren" weich gesperrt.
        Assert.Equal("true", Knopf(cut, "Importieren").GetAttribute("aria-disabled"));
        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.Null(Knopf(cut, "Importieren").GetAttribute("aria-disabled")));
        Knopf(cut, "Importieren").Click();

        Assert.Equal(new[] { "C:/paket/Tab_TwwNutzungsart_STAMM.csv" }, k.Importiert);
        Assert.Equal(3, cut.FindAll(".epos-tww-importbericht tbody tr").Count);
        Assert.Contains("Die Angabe Bezug_Kaltwasser fehlt.", cut.Find(".epos-tww-import-abgelehnt").TextContent);
        Assert.Contains("übersprungen", cut.Find(".epos-tww-import-uebersprungen").TextContent);
        Assert.Contains("notizen.csv", cut.Find(".epos-tww-import-hinweis").TextContent);
        Assert.Equal(20, cut.Instance.GewaehltId);
        Assert.Contains("1 angelegt", cut.Instance.Status);

        Knopf(cut, "Abbrechen").Click();
        Assert.False(cut.Instance.ImportOffen);
    }

    // =================================================================================
    // Tastatur, Überlagerungen
    // =================================================================================

    [Fact]
    public void Esc_wirkt_wie_Beenden_aber_nicht_solange_eine_Ueberlagerung_steht()
    {
        bool? geschlossen = null;
        var cut = Aufbauen(new Pruefkatalog(), b => geschlossen = b);

        Knopf(cut, "Import…").Click();
        cut.Find(".epos-tww-katalog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(geschlossen);

        Knopf(cut, "Abbrechen").Click();
        cut.Find(".epos-tww-katalog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(geschlossen);
    }

    [Fact]
    public void Tagesgang_und_Kategorien_oeffnen_die_Editoren_des_Zapfprofils_zur_Zeile()
    {
        var cut = Aufbauen(new Pruefkatalog());
        cut.Find("button.epos-tww-tagesgang").Click();
        Assert.NotNull(cut.FindComponent<TagesgangEditor>());
        Assert.Equal(1, cut.FindComponent<TagesgangEditor>().Instance.Daten.IdNutzungsart);
    }

    [Fact]
    public void Grafik_ohne_Vorschau_nennt_den_Grund()
    {
        var cut = Aufbauen(new Pruefkatalog());
        cut.Find("button.epos-tww-grafik").Click();
        Assert.Contains("Keine Vorschau", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Sicht_des_Assistenten_waehlt_die_Zeile_und_nennt_den_Sperrgrund()
    {
        KiMaskenbruecke.Leeren();
        var cut = Aufbauen(new Pruefkatalog());
        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BRAUCHWASSER_NUTZUNGSARTEN));
        KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.BRAUCHWASSER_NUTZUNGSARTEN, feld)!;

        Assert.Equal(1, Zugang("satz").Lesen());
        Zugang("satz").Setzen(2);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.GewaehltId));
        Assert.Equal("gehört zur Auslieferung", Zugang("sperrgrund").Lesen());
        Assert.Equal("T-1", Zugang("katalogversion").Lesen());
        Assert.Throws<InvalidOperationException>(() => Zugang("satz").Setzen(99));

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BRAUCHWASSER_NUTZUNGSARTEN));
    }

    // =================================================================================
    // Die Überlagerung der VDI-4655-Typtage
    // =================================================================================

    /// <summary>Ein Gabensatz, mit dem der Importdialog der Typtage zeichnet (leerer Stand).</summary>
    private static IReadOnlyDictionary<string, object> Typtaggaben()
        => new Dictionary<string, object>
        {
            ["Stand"] = new Func<TwwTyptagStandDaten>(() => new TwwTyptagStandDaten()),
            ["TitelAnzeigen"] = false
        };

    /// <summary>
    /// Ohne den Delegaten steht der Knopf nicht da; mit ihm öffnet er die Überlagerung, und ein
    /// geänderter Stand meldet sich mit EIGENEM Statustext — nicht mit dem Titel des Dialogs.
    /// </summary>
    [Fact]
    public void Der_Knopf_der_Typtage_oeffnet_die_Ueberlagerung_und_meldet_den_neuen_Stand()
    {
        var ohne = Aufbauen(new Pruefkatalog());
        Assert.Empty(ohne.FindAll("button").Where(b => b.TextContent.Trim() == "VDI-4655-Typtage…"));

        var cut = Aufbauen(new Pruefkatalog(), typtagGaben: Typtaggaben);
        Assert.Empty(cut.FindAll(".epos-tww-typtage"));

        Knopf(cut, "VDI-4655-Typtage…").Click();
        Assert.NotEmpty(cut.FindAll(".epos-tww-typtage"));
        Assert.Equal("", cut.Instance.Status);

        // Zu, ohne Änderung: die Statuszeile bleibt leer.
        cut.InvokeAsync(() => cut.FindComponent<TwwTyptagImportDialog>().Instance.Geschlossen.InvokeAsync(false));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-tww-typtage")));
        Assert.Equal("", cut.Instance.Status);

        // Zu, mit Änderung: der eigene Satz des Bestands, nicht „VDI-4655-Typtage".
        Knopf(cut, "VDI-4655-Typtage…").Click();
        cut.InvokeAsync(() => cut.FindComponent<TwwTyptagImportDialog>().Instance.Geschlossen.InvokeAsync(true));
        cut.WaitForAssertion(() => Assert.Equal(Resource.ZPGT_MSG_STAND_NEU, cut.Instance.Status));
        Assert.Empty(cut.FindAll(".epos-tww-typtage"));
        Assert.NotEqual(Resource.ZPGT_TITEL, cut.Instance.Status);
    }

    // =================================================================================
    // Wache über das Textbündel
    // =================================================================================

    private static readonly Regex Eigenschaft = new(
        @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c></summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \} = ""(?<wert>(?:[^""\\]|\\.)*)"";",
        RegexOptions.Compiled);

    /// <summary>
    /// Jede Beschriftung des Bündels trägt ihren <c>ZPGK_</c>-Schlüssel, der steht in beiden Sprachen
    /// mit denselben Platzhaltern, und der Vorgabewert ist der Text der neutralen Ressource.
    /// </summary>
    [Fact]
    public void Jede_Beschriftung_des_Buendels_steht_mit_ihrem_Schluessel_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "Dialoge", "Bedarf", "TwwNutzungsartAdminTexte.cs"));
        List<Match> treffer = Eigenschaft.Matches(quelle).ToList();
        string[] eigenschaften = typeof(TwwNutzungsartAdminTexte).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string)).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.True(eigenschaften.Length >= 90, "Nur " + eigenschaften.Length + " Beschriftungen.");
        Assert.Equal(eigenschaften, treffer.Select(m => m.Groups["name"].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        var vorgabe = new TwwNutzungsartAdminTexte();
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string k = m.Groups["schluessel"].Value;
            string de = Resource.ResourceManager.GetString(k, DE) ?? "";
            string en = Resource.ResourceManager.GetString(k, EN) ?? "";
            string rueckfall = (string)typeof(TwwNutzungsartAdminTexte).GetProperty(m.Groups["name"].Value)!.GetValue(vorgabe)!;
            if (!k.StartsWith("ZPGK_", StringComparison.Ordinal)) funde.Add(k + ": kein ZPGK_-Schlüssel");
            if (de.Length == 0) funde.Add(k + ": fehlt deutsch");
            if (en.Length == 0) funde.Add(k + ": fehlt englisch");
            if (!string.Equals(de, rueckfall, StringComparison.Ordinal)) funde.Add(k + ": Rückfall ≠ Ressource");
            if (!Platzhalter(de).SequenceEqual(Platzhalter(en))) funde.Add(k + ": Platzhalter weichen ab");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    private static string[] Platzhalter(string text)
        => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

    private static string Wurzel([CallerFilePath] string eigeneDatei = "")
    {
        string? ordner = Path.GetDirectoryName(eigeneDatei);
        while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
            ordner = Path.GetDirectoryName(ordner);
        Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!;
    }
}
