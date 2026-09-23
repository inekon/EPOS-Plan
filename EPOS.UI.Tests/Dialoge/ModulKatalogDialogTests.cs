using System.Globalization;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Modulkatalog der Erzeuger (iU9-W14a.3) — EINE Komponente, ZWEI Ausprägungen.
/// Soll sind die Feldkarten von <c>Form_AdminStromspeicher</c> (20 Zeilen plus die
/// SECHS zur Laufzeit gebauten AP3-Felder, Risiko R-W14-10) und <c>Form_AdminPV</c>
/// (29 Zeilen).
///
/// <para><b>Der Feldkartenabgleich läuft je AUSPRÄGUNG</b>, nicht je Komponente.</para>
///
/// <para>Die Sprache pinnt die Klasse selbst (Regel seit iU9-W8).</para>
/// </summary>
public class ModulKatalogDialogTests : EposBunitContext
{

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander. Dass das Register wirklich teilt, prüft
    /// <c>KatalogfilterstandTests</c>.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();
    public ModulKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Das Profil in DEUTSCH — so, wie die Hülle es liefert.</summary>
    private static ModulKatalogProfil Profil(ModulKatalogArt art) =>
        ModulKatalogProfil.Finde(art, s => WindowsFormsApplication1.MyResource.Resource
                                               .ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<Katalogfilterzeile> Zeilen() => new[]
    {
        new Katalogfilterzeile(1, "Modul A")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul A")
            .MitText(Katalogfilterprofil.SpHersteller, "Ablytek"),
        new Katalogfilterzeile(2, "Modul B")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul B")
            .MitText(Katalogfilterprofil.SpHersteller, "Jinkosolar")
    };

    /// <summary>Ein vollständiger Satz nach dem Profil.</summary>
    private static IReadOnlyList<ModulFeldwert> Felder(ModulKatalogArt art, string name)
    {
        var liste = new List<ModulFeldwert>();
        foreach (var feld in Profil(art).Felder)
        {
            string wert = feld.Schluessel == ModulKatalogProfil.FeldBezeichner ? name
                        : feld.Art == BrowserFeldArt.Zahl ? "12,50"
                        : feld.Art == BrowserFeldArt.Ganzzahl ? "6000"
                        : "Wert";

            liste.Add(new ModulFeldwert
            {
                Schluessel = feld.Schluessel,
                Bezeichnung = feld.Bezeichnung,
                Einheit = feld.Einheit,
                Art = feld.Art,
                LeerErlaubt = feld.LeerErlaubt,
                Gesperrt = feld.Gesperrt,
                Gruppe = feld.Gruppe,
                // Die OPTIONEN eines Auswahlfeldes gehören dazu - so reicht sie die
                // Hülle herein (ModulKatalogHuelle), und ohne sie hätte die
                // Zelltechnologie weder eine Klappliste noch Wahleinträge für den
                // Assistenten.
                Optionen = feld.Optionen,
                Wert = wert
            });
        }
        return liste;
    }

    private IRenderedComponent<ModulKatalogDialog> Aufbauen(
        ModulKatalogArt art = ModulKatalogArt.Stromspeicher,
        ModulKatalogWege? wege = null,
        Action<ModulErgebnis>? geschlossen = null)
    {
        var standard = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = name => Felder(art, name),
            Speichern = (f, _, __) => new KatalogSpeicherErgebnis(
                true, "Datensatz gespeichert",
                f.First(x => x.Schluessel == ModulKatalogProfil.FeldBezeichner).Wert),
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
        };

        return Render<ModulKatalogDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.ProfilVorgabe, Profil(art))
            .Add(x => x.Wege, wege ?? standard)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    // =================================================================================
    // Feldbestand je Ausprägung
    // =================================================================================

