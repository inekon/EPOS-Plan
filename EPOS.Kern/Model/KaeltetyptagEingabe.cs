using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Typtag der Kältebedarfszuordnung (Konzept Kältebedarfsprofile 3.4): 24 Stundenwerte als relative Form und
    /// eine Tagesmenge als relatives Gewicht. Die Nummer 1 … 6 ist sein Name in der Matrix.
    /// </summary>
    public sealed class Kaeltetyptag
    {
        /// <summary>Nummer 1 … <see cref="KaeltetyptagSchema.NR_MAX"/>, eindeutig je Projektkopie.</summary>
        public int Nr { get; set; }

        /// <summary>Anzeigename („Sommerwerktag“ …); nie leer.</summary>
        public string Bezeichner { get; set; } = "";

        /// <summary>Die 24 Stundenwerte (Stunde 0 Uhr zuerst), je ≥ 0; nur das Verhältnis zählt.</summary>
        public double[] Stunden { get; set; } = new double[KaeltetyptagSchema.STUNDEN];

        /// <summary>Tagesmenge g_T (relatives Gewicht des Tages), ≥ 0; Vorgabe 1.</summary>
        public double Tagesmenge { get; set; } = 1.0;

        /// <summary>Summe der Stundenwerte Σ f_T.</summary>
        public double Formsumme()
        {
            double s = 0;
            if (Stunden != null) foreach (double f in Stunden) s += f;
            return s;
        }
    }

    /// <summary>
    /// Die Typtag-Eingabe EINER Projektkopie des Kältebedarfs: bis zu sechs Typtage und die Zuordnungsmatrix
    /// 12 Monate × 3 Tagesarten (Werktag, Samstag, Sonn-/Feiertag) → Nummer des Typtags (0 = keine Zuordnung).
    /// Plattformfrei und ohne Datenbank — <see cref="Typtagschicht"/> rechnet daraus die Jahresreihe,
    /// <see cref="KaeltetyptagCtrl"/> liest und schreibt sie.
    /// </summary>
    public sealed class KaeltetyptagEingabe
    {
        /// <summary>Die Typtage (höchstens sechs, Nummern eindeutig).</summary>
        public List<Kaeltetyptag> Typtage { get; } = new List<Kaeltetyptag>();

        /// <summary>Matrix [Monat 0 … 11, Tagesart 0 … 2] → Nummer des Typtags; 0 = Zelle leer.</summary>
        public int[,] Matrix { get; } = new int[KaeltetyptagSchema.MONATE, 3];

        /// <summary>Der Typtag mit Nummer <paramref name="nr"/>; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public Kaeltetyptag Typtag(int nr)
        {
            foreach (Kaeltetyptag t in Typtage)
                if (t.Nr == nr) return t;
            return null;
        }

        /// <summary>Gibt es weder Typtage noch eine gesetzte Zelle?</summary>
        public bool Leer
        {
            get
            {
                if (Typtage.Count > 0) return false;
                foreach (int nr in Matrix) if (nr != 0) return false;
                return true;
            }
        }

        /// <summary>
        /// Prüft die Eingabe gegen die Regeln des Schemas: Nummern 1 … 6 und eindeutig, Bezeichner nicht leer,
        /// 24 Stundenwerte ≥ 0 und endlich, Tagesmenge ≥ 0 und endlich, jede gesetzte Zelle zeigt auf einen Typtag.
        /// <c>null</c> = zulässig, sonst der Grund.
        /// </summary>
        public string Pruefen()
        {
            if (Typtage.Count > KaeltetyptagSchema.NR_MAX) return "Hoechstens " + KaeltetyptagSchema.NR_MAX + " Typtage";
            var nummern = new HashSet<int>();
            foreach (Kaeltetyptag t in Typtage)
            {
                if (t == null) return "Leerer Typtag";
                if (t.Nr < 1 || t.Nr > KaeltetyptagSchema.NR_MAX) return "Typtag-Nummer " + t.Nr + " ausserhalb 1 ... " + KaeltetyptagSchema.NR_MAX;
                if (!nummern.Add(t.Nr)) return "Typtag-Nummer " + t.Nr + " doppelt";
                if (string.IsNullOrWhiteSpace(t.Bezeichner)) return "Typtag " + t.Nr + " ohne Bezeichner";
                if (t.Stunden == null || t.Stunden.Length != KaeltetyptagSchema.STUNDEN) return "Typtag " + t.Nr + " ohne 24 Stundenwerte";
                foreach (double f in t.Stunden)
                    if (double.IsNaN(f) || double.IsInfinity(f) || f < 0) return "Typtag " + t.Nr + " mit unzulaessigem Stundenwert";
                if (double.IsNaN(t.Tagesmenge) || double.IsInfinity(t.Tagesmenge) || t.Tagesmenge < 0)
                    return "Typtag " + t.Nr + " mit unzulaessiger Tagesmenge";
            }
            for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                for (int a = 0; a < 3; a++)
                {
                    int nr = Matrix[m, a];
                    if (nr != 0 && !nummern.Contains(nr))
                        return "Matrixzelle Monat " + (m + 1) + "/" + KaeltetyptagSchema.TAGESARTEN[a] + " zeigt auf fehlenden Typtag " + nr;
                }
            return null;
        }
    }
}
