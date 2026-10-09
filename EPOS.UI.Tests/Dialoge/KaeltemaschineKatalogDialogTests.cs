using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Verwaltung „Kältemaschinen"</b> (KU3-1, Teil 2) — Liste, Stammblatt, Kennlinie, Schloss,
/// Duplizieren und Löschen gegen einen Stub der Hülle, der die drei gesäten Geräte
/// (<see cref="KaeltemaschineSchema.SAAT"/>) führt und die Prüfregeln des Kerns
/// (<c>KaeltemaschineKatalogHuelle.Pruefen</c>, ohne Datenbank) nimmt.
///
/// <para>Die Kultur ist auf de-DE gepinnt; die englische Oberfläche prüft
/// <see cref="KaeltemaschineKatalogDialogEnglischTests"/>.</para>
/// </summary>
public class KaeltemaschineKatalogDialogTests : EposBunitContext
{
    public KaeltemaschineKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Der Stub der Hülle: die Saat als Auslieferungssätze, Schreibwege als Listen.</summary>
    internal sealed class Katalog
    {
        internal readonly List<KaeltemaschineDaten> Saetze;
        internal readonly List<KaeltemaschineDaten> Gespeichert = new();
        internal readonly List<KaeltemaschineModel> Modelle = new();
        internal readonly List<int> Geloescht = new();
        internal readonly List<(int Id, string Name)> Dupliziert = new();
        internal bool? Geschlossen;
        private int _naechste = 100;

        internal Katalog()
        {
            Saetze = KaeltemaschineSchema.SAAT.Select((g, i) => KaeltemaschineKatalogHuelle.AlsDaten(new KaeltemaschineModel
            {
                Id = i + 1,
                Bezeichner = g.Bezeichner,
                Nennkaelteleistung_kW = g.Nennkaelteleistung,
                Nenn_EER = g.NennEer,
                Kaeltemittel = g.Kaeltemittel,
                Rueckkuehlart = g.Rueckkuehlart,
                Mindestteillast_Prozent = g.Mindestteillast,
                Hilfsstrom_Rueckkuehlung_kW = g.HilfsstromRueckkuehlung,
                Kaltwasser_Vorlauf_Min = g.KaltwasserVorlaufMin,
                ReadOnly = true,
                Kennlinie = g.Kennlinie.Select(p => new KaeltemaschineKenndatenModel
                {
                    Rueckkuehltemperatur = p.Rueckkuehl, Kaltwassertemperatur = p.Kaltwasser,
                    EER = p.Eer, Kaelteleistung_kW = p.Leistung
                }).ToList()
            })).ToList();
        }

        internal IReadOnlyList<Katalogfilterzeile> Zeilen()
            => Saetze.Select(s =>
                {
                    KaeltemaschineModel m = KaeltemaschineKatalogHuelle.AlsModell(s);
                    return new Katalogfilterzeile(s.Id, s.Bezeichner)
                        {
                            Geschuetzt = s.Auslieferung,
                            Schluessel = s.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        }
                        .MitText(Katalogfilterprofil.SpBezeichner, s.Bezeichner)
                        .MitText(Katalogfilterprofil.SpHersteller, s.Firma)
                        .MitText(Katalogfilterprofil.SpTyp, s.Typ)
                        .MitZahl(Katalogfilterprofil.SpNennkaelteleistung, s.Nennkaelteleistung, 1)
                        .MitZahl(Katalogfilterprofil.SpEer, s.NennEer, 2)
                        .MitText(Katalogfilterprofil.SpRueckkuehlart, KaeltemaschineStammCtrl.RueckkuehlartText(m.Rueckkuehlart));
                })
                .ToList();

        internal KaeltemaschineSpeicherErgebnis Speichern(KaeltemaschineDaten d)
        {
            KaeltemaschineDaten kopie = d.Kopie();
            Gespeichert.Add(kopie);
            Modelle.Add(KaeltemaschineKatalogHuelle.AlsModell(kopie));
            if (kopie.Id <= 0)
            {
                kopie.Id = ++_naechste;
                Saetze.Add(kopie);
            }
            else
            {
                Saetze[Saetze.FindIndex(s => s.Id == kopie.Id)] = kopie;
            }
            return new KaeltemaschineSpeicherErgebnis(true, "", kopie.Id);
        }
    }

