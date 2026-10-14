using System;
using System.Collections.Generic;
using KiKern;

namespace EPOS.UI.Seiten.Pufferspeicher;

/// <summary>
/// Das FLACHE Abbild der Ansicht „Pufferspeicher-Auslegung" für den Hilfe-Assistenten
/// (Maske <c>Form_PufferAuslegung</c>): Klassen-Set, Vorlage, Gerätetyp, Zweiterzeuger,
/// Übergabeart, Heizgrenze, Anlagenvolumen, Sperrprofil und die drei Ziele.
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten; die Ansicht setzt sie beim Anmelden. Ein Setzen wirkt wie die Bedienung — es
/// markiert das Ergebnis als veraltet bzw. rechnet bei einem Schalter neu. Geschrieben wird
/// erst mit „Auslegung speichern" oder „Übernehmen", und diese zwei bleiben dem Anwender.</para>
/// </summary>
public sealed class PufferAuslegungKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — die Ansicht setzt sie beim Anmelden
    // =====================================================================

    public Func<string, bool>? KlasseLesen { get; init; }
    public Action<string, bool>? KlasseSetzen { get; init; }

    public Func<string>? VorlageLesen { get; init; }
    public Action<string>? VorlageSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? VorlageEintraege { get; init; }

    public Func<bool>? GeregeltLesen { get; init; }
    public Action<bool>? GeregeltSetzen { get; init; }

    public Func<bool>? ZweiterzeugerFreiLesen { get; init; }
    public Action<bool>? ZweiterzeugerFreiSetzen { get; init; }

    public Func<string>? UebergabeartLesen { get; init; }
    public Action<string>? UebergabeartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? UebergabeartEintraege { get; init; }

    public Func<string>? SperrprofilLesen { get; init; }
    public Action<string>? SperrprofilSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SperrprofilEintraege { get; init; }

    public Func<string, double?>? ZahlLesen { get; init; }
    public Action<string, double?>? ZahlSetzen { get; init; }

    /// <summary>Die Schlüssel der Zahlenfelder (<see cref="ZahlLesen"/>/<see cref="ZahlSetzen"/>).</summary>
    public const string HEIZGRENZE = "heizgrenze";
    public const string ANLAGENVOLUMEN = "anlagenvolumen";
    public const string STARTZIEL = "startziel";
    public const string MINDESTLAUFZEIT = "mindestlaufzeit";
    public const string DECKUNGSZIEL = "deckungsziel";

    /// <summary>Die Schlüssel der Klassen (<see cref="KlasseLesen"/>/<see cref="KlasseSetzen"/>).</summary>
    public const string HEIZUNG = "heizung";
    public const string BRAUCHWASSER = "brauchwasser";
    public const string PROZESS = "prozess";

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Speicherklasse Heizung (Heizzone).</summary>
    public bool KlasseHeizung
    {
        get => KlasseLesen?.Invoke(HEIZUNG) ?? false;
        set => KlasseSetzen?.Invoke(HEIZUNG, value);
    }

    /// <summary>Speicherklasse Brauchwasser (Brauchwasserzone aus dem Zapfprofil).</summary>
    public bool KlasseBrauchwasser
    {
        get => KlasseLesen?.Invoke(BRAUCHWASSER) ?? false;
        set => KlasseSetzen?.Invoke(BRAUCHWASSER, value);
    }

    /// <summary>Speicherklasse Prozess (Prozesszone).</summary>
    public bool KlasseProzess
    {
        get => KlasseLesen?.Invoke(PROZESS) ?? false;
        set => KlasseSetzen?.Invoke(PROZESS, value);
    }

    /// <summary>Die Vorlage der Anlage (<c>WP_MONO</c> … <c>PROZESS</c>).</summary>
    public string Vorlage
    {
        get => VorlageLesen?.Invoke() ?? "";
        set => VorlageSetzen?.Invoke(value);
    }

    /// <summary>Die Vorlagen zur Wahl.</summary>
    public IReadOnlyList<KiWahleintrag> VorlageWahl => VorlageEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Wärmepumpe leistungsgeregelt statt Fixed-Speed.</summary>
    public bool Geregelt
    {
        get => GeregeltLesen?.Invoke() ?? false;
        set => GeregeltSetzen?.Invoke(value);
    }

    /// <summary>Zweiterzeuger in der Sperre frei (sonst Heizstab, mitgesperrt).</summary>
    public bool ZweiterzeugerFrei
    {
        get => ZweiterzeugerFreiLesen?.Invoke() ?? false;
        set => ZweiterzeugerFreiSetzen?.Invoke(value);
    }

    /// <summary>Die Übergabeart; leer = keine Angabe.</summary>
    public string Uebergabeart
    {
        get => UebergabeartLesen?.Invoke() ?? "";
        set => UebergabeartSetzen?.Invoke(value);
    }

    /// <summary>Die Übergabearten zur Wahl.</summary>
    public IReadOnlyList<KiWahleintrag> UebergabeartWahl => UebergabeartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Das Sperrprofil (<c>KEINE</c>, <c>ZWEI_MAL_ZWEI</c>, <c>DREI_MAL_ZWEI</c>, <c>EIGEN</c>).</summary>
    public string Sperrprofil
    {
        get => SperrprofilLesen?.Invoke() ?? "";
        set => SperrprofilSetzen?.Invoke(value);
    }

    /// <summary>Die Sperrprofile zur Wahl.</summary>
    public IReadOnlyList<KiWahleintrag> SperrprofilWahl => SperrprofilEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Heizgrenze [°C]; leer = Vorgabe.</summary>
    public double? Heizgrenze
    {
        get => ZahlLesen?.Invoke(HEIZGRENZE);
        set => ZahlSetzen?.Invoke(HEIZGRENZE, value);
    }

    /// <summary>Nicht absperrbares Anlagenvolumen [l]; leer = Vorgabe.</summary>
    public double? Anlagenvolumen
    {
        get => ZahlLesen?.Invoke(ANLAGENVOLUMEN);
        set => ZahlSetzen?.Invoke(ANLAGENVOLUMEN, value);
    }

    /// <summary>Startziel [1/d]; leer = Vorgabe der Vorlage.</summary>
    public double? Startziel
    {
        get => ZahlLesen?.Invoke(STARTZIEL);
        set => ZahlSetzen?.Invoke(STARTZIEL, value);
    }

    /// <summary>Mindestlaufzeit je Start [min]; leer = Vorgabe der Vorlage.</summary>
    public double? Mindestlaufzeit
    {
        get => ZahlLesen?.Invoke(MINDESTLAUFZEIT);
        set => ZahlSetzen?.Invoke(MINDESTLAUFZEIT, value);
    }

    /// <summary>Ziel-Deckungsgrad [%]; leer = 100 %.</summary>
    public double? Deckungsziel
    {
        get => ZahlLesen?.Invoke(DECKUNGSZIEL);
        set => ZahlSetzen?.Invoke(DECKUNGSZIEL, value);
    }
}
