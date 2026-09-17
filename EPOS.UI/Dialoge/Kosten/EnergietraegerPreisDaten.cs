namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Ein Schnellwahlsatz aus dem Gesetzeskatalog (iU9-W4.3) — die Beschriftung
/// eines Schnellwahlknopfes samt Herkunft.
///
/// <para>Im Bestand rechnete jeder der beiden Blöcke selbst: Katalogzeile
/// lesen, Einheit umrechnen, Rückfallebene ziehen, Text bauen
/// (<c>ucStromAufschlaege.Satz</c>, <c>ucBrennstoffBestandteile.Satz</c>). Das
/// ist Datenseite und liegt jetzt in der Hülle; die Komponente bekommt das
/// Ergebnis.</para>
/// </summary>
/// <param name="Beschriftung">Was auf dem Knopf steht — der Jahressatz, bzw.
/// ein Strich, wenn der Katalog nichts hergibt.</param>
/// <param name="Herkunft">Klartext der Quelle bzw. der Grund, aus dem es
/// keinen Satz gibt — im Bestand der Kurztext des Knopfes.</param>
/// <param name="CtKwh">Der Satz in ct/kWh; <c>null</c> = nicht belegbar, dann
/// ist der Knopf gesperrt.</param>
/// <param name="Empfohlen">Hebt den Knopf hervor (Strom: der Satz, der zur
/// Unternehmensart des Projekts passt — BW4, Befund B3).</param>
public sealed record Schnellwahlsatz(string Beschriftung, string Herkunft,
                                     double? CtKwh, bool Empfohlen = false);

/// <summary>
/// Der Bearbeitungsstand der Preiszerlegung „Strompreis Details" eines
/// STROM-Trägers (Anwenderentscheide SP-E-2/SP-E-3, Fachkonzept 4.2/4.3).
///
/// <para><b>Es sind Anteile, kein Aufschlag.</b> Die Werte sagen, woraus der
/// Arbeitspreis besteht; addiert wird nichts. Ein Anteil ohne gesetzten
/// Aktiv-Schalter trägt zur Summe nichts bei — einen Modus gibt es nicht
/// mehr.</para>
///
/// <para><b>Warum veränderlich.</b> Wie die Positionszeile der Kostenverwaltung
/// schreibt die Komponente in das übergebene Objekt, und die Hülle liest es
/// beim Speichern zurück — genau die Arbeitsteilung von <c>InsModell</c> und
/// <c>Uebernehmen</c>. Die Komponente kennt das Fachmodell
/// <c>StrompreisZerlegungModel</c> dabei nicht.</para>
/// </summary>
public sealed class StrompreisDetailsStand
{
    // ---- Gruppe 1: Beschaffung und Vertrieb ----

    /// <summary>Beschaffung — der Anteil, der weder Netz noch Steuer, Abgabe
    /// oder Umlage ist.</summary>
    public double Beschaffung { get; set; }
    public bool BeschaffungAktiv { get; set; }

    public double Vertrieb { get; set; }
    public bool VertriebAktiv { get; set; }

    // ---- Gruppe 2: Netzentgelte ----

    /// <summary>Arbeitspreis Netz.</summary>
    public double Netzentgelt { get; set; }
    public bool NetzentgeltAktiv { get; set; }

    // ---- Gruppe 3: Steuern, Abgaben und Umlagen ----

    public double Stromsteuer { get; set; }
    public bool StromsteuerAktiv { get; set; }

    public double Konzession { get; set; }
    public bool KonzessionAktiv { get; set; }

    /// <summary>Umlagen als Summenwert; wirksam, solange
    /// <see cref="UmlagenEinzeln"/> aus ist.</summary>
    public double Umlagen { get; set; }
    public bool UmlagenAktiv { get; set; }

    /// <summary>Statt des Summenfelds die drei Einzelumlagen pflegen.</summary>
    public bool UmlagenEinzeln { get; set; }

    public double UmlageKwkg { get; set; }
    public bool UmlageKwkgAktiv { get; set; }

    public double UmlageOffshore { get; set; }
    public bool UmlageOffshoreAktiv { get; set; }

    public double UmlageStromNev19 { get; set; }
    public bool UmlageStromNev19Aktiv { get; set; }

    // ---- Vergütung: NICHT MEHR HIER (SP-E-5 (a), 17.09.2026) ----
    // Sie steht bei den Wirtschaftlichkeitsparametern und stellt von dort auch den
    // Verkaufspreis der Speicherwelt; die Trägerkarte kennt sie nicht mehr.
}

/// <summary>
/// Der Bearbeitungsstand der Preiszerlegung eines BRENNSTOFF-Trägers
/// (iU9-W4.3, Vorbild <c>ucBrennstoffBestandteile</c>, Konzept
/// BHKW-Wirtschaftlichkeit § 4.1).
///
/// <para><b>Ein LEERES Feld heißt „kein Anteil"</b> und ist deshalb
/// <c>null</c>, nicht 0 — anders als beim Strom-Block, wo ein Vorschlagssatz
/// einspringt (Konzept § 5.1, Falle aus E5).</para>
///
/// <para><b>Einen Modus gibt es nicht mehr.</b> Wie beim Strom sagt ein Anteil
/// mit Wert und Aktiv-Schalter alles, was die zwei Modi sagten; die Summe der
/// aktiven Anteile steht neben dem Arbeitspreis, der Unterschied in der
/// Restzeile.</para>
/// </summary>
public sealed class BrennstoffBestandteileStand
{
    public double? Energiesteuer { get; set; }
    public double? CO2 { get; set; }
    public double? Netzentgelt { get; set; }
    public double? Vertrieb { get; set; }

    public bool EnergiesteuerAktiv { get; set; }
    public bool CO2Aktiv { get; set; }
    public bool NetzentgeltAktiv { get; set; }
    public bool VertriebAktiv { get; set; }
}

/// <summary>
/// Was die Hülle zu einem Stand ausrechnet: die Summenzeile und die Restzeile
/// (iU9-W4.3).
///
/// <para>Beides fällt für BEIDE Träger aus derselben Rechnung
/// (<c>Preisanteile.SummeCtKwh</c> / <c>Preisanteile.RestCtKwh</c> über den
/// Engine-Satz des jeweiligen Controllers) — die Formeln stehen in der Engine,
/// nicht in der Oberfläche.</para>
/// </summary>
/// <param name="SummeText">Die fertige Summenzeile.</param>
/// <param name="RestText">Die fertige Restzeile.</param>
/// <param name="RestNegativ">Der nicht aufgeschlüsselte Rest ist negativ — die
/// ausgewiesenen Bestandteile sind zusammen teurer als der Preis. Das wird
/// benannt, nicht geglättet.</param>
public sealed record PreisblockAnzeige(string SummeText, string RestText, bool RestNegativ);