    private IRenderedComponent<KaeltemaschineKatalogDialog> Aufbauen(Katalog? k = null, Schlosspruefung? schloss = null)
    {
        Katalog kat = k ?? new Katalog();
        return Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, () => schloss is null ? kat.Zeilen() : schloss.Markieren(kat.Zeilen()))
            .Add(x => x.Katalogprofil, Katalogfilterprofil.Finde(Anlagenart.Kaeltemaschine,
                s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Rueckkuehlarten, KaeltemaschineKatalogHuelle.Rueckkuehlarten())
            .Add(x => x.Lies, id => kat.Saetze.FirstOrDefault(s => s.Id == id) is KaeltemaschineDaten d
                ? Mit(d.Kopie(), schloss) : null)
            .Add(x => x.Pruefen, KaeltemaschineKatalogHuelle.Pruefen)
            .Add(x => x.Speichern, kat.Speichern)
            .Add(x => x.Loeschen, id =>
            {
                kat.Geloescht.Add(id);
                kat.Saetze.RemoveAll(s => s.Id == id);
                return new KaeltemaschineSpeicherErgebnis(true, "", id);
            })
            .Add(x => x.Duplizieren, (id, name) =>
            {
                kat.Dupliziert.Add((id, name));
                KaeltemaschineDaten neu = kat.Saetze.First(s => s.Id == id).Kopie();
                neu.Id = 0;
                neu.Bezeichner = name;
                neu.Auslieferung = false;
                return kat.Speichern(neu);
            })
            .Add(x => x.Schloss, schloss?.Weg())
            .Add(x => x.Geschlossen, e => kat.Geschlossen = e));
    }

    private static KaeltemaschineDaten Mit(KaeltemaschineDaten d, Schlosspruefung? schloss)
    {
        if (schloss is not null) d.Auslieferung = schloss.Gesperrt.Contains(d.Id);
        return d;
    }

    private static IElement Fussleiste(IRenderedComponent<KaeltemaschineKatalogDialog> cut)
        => cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();

    private static IElement Knopf(IRenderedComponent<KaeltemaschineKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung(IRenderedComponent<KaeltemaschineKatalogDialog> cut, string text)
        => cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

    private static IElement Stammblatt(IRenderedComponent<KaeltemaschineKatalogDialog> cut) => cut.Find(".epos-stammblatt");

    /// <summary>Dupliziert den ersten Saatsatz und liefert den Dialog mit der gewählten Kopie.</summary>
    private IRenderedComponent<KaeltemaschineKatalogDialog> MitKopie(Katalog k, Schlosspruefung? schloss = null)
    {
        var cut = Aufbauen(k, schloss);
        Handlung(cut, Texte.KnopfDuplizieren).Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == Texte.Ok).Click();
        return cut;
    }

    private static KaeltemaschineKatalogTexte Texte => new();

    // =================================================================================
    // Liste und Gerüst
    // =================================================================================

    [Fact]
    public void Die_Liste_zeigt_die_drei_gesaeten_Geraete_mit_den_Spalten_des_Kerns()
    {
        var cut = Aufbauen();

        Assert.Equal(KaeltemaschineSchema.SAAT.Select(g => g.Bezeichner), cut.Instance.Namensliste);
        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody tr").Count);
        string kopf = cut.Find(".epos-katalogliste thead").TextContent;
        foreach (string spalte in new[] { R.KFLT_SP_BEZEICHNER, R.KFLT_SP_NENNKAELTELEISTUNG, R.KFLT_SP_EER })
            Assert.Contains(spalte, kopf);
        Assert.Contains(R.KM_RUECKKUEHLART_LUFT, cut.Find(".epos-katalogliste tbody").TextContent);
        Assert.DoesNotContain("TROCKENKUEHLER", cut.Markup);
        Assert.Equal(new[] { R.ADM_BTN_SPEICHERN, R.ADM_BTN_VERWERFEN, R.ADM_BTN_IMPORT, R.ADM_BTN_NEU, R.ADM_BTN_BEENDEN },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }

    [Fact]
    public void Das_Blatt_oeffnet_mit_Kenndaten_und_Kennlinie()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);                                    // 200 kW, Trockenkühler

        Assert.Equal(new[] { R.ADM_SB_KENNDATEN, R.KM_GRUPPE_KENNLINIE },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        string blatt = Stammblatt(cut).TextContent;
        Assert.Contains("R513A", blatt);
        Assert.Contains(R.KM_RUECKKUEHLART_TROCKENKUEHLER, blatt);
        Assert.Equal(6, cut.Instance.Arbeitsstand.Kennlinie.Count);
        Assert.Equal(2, cut.Instance.Arbeitsstand.RueckkuehlartIndex);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<KaeltemaschineKatalogDialog>();

        Assert.Equal(R.KM_TITEL, cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(R.KM_LEER, cut.Find(".epos-kaeltemaschine-leer").TextContent);
        Assert.Equal(new[] { R.ADM_BTN_BEENDEN },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }

    // =================================================================================
    // Schloss: Auslieferungssätze sind gesperrt
    // =================================================================================

    [Fact]
    public void Ein_Auslieferungssatz_ist_nur_lesbar_und_wird_nie_geschrieben()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);

        Assert.True(cut.Instance.Auslieferung);
        Assert.Empty(Stammblatt(cut).QuerySelectorAll("input"));
        Assert.Empty(Stammblatt(cut).QuerySelectorAll("select"));

        IElement speichern = Knopf(cut, R.ADM_BTN_SPEICHERN);
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        speichern.Click();

        Assert.Equal(R.ADM_SPEICHERN_GESPERRT, cut.Instance.Meldung);
        Assert.Empty(k.Gespeichert);
        Assert.Equal(R.ADM_AW_LOESCHEN_GESPERRT, Handlung(cut, R.BST_BTN_LOESCHEN).GetAttribute("title"));
    }

    [Fact]
    public void Das_Schloss_sperrt_eine_Kopie_nach_Rueckfrage()
    {
        var schloss = new Schlosspruefung(1, 2, 3);
        var k = new Katalog();
        var cut = MitKopie(k, schloss);
        Assert.False(cut.Instance.Auslieferung);

        Assert.Equal("Schloss setzen...", Schlosspruefung.Beschriftung(cut));
        Schlosspruefung.Knopf(cut).Click();
        Assert.True(cut.Instance.Schlossfrage);
        Schlosspruefung.Ja(cut);

        Assert.Contains(101, schloss.Gesperrt);
        Assert.True(cut.Instance.Auslieferung);
        Assert.Empty(Stammblatt(cut).QuerySelectorAll("input"));
    }

    // =================================================================================
    // Duplizieren, Kennlinie, Prüfregeln
    // =================================================================================

    [Fact]
    public void Duplizieren_legt_eine_Kopie_ohne_Schloss_samt_Kennlinie_an()
    {
        var k = new Katalog();
        var cut = MitKopie(k);

        string quelle = KaeltemaschineSchema.SAAT[0].Bezeichner;
        Assert.Equal((1, quelle + " (Kopie)"), k.Dupliziert.Single());
        Assert.False(cut.Instance.Auslieferung);
        Assert.False(k.Saetze.Last().Auslieferung);
        Assert.Equal(6, cut.Instance.Arbeitsstand.Kennlinie.Count);
        Assert.Equal(6 * 4, Stammblatt(cut).QuerySelectorAll(".epos-kaeltemaschine-punkt input").Length);
    }

    [Fact]
    public void Die_Kennlinie_prueft_das_Temperaturpaar_und_schreibt_nichts()
    {
        var k = new Katalog();
        var cut = MitKopie(k);
        int vorher = k.Gespeichert.Count;

        cut.FindAll("button").First(b => b.TextContent.Trim() == R.KM_KENNLINIE_NEU).Click();
        Assert.True(cut.Instance.Geaendert);
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();
        Assert.Equal(R.KM_MSG_KENNLINIE_TEMPERATUR_LEER, cut.Instance.Meldung);

        IReadOnlyList<IElement> neu = cut.FindAll(".epos-kaeltemaschine-punkt").Last().QuerySelectorAll("input").ToList();
        neu[0].Input("25");
        cut.FindAll(".epos-kaeltemaschine-punkt").Last().QuerySelectorAll("input")[1].Input("6");
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();

        Assert.Equal(R.KM_MSG_KENNLINIE_DOPPELT, cut.Instance.Meldung);
        Assert.Contains("epos-status--fehler", cut.Find(".epos-leiste-fueller.epos-status").ClassName);
        Assert.Equal(vorher, k.Gespeichert.Count);
    }

    [Fact]
    public void Ein_Punkt_wird_entfernt_und_erst_mit_Speichern_geschrieben()
    {
        var k = new Katalog();
        var cut = MitKopie(k);
        int vorher = k.Gespeichert.Count;

        cut.FindAll(".epos-kaeltemaschine-punkt button").First().Click();
        Assert.Equal(5, cut.Instance.Arbeitsstand.Kennlinie.Count);
        Assert.Equal(vorher, k.Gespeichert.Count);

        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();

        Assert.Equal(vorher + 1, k.Gespeichert.Count);
        Assert.Equal(5, k.Modelle.Last().Kennlinie.Count);
        Assert.False(cut.Instance.Geaendert);
    }

    [Fact]
    public void Kein_Anzeigetext_ist_Steuerwert()
    {
        var k = new Katalog();
        var cut = MitKopie(k);

        IElement auswahl = Stammblatt(cut).QuerySelector("select")!;
        Assert.Contains(R.KM_RUECKKUEHLART_NASSKUEHLER, auswahl.TextContent);
        Assert.DoesNotContain(auswahl.QuerySelectorAll("option"),
                              o => KaeltemaschineSchema.RUECKKUEHLARTEN.Contains(o.GetAttribute("value") ?? ""));
        auswahl.Change("3");
        Knopf(cut, R.ADM_BTN_SPEICHERN).Click();

        Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER, k.Modelle.Last().Rueckkuehlart);
        Assert.Equal(3, k.Gespeichert.Last().RueckkuehlartIndex);
    }

    [Fact]
    public void Neu_legt_nach_OK_an_und_Abbrechen_schreibt_nichts()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);

        Knopf(cut, R.ADM_BTN_NEU).Click();
        Assert.True(cut.Instance.NeuOffen);
        cut.Find(".epos-kaeltemaschine-neu input").Input("Eigene Kältemaschine");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();

        Assert.False(cut.Instance.NeuOffen);
        Assert.Equal("Eigene Kältemaschine", k.Gespeichert.Single().Bezeichner);
        Assert.Equal(string.Format(R.KM_MSG_ANGELEGT, "Eigene Kältemaschine"), cut.Instance.Status);
        Assert.False(cut.Instance.Auslieferung);
    }

    [Fact]
    public void Loeschen_fragt_zurueck_und_loescht_eine_Kopie()
    {
        var k = new Katalog();
        var cut = MitKopie(k);
        string name = cut.Instance.Arbeitsstand.Bezeichner;

        Handlung(cut, R.BST_BTN_LOESCHEN).Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains("„" + name + "“", Schlosspruefung.Frage(cut));
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == R.ALLG_BTN_JA).Click();

        Assert.Equal(new[] { 101 }, k.Geloescht);
        Assert.Equal(string.Format(R.KM_MSG_GELOESCHT, name), cut.Instance.Status);
    }

    [Fact]
    public void Beenden_schliesst_und_haelt_bei_Aenderungen_an()
    {
        var k = new Katalog();
        var cut = MitKopie(k);
        cut.FindAll(".epos-kaeltemaschine-punkt button").First().Click();

        Knopf(cut, R.ADM_BTN_BEENDEN).Click();
        Assert.Null(k.Geschlossen);
        Assert.Equal(R.ADM_MSG_UNGESPEICHERT, cut.Instance.Meldung);

        Knopf(cut, R.ADM_BTN_VERWERFEN).Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(k.Geschlossen);
    }
}

