using System.Globalization;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Kalenderbedienung über dem Arbeitsstand</b> (Konzept Konditionierungsprofile 7.8, E110; Welle K1b): jede
/// Handlung des Bausteins <see cref="KalenderbedienungAbschnitt"/> geht über ihren Delegaten im <see cref="KonditionierungWeg"/>
/// (Kern-Schicht <c>Kalenderbedienung</c>) in den Arbeitsstand — ein Schritt für „Zurücknehmen", geschrieben wird mit
/// dem OK des Editors. An einer Zone wirkt sie auf deren Kalender.
/// </summary>
public sealed partial class KonditionierungBearbeitung
{
    /// <summary>Die Zone der Bearbeitung; <c>null</c> = das Gebäude.</summary>
    public int? ZonenId => _zone?.Id;

    /// <summary>Bietet der Weg die Kalenderbedienung (mindestens die Ansicht)?</summary>
    public bool MitKalenderbedienung => MitWeg && Weg.Sperre is null && Weg.Kalenderansicht is not null;

    /// <summary>
    /// Der Schlüssel der Ansicht: die Vorschau (<see cref="Vorschauschluessel"/>), die Zone und die Ferienzeiträume des
    /// Arbeitsstands (die Felder unter der Matrix ändern keine Fassung).
    /// </summary>
    public string Kalenderschluessel(KonditionierungGroesse g)
        => string.Join("|", Vorschauschluessel(g), _zone?.Id.ToString(CultureInfo.InvariantCulture) ?? "-",
                       string.Join(",", _arbeit.Ferienbeginne()), string.Join(",", _arbeit.Ferienenden()));

    /// <summary>Die Ansicht der Größe; <c>null</c> = keine (ohne Weg, ohne Delegat oder bei einem ungültigen Stand).</summary>
    public KalenderAnsicht? Kalenderansicht(KonditionierungGroesse g)
    {
        if (!MitKalenderbedienung) return null;
        try { return Weg.Kalenderansicht!(Eingabestand(), Ort(g)); }
        catch (Exception) { return null; }
    }

    /// <summary>Der Pinsel auf das Wochenprofil (Rang <c>null</c> = Standardwoche).</summary>
    public bool ProfilPinseln(KonditionierungGroesse g, int? rang, KalenderPinselstrich strich, long? idWoche = null)
        => Bietet(KonditionierungHandlung.ProfilPinsel)
           && Ausfuehren("KP|" + g, s => Weg.ProfilPinsel!(s, new KalenderProfilort(Ort(g), rang, idWoche), strich));

    /// <summary>„Tag kopieren": Quelltag des Quellprofils auf die Zieltage des Zielprofils (gleiche Einheit).</summary>
    public bool TagKopieren(KonditionierungGroesse g, int? rang, int quelltag, KonditionierungGroesse zielGroesse, int? zielRang,
                            IReadOnlyList<int> zieltage, long? idWoche = null, long? zielIdWoche = null)
        => Bietet(KonditionierungHandlung.TagKopieren)
           && Ausfuehren("KT|" + g, s => Weg.TagKopieren!(s, new KalenderProfilort(Ort(g), rang, idWoche), quelltag,
                                                           new KalenderProfilort(Ort(zielGroesse), zielRang, zielIdWoche), zieltage));

    /// <summary>„Woche kopieren" in ein anderes Profil derselben Größe oder einer Größe gleicher Einheit.</summary>
    public bool WocheKopieren(KonditionierungGroesse g, int? rang, KonditionierungGroesse zielGroesse, int? zielRang,
                              long? idWoche = null, long? zielIdWoche = null)
        => Bietet(KonditionierungHandlung.WocheKopieren)
           && Ausfuehren("KW|" + g, s => Weg.WocheKopieren!(s, new KalenderProfilort(Ort(g), rang, idWoche),
                                                            new KalenderProfilort(Ort(zielGroesse), zielRang, zielIdWoche)));

    /// <summary>Eine Zuordnungszeile oder einen Einzeltag setzen — gekoppelt in den Größen von <paramref name="giltFuer"/>.</summary>
    public bool ZuordnungSetzen(KalenderZeilenschluessel? alt, KalenderZeilenschluessel neu, KalenderWirkungsangabe wirkung,
                                IReadOnlyList<KonditionierungGroesse> giltFuer)
        => Bietet(KonditionierungHandlung.ZuordnungSetzen)
           && Ausfuehren("KZ", s => Weg.ZuordnungSetzen!(s, _zone?.Id, alt, neu, wirkung, giltFuer));

    /// <summary>Eine gekoppelte Zeile in allen Größen löschen.</summary>
    public bool ZuordnungLoeschen(KalenderZeilenschluessel schluessel)
        => Bietet(KonditionierungHandlung.ZuordnungLoeschen)
           && Ausfuehren("KL", s => Weg.ZuordnungLoeschen!(s, _zone?.Id, schluessel));

