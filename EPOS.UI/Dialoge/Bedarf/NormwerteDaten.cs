using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>Die Quelle, aus der „Normwerte einlesen…" liest (Konzept Nutzungsprofile 6.5).</summary>
public enum NormwerteQuelle
{
    /// <summary>Eine CSV des Anwenders im Format 6.4 mit der Spalte <c>Aussenluft_Person</c>.</summary>
    Csv = 0,

    /// <summary>Eine Projektdatei (<c>.sqproj</c>), deren Nutzungsprofile die DIN-Nummer tragen.</summary>
    Projektdatei = 1,
}

/// <summary>
/// <b>Ein Profil der Quelle, das ein DIN-Profil trifft</b> — die Zeile der Vorschau, ohne Fachklasse des Kerns.
/// </summary>
/// <param name="Nummer">Die DIN-Nummer; leer bei den Wohnprofilen.</param>
/// <param name="Bezeichner">Der Name des DIN-Profils.</param>
/// <param name="HatWerte">Trägt das DIN-Profil schon Werte (dann nur mit „überschreiben")?</param>
/// <param name="Eingelesen">Stammen die vorhandenen Werte aus einem früheren Einlesen?</param>
/// <param name="Abweichungen">Die Spalten, deren vorhandener Wert vom neuen abweicht.</param>
public sealed record NormwerteTreffer(string Nummer, string Bezeichner, bool HatWerte, bool Eingelesen,
                                      IReadOnlyList<string> Abweichungen);

