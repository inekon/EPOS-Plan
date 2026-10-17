using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Zeitprogramm eines Erzeugers</b> (AK2-1; Anlagenkopplung 4.3, 8.2, F7): 168 Verfügbarkeitsfaktoren
    /// 0 … 1 aus <c>Tab_Energieanlagen.Zeitprogramm</c>, Montag 00:00 bis Sonntag 23:00, im Format des
    /// Sollwertprofils (<see cref="AnlagenkopplungSchema.WochenprofilLesen"/>, Trennzeichen <c>;</c>,
    /// Dezimalpunkt).
    ///
    /// <para><b>Streng, nicht tolerant:</b> NULL oder leer heißt „immer verfügbar" (168 × 1). Eine falsche
    /// Wertzahl (etwa 167), ein Wert, der keine Zahl ist, oder ein Faktor außerhalb 0 … 1 ist ein
    /// <b>benannter</b> Fehler (<see cref="AnlagenzeitprogrammFehler"/>) — kein Auffüllen, kein Abschneiden,
    /// kein Begrenzen.</para>
    ///
    /// <para><b>Wochenbeginn wie beim Sollwertprofil:</b> Stunde 0 des Rechenjahres liegt auf dem Wochentag,
    /// den die Wochenendmaske des Ortszeit-Kalenders ergibt (<see cref="WochentagAusWochenende"/>, dieselbe
    /// Regel wie <c>GebaeudeModellEingang.WochentagDesErstenTags</c>); <see cref="Faktor"/> liest dann
    /// <c>(wochentag0 · 24 + stunde) mod 168</c>. Ohne Kalender beginnt das Jahr am Montag.</para>
    ///
    /// <para>Noch ohne Wirkung im Rechenweg — die Klasse ist der Leser, den der Anlagenfahrplan (AK2-2) nimmt.
    /// Die Sperrzeit der Anlage geht dem Zeitprogramm vor (F7); das entscheidet der Fahrplan, nicht der Leser.</para>
    /// </summary>
    public sealed class Anlagenzeitprogramm
    {
        /// <summary>Zahl der Wochenstunden: Montag 00:00 bis Sonntag 23:00.</summary>
        public const int WOCHENSTUNDEN = AnlagenkopplungSchema.WOCHENWERTE;

        private readonly double[] _faktoren;

        private Anlagenzeitprogramm(double[] faktoren, bool gepflegt, int wochentagDesErstenTags)
        {
            _faktoren = faktoren;
            Gepflegt = gepflegt;
            WochentagDesErstenTags = wochentagDesErstenTags;
        }

        /// <summary>Das Zeitprogramm ohne Eintrag: immer verfügbar, Wochenbeginn Montag.</summary>
        public static Anlagenzeitprogramm ImmerVerfuegbar { get; } = new Anlagenzeitprogramm(Einsen(), false, 0);

        /// <summary>Trug die Anlage ein Zeitprogramm? <c>false</c> = NULL oder leer, also immer verfügbar.</summary>
        public bool Gepflegt { get; }

        /// <summary>Der Wochentag der Stunde 0 des Rechenjahres: 0 = Montag … 6 = Sonntag.</summary>
        public int WochentagDesErstenTags { get; }

        /// <summary>Die 168 Faktoren in Wochenordnung (Montag 00:00 zuerst).</summary>
        public IReadOnlyList<double> Faktoren => _faktoren;

        /// <summary>
        /// Der Verfügbarkeitsfaktor in der Stunde <paramref name="stunde"/> des Rechenjahres (0 … 8759, jede
        /// nichtnegative Zahl ist zulässig) — über <c>(wochentag0 · 24 + stunde) mod 168</c>.
        /// </summary>
        public double Faktor(int stunde)
        {
            if (stunde < 0) throw new ArgumentOutOfRangeException(nameof(stunde));
            return _faktoren[(WochentagDesErstenTags * 24 + stunde) % WOCHENSTUNDEN];
        }

        /// <summary>Dasselbe Programm mit einem anderen Wochenbeginn (0 = Montag … 6 = Sonntag).</summary>
        public Anlagenzeitprogramm ImKalender(int wochentagDesErstenTags)
        {
            if (wochentagDesErstenTags < 0 || wochentagDesErstenTags > 6)
                throw new ArgumentOutOfRangeException(nameof(wochentagDesErstenTags));
            return new Anlagenzeitprogramm(_faktoren, Gepflegt, wochentagDesErstenTags);
        }

        /// <summary>
        /// Der Wochentag des ersten Tags aus der Wochenendmaske des Ortszeit-Kalenders — dieselbe Regel wie
        /// beim Sollwertprofil; <c>-1</c>, wenn die Maske keine regelmäßige Woche ist.
        /// </summary>
        public static int WochentagAusWochenende(bool[] wochenende)
            => GebaeudeModellEingang.WochentagDesErstenTags(wochenende);

        /// <summary>
        /// <b>Der strenge Leser.</b> NULL oder leer = <see cref="ImmerVerfuegbar"/>; sonst genau 168 Faktoren
        /// 0 … 1. Jeder Verstoß wirft <see cref="AnlagenzeitprogrammFehler"/> mit dem Text der Ressource.
        /// </summary>
        /// <param name="text">Der Inhalt von <c>Tab_Energieanlagen.Zeitprogramm</c>.</param>
        /// <param name="anlage">Der Bezeichner der Anlage — für die Meldung.</param>
        /// <param name="wochentagDesErstenTags">Wochentag der Stunde 0 (0 = Montag).</param>
        public static Anlagenzeitprogramm Lesen(string text, string anlage = null, int wochentagDesErstenTags = 0)
        {
            if (wochentagDesErstenTags < 0 || wochentagDesErstenTags > 6)
                throw new ArgumentOutOfRangeException(nameof(wochentagDesErstenTags));
            AnlagenkopplungSchema.Wochenprofil p = AnlagenkopplungSchema.WochenprofilLesen(text);
            CultureInfo k = CultureInfo.CurrentCulture;
            string name = anlage ?? "";
            switch (p.Befund)
            {
                case AnlagenkopplungSchema.WochenprofilBefund.KeinProfil:
                    return wochentagDesErstenTags == 0
                        ? ImmerVerfuegbar
                        : new Anlagenzeitprogramm(Einsen(), false, wochentagDesErstenTags);
                case AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl:
                    throw new AnlagenzeitprogrammFehler(AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl, 0,
                        string.Format(k, MyResource.Resource.SIMENG_AK2_ZEITPROGRAMM_WERTZAHL, name, p.Gefunden, WOCHENSTUNDEN));
                case AnlagenkopplungSchema.WochenprofilBefund.KeineZahl:
                    throw new AnlagenzeitprogrammFehler(AnlagenkopplungSchema.WochenprofilBefund.KeineZahl, p.Stelle,
                        string.Format(k, MyResource.Resource.SIMENG_AK2_ZEITPROGRAMM_KEINE_ZAHL, name, p.Stelle));
            }

            double[] werte = p.Werte;
            for (int i = 0; i < werte.Length; i++)
                if (werte[i] < 0.0 || werte[i] > 1.0)
                    throw new AnlagenzeitprogrammFehler(AnlagenkopplungSchema.WochenprofilBefund.Gelesen, i + 1,
                        string.Format(k, MyResource.Resource.SIMENG_AK2_ZEITPROGRAMM_WERT, name, i + 1,
                                      werte[i].ToString("0.###", k)));
            return new Anlagenzeitprogramm(werte, true, wochentagDesErstenTags);
        }

        /// <summary>Prüft einen Text ohne zu werfen: <c>null</c> = gültig (auch leer), sonst die Meldung.</summary>
        public static string Pruefen(string text, string anlage = null)
        {
            try
            {
                Lesen(text, anlage);
                return null;
            }
            catch (AnlagenzeitprogrammFehler f)
            {
                return f.Message;
            }
        }

        private static double[] Einsen()
        {
            var e = new double[WOCHENSTUNDEN];
            for (int i = 0; i < e.Length; i++) e[i] = 1.0;
            return e;
        }
    }

    /// <summary>
    /// Ein ungültiges Zeitprogramm (<see cref="Anlagenzeitprogramm.Lesen"/>): falsche Wertzahl, keine Zahl
    /// oder ein Faktor außerhalb 0 … 1. Die Meldung kommt aus der Ressource, in der Sprache des Anwenders.
    /// </summary>
    public sealed class AnlagenzeitprogrammFehler : FormatException
    {
        internal AnlagenzeitprogrammFehler(AnlagenkopplungSchema.WochenprofilBefund befund, int stelle, string meldung)
            : base(meldung)
        {
            Befund = befund;
            Stelle = stelle;
        }

        /// <summary>
        /// Der Befund des Lesers: <see cref="AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl"/>,
        /// <see cref="AnlagenkopplungSchema.WochenprofilBefund.KeineZahl"/> oder — bei einem Faktor außerhalb
        /// 0 … 1 — <see cref="AnlagenkopplungSchema.WochenprofilBefund.Gelesen"/> mit der <see cref="Stelle"/>.
        /// </summary>
        public AnlagenkopplungSchema.WochenprofilBefund Befund { get; }

        /// <summary>Die Stelle (1 … 168) des fehlerhaften Werts; 0 bei falscher Wertzahl.</summary>
        public int Stelle { get; }

        /// <summary>Steht ein Faktor außerhalb 0 … 1?</summary>
        public bool AusserhalbBereich => Befund == AnlagenkopplungSchema.WochenprofilBefund.Gelesen;
    }
}
