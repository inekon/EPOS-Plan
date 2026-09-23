using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Tagtyp eines Kalendertags (Konzept 2.1, 4.2); die Zahl ist die Spalte
    /// <c>Tab_TwwTagesgang_STAMM.Tagtyp</c>.
    /// </summary>
    internal enum ZapfTagtyp
    {
        Werktag = 1,
        Samstag = 2,

        /// <summary>Sonntag und Feiertag — ein Feiertag trägt Sonntagsmenge und Sonntagsgang.</summary>
        SonnFeiertag = 3,

        /// <summary>Ferien- bzw. Ruhetag der Zone.</summary>
        Ruhetag = 4
    }

    /// <summary>Ein zusammenhängendes Ferienfenster, Jahrestage 1 … 365 einschließlich, <see cref="Beginn"/> ≤ <see cref="Ende"/>.</summary>
    internal sealed record Ferienfenster(int Beginn, int Ende)
    {
        /// <summary>Liegt der Jahrestag im Fenster?</summary>
        internal bool Enthaelt(int tag) => tag >= Beginn && tag <= Ende;
    }

    /// <summary>
    /// <b>Schicht S2 — der Kalender</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 4.2; A6):
    /// 365 Tage ohne Schaltjahr, der Tagtyp je Tag aus Wochentag, den Kennzeichen „Wochenende
    /// oder Feiertag" der Klimaregion und den Ferienfenstern der Zone.
    ///
    /// <code>
    /// Wochentag(d) = (WochentagJan1 + d − 1) mod 7        Montag = 0 … Sonntag = 6
    /// Ferienfenster enthält d                  -> Ruhetag
    /// sonst Wochentag = Samstag                -> Samstag
    /// sonst Wochentag = Sonntag oder We[d]     -> SonnFeiertag   (Feiertag wie Sonntag)
    /// sonst                                    -> Werktag
    /// </code>
    ///
    /// <para>Ein Feiertag am Samstag bleibt Samstag (Konzept 4.2). Anders als die Zeile des
    /// Papiers („We[d] und Samstag") entscheidet der Wochentag allein über Samstag und Sonntag;
    /// bei stimmigen Klimadaten (jedes Wochenende gekennzeichnet) ist das dasselbe, bei einer
    /// Lücke der Kennzeichen bleibt ein Samstag Samstag und ein Sonntag Sonntag.</para>
    /// </summary>
    internal static class Zapfkalender
    {
        /// <summary>Tage des Rechenjahres — fest, ohne Schaltjahr.</summary>
        internal const int TAGE = 365;

        /// <summary>Stunden eines Tages.</summary>
        internal const int STUNDEN_TAG = 24;

        /// <summary>Stunden des Rechenjahres.</summary>
        internal const int STUNDEN_JAHR = TAGE * STUNDEN_TAG;

        /// <summary>Monate des Rechenjahres.</summary>
        internal const int MONATE = 12;

        /// <summary>Wochentage.</summary>
        internal const int WOCHENTAGE = 7;

        /// <summary>Index des Samstags (Montag = 0).</summary>
        internal const int SAMSTAG = 5;

        /// <summary>Index des Sonntags (Montag = 0).</summary>
        internal const int SONNTAG = 6;

        /// <summary>Tage je Monat im Jahr ohne Schaltjahr.</summary>
        internal static readonly IReadOnlyList<int> TageJeMonat = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Der Wochentag (Montag = 0 … Sonntag = 6) des Jahrestags 1 … 365.</summary>
        internal static int Wochentag(int wochentagJan1, int tag) => (wochentagJan1 + tag - 1) % WOCHENTAGE;

        /// <summary>Der Monat 1 … 12 des Jahrestags 1 … 365.</summary>
        internal static int Monat(int tag)
        {
            if (tag < 1 || tag > TAGE) throw new ArgumentOutOfRangeException(nameof(tag));
            int rest = tag;
            for (int m = 0; m < MONATE; m++)
            {
                if (rest <= TageJeMonat[m]) return m + 1;
                rest -= TageJeMonat[m];
            }
            return MONATE;
        }

        /// <summary>
        /// Die Ferienfenster aus einem Paar Jahrestage (Konzept 4.2): <c>null</c>, 0 und 366
        /// heißen „keine Angabe", Werte über 365 werden auf 365 gekappt. Beginn leer und Ende
        /// gesetzt ergibt 1 … Ende; Beginn &gt; Ende ergibt Beginn … 365 und 1 … Ende; Ende leer
        /// ergibt kein Fenster. Zurück kommen null, ein oder zwei Fenster.
        /// </summary>
        internal static IReadOnlyList<Ferienfenster> AusJahrestagen(int? beginn, int? ende)
        {
            int? b = Angabe(beginn);
            int? e = Angabe(ende);
            if (!e.HasValue) return new Ferienfenster[0];
            if (!b.HasValue) return new[] { new Ferienfenster(1, e.Value) };
            if (b.Value <= e.Value) return new[] { new Ferienfenster(b.Value, e.Value) };
            return new[] { new Ferienfenster(b.Value, TAGE), new Ferienfenster(1, e.Value) };
        }

        /// <summary>Die Ferienfenster einer Zone aus ihren vier Paaren.</summary>
        internal static IReadOnlyList<Ferienfenster> FensterDerZone(ZonenStand z)
        {
            var fenster = new List<Ferienfenster>();
            int anzahl = Math.Max(z.Ferienbeginn?.Length ?? 0, z.Ferienende?.Length ?? 0);
            for (int i = 0; i < anzahl; i++)
            {
                int? b = z.Ferienbeginn != null && i < z.Ferienbeginn.Length ? z.Ferienbeginn[i] : null;
                int? e = z.Ferienende != null && i < z.Ferienende.Length ? z.Ferienende[i] : null;
                fenster.AddRange(AusJahrestagen(b, e));
            }
            return fenster;
        }

        /// <summary>
        /// <b>Der Kalender eines Jahres</b>: 365 Tagtypen. Ein Kalender der Klimaregion mit
        /// anderer Länge oder ein Wochentag außerhalb 0 … 6 wird benannt abgelehnt.
        /// </summary>
        internal static ZapfTagtyp[] Bilden(int wochentagJan1, bool[] we, IReadOnlyList<Ferienfenster> ferien)
        {
            Pruefen(wochentagJan1, we);
            var kalender = new ZapfTagtyp[TAGE];
            for (int d = 1; d <= TAGE; d++)
            {
                int wt = Wochentag(wochentagJan1, d);
                ZapfTagtyp typ;
                if (InFerien(ferien, d)) typ = ZapfTagtyp.Ruhetag;
                else if (wt == SAMSTAG) typ = ZapfTagtyp.Samstag;
                else if (wt == SONNTAG || we[d - 1]) typ = ZapfTagtyp.SonnFeiertag;
                else typ = ZapfTagtyp.Werktag;
                kalender[d - 1] = typ;
            }
            return kalender;
        }

        /// <summary>Prüft Wochentag und Kennzeichen des Klimakalenders; benannte Ablehnung bei Fehlern.</summary>
        internal static void Pruefen(int wochentagJan1, bool[] we)
        {
            if (wochentagJan1 < 0 || wochentagJan1 >= WOCHENTAGE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.KalenderUngueltig, "",
                    "Nicht rechenbar — der Wochentag des 1. Januar liegt nicht in 0 … 6.");
            if (we == null || we.Length != TAGE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.KalenderUngueltig, "",
                    "Nicht rechenbar — der Kalender der Klimaregion trägt nicht 365 Tage.");
        }

        private static bool InFerien(IReadOnlyList<Ferienfenster> ferien, int tag)
        {
            if (ferien == null) return false;
            foreach (Ferienfenster f in ferien)
                if (f != null && f.Enthaelt(tag)) return true;
            return false;
        }

        private static int? Angabe(int? jahrestag)
        {
            if (!jahrestag.HasValue) return null;
            int t = jahrestag.Value;
            if (t <= 0 || t == TAGE + 1) return null;
            return t > TAGE ? TAGE : t;
        }
    }
}
