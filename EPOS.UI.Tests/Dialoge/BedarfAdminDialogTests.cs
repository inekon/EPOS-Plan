using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Verwaltung der drei Bedarfskataloge (iU9-W14b.1) — DRILLINGE. Soll sind die
/// Feldkarten von <c>Form_Stromverbraucher_Admin</c> (13 Steuerelemente),
/// <c>Form_Prozesswaerme_Admin</c> (13) und <c>Form_Brauchwasser_Admin</c> (14): je
/// 7 Knöpfe, 6 Beschriftungen, 4 Textfelder und eine Liste.
///
/// <para><b>Der Feldbestand wird je AUSPRÄGUNG geprüft</b>, nicht je Komponente
/// (Risiko R‑W14‑1) — genau wie in Welle 8 und Welle 13.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class BedarfAdminDialogTests : BunitContext
{
    private static readonly string[] KATALOG = { "Alpha", "Beta", "Gamma" };

    public BedarfAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>DeutscheOberflaeche</c> aus <c>GebaeudeKatalogDialogTests</c>) — Kultur UND
    /// Thread-Kultur, damit ein Lauf unter <c>LANG=en_US.UTF-8</c> dieselben deutschen
    /// Beschriftungen sieht.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =====================================================================
    // Aufbau
    // =====================================================================

    /// <summary>
    /// Die Texte je Ausprägung — dieselben, die <c>BedarfAdminHuelle</c> aus
    /// <c>MyResource</c> holt. Sie stehen hier als Erwartungswerte, damit der Test
    /// nicht dieselbe Quelle prüft, aus der er liest.
    /// </summary>
    private sealed record Beschriftung(string Titel, string Katalog, string Jahressumme,
                                       string Einheit, string Aendern, string Neu,
                                       string TypAendern, string Loeschen, string KeineWahl);

    private static Beschriftung Texte(BedarfsArt art) => art switch
    {
        BedarfsArt.Stromverbraucher => new Beschriftung(
            "Stromverbraucher Verwaltung", "Datenbank Stromverbraucher",
            "jährlicher Strombedarf:", "MWh",
            "Verbraucher in DB ändern", "Verbraucher in DB neu", "Typ in DB ändern",
            "Verbraucher in DB löschen", "Bitte wählen Sie zuerst einen Verbraucher aus!"),
        BedarfsArt.Prozesswaerme => new Beschriftung(
            "Prozesswärme Verwaltung", "Datenbank Prozesswärme:",
            "jährlicher Prozesswärmebedarf:", "MWth",
            "Prozess ändern", "Neuer Prozess", "Typ ändern",
            "Prozess löschen", "Bitte wählen Sie einen Prozess aus, den Sie löschen möchten."),
        _ => new Beschriftung(
            "Administration Brauchwasser", "Datenbank Brauchwasserprofile",
            "jährlicher Wärmebedarf:", "MWth",
            "Profil ändern...", "Neues Profil...", "Profiltyp ändern...",
            "Profil löschen", "Bitte wählen Sie zuerst ein Profil aus!")
    };

    private IRenderedComponent<BedarfAdminDialog> Aufbauen(
        BedarfsArt art,
        IReadOnlyList<string>? katalog = null,
        Func<string, BedarfLoeschAusgang>? loeschen = null,
        Func<string, bool>? exists = null,
        Func<string, string>? jahressumme = null,
        Func<string, IReadOnlyDictionary<string, object>?>? vorschau = null,
        Action<bool>? geschlossen = null)
    {
        Beschriftung t = Texte(art);
        IReadOnlyList<string> liste = katalog ?? KATALOG;

        return Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Katalog, () => liste)
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n =>
                n.Length > 0 ? ("Beschreibung " + n, "Typ " + n) : null))
            .Add(x => x.Jahressumme, jahressumme ?? (n => n.Length > 0 ? "123,45" : ""))
            .Add(x => x.Loeschen, loeschen ?? (_ => BedarfLoeschAusgang.Geloescht))
            .Add(x => x.Exists, exists ?? (_ => false))
            .Add(x => x.Vorschau, vorschau)
            .Add(x => x.TitelText, t.Titel)
            .Add(x => x.LabelKatalog, t.Katalog)
            .Add(x => x.LabelJahressumme, t.Jahressumme)
            .Add(x => x.EinheitText, t.Einheit)
            .Add(x => x.BtnAendernText, t.Aendern)
            .Add(x => x.BtnNeuText, t.Neu)
            .Add(x => x.BtnTypAendernText, t.TypAendern)
            .Add(x => x.BtnLoeschenText, t.Loeschen)
            .Add(x => x.MeldungKeineWahl, t.KeineWahl)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    private static IElement Knopf(IRenderedComponent<BedarfAdminDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    public static IEnumerable<object[]> AlleArten() => new[]
    {
        new object[] { BedarfsArt.Stromverbraucher },
        new object[] { BedarfsArt.Prozesswaerme },
        new object[] { BedarfsArt.Brauchwasser }
    };

    // =====================================================================
    // 1 — Feldbestand je Ausprägung
    // =====================================================================

    /// <summary>
    /// Die SIEBEN Knöpfe der Feldkarte je Ausprägung: „ändern", „neu",
    /// „Typ ändern", „löschen", „Grafik", OK und Abbrechen. Einen
    /// „Ergebnisse"-Knopf gab es in KEINEM der drei Designer (Befund W14‑B78); er
    /// wird deshalb nicht nachgebaut (A‑4).
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Die_Maske_zeigt_ihre_sieben_Knoepfe(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art);

        string knoepfe = string.Join("|", cut.FindAll("button").Select(b => b.TextContent.Trim()));

        Assert.Contains(t.Aendern, knoepfe);
        Assert.Contains(t.Neu, knoepfe);
        Assert.Contains(t.TypAendern, knoepfe);
        Assert.Contains(t.Loeschen, knoepfe);
        Assert.Contains("Grafik", knoepfe);
        Assert.Contains("OK", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);

        Assert.DoesNotContain("Ergebnisse", knoepfe);
    }

    /// <summary>
    /// Titel, Listenbeschriftung, die vier Anzeigefelder und das Einheitenkürzel —
    /// je Ausprägung, wörtlich aus dem Designer.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Die_Maske_zeigt_ihre_Beschriftungen(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art);

        Assert.Equal(t.Titel, cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains(t.Katalog, cut.Markup);
        Assert.Contains(t.Jahressumme, cut.Markup);
        Assert.Contains(t.Einheit, cut.Markup);
        Assert.Contains("Name:", cut.Markup);
        Assert.Contains("Beschreibung:", cut.Markup);
        Assert.Contains("Typ:", cut.Markup);
    }

    /// <summary>
    /// Der Katalog steht in der Liste, und die ERSTE Zeile ist gewählt — der
    /// Vorläufer setzte <c>SelectedIndex = 0</c> am Ende von <c>SetControls</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Der_Katalog_steht_in_der_Liste_und_die_erste_Zeile_ist_gewaehlt(BedarfsArt art)
    {
        var cut = Aufbauen(art);

        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Contains("Alpha", cut.Find("tbody").TextContent);
        Assert.Contains("Gamma", cut.Find("tbody").TextContent);

        Assert.Equal("Alpha", cut.Instance.Gewaehlt);
        Assert.Contains("Beschreibung Alpha", cut.Markup);
        Assert.Contains("Typ Alpha", cut.Markup);
        Assert.Equal("123,45", cut.Instance.JahressummeText);
    }

    /// <summary>Ein leerer Katalog lässt die Anzeigefelder leer.</summary>
    [Fact]
    public void Ein_leerer_Katalog_laesst_die_Felder_leer()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, katalog: Array.Empty<string>());

        Assert.Empty(cut.FindAll("tbody tr"));
        Assert.Equal("", cut.Instance.Gewaehlt);
        Assert.Equal("", cut.Instance.JahressummeText);
    }

    /// <summary>
    /// Eine andere Zeile wählen zeigt deren Kopf — <b>EIN</b> Ereignis. Der
    /// Vorläufer hatte beim Brauchwasser und bei der Prozesswärme <c>Click</c> UND
    /// <c>SelectedIndexChanged</c> auf dieselbe Arbeit verdrahtet und lief je
    /// Mausklick zweimal durch (Befund W14‑B52).
    /// </summary>
    [Fact]
    public void Eine_andere_Zeile_zeigt_ihren_Kopf()
    {
        int aufrufe = 0;
        var cut = Aufbauen(BedarfsArt.Brauchwasser,
                           jahressumme: n => { aufrufe++; return n + "-summe"; });

        aufrufe = 0;
        cut.FindAll("tbody tr")[2].QuerySelector("button")!.Click();

        Assert.Equal("Gamma", cut.Instance.Gewaehlt);
        Assert.Equal("Gamma-summe", cut.Instance.JahressummeText);
        Assert.Equal(1, aufrufe);
        Assert.Contains("Beschreibung Gamma", cut.Markup);
    }

    // =====================================================================
    // 2 — Löschen
    // =====================================================================

    /// <summary>
    /// <b>A‑1:</b> Ohne Auswahl meldet die Maske und fragt NICHT. Das Brauchwasser
    /// prüfte als einziges nicht und fragte bei leerer Liste
    /// „Soll  wirklich gelöscht werden ?" (Befund W14‑B51).
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Ohne_Auswahl_meldet_das_Loeschen_statt_zu_fragen(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art, katalog: Array.Empty<string>());

        Knopf(cut, t.Loeschen).Click();

        Assert.Equal(t.KeineWahl, cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }

    /// <summary>
    /// <b>A‑2:</b> EIN Löschsatz mit Platzhalter für alle drei — der Bestand hatte
    /// denselben Satz in drei Schreibweisen (Befund W14‑B64).
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Das_Loeschen_fragt_mit_dem_Namen(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art);

        Knopf(cut, t.Loeschen).Click();

        Assert.Contains("Soll Alpha wirklich gelöscht werden ?", cut.Markup);
    }

    [Fact]
    public void Nein_laesst_den_Satz_stehen()
    {
        int geloescht = 0;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           loeschen: _ => { geloescht++; return BedarfLoeschAusgang.Geloescht; });

        Knopf(cut, Texte(BedarfsArt.Prozesswaerme).Loeschen).Click();
        Knopf(cut, "Nein").Click();

        Assert.Equal(0, geloescht);
        Assert.Equal("", cut.Instance.Meldung);
    }

    [Fact]
    public void Ja_loescht_und_meldet_den_Namen()
    {
        var rest = new List<string> { "Alpha", "Beta", "Gamma" };
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Prozesswaerme)
            .Add(x => x.Katalog, () => rest)
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B " + n, "T " + n)))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Loeschen, n => { rest.Remove(n); return BedarfLoeschAusgang.Geloescht; })
            .Add(x => x.BtnLoeschenText, "Prozess löschen"));

        Knopf(cut, "Prozess löschen").Click();
        Knopf(cut, "Ja").Click();

        Assert.Equal(2, rest.Count);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Contains("\"Alpha\" wurde gelöscht.", cut.Instance.Meldung);
        Assert.Equal("Beta", cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>A‑3:</b> Ein Auslieferungssatz bleibt stehen, und die Meldung steht IM
    /// Dialog. Der Vorläufer liess den Stammcontroller sie über
    /// <c>Meldung.Hinweis</c> zeigen — in einer WebView ein modaler Kasten darüber.
    /// </summary>
    [Fact]
    public void Ein_schreibgeschuetzter_Satz_bleibt_stehen()
    {
        var cut = Aufbauen(BedarfsArt.Brauchwasser,
                           loeschen: _ => BedarfLoeschAusgang.Schreibgeschuetzt);

        Knopf(cut, Texte(BedarfsArt.Brauchwasser).Loeschen).Click();
        Knopf(cut, "Ja").Click();

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.Equal(3, cut.FindAll("tbody tr").Count);
    }

    /// <summary>
    /// Ein fehlgeschlagenes Löschen meldet ebenfalls. Die Prozesswärme färbte dafür
    /// JEDE Ausnahme als „Fehler beim Löschvorgang!" ein — fünf <c>MessageBox</c> in
    /// einem Handler (Befund W14‑B59).
    /// </summary>
    [Fact]
    public void Ein_fehlgeschlagenes_Loeschen_meldet()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           loeschen: _ => BedarfLoeschAusgang.Fehlgeschlagen);

        Knopf(cut, Texte(BedarfsArt.Prozesswaerme).Loeschen).Click();
        Knopf(cut, "Ja").Click();

        Assert.Contains("konnte nicht", cut.Instance.Meldung);
    }

    // =====================================================================
    // 3 — Neu: Namensabfrage → Exists → Stammkopf
    // =====================================================================

    /// <summary>
    /// Die Reihenfolge des Vorläufers, wörtlich: Namensabfrage → <c>Exists</c> →
    /// <c>TypStammHuelle.Neu</c>. Ein belegter Name kommt gar nicht bis zum
    /// Stammkopf.
    /// </summary>
    [Fact]
    public void Ein_belegter_Name_kommt_nicht_bis_zum_Stammkopf()
    {
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.Katalog, () => KATALOG)
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B", "T")))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Exists, n => n == "Alpha")
            .Add(x => x.TypStammGaben, (name, beschr, typ, neu) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>())
            .Add(x => x.BtnNeuText, "Verbraucher in DB neu"));

        Knopf(cut, "Verbraucher in DB neu").Click();

        cut.Find(".epos-ueberlagerung input").Input("Alpha");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Contains("existiert bereits", cut.Instance.Meldung);
        Assert.False(cut.Instance.TypStammOffen);
    }

    // =====================================================================
    // 4 — „Grafik"
    // =====================================================================

    /// <summary>
    /// „Grafik" rechnet vor und zeigt den Ergebnisdialog als ÜBERLAGERUNG — kein
    /// zweites Fenster (Risiko R2).
    /// </summary>
    [Fact]
    public void Grafik_rechnet_vor_und_oeffnet_die_Ueberlagerung()
    {
        string gerechnet = "";
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           vorschau: n =>
                           {
                               gerechnet = n;
                               return new Dictionary<string, object>
                               {
                                   ["Daten"] = new BedarfErgebnisDaten(),
                                   ["TitelText"] = "Simulation Ergebnisse"
                               };
                           });

        Knopf(cut, "Grafik").Click();

        Assert.Equal("Alpha", gerechnet);
        Assert.True(cut.Instance.ErgebnisOffen);
    }

    /// <summary>
    /// Ohne Vorschau-Delegat bleibt der Knopf wirkungslos — und ohne Auswahl
    /// gesperrt.
    /// </summary>
    [Fact]
    public void Ohne_Auswahl_ist_Grafik_gesperrt()
    {
        var cut = Aufbauen(BedarfsArt.Stromverbraucher, katalog: Array.Empty<string>());

        Assert.True(Knopf(cut, "Grafik").HasAttribute("disabled"));
        Assert.True(Knopf(cut, Texte(BedarfsArt.Stromverbraucher).Aendern).HasAttribute("disabled"));
    }

    // =====================================================================
    // 5 — Schluss
    // =====================================================================

    /// <summary>
    /// „OK" liefert OK. Der Vorläufer tat das schon; die Solarganglinie tat es
    /// nicht (Befund W14‑B4).
    /// </summary>
    [Fact]
    public void OK_liefert_OK_und_Abbrechen_liefert_Abbruch()
    {
        bool? antwort = null;
        var cut = Aufbauen(BedarfsArt.Brauchwasser, geschlossen: b => antwort = b);

        Knopf(cut, "OK").Click();
        Assert.True(antwort);

        antwort = null;
        Knopf(cut, "Abbrechen").Click();
        Assert.False(antwort);
    }

    /// <summary>Esc schließt — aber erst, wenn keine Überlagerung offen ist.</summary>
    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Ueberlagerung()
    {
        bool? antwort = null;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, geschlossen: b => antwort = b);

        Knopf(cut, Texte(BedarfsArt.Prozesswaerme).Loeschen).Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);

        Knopf(cut, "Nein").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(antwort);
    }
}
