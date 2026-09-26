using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Eine Zone eines Projektgebäudes</b>, wie der Gebäudedialog sie führt (Gebäudesimulation G3,
/// Welle D2; Mehrzonenkonzept 5.1) — das DTO zwischen Hülle und <see cref="ZonenDialog"/>, ohne
/// Fachklasse des Kerns. Ein Gebäude trägt bis zu <see cref="GebaeudeZonenregeln.PFLEGEGRENZE"/> Zonen
/// (Stufe G6a), und der Lauf rechnet sie alle (Stufe G6b).
/// </summary>
/// <remarks>
/// <para><b>Vorläufige Zeilen tragen eine NEGATIVE Id</b> (Softwarearchitektur 3.3 Punkt 2): Nur eine
/// positive Id hat eine Entsprechung in der Datenbank; die endgültige Id entsteht im OK-Weg des
/// Gebäudedialogs (<c>GebaeudeZonenCtrl.SpeichernJeGebaeude</c>).</para>
/// <para><b>Was der Dialog an der Zone bearbeitet</b> (Stufe G6b, Welle W2): Bezeichner, Nutzfläche,
/// Raumhöhe, Volumen, „beheizt", die vier Sollwerte samt Maximalraumtemperatur, Infiltration und
/// Nutzerlüftung, innere Gewinne, Bewohner, Strahlungsanteil und Leistungsgrenze der Heizung — und die
/// Bauteile. Ein leerer Wert (<c>null</c>) übernimmt den des Gebäudes (Vorgabenkaskade
/// <see cref="Zonenvorgaben"/>). Die Kühl- und Übergabespalten der Zone liest der Dialog nicht
/// (Anwenderentscheid A4 (a)); die Hülle hält sie beim Schreiben, wie sie stehen.</para>
/// </remarks>
public sealed class ZoneDaten
{
    /// <summary><c>Tab_Zone.ID</c>; ≤ 0 = vorläufig.</summary>
    public int Id { get; set; }

    /// <summary>Name der Zone (Pflicht).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Nutzfläche der Zone [m²]; <c>null</c> = die Nutzfläche des Gebäudes.</summary>
    public double? Nutzflaeche { get; set; }

    /// <summary>Raumhöhe [m]; <c>null</c> = die des Gebäudes.</summary>
    public double? Raumhoehe { get; set; }

    /// <summary>Luftvolumen [m³]; <c>null</c> = Nutzfläche × Raumhöhe.</summary>
    public double? Volumen { get; set; }

    /// <summary>Wird die Zone beheizt? Nein = sie schwingt frei (kein Heizen, kein Kühlen, kein Sollwert).</summary>
    public bool IstBeheizt { get; set; } = true;

    /// <summary>Raumsolltemperatur am Tag [°C]; <c>null</c> = die des Gebäudes.</summary>
    public double? SollTag { get; set; }

    /// <summary>Nachtabsenkung auf [°C]; <c>null</c> = die des Gebäudes.</summary>
    public double? SollNacht { get; set; }

    /// <summary>Wochenendabsenkung [°C]; <c>null</c> = die des Gebäudes.</summary>
    public double? SollWochenende { get; set; }

    /// <summary>Sollwert in den Ferien [°C]; <c>null</c> = der des Gebäudes.</summary>
    public double? SollFerien { get; set; }

    /// <summary>Maximalraumtemperatur [°C]; <c>null</c> = die des Gebäudes.</summary>
    public double? Maximaleraumtemperatur { get; set; }

    /// <summary>Infiltration [1/h]; <c>null</c> = die des Gebäudes.</summary>
    public double? LuftwechselInfiltration { get; set; }

    /// <summary>Nutzerlüftung [1/h]; <c>null</c> = die des Gebäudes.</summary>
    public double? LuftwechselNutzer { get; set; }

    /// <summary>Innere Wärmegewinne [W]; <c>null</c> = die des Gebäudes nach dem Flächenschlüssel.</summary>
    public double? InterneWaermegewinne { get; set; }

    /// <summary>Bewohner [–]; <c>null</c> = die des Gebäudes nach dem Flächenschlüssel.</summary>
    public double? Bewohner { get; set; }

    /// <summary>Strahlungsanteil der Heizung [–]; <c>null</c> = der des Gebäudes.</summary>
    public double? HeizungStrahlungsanteil { get; set; }

    /// <summary>Leistungsgrenze der Heizung [kW]; <c>null</c> = die des Gebäudes (ab zwei Zonen anteilig).</summary>
    public double? HeizleistungMaxKw { get; set; }

    /// <summary>Die Eingaben der Zone für die Vorgabenkaskade des Kerns (<see cref="Zonenvorgaben"/>).</summary>
    public Zoneneingaben Eingaben()
        => new(Nutzflaeche, Raumhoehe, Volumen, IstBeheizt, SollTag, SollNacht, SollWochenende, SollFerien,
               Maximaleraumtemperatur, HeizungStrahlungsanteil, HeizleistungMaxKw, LuftwechselInfiltration,
               LuftwechselNutzer, InterneWaermegewinne, Bewohner);

    /// <summary>
    /// Die Zone, deren Duplikat diese ist (Stufe G6a) — ihre Id im Arbeitsstand; <c>null</c> = kein
    /// Duplikat. Die Hülle übernimmt damit die Spalten der Vorlage, die die Oberfläche nicht führt
    /// (Sollwerte, Lüftung, Kühl- und Übergabeeingaben), statt sie still auf NULL fallen zu lassen;
    /// Herkunft und Quellkennung der Vorlage gehen nicht mit.
    /// </summary>
    public int? VorlageId { get; set; }

    /// <summary>Die Bauteile der Zone in ihrer Reihenfolge (Rang).</summary>
    public List<BauteilDaten> Bauteile { get; set; } = new();

    /// <summary>Eine entkoppelte Kopie samt Bauteilen — der Arbeitsstand eines Dialogs.</summary>
    public ZoneDaten Kopie()
    {
        var k = (ZoneDaten)MemberwiseClone();
        k.Bauteile = Bauteile.Select(b => b.Kopie()).ToList();
        return k;
    }

    /// <summary>Tragen beide dieselben Werte — Zone und jedes Bauteil, in derselben Reihenfolge?</summary>
    public bool GleicheWerte(ZoneDaten? andere)
    {
        if (andere is null || Id != andere.Id || Bezeichner != andere.Bezeichner || Nutzflaeche != andere.Nutzflaeche
            || VorlageId != andere.VorlageId || Eingaben() != andere.Eingaben()
            || Bauteile.Count != andere.Bauteile.Count) return false;
        for (int i = 0; i < Bauteile.Count; i++)
            if (!Bauteile[i].GleicheWerte(andere.Bauteile[i])) return false;
        return true;
    }
}

/// <summary>
/// <b>Ein Bauteil einer Zone</b> — eine Zeile des Bauteilrasters im <see cref="ZonenDialog"/> und der
/// Satz des <see cref="BauteilDialog"/> (Mehrzonenkonzept 4.2, 5.1). Bauteilart und Randbedingung sind
/// Persistenzwerte (<c>DbWerte.BAUTEILART_*</c>, <c>DbWerte.RANDBEDINGUNG_*</c>), nie Anzeigetexte;
/// <c>null</c> heißt „nicht angegeben" (Vorgabe nach Bauteilart bzw. Wert des Gebäudes).
/// </summary>
public sealed class BauteilDaten
{
    /// <summary><c>Tab_Bauteil.ID</c>; ≤ 0 = vorläufig.</summary>
    public int Id { get; set; }

    /// <summary>Name des Bauteils (Pflicht).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Die Bauteilart als Persistenzwert.</summary>
    public string Bauteilart { get; set; } = DbWerte.BAUTEILART_AUSSENWAND;

    /// <summary>Fläche [m²].</summary>
    public double? Flaeche { get; set; }

    /// <summary>U-Wert [W/(m²K)]; <c>null</c> = aus dem Aufbau.</summary>
    public double? UWert { get; set; }

    /// <summary>g-Wert [–] (Fenster, Vorhangfassade); <c>null</c> = Wert des Gebäudes.</summary>
    public double? GWert { get; set; }

    /// <summary>Rahmenanteil [–] (Fenster, Vorhangfassade); <c>null</c> = Wert des Gebäudes.</summary>
    public double? Rahmenanteil { get; set; }

    /// <summary>Verschattungsfaktor [–] (Fenster, Vorhangfassade); <c>null</c> = Wert des Gebäudes.</summary>
    public double? Verschattung { get; set; }

    /// <summary>Azimut [°], 0° = Nord; <c>null</c> nur waagerecht oder nicht an Außenluft.</summary>
    public double? Azimut { get; set; }

    /// <summary>Neigung [°]; <c>null</c> = nach Bauteilart.</summary>
    public double? Neigung { get; set; }

    /// <summary>Randbedingung als Persistenzwert; <c>null</c> = Außenluft, an Innenwand und Decke „innerhalb der Zone".</summary>
    public string? Randbedingung { get; set; }

    /// <summary>Wärmebrücke ψ·L [W/K]; <c>null</c> = keine.</summary>
    public double? PsiL { get; set; }

    /// <summary>Aufbau des Projekts (<c>Tab_Bauteilaufbau.ID</c>); <c>null</c> = keiner bzw. einer aus dem Katalog.</summary>
    public int? IdAufbau { get; set; }

    /// <summary>
    /// Aufbau des KATALOGS, den der OK-Weg des Gebäudedialogs in das Projekt übernimmt
    /// (<c>BauteilaufbauCtrl.CopyFromStamm</c>) — bis dahin schreibt die Wahl nichts.
    /// </summary>
    public int? IdAufbauStamm { get; set; }

    /// <summary>Der Name des gewählten Aufbaus — nur Anzeige.</summary>
    public string AufbauText { get; set; } = "";

    /// <summary>Der U-Wert des gewählten Aufbaus, im Kern gerechnet — nur Anzeige und Summen.</summary>
    public double? UAufbau { get; set; }

    /// <summary>Herkunft (Persistenzwert) — reist unverändert mit.</summary>
    public string? Herkunft { get; set; }

    /// <summary>Kennung eines Imports — reist unverändert mit.</summary>
    public string? Quellkennung { get; set; }

    /// <summary>
    /// Die Nachbarzone einer Trennfläche (<c>Tab_Bauteil.ID_Nachbarzone</c>, Randbedingung
    /// <c>ZONE</c>; die Id einer Zone desselben Gebäudes, vorläufig negativ); <c>null</c> = keine.
    /// </summary>
    public int? IdNachbarzone { get; set; }

    /// <summary>Die Zuordnung einer Trennfläche (IW/AW); <c>null</c> = die 4-K-Regel.</summary>
    public string? TrennflaecheZuordnung { get; set; }

    /// <summary>Trägt das Bauteil einen Aufbau (aus dem Projekt oder zur Übernahme aus dem Katalog)?</summary>
    public bool MitAufbau => IdAufbau.HasValue || IdAufbauStamm.HasValue;

    /// <summary>Der U-Wert, mit dem das Bauteil rechnet: eingetragen, sonst der des Aufbaus.</summary>
    public double? UWirksam => UWert ?? UAufbau;

    /// <summary>Eine entkoppelte Kopie.</summary>
    public BauteilDaten Kopie() => (BauteilDaten)MemberwiseClone();

    /// <summary>Stimmen die Werte überein, die geschrieben werden?</summary>
    public bool GleicheWerte(BauteilDaten? b)
        => b is not null && Id == b.Id && Bezeichner == b.Bezeichner && Bauteilart == b.Bauteilart
           && Flaeche == b.Flaeche && UWert == b.UWert && GWert == b.GWert && Rahmenanteil == b.Rahmenanteil
           && Verschattung == b.Verschattung && Azimut == b.Azimut && Neigung == b.Neigung
           && Randbedingung == b.Randbedingung && PsiL == b.PsiL && IdAufbau == b.IdAufbau
           && IdAufbauStamm == b.IdAufbauStamm && IdNachbarzone == b.IdNachbarzone
           && TrennflaecheZuordnung == b.TrennflaecheZuordnung;

    /// <summary>Die Angaben für die Prüfregeln des Kerns (<c>GebaeudeZonenCtrl.BauteilPruefen</c>).</summary>
    public Bauteilangabe Angabe()
        => new(Bezeichner, Bauteilart, Flaeche, UWert, MitAufbau, GWert, Rahmenanteil, Verschattung,
               Azimut, Neigung, Randbedingung, PsiL, IdNachbarzone, TrennflaecheZuordnung);
}

/// <summary>
/// Ein Aufbau zur Wahl im <see cref="BauteilDialog"/> — aus dem Projekt (<see cref="Katalog"/>
/// <c>false</c>) oder aus dem Stammkatalog; <see cref="UWert"/> im Kern gerechnet, <c>null</c> =
/// nicht bestimmbar.
/// </summary>
public sealed record AufbauWahl(int Id, bool Katalog, string Text, string Bauteilart, double? UWert);

/// <summary>
/// <b>Ein Luftstrom zwischen zwei Zonen</b> eines Gebäudes (Stufe G6b; <c>Tab_Zonenluftstrom</c>) — das
/// DTO zwischen Hülle, Gebäudeeditor und <see cref="LuftaustauschDialog"/>. Die Zonen heißen über ihre
/// Id im Arbeitsstand (≤ 0 = vorläufig); gerechnet wird nur der eingegebene Strom. Die Reihenfolge von A
/// und B ist gleichgültig — der Kern dreht das Paar beim Schreiben auf A &lt; B.
/// </summary>
public sealed class ZonenluftstromDaten
{
    /// <summary><c>Tab_Zonenluftstrom.ID</c>; ≤ 0 = vorläufig.</summary>
    public int Id { get; set; }

    /// <summary>Die eine Zone; <c>null</c> = noch nicht gewählt.</summary>
    public int? IdZoneA { get; set; }

    /// <summary>Die andere Zone; <c>null</c> = noch nicht gewählt.</summary>
    public int? IdZoneB { get; set; }

    /// <summary>Der Volumenstrom V̇ [m³/h]; <c>null</c> = noch nicht eingegeben.</summary>
    public double? Volumenstrom { get; set; }

    /// <summary>Eine entkoppelte Kopie.</summary>
    public ZonenluftstromDaten Kopie() => (ZonenluftstromDaten)MemberwiseClone();

    /// <summary>Tragen beide dieselben Werte?</summary>
    public bool GleicheWerte(ZonenluftstromDaten? l)
        => l is not null && Id == l.Id && IdZoneA == l.IdZoneA && IdZoneB == l.IdZoneB && Volumenstrom == l.Volumenstrom;
}

/// <summary>
/// <b>Der Stand der Zonen eines Gebäudes</b>, wie der OK-Weg ihn prüft und schreibt (Stufe G6b): die
/// Zonen, die Luftströme zwischen ihnen und der Tagessollwert des Gebäudes (für die Regel „ψ·L der
/// Zonengrenze gehört der wärmeren Zone").
/// </summary>
/// <param name="Zonen">Die Zonen in Listenfolge.</param>
/// <param name="Luftstroeme">Die Luftströme; <c>null</c> = ungeändert (der Schreibweg lässt die gespeicherten stehen).</param>
/// <param name="SollTagGebaeude">Der Tagessollwert des Gebäudes [°C]; <c>null</c> = keiner.</param>
public sealed record ZonenstandDaten(IReadOnlyList<ZoneDaten> Zonen, IReadOnlyList<ZonenluftstromDaten>? Luftstroeme,
                                     double? SollTagGebaeude = null);

/// <summary>
/// Eine Zone zur Wahl als Nachbarzone einer Trennfläche (Stufe G6b) — ihre Id im Arbeitsstand
/// (≤ 0 = vorläufig) und ihr Name.
/// </summary>
public sealed record NachbarzoneWahl(int Id, string Bezeichner);

/// <summary>
/// Eine Trennfläche, die eine ANDERE Zone mit der gezeigten führt (Stufe G6b) — die Gegenseite, im
/// <see cref="ZonenDialog"/> gespiegelt und nur zum Lesen.
/// </summary>
/// <param name="IdZone">Die Id der führenden Zone.</param>
/// <param name="Zone">Ihr Name.</param>
/// <param name="Bauteil">Das Bauteil, wie die führende Zone es trägt.</param>
public sealed record GegenseiteDaten(int IdZone, string Zone, BauteilDaten Bauteil);

/// <summary>
/// Die Schichten eines Aufbaus zum ANSEHEN im <see cref="BauteilDialog"/> — Aufbau, Summenfuß
/// (fertig aus dem Kern) und die Baustoffe, die seine Schichten nennen.
/// </summary>
public sealed record AufbauAnsichtDaten(BauteilaufbauDaten Aufbau, AufbauKennwerteDaten Kennwerte,
                                        IReadOnlyList<BaustoffWahl> Baustoffe);

/// <summary>
/// Der Vorschlag für „Gebäude als eine Zone übernehmen" (Anwenderentscheid vom 25.09.2026
/// „Hochrechnen“) — was die Rückfrage nennt, und die Zone, die danach im Arbeitsstand steht.
/// </summary>
/// <param name="Ok">Ließ sich der Vorschlag bilden?</param>
/// <param name="Meldung">Der Grund, wenn nicht.</param>
/// <param name="Faktor">Der Hochrechnungsfaktor der Berechnung.</param>
/// <param name="Zone">Die Zone mit negativen vorläufigen Ids, hochgerechnet.</param>
/// <param name="NutzflaecheGebaeude">Nutzfläche des Gebäudes [m²].</param>
/// <param name="Einheit">Die Einheit der Angabe im Projekt.</param>
/// <param name="Angabe">Die Angabe im Projekt.</param>
/// <param name="Verbrauchsangabe">Ist die Angabe ein Verbrauch?</param>
/// <param name="HeizgrenzeKw">Die Heizleistungsgrenze des Gebäudes [kW]; <c>null</c> = keine.</param>
/// <param name="KuehlgrenzeKw">Die Kühlleistungsgrenze des gekühlten Gebäudes [kW]; <c>null</c> = keine.</param>
public sealed record ZonenuebernahmeDaten(bool Ok, string Meldung, double Faktor, ZoneDaten? Zone,
                                          double NutzflaecheGebaeude, string Einheit, double Angabe,
                                          bool Verbrauchsangabe, double? HeizgrenzeKw = null, double? KuehlgrenzeKw = null)
{
    /// <summary>
    /// Trägt das Gebäude eine Leistungsgrenze? Sie wird nicht hochgerechnet und gilt danach
    /// unverändert der hochgerechneten Hülle — die Rückfrage nennt sie mit ihrem Wert (E40).
    /// </summary>
    public bool Leistungsgrenzen => HeizgrenzeKw.HasValue || KuehlgrenzeKw.HasValue;
}

/// <summary>Was die Übernahme eines Katalogaufbaus in das Projekt ergab (OK-Weg des Gebäudedialogs).</summary>
public sealed record AufbauUebernahmeErgebnis(bool Ok, string Meldung, AufbauWahl? Wahl);

/// <summary>
/// <b>Der Zonenweg eines Projektgebäudes</b> — alles, was der Gebäudedialog für Zonen und Bauteile
/// braucht, als EIN Parameter (Stufe G3, Welle D2). Die Hülle baut ihn nur für ein gespeichertes
/// Projektgebäude; ohne ihn (Katalogsatz, Verwaltung, ohne Gaben) steht der Zonenreiter mit seinem
/// Grund da, und der Übernahmeknopf ist weich gesperrt.
/// </summary>
/// <remarks>
/// <para><b>Geschrieben wird im OK-Weg, in benannten Schritten</b> (Softwarearchitektur 3.3):
/// erst die Gebäudedaten, dann die Katalogaufbauten (<see cref="AufbauUebernehmen"/>), dann die
/// Zonen (<see cref="Speichern"/>). Kein Delegat, kein Schritt.</para>
/// </remarks>
public sealed class GebaeudeZonenweg
{
    /// <summary>Die Zonen des Projektgebäudes beim Öffnen, in Rangfolge.</summary>
    public IReadOnlyList<ZoneDaten> Zonen { get; init; } = Array.Empty<ZoneDaten>();

    /// <summary>Die Luftströme zwischen den Zonen beim Öffnen (Stufe G6b).</summary>
    public IReadOnlyList<ZonenluftstromDaten> Luftstroeme { get; init; } = Array.Empty<ZonenluftstromDaten>();

    /// <summary>
    /// Bildet den Vorschlag der Übernahme aus dem Arbeitsstand — der Hochrechnungsfaktor kommt aus
    /// der Berechnung des Kerns (ein Jahreslauf). Schreibt nichts.
    /// </summary>
    public Func<GebaeudeKatalogDaten, Task<ZonenuebernahmeDaten>>? Uebernehmen { get; init; }

    /// <summary>OK-Weg, Schritt 2: übernimmt einen Katalogaufbau (Id) in das Projekt.</summary>
    public Func<int, AufbauUebernahmeErgebnis>? AufbauUebernehmen { get; init; }

    /// <summary>
    /// OK-Weg, Schritt 3: schreibt die Zonen des Gebäudes (Abgleich über die Ids) samt ihrer
    /// Luftströme (Stufe G6b; <see cref="ZonenstandDaten.Luftstroeme"/> <c>null</c> = ungeändert);
    /// leer = gelungen.
    /// </summary>
    public Func<ZonenstandDaten, string>? Speichern { get; init; }

    /// <summary>
    /// Die Prüfregeln des Kerns über die ganze Zonenliste samt Kopplung (<c>GebaeudeZonenCtrl.Pruefen</c>,
    /// Stufe G6a/G6b) — der OK-Weg fragt sie VOR dem ersten Schritt, damit eine verletzte Regel nichts
    /// halb schreibt; leer = gültig. Kein Delegat = die Prüfung läuft allein im Schreibweg.
    /// </summary>
    public Func<ZonenstandDaten, string>? Pruefen { get; init; }

    /// <summary>
    /// Die Hinweise der Kopplung über die Zonenliste (<c>Zonenkopplungsregeln.Hinweise</c>, Stufe
    /// G6b: die Hülle jeder Zone ist geschlossen) — sie halten kein OK an; kein Delegat = keine.
    /// </summary>
    public Func<IReadOnlyList<ZoneDaten>, IReadOnlyList<string>>? Hinweise { get; init; }

    /// <summary>Die Aufbauten des Projekts zur Wahl.</summary>
    public IReadOnlyList<AufbauWahl> Projektaufbauten { get; init; } = Array.Empty<AufbauWahl>();

    /// <summary>Die Aufbauten des Stammkatalogs zur Wahl.</summary>
    public IReadOnlyList<AufbauWahl> Katalogaufbauten { get; init; } = Array.Empty<AufbauWahl>();

    /// <summary>Die Schichten eines Aufbaus zum Ansehen; <c>null</c> = keine Ansicht.</summary>
    public Func<AufbauWahl, AufbauAnsichtDaten?>? Aufbau { get; init; }

    /// <summary>
    /// Warum die Datenbank keine Kopplung zwischen Zonen kennt — Trennflächen zu einer Nachbarzone und
    /// Luftaustausch (Schemaschritt S-G fehlt, etwa auf iOS; Stufe G6b); <c>null</c> = sie kennt sie.
    /// Dann bietet der Bauteildialog die Nachbarzone nicht an, und „Luftaustausch …" ist weich gesperrt.
    /// </summary>
    public string? KopplungSperre { get; init; }
}
