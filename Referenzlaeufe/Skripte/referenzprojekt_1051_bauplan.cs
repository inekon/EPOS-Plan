// Der Bauplan des Referenzprojekts 1051 "Referenzprojekt Konditionierung" (Teilkonzept
// Konditionierungsprofile 10.2; Entwurf KP3, Festlegungen 31 und 32; E58 F5 (a), F8 (a)) - die EINE
// Stelle, an der Bau, gesaete Zellen und Programmwege stehen. Nutzer:
//   - das Saatskript referenzprojekt_1051_konditionierung.cs (#:include), das 1051 in der
//     Testdatenbank anlegt;
//   - die Bauwahlprobe EPOS.Kern.Tests/KonditionierungBauwahlprobeTests (verlinkt im Testprojekt), die
//     dieselben Wege je Kandidat auf einer Arbeitskopie geht und den Bau waehlt;
//   - die Wache der Welle RP1b (Festlegung 33).
// Darum nur Wege des Kerns, die auch Dialog und Assistent gehen (GebaeudeStammCtrl und WizardCtrl sind
// internal: EPOS.Kern gibt sie dem Skript frei), und kein SQL, das schreibt -
// ausser den Kopfzellen von Tab_Projekt (Beschreibung, Datum, Kostenstempel; Muster 1049/1050/1052).
//
// DER BAU. 1051 ist eine Kopie von 1007 (ein Gebaeude auf dem VDI-Weg, Klima 1007001). Dessen Bau
// (EFH-A-TS-212) traegt die Rampe nur an einem Morgen (Befund B25); der Bau wird deshalb per Probe
// mit dem echten Kern gewaehlt (Festlegung 31): Kandidaten Verw_I_40 und Verw_I_33, Gegenprobe
// EFH-A-TS-212. Der gewaehlte Katalogbau wird ueber "Duplizieren..." (GebaeudeStammCtrl.Duplizieren)
// zu einem EIGENEN Referenzkatalogbau - die Bestandssaetze bleiben Fixtures anderer Tests.
//
// DIE KONDITIONIERUNG DES REFERENZKATALOGBAUS - der Arbeitsstand des Katalogeditors
// (Konditionierungsarbeit) und EIN OK (GebaeudeStammCtrl.KatalogSchreiben; N1.66 Nr. 1):
//   1. Schalter des Baus: Kuehlung_Aktiv = 1 (die Kuehlvorlage verlangt ihn; das Projekt rechnet die
//      Kuehlung nicht, Kuehlbetrieb bleibt aus) und Sommerlueftung = 1.
//   2. Ferienzeitraeume 23.12.-6.1. (Tag 357-6, ueber den Jahreswechsel) und 1.-14.8. (Tag 213-226).
//   3. Heizperiode 1.10.-30.4. (Zeile SAISON der Heizspalte, Tag 274-120; E53).
//   4. Die ausgelieferte Vorlage "Buero" in allen fuenf Groessen ("Vorlage uebernehmen"); bei der
//      Lueftung zuerst "aufteilen" (Gesamtangabe Luftwechselrate, E56 F5 (a)). Die Vorlagen bringen
//      die neun Feiertage "wie Sonntag" mit.
//   5. Die Nachtzeile der Lueftung wird die Nachtauskuehlung (E58 F8 (a), P9): 2,0 1/h von 18 bis
//      7 Uhr, bedingt mit dem Aussenabstand 2 K; Wochenende und Ferien bleiben 0,1 1/h, die
//      ausgelieferte Vorlage bleibt unveraendert.
// Danach die Zuordnung Katalog -> Projekt (WizardCtrl.Schreibe_Projekt_ZuordungGebaeude, der Weg der
// Gebaeudeliste) mit der Flaeche des Baus; sie ersetzt das Gebaeude der Kopie.
//
// DIE PROJEKTEINSTELLUNG. Aufheizoptimierung an (KonfigurationCtrl.AufheizvorgabeSetzen): Bemessung (b)
// "kaelteste Stunde - 2 K", taeglich, Reserve leer (wirksam 20 %), kein Aufschlag, keine manuelle
// Aufheizzeit (Festlegung 42). Alles uebrige wie 1007.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Referenzlaeufe.Skripte
{
    /// <summary>Der Bauplan des Referenzprojekts 1051 (Kopf der Datei).</summary>
    public static class Konditionierungsprojekt1051
    {
        public const int VORLAGE = 1007;
        public const string VORLAGE_NAME = "Laurentiuskirche";
        public const int NEU = 1051;
        public const string NAME = "Referenzprojekt Konditionierung";
        public const string BESCHREIBUNG =
            "Referenzprojekt der Konditionierungsprofile: Kopie von Projekt 1007 mit einem Bürobau aus dem Katalog — " +
            "Kalender aller fünf Größen aus der Vorlage „Büro“, Ferien, Feiertage, Heizperiode 1.10.–30.4., " +
            "Nachtauskühlung und Sommerlüftung, Aufheizoptimierung mit der Bemessung „kälteste Stunde − 2 K“. " +
            "Hält Vorlagenweg, Kopierweg Katalog → Projekt, Kalender und Aufheizrampe im Regressionsnetz.";
        public const string DATUM = "2026-10-03 00:00:00";

        /// <summary>Die Kandidaten der Bauwahlprobe (Festlegung 31) und ihre Gegenprobe (der Bau von 1007).</summary>
        public static readonly string[] KANDIDATEN = { "Verw_I_40", "Verw_I_33" };
        public const string GEGENPROBE = "EFH-A-TS-212";

        /// <summary>Der per Probe gewaehlte Katalogbau (KonditionierungBauwahlprobeTests).</summary>
        public const string KATALOGBAU = "Verw_I_40";

        /// <summary>Der Name des eigenen Referenzkatalogbaus.</summary>
        public const string REFERENZBAU = "Referenzbau Konditionierung";

        /// <summary>Die Ferienzeitraeume als Jahrestage (Gemeinjahr): 23.12.-6.1. und 1.-14.8.</summary>
        public static readonly (int Beginn, int Ende)[] FERIEN = { (357, 6), (213, 226) };

        /// <summary>Die Heizperiode 1.10.-30.4. als Jahrestage (E53).</summary>
        public const int HEIZPERIODE_BEGINN = 274, HEIZPERIODE_ENDE = 120;

        /// <summary>Die Nachtzeile der Lueftung (Nachtauskuehlung, E58 F8 (a)).</summary>
        public const double NACHTLUEFTUNG = 2.0;       // 1/h
        public const int NACHTLUEFTUNG_VON = 18, NACHTLUEFTUNG_BIS = 7;
        public const double NACHTLUEFTUNG_ABSTAND_K = 2.0;

        /// <summary>Die Aufheizvorgabe des Projekts: an, (b) mit 2 K, Reserve leer, taeglich, ohne Aufschlag.</summary>
        public static readonly Aufheizvorgabe AUFHEIZ = new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 2.0,
                                                                          null, DbWerte.AUFHEIZ_ART_TAEGLICH, 0, 0.0);

        private static DbParam P(object w) => new DbParam("?", w ?? DBNull.Value);

        private static long Zahl(string sql, params object[] w) => Zonenprojekt1052.Zahl(sql, w);

        private static DataTable Tabelle(string sql, params object[] w) => Zonenprojekt1052.Tabelle(sql, w);

        /// <summary>Die ausgelieferte Vorlage "Buero" einer Groesse; <c>null</c> = keine oder mehrere.</summary>
        public static KonditionierungsvorlageCtrl.Vorlage Buero(Konditionierungsgroesse g)
        {
            List<KonditionierungsvorlageCtrl.Vorlage> l = new KonditionierungsvorlageCtrl().Liste(g)
                .Where(v => v.Ausgeliefert && string.Equals(v.Nutzung, DbWerte.KOND_NUTZUNG_BUERO, StringComparison.Ordinal)).ToList();
            return l.Count == 1 ? l[0] : null;
        }

        // =====================================================================
        //  Der Referenzkatalogbau
        // =====================================================================

        /// <summary>
        /// Legt aus dem Katalogbau <paramref name="quelle"/> den eigenen Katalogbau <paramref name="name"/>
        /// an ("Duplizieren...") und konditioniert ihn im Arbeitsstand mit EINEM OK (Kopf der Datei).
        /// <c>null</c> = gelungen, sonst der Grund.
        /// </summary>
        public static string KatalogbauAnlegen(string quelle, string name, out int id)
        {
            id = 0;
            var stamm = new GebaeudeStammCtrl();
            GebaeudeModel q = stamm.Lies(quelle);
            if (q == null || q.ID <= 0) return "Der Katalogbau '" + quelle + "' fehlt.";
            Katalogkopie.Ergebnis k = GebaeudeStammCtrl.Duplizieren(q.ID, name);
            if (!k.Ok) return "Duplizieren scheitert: " + k.Meldung;
            id = k.Id;

            var kond = new KonditionierungCtrl();
            Konditionierungsstand ebene = kond.StandLesen(KonditionierungCtrl.Eigner.Katalogbau(id), out string m);
            if (m != null) return "Konditionierung des Katalogbaus: " + m;
            Matrixeingang alt = ebene.Bestand;

            // Die Kuehlung des Baus ist an (Schalter 1); wirksam wird sie, sobald ein Kuehlsollwert steht -
            // dieselbe Regel wie der Editor (KonditionierungHuelle.Arbeitsstand).
            static Konditionierungsarbeitsstand Kuehlung(Konditionierungsarbeitsstand a)
                => a.MitGebaeude(a.Gebaeude.MitBestand(b => b.KuehlungWirksam = b.KuehlSollwert.HasValue));

            // 2) Ferien.
            var a = new Konditionierungsarbeitsstand(ebene.MitBestand(b =>
            {
                for (int i = 0; i < FERIEN.Length; i++)
                {
                    b.Ferienbeginn[i] = FERIEN[i].Beginn;
                    b.Ferienende[i] = FERIEN[i].Ende;
                }
            }), null, q.Wohnflaeche_gesamt, null);
            a = Kuehlung(a);

            // 3) Heizperiode.
            Konditionierungsschritt s = Konditionierungsarbeit.ZelleSetzen(a, new Konditionierungsort(Konditionierungsgroesse.Heizsoll),
                DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(HEIZPERIODE_BEGINN, HEIZPERIODE_ENDE));
            if (!s.Ok) return "Heizperiode: " + s.Meldung;
            a = Kuehlung(s.Stand);

            // 4) "Buero" in allen fuenf Groessen.
            var vorlagen = new KonditionierungsvorlageCtrl();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                KonditionierungsvorlageCtrl.Vorlage v = Buero(g);
                if (v == null) return "Die ausgelieferte Vorlage 'Büro' der Größe " + g + " fehlt oder ist mehrdeutig.";
                Konditionierungsvorlage inhalt = vorlagen.Inhalt(v.Id, out string mv);
                if (inhalt == null) return "Vorlage " + v.Bezeichner + ": " + mv;
                var ort = new Konditionierungsort(g);
                s = Konditionierungsarbeit.VorlageUebernehmen(a, ort, inhalt);
                if (s.Rueckfrage && g == Konditionierungsgroesse.Lueftung)
                {
                    // Rueckfrage "aufteilen" (Gesamtangabe Luftwechselrate): ja.
                    Konditionierungsschritt t = Konditionierungsarbeit.LuftwechselAufteilen(a);
                    if (!t.Ok) return "Luftwechsel aufteilen: " + t.Meldung;
                    a = Kuehlung(t.Stand);
                    s = Konditionierungsarbeit.VorlageUebernehmen(a, ort, inhalt);
                }
                if (s.Rueckfrage) return "Vorlage " + v.Bezeichner + ": unerwartete Rückfrage";
                if (!s.Ok) return "Vorlage " + v.Bezeichner + ": " + s.Meldung;
                a = Kuehlung(s.Stand);
            }

            // 5) Die Nachtzeile der Lueftung wird die Nachtauskuehlung.
            s = Konditionierungsarbeit.ZelleSetzen(a, new Konditionierungsort(Konditionierungsgroesse.Lueftung), DbWerte.KOND_ZEILE_NACHT,
                Matrixzelle.AusWert(NACHTLUEFTUNG, NACHTLUEFTUNG_VON, NACHTLUEFTUNG_BIS, NACHTLUEFTUNG_ABSTAND_K));
            if (s.Rueckfrage) return "Nachtzeile der Lüftung: unerwartete Rückfrage";
            if (!s.Ok) return "Nachtzeile der Lüftung: " + s.Meldung;
            a = s.Stand;
            s = Konditionierungsarbeit.MatrixErneut(a, new Konditionierungsort(Konditionierungsgroesse.Lueftung));
            if (!s.Ok) return "Lüftung erneut anwenden: " + s.Meldung;
            a = s.Stand;

            // OK: Kopf mit den Bestandsfeldern des Arbeitsstands und den Schaltern, dazu die Konditionierung.
            GebaeudeModel modell = stamm.Lies(name);
            if (modell == null || modell.ID != id) return "Der Referenzkatalogbau ist nicht lesbar.";
            Bestandsfelder(modell, alt, a.Gebaeude.Bestand);
            modell.Kuehlung_Aktiv = true;
            modell.Sommerlueftung = true;
            GebaeudeStammCtrl.Katalogschreibergebnis e = GebaeudeStammCtrl.KatalogSchreiben(modell, false, name, a.Gebaeude);
            if (!e.Ok) return "OK des Katalogbaus: " + e.Meldung;
            if (e.Id != id) return "Das OK traf den Katalogbau " + e.Id + " statt " + id + ".";
            return null;
        }

        /// <summary>
        /// Die geaenderten Bestandsfelder in den Kopf - wie der Editor sie in seinen Feldsatz zurueckschreibt
        /// (nur Geaendertes) und der Feldsatz ins Modell geht.
        /// </summary>
        private static void Bestandsfelder(GebaeudeModel g, Matrixeingang alt, Matrixeingang neu)
        {
            static bool Anders(double? x, double? y) => !Kalendervergleich.Gleich(x, y);
            if (Anders(alt.SollTag, neu.SollTag) && neu.SollTag.HasValue) g.Raumsolltemperatur_Tag = neu.SollTag.Value;
            if (Anders(alt.SollNacht, neu.SollNacht) && neu.SollNacht.HasValue) g.Raumsolltemperatur_Nachtabsenkung = neu.SollNacht.Value;
            if (Anders(alt.SollWochenende, neu.SollWochenende) && neu.SollWochenende.HasValue) g.Raumsolltemperatur_Wochenende = neu.SollWochenende.Value;
            if (Anders(alt.SollFerien, neu.SollFerien) && neu.SollFerien.HasValue) g.Raumsolltemperatur_Ferien = neu.SollFerien.Value;
            if (Anders(alt.KuehlSollwert, neu.KuehlSollwert)) g.Kuehl_Sollwert = neu.KuehlSollwert;
            if (Anders(alt.KuehlSollwertNacht, neu.KuehlSollwertNacht)) g.Kuehl_Sollwert_Nacht = neu.KuehlSollwertNacht;
            if (Anders(alt.LuftwechselInfiltration, neu.LuftwechselInfiltration)) g.Luftwechsel_Infiltration = neu.LuftwechselInfiltration;
            if (Anders(alt.LuftwechselNutzer, neu.LuftwechselNutzer)) g.Luftwechsel_Nutzer = neu.LuftwechselNutzer;
            if (Anders(alt.Luftwechselrate, neu.Luftwechselrate) && neu.Luftwechselrate.HasValue) g.Luftwechselrate = neu.Luftwechselrate.Value;
            if (Anders(alt.InterneWaermegewinne, neu.InterneWaermegewinne) && neu.InterneWaermegewinne.HasValue)
                g.Interne_Waermegewinne = neu.InterneWaermegewinne.Value;
            if (alt.NachtBeginn != neu.NachtBeginn) g.Nachtabsenkung_Beginn = neu.NachtBeginn;
            if (alt.NachtEnde != neu.NachtEnde) g.Nachtabsenkung_Ende = neu.NachtEnde;
            if (Anders(alt.Ferienmerker, neu.Ferienmerker)) g.Ferien = neu.Ferienmerker;
            if (Anders(alt.Wochenendmerker, neu.Wochenendmerker)) g.Wochenende = neu.Wochenendmerker;
            double[] b = neu.Ferienbeginn, e = neu.Ferienende;
            g.Ferienbeginn_1 = b[0]; g.Ferienende_1 = e[0];
            g.Ferienbeginn_2 = b[1]; g.Ferienende_2 = e[1];
            g.Ferienbeginn_3 = b[2]; g.Ferienende_3 = e[2];
            g.Ferienbeginn_4 = b[3]; g.Ferienende_4 = e[3];
        }

        // =====================================================================
        //  Das Projekt
        // =====================================================================

        /// <summary>
        /// Baut 1051 auf der frischen Kopie <paramref name="projekt"/> von 1007: Kopfzellen, Zuordnung des
        /// Referenzkatalogbaus <paramref name="katalogbau"/> statt des kopierten Gebaeudes, Aufheizvorgabe.
        /// <c>null</c> = gelungen, sonst der Grund.
        /// </summary>
        public static string Bauen(int projekt, int katalogbau)
        {
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?",
                                           P(BESCHREIBUNG), P(DATUM), P(DATUM), P(projekt));

            DataTable z = Tabelle("SELECT ID, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, dezWarmwasserbereitung " +
                                  "FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", projekt);
            if (z.Rows.Count != 1) return "Die Kopie trägt " + z.Rows.Count + " Gebäudezuordnungen statt einer.";
            DataTable kb = Tabelle("SELECT Bezeichner, Wohnflaeche_gesamt FROM Tab_Gebaeude_STAMM WHERE ID = ?", katalogbau);
            if (kb.Rows.Count != 1) return "Der Referenzkatalogbau " + katalogbau + " fehlt.";
            DataRow r = z.Rows[0];
            var zeile = new Z_ProjGebModel
            {
                ID_Z = 0,
                ID_Projekt = projekt,
                Gebaeudename = Convert.ToString(kb.Rows[0][0], CultureInfo.InvariantCulture),
                ID_Gebaeude_Stamm = katalogbau,
                Wohnflaeche = Convert.ToDouble(kb.Rows[0][1], CultureInfo.InvariantCulture),
                Einheit = Convert.ToString(r[1], CultureInfo.InvariantCulture),
                Jahresnutzungsgrad = Convert.ToDouble(r[2], CultureInfo.InvariantCulture),
                DezentralWarmwasser = r[3] != DBNull.Value && Convert.ToInt64(r[3], CultureInfo.InvariantCulture) != 0,
            };
            using (DbVorgang v = DataRepository.Vorgang())
            {
                if (!new WizardCtrl().Schreibe_Projekt_ZuordungGebäude(projekt, new List<Z_ProjGebModel> { zeile }, v, out string fehl))
                {
                    v.Rollback();
                    return "Die Gebäudeliste ist nicht geschrieben (" + fehl + ").";
                }
                v.Commit();
            }

            if (!KonfigurationCtrl.AufheizvorgabeSetzen(projekt, AUFHEIZ)) return "Die Aufheizvorgabe ist nicht geschrieben.";

            // Kopfzellen erneut: Die Schreibwege stempeln Aenderungsdatum und (ueber die Trigger des
            // Kostenstempels) Kosten_Geaendert - leer wie bei jedem anderen Projekt der Testdatenbank.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Aenderungsdatum = ?, Erstelldatum = ?, Kosten_Geaendert = NULL WHERE ID = ?",
                                           P(DATUM), P(DATUM), P(projekt));
            return null;
        }

        // =====================================================================
        //  Pruefen
        // =====================================================================

        /// <summary>Die Id des Referenzkatalogbaus; 0 = keiner.</summary>
        public static int Referenzbau() => (int)Zahl("SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", REFERENZBAU);

        /// <summary>Die Abweichungen von 1051 und seinem Referenzkatalogbau vom Plan; leer = steht wie gesaet.</summary>
        public static List<string> Pruefen(int projekt)
        {
            var f = new List<string>();
            DataTable p = Tabelle("SELECT Beschreibung, Aenderungsdatum, Erstelldatum, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", projekt);
            if (p.Rows.Count != 1) { f.Add("Projekt " + projekt + " fehlt"); return f; }
            if (!string.Equals(Convert.ToString(p.Rows[0][0], CultureInfo.InvariantCulture), BESCHREIBUNG, StringComparison.Ordinal))
                f.Add("Beschreibung weicht ab");
            foreach (int i in new[] { 1, 2 })
                if (!string.Equals(Datum(p.Rows[0][i]), DATUM, StringComparison.Ordinal)) f.Add(p.Columns[i].ColumnName + " weicht ab");
            if (p.Rows[0][3] != DBNull.Value) f.Add("Kosten_Geaendert ist gestempelt");

            int kb = Referenzbau();
            if (kb <= 0) { f.Add("Der Referenzkatalogbau fehlt"); return f; }
            if (Zahl("SELECT ReadOnly FROM Tab_Gebaeude_STAMM WHERE ID = ?", kb) != 0) f.Add("Der Referenzkatalogbau ist ausgeliefert");

            int geb = Zonenprojekt1052.Gebaeude(projekt);
            if (geb <= 0) { f.Add("nicht genau ein Gebäude mit Projektkopie"); return f; }
            if (Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", geb) != kb) f.Add("Das Gebäude stammt nicht aus dem Referenzkatalogbau");
            if (Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude z, Tab_Gebaeude_STAMM s WHERE z.ID_Projekt = ? AND s.ID = ? " +
                     "AND z.Wohnflaeche_Waermebedarf = s.Wohnflaeche_gesamt", projekt, kb) != 1)
                f.Add("Die Zuordnung trägt nicht die Fläche des Baus");
            if (Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = ?", geb) != 0) f.Add("Das Gebäude trägt Zonen");

            // Bau und Gebaeude: Schalter, Ferien, Nachtzeit, Heizgrenze, manuelle Aufheizzeit.
            foreach ((string tabelle, long id) in new[] { ("Tab_Gebaeude_STAMM", (long)kb), ("Tab_Gebaeude", (long)geb) })
            {
                DataTable g = Tabelle("SELECT Kuehlung_Aktiv, Sommerlueftung, Ferienbeginn_1, Ferienende_1, Ferienbeginn_2, Ferienende_2, " +
                                      "Heizleistung_Max FROM \"" + tabelle + "\" WHERE ID = ?", id);
                DataRow r = g.Rows[0];
                string soll = "1|1|357|6|213|226|∅", ist = string.Join("|", r.ItemArray.Select(o => o == DBNull.Value ? "∅" : Convert.ToString(o, CultureInfo.InvariantCulture)));
                if (ist != soll) f.Add(tabelle + ": " + ist + " statt " + soll);
            }
            if (Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ? AND Aufheizzeit_Manuell_H IS NOT NULL", geb) != 0)
                f.Add("Das Gebäude trägt eine manuelle Aufheizzeit");

            // Die Konditionierung an Bau und Gebaeude: fuenf Kalender aus "Buero", Saison, Nachtauskuehlung.
            var kond = new KonditionierungCtrl();
            foreach (KonditionierungCtrl.Eigner e in new[] { KonditionierungCtrl.Eigner.Katalogbau(kb), KonditionierungCtrl.Eigner.Gebaeude(geb) })
            {
                Konditionierungsstand st = kond.StandLesen(e, out string ms);
                string wer = e.ToString();
                if (ms != null) f.Add(wer + ": " + ms);
                IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> angelegt = st.Angelegt();
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    KonditionierungsvorlageCtrl.Vorlage v = Buero(g);
                    if (!angelegt.ContainsKey(g)) f.Add(wer + ": kein Kalender " + g);
                    else if (v == null || st.Herkunft(g).Vorlage != v.Bezeichner) f.Add(wer + ": Herkunft " + g + " '" + st.Herkunft(g) + "'");
                }
                Matrixzelle saison = st.Vorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON);
                if (saison.Von != HEIZPERIODE_BEGINN || saison.Bis != HEIZPERIODE_ENDE) f.Add(wer + ": Heizperiode " + saison.Von + "–" + saison.Bis);
                Matrixzelle nacht = st.Vorgabe(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT);
                if (!nacht.Belegt || nacht.Aus || nacht.Wert != NACHTLUEFTUNG || nacht.Von != NACHTLUEFTUNG_VON || nacht.Bis != NACHTLUEFTUNG_BIS
                    || nacht.BedingtK != NACHTLUEFTUNG_ABSTAND_K)
                    f.Add(wer + ": Nachtzeile der Lüftung weicht ab");
            }
            if (Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NOT NULL", geb) != 0)
                f.Add("Das Gebäude trägt Zonenkalender");

            // Die Projekteinstellung.
            Aufheizvorgabe av = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
            if (!AUFHEIZ.Equals(av)) f.Add("Aufheizvorgabe " + av + " statt " + AUFHEIZ);
            if (KonfigurationCtrl.KuehlbetriebLesen(projekt)) f.Add("Kühlbetrieb an");

            foreach (string t in new[] { "Tab_Ergebnis", "Tab_ErgebnisWirtschaftlichkeit" })
                if (Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ID_Projekt = ?", projekt) != 0) f.Add(t + " führt Zeilen");
            return f;
        }

        private static string Datum(object o)
            => o is DateTime d ? d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
               : o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
    }
}
