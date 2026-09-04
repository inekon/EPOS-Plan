using System.Globalization;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
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
public class ModulKatalogDialogTests : BunitContext
{
    public ModulKatalogDialogTests()
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
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    /// <summary>Das Profil in DEUTSCH — so, wie die Hülle es liefert.</summary>
    private static ModulKatalogProfil Profil(ModulKatalogArt art) =>
        ModulKatalogProfil.Finde(art, s => WindowsFormsApplication1.MyResource.Resource
                                               .ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<ModulZeile> Zeilen() => new[]
    {
        new ModulZeile(1, "Modul A"),
        new ModulZeile(2, "Modul B")
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
            Liste = Zeilen,
            Detail = name => Felder(art, name),
            Speichern = (f, _, __) => new KatalogSpeicherErgebnis(
                true, "Datensatz gespeichert",
                f.First(x => x.Schluessel == ModulKatalogProfil.FeldBezeichner).Wert),
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
        };

        return Render<ModulKatalogDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.ProfilVorgabe, Profil(art))
            .Add(x => x.Wege, wege ?? standard)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    // =================================================================================
    // Feldbestand je Ausprägung
    // =================================================================================

    [Theory]
    [InlineData(ModulKatalogArt.Stromspeicher, "Administration Stromspeicher", 13)]
    [InlineData(ModulKatalogArt.Photovoltaik, "Administration Photovoltaik Module", 13)]
    public void Jede_Auspraegung_zeigt_ihren_Titel_und_ihre_dreizehn_Felder(
        ModulKatalogArt art, string titel, int felder)
    {
        var cut = Aufbauen(art);

        Assert.Equal(titel, cut.Find(".epos-dialog-titel").TextContent);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (var feld in Profil(art).Felder)
            Assert.Contains(feld.Bezeichnung, texte);

        int gezeichnet = cut.FindAll(".epos-feld input").Count
                       + cut.FindAll(".epos-feld textarea").Count;
        Assert.Equal(felder, gezeichnet);
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

        cut.FindAll(".epos-anlagenwahl")[1].Click();

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

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();

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

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
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
    public void Neu_belegt_die_Photovoltaik_mit_zwei_Leerfeldern_und_zehn_Nullen()
    {
        var cut = Aufbauen(ModulKatalogArt.Photovoltaik);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Neues Modul");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        var werte = cut.FindAll(".epos-feld input").Select(e => e.GetAttribute("value") ?? "").ToList();
        Assert.Contains("Neues Modul", werte);
        Assert.Equal(10, werte.Count(w => w == "0"));
    }

    [Fact]
    public void Nach_Neu_schreibt_Speichern_ein_ANLEGEN()
    {
        bool? neu = null;
        var wege = new ModulKatalogWege
        {
            Liste = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (f, n, _) => { neu = n; return new KatalogSpeicherErgebnis(true, "ok", "X"); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
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
            Liste = Zeilen,
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
            Liste = Zeilen,
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
            Liste = Zeilen,
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
            Liste = Zeilen,
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
            Liste = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Speichern = (f, _, s) => { gesehen = f; schluessel = s; return new KatalogSpeicherErgebnis(true, "ok", "Modul A"); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal("Modul A", schluessel);
        Assert.Equal(13, gesehen!.Count);
    }

    [Fact]
    public void Ein_Fehlschlag_beim_Speichern_nennt_den_Grund()
    {
        var wege = new ModulKatalogWege
        {
            Liste = Zeilen,
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
            Liste = Zeilen,
            Detail = n => Felder(art, n),
            Loeschen = n => { geloescht = n; return new KatalogSpeicherErgebnis(true, "", n); }
        };
        var cut = Aufbauen(art, wege);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();

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
            Liste = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Loeschen = n => { gerufen = true; return new KatalogSpeicherErgebnis(true, "", n); }
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
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
            Liste = Zeilen,
            Detail = n => Felder(ModulKatalogArt.Stromspeicher, n),
            Loeschen = _ => new KatalogSpeicherErgebnis(false, "Der Satz ist schreibgeschützt.", "")
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Equal("Der Satz ist schreibgeschützt.", cut.Instance.Meldung);
    }

    [Fact]
    public void Loeschen_ohne_Auswahl_meldet()
    {
        var wege = new ModulKatalogWege
        {
            Liste = () => Array.Empty<ModulZeile>(),
            Detail = _ => null
        };
        var cut = Aufbauen(wege: wege);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();

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

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Bestaetigt);
        Assert.Equal("Modul A", ergebnis.Bezeichner);
    }

    [Fact]
    public void Esc_schliesst_ohne_Bestaetigung()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.Bestaetigt);
    }

    [Fact]
    public void Esc_bei_offener_Rueckfrage_schliesst_den_Dialog_nicht()
    {
        ModulErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }
}
