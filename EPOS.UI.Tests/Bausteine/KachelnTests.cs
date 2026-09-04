using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <see cref="ErzeugerKachel"/> (iU9-W10b.0d) — der Ersatz fuer das UserControl
/// <c>Views/Simulation/ErzeugerKarte.cs</c>.
///
/// <para>Geprueft werden die Sichtbarkeitsregeln des Vorlaeufers (▲▼ nur mit
/// <c>Reihenfolge</c> und dort nur ausgegraut statt versteckt, ✎ nur mit
/// <c>Editierbar</c>, + bzw. × je nach Zustand), die sechs Chipstile, die acht
/// Ereignisse und der Aufklappbereich.</para>
/// </summary>
public class ErzeugerKachelTests : BunitContext
{
    private static ErzeugerKachelDaten Aufbau() => new ErzeugerKachelDaten
    {
        Schluessel = "Wärmepumpe",
        Rang = "1",
        Titel = "Wärmepumpe · WP 1",
        Chips = new[]
        {
            new ChipDaten("Quelle: Außenluft", ChipStil.Quelle, "Wärmequelle ändern", ChipZiel.Quelle),
            new ChipDaten("Senke: Heizkreis", ChipStil.Neutral, "", ChipZiel.Senke),
            new ChipDaten("55 / 45 °C", ChipStil.Flaeche),
            new ChipDaten("Konfiguration prüfen", ChipStil.Warnung, "W3: …", ChipZiel.Senke)
        },
        Reihenfolge = true,
        AufMoeglich = false,
        AbMoeglich = true,
        Umschaltbar = true,
        Editierbar = true
    };

