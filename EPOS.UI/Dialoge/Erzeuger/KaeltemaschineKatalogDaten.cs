using System.Globalization;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Der Feldsatz der Verwaltung „Kältemaschinen"</b> (KU3-1) — das plattformfreie Abbild von
/// <c>KaeltemaschineModel</c> samt Kennlinie. Die Hülle (<c>KaeltemaschineKatalogHuelle</c>) bildet
/// zwischen beiden ab; die Komponente kennt weder Tabelle noch Persistenzwerte.
///
/// <para><b>Die Rückkühlart ist ein LISTENPLATZ</b> (<see cref="RueckkuehlartIndex"/>), kein Text: Der
/// Anzeigetext ist sprachabhängig und nie ein Steuerwert; die Abbildung Platz → Persistenzwert liegt
/// in der Hülle über <c>KaeltemaschineSchema.RUECKKUEHLARTEN</c>.</para>
///
/// <para><b>Leer ist nicht null:</b> Alle Zahlen sind <c>double?</c> — ein leerer Hilfsstrom heißt
/// „im EER enthalten", eine 0 wäre eine gemessene Null.</para>
/// </summary>
public sealed class KaeltemaschineDaten
{
    /// <summary>Id des Katalogsatzes; 0 = neu.</summary>
    public int Id { get; set; }

    /// <summary>Name des Geräts (im Katalog eindeutig).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Hersteller.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Typbezeichnung.</summary>
    public string Typ { get; set; } = "";

    /// <summary>Freitext.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Nennkälteleistung [kW].</summary>
    public double? Nennkaelteleistung { get; set; }

    /// <summary>EER im Nennpunkt [—].</summary>
    public double? NennEer { get; set; }

    /// <summary>Kältemittel.</summary>
    public string Kaeltemittel { get; set; } = "";

    /// <summary>Listenplatz der Rückkühlart; <c>null</c> = keine Angabe.</summary>
    public int? RueckkuehlartIndex { get; set; }

    /// <summary>Kleinste Teillast [%].</summary>
    public double? Mindestteillast { get; set; }

    /// <summary>Elektrische Leistung der Rückkühlung im Nennpunkt [kW]; <c>null</c> = im EER enthalten.</summary>
    public double? Hilfsstrom { get; set; }

    /// <summary>Kleinster zulässiger Kaltwasservorlauf [°C].</summary>
    public double? KaltwasserMin { get; set; }

    /// <summary>Gerätepreis [€].</summary>
    public double? Modulkosten { get; set; }

    // ------------------------------------------------------------ Katalogfelder (K-A)

    /// <summary>Listenplatz der Geräteart in <c>KaelteKatalogfelderSchema.GERAETEARTEN</c>; <c>null</c> = nach der Rückkühlart.</summary>
    public int? GeraeteartIndex { get; set; }

    /// <summary>GWP des Kältemittels [—].</summary>
    public double? Gwp { get; set; }

    /// <summary>Füllmenge des Kältemittels [kg].</summary>
    public double? Fuellmenge { get; set; }

    /// <summary>Listenplatz der Art der saisonalen Kennzahl in <c>KaelteKatalogfelderSchema.SAISON_ARTEN</c>; <c>null</c> = keine Angabe.</summary>
    public int? SaisonArtIndex { get; set; }

    /// <summary>Wert der saisonalen Kennzahl: SEER [—] oder η<sub>s,c</sub> [%].</summary>
    public double? Saisonkennzahl { get; set; }

    // ------------------------------------------------------------ Teillast und Takten (KM3)

    /// <summary>
    /// Listenplatz der Teillastrechnung in <c>KaeltemaschineTeillastSchema.TEILLAST_WEGE</c> (0 linear, 1 Kurve);
    /// <c>null</c> = wie bisher (linear, ohne Taktverlust).
    /// </summary>
    public int? TeillastWegIndex { get; set; }

    /// <summary>Beiwert a der Teillastkurve; leer = keine Kurve gepflegt.</summary>
    public double? KurveA { get; set; }

    /// <summary>Beiwert b der Teillastkurve.</summary>
    public double? KurveB { get; set; }

    /// <summary>Beiwert c der Teillastkurve.</summary>
    public double? KurveC { get; set; }

    /// <summary>Lastgrad (0 … 1), ab dem die Kurve gilt; leer = Mindestteillast.</summary>
    public double? KurveLastgradMin { get; set; }

    /// <summary>Taktverlustfaktor C_d (0 … 1); leer = Vorgabe 0,9.</summary>
    public double? Cd { get; set; }

