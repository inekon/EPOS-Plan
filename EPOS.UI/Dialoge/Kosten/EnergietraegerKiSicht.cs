using System.Globalization;
using EPOS.UI.Dienste;
using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Energieträgerverwaltung" für den Hilfe-Assistenten
/// (Welle KI‑F4) — Listenkopf, Trägerkarte und die beiden Preisblöcke in EINEM
/// Feldsatz.
///
/// <para><b>Warum ein Sichtmodell.</b> Die Maske führt ihren Stand an zwei Orten:
/// Filter, Trägerwahl und die zwei Stammfelder liegen in privaten Feldern des
/// Dialogs, die Preisangaben in <c>EnergietraegerAnsicht.Stand</c>. Ein Katalog, der
/// nur den einen oder nur den anderen anmeldete, verschwiege dem Assistenten die
/// Hälfte der Maske.</para>
///
/// <para><b>Die Preisblöcke sind BAUSTEINE und keine eigenen Masken.</b>
/// <c>StrompreisDetails</c> und <c>BrennstoffBestandteile</c> gehen in keinem
/// eigenen Fenster auf: Sie stehen als aufklappbare Gruppen IN der Trägerkarte, und
/// welcher von beiden dasteht, entscheidet die Familie des Trägers (Strom gegen
/// Brennstoff). Ihre Werte sind deshalb Felder DIESER Maske — dieselbe Bauart wie
/// die Stammfelder der Wärmepumpe, die in einem Baustein stehen und trotzdem zu
/// <c>Form_WP</c> gehören.</para>
///
/// <para><b>Jeder Bestandteil trägt ZWEI Felder: seinen Wert und seinen Schalter.</b>
/// Der Schalter sagt „gepflegt" gegen „kein Anteil" — ein Wert allein könnte das
/// nicht ausdrücken, und ein Betrag hinter einem ausgeschalteten Schalter wirkt
/// nicht. Beides steht sichtbar nebeneinander auf der Maske, also auch im Katalog.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten. Nach jedem Setzen in die Karte läuft <see cref="Nachziehen"/> —
/// derselbe Weg, den auch ein Tastendruck des Anwenders nimmt.</para>
/// </summary>
public sealed class EnergietraegerKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? SucheLesen { get; init; }
    public Action<string>? SucheSetzen { get; init; }

    public Func<int?>? TraegerLesen { get; init; }
    public Action<int?>? TraegerSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? TraegerEintraege { get; init; }

    public Func<string>? StammNameLesen { get; init; }
    public Action<string>? StammNameSetzen { get; init; }

    public Func<int?>? StammGruppeLesen { get; init; }
    public Action<int?>? StammGruppeSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? GruppenEintraege { get; init; }

    /// <summary>Der Stand der Trägerkarte; <c>null</c> = kein Träger gewählt.</summary>
    public Func<EnergietraegerStand?>? StandLesen { get; init; }

    /// <summary>Die Hülle rechnet nach — nach JEDER Änderung in der Karte.</summary>
    public Action? Nachziehen { get; init; }

    /// <summary>
    /// Der Wechsel der Preisbasis — DERSELBE Weg wie die Klappliste der Karte
    /// (<c>PreisbasisGewechselt</c>): Die Hülle rechnet die Anzeige des
    /// Arbeitspreises in die neue Einheit um, der gespeicherte Preis je
    /// Mengeneinheit bleibt. <c>null</c> = der Assistent kann die Preisbasis
    /// nicht setzen.
    /// </summary>
    public Action<int>? PreisbasisSetzen { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI‑D‑Q6)
    // =====================================================================

    /// <summary>Die Energieträger der Liste — Schlüssel ist ihre Katalog-Id.</summary>
    public IReadOnlyList<KiWahleintrag> EnergietraegerWahl
        => TraegerEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Gruppen des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> StammgruppeWahl
        => GruppenEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Preisbasen der Karte — Schlüssel ist ihr Listenplatz.</summary>
    public IReadOnlyList<KiWahleintrag> PreisbasisWahl
        => KiMaskenanmeldung.Eintraege(Stand?.Preisbasen, z => z.Id, z => z.Text);

    // =====================================================================
    //  Listenkopf und Stammfelder
    // =====================================================================

    /// <summary>Der Filtertext über der Trägerliste.</summary>
    public string Suche
    {
        get => SucheLesen?.Invoke() ?? "";
        set => SucheSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der in der Liste markierte Energieträger.</summary>
    public int? Energietraeger
    {
        get => TraegerLesen?.Invoke();
        set => TraegerSetzen?.Invoke(value);
    }

    /// <summary>Die Bezeichnung des Katalogsatzes (nur in der Katalogverwaltung).</summary>
    public string Stammname
    {
        get => StammNameLesen?.Invoke() ?? "";
        set => StammNameSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Gruppe des Katalogsatzes (nur in der Katalogverwaltung).</summary>
    public int? Stammgruppe
    {
        get => StammGruppeLesen?.Invoke();
        set => StammGruppeSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Die Trägerkarte
    // =====================================================================

    private EnergietraegerStand? Stand => StandLesen?.Invoke();

    private void Setze(Action<EnergietraegerStand> schritt)
    {
        EnergietraegerStand? stand = Stand;
        if (stand is null) return;
        schritt(stand);
        Nachziehen?.Invoke();
    }

    /// <summary>Der Arbeitspreis des Trägers.</summary>
    public double Arbeitspreis
    {
        get => Stand?.Arbeitspreis ?? 0;
        set => Setze(s => s.Arbeitspreis = value);
    }

    /// <summary>Der Grundpreis des Trägers.</summary>
    public double Grundpreis
    {
        get => Stand?.Grundpreis ?? 0;
        set => Setze(s => s.Grundpreis = value);
    }

    /// <summary>Der Leistungspreis des Trägers.</summary>
    public double Leistungspreis
    {
        get => Stand?.Leistungspreis ?? 0;
        set => Setze(s => s.Leistungspreis = value);
    }

    /// <summary>Gilt der Leistungspreis je MONAT statt je Jahr?</summary>
    public bool LeistungspreisMonatlich
    {
        get => Stand?.LeistungsModusMonat ?? false;
        set => Setze(s => s.LeistungsModusMonat = value);
    }

    /// <summary>
    /// Q11 — die Staffelgrenze der zweistufigen Leistungspreis-Staffel [kW]; leer =
    /// nicht gepflegt. Nur beim Stromträger im Projektkontext
    /// (<see cref="EnergietraegerStand.MitStaffel"/>) auf der Maske; sonst bleibt
    /// ein Setzen folgenlos.
    /// </summary>
    public double? StaffelGrenze
    {
        get => Stand is { MitStaffel: true } ? Stand.StaffelGrenze : null;
        set => SetzeStaffel(s => s.StaffelGrenze = value);
    }

    /// <summary>Q11 — Leistungspreis bis zur Staffelgrenze [€/(kW·a)].</summary>
    public double? StaffelPreis1
    {
        get => Stand is { MitStaffel: true } ? Stand.StaffelPreis1 : null;
        set => SetzeStaffel(s => s.StaffelPreis1 = value);
    }

    /// <summary>Q11 — Leistungspreis über der Staffelgrenze [€/(kW·a)].</summary>
    public double? StaffelPreis2
    {
        get => Stand is { MitStaffel: true } ? Stand.StaffelPreis2 : null;
        set => SetzeStaffel(s => s.StaffelPreis2 = value);
    }

    /// <summary>Wie <see cref="Setze"/>, aber nur, wo die Karte die Staffel zeigt —
    /// ein Feld, das nicht auf der Maske steht, schreibt der Assistent nicht.</summary>
    private void SetzeStaffel(Action<EnergietraegerStand> schritt)
    {
        if (Stand is not { MitStaffel: true }) return;
        Setze(schritt);
    }

    /// <summary>Der Heizwert des Trägers.</summary>
    public double Heizwert
    {
        get => Stand?.Heizwert ?? 0;
        set => Setze(s => s.Heizwert = value);
    }

    /// <summary>Der Brennwert des Trägers.</summary>
    public double Brennwert
    {
        get => Stand?.Brennwert ?? 0;
        set => Setze(s => s.Brennwert = value);
    }

    /// <summary>
    /// Die Preisbasis, in der der Arbeitspreis eingegeben wird.
    ///
    /// <para><b>Nicht über <see cref="Setze"/>.</b> Nähme der Assistent den Weg
    /// eines Zahlenfeldes — Id setzen, dann nachziehen —, läse die Hülle die
    /// stehende Zahl als Eingabe in der NEUEN Einheit, und der gespeicherte Preis
    /// verschöbe sich um den Heizwert. Der Wechsel geht deshalb über
    /// <see cref="PreisbasisSetzen"/>, denselben Weg wie die Klappliste.</para>
    /// </summary>
    public int? Preisbasis
    {
        get => Stand?.PreisbasisId;
        set
        {
            if (Stand is null || !value.HasValue || PreisbasisSetzen is null) return;
            if (value == Stand.PreisbasisId) return;
            PreisbasisSetzen(value.Value);
        }
    }

    /// <summary>Die Basiseinheit der Preiskette — Anzeige, nicht Eingabe.</summary>
    public string Basiseinheit => Stand?.Basiseinheit ?? "";

    /// <summary>Der ausgerechnete Preis je kWh samt Herleitung — Anzeige.</summary>
    public string Effektivpreis => Stand?.EffektivText ?? "";

    /// <summary>
    /// Der Preisstand, ab dem die Angaben gelten (ISO, <c>JJJJ-MM-TT</c>).
    /// </summary>
    public string GueltigAb
    {
        get => Stand?.GueltigAb?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        set => Setze(s => s.GueltigAb = Datum(value));
    }

    private static DateOnly? Datum(string? wert)
    {
        if (string.IsNullOrWhiteSpace(wert)) return null;
        return DateOnly.TryParse(wert, CultureInfo.InvariantCulture, out DateOnly d)
            || DateOnly.TryParse(wert, CultureInfo.CurrentCulture, out d)
                   ? d
                   : null;
    }

    // =====================================================================
    //  Emissionen der Trägerkarte
    // =====================================================================

    /// <summary>Rechnet die Maske in CO₂-ÄQUIVALENT statt in reinem CO₂?</summary>
    public bool EmissionenAlsCo2e
    {
        get => Stand?.ModusCo2e ?? false;
        set => Setze(s => s.ModusCo2e = value);
    }

    /// <summary>Der CO₂-Ersatzwert, solange der Katalog keinen führt.</summary>
    public double Co2
    {
        get => Stand?.AltCO2 ?? 0;
        set => Setze(s => s.AltCO2 = value);
    }

    /// <summary>Der SO₂-Ersatzwert, solange der Katalog keinen führt.</summary>
    public double So2
    {
        get => Stand?.AltSO2 ?? 0;
        set => Setze(s => s.AltSO2 = value);
    }

    /// <summary>Der NOx-Ersatzwert, solange der Katalog keinen führt.</summary>
    public double Nox
    {
        get => Stand?.AltNOx ?? 0;
        set => Setze(s => s.AltNOx = value);
    }

    // =====================================================================
    //  Baustein „Strompreis Details" — nur an einem Stromträger
    // =====================================================================

    private StrompreisDetailsStand? Strom => Stand?.Zerlegung;

    private void Strompreis(Action<StrompreisDetailsStand> schritt)
    {
        StrompreisDetailsStand? block = Strom;
        if (block is null) return;
        schritt(block);
        Nachziehen?.Invoke();
    }

    /// <summary>Beschaffung — der Einkaufsanteil des Arbeitspreises.</summary>
    public double StromBeschaffung
    {
        get => Strom?.Beschaffung ?? 0;
        set => Strompreis(b => b.Beschaffung = value);
    }

    /// <summary>Ist der Beschaffungsanteil gepflegt?</summary>
    public bool StromBeschaffungAktiv
    {
        get => Strom?.BeschaffungAktiv ?? false;
        set => Strompreis(b => b.BeschaffungAktiv = value);
    }

    /// <summary>Vertrieb — Marge und Abrechnung des Lieferanten.</summary>
    public double StromVertrieb
    {
        get => Strom?.Vertrieb ?? 0;
        set => Strompreis(b => b.Vertrieb = value);
    }

    /// <summary>Ist der Vertriebsanteil gepflegt?</summary>
    public bool StromVertriebAktiv
    {
        get => Strom?.VertriebAktiv ?? false;
        set => Strompreis(b => b.VertriebAktiv = value);
    }

    /// <summary>Das Arbeitspreis-Netzentgelt.</summary>
    public double StromNetzentgelt
    {
        get => Strom?.Netzentgelt ?? 0;
        set => Strompreis(b => b.Netzentgelt = value);
    }

    /// <summary>Ist das Netzentgelt gepflegt?</summary>
    public bool StromNetzentgeltAktiv
    {
        get => Strom?.NetzentgeltAktiv ?? false;
        set => Strompreis(b => b.NetzentgeltAktiv = value);
    }

    /// <summary>Die Stromsteuer.</summary>
    public double Stromsteuer
    {
        get => Strom?.Stromsteuer ?? 0;
        set => Strompreis(b => b.Stromsteuer = value);
    }

    /// <summary>Ist die Stromsteuer gepflegt?</summary>
    public bool StromsteuerAktiv
    {
        get => Strom?.StromsteuerAktiv ?? false;
        set => Strompreis(b => b.StromsteuerAktiv = value);
    }

    /// <summary>Die Konzessionsabgabe.</summary>
    public double StromKonzession
    {
        get => Strom?.Konzession ?? 0;
        set => Strompreis(b => b.Konzession = value);
    }

    /// <summary>Ist die Konzessionsabgabe gepflegt?</summary>
    public bool StromKonzessionAktiv
    {
        get => Strom?.KonzessionAktiv ?? false;
        set => Strompreis(b => b.KonzessionAktiv = value);
    }

    /// <summary>Die Umlagen als EINE Summe.</summary>
    public double StromUmlagen
    {
        get => Strom?.Umlagen ?? 0;
        set => Strompreis(b => b.Umlagen = value);
    }

    /// <summary>Ist die Umlagensumme gepflegt?</summary>
    public bool StromUmlagenAktiv
    {
        get => Strom?.UmlagenAktiv ?? false;
        set => Strompreis(b => b.UmlagenAktiv = value);
    }

    /// <summary>Stehen die Umlagen einzeln statt als Summe?</summary>
    public bool StromUmlagenEinzeln
    {
        get => Strom?.UmlagenEinzeln ?? false;
        set => Strompreis(b => b.UmlagenEinzeln = value);
    }

    /// <summary>Die KWKG-Umlage.</summary>
    public double StromUmlageKwkg
    {
        get => Strom?.UmlageKwkg ?? 0;
        set => Strompreis(b => b.UmlageKwkg = value);
    }

    /// <summary>Ist die KWKG-Umlage gepflegt?</summary>
    public bool StromUmlageKwkgAktiv
    {
        get => Strom?.UmlageKwkgAktiv ?? false;
        set => Strompreis(b => b.UmlageKwkgAktiv = value);
    }

    /// <summary>Die Offshore-Netzumlage.</summary>
    public double StromUmlageOffshore
    {
        get => Strom?.UmlageOffshore ?? 0;
        set => Strompreis(b => b.UmlageOffshore = value);
    }

    /// <summary>Ist die Offshore-Netzumlage gepflegt?</summary>
    public bool StromUmlageOffshoreAktiv
    {
        get => Strom?.UmlageOffshoreAktiv ?? false;
        set => Strompreis(b => b.UmlageOffshoreAktiv = value);
    }

    /// <summary>Die Umlage nach § 19 StromNEV.</summary>
    public double StromUmlageStromNev
    {
        get => Strom?.UmlageStromNev19 ?? 0;
        set => Strompreis(b => b.UmlageStromNev19 = value);
    }

    /// <summary>Ist die § 19-StromNEV-Umlage gepflegt?</summary>
    public bool StromUmlageStromNevAktiv
    {
        get => Strom?.UmlageStromNev19Aktiv ?? false;
        set => Strompreis(b => b.UmlageStromNev19Aktiv = value);
    }

    // =====================================================================
    //  Baustein „Preisbestandteile" — nur an einem Brennstoffträger
    // =====================================================================

    private BrennstoffBestandteileStand? Brennstoff => Stand?.Bestandteile;

    private void Bestandteil(Action<BrennstoffBestandteileStand> schritt)
    {
        BrennstoffBestandteileStand? block = Brennstoff;
        if (block is null) return;
        schritt(block);
        Nachziehen?.Invoke();
    }

    /// <summary>Die Energiesteuer im Arbeitspreis.</summary>
    public double BrennstoffEnergiesteuer
    {
        get => Brennstoff?.Energiesteuer ?? 0;
        set => Bestandteil(b => b.Energiesteuer = value);
    }

    /// <summary>Ist die Energiesteuer gepflegt?</summary>
    public bool BrennstoffEnergiesteuerAktiv
    {
        get => Brennstoff?.EnergiesteuerAktiv ?? false;
        set => Bestandteil(b => b.EnergiesteuerAktiv = value);
    }

    /// <summary>Der CO₂-Bestandteil nach BEHG.</summary>
    public double BrennstoffCo2
    {
        get => Brennstoff?.CO2 ?? 0;
        set => Bestandteil(b => b.CO2 = value);
    }

    /// <summary>Ist der CO₂-Bestandteil gepflegt?</summary>
    public bool BrennstoffCo2Aktiv
    {
        get => Brennstoff?.CO2Aktiv ?? false;
        set => Bestandteil(b => b.CO2Aktiv = value);
    }

    /// <summary>Das Netz- und Messentgelt im Arbeitspreis.</summary>
    public double BrennstoffNetzentgelt
    {
        get => Brennstoff?.Netzentgelt ?? 0;
        set => Bestandteil(b => b.Netzentgelt = value);
    }

    /// <summary>Ist das Netz- und Messentgelt gepflegt?</summary>
    public bool BrennstoffNetzentgeltAktiv
    {
        get => Brennstoff?.NetzentgeltAktiv ?? false;
        set => Bestandteil(b => b.NetzentgeltAktiv = value);
    }

    /// <summary>Beschaffung und Vertrieb im Arbeitspreis.</summary>
    public double BrennstoffVertrieb
    {
        get => Brennstoff?.Vertrieb ?? 0;
        set => Bestandteil(b => b.Vertrieb = value);
    }

    /// <summary>Sind Beschaffung und Vertrieb gepflegt?</summary>
    public bool BrennstoffVertriebAktiv
    {
        get => Brennstoff?.VertriebAktiv ?? false;
        set => Bestandteil(b => b.VertriebAktiv = value);
    }
}
