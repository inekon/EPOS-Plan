using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Pruefprotokoll des Lastgangimports (iU9-W12.1), Vorbild
/// <c>Views/Stromverbraucher/Form_GanglinieProtokoll</c>.
///
/// <para>Soll ist die Feldkarte: Kopftext, Protokollliste mit den Spalten
/// „Stufe" und „Meldung", „OK" (nur bei moeglichem Import bedienbar) und ein
/// zweiter Knopf, der „Abbrechen" oder „Schliessen" heisst. Dazu die
/// Entscheidung, die im Vorlaeufer in der statischen Tuer <c>Zeigen</c>
/// stand.</para>
/// </summary>
public class GanglinieProtokollDialogTests : BunitContext
{
    public GanglinieProtokollDialogTests()
    {
        // QuickGrid ruft beim Zeichnen JS - im Test genuegt der lockere Modus.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static PruefMeldung Fehler() => new(PruefStufe.Fehler, "IMPORT_PROT_DATEI_LEER");
    private static PruefMeldung Warnung() => new(PruefStufe.Warnung, "IMPORT_PROT_NEGATIVE_WERTE", "7");
    private static PruefMeldung Info() => new(PruefStufe.Info, "IMPORT_PROT_ERGEBNIS", "8760", "1", "12345");

    private IRenderedComponent<GanglinieProtokollDialog> Zeige(
        bool moeglich = true, bool bestaetigen = true,
        IReadOnlyList<PruefMeldung>? meldungen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<GanglinieProtokollDialog>(p => p
            .Add(x => x.Meldungen, meldungen ?? new[] { Info(), Warnung() })
            .Add(x => x.ImportMoeglich, moeglich)
            .Add(x => x.BestaetigungNoetig, bestaetigen)
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IElement Ok(IRenderedComponent<GanglinieProtokollDialog> cut)
        => cut.FindAll(".epos-leiste button")[1];

    private static IElement Zweiter(IRenderedComponent<GanglinieProtokollDialog> cut)
        => cut.FindAll(".epos-leiste button")[0];

    // =====================================================================
    // Feldbestand (Feldkarte: lbl_Kopf, listView_Protokoll, btn_OK, btn_Abbrechen)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_die_vier_Felder_der_Feldkarte()
    {
        var cut = Zeige();

        Assert.Contains("Prüfprotokoll", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-protokoll-kopf"));      // lbl_Kopf
        Assert.Single(cut.FindAll(".epos-raster"));              // listView_Protokoll
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);
    }

    /// <summary>Zwei Spalten, wie die zwei <c>ColumnHeader</c> des Vorlaeufers.</summary>
    [Fact]
    public void Die_Liste_hat_die_Spalten_Stufe_und_Meldung()
    {
        var cut = Zeige();
        var kopf = cut.FindAll(".epos-raster thead th");

        Assert.Equal(2, kopf.Count);
        Assert.Contains("Stufe", kopf[0].TextContent);
        Assert.Contains("Meldung", kopf[1].TextContent);
    }

    [Fact]
    public void Jede_Meldung_wird_eine_Zeile()
    {
        var cut = Zeige(meldungen: new[] { Info(), Warnung(), Fehler() });

        Assert.Equal(3, cut.FindAll(".epos-raster tbody tr").Count);
    }

    [Fact]
    public void Ohne_Meldungen_bleibt_die_Liste_leer()
    {
        var cut = Zeige(meldungen: Array.Empty<PruefMeldung>());

        Assert.Empty(cut.FindAll(".epos-raster tbody tr td"));
    }

    // =====================================================================
    // Die drei Kopftexte (:42-45)
    // =====================================================================

    [Fact]
    public void Ohne_moeglichen_Import_steht_der_Fehlerkopf()
    {
        var cut = Zeige(moeglich: false, bestaetigen: true);

        Assert.Contains("kann nicht importiert werden", cut.Find(".epos-protokoll-kopf").TextContent);
    }

    [Fact]
    public void Bei_einem_Eingriff_steht_der_Bestaetigungskopf()
    {
        var cut = Zeige(moeglich: true, bestaetigen: true);

        Assert.Contains("bestätigen", cut.Find(".epos-protokoll-kopf").TextContent);
    }

    /// <summary>
    /// Der dritte Kopftext war im Vorlaeufer ueber <c>Zeigen</c> unerreichbar
    /// (:121-123). Die Komponente kann ihn zeigen — wer sie ohne <c>Noetig</c>
    /// aufmacht, bekommt ihn.
    /// </summary>
    [Fact]
    public void Ein_sauberer_Lauf_zeigt_den_dritten_Kopftext()
    {
        var cut = Zeige(moeglich: true, bestaetigen: false);

        Assert.Contains("geprüft und kann übernommen werden",
                        cut.Find(".epos-protokoll-kopf").TextContent);
    }

    // =====================================================================
    // Die zwei Knoepfe
    // =====================================================================

    [Fact]
    public void OK_ist_ohne_moeglichen_Import_gesperrt()
    {
        Assert.True(Ok(Zeige(moeglich: false)).HasAttribute("disabled"));
        Assert.False(Ok(Zeige(moeglich: true)).HasAttribute("disabled"));
    }

    /// <summary>
    /// <c>btn_Abbrechen.Text = importMoeglich ? IMPORT_BTN_ABBRECHEN :
    /// IMPORT_BTN_SCHLIESSEN</c> (:60-62) — ohne moeglichen Import ist er der
    /// einzige Ausgang und heisst deshalb anders.
    /// </summary>
    [Fact]
    public void Der_zweite_Knopf_heisst_Abbrechen_oder_Schliessen()
    {
        Assert.Contains("Abbrechen", Zweiter(Zeige(moeglich: true)).TextContent);
        Assert.Contains("Schließen", Zweiter(Zeige(moeglich: false)).TextContent);
    }

    [Fact]
    public void OK_meldet_true_und_Abbrechen_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        Ok(cut).Click();
        Assert.True(ergebnis);

        ergebnis = null;
        Zweiter(cut).Click();
        Assert.False(ergebnis);
    }

    /// <summary>Ein gesperrtes OK meldet nichts — auch nicht bei einem Klick.</summary>
    [Fact]
    public void Ein_gesperrtes_OK_meldet_nichts()
    {
        bool? ergebnis = null;
        var cut = Zeige(moeglich: false, geschlossen: b => ergebnis = b);

        Ok(cut).Click();
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_mit_Abbruch()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    // =====================================================================
    // Die Stufenklassen (frueher drei Color.FromArgb)
    // =====================================================================

    [Fact]
    public void Jede_Stufe_traegt_ihre_eigene_Klasse()
    {
        var cut = Zeige(meldungen: new[] { Fehler(), Warnung(), Info() });
        var zeilen = cut.FindAll(".epos-raster tbody tr");

        Assert.Contains("epos-stufe--fehler", zeilen[0].InnerHtml);
        Assert.Contains("epos-stufe--warnung", zeilen[1].InnerHtml);
        Assert.Contains("epos-stufe--info", zeilen[2].InnerHtml);
    }

    // =====================================================================
    // Noetig — die Entscheidung aus der statischen Tuer Zeigen (:93)
    // =====================================================================

    [Fact]
    public void Noetig_haelt_den_sauberen_Lauf_vom_Dialog_fern()
    {
        Assert.False(GanglinieProtokollDialog.Noetig(true, false));   // sauber: kein Fenster
        Assert.True(GanglinieProtokollDialog.Noetig(true, true));     // Eingriff
        Assert.True(GanglinieProtokollDialog.Noetig(false, false));   // Fehler
        Assert.True(GanglinieProtokollDialog.Noetig(false, true));
    }

    // =====================================================================
    // Befund W12-B17
    // =====================================================================

    /// <summary>
    /// Der Vorlaeufer war als einziger der Kette OHNE <c>InfoKnopf.Anbringen</c>.
    /// </summary>
    [Fact]
    public void Der_Dialog_hat_einen_Infoknopf()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-infoknopf"));
    }
}
