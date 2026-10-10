using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die eingebauten Typkennfelder der Kältemaschinen</b> (KM1, Stufe 1 der Kälterecherche): eine kuratierte
    /// Auswahl aus PNNL Copper <c>chiller_curves.json</c> (BSD-2) als eingebettete Ressource
    /// <c>KaeltemaschinenTypkennfelder.json</c>. Sie decken die Arten ab — Rückkühlung Luft, Trockenkühler,
    /// Nasskühler, Wasser × Verdichter Scroll, Schraube, Turbo, Hubkolben × Leistungsklassen von 20 bis 2 000 kW.
    ///
    /// <para><b>Neutral.</b> Bezeichner „Typkennfeld &lt;Rückkühlung&gt; &lt;Verdichter&gt; &lt;Klasse&gt; kW“, Firma
    /// leer, Typ <see cref="TYP"/>, die Beschreibung nennt Quelle, Lizenz und Datensatznummer — kein Hersteller,
    /// keine Produktbezeichnung (die Quelle führt keine). Das Kennfeld entsteht zur Laufzeit aus den Kurven
    /// (<see cref="KaeltemaschinenKennfeld.Modell"/>) und ist auf die Klasse am Eurovent-Nennpunkt skaliert.</para>
    ///
    /// <para><b>Einspielen.</b> <see cref="Einspielen"/> schreibt die Sätze als Auslieferungssätze
    /// (<c>ReadOnly = 1</c>, Katalogschlüssel aus dem Bezeichner, Prüfsumme über
    /// <see cref="KatalogSchluesselSaat"/>) — idempotent über den Katalogschlüssel; ein Satz, dessen Schlüssel
    /// oder Bezeichner schon steht, wird übersprungen und nie überschrieben.</para>
    /// </summary>
    public static class KaeltemaschinenTypkennfelder
    {
        /// <summary>Der Typ der Typkennfelder im Katalog.</summary>
        public const string TYP = KaeltemaschinenKennfeld.TYP;

        /// <summary>Der feste Name der eingebetteten Ressource.</summary>
        public const string RESSOURCE = "EPOS.Kern.Katalog.KaeltemaschinenTypkennfelder.json";

        /// <summary>Ein eingebautes Typkennfeld: Bezeichner, Rückkühlart, Klasse, Quellnummer und der Kurvensatz.</summary>
        public sealed record Typkennfeld(string Bezeichner, string Rueckkuehlart, double KlasseKw, string QuellNr,
                                         KaeltemaschinenKurvensatz Kurven)
        {
            /// <summary>Der Katalogsatz samt Kennfeld.</summary>
            public KaeltemaschineModel Modell() => KaeltemaschinenKennfeld.Modell(
                Kurven, Bezeichner, Rueckkuehlart, KlasseKw, TYP,
                "Typkennfeld aus offenen US-Kurvendaten (PNNL Copper, BSD-2), Datensatz " + QuellNr + "; Nennpunkt Eurovent");
        }

        /// <summary>Das Ergebnis von <see cref="Einspielen"/>.</summary>
        public sealed record Einspielergebnis(bool Ok, int Neu, int Uebersprungen, string Fehler);

        private static IReadOnlyList<Typkennfeld> _gelesen;

        /// <summary>Die eingebauten Typkennfelder in der Folge der Ressource.</summary>
        public static IReadOnlyList<Typkennfeld> Lesen()
        {
            if (_gelesen != null) return _gelesen;
            using Stream s = typeof(KaeltemaschinenTypkennfelder).Assembly.GetManifestResourceStream(RESSOURCE)
                ?? throw new InvalidOperationException("Ressource " + RESSOURCE + " fehlt.");
            using var r = new StreamReader(s);
            _gelesen = AusText(r.ReadToEnd());
            return _gelesen;
        }

        /// <summary>Liest die Typkennfelder aus dem Text der Ressource (für Tests auch direkt).</summary>
        public static IReadOnlyList<Typkennfeld> AusText(string json)
        {
            var liste = new List<Typkennfeld>();
            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (JsonElement e in doc.RootElement.GetProperty("saetze").EnumerateArray())
            {
                string nr = e.GetProperty("copper_nr").GetString();
                KaeltemaschinenKurvensatz k = CopperKaltwassersatzLeser.SatzLesen(nr, e.GetProperty("copper"), out string grund)
                    ?? throw new FormatException("Typkennfeld " + nr + ": " + grund);
                liste.Add(new Typkennfeld(e.GetProperty("bezeichner").GetString(), e.GetProperty("rueckkuehlart").GetString(),
                                          e.GetProperty("klasse_kw").GetDouble(), nr, k));
            }
            return liste;
        }

        /// <summary>Der Katalogschlüssel eines Typkennfelds (Kürzel „KM“ der Katalogfassung).</summary>
        public static string Schluessel(string bezeichner) =>
            Katalogfassung.Schluesselstamm(Katalogfassung.Tabelle(KaeltemaschineSchema.TAB_STAMM), bezeichner);

        /// <summary>
        /// Schreibt die fehlenden Typkennfelder in <c>Tab_Kaeltemaschine_STAMM</c> samt Kennlinie — über
        /// <paramref name="db"/> (Tests und der Schemaschritt geben den Zugriff vor; <c>null</c> = der laufende).
        /// Wiederholbar: Ein Satz, dessen Katalogschlüssel oder Bezeichner schon steht, zählt als übersprungen.
        /// </summary>
        public static Einspielergebnis Einspielen(IDatenzugriff db = null)
        {
            IDatenzugriff vorher = null;
            bool getauscht = db != null && !ReferenceEquals(db, DataRepository.Zugriff);
            if (getauscht) { vorher = DataRepository.Zugriff; DataRepository.Zugriff = db; }
            try
            {
                return EinspielenIntern();
            }
            finally
            {
                if (getauscht) DataRepository.Zugriff = vorher;
            }
        }

        /// <summary>Das Ergebnis von <see cref="ErgaenzenMitZaehlung"/>: gefüllte und übersprungene Typkennfelder.</summary>
        public readonly record struct Ergaenzungsergebnis(int Gefuellt, int Uebersprungen);

        /// <summary>
        /// <b>Ergänzt die schon eingespielten Typkennfelder</b> um die Spalten von Teillast und Takten
        /// (<see cref="KaeltemaschineTeillastSchema"/>, Fachkonzept 4.3 und 6): Weg, Kurve, untere Gültigkeit und
        /// Verdichterregelung aus dem eingebetteten Copper-Satz (<see cref="Typkennfeld.Modell"/>), je Feld nur wo leer;
        /// die Prüfsumme der ergänzten Sätze wird neu gebildet. Nur ausgelieferte Sätze (<c>ReadOnly = 1</c>) mit dem
        /// Schlüssel des Typkennfelds; Projektkopien und eigene Sätze bleiben unberührt. Wiederholbar: ein zweiter Lauf
        /// findet nichts zu tun.
        /// </summary>
        /// <returns>Die Zahl der ergänzten Katalogsätze.</returns>
        public static int Ergaenzen() => ErgaenzenMitZaehlung().Gefuellt;

        /// <summary>
        /// Wie <see cref="Ergaenzen"/>, mit Zählung: gefüllt = Sätze mit mindestens einem ergänzten Feld; übersprungen =
        /// Typkennfelder ohne ausgelieferten Satz in der Datenbank oder ohne leeres Feld, das der Copper-Satz füllt.
        /// Ohne die Spalten des Schritts <see cref="KaeltemaschineTeillastSchema.SCHRITT"/> (0, 0).
        /// </summary>
        public static Ergaenzungsergebnis ErgaenzenMitZaehlung()
        {
            string tab = KaeltemaschineSchema.TAB_STAMM;
            if (!DataRepository.TabelleVorhanden(tab) || !Katalogfassung.SpaltenVorhanden(tab) ||
                !KaeltemaschineTeillastSchema.EingabespaltenVorhanden(tab))
                return new Ergaenzungsergebnis(0, 0);
            IReadOnlyList<string> spalten = KaeltemaschineTeillastSchema.EINGABESPALTEN;
            string liste = string.Join(", ", spalten.Select(s => "\"" + s + "\""));
            int gefuellt = 0, ueber = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (Typkennfeld t in Lesen())
                    {
                        DataTable dt = v.Lese("SELECT ID, " + liste + " FROM " + tab + " WHERE " + Katalogfassung.SPALTE_SCHLUESSEL +
                                              " = ? AND ReadOnly = 1", new DbParam("?", Schluessel(t.Bezeichner)));
                        if (dt == null || dt.Rows.Count == 0) { ueber++; continue; }
                        DataRow r = dt.Rows[0];
                        object[] soll = Felder(t.Modell());
                        var setzen = new List<string>();
                        var werte = new List<DbParam>();
                        for (int i = 0; i < spalten.Count; i++)
                        {
                            if (soll[i] == null || r[spalten[i]] != DBNull.Value) continue;
                            setzen.Add("\"" + spalten[i] + "\" = ?");
                            werte.Add(new DbParam("?", soll[i]));
                        }
                        if (setzen.Count == 0) { ueber++; continue; }
                        // Die Pruefsumme leeren: KatalogSchluesselSaat bildet sie danach ueber die ergaenzte Zeile neu.
                        werte.Add(new DbParam("?", Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture)));
                        v.Ausfuehren("UPDATE " + tab + " SET " + string.Join(", ", setzen) + ", \"" +
                                     Katalogfassung.SPALTE_PRUEFSUMME + "\" = NULL WHERE ID = ?", werte.ToArray());
                        gefuellt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            if (gefuellt > 0) KatalogSchluesselSaat.Ausfuehren(null, Katalogfassung.Stufe3);
            return new Ergaenzungsergebnis(gefuellt, ueber);
        }

        /// <summary>Die acht Felder von Teillast und Takten in der Folge von <see cref="KaeltemaschineTeillastSchema.EINGABESPALTEN"/>.</summary>
        private static object[] Felder(KaeltemaschineModel m) => new object[]
        {
            m.Teillast_Weg, m.Teillastkurve_a, m.Teillastkurve_b, m.Teillastkurve_c, m.Teillastkurve_Lastgrad_Min,
            m.Taktverlustfaktor_Cd, m.Verdichterregelung, m.Kennfeld_Randweg
        };

        private static Einspielergebnis EinspielenIntern()
        {
            string tab = KaeltemaschineSchema.TAB_STAMM;
            if (!DataRepository.TabelleVorhanden(tab) || !Katalogfassung.SpaltenVorhanden(tab))
                return new Einspielergebnis(false, 0, 0, MyResource.Resource.KM_MSG_TYPKENNFELDER_SCHEMA);
            int neu = 0, ueber = 0;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        foreach (Typkennfeld t in Lesen())
                        {
                            string schluessel = Schluessel(t.Bezeichner);
                            object da = v.Skalar(
                                "SELECT COUNT(*) FROM " + tab + " WHERE " + Katalogfassung.SPALTE_SCHLUESSEL + " = ? OR Bezeichner = ?",
                                new DbParam("?", schluessel), new DbParam("?", t.Bezeichner));
                            if (da != null && da != DBNull.Value && Convert.ToInt32(da, CultureInfo.InvariantCulture) > 0)
                            {
                                ueber++;
                                continue;
                            }
                            KaeltemaschineModel m = t.Modell();
                            if (KaeltemaschineStammCtrl.Pruefen(m) != null) { ueber++; continue; }
                            int id = KaeltemaschineStammCtrl.KopfSchreiben(v, tab, m, true, null);
                            KaeltemaschineStammCtrl.KennlinieSchreiben(v, KaeltemaschineSchema.TAB_KENNDATEN_STAMM, id, m.Kennlinie, null);
                            v.Ausfuehren("UPDATE " + tab + " SET ReadOnly = 1, " + Katalogfassung.SPALTE_SCHLUESSEL + " = ? WHERE ID = ?",
                                         new DbParam("?", schluessel), new DbParam("?", id));
                            v.Ausfuehren("UPDATE " + KaeltemaschineSchema.TAB_KENNDATEN_STAMM + " SET ReadOnly = 1 WHERE " +
                                         KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", id));
                            neu++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                // Die Prüfsumme der neuen Auslieferungssätze (Schlüssel steht schon) - derselbe Weg wie die Saat.
                if (neu > 0) KatalogSchluesselSaat.Ausfuehren(null, Katalogfassung.Stufe3);
                return new Einspielergebnis(true, neu, ueber, "");
            }
            catch (Exception ex)
            {
                return new Einspielergebnis(false, 0, 0, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message));
            }
        }
    }
}
