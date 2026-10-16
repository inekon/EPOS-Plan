using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// N5: „Klimaregion wählen…“ an Gruppe 4 des Ampelblatts — öffnet die Liste der Kopfleiste vorbelegt mit der Region des
/// Projekts, speichert über den Weg der Hülle, prüft das Blatt neu und gibt dem offenen Editor den Konditionierungsweg mit dem
/// Raster der neuen Region; ein benannter Grund bleibt am Feld; ohne Delegat der Hinweis wie bisher.
/// </summary>
public partial class GebaeudeDialogTests
{
    private sealed class Klimaprobe
    {
        public readonly List<int> Gespeichert = new();
        public int Gefragt;
        public string? Grund;
        public int Herkunft;

        public GebaeudeKlimaregionWeg Weg()
        {
            Gefragt++;
            return new GebaeudeKlimaregionWeg
            {
                Eintraege = new[] { (1, "Ort A (Probe)"), (2, "Ort B (Probe)") },
                Gewaehlt = 1,
                Speichern = id => { if (Grund is null) Gespeichert.Add(id); return Grund; },
                Herkunftszeile = id => { Herkunft++; return id == 1 ? "Klimadaten: Probequelle A" : ""; },
            };
        }
    }

    private static Task Waehlen(IRenderedComponent<GebaeudeDialog> cut, int id)
    {
        IRenderedComponent<Suchauswahl> feld = cut.FindComponents<Suchauswahl>()
            .Single(s => s.Instance.Bezeichnung == R.MPB_KLIMA_LABEL);
        return cut.InvokeAsync(() => feld.Instance.AuswahlChanged.InvokeAsync(id));
    }

    [Fact]
    public async Task N5_Klimaregion_waehlen_speichert_die_Region_und_prueft_neu()
    {
        using var _ = new Kulturvorrichtung();
        var probe = new Klimaprobe();
        var pruefweg = new ModellpruefungProbeweg();
        var cut = Aufbauen(zeilen: GespeicherteZeilen());
        cut.Render(p => p.Add(x => x.ModellpruefungGaben, _ => pruefweg.Weg()).Add(x => x.KlimaregionGaben, probe.Weg));
        Pruefknopf(cut)!.Click();
        int geprueft = pruefweg.Geprueft;

        cut.Find(".epos-modellpruefung-gruppe[data-gruppe='4'] button.epos-modellpruefung-klima").Click();
        Assert.True(cut.Instance.KlimaregionOffen);
        Assert.Equal(1, probe.Gefragt);
        Assert.Equal(R.MPB_KLIMA_TITEL, cut.FindAll(".epos-ueberlagerung-titel").Last().TextContent);
        IRenderedComponent<Suchauswahl> feld = cut.FindComponents<Suchauswahl>().Single(s => s.Instance.Bezeichnung == R.MPB_KLIMA_LABEL);
        Assert.Equal(1, feld.Instance.Auswahl);
        Assert.Equal(2, feld.Instance.Eintraege.Count);

        await Waehlen(cut, 2);
        cut.Find("button.epos-klimawahl-uebernehmen").Click();
        Assert.Equal(new[] { 2 }, probe.Gespeichert);
        Assert.False(cut.Instance.KlimaregionOffen);
        Assert.True(cut.Instance.ModellpruefungOffen);
        Assert.True(pruefweg.Geprueft > geprueft);
        Assert.Equal(string.Format(R.MPB_KLIMA_GESPEICHERT, "Ort B (Probe)"), cut.Instance.Meldung);
    }

