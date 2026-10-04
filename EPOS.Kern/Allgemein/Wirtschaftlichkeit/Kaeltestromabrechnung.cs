using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Abrechnung des Kältestroms</b> (Stufe KU2 Welle 3; Kühlkonzept 6.1–6.3; Entscheide
    /// E33 und E34, Konzept Gebäudesimulation N1.38/N1.39) — EINE Stelle für die Fragen, die Lauf,
    /// Kostenrechnung und Wirtschaftlichkeit gleich beantworten müssen.
    ///
    /// <list type="number">
    /// <item><b>Welcher Stromträger bepreist den Netzbezug des Projekts?</b>
    /// <see cref="Projekttraeger"/> — derselbe Träger, mit dem der <c>KostenEmissionRechner</c> den
    /// Netzbezug bepreist: der zugeordnete bzw. an einer Anlage gewählte, sonst der
    /// Auslieferungsträger des Katalogs.</item>
    /// <item><b>Weicht der Kühlträger einer Anlage davon ab?</b> <see cref="Abweichend"/> — nur dann
    /// wirkt die Abrechnungsart (E34); ohne Kühlträger oder mit dem Träger des Projekts läuft der
    /// Kältestrom wie der Wärmepumpenstrom.</item>
    /// <item><b>Welche Mengen trägt welcher Kühlträger?</b> <see cref="Anteile"/> — aus dem
    /// GESPEICHERTEN Ergebnis (Modulzeilen der Wärmepumpe, Schemaschritt 119), damit Kosten und
    /// Emissionen ohne frischen Lauf entstehen: anteilig am Netzbezug eine Teilmenge von
    /// <c>Stromrestbedarf</c>, mit eigenem Zähler eine Menge daneben.</item>
    /// </list>
    ///
    /// <para><b>Bepreist wird genau einmal</b>, im <c>KostenEmissionRechner</c>; die Wege, die den
    /// Netzbezug ein zweites Mal aus anderen Größen bewerten (Rollentarif, „% der Stromkosten",
    /// Autarkie, die Bemessungsmenge der Entlastung nach § 9b StromStG, der Preis des vermiedenen
    /// Bezugs), nehmen die Mengen und den Projektträger von hier, statt sie neu zu bilden.</para>
    ///
    /// <para><b>Ein eigener Zähler trägt auch Grund- und Leistungspreis</b> seines Kühlträgers
    /// (Entscheid E35, Konzept Gebäudesimulation N1.40): je Zähler — je Anlage — einen Grundpreis und
    /// den Leistungspreis auf die eigene Spitze des Kältestroms der Anlage
    /// (<see cref="EigeneZaehler"/>, <c>ZeitreihenSatz.Kaeltestromspitzen</c>).</para>
    ///
    /// <para><b>Die Kältemaschine rechnet wie die Wärmepumpe</b> (KU3-4d, Schemaschritt 184): Ihre Ergebniszeile
    /// (<c>Tab_ErgebnisKaeltemaschine</c>) trägt Netzbezug, abweichenden Kühlträger, Abrechnungsart und die Spitze
    /// ihres Kältestroms; jede Frage dieser Klasse nimmt sie NACH den Modulen der Wärmepumpe mit. Ihr Schlüssel in
    /// <c>Kaeltestromspitzen</c> ist <see cref="SchluesselKaeltemaschine"/> (negativ — die Modulplätze der Wärmepumpe
    /// sind es nie).</para>
    /// </summary>
    public static class Kaeltestromabrechnung
    {
        /// <summary>
        /// Der Stromträger, der den Netzbezug des Projekts bepreist (<c>energy_carrier.id</c>):
        /// <see cref="Emissionsquelle.StromTraeger(int)"/>, sonst der Auslieferungsträger des
        /// Katalogs — dieselbe Wahl wie die Kostenseite des <c>KostenEmissionRechner</c>. 0 = keiner.
        /// </summary>
        public static int Projekttraeger(int idProjekt)
        {
            int t = Emissionsquelle.StromTraeger(idProjekt);
            if (t <= 0) t = Emissionsquelle.KatalogStromTraeger(idProjekt);
            return t;
        }

        /// <summary>
        /// Die Stromträger des Projekts (<c>energy_project_settings</c>, <c>pricing_model =
        /// 'ELECTRICITY'</c>) — die Auswahl des Kühlträgers (K9, E33: „ein anderer Stromträger des
        /// Projekts"), nach Namen geordnet. Ein Träger, der dem Projekt nicht zugeordnet ist, steht
        /// nicht darin: Seine Preise pflegt die Kostenseite des Projekts, und erst dort wird er
        /// zugeordnet.
        /// </summary>
        public static List<KeyValuePair<int, string>> StromtraegerDesProjekts(int idProjekt)
        {
            var liste = new List<KeyValuePair<int, string>>();
            if (idProjekt <= 0) return liste;
            try
            {
                System.Data.DataTable dt = DataRepository.GetDataTable(
                    "SELECT DISTINCT c.id AS id, c.name AS name FROM energy_project_settings s " +
                    "INNER JOIN energy_carrier c ON c.id = s.[ID_Energieträger] " +
                    "WHERE s.ID_Projekt = ? AND c.pricing_model = 'ELECTRICITY' ORDER BY c.name, c.id",
                    new DbParam("@p", idProjekt));
                if (dt == null) return liste;
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    if (r["id"] == DBNull.Value) continue;
                    liste.Add(new KeyValuePair<int, string>(
                        Convert.ToInt32(r["id"], System.Globalization.CultureInfo.InvariantCulture),
                        r["name"] == DBNull.Value ? "" : r["name"].ToString()));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Stromträger des Projekts ließen sich nicht lesen: " + ex.Message);
            }
            return liste;
        }

        /// <summary>
        /// Weicht der Kühlträger vom Stromträger des Projekts ab? Nur dann wirkt die Abrechnungsart
        /// (E34): NULL, 0 oder derselbe Träger heißt „wie Heizbetrieb" — Tarif und Faktor des Projekts.
        /// </summary>
        public static bool Abweichend(int? kuehltraeger, int projekttraeger)
        {
            return kuehltraeger.HasValue && kuehltraeger.Value > 0 && kuehltraeger.Value != projekttraeger;
        }

        /// <summary>Eine Menge Kältestrom, die ein abweichender Kühlträger trägt — je Träger und Abrechnungsart zusammengefasst.</summary>
        public sealed class Anteil
        {
            /// <summary>Der Kühlträger (<c>energy_carrier.id</c>).</summary>
            public int Traeger;

            /// <summary><c>true</c> = eigener Zähler: die Menge liegt NEBEN dem Netzbezug <c>Stromrestbedarf</c>; <c>false</c> = anteilig, die Menge ist ein Teil davon.</summary>
            public bool EigenerZaehler;

            /// <summary>Die Menge [MWh/a] — der Netzbezug des Kältestroms der Anlagen dieses Trägers.</summary>
            public double MengeMwh;

            /// <summary>Die Anlagen (Bezeichner der Modulzeilen), deren Kältestrom darin steckt.</summary>
            public List<string> Anlagen = new List<string>();
        }

        /// <summary>
        /// Die Mengen der abweichenden Kühlträger im gespeicherten Ergebnis, je Träger und
        /// Abrechnungsart zusammengefasst, in der Reihenfolge ihres ersten Auftretens. Leer ohne
        /// Wärmepumpenergebnis oder ohne abweichenden Kühlträger — dann rechnet jede Stelle wie ohne
        /// diese Klasse.
        /// </summary>
        public static List<Anteil> Anteile(ErgebnisModel m)
        {
            var liste = new List<Anteil>();
            foreach (Quelle q in Quellen(m))
            {
                if (!q.Traeger.HasValue || q.Traeger.Value <= 0) continue;
                double menge = q.NetzbezugMwh ?? 0.0;
                if (!(menge > 0)) continue;

                bool zaehler = q.EigenerZaehler == true;
                Anteil a = liste.Find(x => x.Traeger == q.Traeger.Value && x.EigenerZaehler == zaehler);
                if (a == null)
                {
                    a = new Anteil { Traeger = q.Traeger.Value, EigenerZaehler = zaehler };
                    liste.Add(a);
                }
                a.MengeMwh += menge;
                a.Anlagen.Add(q.Anlage);
            }
            return liste;
        }

        /// <summary>
        /// Der Schlüssel einer Kältemaschine in <c>ZeitreihenSatz.Kaeltestromspitzen</c> und
        /// <see cref="Zaehler.Modulindex"/>: <c>-1 - i</c> für die <paramref name="i"/>-te Zeile von
        /// <see cref="ErgebnisModel.Kaeltemaschinen"/> — negativ, damit er nie auf einen Modulplatz der Wärmepumpe fällt.
        /// </summary>
        public static int SchluesselKaeltemaschine(int i) => -1 - i;

        /// <summary>Die Spitze einer Stundenreihe des Kältestroms [kW] (kWh je Stunde = kW); <c>null</c> ohne Reihe.</summary>
        public static double? Stundenspitze(double[] stunden)
        {
            if (stunden == null || stunden.Length == 0) return null;
            double max = 0.0;
            foreach (double w in stunden) if (w > max) max = w;
            return max;
        }

        /// <summary>Eine Anlage mit Kältestrom im gespeicherten Ergebnis — Modulzeile der Wärmepumpe oder Kältemaschine.</summary>
        private sealed class Quelle
        {
            public int Schluessel;
            public string Anlage = "?";
            public int? Traeger;
            public bool? EigenerZaehler;
            public double? NetzbezugMwh;
            public double? StromspitzeKw;
        }

        /// <summary>Die Anlagen mit Kältestrom: erst die Module der Wärmepumpe, dann die Kältemaschinen (KU3-4d).</summary>
        private static IEnumerable<Quelle> Quellen(ErgebnisModel m)
        {
            if (m == null) yield break;
            if (m.Waermepumpe != null && m.Waermepumpe.Module != null)
                for (int i = 0; i < m.Waermepumpe.Module.Count; i++)
                {
                    ErgebnisWaermepumpeModulModel mo = m.Waermepumpe.Module[i];
                    if (mo == null) continue;
                    yield return new Quelle
                    {
                        Schluessel = i,
                        Anlage = string.IsNullOrEmpty(mo.Modul) ? "?" : mo.Modul,
                        Traeger = mo.Kuehl_CarrierId,
                        EigenerZaehler = mo.Kuehl_EigenerZaehler,
                        NetzbezugMwh = mo.Kaeltestrom_Netzbezug,
                    };
                }
            if (m.Kaeltemaschinen != null)
                for (int i = 0; i < m.Kaeltemaschinen.Count; i++)
                {
                    ErgebnisKaeltemaschineModel k = m.Kaeltemaschinen[i];
                    if (k == null) continue;
                    yield return new Quelle
                    {
                        Schluessel = SchluesselKaeltemaschine(i),
                        Anlage = string.IsNullOrEmpty(k.Bezeichner) ? "?" : k.Bezeichner,
                        Traeger = k.Kuehl_CarrierId,
                        EigenerZaehler = k.Kuehl_EigenerZaehler,
                        NetzbezugMwh = k.Kaeltestrom_Netzbezug_MWh,
                        StromspitzeKw = k.Stromspitze_kW,
                    };
                }
        }

        /// <summary>
        /// <b>Ein eigener Zähler</b> (E34 Wahl 2; E35, Konzept Gebäudesimulation N1.40): die Anlage
        /// mit abweichendem Kühlträger und eigenem Zähler. <b>Je Anlage ein Zähler</b> — die Wahl steht
        /// je Anlage (E34), und zwei Anlagen mit demselben Kühlträger und eigenem Zähler sind zwei
        /// Zähler: zwei Grundpreise, zwei eigene Spitzen. Einen gemeinsamen Zähler mehrerer Anlagen
        /// bildet das Datenmodell nicht ab.
        /// </summary>
        public sealed class Zaehler
        {
            /// <summary>Platz der Anlage in der Modulliste der Wärmepumpe (<c>Module[i]</c>) — derselbe
            /// Index wie der Modulplatz des Laufs (<c>Kaelteerzeuger.Modulindex</c>); bei einer Kältemaschine
            /// <see cref="SchluesselKaeltemaschine"/>. Schlüssel in <c>ZeitreihenSatz.Kaeltestromspitzen</c>.</summary>
            public int Modulindex;

            /// <summary>Die gespeicherte Spitze des Kältestroms [kW] (Kältemaschine, Schritt 184) — die Jahresspitze,
            /// wenn der Lauf keine Zeitreihen geführt hat; <c>null</c> bei der Wärmepumpe.</summary>
            public double? StromspitzeKw;

            /// <summary>Bezeichner der Anlage (Modulzeile).</summary>
            public string Anlage = "";

            /// <summary>Der Kühlträger (<c>energy_carrier.id</c>).</summary>
            public int Traeger;

            /// <summary>Der Kältestrom über diesen Zähler [MWh/a] — ganz aus dem Netz; auch 0.</summary>
            public double MengeMwh;
        }

        /// <summary>
        /// Die eigenen Zähler im gespeicherten Ergebnis, in Modulreihenfolge (E35). Anders als
        /// <see cref="Anteile"/> steht hier auch ein Zähler ohne Kältestrom im Jahr: Der Grundpreis
        /// gehört zum Zähler, nicht zur Menge. Leer ohne eigenen Zähler.
        /// </summary>
        public static List<Zaehler> EigeneZaehler(ErgebnisModel m)
        {
            var liste = new List<Zaehler>();
            foreach (Quelle q in Quellen(m))
            {
                if (!q.Traeger.HasValue || q.Traeger.Value <= 0 || q.EigenerZaehler != true) continue;
                double menge = q.NetzbezugMwh ?? 0.0;
                liste.Add(new Zaehler
                {
                    Modulindex = q.Schluessel,
                    Anlage = q.Anlage,
                    Traeger = q.Traeger.Value,
                    MengeMwh = menge > 0 ? menge : 0.0,
                    StromspitzeKw = q.StromspitzeKw
                });
            }
            return liste;
        }

        /// <summary>
        /// Die Teilmenge von <c>Stromrestbedarf</c> [MWh/a], die abweichende Kühlträger anteilig
        /// tragen (E34, Wahl 1) — sie trägt NICHT den Stromträger des Projekts.
        /// </summary>
        public static double AnteiligMwh(ErgebnisModel m)
        {
            double s = 0.0;
            foreach (Anteil a in Anteile(m)) if (!a.EigenerZaehler) s += a.MengeMwh;
            return s;
        }

        /// <summary>
        /// Der Kältestrom über eigene Zähler [MWh/a] (E34, Wahl 2) — er liegt NEBEN dem Netzbezug
        /// <c>Stromrestbedarf</c> und ist trotzdem Strom aus dem Netz.
        /// </summary>
        public static double EigenerZaehlerMwh(ErgebnisModel m)
        {
            double s = 0.0;
            foreach (Anteil a in Anteile(m)) if (a.EigenerZaehler) s += a.MengeMwh;
            return s;
        }

        /// <summary>
        /// Der Netzbezug des Kältestroms aller Anlagen [MWh/a] — die Summe der Modulspalte
        /// <c>Kaeltestrom_Netzbezug</c> und der Spalte <c>Kaeltestrom_Netzbezug_MWh</c> der Kältemaschinen, gleich welcher Träger ihn bepreist; <c>null</c>, wenn der Lauf
        /// keinen Kältestrom gerechnet hat.
        /// </summary>
        public static double? NetzbezugKaeltestromMwh(ErgebnisModel m)
        {
            double s = 0.0;
            bool gerechnet = false;
            foreach (Quelle q in Quellen(m))
            {
                if (!q.NetzbezugMwh.HasValue) continue;
                gerechnet = true;
                s += q.NetzbezugMwh.Value;
            }
            return gerechnet ? (double?)s : null;
        }
    }
}
