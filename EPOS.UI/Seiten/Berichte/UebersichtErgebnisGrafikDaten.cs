using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>Eine Version der Grafik: Name, Farbrolle und Farbe (aus der Palette des Hauses) und Simulationsstand.</summary>
public sealed class ErgebnisGrafikVersion
{
    /// <summary>Der Name der Version wie im Spaltenkopf der Tabelle.</summary>
    public string Name { get; init; } = "";

    /// <summary>Die Farbrolle — in allen Diagrammen des Abschnitts dieselbe.</summary>
    public Farbrolle Rolle { get; init; } = Farbrolle.STAMM;

    /// <summary>Die Farbe der Rolle als <c>#RRGGBB</c> für die Legende.</summary>
    public string Farbe { get; init; } = "";

    /// <summary>Liegt ein Simulationsergebnis vor? Sonst keine Balken und in der Legende grau.</summary>
    public bool Simuliert { get; init; } = true;
}

/// <summary>Ein Diagramm einer Gruppe: die Zeilen EINER Einheit samt Zeichenmodell.</summary>
public sealed class ErgebnisDiagramm
{
    /// <summary>Die je Blatt eindeutige Kennung des Bildes (<c>uebersicht-erg-&lt;gruppe&gt;-&lt;n&gt;</c>).</summary>
    public string Kennung { get; init; } = "";

    /// <summary>Die Einheit der Achse; leer bei einer dimensionslosen Kennzahl.</summary>
    public string Einheit { get; init; } = "";

    /// <summary>Titel und Sprachausgabe des Diagramms („Bedarf [MWh/a]“).</summary>
    public string Bezeichnung { get; init; } = "";

    /// <summary>Die Zeilen des Diagramms, in der Reihenfolge der Tabelle.</summary>
    public IReadOnlyList<ErgebnisVergleichZeile> Zeilen { get; init; } = Array.Empty<ErgebnisVergleichZeile>();

    /// <summary>Das Zeichenmodell des Kerns (<see cref="ChartRenderer.VersionsbalkenModell"/>).</summary>
    public Zeichenmodell? Modell { get; init; }
}

/// <summary>
/// Baut aus derselben Liste <see cref="ErgebnisVergleichGruppe"/>/<see cref="ErgebnisVergleichZeile"/>, die auch die Tabelle
/// zeigt, die Diagramme des Abschnitts „Ergebnisse — Energie“: je Gruppe ein Diagramm je Einheit (eine dimensionslose
/// Kennzahl wie die JAZ steht für sich), je Version eine Serie in der Farbe ihrer Rolle. Kein eigener Datenweg: Zahl,
/// Text und Abweichung kommen aus der Zelle, die auch die Tabelle schreibt.
/// </summary>
public static class UebersichtErgebnisGrafikBau
{
    /// <summary>Die Farbrollen der Versionen: der Stamm (dunkelblau), dann die Serien der Varianten (höchstens acht), gut unterscheidbar geordnet.</summary>
    public static readonly IReadOnlyList<Farbrolle> ROLLEN = new[]
    {
        Farbrolle.STAMM, Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_5, Farbrolle.SERIE_7,
        Farbrolle.SERIE_6, Farbrolle.SERIE_4, Farbrolle.SERIE_8, Farbrolle.SERIE_3
    };

    /// <summary>Die Versionen des Stands mit ihrer Farbe, der Stamm zuerst.</summary>
    public static IReadOnlyList<ErgebnisGrafikVersion> Versionen(UebersichtErgebnisStand stand)
    {
        var liste = new List<ErgebnisGrafikVersion>();
        if (stand is null) return liste;
        for (int i = 0; i < stand.Versionsspalten.Count; i++)
        {
            Farbrolle rolle = ROLLEN[i % ROLLEN.Count];
            liste.Add(new ErgebnisGrafikVersion
            {
                Name = stand.Versionsspalten[i].Name,
                Rolle = rolle,
                Farbe = Diagrammfarben.Hex(Farbpalette.Aktuell[rolle]),
                Simuliert = i >= stand.Simuliert.Count || stand.Simuliert[i]
            });
        }
        return liste;
    }

