using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachzug der Komponenten-Übernahme</b>
    /// (<c>KomponentenUebernahmeCtrl.Uebernehmen</c>, Schritte 8 und 9).
    ///
    /// <para><b>Worum es geht.</b> Löschen und Anlegen laufen dort in EINER Transaktion;
    /// die Senkenlisten (Schritt 8) und die Betriebsführung des Stromspeichers
    /// (Schritt 9) entstehen NACH deren Commit. Der Kommentar an der Stelle begründet
    /// das mit dem AutoWert der Anlagen-ID. Diese Klasse misst, was an der Begründung
    /// stimmt — und hält den Nachzug selbst fest.</para>
    ///
    /// <para><b>Was hier geprüft wird.</b> Erstens: Eine AutoWert-ID steht beim
    /// <c>INSERT</c> fest und ist innerhalb des Vorgangs sofort lesbar — der Satz „steht
    /// erst nach dem Commit fest" trifft also nicht auf die ID zu. Zweitens: Sie ist
    /// einer ZWEITEN Verbindung erst nach dem Commit sichtbar — und genau so beschafft
    /// der Nachzug sie (<c>NeueAnlagenIds</c>, <c>AnlageFinden</c> über
    /// <c>DataRepository</c>). Das ist der wahre Kern des Kommentars. Drittens: Der
    /// Nachzug trägt die Senkenkette der Quelle tatsächlich an die neuen Anlagenzeilen.</para>
    ///
    /// <para><b>Was hier NICHT geprüft wird: ein Rechenergebnis.</b> Die Übernahme
    /// verschiebt Bestand, sie rechnet nicht; der Referenzlauf bleibt byte-gleich.</para>
    ///
    /// <para>Jeder Fall bekommt eine EIGENE, unberührte Arbeitskopie; die Klasse trägt
    /// <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class UebernahmeNachzugTests
    {
        /// <summary>Quelle: zwei Wärmepumpen-Anlagenzeilen mit vier Senkenzeilen.</summary>
        private const int QUELLE = 1043;

        /// <summary>Ziel: führt ebenfalls zwei Wärmepumpen, aber eigene Senkenlisten.</summary>
        private const int ZIEL = 1019;

        private const string GEWERK = "Wärmepumpe";

        // =================================================================================
        // 1 — Die AutoWert-ID: wann sie feststeht, und wann sie WER sieht
        // =================================================================================

        /// <summary>
        /// <b>Die ID entsteht beim <c>INSERT</c>, nicht beim <c>COMMIT</c>.</b> Innerhalb
        /// des offenen Vorgangs liefert <c>last_insert_rowid()</c> sie sofort
        /// (<see cref="DbVorgang.EinfuegenUndId"/>), und ein Lesebefehl DESSELBEN
        /// Vorgangs findet die Zeile darunter.
        ///
        /// <para>Der Bestand nutzt genau das bereits an derselben Einfügeanweisung:
        /// <c>SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen</c> legt
        /// Anlagenzeilen über <c>AnlagenSql.SQL_ANLAGE_INSERT</c> an und arbeitet mit der
        /// frischen ID noch in derselben Transaktion weiter.</para>
        /// </summary>
        [Fact]
        public void Eine_AutoWert_Id_steht_beim_Einfuegen_fest_und_nicht_erst_beim_Commit()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                string bezeichner = "NL Autowertprobe";

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    int id = v.EinfuegenUndId(EINFUEGEN, Werte(bezeichner));

                    // Die ID steht — vor jedem Commit.
                    Assert.True(id > 0);

                    // ... und die Zeile ist im Vorgang lesbar.
                    object gelesen = v.Skalar(
                        "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                        new DbParam("@id", id));
                    Assert.Equal(bezeichner, Convert.ToString(gelesen));

                    v.Rollback();
                }
            }
        }

        /// <summary>
        /// <b>Der wahre Kern der Begründung.</b> Dieselbe, noch nicht festgeschriebene
        /// Zeile ist einer ZWEITEN Verbindung unsichtbar — und über eine zweite
        /// Verbindung beschafft sich der Nachzug seine IDs
        /// (<c>NeueAnlagenIds</c> und <c>AnlageFinden</c> lesen über
        /// <c>DataRepository</c>, nicht über den Vorgang).
        ///
        /// <para><b>Die Gegenprobe steht im selben Fall:</b> Unter einer angemeldeten
        /// <c>Vorgangsklammer</c> läuft derselbe Lesebefehl auf DERSELBEN Verbindung und
        /// findet die Zeile. Wer die beiden Schritte unter die Klammer zöge, nähme dem
        /// Satz „steht erst nach dem Commit fest" damit die Grundlage.</para>
        /// </summary>
        [Fact]
        public void Ohne_Klammer_sieht_eine_zweite_Verbindung_die_neue_Zeile_nicht()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                string bezeichner = "NL Sichtbarkeitsprobe";

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    int id = v.EinfuegenUndId(EINFUEGEN, Werte(bezeichner));
                    Assert.True(id > 0);

                    // OHNE Klammer: eigene Verbindung, eigener Blick — die Zeile fehlt.
                    Assert.Equal(0, Zaehlen(id));

                    // MIT Klammer: dieselbe Verbindung, dieselbe Transaktion — die Zeile steht.
                    using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                    {
                        Assert.Equal(1, Zaehlen(id));
                    }

                    v.Rollback();
                }
            }
        }

        // =================================================================================
        // 2 — Der Nachzug selbst
        // =================================================================================

        /// <summary>
        /// <b>Der Nachzug trägt die Senkenkette der Quelle an die NEUEN Anlagenzeilen.</b>
        /// Ohne Schritt 8 startete jede übernommene Komponente mit der
        /// Rang-1-Vorbelegung Heizkreis/Beides statt mit der Kette, die sie in der Quelle
        /// hatte.
        ///
        /// <para>Verglichen wird, was am Rechenweg hängt: Ziel und Bedarfsart je Rang, in
        /// der Reihenfolge der Quelle. Die Puffer-Verweise bleiben absichtlich
        /// aussen vor — sie werden über den Bezeichner abgebildet, und was sich nicht
        /// abbilden lässt, wird GELEERT und gemeldet.</para>
        /// </summary>
        [Fact]
        public void Die_Uebernahme_traegt_die_Senkenkette_der_Quelle_nach()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                List<string> quelle = Senkenketten(QUELLE);
                Assert.NotEmpty(quelle);

                // Der Stand des Ziels ist ein ANDERER - sonst bewiese der Vergleich nichts.
                Assert.NotEqual(quelle, Senkenketten(ZIEL));

                string fehler, hinweise;
                bool ok = new KomponentenUebernahmeCtrl()
                    .Uebernehmen(QUELLE, ZIEL, GEWERK, out fehler, out hinweise);

                Assert.True(ok, fehler);
                Assert.Equal(quelle, Senkenketten(ZIEL));
            }
        }

        // =================================================================================
        // Helfer
        // =================================================================================

        /// <summary>
        /// Eine Wegwerf-Anlagenzeile. Die Geräte-Fremdschlüssel gehen ausdrücklich als
        /// NULL heraus: Ihre Vorgabe 0 wäre eine Phantom-Referenz, und alle acht
        /// Beziehungen sind ERZWUNGEN.
        /// </summary>
        private const string EINFUEGEN =
            "INSERT INTO Tab_Energieanlagen " +
            "(ID_Projekt, Bezeichner, ID_Type, ID_WP, ID_SP, ID_Solar, ID_PV, " +
            " ID_Kessel, ID_BHKW, ID_PUFFER) " +
            "VALUES (?,?,?,NULL,NULL,NULL,NULL,NULL,NULL,NULL)";

        private static DbParam[] Werte(string bezeichner)
        {
            return new DbParam[]
            {
                new DbParam("@p", ZIEL),
                new DbParam("@b", bezeichner),
                new DbParam("@t", WizardItemClass.WP_TYP)
            };
        }

        /// <summary>Wie viele Anlagenzeilen dieser ID sieht eine EIGENE Verbindung?</summary>
        private static int Zaehlen(int id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@id", id));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        /// <summary>
        /// Die Senkenketten der Wärmepumpen-Anlagenzeilen eines Projekts: je Anlage eine
        /// Zeile „Rang:Ziel:Bedarfsart|…", in Anlagenreihenfolge. OHNE IDs — die vergibt
        /// die Übernahme neu.
        /// </summary>
        private static List<string> Senkenketten(int idProjekt)
        {
            var ketten = new List<string>();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT a.ID, s.Rang, s.Ziel, s.Bedarfsart " +
                "FROM Z_AnlageSenke s JOIN Tab_Energieanlagen a ON a.ID = s.ID_Anlage " +
                "WHERE a.ID_Projekt = ? AND a.ID_Type = " +
                WizardItemClass.WP_TYP.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " ORDER BY a.ID, s.Rang",
                new DbParam("@p", idProjekt));

            if (dt == null) return ketten;

            int letzte = 0;
            string kette = "";
            foreach (DataRow r in dt.Rows)
            {
                int anlage = Convert.ToInt32(r["ID"]);
                if (anlage != letzte)
                {
                    if (letzte != 0) ketten.Add(kette);
                    letzte = anlage;
                    kette = "";
                }
                if (kette.Length > 0) kette += "|";
                kette += Convert.ToString(r["Rang"]) + ":" +
                         Convert.ToString(r["Ziel"]) + ":" +
                         Convert.ToString(r["Bedarfsart"]);
            }
            if (letzte != 0) ketten.Add(kette);

            return ketten;
        }
    }
}
