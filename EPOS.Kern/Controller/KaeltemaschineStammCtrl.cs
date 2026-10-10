using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Katalog der Kältemaschinen</b> (<c>Tab_Kaeltemaschine_STAMM</c> samt Kennlinie, KU3-1): Lesen,
    /// Prüfen, Speichern, Löschen, Duplizieren und das Auslieferungskennzeichen. Alle Zugriffe über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern; Kopf und Kennlinie in EINEM Vorgang.
    /// </summary>
    public static class KaeltemaschineStammCtrl
    {
        /// <summary>Die Katalogtabelle.</summary>
        public const string TABLE = KaeltemaschineSchema.TAB_STAMM;

        /// <summary>Die Kennlinie des Katalogs.</summary>
        public const string CURVE = KaeltemaschineSchema.TAB_KENNDATEN_STAMM;

        /// <summary>Der Ausgang eines Schreibwegs: bei Erfolg die ID, sonst der Grund in der Oberflächensprache.</summary>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, int Id);

        /// <summary>Eine Zeile der Katalogliste.</summary>
        public sealed record Listenzeile(int Id, string Bezeichner, string Firma, double? Nennkaelteleistung_kW,
                                         double? Nenn_EER, string Rueckkuehlart, bool ReadOnly, string Typ = null,
                                         bool Katalogsatz = false, string Geraeteart = null);

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Alle Katalogsätze, nach Bezeichner sortiert.</summary>
        public static IReadOnlyList<Listenzeile> Liste()
        {
            var liste = new List<Listenzeile>();
            // Schritt 211: die Geraeteart nur, wenn die Spalte steht; sonst (und leer) nach der Rueckfuellregel.
            bool mitArt = DataRepository.SpalteVorhanden(TABLE, KaelteKatalogfelderSchema.SPALTE_GERAETEART);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Firma, Typ, " + KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG + ", " +
                KaeltemaschineSchema.SPALTE_NENN_EER + ", " + KaeltemaschineSchema.SPALTE_RUECKKUEHLART +
                (mitArt ? ", " + KaelteKatalogfelderSchema.SPALTE_GERAETEART : "") +
                ", ReadOnly, " + Katalogfassung.SPALTE_SCHLUESSEL + " FROM " + TABLE + " ORDER BY Bezeichner");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                string rueck = Text(r[KaeltemaschineSchema.SPALTE_RUECKKUEHLART]);
                liste.Add(new Listenzeile(Ganz(r["ID"]), Text(r["Bezeichner"]) ?? "", Text(r["Firma"]),
                                          Zahl(r[KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG]),
                                          Zahl(r[KaeltemaschineSchema.SPALTE_NENN_EER]),
                                          rueck,
                                          Ganz(r["ReadOnly"]) == 1, Text(r["Typ"]),
                                          !string.IsNullOrEmpty(Text(r[Katalogfassung.SPALTE_SCHLUESSEL])),
                                          KaelteKatalogfelderSchema.GeraeteartWirksam(
                                              mitArt ? Text(r[KaelteKatalogfelderSchema.SPALTE_GERAETEART]) : null, rueck)));
            }
            return liste;
        }

        /// <summary>
        /// <b>Die Zeilen der Katalogliste</b> der Verwaltung (Profil <see cref="Katalogfilterprofil.Finde"/> mit
        /// <see cref="Anlagenart.Kaeltemaschine"/>): Bezeichner, Firma, Typ, Nennkälteleistung, Nenn-EER, die
        /// Rückkühlart und die Herkunft (<see cref="HerkunftText"/>) als ANZEIGETEXT — der Schlüssel der Zeile ist die Id, der Persistenzwert bleibt im Kern.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var zeilen = new List<Katalogfilterzeile>();
            foreach (Listenzeile z in Liste())
            {
                var zeile = new Katalogfilterzeile(z.Id, z.Bezeichner)
                {
                    Geschuetzt = z.ReadOnly,
                    Schluessel = z.Id.ToString(CultureInfo.InvariantCulture)
                };
                zeilen.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, z.Bezeichner)
                    .MitText(Katalogfilterprofil.SpHersteller, z.Firma)
                    .MitText(Katalogfilterprofil.SpTyp, z.Typ)
                    .MitZahl(Katalogfilterprofil.SpNennkaelteleistung, z.Nennkaelteleistung_kW, 1)
                    .MitZahl(Katalogfilterprofil.SpEer, z.Nenn_EER, 2)
                    .MitText(Katalogfilterprofil.SpGeraeteart, GeraeteartText(z.Geraeteart))
                    .MitText(Katalogfilterprofil.SpRueckkuehlart, RueckkuehlartText(z.Rueckkuehlart))
                    .MitText(Katalogfilterprofil.SpHerkunft, HerkunftText(z.Typ, z.Katalogsatz)));
            }
            return zeilen;
        }

        /// <summary>
        /// <b>Die Herkunft eines Katalogsatzes</b> als Anzeigetext der Spalte Herkunft: „Typkennfeld“ für jeden Satz
        /// mit <c>Typ = </c><see cref="KaeltemaschinenKennfeld.TYP"/> (die eingebauten Typkennfelder, ihre Kopien und
        /// ein eingelesener Copper-Kurvensatz — der Import setzt denselben Typ), „Auslieferung“ für einen anderen Satz
        /// mit Katalogschlüssel (die gesäten Beispielgeräte; eine Kopie trägt keinen Schlüssel), sonst „eigen“.
        /// <para>Ein eingelesener Satz aus der CSV-Kennfeldvorlage hinterlässt kein Kennzeichen — weder Quelle noch
        /// Schlüssel —, er steht deshalb als „eigen“.</para>
        /// </summary>
        /// <param name="typ">Die Spalte <c>Typ</c> des Satzes.</param>
        /// <param name="katalogsatz">Trägt der Satz einen Katalogschlüssel (<c>Katalog_Schluessel</c>)?</param>
        public static string HerkunftText(string typ, bool katalogsatz)
        {
            if (string.Equals(typ, KaeltemaschinenKennfeld.TYP, StringComparison.Ordinal))
                return MyResource.Resource.KM_HERKUNFT_TYPKENNFELD;
            return katalogsatz ? MyResource.Resource.KM_HERKUNFT_AUSLIEFERUNG : MyResource.Resource.KM_HERKUNFT_EIGEN;
        }

        /// <summary>
        /// Der Trichter, den der Schalter „Typkennfelder ausblenden“ auf die Spalte Herkunft legt: die Verneinung
        /// (<see cref="Katalogfilterprofil.AUSDRUCK_NICHT"/>) des Herkunftstexts „Typkennfeld“ in der Oberflächensprache.
        /// </summary>
        public static string AusdruckOhneTypkennfelder
            => Katalogfilterprofil.AUSDRUCK_NICHT + MyResource.Resource.KM_HERKUNFT_TYPKENNFELD;

        /// <summary>
        /// Der Anzeigetext einer Rückkühlart in der Oberflächensprache; leer für <c>null</c>, der Wert selbst für
        /// einen unbekannten. <b>Nie ein Steuerwert</b> — geschrieben wird allein der Persistenzwert aus
        /// <see cref="KaeltemaschineSchema.RUECKKUEHLARTEN"/>.
        /// </summary>
        public static string RueckkuehlartText(string persistenzwert)
        {
            switch (persistenzwert)
            {
                case null: return "";
                case KaeltemaschineSchema.RUECKKUEHLART_LUFT: return MyResource.Resource.KM_RUECKKUEHLART_LUFT;
                case KaeltemaschineSchema.RUECKKUEHLART_WASSER: return MyResource.Resource.KM_RUECKKUEHLART_WASSER;
                case KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER: return MyResource.Resource.KM_RUECKKUEHLART_TROCKENKUEHLER;
                case KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER: return MyResource.Resource.KM_RUECKKUEHLART_NASSKUEHLER;
                default: return persistenzwert;
            }
        }

        /// <summary>Ein Katalogsatz samt Kennlinie; <c>null</c>, wenn es die ID nicht gibt.</summary>
        public static KaeltemaschineModel Laden(int id) => Lesen(TABLE, CURVE, id, true);

        /// <summary>Gehört der Satz zur Auslieferung (<c>ReadOnly = 1</c>)?</summary>
        public static bool Gesperrt(int id)
        {
            object o = DataRepository.ExecuteScalar("SELECT ReadOnly FROM " + TABLE + " WHERE ID = ?", new DbParam("?", id));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) == 1;
        }

        /// <summary>Gibt es einen anderen Satz mit diesem Bezeichner?</summary>
        public static bool NameBelegt(string bezeichner, int ausserId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TABLE + " WHERE Bezeichner = ? AND ID <> ?",
                new DbParam("?", (bezeichner ?? "").Trim()), new DbParam("?", ausserId));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Prüft einen Satz vor dem Schreiben — ohne Datenbank. Liefert <c>null</c>, wenn er schreibbar ist,
        /// sonst den Grund in der Oberflächensprache.
        /// </summary>
        public static string Pruefen(KaeltemaschineModel m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.Bezeichner))
                return MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG;
            if (m.Rueckkuehlart != null && !KaeltemaschineSchema.RUECKKUEHLARTEN.Contains(m.Rueckkuehlart))
                return MyResource.Resource.KM_MSG_RUECKKUEHLART_UNGUELTIG;
            if ((m.Nennkaelteleistung_kW.HasValue && m.Nennkaelteleistung_kW.Value <= 0) ||
                (m.Nenn_EER.HasValue && m.Nenn_EER.Value <= 0))
                return MyResource.Resource.KM_MSG_NENNWERT_UNGUELTIG;
            if (m.Mindestteillast_Prozent.HasValue && (m.Mindestteillast_Prozent.Value < 0 || m.Mindestteillast_Prozent.Value > 100))
                return MyResource.Resource.KM_MSG_TEILLAST_UNGUELTIG;
            if ((m.Hilfsstrom_Rueckkuehlung_kW.HasValue && m.Hilfsstrom_Rueckkuehlung_kW.Value < 0) ||
                (m.Modulkosten.HasValue && m.Modulkosten.Value < 0))
                return MyResource.Resource.KM_MSG_NENNWERT_UNGUELTIG;
            var punkte = new HashSet<(double, double)>();
            foreach (KaeltemaschineKenndatenModel k in m.Kennlinie ?? new List<KaeltemaschineKenndatenModel>())
            {
                if ((k.EER.HasValue && k.EER.Value <= 0) || (k.Kaelteleistung_kW.HasValue && k.Kaelteleistung_kW.Value < 0))
                    return MyResource.Resource.KM_MSG_KENNLINIE_UNGUELTIG;
                if (!punkte.Add((k.Rueckkuehltemperatur, k.Kaltwassertemperatur)))
                    return MyResource.Resource.KM_MSG_KENNLINIE_DOPPELT;
            }
            return TeillastPruefen(m) ?? KatalogfelderPruefen(m);
        }

        /// <summary>
        /// Prüft die fünf Katalogfelder (<see cref="KaelteKatalogfelderSchema"/>) — ohne Datenbank: Geräteart aus der
        /// Wertemenge und verträglich mit der Rückkühlart (ein luftgekühlter Kaltwassersatz rückkühlt mit Luft, ein
        /// wassergekühlter nicht), GWP und Füllmenge im Bereich, saisonale Kennzahl nur mit Art und im Bereich ihrer Art.
        /// </summary>
        public static string KatalogfelderPruefen(KaeltemaschineModel m)
        {
            if (m == null) return null;
            if (!string.IsNullOrEmpty(m.Geraeteart) && !KaelteKatalogfelderSchema.GERAETEARTEN.Contains(m.Geraeteart))
                return MyResource.Resource.KM_MSG_GERAETEART_UNGUELTIG;
            if (m.Rueckkuehlart != null &&
                ((m.Geraeteart == KaelteKatalogfelderSchema.GERAETEART_KWS_LUFT && m.Rueckkuehlart != KaeltemaschineSchema.RUECKKUEHLART_LUFT) ||
                 (m.Geraeteart == KaelteKatalogfelderSchema.GERAETEART_KWS_WASSER && m.Rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_LUFT)))
                return MyResource.Resource.KM_MSG_GERAETEART_RUECKKUEHLART;
            if ((m.Kaeltemittel_GWP.HasValue && (m.Kaeltemittel_GWP.Value < 0 || m.Kaeltemittel_GWP.Value > KaelteKatalogfelderSchema.GWP_MAX)) ||
                (m.Kaeltemittel_Fuellmenge_kg.HasValue && (m.Kaeltemittel_Fuellmenge_kg.Value <= 0 ||
                                                           m.Kaeltemittel_Fuellmenge_kg.Value > KaelteKatalogfelderSchema.FUELLMENGE_MAX)))
                return MyResource.Resource.KM_MSG_KAELTEMITTEL_UNGUELTIG;
            if (m.Saisonkennzahl_Art != null && !KaelteKatalogfelderSchema.SAISON_ARTEN.Contains(m.Saisonkennzahl_Art))
                return MyResource.Resource.KM_MSG_SAISONKENNZAHL_UNGUELTIG;
            if (m.Saisonkennzahl.HasValue != (m.Saisonkennzahl_Art != null))
                return MyResource.Resource.KM_MSG_SAISONKENNZAHL_UNGUELTIG;
            if (m.Saisonkennzahl.HasValue)
            {
                double w = m.Saisonkennzahl.Value;
                bool seer = m.Saisonkennzahl_Art == KaelteKatalogfelderSchema.SAISON_SEER;
                double von = seer ? KaelteKatalogfelderSchema.SEER_MIN : KaelteKatalogfelderSchema.ETA_S_C_MIN;
                double bis = seer ? KaelteKatalogfelderSchema.SEER_MAX : KaelteKatalogfelderSchema.ETA_S_C_MAX;
                if (double.IsNaN(w) || w < von || w > bis) return MyResource.Resource.KM_MSG_SAISONKENNZAHL_UNGUELTIG;
            }
            return null;
        }

        /// <summary>
        /// Der Anzeigetext einer Geräteart in der Oberflächensprache; leer für <c>null</c>, der Wert selbst für einen
        /// unbekannten. <b>Nie ein Steuerwert</b> — geschrieben wird allein der Persistenzwert.
        /// </summary>
        public static string GeraeteartText(string persistenzwert)
        {
            switch (persistenzwert)
            {
                case null: return "";
                case KaelteKatalogfelderSchema.GERAETEART_KWS_LUFT: return MyResource.Resource.KM_GERAETEART_KWS_LUFT;
                case KaelteKatalogfelderSchema.GERAETEART_KWS_WASSER: return MyResource.Resource.KM_GERAETEART_KWS_WASSER;
                case KaelteKatalogfelderSchema.GERAETEART_KWS_FREIKUEHLUNG: return MyResource.Resource.KM_GERAETEART_KWS_FREIKUEHLUNG;
                case KaelteKatalogfelderSchema.GERAETEART_SPLIT: return MyResource.Resource.KM_GERAETEART_SPLIT;
                case KaelteKatalogfelderSchema.GERAETEART_MULTISPLIT: return MyResource.Resource.KM_GERAETEART_MULTISPLIT;
                case KaelteKatalogfelderSchema.GERAETEART_VRF: return MyResource.Resource.KM_GERAETEART_VRF;
                case KaelteKatalogfelderSchema.GERAETEART_ABSORPTION: return MyResource.Resource.KM_GERAETEART_ABSORPTION;
                default: return persistenzwert;
            }
        }

        /// <summary>
        /// Prüft die acht Felder von Teillast und Takten (<see cref="KaeltemaschineTeillastSchema"/>) — ohne Datenbank.
        /// Persistenzwerte aus den Wertelisten, x_u und C_d in 0…1, die Beiwerte nur zu dritt und in ihren Eingabegrenzen,
        /// die Kurve plausibel nach Fachkonzept 3.2: E(x) &gt; 0 und 0,3 ≤ g(x) ≤ 2,0 auf [x_u, 1], 0,9 ≤ EIRFPLR(1) ≤ 1,1.
        /// <c>null</c>, wenn alles passt, sonst der Grund in der Oberflächensprache.
        /// </summary>
        public static string TeillastPruefen(KaeltemaschineModel m)
        {
            if (m == null) return null;
            if (m.Teillast_Weg != null && !KaeltemaschineTeillastSchema.TEILLAST_WEGE.Contains(m.Teillast_Weg))
                return MyResource.Resource.KM_MSG_TEILLASTWEG_UNGUELTIG;
            if (m.Verdichterregelung != null && !KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN.Contains(m.Verdichterregelung))
                return MyResource.Resource.KM_MSG_VERDICHTERREGELUNG_UNGUELTIG;
            if (m.Kennfeld_Randweg != null && !KaeltemaschineTeillastSchema.RANDWEGE.Contains(m.Kennfeld_Randweg))
                return MyResource.Resource.KM_MSG_RANDWEG_UNGUELTIG;
            if (!Anteil(m.Teillastkurve_Lastgrad_Min) || !Anteil(m.Taktverlustfaktor_Cd))
                return MyResource.Resource.KM_MSG_TEILLASTANTEIL_UNGUELTIG;

            int gepflegt = (m.Teillastkurve_a.HasValue ? 1 : 0) + (m.Teillastkurve_b.HasValue ? 1 : 0) +
                           (m.Teillastkurve_c.HasValue ? 1 : 0);
            if (gepflegt == 0) return null;
            double a = m.Teillastkurve_a ?? double.NaN, bw = m.Teillastkurve_b ?? double.NaN, c = m.Teillastkurve_c ?? double.NaN;
            if (gepflegt < 3 ||
                !Bereich(a, KaeltemaschineTeillastSchema.KURVE_A_MIN, KaeltemaschineTeillastSchema.KURVE_A_MAX) ||
                !Bereich(bw, KaeltemaschineTeillastSchema.KURVE_BC_MIN, KaeltemaschineTeillastSchema.KURVE_BC_MAX) ||
                !Bereich(c, KaeltemaschineTeillastSchema.KURVE_BC_MIN, KaeltemaschineTeillastSchema.KURVE_BC_MAX))
                return MyResource.Resource.KM_MSG_TEILLASTKURVE_BEIWERTE;
            return KurvePlausibel(a, bw, c, UntereGueltigkeit(m)) ? null : MyResource.Resource.KM_MSG_TEILLASTKURVE_UNPLAUSIBEL;
        }

        /// <summary>
        /// Die untere Gültigkeit der Kurve: x_u, sonst die Mindestteillast als Anteil, sonst 0 (Fachkonzept 4.1).
        /// </summary>
        public static double UntereGueltigkeit(KaeltemaschineModel m)
            => m?.Teillastkurve_Lastgrad_Min ?? (m?.Mindestteillast_Prozent.HasValue == true ? m.Mindestteillast_Prozent.Value / 100.0 : 0.0);

        /// <summary>
        /// Die Plausibilität einer Kurve nach Fachkonzept 3.2 — geprüft an <see cref="KaeltemaschineTeillastSchema.PRUEF_STELLEN"/>
        /// gleichmäßigen Stellen auf [max(x_u, <see cref="KaeltemaschineTeillastSchema.PRUEF_LASTGRAD_UNTEN"/>), 1].
        /// </summary>
        public static bool KurvePlausibel(double a, double b, double c, double xu)
        {
            double e1 = a + b + c;
            if (double.IsNaN(e1) || e1 < KaeltemaschineTeillastSchema.EIRFPLR1_MIN || e1 > KaeltemaschineTeillastSchema.EIRFPLR1_MAX)
                return false;
            double von = Math.Max(xu, KaeltemaschineTeillastSchema.PRUEF_LASTGRAD_UNTEN);
            if (von > 1) von = 1;
            int n = KaeltemaschineTeillastSchema.PRUEF_STELLEN;
            for (int i = 0; i <= n; i++)
            {
                double x = von + (1 - von) * i / n;
                double e = (a + b * x + c * x * x) / e1;
                if (!(e > 0)) return false;
                double g = x / e;
                if (g < KaeltemaschineTeillastSchema.G_MIN || g > KaeltemaschineTeillastSchema.G_MAX) return false;
            }
            return true;
        }

        /// <summary>
        /// Liest eine Eingabezahl der Teillastfelder: Komma oder Punkt als Dezimalzeichen, leer = <c>null</c>.
        /// <c>false</c>, wenn der Text keine endliche Zahl ist.
        /// </summary>
        public static bool ZahlLesen(string text, out double? wert)
        {
            wert = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            string s = text.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || double.IsNaN(d) ||
                double.IsInfinity(d))
                return false;
            wert = d;
            return true;
        }

        /// <summary>
        /// <b>Kontrollwert Nenn-EER</b> (Fachkonzept Teillast und Takten 3.5): Weicht der gepflegte Nenn-EER um mehr als
        /// <see cref="KaelteFestwerte.NENN_EER_ABWEICHUNG"/> vom EER des eigenen Kennfelds am Eurovent-Nennpunkt ab
        /// (bilinear wie die Simulation), der Hinweis in der Oberflächensprache — kein Fehler, <see cref="Pruefen"/> lehnt
        /// deswegen nicht ab. <c>null</c> ohne Nenn-EER, ohne Kennfeld oder innerhalb der Spanne.
        /// </summary>
        public static string NennEerHinweis(KaeltemaschineModel m)
        {
            if (m?.Nenn_EER == null || !(m.Nenn_EER.Value > 0) || m.Kennlinie == null || m.Kennlinie.Count == 0) return null;
            var k = new KaeltemaschinenKennlinie(m.Kennlinie.Select(p => (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER, p.Kaelteleistung_kW)));
            if (k.Leer) return null;
            double eerKf = k.Auswerten(KaeltemaschinenKennfeld.Nennrueckkuehltemperatur(m.Rueckkuehlart),
                                       KaeltemaschinenKennfeld.NENN_KALTWASSER_C).Eer;
            if (!(eerKf > 0)) return null;
            double abw = Math.Abs(m.Nenn_EER.Value - eerKf) / eerKf;
            if (!(abw > KaelteFestwerte.NENN_EER_ABWEICHUNG)) return null;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_MSG_NENN_EER_ABWEICHUNG,
                m.Nenn_EER.Value.ToString("0.00", CultureInfo.CurrentCulture), eerKf.ToString("0.00", CultureInfo.CurrentCulture),
                abw.ToString("0 %", CultureInfo.CurrentCulture));
        }

        private static bool Anteil(double? x) => !x.HasValue || (x.Value >= 0 && x.Value <= 1);

        private static bool Bereich(double x, double min, double max) => !double.IsNaN(x) && x >= min && x <= max;

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// Schreibt einen Katalogsatz samt Kennlinie: neu (<c>m.Id == 0</c>) oder über einen bestehenden, der
        /// nicht zur Auslieferung gehört. Ein ausgelieferter Satz wird nie überschrieben (AD-Q11) — ihn ändert
        /// man über <see cref="Duplizieren"/>.
        /// </summary>
        public static SpeicherErgebnis Speichern(KaeltemaschineModel m)
        {
            string grund = Pruefen(m);
            if (grund != null) return new SpeicherErgebnis(false, grund, 0);
            bool neu = m.Id <= 0;
            try
            {
                if (!neu && Gesperrt(m.Id))
                    return new SpeicherErgebnis(false, MyResource.Resource.KM_MSG_AUSGELIEFERT, m.Id);
                if (NameBelegt(m.Bezeichner, neu ? 0 : m.Id))
                    return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, m.Id);

                int id;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        id = KopfSchreiben(v, TABLE, m, neu, null);
                        KennlinieSchreiben(v, CURVE, id, m.Kennlinie, null);
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                return new SpeicherErgebnis(true, MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, id);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), 0);
            }
        }

        /// <summary>Löscht einen Katalogsatz samt Kennlinie (Kaskade); ein ausgelieferter Satz bleibt.</summary>
        public static SpeicherErgebnis Loeschen(int id)
        {
            try
            {
                if (Gesperrt(id))
                    return new SpeicherErgebnis(false, MyResource.Resource.KM_MSG_AUSGELIEFERT, id);
                int n = DataRepository.ExecuteNonQuery("DELETE FROM " + TABLE + " WHERE ID = ?", new DbParam("?", id));
                return n > 0
                    ? new SpeicherErgebnis(true, "", id)
                    : new SpeicherErgebnis(false, MyResource.Resource.KBROW_MSG_LOESCHEN_FEHLER, id);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), id);
            }
        }

        /// <summary>Kopiert einen Satz samt Kennlinie als eigenen Anwendersatz (AD-Q11).</summary>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName) =>
            Katalogkopie.Duplizieren(TABLE, id, neuerName,
                new Katalogkopie.Kindtabelle(CURVE, KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE));

        /// <summary>Setzt oder löst das Auslieferungskennzeichen (Schloss).</summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt) =>
            Auslieferungskennzeichen.SetzenInTabelle(TABLE, ids, gesperrt);

        // =================================================================
        //  Gemeinsame Bausteine (auch für die Projektkopie)
        // =================================================================

        internal static KaeltemaschineModel Lesen(string tabelle, string kennlinie, int id, bool katalog)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + tabelle + " WHERE ID = ?", new DbParam("?", id));
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            var m = new KaeltemaschineModel
            {
                Id = Ganz(r["ID"]),
                Bezeichner = Text(r["Bezeichner"]) ?? "",
                Firma = Text(r["Firma"]),
                Typ = Text(r["Typ"]),
                Beschreibung = Text(r["Beschreibung"]),
                Nennkaelteleistung_kW = Zahl(r[KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG]),
                Nenn_EER = Zahl(r[KaeltemaschineSchema.SPALTE_NENN_EER]),
                Kaeltemittel = Text(r[KaeltemaschineSchema.SPALTE_KAELTEMITTEL]),
                Rueckkuehlart = Text(r[KaeltemaschineSchema.SPALTE_RUECKKUEHLART]),
                Mindestteillast_Prozent = Zahl(r[KaeltemaschineSchema.SPALTE_MINDESTTEILLAST]),
                Hilfsstrom_Rueckkuehlung_kW = Zahl(r[KaeltemaschineSchema.SPALTE_HILFSSTROM_RUECKKUEHLUNG]),
                Kaltwasser_Vorlauf_Min = Zahl(r[KaeltemaschineSchema.SPALTE_KALTWASSER_VORLAUF_MIN]),
                Modulkosten = Zahl(r[KaeltemaschineSchema.SPALTE_MODULKOSTEN]),
            };
            // Schritt 210: Teillast und Takten - vor dem Schritt fehlen die Spalten, dann null.
            if (r.Table.Columns.Contains(KaeltemaschineTeillastSchema.SPALTE_TEILLAST_WEG))
            {
                m.Teillast_Weg = Text(r[KaeltemaschineTeillastSchema.SPALTE_TEILLAST_WEG]);
                m.Teillastkurve_a = Zahl(r[KaeltemaschineTeillastSchema.SPALTE_KURVE_A]);
                m.Teillastkurve_b = Zahl(r[KaeltemaschineTeillastSchema.SPALTE_KURVE_B]);
                m.Teillastkurve_c = Zahl(r[KaeltemaschineTeillastSchema.SPALTE_KURVE_C]);
                m.Teillastkurve_Lastgrad_Min = Zahl(r[KaeltemaschineTeillastSchema.SPALTE_KURVE_LASTGRAD_MIN]);
                m.Taktverlustfaktor_Cd = Zahl(r[KaeltemaschineTeillastSchema.SPALTE_CD]);
                m.Verdichterregelung = Text(r[KaeltemaschineTeillastSchema.SPALTE_VERDICHTERREGELUNG]);
                m.Kennfeld_Randweg = Text(r[KaeltemaschineTeillastSchema.SPALTE_RANDWEG]);
            }
            // Schritt 211: Katalogfelder - vor dem Schritt fehlen die Spalten; die Geraeteart liest sich dann (und bei
            // einem leeren Wert aus einem aelteren Paket) nach der Rueckfuellregel.
            if (r.Table.Columns.Contains(KaelteKatalogfelderSchema.SPALTE_GERAETEART))
            {
                m.Geraeteart = Text(r[KaelteKatalogfelderSchema.SPALTE_GERAETEART]);
                m.Kaeltemittel_GWP = Zahl(r[KaelteKatalogfelderSchema.SPALTE_GWP]);
                m.Kaeltemittel_Fuellmenge_kg = Zahl(r[KaelteKatalogfelderSchema.SPALTE_FUELLMENGE]);
                m.Saisonkennzahl_Art = Text(r[KaelteKatalogfelderSchema.SPALTE_SAISON_ART]);
                m.Saisonkennzahl = Zahl(r[KaelteKatalogfelderSchema.SPALTE_SAISON_WERT]);
            }
            m.Geraeteart = KaelteKatalogfelderSchema.GeraeteartWirksam(m.Geraeteart, m.Rueckkuehlart);
            if (katalog) m.ReadOnly = Ganz(r["ReadOnly"]) == 1;
            else
            {
                m.IdProjekt = r["ID_Projekt"] == DBNull.Value ? (int?)null : Ganz(r["ID_Projekt"]);
                m.IdStamm = r[KaeltemaschineSchema.SPALTE_ID_STAMM] == DBNull.Value ? (int?)null : Ganz(r[KaeltemaschineSchema.SPALTE_ID_STAMM]);
                // Schritt 183: die Kühleingaben der Projektkopie - vor dem Schritt fehlen die Spalten, dann null.
                if (r.Table.Columns.Contains(KaeltemaschineAnlageSchema.SPALTE_KUEHL_VORLAUF))
                    m.Kuehl_Vorlauf = Zahl(r[KaeltemaschineAnlageSchema.SPALTE_KUEHL_VORLAUF]);
                if (r.Table.Columns.Contains(KaeltemaschineAnlageSchema.SPALTE_KUEHL_HILFSSTROMANTEIL))
                    m.Kuehl_Hilfsstromanteil = Zahl(r[KaeltemaschineAnlageSchema.SPALTE_KUEHL_HILFSSTROMANTEIL]);
            }
            DataTable k = DataRepository.GetDataTable(
                "SELECT * FROM " + kennlinie + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ? ORDER BY " +
                KaeltemaschineSchema.SPALTE_KALTWASSERTEMPERATUR + ", " + KaeltemaschineSchema.SPALTE_RUECKKUEHLTEMPERATUR,
                new DbParam("?", id));
            if (k != null)
                foreach (DataRow z in k.Rows)
                    m.Kennlinie.Add(new KaeltemaschineKenndatenModel
                    {
                        Rueckkuehltemperatur = Zahl(z[KaeltemaschineSchema.SPALTE_RUECKKUEHLTEMPERATUR]) ?? 0,
                        Kaltwassertemperatur = Zahl(z[KaeltemaschineSchema.SPALTE_KALTWASSERTEMPERATUR]) ?? 0,
                        EER = Zahl(z[KaeltemaschineSchema.SPALTE_EER]),
                        Kaelteleistung_kW = Zahl(z[KaeltemaschineSchema.SPALTE_KAELTELEISTUNG]),
                    });
            return m;
        }

        private static object Wert(object o) => o ?? DBNull.Value;

        private static DbParam[] Kopfwerte(KaeltemaschineModel m) => new[]
        {
            new DbParam("?", m.Bezeichner.Trim()), new DbParam("?", Wert(m.Firma)), new DbParam("?", Wert(m.Typ)),
            new DbParam("?", Wert(m.Beschreibung)), new DbParam("?", Wert(m.Nennkaelteleistung_kW)),
            new DbParam("?", Wert(m.Nenn_EER)), new DbParam("?", Wert(m.Kaeltemittel)),
            new DbParam("?", Wert(m.Rueckkuehlart)), new DbParam("?", Wert(m.Mindestteillast_Prozent)),
            new DbParam("?", Wert(m.Hilfsstrom_Rueckkuehlung_kW)), new DbParam("?", Wert(m.Kaltwasser_Vorlauf_Min)),
            new DbParam("?", Wert(m.Modulkosten)),
        };

        /// <summary>Schreibt den Kopf; <paramref name="projekt"/> ≠ <c>null</c> schreibt eine Projektkopie (ID_Projekt, ID_Stamm).</summary>
        internal static int KopfSchreiben(DbVorgang v, string tabelle, KaeltemaschineModel m, bool neu, int? projekt)
        {
            string spalten = string.Join(", ", KaeltemaschineSchema.Grundspalten.Select(s => "\"" + s + "\""));
            if (neu)
            {
                string zusatz = projekt.HasValue ? ", \"ID_Projekt\", \"" + KaeltemaschineSchema.SPALTE_ID_STAMM + "\"" : "";
                string marken = string.Join(", ", KaeltemaschineSchema.Grundspalten.Select(_ => "?")) + (projekt.HasValue ? ", ?, ?" : "");
                var p = Kopfwerte(m).ToList();
                if (projekt.HasValue)
                {
                    p.Add(new DbParam("?", projekt.Value));
                    p.Add(new DbParam("?", Wert(m.IdStamm)));
                }
                v.Ausfuehren("INSERT INTO " + tabelle + " (" + spalten + zusatz + ") VALUES (" + marken + ")", p.ToArray());
                int neueId = Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
                TeillastSchreiben(v, tabelle, neueId, m);
                KatalogfelderSchreiben(v, tabelle, neueId, m);
                return neueId;
            }
            string setzen = string.Join(", ", KaeltemaschineSchema.Grundspalten.Select(s => "\"" + s + "\" = ?"));
            var q = Kopfwerte(m).ToList();
            q.Add(new DbParam("?", m.Id));
            v.Ausfuehren("UPDATE " + tabelle + " SET " + setzen + " WHERE ID = ?", q.ToArray());
            TeillastSchreiben(v, tabelle, m.Id, m);
            KatalogfelderSchreiben(v, tabelle, m.Id, m);
            return m.Id;
        }

        /// <summary>
        /// Schreibt die acht Felder von Teillast und Takten an den Satz <paramref name="id"/> — in einem eigenen Schritt
        /// im Vorgang <paramref name="v"/> und nur, wenn die Spalten stehen (Schritt <see cref="KaeltemaschineTeillastSchema.SCHRITT"/>).
        /// Eine ältere Datenbank speichert damit alles Übrige wie zuvor; leer bleibt NULL, nie 0.
        /// </summary>
        internal static void TeillastSchreiben(DbVorgang v, string tabelle, int id, KaeltemaschineModel m)
        {
            // Die Auskunft auf der Verbindung des Vorgangs: Sie sieht das Schema, in dem geschrieben wird.
            object da = v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name IN (?, ?, ?, ?, ?, ?, ?, ?)",
                new[] { new DbParam("?", tabelle) }
                    .Concat(KaeltemaschineTeillastSchema.EINGABESPALTEN.Select(s => new DbParam("?", s))).ToArray());
            if (da == null || da == DBNull.Value ||
                Convert.ToInt32(da, CultureInfo.InvariantCulture) < KaeltemaschineTeillastSchema.EINGABESPALTEN.Count)
                return;
            string setzen = string.Join(", ", KaeltemaschineTeillastSchema.EINGABESPALTEN.Select(s => "\"" + s + "\" = ?"));
            v.Ausfuehren("UPDATE " + tabelle + " SET " + setzen + " WHERE ID = ?",
                new DbParam("?", Wert(m.Teillast_Weg)), new DbParam("?", Wert(m.Teillastkurve_a)),
                new DbParam("?", Wert(m.Teillastkurve_b)), new DbParam("?", Wert(m.Teillastkurve_c)),
                new DbParam("?", Wert(m.Teillastkurve_Lastgrad_Min)), new DbParam("?", Wert(m.Taktverlustfaktor_Cd)),
                new DbParam("?", Wert(m.Verdichterregelung)), new DbParam("?", Wert(m.Kennfeld_Randweg)),
                new DbParam("?", id));
        }

        /// <summary>
        /// Schreibt die fünf Katalogfelder (<see cref="KaelteKatalogfelderSchema"/>) an den Satz <paramref name="id"/> — in
        /// einem eigenen Schritt und nur, wenn die Spalten stehen (Schritt <see cref="KaelteKatalogfelderSchema.SCHRITT"/>). Die
        /// Geräteart wird nie leer geschrieben: fehlt sie, gilt die Rückfüllregel aus der Rückkühlart.
        /// </summary>
        internal static void KatalogfelderSchreiben(DbVorgang v, string tabelle, int id, KaeltemaschineModel m)
        {
            object da = v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name IN (?, ?, ?, ?, ?)",
                new[] { new DbParam("?", tabelle) }
                    .Concat(KaelteKatalogfelderSchema.FELDSPALTEN.Select(s => new DbParam("?", s))).ToArray());
            if (da == null || da == DBNull.Value ||
                Convert.ToInt32(da, CultureInfo.InvariantCulture) < KaelteKatalogfelderSchema.FELDSPALTEN.Count)
                return;
            string setzen = string.Join(", ", KaelteKatalogfelderSchema.FELDSPALTEN.Select(s => "\"" + s + "\" = ?"));
            v.Ausfuehren("UPDATE " + tabelle + " SET " + setzen + " WHERE ID = ?",
                new DbParam("?", KaelteKatalogfelderSchema.GeraeteartWirksam(m.Geraeteart, m.Rueckkuehlart)),
                new DbParam("?", Wert(m.Kaeltemittel_GWP)), new DbParam("?", Wert(m.Kaeltemittel_Fuellmenge_kg)),
                new DbParam("?", Wert(m.Saisonkennzahl_Art)), new DbParam("?", Wert(m.Saisonkennzahl)),
                new DbParam("?", id));
        }

        /// <summary>Ersetzt die Kennlinie eines Geräts; <paramref name="projekt"/> ≠ <c>null</c> schreibt <c>ID_Projekt</c> mit.</summary>
        internal static void KennlinieSchreiben(DbVorgang v, string tabelle, int id, IEnumerable<KaeltemaschineKenndatenModel> punkte, int? projekt)
        {
            v.Ausfuehren("DELETE FROM " + tabelle + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", id));
            string spalten = KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + ", " + string.Join(", ", KaeltemaschineSchema.KennlinienSpalten) +
                             (projekt.HasValue ? ", ID_Projekt" : "");
            string marken = "?, ?, ?, ?, ?" + (projekt.HasValue ? ", ?" : "");
            foreach (KaeltemaschineKenndatenModel k in punkte ?? Enumerable.Empty<KaeltemaschineKenndatenModel>())
            {
                var p = new List<DbParam>
                {
                    new DbParam("?", id), new DbParam("?", k.Rueckkuehltemperatur), new DbParam("?", k.Kaltwassertemperatur),
                    new DbParam("?", Wert(k.EER)), new DbParam("?", Wert(k.Kaelteleistung_kW)),
                };
                if (projekt.HasValue) p.Add(new DbParam("?", projekt.Value));
                v.Ausfuehren("INSERT INTO " + tabelle + " (" + spalten + ") VALUES (" + marken + ")", p.ToArray());
            }
        }

        internal static string Text(object o) => o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        internal static double? Zahl(object o) => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        internal static int Ganz(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// <b>Die Projektkopie der Kältemaschine</b> (<c>Tab_Kaeltemaschine</c>, KU3-1): Übernahme Katalog → Projekt
    /// samt Kennlinie, Lesen. Die Zuordnung zum Katalog trägt <c>ID_Stamm</c> (Muster der Wärmepumpe).
    /// </summary>
    public static class KaeltemaschineCtrl
    {
        /// <summary>Die Projekttabelle.</summary>
        public const string TABLE = KaeltemaschineSchema.TAB_PROJEKT;

        /// <summary>Die Kennlinie der Projektkopie.</summary>
        public const string CURVE = KaeltemaschineSchema.TAB_KENNDATEN;

        /// <summary>Eine Projektkopie samt Kennlinie; <c>null</c>, wenn es die ID nicht gibt.</summary>
        public static KaeltemaschineModel Laden(int id) => KaeltemaschineStammCtrl.Lesen(TABLE, CURVE, id, false);

        /// <summary>Die IDs der Kältemaschinen eines Projekts.</summary>
        public static IReadOnlyList<int> IdsImProjekt(int projektId)
        {
            var ids = new List<int>();
            DataTable dt = DataRepository.GetDataTable("SELECT ID FROM " + TABLE + " WHERE ID_Projekt = ? ORDER BY ID",
                                                       new DbParam("?", projektId));
            if (dt != null) foreach (DataRow r in dt.Rows) ids.Add(KaeltemaschineStammCtrl.Ganz(r["ID"]));
            return ids;
        }

        /// <summary>
        /// Die Projektkopien eines Projekts für den Rechenweg (KU3-2): still — eine Datenbank ohne
        /// <c>Tab_Kaeltemaschine</c> (vor Schritt <see cref="KaeltemaschineSchema.SCHRITT"/>) oder ein
        /// Lesefehler liefert die leere Liste, der Lauf rechnet dann wie ohne Kältemaschine.
        /// </summary>
        public static IReadOnlyList<int> IdsImProjektStill(int projektId)
        {
            try
            {
                if (!DataRepository.TabelleVorhanden(TABLE)) return Array.Empty<int>();
                return IdsImProjekt(projektId);
            }
            catch (Exception)
            {
                return Array.Empty<int>();
            }
        }

        /// <summary>Die Zahl der Projektkopien eines Projekts, still wie <see cref="IdsImProjektStill"/>.</summary>
        public static int AnzahlImProjektStill(int projektId) => IdsImProjektStill(projektId).Count;

        /// <summary>Lädt eine Projektkopie für den Rechenweg; <c>null</c> bei einem Lesefehler.</summary>
        public static KaeltemaschineModel LadenStill(int id)
        {
            try { return Laden(id); }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Kopiert den Katalogsatz <paramref name="stammId"/> samt Kennlinie in das Projekt
        /// <paramref name="projektId"/> — Spalte für Spalte nach <see cref="KaeltemaschineSchema.Fachspalten"/> (die acht
        /// Spalten von Teillast und Takten, wenn sie stehen),
        /// mit <c>ID_Stamm</c> als Zuordnung. Führt das Projekt schon eine Kopie dieses Katalogsatzes, bleibt
        /// sie und ihre ID kommt zurück. <c>-1</c>, wenn es den Katalogsatz nicht gibt.
        /// </summary>
        public static int AusKatalogUebernehmen(int stammId, int projektId)
        {
            object vorhanden = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE + " WHERE ID_Projekt = ? AND " + KaeltemaschineSchema.SPALTE_ID_STAMM + " = ? ORDER BY ID",
                new DbParam("?", projektId), new DbParam("?", stammId));
            if (vorhanden != null && vorhanden != DBNull.Value) return Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);

            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(stammId);
            if (m == null) return -1;
            m.IdStamm = stammId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int id = KaeltemaschineStammCtrl.KopfSchreiben(v, TABLE, m, true, projektId);
                    KaeltemaschineStammCtrl.KennlinieSchreiben(v, CURVE, id, m.Kennlinie, projektId);
                    v.Commit();
                    return id;
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
        }
    }
}
