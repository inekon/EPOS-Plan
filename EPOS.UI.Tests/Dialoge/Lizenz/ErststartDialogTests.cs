using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Lizenz;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Lizenz;

/// <summary>
/// <see cref="ErststartDialog"/> — Zeuge der Welle iU9-W15c.7.
///
/// <para><b>Die jüngste Maske des Pakets wird die dünnste Komponente.</b>
/// <c>Form_Erststart</c> war zwei Tage alt, als die Welle sie ablöste, und ihr Ablauf
/// lag schon vollständig oberflächenfrei in <c>ErststartMigration</c>. Geblieben ist
/// eine Hülle um fünf Dinge: Kopftext, Statuszeile, unbestimmter Balken,
/// Protokollfenster, zwei Knöpfe.</para>
///
/// <para><b>Drei Zusagen tragen Gewicht.</b> (1) <b>Kein Abbrechen während des
/// Laufs</b> — beide Knöpfe sind gesperrt, und der Fortschritt zeigt keinen
/// Abbrechen-Knopf; ein Abbruch mitten in der Übertragung hinterließe eine halbe
/// Zieldatei. (2) Der Balken ist <b>unbestimmt</b> (<c>Anteil = null</c>), weil der
/// Ablauf Zeilen meldet und keine Anteile. (3) Die Komponente meldet über
/// <c>LaufAktiv</c>, wann das Fenster zu sperren ist — <b>erst die Sperre lösen, dann
/// schließen</b>, sonst finge der Riegel der Hülle den eigenen Schließbefehl.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class ErststartDialogTests : BunitContext
{
    public ErststartDialogTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    private const string KOPF =
        "Die Datenbank dieses Rechners liegt noch im alten Access-Format vor.\n\n" +
        "Ordner: C:\\ProgramData\\EPOS_PLAN\n\nAblauf:\n" +
        "   1. Kenndaten.accdb wird auf den letzten Access-Stand gebracht.\n" +
        "   2. Alle Daten werden nach Kenndaten.sqlite übertragen.\n" +
        "   3. Die Altdatei bleibt als Kenndaten.vor-sqlite.accdb liegen.";

    private IRenderedComponent<ErststartDialog> Zeigen(
        Func<Action<string>, Task<(bool Ok, string Schlussmeldung)>>? lauf = null,
        EventCallback<bool>? laufAktiv = null,
        EventCallback<bool>? fertig = null)
    {
        return Render<ErststartDialog>(p =>
        {
            p.Add(x => x.Kopftext, KOPF)
             .Add(x => x.ProtokollText, "Protokoll")
             .Add(x => x.KnopfStarten, "Jetzt umstellen")
             .Add(x => x.KnopfBeenden, "Beenden")
             .Add(x => x.StatusBereit, "Bereit.")
             .Add(x => x.StatusLaeuft, "Umstellung läuft - bitte nicht abschalten.")
             .Add(x => x.StatusFertig, "Umstellung abgeschlossen.")
             .Add(x => x.StatusFehler, "Umstellung fehlgeschlagen.");

            if (lauf is not null) p.Add(x => x.Lauf, lauf);
            if (laufAktiv is not null) p.Add(x => x.LaufAktiv, laufAktiv.Value);
            if (fertig is not null) p.Add(x => x.Fertig, fertig.Value);
        });
    }

    private static IElement Knopf(IRenderedComponent<ErststartDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // ==================================================================
    //  Der Kopftext
    // ==================================================================

    /// <summary>
    /// Der Kopftext nennt alle DREI Dateinamen — sie kommen aus
    /// <c>ErststartMigration</c> und stehen nirgends ein zweites Mal.
    /// </summary>
    [Fact]
    public void Der_Kopftext_nennt_alle_drei_Dateinamen()
    {
        var cut = Zeigen();
        string kopf = cut.Find("p.epos-erststart-kopf").TextContent;

        Assert.Contains("Kenndaten.accdb", kopf, StringComparison.Ordinal);
        Assert.Contains("Kenndaten.sqlite", kopf, StringComparison.Ordinal);
        Assert.Contains("Kenndaten.vor-sqlite.accdb", kopf, StringComparison.Ordinal);
        Assert.Contains("C:\\ProgramData\\EPOS_PLAN", kopf, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Vor dem Start
    // ==================================================================

    /// <summary>
    /// Vor dem Start sind beide Knöpfe bedienbar, die Statuszeile sagt „Bereit." und
    /// es läuft kein Balken.
    /// </summary>
    [Fact]
    public void Vor_dem_Start_ist_alles_bedienbar()
    {
        var cut = Zeigen();

        Assert.False(Knopf(cut, "Jetzt umstellen").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Beenden").HasAttribute("disabled"));
        Assert.Equal("Bereit.", cut.Find("p.epos-erststart-status").TextContent.Trim());
        Assert.Empty(cut.FindComponents<EPOS.UI.Bausteine.Fortschritt>());
    }

    /// <summary>Das Protokollfenster ist nur lesbar und in fester Schrittweite.</summary>
    [Fact]
    public void Das_Protokoll_ist_nur_lesbar_und_in_fester_Schrittweite()
    {
        var cut = Zeigen();
        var feld = cut.FindComponent<EPOS.UI.Standards.Textfeld>().Instance;

        Assert.True(feld.Mehrzeilig);
        Assert.True(feld.NurLesen);
        Assert.True(feld.Festbreite);
    }

    /// <summary>
    /// „Beenden" vor dem Start meldet <c>false</c> — nichts ist geschehen, der
    /// Access-Bestand bleibt unangetastet liegen.
    /// </summary>
    [Fact]
    public void Beenden_vor_dem_Start_meldet_Ablehnung()
    {
        var gemeldet = new List<bool>();
        var cut = Zeigen(fertig: EventCallback.Factory.Create<bool>(new object(), b => gemeldet.Add(b)));

        Knopf(cut, "Beenden").Click();

        Assert.Equal(new[] { false }, gemeldet);
    }

    /// <summary>Ohne Lauf-Delegat tut „Jetzt umstellen" nichts.</summary>
    [Fact]
    public void Ohne_Lauf_passiert_nichts()
    {
        var cut = Zeigen();

        Knopf(cut, "Jetzt umstellen").Click();

        Assert.Equal("Bereit.", cut.Find("p.epos-erststart-status").TextContent.Trim());
        Assert.Empty(cut.FindComponents<EPOS.UI.Bausteine.Fortschritt>());
    }

    // ==================================================================
    //  Während des Laufs
    // ==================================================================

    /// <summary>
    /// <b>Kein Abbrechen während des Laufs.</b> Beide Knöpfe sind gesperrt, der
    /// unbestimmte Balken steht, und der Fortschritt zeigt KEINEN Abbrechen-Knopf.
    /// </summary>
    [Fact]
    public void Waehrend_des_Laufs_ist_kein_Abbruch_moeglich()
    {
        var tor = new TaskCompletionSource<(bool, string)>();
        var cut = Zeigen(lauf: _ => tor.Task);

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() => Assert.True(Knopf(cut, "Jetzt umstellen").HasAttribute("disabled")));
        Assert.True(Knopf(cut, "Beenden").HasAttribute("disabled"));

        var balken = cut.FindComponent<EPOS.UI.Bausteine.Fortschritt>().Instance;
        Assert.Null(balken.Anteil);          // unbestimmt - das Marquee des Vorläufers
        Assert.Null(balken.Abbrechen);       // ohne Rückruf zeigt der Baustein keinen Knopf

        tor.SetResult((true, "Fertig."));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindComponents<EPOS.UI.Bausteine.Fortschritt>()));
    }

    /// <summary>
    /// Die Sperre wird an die Hülle gemeldet — <c>true</c> beim Start, <c>false</c>
    /// am Ende, <b>und zwar VOR dem Schließen</b>.
    /// </summary>
    [Fact]
    public void Die_Sperre_wird_gemeldet_und_vor_dem_Schliessen_geloest()
    {
        var folge = new List<string>();
        var cut = Zeigen(
            lauf: _ => Task.FromResult((true, "Fertig.")),
            laufAktiv: EventCallback.Factory.Create<bool>(new object(), a => folge.Add("sperre:" + a)),
            fertig: EventCallback.Factory.Create<bool>(new object(), ok => folge.Add("fertig:" + ok)));

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() => Assert.Equal(3, folge.Count));
        Assert.Equal(new[] { "sperre:True", "sperre:False", "fertig:True" }, folge);
    }

    // ==================================================================
    //  Das Protokoll
    // ==================================================================

    /// <summary>
    /// Jede gemeldete Zeile landet im Protokoll, und die zuletzt gemeldete wird
    /// zugleich die Statuszeile — „die Statuszeile trägt immer den zuletzt
    /// gemeldeten Schritt".
    /// </summary>
    [Fact]
    public void Jede_Zeile_landet_im_Protokoll_und_die_letzte_in_der_Statuszeile()
    {
        var cut = Zeigen(lauf: melden =>
        {
            melden("Schritt 1: Access-Stand wird gehoben.");
            melden("   Schritt 2: 114 Tabellen uebertragen.   ");
            return Task.FromResult((true, "Umstellung erfolgreich."));
        });

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Umstellung erfolgreich.", Protokoll(cut), StringComparison.Ordinal));

        string protokoll = Protokoll(cut);
        Assert.Contains("Schritt 1: Access-Stand wird gehoben.", protokoll, StringComparison.Ordinal);
        Assert.Contains("Schritt 2: 114 Tabellen uebertragen.", protokoll, StringComparison.Ordinal);

        // BITGLEICH ZUM VORLAEUFER: Fertig() setzt erst den Zustandstext und haengt
        // DANACH die Schlussmeldung an - und ZeileAnhaengen zieht jede Zeile in die
        // Statuszeile nach. Am Ende steht dort also die SCHLUSSMELDUNG, nicht
        // "Umstellung abgeschlossen." (Form_Erststart.cs:265-267).
        Assert.Equal("Umstellung erfolgreich.", cut.Find("p.epos-erststart-status").TextContent.Trim());
    }

    /// <summary>Leere Meldungen werden übergangen — sie sind keine Protokollzeile.</summary>
    [Fact]
    public void Leere_Meldungen_landen_nicht_im_Protokoll()
    {
        var cut = Zeigen(lauf: melden =>
        {
            melden("");
            melden(null!);
            melden("Eine Zeile.");
            return Task.FromResult((true, ""));
        });

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() => Assert.Contains("Eine Zeile.", Protokoll(cut), StringComparison.Ordinal));
        Assert.Equal("Eine Zeile.\n", Protokoll(cut));
    }

    // ==================================================================
    //  Das Ende
    // ==================================================================

    /// <summary>
    /// Nach dem Erfolg steht „Umstellung abgeschlossen.", und die Komponente meldet
    /// <c>Fertig(true)</c> — der Assistent schließt sich ohne weiteren Klick.
    /// </summary>
    [Fact]
    public void Nach_dem_Erfolg_meldet_die_Komponente_Fertig_true()
    {
        var gemeldet = new List<bool>();
        var cut = Zeigen(lauf: _ => Task.FromResult((true, "Alles uebertragen.")),
                         fertig: EventCallback.Factory.Create<bool>(new object(), b => gemeldet.Add(b)));

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() => Assert.Equal(new[] { true }, gemeldet));
        Assert.Contains("Alles uebertragen.", Protokoll(cut), StringComparison.Ordinal);
        // Die Schlussmeldung zieht in die Statuszeile nach (siehe oben).
        Assert.Equal("Alles uebertragen.", cut.Find("p.epos-erststart-status").TextContent.Trim());
    }

    /// <summary>
    /// <b>Auch ein Fehlschlag schließt.</b> Der Vorläufer ruft <c>Close()</c>
    /// unbedingt; gemeldet wird der Fehlschlag vom Aufrufer, mit Berichtspfad.
    /// </summary>
    [Fact]
    public void Auch_ein_Fehlschlag_meldet_sich_und_schliesst()
    {
        var gemeldet = new List<bool>();
        var cut = Zeigen(lauf: _ => Task.FromResult((false, "Abbruch: Die Alt-Hebung ist fehlgeschlagen.")),
                         fertig: EventCallback.Factory.Create<bool>(new object(), b => gemeldet.Add(b)));

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() => Assert.Equal(new[] { false }, gemeldet));
        Assert.Contains("Abbruch: Die Alt-Hebung ist fehlgeschlagen.", Protokoll(cut), StringComparison.Ordinal);
        Assert.Equal("Abbruch: Die Alt-Hebung ist fehlgeschlagen.",
                     cut.Find("p.epos-erststart-status").TextContent.Trim());
    }

    /// <summary>
    /// OHNE Schlussmeldung bleibt der Zustandstext stehen — <c>ZeileAnhaengen</c>
    /// übergeht eine leere Zeile, und damit zieht nichts nach.
    /// </summary>
    [Fact]
    public void Ohne_Schlussmeldung_bleibt_der_Zustandstext_stehen()
    {
        var cut = Zeigen(lauf: _ => Task.FromResult((true, "")));

        Knopf(cut, "Jetzt umstellen").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            "Umstellung abgeschlossen.", cut.Find("p.epos-erststart-status").TextContent.Trim()));
    }

    /// <summary>Ein zweiter Klick während des Laufs startet nichts ein zweites Mal.</summary>
    [Fact]
    public void Ein_zweiter_Klick_startet_nichts_ein_zweites_Mal()
    {
        int rufe = 0;
        var tor = new TaskCompletionSource<(bool, string)>();
        var cut = Zeigen(lauf: _ => { rufe++; return tor.Task; });

        Knopf(cut, "Jetzt umstellen").Click();
        cut.WaitForAssertion(() => Assert.True(Knopf(cut, "Jetzt umstellen").HasAttribute("disabled")));
        Knopf(cut, "Jetzt umstellen").Click();

        Assert.Equal(1, rufe);
        tor.SetResult((true, ""));
    }

    private static string Protokoll(IRenderedComponent<ErststartDialog> cut)
        => cut.FindComponent<EPOS.UI.Standards.Textfeld>().Instance.Wert;
}
