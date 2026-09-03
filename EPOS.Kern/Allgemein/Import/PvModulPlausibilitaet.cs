using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Plausibilitaetspruefung der vier Modulkenngroessen, die aus dem PV-Katalog in
    /// die Ertragsrechnung gehen: <c>alpha_SC</c>, <c>beta_OC</c>, <c>gamma_PMP</c>
    /// (im Modell <c>m_Temp_Coeff_Pmax</c>) und <c>T_NOCT</c>.
    /// </summary>
    /// <remarks>
    /// EINHEITEN - so, wie Tab_PV_STAMM sie fuehrt und wie die Units-Zeile der
    /// CEC-Tabelle sie ausweist:
    ///   alpha_SC   A/K     Temperaturkoeffizient des Kurzschlussstroms, positiv
    ///   beta_OC    V/K     Temperaturkoeffizient der Leerlaufspannung, negativ
    ///   gamma_PMP  %/K     Temperaturkoeffizient der Leistung, negativ
    ///   T_NOCT     Grad C  Nennbetriebstemperatur der Zelle
    ///
    /// TYPISCHE WERTE kristalliner Module (CEC-Bestand):
    ///   alpha_SC  +0,002 bis +0,006 A/K
    ///   beta_OC   -0,10 bis -0,15 V/K
    ///   gamma_PMP -0,30 bis -0,45 %/K
    ///   T_NOCT    42 bis 48 Grad C
    ///
    /// ANLASS (Befund 02.09.2026): Bis 27.03.2026 schrieb PhotovoltaikCtrl.Update den
    /// Kurzschlussstrom in alpha_SC, beta_OC und T_NOCT (Kopierfehler aus 5d8122a,
    /// behoben in 4e80222). Der Datenbestand traegt diese Signatur bis heute. Sie wird
    /// hier als harter Fehler erkannt, damit sie ueber Import oder Katalogdialog nicht
    /// erneut entsteht.
    ///
    /// Die Fenster <see cref="NOCT_MIN"/>, <see cref="NOCT_MAX"/> und
    /// <see cref="GAMMA_MIN"/> sind WERTGLEICH mit den gleichnamigen Konstanten der
    /// Ertragsrechnung (SimulationPV.NOCT_MIN/NOCT_MAX/GAMMA_MIN, Paket A), aber
    /// bewusst eigenstaendig definiert: die Eingangspruefung von Import und Katalog
    /// soll nicht an den internen Konstanten des Rechenkerns haengen.
    /// </remarks>
    public static class PvModulPlausibilitaet
    {
        /// <summary>Untergrenze T_NOCT in Grad C (wertgleich SimulationPV.NOCT_MIN).</summary>
        public const double NOCT_MIN = 20.0;

        /// <summary>Obergrenze T_NOCT in Grad C (wertgleich SimulationPV.NOCT_MAX).</summary>
        public const double NOCT_MAX = 60.0;

        /// <summary>Untergrenze alpha_SC in A/K. 0 selbst bedeutet "nicht vorhanden".</summary>
        public const double ALPHA_MIN = 0.0;

        /// <summary>Obergrenze alpha_SC in A/K; darueber liegt meist eine %/K-Angabe vor.</summary>
        public const double ALPHA_MAX = 0.05;

        /// <summary>Untergrenze beta_OC in V/K; darunter liegt meist eine mV/K-Angabe vor.</summary>
        public const double BETA_MIN = -0.5;

        /// <summary>Obergrenze beta_OC in V/K. 0 selbst bedeutet "nicht vorhanden".</summary>
        public const double BETA_MAX = 0.0;

        /// <summary>Untergrenze gamma_PMP in %/K (wertgleich SimulationPV.GAMMA_MIN).</summary>
        public const double GAMMA_MIN = -1.0;

        /// <summary>Obergrenze gamma_PMP in %/K. 0 selbst bedeutet "nicht gepflegt".</summary>
        public const double GAMMA_MAX = 0.0;

        /// <summary>Zusatz fuer Meldungen zu Feldern, die der Aufrufer nicht korrigieren kann.</summary>
        private const string NICHT_PFLEGBAR =
            " (im Katalogdialog nicht pflegbar - per Neuimport oder Reparaturskript berichtigen)";

        /// <summary>
        /// Ergebnis der Pruefung. <see cref="Fehler"/> sperrt das Schreiben,
        /// <see cref="Warnungen"/> ist ein Hinweis mit Rueckfrage.
        /// </summary>
        public sealed class Befund
        {
            /// <summary>Harte Verstoesse - der Datensatz darf so nicht geschrieben werden.</summary>
            public List<string> Fehler = new List<string>();

            /// <summary>Auffaelligkeiten, die der Anwender bestaetigen kann.</summary>
            public List<string> Warnungen = new List<string>();

            /// <summary>Wahr, solange kein harter Verstoss vorliegt.</summary>
            public bool Ok => Fehler.Count == 0;
        }

        /// <summary>Zahl fuer die Meldungstexte - kulturinvariant, damit die Meldung reproduzierbar ist.</summary>
        private static string Z(double wert)
        {
            return wert.ToString("0.####", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Traegt eine Meldung ein: als Fehler, oder - wenn der Aufrufer das Feld gar
        /// nicht pflegen kann - als Hinweis mit dem entsprechenden Zusatz.
        /// </summary>
        private static void Melde(Befund b, bool alsFehler, string text)
        {
            if (alsFehler) b.Fehler.Add(text + ".");
            else b.Warnungen.Add(text + NICHT_PFLEGBAR + ".");
        }

        /// <summary>Meldungstext der Kopierfehler-Signatur fuer die angegebenen Felder.</summary>
        private static string SignaturText(List<string> felder, double iKurzschluss)
        {
            return "alpha_SC/beta_OC/T_NOCT traegt den Wert des Kurzschlussstroms "
                + "(Kopierfehler-Signatur): " + string.Join(", ", felder)
                + " = I_Kurzschluss = " + Z(iKurzschluss);
        }

        /// <summary>
        /// Prueft die vier Kenngroessen eines Modul-Datensatzes. Der Stamm-Controller
        /// erbt vom Modell und kann direkt uebergeben werden.
        /// </summary>
        /// <param name="m">Zu pruefender Datensatz.</param>
        /// <param name="alphaBetaPflegbar">
        /// Ob der Aufrufer alpha_SC und beta_OC ueberhaupt berichtigen kann.
        /// <c>true</c> (Vorgabe) fuer den Import: dort liegen richtige Werte vor, ein
        /// Verstoss ist deshalb ein harter Fehler.
        /// <c>false</c> fuer den Katalogdialog Form_AdminPV: der hat fuer alpha_SC und
        /// beta_OC keine Eingabefelder - nur T_NOCT ist seit E1.2 pflegbar. Befund
        /// 02.09.2026: sechs Bestandssaetze tragen die Kopierfehler-Signatur; als
        /// harter Fehler wuerde sie jedes Speichern dieser Saetze sperren, ohne dass
        /// der Anwender im Dialog etwas dagegen ausrichten koennte. Die alpha_SC- und
        /// beta_OC-Verstoesse werden dann zu Hinweisen, alles zu T_NOCT bleibt Fehler.
        /// </param>
        public static Befund Pruefe(PhotovoltaikModel m, bool alphaBetaPflegbar = true)
        {
            Befund b = new Befund();
            if (m == null)
            {
                b.Fehler.Add("Kein Modul-Datensatz uebergeben.");
                return b;
            }

            // ---- Fehler -------------------------------------------------------
            // Kopierfehler-Signatur: der Kurzschlussstrom steht in einem der drei
            // Felder. Exakter Vergleich, weil genau eine Zuweisung die Ursache war -
            // eine Toleranz wuerde echte Grenzfaelle mitsperren. Die Signatur wird
            // nach Pflegbarkeit getrennt gemeldet: T_NOCT laesst sich im Dialog
            // berichtigen, alpha_SC und beta_OC nicht.
            List<string> kopiertAlphaBeta = new List<string>();
            List<string> kopiertNoct = new List<string>();
            if (m.m_alpha_SC != 0.0 && m.m_alpha_SC == m.m_I_Kurzschluss) kopiertAlphaBeta.Add("alpha_SC");
            if (m.m_beta_OC != 0.0 && m.m_beta_OC == m.m_I_Kurzschluss) kopiertAlphaBeta.Add("beta_OC");
            if (m.m_T_NOCT != 0.0 && m.m_T_NOCT == m.m_I_Kurzschluss) kopiertNoct.Add("T_NOCT");

            List<string> signaturAlsFehler = new List<string>(kopiertNoct);
            if (alphaBetaPflegbar) signaturAlsFehler.InsertRange(0, kopiertAlphaBeta);
            if (signaturAlsFehler.Count > 0)
                Melde(b, true, SignaturText(signaturAlsFehler, m.m_I_Kurzschluss));
            if (!alphaBetaPflegbar && kopiertAlphaBeta.Count > 0)
                Melde(b, false, SignaturText(kopiertAlphaBeta, m.m_I_Kurzschluss));

            if (m.m_Temp_Coeff_Pmax > GAMMA_MAX)
            {
                b.Fehler.Add("Temperaturkoeffizient Pmax muss negativ sein (gamma_PMP = "
                    + Z(m.m_Temp_Coeff_Pmax) + " %/K). Ein positiver Wert ergaebe Mehrertrag bei Waerme.");
            }
            if (m.m_Temp_Coeff_Pmax < GAMMA_MIN)
            {
                b.Fehler.Add("gamma_PMP = " + Z(m.m_Temp_Coeff_Pmax) + " %/K liegt unter der Untergrenze "
                    + Z(GAMMA_MIN) + " %/K.");
            }
            if (m.m_alpha_SC < ALPHA_MIN)
            {
                Melde(b, alphaBetaPflegbar, "alpha_SC = " + Z(m.m_alpha_SC)
                    + " A/K darf nicht negativ sein - der Kurzschlussstrom steigt mit der Temperatur");
            }
            if (m.m_beta_OC > BETA_MAX)
            {
                Melde(b, alphaBetaPflegbar, "beta_OC = " + Z(m.m_beta_OC)
                    + " V/K darf nicht positiv sein - die Leerlaufspannung faellt mit der Temperatur");
            }
            if (m.m_T_NOCT != 0.0 && (m.m_T_NOCT < NOCT_MIN || m.m_T_NOCT > NOCT_MAX))
            {
                b.Fehler.Add("T_NOCT = " + Z(m.m_T_NOCT) + " Grad C liegt ausserhalb des Fensters "
                    + Z(NOCT_MIN) + " bis " + Z(NOCT_MAX) + " Grad C.");
            }

            // ---- Warnungen ----------------------------------------------------
            if (m.m_alpha_SC > ALPHA_MAX)
            {
                b.Warnungen.Add("alpha_SC = " + Z(m.m_alpha_SC) + " A/K ist ungewoehnlich gross (Grenze "
                    + Z(ALPHA_MAX) + " A/K) - vermutlich %/K statt A/K.");
            }
            if (m.m_beta_OC < BETA_MIN)
            {
                b.Warnungen.Add("beta_OC = " + Z(m.m_beta_OC) + " V/K ist ungewoehnlich gross (Grenze "
                    + Z(BETA_MIN) + " V/K) - vermutlich mV/K statt V/K.");
            }
            if (m.m_T_NOCT == 0.0)
            {
                b.Warnungen.Add("T_NOCT nicht vorhanden - Simulation rechnet mit 45 Grad C.");
            }
            if (m.m_Temp_Coeff_Pmax == 0.0)
            {
                b.Warnungen.Add("gamma_PMP nicht gepflegt - Simulation rechnet ohne Temperaturkorrektur.");
            }
            if (m.m_alpha_SC == 0.0)
            {
                b.Warnungen.Add("alpha_SC nicht vorhanden.");
            }
            if (m.m_beta_OC == 0.0)
            {
                b.Warnungen.Add("beta_OC nicht vorhanden.");
            }

            return b;
        }

        /// <summary>Mehrzeiliger Text fuer die Meldung: erst die Fehler, dann die Hinweise.</summary>
        public static string Meldung(Befund b)
        {
            if (b == null) return string.Empty;

            List<string> zeilen = new List<string>();
            if (b.Fehler.Count > 0)
            {
                zeilen.Add("Fehler:");
                foreach (string f in b.Fehler) zeilen.Add("  - " + f);
            }
            if (b.Warnungen.Count > 0)
            {
                if (zeilen.Count > 0) zeilen.Add(string.Empty);
                zeilen.Add("Hinweise:");
                foreach (string w in b.Warnungen) zeilen.Add("  - " + w);
            }
            return string.Join(Environment.NewLine, zeilen);
        }
    }
}
