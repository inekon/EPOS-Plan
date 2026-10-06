namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Die Übergabefelder des Erdreich-Quellendialogs (iU9-W10a.3) — hinein und, nach
/// „OK", wieder hinaus.
///
/// <para><b>Warum ein Record und keine Felder auf der Komponente.</b> Der Vorläufer
/// <c>Form_QuelleErdreich</c> trug ACHTZEHN öffentliche Felder, die der Aufrufer vor
/// <c>ShowDialog</c> einzeln setzte und danach einzeln zurücklas (Vermessung §3 a).
/// Ein Record macht daraus einen Satz: Der Aufrufer baut ihn, die Hülle reicht ihn
/// hinein, und was zurückkommt, ist derselbe Satz mit den geänderten Werten. Was der
/// Dialog NICHT ändert, kann so auch nicht versehentlich verlorengehen.</para>
///
/// <para><b>Die Ergebnisgrößen des Laufs stehen NICHT hier</b>, sondern in
/// <c>ErdreichAuswertung.ErdreichLaufErgebnis</c> (W10a.0b) — sie sind Eingang der
/// Auslegungsprüfung und kein Rückgabewert: Der Aufrufer schreibt sie nirgends hin.</para>
/// </summary>
public sealed record QuelleErdreichDaten
{
    /// <summary>Name der Wärmepumpe — er steht im Titel („Wärmequelle Erdreich — {0}").</summary>
    public string WPName { get; init; } = "";

    /// <summary>
    /// Das Projekt. <c>0</c> heißt „kein Projektbezug" — dann lässt sich kein
    /// Simulationslauf starten, und der Dialog sagt das (Vermessung §3 b).
    /// </summary>
    public int IdProjekt { get; init; }

    /// <summary>Die Energieanlage — sie ordnet dem Dialog das Laufergebnis zu.</summary>
    public int IdAnlage { get; init; }

    /// <summary>
    /// <c>WQ_Quellsystem</c>: Erdkollektor oder Erdsonde. Der Vergleich läuft
    /// <c>OrdinalIgnoreCase</c> gegen den Sondenwert; alles andere heißt Kollektor.
    /// </summary>
    public string Quellsystem { get; init; } = "";

    /// <summary>
    /// <c>WQ_Tiefe</c>. <b>Zwei Bedeutungen in EINEM Feld</b>, wie im Bestand: beim
    /// Kollektor die Verlegetiefe, bei der Sonde die Länge JE Sonde
    /// (<c>btnOk_Click</c>:1248 schreibt <c>Tiefe = laenge</c>).
    /// </summary>
    public double Tiefe { get; init; }

    /// <summary>
    /// <c>WQ_Flaeche</c> — die Kollektorfläche. Bei der Sonde schreibt der Dialog
    /// <c>0</c> zurück.
    /// </summary>
    public double Flaeche { get; init; }

    /// <summary>
    /// <c>WQ_Anzahl</c> — die Sondenzahl. Beim Kollektor schreibt der Dialog
    /// <c>0</c> zurück.
    /// </summary>
    public int Anzahl { get; init; }

    /// <summary>
    /// <c>WQ_Bodentyp</c> — der Katalogschlüssel aus <c>ErdreichTemperatur.Katalog</c>,
    /// nicht sein Anzeigetext.
    /// </summary>
    public string Bodentyp { get; init; } = "";

    /// <summary>
    /// Die Klimazone der REGION (nicht der Anlage), 1…15; <c>0</c> = nicht zugeordnet.
    /// Der Aufrufer schreibt eine Änderung an die Klimaregion zurück.
    /// </summary>
    public int Klimazone { get; init; }

    /// <summary><c>WQ_Spreizung</c> — die nutzbare Spreizung der Quelle [K].</summary>
    public double Spreizung { get; init; }

    /// <summary>
    /// Der Außentemperaturvektor des Projekts (8 760 Stunden) oder <c>null</c>.
    /// Er ist die Grundlage BEIDER Kurven der Vorschau; ohne ihn rechnet
    /// <c>ErdreichTemperatur</c> mit seiner Normnäherung, und die Kennwertzeile sagt
    /// das an.
    /// </summary>
    public double[]? Aussentemperatur { get; init; }

    /// <summary>
    /// Die Auslegungswerte der Wärmepumpen an dieser Quelle (Heizleistung und COP am
    /// Normpunkt der Kennlinie, je Modul) für die VORPRÜFUNG ohne Simulationslauf. Die
    /// Hülle liest sie über <c>ErdreichVorpruefungCtrl</c>; <c>null</c> oder leer heißt
    /// „keine Wärmepumpe" — dann sagt der Dialog, welcher Wert fehlt.
    /// </summary>
    public IReadOnlyList<WpAuslegung>? Auslegung { get; init; }
}

/// <summary>
/// Die Auslegungswerte EINES Wärmepumpenmoduls an der Quelle: Heizleistung und COP am
/// Normpunkt seiner Kennlinie (Sole/Wasser B0/W35). Ein Wert <c>&lt;= 0</c> heißt „fehlt".
/// </summary>
/// <param name="Modul">Bezeichnung des Geräts.</param>
/// <param name="NennheizleistungKw">Heizleistung am Normpunkt [kW].</param>
/// <param name="Cop">Leistungszahl am Normpunkt.</param>
/// <param name="Normpunkt">Kurzname des Normpunkts, z. B. „B0/W35"; leer, wenn er fehlt.</param>
/// <param name="LuftWasser">Luft-Wasser-Wärmepumpe: Die Erdreichquelle wirkt in der Simulation nicht.</param>
public sealed record WpAuslegung(string Modul, double NennheizleistungKw, double Cop, string Normpunkt,
                                 bool LuftWasser = false);
