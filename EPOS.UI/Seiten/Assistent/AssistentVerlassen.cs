namespace EPOS.UI.Seiten.Assistent;

/// <summary>
/// Der Ausgang der Rueckfrage „ungespeicherte Eingaben" (Anwenderentscheid
/// <b>62b-E-1</b> vom 11.09.2026).
///
/// <para><b>Drei Wege, kein vierter.</b> Verlaesst der Anwender den Assistenten
/// mit Eingaben, die noch nicht geschrieben sind, fragt er nach: SPEICHERN laeuft
/// denselben Weg wie der Knopf — Pruefung und die Transaktion aus W16a-O-1 —,
/// VERWERFEN wechselt ohne zu schreiben, BLEIBEN bricht den Wechsel ab.</para>
///
/// <para><b>Warum es drei Werte sind und kein <c>bool</c>.</b> Der Wirt muss zwei
/// Dinge auseinanderhalten koennen, die beide „weiter" bedeuten: Nach dem
/// SPEICHERN gibt es etwas zu melden (und einen Projektkontext, der nachzuziehen
/// ist), nach dem VERWERFEN nicht. Und ein gescheiterter Speicherlauf endet als
/// <see cref="Bleiben"/> — der Assistent steht dann noch, mit seiner Meldung.</para>
/// </summary>
public enum AssistentVerlassen
{
    /// <summary>Der Wechsel findet NICHT statt; der Assistent bleibt, wie er war.</summary>
    Bleiben,

    /// <summary>Der Wechsel findet statt, geschrieben wurde nichts.</summary>
    Verwerfen,

    /// <summary>Der Speicherlauf ist gelungen; der Wechsel findet statt.</summary>
    Gespeichert
}
