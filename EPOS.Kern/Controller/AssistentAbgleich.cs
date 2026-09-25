using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Gewerke, die der Bearbeiten-Zweig des Assistenten je als „Löschen + Neuanlegen"
    /// schreibt. Die Gebäudeliste fehlt mit Absicht: Sie hat ihren eigenen Abgleich
    /// (<c>WizardCtrl.Schreibe_Projekt_ZuordungGebäude</c>).
    /// </summary>
    public enum AssistentGewerk
    {
        /// <summary>Die Anlagenzeilen (<c>Tab_Energieanlagen</c> ohne Puffer) samt Projektgeräten und Trägersätzen.</summary>
        Erzeuger = 0,

        /// <summary><c>Z_Projekt_Prozesswaerme</c>.</summary>
        Prozess = 1,

        /// <summary><c>Z_ProjektStromganglinie</c>.</summary>
        Stromganglinie = 2,

        /// <summary><c>Z_ProjektWaermebedarf</c> (externer Wärmebedarf).</summary>
        Waermebedarf = 3,

        /// <summary><c>Z_Projekt_Stromverbraucher</c>.</summary>
        Stromverbraucher = 4
    }

    /// <summary>
    /// Der ABGLEICH des Bearbeiten-Zweigs (#490): Welches Gewerk hat der Anwender seit
    /// dem Laden geändert? Nur ein geändertes Gewerk wird gelöscht und neu angelegt; ein
    /// unverändertes behält seine Zeilen, seine Ids und — weil keiner seiner Schreibwege
    /// läuft — das Änderungsdatum des Projekts.
    ///
    /// <para><b>Womit verglichen wird.</b> Mit einem ABDRUCK je Gewerk, den
    /// <see cref="AssistentCtrl"/> nach den Ladewegen und nach jedem gelungenen
    /// Speicherlauf nimmt. Der Abdruck enthält genau das, was der Schreibweg des
    /// Gewerks aus der Liste in die Datenbank trägt — nicht mehr: Was er nicht schreibt,
    /// kann keine Änderung sein, und eine Id, die nach dem Neuanlegen eine andere ist,
    /// darf das nächste Speichern nicht zum Schreiben zwingen.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Zuordnungen</b> — über eine ausdrückliche Spaltenliste: Prozesswärme und
    /// Stromverbraucher (Bezeichner, Summe), Stromganglinie (Bezeichner), Wärmebedarf
    /// (Bezeichner, Kanal). Der Projektverweis (<c>ID_Prozesswaerme</c> …) zählt nicht:
    /// Die Add-Wege leiten ihn beim Schreiben aus dem Bezeichner neu ab
    /// (<c>CopyFromStamm</c>, <c>ApplyGanglinieToProjekt</c>). Die 8 760 Werte einer
    /// Stromganglinie hängen an ihrer Projektkopie, nicht an der Zuordnung — sie
    /// werden vom Schreibweg der Zuordnung nie überschrieben.</item>
    /// <item><b>Erzeuger</b> — über Reflexion, wie der Abdruck der ungespeicherten
    /// Eingaben: <c>Tab_Energieanlagen</c> hat 63 Spalten, das Modell trägt dazu die
    /// Stammfelder der Wärmepumpen-Projektkopie; eine abgeschriebene Liste wäre die
    /// zweite Wahrheit, die beim ersten neuen Feld auseinanderliefe. Ausgenommen sind
    /// allein <c>ID</c> und <c>ID_Projekt</c>. Die Strangliste des PV-Dialogs
    /// (<c>PV_Straenge</c>, keine Wertspalte) geht ausdrücklich mit: <c>null</c> heißt
    /// „nicht angefasst", eine gesetzte Liste ist eine Eingabe.</item>
    /// </list>
    ///
    /// <para><b>Im Zweifel geschrieben.</b> Ohne Abdruck (kein Ladeweg gelaufen) gilt
    /// jedes Gewerk als geändert — dann läuft der Bearbeiten-Zweig wie vor dem
    /// Abgleich.</para>
    /// </summary>
    public static class AssistentAbgleich
    {
        /// <summary>Zahl der Gewerke in <see cref="AssistentGewerk"/>.</summary>
        public const int GEWERKE = 5;

        /// <summary>Der Abdruck EINES Gewerks aus den Listen eines Assistentenlaufs.</summary>
        public static string Abdruck(AssistentCtrl lauf, AssistentGewerk gewerk)
        {
            if (lauf == null) return "";

            StringBuilder sb = new StringBuilder();
            switch (gewerk)
            {
                case AssistentGewerk.Erzeuger:
                    foreach (WErzeugerModel e in lauf.Erzeuger)
                    {
                        // Die Pufferzeilen schreibt der Bearbeiten-Zweig nie (FR-1): Er
                        // nimmt sie vor dem Schreiben aus der Liste, und das Löschen
                        // verschont sie. Sie gehören deshalb nicht in den Vergleich.
                        if (e == null || e.ID_Type == WizardItemClass.PUFFER_TYP) continue;
                        sb.Append('#');
                        Werte(sb, e, ERZEUGER_OHNE);
                        Straenge(sb, e.PV_Straenge);
                    }
                    break;

                case AssistentGewerk.Prozess:
                    foreach (Z_ProjektProzesswaermeModel p in lauf.Prozess)
                        Zeile(sb, p == null ? null : p.szProzessname, p == null ? null : Zahl(p.Summe));
                    break;

                case AssistentGewerk.Stromganglinie:
                    foreach (Z_ProjektStromganglinieModel s in lauf.Stromganglinie)
                        Zeile(sb, s == null ? null : s.m_szStromganglinie, "");
                    break;

                case AssistentGewerk.Waermebedarf:
                    foreach (Z_ProjWaermebedarfModel w in lauf.Waermebedarf)
                        Zeile(sb, w == null ? null : w.m_szBezeichner,
                              w == null ? null : Z_ProjektGebGanglinieCtrl.KanalOderHeizung(w.Kanal));
                    break;

                case AssistentGewerk.Stromverbraucher:
                    foreach (Z_ProjektStromverbraucherModel v in lauf.Stromverbraucher)
                        Zeile(sb, v == null ? null : v.m_szVerbraucher, v == null ? null : Zahl(v.m_Summe));
                    break;
            }
            return sb.ToString();
        }

        /// <summary>Die Abdrücke aller fünf Gewerke, Index = <see cref="AssistentGewerk"/>.</summary>
        public static string[] Abdruecke(AssistentCtrl lauf)
        {
            string[] a = new string[GEWERKE];
            for (int i = 0; i < GEWERKE; i++) a[i] = Abdruck(lauf, (AssistentGewerk)i);
            return a;
        }

        /// <summary>
        /// <b>Die Klimaregion des Kopfes als Stammname</b> — der Schlüssel, mit dem der
        /// Speicherweg sie in das Projekt kopiert (<c>ApplyRegionByNameToProjekt</c>) und
        /// mit dem <see cref="KopfGleichGespeichert"/> vergleicht.
        ///
        /// <para><b>Die Id führt</b> (Hausregel „Beziehungen über Ids"): Die Klappliste
        /// meldet die Stamm-Id (<see cref="ProjektKopfDaten.IdKlimaregion"/>); trifft sie
        /// einen Katalogsatz, gilt dessen Name. Nur ohne gültige Id zählt
        /// <see cref="ProjektKopfDaten.Klimaname"/> — ein Kopf, den ein Aufrufer allein
        /// über den Namen füllt. Beide bezeichnen denselben Satz, der Stammname ist
        /// eindeutig (<c>UX_Tab_Klimaregion_STAMM_Name</c>).</para>
        /// </summary>
        public static string Regionsname(ProjektKopfDaten kopf)
        {
            if (kopf == null) return "";

            if (kopf.IdKlimaregion > 0)
            {
                string ausId = KlimaregionStammCtrl.NameVonId(kopf.IdKlimaregion);
                if (ausId.Length > 0) return ausId;
            }
            return kopf.Klimaname ?? "";
        }

        /// <summary>
        /// Steht der Projektkopf in der Datenbank schon so, wie
        /// <c>WizardCtrl.Update_Projekt</c> ihn schreiben würde? Verglichen werden die
        /// geschriebenen Felder — Name, Bearbeiter, Kunde, Beschreibung und die
        /// Klimaregion über ihren Namen; die Region muss zudem schon eine Kopie DIESES
        /// Projekts sein (sonst legte <c>Update_Projekt</c> sie erst an). Im Zweifel
        /// <c>false</c>, dann wird geschrieben.
        /// </summary>
        public static bool KopfGleichGespeichert(int idProjekt, ProjektKopfDaten kopf)
        {
            if (idProjekt <= 0 || kopf == null) return false;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Projektname, Bearbeiter, Kunde, Beschreibung, ID_Klimaregion " +
                    "FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("@id", idProjekt));
                if (dt == null || dt.Rows.Count != 1) return false;
                DataRow r = dt.Rows[0];

                if (!Gleich(r["Projektname"], kopf.Name)) return false;
                if (!Gleich(r["Bearbeiter"], kopf.Bearbeiter)) return false;
                if (!Gleich(r["Kunde"], kopf.Kunde)) return false;
                if (!Gleich(r["Beschreibung"], kopf.Beschreibung)) return false;

                if (r["ID_Klimaregion"] == DBNull.Value) return false;
                int idRegion = Convert.ToInt32(r["ID_Klimaregion"], CultureInfo.InvariantCulture);

                object eigen = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Klimaregion WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@r", idRegion), new DbParam("@p", idProjekt));
                if (eigen == null || eigen == DBNull.Value ||
                    Convert.ToInt32(eigen, CultureInfo.InvariantCulture) == 0) return false;

                string name = KlimaregionStammCtrl.NameZuProjektregion(idRegion, idProjekt);
                return string.Equals(name ?? "", Regionsname(kopf), StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =============================================================================
        // Bausteine
        // =============================================================================

        /// <summary>Was an einer Anlagenzeile KEINE Eingabe ist: die zwei Schlüsselspalten.</summary>
        private static readonly HashSet<string> ERZEUGER_OHNE =
            new HashSet<string>(StringComparer.Ordinal) { "ID", "ID_Projekt" };

        private static bool Gleich(object db, string wert)
        {
            string s = (db == null || db == DBNull.Value) ? "" : Convert.ToString(db, CultureInfo.InvariantCulture);
            return string.Equals(s, wert ?? "", StringComparison.Ordinal);
        }

        private static string Zahl(double wert)
        {
            return wert.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void Zeile(StringBuilder sb, string bezeichner, string wert)
        {
            if (bezeichner == null && wert == null) { sb.Append("#-"); return; }
            sb.Append('#').Append((bezeichner ?? "").Length).Append(':').Append(bezeichner ?? "")
              .Append('=').Append(wert ?? "");
        }

        private static void Straenge(StringBuilder sb, List<AnlageStrangModel> straenge)
        {
            if (straenge == null) { sb.Append("|S-"); return; }
            sb.Append("|S").Append(straenge.Count);
            foreach (AnlageStrangModel s in straenge)
            {
                sb.Append(';');
                if (s == null) { sb.Append('-'); continue; }
                // Id und Anlagenverweis des Strangs vergibt der Schreibweg neu.
                Werte(sb, s, STRANG_OHNE);
            }
        }

        private static readonly HashSet<string> STRANG_OHNE =
            new HashSet<string>(StringComparer.Ordinal) { "ID", "ID_Anlage" };

        /// <summary>Hängt die Wertfelder und -eigenschaften eines Modells an, nach Namen geordnet.</summary>
        private static void Werte(StringBuilder sb, object modell, HashSet<string> ohne)
        {
            foreach (KeyValuePair<string, Func<object, object>> leser in Leser(modell.GetType()))
            {
                if (ohne.Contains(leser.Key)) continue;
                object wert;
                try { wert = leser.Value(modell); } catch (Exception) { wert = null; }
                sb.Append(leser.Key).Append('=')
                  .Append(Convert.ToString(wert, CultureInfo.InvariantCulture)).Append('|');
            }
        }

        private static readonly Dictionary<Type, List<KeyValuePair<string, Func<object, object>>>> _leser =
            new Dictionary<Type, List<KeyValuePair<string, Func<object, object>>>>();

        private static List<KeyValuePair<string, Func<object, object>>> Leser(Type typ)
        {
            lock (_leser)
            {
                List<KeyValuePair<string, Func<object, object>>> fertig;
                if (_leser.TryGetValue(typ, out fertig)) return fertig;

                var namen = new SortedDictionary<string, Func<object, object>>(StringComparer.Ordinal);

                foreach (FieldInfo f in typ.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (IstWert(f.FieldType) && !namen.ContainsKey(f.Name))
                        namen[f.Name] = o => f.GetValue(o);

                foreach (PropertyInfo p in typ.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (p.CanRead && p.GetIndexParameters().Length == 0 && IstWert(p.PropertyType) &&
                        !namen.ContainsKey(p.Name))
                        namen[p.Name] = o => p.GetValue(o);

                fertig = new List<KeyValuePair<string, Func<object, object>>>(namen);
                _leser[typ] = fertig;
                return fertig;
            }
        }

        private static bool IstWert(Type typ)
        {
            Type t = Nullable.GetUnderlyingType(typ) ?? typ;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal)
                || t == typeof(DateTime) || t == typeof(TimeSpan) || t == typeof(Guid);
        }
    }
}
