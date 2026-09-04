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
    public void FormAdminSettingsIstUeberDasHauptfensterZuErreichen()
    {
        // iU9-W12: Bis dahin stand hier Form_Stromganglinie (davor Form_Gebaeude,
        // davor Form_Heizkessel); alle drei sind mit ihrer Welle geloescht
        // (Regel M1). Der Anker kann seine Form ("ueber die STARTSEITE") nicht
        // behalten: Von den zwoelf Masken, deren Pfad mit Form_Start beginnt,
        // faellt keine erst in W13 oder W14 - alle W13/W14-Masken haengen am Menue
        // des MDIMainForm (Befund W12-B26).
        //
        // Nachfolger ist Form_AdminSettings ueber MDIMainForm ->
        // MenuItem_Einstellungen: der kuerzeste und stabilste Weg im Bestand, und
        // W14c ist die LETZTE der W13/W14-Wellen - der Anker haelt damit am
        // laengsten.
        var knoten = Knoten("Form_AdminSettings");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.StartsWith("MDIMainForm", knoten.Pfad, StringComparison.Ordinal);
        Assert.EndsWith("Form_AdminSettings", knoten.Pfad, StringComparison.Ordinal);
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
        // Der Zeuge stand bis iU9-W7.10 auf Form_WP (Masken.WpAdministration); die
        // Maske ist mit W7.3 geloescht. Form_AdminStromspeicher wird ebenso NUR ueber
        // die Sprungtabelle geoeffnet - Masken.StromspeicherAdmin, vom MDI-Menue ueber
        // MenueCtrl und WinFormsNavigation - und kommt erst in Welle 14a an die Reihe.
        var knoten = Knoten("Form_AdminStromspeicher");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.Contains("Masken.StromspeicherAdmin", knoten.Pfad, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Unklar
    // ==================================================================

    [Fact]
    public void EinDauerhaftGesperrterKnopfMachtDenWegUnklarStattJa()
    {
        // Form_PufferSp_Admin schaltet btn_Neu und btn_Bearbeiten in einem Zweig ab
        // und nie wieder ein. Der Weg dahinter wird deshalb nicht behauptet.
        var knoten = Knoten("Form_PufferSp_Bearbeiten");

        Assert.Equal(Erreichbar.Unklar, knoten.Status);
        Assert.Contains(knoten.Oeffner, o => o.Contains("zweifelhaft", StringComparison.Ordinal));
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
        // 28. Die Zahl sinkt mit jeder Welle, der Anteil bleibt.
        Assert.True(ergebnis.Erreichbar(Erreichbar.Ja) >= 27,
                    "Nur " + ergebnis.Erreichbar(Erreichbar.Ja) + " Masken gelten als erreichbar.");

        var uebersicht = Stapel.Uebersicht(ergebnis, Projekt);
        Assert.Contains("| Öffner erreichbar | ", uebersicht, StringComparison.Ordinal);
        Assert.Contains("Öffner erreichbar | Datei |", uebersicht, StringComparison.Ordinal);

        // Seit iU9-W0 ist die Zaehlung von "nein" und "verwaist" leer.
        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Nein));
        Assert.Equal(0, ergebnis.Erreichbar(Erreichbar.Verwaist));

        var befund = Stapel.Erreichbarkeitsbefund(ergebnis, Projekt);
        Assert.Contains("# Öffner erreichbar — Befund aller Masken", befund, StringComparison.Ordinal);
        // iU9-W9: Form_GebWohnflaeche war die erste der beiden "unklar"-Masken und
        // ist mit W9.3 geloescht; uebrig bleibt Form_PufferSp_Bearbeiten (Welle 14a).
        Assert.Contains("| Form_PufferSp_Bearbeiten | unklar |", befund, StringComparison.Ordinal);
        Assert.Contains("| gesamt | " + ergebnis.Masken + " | |", befund, StringComparison.Ordinal);

        // Das Ungeklaerte steht oben - die Liste wird von vorn abgearbeitet.
        var kopf = befund.Substring(befund.IndexOf("| Maske |", StringComparison.Ordinal));
        Assert.True(kopf.IndexOf("| Form_PufferSp_Bearbeiten | unklar |", StringComparison.Ordinal) <
                    kopf.IndexOf("| Form_AdminSettings | ja |", StringComparison.Ordinal),
                    "Die ungeklaerten Masken stehen nicht vorn.");
    }

    [Fact]
    public void OhneSchalterWirdDieErreichbarkeitNichtGerechnet()
    {
        var ergebnis = Stapel.Laufen(Repowurzel.Designer("Klimadaten"), ziel: null, suchwurzel: Projekt,
                                     erreichbarkeit: false);

        Assert.False(ergebnis.MitErreichbarkeit);
        Assert.All(ergebnis.Zeilen, z => Assert.Null(z.Erreichbarkeit));
    }
}
