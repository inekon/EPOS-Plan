using KiKern;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Das flache Abbild des Kennlinieneditors für den Hilfe-Assistenten (Welle #458,
/// Stufe 2) — die Überlagerung „Kenndaten" der Wärmepumpen-Verwaltung und der
/// Wärmepumpen-Anlage.
///
/// <para><b>Eine ÜBERLAGERUNG mit eigenem Arbeitsstand.</b> Der Editor bearbeitet eine
/// KOPIE der Stützstellen, die sein Wirt hereinreicht; „OK" gibt sie zurück, und der
/// Wirt gleicht sie im Kern ab (<c>KenndatenCtrl.Abgleichen</c>). Der Assistent setzt
/// deshalb in genau diese Kopie — dieselben Zahlen, die vor dem Anwender stehen. Einen
/// Speicherweg meldet der Editor nicht an: „OK" bleibt der Klick des Anwenders, und
/// <c>dialog_speichern</c> lehnt benannt ab (Vorbild die Überlagerung
/// „Anlagenwerte" der Photovoltaik).</para>
///
/// <para><b>Die Stützstellen sind SPALTEN</b> über den Zeilen der GEWÄHLTEN
/// Vorlaufstufe — nur die stehen im Raster. Wer eine andere Stufe meint, wählt sie
/// über <see cref="Vorlauf"/>; das ist derselbe Klick wie in der Vorlaufliste. Das
/// Zeilenkennzeichen ist „Vorlauf/Temperatur" (<see cref="KennlinienZeile.Kennzeichen"/>).</para>
///
/// <para><b>Anlegen bleibt beim Anwender.</b> „Neue Vorlauftemperatur" und „Daten
/// übernehmen" legen Stufen und Zeilen an (KI‑D‑Q11); der Assistent füllt die Felder
/// davor, den Knopf drückt der Anwender.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class KennlinienKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Editor setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? VorlaufLesen { get; init; }

    /// <summary>Wählt eine Vorlaufstufe; eine unbekannte lehnt der Editor mit Grund ab.</summary>
    public Action<int?>? VorlaufSetzen { get; init; }

    /// <summary>Die Stufen der Vorlaufliste.</summary>
    public Func<IReadOnlyList<int>>? Vorlaufstufen { get; init; }

    public Func<int?>? NeuerVorlaufLesen { get; init; }
    public Action<int?>? NeuerVorlaufSetzen { get; init; }

    /// <summary>Die Zeilen der gewählten Stufe — die, die das Raster zeigt.</summary>
    public Func<IReadOnlyList<KennlinienZeile>>? ZeilenLesen { get; init; }

    public Func<int?>? NeuTemperaturLesen { get; init; }
    public Action<int?>? NeuTemperaturSetzen { get; init; }

    public Func<double?>? NeuCopLesen { get; init; }
    public Action<double?>? NeuCopSetzen { get; init; }

    public Func<double?>? NeuPthermLesen { get; init; }
    public Action<double?>? NeuPthermSetzen { get; init; }

    // =====================================================================
    //  Die Vorlaufstufe — Wahl der Liste
    // =====================================================================

    /// <summary>
    /// Die gewählte Vorlaufstufe [°C]. Sie zu setzen wählt die Stufe in der Liste —
    /// das Raster zeigt danach deren Stützstellen.
    /// </summary>
    public int? Vorlauf
    {
        get => VorlaufLesen?.Invoke();
        set => VorlaufSetzen?.Invoke(value);
    }

    /// <summary>Die Stufen der Vorlaufliste (KI‑D‑Q6) — Schlüssel ist die Temperatur.</summary>
    public IReadOnlyList<KiWahleintrag> VorlaufWahl
        => EPOS.UI.Dienste.KiMaskenanmeldung.Eintraege(
               Vorlaufstufen?.Invoke() ?? Array.Empty<int>(), s => s);

    /// <summary>Das Feld „Neue Vorlauftemperatur" — angelegt wird mit dem Knopf daneben.</summary>
    public int? NeuerVorlauf
    {
        get => NeuerVorlaufLesen?.Invoke();
        set => NeuerVorlaufSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Die Stützstellen der gewählten Stufe — Spalten
    // =====================================================================

    /// <summary>
    /// Die Zeilen der gewählten Vorlaufstufe — dieselben Objekte, an denen die
    /// Eingabefelder des Rasters hängen.
    /// </summary>
    public IReadOnlyList<KennlinienZeile> Zeilen
        => ZeilenLesen?.Invoke() ?? Array.Empty<KennlinienZeile>();

    // =====================================================================
    //  Die Gruppe „Neue Stützstelle"
    // =====================================================================

    public int? NeuTemperatur
    {
        get => NeuTemperaturLesen?.Invoke();
        set => NeuTemperaturSetzen?.Invoke(value);
    }

    public double? NeuCop
    {
        get => NeuCopLesen?.Invoke();
        set => NeuCopSetzen?.Invoke(value);
    }

    public double? NeuPtherm
    {
        get => NeuPthermLesen?.Invoke();
        set => NeuPthermSetzen?.Invoke(value);
    }
}
