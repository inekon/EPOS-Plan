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
public sealed class PhotovoltaikKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Die GEWÄHLTE Projektzeile; <c>null</c> = keine gewählt.</summary>
    public Func<ErzeugerZeile?>? Zeilenquelle { get; init; }

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

    private ErzeugerZeile? Zeile => Zeilenquelle?.Invoke();

    // =====================================================================
    //  Die Anlage
    // =====================================================================

    /// <summary>Modulneigung [°].</summary>
    public int? Neigung
    {
        get => Zeile?.Neigung;
        set { if (Zeile is ErzeugerZeile z) z.Neigung = value; }
    }

    /// <summary>Azimut [°].</summary>
    public int? Azimut
    {
        get => Zeile?.Azimut;
        set { if (Zeile is ErzeugerZeile z) z.Azimut = value; }
    }

    /// <summary>Die zugeordnete Energieträgervariante; 0 = keine.</summary>
    public int CarrierId
    {
        get => Zeile?.CarrierId ?? 0;
        set { if (Zeile is ErzeugerZeile z) z.CarrierId = value; }
    }

    /// <summary>Anzahl Module der Anlage.</summary>
    public double? AnzahlModule
    {
        get => Zeile?.AnzahlModule;
        set { if (Zeile is ErzeugerZeile z) z.AnzahlModule = value; }
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
        set { if (Zeile is ErzeugerZeile z) z.ModellErweitert = value; }
    }

    /// <summary>Die zwei Einträge des Auswahlfelds „Rechenmodell" (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> ModellErweitertWahl
        => ModellEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Wechselrichter-Wirkungsgrad als Faktor; <c>null</c> = 0,95.</summary>
    public double? WrWirkungsgrad
    {
        get => Zeile?.WrWirkungsgrad;
        set { if (Zeile is ErzeugerZeile z) z.WrWirkungsgrad = value; }
    }

    /// <summary>Pauschale Systemverluste [%]; <c>null</c> = 0.</summary>
    public double? Systemverluste
    {
        get => Zeile?.Systemverluste;
        set { if (Zeile is ErzeugerZeile z) z.Systemverluste = value; }
    }

    // =====================================================================
    //  Wechselrichter und Stränge (PvStraengeFelder)
    // =====================================================================

    /// <summary>Der sichtbare Wechselrichterweg dieser Anlage (W6‑E‑3).</summary>
    public bool MitWechselrichter
    {
        get => Zeile?.MitWechselrichter ?? false;
        set { if (Zeile is ErzeugerZeile z) z.MitWechselrichter = value; }
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
        get => KaltLesen?.Invoke();
        set => KaltSetzen?.Invoke(value);
    }

    /// <summary>
    /// Auslegungstemperatur heiß [°C] des Projekts; <c>null</c> = Vorgabe +70 °C.
    /// Ebenfalls projektweit.
    /// </summary>
    public double? AuslegungHeiss
    {
        get => HeissLesen?.Invoke();
        set => HeissSetzen?.Invoke(value);
    }
}
