namespace EPOS.UI.Seiten.Start;

/// <summary>
/// Die sprachneutralen Schlüssel der <b>21 Kacheln</b> der Startseite
/// (iU9-W16b.2) — dieselbe Drei-Schichten-Regel wie <c>Seitenschluessel</c> und
/// <c>Masken</c>: ASCII, sprachneutral, nie ein Anzeigetext.
///
/// <para><b>Wozu.</b> Der Vorläufer <c>Form_Start</c> band jede Kachel über den
/// NAMEN ihres Steuerelements an ihren Handler — acht über einen Verteiler mit
/// einem Wörterbuch aus 24 Einträgen (<c>CentralControl_Click</c> +
/// <c>_clickEvents</c>), sieben über je zwei einzeilige Weiterleitungsmethoden
/// (<c>label46_Click</c> … <c>label74_Click</c>), sechs als
/// <c>AktionsKarte.Geklickt</c>. Drei Muster für dieselbe Sache, zusammen rund
/// 90 Zeilen Klebstoff (Befund W16-B19). Hier ist eine Kachel EIN Schlüssel und
/// EIN <c>@@onclick</c>; die Zuordnung Schlüssel → Fachweg macht die Hülle in
/// EINEM <c>switch</c>.</para>
///
/// <para><b>Die Reihenfolge ist die des Bestands</b>, Reiter für Reiter von links
/// nach rechts und innerhalb eines Reiters von oben nach unten.</para>
/// </summary>
public static class Kachelschluessel
{
    // ---- Reiter 1: Projekt (fuenf; "Projekt Details" ist mit E-7/K6-a entfallen) ----

    /// <summary>„Neues Projekt" — <c>karte_ProjektNeu</c>.</summary>
    public const string ProjektNeu = "PROJEKT_NEU";

    /// <summary>„Projekt öffnen/bearbeiten" — <c>karte_ProjektOeffnen</c>.</summary>
    public const string ProjektOeffnen = "PROJEKT_OEFFNEN";

    /// <summary>„Zuletzt geöffnet" — <c>karte_ProjektZuletzt</c>.</summary>
    public const string ProjektZuletzt = "PROJEKT_ZULETZT";

    /// <summary>„Speichern unter" — <c>karte_SpeichernUnter</c>.</summary>
    public const string ProjektSpeichernUnter = "PROJEKT_SPEICHERN_UNTER";

    /// <summary>„Projekt löschen" — <c>karte_Delete</c>.</summary>
    public const string ProjektLoeschen = "PROJEKT_LOESCHEN";

    // ---- Reiter 2: Waermebedarf (vier) ----

    /// <summary>„Gebäudedaten eingeben" — <c>pBox_Gebaude</c>.</summary>
    public const string Gebaeude = "GEBAEUDE";

    /// <summary>„Daten importieren" (Wärmebedarf) — <c>pBox_WBedarfDaten</c>.</summary>
    public const string WaermebedarfDaten = "WAERMEBEDARF_DATEN";

    /// <summary>„Prozesswärme" — <c>pBox_Prozess</c>.</summary>
    public const string Prozesswaerme = "PROZESSWAERME";

    /// <summary>„Brauchwasserwärme" — <c>pBox_Brauchwasser</c>.</summary>
    public const string Brauchwasser = "BRAUCHWASSER";

    // ---- Reiter 3: Strombedarf (drei) ----

    /// <summary>„Standardlastprofil" — <c>pBox_StdLastProfil</c>.</summary>
    public const string StromStandardprofil = "STROM_STANDARDPROFIL";

    /// <summary>„Eigenes Profil" — <c>pBox_StromProfilEigenes</c>.</summary>
    public const string StromEigenesProfil = "STROM_EIGENES_PROFIL";

    /// <summary>„Messdaten importieren" — <c>pBox_StromMessdaten</c>.</summary>
    public const string StromMessdaten = "STROM_MESSDATEN";

    // ---- Reiter 4: Energieerzeuger (sieben) ----

    /// <summary>„Wärmepumpe" — <c>pBox_WP</c>.</summary>
    public const string Waermepumpe = "WAERMEPUMPE";

    /// <summary>„Heizkessel" — <c>pBox_Heizkessel</c>.</summary>
    public const string Heizkessel = "HEIZKESSEL";

    /// <summary>„Solarthermie" — <c>pBox_Solarthermie</c>.</summary>
    public const string Solarthermie = "SOLARTHERMIE";

    /// <summary>„BHKW" — <c>pBox_BHKW</c>.</summary>
    public const string Bhkw = "BHKW";

    /// <summary>„Photovoltaik" — <c>pBox_PV</c>.</summary>
    public const string Photovoltaik = "PHOTOVOLTAIK";

    /// <summary>„Stromspeicher" — <c>pBox_Stromspeicher</c>.</summary>
    public const string Stromspeicher = "STROMSPEICHER";

    /// <summary>„Pufferspeicher" — <c>pBox_Pufferspeicher</c>.</summary>
    public const string Pufferspeicher = "PUFFERSPEICHER";

    // ---- Reiter 5: Simulation (ein Knopf, eine Kachel) ----

    /// <summary>Der Knopf „Simulation Konfiguration…" — <c>btn_SimKonfig</c>.</summary>
    public const string SimulationKonfiguration = "SIMULATION_KONFIGURATION";

    /// <summary>„Simulation" (die Ergebnisansicht) — <c>pBox_DetailSim</c>.</summary>
    public const string SimulationErgebnis = "SIMULATION_ERGEBNIS";

    /// <summary>
    /// Die Zahl der Kacheln, die die Startseite führt.
    ///
    /// <para><b>Warum 21 und nicht 22.</b> Der Bestand baute auf Reiter 1 SECHS
    /// Karten; die sechste („Projekt Details") war der einzige Weg in den Altzweig
    /// <c>FormMain</c> und ist mit dem Anwenderentscheid E-7 (K6-a) entfallen. Die
    /// Kachel „Optimierung" auf Reiter 5 wird gar nicht erst gebaut: Ihr Handler war
    /// leer, und der Vorläufer blendete sie zur Laufzeit aus (H11).</para>
    /// </summary>
    public const int Anzahl = 21;
}

/// <summary>
/// Die sprachneutralen Schlüssel der <b>sechs Reiter</b> der Startseite
/// (iU9-W16b.2) — die Nachfolge von <c>tabPage1</c> … <c>tabPage6</c>.
/// </summary>
public static class Reiterschluessel
{
    /// <summary>Reiter 1 „Projekt".</summary>
    public const string Projekt = "PROJEKT";

    /// <summary>Reiter 2 „Wärmebedarf".</summary>
    public const string Waermebedarf = "WAERMEBEDARF";

    /// <summary>Reiter 3 „Strombedarf".</summary>
    public const string Strombedarf = "STROMBEDARF";

    /// <summary>Reiter 4 „Energieerzeuger".</summary>
    public const string Erzeuger = "ERZEUGER";

    /// <summary>Reiter 5 „Simulation".</summary>
    public const string Simulation = "SIMULATION";

    /// <summary>Reiter 6 „Berichte &amp; Kosten" — die Seite aus iU9-W5.</summary>
    public const string BerichteKosten = "BERICHTE_KOSTEN";

    /// <summary>Die sechs Schlüssel in der Reihenfolge des Bestands.</summary>
    public static readonly string[] Alle =
    {
        Projekt, Waermebedarf, Strombedarf, Erzeuger, Simulation, BerichteKosten
    };
}
