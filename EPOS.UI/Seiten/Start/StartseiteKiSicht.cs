using KiKern;

namespace EPOS.UI.Seiten.Start;

/// <summary>
/// Das flache Abbild der Startseite für den Hilfe-Assistenten (Welle #458, Stufe 2) —
/// zwei Einstellwerte des offenen Projekts.
///
/// <para><b>Die Klimaregion</b> des Kopfbandes (<c>Suchauswahl</c>) und <b>die Solarart</b>
/// der Solarthermiekachel (<see cref="ErzeugerReiter"/>, Profil oder Ganglinie). Beide
/// liegen in privaten Feldern der Seite — deshalb eine Sichtklasse. Die Wahl der
/// Klimaregion schreibt erst der Knopf „Speichern" daneben; genau der ist der
/// Speicherweg der Maske. Die Solarart wirkt sofort, wie der Klick.</para>
///
/// <para><b>Was draußen bleibt:</b> die Projekt- und Variantenwahl im Kopfband. Sie
/// öffnet ein anderes Projekt — eine Navigation, kein Einstellwert. Ebenso die Kacheln
/// und die Reiter: Sie öffnen Masken, sie stellen nichts ein.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class StartseiteKiSicht
{
    public Func<int?>? KlimaLesen { get; init; }
    public Action<int?>? KlimaSetzen { get; init; }

    /// <summary>Die Stammregionen der Suchauswahl (Id, Name).</summary>
    public Func<IReadOnlyList<(int Id, string Text)>>? Klimaregionen { get; init; }

    /// <summary><c>true</c> = Ganglinie, <c>false</c> = Kollektorprofil.</summary>
    public Func<bool>? GanglinieLesen { get; init; }
    public Action<bool>? GanglinieSetzen { get; init; }

    /// <summary>Die zwei Beschriftungen der Weiche: Platz 0 Profil, Platz 1 Ganglinie.</summary>
    public Func<IReadOnlyList<string>>? Solararten { get; init; }

    /// <summary>
    /// Die gewählte Klimaregion des offenen Projekts als Stamm-Id; geschrieben wird sie
    /// mit dem Knopf „Speichern" daneben (<c>dialog_speichern</c>).
    /// </summary>
    public int? Klimaregion
    {
        get => KlimaLesen?.Invoke();
        set => KlimaSetzen?.Invoke(value);
    }

    /// <summary>Die Stammregionen der Suchauswahl (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> KlimaregionWahl
        => EPOS.UI.Dienste.KiMaskenanmeldung.Eintraege(
               Klimaregionen?.Invoke() ?? Array.Empty<(int, string)>(), k => k.Id, k => k.Text);

    /// <summary>
    /// Die Weiche der Solarthermiekachel als Listenplatz: 0 = Profil, 1 = Ganglinie.
    /// Sie entscheidet, welche Maske die Kachel öffnet.
    /// </summary>
    public int? Solarart
    {
        get => (GanglinieLesen?.Invoke() ?? false) ? 1 : 0;
        set => GanglinieSetzen?.Invoke(value == 1);
    }

    /// <summary>Die zwei Stellungen der Weiche (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> SolarartWahl
        => EPOS.UI.Dienste.KiMaskenanmeldung.Eintraege(Solararten?.Invoke());
}
