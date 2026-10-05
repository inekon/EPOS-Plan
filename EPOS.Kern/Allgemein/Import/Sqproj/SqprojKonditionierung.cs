using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Woher der Wert einer Größe einer Zone kommt (Rangfolge 16.3: Ganglinie vor Nutzungsprofil vor Vorlage).</summary>
    internal enum Konditionierungsherkunft
    {
        /// <summary>Die Projektdatei liefert nichts — es gilt die Vorlage der Nutzung (bzw. die Programmvorgabe).</summary>
        Vorlage,
        /// <summary>Aus dem DIN-V-18599-Nutzungsprofil (<c>PdProfileUsage</c>) als Zellen der Vorgabe-Matrix.</summary>
        Nutzungsprofil,
        /// <summary>Aus der Tagesganglinie (<c>PdProfileTimeCurve</c>) als Kalender mit Standardwoche und Perioden.</summary>
        Ganglinie,
        /// <summary>Aus einer IFC-Datei von EPOS-Plan (<c>EPOS_Zone</c>, <c>EPOS_Kalender_*</c>, Datenaustauschkonzept 6.3).</summary>
        IfcDatei,
    }

    /// <summary>Der Beleg einer Zelle: Tabelle und Spalte der Projektdatei, Profilnummer (Nutzungsprofil) bzw. Profilname.</summary>
    internal sealed record Zellbeleg(string Tabelle, string Spalte, int? Profilnummer, string Profil, int? TagesartCode = null, int? Tage = null)
    {
        /// <summary>Der Belegtext in der Sprache der Oberfläche (<c>GIMP_BELEG_SQPROJ</c>).</summary>
        public override string ToString() => SqprojProtokoll.Beleg(this);
    }

    /// <summary>Eine Zelle der Vorgabe-Matrix aus dem Nutzungsprofil, mit Beleg.</summary>
    internal sealed record Vorgabebeleg(string Zeile, Matrixzelle Zelle, Zellbeleg Beleg);

    /// <summary>
    /// <b>Die Konditionierung einer Größe einer Zone</b> aus der Projektdatei: Herkunft, der Kalender aus der Ganglinie
    /// (<c>null</c> = keiner), die Vorgabezellen aus dem Nutzungsprofil und die Zahl der benannt begrenzten Werte.
    /// </summary>
    internal sealed class Groessenkonditionierung
    {
        internal Konditionierungsgroesse Groesse { get; init; }
        internal Konditionierungsherkunft Herkunft { get; init; }
        internal Konditionierungskalender Kalender { get; init; }
        internal Zellbeleg KalenderBeleg { get; init; }
        internal IReadOnlyList<Vorgabebeleg> Vorgaben { get; init; } = Array.Empty<Vorgabebeleg>();
        internal int Begrenzt { get; init; }

        /// <summary>Eine fertige Bemerkung (Herkunft „aus IFC-Datei (EPOS)“); <c>null</c> = der Beleg der Ganglinie.</summary>
        internal string BemerkungText { get; init; }

        /// <summary>Die Bemerkung des Kalenders (≤ 200 Zeichen) — der Beleg der Ganglinie; <c>null</c> ohne Kalender.</summary>
        internal string Bemerkung => Kalender == null ? null : BemerkungText ?? SqprojProtokoll.Beleg(KalenderBeleg);

        /// <summary>Der Wert einer Vorgabezeile (<c>null</c> = keine).</summary>
        internal Matrixzelle Vorgabe(string zeile) => Vorgaben.FirstOrDefault(v => v.Zeile == zeile)?.Zelle;
    }

    /// <summary>
    /// <b>Die Konditionierung einer Zone aus der Projektdatei</b> — formatfrei: je Größe Kalender, Perioden und Vorgabezeilen
    /// mit Herkunft und Beleg. Das Schreiben übernimmt <see cref="ZonenplanCtrl.ProjektdateiUebernehmen"/> über den Weg der
    /// Konditionierung; was hier <see cref="Konditionierungsherkunft.Vorlage"/> trägt, bleibt bei der Vorlage der Nutzung.
    /// </summary>
    internal sealed class Zonenkonditionierung
    {
        internal string Zone { get; init; } = "";
        internal string Nutzung { get; init; }
        internal int? Profilnummer { get; init; }
        internal IReadOnlyList<Groessenkonditionierung> Groessen { get; init; } = Array.Empty<Groessenkonditionierung>();
        internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Die Konditionierung einer Größe.</summary>
        internal Groessenkonditionierung Groesse(Konditionierungsgroesse g) => Groessen.First(x => x.Groesse == g);

        /// <summary>Liefert die Projektdatei für mindestens eine Größe etwas?</summary>
        internal bool Liefert => Groessen.Any(g => g.Herkunft != Konditionierungsherkunft.Vorlage);

        private const int TAG_VON = 8;
        private const int TAG_BIS = 18;

        /// <summary>
        /// Der Heizsollwert Tag — für die Zonenzeile: bei einer Ganglinie der häufigste Wert Montag 8 bis 18 Uhr (bei
        /// Gleichstand der höhere), sonst die Zelle TAG der Matrix.
        /// </summary>
        internal double? HeizsollTag
        {
            get
            {
                Groessenkonditionierung h = Groesse(Konditionierungsgroesse.Heizsoll);
                if (h.Kalender?.Standardwoche is IReadOnlyList<double> w)
                {
                    List<double> tag = w.Skip(TAG_VON).Take(TAG_BIS - TAG_VON).Where(double.IsFinite).ToList();
                    if (tag.Count == 0) return null;
                    return tag.GroupBy(x => x).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).First().Key;
                }
                Matrixzelle z = h.Vorgabe(DbWerte.KOND_ZEILE_TAG);
                return z != null && z.Belegt && !z.Aus ? z.Wert : null;
            }
        }
    }

    /// <summary>
    /// <b>Die Konditionierung je Zone aus Ganglinie und Nutzungsprofil</b> (Datenaustauschkonzept 16.3): rein, ohne
    /// Datenbank, deterministisch.
    /// <list type="bullet">
    /// <item><b>Ganglinie</b> → Kalender: die 24 Stunden zur Standardwoche (168 Zellen) nach der Tagesart der Gruppe — alle
    /// Tage, Montag–Samstag oder Montag–Freitag (angenommen, <see cref="SqprojTagesart"/>; an den freien Tagen beim Heiz- und
    /// Kühlsollwert der Nachtwert, d. h. der niedrigste bzw. höchste Wert der Stunden außerhalb der Nutzungszeit — Betriebsart
    /// 2 —, sonst der ganzen Kurve; bei Lüftung, Geräten und Personen „aus“); eine Stunde ohne Wert ist „aus“. Nennwert bei
    /// Personen <c>RatedPersonOccupancyRate</c> × <c>SpecificRatedDryHeatEmission</c> (ohne Wärmeabgabe 70 W je Person),
    /// bei Geräten die spezifische Leistung × Nutzfläche, sonst der Nennwert aus dem Nutzungsprofil.</item>
    /// <item><b>Abschnitte</b> → Perioden der Art ZEITRAUM (Rang 100 + k): eine eigene Woche bei Wochentagsschaltern (die
    /// nicht gewählten Tage „aus“); der Ganzjahresabschnitt ohne Schalter ergibt keine Periode, sondern die Standardwoche
    /// als Grundangabe. Fehlt er, ist die Grundangabe „aus“ und die Abschnitte tragen die Woche.</item>
    /// <item><b>Nutzungsprofil</b> → Vorgabezellen: Heizsollwert TAG aus <c>NominalRoomTemperature</c>, NACHT als
    /// Absenkung um <c>DropOfTemperatureSetback</c> mit <c>Von</c>/<c>Bis</c> aus der Betriebs- bzw. Heizzeit; Lüftung TAG
    /// aus <c>SupplyAirChange</c> bzw. der Außenluft je Fläche und Person über das Volumen; Personen NENNWERT aus
    /// <c>SpecificThermalOutputPowerOfPersons</c> × Fläche (sonst <c>UserCount</c> × 70 W), Geräte NENNWERT aus
    /// <c>SpecificThermalOutputOfDevices</c> × Fläche, je TAG als Anteil der Vollnutzungsstunden an der Betriebszeit
    /// (sonst 100 %).</item>
    /// <item><b>Grenzen</b> der Größe (Konditionierungskonzept 3.1): Werte außerhalb werden begrenzt und je Zone und Größe
    /// benannt (<c>IMP_SQ_PROT_WERT_BEGRENZT</c>).</item>
    /// </list>
    /// </summary>
    internal static class SqprojKonditionierung
    {
        /// <summary>Der Rang der ersten Periode aus einem Abschnitt.</summary>
        internal const int RANG_ABSCHNITT = 100;

        /// <summary>Die Wärmeabgabe je Person ohne Angabe [W] — wie im Haus (<see cref="Matrixeingang.PERSON_W"/>).</summary>
        internal const double PERSON_W = Matrixeingang.PERSON_W;

        private const string TAB_KURVE = "PdProfileTimeCurve";
        private const string TAB_NUTZUNG = "PdProfileUsage";

        /// <summary><b>Bildet die Konditionierung einer Zone.</b> Alle Eingänge dürfen fehlen; dann gilt die Vorlage.</summary>
        internal static Zonenkonditionierung Bilden(string zone, string nutzung, SqprojNutzungsprofil profil, SqprojProfilgruppe gruppe,
                                                    double? flaecheM2, double? volumenM3)
        {
            var groessen = new List<Groessenkonditionierung>();
            var meldungen = new List<PruefMeldung>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                int begrenzt = 0;
                List<Vorgabebeleg> vorgaben = Vorgaben(profil, g, flaecheM2, volumenM3, ref begrenzt);
                SqprojZeitprofil zp = gruppe?.Profil(SqprojProfilklasse.Klasse(g));
                Konditionierungskalender kalender = null;
                Zellbeleg beleg = null;
                if (zp != null && zp.HatKurve)
                {
                    double[] woche = Woche(zp, g, gruppe.Tagesart, ref begrenzt);
                    double? nennwert = Nennwert(zp, g, flaecheM2) ?? vorgaben.FirstOrDefault(v => v.Zeile == DbWerte.KOND_ZEILE_NENNWERT)?.Zelle.Wert;
                    kalender = Kalender(zp, g, woche, Konditionierungsgroessen.HatNennwert(g) ? nennwert : null);
                    beleg = new Zellbeleg(TAB_KURVE, SqprojProfilklasse.Kurvenspalte(zp.Klasse), profil?.Profilnummer ?? gruppe.Profilnummer, zp.Name,
                                          gruppe.TagesartCode, SqprojProtokoll.Wochentage(gruppe.Tagesart));
                }
                Konditionierungsherkunft herkunft = kalender != null ? Konditionierungsherkunft.Ganglinie
                    : vorgaben.Count > 0 ? Konditionierungsherkunft.Nutzungsprofil : Konditionierungsherkunft.Vorlage;
                if (begrenzt > 0)
                    meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.WERT_BEGRENZT, zone ?? "",
                        Konditionierungsgroessen.Kennwort(g), SqprojProtokoll.Z(begrenzt), Konditionierungsgroessen.Bereichstext(g)));
                groessen.Add(new Groessenkonditionierung
                {
                    Groesse = g, Herkunft = herkunft, Kalender = kalender, KalenderBeleg = beleg, Vorgaben = vorgaben, Begrenzt = begrenzt,
                });
            }
            var k = new Zonenkonditionierung { Zone = zone ?? "", Nutzung = nutzung, Profilnummer = profil?.Profilnummer, Groessen = groessen };
            k.Meldungen.AddRange(meldungen);
            return k;
        }

        /// <summary>
        /// <b>Die Standardwoche (168 Zellen, Montag 0 Uhr zuerst)</b> aus den 24 Stunden eines Zeitprofils nach der Tagesart;
        /// NaN = „aus“. Werte außerhalb der Grenzen der Größe werden begrenzt und gezählt.
        /// </summary>
        internal static double[] Woche(SqprojZeitprofil zp, Konditionierungsgroesse g, SqprojTagesart tagesart, ref int begrenzt)
        {
            var tag = new double[Kalenderwoche.TAGESSTUNDEN];
            for (int h = 0; h < tag.Length; h++)
                tag[h] = zp.Stunden[h] is double v ? Math.Round(Begrenzen(g, v, ref begrenzt), Kalenderwoche.NACHKOMMASTELLEN) : double.NaN;
            double nacht = double.NaN;
            // Die Stunden außerhalb der Nutzungszeit (OperatingModeType 2) belegen die Nachtstunden; gerechnet wird mit den
            // Werten der Kurve. Ohne solche Stunden gilt die ganze Kurve.
            List<double> endlich = Enumerable.Range(0, tag.Length).Where(h => zp.Betriebsarten[h] == 2 && double.IsFinite(tag[h])).Select(h => tag[h]).ToList();
            if (endlich.Count == 0) endlich = tag.Where(double.IsFinite).ToList();
            if (endlich.Count > 0 && g == Konditionierungsgroesse.Heizsoll) nacht = endlich.Min();
            if (endlich.Count > 0 && g == Konditionierungsgroesse.Kuehlsoll) nacht = endlich.Max();
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int d = 0; d < 7; d++)
                for (int h = 0; h < Kalenderwoche.TAGESSTUNDEN; h++)
                    woche[Kalenderwoche.Stelle(d, h)] = Frei(tagesart, d) ? nacht : tag[h];
            return woche;
        }

        /// <summary>Ist der Wochentag (0 = Montag) nach der Tagesart frei — Sonntag bei sechs, Samstag und Sonntag bei fünf Tagen?</summary>
        internal static bool Frei(SqprojTagesart tagesart, int tag)
            => tagesart == SqprojTagesart.Werktage ? tag >= 5 : tagesart == SqprojTagesart.WerktageSamstag && tag == 6;

        /// <summary>Der Kalender aus Woche und Abschnitten (Regeln: Klassenkopf).</summary>
        internal static Konditionierungskalender Kalender(SqprojZeitprofil zp, Konditionierungsgroesse g, double[] woche, double? nennwert)
        {
            List<SqprojAbschnitt> abschnitte = zp.Abschnitte;
            bool ganzjahr = abschnitte.Count == 0 || abschnitte.Any(a => a.Ganzjahr && !a.MitWochentagen);
            Kalenderangabe grund = ganzjahr ? Kalenderangabe.AusWoche(woche) : Kalenderangabe.Abgeschaltet;
            var perioden = new List<Kalenderregel>();
            int k = 0;
            foreach (SqprojAbschnitt a in abschnitte)
            {
                if (a.Ganzjahr && !a.MitWochentagen) continue;
                if (perioden.Count >= Kalenderregel.PERIODEN_MAX) break;
                double[] eigene = (double[])woche.Clone();
                if (a.MitWochentagen)
                    for (int d = 0; d < 7; d++)
                        if (!a.Wochentage[d])
                            for (int h = 0; h < Kalenderwoche.TAGESSTUNDEN; h++) eigene[Kalenderwoche.Stelle(d, h)] = double.NaN;
                k++;
                string name = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Kuerzen(zp.Name, KonditionierungSchema.BEZEICHNER_MAX_ZEICHEN - 4), k);
                perioden.Add(Kalenderregel.Zeitraum(RANG_ABSCHNITT + k, DbWerte.KOND_ART_ZEITRAUM, name, a.Beginn, a.Ende,
                                                    Kalenderangabe.AusWoche(eigene)));
            }
            return new Konditionierungskalender(g, grund, nennwert, perioden);
        }

        private static double? Nennwert(SqprojZeitprofil zp, Konditionierungsgroesse g, double? flaecheM2)
        {
            if (g == Konditionierungsgroesse.Personen && zp.Personen is double n) return n * (zp.WattJePerson ?? PERSON_W);
            if (g == Konditionierungsgroesse.Geraete && zp.GeraeteWm2 is double q && flaecheM2 > 0.0) return q * flaecheM2.Value;
            return null;
        }

        /// <summary>Die Vorgabezellen einer Größe aus dem Nutzungsprofil (Regeln: Klassenkopf); leer ohne Profil.</summary>
        internal static List<Vorgabebeleg> Vorgaben(SqprojNutzungsprofil p, Konditionierungsgroesse g, double? flaecheM2, double? volumenM3, ref int begrenzt)
        {
            var l = new List<Vorgabebeleg>();
            if (p == null) return l;
            Zellbeleg B(string spalte) => new Zellbeleg(TAB_NUTZUNG, spalte, p.Profilnummer, p.Name);
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll:
                    if (p.Raumtemperatur is double t)
                    {
                        double tag = Begrenzen(g, t, ref begrenzt);
                        l.Add(new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(tag), B("NominalRoomTemperature")));
                        if (p.Absenkung is double ab)
                        {
                            int? von = p.BetriebBis ?? p.HeizBis, bis = p.BetriebVon ?? p.HeizVon;
                            if (!(von.HasValue && bis.HasValue && von != bis)) von = bis = null;
                            l.Add(new Vorgabebeleg(DbWerte.KOND_ZEILE_NACHT, Matrixzelle.AusWert(Begrenzen(g, t - ab, ref begrenzt), von, bis),
                                                   B("DropOfTemperatureSetback")));
                        }
                    }
                    break;
                case Konditionierungsgroesse.Lueftung:
                    double? n = p.Zuluftwechsel;
                    string spalte = "SupplyAirChange";
                    if (!n.HasValue && volumenM3 > 0.0)
                    {
                        double q = (p.AussenluftJeFlaeche ?? 0.0) * (flaecheM2 ?? 0.0) + (p.AussenluftJePerson ?? 0.0) * (p.Personenzahl ?? 0.0);
                        if (q > 0.0)
                        {
                            n = q / volumenM3.Value;
                            spalte = p.AussenluftJeFlaeche.HasValue && flaecheM2 > 0.0 ? "MinimumExternalAirFlowBasedOnArea" : "MinimumExternalAirFlowBasedOnPersons";
                        }
                    }
                    if (n is double nw) l.Add(new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(Begrenzen(g, nw, ref begrenzt)), B(spalte)));
                    break;
                case Konditionierungsgroesse.Personen:
                    double? pw = p.PersonenWm2 is double s && flaecheM2 > 0.0 ? s * flaecheM2.Value
                        : p.Personenzahl is double z ? z * PERSON_W : (double?)null;
                    if (pw is double w)
                    {
                        l.Add(new Vorgabebeleg(DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(Math.Round(w, 1)),
                                               B(p.PersonenWm2.HasValue && flaecheM2 > 0.0 ? "SpecificThermalOutputPowerOfPersons" : "UserCount")));
                        l.Add(Anteil(g, p.VollnutzungPersonenH, p, "DailyEffectiveLoadHoursOfPersons", B, ref begrenzt));
                    }
                    break;
                case Konditionierungsgroesse.Geraete:
                    if (p.GeraeteWm2 is double gq && flaecheM2 > 0.0)
                    {
                        l.Add(new Vorgabebeleg(DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(Math.Round(gq * flaecheM2.Value, 1)),
                                               B("SpecificThermalOutputOfDevices")));
                        l.Add(Anteil(g, p.VollnutzungGeraeteH, p, "DailyEffectiveLoadHoursOfDevices", B, ref begrenzt));
                    }
                    break;
            }
            return l;
        }

        private static Vorgabebeleg Anteil(Konditionierungsgroesse g, double? vollH, SqprojNutzungsprofil p, string spalte,
                                           Func<string, Zellbeleg> b, ref int begrenzt)
        {
            if (vollH is double v && p.Betriebsstunden is int h && h > 0)
                return new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(Math.Round(Begrenzen(g, v / h, ref begrenzt), 4)), b(spalte));
            return new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(Konditionierungsgroessen.ANTEIL_MAX), b(spalte));
        }

        /// <summary>Begrenzt einen Wert auf die Grenzen der Größe und zählt die Begrenzung.</summary>
        internal static double Begrenzen(Konditionierungsgroesse g, double w, ref int begrenzt)
        {
            double min = Konditionierungsgroessen.Min(g), max = Konditionierungsgroessen.Max(g);
            if (w < min) { begrenzt++; return min; }
            if (w > max) { begrenzt++; return max; }
            return w;
        }

        private static string Kuerzen(string s, int max)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) s = "Abschnitt";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
