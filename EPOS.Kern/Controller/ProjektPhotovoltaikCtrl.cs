using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die AUFGELOESTE PV-Verguetung eines Stands (Konzept § 2.16): welche Zeile gilt —
    /// und woher sie kommt.
    ///
    /// <para>Die Herkunft ist kein Nebenprodukt, sondern die Aussage, die Reiter, Dialog
    /// und Bericht zeigen muessen: Zwei Varianten derselben Gruppe koennen mit
    /// verschiedenen Verguetungen rechnen, und wer die Zahlen liest, muss erkennen,
    /// welche.</para>
    /// </summary>
    public sealed class PvVerguetungStand
    {
        /// <summary>Die geltende Zeile; <c>null</c> = keine gepflegt (Flat-Pfad).</summary>
        public readonly ProjektPhotovoltaikModel Modell;

        /// <summary>Kommt die Zeile vom Stammprojekt? <c>false</c> = eigene Werte.</summary>
        public readonly bool Uebernommen;

        /// <summary>Das Projekt, dessen Zeile gilt — bei Uebernahme der Stamm.</summary>
        public readonly int IdQuelle;

        public PvVerguetungStand(ProjektPhotovoltaikModel modell, bool uebernommen, int idQuelle)
        {
            Modell = modell;
            Uebernommen = uebernommen;
            IdQuelle = idQuelle;
        }

        /// <summary>Gilt ueberhaupt eine AKTIVE Verguetung? Sonst rechnet der Flat-Pfad.</summary>
        public bool Aktiv { get { return Modell != null && Modell.Aktiv; } }
    }

    // ---------------------------------------------------------------------------
    // Zugriff auf Tab_ProjektPhotovoltaik (PV-Konzept Paragraf 6.1, Etappe P3)
    // und die Marktwert-Solar-Rueckfallketten (Paragraf 6.3, Nachtrag N2).
    // Durchgaengig DataRepository mit ?-Parametern; MAX+1-Hausmuster.
    // ---------------------------------------------------------------------------
    public class ProjektPhotovoltaikCtrl
    {
        public const string TABELLE = "Tab_ProjektPhotovoltaik";

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>Die PV-Verguetungszeile des Projekts; null = keine gepflegt.</summary>
        public ProjektPhotovoltaikModel Lies(int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + TABELLE + "] WHERE ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return null;
                return AusZeile(dt.Rows[0]);
            }
            catch { return null; }
        }

        /// <summary>
        /// Wie <see cref="Lies"/>, aber nie null: Ohne Zeile kommt ein VORBELEGTES
        /// Modell zurueck (Aktiv=false, Ueberschusseinspeisung, DV-Entgelt 0,40 -
        /// N5, Ausfallanteil 20 % - F5, Degradation 0,5 %/a - E2.4, IBN 1.1. des
        /// laufenden Jahres) - bewusst OHNE Schreiben; erst Speichern legt die Zeile an
        /// (kein DDL-DEFAULT).
        ///
        /// <para><b>Warum die Degradation NUR hier vorbelegt wird.</b> Eine BESTEHENDE
        /// Zeile behaelt ihr NULL, und NULL heisst 0 %/a - sonst aenderte allein die
        /// Migration die Erloesreihe jedes Bestandsprojekts. Vorbelegt wird also nur,
        /// was der Anwender ohnehin gerade neu anlegt; genau dieselbe Trennung wie bei
        /// DV-Entgelt (N5) und Ausfallanteil (F5).</para>
        /// </summary>
        public ProjektPhotovoltaikModel LiesOderVorbelegt(int idProjekt)
        {
            ProjektPhotovoltaikModel m = Lies(idProjekt);
            if (m != null) return m;

            return new ProjektPhotovoltaikModel
            {
                ID = 0,
                ID_Projekt = idProjekt,
                Aktiv = false,
                Vermarktungsform = DbWerte.PV_VERMARKTUNG_EV,
                Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS,
                Inbetriebnahme = new DateTime(DateTime.Now.Year, 1, 1),
                DvEntgelt = 0.40,
                AusfallanteilProzent = 20.0,
                Par51_Anwenden = DbWerte.PV_SCHALTER_AUTO,
                Par51a_Kompensieren = true,
                Kappung60_Anwenden = DbWerte.PV_SCHALTER_AUTO,
                MarktwertEntwicklung = 0,
                BezugAusPreisreihe = false,
                Degradation = 0.5,
                // Konzept § 2.16: Wer diese Zeile speichert, legt EIGENE Werte an.
                // „Uebernommen" ist der Zustand OHNE Zeile.
                UebernahmeStamm = false
            };
        }

        // =====================================================================
        // Die AUFGELOESTE Verguetung (Konzept § 2.16)
        // =====================================================================

        /// <summary>
        /// Die Verguetungszeile, die fuer diesen Stand GILT — samt der Auskunft, woher
        /// sie kommt (Konzept § 2.16, Schemaschritt 93).
        ///
        /// <para><b>Die Aufloesung in drei Zeilen:</b> Ein STAMMPROJEKT fuehrt immer
        /// eigene Werte — seine Zeile gilt, wie sie ist. Eine VARIANTE mit eigener Zeile
        /// und <c>Uebernahme_Stamm = 0</c> ebenso. Sonst gilt die Zeile ihres Stamms; hat
        /// auch der keine, bleibt es beim Flat-Pfad
        /// (<see cref="PvVerguetungStand.Modell"/> ist dann <c>null</c>).</para>
        ///
        /// <para><b>Warum nicht <see cref="Lies"/> mit einem zweiten Parameter.</b> Der
        /// Aufloesungsweg braucht die Variantenverknuepfung und damit einen zweiten
        /// Controller; <see cref="Lies"/> bleibt der reine Zeilenleser, den Dialoge und
        /// Huellen weiter brauchen — sie pflegen die Zeile EINES Projekts und duerfen
        /// dabei nicht auf die des Stamms umgelenkt werden.</para>
        /// </summary>
        /// <param name="idProjekt">Der Stand, fuer den gerechnet oder angezeigt wird.</param>
        public PvVerguetungStand LiesAufgeloest(int idProjekt)
        {
            if (idProjekt <= 0) return new PvVerguetungStand(null, false, 0);

            ProjektPhotovoltaikModel eigen = Lies(idProjekt);

            int idStamm;
            try { idStamm = new VariantenCtrl().StammRefDerVariante(idProjekt); }
            catch { idStamm = -1; }

            // Stammprojekt (oder freistehendes Projekt): die eigene Zeile gilt, und die
            // Wahl wird gar nicht erst gefragt.
            if (idStamm <= 0 || idStamm == idProjekt)
                return new PvVerguetungStand(eigen, false, idProjekt);

            // Variante mit eigener, geltender Zeile.
            if (eigen != null && !eigen.UebernahmeStamm)
                return new PvVerguetungStand(eigen, false, idProjekt);

            // Variante, die uebernimmt - keine Zeile oder Kennzeichen gesetzt.
            return new PvVerguetungStand(Lies(idStamm), true, idStamm);
        }

        /// <summary>
        /// Die Werte, die „eigene Verguetung" vorlegt (Anwenderentscheid VV‑Q6): eine
        /// KOPIE der Stammzeile auf das eigene Projekt, mit <c>Uebernahme_Stamm = 0</c>.
        /// Fehlt dem Stamm die Zeile, kommt die Vorbelegung des Controllers
        /// (<see cref="LiesOderVorbelegt"/>) — der Anwender will eine Abweichung von
        /// einer bekannten Basis, und wo es keine gibt, den gewohnten leeren Satz.
        ///
        /// <para><b>Schreibt nichts.</b> Angelegt wird die Zeile erst von
        /// <see cref="Speichern"/> — dieselbe Trennung wie bei
        /// <see cref="LiesOderVorbelegt"/>.</para>
        /// </summary>
        public ProjektPhotovoltaikModel VorlageAusStamm(int idProjekt)
        {
            PvVerguetungStand stand = LiesAufgeloest(idProjekt);
            if (stand.Modell == null || stand.IdQuelle == idProjekt)
            {
                ProjektPhotovoltaikModel m = LiesOderVorbelegt(idProjekt);
                m.UebernahmeStamm = false;
                return m;
            }

            ProjektPhotovoltaikModel kopie = Kopie(stand.Modell);
            kopie.ID = 0;                       // Speichern vergibt MAX(ID)+1
            kopie.ID_Projekt = idProjekt;
            kopie.UebernahmeStamm = false;
            return kopie;
        }

        /// <summary>Feldweise Kopie — der ganze Block, nie ein Mischsatz (VV‑Q2).</summary>
        public static ProjektPhotovoltaikModel Kopie(ProjektPhotovoltaikModel m)
        {
            if (m == null) return null;
            return new ProjektPhotovoltaikModel
            {
                ID = m.ID,
                ID_Projekt = m.ID_Projekt,
                Aktiv = m.Aktiv,
                Vermarktungsform = m.Vermarktungsform,
                Einspeiseart = m.Einspeiseart,
                Inbetriebnahme = m.Inbetriebnahme,
                KwpOverride = m.KwpOverride,
                AwOverride = m.AwOverride,
                DvEntgelt = m.DvEntgelt,
                PpaPreis = m.PpaPreis,
                PpaSpotAufschlag = m.PpaSpotAufschlag,
                // ETAPPE E9a (Schritt D): die Erlössätze je Szenario gehören zum Block.
                DvEntgeltBest = m.DvEntgeltBest,
                DvEntgeltWorst = m.DvEntgeltWorst,
                PpaPreisBest = m.PpaPreisBest,
                PpaPreisWorst = m.PpaPreisWorst,
                Par51_Anwenden = m.Par51_Anwenden,
                IMSys_Einbaujahr = m.IMSys_Einbaujahr,
                AusfallanteilProzent = m.AusfallanteilProzent,
                Par51a_Kompensieren = m.Par51a_Kompensieren,
                Kappung60_Anwenden = m.Kappung60_Anwenden,
                MarktwertJahresmittel = m.MarktwertJahresmittel,
                MarktwertEntwicklung = m.MarktwertEntwicklung,
                BezugAusPreisreihe = m.BezugAusPreisreihe,
                Degradation = m.Degradation,
                UebernahmeStamm = m.UebernahmeStamm,
                GeaendertAm = m.GeaendertAm
            };
        }

        /// <summary>
        /// Setzt die WAHL einer Variante, ohne die Werte anzufassen (Konzept § 2.16).
        ///
        /// <para><c>true</c> = übernehmen: Die Zeile bleibt stehen — sie ist der Rückweg
        /// zu den eigenen Werten —, gelesen wird ab jetzt die des Stamms. Ohne Zeile ist
        /// nichts zu tun: „keine Zeile" heißt schon übernehmen.</para>
        ///
        /// <para><c>false</c> = eigene Werte: Hat die Variante eine Zeile, gilt sie
        /// wieder; hat sie keine, legt der Aufrufer sie über
        /// <see cref="VorlageAusStamm"/> und <see cref="Speichern"/> an (VV‑Q6).</para>
        /// </summary>
        /// <returns><c>true</c>, wenn die Wahl steht — auch dann, wenn nichts zu schreiben war.</returns>
        public bool SetzeUebernahme(int idProjekt, bool uebernehmen)
        {
            if (idProjekt <= 0) return false;
            try
            {
                ProjektPhotovoltaikModel m = Lies(idProjekt);
                if (m == null) return uebernehmen;      // keine Zeile = uebernommen
                if (m.UebernahmeStamm == uebernehmen) return true;

                m.UebernahmeStamm = uebernehmen;
                return Speichern(m);
            }
            catch { return false; }
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>Upsert der Projektzeile (eine je Projekt). true bei Erfolg.</summary>
        public bool Speichern(ProjektPhotovoltaikModel m)
        {
            if (m == null || m.ID_Projekt <= 0) return false;
            try
            {
                m.GeaendertAm = DateTime.Now;

                // ETAPPE E9a (Schritt D, Schemaschritt 118): die Erlössätze je Szenario
                // wandern mit, wo die Spalten stehen; eine Datenbank vor 118 schreibt die
                // übrigen Felder wie bisher (Muster Preisbasis der Trägerkarte).
                bool mitSzenario = SzenarioSpaltenVorhanden();
                string szenarioSet = mitSzenario
                    ? "[" + SchemaKatalog.SPALTE_PPV_DV_ENTGELT_BEST + "] = ?, [" +
                      SchemaKatalog.SPALTE_PPV_DV_ENTGELT_WORST + "] = ?, [" +
                      SchemaKatalog.SPALTE_PPV_PPA_PREIS_BEST + "] = ?, [" +
                      SchemaKatalog.SPALTE_PPV_PPA_PREIS_WORST + "] = ?, "
                    : "";

                int rows = (int)DataRepository.ExecuteNonQuery(
                    "UPDATE [" + TABELLE + "] SET Aktiv = ?, Vermarktungsform = ?, " +
                    "Einspeiseart = ?, Inbetriebnahme = ?, KwpOverride = ?, AwOverride = ?, " +
                    "DvEntgelt = ?, PpaPreis = ?, PpaSpotAufschlag = ?, Par51_Anwenden = ?, " +
                    "IMSys_Einbaujahr = ?, AusfallanteilProzent = ?, Par51a_Kompensieren = ?, " +
                    "Kappung60_Anwenden = ?, MarktwertJahresmittel = ?, MarktwertEntwicklung = ?, " +
                    "BezugAusPreisreihe = ?, Degradation = ?, [" +
                    SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM + "] = ?, " + szenarioSet +
                    "GeaendertAm = ? WHERE ID_Projekt = ?",
                    Parameter(m, projektAnsEnde: true, mitSzenario: mitSzenario));
                if (rows > 0) return true;

                object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM [" + TABELLE + "]");
                m.ID = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;

                return DataRepository.ExecuteSQL(
                    "INSERT INTO [" + TABELLE + "] (ID, ID_Projekt, Aktiv, Vermarktungsform, " +
                    "Einspeiseart, Inbetriebnahme, KwpOverride, AwOverride, DvEntgelt, PpaPreis, " +
                    "PpaSpotAufschlag, Par51_Anwenden, IMSys_Einbaujahr, AusfallanteilProzent, " +
                    "Par51a_Kompensieren, Kappung60_Anwenden, MarktwertJahresmittel, " +
                    "MarktwertEntwicklung, BezugAusPreisreihe, Degradation, [" +
                    SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM + "], " +
                    (mitSzenario
                        ? "[" + SchemaKatalog.SPALTE_PPV_DV_ENTGELT_BEST + "], [" +
                          SchemaKatalog.SPALTE_PPV_DV_ENTGELT_WORST + "], [" +
                          SchemaKatalog.SPALTE_PPV_PPA_PREIS_BEST + "], [" +
                          SchemaKatalog.SPALTE_PPV_PPA_PREIS_WORST + "], "
                        : "") +
                    "GeaendertAm) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?" +
                    (mitSzenario ? ", ?, ?, ?, ?" : "") + ")",
                    ParameterInsert(m, mitSzenario));
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Die PV-Vergütungsangaben konnten nicht gespeichert werden: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        // Marktwerte (Paragraf 6.3 / N2)
        // =====================================================================

        /// <summary>
        /// Monatsmarktwert Solar [ct/kWh] aus der Stammreihe „Marktwert Solar" des
        /// Jahres (Tab_Preisreihe, Aufloesung Monat). null = Jahr oder Monat nicht
        /// gepflegt — dann greift die Rueckfallkette des Aufrufers
        /// (<see cref="Jahresmarktwert"/>, Fortschreibung in Etappe P4).
        /// </summary>
        /// <summary>
        /// ETAPPE P6 (Konzept 6.3, Prüfschritt aus P3): Monatsmarktwerte Solar aus
        /// einer CSV in die Stammreihen übernehmen (Bezeichner „Marktwert Solar“,
        /// eine Reihe je Jahr, Auflösung Monat, ct/kWh). Verstanden werden:
        ///  a) der netztransparenz-Export — Kopfzeile mit einer „Solar“-Spalte,
        ///     je Zeile ein Monat („MM.JJJJ“, „JJJJ-MM“ oder deutscher
        ///     Monatsname + Jahr), und
        ///  b) die einfache Liste „Jahr;Monat;Wert“.
        /// Trenner Semikolon oder Tabulator, Dezimalkomma und -punkt. Je Jahr wird
        /// die Stammreihe KOMPLETT ersetzt (die CSV ist die Quelle der Wahrheit);
        /// ein angebrochenes Jahr ist zulässig, solange die Monate lückenlos bei
        /// Januar beginnen (Tab_PreisreiheDaten kennt nur die Reihenfolge).
        /// </summary>
        public bool ImportiereMarktwerteCsv(string pfad, out string bericht)
        {
            bericht = "";
            string[] zeilen;
            try
            {
                byte[] roh = System.IO.File.ReadAllBytes(pfad);
                string text;
                try { text = new System.Text.UTF8Encoding(false, true).GetString(roh); }
                catch (System.Text.DecoderFallbackException)
                { text = System.Text.Encoding.Latin1.GetString(roh); }
                zeilen = text.Replace("\r\n", "\n").Split('\n');
            }
            catch (Exception ex) { bericht = ex.Message; return false; }

            var jahre = new SortedDictionary<int, double?[]>();
            int spalteSolar = -1;
            foreach (string zeileRoh in zeilen)
            {
                string zeile = zeileRoh.Trim();
                if (zeile.Length == 0) continue;
                string[] felder = zeile.Split(zeile.IndexOf('\t') >= 0 ? '\t' : ';');

                // Kopfzeile des netztransparenz-Formats: Spalte mit „Solar“.
                if (spalteSolar < 0)
                {
                    for (int i = 1; i < felder.Length; i++)
                        if (felder[i].IndexOf("Solar", StringComparison.OrdinalIgnoreCase) >= 0)
                        { spalteSolar = i; break; }
                    if (spalteSolar >= 0) continue;
                }

                int jahr, monat; double wert;
                if (felder.Length >= 3 &&
                    int.TryParse(felder[0].Trim(), out jahr) && jahr >= 2000 && jahr <= 2099 &&
                    int.TryParse(felder[1].Trim(), out monat) && monat >= 1 && monat <= 12 &&
                    Zahl(felder[2], out wert))
                    Merke(jahre, jahr, monat, wert);                       // Format b
                else if (spalteSolar > 0 && felder.Length > spalteSolar &&
                         MonatJahr(felder[0], out jahr, out monat) &&
                         Zahl(felder[spalteSolar], out wert))
                    Merke(jahre, jahr, monat, wert);                       // Format a
            }

            if (jahre.Count == 0)
            { bericht = "Keine Marktwert-Zeilen erkannt (erwartet: netztransparenz-Export mit Solar-Spalte oder Jahr;Monat;Wert)."; return false; }

            var teile = new List<string>();
            foreach (KeyValuePair<int, double?[]> kv in jahre)
            {
                int n = 0;
                while (n < 12 && kv.Value[n].HasValue) n++;
                for (int i = n; i < 12; i++)
                    if (kv.Value[i].HasValue)
                    { bericht = "Jahr " + kv.Key + ": Monatslücke vor Monat " + (i + 1) + " — die Reihe braucht lückenlose Monate ab Januar."; return false; }
                if (n == 0) continue;

                object kopf = DataRepository.ExecuteScalar(
                    "SELECT MAX(ID) FROM [" + SchemaKatalog.TAB_PREISREIHE + "] " +
                    "WHERE Bezeichner = ? AND Jahr = ? AND ID_Projekt IS NULL",
                    new DbParam("@b", DbWerte.PV_MARKTWERT_BEZEICHNER),
                    new DbParam("@j", kv.Key));
                int kopfId;
                if (kopf != null && kopf != DBNull.Value)
                {
                    kopfId = Convert.ToInt32(kopf);
                    DataRepository.ExecuteSQL(
                        "DELETE FROM [Tab_PreisreiheDaten] WHERE ID_Preisreihe = " + kopfId);
                }
                else
                {
                    kopfId = DataRepository.GetMaxID(SchemaKatalog.TAB_PREISREIHE) + 1;
                    DataRepository.ExecuteSQL(
                        "INSERT INTO [" + SchemaKatalog.TAB_PREISREIHE + "] " +
                        "(ID, ID_Projekt, Bezeichner, Jahr, Aufloesung, Einheit, ID_Energietraeger) " +
                        "VALUES (?, NULL, ?, ?, ?, ?, NULL)",
                        new DbParam("@id", kopfId),
                        new DbParam("@b", DbWerte.PV_MARKTWERT_BEZEICHNER),
                        new DbParam("@j", kv.Key),
                        new DbParam("@a", DbWerte.PREISREIHE_AUFLOESUNG_MONAT),
                        new DbParam("@e", DbWerte.PREISREIHE_EINHEIT_CT_KWH));
                }

                int datenId = DataRepository.GetMaxID("Tab_PreisreiheDaten");
                for (int m = 0; m < n; m++)
                {
                    datenId++;
                    DataRepository.ExecuteSQL(
                        "INSERT INTO [Tab_PreisreiheDaten] (ID, ID_Preisreihe, Wert) VALUES (?, ?, ?)",
                        new DbParam("@id", datenId),
                        new DbParam("@k", kopfId),
                        new DbParam("@w", kv.Value[m].Value));
                }
                teile.Add(kv.Key + ": " + n + (n == 12 ? " Monate" : " Monate (Jan–" + n + ")"));
            }
            bericht = string.Join(" · ", teile.ToArray());
            return teile.Count > 0;
        }

        private static void Merke(SortedDictionary<int, double?[]> jahre, int jahr, int monat, double wert)
        {
            if (!jahre.ContainsKey(jahr)) jahre[jahr] = new double?[12];
            jahre[jahr][monat - 1] = wert;
        }

        private static bool Zahl(string text, out double wert)
        {
            return double.TryParse((text ?? "").Trim().Replace(",", "."),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out wert);
        }

        private static readonly string[] MONATSNAMEN =
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };

        /// <summary>„MM.JJJJ“, „JJJJ-MM“ oder „‹Monatsname› JJJJ“ (auch „Mrz“) → Jahr/Monat.</summary>
        private static bool MonatJahr(string feld, out int jahr, out int monat)
        {
            jahr = 0; monat = 0;
            string t = (feld ?? "").Trim();
            if (t.Length == 0) return false;
            string[] p1 = t.Split('.');
            if (p1.Length >= 2 && int.TryParse(p1[0], out monat) && monat >= 1 && monat <= 12 &&
                int.TryParse(p1[p1.Length - 1].Substring(0, Math.Min(4, p1[p1.Length - 1].Length)), out jahr) &&
                jahr >= 2000 && jahr <= 2099)
                return true;
            string[] p2 = t.Split('-');
            if (p2.Length == 2 && int.TryParse(p2[0], out jahr) && jahr >= 2000 && jahr <= 2099 &&
                int.TryParse(p2[1], out monat) && monat >= 1 && monat <= 12)
                return true;
            for (int i = 0; i < 12; i++)
            {
                string kurz = i == 2 ? "Mrz" : MONATSNAMEN[i].Substring(0, 3);
                if (t.StartsWith(MONATSNAMEN[i], StringComparison.OrdinalIgnoreCase) ||
                    t.StartsWith(kurz, StringComparison.OrdinalIgnoreCase))
                {
                    int leer = t.LastIndexOf(' ');
                    if (leer > 0 && int.TryParse(t.Substring(leer + 1), out jahr) &&
                        jahr >= 2000 && jahr <= 2099)
                    { monat = i + 1; return true; }
                }
            }
            return false;
        }

        public double? MarktwertMonatCt(int jahr, int monat)
        {
            if (monat < 1 || monat > 12) return null;
            try
            {
                object id = DataRepository.ExecuteScalar(
                    "SELECT MAX(ID) FROM [Tab_Preisreihe] " +
                    "WHERE Bezeichner = ? AND Jahr = ? AND ID_Projekt IS NULL",
                    new DbParam("@b", DbWerte.PV_MARKTWERT_BEZEICHNER),
                    new DbParam("@j", jahr));
                if (id == null || id == DBNull.Value) return null;

                double[] werte = new PreisreiheCtrl().ReadWerte(Convert.ToInt32(id));
                if (werte == null || werte.Length < monat) return null;
                return werte[monat - 1];
            }
            catch { return null; }
        }

        /// <summary>
        /// Jahresmarktwert Solar [ct/kWh] fuer die Marktpraemie (N2):
        /// Projekt-Override (<c>MarktwertJahresmittel</c>) vor dem AMTLICHEN
        /// Katalogwert des EXAKTEN Jahres (<c>EEG_JAHRESMARKTWERT_SOLAR</c>).
        /// null = beides fehlt (z. B. kuenftige Jahre) — die Fortschreibung ueber
        /// <c>MarktwertEntwicklung</c> ist Sache der Erloesbildung (P4), nicht
        /// dieses Lesers: Das Stichtagsmuster des Katalogs wuerde sonst still den
        /// VORJAHRESwert liefern und die Szenariofortschreibung verdecken.
        /// </summary>
        public double? Jahresmarktwert(int jahr, ProjektPhotovoltaikModel projekt)
        {
            if (projekt != null && projekt.MarktwertJahresmittel.HasValue &&
                projekt.MarktwertJahresmittel.Value > 0)
                return projekt.MarktwertJahresmittel.Value;

            try
            {
                GesetzParameter p = new GesetzKatalog()
                    .WertMitHerkunft(DbWerte.GESETZ_EEG_JAHRESMARKTWERT_SOLAR, jahr);
                if (p != null && p.JahrVon == jahr && p.Wert.HasValue) return p.Wert.Value;
            }
            catch { }
            return null;
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        private static ProjektPhotovoltaikModel AusZeile(DataRow r)
        {
            var m = new ProjektPhotovoltaikModel
            {
                ID = Ganz(r, "ID") ?? 0,
                ID_Projekt = Ganz(r, "ID_Projekt") ?? 0,
                Aktiv = Wahr(r, "Aktiv"),
                Vermarktungsform = Text(r, "Vermarktungsform", DbWerte.PV_VERMARKTUNG_EV),
                Einspeiseart = Text(r, "Einspeiseart", DbWerte.PV_EINSPEISEART_UEBERSCHUSS),
                KwpOverride = Zahl(r, "KwpOverride"),
                AwOverride = Zahl(r, "AwOverride"),
                DvEntgelt = Zahl(r, "DvEntgelt"),
                PpaPreis = Zahl(r, "PpaPreis"),
                PpaSpotAufschlag = Zahl(r, "PpaSpotAufschlag"),
                // ETAPPE E9a (Schritt D, Schemaschritt 118): tolerant — eine fehlende
                // Spalte ist „wie Erwartet", eine 0 ebenso (NULL/0-Regel).
                DvEntgeltBest = OhneNull(Zahl(r, SchemaKatalog.SPALTE_PPV_DV_ENTGELT_BEST)),
                DvEntgeltWorst = OhneNull(Zahl(r, SchemaKatalog.SPALTE_PPV_DV_ENTGELT_WORST)),
                PpaPreisBest = OhneNull(Zahl(r, SchemaKatalog.SPALTE_PPV_PPA_PREIS_BEST)),
                PpaPreisWorst = OhneNull(Zahl(r, SchemaKatalog.SPALTE_PPV_PPA_PREIS_WORST)),
                Par51_Anwenden = Text(r, "Par51_Anwenden", DbWerte.PV_SCHALTER_AUTO),
                IMSys_Einbaujahr = Ganz(r, "IMSys_Einbaujahr"),
                AusfallanteilProzent = Zahl(r, "AusfallanteilProzent"),
                Par51a_Kompensieren = Wahr(r, "Par51a_Kompensieren"),
                Kappung60_Anwenden = Text(r, "Kappung60_Anwenden", DbWerte.PV_SCHALTER_AUTO),
                MarktwertJahresmittel = Zahl(r, "MarktwertJahresmittel"),
                MarktwertEntwicklung = Zahl(r, "MarktwertEntwicklung") ?? 0,
                BezugAusPreisreihe = Wahr(r, "BezugAusPreisreihe"),
                // E2.4: NULL bleibt NULL - der Rechner liest daraus 0 %/a. Eine
                // fehlende Spalte (Datenbank vor Schritt 63) ist derselbe Fall.
                Degradation = Zahl(r, "Degradation"),
                // Konzept § 2.16: Eine Zeile, die DA ist, gilt - das ist der Bestand
                // vor Schemaschritt 93. Fehlende Spalte und NULL lesen sich deshalb
                // beide als false; "uebernommen" ist der Zustand OHNE Zeile oder das
                // ausdrueckliche Kennzeichen 1.
                UebernahmeStamm = Wahr(r, SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM)
            };
            object ibn = r["Inbetriebnahme"];
            m.Inbetriebnahme = (ibn == null || ibn == DBNull.Value)
                ? DateTime.MinValue : Convert.ToDateTime(ibn);
            object g = r["GeaendertAm"];
            m.GeaendertAm = (g == null || g == DBNull.Value)
                ? (DateTime?)null : Convert.ToDateTime(g);
            return m;
        }

        /// <summary>
        /// ETAPPE E9a (Schritt D): Führt <c>Tab_ProjektPhotovoltaik</c> die vier Spalten der
        /// Erlössätze je Szenario (Schemaschritt 118)? Ohne sie schreibt
        /// <see cref="Speichern"/> die übrigen Felder wie bisher.
        /// </summary>
        public static bool SzenarioSpaltenVorhanden()
        {
            try
            {
                foreach (SchemaSpalte s in SchemaKatalog.Schritt118_ErloessatzPhotovoltaik)
                    if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// ETAPPE E9a (Schritt D) — die WIRKSAME Vergütungszeile eines Szenarios: eine Kopie,
        /// in der DV-Entgelt und PPA-Festpreis durch ihren gepflegten Szenariowert ersetzt
        /// sind (Regel <see cref="SzenarioSatz.Gepflegt(double?, double)"/>: nicht leer, nicht
        /// 0, vom Erwartungswert verschieden). Für ERWARTET und ohne Pflege kommt
        /// <b>dieselbe Referenz</b> zurück — der Rechenweg bleibt bitgleich. Die EINE Stelle,
        /// an der der Rechenweg die PV-Erlössätze je Szenario liest.
        /// </summary>
        public static ProjektPhotovoltaikModel FuerSzenario(ProjektPhotovoltaikModel m, string szenario)
        {
            if (m == null) return null;
            double? dv = Szenariowert(szenario, m.DvEntgeltBest, m.DvEntgeltWorst);
            double? ppa = Szenariowert(szenario, m.PpaPreisBest, m.PpaPreisWorst);
            bool dvGepflegt = SzenarioSatz.Gepflegt(dv, m.DvEntgelt ?? 0.0);
            bool ppaGepflegt = SzenarioSatz.Gepflegt(ppa, m.PpaPreis ?? 0.0);
            if (!dvGepflegt && !ppaGepflegt) return m;

            ProjektPhotovoltaikModel k = Kopie(m);
            if (dvGepflegt) k.DvEntgelt = dv;
            if (ppaGepflegt) k.PpaPreis = ppa;
            return k;
        }

        private static double? Szenariowert(string szenario, double? best, double? worst)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal)) return best;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal)) return worst;
            return null;
        }

        private static double? OhneNull(double? wert)
        {
            return wert.HasValue && wert.Value != 0 ? wert : null;
        }

        private static DbParam[] Parameter(ProjektPhotovoltaikModel m, bool projektAnsEnde, bool mitSzenario)
        {
            var p = new System.Collections.Generic.List<DbParam>
            {
                new DbParam("@akt", m.Aktiv),
                new DbParam("@ver", m.Vermarktungsform ?? DbWerte.PV_VERMARKTUNG_EV),
                new DbParam("@art", m.Einspeiseart ?? DbWerte.PV_EINSPEISEART_UEBERSCHUSS),
                new DbParam("@ibn", DbParamTyp.Date)
                    { Wert = m.Inbetriebnahme == DateTime.MinValue ? (object)DBNull.Value : m.Inbetriebnahme },
                D("@kwp", m.KwpOverride), D("@aw", m.AwOverride), D("@dv", m.DvEntgelt),
                D("@ppa", m.PpaPreis), D("@spot", m.PpaSpotAufschlag),
                new DbParam("@p51", m.Par51_Anwenden ?? DbWerte.PV_SCHALTER_AUTO),
                G("@ims", m.IMSys_Einbaujahr), D("@ausf", m.AusfallanteilProzent),
                new DbParam("@p51a", m.Par51a_Kompensieren),
                new DbParam("@kap", m.Kappung60_Anwenden ?? DbWerte.PV_SCHALTER_AUTO),
                D("@jw", m.MarktwertJahresmittel),
                new DbParam("@mw", m.MarktwertEntwicklung),
                new DbParam("@bez", m.BezugAusPreisreihe),
                D("@deg", m.Degradation),
                // Konzept § 2.16: 0/1, nie NULL - eine Zeile, die geschrieben wird,
                // sagt ausdruecklich, ob sie gilt.
                new DbParam("@ueb", DbParamTyp.Integer) { Wert = m.UebernahmeStamm ? 1 : 0 }
            };
            // ETAPPE E9a (Schritt D): die vier Erlössätze je Szenario, in der Reihenfolge der
            // Spaltenliste von Speichern; leer oder 0 schreibt NULL („wie Erwartet").
            if (mitSzenario)
            {
                p.Add(D("@dvb", OhneNull(m.DvEntgeltBest)));
                p.Add(D("@dvw", OhneNull(m.DvEntgeltWorst)));
                p.Add(D("@ppab", OhneNull(m.PpaPreisBest)));
                p.Add(D("@ppaw", OhneNull(m.PpaPreisWorst)));
            }
            p.Add(new DbParam("@ga", DbParamTyp.Date) { Wert = m.GeaendertAm ?? (object)DBNull.Value });
            if (projektAnsEnde) p.Add(new DbParam("@pid", m.ID_Projekt));
            return p.ToArray();
        }

        private static DbParam[] ParameterInsert(ProjektPhotovoltaikModel m, bool mitSzenario)
        {
            var kopf = new System.Collections.Generic.List<DbParam>
            {
                new DbParam("@id", m.ID),
                new DbParam("@pid", m.ID_Projekt)
            };
            kopf.AddRange(Parameter(m, projektAnsEnde: false, mitSzenario: mitSzenario));
            return kopf.ToArray();
        }

        private static DbParam D(string name, double? wert)
        {
            return new DbParam(name, DbParamTyp.Double)
            { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        private static DbParam G(string name, int? wert)
        {
            return new DbParam(name, DbParamTyp.Integer)
            { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        private static double? Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte)) return null;
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? (double?)null : Convert.ToDouble(v);
        }

        private static int? Ganz(DataRow r, string spalte)
        {
            double? z = Zahl(r, spalte);
            return z.HasValue ? (int?)Convert.ToInt32(z.Value) : null;
        }

        private static bool Wahr(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte)) return false;
            object v = r[spalte];
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        private static string Text(DataRow r, string spalte, string rueckfall)
        {
            if (!r.Table.Columns.Contains(spalte)) return rueckfall;
            object v = r[spalte];
            string s = (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
            return string.IsNullOrEmpty(s) ? rueckfall : s;
        }
    }
}
