using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die globalen Anwendungseinstellungen (iU9-W14c.6). Soll ist die Feldkarte der
/// gelöschten Maske <c>Form_AdminSettings</c> (28 Kartenzeilen): eine Rubrikenliste
/// mit SECHS Einträgen, elf Textfelder, fünf „Durchsuchen…"-Knöpfe, drei
/// Fußknöpfe — <b>und die zwei Steuerelemente, die zur LAUFZEIT entstanden</b>
/// (<c>chk_KiAus</c>, <c>lbl_KiAus</c>): Die Feldkarte sah sie nicht (R-W14c-6),
/// hier stehen sie.
///
/// <para>Die fünfte Rubrik „Klimadaten" (KL1-A) steht HINTER „Web-Schnittstellen
/// (API)": PVGIS-Adresse (aus der Web-Rubrik hierher gewandert), Portal der
/// DWD-Testreferenzjahre und Adresse der TRY-Regionaldaten.</para>
///
/// <para>Die sechste Rubrik „Diagramme" (DF-1, Anwenderentscheid 20.09.2026) steht
/// zwischen „Klimadaten" und „Anwendung": je Farbrolle ein <c>Farbfeld</c>, in
/// sechs Gruppen, dazu der Knopf „Hausfarben". <b>Die Reiterindizes 0 bis 3 bleiben,
/// was sie waren</b> — „Anwendung" rückt von 4 auf 5.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8).</para>
/// </summary>
public class EinstellungenDialogTests : EposBunitContext
{
    private static Einstellungensatz Satz() => new Einstellungensatz
    {
        VdiPfad = @"C:\Users\x\AppData\Local\WP-Plan",
        DbExportPfad = @"C:\Users\x\AppData\Local\WP-Plan\Backup",
        DbImportPfad = @"C:\Users\x\AppData\Local\WP-Plan\Import",
        DbPfad = @"C:\ProgramData\EPOS_PLAN",
        DbName = "Kenndaten.sqlite",
        WikiUrl = "https://wiki.epos-plan.de",
        PvgisUrl = "https://re.jrc.ec.europa.eu/api/tmy",
        GeokodierungUrl = "https://nominatim.openstreetmap.org",
        TryPortalUrl = "https://kunden.dwd.de/obt/",
        TryRegionalUrl = "https://github.com/RE-Lab-Projects/TRY_DE_2015_2045/releases/download/v1.4.0/data.zip",
        AllgemeinPfad = @"C:\Users\x\AppData\Local\WP-Plan"
    };

    public EinstellungenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<EinstellungenDialog> Zeige(
        Einstellungensatz? satz = null,
        bool kiAus = false,
        bool riegel = false,
        bool kiLesbar = true,
        Func<string, Task<string?>>? waehler = null,
        Func<Einstellungensatz, bool, Task<SpeicherBefund>>? speichern = null,
        Func<Task<Einstellungensatz>>? zuruecksetzen = null,
        Action<bool>? geschlossen = null,
        IReadOnlyList<Farbrollengabe>? farbrollen = null)
    {
        return Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, satz ?? Satz())
            .Add(x => x.Farbrollen, farbrollen ?? Diagrammfarben.Gaben())
            .Add(x => x.KiAbgeschaltet, kiAus)
            .Add(x => x.MaschinenRiegel, riegel)
            .Add(x => x.KiLesbar, kiLesbar)
            .Add(x => x.OrdnerWaehler, waehler ?? (_ => Task.FromResult<string?>(@"D:\Neu")))
            .Add(x => x.Speichern, speichern ??
                ((_, _) => Task.FromResult(new SpeicherBefund(true, ""))))
            .Add(x => x.Zuruecksetzen, zuruecksetzen ?? (() => Task.FromResult(new Einstellungensatz())))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    private static void Reiter(IRenderedComponent<EinstellungenDialog> cut, int nummer)
        => cut.FindAll(".epos-reiter-knopf")[nummer].Click();

    // =====================================================================
    //  Feldbestand (Feldkarte Form_AdminSettings, 28 Zeilen)
    // =====================================================================

