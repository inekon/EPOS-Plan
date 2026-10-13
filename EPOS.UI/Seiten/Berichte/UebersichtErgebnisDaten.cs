using System;
using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>Eine Zelle des Energievergleichs: der Wert einer Version, fertig formatiert.</summary>
public sealed class ErgebnisVergleichZelle
{
    /// <summary>Der Wert im Format der Kennzahl; „—“ ohne Wert.</summary>
    public string Text { get; set; } = "";

    /// <summary>Weicht der Wert vom Stammprojekt ab? (Der Kern entscheidet.)</summary>
    public bool WeichtAb { get; set; }

    /// <summary>Die Abweichung in Prozent zum Stamm („+12,3 %“); leer = keine Angabe.</summary>
    public string Abweichung { get; set; } = "";

    /// <summary>Ist die Version nicht simuliert? Dann trägt die Zelle „—“ mit Kurztext.</summary>
    public bool NichtSimuliert { get; set; }
}

/// <summary>Eine Zeile des Energievergleichs: eine Kennzahl, je Version eine Zelle.</summary>
public sealed class ErgebnisVergleichZeile
{
    /// <summary>Die Beschriftung samt Einheit („Wärmebedarf gesamt [MWh/a]“).</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Je Version eine Zelle, der Stamm zuerst.</summary>
    public IReadOnlyList<ErgebnisVergleichZelle> Zellen { get; set; } = Array.Empty<ErgebnisVergleichZelle>();

    /// <summary>Weicht wenigstens eine Version ab?</summary>
    public bool WeichtAb
    {
        get
        {
            foreach (ErgebnisVergleichZelle z in Zellen)
                if (z.WeichtAb) return true;
            return false;
        }
    }
}

/// <summary>Eine Gruppe des Energievergleichs („Bedarf“, „Strom“ …) mit ihren Zeilen.</summary>
public sealed class ErgebnisVergleichGruppe
{
    /// <summary>Der Gruppentitel.</summary>
    public string Titel { get; set; } = "";

    /// <summary>Die Zeilen der Gruppe.</summary>
    public IReadOnlyList<ErgebnisVergleichZeile> Zeilen { get; set; } = Array.Empty<ErgebnisVergleichZeile>();

    /// <summary>
    /// Steht die Gruppe als Klappgruppe unter der Tafel (die Emissionen, Anwenderwunsch 13.10.2026: als Option, damit
    /// die Übersicht nicht zu voll wird)?
    /// </summary>
    public bool Klappbar { get; set; }

    /// <summary>Weicht in der Gruppe wenigstens eine Zelle vom Stamm ab?</summary>
    public bool WeichtAb
    {
        get
        {
            foreach (ErgebnisVergleichZeile z in Zeilen)
                if (z.WeichtAb) return true;
            return false;
        }
    }
}

/// <summary>
/// Der Anzeigestand des Abschnitts „Ergebnisse — Energie“ der Übersicht (Anwenderauftrag 13.10.2026): dieselben
/// Versionen wie der Komponentenvergleich (Stamm und die Varianten im Vergleich), je Version der Simulationsstand.
/// </summary>
public sealed class UebersichtErgebnisStand
{
    /// <summary>Die Spaltenköpfe der Versionen, der Stamm zuerst.</summary>
    public IReadOnlyList<VersionsSpalte> Versionsspalten { get; set; } = Array.Empty<VersionsSpalte>();

    /// <summary>Die Gruppen mit ihren Zeilen; leer, wenn keine Version simuliert ist.</summary>
    public IReadOnlyList<ErgebnisVergleichGruppe> Gruppen { get; set; } = Array.Empty<ErgebnisVergleichGruppe>();

    /// <summary>Ist keine Version im Vergleich simuliert? Dann zeigt der Abschnitt einen Hinweis statt der Tabelle.</summary>
    public bool KeineSimulation { get; set; }
}
