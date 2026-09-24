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
    /// Autarkie), nehmen die Mengen von hier, statt sie neu zu bilden.</para>
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
            if (m == null || m.Waermepumpe == null || m.Waermepumpe.Module == null) return liste;

            foreach (ErgebnisWaermepumpeModulModel mo in m.Waermepumpe.Module)
            {
                if (mo == null || !mo.Kuehl_CarrierId.HasValue || mo.Kuehl_CarrierId.Value <= 0) continue;
                double menge = mo.Kaeltestrom_Netzbezug ?? 0.0;
                if (!(menge > 0)) continue;

                bool zaehler = mo.Kuehl_EigenerZaehler == true;
                Anteil a = liste.Find(x => x.Traeger == mo.Kuehl_CarrierId.Value && x.EigenerZaehler == zaehler);
                if (a == null)
                {
                    a = new Anteil { Traeger = mo.Kuehl_CarrierId.Value, EigenerZaehler = zaehler };
                    liste.Add(a);
                }
                a.MengeMwh += menge;
                a.Anlagen.Add(string.IsNullOrEmpty(mo.Modul) ? "?" : mo.Modul);
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
        /// <c>Kaeltestrom_Netzbezug</c>, gleich welcher Träger ihn bepreist; <c>null</c>, wenn der Lauf
        /// keinen Kältestrom gerechnet hat.
        /// </summary>
        public static double? NetzbezugKaeltestromMwh(ErgebnisModel m)
        {
            if (m == null || m.Waermepumpe == null || m.Waermepumpe.Module == null) return null;
            double s = 0.0;
            bool gerechnet = false;
            foreach (ErgebnisWaermepumpeModulModel mo in m.Waermepumpe.Module)
            {
                if (mo == null || !mo.Kaeltestrom_Netzbezug.HasValue) continue;
                gerechnet = true;
                s += mo.Kaeltestrom_Netzbezug.Value;
            }
            return gerechnet ? (double?)s : null;
        }
    }
}
