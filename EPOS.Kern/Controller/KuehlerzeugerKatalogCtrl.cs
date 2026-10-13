using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kälteerzeuger der Katalogauswahl der Kältemaschine</b> (KA‑E‑26, Stufe KM‑K1; Konzept Projektdialoge mit
    /// Katalogauswahl 5.10a): Kältemaschinen und Wärmepumpen mit Kühlkennlinie in EINER Katalog- und EINER Projektliste mit
    /// der Spalte „Art“. Dialogfrei und plattformfrei; die Hülle <c>KaeltemaschineAnlageHuelle</c> reicht die Wege in den
    /// Dialog.
    ///
    /// <para><b>Katalog.</b> Die Zeilen der Kältemaschinen kommen unverändert aus
    /// <see cref="KaeltemaschineStammCtrl.Katalogfilterzeilen"/> (Schlüssel = ID), die der Wärmepumpen aus
    /// <c>Tab_WP_STAMM</c> mit mindestens einer Zeile in <c>Tab_Kenndaten_Kuehlung_STAMM</c> (Schlüssel
    /// <see cref="PRAEFIX_WP"/> + ID). Kühlleistung und EER einer Katalog-Wärmepumpe stehen am festen Bezugspunkt
    /// (<see cref="Bezugspunkt"/>).</para>
    ///
    /// <para><b>Projekt.</b> Eine Wärmepumpe wird über <see cref="WaermepumpeUebernehmen"/> Projektwärmepumpe (Kopie und
    /// Anlagenzeile wie der Anlegeweg des Wärmepumpendialogs); ihr Kühlbetrieb geht allein über
    /// <see cref="WPCtrl.KuehlkonfigurationSchreiben"/> (<see cref="KuehlbetriebSetzen"/>), der Rang in der Kältefolge über
    /// <see cref="KaeltefolgeCtrl.RangAnhaengen"/> und <see cref="KaeltefolgeCtrl.RangEntfernen"/>.</para>
    /// </summary>
    public static class KuehlerzeugerKatalogCtrl
    {
        /// <summary>Vorsilbe des Katalogschlüssels einer Wärmepumpe (die Kältemaschine behält ihre ID als Schlüssel).</summary>
        public const string PRAEFIX_WP = "WP";

        /// <summary>Spaltenschlüssel „Art“ (Kältemaschine / Wärmepumpe).</summary>
        public const string SpArt = "ART";

        /// <summary>Schlüssel des Profils (Filterstand) der gemeinsamen Liste.</summary>
        public const string PROFIL = "KUEHLERZEUGER";

        /// <summary>Bezugspunkt der Katalog-Wärmepumpe: Kaltwasser-Vorlauf [°C] (EN 14511, A35/W7 bzw. W30/W7).</summary>
        public const int BEZUG_KALTWASSER_C = 7;

        /// <summary>Bezugspunkt: Quelltemperatur einer Luft-Wärmepumpe [°C].</summary>
        public const double BEZUG_QUELLE_LUFT_C = 35;

        /// <summary>Bezugspunkt: Quelltemperatur einer Sole- oder Wasser-Wärmepumpe [°C].</summary>
        public const double BEZUG_QUELLE_WASSER_C = 30;

        /// <summary>
        /// Ein Kennwert der Kühlkennlinie: Kühlleistung [kW] und EER am Kaltwasser-Vorlauf <paramref name="KaltwasserC"/>
        /// und der Quelltemperatur <paramref name="QuelleC"/>; <paramref name="Rueckfall"/> = die Kennlinie deckt 7 °C nicht
        /// ab, der Wert steht an der kleinsten Kaltwasser-Stützstelle.
        /// </summary>
        public sealed record Bezugswert(double KuehlleistungKw, double Eer, int KaltwasserC, double QuelleC, bool Rueckfall);

        /// <summary>Die Quelltemperatur des Bezugspunkts zur Bauart (<c>Tab_WP.Typ</c>).</summary>
        public static double QuelleBezug(string bauart)
            => string.Equals((bauart ?? "").Trim(), DbWerte.WP_BAUART_LUFT_WASSER, StringComparison.OrdinalIgnoreCase)
                ? BEZUG_QUELLE_LUFT_C : BEZUG_QUELLE_WASSER_C;

        /// <summary>
        /// <b>Der Bezugspunkt einer Kühlkennlinie</b> (Anwenderentscheid 12.10.2026, offener Punkt 4): 7 °C Kaltwasser bei
        /// der Quelle nach EN 14511, über die Kennlinie interpoliert (<see cref="Kuehlkennlinie.Bilden(IEnumerable{KuehlkennlinienZeile}, int?, bool)"/>);
        /// deckt die Kennlinie 7 °C nicht ab, der Wert an der kleinsten Kaltwasser-Stützstelle mit <see cref="Bezugswert.Rueckfall"/>.
        /// Mit <paramref name="kuehlVorlauf"/> (Projektkopie) gilt deren Kühlvorlauf wie bisher (NULL = kleinste Stützstelle).
        /// <c>null</c> = keine rechenbare Kennlinie.
        /// </summary>
        public static Bezugswert Bezugspunkt(IReadOnlyList<KuehlkennlinienZeile> zeilen, string bauart,
                                             bool projekt = false, int? kuehlVorlauf = null)
        {
            if (zeilen == null || zeilen.Count == 0) return null;
            double quelle = QuelleBezug(bauart);
            Kuehlkennlinie k;
            bool rueckfall = false;
            if (projekt)
                k = Kuehlkennlinie.Bilden(zeilen, kuehlVorlauf, true);
            else
            {
                List<int> kalt = zeilen.Select(z => z.Vorlauf).Where(v => v < Kuehlkennlinie.KALTWASSER_VORLAUF_GRENZE)
                                       .Distinct().OrderBy(v => v).ToList();
                bool gedeckt = kalt.Count > 0 && kalt[0] <= BEZUG_KALTWASSER_C && kalt[kalt.Count - 1] >= BEZUG_KALTWASSER_C;
                k = Kuehlkennlinie.Bilden(zeilen, gedeckt ? BEZUG_KALTWASSER_C : (int?)null, true);
                rueckfall = !gedeckt;
            }
            if (k == null || k.Leer) return null;
            KennlinienPunkt p = k.Auswerten(quelle, false);
            int vorlauf = projekt || rueckfall ? k.Vorlauf : BEZUG_KALTWASSER_C;
            return new Bezugswert(p.Pkuehl, p.Eer, vorlauf, quelle, rueckfall);
        }

        /// <summary>Der Bezugspunkt eines Katalogsatzes der Wärmepumpe (<c>Tab_WP_STAMM.ID</c>).</summary>
        public static Bezugswert BezugspunktStamm(int idStammWp)
        {
            if (idStammWp <= 0) return null;
            object typ = StilleDb.Scalar("SELECT Typ FROM Tab_WP_STAMM WHERE ID = ?",
                                         StilleDb.Par("@id", DbParamTyp.Integer, idStammWp));
            return Bezugspunkt(KenndatenKuehlungCtrl.ZeilenStamm(idStammWp), typ as string);
        }

        /// <summary>Der Katalogschlüssel einer Wärmepumpe.</summary>
        public static string WpSchluessel(int idStammWp) => PRAEFIX_WP + idStammWp.ToString(CultureInfo.InvariantCulture);

        /// <summary>Ist der Schlüssel der einer Wärmepumpe? Liefert dann die Stamm‑ID.</summary>
        public static bool IstWaermepumpe(string schluessel, out int idStammWp)
        {
            idStammWp = 0;
            return schluessel != null && schluessel.StartsWith(PRAEFIX_WP, StringComparison.Ordinal)
                && int.TryParse(schluessel.Substring(PRAEFIX_WP.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out idStammWp)
                && idStammWp > 0;
        }

        /// <summary>Der Anzeigetext der Art.</summary>
        public static string ArtText(bool waermepumpe)
            => waermepumpe ? MyResource.Resource.KMA_ART_WAERMEPUMPE : MyResource.Resource.KMA_ART_KAELTEMASCHINE;

        /// <summary>
        /// <b>Das Profil der gemeinsamen Liste</b>: „Art“ vorn, dann die Spalten der Kältemaschine, dazu „Quelle“ (nur
        /// Wärmepumpe) und die Verwendungsspalte. Spalten, die eine Art nicht kennt, bleiben leer.
        /// </summary>
        public static Katalogfilterprofil Profil(Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);
            var spalten = new List<Katalogspalte> { new Katalogspalte(SpArt, MyResource.Resource.KMA_SP_ART) };
            foreach (Katalogspalte s in Katalogfilterprofil.Finde(Anlagenart.Kaeltemaschine, text).Spalten)
            {
                spalten.Add(s);
                if (s.Schluessel == Katalogfilterprofil.SpRueckkuehlart)
                    spalten.Add(new Katalogspalte(Katalogfilterprofil.SpQuelle, t("KFLT_SP_QUELLE"), rang: Katalogspaltenrang.BeiPlatz));
            }
            return Katalogfilterprofil.AusSpalten(PROFIL, spalten).MitVerwendungsspalte(text);
        }

        /// <summary>
        /// <b>Die Katalogzeilen beider Arten</b>: alle Kältemaschinen, dazu jede Katalog-Wärmepumpe mit Kühlkennlinie
        /// (Kühlleistung und EER am Bezugspunkt). Eine Wärmepumpe ohne Kühlkennlinie steht nicht in der Liste.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogzeilen()
        {
            var zeilen = new List<Katalogfilterzeile>();
            foreach (Katalogfilterzeile z in KaeltemaschineStammCtrl.Katalogfilterzeilen())
                zeilen.Add(z.MitText(SpArt, ArtText(false)));
            DataTable dt = StilleDb.Tabelle(
                "SELECT w.ID, w.Bezeichner, w.Firma, w.Typ, w.ReadOnly FROM Tab_WP_STAMM w " +
                "WHERE EXISTS (SELECT 1 FROM Tab_Kenndaten_Kuehlung_STAMM k WHERE k.ID_WP = w.ID) ORDER BY w.Bezeichner");
            if (dt == null) return zeilen;
            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (id <= 0) continue;
                string name = StilleDb.Feld(r, "Bezeichner") as string ?? "";
                string typ = StilleDb.Feld(r, "Typ") as string ?? "";
                Bezugswert b = Bezugspunkt(KenndatenKuehlungCtrl.ZeilenStamm(id), typ);
                var zeile = new Katalogfilterzeile(id, name)
                {
                    Geschuetzt = StilleDb.Zahl(StilleDb.Feld(r, "ReadOnly")) != 0,
                    Schluessel = WpSchluessel(id)
                };
                zeilen.Add(zeile
                    .MitText(SpArt, ArtText(true))
                    .MitText(Katalogfilterprofil.SpBezeichner, name)
                    .MitText(Katalogfilterprofil.SpHersteller, StilleDb.Feld(r, "Firma") as string ?? "")
                    .MitText(Katalogfilterprofil.SpTyp, typ)
                    .MitText(Katalogfilterprofil.SpQuelle, typ)
                    .MitZahl(Katalogfilterprofil.SpNennkaelteleistung, b?.KuehlleistungKw, 1)
                    .MitZahl(Katalogfilterprofil.SpEer, b?.Eer, 2));
            }
            return zeilen;
        }

        // =====================================================================
        //  Projekt
        // =====================================================================

        /// <summary>
        /// Eine Projektwärmepumpe im Kühlbetrieb — eine Zeile je Projektkopie (offener Punkt 5): ihre Anlagen, Rang (der
        /// kleinste ihrer Anlagen), Kühlvorlauf, Kühlträger und Kennwerte am Kühlvorlauf.
        /// </summary>
        public sealed record ProjektWaermepumpe(int IdWp, int? IdStamm, string Bezeichner, string Firma, string Bauart,
                                                IReadOnlyList<int> AnlagenIds, int? Rang, int? KuehlVorlauf,
                                                double? Hilfsstromanteil, string Kuehltraeger, bool EigenerZaehler,
                                                Bezugswert Kennwert);

        /// <summary>
        /// <b>Die Wärmepumpen des Projekts im Kühlbetrieb</b>, gelesen über <see cref="Kaeltefolge.Lesen"/> (dieselbe
        /// Auswahl wie Lauf und Bereich „Kälte“), je Projektkopie eine Zeile. Leer bei jedem Fehler.
        /// </summary>
        public static IReadOnlyList<ProjektWaermepumpe> ProjektWaermepumpen(int idProjekt)
        {
            var liste = new List<ProjektWaermepumpe>();
            if (idProjekt <= 0) return liste;
            List<KaelteerzeugerEintrag> wp;
            try
            {
                wp = Kaeltefolge.Lesen(idProjekt).Erzeuger
                    .Where(e => e.Art == KaeltefolgeStufe.Waermepumpe && e.Kuehlbetrieb && e.IdGeraet > 0).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Lesen der Kälteerzeuger: " + ex.Message);
                return liste;
            }
            foreach (IGrouping<int, KaelteerzeugerEintrag> g in wp.GroupBy(e => e.IdGeraet))
            {
                KaelteerzeugerEintrag e = g.First();
                DataTable dt = StilleDb.Tabelle("SELECT Bezeichner, Firma, Typ, ID_Stamm, Kuehl_Vorlauf FROM Tab_WP WHERE ID = ?",
                                                StilleDb.Par("@id", DbParamTyp.Integer, g.Key));
                DataRow r = dt != null && dt.Rows.Count > 0 ? dt.Rows[0] : null;
                string bauart = r == null ? "" : StilleDb.Feld(r, "Typ") as string ?? "";
                object vor = r == null ? null : StilleDb.Feld(r, "Kuehl_Vorlauf");
                int? kuehlVorlauf = vor == null ? (int?)null : StilleDb.Zahl(vor);
                object stamm = r == null ? null : StilleDb.Feld(r, "ID_Stamm");
                List<int?> raenge = g.Select(x => x.KaelteRang).Where(x => x.HasValue).ToList();
                liste.Add(new ProjektWaermepumpe(
                    g.Key, stamm == null ? (int?)null : StilleDb.Zahl(stamm),
                    r == null ? e.Bezeichner : StilleDb.Feld(r, "Bezeichner") as string ?? e.Bezeichner,
                    r == null ? "" : StilleDb.Feld(r, "Firma") as string ?? "", bauart,
                    g.Select(x => x.AnlagenId).ToList(), raenge.Count == 0 ? null : raenge.Min(),
                    kuehlVorlauf, e.Hilfsstromanteil, e.KuehltraegerName ?? "", e.EigenerZaehler,
                    Bezugspunkt(KenndatenKuehlungCtrl.ZeilenProjekt(g.Key), bauart, true, kuehlVorlauf)));
            }
            return liste;
        }

        /// <summary>Der Rang einer Kältemaschinen-Anlage in der Kältefolge; <c>null</c> = kein Rang.</summary>
        public static IReadOnlyDictionary<int, int> Raenge(int idProjekt) => Kaeltefolge.RaengeLesen(idProjekt);

        /// <summary>
        /// Das Ergebnis von <see cref="WaermepumpeUebernehmen"/>: die Projektkopie, ob sie neu ist, die neu angelegten
        /// Anlagenzeilen, ob das Gerät schon im Projekt stand, und die Meldung (Ablehnung des Kühlbetriebs oder Fehler).
        /// </summary>
        public sealed record Uebernahme(bool Ok, int IdWp, string Bezeichner, bool KopieNeu, IReadOnlyList<int> NeueAnlagen,
                                        bool SchonImProjekt, string Meldung, string Hinweis = null);

        /// <summary>
        /// <b>Eine Katalog-Wärmepumpe ins Projekt übernehmen und kühlen lassen</b> (KA‑E‑26): Die Projektkopie entsteht
        /// über <see cref="WPCtrl.CopyFromStamm(int, int)"/> (idempotent), die Anlagenzeile über den einen Schreibweg der
        /// Erzeuger (<see cref="WizardCtrl.Add_WP_Waermeerzeuger"/>) mit den Vorbelegungen des Wärmepumpendialogs
        /// (Stromträger, Gerätedaten, Vorlauf aus den Kennlinien; <paramref name="vorbelegen"/> für das, was die Hülle
        /// beisteuert). Steht das Gerät schon mit einer Anlage im Projekt, entsteht keine zweite Kopie und keine zweite
        /// Anlage. Danach schaltet <see cref="KuehlbetriebSetzen"/> den Kühlbetrieb ein und hängt den Rang an; eine
        /// Ablehnung steht in <see cref="Uebernahme.Meldung"/> (Kopie und Anlage bleiben, der Dialog merkt sie vor).
        /// </summary>
        public static Uebernahme WaermepumpeUebernehmen(int idProjekt, int idStamm, Action<WErzeugerModel> vorbelegen = null)
        {
            if (idProjekt <= 0 || idStamm <= 0)
                return new Uebernahme(false, 0, "", false, Array.Empty<int>(), false, MyResource.Resource.KMA_MSG_WP_FEHLER);
            var ctrl = new WPCtrl();
            bool kopieDa = ctrl.GetProjektIdZuStamm(idStamm, idProjekt) > 0;
            int kopie = ctrl.CopyFromStamm(idStamm, idProjekt);
            if (kopie <= 0)
                return new Uebernahme(false, 0, "", false, Array.Empty<int>(), false, MyResource.Resource.KMA_MSG_WP_FEHLER);
            string name = Convert.ToString(StilleDb.Scalar("SELECT Bezeichner FROM Tab_WP WHERE ID = ?",
                                           StilleDb.Par("@id", DbParamTyp.Integer, kopie)), CultureInfo.InvariantCulture) ?? "";
            List<int> anlagen = AnlagenDerKopie(idProjekt, kopie);
            bool schonDa = anlagen.Count > 0;
            var neu = new List<int>();
            if (!schonDa)
            {
                var modell = new WErzeugerModel
                {
                    Bezeichner = name,
                    ID_Type = WizardItemClass.WP_TYP,
                    ID_Projekt = idProjekt,
                    ID_WP = kopie
                };
                try { modell.ID_Carrier = EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_WAERMEPUMPE, 0, idProjekt); }
                catch { }
                WaermepumpeGeraeteCtrl.GeraetedatenFuellen(modell, kopie);
                AnlagenTemperaturen.VorlaufAusKennlinien(modell);
                vorbelegen?.Invoke(modell);
                if (!new WizardCtrl().Add_WP_Waermeerzeuger(idProjekt, new List<WErzeugerModel> { modell }))
                    return new Uebernahme(false, kopie, name, !kopieDa, Array.Empty<int>(), false, MyResource.Resource.KMA_MSG_WP_FEHLER);
                neu = AnlagenDerKopie(idProjekt, kopie);
            }
            string grund = KuehlbetriebSetzen(idProjekt, kopie, true, neueKopie: !kopieDa);
            return new Uebernahme(grund == null, kopie, name, !kopieDa, neu, schonDa, grund,
                                  grund == null ? WaermekaskadeHinweis(idProjekt) : null);
        }

        /// <summary>
        /// <b>Der Hinweis zur Wärmekaskade</b> (KM‑K2): Eine Wärmepumpe kühlt nur, wenn die Erzeugerart auf einem
        /// Platz der Wärmekaskade steht (<c>Tab_Einstellungen.Tool_1…4</c>, <see cref="Kaeltefolge"/>:
        /// <c>InWaermekaskade</c>). Die Übernahme einer Wärmepumpe legt — aus dem Kältemaschinen-Dialog wie aus dem
        /// Wärmepumpendialog — KEINEN Kaskadenplatz an: Den Platz vergibt allein die Simulationskonfiguration
        /// (nachgezogen wird nur der Heizkessel, <c>KonfigurationCtrl</c>). Statt das still zu übergehen, nennt dieser
        /// Weg es; den Platz setzt der Anwender dort, wo die Kaskade gepflegt wird.
        /// <c>null</c> = die Kaskade führt einen Platz für Wärmepumpen (oder es gibt kein Projekt).
        /// </summary>
        public static string WaermekaskadeHinweis(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (KonfigurationCtrl.KaskadeLesen(idProjekt).Contains(DbWerte.ERZEUGER_WAERMEPUMPE)) return null;
            }
            catch { return null; }
            return MyResource.Resource.KMA_MSG_WP_OHNE_KASKADE;
        }

        /// <summary>
        /// <b>Die Namen der Wärmepumpen-Projektkopien</b> (KM‑K2) — die Verwendungsmarke „im Projekt verwendet" steht
        /// je Projektkopie (<c>Tab_WP</c>), nicht erst nach eingeschaltetem Kühlbetrieb; dasselbe Maß nimmt die
        /// Projektliste des Wärmepumpendialogs. Leer bei jedem Fehler.
        /// </summary>
        public static IReadOnlyList<string> ProjektkopienNamen(int idProjekt)
        {
            var namen = new List<string>();
            if (idProjekt <= 0) return namen;
            DataTable dt = StilleDb.Tabelle("SELECT Bezeichner FROM Tab_WP WHERE ID_Projekt = ?",
                                            StilleDb.Par("@p", DbParamTyp.Integer, idProjekt));
            if (dt == null) return namen;
            foreach (DataRow r in dt.Rows)
                if (StilleDb.Feld(r, "Bezeichner") is string n && n.Length > 0) namen.Add(n);
            return namen;
        }

        /// <summary>Die Wärmepumpen-Anlagen des Projekts mit dieser Projektkopie.</summary>
        public static List<int> AnlagenDerKopie(int idProjekt, int idWp)
        {
            var liste = new List<int>();
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_WP = ? AND ID_Type = ? ORDER BY ID",
                StilleDb.Par("@p", DbParamTyp.Integer, idProjekt), StilleDb.Par("@wp", DbParamTyp.Integer, idWp),
                StilleDb.Par("@t", DbParamTyp.Integer, WizardItemClass.WP_TYP));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows) liste.Add(StilleDb.Zahl(StilleDb.Feld(r, "ID")));
            return liste;
        }

        /// <summary>
        /// <b>Den Kühlbetrieb einer Projektwärmepumpe schalten</b> — allein über
        /// <see cref="WPCtrl.KuehlkonfigurationSchreiben"/>; Kühlvorlauf und Hilfsstromanteil bleiben, eine eben angelegte
        /// Kopie (<paramref name="neueKopie"/>) schreibt beide leer (kleinste Stützstelle, kein Zuschlag). Beim Einschalten
        /// hängt der Rang ihrer Anlagen hinter die Erzeuger mit Rang (<see cref="KaeltefolgeCtrl.RangAnhaengen"/>), beim
        /// Ausschalten wird er entfernt und die Folge geschlossen. <c>null</c> = geschrieben, sonst der Grund.
        /// </summary>
        public static string KuehlbetriebSetzen(int idProjekt, int idWp, bool an, bool neueKopie = false)
        {
            if (idProjekt <= 0 || idWp <= 0) return MyResource.Resource.KMA_MSG_WP_FEHLER;
            int? vorlauf = null;
            double? hilfs = null;
            if (!neueKopie)
            {
                DataTable dt = StilleDb.Tabelle("SELECT Kuehl_Vorlauf, Kuehl_Hilfsstromanteil FROM Tab_WP WHERE ID = ?",
                                                StilleDb.Par("@id", DbParamTyp.Integer, idWp));
                if (dt != null && dt.Rows.Count > 0)
                {
                    object v = StilleDb.Feld(dt.Rows[0], "Kuehl_Vorlauf"), h = StilleDb.Feld(dt.Rows[0], "Kuehl_Hilfsstromanteil");
                    vorlauf = v == null ? (int?)null : StilleDb.Zahl(v);
                    hilfs = h == null ? (double?)null : StilleDb.Kommazahl(h);
                }
            }
            WPCtrl.SpeicherErgebnis e = WPCtrl.KuehlkonfigurationSchreiben(idWp, idProjekt, an, vorlauf, hilfs);
            if (!e.Ok) return string.IsNullOrEmpty(e.Meldung) ? MyResource.Resource.KMA_MSG_WP_FEHLER : e.Meldung;
            List<int> anlagen = AnlagenDerKopie(idProjekt, idWp);
            return an ? KaeltefolgeCtrl.RangAnhaengen(idProjekt, anlagen) : KaeltefolgeCtrl.RangEntfernen(idProjekt, anlagen);
        }

        /// <summary>
        /// Nimmt eine Übernahme zurück (Abbrechen des Dialogs): die neu angelegten Anlagen samt Kostenpositionen und eine
        /// neu angelegte Projektkopie samt Kennlinien. Eine schon vorhandene Kopie behält ihr Gerät; ihren Kühlbetrieb stellt
        /// der Dialog über <see cref="KuehlbetriebSetzen"/> zurück.
        /// </summary>
        public static void UebernahmeZuruecknehmen(int idProjekt, Uebernahme u)
        {
            if (u == null || idProjekt <= 0) return;
            var wizard = new WizardCtrl();
            foreach (int id in u.NeueAnlagen ?? Array.Empty<int>())
                if (id > 0) wizard.Del_Projekt_ID_Waermeerzeuger(idProjekt, id);
            KaeltefolgeCtrl.RangEntfernen(idProjekt, u.NeueAnlagen ?? Array.Empty<int>());
            if (u.KopieNeu && u.IdWp > 0 && AnlagenDerKopie(idProjekt, u.IdWp).Count == 0)
                new WPCtrl().DeleteFromProjekt(u.Bezeichner, idProjekt);
        }
    }
}
