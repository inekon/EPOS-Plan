using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Importregel für Kühlblöcke</b> (Stufe KU2 der Kühlung; Prüfaufgabe K22, Entscheid E27;
    /// Kühlkonzept 5.1, Festlegung 4; <c>Glossar_Lokalisierung.md</c> Abschnitt 6).
    ///
    /// <para><b>Der Befund.</b> Nicht jeder Block mit Betriebsart 2 einer VDI-3805-Datei (Blatt 22,
    /// Kennlinienkopf <c>710.09</c>) beschreibt einen Kühlbetrieb: Rund ein Drittel der Kühlblöcke der
    /// Herstellerdateien liegt in HEIZLAGE — Vorlauf ab 30 °C oder eine Quelle, die in jedem Punkt
    /// kälter ist als der Vorlauf —, und führt die Kälteleistung am Verdampfer IM HEIZBETRIEB, keinen
    /// EER. Einzelne Datensätze tragen auf der Temperaturachse die Kaltwassertemperatur
    /// (VERTAUSCHTE ACHSEN). Bis KU2 übernahm der Import beides ungeprüft in die Kühltabelle.</para>
    ///
    /// <para><b>Die Regel:</b> Ein solcher Block wird beim Import <b>benannt abgelehnt</b> — nicht
    /// übernommen und im Leseprotokoll gemeldet (<c>IMP_KAT_PROT_KUEHLBLOCK_*</c>), nie still als EER
    /// gelesen und nie umgedeutet. Die Achsenprüfung ist dieselbe wie im Lauf
    /// (<see cref="Kuehlkennlinie.BlockBefund"/>). Schon gespeicherte Sätze ändert die Regel nicht:
    /// Der Lauf lehnt einen solchen Block benannt ab, der Katalog behält ihn, bis ihn ein neuer Import
    /// ersetzt.</para>
    ///
    /// <para><b>Ein Block</b> ist eine Kombination aus Vorlauf und Laststufe — so steht er in der
    /// Datei (ein Kopf <c>710.09</c> je Vorlauf und Laststufe).</para>
    /// </summary>
    public static class KuehlblockPruefung
    {
        /// <summary>Ein abgelehnter Block: Vorlauf, Laststufe und Befund.</summary>
        public readonly struct Abgelehnt
        {
            public Abgelehnt(int vorlauf, int last, KuehlblockBefund befund)
            {
                Vorlauf = vorlauf;
                Last = last;
                Befund = befund;
            }

            public int Vorlauf { get; }
            public int Last { get; }
            public KuehlblockBefund Befund { get; }
        }

        /// <summary>
        /// Trennt die Kühlzeilen eines Satzes in übernommene und abgelehnte Blöcke. Die Reihenfolge
        /// der übernommenen Zeilen bleibt die der Datei.
        /// </summary>
        /// <param name="kuehlung">Die Kühlzeilen, wie <c>WaermepumpenImport</c> sie liest.</param>
        /// <param name="abgelehnt">Die abgelehnten Blöcke in der Reihenfolge ihres ersten Auftretens.</param>
        public static List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> Pruefen(
            IEnumerable<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehlung,
            out List<Abgelehnt> abgelehnt)
        {
            var zeilen = kuehlung == null
                ? new List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)>()
                : kuehlung.ToList();

            var befund = new Dictionary<(int, int), KuehlblockBefund>();
            abgelehnt = new List<Abgelehnt>();
            foreach (var block in zeilen.GroupBy(z => (z.Vorlauf, z.Last)))
            {
                KuehlblockBefund b = Kuehlkennlinie.BlockBefund(block.Key.Vorlauf, block.Select(z => z.Temperatur));
                befund[block.Key] = b;
                if (b != KuehlblockBefund.Gueltig)
                    abgelehnt.Add(new Abgelehnt(block.Key.Vorlauf, block.Key.Last, b));
            }

            return zeilen.Where(z => befund[(z.Vorlauf, z.Last)] == KuehlblockBefund.Gueltig).ToList();
        }
    }
}
