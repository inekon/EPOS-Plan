using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Zeile der Kühlkennlinie, wie sie in <c>Tab_Kenndaten_Kuehlung</c> bzw.
    /// <c>Tab_Kenndaten_Kuehlung_STAMM</c> steht (Stufe KU2 der Kühlung; Kühlkonzept 5.1).
    ///
    /// <para><b><see cref="Eer"/> ist die Spalte <c>COP</c></b>: Sie führt das Kälteverhältnis,
    /// nie das Wärmeverhältnis (Prüfaufgabe K22, <c>Glossar_Lokalisierung.md</c> Abschnitt 6).
    /// Der Spaltenname bleibt eingefroren; im Kern heißt die Größe EER (Festlegung 4).</para>
    /// </summary>
    public readonly struct KuehlkennlinienZeile
    {
        public KuehlkennlinienZeile(int id, int vorlauf, int temperatur, double eer, double pkuehl, int? last)
        {
            ID = id;
            Vorlauf = vorlauf;
            Temperatur = temperatur;
            Eer = eer;
            Pkuehl = pkuehl;
            Last = last;
        }

        /// <summary>Zeilenkennung — sie ordnet Dubletten (die zuerst gespeicherte gilt).</summary>
        public int ID { get; }

        /// <summary>Kaltwasser-Vorlauf der Stützstelle [°C] — die Achse, die <c>Kuehl_Vorlauf</c> wählt.</summary>
        public int Vorlauf { get; }

        /// <summary>Außen- bzw. Quellentemperatur [°C] — die Rückkühlseite der Maschine (Festlegung 5).</summary>
        public int Temperatur { get; }

        /// <summary>Kälteverhältnis [—] aus der Spalte <c>COP</c> (K22).</summary>
        public double Eer { get; }

        /// <summary>Kälteleistung [kW] aus der Spalte <c>Pkuehl</c>.</summary>
        public double Pkuehl { get; }

        /// <summary>Laststufe [%]; <c>null</c> = ohne Laststufe.</summary>
        public int? Last { get; }
    }

    /// <summary>Befund eines Kühlblocks — Vorlauf mit seiner Temperaturachse (K22, Kühlkonzept 5.1 Festlegung 4).</summary>
    public enum KuehlblockBefund
    {
        /// <summary>Kaltwasserlage: ein Kühlbetrieb, dessen Kennzahl der EER ist.</summary>
        Gueltig,

        /// <summary>
        /// HEIZLAGE: Vorlauf ab <see cref="Kuehlkennlinie.KALTWASSER_VORLAUF_GRENZE"/> oder jede
        /// Stützstelle kälter als der Vorlauf — die Kälteleistung am Verdampfer im HEIZbetrieb,
        /// kein EER (Glossar Abschnitt 6). Wird benannt abgelehnt, nie still als EER gelesen.
        /// </summary>
        Heizlage,

        /// <summary>
        /// VERTAUSCHTE ACHSEN: Jede Stützstelle trägt auf der Temperaturachse die
        /// Kaltwassertemperatur (Temperatur = Vorlauf) statt der Außen- bzw. Quellentemperatur.
        /// Wird benannt abgelehnt, nicht still umgedeutet.
        /// </summary>
        AchsenVertauscht
    }

    /// <summary>Wo eine Stundentemperatur auf der Kühlkennlinie liegt — für Zählung und Hinweis.</summary>
    public enum KennlinienLage
    {
        /// <summary>zwischen den Stützstellen oder genau auf einer.</summary>
        Innen,

        /// <summary>kälter als die unterste Stützstelle — deren Werte (die günstige Seite, Kappung).</summary>
        KappungUnten,

        /// <summary>wärmer als die oberste Stützstelle — linear verlängert (die ungünstige Seite).</summary>
        ExtrapolationOben,

        /// <summary>wärmer als die oberste Stützstelle, Verlängerung verboten — deren Werte.</summary>
        KappungOben,

        /// <summary>die Kennlinie hat nur EINE Stützstelle — ihre Werte gelten konstant.</summary>
        EinzelneStuetzstelle
    }

    /// <summary>Kälteleistung und EER einer Stundentemperatur samt ihrer Lage auf der Kennlinie.</summary>
    public readonly struct KennlinienPunkt
    {
        public KennlinienPunkt(double pkuehl, double eer, KennlinienLage lage)
        {
            Pkuehl = pkuehl;
            Eer = eer;
            Lage = lage;
        }

        /// <summary>Kälteleistung [kW] bei Volllast; nie negativ.</summary>
        public double Pkuehl { get; }

        /// <summary>Kälteverhältnis [—]; eine Kälteleistung &gt; 0 hat immer einen EER &gt; 0.</summary>
        public double Eer { get; }

        /// <summary>Lage der Temperatur auf der Kennlinie.</summary>
        public KennlinienLage Lage { get; }
    }

    /// <summary>
    /// <b>Die Kühlkennlinie EINES Projektgeräts für EINEN Kühl-Vorlauf</b> — der
    /// projektseitige Kennlinienleser der Stufe KU2 (Kühlkonzept 5.1, Festlegungen 1 bis 4;
    /// Entscheide E27, E33 zu K21, K22).
    ///
    /// <para><b>Ohne Datenbank.</b> Die Zeilen liefert
    /// <see cref="KenndatenKuehlungCtrl.ZeilenProjekt"/> (<c>Tab_Kenndaten_Kuehlung</c>, nicht
    /// die Stammtabelle); hier stehen Auswahl, Prüfung und Auswertung — prüfbar ohne Lauf
    /// (Stützstellenprobe, Kühlkonzept 10.2).</para>
    ///
    /// <list type="number">
    /// <item><b>Vorlauf (K21):</b> gewählt wird aus den STÜTZSTELLEN, über den Vorlauf wird nie
    /// interpoliert. <c>Kuehl_Vorlauf</c> NULL heißt kleinster Stützwert — die kälteste
    /// angebotene Kaltwassertemperatur, die nie eine Leistung verspricht, die die Maschine
    /// nicht hat. Ein Wert, der keine Stützstelle ist, rechnet mit der NÄCHSTEN (bei
    /// gleichem Abstand der kälteren) und wird einmal je Gerät und Vorlauf benannt
    /// (<see cref="VorlaufAusgewichen"/>).</item>
    /// <item><b>Laststufe (Festlegung 1, K8b):</b> die höchste Laststufe des gewählten Vorlaufs,
    /// <c>MAX(Last)</c> — im VDI-3805-Import die Stufe „MAX" = 100. Zeilen ohne Laststufe gelten
    /// nur, wenn der Vorlauf gar keine trägt (wie <see cref="KenndatenKuehlungCtrl.Reihen"/>).
    /// Teillast skaliert linear mit konstantem EER (<see cref="Kaeltekaskade"/>).</item>
    /// <item><b>Dubletten, deterministisch und benannt:</b> Steht eine Stützstelle
    /// (Vorlauf, Temperatur, Laststufe) mehrfach, gilt die zuerst gespeicherte Zeile (kleinste
    /// <c>ID</c>). Gezählt wird getrennt, ob die weiteren Zeilen gleich sind
    /// (<see cref="Dubletten"/> — in der Testdatenbank steht jede Stützstelle doppelt) oder
    /// abweichen (<see cref="DublettenAbweichend"/>).</item>
    /// <item><b>Achsenlage (K22):</b> Ein Block in Heizlage oder mit vertauschten Achsen
    /// (<see cref="BlockBefund"/>) wird nicht als Kühlkennlinie gelesen — <see cref="Befund"/>
    /// sagt es, der Aufrufer lehnt benannt ab.</item>
    /// <item><b>Auswertung (<see cref="Auswerten"/>):</b> linear zwischen den Stützstellen,
    /// eine Stützstelle wird exakt getroffen. Außerhalb gilt die Regel der Heizseite, auf die
    /// Kälte gespiegelt: Zur GÜNSTIGEN Seite (kälter als die unterste Stützstelle) wird auf die
    /// Stützstelle gekappt — wie die Heizseite nach oben kappt —, zur UNGÜNSTIGEN Seite (wärmer
    /// als die oberste) linear verlängert, wenn die Projekteinstellung
    /// <c>Extrapolation_erlaubt</c> es zulässt, sonst ebenfalls gekappt. Eine verlängerte
    /// Kälteleistung oder ein EER, der dabei auf 0 fällt, heißt: keine Kälte in dieser Stunde.</item>
    /// </list>
    /// </summary>
    public sealed class Kuehlkennlinie
    {
        /// <summary>
        /// Kaltwasser-Vorlauf, ab dem ein Kühlblock in HEIZLAGE liegt [°C]. Die Kühlblöcke der
        /// Herstellerdateien liegen bei 5 bis 25 °C (Kaltwasser bis Flächenkühlung) oder bei 35
        /// bis 90 °C (Verdampferleistung im Heizbetrieb, K22); 30 °C trennt beide Lagen ohne
        /// einen gemessenen Block auf der Grenze.
        /// </summary>
        public const int KALTWASSER_VORLAUF_GRENZE = 30;

        private readonly double[] _temperatur;
        private readonly double[] _eer;
        private readonly double[] _pkuehl;

        private Kuehlkennlinie(IReadOnlyList<int> stuetzstellen, int? gewuenscht)
        {
            Stuetzstellen = stuetzstellen;
            VorlaufGewuenscht = gewuenscht;
            _temperatur = new double[0];
            _eer = new double[0];
            _pkuehl = new double[0];
        }

        private Kuehlkennlinie(IReadOnlyList<int> stuetzstellen, int? gewuenscht, int vorlauf,
                               int? laststufe, double[] temperatur, double[] eer, double[] pkuehl,
                               int dubletten, int abweichend, KuehlblockBefund befund)
        {
            Stuetzstellen = stuetzstellen;
            VorlaufGewuenscht = gewuenscht;
            Vorlauf = vorlauf;
            Laststufe = laststufe;
            _temperatur = temperatur;
            _eer = eer;
            _pkuehl = pkuehl;
            Dubletten = dubletten;
            DublettenAbweichend = abweichend;
            Befund = befund;
        }

        /// <summary>Alle Vorlauf-Stützstellen der Kennlinie, aufsteigend — die Auswahl aus K21.</summary>
        public IReadOnlyList<int> Stuetzstellen { get; }

        /// <summary><c>Kuehl_Vorlauf</c> der Anlage; <c>null</c> = kleinster Stützwert.</summary>
        public int? VorlaufGewuenscht { get; }

        /// <summary>Der Vorlauf, mit dem gerechnet wird [°C] — immer eine Stützstelle.</summary>
        public int Vorlauf { get; }

        /// <summary>true, wenn <see cref="VorlaufGewuenscht"/> gesetzt, aber keine Stützstelle ist.</summary>
        public bool VorlaufAusgewichen
        {
            get { return !Leer && VorlaufGewuenscht.HasValue && VorlaufGewuenscht.Value != Vorlauf; }
        }

        /// <summary>Die Laststufe, deren Zeilen gelten [%]; <c>null</c> = die Zeilen tragen keine.</summary>
        public int? Laststufe { get; }

        /// <summary>true, wenn das Gerät keine einzige Kühlkennlinienzeile trägt.</summary>
        public bool Leer { get { return Stuetzstellen.Count == 0; } }

        /// <summary>Zahl der Temperatur-Stützstellen des gewählten Vorlaufs (nach dem Zusammenfassen der Dubletten).</summary>
        public int Punkte { get { return _temperatur.Length; } }

        /// <summary>Die Temperaturachse des gewählten Vorlaufs [°C], aufsteigend.</summary>
        public IReadOnlyList<double> Temperaturen { get { return _temperatur; } }

        /// <summary>Unterste Temperatur-Stützstelle [°C]; ohne Punkte 0.</summary>
        public double TemperaturMin { get { return _temperatur.Length > 0 ? _temperatur[0] : 0.0; } }

        /// <summary>Oberste Temperatur-Stützstelle [°C]; ohne Punkte 0.</summary>
        public double TemperaturMax { get { return _temperatur.Length > 0 ? _temperatur[_temperatur.Length - 1] : 0.0; } }

        /// <summary>Gleiche Mehrfachzeilen, die zusammengefasst wurden.</summary>
        public int Dubletten { get; }

        /// <summary>Mehrfachzeilen mit ABWEICHENDEN Werten — es gilt jeweils die zuerst gespeicherte.</summary>
        public int DublettenAbweichend { get; }

        /// <summary>Befund des gewählten Blocks; nur <see cref="KuehlblockBefund.Gueltig"/> rechnet.</summary>
        public KuehlblockBefund Befund { get; }

        /// <summary>true, wenn die Kennlinie rechnet: Zeilen vorhanden und Block in Kaltwasserlage.</summary>
        public bool Rechenbar { get { return !Leer && Punkte > 0 && Befund == KuehlblockBefund.Gueltig; } }

        // =====================================================================
        //  Aufbau
        // =====================================================================

        /// <summary>
        /// Baut die Kennlinie aus den Zeilen EINES Geräts für den gewünschten Kühl-Vorlauf.
        /// </summary>
        /// <param name="zeilen">Alle Kühlkennlinienzeilen des Geräts (Reihenfolge beliebig).</param>
        /// <param name="kuehlVorlauf"><c>Kuehl_Vorlauf</c>; <c>null</c> = kleinster Stützwert (K21).</param>
        public static Kuehlkennlinie Bilden(IEnumerable<KuehlkennlinienZeile> zeilen, int? kuehlVorlauf)
        {
            List<KuehlkennlinienZeile> alle = zeilen == null
                ? new List<KuehlkennlinienZeile>()
                : zeilen.OrderBy(z => z.ID).ToList();

            List<int> stuetzstellen = alle.Select(z => z.Vorlauf).Distinct().OrderBy(v => v).ToList();
            if (stuetzstellen.Count == 0) return new Kuehlkennlinie(stuetzstellen, kuehlVorlauf);

            int vorlauf = VorlaufWaehlen(stuetzstellen, kuehlVorlauf);

            int? laststufe;
            List<KuehlkennlinienZeile> block = BlockDerHoechstenLaststufe(alle, vorlauf, out laststufe);

            // Dubletten: je Temperatur gilt die zuerst gespeicherte Zeile (Liste nach ID geordnet).
            var erste = new SortedDictionary<int, KuehlkennlinienZeile>();
            int dubletten = 0, abweichend = 0;
            foreach (KuehlkennlinienZeile z in block)
            {
                KuehlkennlinienZeile vorhanden;
                if (!erste.TryGetValue(z.Temperatur, out vorhanden))
                {
                    erste.Add(z.Temperatur, z);
                    continue;
                }
                if (vorhanden.Eer.Equals(z.Eer) && vorhanden.Pkuehl.Equals(z.Pkuehl)) dubletten++;
                else abweichend++;
            }

            int n = erste.Count;
            var temperatur = new double[n];
            var eer = new double[n];
            var pkuehl = new double[n];
            int i = 0;
            foreach (KeyValuePair<int, KuehlkennlinienZeile> kv in erste)
            {
                temperatur[i] = kv.Key;
                eer[i] = kv.Value.Eer;
                pkuehl[i] = kv.Value.Pkuehl;
                i++;
            }

            KuehlblockBefund befund = BlockBefund(vorlauf, erste.Keys);
            return new Kuehlkennlinie(stuetzstellen, kuehlVorlauf, vorlauf, laststufe,
                                      temperatur, eer, pkuehl, dubletten, abweichend, befund);
        }

        /// <summary>
        /// Die Vorlaufwahl aus K21: ohne Wunsch der kleinste Stützwert, sonst der Wunsch, wenn er
        /// eine Stützstelle ist, sonst die nächste — bei gleichem Abstand die kältere.
        /// </summary>
        public static int VorlaufWaehlen(IReadOnlyList<int> stuetzstellen, int? gewuenscht)
        {
            if (stuetzstellen == null || stuetzstellen.Count == 0)
                throw new ArgumentException("Keine Stützstelle.", nameof(stuetzstellen));

            int kleinster = stuetzstellen.Min();
            if (!gewuenscht.HasValue) return kleinster;

            int bester = kleinster;
            int abstand = int.MaxValue;
            foreach (int v in stuetzstellen.OrderBy(v => v))
            {
                int d = Math.Abs(v - gewuenscht.Value);
                if (d < abstand) { abstand = d; bester = v; }
            }
            return bester;
        }

        /// <summary>
        /// Die Zeilen eines Vorlaufs in seiner höchsten Laststufe (<c>MAX(Last)</c>, Festlegung 1).
        /// Trägt keine Zeile des Vorlaufs eine Laststufe, gelten alle; sonst nur die mit dem
        /// Höchstwert — wie <see cref="KenndatenKuehlungCtrl.Reihen"/> es auf dem Stamm tut.
        /// </summary>
        private static List<KuehlkennlinienZeile> BlockDerHoechstenLaststufe(
            List<KuehlkennlinienZeile> alle, int vorlauf, out int? laststufe)
        {
            List<KuehlkennlinienZeile> jeVorlauf = alle.Where(z => z.Vorlauf == vorlauf).ToList();
            List<int> lasten = jeVorlauf.Where(z => z.Last.HasValue).Select(z => z.Last.Value).ToList();
            if (lasten.Count == 0)
            {
                laststufe = null;
                return jeVorlauf;
            }

            int max = lasten.Max();
            laststufe = max;
            return jeVorlauf.Where(z => z.Last.HasValue && z.Last.Value == max).ToList();
        }

        /// <summary>
        /// <b>Die Achsenprüfung eines Kühlblocks</b> (K22; Kühlkonzept 5.1, Festlegung 4) — dieselbe
        /// Regel für den Import (<see cref="KuehlblockPruefung"/>) und den Lauf.
        /// <list type="bullet">
        /// <item><see cref="KuehlblockBefund.AchsenVertauscht"/>: Jede Stützstelle trägt als
        /// Temperatur den Vorlauf — die Kaltwassertemperatur auf der Temperaturachse.</item>
        /// <item><see cref="KuehlblockBefund.Heizlage"/>: Vorlauf ab
        /// <see cref="KALTWASSER_VORLAUF_GRENZE"/>, oder jede Stützstelle kälter als der Vorlauf —
        /// eine Quelle, die kälter ist als das Wasser, das die Maschine liefert, ist Heizbetrieb.</item>
        /// </list>
        /// Ein Block ohne Temperatur gilt als gültig — er rechnet ohnehin nicht.
        /// </summary>
        public static KuehlblockBefund BlockBefund(int vorlauf, IEnumerable<int> temperaturen)
        {
            List<int> t = temperaturen == null ? new List<int>() : temperaturen.ToList();
            if (t.Count == 0) return KuehlblockBefund.Gueltig;
            if (t.All(x => x == vorlauf)) return KuehlblockBefund.AchsenVertauscht;
            if (vorlauf >= KALTWASSER_VORLAUF_GRENZE || t.All(x => x < vorlauf)) return KuehlblockBefund.Heizlage;
            return KuehlblockBefund.Gueltig;
        }

        /// <summary>
        /// Die Befunde ALLER Vorläufe einer Kennlinie, jeder in seiner höchsten Laststufe — die
        /// Kennzeichnung eines schon gespeicherten Satzes (Stammdialog, Prüfwerkzeuge): Bestandsdaten
        /// werden nie still geändert, sie werden benannt.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<int, KuehlblockBefund>> Befunde(IEnumerable<KuehlkennlinienZeile> zeilen)
        {
            List<KuehlkennlinienZeile> alle = zeilen == null
                ? new List<KuehlkennlinienZeile>()
                : zeilen.OrderBy(z => z.ID).ToList();

            var ergebnis = new List<KeyValuePair<int, KuehlblockBefund>>();
            foreach (int v in alle.Select(z => z.Vorlauf).Distinct().OrderBy(v => v))
            {
                int? last;
                List<KuehlkennlinienZeile> block = BlockDerHoechstenLaststufe(alle, v, out last);
                ergebnis.Add(new KeyValuePair<int, KuehlblockBefund>(
                    v, BlockBefund(v, block.Select(z => z.Temperatur).Distinct())));
            }
            return ergebnis;
        }

        // =====================================================================
        //  Auswertung
        // =====================================================================

        /// <summary>
        /// Kälteleistung und EER bei der Stundentemperatur <paramref name="temperatur"/> [°C].
        /// Nur für eine <see cref="Rechenbar"/>e Kennlinie; sonst Leistung 0.
        /// </summary>
        /// <param name="temperatur">Außen- bzw. Quellentemperatur der Stunde [°C].</param>
        /// <param name="extrapolationErlaubt">Projekteinstellung <c>Extrapolation_erlaubt</c> — sie
        /// gilt für die ungünstige Seite (wärmer als die oberste Stützstelle).</param>
        public KennlinienPunkt Auswerten(double temperatur, bool extrapolationErlaubt)
        {
            int n = _temperatur.Length;
            if (!Rechenbar || n == 0) return new KennlinienPunkt(0.0, 0.0, KennlinienLage.Innen);

            if (n == 1)
            {
                KennlinienLage lage = temperatur == _temperatur[0]
                    ? KennlinienLage.Innen : KennlinienLage.EinzelneStuetzstelle;
                return Punkt(_pkuehl[0], _eer[0], lage);
            }

            // Die günstige Seite: kälter als die unterste Stützstelle - Kappung.
            if (temperatur < _temperatur[0])
                return Punkt(_pkuehl[0], _eer[0], KennlinienLage.KappungUnten);

            // Die ungünstige Seite: wärmer als die oberste Stützstelle.
            if (temperatur > _temperatur[n - 1])
            {
                if (!extrapolationErlaubt)
                    return Punkt(_pkuehl[n - 1], _eer[n - 1], KennlinienLage.KappungOben);

                double eer = Interp(_temperatur[n - 2], _temperatur[n - 1], _eer[n - 2], _eer[n - 1], temperatur);
                double pk = Interp(_temperatur[n - 2], _temperatur[n - 1], _pkuehl[n - 2], _pkuehl[n - 1], temperatur);
                return Punkt(pk, eer, KennlinienLage.ExtrapolationOben);
            }

            // Innen: eine Stützstelle exakt, dazwischen linear.
            for (int i = 0; i < n; i++)
            {
                if (temperatur == _temperatur[i])
                    return Punkt(_pkuehl[i], _eer[i], KennlinienLage.Innen);
                if (i > 0 && temperatur < _temperatur[i])
                {
                    double eer = Interp(_temperatur[i - 1], _temperatur[i], _eer[i - 1], _eer[i], temperatur);
                    double pk = Interp(_temperatur[i - 1], _temperatur[i], _pkuehl[i - 1], _pkuehl[i], temperatur);
                    return Punkt(pk, eer, KennlinienLage.Innen);
                }
            }

            return Punkt(_pkuehl[n - 1], _eer[n - 1], KennlinienLage.Innen);   // nicht erreichbar
        }

        /// <summary>
        /// Ein Punkt der Kennlinie mit der Leistungsregel: Kälte gibt es nur mit Leistung UND EER
        /// über 0 — eine Verlängerung, die eines von beiden auf 0 bringt, liefert keine Kälte.
        /// </summary>
        private static KennlinienPunkt Punkt(double pkuehl, double eer, KennlinienLage lage)
        {
            if (!(pkuehl > 0.0) || !(eer > 0.0)) return new KennlinienPunkt(0.0, eer > 0.0 ? eer : 0.0, lage);
            return new KennlinienPunkt(pkuehl, eer, lage);
        }

        /// <summary>Lineare Interpolation — dieselbe Schreibweise wie <see cref="SimulationWaermepumpe.Interp"/>.</summary>
        private static double Interp(double x0, double x1, double y0, double y1, double xq)
        {
            return y0 + (xq - x0) * (y1 - y0) / (x1 - x0);
        }
    }
}
