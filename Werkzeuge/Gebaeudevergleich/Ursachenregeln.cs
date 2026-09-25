using System;
using System.Collections.Generic;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>Die Eingänge der Ursachenregeln — alles, was eine Regel liest, und nichts sonst.</summary>
    internal sealed class Regeleingang
    {
        internal bool AltOk = true;
        internal bool NeuOk = true;
        internal bool IstFlaeche = true;
        internal bool HatZone;
        internal bool FesteNennleistung;
        internal bool KuehlungWirksam;
        internal bool HeizkreisAktiv;
        internal bool IstNwg;
        internal bool SpalteTagesbilanz;

        /// <summary>Δ Jahreswärme neu gegen alt [%].</summary>
        internal double DeltaJahrProzent = double.NaN;

        /// <summary>Δ Spitze der Stunde neu gegen alt [%].</summary>
        internal double DeltaSpitzeProzent = double.NaN;

        /// <summary>Katalogtreffer des neuen Wegs [%]; <c>null</c> = kein Katalogwert oder Verbrauchsangabe.</summary>
        internal double? KatalogtrefferNeuProzent;

        internal double NachtanteilAltProzent = double.NaN;
        internal double NachtanteilNeuProzent = double.NaN;
        internal double? FensteranteilProzent;
        internal double? BauweiseJeM2;
        internal double? InnereGewinneWm2;
        internal double AbsenkungK;
    }

    /// <summary>
    /// <b>Die Ursachenregeln</b> — reine Funktionen: Merkmale und Kennzahlen hinein, Codes und
    /// Ampel heraus. Jede Schwelle ist eine benannte Konstante mit ihrer Quelle; eine Schwelle
    /// wird nicht gebogen, damit eine Zeile grün wird (Auftrag T4: „Bänder nicht verbreitern").
    ///
    /// <para><b>Die Ampel.</b> <see cref="Ampel.Fehler"/>: ein Weg gescheitert, ein Wert
    /// unplausibel oder U‑BW (Vermerk „Datenfehler"). <see cref="Ampel.Erklaert"/>: U‑E8 mit
    /// |Δ Jahr| ≤ <see cref="BAND_E8_PROZENT"/>, oder Flächenangabe mit Δ Jahr in
    /// [<see cref="MW_UNTEN_PROZENT"/>; <see cref="MW_OBEN_PROZENT"/>] und entweder ohne
    /// Katalogwert oder mit einem Katalogtreffer in [<see cref="KAT_UNTEN_PROZENT"/>;
    /// <see cref="KAT_OBEN_PROZENT"/>]. <see cref="Ampel.ZuPruefen"/>: alles andere. Die
    /// Spitzenregeln und U‑KU verschieben die Ampel nicht.</para>
    /// </summary>
    internal static class Ursachenregeln
    {
        // ---- Codes, in der Reihenfolge der Ausgabe ---------------------------------------
        internal const string U_E8 = "U-E8";
        internal const string U_E8Z = "U-E8Z";
        internal const string U_NN = "U-NN";
        internal const string U_MW = "U-MW";
        internal const string U_KAT = "U-KAT";
        internal const string U_SP = "U-SP";
        internal const string U_NG = "U-NG";
        internal const string U_SOL = "U-SOL";
        internal const string U_BW = "U-BW";
        internal const string U_IL = "U-IL";
        internal const string U_ZO = "U-ZO";
        internal const string U_AK = "U-AK";
        internal const string U_KU = "U-KU";
        internal const string U_TB = "U-TB";

        internal static readonly string[] ALLE =
            { U_E8, U_E8Z, U_NN, U_MW, U_KAT, U_SP, U_NG, U_SOL, U_BW, U_IL, U_ZO, U_AK, U_KU, U_TB };

        internal const string VERMERK_DATENFEHLER = "Datenfehler";

        // ---- Schwellen -------------------------------------------------------------------

        /// <summary>
        /// Band E8 [%]: |Δ Jahr| bei Verbrauchsangabe ohne Zone und ohne feste Nennleistung. Beide
        /// Wege rechnen auf den angegebenen Verbrauch zurück — der Tagesbilanz-Weg streckt linear
        /// (<c>TagesbilanzPhysik.cs:224</c>, zweiter Aufruf in <c>SimulationWaermebedarf</c>), der
        /// VDI-Weg multipliziert nach (<c>EinLaufMitNachmultiplikation</c>, E8). Quelle der Zahl:
        /// die Messung an Projekt 1009 der Testdatenbank (Probe T3b: 75,000 MWh auf beiden Wegen,
        /// |Δ Jahr| = 0,000 %, Verbrauchstreffer je 100,0 %) plus ein Rand von 0,1 Prozentpunkten;
        /// der Auftrag erlaubt höchstens 2 %.
        /// </summary>
        internal const double BAND_E8_PROZENT = 0.1;

        /// <summary>
        /// Modellwechsel [%]: Δ Jahr in [+5; +50] bei Flächenangabe — der Abstand, den der Wechsel
        /// vom Tagesbilanz- zum Stundenmodell an den Referenzprojekten erklärt (Konzept
        /// Gebäudesimulation 5.10; Wechsel R11→R12 in <c>ueberholt/Referenzbasen/LIESMICH.md</c>).
        /// </summary>
        internal const double MW_UNTEN_PROZENT = 5.0;

        /// <inheritdoc cref="MW_UNTEN_PROZENT"/>
        internal const double MW_OBEN_PROZENT = 50.0;

        /// <summary>
        /// Katalogtreffer [%]: neu / Katalogwert in [90; 115] bestätigt den neuen Weg (Konzept
        /// Gebäudesimulation 10.4, positives Abnahmekriterium).
        /// </summary>
        internal const double KAT_UNTEN_PROZENT = 90.0;

        /// <inheritdoc cref="KAT_UNTEN_PROZENT"/>
        internal const double KAT_OBEN_PROZENT = 115.0;

        /// <summary>Aufheizspitze [Prozentpunkte]: Δ Spitze − Δ Jahr ab 15 bei Nachtabsenkung.</summary>
        internal const double SP_PROZENTPUNKTE = 15.0;

        /// <summary>Nachtgrundlast [Prozentpunkte]: Nachtanteil neu − alt ab 5.</summary>
        internal const double NG_PROZENTPUNKTE = 5.0;

        /// <summary>Solareintrag [%]: Fensteranteil (Fenster / (Außenwand + Fenster)) ab 30.</summary>
        internal const double SOL_FENSTERANTEIL_PROZENT = 30.0;

        /// <summary>
        /// Bauweise je m² Nutzfläche [Wh/(m²K)]: die Plausibilitätsgrenzen des VDI-Wegs
        /// (<c>GebaeudeFestwerte.BAUWEISE_JE_M2_MIN/MAX</c>, Konzept 4.8; geprüft in
        /// <c>ErsatzparameterRC.AusKlassenweg</c>). 50 Wh/K ist der stille Rückfallwert des
        /// Bestands (Konzept 5.11).
        /// </summary>
        internal const double BW_MIN_WH_M2K = GebaeudeFestwerte.BAUWEISE_JE_M2_MIN;

        /// <inheritdoc cref="BW_MIN_WH_M2K"/>
        internal const double BW_MAX_WH_M2K = GebaeudeFestwerte.BAUWEISE_JE_M2_MAX;

        /// <summary>U‑BW greift auch innerhalb der Grenzen, wenn der Wert weniger als 10 % vom Rand entfernt ist.</summary>
        internal const double BW_RANDABSTAND = 0.10;

        /// <summary>
        /// Innere Lasten [W/m² Nutzfläche], unauffälliges Band: Wohnnutzung nach DIN V 18599-10
        /// (45 bzw. 90 Wh/(m²d), also rund 1,9 bis 3,8 W/m²) bis zum Pauschalwert 5 W/m² der
        /// DIN V 4108-6; darunter und darüber gilt die Last als auffällig (U‑IL).
        /// </summary>
        internal const double IL_MIN_W_M2 = 1.5;

        /// <inheritdoc cref="IL_MIN_W_M2"/>
        internal const double IL_MAX_W_M2 = 5.0;

        // ---- Regeln ----------------------------------------------------------------------

        internal static bool E8(Regeleingang e) => !e.IstFlaeche && !e.HatZone && !e.FesteNennleistung;
        internal static bool E8Z(Regeleingang e) => !e.IstFlaeche && e.HatZone;
        internal static bool NN(Regeleingang e) => !e.IstFlaeche && e.FesteNennleistung;

        internal static bool MW(Regeleingang e)
            => e.IstFlaeche && Im(e.DeltaJahrProzent, MW_UNTEN_PROZENT, MW_OBEN_PROZENT);

        internal static bool KAT(Regeleingang e)
            => e.IstFlaeche && e.KatalogtrefferNeuProzent.HasValue
               && Im(e.KatalogtrefferNeuProzent.Value, KAT_UNTEN_PROZENT, KAT_OBEN_PROZENT);

        internal static bool SP(Regeleingang e)
            => Kennzahlen.Endlich(e.DeltaSpitzeProzent) && Kennzahlen.Endlich(e.DeltaJahrProzent)
               && e.DeltaSpitzeProzent - e.DeltaJahrProzent >= SP_PROZENTPUNKTE && e.AbsenkungK > 0.0;

        internal static bool NG(Regeleingang e)
            => Kennzahlen.Endlich(e.NachtanteilAltProzent) && Kennzahlen.Endlich(e.NachtanteilNeuProzent)
               && e.NachtanteilNeuProzent - e.NachtanteilAltProzent >= NG_PROZENTPUNKTE;

        internal static bool SOL(Regeleingang e)
            => e.FensteranteilProzent.HasValue && e.FensteranteilProzent.Value >= SOL_FENSTERANTEIL_PROZENT;

        internal static bool BW(Regeleingang e)
        {
            if (!e.BauweiseJeM2.HasValue) return false;
            double b = e.BauweiseJeM2.Value;
            if (!Kennzahlen.Endlich(b)) return true;
            return b < BW_MIN_WH_M2K * (1.0 + BW_RANDABSTAND) || b > BW_MAX_WH_M2K * (1.0 - BW_RANDABSTAND);
        }

        internal static bool IL(Regeleingang e)
            => e.IstNwg || (e.InnereGewinneWm2.HasValue
                            && (e.InnereGewinneWm2.Value < IL_MIN_W_M2 || e.InnereGewinneWm2.Value > IL_MAX_W_M2));

        /// <summary>Alle Regeln und die Ampel.</summary>
        internal static Regelbefund Anwenden(Regeleingang e)
        {
            var b = new Regelbefund();
            void Wenn(bool gilt, string code) { if (gilt) b.Codes.Add(code); }

            Wenn(E8(e), U_E8);
            Wenn(E8Z(e), U_E8Z);
            Wenn(NN(e), U_NN);
            Wenn(MW(e), U_MW);
            Wenn(KAT(e), U_KAT);
            Wenn(SP(e), U_SP);
            Wenn(NG(e), U_NG);
            Wenn(SOL(e), U_SOL);
            Wenn(BW(e), U_BW);
            Wenn(IL(e), U_IL);
            Wenn(e.HatZone, U_ZO);
            Wenn(e.HeizkreisAktiv, U_AK);
            Wenn(e.KuehlungWirksam, U_KU);
            Wenn(e.SpalteTagesbilanz, U_TB);

            b.Ampel = AmpelBilden(e, b.Codes);
            if (b.Codes.Contains(U_BW)) b.Vermerk = VERMERK_DATENFEHLER;
            return b;
        }

        private static Ampel AmpelBilden(Regeleingang e, List<string> codes)
        {
            if (!e.AltOk || !e.NeuOk || !Kennzahlen.Endlich(e.DeltaJahrProzent)) return Ampel.Fehler;
            if (codes.Contains(U_BW)) return Ampel.Fehler;

            if (codes.Contains(U_E8) && Math.Abs(e.DeltaJahrProzent) <= BAND_E8_PROZENT) return Ampel.Erklaert;

            if (e.IstFlaeche && Im(e.DeltaJahrProzent, MW_UNTEN_PROZENT, MW_OBEN_PROZENT)
                && (!e.KatalogtrefferNeuProzent.HasValue || codes.Contains(U_KAT)))
                return Ampel.Erklaert;

            return Ampel.ZuPruefen;
        }

        private static bool Im(double w, double unten, double oben)
            => Kennzahlen.Endlich(w) && w >= unten && w <= oben;

        /// <summary>Der Vorschlag zu einem Code — der Text der Regel für Bericht und Zusammenfassung.</summary>
        internal static string Vorschlag(string code) => code switch
        {
            U_E8 => "Verbrauchsangabe: beide Wege sind auf den Verbrauch zurückgerechnet (linear gestreckt bzw. nachmultipliziert, E8); erwartet |Δ Jahr| im Band E8, anders sind nur Form und Spitze",
            U_E8Z => "Verbrauchsangabe mit Zone: keine Rückrechnung auf den Verbrauch - zu prüfen",
            U_NN => "Verbrauchsangabe mit fester Nennleistung: zwei Läufe, die Abweichung vom Verbrauch wird benannt - zu prüfen",
            U_MW => "Modellwechsel Tagesbilanz → Stundenmodell (Konzept 5.10)",
            U_KAT => "Katalogtreffer des neuen Wegs bestätigt (90–115 %, Konzept 10.4)",
            U_SP => "Aufheizspitze des idealen Heizers nach der Nachtabsenkung",
            U_NG => "physikalische Nachtgrundlast des Stundenmodells",
            U_SOL => "stündlicher Solareintrag, zwei Kapazitäten",
            U_BW => "Bauweise je m² Nutzfläche am oder jenseits des Plausibilitätsrands - Datenfehler (Konzept 5.11: 50 Wh/K ist der stille Rückfallwert)",
            U_IL => "Nutzungsprofil: Nichtwohngebäude oder auffällige innere Lasten",
            U_ZO => "Zone: nur der VDI-Weg rechnet sie",
            U_AK => "Heizkreis aktiv: nur der VDI-Weg rechnet die Anlagenkopplung",
            U_KU => "Kühlung wirksam: der VDI-Weg rechnet Kälte, der Tagesbilanz-Weg hat keine Kühlreihe; Kälte ist ausgewiesen",
            U_TB => "Spalte Gebaeude_Modell = TAGESBILANZ: rechnet heute auf dem Bestandsweg",
            _ => "",
        };

        /// <summary>Anzeigetext der Ampel.</summary>
        internal static string Text(Ampel a) => a switch
        {
            Ampel.Erklaert => "erklärt",
            Ampel.ZuPruefen => "zu prüfen",
            _ => "Fehler",
        };
    }
}