    /// <summary>Teilt die Zeilen einer Gruppe nach Einheit: je Einheit eine Liste in der Reihenfolge des ersten Auftretens; ohne Einheit je Zeile eine.</summary>
    public static IReadOnlyList<IReadOnlyList<ErgebnisVergleichZeile>> NachEinheit(IReadOnlyList<ErgebnisVergleichZeile> zeilen)
    {
        var teile = new List<List<ErgebnisVergleichZeile>>();
        var jeEinheit = new Dictionary<string, List<ErgebnisVergleichZeile>>(StringComparer.Ordinal);
        foreach (ErgebnisVergleichZeile z in zeilen)
        {
            string e = z.Einheit ?? "";
            if (e.Length == 0)
            {
                teile.Add(new List<ErgebnisVergleichZeile> { z });
                continue;
            }
            if (!jeEinheit.TryGetValue(e, out List<ErgebnisVergleichZeile>? liste))
            {
                liste = new List<ErgebnisVergleichZeile>();
                jeEinheit[e] = liste;
                teile.Add(liste);
            }
            liste.Add(z);
        }
        return teile;
    }

    /// <summary>Die Diagramme einer Gruppe, je Einheit eines.</summary>
    /// <param name="gruppe">Die Gruppe der Tabelle.</param>
    /// <param name="versionen">Die Versionen samt Farbe (<see cref="Versionen"/>).</param>
    public static IReadOnlyList<ErgebnisDiagramm> Diagramme(ErgebnisVergleichGruppe gruppe, IReadOnlyList<ErgebnisGrafikVersion> versionen)
    {
        var serien = versionen.Select(v => new Versionsbalkenserie { Name = v.Name, Rolle = v.Rolle }).ToList();
        var liste = new List<ErgebnisDiagramm>();
        int n = 0;
        foreach (IReadOnlyList<ErgebnisVergleichZeile> teil in NachEinheit(gruppe.Zeilen))
        {
            string einheit = teil[0].Einheit ?? "";
            var zeilen = teil.Select(z => Zeile(z, versionen)).ToList();
            liste.Add(new ErgebnisDiagramm
            {
                Kennung = "uebersicht-erg-" + gruppe.Schluessel + "-" + n++,
                Einheit = einheit,
                Bezeichnung = einheit.Length > 0 ? gruppe.Titel + " [" + einheit + "]" : gruppe.Titel + ": " + Name(teil[0]),
                Zeilen = teil,
                Modell = ChartRenderer.VersionsbalkenModell(einheit, serien, zeilen)
            });
        }
        return liste;
    }

    /// <summary>Der Name der Kennzahl ohne Einheit.</summary>
    public static string Name(ErgebnisVergleichZeile z) => string.IsNullOrEmpty(z.Kennzahl) ? z.Bezeichnung : z.Kennzahl;

    /// <summary>Eine Zeile der Tabelle als Kennzahl der Versionsbalken — Texte wörtlich aus den Zellen.</summary>
    private static Versionsbalkenzeile Zeile(ErgebnisVergleichZeile z, IReadOnlyList<ErgebnisGrafikVersion> versionen)
    {
        var werte = new List<double?>();
        var texte = new List<string>();
        var zeiger = new List<string>();
        var ab = new List<bool>();
        string einheit = string.IsNullOrEmpty(z.Einheit) ? "" : " " + z.Einheit;
        for (int i = 0; i < versionen.Count; i++)
        {
            ErgebnisVergleichZelle? c = i < z.Zellen.Count ? z.Zellen[i] : null;
            bool mit = c is not null && !c.NichtSimuliert && versionen[i].Simuliert && c.Wert.HasValue;
            werte.Add(mit ? c!.Wert : null);
            string prozent = c is null || string.IsNullOrEmpty(c.Abweichung) ? "" : c.Abweichung;
            texte.Add(mit ? c!.Text + (prozent.Length > 0 ? "  " + prozent : "") : "");
            zeiger.Add(mit ? versionen[i].Name + " · " + Name(z) + ": " + c!.Text + einheit
                             + (prozent.Length > 0 ? " (" + prozent + ")" : "") : "");
            ab.Add(mit && c!.WeichtAb);
        }
        return new Versionsbalkenzeile { Bezeichnung = Name(z), Werte = werte, Texte = texte, Zeigertexte = zeiger, WeichtAb = ab };
    }
}
