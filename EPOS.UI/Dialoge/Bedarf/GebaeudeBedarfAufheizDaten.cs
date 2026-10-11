namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Gruppe „Aufheizung" des Bedarfsdialogs</b> (Entwurf KP3, Welle O2; E59, E60, Festlegungen 25, 39, 41) —
/// die Werte der Ergebniszeile des Laufs (<c>GebaeudeKennzahlen.Bilden</c>) bzw. bei ausgeschalteter Optimierung die
/// Teile der Auslegungsgröße aus der Auskunft der Bemessung; fertig benannt von der Hülle. Alle Zahlen nullbar:
/// ohne Wert steht „—", nie eine erfundene Zahl. Leistungen in kW, Zeiten in h.
/// </summary>
public sealed class GebaeudeBedarfAufheizDaten
{
    /// <summary>Kennwort AUS: Die Optimierung ist ausgeschaltet, es gibt nur die Auslegungsgröße aus der Auskunft.</summary>
    public const string AUS = "AUS";

    /// <summary>Der Zustand als Kennwort: BEMESSEN, UNERREICHBAR, GEKOPPELT oder <see cref="AUS"/>.</summary>
    public string Zustand { get; init; } = "";

    /// <summary>Der Zustand als Anzeigetext.</summary>
    public string Zustandtext { get; init; } = "";

    /// <summary>Ist die Optimierung des Projekts ausgeschaltet?</summary>
    public bool SchalterAus => Zustand == AUS;

    /// <summary>t_auf,max — die BEMESSENE Aufheizzeit [h] (auch bei Art „manuell"); <c>null</c> bei UNERREICHBAR.</summary>
    public int? AufheizzeitMaxH { get; init; }

    /// <summary>T_a,B [°C] — die Außentemperatur des Bemessungsfalls.</summary>
    public double? AussenC { get; init; }

    /// <summary>Die Variante der Bemessung als Anzeigetext („kälteste Stunde" …).</summary>
    public string Variante { get; init; } = "";

    /// <summary>Die wirksame Art als Anzeigetext (täglich, fest, manuell (n h)); leer bei GEKOPPELT.</summary>
    public string Art { get; init; } = "";

    /// <summary>Die manuelle Aufheizzeit des Gebäudes [h]; <c>null</c> = Art des Projekts.</summary>
    public int? AufheizzeitManuellH { get; init; }

    /// <summary>P_auf [kW], skaliert wie die Spitzen.</summary>
    public double? LeistungKw { get; init; }

    /// <summary>Die Quelle von P_auf als Anzeigetext (Heizleistungsgrenze, Zielleistung, gemischt).</summary>
    public string Quelle { get; init; } = "";

    /// <summary>Tage mit einer Rampe.</summary>
    public int? Aufheiztage { get; init; }

    /// <summary>Σ (n − 1) — die Rampenstunden des Jahres [h].</summary>
    public int? AufheizstundenH { get; init; }

    /// <summary>Die längste Rampe des Jahres [h].</summary>
    public int? AufheizzeitLaengsteH { get; init; }

    /// <summary>W2 — Tage, an denen die Absenkdauer die Rampe begrenzt hat.</summary>
    public int? TageBegrenzt { get; init; }

    /// <summary>W1 — Tage ohne erreichbare Rampe bis 48 h.</summary>
    public int? TageUnerreichbar { get; init; }

    /// <summary>W3 — Tage außerhalb des Nachweisbands.</summary>
    public int? TageNachweisband { get; init; }

    /// <summary>W4 — Sprünge aus „aus" ohne Rampe.</summary>
    public int? SpruengeAus { get; init; }

    /// <summary>Σ der Kappungsanteile an der Heizleistungsgrenze [h].</summary>
    public double? KappungsstundenH { get; init; }

    /// <summary>Die Auslegungsgröße Φ_HL + Φ_RH [kW] (E60, P17 (b)); <c>null</c>, wenn ein Teil fehlt.</summary>
    public double? AuslegungsgroesseKw { get; init; }

    /// <summary>Φ_HL — die stationäre Auslegungsheizlast [kW].</summary>
    public double? AuslegungsheizlastKw { get; init; }

    /// <summary>Φ_RH — der Aufheizzuschlag [kW].</summary>
    public double? AufheizzuschlagKw { get; init; }

    /// <summary>Die ideale Spitze [kW] — die höchste Stundenlast des Laufs.</summary>
    public double? SpitzeKw { get; init; }

    /// <summary>Das größte gleitende 24-h-Mittel [kW].</summary>
    public double? SpitzeTagesmittelKw { get; init; }

    /// <summary>Verbrauchsangabe bei ausgeschalteter Optimierung: Den Faktor kennt erst der Jahreslauf.</summary>
    public bool FaktorErstImLauf { get; init; }

    /// <summary>
    /// Der Hinweis der Rückstufe (Anlagenkopplung AK3, Festlegung 20) aus der Aufheizauskunft: Sie rechnet ohne
    /// geschlossenen Kreis auf dem Profilweg; leer ohne Rückstufe oder ohne Auskunft.
    /// </summary>
    public string Rueckstufe { get; init; } = "";

    /// <summary>Die benannten Hinweise W1–W5, fertig formuliert; leer = keiner.</summary>
    public IReadOnlyList<string> Hinweise { get; init; } = new List<string>();

    /// <summary>Die Zonen eines Mehrzonengebäudes in Rangfolge; leer bei höchstens einer Zone.</summary>
    public IReadOnlyList<GebaeudeBedarfAufheizZoneDaten> Zonen { get; init; } = new List<GebaeudeBedarfAufheizZoneDaten>();
}

/// <summary>EINE Zone in der Gruppe „Aufheizung" (Festlegung 22): ihre eigene Bemessung und Zählung.</summary>
public sealed class GebaeudeBedarfAufheizZoneDaten
{
    /// <summary>Der Name der Zone.</summary>
    public string Name { get; init; } = "";

    /// <summary>Der Zustand als Anzeigetext (bemessen, unerreichbar, gekoppelt, unbeheizt).</summary>
    public string Zustandtext { get; init; } = "";

    /// <summary>t_auf,max der Zone [h].</summary>
    public int? AufheizzeitMaxH { get; init; }

    /// <summary>P_auf der Zone [kW].</summary>
    public double? LeistungKw { get; init; }

    /// <summary>Die Quelle von P_auf als Anzeigetext.</summary>
    public string Quelle { get; init; } = "";

    /// <summary>Tage mit einer Rampe.</summary>
    public int? Aufheiztage { get; init; }

    /// <summary>Die längste Rampe [h].</summary>
    public int? AufheizzeitLaengsteH { get; init; }

    /// <summary>Σ der Kappungsanteile [h].</summary>
    public double? KappungsstundenH { get; init; }
}
