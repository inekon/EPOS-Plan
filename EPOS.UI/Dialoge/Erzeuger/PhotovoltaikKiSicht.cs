using KiKern;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Das FLACHE Abbild des Photovoltaik-Projektdialogs für den Hilfe-Assistenten
/// (Welle KI‑F7, Anwenderentscheid 21.09.2026).
///
/// <para><b>Warum überhaupt ein Sichtmodell — die Maske HAT doch ein Daten-Objekt.</b>
/// Sie hat eines, und diese Klasse reicht jede seiner Eigenschaften unverändert durch:
/// <see cref="ErzeugerZeile"/> bleibt der Ort, an dem Neigung, Azimut, Modellfelder und
/// Stränge stehen. Zwei Größen der Maske stehen dort aber NICHT — die
/// Auslegungstemperaturen. Sie gehören dem PROJEKT
/// (<c>Tab_Einstellungen.Ausleg_T_Kalt</c> / <c>…Ausleg_T_Heiss</c>), stehen im
/// Strangabschnitt vor dem Anwender und werden über
/// <c>KonfigurationCtrl.AuslegungstemperaturenSchreiben</c> zurückgeschrieben. Die
/// Brücke kennt je Maske EIN Daten-Objekt; also braucht <c>Form_PV</c> eines, das beide
/// Herkünfte trägt.</para>
///
/// <para><b>Sie hält keinen Zustand.</b> Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten — die gewählte Projektzeile wechselt mit jedem Klick in der Liste, und die
/// zwei Temperaturen liegen in den lebenden Feldern des Strangbausteins. Ohne gewählte
/// Zeile meldet der Dialog gar keine Sicht an; die Felder sind dann leer, genau wie auf
/// der Maske.</para>
///
/// <para><b>Das RECHENMODELL ist ein Wahlfeld</b> (KI‑D‑Q6, KI‑D‑Q7): Auf der Maske
/// steht ein Auswahlfeld mit „Einfach" und „Erweitert", in der Anlage ein
/// Wahrheitswert. Die Begleiteigenschaft <see cref="ModellErweitertWahl"/> trägt die
/// zwei Einträge mit denselben Schlüsseln (0 und 1) und denselben Texten, die der
/// Anwender liest; die Eigenschaft selbst bleibt der Wahrheitswert der Anlage.</para>
/// </summary>
/// <remarks>
/// <b>Seit Welle #458 (Stufe 2) dazu der Aufklapper „Alle Daten"</b> — der gewählte
/// KATALOGsatz des Moduls als FELDTAFEL (<see cref="AlleDaten"/>, Feldkarte aus dem
/// <c>ModulKatalogProfil</c>). Die Sicht steht deshalb auch, solange nur eine Katalogzeile
/// gewählt ist; die Felder der Anlage und die zwei Auslegungstemperaturen sind dann leer,
/// und ein Setzen lehnt benannt ab — die Maske zeigt den Strangabschnitt nur zu einer
/// Projektzeile.
/// </remarks>
public sealed class PhotovoltaikKiSicht : EPOS.UI.Dienste.IKiFeldtafel
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Die GEWÄHLTE Projektzeile; <c>null</c> = keine gewählt.</summary>
    public Func<ErzeugerZeile?>? Zeilenquelle { get; init; }

    /// <summary>
    /// Wählt die EINZIGE Projektzeile, wenn keine gewählt ist, und liefert sie;
    /// <c>null</c> = keine oder mehrere Zeilen (dann wird nicht geraten).
    /// </summary>
    public Func<ErzeugerZeile?>? Einzelwahl { get; init; }

    /// <summary>
    /// Der Übernahmeweg der Hand nach dem Setzen eines Felds der Anlage — dasselbe
    /// <c>Uebernehmen</c>, das die Eingabefelder rufen (Arbeitskopie, nicht Datenbank).
    /// Der Energieträger und die zwei Auslegungstemperaturen gehen eigene Wege.
    /// </summary>
    public Action<ErzeugerZeile>? Uebernommen { get; init; }

    /// <summary>Die lebende Auslegungstemperatur des kalten Falls [°C].</summary>
    public Func<double?>? KaltLesen { get; init; }

    /// <summary>Die lebende Auslegungstemperatur des heißen Falls [°C].</summary>
    public Func<double?>? HeissLesen { get; init; }

    /// <summary>
    /// Der Schreibweg des kalten Falls — DERSELBE, den das Eingabefeld der Maske geht
    /// (Prüfung der Strangampel, dann <c>TemperaturenSetzen</c> in die
    /// Projekteinstellungen).
    /// </summary>
    public Action<double?>? KaltSetzen { get; init; }

    /// <summary>Der Schreibweg des heißen Falls — dieselbe Bauart.</summary>
    public Action<double?>? HeissSetzen { get; init; }

    /// <summary>Die zwei Einträge des Auswahlfelds „Rechenmodell".</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? ModellEintraege { get; init; }

    /// <summary>Die Feldtafel des Aufklappers „Alle Daten" (Welle #458, Stufe 2).</summary>
    public AlleDatenTafel? AlleDaten { get; init; }

    /// <inheritdoc/>
    public object? Lesen(string schluessel) => AlleDaten?.Lesen(schluessel);

    /// <inheritdoc/>
    public void Setzen(string schluessel, object? wert) => AlleDaten?.Setzen(schluessel, wert);

    private ErzeugerZeile? Zeile => Zeilenquelle?.Invoke();

    /// <summary>
    /// Schreibt in die gewählte Zeile — oder in die einzige, die <see cref="Einzelwahl"/>
    /// wählt — und geht danach den Übernahmeweg; ohne Zeile eine BENANNTE Absage
    /// (Meldung 10.10.2026: vorher verwarf der Setzer den Wert still).
    /// </summary>
    private void Schreibe(Action<ErzeugerZeile> schreiben, bool uebernehmen)
    {
        ErzeugerZeile z = Zeile ?? Einzelwahl?.Invoke()
            ?? throw new InvalidOperationException(
                   WindowsFormsApplication1.MyResource.Resource.KI_ERZ_KEINE_PROJEKTZEILE);
        schreiben(z);
        if (uebernehmen) Uebernommen?.Invoke(z);
    }

    // =====================================================================
    //  Die Anlage
    // =====================================================================

    /// <summary>Modulneigung [°].</summary>
    public int? Neigung
    {
        get => Zeile?.Neigung;
        set => Schreibe(z => z.Neigung = value, uebernehmen: true);
    }

    /// <summary>Azimut [°].</summary>
    public int? Azimut
    {
        get => Zeile?.Azimut;
        set => Schreibe(z => z.Azimut = value, uebernehmen: true);
    }

    /// <summary>Die zugeordnete Energieträgervariante; 0 = keine.</summary>
    public int CarrierId
    {
        get => Zeile?.CarrierId ?? 0;
        set => Schreibe(z => z.CarrierId = value, uebernehmen: false);
    }

    /// <summary>Anzahl Module der Anlage.</summary>
    public double? AnzahlModule
    {
        get => Zeile?.AnzahlModule;
        set => Schreibe(z => z.AnzahlModule = value, uebernehmen: true);
    }

    // =====================================================================
    //  Die Modellfelder (PvModellFelder)
    // =====================================================================

    /// <summary>
    /// Das Rechenmodell: <c>true</c> = erweitert, <c>false</c> = einfach. Gesetzt wird
    /// es über den Text des Auswahlfelds (siehe <see cref="ModellErweitertWahl"/>).
    /// </summary>
    public bool ModellErweitert
    {
        get => Zeile?.ModellErweitert ?? false;
        set => Schreibe(z => z.ModellErweitert = value, uebernehmen: true);
    }

    /// <summary>Die zwei Einträge des Auswahlfelds „Rechenmodell" (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> ModellErweitertWahl
        => ModellEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Wechselrichter-Wirkungsgrad als Faktor; <c>null</c> = 0,95.</summary>
    public double? WrWirkungsgrad
    {
        get => Zeile?.WrWirkungsgrad;
        set => Schreibe(z => z.WrWirkungsgrad = value, uebernehmen: true);
    }

    /// <summary>Pauschale Systemverluste [%]; <c>null</c> = 0.</summary>
    public double? Systemverluste
    {
        get => Zeile?.Systemverluste;
        set => Schreibe(z => z.Systemverluste = value, uebernehmen: true);
    }

    /// <summary>Bodenalbedo vor der Anlage (0…1); <c>null</c> = 0,2.</summary>
    public double? Albedo
    {
        get => Zeile?.Albedo;
        set => Schreibe(z => z.Albedo = value, uebernehmen: true);
    }

    // =====================================================================
    //  Wechselrichter und Stränge (PvStraengeFelder)
    // =====================================================================

    /// <summary>Der sichtbare Wechselrichterweg dieser Anlage (W6‑E‑3).</summary>
    public bool MitWechselrichter
    {
        get => Zeile?.MitWechselrichter ?? false;
        set => Schreibe(z => z.MitWechselrichter = value, uebernehmen: true);
    }

    /// <summary>
    /// Die Stränge der Anlage — die LEBENDE Liste der Zeile, nicht eine Kopie: Der
    /// Anwender legt Stränge an und löscht sie, während der Dialog steht, und der
    /// Katalog löst seine Spalten bei jedem Zugriff neu auf.
    /// </summary>
    public List<StrangZeile> Straenge => Zeile?.Straenge ?? LEER;

    private static readonly List<StrangZeile> LEER = new();

    // =====================================================================
    //  Die zwei Auslegungstemperaturen — sie gehören dem PROJEKT
    // =====================================================================

    /// <summary>
    /// Auslegungstemperatur kalt [°C] des Projekts; <c>null</c> = Vorgabe −10 °C.
    /// <b>Sie gilt für JEDE Anlage des Projekts</b>, nicht nur für die gewählte Zeile.
    /// </summary>
    public double? AuslegungKalt
    {
        get => Zeile is null ? null : KaltLesen?.Invoke();
        set => Schreibe(_ => KaltSetzen?.Invoke(value), uebernehmen: false);
    }

    /// <summary>
    /// Auslegungstemperatur heiß [°C] des Projekts; <c>null</c> = Vorgabe +70 °C.
    /// Ebenfalls projektweit.
    /// </summary>
    public double? AuslegungHeiss
    {
        get => Zeile is null ? null : HeissLesen?.Invoke();
        set => Schreibe(_ => HeissSetzen?.Invoke(value), uebernehmen: false);
    }
}
