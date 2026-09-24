namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die EDITOREN der Stufe Experte (Umsetzungskonzept Zapfprofilgenerator 5.1, 5.3;
//  Stufe Z4, Gruppe 2b): „Tagesgang bearbeiten" und „Zapfkategorien und Streuung". Beide
//  gehören zum Katalog, nicht zum Arbeitsstand des Projekts — ihr OK schreibt in EINER
//  Transaktion über die Hülle (TwwNutzungsartCtrl.TagesgangSpeichern bzw.
//  KategorienSpeichern); ist die Nutzungsart gesperrt, entsteht eine neue Katalogversion
//  (Status eigen), und der Zapfprofil-Dialog stellt die Zone auf sie um. Nur Daten: Der
//  Dialog kennt keinen Kern-Typ.
// =====================================================================================

/// <summary>
/// Ein Tagesgangsatz mit seinen Werten, wie der Editor ihn zeigt oder als Vorlage lädt: je
/// Tagtyp (Werktag, Samstag, Sonn-/Feiertag, Ruhetag) 24 Stundenanteile [-] und die Herkunft je
/// Tagtyp als Kurztext — nie ein Beleg.
/// </summary>
public sealed class ZapfprofilTagesgangsatzDaten
{
    /// <summary>Vier Tagtypen.</summary>
    public const int TAGTYPEN = 4;

    /// <summary>Vierundzwanzig Stunden.</summary>
    public const int STUNDEN = 24;

    /// <summary>Die Id des Satzes (<c>Tab_TwwTagesgangsatz_STAMM.ID</c>).</summary>
    public int Id { get; set; }

    /// <summary>Der neutrale Name samt Katalogversion („Satz · TEST-1").</summary>
    public string Name { get; set; } = "";

    /// <summary>Der Stand der Katalogzeile als Text (Auslieferung, eigen, Import).</summary>
    public string Status { get; set; } = "";

    /// <summary>Taugt der Satz als Vorlage (alle vier Tagtypen)? Sonst nennt <see cref="Sperrgrund"/> den Grund.</summary>
    public bool Waehlbar { get; set; } = true;

    /// <summary>Warum der Satz nicht als Vorlage taugt; leer, solange er es tut.</summary>
    public string Sperrgrund { get; set; } = "";

    /// <summary>Je Tagtyp 24 Anteile [-] (Zeile t = Tagtyp t + 1); ein fehlender Tagtyp trägt Nullen.</summary>
    public double[][] Anteile { get; set; } = Leer();

    /// <summary>Die Herkunft je Tagtyp als Kurztext; leer = der Satz führt den Tagtyp nicht.</summary>
    public string[] Herkunft { get; set; } = new[] { "", "", "", "" };

    /// <summary>Vier Tagtypen zu je 24 Nullen.</summary>
    public static double[][] Leer()
        => Enumerable.Range(0, TAGTYPEN).Select(_ => new double[STUNDEN]).ToArray();
}

/// <summary>
/// <b>Der Stand des Tagesgang-Editors beim Öffnen</b> (5.1): die Nutzungsart der Zone, der Satz,
/// den die Zone rechnet (ihre Expertenwahl, sonst der Satz der Nutzungsart), die Wochenfaktoren
/// Mo–So [-], die Vorlagen des Katalogs, ob „OK" an Ort und Stelle schreibt oder eine neue
/// Katalogversion anlegt (<see cref="Kopie"/> samt Grund und Vorschlag der Katalogversion) und —
/// wenn der Editor gar nicht schreiben kann — der benannte Grund.
/// </summary>
public sealed class ZapfprofilTagesgangDaten
{
    /// <summary>Sieben Wochentage.</summary>
    public const int WOCHENTAGE = 7;

    /// <summary>Die Nutzungsart, deren Tagesgang „OK" schreibt.</summary>
    public int IdNutzungsart { get; set; }

    /// <summary>Die Nutzungsart als Kurztext („Name · Katalogversion").</summary>
    public string Nutzungsart { get; set; } = "";

    /// <summary>Der Satz, mit dem der Editor öffnet.</summary>
    public ZapfprofilTagesgangsatzDaten Satz { get; set; } = new();

    /// <summary>Die sieben Wochenfaktoren Mo–So [-] der Nutzungsart.</summary>
    public double[] Wochenfaktoren { get; set; } = new double[WOCHENTAGE];

    /// <summary>Die Herkunft der Wochenfaktoren als Kurztext.</summary>
    public string WochengangHerkunft { get; set; } = "";

    /// <summary>Die Tagesgangsätze des Katalogs als Vorlagen; ein unvollständiger ist gesperrt mit Grund.</summary>
    public List<ZapfprofilTagesgangsatzDaten> Vorlagen { get; set; } = new();

    /// <summary>
    /// Schreibt „OK" eine NEUE Katalogversion (Nutzungsart oder Satz gesperrt: Auslieferung,
    /// benutzt)? Dann bearbeitet der Anwender eine eigene Kopie; <see cref="Sperrgrund"/> nennt den
    /// Grund, <see cref="KatalogversionVorschlag"/> eine freie Version.
    /// </summary>
    public bool Kopie { get; set; }

    /// <summary>Warum eine neue Katalogversion entsteht; leer, wenn an Ort und Stelle geschrieben wird.</summary>
    public string Sperrgrund { get; set; } = "";

    /// <summary>Eine freie Katalogversion für die Kopie — nur ein Vorschlag, der Schreibweg prüft erneut.</summary>
    public string KatalogversionVorschlag { get; set; } = "";

    /// <summary>Kann der Editor schreiben? Sonst nennt <see cref="Grund"/> den Grund (keine Zeile, kein Satz, keine Tabellen).</summary>
    public bool Verfuegbar { get; set; } = true;

    /// <summary>Der benannte Grund, warum der Editor nicht schreiben kann; leer, wenn er es kann.</summary>
    public string Grund { get; set; } = "";
}

/// <summary>
/// Die Eingabe von „OK" des Tagesgang-Editors: je Tagtyp 24 Werte und sieben Wochenfaktoren in
/// PROZENT, wie sie im Editor stehen (leer = 0), und die Katalogversion der Kopie. Die Hülle
/// normiert je Reihe auf Σ 1 über den Kern — eine Reihe ohne Summe oder mit negativem Wert lehnt
/// sie benannt ab.
/// </summary>
public sealed class ZapfprofilTagesgangEingabeDaten
{
    public int IdNutzungsart { get; set; }

    /// <summary>Je Tagtyp 24 Werte [%].</summary>
    public double[][] TagesgaengeProzent { get; set; } = ZapfprofilTagesgangsatzDaten.Leer();

    /// <summary>Sieben Wochenfaktoren Mo–So [%].</summary>
    public double[] WochenfaktorenProzent { get; set; } = new double[ZapfprofilTagesgangDaten.WOCHENTAGE];

    /// <summary>Die Katalogversion einer neuen Zeile; leer, wenn an Ort und Stelle geschrieben wird.</summary>
    public string Katalogversion { get; set; } = "";
}

/// <summary>
/// Das Ergebnis von „OK" des Tagesgang-Editors: geschrieben (oder nichts zu schreiben), die
/// Nutzungsart und der Satz, die jetzt die Werte tragen, ob eine neue Zeile entstand, und bei
/// einer Ablehnung die benannte Meldung.
/// </summary>
public sealed record ZapfprofilTagesgangErgebnis(bool Ok, int IdNutzungsart, int IdTagesgangsatz, bool NeueZeile,
                                                  ZapfprofilMeldung? Meldung);

/// <summary>
/// Die Prüfung der Zapfkategorien im Editor — dieselben Regeln wie der Schreibweg und der
/// Rechenweg (<c>Zapfkategoriensatz.Pruefen</c> im Kern): die Summe der Anteile, der Hinweis bei
/// Σ ≠ 1 (die Rechnung normiert, nicht blockierend) und der erste Verstoß als benannte
/// Ablehnung (<c>null</c> = gültig). Ohne Datenbank — sie läuft je Eingabe.
/// </summary>
public sealed class ZapfprofilKategorienPruefungDaten
{
    public double SummeAnteil { get; set; }
    public string Hinweis { get; set; } = "";
    public ZapfprofilMeldung? Ablehnung { get; set; }
}

/// <summary>
/// Die Zusätze des Kategorien-Editors zum Stand der Hülle (<see cref="ZapfprofilKategorienDaten"/>):
/// die Nutzungsart als Kurztext, der Vorschlag der Katalogversion einer Kopie und — wenn der Editor
/// nicht schreiben kann — der benannte Grund.
/// </summary>
public sealed class ZapfprofilKategorienEditorDaten
{
    public ZapfprofilKategorienDaten Stand { get; set; } = new();
    public string Nutzungsart { get; set; } = "";
    public string KatalogversionVorschlag { get; set; } = "";
    public bool Verfuegbar { get; set; } = true;
    public string Grund { get; set; } = "";
}

/// <summary>
/// Der Katalog nach einem Schreibweg der Editoren: die Nutzungsarten und die Tagesgangsätze, wie
/// der Zapfprofil-Dialog sie in Auswahl und aufgeklapptem Katalog zeigt.
/// </summary>
public sealed class ZapfprofilKatalogstandDaten
{
    public List<ZapfprofilNutzungsartDaten> Katalog { get; set; } = new();
    public List<ZapfprofilKatalogeintragDaten> Tagesgangsaetze { get; set; } = new();
}
