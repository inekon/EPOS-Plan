using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>KBP-5a</b> (Entscheid OE-5): die Eingabearten Monatswerte und Wochenprofil der Prozesswärme- und
    /// Brauchwasserzeilen im <c>BedarfsProfileDialog</c> — dieselben Felder der Dialogzeile wie bei der Kälte
    /// (<see cref="KaeltebedarfsprofilAbbildung"/>), plattformfrei, damit die Windows-Hülle, der Gebäudekatalog und der
    /// Schreibweg des Zapfprofils sie gleich abbilden. Ohne Projektkopie (Assistent vor dem Speichern) bleibt die
    /// Eingabeart der Zeile leer — der Dialog bietet sie dann nicht an, die Zeile rechnet mit der Jahressumme.
    /// </summary>
    internal static class BedarfsprofilEingabeAbbildung
    {
        /// <summary>Die Projektkopie-Tabellen einer Bedarfsart; <c>null</c> = die Art führt keine Eingabeart.</summary>
        internal static Bedarfsprofilkopie.Tabellen Tabellen(BedarfsArt art) => art switch
        {
            BedarfsArt.Prozesswaerme => Bedarfsprofilkopie.Prozesswaerme,
            BedarfsArt.Brauchwasser => Bedarfsprofilkopie.Brauchwasser,
            _ => null,
        };

        /// <summary>
        /// Belegt Eingabeart, Monatsanteile und Wochenprofil der Zeile aus dem Modell (<paramref name="monate"/> und
        /// <paramref name="woche"/> = bearbeitet, noch nicht gespeichert) oder aus der Projektkopie, auf die die Zeile zeigt.
        /// </summary>
        internal static void InZeile(BedarfsProfilZeile zeile, BedarfsArt art, int idProjekt, string eingabeart,
                                     double[] monate, double[] woche)
        {
            Bedarfsprofilkopie.Tabellen t = Tabellen(art);
            if (zeile == null || t == null) return;
            double[] m = monate ?? (idProjekt > 0 ? Bedarfsprofilkopie.Monate(t, zeile.IdStamm, idProjekt) : null);
            double[] w = woche ?? (idProjekt > 0 ? Bedarfsprofilkopie.Woche(t, zeile.IdStamm, idProjekt) : null);
            if (m == null || w == null)
            {
                zeile.Eingabeart = null;
                return;
            }
            zeile.Eingabeart = BedarfsprofilEingabeartSchema.Eingabeart(eingabeart);
            zeile.Monatsanteile = m;
            zeile.Wochenprofil = w;
            zeile.ProfilGeaendert = monate != null || woche != null;
        }

        /// <summary>Eine neu übernommene Zeile: Eingabeart 'jahr', Monate und Woche ihrer Projektkopie.</summary>
        internal static void Aufgenommen(BedarfsProfilZeile zeile, BedarfsArt art, int idProjekt)
            => InZeile(zeile, art, idProjekt, KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR, null, null);

        /// <summary>
        /// Der Rückweg in die Modellfelder: die Eingabeart immer, Monate und Woche nur, wenn der Anwender sie übernommen hat.
        /// </summary>
        internal static (string Eingabeart, double[] Monate, double[] Woche) AusZeile(BedarfsProfilZeile zeile)
            => zeile == null
                ? (KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR, null, null)
                : (BedarfsprofilEingabeartSchema.Eingabeart(zeile.Eingabeart),
                   zeile.ProfilGeaendert ? zeile.Monatsanteile : null,
                   zeile.ProfilGeaendert ? zeile.Wochenprofil : null);
    }
}