    /// <summary>Listenplatz der Verdichterregelung in <c>KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN</c>; <c>null</c> = keine Angabe.</summary>
    public int? VerdichterregelungIndex { get; set; }

    /// <summary>Listenplatz des Wegs am Kennfeldrand in <c>KaeltemaschineTeillastSchema.RANDWEGE</c>; <c>null</c> = Randwert (Vorgabe).</summary>
    public int? RandwegIndex { get; set; }

    /// <summary>Auslieferungssatz (<c>ReadOnly</c>)? Dann ist das Blatt nur lesbar.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Die Kennlinienpunkte in der Reihenfolge des Blatts.</summary>
    public List<KaeltemaschinePunktDaten> Kennlinie { get; set; } = new();

    /// <summary>Eine tiefe Kopie — der Arbeitsstand des Blatts teilt keinen Punkt mit dem gespeicherten.</summary>
    public KaeltemaschineDaten Kopie()
    {
        var k = (KaeltemaschineDaten)MemberwiseClone();
        k.Kennlinie = Kennlinie.Select(p => p.Kopie()).ToList();
        return k;
    }

    /// <summary>Wie viele Felder weichen ab? Die Kennlinie zählt als EIN Feld.</summary>
    public int Abweichungen(KaeltemaschineDaten anderer)
    {
        if (anderer is null) return 0;
        int n = 0;
        if (!Gleich(Bezeichner, anderer.Bezeichner)) n++;
        if (!Gleich(Firma, anderer.Firma)) n++;
        if (!Gleich(Typ, anderer.Typ)) n++;
        if (!Gleich(Beschreibung, anderer.Beschreibung)) n++;
        if (Nennkaelteleistung != anderer.Nennkaelteleistung) n++;
        if (NennEer != anderer.NennEer) n++;
        if (!Gleich(Kaeltemittel, anderer.Kaeltemittel)) n++;
        if (RueckkuehlartIndex != anderer.RueckkuehlartIndex) n++;
        if (Mindestteillast != anderer.Mindestteillast) n++;
        if (Hilfsstrom != anderer.Hilfsstrom) n++;
        if (KaltwasserMin != anderer.KaltwasserMin) n++;
        if (Modulkosten != anderer.Modulkosten) n++;
        if (GeraeteartIndex != anderer.GeraeteartIndex) n++;
        if (Gwp != anderer.Gwp) n++;
        if (Fuellmenge != anderer.Fuellmenge) n++;
        if (SaisonArtIndex != anderer.SaisonArtIndex) n++;
        if (Saisonkennzahl != anderer.Saisonkennzahl) n++;
        if (TeillastWegIndex != anderer.TeillastWegIndex) n++;
        if (KurveA != anderer.KurveA) n++;
        if (KurveB != anderer.KurveB) n++;
        if (KurveC != anderer.KurveC) n++;
        if (KurveLastgradMin != anderer.KurveLastgradMin) n++;
        if (Cd != anderer.Cd) n++;
        if (VerdichterregelungIndex != anderer.VerdichterregelungIndex) n++;
        if (RandwegIndex != anderer.RandwegIndex) n++;
        if (Kennlinie.Count != anderer.Kennlinie.Count
            || Kennlinie.Where((p, i) => !p.Gleich(anderer.Kennlinie[i])).Any()) n++;
        return n;
    }

    private static bool Gleich(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.Ordinal);
}

/// <summary>Ein Punkt der Kennlinie: EER und Kälteleistung über Rückkühl- und Kaltwassertemperatur.</summary>
public sealed class KaeltemaschinePunktDaten
{
    /// <summary>Eintrittstemperatur des Rückkühlmediums [°C]; leer nur im Arbeitsstand eines neuen Punkts.</summary>
    public double? Rueckkuehltemperatur { get; set; }

    /// <summary>Kaltwasservorlauf [°C].</summary>
    public double? Kaltwassertemperatur { get; set; }

    /// <summary>EER im Punkt [—].</summary>
    public double? Eer { get; set; }

    /// <summary>Kälteleistung im Punkt [kW].</summary>
    public double? Kaelteleistung { get; set; }

    /// <summary>Eine Kopie des Punkts.</summary>
    public KaeltemaschinePunktDaten Kopie() => (KaeltemaschinePunktDaten)MemberwiseClone();

    /// <summary>Tragen beide Punkte dieselben vier Werte?</summary>
    public bool Gleich(KaeltemaschinePunktDaten p)
        => p is not null && Rueckkuehltemperatur == p.Rueckkuehltemperatur && Kaltwassertemperatur == p.Kaltwassertemperatur
           && Eer == p.Eer && Kaelteleistung == p.Kaelteleistung;
}

