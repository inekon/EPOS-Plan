using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Lasten, Außenluft und Zeitpläne eines Raums der gbXML-Datei</b> (Welle DA-1, Konzept Datenaustausch der
    /// Simulationsdaten 4.1), in SI: Wärmeabgabe je Person [W], Außenluft je Person [m³/h] und je Fläche [m³/(h·m²)],
    /// Luftwechsel der Zone [1/h] und die referenzierten Zeitpläne an <c>Space</c> (Personen, Geräte, Licht) und <c>Zone</c>
    /// (Heizen, Kühlen, Lüftung). Personenzahl, Geräte- und Lichtleistung stehen schon an <see cref="AbbildRaum"/>.
    /// <c>null</c> heißt jeweils: die Datei liefert nichts — dann füllt das Nutzungsprofil (E138 F2).
    /// </summary>
    internal sealed class GbxmlRaumlasten
    {
        /// <summary>Wärmeabgabe je Person [W] (<c>PeopleHeatGain</c>); <c>null</c> = keine.</summary>
        public double? WaermeJePersonW { get; set; }

        /// <summary>Die Art der Wärmeabgabe (<c>heatGainType</c>: <c>Total</c>, <c>Sensible</c>, <c>Latent</c>); <c>null</c> = keine Angabe.</summary>
        public string WaermeArt { get; set; }

        /// <summary>Außenluft je Person [m³/h] (<c>OAFlowPerPerson</c> am Raum, sonst an der Zone); <c>null</c> = keine.</summary>
        public double? AussenluftJePersonM3h { get; set; }

        /// <summary>Außenluft je Fläche [m³/(h·m²)] (<c>OAFlowPerArea</c> am Raum, sonst an der Zone); <c>null</c> = keine.</summary>
        public double? AussenluftJeFlaecheM3hM2 { get; set; }

        /// <summary>Luftwechsel der Zone [1/h] (<c>Zone/AirChangesPerHour</c>) — die Nutzerlüftung; <c>null</c> = keiner.</summary>
        public double? ZonenLuftwechselJeH { get; set; }

        /// <summary>Der Personenplan (<c>Space/@peopleScheduleIdRef</c>); <c>null</c> = keiner.</summary>
        public GbxmlZeitplan PersonenPlan { get; set; }

        /// <summary>Der Geräteplan (<c>Space/@equipmentScheduleIdRef</c>); <c>null</c> = keiner.</summary>
        public GbxmlZeitplan GeraetePlan { get; set; }

        /// <summary>Der Lichtplan (<c>Space/@lightScheduleIdRef</c>); <c>null</c> = keiner.</summary>
        public GbxmlZeitplan LichtPlan { get; set; }

        /// <summary>Der Heizsollwertplan (<c>Zone/@heatSchedIdRef</c>); <c>null</c> = keiner.</summary>
        public GbxmlZeitplan HeizPlan { get; set; }

        /// <summary>Der Kühlsollwertplan (<c>Zone/@coolSchedIdRef</c>); <c>null</c> = keiner.</summary>
        public GbxmlZeitplan KuehlPlan { get; set; }

        /// <summary>Der Lüftungsplan (<c>Zone/@airChangesSchedIdRef</c>, Annahme); <c>null</c> = keiner.</summary>
        public GbxmlZeitplan LueftungPlan { get; set; }

        /// <summary>Die Pläne, die der Raum referenziert (ohne Doppel).</summary>
        public IEnumerable<GbxmlZeitplan> Plaene
            => new[] { PersonenPlan, GeraetePlan, LichtPlan, HeizPlan, KuehlPlan, LueftungPlan }.Where(p => p != null).Distinct();

        /// <summary>Die Zahl der gelesenen Lastwerte dieses Satzes.</summary>
        public int Werte => new[] { WaermeJePersonW, AussenluftJePersonM3h, AussenluftJeFlaecheM3hM2, ZonenLuftwechselJeH }.Count(w => w.HasValue);

        /// <summary>Trägt der Satz etwas?</summary>
        public bool Traegt => Werte > 0 || Plaene.Any();
    }
}
