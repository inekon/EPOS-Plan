using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Lizenz;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Lizenz;

/// <summary>
/// <see cref="LizenzVerwaltungDialog"/> — Zeuge der Welle iU9-W15c.5.
///
/// <para><b>Was die Maske kann</b>, genau wie ihr Vorläufer: den Lizenzstatus zeigen,
/// mit Schlüssel und E-Mail aktivieren, eine <c>.lic</c>-Datei einlesen, eine
/// Testversion anfordern und das Gerät von der Lizenz lösen. Siebzehn
/// Steuerelemente hatte die Feldkarte; sechs davon waren Gruppenrahmen und
/// Beschriftungen.</para>
///
/// <para><b>Drei Zusagen tragen Gewicht.</b> (1) Die sechs Zustände werden auf DREI
/// Stufen abgebildet — grün, orange, rot; das ist die einzige Stelle des Bestands,
/// die Lizenzzustände sichtbar unterscheidet. (2) Nach einer erfolgreichen
/// Aktivierung ist das Schlüsselfeld LEER (Sicherheitsregel S-4; der Vorläufer ruft
/// <c>_schluessel.Clear()</c>). (3) „Nein" auf die Rückfrage vor dem Lösen tut
/// nichts — der Delegat wird nicht einmal gerufen.</para>
///
/// <para><b>Kein Netz.</b> Die vier Wege nach draußen sind Delegaten; hier zählen
/// Prüfstände, wie oft und womit sie gerufen wurden.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class LizenzVerwaltungDialogTests : BunitContext
{
    public LizenzVerwaltungDialogTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;

        // Der Infoknopf zieht IHilfeDienst; ohne Umgebung ist es die stille Fassung.
        Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());
    }

    private const string PORTAL = "https://epos-plan.de/lizenzportal/";

    private static LizenzGaben Lage(string zustand = "NICHTAKTIVIERT", bool hatToken = false,
                                    string statustext = "Nicht aktiviert — Testversion oder Lizenzschlüssel unter Administration → Lizenz.")
        => new(zustand, statustext,
               hatToken ? "Lizenz EPOS-2026-00001 · Musterfirma\r\nBenutzer: kunde@firma.de · Gerät: PC-01"
                        : "Keine Lizenz auf diesem Arbeitsplatz.",
               hatToken, PORTAL);

    private IRenderedComponent<LizenzVerwaltungDialog> Zeigen(
        LizenzGaben? lage = null,
        Func<string, string, Task<(bool Ok, string Meldung)>>? aktivieren = null,
        Func<Task<(string Schluessel, string Email, string Meldung)>>? licLesen = null,
        Func<string, Task<(bool Ok, string Meldung)>>? trial = null,
        Func<Task<(bool Ok, bool Netzfehler, string Meldung)>>? freigeben = null,
        Func<Task<LizenzGaben>>? auffrischen = null,
        string emailVorgabe = "")
    {
        return Render<LizenzVerwaltungDialog>(p =>
        {
            p.Add(x => x.Lage, lage ?? Lage())
             .Add(x => x.EmailVorgabe, emailVorgabe)
             .Add(x => x.EmailPruefen, (Func<string, bool>)EmailRegel)
             .Add(x => x.TitelText, "Lizenz — EPOS-Plan")
             .Add(x => x.GruppeStatus, "Lizenzstatus auf diesem Arbeitsplatz")
             .Add(x => x.GruppeAktivieren, "Aktivieren")
             .Add(x => x.GruppeAktionen, "Weitere Aktionen")
             .Add(x => x.LabelSchluessel, "Lizenzschlüssel:")
             .Add(x => x.LabelEmail, "E-Mail (Benutzer):")
             .Add(x => x.KnopfLic, "Lizenzdatei (.lic)…")
             .Add(x => x.KnopfAktivieren, "Jetzt aktivieren")
             .Add(x => x.KnopfTrial, "Testversion anfordern…")
             .Add(x => x.KnopfFreigeben, "Gerät von der Lizenz lösen")
             .Add(x => x.KnopfSchliessen, "Schließen")
             .Add(x => x.HinweisAktivierung, "Die Aktivierung benötigt einmalig eine Internetverbindung.")
             .Add(x => x.LinkPortal, "Lizenzportal öffnen (Benutzer und Geräte verwalten, Schlüssel neu erzeugen)")
             .Add(x => x.MsgEingabeFehlt, "Bitte Lizenzschlüssel und E-Mail-Adresse angeben.")
             .Add(x => x.MsgEmailUngueltig, "Die E-Mail-Adresse \"{0}\" ist ungültig — bitte prüfen (Beispiel: name@firma.de).")
             .Add(x => x.MsgAktiviert, "Die Lizenz wurde erfolgreich aktiviert.")
             .Add(x => x.MsgAktivierungFehler, "Die Aktivierung ist fehlgeschlagen.")
             .Add(x => x.MsgLicOhneSchluessel, "In der gewählten Datei wurde kein gültiger Lizenzschlüssel gefunden.")
             .Add(x => x.MsgTrialEmail, "Bitte oben eine gültige E-Mail-Adresse eintragen (Beispiel: name@firma.de).")
             .Add(x => x.MsgTrialOk, "Der Test-Lizenzschlüssel wurde per E-Mail versandt.")
             .Add(x => x.MsgTrialFehler, "Die Anforderung ist fehlgeschlagen.")
             .Add(x => x.MsgFreigebenFrage, "Dieses Gerät von der Lizenz lösen?")
             .Add(x => x.MsgServerNichtErreichbar, "Der Lizenzserver ist zurzeit nicht erreichbar — bitte später erneut versuchen.")
             .Add(x => x.StatusAktivierung, "Aktivierung läuft…")
             .Add(x => x.StatusTrial, "Testversion wird angefordert…")
             .Add(x => x.StatusFreigabe, "Gerät wird freigegeben…")
             .Add(x => x.HinweisLicGeladen, "Lizenzdatei geladen — bitte mit ‚Jetzt aktivieren' abschließen.");

            if (aktivieren is not null) p.Add(x => x.Aktivierenweg, aktivieren);
            if (licLesen is not null) p.Add(x => x.LicLesen, licLesen);
            if (trial is not null) p.Add(x => x.Trialweg, trial);
            if (freigeben is not null) p.Add(x => x.Freigebenweg, freigeben);
            if (auffrischen is not null) p.Add(x => x.Auffrischen, auffrischen);
        });
    }

    /// <summary>Dieselbe Regel wie <c>LizenzCtrl.EmailGueltig</c> — im Prüfstand nachgebildet.</summary>
    private static bool EmailRegel(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var adresse)) return false;
        return adresse.Host.Contains('.') && adresse.Address == email;
    }

    private static IElement Knopf(IRenderedComponent<LizenzVerwaltungDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Schluesselfeld(IRenderedComponent<LizenzVerwaltungDialog> cut)
        => cut.Find("input.epos-lizverw-grossschrift");

    private static IElement Emailfeld(IRenderedComponent<LizenzVerwaltungDialog> cut)
        => cut.Find("input[type=email]");

    // ==================================================================
    //  Feldbestand und Zustandsanzeige
    // ==================================================================

    /// <summary>
    /// Die drei Gruppen des Vorläufers stehen als drei Abschnitte da, samt den zwei
    /// Eingabefeldern, den fünf Knöpfen und dem Portalverweis.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_ist_vollstaendig()
    {
        var cut = Zeigen();

        Assert.Equal(3, cut.FindAll("section.epos-lizverw-gruppe").Count);
        Assert.Contains("Lizenzstatus auf diesem Arbeitsplatz", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Aktivieren", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Weitere Aktionen", cut.Markup, StringComparison.Ordinal);

        Assert.NotNull(Schluesselfeld(cut));
        Assert.NotNull(Emailfeld(cut));

        foreach (string text in new[] { "Lizenzdatei (.lic)…", "Jetzt aktivieren",
                                        "Testversion anfordern…", "Gerät von der Lizenz lösen",
                                        "Schließen" })
            Assert.NotNull(Knopf(cut, text));

        var portal = cut.Find("a[target=_blank]");
        Assert.Equal(PORTAL, portal.GetAttribute("href"));
    }

    /// <summary>
    /// <b>Die sechs Zustände, drei Stufen.</b> Bitgleich zu den drei Farben des
    /// Vorläufers (<c>Form_LizenzVerwaltung.cs:134-137</c>).
    /// </summary>
    [Theory]
    [InlineData("GUELTIG", "gut")]
    [InlineData("KULANZ", "warn")]
    [InlineData("NACHPRUEFUNG", "warn")]
    [InlineData("LESEMODUS", "schlecht")]
    [InlineData("UHRMANIPULIERT", "schlecht")]
    [InlineData("NICHTAKTIVIERT", "schlecht")]
    public void Jeder_Zustand_bekommt_seine_Stufe(string zustand, string stufe)
    {
        var cut = Zeigen(Lage(zustand, hatToken: zustand != "NICHTAKTIVIERT", statustext: "Ein Satz zum Zustand."));

        var status = cut.Find("p.epos-lizverw-status");
        Assert.Contains("epos-lizverw-status--" + stufe, status.ClassName, StringComparison.Ordinal);
        Assert.Equal("Ein Satz zum Zustand.", status.TextContent.Trim());
    }

    /// <summary>Der Statussatz wird gemeldet, nicht nur gezeigt (<c>role="status"</c>).</summary>
    [Fact]
    public void Der_Statussatz_wird_gemeldet()
    {
        var cut = Zeigen();
        Assert.Equal("status", cut.Find("p.epos-lizverw-status").GetAttribute("role"));
    }

    /// <summary>Die Detailzeile steht darunter — mit Token die vier Angaben, ohne der Ersatztext.</summary>
    [Fact]
    public void Die_Detailzeile_folgt_dem_Token()
    {
        Assert.Contains("Keine Lizenz auf diesem Arbeitsplatz.",
                        Zeigen().Find("p.epos-lizverw-detail").TextContent, StringComparison.Ordinal);

        Assert.Contains("Musterfirma",
                        Zeigen(Lage("GUELTIG", hatToken: true)).Find("p.epos-lizverw-detail").TextContent,
                        StringComparison.Ordinal);
    }

    // ==================================================================
    //  Die Sperrlogik der beiden Aktionsknöpfe
    // ==================================================================

    /// <summary>
    /// Bitgleich zu <c>StatusAnzeigen()</c>: „Gerät lösen" ist gesperrt, solange kein
    /// Token da ist; „Testversion" ist gesperrt, sobald eines da ist.
    /// </summary>
    [Fact]
    public void Ohne_Token_ist_nur_die_Testversion_bedienbar()
    {
        var cut = Zeigen(Lage(hatToken: false));

        Assert.False(Knopf(cut, "Testversion anfordern…").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Gerät von der Lizenz lösen").HasAttribute("disabled"));
    }

    /// <summary>Und umgekehrt.</summary>
    [Fact]
    public void Mit_Token_ist_nur_das_Loesen_bedienbar()
    {
        var cut = Zeigen(Lage("GUELTIG", hatToken: true));

        Assert.True(Knopf(cut, "Testversion anfordern…").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Gerät von der Lizenz lösen").HasAttribute("disabled"));
    }

    // ==================================================================
    //  Aktivieren
    // ==================================================================

    /// <summary>Leere Eingabe meldet — und ruft den Server nicht.</summary>
    [Fact]
    public void Eine_leere_Eingabe_meldet_und_ruft_nichts()
    {
        int rufe = 0;
        var cut = Zeigen(aktivieren: (s, m) => { rufe++; return Task.FromResult((true, "")); });

        Knopf(cut, "Jetzt aktivieren").Click();

        Assert.Equal(0, rufe);
        Assert.Contains("Bitte Lizenzschlüssel und E-Mail-Adresse angeben.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Eine ungültige Adresse meldet MIT der Adresse — der Vorläufer nennt sie
    /// ausdrücklich, damit der Anwender den Tippfehler sieht. (Der Schlüssel wird
    /// in KEINER Meldung wiederholt, Regel S-5.)
    /// </summary>
    [Fact]
    public void Eine_ungueltige_Adresse_wird_mit_Adresse_gemeldet()
    {
        int rufe = 0;
        var cut = Zeigen(aktivieren: (s, m) => { rufe++; return Task.FromResult((true, "")); });

        Schluesselfeld(cut).Input("EPOS-F-04795-LFKP-XYYU-ML");
        Emailfeld(cut).Input("name@firma");
        Knopf(cut, "Jetzt aktivieren").Click();

        Assert.Equal(0, rufe);
        Assert.Contains("Die E-Mail-Adresse \"name@firma\" ist ungültig", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("EPOS-F-04795", cut.Find("div.epos-warnbanner").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Regel S-4:</b> Nach einer erfolgreichen Aktivierung ist das Schlüsselfeld
    /// leer, und der Schlüssel geht GROSSGESCHRIEBEN hinaus
    /// (<c>CharacterCasing = Upper</c> des Vorläufers).
    /// </summary>
    [Fact]
    public void Nach_dem_Erfolg_ist_das_Schluesselfeld_leer()
    {
        string? gesehen = null;
        var cut = Zeigen(
            aktivieren: (s, m) => { gesehen = s; return Task.FromResult((true, "")); },
            auffrischen: () => Task.FromResult(Lage("GUELTIG", hatToken: true, statustext: "Firmenlizenz · gültig bis 31.12.2026")));

        Schluesselfeld(cut).Input("epos-f-04795-lfkp-xyyu-ml");
        Emailfeld(cut).Input("kunde@firma.de");
        Knopf(cut, "Jetzt aktivieren").Click();

        cut.WaitForAssertion(() => Assert.Contains("Die Lizenz wurde erfolgreich aktiviert.", cut.Markup, StringComparison.Ordinal));

        Assert.Equal("EPOS-F-04795-LFKP-XYYU-ML", gesehen);
        Assert.Equal("", Schluesselfeld(cut).GetAttribute("value"));
        Assert.Contains("Firmenlizenz · gültig bis 31.12.2026", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ein Fehlschlag zeigt die Meldung des Servers; ohne Meldung den eigenen Text —
    /// bitgleich zu <c>antwort.Meldung ?? LIZ_MSG_AKTIVIERUNG_FEHLER</c>.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_zeigt_die_Servermeldung_sonst_den_eigenen_Text()
    {
        var mitMeldung = Zeigen(aktivieren: (s, m) => Task.FromResult((false, "Lizenzschlüssel unbekannt.")));
        Schluesselfeld(mitMeldung).Input("ABC");
        Emailfeld(mitMeldung).Input("kunde@firma.de");
        Knopf(mitMeldung, "Jetzt aktivieren").Click();
        mitMeldung.WaitForAssertion(() => Assert.Contains("Lizenzschlüssel unbekannt.", mitMeldung.Markup, StringComparison.Ordinal));

        var ohne = Zeigen(aktivieren: (s, m) => Task.FromResult((false, "")));
        Schluesselfeld(ohne).Input("ABC");
        Emailfeld(ohne).Input("kunde@firma.de");
        Knopf(ohne, "Jetzt aktivieren").Click();
        ohne.WaitForAssertion(() => Assert.Contains("Die Aktivierung ist fehlgeschlagen.", ohne.Markup, StringComparison.Ordinal));
    }

    // ==================================================================
    //  Die .lic-Datei
    // ==================================================================

    /// <summary>Eine gelesene Datei füllt beide Felder und meldet den Hinweis.</summary>
    [Fact]
    public void Eine_gelesene_Lizenzdatei_fuellt_beide_Felder()
    {
        var cut = Zeigen(licLesen: () => Task.FromResult(("EPOS-F-1", "kunde@firma.de", "")));

        Knopf(cut, "Lizenzdatei (.lic)…").Click();

        cut.WaitForAssertion(() => Assert.Equal("EPOS-F-1", Schluesselfeld(cut).GetAttribute("value")));
        Assert.Equal("kunde@firma.de", Emailfeld(cut).GetAttribute("value"));
        Assert.Contains("Lizenzdatei geladen", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Eine Datei ohne Schlüssel meldet — und lässt die Felder unberührt.</summary>
    [Fact]
    public void Eine_Lizenzdatei_ohne_Schluessel_meldet()
    {
        var cut = Zeigen(licLesen: () => Task.FromResult(("", "",
            "In der gewählten Datei wurde kein gültiger Lizenzschlüssel gefunden.")));

        Knopf(cut, "Lizenzdatei (.lic)…").Click();

        cut.WaitForAssertion(() => Assert.Contains("kein gültiger Lizenzschlüssel gefunden", cut.Markup, StringComparison.Ordinal));
        Assert.Equal("", Schluesselfeld(cut).GetAttribute("value"));
    }

    /// <summary>
    /// Ein abgebrochener Wähler (kein Schlüssel, keine Meldung) lässt alles, wie es
    /// war — bitgleich zu <c>if (dialog.ShowDialog(this) != DialogResult.OK) return;</c>.
    /// </summary>
    [Fact]
    public void Ein_abgebrochener_Waehler_aendert_nichts()
    {
        var cut = Zeigen(licLesen: () => Task.FromResult(("", "", "")));

        Schluesselfeld(cut).Input("VORHER");
        Knopf(cut, "Lizenzdatei (.lic)…").Click();

        cut.WaitForAssertion(() => Assert.Equal("VORHER", Schluesselfeld(cut).GetAttribute("value")));
        Assert.Empty(cut.FindAll("div.epos-warnbanner"));
    }

    // ==================================================================
    //  Testversion
    // ==================================================================

    /// <summary>Ohne gültige Adresse wird die Testversion gar nicht erst angefordert.</summary>
    [Fact]
    public void Die_Testversion_verlangt_zuerst_eine_Adresse()
    {
        int rufe = 0;
        var cut = Zeigen(trial: m => { rufe++; return Task.FromResult((true, "")); });

        Knopf(cut, "Testversion anfordern…").Click();

        Assert.Equal(0, rufe);
        Assert.Contains("Bitte oben eine gültige E-Mail-Adresse eintragen", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Mit Adresse läuft sie und meldet den Erfolg.</summary>
    [Fact]
    public void Die_Testversion_meldet_den_Versand()
    {
        string? gesehen = null;
        var cut = Zeigen(trial: m => { gesehen = m; return Task.FromResult((true, "")); });

        Emailfeld(cut).Input("kunde@firma.de");
        Knopf(cut, "Testversion anfordern…").Click();

        cut.WaitForAssertion(() => Assert.Contains("wurde per E-Mail versandt", cut.Markup, StringComparison.Ordinal));
        Assert.Equal("kunde@firma.de", gesehen);
    }

    // ==================================================================
    //  Gerät lösen
    // ==================================================================

    /// <summary>Der Knopf öffnet zuerst die Rückfrage — gelöst wird noch nichts.</summary>
    [Fact]
    public void Vor_dem_Loesen_kommt_die_Rueckfrage()
    {
        int rufe = 0;
        var cut = Zeigen(Lage("GUELTIG", hatToken: true),
                         freigeben: () => { rufe++; return Task.FromResult((true, false, "")); });

        Knopf(cut, "Gerät von der Lizenz lösen").Click();

        Assert.Contains("Dieses Gerät von der Lizenz lösen?", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, rufe);
    }

    /// <summary><b>„Nein" tut nichts</b> — der Delegat wird nicht gerufen.</summary>
    [Fact]
    public void Ein_Nein_auf_die_Rueckfrage_tut_nichts()
    {
        int rufe = 0;
        var cut = Zeigen(Lage("GUELTIG", hatToken: true),
                         freigeben: () => { rufe++; return Task.FromResult((true, false, "")); });

        Knopf(cut, "Gerät von der Lizenz lösen").Click();
        Knopf(cut, "Nein").Click();

        Assert.Equal(0, rufe);
        Assert.DoesNotContain("Dieses Gerät von der Lizenz lösen?", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>„Ja" löst und frischt danach auf.</summary>
    [Fact]
    public void Ein_Ja_loest_und_frischt_auf()
    {
        int rufe = 0;
        var cut = Zeigen(Lage("GUELTIG", hatToken: true),
                         freigeben: () => { rufe++; return Task.FromResult((true, false, "")); },
                         auffrischen: () => Task.FromResult(Lage()));

        Knopf(cut, "Gerät von der Lizenz lösen").Click();
        Knopf(cut, "Ja").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, rufe));
        cut.WaitForAssertion(() => Assert.True(Knopf(cut, "Gerät von der Lizenz lösen").HasAttribute("disabled")));
    }

    /// <summary>
    /// Ein reiner NETZFEHLER meldet „Server nicht erreichbar" — und ist ausdrücklich
    /// kein Ablehnungsgrund; das Token bleibt liegen.
    /// </summary>
    [Fact]
    public void Ein_Netzfehler_beim_Loesen_wird_gemeldet()
    {
        var cut = Zeigen(Lage("GUELTIG", hatToken: true),
                         freigeben: () => Task.FromResult((false, true, "")),
                         auffrischen: () => Task.FromResult(Lage("GUELTIG", hatToken: true)));

        Knopf(cut, "Gerät von der Lizenz lösen").Click();
        Knopf(cut, "Ja").Click();

        cut.WaitForAssertion(() => Assert.Contains("Der Lizenzserver ist zurzeit nicht erreichbar", cut.Markup, StringComparison.Ordinal));
    }

    // ==================================================================
    //  Schließen und Vorbelegung
    // ==================================================================

    /// <summary>„Schließen" meldet sich — genau einmal.</summary>
    [Fact]
    public void Schliessen_meldet_sich()
    {
        int rufe = 0;
        var cut = Render<LizenzVerwaltungDialog>(p =>
        {
            p.Add(x => x.Lage, Lage())
             .Add(x => x.KnopfSchliessen, "Schließen")
             .Add(x => x.Geschlossen, EventCallback.Factory.Create(new object(), () => rufe++));
        });

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Schließen").Click();

        Assert.Equal(1, rufe);
    }

    /// <summary>
    /// Die E-Mail wird aus dem Token vorbelegt; der SCHLÜSSEL nie (Regel S-4 — er ist
    /// wie ein Passwort zu behandeln).
    /// </summary>
    [Fact]
    public void Die_Adresse_wird_vorbelegt_der_Schluessel_nicht()
    {
        var cut = Zeigen(Lage("GUELTIG", hatToken: true), emailVorgabe: "kunde@firma.de");

        Assert.Equal("kunde@firma.de", Emailfeld(cut).GetAttribute("value"));
        Assert.Equal("", Schluesselfeld(cut).GetAttribute("value"));
    }

    /// <summary>
    /// Ohne Delegat passiert nichts — die Komponente kennt den Lizenzkern nicht und
    /// kann ohne Hülle nichts auslösen (Regel S-2).
    /// </summary>
    [Fact]
    public void Ohne_Delegaten_passiert_nichts()
    {
        var cut = Zeigen();

        Schluesselfeld(cut).Input("EPOS-F-1");
        Emailfeld(cut).Input("kunde@firma.de");
        Knopf(cut, "Jetzt aktivieren").Click();
        Knopf(cut, "Lizenzdatei (.lic)…").Click();

        Assert.Empty(cut.FindAll("div.epos-warnbanner"));
    }

    /// <summary>Der Infoknopf trägt den Schlüssel aus <c>help_mapping.txt</c>.</summary>
    [Fact]
    public void Der_Infoknopf_traegt_seinen_Schluessel()
    {
        var cut = Zeigen();
        Assert.NotNull(cut.FindComponent<EPOS.UI.Bausteine.InfoKnopf>());
        Assert.Equal("Form_LizenzVerwaltung.btn_Help",
                     cut.FindComponent<EPOS.UI.Bausteine.InfoKnopf>().Instance.Schluessel);
    }
}
