using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Der Zugang des Assistenten zum Blatt „Nutzungsprofile"</b> (NP3c; Konzept Nutzungsprofile 6.1,
/// NP-F3, NP-F22) — die Feldtafel der Kennwerte aus dem Profil <see cref="KiNutzungsprofilfelder"/>, die
/// die Sichtklasse des Gebäudeeditors (<see cref="GebaeudeKatalogKiSicht"/>) für ihre Schlüssel
/// <c>np_*</c> befragt, und die Zuordnungszeilen als Raster zum Lesen.
/// </summary>
/// <remarks>
/// <para><b>Wer ihn hält.</b> Der Wirt des Blatts legt ihn an und reicht ihn der Sicht und dem Blatt; das
/// Blatt bindet sich beim Erscheinen an (<see cref="Anbinden"/>) und löst sich beim Verschwinden
/// (<see cref="Loesen"/>). Den Zustand — den ENTWURF des Profileditors, die gewählte Kategorie, die
/// Zuordnungen — hält allein das Blatt; der Zugang liest und schreibt über dessen Delegaten.</para>
/// <para><b>Was Setzen tut.</b> Es schreibt in den Entwurf, genau wie eine Eingabe von Hand; in den
/// Katalog geht er erst mit „Speichern" des Blatts. Ohne offenes Blatt, ohne gewähltes Profil und an
/// einem ausgelieferten Profil lehnt es mit Grund ab. Lesen ohne Blatt oder Profil gibt leer.</para>
/// </remarks>
public sealed class RaumnutzungKiZugang
{
    private object? _halter;
    private Func<RaumnutzungProfilDaten?>? _entwurf;
    private Func<string>? _kategorie;
    private Func<IReadOnlyList<RaumnutzungZuordnungKiZeile>>? _zeilen;
    private Action? _auffrischen;

    /// <summary>Steht das Blatt offen (hat sich ein Blatt angebunden)?</summary>
    public bool Angebunden => _entwurf is not null;

    /// <summary>
    /// Das Blatt bindet sich an: Entwurf des Editors, Name der Kategorie des Entwurfs, die Zuordnungszeilen
    /// und das Neuzeichnen nach einem Setzen.
    /// </summary>
    public void Anbinden(object halter, Func<RaumnutzungProfilDaten?> entwurf, Func<string> kategorie,
                         Func<IReadOnlyList<RaumnutzungZuordnungKiZeile>> zeilen, Action auffrischen)
    {
        _halter = halter;
        _entwurf = entwurf;
        _kategorie = kategorie;
        _zeilen = zeilen;
        _auffrischen = auffrischen;
    }

    /// <summary>Das Blatt löst sich — nur, wenn es selbst noch angebunden ist.</summary>
    public void Loesen(object halter)
    {
        if (!ReferenceEquals(_halter, halter)) return;
        _halter = null;
        _entwurf = null;
        _kategorie = null;
        _zeilen = null;
        _auffrischen = null;
    }

    /// <summary>Die Zeilen der Zuordnungstabelle (Raster zum Lesen); ohne Blatt keine.</summary>
    public IReadOnlyList<RaumnutzungZuordnungKiZeile> Zuordnungen
        => _zeilen?.Invoke() ?? Array.Empty<RaumnutzungZuordnungKiZeile>();

    /// <summary>
    /// Die abgeleiteten Nutzungstage im Jahr eines Feldsatzes (E93) — das Blatt reicht den Weg seiner Hülle herein;
    /// das Feld ist nur lesbar. <c>null</c> = ohne Blatt.
    /// </summary>
    public Func<RaumnutzungProfilDaten, int?>? Nutzungstage { get; set; }

    /// <summary>Ist der Schlüssel ein Feld des Profileditors?</summary>
    public static bool IstFeld(string schluessel) => KiNutzungsprofilfelder.Finde(schluessel) is not null;

