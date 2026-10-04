using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Zonen eines Puffers (Konzept 3.2).</summary>
    public enum PufferZone
    {
        Heizung,
        Brauchwasser,
        Prozess
    }

    /// <summary>Die Stufe einer Warnzeile (Konzept 3.6) — Hinweise und Warnungen, keine Sperren.</summary>
    public enum PufferStufe
    {
        Hinweis,
        Warnung
    }

    /// <summary>
    /// Die Codes der Warnliste (Konzept 3.6, Präfix <c>PA-</c>). Der Ressourcenschlüssel der Oberfläche
    /// (Stufe P2) entsteht aus dem Code (<see cref="PufferWarnung.Ressourcenschluessel"/>).
    /// </summary>
    public static class PufferWarncode
    {
        public const string KEIN_PUFFER = "PA-KEIN-PUFFER";
        public const string BAND_UNTER = "PA-BAND-UNTER";
        public const string BAND_UEBER = "PA-BAND-UEBER";
        public const string ABTAU_VORRANG = "PA-ABTAU-VORRANG";
        public const string STARTS_TAG = "PA-STARTS-TAG";
        public const string STARTS_JAHR = "PA-STARTS-JAHR";
        public const string PRAXISGRENZE = "PA-PRAXISGRENZE";
        public const string EXTRAPOLATION = "PA-EXTRAPOLATION";
        public const string TANK_IM_TANK = "PA-TANK-IM-TANK";
        public const string OHNE_PUFFER_GEREGELT = "PA-OHNE-PUFFER-GEREGELT";
        public const string HYGIENE_W551 = "PA-HYGIENE-W551";
        public const string HYGIENE_TEMPERATUR = "PA-HYGIENE-TEMPERATUR";
        public const string BW_UEBERDIMENSIONIERT = "PA-BW-UEBERDIMENSIONIERT";
        public const string UEBERGABE_UNBEKANNT = "PA-UEBERGABE-UNBEKANNT";
        public const string ZWEITERZEUGER_FREI = "PA-ZWEITERZEUGER-FREI";
        public const string HEIZSTAB_GESPERRT = "PA-HEIZSTAB-GESPERRT";
        public const string KEINE_REIHE = "PA-KEINE-REIHE";
        /// <summary>
        /// Hinweis des Abgleichs mit der Jahressimulation (Welle P4b): Die Starts je Tag des Probelaufs
        /// weichen um mehr als 30 % von der Zweipunktschätzung D2 ab. Kein Code der Auslegungsliste
        /// (<see cref="ALLE"/>) — er entsteht erst mit einem Lauf.
        /// </summary>
        public const string STARTS_ABWEICHUNG = "PA-STARTS-ABWEICHUNG";
        /// <summary>Der Prozessvorlauf liegt über 95 °C oder über dem Vorlauf aller Erzeuger an der Kaskade.</summary>
        public const string PROZESS_TEMPERATUR = "PA-PROZESS-TEMPERATUR";
        /// <summary>Das Aufheizkriterium K12 ist eingeschaltet, aber keine KP3-Bemessung der Gebäude liegt vor.</summary>
        public const string AUFHEIZ_KEINE_BEMESSUNG = "PA-AUFHEIZ-KEINE-BEMESSUNG";

        /// <summary>Alle Codes der Liste.</summary>
        public static readonly IReadOnlyList<string> ALLE = new[]
        {
            KEIN_PUFFER, BAND_UNTER, BAND_UEBER, ABTAU_VORRANG, STARTS_TAG, STARTS_JAHR, PRAXISGRENZE,
            EXTRAPOLATION, TANK_IM_TANK, OHNE_PUFFER_GEREGELT, HYGIENE_W551, HYGIENE_TEMPERATUR,
            BW_UEBERDIMENSIONIERT, UEBERGABE_UNBEKANNT, ZWEITERZEUGER_FREI, HEIZSTAB_GESPERRT, KEINE_REIHE,
            PROZESS_TEMPERATUR, AUFHEIZ_KEINE_BEMESSUNG
        };
    }

    /// <summary>
    /// Eine Zeile der Warnliste: Code, Stufe, deutscher Klartext mit Zahlen (der Ressourcentext steht unter
    /// <see cref="Ressourcenschluessel"/>), Herkunft als <see cref="Textbaustein"/> und Zone.
    /// </summary>
    public sealed record PufferWarnung(string Code, PufferStufe Stufe, string Text, Textbaustein HerkunftBaustein, PufferZone? Zone)
    {
        /// <summary>Eine Warnung mit einer Herkunft als Klartext (ohne Ressourcenschlüssel).</summary>
        public PufferWarnung(string code, PufferStufe stufe, string text, string herkunft, PufferZone? zone)
            : this(code, stufe, text, Textbaustein.Klar(herkunft), zone)
        {
        }

        /// <summary>Eine Warnung mit dem Klartext als <see cref="Textbaustein"/> (Schlüssel <c>PA_&lt;CODE&gt;_TEXT</c>, Zahlen als Argumente).</summary>
        public PufferWarnung(string code, PufferStufe stufe, Textbaustein text, Textbaustein herkunft, PufferZone? zone)
            : this(code, stufe, text?.Klartext ?? "", herkunft, zone)
        {
            TextBaustein = text;
        }

        /// <summary>Der Ressourcenschlüssel des Texts: <c>PA-KEIN-PUFFER</c> → <c>PA_KEIN_PUFFER</c>.</summary>
        public string Ressourcenschluessel => Code.Replace('-', '_');

        /// <summary>
        /// Der Klartext mit Zahlen als <see cref="Textbaustein"/> (<c>PA_&lt;CODE&gt;_TEXT</c>); die Auflösung formatiert
        /// die Zahlen in der Sprache der Ansicht bzw. des Berichts. Ohne Baustein: <see cref="Text"/> als Klartext.
        /// </summary>
        public Textbaustein TextBaustein { get; init; }

        /// <summary>Der Klartext als Baustein — <see cref="TextBaustein"/> oder <see cref="Text"/> als Klartext.</summary>
        public Textbaustein KlartextBaustein => TextBaustein ?? Textbaustein.Klar(Text);

        /// <summary>Die Herkunft als deutscher Klartext.</summary>
        public string Herkunft => HerkunftBaustein?.Klartext ?? "";
    }

    /// <summary>Die Kennungen der Kriterien (Konzept 3.3).</summary>
    public static class PufferKriteriumKennung
    {
        public const string K1 = "K1";
        public const string K2 = "K2";
        public const string K3 = "K3";
        public const string K4 = "K4";
        public const string K4E = "K4e";
        public const string D1 = "D1";
        public const string D2 = "D2";
        public const string K9 = "K9";
        public const string K9E = "K9e";
        public const string K10 = "K10";
        /// <summary>Verschiebedauer des BHKW (Schlüssel <c>BHKW.Verschiebedauer_h</c>).</summary>
        public const string KV = "KV";
        /// <summary>Aufheizen nach Absenkung (V30, KP3): Puffer deckt Φ_n − P_gen über die Aufheizdauer.</summary>
        public const string K12 = "K12";
        /// <summary>Brauchwasserzone aus dem Zapfprofil (Nenninhalt des Trinkwasserspeichers).</summary>
        public const string B_SPEICHER = "B-Speicher";
        /// <summary>Brauchwasserzone für Frischwasser-/Wohnungsstation (Konzept 3.2).</summary>
        public const string B_FRISCHWASSER = "B-Frischwasser";
    }

    /// <summary>
    /// Ein Kriterium einer Zone: Volumen [l] mit Herkunft und Rechenweg. <see cref="Aktiv"/> = von der
    /// Vorlage bzw. dem Weg eingeschaltet und damit Kandidat der Bemessung; <see cref="Gueltig"/> = der
    /// Wert ist berechenbar und erreicht sein Ziel.
    /// </summary>
    public sealed record PufferKriterium
    {
        public string Kennung { get; init; }
        public string Bezeichnung { get; init; }
        /// <summary>Volumen [l]; <c>null</c> = nicht berechenbar.</summary>
        public double? VolumenL { get; init; }
        public bool Aktiv { get; init; }
        public bool Gueltig { get; init; } = true;
        /// <summary>Enthält der Wert schon den nutzbaren Anteil η_s (dann nicht erneut teilen)?</summary>
        public bool EnthaeltNutzanteil { get; init; }
        /// <summary>Die Herkunft (Norm, Studie, Tool, Setzung) als Zitat — Ressourcenschlüssel <c>PAUS_HERK_*</c>.</summary>
        public Textbaustein HerkunftBaustein { get; init; } = Textbaustein.Leer;
        /// <summary>Der Rechenweg mit den eingesetzten Zahlen — Ressourcenschlüssel <c>PAUS_WEG_*</c> mit Argumenten.</summary>
        public Textbaustein RechenwegBaustein { get; init; } = Textbaustein.Leer;
        /// <summary>Die Herkunft als deutscher Klartext.</summary>
        public string Herkunft => HerkunftBaustein?.Klartext ?? "";
        /// <summary>Der Rechenweg als deutscher Klartext.</summary>
        public string Rechenweg => RechenwegBaustein?.Klartext ?? "";
    }

    /// <summary>Das Betriebsbild einer Zweipunktsimulation (D2) als Kennzahlen.</summary>
    public sealed record PufferBetriebsbild
    {
        /// <summary>Volumen [l], mit dem simuliert wurde.</summary>
        public double VolumenL { get; init; }
        /// <summary>Starts insgesamt.</summary>
        public int Starts { get; init; }
        /// <summary>Starts in Stunden mit Bedarf (Heizperiode = Stunden mit Heizbedarf &gt; 0).</summary>
        public int StartsHeizperiode { get; init; }
        /// <summary>Stunden mit Bedarf &gt; 0.</summary>
        public int Heizstunden { get; init; }
        /// <summary>Starts je Tag der Heizperiode: Starts der Heizperiode ÷ (Heizstunden ÷ 24).</summary>
        public double StartsJeTag { get; init; }
        /// <summary>Stunden im Ladebetrieb.</summary>
        public int Laufstunden { get; init; }
        /// <summary>Mittlere Laufzeit je Start [h].</summary>
        public double MittlereLaufzeitH { get; init; }
        /// <summary>Deckungsgrad der Simulation.</summary>
        public double Deckungsgrad { get; init; }
    }

    /// <summary>Der Bereitschaftsverlust (K11) in drei Darstellungen (Konzept 3.3, Runde 3 Abschnitt 5.4).</summary>
    public sealed record PufferVerlust
    {
        /// <summary>Volumen [l], für das der Verlust gilt.</summary>
        public double VolumenL { get; init; }
        /// <summary>Bereitschaftsverlust [kWh/d] bei der Prüfspreizung (45 K).</summary>
        public double KwhJeTag { get; init; }
        /// <summary>Wärmeverlustrate [W/K] = Q_B · 1000 / (24 · 45).</summary>
        public double WJeK { get; init; }
        /// <summary>Betriebsverlust [kWh/a] = Q_B · (ϑ_mittel − ϑ_Raum) / 45 · 365.</summary>
        public double KwhJeJahr { get; init; }
        /// <summary>Betriebsfaktor (ϑ_mittel − ϑ_Raum) / 45.</summary>
        public double Betriebsfaktor { get; init; }
        /// <summary>Aus dem Katalogsatz (sonst Klasse-C-Grenze).</summary>
        public bool AusKatalog { get; init; }
        /// <summary>Klasse-C-Formel über ihrem Geltungsbereich (2 000 l) angewandt.</summary>
        public bool Extrapoliert { get; init; }
        /// <summary>Die Herkunft (Katalogsatz oder Klasse-C-Grenze) als <see cref="Textbaustein"/>.</summary>
        public Textbaustein HerkunftBaustein { get; init; } = Textbaustein.Leer;
        /// <summary>Die Herkunft als deutscher Klartext.</summary>
        public string Herkunft => HerkunftBaustein?.Klartext ?? "";
    }

    /// <summary>Das Ergebnis einer Zone.</summary>
    public sealed record PufferZonenergebnis
    {
        public PufferZone Zone { get; init; }
        public IReadOnlyList<PufferKriterium> Kriterien { get; init; } = Array.Empty<PufferKriterium>();
        /// <summary>Kennung des bemessenden Kriteriums; <c>null</c> = keines (Volumen 0).</summary>
        public string Bemessend { get; init; }
        /// <summary>Volumen der Zone [l] (bemessender Wert).</summary>
        public double VolumenL { get; init; }
        /// <summary>Die Vorprüfung K1 sagt „kein Puffer erforderlich“.</summary>
        public bool KeinPuffer { get; init; }
        /// <summary>Betriebsbild mit dem Zonenvolumen (Heiz- und Prozesszone); <c>null</c> = ohne Reihe.</summary>
        public PufferBetriebsbild Betriebsbild { get; init; }

        /// <summary>Das Kriterium mit der Kennung; <c>null</c>, wenn die Zone es nicht führt.</summary>
        public PufferKriterium Kriterium(string kennung)
        {
            foreach (PufferKriterium k in Kriterien) if (k.Kennung == kennung) return k;
            return null;
        }
    }

    /// <summary>Die Kennzahlen der Auslegung (Konzept 3.3–3.5).</summary>
    public sealed record PufferKennzahlen
    {
        /// <summary>Nutzbarer Anteil η_s = s_aus − s_ein.</summary>
        public double Nutzanteil { get; init; }
        /// <summary>Spreizung ϑ_VL − ϑ_RL [K].</summary>
        public double SpreizungK { get; init; }
        /// <summary>Auslegungsheizlast [kW], mit der die Heizzone rechnete.</summary>
        public double AuslegungsheizlastKw { get; init; }
        /// <summary>Anlagenvolumen [l], mit dem K1/K4 rechneten.</summary>
        public double AnlagenvolumenL { get; init; }
        /// <summary>Plausibilitätsband nach Übergabeart [l] (Heizlast); <c>null</c> = ohne Heizzone.</summary>
        public double? BandMinL { get; init; }
        public double? BandMaxL { get; init; }
        /// <summary>Plausibilitätsband nach Wärmepumpenleistung [l]; <c>null</c> = keine Wärmepumpe.</summary>
        public double? BandWpMinL { get; init; }
        public double? BandWpMaxL { get; init; }
        /// <summary>Nutzbare Kapazität der Empfehlung [kWh] = V · 1,16 · ΔT · η_s / 1000.</summary>
        public double KapazitaetKwh { get; init; }
        /// <summary>Bereitschaftsverlust der Empfehlung.</summary>
        public PufferVerlust Verlust { get; init; }
        /// <summary>Tagesbedarf je Person [l] bei 60 °C; <c>null</c> = unbekannt.</summary>
        public double? LiterJePerson { get; init; }
        /// <summary>Liegt der Tagesbedarf je Person im Band 28–50 l?</summary>
        public bool? LiterJePersonImBand { get; init; }
        /// <summary>Zirkulationsverlust [kWh/d], mit dem die Brauchwasserzone rechnete.</summary>
        public double? ZirkulationKwhD { get; init; }
        /// <summary>Zonenanteil Heizung V_H/(V_H + V_B) beim Kombipuffer; <c>null</c> = kein Kombipuffer.</summary>
        public double? ZonenanteilHeizung { get; init; }
        /// <summary>Mindestzahl der Schichten (≥ 2 beim Kombipuffer, sonst 1).</summary>
        public int SchichtenMindest { get; init; } = 1;
        /// <summary>Faustwert der Vorlage als Gegenprobe [l]; <c>null</c> = keiner.</summary>
        public double? FaustwertGegenprobeL { get; init; }
    }

    /// <summary>
    /// Das Ergebnis der Pufferspeicher-Auslegung (Konzept 3.4, 5): je Zone die Kriterien mit Herkunft
    /// und das bemessende Kriterium, die Summe, die Empfehlung (gerundet auf die Nenninhalte, an der
    /// Praxisgrenze gestoppt), der Katalogvorschlag, die Kennzahlen und die Warnliste.
    /// </summary>
    public sealed record PufferAuslegungErgebnis
    {
        public IReadOnlyList<PufferZonenergebnis> Zonen { get; init; } = Array.Empty<PufferZonenergebnis>();
        /// <summary>Summe der Zonen [l], ungerundet.</summary>
        public double SummeL { get; init; }
        /// <summary>Empfohlener Nenninhalt [l]; 0 = kein Puffer.</summary>
        public double EmpfehlungL { get; init; }
        /// <summary>Die Empfehlung liegt über dem Ende der Nenninhaltsliste (Raster).</summary>
        public bool UeberListenende { get; init; }
        /// <summary>Die Summe überschreitet die Praxisgrenze; die Empfehlung steht auf ihr.</summary>
        public bool AnPraxisgrenze { get; init; }
        /// <summary>Zone und Kriterium, die die größte Zone bemessen (z. B. „Heizung: K4“).</summary>
        public string Bemessend { get; init; }
        /// <summary>Kleinster Katalogpuffer ≥ Summe (Kombispeicher bei Heizung + Brauchwasser); <c>null</c> = keiner.</summary>
        public PufferKatalogsatz Katalogvorschlag { get; init; }
        public PufferKennzahlen Kennzahlen { get; init; } = new PufferKennzahlen();
        public IReadOnlyList<PufferWarnung> Warnungen { get; init; } = Array.Empty<PufferWarnung>();

        /// <summary>Die Zone; <c>null</c>, wenn sie nicht gerechnet wurde.</summary>
        public PufferZonenergebnis Zone(PufferZone zone)
        {
            foreach (PufferZonenergebnis z in Zonen) if (z.Zone == zone) return z;
            return null;
        }

        /// <summary>Trägt die Warnliste den Code?</summary>
        public bool HatWarnung(string code)
        {
            foreach (PufferWarnung w in Warnungen) if (w.Code == code) return true;
            return false;
        }
    }
}
