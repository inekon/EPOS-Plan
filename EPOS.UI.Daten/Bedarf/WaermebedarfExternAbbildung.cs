using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Abbildung Zeile ↔ Modell des Dialogs „Wärmebedarf extern“ / „Kältebedarf extern“ — plattformfrei, damit
    /// der Rückweg der Windows-Hülle (<c>WaermebedarfExternHuelle</c>) und des Assistenten ohne Windows prüfbar ist.
    /// Der Dialog kennt nur Kennung, Ganglinie, Bezeichner, Kanal und Kälte-Art; die übrigen Felder der Zuordnung
    /// stehen allein im Modell.
    /// </summary>
    internal static class WaermebedarfExternAbbildung
    {
        /// <summary>Die Dialogzeilen aus der Modellliste des Aufrufers.</summary>
        internal static List<WaermebedarfExternZeile> Zeilen(IEnumerable<Z_ProjWaermebedarfModel> modelle)
        {
            var zeilen = new List<WaermebedarfExternZeile>();
            if (modelle == null) return zeilen;
            foreach (Z_ProjWaermebedarfModel m in modelle) zeilen.Add(AusModell(m));
            return zeilen;
        }

        /// <summary>
        /// Der Änderungsrückruf des Dialogs: schreibt die Dialogzeilen in die Modellliste des Aufrufers zurück, die
        /// danach gespeichert wird (Löschen + Neuanlegen).
        ///
        /// <para><b>Die geladenen Modelle werden fortgeschrieben, nicht ersetzt.</b> Was der Dialog nicht kennt —
        /// die Deckungsfelder des Schritts 213 (Deckungsart, EER-Weg und Stützpunkte, Kühlträger, eigener Zähler) —
        /// kommt aus dem beim Öffnen geladenen Modell derselben Zuordnung (über <c>ID_Z</c>); sonst fiele eine
        /// Split-Zeile bei jeder Änderung im Dialog still auf „zentral“ zurück. Neue Zeilen (<c>ID_Z = 0</c>) stehen
        /// auf der Vorgabe; einen Kanal außer Kühlung normalisiert der Speicherweg auf „zentral“.</para>
        /// </summary>
        internal static Action Rueckweg(List<WaermebedarfExternZeile> zeilen, List<Z_ProjWaermebedarfModel> modelle,
                                        int projektId)
        {
            // Stand beim Öffnen festhalten: Der Rückruf leert die Liste des Aufrufers und füllt sie neu.
            var geladen = new Dictionary<int, Z_ProjWaermebedarfModel>();
            foreach (Z_ProjWaermebedarfModel m in modelle)
                if (m != null && m.m_ID_Z > 0) geladen[m.m_ID_Z] = m;

            return () =>
            {
                modelle.Clear();
                foreach (WaermebedarfExternZeile z in zeilen)
                {
                    geladen.TryGetValue(z.IdZ, out Z_ProjWaermebedarfModel vorher);
                    modelle.Add(NachModell(z, projektId, z.IdZ > 0 ? vorher : null));
                }
            };
        }

        internal static WaermebedarfExternZeile AusModell(Z_ProjWaermebedarfModel m)
        {
            return new WaermebedarfExternZeile
            {
                IdZ = m.m_ID_Z,
                IdGanglinie = m.m_ID_Ganglinie,
                Bezeichner = m.m_szBezeichner ?? "",
                Kanal = Z_ProjektGebGanglinieCtrl.KanalOderHeizung(m.Kanal),
                KaelteArt = KaelteArtSchema.Normal(m.KaelteArt)
            };
        }

        /// <summary>
        /// Das Modell einer Dialogzeile. <paramref name="vorher"/> ist das beim Öffnen geladene Modell derselben
        /// Zuordnung: Seine Deckungsfelder werden übernommen; Kennung, Ganglinie, Bezeichner, Kanal und Kälte-Art
        /// kommen aus der Zeile.
        /// </summary>
        internal static Z_ProjWaermebedarfModel NachModell(WaermebedarfExternZeile z, int projektId,
                                                           Z_ProjWaermebedarfModel vorher = null)
        {
            var neu = new Z_ProjWaermebedarfModel();
            if (vorher != null) Z_ProjektKaeltebedarfCtrl.DeckungUebernehmen(vorher, neu);
            neu.m_ID_Z = z.IdZ;
            neu.m_ID_Projekt = projektId;
            neu.m_ID_Ganglinie = z.IdGanglinie;
            neu.m_szBezeichner = z.Bezeichner;
            neu.Kanal = Z_ProjektGebGanglinieCtrl.KanalOderHeizung(z.Kanal);
            neu.KaelteArt = KaelteArtSchema.Normal(z.KaelteArt);
            return neu;
        }
    }
}