    /// <summary>Der Wert eines Felds im Typ des Katalogfelds; <c>null</c> = leer, ohne Blatt oder ohne Profil.</summary>
    public object? Lesen(string schluessel)
    {
        if (KiNutzungsprofilfelder.Finde(schluessel) is not KiNutzungsprofilfelder.Feld f) return null;
        if (_entwurf?.Invoke() is not RaumnutzungProfilDaten s) return null;
        return f.Kennwert switch
        {
            KiNutzungsprofilfelder.Kennwert.Nummer => s.Nummer,
            KiNutzungsprofilfelder.Kennwert.Name => s.Bezeichner,
            KiNutzungsprofilfelder.Kennwert.Beschreibung => s.Beschreibung,
            KiNutzungsprofilfelder.Kennwert.Kategorie => _kategorie?.Invoke() ?? "",
            KiNutzungsprofilfelder.Kennwert.NutzungVon => s.NutzungVon,
            KiNutzungsprofilfelder.Kennwert.NutzungBis => s.NutzungBis,
            KiNutzungsprofilfelder.Kennwert.BetriebVon => s.BetriebVon,
            KiNutzungsprofilfelder.Kennwert.BetriebBis => s.BetriebBis,
            KiNutzungsprofilfelder.Kennwert.Woche => s.NutzungstageWoche,
            KiNutzungsprofilfelder.Kennwert.TageJahr => Nutzungstage?.Invoke(s),
            KiNutzungsprofilfelder.Kennwert.Feiertage => s.FeiertageWieSonntag == true,
            KiNutzungsprofilfelder.Kennwert.HeizSoll => s.HeizSoll,
            KiNutzungsprofilfelder.Kennwert.HeizAusserhalb => s.HeizSollAusserhalb,
            KiNutzungsprofilfelder.Kennwert.HeizAus => s.HeizAusAusserhalb == true,
            KiNutzungsprofilfelder.Kennwert.KuehlSoll => s.KuehlSoll,
            KiNutzungsprofilfelder.Kennwert.KuehlAusserhalb => s.KuehlSollAusserhalb,
            KiNutzungsprofilfelder.Kennwert.KuehlAus => s.KuehlAusAusserhalb == true,
            KiNutzungsprofilfelder.Kennwert.LuftEinheit => s.LuftEinheit == RaumnutzungLuftEinheit.JeFlaeche
                ? KiNutzungsprofilfelder.LUFT_JE_FLAECHE : KiNutzungsprofilfelder.LUFT_JE_STUNDE,
            KiNutzungsprofilfelder.Kennwert.Luft => s.Aussenluft,
            KiNutzungsprofilfelder.Kennwert.LuftAusserhalb => s.AussenluftAusserhalb,
            KiNutzungsprofilfelder.Kennwert.PersonenFlaeche => s.PersonenFlaeche,
            KiNutzungsprofilfelder.Kennwert.PersonenWaerme => s.PersonenWaerme,
            KiNutzungsprofilfelder.Kennwert.PersonenAnteil => Prozent(s.PersonenAnteil),
            KiNutzungsprofilfelder.Kennwert.PersonenAusserhalb => Prozent(s.PersonenAnteilAusserhalb),
            KiNutzungsprofilfelder.Kennwert.Geraete => s.GeraeteLeistung,
            KiNutzungsprofilfelder.Kennwert.GeraeteAnteil => Prozent(s.GeraeteAnteil),
            KiNutzungsprofilfelder.Kennwert.GeraeteAusserhalb => Prozent(s.GeraeteAnteilAusserhalb),
            KiNutzungsprofilfelder.Kennwert.Beleuchtung => s.BeleuchtungLeistung,
            KiNutzungsprofilfelder.Kennwert.BeleuchtungAnteil => Prozent(s.BeleuchtungAnteil),
            KiNutzungsprofilfelder.Kennwert.WegHeizen => Wegwert(s, KonditionierungGroesse.Heizen),
            KiNutzungsprofilfelder.Kennwert.WegKuehlen => Wegwert(s, KonditionierungGroesse.Kuehlen),
            KiNutzungsprofilfelder.Kennwert.WegLueftung => Wegwert(s, KonditionierungGroesse.Lueftung),
            KiNutzungsprofilfelder.Kennwert.WegGeraete => Wegwert(s, KonditionierungGroesse.Geraete),
            KiNutzungsprofilfelder.Kennwert.WegPersonen => Wegwert(s, KonditionierungGroesse.Personen),
            _ => null,
        };
    }

