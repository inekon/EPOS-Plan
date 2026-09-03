namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// EIN Projektpuffer, wie ihn die Auswahlliste des Quellendialogs zeigt.
/// Der Anzeigetext ist bereits fertig (<c>SpeicherItem.ToString</c>:64-78) — mit
/// Temperaturpaar die lange Form, ohne die kurze: „0/0 °C" wäre eine Angabe, die es
/// nicht gibt.
/// </summary>
/// <param name="Id">Der Fremdschlüssel <c>WQ_ID_Puffer</c>.</param>
/// <param name="Bezeichner">Der Name — er geht als Altwert mit hinaus.</param>
/// <param name="Anzeige">Die Listenzeile.</param>
/// <param name="Daten">Der Detailblock unter der Liste (<c>ZeigeSpeicherDaten</c>:850-864).</param>
/// <param name="Gesamtvolumen">Liter — Eingangsgröße der Kapazitätsrechnung.</param>
public sealed record QuellPufferzeile(int Id, string Bezeichner, string Anzeige,
                                      string Daten, int Gesamtvolumen);

/// <summary>
/// Die dreizehn Übergabefelder des Quellendialogs „Pufferspeicher"
/// (Vermessung §4 b) — hinein und, nach „OK", wieder hinaus.
///
/// <para><b>Zwei Erzeugerarten in einer Maske.</b> <see cref="IstKessel"/>
/// entscheidet: Die Wärmepumpe zieht Verdampferwärme aus dem Speicher und pflegt dazu
/// vier Parameter; der Heizkessel nimmt seit Etappe D5b die Eintrittstemperatur aus
/// dem Puffer statt aus dem Systemrücklauf (Kaskade) und pflegt dafür den
/// Temperaturbezug. Was die jeweils andere Art angeht, bleibt UNANGETASTET
/// (Befund W10‑B15) — sonst überschriebe eine Kesselbearbeitung die WP-Vorgaben mit
/// 10 °C/5 K.</para>
/// </summary>
public sealed record QuellePufferspeicherDaten
{
    /// <summary>Name der Anlage — er steht im Titel.</summary>
    public string WPName { get; init; } = "";

    /// <summary>Das Projekt, dessen Puffer zur Wahl stehen.</summary>
    public int IdProjekt { get; init; }

    /// <summary>
    /// <c>true</c> = Heizkessel (Kaskade), <c>false</c> = Wärmepumpe. Der Dialog
    /// zeigt danach ganz verschiedene Blöcke.
    /// </summary>
    public bool IstKessel { get; init; }

    /// <summary>Der gewählte Puffer (<c>WQ_ID_Puffer</c>); <c>0</c> = keiner.</summary>
    public int IdPuffer { get; init; }

    /// <summary>
    /// Der Bezeichner des Puffers (<c>WQ_Puffer</c>) — Altkompatibilität. Er ist die
    /// Rückfallkette der Vorauswahl, wenn der Fremdschlüssel fehlt.
    /// </summary>
    public string Pufferspeicher { get; init; } = "";

    /// <summary>Nur WP: Quelltemperatur [°C].</summary>
    public double Quelltemperatur { get; init; } = 10;

    /// <summary>Nur WP: nutzbare Spreizung [K].</summary>
    public double Spreizung { get; init; } = 5;

    /// <summary>Nur WP: Regenerationsleistung [kW].</summary>
    public double Regeneration { get; init; }

    /// <summary>Nur WP: Quelle unbegrenzt verfügbar.</summary>
    public bool Unbegrenzt { get; init; }

    /// <summary>
    /// Quell-Entnahmehöhe 0…1; <c>null</c> = oben. LEER ist gültig — die Spalte bleibt
    /// dann NULL (Paket Q1, Konzept 8.4). Sie gilt für BEIDE Erzeugerarten.
    /// </summary>
    public double? Anschlusshoehe { get; init; }

    /// <summary>Nur Kessel: <c>WQ_TemperaturModus</c> — „berechnet" oder „fest".</summary>
    public string TemperaturModus { get; init; } = "";

    /// <summary>Nur Kessel: fest vorgegebener Vorlauf [°C].</summary>
    public int VorlaufAnlage { get; init; }

    /// <summary>Nur Kessel: fest vorgegebener Rücklauf [°C].</summary>
    public int RuecklaufAnlage { get; init; }
}
