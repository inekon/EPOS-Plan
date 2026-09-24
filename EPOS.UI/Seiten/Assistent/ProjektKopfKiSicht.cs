using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Seiten.Assistent;

/// <summary>
/// Das flache Abbild des Projektkopfs für den Hilfe-Assistenten (Welle #458, Stufe 2) —
/// Schritt 1 des Assistenten „Neues Projekt" (<see cref="ProjektKopfSeite"/>).
///
/// <para><b>Warum eine Sichtklasse und nicht das Daten-Objekt selbst.</b> Die Seite
/// schreibt an Ort und Stelle in <see cref="ProjektKopfDaten"/>, und für Kunde,
/// Bearbeiter und Beschreibung reicht diese Sicht die Eigenschaft unverändert durch.
/// Zwei Felder brauchen aber den Weg der Seite und nicht die nackte Eigenschaft:</para>
/// <list type="bullet">
///   <item><description>Die <b>Klimaregion</b> steht im Kopf DOPPELT — als Id und als
///     Name (Altprojekte führen nur den Namen). Die Seite setzt beide zugleich
///     (<c>KlimaGewaehlt</c>); eine gesetzte Id neben einem alten Namen wäre ein
///     Widerspruch, den der Assistent erzeugt hätte.</description></item>
///   <item><description>Der <b>Projektname</b> steht im Bearbeiten-Modus fest
///     (<see cref="ProjektKopfDaten.NameAenderbar"/>, auf der Maske nur lesbar). Das
///     Daten-Objekt nähme ihn trotzdem an; die Sicht lehnt benannt ab.</description></item>
/// </list>
///
/// <para><b>Kein Speicherweg.</b> Angelegt wird das Projekt mit „Fertig" am Ende des
/// Assistenten, und das bleibt der Klick des Anwenders.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class ProjektKopfKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — die Seite setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Der Projektkopf, in den die Seite schreibt.</summary>
    public Func<ProjektKopfDaten?>? Kopf { get; init; }

    public Func<int?>? KlimaLesen { get; init; }
    public Action<int?>? KlimaSetzen { get; init; }

    /// <summary>Die wählbaren Klimaregionen (Stamm-Id, Name).</summary>
    public Func<IReadOnlyList<(int Id, string Text)>>? Klimaregionen { get; init; }

    /// <summary>Der Grund, warum der Name feststeht (Bearbeiten-Modus).</summary>
    public Func<string>? NameFestGrund { get; init; }

    // =====================================================================
    //  Die fünf Felder
    // =====================================================================

    /// <summary>Der Projektname; im Bearbeiten-Modus nur lesbar.</summary>
    public string Name
    {
        get => Kopf?.Invoke()?.Name ?? "";
        set
        {
            ProjektKopfDaten? kopf = Kopf?.Invoke();
            if (kopf is null) return;
            if (!kopf.NameAenderbar)
                throw new InvalidOperationException(NameFestGrund?.Invoke() ?? "");
            kopf.Name = value ?? "";
        }
    }

    /// <summary>Die Klimaregion als Stamm-Id — gesetzt über den Weg der Seite.</summary>
    public int? Klimaregion
    {
        get => KlimaLesen?.Invoke();
        set => KlimaSetzen?.Invoke(value);
    }

    /// <summary>Die Klimaregionen der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> KlimaregionWahl
        => EPOS.UI.Dienste.KiMaskenanmeldung.Eintraege(
               Klimaregionen?.Invoke() ?? Array.Empty<(int, string)>(), k => k.Id, k => k.Text);

    public string Kunde
    {
        get => Kopf?.Invoke()?.Kunde ?? "";
        set { if (Kopf?.Invoke() is ProjektKopfDaten k) k.Kunde = value ?? ""; }
    }

    public string Bearbeiter
    {
        get => Kopf?.Invoke()?.Bearbeiter ?? "";
        set { if (Kopf?.Invoke() is ProjektKopfDaten k) k.Bearbeiter = value ?? ""; }
    }

    public string Beschreibung
    {
        get => Kopf?.Invoke()?.Beschreibung ?? "";
        set { if (Kopf?.Invoke() is ProjektKopfDaten k) k.Beschreibung = value ?? ""; }
    }
}
