using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Bauteilaufbauten samt Schichten</b> — Katalog und Projektkopie (Gebäudesimulation
    /// Stufe G3, Schritt S-B; Softwarearchitektur 2.2, 2.6 und 2.9).
    ///
    /// <para><b>Der Aufbau ist ein Aggregat.</b> Gelesen, geschrieben und kopiert wird er mit
    /// seinen Schichten; ein Aufbau ohne Schichten ist ein Aufbau ohne U-Wert. Das Speichern
    /// läuft in EINER Transaktion: Kopf anlegen oder ändern, die Schichten des Aufbaus löschen
    /// und in Listenreihenfolge neu anlegen, <c>Reihenfolge</c> lückenlos ab 1. Die Schichten
    /// dürfen neue Schlüssel bekommen — auf eine Schicht zeigt nichts; auf den Aufbau zeigen die
    /// Bauteile, und sein Schlüssel bleibt.</para>
    ///
    /// <para><b>Die Stoffwerte der Schicht sind eine Kopie</b> zum Zeitpunkt der Zuordnung:
    /// Trägt eine Schicht einen Baustoff, übernimmt das Speichern jeden fehlenden Wert (λ, ρ, c)
    /// aus ihm; ein angegebener Wert bleibt. <c>ID_Baustoff</c> zeigt je Seite auf die eigene
    /// Ablage (W11) — der Katalog auf <c>Tab_Baustoff_STAMM</c>, die Kopie auf <c>Tab_Baustoff</c>
    /// desselben Projekts.</para>
    ///
    /// <para><b>Lesewege</b> (2.9): je Aufbau und je Projekt in ZWEI Abfragen — die Aufbauten und
    /// alle ihre Schichten, sortiert nach (<c>ID_Aufbau</c>, <c>Reihenfolge</c>); die Zuordnung
    /// geschieht im Speicher. Nie eine Abfrage je Aufbau.</para>
    /// </summary>
    public sealed class BauteilaufbauCtrl
    {
        /// <summary>Was ein Schreibversuch ergeben hat — dieselbe Form wie <see cref="BaustoffCtrl.Ergebnis"/>.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung, int Id)
        {
            internal static Ergebnis Gut(int id) => new Ergebnis(true, "", id);
            internal static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "", 0);
        }

        private const string SCHICHTSPALTEN_SQL =
            "\"ID\", \"ID_Aufbau\", \"Reihenfolge\", \"ID_Baustoff\", \"Dicke\", \"IstLuftschicht\", \"Lambda\", \"Rho\", \"cp\"";

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Ein Katalogaufbau samt Schichten; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public BauteilaufbauModel LesenKatalogsatz(int id) => LesenEinen(Seite.Katalog, id);

        /// <summary>Ein Projektaufbau samt Schichten; <c>null</c>, wenn es ihn nicht gibt.</summary>
        public BauteilaufbauModel LesenProjektsatz(int id) => LesenEinen(Seite.Projekt, id);

        /// <summary>
        /// Der Katalog samt Schichten, wahlweise auf eine Bauteilart eingeengt — die Einengung
        /// steht in der Abfrage; <c>null</c> = alle. Zwei Abfragen.
        /// </summary>
        public List<BauteilaufbauModel> LesenKatalog(string bauteilart = null)
        {
            string bedingung = bauteilart == null ? "" : " WHERE a.\"Bauteilart\" = ?";
            DbParam[] p = bauteilart == null ? new DbParam[0] : new[] { new DbParam("@art", bauteilart) };
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT a.* FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" a" + bedingung +
                " ORDER BY a.\"Bezeichner\", a.\"ID\"", p);
            DataTable schichten = DataRepository.GetDataTable(
                "SELECT " + Praefix("s", SCHICHTSPALTEN_SQL) + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT_STAMM + "\" s " +
                "INNER JOIN \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" a ON a.\"ID\" = s.\"ID_Aufbau\"" + bedingung +
                " ORDER BY s.\"ID_Aufbau\", s.\"Reihenfolge\", s.\"ID\"", p);
            return Zusammenfuehren(kopf, schichten);
        }

        /// <summary>
        /// <b>Der Leseweg des Rechenkerns</b> (2.9): alle Aufbauten eines Projekts samt Schichten in
        /// ZWEI Abfragen, die Schichten nach (<c>ID_Aufbau</c>, <c>Reihenfolge</c>).
        /// </summary>
        public List<BauteilaufbauModel> LesenJeProjekt(int idProjekt)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT * FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ? ORDER BY \"Bezeichner\", \"ID\"",
                new DbParam("@p", idProjekt));
            DataTable schichten = DataRepository.GetDataTable(
                "SELECT " + Praefix("s", SCHICHTSPALTEN_SQL) + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT + "\" s " +
                "INNER JOIN \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" a ON a.\"ID\" = s.\"ID_Aufbau\" " +
                "WHERE a.\"ID_Projekt\" = ? ORDER BY s.\"ID_Aufbau\", s.\"Reihenfolge\", s.\"ID\"",
                new DbParam("@p", idProjekt));
            return Zusammenfuehren(kopf, schichten);
        }

        /// <summary>
        /// <b>Die Zeilen der Katalogverwaltung</b> nach <see cref="Katalogfilterprofil.FuerBauteilaufbau"/>
        /// — ZWEI Abfragen (Köpfe, alle Schichten nach Aufbau und Reihenfolge), zusammengeführt im
        /// Speicher. Je Aufbau stehen Bauteilart und Herkunft als Anzeigetext, dazu U, R und die
        /// flächenbezogene Kapazität aus <see cref="Kennwerte(BauteilaufbauModel, bool)"/> — die
        /// Liste rechnet über denselben Weg wie der Summenfuß des Stammblatts.
        ///
        /// <para><b>Der Schlüssel der Zeile ist die Id</b> (Mehrzonenkonzept 5.2), nicht der Name;
        /// ein Aufbau der Auslieferung trägt das Schloss.</para>
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();
            DataTable kopf = StilleDb.Tabelle(
                "SELECT * FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" ORDER BY \"Bezeichner\", \"ID\"");
            if (kopf == null) return liste;
            DataTable schichten = StilleDb.Tabelle(
                "SELECT " + SCHICHTSPALTEN_SQL + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT_STAMM + "\" " +
                "ORDER BY \"ID_Aufbau\", \"Reihenfolge\", \"ID\"");

            foreach (BauteilaufbauModel a in Zusammenfuehren(kopf, schichten))
            {
                BauteilaufbauKennwerte k = Kennwerte(a, mitBezugsperiode: false);
                var zeile = new Katalogfilterzeile(a.ID, a.Bezeichner)
                {
                    Geschuetzt = a.ReadOnly,
                    Schluessel = a.ID.ToString(CultureInfo.InvariantCulture)
                };
                liste.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, a.Bezeichner)
                    .MitText(Katalogfilterprofil.SpBauteilart, BauteilartText(a.Bauteilart))
                    .MitZahl(Katalogfilterprofil.SpUWert, k.U_WM2K, 3)
                    .MitZahl(Katalogfilterprofil.SpRWert, k.R_M2KW, 2)
                    .MitZahl(Katalogfilterprofil.SpKapazitaet, k.Kapazitaet_KJM2K, 0)
                    .MitZahl(Katalogfilterprofil.SpSchichten, a.Schichten.Count, 0)
                    .MitZahl(Katalogfilterprofil.SpDicke, a.Schichten.Count > 0 ? a.Gesamtdicke : (double?)null, 3)
                    .MitText(Katalogfilterprofil.SpHerkunft, BaustoffCtrl.HerkunftText(a.Herkunft)));
            }
            return liste;
        }

        /// <summary>
        /// Der Anzeigetext einer Bauteilart (<see cref="DbWerte.BAUTEILARTEN"/>) — nie Steuerwert;
        /// <c>null</c> heißt am Aufbau „für jede Bauteilart", ein unbekannter Wert erscheint, wie er ist.
        /// </summary>
        public static string BauteilartText(string art)
        {
            if (art == null) return MyResource.Resource.BTA_ART_JEDE;
            string t = DbWerte.BAUTEILARTEN.Contains(art)
                ? MyResource.Resource.ResourceManager.GetString("BTA_ART_" + art)
                : null;
            return string.IsNullOrEmpty(t) ? art : t;
        }

        // =================================================================
        //  Die Schichtdicke: gespeichert in m, angezeigt in mm
        // =================================================================

        /// <summary>Millimeter je Meter — die Anzeigeeinheit der Schichtdicke ist mm, gespeichert wird m.</summary>
        public const double MM_JE_M = 1000.0;

        /// <summary>
        /// Eine Schichtdicke in m als Anzeigewert in mm (die EINE Umrechnung an der Anzeigekante),
        /// auf 10⁻⁶ mm gerundet — sonst trüge 0,175 m als 175,00000000000003 mm zurück, und jedes
        /// Speichern verschöbe die Dicke um ein Bit.
        /// </summary>
        public static double DickeMm(double dickeM) => Math.Round(dickeM * MM_JE_M, 6);

        /// <summary>Ein Anzeigewert in mm als Schichtdicke in m, auf 10⁻⁹ m gerundet (Hin und Zurück sind stabil).</summary>
        public static double DickeM(double dickeMm) => Math.Round(dickeMm / MM_JE_M, 9);

        // =================================================================
        //  Kennwerte eines Aufbaus (Summenfuß, Listenspalten)
        // =================================================================

        /// <summary>
        /// <b>Die Kennwerte eines Aufbaus</b> für die Anzeige — R je Schicht, R = Σ d/λ, U mit den
        /// Übergängen aus der Neigung der Bauteilart an Außenluft, die flächenbezogene Kapazität
        /// Σ ρ·c_p·d und die Bezugsperiode T_BT nach VDI 6007 Blatt 1 Gl. (10a)–(10d) samt der
        /// wirksamen Kapazität C₁ je m². Ohne Datenbank; gerechnet über <c>Bauteilreduktion</c> —
        /// der Weg, den der Lauf nimmt.
        ///
        /// <para><b>Nie eine Ausnahme.</b> Eine unvollständige Schicht (etwa während der Eingabe)
        /// lässt die Summen leer und nennt in <see cref="BauteilaufbauKennwerte.Grund"/> die erste
        /// Lücke; die R-Werte der übrigen Schichten stehen trotzdem.</para>
        /// </summary>
        /// <param name="m">Der Aufbau samt Schichten (Dicke in m).</param>
        /// <param name="mitBezugsperiode">Auch T_BT und C₁ bestimmen (zweimal die Reduktion) — die Liste braucht es nicht.</param>
        public static BauteilaufbauKennwerte Kennwerte(BauteilaufbauModel m, bool mitBezugsperiode = true)
        {
            string art = m?.Bauteilart;
            string wer = string.IsNullOrWhiteSpace(m?.Bezeichner) ? MyResource.Resource.BTA_WER_AUFBAU : m.Bezeichner.Trim();
            double neigung = GebaeudeZonenCtrl.NeigungVorgabe(art);
            Waermestromrichtung richtung = Bauteilreduktion.RichtungAusNeigung(neigung);
            (double rSi, double rSe) = Bauteilreduktion.Uebergangswiderstaende(neigung, Bauteilrand.Aussenluft);

            List<BauteilschichtModel> modelle = (m?.Schichten ?? new List<BauteilschichtModel>()).Where(s => s != null).ToList();
            var rJe = new double?[modelle.Count];
            var schichten = new List<Schicht>(modelle.Count);
            string grund = modelle.Count == 0 ? MyResource.Resource.BAUTEIL_MSG_KEINE_SCHICHT : null;

            for (int i = 0; i < modelle.Count; i++)
            {
                if (!SchichtAusModell(modelle[i], i + 1, out Schicht schicht, out string fehlt))
                {
                    grund ??= fehlt;
                    continue;
                }
                schichten.Add(schicht);
                try
                {
                    Bauteilreduktion.Pruefen(new[] { schicht }, wer);
                    rJe[i] = Bauteilreduktion.Waermedurchlasswiderstand(schicht, richtung, wer, i + 1);
                }
                catch (GebaeudeModellException)
                {
                    // Die Meldung mit der richtigen Schichtnummer liefert der Gesamtlauf darunter.
                }
            }

            var basis = new BauteilaufbauKennwerte
            {
                NeigungGrad = neigung,
                RSi_M2KW = rSi,
                RSe_M2KW = rSe,
                RJeSchicht_M2KW = rJe
            };
            if (grund != null) return basis with { Grund = grund };

            Schichtkennwerte k;
            try
            {
                k = Bauteilreduktion.Kennwerte(schichten, richtung, rSi, rSe, wer);
            }
            catch (GebaeudeModellException ex)
            {
                return basis with { Grund = ex.Message };
            }

            var ergebnis = basis with
            {
                R_M2KW = k.R_M2KW,
                U_WM2K = k.U_WM2K,
                Kapazitaet_KJM2K = k.Kapazitaet_JM2K / JE_KILO
            };
            if (!mitBezugsperiode) return ergebnis;

            try
            {
                Bezugsperiodenwahl wahl = Bauteilreduktion.BezugsperiodeWaehlen(schichten, 1.0, richtung, wer);
                return ergebnis with
                {
                    Bezugsperiode_D = wahl.Periode_d,
                    R1Rel = wahl.R1rel,
                    C1Rel = wahl.C1rel,
                    KapazitaetWirksam_KJM2K = wahl.Kennwerte.C1_Jk / JE_KILO
                };
            }
            catch (GebaeudeModellException ex)
            {
                return ergebnis with { PeriodeGrund = ex.Message };
            }
        }

        /// <summary>J je kJ.</summary>
        private const double JE_KILO = 1000.0;

        /// <summary>
        /// Eine Schicht des Modells als Schicht des Bauteilwegs — oder die benannte Lücke. Eine
        /// Luftschicht ohne λ ist eine ruhende Luftschicht (DIN EN ISO 6946 Tabelle 8); jede
        /// übrige Schicht braucht λ, eine Schicht aus Stoff dazu ρ und c_p (Mehrzonenkonzept 3.4).
        /// </summary>
        internal static bool SchichtAusModell(BauteilschichtModel s, int nummer, out Schicht schicht, out string fehlt)
        {
            schicht = default;
            fehlt = null;
            if (s == null || !(s.Dicke > 0) || double.IsInfinity(s.Dicke))
            {
                fehlt = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_DICKE, nummer);
                return false;
            }
            if (s.IstLuftschicht && !s.Lambda.HasValue)
            {
                schicht = Schicht.RuhendeLuft(s.Dicke);
                return true;
            }
            if (!s.Lambda.HasValue)
            {
                fehlt = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_LAMBDA, nummer);
                return false;
            }
            if (!s.IstLuftschicht && (!s.Rho.HasValue || !s.Cp.HasValue))
            {
                fehlt = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_FEHLT, nummer,
                                      !s.Rho.HasValue ? MyResource.Resource.KFLT_SP_RHO : MyResource.Resource.KFLT_SP_CP);
                return false;
            }
            schicht = new Schicht(s.Dicke, s.Lambda.Value, s.Rho ?? double.NaN, s.Cp ?? double.NaN, s.IstLuftschicht);
            return true;
        }

        // =================================================================
        //  Die Eingabeprüfung der Verwaltung (Mehrzonenkonzept 5.3)
        // =================================================================

        /// <summary>
        /// <b>Die Prüfregeln des Aufbaudialogs</b> (Mehrzonenkonzept 5.3) — <c>null</c> = gültig,
        /// sonst die erste verletzte Regel als Meldung. Ohne Datenbank; die Oberfläche ruft sie
        /// genau einmal, im Speicherweg ihrer Leiste, und der Assistent über denselben Haken.
        ///
        /// <para>Über <see cref="Pruefen"/> hinaus: <b>mindestens eine Schicht</b>; <b>Dicke
        /// 0,001 … 1,0 m</b>, eine ruhende Luftschicht höchstens 0,3 m (DIN EN ISO 6946, 6.9.1);
        /// jede Schicht mit <b>λ</b> (außer der ruhenden Luftschicht), eine Stoffschicht auch mit
        /// <b>ρ und c_p</b> — der Bauteilweg braucht sie für die Speichermasse; die
        /// <b>Reihenfolge lückenlos ab 1</b>, wo sie gesetzt ist; kein Schichtaufbau für Fenster
        /// und Vorhangfassade (sie rechnen aus dem U-Wert, VDI 6007 Blatt 1 Gl. (25)/(26)).</para>
        /// </summary>
        public static string EingabePruefen(BauteilaufbauModel m)
        {
            string t = Pruefen(m);
            if (t != null) return t;

            List<BauteilschichtModel> schichten = (m.Schichten ?? new List<BauteilschichtModel>()).Where(s => s != null).ToList();
            if (schichten.Count == 0) return MyResource.Resource.BAUTEIL_MSG_KEINE_SCHICHT;

            if (m.Bauteilart == DbWerte.BAUTEILART_FENSTER || m.Bauteilart == DbWerte.BAUTEILART_VORHANGFASSADE)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G3_TRANSPARENT_SCHICHTEN, m.Bezeichner.Trim());

            for (int i = 0; i < schichten.Count; i++)
            {
                BauteilschichtModel s = schichten[i];
                int nr = i + 1;
                if (s.Reihenfolge > 0 && s.Reihenfolge != nr)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_REIHENFOLGE, nr, s.Reihenfolge);

                if (s.Dicke < GebaeudeFestwerte.SCHICHT_DICKE_MIN_M || s.Dicke > GebaeudeFestwerte.SCHICHT_DICKE_MAX_M)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_DICKE_BAND, nr,
                                         DickeMm(s.Dicke), DickeMm(GebaeudeFestwerte.SCHICHT_DICKE_MIN_M),
                                         DickeMm(GebaeudeFestwerte.SCHICHT_DICKE_MAX_M));

                bool ruhend = s.IstLuftschicht && !s.Lambda.HasValue;
                if (ruhend && s.Dicke > GebaeudeFestwerte.LUFTSCHICHT_DICKE_MAX_M)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_LUFTSCHICHT_DICKE, nr,
                                         DickeMm(s.Dicke), DickeMm(GebaeudeFestwerte.LUFTSCHICHT_DICKE_MAX_M));

                if (!SchichtAusModell(s, nr, out _, out string fehlt)) return fehlt;
            }
            return null;
        }

        // =================================================================
        //  Duplizieren
        // =================================================================

        /// <summary>
        /// <b>„Duplizieren…"</b>: kopiert den Katalogaufbau <paramref name="id"/> samt Schichten als
        /// EIGENEN Aufbau unter <paramref name="neuerName"/> — <c>ReadOnly = 0</c>, Herkunft
        /// <see cref="DbWerte.HERKUNFT_MANUELL"/>, ohne Quellkennung; Beschreibung, Bauteilart,
        /// Quelle und alle Schichtwerte wie im Original (Entscheid AD-Q11: Auslieferungssätze werden
        /// nie überschrieben). Über den Aggregatweg <see cref="KatalogSpeichern"/>, also in EINER
        /// Transaktion und mit einer Id aus der Folge der Tabelle.
        /// </summary>
        public Ergebnis KatalogDuplizieren(int id, string neuerName)
        {
            BauteilaufbauModel alt = LesenKatalogsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            BauteilaufbauModel neu = alt.Kopie();
            neu.ID = 0;
            neu.Bezeichner = neuerName ?? "";
            neu.Herkunft = DbWerte.HERKUNFT_MANUELL;
            neu.Quellkennung = null;
            neu.ReadOnly = false;
            foreach (BauteilschichtModel s in neu.Schichten) { s.ID = 0; s.ID_Aufbau = 0; }
            return KatalogSpeichern(neu);
        }

        // =================================================================
        //  Prüfen
        // =================================================================

        /// <summary>
        /// Die Prüfung eines Aufbaus samt Schichten vor dem Schreiben, ohne Datenbank — <c>null</c>
        /// = gültig. Name Pflicht, Längen nach Schema, Bauteilart und Herkunft aus ihren Listen,
        /// jede Schicht mit Dicke größer null und Stoffwerten — wenn angegeben — im Band.
        /// </summary>
        public static string Pruefen(BauteilaufbauModel m)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.Bezeichner)) return MyResource.Resource.BAUTEIL_MSG_AUFBAU_NAME_LEER;
            string t = BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_NAME, m.Bezeichner, BaustoffSchema.LAENGE_BEZEICHNER)
                       ?? BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_BESCHREIBUNG, m.Beschreibung, BauteilaufbauSchema.LAENGE_BESCHREIBUNG)
                       ?? BaustoffCtrl.Laenge(MyResource.Resource.KFLT_SP_QUELLE, m.Quelle, BaustoffSchema.LAENGE_QUELLE)
                       ?? BaustoffCtrl.Laenge(MyResource.Resource.BAUTEIL_FELD_QUELLKENNUNG, m.Quellkennung, BaustoffSchema.LAENGE_QUELLKENNUNG)
                       ?? BaustoffCtrl.HerkunftPruefen(m.Herkunft)
                       ?? BauteilartPruefen(m.Bauteilart, true);
            if (t != null) return t;

            int nr = 0;
            foreach (BauteilschichtModel s in m.Schichten ?? new List<BauteilschichtModel>())
            {
                nr++;
                if (s == null) continue;
                if (!(s.Dicke > 0) || double.IsInfinity(s.Dicke))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_DICKE, nr);
                string w = BaustoffCtrl.StoffwertePruefen(s.Lambda, s.Rho, s.Cp);
                if (w != null) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_WERT, nr, w);
            }
            return null;
        }

        /// <summary>Die Bauteilart ist einer der neun Werte — oder NULL, wo NULL zulässig ist.</summary>
        internal static string BauteilartPruefen(string art, bool nullErlaubt)
        {
            if ((art == null && nullErlaubt) || (art != null && DbWerte.BAUTEILARTEN.Contains(art))) return null;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_BAUTEILART, art ?? "");
        }

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>
        /// Speichert einen KATALOGaufbau samt Schichten in EINER Transaktion — Id ≤ 0 legt an
        /// (<c>ReadOnly = 0</c>, ohne Herkunft MANUELL), sonst wird geändert. Ein Aufbau der
        /// Auslieferung wird nicht geändert.
        /// </summary>
        public Ergebnis KatalogSpeichern(BauteilaufbauModel m) => Speichern(Seite.Katalog, null, m);

        /// <summary>Speichert einen PROJEKTaufbau samt Schichten in EINER Transaktion.</summary>
        public Ergebnis ProjektSpeichern(int idProjekt, BauteilaufbauModel m) => Speichern(Seite.Projekt, idProjekt, m);

        /// <summary>Löscht einen Katalogaufbau samt Schichten (Kaskade) — nie einen der Auslieferung.</summary>
        public Ergebnis KatalogLoeschen(int id)
        {
            BauteilaufbauModel alt = LesenKatalogsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            if (alt.ReadOnly) return Ergebnis.Fehler(Schreibgeschuetzt(alt.Bezeichner));
            int n = DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return n == 1 ? Ergebnis.Gut(id) : Ergebnis.Fehler(NichtGefunden(id));
        }

        /// <summary>
        /// Löscht einen Projektaufbau samt Schichten — nie einen, auf den ein Bauteil zeigt (der
        /// Fremdschlüssel ist restriktiv; die Prüfung nennt die Zahl vorher).
        /// </summary>
        public Ergebnis ProjektLoeschen(int id)
        {
            BauteilaufbauModel alt = LesenProjektsatz(id);
            if (alt == null) return Ergebnis.Fehler(NichtGefunden(id));
            long benutzt = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_BAUTEIL + "\" WHERE \"ID_Aufbau\" = ?", new DbParam("@id", id)),
                CultureInfo.InvariantCulture);
            if (benutzt > 0)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_VERWENDET,
                                                     alt.Bezeichner, benutzt));
            int n = DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return n == 1 ? Ergebnis.Gut(id) : Ergebnis.Fehler(NichtGefunden(id));
        }

        /// <summary>
        /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> — nur das Auslieferungskennzeichen der
        /// Katalogaufbauten; die Schichten folgen ihrem Aufbau (L1).
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt)
            => Auslieferungskennzeichen.SetzenInTabelle(BauteilaufbauSchema.TAB_AUFBAU_STAMM, ids, gesperrt);

        // =================================================================
        //  Katalog → Projekt
        // =================================================================

        /// <summary>
        /// Kopiert einen Katalogaufbau samt Schichten in das Projekt (Softwarearchitektur 2.6) —
        /// sofern das Projekt keinen Aufbau dieses Namens führt. <b>Die Stoffe der Schichten reisen
        /// mit</b>: Jeder Katalogstoff wird über <see cref="BaustoffCtrl"/> in das Projekt kopiert
        /// (oder die vorhandene Kopie genommen), und <c>ID_Baustoff</c> der Schicht zeigt danach auf
        /// die Projektkopie (W11). Aufbau, Stoffe und Schichten in EINER Transaktion; die Kopie
        /// trägt die Herkunft <see cref="DbWerte.HERKUNFT_KATALOG"/>, alle übrigen Werte
        /// NULL-erhaltend über <see cref="BauteilaufbauSchema.Fachspalten"/>.
        /// </summary>
        /// <returns>Die Id des kopierten ODER vorhandenen Projektaufbaus, <c>-1</c> bei Fehler.</returns>
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            if (idProjekt <= 0) return -1;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    DataTable dt = v.Lese("SELECT * FROM \"" + BauteilaufbauSchema.TAB_AUFBAU_STAMM + "\" WHERE \"ID\" = ?",
                                          new DbParam("@id", stammId));
                    if (dt == null || dt.Rows.Count == 0) { v.Rollback(); return -1; }
                    DataRow s = dt.Rows[0];
                    string bezeichner = Convert.ToString(s["Bezeichner"], CultureInfo.InvariantCulture);

                    object vorhanden = v.Skalar(
                        "SELECT \"ID\" FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID_Projekt\" = ? AND \"Bezeichner\" = ? " +
                        "ORDER BY \"ID\" LIMIT 1", new DbParam("@p", idProjekt), new DbParam("@b", bezeichner));
                    if (vorhanden != null)
                    {
                        v.Rollback();
                        return Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
                    }

                    var ps = new List<DbParam> { new DbParam("@p", idProjekt), new DbParam("@b", bezeichner) };
                    foreach (string spalte in BauteilaufbauSchema.Fachspalten)
                        ps.Add(spalte == BauteilaufbauSchema.SPALTE_HERKUNFT
                            ? new DbParam("@herkunft", DbWerte.HERKUNFT_KATALOG)
                            : new DbParam("@" + spalte, s.Table.Columns.Contains(spalte) ? s[spalte] : DBNull.Value));
                    int neu = v.EinfuegenUndId(
                        "INSERT INTO \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" (\"ID_Projekt\", \"Bezeichner\", " +
                        string.Join(", ", BauteilaufbauSchema.Fachspalten.Select(x => "\"" + x + "\"")) + ") VALUES (?, ?, " +
                        BaustoffCtrl.Fragezeichen(BauteilaufbauSchema.Fachspalten.Count) + ")", ps.ToArray());

                    DataTable schichten = v.Lese(
                        "SELECT " + SCHICHTSPALTEN_SQL + " FROM \"" + BauteilaufbauSchema.TAB_SCHICHT_STAMM + "\" " +
                        "WHERE \"ID_Aufbau\" = ? ORDER BY \"Reihenfolge\", \"ID\"", new DbParam("@a", stammId));
                    var stoffe = new Dictionary<int, int>();
                    int rang = 0;
                    foreach (DataRow r in schichten.Rows)
                    {
                        BauteilschichtModel sch = SchichtAus(r);
                        if (sch.ID_Baustoff.HasValue)
                        {
                            if (!stoffe.TryGetValue(sch.ID_Baustoff.Value, out int projektStoff))
                            {
                                projektStoff = BaustoffCtrl.CopyFromStamm(v, sch.ID_Baustoff.Value, idProjekt);
                                if (projektStoff <= 0) { v.Rollback(); return -1; }
                                stoffe[sch.ID_Baustoff.Value] = projektStoff;
                            }
                            sch.ID_Baustoff = projektStoff;
                        }
                        SchichtEinfuegen(v, BauteilaufbauSchema.TAB_SCHICHT, neu, ++rang, sch);
                    }

                    v.Commit();
                    return neu;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Aufbau " + stammId + " nicht in das Projekt " + idProjekt + " kopiert: " + ex.Message);
                return -1;
            }
        }

        // =================================================================
        //  intern
        // =================================================================

        private enum Seite { Katalog, Projekt }

        private static string AufbauTabelle(Seite s) => s == Seite.Katalog ? BauteilaufbauSchema.TAB_AUFBAU_STAMM : BauteilaufbauSchema.TAB_AUFBAU;
        private static string SchichtTabelle(Seite s) => s == Seite.Katalog ? BauteilaufbauSchema.TAB_SCHICHT_STAMM : BauteilaufbauSchema.TAB_SCHICHT;
        private static string StoffTabelle(Seite s) => s == Seite.Katalog ? BaustoffSchema.TAB_STAMM : BaustoffSchema.TAB_PROJEKT;

        private Ergebnis Speichern(Seite seite, int? idProjekt, BauteilaufbauModel m)
        {
            if (m != null) m.Schichten ??= new List<BauteilschichtModel>();
            string fehler = Pruefen(m);
            if (fehler != null) return Ergebnis.Fehler(fehler);
            string tab = AufbauTabelle(seite);
            string name = m.Bezeichner.Trim();

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    bool neu = m.ID <= 0;
                    if (!neu)
                    {
                        DataTable alt = v.Lese("SELECT * FROM \"" + tab + "\" WHERE \"ID\" = ?", new DbParam("@id", m.ID));
                        if (alt.Rows.Count == 0) { v.Rollback(); return Ergebnis.Fehler(NichtGefunden(m.ID)); }
                        DataRow a = alt.Rows[0];
                        if (seite == Seite.Katalog && Convert.ToInt64(a["ReadOnly"], CultureInfo.InvariantCulture) != 0)
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(Schreibgeschuetzt(Convert.ToString(a["Bezeichner"], CultureInfo.InvariantCulture)));
                        }
                        if (seite == Seite.Projekt) idProjekt = Convert.ToInt32(a["ID_Projekt"], CultureInfo.InvariantCulture);
                    }

                    // Der Name ist auf seiner Seite frei (Katalog als Ganzes, Kopie je Projekt).
                    var np = new List<DbParam> { new DbParam("@b", name), new DbParam("@id", neu ? 0 : m.ID) };
                    string nsql = "SELECT COUNT(*) FROM \"" + tab + "\" WHERE \"Bezeichner\" = ? AND \"ID\" <> ?";
                    if (seite == Seite.Projekt) { nsql += " AND \"ID_Projekt\" = ?"; np.Add(new DbParam("@p", idProjekt ?? 0)); }
                    if (Convert.ToInt64(v.Skalar(nsql, np.ToArray()), CultureInfo.InvariantCulture) > 0)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NAME_VERGEBEN, name));
                    }

                    // Die Stoffe der Schichten gehören zur eigenen Ablage (W11); fehlende Werte kommen aus ihnen.
                    int nr = 0;
                    foreach (BauteilschichtModel s in m.Schichten.Where(x => x != null))
                    {
                        nr++;
                        if (!s.ID_Baustoff.HasValue) continue;
                        string ssql = "SELECT * FROM \"" + StoffTabelle(seite) + "\" WHERE \"ID\" = ?";
                        var sp = new List<DbParam> { new DbParam("@s", s.ID_Baustoff.Value) };
                        if (seite == Seite.Projekt) { ssql += " AND \"ID_Projekt\" = ?"; sp.Add(new DbParam("@p", idProjekt ?? 0)); }
                        DataTable st = v.Lese(ssql, sp.ToArray());
                        if (st.Rows.Count == 0)
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_SCHICHT_BAUSTOFF,
                                                                 nr, s.ID_Baustoff.Value));
                        }
                        s.Lambda ??= BaustoffCtrl.ZahlAus(st.Rows[0], "Lambda");
                        s.Rho ??= BaustoffCtrl.ZahlAus(st.Rows[0], "Rho");
                        s.Cp ??= BaustoffCtrl.ZahlAus(st.Rows[0], "cp");
                    }

                    var kp = new List<DbParam>();
                    if (neu && seite == Seite.Projekt) kp.Add(new DbParam("@p", idProjekt ?? 0));
                    kp.Add(new DbParam("@b", name));
                    kp.Add(BaustoffCtrl.Text("@be", m.Beschreibung));
                    kp.Add(BaustoffCtrl.Text("@art", m.Bauteilart));
                    kp.Add(BaustoffCtrl.Text("@q", m.Quelle));
                    kp.Add(BaustoffCtrl.Text("@h", m.Herkunft ?? (neu ? DbWerte.HERKUNFT_MANUELL : null)));
                    kp.Add(BaustoffCtrl.Text("@qk", m.Quellkennung));

                    if (neu)
                    {
                        m.ID = v.EinfuegenUndId(
                            "INSERT INTO \"" + tab + "\" (" + (seite == Seite.Projekt ? "\"ID_Projekt\", " : "") +
                            "\"Bezeichner\", \"Beschreibung\", \"Bauteilart\", \"Quelle\", \"Herkunft\", \"Quellkennung\"" +
                            (seite == Seite.Katalog ? ", \"ReadOnly\"" : "") + ") VALUES (" +
                            (seite == Seite.Projekt ? "?, " : "") + "?, ?, ?, ?, ?, ?" + (seite == Seite.Katalog ? ", 0" : "") + ")",
                            kp.ToArray());
                        if (m.Herkunft == null) m.Herkunft = DbWerte.HERKUNFT_MANUELL;
                    }
                    else
                    {
                        kp.Add(new DbParam("@id", m.ID));
                        v.Ausfuehren("UPDATE \"" + tab + "\" SET \"Bezeichner\" = ?, \"Beschreibung\" = ?, \"Bauteilart\" = ?, " +
                                     "\"Quelle\" = ?, \"Herkunft\" = ?, \"Quellkennung\" = ? WHERE \"ID\" = ?", kp.ToArray());
                        v.Ausfuehren("DELETE FROM \"" + SchichtTabelle(seite) + "\" WHERE \"ID_Aufbau\" = ?", new DbParam("@a", m.ID));
                    }
                    if (seite == Seite.Projekt) m.ID_Projekt = idProjekt;

                    int rang = 0;
                    foreach (BauteilschichtModel s in m.Schichten.Where(x => x != null))
                        s.ID = SchichtEinfuegen(v, SchichtTabelle(seite), m.ID, ++rang, s);
                    m.Schichten = m.Schichten.Where(x => x != null).ToList();

                    v.Commit();
                    return Ergebnis.Gut(m.ID);
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NICHT_GESPEICHERT, ex.Message));
            }
        }

        /// <summary>
        /// <b>Legt einen PROJEKTaufbau samt Schichten im Vorgang des Aufrufers an</b> — der Weg des
        /// Bauteilvorschlags eines Imports (Stufe G4b, <c>GebaeudeZonenCtrl.VorschlagSchreiben</c>):
        /// Aufbau, Zone und Bauteile entstehen in EINEM Vorgang. Ohne Namensprüfung und ohne
        /// Baustoffabgleich — der Aufrufer vergibt einen im Projekt freien Namen
        /// (<see cref="FreierName"/>) und hat den Aufbau geprüft (<see cref="Pruefen"/>); die Stoffwerte
        /// der Schichten sind Kopien, <c>ID_Baustoff</c> bleibt, wie er ist. Setzt Id, Projekt und
        /// Reihenfolge am Modell und liefert die neue Id.
        /// </summary>
        internal static int ProjektaufbauEinfuegen(DbVorgang v, int idProjekt, BauteilaufbauModel m)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (m == null) throw new ArgumentNullException(nameof(m));
            int id = v.EinfuegenUndId(
                "INSERT INTO \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" (\"ID_Projekt\", \"Bezeichner\", \"Beschreibung\", " +
                "\"Bauteilart\", \"Quelle\", \"Herkunft\", \"Quellkennung\") VALUES (?, ?, ?, ?, ?, ?, ?)",
                new[]
                {
                    new DbParam("@p", idProjekt),
                    new DbParam("@b", m.Bezeichner.Trim()),
                    BaustoffCtrl.Text("@be", m.Beschreibung),
                    BaustoffCtrl.Text("@art", m.Bauteilart),
                    BaustoffCtrl.Text("@q", m.Quelle),
                    BaustoffCtrl.Text("@h", m.Herkunft),
                    BaustoffCtrl.Text("@qk", m.Quellkennung)
                });
            m.ID = id;
            m.ID_Projekt = idProjekt;
            m.Bezeichner = m.Bezeichner.Trim();
            m.Schichten = (m.Schichten ?? new List<BauteilschichtModel>()).Where(x => x != null).ToList();
            int rang = 0;
            foreach (BauteilschichtModel s in m.Schichten)
                s.ID = SchichtEinfuegen(v, BauteilaufbauSchema.TAB_SCHICHT, id, ++rang, s);
            return id;
        }

        /// <summary>
        /// Ein Aufbauname, der unter <paramref name="vergeben"/> noch frei ist — sonst mit „ (2)",
        /// „ (3)" … ergänzt, höchstens <see cref="BaustoffSchema.LAENGE_BEZEICHNER"/> Zeichen. Der
        /// gewählte Name wird in die Menge aufgenommen. Der Vergleich ist ordinal, wie die Namensprobe
        /// von <see cref="ProjektSpeichern"/> in SQL.
        /// </summary>
        internal static string FreierName(ISet<string> vergeben, string name)
        {
            if (vergeben == null) throw new ArgumentNullException(nameof(vergeben));
            string basis = Laengenrecht((name ?? "").Trim(), BaustoffSchema.LAENGE_BEZEICHNER);
            string kandidat = basis;
            for (int n = 2; !vergeben.Add(kandidat); n++)
            {
                string zusatz = " (" + n.ToString(CultureInfo.InvariantCulture) + ")";
                kandidat = Laengenrecht(basis, BaustoffSchema.LAENGE_BEZEICHNER - zusatz.Length) + zusatz;
            }
            return kandidat;
        }

        private static string Laengenrecht(string text, int laenge)
        {
            if (text.Length <= laenge) return text;
            int n = char.IsHighSurrogate(text[laenge - 1]) ? laenge - 1 : laenge;
            return text.Substring(0, n);
        }

        /// <summary>Legt eine Schicht an; setzt Aufbau und Reihenfolge am Modell und liefert die neue Id.</summary>
        private static int SchichtEinfuegen(DbVorgang v, string tabelle, int idAufbau, int reihenfolge, BauteilschichtModel s)
        {
            s.ID_Aufbau = idAufbau;
            s.Reihenfolge = reihenfolge;
            return v.EinfuegenUndId(
                "INSERT INTO \"" + tabelle + "\" (\"ID_Aufbau\", \"Reihenfolge\", \"ID_Baustoff\", \"Dicke\", \"IstLuftschicht\", " +
                "\"Lambda\", \"Rho\", \"cp\") VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                new[]
                {
                    new DbParam("@a", idAufbau),
                    new DbParam("@r", reihenfolge),
                    new DbParam("@s", DbParamTyp.Integer) { Wert = s.ID_Baustoff.HasValue ? (object)s.ID_Baustoff.Value : DBNull.Value },
                    new DbParam("@d", DbParamTyp.Double) { Wert = s.Dicke },
                    new DbParam("@l", s.IstLuftschicht ? 1 : 0),
                    BaustoffCtrl.Zahl("@la", s.Lambda),
                    BaustoffCtrl.Zahl("@rh", s.Rho),
                    BaustoffCtrl.Zahl("@cp", s.Cp)
                });
        }

        private BauteilaufbauModel LesenEinen(Seite seite, int id)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT * FROM \"" + AufbauTabelle(seite) + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            DataTable schichten = DataRepository.GetDataTable(
                "SELECT " + SCHICHTSPALTEN_SQL + " FROM \"" + SchichtTabelle(seite) + "\" WHERE \"ID_Aufbau\" = ? " +
                "ORDER BY \"Reihenfolge\", \"ID\"", new DbParam("@id", id));
            return Zusammenfuehren(kopf, schichten).FirstOrDefault();
        }

        /// <summary>Köpfe und Schichten zusammenführen — im Speicher, über die gelesenen Ids.</summary>
        private static List<BauteilaufbauModel> Zusammenfuehren(DataTable kopf, DataTable schichten)
        {
            var liste = new List<BauteilaufbauModel>();
            if (kopf == null) return liste;
            var jeId = new Dictionary<int, BauteilaufbauModel>();
            foreach (DataRow r in kopf.Rows)
            {
                var a = new BauteilaufbauModel
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    ID_Projekt = BaustoffCtrl.GanzAus(r, "ID_Projekt"),
                    Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                    Beschreibung = BaustoffCtrl.TextAus(r, "Beschreibung"),
                    Bauteilart = BaustoffCtrl.TextAus(r, "Bauteilart"),
                    Quelle = BaustoffCtrl.TextAus(r, "Quelle"),
                    Herkunft = BaustoffCtrl.TextAus(r, "Herkunft"),
                    Quellkennung = BaustoffCtrl.TextAus(r, "Quellkennung"),
                    ReadOnly = (BaustoffCtrl.GanzAus(r, "ReadOnly") ?? 0) != 0
                };
                liste.Add(a);
                jeId[a.ID] = a;
            }
            if (schichten != null)
                foreach (DataRow r in schichten.Rows)
                {
                    BauteilschichtModel s = SchichtAus(r);
                    if (jeId.TryGetValue(s.ID_Aufbau, out BauteilaufbauModel a)) a.Schichten.Add(s);
                }
            return liste;
        }

        private static BauteilschichtModel SchichtAus(DataRow r)
        {
            return new BauteilschichtModel
            {
                ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                ID_Aufbau = Convert.ToInt32(r["ID_Aufbau"], CultureInfo.InvariantCulture),
                Reihenfolge = Convert.ToInt32(r["Reihenfolge"], CultureInfo.InvariantCulture),
                ID_Baustoff = BaustoffCtrl.GanzAus(r, "ID_Baustoff"),
                Dicke = Convert.ToDouble(r["Dicke"], CultureInfo.InvariantCulture),
                IstLuftschicht = (BaustoffCtrl.GanzAus(r, "IstLuftschicht") ?? 0) != 0,
                Lambda = BaustoffCtrl.ZahlAus(r, "Lambda"),
                Rho = BaustoffCtrl.ZahlAus(r, "Rho"),
                Cp = BaustoffCtrl.ZahlAus(r, "cp")
            };
        }

        private static string Praefix(string alias, string spalten)
            => string.Join(", ", spalten.Split(new[] { ", " }, StringSplitOptions.None).Select(s => alias + "." + s));

        private static string NichtGefunden(int id)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NICHT_GEFUNDEN, id);

        private static string Schreibgeschuetzt(string name)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUTEIL_MSG_AUFBAU_SCHREIBGESCHUETZT, name);
    }
}
