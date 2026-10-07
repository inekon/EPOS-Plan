using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der AK3-Weg auf der Bedarfsseite</b> (AK3-W3b/W3c; Entwurf AK3 2.1, 2.6, Festlegungen 10, 14, 17): Mit Stufe AK3
    /// im Kern (<see cref="Ak3Kernstufe"/>) ist der Gebäudelauf der Bedarfsrechnung <b>Pass 1</b> — unbegrenzt, ohne
    /// Profilweg —, und je gekoppeltem Gebäude auf dem VDI-Weg (eine oder mehrere Zonen) entsteht daneben ein Stepper
    /// für den Kreis.
    /// Die Kaskadenstunde ruft den Kreis über die Naht (<see cref="Ak3Stundenbedarf"/>); nach der Schleife führt
    /// <see cref="Ak3Nachfuehren"/> Gebäudeergebnisse, Heizkreis, Summen-, Monats- und Dauerlinienreihen aus den
    /// Werten der Naht nach. Ohne Stufe AK3 ist <see cref="Ak3"/> <c>null</c> und nichts hiervon wirkt.
    /// </summary>
    public partial class SimulationWaermebedarf
    {
        private Ak3Weg _ak3;

        /// <summary>Der AK3-Weg des laufenden Laufs; <c>null</c> = der heutige Lauf.</summary>
        internal Ak3Weg Ak3 => _ak3;

        /// <summary>
        /// <b>Ist diese Bedarfsrechnung der Projektlauf</b> (mit Kaskade, <see cref="SimulationRunner"/>)? Nur er rechnet
        /// mit Stufe AK3 den geschlossenen Kreis; Vorgabe <c>false</c> = eine Auskunft (Festlegung 20).
        /// </summary>
        internal bool Projektlauf { get; set; }

        /// <summary>
        /// Hat diese Auskunft mit Stufe AK3 auf den Profilweg zurückgestuft (Festlegung 20)? Der Hinweis
        /// <see cref="Ak3Kernstufe.RUECKSTUFE_TEXT"/> steht dann im Laufprotokoll.
        /// </summary>
        internal bool Ak3Rueckstufe { get; private set; }

        /// <summary>Nennt die Rückstufe einer Auskunft, wenn der Projektlauf den Kreis rechnen würde (Festlegung 20).</summary>
        internal void Ak3RueckstufeNennen()
        {
            if (Projektlauf || !Ak3Kernstufe.Rueckstufe(AnlagenkopplungProjekt)) return;
            Ak3Rueckstufe = true;
            SimulationProtokoll.Aktuell.HinweisEinmal("ak3-rueckstufe", Ak3Kernstufe.RUECKSTUFE_TEXT);
        }

        /// <summary>
        /// Legt den AK3-Weg an, wenn die Kernstufe für die Projektstufe wirkt und mindestens ein Gebäude gekoppelt
        /// auf dem VDI-Weg rechnet — Einzonen- wie Mehrzonengebäude (Entwurf 2.6: Anlage außen, Zonen innen);
        /// Altweg-Gebäude sind feste Last (Festlegung 17).
        /// </summary>
        /// <returns>false = benannter Abbruch (ungültiges Zeitprogramm, wie im Fahrplan).</returns>
        private bool Ak3Vorbereiten(int idProjekt, int idKlimaregion, ProjektGebaeudeCtrl ctrl)
        {
            _ak3 = null;
            _vdi6007.Ak3Erfassen = null;
            string stufe = AnlagenkopplungProjekt;
            if (FahrplanUnterdrueckt || !Ak3Kernstufe.Wirksam(stufe) || ctrl == null || ctrl.rows == 0) return true;
            // Festlegung 20: Nur der Projektlauf (mit Kaskade) rechnet den Kreis; jede andere Bedarfsrechnung ist eine
            // Auskunft und rechnet auf dem Profilweg — benannt.
            if (!Projektlauf)
            {
                Ak3RueckstufeNennen();
                return true;
            }

            bool gekoppelt = false;
            for (int i = 0; i < ctrl.rows; i++)
            {
                ProjektGebaeudeModel item = ctrl.items[i];
                if (ReferenceEquals(RechenwegWaehlen(item), _vdi6007) && Waermeuebergabe.KopplungWirksamFuer(item, stufe))
                    gekoppelt = true;
            }
            if (!gekoppelt) return true;

            // Die Datenbankseite der Angebotsfunktion: Masken, Zeitprogramm, Vorlaufangebot und Ptherm der
            // Heizerzeuger aus demselben Lader wie der Fahrplan (kein neues SQL).
            List<Anlagenfahrplan.Erzeugerzeile> erzeuger;
            try
            {
                erzeuger = Anlagenfahrplan.ErzeugerAusProjekt(idProjekt, Stundentemperatur,
                    Anlagenzeitprogramm.WochentagAusWochenende(_kalender.Gemeinsam.WochenendeOrtszeit), idKlimaregion);
            }
            catch (AnlagenzeitprogrammFehler f)
            {
                Fehlertext = f.Message;
                SimulationProtokoll.Aktuell.Fehlermeldung(f.Message);
                return false;
            }

            _ak3 = new Ak3Weg { Erzeuger = erzeuger };
            _vdi6007.Ak3Erfassen = _ak3.Erfassen;
            SimulationProtokoll.Aktuell.HinweisEinmal("ak3-kernstufe",
                "Anlagenkopplung AK3 (Kernstufe): Gebäude und Kaskade rechnen je Stunde im geschlossenen Kreis.");
            return true;
        }

        /// <summary>
        /// <b>Vor jedem Durchgang der Kaskade</b> (AK3-W3d; Konzept Simulationsablauf 23.4): Der zweite Feldlauf der
        /// Erdsonde wiederholt den ganzen Durchgang an derselben Instanz, die Bedarfsrechnung läuft nicht neu. Vor dem
        /// ersten Durchgang sichert diese Methode den Stand von Pass 1 (Heizkanal-, Summen-, Monats- und
        /// Dauerlinienreihen, Heizkreis samt Vorlauf und Rücklauf je Stunde, Gebäudeergebnisse und Kennzahlen der
        /// gekoppelten Gebäude); vor jedem weiteren stellt sie ihn Zeichen für Zeichen wieder her und legt den Kreis neu
        /// an — frische Stepper, Abweichung null, kein Kreis. So rechnet jeder Feldlauf ein volles Jahr aus Pass 1, keine
        /// Abweichung wird doppelt nachgeführt, und das Ergebnis ist das des letzten Feldlaufs. Ohne AK3-Weg: nichts.
        /// </summary>
        internal void Ak3FeldlaufBeginnen()
        {
            if (_ak3 == null || _ak3.Gebaeude.Count == 0) return;
            if (_ak3.Pass1Stand == null)
            {
                _ak3.Pass1Stand = Ak3Pass1Sichern();
                return;
            }
            Ak3Pass1Herstellen(_ak3.Pass1Stand);
            _ak3.NeuerFeldlauf();
        }

        private Ak3Weg.Pass1Sicherung Ak3Pass1Sichern()
        {
            var s = new Ak3Weg.Pass1Sicherung
            {
                Waermebedarf = (double[])Waermebedarf.Clone(),
                Heizkanal = (double[])Waermebedarf_Heizkanal_Stunde.Clone(),
                GebaeudeMonat = (double[])Waermebedarf_Gebaeude_Monat.Clone(),
                Sortiert = (double[])Waermebedarf_sortiert.Clone(),
                DauerlinieNichtSortiert = (double[])Dauerlinie_nicht_sortiert.Clone(),
                Dauerlinie = (double[])Dauerlinie.Clone(),
                Max = Waermebedarf_Max,
                Gesamt = Waermebedarf_Gesamt,
                GebaeudeGesamt = Waermebedarf_Gebaeude_Gesamt,
                Heizkreis = Heizkreis,
                VorlaufC = Heizkreis != null ? (double[])Heizkreis.VorlaufC.Clone() : null,
                RuecklaufC = Heizkreis != null ? (double[])Heizkreis.RuecklaufC.Clone() : null,
            };
            foreach (Ak3Weg.Eintrag g in _ak3.Gebaeude)
            {
                s.Ergebnisse[g.Index] = GebaeudeErgebnisse.Ergebnis(g.Index);
                if (g.Index < GebaeudeKennzahlenListe.Count) s.Kennzahlen[g.Index] = GebaeudeKennzahlenListe[g.Index];
            }
            return s;
        }

        private void Ak3Pass1Herstellen(Ak3Weg.Pass1Sicherung s)
        {
            Array.Copy(s.Waermebedarf, Waermebedarf, s.Waermebedarf.Length);
            Array.Copy(s.Heizkanal, Waermebedarf_Heizkanal_Stunde, s.Heizkanal.Length);
            Array.Copy(s.GebaeudeMonat, Waermebedarf_Gebaeude_Monat, s.GebaeudeMonat.Length);
            Array.Copy(s.Sortiert, Waermebedarf_sortiert, s.Sortiert.Length);
            Array.Copy(s.DauerlinieNichtSortiert, Dauerlinie_nicht_sortiert, s.DauerlinieNichtSortiert.Length);
            Array.Copy(s.Dauerlinie, Dauerlinie, s.Dauerlinie.Length);
            Waermebedarf_Max = s.Max;
            Waermebedarf_Gesamt = s.Gesamt;
            Waermebedarf_Gebaeude_Gesamt = s.GebaeudeGesamt;
            Heizkreis = s.Heizkreis;
            if (Heizkreis != null)
            {
                Array.Copy(s.VorlaufC, Heizkreis.VorlaufC, s.VorlaufC.Length);
                Array.Copy(s.RuecklaufC, Heizkreis.RuecklaufC, s.RuecklaufC.Length);
            }
            foreach (KeyValuePair<int, GebaeudeModellErgebnis> e in s.Ergebnisse)
                if (e.Value != null) GebaeudeErgebnisse.Setzen(e.Key, e.Value);
            foreach (KeyValuePair<int, ErgebnisGebaeudeModel> k in s.Kennzahlen)
                if (k.Key < GebaeudeKennzahlenListe.Count) GebaeudeKennzahlenListe[k.Key] = k.Value;
        }

        /// <summary>
        /// <b>Nach der Kaskadenschleife</b> (Entwurf AK3 2.1 Schritt 5): Stepper abschließen und skalieren, Heizkreis neu
        /// bilden, Heizkanal-, Summen-, Monats- und Dauerlinienreihen um die Abweichung der Naht nachführen
        /// (Festlegung 14, gekoppelte Reihen). Netzverluste, Brauchwasser und Prozess bleiben Pass 1 (Festlegung 10).
        /// </summary>
        /// <returns>false, wenn der Kreis nicht das ganze Jahr gerechnet hat (dann bleibt Pass 1 stehen, benannt).</returns>
        internal bool Ak3Nachfuehren()
        {
            if (_ak3 == null || _ak3.Gebaeude.Count == 0) return true;
            if (_ak3.Gebaeude.Any(g => g.Stepper.NaechsteStunde != 8760))
            {
                SimulationProtokoll.Aktuell.HinweisEinmal("ak3-ohne-kaskadenstunde",
                    "Anlagenkopplung AK3: Die Kaskade hat den Kreis nicht je Stunde gerufen; die Gebäude bleiben beim unbegrenzten Lauf.");
                return false;
            }

            foreach (Ak3Weg.Eintrag g in _ak3.Gebaeude)
            {
                GebaeudeModellErgebnis e = Zonenrechnung.Abschluss(g.Stepper, g.Index, g.Zeile.ID_Gebaeude);
                if (g.Skaliert) e = e.Skaliert(g.Faktor);
                GebaeudeErgebnisse.Setzen(g.Index, e);
                if (g.Index < GebaeudeKennzahlenListe.Count)
                {
                    var reihe = new double[8760];
                    for (int h = 0; h < 8760; h++) reihe[h] = g.Pass1W[h] + _ak3.DeltaJeGebaeudeW(g, h);
                    GebaeudeKennzahlenListe[g.Index] = KennzahlenEinesGebaeudes(g.Zeile, g.Index, reihe);
                }
            }
            Heizkreis = HeizkreisProjekt.Bilden(GebaeudeErgebnisse.Alle);

            double[] d = _ak3.DeltaKw;
            if (d.All(x => x == 0.0)) return true;
            double summe = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                if (d[h] == 0.0) continue;
                Waermebedarf_Heizkanal_Stunde[h] += d[h];
                Waermebedarf[h] += d[h];
                summe += d[h];
            }
            WPPlan.Core.BhkwPlan.MonatsSumme(Waermebedarf_Heizkanal_Stunde, Waermebedarf_Gebaeude_Monat, mo_anfang, mo_ende);
            Waermebedarf_Gebaeude_Gesamt += summe / 1000;
            Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;

            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_sortiert);
            WPPlan.Core.BhkwPlan.VectorInit(Dauerlinie_nicht_sortiert);
            WPPlan.Core.BhkwPlan.VectorInit(Dauerlinie);
            WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf, Waermebedarf_sortiert);
            WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf, Dauerlinie_nicht_sortiert);
            Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf);
            WPPlan.Core.BhkwPlan.Normieren(Waermebedarf_sortiert, Waermebedarf_Max);
            WPPlan.Core.BhkwPlan.Normieren(Dauerlinie_nicht_sortiert, Waermebedarf_Max);
            WPPlan.Core.BhkwPlan.Heapsort(Waermebedarf_sortiert, Dauerlinie);
            Array.Reverse(Dauerlinie);
            return true;
        }
    }

    /// <summary>Die Erfassung des AK3-Wegs je Lauf: die Stepper der gekoppelten Gebäude, Pass 1 und die Abweichung je Stunde.</summary>
    internal sealed class Ak3Weg
    {
        /// <summary>Ein erfasstes Gebäude.</summary>
        internal sealed class Eintrag
        {
            internal int Index;
            internal ProjektGebaeudeModel Zeile;
            internal Func<GebaeudeStepper> Fabrik;
            internal GebaeudeStepper Stepper;
            internal double Faktor = 1.0;
            internal bool Skaliert;
            internal double[] Pass1W;
            internal double[] KreisW = new double[8760];
        }

        private readonly List<Eintrag> _gebaeude = new List<Eintrag>();

        /// <summary>Die erfassten Gebäude in Zeilenreihenfolge.</summary>
        internal IReadOnlyList<Eintrag> Gebaeude => _gebaeude;

        /// <summary>Abweichung des Heizkanals je Stunde gegen Pass 1 [kWh] (0 = Zeichen für Zeichen Pass 1).</summary>
        internal double[] DeltaKw { get; } = new double[8760];

        /// <summary>Die Heizerzeuger des Projekts (Kaskadenplätze, Masken, Vorlaufangebot, Ptherm).</summary>
        internal List<Anlagenfahrplan.Erzeugerzeile> Erzeuger { get; set; } = new List<Anlagenfahrplan.Erzeugerzeile>();

        /// <summary>Der Kreis des Laufs (nach der ersten Kaskadenstunde gebaut); für Proben und Kennzahlen.</summary>
        internal Anlagenkopplung Kreis { get; set; }

        /// <summary>Der Stand von Pass 1 vor dem ersten Feldlauf (<see cref="SimulationWaermebedarf.Ak3FeldlaufBeginnen"/>).</summary>
        internal sealed class Pass1Sicherung
        {
            internal double[] Waermebedarf, Heizkanal, GebaeudeMonat, Sortiert, DauerlinieNichtSortiert, Dauerlinie;
            internal double Max, Gesamt, GebaeudeGesamt;
            internal HeizkreisProjekt Heizkreis;
            internal double[] VorlaufC, RuecklaufC;
            internal readonly Dictionary<int, GebaeudeModellErgebnis> Ergebnisse = new Dictionary<int, GebaeudeModellErgebnis>();
            internal readonly Dictionary<int, ErgebnisGebaeudeModel> Kennzahlen = new Dictionary<int, ErgebnisGebaeudeModel>();
        }

        /// <summary>Gesicherter Stand von Pass 1; <c>null</c> bis zum ersten Durchgang der Kaskade.</summary>
        internal Pass1Sicherung Pass1Stand { get; set; }

        /// <summary>Der laufende Feldlauf (1 = erster Durchgang, 2 = zweiter Feldlauf der Erdsonde).</summary>
        internal int Feldlauf { get; private set; } = 1;

        /// <summary>Die Kreise der früheren Feldläufe in ihrer Reihenfolge (Proben und Messung); der letzte ist <see cref="Kreis"/>.</summary>
        internal List<Anlagenkopplung> FruehereKreise { get; } = new List<Anlagenkopplung>();

        /// <summary>
        /// Legt den Kreis für einen weiteren Feldlauf neu an (AK3-W3d): je Gebäude ein frischer Stepper aus seiner
        /// Fabrik, Kreisreihen und Abweichung null, der Kreis des vorigen Feldlaufs wandert nach <see cref="FruehereKreise"/>.
        /// </summary>
        internal void NeuerFeldlauf()
        {
            if (Kreis != null) FruehereKreise.Add(Kreis);
            Kreis = null;
            Array.Clear(DeltaKw, 0, DeltaKw.Length);
            foreach (Eintrag e in _gebaeude)
            {
                e.Stepper = e.Fabrik();
                Array.Clear(e.KreisW, 0, e.KreisW.Length);
            }
            Feldlauf++;
        }

        internal void Erfassen(int index, ProjektGebaeudeModel zeile, Func<GebaeudeStepper> fabrik)
        {
            if (fabrik == null) throw new ArgumentNullException(nameof(fabrik));
            _gebaeude.RemoveAll(e => e.Index == index);
            _gebaeude.Add(new Eintrag { Index = index, Zeile = zeile, Fabrik = fabrik, Stepper = fabrik() });
            _gebaeude.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        internal void FaktorSetzen(int index, double faktor)
        {
            foreach (Eintrag e in _gebaeude)
                if (e.Index == index)
                {
                    e.Faktor = faktor;
                    e.Skaliert = true;
                }
        }

        internal void Pass1Merken(int index, double[] reiheW)
        {
            foreach (Eintrag e in _gebaeude)
                if (e.Index == index) e.Pass1W = (double[])reiheW.Clone();
        }

        /// <summary>Die Abweichung eines Gebäudes in Stunde <paramref name="h"/> [W] (0 in Stunden ohne Abweichung).</summary>
        internal double DeltaJeGebaeudeW(Eintrag e, int h) => DeltaKw[h] == 0.0 ? 0.0 : e.KreisW[h] - e.Pass1W[h];
    }

    /// <summary>
    /// <b>Die Bedarfsnaht im AK3-Weg</b> (AK3-W3b; Entwurf AK3 2.1 Schritt 4, 2.3): liest die Wärmekanäle wie die
    /// Vorgabe (Pass 1), rechnet die gekoppelte Stunde im Kreis und ersetzt im Heizkanal den Pass-1-Anteil der
    /// gekoppelten Gebäude durch die Lösung. Vorrang (Festlegung 7): Brauchwasser, Prozess und der Rest des
    /// Heizkanals (Netzverlustanteil, Lastgänge, ungekoppelte und Altweg-Gebäude als feste Last). Stimmt die Lösung
    /// je Gebäude Bit für Bit mit Pass 1 überein, bleibt der Kanal unberührt — Gate „ohne Grenzen bitgleich zu AK1“.
    /// Heizkreis (Vorlauf, Rücklauf der Stunde) und Stufeneingang der Wärmepumpe werden in derselben Stunde nachgeführt.
    /// </summary>
    internal sealed class Ak3Stundenbedarf : IStundenbedarf
    {
        private readonly Ak3Weg _weg;
        private readonly Func<Anlagenkopplung> _bauen;
        private readonly HeizkreisProjekt _heizkreis;
        private readonly Action<int, double> _stufeneingang;
        private Anlagenkopplung _kreis;

        /// <param name="weg">Die Erfassung des Laufs.</param>
        /// <param name="bauen">Baut den Kreis bei der ersten Stunde (dann sind die Module der Kaskade aufgebaut).</param>
        /// <param name="heizkreis">Der Heizkreis aus Pass 1 (Vorlauf je Stunde für die Wärmepumpe); <c>null</c> = keiner.</param>
        /// <param name="stufeneingang">Führt den Summenvektor am Stufeneingang der Wärmepumpe nach (Stunde, Abweichung kWh).</param>
        internal Ak3Stundenbedarf(Ak3Weg weg, Func<Anlagenkopplung> bauen, HeizkreisProjekt heizkreis, Action<int, double> stufeneingang)
        {
            _weg = weg ?? throw new ArgumentNullException(nameof(weg));
            _bauen = bauen ?? throw new ArgumentNullException(nameof(bauen));
            _heizkreis = heizkreis;
            _stufeneingang = stufeneingang;
        }

        public void BedarfDerStunde(int stunde, Kanalsatz kanaele, double[] rest)
        {
            VektorStundenbedarf.Instanz.BedarfDerStunde(stunde, kanaele, rest);
            if (_kreis == null)
            {
                _kreis = _bauen();
                _weg.Kreis = _kreis;
            }

            IReadOnlyList<Ak3Weg.Eintrag> g = _weg.Gebaeude;
            double pass1W = 0.0;
            foreach (Ak3Weg.Eintrag e in g) pass1W += e.Pass1W[stunde];
            double heiz = rest[Kanal.HEIZUNG];
            var vorrang = new Stundenvorrang(rest[Kanal.BRAUCHWASSER], rest[Kanal.PROZESS], 0.0, heiz - pass1W / 1000.0);
            double v0 = _heizkreis != null ? _heizkreis.VorlaufC[stunde] : double.NaN;

            Kopplungsstunde k = _kreis.Stunde(stunde, v0, vorrang);
            _kreis.Festschreiben(stunde);

            bool gleich = true;
            double kreisW = 0.0;
            for (int i = 0; i < g.Count; i++)
            {
                g[i].KreisW[stunde] = k.HeizlastW[i];
                kreisW += k.HeizlastW[i];
                if (!k.HeizlastW[i].Equals(g[i].Pass1W[stunde])) gleich = false;
            }
            if (gleich) return;

            double delta = (kreisW - pass1W) / 1000.0;
            rest[Kanal.HEIZUNG] = heiz + delta;
            _weg.DeltaKw[stunde] = delta;
            _stufeneingang?.Invoke(stunde, delta);
            if (_heizkreis != null)
            {
                _heizkreis.VorlaufC[stunde] = k.VorlaufC;
                _heizkreis.RuecklaufC[stunde] = k.RuecklaufC;
            }
        }
    }
}