    /// <summary>
    /// Die Rubrikenliste IST eine Reiterleiste (A-16): fünf Blätter mit
    /// sprachneutralem Schlüssel statt Panels über den Index (Befunde
    /// W14c-B50/B51).
    ///
    /// <para><b>Die Folge zählt</b> (KL1-A): „Klimadaten" steht HINTER
    /// „Web-Schnittstellen (API)", damit die Indizes 0 bis 2 bleiben, was sie
    /// waren.</para>
    /// </summary>
    [Fact]
    public void Die_sechs_Rubriken_sind_sechs_Reiter()
    {
        var cut = Zeige();
        var reiter = cut.FindAll(".epos-reiter-knopf").Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "VDI Datensätze", "Datenbank", "Web-Schnittstellen (API)",
                             "Klimadaten", "Diagramme", "Anwendung" },
                     reiter);
    }

    [Fact]
    public void Jede_Rubrik_zeigt_ihre_Felder()
    {
        var cut = Zeige();

        // Rubrik 1: der VDI-Pfad - EIN Feld mit Durchsuchen-Knopf.
        Assert.Single(cut.FindAll("input[type=text]"));
        Assert.Single(cut.FindAll(".epos-dateiwahl button"));

        // Rubrik 2 "Datenbank": VIER Felder (Export, Import, DB-Pfad, DB-Name),
        // davon drei mit Knopf - genau der Bestand von panel_Export (Befund W14c-B50).
        Reiter(cut, 1);
        Assert.Equal(4, cut.FindAll("input[type=text]").Count);
        Assert.Equal(3, cut.FindAll(".epos-dateiwahl button").Count);

        // Rubrik 3 "Web-Schnittstellen (API)": Wiki und Nominatim, ohne Knopf -
        // die PVGIS-Adresse ist in die Klimarubrik gewandert (KL1-A).
        Reiter(cut, 2);
        Assert.Equal(2, cut.FindAll("input[type=text]").Count);
        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));

        // Rubrik 4 "Klimadaten": die drei Adressen, ohne Knopf.
        Reiter(cut, 3);
        Assert.Equal(3, cut.FindAll("input[type=text]").Count);
        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));

        // Rubrik 5 "Diagramme": je Farbrolle ein Waehler und ein Hexfeld, dazu das
        // Vorgabemuster - und keine Dateiwahl.
        Reiter(cut, 4);
        int rollen = Diagrammfarben.Rollen.Count;
        Assert.Equal(rollen, cut.FindAll("input[type=color]").Count);
        Assert.Equal(rollen, cut.FindAll(".epos-farbfeld-hex").Count);
        Assert.Equal(rollen, cut.FindAll(".epos-farbfeld-vorgabe").Count);
        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));

        // Rubrik 6: der Allgemein-Pfad UND der KI-Schalter.
        Reiter(cut, 5);
        Assert.Single(cut.FindAll("input[type=text]"));
        Assert.Single(cut.FindAll(".epos-dateiwahl button"));
        Assert.Single(cut.FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void Die_elf_Werte_stehen_in_den_Feldern()
    {
        var cut = Zeige();

        Assert.Equal(@"C:\Users\x\AppData\Local\WP-Plan", cut.Find("input[type=text]").GetAttribute("value"));

        Reiter(cut, 1);
        var felder = cut.FindAll("input[type=text]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Equal(@"C:\Users\x\AppData\Local\WP-Plan\Backup", felder[0]);
        Assert.Equal(@"C:\Users\x\AppData\Local\WP-Plan\Import", felder[1]);
        Assert.Equal(@"C:\ProgramData\EPOS_PLAN", felder[2]);
        Assert.Equal("Kenndaten.sqlite", felder[3]);           // A-12: der NAME im NAMENSfeld

        Reiter(cut, 2);
        felder = cut.FindAll("input[type=text]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Equal("https://wiki.epos-plan.de", felder[0]);
        Assert.Equal("https://nominatim.openstreetmap.org", felder[1]);

        // Die fuenfte Rubrik: PVGIS und die zwei TRY-Adressen (KL1-A).
        Reiter(cut, 3);
        felder = cut.FindAll("input[type=text]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Equal("https://re.jrc.ec.europa.eu/api/tmy", felder[0]);
        Assert.Equal("https://kunden.dwd.de/obt/", felder[1]);
        Assert.Equal("https://github.com/RE-Lab-Projects/TRY_DE_2015_2045/releases/download/v1.4.0/data.zip", felder[2]);
    }

    /// <summary>
    /// Befund W14c-B52: Ordner und Name der Datenbank wirken erst beim nächsten
    /// Start — das sagt der Dialog jetzt.
    /// </summary>
    [Fact]
    public void Die_Datenbankrubrik_sagt_dass_der_Neustart_zaehlt()
    {
        var cut = Zeige();
        Reiter(cut, 1);

        Assert.Contains("nächsten Programmstart", cut.Markup);
    }

    // =====================================================================
    //  Entscheid E-5: ohne Ordnerwaehler sind die Pfade fest
    // =====================================================================

    /// <summary>
    /// Der Dialog OHNE Ordnerwähler — die iOS-Lage: <c>OrdnerWaehlen</c> liefert dort
    /// immer <c>""</c>, es gibt also keinen Wähler.
    /// </summary>
    private IRenderedComponent<EinstellungenDialog> OhneWaehler(
        Func<Einstellungensatz, bool, Task<SpeicherBefund>>? speichern = null,
        Func<Task<Einstellungensatz>>? zuruecksetzen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, Satz())
            .Add(x => x.Speichern, speichern ??
                ((_, _) => Task.FromResult(new SpeicherBefund(true, ""))))
            .Add(x => x.Zuruecksetzen, zuruecksetzen ??
                (() => Task.FromResult(new Einstellungensatz())))
            .Add(x => x.Geschlossen, geschlossen ?? (_ => { })));
    }

    /// <summary>Zählt über alle sechs Rubriken — ein Reiterblatt zeichnet nur, wenn es aktiv ist.</summary>
    private static (int Felder, int NurLesend, int Knoepfe) Pfadfelder(
        IRenderedComponent<EinstellungenDialog> cut)
    {
        int felder = 0, lesend = 0, knoepfe = 0;
        for (int r = 0; r < 6; r++)
        {
            Reiter(cut, r);
            var pfade = cut.FindAll(".epos-dateiwahl input");
            felder += pfade.Count;
            lesend += pfade.Count(e => e.HasAttribute("readonly"));
            knoepfe += cut.FindAll(".epos-dateiwahl button").Count;
        }
        return (felder, lesend, knoepfe);
    }

    /// <summary>
    /// Entscheid E-5, Windows-Seite: <b>Mit Wähler bleibt alles wie bisher</b> — fünf
    /// beschreibbare Pfadfelder, fünf „Durchsuchen…"-Knöpfe, kein Hinweis.
    /// </summary>
    [Fact]
    public void Mit_Ordnerwaehler_sind_die_fuenf_Pfade_beschreibbar()
    {
        var cut = Zeige();

        var (felder, lesend, knoepfe) = Pfadfelder(cut);

        Assert.Equal(5, felder);
        Assert.Equal(0, lesend);
        Assert.Equal(5, knoepfe);
        Assert.DoesNotContain("fest vorgegeben", cut.Markup);
    }

    /// <summary>
    /// Entscheid E-5, iOS-Seite: <b>Kein Wähler, feste Pfade.</b> Die fünf Pfadfelder
    /// sind nur lesend, es gibt keinen Knopf, und über der Rubrikenleiste steht,
    /// warum das so ist.
    /// </summary>
    [Fact]
    public void Ohne_Ordnerwaehler_sind_die_fuenf_Pfade_fest_und_nur_lesend()
    {
        var cut = OhneWaehler();

        var (felder, lesend, knoepfe) = Pfadfelder(cut);

        Assert.Equal(5, felder);
        Assert.Equal(5, lesend);          // alle fünf nur lesend
        Assert.Equal(0, knoepfe);         // kein „Durchsuchen…"
        Assert.Contains("Die Ordner sind auf dieser Plattform fest vorgegeben.", cut.Markup);
    }

    /// <summary>
    /// Der sichtbare Wert ist der des Kerns (<c>EinstellungenCtrl</c>) — auf iOS die
    /// Sandbox-Pfade. Nur lesend heißt nicht leer.
    /// </summary>
    [Fact]
    public void Ohne_Ordnerwaehler_zeigen_die_Felder_die_Vorgaben_des_Kerns()
    {
        var cut = OhneWaehler();

        Assert.Equal(@"C:\Users\x\AppData\Local\WP-Plan",
                     cut.Find(".epos-dateiwahl input").GetAttribute("value"));
    }

    /// <summary>
    /// Entscheid E-5: „Speichern" schreibt die übrigen Werte unverändert; <b>die fünf
    /// Pfade gehen als das zurück, was der Kern vorgegeben hat</b> — der Dialog
    /// überschreibt sie nicht.
    /// </summary>
    [Fact]
    public void Ohne_Ordnerwaehler_speichert_der_Dialog_die_Nicht_Pfad_Werte()
    {
        Einstellungensatz? uebergeben = null;
        bool? ergebnis = null;
        var cut = OhneWaehler(
            speichern: (s, _) =>
            {
                uebergeben = s;
                return Task.FromResult(new SpeicherBefund(true, ""));
            },
            geschlossen: b => ergebnis = b);

        // Eine Nicht-Pfad-Rubrik bearbeiten: die Klimarubrik, Feld 0 ist PVGIS.
        Reiter(cut, 3);
        cut.FindAll("input[type=text]")[0].Input("https://neu/api/tmy");

        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.NotNull(uebergeben);
        Assert.Equal("https://neu/api/tmy", uebergeben!.PvgisUrl);          // geschrieben
        Assert.Equal("https://wiki.epos-plan.de", uebergeben.WikiUrl);      // unverändert
        Assert.Equal("Kenndaten.sqlite", uebergeben.DbName);                // kein Pfad, bleibt
        Assert.Equal(@"C:\Users\x\AppData\Local\WP-Plan", uebergeben.VdiPfad);
        Assert.Equal(@"C:\ProgramData\EPOS_PLAN", uebergeben.DbPfad);
        Assert.True(ergebnis);
    }

    /// <summary>
    /// Auch „Standardwerte" ändert die festen Pfade nicht: Ohne Wähler gibt es keinen
    /// Weg, eine andere Vorgabe als die des Kerns wirksam zu machen. Die übrigen
    /// Werksstandards greifen wie sonst.
    /// </summary>
    [Fact]
    public void Ohne_Ordnerwaehler_laesst_Standardwerte_die_Pfade_stehen()
    {
        var cut = OhneWaehler(zuruecksetzen: () => Task.FromResult(new Einstellungensatz
        {
            VdiPfad = @"D:\Werk",
            DbExportPfad = @"D:\Werk",
            DbImportPfad = @"D:\Werk",
            DbPfad = @"D:\Werk",
            AllgemeinPfad = @"D:\Werk",
            DbName = "Kenndaten.sqlite",
            PvgisUrl = "https://re.jrc.ec.europa.eu/api/tmy",
            TryPortalUrl = "https://kunden.dwd.de/obt/",
            TryRegionalUrl = "https://github.com/RE-Lab-Projects/TRY_DE_2015_2045/releases/download/v1.4.0/data.zip"
        }));

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(@"C:\Users\x\AppData\Local\WP-Plan", cut.Instance.Werte.VdiPfad);
        Assert.Equal(@"C:\ProgramData\EPOS_PLAN", cut.Instance.Werte.DbPfad);
        Assert.Equal("https://re.jrc.ec.europa.eu/api/tmy", cut.Instance.Werte.PvgisUrl);
        Assert.Equal("https://kunden.dwd.de/obt/", cut.Instance.Werte.TryPortalUrl);
        Assert.Equal("https://github.com/RE-Lab-Projects/TRY_DE_2015_2045/releases/download/v1.4.0/data.zip", cut.Instance.Werte.TryRegionalUrl);
    }

    [Fact]
    public void Der_Ordnerwaehler_uebernimmt_den_gewaehlten_Pfad()
    {
        var cut = Zeige(waehler: _ => Task.FromResult<string?>(@"D:\VDI"));

        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal(@"D:\VDI", cut.Instance.Werte.VdiPfad);
    }

    // =====================================================================
    //  Der KI-Schalter (R-W14c-6: zwei Laufzeit-Steuerelemente)
    // =====================================================================

    [Fact]
    public void Der_KI_Schalter_zeigt_den_Registry_Stand()
    {
        var cut = Zeige(kiAus: true);
        Reiter(cut, 5);

        Assert.True(cut.Instance.KiAus);
        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    /// <summary>
    /// Ein maschinenweiter Riegel (HKLM) ist die Sperre der Verwaltung — sie darf sich
    /// hier nicht lösen lassen, und der Grund steht daneben.
    /// </summary>
    [Fact]
    public void Ein_Maschinenriegel_sperrt_den_Schalter_und_sagt_warum()
    {
        var cut = Zeige(kiAus: true, riegel: true);
        Reiter(cut, 5);

        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("disabled"));
        Assert.Contains("verwaltungsseitig gesperrt", cut.Markup);
    }

    /// <summary>
    /// Befund W14c-B49: Konnte der Schalter nicht gelesen werden, verschwand er
    /// kommentarlos. Jetzt sagt ein Banner, was los ist.
    /// </summary>
    [Fact]
    public void Ein_unlesbarer_KI_Schalter_meldet_sich()
    {
        var cut = Zeige(kiLesbar: false);

        Assert.Contains("KI-Schalter", cut.Instance.Meldung);
    }

    [Fact]
    public void Bei_Maschinenriegel_wird_der_Schalter_nicht_geschrieben()
    {
        bool? geschrieben = null;
        var cut = Zeige(kiAus: false, riegel: true, speichern: (_, ki) =>
        {
            geschrieben = ki;
            return Task.FromResult(new SpeicherBefund(true, ""));
        });

        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.Equal(false, geschrieben);          // der GELESENE Stand, nicht der Feldstand
    }

    // =====================================================================
    //  DL-2 Nr. 10 — die EINE Fussleiste (Entscheid DL-Q6)
    // =====================================================================

    /// <summary>
    /// Der Dialog trägt <b>genau eine</b> Fußleiste, und sie läuft
    /// <b>Standardwerte · Füller · Abbrechen · OK (primär)</b>. „Standardwerte" wirkt
    /// auf alle sechs Rubriken zugleich und steht deshalb im Aktionsschlitz der
    /// <c>SpeichernLeiste</c>, links vom Füller — die zweite Leiste darüber ist
    /// entfallen (Konzept Knopfleisten der Administrationsdialoge, Nr. 10;
    /// Anwenderentscheid DL-Q6 vom 20.09.2026: der Schlussknopf heißt „OK").
    /// </summary>
    [Fact]
    public void Die_Fussleiste_traegt_Standardwerte_Abbrechen_und_OK()
    {
        var cut = Zeige();

        var leisten = cut.FindAll(".epos-einstellungen > .epos-leiste");
        Assert.Single(leisten);

        var fuss = leisten[0];
        Assert.Equal(
            new[] { "Standardwerte", "Abbrechen", "OK" },
            fuss.QuerySelectorAll("button").Select(k => k.TextContent.Trim()).ToArray());

        // Der Fueller ist die Statusspanne der SpeichernLeiste; sie steht HINTER
        // dem Aktionsschlitz und schiebt Abbrechen und OK nach rechts.
        Assert.NotNull(fuss.QuerySelector(".epos-status"));
        Assert.Equal("Standardwerte",
                     fuss.Children.OfType<AngleSharp.Dom.IElement>()
                         .TakeWhile(e => !e.ClassList.Contains("epos-status"))
                         .Last().TextContent.Trim());

        // Genau EIN primaerer Knopf, und er steht zuletzt.
        var primaer = Assert.Single(fuss.QuerySelectorAll(".epos-knopf--primaer"));
        Assert.Equal("OK", primaer.TextContent.Trim());
    }

    // =====================================================================
    //  Speichern und Standardwerte
    // =====================================================================

    [Fact]
    public void Speichern_gibt_die_elf_Werte_weiter_und_schliesst()
    {
        Einstellungensatz? uebergeben = null;
        bool? ergebnis = null;
        var cut = Zeige(speichern: (s, _) =>
        {
            uebergeben = s;
            return Task.FromResult(new SpeicherBefund(true, ""));
        }, geschlossen: b => ergebnis = b);

        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.NotNull(uebergeben);
        Assert.Equal("Kenndaten.sqlite", uebergeben!.DbName);
        Assert.Equal(@"C:\ProgramData\EPOS_PLAN", uebergeben.DbPfad);
        Assert.Equal("https://re.jrc.ec.europa.eu/api/tmy", uebergeben.PvgisUrl);
        Assert.Equal("https://kunden.dwd.de/obt/", uebergeben.TryPortalUrl);
        Assert.Equal("https://github.com/RE-Lab-Projects/TRY_DE_2015_2045/releases/download/v1.4.0/data.zip", uebergeben.TryRegionalUrl);
        Assert.True(ergebnis);
    }

    /// <summary>
    /// <b>Die Klimarubrik bindet und speichert die zwei TRY-Adressen</b> (KL1-A): Was
    /// in Feld 1 und 2 der fünften Rubrik getippt wird, steht im Wertesatz, den
    /// „Speichern" weiterreicht — die PVGIS-Adresse daneben bleibt, was sie war.
    /// </summary>
    [Fact]
    public void Die_Klimarubrik_bindet_und_speichert_die_zwei_TRY_Adressen()
    {
        Einstellungensatz? uebergeben = null;
        var cut = Zeige(speichern: (s, _) =>
        {
            uebergeben = s;
            return Task.FromResult(new SpeicherBefund(true, ""));
        });

        Reiter(cut, 3);
        Assert.Equal(3, cut.FindAll("input[type=text]").Count);
        cut.FindAll("input[type=text]")[1].Input("https://portal/neu/");
        cut.FindAll("input[type=text]")[2].Input("https://regional/neu/data.zip");

        Assert.Equal("https://portal/neu/", cut.Instance.Werte.TryPortalUrl);
        Assert.Equal("https://regional/neu/data.zip", cut.Instance.Werte.TryRegionalUrl);

        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.NotNull(uebergeben);
        Assert.Equal("https://portal/neu/", uebergeben!.TryPortalUrl);
        Assert.Equal("https://regional/neu/data.zip", uebergeben.TryRegionalUrl);
        Assert.Equal("https://re.jrc.ec.europa.eu/api/tmy", uebergeben.PvgisUrl);
    }

    /// <summary>Ein Fehlschlag beim Ordneranlegen hält den Dialog offen und meldet sich.</summary>
    [Fact]
    public void Ein_Ordnerfehler_haelt_den_Dialog_offen()
    {
        bool? ergebnis = null;
        var cut = Zeige(
            speichern: (_, _) => Task.FromResult(
                new SpeicherBefund(false, "Die Ordner konnten nicht erstellt werden.")),
            geschlossen: b => ergebnis = b);

        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.Null(ergebnis);
        Assert.Contains("Ordner", cut.Instance.Meldung);
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_speichern()
    {
        int gespeichert = 0;
        bool? ergebnis = null;
        var cut = Zeige(speichern: (_, _) =>
        {
            gespeichert++;
            return Task.FromResult(new SpeicherBefund(true, ""));
        }, geschlossen: b => ergebnis = b);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.Equal(0, gespeichert);
        Assert.False(ergebnis);
    }

    /// <summary>
    /// „Standardwerte" fragt mit Vorgabe „Nein" (A-1) und SPEICHERT NICHT; die
    /// Meldung nennt den Weg, auf dem die geladenen Werte ankommen: „Die
    /// Standardwerte wurden geladen. Mit ‚OK' werden sie übernommen." (Bis DL-2
    /// hieß der Schlussknopf „Speichern", und die Meldung nannte ihn so.)
    /// </summary>
    [Fact]
    public void Standardwerte_fragen_laden_aber_speichern_nicht()
    {
        int gespeichert = 0, zurueckgesetzt = 0;
        var cut = Zeige(
            speichern: (_, _) => { gespeichert++; return Task.FromResult(new SpeicherBefund(true, "")); },
            zuruecksetzen: () =>
            {
                zurueckgesetzt++;
                return Task.FromResult(new Einstellungensatz
                {
                    VdiPfad = @"C:\Vorgabe",
                    DbName = "Kenndaten.sqlite",
                    DbPfad = @"C:\ProgramData\EPOS_PLAN"
                });
            });

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();

        var frage = cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>();
        Assert.True(frage.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);

        frage.FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.Equal(0, zurueckgesetzt);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(1, zurueckgesetzt);
        Assert.Equal(0, gespeichert);                            // NICHT gespeichert
        Assert.Equal(@"C:\Vorgabe", cut.Instance.Werte.VdiPfad);
        Assert.Contains("OK", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Befund W14c-B53, behoben</b> (A-12): Der Vorläufer setzte den DB-NAMEN in
    /// das PFADfeld und liess das Namensfeld unberührt.
    /// </summary>
    [Fact]
    public void Standardwerte_setzen_den_Namen_ins_Namensfeld()
    {
        var cut = Zeige(zuruecksetzen: () => Task.FromResult(new Einstellungensatz
        {
            DbPfad = @"C:\ProgramData\EPOS_PLAN",
            DbName = "Kenndaten.sqlite"
        }));

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(@"C:\ProgramData\EPOS_PLAN", cut.Instance.Werte.DbPfad);
        Assert.Equal("Kenndaten.sqlite", cut.Instance.Werte.DbName);
    }

    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Rueckfrage()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.Find("div.epos-einstellungen").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);

        cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>()
           .FindAll("button").First(b => b.TextContent.Trim() == "Nein").Click();
        cut.Find("div.epos-einstellungen").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc: Abbrechen ohne zu speichern.</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    // =====================================================================
    //  Die Rubrik „Diagramme" (DF-1)
    // =====================================================================

    /// <summary>Die Rubrik ist der fünfte Reiter (Index 4) — dorthin schaltet jeder Fall unten.</summary>
    private const int REITER_DIAGRAMME = 4;

    /// <summary>Das Hexfeld einer Rolle.</summary>
    private static AngleSharp.Dom.IElement Hexfeld(
        IRenderedComponent<EinstellungenDialog> cut, string rolle)
        => cut.Find($"#epos-farbe-{rolle} ~ .epos-farbfeld-hex");

    /// <summary>
    /// Die Rubrik zeigt <b>alle</b> Farbrollen — in den sechs Gruppen des
    /// Zeichenmodells, jede mit Anzeigename, Wähler, Hexfeld und dem Muster der
    /// Hausfarbe daneben.
    /// </summary>
    [Fact]
    public void Die_Diagrammrubrik_zeigt_alle_Farbrollen_in_ihren_Gruppen()
    {
        var cut = Zeige();
        Reiter(cut, REITER_DIAGRAMME);

        Assert.Equal(Diagrammfarben.Rollen.Count, cut.FindAll(".epos-farbfeld").Count);
        Assert.Equal(Diagrammfarben.Gruppen.Count,
                     cut.FindAll(".epos-gruppenkopf-titel").Count);

        // Der Anzeigename kommt aus der Ressource, nicht aus dem Schluessel.
        Assert.Contains("Wärmepumpe", cut.Markup);
        Assert.Contains("Erzeuger und Bedarf", cut.Markup);

        // Die Hausfarbe steht als Wert im Waehler UND als Muster daneben.
        Assert.Equal("#4172C4", cut.Find("#epos-farbe-WAERME_WP").GetAttribute("value"));
        Assert.Equal("#4172C4", Hexfeld(cut, "WAERME_WP").GetAttribute("value"));
    }

    /// <summary>Ein gespeicherter Stand steht in den Feldern — und nur er weicht ab.</summary>
    [Fact]
    public void Die_gespeicherten_Farben_stehen_in_den_Feldern()
    {
        Einstellungensatz satz = Satz();
        satz.DiagrammFarben = "WAERME_WP=#FF0000";

        var cut = Zeige(satz);
        Reiter(cut, REITER_DIAGRAMME);

        Assert.Equal("#FF0000", cut.Find("#epos-farbe-WAERME_WP").GetAttribute("value"));
        Assert.Equal("#70AD47", cut.Find("#epos-farbe-STROM_PV").GetAttribute("value"));
    }

    /// <summary>
    /// <b>Die Aussage der Rubrik:</b> Eine geänderte Farbe landet im Wertesatz, den
    /// „OK" weiterreicht — und zwar als kompakter Text mit NUR der abweichenden
    /// Rolle.
    /// </summary>
    [Fact]
    public void Eine_geaenderte_Farbe_landet_im_Satz()
    {
        Einstellungensatz? uebergeben = null;
        var cut = Zeige(speichern: (s, _) =>
        {
            uebergeben = s;
            return Task.FromResult(new SpeicherBefund(true, ""));
        });

        Reiter(cut, REITER_DIAGRAMME);
        cut.Find("#epos-farbe-WAERME_WP").Change("#ff0000");
        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.NotNull(uebergeben);
        Assert.Equal("WAERME_WP=#FF0000", uebergeben!.DiagrammFarben);
    }

    /// <summary>Dasselbe über das Hexfeld — es ist beschreibbar, nicht nur Anzeige.</summary>
    [Fact]
    public void Das_Hexfeld_nimmt_eine_getippte_Farbe_entgegen()
    {
        Einstellungensatz? uebergeben = null;
        var cut = Zeige(speichern: (s, _) =>
        {
            uebergeben = s;
            return Task.FromResult(new SpeicherBefund(true, ""));
        });

        Reiter(cut, REITER_DIAGRAMME);
        Hexfeld(cut, "SPEICHER_2").Change("#123456");
        cut.FindAll("button.epos-knopf--primaer").Last().Click();

        Assert.Equal("SPEICHER_2=#123456", uebergeben!.DiagrammFarben);
    }

    /// <summary>
    /// Ein ungültiges Hex wird benannt abgewiesen: Das Feld meldet sich, und der
    /// Wertesatz bleibt unberührt — eine Fehleingabe kann gar nicht gespeichert
    /// werden.
    /// </summary>
    [Fact]
    public void Ein_ungueltiges_Hex_wird_abgewiesen_und_nicht_gespeichert()
    {
        Einstellungensatz? uebergeben = null;
        var cut = Zeige(speichern: (s, _) =>
        {
            uebergeben = s;
            return Task.FromResult(new SpeicherBefund(true, ""));
        });

        Reiter(cut, REITER_DIAGRAMME);
        Hexfeld(cut, "WAERME_WP").Change("rot");

        Assert.Single(cut.FindAll(".epos-farbfeld--ungueltig"));
        Assert.Contains("#RRGGBB", cut.Markup);

        cut.FindAll("button.epos-knopf--primaer").Last().Click();
        Assert.Equal("", uebergeben!.DiagrammFarben);
    }

    /// <summary>
    /// „Hausfarben" setzt alle Rollen zurück, verwirft eine stehengebliebene
    /// Fehleingabe — und <b>speichert nicht</b>, wörtlich wie „Standardwerte".
    /// </summary>
    [Fact]
    public void Hausfarben_setzt_zurueck_und_speichert_nicht()
    {
        bool gespeichert = false;
        var cut = Zeige(speichern: (_, _) =>
        {
            gespeichert = true;
            return Task.FromResult(new SpeicherBefund(true, ""));
        });

        Reiter(cut, REITER_DIAGRAMME);
        cut.Find("#epos-farbe-WAERME_WP").Change("#ff0000");
        Hexfeld(cut, "STROM_PV").Change("gruen");
        Assert.Single(cut.FindAll(".epos-farbfeld--ungueltig"));

        cut.Find(".epos-farbfeld-fuss button").Click();

        Assert.Equal("#4172C4", cut.Find("#epos-farbe-WAERME_WP").GetAttribute("value"));
        Assert.Equal("#70AD47", Hexfeld(cut, "STROM_PV").GetAttribute("value"));
        Assert.Empty(cut.FindAll(".epos-farbfeld--ungueltig"));
        Assert.Contains("Hausfarben wurden geladen", cut.Instance.Meldung);
        Assert.False(gespeichert);
    }

    /// <summary>
    /// Ohne Rollenliste steht die Rubrik leer da, statt zu fehlen — die Hausregel
    /// „jede Ansicht zeichnet auch ohne Gaben".
    /// </summary>
    [Fact]
    public void Ohne_Farbrollen_bleibt_die_Rubrik_leer_und_der_Dialog_steht()
    {
        var cut = Zeige(farbrollen: new List<Farbrollengabe>());
        Reiter(cut, REITER_DIAGRAMME);

        Assert.Empty(cut.FindAll(".epos-farbfeld"));
        Assert.Equal(6, cut.FindAll(".epos-reiter-knopf").Count);
    }
}
