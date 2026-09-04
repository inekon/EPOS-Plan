using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die globalen Anwendungseinstellungen (iU9-W14c.6). Soll ist die Feldkarte der
/// gelöschten Maske <c>Form_AdminSettings</c> (28 Kartenzeilen): eine Rubrikenliste
/// mit VIER Einträgen, neun Textfelder, fünf „Durchsuchen…"-Knöpfe, drei
/// Fußknöpfe — <b>und die zwei Steuerelemente, die zur LAUFZEIT entstanden</b>
/// (<c>chk_KiAus</c>, <c>lbl_KiAus</c>): Die Feldkarte sah sie nicht (R-W14c-6),
/// hier stehen sie.
///
/// <para>Die Kultur ist auf de-DE gepinnt (Regel seit W8).</para>
/// </summary>
public class EinstellungenDialogTests : BunitContext
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
        AllgemeinPfad = @"C:\Users\x\AppData\Local\WP-Plan"
    };

    public EinstellungenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    private IRenderedComponent<EinstellungenDialog> Zeige(
        Einstellungensatz? satz = null,
        bool kiAus = false,
        bool riegel = false,
        bool kiLesbar = true,
        Func<string, Task<string?>>? waehler = null,
        Func<Einstellungensatz, bool, Task<SpeicherBefund>>? speichern = null,
        Func<Task<Einstellungensatz>>? zuruecksetzen = null,
        Action<bool>? geschlossen = null)
    {
        return Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, satz ?? Satz())
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
    /// Die Rubrikenliste IST eine Reiterleiste (A-16): vier Blätter mit
    /// sprachneutralem Schlüssel statt vier Panels über den Index (Befunde
    /// W14c-B50/B51).
    /// </summary>
    [Fact]
    public void Die_vier_Rubriken_sind_vier_Reiter()
    {
        var cut = Zeige();
        var reiter = cut.FindAll(".epos-reiter-knopf").Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "VDI Datensätze", "Datenbank", "Web-Schnittstellen (API)", "Anwendung" },
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

        // Rubrik 3: die drei URLs, ohne Knopf.
        Reiter(cut, 2);
        Assert.Equal(3, cut.FindAll("input[type=text]").Count);
        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));

        // Rubrik 4: der Allgemein-Pfad UND der KI-Schalter.
        Reiter(cut, 3);
        Assert.Single(cut.FindAll("input[type=text]"));
        Assert.Single(cut.FindAll(".epos-dateiwahl button"));
        Assert.Single(cut.FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void Die_neun_Werte_stehen_in_den_Feldern()
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
        Assert.Equal("https://re.jrc.ec.europa.eu/api/tmy", felder[0]);
        Assert.Equal("https://wiki.epos-plan.de", felder[1]);
        Assert.Equal("https://nominatim.openstreetmap.org", felder[2]);
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

    /// <summary>
    /// Entscheid E-5: <b>Kein Wähler, kein Knopf.</b> Auf iOS liefert
    /// <c>OrdnerWaehlen</c> immer <c>""</c> — die Pfadfelder bleiben beschreibbar.
    /// </summary>
    [Fact]
    public void Ohne_Ordnerwaehler_bleiben_die_fuenf_Knoepfe_weg()
    {
        var cut = Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, Satz())
            .Add(x => x.Speichern, (_, _) => Task.FromResult(new SpeicherBefund(true, ""))));

        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));
        Assert.Single(cut.FindAll("input[type=text]"));       // das Feld bleibt
        Assert.False(cut.Find("input[type=text]").HasAttribute("readonly"));
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
        Reiter(cut, 3);

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
        Reiter(cut, 3);

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
    //  Speichern und Standardwerte
    // =====================================================================

    [Fact]
    public void Speichern_gibt_die_neun_Werte_weiter_und_schliesst()
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
        Assert.True(ergebnis);
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
    /// „Standardwerte" fragt mit Vorgabe „Nein" (A-1) und SPEICHERT NICHT — wörtlich
    /// wie der Vorläufer: „Die Standardwerte wurden geladen. Mit ‚Speichern' werden
    /// sie übernommen."
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
        Assert.Contains("Speichern", cut.Instance.Meldung);
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
}
