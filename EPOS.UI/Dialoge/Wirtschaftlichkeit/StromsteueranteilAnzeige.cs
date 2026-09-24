using System.Globalization;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Eine Zeile der Anzeige des erfassten Stromsteueranteils. <see cref="Zustand"/>
/// <c>null</c> = Herleitungszeile, sonst Kohärenzzeile.
/// </summary>
public sealed record StromsteueranteilZeile(string Text, KohaerenzZustand? Zustand);

/// <summary>
/// ETAPPE E18 (Konzept § 6.3 Nr. 16, E18‑Q4/Q5 a) — der Rückweg „die Pflegestelle der
/// Unternehmensart zeigt den erfassten Preisanteil". Der Hinweg (die Unternehmensart
/// hebt die Schnellwahl in „Strompreis Details" hervor) steht in
/// <c>EnergietraegerHuelle.ReduzierterSatzEmpfohlen</c>; hier wird dieselbe Regel
/// umgekehrt angezeigt.
///
/// <para><b>Nur Anzeige, keine zweite Pflegestelle</b> (Konzept § 6.5): gepflegt wird
/// der Anteil ausschließlich im Energieträgerdialog. Die Zeilen folgen der GEWÄHLTEN
/// (auch noch ungespeicherten) Unternehmensart; eine Abweichung ist ein Hinweis, keine
/// Sperre. Gerechnet wird hier nichts, was in den Kapitalwert ginge.</para>
/// </summary>
public static class StromsteueranteilAnzeige
{
    /// <summary>Toleranz des Satzvergleichs [ct/kWh] — dieselbe wie Fall 4 der
    /// Kohärenzprüfung (<c>KohaerenzPruefung.TOLERANZ_CT_KWH</c>).</summary>
    public const double TOLERANZ_CT_KWH = 0.005;

    /// <summary>
    /// Die Zeilen für einen Lesestand, eine Unternehmensart und ein Katalogjahr.
    /// Ohne Lesestand (<paramref name="stand"/> <c>null</c>, z. B. ohne Gaben) keine Zeile.
    /// </summary>
    public static IReadOnlyList<StromsteueranteilZeile> Zeilen(
        StromsteueranteilStand? stand, string? unternehmensart, int jahr,
        Func<string, int, GesetzParameter>? katalog, BhkwWirtschaftlichkeitTexte t, CultureInfo kultur)
    {
        var zeilen = new List<StromsteueranteilZeile>();
        if (stand is null) return zeilen;

        if (!stand.Lesbar)
        {
            zeilen.Add(new StromsteueranteilZeile(string.Format(kultur, t.StAnteilNichtLesbar, stand.Grund), null));
            return zeilen;
        }
        if (!stand.HatTraeger)
        {
            zeilen.Add(new StromsteueranteilZeile(t.StAnteilKeinTraeger, null));
            return zeilen;
        }
        if (!stand.WertCtKwh.HasValue)
        {
            zeilen.Add(new StromsteueranteilZeile(string.Format(kultur, t.StAnteilKeiner, stand.TraegerName), null));
            zeilen.Add(new StromsteueranteilZeile(t.StAnteilPflege, null));
            return zeilen;
        }

        double wert = stand.WertCtKwh.Value;
        double regel = Satz(katalog, DbWerte.GESETZ_STROMST_REGELSATZ, jahr,
                            StrompreisZerlegungModel.STROMSTEUER_REGELFALL);
        double reduziert = Satz(katalog, DbWerte.GESETZ_STROMST_REDUZIERT, jahr,
                                StrompreisZerlegungModel.STROMSTEUER_REDUZIERT);
        string jahrText = jahr.ToString(CultureInfo.InvariantCulture);

        string satz = Nah(wert, regel)
            ? string.Format(kultur, t.StAnteilRegel, jahrText, Zahl(regel, kultur))
            : Nah(wert, reduziert)
                ? string.Format(kultur, t.StAnteilReduziert, jahrText, Zahl(reduziert, kultur))
                : string.Format(kultur, t.StAnteilAbweichend, Zahl(regel, kultur), Zahl(reduziert, kultur), jahrText);

        zeilen.Add(new StromsteueranteilZeile(
            string.Format(kultur, t.StAnteilErfasst, stand.TraegerName, Zahl(wert, kultur),
                          stand.Aktiv ? t.StAnteilAktiv : t.StAnteilInaktiv) + " " + satz,
            null));

        if (!stand.Aktiv)
        {
            zeilen.Add(new StromsteueranteilZeile(t.StAnteilPflege, null));
            return zeilen;
        }

        bool reduziertEmpfohlen = ReduzierterSatzEmpfohlen(unternehmensart);
        string name = reduziertEmpfohlen ? t.StAnteilSatzReduziert : t.StAnteilSatzRegel;
        bool passt = Nah(wert, reduziertEmpfohlen ? reduziert : regel);
        zeilen.Add(passt
            ? new StromsteueranteilZeile(string.Format(kultur, t.StAnteilPasst, name), KohaerenzZustand.Ok)
            : new StromsteueranteilZeile(string.Format(kultur, t.StAnteilVorschlag, name), KohaerenzZustand.Abweichend));
        return zeilen;
    }

    /// <summary>
    /// Dieselbe Bedingung wie <c>EnergietraegerHuelle.ReduzierterSatzEmpfohlen</c> und
    /// <c>SteuerGutschriftRechner.ProduzierendesGewerbe</c>: produzierendes Gewerbe und
    /// Land-/Forstwirtschaft, ordinal verglichen.
    /// </summary>
    public static bool ReduzierterSatzEmpfohlen(string? unternehmensart)
        => string.Equals(unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE, StringComparison.Ordinal)
        || string.Equals(unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST, StringComparison.Ordinal);

    /// <summary>Ein Stromsteuersatz des Jahres in ct/kWh aus dem Katalog (EUR/MWh ÷ 10
    /// oder ct/kWh), sonst die Rückfallebene — wie die Schnellwahl des Trägerdialogs.</summary>
    private static double Satz(Func<string, int, GesetzParameter>? katalog, string schluessel, int jahr,
                               double rueckfall)
    {
        try
        {
            GesetzParameter? p = katalog?.Invoke(schluessel, jahr);
            if (p is not null && p.Wert.HasValue)
            {
                string e = (p.Einheit ?? "").Trim();
                if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
                    return p.Wert.Value / 10.0;
                if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                    return p.Wert.Value;
            }
        }
        catch { /* ein stummer Katalog kostet nur den Satzabgleich, nie die Zeile */ }
        return rueckfall;
    }

    private static bool Nah(double a, double b) => Math.Abs(a - b) <= TOLERANZ_CT_KWH;

    private static string Zahl(double wert, CultureInfo kultur) => wert.ToString("0.000", kultur);
}
