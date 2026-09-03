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
        // Gemessener Stand nach Welle 10a: 53 Dateien (58 nach W9, 66 nach W8,
        // 76 nach W7, 82 nach W6, 89 nach W5, 92 nach iU9-W4, 101 nach iU9-W3,
        // 105 nach iU9-W2, 108 nach iU9-W0). Jede umgestellte Maske senkt die
        // Zahl (Regel M1); Welle 10a nimmt FUENF Designer-Dateien mit -
        // Form_Betriebsmodus, Form_Klimazonenkarte, Form_QuelleErdreich,
        // Form_QuellePufferspeicher und Form_PufferSp_Projekt. Ihre beiden
        // Geschwister Form_Quellprofil und Form_Waermesenke hatten nie einen
        // Designer (Befund W10-B38) und zaehlen hier deshalb nicht mit.
        Assert.True(dateien.Count >= 53, "Es wurden nur " + dateien.Count + " Designer-Dateien gefunden.");
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
        // Gemessener Stand nach Welle 10a: 50 Masken (55 nach W9, 63 nach W8,
        // 73 nach W7, 81 nach W6, 88 nach W5, 91 nach iU9-W4, 98 nach iU9-W3,
        // 102 nach iU9-W2, 105 nach iU9-W0, 111 nach iU9-W1). Welle 10a stellt
        // SIEBEN Masken um, aber nur FUENF davon hatte die Karte je gesehen:
        // Form_Quellprofil und Form_Waermesenke bauen ihre Oberflaeche im Code
        // auf und haben keinen Designer (Befund W10-B38).
        Assert.True(Lauf.Value.Masken >= 50, "Nur " + Lauf.Value.Masken + " Masken gelesen.");
        Assert.All(Lauf.Value.Zeilen, z => Assert.True(z.Gelesen));
        Assert.All(Lauf.Value.Zeilen, z => Assert.False(string.IsNullOrWhiteSpace(z.Bezeichner)));
    }

    [Fact]
    public void DieHaelfteDerMaskenIstLokalisiert()
    {
        // Bis Welle 5 stand der Zaehler unveraendert bei 59: Keine der Masken der
        // Wellen 2 bis 5 war lokalisiert, sie alle setzten ihre Texte im Code.
        // Welle 6 stellt erstmals wieder LOKALISIERTE Masken um (54), Welle 7
        // sieben weitere (47), Welle 8 alle zehn (37) - auch die drei
        // Brauchwassermasken zeichnen ueber ApplyResources, obwohl ihre Texte
        // deutsche Literale in der neutralen .resx sind. Welle 9 nimmt acht
        // weitere mit (29); nur Form_Brauchwasser war unlokalisiert.
        // Der ANTEIL bleibt bei rund der Haelfte: Der Leser muss weiterhin
        // beide Wege koennen, nicht nur den Designer.
        Assert.True(Lauf.Value.Lokalisierte >= 29,
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
