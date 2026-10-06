using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Profilverweis einer Planzone</b> (Konzept Nutzungsprofile NP-F23): ein Profil des Katalogs mit Id und Name;
    /// ohne Katalog (reine Kernwege ohne Datenbank) eine der alten Kennungen als Name ohne Id; ein Text, zu dem kein Profil
    /// passt, bleibt als Name ohne Id mit dem Befund <see cref="NichtImKatalog"/>.
    /// </summary>
    /// <param name="Id">Die Id des Profils (<c>Tab_Raumnutzungsprofil.ID</c>) oder <c>null</c>.</param>
    /// <param name="Name">Der Name: der Profilname, die alte Kennung oder der Text der Datei.</param>
    /// <param name="NichtImKatalog">Kein Profil passt zum Namen; der Text bleibt (Befund <see cref="BEFUND_NICHT_IM_KATALOG"/>).</param>
    internal sealed record Planprofil(long? Id, string Name, bool NichtImKatalog = false)
    {
        /// <summary>Die Kennung des Befunds „(nicht im Katalog)“ — der Anzeigetext gehört der Oberfläche.</summary>
        internal const string BEFUND_NICHT_IM_KATALOG = "NUTZUNG_NICHT_IM_KATALOG";

        /// <summary>Der Vorsatz des Auswahlschlüssels eines Katalogprofils.</summary>
        internal const string SCHLUESSEL_PRAEFIX = "#";

        /// <summary>Der Schlüssel der Auswahl: <c>#&lt;Id&gt;</c> für ein Profil des Katalogs, sonst der Name.</summary>
        internal string Schluessel => Id is long id ? SCHLUESSEL_PRAEFIX + id.ToString(CultureInfo.InvariantCulture) : Name;

        public override string ToString() => Name ?? "";
    }

    /// <summary>
    /// <b>Woher das Profil einer Planzone kommt</b> (Konzept Nutzungsprofile 6.2, Herleitungszeile des Zonenbaums): eine Art der
    /// Zuordnung (<see cref="RaumnutzungSchema.ZUORDNUNG_DIN"/>, <see cref="RaumnutzungSchema.ZUORDNUNG_IFC"/>,
    /// <see cref="RaumnutzungSchema.ZUORDNUNG_HOTTCAD"/>) mit ihrem Schlüssel (DIN-Nummer, Nutzungsklasse, Raumtyp), die Nutzung
    /// der Datei (<see cref="DATEI"/>, <c>EPOS_Zone.Nutzung</c>), der gespeicherte Plan (<see cref="GESPEICHERT"/>) oder die
    /// Wahl von Hand (<see cref="HAND"/>). Sprachneutral — den Text bildet die Hülle.
    /// </summary>
    /// <param name="Art">Die Art der Quelle.</param>
    /// <param name="Schluessel">Der Schlüssel der Zuordnung; <c>null</c> für Datei, gespeicherten Plan und Hand.</param>
    internal sealed record Profilquelle(string Art, string Schluessel = null)
    {
        /// <summary>Die Nutzung der Datei selbst (<c>EPOS_Zone.Nutzung</c> einer IFC-Datei von EPOS-Plan).</summary>
        internal const string DATEI = "DATEI";

        /// <summary>Die Nutzung des gespeicherten Plans eines früheren Imports derselben Datei.</summary>
        internal const string GESPEICHERT = "GESPEICHERT";

        /// <summary>Von Hand gewählt (Zonenbaum: Klappliste der Zone, „Zone hinzufügen“ mit Nutzung).</summary>
        internal const string HAND = "HAND";

        /// <summary>Von Hand gewählt.</summary>
        internal static readonly Profilquelle Hand = new Profilquelle(HAND);
    }

    /// <summary>
    /// <b>Die Vorbelegung der Nutzung beim Import</b> (Konzept Nutzungsprofile NP-F12, 4.4, 5.4): ein Leser der Zuordnung
    /// <c>Tab_Raumnutzungszuordnung</c> (Art <c>DIN_NUMMER</c>, <c>IFC_KLASSE</c>, <c>HOTTCAD_RAUMTYP</c>) mit der Vorgabe im
    /// Code als Rückfall: Trägt die Zuordnung eine Zeile für (Art, Schlüssel), gilt sie — auch „keine“ (<c>ID_Profil</c>
    /// leer); fehlt die Zeile, gilt die Vorgabe (<see cref="Din18599Nutzung"/>, <see cref="Zonenplan.NutzungAusKlasse"/>) und
    /// deren Kennung führt auf das EPOS-Muster gleicher Nutzung. Eine Zeile der Art <c>HOTTCAD_RAUMTYP</c> geht der
    /// Nutzungsklasse des Raums vor (<see cref="GebaeudeZonierung.Nutzungsklasse"/>). Ohne Datenbank oder Katalog
    /// (<see cref="Vorgabe"/>) bleibt die Vorgabe im Code wirksam und liefert die alten Kennungen ohne Id.
    ///
    /// <para>Ein Stand liest Zuordnung, Kategorien und Profile einmal (<see cref="Lesen"/>); der Aufrufer hält ihn für die
    /// Dauer eines Schritts.</para>
    /// </summary>
    internal sealed class Raumnutzungsvorbelegung
    {
        /// <summary>Der Stand ohne Katalog: nur die Vorgabe im Code.</summary>
        internal static readonly Raumnutzungsvorbelegung Vorgabe = new Raumnutzungsvorbelegung(null, null, null);

        /// <summary>Die alte Kennung → Name des EPOS-Musters gleicher Nutzung.</summary>
        private static readonly IReadOnlyDictionary<string, string> MUSTER_JE_KENNUNG = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DbWerte.KOND_NUTZUNG_WOHNEN] = RaumnutzungSaat.WOHNEN,
            [DbWerte.KOND_NUTZUNG_BUERO] = RaumnutzungSaat.BUERO,
            [DbWerte.KOND_NUTZUNG_SCHULE] = RaumnutzungSaat.SCHULE,
            [DbWerte.KOND_NUTZUNG_SONSTIGE] = RaumnutzungSaat.SONSTIGE,
        };

        private readonly List<(long Id, string Name, string Art, string Kategorie, Raumnutzungsprofil Profil)> _profile;
        private readonly Dictionary<string, long?> _zuordnung;

        private Raumnutzungsvorbelegung(IReadOnlyList<RaumnutzungCtrl.Kategorie> kategorien, IReadOnlyList<Raumnutzungsprofil> profile,
                                        IReadOnlyList<RaumnutzungCtrl.Zuordnung> zuordnungen)
        {
            if (kategorien == null || profile == null || zuordnungen == null) return;
            // Profile in der Ordnung der Kategorien (Reihenfolge, Name), darin nach Nummer und Name.
            var rang = new Dictionary<long, (int Rang, string Art, string Name)>();
            for (int i = 0; i < kategorien.Count; i++) rang[kategorien[i].Id] = (i, kategorien[i].Art, kategorien[i].Bezeichner);
            _profile = profile.Where(p => rang.ContainsKey(p.IdKatalog))
                              .Select((p, i) => (P: p, I: i)).OrderBy(x => rang[x.P.IdKatalog].Rang).ThenBy(x => x.I)
                              .Select(x => (x.P.Id, x.P.Bezeichner, rang[x.P.IdKatalog].Art, rang[x.P.IdKatalog].Name, x.P)).ToList();
            _zuordnung = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
            foreach (RaumnutzungCtrl.Zuordnung z in zuordnungen)
            {
                string k = Schluessel(z.Art, z.Schluessel);
                if (k != null && !_zuordnung.ContainsKey(k)) _zuordnung[k] = z.IdProfil;
            }
        }

        /// <summary>Liegt ein Katalog vor (Datenbank mit den Tabellen der Nutzungsprofile)?</summary>
        internal bool MitKatalog => _profile != null;

        /// <summary>
        /// Der Stand aus der Datenbank: Zuordnung, Kategorien und Profile; ohne Datenbank oder Katalog — und bei jedem
        /// Lesefehler — <see cref="Vorgabe"/>.
        /// </summary>
        internal static Raumnutzungsvorbelegung Lesen()
        {
            try
            {
                if (!DataRepository.DatenbankVorhanden() || !RaumnutzungCtrl.Lesbar()) return Vorgabe;
                var ctrl = new RaumnutzungCtrl();
                return new Raumnutzungsvorbelegung(ctrl.Kategorien(), ctrl.Profile(), ctrl.Zuordnungen());
            }
            catch (Exception)
            {
                return Vorgabe;
            }
        }

        // ------------------------------------------------------------------
        //  Zuordnung mit Vorgabe
        // ------------------------------------------------------------------

        /// <summary>Das Profil einer DIN-V-18599-10-Nummer: Zeile <c>DIN_NUMMER</c>, sonst <see cref="Din18599Nutzung"/>.</summary>
        internal Planprofil AusDinNummer(int? nummer)
            => nummer is int n ? Aufloesen(RaumnutzungSchema.ZUORDNUNG_DIN, n.ToString(CultureInfo.InvariantCulture)) : null;

        /// <summary>Das Profil einer Nutzungsklasse (Z6): Zeile <c>IFC_KLASSE</c>, sonst <see cref="Zonenplan.NutzungAusKlasse"/>.</summary>
        internal Planprofil AusKlasse(string klasse) => Aufloesen(RaumnutzungSchema.ZUORDNUNG_IFC, klasse);

        /// <summary>
        /// Der Zuordnungsschlüssel eines Raums: (<c>HOTTCAD_RAUMTYP</c>, Raumtyp), wenn die Zuordnung eine Zeile für den
        /// Raumtyp der Datei trägt (wie geschrieben oder ohne Vorsatz <c>mrt</c>), sonst (<c>IFC_KLASSE</c>, Nutzungsklasse).
        /// </summary>
        internal (string Art, string Schluessel) Herleitung(AbbildRaum r)
        {
            string typ = (r?.Raumtyp ?? "").Trim();
            if (_zuordnung != null && typ.Length > 0)
            {
                if (_zuordnung.ContainsKey(Schluessel(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, typ))) return (RaumnutzungSchema.ZUORDNUNG_HOTTCAD, typ);
                if (typ.Length > 3 && typ.StartsWith("mrt", StringComparison.Ordinal)
                    && _zuordnung.ContainsKey(Schluessel(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, typ.Substring(3))))
                    return (RaumnutzungSchema.ZUORDNUNG_HOTTCAD, typ.Substring(3));
            }
            return (RaumnutzungSchema.ZUORDNUNG_IFC, GebaeudeZonierung.Nutzungsklasse(r));
        }

        /// <summary>
        /// <b>Löst (Art, Schlüssel) auf</b>: die Zeile der Zuordnung, wenn es eine gibt (<c>null</c> bei „keine“), sonst die
        /// Vorgabe im Code — über deren Kennung das EPOS-Muster gleicher Nutzung (<see cref="AusKennung"/>).
        /// <c>HOTTCAD_RAUMTYP</c> hat keine Vorgabe.
        /// </summary>
        internal Planprofil Aufloesen(string art, string schluessel)
        {
            string s = schluessel?.Trim();
            if (string.IsNullOrEmpty(art) || string.IsNullOrEmpty(s)) return null;
            if (_zuordnung != null && _zuordnung.TryGetValue(Schluessel(art, s), out long? id))
                return id is long i && _profile.FirstOrDefault(p => p.Id == i) is { Name: not null } p ? new Planprofil(p.Id, p.Name) : null;
            switch (art)
            {
                case RaumnutzungSchema.ZUORDNUNG_DIN:
                    return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? AusKennung(Din18599Nutzung.Nutzung(n)) : null;
                case RaumnutzungSchema.ZUORDNUNG_IFC:
                    return AusKennung(Zonenplan.NutzungAusKlasse(s));
                default:
                    return null;
            }
        }

        /// <summary>
        /// Eine alte Kennung (<see cref="DbWerte.KOND_NUTZUNGEN"/>) → das EPOS-Muster gleicher Nutzung; ohne Katalog oder
        /// ohne dieses Muster die Kennung als Name ohne Id. <c>null</c> für <c>null</c>.
        /// </summary>
        internal Planprofil AusKennung(string kennung)
        {
            if (string.IsNullOrEmpty(kennung)) return null;
            if (_profile != null && MUSTER_JE_KENNUNG.TryGetValue(kennung, out string muster)
                && _profile.FirstOrDefault(p => p.Art == RaumnutzungSchema.ART_EPOS_MUSTER
                                                && string.Equals(p.Name, muster, StringComparison.OrdinalIgnoreCase)) is { Name: not null } p)
                return new Planprofil(p.Id, p.Name);
            return new Planprofil(null, kennung);
        }

        // ------------------------------------------------------------------
        //  Text → Profil
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Ein Text als Profil</b> (NP-F23; <c>EPOS_Zone.Nutzung</c> einer IFC-Datei, die gespeicherte Nutzung einer Zone):
        /// ein Auswahlschlüssel <c>#&lt;Id&gt;</c> oder der Name eines Profils (ganzer Vergleich ohne Unterschied der
        /// Schreibung; bei gleichem Namen in mehreren Kategorien das erste in der Ordnung der Kategorien) → dieses Profil;
        /// eine alte Kennung → das EPOS-Muster gleicher Nutzung; sonst bleibt der Text mit dem Befund „nicht im Katalog“.
        /// <c>null</c> für leer.
        /// </summary>
        internal Planprofil AusText(string text)
        {
            string t = KonditionierungNutzungSchema.Nutzungstext(text);
            if (t == null) return null;
            if (_profile != null)
            {
                if (t.StartsWith(Planprofil.SCHLUESSEL_PRAEFIX, StringComparison.Ordinal)
                    && long.TryParse(t.Substring(Planprofil.SCHLUESSEL_PRAEFIX.Length), NumberStyles.None, CultureInfo.InvariantCulture, out long id)
                    && _profile.FirstOrDefault(p => p.Id == id) is { Name: not null } nachId)
                    return new Planprofil(nachId.Id, nachId.Name);
                if (_profile.FirstOrDefault(p => string.Equals(p.Name?.Trim(), t, StringComparison.OrdinalIgnoreCase)) is { Name: not null } nachName)
                    return new Planprofil(nachName.Id, nachName.Name);
            }
            if (MUSTER_JE_KENNUNG.ContainsKey(t)) return AusKennung(t);
            return new Planprofil(null, t, NichtImKatalog: true);
        }

        /// <summary>
        /// <b>Die Wahl von Hand</b> (Zonenbaum): wie <see cref="AusText"/>, aber nur, was der Katalog kennt — ohne Katalog
        /// eine der Kennungen aus <see cref="Zonenplan.NUTZUNGEN"/>; ein Text, der schon an der Zone steht
        /// (<paramref name="bisher"/>), bleibt wählbar. <c>false</c> = nicht wählbar; <c>null</c> = keine.
        /// </summary>
        internal bool Wahl(string text, Planprofil bisher, out Planprofil profil)
        {
            profil = AusText(text);
            if (profil == null) return true;
            if (bisher != null && string.Equals(profil.Name, bisher.Name, StringComparison.Ordinal) && profil.Id == bisher.Id) return true;
            if (profil.NichtImKatalog) return false;
            return MitKatalog ? profil.Id.HasValue || Zonenplan.NUTZUNGEN.Contains(profil.Name) : Zonenplan.NUTZUNGEN.Contains(profil.Name);
        }

        /// <summary>
        /// Die Profile der Auswahl: <c>(Schlüssel, Name, Art der Kategorie, Name der Kategorie)</c> in der Ordnung der
        /// Kategorien; ohne Katalog die Kennungen aus <see cref="Zonenplan.NUTZUNGEN"/> (Art und Kategorie leer).
        /// </summary>
        internal IReadOnlyList<(string Schluessel, string Name, string Art, string Kategorie)> Auswahl()
            => _profile == null
                ? Zonenplan.NUTZUNGEN.Select(k => (k, k, (string)null, (string)null)).ToList()
                : _profile.Select(p => (new Planprofil(p.Id, p.Name).Schluessel, p.Name, p.Art, p.Kategorie)).ToList();

        /// <summary>Der Name der Kategorie eines Profils des Katalogs; <c>null</c> ohne Katalog oder für ein unbekanntes Profil.</summary>
        internal string Kategorie(long? id)
            => id is long i && _profile?.FirstOrDefault(p => p.Id == i) is { Name: not null } p ? p.Kategorie : null;

        /// <summary>Das Profil des Katalogs mit seinen Kennwerten; <c>null</c> ohne Katalog oder für ein unbekanntes Profil.</summary>
        internal Raumnutzungsprofil Profil(long? id)
            => id is long i && _profile?.FirstOrDefault(p => p.Id == i) is { Name: not null } p ? p.Profil : null;

        private static string Schluessel(string art, string schluessel)
        {
            string s = schluessel?.Trim();
            return string.IsNullOrEmpty(art) || string.IsNullOrEmpty(s) ? null : art + "\u001F" + s;
        }
    }
}
