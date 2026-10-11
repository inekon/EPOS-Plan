using System.Globalization;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Eine Zeile der linken Liste „ausgewählt im Projekt" (iU9-W6.3 … W6.7).
///
/// <para><b>Warum ein eigener Typ.</b> Im Bestand steht dort ein
/// <c>WErzeugerModel</c> — eine Fachklasse des Kerns mit über dreißig Feldern, von
/// denen die Maske sechs anfasst. Eine Razor-Komponente kennt die Fachklassen des Kerns
/// nicht (<c>EPOS.UI/CLAUDE.md</c>); sie bekommt diese Zeile, und die Hülle hält die
/// Zuordnung zum Modell über <see cref="Schluessel"/>.</para>
///
/// <para><b>Mehrere Zeilen dürfen dasselbe Gerät führen.</b> Das ist keine Nachlässigkeit,
/// sondern Fachlage: Zwei gleiche Kessel im Projekt teilen sich EINE Kopie in
/// <c>Tab_Heizkessel</c> — <see cref="GeraetId"/> ist dann bei beiden dieselbe, während
/// <see cref="Schluessel"/> die Zeilen unterscheidet. Genau daran hängt die Regel, dass
/// „▶" die Projektkopie nur entfernt, wenn keine zweite Zeile mehr darauf verweist.</para>
/// </summary>
public sealed class ErzeugerZeile
{
    /// <summary>
    /// Die Zeile selbst — im Bestand <c>WErzeugerModel.ID</c> (ein Zähler ab 100000,
    /// keine Datenbank-Id). Über ihn findet die Hülle das Modell wieder.
    /// </summary>
    public int Schluessel { get; set; }

    /// <summary>Anzeigename der Zeile.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>
    /// Das GERÄT, auf das die Zeile verweist — <c>ID_Kessel</c>, <c>ID_BHKW</c>,
    /// <c>ID_PV</c>, <c>ID_SP</c> bzw. <c>ID_PUFFER</c>. Mehrere Zeilen dürfen denselben
    /// Wert tragen (siehe Klassenkommentar).
    /// </summary>
    public int GeraetId { get; set; }

    /// <summary>Zugeordnete Energieträgervariante (<c>ID_Carrier</c>); 0 = keine.</summary>
    public int CarrierId { get; set; }

    /// <summary>Vorlauftemperatur [°C]; <c>null</c> = leeres Feld.</summary>
    public int? Vorlauf { get; set; }

    /// <summary>Rücklauftemperatur [°C]; <c>null</c> = leeres Feld.</summary>
    public int? Ruecklauf { get; set; }

    /// <summary>
    /// Die HERKUNFT eines vorbelegten Paars (Anwenderauftrag 30.09.2026) — fertig formuliert
    /// von der Hülle (Kern: <c>AnlagenTemperaturen.Herleitung</c>), etwa „Vorgabe 70/50 °C —
    /// so rechnet die Simulation ohne Eintrag.". Leer = das Paar ist das der Anlage, nichts
    /// wurde vorbelegt; eine Eingabe in Vorlauf oder Rücklauf leert es. Nur beim Heizkessel
    /// belegt.
    /// </summary>
    public string TemperaturHerleitung { get; set; } = "";

    /// <summary>Untere Grenzleistung — nur beim BHKW belegt.</summary>
    public double? Grenzleistung { get; set; }

    /// <summary>
    /// Die Zeile „Senken: …" der Anlage (Anwenderentscheid 23.09.2026) — FERTIG
    /// FORMULIERT von der Hülle (<c>Senkenvorbelegung.Anzeigezeile</c> im Kern): die
    /// Senkenzeilen einer gespeicherten Anlage, die Vorbelegung einer Anlage ohne Zeile,
    /// oder was beim Speichern aus dem Bedarf des Projekts entsteht. Leer = keine Zeile
    /// (kein Wärmeerzeuger, kein Projekt) — dann zeigt der Dialog nichts.
    /// </summary>
    public string Senken { get; set; } = "";

    /// <summary>Modulneigung [°] — nur bei der Photovoltaik belegt.</summary>
    public int? Neigung { get; set; }

    /// <summary>Azimut [°] — nur bei der Photovoltaik belegt.</summary>
    public int? Azimut { get; set; }

    /// <summary>Anzahl Module — nur bei der Photovoltaik belegt (im Modell ein <c>double</c>).</summary>
    public double? AnzahlModule { get; set; }

