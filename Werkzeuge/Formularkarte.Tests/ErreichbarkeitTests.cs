using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Der Erreichbarkeitsgraph gegen den echten Bestand.
///
/// <para>
/// Anlass ist der Befund vom 03.09.2026: Die Feldkarte wies fuer
/// <c>Form_Kosten_Auswahl</c> einen Aufrufer aus (<c>Form_Kosten</c>), und daran
/// wurde der erste Blazor-Dialog gehaengt - <c>Form_Kosten</c> selbst hatte aber
/// seit KD6a keinen Einstieg mehr. Mit iU9-W0 ist diese Kette abgetragen
/// (Anwenderentscheid iF29): Der Bestand fuehrt seither KEINE unerreichbare und
/// keine verwaiste Maske mehr. Die Tests halten beides fest - den abgetragenen
/// Zustand am echten Bestand und die Mechanik "Oeffner ohne Wurzel = nein" am
/// eingefrorenen Pruefmuster.
/// </para>
/// </summary>
public sealed class ErreichbarkeitTests
{
    /// <summary>Der Projektbaum, in dem die Masken des Bestands liegen.</summary>
    private static string Projekt => Repowurzel.Datei("WindowsFormsApplication1");

    private static Erreichbarkeitsgraph Graph => Erreichbarkeit.Bauen(Projekt);

    private static Maskenknoten Knoten(string klasse)
    {
        var knoten = Graph.Fuer(klasse);
        Assert.True(knoten is not null, "Der Graph kennt die Maske " + klasse + " nicht.");
        return knoten!;
    }

    // ==================================================================
    //  Die beiden Wurzeln
    // ==================================================================

    [Theory]
    [InlineData("MDIMainForm")]
    [InlineData("Form_Start")]
    public void DieBeidenEinstiegeSindWurzeln(string klasse)
    {
        var knoten = Knoten(klasse);

        Assert.True(knoten.Wurzel, klasse + " gilt nicht als Wurzel.");
        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.Equal(klasse, knoten.Pfad);
    }

    // ==================================================================
    //  Der Befund, der das Werkzeug ausgeloest hat - abgetragen mit iU9-W0
    // ==================================================================

    [Fact]
    public void DieStillgelegtenMaskenGibtEsNichtMehr()
    {
        // Anwenderentscheid iF29: Form_Kosten, sein Kostenfaktordialog, seine
        // Positionszeile, die Variantenprobe, die Kurzansicht der Simulation und die
        // KWKG-Modulmaske sind geloescht statt umgestellt. Der Graph darf sie nicht
        // mehr kennen - sonst lebt eine Fassung wieder.
        foreach (var klasse in new[] { "Form_Kosten", "Form_KostenfaktorItem", "ucKostenZeile",
                                       "Form_Variantentest", "Form_Simulation_Kurz", "Form_KwkgModule" })
        {
            Assert.True(Graph.Fuer(klasse) is null, "Der Graph kennt " + klasse + " noch.");
        }
    }

    [Fact]
    public void KeineMaskeDesBestandsIstMehrUnerreichbarOderVerwaist()
    {
        // Das ist die Abnahme von iU9-W0: Was keinen Weg hatte, ist entweder
        // umgestellt oder geloescht. Bleibt nur "ja" und das begruendete "unklar".
        //
        // Gezaehlt werden die MASKEN des Stapellaufs, nicht jede Klasse des Graphen:
        // Basisklassen ohne Designer (BaseForm) sind keine Masken und stehen dort
        // dauerhaft auf "verwaist", ohne dass jemand sie oeffnen sollte.
        var offen = Stapel.Laufen(Projekt, ziel: null).Zeilen
            .Where(z => z.Erreichbarkeit is not null &&
                        z.Erreichbarkeit.Status is Erreichbar.Nein or Erreichbar.Verwaist)
            .Select(z => z.Bezeichner + " (" + z.Erreichbarkeit!.StatusText + ")")
            .ToArray();

        Assert.True(offen.Length == 0, "Ohne Weg: " + string.Join(", ", offen));
    }

    [Fact]
    public void DerGrundStehtSoAuchImKopfDerFeldkarte()
    {
        // Am eingefrorenen Pruefmuster: Form_KostenfaktorItem hat genau einen Oeffner
        // (Form_Kosten.Auszug.cs), aber keine Wurzel darueber - die Karte sagt das im
        // Kopf, statt einen Weg zu behaupten.
        Erreichbarkeit.Vergessen();
        try
        {
            var maske = Kartenbau.Vollstaendig(Repowurzel.Pruefmuster("Kosten/Form_KostenfaktorItem.Designer.cs"),
                                               null, Repowurzel.PruefmusterWurzel);
            var karte = FeldkarteSchreiber.Schreiben(maske);

            Assert.Contains("| Öffner erreichbar | nein", karte, StringComparison.Ordinal);
            Assert.Contains("Form_Kosten.AddKostenItem", karte, StringComparison.Ordinal);
        }
        finally
        {
            Erreichbarkeit.Vergessen();
        }
    }

