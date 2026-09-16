namespace EPOS.UI.Seiten.Assistent;

/// <summary>
/// Der EINSTIEG in einen Assistentenlauf (Anwenderentscheid vom 16.09.2026):
/// Wer den Assistenten aufmacht, sagt damit auch, WOMIT er aufmacht.
///
/// <para><b>Warum es zwei Werte gibt.</b> Der Assistent führt dreizehn Schritte,
/// und der erste ist die Komponentenauswahl („Projekt-Erstellungskonfiguration"):
/// Sie schaltet die zwölf Fachschritte frei. Für ein Projekt, das es noch gar
/// nicht gibt, ist diese Wahl gegenstandslos — der Anwender sieht dreizehn
/// Kacheln, bevor er einen Projektnamen vergeben hat, und die Komponenten kommen
/// hinterher ohnehin über die Reiter der Startseite. Jede NEUANLAGE steigt deshalb
/// bei der PROJEKTKONFIGURATION ein — gleich, von welchem Weg sie kommt.</para>
///
/// <para><b>Es ist ein Modus des LAUFS, keine zweite Seite.</b> Der Schritt
/// „Komponenten" bleibt stehen und ist über jeden anderen Einstieg unverändert
/// erreichbar (vor allem über die Betriebsart BEARBEITEN); <see cref="Neuanlage"/>
/// überspringt ihn nur.</para>
/// </summary>
public enum AssistentEinstieg
{
    /// <summary>
    /// Der Lauf über ALLE freigeschalteten Schritte, beginnend bei der
    /// Komponentenauswahl — das Verhalten seit jeher und der Rückfall jeder Hülle,
    /// die nichts anderes sagt.
    /// </summary>
    Vollstaendig,

    /// <summary>
    /// Nur die PROJEKTANLAGE: Der Lauf beginnt auf der Projektkonfiguration, der
    /// Komponentenschritt ist nicht freigeschaltet, und der Hauptknopf heißt
    /// „Weiter ▶" statt „Speichern" — er legt das Projekt an und meldet dem Wirt
    /// das Ziel, zu dem es weitergeht (der Reiter „Wärmebedarf" der Startseite).
    /// </summary>
    Neuanlage
}