    // --- Kollektorfeld der Solarthermie (Welle M2) — nur beim Solarkollektor belegt ----------
    /// <summary>Leistung der Solarkreispumpe [W]; <c>null</c> = nicht gepflegt.</summary>
    public double? SolarPumpenleistungW { get; set; }
    /// <summary>Verluste des Solarkreises [%]; <c>null</c> = Vorgabe 8 %.</summary>
    public double? SolarkreisverlusteProzent { get; set; }
    /// <summary>Grädigkeit des Wärmeübertragers [K]; <c>null</c> = Vorgabe 5 K.</summary>
    public double? SolarGraedigkeitK { get; set; }
    /// <summary>Spreizung des Kollektorkreises [K]; <c>null</c> = Vorgabe 10 K.</summary>
    public double? SolarSpreizungK { get; set; }
    /// <summary>Arbeitstemperatur aus dem Speicher statt fest 50 °C.</summary>
    public bool SolarArbeitstemperaturAusSpeicher { get; set; }

    // --- Photovoltaik, Paket A/B des PV-Ertragsmodells (Merge 5, aus Form_PV nachgezogen) ----
    /// <summary>Wechselrichter-Wirkungsgrad als Faktor; NULL = 0,95 (Bestand). Nur Modell EINFACH.</summary>
    public double? WrWirkungsgrad { get; set; }
    /// <summary>Systemverluste in Prozent; NULL = 0.</summary>
    public double? Systemverluste { get; set; }
    /// <summary>
    /// Bodenalbedo vor der Anlage (0…1); NULL = 0,2. Bei Photovoltaik und Solarthermie belegt
    /// (<c>Tab_Energieanlagen.Albedo</c>).
    /// </summary>
    public double? Albedo { get; set; }
    /// <summary>Rechenmodell ERWEITERT gewaehlt (sonst EINFACH, der Rechenweg des Bestands).</summary>
    public bool ModellErweitert { get; set; }
    /// <summary>Wechselrichter (nur ERWEITERT): AC-Nennleistung in kW; NULL = ohne Clipping.</summary>
    public double? WrNennleistungKw { get; set; }
    /// <summary>Teillast-Kennlinie des Wechselrichters bei 10, 50 und 100 % Last; NULL = nicht bekannt.</summary>
    public double? WrEta10 { get; set; }
    public double? WrEta50 { get; set; }
    public double? WrEta100 { get; set; }

    // --- Wechselrichter, Stufe S2 (Anwenderentscheide W6-E-2 und W6-E-3) --------------
    /// <summary>
    /// Der SICHTBARE Wechselrichterweg dieser Anlage (<b>W6‑E‑3</b>):
    /// <c>false</c> = vereinfacht, Pauschalen ohne Wechselrichter (der Weg von heute);
    /// <c>true</c> = mit Wechselrichter, also Katalog, Stränge, Kennlinie und Clipping.
    ///
    /// <para>In der Datenbank ist es die Spalte
    /// <c>Tab_Energieanlagen.PV_Wechselrichterweg</c>, wo NULL und
    /// <c>PV_WR_WEG_VEREINFACHT</c> beide „vereinfacht" heissen (Konzept 7.1). Der
    /// Unterschied „nie gewählt" gegen „ausdrücklich vereinfacht" gehört der Ablage,
    /// nicht der Maske — hier ist die Frage ein Ja/Nein.</para>
    /// </summary>
    public bool MitWechselrichter { get; set; }

    /// <summary>
    /// Die Stränge dieser Anlage (<c>Z_AnlageStrang</c>) in Rangfolge — sie gehören
    /// der Hülle und werden IN PLACE bearbeitet, wie die Zeile selbst.
    ///
    /// <para>Eine LEERE Liste ist der Normalfall des Bestands: Ohne Strangzeile
    /// rechnet die Anlage wie bisher.</para>
    /// </summary>
    public List<StrangZeile> Straenge { get; set; } = new();

    /// <summary>
    /// Ist das GERÄT, auf das diese Zeile verweist, ein Auslieferungssatz
    /// (<c>ReadOnly</c> in <c>Tab_PV_STAMM</c>)? Auftrag #211, Fachkonzept 4.5, Muster
    /// <c>WaermepumpeStammDaten.NurLesen</c> — der Katalogsatz selbst bleibt dabei
    /// unangetastet, nur der Assistent lehnt eine Feldsetzung an dieser Zeile ab, statt
    /// erst beim Speichern zu scheitern.
    /// </summary>
    public bool NurLesen { get; set; }
}

