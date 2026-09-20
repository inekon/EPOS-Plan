using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Kapitalwert-Verlauf (iU9-W1.6). Soll ist die Feldkarte von
/// <c>Form_WirtschaftlichkeitVerlauf</c>: Zeitraum (2..60), Szenario,
/// "Aktualisieren", zwei Bilder, Restwertzeile, Statuszeile, "Schliessen".
///
/// <para><b>Die Kultur ist gepinnt (Etappe B6, K11).</b> Die Beschriftungen des
/// Dialogs sind bis B5b deutsche Zeichenketten im Quelltext gewesen; seit B6 kommen
/// sie aus <c>MyResource</c> und folgen damit der Oberflaechensprache. Die
/// Erwartungen hier sind deutsch, der CI-Laeufer steht auf en-US - ohne die
/// Vorrichtung waeren diese Faelle auf dem Laeufer rot und auf dem Entwicklungsrechner
/// gruen.</para>
/// </summary>
public class KapitalwertVerlaufDialogTests : EposBunitContext
{

    private static readonly (int Id, string Text)[] Szenarien =
    {
        (0, "Erwartet"),
        (1, "Best"),
        (2, "Worst")
    };

    /// <summary>
    /// Die beiden Verläufe als ZEICHENMODELL — je EINE Instanz, EINMAL gebaut: Der
    /// Baustein <c>DiagrammSvg</c> vergleicht die MODELLREFERENZ, und ein je
    /// Zeichenlauf neu gebautes Modell verwürfe mit dem Baum auch Zoom, Zeigerstelle
    /// und abgewählte Reihen. Die Titel sind mit Absicht unverwechselbar — daran
    /// erkennt der Prüfstand, welches der beiden Bilder oben steht.
    /// </summary>
    private static readonly Zeichenmodell ModellDifferenz = Verlaufsbild("BILD-DIFFERENZ");
    private static readonly Zeichenmodell ModellAbsolut = Verlaufsbild("BILD-ABSOLUT");

