using System;
using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// <b>DIE KANDIDATEN DER GRÖSSENSUCHE SIND GERÄTE</b> (Anwenderentscheid vom 15.09.2026:
/// „Größensuch Stromspeicher nur über Kapazität und Leistung im Katalog des Projektes oder
/// wahlweise aus Stammdaten").
///
/// <para><b>Was diese Datei tut und was nicht.</b> Sie löst die
/// <see cref="FlottenKandidatenquelle"/> in einen Gerätebestand auf — das ist der einzige
/// Schritt, der eine Datenbank braucht, und er gehört deshalb in den Kern. WELCHE dieser
/// Geräte gerechnet werden, entscheidet allein <c>FlottenGeraetewahl.Waehle</c> in der
/// Engine: dieselbe Regel für die Kandidatenzeile der Station „Optimierung", für die
/// Kandidatentabelle und für den Lauf.</para>
///
/// <para><b>Keine zweite Abbildung.</b> Ein Gerät des Projektkatalogs entsteht über
/// <see cref="EinheitAusProjektanlage"/>, ein Gerät der Stammdaten über
/// <c>EinheitAusKatalogsatz</c> — genau die Wege, die auch „Speicher hinzufügen" geht. Wer
/// dort etwas ändert, ändert die Suche mit; das ist der Zweck.</para>
/// </summary>
public static partial class SpeicherFlottenStudieCtrl
{
    /// <summary>
    /// Der Gerätebestand EINER Quelle, als Kandidaten der Größensuche.
    /// </summary>
    /// <remarks>
    /// <para><b>Ein Gerät ohne Kapazität oder ohne Leistung steht nicht darin.</b> Es
    /// ließe sich nicht rechnen, und ein Kandidat, der nur die Liste verlängert, ist eine
    /// Zusage ohne Deckung — dieselbe Regel wie bei
    /// <see cref="Projektanlagenkandidaten"/>.</para>
    /// <para><b>Die Reihenfolge ist die der Quelle</b> (Katalog nach Bezeichner, Projekt
    /// nach Anlagenfolge); die verbindliche Ordnung der Kandidaten stellt danach die
    /// Auswahlregel der Engine her.</para>
    /// <para><b>Die Quellkennung ist die ID des Satzes</b> als Text: die Katalog-ID
    /// beziehungsweise <c>Tab_Energieanlagen.ID</c>. Sie ist je Quelle eindeutig und
    /// damit das letzte Unterscheidungsmerkmal der Sortierung; über sie findet die
    /// Übernahme zum Satz zurück.</para>
    /// </remarks>
    /// <param name="projektId">Das offene Projekt; nur für <see cref="FlottenKandidatenquelle.Projektkatalog"/> nötig.</param>
    /// <param name="quelle">Projektkatalog oder Stammdaten.</param>
    /// <returns>Die Geräte; leer, wenn die Quelle keinen brauchbaren Satz führt.</returns>
    public static IReadOnlyList<FlottenGeraetekandidat> Geraetekandidaten(
        int projektId, FlottenKandidatenquelle quelle)
        => quelle == FlottenKandidatenquelle.Stammdaten
            ? AusStammdaten()
            : AusProjektkatalog(projektId);

    /// <summary>Die Speicher der Stammdaten (<c>Tab_Stromspeicher_STAMM</c>).</summary>
    private static List<FlottenGeraetekandidat> AusStammdaten()
    {
        var liste = new List<FlottenGeraetekandidat>();
        foreach (StromspeicherStammCtrl.KatalogZeile z in StromspeicherStammCtrl.KatalogZeilen())
        {
            StromspeicherModel m = StromspeicherStammCtrl.Katalogsatz(z.Id);
            FlottenEinheit e = EinheitAusKatalogsatz(m, out bool neutral);
            if (e == null || !(e.KapazitaetKWh > 0.0) || !(e.EntladeleistungKw > 0.0)) continue;
            liste.Add(new FlottenGeraetekandidat
            {
                Quellkennung = z.Id.ToString(CultureInfo.InvariantCulture),
                Geraet = e,
                // WAS DER SATZ FUEHRT, entscheidet die Uebernahme auf die Vorlage des
                // Anwenders (FlottenGeraeteuebernahme) — gelesen an der frisch gebauten
                // Einheit, denn nur hier ist noch zu sehen, was aus dem Satz stammt.
                Gefuehrt = FlottenGeraeteuebernahme.Gefuehrt(e),
                NeutraleKennwerte = neutral
            });
        }
        return liste;
    }

