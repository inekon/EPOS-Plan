using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Warum <see cref="ZapfprofilCtrl.Speichern(int, ZapfprofilStand)"/> einen Arbeitsstand nicht schreibt.</summary>
    internal enum ZapfSpeicherfehler
    {
        /// <summary>Die Tww-Tabellen fehlen in dieser Datenbank (älterer iOS-Seed, 3.2).</summary>
        TabellenFehlen = 1,

        /// <summary>Das Projekt steht nicht in <c>Tab_Projekt</c>.</summary>
        ProjektFehlt = 2,

        /// <summary>Eine Zone verweist auf eine Nutzungsart, die der Katalog nicht führt.</summary>
        NutzungsartFehlt = 3,

        /// <summary>Eine Zone verweist auf einen Tagesgangsatz, den der Katalog nicht führt.</summary>
        TagesgangsatzFehlt = 4,

        /// <summary>Ein Wohnungstyp verweist auf eine Ausstattungsklasse, die der DIN-4708-Katalog nicht führt.</summary>
        AusstattungFehlt = 5,

        /// <summary>Die Projektzeile verweist auf einen Bedarfstag, den der Katalog nicht führt.</summary>
        BedarfstagFehlt = 6,

        /// <summary>Eine Zone verweist auf ein Gebäude, das es nicht gibt.</summary>
        GebaeudeFehlt = 7,

        /// <summary>Eine Zone ist unvollständig oder ungültig (Name, Bezugsmenge, Wertemenge).</summary>
        ZoneUngueltig = 8,

        /// <summary>Eine Zone trägt die Id einer Zone eines ANDEREN Projekts — sie wird nie still umgehängt.</summary>
        ZoneFremd = 9,

        /// <summary>
        /// Ein Wohnungstyp ist ungültig (Anzahl nicht positiv) oder trägt die Id eines Wohnungstyps
        /// einer ANDEREN Zone; eine unbekannte Id legt eine neue Zeile an.
        /// </summary>
        WohnungstypUngueltig = 10,

        /// <summary>Eine Zone verweist auf das Gebäude eines ANDEREN Projekts.</summary>
        GebaeudeFremd = 11,

        /// <summary>Eine Projektgröße liegt außerhalb ihrer Wertemenge (Perzentil, Methode, Speicherart …).</summary>
        ProjektUngueltig = 12
    }

    /// <summary>Die benannte Ablehnung des Schreibwegs: Grund, betroffene Zone (leer = Projekt) und Klartext.</summary>
    internal sealed class ZapfprofilSpeicherException : Exception
    {
        internal ZapfprofilSpeicherException(ZapfSpeicherfehler fehler, string zone, string meldung) : base(meldung)
        {
            Fehler = fehler;
            Zone = zone ?? "";
        }

        internal ZapfSpeicherfehler Fehler { get; }

        internal string Zone { get; }
    }

    /// <summary>
    /// <b>Der Schreibweg des Arbeitsstands</b> (Umsetzungskonzept Zapfprofilgenerator 3.3,
    /// 5.2; Stufe Z1, Gruppe 2).
    ///
    /// <para><b>Ein Vorgang.</b> <c>Tab_TwwProjekt</c>, Zonen und Wohnungstypen entstehen in
    /// EINEM <see cref="DbVorgang"/> — dem eigenen oder dem des Aufrufers (Bedarfsprofil-Dialog:
    /// derselbe Vorgang wie <c>Add_Projekt_Brauchwasser</c>). Eine Ablehnung wirft
    /// <see cref="ZapfprofilSpeicherException"/> vor dem ersten Schreiben; der Aufrufer rollt
    /// seinen Vorgang zurück.</para>
    ///
    /// <para><b>Upsert.</b> Die Projektzeile wird angelegt oder geändert; Zonen und Wohnungstypen
    /// mit bekannter Id des Projekts werden geändert, neue angelegt, und was im Stand fehlt,
    /// wird gelöscht (Wohnungstypen einer gelöschten Zone mit ihr). Die Reihenfolge ist die
    /// Position in der Liste. Ein zweites Speichern desselben Stands legt nichts an und ändert
    /// keine Id.</para>
    ///
    /// <para><b>Die Weiche nur ausdrücklich.</b> <c>Weg</c> kommt allein aus
    /// <see cref="ZapfprofilStand.Weg"/> — nie aus <see cref="ProjektStand.Weg"/> und nie aus
    /// dem Umstand, dass Zonen da sind; er wird bei jedem Speichern geschrieben (3.3). Ohne
    /// <see cref="ZapfprofilStand.Projekt"/> entsteht eine neue Projektzeile mit den Vorgaben der
    /// DDL, eine vorhandene behält ihre Größen.</para>
    ///
    /// <para><b>Katalogverweise.</b> Nutzungsart, Tagesgangsatz, Ausstattungsklasse, Bedarfstag
    /// und Gebäude müssen am Ziel stehen — das Gebäude im selben Projekt —, sonst die benannte
    /// Ablehnung; ebenso eine Projektgröße außerhalb ihrer Wertemenge (N8). Eine Katalogzeile wird
    /// hier nie geschrieben: Was eine Zone benutzt, bleibt über
    /// <see cref="TwwNutzungsartCtrl.IstBenutzt"/> gesperrt (3.2).</para>
    /// </summary>
    internal static partial class ZapfprofilCtrl
    {
        /// <summary>Speichert den Arbeitsstand in einem eigenen Vorgang und liefert ihn mit den Ids der Datenbank zurück.</summary>
        internal static ZapfprofilStand Speichern(int idProjekt, ZapfprofilStand stand)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilStand geschrieben = Speichern(idProjekt, stand, v);
                v.Commit();
                return geschrieben;
            }
        }

        /// <summary>
        /// Speichert den Arbeitsstand im übergebenen Vorgang (kein Commit) und liefert ihn mit
        /// den Ids der Datenbank zurück (neue Zonen und Wohnungstypen tragen ihre neue Id).
        /// </summary>
        internal static ZapfprofilStand Speichern(int idProjekt, ZapfprofilStand stand, DbVorgang v)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (v == null) throw new ArgumentNullException(nameof(v));
            IReadOnlyList<ZonenStand> zonen = stand.Zonen ?? new ZonenStand[0];

            // --- 1. Prüfen, bevor geschrieben wird ------------------------------------------
            foreach (string t in new[] { TwwSchema.TAB_TWW_PROJEKT, TwwSchema.TAB_TWW_ZONE, TwwSchema.TAB_TWW_WOHNUNGSTYP })
                if (Anzahl(v, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", t) == 0)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.TabellenFehlen, "",
                        "Das Zapfprofil kann nicht gespeichert werden — die Tabelle " + t + " fehlt in dieser Datenbank.");
            if (Anzahl(v, "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?", idProjekt) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ProjektFehlt, "",
                    "Das Zapfprofil kann nicht gespeichert werden — das Projekt " + idProjekt + " gibt es nicht.");

            var bekannteZonen = new HashSet<int>();
            DataTable dz = v.Lese("SELECT ID FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Projekt = ?",
                                  new DbParam("@projekt", idProjekt));
            if (dz != null) foreach (DataRow r in dz.Rows) bekannteZonen.Add(Ganz(r, "ID"));

            foreach (ZonenStand z in zonen) ZonePruefen(v, idProjekt, z, bekannteZonen);
            if (stand.Projekt != null) ProjektPruefen(stand.Projekt);
            if (stand.Projekt?.IdBedarfstag != null
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " WHERE ID = ?",
                          stand.Projekt.IdBedarfstag.Value) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.BedarfstagFehlt, "",
                    "Das Zapfprofil kann nicht gespeichert werden — der Bedarfstag " + stand.Projekt.IdBedarfstag.Value
                    + " steht nicht im Katalog.");

            // --- 2. Projektzeile ---------------------------------------------------------------
            string weg = stand.Weg == BrauchwasserWeg.Generator ? TwwSchema.WEG_GENERATOR : TwwSchema.WEG_BESTAND;
            string jetzt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            object vorhanden = v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_PROJEKT + " WHERE ID_Projekt = ?",
                                        new DbParam("@projekt", idProjekt));
            bool zeileDa = vorhanden != null && vorhanden != DBNull.Value;
            int idZeile;
            if (stand.Projekt == null)
            {
                if (zeileDa)
                {
                    idZeile = Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
                    v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_PROJEKT + " SET Weg = ?, Aenderungsdatum = ? WHERE ID = ?",
                                 new DbParam("@weg", weg), new DbParam("@datum", jetzt), new DbParam("@id", idZeile));
                }
                else
                    idZeile = v.EinfuegenUndId(
                        "INSERT INTO " + TwwSchema.TAB_TWW_PROJEKT + " (ID_Projekt, Weg, Aenderungsdatum) VALUES (?, ?, ?)",
                        new[] { new DbParam("@projekt", idProjekt), new DbParam("@weg", weg), new DbParam("@datum", jetzt) });
            }
            else
            {
                List<KeyValuePair<string, object>> werte = ProjektWerte(stand.Projekt, weg, jetzt);
                if (zeileDa)
                {
                    idZeile = Convert.ToInt32(vorhanden, CultureInfo.InvariantCulture);
                    Aendern(v, TwwSchema.TAB_TWW_PROJEKT, werte, idZeile);
                }
                else
                {
                    werte.Insert(0, new KeyValuePair<string, object>("ID_Projekt", idProjekt));
                    idZeile = Anlegen(v, TwwSchema.TAB_TWW_PROJEKT, werte);
                }
            }

            // --- 3. Zonen: entfernen, ändern, anlegen -------------------------------------------
            var behalten = new HashSet<int>(zonen.Where(z => z.Id > 0 && bekannteZonen.Contains(z.Id)).Select(z => z.Id));
            foreach (int alt in bekannteZonen.OrderBy(x => x))
            {
                if (behalten.Contains(alt)) continue;
                v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_WOHNUNGSTYP + " WHERE ID_Zone = ?", new DbParam("@zone", alt));
                v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID = ?", new DbParam("@id", alt));
            }

            var geschrieben = new List<ZonenStand>(zonen.Count);
            for (int i = 0; i < zonen.Count; i++)
            {
                ZonenStand z = zonen[i];
                List<KeyValuePair<string, object>> werte = ZonenWerte(z, i + 1);
                int idZone;
                if (behalten.Contains(z.Id))
                {
                    idZone = z.Id;
                    Aendern(v, TwwSchema.TAB_TWW_ZONE, werte, idZone);
                }
                else
                {
                    werte.Insert(0, new KeyValuePair<string, object>("ID_Projekt", idProjekt));
                    idZone = Anlegen(v, TwwSchema.TAB_TWW_ZONE, werte);
                }
                geschrieben.Add(z with
                {
                    Id = idZone,
                    Reihenfolge = i + 1,
                    Wohnungen = WohnungenSchreiben(v, idZone, z)
                });
            }

            ProjektStand projekt = stand.Projekt == null ? null
                : stand.Projekt with { Id = idZeile, Weg = stand.Weg, Aenderungsdatum = jetzt };
            return new ZapfprofilStand(stand.Weg, geschrieben, projekt);
        }

        // =================================================================================
        // Prüfen
        // =================================================================================

        /// <summary>
        /// Die Projektgrößen gegen ihre Wertemengen — vor dem ersten Schreiben, damit eine
        /// CHECK-Klausel nie als unbenannte Ausnahme mitten im Vorgang greift (N8). Die
        /// Enum-Größen prüft <see cref="Enum.IsDefined(Type, object)"/>, Perzentil und
        /// Realisierungen die Wertemengen der DDL aus <see cref="TwwSchema"/>.
        /// </summary>
        private static void ProjektPruefen(ProjektStand p)
        {
            string grund = null;
            if (!TwwSchema.Perzentile.Contains(p.Perzentil)) grund = "das Perzentil " + p.Perzentil;
            else if (p.Realisierungen < TwwSchema.RealisierungenMindestens) grund = "die Zahl der Realisierungen";
            else if (p.RealisierungenAuslegung.HasValue && p.RealisierungenAuslegung.Value < TwwSchema.RealisierungenMindestens)
                grund = "die Zahl der Realisierungen der Auslegung";
            else if (!Enum.IsDefined(typeof(ZapfZirkulationsmethode), p.ZirkMethode)) grund = "die Methode der Zirkulation";
            else if (p.ZirkLage.HasValue && !Enum.IsDefined(typeof(ZapfLeitungslage), p.ZirkLage.Value))
                grund = "die Lage der Zirkulationsleitung";
            else if (!Enum.IsDefined(typeof(ZapfSpeicherart), p.Speicherart)) grund = "die Speicherart";
            else if (p.BedarfstagQuelle.HasValue && !Enum.IsDefined(typeof(ZapfBedarfstagquelle), p.BedarfstagQuelle.Value))
                grund = "die Quelle des Bedarfstags";
            if (grund != null)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ProjektUngueltig, "",
                    "Das Zapfprofil kann nicht gespeichert werden — " + grund + " liegt außerhalb der Wertemenge.");
        }

        private static void ZonePruefen(DbVorgang v, int idProjekt, ZonenStand z, HashSet<int> bekannteZonen)
        {
            if (z == null)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, "", "Eine Zone ohne Angaben.");
            string name = z.Name ?? "";
            if (name.Trim().Length == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, "",
                    "Das Zapfprofil kann nicht gespeichert werden — eine Zone hat keinen Namen.");
            if (double.IsNaN(z.Bezugsmenge) || double.IsInfinity(z.Bezugsmenge) || z.Bezugsmenge <= 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, name,
                    "Das Zapfprofil kann nicht gespeichert werden — die Zone „" + name + "“ hat keine positive Bezugsmenge.");
            if (!Enum.IsDefined(typeof(ZapfNiveau), z.Niveau) || !Enum.IsDefined(typeof(ZapfTopologie), z.Topologie)
                || (z.JahresmesswertEinheit.HasValue && !Enum.IsDefined(typeof(ZapfMesswerteinheit), z.JahresmesswertEinheit.Value))
                || (z.JahresmesswertBilanzgrenze.HasValue && !Enum.IsDefined(typeof(ZapfBilanzgrenze), z.JahresmesswertBilanzgrenze.Value))
                || !FerienImBereich(z))
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, name,
                    "Das Zapfprofil kann nicht gespeichert werden — die Zone „" + name + "“ trägt einen Wert außerhalb seiner Wertemenge.");

            if (z.Id > 0 && !bekannteZonen.Contains(z.Id)
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID = ?", z.Id) > 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneFremd, name,
                    "Das Zapfprofil kann nicht gespeichert werden — die Zone „" + name + "“ gehört zu einem anderen Projekt.");

            if (Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?", z.IdNutzungsart) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.NutzungsartFehlt, name,
                    "Das Zapfprofil kann nicht gespeichert werden — die Nutzungsart " + z.IdNutzungsart + " der Zone „"
                    + name + "“ steht nicht im Katalog.");
            if (z.IdTagesgangsatz.HasValue
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE ID = ?",
                          z.IdTagesgangsatz.Value) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.TagesgangsatzFehlt, name,
                    "Das Zapfprofil kann nicht gespeichert werden — der Tagesgangsatz " + z.IdTagesgangsatz.Value
                    + " der Zone „" + name + "“ steht nicht im Katalog.");
            if (z.IdGebaeude.HasValue
                && Anzahl(v, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ?", z.IdGebaeude.Value) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.GebaeudeFehlt, name,
                    "Das Zapfprofil kann nicht gespeichert werden — das Gebäude " + z.IdGebaeude.Value + " der Zone „"
                    + name + "“ gibt es nicht.");
            if (z.IdGebaeude.HasValue
                && Anzahl(v, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ? AND ID_Projekt = ?",
                          z.IdGebaeude.Value, idProjekt) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.GebaeudeFremd, name,
                    "Das Zapfprofil kann nicht gespeichert werden — das Gebäude " + z.IdGebaeude.Value + " der Zone „"
                    + name + "“ gehört zu einem anderen Projekt.");

            foreach (WohnungstypStand w in z.Wohnungen ?? new WohnungstypStand[0])
            {
                if (w == null || w.Anzahl <= 0)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.WohnungstypUngueltig, name,
                        "Das Zapfprofil kann nicht gespeichert werden — ein Wohnungstyp der Zone „" + name
                        + "“ hat keine positive Anzahl.");
                // Wie bei den Zonen (ZoneFremd): Die Id eines Wohnungstyps einer ANDEREN Zone wird
                // nie still als neue Zeile angelegt; eine unbekannte Id legt eine neue an.
                if (w.Id > 0)
                {
                    object zoneDesTyps = v.Skalar("SELECT ID_Zone FROM " + TwwSchema.TAB_TWW_WOHNUNGSTYP + " WHERE ID = ?",
                                                  new DbParam("@id", w.Id));
                    if (zoneDesTyps != null && zoneDesTyps != DBNull.Value
                        && Convert.ToInt32(zoneDesTyps, CultureInfo.InvariantCulture) != z.Id)
                        throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.WohnungstypUngueltig, name,
                            "Das Zapfprofil kann nicht gespeichert werden — ein Wohnungstyp der Zone „" + name
                            + "“ gehört zu einer anderen Zone.");
                }
                if (w.IdAusstattung.HasValue
                    && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM + " WHERE ID = ? AND Art = ?",
                              w.IdAusstattung.Value, TwwSchema.DIN4708_ART_AUSSTATTUNG) == 0)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.AusstattungFehlt, name,
                        "Das Zapfprofil kann nicht gespeichert werden — die Ausstattungsklasse " + w.IdAusstattung.Value
                        + " eines Wohnungstyps der Zone „" + name + "“ steht nicht im Katalog.");
            }
        }

        private static bool FerienImBereich(ZonenStand z)
        {
            foreach (int?[] reihe in new[] { z.Ferienbeginn, z.Ferienende })
            {
                if (reihe == null) continue;
                if (reihe.Length > 4) return false;
                foreach (int? t in reihe)
                    if (t.HasValue && (t.Value < FERIEN_KEINE_ANGABE_NULL || t.Value > FERIEN_KEINE_ANGABE_366)) return false;
            }
            return true;
        }

        private static long Anzahl(DbVorgang v, string sql, params object[] werte)
        {
            var p = new DbParam[werte.Length];
            for (int i = 0; i < werte.Length; i++) p[i] = new DbParam("@p" + i, werte[i]);
            object o = v.Skalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        // =================================================================================
        // Wohnungstypen
        // =================================================================================

        private static IReadOnlyList<WohnungstypStand> WohnungenSchreiben(DbVorgang v, int idZone, ZonenStand z)
        {
            IReadOnlyList<WohnungstypStand> liste = z.Wohnungen ?? new WohnungstypStand[0];
            var bekannt = new HashSet<int>();
            DataTable dt = v.Lese("SELECT ID FROM " + TwwSchema.TAB_TWW_WOHNUNGSTYP + " WHERE ID_Zone = ?",
                                  new DbParam("@zone", idZone));
            if (dt != null) foreach (DataRow r in dt.Rows) bekannt.Add(Ganz(r, "ID"));

            var behalten = new HashSet<int>(liste.Where(w => w.Id > 0 && bekannt.Contains(w.Id)).Select(w => w.Id));
            foreach (int alt in bekannt.OrderBy(x => x))
                if (!behalten.Contains(alt))
                    v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_WOHNUNGSTYP + " WHERE ID = ?", new DbParam("@id", alt));

            var geschrieben = new List<WohnungstypStand>(liste.Count);
            for (int i = 0; i < liste.Count; i++)
            {
                WohnungstypStand w = liste[i];
                var werte = new List<KeyValuePair<string, object>>
                {
                    W("Anzahl", w.Anzahl),
                    W("Raumzahl", w.Raumzahl),
                    W("Personen", w.Personen),
                    W("ID_Ausstattung", w.IdAusstattung),
                    W("Reihenfolge", i + 1)
                };
                int id;
                if (behalten.Contains(w.Id))
                {
                    id = w.Id;
                    Aendern(v, TwwSchema.TAB_TWW_WOHNUNGSTYP, werte, id);
                }
                else
                {
                    werte.Insert(0, W("ID_Zone", idZone));
                    id = Anlegen(v, TwwSchema.TAB_TWW_WOHNUNGSTYP, werte);
                }
                geschrieben.Add(w with { Id = id, Reihenfolge = i + 1 });
            }
            return geschrieben;
        }

        // =================================================================================
        // Spaltenwerte — die Namen stehen hier fest, nie aus einer Eingabe
        // =================================================================================

        private static KeyValuePair<string, object> W(string spalte, object wert) => new KeyValuePair<string, object>(spalte, wert);

        private static object Bool(bool b) => b ? 1 : 0;

        private static object Enumwert<T>(T? e) where T : struct, Enum => e.HasValue ? Convert.ToInt32(e.Value) : (object)null;

        private static List<KeyValuePair<string, object>> ZonenWerte(ZonenStand z, int reihenfolge)
        {
            var w = new List<KeyValuePair<string, object>>
            {
                W("ID_Nutzungsart", z.IdNutzungsart),
                W("ID_Tagesgangsatz", z.IdTagesgangsatz),
                W("ID_Gebaeude", z.IdGebaeude),
                W("Reihenfolge", reihenfolge),
                W("Name", z.Name ?? ""),
                W("Bezugsmenge", z.Bezugsmenge),
                W("Niveau", (int)z.Niveau),
                W("Personen_je_WE", z.PersonenJeWe),
                W("Wohnflaeche_je_WE", z.WohnflaecheJeWeM2),
                W("Topologie", (int)z.Topologie),
                W("Zirkulation", Bool(z.Zirkulation))
            };
            for (int i = 0; i < 4; i++)
            {
                string n = (i + 1).ToString(CultureInfo.InvariantCulture);
                w.Add(W("Ferienbeginn_" + n, z.Ferienbeginn != null && i < z.Ferienbeginn.Length ? z.Ferienbeginn[i] : null));
                w.Add(W("Ferienende_" + n, z.Ferienende != null && i < z.Ferienende.Length ? z.Ferienende[i] : null));
            }
            w.Add(W("Jahresmesswert", z.Jahresmesswert));
            w.Add(W("Jahresmesswert_Einheit", Enumwert(z.JahresmesswertEinheit)));
            w.Add(W("Jahresmesswert_Bilanzgrenze", Enumwert(z.JahresmesswertBilanzgrenze)));
            w.Add(W("Jahresmesswert_Quelle", z.JahresmesswertQuelle));
            w.Add(W("Jahresmesswert_Zeitraum", z.JahresmesswertZeitraum));
            w.Add(W("Speicherverlust_Kwh_a", z.SpeicherverlustKwhJeJahr));
            w.Add(W("Tagesbedarf_Auto", Bool(z.TagesbedarfAuto)));
            w.Add(W("Tagesbedarf_Manuell_Kwh", z.TagesbedarfManuellKwh));
            w.Add(W("Bedarf_Spez", z.BedarfSpezKwhJeEinheitTag));
            w.Add(W("Zapftemperatur", z.ZapftemperaturC));
            w.Add(W("Kaltwasser_Mittel", z.KaltwasserMittelC));
            w.Add(W("Kaltwasser_Amplitude", z.KaltwasserAmplitudeK));
            for (int m = 0; m < NutzungsartRaster.MONATE; m++)
                w.Add(W("Auslastung_" + (m + 1).ToString("00", CultureInfo.InvariantCulture),
                        z.Auslastung != null && m < z.Auslastung.Length ? z.Auslastung[m] : null));
            return w;
        }

        private static List<KeyValuePair<string, object>> ProjektWerte(ProjektStand p, string weg, string datum)
        {
            return new List<KeyValuePair<string, object>>
            {
                W("Weg", weg),
                W("Jahresreihe_Stochastisch", Bool(p.JahresreiheStochastisch)),
                W("Seed", p.Seed),
                W("Realisierungen", p.Realisierungen),
                W("Realisierungen_Auslegung", p.RealisierungenAuslegung),
                W("Perzentil", p.Perzentil),
                W("Zirk_Auto", Bool(p.ZirkAuto)),
                W("Zirk_Methode", (int)p.ZirkMethode),
                W("Zirk_Lage", Enumwert(p.ZirkLage)),
                W("Zirk_Laenge_m", p.ZirkLaengeM),
                W("Zirk_Verlust_W_m", p.ZirkVerlustWJeM),
                W("Zirk_Anteil", p.ZirkAnteil),
                W("Zirk_Kennwert", p.ZirkKennwert),
                W("Zirk_Flaeche_m2", p.ZirkFlaecheM2),
                W("Zirk_Laufzeit_h", p.ZirkLaufzeitH),
                W("Zirk_Manuell_Kw", p.ZirkManuellKw),
                W("Leitungsinhalt_l", p.LeitungsinhaltL),
                W("Lade_Auto", Bool(p.LadeAuto)),
                W("Ladefenster_h", p.LadefensterH),
                W("Ladefenster_Beginn_h", p.LadefensterBeginnH),
                W("Lade_Manuell_Kw", p.LadeManuellKw),
                W("Speicher_C", p.SpeicherC),
                W("Kaltwasser_Auslegung_C", p.KaltwasserAuslegungC),
                W("Erzeuger_Kw", p.ErzeugerKw),
                W("Uebertrager_Kw", p.UebertragerKw),
                W("Uebertrager_UA_W_K", p.UebertragerUaWJeK),
                W("Uebertrager_Flaeche_m2", p.UebertragerFlaecheM2),
                W("Speicherart", (int)p.Speicherart),
                W("Sensorhoehe_Anteil", p.SensorhoeheAnteil),
                W("Nachweis_Volumen_l", p.NachweisVolumenL),
                W("Speicherverlust_W", p.SpeicherverlustW),
                W("Nutzanteil", p.Nutzanteil),
                W("Zuschlag", p.Zuschlag),
                W("Bedarfstag_Quelle", Enumwert(p.BedarfstagQuelle)),
                W("ID_Bedarfstag", p.IdBedarfstag),
                W("Auslegung_Volumen_l", p.AuslegungVolumenL),
                W("Auslegung_Leistung_Kw", p.AuslegungLeistungKw),
                W("Aenderungsdatum", datum)
            };
        }

        /// <summary><c>INSERT</c> mit den festen Spaltennamen und <c>?</c>-Parametern; liefert die neue Id.</summary>
        private static int Anlegen(DbVorgang v, string tabelle, List<KeyValuePair<string, object>> werte)
        {
            string sql = "INSERT INTO " + tabelle + " (" + string.Join(", ", werte.Select(w => w.Key)) + ") VALUES (" +
                         string.Join(", ", werte.Select(_ => "?")) + ")";
            return v.EinfuegenUndId(sql, werte.Select((w, i) => new DbParam("@w" + i, w.Value)).ToArray());
        }

        /// <summary><c>UPDATE … WHERE ID = ?</c> mit den festen Spaltennamen und <c>?</c>-Parametern.</summary>
        private static void Aendern(DbVorgang v, string tabelle, List<KeyValuePair<string, object>> werte, int id)
        {
            string sql = "UPDATE " + tabelle + " SET " + string.Join(", ", werte.Select(w => w.Key + " = ?")) + " WHERE ID = ?";
            var p = werte.Select((w, i) => new DbParam("@w" + i, w.Value)).ToList();
            p.Add(new DbParam("@id", id));
            v.Ausfuehren(sql, p.ToArray());
        }
    }
}
