using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Abbildung Zeile ↔ Kern der Zonen</b> (Stufe G3; Softwarearchitektur 2.9,
    /// Mehrzonenkonzept 4.1–4.3) — die EINE Stelle, an der eine Zeile aus <c>Tab_Zone</c>,
    /// <c>Tab_Bauteil</c> und <c>Tab_Bauteilaufbau</c>/<c>Tab_Bauteilschicht</c> zum Kern-Datensatz
    /// <see cref="GebaeudeZonensatz"/>/<see cref="BauteilEingang"/> wird, und zurück. Ohne
    /// Datenbank, ohne Zustand: Gelesen wird in <see cref="GebaeudeZonenanschluss"/>,
    /// geschrieben über <c>GebaeudeZonenCtrl.SpeichernJeGebaeude</c>.
    ///
    /// <para><b>Zeile → Kern</b> (<see cref="AlsZonensatz"/>): die Persistenzwerte
    /// <c>DbWerte.BAUTEILART_*</c> und <c>DbWerte.RANDBEDINGUNG_*</c> auf die Kern-Aufzählungen
    /// <see cref="Bauteilart"/> und <see cref="Bauteilrand"/>, ein unbekannter Wert ist ein
    /// benannter Fehler. <b>NULL</b> heißt NaN, und NaN trägt die Regel von
    /// <see cref="BauteilEingang"/>: U-Wert NaN = aus den Schichten, Neigung NaN = nach Art,
    /// Azimut NaN = keiner, g-Wert, Rahmenanteil und Verschattung NaN = Wert des Gebäudes (der
    /// seinerseits die Vorgabe trägt). <c>Psi_L</c> NULL heißt 0 (keine Wärmebrücke). Die
    /// Randbedingung folgt der Regel <see cref="RandAusZeile(string, string)"/>. Die Schichten
    /// kommen aus dem Aufbau des Bauteils mit der Wertekopie λ/ρ/c_p der Schicht
    /// (<see cref="AlsSchicht"/>).</para>
    ///
    /// <para><b>Kern → Zeile</b> (<see cref="AlsZoneModel"/>): für den Knopf „Gebäude als eine
    /// Zone übernehmen" — aus <see cref="GebaeudeZonenuebernahme.AlsEineZone"/> neue Zeilen mit
    /// <b>negativen vorläufigen Ids</b> (Muster A6) und Herkunft
    /// <see cref="DbWerte.HERKUNFT_VORGABE"/>. Der Rundlauf Kern → Zeile → Kern ist verlustfrei;
    /// was eine Zeile nicht tragen kann (ein Schichtaufbau ohne Aufbau, ein eigenes α_kon,
    /// „innerhalb der Zone" an einer Art außer Innenwand und Decke), wird benannt abgelehnt.</para>
    /// </summary>
    internal static class GebaeudeZonenabbildung
    {
        /// <summary>Die neun Bauteilarten: Persistenzwert ↔ Kern-Aufzählung, in Schemareihenfolge.</summary>
        private static readonly (string Wert, Bauteilart Art)[] Arten =
        {
            (DbWerte.BAUTEILART_AUSSENWAND, Bauteilart.Aussenwand),
            (DbWerte.BAUTEILART_DACH, Bauteilart.Dach),
            (DbWerte.BAUTEILART_BODENPLATTE, Bauteilart.Bodenplatte),
            (DbWerte.BAUTEILART_FENSTER, Bauteilart.Fenster),
            (DbWerte.BAUTEILART_TUER, Bauteilart.Tuer),
            (DbWerte.BAUTEILART_INNENWAND, Bauteilart.Innenwand),
            (DbWerte.BAUTEILART_DECKE, Bauteilart.Decke),
            (DbWerte.BAUTEILART_VORHANGFASSADE, Bauteilart.Vorhangfassade),
            (DbWerte.BAUTEILART_SONSTIGES, Bauteilart.Sonstiges),
        };

        /// <summary>
        /// Die vier Randbedingungen der Zeile ↔ Kern-Aufzählung, in Schemareihenfolge.
        /// <see cref="Bauteilrand.Innen"/> hat KEINEN Persistenzwert — es steht als NULL an
        /// Innenwand und Decke (<see cref="RandAusZeile(string, string)"/>).
        /// </summary>
        private static readonly (string Wert, Bauteilrand Rand)[] Raender =
        {
            (DbWerte.RANDBEDINGUNG_AUSSENLUFT, Bauteilrand.Aussenluft),
            (DbWerte.RANDBEDINGUNG_ERDREICH, Bauteilrand.Erdreich),
            (DbWerte.RANDBEDINGUNG_ZONE, Bauteilrand.Zone),
            (DbWerte.RANDBEDINGUNG_UNBEHEIZT, Bauteilrand.Unbeheizt),
        };

        // =====================================================================
        //  Die Persistenzwerte
        // =====================================================================

        /// <summary>Die Kern-Art zum Persistenzwert; <c>null</c> für einen unbekannten Wert.</summary>
        internal static Bauteilart? ArtAusZeile(string bauteilart)
        {
            foreach ((string wert, Bauteilart art) in Arten)
                if (string.Equals(wert, bauteilart, StringComparison.Ordinal)) return art;
            return null;
        }

        /// <summary>Der Persistenzwert einer Kern-Art.</summary>
        internal static string ArtFuerZeile(Bauteilart art)
        {
            foreach ((string wert, Bauteilart a) in Arten)
                if (a == art) return wert;
            throw new ArgumentOutOfRangeException(nameof(art), art, "Bauteilart ohne Persistenzwert.");
        }

        /// <summary>
        /// <b>Die Regel der leeren Randbedingung</b> — die EINE Stelle, an der sie steht: An einer
        /// <b>Innenwand</b> oder <b>Decke</b> heißt NULL „innerhalb der Zone"
        /// (<see cref="Bauteilrand.Innen"/>, Innenbauteilgruppe, symmetrisch beaufschlagt — so
        /// liest der Bauteilweg <see cref="BauteilEingang.Gruppe"/>); an jeder anderen Art heißt
        /// NULL Außenluft. Das Schema kennt keinen Wert „innen" (W6: allein die Randbedingung
        /// trägt die Aussage); ein IFC-Innenbauteil mit Gegenstück in derselben Zone ist innere
        /// Masse (Mehrzonenkonzept, Abschnitt IFC-Randbedingung) und steht so. Ein ausdrücklich
        /// gesetzter Wert gilt an jeder Art — eine Innenwand an <c>AUSSENLUFT</c> ist ein
        /// Außenbauteil.
        /// </summary>
        internal static bool LeerHeisstInnen(Bauteilart art) => art == Bauteilart.Innenwand || art == Bauteilart.Decke;

        /// <summary>
        /// Die Randbedingung einer Zeile nach der Regel <see cref="LeerHeisstInnen"/>;
        /// <c>null</c>, wenn Bauteilart oder Randbedingung kein bekannter Persistenzwert ist.
        /// Die Prüfregel des Dialogs (<c>GebaeudeZonenCtrl.BrauchtAzimut</c>) fragt hierüber.
        /// </summary>
        internal static Bauteilrand? RandAusZeile(string bauteilart, string randbedingung)
        {
            Bauteilart? art = ArtAusZeile(bauteilart);
            if (art == null) return null;
            return RandAusZeile(art.Value, randbedingung);
        }

        /// <summary>Die Randbedingung einer Zeile der Art <paramref name="art"/>; <c>null</c> für einen unbekannten Wert.</summary>
        internal static Bauteilrand? RandAusZeile(Bauteilart art, string randbedingung)
        {
            if (randbedingung == null) return LeerHeisstInnen(art) ? Bauteilrand.Innen : Bauteilrand.Aussenluft;
            foreach ((string wert, Bauteilrand rand) in Raender)
                if (string.Equals(wert, randbedingung, StringComparison.Ordinal)) return rand;
            return null;
        }

        /// <summary>
        /// Der Persistenzwert einer Kern-Randbedingung an einem Bauteil der Art
        /// <paramref name="art"/> — die Umkehrung von <see cref="RandAusZeile(Bauteilart, string)"/>:
        /// „innerhalb der Zone" wird NULL und geht nur an Innenwand und Decke; jede andere
        /// Randbedingung steht ausdrücklich, auch Außenluft (an Innenwand und Decke hieße NULL
        /// sonst „innen").
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.BauteilUngueltig"/>, wenn
        /// „innerhalb der Zone" an einer anderen Art steht.</exception>
        internal static string RandFuerZeile(Bauteilart art, Bauteilrand rand, string wer)
        {
            if (rand == Bauteilrand.Innen)
            {
                if (LeerHeisstInnen(art)) return null;
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_ZEILE_INNEN, wer, ArtFuerZeile(art)));
            }
            foreach ((string wert, Bauteilrand r) in Raender)
                if (r == rand) return wert;
            throw new ArgumentOutOfRangeException(nameof(rand), rand, "Randbedingung ohne Persistenzwert.");
        }

        // =====================================================================
        //  Zeile → Kern
        // =====================================================================

        /// <summary>
        /// Die Zonen aller Gebäude eines Projekts als Kern-Datensätze, je <c>Tab_Gebaeude.ID</c>
        /// in der Reihenfolge der Zeilen. <b>Eine Zone, deren Zeilen sich nicht abbilden lassen,
        /// wird nicht verschwiegen</b>, sondern als <see cref="GebaeudeZonensatz.Unlesbar"/>
        /// angehängt: Das Lesen (auch für den Dialog) bleibt heil, und erst der Lauf, der die
        /// Zone rechnet, bricht für dieses Gebäude benannt ab (<see cref="GebaeudeZonensatz.EineZone"/>).
        /// </summary>
        /// <param name="zonenJeGebaeude">Die Zeilen je Gebäude, wie <c>GebaeudeZonenCtrl.LesenJeProjekt</c> sie liefert.</param>
        /// <param name="aufbauten">Die Aufbauten des Projekts samt Schichten, je <c>Tab_Bauteilaufbau.ID</c>.</param>
        internal static Dictionary<int, IReadOnlyList<GebaeudeZonensatz>> JeGebaeude(
            IReadOnlyDictionary<int, List<ZoneModel>> zonenJeGebaeude,
            IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten)
        {
            var ergebnis = new Dictionary<int, IReadOnlyList<GebaeudeZonensatz>>();
            if (zonenJeGebaeude == null) return ergebnis;
            foreach (KeyValuePair<int, List<ZoneModel>> je in zonenJeGebaeude)
            {
                var saetze = new List<GebaeudeZonensatz>();
                foreach (ZoneModel z in je.Value ?? new List<ZoneModel>())
                {
                    if (z == null) continue;
                    try
                    {
                        saetze.Add(AlsZonensatz(z, aufbauten));
                    }
                    catch (GebaeudeModellException ex)
                    {
                        saetze.Add(GebaeudeZonensatz.Unlesbar(z.ID, z.Bezeichner, ex.Grund, ex.Message));
                    }
                }
                if (saetze.Count > 0) ergebnis[je.Key] = saetze.AsReadOnly();
            }
            return ergebnis;
        }

        /// <summary>
        /// Eine Zone samt Bauteilen als Kern-Datensatz (Regeln: Klassenkopf). Die Bauteile in der
        /// Reihenfolge der Liste (<c>Rang</c>), die Schichten in der des Aufbaus
        /// (<c>Reihenfolge</c>, innen → außen).
        /// </summary>
        /// <exception cref="GebaeudeModellException">benannt: unbekannte Bauteilart oder Randbedingung
        /// (<see cref="GebaeudeModellFehler.BauteilUngueltig"/>), ein Aufbau, den das Projekt nicht führt
        /// (ebenso), eine Schicht ohne Wertekopie oder ein Aufbau ohne Schicht
        /// (<see cref="GebaeudeModellFehler.SchichtUngueltig"/>).</exception>
        internal static GebaeudeZonensatz AlsZonensatz(ZoneModel zone, IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            string wer = Wer(zone.Bezeichner);
            var bauteile = new List<BauteilEingang>();
            int nummer = 0;
            foreach (BauteilModel b in zone.Bauteile ?? new List<BauteilModel>())
            {
                nummer++;
                if (b == null) continue;
                string werB = wer + ", " + (string.IsNullOrEmpty(b.Bezeichner) ? "#" + nummer.ToString(CultureInfo.InvariantCulture) : b.Bezeichner);
                bauteile.Add(AlsBauteil(b, aufbauten, werB));
            }
            // Die Nutzfläche der Zone (G3, Flächenschlüssel): NULL heißt die des Gebäudes (NaN).
            return new GebaeudeZonensatz(zone.ID, zone.Bezeichner, bauteile.AsReadOnly(),
                                         zone.Nutzflaeche ?? double.NaN);
        }

        /// <summary>Ein Bauteil als Kern-Eingang (Regeln: Klassenkopf).</summary>
        internal static BauteilEingang AlsBauteil(BauteilModel b, IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten, string wer)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));

            Bauteilart art = ArtAusZeile(b.Bauteilart)
                ?? throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                       Format(MyResource.Resource.SIMENG_G3_ZEILE_BAUTEILART, wer, b.Bauteilart ?? "", string.Join(", ", DbWerte.BAUTEILARTEN)));
            Bauteilrand rand = RandAusZeile(art, b.Randbedingung)
                ?? throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                       Format(MyResource.Resource.SIMENG_G3_ZEILE_RANDBEDINGUNG, wer, b.Randbedingung, string.Join(", ", DbWerte.RANDBEDINGUNGEN)));

            IReadOnlyList<Schicht> schichten = null;
            if (b.ID_Aufbau.HasValue)
            {
                if (aufbauten == null || !aufbauten.TryGetValue(b.ID_Aufbau.Value, out BauteilaufbauModel aufbau) || aufbau == null)
                    throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                        Format(MyResource.Resource.SIMENG_G3_ZEILE_AUFBAU_FEHLT, wer, b.ID_Aufbau.Value.ToString(CultureInfo.InvariantCulture)));
                schichten = AlsSchichten(aufbau, wer);
            }

            return new BauteilEingang(
                b.Bezeichner, art, b.Flaeche, rand,
                uWert_WM2K: b.U_Wert ?? double.NaN,
                schichten: schichten,
                neigungGrad: b.Neigung ?? double.NaN,
                azimutGrad: b.Azimut ?? double.NaN,
                gWert: b.g_Wert ?? double.NaN,
                rahmenanteil: b.Rahmenanteil ?? double.NaN,
                verschattungsfaktor: b.Verschattungsfaktor ?? double.NaN,
                psiL_WK: b.Psi_L ?? 0.0);
        }

        /// <summary>
        /// Die Schichten eines Aufbaus, innen → außen. Ein Aufbau ohne Schicht ist ein Aufbau
        /// ohne U-Wert — benannt abgelehnt, nicht still als „nur U-Wert" gelesen.
        /// </summary>
        internal static IReadOnlyList<Schicht> AlsSchichten(BauteilaufbauModel aufbau, string wer)
        {
            if (aufbau == null) throw new ArgumentNullException(nameof(aufbau));
            List<BauteilschichtModel> zeilen = (aufbau.Schichten ?? new List<BauteilschichtModel>()).Where(s => s != null).ToList();
            string werA = wer + " (" + aufbau.Bezeichner + ")";
            if (zeilen.Count == 0)
                throw new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_AUFBAU_LEER, werA));
            var schichten = new Schicht[zeilen.Count];
            for (int i = 0; i < zeilen.Count; i++) schichten[i] = AlsSchicht(zeilen[i], i + 1, werA);
            return schichten;
        }

        /// <summary>
        /// <b>Eine Schicht aus ihrer Zeile</b> — mit der Wertekopie λ/ρ/c_p der Schicht, nie mit
        /// den Werten des Baustoffs, auf den sie zeigt (die Kopie hält ein gerechnetes Ergebnis
        /// fest, Softwarearchitektur 2.6). Eine Luftschicht ohne λ ist eine ruhende Luftschicht
        /// (<see cref="Schicht.RuhendeLuft"/>, Widerstand nach DIN EN ISO 6946 Tabelle 8); eine
        /// Luftschicht mit λ eine Schicht mit äquivalenter Leitfähigkeit, ρ und c_p dürfen dort
        /// fehlen (NaN, keine Kapazität). Eine Schicht, die keine Luftschicht ist, braucht alle
        /// drei Werte — fehlt einer, ist das ein benannter Fehler.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.SchichtUngueltig"/>.</exception>
        internal static Schicht AlsSchicht(BauteilschichtModel s, int nummer, string wer)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (s.IstLuftschicht)
                return s.Lambda.HasValue
                    ? new Schicht(s.Dicke, s.Lambda.Value, s.Rho ?? double.NaN, s.Cp ?? double.NaN, true)
                    : Schicht.RuhendeLuft(s.Dicke);

            var fehlt = new List<string>();
            if (!s.Lambda.HasValue) fehlt.Add("λ");
            if (!s.Rho.HasValue) fehlt.Add("ρ");
            if (!s.Cp.HasValue) fehlt.Add("c_p");
            if (fehlt.Count > 0)
                throw new GebaeudeModellException(GebaeudeModellFehler.SchichtUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_ZEILE_SCHICHT_OHNE_WERTE, wer,
                           nummer.ToString(CultureInfo.CurrentCulture), string.Join(", ", fehlt)));
            return new Schicht(s.Dicke, s.Lambda.Value, s.Rho.Value, s.Cp.Value);
        }

        // =====================================================================
        //  Kern → Zeile
        // =====================================================================

        /// <summary>
        /// <b>Ein Kern-Datensatz als neue Zeilen</b> — für „Gebäude als eine Zone übernehmen":
        /// eine Zone mit der Id <c>-1</c>, ihre Bauteile mit <c>-1, -2, …</c> in der Reihenfolge
        /// des Satzes, alle mit Herkunft <see cref="DbWerte.HERKUNFT_VORGABE"/>; die Nutzfläche des
        /// Satzes (NaN = NULL), die übrigen Spalten der Zone bleiben NULL (= Wert des Gebäudes), sie
        /// ist beheizt. Rang, Eltern-Id und endgültige Ids vergibt
        /// <c>GebaeudeZonenCtrl.SpeichernJeGebaeude</c>.
        ///
        /// <para>NaN wird NULL, <c>Psi_L</c> 0 wird NULL (keine Wärmebrücke), die Randbedingung
        /// nach <see cref="RandFuerZeile"/>. Damit liest <see cref="AlsZonensatz"/> denselben
        /// Satz zurück, Feld für Feld und bitgleich.</para>
        /// </summary>
        /// <exception cref="GebaeudeModellException">benannt (<see cref="GebaeudeModellFehler.BauteilUngueltig"/>),
        /// wenn ein Bauteil etwas trägt, das die Zeile nicht aufnehmen kann: einen Schichtaufbau
        /// (die Übernahme schreibt keinen Aufbau), einen eigenen Übergangskoeffizienten oder
        /// „innerhalb der Zone" an einer anderen Art als Innenwand oder Decke.</exception>
        internal static ZoneModel AlsZoneModel(GebaeudeZonensatz satz)
        {
            if (satz == null) throw new ArgumentNullException(nameof(satz));
            if (satz.Lesefehler != null)
                throw new ArgumentException("Eine unlesbare Zone lässt sich nicht zurückschreiben.", nameof(satz));

            string wer = Wer(satz.Bezeichnung);
            var zone = new ZoneModel
            {
                ID = -1,
                Bezeichner = satz.Bezeichnung,
                Nutzflaeche = Zahl(satz.Nutzflaeche_M2),
                IstBeheizt = true,
                Herkunft = DbWerte.HERKUNFT_VORGABE,
            };
            for (int i = 0; i < satz.Bauteile.Count; i++)
            {
                BauteilEingang b = satz.Bauteile[i] ?? throw new ArgumentException("Der Bauteilsatz enthält einen leeren Eintrag.", nameof(satz));
                string werB = wer + ", " + (string.IsNullOrEmpty(b.Bezeichnung) ? "#" + (i + 1).ToString(CultureInfo.InvariantCulture) : b.Bezeichnung);
                zone.Bauteile.Add(AlsBauteilModel(b, -(i + 1), werB));
            }
            return zone;
        }

        /// <summary>Ein Kern-Bauteil als neue Zeile mit der vorläufigen Id <paramref name="vorlaeufigeId"/> (Regeln: <see cref="AlsZoneModel"/>).</summary>
        internal static BauteilModel AlsBauteilModel(BauteilEingang b, int vorlaeufigeId, string wer)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (vorlaeufigeId >= 0) throw new ArgumentOutOfRangeException(nameof(vorlaeufigeId), vorlaeufigeId, "Eine vorläufige Id ist negativ.");
            if (b.HatSchichten)
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_ZEILE_SCHICHTEN, wer));
            if (!double.IsNaN(b.AlphaKonInnen_WM2K) || !double.IsNaN(b.AlphaKonAussen_WM2K))
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Format(MyResource.Resource.SIMENG_G3_ZEILE_ALPHA, wer));

            return new BauteilModel
            {
                ID = vorlaeufigeId,
                Bezeichner = b.Bezeichnung ?? "",
                Bauteilart = ArtFuerZeile(b.Art),
                ID_Aufbau = null,
                Flaeche = b.Flaeche_M2,
                U_Wert = Zahl(b.UWert_WM2K),
                g_Wert = Zahl(b.GWert),
                Rahmenanteil = Zahl(b.Rahmenanteil),
                Verschattungsfaktor = Zahl(b.Verschattungsfaktor),
                Neigung = Zahl(b.NeigungGrad),
                Azimut = Zahl(b.AzimutGrad),
                Randbedingung = RandFuerZeile(b.Art, b.Rand, wer),
                Psi_L = b.PsiL_WK == 0.0 ? (double?)null : b.PsiL_WK,
                Herkunft = DbWerte.HERKUNFT_VORGABE,
            };
        }

        // =====================================================================
        //  intern
        // =====================================================================

        /// <summary>NaN → NULL, sonst der Wert.</summary>
        private static double? Zahl(double w) => double.IsNaN(w) ? (double?)null : w;

        /// <summary>Die Bezeichnung einer Zone für Meldungen.</summary>
        internal static string Wer(string zone)
            => Format(MyResource.Resource.SIMENG_G3_ZONE_WER, zone ?? "");

        private static string Format(string muster, params object[] werte)
            => string.Format(CultureInfo.CurrentCulture, muster, werte);
    }
}