    /// <summary>Die Speicheranlagen des offenen Projekts (<c>Tab_Stromspeicher</c>).</summary>
    /// <remarks>
    /// Eine Projektanlage führt ihre Betriebsführung selbst (Variantenzeile): Wirkungsgrade
    /// und SoC-Fenster stehen an ihr, und wo sie stehen, greift keine neutrale Vorgabe.
    /// Fehlt etwas, füllt <c>FlottenGeraetevorgaben.LueckenFuellen</c> es hier — und der
    /// Kandidat wird gekennzeichnet.
    /// </remarks>
    private static List<FlottenGeraetekandidat> AusProjektkatalog(int projektId)
    {
        var liste = new List<FlottenGeraetekandidat>();
        if (projektId <= 0) return liste;

        var ctrl = new StromspeicherSimCtrl();
        foreach (int anlageId in ctrl.Speicheranlagen(projektId))
        {
            FlottenEinheit e = EinheitAusProjektanlage(projektId, anlageId, liste.Count + 1);
            if (e == null || !(e.KapazitaetKWh > 0.0) || !(e.EntladeleistungKw > 0.0)) continue;
            // ERST LESEN, DANN FUELLEN: Nach LueckenFuellen ist nicht mehr zu sehen, ob
            // ein Wert aus der Anlagenzeile oder aus der neutralen Vorgabe stammt.
            FlottenKennwertherkunft gefuehrt = FlottenGeraeteuebernahme.Gefuehrt(e);
            bool neutral = FlottenGeraetevorgaben.LueckenFuellen(e);
            liste.Add(new FlottenGeraetekandidat
            {
                Quellkennung = anlageId.ToString(CultureInfo.InvariantCulture),
                Geraet = e,
                Gefuehrt = gefuehrt,
                NeutraleKennwerte = neutral
            });
        }
        return liste;
    }

    /// <summary>
    /// <b>Schreibt den Gerätebestand in JEDE Suchachse</b> des Suchraums — der Schritt,
    /// der vor der Kandidatenzeile, vor der Kandidatentabelle und vor jedem Lauf steht.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum an der Achse und nicht als Beigabe des Laufs.</b> Die Zählregel der
    /// Engine (<c>FlottenOptimierer.Kandidatenzahl</c>) bekommt allein die Konfiguration;
    /// stünde der Bestand woanders, zählte die Seite andere Kandidaten als der Lauf
    /// rechnet. Er gehört damit zum Suchraum.</para>
    /// <para><b>Gelesen wird EINMAL je Quelle</b>, auch wenn mehrere Achsen dieselbe
    /// wählen: Der Bestand hängt an der Quelle, nicht an der Achse.</para>
    /// </remarks>
    /// <param name="projektId">Das offene Projekt.</param>
    /// <param name="auslegung">Der Suchraum; <c>null</c> wird übergangen.</param>
    public static void GeraetebestandAuffrischen(int projektId, FlottenAuslegungEingang auslegung)
    {
        if (auslegung?.Achsen is not { Count: > 0 } achsen) return;

        // Ein älterer Stand wird zuerst benannt umgesetzt — sonst stünde eine
        // C-Rate-Kopplung neben einem Gerätebestand, den sie nicht lesen kann.
        FlottenAltstand.Normalisiere(auslegung);

        var bestaende = new Dictionary<FlottenKandidatenquelle, IReadOnlyList<FlottenGeraetekandidat>>();
        foreach (FlottenAuslegungsAchse a in achsen)
        {
            if (a == null) continue;
            if (!bestaende.TryGetValue(a.Quelle, out IReadOnlyList<FlottenGeraetekandidat> bestand))
            {
                bestand = Geraetekandidaten(projektId, a.Quelle);
                bestaende[a.Quelle] = bestand;
            }
            a.Geraete = new List<FlottenGeraetekandidat>(bestand);
        }
    }
}
