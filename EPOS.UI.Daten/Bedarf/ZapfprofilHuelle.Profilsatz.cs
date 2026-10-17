using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    internal static partial class ZapfprofilHuelle
    {
        // =================================================================================
        // Profil ablegen (ZC6): Katalogsatz oder Projektzeile aus dem Generatorergebnis
        // =================================================================================

        internal const string PS_KNOPF_KATALOG = "ZPG_PS_KNOPF_KATALOG";
        internal const string PS_KNOPF_PROJEKT = "ZPG_PS_KNOPF_PROJEKT";
        internal const string PS_WARNUNG_PROJEKT = "ZPG_PS_WARNUNG_PROJEKT";
        internal const string PS_HINWEIS_GENERATOR = "ZPG_PS_HINWEIS_WEG_GENERATOR";
        internal const string PS_HINWEIS_BESTAND = "ZPG_PS_HINWEIS_WEG_BESTAND";
        internal const string PS_HINWEIS_ZIRK_DOPPELT = "ZPG_PS_HINWEIS_ZIRK_DOPPELT";
        internal const string PS_NAME_PROJEKT = "ZPG_PS_NAME_PROJEKT";
        internal const string PS_VARIANTE_MIT = "ZPG_PS_VARIANTE_MIT";
        internal const string PS_VARIANTE_OHNE = "ZPG_PS_VARIANTE_OHNE";
        internal const string PS_BESCHREIBUNG = "ZPG_PS_BESCHREIBUNG";
        internal const string PS_SPITZE = "ZPG_PS_SPITZE";
        internal const string PS_WOCHENTAGE = "ZPG_PS_WOCHENTAGE";
        internal const string PS_MSG_KEIN_ERGEBNIS = "ZPG_PS_MSG_KEIN_ERGEBNIS";
        internal const string PS_MSG_OHNE_BEDARF = "ZPG_PS_MSG_OHNE_BEDARF";
        internal const string PS_MSG_NAME_LEER = "ZPG_PS_MSG_NAME_LEER";
        internal const string PS_MSG_NAME_VORHANDEN = "ZPG_PS_MSG_NAME_VORHANDEN";
        internal const string PS_MSG_TYP_VORHANDEN = "ZPG_PS_MSG_TYP_VORHANDEN";
        internal const string PS_MSG_NICHT_GESPEICHERT = "ZPG_PS_MSG_NICHT_GESPEICHERT";
        internal const string PS_MSG_KATALOG_GESPEICHERT = "ZPG_PS_MSG_KATALOG_GESPEICHERT";
        internal const string PS_MSG_PROJEKT_HINZUGEFUEGT = "ZPG_PS_MSG_PROJEKT_HINZUGEFUEGT";

        /// <summary>
        /// <b>Die Vorschau des Profilsatzes</b> zum Arbeitsstand: derselbe Generatorweg wie
        /// <see cref="Jahresreihe"/> und der Lauf (<see cref="ZapfprofilCtrl.Rechnen(int, ZapfprofilStand, int, bool[])"/>),
        /// abgebildet über <see cref="ZapfprofilProfilsatzAbbildung"/>. Schreibt nichts.
        /// </summary>
        internal static ZapfprofilProfilsatzVorschauDaten ProfilsatzVorschau(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                              ZapfprofilStand basis, bool mitZirkulation)
        {
            bool generator = RechenwegIstGenerator(idProjekt);
            bool bestandZirkulation = BestandswegHatZirkulation(idProjekt);
            var d = new ZapfprofilProfilsatzVorschauDaten
            {
                MitZirkulation = mitZirkulation,
                Variante = Variantentext(mitZirkulation),
                RechenwegGenerator = generator,
                HinweisRechenweg = generator
                    ? Text_(PS_HINWEIS_GENERATOR, "Das Projekt rechnet sein Brauchwasser gerade über den Zapfprofilgenerator; ein abgelegter Profilsatz rechnet erst mit, wenn der Rechenweg auf Bestandsprofile steht.")
                    : Text_(PS_HINWEIS_BESTAND, "Das Projekt rechnet sein Brauchwasser gerade über die Bestandsprofile; ein Profilsatz, der dem Projekt hinzugefügt wird, rechnet mit."),
                WarnungProjekt = Text_(PS_WARNUNG_PROJEKT, "Das Profil steht dann nur in diesem Projekt, nicht im Katalog. Es rechnet als Bestandsprofil ohne Ferien, Feiertage, Stochastik und Typtage."),
                KnopfKatalog = Text_(PS_KNOPF_KATALOG, "Als Katalogsatz speichern…"),
                KnopfProjekt = Text_(PS_KNOPF_PROJEKT, "Zum Projekt hinzufügen"),
                BestandswegHatZirkulation = bestandZirkulation,
                HinweisZirkulationDoppelt = bestandZirkulation
                    ? Text_(PS_HINWEIS_ZIRK_DOPPELT, "Das Projekt rechnet auf dem Bestandsweg bereits eine Zirkulation. Mit Zirkulation im Profil zählt der Verlust doppelt — empfohlen: ohne Zirkulation.")
                    : "",
            };

            (string kennung, string grund, ZapfprofilProfilsatz satz) = ProfilsatzRechnen(idProjekt, eingabe, basis, mitZirkulation);
            if (satz == null)
            {
                d.Kennung = kennung;
                d.Grund = grund;
                return d;
            }
            d.Gerechnet = true;
            d.JahressummeMwh = satz.JahressummeMwh;
            d.MonatssummenMwh = satz.MonatssummenMwh.ToList();
            d.SpitzenstundeWocheKw = satz.SpitzenstundeWocheKw;
            d.SpitzenstundeWocheIndex = satz.SpitzenstundeWocheIndex;
            d.SpitzenstundeText = Spitzentext(satz.SpitzenstundeWocheIndex, satz.SpitzenstundeWocheKw);
            try { d.NameProjekt = ProjektzeilenName(idProjekt); }
            catch (Exception) { d.NameProjekt = ""; }
            return d;
        }

        /// <summary>
        /// <b>„Als Katalogsatz speichern…"</b>: legt in EINEM Vorgang das Wochenprofil (Typ
        /// <paramref name="typ"/>, leer = der Name) über <see cref="TypProfilCtrl.SpeichernUnter"/> und den
        /// Kopf mit zwölf Monatssummen [MWh] über <see cref="BrauchwasserStammCtrl.SaveHead"/> in
        /// <c>Tab_Brauchwasser_STAMM</c> an — als Anwendersatz (<c>ReadOnly</c> = 0, ohne Katalogschlüssel).
        /// Liefert die ID des Kopfs oder eine benannte Ablehnung: Name leer, Name oder Typ schon
        /// vorhanden, kein Ergebnis gerechnet, Schreiben gescheitert. Der Rechenweg des Projekts bleibt.
        /// </summary>
        internal static ZapfprofilProfilsatzErgebnis AlsKatalogsatzSpeichern(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                             ZapfprofilStand basis, string name, string typ,
                                                                             string beschreibung, bool mitZirkulation)
        {
            string n = (name ?? "").Trim();
            if (n.Length == 0)
                return Abgelehnt(PS_MSG_NAME_LEER, Text_(PS_MSG_NAME_LEER, "Bitte einen Namen für den Katalogsatz angeben."), n);
            string t = string.IsNullOrWhiteSpace(typ) ? n : typ.Trim();
            string b = string.IsNullOrWhiteSpace(beschreibung) ? Beschreibungstext(mitZirkulation) : beschreibung.Trim();

            var stamm = new BrauchwasserStammCtrl();
            if (stamm.Exists(n))
                return Abgelehnt(PS_MSG_NAME_VORHANDEN,
                                 Format(Text_(PS_MSG_NAME_VORHANDEN, "Ein Katalogsatz „{0}“ ist schon vorhanden."), n), n);
            // Ein belegter Typname wird wie der Projektname eindeutig gemacht („ (2)“, „ (3)“ …),
            // statt das Wochenprofil fremder Sätze zu überschreiben.
            t = TypnameEindeutig(t);

            (string kennung, string grund, ZapfprofilProfilsatz satz) = ProfilsatzRechnen(idProjekt, eingabe, basis, mitZirkulation);
            if (satz == null) return Abgelehnt(kennung, grund, n);

            int id = 0;
            string[] datenbank;
            bool ok = false;
            using (DataRepository.EngineModus())
            {
                try
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        using (Vorgangsklammer.Setzen(v))
                        {
                            if (TypProfilCtrl.SpeichernUnter(BedarfsArt.Brauchwasser, t, satz.WochenanteileFeld(), b)
                                    == TypAnlageErgebnis.Angelegt
                                && stamm.SaveHead(n, t, b, satz.MonatssummenMwh.ToArray(), true))
                            {
                                object wert = DataRepository.ExecuteScalar(
                                    "SELECT ID FROM Tab_Brauchwasser_STAMM WHERE Bezeichner = ?",
                                    new DbParam("@bez", n));
                                id = (wert != null && wert != DBNull.Value) ? Convert.ToInt32(wert, CultureInfo.InvariantCulture) : 0;
                                ok = id > 0;
                            }
                        }
                        if (ok) v.Commit();
                    }
                }
                catch (Exception ex) { ok = false; DataRepository.FehlerMelden(ex.Message); }
                datenbank = DataRepository.StilleFehlerAbholen();
            }
            if (!ok)
                return Abgelehnt(PS_MSG_NICHT_GESPEICHERT,
                                 Format(Text_(PS_MSG_NICHT_GESPEICHERT, "Der Profilsatz wurde nicht gespeichert — {0}"),
                                        string.Join(" ", datenbank ?? new string[0])), n);
            return new ZapfprofilProfilsatzErgebnis
            {
                Erfolg = true, Id = id, Name = n, JahressummeMwh = satz.JahressummeMwh,
                Kennung = PS_MSG_KATALOG_GESPEICHERT,
                Text = Format(Text_(PS_MSG_KATALOG_GESPEICHERT, "Der Katalogsatz „{0}“ ist gespeichert."), n)
            };
        }

        /// <summary>
        /// <b>„Zum Projekt hinzufügen"</b>: legt in EINEM Vorgang eine Projektkopie (<c>Tab_Brauchwasser</c>
        /// samt Typzeile <c>Tab_Brauchwassertyp</c>, ohne Katalogsatz) und ihre Zuordnungszeile
        /// <c>Z_Projekt_Brauchwasser</c> an — mit eigenen Monatsanteilen [%], eigenem Wochenprofil [%],
        /// Eingabeart „woche“ und der Jahressumme [MWh] des Generators (<c>WizardCtrl.Add_Projekt_Brauchwasser</c>).
        /// Name „Zapfprofil &lt;Projektname&gt;“ (ohne Projektnamen das Datum), bei Gleichnamigkeit mit Zähler.
        /// Liefert die ID der Zuordnungszeile oder eine benannte Ablehnung. Der Rechenweg des Projekts bleibt.
        /// </summary>
        internal static ZapfprofilProfilsatzErgebnis ZumProjektHinzufuegen(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                           ZapfprofilStand basis, bool mitZirkulation)
        {
            (string kennung, string grund, ZapfprofilProfilsatz satz) = ProfilsatzRechnen(idProjekt, eingabe, basis, mitZirkulation);
            if (satz == null) return Abgelehnt(kennung, grund, "");

            string n;
            try { n = ProjektzeilenName(idProjekt); }
            catch (Exception ex)
            {
                return Abgelehnt(PS_MSG_NICHT_GESPEICHERT,
                                 Format(Text_(PS_MSG_NICHT_GESPEICHERT, "Der Profilsatz wurde nicht gespeichert — {0}"), ex.Message), "");
            }
            string b = Beschreibungstext(mitZirkulation);

            int idZ = 0;
            string[] datenbank;
            bool ok = false;
            using (DataRepository.EngineModus())
            {
                try
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        using (Vorgangsklammer.Setzen(v))
                        {
                            object m = v.Skalar("SELECT Max(ID) FROM Tab_Brauchwasser");
                            int idKopf = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m, CultureInfo.InvariantCulture) : 0) + 1;
                            v.Ausfuehren("INSERT INTO Tab_Brauchwasser (ID, ID_Projekt, Bezeichner, Typ, Beschreibung) VALUES (?, ?, ?, ?, ?)",
                                         new DbParam("@hid", idKopf),
                                         new DbParam("@hproj", idProjekt),
                                         new DbParam("@hbez", n),
                                         new DbParam("@htyp", n),
                                         new DbParam("@hbesch", b));

                            object mt = v.Skalar("SELECT Max(ID) FROM Tab_Brauchwassertyp");
                            int idTyp = ((mt != null && mt != DBNull.Value) ? Convert.ToInt32(mt, CultureInfo.InvariantCulture) : 0) + 1;
                            v.Ausfuehren("INSERT INTO Tab_Brauchwassertyp (ID, ID_Brauchwasser, ID_Projekt, Typname, Beschreibung) VALUES (?, ?, ?, ?, ?)",
                                         new DbParam("@tid", idTyp),
                                         new DbParam("@tbw", idKopf),
                                         new DbParam("@tproj", idProjekt),
                                         new DbParam("@ttypn", n),
                                         new DbParam("@tbesch", b));

                            // Die Zeile über den Schreibweg des Assistenten: er findet die Kopie über ihre ID,
                            // schreibt Monatsanteile und Wochenprofil hinein (KBP-5a), die Zeile, die Eingabeart
                            // und markiert das Projekt als geändert.
                            var zeile = new Z_ProjektBrauchwasserModel
                            {
                                ID_Projekt = idProjekt, ID_Brauchwasser = idKopf, szBezeichner = n,
                                Summe = satz.JahressummeMwh,
                                Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_WOCHE,
                                Monatsanteile = satz.MonatsanteileProzent.ToArray(),
                                Wochenprofil = satz.WochenanteileProzent.ToArray()
                            };
                            if (new WizardCtrl().Add_Projekt_Brauchwasser(idProjekt, new List<Z_ProjektBrauchwasserModel> { zeile }, v))
                            {
                                object wert = v.Skalar("SELECT ID FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ? AND ID_Brauchwasser = ?",
                                                       new DbParam("@proj", idProjekt), new DbParam("@bw", idKopf));
                                idZ = (wert != null && wert != DBNull.Value) ? Convert.ToInt32(wert, CultureInfo.InvariantCulture) : 0;
                                ok = idZ > 0;
                            }
                        }
                        if (ok) v.Commit();
                    }
                }
                catch (Exception ex) { ok = false; DataRepository.FehlerMelden(ex.Message); }
                datenbank = DataRepository.StilleFehlerAbholen();
            }
            if (!ok)
                return Abgelehnt(PS_MSG_NICHT_GESPEICHERT,
                                 Format(Text_(PS_MSG_NICHT_GESPEICHERT, "Der Profilsatz wurde nicht gespeichert — {0}"),
                                        string.Join(" ", datenbank ?? new string[0])), n);
            return new ZapfprofilProfilsatzErgebnis
            {
                Erfolg = true, Id = idZ, Name = n, JahressummeMwh = satz.JahressummeMwh,
                Kennung = PS_MSG_PROJEKT_HINZUGEFUEGT,
                Text = Format(Text_(PS_MSG_PROJEKT_HINZUGEFUEGT, "„{0}“ steht als Brauchwasserprofil im Projekt."), n)
            };
        }

        // =================================================================================

        /// <summary>
        /// Rechnet den Arbeitsstand über den Generatorweg und bildet ihn ab; ohne Ergebnis Kennung und
        /// Grund (keine Zone, nicht verfügbar, keine Klimaregion, Ablehnung des Eingangs, kein Bedarf).
        /// </summary>
        private static (string Kennung, string Grund, ZapfprofilProfilsatz Satz) ProfilsatzRechnen(
            int idProjekt, ZapfprofilEingabeDaten eingabe, ZapfprofilStand basis, bool mitZirkulation)
        {
            string keinErgebnis = Text_(PS_MSG_KEIN_ERGEBNIS, "Es ist kein Zapfprofil gerechnet — ohne Ergebnis kein Profilsatz.");
            if (eingabe == null || eingabe.Zonen.Count == 0)
                return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis + " " + Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt."), null);

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja) return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis + " " + Verfuegbarkeitsgrund(verfuegbar), null);
            if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we))
                return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis + " "
                        + Text_("ZPG_MSG_KEINE_KLIMAREGION", "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau."), null);

            ZapfprofilProfilsatz satz;
            try
            {
                ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
                ZapfprofilErgebnis e = ZapfprofilCtrl.Rechnen(idProjekt, stand, jan1, we);
                satz = ZapfprofilProfilsatzAbbildung.Abbilden(e, jan1, mitZirkulation);
            }
            catch (ZapfprofilEingabeException ex) { return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis + " " + Satztext(ex.Satz), null); }
            catch (ParametersatzException ex) { return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis + " " + Satztext(ex.Satz), null); }
            catch (Exception ex) { return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis + " " + ex.Message, null); }

            if (satz == null) return (PS_MSG_KEIN_ERGEBNIS, keinErgebnis, null);
            if (!(satz.JahressummeKwh > 0.0))
                return (PS_MSG_OHNE_BEDARF, Text_(PS_MSG_OHNE_BEDARF, "Das Zapfprofil trägt keinen Bedarf — es gibt nichts abzulegen."), null);
            return ("", "", satz);
        }

        /// <summary>Der Typname <paramref name="t"/> oder, wenn belegt, mit dem ersten freien Zähler „ (2)“, „ (3)“ …</summary>
        internal static string TypnameEindeutig(string t)
        {
            string n = t;
            for (int i = 2; TypProfilCtrl.TypExists(BedarfsArt.Brauchwasser, n); i++)
                n = t + " (" + i.ToString(CultureInfo.InvariantCulture) + ")";
            return n;
        }

        /// <summary>
        /// Führt das Projekt auf dem Bestandsweg eine feste Zirkulation (Leistung und Laufzeit in
        /// <c>Tab_Einstellungen</c>, dieselbe Vorgabe, die <c>SimulationWaermebedarf.BestandswegZirkulation</c> liest)?
        /// </summary>
        internal static bool BestandswegHatZirkulation(int idProjekt)
        {
            try { return KonfigurationCtrl.NetzverlustvorgabeLesen(idProjekt)?.MitZirkulation == true; }
            catch (Exception) { return false; }
        }

        /// <summary>Gilt im Projekt der Generatorweg? Ohne lesbaren Weg: nein (Bestandsprofile).</summary>
        private static bool RechenwegIstGenerator(int idProjekt)
        {
            try { return ZapfprofilCtrl.Weg(idProjekt) == BrauchwasserWeg.Generator; }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// „Zapfprofil &lt;Projektname&gt;“, ohne Projektnamen mit dem Datum; trägt das Projekt den Namen
        /// schon (Kopie oder Zuordnungszeile) oder der Katalog, mit Zähler „ (2)“, „ (3)“ …
        /// </summary>
        internal static string ProjektzeilenName(int idProjekt)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            string bezug = string.IsNullOrWhiteSpace(projekt.m_szProjektname)
                ? DateTime.Today.ToString("d", CultureInfo.CurrentCulture)
                : projekt.m_szProjektname.Trim();
            string grund = Format(Text_(PS_NAME_PROJEKT, "Zapfprofil {0}"), bezug);
            var stamm = new BrauchwasserStammCtrl();
            string n = grund;
            for (int i = 2; NameBelegt(n, idProjekt, stamm); i++)
                n = grund + " (" + i.ToString(CultureInfo.InvariantCulture) + ")";
            return n;
        }

        private static bool NameBelegt(string n, int idProjekt, BrauchwasserStammCtrl stamm)
        {
            if (BrauchwasserStammCtrl.GetProjektId(n, idProjekt) > 0 || stamm.Exists(n)) return true;
            object v = DataRepository.ExecuteScalar("SELECT ID FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ? AND Bezeichner = ?",
                                                    new DbParam("@proj", idProjekt), new DbParam("@bez", n));
            return v != null && v != DBNull.Value;
        }

        private static string Variantentext(bool mitZirkulation)
            => mitZirkulation
                ? Text_(PS_VARIANTE_MIT, "Zapfwärme und Zirkulationsverlust")
                : Text_(PS_VARIANTE_OHNE, "nur Zapfwärme");

        private static string Beschreibungstext(bool mitZirkulation)
            => Format(Text_(PS_BESCHREIBUNG, "Aus dem Zapfprofilgenerator ({0})"), Variantentext(mitZirkulation));

        /// <summary>„Mo 07–08 Uhr: 3,2 kW" — Wochentag in der Oberflächensprache, Montag = Wochenstunde 0.</summary>
        internal static string Spitzentext(int index, double kw)
        {
            int tag = Math.Clamp(index, 0, ZapfprofilProfilsatzAbbildung.WOCHENSTUNDEN - 1) / ZapfprofilProfilsatzAbbildung.STUNDEN_TAG;
            int stunde = Math.Clamp(index, 0, ZapfprofilProfilsatzAbbildung.WOCHENSTUNDEN - 1) % ZapfprofilProfilsatzAbbildung.STUNDEN_TAG;
            // Eigene Kurznamen statt der Kulturdaten: ICU und Windows kürzen verschieden ("Mo." gegen "Mo").
            string[] namen = Text_(PS_WOCHENTAGE, "Mo,Di,Mi,Do,Fr,Sa,So").Split(',');
            string name = namen.Length == ZapfprofilProfilsatzAbbildung.WOCHENTAGE ? namen[tag].Trim() : (tag + 1).ToString(CultureInfo.CurrentCulture);
            return Format(Text_(PS_SPITZE, "{0} {1:00}–{2:00} Uhr: {3:N1} kW"), name, stunde, stunde + 1, kw);
        }

        private static ZapfprofilProfilsatzErgebnis Abgelehnt(string kennung, string text, string name)
            => new ZapfprofilProfilsatzErgebnis { Erfolg = false, Id = 0, Name = name ?? "", Kennung = kennung ?? "", Text = text ?? "" };
    }
}
