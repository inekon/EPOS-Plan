using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Normwerte einlesen…" am Blatt „Nutzungsprofile"</b> (Stufe NP7b; Konzept Nutzungsprofile 6.5, E131) über einem Weg
/// OHNE Datenbank — die Delegaten merken sich, was sie bekommen haben. Geprüft werden: der Dialog ohne Gaben, die Vorschau
/// mit Treffern, Stand, Abweichungen und der Tabelle „Nicht übernommen", die weiche Sperre von „Übernehmen", der Schalter
/// „vorhandene Werte überschreiben" aus und an, Abbrechen ohne zu schreiben, die Knöpfe nur an der Kategorie DIN, das
/// Entfernen mit Rückfrage und die Texte in Englisch. Alle Werte sind Phantasiewerte; das Programm kennt keine Normwerte.
/// </summary>
/// <remarks>Die Kultur ist über <see cref="EposBunitContext"/> auf de-DE gepinnt.</remarks>
public class NormwerteEinlesenTests : EposBunitContext
{
    private const long KAT_MUSTER = 1;
    private const long KAT_DIN = 3;

    /// <summary>Ein Weg, der mitschreibt — die Rolle der Hülle im Prüfstand.</summary>
    private sealed class Probeweg
    {
        internal readonly List<string> Spur = new();
        internal NormwerteVorschau? Vorschau = Probevorschau();
        internal int Eingelesen = 2;

        internal NormwerteWeg Weg() => new()
        {
            Laden = q =>
            {
                Spur.Add("Laden:" + q);
                return Task.FromResult(Vorschau);
            },
            Einlesen = ueberschreiben =>
            {
                Spur.Add("Einlesen:" + ueberschreiben);
                return new RaumnutzungErgebnis(true, "2 Profil(e) gefüllt", 2);
            },
            VorlageSpeichern = () =>
            {
                Spur.Add("Vorlage");
                return Task.FromResult(new RaumnutzungErgebnis(true, "Vorlage gespeichert: probe.csv"));
            },
            Entfernen = () =>
            {
                Spur.Add("Entfernen");
                Eingelesen = 0;
                return new RaumnutzungErgebnis(true, "Eingelesene Werte aus 2 Profil(en) der DIN entfernt.", 2);
            },
            Eingelesene = () => Eingelesen,
        };
    }

    /// <summary>Zwei Treffer (einer leer, einer mit abweichenden Werten), dazu je ein Hinweis jeder Art.</summary>
    private static NormwerteVorschau Probevorschau() => new(
        true, "", "probe.csv",
        new[]
        {
            new NormwerteTreffer("901", "Phantasieraum A", false, false, Array.Empty<string>()),
            new NormwerteTreffer("902", "Phantasieraum B", true, true, new[] { "Heiz_Soll", "Aussenluft_Person" }),
        },
        new[] { "Nr. 999 unbekannt" }, new[] { "Nr. 903 ohne Wert" }, new[] { "Nr. 901 doppelt" }, new[] { "Zeile 7: Fehler" });

