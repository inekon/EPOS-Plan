using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpeicherEngine;

/// <summary>
/// <b>Die AUSWAHLREGEL der Groessensuche</b> (Anwenderentscheid vom 15.09.2026): welche
/// Geraete einer Quelle in die vorgegebenen Bereiche fuer Kapazitaet und Leistung passen
/// — und welche ihnen am naechsten kommen, wenn keines hineinpasst.
/// </summary>
/// <remarks>
/// <para><b>Sie steht an EINER Stelle.</b> Dieselbe Methode zaehlt die Kandidatenzeile der
/// Station „Optimierung", fuellt die Kandidatentabelle und baut die Achse des Laufs. Eine
/// zweite Formel in der Oberflaeche liefe beim ersten neuen Sonderfall auseinander.</para>
///
/// <para><b>DIE REGEL, Wort fuer Wort.</b></para>
/// <list type="number">
///   <item><description><b>Treffer.</b> Ein Geraet ist ein TREFFER, wenn seine Kapazitaet
///   im Kapazitaetsbereich UND seine Entladeleistung im Leistungsbereich liegt (Grenzen
///   eingeschlossen). Sein Abstand ist 0.</description></item>
///   <item><description><b>Gibt es Treffer, gibt es nur Treffer.</b> Dann sind die
///   Kandidaten genau die Treffer — wer einen Bereich vorgibt und Geraete darin findet,
///   will nicht daneben rechnen.</description></item>
///   <item><description><b>Gibt es keinen, kommen die NAECHSTLIEGENDEN</b> — hoechstens
///   <see cref="NAECHSTLIEGENDE_HOECHSTENS"/> Stueck, nach steigendem Abstand.</description></item>
/// </list>
///
/// <para><b>DER ABSTAND.</b> Je Groesse wird gemessen, wie weit das Geraet AUSSERHALB
/// seines Bereichs liegt, und dieser Ueberstand auf die MITTE des Bereichs bezogen:</para>
/// <code>
/// ueberstand(x) = x &lt; von ? von - x : x &gt; bis ? x - bis : 0
/// bezug         = (von + bis) / 2
/// d             = ueberstand / bezug
/// Abstand       = d(Kapazitaet) + d(Leistung)
/// </code>
/// <para><b>Warum normiert.</b> Kilowattstunden und Kilowatt sind verschiedene Groessen;
/// 10 kWh neben einem 1 000-kWh-Bereich sind wenig, 10 kW neben einem 20-kW-Bereich sind
/// viel. Ohne Normierung entschiede allein die Einheit, in der gemessen wird.</para>
/// <para><b>Warum die MITTE und nicht die Breite.</b> Ein Bereich darf zu einem Punkt
/// zusammenfallen (<c>von == bis</c>) — genau dann, wenn der Anwender eine Groesse
/// festnagelt. Die Breite waere dort 0 und der Abstand nicht definiert; die Mitte ist
/// immer positiv, weil beide Grenzen positiv sein muessen.</para>
/// <para><b>Warum die Summe und nicht der Euklid.</b> Ein Geraet, das in BEIDEN Groessen
/// danebenliegt, passt schlechter als eines, das nur in einer danebenliegt — die Summe
/// sagt das, die Wurzel schwaecht es ab. Und sie kommt ohne Wurzel aus, also ohne die
/// letzte Stelle, an der zwei Rechner auseinanderlaufen koennten.</para>
///
/// <para><b>DIE REIHENFOLGE IST DETERMINISTISCH.</b> Sortiert wird nach Abstand, bei
/// Gleichstand nach Kapazitaet, dann nach Leistung, zuletzt nach
/// <see cref="FlottenGeraetekandidat.Quellkennung"/> in ordinaler Textordnung. Die
/// Quellkennung ist je Quelle eindeutig; damit hat die Sortierung immer ein letztes
/// Unterscheidungsmerkmal, und zwei Laeufe auf derselben Datenbank liefern dieselbe
/// Reihenfolge.</para>
/// </remarks>
public static class FlottenGeraetewahl
{
    /// <summary>
    /// Wie viele NAECHSTLIEGENDE Geraete angeboten werden, wenn kein einziges in die
    /// Bereiche passt.
    /// </summary>
    /// <remarks>
    /// Fuenf ist eine Wahl, keine Ableitung: genug, um eine Groessenordnung abzuschreiten,
    /// wenig genug, dass die Kandidatentabelle mit einem Blick zu lesen bleibt. Wer mehr
    /// sehen will, weitet seinen Bereich — dann sind es Treffer und keine Ersatzvorschlaege.
    /// </remarks>
    public const int NAECHSTLIEGENDE_HOECHSTENS = 5;

    /// <summary>Zahlenrauschen, das noch als „auf der Grenze" gilt.</summary>
    private const double SCHRANKE = 1e-9;

