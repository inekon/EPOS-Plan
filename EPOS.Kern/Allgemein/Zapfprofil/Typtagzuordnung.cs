using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Anbindung der eingespielten Typtage an eine Rechnung</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.2, 5.3; Stufe Z4b): der eingespielte Satz, die gewählte Klimazone
    /// und Gebäudeart und das Wetter des Jahres. Steht sie im
    /// <see cref="Zapfprofileingang.Typtage"/>, rechnet der Jahresgang über die Typtage; steht
    /// sie nicht, rechnet er wie im Bestand über den Formvektor (die Weiche, 2.2).
    ///
    /// <para><b>Nur Daten.</b> Kein Feld kennt eine Datenbank oder einen Dienst; die
    /// Tagesmittel der Außentemperatur liest der Controller aus <c>Tab_Klimadaten.Temperatur</c>,
    /// den Bedeckungsgrad in Achteln aus <c>Tab_Solar.Bedeckungsgrad</c>.</para>
    /// </summary>
    internal sealed record Typtaganbindung
    {
        /// <summary>Der eingespielte Satz; <c>null</c> oder leer heißt „nicht verfügbar".</summary>
        public Normformvektorsatz Daten { get; init; }

        /// <summary>Die gewählte Klimazone des Pakets.</summary>
        public int Klimazone { get; init; }

        /// <summary>Die gewählte Gebäudeart des Pakets (etwa die Variante des Wohngebäudes).</summary>
        public string Gebaeudeart { get; init; } = "";

        /// <summary>Die 365 Tagesmittel der Außentemperatur [°C].</summary>
        public double[] TagesmittelC { get; init; }

        /// <summary>
        /// Die 365 Tagesmittel des Bedeckungsgrads [Achtel, 0 … 8]; <c>null</c> = keine Angabe.
        /// Ohne sie rechnet nur ein Paket, dessen Kategorien nicht nach Bewölkung unterscheiden.
        /// </summary>
        public double[] BedeckungAchtel { get; init; }
    }

    /// <summary>Ein Kalendertag mit seinem Typtag: Jahrestag, Code, die drei Merkmale und der Faktor.</summary>
    internal sealed record Typtagtag(int Tag, string Typtag, Typtagjahreszeit Jahreszeit, Typtagart Tagart,
                                     Typtagbewoelkung Bewoelkung, double Faktor);

    /// <summary>
    /// Das zugeordnete Jahr: 365 <see cref="Tage"/> und die gerechnete Zahl der Kalendertage je
    /// Kategorie (<see cref="AnzahlJeTyptag"/>) — die Kontrollgröße gegen die eingespielte
    /// Tabelle.
    /// </summary>
    internal sealed class Typtagjahr
    {
        /// <summary>Die 365 Kalendertage in der Reihenfolge des Jahres.</summary>
        internal IReadOnlyList<Typtagtag> Tage { get; init; } = new Typtagtag[0];

        /// <summary>Je Typtagcode die gerechnete Zahl der Kalendertage.</summary>
        internal IReadOnlyDictionary<string, int> AnzahlJeTyptag { get; init; } = new Dictionary<string, int>();

        /// <summary>Die Klimazone, für die das Jahr gilt.</summary>
        internal int Klimazone { get; init; }

        /// <summary>Die Gebäudeart, für die das Jahr gilt.</summary>
        internal string Gebaeudeart { get; init; } = "";

        /// <summary>Die 365 Faktoren der Tagesenergie in Tagesreihenfolge.</summary>
        internal double[] Faktoren => Tage.Select(t => t.Faktor).ToArray();
    }

    /// <summary>
    /// <b>Schicht S2, Typtagweg — die Zuordnung der eingespielten Typtage zum Kalender</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 4.2 „VDI-4655-Typtage (Z4b)", Grundlagen 5,
    /// Abschnitte 2.2 und 2.5).
    ///
    /// <code>
    /// Jahreszeit(d) = Sommer     Tagesmittel(d) &gt; Heizgrenze(Gebäudeart)
    ///               = Winter     Tagesmittel(d) &lt; Wintergrenze
    ///               = Übergang   sonst
    /// Tagart(d)     = Sonntag    Kennzeichen „Wochenende oder Feiertag" und Wochentag ≠ Samstag
    ///               = Werktag    sonst (Montag bis Samstag)
    /// Bewölkung(d)  = bewölkt    Tagesmittel des Bedeckungsgrads ≥ Schwelle
    ///               = heiter     sonst;  entfällt, wo die Kategorie nicht unterscheidet
    /// Q_TT(d) = Q_a · (1/365 + n_E · F_TT(d)),  F_TT eines Typtags mit negativem Ergebnis
    ///           auf 0 gesetzt (dann Q_TT = Q_a/365), danach so skaliert, dass Σ_d Q_d = Q_a bleibt
    /// </code>
    ///
    /// <para><b>Jede Grenze kommt aus dem Paket</b> (Konzept Kapitel 6 (a)): Heizgrenze je
    /// Gebäudeart, Wintergrenze und Bewölkungsschwelle stehen in <c>kennwerte.csv</c> des
    /// Anwenders, nie im Quelltext. Fehlt eine, lehnt die Zone benannt ab.</para>
    ///
    /// <para><b>Was der Typtagweg NICHT tut.</b> Er ersetzt allein den Jahresgang der Bilanz.
    /// Die Auslegung (Wochenreihe, Bedarfstag, Summenlinie) bleibt unberührt — die
    /// Referenzlastprofile sind ausdrücklich nicht für Auslegungsspitzen gedacht (Grundlagen 5,
    /// Abschnitt 7.4). Den Urlaubstag der Richtlinie (kein Warmwasser) gibt es hier nicht: Ein
    /// Ferientag der Zone bleibt ein Werktag oder Sonntag seiner Jahreszeit, damit die
    /// Jahresenergie erhalten bleibt; die Ferienregel des Formvektors wirkt auf dem Typtagweg
    /// nicht, und ein Hinweis nennt das.</para>
    /// </summary>
    internal static class Typtagzuordnung
    {
        /// <summary>Kennung des Hinweises: Die gerechnete Zahl der Typtage weicht von der eingespielten ab.</summary>
        internal const string HINWEIS_ANZAHL = "TYPTAGE_ANZAHL";

        /// <summary>
        /// Kennung des Hinweises: Der Faktor eines Typtags ist auf 0 gesetzt, weil die Gleichung
        /// für ihn einen negativen Tagesbedarf ergäbe (Grundlagen 5, Abschnitt 2.5, Anmerkung zu
        /// Gl. (1)–(3)).
        /// </summary>
        internal const string HINWEIS_FAKTOR_NULL = "TYPTAGE_FAKTOR_NULL";

        /// <summary>Kennung des Hinweises: Die Tagesmengen mussten auf die Jahresmenge skaliert werden.</summary>
        internal const string HINWEIS_SKALIERUNG = "TYPTAGE_SKALIERUNG";

        /// <summary>Kennung des Hinweises: Die Ferienfenster der Zone wirken auf dem Typtagweg nicht.</summary>
        internal const string HINWEIS_FERIEN = "TYPTAGE_FERIEN_OHNE_WIRKUNG";

        /// <summary>Kennung des Hinweises: Das Paket führt keine Tagesgänge — die Tagesform kommt aus dem Tagesgangsatz.</summary>
        internal const string HINWEIS_OHNE_GANG = "TYPTAGE_OHNE_TAGESGANG";

        /// <summary>Über dieser relativen Abweichung nennt ein Hinweis die Skalierung [-].</summary>
        internal const double SKALIERUNG_SCHWELLE = 1e-9;

        // =================================================================================
        //  Die Begriffe (sprachfrei, Muster in beiden Sprachen)
        // =================================================================================

        /// <summary>Die Jahreszeit als Begriff (<c>BEGRIFF_TYPTAG_JAHRESZEIT_…</c>).</summary>
        internal static ZapfSatz Jahreszeitbegriff(Typtagjahreszeit j)
            => ZapfSatz.Neu("BEGRIFF_TYPTAG_JAHRESZEIT_" + ((int)j).ToString(CultureInfo.InvariantCulture));

        /// <summary>Die Tagart als Begriff (<c>BEGRIFF_TYPTAG_TAGART_…</c>).</summary>
        internal static ZapfSatz Tagartbegriff(Typtagart a)
            => ZapfSatz.Neu("BEGRIFF_TYPTAG_TAGART_" + ((int)a).ToString(CultureInfo.InvariantCulture));

        /// <summary>Die Bewölkung als Begriff (<c>BEGRIFF_TYPTAG_BEWOELKUNG_…</c>).</summary>
        internal static ZapfSatz Bewoelkungsbegriff(Typtagbewoelkung b)
            => ZapfSatz.Neu("BEGRIFF_TYPTAG_BEWOELKUNG_" + ((int)b).ToString(CultureInfo.InvariantCulture));

        // =================================================================================
        //  Die Zuordnung
        // =================================================================================

        /// <summary>
        /// <b>Ordnet jedem der 365 Kalendertage einen Typtag zu.</b> Geprüft wird vorher, ob der
        /// Satz trägt, ob er Zone und Gebäudeart führt, ob er zu ihnen jede Kategorie vollständig
        /// führt und ob das Wetter reicht; jede Lücke ist eine benannte Ablehnung
        /// (<see cref="ZapfEingabefehler.TyptageUngueltig"/>). <paramref name="hinweise"/> nimmt
        /// die Kontrolle gegen die eingespielte Tabelle auf.
        /// </summary>
        internal static Typtagjahr Zuordnen(Typtaganbindung a, int wochentagJan1, bool[] we, string zone,
                                            IReadOnlyList<Ferienfenster> ferien = null,
                                            ICollection<ZapfHinweis> hinweise = null)
        {
            if (a == null || a.Daten == null || !a.Daten.Traegt)
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_NICHT_VERFUEGBAR"));
            Normformvektorsatz satz = a.Daten;
            Zapfkalender.Pruefen(wochentagJan1, we);
            if (!satz.Klimazonen.Contains(a.Klimazone))
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_ZONE_FEHLT", a.Klimazone));
            string art = a.Gebaeudeart ?? "";
            if (!satz.Gebaeudearten.Contains(art))
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_GEBAEUDEART_FEHLT", art));
            if (!satz.Vollstaendig(a.Klimazone, art, out IReadOnlyList<string> fehlend))
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_UNVOLLSTAENDIG", a.Klimazone, art, fehlend.ToArray()));
            if (a.TagesmittelC == null || a.TagesmittelC.Length != Zapfkalender.TAGE)
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_TEMPERATUR", Zapfkalender.TAGE));

            double heizgrenze = satz.Kennwert(Typtagkennwert.Heizgrenze(art))
                                ?? throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_KENNWERT_FEHLT", Typtagkennwert.Heizgrenze(art)));
            double wintergrenze = satz.Kennwert(Typtagkennwert.WINTERGRENZE)
                                  ?? throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_KENNWERT_FEHLT", Typtagkennwert.WINTERGRENZE));
            bool nachBewoelkung = satz.Kategorien.Any(k => k.Bewoelkung != Typtagbewoelkung.Ohne);
            double schwelle = 0.0;
            if (nachBewoelkung)
            {
                schwelle = satz.Kennwert(Typtagkennwert.BEWOELKUNG_SCHWELLE)
                           ?? throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_KENNWERT_FEHLT", Typtagkennwert.BEWOELKUNG_SCHWELLE));
                if (a.BedeckungAchtel == null || a.BedeckungAchtel.Length != Zapfkalender.TAGE)
                    throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_BEDECKUNG_FEHLT", Zapfkalender.TAGE));
            }

            var tage = new List<Typtagtag>(Zapfkalender.TAGE);
            var anzahl = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Typtagkategorie k in satz.Kategorien) anzahl[k.Code] = 0;

            for (int d = 1; d <= Zapfkalender.TAGE; d++)
            {
                double t = a.TagesmittelC[d - 1];
                if (double.IsNaN(t) || double.IsInfinity(t))
                    throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_TEMPERATUR_TAG", d));
                Typtagjahreszeit js = t > heizgrenze ? Typtagjahreszeit.Sommer
                                    : t < wintergrenze ? Typtagjahreszeit.Winter
                                    : Typtagjahreszeit.Uebergang;

                int wochentag = Zapfkalender.Wochentag(wochentagJan1, d);
                Typtagart ta = we[d - 1] && wochentag != Zapfkalender.SAMSTAG ? Typtagart.Sonntag : Typtagart.Werktag;

                Typtagbewoelkung bw = Typtagbewoelkung.Ohne;
                if (nachBewoelkung)
                {
                    double n = a.BedeckungAchtel[d - 1];
                    if (double.IsNaN(n) || double.IsInfinity(n))
                        throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_BEDECKUNG_TAG", d));
                    bw = n >= schwelle ? Typtagbewoelkung.Bewoelkt : Typtagbewoelkung.Heiter;
                }

                Typtagkategorie k = satz.Kategorie(js, ta, bw)
                                    ?? throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_KATEGORIE_FEHLT",
                                           Jahreszeitbegriff(js), Tagartbegriff(ta), Bewoelkungsbegriff(bw)));
                double f = satz.Faktor(a.Klimazone, art, k.Code)
                           ?? throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_FAKTOR_FEHLT", k.Code, a.Klimazone, art));
                tage.Add(new Typtagtag(d, k.Code, k.Jahreszeit, k.Tagart, k.Bewoelkung, f));
                anzahl[k.Code] = anzahl[k.Code] + 1;
            }

            var jahr = new Typtagjahr
            {
                Tage = tage,
                AnzahlJeTyptag = anzahl,
                Klimazone = a.Klimazone,
                Gebaeudeart = art
            };
            AnzahlPruefen(satz, jahr, zone, hinweise);
            if (ferien != null && ferien.Count > 0 && hinweise != null)
                ZapfHinweis.Einmal(hinweise, new ZapfHinweis(zone, HINWEIS_FERIEN, ZapfSatz.Neu("HINWEIS_TYPTAGE_FERIEN", zone)));
            return jahr;
        }

        /// <summary>
        /// Die Kontrolle gegen die eingespielte Tabelle: Weicht die gerechnete Zahl der
        /// Kalendertage einer Kategorie von der eingespielten ab, nennt es ein Hinweis mit der
        /// Kategorie der größten Abweichung und der Zahl der abweichenden Kategorien. Das Wetter
        /// des Projekts und die Tabelle der Richtlinie stammen aus verschiedenen Jahren — eine
        /// Abweichung ist erwartbar und entscheidet die Rechnung nicht.
        /// </summary>
        private static void AnzahlPruefen(Normformvektorsatz satz, Typtagjahr jahr, string zone,
                                          ICollection<ZapfHinweis> hinweise)
        {
            if (hinweise == null) return;
            int abweichend = 0;
            string schlimmste = "";
            int gerechnet = 0, eingespielt = 0, groesste = 0;
            foreach (Typtagkategorie k in satz.Kategorien)
            {
                int soll = satz.Anzahl(jahr.Klimazone, jahr.Gebaeudeart, k.Code) ?? 0;
                int ist = jahr.AnzahlJeTyptag.TryGetValue(k.Code, out int n) ? n : 0;
                int d = Math.Abs(ist - soll);
                if (d == 0) continue;
                abweichend++;
                if (d <= groesste) continue;
                groesste = d;
                schlimmste = k.Code;
                gerechnet = ist;
                eingespielt = soll;
            }
            if (abweichend == 0) return;
            ZapfHinweis.Einmal(hinweise, new ZapfHinweis(zone, HINWEIS_ANZAHL,
                ZapfSatz.Neu("HINWEIS_TYPTAGE_ANZAHL", zone, abweichend, schlimmste, gerechnet, eingespielt)));
        }

        // =================================================================================
        //  Die Tagesmengen und die Stundenreihe
        // =================================================================================

        /// <summary>
        /// Die Einheiten n_E der Gleichung: Personen bzw. Wohneinheiten der Zone. Eine andere
        /// Bezugsart trägt der Typtagweg nicht — die Referenzlastprofile gelten allein für
        /// Wohngebäude (Grundlagen 5, Abschnitt 2.7) —, sie wird benannt abgelehnt.
        /// </summary>
        internal static double Einheiten(Nutzungsart n, Mengenergebnis menge, string zone)
        {
            if (n == null || menge == null)
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_NICHT_VERFUEGBAR"));
            if (n.Bezug != ZapfBezugsart.Personen && n.Bezug != ZapfBezugsart.Wohneinheiten)
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_BEZUGSART", ZapfprofilAuslegung.Bezugsartbegriff(n.Bezug)));
            double e = menge.Bezugsmenge;
            if (double.IsNaN(e) || double.IsInfinity(e) || e <= 0)
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_EINHEITEN", e));
            return e;
        }

        /// <summary>
        /// <b>Die 365 Tagesmengen</b> [kWh] nach der Methodik der Richtlinie:
        /// <c>Q_TT = Q_a · (1/365 + n_E · F_TT)</c>, danach so skaliert, dass
        /// <c>Σ_d Q_d = Q_a</c> gilt.
        ///
        /// <para><b>Nicht die Tagesmenge wird geklemmt, sondern der Faktor genullt</b>
        /// (Grundlagen 5, Abschnitt 2.5, Anmerkung zu Gl. (1)–(3)): Ergäbe die Gleichung für eine
        /// Typtagkategorie einen negativen Tagesbedarf, ist <c>F_TT = 0</c> zu setzen — jeder Tag
        /// dieses Typtags trägt dann den Mittelwertanteil <c>Q_a/365</c>. Die Entscheidung fällt
        /// je Typtag, nicht je Tag: Der Faktor ist innerhalb eines Typtags derselbe. Ein Hinweis
        /// nennt die Nullung, ein zweiter die Skalierung; verteilt das Jahr bei positiver
        /// Jahresmenge nichts, wird benannt abgelehnt.</para>
        /// </summary>
        internal static double[] Tagesmengen(double jahresKwh, double einheiten, Typtagjahr jahr, string zone,
                                             ICollection<ZapfHinweis> hinweise = null)
        {
            if (jahr == null || jahr.Tage.Count != Zapfkalender.TAGE)
                throw Ablehnen(zone, ZapfSatz.Neu("EINGABE_TYPTAGE_NICHT_VERFUEGBAR"));

            // Zuerst die Typtage sammeln, deren Faktor auf 0 zu setzen ist (§2.5): Der Faktor je
            // Typtag entscheidet, nicht der einzelne Tag - so trifft die Nullung jeden Tag dieses
            // Typtags gleich, und die Tagesform der Jahresreihe bleibt in sich stimmig.
            var genullt = new HashSet<string>(StringComparer.Ordinal);
            int tageMitNullung = 0;
            foreach (Typtagtag t in jahr.Tage)
                if (1.0 / Zapfkalender.TAGE + einheiten * t.Faktor < 0.0)
                {
                    genullt.Add(t.Typtag);
                    tageMitNullung++;
                }
            if (genullt.Count > 0 && hinweise != null)
                ZapfHinweis.Einmal(hinweise, new ZapfHinweis(zone, HINWEIS_FAKTOR_NULL,
                    ZapfSatz.Neu("HINWEIS_TYPTAGE_FAKTOR_NULL", zone, genullt.Count, tageMitNullung)) { Warnung = true });

            var roh = new double[Zapfkalender.TAGE];
            double summe = 0.0;
            for (int i = 0; i < roh.Length; i++)
            {
                double f = genullt.Contains(jahr.Tage[i].Typtag) ? 0.0 : jahr.Tage[i].Faktor;
                double q = jahresKwh * (1.0 / Zapfkalender.TAGE + einheiten * f);
                roh[i] = q;
                summe += q;
            }

            if (jahresKwh == 0.0) return roh;
            if (summe <= 0.0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.KeineVerteilung, zone ?? "",
                    ZapfSatz.Neu("EINGABE_TYPTAGE_KEINE_VERTEILUNG", zone ?? ""));

            double skal = jahresKwh / summe;
            for (int i = 0; i < roh.Length; i++) roh[i] *= skal;
            if (Math.Abs(skal - 1.0) > SKALIERUNG_SCHWELLE && hinweise != null)
                ZapfHinweis.Einmal(hinweise, new ZapfHinweis(zone, HINWEIS_SKALIERUNG,
                    ZapfSatz.Neu("HINWEIS_TYPTAGE_SKALIERUNG", zone, skal)));
            return roh;
        }

        /// <summary>
        /// <b>Die Stundenreihe aus den Tagesgängen des Pakets</b> (8760 Werte): Jeder Tag trägt
        /// den Tagesgang seines Typtags, auf Stunden zusammengefasst. <c>null</c>, wenn das Paket
        /// zu dieser Gebäudeart nicht jeden benutzten Typtag führt — dann trägt der Tagesgangsatz
        /// der Zone die Tagesform, und ein Hinweis nennt das.
        /// </summary>
        internal static double[] Stundenreihe(double[] tagesmengen, Typtagjahr jahr, Normformvektorsatz satz,
                                              string gebaeudeart, string zone = "",
                                              ICollection<ZapfHinweis> hinweise = null)
        {
            if (tagesmengen == null || jahr == null || satz == null || tagesmengen.Length != Zapfkalender.TAGE) return null;
            var gaenge = new Dictionary<string, double[]>(StringComparer.Ordinal);
            foreach (Typtagtag t in jahr.Tage)
            {
                if (gaenge.ContainsKey(t.Typtag)) continue;
                Typtaggang g = satz.Gang(gebaeudeart, t.Typtag);
                if (g == null)
                {
                    if (hinweise != null)
                        ZapfHinweis.Einmal(hinweise, new ZapfHinweis(zone, HINWEIS_OHNE_GANG,
                            ZapfSatz.Neu("HINWEIS_TYPTAGE_OHNE_TAGESGANG", gebaeudeart ?? "")));
                    return null;
                }
                gaenge[t.Typtag] = g.Stundenanteile();
            }

            var reihe = new double[Zapfkalender.STUNDEN_JAHR];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                double[] anteile = gaenge[jahr.Tage[d].Typtag];
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    reihe[d * Zapfkalender.STUNDEN_TAG + h] = tagesmengen[d] * anteile[h];
            }
            return reihe;
        }

        /// <summary>
        /// <b>Je Kalendertag die Tageszeitdichte seines Typtags</b> (365 Werte, Stufe Z4b): die
        /// Tagesform des Pakets für die gezogene Jahresreihe (4.4). <c>null</c>, wenn das Paket zu
        /// dieser Gebäudeart nicht jeden benutzten Typtag führt oder ein Tagesgang nichts trägt —
        /// dann zieht das Ensemble über den Tagesgangsatz der Zone, genau wie die deterministische
        /// Reihe (<see cref="Stundenreihe"/>, die den Hinweis dazu setzt).
        /// </summary>
        internal static Tageszeitdichte[] Dichten(Typtagjahr jahr, Normformvektorsatz satz, string gebaeudeart)
        {
            if (jahr == null || satz == null || jahr.Tage.Count != Zapfkalender.TAGE) return null;
            var dichten = new Dictionary<string, Tageszeitdichte>(StringComparer.Ordinal);
            foreach (Typtagtag t in jahr.Tage)
            {
                if (dichten.ContainsKey(t.Typtag)) continue;
                Typtaggang g = satz.Gang(gebaeudeart, t.Typtag);
                if (g == null) return null;
                Tageszeitdichte d = Tageszeitdichte.Aus(g.Stundenanteile());
                if (d.Leer) return null;
                dichten[t.Typtag] = d;
            }
            var jeTag = new Tageszeitdichte[Zapfkalender.TAGE];
            for (int i = 0; i < jeTag.Length; i++) jeTag[i] = dichten[jahr.Tage[i].Typtag];
            return jeTag;
        }

        private static ZapfprofilEingabeException Ablehnen(string zone, ZapfSatz satz)
            => new ZapfprofilEingabeException(ZapfEingabefehler.TyptageUngueltig, zone ?? "", satz);
    }
}
