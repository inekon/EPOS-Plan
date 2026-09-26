using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Seiten.Assistent;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die erste Assistentenseite (iU9-W15a.6) — Soll ist die Feldkarte von
/// <c>Wizard_Projekt</c>: zehn Kartenzeilen, davon vier Textfelder, zwei GESPERRTE
/// Datumsfelder, ein Auswahlfeld und die Kopfzeile.
///
/// <para>Geprueft wird vor allem der RUECKWEG: Die Seite schreibt AN ORT UND STELLE
/// in das uebergebene <see cref="ProjektKopfDaten"/> — daraus liest
/// <c>WizardParent</c> (Weg (a), Befund W15a-B42).</para>
/// </summary>
public class ProjektKopfSeiteTests : EposBunitContext
{
    private static readonly (int Id, string Text)[] REGIONEN =
    {
        (12, "Region 12 Mannheim"),
        (5, "Region 05 Hamburg")
    };

    public ProjektKopfSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static ProjektKopfDaten Satz() => new ProjektKopfDaten
    {
        Name = "Laurentiuskirche",
        Beschreibung = "Denkmalschutz",
        Kunde = "Kirchengemeinde",
        Bearbeiter = "M. Muster",
        Erstelldatum = new DateTime(2020, 1, 12),
        Aenderungsdatum = new DateTime(2026, 5, 4),
        IdKlimaregion = 12,
        Klimaname = "Region 12 Mannheim",
        NameAenderbar = false
    };

    /// <summary>Die Textfelder ohne die Klimaregion (die Suchauswahl ist auch ein Textfeld).</summary>
    private const string TEXTFELDER = "input[type=text]:not([role=combobox])";

    /// <summary>Das Eingabefeld der Klimaregion (Suchauswahl).</summary>
    private const string KLIMAFELD = "input[role=combobox]";

    /// <summary>Wählt einen Eintrag der Klappliste wie der Anwender: aufklappen, anklicken.</summary>
    private static void Waehlen(IRenderedComponent<ProjektKopfSeite> cut, string text)
    {
        cut.Find(KLIMAFELD).Click();
        cut.FindAll("li[role=option]").First(li => li.TextContent.Trim() == text).Click();
    }