    private static Zeichenmodell Verlaufsbild(string titel)
    {
        var werte = new double[21];
        for (int i = 0; i < werte.Length; i++) werte[i] = -12000 + i * 1400;

        return ChartRenderer.KapitalwertVerlaufModell(
            titel,
            new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Variante A", werte, ChartRenderer.C_WP)
            },
            "Fussnote");
    }

    public KapitalwertVerlaufDialogTests()
    {
        // Die Verlaufsbilder haben eine Zeichenflaeche und binden deshalb das
        // JS-Modul des Zooms; im Pruefstand gibt es keines.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static KapitalwertVerlaufBilder Ergebnis(int jahre, string szenario) =>
        new(ModellDifferenz, ModellAbsolut, "Restwert-Barwerte …",
            "Verlauf über " + jahre + " Jahre, Szenario „" + szenario + "“.");

    private IRenderedComponent<KapitalwertVerlaufDialog> Aufbauen(
        Func<int, int, CancellationToken, Task<KapitalwertVerlaufBilder>>? berechnen = null,
        Action? beimSchliessen = null,
        int jahreVorgabe = 20,
        bool titelAnzeigen = true,
        Func<Farbrolle, Farbe, Task>? farbeSetzen = null)
    {
        return Render<KapitalwertVerlaufDialog>(p => p
            .Add(x => x.Szenarien, Szenarien)
            .Add(x => x.JahreVorgabe, jahreVorgabe)
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
            .Add(x => x.FarbeSetzen, farbeSetzen)
            .Add(x => x.Berechnen, berechnen ??
                ((jahre, szenario, _) => Task.FromResult(Ergebnis(jahre, Szenarien[szenario].Text))))
            .Add(x => x.Geschlossen, () => beimSchliessen?.Invoke()));
    }

    /// <summary>
    /// Ein Titel, eine Stelle (W11b‑B‑9): Zeigt der Wirt schon einen — die
    /// Überlagerung der Wirtschaftlichkeitsseite tut es —, bleibt der eigene Kopf
    /// weg; der Hilfeknopf bleibt.
    /// </summary>
    [Fact]
    public void Ohne_TitelAnzeigen_bleibt_der_eigene_Kopf_weg_und_der_Hilfeknopf_steht()
    {
        var cut = Aufbauen(titelAnzeigen: false);

        Assert.Empty(cut.FindAll("h1.epos-dialog-titel"));
        Assert.Contains("epos-dialog-kopf--ohnetitel", cut.Find("div.epos-dialog-kopf").ClassName);
        Assert.NotNull(cut.Find(".epos-infoknopf"));
    }

    /// <summary>Im eigenen Fenster (Vorgabe) steht der Kopf wie bisher.</summary>
    [Fact]
    public void Mit_TitelAnzeigen_steht_der_eigene_Kopf()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll("h1.epos-dialog-titel"));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll("input[type=text]"));        // Zeitraum
        Assert.Single(cut.FindAll("select"));                  // Szenario
        // Aktualisieren + Schliessen. Die beiden Diagramme fuehren eine eigene Leiste
        // ("Bereich", "1:1"), ihre Knoepfe tragen aber epos-diagramm-knopf.
        Assert.Equal(2, cut.FindAll("button.epos-knopf:not(.epos-dialog-zu)").Count);
        // Zwei Bilder (nach dem Lauf beim Oeffnen).
        Assert.Equal(2, cut.FindComponents<DiagrammSvg>().Count);
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen();

        Assert.Equal("Kapitalwert-Verlauf über den Nutzungszeitraum",
                     cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Zeitraum [Jahre]:", texte[0].TextContent);
        Assert.Equal("Szenario:", texte[1].TextContent);
        Assert.Equal("Aktualisieren", cut.Find(".epos-neuzeile button.epos-knopf").TextContent);
    }

    [Fact]
    public void Die_Vorgabe_des_Zeitraums_wird_uebernommen()
    {
        // ParameterVorbelegen: der gespeicherte Betrachtungszeitraum.
        var cut = Aufbauen(jahreVorgabe: 35);

        Assert.Equal("35", cut.Find("input[type=text]").GetAttribute("value"));
    }

    [Fact]
    public void Die_Vorgabe_wird_auf_den_Bereich_geklemmt()
    {
        Assert.Equal("2", Aufbauen(jahreVorgabe: 0).Find("input[type=text]").GetAttribute("value"));
        Assert.Equal("60", Aufbauen(jahreVorgabe: 99).Find("input[type=text]").GetAttribute("value"));
    }

    [Fact]
    public void Der_Dialog_rechnet_schon_beim_Oeffnen()
    {
        // Form_WirtschaftlichkeitVerlauf_Load rief btnZeichnen_Click.
        int laeufe = 0;
        var cut = Aufbauen(berechnen: (jahre, szenario, _) =>
        {
            laeufe++;
            return Task.FromResult(Ergebnis(jahre, Szenarien[szenario].Text));
        });

        Assert.Equal(1, laeufe);
        Assert.Equal(2, cut.FindComponents<DiagrammSvg>().Count);
    }

    /// <summary>
    /// <b>Beide Verläufe stehen als SVG im Baum</b> (Etappe DG-E3, Gruppe (c)) — in
    /// der Reihenfolge Differenz, absolut, jeder unter seiner EIGENEN Kennung: Zwei
    /// gleiche schnitten das eine Bild am <c>clipPath</c>-Rechteck des anderen.
    /// </summary>
    [Fact]
    public void Die_Bilder_stehen_als_DiagrammSvg_in_der_Seite()
    {
        var cut = Aufbauen();

        var bilder = cut.FindComponents<DiagrammSvg>();
        Assert.Equal("kapitalwert-differenz", bilder[0].Instance.Kennung);
        Assert.Equal("kapitalwert-absolut", bilder[1].Instance.Kennung);

        var flaechen = cut.FindAll(".epos-diagramm-svg");
        Assert.Contains("BILD-DIFFERENZ", flaechen[0].TextContent);
        Assert.Contains("BILD-ABSOLUT", flaechen[1].TextContent);

        // MIT Zeichenflaeche: Der Verlauf traegt die Bedienleiste des Zooms - anders
        // als die Kennlinien, die keine Flaeche haben (DG-E3-7).
        Assert.Equal(2, cut.FindAll(".epos-diagramm-leiste").Count);
    }

    [Fact]
    public void Restwert_und_Statuszeile_stehen_unter_den_Bildern()
    {
        var cut = Aufbauen();

        var zeilen = cut.FindAll(".epos-herleitung-text");
        Assert.Equal("Restwert-Barwerte …", zeilen[0].TextContent);
        Assert.Equal("Verlauf über 20 Jahre, Szenario „Erwartet“.", zeilen[1].TextContent);
    }

    [Fact]
    public void Aktualisieren_rechnet_mit_Jahren_und_Szenario()
    {
        int erhalteneJahre = 0;
        int erhaltenesSzenario = -1;
        var cut = Aufbauen(berechnen: (jahre, szenario, _) =>
        {
            erhalteneJahre = jahre;
            erhaltenesSzenario = szenario;
            return Task.FromResult(Ergebnis(jahre, Szenarien[szenario].Text));
        });

        cut.Find("input[type=text]").Input("30");
        cut.Find("select").Change("2");
        cut.Find(".epos-neuzeile button.epos-knopf").Click();

        Assert.Equal(30, erhalteneJahre);
        Assert.Equal(2, erhaltenesSzenario);
        Assert.Contains("Worst", cut.FindAll(".epos-herleitung-text")[1].TextContent);
    }

    [Fact]
    public void Waehrend_der_Rechnung_sind_die_Eingaben_gesperrt()
    {
        // SetBusy: numJahre/cbSzenario/btnZeichnen gesperrt, Schliessen heisst
        // "Abbrechen".
        var tor = new TaskCompletionSource<KapitalwertVerlaufBilder>();
        var cut = Aufbauen(berechnen: (_, _, _) => tor.Task);

        Assert.True(cut.Instance.Laeuft);
        Assert.True(cut.Find(".epos-neuzeile button.epos-knopf").HasAttribute("disabled"));
        Assert.True(cut.Find("select").HasAttribute("disabled"));
        Assert.Equal("Abbrechen", cut.Find(".epos-knopf--primaer").TextContent);
        Assert.Equal("Berechnung läuft …", cut.Instance.Status);

        tor.SetResult(Ergebnis(20, "Erwartet"));
        cut.WaitForAssertion(() => Assert.False(cut.Instance.Laeuft), TimeSpan.FromSeconds(5));
        Assert.Equal("Schließen", cut.Find(".epos-knopf--primaer").TextContent);
    }

    [Fact]
    public void Der_Knopf_bricht_die_laufende_Rechnung_ab_und_schliesst_nicht()
    {
        bool geschlossen = false;
        var tor = new TaskCompletionSource<KapitalwertVerlaufBilder>();
        CancellationToken merker = default;
        var cut = Aufbauen(
            berechnen: (_, _, ct) => { merker = ct; return tor.Task; },
            beimSchliessen: () => geschlossen = true);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.True(merker.IsCancellationRequested);
        Assert.False(geschlossen);

        tor.SetCanceled(merker);
        cut.WaitForAssertion(() => Assert.Equal("Vorgang abgebrochen.", cut.Instance.Status), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Ein_Fehler_erscheint_als_Warnbanner()
    {
        // Frueher eine MessageBox ("Fehler beim Berechnen des Verlaufs: …").
        var cut = Aufbauen(berechnen: (_, _, _) =>
            Task.FromException<KapitalwertVerlaufBilder>(new InvalidOperationException("kein Ergebnis")));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-warnbanner")), TimeSpan.FromSeconds(5));
        Assert.Equal("Fehler beim Berechnen des Verlaufs: kein Ergebnis",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Schliessen_und_Esc_melden_das_Ende()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(1, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);

        // Enter bleibt unbelegt.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(2, gemeldet);
    }

    /// <summary>Anwenderentscheid 15.09.2026: das Kreuz der Kopfzeile wirkt wie Esc.</summary>
    [Fact]
    public void Kreuz_meldet_das_Ende()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, gemeldet);
    }

    /// <summary>
    /// Titel-bedingter Kopf: Das Kreuz haengt an DERSELBEN Bedingung wie der Titel
    /// (<c>TitelAnzeigen</c>) — ohne Titel zeigt der Kopf weder Titel noch Kreuz.
    /// </summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titelAnzeigen: false);

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_WirtschaftlichkeitVerlauf.btn_Help" }, hilfe.Geoeffnet);
    }

    // =================================================================================
    //  Die Farbwahl am Bild (Farbrollen, Bedienung Teil 2)
    // =================================================================================

    /// <summary>
    /// <b>Mit Schreibweg trägt das Farbfeld des Legendeneintrags die Klasse
    /// <c>epos-legende-farbfeld</c>, und ein Klick öffnet den Wähler.</b> Die
    /// Verlaufsreihen nehmen Hausfarben, und daraus wird im Modell eine Farbrolle;
    /// ohne <c>FarbeSetzen</c> gäbe es dort nichts zu klicken.
    /// </summary>
    [Fact]
    public void Das_Farbfeld_des_Verlaufs_oeffnet_den_Farbwaehler()
    {
        var cut = Aufbauen(farbeSetzen: (rolle, farbe) => Task.CompletedTask);

        Assert.NotEmpty(cut.FindAll("rect.epos-legende-farbfeld"));
        Assert.Empty(cut.FindAll(".epos-farbwahl"));

        cut.FindAll("rect.epos-legende-farbfeld")[0].Click();
        Assert.Single(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>Ohne Schreibweg bleibt das Farbfeld ein gemaltes Rechteck.</summary>
    [Fact]
    public void Ohne_Schreibweg_traegt_der_Verlauf_kein_Farbfeld()
    {
        Assert.Empty(Aufbauen().FindAll("rect.epos-legende-farbfeld"));
    }
}
