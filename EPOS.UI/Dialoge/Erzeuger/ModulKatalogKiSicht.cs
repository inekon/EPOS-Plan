using System.Globalization;
using EPOS.UI.Standards;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Das FLACHE Abbild des Modulkatalogs für den Hilfe-Assistenten (Welle KI‑F5) —
/// eine Sichtklasse für alle DREI Ausprägungen: PV-Modul, Stromspeicher und
/// Wechselrichter.
///
/// <para><b>Warum ein Sichtmodell.</b> Der <see cref="ModulKatalogDialog"/> führt
/// seinen Stand nicht als DTO mit benannten Eigenschaften, sondern als LISTE von
/// <see cref="ModulFeldwert"/> — je Feld des <c>ModulKatalogProfil</c> eine Zeile mit
/// Schlüssel, Beschriftung und Wert als TEXT. Welche Zeilen es gibt, sagt das Profil
/// zur Laufzeit. Ein daran angemeldeter Katalog hätte keine Eigenschaft, an die er
/// binden könnte; diese Klasse legt je Profilschlüssel genau eine benannte
/// Eigenschaft darüber.</para>
///
/// <para><b>Eine Klasse, drei Katalogschlüssel.</b> Jede Ausprägung deklariert nur
/// ihre eigenen Felder (<c>Form_AdminPV</c> fünfzehn, <c>Form_AdminStromspeicher</c>
/// vierzehn, <c>Form_AdminWechselrichter</c> sechsundzwanzig); fünf Eigenschaften
/// teilen sie sich, weil dieselben Profilschlüssel dahinterstehen (Bezeichner, Firma,
/// Beschreibung, Leistung, Modulkosten). Eine Eigenschaft, deren Schlüssel die offene
/// Ausprägung nicht führt, liefert leer und nimmt nichts an — gelesen wird sie nie,
/// weil kein Katalogeintrag sie nennt. Dasselbe Muster wie
/// <c>BedarfAdminKiSicht</c>, die drei Bedarfskataloge trägt.</para>
///
/// <para><b>Die Werte bleiben TEXT, die Sicht rechnet um.</b> Gelesen und geschrieben
/// wird mit denselben Regeln, nach denen die Maske ihre Zahlenfelder füllt
/// (<see cref="Zahlen.ZahlParsen"/>, <see cref="Zahlen.GanzzahlParsen"/>, Ausgabe in
/// der laufenden Kultur) — sonst stünde nach einer Assistentensetzung eine Zahl im
/// Feld, die der Dialog selbst nicht mehr läse.</para>
///
/// <para><b>Was DRAUSSEN bleibt</b>: die Katalogliste (zwanzigtausend Module sind eine
/// Menge von Verweisen und keine Auswahl, KI‑D‑Q5), ihr Filterstand und der Aufklapper
/// „Alle Parameter und ihre Verwendung" (reine Auskunft).</para>
/// </summary>
public sealed class ModulKatalogKiSicht
{
    /// <summary>Sucht ein Feld des lebenden Feldsatzes; <c>null</c>, wenn es die
    /// offene Ausprägung nicht führt.</summary>
    public Func<string, ModulFeldwert?>? Feldsuche { get; init; }

    /// <summary>Wird nach jedem Setzen gerufen — der Dialog löscht dort seine
    /// Meldung, so wie nach einer Eingabe von Hand.</summary>
    public Action? Gesetzt { get; init; }

    // =====================================================================
    //  Der Zugriff auf eine Zeile des Feldsatzes
    // =====================================================================

    private ModulFeldwert? Feld(string schluessel) => Feldsuche?.Invoke(schluessel);

    private string Text(string schluessel) => Feld(schluessel)?.Wert ?? "";

    private void TextSetzen(string schluessel, string? wert)
    {
        ModulFeldwert? feld = Feld(schluessel);

        // GESPERRT heisst gesperrt — dieselbe Wache wie im Dialog: Der Bezeichner
        // ist der WHERE-Schluessel des UPDATE, die Herkunft die Auskunft des
        // Imports. Der Katalog deklariert beide ohnehin nur lesend; diese Zeile ist
        // die zweite Sicherung, falls ein Eintrag das einmal vergisst.
        if (feld is null || feld.Gesperrt) return;

        feld.Wert = wert ?? "";
        Gesetzt?.Invoke();
    }