    /// <summary>Die Ferienzeiträume setzen (nur am Gebäude — Ferien gelten für Gebäude und Zonen).</summary>
    public bool FerienSetzen(IReadOnlyList<KalenderFerienzeile> ferien)
        => !IstZone && Bietet(KonditionierungHandlung.FerienSetzen)
           && Ausfuehren("KF", s => Weg.FerienSetzen!(s, ferien));

    /// <summary>Die Saison von–bis einer Größe; beide <c>null</c> = ganzjährig.</summary>
    public bool SaisonSetzen(KonditionierungGroesse g, int? von, int? bis)
        => Bietet(KonditionierungHandlung.SaisonSetzen)
           && Ausfuehren("KS|" + g, s => Weg.SaisonSetzen!(s, Ort(g), von, bis));

    /// <summary>„Feiertage laden" (bundeseinheitlich) in den gewählten Größen.</summary>
    public bool FeiertageLaden(IReadOnlyList<KonditionierungGroesse> giltFuer)
        => Bietet(KonditionierungHandlung.FeiertageLaden)
           && Ausfuehren("KH", s => Weg.FeiertageLaden!(s, _zone?.Id, giltFuer));

    /// <summary>„Monat kopieren" (Monate 1 … 12).</summary>
    public bool MonatKopieren(int quellmonat, int zielmonat)
        => Bietet(KonditionierungHandlung.MonatKopieren)
           && Ausfuehren("KM", s => Weg.MonatKopieren!(s, _zone?.Id, quellmonat, zielmonat));

    /// <summary>Die Größen mit angelegtem Kalender — die Vorgabe von „gilt für".</summary>
    // ------------------------------------------------------------ Stufe 2 (Welle K2-U)

    /// <summary>
    /// Legt eine benannte Woche der Größe an; ihre Werte kommen aus dem Wochenprofil am Quellort (<paramref name="rang"/>,
    /// <paramref name="idWoche"/>; beide leer = die Standardwoche). Ein doppelter oder leerer Name wird benannt abgelehnt.
    /// </summary>
    public bool WocheAnlegen(KonditionierungGroesse g, string name, int? rang = null, long? idWoche = null)
        => Bietet(KonditionierungHandlung.WocheAnlegen)
           && Ausfuehren("KWA|" + g, s => Weg.WocheAnlegen!(s, Ort(g), name ?? "", new KalenderProfilort(Ort(g), rang, idWoche)));

    /// <summary>Benennt eine benannte Woche der Größe um (Name je Größe eindeutig).</summary>
    public bool WocheUmbenennen(KonditionierungGroesse g, long idWoche, string name)
        => Bietet(KonditionierungHandlung.WocheUmbenennen)
           && Ausfuehren("KWU|" + g, s => Weg.WocheUmbenennen!(s, Ort(g), idWoche, name ?? ""));

    /// <summary>Löscht eine benannte Woche — abgelehnt mit Name und Zeilenzahl, solange eine Zeile auf sie verweist.</summary>
    public bool WocheLoeschen(KonditionierungGroesse g, long idWoche)
        => Bietet(KonditionierungHandlung.WocheLoeschen)
           && Ausfuehren("KWL|" + g, s => Weg.WocheLoeschen!(s, Ort(g), idWoche));

    /// <summary>Setzt die Wochenendtage des Gebäudes (0 = Montag; leer = Samstag und Sonntag). Nur am Gebäude.</summary>
    public bool WochenendeSetzen(IReadOnlyList<int> tage)
        => !IstZone && Bietet(KonditionierungHandlung.WochenendeSetzen)
           && Ausfuehren("KWE", s => Weg.WochenendeSetzen!(s, tage));

    /// <summary>Setzt das Feiertagsland des Gebäudes (<c>null</c> = nur bundeseinheitlich). Nur am Gebäude.</summary>
    public bool FeiertagslandSetzen(string? land)
        => !IstZone && Bietet(KonditionierungHandlung.FeiertagslandSetzen)
           && Ausfuehren("KFL", s => Weg.FeiertagslandSetzen!(s, land));

    /// <summary>Setzt die Ferienliste mit Namen (beliebig viele). Nur am Gebäude.</summary>
    public bool FerienlisteSetzen(IReadOnlyList<KalenderFerienzeile> ferien)
        => !IstZone && Bietet(KonditionierungHandlung.FerienlisteSetzen)
           && Ausfuehren("KFE", s => Weg.FerienlisteSetzen!(s, ferien));

    public IReadOnlyList<KonditionierungGroesse> AngelegteGroessen
        => KonditionierungDaten.Alle.Where(Angelegt).ToList();
}
