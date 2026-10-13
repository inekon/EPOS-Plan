using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// MA-5: der Sprung aus dem Blatt „Modell prüfen“ in die Gebäudeansicht mit Hervorhebung der Gruppe und die Vorschläge an der
/// Legende — die Übernahme läuft über den Weg des Blatts, die Ansicht liest neu, das Ampelblatt prüft neu.
/// </summary>
public partial class GebaeudeDialogTests
{
    private sealed class Ansichtprobe
    {
        public int Neugelesen, Uebernommen, Verworfen;
        public bool Erledigt;
        public readonly List<ModellpruefungAuswahl> Auswahlen = new();

        public ModellpruefungStand Stand()
        {
            ModellpruefungGruppenzeile G(Pruefgruppe g, Pruefsprungziel z, params ModellpruefungBefundzeile[] b)
                => new(g, "Gruppe " + (int)g, b.Length > 0 ? Pruefampel.Gelb : Pruefampel.Gruen, b.Length, b.Count(x => x.HatVorschlag), z,
                       Erledigt && g == Pruefgruppe.AufbautenStoffwerte, b);
            ModellpruefungBefundzeile[] aufbau = Erledigt
                ? Array.Empty<ModellpruefungBefundzeile>()
                : new[] { new ModellpruefungBefundzeile("Nur U-Wert", Pruefampel.Gelb, "Zone 1 — Boden", "Aufbau Boden", 501, 42) };
            return new ModellpruefungStand(Rechenstatus.RechnetMitVorgaben, "rechnet mit Vorgaben", new[]
            {
                G(Pruefgruppe.GeometrieBauteile, Pruefsprungziel.Randbedingung),
                G(Pruefgruppe.AufbautenStoffwerte, Pruefsprungziel.Aufbau, aufbau),
                G(Pruefgruppe.NutzungKonditionierung, Pruefsprungziel.Zonen,
                  new ModellpruefungBefundzeile("Zone ohne Profil", Pruefampel.Gelb, "Zone 1", "Profil Büro", 501, null)),
                G(Pruefgruppe.OrtLage, Pruefsprungziel.Befund),
                G(Pruefgruppe.Rechenfaehigkeit, Pruefsprungziel.Befund),
            });
        }

        public ModellpruefungWeg Weg() => new()
        {
            Gebaeudename = "Importhaus",
            Pruefen = Stand,
            UebernehmenAuswahl = a => { Uebernommen++; Auswahlen.Add(a); Erledigt = true; return new ModellpruefungAusgang(true, "1 übernommen", Stand()); },
            VerwerfenAuswahl = a => { Verworfen++; Auswahlen.Add(a); return new ModellpruefungAusgang(true, "1 verworfen", Stand()); },
        };

