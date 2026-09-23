using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// SENKEN BEIM ANLEGEN EINER ERZEUGERANLAGE (Anwenderentscheid 23.09.2026, Punkt 2).
    ///
    /// <para><b>Der Befund.</b> Eine neu angelegte Anlage bekam bis hierher KEINE Zeile in
    /// <c>Z_AnlageSenke</c>. Der Lauf nahm dann die Vorbelegung Heizkreis (Heizung +
    /// Warmwasser) (<c>Senkenliste.Vorbelegung</c>) — und die bedient den Kanal
    /// PROZESSWÄRME nicht. In einem Projekt mit Prozesswärme deckte eine frisch angelegte
    /// Solarthermie oder Wärmepumpe diesen Bedarf deshalb nie, ohne dass es jemand gewählt
    /// hätte (Projekte „Test: Prozesswärme+ST", Welle #441).</para>
    ///
    /// <para><b>Die Regel.</b> Beim Anlegen entstehen die Senkenzeilen im selben Schritt,
    /// abgeleitet aus den BEDARFSKANÄLEN des Projekts (<see cref="KanaeleMitBedarf"/>):
    /// <list type="bullet">
    /// <item>Heizung oder Warmwasser mit Bedarf → Direktsenke Heizkreis (Heizung +
    ///   Warmwasser), Rang 1 — dieselbe Zeile wie die heutige Vorbelegung;</item>
    /// <item>Prozesswärme mit Bedarf → zusätzlich (bzw. allein) die Direktsenke
    ///   Prozesswärme auf dem nächsten Rang;</item>
    /// <item>kein Bedarf im Projekt → KEINE Zeile. Die Vorbelegung im Lauf bleibt der
    ///   Rückfall für den Altbestand und für einen Bedarf, der erst später dazukommt.</item>
    /// </list></para>
    ///
    /// <para><b>Nur Wärmeerzeuger</b> (<see cref="IstWaermeerzeuger"/>): Wärmepumpe,
    /// Solarthermie, Heizkessel (auch der Elektrokessel — er ist ein Heizkessel mit
    /// Stromträger) und BHKW — genau die Typen, für die der Lauf eine Senkenliste führt
    /// (<c>ProjektPuffer.WAERMEERZEUGER_TYPEN</c>). Pufferspeicher sind selbst ZIEL einer
    /// Senke und bekommen keine; Referenzanlagen, Photovoltaik und Stromspeicher haben
    /// keinen Wärmekanal. Die Wärmepumpe wird wie jeder andere Erzeuger behandelt: Ihre
    /// Besonderheiten (Quellpuffer, Booster) hängen an der QUELLE, nicht an der Senke.</para>
    ///
    /// <para><b>Nur beim Anlegen.</b> Eine BESTEHENDE Anlage behält, was sie hat — auch
    /// keine Zeile. Wer die Senken ändern will, tut es im Senkendialog. Der Rechenweg bleibt
    /// unberührt: Die Engine liest dieselben Zeilen wie bisher, und für eine Anlage ohne
    /// Zeile gilt weiter die Vorbelegung.</para>
    ///
    /// <para>Dialogfrei (Konzept 13.4); gelesen wird über <c>StilleDb</c> und damit — unter
    /// einer <c>Vorgangsklammer</c> — auf der Verbindung des laufenden Speichervorgangs, so
    /// dass auch ein im selben Vorgang geschriebener Bedarf zählt.</para>
    /// </summary>
    public static class Senkenvorbelegung
    {
        /// <summary>Trenner der Senkenliste in der Anzeigezeile.</summary>
        private const string TRENNER = "; ";

        // =====================================================================
        // Welche Anlagen, welcher Bedarf
        // =====================================================================

        /// <summary>
        /// true für die Anlagentypen mit Senkenliste — Wärmepumpe, Solarthermie, Heizkessel,
        /// BHKW (<c>ProjektPuffer.WAERMEERZEUGER_TYPEN</c>).
        /// </summary>
        public static bool IstWaermeerzeuger(int idType)
        {
            return idType == ProjektPuffer.TYP_WP ||
                   idType == ProjektPuffer.TYP_SOLARTHERMIE ||
                   idType == ProjektPuffer.TYP_KESSEL ||
                   idType == ProjektPuffer.TYP_BHKW;
        }

        /// <summary>
        /// Die Bedarfskanäle des Projekts, in denen ein Bedarf ZUGEORDNET ist — Feld der
        /// Länge <see cref="Kanal.ANZAHL"/>, Index <see cref="Kanal.HEIZUNG"/>,
        /// <see cref="Kanal.BRAUCHWASSER"/>, <see cref="Kanal.PROZESS"/>; nie <c>null</c>.
        ///
        /// <para><b>Dieselben Quellen wie der Lauf</b>
        /// (<c>SimulationWaermebedarf.Waermebedarf_berechnen</c>): Heizung aus den
        /// Projektgebäuden (<c>Z_ProjektGebaeude</c>), Warmwasser aus
        /// <c>Z_Projekt_Brauchwasser</c>, Prozesswärme aus <c>Z_Projekt_Prozesswaerme</c>,
        /// dazu jede externe Wärmeganglinie (<c>Z_ProjektWaermebedarf</c>) in dem Kanal, der
        /// an ihrer Zuordnung steht — leer und unbekannt heißt Heizung
        /// (<see cref="Kanal.AusText"/>).</para>
        ///
        /// <para><b>Zugeordnet, nicht gerechnet</b> — dieselbe Frage, die das Schema ohne
        /// Lauf stellt (<c>SchemaModell.Aufbauen</c>, „Profil zugeordnet ja/nein"). Eine
        /// Jahresmenge gibt es vor dem ersten Lauf nicht. Ist eine Abfrage nicht auswertbar,
        /// zählt der Kanal als OHNE Bedarf: Dann entsteht keine Zeile, und es gilt die
        /// Vorbelegung — nie eine Senke aus Unkenntnis.</para>
        /// </summary>
        public static bool[] KanaeleMitBedarf(int idProjekt)
        {
            bool[] kanaele = new bool[Kanal.ANZAHL];
            if (idProjekt <= 0) return kanaele;

            kanaele[Kanal.HEIZUNG] = Anzahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?",
                                            idProjekt) > 0;
            kanaele[Kanal.BRAUCHWASSER] = Anzahl("SELECT COUNT(*) FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?",
                                                 idProjekt) > 0;
            kanaele[Kanal.PROZESS] = Anzahl("SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?",
                                            idProjekt) > 0;

            // Externe Waermeganglinien: je Zeile in den Kanal ihrer Zuordnung. Ohne die
            // Spalte Kanal (vor Paket K1) laufen alle im Heizkanal - wie im Lauf.
            HashSet<string> spalten = StilleDb.SpaltenNamen("Z_ProjektWaermebedarf");
            if (spalten == null) return kanaele;              // keine Tabelle - kein Beitrag

            if (spalten.Contains("Kanal"))
            {
                DataTable dt = StilleDb.Tabelle(
                    "SELECT Kanal FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?",
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        kanaele[Kanal.AusText(StilleDb.Text(StilleDb.Feld(r, "Kanal")))] = true;
            }
            else if (Anzahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?", idProjekt) > 0)
            {
                kanaele[Kanal.HEIZUNG] = true;
            }

            return kanaele;
        }

        // =====================================================================
        // Die Ableitung
        // =====================================================================

        /// <summary>
        /// Die Senkenzeilen einer NEUEN Anlage aus den Bedarfskanälen; leer, wenn kein Kanal
        /// Bedarf hat. Nie <c>null</c>. Die Ränge sind lückenlos ab 1.
        /// </summary>
        public static List<Z_AnlageSenkeModel> Ableiten(bool[] kanaele)
        {
            List<Z_AnlageSenkeModel> zeilen = new List<Z_AnlageSenkeModel>();
            if (kanaele == null || kanaele.Length < Kanal.ANZAHL) return zeilen;

            if (kanaele[Kanal.HEIZUNG] || kanaele[Kanal.BRAUCHWASSER])
                zeilen.Add(new Z_AnlageSenkeModel
                {
                    Rang = zeilen.Count + 1,
                    Ziel = DbWerte.WS_ZIEL_HEIZKREIS,
                    Bedarfsart = DbWerte.WS_TYP_BEIDES
                });

            if (kanaele[Kanal.PROZESS])
                zeilen.Add(new Z_AnlageSenkeModel
                {
                    Rang = zeilen.Count + 1,
                    Ziel = DbWerte.WS_ZIEL_PROZESS,
                    // Bei jedem anderen Ziel als dem Heizkreis ist die Bedarfsart
                    // bedeutungslos und traegt die neutrale Vorbelegung
                    // (Z_AnlageSenkeModel.Bedarfsart).
                    Bedarfsart = DbWerte.WS_TYP_BEIDES
                });

            return zeilen;
        }

        /// <summary>Die Senkenzeilen einer neuen Anlage dieses Projekts (siehe <see cref="Ableiten(bool[])"/>).</summary>
        public static List<Z_AnlageSenkeModel> Ableiten(int idProjekt)
        {
            return Ableiten(KanaeleMitBedarf(idProjekt));
        }

        // =====================================================================
        // Das Schreiben
        // =====================================================================

        /// <summary>
        /// Schreibt die abgeleiteten Senkenzeilen für die genannten NEUEN Anlagen.
        ///
        /// <para>Berücksichtigt wird nur eine Anlage, die zu <paramref name="idProjekt"/>
        /// gehört und ein Wärmeerzeuger ist. Mit <paramref name="ersetzen"/> = <c>false</c>
        /// wird eine Anlage übergangen, die bereits eine Senkenzeile führt — die Rettung des
        /// Speicherwegs (<c>WizardCtrl.SenkenWiederherstellen</c>) oder der Dialog waren
        /// dann schneller, und deren Liste gilt. Mit <c>true</c> wird die Liste neu
        /// abgeleitet und geschrieben, AUCH leer: Das ist der Nachzug des Assistenten, der
        /// den Bedarf erst NACH den Anlagen schreibt (<c>AssistentCtrl</c>) — die Anlagen
        /// sind in demselben Vorgang entstanden, ihre Zeilen stammen allein von hier.</para>
        ///
        /// <para>Ohne Bedarf und ohne <paramref name="ersetzen"/> entsteht nichts. BEST
        /// EFFORT: Ein gescheitertes Schreiben lässt die Anlage ohne Zeile — dann gilt die
        /// Vorbelegung, wie bisher.</para>
        /// </summary>
        /// <returns>Zahl der Anlagen, deren Senkenliste geschrieben wurde.</returns>
        public static int Anlegen(int idProjekt, IEnumerable<int> idAnlagen, bool ersetzen)
        {
            if (idProjekt <= 0 || idAnlagen == null || !Z_AnlageSenkeCtrl.SpalteVorhanden()) return 0;

            List<Z_AnlageSenkeModel> vorlage = Ableiten(idProjekt);
            if (vorlage.Count == 0 && !ersetzen) return 0;

            Z_AnlageSenkeCtrl ctrl = new Z_AnlageSenkeCtrl();

            HashSet<int> mitSenken = new HashSet<int>();
            if (!ersetzen)
                foreach (Z_AnlageSenkeModel z in ctrl.LesenJeProjekt(idProjekt))
                    mitSenken.Add(z.ID_Anlage);

            int geschrieben = 0;
            HashSet<int> gesehen = new HashSet<int>();
            foreach (int idAnlage in idAnlagen)
            {
                if (idAnlage <= 0 || !gesehen.Add(idAnlage)) continue;
                if (!IstWaermeerzeuger(AnlagenTyp(idProjekt, idAnlage))) continue;
                if (!ersetzen && mitSenken.Contains(idAnlage)) continue;

                if (ctrl.SchreibenJeAnlage(idAnlage, Kopie(vorlage))) geschrieben++;
            }

            return geschrieben;
        }

        // =====================================================================
        // Die Anzeige im Anlagendialog
        // =====================================================================

        /// <summary>
        /// Die Zeile „Senken: …" des Anlagendialogs; <c>""</c>, wenn es für diese Anlage
        /// nichts zu zeigen gibt (kein Wärmeerzeuger, kein Projekt).
        ///
        /// <para>Drei Fälle:
        /// <list type="number">
        /// <item>GESPEICHERTE Anlage mit Senkenzeilen → die Zeilen in Rangfolge, beschriftet
        ///   wie an der Erzeugerkarte (<see cref="WaermesenkeClass.SenkeAnzeige"/>);</item>
        /// <item>gespeicherte Anlage OHNE Zeile (Altbestand) → die Vorbelegung, als solche
        ///   gekennzeichnet — genau das rechnet der Lauf;</item>
        /// <item>NOCH NICHT gespeicherte Anlage → was beim Speichern entsteht
        ///   (<see cref="Ableiten(int)"/>); ohne Bedarf die Vorbelegung.</item>
        /// </list>
        /// Gespeichert heißt: Unter <paramref name="idAnlage"/> steht in DIESEM Projekt eine
        /// Anlage DIESES Typs. Die Dialoge vergeben neuen Zeilen vorläufige Schlüssel, die
        /// mit einer fremden Anlagen-Id zusammenfallen können — die Prüfung auf Projekt und
        /// Typ hält deren Senken aus der Anzeige.</para>
        /// </summary>
        public static string Anzeigezeile(int idProjekt, int idAnlage, int idType)
        {
            if (idProjekt <= 0 || !IstWaermeerzeuger(idType)) return "";

            string vorbelegung = string.Format(MyResource.Resource.ANL_SENKEN_VORBELEGUNG,
                                               MyResource.Resource.SIM_HEIZKREIS_BEIDES);
            string text;

            bool gespeichert = idAnlage > 0 && AnlagenTyp(idProjekt, idAnlage) == idType;
            if (gespeichert)
            {
                List<Z_AnlageSenkeModel> zeilen = Z_AnlageSenkeCtrl.SpalteVorhanden()
                    ? new Z_AnlageSenkeCtrl().LesenJeAnlage(idAnlage)
                    : new List<Z_AnlageSenkeModel>();
                text = zeilen.Count > 0 ? Liste(zeilen) : vorbelegung;
            }
            else
            {
                List<Z_AnlageSenkeModel> abgeleitet = Ableiten(idProjekt);
                text = abgeleitet.Count > 0
                    ? string.Format(MyResource.Resource.ANL_SENKEN_BEIM_SPEICHERN, Liste(abgeleitet))
                    : vorbelegung;
            }

            return string.Format(MyResource.Resource.ANL_SENKEN_ZEILE, text);
        }

        // =====================================================================
        // Innenleben
        // =====================================================================

        /// <summary>Die Senkenzeilen in Rangfolge, beschriftet wie an der Erzeugerkarte.</summary>
        private static string Liste(List<Z_AnlageSenkeModel> zeilen)
        {
            List<Z_AnlageSenkeModel> sortiert = new List<Z_AnlageSenkeModel>(zeilen);
            sortiert.Sort((a, b) => a.Rang.CompareTo(b.Rang));

            StringBuilder sb = new StringBuilder();
            foreach (Z_AnlageSenkeModel z in sortiert)
            {
                if (z == null) continue;
                if (sb.Length > 0) sb.Append(TRENNER);
                sb.Append(WaermesenkeClass.SenkeAnzeige(z));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Der Typ der Anlage, wenn sie zu diesem Projekt gehört; sonst 0. Über
        /// <c>StilleDb</c> — unter einer Vorgangsklammer sieht die Abfrage auch eine Anlage,
        /// die der laufende Vorgang eben angelegt hat.
        /// </summary>
        private static int AnlagenTyp(int idProjekt, int idAnlage)
        {
            if (idProjekt <= 0 || idAnlage <= 0) return 0;
            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT ID_Type FROM Tab_Energieanlagen WHERE ID = ? AND ID_Projekt = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idAnlage),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt)));
        }

        /// <summary>Eine Zählabfrage mit dem Projekt als einzigem Parameter; 0, wenn sie nicht läuft.</summary>
        private static int Anzahl(string sql, int idProjekt)
        {
            return StilleDb.Zahl(StilleDb.Scalar(sql, StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt)));
        }

        /// <summary>Frische Modelle je Anlage — <c>SchreibenJeAnlage</c> nimmt die Liste, wie sie kommt.</summary>
        private static List<Z_AnlageSenkeModel> Kopie(List<Z_AnlageSenkeModel> vorlage)
        {
            List<Z_AnlageSenkeModel> kopie = new List<Z_AnlageSenkeModel>();
            foreach (Z_AnlageSenkeModel z in vorlage)
                kopie.Add(new Z_AnlageSenkeModel { Rang = z.Rang, Ziel = z.Ziel, Bedarfsart = z.Bedarfsart });
            return kopie;
        }
    }
}
