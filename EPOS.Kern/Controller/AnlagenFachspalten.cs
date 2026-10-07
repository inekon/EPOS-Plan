using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FACHSPALTEN von <c>Tab_Energieanlagen</c> und der EINE Weg, sie von einer
    /// bestehenden Anlagenzeile auf eine neu angelegte zu übertragen.
    ///
    /// <para><b>Was eine Fachspalte ist.</b> Jede Spalte, die die Tabelle JETZT führt und
    /// die Einfügeanweisung <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nicht nennt, ohne
    /// <c>ID</c> — das Komplement des Modells. Keine zweite Liste: Wer eine Spalte ins
    /// Modell aufnimmt, verkleinert die Menge von selbst; wer per Schemaschritt eine Spalte
    /// anlegt, ohne das Modell zu erweitern (Sondenfeld, KWKG, Steuer, Quellangaben), ist
    /// von selbst geschützt. Dieselbe Menge rettet der Speicherweg des Assistenten
    /// (<c>WizardCtrl.Fachspalten</c> leitet hierher weiter).</para>
    ///
    /// <para><b>Was nie übertragen wird.</b> Schlüssel und Projektbezug (<c>ID</c>,
    /// <c>ID_Projekt</c>) und die Ergebnisspalten (<see cref="ERGEBNIS"/>): Werte, die die
    /// Simulation schreibt, gehören zum Lauf des Quellprojekts, nicht zur Anlage. Die
    /// Tabelle führt gegenwärtig keine — die Ergebnisse stehen in eigenen Tabellen —, die
    /// Menge hält den Platz, und die Wache <c>FachspaltenEinordnungWacheTests</c> verlangt
    /// für jede neue Spalte eine bewusste Einordnung.</para>
    ///
    /// <para><b>Verweise auf projekteigene Zeilen</b> (<see cref="PROJEKTBEZUG"/>):
    /// Innerhalb eines Projekts unverändert; über Projektgrenzen auf die gleichnamige
    /// Zeile des Zielprojekts abgebildet (Muster <c>KomponentenUebernahmeCtrl</c> bei den
    /// Pufferverweisen), sonst als Projektkopie samt Kindzeilen ins Ziel übernommen — nie ein Verweis in ein fremdes Projekt.</para>
    /// </summary>
    internal static class AnlagenFachspalten
    {
        private const string TABELLE = "Tab_Energieanlagen";

        /// <summary>Schlüssel und Projektbezug — nie übertragen.</summary>
        public static readonly HashSet<string> AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID", "ID_Projekt" };

        /// <summary>
        /// Ergebnisspalten der Simulation — nie übertragen. Gegenwärtig leer: Die Simulation
        /// schreibt keine Spalte von <c>Tab_Energieanlagen</c>.
        /// </summary>
        public static readonly HashSet<string> ERGEBNIS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fachspalten, die auf eine PROJEKTEIGENE Zeile zeigen (Spalte → Tabelle mit
        /// <c>ID_Projekt</c> und <c>Bezeichner</c>).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PROJEKTBEZUG =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "WQ_ID_Quellprofil", "Tab_Quellprofil" },
                { KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE, KaeltemaschineSchema.TAB_PROJEKT }
            };

        /// <summary>Die Spalten, die <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nennt - einmal aus der Anweisung gelesen.</summary>
        private static HashSet<string> m_InsertSpalten;

        /// <summary>Die Modellspalten: die Spalten der vollständigen Einfügeanweisung.</summary>
        public static HashSet<string> Modellspalten()
        {
            if (m_InsertSpalten != null) return m_InsertSpalten;

            string sql = AnlagenSql.SQL_ANLAGE_INSERT;
            HashSet<string> menge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int auf = sql.IndexOf('(');
            int zu = auf >= 0 ? sql.IndexOf(')', auf) : -1;
            if (auf >= 0 && zu > auf)
            {
                foreach (string s in sql.Substring(auf + 1, zu - auf - 1).Split(','))
                {
                    string name = s.Trim();
                    if (name.Length > 0) menge.Add(name);
                }
            }
            m_InsertSpalten = menge;
            return menge;
        }

        /// <summary>
        /// Die Fachspalten: alle Spalten von <c>Tab_Energieanlagen</c>, die
        /// <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nicht nennt, ohne <c>ID</c> - in
        /// Schemareihenfolge. Leer, wenn die Tabelle nur die Modellspalten führt.
        /// </summary>
        public static List<string> Fachspalten()
        {
            List<string> fach = new List<string>();
            HashSet<string> insert = Modellspalten();
            foreach (string spalte in DataRepository.SpaltenVonTabelle(TABELLE))
            {
                if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                if (insert.Contains(spalte)) continue;
                fach.Add(spalte);
            }
            return fach;
        }

        /// <summary>
        /// Die Fachspalten, die eine Übernahme von einer Quellzeile überträgt:
        /// <see cref="Fachspalten"/> ohne <see cref="AUSSCHLUSS"/> und <see cref="ERGEBNIS"/>.
        /// Liest das Schema — deshalb VOR einem offenen Vorgang erfragen.
        /// </summary>
        public static List<string> UebertragbareSpalten()
        {
            List<string> liste = new List<string>();
            foreach (string spalte in Fachspalten())
                if (!AUSSCHLUSS.Contains(spalte) && !ERGEBNIS.Contains(spalte))
                    liste.Add(spalte);
            return liste;
        }

        /// <summary>
        /// Die Kindtabellen einer projekteigenen Verweistabelle aus <see cref="PROJEKTBEZUG"/>
        /// (Tabelle → Kindtabelle mit Fremdschlüssel), die eine Projektkopie mitnimmt.
        /// Ergebnistabellen (<c>Tab_ErgebnisKaeltemaschine</c>) gehören zum Lauf und stehen in
        /// <see cref="KIND_AUSSCHLUSS"/>.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string Tabelle, string Fk)[]> PROJEKTKINDER =
            new Dictionary<string, (string Tabelle, string Fk)[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Tab_Quellprofil", new[] { ("Tab_QuellprofilDaten", "ID_Quellprofil") } },
                { KaeltemaschineSchema.TAB_PROJEKT, new[] { ("Tab_Kenndaten_Kaeltemaschine",
                                                             KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE) } }
            };

        /// <summary>Tabellen, die auf eine Verweistabelle zeigen, aber nie mitkopiert werden (Ergebnisse).</summary>
        public static readonly HashSet<string> KIND_AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Tab_Energieanlagen", "Tab_ErgebnisKaeltemaschine" };

        /// <summary>
        /// Die Geräteverweise einer Anlagenzeile: die Fremdschlüssel auf die Projektkopie des
        /// Geräts. Eine KOPIE der Zeile (<see cref="KopieSpalten"/>) lässt den Verweis aus,
        /// den sie selbst neu setzt.
        /// </summary>
        public static readonly HashSet<string> GERAETEVERWEISE =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ID_WP", "ID_SP", "ID_PV", "ID_Solar", "ID_Kessel", "ID_BHKW", "ID_PUFFER",
              KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE };

        /// <summary>
        /// Was eine vollständige KOPIE der Zeile außer <see cref="AUSSCHLUSS"/> und
        /// <see cref="ERGEBNIS"/> nie überträgt: den Bezeichner (die Kopie trägt ihren eigenen).
        /// Dazu kommt der Geräteverweis, den der Aufrufer neu setzt.
        /// </summary>
        public static readonly HashSet<string> KOPIE_AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Bezeichner" };

        /// <summary>
        /// Die Spalten einer vollständigen KOPIE der Anlagenzeile (Anwenderentscheid
        /// 07.10.2026, weitere Stücke der Flottenstudie): JEDE Spalte des Schemas — Modell-
        /// wie Fachspalten — ohne <see cref="AUSSCHLUSS"/>, <see cref="ERGEBNIS"/>,
        /// <see cref="KOPIE_AUSSCHLUSS"/> und den Geräteverweis <paramref name="geraeteverweis"/>.
        /// Liest das Schema — deshalb VOR einem offenen Vorgang erfragen.
        /// </summary>
        public static List<string> KopieSpalten(string geraeteverweis)
        {
            if (!GERAETEVERWEISE.Contains(geraeteverweis ?? ""))
                throw new ArgumentException("Kein Geräteverweis: " + geraeteverweis, nameof(geraeteverweis));

            List<string> liste = new List<string>();
            foreach (string spalte in DataRepository.SpaltenVonTabelle(TABELLE))
            {
                if (AUSSCHLUSS.Contains(spalte) || ERGEBNIS.Contains(spalte) || KOPIE_AUSSCHLUSS.Contains(spalte))
                    continue;
                if (string.Equals(spalte, geraeteverweis, StringComparison.OrdinalIgnoreCase)) continue;
                liste.Add(spalte);
            }
            return liste;
        }

        /// <summary>Ausgang einer Übertragung: Projektverweise, die ins Ziel kopiert wurden bzw. leer blieben.</summary>
        public sealed class Ausgang
        {
            /// <summary>Projekteigene Zeilen (Quellprofil, Kältemaschine), die als Projektkopie ins Ziel kamen.</summary>
            public int Kopiert;
            /// <summary>Projektverweise ohne Entsprechung im Ziel, die leer bleiben.</summary>
            public int Verloren;
        }

        /// <summary>
        /// Überträgt die Spalten <paramref name="spalten"/> der Anlagenzeile
        /// <paramref name="idQuelle"/> auf die eben angelegte Zeile <paramref name="idZiel"/> —
        /// EIN UPDATE mit Zeilenwert-Zuweisung aus der Quellzeile, NULL eingeschlossen (sonst
        /// trüge die neue Zeile die Vorgabe ihrer Spalte statt des Quellwerts).
        ///
        /// <para>Verweise aus <see cref="PROJEKTBEZUG"/>: im selben Projekt unverändert;
        /// über Projektgrenzen auf die gleichnamige Zeile des Zielprojekts (kleinste ID bei
        /// Namensdoppeln); fehlt sie, wird die Quellzeile samt <see cref="PROJEKTKINDER"/> als
        /// Projektkopie ins Ziel übernommen (Anwenderentscheid 07.10.2026). Eine zweite Anlage
        /// mit demselben Verweis findet danach die Kopie als gleichnamige Zeile.</para>
        /// </summary>
        /// <param name="v">Der offene Vorgang der Übernahme — die neue Zeile ist nur dort sichtbar.</param>
        /// <param name="spalten">Aus <see cref="UebertragbareSpalten"/> oder <see cref="KopieSpalten"/>,
        /// vor dem Vorgang erfragt. Spaltennamen stammen aus dem Schema, nie aus einer Eingabe.</param>
        public static Ausgang Uebertragen(DbVorgang v, IReadOnlyList<string> spalten, int idQuelle, int idZiel)
        {
            var ausgang = new Ausgang();
            if (v == null || spalten == null || spalten.Count == 0 || idQuelle <= 0 || idZiel <= 0 ||
                idQuelle == idZiel)
                return ausgang;

            DataTable projekte = v.Lese("SELECT q.ID_Projekt AS PQ, z.ID_Projekt AS PZ FROM " + TABELLE + " q, " +
                                        TABELLE + " z WHERE q.ID = ? AND z.ID = ?",
                                        new DbParam("@q", idQuelle), new DbParam("@z", idZiel));
            if (projekte == null || projekte.Rows.Count == 0 || projekte.Rows[0]["PZ"] == DBNull.Value)
                return ausgang;
            int projektZiel = Convert.ToInt32(projekte.Rows[0]["PZ"], CultureInfo.InvariantCulture);
            bool gleichesProjekt = projekte.Rows[0]["PQ"] != DBNull.Value &&
                Convert.ToInt32(projekte.Rows[0]["PQ"], CultureInfo.InvariantCulture) == projektZiel;

            var direkt = new List<string>();
            var bezuege = new List<string>();
            foreach (string spalte in spalten)
            {
                if (AUSSCHLUSS.Contains(spalte) || ERGEBNIS.Contains(spalte)) continue;
                if (!gleichesProjekt && PROJEKTBEZUG.ContainsKey(spalte)) bezuege.Add(spalte);
                else direkt.Add(spalte);
            }

            if (direkt.Count > 0)
            {
                var ziel = new List<string>();
                var quelle = new List<string>();
                foreach (string spalte in direkt)
                {
                    ziel.Add("[" + spalte + "]");
                    quelle.Add("q.[" + spalte + "]");
                }
                v.Ausfuehren("UPDATE " + TABELLE + " SET (" + string.Join(", ", ziel) + ") = (SELECT " +
                             string.Join(", ", quelle) + " FROM " + TABELLE + " q WHERE q.ID = ?) WHERE ID = ?",
                             new DbParam("@q", idQuelle), new DbParam("@z", idZiel));
            }

            foreach (string spalte in bezuege)
            {
                string tabelle = PROJEKTBEZUG[spalte];
                object wert = v.Skalar("SELECT [" + spalte + "] FROM " + TABELLE + " WHERE ID = ?",
                                       new DbParam("@q", idQuelle));
                object neu = DBNull.Value;
                if (wert != null)
                {
                    int idVerweis = Convert.ToInt32(wert, CultureInfo.InvariantCulture);
                    object namensgleich = v.Skalar(
                        "SELECT pz.ID FROM [" + tabelle + "] pz JOIN [" + tabelle + "] pq " +
                        "ON pq.Bezeichner = pz.Bezeichner WHERE pq.ID = ? AND pz.ID_Projekt = ? " +
                        "ORDER BY pz.ID LIMIT 1",
                        new DbParam("@v", idVerweis), new DbParam("@p", projektZiel));
                    if (namensgleich != null)
                    {
                        neu = namensgleich;
                    }
                    else
                    {
                        int kopie = ProjektzeileKopieren(v, tabelle, idVerweis, projektZiel);
                        if (kopie > 0) { neu = kopie; ausgang.Kopiert++; }
                        else ausgang.Verloren++;
                    }
                }
                v.Ausfuehren("UPDATE " + TABELLE + " SET [" + spalte + "] = ? WHERE ID = ?",
                             new DbParam("@w", neu), new DbParam("@z", idZiel));
            }
            return ausgang;
        }

        /// <summary>
        /// Kopiert die projekteigene Zeile <paramref name="id"/> von <paramref name="tabelle"/>
        /// samt ihren <see cref="PROJEKTKINDER"/> in das Projekt <paramref name="projektZiel"/> —
        /// ganze Zeile außer <c>ID</c>, <c>ID_Projekt</c> auf das Ziel, Kindzeilen mit neuem
        /// Fremdschlüssel. Spalten aus dem Schema (im Vorgang gelesen), Werte über Parameter.
        /// </summary>
        /// <returns>Die ID der Kopie; 0, wenn die Quellzeile fehlt.</returns>
        private static int ProjektzeileKopieren(DbVorgang v, string tabelle, int id, int projektZiel)
        {
            List<string> spalten = SpaltenImVorgang(v, tabelle);
            var ziel = new List<string>();
            var quelle = new List<string>();
            var ps = new List<DbParam>();
            foreach (string spalte in spalten)
            {
                if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                ziel.Add("[" + spalte + "]");
                if (string.Equals(spalte, "ID_Projekt", StringComparison.OrdinalIgnoreCase))
                {
                    quelle.Add("?");
                    ps.Add(new DbParam("@p", projektZiel));
                }
                else quelle.Add("[" + spalte + "]");
            }
            ps.Add(new DbParam("@id", id));
            if (v.Ausfuehren("INSERT INTO [" + tabelle + "] (" + string.Join(", ", ziel) + ") SELECT " +
                             string.Join(", ", quelle) + " FROM [" + tabelle + "] WHERE ID = ?", ps.ToArray()) != 1)
                return 0;
            int neu = Convert.ToInt32(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);

            if (PROJEKTKINDER.TryGetValue(tabelle, out (string Tabelle, string Fk)[] kinder))
            {
                foreach ((string kind, string fk) in kinder)
                {
                    var kz = new List<string>();
                    var kq = new List<string>();
                    var kp = new List<DbParam>();
                    foreach (string spalte in SpaltenImVorgang(v, kind))
                    {
                        if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                        kz.Add("[" + spalte + "]");
                        if (string.Equals(spalte, fk, StringComparison.OrdinalIgnoreCase))
                        { kq.Add("?"); kp.Add(new DbParam("@fk", neu)); }
                        else if (string.Equals(spalte, "ID_Projekt", StringComparison.OrdinalIgnoreCase))
                        { kq.Add("?"); kp.Add(new DbParam("@p", projektZiel)); }
                        else kq.Add("[" + spalte + "]");
                    }
                    kp.Add(new DbParam("@alt", id));
                    v.Ausfuehren("INSERT INTO [" + kind + "] (" + string.Join(", ", kz) + ") SELECT " +
                                 string.Join(", ", kq) + " FROM [" + kind + "] WHERE [" + fk + "] = ? ORDER BY ID",
                                 kp.ToArray());
                }
            }
            return neu;
        }

        /// <summary>
        /// Die KINDTABELLEN einer Anlagenzeile, die eine vollständige Kopie der Anlage
        /// mitnimmt (Anwenderentscheid 07.10.2026, weitere Stücke der Flottenstudie), in
        /// Kopierreihenfolge: Tabelle → Spalte des Verweises auf <c>Tab_Energieanlagen</c>.
        /// <list type="bullet">
        /// <item><c>Tab_StromspeicherVariante</c> — Betriebsführung des Stromspeichers;</item>
        /// <item><c>Z_AnlageSenke</c> — Senken samt Lade-Prioritäten. Den Puffer TEILT die Kopie
        /// mit der Quelle (mehrere Erzeuger an einem Puffer sind die Regel);</item>
        /// <item><c>Z_AnlagePufferVerbund</c> — Parallelverbund; <c>ID_Senke</c> wird auf die
        /// kopierte Senke umgeschlüsselt (<see cref="ANLAGENKIND_UMSCHLUESSEL"/>), deshalb NACH den Senken;</item>
        /// <item><c>Z_AnlageStrang</c> — Stränge; sie gehören der Anlage (Gruppierung je Anlage,
        /// Wechselrichter, Gerätenummer), die Kopie bekommt eigene Stränge mit denselben Namen;</item>
        /// <item><c>Tab_Sperrfenster</c> — Sperrprofil;</item>
        /// <item><c>Tab_ProjektWerte</c> — Kostenpositionen der Anlage (Anwenderentscheid
        /// 07.10.2026): Beträge und Mengen nach Kapazität skaliert
        /// (<see cref="ANLAGENKIND_SKALIERT"/>) — außer Positionen mit Energiekostenbezug
        /// (<c>BemessungKatalog.ENERGIEKOSTEN_BEZUG</c>), die unverändert mitgehen —, der Geräteanker auf das Gerät der Kopie
        /// umgeschlüsselt (<see cref="ANLAGENKIND_GERAETEANKER"/>).</item>
        /// </list>
        /// Verweise der Kindzeilen auf andere projekteigene Zeilen (Puffer, Wechselrichter,
        /// PV-Modul) bleiben im selben Projekt gleich (Flottenstudie); über Projektgrenzen
        /// (Komponentenübernahme) werden sie nach <see cref="ANLAGENKIND_PROJEKTBEZUG"/>
        /// umgeschlüsselt (<see cref="AnlagenkinderUebertragen"/>).
        /// </summary>
        public static readonly (string Tabelle, string Fk)[] ANLAGENKINDER =
        {
            ("Tab_StromspeicherVariante", "ID_Energieanlage"),
            ("Z_AnlageSenke", "ID_Anlage"),
            ("Z_AnlagePufferVerbund", "ID_Anlage"),
            ("Z_AnlageStrang", "ID_Anlage"),
            ("Tab_Sperrfenster", "ID_Energieanlage"),
            ("Tab_ProjektWerte", "ID_Anlage")
        };

        /// <summary>
        /// Zahlenspalten einer Kindzeile („Tabelle.Spalte“), die die Kopie mit dem
        /// KOSTENFAKTOR (Kapazität des Stücks ÷ Kapazität der vertretenen Anlage) skaliert.
        /// <para><b>Welche.</b> Der Bestand kennzeichnet keine Pauschalen; die Bemessungsart
        /// (<c>BemessungKatalog</c>) unterscheidet nur FESTE Arten (Betrag in
        /// <c>EingegebenerWert</c>) von ABGELEITETEN (Betrag = <c>Menge</c> × Satz). Skaliert
        /// werden deshalb alle Beträge (<c>EingegebenerWert</c> samt Bandbreite
        /// <c>Worstcase</c>/<c>Bestcase</c> — bei abgeleiteten Arten der erfasste
        /// Rückfallbetrag) und die Bezugsgröße <c>Menge</c>; der SATZ
        /// (<c>Einheitpreis</c>) und die Nutzungsdauern bleiben (<see cref="ANLAGENKIND_UNSKALIERT"/>).
        /// So wächst der Betrag jeder Art mit demselben Faktor.</para>
        /// <para><b>Ausnahme.</b> Positionen, deren Bemessungsart die Energiekosten bzw. den
        /// Endenergieeinsatz zur Bezugsgröße hat (<c>BemessungKatalog.ENERGIEKOSTEN_BEZUG</c>),
        /// bleiben unskaliert (Anwenderentscheid 07.10.2026): ihre Bezugsgröße hängt nicht an
        /// der Baugröße der Anlage.</para>
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_SKALIERT =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Tab_ProjektWerte.EingegebenerWert", "Tab_ProjektWerte.Worstcase",
                "Tab_ProjektWerte.Bestcase", "Tab_ProjektWerte.Menge"
            };

        /// <summary>
        /// Zahlenspalten (REAL) einer skalierten Kindtabelle, die die Kopie bewusst
        /// UNVERÄNDERT übernimmt: Satz und Nutzungsdauern hängen nicht von der Größe ab.
        /// Jede REAL-Spalte von <c>Tab_ProjektWerte</c> steht hier oder in
        /// <see cref="ANLAGENKIND_SKALIERT"/> (Wache <c>FachspaltenEinordnungWacheTests</c>).
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_UNSKALIERT =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Tab_ProjektWerte.Einheitpreis", "Tab_ProjektWerte.Nutzungsdauer",
                "Tab_ProjektWerte.Worstcase_Nutzungsdauer", "Tab_ProjektWerte.Bestcase_Nutzungsdauer"
            };

        /// <summary>
        /// Geräteanker einer Kindzeile („Tabelle.Spalte“): Er zeigt auf den Gerätedatensatz
        /// der Anlage (<see cref="GERAETESPALTEN"/>). In der Kopie zeigt er auf das Gerät der
        /// KOPIE, wo es ein anderes ist (Stromspeicher: neue Gerätezeile); sonst bleibt er.
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_GERAETEANKER =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Tab_ProjektWerte.ID_AnlageGeraet" };

        /// <summary>Die Geräteverweise einer Anlagenzeile — dieselben wie im
        /// Ankernachzug der Kostenpositionen (<c>KostenProjektPositionenCtrl.AnkerNachziehen</c>).</summary>
        public static readonly string[] GERAETESPALTEN =
            { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        /// <summary>
        /// Verweise einer Kindzeile auf eine ANDERE Kindzeile derselben Anlage
        /// („Tabelle.Spalte“ → Kindtabelle): Sie zeigen in der Kopie auf die kopierte Zeile.
        /// Ein Wert ohne Entsprechung (fremde Anlage) bleibt unverändert.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> ANLAGENKIND_UMSCHLUESSEL =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Z_AnlagePufferVerbund.ID_Senke", "Z_AnlageSenke" }
            };

        /// <summary>
        /// Verweise einer Kindzeile auf eine PROJEKTGEBUNDENE Zeile außerhalb der Anlage
        /// („Tabelle.Spalte“ → Zieltabelle mit <c>ID_Projekt</c> und <c>Bezeichner</c>).
        /// Im selben Projekt bleiben sie gleich; über Projektgrenzen zeigen sie auf die
        /// GLEICHNAMIGE Zeile gleicher Art (<see cref="PROJEKTBEZUG_ART"/>) des Zielprojekts —
        /// nur wenn es genau eine gibt, oder nach der Abbildung des Aufrufers
        /// (<see cref="KinderAuftrag.Abbildung"/>). Ohne Gegenstelle bleibt der Verweis leer,
        /// bei einem <see cref="ANLAGENKIND_TRAGEND"/>en Verweis fällt die Kindzeile weg; beides
        /// wird gemeldet (<see cref="KinderAusgang.FehlendeBezuege"/>).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> ANLAGENKIND_PROJEKTBEZUG =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Z_AnlageSenke.ID_Puffer", "Tab_Pufferspeicher" },
                { "Z_AnlagePufferVerbund.ID_Puffer", "Tab_Pufferspeicher" },
                { "Z_AnlageStrang.ID_Wechselrichter", "Tab_Wechselrichter" },
                { "Z_AnlageStrang.ID_PV", "Tab_PV" },
                { "Tab_StromspeicherVariante.ID_Preisreihe", "Tab_Preisreihe" },
                { "Tab_StromspeicherVariante.ID_Kostenprofil", "Tab_Kostenprofil" }
            };

        /// <summary>
        /// Projektverweise, ohne die die Kindzeile keinen Sinn trägt: Fehlt die Gegenstelle im
        /// Zielprojekt, wird die ganze Kindzeile weggelassen (ein Parallelverbund ohne Puffer).
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_TRAGEND =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Z_AnlagePufferVerbund.ID_Puffer" };

        /// <summary>
        /// Die Art, die eine Gegenstelle außer dem Bezeichner teilen muss (Zieltabelle →
        /// Spalte): ein Wärme- und ein Kältepuffer gleichen Namens sind verschiedene Speicher.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PROJEKTBEZUG_ART =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Tab_Pufferspeicher", "Verwendung" }
            };

        /// <summary>
        /// Projektspalten einer Kindzeile („Tabelle.Spalte“): Sie tragen das Projekt selbst und
        /// zeigen über Projektgrenzen auf das Zielprojekt.
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_PROJEKTSPALTE =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Tab_ProjektWerte.ProjektID" };

        /// <summary>
        /// Verweisspalten einer Kindzeile („Tabelle.Spalte“), die auf PROJEKTFREIE Zeilen
        /// zeigen (Kostenkatalog, Vorlagen, Nutzungsdauern) und deshalb über Projektgrenzen
        /// unverändert mitgehen. Jede Verweisspalte einer Anlagenkind-Tabelle steht hier, in
        /// <see cref="ANLAGENKIND_PROJEKTBEZUG"/>, <see cref="ANLAGENKIND_PROJEKTSPALTE"/>,
        /// <see cref="ANLAGENKIND_UMSCHLUESSEL"/>, <see cref="ANLAGENKIND_GERAETEANKER"/> oder ist
        /// der Anlagenverweis selbst (Wache <c>FachspaltenEinordnungWacheTests</c>).
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_PROJEKTFREI =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Tab_ProjektWerte.StammID", "Tab_ProjektWerte.KomponentenID", "Tab_ProjektWerte.KategorieID",
                "Tab_ProjektWerte.VorlageID", "Tab_ProjektWerte.NutzungsdauerID", "Tab_ProjektWerte.Gruppe"
            };

        /// <summary>
        /// Tabellen mit Verweis auf eine Anlagenzeile, die eine Kopie NICHT mitnimmt:
        /// Ergebnisse des Laufs (<c>Tab_ErgebnisErdreich</c>, <c>Tab_ErgebnisPufferspeicher</c>,
        /// <c>Tab_ErgebnisStromspeicher</c>), die gespeicherte Auslegungsstudie
        /// (<c>Tab_SpeicherAuslegung</c>, eindeutig je Projekt, Anlage und Bezeichner — die Studie
        /// gehört der vertretenen Anlage).
        /// </summary>
        public static readonly HashSet<string> ANLAGENKIND_AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Tab_ErgebnisErdreich", "Tab_ErgebnisPufferspeicher", "Tab_ErgebnisStromspeicher",
              "Tab_SpeicherAuslegung" };

        /// <summary>Spaltennamen, die ohne Fremdschlüssel auf eine Anlagenzeile zeigen.</summary>
        public static readonly HashSet<string> ANLAGENVERWEIS_SPALTEN =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID_Anlage", "ID_Energieanlage" };

        /// <summary>Auftrag an <see cref="AnlagenkinderUebertragen"/>.</summary>
        public sealed class KinderAuftrag
        {
            /// <summary>Faktor der <see cref="ANLAGENKIND_SKALIERT"/>-Spalten; 1 (oder ungültig) = unverändert.</summary>
            public double Kostenfaktor = 1.0;

            /// <summary>Kindtabellen, die dieser Aufrufer NICHT mitnimmt (die Komponentenübernahme
            /// lässt die Kostenpositionen stehen, Nutzerentscheidung 18.08.2026).</summary>
            public ISet<string> Ohne;

            /// <summary>
            /// Maßgebliche Abbildung „Zieltabelle → (Quell-ID → Ziel-ID)“ für Projektverweise
            /// (<see cref="ANLAGENKIND_PROJEKTBEZUG"/>), etwa die eben angelegten Gerätekopien
            /// oder die Pufferabbildung der Übernahme. Sie geht vor; was sie nicht kennt, geht auf
            /// die eindeutige gleichnamige Zeile gleicher Art — außer für die Tabellen aus
            /// <see cref="NurAbbildung"/>.
            /// </summary>
            public IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> Abbildung;

            /// <summary>Zieltabellen, für die allein <see cref="Abbildung"/> gilt (die Pufferabbildung
            /// der Übernahme kennt nur den VERBAUTEN Bestand; ein gleichnamiger Altbestand zählt nicht).</summary>
            public ISet<string> NurAbbildung;

            /// <summary>
            /// false (Vorgabe): eine scheiternde Kindzeile wirft und rollt mit dem Vorgang zurück
            /// (Flottenstudie). true: jede Kindtabelle läuft unter einem eigenen Sicherungspunkt;
            /// ein Scheitern nimmt nur diese Tabelle zurück und steht in
            /// <see cref="KinderAusgang.GescheiterteTabellen"/> (Komponentenübernahme, NL-Q2).
            /// </summary>
            public bool FehlerMelden;
        }

        /// <summary>Ausgang von <see cref="AnlagenkinderUebertragen"/>.</summary>
        public sealed class KinderAusgang
        {
            /// <summary>Kopierte Kindzeilen.</summary>
            public int Kopiert;
            /// <summary>Kindzeilen, die ohne ihren tragenden Projektverweis weggelassen wurden.</summary>
            public int Weggelassen;
            /// <summary>Bezeichner der Quellzeilen ohne Gegenstelle im Zielprojekt (je Bezeichner einmal).</summary>
            public List<string> FehlendeBezuege = new List<string>();
            /// <summary>Kindtabellen, deren Kopie scheiterte (nur mit <see cref="KinderAuftrag.FehlerMelden"/>).</summary>
            public List<string> GescheiterteTabellen = new List<string>();
        }

        /// <summary>
        /// Kopiert die <see cref="ANLAGENKINDER"/> der Anlage <paramref name="idQuelle"/> auf die
        /// eben angelegte Anlage <paramref name="idZiel"/> — Kurzform von
        /// <see cref="AnlagenkinderUebertragen"/> mit Kostenfaktor; ein scheiterndes INSERT wirft.
        /// </summary>
        /// <param name="v">Der offene Vorgang.</param>
        /// <param name="idQuelle">Die vertretene Anlage.</param>
        /// <param name="idZiel">Die eben angelegte Kopie.</param>
        /// <param name="kostenfaktor">Faktor der <see cref="ANLAGENKIND_SKALIERT"/>-Spalten;
        /// 1 (oder ein ungültiger Wert) = unverändert.</param>
        /// <returns>Die Zahl der kopierten Kindzeilen.</returns>
        public static int AnlagenkinderKopieren(DbVorgang v, int idQuelle, int idZiel, double kostenfaktor = 1.0)
        {
            return AnlagenkinderUebertragen(v, idQuelle, idZiel, new KinderAuftrag { Kostenfaktor = kostenfaktor }).Kopiert;
        }

        /// <summary>
        /// DER Kernweg der Kindzeilen: kopiert die <see cref="ANLAGENKINDER"/> der Anlage
        /// <paramref name="idQuelle"/> auf die eben angelegte Anlage <paramref name="idZiel"/> —
        /// je Zeile ein INSERT … SELECT mit neuer ID, Verweis auf die neue Anlage und
        /// <see cref="ANLAGENKIND_UMSCHLUESSEL"/>. Liegen beide Anlagen in VERSCHIEDENEN Projekten
        /// (Komponentenübernahme), werden die Projektverweise nach
        /// <see cref="ANLAGENKIND_PROJEKTBEZUG"/> umgeschlüsselt, die Projektspalten
        /// (<see cref="ANLAGENKIND_PROJEKTSPALTE"/>) auf das Zielprojekt gesetzt, und ein Verweis
        /// innerhalb der Anlage oder ein Geräteanker ohne Entsprechung bleibt leer, statt ins
        /// Quellprojekt zu zeigen. Läuft im Vorgang des Aufrufers. Eine Tabelle, die das Schema
        /// (noch) nicht kennt, wird übergangen.
        /// </summary>
        public static KinderAusgang AnlagenkinderUebertragen(DbVorgang v, int idQuelle, int idZiel, KinderAuftrag auftrag)
        {
            var ausgang = new KinderAusgang();
            if (v == null || idQuelle <= 0 || idZiel <= 0 || idQuelle == idZiel) return ausgang;
            auftrag = auftrag ?? new KinderAuftrag();
            double kostenfaktor = auftrag.Kostenfaktor;
            if (!double.IsFinite(kostenfaktor) || kostenfaktor <= 0.0) kostenfaktor = 1.0;
            Dictionary<long, long> anker = Geraeteanker(v, idQuelle, idZiel);

            DataTable projekte = v.Lese("SELECT q.ID_Projekt AS PQ, z.ID_Projekt AS PZ FROM " + TABELLE + " q, " +
                                        TABELLE + " z WHERE q.ID = ? AND z.ID = ?",
                                        new DbParam("@q", idQuelle), new DbParam("@z", idZiel));
            if (projekte == null || projekte.Rows.Count == 0 || projekte.Rows[0]["PZ"] == DBNull.Value) return ausgang;
            long projektZiel = Convert.ToInt64(projekte.Rows[0]["PZ"], CultureInfo.InvariantCulture);
            bool fremd = projekte.Rows[0]["PQ"] == DBNull.Value ||
                         Convert.ToInt64(projekte.Rows[0]["PQ"], CultureInfo.InvariantCulture) != projektZiel;
            // Zieltabelle → (Quell-ID → Ziel-ID oder null), je Übertragung einmal aufgelöst.
            var gegenstellen = new Dictionary<string, Dictionary<long, long?>>(StringComparer.OrdinalIgnoreCase);

            // Kindtabelle → (alte ID → neue ID), für die Umschlüsselung späterer Kinder.
            var zuordnung = new Dictionary<string, Dictionary<long, long>>(StringComparer.OrdinalIgnoreCase);
            int punkt = 0;
            foreach ((string tabelle, string fk) in ANLAGENKINDER)
            {
                if (auftrag.Ohne != null && auftrag.Ohne.Contains(tabelle)) continue;
                List<string> spalten = SpaltenImVorgang(v, tabelle);
                if (spalten.Count == 0) continue;
                var karte = new Dictionary<long, long>();
                zuordnung[tabelle] = karte;

                string sicherung = "Kindzeilen_" + (++punkt).ToString(CultureInfo.InvariantCulture);
                if (auftrag.FehlerMelden) v.Ausfuehren("SAVEPOINT " + sicherung);
                int kopiert = 0, weggelassen = 0;
                var fehlend = new List<string>();
                try
                {
                    DataTable quelle = v.Lese("SELECT * FROM [" + tabelle + "] WHERE [" + fk + "] = ? ORDER BY ID",
                                              new DbParam("@a", idQuelle));
                    foreach (DataRow zeile in quelle.Rows)
                    {
                        if (KindzeileKopieren(v, tabelle, fk, spalten, zeile, idZiel, projektZiel, fremd, kostenfaktor,
                                              anker, zuordnung, gegenstellen, auftrag.Abbildung, auftrag.NurAbbildung,
                                              fehlend, out long neu))
                        {
                            karte[Convert.ToInt64(zeile["ID"], CultureInfo.InvariantCulture)] = neu;
                            kopiert++;
                        }
                        else weggelassen++;
                    }
                    if (auftrag.FehlerMelden) v.Ausfuehren("RELEASE " + sicherung);
                }
                catch (Exception) when (auftrag.FehlerMelden)
                {
                    v.Ausfuehren("ROLLBACK TO " + sicherung);
                    v.Ausfuehren("RELEASE " + sicherung);
                    karte.Clear();
                    ausgang.GescheiterteTabellen.Add(tabelle);
                    continue;
                }
                ausgang.Kopiert += kopiert;
                ausgang.Weggelassen += weggelassen;
                foreach (string f in fehlend)
                    if (!ausgang.FehlendeBezuege.Contains(f)) ausgang.FehlendeBezuege.Add(f);
            }
            return ausgang;
        }

        /// <summary>
        /// Eine Kindzeile kopieren. false: Die Zeile wurde weggelassen, weil ihr tragender
        /// Projektverweis (<see cref="ANLAGENKIND_TRAGEND"/>) im Zielprojekt keine Gegenstelle hat.
        /// </summary>
        private static bool KindzeileKopieren(DbVorgang v, string tabelle, string fk, List<string> spalten, DataRow zeile,
                                              int idZiel, long projektZiel, bool fremd, double kostenfaktor,
                                              Dictionary<long, long> anker,
                                              Dictionary<string, Dictionary<long, long>> zuordnung,
                                              Dictionary<string, Dictionary<long, long?>> gegenstellen,
                                              IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> abbildung,
                                              ISet<string> nurAbbildung, List<string> fehlend, out long neueId)
        {
            neueId = 0;
            long alteId = Convert.ToInt64(zeile["ID"], CultureInfo.InvariantCulture);
            var ziel = new List<string>();
            var werte = new List<string>();
            var ps = new List<DbParam>();
            foreach (string spalte in spalten)
            {
                if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                string schluessel = tabelle + "." + spalte;
                ziel.Add("[" + spalte + "]");
                if (string.Equals(spalte, fk, StringComparison.OrdinalIgnoreCase))
                {
                    werte.Add("?");
                    ps.Add(new DbParam("@fk", idZiel));
                }
                else if (ANLAGENKIND_UMSCHLUESSEL.TryGetValue(schluessel, out string bezug))
                {
                    object alt = zeile[spalte];
                    object neu = alt;
                    if (alt != DBNull.Value)
                    {
                        if (zuordnung.TryGetValue(bezug, out Dictionary<long, long> k) &&
                            k.TryGetValue(Convert.ToInt64(alt, CultureInfo.InvariantCulture), out long n))
                            neu = n;
                        else if (fremd)
                            neu = DBNull.Value;      // zeigte ins Quellprojekt
                    }
                    werte.Add("?");
                    ps.Add(new DbParam("@u", neu));
                }
                else if (fremd && ANLAGENKIND_PROJEKTBEZUG.TryGetValue(schluessel, out string zieltabelle))
                {
                    object alt = zeile[spalte];
                    object neu = DBNull.Value;
                    if (alt != DBNull.Value)
                    {
                        long quellId = Convert.ToInt64(alt, CultureInfo.InvariantCulture);
                        long? gegen = Gegenstelle(v, zieltabelle, quellId, projektZiel, gegenstellen, abbildung, nurAbbildung);
                        if (gegen.HasValue) neu = gegen.Value;
                        else
                        {
                            string name = Bezeichnung(v, zieltabelle, quellId);
                            if (!fehlend.Contains(name)) fehlend.Add(name);
                            if (ANLAGENKIND_TRAGEND.Contains(schluessel)) return false;
                        }
                    }
                    werte.Add("?");
                    ps.Add(new DbParam("@b", neu));
                }
                else if (fremd && ANLAGENKIND_PROJEKTSPALTE.Contains(schluessel))
                {
                    werte.Add("?");
                    ps.Add(new DbParam("@p", projektZiel));
                }
                else if (kostenfaktor != 1.0 && ANLAGENKIND_SKALIERT.Contains(schluessel) &&
                         ZeileSkalierbar(tabelle, zeile))
                {
                    // NULL bleibt NULL (nicht gepflegt), sonst Wert × Faktor.
                    werte.Add("[" + spalte + "] * ?");
                    ps.Add(new DbParam("@f", kostenfaktor));
                }
                else if (ANLAGENKIND_GERAETEANKER.Contains(schluessel))
                {
                    object alt = zeile[spalte];
                    object neu = alt;
                    if (alt != DBNull.Value)
                    {
                        if (anker.TryGetValue(Convert.ToInt64(alt, CultureInfo.InvariantCulture), out long n)) neu = n;
                        else if (fremd) neu = DBNull.Value;
                    }
                    werte.Add("?");
                    ps.Add(new DbParam("@g", neu));
                }
                else werte.Add("[" + spalte + "]");
            }
            ps.Add(new DbParam("@id", alteId));
            if (v.Ausfuehren("INSERT INTO [" + tabelle + "] (" + string.Join(", ", ziel) + ") SELECT " +
                             string.Join(", ", werte) + " FROM [" + tabelle + "] WHERE ID = ?",
                             ps.ToArray()) != 1)
                throw new InvalidOperationException("Kindzeile " + tabelle + " " + alteId + " nicht kopiert.");
            neueId = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Die Gegenstelle der Quellzeile <paramref name="quellId"/> von <paramref name="tabelle"/>
        /// im Zielprojekt: nach der Abbildung des Aufrufers, sonst (außer für
        /// <paramref name="nurAbbildung"/>) die EINE Zeile gleichen Bezeichners und gleicher Art
        /// (<see cref="PROJEKTBEZUG_ART"/>); null ohne eindeutige.
        /// </summary>
        private static long? Gegenstelle(DbVorgang v, string tabelle, long quellId, long projektZiel,
                                         Dictionary<string, Dictionary<long, long?>> cache,
                                         IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> abbildung,
                                         ISet<string> nurAbbildung)
        {
            if (!cache.TryGetValue(tabelle, out Dictionary<long, long?> karte))
                cache[tabelle] = karte = new Dictionary<long, long?>();
            if (karte.TryGetValue(quellId, out long? bekannt)) return bekannt;

            long? ergebnis = null;
            if (abbildung != null && abbildung.TryGetValue(tabelle, out IReadOnlyDictionary<int, int> vorgabe) &&
                vorgabe != null && quellId <= int.MaxValue && vorgabe.TryGetValue((int)quellId, out int z))
            {
                ergebnis = z;
            }
            else if (nurAbbildung == null || !nurAbbildung.Contains(tabelle))
            {
                string art = PROJEKTBEZUG_ART.TryGetValue(tabelle, out string a) &&
                             SpaltenImVorgang(v, tabelle).Contains(a, StringComparer.OrdinalIgnoreCase)
                    ? " AND pz.[" + a + "] IS pq.[" + a + "]" : "";
                DataTable treffer = v.Lese(
                    "SELECT pz.ID FROM [" + tabelle + "] pz JOIN [" + tabelle + "] pq ON pq.Bezeichner = pz.Bezeichner" +
                    art + " WHERE pq.ID = ? AND pz.ID_Projekt = ? ORDER BY pz.ID LIMIT 2",
                    new DbParam("@q", quellId), new DbParam("@p", projektZiel));
                if (treffer.Rows.Count == 1)
                    ergebnis = Convert.ToInt64(treffer.Rows[0]["ID"], CultureInfo.InvariantCulture);
            }
            karte[quellId] = ergebnis;
            return ergebnis;
        }

        /// <summary>Der Bezeichner einer projektgebundenen Zeile für den Hinweis; „#ID“, wenn er fehlt.</summary>
        private static string Bezeichnung(DbVorgang v, string tabelle, long id)
        {
            object o = v.Skalar("SELECT Bezeichner FROM [" + tabelle + "] WHERE ID = ?", new DbParam("@id", id));
            string text = o == null ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? "#" + id.ToString(CultureInfo.InvariantCulture) : text;
        }

        /// <summary>
        /// Skaliert die Kostenpositionen (<c>Tab_ProjektWerte</c>) der Anlage
        /// <paramref name="anlageId"/> AN ORT UND STELLE mit <paramref name="faktor"/> — die
        /// <see cref="ANLAGENKIND_SKALIERT"/>-Spalten, nach derselben Regel wie die Kopie
        /// (<see cref="AnlagenkinderKopieren"/>): Positionen mit Energiekostenbezug
        /// (<c>BemessungKatalog.NachKapazitaetSkalierbar</c>) bleiben, NULL bleibt NULL.
        /// Gerufen, wenn eine Übernahme der Anlage eine neue Kapazität zurückschreibt
        /// (Anwenderentscheid 07.10.2026). Läuft im Vorgang der Übernahme.
        /// </summary>
        /// <param name="v">Der offene Vorgang.</param>
        /// <param name="anlageId">Die Anlage, deren Positionen wachsen oder schrumpfen.</param>
        /// <param name="faktor">Neue ÷ alte nutzbare Kapazität; 1 oder ungültig = nichts.</param>
        /// <returns>Die Zahl der skalierten Positionen.</returns>
        public static int KostenpositionenSkalieren(DbVorgang v, int anlageId, double faktor)
        {
            const string KOSTENTABELLE = "Tab_ProjektWerte";
            if (v == null || anlageId <= 0 || !double.IsFinite(faktor) || faktor <= 0.0 || faktor == 1.0) return 0;
            var vorhanden = new HashSet<string>(SpaltenImVorgang(v, KOSTENTABELLE), StringComparer.OrdinalIgnoreCase);
            List<string> spalten = ANLAGENKIND_SKALIERT
                .Where(s => s.StartsWith(KOSTENTABELLE + ".", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Substring(KOSTENTABELLE.Length + 1))
                .Where(vorhanden.Contains)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            if (spalten.Count == 0 || !vorhanden.Contains("ID_Anlage")) return 0;

            var ps = new List<DbParam>();
            foreach (string _ in spalten) ps.Add(new DbParam("@f", faktor));
            ps.Add(new DbParam("@a", anlageId));
            string bedingung = "";
            if (vorhanden.Contains("Bemessung"))
            {
                bedingung = " AND (Bemessung IS NULL OR Bemessung NOT IN (" +
                            string.Join(", ", BemessungKatalog.ENERGIEKOSTEN_BEZUG.Select(_ => "?")) + "))";
                foreach (string art in BemessungKatalog.ENERGIEKOSTEN_BEZUG) ps.Add(new DbParam("@b", art));
            }
            return v.Ausfuehren("UPDATE [" + KOSTENTABELLE + "] SET " +
                                string.Join(", ", spalten.Select(s => "[" + s + "] = [" + s + "] * ?")) +
                                " WHERE ID_Anlage = ?" + bedingung, ps.ToArray());
        }

        /// <summary>
        /// Darf die Kopie diese Kindzeile skalieren? Eine Kostenposition mit
        /// Energiekostenbezug nicht (Anwenderentscheid 07.10.2026) — ihre Bezugsgröße hängt
        /// nicht an der Baugröße; jede andere Zeile ja.
        /// </summary>
        private static bool ZeileSkalierbar(string tabelle, DataRow zeile)
        {
            if (!string.Equals(tabelle, "Tab_ProjektWerte", StringComparison.OrdinalIgnoreCase) ||
                !zeile.Table.Columns.Contains("Bemessung"))
                return true;
            object art = zeile["Bemessung"];
            return art == DBNull.Value ||
                   BemessungKatalog.NachKapazitaetSkalierbar(Convert.ToString(art, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Geräteverweis der Quelle → Geräteverweis der Kopie, nur wo sie sich unterscheiden
        /// (<see cref="GERAETESPALTEN"/>, die das Schema kennt).
        /// </summary>
        private static Dictionary<long, long> Geraeteanker(DbVorgang v, int idQuelle, int idZiel)
        {
            var karte = new Dictionary<long, long>();
            var vorhanden = new HashSet<string>(SpaltenImVorgang(v, "Tab_Energieanlagen"), StringComparer.OrdinalIgnoreCase);
            List<string> spalten = GERAETESPALTEN.Where(vorhanden.Contains).ToList();
            if (spalten.Count == 0) return karte;
            string liste = string.Join(", ", spalten.Select(sp => "[" + sp + "]"));
            DataTable q = v.Lese("SELECT " + liste + " FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@q", idQuelle));
            DataTable z = v.Lese("SELECT " + liste + " FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@z", idZiel));
            if (q.Rows.Count != 1 || z.Rows.Count != 1) return karte;
            foreach (string sp in spalten)
            {
                object a = q.Rows[0][sp], b = z.Rows[0][sp];
                if (a == DBNull.Value || b == DBNull.Value) continue;
                long alt = Convert.ToInt64(a, CultureInfo.InvariantCulture);
                long neu = Convert.ToInt64(b, CultureInfo.InvariantCulture);
                if (alt > 0 && alt != neu && !karte.ContainsKey(alt)) karte[alt] = neu;
            }
            return karte;
        }

        /// <summary>Die Spalten einer Tabelle — auf der Verbindung des Vorgangs gelesen.</summary>
        private static List<string> SpaltenImVorgang(DbVorgang v, string tabelle)
        {
            var liste = new List<string>();
            DataTable dt = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            foreach (DataRow r in dt.Rows) liste.Add(Convert.ToString(r["name"], CultureInfo.InvariantCulture));
            return liste;
        }
    }
}
