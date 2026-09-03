using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Rahmen des Projektassistenten aus Sicht seiner Seiten.
    ///
    /// <para>
    /// <b>Wozu.</b> Bis Paket P4 suchten die mitlaufenden Fachformulare ihren Rahmen
    /// über eine <b>Zeichenkette</b>: <c>foreach (Form form in Application.OpenForms)
    /// if (form.Name == "WizardParent")</c> — elf gleichlautende Kopien, die beim
    /// Umbenennen des Formulars stillschweigend ins Leere gelaufen wären und die jedes
    /// beliebige offene Fenster gleichen Namens akzeptiert hätten. An ihre Stelle tritt
    /// die typisierte Anmeldung <see cref="WizardParent.Aktiver"/>: Der Rahmen trägt
    /// sich selbst ein und meldet sich beim Schließen wieder ab.
    /// </para>
    /// <para>
    /// Die Schnittstelle führt <b>nur</b>, was die Seiten wirklich brauchen. Die
    /// Modelllisten des Rahmens (<c>list_werzmodel</c> &amp; Co.) bleiben bewusst
    /// draußen: Sie sind öffentliche Felder von <see cref="WizardParent"/>, werden von
    /// den Fachformularen unverändert weiterverwendet und gehören nicht in einen
    /// Vertrag, der später einmal einen zweiten Rahmen tragen soll.
    /// </para>
    /// </summary>
    public interface IAssistentRahmen
    {
        /// <summary>Die Seiten des Assistenten in ihrer festen Reihenfolge (siehe <see cref="AssistentSeiten"/>).</summary>
        List<WizardSeite> Seiten { get; }

        /// <summary>Betriebsart: <see cref="WizardParent.WIZARD_MODE_NEU"/> oder <see cref="WizardParent.WIZARD_MODE_BEARBEITEN"/>.</summary>
        int Betriebsart { get; }

        /// <summary>Tab_Projekt.ID des Projekts, an dem der Assistent gerade arbeitet; 0 im Neu-Modus vor dem Speichern.</summary>
        int ProjektID { get; }
    }
}