/// <summary>Dieselbe Verwaltung auf der englischen Oberfläche: Titel, Gruppen, Rückkühlart, Steuerwert.</summary>
public sealed class KaeltemaschineKatalogDialogEnglischTests : EposBunitContext
{
    public KaeltemaschineKatalogDialogEnglischTests() : base("en-US")
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    [Fact]
    public void Die_englische_Oberflaeche_traegt_englische_Texte_und_denselben_Steuerwert()
    {
        var k = new KaeltemaschineKatalogDialogTests.Katalog();
        var cut = Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, k.Zeilen)
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Rueckkuehlarten, KaeltemaschineKatalogHuelle.Rueckkuehlarten())
            .Add(x => x.Lies, id => k.Saetze.First(s => s.Id == id).Kopie())
            .Add(x => x.Pruefen, KaeltemaschineKatalogHuelle.Pruefen)
            .Add(x => x.Speichern, k.Speichern)
            .Add(x => x.Duplizieren, (id, name) =>
            {
                KaeltemaschineDaten neu = k.Saetze.First(s => s.Id == id).Kopie();
                neu.Id = 0;
                neu.Bezeichner = name;
                neu.Auslieferung = false;
                return k.Speichern(neu);
            }));

        Assert.Equal("Chillers", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Performance curve", cut.FindAll(".epos-stammblattgruppe-titel")[1].TextContent);
        Assert.Contains("Air-cooled", cut.Find(".epos-katalogliste tbody").TextContent);
        Assert.Contains("Rated cooling capacity", cut.Find(".epos-stammblatt").TextContent);

        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim().StartsWith("Duplicate", StringComparison.Ordinal)).Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        IElement auswahl = cut.Find(".epos-stammblatt select");
        Assert.Contains("Wet cooling tower", auswahl.TextContent);
        auswahl.Change("3");
        cut.FindAll("button").First(b => b.TextContent.Trim() == R.ADM_BTN_SPEICHERN).Click();

        Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER,
                     KaeltemaschineKatalogHuelle.AlsModell(k.Gespeichert.Last()).Rueckkuehlart);
    }
}