    [Theory]
    [InlineData(ModulKatalogArt.Stromspeicher, "Administration Stromspeicher", 14)]
    [InlineData(ModulKatalogArt.Photovoltaik, "Administration Photovoltaik Module", 14)]
    public void Jede_Auspraegung_zeigt_ihren_Titel_und_ihre_Felder(
        ModulKatalogArt art, string titel, int felder)
    {
        var cut = Aufbauen(art);

        Assert.Equal(titel, cut.Find(".epos-dialog-titel").TextContent);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (var feld in Profil(art).Felder)
            Assert.Contains(feld.Bezeichnung, texte);

        // Merge 5: die Photovoltaik hat 15 Felder - 14 Eingabefelder und die Zelltechnologie
        // als Auswahlfeld (select), das hier gesondert gezaehlt wird.
        int gezeichnet = cut.FindAll(".epos-feld input").Count
                       + cut.FindAll(".epos-feld textarea").Count;
        Assert.Equal(felder, gezeichnet);
        Assert.Equal(art == ModulKatalogArt.Photovoltaik ? 1 : 0, cut.FindAll(".epos-feld select").Count);
    }

    /// <summary>
    /// <b>Risiko R-W14-10:</b> Die SECHS AP3-Gerätefelder des Stromspeichers baute der
    /// Vorläufer zur Laufzeit auf; die Feldkarte sah sie nicht. Hier stehen sie als
    /// zweite Feldgruppe — die Photovoltaik hat keine.
    /// </summary>
    [Fact]
    public void Der_Stromspeicher_hat_die_zweite_Feldgruppe_der_Geraetetechnik()
    {
        var sp = Aufbauen(ModulKatalogArt.Stromspeicher);
        var titel = sp.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        Assert.Equal(3, titel.Count);   // Liste + Bestand + Gerätetechnik
        Assert.Contains(Profil(ModulKatalogArt.Stromspeicher).GruppeZwei, titel);

        var texte = sp.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (var feld in Profil(ModulKatalogArt.Stromspeicher).Felder.Where(f => f.Gruppe == 1))
            Assert.Contains(feld.Bezeichnung, texte);

        var pv = Aufbauen(ModulKatalogArt.Photovoltaik);
        Assert.Equal(2, pv.FindAll(".epos-gruppenkopf-titel").Count);
    }

