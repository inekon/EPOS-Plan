using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using static WindowsFormsApplication1.Textbaustein;

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
        /// <summary>Ein Teillastfeld des Projektgeräts der Anlage (Welle M4: <c>Tab_WP.Mindestleistung_kW</c>, <c>Tab_BHKW.Mindestlaufzeit_min</c>).</summary>
        public const string TEILLAST = "Teillastfeld der Anlage";
        public const string PARAMETER = "Vorgabetabelle";
        public const string VORGABE = "Vorgabe";
        public const string GESPEICHERT = "Gespeicherte Auslegung";
    }

    /// <summary>
    /// Eine Zeile der Herkunftsliste: Feld des Eingangs, Quelle und Text als <see cref="Textbaustein"/>
    /// (Ressourcenschlüssel <c>PAUS_HERK_*</c> mit Argumenten; Oberfläche und Bericht lösen ihn auf).
    /// </summary>
    public sealed record PufferAuslegungHerkunft(string Feld, string Quelle, Textbaustein Baustein)
    {
        /// <summary>Eine Zeile mit einem Klartext ohne Ressourcenschlüssel.</summary>
        public PufferAuslegungHerkunft(string feld, string quelle, string text) : this(feld, quelle, Textbaustein.Klar(text))
        {
        }

        /// <summary>Der Text als deutscher Klartext.</summary>
        public string Text => Baustein?.Klartext ?? "";
    }

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
        /// <summary>
        /// Die gespeicherten Kriterienschalter, die von der Vorlage ABWEICHEN (Kennung → an/aus); leer = die
        /// Vorlage gilt. Sie stehen schon im Parametersatz des Eingangs.
        /// </summary>
        public IReadOnlyDictionary<string, bool> Kriterien { get; init; } = new Dictionary<string, bool>(StringComparer.Ordinal);
        /// <summary>Der Parametersatz VOR den gespeicherten Kriterienschaltern — Grundlage jeder neuen Überschreibung.</summary>
        public PufferAuslegungParameter KriterienBasis { get; init; }
        /// <summary>Die gespeicherte Anzeigestufe (<c>SCHNELL</c>, <c>STANDARD</c>, <c>EXPERTE</c>); <c>null</c> = Vorgabe.</summary>
        public string Anzeigestufe { get; init; }

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

    /// <summary>
    /// Eine gespeicherte Auslegung aus <c>Tab_PufferAuslegung</c>, wie der Bericht sie zeigt
    /// (Stufe P3): die gespeicherten Zonenvolumina, Empfehlung, bemessendes Kriterium und Zeitpunkt,
    /// dazu der Projektpuffer (Name, gewähltes Volumen) und — aus einer Nachrechnung mit der
    /// gespeicherten Eingabe und dem aktuellen Projektstand — Vorlage, Nutzungsprofil, Herkunft des
    /// bemessenden Kriteriums, Kennzahlen und Warnliste. Die Nachrechnung schreibt nichts.
    /// </summary>
    public sealed record PufferAuslegungGespeichert
    {
        /// <summary>Die ID der Zeile in <c>Tab_PufferAuslegung</c>.</summary>
        public int IdZeile { get; init; }
        /// <summary>Der Projektpuffer; <c>null</c> = „neu anlegen“, noch nicht übernommen.</summary>
        public int? IdPuffer { get; init; }
        /// <summary>Der Bezeichner des Projektpuffers; <c>null</c> ohne Puffer.</summary>
        public string Puffername { get; init; }
        /// <summary>Das Volumen des Projektpuffers [l] (<c>Gesamtvolumen</c>); <c>null</c> ohne Puffer.</summary>
        public double? GewaehltL { get; init; }
        public bool KlasseHeizung { get; init; }
        public bool KlasseBrauchwasser { get; init; }
        public bool KlasseProzess { get; init; }
        /// <summary>Die wirksame Vorlage (gespeichert, sonst Vorbelegung); <c>null</c> ohne Nachrechnung und ohne Spalte.</summary>
        public PufferVorlage? Vorlage { get; init; }
        /// <summary>Das wirksame Nutzungsprofil (gespeichert, sonst abgeleitet).</summary>
        public PufferNutzungsprofil? Nutzungsprofil { get; init; }
        /// <summary>Die Herkunft des abgeleiteten Nutzungsprofils; <c>null</c> = keine (gespeichertes Profil).</summary>
        public Textbaustein NutzungsprofilHerkunft { get; init; }
        public double? VolumenHeizungL { get; init; }
        public double? VolumenBrauchwasserL { get; init; }
        public double? VolumenProzessL { get; init; }
        public double? EmpfehlungL { get; init; }
        /// <summary>Zone und Kriterium, die bemessen (z. B. „Heizung: K4“), wie gespeichert.</summary>
        public string Bemessend { get; init; }
        /// <summary>Die Herkunft des bemessenden Kriteriums aus der Nachrechnung; <c>null</c> = unbekannt.</summary>
        public Textbaustein BemessendHerkunft { get; init; }
        public DateTime? BerechnetAm { get; init; }
        /// <summary>Die Empfehlung der Nachrechnung [l]; <c>null</c> = keine Nachrechnung.</summary>
        public double? NachgerechnetL { get; init; }
        public double? StartsJeTag { get; init; }
        /// <summary>Starts je Tag des Rang-1-Erzeugers im letzten Probelauf dieser Sitzung; <c>null</c> = kein Lauf.</summary>
        public double? ProbelaufStartsJeTag { get; init; }
        /// <summary>Zeitpunkt des letzten Probelaufs; <c>null</c> = kein Lauf.</summary>
        public DateTime? ProbelaufAm { get; init; }
        public double? VerlustKwhJeTag { get; init; }
        public double? VerlustWJeK { get; init; }
        public IReadOnlyList<PufferWarnung> Warnungen { get; init; } = Array.Empty<PufferWarnung>();
        /// <summary>Die Nutzen-Aufwand-Zeile der Nachrechnung (Welle P4d); leer = nicht gerechnet.</summary>
        public IReadOnlyList<PufferNachbarstufe> Nachbarstufen { get; init; } = Array.Empty<PufferNachbarstufe>();
        /// <summary>Warum die Nachrechnung nicht möglich war; <c>null</c> = sie lief (oder war nicht verlangt).</summary>
        public string Fehlertext { get; init; }

        /// <summary>Die Zone des bemessenden Kriteriums; <c>null</c>, wenn <see cref="Bemessend"/> keine nennt.</summary>
        public PufferZone? BemessendeZone => Teilen(Bemessend).Zone;

        /// <summary>Die Kennung des bemessenden Kriteriums (z. B. „K4“); <c>null</c> ohne.</summary>
        public string BemessendeKennung => Teilen(Bemessend).Kennung;

        private static (PufferZone? Zone, string Kennung) Teilen(string bemessend)
        {
            if (string.IsNullOrWhiteSpace(bemessend)) return (null, null);
            int i = bemessend.IndexOf(':');
            if (i < 0) return (null, bemessend.Trim());
            PufferZone? zone = Enum.TryParse(bemessend.Substring(0, i).Trim(), out PufferZone z) ? z : null;
            string kennung = bemessend.Substring(i + 1).Trim();
            return (zone, kennung.Length == 0 ? null : kennung);
        }
    }

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
        /// <summary>Der Katalogsatz einer Wärmepumpe (über <c>Tab_WP.ID_Stamm</c>) — Rückfall der Teillastfelder.</summary>
        internal const string SQL_WP_STAMM = "SELECT * FROM Tab_WP_STAMM WHERE ID = ?";
        internal const string SQL_SOLAR = "SELECT * FROM Tab_Solarkollektoren WHERE ID = ?";

        /// <summary>Der Name eines Brennstoffs im Projekt — aus der Projektkopie (<see cref="ProjektBrennstoffe.Sicht"/>).</summary>
        private static string BrennstoffName(int idProjekt, int idBrennstoff)
        {
            string quelle = ProjektBrennstoffe.Sicht(idProjekt, out DbParam[] sicht);
            return Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM " + quelle + " AS b WHERE b.ID = ?",
                                                                 ProjektBrennstoffe.Mit(sicht, P("@id", idBrennstoff))),
                                    CultureInfo.InvariantCulture);
        }

        internal const string SQL_UEBERGABE =
            "SELECT Uebergabe_Art, COUNT(*) AS Anzahl FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Uebergabe_Art IS NOT NULL " +
            "GROUP BY Uebergabe_Art ORDER BY COUNT(*) DESC, Uebergabe_Art";
        internal const string SQL_GEBAEUDE_ANZAHL = "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?";
        internal const string SQL_PROZESS =
            "SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?";
        /// <summary>Höchster Vorlauf und tiefster Rücklauf der zugeordneten Prozesse mit Temperaturpaar (V29).</summary>
        internal const string SQL_PROZESS_TEMPERATUR =
            "SELECT MAX(p.Vorlauf) AS VL, MIN(p.Ruecklauf) AS RL FROM Tab_Prozesswaerme p " +
            "INNER JOIN Z_Projekt_Prozesswaerme z ON z.ID_Prozesswaerme = p.ID " +
            "WHERE z.ID_Projekt = ? AND p.ID_Projekt = ? AND p.Vorlauf IS NOT NULL AND p.Ruecklauf IS NOT NULL";
        /// <summary>Höchster gepflegter Vorlauf der Wärmeerzeuger an der Kaskade (V29, Gegenstück zu <c>SQL_SYSTEM_VORLAUF</c>).</summary>
        internal static readonly string SQL_ERZEUGER_VORLAUF_MAX =
            "SELECT MAX(Vorlauf) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type IN (" +
            ProjektPuffer.SYSTEMVORGABE_TYPEN + ") AND Vorlauf > 0";
        internal const string SQL_ZAPFNUTZUNGEN =
            "SELECT n.Bezeichner FROM " + TwwSchema.TAB_TWW_ZONE + " z JOIN " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
            " n ON n.ID = z.ID_Nutzungsart WHERE z.ID_Projekt = ? ORDER BY z.Reihenfolge, z.ID";
        /// <summary>
        /// Die Nutzung an den Kalendern der Projektgebäude und ihrer Zonen (am Zonenkalender ist
        /// <c>ID_Gebaeude</c> das Gebäude der Zone) — die Kopie trägt sie selbst
        /// (<see cref="KonditionierungNutzungSchema"/>).
        /// </summary>
        internal const string SQL_KONDITIONIERUNG =
            "SELECT k.ID_Gebaeude, k.Nutzung FROM " + KonditionierungSchema.TAB_KALENDER + " k JOIN Tab_Gebaeude g " +
            "ON g.ID = k.ID_Gebaeude WHERE g.ID_Projekt = ? AND k.Nutzung IS NOT NULL ORDER BY g.ID, k.ID";
        /// <summary>
        /// Die KP3-Aufheizbemessung des jüngsten Laufs mit Gebäudeergebnis (V30): Zahl der bemessenen Gebäude,
        /// Summe der Aufheizleistung Φ_n und längste Aufheizzeit t_auf,max.
        /// </summary>
        internal const string SQL_AUFHEIZ =
            "SELECT COUNT(g." + AufheizErgebnisSchema.SPALTE_LEISTUNG + ") AS Anzahl, SUM(g." + AufheizErgebnisSchema.SPALTE_LEISTUNG +
            ") AS Leistung, MAX(g." + AufheizErgebnisSchema.SPALTE_ZEIT_MAX + ") AS Dauer FROM " + AufheizErgebnisSchema.TAB_GEBAEUDE +
            " g WHERE g.ID_Ergebnis = (SELECT MAX(e.ID) FROM Tab_Ergebnis e WHERE e.ID_Projekt = ? AND EXISTS (SELECT 1 FROM " +
            AufheizErgebnisSchema.TAB_GEBAEUDE + " x WHERE x.ID_Ergebnis = e.ID)) AND g." + AufheizErgebnisSchema.SPALTE_LEISTUNG + " > 0";

        internal const string SQL_KATALOG =
            "SELECT ID, Bezeichner, Speichertyp, Gesamtvolumen, Bereitschaftsverluste FROM Tab_Pufferspeicher_STAMM " +
            "WHERE Gesamtvolumen > 0 ORDER BY Gesamtvolumen, ID";

        internal const string SQL_GESPEICHERT =
            "SELECT a.*, p.Bezeichner AS Puffer_Bezeichner, p.Gesamtvolumen AS Puffer_Volumen FROM " + PufferAuslegungSchema.TAB +
            " a LEFT JOIN Tab_Pufferspeicher p ON p.ID = a.ID_Pufferspeicher AND p.ID_Projekt = a.ID_Projekt " +
            "WHERE a.ID_Projekt = ? ORDER BY a.ID";

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
        internal const string SQL_ZEILE_ERGAENZUNG =
            "UPDATE " + PufferAuslegungSchema.TAB + " SET Kriterien_Aktiv = ?, Sperrzeit_Expertenweg = ?, " +
            "Auslegungsheizlast_kW = ?, Wohneinheiten = ?, Anzeigestufe = ? WHERE ID = ?";
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
            void H(string feld, string quelle, Textbaustein text) => h.Add(new PufferAuslegungHerkunft(feld, quelle, text));

            PufferAuslegungParameter p = PufferAuslegungParameter.Lesen(idProjekt);
            H(nameof(PufferAuslegungEingang.Parameter), PufferHerkunftsquelle.PARAMETER,
              ProjektPufferparameter.HatKopie(idProjekt)
                  ? T("PAUS_HERK_PARAMETER_KOPIE", "{0} (Projektkopie), fehlende Schlüssel aus {1}",
                      ProjektPufferparameter.TAB, PufferAuslegungSchema.TAB_PARAMETER)
                  : DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB_PARAMETER)
                      ? T("PAUS_HERK_PARAMETER_TABELLE", "{0}, fehlende Schlüssel aus den eingebauten Vorgaben", PufferAuslegungSchema.TAB_PARAMETER)
                      : T("PAUS_HERK_PARAMETER_EINGEBAUT", "eingebaute Vorgaben (Vorgabetabelle fehlt)"));

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
                H("Klassen", PufferHerkunftsquelle.PUFFER, T("PAUS_HERK_KLASSEN_PUFFER", "Klassen-Set des Puffers (Nutzung_Heizung/_Brauchwasser/_Prozess)"));
            }
            else
            {
                kH = true; kB = false; kP = prozessVorhanden;
                H("Klassen", PufferHerkunftsquelle.VORGABE, kP
                    ? T("PAUS_HERK_KLASSEN_NEU_PROZESS", "neuer Puffer: Heizung und Prozesswärme (Projekt trägt Prozesswärme)")
                    : T("PAUS_HERK_KLASSEN_NEU", "neuer Puffer: Heizung"));
            }

            // ---- Temperaturpaar ----
            double vl, rl;
            double? pv = ZahlOderNull(pz, "Vorlauf"), pr = ZahlOderNull(pz, "Ruecklauf");
            if (pv > 0 && pr > 0 && pv > pr)
            {
                vl = pv.Value; rl = pr.Value;
                H(nameof(PufferAuslegungEingang.VorlaufC), PufferHerkunftsquelle.PUFFER, T("PAUS_HERK_TEMPERATUR_PUFFER", "Temperaturpaar des Puffers"));
            }
            else
            {
                int? sv = PufferSpCtrl.SystemVorlauf(idProjekt), sr = PufferSpCtrl.SystemRuecklauf(idProjekt);
                if (sv > 0 && sr > 0 && sv > sr)
                {
                    vl = sv.Value; rl = sr.Value;
                    H(nameof(PufferAuslegungEingang.VorlaufC), PufferHerkunftsquelle.KASKADE,
                      T("PAUS_HERK_TEMPERATUR_KASKADE", "Systemtemperaturen der Erzeuger (kleinster Vorlauf, größter Rücklauf)"));
                }
                else
                {
                    vl = SimulationControl.KESSEL_VORLAUF_RUECKFALL; rl = SimulationControl.KESSEL_RUECKLAUF_RUECKFALL;
                    H(nameof(PufferAuslegungEingang.VorlaufC), PufferHerkunftsquelle.VORGABE, T("PAUS_HERK_TEMPERATUR_RUECKFALL", "Rückfall-Temperaturpaar des Rechenkerns"));
                }
            }

            // ---- Temperaturpaar der Prozesswärme (V29) ----
            double? prozessVl = null, prozessRl = null, erzeugerVlMax = null;
            if (kP && prozessVorhanden)
            {
                DataRow pt = ErsteZeile(SQL_PROZESS_TEMPERATUR, P("@projekt", idProjekt), P("@projekt2", idProjekt));
                double? tv = ZahlOderNull(pt, "VL"), tr = ZahlOderNull(pt, "RL");
                if (tv > 0 && tr >= 0 && tv > tr) { prozessVl = tv; prozessRl = tr; }
                double ev = Zahl(DataRepository.ExecuteScalar(SQL_ERZEUGER_VORLAUF_MAX, P("@projekt", idProjekt)));
                if (ev > 0) erzeugerVlMax = ev;
                H(nameof(PufferAuslegungEingang.ProzessVorlaufC), prozessVl.HasValue ? PufferHerkunftsquelle.PROJEKT : PufferHerkunftsquelle.PUFFER,
                  prozessVl.HasValue
                      ? T("PAUS_HERK_PROZESS_TEMPERATUR", "Temperaturpaar der Prozesswärme")
                      : T("PAUS_HERK_PROZESS_TEMPERATUR_KEINE", "kein Temperaturpaar der Prozesswärme gepflegt: Temperaturpaar des Puffers"));
                H(nameof(PufferAuslegungEingang.ErzeugerVorlaufMaxC), erzeugerVlMax.HasValue ? PufferHerkunftsquelle.KASKADE : PufferHerkunftsquelle.VORGABE,
                  erzeugerVlMax.HasValue
                      ? T("PAUS_HERK_ERZEUGER_VORLAUF", "höchster Vorlauf der Erzeuger an der Kaskade")
                      : T("PAUS_HERK_ERZEUGER_VORLAUF_KEINE", "kein Erzeugervorlauf gepflegt: Prüfung nur gegen 95 °C"));
            }

            // ---- Schwellen (Datenbank in %, Eingang als Anteil) ----
            double? sEin = ZahlOderNull(pz, "Schwelle_Ein"), sAus = ZahlOderNull(pz, "Schwelle_Aus");
            double? schwelleEin = sEin > 0 ? sEin / 100.0 : null;
            double? schwelleAus = sAus > 0 ? sAus / 100.0 : null;
            H(nameof(PufferAuslegungEingang.SchwelleEin), schwelleEin.HasValue ? PufferHerkunftsquelle.PUFFER : PufferHerkunftsquelle.VORGABE,
              schwelleEin.HasValue ? T("PAUS_HERK_SCHWELLE_EIN_PUFFER", "Einschaltschwelle des Puffers")
                                   : T("PAUS_HERK_SCHWELLE_VORGABE", "Vorgabe {0}", PufferAuslegungVorgaben.SCHWELLE_EIN));
            H(nameof(PufferAuslegungEingang.SchwelleAus), schwelleAus.HasValue ? PufferHerkunftsquelle.PUFFER : PufferHerkunftsquelle.VORGABE,
              schwelleAus.HasValue ? T("PAUS_HERK_SCHWELLE_AUS_PUFFER", "Ausschaltschwelle des Puffers")
                                   : T("PAUS_HERK_SCHWELLE_VORGABE", "Vorgabe {0}", PufferAuslegungVorgaben.SCHWELLE_AUS));

            // ---- Einstellungen ----
            DataRow einst = ErsteZeile(SQL_EINSTELLUNGEN, P("@projekt", idProjekt));
            double? heizgrenze = ZahlOderNull(einst, "Kessel_Heizgrenze");
            H(nameof(PufferAuslegungEingang.HeizgrenzeC), heizgrenze.HasValue ? PufferHerkunftsquelle.PROJEKT : PufferHerkunftsquelle.VORGABE,
              heizgrenze.HasValue ? Klar("Tab_Einstellungen.Kessel_Heizgrenze")
                                  : T("PAUS_HERK_HEIZGRENZE_LEER", "leer: {0} °C", (double)SimulationSPK.HEIZGRENZE_VORGABE_C));
            heizgrenze ??= SimulationSPK.HEIZGRENZE_VORGABE_C;
            double? zirkKw = ZahlOderNull(einst, "Zirkulation_Leistung_kW");
            double? zirkH = ZahlOderNull(einst, "Zirkulation_Laufzeit_h_d");
            if (zirkKw.HasValue)
                H(nameof(PufferAuslegungEingang.ZirkulationProjektKw), PufferHerkunftsquelle.PROJEKT, Klar("Tab_Einstellungen.Zirkulation_Leistung_kW"));

            // ---- Kaskade und Erzeuger ----
            List<Anlage> anlagen = AnlagenLesen(idProjekt);
            int rang1 = Rang1(einst, anlagen, out Textbaustein rangText);
            H(nameof(PufferAuslegungEingang.Erzeuger), PufferHerkunftsquelle.KASKADE, rangText);
            PufferErzeuger erz = ErzeugerBauen(idProjekt, rang1, anlagen, h, out bool festbrennstoff, out bool bivalent,
                                               out IReadOnlyList<PufferSperrfenster> sperre, out double? mindestlaufzeit);

            // ---- Vorlage ----
            PufferVorlage vorlage;
            Textbaustein vText;
            if (kP && !kH && !kB) { vorlage = PufferVorlage.PROZESS; vText = T("PAUS_HERK_VORLAGE_PROZESS", "nur Prozesswärme"); }
            else
                switch (rang1)
                {
                    case TYP_SOLAR: vorlage = PufferVorlage.SOLAR; vText = T("PAUS_HERK_VORLAGE_SOLAR", "Solarthermie an Rang 1"); break;
                    case TYP_WP:
                        vorlage = bivalent ? PufferVorlage.WP_BIVALENT : PufferVorlage.WP_MONO;
                        vText = bivalent
                            ? T("PAUS_HERK_VORLAGE_WP_BIVALENT", "Wärmepumpe mit Zweiterzeuger (Kessel, Heizstab oder bivalenter Betrieb)")
                            : T("PAUS_HERK_VORLAGE_WP_MONO", "Wärmepumpe ohne Zweiterzeuger");
                        break;
                    case TYP_BHKW: vorlage = PufferVorlage.BHKW; vText = T("PAUS_HERK_VORLAGE_BHKW", "BHKW an Rang 1"); break;
                    case TYP_KESSEL:
                        vorlage = festbrennstoff ? PufferVorlage.FESTBRENNSTOFF : PufferVorlage.KESSEL;
                        vText = festbrennstoff ? T("PAUS_HERK_VORLAGE_FESTBRENNSTOFF", "Festbrennstoffkessel an Rang 1")
                                               : T("PAUS_HERK_VORLAGE_KESSEL", "Kessel an Rang 1");
                        break;
                    default:
                        vorlage = kP ? PufferVorlage.PROZESS : PufferVorlage.WP_MONO;
                        vText = T("PAUS_HERK_VORLAGE_KEIN_ERZEUGER", "kein Wärmeerzeuger im Projekt: Vorgabe");
                        break;
                }
            H(nameof(PufferAuslegungEingang.Vorlage), rang1 == 0 && !(kP && !kH && !kB) ? PufferHerkunftsquelle.VORGABE : PufferHerkunftsquelle.KASKADE, vText);
            if (sperre.Count > 0)
                H(nameof(PufferAuslegungEingang.Sperrfenster), PufferHerkunftsquelle.KASKADE, T("PAUS_HERK_SPERRE_ANLAGE", "Sperrzeit der Anlage an Rang 1 (Sperrzeit_von/_bis)"));
            else
                H(nameof(PufferAuslegungEingang.Sperrfenster), PufferHerkunftsquelle.VORGABE, T("PAUS_HERK_SPERRE_KEINE", "keine Sperrzeit gepflegt"));

            // ---- Übergabeart (häufigste über die Gebäude) ----
            string uebergabe = null;
            DataTable ue = DataRepository.GetDataTable(SQL_UEBERGABE, P("@projekt", idProjekt));
            if (ue != null && ue.Rows.Count > 0) uebergabe = Text(ue.Rows[0], "Uebergabe_Art");
            int gebaeude = (int)Zahl(DataRepository.ExecuteScalar(SQL_GEBAEUDE_ANZAHL, P("@projekt", idProjekt)));
            H(nameof(PufferAuslegungEingang.Uebergabeart), uebergabe != null ? PufferHerkunftsquelle.GEBAEUDE : PufferHerkunftsquelle.VORGABE,
              uebergabe != null
                  ? T("PAUS_HERK_UEBERGABE_GEBAEUDE", "häufigste Übergabeart über {0} Gebäude", gebaeude)
                  : T("PAUS_HERK_UEBERGABE_KEINE", "keine Übergabeart gepflegt: ideal, rechnet wie FLAECHE (Hinweis {0})",
                      PufferWarncode.UEBERGABE_UNBEKANNT));
            H(nameof(PufferAuslegungEingang.AnlagenvolumenL), PufferHerkunftsquelle.VORGABE,
              T("PAUS_HERK_ANLAGENVOLUMEN", "Übergabeart × Auslegungsheizlast (Anlagenvolumen.<Übergabe>_l_kW)"));
            H(nameof(PufferAuslegungEingang.AuslegungsheizlastKw), PufferHerkunftsquelle.REIHE, T("PAUS_HERK_HEIZLAST_REIHE", "Maximum der Heizreihe"));

            // ---- Zapfprofil ----
            PufferZapfprofil zp = mitZapfprofil ? Zapfprofil(idProjekt, reihen, h) : null;

            // ---- Nenninhalte und Katalog ----
            IReadOnlyList<double> nenn = Nenninhalte(h);
            IReadOnlyList<PufferKatalogsatz> katalog = Katalog();
            H(nameof(PufferAuslegungEingang.Katalog), PufferHerkunftsquelle.KATALOG,
              T("PAUS_HERK_KATALOG_PUFFER", "{0} Katalogpuffer aus Tab_Pufferspeicher_STAMM", katalog.Count));

            // ---- Nutzungsprofil ----
            PufferNutzungsprofilAbleitung np = global::WindowsFormsApplication1.Nutzungsprofil.Ableiten(
                Zapfnutzungen(idProjekt), prozessVorhanden, Konditionierungsnutzungen(idProjekt),
                NutzungsprofilZuordnung.Lesen());
            H(nameof(PufferAuslegungEingang.Nutzungsprofil), np.Vorgabe ? PufferHerkunftsquelle.VORGABE : PufferHerkunftsquelle.PROJEKT, np.HerkunftBaustein);

            // ---- Aufheizbemessung KP3 (Kriterium K12, V30) ----
            var (aufheizKw, aufheizH, aufheizGebaeude) = Aufheizbemessung(idProjekt);
            H(nameof(PufferAuslegungEingang.AufheizleistungKw), aufheizKw.HasValue ? PufferHerkunftsquelle.GEBAEUDE : PufferHerkunftsquelle.VORGABE,
              aufheizKw.HasValue ? T("PAUS_HERK_AUFHEIZ_KP3", "Aufheizbemessung KP3 des letzten Laufs: Summe Aufheiz_Leistung_Kw über {0} Gebäude, längste Aufheizzeit", aufheizGebaeude)
                                 : T("PAUS_HERK_AUFHEIZ_KEINE", "keine Aufheizbemessung im letzten Lauf (Kriterium K12 bemisst nicht)"));

            if (reihen != null)
                H("Reihen", PufferHerkunftsquelle.REIHE, reihen.Vorhanden
                    ? T("PAUS_HERK_REIHEN", "Bedarfsreihen aus KanaeleDrei()")
                    : T("PAUS_HERK_REIHEN_KEINE", "keine Reihen: {0}", reihen.Fehlertext));

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
                ProzessVorlaufC = prozessVl,
                ProzessRuecklaufC = prozessRl,
                ErzeugerVorlaufMaxC = erzeugerVlMax,
                SchwelleEin = schwelleEin,
                SchwelleAus = schwelleAus,
                ReiheHeizung = reihen?.Vorhanden == true ? reihen.Heizung : Array.Empty<double>(),
                ReiheBrauchwasser = reihen?.Vorhanden == true ? reihen.Brauchwasser : Array.Empty<double>(),
                ReiheProzess = reihen?.Vorhanden == true ? reihen.Prozess : Array.Empty<double>(),
                Uebergabeart = uebergabe,
                HeizgrenzeC = heizgrenze,
                Sperrfenster = sperre,
                MindestlaufzeitMin = mindestlaufzeit,
                AufheizleistungKw = aufheizKw,
                AufheizdauerH = aufheizH,
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

        /// <summary>
        /// Φ_n [kW] und n [h] aus der KP3-Aufheizbemessung des jüngsten Laufs mit Gebäudeergebnis; ohne Bemessung
        /// beide <c>null</c>. Eine Aufheizzeit 0 h zählt als „keine Rampe“ (Vorgabe <c>Aufheiz.Dauer_h</c>).
        /// </summary>
        public static (double? LeistungKw, double? DauerH, int Gebaeude) Aufheizbemessung(int idProjekt)
        {
            if (!DataRepository.TabelleVorhanden(AufheizErgebnisSchema.TAB_GEBAEUDE)) return (null, null, 0);
            DataRow r = ErsteZeile(SQL_AUFHEIZ, P("@p", idProjekt));
            if (r == null || r["Anzahl"] == DBNull.Value) return (null, null, 0);
            int anzahl = Convert.ToInt32(r["Anzahl"], CultureInfo.InvariantCulture);
            if (anzahl <= 0 || r["Leistung"] == DBNull.Value) return (null, null, 0);
            double? dauer = r["Dauer"] == DBNull.Value ? (double?)null : Zahl(r["Dauer"]);
            return (Zahl(r["Leistung"]), dauer > 0 ? dauer : null, anzahl);
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

        /// <summary>Der Schlüssel der Herkunftszeile „kein Wärmeerzeuger im Projekt“ (die Oberfläche blendet sie aus).</summary>
        public const string SCHLUESSEL_KEIN_ERZEUGER = "PAUS_HERK_RANG1_KEINER";

        /// <summary>Der Kaskadeneintrag als Baustein (Ressource je Erzeugertyp, Rückfall der Eintrag selbst).</summary>
        private static Textbaustein Erzeugertext(int typ, string eintrag)
        {
            switch (typ)
            {
                case TYP_WP: return T("PAUS_HERK_TYP_WP", eintrag);
                case TYP_BHKW: return T("PAUS_HERK_TYP_BHKW", eintrag);
                case TYP_KESSEL: return T("PAUS_HERK_TYP_KESSEL", eintrag);
                case TYP_SOLAR: return T("PAUS_HERK_TYP_SOLAR", eintrag);
                default: return Klar(eintrag);
            }
        }

        /// <summary>Der Typ an Rang 1: der erste Kaskadeneintrag mit vorhandener Anlage, sonst die feste Rangfolge.</summary>
        private static int Rang1(DataRow einst, List<Anlage> anlagen, out Textbaustein text)
        {
            for (int i = 1; i <= 4; i++)
            {
                string eintrag = Text(einst, "Tool_" + i);
                int typ = TypAusKaskade(eintrag);
                if (typ != 0 && anlagen.Any(a => a.Typ == typ))
                {
                    text = T("PAUS_HERK_RANG1_KASKADE", "Rang 1 der Kaskade: {0} (Tab_Einstellungen.Tool_{1})", Erzeugertext(typ, eintrag), i);
                    return typ;
                }
            }
            foreach (int typ in RANG_RUECKFALL)
                if (anlagen.Any(a => a.Typ == typ))
                {
                    text = T("PAUS_HERK_RANG1_RUECKFALL", "Kaskade ohne vorhandenen Erzeuger: erster Erzeuger in der Rangfolge WP, BHKW, Kessel, Solar");
                    return typ;
                }
            text = T(SCHLUESSEL_KEIN_ERZEUGER, "kein Wärmeerzeuger im Projekt");
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

        /// <summary>
        /// Die Mindestleistung einer Wärmepumpe aus den Teillastfeldern der Welle M4 — Rangfolge
        /// <b>Projektgerät der Anlage</b> (<c>Tab_WP.Mindestleistung_kW</c>, die Zeile, die die Simulation liest)
        /// → <b>Katalog</b> (<c>Tab_WP_STAMM</c> über <c>Tab_WP.ID_Stamm</c>) → <c>null</c> (Vorgabe:
        /// Anteil der Nennleistung bzw. Fixed-Speed). Leer und 0 gelten als „nicht gepflegt".
        /// </summary>
        private static double? WpMindestleistung(DataRow geraet, out string quelle, out Textbaustein text)
        {
            quelle = null;
            text = Textbaustein.Leer;
            double? p = ZahlOderNull(geraet, ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG);
            if (p > 0)
            {
                quelle = PufferHerkunftsquelle.TEILLAST;
                text = T("PAUS_HERK_TEILLAST_WP_MINDEST", "Teillastfeld der Anlage: {0}.{1} = {2} kW",
                         ErzeugerTeillastSchema.TAB_WP, ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG, p.Value);
                return p;
            }
            double? idStamm = ZahlOderNull(geraet, "ID_Stamm");
            if (!(idStamm > 0)) return null;
            DataRow stamm = ErsteZeile(SQL_WP_STAMM, P("@id", (int)idStamm.Value));
            p = ZahlOderNull(stamm, ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG);
            if (!(p > 0)) return null;
            quelle = PufferHerkunftsquelle.KATALOG;
            text = T("PAUS_HERK_KATALOG_WP_MINDEST", "Katalog: {0}.{1} = {2} kW (Katalogsatz {3})",
                     ErzeugerTeillastSchema.TAB_WP_STAMM, ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG, p.Value, (int)idStamm.Value);
            return p;
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

        private static PufferErzeuger ErzeugerBauen(int idProjekt, int rang1, List<Anlage> anlagen, List<PufferAuslegungHerkunft> h,
                                                    out bool festbrennstoff, out bool bivalent,
                                                    out IReadOnlyList<PufferSperrfenster> sperre, out double? mindestlaufzeit)
        {
            festbrennstoff = false;
            bivalent = false;
            sperre = Array.Empty<PufferSperrfenster>();
            mindestlaufzeit = null;
            void H(Textbaustein text) => h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Erzeuger), PufferHerkunftsquelle.KASKADE, text));

            double nenn = 0, zweit = 0, heizstabKw = 0, kollektor = 0;
            double? mindest = null;
            var teillast = new List<PufferAuslegungHerkunft>();
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
                        double? pmin = WpMindestleistung(g, out string quelle, out Textbaustein text);
                        if (pmin > 0)
                        {
                            mindest = (mindest ?? 0) + pmin.Value;
                            if (pmin.Value < kw) geregelt = true;
                            teillast.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Erzeuger), quelle, text));
                        }
                        if (a.Heizstab) { heizstab = true; heizstabKw += ZahlOderNull(g, "Heizung") ?? 0; }
                        if (a.Bivalent) bivalent = true;
                    }
                    else if (a.Typ == TYP_BHKW)
                    {
                        double? lz = ZahlOderNull(g, ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT);
                        if (lz > 0)
                        {
                            mindestlaufzeit = Math.Max(mindestlaufzeit ?? 0, lz.Value);
                            teillast.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.MindestlaufzeitMin), PufferHerkunftsquelle.TEILLAST,
                                T("PAUS_HERK_TEILLAST_BHKW_LAUFZEIT", "Teillastfeld der Anlage: {0}.{1} = {2} min",
                                  ErzeugerTeillastSchema.TAB_BHKW, ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT, lz.Value)));
                        }
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
                            PufferBrennstoff art = BrennstoffAus(BrennstoffName(idProjekt, idB));
                            if (art != PufferBrennstoff.Keiner) { brennstoff = art; festbrennstoff = true; }
                        }
                    }
                    // V14: die Sperrfenster der Wärmepumpe (Tab_Sperrfenster) vor dem Altfenster.
                    if (a.Typ == TYP_WP && sperre.Count == 0)
                    {
                        List<Sperrfenster> tabelle = SperrfensterCtrl.Lesen(a.Id);
                        if (tabelle.Count > 0)
                            sperre = tabelle.Select(f => new PufferSperrfenster(f.VonH, f.DauerH)).ToList();
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
                H(T("PAUS_HERK_ERZEUGER", "Nennleistung {0} kW{1}{2}; Zweiterzeuger {3} kW{4}",
                    nenn,
                    n1 > 1 ? T("PAUS_HERK_ERZEUGER_SUMME", " (Summe aus {0} Anlagen)", n1) : Leer,
                    geregelt ? T("PAUS_HERK_ERZEUGER_GEREGELT", ", leistungsgeregelt") : Leer,
                    zweit,
                    nurHeizstab ? T("PAUS_HERK_ERZEUGER_HEIZSTAB", " (Heizstab)") : Leer));
            if (kollektor > 0)
                H(T("PAUS_HERK_KOLLEKTOR", "Kollektorfläche {0} m² (Bezugsfläche × Modulanzahl)", kollektor));
            h.AddRange(teillast);
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
            void H(string quelle, Textbaustein text) => h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Zapfprofil), quelle, text));
            PufferZapfprofil zp = null;
            bool generator = false;
            try { generator = ZapfprofilCtrl.Weg(idProjekt) == BrauchwasserWeg.Generator; }
            catch (Exception ex) { H(PufferHerkunftsquelle.ZAPFPROFIL, T("PAUS_HERK_ZAPF_WEG_FEHLT", "Weg nicht lesbar: {0}", ex.Message)); }

            if (generator)
            {
                try
                {
                    zp = AusGenerator(idProjekt, out Textbaustein text);
                    H(PufferHerkunftsquelle.ZAPFPROFIL, text);
                }
                catch (Exception ex)
                {
                    H(PufferHerkunftsquelle.ZAPFPROFIL, T("PAUS_HERK_ZAPF_NICHT_RECHENBAR", "Zapfprofil-Auslegung nicht rechenbar: {0}", ex.Message));
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
                    H(PufferHerkunftsquelle.REIHE, T("PAUS_HERK_ZAPF_REIHE", "D_max {0} kWh als Tagesmaximum der Brauchwasserreihe{1}", dmax,
                                                     generator ? Leer : T("PAUS_HERK_ZAPF_BESTANDSWEG", " (Bestandsweg: Trinkwasser über den Puffer)")));
                }
            }
            else if (brauchtReihe && !generator)
                H(PufferHerkunftsquelle.VORGABE, T("PAUS_HERK_ZAPF_OHNE_REIHE", "Bestandsweg: D_max erst mit den Bedarfsreihen"));
            return zp;
        }

        /// <summary>Das Zapfprofil-Ergebnis aus <c>ZapfprofilCtrl.Auslegung</c>, über die Topologiegruppen summiert.</summary>
        private static PufferZapfprofil AusGenerator(int idProjekt, out Textbaustein text)
        {
            if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we))
                throw new InvalidOperationException("Kein Klimakalender für das Projekt.");
            ZapfprofilStand stand = ZapfprofilCtrl.Lies(idProjekt);
            Auslegungsrechnung r = ZapfprofilCtrl.Auslegung(idProjekt, stand, jan1, we, new Auslegungslauf(null, null));
            return ZapfprofilAus(r, out text);
        }

        /// <summary>
        /// Das Ergebnis einer Zapfprofil-Auslegung als Eingang der Brauchwasserzone, über die
        /// Topologiegruppen summiert — derselbe Weg für den gespeicherten Stand (Vorbelegung) und für
        /// den Arbeitsstand, den der Zapfprofil-Auslegungsdialog mit „An Speicherauslegung übergeben…"
        /// herüberreicht (Stufe P2). Ohne Topologiegruppe wird benannt abgelehnt.
        /// </summary>
        internal static PufferZapfprofil ZapfprofilAus(Auslegungsrechnung r, out Textbaustein text)
        {
            if (r?.Ergebnis == null) throw new ArgumentNullException(nameof(r));
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
            text = T("PAUS_HERK_ZAPF_AUSLEGUNG", "Zapfprofil-Auslegung ({0}, {1}{2}, D_max {3} kWh)",
                     gruppen.Count > 1 ? T("PAUS_HERK_ZAPF_GRUPPEN", "{0} Topologiegruppen", gruppen.Count)
                                       : T("PAUS_HERK_ZAPF_GRUPPE", "{0} Topologiegruppe", gruppen.Count),
                     Topologietext(topo),
                     zp.NenninhaltL.HasValue ? T("PAUS_HERK_ZAPF_NENNINHALT", ", Nenninhalt {0} l", zp.NenninhaltL.Value) : Leer,
                     dmax);
            return zp;
        }

        /// <summary>Die Topologie der Brauchwasserzone als Baustein (Schlüssel <c>PAUS_TOPO_*</c> der Oberfläche).</summary>
        private static Textbaustein Topologietext(PufferBwTopologie t)
        {
            switch (t)
            {
                case PufferBwTopologie.Speicher: return T("PAUS_TOPO_SPEICHER", "Trinkwasserspeicher");
                case PufferBwTopologie.Frischwasser: return T("PAUS_TOPO_FRISCHWASSER", "Frischwasserstation");
                case PufferBwTopologie.Wohnungsstation: return T("PAUS_TOPO_WOHNUNGSSTATION", "Wohnungsstationen");
                default: return T("PAUS_TOPO_DURCHFLUSS", "Durchfluss ohne Speicher");
            }
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
                                                      T("PAUS_HERK_NENNINHALTE_PARAMETER", "Speicherauslegung.Nenninhalt.* ({0})", w.Quelle)));
                    return w.Liste.WerteL;
                }
            }
            catch (Exception)
            {
                // ohne Katalogversion der Tww-Parameter: die feste Liste des Rechenkerns
            }
            h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Nenninhalte), PufferHerkunftsquelle.VORGABE,
                                              T("PAUS_HERK_NENNINHALTE_VORGABE", "feste Liste 100 … 10 000 l, darüber Raster 1 000 l")));
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
        /// Die Nutzung der Konditionierung der Projektgebäude — allein aus der Kopie an den Kalendern des
        /// Gebäudes und seiner Zonen (<c>Tab_Konditionierungskalender.Nutzung</c>, Schemaschritt
        /// <see cref="KonditionierungNutzungSchema.SCHRITT"/>). Kein Verweis auf die Vorlage: Umbenennen, Löschen
        /// oder Katalogabgleich einer Vorlage ändern die Vorbelegung nicht.
        /// </summary>
        private static IEnumerable<string> Konditionierungsnutzungen(int idProjekt)
        {
            var l = new List<string>();
            if (!KonditionierungNutzungSchema.SchemaVollstaendig()) return l;
            DataTable t = DataRepository.GetDataTable(SQL_KONDITIONIERUNG, P("@projekt", idProjekt));
            if (t != null)
                foreach (DataRow r in t.Rows)
                {
                    string n = Text(r, "Nutzung");
                    if (!string.IsNullOrEmpty(n)) l.Add(n);
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
            void H(string feld, string spalte) => h.Add(new PufferAuslegungHerkunft(feld, PufferHerkunftsquelle.GESPEICHERT, Klar(PufferAuslegungSchema.TAB + "." + spalte)));

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

            // Die Sitzungseingaben (Welle P4c) - nur, wenn die Spalten stehen.
            bool Hat(string spalte) => z.Table.Columns.Contains(spalte);
            PufferAuslegungParameter kb = e.Parameter ?? PufferAuslegungParameter.Vorgabe();
            var kriterien = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (Hat(PufferAuslegungErgaenzungSchema.SPALTE_KRITERIEN) &&
                (v = ZahlOderNull(z, PufferAuslegungErgaenzungSchema.SPALTE_KRITERIEN)).HasValue)
            {
                foreach (KeyValuePair<string, bool> k in KriterienAusMaske((int)v.Value))
                    if (k.Value != kb.VorlageAn(e.Vorlage.ToString(), k.Key)) kriterien[k.Key] = k.Value;
                e = e with { Parameter = ParameterMitKriterien(kb, e.Vorlage, kriterien) };
                H("Kriterien", PufferAuslegungErgaenzungSchema.SPALTE_KRITERIEN);
            }
            if (Hat(PufferAuslegungErgaenzungSchema.SPALTE_EXPERTENWEG) &&
                (v = ZahlOderNull(z, PufferAuslegungErgaenzungSchema.SPALTE_EXPERTENWEG)).HasValue)
            { e = e with { SperrzeitExpertenweg = v.Value != 0 }; H(nameof(e.SperrzeitExpertenweg), PufferAuslegungErgaenzungSchema.SPALTE_EXPERTENWEG); }
            if (Hat(PufferAuslegungErgaenzungSchema.SPALTE_HEIZLAST) &&
                (v = ZahlOderNull(z, PufferAuslegungErgaenzungSchema.SPALTE_HEIZLAST)).HasValue)
            { e = e with { AuslegungsheizlastKw = v }; H(nameof(e.AuslegungsheizlastKw), PufferAuslegungErgaenzungSchema.SPALTE_HEIZLAST); }
            if (Hat(PufferAuslegungErgaenzungSchema.SPALTE_WOHNEINHEITEN) &&
                (v = ZahlOderNull(z, PufferAuslegungErgaenzungSchema.SPALTE_WOHNEINHEITEN)).HasValue)
            { e = e with { Wohneinheiten = v }; H(nameof(e.Wohneinheiten), PufferAuslegungErgaenzungSchema.SPALTE_WOHNEINHEITEN); }
            string stufe = Hat(PufferAuslegungErgaenzungSchema.SPALTE_ANZEIGESTUFE)
                ? Text(z, PufferAuslegungErgaenzungSchema.SPALTE_ANZEIGESTUFE) : null;

            return basis with
            {
                Eingang = e, Herkunft = h.AsReadOnly(), Gespeichert = true, IdZeile = (int)Zahl(z["ID"]),
                Kriterien = kriterien, KriterienBasis = kb, Anzeigestufe = stufe
            };
        }

        // =================================================================================
        //  Kriterienschalter (Welle P4c)
        // =================================================================================

        /// <summary>
        /// Die Bitmaske der Kriterienschalter einer Vorlage im Parametersatz: Bit i = Schalter i in der
        /// Reihenfolge von <see cref="PufferAuslegungVorgaben.VORLAGE_SCHALTER"/> (Bit 0 = K1).
        /// </summary>
        public static int KriterienMaske(PufferAuslegungParameter p, PufferVorlage vorlage)
        {
            p ??= PufferAuslegungParameter.Vorgabe();
            int m = 0;
            for (int i = 0; i < PufferAuslegungVorgaben.VORLAGE_SCHALTER.Count; i++)
                if (p.VorlageAn(vorlage.ToString(), PufferAuslegungVorgaben.VORLAGE_SCHALTER[i])) m |= 1 << i;
            return m;
        }

        /// <summary>Die Schalter einer Bitmaske (Kennung → an/aus), alle neun.</summary>
        public static IReadOnlyDictionary<string, bool> KriterienAusMaske(int maske)
        {
            var d = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < PufferAuslegungVorgaben.VORLAGE_SCHALTER.Count; i++)
                d[PufferAuslegungVorgaben.VORLAGE_SCHALTER[i]] = (maske & (1 << i)) != 0;
            return d;
        }

        /// <summary>Der Parametersatz mit den abweichenden Kriterienschaltern der Vorlage.</summary>
        public static PufferAuslegungParameter ParameterMitKriterien(PufferAuslegungParameter p, PufferVorlage vorlage,
                                                                     IReadOnlyDictionary<string, bool> kriterien)
        {
            p ??= PufferAuslegungParameter.Vorgabe();
            if (kriterien == null || kriterien.Count == 0) return p;
            var werte = new Dictionary<string, double>(p.Werte, StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> k in kriterien)
                if (PufferAuslegungVorgaben.VORLAGE_SCHALTER.Contains(k.Key))
                    werte[PufferAuslegungVorgaben.VorlageSchluessel(vorlage.ToString(), k.Key)] = k.Value ? 1 : 0;
            return PufferAuslegungParameter.Mit(werte);
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
        /// Empfehlung, das bemessende Kriterium und den Zeitpunkt. Dazu die Sitzungseingaben (Welle P4c):
        /// Kriterienschalter als Bitmaske, Expertenweg, Heizlast und Wohneinheiten nur bei Abweichung,
        /// die <paramref name="anzeigestufe"/> wie übergeben. Liefert die ID der Zeile; -1 bei Fehler.
        /// </summary>
        public static int Speichern(int idProjekt, int? idPuffer, PufferAuslegungEingang eingang, PufferAuslegungErgebnis ergebnis,
                                    string anzeigestufe = null)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (!DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB)) return -1;
            // Die Projektkopie der Vorgaben entsteht mit der ersten gespeicherten Auslegung - wertgleich
            // zum Stamm, die Vorbelegung unten liest also dieselben Werte wie davor.
            ProjektPufferparameter.Sichern(idProjekt);
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

            int zeile;
            DataRow vorhanden = ZeileLesen(idProjekt, idPuffer);
            if (vorhanden != null)
            {
                zeile = (int)Zahl(vorhanden["ID"]);
                werte.Add(P("@id", zeile));
                if (DataRepository.ExecuteNonQuery(SQL_ZEILE_AENDERN, werte.ToArray()) < 0) return -1;
            }
            else
            {
                if (DataRepository.ExecuteNonQuery(SQL_ZEILE_EINFUEGEN, werte.ToArray()) < 0) return -1;
                DataRow neu = ZeileLesen(idProjekt, idPuffer);
                if (neu == null) return -1;
                zeile = (int)Zahl(neu["ID"]);
            }
            return ErgaenzungSchreiben(zeile, eingang, b, anzeigestufe) ? zeile : -1;
        }

        /// <summary>
        /// Schreibt die Sitzungseingaben (Welle P4c) an die Zeile: NULL = Vorgabe. Ohne die Spalten (älterer
        /// Schemastand) bleibt es beim Bisherigen.
        /// </summary>
        private static bool ErgaenzungSchreiben(int zeile, PufferAuslegungEingang eingang, PufferAuslegungEingang b, string anzeigestufe)
        {
            if (!PufferAuslegungErgaenzungSchema.Vorhanden(PufferAuslegungSchema.TAB, PufferAuslegungErgaenzungSchema.SPALTE_KRITERIEN))
                return true;
            int maske = KriterienMaske(eingang.Parameter, eingang.Vorlage);
            int basis = KriterienMaske(b.Parameter, eingang.Vorlage);
            double? we = eingang.Wohneinheiten.HasValue ? Math.Round(eingang.Wohneinheiten.Value, MidpointRounding.AwayFromZero) : null;
            double? wb = b.Wohneinheiten.HasValue ? Math.Round(b.Wohneinheiten.Value, MidpointRounding.AwayFromZero) : null;
            string stufe = anzeigestufe != null && PufferAuslegungErgaenzungSchema.ANZEIGESTUFEN.Contains(anzeigestufe) ? anzeigestufe : null;
            return DataRepository.ExecuteNonQuery(SQL_ZEILE_ERGAENZUNG,
                PObj("@kriterien", DbParamTyp.Integer, maske != basis ? maske : (object)DBNull.Value),
                PObj("@experte", DbParamTyp.Integer, Abw(eingang.SperrzeitExpertenweg, b.SperrzeitExpertenweg)),
                PObj("@heizlast", DbParamTyp.Double, Abw(eingang.AuslegungsheizlastKw, b.AuslegungsheizlastKw)),
                PObj("@we", DbParamTyp.Integer, we.HasValue && we != wb ? (long)we.Value : (object)DBNull.Value),
                PObj("@stufe", DbParamTyp.VarWChar, (object)stufe ?? DBNull.Value),
                P("@id", zeile)) >= 0;
        }

        // =================================================================================
        //  Gespeicherte Auslegungen lesen (Bericht, Stufe P3)
        // =================================================================================

        /// <summary>
        /// Die gespeicherten Auslegungen des Projekts (eine je Zeile in <c>Tab_PufferAuslegung</c>, nach
        /// ID) mit Puffername und gewähltem Volumen. Mit <paramref name="nachrechnen"/> rechnet jede Zeile
        /// mit ihrer gespeicherten Eingabe und dem aktuellen Projektstand nach (Bedarfsreihen einmal je
        /// Projekt) und trägt Vorlage, Nutzungsprofil, Herkunft des bemessenden Kriteriums, Kennzahlen und
        /// Warnliste; misslingt die Nachrechnung, steht der Grund in <see cref="PufferAuslegungGespeichert.Fehlertext"/>.
        /// Nur lesend. Ohne Tabelle oder Zeile: leere Liste.
        /// </summary>
        public static IReadOnlyList<PufferAuslegungGespeichert> Gespeichert(int idProjekt, bool nachrechnen = true)
        {
            var liste = new List<PufferAuslegungGespeichert>();
            if (idProjekt <= 0 || !DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB)) return liste.AsReadOnly();
            DataTable t = DataRepository.GetDataTable(SQL_GESPEICHERT, P("@projekt", idProjekt));
            if (t == null || t.Rows.Count == 0) return liste.AsReadOnly();

            PufferAuslegungReihen reihen = null;
            if (nachrechnen)
            {
                try { reihen = Reihen(idProjekt); }
                catch (Exception ex) { reihen = new PufferAuslegungReihen(null, null, null, ex.Message); }
            }

            foreach (DataRow z in t.Rows)
            {
                double? idp = ZahlOderNull(z, "ID_Pufferspeicher");
                int? idPuffer = idp.HasValue ? (int)idp.Value : null;
                string np = Text(z, "Nutzungsprofil"), vo = Text(z, "Vorlage");
                DateTime? am = DateTime.TryParse(Text(z, "Berechnet_am"), CultureInfo.InvariantCulture,
                                                 DateTimeStyles.AssumeLocal, out DateTime d) ? d : null;
                var g = new PufferAuslegungGespeichert
                {
                    IdZeile = (int)Zahl(z["ID"]),
                    IdPuffer = idPuffer,
                    Puffername = Text(z, "Puffer_Bezeichner"),
                    GewaehltL = ZahlOderNull(z, "Puffer_Volumen"),
                    KlasseHeizung = Zahl(z["Klasse_Heizung"]) != 0,
                    KlasseBrauchwasser = Zahl(z["Klasse_Brauchwasser"]) != 0,
                    KlasseProzess = Zahl(z["Klasse_Prozess"]) != 0,
                    Vorlage = vo != null && Enum.TryParse(vo, out PufferVorlage vw) ? vw : null,
                    Nutzungsprofil = np != null && Enum.TryParse(np, out PufferNutzungsprofil nw) ? nw : null,
                    VolumenHeizungL = ZahlOderNull(z, "Volumen_H_l"),
                    VolumenBrauchwasserL = ZahlOderNull(z, "Volumen_B_l"),
                    VolumenProzessL = ZahlOderNull(z, "Volumen_P_l"),
                    EmpfehlungL = ZahlOderNull(z, "Volumen_Empfehlung_l"),
                    Bemessend = Text(z, "Bemessend"),
                    BerechnetAm = am
                };
                liste.Add(nachrechnen ? Nachrechnen(idProjekt, g, reihen) : g);
            }
            return liste.AsReadOnly();
        }

        /// <summary>Die Nachrechnung einer gespeicherten Zeile — Vorbelegung samt Zeile, dann der Rechenkern.</summary>
        private static PufferAuslegungGespeichert Nachrechnen(int idProjekt, PufferAuslegungGespeichert g, PufferAuslegungReihen reihen)
        {
            PufferAuslegungVorbelegung v;
            try { v = Vorbelegen(idProjekt, g.IdPuffer, reihen); }
            catch (Exception ex) { return g with { Fehlertext = ex.Message }; }

            g = g with
            {
                Vorlage = v.Eingang.Vorlage,
                Nutzungsprofil = v.Eingang.Nutzungsprofil ?? v.Nutzungsprofil?.Profil,
                NutzungsprofilHerkunft = g.Nutzungsprofil.HasValue ? null : v.Nutzungsprofil?.HerkunftBaustein
            };
            if (reihen == null || !reihen.Vorhanden) return g with { Fehlertext = reihen?.Fehlertext ?? "" };

            PufferAuslegungErgebnis r;
            try { r = Rechnen(v.Eingang); }
            catch (Exception ex) { return g with { Fehlertext = ex.Message }; }

            PufferZonenergebnis zone = g.BemessendeZone.HasValue ? r.Zone(g.BemessendeZone.Value) : null;
            PufferKriterium k = g.BemessendeKennung == null ? null : zone?.Kriterium(g.BemessendeKennung);
            PufferBetriebsbild bild = r.Zone(PufferZone.Heizung)?.Betriebsbild ?? r.Zone(PufferZone.Prozess)?.Betriebsbild;
            return g with
            {
                BemessendHerkunft = string.IsNullOrWhiteSpace(k?.Herkunft) ? null : k.HerkunftBaustein,
                NachgerechnetL = r.EmpfehlungL,
                StartsJeTag = bild?.StartsJeTag,
                ProbelaufStartsJeTag = PufferProbelaufCtrl.Letzter(idProjekt, g.IdPuffer)?.Rang1?.StartsJeTag,
                ProbelaufAm = PufferProbelaufCtrl.Letzter(idProjekt, g.IdPuffer)?.Zeitpunkt,
                VerlustKwhJeTag = r.Kennzahlen?.Verlust?.KwhJeTag,
                VerlustWJeK = r.Kennzahlen?.Verlust?.WJeK,
                Warnungen = r.Warnungen ?? Array.Empty<PufferWarnung>(),
                Nachbarstufen = PufferAuslegung.Nachbarstufen(v.Eingang, r).Stufen
            };
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
        /// liest); beim Neuanlegen gelten die Vorgaben. Mit <paramref name="katalogsatz"/> und einem
        /// Katalogvorschlag merkt der Puffer den Katalogsatz (<c>Tab_Pufferspeicher.ID_Stamm</c>, Welle P4c),
        /// sonst wird der Verweis leer. Liefert die Puffer-ID; -1, wenn nichts übernommen
        /// wurde (keine Empfehlung, Puffer nicht im Projekt, Schreibfehler).
        /// </summary>
        public static int Uebernehmen(int idProjekt, int? idPuffer, PufferAuslegungErgebnis ergebnis, string bezeichner,
                                      bool katalogsatz = true)
        {
            int id = UebernehmenOhneVerweis(idProjekt, idPuffer, ergebnis, bezeichner);
            if (id <= 0) return id;
            int? stamm = katalogsatz && ergebnis.Katalogvorschlag?.Id > 0 ? ergebnis.Katalogvorschlag.Id : null;
            return PufferSpCtrl.KatalogverweisSetzen(id, idProjekt, stamm) ? id : -1;
        }

        private static int UebernehmenOhneVerweis(int idProjekt, int? idPuffer, PufferAuslegungErgebnis ergebnis, string bezeichner)
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

        /// <summary>
        /// Schreibt das Sperrprofil der Auslegung an die Wärmepumpen des Projekts — <b>nur auf Zuruf</b>
        /// (Schalter „Sperrprofil an die Wärmepumpe schreiben“ im Übernahme-Block, Vorgabe aus). Jedes
        /// Fenster gilt an allen Tagen und sperrt den Heizstab mit; die Liste ersetzt die Fenster der
        /// Anlage, das Altfenster geht aus (<see cref="SperrfensterCtrl.Schreiben"/>).
        /// </summary>
        /// <returns>Die Zahl der beschriebenen Anlagen; -1 bei einem Schreibfehler.</returns>
        public static int SperrprofilSchreiben(int idProjekt, IReadOnlyList<PufferSperrfenster> fenster)
        {
            if (idProjekt <= 0 || !SperrfensterCtrl.TabelleVorhanden()) return 0;
            var liste = (fenster ?? Array.Empty<PufferSperrfenster>())
                .Where(f => f != null && f.DauerH > 0)
                .Select(f => new Sperrfenster { VonH = f.BeginnH, DauerH = Math.Min(24, f.DauerH) })
                .ToList();
            int n = 0;
            foreach (Anlage a in AnlagenLesen(idProjekt))
            {
                if (a.Typ != TYP_WP) continue;
                if (!SperrfensterCtrl.Schreiben(a.Id, liste)) return -1;
                n++;
            }
            return n;
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
