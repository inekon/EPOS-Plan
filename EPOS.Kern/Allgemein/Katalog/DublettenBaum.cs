using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Die vier Ebenen des Dublettenbaums (iU9-W14c.0h).</summary>
    public enum DublettenKnotenArt
    {
        /// <summary>Ein Katalog — „{0} ({1} Sätze)".</summary>
        Wurzel,
        /// <summary>Namens- oder Inhaltsdubletten — „Namensdubletten ({0} Gruppen)".</summary>
        Ast,
        /// <summary>Eine Dublettengruppe.</summary>
        Gruppe,
        /// <summary>Ein einzelner Satz — „ID {n} — {Name}".</summary>
        Blatt
    }

    /// <summary>
    /// Ein Knoten des Dublettenbefunds — <b>anzeigefrei</b> (iU9-W14c.0h).
    ///
    /// <para><b>Warum ein <see cref="Schluessel"/> und kein Index.</b> Der Wirt bekommt
    /// die Auswahl als Zeichenkette zurück und schlägt sie in seinem eigenen
    /// Verzeichnis nach. Ein Index bräche, sobald ein Neuscan die Reihenfolge ändert —
    /// und genau das tut der Dialog nach jeder Aktion.</para>
    ///
    /// <para><b>Warum <see cref="Kennzeichen"/> getrennt vom <see cref="Text"/>.</b> Der
    /// Vorläufer hängte <c>" [Auslieferung]"</c> an den Blatttext. Getrennt lässt es
    /// sich als Abzeichen zeichnen (und in <c>forced-colors</c> als Rahmen); zusammen
    /// wäre es nur Text.</para>
    /// </summary>
    public sealed class DublettenKnoten
    {
        public DublettenKnoten(string schluessel, string text, DublettenKnotenArt art,
                               string katalog, IReadOnlyList<DublettenKnoten> kinder,
                               bool vonVornOffen, string kennzeichen = "",
                               int gruppenIndex = -1, bool istNamensgruppe = false,
                               int satzId = 0, bool istAuslieferung = false)
        {
            Schluessel = schluessel ?? "";
            Text = text ?? "";
            Art = art;
            Katalog = katalog ?? "";
            Kinder = kinder ?? Array.Empty<DublettenKnoten>();
            VonVornOffen = vonVornOffen;
            Kennzeichen = kennzeichen ?? "";
            GruppenIndex = gruppenIndex;
            IstNamensgruppe = istNamensgruppe;
            SatzId = satzId;
            IstAuslieferung = istAuslieferung;
        }

        /// <summary>Eindeutig im ganzen Baum, sprachneutral.</summary>
        public string Schluessel { get; }

        /// <summary>Die fertige Zeile.</summary>
        public string Text { get; }

        /// <summary>Optionales Abzeichen — „[Auslieferung]".</summary>
        public string Kennzeichen { get; }

        public DublettenKnotenArt Art { get; }

        /// <summary>Der Registry-Schlüssel des Katalogs, zu dem der Knoten gehört.</summary>
        public string Katalog { get; }

        /// <summary>Index der Gruppe in ihrer Liste; −1 an Wurzel und Ast.</summary>
        public int GruppenIndex { get; }

        /// <summary>Steht die Gruppe unter „Namensdubletten"? Sonst unter „Inhaltsdubletten".</summary>
        public bool IstNamensgruppe { get; }

        /// <summary>Die Satz-Id am Blatt; 0 sonst.</summary>
        public int SatzId { get; }

        /// <summary>Trägt der Satz <c>ReadOnly</c>, gehört er also zur Auslieferung?</summary>
        public bool IstAuslieferung { get; }

        /// <summary>Vorgabe des Aufklappzustands.</summary>
        public bool VonVornOffen { get; }

        public IReadOnlyList<DublettenKnoten> Kinder { get; }
    }

    /// <summary>
    /// Baut aus den Scanergebnissen den vierstufigen Dublettenbaum (iU9-W14c.0h).
    ///
    /// <para><b>Warum das hier steht.</b> <c>Form_KatalogDubletten.BaumFuellen</c> baute
    /// einen <c>TreeView</c> aus <c>TreeNode</c>-Objekten, deren <c>Tag</c> einen
    /// <c>KnotenInfo</c> mit <c>KatalogDefinition</c>, <c>DublettenGruppe</c> und
    /// <c>KatalogSatz</c> trug — also drei Fachtypen, von denen einer eine
    /// <c>DataRow</c> führt. Der Baum selbst ist aber anzeigefrei: vier Ebenen, ein
    /// Text je Knoten, ein Kennzeichen am Blatt. Genau das steht hier.</para>
    ///
    /// <para><b>Bitgleich zum Vorläufer:</b> die Reihenfolge der Registry, nur gescannte
    /// Kataloge; ein Ast entsteht NUR, wenn er Gruppen hat; eine Wurzel entsteht
    /// IMMER, auch für einen Katalog ohne Dubletten; <b>Wurzel und Ast sind von vorn
    /// offen, die Gruppen zu</b> (<c>wurzel.Expand()</c> und
    /// <c>foreach (ast) ast.Expand()</c>).</para>
    /// </summary>
    public static class DublettenBaum
    {
        /// <summary>
        /// Inhaltsgruppen für die Anzeige: Gruppen, deren Sätze alle denselben
        /// normalisierten Namen tragen, stehen bereits als Namensgruppe im Baum und
        /// werden hier NICHT wiederholt.
        /// </summary>
        public static IReadOnlyList<DublettenGruppe> AnzuzeigendeInhaltsgruppen(ScanErgebnis erg)
        {
            var liste = new List<DublettenGruppe>();
            if (erg == null) return liste;
            foreach (DublettenGruppe g in erg.Inhaltsgruppen)
                if (g.VerschiedeneNamen) liste.Add(g);
            return liste;
        }

        /// <summary>
        /// Der ganze Baum, in der Reihenfolge der Registry. Nur Kataloge, für die ein
        /// Scanergebnis vorliegt.
        /// </summary>
        public static IReadOnlyList<DublettenKnoten> Bauen(
            IReadOnlyDictionary<string, ScanErgebnis> ergebnisse)
        {
            var wurzeln = new List<DublettenKnoten>();
            if (ergebnisse == null) return wurzeln;

            foreach (KatalogDefinition k in KatalogRegistry.Alle)
            {
                ScanErgebnis erg;
                if (!ergebnisse.TryGetValue(k.Schluessel, out erg) || erg == null) continue;

                var aeste = new List<DublettenKnoten>();
                if (erg.Fehler == null)
                {
                    if (erg.Namensgruppen.Count > 0)
                        aeste.Add(Ast(k, erg.Namensgruppen, true,
                                      string.Format(CultureInfo.CurrentCulture,
                                                    MyResource.Resource.ADM_DUBLETTEN_AST_NAMEN,
                                                    erg.Namensgruppen.Count)));

                    IReadOnlyList<DublettenGruppe> inhalt = AnzuzeigendeInhaltsgruppen(erg);
                    if (inhalt.Count > 0)
                        aeste.Add(Ast(k, inhalt, false,
                                      string.Format(CultureInfo.CurrentCulture,
                                                    MyResource.Resource.ADM_DUBLETTEN_AST_INHALT,
                                                    inhalt.Count)));
                }

                wurzeln.Add(new DublettenKnoten(
                    "K:" + k.Schluessel,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ADM_DUBLETTEN_WURZEL,
                                  KatalogRegistry.Anzeige(k.Schluessel), erg.Saetze.Count),
                    DublettenKnotenArt.Wurzel, k.Schluessel, aeste, vonVornOffen: true));
            }
            return wurzeln;
        }

        private static DublettenKnoten Ast(KatalogDefinition k, IReadOnlyList<DublettenGruppe> gruppen,
                                           bool namen, string text)
        {
            string astSchluessel = "K:" + k.Schluessel + (namen ? "/N" : "/I");
            var kinder = new List<DublettenKnoten>();

            for (int i = 0; i < gruppen.Count; i++)
            {
                DublettenGruppe g = gruppen[i];
                string gruppenSchluessel = astSchluessel + "/" + i.ToString(CultureInfo.InvariantCulture);

                var blaetter = new List<DublettenKnoten>();
                foreach (KatalogSatz s in g.Saetze)
                    blaetter.Add(new DublettenKnoten(
                        gruppenSchluessel + "/" + s.Id.ToString(CultureInfo.InvariantCulture),
                        "ID " + s.Id.ToString(CultureInfo.CurrentCulture) + " — " + s.Name,
                        DublettenKnotenArt.Blatt, k.Schluessel,
                        Array.Empty<DublettenKnoten>(), vonVornOffen: false,
                        kennzeichen: s.ReadOnly ? MyResource.Resource.ADM_DUBLETTEN_AUSLIEFERUNG : "",
                        gruppenIndex: i, istNamensgruppe: namen,
                        satzId: s.Id, istAuslieferung: s.ReadOnly));

                // Namensgruppe: der Name selbst. Inhaltsgruppe: "gleicher Inhalt: {0}"
                // mit den verschiedenen Namen, " / "-getrennt.
                string gruppentext = namen
                    ? g.Saetze[0].Name
                    : string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.ADM_DUBLETTEN_GRUPPE_INHALT, NamensListe(g));

                kinder.Add(new DublettenKnoten(gruppenSchluessel, gruppentext,
                                               DublettenKnotenArt.Gruppe, k.Schluessel, blaetter,
                                               vonVornOffen: false,
                                               gruppenIndex: i, istNamensgruppe: namen));
            }

            return new DublettenKnoten(astSchluessel, text, DublettenKnotenArt.Ast,
                                       k.Schluessel, kinder, vonVornOffen: true);
        }

        /// <summary>Die verschiedenen Namen einer Inhaltsgruppe, <c>" / "</c>-getrennt.</summary>
        public static string NamensListe(DublettenGruppe g)
        {
            var namen = new List<string>();
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            if (g != null)
                foreach (KatalogSatz s in g.Saetze)
                    if (gesehen.Add(s.NameNormalisiert)) namen.Add(s.Name);
            return string.Join(" / ", namen.ToArray());
        }
    }
}
