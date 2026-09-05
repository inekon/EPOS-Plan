using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Übersichtsseite (iU9-W5.5/W5.6) — Nachfolge von
    /// <c>Views/BerichteKosten/UcBkUebersicht.cs</c> (1 552 Z.).
    ///
    /// <para><b>Was hier liegt.</b> Die Verwaltung der Vergleichsgruppe über
    /// <see cref="VariantenCtrl"/>, der Simulationsstand über
    /// <see cref="BerichtsDatenSammler.ErmittleStatus"/>, der headless
    /// Simulationslauf über <see cref="SimulationRunner"/> und beide Ansichten
    /// des Komponentenbereichs über <see cref="ProjektDetails"/> und
    /// <see cref="AbweichungsErmittler"/> — es gibt bewusst keine zweite
    /// Vergleichslogik.</para>
    ///
    /// <para><b>Der Puffer gehört zu genau einer Gruppe</b> (wie im Vorläufer):
    /// Er wird verworfen, sobald sich der Stand ändern konnte —
    /// Gruppenwechsel, Simulation, Übernahme, Anlegen/Löschen einer
    /// Variante.</para>
    /// </summary>
    internal sealed class UebersichtSeiteGaben
    {
        /// <summary>
        /// Registry-Zweig der Variantenprobe — derselbe Pfad, den der mit
        /// iU9-W0 gelöschte Altdialog „Projektvarianten" benutzt hat, damit die
        /// zuletzt bearbeitete Gruppe eines Bestandsstands erhalten bleibt.
        /// </summary>
        private const string RegPfad = @"Software\EPOS_PLAN\Variantentest";
        private const string RegWertStamm = "LetzterStammID";

        /// <summary>
        /// Höchstzahl der Variantenspalten in der Gegenüberstellung. Mehr sind
        /// auf einem Bildschirm nicht mehr zu lesen; darüber wird gekappt —
        /// aber SICHTBAR (Überschrift und Statuszeile nennen die Zahl).
        /// </summary>
        private const int MAX_VARIANTENSPALTEN = 8;

        private readonly VariantenCtrl _ctrl = new VariantenCtrl();

        private int _aktuellesProjekt = -1;
        private int _stammId = -1;
        private string _stammName = "";
        private bool _nurStaemme;
        private int _markiert = -1;
        private string _markiertName = "";

        private readonly Dictionary<int, ProjektDetails> _details = new Dictionary<int, ProjektDetails>();
        private int _detailsGruppe = -1;

        /// <summary>Die Zeilen des Komponentenbereichs samt ihrer Übernahmedaten.</summary>
        private readonly Dictionary<int, UebernahmeSatz> _uebernahmen = new Dictionary<int, UebernahmeSatz>();

        /// <summary>Das Stammprojekt hat gewechselt (Id, Name) — der Wirt zieht nach.</summary>
        internal event Action<int, string> StammGewechselt;

        /// <summary>Eine Listenzeile wurde markiert (Id, Name) — die Kostenseite folgt ihr.</summary>
        internal event Action<int, string> ProjektMarkiert;

        /// <summary>Der Datensatz einer Übernahmezeile (Vorbild <c>UebernahmeZeile</c>).</summary>
        private sealed class UebernahmeSatz
        {
            internal int IdStamm;
            internal int IdVariante;
            internal string Gewerk = "";
            internal string Merkmal = "";
            internal AbweichungsErmittler.Merkmal Feld;
        }

        // =====================================================================
        // Kontext
        // =====================================================================

        /// <summary>
        /// Setzt den Projektkontext des Reiters. Ist das Projekt eine Variante,
        /// wird deren Stammprojekt gewählt und die Variante markiert.
        /// </summary>
        internal void SetzeAktuellesProjekt(int idProjekt)
        {
            _aktuellesProjekt = idProjekt;
            _markiert = -1;
            VorauswahlBestimmen();
            VerwirfDetails();
        }

        /// <summary>Das gewählte Stammprojekt (0 = keins).</summary>
        internal int IdStamm { get { return _stammId > 0 ? _stammId : 0; } }

        /// <summary>Name des gewählten Stammprojekts.</summary>
        internal string StammName { get { return _stammName; } }

        /// <summary>Die markierte Version (Stamm oder Variante).</summary>
        internal int IdMarkiert { get { return _markiert; } }

        /// <summary>Name der markierten Version.</summary>
        internal string NameMarkiert { get { return _markiertName; } }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<UebersichtStand>(Laden),
                ["StammGewechselt"] = new Action<int>(StammSetzen),
                ["FilterGewechselt"] = new Action<bool>(FilterSetzen),
                ["ZeileMarkiert"] = new Action<int>(ZeileSetzen),
                ["VarianteAnlegen"] = new Func<string, string>(VarianteAnlegen),
                ["LoeschFrage"] = new Func<string>(LoeschFrage),
                ["VarianteLoeschen"] = new Func<bool, string>(VarianteLoeschen),

                // Entscheid O-4 vom 04.09.2026: Trifft der Projektname MEHRERE Projekte,
                // wird gefragt, statt still beide zu loeschen. Die Zaehlung ist DIESELBE,
                // mit der VariantenCtrl.LoescheVariante abbricht - die Seite ruehrt keine
                // Datenbank an (Hausregel EPOS.UI). Die zwei Texte sind die des
                // ProjektWahlDialogs (Entscheid O-3); es ist dieselbe Frage.
                ["NamensAnzahl"] = new Func<string, int>(ProjektCtrl.AnzahlGleicherNamen),
                ["MehrdeutigTitel"] = T("PROJ_MSG_NAME_MEHRDEUTIG_TITEL",
                    "Projektname mehrfach vergeben"),
                ["MehrdeutigFormat"] = T("PROJ_MSG_NAME_MEHRDEUTIG",
                    "Der Projektname „{0}“ ist {1}-mal vergeben. Alle {1} Projekte werden "
                    + "gelöscht. Fortfahren?"),
                ["Simulation"] = new Func<Action<Laufschritt>, Task<LaufErgebnis>>(Simulieren),
                ["UebernahmeGaben"] = new Func<VergleichZeile,
                                               IReadOnlyDictionary<string, object>>(UebernahmeGaben),
                ["Uebernehmen"] = new Func<VergleichZeile, int, string>(Uebernehmen),

                ["TitelText"] = MyResource.Resource.BK_KOPF_UEBERSICHT,
                ["LabelStamm"] = T("BKS_LBL_STAMM", "Stammprojekt:"),
                ["LabelNurStaemme"] = T("BKS_LBL_NUR_STAEMME", "nur Stammprojekte"),
                ["LabelBezeichner"] = T("BKS_LBL_BEZEICHNER", "Bezeichner:"),

                // Anwenderwunsch 05.09.2026 (W5-E-1): die Versionswahl ist ein
                // Auswahlfeld. Die drei Spaltenkoepfe Art/Bezeichner/Projektname
                // sind damit weg - der Eintragstext traegt Bezeichner und
                // Projektname, die Art sagt die Reihenfolge. Geblieben ist der
                // Kopf "Simulation": er beschriftet jetzt die Statuszeile.
                ["LabelVariante"] = T("BKS_LBL_VARIANTE", "Variante:"),
                ["SpalteSimulation"] = MyResource.Resource.BK_BER_SP_SIMULATION,
                ["SimNieText"] = T("BKS_SIM_NIE", "noch nicht simuliert"),
                ["SimGrundFehlt"] = T("BKS_SIM_GRUND_FEHLT",
                    "Für diese Version liegt kein Simulationsergebnis vor."),
                ["SimGrundVeraltet"] = T("BKS_SIM_GRUND_VERALTET",
                    "Das Simulationsergebnis ist älter als die letzte Änderung am Projekt."),
                ["SpalteAktion"] = MyResource.Resource.BK_SP_AKTION,
                ["AnlegenText"] = T("BKS_BTN_ANLEGEN", "Variante anlegen"),
                ["LoeschenText"] = MyResource.Resource.BK_BTN_LOESCHEN,
                ["SimulierenText"] = T("BKS_BTN_SIMULIEREN", "Simulation starten"),
                ["UebernehmenText"] = MyResource.Resource.BK_BTN_UEBERNEHMEN,
                ["WahlKurztext"] = T("BKS_WAHL_VERSION", "Version wählen"),
                ["JaText"] = T("BKS_BTN_JA", "Ja"),
                ["NeinText"] = T("BKS_BTN_NEIN", "Nein"),
                ["StatusAbgebrochen"] = MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN,
                ["HilfeSchluessel"] = "UcBkUebersicht.btn_Help"
            };
        }

        // =====================================================================
        // Laden (Vorbild LadeProjekte + LadeAuswahl + ZeigeKomponenten)
        // =====================================================================

        private UebersichtStand Laden()
        {
            var stand = new UebersichtStand { NurStaemme = _nurStaemme };
            _uebernahmen.Clear();

            try { _ctrl.StelleVariantentabelleSicher(); } catch { }

            List<ValueTuple<int, string>> staemme = Staemme();
            stand.Staemme = staemme;
            if (staemme.Count == 0)
            {
                stand.KomponentenTitel = MyResource.Resource.BK_LBL_KOMPONENTEN_VERGLEICH;
                return stand;
            }

            if (_stammId <= 0 || !staemme.Any(x => x.Item1 == _stammId))
            {
                _stammId = staemme[0].Item1;
                _stammName = staemme[0].Item2;
            }
            stand.StammId = _stammId;

            // --- die Liste --------------------------------------------------
            var stand2 = new Dictionary<int, BerichtsDatenSammler.VariantenStatus>();
            try
            {
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(_stammId, _stammName))
                    stand2[st.IdProjekt] = st;
            }
            catch { }

            var zeilen = new List<VarianteZeile>();
            try
            {
                foreach (VariantenCtrl.VarianteInfo vi in _ctrl.LadeGruppe(_stammId, _stammName))
                {
                    BerichtsDatenSammler.VariantenStatus st;
                    stand2.TryGetValue(vi.IdProjekt, out st);
                    zeilen.Add(new VarianteZeile
                    {
                        IdProjekt = vi.IdProjekt,
                        Art = vi.IstStamm ? MyResource.Resource.BK_ART_STAMM
                                          : MyResource.Resource.BK_ART_VARIANTE,
                        Bezeichner = vi.IstStamm ? MyResource.Resource.BK_ART_STAMMPROJEKT
                                                 : vi.Variantenname,
                        Projektname = vi.Projektname,
                        SimStand = st != null ? st.SimStandText : "",

                        // Der REINE Zeitpunkt fuer die Statuszeile der Seite
                        // (Anwenderwunsch 05.09.2026, W5-E-1). SimStandText traegt
                        // das "⚠" und im Fehlfall den Wortlaut "— (fehlt) ⚠" schon
                        // in sich; die Statuszeile setzt beides selbst zusammen und
                        // haengt den Grund als Kurztext ans Warnzeichen. Format wie
                        // in SimStandText - es ist derselbe Wert.
                        SimZeitpunkt = st != null && st.SimStand.HasValue
                            ? st.SimStand.Value.ToString("dd.MM.yy HH:mm")
                            : "",
                        IstStamm = vi.IstStamm,
                        Auffaellig = st != null && (!st.SimStand.HasValue || st.Veraltet)
                    });
                }
            }
            catch (Exception ex)
            {
                stand.Statuszeile = string.Format(MyResource.Resource.BK_MSG_LADEFEHLER, ex.Message);
            }
            stand.Zeilen = zeilen;

            // Markierung: die vorgemerkte Zeile, sonst das GEOEFFNETE Projekt,
            // sonst der Stamm (Vorbild WaehleZeile).
            if (zeilen.Count > 0)
            {
                if (!zeilen.Any(z => z.IdProjekt == _markiert))
                    _markiert = zeilen.Any(z => z.IdProjekt == _aktuellesProjekt)
                        ? _aktuellesProjekt : zeilen[0].IdProjekt;

                VarianteZeile m = zeilen.First(z => z.IdProjekt == _markiert);
                _markiertName = m.Projektname;
                stand.MarkierteId = _markiert;
                stand.Loeschbar = !m.IstStamm;
                stand.SimulierenMoeglich = true;
            }
            stand.AnlegenMoeglich = _stammId > 0;

            // --- der Komponentenbereich -------------------------------------
            Komponentenbereich(stand);
            return stand;
        }

        /// <summary>
        /// Das Stamm-Dropdown — je nach Filter alle Projekte oder nur bereits
        /// gesetzte Stammprojekte.
        /// </summary>
        private List<ValueTuple<int, string>> Staemme()
        {
            var liste = new List<ValueTuple<int, string>>();
            try
            {
                var pc = new ProjektCtrl();
                pc.ReadAll();

                HashSet<int> nurStaemme = _nurStaemme ? _ctrl.LiesStammProjektIds() : null;
                foreach (ProjektModel p in pc.items)
                {
                    if (nurStaemme != null && !nurStaemme.Contains(p.m_ID)) continue;
                    liste.Add(new ValueTuple<int, string>(p.m_ID, p.m_szProjektname));
                }
            }
            catch { }
            return liste;
        }

        // =====================================================================
        // Der Komponentenbereich — zwei Ansichten, eine Diff-Welt
        // =====================================================================

        private void Komponentenbereich(UebersichtStand stand)
        {
            VarianteZeile markiert = stand.Zeilen.FirstOrDefault(z => z.IdProjekt == stand.MarkierteId);
            if (markiert == null)
            {
                stand.KomponentenTitel = MyResource.Resource.BK_LBL_KOMPONENTEN_VERGLEICH;
                return;
            }

            ProjektDetails ds = Details(_stammId, _stammId);
            if (markiert.IstStamm) Gegenueberstellung(stand, ds);
            else Unterschiede(stand, markiert, ds);
        }

        /// <summary>
        /// Detaildaten eines Projekts der Gruppe — aus dem Puffer, sonst frisch
        /// geladen. Der Puffer gehört zu GENAU EINER Gruppe.
        /// </summary>
        private ProjektDetails Details(int idStamm, int idProjekt)
        {
            if (_detailsGruppe != idStamm) { _details.Clear(); _detailsGruppe = idStamm; }

            ProjektDetails d;
            if (_details.TryGetValue(idProjekt, out d)) return d;

            d = ProjektDetails.Lade(idProjekt);
            _details[idProjekt] = d;
            return d;
        }

        private void VerwirfDetails()
        {
            _details.Clear();
            _detailsGruppe = -1;
        }

        /// <summary>
        /// Gegenüberstellung Stamm ↔ Varianten: Gewerk · Merkmal · Stamm · je
        /// Variante eine Spalte, in der Reihenfolge der oberen Liste.
        /// </summary>
        private void Gegenueberstellung(UebersichtStand stand, ProjektDetails ds)
        {
            List<VarianteZeile> varianten = stand.Zeilen.Where(z => !z.IstStamm).ToList();
            int ausgelassen = Math.Max(0, varianten.Count - MAX_VARIANTENSPALTEN);
            if (ausgelassen > 0) varianten = varianten.GetRange(0, MAX_VARIANTENSPALTEN);

            string gekappt = ausgelassen > 0
                ? string.Format(MyResource.Resource.BK_LBL_VARIANTEN_GEKAPPT, varianten.Count, ausgelassen)
                : "";
            stand.KomponentenTitel = MyResource.Resource.BK_LBL_KOMPONENTEN_VERGLEICH +
                                     (gekappt.Length == 0 ? "" : " — " + gekappt);

            var spalten = new List<string>
            {
                MyResource.Resource.BK_SP_GEWERK,
                MyResource.Resource.BK_SP_MERKMAL,
                MyResource.Resource.BK_SP_WERT_STAMM
            };
            foreach (VarianteZeile v in varianten) spalten.Add(SpaltenKopf(v));
            stand.Spalten = spalten;

            var versionen = new List<ProjektDetails> { ds };
            foreach (VarianteZeile v in varianten) versionen.Add(Details(_stammId, v.IdProjekt));

            var zeilen = new List<VergleichZeile>();
            FuelleVergleich(versionen, zeilen);
            stand.Vergleich = zeilen;

            if (zeilen.Count == 0) { stand.Statuszeile = MyResource.Resource.BK_MSG_KEINE_KOMPONENTEN; return; }

            string status = string.Format(MyResource.Resource.BK_MSG_VERGLEICH_UMFANG,
                                          zeilen.Count, varianten.Count);
            stand.Statuszeile = gekappt.Length == 0 ? status : status + "  " + gekappt;
        }

        private static string SpaltenKopf(VarianteZeile z)
        {
            return string.IsNullOrEmpty(z.Bezeichner) ? z.Projektname : z.Bezeichner;
        }

        /// <summary>
        /// Die Zeilen der Gegenüberstellung — sie kommen seit dem Anwenderbefund
        /// W5‑E‑2 (05.09.2026) fertig aus dem Kern
        /// (<see cref="KomponentenVergleich.Gegenueberstellung"/>); hier bleibt
        /// allein die Abbildung auf den Zeilentyp der Razor-Seite.
        ///
        /// <para><b>Was sich geändert hat.</b> Gezeigt werden nur noch die
        /// tatsächlich VERWENDETEN Erzeugerkomponenten — je Gewerk die Stückzahl
        /// und darunter eine Zeile je Komponente. Die Parameterblöcke „Anlage"
        /// und „Gebäude" der Feldliste sind aus dieser Ansicht heraus: „Gewerk
        /// Anlage gibt es nicht. Dort stehen Parameter." Die Parameter werden
        /// weiterhin verglichen — in der UNTERSCHIEDSansicht einer Variante, wo
        /// eine Zeile eine Änderung zeigt und die Übernahme trägt.</para>
        /// </summary>
        private static void FuelleVergleich(List<ProjektDetails> versionen, List<VergleichZeile> ziel)
        {
            foreach (KomponentenVergleichZeile z in KomponentenVergleich.Gegenueberstellung(versionen))
                ziel.Add(new VergleichZeile
                {
                    Gewerk = z.Gewerk,
                    Merkmal = z.Merkmal,
                    Zellen = z.Zellen,
                    Kurztexte = z.Kurztexte
                });
        }

        /// <summary>Unterschiede der Variante gegenüber dem Stamm samt Aktionsspalte.</summary>
        private void Unterschiede(UebersichtStand stand, VarianteZeile z, ProjektDetails ds)
        {
            stand.KomponentenTitel = string.Format(MyResource.Resource.BK_LBL_KOMPONENTEN_DIFF,
                string.IsNullOrEmpty(z.Bezeichner) ? z.Projektname : z.Bezeichner);

            stand.Spalten = new List<string>
            {
                MyResource.Resource.BK_SP_GEWERK,
                MyResource.Resource.BK_SP_MERKMAL,
                MyResource.Resource.BK_SP_WERT_STAMM,
                MyResource.Resource.BK_SP_WERT_VARIANTE
            };

            ProjektDetails dv = Details(_stammId, z.IdProjekt);
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(ds, dv);

            if (liste.Count == 0)
            {
                stand.Statuszeile = MyResource.Resource.BK_MSG_KEINE_ABWEICHUNG;
                return;
            }

            var zeilen = new List<VergleichZeile>();
            int schluessel = 1;
            foreach (Abweichung a in liste)
            {
                var satz = new UebernahmeSatz
                {
                    IdStamm = _stammId,
                    IdVariante = z.IdProjekt,
                    Gewerk = a.Gewerk,
                    Merkmal = a.Merkmal
                };
                satz.Feld = AbweichungsErmittler.Felder
                    .FirstOrDefault(x => x.Gewerk == a.Gewerk && x.Label == a.Merkmal);

                var zeile = new VergleichZeile
                {
                    Schluessel = schluessel,
                    Gewerk = a.Gewerk,
                    Merkmal = a.Merkmal,
                    Zellen = new List<string> { a.WertStamm, a.WertVariante },
                    MitAktion = true,
                    Sperrgrund = Sperrgrund(satz) ?? "",
                    AktionKurztext = satz.Feld != null
                        ? MyResource.Resource.BK_TIP_UEBERNEHMEN_FELD
                        : MyResource.Resource.BK_TIP_UEBERNEHMEN_KOMP
                };
                _uebernahmen[schluessel] = satz;
                zeilen.Add(zeile);
                schluessel++;
            }
            stand.Vergleich = zeilen;
            stand.Statuszeile = string.Format(MyResource.Resource.BK_MSG_ANZAHL_UNTERSCHIEDE, liste.Count);
        }

        /// <summary>Grund, warum diese Zeile nicht übernommen werden kann (null = sie kann).</summary>
        private static string Sperrgrund(UebernahmeSatz s)
        {
            if (s == null) return MyResource.Resource.BK_MSG_UEB_KEIN_FELD;

            // Stufe 1: ganzer Komponentenbestand eines Gewerks.
            if (s.Feld == null || string.IsNullOrEmpty(s.Feld.Tabelle) || string.IsNullOrEmpty(s.Feld.Spalte))
                return KomponentenUebernahmeCtrl.Unterstuetzt(s.Gewerk)
                    ? null
                    : string.Format(MyResource.Resource.BK_MSG_KOMP_GEWERK_UNBEKANNT, s.Gewerk);

            // Stufe 3: der Bezeichner ist der Schluessel der Zuordnung selbst.
            if (MerkmalUebernahmeCtrl.IstSchluesselspalte(s.Feld.Spalte))
                return MyResource.Resource.BK_TIP_UEBERNEHMEN_GESPERRT_SCHLUESSEL;

            return null;
        }

        // =====================================================================
        // Auswahl
        // =====================================================================

        private void StammSetzen(int id)
        {
            if (id == _stammId) return;
            _stammId = id;
            _stammName = Staemme().Where(x => x.Item1 == id).Select(x => x.Item2).FirstOrDefault() ?? "";
            _markiert = -1;
            SpeichereLetztenStamm(id);
            VerwirfDetails();
            Melde();
        }

        private void FilterSetzen(bool an)
        {
            _nurStaemme = an;
            VerwirfDetails();
        }

        private void ZeileSetzen(int idProjekt)
        {
            _markiert = idProjekt;
            Action<int, string> h = ProjektMarkiert;
            if (h != null) h(idProjekt, _markiertName);
        }

        private void Melde()
        {
            Action<int, string> h = StammGewechselt;
            if (h != null) h(_stammId, _stammName);
        }

        /// <summary>
        /// Bestimmt das vorzuwählende Stammprojekt: das geöffnete Projekt (ist
        /// es eine Variante, deren Stamm), sonst die zuletzt gewählte Auswahl
        /// (Registry), sonst der erste Eintrag.
        /// </summary>
        private void VorauswahlBestimmen()
        {
            int gewuenscht = -1;
            if (_aktuellesProjekt > 0)
            {
                int refId = 0;
                try { refId = _ctrl.StammRefDerVariante(_aktuellesProjekt); }
                catch { }
                if (refId > 0) { _markiert = _aktuellesProjekt; gewuenscht = refId; }
                else gewuenscht = _aktuellesProjekt;
            }
            if (gewuenscht <= 0) gewuenscht = LiesLetztenStamm();

            List<ValueTuple<int, string>> staemme = Staemme();
            if (staemme.Count == 0) return;

            ValueTuple<int, string> treffer = staemme.FirstOrDefault(x => x.Item1 == gewuenscht);
            if (treffer.Item1 <= 0) treffer = staemme[0];

            _stammId = treffer.Item1;
            _stammName = treffer.Item2;
            SpeichereLetztenStamm(_stammId);
            Melde();
        }

        private static void SpeichereLetztenStamm(int idProjekt)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPfad))
                {
                    if (key != null) key.SetValue(RegWertStamm, idProjekt, RegistryValueKind.DWord);
                }
            }
            catch { /* Persistenz ist optional */ }
        }

        private static int LiesLetztenStamm()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPfad))
                {
                    object v = key?.GetValue(RegWertStamm);
                    if (v != null) return Convert.ToInt32(v);
                }
            }
            catch { }
            return -1;
        }

        // =====================================================================
        // Variante anlegen und löschen
        // =====================================================================

        private string VarianteAnlegen(string bezeichner)
        {
            if (_stammId <= 0) return MyResource.Resource.BK_MSG_KEIN_STAMM;

            try
            {
                string fehler;
                int neueId = _ctrl.AnlegenAusStamm(_stammId, _stammName, bezeichner, out fehler);
                if (neueId <= 0)
                    return fehler ?? MyResource.Resource.BK_MSG_ANLEGEN_FEHLGESCHLAGEN;

                VerwirfDetails();

                // Ä19: Auch die Variantenliste des Projektkopfs kennt die neue
                // Variante sofort.
                StartseiteHuelle.Aktuelle?.VariantenAnzeigeAktualisieren();

                return string.Format(MyResource.Resource.BK_MSG_VARIANTE_ANGELEGT,
                                     (bezeichner ?? "").Trim());
            }
            catch (Exception ex)
            {
                return string.Format(MyResource.Resource.BK_MSG_ANLEGEFEHLER, ex.Message);
            }
        }

        private string LoeschFrage()
        {
            VarianteZeile z = MarkierteZeile();
            if (z == null || z.IstStamm) return "";
            return string.Format(MyResource.Resource.BK_MSG_LOESCHEN_FRAGE, z.Bezeichner);
        }

        /// <summary>
        /// Löscht die markierte Variante (iU9-W15a, Entscheid O-4 vom 04.09.2026).
        /// </summary>
        /// <param name="alleGleichenNamens">
        /// Die Antwort auf die Mehrdeutigkeits-Rückfrage der Seite: <c>true</c> = der
        /// Anwender hat dem Löschen ALLER Projekte dieses Namens ausdrücklich zugestimmt.
        /// </param>
        private string VarianteLoeschen(bool alleGleichenNamens)
        {
            VarianteZeile z = MarkierteZeile();
            if (z == null || z.IstStamm) return MyResource.Resource.BK_MSG_NUR_VARIANTE;

            try
            {
                LoeschBefund befund = _ctrl.LoescheVariante(z.IdProjekt, z.Projektname,
                                                            alleGleichenNamens);

                // Der Kern hat abgebrochen, ohne etwas anzufassen: Der Name trifft
                // mehrere Projekte, und die Zustimmung fehlt. Hier ist das das
                // SICHERUNGSNETZ - die Rueckfrage steht in der Seite; ein "Nein" kommt
                // gar nicht bis hierher. Gemeldet wird derselbe Satz, damit der Anwender
                // sieht, WARUM nichts geloescht wurde.
                if (befund.Stand == LoeschStand.Mehrdeutig)
                    return string.Format(T("PROJ_MSG_NAME_MEHRDEUTIG",
                        "Der Projektname „{0}“ ist {1}-mal vergeben. Alle {1} Projekte werden "
                        + "gelöscht. Fortfahren?"), befund.Projektname, befund.Anzahl);

                if (befund.Stand != LoeschStand.Geloescht)
                    return string.IsNullOrEmpty(befund.Fehlertext)
                        ? MyResource.Resource.BK_MSG_LOESCHEN_FEHLGESCHLAGEN
                        : befund.Fehlertext;

                VerwirfDetails();          // die Gruppe hat eine Spalte weniger
                _markiert = -1;
                return string.Format(MyResource.Resource.BK_MSG_VARIANTE_GELOESCHT, z.Bezeichner);
            }
            catch (Exception ex)
            {
                return string.Format(MyResource.Resource.BK_MSG_LOESCHFEHLER, ex.Message);
            }
        }

        private VarianteZeile MarkierteZeile()
        {
            try { return Laden().Zeilen.FirstOrDefault(z => z.IdProjekt == _markiert); }
            catch { return null; }
        }

        // =====================================================================
        // Simulation (Vorbild btnSimulieren_Click)
        // =====================================================================

        private async Task<LaufErgebnis> Simulieren(Action<Laufschritt> melder)
        {
            VarianteZeile z = MarkierteZeile();
            if (z == null || _stammId <= 0)
                return new LaufErgebnis { Statuszeile = MyResource.Resource.BK_MSG_BITTE_WAEHLEN };

            // Zu simulierende Projekte: der Stamm immer, plus die gewaehlte
            // Variante — so werden die Ergebnisse BEIDER frisch geschrieben.
            var laeufe = new List<Tuple<int, string>>();
            laeufe.Add(Tuple.Create(_stammId,
                string.Format(MyResource.Resource.BK_PRAEFIX_STAMM, _stammName)));
            if (!z.IstStamm)
                laeufe.Add(Tuple.Create(z.IdProjekt,
                    string.Format(MyResource.Resource.BK_PRAEFIX_VARIANTE, z.Bezeichner)));

            try
            {
                var meldungen = new List<string>();
                for (int i = 0; i < laeufe.Count; i++)
                {
                    Tuple<int, string> lauf = laeufe[i];
                    melder(new Laufschritt(i, laeufe.Count, lauf.Item2));

                    await Task.Run(() =>
                    {
                        // Headless-Lauf: neue Instanz je Projekt.
                        string fehler;
                        var runner = new SimulationRunner();
                        int erg = runner.SimuliereUndSpeichere(lauf.Item1, out fehler);
                        meldungen.Add(erg > 0
                            ? string.Format(MyResource.Resource.BK_MSG_SIM_OK, lauf.Item2, erg)
                            : string.Format(MyResource.Resource.BK_MSG_SIM_FEHLER, lauf.Item2, fehler));

                        // Auch ein ERFOLGREICHER Lauf kann mit einer
                        // Ersatzannahme gerechnet haben (Paket-8-Fehlerkanal).
                        string hinweise = runner.Protokoll != null
                            ? runner.Protokoll.HinweistextFuerAnzeige() : "";
                        if (!string.IsNullOrEmpty(hinweise))
                            meldungen.Add("    " + hinweise.Replace("\r\n", "\r\n    ")
                                                           .Replace("\n", "\n    "));
                    });
                }

                VerwirfDetails();
                return new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = string.Format(MyResource.Resource.BK_MSG_SIM_FERTIG, laeufe.Count),
                    Meldung = string.Join("\r\n", meldungen)
                };
            }
            catch (Exception ex)
            {
                return new LaufErgebnis
                {
                    Fehler = string.Format(MyResource.Resource.BK_MSG_SIMFEHLER, ex.Message)
                };
            }
        }

        // =====================================================================
        // Übernahme
        // =====================================================================

        private IReadOnlyDictionary<string, object> UebernahmeGaben(VergleichZeile zeile)
        {
            UebernahmeSatz s;
            if (zeile == null || !_uebernahmen.TryGetValue(zeile.Schluessel, out s)) return null;
            if (Sperrgrund(s) != null) return null;

            List<UebernahmeQuelle> quellen = Quellen(s.IdVariante);
            if (quellen.Count == 0) return null;

            var ctrl = new KomponentenUebernahmeCtrl();
            bool mitKlartext = s.Feld == null;

            return new Dictionary<string, object>
            {
                ["TitelText"] = mitKlartext ? MyResource.Resource.BK_UEB_TITEL_KOMP
                                            : MyResource.Resource.BK_UEB_TITEL_FELD,
                ["Gegenstand"] = mitKlartext ? s.Gewerk : s.Gewerk + " · " + s.Merkmal,
                ["ZielName"] = ZielName(s.IdVariante),
                ["Quellen"] = (IReadOnlyList<UebernahmeQuelle>)quellen,
                ["MitKlartext"] = mitKlartext,
                ["Lader"] = new Func<int, UebernahmeVorschau>(id => mitKlartext
                    ? VorschauKomponenten(ctrl, id, s.IdVariante, s.Gewerk)
                    : VorschauFeld(id, s.IdVariante, s.Feld)),

                ["LabelQuelle"] = MyResource.Resource.BK_UEB_LBL_QUELLE,
                ["LabelWertQuelle"] = MyResource.Resource.BK_UEB_LBL_WERT_QUELLE,
                ["LabelZiel"] = MyResource.Resource.BK_UEB_LBL_ZIEL,
                ["LabelWertZiel"] = MyResource.Resource.BK_UEB_LBL_WERT_ZIEL,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.BK_UEB_BTN_ABBRUCH,
                ["MeldungKeineQuelle"] = MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE
            };
        }

        /// <summary>
        /// Die wählbaren Quellen: das Stammprojekt (Vorgabe) und jede andere
        /// Variante derselben Gruppe — das Ziel selbst natürlich nicht.
        /// </summary>
        private List<UebernahmeQuelle> Quellen(int idZiel)
        {
            var liste = new List<UebernahmeQuelle>();
            try
            {
                foreach (VariantenCtrl.VarianteInfo vi in _ctrl.LadeGruppe(_stammId, _stammName))
                {
                    if (vi.IdProjekt == idZiel) continue;
                    liste.Add(new UebernahmeQuelle
                    {
                        Id = vi.IdProjekt,
                        Anzeige = vi.IstStamm
                            ? string.Format(MyResource.Resource.BK_UEB_QUELLE_STAMM, vi.Projektname)
                            : string.Format(MyResource.Resource.BK_UEB_QUELLE_VARIANTE, vi.Variantenname)
                    });
                }
            }
            catch { }
            return liste;
        }

        private string ZielName(int idZiel)
        {
            try
            {
                VarianteZeile z = Laden().Zeilen.FirstOrDefault(x => x.IdProjekt == idZiel);
                if (z == null) return "";
                return string.IsNullOrEmpty(z.Bezeichner) ? z.Projektname : z.Bezeichner;
            }
            catch { return ""; }
        }

        private static UebernahmeVorschau VorschauFeld(int idQuelle, int idZiel,
                                                       AbweichungsErmittler.Merkmal f)
        {
            MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(idQuelle, idZiel, f);
            var v = new UebernahmeVorschau
            {
                Moeglich = b.Moeglich && !b.Gleichstand,
                Grund = b.Moeglich ? (b.Gleichstand ? MyResource.Resource.BK_MSG_UEB_GLEICH : "") : b.Grund,
                WertQuelle = b.Quelle.Anzeigewert,
                WertZiel = b.Ziel.Anzeigewert
            };

            // Quelle und Ziel koennen unterschiedliche Komponenten sein - der
            // Dialog nennt sie, statt darueber hinweg zu schreiben.
            if (b.Quelle.Bezeichner.Length > 0 || b.Ziel.Bezeichner.Length > 0)
                v.Komponenten = string.Format(MyResource.Resource.BK_UEB_KOMPONENTEN,
                                              Oder(b.Quelle.Bezeichner), Oder(b.Ziel.Bezeichner));
            return v;
        }

        private static UebernahmeVorschau VorschauKomponenten(KomponentenUebernahmeCtrl ctrl,
                                                              int idQuelle, int idZiel, string gewerk)
        {
            KomponentenUebernahmeCtrl.Vorschau p = ctrl.Planen(idQuelle, idZiel, gewerk);
            return new UebernahmeVorschau
            {
                Moeglich = p.Moeglich,
                Grund = p.Grund,
                Klartext = p.Klartext
            };
        }

        private static string Oder(string s) { return string.IsNullOrEmpty(s) ? "—" : s; }

        /// <summary>Führt die Übernahme aus und liefert die Meldung.</summary>
        private string Uebernehmen(VergleichZeile zeile, int idQuelle)
        {
            UebernahmeSatz s;
            if (zeile == null || !_uebernahmen.TryGetValue(zeile.Schluessel, out s)) return "";

            try
            {
                string meldung;
                if (s.Feld == null)
                {
                    var ctrl = new KomponentenUebernahmeCtrl();
                    KomponentenUebernahmeCtrl.Vorschau v =
                        ctrl.Planen(idQuelle, s.IdVariante, s.Gewerk);

                    string fehler, hinweise;
                    if (!ctrl.Uebernehmen(idQuelle, s.IdVariante, s.Gewerk, out fehler, out hinweise))
                        return string.Format(MyResource.Resource.BK_MSG_KOMP_FEHLER, fehler ?? "");

                    meldung = string.Format(MyResource.Resource.BK_MSG_KOMP_OK, s.Gewerk,
                                            v.Anlegen.Count, v.Gleichziehen.Count, v.Entfernen.Count);
                    if (!string.IsNullOrEmpty(hinweise)) meldung += "  " + hinweise;
                }
                else
                {
                    MerkmalUebernahmeCtrl.Befund b =
                        MerkmalUebernahmeCtrl.Pruefe(idQuelle, s.IdVariante, s.Feld);

                    string fehler;
                    if (!MerkmalUebernahmeCtrl.Schreibe(b, s.IdVariante, s.Feld, out fehler))
                        return string.Format(MyResource.Resource.BK_MSG_UEB_FEHLER, fehler ?? "");

                    meldung = string.Format(MyResource.Resource.BK_MSG_UEB_OK,
                                            s.Gewerk, s.Merkmal, b.Quelle.Anzeigewert);
                }

                // Nach jedem Schreibvorgang: Puffer verwerfen, Zeile markiert
                // lassen, auf veraltete Ergebnisse hinweisen.
                VerwirfDetails();
                _markiert = s.IdVariante;
                if (MerkmalUebernahmeCtrl.HatErgebnisse(s.IdVariante))
                    meldung += "  " + MyResource.Resource.BK_MSG_UEB_ERGEBNIS_VERALTET;
                return meldung;
            }
            catch (Exception ex)
            {
                return string.Format(MyResource.Resource.BK_MSG_UEB_FEHLER, ex.Message);
            }
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(t) ? rueckfall : t;
            }
            catch { return rueckfall; }
        }
    }
}
