using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Export;

/// <summary>
/// Die Rückmeldung des Speicherwegs des Klimaexports (Auftrag KD-5): geschrieben (mit Pfad im Text), abgebrochen
/// (Dateiwahl verlassen — der Dialog bleibt still offen) oder gescheitert (Grund im Text).
/// </summary>
public sealed record KlimaExportErgebnis(bool Erfolg, bool Abbruch, string Text)
{
    /// <summary>Die Dateiwahl wurde abgebrochen.</summary>
    public static KlimaExportErgebnis Abgebrochen { get; } = new(false, true, "");
}

/// <summary>
/// Das FLACHE Abbild des <see cref="KlimaExportDialog"/> für den Hilfe-Assistenten (Auftrag KD-5): die Formatfelder
/// als Steuerwerte (Position der Optionsgruppe) und die Parameterwahl als Schlüsselliste. Exportieren bleibt ein Klick
/// des Anwenders (Dateiwahl der Plattform). Sie hält keinen Zustand: Jede Eigenschaft ruft ihren Delegaten.
/// </summary>
public sealed class KlimaExportKiSicht
{
    public Func<KlimaExportOptionen>? Lesen { get; init; }
    public Action<Action<KlimaExportOptionen>>? Aendern { get; init; }

    /// <summary>0 = Semikolon, 1 = Komma, 2 = Tabulator.</summary>
    public int Trennzeichen { get => (int)(Lesen?.Invoke().Trennzeichen ?? 0); set => Aendern?.Invoke(o => o.Trennzeichen = Begrenzt<KlimaTrennzeichen>(value)); }

    /// <summary>0 = Komma, 1 = Punkt.</summary>
    public int Dezimalzeichen { get => (int)(Lesen?.Invoke().Dezimalzeichen ?? 0); set => Aendern?.Invoke(o => o.Dezimalzeichen = Begrenzt<KlimaDezimalzeichen>(value)); }

    /// <summary>0 = Stunde, 1 = Datum, 2 = beides, 3 = ISO 8601, 4 = keine.</summary>
    public int Zeitspalte { get => (int)(Lesen?.Invoke().Zeitspalte ?? 0); set => Aendern?.Invoke(o => o.Zeitspalte = Begrenzt<KlimaZeitspalte>(value)); }

    /// <summary>0 = MEZ ohne Sommerzeit, 1 = UTC.</summary>
    public int Zeitbezug { get => (int)(Lesen?.Invoke().Zeitbezug ?? 0); set => Aendern?.Invoke(o => o.Zeitbezug = Begrenzt<KlimaZeitbezug>(value)); }

    /// <summary>0 = eine Kopfzeile, 1 = mit Einheitenzeile, 2 = ohne Kopf.</summary>
    public int Kopfart { get => (int)(Lesen?.Invoke().Kopfart ?? 0); set => Aendern?.Invoke(o => o.Kopfart = Begrenzt<KlimaKopfart>(value)); }

    /// <summary>Herkunftsblock mit #-Zeilen.</summary>
    public bool Herkunft { get => Lesen?.Invoke().Herkunft ?? true; set => Aendern?.Invoke(o => o.Herkunft = value); }

    /// <summary>Die gewählten Größen als Schlüsselliste, durch Komma getrennt; Unbekannte fallen weg.</summary>
    public string Parameter
    {
        get => string.Join(",", Lesen?.Invoke().Parameter ?? new List<string>());
        set => Aendern?.Invoke(o => o.Parameter = (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                    .Where(s => KlimaExportOptionen.Groesse(s) is not null).Distinct().ToList());
    }

    private static T Begrenzt<T>(int wert) where T : struct, Enum
        => Enum.IsDefined(typeof(T), wert) ? (T)(object)wert : default;
}

/// <summary>Die Anzeigetexte des <see cref="KlimaExportDialog"/> (ein Bündel; Ressourcenschlüssel im Kommentar).</summary>
public sealed class KlimaExportTexte
{
    /// <summary>KEXP_TITEL</summary>
    public string Titel { get; set; } = Resource.KEXP_TITEL;

    /// <summary>KEXP_MSG_LESEN</summary>
    public string Lesen { get; set; } = Resource.KEXP_MSG_LESEN;

    /// <summary>KEXP_MSG_KEINE_DATEN</summary>
    public string KeineDaten { get; set; } = Resource.KEXP_MSG_KEINE_DATEN;

    /// <summary>KEXP_GRUND_OHNE_ROHDATEN</summary>
    public string OhneRohdaten { get; set; } = Resource.KEXP_GRUND_OHNE_ROHDATEN;

    /// <summary>KEXP_GRP_PARAMETER</summary>
    public string Parameter { get; set; } = Resource.KEXP_GRP_PARAMETER;

    /// <summary>KEXP_GRP_FORMAT</summary>
    public string Format { get; set; } = Resource.KEXP_GRP_FORMAT;

    /// <summary>KEXP_ALLE</summary>
    public string Alle { get; set; } = Resource.KEXP_ALLE;

    /// <summary>KEXP_KEINE</summary>
    public string Keine { get; set; } = Resource.KEXP_KEINE;