/// <summary>Der Ausgang eines Schreibwegs der Hülle: bei Erfolg die Id, sonst der Grund.</summary>
/// <summary>
/// <b>Die zwei Kennlinienbilder des Stammblatts</b> (KD-3): EER und Kälteleistung über der Rückkühltemperatur (bei
/// Luft der Außentemperatur), eine Linie je Kaltwassertemperatur — gezeichnet vom Renderer des Kerns
/// (<c>ChartRenderer.KennlinienModell</c>), gebaut in der Hülle (<c>KaeltemaschineKennlinienbild</c>).
/// <c>null</c> = kein vollständiger Punkt, der Baustein zeigt den Platzhalter.
/// </summary>
public sealed record KaeltemaschineKennlinienbilder(WindowsFormsApplication1.Zeichnung.Zeichenmodell? Eer,
                                                    WindowsFormsApplication1.Zeichnung.Zeichenmodell? Leistung)
{
    /// <summary>Ohne Bild.</summary>
    public static readonly KaeltemaschineKennlinienbilder Leer = new(null, null);
}

public sealed record KaeltemaschineSpeicherErgebnis(bool Ok, string Meldung, int Id);

/// <summary>Der Ausgang von „Typkennfelder laden…" (KM1): neu angelegt, übersprungen, bei Fehler der Grund.</summary>
public sealed record KaeltemaschineTypkennfelderErgebnis(bool Ok, int Neu, int Uebersprungen, string Meldung);

/// <summary>Die Lesezeile der Gruppe „Teillast und Takten": g bei 25, 50 und 75 % Last und ein Hinweis (leer = keiner).</summary>
public sealed record KaeltemaschineTeillastLesestand(double G25, double G50, double G75, string Hinweis);

/// <summary>
/// Die Teillastfelder eines Typkennfelds für die Schnellwahl „Kurve aus Typkennfeld" — Listenplätze wie im Feldsatz.
/// </summary>
public sealed record KaeltemaschineTypkurve(string Typkennfeld, int? TeillastWegIndex, double? KurveA, double? KurveB,
                                            double? KurveC, double? KurveLastgradMin, int? VerdichterregelungIndex);

/// <summary>Der Ausgang von „Typkennfeld auf Datenblatt skalieren…": der neue, ungespeicherte Satz oder der Grund.</summary>
public sealed record KaeltemaschineSkalierErgebnis(KaeltemaschineDaten? Satz, string Meldung);

/// <summary>Ein Eingabepaar der Auskunft „Teillastpunkte prüfen…": Außentemperatur [°C] und Lastgrad [%] der Nennleistung.</summary>
public sealed record KaeltemaschineAuskunftEingabe(string Name, double? AussenC, double? LastgradProzent);

/// <summary>Eine Ergebniszeile der Auskunft (Leistungen in kW, Lastgrad der Maschine als Anteil).</summary>
public sealed record KaeltemaschineAuskunftZeile(string Name, double AussenC, double RueckkuehlC, double KaelteKw,
                                                 double LastgradMaschine, double LeistungsaufnahmeKw, double Eer,
                                                 bool Takt, bool Randwert);

/// <summary>Der Ausgang der Auskunft: die Zeilen oder der Grund (nichts wird gespeichert).</summary>
public sealed record KaeltemaschineAuskunftErgebnis(IReadOnlyList<KaeltemaschineAuskunftZeile>? Zeilen, string Meldung);

/// <summary>
/// <b>Ein Eintrag der Klappliste „Kältemittel“</b> (KM4-E3-a, Konzept Kältemittel als Stammdatum 7.1) — fertig aus der
/// Hülle über <c>KaeltemittelCtrl.Liste()</c>: <paramref name="Code"/> ist der Schlüssel und der Text, den der Satz
/// speichert (die ID setzt der Schreibweg des Kerns per Code-Abgleich), <paramref name="Text"/> die Anzeige (Code und
/// Bezeichnung), <paramref name="Gwp"/> der Tabellenwert und <paramref name="Lesewerte"/> die übrigen Stammwerte
/// (F-Gas, natürlich, Sicherheitsgruppe, PFAS, Status) als eine Lesezeile.
/// </summary>
public sealed record KaeltemittelWahl(int Id, string Code, string Text, double? Gwp, string Lesewerte);

