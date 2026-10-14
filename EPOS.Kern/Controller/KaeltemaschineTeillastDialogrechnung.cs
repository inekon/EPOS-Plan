using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Rechnungen der Gruppe „Teillast und Takten" im Katalogdialog der Kältemaschine</b> (Fachkonzept Teillast und
    /// Takten 7.1, 3.2, 3.5, 3.7; Welle KM3-E3-a): Lesezeile g(0,25)/g(0,5)/g(0,75), Schnellwahl „Kurve aus Typkennfeld",
    /// „Typkennfeld auf Datenblatt skalieren…" und die Auskunft „Teillastpunkte prüfen…". Alles datenbankfrei und ohne
    /// Speicherung — gerechnet wird mit DENSELBEN Bausteinen wie der Lauf (<see cref="Kaeltemaschinenteillast"/>,
    /// <see cref="KaeltemaschinenRand"/>, <see cref="Kaeltemaschine.Rueckkuehltemperatur"/>); gespeichert wird nur über den
    /// Speicherweg des Dialogs (<see cref="KaeltemaschineStammCtrl.Speichern"/>).
    /// </summary>
    public static class KaeltemaschineTeillastDialogrechnung
    {
        /// <summary>Die Lastgrade der Lesezeile.</summary>
        public static readonly IReadOnlyList<double> LESEZEILE_LASTGRADE = new[] { 0.25, 0.5, 0.75 };

        /// <summary>Die Lastgrade der kleinen Kurve: 10 % bis 100 % in Zehnerschritten.</summary>
        public static readonly IReadOnlyList<double> KURVE_LASTGRADE =
            Enumerable.Range(1, 10).Select(i => i / 10.0).ToArray();

        /// <summary>Höchstzahl der Punkte der Auskunft (A bis D, Fachkonzept 3.7).</summary>
        public const int AUSKUNFT_PUNKTE = 4;

        // =====================================================================
        //  Lesezeile
        // =====================================================================

        /// <summary>
        /// Der Lesestand der Gruppe: g bei 25, 50 und 75 % Last, die Herkunft der wirksamen Kurve, ein Hinweis in der
        /// Oberflächensprache und die Punkte der kleinen Kurve (Lastgrad, g).
        /// </summary>
        public sealed record Lesestand(double G25, double G50, double G75, KaeltemaschinenKurvenherkunft Herkunft,
                                       string Hinweis, IReadOnlyList<(double Lastgrad, double G)> Kurve);

        /// <summary>
        /// Die Lesezeile eines Arbeitsstands: g(x) = x / E(x) der WIRKSAMEN Kurve (<see cref="Kaeltemaschinenteillast.AusModell"/>
        /// — wie der Lauf, also mit Vorgabekurve und Rückfall auf linear); der Hinweis nennt den Bestandsweg, die
        /// Vorgabekurve oder eine verworfene Kurve.
        /// </summary>
        public static Lesestand Lesezeile(KaeltemaschineModel m)
        {
            Kaeltemaschinenteillast t = Kaeltemaschinenteillast.AusModell(m);
            string hinweis = t.Herkunft switch
            {
                KaeltemaschinenKurvenherkunft.Bestand => MyResource.Resource.KM_TT_HINWEIS_BESTAND,
                KaeltemaschinenKurvenherkunft.Linear => MyResource.Resource.KM_TT_HINWEIS_LINEAR,
                KaeltemaschinenKurvenherkunft.Vorgabekurve => MyResource.Resource.KM_TT_HINWEIS_VORGABEKURVE,
                KaeltemaschinenKurvenherkunft.Verworfen => MyResource.Resource.KM_TT_HINWEIS_VERWORFEN,
                _ => ""
            };
            var kurve = KURVE_LASTGRADE.Select(x => (x, t.G(x))).ToList();
            return new Lesestand(t.G(0.25), t.G(0.5), t.G(0.75), t.Herkunft, hinweis, kurve);
        }

        // =====================================================================
        //  Schnellwahl „Kurve aus Typkennfeld"
        // =====================================================================

        /// <summary>Die Teillastfelder eines Typkennfelds: Weg, Beiwerte, x_u und Verdichterregelung (Persistenzwerte).</summary>
        public sealed record Typkurve(string Bezeichner, string TeillastWeg, double? A, double? B, double? C,
                                      double? LastgradMin, string Verdichterregelung);

        /// <summary>Die Bezeichner der eingebauten Typkennfelder in der Folge der Ressource; leer, wenn sie nicht lesbar ist.</summary>
        public static IReadOnlyList<string> Typkennfeldnamen()
        {
            try { return KaeltemaschinenTypkennfelder.Lesen().Select(t => t.Bezeichner).ToList(); }
            catch (Exception) { return Array.Empty<string>(); }
        }

        /// <summary>
        /// Die Teillastkurve eines eingebauten Typkennfelds (Fachkonzept 7.1): die Felder, die der Import des Satzes
        /// setzt (<see cref="KaeltemaschineTeillastkurve.Uebernehmen"/>) — C_d und Randweg bleiben beim Anwender.
        /// <c>null</c> bei unbekanntem Bezeichner.
        /// </summary>
        public static Typkurve KurveAusTypkennfeld(string bezeichner)
        {
            KaeltemaschinenTypkennfelder.Typkennfeld t = Finde(bezeichner);
            if (t == null) return null;
            KaeltemaschineModel m = t.Modell();
            return new Typkurve(t.Bezeichner, m.Teillast_Weg, m.Teillastkurve_a, m.Teillastkurve_b, m.Teillastkurve_c,
                                m.Teillastkurve_Lastgrad_Min, m.Verdichterregelung);
        }

        // =====================================================================
        //  Schnellwahl „Typkennfeld auf Datenblatt skalieren…"
        // =====================================================================

        /// <summary>Der Ausgang der Skalierung: der neue Satz (ohne Id, nicht gespeichert) oder der Grund.</summary>
        public sealed record Skalierergebnis(KaeltemaschineModel Satz, string Meldung)
        {
            /// <summary>Ist ein Satz entstanden?</summary>
            public bool Ok => Satz != null;
        }

        /// <summary>
        /// Skaliert ein eingebautes Typkennfeld auf den Nennpunkt eines Datenblatts (Fachkonzept 3.5) — siehe
        /// <see cref="AufDatenblattSkalieren(KaeltemaschineModel, double, double, string)"/>.
        /// </summary>
        public static Skalierergebnis AufDatenblattSkalieren(string typkennfeld, double nennleistungKw, double nennEer)
        {
            KaeltemaschinenTypkennfelder.Typkennfeld t = Finde(typkennfeld);
            if (t == null)
                return new Skalierergebnis(null, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KM_MSG_TYPKENNFELD_UNBEKANNT, typkennfeld ?? ""));
            return AufDatenblattSkalieren(t.Modell(), nennleistungKw, nennEer, t.Bezeichner);
        }

        /// <summary>
        /// <b>Skalierung auf den Nennpunkt</b> (Fachkonzept 3.5): Q und EER des Kennfelds am Eurovent-Nennpunkt
        /// (Kaltwasser 7 °C; Luft: Außenluft 35 °C, im Kennfeld Rückkühlung 40 °C; sonst 30 °C) werden bilinear wie in der
        /// Simulation gelesen; jede Stützstelle bekommt Q_i' = Q_i · Q_DB / Q_KF und EER_i' = EER_i · EER_DB / EER_KF.
        /// Teillastfelder, Mindestteillast und Rückkühlart bleiben; der neue Satz ist ein eigener Satz (Id 0, nicht
        /// ausgeliefert), seine Beschreibung nennt die Herkunft als Text. Nichts wird gespeichert.
        /// </summary>
        public static Skalierergebnis AufDatenblattSkalieren(KaeltemaschineModel quelle, double nennleistungKw, double nennEer,
                                                             string herkunft = null)
        {
            if (quelle == null || !(nennleistungKw > 0) || !(nennEer > 0) || double.IsInfinity(nennleistungKw) ||
                double.IsInfinity(nennEer))
                return new Skalierergebnis(null, MyResource.Resource.KM_MSG_SKALIEREN_UNGUELTIG);
            (double qKf, double eerKf) = Nennpunkt(quelle);
            if (!(qKf > 0) || !(eerKf > 0))
                return new Skalierergebnis(null, MyResource.Resource.KM_MSG_AUSKUNFT_KEIN_KENNFELD);

            double fq = nennleistungKw / qKf, fe = nennEer / eerKf;
            string name = herkunft ?? quelle.Bezeichner ?? "";
            var m = new KaeltemaschineModel
            {
                Id = 0,
                Bezeichner = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_TT_SKALIERT_NAME, name),
                Firma = quelle.Firma,
                Typ = quelle.Typ,
                Beschreibung = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_TT_SKALIERT_BESCHREIBUNG, name,
                    nennleistungKw.ToString("0.#", CultureInfo.CurrentCulture), nennEer.ToString("0.00", CultureInfo.CurrentCulture)),
                Nennkaelteleistung_kW = nennleistungKw,
                Nenn_EER = nennEer,
                Kaeltemittel = quelle.Kaeltemittel,
                Rueckkuehlart = quelle.Rueckkuehlart,
                Mindestteillast_Prozent = quelle.Mindestteillast_Prozent,
                Hilfsstrom_Rueckkuehlung_kW = quelle.Hilfsstrom_Rueckkuehlung_kW,
                Kaltwasser_Vorlauf_Min = quelle.Kaltwasser_Vorlauf_Min,
                Modulkosten = null,
                ReadOnly = false,
                Teillast_Weg = quelle.Teillast_Weg,
                Teillastkurve_a = quelle.Teillastkurve_a,
                Teillastkurve_b = quelle.Teillastkurve_b,
                Teillastkurve_c = quelle.Teillastkurve_c,
                Teillastkurve_Lastgrad_Min = quelle.Teillastkurve_Lastgrad_Min,
                Taktverlustfaktor_Cd = quelle.Taktverlustfaktor_Cd,
                Verdichterregelung = quelle.Verdichterregelung,
                Kennfeld_Randweg = quelle.Kennfeld_Randweg,
                Kennlinie = quelle.Kennlinie.Select(p => new KaeltemaschineKenndatenModel
                {
                    Rueckkuehltemperatur = p.Rueckkuehltemperatur,
                    Kaltwassertemperatur = p.Kaltwassertemperatur,
                    EER = p.EER.HasValue ? Math.Round(p.EER.Value * fe, 3, MidpointRounding.AwayFromZero) : null,
                    Kaelteleistung_kW = p.Kaelteleistung_kW.HasValue
                        ? Math.Round(p.Kaelteleistung_kW.Value * fq, 2, MidpointRounding.AwayFromZero) : null
                }).ToList()
            };
            return new Skalierergebnis(m, "");
        }

        /// <summary>Q [kW] und EER des eigenen Kennfelds am Eurovent-Nennpunkt; (0, 0) ohne Kennfeld.</summary>
        public static (double LeistungKw, double Eer) Nennpunkt(KaeltemaschineModel m)
        {
            KaeltemaschinenKennlinie k = Kennlinie(m);
            if (k == null) return (0.0, 0.0);
            KaeltemaschinenPunkt p = k.Auswerten(KaeltemaschinenKennfeld.Nennrueckkuehltemperatur(m.Rueckkuehlart),
                                                 KaeltemaschinenKennfeld.NENN_KALTWASSER_C);
            return (p.LeistungKw, p.Eer);
        }

        // =====================================================================
        //  Auskunft „Teillastpunkte prüfen…"
        // =====================================================================

        /// <summary>Ein Eingabepaar der Auskunft: Name (A bis D), Außentemperatur [°C] und Lastgrad (Anteil der Nennleistung).</summary>
        public sealed record Auskunftspunkt(string Name, double AussenC, double Lastgrad);

        /// <summary>
        /// Eine Zeile der Auskunft: Rückkühltemperatur nach Rückkühlart, verfügbare Kälteleistung Q_av, gedeckte
        /// Kälteleistung, Lastgrad der Maschine (Leistung durch Q_av), Leistungsaufnahme des Verdichters (ohne Hilfsstrom),
        /// EER, ob die Maschine taktet und ob der Punkt am Kennfeldrand liegt.
        /// </summary>
        public sealed record Auskunftszeile(string Name, double AussenC, double RueckkuehlC, double VerfuegbarKw,
                                            double KaelteKw, double LastgradMaschine, double LeistungsaufnahmeKw, double Eer,
                                            bool Takt, bool Randwert);

        /// <summary>Der Ausgang der Auskunft: die Zeilen oder der Grund.</summary>
        public sealed record Auskunftsergebnis(IReadOnlyList<Auskunftszeile> Zeilen, string Meldung)
        {
            /// <summary>Ist gerechnet worden?</summary>
            public bool Ok => Zeilen != null;
        }

        /// <summary>
        /// <b>Die Ökodesign-Punkte aus dem eigenen Kennfeld</b> (Fachkonzept 3.7): je Paar die Rückkühltemperatur nach
        /// Rückkühlart (<see cref="Kaeltemaschine.Rueckkuehltemperatur"/>, Luft + 5 K, Nasskühler ohne Feuchte), Q_av und
        /// Volllast-EER bei Kaltwasser 7 °C nach dem Randweg des Satzes (<see cref="KaeltemaschinenRand.Auswerten"/>), die
        /// Last = Lastgrad · Nennkälteleistung (ohne Nennwert: Q am Nennpunkt des Kennfelds) und die Stunde der Lastachse
        /// (<see cref="Kaeltemaschinenteillast.Stunde"/>) — Leistungsaufnahme und EER wie im Lauf. Keine Normtafel, keine
        /// voreingetragenen Punkte, keine Speicherung.
        /// </summary>
        public static Auskunftsergebnis TeillastpunkteAuskunft(KaeltemaschineModel m, IReadOnlyList<Auskunftspunkt> punkte)
        {
            KaeltemaschinenKennlinie k = Kennlinie(m);
            if (k == null) return new Auskunftsergebnis(null, MyResource.Resource.KM_MSG_AUSKUNFT_KEIN_KENNFELD);
            if (punkte == null || punkte.Count == 0 || punkte.Count > AUSKUNFT_PUNKTE ||
                punkte.Any(p => p == null || !Endlich(p.AussenC) || !(p.Lastgrad > 0) || p.Lastgrad > 1))
                return new Auskunftsergebnis(null, MyResource.Resource.KM_MSG_AUSKUNFT_EINGABE);

            double nenn = m.Nennkaelteleistung_kW is double q && q > 0 ? q : Nennpunkt(m).LeistungKw;
            if (!(nenn > 0)) return new Auskunftsergebnis(null, MyResource.Resource.KM_MSG_AUSKUNFT_KEIN_KENNFELD);
            double mt = Math.Max(0.0, Math.Min(100.0, m.Mindestteillast_Prozent ?? 0.0)) / 100.0;
            Kaeltemaschinenteillast t = Kaeltemaschinenteillast.AusModell(m);

            var zeilen = new List<Auskunftszeile>(punkte.Count);
            foreach (Auskunftspunkt p in punkte)
            {
                double rk = Kaeltemaschine.Rueckkuehltemperatur(m.Rueckkuehlart, p.AussenC, null, out _);
                KaeltemaschinenRandpunkt rp = KaeltemaschinenRand.Auswerten(k, rk, KaeltemaschinenKennfeld.NENN_KALTWASSER_C,
                                                                           m.Kennfeld_Randweg);
                double last = p.Lastgrad * nenn;
                KaeltemaschinenTeillaststunde s = t.Stunde(last, rp.LeistungKw, rp.Eer, nenn, mt);
                zeilen.Add(new Auskunftszeile(p.Name ?? "", p.AussenC, rk, rp.LeistungKw, s.KaelteKwh, s.Lastgrad,
                                              s.VerdichterKwh, s.Eer, s.Takt, rp.Randwert));
            }
            return new Auskunftsergebnis(zeilen, "");
        }

        // =====================================================================

        private static KaeltemaschinenTypkennfelder.Typkennfeld Finde(string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return null;
            try
            {
                return KaeltemaschinenTypkennfelder.Lesen()
                    .FirstOrDefault(t => string.Equals(t.Bezeichner, bezeichner.Trim(), StringComparison.Ordinal));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static KaeltemaschinenKennlinie Kennlinie(KaeltemaschineModel m)
        {
            if (m?.Kennlinie == null || m.Kennlinie.Count == 0) return null;
            var k = new KaeltemaschinenKennlinie(m.Kennlinie.Select(p => (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER,
                                                                           p.Kaelteleistung_kW)));
            return k.Leer ? null : k;
        }

        private static bool Endlich(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
