using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Vergleichsberichts und der Kalibrierung aus einer Messreihe</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 3,
    /// Punkte 4 und 5): Sie hält die gemessene Reihe eines Projekts gegen die gerechnete
    /// Jahresreihe des Arbeitsstands (<see cref="Messvergleich"/>), bildet aus derselben Reihe
    /// einen Jahresmesswert (<see cref="Messkalibrierung"/>) und, für eine Nichtwohn-Zone, den
    /// Kalibriervorschlag samt Schreibweg in eine Anwenderkopie
    /// (<see cref="TwwNutzungsartCtrl.VorschlagUebernehmen"/>).
    ///
    /// <para><b>Dieselbe Rechnung wie der Lauf</b>: Die gerechnete Reihe kommt aus
    /// <see cref="ZapfprofilCtrl.Rechnen(int, ZapfprofilStand, int, bool[], CancellationToken)"/> —
    /// deterministisch oder stochastisch, genau wie der Rechenweg des Arbeitsstands es sagt. Ein
    /// zweiter Rechenweg für den Vergleich entsteht nicht. Der Lauf läuft <b>nebenläufig</b> (Muster
    /// der Jahresreihe, 5.1); <c>abbruch</c> beendet ihn mit
    /// <see cref="OperationCanceledException"/>.</para>
    ///
    /// <para><b>Nur Verhältniszahlen verlassen den Vergleich</b> (Konzept Kapitel 9 K5): Das DTO
    /// trägt Verhältnisse, Anteile und Zählungen — keine gemessene Menge und keine gemessene
    /// Leistung. Der <b>Kalibriervorschlag</b> ist die Ausnahme mit Grund: Seine Zahlen sind
    /// Parameter der eigenen Katalogkopie des Anwenders und bleiben in dessen Datenbank
    /// (Konzept 4.8).</para>
    ///
    /// <para><b>Die Spitzenstreuung</b> (N15 Gruppe 3) kommt aus den Stundenspitzen der
    /// Realisierungen, die das Ergebnis je Zone führt (<c>ZonenErgebnis.StundenspitzenKw</c>). Sie
    /// steht, wenn <b>genau eine</b> Zone ein Ensemble trägt; bei mehreren Zonen ist die Spitze der
    /// Summe nicht die Summe der Spitzen (eigene Ziehung je Zone), und die Hülle benennt das
    /// (<c>MESSVERGLEICH_ENSEMBLE_ZONEN</c> in der Warnliste, dazu <c>EnsembleZonen</c> im DTO, damit
    /// die Zeile ihren eigenen Strichvermerk trägt) statt zu rechnen. Ohne Ensemble bleibt sie
    /// <c>null</c> und wird benannt (<c>MESSVERGLEICH_OHNE_ENSEMBLE</c>). Das Band der Dauerlinie
    /// braucht sie nicht — es ist ein Quantil der gerechneten Reihe.</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>
        /// <b>Die Hinweiskennungen des Vergleichs und der Kalibrierung</b> — je eine Ressource
        /// <c>ZPG_WARN_…</c> als Titel der Warnliste, wie bei <see cref="BILANZHINWEISE"/>. Die Wache
        /// <c>ZapfprofilHuelleMessreihenTests</c> hält die Liste gegen den Quelltext der beiden
        /// Kern-Dateien UND dieser Hülle und beide Sprachen gegen die Liste.
        /// </summary>
        internal static readonly string[] VALIDIERUNGSHINWEISE =
        {
            // Der Vergleich (Messvergleich.cs)
            "MESSVERGLEICH_BAND_NICHT_BEWERTBAR",
            "MESSVERGLEICH_BAND_UNGUELTIG", "MESSVERGLEICH_ENERGIE_ABWEICHUNG", "MESSVERGLEICH_FORM_IM_RAHMEN",
            "MESSVERGLEICH_FORM_UEBER_SCHWELLE", "MESSVERGLEICH_KALENDER_RASTER", "MESSVERGLEICH_OHNE_EINHEITEN",
            "MESSVERGLEICH_OHNE_ENSEMBLE", "MESSVERGLEICH_OHNE_FEIERTAGE", "MESSVERGLEICH_OHNE_MESSREIHE",
            "MESSVERGLEICH_OHNE_RECHNUNG", "MESSVERGLEICH_OHNE_STUNDENWERTE", "MESSVERGLEICH_OHNE_VERGLEICHSTAG",
            "MESSVERGLEICH_OHNE_VOLLEN_TAG", "MESSVERGLEICH_RECHNUNG_OHNE_MENGE", "MESSVERGLEICH_SCHALTTAG",
            "MESSVERGLEICH_SPITZENSTREUUNG", "MESSVERGLEICH_SPITZE_IM_BAND", "MESSVERGLEICH_SPITZE_UEBER_BAND",
            "MESSVERGLEICH_SPITZE_UNTER_BAND", "MESSVERGLEICH_SPREIZUNG_FEHLT", "MESSVERGLEICH_TAGTYP_FEHLT",
            "MESSVERGLEICH_TEILJAHR",
            // Diese Hülle selbst: Die Spreizung mehrerer Zonen ist ihre Sache, nicht die des Kerns
            // (der Vergleich nimmt EINE Spreizung); ebenso die Stichprobe der Realisierungsspitzen,
            // die bei mehreren Ensembles nicht zu bilden ist.
            "MESSVERGLEICH_SPREIZUNG_ZONEN", "MESSVERGLEICH_ENSEMBLE_ZONEN",
            // Die Kalibrierung (Messkalibrierung.cs)
            "MESSKALIBRIERUNG_HOCHGERECHNET", "MESSKALIBRIERUNG_HOCHGERECHNET_JAHRESGANG",
            "MESSKALIBRIERUNG_OHNE_BEZUGSMENGE", "MESSKALIBRIERUNG_OHNE_MENGE", "MESSKALIBRIERUNG_OHNE_MESSREIHE",
            "MESSKALIBRIERUNG_OHNE_STUNDENWERTE", "MESSKALIBRIERUNG_OHNE_TAGESGANG", "MESSKALIBRIERUNG_REIHE_ZU_KURZ",
            "MESSKALIBRIERUNG_TAGTYP_FEHLT", "MESSKALIBRIERUNG_WOCHENTAG_FEHLT"
        };

        /// <summary>
        /// Ein Satz des Vergleichs oder der Kalibrierung als Zeile der Warnliste: Titel aus
        /// <c>ZPG_WARN_…</c>, sonst der Sammeltitel; der Satz in der Oberflächensprache. Alle Sätze
        /// dieser Gruppe sind <b>Hinweise</b> — der Vergleich blockiert nichts.
        /// </summary>
        internal static ZapfprofilWarnDaten Validierungswarnung(ZapfSatz s)
        {
            string kennung = "ZPG_WARN_" + (s?.Kennung ?? "");
            string titel = Text_(kennung, null) ?? Text_("ZPG_AUS_HINWEIS", "Hinweis");
            return new ZapfprofilWarnDaten(kennung, titel, Satztext(s), ZapfprofilWarnstufe.Hinweis);
        }

        // =================================================================================
        // Der Vergleichsbericht (Kennzahlen (a) bis (e))
        // =================================================================================

        /// <summary>
        /// <b>Der Vergleich „synthetisch gegen gemessen"</b>: Er rechnet den Arbeitsstand
        /// (derselbe Weg wie der Lauf), liest die Messreihe <paramref name="reihe"/> des Projekts
        /// zurück und gibt die Kennzahlen als DTO — die fünf der Abnahme (a) bis (e), die der Reiter
        /// „Kennzahlen" als acht Zeilen zeigt (Energie und Abweichung, Spitze, Band, Spitzenstreuung,
        /// √N, Form, Monate). Jede Ablehnung ist ein benannter Grund im
        /// Ergebnis, keine Ausnahme; <paramref name="abbruch"/> bricht den Lauf ab
        /// (<see cref="OperationCanceledException"/> reicht der Aufrufer weiter).
        /// </summary>
        internal static ZapfprofilMessvergleichDaten Vergleichsbericht(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                  ZapfprofilStand basis, string reihe,
                                                                  CancellationToken abbruch)
        {
            var d = new ZapfprofilMessvergleichDaten { Reihe = reihe ?? "", Formschwelle = Formschwelle() };
            if (string.IsNullOrWhiteSpace(reihe)) return Ohne(d, ZapfSatz.Neu("MESSVERGLEICH_OHNE_MESSREIHE"));
            if (eingabe == null || eingabe.Zonen.Count == 0)
                return OhneText(d, "ZPG_MSG_KEINE_ZONE", Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt."));

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja) return OhneText(d, VerfuegbarkeitsKennung(verfuegbar), Verfuegbarkeitsgrund(verfuegbar));
            if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we))
                return OhneText(d, "ZPG_MSG_KEINE_KLIMAREGION",
                                Text_("ZPG_MSG_KEINE_KLIMAREGION", "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau."));

            var hinweise = new List<ZapfSatz>();
            Messreihe gemessen = TwwMessreihenCtrl.Lesen(idProjekt, reihe, out ZapfSatz fehler, hinweise);
            if (gemessen == null) return Ohne(d, fehler ?? ZapfSatz.Neu("MESSVERGLEICH_OHNE_MESSREIHE"));

            ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
            ZapfprofilErgebnis e;
            try
            {
                e = ZapfprofilCtrl.Rechnen(idProjekt, stand, jan1, we, abbruch);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is ZapfprofilEingabeException || ex is ParametersatzException)
            {
                ZapfSatz satz = ex is ZapfprofilEingabeException z ? z.Satz : ((ParametersatzException)ex).Satz;
                return Ohne(d, satz ?? ZapfSatz.Neu("MESSVERGLEICH_OHNE_RECHNUNG"));
            }
            catch (Exception ex) { return OhneText(d, "ZPG_MSG_JAHRESREIHE_UNERWARTET", ex.Message); }

            d.Stochastisch = e.Stochastisch;
            IReadOnlyList<double> spitzen = Realisierungsspitzen(e, hinweise, out int ensembleZonen);
            d.EnsembleZonen = ensembleZonen;
            var eingang = new Messvergleichseingang
            {
                Reihe = gemessen,
                SpreizungK = Gesamtspreizung(stand, gemessen.IstVolumen, hinweise),
                Gerechnet = Bilanzreihe.Summe(new[] { e.Zapfung, e.Zirkulation }.Where(r => r != null)),
                Kalender = Zapfkalender.Bilden(jan1, we, null),
                SynthetischeStundenspitzenKw = spitzen,
                Einheiten = Einheiten(stand)
            };
            try { eingang = Messvergleich.AusParametern(eingang, ZapfprofilCtrl.Parameter()); }
            catch (ParametersatzException) { /* die Vorgaben des Eingangs gelten */ }

            Messvergleichsergebnis v = Messvergleich.Vergleichen(eingang);
            hinweise.AddRange(v.Hinweise);
            if (!v.Ok) return Ohne(d, v.Abbruch, hinweise);

            d.Ok = true;
            d.Formschwelle = eingang.Formschwelle;
            if (v.Energie is { } a)
            {
                d.EnergieVerhaeltnis = a.Verhaeltnis;
                d.EnergieAbweichung = a.Abweichung;
            }
            if (v.Band is { } b)
            {
                d.Spitzenverhaeltnis = b.Spitzenverhaeltnis;
                d.BandUnten = b.BandUnten;
                d.BandOben = b.BandOben;
                d.PerzentilUnten = b.PerzentilUnten;
                d.PerzentilOben = b.PerzentilOben;
                d.Dauerlinienwerte = b.Dauerlinienwerte;
                d.Lage = (ZapfprofilSpitzenlage)(int)b.Lage;
                d.BandEinheiten = b.Einheiten;
                d.BandMindestEinheiten = b.MindestEinheiten;
            }
            d.Gesamtampel = (ZapfprofilVergleichsampel)(int)v.Gesamtampel;
            if (v.Streuung is { } s)
            {
                d.StreuungUnten = s.Unten;
                d.StreuungOben = s.Oben;
                d.Streubreite = s.Streubreite;
                d.Realisierungen = s.Realisierungen;
            }
            if (v.WurzelN is { } w)
            {
                d.Einheiten = w.Einheiten;
                d.WurzelNVerhaeltnis = w.WurzelNVerhaeltnis;
                d.Skalierungsmass = w.Skalierungsmass;
            }
            if (v.Form is { } f)
            {
                d.Formmass = f.Formmass;
                d.Formschwelle = f.Schwelle;
                d.FormImRahmen = f.ImRahmen;
                foreach (Tagesgangabweichung t in f.JeTagtyp)
                    d.Form.Add(new ZapfprofilFormabgleichDaten(Tagtypname(t.Tagtyp), t.TageGemessen, t.TageGerechnet,
                                                               t.MittlereAbweichung, t.VerschobenerAnteil,
                                                               t.MittlereAbweichung <= f.Schwelle));
            }
            if (v.Monate is { } m)
            {
                d.MonateGroessteAbweichung = m.GroessteAbweichung;
                d.MonateGroessterMonat = m.GroessterMonat;
            }
            foreach (ZapfSatz h in hinweise) d.Hinweise.Add(Validierungswarnung(h));
            return d;
        }

        /// <summary>
        /// <b>Die Stichprobe der Realisierungsspitzen</b> für die Spitzenstreuung (N15 Gruppe 3):
        /// die Stundenspitzen des Ensembles, wenn <b>genau eine</b> Zone eines trägt. Bei mehreren
        /// Zonen ist sie nicht zu bilden — jede Zone zieht ihre Realisierungen für sich, die Spitze
        /// der Summe ist nicht die Summe der Spitzen; das wird benannt, nicht geschätzt. Leer heißt
        /// für den Kern „kein Ensemble" (<c>MESSVERGLEICH_OHNE_ENSEMBLE</c>).
        ///
        /// <para><paramref name="ensembleZonen"/> trägt die Zahl der tragenden Zonen, <b>wenn es mehr
        /// als eine ist</b>, sonst 0 — sie geht ins DTO. Denn beide Fälle enden für den Kern in einer
        /// leeren Stichprobe, und ein Strich mit „ohne Ensemble" wäre bei mehreren Ensembles der
        /// falsche Grund: Die Rechnung ist stochastisch, nur die Stichprobe nicht bildbar.</para>
        /// </summary>
        private static IReadOnlyList<double> Realisierungsspitzen(ZapfprofilErgebnis e, ICollection<ZapfSatz> hinweise,
                                                                 out int ensembleZonen)
        {
            List<IReadOnlyList<double>> mit = (e?.JeZone ?? new ZonenErgebnis[0])
                .Where(z => !z.Abgelehnt && z.StundenspitzenKw != null && z.StundenspitzenKw.Count > 0)
                .Select(z => z.StundenspitzenKw).ToList();
            ensembleZonen = mit.Count > 1 ? mit.Count : 0;
            if (mit.Count == 1) return mit[0];
            if (mit.Count > 1) hinweise?.Add(ZapfSatz.Neu("MESSVERGLEICH_ENSEMBLE_ZONEN", mit.Count));
            return new double[0];
        }

        private static ZapfprofilMessvergleichDaten Ohne(ZapfprofilMessvergleichDaten d, ZapfSatz grund,
                                                         IEnumerable<ZapfSatz> hinweise = null)
        {
            d.Ok = false;
            d.Kennung = Satzkennung(grund, "ZPG_SATZ_MESSVERGLEICH_OHNE_RECHNUNG");
            d.Abbruch = Satztext(grund);
            foreach (ZapfSatz h in hinweise ?? Enumerable.Empty<ZapfSatz>()) d.Hinweise.Add(Validierungswarnung(h));
            return d;
        }

        private static ZapfprofilMessvergleichDaten OhneText(ZapfprofilMessvergleichDaten d, string kennung, string grund)
        {
            d.Ok = false;
            d.Kennung = kennung ?? "";
            d.Abbruch = grund ?? "";
            return d;
        }

        // =================================================================================
        // „Aus Messreihe kalibrieren" — der Jahresmesswert aus der Reihe
        // =================================================================================

        /// <summary>
        /// <b>Der Jahresmesswert aus der Messreihe</b> (4.1, 4.8): dieselbe Rechnung wie ein von
        /// Hand gepflegter Messwert — die Energie der Reihe, bei einem Teiljahr über den Jahresgang
        /// der Rechnung hochgerechnet und das benannt. Die <b>Bilanzgrenze</b> nimmt die Zone, wenn
        /// sie eine führt, sonst die der Nutzungsart: Sie sagt, was der Zähler gemessen hat, und das
        /// entscheidet nicht die Hülle. Geschrieben wird nichts — der Dialog setzt die Felder und
        /// speichert erst mit seinem OK.
        ///
        /// <para><b>Gerechnet wird nur, wenn es etwas zu rechnen gibt</b>: Die gerechnete Jahresreihe
        /// trägt allein die Hochrechnung eines <b>Teiljahrs</b>. Deckt die Reihe ein ganzes Jahr
        /// (365 ± <c>Messkalibrierung.JAHRESRAND_TAGE</c> Tage), gilt ihre Energie unverändert — dann
        /// läuft der Generator überhaupt nicht. Der Lauf des Teiljahrs läuft <b>nebenläufig</b> wie
        /// der Vergleich (Muster der Jahresreihe, 5.1); <paramref name="abbruch"/> beendet ihn mit
        /// <see cref="OperationCanceledException"/>.</para>
        /// </summary>
        internal static ZapfprofilMesskalibrierungDaten MesswertAusReihe(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                        ZapfprofilStand basis, string reihe, int zone,
                                                                        CancellationToken abbruch)
        {
            var d = new ZapfprofilMesskalibrierungDaten { Reihe = reihe ?? "" };
            if (string.IsNullOrWhiteSpace(reihe)) return OhneWert(d, ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE"));

            var hinweise = new List<ZapfSatz>();
            Messreihe gemessen = TwwMessreihenCtrl.Lesen(idProjekt, reihe, out ZapfSatz fehler, hinweise);
            if (gemessen == null) return OhneWert(d, fehler ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE"), hinweise);

            ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
            ZonenStand z = Zone(stand, zone);
            if (z == null) return OhneWert(d, ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_BEZUGSMENGE"), hinweise);

            ZapfBilanzgrenze grenze = Bilanzgrenze(z);
            // Nur ein Teiljahr wird hochgerechnet - nur dafuer wird die Jahresreihe gerechnet.
            bool teiljahr = Math.Abs(gemessen.Tage - Zapfkalender.TAGE) > Messkalibrierung.JAHRESRAND_TAGE;
            Messwert m = Messkalibrierung.Jahresmesswert(
                gemessen, Spreizung(stand, z), grenze, z.SpeicherverlustKwhJeJahr, Mindesttage(),
                out ZapfSatz grund, hinweise, teiljahr ? GerechneteReihe(idProjekt, stand, abbruch) : null);
            if (m == null) return OhneWert(d, grund ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MENGE"), hinweise);

            d.Ok = true;
            d.Wert = m.WertKwh;
            d.EinheitId = (int)ZapfprofilMesswerteinheit.KwhJeJahr;
            d.BilanzgrenzeId = (int)grenze;
            d.Quelle = Format(Text_("ZPG_KAL_QUELLE", "Messreihe {0}"), gemessen.Bezeichnung);
            d.Zeitraum = m.Zeitraum ?? "";
            d.Hochgerechnet = hinweise.Any(h => h.Kennung.StartsWith("MESSKALIBRIERUNG_HOCHGERECHNET", StringComparison.Ordinal));
            foreach (ZapfSatz h in hinweise) d.Hinweise.Add(Validierungswarnung(h));
            return d;
        }

        private static ZapfprofilMesskalibrierungDaten OhneWert(ZapfprofilMesskalibrierungDaten d, ZapfSatz grund,
                                                                IEnumerable<ZapfSatz> hinweise = null)
        {
            d.Ok = false;
            d.Kennung = Satzkennung(grund, "ZPG_SATZ_MESSKALIBRIERUNG_OHNE_MESSREIHE");
            d.Abbruch = Satztext(grund);
            foreach (ZapfSatz h in hinweise ?? Enumerable.Empty<ZapfSatz>()) d.Hinweise.Add(Validierungswarnung(h));
            return d;
        }

        // =================================================================================
        // Der Kalibriervorschlag einer Nichtwohn-Zone
        // =================================================================================

        /// <summary>
        /// <b>Der Vorschlag als Vorschau</b> (4.8): Tagesbedarf je Einheit, Wochenfaktoren und die
        /// Tagesgänge je Tagtyp aus den vollständigen Tagen der Messung, dazu die Bezeichnung der
        /// Anwenderkopie, die entstehen würde. <b>Geschrieben wird nichts</b> — das tut erst
        /// <see cref="VorschlagUebernehmen"/> nach der Rückfrage.
        /// </summary>
        internal static ZapfprofilVorschlagDaten Kalibriervorschlag(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                    ZapfprofilStand basis, string reihe, int zone)
        {
            var d = new ZapfprofilVorschlagDaten { Reihe = reihe ?? "" };
            if (string.IsNullOrWhiteSpace(reihe)) return OhneVorschlag(d, ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE"));

            var hinweise = new List<ZapfSatz>();
            Messreihe gemessen = TwwMessreihenCtrl.Lesen(idProjekt, reihe, out ZapfSatz fehler, hinweise);
            if (gemessen == null) return OhneVorschlag(d, fehler ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE"), hinweise);

            ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
            ZonenStand z = Zone(stand, zone);
            if (z == null || z.IdNutzungsart <= 0)
                return OhneVorschlag(d, ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_BEZUGSMENGE"), hinweise);

            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(
                gemessen, Spreizung(stand, z), z.Bezugsmenge, Mindesttage(), out ZapfSatz grund, hinweise);
            if (v == null) return OhneVorschlag(d, grund ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_TAGESGANG"), hinweise);

            Nutzungsart n = ZapfprofilCtrl.LiesNutzungsart(z.IdNutzungsart);
            d.Ok = true;
            d.Vorlage = n == null ? "" : Kopiename(n.Name, n.Katalogversion);
            d.Kopie = n == null ? "" : Kopiename(n.Name, TwwNutzungsartCtrl.FreieKopieversion(z.IdNutzungsart));
            d.TagesbedarfKwh = v.TagesbedarfKwh;
            d.TagesbedarfJeEinheitKwh = v.TagesbedarfJeEinheitKwh;
            d.Bezugsmenge = v.Bezugsmenge;
            d.VolleTage = v.VolleTage;
            d.Wochenfaktoren = (v.Wochenfaktoren ?? new double[0]).ToList();
            foreach (Tagesgangvorschlag g in v.Tagesgaenge ?? new Tagesgangvorschlag[0])
                d.Tagesgaenge.Add(new ZapfprofilVorschlagsgangDaten(Tagtypname(g.Tagtyp), g.Tage,
                                                                   (g.Anteile ?? new double[0]).ToList()));
            foreach (ZapfSatz h in hinweise) d.Hinweise.Add(Validierungswarnung(h));
            return d;
        }

        private static ZapfprofilVorschlagDaten OhneVorschlag(ZapfprofilVorschlagDaten d, ZapfSatz grund,
                                                              IEnumerable<ZapfSatz> hinweise = null)
        {
            d.Ok = false;
            d.Kennung = Satzkennung(grund, "ZPG_SATZ_MESSKALIBRIERUNG_OHNE_TAGESGANG");
            d.Abbruch = Satztext(grund);
            foreach (ZapfSatz h in hinweise ?? Enumerable.Empty<ZapfSatz>()) d.Hinweise.Add(Validierungswarnung(h));
            return d;
        }

        /// <summary>
        /// <b>Die Übernahme des Vorschlags</b> (<see cref="TwwNutzungsartCtrl.VorschlagUebernehmen"/>):
        /// EIN Vorgang legt die Anwenderkopie samt eigenem Tagesgangsatz und den Zapfkategorien der
        /// Vorlage an; die Vorlage bleibt unberührt — auch eine freie (K7). Die Bezugstemperaturen
        /// der Kopie sind die der Zone, damit sie die Messenergie mit Temperaturfaktor 1 wiedergibt.
        /// Der Aufrufer stellt die Zone danach auf die neue Id um.
        /// </summary>
        internal static ZapfprofilVorschlagErgebnisDaten VorschlagUebernehmen(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                             ZapfprofilStand basis, string reihe, int zone)
        {
            var e = new ZapfprofilVorschlagErgebnisDaten();
            ZapfprofilVorschlagDaten vorschau = Kalibriervorschlag(idProjekt, eingabe, basis, reihe, zone);
            if (!vorschau.Ok)
            {
                e.Abbruch = vorschau.Abbruch;
                return e;
            }

            var hinweise = new List<ZapfSatz>();
            Messreihe gemessen = TwwMessreihenCtrl.Lesen(idProjekt, reihe, out ZapfSatz fehler, hinweise);
            ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
            ZonenStand z = Zone(stand, zone);
            if (gemessen == null || z == null || z.IdNutzungsart <= 0)
            {
                e.Abbruch = Satztext(fehler ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MESSREIHE"));
                return e;
            }

            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(
                gemessen, Spreizung(stand, z), z.Bezugsmenge, Mindesttage(), out ZapfSatz grund, hinweise);
            if (v == null)
            {
                e.Abbruch = Satztext(grund ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_TAGESGANG"));
                return e;
            }

            TwwKatalogErgebnis erg = TwwNutzungsartCtrl.VorschlagUebernehmen(
                z.IdNutzungsart, v, gemessen.Bezeichnung, null, Temperaturen(stand, z));
            if (!erg.Ok)
            {
                e.Abbruch = Text_(KategorienSchluessel(erg.Ausgang), erg.Ausgang.ToString());
                return e;
            }

            Nutzungsart neu = ZapfprofilCtrl.LiesNutzungsart(erg.Id);
            e.Ok = true;
            e.IdNutzungsart = erg.Id;
            // Die Hinweise gehoeren zu den Werten der Kopie: Der Dialog haelt sie in der Warnliste,
            // auch wenn die Vorschau mit der Uebernahme zugeht.
            foreach (ZapfSatz h in hinweise) e.Hinweise.Add(Validierungswarnung(h));
            e.Kopie = neu == null ? vorschau.Kopie : Kopiename(neu.Name, neu.Katalogversion);
            e.Meldung = Format(Text_("ZPG_STATUS_VORSCHLAG", "Kalibrierte Kopie angelegt: {0}"), e.Kopie);
            return e;
        }

        // =================================================================================
        // Kleinkram: Zone, Spreizung, Einheiten, Namen
        // =================================================================================

        /// <summary>Die Zone an der Stelle <paramref name="index"/> des Arbeitsstands; <c>null</c> außerhalb.</summary>
        private static ZonenStand Zone(ZapfprofilStand stand, int index)
        {
            IReadOnlyList<ZonenStand> zonen = stand?.Zonen;
            return zonen != null && index >= 0 && index < zonen.Count ? zonen[index] : null;
        }

        /// <summary>
        /// Die Spreizung θ_Zapf − θ̄_KW [K] EINER Zone — sie wirkt <b>nur bei einer Volumenreihe</b>.
        /// Genommen werden die Temperaturen der Zone; führt sie keine, die Bezugstemperaturen ihrer
        /// Nutzungsart. Bleibt sie ≤ 0, lehnt der Kern die Volumenreihe benannt ab
        /// (<c>MESSVERGLEICH_SPREIZUNG_FEHLT</c>) — die Hülle rät nicht. Für den Vergleich über alle
        /// Zonen gilt <see cref="Gesamtspreizung"/>.
        /// </summary>
        private static double Spreizung(ZapfprofilStand stand, ZonenStand z)
        {
            ZonenStand zone = z;
            if (zone == null) return 0.0;
            Temperaturbezug bezug = null;
            if (zone.IdNutzungsart > 0)
                bezug = ZapfprofilCtrl.LiesNutzungsart(zone.IdNutzungsart)?.Bezugstemperaturen;
            double zapf = zone.ZapftemperaturC ?? bezug?.ZapftemperaturC ?? 0.0;
            double kalt = zone.KaltwasserMittelC ?? bezug?.KaltwasserC ?? 0.0;
            return zapf - kalt;
        }

        /// <summary>
        /// Wie weit zwei Spreizungen auseinanderliegen dürfen, um als gleich zu gelten [K] — eine
        /// Rundung der Eingabe ist kein Unterschied der Temperaturen.
        /// </summary>
        private const double SPREIZUNG_GLEICH = 1e-9;

        /// <summary>
        /// <b>Die Spreizung des ganzen Vergleichs</b> [K]: Der Kern nimmt EINE Spreizung, der
        /// Arbeitsstand kann mehrere Zonen mit verschiedenen Temperaturen führen. Genommen wird das
        /// <b>mengengewichtete Mittel</b> über die Zonen (Gewicht ist die Bezugsmenge — sie sagt,
        /// welcher Anteil der gemessenen Menge aus welcher Zone kommt); ohne jede Bezugsmenge das
        /// ungewichtete Mittel.
        ///
        /// <para>Die Spreizung wirkt <b>nur bei einer Volumenreihe</b> (dort rechnet sie m³ in kWh).
        /// Führen dann mehrere Zonen VERSCHIEDENE Temperaturen, ist das Mittel eine Annahme — sie
        /// wird benannt (<c>MESSVERGLEICH_SPREIZUNG_ZONEN</c>), nie still genommen. Bei einer
        /// Energie- oder Leistungsreihe ist die Spreizung ohne Wirkung; dann steht kein Hinweis.</para>
        /// </summary>
        private static double Gesamtspreizung(ZapfprofilStand stand, bool volumenreihe, ICollection<ZapfSatz> hinweise)
        {
            IReadOnlyList<ZonenStand> zonen = stand?.Zonen;
            if (zonen == null || zonen.Count == 0) return 0.0;
            if (zonen.Count == 1) return Spreizung(stand, zonen[0]);

            double erste = Spreizung(stand, zonen[0]);
            bool verschieden = false;
            double menge = 0.0, gewichtet = 0.0, summe = 0.0;
            foreach (ZonenStand z in zonen)
            {
                double s = Spreizung(stand, z);
                double m = z.Bezugsmenge > 0.0 ? z.Bezugsmenge : 0.0;
                menge += m;
                gewichtet += s * m;
                summe += s;
                if (Math.Abs(s - erste) > SPREIZUNG_GLEICH) verschieden = true;
            }
            double mittel = menge > 0.0 ? gewichtet / menge : summe / zonen.Count;
            if (verschieden && volumenreihe)
                hinweise?.Add(ZapfSatz.Neu("MESSVERGLEICH_SPREIZUNG_ZONEN", zonen.Count, mittel));
            return mittel;
        }

        /// <summary>Die Bezugstemperaturen der Kopie: die der Zone, sonst die der Vorlage (dann <c>null</c>).</summary>
        private static Temperaturbezug Temperaturen(ZapfprofilStand stand, ZonenStand z)
            => z?.ZapftemperaturC is double zapf && z.KaltwasserMittelC is double kalt
                ? new Temperaturbezug(zapf, kalt) : null;

        /// <summary>
        /// Die Summe der Einheiten aller Zonen (N der √N-Skalierung), kaufmännisch gerundet; 0 =
        /// unbekannt, dann nennt der Kern <c>MESSVERGLEICH_OHNE_EINHEITEN</c>.
        /// </summary>
        private static int Einheiten(ZapfprofilStand stand)
        {
            double summe = stand?.Zonen?.Sum(z => z.Bezugsmenge) ?? 0.0;
            if (!(summe > 0.0) || summe > int.MaxValue) return 0;
            return (int)Math.Round(summe, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Die Bilanzgrenze des Messwerts: die der Zone, sonst die ihrer Nutzungsart, sonst „an der
        /// Zapfstelle". Sie sagt, was der Zähler gemessen hat.
        /// </summary>
        private static ZapfBilanzgrenze Bilanzgrenze(ZonenStand z)
        {
            if (z?.JahresmesswertBilanzgrenze is ZapfBilanzgrenze g) return g;
            if (z != null && z.IdNutzungsart > 0
                && ZapfprofilCtrl.LiesNutzungsart(z.IdNutzungsart) is { } n) return n.Grenze;
            return ZapfBilanzgrenze.Zapfstelle;
        }

        /// <summary>
        /// Die gerechnete Jahresreihe zum Arbeitsstand für die Hochrechnung eines Teiljahrs;
        /// <c>null</c>, wenn sie nicht zu rechnen ist — dann rechnet die Kalibrierung flach und
        /// nennt den Bias. <paramref name="abbruch"/> reicht bis in den Kern; ein Abbruch verlässt die
        /// Hülle als <see cref="OperationCanceledException"/> — ein abgebrochener Lauf ist keine
        /// flache Hochrechnung.
        /// </summary>
        private static Bilanzreihe GerechneteReihe(int idProjekt, ZapfprofilStand stand, CancellationToken abbruch)
        {
            try
            {
                if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we)) return null;
                ZapfprofilErgebnis e = ZapfprofilCtrl.Rechnen(idProjekt, stand, jan1, we, abbruch);
                return Bilanzreihe.Summe(new[] { e.Zapfung, e.Zirkulation }.Where(r => r != null));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is ZapfprofilEingabeException || ex is ParametersatzException
                                       || ex is ZapfAuslegungException)
            {
                return null;
            }
        }

        /// <summary>Der Name einer Katalogzeile samt Katalogversion — „Name · Version".</summary>
        private static string Kopiename(string name, string version)
            => string.IsNullOrWhiteSpace(version) ? name ?? "" : (name ?? "") + " · " + version;

        /// <summary>Der Tagtyp in der Oberflächensprache (<c>ZPG_TAGTYP_…</c>).</summary>
        internal static string Tagtypname(ZapfTagtyp t)
        {
            switch (t)
            {
                case ZapfTagtyp.Samstag: return Text_("ZPG_TAGTYP_SAMSTAG", "Samstag");
                case ZapfTagtyp.SonnFeiertag: return Text_("ZPG_TAGTYP_SONNTAG", "Sonn-/Feiertag");
                case ZapfTagtyp.Ruhetag: return Text_("ZPG_TAGTYP_RUHETAG", "Ruhetag");
                default: return Text_("ZPG_TAGTYP_WERKTAG", "Werktag");
            }
        }

        /// <summary>Die Schwelle des Formabgleichs [-] aus dem Parametersatz; ohne Satz die Vorgabe des Eingangs.</summary>
        private static double Formschwelle()
        {
            var vorgabe = new Messvergleichseingang();
            try
            {
                Parametersatz p = ZapfprofilCtrl.Parameter();
                return p != null && p.Enthaelt(ZapfParameter.VALIDIERUNG_FORMSCHWELLE)
                    ? p.Wert(ZapfParameter.VALIDIERUNG_FORMSCHWELLE) : vorgabe.Formschwelle;
            }
            catch (ParametersatzException) { return vorgabe.Formschwelle; }
        }
    }
}
