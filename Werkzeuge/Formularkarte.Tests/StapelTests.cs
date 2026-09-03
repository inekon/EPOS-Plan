using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Der Stapellauf ueber ALLE Designer-Dateien des Repos. Das ist der
/// eigentliche Abnahmetest des Werkzeugs: Vor iU9 muss jede Maske eine Karte
/// bekommen - eine, an der der Leser scheitert, waere ein Loch im
/// Vollstaendigkeitsnetz.
/// </summary>
public sealed class StapelTests
{
    private static readonly Lazy<Stapelergebnis> Lauf =
        new(() => Stapel.Laufen(Repowurzel.Pfad, ziel: null));

    [Fact]
    public void FindetAlleDesignerDateienUnabhaengigVonDerSchreibweise()
    {
        // Der Bestand schreibt beides: Form_KostenKomponente.Designer.cs und
        // Form_BHKWEing.designer.cs. Wer nur die grosse Schreibweise sucht,
        // uebersieht ueber ein Drittel der Masken.
        //
        // iU9-W4 (03.09.2026): Bis dahin stand hier Form_KostenKomponente (davor
        // Form_Kosten_VarAuswahl). Die Maske ist mit iU9-W4.2 geloescht (Regel M1);
        // der Zeuge fuer die grosse Schreibweise ist jetzt Form_Heizkessel - eine
        // ueber die Startseite erreichbare Maske, die bis Welle 6 im Bestand bleibt.
        var dateien = Stapel.Dateien(Repowurzel.Pfad);

        Assert.Contains(dateien, d => d.EndsWith("Form_Heizkessel.Designer.cs", StringComparison.Ordinal));
        Assert.Contains(dateien, d => d.EndsWith("Form_BHKWEing.designer.cs", StringComparison.Ordinal));
        // Gemessener Stand 03.09.2026 nach iU9-W5: 89 Dateien (92 nach iU9-W4,
        // 101 nach iU9-W3, 105 nach iU9-W2, 108 nach iU9-W0). Die drei
        // Designer-Masken der Welle 5 (UcBericht, UcWirtschaftlichkeit,
        // Form_BkUebernahme) sind umgestellt und geloescht; die drei uebrigen
        // Masken des Reiters (UcBerichteKosten, UcBkKosten, UcBkUebersicht)
        // waren K4 und zaehlten hier ohnehin nicht mit.
        Assert.True(dateien.Count >= 89, "Es wurden nur " + dateien.Count + " Designer-Dateien gefunden.");
    }

    [Fact]
    public void KeineEinzigeDateiBleibtUngelesen()
    {
        Assert.Empty(Lauf.Value.Fehler);
    }

    [Fact]
    public void DreiDateienSindKeineMasken()
    {
        // Resource.Designer.cs, Settings.Designer.cs, Resources.Designer.cs -
        // sie haben kein InitializeComponent und werden uebersprungen, nicht
        // als Fehler gezaehlt.
        Assert.All(Lauf.Value.KeineMaske,
                   d => Assert.DoesNotContain("InitializeComponent", File.ReadAllText(d)));
        Assert.Equal(Lauf.Value.Dateien, Lauf.Value.Masken + Lauf.Value.KeineMaske.Count);
    }

    [Fact]
    public void JedeMaskeLiefertEineKarte()
    {
        // Gemessener Stand 03.09.2026 nach iU9-W5: 88 Masken (91 nach iU9-W4,
        // 98 nach iU9-W3, 102 nach iU9-W2, 105 nach iU9-W0, 111 nach iU9-W1).
        // Die drei Designer-Masken der Welle 5 sind auf Razor-Komponenten
        // umgestellt und geloescht (Regel M1).
        Assert.True(Lauf.Value.Masken >= 88, "Nur " + Lauf.Value.Masken + " Masken gelesen.");
        Assert.All(Lauf.Value.Zeilen, z => Assert.True(z.Gelesen));
        Assert.All(Lauf.Value.Zeilen, z => Assert.False(string.IsNullOrWhiteSpace(z.Bezeichner)));
    }

