using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>Die Ampel eines Kriteriums und des ganzen Objekts.</summary>
    internal enum Ampel
    {
        /// <summary>Erfüllt.</summary>
        Gruen = 0,

        /// <summary>Nicht entschieden — die Kennzahl ist nicht bildbar (kein Ensemble, kein voller Tag).</summary>
        Gelb = 1,

        /// <summary>Verletzt.</summary>
        Rot = 2
    }

    /// <summary>Ein Kriterium der Abnahme: Name, Ampel, das Maß als Verhältniszahl und die Schranke.</summary>
    internal sealed record Kriterium(string Name, Ampel Ampel, double? Mass, string Schranke, string Satz);

    /// <summary>Die Form eines Tagtyps im Bericht — Anteile, Tage und die mittlere Abweichung.</summary>
    internal sealed record Formzeile(string Tagtyp, int TageGemessen, int TageGerechnet,
                                     double MittlereAbweichung, double VerschobenerAnteil, bool ImRahmen);

    /// <summary>Der Kalibriervorschlag einer Nichtwohn-Zone — <b>nur als Verhältnis und Anteile</b>.</summary>
    internal sealed record Vorschlagszeilen(double? TagesbedarfVerhaeltnis, int VolleTage,
                                            IReadOnlyList<double> Wochenfaktoren,
                                            IReadOnlyList<Formzeile> Tagesgaenge);

    /// <summary>
    /// <b>Der Befund eines Messobjekts</b> — was in den Bericht geht. Er führt
    /// <b>ausschließlich Verhältniszahlen, Anteile und Zählungen</b>: keine gemessene Menge, keine
    /// gemessene Leistung, keinen Objektnamen außer der anonymen Kennung (Konzept Kapitel 9 K5).
    /// Die Wache <see cref="Berichtswache"/> hält den geschriebenen Text dagegen.
    /// </summary>
    internal sealed class Objektbefund
    {
        /// <summary>Die anonyme Kennung des Objekts.</summary>
        internal string Kennung { get; init; } = "";

        /// <summary>Der Ordnername unter dem Quellordner — er ist Sache des Anwenders und steht nicht im Bericht.</summary>
        internal string Ordner { get; init; } = "";

        /// <summary>Die Nutzungsart des Katalogs.</summary>
        internal string Nutzungsart { get; set; } = "";

        internal ZapfBezugsart Bezugsart { get; set; }
        internal ZapfKalenderart Kalenderart { get; set; }

        /// <summary>Die Bezugsmenge der Zone — eine Eingabe des Anwenders, keine Messgröße.</summary>
        internal double Bezugsmenge { get; set; }

        /// <summary>N der √N-Skalierung (die gerundete Bezugsmenge).</summary>
        internal int Einheiten { get; set; }

        /// <summary>Woher die Bezugsmenge stammt; <c>null</c> = nicht angegeben.</summary>
        internal Bezugsmengenherkunft? Herkunft { get; set; }

        /// <summary>
        /// Trägt die Bezugsmenge die √N-Skalierung? Nein, wenn sie ausdrücklich ein Platzhalter oder
        /// unbekannt ist — eine gesetzte Zahl von Einheiten sagt dann nichts über die Größe des Objekts.
        /// Ohne Angabe ja (so rechnete das Werkzeug, bevor es die Herkunft kannte).
        /// </summary>
        internal bool EinheitenBelastbar => Herkunft != Bezugsmengenherkunft.Platzhalter
                                            && Herkunft != Bezugsmengenherkunft.Unbekannt;

        internal ZapfBilanzgrenze Grenze { get; set; }
        internal bool Stochastisch { get; set; }
        internal int Realisierungen { get; set; }
        internal int Seed { get; set; }

        /// <summary>Die Auflösung der Messreihe [min] und ihre Länge [d] — Kopfangaben, keine Mengen.</summary>
        internal int AufloesungMin { get; set; }
        internal double MesstageGesamt { get; set; }
        internal double Lueckenanteil { get; set; }
        internal int Schalttage { get; set; }

        /// <summary>Anteil des Jahres, den die Reihe abdeckt (1 = volles Jahr).</summary>
        internal double Jahresanteil { get; set; }

        /// <summary>Ist die Reihe ein Teiljahr? Dann ist der Jahresmesswert hochgerechnet.</summary>
        internal bool Teiljahr => Jahresanteil < 1.0 - 1e-9;

        // --- die fünf Kennzahlen (a) bis (e) -------------------------------------------
        internal double? EnergieVerhaeltnis { get; set; }
        internal double? EnergieAbweichung { get; set; }
        internal double? Spitzenverhaeltnis { get; set; }
        internal double? BandUnten { get; set; }
        internal double? BandOben { get; set; }
        internal double PerzentilUnten { get; set; }
        internal double PerzentilOben { get; set; }
        internal Spitzenlage Lage { get; set; }
        internal int Dauerlinienwerte { get; set; }
        internal double? StreuungUnten { get; set; }
        internal double? StreuungOben { get; set; }
        internal double? Streubreite { get; set; }
        internal int StreuungRealisierungen { get; set; }
        internal double? WurzelNVerhaeltnis { get; set; }
        internal double? Skalierungsmass { get; set; }
        internal double? Formmass { get; set; }

        // --- Analyse „Band je Größenklasse" (Bandanalyse; keine Ampel) ------------------

        /// <summary>
        /// Das <b>Perzentil der Messspitze in der gerechneten Dauerlinie</b> [-]: der Anteil der
        /// gerechneten Stunden, die höchstens so groß sind wie die Messspitze. 1 heißt: Die Messspitze
        /// liegt auf oder über der größten gerechneten Stunde.
        /// </summary>
        internal double? MessspitzePerzentil { get; set; }

        /// <summary>Die kleinste und größte Jahresspitze des Ensembles, bezogen auf die verglichene Realisierung [-].</summary>
        internal double? EnsembleUnten { get; set; }
        internal double? EnsembleOben { get; set; }

        /// <summary>Der Anteil der Ensemblespitzen, die höchstens so groß sind wie die Messspitze [-].</summary>
        internal double? EnsembleAnteilDarunter { get; set; }
        internal double Formschwelle { get; set; }
        internal List<Formzeile> Form { get; } = new List<Formzeile>();
        internal double? MonateGroessteAbweichung { get; set; }
        internal int MonateGroessterMonat { get; set; }
        internal IReadOnlyList<double> MonatsanteileGemessen { get; set; } = new double[0];
        internal IReadOnlyList<double> MonatsanteileGerechnet { get; set; } = new double[0];

        // --- Kalibrierung ---------------------------------------------------------------
        /// <summary>Der Kalibrierfaktor — eine Verhältniszahl (Messwert / Rechnung).</summary>
        internal double? Kalibrierfaktor { get; set; }

        /// <summary>
        /// Das verbliebene relative Residuum der Jahresenergie nach der Kalibrierung; 0 = exakt.
        /// Es ist das Maß des vierten Abnahmekriteriums.
        /// </summary>
        internal double? EnergieResiduum { get; set; }

        /// <summary>Der Anteil der Zirkulation an der gerechneten Jahresmenge [-].</summary>
        internal double? Zirkulationsanteil { get; set; }

        /// <summary>
        /// Lief der Vergleich gegen die <b>kalibrierte</b> Jahresreihe? <c>false</c> heißt: Die
        /// Kalibrierung ist nicht gelaufen, verglichen wurde gegen die rohe Rechnung — dann tragen
        /// Band und Form auch den Niveaufehler der geschätzten Bezugsmenge.
        /// </summary>
        internal bool GegenKalibrierteReihe { get; set; }

        /// <summary>Der Kalibriervorschlag einer Nichtwohn-Zone; <c>null</c> = keiner.</summary>
        internal Vorschlagszeilen Vorschlag { get; set; }

        /// <summary>Die Sätze des Kerns und des Werkzeugs im Klartext.</summary>
        internal List<string> Hinweise { get; } = new List<string>();

        /// <summary>Der Grund, wenn das Objekt gar nicht auswertbar war; <c>null</c> = ausgewertet.</summary>
        internal string Abbruch { get; set; }

        /// <summary>
        /// Die Schreibweisen der Kennzahlen der Messung, die in keinem Bericht stehen dürfen
        /// (<see cref="Berichtswache.Verbotene"/>). Sie stehen <b>nur hier im Arbeitsspeicher</b> und
        /// gehen nie in eine Datei — die Wache braucht sie, um sie zu finden.
        /// </summary>
        internal List<string> Verbotene { get; } = new List<string>();

        /// <summary>Die vier Kriterien nach Kapitel 7 Zeile Z5, in der Reihenfolge des Papiers.</summary>
        internal List<Kriterium> Kriterien { get; } = new List<Kriterium>();

        /// <summary>Die Ampel des Objekts: die schlechteste seiner Kriterien; ein Abbruch ist rot.</summary>
        internal Ampel Gesamt => Abbruch != null ? Ampel.Rot
                                 : Kriterien.Count == 0 ? Ampel.Rot
                                 : Kriterien.Max(k => k.Ampel);

        /// <summary>Wie genau die Jahresenergie nach der Kalibrierung stimmen muss (relativ).</summary>
        internal const double ENERGIE_GENAU = 1e-9;

        /// <summary>
        /// <b>Die drei Kriterien je Objekt</b> nach Kapitel 7 Zeile Z5: Band der Dauerlinie,
        /// Formabgleich des Tagesgangs, Jahresenergie nach der Kalibrierung.
        ///
        /// <para><b>Warum die √N-Skalierung hier kein Kriterium ist.</b> Kapitel 7 nennt sie als
        /// messbares Kriterium der Stufe, und das Werkzeug rechnet und berichtet sie — aber sie ist
        /// eine Aussage über das <b>Verhältnis von Objekten verschiedener Größe</b>: Die Spitze je
        /// Einheit fällt mit der Zahl der Einheiten wie 1/√N (4.4). An <b>einem</b> Objekt ist
        /// <c>Skalierungsmass = Spitzenverhaeltnis · √N</c> nur eine Zahl; ein Band um 1 zu ziehen
        /// hieße zu behaupten, die Rechnung überschätze die Spitze jedes Objekts um genau den Faktor
        /// √N — das sagt das Konzept nicht, und schon ein Mehrfamilienhaus mit 48 Personen könnte es
        /// nicht erfüllen. Geprüft wird die Skalierung deshalb <b>im Sammelbericht</b> über alle
        /// Objekte (<see cref="Sammelkriterium"/>): dort trägt sie die Steigung von
        /// ln(Spitzenverhältnis) über ln(N) und hält sie gegen −0,5.</para>
        /// </summary>
        internal void KriterienBilden()
        {
            Kriterien.Clear();
            if (Abbruch != null) return;

            // (b) Die Messspitze im P85-P95-Band der synthetischen Dauerlinie (Konzept 3.6).
            string band = Zahl(BandUnten) + " … " + Zahl(BandOben);
            Kriterien.Add(new Kriterium("Band der Dauerlinie",
                Lage == Spitzenlage.ImBand ? Ampel.Gruen
                : Lage == Spitzenlage.Unbestimmt ? Ampel.Gelb : Ampel.Rot,
                Spitzenverhaeltnis, band,
                Lage == Spitzenlage.Unbestimmt
                    ? "Ohne Stundenwerte der Messung ist die Lage nicht entscheidbar."
                    : Lage == Spitzenlage.ImBand
                      ? "Die Messspitze liegt im Band der gerechneten Dauerlinie."
                      : Lage == Spitzenlage.Oberhalb
                        ? "Die Messspitze liegt ueber dem Band - die Rechnung unterschaetzt die Spitze."
                        : "Die Messspitze liegt unter dem Band - die Rechnung ueberschaetzt die Spitze staerker "
                          + "als erwartet."));

            // (d) Der Formabgleich des Tagesgangs gegen die Schwelle (Parameter).
            Kriterien.Add(new Kriterium("Formabgleich Tagesgang",
                Formmass == null ? Ampel.Gelb : Formmass.Value <= Formschwelle ? Ampel.Gruen : Ampel.Rot,
                Formmass, "höchstens " + Zahl(Formschwelle),
                Formmass == null
                    ? "Ohne einen vollstaendigen Messtag je Tagtyp nicht bildbar."
                    : "Das Formmass ist die groesste mittlere Stundenabweichung ueber die Tagtypen."));

            // (a)/(4) Die Jahresenergie nach der Kalibrierung.
            Kriterien.Add(new Kriterium("Energie nach Kalibrierung",
                EnergieResiduum == null ? Ampel.Gelb
                : EnergieResiduum.Value <= ENERGIE_GENAU ? Ampel.Gruen : Ampel.Rot,
                EnergieResiduum, "höchstens " + Zahl(ENERGIE_GENAU),
                EnergieResiduum == null
                    ? "Die Kalibrierung ist nicht gelaufen - Grund in der Hinweisliste."
                    : "Nach der Kalibrierung ist die Jahresenergie der Rechnung der Nettomesswert; "
                      + "das Residuum ist der verbliebene relative Abstand."));
        }

        private static string Zahl(double? w)
            => w == null ? "—" : w.Value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
    }
}
