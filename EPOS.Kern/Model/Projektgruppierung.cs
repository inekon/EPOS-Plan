using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Stammgruppe des Projekttransfers</b> — ein Stammprojekt und die
    /// Varianten, die mit ihm im selben Paket reisen (Auftrag PI-1,
    /// Anwenderentscheid PI-Q1 vom 19.09.2026).
    ///
    /// <para><b>Eine Gruppe ist genau ein Paket.</b> Der Export schreibt je Gruppe
    /// eine <c>.wpx</c>-Datei, deren Hauptprojekt der <see cref="Stamm"/> ist; die
    /// <see cref="Varianten"/> reisen als Variantenbäume darin (Paketformat V2,
    /// Konzept Projekttransfer § 4).</para>
    /// </summary>
    /// <remarks>
    /// <para><b>Warum die Gruppe und nicht die Wahl das Paket bestimmt.</b> Wählt der
    /// Anwender in der Liste drei Varianten desselben Stamms, so sind das nicht drei
    /// Pakete: Jede Variante brauchte ihren Stamm, der Stamm käme dreimal mit, und am
    /// Ziel stünde er dreimal — einmal echt und zweimal als „(2)"/„(3)". Genau dieser
    /// Fall steht im Anwenderauftrag („nicht mehrfach").</para>
    /// </remarks>
    public sealed class Transfergruppe
    {
        public Transfergruppe(string stamm, int stammId,
                              IReadOnlyList<string> varianten,
                              IReadOnlyList<int> variantenIds,
                              bool nurVariante = false)
        {
            Stamm = stamm ?? "";
            StammId = stammId;
            Varianten = varianten ?? Array.Empty<string>();
            VariantenIds = variantenIds ?? Array.Empty<int>();
            NurVariante = nurVariante;
        }

        /// <summary>Der Name des Hauptprojekts dieses Pakets.</summary>
        public string Stamm { get; }

        /// <summary>Die Projekt-Id des Hauptprojekts.</summary>
        public int StammId { get; }

        /// <summary>Die mitreisenden Variantenprojekte, nach Namen sortiert.</summary>
        public IReadOnlyList<string> Varianten { get; }

        /// <summary>Die Projekt-Ids der mitreisenden Varianten, in derselben Folge.</summary>
        public IReadOnlyList<int> VariantenIds { get; }

        /// <summary>
        /// <b>Das Hauptprojekt ist selbst eine Variante</b>, deren Stamm im Bestand
        /// nicht auffindbar ist — es reist allein.
        ///
        /// <para>Dieser Fall bleibt ausdrücklich erlaubt: Er ist der Weg, auf dem eine
        /// einzelne Variante samt der PV-Vergütungsbeilage ihres Stamms weitergegeben
        /// wird (Konzept § 2.16). Der Export verweigert nur die Umkehrung — ein
        /// Variantenprojekt als Hauptprojekt, an dem weitere Varianten hängen.</para>
        /// </summary>
        public bool NurVariante { get; }

        /// <summary>Wie viele Projekte reisen in diesem Paket? Stamm plus Varianten.</summary>
        public int Projektzahl => 1 + Varianten.Count;
    }

    /// <summary>
    /// <b>Die Wahlregel des Projekttransfers</b> (Auftrag PI-1) — sie steht hier und
    /// sonst nirgends: Der Dialog fragt sie, um dem Anwender zu zeigen, was seine Wahl
    /// nach sich zieht, und der Kern fragt sie, um daraus Pakete zu machen.
    ///
    /// <para><b>Ohne Datenbank.</b> Alles, was die Regel braucht, steht bereits in der
    /// <see cref="ProjektKopfZeile"/>, die <c>ProjektCtrl.NamenListe()</c> in EINER
    /// Abfrage liefert (Id, Name, Stamm-Id, Stammname). Eine zweite Abfrage je Zeile
    /// wäre bei vierundzwanzig Projekten vierundzwanzig Rundläufe bei jedem
    /// Tastendruck im Suchfeld.</para>
    /// </summary>
    public static class Projektgruppierung
    {
        /// <summary>
        /// <b>Aus der Wahl des Anwenders die Pakete bilden.</b>
        ///
        /// <list type="number">
        ///   <item><description>Jede gewählte Zeile wird auf ihre Stammgruppe abgebildet:
        ///     eine Variante auf ihr Stammprojekt, alles andere auf sich selbst.</description></item>
        ///   <item><description>Jede Stammgruppe kommt GENAU EINMAL vor — auch wenn drei
        ///     ihrer Varianten gewählt sind.</description></item>
        ///   <item><description>Zu jeder Gruppe reisen ALLE Varianten ihres Stamms mit
        ///     (Vorbelegung TF1 des Konzepts), außer den in
        ///     <paramref name="variantenAbgewaehlt"/> benannten. <b>Eine ausdrücklich
        ///     gewählte Variante lässt sich nicht abwählen</b> — sie ist der Grund,
        ///     warum es das Paket gibt.</description></item>
        /// </list>
        ///
        /// <para>Die Gruppen kommen nach dem Namen des Stammprojekts sortiert; die
        /// Reihenfolge ist bestimmt, weil sie die Dateinamen und damit die
        /// Kollisionszählung („(2)", „(3)") festlegt.</para>
        /// </summary>
        /// <param name="bestand">Alle Projekte (<c>ProjektCtrl.NamenListe()</c>).</param>
        /// <param name="gewaehlt">Die Projekt-Ids, die der Anwender angehakt hat.</param>
        /// <param name="variantenAbgewaehlt">Projekt-Ids von Varianten, deren Häkchen
        /// der Anwender im Gruppenblock ausgeschaltet hat; <c>null</c> = keine.</param>
        public static IReadOnlyList<Transfergruppe> Gruppieren(
            IReadOnlyList<ProjektKopfZeile> bestand,
            IReadOnlyCollection<int> gewaehlt,
            IReadOnlyCollection<int> variantenAbgewaehlt = null)
        {
            var gruppen = new List<Transfergruppe>();
            if (bestand == null || bestand.Count == 0 || gewaehlt == null || gewaehlt.Count == 0)
                return gruppen;

            Dictionary<int, ProjektKopfZeile> nachId = NachId(bestand);
            var gewaehltSatz = new HashSet<int>(gewaehlt);
            var abgewaehlt = new HashSet<int>(variantenAbgewaehlt ?? Array.Empty<int>());

            // 1) Wahl -> Gruppenköpfe (jeder genau einmal).
            var koepfe = new List<int>();
            foreach (int id in gewaehlt)
            {
                int kopf = Gruppenkopf(nachId, id);
                if (kopf > 0 && !koepfe.Contains(kopf)) koepfe.Add(kopf);
            }

            // 2) Je Kopf die Varianten sammeln.
            foreach (int kopfId in koepfe.OrderBy(i => Name(nachId, i), StringComparer.CurrentCultureIgnoreCase)
                                         .ThenBy(i => i))
            {
                ProjektKopfZeile kopf = nachId[kopfId];

                var namen = new List<string>();
                var ids = new List<int>();
                foreach (ProjektKopfZeile v in bestand.Where(z => z.StammId == kopfId && z.Id != kopfId)
                                                      .OrderBy(z => z.Name, StringComparer.CurrentCultureIgnoreCase)
                                                      .ThenBy(z => z.Id))
                {
                    // Eine ausdrücklich gewählte Variante bleibt drin, komme was wolle.
                    if (!gewaehltSatz.Contains(v.Id) && abgewaehlt.Contains(v.Id)) continue;
                    namen.Add(v.Name);
                    ids.Add(v.Id);
                }

                bool nurVariante = kopf.StammId > 0 && !nachId.ContainsKey(kopf.StammId);
                gruppen.Add(new Transfergruppe(kopf.Name, kopf.Id, namen, ids, nurVariante));
            }

            return gruppen;
        }

        /// <summary>
        /// <b>Welche Stammprojekte kommen durch die Wahl DAZU?</b> — die Zeilen, die der
        /// Anwender nicht angehakt hat und die trotzdem mitreisen, weil eine ihrer
        /// Varianten gewählt ist.
        ///
        /// <para>Der Dialog zeigt sie in der Spalte „mitgenommen" und nennt ihre Zahl im
        /// Hinweisbanner. <b>Eine stille Mitnahme wäre eine Überraschung</b>: Das Paket
        /// trüge ein Projekt, das der Anwender nie gewählt hat, und am Zielrechner
        /// entstünde es.</para>
        /// </summary>
        public static IReadOnlyList<int> Nachgezogen(
            IReadOnlyList<ProjektKopfZeile> bestand, IReadOnlyCollection<int> gewaehlt)
        {
            var nach = new List<int>();
            if (bestand == null || gewaehlt == null || gewaehlt.Count == 0) return nach;

            Dictionary<int, ProjektKopfZeile> nachId = NachId(bestand);
            var gewaehltSatz = new HashSet<int>(gewaehlt);

            foreach (int id in gewaehlt)
            {
                int kopf = Gruppenkopf(nachId, id);
                if (kopf <= 0 || kopf == id) continue;
                if (gewaehltSatz.Contains(kopf) || nach.Contains(kopf)) continue;
                nach.Add(kopf);
            }
            return nach;
        }

        /// <summary>
        /// <b>Wie viele Varianten hängen an jedem Projekt?</b> — der Wert der Spalte
        /// „Varianten" der Transferliste, in EINEM Durchlauf über den Bestand statt in
        /// einer Abfrage je Zeile.
        /// </summary>
        public static IReadOnlyDictionary<int, int> Variantenzahl(IReadOnlyList<ProjektKopfZeile> bestand)
        {
            var zahl = new Dictionary<int, int>();
            if (bestand == null) return zahl;

            foreach (ProjektKopfZeile z in bestand)
            {
                if (z.StammId <= 0 || z.StammId == z.Id) continue;
                zahl[z.StammId] = zahl.TryGetValue(z.StammId, out int n) ? n + 1 : 1;
            }
            return zahl;
        }

        // ------------------------------------------------------------------
        //  Handwerkszeug
        // ------------------------------------------------------------------

        /// <summary>
        /// Der Gruppenkopf einer Zeile: bei einer Variante ihr Stammprojekt, sonst sie
        /// selbst. Ein Stamm, den der Bestand nicht führt, ist kein Kopf — dann bleibt
        /// die Variante ihr eigener (und reist allein, <see cref="Transfergruppe.NurVariante"/>).
        /// </summary>
        private static int Gruppenkopf(Dictionary<int, ProjektKopfZeile> nachId, int id)
        {
            if (!nachId.TryGetValue(id, out ProjektKopfZeile z)) return 0;
            if (z.StammId > 0 && z.StammId != id && nachId.ContainsKey(z.StammId)) return z.StammId;
            return id;
        }

        private static Dictionary<int, ProjektKopfZeile> NachId(IReadOnlyList<ProjektKopfZeile> bestand)
        {
            var map = new Dictionary<int, ProjektKopfZeile>();
            foreach (ProjektKopfZeile z in bestand)
                if (z != null && z.Id > 0 && !map.ContainsKey(z.Id)) map[z.Id] = z;
            return map;
        }

        private static string Name(Dictionary<int, ProjektKopfZeile> nachId, int id)
            => nachId.TryGetValue(id, out ProjektKopfZeile z) ? z.Name : "";
    }
}