    /// <summary>
    /// Nachzug N6: Unter der Liste steht die Herkunftszeile der gewählten Region wie im Projektassistenten; gefragt wird nur bei
    /// einem Wechsel der Wahl. Gegenprobe: eine Region ohne Auskunft zeigt keine Zeile.
    /// </summary>
    [Fact]
    public async Task N6_Klimaregion_zeigt_die_Herkunftszeile_der_Wahl()
    {
        using var _ = new Kulturvorrichtung();
        var probe = new Klimaprobe();
        var cut = Aufbauen(zeilen: GespeicherteZeilen());
        cut.Render(p => p.Add(x => x.ModellpruefungGaben, _ => new ModellpruefungProbeweg().Weg()).Add(x => x.KlimaregionGaben, probe.Weg));
        Pruefknopf(cut)!.Click();
        cut.Find("button.epos-modellpruefung-klima").Click();
        Assert.Equal("Klimadaten: Probequelle A", cut.Find(".epos-klimawahl-herkunft").TextContent.Trim());
        cut.Render();
        Assert.Equal(1, probe.Herkunft);

        await Waehlen(cut, 2);
        Assert.Empty(cut.FindAll(".epos-klimawahl-herkunft"));
        Assert.Equal(2, probe.Herkunft);
    }

    /// <summary>Ein Projektkontext, der seinen Wechsel auf Zuruf meldet (Nachzug N6).</summary>
    private sealed class Wechselkontext : IProjektKontext
    {
        public bool Vorhanden => true;
        public int Id => 1;
        public string Name => "Probe";
        public string Klimazone => "";
        public bool Uebernehmen(int id, string name) => false;
        public event Action? Gewechselt;
        public void Melden() => Gewechselt?.Invoke();
        public bool Hoert => Gewechselt is not null;
    }

    /// <summary>
    /// Nachzug N6: Eine offene Wärmebedarfsansicht liest ihren Parametersatz neu, sobald die Klimaregion wechselt — über die
    /// eigene Wahl und über das Ereignis des Projektkontexts. Gegenproben: geschlossen wird nichts gelesen; nach dem Schließen
    /// des Dialogs hört er nicht mehr zu.
    /// </summary>
    [Fact]
    public async Task N6_offene_Waermebedarfsansicht_liest_die_neue_Klimaregion()
    {
        using var _ = new Kulturvorrichtung();
        IProjektKontext vorher = WindowsFormsApplication1.Dienste.Projekt;
        var kontext = new Wechselkontext();
        WindowsFormsApplication1.Dienste.Projekt = kontext;
        try
        {
            int gelesen = 0;
            var probe = new Klimaprobe();
            var cut = Aufbauen(zeilen: GespeicherteZeilen(), bedarfGaben: z =>
            {
                gelesen++;
                return new Dictionary<string, object> { ["Daten"] = new GebaeudeBedarfDaten { Name = z.Name } };
            });
            cut.Render(p => p.Add(x => x.ModellpruefungGaben, _ => new ModellpruefungProbeweg().Weg()).Add(x => x.KlimaregionGaben, probe.Weg));
            Assert.True(kontext.Hoert);

            // Gegenprobe: ohne offene Ansicht liest der Wechsel nichts.
            await cut.InvokeAsync(kontext.Melden);
            Assert.Equal(0, gelesen);

            Knopf(cut, "Simulation...").Click();
            Assert.True(cut.Instance.BedarfOffen);
            Assert.Equal(1, gelesen);
            int fassung = cut.Instance.BedarfFassung;

            // Eigene Wahl der Region.
            Pruefknopf(cut)!.Click();
            cut.Find("button.epos-modellpruefung-klima").Click();
            await Waehlen(cut, 2);
            cut.Find("button.epos-klimawahl-uebernehmen").Click();
            Assert.Equal(new[] { 2 }, probe.Gespeichert);
            Assert.Equal(2, gelesen);
            Assert.True(cut.Instance.BedarfOffen);
            Assert.Equal(fassung + 1, cut.Instance.BedarfFassung);

            // Wechsel über die Kopfleiste (Ereignis des Projektkontexts).
            await cut.InvokeAsync(kontext.Melden);
            cut.WaitForAssertion(() => Assert.Equal(3, gelesen));
            Assert.Equal(fassung + 2, cut.Instance.BedarfFassung);

            DisposeComponents();
            Assert.False(kontext.Hoert);
        }
        finally
        {
            WindowsFormsApplication1.Dienste.Projekt = vorher;
        }
    }