    /// <summary>
    /// Die drei BERICHTIGTEN Einheiten des Stromspeichers (Abnahmebefund 1, AP0-Entscheid
    /// 16.08.2026): kWh an der Kapazität, €/kWh an den Modulkosten. Der Vorläufer
    /// schrieb sie zur Laufzeit über die Designer-Werte (Befund W14-B40).
    /// </summary>
    [Fact]
    public void Die_berichtigten_Einheiten_stehen_gleich_richtig_da()
    {
        var cut = Aufbauen(ModulKatalogArt.Stromspeicher);
        var einheiten = cut.FindAll(".epos-einheit").Select(e => e.TextContent).ToList();

        Assert.Contains("kWh", einheiten);
        Assert.Contains("€/kWh", einheiten);

        // Der Designer trug an der Kapazitaet "kW" und an den Modulkosten ein nacktes
        // "€"; beides berichtigte der Vorlaeufer erst zur Laufzeit. Jetzt steht es
        // gleich richtig im Profil - das "€" der AP3-Investition bleibt davon
        // unberuehrt.
        var profil = Profil(ModulKatalogArt.Stromspeicher);
        Assert.Equal("kWh", profil.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldEnergie).Einheit);
        Assert.Equal("€/kWh", profil.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldModulkosten).Einheit);
        Assert.Equal("€", profil.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldInvestitionFix).Einheit);
    }

    [Fact]
    public void Der_Bezeichner_ist_in_beiden_Auspraegungen_gesperrt()
    {
        foreach (var art in ModulKatalogProfil.AlleArten)
        {
            var cut = Aufbauen(art);
            Assert.True(cut.FindAll("input[type=text]")[0].HasAttribute("readonly"),
                        art + ": der Bezeichner ist nicht gesperrt.");
        }
    }

    // =================================================================================
    // Liste und Auswahl
    // =================================================================================

    [Fact]
    public void Beim_Oeffnen_steht_die_erste_Zeile_und_ihr_Feldsatz()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.Instance.Zeilen.Count);
        Assert.Equal("Modul A", cut.Instance.Gewaehlt);
        Assert.Equal("Modul A", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
    }

    [Fact]
    public void Eine_andere_Zeile_zieht_ihren_Feldsatz_nach()
    {
        var cut = Aufbauen();

        Zeilenklick.Zeile(cut, 1);

        Assert.Equal("Modul B", cut.Instance.Gewaehlt);
        Assert.Equal("Modul B", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
    }

    // =================================================================================
    // Neu — die Vorbelegungen
    // =================================================================================

    [Fact]
    public void Neu_fragt_erst_den_Namen()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();

        Assert.True(cut.Instance.Namensfrage);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Die dreizehn Vorbelegungen des Stromspeichers — darunter die Zelltechnologie aus
    /// <c>DbWerte.SP_TYP_LITHIUM_IONEN</c> und die ZWEI fachlichen Vorgaben
    /// <c>eta_RT = 0,90</c> und <c>c_ver = 0,025</c> (Fachkonzept 5.2/5.4).
    /// </summary>
    [Fact]
    public void Neu_belegt_den_Stromspeicher_mit_den_zwei_fachlichen_Vorgaben()
    {
        var cut = Aufbauen(ModulKatalogArt.Stromspeicher);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Neuer Speicher");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        Assert.True(cut.Instance.IstNeu);

        var texte = cut.FindAll("input[type=text]").Select(e => e.GetAttribute("value")).ToList();
        Assert.Contains("Neuer Speicher", texte);
        Assert.Contains("Lithium-Ionen", texte);
        Assert.Contains("0,9", texte);      // eta_RT
        Assert.Contains("0,025", texte);    // c_ver
    }

    /// <summary>
    /// Die Vorbelegungen der Photovoltaik: zwei leere Textfelder und zehn Nullen
    /// (<c>Form_AdminPV.btn_Neu_Click</c> Z. 180-192).
    /// </summary>
    [Fact]
    public void Neu_belegt_die_Photovoltaik_mit_zwei_Leerfeldern_und_elf_Nullen()
    {
        var cut = Aufbauen(ModulKatalogArt.Photovoltaik);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Neues Modul");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        var werte = cut.FindAll(".epos-feld input").Select(e => e.GetAttribute("value") ?? "").ToList();
        Assert.Contains("Neues Modul", werte);
        Assert.Equal(11, werte.Count(w => w == "0"));     // Merge 5: dazu die NOCT-Zelltemperatur
    }

    [Fact]
    public void Nach_Neu_schreibt_Speichern_ein_ANLEGEN()
    {
        bool? neu = null;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (f, n, _) => { neu = n; return new KatalogSpeicherErgebnis(true, "ok", "X"); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Neuer Speicher");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.True(neu);
    }

    // =================================================================================
    // Die leerErlaubt-Regel (BITGLEICH)
    // =================================================================================

    /// <summary>
    /// Beim Stromspeicher darf KEINES der fünf Bestandsfelder leer sein
    /// (<c>Form_AdminStromspeicher.cs:111-115</c>, alle ohne <c>leerErlaubt</c>).
    /// </summary>
    [Fact]
    public void Der_Stromspeicher_laesst_kein_Bestandsfeld_leer()
    {
        bool geschrieben = false;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (_, __, ___) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "ok", "X"); }
        };
        var cut = Aufbauen(ModulKatalogArt.Stromspeicher, wege);

        // „Energie" ist das erste Zahlenfeld der Bestandsgruppe.
        cut.FindAll("input[inputmode=decimal]")[0].Input("");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.False(geschrieben);
        Assert.Contains("Energie", cut.Instance.Meldung);
    }

    /// <summary>
    /// Die SECHS AP3-Felder dürfen leer bleiben und heißen dann „nicht gepflegt"
    /// (<c>Form_AdminStromspeicher.cs:117-127</c>, alle mit <c>leerErlaubt: true</c>).
    /// </summary>
    [Fact]
    public void Die_AP3_Felder_duerfen_leer_bleiben()
    {
        bool geschrieben = false;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (_, __, ___) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "ok", "X"); }
        };
        var cut = Aufbauen(ModulKatalogArt.Stromspeicher, wege);

        // Das Ganzzahlfeld gibt es nur einmal - die zugesicherten Zyklen (AP3).
        cut.FindAll("input[inputmode=numeric]")[0].Input("");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.True(geschrieben);
    }

    /// <summary>
    /// Bei der Photovoltaik ist es umgekehrt: NEUN von zehn Zahlfeldern dürfen leer
    /// sein, allein die Nennleistung nicht („ein leeres Feld meldete bisher schon beim
    /// Verlassen", <c>Form_AdminPV.cs:72-75</c>).
    /// </summary>
    [Fact]
    public void Die_Photovoltaik_laesst_nur_die_Nennleistung_nicht_leer()
    {
        var geschrieben = new List<bool>();
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Photovoltaik, n),
            Speichern = (_, __, ___) => { geschrieben.Add(true); return new KatalogSpeicherErgebnis(true, "ok", "X"); }
        };
        var cut = Aufbauen(ModulKatalogArt.Photovoltaik, wege);

        // Feld 0 der Zahlenreihe ist die Nennleistung (Profilreihenfolge).
        cut.FindAll("input[inputmode=decimal]")[0].Input("");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();
        Assert.Empty(geschrieben);

        // Feld 1 ist der Wirkungsgrad - er darf leer bleiben.
        cut.FindAll("input[inputmode=decimal]")[0].Input("300");
        cut.FindAll("input[inputmode=decimal]")[1].Input("");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();
        Assert.Single(geschrieben);
    }

    [Fact]
    public void Eine_ungueltige_Zahl_haelt_den_Speicherweg_auf()
    {
        bool geschrieben = false;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (_, __, ___) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "ok", "X"); }
        };
        var cut = Aufbauen(ModulKatalogArt.Stromspeicher, wege);

        cut.FindAll("input[inputmode=decimal]")[0].Input("keine Zahl");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.False(geschrieben);
        Assert.False(string.IsNullOrEmpty(cut.Instance.Meldung));
    }

    // =================================================================================
    // Speichern und Löschen
    // =================================================================================

    [Fact]
    public void Speichern_reicht_den_Feldsatz_und_den_Schluessel_weiter()
    {
        IReadOnlyList<ModulFeldwert>? gesehen = null;
        string? schluessel = null;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (f, _, s) => { gesehen = f; schluessel = s; return new KatalogSpeicherErgebnis(true, "ok", "Modul A"); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal("Modul A", schluessel);
        // VIERZEHN seit W14a-E-10-Q7 (Migrationsschritt 68): der Stromspeicher
        // fuehrt jetzt auch das Feld "Firma".
        Assert.Equal(14, gesehen!.Count);
    }

    [Fact]
    public void Ein_Fehlschlag_beim_Speichern_nennt_den_Grund()
    {
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (_, __, ___) => new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", "")
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Angleichung E-3 (Befund W14-B35).</b> Beide Ausprägungen fragen vor dem
    /// Löschen zurück; die Photovoltaik war die EINZIGE der elf Masken, die
    /// kommentarlos löschte.
    /// </summary>
    [Theory]
    [InlineData(ModulKatalogArt.Stromspeicher)]
    [InlineData(ModulKatalogArt.Photovoltaik)]
    public void Loeschen_fragt_in_beiden_Auspraegungen_zurueck(ModulKatalogArt art)
    {
        string? geloescht = null;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(art, n),
            Loeschen = n => { geloescht = n; return new KatalogSpeicherErgebnis(true, "", n); }
        };
        var cut = Aufbauen(art, wege);

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();

        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains("Modul A", cut.Find(".epos-rueckfrage").TextContent);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal("Modul A", geloescht);
    }

    [Fact]
    public void Nein_in_der_Rueckfrage_loescht_nicht()
    {
        bool gerufen = false;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Loeschen = n => { gerufen = true; return new KatalogSpeicherErgebnis(true, "", n); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(gerufen);
    }

    /// <summary>
    /// <b>Befund W14-B42.</b> Der Vorläufer deutete JEDE Ausnahme beim Löschen als
    /// „Es besteht eine Projektzuordnung!". Jetzt kommt der wirkliche Grund durch.
    /// </summary>
    [Fact]
    public void Ein_abgelehntes_Loeschen_nennt_den_wirklichen_Grund()
    {
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Loeschen = _ => new KatalogSpeicherErgebnis(false, "Der Satz ist schreibgeschützt.", "")
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal("Der Satz ist schreibgeschützt.", cut.Instance.Meldung);
    }

    [Fact]
    public void Loeschen_ohne_Auswahl_meldet()
    {
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Array.Empty<Katalogfilterzeile>,
            Detail = _ => null
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();

        Assert.False(cut.Instance.Loeschfrage);
        Assert.Equal(Profil(ModulKatalogArt.Stromspeicher).MeldungOhneAuswahl,
                     cut.Instance.Meldung);
    }

    // =================================================================================
    // Beenden und Esc
    // =================================================================================

    [Fact]
    public void Beenden_meldet_bestaetigt_und_den_gewaehlten_Eintrag()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.FindAll(".epos-leiste .epos-knopf")[4].Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
        Assert.Equal("Modul A", ergebnis.Bezeichner);
    }

    /// <summary>Esc wirkt wie „Beenden" — EIN Schlussweg (Konzept Administrationsdialoge, V15).</summary>
    [Fact]
    public void Esc_schliesst_wie_Beenden()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
    }

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026) — und es tut,
    /// was „Beenden" tut (V15).
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Beenden()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog-zu").Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
    }

    /// <summary>
    /// <b>„Beenden" statt „OK"</b> — und geänderte Felder halten es auf: „Speichern oder
    /// Verwerfen" im Warnband, der Dialog bleibt offen (V11, Konzept 3.3).
    /// </summary>
    [Fact]
    public void Beenden_haelt_bei_geaenderten_Feldern_an()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        Assert.Equal("Beenden", cut.FindAll(".epos-leiste .epos-knopf").Last().TextContent.Trim());

        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();

        Assert.Null(ergebnis);
        Assert.Contains("Verwerfen", cut.Instance.Meldung);

        // Ein Zeilenwechsel haelt ebenso an.
        Zeilenklick.Zeile(cut, 1);
        Assert.Equal("Modul A", cut.Instance.Gewaehlt);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();       // Verwerfen
        cut.FindAll(".epos-leiste .epos-knopf").Last().Click();   // Beenden
        Assert.NotNull(ergebnis);
    }

    // =================================================================================
    //  Auslieferungssätze (Entscheid AD-Q11) und das Schloss (V10)
    // =================================================================================

    private static IReadOnlyList<Katalogfilterzeile> ZeilenMitAuslieferung() => new[]
    {
        new Katalogfilterzeile(1, "Modul A") { Geschuetzt = true }
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul A")
            .MitText(Katalogfilterprofil.SpHersteller, "Ablytek"),
        new Katalogfilterzeile(2, "Modul B")
            .MitText(Katalogfilterprofil.SpBezeichner, "Modul B")
            .MitText(Katalogfilterprofil.SpHersteller, "Jinkosolar")
    };

    /// <summary>
    /// <b>Der Modulkatalog wertet ReadOnly aus</b> (AD-Q11, V13): Ein Auslieferungssatz
    /// trägt das Schloss, seine Felder sind nur lesbar, „Speichern" ist weich gesperrt
    /// und nennt den Weg über „Duplizieren…" — geschrieben wird nichts.
    /// </summary>
    [Fact]
    public void Ein_Auslieferungssatz_ist_nur_lesbar_und_sperrt_Speichern_weich()
    {
        bool geschrieben = false;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = ZeilenMitAuslieferung,
            Detail = name => Felder(ModulKatalogArt.Stromspeicher, name),
            Speichern = (f, _, __) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "", "Modul A"); },
            Duplizieren = (id, name) => new KatalogSpeicherErgebnis(true, "", name)
        };
        var cut = Aufbauen(wege: wege);

        Assert.True(cut.Instance.Auslieferungssatz);
        Assert.NotNull(cut.FindAll(".epos-katalogliste tbody tr")[0].QuerySelector(".epos-schloss"));
        Assert.Empty(cut.FindAll("input[inputmode=decimal]"));

        var speichern = cut.FindAll(".epos-leiste .epos-knopf")[0];
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        speichern.Click();

        Assert.False(geschrieben);
        Assert.Contains("Duplizieren", cut.Instance.Meldung);

        // Der eigene Satz bleibt bearbeitbar.
        Zeilenklick.Zeile(cut, 1);
        Assert.False(cut.Instance.Auslieferungssatz);
        Assert.NotEmpty(cut.FindAll("input[inputmode=decimal]"));
    }

    /// <summary>
    /// <b>„Duplizieren…"</b> fragt den Namen (vorbelegt „Name (Kopie)"), ruft den Weg mit
    /// der ID der gewählten Zeile und wählt danach die Kopie.
    /// </summary>
    [Fact]
    public void Duplizieren_legt_die_Kopie_an_und_waehlt_sie()
    {
        var katalog = ZeilenMitAuslieferung().ToList();
        (int, string)? gerufen = null;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = () => katalog,
            Detail = name => Felder(ModulKatalogArt.Stromspeicher, name),
            Duplizieren = (id, name) =>
            {
                gerufen = (id, name);
                katalog.Add(new Katalogfilterzeile(3, name).MitText(Katalogfilterprofil.SpBezeichner, name));
                return new KatalogSpeicherErgebnis(true, "", name);
            }
        };
        var cut = Aufbauen(wege: wege);

        var texte = cut.FindAll(".epos-leiste .epos-knopf").Select(k => k.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu...", "Duplizieren...", "Löschen", "Beenden" }, texte);

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();
        Assert.Equal("Modul A (Kopie)", cut.Find(".epos-ueberlagerung input[type=text]").GetAttribute("value"));
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal((1, "Modul A (Kopie)"), gerufen);
        Assert.Equal("Modul A (Kopie)", cut.Instance.Gewaehlt);
        Assert.False(cut.Instance.Auslieferungssatz);
        Assert.Contains("dupliziert", cut.Instance.Status);
    }

    [Fact]
    public void Esc_bei_offener_Rueckfrage_schliesst_den_Dialog_nicht()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    // =================================================================================
    //  Der Hilfe-Assistent (Welle KI-F5)
    // =================================================================================

    /// <summary>
    /// <b>Jede der drei Ausprägungen meldet ihren EIGENEN Katalogschlüssel an</b> —
    /// eine Komponente, drei Masken.
    /// </summary>
    [Theory]
    [InlineData(ModulKatalogArt.Photovoltaik, KiMaskennamen.PV_MODULKATALOG)]
    [InlineData(ModulKatalogArt.Stromspeicher, KiMaskennamen.STROMSPEICHER_KATALOG)]
    [InlineData(ModulKatalogArt.Wechselrichter, KiMaskennamen.WECHSELRICHTER_KATALOG)]
    public void Jede_Auspraegung_meldet_ihren_eigenen_Katalogschluessel_an(
        ModulKatalogArt art, string maske)
    {
        Aufbauen(art);

        Assert.True(KiMaskenbruecke.IstAngemeldet(maske));
        Assert.Equal(maske, KiMaskenbruecke.AktiveMaske());
    }

    /// <summary>
    /// <b>Der ZEUGE des PV-Modulkatalogs.</b> Gelesen und gesetzt wird über die
    /// Sichtklasse auf den lebenden Feldsatz; die Zelltechnologie ist ein WAHLFELD,
    /// dessen Einträge aus dem Profil kommen (KI‑D‑Q6).
    /// </summary>
    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Felder_des_PV_Modulkatalogs()
    {
        var cut = Aufbauen(ModulKatalogArt.Photovoltaik);

        // Der Bezeichner ist gesperrt — er ist der Schlüssel des UPDATE.
        Assert.False(KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_MODULKATALOG, "name").Setzbar);

        KiFeldzugang pmax =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_MODULKATALOG, "pmax");
        Assert.NotNull(pmax);
        Assert.True(pmax.Setzbar);
        pmax.Setzen(425.0);
        cut.Render();
        Assert.Equal(425.0, Convert.ToDouble(pmax.Lesen(), CultureInfo.InvariantCulture));

        // ... und der Wert steht im FELDSATZ der Maske, nicht nur in der Sicht.
        Assert.Equal("425", cut.Instance.Felder
                                .First(f => f.Schluessel == ModulKatalogProfil.FeldLeistung).Wert);

        KiFeldzugang technologie =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.PV_MODULKATALOG, "technologie");
        Assert.NotEmpty(technologie.Wahleintraege());
    }

    /// <summary>
    /// <b>Der ZEUGE des Wechselrichterkatalogs.</b> Die HERKUNFT ist Auskunft des
    /// Imports und deshalb nur lesbar, die Zahl der MPP-Tracker eine Ganzzahl.
    /// </summary>
    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Felder_des_Wechselrichterkatalogs()
    {
        var cut = Aufbauen(ModulKatalogArt.Wechselrichter);

        Assert.False(KiMaskenbruecke
                     .Feldzugang(KiMaskennamen.WECHSELRICHTER_KATALOG, "herkunft").Setzbar);

        KiFeldzugang mppt =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.WECHSELRICHTER_KATALOG, "anzahl_mppt");
        mppt.Setzen(3);
        cut.Render();
        Assert.Equal(3, Convert.ToInt32(mppt.Lesen(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// <b>Der Speicherweg des Modulkatalogs</b> schreibt denselben Feldsatz zurück,
    /// den der Speicherknopf schreibt — und lehnt benannt ab, wenn eine Pflichtzahl
    /// fehlt.
    /// </summary>
    [Fact]
    public void Der_Speicherweg_schreibt_den_Feldsatz_und_prueft_die_Pflichtfelder()
    {
        IReadOnlyList<ModulFeldwert>? geschrieben = null;
        var wege = new ModulKatalogWege
        {
            Katalogzeilen = Zeilen,
            Detail = name => Felder(ModulKatalogArt.Stromspeicher, name),
            Speichern = (f, _, __) =>
            {
                geschrieben = f;
                return new KatalogSpeicherErgebnis(
                    true, "Datensatz gespeichert",
                    f.First(x => x.Schluessel == ModulKatalogProfil.FeldBezeichner).Wert);
            },
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
        };

        var cut = Aufbauen(ModulKatalogArt.Stromspeicher, wege);

        KiMaskenbruecke.Feldzugang(KiMaskennamen.STROMSPEICHER_KATALOG, "energie").Setzen(120.0);
        cut.Render();

        KiErgebnis ok = KiMaskenbruecke.Haken(KiMaskennamen.STROMSPEICHER_KATALOG)
                                       .Speichern().GetAwaiter().GetResult();

        Assert.Equal(KiStatus.Ausgefuehrt, ok.Status);
        Assert.NotNull(geschrieben);
        Assert.Equal("120", geschrieben!
                            .First(f => f.Schluessel == ModulKatalogProfil.FeldEnergie).Wert);

        // Eine geleerte PFLICHTZAHL hält den Speicherweg an.
        cut.Instance.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldEnergie).Wert = "";
        geschrieben = null;

        KiErgebnis abgelehnt = KiMaskenbruecke.Haken(KiMaskennamen.STROMSPEICHER_KATALOG)
                                              .Speichern().GetAwaiter().GetResult();

        Assert.NotEqual(KiStatus.Ausgefuehrt, abgelehnt.Status);
        Assert.Null(geschrieben);
    }
}
