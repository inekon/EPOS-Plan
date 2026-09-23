using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Was die drei Zeitreihenverwaltungen im Stammblatt gleich tun</b> (Konzept
/// Administrationsdialoge, Stufe 4: V8, V9, V12, V13) — Wärmebedarf Lastgang,
/// Solarthermieganglinie und Stromganglinie.
///
/// <para><b>Warum eine gemeinsame Stelle.</b> Kennzahlen, Löschsperre mit dem
/// Projektnamen, Rückfrage und Vergleich folgen in allen drei Verwaltungen denselben
/// Regeln; drei Fassungen liefen beim ersten Wechsel auseinander (Hausregel „ein Dialog
/// baut kein Hausmuster selbst nach"). Die Oberfläche rechnet dabei nichts — die Zahlen
/// kommen fertig aus der Zeile der Liste und aus <see cref="Ganglinienansicht"/>.</para>
///
/// <para><b>Wann Löschen gesperrt ist.</b> Eine Zeile bleibt stehen, wenn sie ein
/// Auslieferungssatz ist ODER ein Projekt sie verwendet. Sind es alle Zielzeilen, ist
/// „Löschen" WEICH gesperrt, und der Grund nennt die Projekte (Konzept 3.4: „Dass ein
/// Projekt einen Satz verwendet, ist kein Kennzeichen an der Zeile, sondern der
/// Sperrgrund von Löschen…").</para>
/// </summary>
public static class Ganglinienblatt
{
    /// <summary>
    /// Die drei Kennzahlen im Kopf: Jahresarbeit und Spitze aus der Zeile der Liste —
    /// dieselben Zahlen, nach denen sie sortiert —, die Volllaststunden aus der Ansicht.
    /// </summary>
    public static IReadOnlyList<Stammblattkennzahl> Kennzahlen(Katalogfilterzeile? zeile,
                                                               Ganglinienansicht? ansicht)
    {
        if (zeile is null) return Array.Empty<Stammblattkennzahl>();

        string volllast = ansicht?.Kennzahlen?.VollbenutzungsstundenH is double h
            ? h.ToString("N0", CultureInfo.CurrentCulture) + " h"
            : ParameterVerwendung.LEER;

        return new[]
        {
            new Stammblattkennzahl(MitEinheit(zeile.Text(Katalogfilterprofil.SpJahresarbeitMwh), "MWh"),
                                   Resource.KFLT_SP_JAHRESARBEIT),
            new Stammblattkennzahl(MitEinheit(zeile.Text(Katalogfilterprofil.SpSpitzeKw), "kW"),
                                   Resource.KFLT_SP_SPITZE),
            new Stammblattkennzahl(volllast, Resource.ADM_SB_VOLLLAST)
        };
    }

    private static string MitEinheit(string wert, string einheit)
        => wert.Length == 0 || wert == ParameterVerwendung.LEER ? ParameterVerwendung.LEER : wert + " " + einheit;

    /// <summary>„Auslieferungssatz" oder „eigener Satz".</summary>
    public static string Herkunft(bool auslieferung)
        => auslieferung ? Resource.ADM_SB_AUSLIEFERUNG : Resource.ADM_SB_EIGENER_SATZ;

    /// <summary>Die Projekte, die einen Satz verwenden; ohne Karte keine.</summary>
    public static IReadOnlyList<string> Projekte(IReadOnlyDictionary<string, IReadOnlyList<string>>? verwendung,
                                                 string bezeichner)
        => verwendung is not null && verwendung.TryGetValue(bezeichner ?? "", out IReadOnlyList<string>? p) && p is not null
            ? p : Array.Empty<string>();

    /// <summary>Die Projekte als Aufzählung in Anführungszeichen: „Projekt 1", „Projekt 2".</summary>
    public static string Projektliste(IEnumerable<string> projekte)
        => string.Join(", ", projekte.Select(p => "„" + p + "“"));

    /// <summary>
    /// <b>Der Sperrgrund von „Löschen"</b>: leer, solange eine der Zielzeilen gelöscht
    /// werden darf; sonst die Projekte, die sie verwenden, oder — sind es nur
    /// Auslieferungssätze — der Satz dazu.
    /// </summary>
    public static string LoeschSperrgrund(IReadOnlyList<string> ziele, ISet<string> geschuetzt,
                                          IReadOnlyDictionary<string, IReadOnlyList<string>>? verwendung)
    {
        if (ziele.Count == 0) return "";
        if (ziele.Any(z => !geschuetzt.Contains(z) && Projekte(verwendung, z).Count == 0)) return "";

        var projekte = ziele.SelectMany(z => Projekte(verwendung, z)).Distinct(StringComparer.Ordinal).ToList();
        return projekte.Count > 0
            ? string.Format(CultureInfo.CurrentCulture, Resource.ADM_AW_LOESCHEN_VERWENDET, Projektliste(projekte))
            : Resource.ADM_AW_LOESCHEN_GESPERRT_OHNE_KOPIE;
    }

    /// <summary>
    /// <b>Die Rückfrage vor dem Löschen</b> (AD-Q9): nennt, was gelöscht wird, und was
    /// stehen bleibt — getrennt nach Auslieferungssatz und Verwendung in Projekten.
    /// </summary>
    /// <param name="loeschbar">Die Zeilen, die gelöscht werden.</param>
    /// <param name="ausgeliefert">Die Auslieferungssätze, die stehen bleiben.</param>
    /// <param name="verwendet">Die Zeilen, die ein Projekt verwendet.</param>
    /// <param name="vorlageEinzeln">Die Frage für genau eine Zeile (<c>{0}</c> = ihr Name).</param>
    public static string Rueckfrage(IReadOnlyList<string> loeschbar, IReadOnlyList<string> ausgeliefert,
                                    IReadOnlyList<string> verwendet, string vorlageEinzeln)
    {
        string frage = loeschbar.Count == 1
            ? string.Format(CultureInfo.CurrentCulture, vorlageEinzeln, loeschbar[0])
            : string.Format(CultureInfo.CurrentCulture, Resource.ADM_LOESCHEN_FRAGE_MEHR,
                            loeschbar.Count, string.Join(", ", loeschbar));
        if (ausgeliefert.Count > 0)
            frage += " " + string.Format(CultureInfo.CurrentCulture, Resource.ADM_LOESCHEN_BLEIBEN,
                                         string.Join(", ", ausgeliefert));
        if (verwendet.Count > 0)
            frage += " " + string.Format(CultureInfo.CurrentCulture, Resource.ADM_LOESCHEN_BLEIBEN_VERWENDET,
                                         string.Join(", ", verwendet));
        return frage;
    }

    /// <summary>Die Statuszeile nach dem Löschen: der Name bei einer Zeile, sonst die Zahlen.</summary>
    public static string Geloescht(int ziele, int geloescht, string einzeln, string vorlageEinzeln)
    {
        int blieben = ziele - geloescht;
        return ziele == 1
            ? string.Format(CultureInfo.CurrentCulture, vorlageEinzeln, einzeln)
            : blieben > 0
                ? string.Format(CultureInfo.CurrentCulture, Resource.ADM_MSG_GELOESCHT_BLEIBEN, geloescht, blieben)
                : string.Format(CultureInfo.CurrentCulture, Resource.ADM_MSG_GELOESCHT, geloescht);
    }

    /// <summary>
    /// <b>Der Vergleich</b> (V12): die Spalten der Liste, dazu Herkunft und Verwendung.
    /// </summary>
    public static IReadOnlyList<Vergleichszeile> Vergleich(Katalogfilterprofil? profil,
                                                           IReadOnlyList<Katalogfilterzeile> zeilen,
                                                           IReadOnlyDictionary<string, IReadOnlyList<string>>? verwendung)
    {
        var liste = profil is null
            ? new List<Vergleichszeile>()
            : new List<Vergleichszeile>(Vergleichsbau.AusSpalten(profil, zeilen));

        var herkunft = zeilen.Select(z => Herkunft(z.Geschuetzt)).ToList();
        liste.Add(new Vergleichszeile(Resource.ADM_VG_HERKUNFT, herkunft, Vergleichsbau.Abweichend(herkunft)));

        if (verwendung is not null)
        {
            var projekte = zeilen.Select(z =>
            {
                IReadOnlyList<string> p = Projekte(verwendung, z.Bezeichner);
                return p.Count == 0 ? ParameterVerwendung.LEER : string.Join(", ", p);
            }).ToList();
            liste.Add(new Vergleichszeile(Resource.ADM_SB_VERWENDET, projekte, Vergleichsbau.Abweichend(projekte)));
        }
        return liste;
    }
}
