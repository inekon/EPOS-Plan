using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Größe, um die es beim Übernahmeweg der Trägerkarte geht — der EINE Weg
    /// bekommt sie als Parameter mit; zwei getrennte Übernahmewege wären zwei
    /// Wahrheiten.
    /// </summary>
    public enum Rueckfallgroesse
    {
        /// <summary>CO₂-Faktor [g/kWh].</summary>
        Co2 = 0,

        /// <summary>Arbeitspreis [€ je Abrechnungseinheit].</summary>
        Arbeitspreis = 1,

        /// <summary>Leistungspreis [€/(kW·a)] bzw. [€/(kW·Monat)].</summary>
        Leistungspreis = 2
    }

    /// <summary>
    /// Ein Energieträger DERSELBEN KATEGORIE, der die gesuchte Größe trägt — ein
    /// Vorschlag, mehr nicht. Geschrieben wird erst, wenn der Anwender ihn wählt.
    /// </summary>
    public sealed class Rueckfallkandidat
    {
        /// <summary><c>energy_carrier.id</c> des Trägers, von dem der Wert stammt.</summary>
        public int TraegerId;

        /// <summary>Sein Anzeigename.</summary>
        public string Name = "";

        /// <summary>Der Wert, den er trägt — schon aufgelöst über seine Lesekette.</summary>
        public double Wert;

        /// <summary>Die Einheit des Wertes, damit man sieht, was man übernimmt.</summary>
        public string Einheit = "";

        /// <summary>Ist der Träger diesem Projekt zugeordnet? (Ordnet die Liste.)</summary>
        public bool ImProjekt;
    }

    /// <summary>
    /// <b>Der Übernahmeweg aus der Kategorie</b> — ein BEDIENWEG, kein Rechenweg
    /// (Anwenderentscheid 15.09.2026: „nur mit Rückfrage, Werte nicht ohne Wissen
    /// setzen!").
    ///
    /// <para><b>Was er ist.</b> Fehlt einem Energieträger der CO₂-Wert, der Arbeits-
    /// oder der Leistungspreis, nennt die Trägerkarte die Lücke und bietet an, den
    /// Wert eines Trägers DERSELBEN KATEGORIE zu übernehmen. Dieser Controller sagt,
    /// ob eine Lücke besteht, wer als Geber in Frage kommt und was er trägt — und er
    /// schreibt den gewählten Wert, nachdem der Anwender ihn gesehen und bestätigt
    /// hat.</para>
    ///
    /// <para><b>Was er ausdrücklich NICHT ist.</b> Ein automatischer Rückfall im
    /// Rechenweg. <see cref="Emissionsquelle"/> und
    /// <see cref="KostenEmissionRechner"/> bleiben unverändert: Ein zugeordneter
    /// Träger ohne CO₂-Wert ist dort weiterhin eine Datenlücke, ein Träger ohne
    /// Arbeitspreis weiterhin ein benannter Fehlgrund. Nichts von hier greift in eine
    /// Rechnung ein — <b>ein geliehener Wert wird nie still gesetzt</b>.</para>
    ///
    /// <para><b>Die Kategorie ist <c>energy_carrier.pricing_model</c></b> — derselbe
    /// KATEGORIECODE, auf dem die Zulässigkeitsprüfung
    /// (<see cref="EnergietraegerZulaessigkeit"/>) ruht. Die angezeigte Zeile
    /// „Gruppe: …" (<c>group_code</c>) ist feiner geschnitten: Sie trennt innerhalb
    /// derselben Kategorie (Gas, Wasserstoff, Sonstige Energieträger sind alle
    /// GASEOUS_FUEL). Über die Gruppe zu suchen hieße, dem Anwender Geber
    /// vorzuenthalten, die fachlich zur selben Familie gehören.</para>
    ///
    /// <para><b>Wohin geschrieben wird:</b> in die PROJEKTÜBERSTEUERUNG
    /// (<c>energy_project_settings</c> für diesen Träger in diesem Projekt), nie in
    /// den Katalog — der Katalog gilt für alle Projekte, die Entscheidung des
    /// Anwenders gilt für seines. Genau dort steht auch die oberste Ebene beider
    /// Leseketten (<see cref="EmissionsFaktorLader"/> Ebene <c>PROJEKT</c>,
    /// <c>custom_price_work</c>/<c>custom_price_power</c> in
    /// <see cref="KostenEmissionRechner"/>) — der übernommene Wert gilt damit sofort
    /// und ohne zweite Regel.</para>
    ///
    /// <para><b>Einheitentreue.</b> Ein Preis ist eine Zahl JE ABRECHNUNGSEINHEIT;
    /// 0,95 €/L in einen Träger zu schreiben, der nach Nm³ abrechnet, wäre eine
    /// falsche Zahl mit richtigem Anschein. Als Geber eines Preises kommt deshalb nur
    /// in Frage, wer dieselbe Abrechnungseinheit führt — beim Leistungspreis
    /// zusätzlich denselben Modus (Jahr/Monat). Der CO₂-Faktor steht bei jedem Träger
    /// in g/kWh und kennt diese Einschränkung nicht.</para>
    /// </summary>
    public static class EnergietraegerRueckfall
    {
        // =====================================================================
        //  Die Größen als Schlüssel (die Oberfläche reicht Zeichenketten durch)
        // =====================================================================

        /// <summary>Schlüssel der Größe „CO₂-Wert".</summary>
        public const string GROESSE_CO2 = "CO2";

        /// <summary>Schlüssel der Größe „Arbeitspreis".</summary>
        public const string GROESSE_ARBEITSPREIS = "ARBEITSPREIS";

        /// <summary>Schlüssel der Größe „Leistungspreis".</summary>
        public const string GROESSE_LEISTUNGSPREIS = "LEISTUNGSPREIS";

        /// <summary>Der Schlüssel einer Größe.</summary>
        public static string Schluessel(Rueckfallgroesse groesse)
        {
            switch (groesse)
            {
                case Rueckfallgroesse.Arbeitspreis: return GROESSE_ARBEITSPREIS;
                case Rueckfallgroesse.Leistungspreis: return GROESSE_LEISTUNGSPREIS;
                default: return GROESSE_CO2;
            }
        }

        /// <summary>Die Größe zu einem Schlüssel; <c>false</c> = unbekannt.</summary>
        public static bool Groesse(string schluessel, out Rueckfallgroesse groesse)
        {
            groesse = Rueckfallgroesse.Co2;
            if (string.Equals(schluessel, GROESSE_CO2, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(schluessel, GROESSE_ARBEITSPREIS, StringComparison.OrdinalIgnoreCase))
            { groesse = Rueckfallgroesse.Arbeitspreis; return true; }
            if (string.Equals(schluessel, GROESSE_LEISTUNGSPREIS, StringComparison.OrdinalIgnoreCase))
            { groesse = Rueckfallgroesse.Leistungspreis; return true; }
            return false;
        }

        // =====================================================================
        //  Die Kategorie
        // =====================================================================

        /// <summary>
        /// Der KATEGORIECODE eines Trägers (<c>energy_carrier.pricing_model</c>);
        /// leer, wenn der Träger unbekannt ist.
        /// </summary>
        public static string Kategorie(int traegerId)
        {
            if (traegerId <= 0) return "";
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT pricing_model FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", traegerId));
                if (o != null && o != DBNull.Value) return Convert.ToString(o) ?? "";
            }
            catch { }
            return "";
        }

        /// <summary>
        /// Der Name einer Kategorie in Worten. Die sechs Codes sind Technik; in einer
        /// Rückfrage steht der Name. Ohne Ressourcenschlüssel bleibt der Code stehen —
        /// eine erfundene Übersetzung wäre schlechter als die ehrliche Technik.
        /// </summary>
        public static string KategorieName(string code)
        {
            string c = (code ?? "").Trim().ToUpperInvariant();
            switch (c)
            {
                case "ELECTRICITY": return T("ETV_KAT_ELECTRICITY", "Strom");
                case "GASEOUS_FUEL": return T("ETV_KAT_GASEOUS_FUEL", "Gasförmige Brennstoffe");
                case "LIQUID_FUEL": return T("ETV_KAT_LIQUID_FUEL", "Flüssige Brennstoffe");
                case "SOLID_FUEL": return T("ETV_KAT_SOLID_FUEL", "Feste Brennstoffe");
                case "HEAT": return T("ETV_KAT_HEAT", "Wärme");
                case "ANIMAL_FAT": return T("ETV_KAT_ANIMAL_FAT", "Tierische Fette");
                default: return c;
            }
        }

        // =====================================================================
        //  Was ein Träger trägt
        // =====================================================================

        /// <summary>
        /// Der Wert, den ein Träger für diese Größe trägt; <c>null</c> = nicht
        /// gepflegt.
        ///
        /// <para><b>Beide Leseketten bleiben, wo sie sind.</b> Der CO₂-Faktor kommt
        /// aus <see cref="Emissionsquelle"/> und wird über deren
        /// <c>Co2Gepflegt</c> beurteilt — dieselbe Kennzeichnung, die es dort schon
        /// gab und die bis hierher niemand auswertete. Die Preise kommen aus
        /// <see cref="KostenEmissionRechner.PreiseDesTraegers"/>, wo „0 zählt als
        /// nicht gepflegt" seit jeher steht.</para>
        /// </summary>
        public static double? Wert(int idProjekt, int traegerId, Rueckfallgroesse groesse)
        {
            if (traegerId <= 0) return null;

            if (groesse == Rueckfallgroesse.Co2)
            {
                try
                {
                    Emissionsfaktoren f = Emissionsquelle.Fuer(
                        idProjekt, traegerId, 0, Emissionsquelle.Modus(idProjekt));
                    return f.Co2Gepflegt ? (double?)f.Co2GKwh : null;
                }
                catch { return null; }
            }

            try
            {
                double? arbeit, leistung;
                KostenEmissionRechner.PreiseDesTraegers(idProjekt, traegerId,
                                                        out arbeit, out leistung);
                return groesse == Rueckfallgroesse.Arbeitspreis ? arbeit : leistung;
            }
            catch { return null; }
        }

        /// <summary>
        /// Die Einheit, in der diese Größe bei diesem Träger steht — sie gehört an
        /// jede Zahl, die zur Übernahme angeboten wird.
        /// </summary>
        public static string Einheit(int traegerId, Rueckfallgroesse groesse)
        {
            if (groesse == Rueckfallgroesse.Co2) return "g/kWh";

            Steckbrief s = Steckbrief.Lesen(traegerId);
            if (groesse == Rueckfallgroesse.Leistungspreis)
                return EnergietraegerPreiskarte.LeistungspreisEinheit(s.LeistungsModusMonat);

            return EnergietraegerPreiskarte.ArbeitspreisEinheit(s.Abrechnungseinheit, true);
        }

        // =====================================================================
        //  Die Kandidaten
        // =====================================================================

        /// <summary>
        /// Die Träger derselben Kategorie, die die gesuchte Größe tragen — ohne den
        /// Träger selbst, ohne die, bei denen die Einheit nicht passt, und ohne die,
        /// die selbst nichts tragen. Leere Liste = es gibt nichts zu übernehmen; dann
        /// bleibt es bei der Lücke.
        ///
        /// <para><b>Die Reihenfolge ist fest</b> und hängt an keiner Kultur: erst die
        /// dem Projekt ZUGEORDNETEN Träger (sie sind die nächsten Verwandten und die,
        /// deren Werte der Anwender selbst gepflegt hat), dann die übrigen; innerhalb
        /// beider Gruppen nach Namen (<see cref="StringComparison.OrdinalIgnoreCase"/>),
        /// bei gleichem Namen nach Id.</para>
        /// </summary>
        public static IReadOnlyList<Rueckfallkandidat> Kandidaten(int idProjekt, int traegerId,
                                                                 Rueckfallgroesse groesse)
        {
            var liste = new List<Rueckfallkandidat>();

            string kategorie = Kategorie(traegerId);
            if (kategorie.Length == 0) return liste;

            Steckbrief ziel = Steckbrief.Lesen(traegerId);

            DataTable dt;
            try
            {
                dt = DataRepository.GetDataTable(
                    "SELECT id, [name], billing_unit, price_power_modus FROM energy_carrier " +
                    "WHERE pricing_model = ? AND id <> ? ORDER BY id",
                    new DbParam("@k", kategorie), new DbParam("@c", traegerId));
            }
            catch { return liste; }
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id;
                try { id = Convert.ToInt32(r["id"]); }
                catch { continue; }
                if (id <= 0) continue;

                var geber = new Steckbrief
                {
                    Abrechnungseinheit = Text(r, "billing_unit"),
                    LeistungsModusMonat = string.Equals(Text(r, "price_power_modus"),
                        DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal)
                };
                if (!Passt(ziel, geber, groesse)) continue;

                double? wert = Wert(idProjekt, id, groesse);
                if (!wert.HasValue || wert.Value <= 0.0) continue;

                liste.Add(new Rueckfallkandidat
                {
                    TraegerId = id,
                    Name = Text(r, "name"),
                    Wert = wert.Value,
                    Einheit = Einheit(id, groesse),
                    ImProjekt = ImProjekt(idProjekt, id)
                });
            }

            liste.Sort(Vergleich);
            return liste;
        }

        /// <summary>Die feste Reihenfolge: Projektträger zuerst, dann Name, dann Id.</summary>
        private static int Vergleich(Rueckfallkandidat a, Rueckfallkandidat b)
        {
            if (a.ImProjekt != b.ImProjekt) return a.ImProjekt ? -1 : 1;
            int n = string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase);
            return n != 0 ? n : a.TraegerId.CompareTo(b.TraegerId);
        }

        /// <summary>
        /// Darf dieser Geber diese Größe stellen? Der CO₂-Faktor steht überall in
        /// g/kWh; ein Preis nur dann, wenn die Abrechnungseinheit — und beim
        /// Leistungspreis auch der Modus — dieselbe ist.
        /// </summary>
        private static bool Passt(Steckbrief ziel, Steckbrief geber, Rueckfallgroesse groesse)
        {
            if (groesse == Rueckfallgroesse.Co2) return true;
            if (groesse == Rueckfallgroesse.Leistungspreis
                && ziel.LeistungsModusMonat != geber.LeistungsModusMonat) return false;

            return string.Equals(
                EnergietraegerPreisCtrl.EinheitSchluessel(ziel.Abrechnungseinheit),
                EnergietraegerPreisCtrl.EinheitSchluessel(geber.Abrechnungseinheit),
                StringComparison.Ordinal);
        }

        // =====================================================================
        //  Die Übernahme
        // =====================================================================

        /// <summary>
        /// Schreibt den übernommenen Wert in die PROJEKTÜBERSTEUERUNG dieses Trägers.
        /// <c>false</c> = nichts geschrieben (kein Projekt, kein Träger, unbekannte
        /// Größe oder ein Schreibfehler).
        ///
        /// <para><b>Nur die eine Spalte.</b> Drei feste Anweisungen statt eines
        /// zusammengesetzten SQL-Textes — und nur der Wert, den der Anwender bestätigt
        /// hat; alles andere der Karte bleibt unberührt, bis er speichert. Wo noch
        /// keine Projektzeile stand, entsteht sie: Die Projektübersteuerung IST die
        /// Zuordnung, und das Speichern der Karte legt sie ohnehin an
        /// (<c>EnergietraegerKatalogCtrl.InsProjekt</c>).</para>
        ///
        /// <para><b>Der Katalog bleibt unverändert.</b> Weder
        /// <c>energy_carrier</c> noch <c>emissionswert</c> werden angefasst.</para>
        /// </summary>
        public static bool Uebernehmen(int idProjekt, int traegerId,
                                       Rueckfallgroesse groesse, double wert)
        {
            if (idProjekt <= 0 || traegerId <= 0) return false;

            try
            {
                ZeileSicherstellen(idProjekt, traegerId);

                DbParam[] p =
                {
                    new DbParam("@w", wert),
                    new DbParam("@p", idProjekt),
                    new DbParam("@c", traegerId)
                };

                switch (groesse)
                {
                    case Rueckfallgroesse.Co2:
                        return DataRepository.ExecuteNonQuery(
                            "UPDATE energy_project_settings SET co2 = ? " +
                            "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?", p) > 0;

                    case Rueckfallgroesse.Arbeitspreis:
                        return DataRepository.ExecuteNonQuery(
                            "UPDATE energy_project_settings SET custom_price_work = ? " +
                            "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?", p) > 0;

                    case Rueckfallgroesse.Leistungspreis:
                        return DataRepository.ExecuteNonQuery(
                            "UPDATE energy_project_settings SET custom_price_power = ? " +
                            "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?", p) > 0;
                }
            }
            catch { }
            return false;
        }

        /// <summary>Legt die Projektzeile an, wenn es noch keine gibt (Upsert-Hälfte).</summary>
        private static void ZeileSicherstellen(int idProjekt, int traegerId)
        {
            if (ImProjekt(idProjekt, traegerId)) return;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID_Projekt, [ID_Energieträger]) " +
                "VALUES (?, ?)",
                new DbParam("@p", idProjekt), new DbParam("@c", traegerId));
        }

        private static bool ImProjekt(int idProjekt, int traegerId)
        {
            if (idProjekt <= 0) return false;
            try { return EnergietraegerPreisCtrl.ImProjekt(idProjekt, traegerId); }
            catch { return false; }
        }

        // =====================================================================
        //  Kleinwerkzeug
        // =====================================================================

        /// <summary>Was ein Träger für die Einheitenfrage mitbringt.</summary>
        private sealed class Steckbrief
        {
            public string Abrechnungseinheit = "";
            public bool LeistungsModusMonat;

            public static Steckbrief Lesen(int traegerId)
            {
                var s = new Steckbrief();
                if (traegerId <= 0) return s;
                try
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT billing_unit, price_power_modus FROM energy_carrier WHERE id = ?",
                        new DbParam("@c", traegerId));
                    if (dt == null || dt.Rows.Count == 0) return s;

                    s.Abrechnungseinheit = Text(dt.Rows[0], "billing_unit");
                    s.LeistungsModusMonat = string.Equals(Text(dt.Rows[0], "price_power_modus"),
                        DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal);
                }
                catch { }
                return s;
            }
        }

        private static string Text(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            return Convert.ToString(r[spalte]) ?? "";
        }

        /// <summary>
        /// MyResource mit deutschem Rückfall (Drei-Schichten-Regel) — dasselbe Muster
        /// wie <c>WirtschaftlichkeitCtrl.T</c>.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