    [Fact]
    public void WasAmEinstiegslosenFormKostenHingIstEbenfallsUnerreichbar()
    {
        // Die Karte nannte fuer Form_KostenfaktorItem einen Aufrufer - der hing aber
        // selbst an Form_Kosten. Genau diese Kette hat iU8-9 uebersehen; sie wird am
        // Pruefmuster weiter geprueft, nachdem der Bestand sie mit iU9-W0 los ist.
        Erreichbarkeit.Vergessen();
        try
        {
            var maske = Kartenbau.Vollstaendig(Repowurzel.Pruefmuster("Kosten/Form_KostenfaktorItem.Designer.cs"),
                                               null, Repowurzel.PruefmusterWurzel);
            var befund = maske.Erreichbarkeit;

            Assert.NotNull(befund);
            Assert.Equal(Erreichbar.Nein, befund!.Status);
            Assert.All(befund.Oeffner, o => Assert.Contains("Form_Kosten.", o, StringComparison.Ordinal));
        }
        finally
        {
            Erreichbarkeit.Vergessen();
        }
    }

    // ==================================================================
    //  Masken mit Weg
    // ==================================================================

    /// <summary>
    /// iU9-W4: Die beiden Kostenmasken, an denen dieser Test bis Welle 3 hing
    /// (Form_KostenKomponente, Form_Energietraeger), sind umgestellt und
    /// geloescht. Ihre Nachfolge sind Huellen, keine Formulare — der Graph
    /// kennt sie deshalb zu Recht nicht mehr. Der Test dreht sich um: Er
    /// sichert, dass die Umstellung haelt.
    /// </summary>
    [Fact]
    public void DieUmgestelltenKostenmaskenStehenNichtMehrImGraphen()
    {
        foreach (var klasse in new[] { "Form_KostenKomponente", "Form_Energietraeger",
                                       "ucFuelSettings", "ucVorlagenZeile", "ucErtragBonus",
                                       "ucStromAufschlaege", "ucBrennstoffBestandteile" })
        {
            Assert.True(Graph.Fuer(klasse) is null, "Der Graph kennt " + klasse + " noch.");
        }
    }

    [Fact]
    public void DasHauptfensterIstDieWurzelUndDamitErreichbar()
    {
        // Die Zeugenkette: Form_Heizkessel -> Form_Gebaeude ->
        // Form_Stromganglinie (bis W12) -> Form_AdminSettings (bis W14c) ->
        // MDIMainForm. Jeder Vorgaenger ist mit seiner Welle geloescht (Regel M1).
        //
        // iU9-W14c.9: MDIMainForm ist die WURZEL des Erreichbarkeitsgraphen -
        // Pfadlaenge 1, und sie faellt als ALLERLETZTE Maske ueberhaupt (Welle 16).
        // Der Anker kann damit nicht mehr unerreichbar werden und muss nicht noch
        // einmal umziehen. Form_ProjektSpeichernUnter waere der zweitbeste
        // gewesen - sie faellt schon mit W15a und traegt seit W14a den
        // Maskenschluessel-Zeugen; zwei Anker auf einer Maske sind unnoetig.
        var knoten = Knoten("MDIMainForm");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.Equal("MDIMainForm", knoten.Pfad);
    }

    [Fact]
    public void DerAssistentZiehtSeineDreizehnSeitenMit()
    {
        // Die Seiten stehen als Erzeugerliste in einem statischen Feld von
        // AssistentSeiten; wer nur Methodenrumpfe liest, findet sie nicht.
        foreach (var klasse in new[] { "Wizard_Komponenten", "Wizard_Projekt", "Wizard_Stromlastgang" })
        {
            Assert.Equal(Erreichbar.Ja, Knoten(klasse).Status);
        }
    }