    [Fact]
    public async Task N5_Gegenprobe_Grund_bleibt_am_Feld_und_unveraenderte_Wahl_schreibt_nicht()
    {
        using var _ = new Kulturvorrichtung();
        var probe = new Klimaprobe { Grund = "Probegrund" };
        var cut = Aufbauen(zeilen: GespeicherteZeilen());
        cut.Render(p => p.Add(x => x.ModellpruefungGaben, _ => new ModellpruefungProbeweg().Weg()).Add(x => x.KlimaregionGaben, probe.Weg));
        Pruefknopf(cut)!.Click();
        cut.Find("button.epos-modellpruefung-klima").Click();

        // Unverändert: OK schließt nur.
        cut.Find("button.epos-klimawahl-uebernehmen").Click();
        Assert.False(cut.Instance.KlimaregionOffen);
        Assert.Empty(probe.Gespeichert);

        cut.Find("button.epos-modellpruefung-klima").Click();
        await Waehlen(cut, 2);
        cut.Find("button.epos-klimawahl-uebernehmen").Click();
        Assert.True(cut.Instance.KlimaregionOffen);
        Assert.Equal("Probegrund", cut.Find(".epos-klimawahl-meldung").TextContent);
        cut.Find("button.epos-klimawahl-abbrechen").Click();
        Assert.False(cut.Instance.KlimaregionOffen);
        Assert.Empty(probe.Gespeichert);
    }

    [Fact]
    public void N5_Ohne_Delegat_nennt_der_Knopf_den_Ort_der_Wahl()
    {
        using var _ = new Kulturvorrichtung();
        var cut = MitModellpruefung(GespeicherteZeilen(), new List<string>());
        Pruefknopf(cut)!.Click();
        cut.Find("button.epos-modellpruefung-klima").Click();
        Assert.False(cut.Instance.KlimaregionOffen);
        Assert.Contains(R.MPB_HINWEIS_KLIMA, cut.Markup);
    }

    /// <summary>
    /// Steht der Editor unter dem Ampelblatt, bekommt er nach der Wahl den Konditionierungsweg mit dem Raster der neuen Region
    /// (Wochentag des 1. Januar) — die übrigen Gaben bleiben dieselben.
    /// </summary>
    [Fact]
    public async Task N5_Editor_bekommt_das_Raster_der_neuen_Region()
    {
        using var _ = new Kulturvorrichtung();
        var probe = new Klimaprobe();
        int gelesen = 0;
        var cut = MitModellpruefung(GespeicherteZeilen(), new List<string>());
        cut.Render(p => p
            .Add(x => x.KlimaregionGaben, probe.Weg)
            .Add(x => x.ProjektGaben, _ =>
            {
                gelesen++;
                return new Dictionary<string, object>
                {
                    ["StartReiter"] = GebaeudeKatalogDialog.REITER_ZONEN,
                    ["Konditionierung"] = new KonditionierungWeg { WochentagJan1 = gelesen },
                };
            }));
        cut.FindAll("td.epos-spalte-stift button").First().Click();
        GebaeudeKatalogDialog editor = cut.FindComponent<GebaeudeKatalogDialog>().Instance;
        Assert.Equal(1, editor.Konditionierungsbearbeitung.Weg.WochentagJan1);
        string schluessel = editor.Konditionierungsbearbeitung.Vorschauschluessel(KonditionierungGroesse.Heizen);

        cut.Find(".epos-pruefleiste-gruppe[data-gruppe='4']").Click();
        cut.Find("button.epos-modellpruefung-klima").Click();
        await Waehlen(cut, 2);
        cut.Find("button.epos-klimawahl-uebernehmen").Click();

        Assert.Equal(2, gelesen);
        Assert.Equal(2, editor.Konditionierungsbearbeitung.Weg.WochentagJan1);
        Assert.NotEqual(schluessel, editor.Konditionierungsbearbeitung.Vorschauschluessel(KonditionierungGroesse.Heizen));
    }
}
