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
        ProjektUngueltig = 12,

        /// <summary>Der konstruierte Bedarfstag ist ungültig (ohne Namen, ohne Ereignis, Ereignis außerhalb des Tages).</summary>
        BedarfstagUngueltig = 13,

        /// <summary>Der Name des konstruierten Bedarfstags ist in seiner Katalogversion schon vergeben.</summary>
        BedarfstagNameBelegt = 14
    }

    /// <summary>
    /// Die benannte Ablehnung des Schreibwegs: Grund, betroffene Zone (leer = Projekt) und der Grund als
    /// Satz (Kennung und Werte, N11 (k)); die Meldung ist der deutsche Wortlaut „Das Zapfprofil kann nicht
    /// gespeichert werden — …".
    /// </summary>
    internal sealed class ZapfprofilSpeicherException : Exception
    {
        internal ZapfprofilSpeicherException(ZapfSpeicherfehler fehler, string zone, ZapfSatz grund)
            : base(ZapfSatz.Neu("SPEICHER_NICHT_GESPEICHERT", grund).Klartext)
        {
            Fehler = fehler;
            Zone = zone ?? "";
            Grund = grund;
        }

        internal ZapfSpeicherfehler Fehler { get; }

        internal string Zone { get; }

        /// <summary>Der Grund als Satz (ohne den Vorsatz „Das Zapfprofil kann nicht gespeichert werden —").</summary>
        internal ZapfSatz Grund { get; }

        /// <summary>Der ganze Satz: „Das Zapfprofil kann nicht gespeichert werden —" samt <see cref="Grund"/>.</summary>
        internal ZapfSatz Satz => ZapfSatz.Neu("SPEICHER_NICHT_GESPEICHERT", Grund);
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
    /// <para><b>Veraltet.</b> Jedes erfolgreiche Speichern setzt im selben Vorgang
    /// <c>Tab_Projekt.Aenderungsdatum</c> (<see cref="MerkmalUebernahmeCtrl.MarkiereProjektGeaendert"/>):
    /// Ein vorhandenes Simulationsergebnis gilt damit als veraltet. Eine Ablehnung schreibt nichts,
    /// ein Rollback des Aufrufers nimmt die Marke mit zurück.</para>
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
                        ZapfSatz.Neu("SPEICHER_TABELLE_FEHLT", ZapfSatz.Tabelle(t)));
            if (Anzahl(v, "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?", idProjekt) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ProjektFehlt, "",
                    ZapfSatz.Neu("SPEICHER_PROJEKT_FEHLT", idProjekt));

            var bekannteZonen = new HashSet<int>();
            DataTable dz = v.Lese("SELECT ID FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Projekt = ?",
                                  new DbParam("@projekt", idProjekt));
            if (dz != null) foreach (DataRow r in dz.Rows) bekannteZonen.Add(Ganz(r, "ID"));

            foreach (ZonenStand z in zonen) ZonePruefen(v, idProjekt, z, bekannteZonen);

            // Ein konstruierter Bedarfstag (Z2, Gruppe 2) entsteht im selben Vorgang als
            // Katalogzeile; die Projektzeile zeigt danach auf ihn (Quelle Konstruktor).
            BedarfstagKatalogzeile entwurf = stand.BedarfstagEntwurf;
            ProjektStand projektZeile = stand.Projekt;
            if (entwurf != null)
            {
                EntwurfPruefen(v, entwurf);
                projektZeile = (projektZeile ?? ProjektVorgabe(v))
                               with { BedarfstagQuelle = ZapfBedarfstagquelle.Konstruktor, IdBedarfstag = null };
            }
            if (projektZeile != null) ProjektPruefen(projektZeile);
            if (projektZeile?.IdBedarfstag != null
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " WHERE ID = ?",
                          projektZeile.IdBedarfstag.Value) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.BedarfstagFehlt, "",
                    ZapfSatz.Neu("SPEICHER_BEDARFSTAG_FEHLT", projektZeile.IdBedarfstag.Value));
            if (entwurf != null) projektZeile = projektZeile with { IdBedarfstag = BedarfstagAnlegen(v, entwurf) };

            // --- 2. Projektzeile ---------------------------------------------------------------
            string weg = stand.Weg == BrauchwasserWeg.Generator ? TwwSchema.WEG_GENERATOR : TwwSchema.WEG_BESTAND;
            string jetzt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            object vorhanden = v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_PROJEKT + " WHERE ID_Projekt = ?",
                                        new DbParam("@projekt", idProjekt));
            bool zeileDa = vorhanden != null && vorhanden != DBNull.Value;
            int idZeile;
            if (projektZeile == null)
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
                List<KeyValuePair<string, object>> werte = ProjektWerte(projektZeile, weg, jetzt);
                // Schritt 124 (T3): die Laufangaben der Auslegung — vor dem Schritt fehlen die Spalten.
                // Eine gesetzte Angabe lehnt der Schreibweg dann benannt ab, statt sie still fallen zu lassen.
                if (SpalteImVorgang(v, TwwSchema.TAB_TWW_PROJEKT, TwwSchema.SPALTE_PERSONEN_AUTO))
                    werte.AddRange(LaufangabenWerte(projektZeile));
                else if (projektZeile.Erzeugerart.HasValue || projektZeile.UebertragerWerkstoff.HasValue
                         || !projektZeile.PersonenAuto || projektZeile.PersonenManuell.HasValue
                         || projektZeile.FuellstandBezug.HasValue)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.TabellenFehlen, "",
                        ZapfSatz.Neu("SPEICHER_SPALTE_FEHLT", TwwSchema.TAB_TWW_PROJEKT + "." + TwwSchema.SPALTE_PERSONEN_AUTO));
                // Schritt 131 (T3 „Typtage"): die Wahl des Typtagwegs - vor dem Schritt fehlen die
                // Spalten. Eine gesetzte Wahl lehnt der Schreibweg dann benannt ab, statt sie still
                // fallen zu lassen; OHNE Wahl laeuft das Speichern durch wie vor dem Schritt.
                if (SpalteImVorgang(v, TwwSchema.TAB_TWW_PROJEKT, TwwSchema.SPALTE_TYPTAGE_AKTIV))
                    werte.AddRange(TyptagwahlWerte(projektZeile));
                else if (projektZeile.TyptageAktiv || projektZeile.TyptageKlimazone.HasValue
                         || !string.IsNullOrWhiteSpace(projektZeile.TyptageGebaeudeart))
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.TabellenFehlen, "",
                        ZapfSatz.Neu("SPEICHER_SPALTE_FEHLT", TwwSchema.TAB_TWW_PROJEKT + "." + TwwSchema.SPALTE_TYPTAGE_AKTIV));
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

            // --- 3b. Die Zeilen des Konstruktors (Schritt 145, ZU25) -----------------------------
            KonstruktorzeilenSchreiben(v, idZeile, stand.Konstruktorzeilen);

            // --- 4. Das Projekt als geändert markieren -------------------------------------------
            // Zonen, Projektgrößen und die Weiche sind Eingangsgrößen der Simulation: Ein
            // gespeichertes Ergebnis ist ab hier veraltet (Tab_Projekt.Aenderungsdatum, derselbe
            // Mechanismus wie in den übrigen Schreibwegen). Die Marke läuft über die
            // Vorgangsklammer im SELBEN Vorgang — rollt der Aufrufer zurück, fällt sie mit.
            using (Vorgangsklammer.Setzen(v))
                MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);

            ProjektStand projekt = projektZeile == null ? null
                : projektZeile with { Id = idZeile, Weg = stand.Weg, Aenderungsdatum = jetzt };
            // Ohne Entwurf: Er ist jetzt Katalogzeile. Die Zeilen des Konstruktors bleiben am Stand —
            // sie stehen ab hier in der Datenbank und tragen das erneute Öffnen des Konstruktors.
            // Ebenso der Bezug des gespeicherten Tags (Folge (a) aus N21): beim Entwurf der seine,
            // sonst der der Katalogzeile — der Dialog arbeitet mit diesem Stand weiter.
            KonstruktorBezugStand bezug = entwurf != null
                ? new KonstruktorBezugStand(true, entwurf.Bezugsmenge, entwurf.Bezugsart)
                : projektZeile?.IdBedarfstag != null ? KonstruktorBezug(projektZeile, v.Lese) : null;
            return new ZapfprofilStand(stand.Weg, geschrieben, projekt)
            {
                Konstruktorzeilen = stand.Konstruktorzeilen ?? new KonstruktorzeileStand[0],
                KonstruktorBezug = bezug
            };
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
            ZapfSatz grund = null;
            if (!TwwSchema.Perzentile.Contains(p.Perzentil)) grund = ZapfSatz.Neu("BEGRIFF_PERZENTIL_WERT", p.Perzentil);
            else if (p.Realisierungen < TwwSchema.RealisierungenMindestens) grund = ZapfSatz.Neu("BEGRIFF_REALISIERUNGEN");
            else if (p.RealisierungenAuslegung.HasValue && p.RealisierungenAuslegung.Value < TwwSchema.RealisierungenMindestens)
                grund = ZapfSatz.Neu("BEGRIFF_REALISIERUNGEN_AUSLEGUNG");
            else if (!Enum.IsDefined(typeof(ZapfZirkulationsmethode), p.ZirkMethode)) grund = ZapfSatz.Neu("BEGRIFF_ZIRK_METHODE");
            else if (p.ZirkLage.HasValue && !Enum.IsDefined(typeof(ZapfLeitungslage), p.ZirkLage.Value))
                grund = ZapfSatz.Neu("BEGRIFF_ZIRK_LAGE");
            else if (!Enum.IsDefined(typeof(ZapfSpeicherart), p.Speicherart)) grund = ZapfSatz.Neu("BEGRIFF_SPEICHERART");
            else if (p.BedarfstagQuelle.HasValue && !Enum.IsDefined(typeof(ZapfBedarfstagquelle), p.BedarfstagQuelle.Value))
                grund = ZapfSatz.Neu("BEGRIFF_BEDARFSTAG_QUELLE");
            // Schritt 124: die Wertemengen der DDL (TwwSchema, EINE Quelle).
            else if (p.Erzeugerart.HasValue && !TwwSchema.Werte(TwwSchema.ERZEUGERART_WERTE).Contains((int)p.Erzeugerart.Value))
                grund = ZapfSatz.Neu("BEGRIFF_ERZEUGERART");
            else if (p.UebertragerWerkstoff.HasValue
                     && !TwwSchema.Werte(TwwSchema.WERKSTOFF_WERTE).Contains((int)p.UebertragerWerkstoff.Value))
                grund = ZapfSatz.Neu("BEGRIFF_WERKSTOFF");
            else if (p.FuellstandBezug.HasValue && !TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE).Contains((int)p.FuellstandBezug.Value))
                grund = ZapfSatz.Neu("BEGRIFF_FUELLSTAND_BEZUG");
            else if (p.PersonenManuell.HasValue && (double.IsNaN(p.PersonenManuell.Value) || double.IsInfinity(p.PersonenManuell.Value)
                                                    || p.PersonenManuell.Value < 0))
                grund = ZapfSatz.Neu("BEGRIFF_PERSONEN");
            // Schritt 131: die Wahl des Typtagwegs - die Klimazone ist eine Nummer des Pakets > 0
            // (Wertemenge der DDL, TwwSchema.SpaltenT3Typtage).
            else if (p.TyptageKlimazone.HasValue && p.TyptageKlimazone.Value <= 0)
                grund = ZapfSatz.Neu("BEGRIFF_TYPTAGE_KLIMAZONE");
            if (grund != null)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ProjektUngueltig, "",
                    ZapfSatz.Neu("SPEICHER_WERTEMENGE", grund));
        }

        private static void ZonePruefen(DbVorgang v, int idProjekt, ZonenStand z, HashSet<int> bekannteZonen)
        {
            if (z == null)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, "", ZapfSatz.Neu("EINGABE_ZONE_OHNE_ANGABEN"));
            string name = z.Name ?? "";
            if (name.Trim().Length == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, "", ZapfSatz.Neu("SPEICHER_ZONE_OHNE_NAME"));
            if (double.IsNaN(z.Bezugsmenge) || double.IsInfinity(z.Bezugsmenge) || z.Bezugsmenge <= 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, name,
                    ZapfSatz.Neu("SPEICHER_ZONE_BEZUGSMENGE", name));
            if (!Enum.IsDefined(typeof(ZapfNiveau), z.Niveau) || !Enum.IsDefined(typeof(ZapfTopologie), z.Topologie)
                || (z.JahresmesswertEinheit.HasValue && !Enum.IsDefined(typeof(ZapfMesswerteinheit), z.JahresmesswertEinheit.Value))
                || (z.JahresmesswertBilanzgrenze.HasValue && !Enum.IsDefined(typeof(ZapfBilanzgrenze), z.JahresmesswertBilanzgrenze.Value))
                || !FerienImBereich(z))
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneUngueltig, name,
                    ZapfSatz.Neu("SPEICHER_ZONE_WERTEMENGE", name));

            if (z.Id > 0 && !bekannteZonen.Contains(z.Id)
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID = ?", z.Id) > 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneFremd, name,
                    ZapfSatz.Neu("SPEICHER_ZONE_FREMD", name));

            if (Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?", z.IdNutzungsart) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.NutzungsartFehlt, name,
                    ZapfSatz.Neu("SPEICHER_NUTZUNGSART_FEHLT", name));
            if (z.IdTagesgangsatz.HasValue
                && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE ID = ?",
                          z.IdTagesgangsatz.Value) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.TagesgangsatzFehlt, name,
                    ZapfSatz.Neu("SPEICHER_TAGESGANGSATZ_FEHLT", z.IdTagesgangsatz.Value, name));
            if (z.IdGebaeude.HasValue
                && Anzahl(v, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ?", z.IdGebaeude.Value) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.GebaeudeFehlt, name,
                    ZapfSatz.Neu("SPEICHER_GEBAEUDE_FEHLT", z.IdGebaeude.Value, name));
            if (z.IdGebaeude.HasValue
                && Anzahl(v, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ? AND ID_Projekt = ?",
                          z.IdGebaeude.Value, idProjekt) == 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.GebaeudeFremd, name,
                    ZapfSatz.Neu("SPEICHER_GEBAEUDE_FREMD", z.IdGebaeude.Value, name));

            // Außerhalb der Bezugsart Wohneinheiten/Personen (Mengengeruest.WohnungstabelleWirksam) wird
            // die Wohnungstabelle weder gerechnet noch geprüft — eine verdeckte Zeile bleibt ungeprüft
            // und wird gar nicht erst geschrieben (WohnungenSchreiben verwirft sie ebenso, Z4, Gruppe
            // 2a Punkt 6); nur das CHECK der Datenbank bliebe sonst die einzige, unbenannte Ablehnung.
            if (!WohnungstabelleWirksamFuerNutzungsart(v, z.IdNutzungsart)) return;
            foreach (WohnungstypStand w in z.Wohnungen ?? new WohnungstypStand[0])
            {
                if (w == null || w.Anzahl <= 0)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.WohnungstypUngueltig, name,
                        ZapfSatz.Neu("SPEICHER_WOHNUNGSTYP_ANZAHL", name));
                // Wie bei den Zonen (ZoneFremd): Die Id eines Wohnungstyps einer ANDEREN Zone wird
                // nie still als neue Zeile angelegt; eine unbekannte Id legt eine neue an.
                if (w.Id > 0)
                {
                    object zoneDesTyps = v.Skalar("SELECT ID_Zone FROM " + TwwSchema.TAB_TWW_WOHNUNGSTYP + " WHERE ID = ?",
                                                  new DbParam("@id", w.Id));
                    if (zoneDesTyps != null && zoneDesTyps != DBNull.Value
                        && Convert.ToInt32(zoneDesTyps, CultureInfo.InvariantCulture) != z.Id)
                        throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.WohnungstypUngueltig, name,
                            ZapfSatz.Neu("SPEICHER_WOHNUNGSTYP_FREMD", name));
                }
                if (w.IdAusstattung.HasValue
                    && Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM + " WHERE ID = ? AND Art = ?",
                              w.IdAusstattung.Value, TwwSchema.DIN4708_ART_AUSSTATTUNG) == 0)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.AusstattungFehlt, name,
                        ZapfSatz.Neu("SPEICHER_AUSSTATTUNG_FEHLT", w.IdAusstattung.Value, name));
            }
        }

        /// <summary>
        /// Ist die Wohnungstabelle für die Nutzungsart <paramref name="idNutzungsart"/> wirksam
        /// (<see cref="Mengengeruest.WohnungstabelleWirksam(ZapfBezugsart)"/>) — im laufenden Vorgang
        /// gelesen, damit Prüfung und Schreiben denselben Stand sehen. <c>false</c> ohne Zeile.
        /// </summary>
        private static bool WohnungstabelleWirksamFuerNutzungsart(DbVorgang v, int idNutzungsart)
        {
            object roh = v.Skalar("SELECT Bezugsart FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                                  new DbParam("@id", idNutzungsart));
            return roh != null && roh != DBNull.Value
                && Mengengeruest.WohnungstabelleWirksam((ZapfBezugsart)Convert.ToInt32(roh, CultureInfo.InvariantCulture));
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

        /// <summary>Führt die Tabelle die Spalte? Im laufenden Vorgang gefragt (Stand vor oder nach einem Schemaschritt).</summary>
        internal static bool SpalteImVorgang(DbVorgang v, string tabelle, string spalte)
            => Anzahl(v, "SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = ?", tabelle, spalte) > 0;

        /// <summary>
        /// <b>Die Zeilen des Konstruktors am Auslegungssatz</b> (<c>Tab_TwwKonstruktorzeile</c>,
        /// Schemaschritt T5, Anwenderentscheid ZU25) — <b>ersetzend</b>: erst weg, was am Satz
        /// steht, dann die Zeilen des Stands in ihrer Reihenfolge (1 … n). Ein leerer Stand löscht
        /// also, was da war; das ist gewollt, denn der Konstruktor gibt seine Zeilen immer
        /// geschlossen her.
        ///
        /// <para>Vor dem Schritt fehlt die Tabelle: Gegebene Zeilen lehnt der Schreibweg dann
        /// benannt ab, statt sie still fallen zu lassen; OHNE Zeilen läuft das Speichern durch wie
        /// vor dem Schritt.</para>
        /// </summary>
        private static void KonstruktorzeilenSchreiben(DbVorgang v, int idAuslegung,
                                                       IReadOnlyList<KonstruktorzeileStand> zeilen)
        {
            if (Anzahl(v, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                       TwwSchema.TAB_TWW_KONSTRUKTORZEILE) == 0)
            {
                if (zeilen != null && zeilen.Count > 0)
                    throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.TabellenFehlen, "",
                        ZapfSatz.Neu("SPEICHER_TABELLE_FEHLT", ZapfSatz.Tabelle(TwwSchema.TAB_TWW_KONSTRUKTORZEILE)));
                return;
            }

            v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_KONSTRUKTORZEILE + " WHERE ID_TwwProjekt = ?",
                         new DbParam("@auslegung", idAuslegung));
            if (zeilen == null) return;
            int reihenfolge = 0;
            foreach (KonstruktorzeileStand z in zeilen)
            {
                if (z == null) continue;
                v.Ausfuehren(
                    "INSERT INTO " + TwwSchema.TAB_TWW_KONSTRUKTORZEILE +
                    " (ID_TwwProjekt, Reihenfolge, Beginn_h, Ende_h, Regel, Anzahl, Volumen_l, " +
                    "Zapftemperatur_C, Verbraucher) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    new DbParam("@auslegung", idAuslegung),
                    new DbParam("@reihenfolge", ++reihenfolge),
                    new DbParam("@beginn", AlsWert(z.BeginnH)),
                    new DbParam("@ende", AlsWert(z.EndeH)),
                    new DbParam("@regel", z.Regel ?? ""),
                    new DbParam("@anzahl", AlsWert(z.Anzahl)),
                    new DbParam("@volumen", AlsWert(z.VolumenL)),
                    new DbParam("@temperatur", AlsWert(z.ZapftemperaturC)),
                    new DbParam("@verbraucher", z.Verbraucher ?? ""));
            }
        }

        /// <summary>Eine nullbare Zahl als Parameterwert; <c>null</c> bleibt <c>NULL</c>.</summary>
        private static object AlsWert(double? wert) => wert.HasValue ? (object)wert.Value : null;

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
            // Außerhalb der wirksamen Bezugsart (ZonePruefen) wird nichts geschrieben — eine
            // verdeckte Wohnungstabelle verschwindet dann mit dem nächsten Speichern; ihre Werte
            // wären ohnehin nie gerechnet oder geprüft worden (Z4, Gruppe 2a Punkt 6).
            IReadOnlyList<WohnungstypStand> liste = WohnungstabelleWirksamFuerNutzungsart(v, z.IdNutzungsart)
                ? (z.Wohnungen ?? new WohnungstypStand[0]) : new WohnungstypStand[0];
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

        /// <summary>Die Laufangaben der Auslegung (Schritt 124, <see cref="TwwSchema.SpaltenT3"/>) in ihren Spalten.</summary>
        private static IEnumerable<KeyValuePair<string, object>> LaufangabenWerte(ProjektStand p)
        {
            yield return W(TwwSchema.SPALTE_ERZEUGERART, Enumwert(p.Erzeugerart));
            yield return W(TwwSchema.SPALTE_UEBERTRAGER_WERKSTOFF, Enumwert(p.UebertragerWerkstoff));
            yield return W(TwwSchema.SPALTE_PERSONEN_AUTO, Bool(p.PersonenAuto));
            yield return W(TwwSchema.SPALTE_PERSONEN_MANUELL, p.PersonenManuell);
            yield return W(TwwSchema.SPALTE_FUELLSTAND_BEZUG, Enumwert(p.FuellstandBezug));
        }

        /// <summary>
        /// Die Wahl des Typtagwegs (Schritt 131, <see cref="TwwSchema.SpaltenT3Typtage"/>) in ihren
        /// Spalten: gespeichert wird die WAHL, nie ein Wert der Typtage. Eine leere Gebäudeart
        /// wird als NULL geschrieben — „keine Wahl" ist genau ein Zustand.
        /// </summary>
        private static IEnumerable<KeyValuePair<string, object>> TyptagwahlWerte(ProjektStand p)
        {
            yield return W(TwwSchema.SPALTE_TYPTAGE_AKTIV, Bool(p.TyptageAktiv));
            yield return W(TwwSchema.SPALTE_TYPTAGE_KLIMAZONE, p.TyptageKlimazone);
            yield return W(TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART,
                           string.IsNullOrWhiteSpace(p.TyptageGebaeudeart) ? null : p.TyptageGebaeudeart.Trim());
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
