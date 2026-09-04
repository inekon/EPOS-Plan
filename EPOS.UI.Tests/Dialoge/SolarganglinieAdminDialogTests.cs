using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung der Solarthermie-Ganglinien (iU9-W14b.2). Soll ist die Feldkarte von
/// <c>Form_Solarganglinie_Admin</c> (11 Steuerelemente: 6 Knöpfe, 3 Beschriftungen,
/// 2 Textfelder, 1 Liste, 1 Gruppenrahmen).
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class SolarganglinieAdminDialogTests : BunitContext
{
    private static readonly List<SolarganglinieAdminDialog.Katalogzeile> KATALOG = new()
    {
        new(1, "Tsol1", "Leistung Solarsystem [W]", false),
        new(2, "Auslieferung Sued", "Referenzjahr 2020", true),
        new(3, "Messreihe Nord", "Standort Nord", false)
    };

    public SolarganglinieAdminDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>DeutscheOberflaeche</c>, Regel seit W8).
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

    private IRenderedComponent<SolarganglinieAdminDialog> Aufbauen(
        Func<string, Task<bool>>? hatZuordnung = null,
        Func<string, Task<bool>>? loeschen = null,
        Func<string, Task<string?>>? dateiWaehlen = null,
        Func<string, Task<AblageErgebnis>>? ablegen = null,
        Func<string, Task<bool>>? mitSystem = null,
        Func<string, IProgress<ImportFortschritt>, Task<SolarganglinieImportErgebnis>>? einlesen = null,
        List<SolarganglinieAdminDialog.Katalogzeile>? katalog = null,
        Action<bool>? geschlossen = null)
    {
        List<SolarganglinieAdminDialog.Katalogzeile> liste = katalog ?? KATALOG;

        return Render<SolarganglinieAdminDialog>(p => p
            .Add(x => x.Katalog, () => Task.FromResult(new List<SolarganglinieAdminDialog.Katalogzeile>(liste)))
            .Add(x => x.HatProjektzuordnung, hatZuordnung ?? (_ => Task.FromResult(false)))
            .Add(x => x.Loeschen, loeschen ?? (_ => Task.FromResult(true)))
            .Add(x => x.DateiWaehlen, dateiWaehlen)
            .Add(x => x.Ablegen, ablegen)
            .Add(x => x.MitSystemOeffnen, mitSystem)
            .Add(x => x.Einlesen, einlesen)
            .Add(x => x.Ordner, @"D:\VDI-3805-Daten\Solarthermie")
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    private static IElement Knopf(IRenderedComponent<SolarganglinieAdminDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =====================================================================
    // 1 — Feldbestand
    // =====================================================================

    /// <summary>
    /// Die Knöpfe der Feldkarte, wörtlich: „Datei Auswählen…", „Datei bearbeiten…",
    /// „Datei Einlesen…", „Ganglinie Löschen", „OK". Der sechste Knopf des
    /// Vorläufers — <c>btn_Hilfe</c> („Hilfe"/„Help") — hatte KEINEN Click-Handler
    /// (Befund W14‑B74) und ist hier der <c>InfoKnopf</c>.
    /// </summary>
    [Fact]
    public void Die_Maske_zeigt_ihre_Knoepfe_und_den_Ordner()
    {
        var cut = Aufbauen(dateiWaehlen: _ => Task.FromResult<string?>(""));

        Assert.Equal("Solarthermie Ganglinie", cut.Find(".epos-dialog-titel").TextContent);

        string knoepfe = string.Join("|", cut.FindAll("button").Select(b => b.TextContent.Trim()));
        Assert.Contains("Datei Auswählen...", knoepfe);
        Assert.Contains("Datei bearbeiten...", knoepfe);
        Assert.Contains("Datei Einlesen...", knoepfe);
        Assert.Contains("Ganglinie Löschen", knoepfe);
        Assert.Contains("OK", knoepfe);

        // Der Hilfeknopf ist der InfoKnopf, kein eigener Knopf mit totem Handler.
        Assert.DoesNotContain("Hilfe", knoepfe);

        Assert.Contains("Ganglinien in DB", cut.Markup);
        Assert.Contains("Ganglinie aus Datei Einlesen", cut.Markup);
        Assert.Contains("Stundenwerte über 1 Jahr als Textdatei", cut.Markup);
    }

    /// <summary>
    /// <b>A‑6:</b> Der Ordner steht sichtbar da. Im Bestand war <c>textBox_Ordner</c>
    /// samt Beschriftung <c>Visible = False</c> (Befund W14‑B79) — man sah nicht,
    /// wohin die Datei kopiert wird.
    /// </summary>
    [Fact]
    public void Der_Ganglinienordner_ist_sichtbar()
    {
        var cut = Aufbauen();

        Assert.Contains("Datei Basis Ordner:", cut.Markup);
        Assert.Contains(@"D:\VDI-3805-Daten\Solarthermie", cut.Markup);
    }

    /// <summary>Der Katalog steht in der Liste — mit Bezeichner UND Beschreibung.</summary>
    [Fact]
    public void Der_Katalog_steht_in_der_Liste()
    {
        var cut = Aufbauen();

        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Contains("Tsol1", cut.Find("tbody").TextContent);
        Assert.Contains("Leistung Solarsystem [W]", cut.Find("tbody").TextContent);
        Assert.Contains("Messreihe Nord", cut.Find("tbody").TextContent);
    }

    // =====================================================================
    // 2 — Löschen
    // =====================================================================

    /// <summary>Ohne Auswahl bleibt der Löschknopf gesperrt.</summary>
    [Fact]
    public void Ohne_Auswahl_ist_der_Loeschknopf_gesperrt()
    {
        var cut = Aufbauen();

        Assert.Equal("", cut.Instance.Gewaehlt);
        Assert.True(Knopf(cut, "Ganglinie Löschen").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>A‑5:</b> Vor dem Löschen wird gefragt — mit dem Namen. Der Vorläufer
    /// löschte ohne Rückfrage (Befund W14‑B68).
    /// </summary>
    [Fact]
    public void Das_Loeschen_fragt_mit_dem_Namen()
    {
        var cut = Aufbauen();

        cut.FindAll("tbody tr")[0].QuerySelector("button")!.Click();
        Knopf(cut, "Ganglinie Löschen").Click();

        Assert.Contains("Soll Tsol1 wirklich gelöscht werden ?", cut.Markup);
    }

    /// <summary>
    /// Eine zugeordnete Ganglinie bleibt stehen — die Sperre fragt seit W14b.0d die
    /// Datenbank mit <c>COUNT(*)</c> statt mit verkettetem inline-SQL über den
    /// Anwendertext (Befund W14‑B12).
    /// </summary>
    [Fact]
    public void Eine_zugeordnete_Ganglinie_bleibt_stehen()
    {
        int geloescht = 0;
        var cut = Aufbauen(hatZuordnung: _ => Task.FromResult(true),
                           loeschen: _ => { geloescht++; return Task.FromResult(true); });

        cut.FindAll("tbody tr")[0].QuerySelector("button")!.Click();
        Knopf(cut, "Ganglinie Löschen").Click();

        Assert.Equal(0, geloescht);
        Assert.Contains("Projektzuordnung", cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }

    /// <summary>Ein Auslieferungssatz bleibt stehen, und es wird gar nicht erst gefragt.</summary>
    [Fact]
    public void Ein_schreibgeschuetzter_Satz_bleibt_stehen()
    {
        var cut = Aufbauen();

        cut.FindAll("tbody tr")[1].QuerySelector("button")!.Click();
        Knopf(cut, "Ganglinie Löschen").Click();

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.DoesNotContain("wirklich gelöscht", cut.Markup);
    }

    /// <summary>
    /// <b>A‑5:</b> „Ja" löscht und wertet den Rückgabewert aus — der Vorläufer
    /// prüfte ihn nicht (Befund W14‑B68).
    /// </summary>
    [Fact]
    public void Ja_loescht_und_meldet()
    {
        var rest = new List<SolarganglinieAdminDialog.Katalogzeile>(KATALOG);
        var cut = Aufbauen(katalog: rest,
                           loeschen: n =>
                           {
                               rest.RemoveAll(z => z.Bezeichner == n);
                               return Task.FromResult(true);
                           });

        cut.FindAll("tbody tr")[0].QuerySelector("button")!.Click();
        Knopf(cut, "Ganglinie Löschen").Click();
        Knopf(cut, "Ja").Click();

        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Contains("Tsol1", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_fehlgeschlagenes_Loeschen_meldet()
    {
        var cut = Aufbauen(loeschen: _ => Task.FromResult(false));

        cut.FindAll("tbody tr")[0].QuerySelector("button")!.Click();
        Knopf(cut, "Ganglinie Löschen").Click();
        Knopf(cut, "Ja").Click();

        Assert.Contains("schreibgeschützt", cut.Instance.Meldung);
        Assert.Equal(3, cut.FindAll("tbody tr").Count);
    }

    // =====================================================================
    // 3 — Datei wählen, ablegen, anzeigen
    // =====================================================================

    /// <summary>
    /// Die gewählte Datei wird verlustfrei abgelegt, und der Dialog arbeitet danach
    /// mit dem Pfad IM Ganglinienordner.
    /// </summary>
    [Fact]
    public void Die_gewaehlte_Datei_wird_abgelegt()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"C:\Downloads\Tsol2.txt"),
            ablegen: q => Task.FromResult(new AblageErgebnis(@"D:\VDI-3805-Daten\Solarthermie\Tsol2.txt")));

        Knopf(cut, "Datei Auswählen...").Click();

        Assert.Equal(@"D:\VDI-3805-Daten\Solarthermie\Tsol2.txt", cut.Instance.Pfad);
        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Befund W14‑B69, behoben:</b> Ein Fehlschlag der Ablage lief in ein
    /// <c>catch { }</c>. Jetzt meldet er — und der Import läuft mit der
    /// Originaldatei weiter.
    /// </summary>
    [Fact]
    public void Eine_fehlgeschlagene_Ablage_meldet_statt_zu_schweigen()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"C:\Downloads\Tsol2.txt"),
            ablegen: q => Task.FromResult(new AblageErgebnis("", "Kopieren ging nicht: Zugriff verweigert")));

        Knopf(cut, "Datei Auswählen...").Click();

        Assert.Equal(@"C:\Downloads\Tsol2.txt", cut.Instance.Pfad);
        Assert.Contains("Zugriff verweigert", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Befund W14‑B67, behoben:</b> „Datei bearbeiten…" öffnet den VOLLEN Pfad.
    /// Der Vorläufer verkettete den Ordner mit <c>textBox_Name.Text</c>, das den
    /// vollen Pfad bereits trug — und verdoppelte ihn damit.
    /// </summary>
    [Fact]
    public void Anzeigen_oeffnet_den_vollen_Pfad()
    {
        string? geoeffnet = null;
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"C:\Downloads\Tsol2.txt"),
            ablegen: q => Task.FromResult(new AblageErgebnis(@"D:\VDI-3805-Daten\Solarthermie\Tsol2.txt")),
            mitSystem: p => { geoeffnet = p; return Task.FromResult(true); });

        Assert.True(Knopf(cut, "Datei bearbeiten...").HasAttribute("disabled"));

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei bearbeiten...").Click();

        Assert.Equal(@"D:\VDI-3805-Daten\Solarthermie\Tsol2.txt", geoeffnet);
    }

    // =====================================================================
    // 4 — Einlesen
    // =====================================================================

    /// <summary>Ohne gewählte Datei bleibt „Datei Einlesen…" gesperrt.</summary>
    [Fact]
    public void Ohne_Datei_ist_das_Einlesen_gesperrt()
    {
        var cut = Aufbauen(einlesen: (p, m) => Task.FromResult(new SolarganglinieImportErgebnis()));

        Assert.True(Knopf(cut, "Datei Einlesen...").HasAttribute("disabled"));
    }

    /// <summary>
    /// Ein erfolgreicher Import meldet, lädt den Katalog neu und gibt den Pfad frei.
    /// </summary>
    [Fact]
    public void Ein_erfolgreicher_Import_laedt_den_Katalog_neu()
    {
        var liste = new List<SolarganglinieAdminDialog.Katalogzeile>(KATALOG);
        var cut = Aufbauen(
            katalog: liste,
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\VDI-3805-Daten\Solarthermie\Tsol2.txt"),
            einlesen: (p, m) =>
            {
                liste.Add(new SolarganglinieAdminDialog.Katalogzeile(4, "Tsol2", "Neu", false));
                return Task.FromResult(new SolarganglinieImportErgebnis
                {
                    Erfolgreich = true,
                    Bezeichner = "Tsol2",
                    Meldung = "Die Ganglinie \"Tsol2\" wurde mit 8760 Werten eingelesen."
                });
            });

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei Einlesen...").Click();

        Assert.Equal(4, cut.FindAll("tbody tr").Count);
        Assert.Contains("8760", cut.Instance.Meldung);
        Assert.Equal("", cut.Instance.Pfad);
    }

    /// <summary>
    /// Ein belegter Name meldet und lässt den Katalog stehen. Der Vorläufer prüfte
    /// dafür <c>listBox_Extern.FindString</c> — eine PRÄFIXsuche in der ANZEIGE
    /// (Befund W14‑B70).
    /// </summary>
    [Fact]
    public void Ein_belegter_Name_meldet_und_schreibt_nicht()
    {
        var cut = Aufbauen(
            dateiWaehlen: _ => Task.FromResult<string?>(@"D:\VDI-3805-Daten\Solarthermie\Tsol1.txt"),
            einlesen: (p, m) => Task.FromResult(new SolarganglinieImportErgebnis
            {
                Erfolgreich = false,
                Meldung = "Solarganglinie ist bereits in Datenbank vorhanden!"
            }));

        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei Einlesen...").Click();

        Assert.Contains("bereits in Datenbank", cut.Instance.Meldung);
        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Equal(@"D:\VDI-3805-Daten\Solarthermie\Tsol1.txt", cut.Instance.Pfad);
    }

    // =====================================================================
    // 5 — Schluss
    // =====================================================================

    /// <summary>
    /// <b>A‑7:</b> „OK" liefert OK. Der Vorläufer setzte ein eigenes Feld
    /// <c>result</c>, das niemand las, und liess <c>this.DialogResult</c> auf
    /// <c>Cancel</c> stehen — <c>MitOk</c> lieferte damit IMMER <c>false</c>
    /// (Befund W14‑B4).
    /// </summary>
    [Fact]
    public void OK_liefert_OK()
    {
        bool? antwort = null;
        var cut = Aufbauen(geschlossen: b => antwort = b);

        Knopf(cut, "OK").Click();

        Assert.True(antwort);
    }

    /// <summary>Esc schließt — aber erst, wenn die Rückfrage zu ist.</summary>
    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Rueckfrage()
    {
        bool? antwort = null;
        var cut = Aufbauen(geschlossen: b => antwort = b);

        cut.FindAll("tbody tr")[0].QuerySelector("button")!.Click();
        Knopf(cut, "Ganglinie Löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(antwort);

        Knopf(cut, "Nein").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(antwort);
    }
}
