using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Musterschlüssel mit Parameter, den der Katalog nicht aufzählt, sondern beim Nachschlagen bildet (Konzept
    /// Berichtsvorlagen 4.5): das Muster, wie Katalogansicht und Baukasten es zeigen, die Ressource seiner Beschreibung
    /// und ein Beispiel, das sich auflöst.
    /// </summary>
    public sealed class Vorlagenfeldmuster
    {
        internal Vorlagenfeldmuster(string muster, string beschreibungId, string beispiel)
        {
            Muster = muster;
            BeschreibungId = beschreibungId;
            Beispiel = beispiel;
        }

        /// <summary>Das Muster, etwa <c>stand.&lt;n&gt;.&lt;schluessel&gt;</c>.</summary>
        public string Muster { get; }

        /// <summary>Die Ressource der Beschreibung (<c>VF_MUSTER_*_POSITIONEN</c>).</summary>
        public string BeschreibungId { get; }

        /// <summary>Ein Schlüssel nach dem Muster, den <see cref="Vorlagenfeldkatalog.Finde"/> auflöst.</summary>
        public string Beispiel { get; }

        /// <summary>Die Beschreibung in der gewählten Sprache.</summary>
        public string Beschreibung(bool englisch)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            try { return R.ResourceManager.GetString(BeschreibungId, kultur) ?? BeschreibungId; }
            catch (Exception) { return BeschreibungId; }
        }

        /// <inheritdoc/>
        public override string ToString() { return Muster; }
    }

    /// <summary>
    /// <b>Katalog v7: Positionsadressierung</b> (Etappe BV-E9, Entscheidfrage BV-Q10 Lesart b; Konzept 4.5, 4.7) — ein
    /// Standwert außerhalb der Blöcke nach der POSITION seines Stands:
    /// <list type="bullet">
    /// <item><c>stand.&lt;n&gt;.&lt;rest&gt;</c> ist <c>stand.&lt;rest&gt;</c> des n-ten Stands in der Folge von
    /// <see cref="Berichtswerte.Staende"/> — derselben Folge, in der <c>{{#je stand}}</c> wiederholt: das Stammprojekt
    /// vorn (Position 1), dann die Varianten in der Reihenfolge des Baums;</item>
    /// <item><c>variante.&lt;n&gt;.&lt;rest&gt;</c> ist derselbe Wert der n-ten Variante (<see cref="Berichtswerte.Varianten"/>,
    /// die Folge von <c>{{#je variante}}</c>) — die Adresse, die nicht davon abhängt, ob der Baum einen Stamm trägt.</item>
    /// </list>
    /// <b>Keine Position 0 und kein <c>stand.stamm</c>:</b> Der Stamm hat seine eigenen Schlüssel (<c>stamm.*</c>,
    /// <c>projekt.*</c>) und steht als <c>stand.1</c> an derselben Stelle wie im ersten Durchlauf von <c>{{#je stand}}</c>;
    /// eine dritte Schreibweise für ihn wäre ein zweiter Name für dieselbe Zahl.
    ///
    /// <para><b>Musterschlüssel statt Aufzählung</b> (Konzept 4.5): Die Einträge stehen nicht in <see cref="Alle"/> — je
    /// Position ein Zwilling jedes Standwerts wären Tausende. <see cref="Finde"/> bildet den Eintrag beim Nachschlagen aus
    /// seinem Vorbild im Kontext Stand (Art, Format, Einheit, Leerwert, Ausgaben, Bedarf wie dieses), Kontext Gruppe — gültig
    /// überall im Bericht — und merkt ihn sich. Fehlt die Position im Lauf, bleibt die Stelle leer mit Grund („Stand 3
    /// nicht gewählt“); der Prüfer gibt einen Hinweis, wenn die Vorlage mehr Positionen nutzt, als der Lauf hat. Katalogansicht
    /// und Baukasten zeigen die zwei Muster (<see cref="Positionsmuster"/>).</para>
    /// </summary>
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Fassung der Positionsadressierung (Etappe BV-E9).</summary>
        internal const int FASSUNG_POSITION = 7;

        /// <summary>Musterschlüssel eines Stands nach Position.</summary>
        public const string MUSTER_STAND_POSITION = "stand.<n>.<schluessel>";

        /// <summary>Musterschlüssel einer Variante nach Position.</summary>
        public const string MUSTER_VARIANTE_POSITION = "variante.<n>.<schluessel>";

        /// <summary>Vorsilbe der Varianten nach Position.</summary>
        private const string VARIANTE = "variante.";

        /// <summary>Die höchste Position, die der Katalog auflöst — eine Grenze gegen Unsinn, keine fachliche.</summary>
        public const int POSITION_MAX = 999;

        /// <summary>Die zwei Muster der Positionsadressierung, wie Katalogansicht und Baukasten sie zeigen.</summary>
        public static readonly IReadOnlyList<Vorlagenfeldmuster> Positionsmuster = new[]
        {
            new Vorlagenfeldmuster(MUSTER_STAND_POSITION, nameof(R.VF_MUSTER_STAND_POSITIONEN), "stand.2.anzeige"),
            new Vorlagenfeldmuster(MUSTER_VARIANTE_POSITION, nameof(R.VF_MUSTER_VARIANTE_POSITIONEN), "variante.1.kennzahl.eff.jaz"),
        };

        /// <summary>Die gebildeten Einträge des Hauptkatalogs je Schlüssel (einmal gebildet, von jedem Lauf geteilt).</summary>
        private static readonly ConcurrentDictionary<string, Vorlagenfeld> _positionen =
            new ConcurrentDictionary<string, Vorlagenfeld>(StringComparer.Ordinal);

        /// <summary>
        /// Zerlegt einen normierten Schlüssel nach den Mustern: <c>stand.&lt;n&gt;.&lt;rest&gt;</c> bzw.
        /// <c>variante.&lt;n&gt;.&lt;rest&gt;</c> mit n von 1 bis <see cref="POSITION_MAX"/> ohne führende Null.
        /// </summary>
        public static bool IstPositionsschluessel(string schluessel, out bool variante, out int position, out string rest)
        {
            variante = false;
            position = 0;
            rest = null;
            if (string.IsNullOrEmpty(schluessel)) return false;
            string vorsilbe = schluessel.StartsWith(STAND, StringComparison.Ordinal) ? STAND
                            : schluessel.StartsWith(VARIANTE, StringComparison.Ordinal) ? VARIANTE : null;
            if (vorsilbe == null) return false;
            int punkt = schluessel.IndexOf('.', vorsilbe.Length);
            if (punkt <= vorsilbe.Length || punkt == schluessel.Length - 1) return false;
            string zahl = schluessel.Substring(vorsilbe.Length, punkt - vorsilbe.Length);
            if (zahl[0] == '0' || zahl.Length > 3) return false;
            foreach (char c in zahl) if (c < '0' || c > '9') return false;
            position = int.Parse(zahl, CultureInfo.InvariantCulture);
            if (position < 1 || position > POSITION_MAX) return false;
            variante = vorsilbe == VARIANTE;
            rest = schluessel.Substring(punkt + 1);
            return true;
        }

        /// <summary>
        /// Der Eintrag zu einem Positionsschlüssel oder <c>null</c> (kein Positionsschlüssel, Fassung vor
        /// <see cref="FASSUNG_POSITION"/>, kein Vorbild im Kontext Stand). <paramref name="finde"/> schlägt das Vorbild
        /// <c>stand.&lt;rest&gt;</c> nach (auch über einen Alias); der gebildete Schlüssel nennt das Vorbild beim Namen.
        /// </summary>
        internal static Vorlagenfeld Positionsfeld(string normiert, Func<string, Vorlagenfeld> finde, int fassung,
                                                   ConcurrentDictionary<string, Vorlagenfeld> gebildet)
        {
            if (fassung < FASSUNG_POSITION || finde == null) return null;
            if (!IstPositionsschluessel(normiert, out bool variante, out int position, out string rest)) return null;
            Vorlagenfeld vorbild = finde(STAND + rest);
            if (vorbild == null || vorbild.Kontext != Vorlagenfeldkontext.Stand ||
                !vorbild.Schluessel.StartsWith(STAND, StringComparison.Ordinal)) return null;
            string schluessel = (variante ? VARIANTE : STAND) + position.ToString(CultureInfo.InvariantCulture) + "."
                              + vorbild.Schluessel.Substring(STAND.Length);
            return gebildet.GetOrAdd(schluessel, s => BildePosition(s, vorbild, position, variante));
        }

        /// <summary>Der Eintrag des Hauptkatalogs zu einem Positionsschlüssel (<see cref="Finde"/>).</summary>
        private static Vorlagenfeld Positionsfeld(string normiert)
        {
            return Positionsfeld(normiert, s => _index.TryGetValue(s, out Vorlagenfeld f) ? f : null, KATALOGFASSUNG, _positionen);
        }

        private static Vorlagenfeld BildePosition(string schluessel, Vorlagenfeld vorbild, int position, bool variante)
        {
            string nummer = position.ToString(CultureInfo.InvariantCulture);
            return new Vorlagenfeld(schluessel, vorbild.Art, Vorlagenfeldkontext.Gruppe, w => AnPosition(w, vorbild, position, variante))
            {
                Seit = FASSUNG_POSITION,
                Format = vorbild.Format,
                Einheit = vorbild.Einheit,
                Leerwert = vorbild.Leerwert,
                Ausgaben = vorbild.Ausgaben,
                Bedarf = vorbild.Bedarf,
                Ableitung = new Vorlagenfeldableitung(variante ? MUSTER_VARIANTE_POSITION : MUSTER_STAND_POSITION,
                    variante ? nameof(R.VF_MUSTER_VARIANTE_POSITION) : nameof(R.VF_MUSTER_STAND_POSITION), vorbild.Schluessel)
                {
                    Bezeichnung = k => Beschreibung(vorbild, k.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase)),
                    Zusatz = k => nummer,
                },
            };
        }

        /// <summary>
        /// Der Wert des Vorbilds am Stand der Position: <see cref="Berichtswerte.Staende"/> bzw.
        /// <see cref="Berichtswerte.Varianten"/>, ab 1 gezählt; fehlt die Position, der Grund „Stand n nicht gewählt“.
        /// </summary>
        private static object AnPosition(Berichtswerte w, Vorlagenfeld vorbild, int position, bool variante)
        {
            IReadOnlyList<VariantenDaten> folge = variante ? w.Varianten : w.Staende;
            if (position > folge.Count)
                return new Leergrund(string.Format(w.Kultur,
                    w.Text(variante ? nameof(R.BV_GRUND_VARIANTE_NICHT_GEWAEHLT) : nameof(R.BV_GRUND_STAND_NICHT_GEWAEHLT)), position));
            return vorbild.Quelle(w.MitStand(folge[position - 1]));
        }
    }
}