    /// <summary>
    /// Die Kandidaten EINER Suchachse: die Treffer, sonst die naechstliegenden Geraete —
    /// jeweils mit gefuellter <see cref="FlottenGeraetekandidat.Abweichung"/>.
    /// </summary>
    /// <param name="achse">Die Suchachse samt Bereichen und Geraetebestand.</param>
    /// <returns>
    /// Die gewaehlten Geraete als NEUE Kandidatenobjekte (der Bestand der Achse bleibt
    /// unberuehrt); leer, wenn die Achse keinen brauchbaren Bereich oder kein Geraet fuehrt.
    /// </returns>
    public static IReadOnlyList<FlottenGeraetekandidat> Waehle(FlottenAuslegungsAchse achse)
    {
        if (achse is null || !BereichBrauchbar(achse)) return Array.Empty<FlottenGeraetekandidat>();
        if (achse.Geraete is not { Count: > 0 }) return Array.Empty<FlottenGeraetekandidat>();

        double kapVon = achse.KapazitaetVonKWh, kapBis = achse.KapazitaetBisKWh;
        double leiVon = achse.LeistungVonKw, leiBis = achse.LeistungBisKw;
        double kapBezug = (kapVon + kapBis) / 2.0;
        double leiBezug = (leiVon + leiBis) / 2.0;

        var bewertet = new List<FlottenGeraetekandidat>(achse.Geraete.Count);
        foreach (FlottenGeraetekandidat g in achse.Geraete)
        {
            if (g?.Geraet is null) continue;
            if (!(g.KapazitaetKWh > 0.0) || !(g.LeistungKw > 0.0)) continue;   // kein rechenbares Geraet

            double d = Ueberstand(g.KapazitaetKWh, kapVon, kapBis) / kapBezug
                     + Ueberstand(g.LeistungKw, leiVon, leiBis) / leiBezug;

            bewertet.Add(new FlottenGeraetekandidat
            {
                Quellkennung = g.Quellkennung ?? "",
                Geraet = g.Geraet,
                NeutraleKennwerte = g.NeutraleKennwerte,
                Gefuehrt = g.Gefuehrt,
                Abweichung = d <= SCHRANKE ? 0.0 : d
            });
        }
        if (bewertet.Count == 0) return Array.Empty<FlottenGeraetekandidat>();

        List<FlottenGeraetekandidat> geordnet = bewertet
            .OrderBy(x => x.Abweichung)
            .ThenBy(x => x.KapazitaetKWh)
            .ThenBy(x => x.LeistungKw)
            .ThenBy(x => x.Quellkennung, StringComparer.Ordinal)
            .ToList();

        var treffer = geordnet.Where(x => x.Treffer).ToList();
        return treffer.Count > 0
            ? treffer
            : geordnet.Take(NAECHSTLIEGENDE_HOECHSTENS).ToList();
    }

    /// <summary>
    /// Traegt die Achse zwei brauchbare Bereiche? Beide Untergrenzen muessen positiv und
    /// beide Obergrenzen mindestens so gross sein.
    /// </summary>
    public static bool BereichBrauchbar(FlottenAuslegungsAchse achse)
        => achse is not null
           && Brauchbar(achse.KapazitaetVonKWh, achse.KapazitaetBisKWh)
           && Brauchbar(achse.LeistungVonKw, achse.LeistungBisKw);

    private static bool Brauchbar(double von, double bis)
        => double.IsFinite(von) && double.IsFinite(bis) && von > 0.0 && bis >= von;

    /// <summary>Wie weit <paramref name="wert"/> ausserhalb von <c>[von, bis]</c> liegt; 0 = darin.</summary>
    private static double Ueberstand(double wert, double von, double bis)
        => wert < von ? von - wert : wert > bis ? wert - bis : 0.0;

    /// <summary>
    /// Die Abweichung als Text in Prozent, wie sie die Kandidatentabelle zeigt — der
    /// Kern formatiert sie, damit Tabelle, Bericht und Ausdruck dieselbe Zahl nennen.
    /// </summary>
    /// <param name="abweichung">Der normierte Abstand.</param>
    /// <param name="kultur">Die Kultur des Ausdrucks; <c>null</c> = die laufende.</param>
    public static string Abweichungstext(double abweichung, CultureInfo? kultur = null)
        => (abweichung * 100.0).ToString("0.#", kultur ?? CultureInfo.CurrentCulture);
}

