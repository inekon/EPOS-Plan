using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Der Befund eines Wärmepumpensatzes im Kälteimport (Entscheid E119).</summary>
    public enum KaelteimportBefund
    {
        /// <summary>Kühlfähig nach E15 und mit mindestens einem gültigen Kühlblock — wird angeboten.</summary>
        Kuehlfaehig,

        /// <summary>Der Satz führt keinen Kühlblock (Satzart <c>710.09</c> mit Betriebsart 2).</summary>
        KeineKuehlkennlinie,

        /// <summary>Jeder Kühlblock liegt in Heizlage (<see cref="KuehlblockBefund.Heizlage"/>).</summary>
        NurHeizlage,

        /// <summary>Jeder Kühlblock trägt vertauschte Achsen (<see cref="KuehlblockBefund.AchsenVertauscht"/>).</summary>
        NurAchsenVertauscht,

        /// <summary>Kein Kühlblock ist gültig, die Gründe sind gemischt (Heizlage und vertauschte Achsen).</summary>
        KeinGueltigerKuehlblock,

        /// <summary>Gültige Kühlblöcke, aber keine Nennkühlleistung (Satz <c>700</c>, Feld 20) größer null.</summary>
        KeineNennkuehlleistung
    }

    /// <summary>
    /// <b>Der Filter des Kälteimports</b> (Entscheid E119 vom 10.10.2026): Aus einer VDI-3805-Datei
    /// nach Blatt 22 werden im Menüpunkt „Import Kälteanlagen VDI 3805“ nur Wärmepumpen angeboten
    /// und übernommen, die Kälte erzeugen können.
    ///
    /// <para><b>Die Regel.</b> „Kühlfähig“ hat nach Entscheid <b>E15</b> zwei Kriterien: im Katalog
    /// eine Kühlleistung größer null, rechenbar mit einer Kühlkennlinie. Der Kälteimport verlangt
    /// <b>beide</b>: mindestens einen Kühlblock, den <see cref="KuehlblockPruefung"/> annimmt, und
    /// eine Nennkühlleistung größer null aus dem Gerätesatz <c>700</c> (Feld 20). Ein Gerät mit
    /// Nennkühlleistung, aber ohne Kühlblock ist nach dem Katalogkriterium kühlfähig, könnte aber
    /// keine Kälte rechnen — es wird übergangen („keine Kühlkennlinie“).</para>
    ///
    /// <para>Die Prüfreihenfolge legt den Grund fest: zuerst die Kühlkennlinie, dann die Gültigkeit
    /// ihrer Blöcke, zuletzt die Nennkühlleistung.</para>
    /// </summary>
    public static class KuehlfaehigkeitsPruefung
    {
        /// <summary>Der Befund des Satzes <paramref name="index"/> eines gelesenen Imports.</summary>
        public static KaelteimportBefund Befund(WaermepumpenImport import, int index)
        {
            List<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenn;
            List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> roh;
            import.KennlinienRoh(index, out kenn, out roh);
            return Befund(roh, import._list[index].szKuehlleistung);
        }

        /// <summary>
        /// Der Befund aus den Kühlzeilen, wie sie in der Datei stehen, und dem Text der
        /// Nennkühlleistung.
        /// </summary>
        public static KaelteimportBefund Befund(
            IReadOnlyCollection<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehlungRoh,
            string nennkuehlleistung)
        {
            if (kuehlungRoh == null || kuehlungRoh.Count == 0)
                return KaelteimportBefund.KeineKuehlkennlinie;

            List<KuehlblockPruefung.Abgelehnt> abgelehnt;
            var gueltig = KuehlblockPruefung.Pruefen(kuehlungRoh, out abgelehnt);
            if (gueltig.Count == 0)
            {
                if (abgelehnt.All(a => a.Befund == KuehlblockBefund.Heizlage))
                    return KaelteimportBefund.NurHeizlage;
                if (abgelehnt.All(a => a.Befund == KuehlblockBefund.AchsenVertauscht))
                    return KaelteimportBefund.NurAchsenVertauscht;
                return KaelteimportBefund.KeinGueltigerKuehlblock;
            }

            if (!(ZahlText.NachDouble(nennkuehlleistung ?? "") > 0.0))
                return KaelteimportBefund.KeineNennkuehlleistung;

            return KaelteimportBefund.Kuehlfaehig;
        }

        /// <summary>Der Ressourcenschlüssel der Protokollzeile eines übergangenen Satzes.</summary>
        public static string Meldungsschluessel(KaelteimportBefund befund)
        {
            switch (befund)
            {
                case KaelteimportBefund.KeineKuehlkennlinie: return "IMP_KAT_PROT_KAELTE_KEINE_KENNLINIE";
                case KaelteimportBefund.NurHeizlage: return "IMP_KAT_PROT_KAELTE_HEIZLAGE";
                case KaelteimportBefund.NurAchsenVertauscht: return "IMP_KAT_PROT_KAELTE_ACHSEN";
                case KaelteimportBefund.KeinGueltigerKuehlblock: return "IMP_KAT_PROT_KAELTE_UNGUELTIG";
                case KaelteimportBefund.KeineNennkuehlleistung: return "IMP_KAT_PROT_KAELTE_NENNKUEHL";
                default: return "";
            }
        }

        /// <summary>
        /// Wendet den Filter auf einen gelesenen Import an: liefert die Indizes der angebotenen
        /// Sätze und schreibt je übergangenem Satz eine Protokollzeile mit Grund, dazu eine
        /// Bilanzzeile. Die Meldungen des Lesers zu abgelehnten Kühlblöcken bleiben nur für die
        /// angebotenen Sätze stehen — für einen übergangenen nennt die eigene Zeile den Grund.
        /// </summary>
        public static List<int> Filtern(WaermepumpenImport import, List<PruefMeldung> meldungen)
        {
            var angeboten = new List<int>();
            var uebergangen = new List<PruefMeldung>();
            var namenAngeboten = new HashSet<string>();

            for (int i = 0; i < import._list.Count; i++)
            {
                KaelteimportBefund b = Befund(import, i);
                string name = import._list[i].szName ?? "";
                if (b == KaelteimportBefund.Kuehlfaehig)
                {
                    angeboten.Add(i);
                    namenAngeboten.Add(name);
                }
                else
                {
                    uebergangen.Add(new PruefMeldung(PruefStufe.Info, Meldungsschluessel(b), name));
                }
            }

            foreach (PruefMeldung m in import.Meldungen)
            {
                bool kuehlblock = m.Schluessel == "IMP_KAT_PROT_KUEHLBLOCK_HEIZLAGE"
                               || m.Schluessel == "IMP_KAT_PROT_KUEHLBLOCK_ACHSE";
                if (!kuehlblock || (m.Werte.Length > 0 && namenAngeboten.Contains(m.Werte[0])))
                    meldungen.Add(m);
            }

            meldungen.Add(new PruefMeldung(PruefStufe.Info, "IMP_KAT_PROT_KAELTE_BILANZ",
                angeboten.Count.ToString(CultureInfo.InvariantCulture),
                import._list.Count.ToString(CultureInfo.InvariantCulture),
                uebergangen.Count.ToString(CultureInfo.InvariantCulture)));
            meldungen.AddRange(uebergangen);
            return angeboten;
        }
    }
}