    [Fact]
    public void Kopfzeile_zeigt_Rang_Titel_und_die_vier_Schalter()
    {
        var cut = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, Aufbau()));

        Assert.Equal("1", cut.Find("span.epos-erzeugerkachel-rang").TextContent);
        Assert.Equal("Wärmepumpe · WP 1", cut.Find("span.epos-erzeugerkachel-titel").TextContent);

        var glyphen = cut.FindAll("button.epos-erzeugerkachel-glyphe");
        Assert.Equal(4, glyphen.Count);                        // ▲ ▼ ✎ ×
        Assert.Equal("▲", glyphen[0].TextContent.Trim());
        Assert.Equal("×", glyphen[3].TextContent.Trim());
    }

    /// <summary>
    /// ▲▼ bleiben SICHTBAR, auch wenn sie nicht moeglich sind — nur ausgegraut.
    /// Sonst rutschte die Kopfzeile bei jedem Verschieben um eine Schalterbreite.
    /// </summary>
    [Fact]
    public void Das_nicht_moegliche_Verschieben_ist_ausgegraut_nicht_versteckt()
    {
        var cut = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, Aufbau()));

        var glyphen = cut.FindAll("button.epos-erzeugerkachel-glyphe");
        Assert.True(glyphen[0].HasAttribute("disabled"));      // AufMoeglich = false
        Assert.False(glyphen[1].HasAttribute("disabled"));     // AbMoeglich  = true
    }

    /// <summary>
    /// Ohne Kaskade (Strom- und Speicherseite) gibt es weder ▲▼ noch ✎ — dort
    /// entscheidet nur die Teilnahme.
    /// </summary>
    [Fact]
    public void Ohne_Reihenfolge_und_ohne_Editierbar_bleiben_die_Schalter_weg()
    {
        ErzeugerKachelDaten a = Aufbau();
        a.Reihenfolge = false;
        a.Editierbar = false;

        var cut = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, a));

        Assert.Single(cut.FindAll("button.epos-erzeugerkachel-glyphe"));   // nur ×
    }

    [Fact]
    public void Eine_verfuegbare_Kachel_zeigt_Aufnehmen_statt_Entfernen()
    {
        ErzeugerKachelDaten a = Aufbau();
        a.Zustand = Kachelzustand.Verfuegbar;

        var cut = Render<ErzeugerKachel>(p => p
            .Add(x => x.Aufbau, a)
            .Add(x => x.AufnehmenText, "+ aufnehmen"));

        Assert.Single(cut.FindAll("button.epos-erzeugerkachel-aufnehmen"));
        Assert.Equal("+ aufnehmen",
                     cut.Find("button.epos-erzeugerkachel-aufnehmen").TextContent.Trim());
        Assert.DoesNotContain(cut.FindAll("button.epos-erzeugerkachel-glyphe"),
                              b => b.TextContent.Trim() == "×");

        Assert.Single(cut.FindAll("div.epos-erzeugerkachel--verfuegbar"));
    }

    [Fact]
    public void Jeder_Chipstil_bekommt_seine_eigene_Klasse()
    {
        var cut = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, Aufbau()));

        Assert.Equal(4, cut.FindAll(".epos-chip").Count);
        Assert.Single(cut.FindAll(".epos-chip--quelle"));
        Assert.Single(cut.FindAll(".epos-chip--flaeche"));
        Assert.Single(cut.FindAll(".epos-chip--warnung"));

        // Chips MIT Ziel sind Knoepfe, der Temperaturchip ohne Ziel ist es nicht.
        Assert.Equal(3, cut.FindAll("button.epos-chip--ziel").Count);
        Assert.Single(cut.FindAll("span.epos-chip"));
    }

    [Fact]
    public void Die_fuenf_Knopfereignisse_melden_ihren_Schluessel()
    {
        string oben = "", unten = "", bearbeitet = "", entfernt = "";

        var cut = Render<ErzeugerKachel>(p => p
            .Add(x => x.Aufbau, Aufbau())
            .Add(x => x.NachOben, (string s) => oben = s)
            .Add(x => x.NachUnten, (string s) => unten = s)
            .Add(x => x.Bearbeiten, (string s) => bearbeitet = s)
            .Add(x => x.Entfernen, (string s) => entfernt = s));

        cut.FindAll("button.epos-erzeugerkachel-glyphe")[1].Click();   // ▼
        Assert.Equal("Wärmepumpe", unten);

        cut.FindAll("button.epos-erzeugerkachel-glyphe")[2].Click();   // ✎
        Assert.Equal("Wärmepumpe", bearbeitet);

        cut.FindAll("button.epos-erzeugerkachel-glyphe")[3].Click();   // ×
        Assert.Equal("Wärmepumpe", entfernt);

        // ▲ ist ausgegraut - es meldet nichts.
        Assert.Equal("", oben);
    }

    /// <summary>
    /// Der Doppelklick auf einen Chip MIT Ziel oeffnet dessen Editor; die
    /// Eingabetaste tut dasselbe (der Tastaturweg, den der Vorlaeufer nicht hatte).
    /// </summary>
    [Fact]
    public void Ein_Chip_mit_Ziel_meldet_sich_bei_Doppelklick_und_Eingabe()
    {
        ChipDaten? gemeldet = null;

        var cut = Render<ErzeugerKachel>(p => p
            .Add(x => x.Aufbau, Aufbau())
            .Add(x => x.ChipBearbeiten, (ChipDaten c) => gemeldet = c));

        cut.FindAll("button.epos-chip--ziel")[0].DoubleClick();
        Assert.NotNull(gemeldet);
        Assert.Equal(ChipZiel.Quelle, gemeldet!.Ziel);

        gemeldet = null;
        cut.FindAll("button.epos-chip--ziel")[1]
           .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.NotNull(gemeldet);
        Assert.Equal(ChipZiel.Senke, gemeldet!.Ziel);
    }

    /// <summary>
    /// Der Aufklappbereich erscheint nur mit Detailchips, und ein Klick auf die
    /// Kachel klappt sie dann auf UND waehlt sie aus (Abnahmebefund 2).
    /// </summary>
    [Fact]
    public void Der_Detailbereich_erscheint_nur_mit_Detailchips()
    {
        ErzeugerKachelDaten ohne = Aufbau();
        var kahl = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, ohne));
        Assert.Empty(kahl.FindAll("button.epos-erzeugerkachel-pfeil"));
        Assert.Empty(kahl.FindAll("div.epos-erzeugerkachel-detail"));

        ErzeugerKachelDaten mit = Aufbau();
        mit.Detailchips = new[] { new ChipDaten("Kapazität 11,0 kWh", ChipStil.Senke) };
        mit.Aufgeklappt = true;

        var offen = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, mit));
        Assert.Single(offen.FindAll("button.epos-erzeugerkachel-pfeil"));
        Assert.Equal("▾", offen.Find("button.epos-erzeugerkachel-pfeil").TextContent.Trim());
        Assert.Single(offen.FindAll("div.epos-erzeugerkachel-detail"));
    }

    [Fact]
    public void Ein_Klick_auf_die_Kachel_waehlt_aus_und_klappt_um()
    {
        string gewaehlt = "", umgeschaltet = "";

        ErzeugerKachelDaten a = Aufbau();
        a.Detailchips = new[] { new ChipDaten("Modul", ChipStil.Flaeche) };

        var cut = Render<ErzeugerKachel>(p => p
            .Add(x => x.Aufbau, a)
            .Add(x => x.Ausgewaehlt, (string s) => gewaehlt = s)
            .Add(x => x.Umschalten, (string s) => umgeschaltet = s));

        cut.Find("div.epos-erzeugerkachel").Click();
        Assert.Equal("Wärmepumpe", gewaehlt);
        Assert.Equal("Wärmepumpe", umgeschaltet);
    }

    [Fact]
    public void Eine_hervorgehobene_Kachel_traegt_die_Auswahlklasse()
    {
        ErzeugerKachelDaten a = Aufbau();
        a.Hervorgehoben = true;

        var cut = Render<ErzeugerKachel>(p => p.Add(x => x.Aufbau, a));
        Assert.Single(cut.FindAll("div.epos-erzeugerkachel--hervor"));
    }
}

