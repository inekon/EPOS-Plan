using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Das FLACHE Abbild der Maske „Wirtschaftlichkeits-Parameter" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Drei Objekte, ein Feldsatz.</b> Die Maske pflegt den Parametersatz des
/// Projekts UND die zwei Szenariosätze Best und Worst. Alle drei sind
/// Fachklassen des Kerns mit öffentlichen FELDERN statt Eigenschaften; die
/// Maskenbrücke löst über Eigenschaften auf, und ein Katalog an einem von ihnen
/// verschwiege außerdem die zwei anderen.</para>
///
/// <para><b>Die Szenariofelder zeigen den WIRKSAMEN Wert, nicht den gepflegten.</b>
/// Ein leeres Feld gäbe es sonst für jede Vorgabe, und niemand sähe, womit
/// gerechnet wird — die Maske hält es genauso. Gesetzt wird dagegen der gepflegte
/// Wert: Wer tippt, pflegt.</para>
///
/// <para><b>Die Erwartet-Spalte der Szenariotabelle ist ANZEIGE</b> und steht nicht
/// im Katalog: Sie wiederholt, was oben unter „Allgemein" gepflegt wird.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class WirtschaftlichkeitParameterKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Der Parametersatz des Projekts; nie <c>null</c>, solange die Maske steht.</summary>
    public Func<WirtschaftlichkeitParameter?>? ParameterLesen { get; init; }

    /// <summary>Der Best-Szenariosatz.</summary>
    public Func<SzenarioSatz?>? BestLesen { get; init; }

    /// <summary>Der Worst-Szenariosatz.</summary>
    public Func<SzenarioSatz?>? WorstLesen { get; init; }

    public Func<IReadOnlyList<KiWahleintrag>>? ParkEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? MethodeEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BiomasseEintraege { get; init; }

    /// <summary>ETAPPE E15 (V‑G7): die Arten der Risikoberücksichtigung.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? RisikoEintraege { get; init; }

    /// <summary>ETAPPE E19: die Unternehmensarten nach StromStG — Schlüssel ist ihr Steuerwert.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? UnternehmensartEintraege { get; init; }

    /// <summary>
    /// ETAPPE E19 (E19‑Q5 a): der Setzweg der Unternehmensart. Er gehört dem Dialog, weil
    /// nur er weiß, ob die Vergleichsgruppe ein BHKW führt — dann ist der Dialog
    /// „BHKW-Wirtschaftlichkeit" die einzige Pflegestelle. Liefert leer oder den Grund der
    /// Ablehnung.
    /// </summary>
    public Func<string?, string>? UnternehmensartSetzen { get; init; }

    /// <summary>Die Maske zeichnet neu — nach jedem Setzen.</summary>
    public Action? Nachziehen { get; init; }

    private WirtschaftlichkeitParameter? P => ParameterLesen?.Invoke();
    private SzenarioSatz? B => BestLesen?.Invoke();
    private SzenarioSatz? W => WorstLesen?.Invoke();

    private void Setze(Action<WirtschaftlichkeitParameter> schritt)
    {
        WirtschaftlichkeitParameter? satz = P;
        if (satz is null) return;
        schritt(satz);
        Nachziehen?.Invoke();
    }

    private void Best(Action<SzenarioSatz> schritt)
    {
        SzenarioSatz? satz = B;
        if (satz is null) return;
        schritt(satz);
        Nachziehen?.Invoke();
    }

    private void Worst(Action<SzenarioSatz> schritt)
    {
        SzenarioSatz? satz = W;
        if (satz is null) return;
        schritt(satz);
        Nachziehen?.Invoke();
    }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Die Referenz-Kraftwerksparks — Schlüssel ist ihre Id.</summary>
    public IReadOnlyList<KiWahleintrag> KraftwerksparkWahl
        => ParkEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Emissionsmethoden — Schlüssel ist ihr Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> EmissionsmethodeWahl
        => MethodeEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Biomasse-Konventionen — Schlüssel ist ihr Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> BiomassekonventionWahl
        => BiomasseEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>ETAPPE E15: die Arten der Risikoberücksichtigung — Schlüssel ist ihr
    /// Steuerwert (leer = aus, ZINS, ABZUG).</summary>
    public IReadOnlyList<KiWahleintrag> RisikoArtWahl
        => RisikoEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>ETAPPE E19: die Unternehmensarten — Schlüssel ist ihr Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> UnternehmensartWahl
        => UnternehmensartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Allgemein
    // =====================================================================

    /// <summary>Der Kalkulationszinssatz i.</summary>
    public double Zinssatz
    {
        get => P?.Zinssatz ?? 0;
        set => Setze(p => p.Zinssatz = value);
    }

    /// <summary>Der Betrachtungszeitraum T.</summary>
    public int Betrachtungszeitraum
    {
        get => P?.Betrachtungszeitraum ?? 0;
        set => Setze(p => p.Betrachtungszeitraum = value);
    }

    /// <summary>Die Preissteigerung der Energiekosten p_E.</summary>
    public double PreissteigerungEnergie
    {
        get => P?.PreissteigerungEnergie ?? 0;
        set => Setze(p => p.PreissteigerungEnergie = value);
    }

    /// <summary>Die Preissteigerung der Betriebskosten p_B.</summary>
    public double PreissteigerungBetrieb
    {
        get => P?.PreissteigerungBetrieb ?? 0;
        set => Setze(p => p.PreissteigerungBetrieb = value);
    }

    /// <summary>Die Preissteigerung der Investition p_I; leer = „wie Betrieb".</summary>
    public double? PreissteigerungInvestition
    {
        get => P?.PreissteigerungInvestition;
        set => Setze(p => p.PreissteigerungInvestition = value);
    }

    // =====================================================================
    //  Strom, Brennstoff, Bilanzierung
    // =====================================================================

    /// <summary>Die Einspeisevergütung für PV-Strom.</summary>
    public double Einspeiseverguetung
    {
        get => P?.Einspeiseverguetung ?? 0;
        set => Setze(p => p.Einspeiseverguetung = value);
    }

    /// <summary>Der konstante CO₂-Preis; 0 = Pfad aus dem Gesetzeskatalog.</summary>
    public double Co2Preis
    {
        get => P?.CO2Preis ?? 0;
        set => Setze(p => p.CO2Preis = value);
    }

    /// <summary>Der Referenz-Kraftwerkspark der Stromgutschrift.</summary>
    public int Kraftwerkspark
    {
        get => P?.IdKraftwerkspark ?? 0;
        set => Setze(p => p.IdKraftwerkspark = value);
    }

    /// <summary>
    /// ETAPPE E19 (Konzept § 6.3 Nr. 33): die Unternehmensart nach StromStG — nur ohne
    /// BHKW hier gepflegt. Mit BHKW lehnt der Setzer benannt ab und nennt den Dialog
    /// „BHKW-Wirtschaftlichkeit" (keine zweite Pflegestelle derselben Spalte).
    /// </summary>
    public string Unternehmensart
    {
        get => P?.Unternehmensart ?? "";
        set
        {
            if (P is null) return;
            string grund = UnternehmensartSetzen?.Invoke(value) ?? "";
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
            Nachziehen?.Invoke();
        }
    }

    /// <summary>Das Bilanzjahr; 0 = nicht gepflegt.</summary>
    public int Bilanzjahr
    {
        get => P?.BilanzJahr ?? 0;
        set => Setze(p => p.BilanzJahr = value);
    }

    /// <summary>Die Emissionsmethode.</summary>
    public string Emissionsmethode
    {
        get => P?.EmissionsMethode ?? "";
        set => Setze(p => p.EmissionsMethode = value ?? "");
    }

    /// <summary>Die Biomasse-Konvention.</summary>
    public string Biomassekonvention
    {
        get => P?.BiomasseKonvention ?? "";
        set => Setze(p => p.BiomasseKonvention = value ?? "");
    }

    /// <summary>Liegt ein Nachhaltigkeitsnachweis für die Biomasse vor?</summary>
    public bool Nachhaltigkeitsnachweis
    {
        get => P?.NachhaltigkeitsnachweisBiomasse ?? false;
        set => Setze(p => p.NachhaltigkeitsnachweisBiomasse = value);
    }

    // =====================================================================
    //  Risiko (ETAPPE E15, V-G7) — DIN EN 17463, 6.5 und Anhang F
    // =====================================================================

    /// <summary>Die Art der Risikoberücksichtigung: leer = aus, ZINS, ABZUG.</summary>
    public string RisikoArt
    {
        get => Risikoart.Normiert(P?.RisikoArt) ?? "";
        set => Setze(p => p.RisikoArt = Risikoart.Normiert(value));
    }

    /// <summary>Der Zinszuschlag [%-Punkte]; wirkt nur bei Art ZINS.</summary>
    public double? RisikoZinszuschlag
    {
        get => P?.RisikoZinszuschlag;
        set => Setze(p => p.RisikoZinszuschlag = value);
    }

    /// <summary>Die Rückflusseinbuße R_loss [€ je Periode]; wirkt nur bei Art ABZUG.</summary>
    public double? RisikoVerlust
    {
        get => P?.RisikoVerlust;
        set => Setze(p => p.RisikoVerlust = value);
    }

    /// <summary>Die Eintrittswahrscheinlichkeit p_loss [%]; wirkt nur bei Art ABZUG.</summary>
    public double? RisikoWahrscheinlichkeit
    {
        get => P?.RisikoWahrscheinlichkeit;
        set => Setze(p => p.RisikoWahrscheinlichkeit = value);
    }

    // =====================================================================
    //  Szenarien — je Größe ein Best- und ein Worst-Feld
    // =====================================================================

    /// <summary>Der wirksame Kalkulationszins des BEST-Falls.</summary>
    public double BestZinssatz
    {
        get => B?.ZinsWirksam(Zinssatz) ?? 0;
        set => Best(s => s.Zinssatz = value);
    }

    /// <summary>Der wirksame Kalkulationszins des WORST-Falls.</summary>
    public double WorstZinssatz
    {
        get => W?.ZinsWirksam(Zinssatz) ?? 0;
        set => Worst(s => s.Zinssatz = value);
    }

    /// <summary>Die wirksame Preissteigerung Energie des BEST-Falls.</summary>
    public double BestPreissteigerungEnergie
    {
        get => B?.PreisEnergieWirksam(PreissteigerungEnergie) ?? 0;
        set => Best(s => s.PreissteigerungEnergie = value);
    }

    /// <summary>Die wirksame Preissteigerung Energie des WORST-Falls.</summary>
    public double WorstPreissteigerungEnergie
    {
        get => W?.PreisEnergieWirksam(PreissteigerungEnergie) ?? 0;
        set => Worst(s => s.PreissteigerungEnergie = value);
    }

    /// <summary>Die wirksame Preissteigerung Betrieb des BEST-Falls.</summary>
    public double BestPreissteigerungBetrieb
    {
        get => B?.PreisBetriebWirksam(PreissteigerungBetrieb) ?? 0;
        set => Best(s => s.PreissteigerungBetrieb = value);
    }

    /// <summary>Die wirksame Preissteigerung Betrieb des WORST-Falls.</summary>
    public double WorstPreissteigerungBetrieb
    {
        get => W?.PreisBetriebWirksam(PreissteigerungBetrieb) ?? 0;
        set => Worst(s => s.PreissteigerungBetrieb = value);
    }

    /// <summary>Die wirksame Preissteigerung Investition des BEST-Falls.</summary>
    public double BestPreissteigerungInvestition
    {
        get => B?.PreisInvestWirksam(P?.PreisInvestWirksam ?? 0) ?? 0;
        set => Best(s => s.PreissteigerungInvestition = value);
    }

    /// <summary>Die wirksame Preissteigerung Investition des WORST-Falls.</summary>
    public double WorstPreissteigerungInvestition
    {
        get => W?.PreisInvestWirksam(P?.PreisInvestWirksam ?? 0) ?? 0;
        set => Worst(s => s.PreissteigerungInvestition = value);
    }

    /// <summary>Die wirksame Investitionsänderung des BEST-Falls.</summary>
    public double BestInvestitionAenderung
    {
        get => B?.InvestWirksam ?? 0;
        set => Best(s => s.InvestitionAenderung = value);
    }

    /// <summary>Die wirksame Investitionsänderung des WORST-Falls.</summary>
    public double WorstInvestitionAenderung
    {
        get => W?.InvestWirksam ?? 0;
        set => Worst(s => s.InvestitionAenderung = value);
    }

    /// <summary>Die wirksame Ertragsänderung des BEST-Falls.</summary>
    public double BestErtragAenderung
    {
        get => B?.ErtragWirksam ?? 0;
        set => Best(s => s.ErtragAenderung = value);
    }

    /// <summary>Die wirksame Ertragsänderung des WORST-Falls.</summary>
    public double WorstErtragAenderung
    {
        get => W?.ErtragWirksam ?? 0;
        set => Worst(s => s.ErtragAenderung = value);
    }

    /// <summary>Die wirksame Nutzungsdaueränderung des BEST-Falls.</summary>
    public double BestNutzungsdauerAenderung
    {
        get => B?.DauerWirksam ?? 0;
        set => Best(s => s.NutzungsdauerAenderung = value);
    }

    /// <summary>Die wirksame Nutzungsdaueränderung des WORST-Falls.</summary>
    public double WorstNutzungsdauerAenderung
    {
        get => W?.DauerWirksam ?? 0;
        set => Worst(s => s.NutzungsdauerAenderung = value);
    }

    // =====================================================================
    //  ETAPPE E9b — Zeilen 8 und 9 der Szenariotafel
    // =====================================================================
    //
    // Betrachtungszeitraum und Mengenänderung je Szenario haben KEINE Vorgabe (E9a-Q5):
    // leer heißt „wie Erwartet". Anders als die vierzehn Felder darüber tragen sie
    // deshalb den GEPFLEGTEN Wert — wie die Maske, deren Felder leer bleiben, solange
    // nichts gepflegt ist; der Platzhalter dort nennt den Erwartungswert.

    /// <summary>ETAPPE E9b: der gepflegte Betrachtungszeitraum des BEST-Falls [a]; leer = wie Erwartet.</summary>
    public int? BestZeitraum
    {
        get => B?.Zeitraum;
        set => Best(s => s.Zeitraum = value);
    }

    /// <summary>ETAPPE E9b: der gepflegte Betrachtungszeitraum des WORST-Falls [a]; leer = wie Erwartet.</summary>
    public int? WorstZeitraum
    {
        get => W?.Zeitraum;
        set => Worst(s => s.Zeitraum = value);
    }

    /// <summary>ETAPPE E9b: die gepflegte Mengenänderung des BEST-Falls [%]; leer = wie Erwartet.</summary>
    public double? BestMenge
    {
        get => B?.Menge;
        set => Best(s => s.Menge = value);
    }

    /// <summary>ETAPPE E9b: die gepflegte Mengenänderung des WORST-Falls [%]; leer = wie Erwartet.</summary>
    public double? WorstMenge
    {
        get => W?.Menge;
        set => Worst(s => s.Menge = value);
    }
}
