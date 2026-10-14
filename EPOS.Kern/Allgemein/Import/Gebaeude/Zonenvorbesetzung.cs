using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Woher die Besetzung einer Größe einer Zone kommt (Vorgaberegel, Entwurf Gebäudedialog 2.5) — in der Rangfolge der Regel:
    /// Ganglinie der Datei vor Wert der Datei vor Profil vor Gebäudekalender (Erbe). Eine unbeheizte Zone bekommt weder Heiz- noch
    /// Kühlkalender.
    /// </summary>
    internal enum Zonenbesetzung
    {
        /// <summary>Keine eigene Besetzung: Die Zone erbt den Kalender bzw. die Matrix des Gebäudes („geerbt vom Gebäude“).</summary>
        Gebaeude = 0,

        /// <summary>Das Profil der Zone über den Generator (aus der Zuordnung, der Vorgabe oder von Hand).</summary>
        Profil = 1,

        /// <summary>Ein Wert der Datei (Konditionierung der Projektdatei, Zellen der Matrix).</summary>
        Datei = 2,

        /// <summary>Die Ganglinie der Datei als Kalender.</summary>
        Ganglinie = 3,

        /// <summary>Heiz- oder Kühlsollwert einer unbeheizten Zone: keiner, auch kein Erbe.</summary>
        Unbeheizt = 4
    }

    /// <summary>
    /// <b>Die Vorbesetzung einer Zone</b> (Entwurf Gebäudedialog Mehrzonen 2.5, Konzept Nutzungsprofile NP-F16): Eine Zone ohne
    /// zugeordnetes Profil bekommt das Profil ihrer DIN-Nummer (Zuordnung <c>DIN_NUMMER</c> vor der Vorgabe im Code), sonst das
    /// der Gebäudeart (<see cref="KENNUNG_JE_GEBAEUDEART"/>, Vorgabe im Code nach dem Muster der Zuordnung der Pufferauslegung:
    /// ganzer Schlüssel, ohne Unterschied der Schreibung, getrimmt; die Kennung führt auf das EPOS-Muster gleicher Nutzung).
    /// Die Herkunft ist als <see cref="Profilquelle"/> der Art <see cref="VORGABE_DIN"/> bzw. <see cref="VORGABE_GEBAEUDEART"/>
    /// ablesbar; der Anwender wählt sie ab. Die Rangfolge je Größe (Ganglinie vor Datei vor Profil) bleibt
    /// (<see cref="Wirksam"/>). Ohne Datenbank und ohne Oberfläche.
    /// </summary>
    internal static class Zonenvorbesetzung
    {
        /// <summary>Art der Profilquelle: Vorgabe aus der DIN-Nummer der Projektdatei (Schlüssel = Nummer).</summary>
        internal const string VORGABE_DIN = "VORGABE_DIN_NUMMER";

        /// <summary>Art der Profilquelle: Vorgabe aus der Gebäudeart (Schlüssel = Gebäudeart).</summary>
        internal const string VORGABE_GEBAEUDEART = "VORGABE_GEBAEUDEART";

        /// <summary>
        /// Die Vorgabe im Code: Gebäudeart (<c>Tab_Gebaeude.Gebaeudeart</c>) → alte Kennung der Nutzung. Nur Arten, zu denen ein
        /// EPOS-Muster mit Kennwerten passt (Wohnen, Büro, Schule); jede andere Art bekommt keine Vorgabe — die Zone erbt dann
        /// sichtbar vom Gebäude.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string> KENNUNG_JE_GEBAEUDEART =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Einfamilienhaus"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["Mehrfamilienhaus"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["kleines Mehrfamilienhaus"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["grosses Mehrfamilienhaus"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["Reiheneckhaus"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["Reihenmittelhaus"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["Wohnblock"] = DbWerte.KOND_NUTZUNG_WOHNEN,
                ["Schule"] = DbWerte.KOND_NUTZUNG_SCHULE,
                ["Verwaltung"] = DbWerte.KOND_NUTZUNG_BUERO,
                ["Verwaltungsgebäude"] = DbWerte.KOND_NUTZUNG_BUERO,
            };

        /// <summary>Die Kennung der Nutzung einer Gebäudeart (ganzer Schlüssel, getrimmt); <c>null</c> = keine Vorgabe.</summary>
        internal static string KennungAusGebaeudeart(string gebaeudeart)
        {
            string a = gebaeudeart?.Trim();
            return string.IsNullOrEmpty(a) ? null : KENNUNG_JE_GEBAEUDEART.TryGetValue(a, out string k) ? k : null;
        }

        /// <summary>Das Profil der Gebäudeart: das EPOS-Muster gleicher Nutzung (ohne Katalog die Kennung); <c>null</c> = keins.</summary>
        internal static Planprofil AusGebaeudeart(Raumnutzungsvorbelegung vorbelegung, string gebaeudeart)
            => (vorbelegung ?? Raumnutzungsvorbelegung.Vorgabe).AusKennung(KennungAusGebaeudeart(gebaeudeart));

        /// <summary>
        /// <b>Die Vorgabe einer Zone ohne Profil</b>: das Profil der DIN-Nummer (wenn die Zuordnung bzw. ihre Vorgabe eines
        /// trägt), sonst das der Gebäudeart; mit der Quelle. Beides <c>null</c> ohne Vorgabe.
        /// </summary>
        internal static (Planprofil Profil, Profilquelle Quelle) Vorgabe(Raumnutzungsvorbelegung vorbelegung, int? dinNummer, string gebaeudeart)
        {
            Raumnutzungsvorbelegung v = vorbelegung ?? Raumnutzungsvorbelegung.Vorgabe;
            if (dinNummer is int n && v.AusDinNummer(n) is Planprofil din && !din.NichtImKatalog)
                return (din, new Profilquelle(VORGABE_DIN, n.ToString(CultureInfo.InvariantCulture)));
            if (AusGebaeudeart(v, gebaeudeart) is Planprofil art)
                return (art, new Profilquelle(VORGABE_GEBAEUDEART, gebaeudeart.Trim()));
            return (null, null);
        }

        /// <summary>
        /// <b>Setzt die Vorgabe der Gebäudeart in den Plan</b> (Stufe 4 der Regel): jede beheizte Zone mit Räumen, die weder ein
        /// Profil noch eine Quelle trägt, bekommt das Profil der Gebäudeart mit der Quelle <see cref="VORGABE_GEBAEUDEART"/>. Eine
        /// Zone, deren Zuordnung auf „keine“ führt (Quelle gesetzt), bleibt ohne. Der Aufrufer legt die Schritte des Anwenders
        /// danach auf — so bleibt die Vorgabe abwählbar („keine“ von Hand). Gibt die Zahl der vorbesetzten Zonen zurück.
        /// </summary>
        internal static int Anwenden(Zonenplan plan, string gebaeudeart)
        {
            if (plan == null || KennungAusGebaeudeart(gebaeudeart) == null) return 0;
            (Planprofil profil, Profilquelle quelle) = Vorgabe(plan.Vorbelegung, null, gebaeudeart);
            if (profil == null) return 0;
            var beheizt = plan.Bilanz().ToDictionary(z => z.Schluessel, z => z.Beheizt == true && z.Raeume > 0, StringComparer.Ordinal);
            int n = 0;
            foreach (Planzone z in plan.Zonen)
            {
                if (z.Profil != null || z.Quelle != null || !beheizt.TryGetValue(z.Schluessel, out bool warm) || !warm) continue;
                z.Profil = profil;
                z.Quelle = quelle;
                n++;
            }
            return n;
        }

        /// <summary>Ist die Quelle eine Vorgabe der Regel (DIN-Nummer oder Gebäudeart)?</summary>
        internal static bool IstVorgabe(Profilquelle quelle)
            => quelle != null && (quelle.Art == VORGABE_DIN || quelle.Art == VORGABE_GEBAEUDEART);

        // ------------------------------------------------------------------
        //  Rangfolge je Größe
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Die Rangfolge einer Größe</b>: Ganglinie der Datei vor Wert der Datei vor Profil (mit Kennwert) vor dem Erbe des
        /// Gebäudes; Heiz- und Kühlsollwert einer unbeheizten Zone sind <see cref="Zonenbesetzung.Unbeheizt"/>.
        /// </summary>
        internal static Zonenbesetzung Wirksam(Konditionierungsgroesse groesse, bool beheizt, bool ganglinie, bool dateiwert, bool profilwert)
        {
            if (!beheizt && (groesse == Konditionierungsgroesse.Heizsoll || groesse == Konditionierungsgroesse.Kuehlsoll))
                return Zonenbesetzung.Unbeheizt;
            if (ganglinie) return Zonenbesetzung.Ganglinie;
            if (dateiwert) return Zonenbesetzung.Datei;
            if (profilwert) return Zonenbesetzung.Profil;
            return Zonenbesetzung.Gebaeude;
        }

        /// <summary>
        /// <b>Die Besetzung jeder Größe einer Zone</b> aus der Konditionierung der Datei und dem Profil (Kennwerte über den
        /// Generator, <see cref="Raumnutzungsgenerator"/>) — in der Ordnung von <see cref="Konditionierungsgroessen.Alle"/>.
        /// </summary>
        /// <param name="datei">Die Konditionierung der Projektdatei; <c>null</c> = keine.</param>
        /// <param name="profil">Das Profil des Katalogs mit Kennwerten; <c>null</c> = keins.</param>
        /// <param name="beheizt">Ist die Zone beheizt?</param>
        internal static IReadOnlyList<(Konditionierungsgroesse Groesse, Zonenbesetzung Besetzung)> JeGroesse(
            Zonenkonditionierung datei, Raumnutzungsprofil profil, bool beheizt)
        {
            HashSet<Konditionierungsgroesse> ohne = new HashSet<Konditionierungsgroesse>(OhneKennwert(profil));
            var liste = new List<(Konditionierungsgroesse, Zonenbesetzung)>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Groessenkonditionierung d = datei?.Groessen.FirstOrDefault(x => x.Groesse == g);
                bool geliefert = d != null && d.Herkunft != Konditionierungsherkunft.Vorlage;
                liste.Add((g, Wirksam(g, beheizt, geliefert && d.Kalender != null, geliefert && d.Vorgaben.Count > 0,
                                      profil != null && !ohne.Contains(g))));
            }
            return liste;
        }

        /// <summary>
        /// <b>Die Größen, für die ein Profil keinen Kennwert trägt</b> (Lücke 3, NP-F18): Der Generator legt für sie keinen
        /// Kalender an; die Zone erbt diese Größen vom Gebäude. Leer ohne Profil.
        /// </summary>
        internal static IReadOnlyList<Konditionierungsgroesse> OhneKennwert(Raumnutzungsprofil profil)
        {
            if (profil == null) return Array.Empty<Konditionierungsgroesse>();
            return Raumnutzungsgenerator.Erzeugen(profil, null)
                .Where(g => g.Weg == Raumnutzungsweg.Keiner).Select(g => g.Groesse).ToList();
        }
    }
}