/// <summary>
/// Eine Zeile der rechten Liste „aus Datenbank" (Katalog).
/// </summary>
/// <param name="Id">Primärschlüssel des Katalogsatzes — er ist der Steuerwert, nicht der Name.</param>
/// <param name="Bezeichner">Anzeigename.</param>
/// <param name="Eigenschaften">
/// Zweite Spalte, mehrzeilig. Der BHKW-Dialog zeigte dort Firma, Brennstoff, Ptherm und
/// Pel untereinander (<c>DataGridView</c>-Spalte „Eigenschaften"); wo es keine zweite
/// Spalte gibt, bleibt sie leer.
/// </param>
public sealed record KatalogZeile(int Id, string Bezeichner, string Eigenschaften = "");

/// <summary>
/// Der Detailblock unter den beiden Listen — er zeigt entweder die gewählte Projektzeile
/// oder den gewählten Katalogsatz. Die Werte kommen FERTIG FORMATIERT herein; die
/// Komponente rechnet nicht.
/// </summary>
/// <param name="Bezeichner">Name.</param>
/// <param name="Beschreibung">Freitext.</param>
/// <param name="Felder">
/// Die übrigen Anzeigefelder als Paare (Beschriftung, Wert) in Anzeigereihenfolge — je
/// Erzeugerart andere. So braucht es nicht fünf fast gleiche Detailtypen.
/// </param>
/// <param name="Schalter">
/// Ein Ja/Nein-Merkmal mit Beschriftung, <c>null</c> = keines. Beim Heizkessel ist das
/// „Brennwertkessel".
/// </param>
/// <param name="Kennwerte">
/// Die Zahlwerte des Satzes für die Zusammenfassung der Detailzeile (UeS2b) — unabhängig
/// von den Beschriftungen der <paramref name="Felder"/>; <c>null</c> = keine.
/// </param>
public sealed record ErzeugerDetail(
    string Bezeichner,
    string Beschreibung,
    IReadOnlyList<(string Feld, string Wert)> Felder,
    (string Feld, bool Wert)? Schalter = null,
    ErzeugerKennwerte? Kennwerte = null)
{
    /// <summary>
    /// Ist der Anzeigewert eine ZAHL? Dann bekommt sein Feld im
    /// <c>Formularraster</c> die kurze Breite — „290" hinter
    /// „thermische Leistung [kWth]:" braucht keine halbe Blockbreite, und die
    /// Einheit steht ohnehin schon in der Beschriftung.
    ///
    /// <para>Anwenderfoto „Verwaltung BHKW" vom 05.09.2026 (iU8‑E‑2, Paket P1):
    /// „Stelle diesen Dialog kompakter dar, insbesondere Daten zum
    /// BHKW-Modul unten." Die Entscheidung steht HIER und nicht in sechs
    /// Dialogen, weil alle sechs Erzeuger-Projektmasken denselben
    /// Detailblock zeichnen. Sie hängt am WERT, nicht an der Beschriftung:
    /// Die Feldnamen kommen je Erzeugerart anders herein, eine Zahl bleibt
    /// eine Zahl.</para>
    ///
    /// <para>Beide Kulturen werden gefragt — der Wert wird von der Hülle in
    /// der laufenden Kultur formatiert („0,9" auf Deutsch), Ganzzahlen sind
    /// in beiden gleich. Rät die Probe falsch, ändert sich nur die BREITE
    /// eines Anzeigefeldes.</para>
    /// </summary>
    public static bool IstZahl(string wert)
        => !string.IsNullOrWhiteSpace(wert)
           && (double.TryParse(wert, NumberStyles.Any, CultureInfo.CurrentCulture, out _)
               || double.TryParse(wert, NumberStyles.Any, CultureInfo.InvariantCulture, out _));
}