/// <summary>
/// <b>Die BENANNTE UMSETZUNG aelterer Staende</b> auf die einzige gueltige Kopplung
/// Kapazitaet × Leistung.
/// </summary>
/// <remarks>
/// <para>Ein vor der Umstellung gespeicherter Stand traegt in
/// <see cref="FlottenAuslegungsAchse.Modus"/> die Zahl 1 oder 2 — eine C-Rate-Kopplung.
/// Er soll sich oeffnen lassen: nicht mit einem Absturz, aber auch nicht stillschweigend.
/// Deshalb wird er UMGERECHNET, nicht verworfen — die Absicht des Anwenders steckte in
/// den Grenzen, und aus <c>P = E · C</c> laesst sie sich wiederherstellen:</para>
/// <list type="table">
///   <item><term><c>KapazitaetUndCRate</c></term>
///     <description>Der Kapazitaetsbereich bleibt; der Leistungsbereich entsteht aus den
///     ECKEN: <c>P_von = E_von · C_von</c>, <c>P_bis = E_bis · C_bis</c>. Er umfasst damit
///     jede Leistung, die das alte Raster erzeugen konnte.</description></item>
///   <item><term><c>LeistungUndCRate</c></term>
///     <description>Der Leistungsbereich bleibt; der Kapazitaetsbereich entsteht als
///     <c>E_von = P_von / C_bis</c>, <c>E_bis = P_bis / C_von</c> — die schnellste C-Rate
///     ergibt die kleinste Kapazitaet.</description></item>
/// </list>
/// <para><b>Was NICHT umgerechnet wird</b>, sind die Schrittweiten: Die Geraetesuche
/// rastert nicht, sie waehlt unter vorhandenen Geraeten. Sie bleiben im Stand stehen und
/// werden nicht gelesen.</para>
/// <para><b>Ohne brauchbare C-Raten bleibt der vorhandene Bereich stehen.</b> Eine
/// C-Rate von 0 ergaebe eine Division durch null beziehungsweise einen Leistungsbereich
/// von 0 bis 0; dann ist der gespeicherte Bereich die bessere Auskunft als eine gerechnete
/// Null, und die Vorpruefung der Seite sagt dem Anwender, was fehlt.</para>
/// </remarks>
public static class FlottenAltstand
{
    /// <summary>
    /// Setzt EINE Suchachse auf <see cref="FlottenAuslegungsmodus.KapazitaetUndLeistung"/>
    /// um.
    /// </summary>
    /// <param name="achse">Die Achse; <c>null</c> wird uebergangen.</param>
    /// <returns><c>true</c>, wenn wirklich umgesetzt wurde.</returns>
    public static bool Normalisiere(FlottenAuslegungsAchse achse)
    {
        if (achse is null || achse.Modus == FlottenAuslegungsmodus.KapazitaetUndLeistung)
            return false;

        if (achse.Modus == FlottenAuslegungsmodus.KapazitaetUndCRate)
        {
            if (Brauchbar(achse.CRateVon, achse.CRateBis)
                && achse.KapazitaetVonKWh > 0 && achse.KapazitaetBisKWh >= achse.KapazitaetVonKWh)
            {
                achse.LeistungVonKw = achse.KapazitaetVonKWh * achse.CRateVon;
                achse.LeistungBisKw = achse.KapazitaetBisKWh * achse.CRateBis;
            }
        }
        else if (achse.Modus == FlottenAuslegungsmodus.LeistungUndCRate)
        {
            if (Brauchbar(achse.CRateVon, achse.CRateBis)
                && achse.LeistungVonKw > 0 && achse.LeistungBisKw >= achse.LeistungVonKw)
            {
                achse.KapazitaetVonKWh = achse.LeistungVonKw / achse.CRateBis;
                achse.KapazitaetBisKWh = achse.LeistungBisKw / achse.CRateVon;
            }
        }

        achse.Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung;
        return true;
    }

    /// <summary>Setzt den ganzen Suchraum um; <c>true</c>, wenn wenigstens eine Achse umgesetzt wurde.</summary>
    /// <param name="auslegung">Der Suchraum; <c>null</c> wird uebergangen.</param>
    public static bool Normalisiere(FlottenAuslegungEingang auslegung)
    {
        bool umgesetzt = false;
        if (auslegung?.Achsen is not { Count: > 0 } achsen) return false;
        foreach (FlottenAuslegungsAchse a in achsen) umgesetzt |= Normalisiere(a);
        return umgesetzt;
    }

    /// <summary>Setzt die Kopplung eines aufbewahrten ERGEBNISSES um (Achsen der Groessen-Sicht).</summary>
    /// <param name="ergebnis">Das Ergebnis; <c>null</c> wird uebergangen.</param>
    public static bool Normalisiere(FlottenAuslegungErgebnis ergebnis)
    {
        if (ergebnis is null || ergebnis.Achsenmodus == FlottenAuslegungsmodus.KapazitaetUndLeistung)
            return false;
        ergebnis.Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndLeistung;
        return true;
    }

    private static bool Brauchbar(double von, double bis)
        => double.IsFinite(von) && double.IsFinite(bis) && von > 0.0 && bis >= von;
}
