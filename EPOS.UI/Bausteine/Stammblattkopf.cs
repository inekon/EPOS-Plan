using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Der Kopf des Stammblatts aus der Zeile der Liste</b> (Konzept
/// Administrationsdialoge, Stufe 3, V9): Herkunftszeile und Kennzahlen stehen schon in
/// der <see cref="Katalogfilterzeile"/> — dieselben Werte, nach denen die Liste
/// sortiert und filtert. Kein zweiter Datenweg, keine zweite Formatierung.
///
/// <para><b>Die Herkunftszeile</b> nennt die ersten zwei belegten Textspalten außer
/// dem Bezeichner (beim Heizkessel Hersteller und Brennstoff) und dahinter die Herkunft
/// („Auslieferungssatz" oder „eigener Satz").</para>
///
/// <para><b>Die Kennzahlen</b> sind die ersten drei Zahlen- und Ja/Nein-Spalten des
/// Profils (beim Heizkessel P_th, η und Brennwert) — die Spalten, die das Profil für
/// die Wahl eines Geräts ausgesucht hat.</para>
/// </summary>
public static class Stammblattkopf
{
    /// <summary>„Hersteller · Brennstoff · eigener Satz".</summary>
    public static string Unterzeile(Katalogfilterprofil? profil, Katalogfilterzeile? zeile, string herkunft)
    {
        var teile = new List<string>();
        if (profil is not null && zeile is not null)
        {
            foreach (Katalogspalte s in profil.Spalten)
            {
                if (teile.Count >= 2) break;
                if (s.Art != Katalogspaltenart.Text || Spaltenraenge.IstElastisch(s)) continue;
                string text = zeile.Text(s.Schluessel);
                if (text.Length > 0 && text != ParameterVerwendung.LEER) teile.Add(text);
            }
        }
        if (!string.IsNullOrEmpty(herkunft)) teile.Add(herkunft);
        return string.Join(" · ", teile);
    }

    /// <summary>Bis zu <paramref name="hoechstens"/> Kennzahlen: Wert samt Einheit und Spaltentitel.</summary>
    public static IReadOnlyList<Stammblattkennzahl> Kennzahlen(Katalogfilterprofil? profil,
                                                               Katalogfilterzeile? zeile,
                                                               int hoechstens = 3)
    {
        var liste = new List<Stammblattkennzahl>();
        if (profil is null || zeile is null) return liste;

        foreach (Katalogspalte s in profil.Spalten)
        {
            if (liste.Count >= hoechstens) break;
            if (s.Art == Katalogspaltenart.Text) continue;

            string wert = zeile.Text(s.Schluessel);
            if (s.Einheit.Length > 0 && wert.Length > 0 && wert != ParameterVerwendung.LEER)
                wert += " " + s.Einheit;
            liste.Add(new Stammblattkennzahl(wert.Length > 0 ? wert : ParameterVerwendung.LEER, s.Titel));
        }
        return liste;
    }
}