    [Fact]
    public void DieSprungtabelleLoestDieMaskenschluesselAuf()
    {
        // Die Kette der Zeugen: Bis iU9-W7.10 stand hier Form_WP
        // (Masken.WpAdministration, mit W7.3 geloescht), danach bis iU9-W14a.7
        // Form_AdminStromspeicher (Masken.StromspeicherAdmin, mit W14a.3 geloescht).
        //
        // Form_ProjektSpeichernUnter wird ebenso NUR ueber die Sprungtabelle
        // geoeffnet - Masken.ProjektSpeichernUnter, vom MDI-Menue ueber MenueCtrl und
        // WinFormsNavigation - und kommt erst in Welle 15a an die Reihe. Von den fuenf
        // Maskenschluesseln, hinter denen nach W14 noch eine WinForms-Maske steht, ist
        // sie der kuerzeste Weg (Vermessung W14 § 14.1).
        var knoten = Knoten("Form_ProjektSpeichernUnter");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.Contains("Masken.ProjektSpeichernUnter", knoten.Pfad, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Unklar
    // ==================================================================

    /// <summary>
    /// Ein dauerhaft gesperrter Knopf macht den Weg dahinter „unklar", nicht „ja" —
    /// gegen das eingefrorene PRUEFMUSTER.
    /// </summary>
    /// <remarks>
    /// <para>Bis iU9-W14a.7 lief dieser Fall gegen den echten Bestand:
    /// <c>Form_PufferSp_Admin</c> schaltete <c>btn_Neu</c> und <c>btn_Bearbeiten</c> in
    /// einem Zweig ab und nie wieder ein, und ihr Kind <c>Form_PufferSp_Bearbeiten</c>
    /// war deshalb die EINE „unklar"-Maske des Programms. Mit W14a sind beide Razor;
    /// der Erreichbarkeitsbefund zaehlt seither 0 nein / 0 verwaist / 0 unklar.</para>
    /// <para>Damit die REGEL prueffbar bleibt, liegen beide Masken eingefroren unter
    /// <c>Pruefmuster/Pufferspeicher/</c> — samt einer Wurzel
    /// (<c>MDIMainForm.Auszug.cs</c>), ohne die jede Maske dort „nein" waere. Dasselbe
    /// Vorgehen wie bei den Ankern der Wellen 2, 4 und 7.</para>
    /// </remarks>
    [Fact]
    public void EinDauerhaftGesperrterKnopfMachtDenWegUnklarStattJa()
    {
        Erreichbarkeit.Vergessen();
        try
        {
            var graph = Erreichbarkeit.Bauen(Repowurzel.PruefmusterWurzel);

            var admin = graph.Fuer("Form_PufferSp_Admin");
            Assert.True(admin is not null, "Das Pruefmuster kennt Form_PufferSp_Admin nicht.");
            Assert.Equal(Erreichbar.Ja, admin!.Status);

            var knoten = graph.Fuer("Form_PufferSp_Bearbeiten");
            Assert.True(knoten is not null, "Das Pruefmuster kennt Form_PufferSp_Bearbeiten nicht.");

            Assert.Equal(Erreichbar.Unklar, knoten!.Status);
            Assert.Contains(knoten.Oeffner, o => o.Contains("zweifelhaft", StringComparison.Ordinal));
        }
        finally
        {
            Erreichbarkeit.Vergessen();
        }
    }

    /// <summary>
    /// Im laufenden Bestand gibt es nach iU9-W14a KEINEN „unklar"-Zustand mehr — und
    /// auch kein „nein" und kein „verwaist". Das ist der Meilenstein der Welle.
    /// </summary>
    [Fact]
    public void DerBestandFuehrtKeineUngeklaerteMaskeMehr()
    {
        var ergebnis = Stapel.Laufen(Projekt, ziel: null);

        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Unklar));
        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Nein));
        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Verwaist));
        Assert.Equal(ergebnis.Masken, ergebnis.Erreichbar(Erreichbar.Ja));
    }

    // ==================================================================
    //  Das Pruefmuster
    // ==================================================================

    [Fact]
    public void DasPruefmusterIstUnerreichbarUndSagtWarum()
    {
        Erreichbarkeit.Vergessen();
        try
        {
            var maske = Kartenbau.Vollstaendig(Repowurzel.Pruefmuster("Kosten/Form_Kosten_Auswahl.Designer.cs"),
                                               null, Repowurzel.PruefmusterWurzel);
            var befund = maske.Erreichbarkeit;
            Assert.NotNull(befund);

            // Im eingefrorenen Muster gibt es einen Oeffner (Form_Kosten.Auszug.cs),
            // aber keine Wurzel - "nein". Der eigentliche Grund steht daneben.
            Assert.Equal(Erreichbar.Nein, befund!.Status);
            Assert.Contains("Form_Kosten.CreateNewEnergyCarrier", string.Join(" ", befund.Oeffner), StringComparison.Ordinal);
            Assert.Contains(befund.Hinweise, h => h.Contains("gelöscht", StringComparison.Ordinal) &&
                                                  h.Contains("Blazor-Nachfolge", StringComparison.Ordinal));
        }
        finally
        {
            Erreichbarkeit.Vergessen();
        }
    }

    // ==================================================================
    //  Die Zaehlung im Stapellauf
    // ==================================================================

    [Fact]
    public void DerStapellaufZaehltUndSpaltetDieZustaendeAuf()
    {
        var ergebnis = Stapel.Laufen(Projekt, ziel: null);

        Assert.True(ergebnis.MitErreichbarkeit, "Der Stapellauf hat die Erreichbarkeit nicht gerechnet.");
        Assert.Equal(ergebnis.Masken,
                     ergebnis.Erreichbar(Erreichbar.Ja) + ergebnis.Erreichbar(Erreichbar.Nein) +
                     ergebnis.Erreichbar(Erreichbar.Verwaist) + ergebnis.Erreichbar(Erreichbar.Unklar));
        // Gemessener Stand nach Welle 10a: 49 von 50 (54 von 55 nach W9, 61 von
        // 63 nach W8, 71 von 73 nach W7, 79 von 81 nach W6, 86 von 88 nach W5,
        // 89 von 91 nach iU9-W4, 96 von 98 nach iU9-W3) - die eine uebrige ist
        // "unklar". Nach Welle 10b: 48 von 49, nach Welle 11b: 42 von 43, nach
        // Welle 12: 37 von 38, nach Welle 13: 31 von 32, nach Welle 14b: 27 von
        // 28. Mit Welle 14a faellt die letzte "unklar"-Maske: nach BEIDEN Wellen
        // sind es 21 von 21 - ALLE erreichbar. Nach Welle 14c: 17 von 17. Die Zahl
        // sinkt mit jeder Welle, der Anteil steht seit W14a auf 100 %.
        Assert.True(ergebnis.Erreichbar(Erreichbar.Ja) >= 17,
                    "Nur " + ergebnis.Erreichbar(Erreichbar.Ja) + " Masken gelten als erreichbar.");

        var uebersicht = Stapel.Uebersicht(ergebnis, Projekt);
        Assert.Contains("| Öffner erreichbar | ", uebersicht, StringComparison.Ordinal);
        Assert.Contains("Öffner erreichbar | Datei |", uebersicht, StringComparison.Ordinal);

        // Seit iU9-W0 ist die Zaehlung von "nein" und "verwaist" leer.
        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Nein));
        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Verwaist));

        var befund = Stapel.Erreichbarkeitsbefund(ergebnis, Projekt);
        Assert.Contains("# Öffner erreichbar — Befund aller Masken", befund, StringComparison.Ordinal);
        // iU9-W9: Form_GebWohnflaeche war die erste der beiden "unklar"-Masken und ist
        // mit W9.3 geloescht; die zweite - Form_PufferSp_Bearbeiten - faellt mit
        // W14a.1. Seither steht KEINE Maske mehr auf "unklar"; die Regel selbst prueft
        // EinDauerhaftGesperrterKnopfMachtDenWegUnklarStattJa am Pruefmuster.
        Assert.Contains("| unklar | 0 |", befund, StringComparison.Ordinal);

        // iU9-W14c.9: Bis dahin stand hier Form_AdminSettings (davor
        // Form_Stromganglinie); beide sind mit ihrer Welle gefallen. MDIMainForm
        // ist die Wurzel und faellt als allerletzte.
        Assert.Contains("| MDIMainForm | ja |", befund, StringComparison.Ordinal);
        Assert.Contains("| gesamt | " + ergebnis.Masken + " | |", befund, StringComparison.Ordinal);
    }

    [Fact]
    public void OhneSchalterWirdDieErreichbarkeitNichtGerechnet()
    {
        // iU9-W14c.9: Der ORDNER Views/Klimadaten ist mit dieser Welle leer und
        // geloescht; die Maske liegt als Pruefmuster. Der Fall braucht nur einen
        // Ordner mit wenigen Masken - was er prueft, ist der SCHALTER.
        var ergebnis = Stapel.Laufen(Repowurzel.Pruefmuster("Klimadaten"), ziel: null,
                                     suchwurzel: Projekt, erreichbarkeit: false);

        Assert.False(ergebnis.MitErreichbarkeit);
        Assert.All(ergebnis.Zeilen, z => Assert.Null(z.Erreichbarkeit));
    }
}
