using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das gerade geöffnete Projekt.
    ///
    /// <para><b>Die Lücke, die diese Schnittstelle schließt.</b> Eine „aktuelle
    /// Projekt-ID" gab es im Bestand nicht als eigenen Wert — sie hing an
    /// <c>Program.startfrm.m_ID_Projekt</c>, also an einem Feld der Startmaske.
    /// <c>KiAktionenProjekt</c> hält das ausdrücklich als „Andockpunkt
    /// <c>Program.startfrm</c>" fest. Kern-Code, der wissen will, an welchem Projekt
    /// gearbeitet wird, musste deshalb die Oberfläche kennen.</para>
    ///
    /// <para>Träger bleibt bis iU9 die Startmaske; die Windows-Fassung reicht nur durch.</para>
    /// </summary>
    public interface IProjektKontext
    {
        /// <summary><c>Tab_Projekt.ID</c> des offenen Projekts; <c>0</c> = keins offen.</summary>
        int Id { get; }

        /// <summary>Projektname des offenen Projekts; <c>""</c> = keins offen.</summary>
        string Name { get; }

        /// <summary>Klimaregion des offenen Projekts; <c>""</c>, wenn unbekannt.</summary>
        string Klimazone { get; }

        /// <summary>
        /// Setzt das offene Projekt und zieht alles nach, was daran hängt — Kopfband,
        /// Klimaregion, Kachelstatus, Menüfreigaben, „zuletzt geöffnet".
        /// </summary>
        /// <param name="id">
        /// <c>Tab_Projekt.ID</c>; wird nur als Rückfall benutzt, wenn kein Name vorliegt.
        /// </param>
        /// <param name="name">Projektname — der führende Schlüssel.</param>
        /// <returns>
        /// <c>false</c>, wenn es keine Oberfläche gibt oder zu Name und ID kein Projekt
        /// existiert (z. B. zwischenzeitlich gelöscht). Der Aufrufer erkennt daran, dass
        /// er keine Erfolgsmeldung zeigen darf; der bisherige Kontext bleibt stehen.
        /// </returns>
        bool Uebernehmen(int id, string name);

        /// <summary>Wird nach jedem erfolgreichen Wechsel ausgelöst.</summary>
        event Action Gewechselt;
    }
}