    private double? Zahl(string schluessel)
        => Zahlen.ZahlParsen(Text(schluessel), out double d) ? d : null;

    private void ZahlSetzen(string schluessel, double? wert)
        => TextSetzen(schluessel, wert?.ToString(CultureInfo.CurrentCulture) ?? "");

    private int? Ganzzahl(string schluessel)
        => Zahlen.GanzzahlParsen(Text(schluessel), out int i) ? i : null;

    private void GanzzahlSetzen(string schluessel, int? wert)
        => TextSetzen(schluessel, wert?.ToString(CultureInfo.CurrentCulture) ?? "");

    /// <summary>
    /// Der Listenplatz der Option, deren Datenbankcode im Feld steht — genau die
    /// Umsetzung, die das Auswahlfeld der Maske verwendet.
    /// </summary>
    private int? Wahl(string schluessel)
    {
        ModulFeldwert? feld = Feld(schluessel);
        if (feld is null) return null;

        for (int i = 0; i < feld.Optionen.Count; i++)
            if (string.Equals(feld.Optionen[i].Wert, feld.Wert ?? "", StringComparison.Ordinal))
                return i;

        return feld.Optionen.Count > 0 ? 0 : null;
    }

    private void WahlSetzen(string schluessel, int? platz)
    {
        ModulFeldwert? feld = Feld(schluessel);
        if (feld is null || feld.Gesperrt) return;

        feld.Wert = platz.HasValue && platz.Value >= 0 && platz.Value < feld.Optionen.Count
            ? feld.Optionen[platz.Value].Wert
            : "";
        Gesetzt?.Invoke();
    }

    /// <summary>Die Einträge eines Auswahlfeldes — Schlüssel ist der Listenplatz.</summary>
    private IReadOnlyList<KiWahleintrag> Eintraege(string schluessel)
    {
        ModulFeldwert? feld = Feld(schluessel);
        if (feld is null || feld.Optionen.Count == 0) return Array.Empty<KiWahleintrag>();

        var liste = new List<KiWahleintrag>(feld.Optionen.Count);
        for (int i = 0; i < feld.Optionen.Count; i++)
            liste.Add(new KiWahleintrag(i.ToString(CultureInfo.InvariantCulture),
                                        feld.Optionen[i].Text));
        return liste;
    }

    // =====================================================================
    //  Die Felder ALLER drei Auspraegungen
    // =====================================================================

    /// <summary>Der Bezeichner des Katalogsatzes — nur lesbar (WHERE-Schlüssel).</summary>
    public string Bezeichner
    {
        get => Text(ModulKatalogProfil.FeldBezeichner);
        set => TextSetzen(ModulKatalogProfil.FeldBezeichner, value);
    }

    /// <summary>Der Hersteller.</summary>
    public string Firma
    {
        get => Text(ModulKatalogProfil.FeldFirma);
        set => TextSetzen(ModulKatalogProfil.FeldFirma, value);
    }

    /// <summary>Die Beschreibung (PV-Modul und Wechselrichter, mehrzeilig).</summary>
    public string Beschreibung
    {
        get => Text(ModulKatalogProfil.FeldBeschreibung);
        set => TextSetzen(ModulKatalogProfil.FeldBeschreibung, value);
    }

    /// <summary>
    /// Die Leistung — beim Stromspeicher die Lade-/Entladeleistung [kW], beim
    /// PV-Modul die Nennleistung P<sub>max</sub> [W]. EIN Profilschlüssel, zwei
    /// Bedeutungen; Einheit und Erläuterung stehen je Katalogeintrag.
    /// </summary>
    public double? Leistung
    {
        get => Zahl(ModulKatalogProfil.FeldLeistung);
        set => ZahlSetzen(ModulKatalogProfil.FeldLeistung, value);
    }

