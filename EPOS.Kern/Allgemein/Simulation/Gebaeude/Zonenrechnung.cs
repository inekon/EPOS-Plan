using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zuordnung eines Zonenpaars mit Trennflächen nach der 4-K-Regel (Mehrzonenkonzept 2.2 Punkt 1,
    /// Anwenderentscheid A1 = M3 (b)): das Δϑ des adiabaten Vorlaufs, die gewählte Gruppe und das im
    /// gekoppelten Lauf erreichte Δϑ [K] (max_h |θ̄_A − θ̄_B|).
    /// </summary>
    internal readonly record struct Zonenpaarzuordnung(int ZoneA, int ZoneB, double DeltaVorlaufK,
                                                      Trennflaechenzuordnung Gruppe, double DeltaLaufK)
    {
        /// <summary>Liegt das im Lauf erreichte Δϑ eines Paars der Innengruppe über 4 K (benannt, nicht umgeschaltet)?</summary>
        internal bool Ueberschritten => Gruppe == Trennflaechenzuordnung.Innen && DeltaLaufK >= GebaeudeFestwerte.VIER_K_GRENZE_K;
    }

    /// <summary>
    /// Das Ergebnis EINER Zone eines Mehrzonengebäudes (Stufe G6b, W5; Mehrzonenkonzept 2.8): die
    /// Reihen und Kennzahlen der Zone (<see cref="Ergebnis"/>, unskaliert — ein Gebäude mit Zonen trägt
    /// seine echte Hülle, Skalierungsfaktor 1, Festlegung 11) und die Befunde der Zonenschleife für
    /// <c>Tab_ErgebnisZone</c>.
    /// </summary>
    /// <param name="ZonenId">Die Zone (<c>Tab_Zone.ID</c>).</param>
    /// <param name="Rang">Die Reihenfolge der Zone im Gebäude.</param>
    /// <param name="Bezeichnung">Der Name der Zone.</param>
    /// <param name="IstBeheizt">Wird die Zone beheizt?</param>
    /// <param name="Nutzflaeche_M2">Die Nutzfläche der Zone [m²].</param>
    /// <param name="Ergebnis">Reihen und Kennzahlen der Zone.</param>
    /// <param name="DeltaThetaMaxK">Größter Abstand der Raumluft zu einer Nachbarzone [K]; NaN ohne Nachbarzone.</param>
    /// <param name="DurchlaeufeMax">Höchstzahl der Durchläufe einer Stunde.</param>
    /// <param name="MusterwechselH">Stunden mit gehaltenem oder nicht haltbarem Muster.</param>
    internal sealed record GebaeudeZonenergebnis(int ZonenId, int Rang, string Bezeichnung, bool IstBeheizt, double Nutzflaeche_M2,
                                         GebaeudeModellErgebnis Ergebnis, double DeltaThetaMaxK, int DurchlaeufeMax, int MusterwechselH);

    /// <summary>Das Ergebnis der Mehrzonen-Rechnung eines Gebäudes (Stufe G6b, Welle W4) samt den Befunden der Zonenschleife.</summary>
    internal sealed class Mehrzonenergebnis
    {
        internal Mehrzonenergebnis(GebaeudeModellErgebnis gebaeude, IReadOnlyList<GebaeudeModellErgebnis> zonen,
                                   IReadOnlyList<ZonenEingang> eingaenge, Zonenschleife schleife,
                                   IReadOnlyList<Zonenpaarzuordnung> paare, double zeitGesamtMs, double zeitVorlaeufeMs)
        {
            Gebaeude = gebaeude;
            Zonen = zonen;
            Eingaenge = eingaenge;
            Schleife = schleife;
            Paare = paare;
            ZeitGesamtMs = zeitGesamtMs;
            ZeitVorlaeufeMs = zeitVorlaeufeMs;
        }

        /// <summary>Das Gebäude: Summe der Zonenlasten, Temperaturen flächengewichtet über die beheizten Zonen (unskaliert).</summary>
        internal GebaeudeModellErgebnis Gebaeude { get; }

        /// <summary>Die Ergebnisse je Zone, in der Rechenreihenfolge (unskaliert).</summary>
        internal IReadOnlyList<GebaeudeModellErgebnis> Zonen { get; }

        /// <summary>Die Eingänge der Zonen.</summary>
        internal IReadOnlyList<ZonenEingang> Eingaenge { get; }

        /// <summary>Die Zonenschleife mit ihren Befunden (Durchläufe, Musterwechsel, Vorlauf).</summary>
        internal Zonenschleife Schleife { get; }

        /// <summary>Die Zonenpaare der 4-K-Regel (nur Trennflächen ohne ausdrückliche Zuordnung zwischen beheizten Zonen).</summary>
        internal IReadOnlyList<Zonenpaarzuordnung> Paare { get; }

        /// <summary>Die Rechenzeit der ganzen Mehrzonen-Rechnung [ms].</summary>
        internal double ZeitGesamtMs { get; }

        /// <summary>Die Rechenzeit der Vorläufe [ms]: adiabater Vorlauf der 4-K-Regel und gekoppelter Vorlauf.</summary>
        internal double ZeitVorlaeufeMs { get; }
    }

    /// <summary>
    /// <b>Die Mehrzonen-Rechnung eines Gebäudes</b> (Stufe G6b, Welle W4; Mehrzonenkonzept 2.2–2.9) —
    /// der Ablauf um die Zonenschleife:
    /// <list type="number">
    /// <item><b>Adiabater Vorlauf der 4-K-Regel</b>, nur wenn es ein beheiztes Zonenpaar mit
    /// Trennflächen ohne ausdrückliche Zuordnung gibt: Jede beheizte Zone rechnet ein Jahr für sich,
    /// jede Trennfläche adiabat in der Innengruppe, ohne Luftaustausch; Δϑ = max_h |θ̄_A − θ̄_B|; unter
    /// 4 K Innengruppe, sonst Außengruppe. Unbeheizte Zonen koppeln immer über die Außengruppe. Die
    /// Zuordnung steht danach fest; eine Überschreitung von 4 K im gekoppelten Lauf wird benannt.</item>
    /// <item>Die Zonen (<see cref="ZonenEingang.Bauen"/>) und die Zonenschleife: Vorlauf nach A3, das Jahr.</item>
    /// <item>Die Summe in das Gebäude: Heizlast Σ max(Φ_h,z, 0), Kühlbedarf Σ getrennt (E31);
    /// Raumluft, operative Temperatur, Sollwert und θ_max flächengewichtet über die beheizten Zonen
    /// (Festlegung 10); Umschaltung, gleichzeitiges Heizen und Kühlen und Sommerlüftung als Stunden, in
    /// denen mindestens eine Zone den Fall erfüllt.</item>
    /// </list>
    /// Ohne Datenbank, ohne Protokoll; der Aufrufer meldet (<see cref="Vdi6007Rechenweg"/>).
    /// </summary>
    internal static class Zonenrechnung
    {
        /// <summary>Rechnet das Gebäude <paramref name="gebaeude"/> mit seinen Zonen (Klassenkopf).</summary>
        /// <exception cref="GebaeudeModellException">bei jedem benannten Fehler der Zonen, der Kopplung oder der Schleife.</exception>
        internal static Mehrzonenergebnis Rechnen(ProjektGebaeudeModel gebaeude, GebaeudeKlima klima, bool kuehlbetrieb,
                                                  string anlagenkopplung, int index, int idGebaeude)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            if (klima == null) throw new ArgumentNullException(nameof(klima));
            string wer = Wer(gebaeude);
            var uhr = Stopwatch.StartNew();

            // 1. Die 4-K-Regel über den adiabaten Vorlauf.
            List<(int A, int B)> regelpaare = Regelpaare(gebaeude);
            var zuordnung = new Dictionary<(int, int), Trennflaechenzuordnung>();
            var deltaVorlauf = new Dictionary<(int, int), double>();
            if (regelpaare.Count > 0)
            {
                IReadOnlyList<ZonenEingang> adiabat = ZonenEingang.Bauen(gebaeude, klima, kuehlbetrieb, anlagenkopplung, adiabat: true);
                var luft = new Dictionary<int, double[]>();
                foreach (ZonenEingang z in adiabat)
                    if (z.IstBeheizt && regelpaare.Any(p => p.A == z.ZonenId || p.B == z.ZonenId))
                        luft[z.ZonenId] = Zonenlauf.Laufen(z, index, idGebaeude).Raumtemperatur;
                foreach ((int a, int b) in regelpaare)
                {
                    double d = GroessteDifferenz(luft[a], luft[b]);
                    deltaVorlauf[(a, b)] = d;
                    zuordnung[(a, b)] = d < GebaeudeFestwerte.VIER_K_GRENZE_K ? Trennflaechenzuordnung.Innen : Trennflaechenzuordnung.Aussen;
                }
            }
            double zeitAdiabat = uhr.Elapsed.TotalMilliseconds;

            // 2. Zonen und Schleife.
            Trennflaechenzuordnung VierK(int a, int b)
                => zuordnung.TryGetValue(Paar(a, b), out Trennflaechenzuordnung g) ? g : Trennflaechenzuordnung.Regel;
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(gebaeude, klima, kuehlbetrieb, anlagenkopplung, VierK);
            var schleife = new Zonenschleife(zonen, wer);
            double vorBeginn = uhr.Elapsed.TotalMilliseconds;
            schleife.Vorlauf();
            double zeitVorlauf = uhr.Elapsed.TotalMilliseconds - vorBeginn;
            schleife.Jahr();
            GebaeudeModellErgebnis[] ergebnisse = schleife.Zonenergebnisse(index, idGebaeude);

            // 3. Die 4-K-Regel nach dem Lauf: das erreichte Δϑ (benannt, nicht umgeschaltet).
            var paare = new List<Zonenpaarzuordnung>();
            foreach ((int a, int b) in regelpaare)
            {
                double d = GroessteDifferenz(ergebnisse[Stelle(zonen, a)].Raumtemperatur, ergebnisse[Stelle(zonen, b)].Raumtemperatur);
                paare.Add(new Zonenpaarzuordnung(a, b, deltaVorlauf[(a, b)], zuordnung[(a, b)], d));
            }

            GebaeudeModellErgebnis summe = Gebaeudeergebnis(zonen, ergebnisse, schleife, index, idGebaeude);
            summe.ZonenAnhaengen(Zonenergebnisse(zonen, ergebnisse, schleife));
            uhr.Stop();
            return new Mehrzonenergebnis(summe, ergebnisse, zonen, schleife, paare,
                                         uhr.Elapsed.TotalMilliseconds, zeitAdiabat + zeitVorlauf);
        }

        /// <summary>
        /// Die Summe der Zonen als Ergebnis des Gebäudes (Klassenkopf, Festlegung 10): Heizlast und
        /// Kühlbedarf summiert, Temperaturen, Sollwert und θ_max flächengewichtet über die beheizten Zonen.
        /// </summary>
        internal static GebaeudeModellErgebnis Gebaeudeergebnis(IReadOnlyList<ZonenEingang> zonen, IReadOnlyList<GebaeudeModellErgebnis> ergebnisse,
                                                              Zonenschleife schleife, int index, int idGebaeude)
        {
            var heiz = new double[8760];
            var luft = new double[8760];
            var op = new double[8760];
            var soll = new double[8760];
            bool kuehlWirksam = false;
            double? kuehlSoll = null;
            double flaeche = 0.0, thetaMax = 0.0;
            for (int z = 0; z < zonen.Count; z++)
            {
                GebaeudeModellErgebnis r = ergebnisse[z];
                for (int h = 0; h < 8760; h++) heiz[h] += Math.Max(r.HeizlastW[h], 0.0);
                if (r.KuehlbedarfKwh != null)
                {
                    kuehlWirksam = true;
                    kuehlSoll ??= r.KuehlSollwert;
                }
                if (!zonen[z].IstBeheizt) continue;
                double a = zonen[z].Eingang.Nutzflaeche_M2;
                flaeche += a;
                thetaMax += a * zonen[z].Eingang.ThetaMaxWert;
                for (int h = 0; h < 8760; h++)
                {
                    luft[h] += a * r.Raumtemperatur[h];
                    op[h] += a * r.OperativeTemperatur[h];
                    soll[h] += a * r.Heizsollwert[h];
                }
            }
            for (int h = 0; h < 8760; h++)
            {
                luft[h] /= flaeche;
                op[h] /= flaeche;
                soll[h] /= flaeche;
            }
            thetaMax /= flaeche;

            double[] kuehl = null;
            if (kuehlWirksam)
            {
                kuehl = new double[8760];
                foreach (GebaeudeModellErgebnis r in ergebnisse)
                    if (r.KuehlbedarfKwh != null)
                        for (int h = 0; h < 8760; h++) kuehl[h] += r.KuehlbedarfKwh[h];
            }

            double summeW = 0.0;
            for (int h = 0; h < 8760; h++) summeW += heiz[h];
            int umschaltung = 0, beides = 0, sommer = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (schleife.StundenMitUmschaltung[h]) umschaltung++;
                if (schleife.StundenMitHeizen[h] && schleife.StundenMitKuehlen[h]) beides++;
                if (schleife.StundenMitSommerlueftung[h]) sommer++;
            }
            GebaeudeModellEingang erste = zonen.First(z => z.IstBeheizt).Eingang;
            return new GebaeudeModellErgebnis(index, idGebaeude, DbWerte.GEBAEUDE_MODELL_VDI6007,
                                              heiz, luft, op, kuehl, thetaMax, summeW / 1000.0, 1.0, umschaltung, beides,
                                              soll, sommer, kuehlWirksam ? kuehlSoll : null, null, null, erste.Nachtzeit);
        }

        /// <summary>
        /// Die Ergebnisse je Zone (W5): die Reihen der Zone und die Befunde der Schleife; Δϑ_max über
        /// alle Nachbarzonen der Trennflächen (Innen- und Außengruppe), NaN ohne Nachbarzone.
        /// </summary>
        internal static List<GebaeudeZonenergebnis> Zonenergebnisse(IReadOnlyList<ZonenEingang> zonen, IReadOnlyList<GebaeudeModellErgebnis> ergebnisse,
                                                            Zonenschleife schleife)
        {
            var liste = new List<GebaeudeZonenergebnis>();
            for (int z = 0; z < zonen.Count; z++)
            {
                GebaeudeModellEingang e = zonen[z].Eingang;
                double delta = double.NaN;
                foreach (BauteilEingang b in e.Bauteile)
                {
                    if (b == null || b.Rand != Bauteilrand.Zone || !b.IdNachbarzone.HasValue) continue;
                    int j = Stelle(zonen, b.IdNachbarzone.Value);
                    double d = GroessteDifferenz(ergebnisse[z].Raumtemperatur, ergebnisse[j].Raumtemperatur);
                    delta = double.IsNaN(delta) ? d : Math.Max(delta, d);
                }
                liste.Add(new GebaeudeZonenergebnis(zonen[z].ZonenId, e.Zone?.Rang ?? z + 1, zonen[z].Bezeichnung, zonen[z].IstBeheizt,
                                            e.Nutzflaeche_M2, ergebnisse[z], delta, schleife.DurchlaeufeMaxJeZone[z],
                                            schleife.MusterwechselJeZone[z]));
            }
            return liste;
        }

        /// <summary>
        /// Die Zonenpaare der 4-K-Regel: je Paar beheizter Zonen mit mindestens einer Trennfläche ohne
        /// ausdrückliche Zuordnung (A1 = M3 (b)) — (kleinere Kennung, größere Kennung), sortiert.
        /// </summary>
        internal static List<(int A, int B)> Regelpaare(ProjektGebaeudeModel g)
        {
            var paare = new SortedSet<(int, int)>();
            if (g.Zonen == null || g.Zonen.Count < 2) return new List<(int, int)>();
            var beheizt = new Dictionary<int, bool>();
            foreach (GebaeudeZonensatz z in g.Zonen)
                if (z != null) beheizt[z.ZonenId] = z.IstBeheizt;
            foreach (GebaeudeZonensatz z in g.Zonen)
            {
                if (z == null || !z.IstBeheizt) continue;
                foreach (BauteilEingang b in z.Bauteile)
                    if (b != null && b.Rand == Bauteilrand.Zone && b.Zuordnung == Trennflaechenzuordnung.Regel
                        && b.IdNachbarzone is int n && n != z.ZonenId && beheizt.TryGetValue(n, out bool nb) && nb)
                        paare.Add(Paar(z.ZonenId, n));
            }
            return paare.ToList();
        }

        private static (int, int) Paar(int a, int b) => a < b ? (a, b) : (b, a);

        private static int Stelle(IReadOnlyList<ZonenEingang> zonen, int id)
        {
            for (int i = 0; i < zonen.Count; i++) if (zonen[i].ZonenId == id) return i;
            throw new ArgumentException("Zone " + id.ToString(CultureInfo.InvariantCulture) + " fehlt.", nameof(id));
        }

        private static double GroessteDifferenz(double[] a, double[] b)
        {
            double d = 0.0;
            for (int h = 0; h < a.Length; h++) d = Math.Max(d, Math.Abs(a[h] - b[h]));
            return d;
        }

        private static string Wer(ProjektGebaeudeModel g)
            => string.IsNullOrEmpty(g.Gebaeudename)
                ? "Gebäude " + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture)
                : g.Gebaeudename + " (" + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