/// <summary>
/// <b>Die Lesezeilen unter der Klappliste „Kältemittel“</b> (KM4-E3-a) — eine Stelle für Dialog und Tests:
/// Eintrag zum Text, GWP mit Herkunft nach KM4-Q17 (Tabellenwert vor Geräte-GWP), Abweichungshinweis und die Frage, ob
/// die Schnellwahl „GWP aus Kältemittel übernehmen“ wirkt.
/// </summary>
public static class KaeltemittelLesung
{
    /// <summary>Der Listenplatz des Codes (Trim, ohne Groß/Klein); −1 ohne Treffer, <c>null</c> bei leerem Text.</summary>
    public static int? Platz(IReadOnlyList<KaeltemittelWahl> liste, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string code = text.Trim();
        for (int i = 0; i < liste.Count; i++)
            if (string.Equals(liste[i].Code?.Trim(), code, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    /// <summary>Der Eintrag zum Text; <c>null</c> ohne Treffer oder bei leerem Text.</summary>
    public static KaeltemittelWahl? Eintrag(IReadOnlyList<KaeltemittelWahl> liste, string? text)
        => Platz(liste, text) is int i && i >= 0 ? liste[i] : null;

    /// <summary>Die Einträge der Klappliste: ein Bestandstext ohne Treffer zuerst als „nicht zugeordnet: …“ (Platz −1).</summary>
    public static IReadOnlyList<(int Id, string Text)> Eintraege(IReadOnlyList<KaeltemittelWahl> liste, string? text,
                                                               KaeltemaschineKatalogTexte t)
    {
        var l = new List<(int, string)>();
        if (Platz(liste, text) == -1)
            l.Add((-1, string.Format(CultureInfo.CurrentCulture, t.KaeltemittelNichtZugeordnet, text!.Trim())));
        for (int i = 0; i < liste.Count; i++) l.Add((i, liste[i].Text));
        return l;
    }

    /// <summary>Die Anzeige des Werts im Lesemodus: Text des Eintrags, sonst „nicht zugeordnet: …“, leer bleibt leer.</summary>
    public static string Anzeige(IReadOnlyList<KaeltemittelWahl> liste, string? text, KaeltemaschineKatalogTexte t)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return Eintrag(liste, text) is KaeltemittelWahl e
            ? e.Text
            : string.Format(CultureInfo.CurrentCulture, t.KaeltemittelNichtZugeordnet, text.Trim());
    }

    /// <summary>
    /// Die Lesezeilen in ihrer Reihenfolge: GWP mit Herkunft, Abweichungshinweis, die übrigen Stammwerte; ohne
    /// Tabellenzeile der Hinweis, dass die Prüfung das Geräte-GWP nimmt. Leere Zeilen fallen weg.
    /// </summary>
    public static IReadOnlyList<string> Zeilen(IReadOnlyList<KaeltemittelWahl> liste, string? text, double? geraeteGwp,
                                               KaeltemaschineKatalogTexte t)
    {
        var z = new List<string>();
        KaeltemittelWahl? e = Eintrag(liste, text);
        if (e?.Gwp is double tab)
            z.Add(string.Format(CultureInfo.CurrentCulture, t.GwpAusTabelle, Gwp(tab)));
        else if (geraeteGwp is double g)
            z.Add(string.Format(CultureInfo.CurrentCulture, t.GwpAusGeraet, Gwp(g)));
        if (e is not null && WeichtAb(e, geraeteGwp))
            z.Add(string.Format(CultureInfo.CurrentCulture, t.GwpAbweichung, Gwp(geraeteGwp!.Value), Gwp(e.Gwp!.Value)));
        if (e is not null && !string.IsNullOrWhiteSpace(e.Lesewerte)) z.Add(e.Lesewerte);
        if (e is null && !string.IsNullOrWhiteSpace(text) && liste.Count > 0) z.Add(t.KaeltemittelOhneZuordnung);
        return z;
    }

    /// <summary>Weicht das Geräte-GWP vom Tabellenwert des Eintrags ab (KM4-Q17 a, Schwelle des Kerns)?</summary>
    public static bool WeichtAb(KaeltemittelWahl? e, double? geraeteGwp)
        => e is not null && WindowsFormsApplication1.KaeltemittelZulaessigkeitszeile.GwpWeichtAb(e.Gwp, geraeteGwp);

    /// <summary>Wirkt die Schnellwahl? Nur mit Tabellenwert und bei leerem oder abweichendem Geräte-GWP.</summary>
    public static bool SchnellwahlAktiv(IReadOnlyList<KaeltemittelWahl> liste, string? text, double? geraeteGwp)
        => Eintrag(liste, text) is KaeltemittelWahl e && e.Gwp.HasValue && (!geraeteGwp.HasValue || WeichtAb(e, geraeteGwp));

    private static string Gwp(double wert) => wert.ToString("#,0.###", CultureInfo.CurrentCulture);
}