        public GebaeudeAnsichtDaten Daten()
        {
            var datei = new GebaeudeAnsichtDateikoerper(new float[] { 0, 0, 0, 6, 0, 0, 6, 8, 0, 0, 8, 0 }, new[] { 0, 1, 2, 0, 2, 3 },
                                                        new[] { 0, 1, 1, 2, 2, 3, 0, 3 }, 2, "FacetedBrep", Array.Empty<string>())
            { Gruppen = new byte[] { 1, 3 } };
            return new GebaeudeAnsichtDaten
            {
                Geschosse = new[]
                {
                    new GebaeudeAnsichtGeschoss("g-eg", "Erdgeschoss", false, 0, 0, 6, 8, new[]
                    {
                        new GebaeudeAnsichtRaum("r-a", "Raum A", "z1", true, false, "48 m²",
                            new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)new[] { new GebaeudeAnsichtPunkt(0, 0), new GebaeudeAnsichtPunkt(6, 0), new GebaeudeAnsichtPunkt(6, 8), new GebaeudeAnsichtPunkt(0, 8) } }),
                    }),
                },
                Zonen = new[] { new GebaeudeAnsichtZone("z1", "Zone 1", 0, true, false, false) },
                Koerperraeume = new[]
                {
                    new GebaeudeAnsichtKoerperraum("r-a", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null)
                    {
                        Dateikoerper = datei, Herkunft = Koerperherkunft.Datei, Dreiecksbauteile = new string?[] { "aw-1", "bp-1" },
                    },
                },
                Flaechengruppen = Enumerable.Range(0, GebaeudeAnsichtRandgruppen.ZAHL)
                    .Select(i => new GebaeudeAnsichtFlaechengruppe((Randgruppe)i, i is 1 or 3 ? 48 : 0, i is 1 or 3 ? 1 : 0)).ToList(),
                Bauteilstufen = new Dictionary<string, Aufbaustufe> { ["aw-1"] = Aufbaustufe.A, ["bp-1"] = Erledigt ? Aufbaustufe.A : Aufbaustufe.C },
                Aufbausummen = new[] { new GebaeudeAnsichtAufbausumme(Aufbaustufe.A, 1, 12, 0, 0), new GebaeudeAnsichtAufbausumme(Aufbaustufe.C, 1, 48, 0, 0) },
                BauteilIds = new Dictionary<string, int> { ["aw-1"] = 41, ["bp-1"] = 42 },
                ZonenIds = new Dictionary<string, int> { ["z1"] = 501 },
                Nutzungen = new Dictionary<string, GebaeudeAnsichtNutzung> { ["z1"] = new GebaeudeAnsichtNutzung("", "") },
            };
        }
    }

    private IRenderedComponent<GebaeudeDialog> MitAnsichtprobe(Ansichtprobe p)
    {
        var zeilen = new List<GebaeudeProjektZeile>
        {
            new() { IdZ = 1, IdGebaeude = 7, IdKatalog = 42, Name = "Importhaus", HatProjektkopie = true, Wohnflaeche = 150 },
        };
        return Render<GebaeudeDialog>(c => c
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => Array.Empty<Katalogfilterzeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.KatalogGaben, _ => new Dictionary<string, object>())
            .Add(x => x.Importquelle, _ => new GebaeudeImportquelleAngabe("haus.ifc", "IFC", "05.10.2026 10:00", "Importquelle: haus.ifc"))
            .Add(x => x.DateiNeuLesen, _ => Task.FromResult<GebaeudeNeulesestand?>(new GebaeudeNeulesestand
            {
                Zustand = GebaeudeNeulesezustand.Passend, Hinweis = "passt", Ansicht = p.Daten(),
                AnsichtNeuLesen = () => { p.Neugelesen++; return p.Daten(); },
            }))
            .Add(x => x.ModellpruefungGaben, _ => p.Weg()));
    }

    private static IElement Ansichtknopf(IRenderedComponent<GebaeudeDialog> cut, Pruefgruppe g)
        => cut.FindAll(".epos-modellpruefung-gruppe")[(int)g - 1].QuerySelector(".epos-modellpruefung-ansicht")!;

    [Fact]
    public void MA5_Ansicht_aus_der_Ampel_hebt_die_Gruppe_hervor_und_uebernimmt_an_der_Legende()
    {
        var p = new Ansichtprobe();
        var cut = MitAnsichtprobe(p);
        Pruefknopf(cut)!.Click();
        Ansichtknopf(cut, Pruefgruppe.AufbautenStoffwerte).Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.AnsichtPruefung));
        Assert.Equal(Pruefgruppe.AufbautenStoffwerte, cut.Instance.AnsichtPruefung!.Gruppe);
        IRenderedComponent<GebaeudeAnsicht> ansicht = cut.FindComponent<GebaeudeAnsicht>();
        Assert.Equal("aufbau", ansicht.Instance.Farbmodus);
        Assert.Equal("1", cut.Find(".epos-gebansicht-pruefzeile").GetAttribute("data-bauteile"));
        Assert.Contains("epos-gebansicht-raum--geprueft", cut.Find("g[data-raum='r-a']").ClassName);

        cut.Find(".epos-gebansicht-aufbaueintrag[data-stufe='" + GebaeudeAnsichtAufbaustufen.Schluessel(Aufbaustufe.C) + "'] .epos-gebansicht-uebernehmen").Click();
        Assert.Equal(1, p.Uebernommen);
        Assert.Equal(new[] { 42 }, Assert.Single(p.Auswahlen).Bauteile);
        Assert.Equal(1, p.Neugelesen);                                        // die Ansicht zeichnet neu
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-gebansicht-uebernehmen")));
        Assert.Empty(cut.Instance.AnsichtPruefung!.Befunde);
        Assert.NotEmpty(cut.FindAll(".epos-modellpruefung-zuruecknehmen"));  // das Ampelblatt darunter kennt die Rücknahme
    }

    [Fact]
    public void MA5_Gruppe_3_oeffnet_den_Farbmodus_Nutzung_und_verwirft_an_der_Legende()
    {
        var p = new Ansichtprobe();
        var cut = MitAnsichtprobe(p);
        Pruefknopf(cut)!.Click();
        Ansichtknopf(cut, Pruefgruppe.NutzungKonditionierung).Click();
        cut.WaitForAssertion(() => Assert.Equal("nutzung", cut.FindComponent<GebaeudeAnsicht>().Instance.Farbmodus));
        Assert.Equal("1", cut.Find(".epos-gebansicht-pruefzeile").GetAttribute("data-zonen"));
        cut.Find(".epos-gebansicht-nutzungseintrag--fehlt .epos-gebansicht-verwerfen").Click();
        Assert.Equal(1, p.Verworfen);
        Assert.Equal(new[] { 501 }, Assert.Single(p.Auswahlen).Zonen);
    }
}
