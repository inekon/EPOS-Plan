using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Der Erreichbarkeitsgraph gegen den echten Bestand.
///
/// <para>
/// Anlass ist der Befund vom 03.09.2026: Die Feldkarte wies fuer
/// <c>Form_Kosten_Auswahl</c> einen Aufrufer aus (<c>Form_Kosten</c>), und daran
/// wurde der erste Blazor-Dialog gehaengt - <c>Form_Kosten</c> selbst hat aber
/// seit KD6a keinen Einstieg mehr. Die Tests halten genau diese Faelle fest:
/// eine Maske mit Oeffner, aber ohne Weg dorthin; eine Maske mit Weg; und die
/// beiden Wurzeln.
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
    //  Der Befund, der das Werkzeug ausgeloest hat
    // ==================================================================

    [Fact]
    public void FormKostenHatEinenOeffnerAberKeinenEinstieg()
    {
        var knoten = Knoten("Form_Kosten");

        Assert.Equal(Erreichbar.Nein, knoten.Status);
        Assert.Equal("", knoten.Pfad);

        // Der eine Oeffner ist Form_Start.btn_Kosten_Click - und der Knopf wird in
        // BaueBerichteKostenSeite mit EntferneAltknopf aus der Maske genommen.
        var oeffner = Assert.Single(knoten.Oeffner);
        Assert.Contains("Form_Start.btn_Kosten_Click", oeffner, StringComparison.Ordinal);
        Assert.Contains("gesperrt", oeffner, StringComparison.Ordinal);
        Assert.Contains("EntferneAltknopf", oeffner, StringComparison.Ordinal);
        Assert.Contains("btn_Kosten", oeffner, StringComparison.Ordinal);
    }

    [Fact]
    public void DerGrundStehtSoAuchImKopfDerFeldkarte()
    {
        var maske = Kartenbau.Vollstaendig(Repowurzel.Designer("Kosten/Form_Kosten.Designer.cs"), null, Projekt);
        var karte = FeldkarteSchreiber.Schreiben(maske);

        Assert.Contains("| Öffner erreichbar | nein", karte, StringComparison.Ordinal);
        Assert.Contains("EntferneAltknopf", karte, StringComparison.Ordinal);
    }

    [Fact]
    public void WasAmEinstiegslosenFormKostenHaengtIstEbenfallsUnerreichbar()
    {
        // Die Karte nannte fuer beide einen Aufrufer - beide haengen aber an
        // Form_Kosten. Genau diese Kette hat iU8-9 uebersehen.
        foreach (var klasse in new[] { "Form_KostenfaktorItem", "ucKostenZeile" })
        {
            var knoten = Knoten(klasse);
            Assert.Equal(Erreichbar.Nein, knoten.Status);
            Assert.All(knoten.Oeffner, o => Assert.Contains("Form_Kosten.", o, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void FormVariantentestHaengtAmZweitenEntferntenAltknopf()
    {
        var knoten = Knoten("Form_Variantentest");

        Assert.Equal(Erreichbar.Nein, knoten.Status);
        var oeffner = Assert.Single(knoten.Oeffner);
        Assert.Contains("Form_Start.btn_Varianten_Click", oeffner, StringComparison.Ordinal);
        Assert.Contains("btn_Varianten wird zur Laufzeit entfernt", oeffner, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Masken mit Weg
    // ==================================================================

    [Fact]
    public void FormKostenKomponenteIstUeberDenReiterBerichteUndKostenZuErreichen()
    {
        var knoten = Knoten("Form_KostenKomponente");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.NotEqual("", knoten.Pfad);

        // Der Weg, an dem die Nachfolge von Form_Kosten haengt: der Knopf
        // "Kostenverwaltung oeffnen..." auf der Seite Kosten des Reiters.
        Assert.Contains(knoten.Oeffner,
                        o => o.StartsWith("UcBkKosten.btnVerwaltung_Click", StringComparison.Ordinal));
    }

    [Fact]
    public void FormEnergietraegerIstErreichbar()
    {
        var knoten = Knoten("Form_Energietraeger");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.Contains("Form_Energietraeger", knoten.Pfad, StringComparison.Ordinal);
        Assert.Contains(knoten.Oeffner,
                        o => o.StartsWith("UcBkKosten.btnTraeger_Click", StringComparison.Ordinal));
    }

    [Fact]
    public void FormHeizkesselIstUeberDieStartseiteZuErreichen()
    {
        var knoten = Knoten("Form_Heizkessel");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.StartsWith("Form_Start", knoten.Pfad, StringComparison.Ordinal);
        Assert.EndsWith("Form_Heizkessel", knoten.Pfad, StringComparison.Ordinal);
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
        // Form_WP wird NUR ueber Masken.WpAdministration geoeffnet - der Weg fuehrt
        // vom MDI-Menue ueber MenueCtrl und WinFormsNavigation.
        var knoten = Knoten("Form_WP");

        Assert.Equal(Erreichbar.Ja, knoten.Status);
        Assert.Contains("Masken.WpAdministration", knoten.Pfad, StringComparison.Ordinal);
    }

    // ==================================================================
    //  Verwaist und unklar
    // ==================================================================

    [Fact]
    public void FormSimulationKurzIstVerwaistUndWirdNichtEinmalUebersetzt()
    {
        var knoten = Knoten("Form_Simulation_Kurz");

        Assert.Equal(Erreichbar.Verwaist, knoten.Status);
        Assert.Empty(knoten.Oeffner);
        Assert.False(knoten.Uebersetzt);
        Assert.Contains(knoten.Hinweise, h => h.Contains("Compile Remove", StringComparison.Ordinal));
    }

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
        Assert.True(ergebnis.Erreichbar(Erreichbar.Ja) > 100,
                    "Nur " + ergebnis.Erreichbar(Erreichbar.Ja) + " Masken gelten als erreichbar.");

        var uebersicht = Stapel.Uebersicht(ergebnis, Projekt);
        Assert.Contains("| Öffner erreichbar | ", uebersicht, StringComparison.Ordinal);
        Assert.Contains("Öffner erreichbar | Datei |", uebersicht, StringComparison.Ordinal);

        var befund = Stapel.Erreichbarkeitsbefund(ergebnis, Projekt);
        Assert.Contains("# Öffner erreichbar — Befund aller Masken", befund, StringComparison.Ordinal);
        Assert.Contains("| Form_Kosten | nein |", befund, StringComparison.Ordinal);
        Assert.Contains("| Form_Simulation_Kurz | verwaist |", befund, StringComparison.Ordinal);
        Assert.Contains("| gesamt | " + ergebnis.Masken + " | |", befund, StringComparison.Ordinal);

        // Unerreichbares steht oben - die Liste wird von vorn abgearbeitet.
        var kopf = befund.Substring(befund.IndexOf("| Maske |", StringComparison.Ordinal));
        Assert.True(kopf.IndexOf("| Form_Kosten | nein |", StringComparison.Ordinal) <
                    kopf.IndexOf("| Form_Heizkessel | ja |", StringComparison.Ordinal),
                    "Die unerreichbaren Masken stehen nicht vorn.");
    }

    [Fact]
    public void OhneSchalterWirdDieErreichbarkeitNichtGerechnet()
    {
        var ergebnis = Stapel.Laufen(Repowurzel.Designer("Kosten"), ziel: null, suchwurzel: Projekt,
                                     erreichbarkeit: false);

        Assert.False(ergebnis.MitErreichbarkeit);
        Assert.All(ergebnis.Zeilen, z => Assert.Null(z.Erreichbarkeit));
    }
}
