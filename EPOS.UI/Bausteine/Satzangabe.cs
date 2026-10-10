using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Eine Kurzangabe des gewählten Satzes</b> (UeS2, Anwenderentscheid 10.10.2026: „Im
/// Bereich ‚Projektsatz' kurze Angaben der Anlagendaten und einen Bearbeiten Button anstelle
/// Details."). Die aufgeklappte Detailzeile der <see cref="Zweispaltenauswahl"/> zeigt eine
/// Liste davon als kompaktes Raster — Beschriftung vor Wert, mehrere je Zeile. Der Wert
/// kommt fertig formatiert (Zahlformat der Kultur, mit Einheit) vom Wirt.
/// </summary>
/// <param name="Beschriftung">Kurze Beschriftung ohne Doppelpunkt („Leistung").</param>
/// <param name="Wert">Der formatierte Wert samt Einheit („120,00 kW").</param>
public sealed record Satzangabe(string Beschriftung, string Wert);

/// <summary>
/// <b>Bausteine der Zusammenfassung</b> (UeS2b): was mehrere Wirte gleich bilden — die
/// Katalogangaben aus den Spalten des Profils, Invest und Betrieb, die Senken ohne Präfix.
/// </summary>
public static class Satzangaben
{
    /// <summary>
    /// Die Angaben einer Katalogzeile aus den Spalten des Profils, in dessen Reihenfolge —
    /// nur die genannten Schlüssel (ohne Schlüssel alle außer Bezeichner und Verwendung),
    /// leere Werte bleiben weg, die Einheit der Spalte steht hinter dem Wert.
    /// </summary>
    public static void Katalog(List<Satzangabe> liste, WindowsFormsApplication1.Katalogfilterzeile zeile,
                               WindowsFormsApplication1.Katalogfilterprofil profil, params string[] schluessel)
    {
        foreach (WindowsFormsApplication1.Katalogspalte spalte in profil.Spalten)
        {
            if (schluessel.Length > 0 ? !schluessel.Contains(spalte.Schluessel)
                                      : spalte.Schluessel is WindowsFormsApplication1.Katalogfilterprofil.SpBezeichner
                                                          or WindowsFormsApplication1.Katalogfilterprofil.SpVerwendet) continue;
            string wert = zeile.Text(spalte.Schluessel);
            if (string.IsNullOrWhiteSpace(wert) || wert == WindowsFormsApplication1.ParameterVerwendung.LEER) continue;
            liste.Add(new(spalte.Titel, spalte.Einheit.Length > 0 ? wert + " " + spalte.Einheit : wert));
        }
    }

    /// <summary>Invest (€) und Betrieb (€/a) einer Projektzeile; ohne Summe nichts.</summary>
    public static void Kosten(List<Satzangabe> liste, (double Invest, double Betrieb)? kosten)
    {
        if (kosten is not { } k) return;
        System.Globalization.CultureInfo kultur = System.Globalization.CultureInfo.CurrentCulture;
        liste.Add(new(Resource.AUSWAHL_ZF_INVEST, string.Format(kultur, Resource.AUSWAHL_ZF_EURO, k.Invest)));
        liste.Add(new(Resource.AUSWAHL_ZF_BETRIEB, string.Format(kultur, Resource.AUSWAHL_ZF_EURO_JAHR, k.Betrieb)));
    }

    /// <summary>Die Senkenzeile ohne ihr Präfix „Senken: " (<c>ANL_SENKEN_ZEILE</c>); leer bleibt leer.</summary>
    public static string Senkenwert(string? zeile)
    {
        if (string.IsNullOrWhiteSpace(zeile)) return "";
        string praefix = Resource.ANL_SENKEN_ZEILE.Split("{0}")[0];
        return praefix.Length > 0 && zeile.StartsWith(praefix, System.StringComparison.Ordinal)
            ? zeile[praefix.Length..].Trim() : zeile.Trim();
    }

    /// <summary>Die Senken einer Projektzeile als Angabe, wenn es welche gibt.</summary>
    public static void Senken(List<Satzangabe> liste, string? zeile)
    {
        string senken = Senkenwert(zeile);
        if (senken.Length > 0) liste.Add(new(Resource.AUSWAHL_ZF_SENKEN, senken));
    }

    /// <summary>Vorlauf/Rücklauf in °C; ohne beide nichts.</summary>
    public static void Temperaturpaar(List<Satzangabe> liste, int? vorlauf, int? ruecklauf)
    {
        if (vorlauf is null && ruecklauf is null) return;
        System.Globalization.CultureInfo k = System.Globalization.CultureInfo.CurrentCulture;
        liste.Add(new(Resource.AUSWAHL_ZF_VORLAUF_RUECKLAUF,
                      (vorlauf?.ToString(k) ?? WindowsFormsApplication1.ParameterVerwendung.LEER) + "/" +
                      (ruecklauf?.ToString(k) ?? WindowsFormsApplication1.ParameterVerwendung.LEER) + " °C"));
    }
}
