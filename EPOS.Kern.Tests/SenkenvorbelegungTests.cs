using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// SENKEN BEIM ANLEGEN (Anwenderentscheid 23.09.2026, Punkt 2): Eine NEU angelegte
    /// Wärmeerzeugeranlage bekommt ihre Senkenzeilen im selben Schritt, abgeleitet aus den
    /// Bedarfskanälen des Projekts (<see cref="Senkenvorbelegung"/>) — Heizung/Warmwasser →
    /// Heizkreis (Heizung + Warmwasser), Prozesswärme → Direktsenke Prozesswärme, kein
    /// Bedarf → keine Zeile. Eine BESTEHENDE Anlage behält, was sie hat.
    ///
    /// <para><b>Das Projekt der Testdatenbank.</b> <c>1041</c> „Prozesswärme mit eigenem
    /// Puffer": ein Gebäude, Brauchwasser, ein Prozesswärmeprofil und eine Wärmeganglinie
    /// im Kanal Heizung; Wärmepumpe 14751 (Heizkreis/Heizung, Prozesswärme, Puffer Kombi),
    /// Heizkessel 14759 (zwei Puffer Kombi), vier Pufferspeicher.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SenkenvorbelegungTests : IDisposable
    {
        private const int PROJEKT = 1041;
        private const int WP_1041 = 14751;
        private const int KESSEL_1041 = 14759;

        /// <summary>Ein Heizkessel des Katalogs, den 1041 noch nicht führt.</summary>
        private const string NEUER_KESSEL = "ecoVIT VKK 186/5";

        private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

        public SenkenvorbelegungTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _kulturVorher;
            CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        // =================================================================================
        // 1 — Die Regel
        // =================================================================================

        /// <summary>
        /// Nur die vier Wärmeerzeuger tragen eine Senkenliste — dieselben Typen, für die der
        /// Lauf sie führt. Pufferspeicher (Ziel einer Senke), Referenzanlagen, PV und
        /// Stromspeicher nicht.
        /// </summary>
        [Fact]
        public void Nur_die_vier_Waermeerzeuger_bekommen_Senken()
        {
            Assert.True(Senkenvorbelegung.IstWaermeerzeuger(WizardItemClass.WP_TYP));
            Assert.True(Senkenvorbelegung.IstWaermeerzeuger(WizardItemClass.SOLAR_TYP));
            Assert.True(Senkenvorbelegung.IstWaermeerzeuger(WizardItemClass.KESSEL_TYP));
            Assert.True(Senkenvorbelegung.IstWaermeerzeuger(WizardItemClass.BHKW_TYP));

            foreach (int typ in new[] { WizardItemClass.PV_TYP, WizardItemClass.SP_TYP,
                                        WizardItemClass.REF_KESSEL_TYP, WizardItemClass.REF_SP_TYP,
                                        WizardItemClass.REF_WP_TYP, WizardItemClass.REF_SOLAR_TYP,
                                        WizardItemClass.REF_PV_TYP, WizardItemClass.PUFFER_TYP, 0 })
                Assert.False(Senkenvorbelegung.IstWaermeerzeuger(typ), "Typ " + typ);
        }

        /// <summary>
        /// Die Ableitung aus den Kanälen: Heizung ODER Warmwasser → Heizkreis
        /// (Heizung + Warmwasser) auf Rang 1; Prozesswärme → zusätzlich bzw. allein die
        /// Direktsenke Prozesswärme; kein Kanal → keine Zeile.
        /// </summary>
        [Fact]
        public void Die_Ableitung_folgt_den_Bedarfskanaelen()
        {
            Assert.Equal(new[] { "Heizkreis/Beides" }, Kurz(Senkenvorbelegung.Ableiten(Kanaele(true, false, false))));
            Assert.Equal(new[] { "Heizkreis/Beides" }, Kurz(Senkenvorbelegung.Ableiten(Kanaele(false, true, false))));
            Assert.Equal(new[] { "Heizkreis/Beides" }, Kurz(Senkenvorbelegung.Ableiten(Kanaele(true, true, false))));
            Assert.Equal(new[] { "Heizkreis/Beides", "Prozesswaerme" },
                         Kurz(Senkenvorbelegung.Ableiten(Kanaele(true, true, true))));
            Assert.Equal(new[] { "Prozesswaerme" }, Kurz(Senkenvorbelegung.Ableiten(Kanaele(false, false, true))));
            Assert.Empty(Senkenvorbelegung.Ableiten(Kanaele(false, false, false)));
            Assert.Empty(Senkenvorbelegung.Ableiten((bool[])null));

            // Raenge lueckenlos ab 1, keine Puffer.
            List<Z_AnlageSenkeModel> beide = Senkenvorbelegung.Ableiten(Kanaele(true, false, true));
            Assert.Equal(new[] { 1, 2 }, beide.Select(z => z.Rang).ToArray());
            Assert.All(beide, z => Assert.Equal(0, z.ID_Puffer));
        }

        /// <summary>
        /// Die Bedarfskanäle der Testprojekte — dieselben Quellen wie der Lauf: Gebäude,
        /// Brauchwasser, Prozesswärme und die Wärmeganglinie im Kanal ihrer Zuordnung.
        /// </summary>
        [Fact]
        public void Die_Bedarfskanaele_kommen_aus_den_Zuordnungen_des_Projekts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // 1041: Gebaeude, Brauchwasser, Prozessprofil, Ganglinie Heizung.
            Assert.Equal(new[] { true, true, true }, Senkenvorbelegung.KanaeleMitBedarf(PROJEKT));
            // 1030: nur eine Waermeganglinie im Kanal Heizung.
            Assert.Equal(new[] { true, false, false }, Senkenvorbelegung.KanaeleMitBedarf(1030));
            // 1026: Gebaeude und Brauchwasser.
            Assert.Equal(new[] { true, true, false }, Senkenvorbelegung.KanaeleMitBedarf(1026));
            // Projekt 19: kein Bedarf.
            Assert.Equal(new[] { false, false, false }, Senkenvorbelegung.KanaeleMitBedarf(19));

            // Eine Ganglinie im Kanal Prozesswaerme zaehlt dort - nicht im Heizkanal.
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_ProjektWaermebedarf SET Kanal = ? WHERE ID_Projekt = ?",
                new DbParam("@k", DbWerte.KANAL_PROZESS), new DbParam("@p", 1030)));
            Assert.Equal(new[] { false, false, true }, Senkenvorbelegung.KanaeleMitBedarf(1030));
        }

        // =================================================================================
        // 2 — Der Speicherweg (WizardCtrl, Loeschen + Neuanlegen)
        // =================================================================================

        /// <summary>
        /// DER KERN DES ENTSCHEIDS: Ein im Projekt NEU angelegter Heizkessel trägt nach dem
        /// Speichern Heizkreis (Heizung + Warmwasser) und Prozesswärme — 1041 hat Bedarf in
        /// allen drei Kanälen. Der bestehende Kessel behält seine zwei Puffersenken, die
        /// Wärmepumpe (anderer Typ, nicht gelöscht) bleibt, wie sie ist.
        /// </summary>
        [Fact]
        public void Ein_neuer_Kessel_bekommt_Heizkreis_und_Prozesswaerme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string[] wpVorher = Kurz(Senken(WP_1041));
            string[] kesselVorher = Kurz(Senken(KESSEL_1041));
            Assert.Equal(2, kesselVorher.Length);

            WizardCtrl wiz = KesselSpeichern(mitNeuem: true);

            int neu = Assert.Single(wiz.NeuAngelegteAnlagen);
            Assert.Equal(new[] { "Heizkreis/Beides", "Prozesswaerme" }, Kurz(Senken(neu)));
            Assert.Equal(new[] { 1, 2 }, Senken(neu).Select(z => z.Rang).ToArray());

            // Der bestehende Kessel ist neu GESCHRIEBEN (neue Id), aber nicht neu ANGELEGT:
            // Er traegt seine geretteten Senken.
            int kesselNachher = AnlageId(PROJEKT, WizardItemClass.KESSEL_TYP, neu);
            Assert.NotEqual(0, kesselNachher);
            Assert.Equal(kesselVorher, Kurz(Senken(kesselNachher)));

            // Die Waermepumpe (anderer Typ) ist unberuehrt.
            Assert.Equal(wpVorher, Kurz(Senken(WP_1041)));
        }

        /// <summary>Ein Projekt NUR mit Prozesswärme: Der neue Kessel bekommt nur die Prozesssenke.</summary>
        [Fact]
        public void Nur_Prozesswaerme_ergibt_nur_die_Prozesssenke()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BedarfEntfernen(heizungUndWarmwasser: true, prozess: false);
            Assert.Equal(new[] { false, false, true }, Senkenvorbelegung.KanaeleMitBedarf(PROJEKT));

            WizardCtrl wiz = KesselSpeichern(mitNeuem: true);

            int neu = Assert.Single(wiz.NeuAngelegteAnlagen);
            Assert.Equal(new[] { "Prozesswaerme" }, Kurz(Senken(neu)));
            Assert.Equal(1, Senken(neu)[0].Rang);
        }

        /// <summary>
        /// Ohne jeden Bedarf entsteht KEINE Zeile — im Lauf gilt dann die Vorbelegung, der
        /// Rückfall für Altbestand und später hinzukommenden Bedarf.
        /// </summary>
        [Fact]
        public void Ohne_Bedarf_entsteht_keine_Senkenzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BedarfEntfernen(heizungUndWarmwasser: true, prozess: true);
            Assert.Equal(new[] { false, false, false }, Senkenvorbelegung.KanaeleMitBedarf(PROJEKT));

            WizardCtrl wiz = KesselSpeichern(mitNeuem: true);

            int neu = Assert.Single(wiz.NeuAngelegteAnlagen);
            Assert.Empty(Senken(neu));
        }

        /// <summary>
        /// NUR BEIM ANLEGEN: Ein bestehender Kessel OHNE Senkenzeile (Altbestand) bleibt beim
        /// Speichern ohne Zeile — er ist neu geschrieben, aber nicht neu angelegt.
        /// </summary>
        [Fact]
        public void Eine_bestehende_Anlage_ohne_Senke_bleibt_ohne_Senke()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Z_AnlageSenke WHERE ID_Anlage = ?",
                                                  new DbParam("@a", KESSEL_1041)));

            WizardCtrl wiz = KesselSpeichern(mitNeuem: false);

            Assert.Empty(wiz.NeuAngelegteAnlagen);
            int kesselNachher = AnlageId(PROJEKT, WizardItemClass.KESSEL_TYP, 0);
            Assert.NotEqual(0, kesselNachher);
            Assert.Empty(Senken(kesselNachher));
        }

        /// <summary>
        /// DER NACHZUG DES ASSISTENTEN: Er schreibt Prozesswärme und Wärmeganglinien erst
        /// NACH den Anlagen. <see cref="WizardCtrl.NeueAnlagenSenkenNachziehen"/> leitet die
        /// Senken der eben neu angelegten Anlagen aus dem dann vollständigen Bedarf neu ab —
        /// und lässt die bestehenden in Ruhe.
        /// </summary>
        [Fact]
        public void Der_Nachzug_des_Assistenten_leitet_aus_dem_vollstaendigen_Bedarf_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Prozesswaerme voruebergehend "fort" (in ein Projekt ohne Prozess geparkt).
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_Projekt_Prozesswaerme SET ID_Projekt = ? WHERE ID_Projekt = ?",
                new DbParam("@z", 1030), new DbParam("@q", PROJEKT)));

            WizardCtrl wiz = KesselSpeichern(mitNeuem: true);
            int neu = Assert.Single(wiz.NeuAngelegteAnlagen);
            Assert.Equal(new[] { "Heizkreis/Beides" }, Kurz(Senken(neu)));

            int kessel = AnlageId(PROJEKT, WizardItemClass.KESSEL_TYP, neu);
            string[] kesselVorher = Kurz(Senken(kessel));

            // Der Assistent schreibt die Prozesswaerme - und zieht nach.
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Z_Projekt_Prozesswaerme SET ID_Projekt = ? WHERE ID_Projekt = ?",
                new DbParam("@z", PROJEKT), new DbParam("@q", 1030)));

            Assert.Equal(1, wiz.NeueAnlagenSenkenNachziehen(PROJEKT));
            Assert.Equal(new[] { "Heizkreis/Beides", "Prozesswaerme" }, Kurz(Senken(neu)));
            Assert.Equal(kesselVorher, Kurz(Senken(kessel)));
        }

        /// <summary>
        /// Der zweite Schreibweg <see cref="WErzeugerCtrl.Insert"/> folgt derselben Regel.
        /// </summary>
        [Fact]
        public void Auch_WErzeugerCtrl_Insert_legt_die_Senken_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // EINE Zeile je Projekt und Geraet (Anlagenzeilen-Eindeutigkeit): eine eigene
            // Geraetekopie fuer die neue Anlage.
            int geraet = new HeizkesselCtrl().CopyFromStamm(NEUER_KESSEL, PROJEKT);
            Assert.True(geraet > 0);
            var ctrl = new WErzeugerCtrl
            {
                ID_Projekt = PROJEKT,
                ID_Type = WizardItemClass.KESSEL_TYP,
                Bezeichner = "Einfuegeprobe Senken",
                ID_Kessel = geraet
            };

            Assert.True(ctrl.Insert());

            int neu = Zahl("SELECT MAX(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                           PROJEKT, WizardItemClass.KESSEL_TYP);
            Assert.NotEqual(KESSEL_1041, neu);
            Assert.Equal(new[] { "Heizkreis/Beides", "Prozesswaerme" }, Kurz(Senken(neu)));
        }

        // =================================================================================
        // 3 — Die Zeile im Anlagendialog
        // =================================================================================

        /// <summary>
        /// „Senken: …" — die Zeilen einer gespeicherten Anlage wie an der Erzeugerkarte;
        /// für eine noch nicht gespeicherte, was beim Speichern entsteht; eine gespeicherte
        /// ohne Zeile zeigt die Vorbelegung als solche. Kein Wärmeerzeuger, kein Projekt:
        /// keine Zeile.
        /// </summary>
        [Fact]
        public void Die_Anzeigezeile_nennt_Senken_Vorschau_und_Vorbelegung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string wp = Senkenvorbelegung.Anzeigezeile(PROJEKT, WP_1041, WizardItemClass.WP_TYP);
            Assert.StartsWith("Senken: Heizkreis (nur Heizwärme); Prozesswärme; Puffer Kombi", wp);

            string vorschau = Senkenvorbelegung.Anzeigezeile(PROJEKT, 0, WizardItemClass.KESSEL_TYP);
            Assert.Equal("Senken: Heizkreis (Heizung + Warmwasser); Prozesswärme " +
                         "(wird beim Speichern aus dem Bedarf des Projekts angelegt)", vorschau);

            // Ein vorlaeufiger Schluessel, der mit der Anlage eines FREMDEN Projekts
            // zusammenfaellt (11274 = Kollektorfeld von 1026), zeigt nicht deren Senken.
            Assert.Equal(vorschau, Senkenvorbelegung.Anzeigezeile(PROJEKT, 11274, WizardItemClass.KESSEL_TYP));

            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Z_AnlageSenke WHERE ID_Anlage = ?",
                                                  new DbParam("@a", KESSEL_1041)));
            Assert.Equal("Senken: Heizkreis (Heizung + Warmwasser) (Vorbelegung – keine Senke zugeordnet)",
                         Senkenvorbelegung.Anzeigezeile(PROJEKT, KESSEL_1041, WizardItemClass.KESSEL_TYP));

            Assert.Equal("", Senkenvorbelegung.Anzeigezeile(PROJEKT, 14770, WizardItemClass.PUFFER_TYP));
            Assert.Equal("", Senkenvorbelegung.Anzeigezeile(PROJEKT, 0, WizardItemClass.PV_TYP));
            Assert.Equal("", Senkenvorbelegung.Anzeigezeile(0, WP_1041, WizardItemClass.WP_TYP));
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Der Speicherweg der Startseitenkachel „Heizkessel": typgefiltert löschen, die
        /// Liste neu schreiben — mit oder ohne einen NEUEN Kessel aus dem Katalog.
        /// </summary>
        private static WizardCtrl KesselSpeichern(bool mitNeuem)
        {
            WErzeugerCtrl lesen = new WErzeugerCtrl();
            lesen.ReadAllFilter("ID_Projekt=" + PROJEKT + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
            var liste = new List<WErzeugerModel>();
            for (int i = 0; i < lesen.rows; i++) liste.Add(lesen.items[i]);
            Assert.Single(liste);

            if (mitNeuem)
                liste.Add(new WErzeugerModel
                {
                    ID_Projekt = PROJEKT,
                    ID_Type = WizardItemClass.KESSEL_TYP,
                    Bezeichner = NEUER_KESSEL,
                    Vorlauf = 70,
                    Ruecklauf = 50
                });

            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.KESSEL_TYP));
            Assert.True(wiz.Add_WP_Waermeerzeuger(PROJEKT, liste));
            return wiz;
        }

        /// <summary>Entfernt Bedarfszuordnungen von 1041 aus der Arbeitskopie.</summary>
        private static void BedarfEntfernen(bool heizungUndWarmwasser, bool prozess)
        {
            if (heizungUndWarmwasser)
            {
                DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
                DataRepository.ExecuteSQL("DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
                DataRepository.ExecuteSQL("DELETE FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            }
            if (prozess)
                DataRepository.ExecuteSQL("DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
        }

        private static bool[] Kanaele(bool heizung, bool warmwasser, bool prozess)
        {
            bool[] k = new bool[Kanal.ANZAHL];
            k[Kanal.HEIZUNG] = heizung;
            k[Kanal.BRAUCHWASSER] = warmwasser;
            k[Kanal.PROZESS] = prozess;
            return k;
        }

        private static List<Z_AnlageSenkeModel> Senken(int idAnlage)
            => new Z_AnlageSenkeCtrl().LesenJeAnlage(idAnlage);

        /// <summary>Kurzform je Zeile: Heizkreis mit Bedarfsart, sonst das Ziel; Puffer mit Id.</summary>
        private static string[] Kurz(List<Z_AnlageSenkeModel> zeilen)
            => zeilen.OrderBy(z => z.Rang).Select(z =>
                   z.Ziel == DbWerte.WS_ZIEL_HEIZKREIS ? z.Ziel + "/" + z.Bedarfsart
                   : z.ID_Puffer > 0 ? z.Ziel + ":" + z.ID_Puffer
                   : z.Ziel).ToArray();

        /// <summary>Die Anlage eines Typs im Projekt außer <paramref name="ausser"/>; 0 = keine.</summary>
        private static int AnlageId(int projekt, int typ, int ausser)
            => Zahl("SELECT MIN(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? AND ID <> ?",
                    projekt, typ, ausser);

        private static int Zahl(string sql, params int[] werte)
        {
            DbParam[] p = werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray();
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }
    }
}