/// <summary>
/// <see cref="SpeicherKachel"/> (iU9-W10b.0d) — der Ersatz fuer
/// <c>Views/Simulation/SpeicherKarte.cs</c> samt dem Steuerelement
/// <c>SchwellenBand</c>.
/// </summary>
public class SpeicherKachelTests : BunitContext
{
    private static SpeicherKachelDaten Daten() => new SpeicherKachelDaten
    {
        IdPuffer = 1008007,
        Bezeichner = "Puffer A",
        Verwendung = "Heizung",
        Schichtung = "5 Schichten",
        Volumen = "800 l",
        Temperaturpaar = "55 / 45 °C",
        LaderAnzahl = 2,
        AbnehmerAnzahl = 3,
        Detailzeilen = new[]
        {
            "Lader: 1. Wärmepumpe (bis 70 %) · 2. Heizkessel (bis 95 %)",
            "Versorgt: Heizung",
            "Entladeprio: automatisch (10)"
        },
        Schwellentext = "Schwellen 10 / 70 / 95 %",
        SchwelleEin = 10,
        SchwelleAusNachrang = 70,
        SchwelleAus = 95
    };

    [Fact]
    public void Die_zugeklappte_Zeile_zeigt_Name_Badges_und_Kurzbilanz()
    {
        var cut = Render<SpeicherKachel>(p => p
            .Add(x => x.Inhalt, Daten())
            .Add(x => x.BilanzText, "{0} Lader · {1} Abnehmer"));

        Assert.Equal("Puffer A", cut.Find("span.epos-speicherkachel-name").TextContent);
        Assert.Equal(2, cut.FindAll("span.epos-chip--badge").Count);        // Verwendung, Schichtung
        Assert.Equal(2, cut.FindAll("span.epos-chip--flaeche").Count);      // Volumen, Temperaturpaar
        Assert.Equal("2 Lader · 3 Abnehmer",
                     cut.Find("span.epos-speicherkachel-bilanz").TextContent);

        // Zugeklappt: kein Detailbereich, kein Schwellenband.
        Assert.Empty(cut.FindAll("div.epos-speicherkachel-detail"));
        Assert.Empty(cut.FindAll("svg.epos-speicherkachel-band"));
    }

    /// <summary>
    /// Ein Ein-Zonen-Speicher fuehrt kein Schicht-Badge — das Verzeichnis kennt
    /// solche Speicher gar nicht erst (PAKET P1).
    /// </summary>
    [Fact]
    public void Ohne_Schichtung_und_ohne_Volumen_bleiben_die_Badges_weg()
    {
        SpeicherKachelDaten d = Daten();
        d.Schichtung = "";
        d.Volumen = "";

        var cut = Render<SpeicherKachel>(p => p.Add(x => x.Inhalt, d));

        Assert.Single(cut.FindAll("span.epos-chip--badge"));     // nur die Verwendung
        Assert.Single(cut.FindAll("span.epos-chip--flaeche"));   // nur das Temperaturpaar
    }

