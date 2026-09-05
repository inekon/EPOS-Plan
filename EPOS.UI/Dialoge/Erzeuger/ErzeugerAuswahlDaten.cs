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

    /// <summary>Untere Grenzleistung — nur beim BHKW belegt.</summary>
    public double? Grenzleistung { get; set; }

    /// <summary>Modulneigung [°] — nur bei der Photovoltaik belegt.</summary>
    public int? Neigung { get; set; }

    /// <summary>Azimut [°] — nur bei der Photovoltaik belegt.</summary>
    public int? Azimut { get; set; }

    /// <summary>Anzahl Module — nur bei der Photovoltaik belegt (im Modell ein <c>double</c>).</summary>
    public double? AnzahlModule { get; set; }

    // --- Photovoltaik, Paket A/B des PV-Ertragsmodells (Merge 5, aus Form_PV nachgezogen) ----
    /// <summary>Wechselrichter-Wirkungsgrad als Faktor; NULL = 0,95 (Bestand). Nur Modell EINFACH.</summary>
    public double? WrWirkungsgrad { get; set; }
    /// <summary>Systemverluste in Prozent; NULL = 0.</summary>
    public double? Systemverluste { get; set; }
    /// <summary>Rechenmodell ERWEITERT gewaehlt (sonst EINFACH, der Rechenweg des Bestands).</summary>
    public bool ModellErweitert { get; set; }
    /// <summary>Wechselrichter (nur ERWEITERT): AC-Nennleistung in kW; NULL = ohne Clipping.</summary>
    public double? WrNennleistungKw { get; set; }
    /// <summary>Teillast-Kennlinie des Wechselrichters bei 10, 50 und 100 % Last; NULL = nicht bekannt.</summary>
    public double? WrEta10 { get; set; }
    public double? WrEta50 { get; set; }
    public double? WrEta100 { get; set; }
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
/// <param name="Parameter">
/// <b>Alle übrigen Eigenschaften des Katalogsatzes</b>, hinter einem Aufklapper
/// (Anwenderwunsch <b>W6‑E‑1</b>, Windows-Abnahme 05.09.2026). <c>null</c> oder leer
/// heißt: Es gibt keinen Aufklapper — so ist es heute bei allen Erzeugerarten außer
/// der Photovoltaik. Die Werte kommen wie die von <paramref name="Felder"/> FERTIG
/// FORMATIERT herein; ein nicht gepflegter Wert steht als „–" darin und nicht als 0.
/// </param>
public sealed record ErzeugerDetail(
    string Bezeichner,
    string Beschreibung,
    IReadOnlyList<(string Feld, string Wert)> Felder,
    (string Feld, bool Wert)? Schalter = null,
    IReadOnlyList<Modulparameter>? Parameter = null)
{
    /// <summary>Die Parameterzeilen ohne <c>null</c> — die Anzeige fragt nur nach der Anzahl.</summary>
    public IReadOnlyList<Modulparameter> Parameterzeilen
        => Parameter ?? Array.Empty<Modulparameter>();

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

/// <summary>
/// Eine Zeile des Parameterblocks eines Katalogsatzes (Anwenderwunsch <b>W6‑E‑1</b>,
/// Windows-Abnahme 05.09.2026: „optional sollten beim ausgewählten PV-Modul alle
/// Eigenschaften/Parameter angezeigt werden").
///
/// <para>Sie trägt dasselbe wie ein Paar aus <see cref="ErzeugerDetail.Felder"/>, dazu
/// die EINHEIT als eigenes Stück — dann steht sie im <c>Formularraster</c> unmittelbar
/// hinter dem kurzen Wertfeld statt am rechten Rand des Blocks (iU8‑E‑2). Woher
/// Beschriftung und Einheit kommen, entscheidet die Hülle; bei der Photovoltaik ist es
/// <c>PhotovoltaikStammCtrl.Parameterzeilen</c> und damit dieselbe Quelle wie im
/// Katalogdialog.</para>
/// </summary>
/// <param name="Feld">Beschriftung, bereits übersetzt und mit „:".</param>
/// <param name="Wert">Der fertig formatierte Wert; „–" heißt „nicht gepflegt".</param>
/// <param name="Einheit">Einheit hinter dem Feld; leer = keine.</param>
public sealed record Modulparameter(string Feld, string Wert, string Einheit = "");

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
