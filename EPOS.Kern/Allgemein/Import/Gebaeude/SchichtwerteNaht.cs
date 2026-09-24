using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Naht „U-Wert und Masse aus Schichten"</b> des Gebäudeimports — sie rechnet nicht
    /// selbst, sondern ruft die EINE Stelle im Kern: <see cref="Bauteilreduktion"/> (Stufe G3,
    /// Mehrzonenkonzept 3.4, DIN EN ISO 6946). Hier steht nur die Übersetzung vom Abbild in deren
    /// Eingang.
    ///
    /// <para><b>Drei Übersetzungen.</b> (1) Die Schichtfolge: Das Abbild führt gbXML-treu außen →
    /// innen (Annahme „erste Schicht außen", Datenaustauschkonzept 3.7), die Reduktion erwartet
    /// innen → außen — umgekehrt wird hier. (2) Die Randbedingung des Abbilds wird zum
    /// <see cref="Bauteilrand"/>. (3) Eine Schicht nur mit R-Wert ist KEINE ruhende Luftschicht
    /// nach Tabelle 8, sondern ein eingetragener Widerstand: Für einen masselosen Aufbau gilt
    /// U = 1/(R_si + R der vollständigen Schichten + Σ R der übrigen + R_se); eine Masse wird ihm
    /// nicht zugeschrieben (Datenaustauschkonzept 3.6, Punkt 2).</para>
    ///
    /// <para><b>Nie eine Ausnahme nach außen.</b> Das Stoffwertband prüft
    /// <see cref="Bauteilreduktion.Pruefen"/> und wirft dabei
    /// <see cref="GebaeudeModellException"/>; hier wird sie gefangen, der Grund geht als Text
    /// zurück und wird im Import zur Meldung — der Ablauf zeigt nichts an und bricht nicht ab.</para>
    ///
    /// <para>Ein eingetragener U-Wert (<c>Construction/U-value</c>) hat immer Vorrang
    /// (Mehrzonenkonzept 3.4); diese Naht wird nur für Bauteile ohne ihn gefragt.</para>
    /// </summary>
    internal static class SchichtwerteNaht
    {
        /// <summary>
        /// U-Wert [W/(m²K)] eines Aufbaus aus seinen Schichten; <c>null</c>, wenn er nicht zu
        /// rechnen ist — kein vollständiger oder masseloser Aufbau, keine Neigung, keine bekannte
        /// Randbedingung, oder die Stoffwerte liegen außerhalb des Bandes (dann steht der Grund in
        /// <paramref name="grund"/>).
        /// </summary>
        /// <param name="aufbau">Der Aufbau.</param>
        /// <param name="neigungGrad">Neigung des Bauteils: 0° = Dach, 90° = Wand, 180° = Boden — sie wählt R_si.</param>
        /// <param name="randbedingung">Randbedingung der Außenseite — sie wählt R_se.</param>
        /// <param name="grund">Der Text der abgelehnten Stoffwertprüfung; sonst <c>null</c>.</param>
        internal static double? UWertAusSchichten(AbbildAufbau aufbau, double? neigungGrad, Randbedingung randbedingung, out string grund)
        {
            grund = null;
            if (aufbau == null || aufbau.Schichten.Count == 0) return null;
            if (aufbau.Status != Aufbaustatus.Vollstaendig && aufbau.Status != Aufbaustatus.Masselos) return null;
            if (!(neigungGrad >= 0.0 && neigungGrad <= 180.0)) return null;
            Bauteilrand? rand = Rand(randbedingung);
            if (!rand.HasValue) return null;

            try
            {
                double neigung = neigungGrad.Value;
                if (aufbau.Status == Aufbaustatus.Vollstaendig)
                    return Bauteilreduktion.UWertAusSchichten(Vollstaendige(aufbau), neigung, rand.Value, aufbau.Kennung).U_WM2K;

                // Masselos: die vollständigen Schichten über die Reduktion, die übrigen mit ihrem
                // eingetragenen R-Wert, sonst d/λ — keine Luftschicht nach Tabelle 8, keine Masse.
                (double rSi, double rSe) = Bauteilreduktion.Uebergangswiderstaende(neigung, rand.Value, aufbau.Kennung);
                Waermestromrichtung richtung = Bauteilreduktion.RichtungAusNeigung(neigung, aufbau.Kennung);
                List<Schicht> vollstaendig = Vollstaendige(aufbau);
                double r = vollstaendig.Count > 0
                    ? Bauteilreduktion.Kennwerte(vollstaendig, richtung, rSi, rSe, aufbau.Kennung).R_M2KW
                    : 0.0;
                foreach (AbbildSchicht s in aufbau.Schichten)
                {
                    if (s.Vollstaendig) continue;
                    r += s.RWertM2KW > 0.0 ? s.RWertM2KW.Value : s.DickeM.Value / s.LambdaWmK.Value;
                }
                return 1.0 / (rSi + r + rSe);
            }
            catch (GebaeudeModellException ex)
            {
                grund = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Flächenbezogene Wärmekapazität Σ ρ·c·d [J/(m²K)] des GANZEN Aufbaus, wenn er VOLLSTÄNDIG
        /// ist; <c>null</c> für jeden anderen (ein masseloser Aufbau trägt keine Masse) und bei
        /// Stoffwerten außerhalb des Bandes. Die Bauart des Imports nimmt nur die raumseitigen
        /// Schichten (<see cref="WirksameKapazitaetAusSchichten"/>).
        /// </summary>
        internal static double? KapazitaetAusSchichten(AbbildAufbau aufbau)
        {
            if (aufbau == null || aufbau.Status != Aufbaustatus.Vollstaendig || aufbau.Schichten.Count == 0) return null;
            try
            {
                // Die Richtung wählt nur den Widerstand ruhender Luftschichten — die gibt es hier nicht.
                return Bauteilreduktion.Kennwerte(Vollstaendige(aufbau), Waermestromrichtung.Horizontal, 0.0, 0.0, aufbau.Kennung)
                                       .Kapazitaet_JM2K;
            }
            catch (GebaeudeModellException)
            {
                return null;
            }
        }

        /// <summary>Raumseitige Tiefe [m], bis zu der Schichten zur wirksamen Speichermasse zählen (Umsetzungskonzept 3.4, Zeile Bauweise).</summary>
        internal const double WIRKSAME_TIEFE_M = 0.10;

        /// <summary>
        /// <b>Die wirksame flächenbezogene Wärmekapazität</b> C″ = Σ ρ·c·d [J/(m²K)] der
        /// RAUMSEITIGEN Schichten bis <paramref name="tiefeM"/> (Umsetzungskonzept 3.4, Zeile
        /// Bauweise: „raumseitige Schichten bis 10 cm"): von innen gezählt, die Schicht an der
        /// Grenze nur mit ihrem Anteil. <c>null</c> für einen nicht vollständigen Aufbau und bei
        /// Stoffwerten außerhalb des Bandes (<see cref="Bauteilreduktion.Pruefen"/>).
        ///
        /// <para>Gerechnet wird an der EINEN Stelle des Kerns —
        /// <see cref="Bauteilreduktion.FlaechenbezogeneKapazitaet"/> je Schicht; hier steht nur die
        /// Tiefengrenze. Raumseitig ist nach der gbXML-Hausannahme die LETZTE Schicht der Datei
        /// (erste Schicht außen, Datenaustauschkonzept 3.7).</para>
        /// </summary>
        internal static double? WirksameKapazitaetAusSchichten(AbbildAufbau aufbau, double tiefeM = WIRKSAME_TIEFE_M)
        {
            if (aufbau == null || aufbau.Status != Aufbaustatus.Vollstaendig || aufbau.Schichten.Count == 0) return null;
            if (!(tiefeM > 0.0)) return null;
            try
            {
                List<Schicht> schichten = Vollstaendige(aufbau);   // raumseitig zuerst
                Bauteilreduktion.Pruefen(schichten, aufbau.Kennung);
                double rest = tiefeM, c = 0.0;
                foreach (Schicht s in schichten)
                {
                    if (rest <= 0.0) break;
                    double d = Math.Min(s.Dicke_M, rest);
                    c += Bauteilreduktion.FlaechenbezogeneKapazitaet(s with { Dicke_M = d });
                    rest -= d;
                }
                return c;
            }
            catch (GebaeudeModellException)
            {
                return null;
            }
        }

        /// <summary>Die vollständigen Schichten als Eingang der Reduktion, raumseitig zuerst.</summary>
        private static List<Schicht> Vollstaendige(AbbildAufbau aufbau)
        {
            var liste = new List<Schicht>(aufbau.Schichten.Count);
            foreach (AbbildSchicht s in aufbau.Schichten)
                if (s.Vollstaendig)
                    liste.Add(new Schicht(s.DickeM.Value, s.LambdaWmK.Value, s.RhoKgM3.Value, s.CpJkgK.Value));
            if (aufbau.Richtung == Schichtrichtung.AussenNachInnen) liste.Reverse();
            return liste;
        }

        private static Bauteilrand? Rand(Randbedingung randbedingung)
        {
            switch (randbedingung)
            {
                case Randbedingung.Aussenluft: return Bauteilrand.Aussenluft;
                case Randbedingung.Erdreich: return Bauteilrand.Erdreich;
                case Randbedingung.Unbeheizt: return Bauteilrand.Unbeheizt;
                case Randbedingung.Innen: return Bauteilrand.Innen;
                default: return null;
            }
        }
    }
}
