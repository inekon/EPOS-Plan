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
        // Der Bestand schreibt beides: gross (.Designer.cs) und klein
        // (.designer.cs). Wer nur die grosse Schreibweise sucht, uebersieht ueber
        // ein Drittel der Masken.
        //
        // iU9-W6 (03.09.2026): Beide Zeugen sind neu. Bis dahin standen hier
        // Form_Heizkessel (gross; davor Form_KostenKomponente, davor
        // Form_Kosten_VarAuswahl) und Form_BHKWEing (klein) - beide sind mit
        // iU9-W6.3 bzw. W6.4 geloescht (Regel M1). Die neuen Zeugen halten
        // laenger: Form_Klimadaten kommt erst in Welle 14c an die Reihe,
        // Form_Brauchwasser_Admin in Welle 14b.
        var dateien = Stapel.Dateien(Repowurzel.Pfad);

        Assert.Contains(dateien, d => d.EndsWith("Form_Klimadaten.Designer.cs", StringComparison.Ordinal));
        Assert.Contains(dateien, d => d.EndsWith("Form_Brauchwasser_Admin.designer.cs", StringComparison.Ordinal));
        // Gemessener Stand nach dem Zusammenfuehren von Welle 5 und Welle 6:
        // 89 Dateien nach W5 (92 nach iU9-W4, 101 nach iU9-W3, 105 nach iU9-W2,
        // 108 nach iU9-W0); Welle 6 senkt die Zahl weiter, der endgueltige Wert
        // steht in W6.9.
        Assert.True(dateien.Count >= 85, "Es wurden nur " + dateien.Count + " Designer-Dateien gefunden.");
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
        // Gemessener Stand nach dem Zusammenfuehren von Welle 5 und Welle 6:
        // 88 Masken nach W5 (91 nach iU9-W4, 98 nach iU9-W3, 102 nach iU9-W2,
        // 105 nach iU9-W0, 111 nach iU9-W1). Jede umgestellte Maske senkt die
        // Zahl (Regel M1); der endgueltige Wert der Welle 6 steht in W6.9.
        Assert.True(Lauf.Value.Masken >= 84, "Nur " + Lauf.Value.Masken + " Masken gelesen.");
        Assert.All(Lauf.Value.Zeilen, z => Assert.True(z.Gelesen));
        Assert.All(Lauf.Value.Zeilen, z => Assert.False(string.IsNullOrWhiteSpace(z.Bezeichner)));
    }

    [Fact]
    public void DieHaelfteDerMaskenIstLokalisiert()
    {
        // Bis Welle 5 stand der Zaehler unveraendert bei 59: Keine der Masken der
        // Wellen 2 bis 5 war lokalisiert, sie alle setzten ihre Texte im Code.
        // Welle 6 stellt erstmals wieder LOKALISIERTE Masken um (Heizkessel,
        // Heizkessel_Bearbeiten, Stromspeicher, PufferSp, BHKWEing), deshalb
        // sinkt er hier mit. Der ANTEIL bleibt bei rund zwei Dritteln: Der Leser
        // muss weiterhin beide Wege koennen, nicht nur den Designer.
        Assert.True(Lauf.Value.Lokalisierte >= 56,
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
        Assert.Contains("Form_Klimadaten", uebersicht, StringComparison.Ordinal);
    }

    [Fact]
    public void StapellaufSchreibtKarteUndSkelettJeMaske()
    {
        var ziel = Path.Combine(Path.GetTempPath(), "formularkarte-" + Guid.NewGuid().ToString("N"));
        try
        {
            // iU9-W6: Views/Heizkessel fuehrt seit Welle 6 nur noch zwei
            // Designer-Masken (Form_Heizkessel und der Katalogeditor sind
            // umgestellt). Der Stapellauf laeuft jetzt ueber Views/Klimadaten -
            // eine Maske, die bis Welle 14c bleibt.
            var ergebnis = Stapel.Laufen(Repowurzel.Designer("Klimadaten"), ziel);

            Assert.Empty(ergebnis.Fehler);
            Assert.True(File.Exists(Path.Combine(ziel, "Form_Klimadaten.karte.md")));
            Assert.True(File.Exists(Path.Combine(ziel, "Form_Klimadaten.razor")));

            // UTF-8 mit BOM - Hausregel fuer neue Dateien.
            var kopf = File.ReadAllBytes(Path.Combine(ziel, "Form_Klimadaten.razor"))[..3];
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, kopf);
        }
        finally
        {
            if (Directory.Exists(ziel)) Directory.Delete(ziel, recursive: true);
        }
    }
}
