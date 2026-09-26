using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
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
public class BedarfAdminDialogTests : EposBunitContext
{
    private static readonly string[] KATALOG = { "Alpha", "Beta", "Gamma" };

    /// <summary>
    /// Die Namensliste als <see cref="Katalogfilterzeile"/> — seit Stufe S3.1
    /// (W14a-E-10) traegt die Verwaltung die <c>Katalogliste</c> mit fuenf Spalten
    /// statt eines Rasters mit dem blossen Bezeichner.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Zeilen(IReadOnlyList<string> namen)
    {
        var liste = new List<Katalogfilterzeile>();
        for (int i = 0; i < namen.Count; i++)
            liste.Add(new Katalogfilterzeile(i + 1, namen[i])
                .MitText(Katalogfilterprofil.SpBezeichner, namen[i])
                .MitText(Katalogfilterprofil.SpTyp, "Typ " + namen[i])
                .MitZahl(Katalogfilterprofil.SpJahressummeMwh, 12.5 + i, 3)
                .MitText(Katalogfilterprofil.SpBeschreibung, "Beschreibung " + namen[i])
                .MitKennzeichen(Katalogfilterprofil.SpAuslieferung, false));
        return liste;
    }

    private static Katalogfilterprofil Profil(BedarfsArt art)
        => Katalogfilterprofil.FuerBedarf(art, s => s);