    /// <summary>Die Modulkosten — Stromspeicher [€/kWh], PV-Modul [€].</summary>
    public double? Modulkosten
    {
        get => Zahl(ModulKatalogProfil.FeldModulkosten);
        set => ZahlSetzen(ModulKatalogProfil.FeldModulkosten, value);
    }

    // ---- Nur der Stromspeicher ------------------------------------------

    /// <summary>Die Speicherart (Textfeld, nicht Klappliste — so steht sie im Profil).</summary>
    public string Typ
    {
        get => Text(ModulKatalogProfil.FeldTyp);
        set => TextSetzen(ModulKatalogProfil.FeldTyp, value);
    }

    /// <summary>Die Kapazität [kWh].</summary>
    public double? Energie
    {
        get => Zahl(ModulKatalogProfil.FeldEnergie);
        set => ZahlSetzen(ModulKatalogProfil.FeldEnergie, value);
    }

    /// <summary>Der nutzbare Ladezustand [%].</summary>
    public double? Ladezustand
    {
        get => Zahl(ModulKatalogProfil.FeldLadezustand);
        set => ZahlSetzen(ModulKatalogProfil.FeldLadezustand, value);
    }

    /// <summary>Die jährliche Degradation [%].</summary>
    public double? Degradation
    {
        get => Zahl(ModulKatalogProfil.FeldDegradation);
        set => ZahlSetzen(ModulKatalogProfil.FeldDegradation, value);
    }

    /// <summary>Der Umlaufwirkungsgrad als Faktor.</summary>
    public double? WirkungsgradRt
    {
        get => Zahl(ModulKatalogProfil.FeldWirkungsgradRt);
        set => ZahlSetzen(ModulKatalogProfil.FeldWirkungsgradRt, value);
    }

    /// <summary>Die Zyklenlebensdauer.</summary>
    public int? Zyklen
    {
        get => Ganzzahl(ModulKatalogProfil.FeldZyklen);
        set => GanzzahlSetzen(ModulKatalogProfil.FeldZyklen, value);
    }

    /// <summary>Die Verschleisskosten je Zyklus.</summary>
    public double? Verschleisskosten
    {
        get => Zahl(ModulKatalogProfil.FeldVerschleisskosten);
        set => ZahlSetzen(ModulKatalogProfil.FeldVerschleisskosten, value);
    }

    /// <summary>Die leistungsbezogenen Kosten [€/kW].</summary>
    public double? Leistungskosten
    {
        get => Zahl(ModulKatalogProfil.FeldLeistungskosten);
        set => ZahlSetzen(ModulKatalogProfil.FeldLeistungskosten, value);
    }

    /// <summary>Der feste Investitionsanteil [€].</summary>
    public double? InvestitionFix
    {
        get => Zahl(ModulKatalogProfil.FeldInvestitionFix);
        set => ZahlSetzen(ModulKatalogProfil.FeldInvestitionFix, value);
    }

    /// <summary>Die Bereitschaftsleistung [W].</summary>
    public double? Standby
    {
        get => Zahl(ModulKatalogProfil.FeldStandby);
        set => ZahlSetzen(ModulKatalogProfil.FeldStandby, value);
    }

    // ---- Nur das PV-Modul -----------------------------------------------

    /// <summary>Der Modulwirkungsgrad [%].</summary>
    public double? Wirkungsgrad
    {
        get => Zahl(ModulKatalogProfil.FeldWirkungsgrad);
        set => ZahlSetzen(ModulKatalogProfil.FeldWirkungsgrad, value);
    }

    /// <summary>Die Spannung im Leistungsmaximum [V].</summary>
    public double? UMpp
    {
        get => Zahl(ModulKatalogProfil.FeldUMpp);
        set => ZahlSetzen(ModulKatalogProfil.FeldUMpp, value);
    }

    /// <summary>Die Leerlaufspannung [V].</summary>
    public double? ULeerlauf
    {
        get => Zahl(ModulKatalogProfil.FeldULeerlauf);
        set => ZahlSetzen(ModulKatalogProfil.FeldULeerlauf, value);
    }

