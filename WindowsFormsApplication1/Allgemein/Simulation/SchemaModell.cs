using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE D4 (Konzept_KonfigUI_Hydraulik, Abschnitt 3 „Ansicht Schema" und
    /// Mockup-Abschnitt 1/2) — das ZEICHENMODELL der Hydraulikübersicht: Knoten,
    /// Kanten und Kaskadenkette eines Projekts.
    ///
    /// <b>Ohne Oberfläche.</b> Hier steht kein <c>System.Windows.Forms</c> und kein
    /// <c>System.Drawing</c>: Was gezeichnet wird, entscheidet dieses Modell, WIE es
    /// gezeichnet wird, entscheidet <c>SchemaAnsicht</c>. Damit ist die Aussage des
    /// Schemas headless prüfbar — Knoten- und Kantenliste gegen die Datenbank, statt
    /// Pixel gegen ein Bild (Verifikationsvorgabe D4).
    ///
    /// <b>Eine Ableitung, keine zweite.</b> Die Verschaltung kommt aus
    /// <see cref="Hydraulikbild"/> — derselben Abbildung, mit der die Dialogprüfung aus
    /// D5b den Ring sucht. Ladepositionen kommen aus <see cref="Ladeordnung"/>,
    /// Speicherstammdaten aus <see cref="WaermesenkeClass.ProjektPufferListe"/>. Das
    /// Modell rechnet nichts nach, was eine dieser Stellen schon weiß.
    ///
    /// <b>Invariante S-1</b> (Konzept Abschnitt 5): Zwischen zwei Speicherknoten steht
    /// IMMER ein Erzeugerknoten. Strukturell garantiert — eine Kante entsteht hier nur
    /// aus einem Quell- oder Senkenbezug, und die gibt es ausschließlich an
    /// <c>Tab_Energieanlagen</c>. <see cref="Pruefen"/> prüft es zusätzlich nach, damit
    /// eine künftige Erweiterung nicht still dagegen verstößt.
    /// </summary>
    public sealed class SchemaModell
    {
        // --- Sprachneutrale Schlüssel (Drei-Schichten-Regel, Schicht „Schlüssel") -----

        public const string PRAEFIX_QUELLE = "QUELLE_";
        public const string PRAEFIX_ERZEUGER = "ERZEUGER_";
        public const string PRAEFIX_SPEICHER = "SPEICHER_";
        public const string ABNEHMER_HEIZKREIS = "ABNEHMER_HEIZKREIS";
        public const string ABNEHMER_WARMWASSER = "ABNEHMER_WARMWASSER";

        /// <summary>
        /// PAKET S2 — der dritte Abnehmerknoten (Konzept 10). Prozesswärme ist seit
        /// Paket K1/K2 ein eigener Kanal und seit S1 ein eigenes Senkenziel
        /// (<c>Prozesswaerme</c>, <c>PufferProzess</c>); bis S2 fehlte sie in der
        /// Abnehmerspalte, weil das Schema die Senke aus den beiden Altspalten las und
        /// die Prozess-Ziele dort gar nicht ausdrückbar sind.
        /// </summary>
        public const string ABNEHMER_PROZESS = "ABNEHMER_PROZESS";

        /// <summary>Spalte des Schemas — die vier Rubriken des Mockups.</summary>
        public enum Knotenart
        {
            /// <summary>Spalte 0: Wärmequelle (Außenluft, Erdsonde, Brennstoff …).</summary>
            Quelle,

            /// <summary>Spalte 1: Wärmeerzeuger.</summary>
            Erzeuger,

            /// <summary>Spalte 2: Pufferspeicher.</summary>
            Speicher,

            /// <summary>Spalte 3: Abnehmer (Heizkreis, Warmwasser).</summary>
            Abnehmer
        }

        /// <summary>Farbsprache der Verbindungen (Mockup, Legende).</summary>
        public enum Kantenart
        {
            /// <summary>Blau: Quellseite (Quelle → Erzeuger).</summary>
            Quelle,

            /// <summary>Blau gestrichelt: Kaskade (Puffer → nachgeschalteter Erzeuger).</summary>
            Kaskade,

            /// <summary>Koralle: Ladung (Erzeuger → Puffer), Kreis = wirksame Priorität.</summary>
            Ladung,

            /// <summary>Grün: Versorgung / Entladung (Puffer bzw. Erzeuger → Abnehmer).</summary>
            Versorgung,

            /// <summary>
            /// PAKET E1 (Befund S2-O7, Konzept § 10): Violett — Versorgung des
            /// PROZESS-Abnehmers.
            ///
            /// <para>Bis hierher trug die Prozesskante dieselbe Farbe wie Heizkreis und
            /// Warmwasser; der dritte Kanal war im Bild nicht von den beiden anderen zu
            /// unterscheiden. Fachlich ist er es sehr wohl — er hat eigene
            /// Temperaturanforderungen, eigene Senken und eigene Speicher.</para>
            ///
            /// <para><b>Eine eigene ART, keine eigene Kantenlogik:</b> Sie verhält sich in
            /// jeder Hinsicht wie <see cref="Versorgung"/> (durchgezogen, Pfeil,
            /// Prioritätskreis) und unterscheidet sich allein in der Farbe — genau die
            /// Trennung, die Kapitel 10 „eigene Kantenfarbe" verlangt.</para>
            /// </summary>
            Prozess
        }

        /// <summary>Ein Kasten im Schema.</summary>
        public sealed class Knoten
        {
            /// <summary>Sprachneutraler Schlüssel, z. B. <c>ERZEUGER_11203</c>.</summary>
            public string Schluessel = "";

            public Knotenart Art;

            /// <summary>Tab_Energieanlagen.ID bzw. Tab_Pufferspeicher.ID; 0 sonst.</summary>
            public int ID;

            /// <summary>ID_Type der Anlage (nur bei <see cref="Knotenart.Erzeuger"/>).</summary>
            public int ID_Type;

            /// <summary>Kaskadenrang als Text; "" = kein Rang.</summary>
            public string Rang = "";

            public string Titel = "";

            /// <summary>Zusatzzeilen unter dem Titel (Temperaturpaar, Volumen, Senke …).</summary>
            public List<string> Zeilen = new List<string>();

            /// <summary>Verwendungs-Badges eines Speichers (Heizung / Warmwasser).</summary>
            public List<string> Badges = new List<string>();

            /// <summary>Mouseover-Text; die Konfigurationsseite ersetzt ihn durch die Kartenkurzinfo.</summary>
            public string Hinweis = "";

            /// <summary>Temperatur-Warnregel (Konzept Abschnitt 5) — amber gezeichnet.</summary>
            public bool Warnung;

            /// <summary>Warnungstext (nur gesetzt, wenn <see cref="Warnung"/>).</summary>
            public string Warntext = "";

            /// <summary>Kaskadenbezug — blau gestrichelter Rahmen (Mockup: „Spitzenkessel").</summary>
            public bool Kaskade;
        }

        /// <summary>Eine Verbindung zwischen zwei Knoten.</summary>
        public sealed class Kante
        {
            public string Von = "";
            public string Nach = "";
            public Kantenart Art;

            /// <summary>Wirksame Ladepriorität für den Kreis an der Kante; 0 = keiner.</summary>
            public int Prioritaet;

            public string Hinweis = "";
        }

        /// <summary>Ein Glied der Kaskadenkette (Pillen-Band unter dem Schema).</summary>
        public sealed class Kettenglied
        {
            /// <summary>Knotenschlüssel, auf den das Glied zeigt; "" = kein Knoten.</summary>
            public string Schluessel = "";

            public string Text = "";
            public Knotenart Art;

            /// <summary>Farbe des Pfeils VOR diesem Glied; beim ersten Glied ohne Bedeutung.</summary>
            public Kantenart PfeilDavor;
        }

        public readonly List<Knoten> Knotenliste = new List<Knoten>();
        public readonly List<Kante> Kantenliste = new List<Kante>();

        /// <summary>Die abgeleiteten Kaskadenketten; leer, wenn das Projekt keine führt.</summary>
        public readonly List<List<Kettenglied>> Ketten = new List<List<Kettenglied>>();

        /// <summary>Projekt, aus dem das Modell stammt.</summary>
        public int ID_Projekt { get; private set; }

        // --- PAKET S2: die Senkenlisten und die Klassen-Sets --------------------------
        //
        // Bis S1 las das Schema die Senke einer Anlage aus WaermesenkeClass.SenkeDaten,
        // also aus den beiden gespiegelten Altspalten. Zwei Dinge gehen darin verloren:
        // die Ränge ab 3 (sie haben keine Altspalte) und die beiden Prozess-Ziele (sie
        // spiegeln sich als „Heizung"). Deshalb liest das Modell die geordneten
        // Senkenlisten; mit Paket A1 ist der Rückfall auf die Altspalten entfallen
        // (Begründung in SenkenlistenLesen).
        //
        // STILL gelesen: Die laute Fassung WaermesenkeClass.SenkenlistenLaden schreibt
        // Protokollzeilen in SimulationProtokoll.Aktuell (Rang-1-Invariante, Puffer-Ziel
        // ohne Puffer). Aus einem Konfigurationsdialog heraus gerufen, landeten sie im
        // Protokoll des NÄCHSTEN Laufs. Die Zeilen kommen deshalb direkt über
        // Z_AnlageSenkeCtrl, die Ladeordnung über SenkenlistenLadenStill - dieselbe
        // Quelle, kein Nebeneffekt.

        /// <summary>Senkenzeilen je Anlagen-ID, in Rangfolge.</summary>
        private readonly Dictionary<int, List<Z_AnlageSenkeModel>> _senken =
            new Dictionary<int, List<Z_AnlageSenkeModel>>();

        /// <summary>Klassen-Set je Puffer-ID (Konzept 6.1).</summary>
        private readonly Dictionary<int, PufferSpCtrl.KlassenSet> _sets =
            new Dictionary<int, PufferSpCtrl.KlassenSet>();

        /// <summary>Die Senkenzeilen einer Anlage in Rangfolge; nie <c>null</c>.</summary>
        private List<Z_AnlageSenkeModel> Senken(int idAnlage)
        {
            List<Z_AnlageSenkeModel> kette;
            return _senken.TryGetValue(idAnlage, out kette)
                ? kette : new List<Z_AnlageSenkeModel>();
        }

        /// <summary>Die Senkenzeile eines Rangs (0-basiert); <c>null</c>, wenn es sie nicht gibt.</summary>
        private Z_AnlageSenkeModel SenkeAufRang(int idAnlage, int index)
        {
            List<Z_AnlageSenkeModel> kette = Senken(idAnlage);
            return index >= 0 && index < kette.Count ? kette[index] : null;
        }

        /// <summary>Bedient der Puffer diesen Kanal (Klassen-Set)? Unbekannte ID: nein.</summary>
        private bool PufferBedient(int idPuffer, int kanal)
        {
            PufferSpCtrl.KlassenSet set;
            if (idPuffer <= 0 || !_sets.TryGetValue(idPuffer, out set) || set == null) return false;

            switch (kanal)
            {
                case Kanal.BRAUCHWASSER: return set.Brauchwasser;
                case Kanal.PROZESS: return set.Prozess;
                default: return set.Heizung;
            }
        }

        /// <summary>
        /// Die Kanäle, die eine DIREKTSENKE bedient — das Anzeige-Gegenstück zu
        /// <c>Kaskadenschleife.SenkenMaske</c>:
        /// <c>Prozesswaerme</c> → {P}; <c>Heizkreis</c> je Bedarfsart
        /// <c>Beides</c> → {H, B}, <c>Warmwasser</c> → {B}, <c>Heizung</c> → {H};
        /// Puffer-Ziele haben keinen direkten Abnehmer.
        /// </summary>
        private static bool DirektsenkeBedient(Z_AnlageSenkeModel z, int kanal)
        {
            if (z == null || WaermesenkeClass.IstPufferZiel(z.Ziel)) return false;

            if (string.Equals(z.Ziel, DbWerte.WS_ZIEL_PROZESS, StringComparison.Ordinal))
                return kanal == Kanal.PROZESS;

            if (kanal == Kanal.PROZESS) return false;

            if (string.Equals(z.Bedarfsart, WaermequelleClass.SENKE_WARMWASSER, StringComparison.Ordinal))
                return kanal == Kanal.BRAUCHWASSER;
            if (string.Equals(z.Bedarfsart, WaermequelleClass.SENKE_HEIZUNG, StringComparison.Ordinal))
                return kanal == Kanal.HEIZUNG;

            return true;                                  // Beides
        }

        /// <summary>true, wenn kein einziger Erzeuger- oder Speicherknoten entstanden ist.</summary>
        public bool IstLeer
        {
            get
            {
                foreach (Knoten k in Knotenliste)
                    if (k.Art == Knotenart.Erzeuger || k.Art == Knotenart.Speicher) return false;
                return true;
            }
        }

        public Knoten Finden(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            foreach (Knoten k in Knotenliste)
                if (string.Equals(k.Schluessel, schluessel, StringComparison.Ordinal)) return k;
            return null;
        }

        /// <summary>Alle Knoten einer Spalte, in Aufbaureihenfolge.</summary>
        public List<Knoten> Spalte(Knotenart art)
        {
            List<Knoten> liste = new List<Knoten>();
            foreach (Knoten k in Knotenliste)
                if (k.Art == art) liste.Add(k);
            return liste;
        }

        // --- Aufbau -------------------------------------------------------------------

        /// <summary>
        /// Baut das Schema eines Projekts.
        /// </summary>
        /// <param name="idProjekt">Projekt; ≤ 0 liefert ein leeres Modell</param>
        /// <param name="kaskade">
        /// Die AUFGENOMMENEN Wärmeerzeuger in Kaskadenreihenfolge (die DB-Werte aus
        /// <c>Tab_Einstellungen.Tool_1..4</c>, wie sie die Kartenansicht liest). Gezeichnet
        /// wird genau das, was gerechnet wird; <c>null</c> = alle Anlagen des Projekts.
        /// </param>
        public static SchemaModell Aufbauen(int idProjekt, IList<string> kaskade)
        {
            SchemaModell m = new SchemaModell();
            m.ID_Projekt = idProjekt;
            if (idProjekt <= 0) return m;

            Hydraulikbild bild = Hydraulikbild.Lesen(idProjekt);
            if (bild == null) return m;

            List<WaermesenkeClass.PufferInfo> puffer =
                WaermesenkeClass.ProjektPufferListe(idProjekt, null);
            if (puffer == null) puffer = new List<WaermesenkeClass.PufferInfo>();

            Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId =
                new Dictionary<int, WaermesenkeClass.PufferInfo>();
            foreach (WaermesenkeClass.PufferInfo p in puffer)
                if (p != null && !pufferJeId.ContainsKey(p.ID)) pufferJeId[p.ID] = p;

            // Rang je Erzeugerart aus der Kaskadenreihenfolge; ohne Vorgabe kein Rang.
            Dictionary<int, int> rangJeTyp = new Dictionary<int, int>();
            if (kaskade != null)
                for (int i = 0; i < kaskade.Count; i++)
                {
                    int typ = TypZuDbWert(kaskade[i]);
                    if (typ > 0 && !rangJeTyp.ContainsKey(typ)) rangJeTyp[typ] = i + 1;
                }

            bool hatBrauchwasser = WaermesenkeClass.ProjektHatBrauchwasser(idProjekt);
            bool hatProzess = ProjektHatProzesswaerme(idProjekt);

            // Anlagen, die gezeichnet werden: die aufgenommenen Arten (oder alle).
            List<Hydraulikbild.AnlagenEintrag> anlagen = new List<Hydraulikbild.AnlagenEintrag>();
            foreach (Hydraulikbild.AnlagenEintrag a in bild.Anlagen)
            {
                if (kaskade != null && !rangJeTyp.ContainsKey(a.ID_Type)) continue;
                anlagen.Add(a);
            }

            // PAKET S2: Senkenlisten (alle Ränge, beide Prozess-Ziele) und Klassen-Sets.
            m.SenkenlistenLesen(idProjekt, anlagen);
            m.KlassenSetsLesen(idProjekt);

            // Quellpuffer je Anlage in der ANZEIGE-Auflösung (Karte und Schema gleich).
            //
            // OHNE Einschränkung auf die Wärmepumpe: Der HEIZKESSEL kann seit Etappe D5a
            // ebenso einen Pufferspeicher als Wärmequelle führen (Konzept 8.4), und die
            // Kette „… → Puffer → Kessel → …" wird hier deshalb genauso gezeichnet wie
            // bei der Wärmepumpe. Die Auswahl trifft Hydraulikbild.QuellpufferAnzeige
            // über WQ_Typ — sie kennt keinen Anlagentyp.
            Dictionary<int, int> quellpuffer = new Dictionary<int, int>();
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                int q = bild.QuellpufferAnzeige(a.ID, puffer);
                if (q > 0 && pufferJeId.ContainsKey(q)) quellpuffer[a.ID] = q;
            }

            m.SpeicherKnotenAnlegen(anlagen, puffer, quellpuffer, hatBrauchwasser, hatProzess);
            m.ErzeugerKnotenAnlegen(bild, anlagen, pufferJeId, quellpuffer, rangJeTyp);
            m.KantenAnlegen(idProjekt, anlagen, quellpuffer, hatBrauchwasser, hatProzess);
            m.KettenAbleiten(anlagen, pufferJeId, quellpuffer, hatBrauchwasser);

            return m;
        }

        /// <summary>
        /// true, wenn dem Projekt mindestens ein Prozesswärme-Anteil zugeordnet ist —
        /// das Gegenstück zu <c>WaermesenkeClass.ProjektHatBrauchwasser</c> und dieselbe
        /// Vorsichtsregel: Ist die Abfrage nicht auswertbar (fehlende Tabelle), gilt
        /// „vorhanden", damit kein Knoten aus Unkenntnis verschwindet.
        /// </summary>
        private static bool ProjektHatProzesswaerme(int idProjekt)
        {
            object v = StilleDb.Scalar(
                "SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", System.Data.OleDb.OleDbType.Integer, idProjekt));

            if (v == null) return true;
            return StilleDb.Zahl(v) > 0;
        }

        /// <summary>
        /// Liest die geordneten Senkenlisten des Projekts aus <c>Z_AnlageSenke</c>.
        ///
        /// <para><b>PAKET A1:</b> Der Rückfall auf die beiden Altslots aus
        /// <see cref="Hydraulikbild"/> ist entfallen — dieselbe Entscheidung wie in
        /// <c>WaermesenkeClass.SenkenlistenLaden</c>. Eine Anlage ohne Zeile bekommt die
        /// RANG-1-VORBELEGUNG <c>Heizkreis/Beides</c>, also genau das, was auch die Engine
        /// für sie rechnet; das Schema zeigt damit keinen Speicher mehr an, den kein Lauf
        /// mehr lädt.</para>
        /// </summary>
        private void SenkenlistenLesen(int idProjekt,
                                       List<Hydraulikbild.AnlagenEintrag> anlagen)
        {
            foreach (Z_AnlageSenkeModel z in new Z_AnlageSenkeCtrl().LesenJeProjekt(idProjekt))
            {
                if (z == null || z.ID_Anlage <= 0) continue;

                List<Z_AnlageSenkeModel> kette;
                if (!_senken.TryGetValue(z.ID_Anlage, out kette))
                {
                    kette = new List<Z_AnlageSenkeModel>();
                    _senken[z.ID_Anlage] = kette;
                }
                kette.Add(z);
            }

            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                if (_senken.ContainsKey(a.ID)) continue;

                List<Z_AnlageSenkeModel> kette = new List<Z_AnlageSenkeModel>();
                kette.Add(new Z_AnlageSenkeModel
                {
                    ID_Anlage = a.ID,
                    Rang = 1,
                    Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                    Bedarfsart = WaermequelleClass.SENKE_BEIDES
                });

                _senken[a.ID] = kette;
            }
        }

        /// <summary>Klassen-Set je Projekt-Puffer (Konzept 6.1) — EINE Abfrage.</summary>
        private void KlassenSetsLesen(int idProjekt)
        {
            foreach (KeyValuePair<int, PufferSpCtrl.KlassenSet> e in
                     PufferSpCtrl.KlassenSetsJeProjekt(idProjekt))
                _sets[e.Key] = e.Value;
        }

        /// <summary><c>Tab_Energieanlagen.ID_Type</c> zu einem Erzeuger-DB-Wert; 0 = unbekannt.</summary>
        private static int TypZuDbWert(string dbWert)
        {
            switch (dbWert)
            {
                case DbWerte.ERZEUGER_WAERMEPUMPE: return ProjektPuffer.TYP_WP;
                case DbWerte.ERZEUGER_HEIZKESSEL: return ProjektPuffer.TYP_KESSEL;
                case DbWerte.ERZEUGER_BHKW: return ProjektPuffer.TYP_BHKW;
                case DbWerte.ERZEUGER_SOLARTHERMIE: return ProjektPuffer.TYP_SOLARTHERMIE;
                default: return 0;
            }
        }

        // --- Knoten -------------------------------------------------------------------

        /// <summary>
        /// Speicherknoten: NUR Puffer, die im Schema wirklich vorkommen — geladen,
        /// als Quelle genutzt oder Zweitsenke.
        ///
        /// Der Filter ist nötig und nicht kosmetisch: Projekt 1023 der Arbeitskopie führt
        /// 79 Puffer-Zeilen, von denen genau EINE an der Hydraulik teilnimmt. Ein Schema
        /// mit 79 Kästen wäre unlesbar und würde 78 Speicher behaupten, die kein Erzeuger
        /// bedient.
        /// </summary>
        private void SpeicherKnotenAnlegen(List<Hydraulikbild.AnlagenEintrag> anlagen,
                                           List<WaermesenkeClass.PufferInfo> puffer,
                                           Dictionary<int, int> quellpuffer,
                                           bool hatBrauchwasser, bool hatProzess)
        {
            // PAKET S2: über ALLE Senkenzeilen, nicht mehr über die zwei Altslots — ein
            // Speicher, den erst Rang 3 lädt, fehlte bis hierher im Schema.
            HashSet<int> beteiligt = new HashSet<int>();
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
                foreach (Z_AnlageSenkeModel z in Senken(a.ID))
                    if (z != null && z.ID_Puffer > 0 && WaermesenkeClass.IstPufferZiel(z.Ziel))
                        beteiligt.Add(z.ID_Puffer);

            foreach (KeyValuePair<int, int> q in quellpuffer) beteiligt.Add(q.Value);

            foreach (WaermesenkeClass.PufferInfo p in puffer)
            {
                if (p == null || !beteiligt.Contains(p.ID)) continue;

                Knoten k = new Knoten();
                k.Schluessel = PRAEFIX_SPEICHER + p.ID;
                k.Art = Knotenart.Speicher;
                k.ID = p.ID;
                k.Titel = p.Bezeichner.Length > 0
                    ? p.Bezeichner : MyResource.Resource.PSP_BEZEICHNER_ERSATZ;

                if (p.Gesamtvolumen > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_VOLUMEN, p.Gesamtvolumen));
                if (p.Vorlauf > 0 && p.Ruecklauf > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                               p.Vorlauf, p.Ruecklauf));

                // PAKET S2: EIN BADGE JE KANAL DES KLASSEN-SETS (Konzept 10) statt eines
                // Badges je Verwendung. Ein Kombispeicher trägt damit weiterhin beide
                // Badges (Mockup „Puffer Kombi"), ein {H, P}-Speicher jetzt aber auch
                // seine Prozess-Kennzeichnung - die Verwendung kennt sie nicht.
                if (PufferBedient(p.ID, Kanal.HEIZUNG))
                    k.Badges.Add(MyResource.Resource.PSP_VERWENDUNG_HEIZUNG_ANZEIGE);
                if (PufferBedient(p.ID, Kanal.BRAUCHWASSER))
                    k.Badges.Add(MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER);
                if (PufferBedient(p.ID, Kanal.PROZESS))
                    k.Badges.Add(MyResource.Resource.KANAL_PROZESS_ANZEIGE);

                PufferSpCtrl.KlassenSet set;
                _sets.TryGetValue(p.ID, out set);
                k.Hinweis = string.Format(MyResource.Resource.PSP_KARTE_VERSORGT,
                                          Warnkriterien.KlassenSetAnzeige(set));
                Knotenliste.Add(k);
            }

            // Abnehmerknoten: nur die, die auch bedient werden (siehe KantenAnlegen).
            if (BedientKanal(anlagen, Kanal.HEIZUNG))
                Knotenliste.Add(new Knoten
                {
                    Schluessel = ABNEHMER_HEIZKREIS,
                    Art = Knotenart.Abnehmer,
                    Titel = MyResource.Resource.SIM_HEIZKREIS,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                });

            if (hatBrauchwasser && BedientKanal(anlagen, Kanal.BRAUCHWASSER))
                Knotenliste.Add(new Knoten
                {
                    Schluessel = ABNEHMER_WARMWASSER,
                    Art = Knotenart.Abnehmer,
                    Titel = MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                });

            if (hatProzess && BedientKanal(anlagen, Kanal.PROZESS))
                Knotenliste.Add(new Knoten
                {
                    Schluessel = ABNEHMER_PROZESS,
                    Art = Knotenart.Abnehmer,
                    Titel = MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                    Hinweis = MyResource.Resource.SIM_SCHEMA_TIP_ABNEHMER
                });
        }

        /// <summary>
        /// Wird dieser KANAL im Projekt überhaupt bedient — unmittelbar durch eine
        /// Direktsenke oder mittelbar über einen Speicher, dessen Klassen-Set ihn führt?
        ///
        /// <para>PAKET S2: Eine Regel für alle drei Kanäle statt zweier Sonderfassungen
        /// für Heizkreis und Warmwasser. Die Aussage ist für Heizung und Warmwasser
        /// dieselbe wie zuvor — nur dass sie jetzt über ALLE Senkenränge läuft und den
        /// Speicher über sein Klassen-Set statt über die Alt-Verwendung fragt.</para>
        /// </summary>
        private bool BedientKanal(List<Hydraulikbild.AnlagenEintrag> anlagen, int kanal)
        {
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
                foreach (Z_AnlageSenkeModel z in Senken(a.ID))
                {
                    if (z == null) continue;

                    if (WaermesenkeClass.IstPufferZiel(z.Ziel))
                    {
                        if (PufferBedient(z.ID_Puffer, kanal)) return true;
                    }
                    else if (DirektsenkeBedient(z, kanal)) return true;
                }

            return false;
        }

        /// <summary>
        /// Erzeugerknoten und — sofern die Quelle KEIN Puffer ist — der zugehörige
        /// Quellknoten. Bei Puffer-Quelle entsteht kein eigener Quellkasten: Die Kaskade
        /// kommt sichtbar aus dem Speicher (Invariante S-1, Mockup „aus Puffer Heizung").
        /// </summary>
        private void ErzeugerKnotenAnlegen(Hydraulikbild bild,
                                           List<Hydraulikbild.AnlagenEintrag> anlagen,
                                           Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                           Dictionary<int, int> quellpuffer,
                                           Dictionary<int, int> rangJeTyp)
        {
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                Knoten k = new Knoten();
                k.Schluessel = PRAEFIX_ERZEUGER + a.ID;
                k.Art = Knotenart.Erzeuger;
                k.ID = a.ID;
                k.ID_Type = a.ID_Type;
                k.Titel = a.Bezeichner.Length > 0 ? a.Bezeichner : Ladeordnung.ErzeugerName(a.ID_Type);

                int rang;
                if (rangJeTyp.TryGetValue(a.ID_Type, out rang)) k.Rang = rang.ToString();

                k.Zeilen.Add(Ladeordnung.ErzeugerName(a.ID_Type));
                if (a.Vorlauf > 0 && a.Ruecklauf > 0)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                               a.Vorlauf, a.Ruecklauf));
                // PAKET A1: Ziel und Zielspeicher kommen aus der SENKENLISTE (Rang 1) und
                // nicht mehr aus der gespiegelten Altspaltensicht — nur so stehen auch die
                // beiden Prozess-Ziele richtig da (sie spiegelten sich als „Heizung").
                Z_AnlageSenkeModel rang1 = SenkeAufRang(a.ID, 0);
                k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_SENKE,
                                           Form_Waermesenke.SenkeAnzeige(rang1)));

                // Temperatur-Warnregel (Konzept Abschnitt 5), Wort für Wort die Regel der
                // Erzeugerkarte: Erzeuger-Vorlauf < Puffer-Vorlauf der Hauptsenke.
                WaermesenkeClass.PufferInfo senke = null;
                if (rang1 != null && WaermesenkeClass.IstPufferZiel(rang1.Ziel) && rang1.ID_Puffer > 0)
                    pufferJeId.TryGetValue(rang1.ID_Puffer, out senke);

                if (senke != null && senke.Vorlauf > 0 && a.Vorlauf > 0 && a.Vorlauf < senke.Vorlauf)
                {
                    k.Warnung = true;
                    k.Warntext = string.Format(
                        MyResource.Resource.SIM_KARTE_TIP_TEMPERATUR_WARNUNG,
                        a.Vorlauf, senke.Bezeichner, senke.Vorlauf);
                }

                int idQuelle;
                bool kaskade = quellpuffer.TryGetValue(a.ID, out idQuelle);
                k.Kaskade = kaskade;
                if (kaskade)
                    k.Zeilen.Add(string.Format(MyResource.Resource.SIM_KARTE_QUELLE_KASKADE,
                                               PufferTitel(idQuelle, pufferJeId)));

                k.Hinweis = string.Join(Environment.NewLine, k.Zeilen.ToArray());
                Knotenliste.Add(k);

                if (kaskade) continue;   // die Quelle ist der Speicherknoten

                string quelltext = Quelltext(a);
                if (quelltext.Length == 0) continue;

                Knotenliste.Add(new Knoten
                {
                    Schluessel = PRAEFIX_QUELLE + a.ID,
                    Art = Knotenart.Quelle,
                    ID = a.ID,
                    ID_Type = a.ID_Type,
                    Titel = quelltext,
                    Hinweis = string.Format(MyResource.Resource.SIM_KARTE_QUELLE, quelltext)
                });
            }
        }

        /// <summary>
        /// Text des Quellkastens je Erzeugerart.
        ///
        /// Für die Wärmepumpe ist das WORTGLEICH die Quellenanzeige der Karte
        /// (<see cref="WaermequelleClass.QuelleAnzeige"/>) — Liste und Schema dürfen die
        /// Quelle nicht verschieden benennen. Die übrigen Arten haben keine wählbare
        /// Quelle (Konzept Anforderung 5); für sie steht dort, woher die Wärme physikalisch
        /// kommt: Solarstrahlung, Brennstoff, Systemrücklauf.
        /// </summary>
        private string Quelltext(Hydraulikbild.AnlagenEintrag a)
        {
            switch (a.ID_Type)
            {
                case ProjektPuffer.TYP_WP:
                    return WaermequelleClass.QuelleAnzeige(ID_Projekt, a.ID, a.WpTyp, a.WQ_Typ, a.WQ_Temp);

                case ProjektPuffer.TYP_SOLARTHERMIE:
                    return MyResource.Resource.SIM_SCHEMA_QUELLE_SOLARSTRAHLUNG;

                case ProjektPuffer.TYP_KESSEL:
                    return MyResource.Resource.SIMQ_QUELLE_SYSTEMRUECKLAUF;

                case ProjektPuffer.TYP_BHKW:
                    return MyResource.Resource.SIM_SCHEMA_QUELLE_BRENNSTOFF;

                default:
                    return "";
            }
        }

        private static string PufferTitel(int idPuffer,
                                          Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId)
        {
            WaermesenkeClass.PufferInfo p;
            if (idPuffer > 0 && pufferJeId.TryGetValue(idPuffer, out p) && p.Bezeichner.Length > 0)
                return p.Bezeichner;
            return MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;
        }

        // --- Kanten -------------------------------------------------------------------

        private void KantenAnlegen(int idProjekt,
                                   List<Hydraulikbild.AnlagenEintrag> anlagen,
                                   Dictionary<int, int> quellpuffer,
                                   bool hatBrauchwasser, bool hatProzess)
        {
            // Ladepositionen EINMAL je beteiligtem Puffer holen - Ladereihenfolge fragt je
            // Aufruf Anlagen und Kaskadenplätze neu ab (Begründung wie in
            // Form_Simulation_Config.SpeicherKarteDaten).
            //
            // PAKET A1: Die Ladeordnung liest die Senkenlisten (ab Rang 3 trägt die Kante
            // erst dadurch ihre Kreisziffer, S2-O6). Sie werden hier EINMAL still geholt
            // und in jeden Aufruf hineingereicht - sonst läse jeder Speicherknoten sie
            // erneut.
            List<Senkenliste> senken = WaermesenkeClass.SenkenlistenLadenStill(idProjekt);

            Dictionary<int, List<Ladeordnung.LadeEintrag>> ordnung =
                new Dictionary<int, List<Ladeordnung.LadeEintrag>>();

            foreach (Knoten k in Knotenliste)
            {
                if (k.Art != Knotenart.Speicher || ordnung.ContainsKey(k.ID)) continue;
                ordnung[k.ID] = Ladeordnung.Ladereihenfolge(idProjekt, k.ID, senken);
            }

            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                string erzeuger = PRAEFIX_ERZEUGER + a.ID;

                // Quellseite
                int idQuelle;
                if (quellpuffer.TryGetValue(a.ID, out idQuelle))
                    Verbinden(PRAEFIX_SPEICHER + idQuelle, erzeuger, Kantenart.Kaskade, 0,
                              MyResource.Resource.SIM_KARTE_TIP_KASKADE);
                else
                    Verbinden(PRAEFIX_QUELLE + a.ID, erzeuger, Kantenart.Quelle, 0, "");

                // PAKET S2: die SENKENKETTE statt Haupt-/Zweitsenke — jede Zeile bekommt
                // ihre Kante. Rang 1 zeichnet wie bisher die Hauptsenke, alles darüber
                // gilt als Zweitsenke im Sinn der Ladeordnung (dort trennt ein einzelnes
                // Kennzeichen nur „erste" von „weitere").
                List<Z_AnlageSenkeModel> kette = Senken(a.ID);
                for (int i = 0; i < kette.Count; i++)
                {
                    Z_AnlageSenkeModel z = kette[i];
                    if (z == null) continue;

                    if (WaermesenkeClass.IstPufferZiel(z.Ziel) && z.ID_Puffer > 0)
                        LadekanteAnlegen(erzeuger, z.ID_Puffer, a.ID, i > 0, ordnung);
                    else
                        DirektkanteAnlegen(erzeuger, z, hatBrauchwasser, hatProzess);
                }
            }

            // Versorgung: jeder Speicher bedient die Kanäle seines KLASSEN-SETS.
            foreach (Knoten k in Knotenliste)
            {
                if (k.Art != Knotenart.Speicher) continue;

                if (PufferBedient(k.ID, Kanal.HEIZUNG))
                    Verbinden(k.Schluessel, ABNEHMER_HEIZKREIS, Kantenart.Versorgung, 0, "");
                if (PufferBedient(k.ID, Kanal.BRAUCHWASSER))
                    Verbinden(k.Schluessel, ABNEHMER_WARMWASSER, Kantenart.Versorgung, 0, "");
                // PAKET E1 (S2-O7): eigene Kantenart für den Prozessabnehmer.
                if (PufferBedient(k.ID, Kanal.PROZESS))
                    Verbinden(k.Schluessel, ABNEHMER_PROZESS, Kantenart.Prozess, 0, "");
            }
        }

        private void LadekanteAnlegen(string erzeuger, int idPuffer, int idAnlage, bool zweitsenke,
                                      Dictionary<int, List<Ladeordnung.LadeEintrag>> ordnung)
        {
            List<Ladeordnung.LadeEintrag> liste;
            int position = 0;
            if (ordnung.TryGetValue(idPuffer, out liste) && liste != null)
                position = Ladeordnung.Position(liste, idAnlage, zweitsenke);

            string hinweis = "";
            if (position > 0 && liste != null)
                hinweis = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS,
                                        position, liste.Count);
            if (zweitsenke)
                hinweis = (hinweis.Length > 0 ? hinweis + " · " : "") +
                          MyResource.Resource.SIM_ROLLE_ZWEITSENKE;

            Verbinden(erzeuger, PRAEFIX_SPEICHER + idPuffer, Kantenart.Ladung, position, hinweis);
        }

        /// <summary>
        /// Direkte Deckung: Erzeuger → Abnehmer, je nach Ziel und Bedarfsart der
        /// Senkenzeile (PAKET S2 — das dritte Ziel <c>Prozesswaerme</c> ist dazugekommen).
        /// </summary>
        private void DirektkanteAnlegen(string erzeuger, Z_AnlageSenkeModel z,
                                        bool hatBrauchwasser, bool hatProzess)
        {
            if (DirektsenkeBedient(z, Kanal.HEIZUNG))
                Verbinden(erzeuger, ABNEHMER_HEIZKREIS, Kantenart.Versorgung, 0, "");

            if (hatBrauchwasser && DirektsenkeBedient(z, Kanal.BRAUCHWASSER))
                Verbinden(erzeuger, ABNEHMER_WARMWASSER, Kantenart.Versorgung, 0, "");

            // PAKET E1 (S2-O7): eigene Kantenart für den Prozessabnehmer.
            if (hatProzess && DirektsenkeBedient(z, Kanal.PROZESS))
                Verbinden(erzeuger, ABNEHMER_PROZESS, Kantenart.Prozess, 0, "");
        }

        /// <summary>
        /// Legt eine Kante an — aber nur, wenn BEIDE Knoten existieren. Ein Bezug auf
        /// einen Puffer, den es im Projekt nicht (mehr) gibt, darf keine Linie ins Leere
        /// zeichnen; die Karte weist denselben Fall als Rückfall auf den Heizkreis aus.
        /// </summary>
        private void Verbinden(string von, string nach, Kantenart art, int prio, string hinweis)
        {
            if (Finden(von) == null || Finden(nach) == null) return;

            foreach (Kante vorhanden in Kantenliste)
                if (string.Equals(vorhanden.Von, von, StringComparison.Ordinal) &&
                    string.Equals(vorhanden.Nach, nach, StringComparison.Ordinal) &&
                    vorhanden.Art == art)
                    return;

            Kantenliste.Add(new Kante
            {
                Von = von, Nach = nach, Art = art, Prioritaet = prio, Hinweis = hinweis ?? ""
            });
        }

        // --- Kaskadenkette ------------------------------------------------------------

        /// <summary>Höchstzahl gezeigter Ketten — mehr wäre kein Band mehr, sondern eine Liste.</summary>
        private const int MAX_KETTEN = 6;

        /// <summary>
        /// Leitet die Kaskadenketten ab (Konzept Abschnitt 3, Mockup Abschnitt 2):
        /// „Erdsonde → WP 1 → Puffer 1 → WP 2 Booster → Puffer 2 → Warmwasser".
        ///
        /// Eine Kette beginnt bei einem Erzeuger OHNE Quellpuffer, der einen Speicher lädt,
        /// aus dem ein anderer Erzeuger seine Quellwärme bezieht; sie folgt dem Weg
        /// Erzeuger → Speicher → Erzeuger → … bis zum letzten Speicher und endet beim
        /// Abnehmer. Zwischen zwei Speichern steht damit immer ein Erzeuger — genau die
        /// Darstellungsvorgabe der Invariante S-1.
        ///
        /// Ohne Quellbezug im Projekt entsteht keine Kette (das ist der Regelfall).
        /// </summary>
        private void KettenAbleiten(List<Hydraulikbild.AnlagenEintrag> anlagen,
                                    Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                    Dictionary<int, int> quellpuffer,
                                    bool hatBrauchwasser)
        {
            if (quellpuffer.Count == 0) return;

            // Puffer -> Erzeuger, die ihn als Quelle nutzen.
            Dictionary<int, List<Hydraulikbild.AnlagenEintrag>> nutzer =
                new Dictionary<int, List<Hydraulikbild.AnlagenEintrag>>();
            foreach (Hydraulikbild.AnlagenEintrag a in anlagen)
            {
                int q;
                if (!quellpuffer.TryGetValue(a.ID, out q)) continue;
                if (!nutzer.ContainsKey(q)) nutzer[q] = new List<Hydraulikbild.AnlagenEintrag>();
                nutzer[q].Add(a);
            }

            foreach (Hydraulikbild.AnlagenEintrag start in anlagen)
            {
                if (quellpuffer.ContainsKey(start.ID)) continue;          // kein Kettenanfang
                int idPuffer = HauptsenkePuffer(start);
                if (idPuffer <= 0 || !nutzer.ContainsKey(idPuffer)) continue;

                List<Kettenglied> kette = new List<Kettenglied>();

                string quelltext = Quelltext(start);
                if (quelltext.Length > 0)
                    kette.Add(new Kettenglied
                    {
                        Schluessel = PRAEFIX_QUELLE + start.ID,
                        Text = quelltext,
                        Art = Knotenart.Quelle
                    });

                kette.Add(Glied(start, Kantenart.Quelle));
                KetteFortsetzen(kette, idPuffer, nutzer, pufferJeId, quellpuffer,
                                hatBrauchwasser, new HashSet<int>());
            }
        }

        private void KetteFortsetzen(List<Kettenglied> kette, int idPuffer,
                                     Dictionary<int, List<Hydraulikbild.AnlagenEintrag>> nutzer,
                                     Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                     Dictionary<int, int> quellpuffer,
                                     bool hatBrauchwasser,
                                     HashSet<int> besucht)
        {
            if (Ketten.Count >= MAX_KETTEN) return;

            // Ringschutz: derselbe Speicher darf in einer Kette nur einmal vorkommen. Die
            // Engine bricht bei einem Ring ab und der Dialog verhindert ihn (D5b) - hier
            // geht es allein darum, dass die ANZEIGE eines Altbestands nicht endlos läuft.
            if (!besucht.Add(idPuffer)) { Ketten.Add(kette); return; }

            kette.Add(new Kettenglied
            {
                Schluessel = PRAEFIX_SPEICHER + idPuffer,
                Text = PufferTitel(idPuffer, pufferJeId),
                Art = Knotenart.Speicher,
                PfeilDavor = Kantenart.Ladung
            });

            List<Hydraulikbild.AnlagenEintrag> folger;
            if (!nutzer.TryGetValue(idPuffer, out folger) || folger.Count == 0)
            {
                KetteAbschliessen(kette, idPuffer, pufferJeId, hatBrauchwasser);
                return;
            }

            for (int i = 0; i < folger.Count; i++)
            {
                Hydraulikbild.AnlagenEintrag b = folger[i];

                // Jeder Zweig bekommt eine eigene Kopie - sonst wüchse die erste Kette
                // um die Glieder aller Geschwister.
                List<Kettenglied> zweig = new List<Kettenglied>(kette);
                zweig.Add(Glied(b, Kantenart.Kaskade));

                int weiter = HauptsenkePuffer(b);
                if (weiter > 0)
                    KetteFortsetzen(zweig, weiter, nutzer, pufferJeId, quellpuffer,
                                    hatBrauchwasser, new HashSet<int>(besucht));
                else
                    DirektAbschliessen(zweig, b, hatBrauchwasser);
            }
        }

        private void KetteAbschliessen(List<Kettenglied> kette, int idPuffer,
                                       Dictionary<int, WaermesenkeClass.PufferInfo> pufferJeId,
                                       bool hatBrauchwasser)
        {
            // PAKET S2: über das KLASSEN-SET statt über die Alt-Verwendung; die Kette
            // endet am ERSTEN Kanal, den der Speicher bedient (das Band zeigt einen Weg,
            // keine Verzweigung).
            if (PufferBedient(idPuffer, Kanal.HEIZUNG))
                kette.Add(new Kettenglied
                {
                    Schluessel = ABNEHMER_HEIZKREIS,
                    Text = MyResource.Resource.SIM_HEIZKREIS,
                    Art = Knotenart.Abnehmer,
                    PfeilDavor = Kantenart.Versorgung
                });
            else if (hatBrauchwasser && PufferBedient(idPuffer, Kanal.BRAUCHWASSER))
                kette.Add(new Kettenglied
                {
                    Schluessel = ABNEHMER_WARMWASSER,
                    Text = MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER,
                    Art = Knotenart.Abnehmer,
                    PfeilDavor = Kantenart.Versorgung
                });
            else if (PufferBedient(idPuffer, Kanal.PROZESS))
                kette.Add(new Kettenglied
                {
                    Schluessel = ABNEHMER_PROZESS,
                    Text = MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                    Art = Knotenart.Abnehmer,
                    // PAKET E1 (S2-O7): eigene Kantenart, auch im Kaskadenband.
                    PfeilDavor = Kantenart.Prozess
                });

            Ketten.Add(kette);
        }

        private void DirektAbschliessen(List<Kettenglied> kette,
                                        Hydraulikbild.AnlagenEintrag a, bool hatBrauchwasser)
        {
            bool warmwasser = hatBrauchwasser &&
                              string.Equals(a.Senke.Bedarfsart, WaermequelleClass.SENKE_WARMWASSER,
                                            StringComparison.Ordinal);

            kette.Add(new Kettenglied
            {
                Schluessel = warmwasser ? ABNEHMER_WARMWASSER : ABNEHMER_HEIZKREIS,
                Text = warmwasser
                    ? MyResource.Resource.SIM_SCHEMA_ABNEHMER_WARMWASSER
                    : MyResource.Resource.SIM_HEIZKREIS,
                Art = Knotenart.Abnehmer,
                PfeilDavor = Kantenart.Versorgung
            });

            Ketten.Add(kette);
        }

        private static Kettenglied Glied(Hydraulikbild.AnlagenEintrag a, Kantenart pfeil)
        {
            return new Kettenglied
            {
                Schluessel = PRAEFIX_ERZEUGER + a.ID,
                Text = a.Bezeichner.Length > 0 ? a.Bezeichner : Ladeordnung.ErzeugerName(a.ID_Type),
                Art = Knotenart.Erzeuger,
                PfeilDavor = pfeil
            };
        }

        /// <summary>
        /// Der Speicher, den die Anlage auf RANG 1 lädt; 0, wenn Rang 1 eine Direktsenke
        /// ist. PAKET S2: aus der Senkenliste statt aus der Altspalte — sonst hinge die
        /// Kaskadenkette an einer Spiegelung, die die Prozess-Ziele nicht abbilden kann.
        /// </summary>
        private int HauptsenkePuffer(Hydraulikbild.AnlagenEintrag a)
        {
            List<Z_AnlageSenkeModel> kette = Senken(a.ID);
            if (kette.Count == 0) return 0;

            Z_AnlageSenkeModel z = kette[0];
            return WaermesenkeClass.IstPufferZiel(z.Ziel) ? z.ID_Puffer : 0;
        }

        // --- Selbstprüfung ------------------------------------------------------------

        /// <summary>
        /// Prüft das Modell gegen die Entwurfsregeln; nie <c>null</c>, leer = in Ordnung.
        ///
        /// Zweck ist das Prüfprogramm der Etappe (Modell testen, nicht Pixel):
        /// <list type="number">
        ///   <item><description>Invariante S-1 — keine Kante Speicher → Speicher.</description></item>
        ///   <item><description>Jede Kante hat beide Endknoten.</description></item>
        ///   <item><description>Kein Knotenschlüssel doppelt.</description></item>
        ///   <item><description>Zwischen zwei Speichergliedern einer Kaskadenkette steht ein
        ///     Erzeugerglied.</description></item>
        /// </list>
        /// </summary>
        public List<string> Pruefen()
        {
            List<string> fehler = new List<string>();

            HashSet<string> gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Knoten k in Knotenliste)
                if (!gesehen.Add(k.Schluessel)) fehler.Add("Knoten doppelt: " + k.Schluessel);

            foreach (Kante e in Kantenliste)
            {
                Knoten von = Finden(e.Von);
                Knoten nach = Finden(e.Nach);
                if (von == null) { fehler.Add("Kante ohne Startknoten: " + e.Von); continue; }
                if (nach == null) { fehler.Add("Kante ohne Zielknoten: " + e.Nach); continue; }

                if (von.Art == Knotenart.Speicher && nach.Art == Knotenart.Speicher)
                    fehler.Add("Invariante S-1 verletzt: " + e.Von + " -> " + e.Nach);
            }

            foreach (List<Kettenglied> kette in Ketten)
                for (int i = 1; i < kette.Count; i++)
                    if (kette[i].Art == Knotenart.Speicher && kette[i - 1].Art == Knotenart.Speicher)
                        fehler.Add("Invariante S-1 verletzt (Kette): " +
                                   kette[i - 1].Schluessel + " -> " + kette[i].Schluessel);

            return fehler;
        }
    }
}
