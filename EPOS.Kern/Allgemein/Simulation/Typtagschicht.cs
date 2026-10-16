using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Typtagschicht der Profilroutine</b> (Konzept Kältebedarfsprofile 3.4, Entscheid OE-1) — der Zweig neben
    /// der Kalenderschicht für Zuordnungen mit Eingabeart <c>'typtag'</c>. Ohne Datenbank und ohne Protokoll; die
    /// Meldungen gibt <see cref="ProfilBedarf"/> aus dem <see cref="Ergebnis"/> aus.
    ///
    /// <code>
    /// t(d)  = Sonntag    an einem Feiertag des Betriebskalenders oder am Sonntag des Rasters
    ///         Samstag    am Samstag des Rasters
    ///         Werktag    sonst
    /// a(h)  = g_T · f_T[s] / Σ f_T,  T = Matrix[m(d), t(d)]            h = 24 d + s (der Tag ohne Ferien)
    /// b(h)  = a(h), an einem Ferientag f · g_W · f_W[s] / Σ f_W,  W = Matrix[m(d), Werktag]
    ///         mit „Ferien wie Sonntag“ an einem Ferientag g_S · f_S[s] / Σ f_S,  S = Matrix[m(d), Sonntag] (ohne f)
    /// q(h)  = b(h) / Σ_Jahr b · S · 1000                  Ferien verteilen um (Vorgabe)
    /// q(h)  = b(h) / Σ_Jahr a · S · 1000                  „Ferien kürzen die Menge“
    /// </code>
    /// S die Jahressumme [MWh], q in kWh. Die Monatsanteile entfallen in dieser Art — die Matrix trägt den Jahresgang.
    /// Der Wochentag folgt dem Raster des Projekts (w₀ = Wochentag des 1. Januar, Montag = 0), wie die übrigen Leser.
    ///
    /// <para><b>Lücken werden benannt, nicht still gefüllt:</b> Ein Tag, dessen Matrixzelle leer ist oder auf einen
    /// fehlenden Typtag zeigt, und ein Tag mit einem Typtag ohne Stundenwerte (Σ f_T = 0) tragen keinen Bedarf; die
    /// Jahressumme verteilt sich auf die übrigen Tage, und das Ergebnis nennt die Zellen bzw. Typtage
    /// (<see cref="Ergebnis.FehlendeZellen"/>, <see cref="Ergebnis.LeereTyptage"/>). Eine Tagesmenge 0 ist dagegen
    /// gewollt und wird nicht gemeldet. Bleibt im ganzen Jahr kein Bedarf, rechnet die Schicht nichts
    /// (<see cref="Ergebnis.Gerechnet"/> = <c>false</c>).</para>
    ///
    /// <para><b>Randfall Ferien:</b> Hat das Jahr ohne Kürzen nur Ferienstunden mit f = 0 als Bedarf, gilt die Reihe ohne
    /// Ferienfaktor (<see cref="Ergebnis.FerienOhneWirkung"/>), wie die Kalenderschicht je Monat.</para>
    /// </summary>
    public static class Typtagschicht
    {
        private const int STUNDEN = 8760;
        private const int TAG = 24;
        private const int TAGE = 365;
        private const int MONATE = 12;

        /// <summary>Was die Schicht gerechnet und was sie ausgelassen hat.</summary>
        public sealed class Ergebnis
        {
            /// <summary>Wurde eine Reihe geschrieben? <c>false</c> = kein Bedarf im ganzen Jahr (Reihe bleibt 0).</summary>
            public bool Gerechnet;

            /// <summary>Die leeren bzw. ins Leere zeigenden Zellen, die im Jahr vorkommen, als „Monat/Tagesart“.</summary>
            public List<string> FehlendeZellen = new List<string>();

            /// <summary>Die Nummern der benutzten Typtage ohne Stundenwerte.</summary>
            public List<int> LeereTyptage = new List<int>();

            /// <summary>Zahl der Tage ohne Bedarf wegen einer Lücke (Zelle oder leerer Typtag).</summary>
            public int TageOhneTyptag;

            /// <summary>Galt die Reihe wegen des Randfalls ohne Ferienfaktor?</summary>
            public bool FerienOhneWirkung;
        }

        /// <summary>
        /// Rechnet die Jahresreihe [kWh] aus der Typtag-Eingabe und schreibt sie nach <paramref name="outJahr"/>
        /// (überschrieben, nicht addiert).
        /// </summary>
        /// <param name="eingabe">Typtage und Matrix.</param>
        /// <param name="jahressummeMwh">Jahressumme S [MWh]; ≤ 0 ergibt eine Nullreihe mit <c>Gerechnet = false</c>.</param>
        /// <param name="outJahr">Ziel [8760].</param>
        /// <param name="moAnfang">Stundenindex der Monatsanfänge (12 Werte), wie bei der Kalenderschicht.</param>
        /// <param name="wochentagJan1">Wochentag des 1. Januar im Raster, Montag = 0 … Sonntag = 6.</param>
        /// <param name="tagesarten">365 Tagesarten des Betriebskalenders (<see cref="Betriebskalender.Tagesarten"/>);
        /// <c>null</c> = ohne Kalender (keine Feiertage, keine Ferien).</param>
        /// <param name="ferienfaktor">Ferienfaktor f (0 … 1) des Kalenders.</param>
        /// <param name="ferienKuerzen">Kürzen die Ferien die Menge (statt umzuverteilen)?</param>
        /// <param name="ferienWieSonntag">
        /// Nimmt ein Ferientag den Sonntag-Typtag seines Monats (<see cref="Betriebskalender.FerienWieSonntag"/>)?
        /// <paramref name="ferienfaktor"/> wirkt dann nicht. Vorgabe nein: der Werktag mit Ferienfaktor wie zuvor.
        /// </param>
        public static Ergebnis TyptageZuJahr(KaeltetyptagEingabe eingabe, double jahressummeMwh, double[] outJahr,
                                             int[] moAnfang, int wochentagJan1, byte[] tagesarten,
                                             double ferienfaktor, bool ferienKuerzen,
                                             bool ferienWieSonntag = false)
        {
            if (eingabe == null) throw new ArgumentNullException(nameof(eingabe));
            if (outJahr == null || outJahr.Length < STUNDEN) throw new ArgumentException("8760 Stunden erwartet.", nameof(outJahr));
            if (moAnfang == null || moAnfang.Length < MONATE) throw new ArgumentException("12 Monatsanfaenge erwartet.", nameof(moAnfang));
            if (tagesarten != null && tagesarten.Length < TAGE) throw new ArgumentException("365 Tagesarten erwartet.", nameof(tagesarten));

            var erg = new Ergebnis();
            Array.Clear(outJahr, 0, STUNDEN);

            var a = new double[STUNDEN];
            var b = new double[STUNDEN];
            int w0 = ((wochentagJan1 % 7) + 7) % 7;
            var fehlend = new SortedSet<int>();
            var leer = new SortedSet<int>();

            int monat = 0;
            for (int d = 0; d < TAGE; d++)
            {
                while (monat < MONATE - 1 && d * TAG >= moAnfang[monat + 1]) monat++;

                byte art = tagesarten != null ? tagesarten[d] : Betriebskalender.TAG_NORMAL;
                bool ferien = art == Betriebskalender.TAG_FERIEN;
                int wochentag = (w0 + d) % 7;
                int spalte = art == Betriebskalender.TAG_FEIERTAG || wochentag == 6 ? 2 : wochentag == 5 ? 1 : 0;

                // a: der Tag ohne Ferien (Typtag seines Wochentags), b: mit Ferien (Werktag · f, mit „Ferien wie
                // Sonntag“ der Sonntag-Typtag des Monats ohne f).
                Tagesform(eingabe, monat, spalte, d, a, 1.0, true, fehlend, leer, erg);
                if (ferien && ferienWieSonntag)
                    Tagesform(eingabe, monat, 2, d, b, 1.0, false, fehlend, leer, erg);
                else if (ferien)
                    Tagesform(eingabe, monat, 0, d, b, ferienfaktor, false, fehlend, leer, erg);
                else
                    Array.Copy(a, d * TAG, b, d * TAG, TAG);
            }

            foreach (int z in fehlend)
                erg.FehlendeZellen.Add((z / 3 + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" +
                                       KaeltetyptagSchema.TAGESARTEN[z % 3]);
            erg.LeereTyptage.AddRange(leer);

            double summeA = 0, summeB = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                summeA += a[h];
                summeB += b[h];
            }

            double[] reihe;
            double nenner;
            if (ferienKuerzen) { reihe = b; nenner = summeA; }
            else if (summeB > 0) { reihe = b; nenner = summeB; }
            else { reihe = a; nenner = summeA; erg.FerienOhneWirkung = summeA > 0; }

            if (!(nenner > 0) || !(jahressummeMwh > 0)) return erg;

            double faktor = jahressummeMwh * 1000.0 / nenner;
            for (int h = 0; h < STUNDEN; h++) outJahr[h] = reihe[h] * faktor;
            erg.Gerechnet = true;
            return erg;
        }

        /// <summary>
        /// Schreibt den Tag <paramref name="d"/> mit dem Typtag der Zelle [<paramref name="monat"/>, <paramref name="spalte"/>]
        /// · <paramref name="faktor"/> nach <paramref name="ziel"/>; eine Lücke bleibt 0 und wird vermerkt.
        /// </summary>
        private static void Tagesform(KaeltetyptagEingabe eingabe, int monat, int spalte, int d, double[] ziel, double faktor, bool zaehlen,
                                      SortedSet<int> fehlend, SortedSet<int> leer, Ergebnis erg)
        {
            int nr = eingabe.Matrix[monat, spalte];
            Kaeltetyptag t = nr != 0 ? eingabe.Typtag(nr) : null;
            if (t == null)
            {
                fehlend.Add(monat * 3 + spalte);
                if (zaehlen) erg.TageOhneTyptag++;
                return;
            }
            double form = t.Formsumme();
            if (!(form > 0))
            {
                if (t.Tagesmenge > 0)
                {
                    leer.Add(t.Nr);
                    if (zaehlen) erg.TageOhneTyptag++;
                }
                return;
            }
            for (int s = 0; s < TAG; s++)
                ziel[d * TAG + s] = faktor * t.Tagesmenge * t.Stunden[s] / form;
        }
    }
}