    public NormwerteEinlesenTests() => Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());

    private IRenderedComponent<NormwerteEinlesenDialog> Dialog(Probeweg? w, Action<RaumnutzungErgebnis?>? geschlossen = null)
        => Render<NormwerteEinlesenDialog>(p => p
               .Add(x => x.Weg, w?.Weg())
               .Add(x => x.Geschlossen, (RaumnutzungErgebnis? e) => geschlossen?.Invoke(e)));

    private static IElement Ok(IRenderedComponent<NormwerteEinlesenDialog> cut) => cut.Find(".epos-leiste .epos-knopf--primaer");

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_ohne_Quellknopf_und_Uebernehmen_ist_gesperrt()
    {
        RaumnutzungErgebnis? ende = RaumnutzungErgebnis.Gut;
        var cut = Dialog(null, e => ende = e);
        Assert.Empty(cut.FindAll(".epos-normwerte-csv"));
        Assert.Empty(cut.FindAll(".epos-normwerte-vorlage"));
        Assert.NotNull(cut.Find(".epos-normwerte-ohnedatei"));
        Assert.Equal("true", Ok(cut).GetAttribute("aria-disabled"));
        Ok(cut).Click();
        Assert.Equal(RaumnutzungErgebnis.Gut, ende); // gesperrt: nicht geschlossen
        Assert.Contains("Zuerst eine Datei", cut.Markup);
    }

    [Fact]
    public void Die_Vorschau_zeigt_Treffer_Stand_Abweichungen_und_was_nicht_uebernommen_wird()
    {
        var w = new Probeweg();
        var cut = Dialog(w);
        cut.Find(".epos-normwerte-csv").Click();
        Assert.Equal(new[] { "Laden:Csv" }, w.Spur);

        IReadOnlyList<IElement> zeilen = cut.FindAll(".epos-normwerte-zeile");
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("leer", zeilen[0].GetAttribute("data-stand"));
        Assert.Equal("eingelesen", zeilen[1].GetAttribute("data-stand"));
        Assert.Contains("Heiz_Soll, Aussenluft_Person", zeilen[1].QuerySelector(".epos-normwerte-abweichung")!.TextContent);
        Assert.Contains("2 Treffer, davon 1 mit vorhandenen Werten und 1 mit Abweichungen", cut.Find(".epos-normwerte-zaehlung").TextContent);
        Assert.Contains("Heiz_Soll 1", cut.Find(".epos-normwerte-spalten").TextContent);
        Assert.Equal(new[] { "unbekannt", "ohnewert", "doppelt", "meldung" },
                     cut.FindAll(".epos-normwerte-hinweis").Select(z => z.GetAttribute("data-art")).ToArray());
        Assert.Null(Ok(cut).GetAttribute("aria-disabled"));
        Assert.False(cut.Instance.Ueberschreiben);
        Assert.NotNull(cut.Find(".epos-normwerte-ueberspringen"));

        cut.Find(".epos-normwerte-projekt").Click();
        Assert.Equal("Laden:Projektdatei", w.Spur.Last());
    }

    [Fact]
    public void Uebernehmen_ohne_Ueberschreiben_schreibt_mit_falsch_und_schliesst_mit_dem_Ergebnis()
    {
        var w = new Probeweg();
        RaumnutzungErgebnis? ende = null;
        var cut = Dialog(w, e => ende = e);
        cut.Find(".epos-normwerte-csv").Click();
        Ok(cut).Click();
        Assert.Equal("Einlesen:False", w.Spur.Last());
        Assert.NotNull(ende);
        Assert.True(ende!.Ok);
    }

    [Fact]
    public void Uebernehmen_mit_Ueberschreiben_schreibt_mit_wahr()
    {
        var w = new Probeweg();
        var cut = Dialog(w);
        cut.Find(".epos-normwerte-csv").Click();
        cut.Find(".epos-normwerte input[type=checkbox]").Change(true);
        Assert.True(cut.Instance.Ueberschreiben);
        Assert.Empty(cut.FindAll(".epos-normwerte-ueberspringen"));
        Ok(cut).Click();
        Assert.Equal("Einlesen:True", w.Spur.Last());
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_schreiben()
    {
        var w = new Probeweg();
        bool gerufen = false;
        RaumnutzungErgebnis? ende = RaumnutzungErgebnis.Gut;
        var cut = Dialog(w, e => { gerufen = true; ende = e; });
        cut.Find(".epos-normwerte-csv").Click();
        cut.FindAll(".epos-leiste .epos-knopf").First(k => k.TextContent.Trim() == "Abbrechen").Click();
        Assert.True(gerufen);
        Assert.Null(ende);
        Assert.DoesNotContain(w.Spur, s => s.StartsWith("Einlesen", StringComparison.Ordinal));
    }

    [Fact]
    public void Das_Kreuz_im_eigenen_Kopf_wirkt_wie_Abbrechen()
    {
        var w = new Probeweg();
        bool gerufen = false;
        RaumnutzungErgebnis? ende = RaumnutzungErgebnis.Gut;
        var cut = Dialog(w, e => { gerufen = true; ende = e; });
        Assert.Equal("Normwerte einlesen", cut.Find(".epos-normwerte h1").TextContent.Trim());
        cut.Find(".epos-normwerte-kopf .epos-dialog-zu").Click();
        Assert.True(gerufen);
        Assert.Null(ende);
    }

    [Fact]
    public void Eine_abgelehnte_Quelle_sperrt_Uebernehmen_mit_dem_Grund_des_Kerns()
    {
        var w = new Probeweg { Vorschau = NormwerteVorschau.Fehler("leer.csv", "nichts einzulesen") };
        var cut = Dialog(w);
        cut.Find(".epos-normwerte-csv").Click();
        Assert.Contains("nichts einzulesen", cut.Find(".epos-normwerte-ablehnung").TextContent);
        Assert.Equal("nichts einzulesen", Ok(cut).GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-normwerte input[type=checkbox]"));
    }

    [Fact]
    public void Vorlage_speichern_meldet_die_Datei()
    {
        var w = new Probeweg();
        var cut = Dialog(w);
        cut.Find(".epos-normwerte-vorlage").Click();
        Assert.Equal(new[] { "Vorlage" }, w.Spur);
        Assert.Contains("probe.csv", cut.Find(".epos-normwerte-meldung").TextContent);
    }

    // =====================================================================
    //  Am Blatt
    // =====================================================================

    private IRenderedComponent<RaumnutzungBlatt> Blatt(Probeweg w)
    {
        var kategorien = new List<RaumnutzungKategorieDaten>
        {
            new(KAT_MUSTER, "EPOS-Muster", RaumnutzungArt.EposMuster, true, "", ""),
            new(KAT_DIN, "DIN/TS 18599-10", RaumnutzungArt.Din18599, true, "", ""),
        };
        var profile = new List<RaumnutzungProfilDaten>
        {
            new() { Id = 11, IdKategorie = KAT_MUSTER, Nummer = "1", Bezeichner = "Muster", Ausgeliefert = true },
            new() { Id = 31, IdKategorie = KAT_DIN, Nummer = "901", Bezeichner = "Phantasieraum A", Ausgeliefert = true },
        };
        var weg = new RaumnutzungWeg
        {
            Kategorien = () => kategorien,
            Profile = id => profile.Where(p => p.IdKategorie == id).ToList(),
            Zuordnungen = () => Array.Empty<RaumnutzungZuordnungDaten>(),
            Normwerte = w.Weg(),
        };
        return Render<RaumnutzungBlatt>(p => p
            .Add(x => x.Katalogweg, weg)
            .Add(x => x.Texte, new RaumnutzungTexte())
            .Add(x => x.GroessenTexte, new KonditionierungTexte()));
    }

    private static void KategorieWaehlen(IRenderedComponent<RaumnutzungBlatt> cut, long id)
        => cut.Find($"[data-kategorie=\"{id}\"] .epos-raumnutzung-kategorie-waehlen").Click();

    [Fact]
    public void Die_Knoepfe_stehen_nur_an_der_Kategorie_DIN()
    {
        var cut = Blatt(new Probeweg());
        Assert.Empty(cut.FindAll(".epos-raumnutzung-normwerte-einlesen"));
        KategorieWaehlen(cut, KAT_MUSTER);
        Assert.Empty(cut.FindAll(".epos-raumnutzung-normwerte-einlesen"));
        KategorieWaehlen(cut, KAT_DIN);
        Assert.NotNull(cut.Find(".epos-raumnutzung-normwerte-einlesen"));
        Assert.NotNull(cut.Find(".epos-raumnutzung-normwerte-entfernen"));
    }

    [Fact]
    public void Der_Dialog_oeffnet_als_Ueberlagerung_und_meldet_die_Zahl_nach_Uebernehmen()
    {
        var w = new Probeweg();
        var cut = Blatt(w);
        KategorieWaehlen(cut, KAT_DIN);
        cut.Find(".epos-raumnutzung-normwerte-einlesen").Click();
        Assert.True(cut.Instance.NormwerteOffen);
        Assert.NotNull(cut.Find(".epos-ueberlagerung .epos-normwerte"));
        Assert.Empty(cut.FindAll(".epos-normwerte h1")); // Titel und Kreuz trägt die Überlagerung
        cut.Find(".epos-normwerte-csv").Click();
        cut.Find(".epos-ueberlagerung .epos-leiste .epos-knopf--primaer").Click();
        Assert.False(cut.Instance.NormwerteOffen);
        Assert.Equal("Einlesen:False", w.Spur.Last());
        Assert.Contains("2 Profil(e) gefüllt", cut.Find(".epos-raumnutzung-normwerte-meldung").TextContent);
    }

    [Fact]
    public void Entfernen_fragt_nach_und_schreibt_erst_mit_Ja()
    {
        var w = new Probeweg();
        var cut = Blatt(w);
        KategorieWaehlen(cut, KAT_DIN);
        cut.Find(".epos-raumnutzung-normwerte-entfernen").Click();
        Assert.Contains("von 2 Profil(en) entfernen", cut.Markup);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Nicht entfernen").Click();
        Assert.Empty(w.Spur);

        cut.Find(".epos-raumnutzung-normwerte-entfernen").Click();
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Entfernen").Click();
        Assert.Equal(new[] { "Entfernen" }, w.Spur);
        Assert.Contains("entfernt", cut.Find(".epos-raumnutzung-normwerte-meldung").TextContent);

        // Danach ohne eingelesene Werte: weich gesperrt mit Grund, keine Rückfrage.
        IElement knopf = cut.Find(".epos-raumnutzung-normwerte-entfernen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Single(w.Spur);
    }

    [Theory]
    [InlineData("de-DE", "Normwerte einlesen…", "vorhandene Werte überschreiben", "Übernehmen")]
    [InlineData("en-US", "Read in standard values…", "overwrite existing values", "Apply")]
    public void Die_Texte_stehen_in_beiden_Sprachen(string kultur, string knopf, string schalter, string ok)
    {
        using var _ = new Kulturvorrichtung(kultur);
        NormwerteTexte t = RaumnutzungNormwerteHuelle.Texte();
        Assert.Equal(knopf, t.KnopfEinlesen);
        Assert.Equal(schalter, t.Ueberschreiben);
        Assert.Equal(ok, t.KnopfUebernehmen);
        Assert.All(typeof(NormwerteTexte).GetProperties().Where(p => p.PropertyType == typeof(string)),
                   p => Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(t)), p.Name));
    }
}
