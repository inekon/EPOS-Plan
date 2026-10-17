using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Katalog der Rückkühlwerke</b> (<c>Tab_Rueckkuehlwerk_STAMM</c>, K-F1): Liste, Laden, Prüfen ohne Datenbank,
    /// Speichern, Löschen, Duplizieren und das Auslieferungskennzeichen — nach dem Muster von
    /// <see cref="KaeltemaschineStammCtrl"/>. Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    /// </summary>
    public static class RueckkuehlwerkStammCtrl
    {
        /// <summary>Die Katalogtabelle.</summary>
        public const string TABLE = RueckkuehlwerkSchema.TAB_STAMM;

        /// <summary>Der Ausgang eines Schreibwegs: bei Erfolg die ID, sonst der Grund in der Oberflächensprache.</summary>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, int Id);

        /// <summary>Eine Zeile der Katalogliste.</summary>
        public sealed record Listenzeile(int Id, string Bezeichner, string Bauart, double? Nennleistung_kW,
                                         double? Annaeherung_Nenn_K, bool ReadOnly, bool Katalogsatz);

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Alle Katalogsätze, nach Bezeichner sortiert; leer vor Schritt <see cref="RueckkuehlwerkSchema.SCHRITT"/>.</summary>
        public static IReadOnlyList<Listenzeile> Liste()
        {
            var liste = new List<Listenzeile>();
            if (!DataRepository.TabelleVorhanden(TABLE)) return liste;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, " + RueckkuehlwerkSchema.SPALTE_BAUART + ", " + RueckkuehlwerkSchema.SPALTE_NENNLEISTUNG + ", " +
                RueckkuehlwerkSchema.SPALTE_ANNAEHERUNG_NENN + ", ReadOnly, " + Katalogfassung.SPALTE_SCHLUESSEL +
                " FROM " + TABLE + " ORDER BY Bezeichner");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add(new Listenzeile(Ganz(r["ID"]), Text(r["Bezeichner"]) ?? "", Text(r[RueckkuehlwerkSchema.SPALTE_BAUART]),
                                          Zahl(r[RueckkuehlwerkSchema.SPALTE_NENNLEISTUNG]),
                                          Zahl(r[RueckkuehlwerkSchema.SPALTE_ANNAEHERUNG_NENN]),
                                          Ganz(r["ReadOnly"]) == 1,
                                          !string.IsNullOrEmpty(Text(r[Katalogfassung.SPALTE_SCHLUESSEL]))));
            return liste;
        }

        /// <summary>Ein Katalogsatz; <c>null</c>, wenn es die ID nicht gibt.</summary>
        public static RueckkuehlwerkModel Laden(int id) => Lesen(TABLE, id, true);

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

        /// <summary>
        /// Die Verwendung eines Katalogsatzes (K-F5a): die Zahl seiner Projektkopien (<c>Tab_Rueckkuehlwerk.ID_Stamm</c>).
        /// Eine Anlage (<c>Tab_Energieanlagen.ID_Rueckkuehlwerk</c>) zeigt immer auf eine solche Kopie; 0 vor dem Schritt.
        /// </summary>
        public static int Verwendungen(int id)
        {
            if (!DataRepository.TabelleVorhanden(RueckkuehlwerkSchema.TAB_PROJEKT)) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + RueckkuehlwerkSchema.TAB_PROJEKT + " WHERE " + RueckkuehlwerkSchema.SPALTE_ID_STAMM + " = ?",
                new DbParam("?", id));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Id des Katalogsatzes → Zahl seiner Projektkopien; nur verwendete Sätze stehen darin.</summary>
        public static IReadOnlyDictionary<int, int> KatalogVerwendung()
        {
            var d = new Dictionary<int, int>();
            if (!DataRepository.TabelleVorhanden(RueckkuehlwerkSchema.TAB_PROJEKT)) return d;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + RueckkuehlwerkSchema.SPALTE_ID_STAMM + " AS Stamm, COUNT(*) AS Anzahl FROM " + RueckkuehlwerkSchema.TAB_PROJEKT +
                " WHERE " + RueckkuehlwerkSchema.SPALTE_ID_STAMM + " IS NOT NULL GROUP BY " + RueckkuehlwerkSchema.SPALTE_ID_STAMM);
            if (dt == null) return d;
            foreach (DataRow r in dt.Rows) d[Ganz(r["Stamm"])] = Ganz(r["Anzahl"]);
            return d;
        }

        /// <summary>
        /// Die Zeilen der Verwaltung „Rückkühlwerke" (K-F5a, Profil <see cref="Katalogfilterprofil.FuerRueckkuehlwerk"/>):
        /// Bezeichner, Bauart, Nennleistung, Annäherung, Ventilator, Freikühlung; der Schlüssel ist die Id. Leere Auswahlen
        /// zeigen die Vorgabe (Bauart trocken, Regelung Ein/Aus, Schaltung parallel).
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();
            if (!DataRepository.TabelleVorhanden(TABLE)) return liste;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, " + RueckkuehlwerkSchema.SPALTE_BAUART + ", " + RueckkuehlwerkSchema.SPALTE_NENNLEISTUNG + ", " +
                RueckkuehlwerkSchema.SPALTE_ANNAEHERUNG_NENN + ", " + RueckkuehlwerkSchema.SPALTE_VENTILATOR_REGELUNG + ", " +
                RueckkuehlwerkSchema.SPALTE_VENTILATOR_NENN + ", " + RueckkuehlwerkSchema.SPALTE_FREIKUEHLUNG_SCHALTUNG +
                ", ReadOnly FROM " + TABLE + " ORDER BY Bezeichner");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                int id = Ganz(r["ID"]);
                string name = Text(r["Bezeichner"]) ?? "";
                string regelung = RegelungText(Text(r[RueckkuehlwerkSchema.SPALTE_VENTILATOR_REGELUNG]));
                double? pVent = Zahl(r[RueckkuehlwerkSchema.SPALTE_VENTILATOR_NENN]);
                string ventilator = pVent.HasValue
                    ? regelung + " · " + pVent.Value.ToString("0.##", CultureInfo.CurrentCulture) + " kW"
                    : regelung;
                var zeile = new Katalogfilterzeile(id, name)
                {
                    Geschuetzt = Ganz(r["ReadOnly"]) == 1,
                    Schluessel = id.ToString(CultureInfo.InvariantCulture)
                };
                liste.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, name)
                    .MitText(Katalogfilterprofil.SpBauart, BauartText(Text(r[RueckkuehlwerkSchema.SPALTE_BAUART])))
                    .MitZahl(Katalogfilterprofil.SpNennleistung, Zahl(r[RueckkuehlwerkSchema.SPALTE_NENNLEISTUNG]), 1)
                    .MitZahl(Katalogfilterprofil.SpAnnaeherung, Zahl(r[RueckkuehlwerkSchema.SPALTE_ANNAEHERUNG_NENN]), 1)
                    .MitText(Katalogfilterprofil.SpVentilator, ventilator)
                    .MitText(Katalogfilterprofil.SpFreikuehlung, SchaltungText(Text(r[RueckkuehlwerkSchema.SPALTE_FREIKUEHLUNG_SCHALTUNG]))));
            }
            return liste;
        }

        // =================================================================
        //  Anzeigetexte der Wertemengen (Steuerwert ≠ Anzeigetext)
        // =================================================================

        /// <summary>Der Anzeigetext einer Bauart; leer = trocken (die Vorgabe), ein unbekannter Wert bleibt, wie er ist.</summary>
        public static string BauartText(string bauart)
        {
            switch (bauart ?? RueckkuehlwerkSchema.BAUART_TROCKEN)
            {
                case RueckkuehlwerkSchema.BAUART_TROCKEN: return MyResource.Resource.RKW_BAUART_TROCKEN;
                case RueckkuehlwerkSchema.BAUART_ADIABAT: return MyResource.Resource.RKW_BAUART_ADIABAT;
                case RueckkuehlwerkSchema.BAUART_HYBRID: return MyResource.Resource.RKW_BAUART_HYBRID;
                case RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN: return MyResource.Resource.RKW_BAUART_KUEHLTURM_OFFEN;
                case RueckkuehlwerkSchema.BAUART_KUEHLTURM_GESCHLOSSEN: return MyResource.Resource.RKW_BAUART_KUEHLTURM_GESCHLOSSEN;
                default: return bauart;
            }
        }

        /// <summary>Der Anzeigetext eines Annäherungswegs; leer = fest (die Vorgabe).</summary>
        public static string WegText(string weg)
            => (weg ?? RueckkuehlwerkSchema.WEG_FEST) == RueckkuehlwerkSchema.WEG_FEST ? MyResource.Resource.RKW_WEG_FEST
             : weg == RueckkuehlwerkSchema.WEG_LASTABHAENGIG ? MyResource.Resource.RKW_WEG_LASTABHAENGIG
             : weg;

        /// <summary>Der Anzeigetext einer Ventilatorregelung; leer = Ein/Aus (die Vorgabe des Rechenwegs).</summary>
        public static string RegelungText(string regelung)
        {
            switch (regelung ?? RueckkuehlwerkSchema.REGELUNG_EIN_AUS)
            {
                case RueckkuehlwerkSchema.REGELUNG_EIN_AUS: return MyResource.Resource.RKW_REGELUNG_EIN_AUS;
                case RueckkuehlwerkSchema.REGELUNG_STUFEN: return MyResource.Resource.RKW_REGELUNG_STUFEN;
                case RueckkuehlwerkSchema.REGELUNG_DREHZAHL: return MyResource.Resource.RKW_REGELUNG_DREHZAHL;
                default: return regelung;
            }
        }

        /// <summary>Der Anzeigetext einer Schaltung der freien Kühlung; leer = parallel (die Vorgabe).</summary>
        public static string SchaltungText(string schaltung)
            => (schaltung ?? RueckkuehlwerkSchema.SCHALTUNG_PARALLEL) == RueckkuehlwerkSchema.SCHALTUNG_PARALLEL ? MyResource.Resource.RKW_SCHALTUNG_PARALLEL
             : schaltung == RueckkuehlwerkSchema.SCHALTUNG_REIHE ? MyResource.Resource.RKW_SCHALTUNG_REIHE
             : schaltung;

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Prüft einen Satz vor dem Schreiben — ohne Datenbank, dieselben Grenzen wie die CHECK-Klauseln von
        /// <see cref="RueckkuehlwerkSchema"/>. Liefert <c>null</c>, wenn er schreibbar ist, sonst den Grund in der
        /// Oberflächensprache. Leere Felder sind immer zulässig (Vorgabe).
        /// </summary>
        public static string Pruefen(RueckkuehlwerkModel m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.Bezeichner) || m.Bezeichner.Trim().Length > 255)
                return MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG;
            if (m.Bauart != null && !RueckkuehlwerkSchema.BAUARTEN.Contains(m.Bauart))
                return MyResource.Resource.RKW_MSG_BAUART_UNGUELTIG;
            // K-F5a: Eine verletzte Wertemenge oder ein Bereich nennt das Feld und - beim Bereich - die Grenzen.
            string wahl = Auswahlbefund(MyResource.Resource.RKW_LBL_ANNAEHERUNG_WEG, m.Annaeherung_Weg, RueckkuehlwerkSchema.ANNAEHERUNG_WEGE)
                       ?? Auswahlbefund(MyResource.Resource.RKW_LBL_VENTILATOR_REGELUNG, m.Ventilator_Regelung, RueckkuehlwerkSchema.VENTILATOR_REGELUNGEN)
                       ?? Auswahlbefund(MyResource.Resource.RKW_LBL_FREIKUEHLUNG_SCHALTUNG, m.Freikuehlung_Schaltung, RueckkuehlwerkSchema.FREIKUEHLUNG_SCHALTUNGEN);
            if (wahl != null) return wahl;
            if (Ausserhalb(m.Nennleistung_kW, v => v > 0))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_NENNLEISTUNG, Groesser(0));
            if (Ausserhalb(m.Annaeherung_Nenn_K, v => v >= 0 && v <= RueckkuehlwerkSchema.ANNAEHERUNG_MAX_K))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_ANNAEHERUNG_NENN, VonBis(0, RueckkuehlwerkSchema.ANNAEHERUNG_MAX_K));
            if (Ausserhalb(m.Ventilator_Nenn_kW, v => v >= 0))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_VENTILATOR_NENN, Ab(0));
            if (m.Ventilator_Stufen.HasValue && m.Ventilator_Stufen.Value < 2)
                return Bereichsbefund(MyResource.Resource.RKW_LBL_VENTILATOR_STUFEN, Ab(2));
            if (Ausserhalb(m.Ventilator_Drehzahl_Min, v => v >= 0 && v <= 1))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_VENTILATOR_DREHZAHL_MIN, VonBis(0, 1));
            if (Ausserhalb(m.Befeuchtung_Wirkungsgrad, v => v >= 0 && v <= 1))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_BEFEUCHTUNG_WIRKUNGSGRAD, VonBis(0, 1));
            if (Ausserhalb(m.Befeuchtung_Ab_C, v => v >= RueckkuehlwerkSchema.BEFEUCHTUNG_AB_MIN_C && v <= RueckkuehlwerkSchema.BEFEUCHTUNG_AB_MAX_C))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_BEFEUCHTUNG_AB,
                                      VonBis(RueckkuehlwerkSchema.BEFEUCHTUNG_AB_MIN_C, RueckkuehlwerkSchema.BEFEUCHTUNG_AB_MAX_C));
            if (Ausserhalb(m.Verdunstung_Faktor, v => v > 0 && v <= RueckkuehlwerkSchema.VERDUNSTUNG_FAKTOR_MAX))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_VERDUNSTUNG_FAKTOR, UeberBis(0, RueckkuehlwerkSchema.VERDUNSTUNG_FAKTOR_MAX));
            if (Ausserhalb(m.Eindickung, v => v > 1 && v <= RueckkuehlwerkSchema.EINDICKUNG_MAX))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_EINDICKUNG, UeberBis(1, RueckkuehlwerkSchema.EINDICKUNG_MAX));
            if (Ausserhalb(m.Drift_Anteil, v => v >= 0 && v <= 1))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_DRIFT_ANTEIL, VonBis(0, 1));
            if (Ausserhalb(m.Modulkosten, v => v >= 0))
                return Bereichsbefund(MyResource.Resource.RKW_LBL_MODULKOSTEN, Ab(0));
            return null;
        }

        /// <summary>Ein gesetzter Steuerwert außerhalb seiner Wertemenge — benannt mit Feld und Wert; sonst <c>null</c>.</summary>
        private static string Auswahlbefund(string feld, string wert, IReadOnlyList<string> menge)
            => wert != null && !menge.Contains(wert)
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_MSG_AUSWAHL_UNGUELTIG, feld, wert)
                : null;

        /// <summary>Ein Wert außerhalb seines Bereichs — benannt mit Feld und Grenzen.</summary>
        private static string Bereichsbefund(string feld, string bereich)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_MSG_WERT_UNGUELTIG, feld, bereich);

        private static string Z(double d) => d.ToString("0.##", CultureInfo.CurrentCulture);
        private static string Groesser(double a) => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_BEREICH_GROESSER, Z(a));
        private static string Ab(double a) => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_BEREICH_AB, Z(a));
        private static string VonBis(double a, double b) => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_BEREICH_VON_BIS, Z(a), Z(b));
        private static string UeberBis(double a, double b) => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_BEREICH_UEBER_BIS, Z(a), Z(b));

        /// <summary>Ein gesetzter Wert, der nicht endlich ist oder die Bedingung verletzt.</summary>
        private static bool Ausserhalb(double? wert, Func<double, bool> gueltig)
            => wert.HasValue && (double.IsNaN(wert.Value) || double.IsInfinity(wert.Value) || !gueltig(wert.Value));

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// Schreibt einen Katalogsatz: neu (<c>m.Id == 0</c>) oder über einen bestehenden, der nicht zur Auslieferung gehört.
        /// Ein ausgelieferter Satz wird nie überschrieben — ihn ändert man über <see cref="Duplizieren"/>.
        /// </summary>
        public static SpeicherErgebnis Speichern(RueckkuehlwerkModel m)
        {
            string grund = Pruefen(m);
            if (grund != null) return new SpeicherErgebnis(false, grund, 0);
            bool neu = m.Id <= 0;
            try
            {
                if (!neu && Gesperrt(m.Id))
                    return new SpeicherErgebnis(false, MyResource.Resource.RKW_MSG_AUSGELIEFERT, m.Id);
                if (NameBelegt(m.Bezeichner, neu ? 0 : m.Id))
                    return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, m.Id);
                int id;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        id = KopfSchreiben(v, TABLE, m, neu, null);
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

        /// <summary>
        /// Löscht einen Katalogsatz — nur einen eigenen und nur ohne Verwendung (K-F5a): Ein ausgelieferter Satz bleibt,
        /// ebenso einer, aus dem eine Projektkopie stammt (<see cref="Verwendungen"/>). Der Fremdschlüssel
        /// (<c>ID_Stamm</c> → NULL) bleibt der Rückweg für Kopien, deren Katalogsatz auf anderem Weg verschwindet.
        /// </summary>
        public static SpeicherErgebnis Loeschen(int id)
        {
            try
            {
                if (Gesperrt(id))
                    return new SpeicherErgebnis(false, MyResource.Resource.RKW_MSG_AUSGELIEFERT, id);
                int verwendet = Verwendungen(id);
                if (verwendet > 0)
                    return new SpeicherErgebnis(false,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RKW_MSG_VERWENDET, verwendet), id);
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

        /// <summary>Kopiert einen Satz als eigenen Anwendersatz.</summary>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName) =>
            Katalogkopie.Duplizieren(TABLE, id, neuerName);

        /// <summary>Setzt oder löst das Auslieferungskennzeichen (Schloss).</summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt) =>
            Auslieferungskennzeichen.SetzenInTabelle(TABLE, ids, gesperrt);

        // =================================================================
        //  Gemeinsame Bausteine (auch für die Projektkopie)
        // =================================================================

        internal static RueckkuehlwerkModel Lesen(string tabelle, int id, bool katalog)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + tabelle + " WHERE ID = ?", new DbParam("?", id));
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            double? ganzzahl = Zahl(r[RueckkuehlwerkSchema.SPALTE_VENTILATOR_STUFEN]);
            var m = new RueckkuehlwerkModel
            {
                Id = Ganz(r["ID"]),
                Bezeichner = Text(r[RueckkuehlwerkSchema.SPALTE_BEZEICHNER]) ?? "",
                Beschreibung = Text(r[RueckkuehlwerkSchema.SPALTE_BESCHREIBUNG]),
                Bauart = Text(r[RueckkuehlwerkSchema.SPALTE_BAUART]),
                Nennleistung_kW = Zahl(r[RueckkuehlwerkSchema.SPALTE_NENNLEISTUNG]),
                Annaeherung_Nenn_K = Zahl(r[RueckkuehlwerkSchema.SPALTE_ANNAEHERUNG_NENN]),
                Annaeherung_Weg = Text(r[RueckkuehlwerkSchema.SPALTE_ANNAEHERUNG_WEG]),
                Ventilator_Nenn_kW = Zahl(r[RueckkuehlwerkSchema.SPALTE_VENTILATOR_NENN]),
                Ventilator_Regelung = Text(r[RueckkuehlwerkSchema.SPALTE_VENTILATOR_REGELUNG]),
                Ventilator_Stufen = ganzzahl.HasValue ? (int?)(int)ganzzahl.Value : null,
                Ventilator_Drehzahl_Min = Zahl(r[RueckkuehlwerkSchema.SPALTE_VENTILATOR_DREHZAHL_MIN]),
                Befeuchtung_Wirkungsgrad = Zahl(r[RueckkuehlwerkSchema.SPALTE_BEFEUCHTUNG_WIRKUNGSGRAD]),
                Befeuchtung_Ab_C = Zahl(r[RueckkuehlwerkSchema.SPALTE_BEFEUCHTUNG_AB]),
                Verdunstung_Faktor = Zahl(r[RueckkuehlwerkSchema.SPALTE_VERDUNSTUNG_FAKTOR]),
                Eindickung = Zahl(r[RueckkuehlwerkSchema.SPALTE_EINDICKUNG]),
                Drift_Anteil = Zahl(r[RueckkuehlwerkSchema.SPALTE_DRIFT_ANTEIL]),
                Freikuehlung_Schaltung = Text(r[RueckkuehlwerkSchema.SPALTE_FREIKUEHLUNG_SCHALTUNG]),
                Modulkosten = Zahl(r[RueckkuehlwerkSchema.SPALTE_MODULKOSTEN]),
            };
            if (katalog) m.ReadOnly = Ganz(r["ReadOnly"]) == 1;
            else
            {
                m.IdProjekt = r["ID_Projekt"] == DBNull.Value ? (int?)null : Ganz(r["ID_Projekt"]);
                m.IdStamm = r[RueckkuehlwerkSchema.SPALTE_ID_STAMM] == DBNull.Value ? (int?)null : Ganz(r[RueckkuehlwerkSchema.SPALTE_ID_STAMM]);
            }
            return m;
        }

        private static object Wert(object o) => o ?? DBNull.Value;

        /// <summary>Die Werte der Fachspalten in der Folge von <see cref="RueckkuehlwerkSchema.Fachspalten"/>.</summary>
        private static DbParam[] Fachwerte(RueckkuehlwerkModel m) => new[]
        {
            new DbParam("?", m.Bezeichner.Trim()), new DbParam("?", Wert(m.Beschreibung)), new DbParam("?", Wert(m.Bauart)),
            new DbParam("?", Wert(m.Nennleistung_kW)), new DbParam("?", Wert(m.Annaeherung_Nenn_K)),
            new DbParam("?", Wert(m.Annaeherung_Weg)), new DbParam("?", Wert(m.Ventilator_Nenn_kW)),
            new DbParam("?", Wert(m.Ventilator_Regelung)), new DbParam("?", Wert(m.Ventilator_Stufen)),
            new DbParam("?", Wert(m.Ventilator_Drehzahl_Min)), new DbParam("?", Wert(m.Befeuchtung_Wirkungsgrad)),
            new DbParam("?", Wert(m.Befeuchtung_Ab_C)), new DbParam("?", Wert(m.Verdunstung_Faktor)),
            new DbParam("?", Wert(m.Eindickung)), new DbParam("?", Wert(m.Drift_Anteil)),
            new DbParam("?", Wert(m.Freikuehlung_Schaltung)), new DbParam("?", Wert(m.Modulkosten)),
        };

        /// <summary>
        /// Schreibt einen Satz; <paramref name="projekt"/> ≠ <c>null</c> schreibt eine Projektkopie (ID_Projekt, ID_Stamm).
        /// <paramref name="festeId"/> legt einen neuen Satz unter dieser ID an (Wiederherstellen einer Projektkopie, K-F5c).
        /// </summary>
        internal static int KopfSchreiben(DbVorgang v, string tabelle, RueckkuehlwerkModel m, bool neu, int? projekt, int? festeId = null)
        {
            string[] spalten = RueckkuehlwerkSchema.Fachspalten;
            if (neu)
            {
                string zusatz = (projekt.HasValue ? ", \"ID_Projekt\", \"" + RueckkuehlwerkSchema.SPALTE_ID_STAMM + "\"" : "")
                              + (festeId.HasValue ? ", \"ID\"" : "");
                string marken = string.Join(", ", spalten.Select(_ => "?")) + (projekt.HasValue ? ", ?, ?" : "")
                              + (festeId.HasValue ? ", ?" : "");
                var p = Fachwerte(m).ToList();
                if (projekt.HasValue)
                {
                    p.Add(new DbParam("?", projekt.Value));
                    p.Add(new DbParam("?", Wert(m.IdStamm)));
                }
                if (festeId.HasValue)
                {
                    v.Ausfuehren("INSERT INTO " + tabelle + " (" + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) + zusatz +
                                 ") VALUES (" + marken + ")", p.Append(new DbParam("?", festeId.Value)).ToArray());
                    return festeId.Value;
                }
                v.Ausfuehren("INSERT INTO " + tabelle + " (" + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) + zusatz +
                             ") VALUES (" + marken + ")", p.ToArray());
                return Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            }
            var q = Fachwerte(m).ToList();
            q.Add(new DbParam("?", m.Id));
            v.Ausfuehren("UPDATE " + tabelle + " SET " + string.Join(", ", spalten.Select(s => "\"" + s + "\" = ?")) +
                         " WHERE ID = ?", q.ToArray());
            return m.Id;
        }

        internal static string Text(object o) => o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        internal static double? Zahl(object o) => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        internal static int Ganz(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// <b>Die Projektkopie des Rückkühlwerks</b> (<c>Tab_Rueckkuehlwerk</c>, K-F1): Übernahme Katalog → Projekt, Lesen,
    /// Speichern und Löschen. Die Zuordnung zum Katalog trägt <c>ID_Stamm</c> (Muster <see cref="KaeltemaschineCtrl"/>);
    /// gewählt wird die Kopie an der Anlagenzeile der Kältemaschine (<see cref="KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen"/>).
    /// </summary>
    public static class RueckkuehlwerkCtrl
    {
        /// <summary>Die Projekttabelle.</summary>
        public const string TABLE = RueckkuehlwerkSchema.TAB_PROJEKT;

        /// <summary>Eine Projektkopie; <c>null</c>, wenn es die ID nicht gibt.</summary>
        public static RueckkuehlwerkModel Laden(int id) => RueckkuehlwerkStammCtrl.Lesen(TABLE, id, false);

        /// <summary>Lädt eine Projektkopie für den Rechenweg; <c>null</c> vor dem Schritt oder bei einem Lesefehler.</summary>
        public static RueckkuehlwerkModel LadenStill(int id)
        {
            try { return DataRepository.TabelleVorhanden(TABLE) ? Laden(id) : null; }
            catch (Exception) { return null; }
        }

        /// <summary>Die IDs der Rückkühlwerke eines Projekts.</summary>
        public static IReadOnlyList<int> IdsImProjekt(int projektId)
        {
            var ids = new List<int>();
            if (!DataRepository.TabelleVorhanden(TABLE)) return ids;
            DataTable dt = DataRepository.GetDataTable("SELECT ID FROM " + TABLE + " WHERE ID_Projekt = ? ORDER BY ID",
                                                       new DbParam("?", projektId));
            if (dt != null) foreach (DataRow r in dt.Rows) ids.Add(RueckkuehlwerkStammCtrl.Ganz(r["ID"]));
            return ids;
        }

        /// <summary>
        /// Legt <b>immer</b> eine neue Projektkopie des Katalogsatzes <paramref name="stammId"/> an (Muster KB-1 der
        /// Kältemaschine: jede Anlage pflegt ihr eigenes Rückkühlwerk), mit <c>ID_Stamm</c> als Zuordnung. <c>-1</c>, wenn
        /// es den Katalogsatz nicht gibt.
        /// </summary>
        public static int AusKatalogUebernehmen(int stammId, int projektId)
        {
            RueckkuehlwerkModel m = RueckkuehlwerkStammCtrl.Laden(stammId);
            if (m == null) return -1;
            m.IdStamm = stammId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int id = RueckkuehlwerkStammCtrl.KopfSchreiben(v, TABLE, m, true, projektId);
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

        /// <summary>Schreibt die Fachspalten einer bestehenden Projektkopie; liefert den Prüftext oder <c>null</c>.</summary>
        public static string Speichern(RueckkuehlwerkModel m)
        {
            string grund = RueckkuehlwerkStammCtrl.Pruefen(m);
            if (grund != null) return grund;
            if (m.Id <= 0) return MyResource.Resource.KM_ANLAGE_FEHLT;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    RueckkuehlwerkStammCtrl.KopfSchreiben(v, TABLE, m, false, null);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Löscht eine Projektkopie; jede Anlagenzeile, die sie führt, verliert ihren Verweis (<c>ID_Rueckkuehlwerk</c> →
        /// NULL) und rechnet wieder über die Rückkühlart ihrer Kältemaschine. Ausdrücklich im Vorgang, damit es auch ohne
        /// eingeschaltete Fremdschlüssel gilt.
        /// </summary>
        public static void Loeschen(int id)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("UPDATE " + RueckkuehlwerkSchema.TAB_ANLAGEN + " SET " + RueckkuehlwerkSchema.SPALTE_ID_RUECKKUEHLWERK +
                                 " = NULL WHERE " + RueckkuehlwerkSchema.SPALTE_ID_RUECKKUEHLWERK + " = ?", new DbParam("?", id));
                    v.Ausfuehren("DELETE FROM " + TABLE + " WHERE ID = ?", new DbParam("?", id));
                    v.Commit();
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
