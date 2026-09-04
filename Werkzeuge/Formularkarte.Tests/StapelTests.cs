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
        // Welle 10b nimmt die letzte dieser Reihe mit: Form_Simulation_Config,
        // den Wirt der sieben Dialoge (50). Welle 11b nimmt SECHS auf einmal -
        // Form_Simulation_Detail, DashboardForm, die drei Navigatoren und
        // Form_SpeicherVariantenVergleich (44); sie werden EINE Razor-Seite.
        // Welle 12 nimmt FUENF mit (39): Form_GanglinieProtokoll,
        // Form_GanglinieImportOptionen, Form_Stromganglinie_Admin,
        // Form_Stromganglinie und Form_PeakShaving. Form_ImportKonflikte faellt
        // in derselben Welle, zaehlte hier aber nie mit - sie hatte keinen
        // Designer (Befund W12-B21). Welle 13 nimmt SECHS mit (33): die vier
        // VDI-3805-Einlesemasken, die Waermebedarfsverwaltung und den
        // CEC-Modulimport. Der Designer der Waermepumpe ist dabei nicht
        // geloescht, sondern nach Pruefmuster/Wärmepumpe/ VERSCHOBEN - er ist
        // der Zeuge des Umlaut-Tests (RazorSchreiberTests) und liegt damit
        // ausserhalb dieses Stapellaufs.
        Assert.True(dateien.Count >= 28, "Es wurden nur " + dateien.Count + " Designer-Dateien gefunden.");
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
        // auf und haben keinen Designer (Befund W10-B38). Welle 10b nimmt EINE
        // weitere mit - Form_Simulation_Config, den Wirt der sieben (49). Welle 11b
        // nimmt SECHS auf einmal (43): die Ergebnisansicht und ihre fuenf
        // Nebenmasken werden EINE Seite (Regel R-W11-2: maskenweise, nicht
        // reiterweise - sonst zwei WebViews in einem Fenster). Welle 12 nimmt
        // FUENF mit (38): die vier Glieder der AP5-Importkette und die
        // Lastspitzenkappung. Welle 13 nimmt SECHS mit (32): die vier
        // VDI-3805-Einlesemasken werden EINE Komponente mit vier Auspraegungen,
        // dazu die Waermebedarfsverwaltung und der CEC-Modulimport. Welle 14a nimmt
        // SIEBEN mit (25): vier Katalogbrowser werden EINE Komponente, zwei
        // Modulkataloge eine zweite, dazu der fehlende vierte Katalogeditor.
        Assert.True(Lauf.Value.Masken >= 25, "Nur " + Lauf.Value.Masken + " Masken gelesen.");
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
        // weitere mit (29); nur Form_Brauchwasser war unlokalisiert. Welle 10b
        // nimmt die EINZIGE lokalisierte Maske ihrer Welle mit - die sieben
        // Dialoge der Welle 10a hatten keine eigene .resx, ihr Wirt
        // Form_Simulation_Config schon (28). Welle 11b nimmt ebenfalls genau EINE
        // lokalisierte mit - Form_Simulation_Detail mit ihren 3 049 neutralen und
        // 248 englischen Eintraegen; die fuenf Nebenmasken hatten keine eigene
        // .resx (27). Welle 12 nimmt ZWEI lokalisierte mit -
        // Form_Stromganglinie und Form_Stromganglinie_Admin; die drei anderen
        // Masken der Welle setzten ihre Texte im Code (25). Welle 13 nimmt VIER
        // lokalisierte mit (21) - Form_Heizkessel_einlesen, Form_PufferSp_einlesen,
        // Form_WP_einlesen und Form_AdminWaermeeinlesen; Form_SolarKollektoren_
        // einlesen hatte weder de-DE noch en-US (Befund W13-B27) und
        // Form_CECImport eine LEERE .resx (B54). Welle 14a nimmt SECHS lokalisierte
        // mit (14) - Form_Heizkessel_Admin, Form_SolarKollektorenAdmin,
        // Form_PufferSp_Admin, Form_PufferSp_Bearbeiten, Form_AdminPV und
        // Form_AdminStromspeicher; Form_BHKWAdmin war als einzige der sieben gar nicht
        // lokalisiert (Befund W14-B11). Gemessen OHNE die Git-Nebenbaeume
        // unter .claude/worktrees (die der Stapellauf seit dem 04.09.2026 uebergeht)
        // sind es 20: Der Lauf im W13-Worktree hatte eine Kopie des Bestands auf
        // einem aelteren Stand mitgezaehlt.
        // Der ANTEIL bleibt bei rund zwei Dritteln: Der Leser muss weiterhin
        // beide Wege koennen, nicht nur den Designer.
        Assert.True(Lauf.Value.Lokalisierte >= 14,
                    "Nur " + Lauf.Value.Lokalisierte + " lokalisierte Masken erkannt.");
    }

    [Fact]
    public void DieHaeufigstenTypenSindAbgedeckt()
    {
        // Der Bestand schrumpft mit jeder Welle von iU9. Die sechs Typen, die bis
        // Welle 16 bleiben (Form_Start, MDIMainForm, WizardParent), muessen im Bestand
        // vorkommen; die fuenf, die schon frueher fallen (NumericUpDown mit W13,
        // DataGridView mit W14a, Chart mit W14c, CheckBox mit W15b, GroupBox mit
        // W15c), genuegen im BESTAND ODER IM PRUEFMUSTER - das eingefrorene Muster
        // ist die einzige Stelle, an der der Leser den Typ nach dem Rueckbau noch
        // vorfindet. Kennen muss der Leser alle elf.
        var bestand = Lauf.Value.Typen;
        var muster = PruefmusterTypen();

        foreach (var typ in new[] { "Label", "TextBox", "Button", "ComboBox", "ListBox", "TabPage" })
            Assert.True(bestand.ContainsKey(typ), "Typ " + typ + " kam im Stapellauf nicht vor.");

        foreach (var typ in new[] { "GroupBox", "CheckBox", "NumericUpDown", "DataGridView", "Chart" })
            Assert.True(bestand.ContainsKey(typ) || muster.Contains(typ),
                        "Typ " + typ + " kam weder im Stapellauf noch im Pruefmuster vor.");

        foreach (var typ in new[] { "Label", "TextBox", "Button", "ComboBox", "GroupBox", "TabPage",
                                    "CheckBox", "NumericUpDown", "ListBox", "DataGridView", "Chart" })
            Assert.True(Typtabelle.Bekannt(typ), "Typ " + typ + " ist dem Leser unbekannt.");
    }

    /// <summary>Alle Steuerelementtypen der eingefrorenen Pruefmuster (der Stapellauf uebergeht den Ordner).</summary>
    private static HashSet<string> PruefmusterTypen()
    {
        var typen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var datei in Directory.EnumerateFiles(Repowurzel.PruefmusterWurzel, "*.cs", SearchOption.AllDirectories)
                     .Where(d => d.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)))
        {
            var maske = Kartenbau.Vollstaendig(datei, null, Repowurzel.PruefmusterWurzel);
            if (maske is null) continue;
            foreach (var typ in Kartenbau.Typzaehlung(maske).Keys) typen.Add(typ);
        }
        return typen;
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
