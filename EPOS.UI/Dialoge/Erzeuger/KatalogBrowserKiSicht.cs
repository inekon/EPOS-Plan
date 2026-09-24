using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Das Abbild einer Erzeugerverwaltung für den Hilfe-Assistenten (Welle #456) — EINE
/// Sichtklasse für alle VIER Ausprägungen des <see cref="KatalogBrowserDialog"/>:
/// Heizkessel, BHKW, Solarkollektoren, Pufferspeicher.
///
/// <para><b>Eine Feldtafel, keine Eigenschaft je Feld.</b> Der Browser führt seinen Satz
/// als Liste von <see cref="BrowserFeldwert"/> — je Feld des <c>KatalogBrowserProfil</c>
/// eine Zeile mit Schlüssel, Art und Wert als TEXT. Der Dialogkatalog erzeugt seine
/// Feldkarte aus demselben Profil (<c>KiDialoge.ErzeugerVerwaltung</c>); diese Klasse
/// beantwortet sie über den Schlüssel (<see cref="IKiFeldtafel"/>). Eine benannte
/// Eigenschaft je Feld — der Weg des <see cref="ModulKatalogKiSicht"/> — wäre die zweite
/// Feldliste von Hand, die dem Profil davonliefe; bei 21, 27, 14 und 6 Feldern ist das
/// keine Kleinigkeit.</para>
///
/// <para><b>Die Werte bleiben TEXT, die Sicht rechnet um</b> — mit denselben Regeln wie
/// der Baustein <c>Katalogfelder</c>, der die Felder zeichnet
/// (<see cref="Zahlen.ZahlParsen"/>, Ausgabe in der laufenden Kultur). Sonst stünde nach
/// einer Assistentensetzung eine Zahl im Feld, die der Speicherweg anders läse als eine
/// Eingabe von Hand.</para>
///
/// <para><b>Der Satz ist die Wahl der Liste</b> (<see cref="Satz"/>): Ihn zu setzen geht
/// denselben Weg wie ein Klick; trägt das Stammblatt ungespeicherte Änderungen, lehnt der
/// Dialog benannt ab.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jeder Zugriff ruft die Delegaten des Dialogs.</para>
/// </summary>
public sealed class KatalogBrowserKiSicht : IKiFeldtafel
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Sucht ein Feld des lebenden Satzes; <c>null</c>, wenn es keins gibt.</summary>
    public Func<string, BrowserFeldwert?>? Feldsuche { get; init; }

    /// <summary>
    /// Wird nach jedem Setzen gerufen — der Dialog führt dort seinen „geändert"-Zustand
    /// nach, wie nach einer Eingabe von Hand (<c>BeiFeldAenderung</c>).
    /// </summary>
    public Action? Gesetzt { get; init; }

    /// <summary>Der gewählte Satz (Bezeichner der Fokuszeile).</summary>
    public Func<string>? SatzLesen { get; init; }

    /// <summary>
    /// Wählt einen Satz; Rückgabe: der Grund, warum der Dialog den Wechsel ablehnt, sonst
    /// <c>null</c>.
    /// </summary>
    public Func<string, string?>? SatzSetzen { get; init; }

    /// <summary>Die Sätze der Liste als Einträge des Wahlfeldes.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    // =====================================================================
    //  Der Satz — das Wahlfeld „satz"
    // =====================================================================

    /// <summary>
    /// Der gewählte Katalogsatz. Ihn zu setzen wählt die Zeile — derselbe Weg wie ein
    /// Klick; ein abgelehnter Wechsel kommt als benannte Ausnahme zurück, damit der
    /// Assistent nicht „gesetzt" meldet, wo nichts geschah.
    /// </summary>
    public string Satz
    {
        get => SatzLesen?.Invoke() ?? "";
        set
        {
            string? grund = SatzSetzen?.Invoke(value ?? "");
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    /// <summary>Die Sätze der Liste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> SatzWahl
        => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Feldtafel — ein Feld je Profilschlüssel
    // =====================================================================

    /// <inheritdoc/>
    /// <remarks>
    /// Ein Zahlenfeld, dessen Text sich nicht als Zahl lesen lässt (eine abgeleitete
    /// Anzeige), kommt als Text heraus — so, wie es dasteht, statt leer.
    /// </remarks>
    public object? Lesen(string schluessel)
    {
        BrowserFeldwert? feld = Feldsuche?.Invoke(schluessel);
        return feld is null ? null : BrowserFeldwertWandler.Lesen(feld);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Nicht editierbar heißt nicht editierbar</b> — dieselbe Wache wie im Baustein
    /// <c>Katalogfelder</c>: Der Bezeichner ist der Schlüssel des <c>UPDATE</c>, die
    /// Investition je kWel des BHKW eine abgeleitete Größe. Der Katalog deklariert beide
    /// ohnehin nur lesend; diese Zeile ist die zweite Sicherung.
    /// </remarks>
    public void Setzen(string schluessel, object? wert)
    {
        BrowserFeldwert? feld = Feldsuche?.Invoke(schluessel);
        if (feld is null || !feld.Editierbar) return;

        BrowserFeldwertWandler.Setzen(feld, wert);
        Gesetzt?.Invoke();
    }
}