    [Fact]
    public void DieHaelfteDerMaskenIstLokalisiert()
    {
        // Gemessener Stand 03.09.2026 nach iU9-W5: 59 von 88 — unveraendert seit
        // iU9-W2, denn auch keine der Masken der Wellen 4 und 5 war lokalisiert
        // (sie alle setzten ihre Texte im Code). Der Anteil steigt damit von
        // Welle zu Welle: Der Leser muss weiterhin beide Wege koennen, nicht nur
        // den Designer.
        Assert.True(Lauf.Value.Lokalisierte >= 59,
                    "Nur " + Lauf.Value.Lokalisierte + " lokalisierte Masken erkannt.");
    }

    [Fact]
    public void DieHaeufigstenTypenSindAbgedeckt()
    {
        var typen = Lauf.Value.Typen;

        foreach (var typ in new[] { "Label", "TextBox", "Button", "ComboBox", "GroupBox", "TabPage",
                                    "CheckBox", "NumericUpDown", "ListBox", "DataGridView", "Chart" })
        {
            Assert.True(typen.ContainsKey(typ), "Typ " + typ + " kam im Stapellauf nicht vor.");
            Assert.True(Typtabelle.Bekannt(typ), "Typ " + typ + " ist dem Leser unbekannt.");
        }
    }

    [Fact]
    public void UnbekannteTypenSindNurDieEigenenSteuerelementeDesHauses()
    {
        // Alles, was der Leser nicht kennt, landet als "sonstig" in der Karte -
        // sichtbar, nicht geraten. Es duerfen nur die selbstgebauten Controls
        // des Bestands sein.
        Assert.All(Lauf.Value.Unbekannt.Keys,
                   typ => Assert.Contains(typ, new[] { "AktionsKarte", "ProjektAuswahl",
                                                       "HeaderGradientPanel", "KlimazonenKarte" }));
    }

    [Fact]
    public void DieUebersichtNenntZahlenUndMasken()
    {
        var uebersicht = Stapel.Uebersicht(Lauf.Value, Repowurzel.Pfad);

        Assert.Contains("# Stapellauf Formularkarte", uebersicht, StringComparison.Ordinal);
        Assert.Contains("| davon Masken (mit InitializeComponent) | " + Lauf.Value.Masken + " |",
                        uebersicht, StringComparison.Ordinal);
        Assert.Contains("Form_Heizkessel", uebersicht, StringComparison.Ordinal);
    }

    [Fact]
    public void StapellaufSchreibtKarteUndSkelettJeMaske()
    {
        var ziel = Path.Combine(Path.GetTempPath(), "formularkarte-" + Guid.NewGuid().ToString("N"));
        try
        {
            // iU9-W4: Views/Kosten fuehrt seit Welle 4 keine Designer-Maske mehr
            // (alle zwanzig sind umgestellt oder stillgelegt). Der Stapellauf
            // laeuft jetzt ueber Views/Heizkessel - vier Masken, davon drei
            // lokalisiert.
            var ergebnis = Stapel.Laufen(Repowurzel.Designer("Heizkessel"), ziel);

            Assert.Empty(ergebnis.Fehler);
            Assert.True(File.Exists(Path.Combine(ziel, "Form_Heizkessel.karte.md")));
            Assert.True(File.Exists(Path.Combine(ziel, "Form_Heizkessel.razor")));
            Assert.True(File.Exists(Path.Combine(ziel, "Form_Heizkessel_Admin.razor")));

            // UTF-8 mit BOM - Hausregel fuer neue Dateien.
            var kopf = File.ReadAllBytes(Path.Combine(ziel, "Form_Heizkessel.razor"))[..3];
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, kopf);
        }
        finally
        {
            if (Directory.Exists(ziel)) Directory.Delete(ziel, recursive: true);
        }
    }
}
