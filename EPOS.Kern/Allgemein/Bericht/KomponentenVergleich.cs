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
        /// <summary>Das Gewerk — nur in der ersten Zeile seines Blocks belegt.</summary>
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
                    Merkmal = AbweichungsErmittler.MERKMAL_ANZAHL,
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
    }
}
