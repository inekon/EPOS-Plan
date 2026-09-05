using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EINE Kachel des Komponentenschritts (iU9-W16a.3) — der Bestand einer Komponente
/// im Projekt, so wie ihn der Anwender sieht und umschaltet.
///
/// <para><b>Woher die Werte kommen.</b> Aus
/// <c>EPOS.Kern/Controller/KomponentenBestandCtrl</c> (K1): <see cref="Kennung"/>,
/// <see cref="SeitenIndex"/>, <see cref="Anzahl"/>, <see cref="Namen"/> und der
/// Anfangswert von <see cref="An"/> sind sein <c>Eintrag</c>; die Hülle setzt sie
/// um. Die Komponente kennt keine Datenbank (Hausregel EPOS.UI).</para>
///
/// <para><b>Sie wird AN ORT UND STELLE bearbeitet.</b> <see cref="An"/> ist das
/// einzige veränderliche Feld — dieselbe Mechanik wie die geteilten Listen der
/// Bedarfsseiten (iU9-W9.0a): Der Assistent hält die Liste, die Seite schaltet
/// darin um, und der Rahmen liest sie danach ohne Rückweg.</para>
/// </summary>
public sealed class KomponentenZeile
{
    /// <summary>Kennung der Komponente (<c>KomponentenBestandCtrl.GEBAEUDE</c> … <c>PUFFER</c>).</summary>
    public int Kennung { get; set; }

    /// <summary>Beschriftung der Kachel — der <c>Titel</c> der <c>AktionsKarte</c>.</summary>
    public string Titel { get; set; } = "";

    /// <summary>
    /// Index der Assistentenseite (<c>WizardItemClass</c>) oder
    /// <see cref="OHNE_SEITE"/> für Brauchwasser und Pufferspeicher — sie haben
    /// keine Seite und sind deshalb nur Anzeige.
    /// </summary>
    public int SeitenIndex { get; set; }

    /// <summary>Zahl der gefundenen Einträge — die Zahl im Satz „{0} im Projekt".</summary>
    public int Anzahl { get; set; }

    /// <summary>
    /// Namen der gefundenen Einträge — der Klartext der Rückfrage beim Abwählen
    /// („Beim Speichern werden N Einträge gelöscht: …").
    /// </summary>
    public IReadOnlyList<string> Namen { get; set; } = System.Array.Empty<string>();

    /// <summary>Zeigt die Kachel „im Projekt"? Das einzige veränderliche Feld.</summary>
    public bool An { get; set; }

    /// <summary>Kennung ohne eigene Assistentenseite (<c>KomponentenBestandCtrl.OHNE_SEITE</c>).</summary>
    public const int OHNE_SEITE = -1;

    /// <summary>Hat die Komponente eine Assistentenseite — also: lässt sie sich umschalten?</summary>
    public bool HatSeite => SeitenIndex != OHNE_SEITE;
}
