using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Regeln der Kopplung zwischen den Zonen eines Gebäudes</b> (Gebäudesimulation Stufe
    /// G6b; Mehrzonenkonzept 4.2, 5.3; Auftrag G6b, Welle W1) — die EINE Regelklasse für die Prüfung
    /// zwischen Zonen, gerufen von <see cref="GebaeudeZonenCtrl.Pruefen(IList{ZoneModel})"/> (Dialog
    /// und Schreibweg) und vom Lauf. Ohne Datenbank; eine Zone heißt über ihre Id — eine positive
    /// (gespeichert) oder eine vorläufige, negative (Arbeitsstand).
    ///
    /// <para><b>Fehler</b> (<see cref="Pruefen"/>, die erste gefundene Meldung, <c>null</c> = gültig):
    /// <list type="number">
    /// <item><b>Nachbar im selben Gebäude:</b> <c>ID_Nachbarzone</c> nennt eine Zone der Liste, nicht
    /// die eigene; eine vorläufige Id muss in der Liste eindeutig sein.</item>
    /// <item><b>Randbedingung und Nachbar stehen nur gemeinsam:</b> <c>ZONE</c> braucht eine Nachbarzone,
    /// eine Nachbarzone steht nur an <c>ZONE</c>; die Zuordnung IW/AW (<c>Trennflaeche_Zuordnung</c>,
    /// A1 = M3 (b)) nur an einer Trennfläche.</item>
    /// <item><b>Trennflächenbilanz:</b> Je Zonenpaar führt nur EINE Seite die Trennflächen — die
    /// Gegenseite entsteht beim Rechnen (Mehrzonenkonzept 4.2); zwei Zeilen über dieselbe Fläche
    /// zählten sie doppelt.</item>
    /// <item><b>Luftströme nur als Paare:</b> zwei verschiedene Zonen der Liste, V̇ &gt; 0, jedes Paar
    /// einmal (gleich in welcher Richtung eingegeben).</item>
    /// <item><b>Mindestens eine beheizte Zone</b> (Festlegung 2: <c>IstBeheizt = 0</c> heißt frei
    /// schwingend; ein Gebäude ohne beheizte Zone hätte keinen Wärmebedarf und wird benannt
    /// abgelehnt).</item>
    /// <item><b>ψ·L auf der Zonengrenze gehört der wärmeren Zone</b> (Mehrzonenkonzept 5.3): Trägt eine
    /// Trennfläche eine Wärmebrücke, darf die führende Zone nicht die kältere sein — beheizt vor
    /// unbeheizt, sonst der höhere Tagessollwert (leer = Wert des Gebäudes).</item>
    /// </list></para>
    ///
    /// <para><b>Hinweise</b> (<see cref="Hinweise"/>, halten kein Speichern an): <b>die Hülle jeder
    /// Zone ist geschlossen</b> — ab <see cref="HUELLE_ABWEICHUNG_HINWEIS"/> Abweichung zwischen den
    /// nach oben und nach unten weisenden Flächen bzw. im waagerechten Flächenvektor nennt der Hinweis
    /// die Zone. Gezählt werden die Flächen gegen Außenluft, Erdreich, unbeheizten Raum und
    /// Nachbarzone, dazu die gespiegelte Gegenseite der Trennflächen, die eine andere Zone führt;
    /// Bauteile innerhalb der Zone zählen nicht. Die waagerechte Probe läuft nur, wenn jedes nicht
    /// waagerechte Bauteil der Zone einen Azimut trägt. <b>Wie der Flächenhinweis erst ab zwei
    /// Zonen</b> — eine übernommene Einzelzone trägt die Flächen des Katalogbaus, deren Dach meist
    /// die geneigte Fläche ist.</para>
    /// </summary>
    public static class Zonenkopplungsregeln
    {
        /// <summary>Ab dieser relativen Abweichung (10 %) gilt die Hülle einer Zone als nicht geschlossen (Hinweis).</summary>
        public const double HUELLE_ABWEICHUNG_HINWEIS = 0.10;

        private static string F(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);

        private static string Name(ZoneModel z) => (z?.Bezeichner ?? "").Trim();

        private static string Name(BauteilModel b) => (b?.Bezeichner ?? "").Trim();

        /// <summary>
        /// Die Zone der Liste zu einer Id: Index in <paramref name="liste"/>, −1 = keine, −2 = mehrdeutig
        /// (eine vorläufige Id, die mehrere Zonen tragen).
        /// </summary>
        internal static int Index(IReadOnlyList<ZoneModel> liste, int id)
        {
            int gefunden = -1;
            for (int i = 0; i < liste.Count; i++)
            {
                if (liste[i].ID != id) continue;
                if (gefunden >= 0) return -2;
                gefunden = i;
            }
            return gefunden;
        }

        /// <summary>
        /// <b>Die Prüfung zwischen den Zonen</b> (Klassenkopf). <paramref name="luftstroeme"/> <c>null</c>
        /// heißt „nicht Teil dieser Prüfung" (der Schreibweg ohne Luftströme lässt die gespeicherten
        /// stehen); leer heißt „keine".
        /// </summary>
        /// <param name="zonen">Die Zonen eines Gebäudes samt Bauteilen, in Listenfolge.</param>
        /// <param name="luftstroeme">Die Luftströme zwischen ihnen; <c>null</c> = nicht geprüft.</param>
        /// <param name="sollTagGebaeude">Der Tagessollwert des Gebäudes [°C] für Zonen ohne eigenen; <c>null</c> = keiner.</param>
        /// <returns><c>null</c> = gültig, sonst die erste Meldung.</returns>
        public static string Pruefen(IList<ZoneModel> zonen, IList<ZonenluftstromModel> luftstroeme = null, double? sollTagGebaeude = null)
        {
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            if (liste.Count == 0) return luftstroeme != null && luftstroeme.Any(l => l != null)
                ? F(MyResource.Resource.ZONE_MSG_LUFTSTROM_ZONE, luftstroeme.First(l => l != null).ID_ZoneA)
                : null;

            // 5) Mindestens eine beheizte Zone.
            if (!liste.Any(z => z.IstBeheizt)) return MyResource.Resource.ZONE_MSG_KEINE_BEHEIZT;

            // 1) und 2) Je Bauteil: Randbedingung, Nachbar und Zuordnung.
            var fuehrt = new HashSet<(int Von, int Nach)>();
            for (int i = 0; i < liste.Count; i++)
            {
                ZoneModel z = liste[i];
                foreach (BauteilModel b in z.Bauteile ?? new List<BauteilModel>())
                {
                    if (b == null) continue;
                    bool zone = string.Equals(b.Randbedingung, DbWerte.RANDBEDINGUNG_ZONE, StringComparison.Ordinal);
                    if (zone && !b.ID_Nachbarzone.HasValue)
                        return F(MyResource.Resource.ZONE_MSG_NACHBAR_FEHLT, Name(b), Name(z));
                    if (!zone && b.ID_Nachbarzone.HasValue)
                        return F(MyResource.Resource.ZONE_MSG_NACHBAR_OHNE_RAND, Name(b), Name(z));
                    if (b.Trennflaeche_Zuordnung != null
                        && (!zone || !DbWerte.TRENNFLAECHE_ZUORDNUNGEN.Contains(b.Trennflaeche_Zuordnung)))
                        return F(MyResource.Resource.ZONE_MSG_TRENNFLAECHE_ZUORDNUNG, Name(b), Name(z), b.Trennflaeche_Zuordnung);
                    if (!zone) continue;

                    int n = Index(liste, b.ID_Nachbarzone.Value);
                    if (n == -2)
                        return F(MyResource.Resource.ZONE_MSG_NACHBAR_MEHRDEUTIG, Name(b), Name(z), b.ID_Nachbarzone.Value);
                    if (n < 0)
                        return F(MyResource.Resource.ZONE_MSG_NACHBAR_FREMD, Name(b), Name(z), b.ID_Nachbarzone.Value);
                    if (n == i || ReferenceEquals(liste[n], z))
                        return F(MyResource.Resource.ZONE_MSG_NACHBAR_EIGEN, Name(b), Name(z));
                    fuehrt.Add((i, n));

                    // 6) ψ·L auf der Zonengrenze gehört der wärmeren Zone.
                    if ((b.Psi_L ?? 0.0) > 0.0 && Kaelter(z, liste[n], sollTagGebaeude))
                        return F(MyResource.Resource.ZONE_MSG_PSI_KALTE_SEITE, Name(b), Name(z), Name(liste[n]));
                }
            }

            // 3) Trennflächenbilanz: je Paar führt nur eine Seite.
            foreach ((int von, int nach) in fuehrt)
                if (von < nach && fuehrt.Contains((nach, von)))
                    return F(MyResource.Resource.ZONE_MSG_TRENNFLAECHE_BEIDSEITIG, Name(liste[von]), Name(liste[nach]));

            // 4) Luftströme nur als Paare.
            if (luftstroeme != null)
            {
                var paare = new HashSet<(int, int)>();
                foreach (ZonenluftstromModel l in luftstroeme)
                {
                    if (l == null) continue;
                    int a = Index(liste, l.ID_ZoneA), b = Index(liste, l.ID_ZoneB);
                    if (a == -2) return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_MEHRDEUTIG, l.ID_ZoneA);
                    if (b == -2) return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_MEHRDEUTIG, l.ID_ZoneB);
                    if (a < 0) return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_ZONE, l.ID_ZoneA);
                    if (b < 0) return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_ZONE, l.ID_ZoneB);
                    if (a == b) return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_EIGEN, Name(liste[a]));
                    if (!(l.Volumenstrom > 0.0) || double.IsInfinity(l.Volumenstrom))
                        return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_WERT, Name(liste[a]), Name(liste[b]),
                                 l.Volumenstrom.ToString("0.###", CultureInfo.CurrentCulture));
                    if (!paare.Add((Math.Min(a, b), Math.Max(a, b))))
                        return F(MyResource.Resource.ZONE_MSG_LUFTSTROM_DOPPELT, Name(liste[a]), Name(liste[b]));
                }
            }
            return null;
        }

        /// <summary>
        /// Ist Zone <paramref name="z"/> kälter als <paramref name="nachbar"/>? Beheizt ist wärmer als
        /// unbeheizt; zwischen zwei beheizten Zonen entscheidet der Tagessollwert (leer = der des
        /// Gebäudes, ohne ihn gleich warm).
        /// </summary>
        internal static bool Kaelter(ZoneModel z, ZoneModel nachbar, double? sollTagGebaeude)
        {
            if (z.IstBeheizt != nachbar.IstBeheizt) return !z.IstBeheizt;
            if (!z.IstBeheizt) return false;
            double? a = z.Raumsolltemperatur_Tag ?? sollTagGebaeude;
            double? b = nachbar.Raumsolltemperatur_Tag ?? sollTagGebaeude;
            return a.HasValue && b.HasValue && a.Value < b.Value;
        }

        // =================================================================
        //  Hinweise: die geschlossene Hülle je Zone
        // =================================================================

        /// <summary>
        /// Die Flächenbilanz einer Zone: Σ A nach oben und nach unten (Neigung &lt; 90° bzw. &gt; 90°,
        /// je mit dem Kosinus), der waagerechte Flächenvektor (Ost, Nord) und seine Betragssumme; ob
        /// jedes nicht waagerechte Bauteil einen Azimut trug.
        /// </summary>
        public readonly record struct Huellbilanz(double Oben, double Unten, double Ost, double Nord, double Waagerecht, bool AzimuteVollstaendig)
        {
            /// <summary>Relative Abweichung oben gegen unten; 0 ohne Fläche.</summary>
            public double AbweichungSenkrecht => Math.Max(Oben, Unten) > 0.0 ? Math.Abs(Oben - Unten) / Math.Max(Oben, Unten) : 0.0;

            /// <summary>Relative Abweichung des waagerechten Flächenvektors gegen die halbe Betragssumme; 0 ohne Fläche.</summary>
            public double AbweichungWaagerecht => Waagerecht > 0.0 ? Math.Sqrt(Ost * Ost + Nord * Nord) / (0.5 * Waagerecht) : 0.0;
        }

        /// <summary>
        /// Die Flächenbilanz der Zone <paramref name="index"/> der Liste (Klassenkopf): ihre Bauteile
        /// außerhalb der Zone und die gespiegelten Trennflächen, die eine andere Zone mit ihr führt.
        /// </summary>
        public static Huellbilanz Bilanz(IList<ZoneModel> zonen, int index)
        {
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            ZoneModel zone = liste[index];
            double oben = 0.0, unten = 0.0, ost = 0.0, nord = 0.0, waagerecht = 0.0;
            bool vollstaendig = true;

            void Zaehlen(BauteilModel b, bool gespiegelt)
            {
                double neigung = b.Neigung ?? GebaeudeZonenCtrl.NeigungVorgabe(b.Bauteilart);
                if (gespiegelt) neigung = 180.0 - neigung;
                double rad = neigung * Math.PI / 180.0;
                double senk = Math.Cos(rad), waag = Math.Sin(rad);
                if (Math.Abs(senk) < 1e-12) senk = 0.0;
                if (Math.Abs(waag) < 1e-12) waag = 0.0;
                if (senk > 0.0) oben += b.Flaeche * senk;
                else if (senk < 0.0) unten -= b.Flaeche * senk;
                if (waag <= 0.0) return;
                waagerecht += b.Flaeche * waag;
                if (!b.Azimut.HasValue) { vollstaendig = false; return; }
                double az = (b.Azimut.Value + (gespiegelt ? 180.0 : 0.0)) * Math.PI / 180.0;
                ost += b.Flaeche * waag * Math.Sin(az);
                nord += b.Flaeche * waag * Math.Cos(az);
            }

            foreach (BauteilModel b in zone.Bauteile ?? new List<BauteilModel>())
            {
                if (b == null) continue;
                if (GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung) == Bauteilrand.Innen) continue;
                Zaehlen(b, false);
            }
            for (int j = 0; j < liste.Count; j++)
            {
                if (j == index) continue;
                foreach (BauteilModel b in liste[j].Bauteile ?? new List<BauteilModel>())
                    if (b != null && b.ID_Nachbarzone.HasValue && Index(liste, b.ID_Nachbarzone.Value) == index
                        && string.Equals(b.Randbedingung, DbWerte.RANDBEDINGUNG_ZONE, StringComparison.Ordinal))
                        Zaehlen(b, true);
            }
            return new Huellbilanz(oben, unten, ost, nord, waagerecht, vollstaendig);
        }

        /// <summary>
        /// <b>Die Hinweise zur geschlossenen Hülle</b> (Klassenkopf) — ab zwei Zonen, je Zone höchstens
        /// zwei (senkrecht, waagerecht). Ohne Datenbank; leer = nichts zu sagen.
        /// </summary>
        public static IReadOnlyList<string> Hinweise(IList<ZoneModel> zonen)
        {
            List<ZoneModel> liste = (zonen ?? new List<ZoneModel>()).Where(z => z != null).ToList();
            var hinweise = new List<string>();
            if (liste.Count < 2) return hinweise;
            CultureInfo k = CultureInfo.CurrentCulture;
            for (int i = 0; i < liste.Count; i++)
            {
                Huellbilanz h = Bilanz(liste, i);
                if (h.AbweichungSenkrecht >= HUELLE_ABWEICHUNG_HINWEIS - 1e-12)
                    hinweise.Add(F(MyResource.Resource.ZONE_HINWEIS_HUELLE_SENKRECHT, Name(liste[i]),
                                   h.Oben.ToString("0.#", k), h.Unten.ToString("0.#", k),
                                   (100.0 * h.AbweichungSenkrecht).ToString("0", k)));
                if (h.AzimuteVollstaendig && h.AbweichungWaagerecht >= HUELLE_ABWEICHUNG_HINWEIS - 1e-12)
                    hinweise.Add(F(MyResource.Resource.ZONE_HINWEIS_HUELLE_WAAGERECHT, Name(liste[i]),
                                   (100.0 * h.AbweichungWaagerecht).ToString("0", k)));
            }
            return hinweise;
        }
    }
}
