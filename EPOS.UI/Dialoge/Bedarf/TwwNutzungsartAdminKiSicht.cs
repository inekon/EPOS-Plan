using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild des Katalogdialogs „Brauchwasser-Nutzungsarten" für den Hilfe-Assistenten
/// (Umsetzungskonzept Zapfprofilgenerator 5.4; Stufe Z4, Gruppe 3) — Muster der Verwaltungen
/// (<c>ModulKatalogKiSicht</c>, <c>BedarfAdminKiSicht</c>).
///
/// <para><b>Die Maske führt keinen Einstellwert</b>: Das Stammblatt ist lesend, geschrieben wird
/// über den Editor („Neu…", „Ändern…", „Speichern unter…"), der sich selbst anmeldet, über
/// „Tagesgang…" und „Kategorien…" (ebenso) und über Löschen und Import — Handlungen, die Zeilen
/// anlegen oder wegnehmen und Klicks des Anwenders bleiben (KI‑D‑Q11). Freigegeben ist sie
/// trotzdem: Die WAHL der Zeile ist <c>satzwahl</c> (setzen heißt wählen, wie ein Klick — auch aus
/// einer Zeile der Auslieferung heraus), und Katalogversion, Stand und Sperrgrund der gewählten
/// Zeile stehen als Anzeige daneben, damit <c>dialog_lesen</c> nennt, woran der Anwender
/// arbeitet.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class TwwNutzungsartAdminKiSicht
{
    /// <summary>Liest die Id der gewählten Zeile; <c>null</c> = keine.</summary>
    public Func<int?>? SatzLesen { get; init; }

    /// <summary>Wählt eine Zeile nach ihrer Id; Rückgabe der Grund einer Ablehnung, sonst <c>null</c>.</summary>
    public Func<int?, string?>? SatzSetzen { get; init; }

    /// <summary>Die Zeilen der Liste: Id und „Name · Katalogversion".</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    public Func<string>? KatalogversionLesen { get; init; }
    public Func<string>? StandLesen { get; init; }
    public Func<string>? SperrgrundLesen { get; init; }

    /// <summary>Die Nutzungsarten des Katalogs (KI‑D‑Q6) — Schlüssel ist die Id.</summary>
    public IReadOnlyList<KiWahleintrag> SatzWahl => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Die gewählte Nutzungsart. Sie zu setzen WÄHLT sie in der Liste — derselbe Weg wie ein Klick;
    /// das Stammblatt zieht nach.
    /// </summary>
    public int? Satz
    {
        get => SatzLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(SatzSetzen, value);
    }

    /// <summary>Die Katalogversion der gewählten Zeile — Anzeige.</summary>
    public string Katalogversion => KatalogversionLesen?.Invoke() ?? "";

    /// <summary>Katalogversion, Stand und Bezugsart der gewählten Zeile — Anzeige.</summary>
    public string Stand => StandLesen?.Invoke() ?? "";

    /// <summary>Warum „Ändern…" nur als „Speichern unter" geht (Auslieferung, benutzt) — Anzeige; leer = frei.</summary>
    public string Sperrgrund => SperrgrundLesen?.Invoke() ?? "";
}
