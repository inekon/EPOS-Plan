using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Tageszeitverteilung eines Tages</b> (4.4: <c>Dichte(t) ∝ φ_Tagtyp(d)(h(t))</c>): Die
    /// Stunde eines Ereignisses folgt den 24 Anteilen des Tagesgangs, die Minute darin ist
    /// gleichverteilt. Ein leerer Tagesgang (Σ φ = 0) zieht nicht.
    /// </summary>
    internal sealed class Tageszeitdichte
    {
        private readonly double[] _kumuliert = new double[Zapfkalender.STUNDEN_TAG];
        private readonly int _letzte;

        private Tageszeitdichte(Zeitstruktur s, int tagtyp)
        {
            double summe = 0.0;
            _letzte = -1;
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                double a = s.TagtypLeer[tagtyp] ? 0.0 : s.Tagesgaenge[tagtyp, h];
                summe += a;
                _kumuliert[h] = summe;
                if (a > 0) _letzte = h;
            }
        }

        /// <summary>Die Dichte des Tagtyps <paramref name="typ"/> aus der normierten Zeitstruktur der Zone.</summary>
        internal static Tageszeitdichte Aus(Zeitstruktur s, ZapfTagtyp typ)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            int t = (int)typ - 1;
            if (t < 0 || t >= Tagesgangsatz.TAGTYPEN) throw new ArgumentOutOfRangeException(nameof(typ));
            return new Tageszeitdichte(s, t);
        }

        /// <summary>Trägt der Tagesgang keine Zapfung?</summary>
        internal bool Leer => _letzte < 0;

        /// <summary>
        /// Die Minute des Tages (0 … 1439) eines Ereignisses: Stunde h ist die erste mit
        /// <c>u · Σφ &lt; Σ_{i ≤ h} φ_i</c> (u gleichverteilt), die Minute darin gleichverteilt in 0 … 59.
        /// </summary>
        internal int Minute(ZapfZufall z)
        {
            if (Leer) throw new InvalidOperationException("Ein leerer Tagesgang zieht keine Minute.");
            double ziel = z.Gleich() * _kumuliert[Zapfkalender.STUNDEN_TAG - 1];
            int stunde = _letzte;
            for (int h = 0; h < _letzte; h++)
                if (ziel < _kumuliert[h])
                {
                    stunde = h;
                    break;
                }
            return stunde * Bedarfstag.MINUTEN_JE_STUNDE + z.Ganzzahl(Bedarfstag.MINUTEN_JE_STUNDE);
        }
    }

    /// <summary>
    /// <b>Der Zapfereignisgenerator</b> (Umsetzungskonzept Zapfprofilgenerator 4.4, Schicht S3):
    /// die Ereignisse EINER Einheit an EINEM Tag.
    ///
    /// <code>
    /// je Kategorie k (Reihenfolge des Katalogs):
    ///   λ_k   = Anteil_k · Q_Tag,Einheit / (E_k,1K · Δθ)          E_k,1K = V̄_k · Dauer_k · c_w / 1000
    ///   n_k   ~ Poisson(λ_k)                                      (Ereignisse des Tages)
    ///   je Ereignis:  Minute ~ Dichte des Tagtyps;  z = Σ₁₂ u − 6
    ///                 V̇ = min(max(0, μ_k + σ_k · z), Kappung_k)
    ///                 E = V̇ · Dauer_k · c_w · Δθ / 1000             [kWh], gleichmäßig über Dauer_k Minuten
    /// E[Σ E] = Σ_k λ_k · V̄_k · Dauer_k · c_w · Δθ / 1000 = Q_Tag,Einheit
    /// </code>
    ///
    /// <para><b>Poisson statt Bernoulli je Minute (Abweichung vom Papier).</b> Das Papier zieht je
    /// Minute ein Bernoulli-Ereignis mit <c>p = λ · Dichte(t)</c> als „Poisson-Näherung"; der
    /// Generator zieht den Poisson-Prozess selbst: die Zahl der Ereignisse je Kategorie und Tag,
    /// dann ihre Zeitpunkte nach der Dichte. Die Erwartung ist dieselbe und exakt (keine Kappung
    /// von p bei 1), die Zahl der Ziehungen folgt den Ereignissen statt den 1440 Minuten.</para>
    ///
    /// <para><b>Kategorien ohne Anteil</b> (oder mit Rate 0) werden übersprungen und ziehen
    /// keinen Zufall; eine nicht endliche Rate lehnt benannt ab
    /// (<see cref="ZapfEingabefehler.StochastikUngueltig"/>), nie als Ausnahme der Poisson-Ziehung.</para>
    ///
    /// <para><b>Gestutztes Mittel.</b> V̄_k ist das Mittel des gekappten Volumenstroms
    /// (<see cref="Zapfverteilung.GestutztesMittel"/>) — der Erwartungswert trifft die Tagesmenge.
    /// Ein Tag ohne Menge oder mit leerem Tagesgang zieht nichts (der Zufallsstrom bleibt stehen).</para>
    /// </summary>
    internal static class Zapfereignisgenerator
    {
        /// <summary>
        /// Die Rate λ_k [Ereignisse je Tag] der Kategorie bei der Tagesmenge der Einheit und der
        /// Spreizung Zapftemperatur − Kaltwasser des Tages. Eine Kategorie ohne Anteil hat die Rate 0
        /// — auch wenn ihr gestutztes Mittel 0 ist (μ = σ = 0), wo die Formel 0/0 wäre.
        /// </summary>
        internal static double Rate(Zapfkategoriewert k, double tagesmengeKwh, double spreizungK)
        {
            if (!(k.AnteilNormiert > 0) || !(tagesmengeKwh > 0)) return 0.0;
            return k.AnteilNormiert * tagesmengeKwh / (k.EnergieJeKelvinKwh * spreizungK);
        }

        /// <summary>
        /// Zieht die Ereignisse einer Einheit an einem Tag (Formel oben) und hängt sie an
        /// <paramref name="ziel"/> an — je Kategorie in der Reihenfolge des Katalogs, je Ereignis in
        /// der Reihenfolge der Ziehung. Minute des Beginns 0 … 1439; der Aufrufer legt fest, ob ein
        /// Ereignis über Mitternacht in den nächsten Tag läuft oder am Tagesanfang weiterläuft.
        /// </summary>
        internal static void Ziehen(ZapfZufall z, Zapfkategoriensatz satz, double tagesmengeKwh, double spreizungK,
                                    Tageszeitdichte dichte, ICollection<Zapfereignis> ziel)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            if (satz == null) throw new ArgumentNullException(nameof(satz));
            if (dichte == null) throw new ArgumentNullException(nameof(dichte));
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (double.IsNaN(tagesmengeKwh) || double.IsInfinity(tagesmengeKwh) || tagesmengeKwh < 0)
                throw new ArgumentOutOfRangeException(nameof(tagesmengeKwh), "Die Tagesmenge ist keine endliche, nicht negative Zahl.");
            if (!(tagesmengeKwh > 0) || dichte.Leer) return;
            if (double.IsNaN(spreizungK) || double.IsInfinity(spreizungK) || !(spreizungK > 0))
                throw new ArgumentOutOfRangeException(nameof(spreizungK), "Die Spreizung Zapftemperatur − Kaltwasser ist nicht positiv.");

            foreach (Zapfkategoriewert k in satz.Werte)
            {
                // Eine Kategorie ohne Anteil oder mit Rate 0 zieht nichts — auch keinen Zufall.
                double rate = Rate(k, tagesmengeKwh, spreizungK);
                if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, satz.Zone,
                        "Nicht rechenbar — die Zapfkategorie „" + (k.Kategorie.Name ?? "") + "“ der Zone „" + satz.Zone
                        + "“ ergibt keine endliche Rate der Ereignisse.");
                if (!(rate > 0)) continue;
                int anzahl = z.Poisson(rate);
                if (anzahl == 0) continue;
                Zapfkategorie kat = k.Kategorie;
                double energieJeVolumenstrom = kat.DauerMin * Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * spreizungK
                                               / Mengengeruest.WH_JE_KWH;
                for (int j = 0; j < anzahl; j++)
                {
                    int minute = dichte.Minute(z);
                    double v = kat.VolumenstromLJeMin + kat.StreuungLJeMin * z.Normal();
                    if (v < 0) v = 0.0;
                    if (kat.KappungLJeMin.HasValue && v > kat.KappungLJeMin.Value) v = kat.KappungLJeMin.Value;
                    ziel.Add(new Zapfereignis(minute, kat.DauerMin, v * energieJeVolumenstrom));
                }
            }
        }
    }
}
