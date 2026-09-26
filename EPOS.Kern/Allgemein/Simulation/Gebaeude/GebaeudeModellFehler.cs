using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die benannten Fehlgründe des Gebäudemodells nach VDI 6007 Blatt 1 (Stufe G0 der
    /// Gebäudesimulation, Umsetzungskonzept 1.3).
    ///
    /// <para><b>Warum benannt.</b> Kein stiller Rückfall: Ein Parameter, der das Netz
    /// unphysikalisch macht, wird nicht auf einen selbst gewählten Klemmwert gezogen,
    /// sondern bricht die Rechnung mit einem Grund ab, den der Aufrufer lesen und dem
    /// Anwender nennen kann. Die einzigen Setzwerte sind die, die die Richtlinie selbst
    /// vorschreibt (Gl. (28a)–(28c) der Außenbauteilgruppe); sie werden im Parametersatz
    /// ausgewiesen (<see cref="ErsatzparameterRC.Gruppenfall"/>). Jeder Grund steht hier
    /// genau einmal; die Tests prüfen gegen den Grund, nicht gegen den Meldungstext.</para>
    /// </summary>
    internal enum GebaeudeModellFehler
    {
        /// <summary>Eine Kapazität ist nicht endlich oder nicht größer null.</summary>
        KapazitaetUngueltig,

        /// <summary>Ein Widerstand des Netzes ist nicht endlich oder nicht größer null.</summary>
        WiderstandUngueltig,

        /// <summary>
        /// Die Außenbauteilgruppe hat keinen gültigen Fall nach Gl. (27)–(28c): Im Regelfall
        /// ist der Restwiderstand null oder negativ und der Grenzfall (28a) greift nicht oder
        /// ist mangels äußerem Übergangswiderstand nicht prüfbar; oder der Gesamtwiderstand
        /// eines Zweigs bzw. der Gruppe ist nicht positiv; oder R_Rest,AW ist nicht endlich.
        /// Rechnerisch bei sehr schlecht gedämmten Außenbauteilen (Rechenschritte A4, A7a).
        /// </summary>
        RRestAwNichtPositiv,

        /// <summary>
        /// Der Widerstand des Fensterzweigs ist null oder negativ (R_1,AF bzw. R_Rest,AF),
        /// nur einer der beiden ist gesetzt, oder die Fensterfläche ist null. Kein Klemmwert
        /// (Rechenschritte A7a).
        /// </summary>
        FensterzweigUngueltig,

        /// <summary>Eine Bezugsfläche ist negativ, nicht endlich oder die Summe null.</summary>
        FlaecheUngueltig,

        /// <summary>Ein Transmissionsleitwert (Σ U·A) ist negativ oder nicht endlich.</summary>
        LeitwertUngueltig,

        /// <summary>
        /// Ein Eigenwert der Systemmatrix ist nicht reell oder nicht negativ. Für ein
        /// passives RC-Netz ist das ausgeschlossen; tritt es auf, ist der Parametersatz
        /// widersprüchlich.
        /// </summary>
        EigenwerteNichtNegativ,

        /// <summary>Das Gleichungssystem der Oberflächen- und Luftknoten ist singulär.</summary>
        SystemSingulaer,

        /// <summary>Eine Randbedingung der Stunde ist nicht endlich oder widersprüchlich.</summary>
        RandUngueltig,

        /// <summary>
        /// Die Stunde ist nach der zulässigen Zahl von Abschnitten nicht zur Ruhe gekommen.
        /// Es entsteht kein Teilstundenergebnis (Rechenschritte 7.1).
        /// </summary>
        AbschnittsdeckelErreicht,

        /// <summary>
        /// Der Vorlauf hat nach der Höchstzahl an Durchläufen die Schwelle der Zustandsänderung
        /// nicht unterschritten (<see cref="Vorlauf2K"/>, Rechenschritte 7.2). Kein stiller
        /// Weiterlauf.
        /// </summary>
        VorlaufNichtKonvergiert,

        // ---- Stufe G1: der Klassenweg und der Eingangsbauer (Konzept 4.8) ----------

        /// <summary>
        /// Eine Pflichtgröße des Gebäudes fehlt oder ist nicht größer null: Nutzfläche,
        /// Raumhöhe, Fläche je Nutzer oder die Luftwechselrate (Rechenschritte 1.1, A1, A7).
        /// </summary>
        PflichtgroesseFehlt,

        /// <summary>
        /// Die Speichermasse je Nutzfläche liegt außerhalb der Plausibilitätsgrenze
        /// (Bauweise/Nutzfläche, Rechenschritte A1, Konzept 4.8).
        /// </summary>
        BauweiseUnplausibel,

        /// <summary>Ein U-Wert einer Bauteilgruppe mit Fläche liegt außerhalb der Plausibilitätsgrenze (Konzept 4.8).</summary>
        UWertUnplausibel,

        /// <summary>Der Gesamtenergiedurchlassgrad liegt nicht in (0, 1] (Konzept 4.8).</summary>
        GWertUnplausibel,

        /// <summary>
        /// Die Fensterflächen je Orientierung ergeben nicht die gesamte Fensterfläche, oder
        /// eine davon ist negativ (Rechenschritte 1.1, Konzept 4.8).
        /// </summary>
        FensterflaechenWidersprechen,

        /// <summary>
        /// Ein Modellparameter der Stufe G1 liegt außerhalb seines Wertebereichs:
        /// Rahmenanteil, Verschattungsfaktor, Masseanteil außen, Innenflächenfaktor,
        /// Strahlungsanteil der Heizung, Heizleistungsgrenze, Kellertemperatur, innere Lasten,
        /// Wärmebrücken, Randbedingung der Grundfläche (Rechenschritte 1.1).
        /// </summary>
        ParameterUngueltig,

        /// <summary>
        /// Der Sollwertfahrplan ist widersprüchlich: ein Sollwert nicht endlich, die obere
        /// Raumtemperatur nicht über dem Tagsollwert, oder ein aktiver Ferienfahrplan mit einem
        /// Tag außerhalb 1…365 (Rechenschritte E8, 1.1).
        /// </summary>
        SollwertfahrplanUngueltig,

        /// <summary>
        /// Die Klimadaten reichen für das Stundenmodell nicht: keine 8 760 Stunden in
        /// Ortszeit, keine Wochenendmaske, ein nicht endlicher Wert (Rechenschritte 1.2, E1).
        /// </summary>
        KlimadatenUnvollstaendig,

        /// <summary>
        /// Der unskalierte Jahreswert des Laufs ist null oder nicht endlich; die
        /// Verbrauchs-Rückrechnung (E8) hätte eine Division durch null (Rechenschritte 8.3,
        /// Umsetzungskonzept 1.5 Punkt 2).
        /// </summary>
        VerbrauchAltNull,

        /// <summary>Eine Ergebnisreihe des Laufs ist nicht endlich oder negativ (Plausibilität nach dem Lauf).</summary>
        ErgebnisUnplausibel,

        // ---- Stufe KU1 der Kühlung (Kühlkonzept 3.2, 3.3) ----------------------------

        /// <summary>
        /// Der Kühlsollwert liegt nicht mindestens
        /// <see cref="GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K"/> über dem höchsten
        /// Heizsollwert des Fahrplans, oder er ist nicht endlich — harte Prüfregel, benannt mit
        /// beiden Werten (Kühlkonzept 3.2, 8.5).
        /// </summary>
        KuehlsollwertUnterHeizsollwert,

        /// <summary>
        /// Die Abschnittsregel ist verletzt (Festlegung F-K3, Kühlkonzept 3.3): Ein Abschnitt
        /// der Stundenschleife hat eine Leistung mit falschem Vorzeichen für seinen Betriebsfall
        /// gebucht — ein Heizfall Kälte oder ein Kühlfall Wärme. Die scharfe Zusicherung „je
        /// Abschnitt nie beides" fällt laut.
        /// </summary>
        AbschnittsregelVerletzt,

        // ---- Stufe AK1 der Anlagenkopplung (Konzept Anlagenkopplung 3, 8.4, 9.5) --------

        /// <summary>
        /// Die Wärmeübergabe ist widersprüchlich oder unplausibel: unbekannte Übergabeart, ein
        /// Auslegungspunkt mit Vorlauf nicht über dem Rücklauf oder Rücklauf nicht über der
        /// Raumtemperatur, eine Auslegungs-Außentemperatur nicht unter der Raumtemperatur, ein
        /// Exponent, ein Proportionalband, eine Nennleistung oder eine Heizkurve außerhalb der
        /// Prüfregel, oder eine hergeleitete Auslegungsheizlast, die nicht größer null ist.
        /// Benannt mit den Werten; der Lauf bricht für dieses Gebäude ab (9.5).
        /// </summary>
        UebergabeUngueltig,

        /// <summary>
        /// Die Übergabegleichung oder der Arbeitspunkt des Raumreglers ist in der festen
        /// Schrittzahl nicht gelöst (Anlagenkopplung 3.2, 10.2 H2) — ein benannter Abbruch statt
        /// einer stillen Näherung.
        /// </summary>
        UebergabeNichtKonvergiert,

        /// <summary>
        /// Das Sollwert-Zeitprogramm ist unbrauchbar: nicht genau 168 Werte, ein Wert keine Zahl
        /// oder außerhalb der Plausibilitätsgrenze, oder der Kalender des Laufs lässt sich keinem
        /// Wochentag zuordnen (Anlagenkopplung 4.3, H-F10). Kein Auffüllen, kein Abschneiden.
        /// </summary>
        SollwertprofilUngueltig,

        // ---- Stufe G3: der Bauteilweg (Mehrzonenkonzept 3.1–3.6, Rechenschritte Kapitel 3) ----

        /// <summary>
        /// Eine Schicht ist unbrauchbar: Dicke, Wärmeleitfähigkeit, Rohdichte oder spezifische
        /// Wärmekapazität außerhalb des Plausibilitätsbands (Mehrzonenkonzept 3.5; λ ≤ 0 gehört
        /// dazu), eine ruhende Luftschicht dicker als nach DIN EN ISO 6946 Tabelle 8 zulässig,
        /// oder ein Aufbau ohne Schicht. Ein Stoffwert außerhalb des Bands ist kein Wert.
        /// </summary>
        SchichtUngueltig,

        /// <summary>
        /// Die Reduktion eines Schichtaufbaus nach VDI 6007 Blatt 1 Gl. (12)–(17) liefert keinen
        /// endlichen, positiven Ersatzwert — ein Aufbau ohne wirksame Speichermasse, der als
        /// masseloses Bauteil zu führen ist.
        /// </summary>
        BauteilreduktionUngueltig,

        /// <summary>
        /// Ein Bauteil ist unbrauchbar: Fläche, Neigung, Azimut, g-Wert, Rahmenanteil,
        /// Verschattung, Übergangskoeffizient oder Wärmebrückenleitwert außerhalb des Bereichs,
        /// kein U-Wert ohne Schichten, ein Fenster mit Schichtaufbau oder an falscher
        /// Randbedingung, ein nicht positiver Widerstand nach Gl. (26), oder kein opakes
        /// Außenbauteil im Satz.
        /// </summary>
        BauteilUngueltig,

        /// <summary>
        /// Ein geneigtes Bauteil an Außenluft hat keinen Azimut; ohne Azimut dürfen nur
        /// waagerechte Flächen (Neigung 0° oder 180°) stehen.
        /// </summary>
        AzimutFehlt,

        /// <summary>
        /// Die Randbedingung „Nachbarzone" ist im Einzonenweg nicht abgebildet; sie gilt nur zwischen
        /// Zonen desselben Gebäudes in der Zonenschleife (Stufe G6b, Mehrzonenkonzept 2.3). Kein
        /// stilles Umdeuten auf eine andere Randbedingung.
        /// </summary>
        RandbedingungNichtAbgebildet,

        /// <summary>
        /// Ein Gebäude trägt mehr Zonen, als der Weg rechnet: der Einzonenweg (Klassen- oder
        /// Bauteilweg, Entscheid A14/E27) höchstens eine, die Zonenschleife (Stufe G6b) bis zur
        /// <see cref="GebaeudeZonenregeln.PFLEGEGRENZE"/>. Keine stille Auswahl einer der Zonen.
        /// </summary>
        MehrereZonen,

        // ---- Entscheid E43: die Nachtzeit je Gebäude (Konzept-Nachtrag N1.48) -------------

        /// <summary>
        /// Die Nachtzeit des Gebäudes ist widersprüchlich (<see cref="Nachtzeit.Pruefen"/>): nur Beginn
        /// oder nur Ende ist gesetzt, eine Stunde liegt außerhalb 0 … 23, oder Beginn und Ende sind
        /// gleich. Kein stiller Rückfall auf die Vorgabe 22 bis 6 Uhr.
        /// </summary>
        NachtzeitUngueltig,

        // ---- Stufe G6b: Mehrzonen-Rechnung (Mehrzonenkonzept 2.2–2.7) ----------------------

        /// <summary>
        /// Das Gebäude hat keine beheizte Zone (Festlegung 2 des Auftrags G6b: <c>IstBeheizt = 0</c>
        /// heißt frei schwingend; ohne beheizte Zone gäbe es keinen Wärmebedarf). Benannt abgelehnt.
        /// </summary>
        KeineBeheizteZone,

        /// <summary>
        /// Die Kopplung der Zonen ist widersprüchlich: eine Trennfläche ohne Nachbarzone oder mit
        /// einer, die das Gebäude nicht führt, eine Trennfläche zur eigenen Zone, ein Luftstrom zu
        /// einer fremden Zone. Keine stille Umdeutung.
        /// </summary>
        ZonenkopplungUngueltig,

        /// <summary>
        /// Die Zonenschleife ist in einer Stunde nach der Höchstzahl der Durchläufe nicht zur Ruhe
        /// gekommen (Mehrzonenkonzept 2.4, Festlegung 6 des Auftrags G6b). Kein stiller Rückfall auf den
        /// letzten Stand: Der Bedarfslauf bricht ab (Festlegung 12).
        /// </summary>
        ZonenkopplungKonvergiertNicht,
    }

    /// <summary>
    /// Der Abbruch des Gebäudemodells mit einem benannten <see cref="GebaeudeModellFehler"/>.
    /// Der Meldungstext ist für das Protokoll gedacht; entschieden wird über
    /// <see cref="Grund"/>.
    /// </summary>
    internal sealed class GebaeudeModellException : Exception
    {
        internal GebaeudeModellException(GebaeudeModellFehler grund, string meldung)
            : base(meldung)
        {
            Grund = grund;
        }

        /// <summary>Der benannte Grund des Abbruchs.</summary>
        internal GebaeudeModellFehler Grund { get; }
    }
}
