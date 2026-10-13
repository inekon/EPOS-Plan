using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zeile der Gegenüberstellung „Komponenten im Vergleich — Stammprojekt und
    /// Varianten“ (Seite „Übersicht“ des Reiters „Berichte &amp; Kosten“).
    ///
    /// <para>Sie ist anzeigefrei: Der Kern sagt, WAS in einer Zeile steht, die
    /// Razor-Komponente sagt, wie sie aussieht. Die Hülle bildet sie auf ihren
    /// eigenen Zeilentyp ab.</para>
    /// </summary>
    public sealed class KomponentenVergleichZeile
    {
        /// <summary>Das Gewerk als SCHLÜSSEL (<see cref="ProjektDetails.GewerkTabellen"/>) — nur in der ersten Zeile seines
        /// Blocks belegt; angezeigt über <see cref="AbweichungsErmittler.Gewerkname"/>.</summary>
        public string Gewerk = "";

        /// <summary>Das Merkmal: „Anzahl Komponenten“ bzw. „Komponente n“.</summary>
        public string Merkmal = "";

        /// <summary>Je Version eine fertig formatierte Zelle (Stamm zuerst).</summary>
        public IReadOnlyList<string> Zellen = Array.Empty<string>();

        /// <summary>
        /// Kurztext je Zelle — die Merkmale der Komponente, die dort steht; leer, wo
        /// die Version diese Komponente nicht führt.
        /// </summary>
        public IReadOnlyList<string> Kurztexte = Array.Empty<string>();
    }

    /// <summary>
    /// Ein Eintrag einer Zelle der Gegenüberstellung JE GEWERK: gleiche Komponenten
    /// einer Version zusammengefasst zu „n × Bezeichner“ samt ihren Kennwerten.
    /// </summary>
    public sealed class KomponentenVergleichEintrag
    {
        /// <summary>Wie oft die Version diese Komponente führt (≥ 1).</summary>
        public int Anzahl;

        /// <summary>Der Bezeichner der Komponente (<see cref="KomponentenVergleich.OHNE_WERT"/>, wenn er fehlt).</summary>
        public string Bezeichner = "";

        /// <summary>
        /// Die wichtigsten Kennwerte einer Komponente, fertig formatiert („16 kW“,
        /// „5,5 kW el · 12,5 kW th“, „1.000 l“); leer, wo die Daten keinen hergeben.
        /// </summary>
        public string Kennwerte = "";

        /// <summary>Die Merkmale der Komponente als Kurztext (<see cref="AbweichungsErmittler.MerkmaleText"/>).</summary>
        public string Kurztext = "";
    }

    /// <summary>Die Zelle einer Version in der Gegenüberstellung je Gewerk.</summary>
    public sealed class KomponentenVergleichZelle
    {
        /// <summary>Die Einträge in der Reihenfolge der Anlagenzeilen; leer = Gewerk nicht vorhanden.</summary>
        public IReadOnlyList<KomponentenVergleichEintrag> Eintraege = Array.Empty<KomponentenVergleichEintrag>();

        /// <summary>Führt die Version das Gewerk?</summary>
        public bool Vorhanden => Eintraege.Count > 0;

        /// <summary>
        /// Weicht die Zelle vom Stammprojekt ab (Bestand, Bezeichner, Anzahl oder
        /// Kennwerte)? Die Stammzelle selbst weicht nie ab.
        /// </summary>
        public bool WeichtAb;
    }

    /// <summary>
    /// Eine Zeile der Gegenüberstellung JE GEWERK — die Seite „Übersicht“ zeigt je
    /// Gewerk eine Zeile, je Version eine Zelle.
    /// </summary>
    public sealed class KomponentenVergleichGewerk
    {
        /// <summary>Das Gewerk als SCHLÜSSEL (<see cref="ProjektDetails.GewerkTabellen"/>).</summary>
        public string Gewerk = "";

        /// <summary>Je Version eine Zelle (Stamm zuerst).</summary>
        public IReadOnlyList<KomponentenVergleichZelle> Zellen = Array.Empty<KomponentenVergleichZelle>();

        /// <summary>Weicht wenigstens eine Version in diesem Gewerk vom Stamm ab?</summary>
        public bool WeichtAb
        {
            get
            {
                foreach (KomponentenVergleichZelle z in Zellen)
                    if (z.WeichtAb) return true;
                return false;
            }
        }
    }

    /// <summary>
    /// Die ZEILENBILDUNG der Gegenüberstellung Stamm ↔ Varianten (Anwenderbefund
    /// W5‑E‑2 vom 05.09.2026).
    ///
    /// <para><b>Was gezeigt wird.</b> Ausschließlich die im Projekt bzw. in der
    /// Variante tatsächlich VERWENDETEN Erzeugerkomponenten — die sieben Gewerke aus
    /// <see cref="ProjektDetails.GewerkTabellen"/> (Wärmepumpe, BHKW, Spitzenkessel,
    /// Solarthermie, Photovoltaik, Pufferspeicher, Stromspeicher), je Gewerk die
    /// Stückzahl und darunter eine Zeile je Komponente mit ihrem Bezeichner. Ein
    /// Gewerk, das keine der Versionen führt, erscheint gar nicht.</para>
    ///
    /// <para><b>Was mit Absicht NICHT gezeigt wird.</b> Die Blöcke „Anlage“ und
    /// „Gebäude“ der deklarativen Feldliste des <see cref="AbweichungsErmittler"/>.
    /// Sie sind Konfigurationsblöcke OHNE Komponentenbestand — Betriebsart,
    /// Vor- und Rücklauftemperatur, Abschaltpunkt, Neigung, Azimut, Solaranteil,
    /// Wärmebedarf, Wohnfläche: lauter PARAMETER, keine Komponenten. Der Anwender
    /// hat sie am 05.09.2026 als Befund gemeldet („Gewerk Anlage gibt es nicht.
    /// Dort stehen Parameter.“); der Berichtsbaustein
    /// <c>BausteineProjekt.Komponentenuebersicht</c> hält es seit jeher so und
    /// zählt allein über <see cref="ProjektDetails.GewerkTabellen"/>.</para>
    ///
    /// <para><b>Der Vergleich der PARAMETER bleibt erhalten</b> — dort, wo er
    /// hingehört: in der UNTERSCHIEDSansicht einer Variante
    /// (<see cref="AbweichungsErmittler.Vergleiche"/>) und im Bericht. Dort zeigt
    /// eine Zeile eine ÄNDERUNG und trägt die Übernahme; hier zeigte sie nur einen
    /// Wert, den das Projekt ohnehin führt.</para>
    ///
    /// <para><b>Warum im Kern.</b> Beide Schalen — die Windows-Hülle und die
    /// iOS-Wurzel — brauchen dieselben Zeilen. Sie stand bis W5‑E‑2 im
    /// Oberflächencode (<c>UebersichtSeiteGaben.FuelleVergleich</c>) und wäre auf
    /// iOS ein zweites Mal entstanden.</para>
    /// </summary>
    public static class KomponentenVergleich
    {
        /// <summary>
        /// Zelltext für „führt diese Version nicht“ — derselbe Strich, den
        /// <see cref="AbweichungsErmittler.Formatiere"/> für einen leeren Wert setzt.
        /// Eine Schreibweise für „hier steht nichts“ statt einer zweiten.
        /// </summary>
        public const string OHNE_WERT = "—";

        /// <summary>Der Trenner der Kurztexte — je Merkmal eine Zeile.</summary>
        public const string KURZTEXT_TRENNER = "\r\n";

        /// <summary>
        /// Baut die Zeilen der Gegenüberstellung: je verwendetem Erzeugergewerk eine
        /// Kopfzeile mit der Stückzahl und darunter eine Zeile je Komponente.
        /// </summary>
        /// <param name="versionen">
        /// Die Versionen in SPALTENreihenfolge — das Stammprojekt zuerst, dann die
        /// Varianten. Eine leere Liste liefert keine Zeilen.
        /// </param>
        /// <param name="kurztextTrenner">
        /// Trenner der Merkmale im Kurztext einer Zelle; Vorgabe
        /// <see cref="KURZTEXT_TRENNER"/>.
        /// </param>
        public static List<KomponentenVergleichZeile> Gegenueberstellung(
            IReadOnlyList<ProjektDetails> versionen, string kurztextTrenner = KURZTEXT_TRENNER)
        {
            var ziel = new List<KomponentenVergleichZeile>();
            if (versionen == null || versionen.Count == 0) return ziel;

            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                string gewerk = g.Key;

                // Wie viele Komponenten führt die reichste Version? 0 = das Gewerk wird
                // von KEINER Version verwendet und erscheint deshalb nicht. Gezählt wird
                // der über Tab_Energieanlagen ermittelte VERBAUTE Bestand
                // (ProjektDetails.LadeGewerk), nicht der rohe Zeilenbestand der
                // Gerätetabelle.
                int maxKomp = 0;
                foreach (ProjektDetails d in versionen)
                    maxKomp = Math.Max(maxKomp, AbweichungsErmittler.Anzahl(d, gewerk));
                if (maxKomp == 0) continue;

                // Kopfzeile des Gewerks: die Stückzahl je Version. Sie ist der Grund
                // für diese Ansicht — der Bestandsvergleich ohne Klickerei durch die
                // Varianten.
                var anzahlen = new List<string>(versionen.Count);
                foreach (ProjektDetails d in versionen)
                    anzahlen.Add(AbweichungsErmittler.AnzahlText(AbweichungsErmittler.Anzahl(d, gewerk)));

                ziel.Add(new KomponentenVergleichZeile
                {
                    Gewerk = gewerk,
                    Merkmal = AbweichungsErmittler.MerkmalAnzahl,
                    Zellen = anzahlen
                });

                // Eine Zeile JE KOMPONENTE mit ihrem Bezeichner je Version
                // (Nutzerauftrag 28.08.2026); die Merkmale stehen im Kurztext der Zelle.
                AbweichungsErmittler.Merkmal bez = AbweichungsErmittler.BezeichnerMerkmal(gewerk);
                for (int k = 0; k < maxKomp; k++)
                {
                    var namen = new List<string>(versionen.Count);
                    var kurz = new List<string>(versionen.Count);
                    foreach (ProjektDetails d in versionen)
                    {
                        DataRow rk = AbweichungsErmittler.KomponenteZeile(d, gewerk, k);
                        namen.Add(rk == null || bez == null
                                  ? OHNE_WERT
                                  : AbweichungsErmittler.Formatiere(rk, bez));
                        kurz.Add(rk == null
                                 ? ""
                                 : AbweichungsErmittler.MerkmaleText(rk, gewerk, kurztextTrenner));
                    }

                    ziel.Add(new KomponentenVergleichZeile
                    {
                        Merkmal = maxKomp == 1
                            ? MyResource.Resource.BK_SP_KOMPONENTE
                            : string.Format(MyResource.Resource.BK_SP_KOMPONENTE_N, k + 1),
                        Zellen = namen,
                        Kurztexte = kurz
                    });
                }
            }

            return ziel;
        }

        /// <summary>
        /// Die Gegenüberstellung JE GEWERK: eine Zeile je verwendetem Erzeugergewerk,
        /// je Version eine Zelle mit „n × Komponente“ und ihren Kennwerten, dazu, ob
        /// die Zelle vom Stammprojekt abweicht. Dieselbe Auswahl wie
        /// <see cref="Gegenueberstellung"/>: ein Gewerk, das keine Version führt,
        /// erscheint nicht.
        /// </summary>
        /// <param name="versionen">Die Versionen in Spaltenreihenfolge, das Stammprojekt zuerst.</param>
        /// <param name="kurztextTrenner">Trenner der Merkmale im Kurztext eines Eintrags.</param>
        public static List<KomponentenVergleichGewerk> JeGewerk(
            IReadOnlyList<ProjektDetails> versionen, string kurztextTrenner = KURZTEXT_TRENNER)
        {
            var ziel = new List<KomponentenVergleichGewerk>();
            if (versionen == null || versionen.Count == 0) return ziel;

            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                string gewerk = g.Key;
                bool verwendet = false;
                foreach (ProjektDetails d in versionen)
                    if (AbweichungsErmittler.Anzahl(d, gewerk) > 0) { verwendet = true; break; }
                if (!verwendet) continue;

                var zellen = new List<KomponentenVergleichZelle>(versionen.Count);
                foreach (ProjektDetails d in versionen)
                    zellen.Add(new KomponentenVergleichZelle { Eintraege = Eintraege(d, gewerk, kurztextTrenner) });

                // Verglichen wird, was die Zelle ZEIGT: Anzahl, Bezeichner und Kennwerte
                // je Eintrag. Die Stammzelle ist die Referenz und weicht nie ab.
                for (int i = 1; i < zellen.Count; i++)
                    zellen[i].WeichtAb = !GleicherInhalt(zellen[0], zellen[i]);

                ziel.Add(new KomponentenVergleichGewerk { Gewerk = gewerk, Zellen = zellen });
            }
            return ziel;
        }

        /// <summary>
        /// Die Einträge einer Version in einem Gewerk: gleiche Komponenten (gleicher
        /// Bezeichner, gleiche Kennwerte) zusammengefasst, in der Reihenfolge ihres
        /// ersten Auftretens.
        /// </summary>
        private static List<KomponentenVergleichEintrag> Eintraege(ProjektDetails d, string gewerk, string trenner)
        {
            var liste = new List<KomponentenVergleichEintrag>();
            int n = AbweichungsErmittler.Anzahl(d, gewerk);
            AbweichungsErmittler.Merkmal bez = AbweichungsErmittler.BezeichnerMerkmal(gewerk);
            for (int k = 0; k < n; k++)
            {
                DataRow rk = AbweichungsErmittler.KomponenteZeile(d, gewerk, k);
                if (rk == null) continue;
                string name = bez == null ? OHNE_WERT : AbweichungsErmittler.Formatiere(rk, bez);
                string kennwerte = Kennwerte(d, gewerk, k);

                KomponentenVergleichEintrag vorhanden = null;
                foreach (KomponentenVergleichEintrag e in liste)
                    if (e.Bezeichner == name && e.Kennwerte == kennwerte) { vorhanden = e; break; }
                if (vorhanden != null) { vorhanden.Anzahl++; continue; }

                liste.Add(new KomponentenVergleichEintrag
                {
                    Anzahl = 1,
                    Bezeichner = name,
                    Kennwerte = kennwerte,
                    Kurztext = AbweichungsErmittler.MerkmaleText(rk, gewerk, trenner)
                });
            }
            return liste;
        }

        private static bool GleicherInhalt(KomponentenVergleichZelle a, KomponentenVergleichZelle b)
        {
            if (a.Eintraege.Count != b.Eintraege.Count) return false;
            for (int i = 0; i < a.Eintraege.Count; i++)
            {
                KomponentenVergleichEintrag x = a.Eintraege[i], y = b.Eintraege[i];
                if (x.Anzahl != y.Anzahl
                    || !string.Equals(x.Bezeichner, y.Bezeichner, StringComparison.Ordinal)
                    || !string.Equals(x.Kennwerte, y.Kennwerte, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Die wichtigsten Kennwerte der n-ten Komponente eines Gewerks, fertig
        /// formatiert in der Anzeigesprache — leer, wo die Daten keinen hergeben:
        ///
        /// <list type="bullet">
        /// <item>Wärmepumpe: Nennleistung in kW (<c>Tab_WP.Nennleistung</c>),</item>
        /// <item>Spitzenkessel: Leistung in kW (<c>Tab_Heizkessel.Ptherm</c>),</item>
        /// <item>BHKW: kW el und kW th (<c>Tab_BHKW.Pel</c>, <c>Ptherm</c>),</item>
        /// <item>Pufferspeicher: Volumen in l (<c>Tab_Pufferspeicher.Gesamtvolumen</c>, sonst
        /// <c>Volumen</c> der Anlagenzeile),</item>
        /// <item>Stromspeicher: Kapazität in kWh (<c>Tab_Stromspeicher.Energie</c>),</item>
        /// <item>Photovoltaik: Leistung in kWp (<c>PV_Leistung</c> der Anlagenzeile),</item>
        /// <item>Solarthermie: Aperturfläche in m² (<c>Tab_Solarkollektoren.Aperturflaeche</c>
        /// × <c>Kollektormodulanzahl</c> der Anlagenzeile).</item>
        /// </list>
        ///
        /// <para>Die Anlagenzeile ist die, aus der die Komponente stammt: Die
        /// Komponentenzeilen kommen in der Reihenfolge der Anlagenzeilen
        /// (<see cref="KomponentenUebernahmeCtrl.GeraeteJeAnlagenzeile"/>); zeigen zwei
        /// Anlagenzeilen auf dasselbe Gerät, zählt das Auftreten.</para>
        /// </summary>
        public static string Kennwerte(ProjektDetails d, string gewerk, int index)
        {
            DataRow rk = AbweichungsErmittler.KomponenteZeile(d, gewerk, index);
            if (rk == null) return "";
            DataRow anlage = AnlageZu(d, gewerk, index);
            var teile = new List<string>(2);
            switch (gewerk)
            {
                case "Wärmepumpe":
                    Kennwert(teile, ProjektDetails.D(rk, "Nennleistung"), "kW");
                    break;
                case "Spitzenkessel":
                    Kennwert(teile, ProjektDetails.D(rk, "Ptherm"), "kW");
                    break;
                case "BHKW":
                    Kennwert(teile, ProjektDetails.D(rk, "Pel"), "kW el");
                    Kennwert(teile, ProjektDetails.D(rk, "Ptherm"), "kW th");
                    break;
                case "Pufferspeicher":
                    Kennwert(teile, ProjektDetails.D(rk, "Gesamtvolumen") ?? ProjektDetails.D(anlage, "Volumen"), "l");
                    break;
                case "Stromspeicher":
                    Kennwert(teile, ProjektDetails.D(rk, "Energie"), "kWh");
                    break;
                case "Photovoltaik":
                    Kennwert(teile, ProjektDetails.D(anlage, "PV_Leistung"), "kWp");
                    break;
                case "Solarthermie":
                    double? apertur = ProjektDetails.D(rk, "Aperturflaeche");
                    double? module = ProjektDetails.D(anlage, "Kollektormodulanzahl");
                    Kennwert(teile, apertur.HasValue && module.HasValue && module.Value > 0
                                    ? apertur.Value * module.Value : apertur, "m²");
                    break;
            }
            return string.Join(" · ", teile);
        }

        /// <summary>Ein Kennwert, sofern er belegt und größer null ist: „16 kW“, „1.000 l“, „12,8 kWh“.</summary>
        private static void Kennwert(List<string> teile, double? wert, string einheit)
        {
            if (!wert.HasValue || double.IsNaN(wert.Value) || wert.Value <= 0) return;
            teile.Add(wert.Value.ToString("#,0.#", BerichtTexte.Kultur) + " " + einheit);
        }

        /// <summary>
        /// Die Anlagenzeile (<c>Tab_Energieanlagen</c>), aus der die n-te Komponente
        /// eines Gewerks stammt; null, wo keine zuzuordnen ist.
        /// </summary>
        private static DataRow AnlageZu(ProjektDetails d, string gewerk, int index)
        {
            KomponentenUebernahmeCtrl.GewerkPlan plan;
            if (d == null || d.Anlagen == null || gewerk == null
                || !KomponentenUebernahmeCtrl.Plaene.TryGetValue(gewerk, out plan)) return null;
            DataTable geraete;
            if (!d.KomponentenAlle.TryGetValue(gewerk, out geraete) || geraete == null
                || index >= geraete.Rows.Count || !geraete.Columns.Contains("ID")
                || !d.Anlagen.Columns.Contains("ID_Type") || !d.Anlagen.Columns.Contains(plan.AnlagenFk))
                return null;

            object id = geraete.Rows[index]["ID"];
            if (id == null || id == DBNull.Value) return null;
            int vorher = 0;
            for (int j = 0; j < index; j++)
                if (Equals(Convert.ToString(geraete.Rows[j]["ID"]), Convert.ToString(id))) vorher++;

            foreach (DataRow a in d.Anlagen.Rows)
            {
                int typ = (int)(ProjektDetails.D(a, "ID_Type") ?? 0);
                if (Array.IndexOf(plan.AnlagenTypen, typ) < 0) continue;
                if (!string.Equals(ProjektDetails.S(a, plan.AnlagenFk), Convert.ToString(id), StringComparison.Ordinal))
                    continue;
                if (vorher-- == 0) return a;
            }
            return null;
        }
    }
}