    private IRenderedComponent<ProjektKopfSeite> Aufbauen(ProjektKopfDaten daten)
        => Render<ProjektKopfSeite>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Klimaregionen, REGIONEN));

    [Fact]
    public void Die_Seite_zeigt_die_neun_Felder_des_Vorlaeufers()
    {
        var cut = Aufbauen(Satz());

        // Projektname, Kunde, Bearbeiter + die zwei gesperrten Datumsfelder;
        // Beschreibung ist ein textarea, Klimaregion die durchsuchbare Auswahl der
        // Kopfleiste (#527, Suchauswahl statt select).
        Assert.Equal(5, cut.FindAll(TEXTFELDER).Count);
        Assert.Single(cut.FindAll("textarea"));
        Assert.Empty(cut.FindAll("select"));
        Assert.Single(cut.FindAll(KLIMAFELD));

        Assert.Contains("Projektkonfiguration", cut.Find(".epos-gruppenkopf").TextContent);
        Assert.Contains("administrativen Projektdaten", cut.Markup);
    }

    [Fact]
    public void Die_beiden_Datumsfelder_sind_gesperrt_und_zeigen_die_Programmsprache()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;              // sonst waere der Name das dritte
        var cut = Aufbauen(daten);

        var gesperrt = cut.FindAll("input[type=text][readonly]");
        Assert.Equal(2, gesperrt.Count);

        // A-9: "d" der aktuellen Kultur - der Vorlaeufer nagelte de-DE fest (B32a).
        Assert.Equal(new DateTime(2026, 5, 4).ToString("d"), gesperrt[0].GetAttribute("value"));
        Assert.Equal(new DateTime(2020, 1, 12).ToString("d"), gesperrt[1].GetAttribute("value"));
    }

    [Fact]
    public void Im_Bearbeiten_Modus_ist_der_Projektname_gesperrt()
    {
        var cut = Aufbauen(Satz());

        // Name + die zwei Datumsfelder
        Assert.Equal(3, cut.FindAll("input[type=text][readonly]").Count);
    }

    [Fact]
    public void Im_Neu_Modus_ist_der_Projektname_aenderbar()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;
        var cut = Aufbauen(daten);

        // nur noch die zwei Datumsfelder
        Assert.Equal(2, cut.FindAll("input[type=text][readonly]").Count);
    }

    [Fact]
    public void Jede_Eingabe_landet_AN_ORT_UND_STELLE_im_uebergebenen_Satz()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;
        var cut = Aufbauen(daten);

        var felder = cut.FindAll(TEXTFELDER + ":not([readonly])");
        felder[0].Input("Neuer Name");                   // Projektname
        felder[1].Input("Neuer Kunde");                  // Kunde
        felder[2].Input("Neuer Bearbeiter");             // Bearbeiter
        cut.Find("textarea").Input("Neue Beschreibung"); // Beschreibung

        Assert.Equal("Neuer Name", daten.Name);
        Assert.Equal("Neuer Kunde", daten.Kunde);
        Assert.Equal("Neuer Bearbeiter", daten.Bearbeiter);
        Assert.Equal("Neue Beschreibung", daten.Beschreibung);
    }

    [Fact]
    public void Die_Klimaregion_traegt_Id_UND_Namen_nach()
    {
        ProjektKopfDaten daten = Satz();
        var cut = Aufbauen(daten);

        Waehlen(cut, "Region 05 Hamburg");

        Assert.Equal(5, daten.IdKlimaregion);
        Assert.Equal("Region 05 Hamburg", daten.Klimaname);
    }

    [Fact]
    public void Ein_Altprojekt_ohne_passende_Id_wird_ueber_den_NAMEN_zugeordnet()
    {
        // Aeltere Projekte fuehren in Tab_Projekt.ID_Klimaregion die Id der
        // PROJEKTKOPIE; sie steht in keiner Stammliste.
        ProjektKopfDaten daten = Satz();
        daten.IdKlimaregion = 987;
        daten.Klimaname = "Region 05 Hamburg";

        var cut = Aufbauen(daten);

        Assert.Equal("Region 05 Hamburg", cut.Find(KLIMAFELD).GetAttribute("value"));
    }

    [Fact]
    public void Ein_leerer_Satz_zeigt_leere_Felder_und_keine_Region()
    {
        var daten = new ProjektKopfDaten();
        var cut = Aufbauen(daten);

        Assert.Equal("", cut.FindAll(TEXTFELDER + ":not([readonly])")[0].GetAttribute("value"));
        Assert.Equal("", cut.Find(KLIMAFELD).GetAttribute("value") ?? "");
    }

    // =====================================================================
    // Merge 5 (Nutzerauftrag 02.09.2026): Pflichtfelder und Namensdoppel
    // =====================================================================

    [Fact]
    public void Leerer_und_vergebener_Name_bringen_den_Hinweis_ein_freier_Name_nimmt_ihn_weg()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;
        daten.Name = "";
        var cut = Render<ProjektKopfSeite>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Klimaregionen, REGIONEN)
            .Add(x => x.VergebeneNamen, new[] { "Speicherhaus" })
            .Add(x => x.PflichtMarke, " *"));

        Assert.Equal(ProjektKopfBefund.NameLeer, cut.Instance.Befund);
        Assert.Contains("Projektnamen", cut.Find(".epos-projektkopf-hinweis").TextContent);
        Assert.Contains("Projektname *", cut.Markup);

        cut.FindAll(TEXTFELDER + ":not([readonly])")[0].Input("speicherhaus");
        Assert.Equal(ProjektKopfBefund.NameVorhanden, cut.Instance.Befund);
        Assert.Contains("existiert bereits", cut.Find(".epos-projektkopf-hinweis").TextContent);

        cut.FindAll(TEXTFELDER + ":not([readonly])")[0].Input("Neubau Ost");
        Assert.Equal(ProjektKopfBefund.Ok, cut.Instance.Befund);
        Assert.Empty(cut.FindAll(".epos-projektkopf-hinweis"));
    }

    [Fact]
    public void Ohne_Klimaregion_bleibt_der_Hinweis_bis_eine_gewaehlt_ist()
    {
        ProjektKopfDaten daten = Satz();
        daten.IdKlimaregion = 0;
        daten.Klimaname = "";
        var cut = Render<ProjektKopfSeite>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Klimaregionen, REGIONEN));

        Assert.Equal(ProjektKopfBefund.KlimaLeer, cut.Instance.Befund);
        Assert.Contains("Klimaregion", cut.Find(".epos-projektkopf-hinweis").TextContent);
        Waehlen(cut, "Region 05 Hamburg");
        Assert.Equal(ProjektKopfBefund.Ok, cut.Instance.Befund);
        Assert.Empty(cut.FindAll(".epos-projektkopf-hinweis"));
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Die sechs kurzen Felder stehen im Formularraster; der handgebaute
    /// Zweispalter <c>epos-projektkopf-raster</c> ist fort.
    ///
    /// <para>Die BESCHREIBUNG bleibt unter dem Raster: Zwischen ihr und den
    /// Feldern steht der Pflichtfeldhinweis (Merge 5), und der gehört zu den
    /// Feldern darüber.</para>
    ///
    /// <para>Geprüft wird das MARKUP: Der Block trägt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> — eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_Projektkopffelder_stehen_im_Formularraster()
    {
        var cut = Aufbauen(Satz());

        Assert.Empty(cut.FindAll(".epos-projektkopf-raster"));
        Assert.Single(cut.FindAll(".epos-formularraster"));
        Assert.Equal(6, cut.FindAll(".epos-formularraster .epos-feld").Count);

        // Die Beschreibung steht als mehrzeiliges - also BREITES - Feld
        // ausserhalb des Rasters.
        Assert.Empty(cut.FindAll(".epos-formularraster .epos-feld--breit"));
        Assert.Single(cut.FindAll(".epos-feld--breit"));
    }

    /// <summary>
    /// Anwenderwunsch 26.09.2026: Das Formular nimmt nur die Breite, die es braucht.
    /// Einleitung, Raster, Hinweis und Beschreibung stehen in EINEM Block
    /// <c>epos-projektkopf-formular</c> unter dem Gruppenkopf — der Balken bleibt
    /// außerhalb und damit breit.
    /// </summary>
    [Fact]
    public void Das_Formular_steht_in_einem_eigenen_Block_unter_dem_Gruppenkopf()
    {
        var cut = Aufbauen(Satz());

        var block = cut.Find(".epos-projektkopf > .epos-projektkopf-formular");
        Assert.Single(block.QuerySelectorAll(".epos-formularraster"));
        Assert.Single(block.QuerySelectorAll("textarea"));
        Assert.Single(block.QuerySelectorAll(".epos-projektkopf-einleitung"));
        Assert.Empty(block.QuerySelectorAll(".epos-gruppenkopf"));
    }

    /// <summary>
    /// Die Regeln der Projektkopfseite im Stilblatt: höchstens 60 rem breit, genau
    /// zwei Spalten, Beschriftung ÜBER dem Feld, darunter 900 px eine Spalte. Alles
    /// hängt an <c>.epos-projektkopf</c> — der Hausraster der übrigen Masken bleibt
    /// unberührt (<c>FormularrasterTests</c>).
    /// </summary>
    [Fact]
    public void Das_Stilblatt_ordnet_den_Projektkopf_kompakt()
    {
        Assert.Contains("max-width: 60rem;", Stilblock(".epos-projektkopf-formular {"), StringComparison.Ordinal);
        Assert.Contains("repeat(2, minmax(0, 1fr))",
                        Stilblock(".epos-projektkopf .epos-formularraster {"), StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1fr);",
                        Stilblock(".epos-projektkopf .epos-formularraster .epos-feld {"), StringComparison.Ordinal);
        Assert.Contains("left: 0;",
                        Stilblock(".epos-projektkopf .epos-formularraster .epos-suchauswahl-liste {"), StringComparison.Ordinal);

        string css = Stilblatt();
        int a = css.IndexOf(".epos-projektkopf .epos-formularraster {", StringComparison.Ordinal);
        int m = css.IndexOf("@media (max-width: 900px)", a, StringComparison.Ordinal);
        Assert.True(m > a, "Der Projektkopf bricht nicht bei 900 px um");
        int r = css.IndexOf(".epos-projektkopf .epos-formularraster {", m, StringComparison.Ordinal);
        Assert.True(r > m && r - m < 200, "Unter 900 px steht der Projektkopf nicht einspaltig");
    }

    private static string Stilblatt()
    {
        System.IO.DirectoryInfo? d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null
               && !System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return System.IO.File.ReadAllText(System.IO.Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    /// <summary>Der Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        string css = Stilblatt();
        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle #458, Stufe 2)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Die Seite meldet ihre fünf
    /// Felder über <c>ProjektKopfKiSicht</c> an: Die Klimaregion ist ein Wahlfeld und
    /// setzt Id UND Name, wie die Klappliste; Kunde schreibt an Ort und Stelle in den
    /// Kopf. Die Prüfung ist die Kopfregel der Seite; einen Speicherweg gibt es nicht.
    /// </summary>
    [Fact]
    public void Die_Seite_meldet_ihre_fuenf_Felder_beim_Assistenten_an()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;
        var cut = Aufbauen(daten);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PROJEKTKOPF));

        KiFeldzugang klima = KiMaskenbruecke.Feldzugang(KiMaskennamen.PROJEKTKOPF, "klimaregion");
        Assert.NotNull(klima);
        Assert.Equal(12, klima.Lesen());

        KiFeldumsetzung u = KiFeldwandler.Wandle(klima, "Region 05 Hamburg");
        Assert.True(u.Ok, u.Grund);
        klima.Setzen(u.Wert);
        Assert.Equal(5, daten.IdKlimaregion);
        Assert.Equal("Region 05 Hamburg", daten.Klimaname);

        KiFeldzugang kunde = KiMaskenbruecke.Feldzugang(KiMaskennamen.PROJEKTKOPF, "kunde");
        kunde.Setzen("Stadtwerke");
        Assert.Equal("Stadtwerke", daten.Kunde);

        // Die Pruefung ist die Kopfregel: ein leerer Name wird benannt.
        KiFeldzugang name = KiMaskenbruecke.Feldzugang(KiMaskennamen.PROJEKTKOPF, "name");
        name.Setzen("");
        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.PROJEKTKOPF);
        Assert.Equal(cut.Instance.TextNameLeer, haken.Befund());
        Assert.Null(haken.Speichern);

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.PROJEKTKOPF));
    }

    /// <summary>
    /// Im BEARBEITEN-Modus steht der Projektname fest — die Seite zeigt ihn nur lesbar,
    /// und der Assistent bekommt eine benannte Absage statt einer stillen Umbenennung.
    /// </summary>
    [Fact]
    public void Im_Bearbeiten_Modus_lehnt_der_Assistent_den_neuen_Namen_ab()
    {
        ProjektKopfDaten daten = Satz();               // NameAenderbar = false
        var cut = Aufbauen(daten);

        KiFeldzugang name = KiMaskenbruecke.Feldzugang(KiMaskennamen.PROJEKTKOPF, "name");
        Assert.NotNull(name);

        var fehler = Assert.Throws<InvalidOperationException>(() => name.Setzen("Anderer Name"));
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KI_DLG_PKOPF_NAME_FEST, fehler.Message);
        Assert.Equal("Laurentiuskirche", daten.Name);

        cut.Instance.Dispose();
    }

    // =====================================================================
    //  #527: die Klappliste der Kopfleiste
    // =====================================================================

    /// <summary>Einträge in der Kurzform der Kopfleiste; die blanken Stammnamen daneben.</summary>
    private static readonly (int Id, string Text)[] KURZFORM =
    {
        (17, "Berlin (TRY 2045 sommerwarm)"),
        (47, "München (PVGIS)")
    };

    private static readonly Dictionary<int, string> STAMMNAMEN = new()
    {
        [17] = "Berlin",
        [47] = "München"
    };

    private IRenderedComponent<ProjektKopfSeite> MitKurzform(ProjektKopfDaten daten,
                                                             Func<int, EPOS.UI.Seiten.Start.KlimaHerkunftGaben?>? herkunft = null)
        => Render<ProjektKopfSeite>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Klimaregionen, KURZFORM)
            .Add(x => x.Klimanamen, STAMMNAMEN)
            .Add(x => x.KlimaHerkunft, herkunft)
            .Add(x => x.PflichtMarke, " *"));

    /// <summary>
    /// Die Klappliste ist gefüllt — mit den Einträgen der Kopfleiste in ihrer
    /// Reihenfolge und Kurzform —, und die Wahl trägt die Stamm-Id und den BLANKEN
    /// Stammnamen, nicht den Anzeigetext mit der Klammer (der Speicherweg sucht mit ihm).
    /// </summary>
    [Fact]
    public void Die_Klappliste_zeigt_die_Kurzform_und_die_Wahl_traegt_den_Stammnamen()
    {
        ProjektKopfDaten daten = Satz();
        daten.IdKlimaregion = 0;
        daten.Klimaname = "";
        var cut = MitKurzform(daten);

        cut.Find(KLIMAFELD).Click();
        Assert.Equal(new[] { "Berlin (TRY 2045 sommerwarm)", "München (PVGIS)" },
                     cut.FindAll("li[role=option]").Select(li => li.TextContent.Trim()).ToArray());
        Assert.Equal("Region auswählen", cut.Find(KLIMAFELD).GetAttribute("placeholder"));
        Assert.Contains("Klimaregion *", cut.Markup);

        cut.FindAll("li[role=option]").First(li => li.TextContent.Contains("Berlin")).Click();

        Assert.Equal(17, daten.IdKlimaregion);
        Assert.Equal("Berlin", daten.Klimaname);
        Assert.Equal("Berlin (TRY 2045 sommerwarm)", cut.Find(KLIMAFELD).GetAttribute("value"));
        Assert.Equal(ProjektKopfBefund.Ok, cut.Instance.Befund);
    }

    /// <summary>
    /// Ein Kopf, der nur den blanken NAMEN führt, wird über die Stammnamen zugeordnet —
    /// der Anzeigetext mit der Klammer träfe ihn nicht.
    /// </summary>
    [Fact]
    public void Ein_Kopf_nur_mit_Stammnamen_findet_seinen_Eintrag()
    {
        ProjektKopfDaten daten = Satz();
        daten.IdKlimaregion = 0;
        daten.Klimaname = "München";
        var cut = MitKurzform(daten);

        Assert.Equal("München (PVGIS)", cut.Find(KLIMAFELD).GetAttribute("value"));
    }

    /// <summary>
    /// Unter der Klappliste steht die Herkunftszeile der Wahl — derselbe Satz wie in der
    /// Kopfleiste; ohne Wahl keine Zeile, und sie folgt einem Wechsel.
    /// </summary>
    [Fact]
    public void Unter_der_Klappliste_steht_die_Herkunft_der_Wahl()
    {
        ProjektKopfDaten daten = Satz();
        daten.IdKlimaregion = 0;
        daten.Klimaname = "";
        var gefragt = new List<int>();
        var cut = MitKurzform(daten, id =>
        {
            gefragt.Add(id);
            return id == 17
                ? new EPOS.UI.Seiten.Start.KlimaHerkunftGaben("TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm",
                                                              "Berlin", "Berlin", "21.09.2026")
                : new EPOS.UI.Seiten.Start.KlimaHerkunftGaben("", "München", "11,5000° / 48,1000°", "");
        });

        Assert.DoesNotContain("Klimadaten:", cut.Markup);

        Waehlen(cut, "Berlin (TRY 2045 sommerwarm)");
        Assert.Contains("Klimadaten: TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm · Berlin · Berlin · Import 21.09.2026",
                        cut.Markup);

        // Ein Tastendruck in einem anderen Feld fragt die Herkunft nicht neu.
        int vorher = gefragt.Count;
        cut.FindAll(TEXTFELDER + ":not([readonly])")[0].Input("Kunde X");
        Assert.Equal(vorher, gefragt.Count);

        Waehlen(cut, "München (PVGIS)");
        Assert.Contains("Klimadaten: München · 11,5000° / 48,1000°", cut.Markup);
    }
}