    /// <summary>Der Strom im Leistungsmaximum [A].</summary>
    public double? IMpp
    {
        get => Zahl(ModulKatalogProfil.FeldIMpp);
        set => ZahlSetzen(ModulKatalogProfil.FeldIMpp, value);
    }

    /// <summary>Der Kurzschlussstrom [A].</summary>
    public double? IKurzschluss
    {
        get => Zahl(ModulKatalogProfil.FeldIKurzschluss);
        set => ZahlSetzen(ModulKatalogProfil.FeldIKurzschluss, value);
    }

    /// <summary>Der Temperaturkoeffizient der Leistung [%/K].</summary>
    public double? GammaPmp
    {
        get => Zahl(ModulKatalogProfil.FeldTempKoeff);
        set => ZahlSetzen(ModulKatalogProfil.FeldTempKoeff, value);
    }

    /// <summary>Die Modullänge [m].</summary>
    public double? Laenge
    {
        get => Zahl(ModulKatalogProfil.FeldLaenge);
        set => ZahlSetzen(ModulKatalogProfil.FeldLaenge, value);
    }

    /// <summary>Die Modulbreite [m].</summary>
    public double? Breite
    {
        get => Zahl(ModulKatalogProfil.FeldBreite);
        set => ZahlSetzen(ModulKatalogProfil.FeldBreite, value);
    }

    /// <summary>Die NOCT-Zelltemperatur [°C].</summary>
    public double? TNoct
    {
        get => Zahl(ModulKatalogProfil.FeldTNoct);
        set => ZahlSetzen(ModulKatalogProfil.FeldTNoct, value);
    }

    /// <summary>Die Zelltechnologie — das einzige Wahlfeld des Modulkatalogs.</summary>
    public int? Technologie
    {
        get => Wahl(ModulKatalogProfil.FeldTechnologie);
        set => WahlSetzen(ModulKatalogProfil.FeldTechnologie, value);
    }

    /// <summary>Die Einträge der Zelltechnologie (Begleiteigenschaft, KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> TechnologieWahl
        => Eintraege(ModulKatalogProfil.FeldTechnologie);

    // ---- Nur der Wechselrichter -----------------------------------------

    /// <summary>Die AC-Nennleistung [kW] — das einzige Pflichtfeld.</summary>
    public double? PAcNenn
    {
        get => Zahl(ModulKatalogProfil.FeldPAcNenn);
        set => ZahlSetzen(ModulKatalogProfil.FeldPAcNenn, value);
    }

    /// <summary>Die maximale AC-Scheinleistung [kVA].</summary>
    public double? SAcMax
    {
        get => Zahl(ModulKatalogProfil.FeldSAcMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldSAcMax, value);
    }

    /// <summary>Die maximale DC-Leistung [kW].</summary>
    public double? PDcMax
    {
        get => Zahl(ModulKatalogProfil.FeldPDcMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldPDcMax, value);
    }

    /// <summary>Der Gerätepreis [€].</summary>
    public double? Kosten
    {
        get => Zahl(ModulKatalogProfil.FeldKosten);
        set => ZahlSetzen(ModulKatalogProfil.FeldKosten, value);
    }

    /// <summary>Woher der Satz stammt — Auskunft des Imports, nur lesbar.</summary>
    public string Herkunft
    {
        get => Text(ModulKatalogProfil.FeldHerkunft);
        set => TextSetzen(ModulKatalogProfil.FeldHerkunft, value);
    }

    /// <summary>Die untere MPP-Spannung [V].</summary>
    public double? UMppMin
    {
        get => Zahl(ModulKatalogProfil.FeldUMppMin);
        set => ZahlSetzen(ModulKatalogProfil.FeldUMppMin, value);
    }

    /// <summary>Die obere MPP-Spannung [V].</summary>
    public double? UMppMax
    {
        get => Zahl(ModulKatalogProfil.FeldUMppMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldUMppMax, value);
    }

    /// <summary>Die höchste zulässige DC-Spannung [V].</summary>
    public double? UDcMax
    {
        get => Zahl(ModulKatalogProfil.FeldUDcMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldUDcMax, value);
    }

