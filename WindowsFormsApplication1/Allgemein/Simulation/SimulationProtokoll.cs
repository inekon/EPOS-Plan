using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Protokoll- und Fehlerkanal der Simulation (Paket 8, Konzept 13.4).
    ///
    /// AUSGANGSLAGE. Die Engine meldete Grenzfälle über <c>MessageBox</c> — im
    /// unbeaufsichtigten Lauf (Variantenbericht, Referenzlauf-Suite) blockiert das den
    /// Prozess bis zum Timeout. Paket 5 (Befund N10) und Paket 6 (Befund N8) haben
    /// dafür bereits einen EINSTUFIGEN Kanal eingeführt: <c>Fehlertext</c> am
    /// Erzeugermodul → <see cref="SimulationControl.Fehlertext"/> →
    /// <c>SimulationRunner.Simuliere(out fehler)</c>. Paket 8 verallgemeinert ihn.
    ///
    /// ZWEI STUFEN, bewusst getrennt:
    ///
    ///   <b>Fehler</b>      — der Lauf ist abgebrochen, es gibt KEIN vollständiges
    ///                        Ergebnis. <c>SimulationRunner.SimuliereUndSpeichere</c>
    ///                        speichert dann nichts und liefert -1.
    ///   <b>Warnungen</b>   — gerechnet wurde, aber mit einer Ersatzannahme
    ///                        (Standardprofil statt hinterlegtem Typ, Anteil 0 …).
    ///   <b>Hinweise</b>    — der Lauf ist vollwertig, eine Randbedingung ist aber
    ///                        erwähnenswert (Extrapolation der WP-Kennlinie,
    ///                        ΔT-Rückfall, Senken-Rückfall, Zwischenstufen-Meldungen).
    ///
    /// AMBIENTER ZUGRIFF. Der Kanal hängt als <see cref="Aktuell"/> am Prozess und
    /// nicht als Parameter an jeder Signatur. Grund: Die meldenden Stellen liegen bis
    /// zu fünf Aufrufebenen tief (<c>berechne_wptherm</c> in der Stundenschleife) und
    /// in Klassen, die <c>SimulationControl</c> gar nicht kennt
    /// (<see cref="SimulationWaermebedarf"/>, <see cref="SimulationStrombedarf"/> —
    /// beide laufen VOR der Kaskade, aufgerufen aus Formular bzw. Runner). Eine
    /// durchgereichte Referenz hätte rund vierzig Signaturen berührt und damit genau
    /// den Rechenpfad angefasst, den Paket 8 nicht anfassen darf.
    ///
    /// EINDEUTIGKEIT — die tragende Invariante (berichtigt in der Nacharbeit, Befund N7).
    /// Ein prozessweiter Kanal ist nur dann eindeutig, wenn zu jedem Zeitpunkt HÖCHSTENS
    /// EIN Simulationslauf im Prozess läuft. Das ist heute so, aber NICHT, weil die
    /// Anwendung einläufig wäre — der Berichtspfad rechnet sehr wohl auf einem
    /// ThreadPool-Thread (<c>BerichtsDatenSammler.Sammle</c> in <c>Task.Run</c>, gerufen
    /// aus <c>Form_Bericht</c>, <c>Form_Wirtschaftlichkeit</c> und
    /// <c>Form_WirtschaftlichkeitVerlauf</c>). Getragen wird die Invariante von zwei
    /// Dingen:
    ///
    ///   1. <b>Modalität.</b> Alle drei Formulare werden ausschließlich über
    ///      <c>ShowDialog()</c> geöffnet (aus <c>Form_Variantentest</c> bzw. aus
    ///      <c>Form_Wirtschaftlichkeit</c> heraus). Solange einer dieser Dialoge offen
    ///      ist, kann der MDI-Thread keinen zweiten Lauf starten — der Simulationsknopf
    ///      der Detailansicht ist nicht erreichbar. <b>Wer eines dieser Formulare je
    ///      nicht-modal öffnet, bricht diese Invariante</b>, und zwar für den Kanal hier
    ///      wie für <c>DataRepository._stillTiefe</c>, das prozessweit dieselbe Annahme
    ///      trifft.
    ///   2. <b>Prozessgrenze.</b> Die Referenzlauf-Suite rechnet jedes Projekt in einem
    ///      EIGENEN Kindprozess.
    ///
    /// Vorgemerkte Härtung (nicht umgesetzt, siehe Paket-8-Protokoll, offene Punkte):
    /// <c>[ThreadStatic]</c> oder <c>AsyncLocal</c> würde die Invariante überflüssig
    /// machen — aber nur, wenn ALLE Lese-/Schreibpaare threadrein sind. Sie sind es
    /// heute nicht: <c>Form_Simulation_Detail</c> schreibt über die Engine und liest
    /// <see cref="Aktuell"/> anschließend auf dem UI-Thread, der Berichtspfad schreibt
    /// UND liest auf dem Worker-Thread. Eine halbe Umstellung wäre schlechter als die
    /// benannte Invariante.
    ///
    /// KONSOLE BLEIBT. Jeder Eintrag geht zusätzlich auf <c>Console.WriteLine</c>:
    /// Die Lauf-Protokolle der Referenzlauf-Suite lesen die Konsolenausgabe der
    /// Kindprozesse mit, und das soll so bleiben.
    ///
    /// ERGEBNISNEUTRAL. Diese Klasse rechnet nichts. Sie sammelt Text.
    /// </summary>
    public sealed class SimulationProtokoll
    {
        private readonly object _sperre = new object();
        private readonly List<string> _hinweise = new List<string>();
        private readonly List<string> _warnungen = new List<string>();
        private readonly List<string> _fehler = new List<string>();

        /// <summary>Schlüssel bereits gemeldeter Einmal-Meldungen (siehe <see cref="HinweisEinmal"/>).</summary>
        private readonly HashSet<string> _einmal = new HashSet<string>(StringComparer.Ordinal);

        private static SimulationProtokoll _aktuell = new SimulationProtokoll();

        /// <summary>
        /// Der Kanal des laufenden bzw. zuletzt gelaufenen Simulationslaufs. Nie
        /// <c>null</c> — vor dem ersten Lauf steht hier ein leeres Protokoll, damit
        /// meldende Stellen keine Null-Prüfung brauchen.
        /// </summary>
        public static SimulationProtokoll Aktuell
        {
            get { return _aktuell; }
        }

        /// <summary>
        /// Beginnt einen neuen Lauf: leerer Kanal, der ab sofort unter
        /// <see cref="Aktuell"/> steht. Aufzurufen von jedem Einstiegspunkt eines
        /// Simulationslaufs (<c>SimulationRunner.Simuliere</c>,
        /// <c>Form_Simulation_Detail.btn_Simulation_Click</c>) — und zwar VOR der
        /// Bedarfsrechnung, denn auch <see cref="SimulationWaermebedarf"/> und
        /// <see cref="SimulationStrombedarf"/> melden hierüber.
        /// </summary>
        public static SimulationProtokoll NeuStarten()
        {
            _aktuell = new SimulationProtokoll();
            return _aktuell;
        }

        // =================================================================================
        // Lesen
        // =================================================================================

        public IList<string> Hinweise
        {
            get { lock (_sperre) { return _hinweise.ToArray(); } }
        }

        public IList<string> Warnungen
        {
            get { lock (_sperre) { return _warnungen.ToArray(); } }
        }

        public IList<string> Fehler
        {
            get { lock (_sperre) { return _fehler.ToArray(); } }
        }

        /// <summary>true, solange kein Eintrag im Fehlerkanal steht.</summary>
        public bool IstFehlerfrei
        {
            get { lock (_sperre) { return _fehler.Count == 0; } }
        }

        /// <summary>true, wenn überhaupt etwas zu melden ist.</summary>
        public bool HatEintraege
        {
            get { lock (_sperre) { return _fehler.Count + _warnungen.Count + _hinweise.Count > 0; } }
        }

        /// <summary>Zahl der Einträge, die dem Anwender nach dem Lauf angezeigt werden.</summary>
        public int AnzahlMeldungen
        {
            get { lock (_sperre) { return _fehler.Count + _warnungen.Count + _hinweise.Count; } }
        }

        // =================================================================================
        // Schreiben
        // =================================================================================

        /// <summary>Randbedingung, die den Lauf nicht einschränkt, aber erwähnenswert ist.</summary>
        public void Hinweis(string text)
        {
            Eintragen(_hinweise, "Hinweis", text);
        }

        /// <summary>
        /// Wie <see cref="Hinweis"/>, aber je <paramref name="schluessel"/> nur EINMAL
        /// je Lauf. Nötig für alles, was in der Stundenschleife auffällt: Die
        /// Extrapolation der Wärmepumpen-Kennlinie tritt in bis zu 8760 Stunden je
        /// Modul auf und würde das Protokoll sonst unlesbar machen.
        /// </summary>
        public void HinweisEinmal(string schluessel, string text)
        {
            lock (_sperre)
            {
                if (!_einmal.Add(schluessel ?? "")) return;
            }
            Eintragen(_hinweise, "Hinweis", text);
        }

        /// <summary>Der Lauf rechnet weiter, aber mit einer Ersatzannahme.</summary>
        public void Warnung(string text)
        {
            Eintragen(_warnungen, "Warnung", text);
        }

        /// <summary>Wie <see cref="Warnung"/>, aber je <paramref name="schluessel"/> nur einmal je Lauf.</summary>
        public void WarnungEinmal(string schluessel, string text)
        {
            lock (_sperre)
            {
                if (!_einmal.Add(schluessel ?? "")) return;
            }
            Eintragen(_warnungen, "Warnung", text);
        }

        /// <summary>
        /// Der Lauf ist abgebrochen. Heißt bewusst nicht <c>Fehler</c> — so hieße die
        /// gleichnamige Eigenschaft.
        /// </summary>
        public void Fehlermeldung(string text)
        {
            Eintragen(_fehler, "FEHLER", text);
        }

        private void Eintragen(List<string> ziel, string art, string text)
        {
            string zeile = (text ?? "").Replace("\r\n", " ").Replace("\n", " ").Trim();
            if (zeile.Length == 0) return;

            lock (_sperre) { ziel.Add(zeile); }

            // Die Referenzlauf-Suite liest die Konsolenausgabe der Kindprozesse mit;
            // die vorhandenen Console-Meldungen der Engine sollen erhalten bleiben.
            try { Console.WriteLine("Simulation " + art + ": " + zeile); }
            catch { /* eine fehlende Konsole darf keinen Rechenlauf abbrechen */ }
        }

        // =================================================================================
        // Ausgabe
        // =================================================================================

        /// <summary>
        /// Der gesammelte Kanal als Fließtext, Fehler zuerst.
        /// </summary>
        /// <param name="nurFehlerUndWarnungen">
        /// true = ohne die Hinweise. Das ist die Fassung, die
        /// <c>SimulationRunner.SimuliereUndSpeichere</c> in <c>out fehler</c> legt und
        /// die der Variantenbericht in seiner Hinweisliste ausgibt (Konzept 13.4).
        /// </param>
        public string AlsText(bool nurFehlerUndWarnungen = false)
        {
            lock (_sperre)
            {
                var sb = new StringBuilder();
                foreach (string z in _fehler) sb.AppendLine("Fehler: " + z);
                foreach (string z in _warnungen) sb.AppendLine("Warnung: " + z);
                if (!nurFehlerUndWarnungen)
                    foreach (string z in _hinweise) sb.AppendLine("Hinweis: " + z);
                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>
        /// Warnungen und Hinweise als Fließtext — der nicht abbrechende Teil, den
        /// <c>Form_Simulation_Detail</c> nach dem Lauf nicht-modal anzeigt.
        /// </summary>
        public string HinweistextFuerAnzeige()
        {
            lock (_sperre)
            {
                var sb = new StringBuilder();
                foreach (string z in _warnungen) sb.AppendLine("• " + z);
                foreach (string z in _hinweise) sb.AppendLine("• " + z);
                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>
        /// Die FEHLER als Fließtext — das Gegenstück zu
        /// <see cref="HinweistextFuerAnzeige"/> (Nacharbeit Paket 8, Befund N6).
        ///
        /// Gebraucht wird er dort, wo der Abbruchgrund allein zu wenig sagt: Ein Modul
        /// legt in seinen <c>Fehlertext</c> einen kurzen, allgemeinen Satz („Die
        /// Stromprofile konnten nicht berechnet werden"), die sprechende Diagnose mit
        /// Ausnahmetext und betroffenem Datensatz steht aber nur im Fehlerkanal. Ohne
        /// diese Ausgabe erreichte sie die Oberfläche nie.
        /// </summary>
        /// <param name="ausserdem">
        /// Optionaler Text, der bereits angezeigt wird (in aller Regel der
        /// Abbruchgrund). Kanaleinträge, die darin schon vorkommen oder ihn enthalten,
        /// werden weggelassen — sonst stünde derselbe Satz zweimal im Dialog.
        /// </param>
        public string FehlertextFuerAnzeige(string ausserdem = null)
        {
            lock (_sperre)
            {
                var sb = new StringBuilder();
                foreach (string z in _fehler)
                {
                    if (!string.IsNullOrEmpty(ausserdem) &&
                        (ausserdem.IndexOf(z, StringComparison.Ordinal) >= 0 ||
                         z.IndexOf(ausserdem, StringComparison.Ordinal) >= 0)) continue;
                    sb.AppendLine("• " + z);
                }
                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>Zahl der Warnungen und Hinweise (ohne Fehler).</summary>
        public int AnzahlWarnungenUndHinweise
        {
            get { lock (_sperre) { return _warnungen.Count + _hinweise.Count; } }
        }
    }
}
