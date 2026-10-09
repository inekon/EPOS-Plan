using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Aus dem Projekt entfernen“ und Abbrechen</b> (Anwenderwunsch 08.10.2026): Die Erzeugerdialoge mit eigener
/// Projektkopie (Heizkessel, BHKW, Solarkollektoren) merken das Entfernen nur vor; gelöscht wird die Kopie erst
/// mit OK, Abbrechen räumt nur die in dieser Sitzung neu angelegten Kopien ab. Geprüft am Heizkesseldialog mit
/// einer Hülle nach dem Muster von <c>HeizkesselHuelle.Oeffnen</c> (Zeilenliste als Anlagenliste, Löschweg als
/// Mitschrift) und an der Vormerkung selbst.
/// </summary>
public class ProjektkopievormerkungTests : EposBunitContext
{
    public ProjektkopievormerkungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel, s => s);

    private static ErzeugerZeile Zeile(int schluessel, string name, int kopieId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = kopieId, CarrierId = 5, Vorlauf = 70, Ruecklauf = 50 };

    /// <summary>Die Hülle im Kleinen: Anlagenliste, Löschmitschrift, Vormerkung, Abschluss wie <c>Oeffnen</c>.</summary>
    private sealed class Huelle
    {
        public readonly List<ErzeugerZeile> Anlagen;
        public readonly List<string> Geloescht = new();
        public readonly Projektkopievormerkung Vormerkung;
        public bool? Ergebnis;

        public Huelle(params ErzeugerZeile[] anlagen)
        {
            Anlagen = anlagen.ToList();
            Vormerkung = new Projektkopievormerkung(name => Geloescht.Add(name));
        }

        public void Entfernen(ErzeugerZeile z) => Vormerkung.Entfernt(z.Bezeichner, z.GeraetId);

        public void Geschlossen(bool ok)
        {
            Ergebnis = ok;
            Vormerkung.Abschliessen(ok, id => Anlagen.Any(a => a.GeraetId == id));
        }
    }

    private IRenderedComponent<HeizkesselDialog> Aufbauen(Huelle h)
        => Render<HeizkesselDialog>(p => p
            .Add(x => x.Zeilen, h.Anlagen)
            .Add(x => x.Katalogprofil, Profil)
            .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante") })
            .Add(x => x.Entfernen, h.Entfernen)
            .Add(x => x.Geschlossen, h.Geschlossen));

    private static void Entfernen(IRenderedComponent<HeizkesselDialog> cut)
        => cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

    private static void Abschluss(IRenderedComponent<HeizkesselDialog> cut, string text)
        => cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == text).Click();

    [Fact]
    public void Entfernen_und_Abbrechen_laesst_die_Projektkopie_stehen()
    {
        var h = new Huelle(Zeile(1, "Kessel A", 100));
        var cut = Aufbauen(h);

        Entfernen(cut);
        Assert.Empty(h.Anlagen);          // im Dialog ist die Zeile weg ...
        Assert.Empty(h.Geloescht);        // ... die Kopie aber nicht

        Abschluss(cut, "Abbrechen");
        Assert.False(h.Ergebnis);
        Assert.Empty(h.Geloescht);
    }

    [Fact]
    public void Entfernen_und_OK_loescht_die_Projektkopie()
    {
        var h = new Huelle(Zeile(1, "Kessel A", 100), Zeile(2, "Kessel B", 200));
        var cut = Aufbauen(h);

        Entfernen(cut);
        Assert.Empty(h.Geloescht);

        Abschluss(cut, "OK");
        Assert.True(h.Ergebnis);
        Assert.Equal(new[] { "Kessel A" }, h.Geloescht);
    }

    [Fact]
    public void Eine_noch_verwiesene_Kopie_bleibt_auch_beim_OK()
    {
        // Zwei Zeilen desselben Kessels teilen sich EINE Kopie.
        var h = new Huelle(Zeile(1, "Kessel A", 100), Zeile(2, "Kessel A", 100));
        var cut = Aufbauen(h);

        Entfernen(cut);
        Abschluss(cut, "OK");

        Assert.Single(h.Anlagen);
        Assert.Empty(h.Geloescht);
    }

    [Fact]
    public void Uebernehmen_und_Abbrechen_raeumt_die_neue_Kopie_ab()
    {
        // „In das Projekt übernehmen" schreibt die Kopie sofort (die Hülle meldet sie als neu angelegt);
        // eine wiederverwendete, schon vorhandene Kopie meldet sie nicht.
        var h = new Huelle(Zeile(1, "Kessel A", 100));
        h.Vormerkung.Angelegt("Kessel B", 200);
        h.Anlagen.Add(Zeile(2, "Kessel B", 200));
        var cut = Aufbauen(h);

        Abschluss(cut, "Abbrechen");

        Assert.Equal(new[] { "Kessel B" }, h.Geloescht);
    }

    [Fact]
    public void Uebernehmen_und_OK_laesst_die_neue_Kopie_stehen()
    {
        var h = new Huelle(Zeile(1, "Kessel A", 100));
        h.Vormerkung.Angelegt("Kessel B", 200);
        h.Anlagen.Add(Zeile(2, "Kessel B", 200));
        var cut = Aufbauen(h);

        Abschluss(cut, "OK");

        Assert.Empty(h.Geloescht);
    }

    [Fact]
    public void Uebernommen_und_wieder_entfernt_geht_beim_OK_einmal()
    {
        var geloescht = new List<string>();
        var v = new Projektkopievormerkung(geloescht.Add);
        v.Angelegt("Kessel B", 200);
        v.Entfernt("Kessel B", 200);

        v.Abschliessen(true, _ => false);

        Assert.Equal(new[] { "Kessel B" }, geloescht);
        Assert.Empty(v.Entfernte);
        Assert.Empty(v.Angelegte);
    }

    [Fact]
    public void Abbrechen_verwirft_die_Vormerkung_ohne_zu_loeschen()
    {
        var geloescht = new List<string>();
        var v = new Projektkopievormerkung(geloescht.Add);
        v.Entfernt("Kessel A", 100);

        v.Abschliessen(false, _ => false);

        Assert.Empty(geloescht);
        Assert.Empty(v.Entfernte);
    }

    // =====================================================================
    //  Trägervarianten (Nachzug zu A5)
    // =====================================================================

    [Fact]
    public void Abbrechen_nimmt_eine_neu_angelegte_Traegervariante_zurueck()
    {
        var geloescht = new List<string>();
        var zurueck = new List<(int, bool, bool)>();
        var v = new Projektkopievormerkung(geloescht.Add, (c, z, k) => zurueck.Add((c, z, k)));
        v.Angelegt("Kessel A", 100);
        v.TraegerAngelegt(77, zuordnungNeu: true, katalogNeu: true);

        v.Abschliessen(false, _ => false);

        Assert.Equal(new[] { "Kessel A" }, geloescht);
        Assert.Equal(new[] { (77, true, true) }, zurueck);
        Assert.Empty(v.AngelegteTraeger);
    }

    [Fact]
    public void OK_laesst_die_neu_angelegte_Traegervariante_stehen()
    {
        var zurueck = new List<(int, bool, bool)>();
        var v = new Projektkopievormerkung(_ => { }, (c, z, k) => zurueck.Add((c, z, k)));
        v.TraegerAngelegt(77, zuordnungNeu: true, katalogNeu: true);

        v.Abschliessen(true, _ => true);

        Assert.Empty(zurueck);
        Assert.Empty(v.AngelegteTraeger);
    }

    [Fact]
    public void Ein_schon_zugeordneter_Traeger_wird_nicht_vorgemerkt_und_ein_doppelter_einmal_zurueckgenommen()
    {
        var zurueck = new List<(int, bool, bool)>();
        var v = new Projektkopievormerkung(_ => { }, (c, z, k) => zurueck.Add((c, z, k)));
        v.TraegerAngelegt(5, zuordnungNeu: false, katalogNeu: false);   // „bereits zugeordnet“
        v.TraegerAngelegt(77, zuordnungNeu: true, katalogNeu: true);
        v.TraegerAngelegt(77, zuordnungNeu: false, katalogNeu: false);  // zweiter Kessel, selbe Variante
        v.TraegerAngelegt(80, zuordnungNeu: true, katalogNeu: false);   // vorhandener Katalogträger, neue Zuordnung

        Assert.Equal(2, v.AngelegteTraeger.Count);
        v.Abschliessen(false, _ => false);

        Assert.Equal(new[] { (80, true, false), (77, true, true) }, zurueck);
    }

    [Fact]
    public void Ohne_Traegerweg_bleibt_Abbrechen_bei_den_Kopien()
    {
        var geloescht = new List<string>();
        var v = new Projektkopievormerkung(geloescht.Add);
        v.TraegerAngelegt(77, true, true);
        v.Angelegt("Kollektor A", 9);

        v.Abschliessen(false, _ => false);

        Assert.Equal(new[] { "Kollektor A" }, geloescht);
    }
}