    /// <summary>Die Startspannung [V].</summary>
    public double? UStart
    {
        get => Zahl(ModulKatalogProfil.FeldUStart);
        set => ZahlSetzen(ModulKatalogProfil.FeldUStart, value);
    }

    /// <summary>Der höchste DC-Arbeitsstrom je MPPT [A].</summary>
    public double? IDcMax
    {
        get => Zahl(ModulKatalogProfil.FeldIDcMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldIDcMax, value);
    }

    /// <summary>Der höchste Kurzschlussstrom je MPPT [A].</summary>
    public double? IScMax
    {
        get => Zahl(ModulKatalogProfil.FeldIScMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldIScMax, value);
    }

    /// <summary>Die Zahl der MPP-Tracker.</summary>
    public int? AnzahlMppt
    {
        get => Ganzzahl(ModulKatalogProfil.FeldAnzahlMppt);
        set => GanzzahlSetzen(ModulKatalogProfil.FeldAnzahlMppt, value);
    }

    /// <summary>Die Zahl der Stränge je MPP-Tracker.</summary>
    public int? StraengeJeMppt
    {
        get => Ganzzahl(ModulKatalogProfil.FeldStraengeJeMppt);
        set => GanzzahlSetzen(ModulKatalogProfil.FeldStraengeJeMppt, value);
    }

    /// <summary>Der Wirkungsgrad bei 5 % Teillast (Faktor).</summary>
    public double? Eta05
    {
        get => Zahl(ModulKatalogProfil.FeldEta05);
        set => ZahlSetzen(ModulKatalogProfil.FeldEta05, value);
    }

    /// <summary>Der Wirkungsgrad bei 10 % Teillast (Faktor).</summary>
    public double? Eta10
    {
        get => Zahl(ModulKatalogProfil.FeldEta10);
        set => ZahlSetzen(ModulKatalogProfil.FeldEta10, value);
    }

    /// <summary>Der Wirkungsgrad bei 20 % Teillast (Faktor).</summary>
    public double? Eta20
    {
        get => Zahl(ModulKatalogProfil.FeldEta20);
        set => ZahlSetzen(ModulKatalogProfil.FeldEta20, value);
    }

    /// <summary>Der Wirkungsgrad bei 30 % Teillast (Faktor).</summary>
    public double? Eta30
    {
        get => Zahl(ModulKatalogProfil.FeldEta30);
        set => ZahlSetzen(ModulKatalogProfil.FeldEta30, value);
    }

    /// <summary>Der Wirkungsgrad bei 50 % Teillast (Faktor).</summary>
    public double? Eta50
    {
        get => Zahl(ModulKatalogProfil.FeldEta50);
        set => ZahlSetzen(ModulKatalogProfil.FeldEta50, value);
    }

    /// <summary>Der Wirkungsgrad bei Volllast (Faktor).</summary>
    public double? Eta100
    {
        get => Zahl(ModulKatalogProfil.FeldEta100);
        set => ZahlSetzen(ModulKatalogProfil.FeldEta100, value);
    }

    /// <summary>Der europäische Wirkungsgrad (Faktor).</summary>
    public double? EtaEuro
    {
        get => Zahl(ModulKatalogProfil.FeldEtaEuro);
        set => ZahlSetzen(ModulKatalogProfil.FeldEtaEuro, value);
    }

    /// <summary>Der höchste Wirkungsgrad (Faktor).</summary>
    public double? EtaMax
    {
        get => Zahl(ModulKatalogProfil.FeldEtaMax);
        set => ZahlSetzen(ModulKatalogProfil.FeldEtaMax, value);
    }

    /// <summary>Die Bereitschaftsleistung [W].</summary>
    public double? PStandby
    {
        get => Zahl(ModulKatalogProfil.FeldPStandby);
        set => ZahlSetzen(ModulKatalogProfil.FeldPStandby, value);
    }

    /// <summary>Die Nachtverlustleistung [W].</summary>
    public double? PNacht
    {
        get => Zahl(ModulKatalogProfil.FeldPNacht);
        set => ZahlSetzen(ModulKatalogProfil.FeldPNacht, value);
    }
}
