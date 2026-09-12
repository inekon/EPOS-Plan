using System;
using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Ein Eingabefeld des Modulkatalogs mit seinem aktuellen Wert.
/// </summary>
/// <remarks>
/// <para>Anders als beim <see cref="KatalogBrowserDialog"/> sind hier fast ALLE Felder
/// editierbar: Der Modulkatalog ist Browser und Editor in einem, „Speichern" schreibt
/// unmittelbar in die Stammtabelle. Nur der Bezeichner ist gesperrt — er ist der
/// Schlüssel des UPDATE.</para>
/// <para><see cref="LeerErlaubt"/> kommt aus dem Profil und ist BITGLEICH aus dem
/// Bestand übernommen (neun von zehn bei der Photovoltaik, null von fünf plus sechs von
/// sechs beim Stromspeicher).</para>
/// </remarks>
public sealed class ModulFeldwert
{
    public required string Schluessel { get; init; }
    public required string Bezeichnung { get; init; }
    public string Einheit { get; init; } = "";
    public string Wert { get; set; } = "";

    /// <summary>Textfeld, Zahlenfeld oder Ganzzahlfeld.</summary>
    public required WindowsFormsApplication1.BrowserFeldArt Art { get; init; }

    /// <summary>Darf das Feld beim Speichern leer sein?</summary>
    public bool LeerErlaubt { get; init; } = true;

    /// <summary>Nur lesbar — beim Bezeichner.</summary>
    public bool Gesperrt { get; init; }

    /// <summary>
    /// 0 = Bestandsfelder, 1 = AP3-Gerätetechnik bzw. „Eingang", 2 = „Wirkungsgrad"
    /// (nur der Wechselrichterkatalog, W6‑E‑2).
    /// </summary>
    public int Gruppe { get; init; }

    /// <summary>Der Feldname, den eine Prüfmeldung nennt — die Beschriftung ohne „:".</summary>
    public string Feldname => (Bezeichnung ?? "").TrimEnd(' ', ':');

    /// <summary>
    /// Die Optionen eines <c>Auswahl</c>-Feldes (Wert = Datenbankcode, Text = Beschriftung);
    /// leer bei allen anderen Feldarten. Paket B des PV-Ertragsmodells (Merge 5).
    /// </summary>
    public IReadOnlyList<(string Wert, string Text)> Optionen { get; init; } = Array.Empty<(string, string)>();
}

/// <summary>
/// Was der Modulkatalog beim Verlassen meldet.
/// </summary>
/// <param name="Bestaetigt">
/// Hat der Anwender „Beenden" gedrückt? Beide Vorläufer setzten dort
/// <c>DialogResult.OK</c>; <c>StromspeicherKontextMenuCtrl</c> wertet es aus.
/// </param>
/// <param name="Bezeichner">Der zuletzt gewählte Eintrag, leer wenn keiner.</param>
public sealed record ModulErgebnis(bool Bestaetigt, string Bezeichner);

/// <summary>
/// Der Satz Delegaten, mit dem die Hülle den Modulkatalog an ihre Stammtabelle hängt.
/// </summary>
public sealed class ModulKatalogWege
{
    /// <summary>
    /// <b>Die vollständige, UNGEFILTERTE Katalogliste</b> mit ihren Parameterspalten —
    /// <c>…StammCtrl.Katalogfilterzeilen()</c> (Anwenderentscheid W14a‑E‑10 vom
    /// 07.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>Bis hierher gab es drei Wege: <c>Liste</c> (alles), <c>Hersteller</c> (die
    /// Klapplistenwerte) und <c>ListeGefiltert</c> (eingeengt auf einen Hersteller) —
    /// und die zwei älteren Ausprägungen ließen sie weg, weshalb PV-Module und
    /// Stromspeicher GAR KEINEN Filter hatten. Gefiltert wird seither VOR dem Raster
    /// in <c>Katalogliste</c> über <c>Katalogfilter.Anwenden</c>; der Weg hierher
    /// liefert alles, was es gibt.</para>
    /// </remarks>
    public Func<IReadOnlyList<Katalogfilterzeile>>? Katalogzeilen { get; init; }

    /// <summary>Die Felder eines Moduls; <c>null</c>, wenn es das Modul nicht gibt.</summary>
    public Func<string, IReadOnlyList<ModulFeldwert>?>? Detail { get; init; }

    /// <summary>
    /// Schreibt die Felder — zweiter Parameter <c>true</c> = anlegen statt ändern,
    /// dritter der ursprüngliche Bezeichner (der WHERE-Schlüssel des UPDATE).
    /// </summary>
    public Func<IReadOnlyList<ModulFeldwert>, bool, string, KatalogSpeicherErgebnis>? Speichern { get; init; }

    /// <summary>Löscht ein Modul und sagt, warum es nicht ging.</summary>
    public Func<string, KatalogSpeicherErgebnis>? Loeschen { get; init; }
}
