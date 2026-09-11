using System;
using System.Collections.Generic;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die sprachneutralen Schlüssel der DREI Schritte des Simulationsablaufs
/// (Auftrag #207, Anwenderentscheid <b>SIM‑Q1</b>).
///
/// <para><b>Zwei Blätter und ein Knopf.</b> ① und ③ sind Blätter der
/// <c>Ablaufleiste</c> und stehen deshalb hier; ② ist der RECHENKNOPF der Leiste
/// und hat keinen Blattschlüssel — ein Lauf ist kein Blatt, das man ansieht
/// (dasselbe Muster wie Schritt 4 der Stromspeicher-Auslegung).</para>
/// </summary>
public static class SimulationSchritt
{
    /// <summary>Schritt ① — die <see cref="SimulationKonfigSeite"/>.</summary>
    public const string Konfiguration = "KONFIGURATION";

    /// <summary>Schritt ③ — die <see cref="SimulationErgebnisSeite"/>.</summary>
    public const string Ergebnis = "ERGEBNIS";
}

/// <summary>
/// Die MARKE einer Simulationsansicht — ein kurzer Text der Form
/// <c>"schritt=3;blatt=STROMSPEICHER"</c> (Konzept „Simulationsablauf" 2.1).
///
/// <para><b>Warum ein Text und kein Datensatz.</b> Die Marke geht durch den
/// Rückwegstapel der <c>AppWurzel</c> und durch
/// <c>Dienste.Navigation.OeffneMaske(Masken.Simulation, marke)</c> — beide
/// tragen Zeichenketten. Mehr als Schritt und Reiterblatt stellt die Wurzel
/// ohnehin nicht wieder her (Regel aus #199: „die Wurzel stellt die Ansicht
/// wieder her, nicht den inneren Zustand einer Komponente").</para>
/// </summary>
public static class SimulationMarke
{
    /// <summary>Der Schritt ① als Marke.</summary>
    public const string SCHRITT_KONFIGURATION = "schritt=1";

    /// <summary>Der Schritt ③ als Marke.</summary>
    public const string SCHRITT_ERGEBNIS = "schritt=3";

    /// <summary>
    /// Liest Schritt und Reiterblatt aus einer Marke. Unbekanntes bleibt leer
    /// bzw. 0 — eine Marke ist ein WUNSCH, kein Befehl, und die Seite entscheidet
    /// selbst, ob sie ihn erfüllen kann (ohne Ergebnis fällt sie auf ① zurück).
    /// </summary>
    public static (int Schritt, string Blatt) Lesen(string? marke)
    {
        int schritt = 0;
        string blatt = "";
        if (string.IsNullOrWhiteSpace(marke)) return (schritt, blatt);

        foreach (string stueck in marke.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int gleich = stueck.IndexOf('=');
            if (gleich <= 0) continue;

            string name = stueck.Substring(0, gleich).Trim();
            string wert = stueck.Substring(gleich + 1).Trim();

            if (string.Equals(name, "schritt", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(wert, out int zahl)) schritt = zahl;
            }
            else if (string.Equals(name, "blatt", StringComparison.OrdinalIgnoreCase))
            {
                blatt = wert;
            }
        }

        return (schritt, blatt);
    }

    /// <summary>Schreibt eine Marke; ein leeres Blatt bleibt weg.</summary>
    public static string Schreiben(int schritt, string? blatt)
        => string.IsNullOrEmpty(blatt) ? "schritt=" + schritt
                                       : "schritt=" + schritt + ";blatt=" + blatt;
}

/// <summary>
/// Die DATENSEITE der Ansicht „Simulation" (Auftrag #207, Stufe S1).
///
/// <para><b>Sie trägt keine eigenen Daten</b>, sondern die zwei fertigen
/// Parametersätze der bestehenden Seiten und zwei Auskünfte, die die Ablaufleiste
/// braucht. Die Fachwege selbst bleiben, wo sie sind: in
/// <c>SimulationKonfigDienste</c> und <c>SimulationErgebnisDienste</c>. Unter
/// Windows legt <c>Views/Simulation/SimulationHuelle.cs</c> das Bündel an und hält
/// die zwei Hülleninstanzen zwischen zwei Besuchen — der gerechnete Lauf, die
/// Bilder und die Gültigkeitsmarke überleben damit einen Ansichtswechsel.</para>
///
/// <para><b>Kein Delegat ist kein Knopf</b> (Hausregel der Auslegungsansicht):
/// Fehlt ein Eintrag, blendet die Ansicht die zugehörige Bedienung aus, statt sie
/// gesperrt oder wirkungslos zu zeigen.</para>
/// </summary>
public sealed class SimulationAnsichtDienste
{
    /// <summary>
    /// Der Parametersatz von Schritt ① (<see cref="SimulationKonfigSeite"/>);
    /// <c>null</c> = die Plattform bietet ihn nicht an.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Konfiguration;

    /// <summary>
    /// Der Parametersatz von Schritt ③ (<see cref="SimulationErgebnisSeite"/>);
    /// <c>null</c> = die Plattform bietet ihn nicht an.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Ergebnis;

    /// <summary>
    /// Der Grund, aus dem der Lauf gar nicht erst starten kann — die ROTE
    /// Vorprüfung. Leer = frei. Unter Windows ist das die Sperre aus ADR‑001
    /// (nicht abgeschlossene Schema-Migration); der Text steht am gesperrten
    /// Rechenknopf als <c>title</c>.
    /// </summary>
    public Func<string>? Sperrgrund;

    /// <summary>
    /// Liegt ein gerechneter Lauf vor? Ohne ihn ist Schritt ③ gesperrt.
    /// </summary>
    /// <remarks>
    /// <b>Nicht dasselbe wie „das Ergebnis ist gültig".</b> Wer aus der
    /// Stromspeicher-Auslegung zurückkommt, hat einen gerechneten Lauf UND ein
    /// veraltetes Ergebnis (der Nachzug setzt <c>_ergebnisGueltig = false</c>) —
    /// er soll ③ betreten dürfen und dort das Banner „Flotte geändert" lesen.
    /// Deshalb fragt diese Auskunft nach dem LAUF und nicht nach der Gültigkeit.
    /// </remarks>
    public Func<bool>? ErgebnisVorhanden;
}