// HIER STAND DER SATZ „Modulparameter" samt ErzeugerDetail.Parameter und
// ErzeugerDetail.Parameterzeilen — der Aufklapper „Alle Modulparameter anzeigen" des
// Photovoltaik-Projektdialogs (W6‑E‑1). Seit dem 15.09.2026 füllt ihn der Baustein
// „Katalogfelder" aus dem Katalogbrowser-Profil, wortgleich zu den übrigen fünf
// Erzeugerdialogen; die Zeilen dieses Aufklappers sind seither Parameterwert
// (Baustein Parameteruebersicht) und kommen aus
// PhotovoltaikStammCtrl.Parameterzeilen. Der Detailblock trug sie nur noch,
// ohne dass eine Hülle sie belegte.

/// <summary>
/// <b>Die Zahlwerte eines Satzes</b> für die Zusammenfassung der Detailzeile (UeS2b): Die
/// Hülle setzt sie aus dem Modell, der Dialog formatiert sie samt Einheit. So liest die
/// Zusammenfassung nicht an den Beschriftungen der Anzeigefelder (Ressourcen) — jeder Wirt,
/// auch der Probenwirt, zeigt die Angaben, sobald er die Zahl reicht. Leer = keine Angabe.
/// </summary>
public sealed record ErzeugerKennwerte
{
    /// <summary>Thermische Leistung in kW (Heizkessel, BHKW).</summary>
    public double? PthermKw { get; init; }
    /// <summary>Elektrische Leistung in kW (BHKW).</summary>
    public double? PelKw { get; init; }
    /// <summary>Lade- und Entladeleistung in kW (Stromspeicher).</summary>
    public double? LeistungKw { get; init; }
    /// <summary>Speichervolumen in l (Pufferspeicher).</summary>
    public double? VolumenLiter { get; init; }
    /// <summary>Nutzbare Kapazität in kWh (Stromspeicher).</summary>
    public double? KapazitaetKwh { get; init; }
    /// <summary>Nennleistung eines Moduls in W (Photovoltaik).</summary>
    public double? ModulleistungW { get; init; }
}

/// <summary>
/// Was der Kern beisteuert, bevor eine Zeile aufgenommen werden kann — die Werte, die
/// <c>btn_Kessel_Hinzu_Click</c> aus dem Stammsatz las, plus die Auswahlliste des
/// Energieträger-Unterdialogs.
/// </summary>
/// <param name="Energietraeger">Die wählbaren Träger, bereits auf die Kategorie eingeengt.</param>
/// <param name="VorwahlId">Vorgewählter Träger; <c>null</c> = keine Vorwahl.</param>
/// <param name="Meldung">
/// Nicht leer heißt: Es geht nicht weiter (etwa „in den Stammdaten nicht gefunden").
/// Dann erscheint kein Unterdialog.
/// </param>
public sealed record TraegerVorbereitung(
    IReadOnlyList<(int Id, string Name)> Energietraeger,
    int? VorwahlId,
    string Meldung = "");

/// <summary>
/// Was beim Aufnehmen herausgekommen ist.
/// </summary>
/// <param name="Zeile">Die neue Zeile; <c>null</c> = nichts aufgenommen.</param>
/// <param name="Meldung">Der Text, den der Vorläufer als <c>MessageBox</c> zeigte; leer = keiner.</param>
/// <param name="Fehler">
/// <c>true</c> zeigt die Meldung als Warnung, <c>false</c> als Hinweis. Der Bestand
/// unterschied das nicht — er zeigte alle vier Ausgänge als schlichte Meldung.
/// </param>
public sealed record AufnahmeErgebnis(ErzeugerZeile? Zeile, string Meldung = "", bool Fehler = false);

/// <summary>
/// <b>Die projektbezogenen Werte eines Pufferspeichers</b> für die Zusammenfassung der
/// Detailzeile (UeS2b): Temperaturpaar, Schwellen und Verwendung stehen an der Projektkopie,
/// gepflegt im Projektspeicher-Dialog — die Verwaltung zeigt sie nur kurz an.
/// </summary>
/// <param name="Vorlauf">Vorlauf in °C; <c>null</c> oder 0 = nicht gepflegt.</param>
/// <param name="Ruecklauf">Rücklauf in °C; <c>null</c> oder 0 = nicht gepflegt.</param>
/// <param name="SchwelleEin">Einschaltschwelle in %.</param>
/// <param name="SchwelleAus">Abschaltschwelle in %.</param>
/// <param name="Verwendung">Die wirksame Verwendung als Anzeigetext; leer = keine.</param>
public sealed record Pufferangaben(int? Vorlauf, int? Ruecklauf, double? SchwelleEin, double? SchwelleAus, string Verwendung);
