namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Der KONTEXT der Kostenverwaltung (iU9-W4.2): Komponente bzw. Anlage,
/// Kategorie und — im Stammkontext — die Variante.
///
/// <para>Im Bestand war das keine Klasse, sondern der Zustand dreier
/// Steuerelemente (<c>cmbKomponente</c>, <c>rbInvest</c>/<c>rbBetrieb</c>,
/// <c>cmbVariante</c>), den <c>Kontext_Geaendert</c> abfragte. Hier ist es ein
/// Wert, den die Komponente an die Hülle reicht — sie fragt damit dieselbe
/// Frage, ohne die Hülle ihre Felder lesen zu lassen.</para>
/// </summary>
/// <param name="EintragId">Id des gewählten Klapplisteneintrags — im
/// Stammkontext die Komponente, im Projektmodus die Anlagenzeile (Ä20).</param>
/// <param name="Invest">Investitionskosten (sonst Betriebskosten).</param>
/// <param name="VarianteId">Die gewählte Vorlage/Variante; <c>null</c> = die
/// erste bzw. keine (im Projektmodus immer <c>null</c>).</param>
public sealed record KostenKomponenteKontext(int? EintragId, bool Invest, int? VarianteId);

/// <summary>
/// EINE Zeile des Positionsrasters, wie die Komponente sie sieht (iU9-W4.2).
///
/// <para><b>Warum veränderlich.</b> Die WinForms-Zeile schrieb ihre Felder in
/// die übergebene <c>KostenVorlagenPosition</c> und ließ sie dort liegen, bis
/// „Speichern" oder „OK" sie in die Datenbank trug (Ä12/Ä19). Genau diese
/// Arbeitsteilung bleibt: Die Komponente ändert die Zeile, die Hülle schreibt
/// sie. Ein unveränderlicher Record wäre hier die falsche Form — der Aufrufer
/// ERWARTET die Änderung am Objekt (Risiko R3 des Wellenplans nennt den
/// umgekehrten Fall).</para>
/// </summary>
public sealed class KostenPositionZeile
{
    /// <summary>Id der Position — Schlüssel für Löschen, Editor und Worst/Best.</summary>
    public int Id { get; set; }

    /// <summary>Bezeichnung (<c>txtBezeichnung</c>).</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Id der Bemessung in <see cref="KostenKomponenteStand.Bemessungen"/>.</summary>
    public int? BemessungId { get; set; }

    /// <summary>Der Satz (<c>txtSatz</c>).</summary>
    public double? Satz { get; set; }

    /// <summary>Nutzungsdauer in Jahren (<c>txtNutzung</c>).</summary>
    public double? Nutzungsdauer { get; set; }

    /// <summary>Einheit hinter dem Satzfeld (<c>lblEinheit</c>) — folgt der Bemessung.</summary>
    public string Einheit { get; set; } = "";

    /// <summary>Der Betrag als fertiger Text (<c>txtBetrag</c>), nie eingebbar.</summary>
    public string BetragText { get; set; } = "";

    /// <summary>Satz und Betrag sind EIN Wert (Kopplungsregel KL4 / § 5.4).</summary>
    public bool Kette { get; set; }

    /// <summary>Kurztext des Betragsfeldes — je Kontext ein anderer.</summary>
    public string BetragKurztext { get; set; } = "";

    /// <summary>Empfehlungsbereich als Kurztext des Satzfeldes; leer = keiner.</summary>
    public string EmpfehlungKurztext { get; set; } = "";

    /// <summary>Darf die Zeile bearbeitet werden? (Auslieferungsvorlagen nicht.)</summary>
    public bool Schreibbar { get; set; } = true;
}

/// <summary>
/// Was die Hülle zu einem <see cref="KostenKomponenteKontext"/> antwortet
/// (iU9-W4.2) — alles, was der Dialog danach zeigt.
///
/// <para>Im Bestand verteilte sich dasselbe auf <c>KopfAnzeigen</c>,
/// <c>VariantenLaden</c>, <c>RasterAufbauen</c>, <c>SummenAnzeigen</c> und
/// <c>ErtragReiterSteuern</c>. Ein Wert statt fünf Methoden heißt: Der Dialog
/// fragt einmal und zeigt, was zurückkommt — und die Hülle behält die Regel,
/// welche Liste zu welchem Kontext gehört (Regel F4).</para>
/// </summary>
public sealed class KostenKomponenteStand
{
    /// <summary>Überschrift (<c>lblTitel</c>).</summary>
    public string Titel { get; set; } = "";

    /// <summary>Unterzeile (<c>lblUntertitel</c>).</summary>
    public string Untertitel { get; set; } = "";

    /// <summary>Die wählbaren Vorlagen/Varianten; leer im Projektmodus.</summary>
    public IReadOnlyList<(int Id, string Text)> Varianten { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Die gewählte Variante.</summary>
    public int? VarianteId { get; set; }

    /// <summary>Zeigt die Variantenzeile (nur im Stammkontext, Ä-KD6a).</summary>
    public bool VariantePflegbar { get; set; }

    /// <summary>Die gewählte Vorlage ist eine Auslieferungsvorlage (<c>lblReadOnly</c>).</summary>
    public bool NurLesen { get; set; }

    /// <summary>Die Positionen in Anzeigereihenfolge.</summary>
    public IReadOnlyList<KostenPositionZeile> Zeilen { get; set; }
        = Array.Empty<KostenPositionZeile>();

    /// <summary>Die wählbaren Bemessungen dieses Kontexts (§ 5.3, gefiltert).</summary>
    public IReadOnlyList<(int Id, string Text)> Bemessungen { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Überschrift der Betragsspalte („Betrag netto [€]" bzw. „[€/a]").</summary>
    public string SpalteBetrag { get; set; } = "";

    /// <summary>Zeigt die Spalte Nutzungsdauer (nur bei Investitionskosten).</summary>
    public bool MitNutzungsdauer { get; set; }

    /// <summary>Zeigt das ± je Zeile (nur im Projektmodus).</summary>
    public bool MitWorstBest { get; set; }

    /// <summary>Der Summenfuß: Nettosumme (stark) und Bruttozeile.</summary>
    public IReadOnlyList<(string Text, bool Stark)> Summen { get; set; }
        = Array.Empty<(string, bool)>();

    /// <summary>Ist eine neue Position möglich? (<c>btnPositionNeu.Enabled</c>)</summary>
    public bool PositionNeuMoeglich { get; set; }

    /// <summary>Löschen der Variante möglich? (<c>btnVarianteLoeschen.Enabled</c>)</summary>
    public bool VarianteLoeschbar { get; set; }

    /// <summary>Zeigt den Abschnitt „Ertrag/Bonus" (FK5: nur BHKW und Photovoltaik).</summary>
    public bool ErtragSichtbar { get; set; }

    /// <summary>Die Werte dieses Abschnitts, wenn er sichtbar ist.</summary>
    public IReadOnlyDictionary<string, object>? ErtragGaben { get; set; }
}