/// <summary>
/// <b>Die Vorschau eines Einlesens</b> — schreibt nichts. <paramref name="Ok"/> = lesbar und mindestens ein Treffer,
/// sonst nennt <paramref name="Meldung"/> den Grund.
/// </summary>
/// <param name="Ok">Lässt sich einlesen?</param>
/// <param name="Meldung">Die benannte Ablehnung; leer = keine.</param>
/// <param name="Datei">Der Dateiname der Quelle.</param>
/// <param name="Treffer">Die zugeordneten Profile.</param>
/// <param name="Unbekannt">Profile der Quelle ohne DIN-Profil.</param>
/// <param name="OhneWerte">Profile der Quelle ohne einen Wert.</param>
/// <param name="Doppelt">Nummern, die die Quelle mehr als einmal führt.</param>
/// <param name="Sonstige">Die übrigen Meldungen beim Lesen und Prüfen.</param>
public sealed record NormwerteVorschau(bool Ok, string Meldung, string Datei, IReadOnlyList<NormwerteTreffer> Treffer,
                                       IReadOnlyList<string> Unbekannt, IReadOnlyList<string> OhneWerte,
                                       IReadOnlyList<string> Doppelt, IReadOnlyList<string> Sonstige)
{
    /// <summary>Treffer, deren DIN-Profil schon Werte trägt.</summary>
    public int MitWerten => Treffer.Count(t => t.HatWerte);

    /// <summary>Treffer mit mindestens einer abweichenden Spalte.</summary>
    public int MitAbweichung => Treffer.Count(t => t.Abweichungen.Count > 0);

    /// <summary>Je Spalte die Zahl der Treffer, die dort abweichen — in der Reihenfolge des ersten Auftretens.</summary>
    public IReadOnlyList<(string Spalte, int Zahl)> AbweichungenJeSpalte
        => Treffer.SelectMany(t => t.Abweichungen).GroupBy(s => s, StringComparer.Ordinal)
                  .Select(g => (g.Key, g.Count())).ToList();

    /// <summary>Eine benannte Ablehnung ohne Treffer.</summary>
    public static NormwerteVorschau Fehler(string datei, string meldung)
        => new(false, meldung ?? "", datei ?? "", Array.Empty<NormwerteTreffer>(), Array.Empty<string>(),
               Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}

/// <summary>
/// <b>Der Weg von „Normwerte einlesen…"</b> — alles, was Dialog und Blatt vom Kern brauchen (Kern
/// <c>RaumnutzungNormwerte</c>, Hülle <c>RaumnutzungNormwerteHuelle</c>).
///
/// <para><b>Kein Delegat, kein Knopf.</b> Ohne <see cref="Laden"/> und <see cref="Einlesen"/> bietet das Blatt
/// „Normwerte einlesen…" nicht an, ohne <see cref="Entfernen"/> nicht „Eingelesene Werte entfernen…".</para>
/// <para><b>Geschrieben wird erst mit „Übernehmen"</b> und nur die zuletzt geladene Vorschau; die Dateiwahl gehört
/// der Plattform (<c>Dienste.Datei</c> in der Hülle).</para>
/// </summary>
public sealed class NormwerteWeg
{
    /// <summary>Die Texte.</summary>
    public NormwerteTexte Texte { get; init; } = new();

    /// <summary>Dateiwahl der Plattform und Vorschau; abgebrochen = <c>null</c>.</summary>
    public Func<NormwerteQuelle, Task<NormwerteVorschau?>>? Laden { get; init; }

    /// <summary>Liest die zuletzt geladene Vorschau ein; der Schalter „vorhandene Werte überschreiben" kommt mit.</summary>
    public Func<bool, RaumnutzungErgebnis>? Einlesen { get; init; }

    /// <summary>Die CSV-Vorlage ohne Werte speichern (Dateiwahl der Plattform); abgebrochen = <c>Ok</c> mit leerer Meldung.</summary>
    public Func<Task<RaumnutzungErgebnis>>? VorlageSpeichern { get; init; }

    /// <summary>Nimmt alle eingelesenen Werte zurück.</summary>
    public Func<RaumnutzungErgebnis>? Entfernen { get; init; }

    /// <summary>Die Zahl der Profile mit eingelesenen Werten.</summary>
    public Func<int>? Eingelesene { get; init; }
}

/// <summary>Die Texte von „Normwerte einlesen…" — je Eigenschaft der Ressourcenschlüssel, Rückfall deutsch.</summary>
public sealed class NormwerteTexte
{
    /// <summary><c>RNP_NW_BTN_EINLESEN</c></summary>
    public string KnopfEinlesen { get; set; } = "Normwerte einlesen…";
    /// <summary><c>RNP_NW_BTN_ENTFERNEN</c></summary>
    public string KnopfEntfernen { get; set; } = "Eingelesene Werte entfernen…";
    /// <summary><c>RNP_NW_LBL_TITEL</c></summary>
    public string Titel { get; set; } = "Normwerte einlesen";
    /// <summary><c>RNP_NW_TXT_HINWEIS</c></summary>
    public string Hinweis { get; set; } =
        "Füllt die Profile der Kategorie DIN/TS 18599-10 mit den Werten aus Ihrer CSV-Datei oder einer Projektdatei. Das Programm liefert keine Normwerte mit.";
    /// <summary><c>RNP_NW_BTN_CSV</c></summary>
    public string KnopfCsv { get; set; } = "CSV-Datei wählen…";
    /// <summary><c>RNP_NW_BTN_PROJEKT</c></summary>
    public string KnopfProjekt { get; set; } = "Projektdatei wählen…";
    /// <summary><c>RNP_NW_BTN_VORLAGE</c></summary>
    public string KnopfVorlage { get; set; } = "Vorlage speichern…";
    /// <summary><c>RNP_NW_TXT_VORLAGE</c></summary>
    public string HinweisVorlage { get; set; } = "Die Vorlage ist die CSV-Datei der Kategorie mit Nummern und Namen, ohne Werte.";
    /// <summary><c>RNP_NW_LBL_DATEI</c></summary>
    public string Datei { get; set; } = "Datei: {0}";
    /// <summary><c>RNP_NW_TXT_OHNE_DATEI</c></summary>
    public string OhneDatei { get; set; } = "Noch keine Datei gewählt.";
    /// <summary><c>RNP_NW_LBL_TREFFER</c></summary>
    public string TitelTreffer { get; set; } = "Treffer";
    /// <summary><c>RNP_NW_LBL_NUMMER</c></summary>
    public string Nummer { get; set; } = "Nummer";
    /// <summary><c>RNP_NW_LBL_PROFIL</c></summary>
    public string Profil { get; set; } = "Profil";
    /// <summary><c>RNP_NW_LBL_STAND</c></summary>
    public string Stand { get; set; } = "Stand";
    /// <summary><c>RNP_NW_LBL_ABWEICHUNG</c></summary>
    public string Abweichungen { get; set; } = "Abweichungen";
    /// <summary><c>RNP_NW_STAND_LEER</c></summary>
    public string StandLeer { get; set; } = "ohne Werte";
    /// <summary><c>RNP_NW_STAND_VORHANDEN</c></summary>
    public string StandVorhanden { get; set; } = "Werte vorhanden";
    /// <summary><c>RNP_NW_STAND_EINGELESEN</c></summary>
    public string StandEingelesen { get; set; } = "eingelesene Werte vorhanden";
    /// <summary><c>RNP_NW_TXT_ZAEHLUNG</c></summary>
    public string Zaehlung { get; set; } = "{0} Treffer, davon {1} mit vorhandenen Werten und {2} mit Abweichungen.";
    /// <summary><c>RNP_NW_TXT_SPALTEN</c></summary>
    public string Spalten { get; set; } = "Abweichungen je Spalte: {0}";
    /// <summary><c>RNP_NW_LBL_HINWEISE</c></summary>
    public string TitelHinweise { get; set; } = "Nicht übernommen";
    /// <summary><c>RNP_NW_LBL_ART</c></summary>
    public string Art { get; set; } = "Art";
    /// <summary><c>RNP_NW_LBL_GRUND</c></summary>
    public string Grund { get; set; } = "Profil oder Meldung";
    /// <summary><c>RNP_NW_ART_UNBEKANNT</c></summary>
    public string ArtUnbekannt { get; set; } = "unbekannte Nummer";
    /// <summary><c>RNP_NW_ART_OHNE</c></summary>
    public string ArtOhneWert { get; set; } = "ohne Wert";
    /// <summary><c>RNP_NW_ART_DOPPELT</c></summary>
    public string ArtDoppelt { get; set; } = "doppelte Nummer";
    /// <summary><c>RNP_NW_ART_SONST</c></summary>
    public string ArtSonstige { get; set; } = "Meldung";
    /// <summary><c>RNP_NW_TXT_KEINE_HINWEISE</c></summary>
    public string KeineHinweise { get; set; } = "Alle Profile der Datei sind zugeordnet.";
    /// <summary><c>RNP_NW_LBL_UEBERSCHREIBEN</c></summary>
    public string Ueberschreiben { get; set; } = "vorhandene Werte überschreiben";
    /// <summary><c>RNP_NW_TXT_UEBERSPRINGEN</c></summary>
    public string Ueberspringen { get; set; } = "Ohne Haken bleiben {0} Profil(e) mit vorhandenen Werten unverändert.";
    /// <summary><c>RNP_NW_BTN_UEBERNEHMEN</c></summary>
    public string KnopfUebernehmen { get; set; } = "Übernehmen";
    /// <summary><c>RNP_NW_BTN_ABBRECHEN</c></summary>
    public string KnopfAbbrechen { get; set; } = "Abbrechen";
    /// <summary><c>RNP_NW_SPERRE_DATEI</c></summary>
    public string SperreDatei { get; set; } = "Zuerst eine Datei wählen.";
    /// <summary><c>RNP_NW_SPERRE_KEINE</c></summary>
    public string SperreKeine { get; set; } = "Kein Profil trägt eingelesene Werte.";
    /// <summary><c>RNP_NW_FRAGE_ENTFERNEN</c></summary>
    public string FrageEntfernen { get; set; } =
        "Die eingelesenen Werte von {0} Profil(en) entfernen? Die Profile stehen danach wieder ohne Werte.";
    /// <summary><c>RNP_NW_BTN_JA</c></summary>
    public string Ja { get; set; } = "Entfernen";
    /// <summary><c>RNP_NW_BTN_NEIN</c></summary>
    public string Nein { get; set; } = "Nicht entfernen";
}