    public BedarfAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
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
            "Verbraucher in DB ändern...", "Verbraucher in DB neu...", "Typ in DB ändern...",
            "Verbraucher in DB löschen", "Bitte wählen Sie zuerst einen Verbraucher aus!"),
        BedarfsArt.Prozesswaerme => new Beschriftung(
            "Prozesswärme Verwaltung", "Datenbank Prozesswärme:",
            "jährlicher Prozesswärmebedarf:", "MWth",
            "Prozess ändern...", "Neuer Prozess...", "Typ ändern...",
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
        Action<bool>? geschlossen = null,
        Func<string, string, string, bool, IReadOnlyDictionary<string, object>>? typStammGaben = null,
        Func<IReadOnlyDictionary<string, object>>? typProfilGaben = null)
    {
        Beschriftung t = Texte(art);
        IReadOnlyList<string> liste = katalog ?? KATALOG;

        return Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Katalogzeilen, () => Zeilen(liste))
            .Add(x => x.Katalogprofil, Profil(art))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n =>
                n.Length > 0 ? ("Beschreibung " + n, "Typ " + n) : null))
            .Add(x => x.Jahressumme, jahressumme ?? (n => n.Length > 0 ? "123,45" : ""))
            .Add(x => x.Loeschen, loeschen ?? (_ => BedarfLoeschAusgang.Geloescht))
            .Add(x => x.Exists, exists ?? (_ => false))
            .Add(x => x.Vorschau, vorschau)
            .Add(x => x.TypStammGaben, typStammGaben)
            .Add(x => x.TypProfilGaben, typProfilGaben)
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
    /// Die SECHS Knöpfe der Feldkarte je Ausprägung: „Grafik…", „Typ ändern…",
    /// „neu…", „ändern…", „löschen" und „Beenden". Einen „Ergebnisse"-Knopf gab es
    /// in KEINEM der drei Designer (Befund W14‑B78); er wird deshalb nicht
    /// nachgebaut (A‑4). „OK" und „Abbrechen" sind mit DL-2 Nr. 3 entfallen — jede
    /// Aktion schreibt sofort in den Katalog.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Die_Maske_zeigt_ihre_sechs_Knoepfe(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art);

        string knoepfe = string.Join("|", cut.FindAll("button").Select(b => b.TextContent.Trim()));

        // Stufe 4 (A5): „ändern…" ist entfallen - die Kenndaten sind direkt bedienbar.
        Assert.DoesNotContain(t.Aendern, knoepfe);
        Assert.Contains(t.Neu, knoepfe);
        Assert.Contains(t.TypAendern, knoepfe);
        Assert.Contains(t.Loeschen, knoepfe);
        Assert.Contains("Grafik...", knoepfe);
        Assert.Contains("Beenden", knoepfe);

        Assert.DoesNotContain("Ergebnisse", knoepfe);
    }

    // =====================================================================
    // 1a — Die eine Fussleiste (DL-2 Nr. 3, Konzept Abschnitt 2 Zeile 3)
    // =====================================================================

    /// <summary>
    /// Die Fußleiste der Verwaltung (Stufe 3): <b>Füller/Statuszeile · Neu… · Beenden</b>.
    /// „Grafik…" und „Typ ändern…" stehen im Kopf der Gruppe Wochenprofil, „Ändern…" im
    /// Kopf der Kenndaten, Duplizieren… und Löschen in der Auswahlleiste (V8: keine
    /// Handlung an zwei Orten). Eine zweite Leiste gibt es nicht.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Die_Fussleiste_traegt_das_Katalogmuster(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art);

        var leisten = cut.FindAll(".epos-leiste");
        Assert.Single(leisten);

        var leiste = leisten[0];
        var knoepfe = leiste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { t.Neu, "Beenden" }, knoepfe);

        // Der Fueller ist die Statuszeile und steht vorn.
        var kinder = leiste.Children.Select(e => e.ClassName ?? "").ToList();
        Assert.Single(leiste.QuerySelectorAll(".epos-leiste-fueller"));
        Assert.Equal(0, kinder.FindIndex(k => k.Contains("epos-leiste-fueller")));
        Assert.Equal("status", leiste.QuerySelector(".epos-leiste-fueller")!.GetAttribute("role"));

        var gruppen = cut.FindAll(".epos-stammblattgruppe-kopf").Select(k => k.TextContent).ToList();
        Assert.Contains(gruppen, g => g.Contains("Grafik...") && g.Contains(t.TypAendern));
        Assert.DoesNotContain(gruppen, g => g.Contains(t.Aendern));
        Assert.Contains(t.Loeschen, cut.Find(".epos-auswahlleiste").TextContent);

        var primaer = leiste.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("Beenden", primaer[0].TextContent.Trim());

        // Die zweite Leiste (Status . Abbrechen . OK) ist entfallen; die eine
        // Statuszeile steht in der Fussleiste.
        Assert.Single(cut.FindAll(".epos-status"));
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
        // Die Ueberschrift ueber der Liste (t.Katalog) entfaellt seit Stufe 3 - sie nahm
        // der Liste eine Zeile; die Liste nennt sich ueber ihre Spaltenkoepfe.
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
        Zeilenklick.Zeile(cut, 2);

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
    /// <remarks>
    /// Seit Stufe 3 (V8) steht „Löschen" in der Auswahlleiste, und die zeigt ohne Zeile
    /// nur ihre leise Zeile — es gibt keinen Knopf, der ins Leere drückt, und damit auch
    /// keine Rückfrage „Soll  wirklich gelöscht werden ?".
    /// </remarks>
    [Theory]
    [MemberData(nameof(AlleArten))]
    public void Ohne_Auswahl_gibt_es_kein_Loeschen(BedarfsArt art)
    {
        Beschriftung t = Texte(art);
        var cut = Aufbauen(art, katalog: Array.Empty<string>());

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == t.Loeschen);
        Assert.Single(cut.FindAll(".epos-auswahlleiste-leise"));
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
            .Add(x => x.Katalogzeilen, () => Zeilen(rest))
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Prozesswaerme))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B " + n, "T " + n)))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Loeschen, n => { rest.Remove(n); return BedarfLoeschAusgang.Geloescht; })
            .Add(x => x.BtnLoeschenText, "Prozess löschen"));

        Knopf(cut, "Prozess löschen").Click();
        Knopf(cut, "Ja").Click();

        Assert.Equal(2, rest.Count);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        // Gelungenes steht seit Stufe 3 in der Statuszeile, nicht im Warnband (V11).
        Assert.Contains("\"Alpha\" wurde gelöscht.", cut.Instance.Status);
        Assert.Equal("", cut.Instance.Meldung);
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
            .Add(x => x.Katalogzeilen, () => Zeilen(KATALOG))
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Stromverbraucher))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B", "T")))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Exists, n => n == "Alpha")
            .Add(x => x.TypStammGaben, (name, beschr, typ, neu) =>
                (IReadOnlyDictionary<string, object>)new Dictionary<string, object>())
            .Add(x => x.BtnNeuText, "Verbraucher in DB neu..."));

        Knopf(cut, "Verbraucher in DB neu...").Click();

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

        Knopf(cut, "Grafik...").Click();

        Assert.Equal("Alpha", gerechnet);
        Assert.True(cut.Instance.ErgebnisOffen);
    }

    /// <summary>
    /// Ohne Auswahl zeigt das Stammblatt „Keine Zeile gewählt." — seine Gruppen und mit
    /// ihnen „Grafik…" und „Ändern…" stehen erst mit einer Zeile da (Stufe 3, V9).
    /// </summary>
    [Fact]
    public void Ohne_Auswahl_stehen_Grafik_und_Aendern_nicht_da()
    {
        var cut = Aufbauen(BedarfsArt.Stromverbraucher, katalog: Array.Empty<string>());

        var texte = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Grafik...", texte);
        Assert.DoesNotContain(Texte(BedarfsArt.Stromverbraucher).Aendern, texte);
        Assert.Single(cut.FindAll(".epos-stammblatt-leer"));
    }

    // =====================================================================
    // 5 — Schluss
    // =====================================================================

    /// <summary>
    /// „Beenden" ist der eine Schlussweg (DL-2 Nr. 3) und meldet <c>true</c>. Einen
    /// Abbruchweg gibt es nicht: Stammkopf, Wochenprofil und Löschen schreiben
    /// sofort in den Katalog — „Abbrechen" nahm nichts zurück.
    /// </summary>
    [Fact]
    public void Beenden_ist_der_eine_Schlussweg()
    {
        bool? antwort = null;
        var cut = Aufbauen(BedarfsArt.Brauchwasser, geschlossen: b => antwort = b);

        Assert.DoesNotContain("Abbrechen", cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList());

        Knopf(cut, "Beenden").Click();
        Assert.True(antwort);
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
        Assert.True(antwort);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc und „Beenden".</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? antwort = null;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, geschlossen: b => antwort = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.True(antwort);
    }

    /// <summary>
    /// Die drei Ueberlagerungen (Stammkopf, Profil, Ergebnis) trugen bislang kein ✕
    /// (<c>Schliessbar="false"</c>) — jetzt schließt ihr Kreuz wie „Abbrechen": Die
    /// Ueberlagerung geht wieder zu.
    /// </summary>
    [Fact]
    public void Ueberlagerungskreuz_schliesst_den_Ergebnisdialog()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           vorschau: n => new Dictionary<string, object>
                           {
                               ["Daten"] = new BedarfErgebnisDaten(),
                               ["TitelText"] = "Simulation Ergebnisse"
                           });

        Knopf(cut, "Grafik...").Click();
        Assert.True(cut.Instance.ErgebnisOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.ErgebnisOffen);
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Ergebnis-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>BedarfErgebnisDialog</c> (<c>TitelAnzeigen="false"</c>) keins von
    /// beidem — sonst stünden zwei Kreuze und zwei Titel übereinander.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Ergebnis_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           vorschau: _ => new Dictionary<string, object>
                           {
                               ["Daten"] = new BedarfErgebnisDaten(),
                               ["TitelText"] = "Simulation Ergebnisse"
                           });

        Knopf(cut, "Grafik...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Stammkopf-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>TypStammDialog</c> (<c>TitelText=""</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Stammkopf_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           typStammGaben: (_, _, _, _) => new Dictionary<string, object>());

        // Seit Stufe 4 oeffnet der Stammkopf nur noch fuer "Neu..." (die Kenndaten sind
        // direkt bedienbar): Namensabfrage, dann der Stammkopf.
        Knopf(cut, "Neuer Prozess...").Click();
        cut.Find(".epos-ueberlagerung input").Input("Delta");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(cut.Instance.TypStammOffen);

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Profil-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>TypProfilDialog</c> (<c>TitelAnzeigen="false"</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Typprofil_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           typProfilGaben: () => new Dictionary<string, object>());

        Knopf(cut, "Typ ändern...").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>BedarfAdminKiSicht</c>: Die Listenwahl ist ein WAHLFELD, und ein
    /// Setzen zieht den Infoblock nach — derselbe Weg wie ein Klick in die Liste.
    /// </summary>
    [Theory]
    [InlineData(BedarfsArt.Prozesswaerme, KiMaskennamen.PROZESSWAERME_ADMIN)]
    [InlineData(BedarfsArt.Stromverbraucher, KiMaskennamen.STROMVERBRAUCHER_ADMIN)]
    [InlineData(BedarfsArt.Brauchwasser, KiMaskennamen.BRAUCHWASSER_ADMIN)]
    public void Jede_Auspraegung_meldet_sich_unter_ihrem_eigenen_Namen_an(
        BedarfsArt art, string maske)
    {
        var cut = Aufbauen(art);

        Assert.True(KiMaskenbruecke.IstAngemeldet(maske));

        WindowsFormsApplication1.KiFeldzugang satz =
            KiMaskenbruecke.Feldzugang(maske, "satz");
        Assert.NotNull(satz);
        Assert.Equal(KATALOG[0], satz.Lesen());

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(satz, KATALOG[2]);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        satz.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(KATALOG[2], cut.Instance.Gewaehlt);

        // Das Stammblatt zieht nach - es ist der Grund, warum diese Maske trotz
        // KI-D-Q5 im Katalog steht. Seit der Welle #456 ist der Typ eine EINGABE.
        WindowsFormsApplication1.KiFeldzugang typ = KiMaskenbruecke.Feldzugang(maske, "typ");
        Assert.NotNull(typ);
        Assert.True(typ.Setzbar);
        Assert.True(typ.IstWahl);
        Assert.Equal("Typ " + KATALOG[2], typ.Lesen());

        WindowsFormsApplication1.KiFeldzugang bedarfsart =
            KiMaskenbruecke.Feldzugang(maske, "bedarfsart");
        Assert.Equal(art.ToString(), bedarfsart.Lesen());
    }

    // =====================================================================
    // Stufe 3 (Konzept Administrationsdialoge): Stammblatt, Auswahlleiste,
    // Duplizieren (V3 V6 V8 V9 V12 V13)
    // =====================================================================

    /// <summary>
    /// <b>Das Stammblatt der Bedarfsprofile</b> (V9): die Gruppe „Wochenprofil" sagt, dass
    /// es zum Typ gehört, und trägt „Grafik…" und „Typ ändern…"; die Kenndaten sind seit
    /// Stufe 4 direkt bedienbar; der Kopf nennt Typ und Herkunft.
    /// </summary>
    [Fact]
    public void Das_Stammblatt_traegt_Wochenprofil_und_Kenndaten()
    {
        var cut = Aufbauen(BedarfsArt.Brauchwasser);

        Assert.Equal(new[] { "Wochenprofil", "Kenndaten" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent));
        Assert.Contains("Typ Alpha", cut.Find(".epos-stammblatt-erklaerung").TextContent);
        Assert.Equal("Typ Alpha · eigener Satz", cut.Find(".epos-stammblatt-unter").TextContent);
    }

    // =====================================================================
    // Stufe 4 (Konzept Administrationsdialoge, A5): die Kenndaten direkt im
    // Stammblatt - Speichern und Verwerfen wie bei den Geraetekatalogen
    // =====================================================================

    /// <summary>Der Dialog mit den Wegen der direkt bedienbaren Kenndaten.</summary>
    private IRenderedComponent<BedarfAdminDialog> MitKenndaten(
        List<(string Name, string Typ, string Beschreibung, double[] Monate)> geschrieben,
        bool geschuetzt = false, Func<string, KatalogSpeicherErgebnis>? ergebnis = null,
        Action<bool>? geschlossen = null)
        => Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.Katalogzeilen, () => KATALOG.Select((n, i) =>
                new Katalogfilterzeile(i + 1, n) { Geschuetzt = geschuetzt }
                    .MitText(Katalogfilterprofil.SpBezeichner, n)).ToList())
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Stromverbraucher))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("Beschreibung " + n, "Buero")))
            .Add(x => x.Jahressumme, n => "12,00")
            .Add(x => x.Typen, () => new[] { "Buero", "Werkstatt" })
            .Add(x => x.Monatswerte, n => Enumerable.Repeat(1.0, 12).ToArray())
            .Add(x => x.Monatsnamen, Enumerable.Range(1, 12).Select(m => "Monat " + m + ":").ToArray())
            .Add(x => x.Speichern, (name, typ, beschr, monate) =>
            {
                geschrieben.Add((name, typ, beschr, monate));
                return ergebnis?.Invoke(name) ?? new KatalogSpeicherErgebnis(true, "", name);
            })
            .Add(x => x.MeldungTypFehlt, "Verbrauchertyp auswählen!")
            .Add(x => x.BtnNeuText, "Verbraucher in DB neu...")
            .Add(x => x.BtnLoeschenText, "Verbraucher in DB löschen")
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    /// <summary>
    /// <b>Direkt bedienbar statt „Ändern…"</b>: Typ und Beschreibung stehen als Felder in
    /// den Kenndaten, die zwölf Monatswerte in der eigenen Gruppe; die Fußleiste trägt
    /// Speichern · Verwerfen · Statuszeile · Neu… · Beenden. Ohne Änderung sind Speichern
    /// und Verwerfen gesperrt.
    /// </summary>
    [Fact]
    public void Die_Kenndaten_sind_direkt_bedienbar()
    {
        var geschrieben = new List<(string, string, string, double[])>();
        var cut = MitKenndaten(geschrieben);

        Assert.Equal(new[] { "Wochenprofil", "Kenndaten", "Monatswerte" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent));
        Assert.Single(cut.FindAll(".epos-stammblatt select"));
        Assert.Single(cut.FindAll(".epos-stammblatt textarea"));
        Assert.Equal(12, cut.FindAll(".epos-stammblatt input[inputmode=decimal]").Count);

        var leiste = cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Verbraucher in DB neu...", "Beenden" },
                     leiste.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Verwerfen").HasAttribute("disabled"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("ändern...") && b.TextContent.Contains("Verbraucher"));
    }

    /// <summary>
    /// <b>Speichern schreibt den Arbeitsstand</b> über den Weg der Hülle: Typ,
    /// Beschreibung und die zwölf Monatswerte; danach ist der Stand gespeichert und die
    /// Statuszeile meldet es.
    /// </summary>
    [Fact]
    public void Speichern_schreibt_Typ_Beschreibung_und_Monatswerte()
    {
        var geschrieben = new List<(string Name, string Typ, string Beschreibung, double[] Monate)>();
        var cut = MitKenndaten(geschrieben);

        cut.Find(".epos-stammblatt select").Change("1");                 // Werkstatt
        cut.Find(".epos-stammblatt textarea").Input("neu beschrieben");
        cut.FindAll(".epos-stammblatt input[inputmode=decimal]")[2].Input("3,5");

        Assert.True(cut.Instance.Geaendert);
        Assert.Equal("3 Felder geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Knopf(cut, "Speichern").Click();

        var satz = Assert.Single(geschrieben);
        Assert.Equal("Alpha", satz.Name);
        Assert.Equal("Werkstatt", satz.Typ);
        Assert.Equal("neu beschrieben", satz.Beschreibung);
        Assert.Equal(3.5, satz.Monate[2]);
        Assert.Equal(1.0, satz.Monate[0]);
        Assert.False(cut.Instance.Geaendert);
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Geänderte Felder halten an</b> (Konzept 3.3): Zeilenwechsel, Neu… und Beenden
    /// melden „Speichern oder Verwerfen"; Verwerfen nimmt den Arbeitsstand zurück.
    /// </summary>
    [Fact]
    public void Geaenderte_Felder_halten_Zeilenwechsel_und_Beenden_an()
    {
        bool? geschlossen = null;
        var geschrieben = new List<(string, string, string, double[])>();
        var cut = MitKenndaten(geschrieben, geschlossen: b => geschlossen = b);

        cut.Find(".epos-stammblatt textarea").Input("geaendert");
        Zeilenklick.Zeile(cut, 1);
        Assert.Equal("Alpha", cut.Instance.Gewaehlt);
        Assert.Contains("Speichern", cut.Instance.Meldung);

        Knopf(cut, "Beenden").Click();
        Assert.Null(geschlossen);

        Knopf(cut, "Verwerfen").Click();
        Assert.False(cut.Instance.Geaendert);
        Assert.Contains("Beschreibung Alpha", cut.Find(".epos-stammblatt textarea").TextContent);

        Zeilenklick.Zeile(cut, 1);
        Assert.Equal("Beta", cut.Instance.Gewaehlt);
        Assert.Empty(geschrieben);
    }

    /// <summary>
    /// <b>Die Prüfregeln des Stammkopfes</b>: Der Typ ist Pflicht, ein leerer Monatswert
    /// hält an und nennt das Feld; geschrieben wird dann nichts.
    /// </summary>
    [Fact]
    public void Speichern_prueft_Typ_und_Monatswerte()
    {
        var geschrieben = new List<(string, string, string, double[])>();
        var cut = MitKenndaten(geschrieben);

        cut.FindAll(".epos-stammblatt input[inputmode=decimal]")[4].Input("");
        Knopf(cut, "Speichern").Click();

        Assert.Empty(geschrieben);
        Assert.Contains("Monat 5", cut.Instance.Meldung);
        Assert.True(cut.Instance.Geaendert);

        // Der Grund steht auch ROT in der Statuszeile neben dem Knopf.
        var status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Equal(cut.Instance.Meldung, status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);
    }

    // =====================================================================
    // Welle #456: die Kenndaten über den Hilfe-Assistenten
    // =====================================================================

    /// <summary>
    /// <b>Der Assistent setzt einen Monatswert und den Typ und speichert</b> — über
    /// denselben Arbeitsstand wie die Felder des Stammblatts und denselben Schreibweg wie
    /// der Knopf; die Prüfung ist die des Knopfes.
    /// </summary>
    [Fact]
    public async Task Der_Assistent_setzt_Monatswert_und_Typ_und_speichert_ueber_den_Knopfweg()
    {
        var geschrieben = new List<(string Name, string Typ, string Beschreibung, double[] Monate)>();
        var cut = MitKenndaten(geschrieben);
        const string maske = KiMaskennamen.STROMVERBRAUCHER_ADMIN;

        WindowsFormsApplication1.KiFeldzugang maerz = KiMaskenbruecke.Feldzugang(maske, "maerz")!;
        Assert.True(maerz.Setzbar);
        Assert.Equal(1.0, maerz.Lesen());

        KiFeldumsetzung u = KiFeldwandler.Wandle(maerz, "3,5");
        Assert.True(u.Ok, u.Grund);
        maerz.Setzen(u.Wert);
        cut.Render();
        Assert.True(cut.Instance.Geaendert);

        WindowsFormsApplication1.KiFeldzugang typ = KiMaskenbruecke.Feldzugang(maske, "typ")!;
        Assert.Equal(2, typ.Wahleintraege().Count);
        KiFeldumsetzung ut = KiFeldwandler.Wandle(typ, "werkstatt");
        Assert.True(ut.Ok, ut.Grund);
        typ.Setzen(ut.Wert);
        cut.Render();

        KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
        Assert.False(haken.IstSchreibgeschuetzt());
        Assert.Equal("", haken.Befund());

        KiKern.KiErgebnis ok = await haken.Speichern();

        Assert.Equal(KiKern.KiStatus.Ausgefuehrt, ok.Status);
        var satz = Assert.Single(geschrieben);
        Assert.Equal("Alpha", satz.Name);
        Assert.Equal("Werkstatt", satz.Typ);
        Assert.Equal(3.5, satz.Monate[2]);
        Assert.Equal(1.0, satz.Monate[0]);
        Assert.False(cut.Instance.Geaendert);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz ist für den Assistenten geschützt</b> — der Grund nennt
    /// „Duplizieren…"; die Wahl eines anderen Satzes bleibt frei (Satzwahl).
    /// </summary>
    [Fact]
    public async Task Ein_Auslieferungssatz_ist_fuer_den_Assistenten_geschuetzt()
    {
        var geschrieben = new List<(string, string, string, double[])>();
        var cut = MitKenndaten(geschrieben, geschuetzt: true);
        const string maske = KiMaskennamen.STROMVERBRAUCHER_ADMIN;

        KiMaskenhaken haken = KiMaskenbruecke.Haken(maske);
        Assert.True(haken.IstSchreibgeschuetzt());
        Assert.Contains("Duplizieren", haken.Schutzgrund());

        KiKern.KiErgebnis abgelehnt = await haken.Speichern();
        Assert.Equal(KiKern.KiStatus.Abgelehnt, abgelehnt.Status);
        Assert.Empty(geschrieben);

        WindowsFormsApplication1.KiFeldzugang satz = KiMaskenbruecke.Feldzugang(maske, "satz")!;
        Assert.True(satz.Feld.Satzwahl);
        satz.Setzen("Beta");
        cut.Render();
        Assert.Equal("Beta", cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>Ungespeicherte Änderungen halten den Satzwechsel an</b> — auch über den
    /// Assistenten, und zwar BENANNT: Der Setzer wirft mit dem Text des Warnbands, statt
    /// still stehen zu bleiben.
    /// </summary>
    [Fact]
    public void Der_Satzwechsel_mit_ungespeicherten_Aenderungen_wird_benannt_abgelehnt()
    {
        var geschrieben = new List<(string, string, string, double[])>();
        var cut = MitKenndaten(geschrieben);
        const string maske = KiMaskennamen.STROMVERBRAUCHER_ADMIN;

        KiMaskenbruecke.Feldzugang(maske, "beschreibung")!.Setzen("von Hand geaendert");
        cut.Render();
        Assert.True(cut.Instance.Geaendert);

        var fehler = Assert.Throws<InvalidOperationException>(
            () => KiMaskenbruecke.Feldzugang(maske, "satz")!.Setzen("Beta"));
        Assert.Contains("Speichern", fehler.Message);
        Assert.Equal("Alpha", cut.Instance.Gewaehlt);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz zeigt seine Werte als TEXT</b> (V13, Stufe 4): keine
    /// gesperrten Felder, sondern Name und Wert; Speichern ist weich gesperrt mit Grund.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_zeigt_Kenndaten_und_Monatswerte_als_Text()
    {
        var geschrieben = new List<(string, string, string, double[])>();
        var cut = MitKenndaten(geschrieben, geschuetzt: true);

        var gruppen = cut.FindAll(".epos-stammblattgruppe--lesen");
        Assert.Equal(2, gruppen.Count);
        Assert.Empty(cut.FindAll(".epos-stammblatt select, .epos-stammblatt textarea, .epos-stammblatt input"));
        var namen = cut.FindAll(".epos-stammblattwert dt").Select(e => e.TextContent).ToList();
        Assert.Contains("Typ", namen);
        Assert.Contains("Beschreibung", namen);
        Assert.Contains("Monat 1", namen);
        Assert.Contains(cut.FindAll(".epos-stammblattwert dd"), d => d.TextContent == "1 MWh");

        IElement speichern = Knopf(cut, "Speichern");
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        speichern.Click();
        Assert.Contains("Duplizieren", cut.Instance.Meldung);
        Assert.Empty(geschrieben);
    }

    /// <summary>
    /// <b>Duplizieren für die Bedarfsprofile</b> (zurückgestellt aus Stufe 2): Die
    /// Namensabfrage ist mit „Name (Kopie)" vorbelegt, der Weg bekommt die ID, danach ist
    /// die Kopie gewählt und die Statuszeile nennt beide. Ohne den Weg keine Handlung.
    /// </summary>
    [Fact]
    public void Duplizieren_legt_das_eigene_Profil_an()
    {
        Assert.DoesNotContain(Aufbauen(BedarfsArt.Prozesswaerme).FindAll(".epos-auswahlleiste button"),
                              b => b.TextContent.Trim() == "Duplizieren...");

        var namen = new List<string>(KATALOG);
        (int, string)? gerufen = null;
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Prozesswaerme)
            .Add(x => x.Katalogzeilen, () => Zeilen(namen))
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Prozesswaerme))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B " + n, "T " + n)))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Duplizieren, (id, name) =>
            {
                gerufen = (id, name);
                namen.Add(name);
                return new KatalogSpeicherErgebnis(true, "", name);
            }));

        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == "Duplizieren...").Click();
        Assert.True(cut.Instance.Duplizierfrage);
        Assert.Equal("Alpha (Kopie)", cut.Find(".epos-ueberlagerung input[type=text]").GetAttribute("value"));
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal((1, "Alpha (Kopie)"), gerufen);
        Assert.Equal("Alpha (Kopie)", cut.Instance.Gewaehlt);
        Assert.Contains("dupliziert", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Ein Auslieferungssatz trägt das Schloss, Löschen ist weich gesperrt</b> (V10,
    /// V13) — der Grund steht am Knopf, ein Klick meldet ihn, es gibt keine Rückfrage.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_sperrt_Loeschen_weich()
    {
        int geloescht = 0;
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Brauchwasser)
            .Add(x => x.Katalogzeilen, () => new[]
            {
                new Katalogfilterzeile(1, "Alpha") { Geschuetzt = true }
                    .MitText(Katalogfilterprofil.SpBezeichner, "Alpha")
            })
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Brauchwasser))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B", "T")))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Loeschen, _ => { geloescht++; return BedarfLoeschAusgang.Geloescht; })
            .Add(x => x.BtnLoeschenText, "Profil löschen"));

        Assert.NotNull(cut.Find(".epos-katalogliste tbody .epos-schloss"));
        Assert.NotNull(cut.Find(".epos-stammblatt-schutz"));

        var loeschen = Knopf(cut, "Profil löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        loeschen.Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("Löschen gesperrt", cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }
    // =================================================================================
    // „Schloss setzen…" / „Schloss aufheben…" (Entscheid AD-Q15)
    // =================================================================================

    /// <summary>
    /// <b>Das Schloss eines Bedarfsprofils aufheben</b> (AD-Q15): Die Rückfrage sagt, dass
    /// das Wochenprofil beim gesperrten Typ gesperrt bleibt (das Typ-Schloss ist getrennt);
    /// nach dem „Ja" sind die Kenndaten bedienbar und „Speichern" schreibt.
    /// </summary>
    [Fact]
    public void Schloss_aufheben_nennt_das_Typschloss_und_gibt_Speichern_frei()
    {
        var schloss = new Schlosspruefung(1);
        var geschrieben = new List<string>();
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Brauchwasser)
            .Add(x => x.Katalogzeilen, () => schloss.Markieren(new[]
            {
                new Katalogfilterzeile(1, "Alpha")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Alpha")
                    .MitText(Katalogfilterprofil.SpTyp, "EFH"),
                new Katalogfilterzeile(2, "Beta")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Beta")
                    .MitText(Katalogfilterprofil.SpTyp, "MFH")
            }))
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Brauchwasser))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("Beschreibung " + n, "EFH")))
            .Add(x => x.Jahressumme, n => "1")
            .Add(x => x.Typen, () => new[] { "EFH", "MFH" })
            .Add(x => x.Monatswerte, n => Enumerable.Repeat(1.0, 12).ToArray())
            .Add(x => x.Monatsnamen, Enumerable.Range(1, 12).Select(m => "Monat " + m + ":").ToArray())
            .Add(x => x.Speichern, (name, typ, beschr, monate) =>
            {
                geschrieben.Add(name);
                return new KatalogSpeicherErgebnis(true, "", name);
            })
            .Add(x => x.Schloss, schloss.Weg())
            .Add(x => x.TypGesperrt, typ => typ == "EFH")
            .Add(x => x.BtnLoeschenText, "Profil löschen"));

        Assert.Equal(new[] { "Vergleichen", "Schloss aufheben...", "Profil löschen" },
                     Schlosspruefung.Handlungen(cut));

        Schlosspruefung.Knopf(cut).Click();
        Assert.EndsWith("Das Wochenprofil gehört zum Typ „EFH“ und bleibt gesperrt.", Schlosspruefung.Frage(cut));
        Schlosspruefung.Ja(cut);

        Assert.Equal(new[] { 1 }, schloss.Aufrufe.Single().Ids);
        Assert.True(Schlosspruefung.Band(cut));
        Assert.Empty(cut.FindAll(".epos-stammblatt-name .epos-schloss"));

        cut.Find(".epos-stammblatt textarea").Input("neu");
        var speichern = cut.FindAll(".epos-leiste .epos-knopf")[0];
        Assert.Null(speichern.GetAttribute("aria-disabled"));
        speichern.Click();
        Assert.Equal(new[] { "Alpha" }, geschrieben);
    }
}
