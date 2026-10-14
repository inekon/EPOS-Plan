using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kältemaschine als Gewerk der Übernahme</b> (Welle KN1, die zwei Nachzüge zu #1035):
    ///
    /// <list type="number">
    /// <item><b>Übernahme als Ganzes</b> — Anlagenzeile samt Stückzahl, Kühlträger und
    /// Abrechnungsart, Projektkopie <c>Tab_Kaeltemaschine</c> samt Kennlinie
    /// (<c>Tab_Kenndaten_Kaeltemaschine</c>) und der Geräteverweis
    /// <c>ID_Kaeltemaschine</c>, der als FACHSPALTE nach dem Einfügen gesetzt wird. Kein
    /// Verweis zeigt nach dem Austausch ins Quellprojekt; was die Übernahme nicht kann
    /// (Kaskade, Kältespeicher, Kältefolge), steht benannt im Dialog und im Hinweiskanal.</item>
    /// <item><b>Unterschiedsliste</b> — eine abweichende <c>Kaeltemaschine_Anzahl</c> bei
    /// gleicher Anlagenzahl ist ein Merkmal mit beiden Werten, über die Merkmalübernahme
    /// übertragbar, und der Komponentenvergleich zieht mit („3 ×" statt „1 ×").</item>
    /// </list>
    ///
    /// <para>DIE REFERENZPROJEKTE BLEIBEN UNBERÜHRT (Einfrierregel zu den
    /// Kältemaschinendaten): Gerechnet wird mit DUPLIKATEN von 1055 (20 kW) und 1059
    /// (10 kW) in der Arbeitskopie der Testdatenbank. Eigene Arbeitskopie je Fall, weil
    /// jeder Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltemaschineUebernahmeTests : IDisposable
    {
        private const string NAME_1055 = "Kältemaschine mit Kältespeicher";
        private const string NAME_1059 = "Referenzprojekt AK3-K";
        private const string NAME_1017 = "WP_PV-Speicher";
        private const string GEWERK = ProjektDetails.GEWERK_KAELTEMASCHINE;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() { _kultur.Dispose(); _db.Dispose(); }

        // ===================================================== 1) Übernahme als Ganzes

        /// <summary>
        /// Duplikat von 1055 (20 kW, Stückzahl auf 2 gestellt) → Duplikat von 1059 (10 kW):
        /// Das Ziel führt danach die 20-kW-Maschine als eigene Projektkopie samt ihren sechs
        /// Kennlinienpunkten, die Anlagenzeile trägt Stückzahl 2, Kühlträger und eigenen
        /// Zähler, der 10-kW-Bestand ist weg, und der Hinweis benennt die Grenzen.
        /// </summary>
        [Fact]
        public void Die_Kaeltemaschine_kommt_als_Gewerk_vollstaendig_ins_Ziel()
        {
            if (!_db.Vorhanden) return;

            int quelle = Duplikat(NAME_1055, "KN1 Quelle");
            int ziel = Duplikat(NAME_1059, "KN1 Ziel");
            int anlageQuelle = Anlagen(quelle).Single();
            Ausfuehren("UPDATE Tab_Energieanlagen SET Kaeltemaschine_Anzahl = 2 WHERE ID = ?", anlageQuelle);
            int geraetQuelle = Ganz("SELECT ID_Kaeltemaschine FROM Tab_Energieanlagen WHERE ID = ?", anlageQuelle);
            int traeger = Ganz("SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID = ?", anlageQuelle);
            Assert.True(traeger > 0);

            // Die Vorschau nennt die Kette UND die Grenzen - vor dem Schreiben.
            KomponentenUebernahmeCtrl.Vorschau v = new KomponentenUebernahmeCtrl().Planen(quelle, ziel, GEWERK);
            Assert.True(v.Moeglich, v.Grund);
            Assert.Contains("Tab_Kenndaten_Kaeltemaschine", v.Klartext);
            Assert.Contains(Resource.BK_KOMP_HINW_KAELTESEITE, v.Klartext);

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(quelle, ziel, GEWERK,
                out string fehler, out string hinweise), fehler);

            // --- Anlagenzeile ----------------------------------------------------
            int anlageZiel = Anlagen(ziel).Single();
            Assert.Equal(2, Ganz("SELECT Kaeltemaschine_Anzahl FROM Tab_Energieanlagen WHERE ID = ?", anlageZiel));
            Assert.Equal(traeger, Ganz("SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID = ?", anlageZiel));
            Assert.Equal(1, Ganz("SELECT Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID = ?", anlageZiel));

            // --- Projektkopie samt Kennlinie und Zuordnung -----------------------
            int geraetZiel = Ganz("SELECT ID_Kaeltemaschine FROM Tab_Energieanlagen WHERE ID = ?", anlageZiel);
            Assert.True(geraetZiel > 0, "Der Geräteverweis ID_Kaeltemaschine ist nicht gesetzt");
            Assert.NotEqual(geraetQuelle, geraetZiel);
            Assert.Equal(ziel, Ganz("SELECT ID_Projekt FROM Tab_Kaeltemaschine WHERE ID = ?", geraetZiel));
            Assert.Equal(20.0, Komma("SELECT Nennkaelteleistung_kW FROM Tab_Kaeltemaschine WHERE ID = ?", geraetZiel), 9);
            Assert.Equal(Text("SELECT Bezeichner FROM Tab_Kaeltemaschine WHERE ID = ?", geraetQuelle),
                         Text("SELECT Bezeichner FROM Tab_Kaeltemaschine WHERE ID = ?", geraetZiel));
            Assert.Equal(Ganz("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ?", geraetQuelle),
                         Ganz("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ? AND ID_Projekt = ?",
                              geraetZiel, ziel));
            // Genau EINE Kopie im Ziel - der Fachspaltenweg legt keine zweite an.
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", ziel));
            // Der 10-kW-Bestand des Ziels ist ersetzt, nicht ergänzt.
            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ? AND Nennkaelteleistung_kW = 10", ziel));
            // Kein Verweis zeigt ins Quellprojekt.
            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM Tab_Energieanlagen a JOIN Tab_Kaeltemaschine k ON k.ID = a.ID_Kaeltemaschine " +
                                 "WHERE a.ID_Projekt = ? AND k.ID_Projekt <> ?", ziel, ziel));

            // --- Kennwert und Stückzahl der Tafel --------------------------------
            KomponentenVergleichEintrag e = Eintrag(ziel);
            Assert.Equal(2, e.Anzahl);
            Assert.Contains("20", e.Kennwerte);

            // --- Die Grenzen sind benannt, nicht still übergangen ----------------
            Assert.Contains(Resource.BK_KOMP_HINW_KAELTESEITE, hinweise);

            // --- Die Quelle bleibt, wie sie war ---------------------------------
            Assert.Equal(2, Ganz("SELECT Kaeltemaschine_Anzahl FROM Tab_Energieanlagen WHERE ID = ?", anlageQuelle));
            Assert.Equal(geraetQuelle, Ganz("SELECT ID_Kaeltemaschine FROM Tab_Energieanlagen WHERE ID = ?", anlageQuelle));
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", quelle));
        }

        /// <summary>
        /// Wiederholbarkeit und Rückweg: Dieselbe Übernahme ein zweites Mal meldet ehrlich
        /// „nichts zu tun" — auch die STÜCKZAHL zählt zum Bestand, eine allein abweichende
        /// Anzahl ist also übernehmbar. Und der Rückweg trägt: Aus einem Stand ohne
        /// Kältemaschine übernommen, führt das Ziel keine mehr.
        /// </summary>
        [Fact]
        public void Zweite_Uebernahme_meldet_nichts_zu_tun_und_der_Rueckweg_raeumt_auf()
        {
            if (!_db.Vorhanden) return;

            int quelle = Duplikat(NAME_1055, "KN1 Quelle");
            int ziel = Duplikat(NAME_1059, "KN1 Ziel");

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(quelle, ziel, GEWERK, out string f1, out _), f1);
            KomponentenUebernahmeCtrl.Vorschau zweite = new KomponentenUebernahmeCtrl().Planen(quelle, ziel, GEWERK);
            Assert.True(zweite.NichtsZuTun);
            Assert.False(zweite.Moeglich);
            Assert.Equal(Resource.BK_MSG_KOMP_NICHTS_ZU_TUN, zweite.Grund);

            // Allein eine andere Stückzahl ist ein Bestandsunterschied (AnlagenKennungen).
            Ausfuehren("UPDATE Tab_Energieanlagen SET Kaeltemaschine_Anzahl = 3 WHERE ID = ?", Anlagen(quelle).Single());
            Assert.True(new KomponentenUebernahmeCtrl().Planen(quelle, ziel, GEWERK).Moeglich);
            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(quelle, ziel, GEWERK, out string f2, out _), f2);
            Assert.Equal(3, Ganz("SELECT Kaeltemaschine_Anzahl FROM Tab_Energieanlagen WHERE ID = ?", Anlagen(ziel).Single()));

            // Rückweg: 1017 rechnet seine Kälte mit der Wärmepumpe und führt keine Maschine.
            int ohne = Duplikat(NAME_1017, "KN1 ohne KM");
            Assert.Empty(Anlagen(ohne));
            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(ohne, ziel, GEWERK, out string f3, out _), f3);
            Assert.Empty(Anlagen(ziel));
            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", ziel));

            // Abbruch: ohne Quelle wird nichts geschrieben.
            Assert.False(new KomponentenUebernahmeCtrl().Uebernehmen(ziel, ziel, GEWERK, out string f4, out _));
            Assert.Equal(Resource.BK_MSG_UEB_KEINE_QUELLE, f4);
        }

        // ================================================= 2) Unterschiedsliste

        /// <summary>
        /// Zwei Duplikate von 1055 mit gleicher Anlagenzahl und verschiedener
        /// <c>Kaeltemaschine_Anzahl</c>: Die Unterschiedsliste meldet das Merkmal mit beiden
        /// Werten (keine Bestands- und keine Anzahlzeile der Stufe 1), die Merkmalübernahme
        /// trägt es, und der Komponentenvergleich zieht nach.
        /// </summary>
        [Fact]
        public void Eine_abweichende_Stueckzahl_ist_ein_Unterschied_und_uebertragbar()
        {
            if (!_db.Vorhanden) return;

            int stamm = Duplikat(NAME_1055, "KN1 Stamm");
            int variante = Duplikat(NAME_1055, "KN1 Variante");
            Ausfuehren("UPDATE Tab_Energieanlagen SET Kaeltemaschine_Anzahl = 3 WHERE ID = ?", Anlagen(variante).Single());

            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(
                ProjektDetails.Lade(stamm), ProjektDetails.Lade(variante));

            Abweichung a = liste.SingleOrDefault(x => x.Schluessel == SCHLUESSEL);
            Assert.NotNull(a);
            Assert.Equal(GEWERK, a.Gewerk);
            Assert.Equal("1", a.WertStamm);
            Assert.Equal("3", a.WertVariante);
            // Die Anlagenzahl ist gleich - Stufe 1 schweigt für dieses Gewerk.
            Assert.DoesNotContain(liste, x => x.Gewerk == GEWERK &&
                                 (x.Schluessel == AbweichungsErmittler.SCHLUESSEL_ANZAHL ||
                                  x.Schluessel == AbweichungsErmittler.SCHLUESSEL_BESTAND));

            // Übertragbar: variante -> stamm, in GENAU die Anlagenzeile der Kältemaschine.
            AbweichungsErmittler.Merkmal f = Merkmal();
            MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(variante, stamm, f);
            Assert.True(b.Moeglich, b.Grund);
            Assert.Equal("3", b.Quelle.Anzeigewert);
            Assert.Equal("1", b.Ziel.Anzeigewert);
            Assert.True(MerkmalUebernahmeCtrl.Schreibe(b, stamm, f, out string fehler), fehler);

            Assert.Equal(3, Ganz("SELECT Kaeltemaschine_Anzahl FROM Tab_Energieanlagen WHERE ID = ?", Anlagen(stamm).Single()));
            // Der Kennwert zieht nach: „3 × 20 kW Kälte".
            KomponentenVergleichEintrag e = Eintrag(stamm);
            Assert.Equal(3, e.Anzahl);
            Assert.Contains("20", e.Kennwerte);
            // Und die Unterschiedsliste ist still.
            Assert.DoesNotContain(AbweichungsErmittler.Vergleiche(ProjektDetails.Lade(stamm), ProjektDetails.Lade(variante)),
                                  x => x.Schluessel == SCHLUESSEL);
        }

        /// <summary>
        /// Gegenprobe zur Merkmalübernahme: Führt ein Stand ZWEI Kältemaschinen, ist „erste
        /// Anlagenzeile" keine belastbare Auswahl — die Übernahme wird benannt abgelehnt und
        /// verweist auf die Komponenten-Übernahme (wie bei jeder Gerätetabelle).
        /// </summary>
        [Fact]
        public void Bei_zwei_Maschinen_wird_die_Merkmaluebernahme_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;

            int stamm = Duplikat(NAME_1055, "KN1 Stamm");
            int variante = Duplikat(NAME_1055, "KN1 Variante");
            ZweiteMaschine(variante);
            Assert.Equal(2, Anlagen(variante).Count);

            MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(variante, stamm, Merkmal());
            Assert.False(b.Moeglich);
            Assert.Equal(string.Format(Resource.BK_MSG_UEB_NICHT_EINDEUTIG, GEWERK, 2, 1), b.Grund);
        }

        // ------------------------------------------------------------------ Bausteine

        private static readonly string SCHLUESSEL =
            "Tab_Energieanlagen." + KaeltemaschineAnlageSchema.SPALTE_ANZAHL;

        /// <summary>Das Merkmal der Stückzahl aus der EINEN Feldliste des Ermittlers.</summary>
        private static AbweichungsErmittler.Merkmal Merkmal() =>
            AbweichungsErmittler.Felder.Single(f => AbweichungsErmittler.Schluessel(f) == SCHLUESSEL);

        /// <summary>Der eine Eintrag des Gewerks in der Komponentenmatrix eines Projekts.</summary>
        private static KomponentenVergleichEintrag Eintrag(int idProjekt)
        {
            List<KomponentenVergleichGewerk> tafel = KomponentenVergleich.JeGewerk(
                new List<ProjektDetails> { ProjektDetails.Lade(idProjekt) });
            return tafel.Single(g => g.Gewerk == GEWERK).Zellen[0].Eintraege.Single();
        }

        /// <summary>
        /// Ein Duplikat eines Referenzprojekts — die Referenzprojekte selbst werden nicht
        /// angefasst (Einfrierregel). Der Name trägt das Kürzel der Welle.
        /// </summary>
        private static int Duplikat(string quellname, string neuerName)
        {
            int id = new ProjektDuplizierenCtrl().Duplizieren(quellname, quellname + " " + neuerName);
            Assert.True(id > 0, "Duplikat von " + quellname + " nicht angelegt");
            return id;
        }

        /// <summary>Eine zweite Kältemaschinen-Anlage auf derselben Projektkopie.</summary>
        private static void ZweiteMaschine(int idProjekt)
        {
            int geraet = Ganz("SELECT ID_Kaeltemaschine FROM Tab_Energieanlagen WHERE ID = ?", Anlagen(idProjekt).Single());
            Ausfuehren("INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, ID_Kaeltemaschine, Kaeltemaschine_Anzahl) " +
                       "VALUES (?, ?, ?, ?, 1)",
                       idProjekt, "Kältemaschine zwei", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE, geraet);
        }

        /// <summary>Die Anlagenzeilen der Kältemaschine eines Projekts, nach Kennung.</summary>
        private static List<int> Anlagen(int idProjekt)
        {
            var liste = new List<int>();
            foreach (DataRow r in Lese("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID",
                                       idProjekt, KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE).Rows)
                liste.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return liste;
        }

        private static DbParam[] Parameter(object[] werte)
        {
            var ps = new DbParam[werte.Length];
            for (int i = 0; i < werte.Length; i++) ps[i] = new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), werte[i]);
            return ps;
        }

        private static DataTable Lese(string sql, params object[] werte) => DataRepository.GetDataTable(sql, Parameter(werte));

        private static void Ausfuehren(string sql, params object[] werte) => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), sql);

        private static int Ganz(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static double Komma(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string sql, params object[] werte) =>
            Convert.ToString(DataRepository.ExecuteScalar(sql, Parameter(werte)), CultureInfo.InvariantCulture);
    }
}
