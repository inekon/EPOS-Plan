using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kopfzeile einer Kostenvorlage (<c>Tab_KostenVorlage</c>, Etappe KD1/KD2 —
    /// Konzept Kostendialoge Rev. 1.2, § 4.1).
    /// </summary>
    public sealed class KostenVorlageKopf
    {
        public int Id;
        public int KomponentenId;
        public int KategorieId;
        public string Name;
        public bool IstStandard;

        /// <summary>Auslieferungs-Seed: nur über „Speichern unter" kopierbar.</summary>
        public bool NurLesen;
    }

    /// <summary>Eine Position einer Kostenvorlage (<c>Tab_KostenVorlagePosition</c>).
    /// NULL heißt durchgängig „nicht gepflegt", nie 0.</summary>
    public sealed class KostenVorlagenPosition
    {
        public int Id;
        public int VorlageId;
        public int? StammId;
        public string Bezeichnung;
        public string Kostenart;
        public string Bemessung;
        public double? Satz;
        public double? BetragNetto;
        public bool IstErloes;
        public double? Nutzungsdauer;
        public double? EmpfehlungVon;
        public double? EmpfehlungBis;
        public int Sortierung;

        /// <summary>ETAPPE H3 (Schritt 59): Pflichtposition der Komponente — wandert
        /// bei jeder Übernahme in die Projektzeile (Löschsperre H1-2) und steuert die
        /// Auto-Anlage (H1-3). false, wenn die Spalte in einer nie migrierten
        /// Datenbank fehlt.</summary>
        public bool IstPflicht;

        /// <summary>STUFE S1 (Migrationsschritt 75): die POSITIONSART dieser Position —
        /// ein Verweis auf <c>Tab_Nutzungsdauer</c> (Konzept Nutzungsdauer/AfA 2.3).
        /// <c>null</c> heißt „keine Positionsart gepflegt" und fällt auf die
        /// Standardzeile der Technik zurück; die Auflösung steht in
        /// <see cref="NutzungsdauerCtrl.Vorgabe"/>.</summary>
        public int? NutzungsdauerId;

        /// <summary>ETAPPE E7c (Schritt E, Schritt 107): Ersatzbeschaffung führen?
        /// <c>null</c> = wie bisher. Gelesen und geschrieben über
        /// <see cref="ErsatzRestwertKennzeichen"/>; in der Projektzeile steht dieselbe
        /// Spalte an <c>Tab_ProjektWerte</c>.</summary>
        public bool? ErsatzFuehren;

        /// <summary>ETAPPE E7c (Schritt E): Restwert ansetzen? <c>null</c> = wie bisher.</summary>
        public bool? RestwertAnsetzen;
    }

    /// <summary>
    /// Datenzugriff der Kostenvorlagen (Etappe KD2, Konzept Kostendialoge Rev. 1.2,
    /// § 5): Lesen und Pflegen von <c>Tab_KostenVorlage</c>/<c>Tab_KostenVorlagePosition</c>.
    ///
    /// <para><b>Der Dialog rechnet und schreibt nicht selbst</b> (Hausmuster
    /// <c>Form_BkUebernahme</c>): Prüf- und Schreiblogik liegen hier, UI-frei und
    /// testbar. Alle Schreibwege prüfen den <c>ReadOnly</c>-Schutz der
    /// Auslieferungsvorlagen; IDs entstehen per MAX+1 (kein AutoWert, ADR-001).</para>
    /// </summary>
    public static class KostenVorlagenCtrl
    {
        // ------------------------------------------------------------------ Lesen ---

        /// <summary>
        /// Ä7 (Entscheidung Philipp 26.08.2026): Zur AUSWAHL stehen überall nur die
        /// sieben Anlagen-Komponenten des Projektbaums — dieselben Kacheln, die der
        /// Komponenten-Wizard anbietet. Die Erfassungsgruppen der KD1-Saat
        /// (Wärmezentrale, Bauliche Anlagen, Stromeinspeisung) bleiben als
        /// Datensätze samt Vorlagenpositionen erhalten — vorhandene Projektdaten
        /// dazu werden weiter gerechnet und berichtet —, werden aber nirgends mehr
        /// zur Auswahl angeboten.
        /// </summary>
        public static readonly string[] WaehlbareKomponenten =
        {
            DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE,
            DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
            DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK,
            DbWerte.KOSTEN_KOMPONENTE_SOLARTHERMIE,
            DbWerte.KOSTEN_KOMPONENTE_STROMSPEICHER,
            DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER,
            DbWerte.KOSTEN_KOMPONENTE_BHKW
        };

        /// <summary>true, wenn die Komponente zur Auswahl angeboten wird (Ä7).</summary>
        public static bool IstWaehlbar(string komponente)
        {
            foreach (string k in WaehlbareKomponenten)
                if (string.Equals(k, komponente, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// <b>Ist diese Kostenkomponente eine ERFASSUNGSGRUPPE?</b> (Anwenderentscheid
        /// K-WZ-1.) Eine Erfassungsgruppe hat keine Anlage im Projektbaum — sie sammelt
        /// Kosten, die zu keinem Gerät gehören (Wärmezentrale, Bauliche Anlagen,
        /// Stromeinspeisung). Sie rechnet in der Wirtschaftlichkeit mit, aber sie ist
        /// keine Anlagenzeile.
        ///
        /// <para><b>Die Regel steht hier und nicht in einer Schale.</b> Die Kostenseite
        /// trennt ihre Zeilen danach: Eine Erfassungsgruppe erscheint mit Kennzeichnung
        /// und Papierkorb, eine anlagenfähige Gruppe ohne gültige Zuordnung gelb. Stünde
        /// die Unterscheidung in der Oberfläche, hätte jede Schale ihre eigene.</para>
        ///
        /// <para>Sie ist das Gegenstück zu <see cref="IstWaehlbar"/>: Was im
        /// Komponenten-Wizard nicht zur Auswahl steht, kann keiner Anlage zugeordnet
        /// werden. Ein leerer Name ist keine Gruppe.</para>
        /// </summary>
        public static bool IstErfassungsgruppe(string komponente)
        {
            return !string.IsNullOrEmpty(komponente) && !IstWaehlbar(komponente);
        }

        /// <summary>Die wählbaren Kostenkomponenten (Ä7; ID, Name), Reihenfolge der Auslieferung.</summary>
        public static IList<KeyValuePair<int, string>> Komponenten()
        {
            var liste = new List<KeyValuePair<int, string>>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [" + SchemaKatalog.SPALTE_KK_KOMPONENTE + "] FROM [" +
                SchemaKatalog.TAB_KOSTENKOMPONENTE + "] ORDER BY [ID]");
            foreach (DataRow r in dt.Rows)
            {
                string name = Convert.ToString(r[1]);
                if (!IstWaehlbar(name)) continue;   // Ä7
                liste.Add(new KeyValuePair<int, string>(Convert.ToInt32(r[0]), name));
            }
            return liste;
        }

        /// <summary>Alle Varianten einer Komponente+Kategorie; Standard zuerst.</summary>
        public static IList<KostenVorlageKopf> Vorlagen(int komponentenId, int kategorieId)
        {
            var liste = new List<KostenVorlageKopf>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [" + SchemaKatalog.SPALTE_KV_NAME + "], [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "], [" +
                SchemaKatalog.SPALTE_KV_READONLY + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = ? AND [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] = ? ORDER BY [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "] DESC, [" +
                SchemaKatalog.SPALTE_KV_NAME + "]",
                new DbParam("@kid", komponentenId),
                new DbParam("@kat", kategorieId));
            foreach (DataRow r in dt.Rows)
                liste.Add(new KostenVorlageKopf
                {
                    Id = Convert.ToInt32(r[0]),
                    KomponentenId = komponentenId,
                    KategorieId = kategorieId,
                    Name = Convert.ToString(r[1]),
                    IstStandard = r[2] != DBNull.Value && Convert.ToBoolean(r[2]),
                    // Ä8: Flag nur noch Herkunftsmarker — die UI zeigt alles editierbar.
                    NurLesen = false,
                });
            return liste;
        }

        /// <summary>Positionen einer Vorlage in Rasterreihenfolge.</summary>
        public static IList<KostenVorlagenPosition> Positionen(int vorlageId)
        {
            var liste = new List<KostenVorlagenPosition>();
            // ETAPPE H3: IstPflicht (Schritt 59) nur lesen, wo die Spalte existiert —
            // eine nie migrierte Datenbank lieferte sonst einen Abfragefehler.
            bool mitPflicht = PflichtSpalteVorhanden();
            // STUFE S1 (Schritt 75): dieselbe tolerante Vorsorge fuer die Positionsart.
            bool mitNutzungsdauerId = NutzungsdauerCtrl.VerweisSpalteVorhanden(
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [" + SchemaKatalog.SPALTE_KVP_STAMMID + "], [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_SATZ + "], [" +
                SchemaKatalog.SPALTE_KVP_BETRAG_NETTO + "], [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "], [" +
                SchemaKatalog.SPALTE_KVP_NUTZUNGSDAUER + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "], [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]" +
                (mitPflicht ? ", [" + SchemaKatalog.SPALTE_KVP_IST_PFLICHT + "]" : "") +
                (mitNutzungsdauerId ? ", [" + NutzungsdauerSchema.SPALTE_VERWEIS + "]" : "") +
                " FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ? ORDER BY [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "], [ID]",
                new DbParam("@vid", vorlageId));
            foreach (DataRow r in dt.Rows)
                liste.Add(new KostenVorlagenPosition
                {
                    Id = Convert.ToInt32(r[0]),
                    VorlageId = vorlageId,
                    StammId = ZahlOderNull(r[1]),
                    Bezeichnung = Convert.ToString(r[2]),
                    Kostenart = Convert.ToString(r[3]),
                    Bemessung = Convert.ToString(r[4]),
                    Satz = WertOderNull(r[5]),
                    BetragNetto = WertOderNull(r[6]),
                    IstErloes = r[7] != DBNull.Value && Convert.ToBoolean(r[7]),
                    Nutzungsdauer = WertOderNull(r[8]),
                    EmpfehlungVon = WertOderNull(r[9]),
                    EmpfehlungBis = WertOderNull(r[10]),
                    Sortierung = r[11] == DBNull.Value ? 0 : Convert.ToInt32(r[11]),
                    IstPflicht = mitPflicht && r[12] != DBNull.Value && Convert.ToBoolean(r[12]),
                    // Die Spaltennummer haengt davon ab, ob IstPflicht mitgelesen wurde -
                    // beide Spalten stehen nur bedingt in der Auswahl.
                    NutzungsdauerId = mitNutzungsdauerId
                        ? ZahlOderNull(r[mitPflicht ? 13 : 12]) : null,
                });

            // ETAPPE E7c (Schritt E): die zwei Kennzeichen je Position — eine eigene
            // Abfrage statt zwei weiterer bedingter Spaltennummern; leer ohne Spalten.
            Dictionary<int, ErsatzRestwertKennzeichen.Paar> kennzeichen =
                ErsatzRestwertKennzeichen.LiesVorlage(vorlageId);
            foreach (KostenVorlagenPosition p in liste)
            {
                ErsatzRestwertKennzeichen.Paar k;
                if (!kennzeichen.TryGetValue(p.Id, out k)) continue;
                p.ErsatzFuehren = k.ErsatzFuehren;
                p.RestwertAnsetzen = k.RestwertAnsetzen;
            }
            return liste;
        }

        /// <summary>ETAPPE H3: Probe der Schritt-59-Spalte an der Vorlagentabelle
        /// (Muster <see cref="WirtschaftlichkeitCtrl.SpalteVorhanden"/>, Ergebnis je
        /// Prozess gemerkt).</summary>
        private static bool? _pflichtSpalte;

        private static bool PflichtSpalteVorhanden()
        {
            if (_pflichtSpalte.HasValue) return _pflichtSpalte.Value;
            _pflichtSpalte = WirtschaftlichkeitCtrl.SpalteVorhanden(
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, SchemaKatalog.SPALTE_KVP_IST_PFLICHT);
            return _pflichtSpalte.Value;
        }

        /// <summary>
        /// Ä8 (Nutzerentscheid 26.08.2026): Der Schreibschutz der
        /// Auslieferungsvorlagen ist AUFGEHOBEN — für Investitions- UND
        /// Betriebskostenvorlagen. Die Auslieferungswerte dürfen direkt gepflegt
        /// werden; das <c>ReadOnly</c>-Flag bleibt in der Datenbank als reiner
        /// Herkunftsmarker der Saat stehen. Einziger Restschutz: Die
        /// STANDARD-Vorlage einer Komponente kann nicht gelöscht werden
        /// (<see cref="VorlageLoeschen"/>) — sie ist die Quelle von
        /// „Speichern unter…" und der Übernahme-Mechanik (§ 8), und die
        /// KD1-Saat läuft nicht erneut.
        /// </summary>
        public static bool IstNurLesen(int vorlageId)
        {
            return false;
        }

        /// <summary>true, wenn die Vorlage die Standardvorlage ihrer Komponente ist.</summary>
        public static bool IstStandard(int vorlageId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + SchemaKatalog.SPALTE_KV_IST_STANDARD + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new DbParam("@id", vorlageId));
            return o != null && o != DBNull.Value && Convert.ToBoolean(o);
        }

        /// <summary>Umsatzsteuersatz [%] aus dem Gesetzeskatalog
        /// (<c>UMSATZSTEUER_REGELSATZ</c>, seit Etappe E1 gesät; KL5: reine Anzeige).</summary>
        public static double? UstSatzProzent()
        {
            try
            {
                return new GesetzKatalog().Wert(
                    DbWerte.GESETZ_UMSATZSTEUER_REGELSATZ, DateTime.Now.Year);
            }
            catch { return null; }
        }

        // -------------------------------------------------------------- Schreiben ---

        /// <summary>Leere neue Variante; Rückgabe ID oder 0 bei Fehler/Namensdublette.</summary>
        public static int VorlageNeu(int komponentenId, int kategorieId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            if (NameBelegt(komponentenId, kategorieId, name)) return 0;

            int id = MaxId(SchemaKatalog.TAB_KOSTENVORLAGE) + 1;
            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] ([ID], [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "], [" +
                SchemaKatalog.SPALTE_KV_NAME + "], [" +
                SchemaKatalog.SPALTE_KV_IST_STANDARD + "], [" +
                SchemaKatalog.SPALTE_KV_READONLY + "], [" +
                SchemaKatalog.SPALTE_KV_GEAENDERT_AM + "]) VALUES (?, ?, ?, ?, FALSE, FALSE, ?)",
                new DbParam("@id", id),
                new DbParam("@kid", komponentenId),
                new DbParam("@kat", kategorieId),
                new DbParam("@n", name.Trim()),
                Datum("@am", DateTime.Now));
            return n == 1 ? id : 0;
        }

        /// <summary>
        /// „Speichern unter": kopiert Kopf und alle Positionen der Quelle in eine neue,
        /// editierbare Variante (auch von ReadOnly-Vorlagen — genau dafür ist der Weg da).
        /// Rückgabe ID der Kopie oder 0.
        /// </summary>
        public static int SpeichernUnter(int quellVorlageId, string neuerName)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT [" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new DbParam("@id", quellVorlageId));
            if (kopf.Rows.Count != 1) return 0;

            int neueId = VorlageNeu(Convert.ToInt32(kopf.Rows[0][0]),
                                    Convert.ToInt32(kopf.Rows[0][1]), neuerName);
            if (neueId == 0) return 0;

            foreach (KostenVorlagenPosition p in Positionen(quellVorlageId))
            {
                p.VorlageId = neueId;
                if (PositionAnlegen(p) == 0)
                {
                    // Halbe Kopie zurücknehmen (Löschweitergabe räumt die Positionen ab).
                    DataRepository.ExecuteNonQuery(
                        "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = " + neueId);
                    return 0;
                }
            }
            return neueId;
        }

        /// <summary>Variante löschen (Löschweitergabe räumt die Positionen ab).
        /// ReadOnly-Vorlagen sind geschützt.</summary>
        public static bool VorlageLoeschen(int vorlageId)
        {
            if (IstStandard(vorlageId)) return false;   // Ä8-Restschutz (s. IstNurLesen)
            return DataRepository.ExecuteNonQuery(
                "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new DbParam("@id", vorlageId)) == 1;
        }

        /// <summary>Neue Position ans Rasterende (FK2: „+ Position hinzufügen").
        /// Rückgabe ID oder 0; ReadOnly-Schutz.
        ///
        /// <para><b>STUFE S1 (Konzept Nutzungsdauer/AfA 2.4.1):</b> Eine neue
        /// INVESTITIONSposition wird mit der Nutzungsdauer ihrer Technik vorbelegt —
        /// ohne Positionsart also mit der Standardzeile. Belegt wird nur, was ohnehin
        /// leer entstünde; ein gespeicherter Wert wird nirgends still ersetzt. Gibt es
        /// die Tabelle oder die Standardzeile nicht, bleibt das Feld leer.</para></summary>
        public static int PositionNeu(int vorlageId, string bezeichnung, string kostenart,
                                      string bemessung)
        {
            if (IstNurLesen(vorlageId) || string.IsNullOrWhiteSpace(bezeichnung)) return 0;

            var p = new KostenVorlagenPosition
            {
                VorlageId = vorlageId,
                Bezeichnung = bezeichnung.Trim(),
                Kostenart = kostenart ?? DbWerte.KOSTENART_SONSTIGE,
                Bemessung = bemessung ?? DbWerte.BEMESSUNG_BETRAG,
                Sortierung = NaechsteSortierung(vorlageId),
                Nutzungsdauer = VorgabeDerVorlage(vorlageId),
            };
            int id = PositionAnlegen(p);
            if (id != 0) KopfBeruehren(vorlageId);
            return id;
        }

        /// <summary>
        /// Die Nutzungsdauer-Vorgabe einer Vorlage (Stufe S1): der Technik-Standard,
        /// aber nur für eine INVESTITIONSvorlage — Betriebskosten kennen keinen Ersatz.
        /// <c>null</c>, wenn die Vorlage unbekannt ist, die Kategorie nicht passt oder
        /// die Technik keine Standardzeile hat.
        /// </summary>
        public static double? VorgabeDerVorlage(int vorlageId)
        {
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT [" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [ID] = ?",
                new DbParam("@id", vorlageId));
            if (kopf.Rows.Count != 1) return null;

            object kat = kopf.Rows[0][1];
            if (kat == DBNull.Value ||
                Convert.ToInt32(kat) != DbWerte.KOSTEN_KATEGORIE_INVESTITION) return null;

            object kid = kopf.Rows[0][0];
            if (kid == DBNull.Value) return null;

            return NutzungsdauerCtrl.Vorgabe(Convert.ToInt32(kid), null).Wert;
        }

        /// <summary>Alle Fachfelder einer Position schreiben; ReadOnly-Schutz.</summary>
        public static bool PositionSpeichern(KostenVorlagenPosition p)
        {
            if (p == null || IstNurLesen(p.VorlageId)) return false;

            // STUFE S1 (Schritt 75): Die Positionsart wandert mit, wo die Spalte schon
            // da ist. Eine nie migrierte Datenbank schreibt die neun Bestandsfelder -
            // dieselbe tolerante Vorsorge wie bei IstPflicht.
            bool mitArt = NutzungsdauerCtrl.VerweisSpalteVorhanden(
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION);

            var parameter = new List<DbParam>
            {
                new DbParam("@b", p.Bezeichnung ?? ""),
                new DbParam("@ka", p.Kostenart ?? ""),
                new DbParam("@bm", p.Bemessung ?? ""),
                Wert("@satz", p.Satz),
                Wert("@betrag", p.BetragNetto),
                new DbParam("@erl", p.IstErloes),
                Wert("@nd", p.Nutzungsdauer),
                Wert("@ev", p.EmpfehlungVon),
                Wert("@eb", p.EmpfehlungBis),
            };
            if (mitArt) parameter.Add(Ganz("@art", p.NutzungsdauerId));
            parameter.Add(new DbParam("@id", p.Id));

            int n = DataRepository.ExecuteNonQuery(
                "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_SATZ + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_BETRAG_NETTO + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_NUTZUNGSDAUER + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "] = ?" +
                (mitArt ? ", [" + NutzungsdauerSchema.SPALTE_VERWEIS + "] = ?" : "") +
                " WHERE [ID] = ?",
                parameter.ToArray());
            if (n == 1) KopfBeruehren(p.VorlageId);
            return n == 1;
        }

        /// <summary>Position löschen; ReadOnly-Schutz über die Vorlage.</summary>
        public static bool PositionLoeschen(int positionId)
        {
            object vid = DataRepository.ExecuteScalar(
                "SELECT [" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "] FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [ID] = ?",
                new DbParam("@id", positionId));
            if (vid == null || vid == DBNull.Value) return false;
            int vorlageId = Convert.ToInt32(vid);
            if (IstNurLesen(vorlageId)) return false;

            bool ok = DataRepository.ExecuteNonQuery(
                "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [ID] = ?",
                new DbParam("@id", positionId)) == 1;
            if (ok) KopfBeruehren(vorlageId);
            return ok;
        }

        // ----------------------------------------------------------------- intern ---

        private static bool NameBelegt(int komponentenId, int kategorieId, string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [" +
                SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = ? AND [" +
                SchemaKatalog.SPALTE_KV_KATEGORIEID + "] = ? AND [" +
                SchemaKatalog.SPALTE_KV_NAME + "] = ?",
                new DbParam("@kid", komponentenId),
                new DbParam("@kat", kategorieId),
                new DbParam("@n", name.Trim()));
            return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
        }

        private static int PositionAnlegen(KostenVorlagenPosition p)
        {
            int id = MaxId(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION) + 1;

            // STUFE S1 (Schritt 75): Die Positionsart entsteht mit der Zeile - sonst
            // verlöre "Speichern unter" sie bei jeder Kopie.
            bool mitArt = NutzungsdauerCtrl.VerweisSpalteVorhanden(
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION);

            var parameter = new List<DbParam>
            {
                new DbParam("@id", id),
                new DbParam("@vid", p.VorlageId),
                Ganz("@sid", p.StammId),
                new DbParam("@b", p.Bezeichnung ?? ""),
                new DbParam("@ka", p.Kostenart ?? ""),
                new DbParam("@bm", p.Bemessung ?? ""),
                Wert("@satz", p.Satz),
                Wert("@betrag", p.BetragNetto),
                new DbParam("@erl", p.IstErloes),
                Wert("@nd", p.Nutzungsdauer),
                Wert("@ev", p.EmpfehlungVon),
                Wert("@eb", p.EmpfehlungBis),
                new DbParam("@so", p.Sortierung),
            };
            if (mitArt) parameter.Add(Ganz("@art", p.NutzungsdauerId));

            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] ([ID], [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "], [" +
                SchemaKatalog.SPALTE_KVP_STAMMID + "], [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_SATZ + "], [" +
                SchemaKatalog.SPALTE_KVP_BETRAG_NETTO + "], [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "], [" +
                SchemaKatalog.SPALTE_KVP_NUTZUNGSDAUER + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "], [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]" +
                (mitArt ? ", [" + NutzungsdauerSchema.SPALTE_VERWEIS + "]" : "") +
                ") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?" +
                (mitArt ? ", ?" : "") + ")",
                parameter.ToArray());
            // ETAPPE E7c (Schritt E): Die Kennzeichen wandern mit (Speichern unter).
            if (n == 1 && (p.ErsatzFuehren.HasValue || p.RestwertAnsetzen.HasValue))
                ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, id,
                                                   p.ErsatzFuehren, p.RestwertAnsetzen);
            return n == 1 ? id : 0;
        }

        private static int NaechsteSortierung(int vorlageId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MAX([" + SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]) FROM [" +
                SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] WHERE [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ?",
                new DbParam("@vid", vorlageId));
            int max = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            return max + 10;
        }

        /// <summary>Pflegestand des Kopfs fortschreiben (jede Positionsänderung).</summary>
        private static void KopfBeruehren(int vorlageId)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] SET [" +
                SchemaKatalog.SPALTE_KV_GEAENDERT_AM + "] = ? WHERE [ID] = ?",
                Datum("@am", DateTime.Now),
                new DbParam("@id", vorlageId));
        }

        private static int MaxId(string tabelle)
        {
            object o = DataRepository.ExecuteScalar("SELECT MAX([ID]) FROM [" + tabelle + "]");
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        private static double? WertOderNull(object o)
        {
            return (o == null || o == DBNull.Value) ? (double?)null : Convert.ToDouble(o);
        }

        private static int? ZahlOderNull(object o)
        {
            return (o == null || o == DBNull.Value) ? (int?)null : Convert.ToInt32(o);
        }

        /// <summary>Nullbarer DOUBLE-Parameter mit ausdrücklichem Typ (ein DBNull ohne
        /// Typ kann der Provider nicht binden — Muster <c>SchemaMigration.ParamOderNull</c>).</summary>
        private static DbParam Wert(string name, double? wert)
        {
            var p = new DbParam(name, DbParamTyp.Double);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>Nullbarer LONG-Parameter.</summary>
        private static DbParam Ganz(string name, int? wert)
        {
            var p = new DbParam(name, DbParamTyp.Integer);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>DATETIME-Parameter.</summary>
        private static DbParam Datum(string name, DateTime wert)
        {
            var p = new DbParam(name, DbParamTyp.Date);
            p.Wert = wert;
            return p;
        }
    }

    /// <summary>
    /// Der Bemessungskatalog der Oberfläche (Konzept § 5.3) — EINE Wahrheit für
    /// Auswahlliste, Einheitenanzeige und Kopplungsregel (KL4/§ 5.4); auch die
    /// Projektseite (KD3) liest hier.
    /// </summary>
    public static class BemessungKatalog
    {
        /// <summary>Ein Eintrag des Katalogs.</summary>
        public sealed class Info
        {
            /// <summary>Persistenzwert (<c>DbWerte.BEMESSUNG_*</c>).</summary>
            public string Persistenz;

            /// <summary>MyResource-Schlüssel des Anzeigetexts.</summary>
            public string ResourceKey;

            /// <summary>Deutscher Rückfalltext (= Designer-Vorgabe, Ä6-Regel 2).</summary>
            public string AnzeigeDe;

            /// <summary>Einheiten-Suffix hinter dem Satzfeld („€/kW", „%", …).</summary>
            public string Einheit;

            /// <summary>
            /// U33: Das Einheitenzeichen im BETRIEBSRASTER, wo es ein anderes ist als
            /// <see cref="Einheit"/>; leer heißt „dieselbe Einheit wie im
            /// Investitionsraster".
            ///
            /// <para><b>Warum es einen zweiten Wert braucht.</b> Eine Betriebszeile
            /// trägt einen JAHRESSATZ. Bei den Mengenarten steckt das Jahr schon in der
            /// Bezugsgröße — „je kWh elektrisch" bemisst sich an einer Jahresmenge
            /// [kWh/a] und heißt deshalb auf beiden Seiten „€/kWh". Eine LEISTUNG kennt
            /// dagegen kein Jahr: „je kWp Leistung" muss die Zeitangabe im Satz tragen,
            /// sonst stünde hinter 12,00 „€/kWp" und daneben ein Betrag von
            /// 3.600,00 €/a. Deshalb „€/kWp·a".</para>
            /// </summary>
            public string EinheitBetrieb;

            /// <summary>In der Auswahl des Investitionsrasters?</summary>
            public bool FuerInvest;

            /// <summary>In der Auswahl des Betriebsrasters?</summary>
            public bool FuerBetrieb;

            /// <summary>Absolut (Satz = Betrag, § 5.4) statt bezugsgrößen-abhängig.</summary>
            public bool Absolut;
        }

        /// <summary>Katalog § 5.3; die beiden Altwerte (generisch je kWh / je Stunde)
        /// stehen nur für die Anzeige von Bestandsdaten, nicht in den Auswahllisten.</summary>
        public static readonly Info[] Alle =
        {
            N(DbWerte.BEMESSUNG_BETRAG,                  "BM_BETRAG",            "fester Betrag",          "€",     true,  false, true),
            N(DbWerte.BEMESSUNG_JAHRESBETRAG,            "BM_JAHRESBETRAG",      "fester Jahresbetrag",    "€/a",   false, true,  true),
            N(DbWerte.BEMESSUNG_PROZENT_INVESTITION,     "BM_P_INVESTITION",     "% der Investition",      "%",     true,  true,  false),
            N(DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,  "BM_P_ERZEUGER",        "% der Erzeugerkosten",   "%",     true,  false, false),
            // ETAPPE H1 — Hilfsenergie an der Endenergie der Anlage (Festlegung 29.08.2026).
            // Die MENGE ist kein Eingabewert, sondern ein ERGEBNISWERT: Sie kommt aus dem
            // Simulationslauf, im Dialog wird nur der Satz gepflegt. Ohne Lauf gibt es
            // keine Menge und damit keinen Betrag - dann bleibt die absolute Angabe.
            N(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,"BM_P_ENDENERGIEKOSTEN","% der Endenergiekosten", "%",     false, true,  false),
            N(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,"BM_P_ENDENERGIEBEDARF","% des Endenergiebedarfs","%",     false, true,  false),
            // Von H1 abgeloest: dieselbe Groesse, aber je Energieart getrennt und
            // projektweit bemessen. Bestandsdaten werden weiter ANGEZEIGT und gerechnet,
            // zur Neuauswahl stehen sie nicht mehr (FuerBetrieb = false).
            N(DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN,"BM_P_BRENNSTOFF",      "% der Brennstoffkosten", "%",     false, false, false),
            N(DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN,     "BM_P_STROM",           "% der Stromkosten",      "%",     false, false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,   "BM_KWH_THERMISCH",     "je kWh thermisch",       "€/kWh", false, true,  false),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,  "BM_KWH_ELEKTRISCH",    "je kWh elektrisch",      "€/kWh", false, true,  false),
            // Die drei Leistungsarten stehen wie „je kWp Leistung" auch im
            // BETRIEBSRASTER: Wartung und Instandhaltung werden branchenüblich je
            // installierter Leistung bemessen (€/kW·a), und bis hierher gab es dafür
            // nur den festen Jahresbetrag, „% der Investition" und die Mengenarten aus
            // dem Lauf. Gerechnet wird ohne eine einzige neue Formel: Menge × Satz kennt
            // BetriebskostenCtrl.Betrag längst, die Bezugsgröße kommt aus derselben
            // Landkarte wie auf der Investitionsseite (TechnikPlanwertCtrl.Geraetespalte
            // über WirtschaftlichkeitCtrl.RueckfallMenge), und die Herkunft der Zeile
            // ist damit die ANLAGE, nicht der Lauf (KostenProjektPositionenCtrl).
            //
            // Auf welche Gewerke die Auswahl damit wächst, sagt nicht diese Zeile,
            // sondern die Landkarte selbst (PasstZuGewerk): Wärmepumpe, Heizkessel,
            // Photovoltaik, Solarthermie, Strom- und Pufferspeicher und BHKW — je
            // nachdem, welche Baugröße das Gewerk zur Art führt. Die drei Gewerke ohne
            // Gerät (Wärmezentrale, Bauliche Anlagen, Stromeinspeisung) bekommen nichts
            // dazu.
            //
            // Das Jahr steht im SATZ, nicht in der Bezugsgröße: Eine Leistung kennt
            // kein Jahr, deshalb die eigene Betriebseinheit „€/kW·a" (siehe
            // Info.EinheitBetrieb). Wo ein Gewerk eine eigene Beschriftung führt, trägt
            // sie ihre eigene Betriebseinheit (Sonderfaelle).
            N(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,     "BM_KW_LEISTUNG",       "je kW Leistung",         "€/kW",  true,  true,  false, "€/kW·a"),
            N(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, "BM_KW_HEIZLEISTUNG",   "je kW Heizleistung",     "€/kW",  true,  true,  false, "€/kW·a"),
            N(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,   "BM_KW_ELEKTRISCH",     "je kW elektrisch",       "€/kW",  true,  true,  false, "€/kW·a"),
            // U33 (18.09.2026): „je kWp Leistung" steht auch im BETRIEBSRASTER — die
            // Wartung einer Photovoltaikanlage wird branchenüblich in €/kWp·a bemessen,
            // und bis hierher gab es dafür keine Bemessung (fester Jahresbetrag,
            // % der Investition, je kWh elektrisch). Gerechnet wird ohne eine einzige
            // neue Formel: BetriebskostenCtrl.Betrag kennt die Art längst (Menge × Satz),
            // die Bezugsgröße kommt aus derselben kWp-Wahrheit wie auf der
            // Investitionsseite (TechnikPlanwertCtrl.BaugroesseSumme →
            // PhotovoltaikCtrl.KwpSumme). Auf die Photovoltaik eingegrenzt wird die
            // Auswahl nicht von Hand, sondern von der Landkarte der Bezugsgrößen
            // (PasstZuGewerk): Kein anderes Gewerk führt eine kWp-Größe.
            N(DbWerte.BEMESSUNG_EUR_PRO_KWP,             "BM_KWP",               "je kWp Leistung",        "€/kWp", true,  true,  false, "€/kWp·a"),
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,  "BM_KWH_KAPAZITAET",    "je kWh Kapazität",       "€/kWh", true,  false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR,    "BM_M2_KOLLEKTOR",      "je m² Kollektorfläche",  "€/m²",  true,  false, false),
            // Altwerte — Anzeige von Bestandsdaten, keine Neuauswahl:
            N(DbWerte.BEMESSUNG_EUR_PRO_KWH,             "BM_KWH",               "je kWh",                 "€/kWh", false, false, false),
            N(DbWerte.BEMESSUNG_EUR_PRO_H,               "BM_STUNDE",            "je Stunde",              "€/h",   false, false, false),
        };

        /// <summary>Eintrag zum Persistenzwert; NULL bei unbekanntem Wert.</summary>
        public static Info Finde(string persistenz)
        {
            foreach (Info i in Alle)
                if (string.Equals(i.Persistenz, persistenz, StringComparison.Ordinal)) return i;
            return null;
        }

        // =====================================================================
        // ANWENDERENTSCHEID 15.09.2026 — DIE AUSWAHL FOLGT DER BEZUGSGRÖSSE
        //
        //   „Prüfe Pufferspeicher Daten mit Volumen/Größe. EUR_PRO_KWH_KAPAZITAET
        //   spielt keine Rolle, nur das Volumen als Bezugsgröße."
        //
        //   Der Katalog oben ist eine FLACHE Liste: Bis zu diesem Entscheid bot die
        //   Auswahl jedem Gewerk jede Art an — auch eine, für die das Gewerk gar
        //   keine Bezugsgröße führt. Ein Satz an so einer Zeile fiel über den
        //   Anwenderentscheid I-2 auf den erfassten Betrag zurück, bei einer
        //   Satzzeile also auf 0, und der Anwender sah nicht, warum.
        //
        //   EINE WAHRHEIT, KEINE ZWEITE LISTE. Welche Art zu welchem Gewerk eine
        //   Größe führt, steht bereits in der Landkarte der Bezugsgrößen
        //   (TechnikPlanwertCtrl.KenntBaugroesse für die Gerätewelt,
        //   EndenergieAufloeser für die Arten aus dem Lauf). WirtschaftlichkeitCtrl
        //   .BasisGrund liest genau sie und beantwortet für JEDE Art und JEDES
        //   Gewerk die Frage, ob eine Bezugsgröße überhaupt möglich ist:
        //   BASISGRUND_GEWERK heißt „die Art passt nicht zu diesem Gewerk", jede
        //   andere Antwort heißt „die Art passt, die Größe fehlt (noch)". Die
        //   Auswahl fragt dort nach, statt eine zweite Zuordnung zu führen.
        //
        //   DER GELTUNGSBEREICH IST BENANNT, NICHT STILL. Dasselbe Muster steht an
        //   JEDEM Gewerk: Auch der Wärmepumpe wird „je kWp Leistung" angeboten und
        //   der Photovoltaik „je m² Kollektorfläche". Der zweite Entscheid desselben
        //   Tages weitet die Filterung deshalb auf ALLE ZEHN Gewerke aus; der
        //   Geltungsbereich bleibt trotzdem benannt, damit sichtbar ist, WORAUF die
        //   Regel wirkt, und nicht nur, DASS sie wirkt.
        //
        //   GEMESSEN, BEVOR SIE SCHARF GESCHALTET WURDE. Die Matrix Art↔Gewerk ist
        //   je Gewerk und je Raster ausgezählt worden, dazu der Bestand der
        //   Auslieferungsvorlagen und der Projektzeilen der Messlatte:
        //
        //     · Kein Gewerk verliert seine Auswahl. Am dünnsten bleibt das
        //       BETRIEBSRASTER der vier Gewerke ohne Geräte- und Laufgrößen
        //       (Pufferspeicher, Wärmezentrale, Bauliche Anlagen, Stromeinspeisung):
        //       fester Jahresbetrag und „% der Investition" — beide tragen dort
        //       wirklich, die zweite über die Investitionskaskade.
        //     · Genau EINE Zeile des Bestands trug eine Art, die an ihrem Gewerk
        //       keine Bezugsgröße führt: die Vorlagenposition „Batteriespeicher" der
        //       Photovoltaik mit „je kWh Kapazität". Sie ist eigens umgestellt
        //       worden (Schemaschritt 78, PvVorlageBatteriespeicher); die
        //       Projektzeilen der Messlatte sind samt und sonders unauffällig.
        //
        //   Eine Art, die eine VORHANDENE Zeile trägt, bleibt davon unberührt in der
        //   Liste (siehe Auswahl, Bedingung 1) — sonst verlöre ein gepflegter Wert
        //   seine Auswahl und ließe sich nicht mehr ändern.
        // =====================================================================

        /// <summary>
        /// Die Gewerke (<c>Tab_KostenKomponente.ID</c>), an denen die Auswahlliste nach
        /// der Bezugsgröße gefiltert wird — <b>der Geltungsbereich des Entscheids</b>,
        /// nicht die Regel selbst. Die Regel steht in <see cref="PasstZuGewerk"/>; seit
        /// dem zweiten Entscheid vom 15.09.2026 deckt der Geltungsbereich alle zehn
        /// Kostenkomponenten ab. Ein Gewerk, das hier fehlt, bekäme weiterhin die
        /// vollständige Liste angeboten.
        /// </summary>
        private static readonly int[] AUSWAHLFILTER_GEWERKE =
        {
            1,   // Wärmepumpe
            2,   // Heizkessel
            3,   // Photovoltaik
            4,   // Solarthermie
            5,   // Stromspeicher
            6,   // Pufferspeicher
            7,   // BHKW
            8,   // Wärmezentrale
            9,   // Bauliche Anlagen
            10,  // Stromeinspeisung
        };

        /// <summary>
        /// Führt dieses Gewerk zu dieser Bemessungsart überhaupt eine Bezugsgröße?
        /// <c>komponentenId</c> 0 = Gewerk unbekannt; dann wird nicht gefiltert.
        ///
        /// <para>Die Antwort kommt aus <see cref="WirtschaftlichkeitCtrl.BasisGrund"/> —
        /// derselben Landkarte, aus der auch der Dialog seinen Grundtext holt, wenn eine
        /// Zeile ohne Bezugsgröße bleibt. Eine eigene Zuordnung gibt es hier bewusst
        /// nicht: Zwei Listen, die dasselbe behaupten, laufen auseinander.</para>
        ///
        /// <para>Absolute Arten (fester Betrag, fester Jahresbetrag) und die
        /// Prozentarten der Investseite brauchen keine Baugröße und passen deshalb zu
        /// jedem Gewerk.</para>
        ///
        /// <para><b>Die Frage gilt für JEDES Gewerk</b> — ob die AUSWAHL ihr folgt, sagt
        /// <see cref="AUSWAHLFILTER_GEWERKE"/>. Wer die Zuordnung Art↔Gewerk wissen will
        /// (Prüfungen, Grundtexte), fragt hier; wer die Liste baut, nimmt
        /// <see cref="Auswahl"/>.</para>
        /// </summary>
        public static bool PasstZuGewerk(string persistenz, int komponentenId)
        {
            if (komponentenId <= 0 || string.IsNullOrEmpty(persistenz)) return true;
            return !string.Equals(WirtschaftlichkeitCtrl.BasisGrund(persistenz, komponentenId),
                                  WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                                  StringComparison.Ordinal);
        }

        /// <summary>
        /// Wird die Auswahlliste dieses Gewerks nach der Bezugsgröße gefiltert?
        /// Siehe <see cref="AUSWAHLFILTER_GEWERKE"/>.
        /// </summary>
        public static bool AuswahlWirdGefiltert(int komponentenId)
        {
            foreach (int k in AUSWAHLFILTER_GEWERKE)
                if (k == komponentenId) return true;
            return false;
        }

        /// <summary>
        /// Die wählbaren Arten EINES Gewerks in Katalogreihenfolge — die EINE Stelle, an
        /// der die Auswahlliste des Investitions- und des Betriebsrasters entsteht.
        ///
        /// <para>Drei Bedingungen, in dieser Reihenfolge: Eine Art, die eine vorhandene
        /// Zeile bereits TRÄGT, steht immer in der Liste (<paramref name="benutzt"/>) —
        /// sonst verlöre eine Bestandsposition beim Anzeigen ihren Wert, und genau dafür
        /// stehen auch die beiden Altwerte des Katalogs. Sonst muss die Art zum Raster
        /// gehören (<see cref="Info.FuerInvest"/>/<see cref="Info.FuerBetrieb"/>) und —
        /// an den Gewerken des Geltungsbereichs (<see cref="AuswahlWirdGefiltert"/>) —
        /// zum Gewerk passen (<see cref="PasstZuGewerk"/>).</para>
        /// </summary>
        /// <param name="komponentenId"><c>Tab_KostenKomponente.ID</c>; 0 = unbekannt,
        /// dann wird nach dem Gewerk nicht gefiltert.</param>
        /// <param name="invest">true = Investitionsraster, false = Betriebsraster.</param>
        /// <param name="benutzt">Persistenzwerte, die vorhandene Zeilen schon tragen;
        /// <c>null</c> erlaubt.</param>
        public static List<Info> Auswahl(int komponentenId, bool invest,
                                         ICollection<string> benutzt)
        {
            bool filtern = AuswahlWirdGefiltert(komponentenId);
            var treffer = new List<Info>();
            foreach (Info i in Alle)
            {
                if (benutzt != null && benutzt.Contains(i.Persistenz)) { treffer.Add(i); continue; }
                if (!(invest ? i.FuerInvest : i.FuerBetrieb)) continue;
                if (filtern && !PasstZuGewerk(i.Persistenz, komponentenId)) continue;
                treffer.Add(i);
            }
            return treffer;
        }

        /// <summary>Anzeigetext (MyResource, deutscher Rückfall) — OHNE Gewerk, also
        /// der allgemeine Name der Art. Wo das Gewerk bekannt ist, gilt
        /// <see cref="Anzeige(string,int)"/>.</summary>
        public static string Anzeige(string persistenz)
        {
            return Anzeige(persistenz, 0);
        }

        // =====================================================================
        // ANWENDERENTSCHEID 15.09.2026 — DIE BESCHRIFTUNG FOLGT DER BEZUGSGRÖSSE
        //
        //   „je kW Leistung" bemisst sich je Gewerk an dessen EINER Baugröße
        //   (TechnikPlanwertCtrl.Geraetespalte). Für zwei Gewerke ist diese Größe
        //   seit dem Entscheid eine andere als eine thermische Leistung: beim BHKW
        //   die ELEKTRISCHE Leistung, beim Pufferspeicher das VOLUMEN in Litern.
        //   Ein Satzfeld mit „€/kW" hinter einem Literwert wäre dort schlicht
        //   falsch, und „je kW Leistung" über einer Pel-Bemessung mehrdeutig.
        //
        //   EIN Persistenzwert, ZWEI Beschriftungen: Der gespeicherte Wert bleibt
        //   EUR_PRO_KW_LEISTUNG (Drei-Schichten-Regel, eingefroren); nur Name und
        //   Einheit der ANZEIGE hängen am Gewerk. Die Texte stehen in MyResource
        //   (beide Sprachen), die Einheitenzeichen nicht — dokumentierte Ausnahme
        //   wie bei BetriebskostenCtrl.SatzEinheit.
        // =====================================================================

        /// <summary>Gewerk-eigene Beschriftung: Persistenzwert + <c>Tab_KostenKomponente.ID</c>
        /// → Ressourcenschlüssel und Einheit.</summary>
        private sealed class Sonderbeschriftung
        {
            public string Persistenz;
            public int KomponentenId;
            public string ResourceKey;
            public string AnzeigeDe;
            public string Einheit;

            /// <summary>Einheit im Betriebsraster — dieselbe Regel wie
            /// <see cref="Info.EinheitBetrieb"/>: Eine Leistung und ein Volumen kennen
            /// kein Jahr, der JAHRESSATZ trägt es deshalb selbst. Leer = wie
            /// <see cref="Einheit"/>.</summary>
            public string EinheitBetrieb;
        }

        private static readonly Sonderbeschriftung[] Sonderfaelle =
        {
            new Sonderbeschriftung
            {
                Persistenz = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                KomponentenId = BetriebskostenCtrl.KOMPONENTE_BHKW,
                ResourceKey = "BM_KW_LEISTUNG_BHKW",
                AnzeigeDe = "je kW elektr. Leistung",
                Einheit = "€/kW",
                EinheitBetrieb = "€/kW·a",
            },
            new Sonderbeschriftung
            {
                Persistenz = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                KomponentenId = BetriebskostenCtrl.KOMPONENTE_PUFFERSPEICHER,
                ResourceKey = "BM_LITER",
                AnzeigeDe = "je Liter",
                Einheit = "€/Ltr.",
                EinheitBetrieb = "€/Ltr.·a",
            },
        };

        private static Sonderbeschriftung Sonderfall(string persistenz, int komponentenId)
        {
            if (komponentenId <= 0) return null;
            foreach (Sonderbeschriftung s in Sonderfaelle)
                if (s.KomponentenId == komponentenId &&
                    string.Equals(s.Persistenz, persistenz, StringComparison.Ordinal)) return s;
            return null;
        }

        /// <summary>
        /// Anzeigetext der Bemessungsart IN DIESEM GEWERK (<c>Tab_KostenKomponente.ID</c>;
        /// 0 = Gewerk unbekannt, dann der allgemeine Name).
        /// </summary>
        public static string Anzeige(string persistenz, int komponentenId)
        {
            Sonderbeschriftung s = Sonderfall(persistenz, komponentenId);
            if (s != null) return Text(s.ResourceKey, s.AnzeigeDe);

            Info i = Finde(persistenz);
            if (i == null) return persistenz ?? "";
            return Text(i.ResourceKey, i.AnzeigeDe);
        }

        /// <summary>
        /// Einheiten-Suffix hinter dem Satzfeld IN DIESEM GEWERK — beim Pufferspeicher
        /// „€/Ltr.", sonst die Einheit des Katalogs. <c>komponentenId</c> 0 = unbekannt.
        /// </summary>
        public static string Einheit(string persistenz, int komponentenId)
        {
            return Einheit(persistenz, komponentenId, false);
        }

        /// <summary>
        /// U33: dasselbe Einheitenzeichen, aber IM RASTER — <paramref name="betrieb"/>
        /// = true fragt das Betriebsraster. Nur wo der Katalog eine eigene
        /// Betriebseinheit führt (<see cref="Info.EinheitBetrieb"/>), fällt die Antwort
        /// anders aus; sonst ist sie Zeichen für Zeichen dieselbe.
        /// </summary>
        public static string Einheit(string persistenz, int komponentenId, bool betrieb)
        {
            Sonderbeschriftung s = Sonderfall(persistenz, komponentenId);
            if (s != null)
                return betrieb && !string.IsNullOrEmpty(s.EinheitBetrieb)
                    ? s.EinheitBetrieb : s.Einheit;

            Info i = Finde(persistenz);
            if (i == null) return "";
            return betrieb && !string.IsNullOrEmpty(i.EinheitBetrieb)
                ? i.EinheitBetrieb : i.Einheit;
        }

        /// <summary>
        /// ETAPPE E2 (Befund B-7) — Einheitenzeichen der BEZUGSMENGE dieser
        /// Bemessungsart in diesem Gewerk („kW", „kWp", „Ltr.", „kWh/a", „h/a", „€").
        ///
        /// <para><b>Abgeleitet, nicht zweitgepflegt.</b> Der Satz einer abgeleiteten Art
        /// ist „€ je Bezugsgröße" (<see cref="Einheit(string,int,bool)"/>); die
        /// Bezugsgröße ist damit genau das, was hinter dem Schrägstrich steht. Eine
        /// zweite Liste daneben wäre die Stelle, an der Satz und Menge auseinanderlaufen
        /// — genau das ist vor E2 geschehen: <c>BetriebskostenCtrl.MengenEinheit</c>
        /// kannte nur die beiden Altarten und beschriftete alles andere mit „€", auch
        /// eine Leistung in kW.</para>
        ///
        /// <para><b>Das Jahr steht im SATZ, nicht in der Bezugsgröße</b> (siehe
        /// <see cref="Info.EinheitBetrieb"/>): Trägt der Betriebssatz „·a", ist die
        /// Bezugsgröße ein BESTAND (Leistung, Fläche, Volumen) und bleibt ohne Jahr;
        /// sonst ist sie eine MENGE je Jahr („kWh/a"). Eine prozentuale Art bemisst sich
        /// an einem Betrag — ihre Bezugsgröße ist „€".</para>
        /// </summary>
        /// <param name="komponentenId"><c>Tab_KostenKomponente.ID</c>; 0 = unbekannt.</param>
        public static string Mengeneinheit(string persistenz, int komponentenId)
        {
            Info i = Finde(persistenz);
            if (i == null || i.Absolut) return DbWerte.KOSTEN_EINHEIT_EURO;

            string satz = Einheit(persistenz, komponentenId, true);
            if (string.IsNullOrEmpty(satz) ||
                !satz.StartsWith("€/", StringComparison.Ordinal))
                return DbWerte.KOSTEN_EINHEIT_EURO;   // „%" — bemessen wird an einem Betrag

            string nenner = satz.Substring(2).Trim();
            if (nenner.Length == 0) return DbWerte.KOSTEN_EINHEIT_EURO;

            if (nenner.EndsWith("·a", StringComparison.Ordinal))
                return nenner.Substring(0, nenner.Length - 2);
            return nenner + "/a";
        }

        private static string Text(string schluessel, string rueckfallDe)
        {
            string text = null;
            try { text = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(text) ? rueckfallDe : text;
        }

        private static Info N(string persistenz, string key, string de, string einheit,
                              bool invest, bool betrieb, bool absolut,
                              string einheitBetrieb = null)
        {
            return new Info
            {
                Persistenz = persistenz, ResourceKey = key, AnzeigeDe = de, Einheit = einheit,
                FuerInvest = invest, FuerBetrieb = betrieb, Absolut = absolut,
                EinheitBetrieb = einheitBetrieb,
            };
        }
    }
}
