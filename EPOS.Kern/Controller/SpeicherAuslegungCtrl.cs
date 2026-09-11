using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Projektgebundene Auslegungsprofile und Kostenadapter, ohne UI-Abhängigkeit.</summary>
    public static partial class SpeicherAuslegungCtrl
    {
        public const string AktuellerStand = "@Aktuell";

        /// <summary>
        /// Die EINE Beschreibung von <c>Tab_SpeicherAuslegung</c> — gelesen von
        /// Schemaschritt 73, von <see cref="SchemaSicherstellen"/> (der stillen
        /// Selbstanlage) und vom Werkzeug <c>Testdatenbankschema</c>.
        ///
        /// <para><b><c>STRICT</c> seit Schemaschritt 74</b> (Auftrag #178, 11.09.2026).
        /// Jede andere Fachtabelle des Zielschemas trägt es; diese eine war die Ausnahme,
        /// und eine neue Datenbank hätte sie ohne diesen Zusatz immer wieder ohne STRICT
        /// bekommen. Die Spaltentypen sind unverändert — <c>INTEGER</c> und <c>TEXT</c>
        /// sind bereits die Typen, die eine STRICT-Tabelle zulässt. Bestandstabellen baut
        /// <see cref="SpeicherAuslegungStrict"/> um; <c>ALTER TABLE … STRICT</c> gibt es
        /// in SQLite nicht.</para>
        /// </summary>
        public const string SQL_TABELLE = "CREATE TABLE IF NOT EXISTS Tab_SpeicherAuslegung (" +
            "ID INTEGER NOT NULL PRIMARY KEY, ID_Projekt INTEGER NOT NULL, ID_Energieanlage INTEGER, " +
            "Bezeichner TEXT NOT NULL, Daten TEXT NOT NULL, Stand TEXT NOT NULL, " +
            "FOREIGN KEY (ID_Projekt) REFERENCES Tab_Projekt(ID) ON DELETE CASCADE, " +
            "FOREIGN KEY (ID_Energieanlage) REFERENCES Tab_Energieanlagen(ID) ON DELETE CASCADE) STRICT";
        public const string SQL_INDEX = "CREATE UNIQUE INDEX IF NOT EXISTS idx_SpeicherAuslegung " +
            "ON Tab_SpeicherAuslegung(ID_Projekt, COALESCE(ID_Energieanlage,0), Bezeichner)";

        public static void SchemaSicherstellen()
        {
            using var db = DataRepository.Vorgang();
            db.Ausfuehren(SQL_TABELLE);
            db.Ausfuehren(SQL_INDEX);
            db.Commit();
        }

        public static int Anlage(int projektId) =>
            new StromspeicherVarianteCtrl().ReadAktiveVariante(projektId)?.ID_Energieanlage ?? 0;

        public static string Serialisieren(SpeicherOptimierungEingaben eingaben)
        {
            byte[] roh = JsonSerializer.SerializeToUtf8Bytes(eingaben, SpeicherAuslegungKopie.JsonOptionen);
            if (roh.Length > 64 * 1024 * 1024)
                throw new InvalidOperationException("Das Auslegungsprofil überschreitet 64 MiB. Bitte den Umfang der importierten Prognosen oder Projektjahre reduzieren.");
            using var ziel = new MemoryStream();
            using (var gz = new GZipStream(ziel, CompressionLevel.Fastest, true)) gz.Write(roh);
            return "gz1:" + Convert.ToBase64String(ziel.ToArray());
        }

        public static SpeicherOptimierungEingaben Deserialisieren(string daten)
        {
            if (!daten.StartsWith("gz1:", StringComparison.Ordinal))
                throw new FormatException("Unbekannte Version des Auslegungsprofils.");
            using var quelle = new MemoryStream(Convert.FromBase64String(daten.Substring(4)));
            using var gz = new GZipStream(quelle, CompressionMode.Decompress);
            using var ziel = new MemoryStream();
            var puffer = new byte[8192];
            int gelesen;
            while ((gelesen = gz.Read(puffer)) > 0)
            {
                if (ziel.Length + gelesen > 64 * 1024 * 1024)
                    throw new FormatException("Das Auslegungsprofil ist zu groß.");
                ziel.Write(puffer, 0, gelesen);
            }
            return JsonSerializer.Deserialize<SpeicherOptimierungEingaben>(ziel.ToArray(), SpeicherAuslegungKopie.JsonOptionen)
                ?? throw new FormatException("Das Auslegungsprofil ist leer.");
        }

        public static IReadOnlyList<SpeicherAuslegungProfil> Profile(int projektId, int anlageId)
        {
            SchemaSicherstellen();
            using var db = DataRepository.Vorgang();
            DataTable t = db.Lese("SELECT Bezeichner,Daten FROM Tab_SpeicherAuslegung " +
                "WHERE ID_Projekt=? AND COALESCE(ID_Energieanlage,0)=? ORDER BY Bezeichner",
                new DbParam("@p", projektId), new DbParam("@a", anlageId));
            return t.Rows.Cast<DataRow>().Select(r => new SpeicherAuslegungProfil
            {
                Name = Convert.ToString(r["Bezeichner"]),
                Eingaben = Deserialisieren(Convert.ToString(r["Daten"]))
            }).ToArray();
        }

        public static void Speichern(int projektId, int anlageId, string name, SpeicherOptimierungEingaben eingaben)
        {
            if (projektId <= 0) throw new ArgumentException("Kein Projekt geöffnet.");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
                throw new ArgumentException("Ein Profilname mit höchstens 120 Zeichen ist erforderlich.");
            ArgumentNullException.ThrowIfNull(eingaben);
            SchemaSicherstellen();
            string daten = Serialisieren(eingaben);
            string stand = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            using var db = DataRepository.Vorgang();
            object id = db.Skalar("SELECT ID FROM Tab_SpeicherAuslegung WHERE ID_Projekt=? " +
                "AND COALESCE(ID_Energieanlage,0)=? AND Bezeichner=?",
                new DbParam("@p", projektId), new DbParam("@a", anlageId), new DbParam("@n", name));
            if (id != null && id != DBNull.Value)
                db.Ausfuehren("UPDATE Tab_SpeicherAuslegung SET Daten=?,Stand=? WHERE ID=?",
                    new DbParam("@d", daten), new DbParam("@s", stand), new DbParam("@id", Convert.ToInt32(id)));
            else
                db.Ausfuehren("INSERT INTO Tab_SpeicherAuslegung (ID_Projekt,ID_Energieanlage,Bezeichner,Daten,Stand) " +
                    "VALUES (?,?,?,?,?)", new DbParam("@p", projektId), new DbParam("@a", anlageId > 0 ? (object)anlageId : DBNull.Value),
                    new DbParam("@n", name), new DbParam("@d", daten), new DbParam("@s", stand));
            db.Commit();
        }

        public static SpeicherOptimierungVorgaben Vorbelegung(int projektId, double bezugsspitze)
        {
            var v = SpeicherOptimierungCtrl.Vorbelegung(projektId, bezugsspitze);
            int anlage = Anlage(projektId);
            v.Modulkosten = Modulkosten(projektId, anlage);
            v.Strompreisprofile = new KostenprofilCtrl().ReadAllByProjekt(projektId);
            var profile = Profile(projektId, anlage);
            v.Auslegungsprofile = profile.Where(p => p.Name != AktuellerStand &&
                p.Name != SpeicherFlottenProjektCtrl.ProjektflottenStand).ToArray();
            var gespeichert = profile.FirstOrDefault(p => p.Name == AktuellerStand);
            if (gespeichert != null)
            {
                double leistungspreis = v.Eingaben.LeistungspreisEurProKwA;
                v.Eingaben = gespeichert.Eingaben.Kopie();
                v.Eingaben.LeistungspreisEurProKwA = leistungspreis;
            }
            else
            {
                var basis = new StromspeicherSimCtrl().LeseParameter(projektId);
                v.Eingaben.Auslegung = new SpeicherAuslegungKonfiguration
                {
                    DirekteKosten = new SpeicherKostensaetze
                    {
                        InvestEurProKw = basis?.CPowEurProKw ?? 0,
                        InvestEurProKwh = basis?.CCapEurProKwh ?? 0,
                        InvestVorhanden = (basis?.CPowEurProKw ?? 0) > 0 || (basis?.CCapEurProKwh ?? 0) > 0,
                        BetriebVorhanden = false,
                        Herkunft = "Gerätedaten; Betriebskosten im Dialog prüfen"
                    }
                };
            }
            return v;
        }

        public static SpeicherKostensaetze Modulkosten(int projektId, int anlageId)
        {
            var ergebnis = new SpeicherKostensaetze { Herkunft = "Kostendialog der aktiven Speicheranlage" };
            if (anlageId <= 0) return ergebnis;
            using var db = DataRepository.Vorgang();
            DataTable t = db.Lese("SELECT w.ID,w.KategorieID,w.Bemessung,w.Einheitpreis,w.IstErloes,w.Kostenart,w.StartJahr," +
                "k.Bezeichnung FROM Tab_ProjektWerte w LEFT JOIN Tab_Kostenfaktor k ON w.StammID=k.StammID " +
                "WHERE w.ProjektID=? AND w.ID_Anlage=? AND w.KategorieID IN (1,2) ORDER BY w.ID",
                new DbParam("@p", projektId), new DbParam("@a", anlageId));
            foreach (DataRow r in t.Rows)
            {
                string basis = Convert.ToString(r["Bemessung"]);
                string name = Convert.ToString(r["Bezeichnung"]) + " (#" + r["ID"] + ")";
                double? satz = r["Einheitpreis"] == DBNull.Value ? null : Convert.ToDouble(r["Einheitpreis"], CultureInfo.InvariantCulture);
                bool invest = Convert.ToInt32(r["KategorieID"]) == 1;
                bool erloes = r["IstErloes"] != DBNull.Value && Convert.ToBoolean(r["IstErloes"]);
                bool zuschuss = string.Equals(Convert.ToString(r["Kostenart"]), DbWerte.KOSTENART_ZUSCHUSS, StringComparison.OrdinalIgnoreCase);
                bool spaeter = r["StartJahr"] != DBNull.Value && Convert.ToInt32(r["StartJahr"]) > 1;
                bool leistung = basis == DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG || basis == DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH;
                bool kapazitaet = basis == DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET;
                bool entladung = !invest && basis == DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH;
                if (!satz.HasValue || !double.IsFinite(satz.Value) || satz < 0 || erloes || zuschuss || spaeter || !(leistung || kapazitaet || entladung))
                {
                    ergebnis.AusgelassenePositionen.Add(name + ": " + (basis.Length > 0 ? basis : "ohne Bemessung") + " / kein übernehmbarer Kostensatz");
                    continue;
                }
                if (invest)
                {
                    ergebnis.InvestVorhanden = true;
                    if (leistung) ergebnis.InvestEurProKw += satz.Value;
                    else ergebnis.InvestEurProKwh += satz.Value;
                }
                else
                {
                    ergebnis.BetriebVorhanden = true;
                    if (leistung) ergebnis.BetriebEurProKwJahr += satz.Value;
                    else if (kapazitaet) ergebnis.BetriebEurProKwhJahr += satz.Value;
                    else ergebnis.BetriebEurProKwhEntladen += satz.Value;
                }
            }
            return ergebnis;
        }
    }
}