    /// <summary>
    /// Schreibt einen Wert in den Entwurf — derselbe Weg wie die Eingabe im Blatt. Die Kategorie nimmt
    /// nichts an (nur lesbar); ohne Blatt, ohne Profil und an einem ausgelieferten Profil kommt die
    /// Ablehnung mit Grund.
    /// </summary>
    public void Setzen(string schluessel, object? wert)
    {
        if (KiNutzungsprofilfelder.Finde(schluessel) is not KiNutzungsprofilfelder.Feld f || f.NurLesen) return;
        if (_entwurf is null) throw new InvalidOperationException(KiNutzungsprofilfelder.GrundOhneBlatt);
        if (_entwurf() is not RaumnutzungProfilDaten s)
            throw new InvalidOperationException(KiNutzungsprofilfelder.GrundOhneProfil);
        if (s.Ausgeliefert) throw new InvalidOperationException(KiNutzungsprofilfelder.GrundAusgeliefert);

        string text = wert as string ?? "";
        int? ganz = wert as int?;
        double? zahl = wert as double?;
        bool ja = wert is true;
        switch (f.Kennwert)
        {
            case KiNutzungsprofilfelder.Kennwert.Nummer: s.Nummer = text; break;
            case KiNutzungsprofilfelder.Kennwert.Name: s.Bezeichner = text; break;
            case KiNutzungsprofilfelder.Kennwert.Beschreibung: s.Beschreibung = text; break;
            case KiNutzungsprofilfelder.Kennwert.NutzungVon: s.NutzungVon = ganz; break;
            case KiNutzungsprofilfelder.Kennwert.NutzungBis: s.NutzungBis = ganz; break;
            case KiNutzungsprofilfelder.Kennwert.BetriebVon: s.BetriebVon = ganz; break;
            case KiNutzungsprofilfelder.Kennwert.BetriebBis: s.BetriebBis = ganz; break;
            case KiNutzungsprofilfelder.Kennwert.Woche:
                text = text.Trim();
                if (text.Length != 0 && (text.Length != 7 || text.Any(c => c != '0' && c != '1')))
                    throw new InvalidOperationException(KiNutzungsprofilfelder.GrundWoche);
                s.NutzungstageWoche = text;
                break;
            case KiNutzungsprofilfelder.Kennwert.Feiertage: s.FeiertageWieSonntag = ja; break;
            case KiNutzungsprofilfelder.Kennwert.HeizSoll: s.HeizSoll = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.HeizAusserhalb: s.HeizSollAusserhalb = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.HeizAus: s.HeizAusAusserhalb = ja; break;
            case KiNutzungsprofilfelder.Kennwert.KuehlSoll: s.KuehlSoll = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.KuehlAusserhalb: s.KuehlSollAusserhalb = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.KuehlAus: s.KuehlAusAusserhalb = ja; break;
            case KiNutzungsprofilfelder.Kennwert.LuftEinheit:
                s.LuftEinheit = text.Trim() switch
                {
                    KiNutzungsprofilfelder.LUFT_JE_STUNDE => RaumnutzungLuftEinheit.JeStunde,
                    KiNutzungsprofilfelder.LUFT_JE_FLAECHE => RaumnutzungLuftEinheit.JeFlaeche,
                    _ => throw new InvalidOperationException(KiNutzungsprofilfelder.GrundLufteinheit),
                };
                break;
            case KiNutzungsprofilfelder.Kennwert.Luft: s.Aussenluft = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.LuftAusserhalb: s.AussenluftAusserhalb = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.PersonenFlaeche: s.PersonenFlaeche = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.PersonenWaerme: s.PersonenWaerme = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.PersonenAnteil: s.PersonenAnteil = Anteil(zahl); break;
            case KiNutzungsprofilfelder.Kennwert.PersonenAusserhalb: s.PersonenAnteilAusserhalb = Anteil(zahl); break;
            case KiNutzungsprofilfelder.Kennwert.Geraete: s.GeraeteLeistung = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.GeraeteAnteil: s.GeraeteAnteil = Anteil(zahl); break;
            case KiNutzungsprofilfelder.Kennwert.GeraeteAusserhalb: s.GeraeteAnteilAusserhalb = Anteil(zahl); break;
            case KiNutzungsprofilfelder.Kennwert.Beleuchtung: s.BeleuchtungLeistung = zahl; break;
            case KiNutzungsprofilfelder.Kennwert.BeleuchtungAnteil: s.BeleuchtungAnteil = Anteil(zahl); break;
        }
        _auffrischen?.Invoke();
    }

    // ------------------------------------------------------------ NP4c: Umschalter, Zeilenbild, Stundenprofil

    /// <summary>Der Weg einer Größe im Entwurf als Wert des Umschalters (<c>kennwerte</c>, <c>zeilenbild</c>, <c>stundenprofil</c>).</summary>
    private static string Wegwert(RaumnutzungProfilDaten s, KonditionierungGroesse g) => RaumnutzungBild.Weg(s, g) switch
    {
        RaumnutzungBildweg.Zeilenbild => KiNutzungsprofilfelder.WEG_ZEILENBILD,
        RaumnutzungBildweg.Stundenprofil => KiNutzungsprofilfelder.WEG_STUNDENPROFIL,
        _ => KiNutzungsprofilfelder.WEG_KENNWERTE,
    };

