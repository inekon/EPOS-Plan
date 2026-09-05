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
/// Der Bearbeitungsstand des Aufschlagsblocks eines STROM-Trägers
/// (iU9-W4.3, Vorbild <c>ucStromAufschlaege</c>, Fachkonzept 4.2/4.3).
///
/// <para><b>Warum veränderlich.</b> Wie die Positionszeile der Kostenverwaltung
/// schreibt die Komponente in das übergebene Objekt, und die Hülle liest es
/// beim Speichern zurück — genau die Arbeitsteilung von <c>InsModell</c> und
/// <c>Uebernehmen</c>. Die Komponente kennt das Fachmodell
/// <c>StromAufschlagModel</c> dabei nicht.</para>
/// </summary>
public sealed class StromAufschlaegeStand
{
    /// <summary><c>true</c> = aufgeschlüsselt, <c>false</c> = Gesamtwert (Override).</summary>
    public bool Aufgeschluesselt { get; set; } = true;

    public double Netzentgelt { get; set; }
    public double Umlagen { get; set; }
    public double Stromsteuer { get; set; }
    public double Konzession { get; set; }
    public double Vertrieb { get; set; }

    public bool NetzentgeltAktiv { get; set; }
    public bool UmlagenAktiv { get; set; }
    public bool StromsteuerAktiv { get; set; }
    public bool KonzessionAktiv { get; set; }
    public bool VertriebAktiv { get; set; }

    /// <summary>Der Gesamtaufschlag im Override-Modus.</summary>
    public double Override { get; set; }

    /// <summary>Vergütung für eingespeisten PV-Strom [ct/kWh] (Fachkonzept 4.3).</summary>
    public double VerguetungPv { get; set; }

    /// <summary>Vergütung für eingespeisten BHKW-Strom [ct/kWh].</summary>
    public double VerguetungBhkw { get; set; }
}

/// <summary>
/// Der Bearbeitungsstand der Preiszerlegung eines BRENNSTOFF-Trägers
/// (iU9-W4.3, Vorbild <c>ucBrennstoffBestandteile</c>, Konzept
/// BHKW-Wirtschaftlichkeit § 4.1).
///
/// <para><b>Ein LEERES Feld heißt „kein Anteil"</b> und ist deshalb
/// <c>null</c>, nicht 0 — anders als beim Strom-Block, wo ein Vorschlagssatz
/// einspringt (Konzept § 5.1, Falle aus E5).</para>
/// </summary>
public sealed class BrennstoffBestandteileStand
{
    /// <summary><c>true</c> = aufgeschlüsselt (Summe ist der Preis),
    /// <c>false</c> = Gesamtwert (Arbeitspreis gilt).</summary>
    public bool Aufgeschluesselt { get; set; }

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
/// <para>Beides fällt aus derselben Rechnung
/// (<c>StromAufschlagCtrl.AlsAufschlagssatz</c> bzw.
/// <c>BrennstoffBestandteilCtrl</c>) — die Formeln stehen in der Engine, nicht
/// in der Oberfläche.</para>
/// </summary>
/// <param name="SummeText">Die fertige Summenzeile.</param>
/// <param name="RestText">Die fertige Restzeile bzw. der Modushinweis.</param>
/// <param name="RestNegativ">Der nicht aufgeschlüsselte Rest ist negativ — die
/// ausgewiesenen Bestandteile sind zusammen teurer als der Preis. Das wird
/// benannt, nicht geglättet.</param>
public sealed record PreisblockAnzeige(string SummeText, string RestText, bool RestNegativ);
