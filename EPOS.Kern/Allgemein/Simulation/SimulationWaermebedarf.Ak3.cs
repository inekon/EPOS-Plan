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

        internal void Erfassen(int index, ProjektGebaeudeModel zeile, GebaeudeStepper stepper)
        {
            _gebaeude.RemoveAll(e => e.Index == index);
            _gebaeude.Add(new Eintrag { Index = index, Zeile = zeile, Stepper = stepper });
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