    [Fact]
    public void Aufgeklappt_stehen_Detailzeilen_Schwellenband_und_Schwellentext()
    {
        var cut = Render<SpeicherKachel>(p => p
            .Add(x => x.Inhalt, Daten())
            .Add(x => x.Aufgeklappt, true));

        var zeilen = cut.FindAll("p.epos-speicherkachel-zeile");
        Assert.Equal(4, zeilen.Count);                      // drei Detailzeilen + Schwellentext
        Assert.StartsWith("Lader:", zeilen[0].TextContent);
        Assert.Equal("Schwellen 10 / 70 / 95 %", zeilen[3].TextContent);

        Assert.Single(cut.FindAll("svg.epos-speicherkachel-band"));
        Assert.Equal("▾", cut.Find("button.epos-speicherkachel-pfeil").TextContent.Trim());
    }

    /// <summary>
    /// Die drei Marken sitzen auf ihren Prozentwerten, und die Reservezone der
    /// vorrangigen Anlage liegt zwischen Nachrang- und Abschaltschwelle.
    /// </summary>
    [Fact]
    public void Das_Schwellenband_setzt_drei_Marken_und_die_Reservezone()
    {
        var cut = Render<SpeicherKachel>(p => p
            .Add(x => x.Inhalt, Daten())
            .Add(x => x.Aufgeklappt, true));

        Assert.Equal("10", cut.Find("line.epos-speicherkachel-marke--ein").GetAttribute("x1"));
        Assert.Equal("70", cut.Find("line.epos-speicherkachel-marke--nachrang").GetAttribute("x1"));
        Assert.Equal("95", cut.Find("line.epos-speicherkachel-marke--aus").GetAttribute("x1"));

        var reserve = cut.Find("rect.epos-speicherkachel-reserve");
        Assert.Equal("70", reserve.GetAttribute("x"));
        Assert.Equal("25", reserve.GetAttribute("width"));
    }

    /// <summary>Ohne Reservezone (Nachrang = Aus) entfaellt das Rechteck.</summary>
    [Fact]
    public void Ohne_Reservezone_bleibt_das_Rechteck_weg()
    {
        SpeicherKachelDaten d = Daten();
        d.SchwelleAusNachrang = 95;

        var cut = Render<SpeicherKachel>(p => p
            .Add(x => x.Inhalt, d)
            .Add(x => x.Aufgeklappt, true));

        Assert.Empty(cut.FindAll("rect.epos-speicherkachel-reserve"));
    }

    [Fact]
    public void Der_Klick_klappt_um_und_waehlt_aus()
    {
        int umgeschaltet = 0, gewaehlt = 0;

        var cut = Render<SpeicherKachel>(p => p
            .Add(x => x.Inhalt, Daten())
            .Add(x => x.Umschalten, (int id) => umgeschaltet = id)
            .Add(x => x.Ausgewaehlt, (int id) => gewaehlt = id));

        cut.Find("div.epos-speicherkachel").Click();
        Assert.Equal(1008007, umgeschaltet);
        Assert.Equal(1008007, gewaehlt);
    }

    [Fact]
    public void Das_Stiftsymbol_und_der_Doppelklick_melden_den_Editorwunsch()
    {
        int bearbeitet = 0;

        var cut = Render<SpeicherKachel>(p => p
            .Add(x => x.Inhalt, Daten())
            .Add(x => x.Bearbeiten, (int id) => bearbeitet = id));

        cut.Find("button.epos-speicherkachel-glyphe").Click();
        Assert.Equal(1008007, bearbeitet);

        bearbeitet = 0;
        cut.Find("div.epos-speicherkachel").DoubleClick();
        Assert.Equal(1008007, bearbeitet);
    }

    /// <summary>
    /// Der Mouseover der zugeklappten Zeile zeigt dieselben Details wie der
    /// Aufklappbereich (Konzept 3a).
    /// </summary>
    [Fact]
    public void Der_Kurzhinweis_traegt_Detailzeilen_und_Schwellentext()
    {
        var cut = Render<SpeicherKachel>(p => p.Add(x => x.Inhalt, Daten()));

        string? titel = cut.Find("div.epos-speicherkachel").GetAttribute("title");
        Assert.NotNull(titel);
        Assert.Contains("Versorgt: Heizung", titel!);
        Assert.EndsWith("Schwellen 10 / 70 / 95 %", titel!);
    }

    [Fact]
    public void Eine_hervorgehobene_Kachel_traegt_die_Auswahlklasse()
    {
        SpeicherKachelDaten d = Daten();
        d.Hervorgehoben = true;

        var cut = Render<SpeicherKachel>(p => p.Add(x => x.Inhalt, d));
        Assert.Single(cut.FindAll("div.epos-speicherkachel--hervor"));
    }
}
