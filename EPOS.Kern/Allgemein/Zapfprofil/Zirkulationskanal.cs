using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was der Zirkulationskanal je Zone braucht (4.3): Name, Jahres-Nutzenergie vor der
    /// Kalibrierung, Zugehörigkeit zu Z1 (Katalog-Bilanzgrenze 1 und Zirkulation „ja") und die
    /// Fläche, wenn die Zone eine trägt.
    /// </summary>
    internal sealed record Zonenanteil(string Zone, double JahresenergieKwh, bool InZ1, double? FlaecheM2);

    /// <summary>
    /// Der Ansatz der Zirkulation (4.3): Methode (<c>null</c> = manuell), Leistung [kW] in den
    /// Laufzeitstunden, Laufzeit [h/d], Gewicht α, Jahresverlust [kWh/a] und je Zone (Reihenfolge
    /// der Eingabe) der Anteil am Jahresverlust [kWh/a]. <see cref="RestKwh"/> ist der Teil, der
    /// keiner Zone zufällt — nur bei manueller Leistung ohne Zone in Z1.
    ///
    /// <para><b>Vor der Kalibrierung (N7).</b> Jahresverlust und Zonenanteile sind der Ansatz
    /// aus den Mengen VOR der Kalibrierung (2.3, 4.1). Kalibriert eine Zone mit Grenze 2 oder 3,
    /// skaliert sie ihren Anteil mit; der verbuchte Jahresverlust steht dann nur in
    /// <see cref="Zapfkennzahlen.JahresverlustZirkulationKwh"/> und in der Reihe
    /// <see cref="ZapfprofilErgebnis.Zirkulation"/> und weicht vom Ansatz ab.</para>
    ///
    /// <para><b>Gewicht α</b> ist der Anteil von Z1 (4.3); angewandt wird es nur bei der Methode
    /// Leitungslänge und beim Flächenkennwert mit gebäudeweiter Fläche <c>Zirk_Flaeche_m2</c>.
    /// Stammt A_N aus den Zonenflächen, ist A_N schon die Fläche von Z1, α entfällt (N7).</para>
    /// </summary>
    internal sealed record Zirkulationsansatz(
        ZapfZirkulationsmethode? Methode,
        double LeistungKw,
        double LaufzeitH,
        double Gewicht,
        double JahresverlustVorKalibrierungKwh,
        IReadOnlyList<double> AnteilJeZoneKwh,
        double RestKwh);

    /// <summary>
    /// <b>Schicht S5 — der Zirkulationskanal</b> (Umsetzungskonzept Zapfprofilgenerator 2.1,
    /// 4.3; A4/ZU5: eigene Teilreihe, Netzverluste unverändert). Die Zirkulation wird NIE in
    /// die Zapfreihe eingerechnet.
    ///
    /// <code>
    /// Z1 = Zonen mit Katalog-Bilanzgrenze 1 und Zirkulation = 1
    /// α  = Σ_{Z1} Q_a,z / Σ_z Q_a,z   (flächengewichtet, wenn jede Zone eine Fläche trägt)
    /// Leitungslänge:   P = α · L · q' / 1000                        [kW]
    /// Anteil:          P = a · Q̄_d,Z1 / t_Lauf,  Q̄_d,Z1 = Σ_{Z1} Q_a,z / 365
    /// Flächenkennwert: P = α · k_A(Lage) · A_N / (365 · t_Lauf)   A_N = Zirk_Flaeche_m2 (gebäudeweit)
    ///                  P = k_A(Lage) · A_N,Z1 / (365 · t_Lauf)    A_N,Z1 = Σ_{Z1} Zonenfläche (N7)
    /// manuell:         P = Zirk_Manuell_Kw
    /// q_zirk,h = P in den Laufzeitstunden, sonst 0;  Q_zirk = P · t_Lauf · 365
    /// Q_zirk,z = Q_zirk · Q_a,z / Σ_{Z1} Q_a
    /// </code>
    ///
    /// <para><b>Vorgabe</b> ist die Methode Flächenkennwert (<c>Zirk_Auto</c> = 1 mit
    /// <c>Zirk_Methode</c>); fehlt jede Fläche, fällt sie mit Hinweis auf die Methode Anteil
    /// zurück. <c>Zirk_Auto</c> = 0 heißt manuell. Die Fläche A_N ist <c>Zirk_Flaeche_m2</c>
    /// (gebäudeweit, mit α), sonst die Summe der Flächen der Zonen in Z1 (Fläche als Bezugsart
    /// oder WE · Wohnfläche je WE, <see cref="Mengengeruest.FlaecheM2"/>) ohne α — eine Zone
    /// außerhalb von Z1 ändert den Verlust der Zonen in Z1 dann nicht (N7). Trägt eine Zone in
    /// Z1 keine Fläche, nennt ein Hinweis sie; sie trägt ihren Mengenanteil am Verlust der
    /// übrigen.</para>
    ///
    /// <para><b>Laufzeitfenster.</b> Die Laufzeitstunden liegen zusammenhängend um die
    /// Tagesmitte der Zapfung: Tagesmitte <c>m = Σ_h (h + ½) · E_h / Σ_h E_h</c> über die
    /// Stundensummen E_h der Zapfreihen (h = 0 … 23), Beginn <c>⌊m − t/2 + ½⌋</c>, so
    /// verschoben, dass das Fenster im Tag liegt; eine gebrochene Laufzeit belegt die letzte
    /// Stunde anteilig. Ohne Zapfung liegt die Mitte bei 12 Uhr.</para>
    /// </summary>
    internal static class Zirkulationskanal
    {
        /// <summary>Watt je Kilowatt.</summary>
        internal const double W_JE_KW = 1000.0;

        /// <summary>Die Mitte des Tages [h] — Tagesmitte ohne Zapfung.</summary>
        internal const double TAGESMITTE_OHNE_ZAPFUNG_H = 12.0;

        /// <summary>
        /// Der Ansatz der Zirkulation aus den Projektgrößen, den Zonenanteilen und dem
        /// Parametersatz. Unvollständige oder ungültige Angaben werden benannt abgelehnt.
        /// </summary>
        internal static Zirkulationsansatz Ansetzen(ProjektStand p, IReadOnlyList<Zonenanteil> zonen, Parametersatz ps,
                                                    Herkunftsprotokoll prot, ICollection<ZapfHinweis> hinweise)
        {
            if (p == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ProjektFehlt, "",
                    "Nicht rechenbar — die Projektgrößen der Zirkulation fehlen.");
            zonen = zonen ?? new Zonenanteil[0];

            double summeAlle = 0.0, summeZ1 = 0.0;
            bool jedeFlaeche = zonen.Count > 0;
            double flaecheAlle = 0.0, flaecheZ1 = 0.0;
            var z1OhneFlaeche = new List<string>();
            foreach (Zonenanteil z in zonen)
            {
                summeAlle += z.JahresenergieKwh;
                if (z.InZ1) summeZ1 += z.JahresenergieKwh;
                if (z.FlaecheM2.HasValue && z.FlaecheM2.Value > 0)
                {
                    flaecheAlle += z.FlaecheM2.Value;
                    if (z.InZ1) flaecheZ1 += z.FlaecheM2.Value;
                }
                else
                {
                    jedeFlaeche = false;
                    if (z.InZ1) z1OhneFlaeche.Add(z.Zone);
                }
            }
            double alpha = Gewicht(summeAlle, summeZ1, jedeFlaeche, flaecheAlle, flaecheZ1);
            prot?.Vermerken("", ZapfFeld.ZIRKULATION_GEWICHT, alpha, "-", Wertstatus.Vorgabe, null,
                            jedeFlaeche ? "flächengewichtet" : "mengengewichtet");

            double laufzeit = ProjektOderParameter(p.ZirkLaufzeitH, ZapfParameter.ZIRKULATION_LAUFZEIT, ps, prot,
                                                   ZapfFeld.ZIRKULATION_LAUFZEIT, "h/d");
            if (double.IsNaN(laufzeit) || !(laufzeit > 0) || laufzeit > Zapfkalender.STUNDEN_TAG)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                    "Nicht rechenbar — die Laufzeit der Zirkulation liegt nicht in (0; 24] h.");

            ZapfZirkulationsmethode? methode;
            double leistung;
            if (!p.ZirkAuto)
            {
                methode = null;
                if (!p.ZirkManuellKw.HasValue || double.IsNaN(p.ZirkManuellKw.Value)
                    || double.IsInfinity(p.ZirkManuellKw.Value) || p.ZirkManuellKw.Value < 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                        "Nicht rechenbar — die Zirkulation steht auf manuell, die Leistung fehlt oder ist negativ.");
                leistung = p.ZirkManuellKw.Value;
                prot?.Vermerken("", ZapfFeld.ZIRKULATION_METHODE, null, "", Wertstatus.Ueberschrieben, null, "manuell");
            }
            else
            {
                methode = p.ZirkMethode;
                double flaecheN = 0.0;
                // α nur auf eine gebäudeweite Fläche; A_N aus den Zonen ist schon die Fläche von Z1 (N7).
                double gewichtFlaeche = alpha;
                if (methode == ZapfZirkulationsmethode.Flaechenkennwert)
                {
                    if (p.ZirkFlaecheM2.HasValue)
                    {
                        flaecheN = NichtNegativ(p.ZirkFlaecheM2.Value, "Fläche der Zirkulation");
                        prot?.Vermerken("", ZapfFeld.ZIRKULATION_FLAECHE, flaecheN, "m²", Wertstatus.Ueberschrieben, null,
                                        "gebäudeweit, mit α");
                    }
                    else
                    {
                        flaecheN = flaecheZ1;
                        gewichtFlaeche = 1.0;
                        prot?.Vermerken("", ZapfFeld.ZIRKULATION_FLAECHE, flaecheN, "m²", Wertstatus.Vorgabe, null,
                                        "Summe der Flächen der Zonen in Z1, ohne α");
                        if (flaecheN > 0)
                            foreach (string name in z1OhneFlaeche)
                                hinweise?.Add(new ZapfHinweis(name, "ZIRKULATION_ZONE_OHNE_FLAECHE",
                                    "Die Zone „" + name + "“ gehört zur Zirkulation, trägt aber keine Fläche; "
                                    + "A_N enthält sie nicht."));
                    }
                    if (!(flaecheN > 0))
                    {
                        methode = ZapfZirkulationsmethode.Anteil;
                        hinweise?.Add(new ZapfHinweis("", "ZIRKULATION_OHNE_FLAECHE",
                            "Keine Fläche für den Flächenkennwert der Zirkulation; gerechnet mit der Methode Anteil."));
                    }
                }
                prot?.Vermerken("", ZapfFeld.ZIRKULATION_METHODE, (int)methode.Value, "",
                                methode == p.ZirkMethode ? Wertstatus.Vorgabe : Wertstatus.Umgerechnet, null,
                                methode.Value.ToString());

                switch (methode.Value)
                {
                    case ZapfZirkulationsmethode.Leitungslaenge:
                    {
                        if (!p.ZirkLaengeM.HasValue)
                            throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                                "Nicht rechenbar — die Methode Leitungslänge braucht eine Leitungslänge.");
                        double laenge = NichtNegativ(p.ZirkLaengeM.Value, "Leitungslänge");
                        prot?.Vermerken("", ZapfFeld.ZIRKULATION_LAENGE, laenge, "m", Wertstatus.Ueberschrieben, null);
                        double qStrich = ProjektOderParameter(p.ZirkVerlustWJeM, ZapfParameter.ZIRKULATION_VERLUST_JE_METER,
                                                              ps, prot, ZapfFeld.ZIRKULATION_VERLUST_JE_METER, "W/m");
                        NichtNegativ(qStrich, "Verlust je Meter");
                        leistung = alpha * laenge * qStrich / W_JE_KW;
                        break;
                    }
                    case ZapfZirkulationsmethode.Anteil:
                    {
                        double a = ProjektOderParameter(p.ZirkAnteil, ZapfParameter.ZIRKULATION_ANTEIL, ps, prot,
                                                        ZapfFeld.ZIRKULATION_ANTEIL, "-");
                        NichtNegativ(a, "Anteil der Zirkulation");
                        double tagesbedarfZ1 = summeZ1 / Zapfkalender.TAGE;
                        leistung = a * tagesbedarfZ1 / laufzeit;
                        break;
                    }
                    case ZapfZirkulationsmethode.Flaechenkennwert:
                    {
                        int lage = Lage(p.ZirkLage, ps, prot);
                        string schluessel = lage == (int)ZapfLeitungslage.InnerhalbHuelle
                                            ? ZapfParameter.ZIRKULATION_KENNWERT_LAGE1 : ZapfParameter.ZIRKULATION_KENNWERT_LAGE2;
                        double k = ProjektOderParameter(p.ZirkKennwert, schluessel, ps, prot,
                                                        ZapfFeld.ZIRKULATION_KENNWERT, "kWh/(m²·a)");
                        NichtNegativ(k, "Flächenkennwert");
                        leistung = gewichtFlaeche * k * flaecheN / (Zapfkalender.TAGE * laufzeit);
                        break;
                    }
                    default:
                        throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                            "Nicht rechenbar — unbekannte Methode der Zirkulation.");
                }
            }

            double jahresverlust = leistung * laufzeit * Zapfkalender.TAGE;
            prot?.Vermerken("", ZapfFeld.ZIRKULATION_LEISTUNG, leistung, "kW", Wertstatus.Vorgabe, null);
            prot?.Vermerken("", ZapfFeld.ZIRKULATION_JAHRESVERLUST, jahresverlust, "kWh/a", Wertstatus.Vorgabe, null,
                            "P · t_Lauf · 365");

            var anteile = new double[zonen.Count];
            double rest = jahresverlust;
            if (summeZ1 > 0)
            {
                rest = 0.0;
                for (int i = 0; i < zonen.Count; i++)
                {
                    if (!zonen[i].InZ1) continue;
                    anteile[i] = jahresverlust * zonen[i].JahresenergieKwh / summeZ1;
                    prot?.Vermerken(zonen[i].Zone, ZapfFeld.ZIRKULATION_ZONENANTEIL, anteile[i], "kWh/a",
                                    Wertstatus.Vorgabe, null);
                }
            }
            else if (jahresverlust > 0)
            {
                hinweise?.Add(new ZapfHinweis("", "ZIRKULATION_OHNE_ZONE",
                    "Keine Zone trägt einen Zirkulationsanteil; die Zirkulation ist gebäudeweit ausgewiesen."));
            }

            foreach (Zonenanteil z in zonen)
                if (!z.InZ1 && z.JahresenergieKwh > 0)
                    hinweise?.Add(new ZapfHinweis(z.Zone, "ZIRKULATION_NICHT_IN_Z1",
                        "Die Zone „" + z.Zone + "“ trägt keinen Zirkulationsanteil (Bilanzgrenze des Kennwerts oder Zirkulation „nein“)."));

            return new Zirkulationsansatz(methode, leistung, laufzeit, alpha, jahresverlust, Array.AsReadOnly(anteile), rest);
        }

        /// <summary>
        /// Das Gewicht α (4.3): flächengewichtet, wenn jede Zone eine Fläche trägt, sonst
        /// mengengewichtet; 0 ohne Zone in Z1 oder ohne Menge.
        /// </summary>
        internal static double Gewicht(double summeAlleKwh, double summeZ1Kwh, bool jedeFlaeche, double flaecheAlleM2,
                                       double flaecheZ1M2)
        {
            if (jedeFlaeche && flaecheAlleM2 > 0) return flaecheZ1M2 / flaecheAlleM2;
            return summeAlleKwh > 0 ? summeZ1Kwh / summeAlleKwh : 0.0;
        }

        /// <summary>
        /// Die Tagesmitte der Zapfung [h]: Schwerpunkt der Stundensummen über alle Tage,
        /// <c>m = Σ_h (h + ½) · E_h / Σ_h E_h</c>; ohne Zapfung 12 Uhr.
        /// </summary>
        internal static double Tagesmitte(IEnumerable<IReadOnlyList<double>> zapfreihenKwh)
        {
            var e = new double[Zapfkalender.STUNDEN_TAG];
            if (zapfreihenKwh != null)
                foreach (IReadOnlyList<double> r in zapfreihenKwh)
                {
                    if (r == null) continue;
                    for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                        for (int d = 0; d < Zapfkalender.TAGE; d++)
                            e[h] += r[d * Zapfkalender.STUNDEN_TAG + h];
                }
            double zaehler = 0.0, nenner = 0.0;
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                zaehler += (h + 0.5) * e[h];
                nenner += e[h];
            }
            return nenner > 0 ? zaehler / nenner : TAGESMITTE_OHNE_ZAPFUNG_H;
        }

        /// <summary>
        /// Das Laufzeitfenster eines Tages: 24 Belegungen 0 … 1 (Anteil der Stunde, in der die
        /// Zirkulation läuft); Σ = t_Lauf.
        /// </summary>
        internal static double[] Laufzeitfenster(double laufzeitH, double tagesmitteH)
        {
            if (double.IsNaN(laufzeitH) || !(laufzeitH > 0) || laufzeitH > Zapfkalender.STUNDEN_TAG)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                    "Nicht rechenbar — die Laufzeit der Zirkulation liegt nicht in (0; 24] h.");
            double beginn = Laufzeitbeginn(laufzeitH, tagesmitteH);
            double ende = beginn + laufzeitH;
            var fenster = new double[Zapfkalender.STUNDEN_TAG];
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                double von = Math.Max(h, beginn);
                double bis = Math.Min(h + 1, ende);
                fenster[h] = bis > von ? bis - von : 0.0;
            }
            return fenster;
        }

        /// <summary>
        /// Der Beginn [h] des Laufzeitfensters: <c>⌊m − t_Lauf/2 + ½⌋</c>, an den Tagesrand
        /// geschoben (N7 (g)). Auch die Auslegung liest ihn, damit Bilanz und Auslegung dieselben
        /// Laufzeitstunden haben.
        /// </summary>
        internal static double Laufzeitbeginn(double laufzeitH, double tagesmitteH)
        {
            double beginn = Math.Floor(tagesmitteH - laufzeitH / 2.0 + 0.5);
            if (beginn < 0) beginn = 0;
            if (beginn + laufzeitH > Zapfkalender.STUNDEN_TAG) beginn = Math.Floor(Zapfkalender.STUNDEN_TAG - laufzeitH);
            return beginn;
        }

        /// <summary>
        /// Die Stundenreihe eines Jahresverlusts [kWh] über das Laufzeitfenster:
        /// <c>q_h = Q / (365 · t_Lauf) · Fenster(h)</c>, Σ_h q_h = Q.
        /// </summary>
        internal static Bilanzreihe Reihe(double jahresverlustKwh, double laufzeitH, double[] fenster)
        {
            if (fenster == null || fenster.Length != Zapfkalender.STUNDEN_TAG)
                throw new ArgumentException("Das Laufzeitfenster trägt nicht 24 Stunden.", nameof(fenster));
            double leistung = jahresverlustKwh / (Zapfkalender.TAGE * laufzeitH);
            var s = new double[Zapfkalender.STUNDEN_JAHR];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    s[d * Zapfkalender.STUNDEN_TAG + h] = leistung * fenster[h];
            return new Bilanzreihe(s);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static int Lage(ZapfLeitungslage? lage, Parametersatz ps, Herkunftsprotokoll prot)
        {
            if (lage.HasValue)
            {
                prot?.Vermerken("", ZapfFeld.ZIRKULATION_LAGE, (int)lage.Value, "", Wertstatus.Ueberschrieben, null);
                return (int)lage.Value;
            }
            ZapfParameterwert pw = ps.Lies(ZapfParameter.ZIRKULATION_LAGE);
            if (pw.Wert != (int)ZapfLeitungslage.InnerhalbHuelle && pw.Wert != (int)ZapfLeitungslage.AusserhalbHuelle)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                    "Nicht rechenbar — der Parameter der Leitungslage ist weder 1 noch 2.");
            prot?.Vermerken("", ZapfFeld.ZIRKULATION_LAGE, pw.Wert, "", Wertstatus.Vorgabe, pw.Herkunft,
                            "Parameter " + ZapfParameter.ZIRKULATION_LAGE);
            return (int)pw.Wert;
        }

        private static double ProjektOderParameter(double? projektwert, string schluessel, Parametersatz ps,
                                                   Herkunftsprotokoll prot, string feld, string einheit)
        {
            if (projektwert.HasValue)
            {
                prot?.Vermerken("", feld, projektwert.Value, einheit, Wertstatus.Ueberschrieben, null);
                return projektwert.Value;
            }
            ZapfParameterwert pw = ps.Lies(schluessel);
            prot?.Vermerken("", feld, pw.Wert, einheit, Wertstatus.Vorgabe, pw.Herkunft, "Parameter " + schluessel);
            return pw.Wert;
        }

        private static double NichtNegativ(double w, string was)
        {
            if (double.IsNaN(w) || double.IsInfinity(w) || w < 0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ZirkulationUngueltig, "",
                    "Nicht rechenbar — " + was + " ist negativ oder keine endliche Zahl ("
                    + w.ToString(CultureInfo.InvariantCulture) + ").");
            return w;
        }
    }
}
