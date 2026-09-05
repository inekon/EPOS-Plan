using System;
using System.Collections;
using System.Collections.Generic;

using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// EIN Punkt des Menuebands (iU9-W16c.1) — der Ersatz fuer ein
/// <c>ToolStripMenuItem</c> samt seinem Ereignishandler.
///
/// <para><b>Was er traegt.</b> Den sprachneutralen <see cref="Name"/> des
/// Vorlaeufers (er steht in <c>help_mapping.txt</c> und in den Protokollen und
/// bleibt deshalb lesbar), den <see cref="TextSchluessel"/> fuer die
/// Beschriftung, das <see cref="Ziel"/> als <c>Seitenschluessel</c>, ein
/// mitzugebendes <see cref="Argument"/>, ein <see cref="Bild"/> und ein
/// <see cref="Kuerzel"/>. Untermenues haengen als <see cref="Untereintraege"/>
/// darunter; ein <see cref="Trenner"/> traegt nichts als seinen Namen.</para>
///
/// <para><b>Kein Delegat.</b> Der Punkt weiss nicht, was beim Klick geschieht —
/// er nennt einen Schluessel. Das Menueband meldet ihn, und
/// <c>Hauptfenster.Springe</c> entscheidet: Ansicht wechseln oder den Weg der
/// Huelle gehen. Damit ist die Tabelle Daten und laesst sich erzeugen
/// (Auflage R-W16-8) und pruefen (Nachweis N4).</para>
///
/// <para><b>Warum <see cref="IEnumerable{T}"/> und <see cref="Add"/>.</b> Nur so
/// laesst sich der Baum als geschachtelte Sammelinitialisierung schreiben — das
/// ist die Form, die das Erzeugerskript ausgibt und die man lesen kann wie den
/// Menuebaum selbst.</para>
///
/// <para><b>Die Beschriftung kommt zur Laufzeit.</b> <see cref="Text"/> liest
/// <c>MyResource</c> mit der aktuellen Oberflaechenkultur; ein fehlender
/// Schluessel liefert den Schluessel selbst zurueck statt einer leeren Zeile —
/// so faellt eine Luecke auf, statt zu verschwinden.</para>
/// </summary>
public sealed class Menuepunkt : IEnumerable<Menuepunkt>
{
    private readonly List<Menuepunkt> _kinder = new();

    /// <summary>Ein gewoehnlicher Punkt, gegebenenfalls mit Untereintraegen.</summary>
    public Menuepunkt(string name, string textSchluessel, string ziel,
                      string argument = "", string bild = "", string kuerzel = "",
                      bool rechtsBuendig = false)
    {
        Name = name;
        TextSchluessel = textSchluessel;
        Ziel = ziel;
        Argument = argument;
        Bild = bild;
        Kuerzel = kuerzel;
        RechtsBuendig = rechtsBuendig;
    }

    /// <summary>Ein Trennstrich — er traegt weder Text noch Ziel.</summary>
    public static Menuepunkt Trennstrich(string name) =>
        new Menuepunkt(name, "", "") { Trenner = true };

    /// <summary>
    /// Der Bezeichner des Vorlaeufers (<c>MenuItem_ProjektNeu</c>) —
    /// sprachneutral und der Anker fuer <c>help_mapping.txt</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>Der <c>MyResource</c>-Schluessel der Beschriftung; leer beim Trenner.</summary>
    public string TextSchluessel { get; }

    /// <summary>
    /// Der <c>Seitenschluessel</c>, den ein Klick meldet. LEER heisst: Der Punkt
    /// klappt nur auf (Untermenue) oder ist ein Trenner.
    /// </summary>
    public string Ziel { get; }

    /// <summary>Zusatzangabe zum Ziel — im Bestand nur <c>"CEC"</c> des PV-Imports.</summary>
    public string Argument { get; }

    /// <summary>Bildname ohne Endung unter <c>wwwroot/bilder/menue/</c>; leer = kein Bild.</summary>
    public string Bild { get; }

    /// <summary>Tastenkuerzel als Anzeigetext (nur <c>"F1"</c> im Bestand).</summary>
    public string Kuerzel { get; }

    /// <summary>Ein Trennstrich statt eines Punktes.</summary>
    public bool Trenner { get; private init; }

    /// <summary>
    /// Der Punkt steht am RECHTEN Rand der Leiste (Anwenderwunsch 05.09.2026,
    /// <b>W16c‑E‑4</b>) — im Bestand standen die zwei Sprachpunkte
    /// „Deutsch"/„Englisch" rechtsbuendig am Ende des <c>MenuStrip</c>, und der
    /// Kopf „Sprache" aus W16c‑E‑2 hat ihre Stelle geerbt.
    ///
    /// <para><b>Nur die Optik wandert, nicht die Ordnung.</b> Das Band setzt
    /// dafuer eine Klasse mit <c>margin-left: auto</c>; der Punkt bleibt an
    /// SEINER Stelle im Markup. Damit sind Tastaturreihenfolge (← → ueber die
    /// vier Koepfe, Ende = „Sprache"), Sprachausgabe und der Nachweis N4
    /// unveraendert — ein umsortiertes Markup haette beides verschoben.</para>
    /// </summary>
    public bool RechtsBuendig { get; }

    /// <summary>Das Untermenue; leer, wenn der Punkt unmittelbar handelt.</summary>
    public IReadOnlyList<Menuepunkt> Untereintraege => _kinder;

    /// <summary>Hat der Punkt ein Untermenue?</summary>
    public bool Klappt => _kinder.Count > 0;

    /// <summary>Die Beschriftung in der aktuellen Oberflaechensprache.</summary>
    public string Text => TextFuer(TextSchluessel);

    /// <summary>
    /// Nachschlagen im Ressourcenkatalog. Ein fehlender Schluessel liefert den
    /// Schluessel — eine leere Menuezeile waere im Betrieb nicht zu deuten.
    /// </summary>
    public static string TextFuer(string schluessel)
    {
        if (string.IsNullOrEmpty(schluessel)) return "";

        string? wert = null;
        try { wert = Resource.ResourceManager.GetString(schluessel); }
        catch { /* kein Katalog: dann der Schluessel */ }

        return string.IsNullOrEmpty(wert) ? schluessel : wert!;
    }

    /// <summary>Haengt einen Untereintrag an (Sammelinitialisierung).</summary>
    public void Add(Menuepunkt kind) => _kinder.Add(kind);

    /// <inheritdoc />
    public IEnumerator<Menuepunkt> GetEnumerator() => _kinder.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
