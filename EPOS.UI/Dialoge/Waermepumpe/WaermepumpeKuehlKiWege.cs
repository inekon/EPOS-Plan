using EPOS.UI.Dienste;
using KiKern;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Die fünf Felder der Gruppe „Kühlbetrieb" für den Hilfe-Assistenten (Stufe KU2 Welle 3;
/// Kühlkonzept 8.2, Entscheide E15, E33, E34) — EIN Weg für die zwei Sichtklassen, deren Masken
/// die Konfiguration der Wärmepumpe zeigen: <see cref="WaermepumpeAnlageKiSicht"/> (Anlagendialog)
/// und <c>KomponentenKonfigurationKiSicht</c> (Konfiguration der Simulation).
///
/// <para><b>Die Regeln stehen nicht hier.</b> Sperrgrund, Abweichung des Kühlträgers, Rückfall der
/// Abrechnungsart sowie Prozent und Anteil sind die statischen Wege von
/// <see cref="WaermepumpeKonfiguration"/>, die auch die Maske ruft. Hier steht nur, was der
/// Assistent darüber hinaus braucht: die Einträge der drei Wahlfelder und die BENANNTE Absage,
/// wo die Maske weich sperrt oder der Wirt keinen Kühlbetrieb anbietet.</para>
/// </summary>
public static class WaermepumpeKuehlKiWege
{
    /// <summary>
    /// Der Feldsatz, in den ein Kühlfeld geschrieben wird — oder die benannte Absage, wenn keiner
    /// offen ist oder der Wirt keine Kühlgaben reicht (dann zeigt die Maske keine Gruppe
    /// „Kühlbetrieb", und ein gesetzter Wert stünde, wo niemand ihn sieht).
    /// </summary>
    public static WaermepumpeAnlageDaten Setzbar(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben)
    {
        if (daten is not null && gaben is not null) return daten;
        throw new InvalidOperationException(
            WindowsFormsApplication1.MyResource.Resource.KI_DLG_WPA_KUEHLUNG_NICHT_EINSTELLBAR);
    }

    /// <summary>„Maschine auch zum Kühlen benutzen" setzen; ein gesperrter Kühlbetrieb nennt seinen Grund.</summary>
    public static void KuehlbetriebSetzen(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben, bool wert)
    {
        WaermepumpeAnlageDaten d = Setzbar(daten, gaben);
        if (wert && WaermepumpeKonfiguration.KuehlbetriebSperrgrund(d, gaben) is string grund)
            throw new InvalidOperationException(grund);
        d.Kuehlbetrieb = wert;
    }

    /// <summary>Den Kühl-Vorlauf setzen; leer = kleinster Stützwert.</summary>
    public static void VorlaufSetzen(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben, int? vorlauf)
        => Setzbar(daten, gaben).KuehlVorlauf = vorlauf;

    /// <summary>Die Stützstellen der Kühlkennlinie, die die Maske zur Wahl stellt — ohne die gesperrten.</summary>
    public static IReadOnlyList<KiWahleintrag> VorlaufWahl(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben)
        => daten is not null && gaben?.Vorlaeufe is { } vorlaeufe
            ? KiMaskenanmeldung.Eintraege(vorlaeufe(daten.IdWp).Where(e => e.Sperrgrund.Length == 0),
                                          e => e.Vorlauf, e => e.Vorlauf + " °C")
            : Array.Empty<KiWahleintrag>();

    /// <summary>Der Hilfsstromanteil in PROZENT, wie die Maske ihn zeigt.</summary>
    public static double? HilfsstromProzent(WaermepumpeAnlageDaten? daten)
        => WaermepumpeKonfiguration.AnteilAlsProzent(daten?.KuehlHilfsstromanteil);

    /// <summary>Den Hilfsstromanteil in Prozent setzen; gespeichert wird der Anteil.</summary>
    public static void HilfsstromSetzen(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben, double? prozent)
        => Setzbar(daten, gaben).KuehlHilfsstromanteil = WaermepumpeKonfiguration.ProzentAlsAnteil(prozent);

    /// <summary>Den Stromträger des Kältestroms setzen (leer = wie Heizbetrieb) — samt Rückfall der Abrechnungsart.</summary>
    public static void KuehltraegerSetzen(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben, int? id)
        => WaermepumpeKonfiguration.KuehltraegerSetzen(Setzbar(daten, gaben), id, gaben);

    /// <summary>Die Stromträger des Projekts, die die Maske für den Kältestrom zur Wahl stellt.</summary>
    public static IReadOnlyList<KiWahleintrag> TraegerWahl(WaermepumpeKuehlGaben? gaben)
        => KiMaskenanmeldung.Eintraege(gaben?.Stromtraeger, e => e.Id, e => e.Text);

    /// <summary>Die Abrechnungsart als Wahl: 0 = anteilig am Netzbezug, 1 = eigener Zähler.</summary>
    public static int Abrechnung(WaermepumpeAnlageDaten? daten)
        => daten is not null ? WaermepumpeKonfiguration.AbrechnungWahl(daten)
                             : WaermepumpeKonfiguration.ABRECHNUNG_ANTEILIG;

    /// <summary>Die Abrechnungsart setzen — „eigener Zähler" nur bei abweichendem Kühlträger (E34).</summary>
    public static void AbrechnungSetzen(WaermepumpeAnlageDaten? daten, WaermepumpeKuehlGaben? gaben, int wahl)
    {
        WaermepumpeAnlageDaten d = Setzbar(daten, gaben);
        if (wahl == WaermepumpeKonfiguration.ABRECHNUNG_ZAEHLER &&
            !WaermepumpeKonfiguration.KuehltraegerWeichtAb(d, gaben))
            throw new InvalidOperationException(
                WindowsFormsApplication1.MyResource.Resource.KI_DLG_WPA_ABRECHNUNG_OHNE_TRAEGER);
        WaermepumpeKonfiguration.AbrechnungSetzen(d, wahl);
    }

    /// <summary>Die zwei Abrechnungsarten der Maske — dieselben Texte wie ihre Optionsgruppe.</summary>
    public static IReadOnlyList<KiWahleintrag> AbrechnungWahl()
    {
        var texte = new WaermepumpeKonfigurationTexte();
        return KiMaskenanmeldung.Eintraege(
            new[] { (WaermepumpeKonfiguration.ABRECHNUNG_ANTEILIG, texte.OptionAnteilig),
                    (WaermepumpeKonfiguration.ABRECHNUNG_ZAEHLER, texte.OptionZaehler) },
            e => e.Item1, e => e.Item2);
    }
}
