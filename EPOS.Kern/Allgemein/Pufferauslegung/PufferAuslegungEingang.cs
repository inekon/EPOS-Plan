using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER EINGANG DER PUFFERSPEICHER-AUSLEGUNG (Konzept Pufferspeicher-Auslegung, Abschnitt 3
    // und 5). Ein reiner Datensatz: Der Controller (PufferAuslegungCtrl, Welle W3) liest Projekt,
    // Puffer, Kaskade, Erzeuger, Gebaeude, Zapfprofil und Vorgabetabelle und baut daraus den
    // Eingang; PufferAuslegung.Rechnen rechnet ihn ohne Datenbank, rein funktional.
    //
    // Reihen in kWh je Stunde (= mittlere kW der Stunde); Laenge beliebig (8 760 im Projekt,
    // kuerzere Reihen in den Tests). NULL in einem nullbaren Feld = Vorgabe.
    // ====================================================================================

    /// <summary>Die sieben Vorlagen (Spalte <c>Tab_PufferAuslegung.Vorlage</c>, Konzept 4.1).</summary>
    public enum PufferVorlage
    {
        WP_MONO,
        WP_BIVALENT,
        BHKW,
        KESSEL,
        FESTBRENNSTOFF,
        SOLAR,
        PROZESS
    }

    /// <summary>Die Nutzungsprofile (Spalte <c>Tab_PufferAuslegung.Nutzungsprofil</c>, Konzept 3.5).</summary>
    public enum PufferNutzungsprofil
    {
        WOHNEN,
        BEHERBERGUNG,
        PFLEGE,
        BUERO_SCHULE,
        GEWERBE
    }

    /// <summary>Der Brennstoff eines Festbrennstoffkessels (K9).</summary>
    public enum PufferBrennstoff
    {
        Keiner,
        Scheitholz,
        Pellets,
        Hackschnitzel
    }

    /// <summary>Die Bauart des Kollektorfelds (K10).</summary>
    public enum PufferKollektorart
    {
        Flach,
        Roehre
    }

    /// <summary>Wie das Trinkwasser bereitet wird (aus der Zapfprofil-Zone, Konzept 3.2).</summary>
    public enum PufferBwTopologie
    {
        /// <summary>Trinkwasserspeicher — der Nenninhalt kommt aus der Zapfprofil-Speicherauslegung.</summary>
        Speicher,
        /// <summary>Frischwasserstation am Puffer.</summary>
        Frischwasser,
        /// <summary>Wohnungsstationen am Puffer.</summary>
        Wohnungsstation,
        /// <summary>Durchfluss ohne Speicher — keine Brauchwasserzone im Puffer.</summary>
        Durchfluss
    }

    /// <summary>Woher der Zirkulationszuschlag der Brauchwasserzone kommt (Spalte <c>Zirkulation_Weg</c>).</summary>
    public enum PufferZirkulationWeg
    {
        ZAPFPROFIL,
        PROJEKT,
        ANTEIL,
        JE_WE
    }

    /// <summary>Ein Sperrfenster je Tag: Beginn [h, 0 … 24) und Dauer [h]; über Mitternacht läuft es in den nächsten Tag.</summary>
    public sealed record PufferSperrfenster(double BeginnH, double DauerH);

    /// <summary>Die Sperrprofile (Spalte <c>Sperrprofil</c>) als Fensterlisten — neutrale Eingaben ohne Rechtsbezug.</summary>
    public static class PufferSperrprofil
    {
        /// <summary>
        /// Die Fenster eines Profils: <c>KEINE</c> (leer), <c>ZWEI_MAL_ZWEI</c> (11–13 h, 17–19 h),
        /// <c>DREI_MAL_ZWEI</c> (6–8 h, 11–13 h, 17–19 h), <c>EIGEN</c> (ein Fenster aus Beginn und Dauer).
        /// Ein unbekanntes Profil wird benannt abgelehnt.
        /// </summary>
        public static IReadOnlyList<PufferSperrfenster> Fenster(string profil, double? beginnH = null, double? dauerH = null)
        {
            switch (profil)
            {
                case null:
                case "KEINE":
                    return Array.Empty<PufferSperrfenster>();
                case "ZWEI_MAL_ZWEI":
                    return new[] { new PufferSperrfenster(11, 2), new PufferSperrfenster(17, 2) };
                case "DREI_MAL_ZWEI":
                    return new[] { new PufferSperrfenster(6, 2), new PufferSperrfenster(11, 2), new PufferSperrfenster(17, 2) };
                case "EIGEN":
                    return (dauerH ?? 0) > 0
                        ? new[] { new PufferSperrfenster(beginnH ?? 0, dauerH.Value) }
                        : Array.Empty<PufferSperrfenster>();
                default:
                    throw new ArgumentException("Unbekanntes Sperrprofil: " + profil);
            }
        }
    }

    /// <summary>Der bemessende Erzeuger (Rang 1 der Kaskade) und sein Zweiterzeuger (Konzept 3.1, 5).</summary>
    public sealed record PufferErzeuger
    {
        /// <summary>Nennleistung (thermisch) [kW] — bei der Wärmepumpe im Auslegungspunkt.</summary>
        public double NennleistungKw { get; init; }

        /// <summary>Kleinste Leistung [kW]; <c>null</c> = Vorgabe (Wärmepumpe: Anteil <c>WP.Mindestleistung_Anteil</c>).</summary>
        public double? MindestleistungKw { get; init; }

        /// <summary>Leistungsgeregelt (moduliert) statt Fixed-Speed bzw. Ein/Aus.</summary>
        public bool Geregelt { get; init; }

        /// <summary>Ist eine Wärmepumpe (K1, K2, Band nach DIN EN 15450)?</summary>
        public bool IstWaermepumpe { get; init; }

        /// <summary>Leistung des Zweiterzeugers (Rang 2) [kW]; 0 = keiner.</summary>
        public double ZweiterzeugerKw { get; init; }

        /// <summary>Der Zweiterzeuger ist ein Heizstab — er gilt in der Sperre als mitgesperrt.</summary>
        public bool Heizstab { get; init; }

        /// <summary>Der Zweiterzeuger ist in der Sperre freigegeben — K4 entfällt.</summary>
        public bool ZweiterzeugerFrei { get; init; }

        /// <summary>Brennstoff eines Festbrennstoffkessels.</summary>
        public PufferBrennstoff Brennstoff { get; init; } = PufferBrennstoff.Keiner;

        /// <summary>Abbrandperiode [h] für DIN EN 303-5; <c>null</c> = Vorgabe.</summary>
        public double? AbbrandperiodeH { get; init; }

        /// <summary>Aperturfläche des Kollektorfelds [m²] (Fläche × Modulanzahl).</summary>
        public double KollektorflaecheM2 { get; init; }

        /// <summary>Bauart des Kollektorfelds.</summary>
        public PufferKollektorart Kollektorart { get; init; } = PufferKollektorart.Flach;
    }

    /// <summary>Das Ergebnis der Zapfprofil-Speicherauslegung, soweit die Brauchwasserzone es braucht (Konzept 3.1).</summary>
    public sealed record PufferZapfprofil
    {
        /// <summary>Wie das Trinkwasser bereitet wird.</summary>
        public PufferBwTopologie Topologie { get; init; } = PufferBwTopologie.Speicher;

        /// <summary>Das größte Defizit D_max [kWh] der Summenlinie.</summary>
        public double DmaxKwh { get; init; }

        /// <summary>Nenninhalt des Trinkwasserspeichers [l] (Topologie Speicher); <c>null</c> = keiner gerechnet.</summary>
        public double? NenninhaltL { get; init; }

        /// <summary>Ladeleistung [kW] — nachrichtlich.</summary>
        public double? LadeleistungKw { get; init; }

        /// <summary>Personen der Zonen.</summary>
        public double? Personen { get; init; }

        /// <summary>Tagesbedarf [l/d] bei 60 °C.</summary>
        public double? TagesbedarfL { get; init; }

        /// <summary>Tagesbedarf als Wärme [kWh/d]; <c>null</c> = aus <see cref="TagesbedarfL"/> (60/10 °C).</summary>
        public double? TagesbedarfKwh { get; init; }

        /// <summary>Zirkulationsverlust [kWh/d], den das Zapfprofil schon trägt; <c>null</c> = trägt keine.</summary>
        public double? ZirkulationKwhD { get; init; }
    }

    /// <summary>Ein Katalogpuffer als Kandidat des Katalogvorschlags — Platzhalter-Schnittstelle bis W3.</summary>
    public sealed record PufferKatalogsatz(int Id, string Bezeichner, double VolumenL, double? BereitschaftsverlustKwhD, bool Kombispeicher);

    /// <summary>
    /// Der Eingang der Pufferspeicher-Auslegung (Konzept 5) — alles, was <see cref="PufferAuslegung.Rechnen"/>
    /// braucht, ohne Datenbank.
    /// </summary>
    public sealed record PufferAuslegungEingang
    {
        // ---- Klassen-Set und Vorlage ----

        /// <summary>Heizzone auslegen.</summary>
        public bool KlasseHeizung { get; init; }

        /// <summary>Brauchwasserzone auslegen.</summary>
        public bool KlasseBrauchwasser { get; init; }

        /// <summary>Prozesszone auslegen.</summary>
        public bool KlasseProzess { get; init; }

        /// <summary>Die Vorlage — sie schaltet die Kriterien (<c>Pufferauslegung.Vorlage.&lt;Typ&gt;.*</c>).</summary>
        public PufferVorlage Vorlage { get; init; } = PufferVorlage.WP_MONO;

        /// <summary>Das Nutzungsprofil — nur Herkunft und Beispielhilfe, kein Rechenwert (E-P20).</summary>
        public PufferNutzungsprofil? Nutzungsprofil { get; init; }

        /// <summary>Expertenweg der Sperrzeit: K4e (Lastgang) statt K4 (VDI 4645 Gleichung 23) bemessend.</summary>
        public bool SperrzeitExpertenweg { get; init; }

        // ---- Erzeuger ----

        /// <summary>Der bemessende Erzeuger.</summary>
        public PufferErzeuger Erzeuger { get; init; } = new PufferErzeuger();

        // ---- Puffer ----

        /// <summary>Vorlauftemperatur des Puffers ϑ_VL [°C].</summary>
        public double VorlaufC { get; init; }

        /// <summary>Rücklauftemperatur des Puffers ϑ_RL [°C].</summary>
        public double RuecklaufC { get; init; }

        /// <summary>
        /// Geforderter Vorlauf der Prozesswärme ϑ_VL,P [°C] — der höchste über die Prozesse des Projekts mit
        /// Temperaturpaar; <c>null</c> = keiner gepflegt, die Prozesszone rechnet mit dem Paar des Puffers.
        /// </summary>
        public double? ProzessVorlaufC { get; init; }

        /// <summary>Rücklauf der Prozesswärme ϑ_RL,P [°C] — der tiefste; <c>null</c> = keiner gepflegt.</summary>
        public double? ProzessRuecklaufC { get; init; }

        /// <summary>
        /// Höchster gepflegter Vorlauf der Wärmeerzeuger an der Kaskade [°C]; <c>null</c> = keiner gepflegt,
        /// dann prüft <see cref="PufferWarncode.PROZESS_TEMPERATUR"/> nur gegen die Grenze 95 °C.
        /// </summary>
        public double? ErzeugerVorlaufMaxC { get; init; }

        /// <summary>Einschaltschwelle s_ein (Anteil 0 … 1); <c>null</c> = Vorgabe <c>Puffer.Schwelle_Ein</c>.</summary>
        public double? SchwelleEin { get; init; }

        /// <summary>Ausschaltschwelle s_aus (Anteil 0 … 1); <c>null</c> = Vorgabe <c>Puffer.Schwelle_Aus</c>.</summary>
        public double? SchwelleAus { get; init; }

        // ---- Reihen [kWh/h] ----

        /// <summary>Heizreihe Q_H(t).</summary>
        public IReadOnlyList<double> ReiheHeizung { get; init; } = Array.Empty<double>();

        /// <summary>Brauchwasserreihe Q_B(t) — nachrichtlich; die Zone rechnet aus dem Zapfprofil.</summary>
        public IReadOnlyList<double> ReiheBrauchwasser { get; init; } = Array.Empty<double>();

        /// <summary>Prozessreihe Q_P(t).</summary>
        public IReadOnlyList<double> ReiheProzess { get; init; } = Array.Empty<double>();

        // ---- Randbedingungen der Heizzone ----

        /// <summary>Übergabeart (<c>RADIATOR</c>, <c>FLAECHE</c>, <c>KONVEKTOR</c>, <c>LUEFTER</c>); <c>null</c> = ideal, rechnet wie FLAECHE mit Hinweis.</summary>
        public string Uebergabeart { get; init; }

        /// <summary>Heizgrenze ϑ_HG [°C]; <c>null</c> = 15 °C.</summary>
        public double? HeizgrenzeC { get; init; }

        /// <summary>Auslegungsheizlast Q̇_Ausl [kW]; <c>null</c> = Maximum der Heizreihe.</summary>
        public double? AuslegungsheizlastKw { get; init; }

        /// <summary>Nicht absperrbares Anlagenvolumen V_Hz [l]; <c>null</c> = Vorgabe nach Übergabeart × Q̇_Ausl.</summary>
        public double? AnlagenvolumenL { get; init; }

        /// <summary>Einzelraumregelung vorhanden (dann ist das Anlagenvolumen absperrbar und K1 greift nicht).</summary>
        public bool Einzelraumregelung { get; init; } = true;

        /// <summary>Trinkwasservorrang der Wärmepumpe (Abtau-Gegenprobe, V45).</summary>
        public bool Trinkwasservorrang { get; init; }

        /// <summary>Die Sperrfenster je Tag (<see cref="PufferSperrprofil.Fenster"/>).</summary>
        public IReadOnlyList<PufferSperrfenster> Sperrfenster { get; init; } = Array.Empty<PufferSperrfenster>();

        /// <summary>Startziel [1/d]; <c>null</c> = Vorlage bzw. <c>Takt.Startziel_je_Tag</c>.</summary>
        public double? StartzielJeTag { get; init; }

        /// <summary>Deckungsziel 0 … 1; <c>null</c> = <c>Deckung.Ziel</c>.</summary>
        public double? Deckungsziel { get; init; }

        /// <summary>Mindestlaufzeit [min]; <c>null</c> = Vorlage bzw. <c>Mindestlaufzeit_min</c>.</summary>
        public double? MindestlaufzeitMin { get; init; }

        /// <summary>Verschiebedauer des BHKW [h]; <c>null</c> = <c>BHKW.Verschiebedauer_h</c>.</summary>
        public double? BhkwVerschiebedauerH { get; init; }

        // ---- Aufheizkriterium K12 (V30, KP3) ----

        /// <summary>K12 ein/aus; <c>null</c> = Vorlagenschalter des Nutzungsprofils (<c>Aufheiz.Nutzungsprofil.*</c>).</summary>
        public bool? AufheizKriterium { get; init; }

        /// <summary>Aufheizleistung Φ_n [kW] aus der KP3-Bemessung, Summe über die Projektgebäude; <c>null</c> = keine Bemessung.</summary>
        public double? AufheizleistungKw { get; init; }

        /// <summary>Aufheizdauer n [h] aus der KP3-Rampe; <c>null</c> = Vorgabe <c>Aufheiz.Dauer_h</c>.</summary>
        public double? AufheizdauerH { get; init; }

        // ---- Brauchwasserzone ----

        /// <summary>Das Zapfprofil-Ergebnis; <c>null</c> = keines.</summary>
        public PufferZapfprofil Zapfprofil { get; init; }

        /// <summary>ΔT_B [K]; <c>null</c> = T_oben − T_Rücklauf der Frischwasser-Vorgaben.</summary>
        public double? DeltaTBK { get; init; }

        /// <summary>Temperatur oben im Puffer [°C]; <c>null</c> = <c>Frischwasser.T_Oben_C</c>.</summary>
        public double? TPufferObenC { get; init; }

        /// <summary>Weg des Zirkulationszuschlags; <c>null</c> = Zapfprofil, sonst Projekt, sonst Anteil.</summary>
        public PufferZirkulationWeg? ZirkulationWeg { get; init; }

        /// <summary>Zirkulationsleistung des Projekts [kW] (<c>Tab_Einstellungen.Zirkulation_Leistung_kW</c>).</summary>
        public double? ZirkulationProjektKw { get; init; }

        /// <summary>Laufzeit der Zirkulation [h/d]; <c>null</c> = 24.</summary>
        public double? ZirkulationLaufzeitHd { get; init; }

        /// <summary>Wohneinheiten für den Weg <see cref="PufferZirkulationWeg.JE_WE"/>.</summary>
        public double? Wohneinheiten { get; init; }

        // ---- Empfehlung ----

        /// <summary>Die Nenninhalte [l], aufsteigend; <c>null</c> = feste Liste 100 … 10 000 l.</summary>
        public IReadOnlyList<double> Nenninhalte { get; init; }

        /// <summary>Raster über dem Listenende [l]; <c>null</c> = 1 000 l.</summary>
        public double? NenninhaltRasterL { get; init; }

        /// <summary>Die Katalogpuffer für den Vorschlag; leer = kein Vorschlag.</summary>
        public IReadOnlyList<PufferKatalogsatz> Katalog { get; init; } = Array.Empty<PufferKatalogsatz>();

        /// <summary>Der Parametersatz; <c>null</c> = eingebaute Vorgaben.</summary>
        public PufferAuslegungParameter Parameter { get; init; }
    }
}