    /// <summary>Die Zeilen des Zeilenbilds im Entwurf (Raster zum Lesen, Werte in der Einheit des Profils); ohne Blatt keine.</summary>
    public IReadOnlyList<RaumnutzungZeilenbildKiZeile> Zeilenbildzeilen
        => _entwurf?.Invoke() is RaumnutzungProfilDaten s
           ? s.Zeilenbild.Select((z, i) => new RaumnutzungZeilenbildKiZeile(
                 (i + 1).ToString(CultureInfo.InvariantCulture), z.Groesse.ToString(), z.Zeile,
                 z.Aus ? DbWerteAus : Text(z.Wert),
                 z.Von.HasValue || z.Bis.HasValue ? Text(z.Von) + "-" + Text(z.Bis) : "",
                 Text(z.DeltaT))).ToList()
           : Array.Empty<RaumnutzungZeilenbildKiZeile>();

    /// <summary>Die Stundenprofile im Entwurf (Raster zum Lesen, 24 Werte mit Semikolon); ohne Blatt keine.</summary>
    public IReadOnlyList<RaumnutzungStundenKiZeile> Stundenzeilen
        => _entwurf?.Invoke() is RaumnutzungProfilDaten s
           ? s.Stunden.Select((h, i) => new RaumnutzungStundenKiZeile(
                 (i + 1).ToString(CultureInfo.InvariantCulture), h.Groesse.ToString(),
                 h.Tagesart == RaumnutzungTagesart.Frei ? "FREI" : "WERKTAG",
                 string.Join(";", h.Werte.Select(w => double.IsNaN(w) ? DbWerteAus : Text(w))))).ToList()
           : Array.Empty<RaumnutzungStundenKiZeile>();

    /// <summary>„aus" wie im Stundenprofil des Kerns (<c>DbWerte.KOND_WOCHE_AUS</c>).</summary>
    private const string DbWerteAus = "aus";

    private static string Text(double? w) => w is double d ? d.ToString("0.####", CultureInfo.InvariantCulture) : "";

    private static string Text(int? w) => w is int d ? d.ToString(CultureInfo.InvariantCulture) : "";

    /// <summary>Ein Anteil 0 … 1 in Prozent — dieselbe Rundung wie das Blatt.</summary>
    private static double? Prozent(double? anteil) => anteil is double a ? Math.Round(a * 100.0, 4) : null;

    /// <summary>Prozent als Anteil 0 … 1 — dieselbe Rundung wie das Blatt.</summary>
    private static double? Anteil(double? prozent) => prozent is double p ? Math.Round(p / 100.0, 6) : null;
}

/// <summary>
/// EINE Zeile der Zuordnungstabelle des Blatts „Nutzungsprofile" für den Assistenten (NP3c) — nur lesbar,
/// Kennzeichen die Nummer ab 1.
/// </summary>
/// <param name="Nummer">Die Nummer der Zeile ab 1 — das Kennzeichen.</param>
/// <param name="Art">Die Art der Zuordnung, wie die Tabelle sie nennt.</param>
/// <param name="Schluessel">Der Schlüssel (DIN-Nummer, IFC-Klasse, Raumtyp).</param>
/// <param name="Profil">Der Name des zugeordneten Profils; leer = keines.</param>
public sealed record RaumnutzungZuordnungKiZeile(string Nummer, string Art, string Schluessel, string Profil);

/// <summary>
/// EINE Zeile des Zeilenbilds im Entwurf des Blatts „Nutzungsprofile" für den Assistenten (NP4c) — nur lesbar,
/// Kennzeichen die Nummer ab 1.
/// </summary>
/// <param name="Nummer">Die Nummer der Zeile ab 1 — das Kennzeichen.</param>
/// <param name="Groesse">Die Größe (Heizen, Kuehlen, Lueftung, Geraete, Personen).</param>
/// <param name="Zeile">Die Zeile (TAG, NACHT, WOCHENENDE, FERIEN).</param>
/// <param name="Wert">Der Wert in der Einheit des Profils oder „aus"; leer = ohne.</param>
/// <param name="Fenster">Das Nachtfenster „von-bis"; leer = ohne.</param>
/// <param name="DeltaT">ΔT der Nachtauskühlung [K]; leer = ohne.</param>
public sealed record RaumnutzungZeilenbildKiZeile(string Nummer, string Groesse, string Zeile, string Wert, string Fenster,
                                                  string DeltaT);

/// <summary>
/// EIN Stundenprofil im Entwurf des Blatts „Nutzungsprofile" für den Assistenten (NP4c) — nur lesbar, Kennzeichen die
/// Nummer ab 1.
/// </summary>
/// <param name="Nummer">Die Nummer der Zeile ab 1 — das Kennzeichen.</param>
/// <param name="Groesse">Die Größe.</param>
/// <param name="Tagesart">WERKTAG oder FREI.</param>
/// <param name="Werte">Die 24 Werte in der Einheit des Profils, mit Semikolon, „aus" bei Heizen und Kühlen.</param>
public sealed record RaumnutzungStundenKiZeile(string Nummer, string Groesse, string Tagesart, string Werte);
