using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die fünf Prüfgruppen der Modellprüfung</b> (Entwurf Geführter Modellaufbau 2.2, E137 F1). Die Zahl ist die
    /// Reihenfolge der Ampelleiste.
    /// </summary>
    public enum Pruefgruppe
    {
        /// <summary>Geometrie und Bauteile: Zonen mit Bauteilen, Hülle, Bauteilart, Fläche, Randbedingung, Orientierung.</summary>
        GeometrieBauteile = 1,

        /// <summary>Aufbauten und Stoffwerte: Bauteil nur mit U-Wert, ungeeigneter Aufbau, Schicht ohne Stoffwert, Kapazität.</summary>
        AufbautenStoffwerte = 2,

        /// <summary>Nutzung und Konditionierung: Profil, Heiz-, Kühl- und Lüftungskalender, Ferien und Heizperiode.</summary>
        NutzungKonditionierung = 3,

        /// <summary>Ort und Lage: Klimaregion, Nordrichtung, Feiertagsland.</summary>
        OrtLage = 4,

        /// <summary>Rechenfähigkeit: die Vorprüfung des Schreib- und Rechenwegs.</summary>
        Rechenfaehigkeit = 5,
    }

    /// <summary>Die Ampelstufe eines Befunds und einer Gruppe.</summary>
    public enum Pruefampel
    {
        /// <summary>Grün: kein offener Punkt.</summary>
        Gruen = 0,

        /// <summary>Gelb: rechnet mit Vorgaben (Annahme, Ersatz, Erbe).</summary>
        Gelb = 1,

        /// <summary>Rot: nicht rechenfähig.</summary>
        Rot = 2,
    }

    /// <summary>Der Rechenstatus des Gebäudes — Kopf über den Gruppen (Entwurf 2.2).</summary>
    public enum Rechenstatus
    {
        /// <summary>Keine Gruppe hat einen offenen Punkt.</summary>
        Vollstaendig = 0,

        /// <summary>Rechenfähig, aber mit Annahmen.</summary>
        RechnetMitVorgaben = 1,

        /// <summary>Mindestens eine Gruppe ist rot.</summary>
        NichtRechenfaehig = 2,
    }

    /// <summary>
    /// <b>Das Sprungziel einer Gruppe</b>: der Farbmodus der Gebäudeansicht, den ein Klick auf die Ampel öffnet (MA-1b, MA-5).
    /// </summary>
    public enum Pruefsprungziel
    {
        /// <summary>Farbmodus „Zonen“.</summary>
        Zonen = 0,

        /// <summary>Farbmodus „Randbedingung“.</summary>
        Randbedingung = 1,

        /// <summary>Farbmodus „Aufbau“.</summary>
        Aufbau = 2,

        /// <summary>Farbmodus „Befund“.</summary>
        Befund = 3,
    }

    /// <summary>Was ein Vorschlag ändert.</summary>
    public enum Vorschlagsart
    {
        /// <summary>Einem Bauteil einen Aufbau zuweisen (<c>Tab_Bauteil.ID_Aufbau</c>).</summary>
        Aufbau = 0,

        /// <summary>Einer Zone ein Nutzungsprofil samt Kalendern geben (Generator).</summary>
        Profil = 1,

        /// <summary>Ferien bzw. Heizperiode des Gebäudes aus einem anderen Gebäude des Projekts übernehmen.</summary>
        FerienSaison = 2,

        /// <summary>Das Feiertagsland eines anderen Gebäudes des Projekts übernehmen.</summary>
        Feiertagsland = 3,
    }

    /// <summary>Woher ein Vorschlag kommt (Rangfolge E137 F3 und F6).</summary>
    public enum Vorschlagsherkunft
    {
        /// <summary>Aufbau des Projekts gleicher Bauteilart und Randbedingung („bestehende Außenwand“).</summary>
        Projektaufbau = 0,

        /// <summary>Aufbau des Bauteilkatalogs (geeignet, gleiche Bauteilart, U-Nähe).</summary>
        Katalogaufbau = 1,

        /// <summary>Typaufbau, auf das U abgeglichen (Ersatzaufbau).</summary>
        Typaufbau = 2,

        /// <summary>Profil der DIN-Nummer der Zone.</summary>
        DinNummer = 3,

        /// <summary>Profil des Raumtyps oder der Nutzungsklasse der Quelle.</summary>
        Raumtyp = 4,

        /// <summary>Profil der Gebäudeart.</summary>
        Gebaeudeart = 5,

        /// <summary>EPOS-Muster „Wohnen“ bzw. „Sonstige“ als letzter Rückfall.</summary>
        EposMuster = 6,

        /// <summary>Wert eines anderen Gebäudes desselben Projekts.</summary>
        Projekt = 7,
    }

    /// <summary>
    /// <b>Ein Vorschlag zu einem Befund</b> — reine Daten: beschreibbar (<see cref="Text"/>) und über
    /// <c>ModellpruefungCtrl</c> ausführbar. Die Felder je Art: <see cref="Vorschlagsart.Aufbau"/> trägt
    /// <see cref="IdAufbau"/> (Projekt- bzw. Katalogaufbau) oder <see cref="Typaufbau"/>; <see cref="Vorschlagsart.Profil"/>
    /// trägt <see cref="IdProfil"/> und <see cref="Name"/>; <see cref="Vorschlagsart.FerienSaison"/> trägt in
    /// <see cref="IdQuelle"/> das Quellgebäude; <see cref="Vorschlagsart.Feiertagsland"/> den Wert in <see cref="Name"/>.
    /// </summary>
    public sealed record Pruefvorschlag(Vorschlagsart Art, Vorschlagsherkunft Herkunft, string Text,
                                        int? IdAufbau = null, string Typaufbau = null, long? IdProfil = null,
                                        string Name = null, int? IdQuelle = null, double? U = null);

    /// <summary>
    /// <b>Ein Befund</b> — Code (sprachneutral, stabil), Ampel, Zielobjekt und Text; dazu der Vorschlag oder <c>null</c>,
    /// wenn es keinen gibt (Pflichteingabe oder spätere Welle).
    /// </summary>
    public sealed record Modellbefund(Pruefgruppe Gruppe, string Code, Pruefampel Ampel, string Text,
                                     int? IdZone = null, int? IdBauteil = null, double? Flaeche = null,
                                     Pruefvorschlag Vorschlag = null);

    /// <summary>
    /// <b>Das Ergebnis einer Prüfgruppe</b>: Ampel (die schärfste ihrer Befunde), Zählung, Befunde, Zahl der ausführbaren
    /// Vorschläge, betroffene Fläche und das Sprungziel der Ansicht.
    /// </summary>
    public sealed record Pruefgruppenergebnis(Pruefgruppe Gruppe, Pruefampel Ampel, IReadOnlyList<Modellbefund> Befunde,
                                              Pruefsprungziel Sprungziel)
    {
        /// <summary>Die Zahl der Befunde.</summary>
        public int Anzahl => Befunde.Count;

        /// <summary>Die Zahl der Befunde mit Vorschlag.</summary>
        public int Vorschlaege => Befunde.Count(b => b.Vorschlag != null);

        /// <summary>Die Summe der Fläche der betroffenen Bauteile [m²].</summary>
        public double Flaeche => Befunde.Sum(b => b.Flaeche ?? 0.0);

        /// <summary>Die Zählung je Befundcode (für die Klappgruppe: eine Zeile je Art).</summary>
        public IReadOnlyDictionary<string, int> JeCode =>
            Befunde.GroupBy(b => b.Code, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
    }

    /// <summary><b>Das Ergebnis der Modellprüfung eines Gebäudes</b>: Rechenstatus und die fünf Gruppen in ihrer Reihenfolge.</summary>
    public sealed record Modellpruefergebnis(int IdGebaeude, bool Mehrzonen, Rechenstatus Status,
                                             IReadOnlyList<Pruefgruppenergebnis> Gruppen)
    {
        /// <summary>Die Gruppe <paramref name="g"/>.</summary>
        public Pruefgruppenergebnis Gruppe(Pruefgruppe g) => Gruppen.First(x => x.Gruppe == g);

        /// <summary>Alle Befunde über alle Gruppen.</summary>
        public IEnumerable<Modellbefund> Befunde => Gruppen.SelectMany(g => g.Befunde);
    }

    /// <summary>
    /// <b>Das Ergebnis einer Übernahme oder Rücknahme</b>: ausgeführte und verworfene Vorschläge (mit Grund), ob eine
    /// Rücknahme möglich ist, und der neue Prüfstand.
    /// </summary>
    public sealed record Uebernahmeergebnis(bool Ok, string Meldung, IReadOnlyList<Pruefgruppe> Gruppen, int Uebernommen,
                                            IReadOnlyList<string> Verworfen, bool RuecknahmeMoeglich,
                                            Modellpruefergebnis Neu);
}
