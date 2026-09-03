using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// KATALOGPFLEGE der Emissionsarten und Emissionswerte (Etappe E4,
    /// Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md § 4.2): Arten lesen,
    /// anlegen, ändern, löschen und abwählen; Werte einer Art je Träger lesen,
    /// eigene Werte pflegen, einen Katalogwert als Trägerwert übernehmen (F8).
    ///
    /// <para>UI-frei und testbar — Hausmuster Ä9
    /// (<see cref="EnergietraegerKatalogCtrl"/>). Der Dialog
    /// <c>Form_Emissionskatalog</c> ruft ausschließlich hierher; er kennt keine
    /// einzige SQL-Zeile.</para>
    ///
    /// <para><b>Drei Schutzregeln, alle mit Klartextgrund statt stiller
    /// Verweigerung:</b></para>
    /// <list type="number">
    ///   <item><description>Die Pflichtart CO₂ lässt sich weder löschen noch
    ///     abwählen (F1).</description></item>
    ///   <item><description>Ausgelieferte Arten lassen sich nicht löschen, nur
    ///     abwählen — „abwählen statt löschen".</description></item>
    ///   <item><description>Eine Art mit Werten bleibt stehen; sonst risse das
    ///     Löschen gepflegte Zahlen weg (dieselbe Regel, die die Beziehung
    ///     FK_emissionswert_art in der Datenbank durchsetzt).</description></item>
    /// </list>
    ///
    /// <para><b>ACE-Falle:</b> Ganzzahlen stehen als LITERAL im SQL, Parameter
    /// gibt es nur für Text und Kommazahlen — ein <c>?</c> in einer Unterabfrage
    /// bindet unter ACE still falsch (Hausbefund).</para>
    /// </summary>
    public static class EmissionskatalogCtrl
    {
        /// <summary>Herkunftstext eines Wertes, der noch aus der Altspalte des
        /// Trägers stammt (kein Katalogwert, keine belegte Quelle).</summary>
        public const string TEXT_TRAEGERSPALTE = "Trägerspalte (Altbestand)";

        /// <summary>Herkunftstext einer Projektübersteuerung (Konzept § 3: die
        /// Projektebene bleibt bis zur Quellenwahl-Umsetzung bei den Altspalten).</summary>
        public const string TEXT_PROJEKTWERT = "Projektwert";

        // =====================================================================
        // Emissionsarten
        // =====================================================================

        /// <summary>
        /// Alle Arten des Katalogs, nach <c>sortierung</c> geordnet.
        /// <paramref name="nurAusgewaehlte"/> liefert die Feldliste des
        /// Emissions-Tabs (F5) — die Pflichtart CO₂ ist dort IMMER dabei, auch
        /// wenn ihr Häkchen auf einer fremden Datenbank fehlen sollte.
        /// </summary>
        /// <returns>Leere Liste, wenn die Tabelle fehlt (Migrationsschritt 57
        /// nicht gelaufen) — der Aufrufer entscheidet, was er dann zeigt.</returns>
        public static List<EmissionsartModel> Arten(bool nurAusgewaehlte)
        {
            var liste = new List<EmissionsartModel>();
            try
            {
                string sql =
                    "SELECT id, kuerzel, [name], einheit, co2_aequivalent, aequivalent_quelle, " +
                    "       ist_pflicht, ausgewaehlt, ist_auslieferung, sortierung " +
                    "FROM " + SchemaKatalog.TAB_EMISSIONSART;
                if (nurAusgewaehlte) sql += " WHERE ausgewaehlt = TRUE OR ist_pflicht = TRUE";
                sql += " ORDER BY sortierung, kuerzel";

                DataTable dt = DataRepository.GetDataTable(sql);
                if (dt == null) return liste;

                foreach (DataRow r in dt.Rows) liste.Add(ArtAus(r));
            }
            catch { /* Tabelle fehlt - leere Liste, kein Dialog */ }
            return liste;
        }

        /// <summary>Eine Art an ihrer ID; <c>null</c>, wenn es sie nicht gibt.</summary>
        public static EmissionsartModel Art(int artId)
        {
            if (artId <= 0) return null;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT id, kuerzel, [name], einheit, co2_aequivalent, aequivalent_quelle, " +
                    "       ist_pflicht, ausgewaehlt, ist_auslieferung, sortierung " +
                    "FROM " + SchemaKatalog.TAB_EMISSIONSART +
                    " WHERE id = " + Ganz(artId));
                if (dt != null && dt.Rows.Count > 0) return ArtAus(dt.Rows[0]);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Neue eigene Art. <paramref name="grund"/> nennt bei <c>0</c> im
        /// Klartext, was fehlt (leeres Kürzel, doppeltes Kürzel).
        /// </summary>
        /// <returns>Vergebene ID oder 0.</returns>
        public static int ArtAnlegen(EmissionsartModel a, out string grund)
        {
            grund = "";
            if (a == null || string.IsNullOrWhiteSpace(a.Kuerzel))
            {
                grund = "Das Kürzel darf nicht leer sein.";
                return 0;
            }
            a.Kuerzel = a.Kuerzel.Trim().ToUpperInvariant().Replace(' ', '_');

            if (KuerzelBelegt(a.Kuerzel, 0))
            {
                grund = "Das Kürzel „" + a.Kuerzel + "“ ist bereits vergeben.";
                return 0;
            }

            int id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSART +
                " (kuerzel, [name], einheit, co2_aequivalent, aequivalent_quelle, " +
                "  ist_pflicht, ausgewaehlt, ist_auslieferung, sortierung) " +
                "VALUES (?, ?, ?, ?, ?, FALSE, ?, FALSE, " + Ganz(a.Sortierung) + ")",
                new[]
                {
                    Text(a.Kuerzel), Text(a.Name), Text(EinheitOderMg(a.Einheit)),
                    Komma(a.Co2Aequivalent), Text(a.AequivalentQuelle),
                    JaNein(a.Ausgewaehlt)
                });
            if (id <= 0) grund = "Die Art konnte nicht angelegt werden.";
            else a.ID = id;
            return id;
        }

        /// <summary>
        /// Ändert Name, Einheit, Äquivalenzfaktor und dessen Quelle.
        ///
        /// <para><b>Bei CO₂ bleibt der Faktor 1</b> (F1/F2) — die Sperre steht
        /// hier und nicht nur im Dialog, damit es bei EINER Fassung der Regel
        /// bleibt. Das Kürzel einer ausgelieferten Art ist unveränderlich: an ihm
        /// hängen die Steuerwerte aus <see cref="DbWerte"/>.</para>
        /// </summary>
        public static bool ArtAendern(EmissionsartModel a, out string grund)
        {
            grund = "";
            if (a == null || a.ID <= 0) { grund = "Keine Art gewählt."; return false; }

            EmissionsartModel bestand = Art(a.ID);
            if (bestand == null) { grund = "Die Art ist nicht mehr vorhanden."; return false; }

            string kuerzel = bestand.Kuerzel;
            if (!bestand.IstAuslieferung && !string.IsNullOrWhiteSpace(a.Kuerzel))
            {
                kuerzel = a.Kuerzel.Trim().ToUpperInvariant().Replace(' ', '_');
                if (KuerzelBelegt(kuerzel, a.ID))
                {
                    grund = "Das Kürzel „" + kuerzel + "“ ist bereits vergeben.";
                    return false;
                }
            }

            double gwp = bestand.IstPflicht ? 1.0 : a.Co2Aequivalent;
            string quelle = bestand.IstPflicht ? bestand.AequivalentQuelle : a.AequivalentQuelle;

            bool ok = DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_EMISSIONSART +
                " SET kuerzel = ?, [name] = ?, einheit = ?, co2_aequivalent = ?, " +
                "     aequivalent_quelle = ? WHERE id = " + Ganz(a.ID),
                Text(kuerzel), Text(a.Name), Text(EinheitOderMg(a.Einheit)),
                Komma(gwp), Text(quelle));
            if (!ok) grund = "Die Änderung konnte nicht gespeichert werden.";
            return ok;
        }

        /// <summary>
        /// Löschen mit den drei Schutzregeln (§ 4.2). <paramref name="grund"/>
        /// nennt bei <c>false</c>, was die Art hält — samt dem Angebot
        /// „abwählen statt löschen", wo es zutrifft.
        /// </summary>
        public static bool ArtLoeschen(int artId, out string grund)
        {
            grund = "";
            EmissionsartModel a = Art(artId);
            if (a == null) { grund = "Die Art ist nicht mehr vorhanden."; return false; }

            if (a.IstPflicht)
            {
                grund = "CO₂ ist die Pflichtart und lässt sich weder löschen noch abwählen.";
                return false;
            }
            if (a.IstAuslieferung)
            {
                grund = "„" + a.Name + "“ ist eine ausgelieferte Art. Sie lässt sich " +
                        "nicht löschen — wählen Sie sie ab, dann verschwindet sie aus " +
                        "den Emissionsfeldern.";
                return false;
            }

            int werte = Zaehle("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EMISSIONSWERT +
                               " WHERE emissionsart_id = " + Ganz(artId));
            if (werte > 0)
            {
                grund = "An der Art hängen " + werte + " Wert(e). Löschen würde sie " +
                        "mitnehmen — wählen Sie die Art stattdessen ab.";
                return false;
            }

            bool ok = DataRepository.ExecuteSQL(
                "DELETE FROM " + SchemaKatalog.TAB_EMISSIONSART + " WHERE id = " + Ganz(artId));
            if (!ok) grund = "Die Art konnte nicht gelöscht werden.";
            return ok;
        }

        /// <summary>
        /// Setzt das Häkchen <c>ausgewaehlt</c> (F5) — global, nicht je Träger.
        /// CO₂ bleibt gesetzt; der Versuch, es abzuwählen, wird mit Grund
        /// abgelehnt statt still ignoriert.
        /// </summary>
        public static bool AuswahlSetzen(int artId, bool ausgewaehlt, out string grund)
        {
            grund = "";
            EmissionsartModel a = Art(artId);
            if (a == null) { grund = "Die Art ist nicht mehr vorhanden."; return false; }

            if (a.IstPflicht && !ausgewaehlt)
            {
                grund = "CO₂ ist Pflicht und bleibt ausgewählt (Konzept F1).";
                return false;
            }

            bool ok = DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_EMISSIONSART +
                " SET ausgewaehlt = ? WHERE id = " + Ganz(artId),
                JaNein(ausgewaehlt));
            if (!ok) grund = "Die Auswahl konnte nicht gespeichert werden.";
            return ok;
        }

        // =====================================================================
        // Emissionswerte
        // =====================================================================

        /// <summary>
        /// Die Werte einer Art, die für den übergebenen Träger in Frage kommen:
        /// seine eigenen Zeilen UND die trägerunabhängigen Vorlagen
        /// (<c>carrier_id IS NULL</c>, z. B. der Strommix). Ohne Trägerkontext
        /// (<paramref name="carrierId"/> ≤ 0) bleiben nur die Vorlagen —
        /// genau die Sicht des Verwaltungsmodus (§ 4.2).
        /// </summary>
        public static List<EmissionswertModel> Werte(int artId, int carrierId)
        {
            var liste = new List<EmissionswertModel>();
            if (artId <= 0) return liste;
            try
            {
                string sql =
                    "SELECT id, emissionsart_id, carrier_id, quelle, quelle_text, wert, " +
                    "       ist_co2e, ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab " +
                    "FROM " + SchemaKatalog.TAB_EMISSIONSWERT +
                    " WHERE emissionsart_id = " + Ganz(artId) + " AND (carrier_id IS NULL";
                if (carrierId > 0) sql += " OR carrier_id = " + Ganz(carrierId);

                // Der GELTENDE Wert steht oben. NICHT über „ist_aktiv DESC":
                // Access führt WAHR als -1 und sortiert absteigend deshalb FALSCH
                // (0) zuerst - der aktive Wert landete ans Listenende, wo ihn
                // niemand sucht (Befund 29.08.2026 am Sichtbeleg). IIF macht die
                // Absicht unabhängig von der Kodierung lesbar.
                sql += ") ORDER BY IIF(ist_aktiv, 0, 1), quelle, wert";

                DataTable dt = DataRepository.GetDataTable(sql);
                if (dt == null) return liste;

                foreach (DataRow r in dt.Rows) liste.Add(WertAus(r));
            }
            catch { }
            return liste;
        }

        /// <summary>Die AKTIVEN Werte eines Trägers, nach Art-ID abrufbar — der
        /// Leseweg des Emissions-Tabs (ein Zugriff statt einer je Art).</summary>
        public static Dictionary<int, EmissionswertModel> AktiveWerte(int carrierId)
        {
            var d = new Dictionary<int, EmissionswertModel>();
            if (carrierId <= 0) return d;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT id, emissionsart_id, carrier_id, quelle, quelle_text, wert, " +
                    "       ist_co2e, ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab " +
                    "FROM " + SchemaKatalog.TAB_EMISSIONSWERT +
                    " WHERE carrier_id = " + Ganz(carrierId) + " AND ist_aktiv = TRUE");
                if (dt == null) return d;

                foreach (DataRow r in dt.Rows)
                {
                    EmissionswertModel w = WertAus(r);
                    if (!d.ContainsKey(w.EmissionsartId)) d.Add(w.EmissionsartId, w);
                }
            }
            catch { }
            return d;
        }

        /// <summary>
        /// Legt einen EIGENEN Wert an (Vorlage ohne Träger oder Trägerwert).
        /// Ausgelieferte Zeilen entstehen ausschließlich in der Migration.
        /// </summary>
        public static int WertAnlegen(EmissionswertModel w, out string grund)
        {
            grund = "";
            if (w == null || w.EmissionsartId <= 0) { grund = "Keine Art gewählt."; return 0; }
            if (!w.Wert.HasValue) { grund = "Ohne Zahlenwert gibt es nichts zu speichern."; return 0; }

            string sql =
                "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSWERT +
                " (emissionsart_id, carrier_id, quelle, quelle_text, wert, ist_co2e, " +
                "  ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab) " +
                "VALUES (" + Ganz(w.EmissionsartId) + ", " +
                (w.CarrierId.HasValue ? Ganz(w.CarrierId.Value) : "NULL") +
                ", ?, ?, ?, ?, " + (w.IstAktiv ? "TRUE" : "FALSE") + ", " +
                (w.HerkunftId.HasValue ? Ganz(w.HerkunftId.Value) : "NULL") + ", FALSE, ?)";

            int id = DataRepository.ExecuteInsertAndGetId(sql, new[]
            {
                Text(DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT),
                Text(string.IsNullOrWhiteSpace(w.QuelleText)
                     ? DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT : w.QuelleText),
                Komma(w.Wert.Value), JaNein(w.IstCo2e),
                Datum(w.GueltigAb ?? DateTime.Today)
            });
            if (id <= 0) grund = "Der Wert konnte nicht angelegt werden.";
            else w.ID = id;
            return id;
        }

        /// <summary>Ändert einen eigenen Wert; ausgelieferte Zeilen bleiben
        /// unangetastet (§ 4.2) — der Grund wird benannt.</summary>
        public static bool WertAendern(EmissionswertModel w, out string grund)
        {
            grund = "";
            if (w == null || w.ID <= 0) { grund = "Kein Wert gewählt."; return false; }
            if (!EigenerWert(w.ID, out grund)) return false;
            if (!w.Wert.HasValue) { grund = "Ohne Zahlenwert gibt es nichts zu speichern."; return false; }

            bool ok = DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_EMISSIONSWERT +
                " SET quelle_text = ?, wert = ?, ist_co2e = ?, gueltig_ab = ? " +
                "WHERE id = " + Ganz(w.ID),
                Text(string.IsNullOrWhiteSpace(w.QuelleText)
                     ? DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT : w.QuelleText),
                Komma(w.Wert.Value), JaNein(w.IstCo2e),
                Datum(w.GueltigAb ?? DateTime.Today));
            if (!ok) grund = "Die Änderung konnte nicht gespeichert werden.";
            return ok;
        }

        /// <summary>Löscht einen eigenen Wert; ausgelieferte Zeilen bleiben.</summary>
        public static bool WertLoeschen(int wertId, out string grund)
        {
            grund = "";
            if (wertId <= 0) { grund = "Kein Wert gewählt."; return false; }
            if (!EigenerWert(wertId, out grund)) return false;

            bool ok = DataRepository.ExecuteSQL(
                "DELETE FROM " + SchemaKatalog.TAB_EMISSIONSWERT + " WHERE id = " + Ganz(wertId));
            if (!ok) grund = "Der Wert konnte nicht gelöscht werden.";
            return ok;
        }

        /// <summary>
        /// ÜBERNEHMEN im Verwaltungsmodus (F8): kopiert <paramref name="vorlage"/>
        /// als aktiven Trägerwert — vorhandene aktive Zeile wird aktualisiert,
        /// sonst entsteht eine neue. Der Zahlenwert wird KOPIERT, die Herkunft
        /// vermerkt; eine spätere Katalogänderung wirkt nicht zurück.
        ///
        /// <para>Aus dem Emissions-Tab heraus wird NICHT hierher geschrieben,
        /// sondern der Wert zurückgereicht — sonst bräche die deferred-Semantik
        /// (Ä12/Ä14): „Abbrechen" muss auch die Übernahme verwerfen.</para>
        /// </summary>
        public static bool Uebernehmen(int carrierId, EmissionswertModel vorlage, out string grund)
        {
            grund = "";
            if (carrierId <= 0) { grund = "Ohne Trägerkontext gibt es nichts zu übernehmen."; return false; }
            if (vorlage == null || !vorlage.Wert.HasValue)
            {
                grund = "Der gewählte Eintrag trägt keinen Zahlenwert.";
                return false;
            }

            EmissionswertModel aktiv = null;
            Dictionary<int, EmissionswertModel> alle = AktiveWerte(carrierId);
            alle.TryGetValue(vorlage.EmissionsartId, out aktiv);

            string quelle = string.IsNullOrEmpty(vorlage.Quelle)
                ? DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT : vorlage.Quelle;
            string text = string.IsNullOrEmpty(vorlage.QuelleText) ? quelle : vorlage.QuelleText;
            string herkunft = vorlage.ID > 0 ? Ganz(vorlage.ID) : "NULL";

            bool ok;
            if (aktiv != null && aktiv.ID > 0)
            {
                ok = DataRepository.ExecuteSQL(
                    "UPDATE " + SchemaKatalog.TAB_EMISSIONSWERT +
                    " SET quelle = ?, quelle_text = ?, wert = ?, ist_co2e = ?, " +
                    "     herkunft_id = " + herkunft + ", gueltig_ab = ? " +
                    "WHERE id = " + Ganz(aktiv.ID),
                    Text(quelle), Text(text), Komma(vorlage.Wert.Value),
                    JaNein(vorlage.IstCo2e), Datum(DateTime.Today));
            }
            else
            {
                ok = DataRepository.ExecuteSQL(
                    "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSWERT +
                    " (emissionsart_id, carrier_id, quelle, quelle_text, wert, ist_co2e, " +
                    "  ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab) " +
                    "VALUES (" + Ganz(vorlage.EmissionsartId) + ", " + Ganz(carrierId) +
                    ", ?, ?, ?, ?, TRUE, " + herkunft + ", FALSE, ?)",
                    Text(quelle), Text(text), Komma(vorlage.Wert.Value),
                    JaNein(vorlage.IstCo2e), Datum(DateTime.Today));
            }
            if (!ok) grund = "Der Wert konnte nicht übernommen werden.";
            return ok;
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>true, wenn der Wert vom Anwender stammt und deshalb änderbar
        /// ist; sonst steht der Grund im Klartext bereit.</summary>
        private static bool EigenerWert(int wertId, out string grund)
        {
            grund = "";
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT quelle, ist_auslieferung FROM " + SchemaKatalog.TAB_EMISSIONSWERT +
                    " WHERE id = " + Ganz(wertId));
                if (dt == null || dt.Rows.Count == 0)
                {
                    grund = "Der Wert ist nicht mehr vorhanden.";
                    return false;
                }
                DataRow r = dt.Rows[0];
                bool auslieferung = r["ist_auslieferung"] != DBNull.Value &&
                                    Convert.ToBoolean(r["ist_auslieferung"]);
                string quelle = Txt(r["quelle"]);
                if (auslieferung || !string.Equals(quelle,
                        DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT, StringComparison.OrdinalIgnoreCase))
                {
                    grund = "Der Eintrag stammt aus der Auslieferung (" +
                            (quelle.Length > 0 ? quelle : "Katalog") + ") und ist unveränderlich. " +
                            "Legen Sie einen eigenen Wert an oder übernehmen Sie diesen.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                grund = ex.Message;
                return false;
            }
        }

        private static bool KuerzelBelegt(string kuerzel, int ausserId)
        {
            string sql = "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EMISSIONSART +
                         " WHERE kuerzel = ?";
            if (ausserId > 0) sql += " AND id <> " + Ganz(ausserId);
            try
            {
                object o = DataRepository.ExecuteScalar(sql, Text(kuerzel));
                return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
            }
            catch { return false; }
        }

        private static int Zaehle(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        private static EmissionsartModel ArtAus(DataRow r)
        {
            return new EmissionsartModel
            {
                ID = Ganzzahl(r["id"]),
                Kuerzel = Txt(r["kuerzel"]),
                Name = Txt(r["name"]),
                Einheit = EinheitOderMg(Txt(r["einheit"])),
                Co2Aequivalent = Kommazahl(r["co2_aequivalent"]),
                AequivalentQuelle = Txt(r["aequivalent_quelle"]),
                IstPflicht = Wahr(r["ist_pflicht"]),
                Ausgewaehlt = Wahr(r["ausgewaehlt"]),
                IstAuslieferung = Wahr(r["ist_auslieferung"]),
                Sortierung = Ganzzahl(r["sortierung"])
            };
        }

        private static EmissionswertModel WertAus(DataRow r)
        {
            return new EmissionswertModel
            {
                ID = Ganzzahl(r["id"]),
                EmissionsartId = Ganzzahl(r["emissionsart_id"]),
                CarrierId = GanzOderNull(r["carrier_id"]),
                Quelle = Txt(r["quelle"]),
                QuelleText = Txt(r["quelle_text"]),
                Wert = KommaOderNull(r["wert"]),
                IstCo2e = Wahr(r["ist_co2e"]),
                IstAktiv = Wahr(r["ist_aktiv"]),
                HerkunftId = GanzOderNull(r["herkunft_id"]),
                IstAuslieferung = Wahr(r["ist_auslieferung"]),
                GueltigAb = r["gueltig_ab"] == DBNull.Value
                            ? (DateTime?)null : Convert.ToDateTime(r["gueltig_ab"])
            };
        }

        /// <summary>Nur die beiden bekannten Einheiten (F4); alles andere gilt als
        /// mg/kWh — eine dritte Einheit hätte im Rechenweg keine Entsprechung.</summary>
        internal static string EinheitOderMg(string einheit)
        {
            return string.Equals(einheit, DbWerte.EMISSION_EINHEIT_G_KWH,
                                 StringComparison.OrdinalIgnoreCase)
                   ? DbWerte.EMISSION_EINHEIT_G_KWH
                   : DbWerte.EMISSION_EINHEIT_MG_KWH;
        }

        internal static string Ganz(int wert)
        {
            return wert.ToString(CultureInfo.InvariantCulture);
        }

        internal static DbParam Text(string wert)
        {
            return new DbParam("@t", DbParamTyp.VarWChar, 255)
            { Wert = (object)wert ?? DBNull.Value };
        }

        internal static DbParam Komma(double wert)
        {
            return new DbParam("@d", DbParamTyp.Double) { Wert = wert };
        }

        internal static DbParam JaNein(bool wert)
        {
            return new DbParam("@b", DbParamTyp.Boolean) { Wert = wert };
        }

        internal static DbParam Datum(DateTime wert)
        {
            return new DbParam("@dt", DbParamTyp.Date) { Wert = wert };
        }

        internal static string Txt(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        internal static bool Wahr(object o)
        {
            return o != null && o != DBNull.Value && Convert.ToBoolean(o);
        }

        internal static int Ganzzahl(object o)
        {
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        internal static int? GanzOderNull(object o)
        {
            return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o);
        }

        internal static double Kommazahl(object o)
        {
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o);
        }

        internal static double? KommaOderNull(object o)
        {
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
        }
    }
}
