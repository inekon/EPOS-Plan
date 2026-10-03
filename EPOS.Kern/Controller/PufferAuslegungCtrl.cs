using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER CONTROLLER DER PUFFERSPEICHER-AUSLEGUNG (Konzept Pufferspeicher-Auslegung, Abschnitt
    // 3.1, 3.5, 4 und 5). Er liest Projekt, Puffer, Kaskade, Erzeuger, Gebaeude, Einstellungen,
    // Zapfprofil, Prozesswaerme und Konditionierung, baut daraus den Eingang des Rechenkerns
    // (PufferAuslegungEingang) samt Herkunft jedes Werts, holt die drei Bedarfsreihen ueber
    // denselben Weg wie der Lauf (Vorpruefen + Bedarf + KanaeleDrei, kein Lauf), speichert die
    // Eingaben in Tab_PufferAuslegung und uebernimmt eine Empfehlung ueber PufferSpCtrl in den
    // Projektpuffer. Alle Zugriffe ueber DataRepository mit ?-Parametern.
    // ====================================================================================

    /// <summary>Woher ein Wert der Vorbelegung stammt (Herkunftsliste, Konzept 5).</summary>
    public static class PufferHerkunftsquelle
    {
        public const string PROJEKT = "Projekt";
        public const string PUFFER = "Puffer";
        public const string KASKADE = "Kaskade";
        public const string GEBAEUDE = "Gebäude";
        public const string ZAPFPROFIL = "Zapfprofil";
        public const string REIHE = "Bedarfsreihe";
        public const string KATALOG = "Katalog";
        public const string PARAMETER = "Vorgabetabelle";
        public const string VORGABE = "Vorgabe";
        public const string GESPEICHERT = "Gespeicherte Auslegung";
    }

    /// <summary>Eine Zeile der Herkunftsliste: Feld des Eingangs, Quelle und Klartext.</summary>
    public sealed record PufferAuslegungHerkunft(string Feld, string Quelle, string Text);

    /// <summary>
    /// Die drei Bedarfsreihen des Projekts [kWh/h, 8 760] aus <c>KanaeleDrei()</c> — oder der
    /// Fehlertext, warum sie nicht gebildet werden konnten.
    /// </summary>
    public sealed record PufferAuslegungReihen(double[] Heizung, double[] Brauchwasser, double[] Prozess, string Fehlertext)
    {
        /// <summary>Liegen die Reihen vor?</summary>
        public bool Vorhanden => Fehlertext == null && Heizung != null;
    }

    /// <summary>
    /// Die Vorbelegung einer Auslegung: der Eingang des Rechenkerns, die Herkunft jedes Werts, die
    /// Ableitung des Nutzungsprofils und ob eine gespeicherte Zeile ihn überschrieben hat.
    /// </summary>
    public sealed record PufferAuslegungVorbelegung
    {
        public int IdProjekt { get; init; }
        /// <summary>Der Projektpuffer; <c>null</c> = „neu anlegen“.</summary>
        public int? IdPuffer { get; init; }
        public PufferAuslegungEingang Eingang { get; init; } = new PufferAuslegungEingang();
        public IReadOnlyList<PufferAuslegungHerkunft> Herkunft { get; init; } = Array.Empty<PufferAuslegungHerkunft>();
        public PufferNutzungsprofilAbleitung Nutzungsprofil { get; init; }
        /// <summary>Hat eine Zeile aus <c>Tab_PufferAuslegung</c> die Vorbelegung überschrieben?</summary>
        public bool Gespeichert { get; init; }
        /// <summary>Die ID der gespeicherten Zeile; <c>null</c> = keine.</summary>
        public int? IdZeile { get; init; }

        /// <summary>Die Quelle des Felds (letzter Eintrag gewinnt); <c>null</c>, wenn das Feld keinen trägt.</summary>
        public string Quelle(string feld)
        {
            string q = null;
            foreach (PufferAuslegungHerkunft h in Herkunft) if (h.Feld == feld) q = h.Quelle;
            return q;
        }
    }

    /// <summary>Eine Vorlage mit ihren Kriterienschaltern und Beispielwerten aus der Vorgabetabelle.</summary>
    public sealed record PufferVorlagenbeschreibung(PufferVorlage Vorlage, IReadOnlyDictionary<string, bool> Kriterien,
                                                    double MindestlaufzeitMin, double StartzielJeTag,
                                                    double BeispielLeistungKw, double Faustwert, string FaustwertEinheit);

    /// <summary>Der Controller der Pufferspeicher-Auslegung (Konzept 5).</summary>
    public static class PufferAuslegungCtrl
    {
        // ---- die Typen der Anlagenzeilen ----
        private const int TYP_WP = ProjektPuffer.TYP_WP;
        private const int TYP_SOLAR = ProjektPuffer.TYP_SOLARTHERMIE;
        private const int TYP_KESSEL = ProjektPuffer.TYP_KESSEL;
        private const int TYP_BHKW = ProjektPuffer.TYP_BHKW;

        /// <summary>Die Rangfolge, wenn die Kaskade keinen vorhandenen Erzeuger nennt.</summary>
        private static readonly int[] RANG_RUECKFALL = { TYP_WP, TYP_BHKW, TYP_KESSEL, TYP_SOLAR };

        // ---- SQL (alle mit Parametern, nie zusammengesetzt) ----
        internal const string SQL_PUFFER =
            "SELECT * FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ?";
        internal const string SQL_ANLAGEN =
            "SELECT ID, ID_Type, ID_WP, ID_Kessel, ID_BHKW, ID_Solar, Heizstab, Bivalenter_Betrieb, Sperrung, " +
            "Sperrzeit_von, Sperrzeit_bis, Kollektormodulanzahl FROM Tab_Energieanlagen " +
            "WHERE ID_Projekt = ? AND ID_Type IN (1, 2, 10, 11) ORDER BY ID";
        internal const string SQL_EINSTELLUNGEN =
            "SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ? ORDER BY ID";
        internal const string SQL_WP = "SELECT * FROM Tab_WP WHERE ID = ?";
        internal const string SQL_KESSEL = "SELECT * FROM Tab_Heizkessel WHERE ID = ?";
        internal const string SQL_BHKW = "SELECT * FROM Tab_BHKW WHERE ID = ?";
        internal const string SQL_SOLAR = "SELECT * FROM Tab_Solarkollektoren WHERE ID = ?";
        internal const string SQL_BRENNSTOFF = "SELECT Bezeichner FROM Tab_Brennstoff_Stamm WHERE ID = ?";
        internal const string SQL_UEBERGABE =
            "SELECT Uebergabe_Art, COUNT(*) AS Anzahl FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Uebergabe_Art IS NOT NULL " +
            "GROUP BY Uebergabe_Art ORDER BY COUNT(*) DESC, Uebergabe_Art";
        internal const string SQL_GEBAEUDE_ANZAHL = "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?";
        internal const string SQL_PROZESS =
            "SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?";
        internal const string SQL_ZAPFNUTZUNGEN =
            "SELECT n.Bezeichner FROM " + TwwSchema.TAB_TWW_ZONE + " z JOIN " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
            " n ON n.ID = z.ID_Nutzungsart WHERE z.ID_Projekt = ? ORDER BY z.Reihenfolge, z.ID";
        internal const string SQL_KONDITIONIERUNG =
            "SELECT k.Groesse, k.Bemerkung FROM " + KonditionierungSchema.TAB_KALENDER + " k JOIN Tab_Gebaeude g " +
            "ON g.ID = k.ID_Gebaeude WHERE g.ID_Projekt = ? AND k.Bemerkung IS NOT NULL ORDER BY g.ID, k.ID";
        internal const string SQL_KONDITIONIERUNG_NUTZUNG =
            "SELECT Nutzung FROM " + KonditionierungVorlagenSchema.TAB_VORLAGE + " WHERE Bezeichner = ? AND Groesse = ?";
        internal const string SQL_KATALOG =
            "SELECT ID, Bezeichner, Speichertyp, Gesamtvolumen, Bereitschaftsverluste FROM Tab_Pufferspeicher_STAMM " +
            "WHERE Gesamtvolumen > 0 ORDER BY Gesamtvolumen, ID";

        internal const string SQL_ZEILE_LESEN =
            "SELECT * FROM " + PufferAuslegungSchema.TAB + " WHERE ID_Projekt = ? AND ID_Pufferspeicher IS ? ORDER BY ID";
        internal const string SQL_ZEILE_EINFUEGEN =
            "INSERT INTO " + PufferAuslegungSchema.TAB + " (ID_Projekt, ID_Pufferspeicher, Klasse_Heizung, " +
            "Klasse_Brauchwasser, Klasse_Prozess, Nutzungsprofil, Vorlage, WP_Geregelt, Zweiterzeuger_Frei, " +
            "Mindestlaufzeit_min, Mindestleistung_kW, Anlagenvolumen_l, Sperrprofil, Sperrdauer_h, Sperrbeginn_h, " +
            "Startziel_je_Tag, Deckungsziel, DeltaT_B_K, T_Puffer_Oben_C, Zirkulation_Weg, BHKW_Verschiebedauer_h, " +
            "Volumen_H_l, Volumen_B_l, Volumen_P_l, Volumen_Empfehlung_l, Bemessend, Berechnet_am) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
        internal const string SQL_ZEILE_AENDERN =
            "UPDATE " + PufferAuslegungSchema.TAB + " SET ID_Projekt = ?, ID_Pufferspeicher = ?, Klasse_Heizung = ?, " +
            "Klasse_Brauchwasser = ?, Klasse_Prozess = ?, Nutzungsprofil = ?, Vorlage = ?, WP_Geregelt = ?, " +
            "Zweiterzeuger_Frei = ?, Mindestlaufzeit_min = ?, Mindestleistung_kW = ?, Anlagenvolumen_l = ?, " +
            "Sperrprofil = ?, Sperrdauer_h = ?, Sperrbeginn_h = ?, Startziel_je_Tag = ?, Deckungsziel = ?, " +
            "DeltaT_B_K = ?, T_Puffer_Oben_C = ?, Zirkulation_Weg = ?, BHKW_Verschiebedauer_h = ?, Volumen_H_l = ?, " +
            "Volumen_B_l = ?, Volumen_P_l = ?, Volumen_Empfehlung_l = ?, Bemessend = ?, Berechnet_am = ? WHERE ID = ?";
        internal const string SQL_ZEILE_UMHAENGEN =
            "UPDATE " + PufferAuslegungSchema.TAB + " SET ID_Pufferspeicher = ? WHERE ID_Projekt = ? AND ID_Pufferspeicher IS NULL";
        internal const string SQL_PUFFER_STAMMWERTE =
            "SELECT Bezeichner, Hersteller, Speichertyp, Investitionskosten, Vorlauf, Ruecklauf, Schwelle_Ein, " +
            "Schwelle_Aus, Schwelle_Aus_Nachrang, Entladeprio, Schwelle_Reserve FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ?";

        // =================================================================================
        //  Vorbelegen
        // =================================================================================

        /// <summary>
        /// Die Vorbelegung des Eingangs für einen Projektpuffer (<paramref name="idPuffer"/>) oder einen
        /// neuen Puffer (<c>null</c>), samt Herkunft jedes Werts. Eine gespeicherte Zeile in
        /// <c>Tab_PufferAuslegung</c> überschreibt die Vorbelegung spaltenweise (NULL = Vorbelegung).
        /// Mit <paramref name="reihen"/> trägt der Eingang die drei Bedarfsreihen; ohne sie bleiben sie
        /// leer (der Aufrufer holt sie mit <see cref="Reihen(int, int)"/>). Ein Puffer, der nicht zum
        /// Projekt gehört, wird benannt abgelehnt (<see cref="ArgumentException"/>).
        /// </summary>
        public static PufferAuslegungVorbelegung Vorbelegen(int idProjekt, int? idPuffer, PufferAuslegungReihen reihen = null)
        {
            PufferAuslegungVorbelegung basis = Grundvorbelegung(idProjekt, idPuffer, reihen, true);
            DataRow zeile = ZeileLesen(idProjekt, idPuffer);
            return zeile == null ? basis : Ueberlagern(basis, zeile);
        }

        /// <summary>Die Vorbelegung ohne gespeicherte Zeile.</summary>
        private static PufferAuslegungVorbelegung Grundvorbelegung(int idProjekt, int? idPuffer, PufferAuslegungReihen reihen,
                                                                   bool mitZapfprofil)
        {
            if (idProjekt <= 0) throw new ArgumentException("Kein Projekt angegeben.", nameof(idProjekt));
            var h = new List<PufferAuslegungHerkunft>();
            void H(string feld, string quelle, string text) => h.Add(new PufferAuslegungHerkunft(feld, quelle, text));

            PufferAuslegungParameter p = PufferAuslegungParameter.Lesen();
            H(nameof(PufferAuslegungEingang.Parameter), PufferHerkunftsquelle.PARAMETER,
              DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB_PARAMETER)
                  ? PufferAuslegungSchema.TAB_PARAMETER + ", fehlende Schlüssel aus den eingebauten Vorgaben"
                  : "eingebaute Vorgaben (Vorgabetabelle fehlt)");

            // ---- Puffer und Klassen-Set ----
            DataRow pz = null;
            if (idPuffer.HasValue)
            {
                pz = ErsteZeile(SQL_PUFFER, P("@id", idPuffer.Value), P("@projekt", idProjekt));
                if (pz == null)
                    throw new ArgumentException("Der Pufferspeicher " + idPuffer.Value + " gehört nicht zum Projekt " + idProjekt + ".",
                                                nameof(idPuffer));
            }
            bool prozessVorhanden = Zahl(DataRepository.ExecuteScalar(SQL_PROZESS, P("@projekt", idProjekt))) > 0;
            bool kH, kB, kP;
            if (pz != null)
            {
                PufferSpCtrl.KlassenSet ks = PufferSpCtrl.KlassenSetAusZeile(pz);
                kH = ks.Heizung; kB = ks.Brauchwasser; kP = ks.Prozess;
                if (!kH && !kB && !kP) kH = true;
                H("Klassen", PufferHerkunftsquelle.PUFFER, "Klassen-Set des Puffers (Nutzung_Heizung/_Brauchwasser/_Prozess)");
            }
            else
            {
                kH = true; kB = false; kP = prozessVorhanden;
                H("Klassen", PufferHerkunftsquelle.VORGABE, "neuer Puffer: Heizung" + (kP ? " und Prozesswärme (Projekt trägt Prozesswärme)" : ""));
            }

            // ---- Temperaturpaar ----
            double vl, rl;
            double? pv = ZahlOderNull(pz, "Vorlauf"), pr = ZahlOderNull(pz, "Ruecklauf");
            if (pv > 0 && pr > 0 && pv > pr)
            {
                vl = pv.Value; rl = pr.Value;
                H(nameof(PufferAuslegungEingang.VorlaufC), PufferHerkunftsquelle.PUFFER, "Temperaturpaar des Puffers");
            }
            else
            {
                int? sv = PufferSpCtrl.SystemVorlauf(idProjekt), sr = PufferSpCtrl.SystemRuecklauf(idProjekt);
                if (sv > 0 && sr > 0 && sv > sr)
                {
                    vl = sv.Value; rl = sr.Value;
                    H(nameof(PufferAuslegungEingang.VorlaufC), PufferHerkunftsquelle.KASKADE, "Systemtemperaturen der Erzeuger (kleinster Vorlauf, größter Rücklauf)");
                }
                else
                {
                    vl = SimulationControl.KESSEL_VORLAUF_RUECKFALL; rl = SimulationControl.KESSEL_RUECKLAUF_RUECKFALL;
                    H(nameof(PufferAuslegungEingang.VorlaufC), PufferHerkunftsquelle.VORGABE, "Rückfall-Temperaturpaar des Rechenkerns");
                }
            }

            // ---- Schwellen (Datenbank in %, Eingang als Anteil) ----
            double? sEin = ZahlOderNull(pz, "Schwelle_Ein"), sAus = ZahlOderNull(pz, "Schwelle_Aus");
            double? schwelleEin = sEin > 0 ? sEin / 100.0 : null;
            double? schwelleAus = sAus > 0 ? sAus / 100.0 : null;
            H(nameof(PufferAuslegungEingang.SchwelleEin), schwelleEin.HasValue ? PufferHerkunftsquelle.PUFFER : PufferHerkunftsquelle.VORGABE,
              schwelleEin.HasValue ? "Einschaltschwelle des Puffers" : "Vorgabe Puffer.Schwelle_Ein");
            H(nameof(PufferAuslegungEingang.SchwelleAus), schwelleAus.HasValue ? PufferHerkunftsquelle.PUFFER : PufferHerkunftsquelle.VORGABE,
              schwelleAus.HasValue ? "Ausschaltschwelle des Puffers" : "Vorgabe Puffer.Schwelle_Aus");

            // ---- Einstellungen ----
            DataRow einst = ErsteZeile(SQL_EINSTELLUNGEN, P("@projekt", idProjekt));
            double? heizgrenze = ZahlOderNull(einst, "Kessel_Heizgrenze");
            H(nameof(PufferAuslegungEingang.HeizgrenzeC), heizgrenze.HasValue ? PufferHerkunftsquelle.PROJEKT : PufferHerkunftsquelle.VORGABE,
              heizgrenze.HasValue ? "Tab_Einstellungen.Kessel_Heizgrenze" : "leer: " + PufferAuslegungVorgaben.Text(SimulationSPK.HEIZGRENZE_VORGABE_C) + " °C");
            heizgrenze ??= SimulationSPK.HEIZGRENZE_VORGABE_C;
            double? zirkKw = ZahlOderNull(einst, "Zirkulation_Leistung_kW");
            double? zirkH = ZahlOderNull(einst, "Zirkulation_Laufzeit_h_d");
            if (zirkKw.HasValue)
                H(nameof(PufferAuslegungEingang.ZirkulationProjektKw), PufferHerkunftsquelle.PROJEKT, "Tab_Einstellungen.Zirkulation_Leistung_kW");

            // ---- Kaskade und Erzeuger ----
            List<Anlage> anlagen = AnlagenLesen(idProjekt);
            int rang1 = Rang1(einst, anlagen, out string rangText);
            H(nameof(PufferAuslegungEingang.Erzeuger), PufferHerkunftsquelle.KASKADE, rangText);
            PufferErzeuger erz = ErzeugerBauen(rang1, anlagen, h, out bool festbrennstoff, out bool bivalent,
                                               out IReadOnlyList<PufferSperrfenster> sperre, out double? mindestlaufzeit);

            // ---- Vorlage ----
            PufferVorlage vorlage;
            string vText;
            if (kP && !kH && !kB) { vorlage = PufferVorlage.PROZESS; vText = "nur Prozesswärme"; }
            else
                switch (rang1)
                {
                    case TYP_SOLAR: vorlage = PufferVorlage.SOLAR; vText = "Solarthermie an Rang 1"; break;
                    case TYP_WP:
                        vorlage = bivalent ? PufferVorlage.WP_BIVALENT : PufferVorlage.WP_MONO;
                        vText = bivalent ? "Wärmepumpe mit Zweiterzeuger (Kessel, Heizstab oder bivalenter Betrieb)" : "Wärmepumpe ohne Zweiterzeuger";
                        break;
                    case TYP_BHKW: vorlage = PufferVorlage.BHKW; vText = "BHKW an Rang 1"; break;
                    case TYP_KESSEL:
                        vorlage = festbrennstoff ? PufferVorlage.FESTBRENNSTOFF : PufferVorlage.KESSEL;
                        vText = festbrennstoff ? "Festbrennstoffkessel an Rang 1" : "Kessel an Rang 1";
                        break;
                    default:
                        vorlage = kP ? PufferVorlage.PROZESS : PufferVorlage.WP_MONO;
                        vText = "kein Wärmeerzeuger im Projekt: Vorgabe";
                        break;
                }
            H(nameof(PufferAuslegungEingang.Vorlage), rang1 == 0 && !(kP && !kH && !kB) ? PufferHerkunftsquelle.VORGABE : PufferHerkunftsquelle.KASKADE, vText);
            if (sperre.Count > 0)
                H(nameof(PufferAuslegungEingang.Sperrfenster), PufferHerkunftsquelle.KASKADE, "Sperrzeit der Anlage an Rang 1 (Sperrzeit_von/_bis)");
            else
                H(nameof(PufferAuslegungEingang.Sperrfenster), PufferHerkunftsquelle.VORGABE, "keine Sperrzeit gepflegt");

            // ---- Übergabeart (häufigste über die Gebäude) ----
            string uebergabe = null;
            DataTable ue = DataRepository.GetDataTable(SQL_UEBERGABE, P("@projekt", idProjekt));
            if (ue != null && ue.Rows.Count > 0) uebergabe = Text(ue.Rows[0], "Uebergabe_Art");
            int gebaeude = (int)Zahl(DataRepository.ExecuteScalar(SQL_GEBAEUDE_ANZAHL, P("@projekt", idProjekt)));
            H(nameof(PufferAuslegungEingang.Uebergabeart), uebergabe != null ? PufferHerkunftsquelle.GEBAEUDE : PufferHerkunftsquelle.VORGABE,
              uebergabe != null
                  ? "häufigste Übergabeart über " + gebaeude + " Gebäude"
                  : "keine Übergabeart gepflegt: ideal, rechnet wie FLAECHE (Hinweis " + PufferWarncode.UEBERGABE_UNBEKANNT + ")");
            H(nameof(PufferAuslegungEingang.AnlagenvolumenL), PufferHerkunftsquelle.VORGABE, "Übergabeart × Auslegungsheizlast (Anlagenvolumen.<Übergabe>_l_kW)");
            H(nameof(PufferAuslegungEingang.AuslegungsheizlastKw), PufferHerkunftsquelle.REIHE, "Maximum der Heizreihe");

            // ---- Zapfprofil ----
            PufferZapfprofil zp = mitZapfprofil ? Zapfprofil(idProjekt, reihen, h) : null;

            // ---- Nenninhalte und Katalog ----
            IReadOnlyList<double> nenn = Nenninhalte(h);
            IReadOnlyList<PufferKatalogsatz> katalog = Katalog();
            H(nameof(PufferAuslegungEingang.Katalog), PufferHerkunftsquelle.KATALOG, katalog.Count + " Katalogpuffer aus Tab_Pufferspeicher_STAMM");

            // ---- Nutzungsprofil ----
            PufferNutzungsprofilAbleitung np = global::WindowsFormsApplication1.Nutzungsprofil.Ableiten(
                Zapfnutzungen(idProjekt), prozessVorhanden, Konditionierungsnutzungen(idProjekt));
            H(nameof(PufferAuslegungEingang.Nutzungsprofil), np.Vorgabe ? PufferHerkunftsquelle.VORGABE : PufferHerkunftsquelle.PROJEKT, np.Herkunft);

            if (reihen != null)
                H("Reihen", PufferHerkunftsquelle.REIHE, reihen.Vorhanden ? "Bedarfsreihen aus KanaeleDrei()" : "keine Reihen: " + reihen.Fehlertext);

            var e = new PufferAuslegungEingang
            {
                KlasseHeizung = kH,
                KlasseBrauchwasser = kB,
                KlasseProzess = kP,
                Vorlage = vorlage,
                Nutzungsprofil = np.Profil,
                Erzeuger = erz,
                VorlaufC = vl,
                RuecklaufC = rl,
                SchwelleEin = schwelleEin,
                SchwelleAus = schwelleAus,
                ReiheHeizung = reihen?.Vorhanden == true ? reihen.Heizung : Array.Empty<double>(),
                ReiheBrauchwasser = reihen?.Vorhanden == true ? reihen.Brauchwasser : Array.Empty<double>(),
                ReiheProzess = reihen?.Vorhanden == true ? reihen.Prozess : Array.Empty<double>(),
                Uebergabeart = uebergabe,
                HeizgrenzeC = heizgrenze,
                Sperrfenster = sperre,
                MindestlaufzeitMin = mindestlaufzeit,
                Zapfprofil = zp,
                ZirkulationProjektKw = zirkKw,
                ZirkulationLaufzeitHd = zirkH,
                Nenninhalte = nenn,
                Katalog = katalog,
                Parameter = p
            };
            return new PufferAuslegungVorbelegung
            {
                IdProjekt = idProjekt,
                IdPuffer = idPuffer,
                Eingang = e,
                Herkunft = h.AsReadOnly(),
                Nutzungsprofil = np
            };
        }

        // ---- Anlagen und Erzeuger ----

        private sealed class Anlage
        {
            public int Id, Typ, IdGeraet, Kollektoranzahl;
            public bool Heizstab, Bivalent, Sperrung;
            public double SperrVon, SperrBis;
        }

        private static List<Anlage> AnlagenLesen(int idProjekt)
        {
            var l = new List<Anlage>();
            DataTable t = DataRepository.GetDataTable(SQL_ANLAGEN, P("@projekt", idProjekt));
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
            {
                int typ = (int)Zahl(r["ID_Type"]);
                string geraet = typ == TYP_WP ? "ID_WP" : typ == TYP_KESSEL ? "ID_Kessel" : typ == TYP_BHKW ? "ID_BHKW" : "ID_Solar";
                l.Add(new Anlage
                {
                    Id = (int)Zahl(r["ID"]),
                    Typ = typ,
                    IdGeraet = (int)Zahl(r[geraet]),
                    Heizstab = Zahl(r["Heizstab"]) != 0,
                    Bivalent = Zahl(r["Bivalenter_Betrieb"]) != 0,
                    Sperrung = Zahl(r["Sperrung"]) != 0,
                    SperrVon = Zahl(r["Sperrzeit_von"]),
                    SperrBis = Zahl(r["Sperrzeit_bis"]),
                    Kollektoranzahl = (int)Zahl(r["Kollektormodulanzahl"])
                });
            }
            return l;
        }

        /// <summary>Der Typ eines Kaskadeneintrags (<c>Tool_1 … Tool_4</c>); 0 = unbekannt.</summary>
        private static int TypAusKaskade(string eintrag)
        {
            switch ((eintrag ?? "").Trim())
            {
                case DbWerte.ERZEUGER_WAERMEPUMPE: return TYP_WP;
                case DbWerte.ERZEUGER_SOLARTHERMIE: return TYP_SOLAR;
                case DbWerte.ERZEUGER_HEIZKESSEL: return TYP_KESSEL;
                case DbWerte.ERZEUGER_BHKW: return TYP_BHKW;
                default: return 0;
            }
        }

        /// <summary>Der Typ an Rang 1: der erste Kaskadeneintrag mit vorhandener Anlage, sonst die feste Rangfolge.</summary>
        private static int Rang1(DataRow einst, List<Anlage> anlagen, out string text)
        {
            for (int i = 1; i <= 4; i++)
            {
                string eintrag = Text(einst, "Tool_" + i);
                int typ = TypAusKaskade(eintrag);
                if (typ != 0 && anlagen.Any(a => a.Typ == typ))
                {
                    text = "Rang 1 der Kaskade: " + eintrag + " (Tab_Einstellungen.Tool_" + i + ")";
                    return typ;
                }
            }
            foreach (int typ in RANG_RUECKFALL)
                if (anlagen.Any(a => a.Typ == typ))
                {
                    text = "Kaskade ohne vorhandenen Erzeuger: erster Erzeuger in der Rangfolge WP, BHKW, Kessel, Solar";
                    return typ;
                }
            text = "kein Wärmeerzeuger im Projekt";
            return 0;
        }

        /// <summary>Die thermische Leistung einer Anlage [kW] (WP: Nennleistung, Kessel/BHKW: Ptherm).</summary>
        private static double Leistung(Anlage a, out DataRow geraet)
        {
            geraet = null;
            if (a.IdGeraet <= 0) return 0;
            switch (a.Typ)
            {
                case TYP_WP:
                    geraet = ErsteZeile(SQL_WP, P("@id", a.IdGeraet));
                    return ZahlOderNull(geraet, "Nennleistung") ?? 0;
                case TYP_KESSEL:
                    geraet = ErsteZeile(SQL_KESSEL, P("@id", a.IdGeraet));
                    return ZahlOderNull(geraet, "Ptherm") ?? 0;
                case TYP_BHKW:
                    geraet = ErsteZeile(SQL_BHKW, P("@id", a.IdGeraet));
                    return ZahlOderNull(geraet, "Ptherm") ?? 0;
                default:
                    geraet = ErsteZeile(SQL_SOLAR, P("@id", a.IdGeraet));
                    return 0;
            }
        }

        /// <summary>Ist der Brennstoff (Bezeichner) ein Festbrennstoff? Liefert die Art für K9.</summary>
        internal static PufferBrennstoff BrennstoffAus(string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return PufferBrennstoff.Keiner;
            string b = bezeichner.ToLowerInvariant();
            if (b.Contains("pellet")) return PufferBrennstoff.Pellets;
            if (b.Contains("hackschnitzel")) return PufferBrennstoff.Hackschnitzel;
            if (b.Contains("holz") || b.Contains("kohle") || b.Contains("koks")) return PufferBrennstoff.Scheitholz;
            return PufferBrennstoff.Keiner;
        }

        private static PufferErzeuger ErzeugerBauen(int rang1, List<Anlage> anlagen, List<PufferAuslegungHerkunft> h,
                                                    out bool festbrennstoff, out bool bivalent,
                                                    out IReadOnlyList<PufferSperrfenster> sperre, out double? mindestlaufzeit)
        {
            festbrennstoff = false;
            bivalent = false;
            sperre = Array.Empty<PufferSperrfenster>();
            mindestlaufzeit = null;
            void H(string text) => h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Erzeuger), PufferHerkunftsquelle.KASKADE, text));

            double nenn = 0, zweit = 0, heizstabKw = 0, kollektor = 0;
            double? mindest = null;
            bool geregelt = false, heizstab = false, roehre = false;
            PufferBrennstoff brennstoff = PufferBrennstoff.Keiner;
            int n1 = 0;
            foreach (Anlage a in anlagen)
            {
                double kw = Leistung(a, out DataRow g);
                if (a.Typ == TYP_SOLAR)
                {
                    if (g != null)
                    {
                        double f = Solarkreis.Modulbezugsflaeche(Text(g, "Bezugsflaeche"), ZahlOderNull(g, "Aperturflaeche") ?? 0,
                                                                 ZahlOderNull(g, "Modulflaeche") ?? 0, out _);
                        kollektor += f * Math.Max(0, a.Kollektoranzahl);
                        string typ = (Text(g, "Kollektortyp") ?? "").ToLowerInvariant();
                        if (typ.Contains("röhre") || typ.Contains("roehre") || typ.Contains("vakuum")) roehre = true;
                    }
                    continue;
                }
                if (a.Typ == rang1)
                {
                    n1++;
                    nenn += kw;
                    if (a.Typ == TYP_WP)
                    {
                        string regelung = Text(g, "Regelung") ?? "";
                        if (regelung.Equals("stetig", StringComparison.OrdinalIgnoreCase)) geregelt = true;
                        if (a.Heizstab) { heizstab = true; heizstabKw += ZahlOderNull(g, "Heizung") ?? 0; }
                        if (a.Bivalent) bivalent = true;
                    }
                    else if (a.Typ == TYP_KESSEL)
                    {
                        double? ml = ZahlOderNull(g, "Mindestleistung");
                        if (ml > 0) { mindest = (mindest ?? 0) + ml.Value; geregelt = true; }
                        double? lz = ZahlOderNull(g, "Mindestlaufzeit_min");
                        if (lz > 0) mindestlaufzeit = lz;
                        int idB = (int)(ZahlOderNull(g, "Brennstoff") ?? 0);
                        if (idB > 0)
                        {
                            PufferBrennstoff art = BrennstoffAus(Convert.ToString(DataRepository.ExecuteScalar(SQL_BRENNSTOFF, P("@id", idB)),
                                                                                  CultureInfo.InvariantCulture));
                            if (art != PufferBrennstoff.Keiner) { brennstoff = art; festbrennstoff = true; }
                        }
                    }
                    if (a.Sperrung && sperre.Count == 0)
                    {
                        double dauer = ((a.SperrBis - a.SperrVon) % 24 + 24) % 24;
                        if (dauer > 0) sperre = PufferSperrprofil.Fenster("EIGEN", a.SperrVon, dauer);
                    }
                }
                else
                    zweit += kw;
            }
            if (rang1 == TYP_WP && zweit > 0) bivalent = true;
            bool nurHeizstab = rang1 == TYP_WP && zweit <= 0 && heizstab;
            if (rang1 == TYP_WP && heizstab) bivalent = true;
            if (nurHeizstab) zweit = heizstabKw;

            if (rang1 != 0 && rang1 != TYP_SOLAR)
                H("Nennleistung " + PufferAuslegungVorgaben.Text(nenn) + " kW" + (n1 > 1 ? " (Summe aus " + n1 + " Anlagen)" : "") +
                  (geregelt ? ", leistungsgeregelt" : "") + "; Zweiterzeuger " + PufferAuslegungVorgaben.Text(zweit) + " kW" +
                  (nurHeizstab ? " (Heizstab)" : ""));
            if (kollektor > 0)
                H("Kollektorfläche " + PufferAuslegungVorgaben.Text(kollektor) + " m² (Bezugsfläche × Modulanzahl)");
            return new PufferErzeuger
            {
                NennleistungKw = nenn,
                MindestleistungKw = mindest,
                Geregelt = geregelt,
                IstWaermepumpe = rang1 == TYP_WP,
                ZweiterzeugerKw = zweit,
                Heizstab = nurHeizstab,
                ZweiterzeugerFrei = false,
                Brennstoff = brennstoff,
                KollektorflaecheM2 = kollektor,
                Kollektorart = roehre ? PufferKollektorart.Roehre : PufferKollektorart.Flach
            };
        }

        // ---- Zapfprofil ----

        /// <summary>Die Abbildung der Zapfprofil-Topologie auf die Brauchwasserzone.</summary>
        internal static PufferBwTopologie Topologie(ZapfTopologie t)
        {
            switch (t)
            {
                case ZapfTopologie.Frischwasserstation: return PufferBwTopologie.Frischwasser;
                case ZapfTopologie.Wohnungsstation: return PufferBwTopologie.Wohnungsstation;
                case ZapfTopologie.Durchfluss: return PufferBwTopologie.Durchfluss;
                default: return PufferBwTopologie.Speicher;
            }
        }

        /// <summary>
        /// D_max einer Brauchwasserreihe als Tagesmaximum: je Tag das größte kumulierte Defizit gegen
        /// eine gleichmäßige Ladung mit dem Tagesmittel (Summenlinie des Tages), über das Jahr das
        /// Maximum; dazu der Tagesbedarf dieses Tages [kWh].
        /// </summary>
        public static (double DmaxKwh, double TagesbedarfKwh) DmaxTag(IReadOnlyList<double> reihe)
        {
            double best = 0, tag = 0;
            if (reihe == null) return (0, 0);
            int tage = reihe.Count / 24;
            for (int d = 0; d < tage; d++)
            {
                double summe = 0;
                for (int s = 0; s < 24; s++) summe += reihe[d * 24 + s];
                double mittel = summe / 24.0, defizit = 0, max = 0;
                for (int s = 0; s < 24; s++)
                {
                    defizit = Math.Max(0, defizit + reihe[d * 24 + s] - mittel);
                    if (defizit > max) max = defizit;
                }
                if (max > best) { best = max; tag = summe; }
            }
            return (best, tag);
        }

        private static PufferZapfprofil Zapfprofil(int idProjekt, PufferAuslegungReihen reihen, List<PufferAuslegungHerkunft> h)
        {
            void H(string quelle, string text) => h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Zapfprofil), quelle, text));
            PufferZapfprofil zp = null;
            bool generator = false;
            try { generator = ZapfprofilCtrl.Weg(idProjekt) == BrauchwasserWeg.Generator; }
            catch (Exception ex) { H(PufferHerkunftsquelle.ZAPFPROFIL, "Weg nicht lesbar: " + ex.Message); }

            if (generator)
            {
                try
                {
                    zp = AusGenerator(idProjekt, out string text);
                    H(PufferHerkunftsquelle.ZAPFPROFIL, text);
                }
                catch (Exception ex)
                {
                    H(PufferHerkunftsquelle.ZAPFPROFIL, "Zapfprofil-Auslegung nicht rechenbar: " + ex.Message);
                }
            }

            // Ohne Generator (oder ohne D_max und Nenninhalt aus ihm): die Brauchwasserreihe des Projekts.
            bool brauchtReihe = zp == null || (zp.DmaxKwh <= 0 && !(zp.NenninhaltL > 0));
            if (brauchtReihe && reihen?.Vorhanden == true)
            {
                (double dmax, double tagKwh) = DmaxTag(reihen.Brauchwasser);
                if (dmax > 0)
                {
                    zp = zp == null
                        ? new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = dmax, TagesbedarfKwh = tagKwh }
                        : zp with { DmaxKwh = dmax, TagesbedarfKwh = zp.TagesbedarfKwh ?? tagKwh, NenninhaltL = null };
                    H(PufferHerkunftsquelle.REIHE, "D_max " + PufferAuslegungVorgaben.Text(dmax) + " kWh als Tagesmaximum der Brauchwasserreihe" +
                      (generator ? "" : " (Bestandsweg: Trinkwasser über den Puffer)"));
                }
            }
            else if (brauchtReihe && !generator)
                H(PufferHerkunftsquelle.VORGABE, "Bestandsweg: D_max erst mit den Bedarfsreihen");
            return zp;
        }

        /// <summary>Das Zapfprofil-Ergebnis aus <c>ZapfprofilCtrl.Auslegung</c>, über die Topologiegruppen summiert.</summary>
        private static PufferZapfprofil AusGenerator(int idProjekt, out string text)
        {
            if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we))
                throw new InvalidOperationException("Kein Klimakalender für das Projekt.");
            ZapfprofilStand stand = ZapfprofilCtrl.Lies(idProjekt);
            Auslegungsrechnung r = ZapfprofilCtrl.Auslegung(idProjekt, stand, jan1, we, new Auslegungslauf(null, null));
            IReadOnlyList<Auslegungsgruppe> gruppen = r.Ergebnis.Gruppen;
            if (gruppen.Count == 0) throw new InvalidOperationException("Die Zapfprofil-Auslegung liefert keine Topologiegruppe.");

            double dmax = 0, tag = 0, zirk = 0;
            double? nenn = null, lade = null, personen = null;
            bool zirkDa = false;
            foreach (Auslegungsgruppe g in gruppen)
            {
                Speicherauslegungsergebnis sa = g.Speicherauslegung;
                if (sa != null)
                {
                    dmax += sa.DmaxKwh;
                    if (sa.Ladeleistung.Angesetzt > 0) lade = (lade ?? 0) + sa.Ladeleistung.Angesetzt;
                    if (sa.Personen > 0) personen = (personen ?? 0) + sa.Personen.Value;
                    double zkw = sa.Zirkulation.Angesetzt;
                    if (zkw > 0)
                    {
                        double laenge = sa.ZirkulationLaufzeit.LaengeH > 0 ? sa.ZirkulationLaufzeit.LaengeH : 24.0;
                        zirk += zkw * laenge;
                        zirkDa = true;
                    }
                }
                double? n = g.Empfehlung?.Rechenbar == true ? g.Empfehlung.NenninhaltL : null;
                n ??= sa?.NenninhaltL;
                if (g.Topologie == ZapfTopologie.Speicher && n > 0) nenn = (nenn ?? 0) + n.Value;
                if (g.Woche != null && g.Woche.TagessummenKwh.Count > 0) tag += g.Woche.TagessummenKwh.Max();
            }
            PufferBwTopologie topo = Topologie(gruppen[0].Topologie);
            double c = ProjektPuffer.WH_JE_LITER_KELVIN;
            var zp = new PufferZapfprofil
            {
                Topologie = topo,
                DmaxKwh = dmax,
                NenninhaltL = topo == PufferBwTopologie.Speicher ? nenn : null,
                LadeleistungKw = lade,
                Personen = personen,
                TagesbedarfKwh = tag > 0 ? tag : (double?)null,
                TagesbedarfL = tag > 0 ? tag * 1000.0 / (c * (BrauchwasserzoneRechner.ZAPF_C - BrauchwasserzoneRechner.KALT_C)) : (double?)null,
                ZirkulationKwhD = zirkDa ? zirk : (double?)null
            };
            text = "Zapfprofil-Auslegung (" + gruppen.Count + " Topologiegruppe" + (gruppen.Count > 1 ? "n" : "") + ", " + topo +
                   (zp.NenninhaltL.HasValue ? ", Nenninhalt " + PufferAuslegungVorgaben.Text(zp.NenninhaltL.Value) + " l" : "") +
                   ", D_max " + PufferAuslegungVorgaben.Text(dmax) + " kWh)";
            return zp;
        }

        // ---- Nenninhalte, Katalog, Nutzungen ----

        private static IReadOnlyList<double> Nenninhalte(List<PufferAuslegungHerkunft> h)
        {
            try
            {
                Nenninhaltswahl w = ZapfprofilCtrl.Nenninhalte(ZapfprofilCtrl.Parameter());
                if (w.Liste != null)
                {
                    h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Nenninhalte), PufferHerkunftsquelle.PARAMETER,
                                                      "Speicherauslegung.Nenninhalt.* (" + w.Quelle + ")"));
                    return w.Liste.WerteL;
                }
            }
            catch (Exception)
            {
                // ohne Katalogversion der Tww-Parameter: die feste Liste des Rechenkerns
            }
            h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Nenninhalte), PufferHerkunftsquelle.VORGABE,
                                              "feste Liste 100 … 10 000 l, darüber Raster 1 000 l"));
            return null;
        }

        /// <summary>Die Katalogpuffer (Bauform Puffer- bzw. Kombispeicher) als Kandidaten des Vorschlags.</summary>
        public static IReadOnlyList<PufferKatalogsatz> Katalog()
        {
            var l = new List<PufferKatalogsatz>();
            if (!DataRepository.TabelleVorhanden(PufferSpStammCtrl.TABLE)) return l;
            DataTable t = DataRepository.GetDataTable(SQL_KATALOG);
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
            {
                string typ = Text(r, "Speichertyp") ?? "";
                bool kombi = typ == DbWerte.PSP_SPEICHERTYP_KOMBI;
                if (!kombi && typ.Length > 0 && typ != DbWerte.PSP_SPEICHERTYP_PUFFER) continue;
                l.Add(new PufferKatalogsatz((int)Zahl(r["ID"]), Text(r, "Bezeichner") ?? "", Zahl(r["Gesamtvolumen"]),
                                            ZahlOderNull(r, "Bereitschaftsverluste"), kombi));
            }
            return l;
        }

        private static IEnumerable<string> Zapfnutzungen(int idProjekt)
        {
            var l = new List<string>();
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZONE) ||
                !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)) return l;
            DataTable t = DataRepository.GetDataTable(SQL_ZAPFNUTZUNGEN, P("@projekt", idProjekt));
            if (t != null) foreach (DataRow r in t.Rows) { string b = Text(r, "Bezeichner"); if (b != null) l.Add(b); }
            return l;
        }

        /// <summary>
        /// Die Nutzung der Konditionierungsvorlagen, aus denen die Kalender der Projektgebäude stammen
        /// (Herkunft in <c>Bemerkung</c>, <see cref="Kalenderherkunft.AusBemerkung"/>).
        /// </summary>
        private static IEnumerable<string> Konditionierungsnutzungen(int idProjekt)
        {
            var l = new List<string>();
            if (!DataRepository.TabelleVorhanden(KonditionierungSchema.TAB_KALENDER) ||
                !DataRepository.TabelleVorhanden(KonditionierungVorlagenSchema.TAB_VORLAGE)) return l;
            DataTable t = DataRepository.GetDataTable(SQL_KONDITIONIERUNG, P("@projekt", idProjekt));
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
            {
                string vorlage = Kalenderherkunft.AusBemerkung(Text(r, "Bemerkung")).Vorlage;
                if (string.IsNullOrEmpty(vorlage)) continue;
                object n = DataRepository.ExecuteScalar(SQL_KONDITIONIERUNG_NUTZUNG, P("@bez", vorlage), P("@groesse", Text(r, "Groesse") ?? ""));
                if (n != null && n != DBNull.Value) l.Add(Convert.ToString(n, CultureInfo.InvariantCulture));
            }
            return l;
        }

        // ---- gespeicherte Zeile ----

        private static DataRow ZeileLesen(int idProjekt, int? idPuffer)
        {
            if (!DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB)) return null;
            return ErsteZeile(SQL_ZEILE_LESEN, P("@projekt", idProjekt), PNull("@puffer", DbParamTyp.Integer, idPuffer));
        }

        private static PufferAuslegungVorbelegung Ueberlagern(PufferAuslegungVorbelegung basis, DataRow z)
        {
            PufferAuslegungEingang e = basis.Eingang;
            PufferErzeuger erz = e.Erzeuger ?? new PufferErzeuger();
            var h = new List<PufferAuslegungHerkunft>(basis.Herkunft);
            void H(string feld, string spalte) => h.Add(new PufferAuslegungHerkunft(feld, PufferHerkunftsquelle.GESPEICHERT, PufferAuslegungSchema.TAB + "." + spalte));

            e = e with
            {
                KlasseHeizung = Zahl(z["Klasse_Heizung"]) != 0,
                KlasseBrauchwasser = Zahl(z["Klasse_Brauchwasser"]) != 0,
                KlasseProzess = Zahl(z["Klasse_Prozess"]) != 0
            };
            H("Klassen", "Klasse_*");

            string np = Text(z, "Nutzungsprofil");
            if (np != null && Enum.TryParse(np, out PufferNutzungsprofil npw)) { e = e with { Nutzungsprofil = npw }; H(nameof(e.Nutzungsprofil), "Nutzungsprofil"); }
            string vo = Text(z, "Vorlage");
            if (vo != null && Enum.TryParse(vo, out PufferVorlage vow)) { e = e with { Vorlage = vow }; H(nameof(e.Vorlage), "Vorlage"); }
            double? wpg = ZahlOderNull(z, "WP_Geregelt");
            if (wpg.HasValue) { erz = erz with { Geregelt = wpg.Value != 0 }; H(nameof(e.Erzeuger), "WP_Geregelt"); }
            double? zf = ZahlOderNull(z, "Zweiterzeuger_Frei");
            if (zf.HasValue) { erz = erz with { ZweiterzeugerFrei = zf.Value != 0 }; H(nameof(e.Erzeuger), "Zweiterzeuger_Frei"); }
            double? ml = ZahlOderNull(z, "Mindestleistung_kW");
            if (ml.HasValue) { erz = erz with { MindestleistungKw = ml }; H(nameof(e.Erzeuger), "Mindestleistung_kW"); }
            e = e with { Erzeuger = erz };

            double? v;
            if ((v = ZahlOderNull(z, "Mindestlaufzeit_min")).HasValue) { e = e with { MindestlaufzeitMin = v }; H(nameof(e.MindestlaufzeitMin), "Mindestlaufzeit_min"); }
            if ((v = ZahlOderNull(z, "Anlagenvolumen_l")).HasValue) { e = e with { AnlagenvolumenL = v }; H(nameof(e.AnlagenvolumenL), "Anlagenvolumen_l"); }
            if ((v = ZahlOderNull(z, "Startziel_je_Tag")).HasValue) { e = e with { StartzielJeTag = v }; H(nameof(e.StartzielJeTag), "Startziel_je_Tag"); }
            if ((v = ZahlOderNull(z, "Deckungsziel")).HasValue) { e = e with { Deckungsziel = v }; H(nameof(e.Deckungsziel), "Deckungsziel"); }
            if ((v = ZahlOderNull(z, "DeltaT_B_K")).HasValue) { e = e with { DeltaTBK = v }; H(nameof(e.DeltaTBK), "DeltaT_B_K"); }
            if ((v = ZahlOderNull(z, "T_Puffer_Oben_C")).HasValue) { e = e with { TPufferObenC = v }; H(nameof(e.TPufferObenC), "T_Puffer_Oben_C"); }
            if ((v = ZahlOderNull(z, "BHKW_Verschiebedauer_h")).HasValue) { e = e with { BhkwVerschiebedauerH = v }; H(nameof(e.BhkwVerschiebedauerH), "BHKW_Verschiebedauer_h"); }
            string zw = Text(z, "Zirkulation_Weg");
            if (zw != null && Enum.TryParse(zw, out PufferZirkulationWeg zww)) { e = e with { ZirkulationWeg = zww }; H(nameof(e.ZirkulationWeg), "Zirkulation_Weg"); }
            string sp = Text(z, "Sperrprofil");
            if (sp != null)
            {
                e = e with { Sperrfenster = PufferSperrprofil.Fenster(sp, ZahlOderNull(z, "Sperrbeginn_h"), ZahlOderNull(z, "Sperrdauer_h")) };
                H(nameof(e.Sperrfenster), "Sperrprofil");
            }
            return basis with { Eingang = e, Herkunft = h.AsReadOnly(), Gespeichert = true, IdZeile = (int)Zahl(z["ID"]) };
        }

        // =================================================================================
        //  Reihen
        // =================================================================================

        /// <summary>Die Klimaregion des Projekts (wie <see cref="StromspeicherAuslegungCtrl"/>); 0 = keine.</summary>
        public static int Klimaregion(int idProjekt)
        {
            if (idProjekt <= 0) return 0;
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            return projekt.m_ID_Klimaregion;
        }

        /// <summary>Die drei Reihen mit der Klimaregion des Projekts.</summary>
        public static PufferAuslegungReihen Reihen(int idProjekt) => Reihen(idProjekt, Klimaregion(idProjekt));

        /// <summary>
        /// Die drei Bedarfsreihen (Heizung, Brauchwasser, Prozess) über denselben Weg wie der Lauf:
        /// <c>SimulationLaufCtrl.Vorpruefen</c>, <c>Bedarf</c>, dann <c>KanaeleDrei()</c> — kein Lauf.
        /// Scheitert ein Schritt, trägt die Rückgabe dessen Fehlertext und keine Reihen.
        /// </summary>
        public static PufferAuslegungReihen Reihen(int idProjekt, int idKlimaregion)
        {
            if (idProjekt <= 0) return new PufferAuslegungReihen(null, null, null, MyResource.Resource.SIM_MSG_KEIN_PROJEKT);
            var konfig = new KonfigurationCtrl();
            konfig.ProjektLesen(idProjekt);
            if (konfig.rows == 0) return new PufferAuslegungReihen(null, null, null, MyResource.Resource.SIM_MSG_KONFIGURATION_FEHLT);

            string fehler = SimulationLaufCtrl.Vorpruefen(idProjekt, konfig, idKlimaregion);
            if (fehler != null) return new PufferAuslegungReihen(null, null, null, fehler);

            var waerme = new SimulationWaermebedarf();
            var strom = new SimulationStrombedarf();
            fehler = SimulationLaufCtrl.Bedarf(idProjekt, idKlimaregion, konfig.m_Netzverluste, konfig.m_szNetzverlusteEinheit, waerme, strom);
            if (fehler != null) return new PufferAuslegungReihen(null, null, null, fehler);

            Kanalsatz k = waerme.KanaeleDrei();
            return new PufferAuslegungReihen(k.Bedarf[Kanal.HEIZUNG], k.Bedarf[Kanal.BRAUCHWASSER], k.Bedarf[Kanal.PROZESS], null);
        }

        // =================================================================================
        //  Rechnen, Durchrechnen
        // =================================================================================

        /// <summary>Rechnet den Eingang — die Fassade des Rechenkerns.</summary>
        public static PufferAuslegungErgebnis Rechnen(PufferAuslegungEingang eingang) => PufferAuslegung.Rechnen(eingang);

        /// <summary>
        /// Reihen, Vorbelegung und Rechnung in einem Zug. Ohne Reihen: <c>null</c> mit dem Fehlertext
        /// in <paramref name="fehlertext"/>.
        /// </summary>
        public static PufferAuslegungErgebnis Durchrechnen(int idProjekt, int? idPuffer, out PufferAuslegungVorbelegung vorbelegung,
                                                           out string fehlertext)
        {
            PufferAuslegungReihen r = Reihen(idProjekt);
            vorbelegung = Vorbelegen(idProjekt, idPuffer, r);
            fehlertext = r.Fehlertext;
            return r.Vorhanden ? Rechnen(vorbelegung.Eingang) : null;
        }

        // =================================================================================
        //  Speichern
        // =================================================================================

        private static object Abw(double? wert, double? basis) => wert.HasValue && wert != basis ? wert.Value : DBNull.Value;
        private static object Abw(bool wert, bool basis) => wert != basis ? (wert ? 1 : 0) : DBNull.Value;
        private static object Abw(string wert, string basis) => wert != null && wert != basis ? wert : DBNull.Value;

        /// <summary>Das Sperrprofil, das die Fensterliste ergibt (Umkehrung von <see cref="PufferSperrprofil.Fenster"/>).</summary>
        internal static (string Profil, double? BeginnH, double? DauerH) SperrprofilAus(IReadOnlyList<PufferSperrfenster> fenster)
        {
            if (fenster == null || fenster.Count == 0) return ("KEINE", null, null);
            foreach (string profil in new[] { "ZWEI_MAL_ZWEI", "DREI_MAL_ZWEI" })
                if (PufferSperrprofil.Fenster(profil).SequenceEqual(fenster)) return (profil, null, null);
            return ("EIGEN", fenster[0].BeginnH, fenster[0].DauerH);
        }

        /// <summary>
        /// Speichert Eingang und Ergebnis in <c>Tab_PufferAuslegung</c> (eine Zeile je Projekt und
        /// Puffer, <c>null</c> = „neu anlegen“): die Klassen immer, jede übrige Eingabespalte nur, wenn
        /// sie von der Vorbelegung abweicht (sonst NULL = Vorgabe), dazu die Zonenvolumina, die
        /// Empfehlung, das bemessende Kriterium und den Zeitpunkt. Liefert die ID der Zeile; -1 bei Fehler.
        /// </summary>
        public static int Speichern(int idProjekt, int? idPuffer, PufferAuslegungEingang eingang, PufferAuslegungErgebnis ergebnis)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (!DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB)) return -1;
            PufferAuslegungEingang b = Grundvorbelegung(idProjekt, idPuffer, null, false).Eingang;
            PufferErzeuger ez = eingang.Erzeuger ?? new PufferErzeuger(), bz = b.Erzeuger ?? new PufferErzeuger();

            var (sp, sb, sd) = SperrprofilAus(eingang.Sperrfenster);
            var (bsp, bsb, bsd) = SperrprofilAus(b.Sperrfenster);
            bool sperreAbw = sp != bsp || sb != bsb || sd != bsd;

            var werte = new List<DbParam>
            {
                P("@projekt", idProjekt),
                PNull("@puffer", DbParamTyp.Integer, idPuffer),
                P("@kh", eingang.KlasseHeizung ? 1 : 0),
                P("@kb", eingang.KlasseBrauchwasser ? 1 : 0),
                P("@kp", eingang.KlasseProzess ? 1 : 0),
                PObj("@np", DbParamTyp.VarWChar, Abw(eingang.Nutzungsprofil?.ToString(), b.Nutzungsprofil?.ToString())),
                PObj("@vorlage", DbParamTyp.VarWChar, Abw(eingang.Vorlage.ToString(), b.Vorlage.ToString())),
                PObj("@wpg", DbParamTyp.Integer, Abw(ez.Geregelt, bz.Geregelt)),
                PObj("@zf", DbParamTyp.Integer, Abw(ez.ZweiterzeugerFrei, bz.ZweiterzeugerFrei)),
                PObj("@mlz", DbParamTyp.Double, Abw(eingang.MindestlaufzeitMin, b.MindestlaufzeitMin)),
                PObj("@mlk", DbParamTyp.Double, Abw(ez.MindestleistungKw, bz.MindestleistungKw)),
                PObj("@avol", DbParamTyp.Double, Abw(eingang.AnlagenvolumenL, b.AnlagenvolumenL)),
                PObj("@sp", DbParamTyp.VarWChar, sperreAbw ? sp : (object)DBNull.Value),
                PObj("@sd", DbParamTyp.Double, sperreAbw && sd.HasValue ? sd.Value : (object)DBNull.Value),
                PObj("@sb", DbParamTyp.Double, sperreAbw && sb.HasValue ? sb.Value : (object)DBNull.Value),
                PObj("@start", DbParamTyp.Double, Abw(eingang.StartzielJeTag, b.StartzielJeTag)),
                PObj("@deck", DbParamTyp.Double, Abw(eingang.Deckungsziel, b.Deckungsziel)),
                PObj("@dtb", DbParamTyp.Double, Abw(eingang.DeltaTBK, b.DeltaTBK)),
                PObj("@toben", DbParamTyp.Double, Abw(eingang.TPufferObenC, b.TPufferObenC)),
                PObj("@zw", DbParamTyp.VarWChar, Abw(eingang.ZirkulationWeg?.ToString(), b.ZirkulationWeg?.ToString())),
                PObj("@bhkw", DbParamTyp.Double, Abw(eingang.BhkwVerschiebedauerH, b.BhkwVerschiebedauerH)),
                PNull("@vh", DbParamTyp.Double, ergebnis?.Zone(PufferZone.Heizung)?.VolumenL),
                PNull("@vb", DbParamTyp.Double, ergebnis?.Zone(PufferZone.Brauchwasser)?.VolumenL),
                PNull("@vp", DbParamTyp.Double, ergebnis?.Zone(PufferZone.Prozess)?.VolumenL),
                PNull("@vemp", DbParamTyp.Double, ergebnis?.EmpfehlungL),
                PObj("@bem", DbParamTyp.VarWChar, (object)ergebnis?.Bemessend ?? DBNull.Value),
                PObj("@am", DbParamTyp.VarWChar, ergebnis == null ? DBNull.Value
                                                                  : DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture))
            };

            DataRow vorhanden = ZeileLesen(idProjekt, idPuffer);
            if (vorhanden != null)
            {
                int id = (int)Zahl(vorhanden["ID"]);
                werte.Add(P("@id", id));
                return DataRepository.ExecuteNonQuery(SQL_ZEILE_AENDERN, werte.ToArray()) >= 0 ? id : -1;
            }
            if (DataRepository.ExecuteNonQuery(SQL_ZEILE_EINFUEGEN, werte.ToArray()) < 0) return -1;
            DataRow neu = ZeileLesen(idProjekt, idPuffer);
            return neu == null ? -1 : (int)Zahl(neu["ID"]);
        }

        // =================================================================================
        //  Übernehmen
        // =================================================================================

        /// <summary>
        /// Übernimmt die Empfehlung in den Projektpuffer (<paramref name="idPuffer"/>) bzw. legt einen
        /// neuen an (<c>null</c>), über <see cref="PufferSpCtrl.ProjektPufferAendern"/> und
        /// <see cref="PufferSpCtrl.ProjektPufferAnlegen"/>: Volumen = Empfehlung, Bereitschaftsverlust aus
        /// dem Katalogvorschlag, sonst aus der Kennzahl, Klassen-Set aus den gerechneten Zonen, beim
        /// Kombipuffer Schichtung mit mindestens zwei Schichten und den Zonenanteilen als Entnahmehöhen.
        /// Schwellen, Temperaturpaar, Hersteller, Speichertyp und Kosten bleiben beim Ändern unverändert
        /// (eine leere Schwelle wird mit ihrer Vorgabe geschrieben, mit der die Simulation sie ohnehin
        /// liest); beim Neuanlegen gelten die Vorgaben. Der Puffer führt keinen Katalogverweis — der
        /// Vorschlag fließt nur über den Verlust ein. Liefert die Puffer-ID; -1, wenn nichts übernommen
        /// wurde (keine Empfehlung, Puffer nicht im Projekt, Schreibfehler).
        /// </summary>
        public static int Uebernehmen(int idProjekt, int? idPuffer, PufferAuslegungErgebnis ergebnis, string bezeichner)
        {
            if (ergebnis == null || idProjekt <= 0 || !(ergebnis.EmpfehlungL > 0)) return -1;
            int volumen = (int)Math.Round(ergebnis.EmpfehlungL, MidpointRounding.AwayFromZero);
            double verluste = ergebnis.Katalogvorschlag?.BereitschaftsverlustKwhD ?? ergebnis.Kennzahlen?.Verlust?.KwhJeTag ?? 0;
            bool h = ergebnis.Zone(PufferZone.Heizung) != null;
            bool b = ergebnis.Zone(PufferZone.Brauchwasser) != null;
            bool p = ergebnis.Zone(PufferZone.Prozess) != null;
            string verwendung = PufferSpCtrl.VerwendungAusKlassenSet(h, b, p);
            double? anteilH = ergebnis.Kennzahlen?.ZonenanteilHeizung;

            PufferSpCtrl.Schichtdaten Schicht(PufferSpCtrl.Schichtdaten alt)
            {
                if (!anteilH.HasValue) return null;
                PufferSpCtrl.Schichtdaten s = alt ?? new PufferSpCtrl.Schichtdaten();
                s.Schichten = PufferSpCtrl.SchichtenKlemmen(Math.Max(s.Schichten, Math.Max(2, ergebnis.Kennzahlen.SchichtenMindest)));
                s.EntnahmeHeizung = anteilH.Value;
                s.EntnahmeBW = 1.0;
                return s;
            }

            if (idPuffer.HasValue)
            {
                DataRow r = ErsteZeile(SQL_PUFFER_STAMMWERTE, P("@id", idPuffer.Value), P("@projekt", idProjekt));
                if (r == null) return -1;
                string name = string.IsNullOrWhiteSpace(bezeichner) ? Text(r, "Bezeichner") : bezeichner;
                double? vl = ZahlOderNull(r, "Vorlauf"), rl = ZahlOderNull(r, "Ruecklauf");
                double? sEin = ZahlOderNull(r, "Schwelle_Ein"), sAus = ZahlOderNull(r, "Schwelle_Aus");
                bool ok = PufferSpCtrl.ProjektPufferAendern(
                    idPuffer.Value, idProjekt, name, Text(r, "Hersteller"), Text(r, "Speichertyp"), volumen, verluste,
                    ZahlOderNull(r, "Investitionskosten") ?? 0, verwendung,
                    vl.HasValue ? (int?)(int)vl.Value : null, rl.HasValue ? (int?)(int)rl.Value : null,
                    sEin > 0 ? sEin.Value : ProjektPuffer.SCHWELLE_EIN_DEFAULT,
                    sAus > 0 ? sAus.Value : ProjektPuffer.SCHWELLE_AUS_DEFAULT,
                    ZahlOderNull(r, "Schwelle_Aus_Nachrang"), (int)(ZahlOderNull(r, "Entladeprio") ?? 0),
                    ZahlOderNull(r, "Schwelle_Reserve") ?? ProjektPuffer.SCHWELLE_RESERVE_DEFAULT,
                    h, b, p, Schicht(anteilH.HasValue ? PufferSpCtrl.SchichtdatenLesen(idPuffer.Value) : null));
                return ok ? idPuffer.Value : -1;
            }

            string neuName = string.IsNullOrWhiteSpace(bezeichner) ? "Pufferspeicher " + volumen + " l" : bezeichner;
            int neu = PufferSpCtrl.ProjektPufferAnlegen(
                idProjekt, neuName, "", anteilH.HasValue ? DbWerte.PSP_SPEICHERTYP_KOMBI : DbWerte.PSP_SPEICHERTYP_PUFFER,
                volumen, verluste, 0, verwendung, PufferSpCtrl.SystemVorlauf(idProjekt), PufferSpCtrl.SystemRuecklauf(idProjekt),
                ProjektPuffer.SCHWELLE_EIN_DEFAULT, ProjektPuffer.SCHWELLE_AUS_DEFAULT, null, 0,
                ProjektPuffer.SCHWELLE_RESERVE_DEFAULT, h, b, p, Schicht(null));
            // Die Auslegungszeile „neu anlegen“ gehört ab jetzt zum angelegten Puffer.
            if (neu > 0 && DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB))
                DataRepository.ExecuteNonQuery(SQL_ZEILE_UMHAENGEN, P("@puffer", neu), P("@projekt", idProjekt));
            return neu;
        }

        // =================================================================================
        //  Vorlagen
        // =================================================================================

        /// <summary>Die sieben Vorlagen mit Kriterienschaltern und Beispielwerten aus der Vorgabetabelle.</summary>
        public static IReadOnlyList<PufferVorlagenbeschreibung> Vorlagen()
        {
            PufferAuslegungParameter p = PufferAuslegungParameter.Lesen();
            var l = new List<PufferVorlagenbeschreibung>();
            foreach (string typ in PufferAuslegungVorgaben.VORLAGEN)
            {
                var k = new Dictionary<string, bool>(StringComparer.Ordinal);
                foreach (string s in PufferAuslegungVorgaben.VORLAGE_SCHALTER) k[s] = p.VorlageAn(typ, s);
                l.Add(new PufferVorlagenbeschreibung(
                    (PufferVorlage)Enum.Parse(typeof(PufferVorlage), typ), k,
                    p.VorlageWert(typ, "Mindestlaufzeit_min", 0), p.VorlageWert(typ, "Startziel_je_Tag", 0),
                    p.VorlageWert(typ, "Beispiel_Leistung_kW", 0), p.VorlageWert(typ, "Faustwert_l_kW", 0),
                    typ == "SOLAR" ? "l/m²" : "l/kW"));
            }
            return l.AsReadOnly();
        }

        // =================================================================================
        //  Hilfen
        // =================================================================================

        private static DbParam P(string name, object wert) => new DbParam(name, wert);

        private static DbParam PObj(string name, DbParamTyp typ, object wert) => new DbParam(name, typ) { Wert = wert ?? DBNull.Value };

        private static DbParam PNull<T>(string name, DbParamTyp typ, T? wert) where T : struct =>
            new DbParam(name, typ) { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };

        private static DataRow ErsteZeile(string sql, params DbParam[] p)
        {
            DataTable t = DataRepository.GetDataTable(sql, p);
            return t != null && t.Rows.Count > 0 ? t.Rows[0] : null;
        }

        private static double Zahl(object v) =>
            v == null || v == DBNull.Value ? 0 : Convert.ToDouble(v, CultureInfo.InvariantCulture);

        private static double? ZahlOderNull(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            object v = r[spalte];
            if (v is string s)
                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null;
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        private static string Text(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            string s = Convert.ToString(r[spalte], CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
    }
}
