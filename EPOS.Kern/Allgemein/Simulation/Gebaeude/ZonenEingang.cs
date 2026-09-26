using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Luftstrom zwischen zwei Zonen desselben Gebäudes als Paar (Stufe G6b; <c>Tab_Zonenluftstrom</c>) — der Kern-Datensatz des Lesers.</summary>
    /// <param name="IdZoneA">Die erste Zone (<c>Tab_Zone.ID</c>).</param>
    /// <param name="IdZoneB">Die zweite Zone.</param>
    /// <param name="Volumenstrom_M3h">Der Volumenstrom [m³/h]; der Gegenstrom gleicher Größe entsteht beim Rechnen.</param>
    internal readonly record struct Zonenluftstrom(int IdZoneA, int IdZoneB, double Volumenstrom_M3h);

    /// <summary>Ein Nachbarglied der äquivalenten Außentemperatur (Gl. (41)/(42)): die Nachbarzone und das U·A ihrer koppelnden Trennflächen [W/K].</summary>
    internal readonly record struct Nachbarglied(int ZonenId, double UA_WK);

    /// <summary>Der Luftaustausch einer Zone mit einer Nachbarzone: deren Kennung und der Leitwert G = c·ρ·V̇ [W/K].</summary>
    internal readonly record struct Luftkopplung(int ZonenId, double Leitwert_WK);

    /// <summary>
    /// <b>Was der Eingangsbauer für eine Zone eines Mehrzonengebäudes zusätzlich bekommt</b> (Stufe
    /// G6b, Welle W3; <see cref="GebaeudeModellEingang.Bauen(ProjektGebaeudeModel, GebaeudeKlima, Zonenkopplung, bool, string, double, double, double)"/>):
    /// die Zone, die Zahl der Zonen des Gebäudes, die Bauteile der Zone samt den gespiegelten
    /// Trennflächen ihrer Nachbarn und mit aufgelöster Gruppe, und ihr Luftaustausch. Gebildet von
    /// <see cref="ZonenEingang.Bauen"/>.
    /// </summary>
    internal sealed class Zonenkopplung
    {
        internal Zonenkopplung(GebaeudeZonensatz zone, int zonenzahl, IReadOnlyList<BauteilEingang> bauteile,
                               IReadOnlyList<Luftkopplung> luftkopplungen)
        {
            Zone = zone ?? throw new ArgumentNullException(nameof(zone));
            if (zonenzahl < 1) throw new ArgumentOutOfRangeException(nameof(zonenzahl));
            Zonenzahl = zonenzahl;
            Bauteile = bauteile ?? zone.Bauteile;
            Luftkopplungen = luftkopplungen ?? Array.Empty<Luftkopplung>();
        }

        /// <summary>Die Zone.</summary>
        internal GebaeudeZonensatz Zone { get; }

        /// <summary>Die Zahl der Zonen des Gebäudes.</summary>
        internal int Zonenzahl { get; }

        /// <summary>Die Bauteile: die eigenen, dahinter die gespiegelten Trennflächen der Nachbarn; jede Trennfläche mit aufgelöster Gruppe.</summary>
        internal IReadOnlyList<BauteilEingang> Bauteile { get; }

        /// <summary>Der Luftaustausch mit den Nachbarzonen.</summary>
        internal IReadOnlyList<Luftkopplung> Luftkopplungen { get; }
    }

    /// <summary>
    /// <b>Der Eingang EINER Zone eines Gebäudes in der Zonenschleife</b> (Stufe G6b, Welle W3;
    /// Mehrzonenkonzept 2.1–2.7, ADR-005) — der Eingang des Gebäudemodells der Zone
    /// (<see cref="GebaeudeModellEingang"/>: Kaskade, Flächenschlüssel, Bauteilweg mit Trennflächen,
    /// Klima des Gebäudes) und die zwei Kopplungen an die Nachbarzonen, je Stunde aus deren
    /// Lufttemperaturen gebildet:
    /// <list type="bullet">
    /// <item><b>θ_eq</b> (Gl. (41)/(42)): (Zähler der Außenglieder + Σ U·A_j·θ_air,j) / Σ U·A — die
    /// Außenglieder vorgerechnet in der Reihenfolge des Bauteilwegs, die Nachbarglieder dahinter
    /// (<see cref="ThetaEq"/>). θ_NR,eq = θ_NR,Lu: ohne strahlende Quellen auf der Nachbarseite
    /// (Mehrzonenkonzept 2.3, 2.6).</item>
    /// <item><b>Luftaustausch ohne Eingriff in den Löser</b> (<see cref="ThetaLue"/>): R_ext = 1/(H_ve +
    /// Σψ·L + Σ G_zj) steht im Parametersatz, und die Zulufttemperatur θ_Lue = ((G_ve + Z)·θ_out +
    /// Σ G_zj·θ_j) / (G_ve + Σ G_zj + Z) geht als <c>ThetaOut</c> in den Rand — im Löser wirkt sie nur
    /// als gExt·ThetaOut. <b>EPOS-Regel in Anlehnung an Gl. (75)</b>: Die Richtlinie zählt den
    /// Nachbarraumstrom dort nicht auf (Mehrzonenkonzept 2.7).</item>
    /// </list>
    /// Ohne Nachbarglied und ohne Luftaustausch gibt die Zone θ_eq und θ_out ihres Eingangs
    /// unverändert weiter — so rechnet eine Einzelzone bitgleich wie <see cref="Vdi6007Rechenweg.Laufen"/>
    /// (Orakel <c>ZonenlaufTests</c>). Ohne Datenbank, ohne Zustand.
    /// </summary>
    internal sealed class ZonenEingang
    {
        private readonly int[] _nachbarIndex;
        private readonly int[] _luftIndex;
        private readonly double _gGesamt;
        private readonly double _gAussen;

        private ZonenEingang(GebaeudeModellEingang eingang, int index, int[] nachbarIndex, int[] luftIndex)
        {
            Eingang = eingang;
            Index = index;
            _nachbarIndex = nachbarIndex;
            _luftIndex = luftIndex;
            // Der Leitwert des masselosen Zweigs, wie der Löser ihn bildet (1/R_ext), und sein
            // Anteil nach außen G_ve = H_ve + Σψ·L (ohne den Luftaustausch).
            double r = eingang.Parameter.R_ext_KW;
            _gGesamt = double.IsPositiveInfinity(r) ? 0.0 : 1.0 / r;
            _gAussen = _gGesamt - eingang.LuftaustauschLeitwert_WK;
        }

        /// <summary>
        /// Eine Zone ohne Nachbarn aus einem fertigen Eingang — die Einzelzone des Orakels (auch der
        /// Klassenweg). θ_eq und θ_out gehen unverändert durch.
        /// </summary>
        internal static ZonenEingang Einzeln(GebaeudeModellEingang eingang)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (eingang.Nachbarglieder.Count > 0 || eingang.Luftkopplungen.Count > 0)
                throw new ArgumentException("Eine Zone mit Nachbarn braucht ihre Nachbarn.", nameof(eingang));
            return new ZonenEingang(eingang, 0, Array.Empty<int>(), Array.Empty<int>());
        }

        /// <summary>Der Eingang des Gebäudemodells der Zone.</summary>
        internal GebaeudeModellEingang Eingang { get; }

        /// <summary>Die Stelle der Zone in der Rechenreihenfolge (Rang, dann Kennung).</summary>
        internal int Index { get; }

        /// <summary>Die Kennung der Zone (<c>Tab_Zone.ID</c>); 0 ohne Zone.</summary>
        internal int ZonenId => Eingang.Zone?.ZonenId ?? 0;

        /// <summary>Die Bezeichnung der Zone.</summary>
        internal string Bezeichnung => Eingang.Zone?.Bezeichnung ?? Eingang.Bezeichnung;

        /// <summary>Wird die Zone beheizt (Festlegung 2)?</summary>
        internal bool IstBeheizt => Eingang.IstBeheizt;

        /// <summary>Die Stellen der Nachbarzonen der Nachbarglieder, in deren Reihenfolge.</summary>
        internal IReadOnlyList<int> NachbarIndex => _nachbarIndex;

        /// <summary>Die Stellen der Zonen, mit denen die Zone Luft austauscht, in der Reihenfolge der Luftkopplungen.</summary>
        internal IReadOnlyList<int> LuftIndex => _luftIndex;

        /// <summary>Koppelt die Zone an eine andere — über eine Trennfläche der Außengruppe oder einen Luftstrom?</summary>
        internal bool Gekoppelt => _nachbarIndex.Length > 0 || _luftIndex.Length > 0;

        /// <summary>
        /// Die äquivalente Außentemperatur der Stunde <paramref name="h"/> [°C] mit den
        /// Lufttemperaturen <paramref name="thetaAir"/> aller Zonen (nach <see cref="Index"/>).
        /// </summary>
        internal double ThetaEq(int h, ReadOnlySpan<double> thetaAir)
        {
            GebaeudeModellEingang e = Eingang;
            if (_nachbarIndex.Length == 0) return e.ThetaEq[h];
            double zaehler = e.ThetaEqZaehler[h];
            IReadOnlyList<Nachbarglied> glieder = e.Nachbarglieder;
            for (int k = 0; k < _nachbarIndex.Length; k++) zaehler += glieder[k].UA_WK * thetaAir[_nachbarIndex[k]];
            return zaehler / e.UaSummeGewichtung_WK;
        }

        /// <summary>
        /// Die Zulufttemperatur der Stunde <paramref name="h"/> [°C] mit dem Luftaustausch
        /// (Klassenkopf); <paramref name="sommerlueftung"/> legt den Zusatzleitwert Z der
        /// Sommerlüftung an die Außenluft. Ohne Luftaustausch θ_out selbst.
        /// </summary>
        internal double ThetaLue(int h, bool sommerlueftung, ReadOnlySpan<double> thetaAir)
        {
            GebaeudeModellEingang e = Eingang;
            if (_luftIndex.Length == 0) return e.ThetaOut[h];
            double z = sommerlueftung ? e.SommerlueftungZusatzleitwertWK : 0.0;
            double zaehler = (_gAussen + z) * e.ThetaOut[h];
            IReadOnlyList<Luftkopplung> luft = e.Luftkopplungen;
            for (int k = 0; k < _luftIndex.Length; k++) zaehler += luft[k].Leitwert_WK * thetaAir[_luftIndex[k]];
            return zaehler / (_gGesamt + z);
        }

        /// <summary>Die Randbedingung der Stunde <paramref name="h"/> mit den Lufttemperaturen der Zonen (Klassenkopf).</summary>
        internal Stundenrand Rand(int h, bool sommerlueftung, ReadOnlySpan<double> thetaAir)
        {
            if (!Gekoppelt) return Eingang.Rand(h, sommerlueftung, Eingang.ThetaEq[h], Eingang.ThetaOut[h]);
            return Eingang.Rand(h, sommerlueftung, ThetaEq(h, thetaAir), ThetaLue(h, sommerlueftung, thetaAir));
        }

        // =====================================================================
        //  Der Bauer der Zonen eines Gebäudes
        // =====================================================================

        /// <summary>
        /// <b>Die Zonen eines Gebäudes</b> (Stufe G6b, Welle W3), in der Rechenreihenfolge nach
        /// <c>Rang</c>, dann Kennung (fest, nicht aus der Eingabereihenfolge; Mehrzonenkonzept 2.4):
        /// <list type="number">
        /// <item>Jede Zone liest ihre Werte über die Vorgabenkaskade (A5 (a)), die Leistungsgrenzen
        /// ab zwei Zonen anteilig (Festlegung 5), Kühlwerte vom Gebäude (A4 (a)).</item>
        /// <item><b>Trennflächen</b>: Je Zonenpaar führt eine Zone die Fläche; die Nachbarzone
        /// rechnet sie gespiegelt (<see cref="BauteilEingang.Gespiegelt"/>) hinter ihren eigenen
        /// Bauteilen. Die Gruppe: ausdrücklich (M3 (b)), sonst nach <paramref name="vierK"/> (die
        /// 4-K-Regel der Zonenschleife); eine unbeheizte Zone auf einer Seite heißt immer Außen; ohne
        /// Entscheid adiabat Innen (Vorlauf).</item>
        /// <item><b>Luftströme</b> als Paare: je Seite der Leitwert c·ρ·V̇ zur anderen Zone.</item>
        /// </list>
        /// Mit genau einer Zone ist das derselbe Eingang wie <see cref="GebaeudeModellEingang.Bauen(ProjektGebaeudeModel, GebaeudeKlima, bool, string, double, double, double)"/>.
        /// </summary>
        /// <param name="vierK">Die Gruppe einer Trennfläche nach der 4-K-Regel für (Zone, Nachbar);
        /// <c>null</c> oder <see cref="Trennflaechenzuordnung.Regel"/> = noch nicht entschieden.</param>
        /// <exception cref="GebaeudeModellException">benannt: keine beheizte Zone, eine Trennfläche oder
        /// ein Luftstrom zu einer Zone, die das Gebäude nicht führt, eine unlesbare Zone, oder jede
        /// Prüfung des Eingangsbauers.</exception>
        internal static IReadOnlyList<ZonenEingang> Bauen(ProjektGebaeudeModel gebaeude, GebaeudeKlima klima,
                                                          bool kuehlbetrieb = false, string anlagenkopplung = null,
                                                          Func<int, int, Trennflaechenzuordnung> vierK = null)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            if (klima == null) throw new ArgumentNullException(nameof(klima));
            if (gebaeude.Zonen == null || gebaeude.Zonen.Count == 0)
                throw new ArgumentException("Das Gebäude führt keine Zone.", nameof(gebaeude));
            string wer = Wer(gebaeude);

            foreach (GebaeudeZonensatz z in gebaeude.Zonen)
            {
                if (z == null) throw new ArgumentException("Die Zonenliste enthält einen leeren Eintrag.", nameof(gebaeude));
                if (z.Lesefehler != null)
                    throw new GebaeudeModellException(z.Lesefehlergrund ?? GebaeudeModellFehler.BauteilUngueltig, wer + ", " + z.Lesefehler);
            }
            List<GebaeudeZonensatz> zonen = gebaeude.Zonen.OrderBy(z => z.Rang).ThenBy(z => z.ZonenId).ToList();
            int n = zonen.Count;
            var index = new Dictionary<int, int>();
            for (int i = 0; i < n; i++)
            {
                if (index.ContainsKey(zonen[i].ZonenId))
                    throw Kopplungsfehler(MyResource.Resource.SIMENG_G6_ZONE_DOPPELT, wer, zonen[i].Bezeichnung,
                                          zonen[i].ZonenId.ToString(CultureInfo.InvariantCulture));
                index[zonen[i].ZonenId] = i;
            }
            if (!zonen.Any(z => z.IstBeheizt))
                throw new GebaeudeModellException(GebaeudeModellFehler.KeineBeheizteZone,
                    wer + ": " + string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G6_KEINE_BEHEIZTE_ZONE,
                                               string.Join(", ", zonen.Select(z => z.Bezeichnung))));

            // ---- Trennflächen: eigene, dahinter die gespiegelten der Nachbarn, jede mit ihrer Gruppe ----
            // Die gespiegelten Flächen stehen hinter den eigenen, nach der Rechenreihenfolge der
            // führenden Zone und dort nach der Reihenfolge ihrer Bauteile (Probe 1: die Trennfläche
            // am Ende der Liste).
            var eigene = new List<BauteilEingang>[n];
            var gespiegelt = new List<BauteilEingang>[n];
            for (int i = 0; i < n; i++)
            {
                eigene[i] = new List<BauteilEingang>();
                gespiegelt[i] = new List<BauteilEingang>();
            }
            for (int i = 0; i < n; i++)
            {
                GebaeudeZonensatz z = zonen[i];
                foreach (BauteilEingang b in z.Bauteile)
                {
                    if (b == null || b.Rand != Bauteilrand.Zone || n < 2)
                    {
                        eigene[i].Add(b);
                        continue;
                    }
                    string werB = wer + ", " + GebaeudeZonenabbildung.Wer(z.Bezeichnung) + ", " + b.Bezeichnung;
                    int nachbar = b.IdNachbarzone ?? throw Kopplungsfehler(MyResource.Resource.SIMENG_G6_NACHBAR_FEHLT, werB);
                    if (!index.TryGetValue(nachbar, out int j))
                        throw Kopplungsfehler(MyResource.Resource.SIMENG_G6_NACHBAR_FREMD, werB, nachbar.ToString(CultureInfo.InvariantCulture));
                    if (j == i)
                        throw Kopplungsfehler(MyResource.Resource.SIMENG_G6_NACHBAR_EIGEN, werB);
                    Trennflaechenzuordnung gruppe = Gruppe(b.Zuordnung, z, zonen[j], vierK);
                    eigene[i].Add(b.MitZuordnung(gruppe));
                    gespiegelt[j].Add(b.Gespiegelt(z.ZonenId).MitZuordnung(gruppe));
                }
            }
            var geordnet = new List<BauteilEingang>[n];
            for (int i = 0; i < n; i++)
            {
                geordnet[i] = eigene[i];
                geordnet[i].AddRange(gespiegelt[i]);
            }

            // ---- Luftströme als Paare ----
            var luft = new List<Luftkopplung>[n];
            for (int i = 0; i < n; i++) luft[i] = new List<Luftkopplung>();
            foreach (Zonenluftstrom s in gebaeude.Zonenluftstroeme ?? Array.Empty<Zonenluftstrom>())
            {
                if (!index.TryGetValue(s.IdZoneA, out int a) || !index.TryGetValue(s.IdZoneB, out int b) || a == b)
                    throw Kopplungsfehler(MyResource.Resource.SIMENG_G6_LUFTSTROM_ZONE, wer,
                        s.IdZoneA.ToString(CultureInfo.InvariantCulture), s.IdZoneB.ToString(CultureInfo.InvariantCulture));
                if (!(s.Volumenstrom_M3h > 0.0) || double.IsInfinity(s.Volumenstrom_M3h))
                    throw Kopplungsfehler(MyResource.Resource.SIMENG_G6_LUFTSTROM_WERT, wer,
                        zonen[a].Bezeichnung, zonen[b].Bezeichnung, s.Volumenstrom_M3h.ToString("G6", CultureInfo.CurrentCulture));
                double g = GebaeudeFestwerte.C_RHO_LUFT * s.Volumenstrom_M3h;
                Hinzufuegen(luft[a], zonen[b].ZonenId, g);
                Hinzufuegen(luft[b], zonen[a].ZonenId, g);
            }

            // ---- Die Eingänge ----
            var eingaenge = new GebaeudeModellEingang[n];
            for (int i = 0; i < n; i++)
                eingaenge[i] = GebaeudeModellEingang.Bauen(gebaeude, klima,
                    new Zonenkopplung(zonen[i], n, geordnet[i].AsReadOnly(), luft[i].AsReadOnly()),
                    kuehlbetrieb, anlagenkopplung);

            var ergebnis = new ZonenEingang[n];
            for (int i = 0; i < n; i++)
            {
                GebaeudeModellEingang e = eingaenge[i];
                int[] nachbarIndex = e.Nachbarglieder.Select(x => index[x.ZonenId]).ToArray();
                int[] luftIndex = e.Luftkopplungen.Select(x => index[x.ZonenId]).ToArray();
                ergebnis[i] = new ZonenEingang(e, i, nachbarIndex, luftIndex);
            }
            return ergebnis;
        }

        /// <summary>Hängt den Leitwert <paramref name="g"/> zur Zone <paramref name="zone"/> an, oder addiert ihn zu einem vorhandenen.</summary>
        private static void Hinzufuegen(List<Luftkopplung> liste, int zone, double g)
        {
            int k = liste.FindIndex(x => x.ZonenId == zone);
            if (k < 0) liste.Add(new Luftkopplung(zone, g));
            else liste[k] = new Luftkopplung(zone, liste[k].Leitwert_WK + g);
        }

        /// <summary>
        /// Die Gruppe einer Trennfläche zwischen <paramref name="zone"/> und <paramref name="nachbar"/>:
        /// ausdrücklich (M3 (b)); sonst eine unbeheizte Zone auf einer Seite → Außen (Mehrzonenkonzept 2.2
        /// Punkt 1); sonst die 4-K-Regel, solange sie nicht entschieden hat Regel (adiabat Innen).
        /// </summary>
        private static Trennflaechenzuordnung Gruppe(Trennflaechenzuordnung ausdruecklich, GebaeudeZonensatz zone,
                                                     GebaeudeZonensatz nachbar, Func<int, int, Trennflaechenzuordnung> vierK)
        {
            if (ausdruecklich != Trennflaechenzuordnung.Regel) return ausdruecklich;
            if (!zone.IstBeheizt || !nachbar.IstBeheizt) return Trennflaechenzuordnung.Aussen;
            return vierK?.Invoke(zone.ZonenId, nachbar.ZonenId) ?? Trennflaechenzuordnung.Regel;
        }

        private static GebaeudeModellException Kopplungsfehler(string muster, params object[] werte)
            => new GebaeudeModellException(GebaeudeModellFehler.ZonenkopplungUngueltig,
                                           string.Format(CultureInfo.CurrentCulture, muster, werte));

        private static string Wer(ProjektGebaeudeModel g)
            => string.IsNullOrEmpty(g.Gebaeudename)
                ? "Gebäude " + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture)
                : g.Gebaeudename + " (" + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