    /// <summary>KEXP_GR_RECHENWERTE</summary>
    public string GruppeRechenwerte { get; set; } = Resource.KEXP_GR_RECHENWERTE;

    /// <summary>KEXP_GR_FASSADENWERTE</summary>
    public string GruppeFassadenwerte { get; set; } = Resource.KEXP_GR_FASSADENWERTE;

    /// <summary>KEXP_GR_ROHWERTE</summary>
    public string GruppeRohwerte { get; set; } = Resource.KEXP_GR_ROHWERTE;

    /// <summary>KEXP_GR_ABGELEITET</summary>
    public string GruppeAbgeleitet { get; set; } = Resource.KEXP_GR_ABGELEITET;

    /// <summary>KEXP_GERECHNET</summary>
    public string Gerechnet { get; set; } = Resource.KEXP_GERECHNET;

    /// <summary>KEXP_VORLAGE_HAUS</summary>
    public string VorlageHaus { get; set; } = Resource.KEXP_VORLAGE_HAUS;

    /// <summary>KEXP_VORLAGE_INTERNATIONAL</summary>
    public string VorlageInternational { get; set; } = Resource.KEXP_VORLAGE_INTERNATIONAL;

    /// <summary>KEXP_TRENNZEICHEN</summary>
    public string Trennzeichen { get; set; } = Resource.KEXP_TRENNZEICHEN;

    /// <summary>KEXP_TZ_SEMIKOLON</summary>
    public string TzSemikolon { get; set; } = Resource.KEXP_TZ_SEMIKOLON;

    /// <summary>KEXP_TZ_KOMMA</summary>
    public string TzKomma { get; set; } = Resource.KEXP_TZ_KOMMA;

    /// <summary>KEXP_TZ_TABULATOR</summary>
    public string TzTabulator { get; set; } = Resource.KEXP_TZ_TABULATOR;

    /// <summary>KEXP_DEZIMALZEICHEN</summary>
    public string Dezimalzeichen { get; set; } = Resource.KEXP_DEZIMALZEICHEN;

    /// <summary>KEXP_DZ_KOMMA</summary>
    public string DzKomma { get; set; } = Resource.KEXP_DZ_KOMMA;

    /// <summary>KEXP_DZ_PUNKT</summary>
    public string DzPunkt { get; set; } = Resource.KEXP_DZ_PUNKT;

    /// <summary>KEXP_DZ_KONFLIKT</summary>
    public string Konflikt { get; set; } = Resource.KEXP_DZ_KONFLIKT;

    /// <summary>KEXP_ZEITSPALTE</summary>
    public string Zeitspalte { get; set; } = Resource.KEXP_ZEITSPALTE;

    /// <summary>KEXP_ZS_STUNDE</summary>
    public string ZsStunde { get; set; } = Resource.KEXP_ZS_STUNDE;

    /// <summary>KEXP_ZS_DATUM</summary>
    public string ZsDatum { get; set; } = Resource.KEXP_ZS_DATUM;

    /// <summary>KEXP_ZS_BEIDES</summary>
    public string ZsBeides { get; set; } = Resource.KEXP_ZS_BEIDES;

    /// <summary>KEXP_ZS_ISO</summary>
    public string ZsIso { get; set; } = Resource.KEXP_ZS_ISO;

    /// <summary>KEXP_ZS_KEINE</summary>
    public string ZsKeine { get; set; } = Resource.KEXP_ZS_KEINE;

    /// <summary>KEXP_ZEITBEZUG</summary>
    public string Zeitbezug { get; set; } = Resource.KEXP_ZEITBEZUG;

    /// <summary>KEXP_ZB_MEZ</summary>
    public string ZbMez { get; set; } = Resource.KEXP_ZB_MEZ;

    /// <summary>KEXP_ZB_UTC</summary>
    public string ZbUtc { get; set; } = Resource.KEXP_ZB_UTC;

    /// <summary>KEXP_KOPFART</summary>
    public string Kopfart { get; set; } = Resource.KEXP_KOPFART;

    /// <summary>KEXP_KA_EINE</summary>
    public string KaEine { get; set; } = Resource.KEXP_KA_EINE;

    /// <summary>KEXP_KA_ZWEI</summary>
    public string KaZwei { get; set; } = Resource.KEXP_KA_ZWEI;

    /// <summary>KEXP_KA_OHNE</summary>
    public string KaOhne { get; set; } = Resource.KEXP_KA_OHNE;

    /// <summary>KEXP_HERKUNFT</summary>
    public string Herkunft { get; set; } = Resource.KEXP_HERKUNFT;

    /// <summary>KEXP_VORSCHAU</summary>
    public string Vorschau { get; set; } = Resource.KEXP_VORSCHAU;

    /// <summary>KEXP_BTN_EXPORT</summary>
    public string Exportieren { get; set; } = Resource.KEXP_BTN_EXPORT;

    /// <summary>ALLG_BTN_ABBRECHEN</summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;

    /// <summary>KEXP_KEINE_PARAMETER</summary>
    public string KeineParameter { get; set; } = Resource.KEXP_KEINE_PARAMETER;
}
