# Recherche: Rücklaufgrenzen bei anderen Wärmeerzeugern (Teil C)

Web-Recherche, nur lesen, kein Repo-Bezug. Frage: Ist eine „Rücklaufgrenze" (wie sie das
Konzept für die Wärmepumpe einführt) auch bei den anderen EPOS-Plan-Erzeugern fachlich
relevant — als harte Grenze oder als stetiger Wirkungsgradeinfluss? Alle Angaben mit Quelle
oder „(Abl.)" für eigene Ableitung aus mehreren Quellen; Sekundärquellen als „(sek.)"
markiert. Normtexte (DIN EN 303-5, DIN V 18599-5, AGFW FW 515) sind paraphrasiert, nicht
zitiert.


## 1 Brennwertkessel Gas/Öl

**Physikalischer Grund.** Der Wasserdampf im Abgas kondensiert erst unterhalb des
Taupunkts; die dabei freiwerdende Kondensationsenthalpie ist der „Brennwertnutzen". Je
niedriger die Kesselrücklauftemperatur (= Eintrittstemperatur des Heizwassers in den
Wärmetauscher), desto tiefer kann das Abgas abgekühlt werden und desto mehr Kondensat
fällt an [Q80][Q82].

**Werte.** Wasserdampftaupunkt: Erdgas ca. 57–59 °C, Heizöl EL ca. 47–48 °C (Schwankung je
nach Luftüberschuss/Abgaszusammensetzung) [Q80][Q81]. Anteil der Kondensationsenthalpie am
Brennwert: Wasserstoff 18 %, Erdgas 11 %, Heizöl EL 6 % [Q80] — eine Wasserstoffbeimischung
ins Erdgas erhöht also tendenziell sowohl den Taupunkt als auch den kondensierbaren Anteil,
quantitative Kurven für Gemische waren über frei zugängliche Quellen nicht auffindbar (Abl.:
qualitative Extrapolation aus [Q80], keine belastbare Zahl für H2-Beimischungsgrade).

Nutzungsgrad in Abhängigkeit von der Rücklauftemperatur (bezogen auf den Heizwert Hi,
Erdgas-Brennwertkessel) [Q82]:

| Rücklauftemperatur | Erdgas | Heizöl EL |
|---|---|---|
| 20 °C | 102,4 % | 99,4 % |
| 35 °C | 99 % | 96 % |
| 50 °C | 92,2 % | 92,7 % |
| 60 °C | 91,7 % | 92,2 % |

Spanne zwischen 20 °C und 60 °C Rücklauf: rund 10,7 Prozentpunkte bei Erdgas, 7,2 bei
Heizöl [Q82]. Praktisch findet bei Gas oberhalb ca. 50–53 °C kaum noch Kondensation statt,
der optimale Bereich liegt bei 30–35 °C [Q83] — Heizöl liegt wegen des niedrigeren Taupunkts
systematisch enger (Kondensation bei Heizöl nur bis knapp unter 48 °C relevant, bei Erdgas
bis knapp unter 57–59 °C) [Q80].

**Folge Wärmepumpen-Vorwärmbetrieb (45–55 °C Kesselrücklauf).** Ein Kesselrücklauf von
45–55 °C liegt bei Erdgas noch unterhalb des Taupunkts (Kondensation läuft weiter, aber
schon deutlich reduziert), bei Heizöl bereits am oder über dem Taupunkt (Kondensation fällt
praktisch aus). Aus der Tabelle oben folgt für Erdgas beim Übergang von einem typischen
Niedertemperatur-Rücklauf (≈30–35 °C, ≈99–100 %) auf 45–55 °C (interpoliert ≈93–95 %) ein
Verlust von rund 5–7 Prozentpunkten Nutzungsgrad (Abl., linear interpoliert aus [Q82]); bei
Heizöl ist der Verlust kleiner, weil dort schon bei 35 °C nur noch 96 % erreicht werden.
Gegenstimmen aus der Praxis bestätigen das qualitativ: Eine Rücklaufanhebung ist bei
Brennwertkesseln „kontraproduktiv", weil sie genau diesen Effekt erzeugt [Q83].

**Hart oder stetig.** Stetiger Wirkungsgradeinfluss, keine harte Grenze. Kein Geräteschaden:
moderne Brennwert-Wärmetauscher (Edelstahl, Aluminium-Silizium-Legierung) sind für den
gesamten Rücklaufbereich korrosionsfest ausgelegt; der Kessel läuft oberhalb des Taupunkts
einfach wie ein Niedertemperaturkessel ohne Brennwertnutzen weiter [Q80][Q83].

**Abhilfe/Wechselwirkung in der Anlage.** Bei reinen Brennwertanlagen keine Abhilfe nötig
(niedriger Rücklauf ist erwünscht). In Hybridanlagen mit Wärmepumpe ist die Vorwärmung des
Kesselrücklaufs durch die Wärmepumpe genau der Mechanismus, der den Brennwertnutzen
schmälert — eine bewusste Zielkonfliktentscheidung, keine Schutzmaßnahme [Q83].

**Abbildung in Normen/Programmen.** DIN V 18599-5 ermittelt eine monatliche
Rücklauftemperatur des Wärmeverteilnetzes (rechnerisch, sinngemäß nach den Vorgaben zur
Netzauslegung) und führt sie dem Teillast-/Kesselnutzungsgrad zu; die Norm selbst wird hier
nicht zitiert, nur der Rechengang paraphrasiert [Q104]. EnergyPlus bildet das in
`Boiler:HotWater` über eine „Normalized Boiler Efficiency Curve" ab, die als Funktion von
Teillast (PLR) allein oder von PLR **und** Kesselwassertemperatur (Ein- oder Austritt, je
nach Modellwahl) parametriert werden kann — genau damit lässt sich ein Brennwerteffekt über
der Rücklauftemperatur nachbilden [Q105].


## 2 Niedertemperatur- und Konstanttemperaturkessel (Bestand)

**Physikalischer Grund.** Bei echten Niedertemperaturkesseln ist der Brennraum thermisch von
der Kesselwassertemperatur entkoppelt; seine Oberflächentemperatur bleibt auch bei
niedrigem Rücklauf hoch genug, um den Taupunkt an der Feuerraumwand nicht zu unterschreiten
— Betrieb bis 35 °C Kesselwassertemperatur ist laut Bauart vorgesehen [Q84]. Bei alten
Konstanttemperaturkesseln (Gusskessel ohne diese Entkopplung) führt ein zu kalter Rücklauf
dagegen zu Kondensation an der Kesselwand und, verbunden mit Ruß, zu „Glanzruß" und
Taupunktkorrosion [Q84][Q85].

**Werte.** Für Konstanttemperaturkessel wird eine Mindestrücklauftemperatur von
üblicherweise 55–65 °C genannt, in der Praxis oft als Sollwert 55 °C angesetzt [Q83][Q84].

**Hart oder stetig.** Eher hart, aber zeitverzögert: keine Sofortabschaltung, sondern
fortschreitende Korrosion/Versottung bei dauerhafter Unterschreitung — ein Bauteilschaden,
der sich über Monate bis Jahre aufbaut, kein augenblicklicher Trip wie bei BHKW oder
Brennstoffzelle.

**Abhilfe.** Rücklaufanhebung über ein thermostatisch geregeltes Ventil (ggf. mit
Drosselventil), das Kesselvorlauf in den Rücklauf einmischt, bis der Sollwert erreicht ist;
bei Kesseln, die nicht niedertemperaturtauglich sind, ist eine solche Rücklaufanhebung
vorzusehen [Q83][Q84].

**Abbildung in Normen/Programmen.** In EPOS-Plan-Begriffen wäre das kein Sonderfall der
Wärmeerzeuger-Rücklaufgrenze, sondern der ganz normale Fall einer vorgeschalteten
Rücklaufanhebung (Mischventil) als Anlagenkomponente — die Mindesttemperatur ist eine
Randbedingung für die Mischventilregelung, nicht für den Kessel selbst. Für EPOS-Plan
relevant nur, soweit Bestandsanlagen mit Konstanttemperaturkessel überhaupt simuliert werden
(derzeit kein Referenzprojekt mit diesem Kesseltyp).


## 3 Biomassekessel (Pellet, Hackschnitzel, Scheitholz/Holzvergaser)

**Physikalischer Grund.** Wie bei Öl/Gas kondensiert Wasserdampf aus dem Rauchgas, sobald
die Kesseleintrittstemperatur (= Rücklauf) den Taupunkt unterschreitet; anders als bei
Gas-Brennwertgeräten ist der klassische Biomassekessel aber nicht für Kondensatkontakt
ausgelegt — das Kondensat ist sauer und bildet mit Flugasche/Ruß aggressiven „Glanzruß", der
Kessel und Kamin angreift [Q85][Q87].

**Werte.** Mindestrücklauf-Richtwerte: Pellets ca. 55 °C, Scheitholz/Holzvergaser 60–65 °C,
in Herstellerangaben häufig „nicht unter 55–60 °C" [Q85]. DIN EN 303-5 verlangt für
Festbrennstoffkessel eine Kesseleintrittstemperatur von mindestens 55 °C, sicherzustellen
durch eine funktionierende Rücklaufanhebung (paraphrasiert, nicht zitiert) [Q87].

**Hart oder stetig.** Hart im Sinn eines Pflichtbauteils: Rücklaufanhebung ist in praktisch
jeder Holz-/Pelletkesselanlage verbaut (meist als Thermostatventil ohne Hilfsenergie, das
erst oberhalb der Solltemperatur öffnet) und gilt als zwingende Voraussetzung für
Inbetriebnahme, Gewährleistung und Förderfähigkeit — technisch ist die Schädigung
(Versottung, verkürzte Lebensdauer) aber wie bei Kesseln in Abschnitt 2 zeitverzögert, kein
Sofort-Trip [Q84][Q85].

**Pufferpflicht (BImSchV/BAFA).** Die 1. BImSchV nennt für neue Festbrennstoff-/
Holzvergaserkessel einen Richtwert von mindestens 55 l Pufferspeicher je kW
Nennwärmeleistung bzw. 12 l je Liter Füllraum des Brennraums [Q86]. Die BAFA-Förderung
verlangt gestaffelt: 30 l/kW bei Hackschnitzel- und Pelletkesseln, 55 l/kW bei Scheitholz-
und Kombikesseln [Q86].

**Wirkungsgradfolge (Brennwert-Biomasse).** Moderne Pellet-Brennwertkessel (z. B. Fröling P4,
Ökofen Pellematic Condens — Herstellerbeispiele, nur in dieser Scratchpad-Datei genannt)
kühlen das Abgas gezielt auf ca. 40–70 °C ab und erreichen dadurch Kesselwirkungsgrade von
über 100 % bis zu rund 117 % bezogen auf den Heizwert, mit einer Mehrleistung von 10–20 %
gegenüber dem reinen Heizwertbetrieb und einer Feinstaubreduktion von rund 20–40 % [Q88].
Das ist das Gegenstück zu Abschnitt 1: Bei diesen Geräten senkt eine zu hohe
Rücklauftemperatur den Brennwertnutzen genauso wie bei Gas — die Rücklaufanhebung muss dann
so ausgelegt sein, dass sie den Kondensationsbetrieb nicht unnötig unterdrückt.

**Abbildung in Normen/Programmen.** DIN EN 303-5 regelt neben der Mindest-Rücklauftemperatur
auch die Mindest-Pufferspeichergröße über eine Formel (Ausbrennzeit, Heizlast,
Kesselnennleistung) — paraphrasiert, nicht zitiert [Q86][Q87]. Simulationsseitig wird dieser
Effekt typischerweise nicht als eigene Kesselkennlinie, sondern als vorgeschaltete
Rücklaufanhebung plus Pufferspeicher-Ladestrategie abgebildet (vgl. Abschnitt 2).


## 4 BHKW (Verbrennungsmotor, Mikro-BHKW, Stirling) und Brennstoffzellen

**Physikalischer Grund.** Beim Verbrennungsmotor-BHKW begrenzt die Kühlwassereintritts-
temperatur in den Motorblock die zulässige Rücklauftemperatur der Wärmeauskopplung —
oberhalb der Grenze ist die Kühlung des Motors nicht mehr sichergestellt, Überhitzungsschutz
schaltet ab [Q89]. Ein optionaler Abgas-Brennwertwärmetauscher kann zusätzlich die
Kondensationswärme des Abgases nutzen, sofern der Rücklauf unter dem Taupunkt liegt — analog
zu Abschnitt 1 [Q91]. Brennstoffzellen (PEM) sind grundsätzlich Niedertemperaturgeräte mit
engerem zulässigem Temperaturfenster als Verbrennungs-BHKW [Q92][Q93].

**Werte.**
- Verbrennungsmotor-BHKW allgemein: Kühlwassereintritt in den Motorblock max. 70 °C, sonst
  Sicherheitsabschaltung „WAK Temperatur Rücklauf" [Q89]. In der Literatur wird teils auch
  eine engere Auslegungsgrenze von 60–65 °C genannt [Q89] (Herstellerabhängig).
- Konkretes Beispiel SenerTec Dachs (Mikro-BHKW, Herstellerbeispiel, nur hier genannt):
  Rücklauftemperatur werkseitig auf 70 °C eingestellt, im Regler zwischen 50 und 73 °C
  einstellbar; oberhalb 73 °C schaltet das Gerät ab (max. Vorlauf 83 °C) [Q90].
- Brennwert-Zusatzwärmetauscher am BHKW: Kondensation setzt voraus, dass der Rücklauf unter
  ca. 50 °C liegt; die Mehrleistung kann bis zu ca. 10 Prozentpunkte Wirkungsgrad bzw. in der
  Spitze rund 15 % zusätzliche Wärmeleistung bringen [Q91].
- Stirling-Mikro-KWK: Viessmann Vitotwin 300-W (seit ca. 2020 nicht mehr im Programm,
  Herstellerbeispiel) war für Rücklauftemperaturen von 30–60 °C ausgelegt; WhisperGen wird
  mit max. 77 °C Rücklauf bzw. typisch 80/60 °C Vor-/Rücklauf angegeben [Q94]. Stirling-
  Geräte reagieren mit sinkendem Gesamtwirkungsgrad (elektrisch **und** thermisch) auf
  steigenden Rücklauf [Q94].
- Brennstoffzelle PEM (Viessmann Vitovalor PT2, Herstellerbeispiel): Rücklauf **maximal
  50 °C** als Datenblattgrenze, Heizwasser-Volumenstrom 0–850 l/h im Bereich 6–50 °C
  Rücklauf; el. 750 W / th. 1,1 kW, Gesamtwirkungsgrad ca. 92 %; ideal für Fußbodenheizung,
  Betrieb mit Radiatoren möglich, aber enger am Limit [Q92].
- Brennstoffzelle SOFC (BlueGen, Herstellerbeispiel): el. 1,5 kW / th. 0,6 kW im
  Auslegungspunkt, ca. 24 h Vorheizzeit, kein taktender Betrieb möglich [Q93] — eine
  spezifische Rücklauf-Obergrenze war über frei zugängliche Quellen nicht auffindbar;
  SOFC-Geräte gelten analog zu PEM als niedertemperaturorientiert (Abl., keine belastbare
  Einzelzahl gefunden).

**Hart oder stetig.** Hart. Sowohl Verbrennungsmotor-BHKW als auch Brennstoffzellen schalten
bei Überschreitung der zulässigen Rücklauftemperatur aktiv ab (Sicherheits-/Schutzfunktion),
kein allmählicher Wirkungsgradverlust wie bei Kesseln. Zusätzlich gibt es bei BHKW mit
Brennwert-Zusatzwärmetauscher einen stetigen Effizienzanteil (Kondensation ja/nein je nach
Rücklauf), der sich dem harten Grundlimit überlagert.

**Abhilfe.** Pufferspeicher zur hydraulischen Entkopplung (Standard bei praktisch jeder
BHKW-Anlage, auch um Takten zu vermeiden); hydraulische Weiche/Rücklaufbegrenzung, damit das
BHKW immer mit dem kühlsten verfügbaren Rücklauf (z. B. aus dem unteren Pufferbereich)
versorgt wird, statt mit dem Mischrücklauf der gesamten Anlage.

**Abbildung in Normen/Programmen.** VDI 4655 liefert Referenzlastprofile für die
Wirtschaftlichkeitsrechnung von KWK-Anlagen in Ein-/Mehrfamilienhäusern, ASUE die
BHKW-Kenndaten (elektrische/thermische Leistung, Wirkungsgrade je Anlagentyp) — die
Rücklauf-Grenztemperatur selbst steht nicht in VDI 4655, sondern im
Hersteller-Datenblatt [Q108][Q89]. Für EPOS-Plan ist das strukturell derselbe Mechanismus,
den das Konzept für die Wärmepumpe vorschlägt: eine geräteseitige Obergrenze, bei deren
Überschreitung der Erzeuger in der betroffenen Stunde nicht verfügbar ist.


## 5 Fernwärme-Hausstation (Randnotiz)

**Hinweis vorab.** EPOS-Plan bildet Fernwärme nach Aufgabenstellung nur als Randfall ab;
dieser Abschnitt ist entsprechend kurz gehalten.

**Physikalischer/vertraglicher Grund.** Aus Netzsicht ist eine niedrige Rücklauftemperatur
entscheidend für die Übertragungseffizienz (geringere Netzverluste, kleinerer
Massenstrombedarf bei gegebener Leistung); eine zu hohe Rücklauftemperatur einer einzelnen
Hausstation verschlechtert die Gesamteffizienz des Netzes und wird daher vertraglich
begrenzt [Q96][Q97]. Ursache für überhöhte Rücklauftemperaturen liegt laut Literatur
überwiegend auf Kundenseite (Trinkwassererwärmung im Sommer, verschmutzte Filter, falsche
Regelung, hydraulische Fehler in der Hausanlage), nicht beim Netzbetreiber [Q97].

**Werte.** Zielwerte in der Literatur: idealerweise ≤ 45 °C, vertraglich häufig 50 °C als
Grenzwert, in älteren Netzen/Verträgen auch bis 60 °C [Q96]. Die AGFW-Regelwerke (u. a.
FW 515 für technische Anschlussbedingungen) verlangen eine am Wärmeübertrager begrenzte
Grädigkeit (Temperaturdifferenz Primär-/Sekundärrücklauf) von in der Regel ≤ 5 K im
Auslegungsfall sowie eine außentemperaturabhängig gleitende Rücklaufbegrenzung
(paraphrasiert, nicht zitiert) [Q95].

**Hart oder stetig.** Vertraglich/wirtschaftlich hart (Pönalen/Bonus-Malus-Tarife; einzelne
Netzbetreiber wie die Stadtwerke Bruneck belohnen niedrige Rücklauftemperaturen mit
niedrigerem Grundpreis, hohe Rücklauftemperaturen über ca. 60 °C werden mit Zuschlägen
belegt) [Q96]. Technisch am Gerät selbst (Wärmeübertrager) eher stetig, zusätzlich wirkt ein
aktiver Rücklaufbegrenzer (Regeleingriff, kein Abschalten) [Q95][Q96].

**Abhilfe.** Rücklaufbegrenzer (Regelventil, das den Primärvolumenstrom bei zu hoher
Sekundär-Rücklauftemperatur drosselt), korrekte Dimensionierung des Wärmeübertragers,
hydraulisch saubere TWE-Einbindung.

**Relevanz für EPOS-Plan.** Gering, siehe Übersichtstafel — solange Fernwärme nur als
einfacher Erzeuger ohne Netzrückwirkungsmodell abgebildet wird.


## 6 Solarthermie

**Physikalischer Grund.** Der Kollektoreintritt entspricht dem Rücklauf aus Speicher/
Solarkreis; der Kollektorwirkungsgrad sinkt mit steigender Temperaturdifferenz zwischen
mittlerer Kollektortemperatur und Umgebung gemäß der Kollektorgleichung
η = η0 − a1·ΔT/G − a2·ΔT²/G, mit a1 (linearer) und a2 (quadratischer)
Wärmeverlustbeiwert [Q98].

**Werte.** Typische Kennwerte: Flachkollektor a1 ≈ 4,6–4,81 W/(m²·K), a2 ≈
0,022–0,025 W/(m²·K²); Vakuumröhrenkollektor a1 ≈ 1,2–1,3 W/(m²·K), a2 ≈
0,006–0,007 W/(m²·K²) [Q98]. Da der Flachkollektor vier Mal so schnell an Wirkungsgrad
verliert, dreht sich der Vorteil bei wachsender Übertemperatur um: Bei 40 K
Temperaturdifferenz liegt die Vakuumröhre bei ca. 68 % gegen ca. 59 % beim
Flachkollektor [Q99]. Eine belastbare einzelne %/K- oder kWh/(m²·a)/K-Kennzahl für den
Rücklaufeinfluss speziell bei solarer Heizungsunterstützung war über frei zugängliche
Quellen nicht auffindbar; der Zusammenhang folgt aber unmittelbar aus a1/a2 oben (Abl.: pro
Kelvin zusätzlicher Übertemperatur sinkt η um a1/G plus einen mit ΔT wachsenden Term aus
a2/G — bei typischer Einstrahlung von rund 700–800 W/m² entspricht das grob 0,5–0,7 %-Punkte
Wirkungsgrad je zusätzlichem Kelvin beim Flachkollektor, deutlich weniger bei der
Vakuumröhre).

**Hart oder stetig.** Stetig, keine harte Grenze durch die Rücklauftemperatur selbst. Die
einzige harte Grenze ist die Stagnation, die aber primär durch fehlende Wärmeabnahme/
fehlenden Volumenstrom bei hoher Einstrahlung ausgelöst wird, nicht direkt durch die
Rücklauftemperatur aus dem Pufferspeicher.

**Abhilfe/Anlagenseite.** Schichtspeicher mit tiefer Entnahme für den Solarkreis-Rücklauf,
damit der Kollektor stets mit der kühlsten verfügbaren Temperatur arbeitet; der
Heizungsrücklauf sollte ebenfalls mit möglichst tiefer Temperatur in den Speicher geführt
werden, um die Schichtung nicht zu stören und so indirekt auch den Solar-Rücklauf niedrig zu
halten [Q100].

**Abbildung in Normen/Programmen.** Die europäisch normierte Kollektorgleichung
(η0, a1, a2) ist Standard in Simulationsprogrammen; Polysun verwendet exakt dieses
„Kollektormodell nach Europäischer Norm" und bildet die Rücklauftemperatur-Abhängigkeit über
ΔT = Kollektortemperatur − Umgebungstemperatur ab, wobei die Kollektoreintrittstemperatur
aus der Speicherschichtung folgt [Q107].


## 7 Elektro-Heizstab / Elektrokessel / Power-to-Heat

**Kurzbefund.** Kein Rücklaufgrenzwert in dem hier diskutierten Sinn. Elektrische
Wärmeerzeuger haben keinen Verbrennungsprozess und damit keinen Taupunkt, keine
Motorkühlung und keine Brennwertkennlinie — die Rücklauftemperatur beeinflusst weder
Wirkungsgrad (der bei Widerstandsheizung nahe 100 % liegt und temperaturunabhängig ist) noch
Betriebssicherheit. Die einzige relevante Temperaturgrenze ist eine **Höchsttemperatur**
(Vorlauf- bzw. Kesseltemperatur), die über einen Sicherheitstemperaturbegrenzer (STB)
abgesichert ist — strukturell das Gegenteil einer Rücklaufgrenze.

**Beispielwerte (Herstellerbeispiele, nur hier genannt).** Industrieller Elektrokessel Bosch
ELHB: Heißwassererzeugung 90–133 °C, STB fest auf 108 °C [Q102]. Elektroheizstab im
Pufferspeicher: Regelbereich Thermostat ca. 31–75 °C, STB bei 98 °C [Q102].

**Relevanz für EPOS-Plan.** Gering — keine zusätzliche Modellierung nötig.


## 8 Pufferspeicher (Randnotiz)

**Physikalischer Grund.** Ein Pufferspeicher ist kein Erzeuger, aber die Rücklauftemperatur
der angeschlossenen Verbraucher/Erzeuger bestimmt, wie gut sich die Temperaturschichtung
halten lässt. Strömt der Rücklauf mit zu hohem Volumenstrom oder ohne geeignete
Einströmvorrichtung ein, vermischt er die Schichten und „zerstört" die nutzbare
Temperaturspreizung; mit einer Schichtlanze (z. B. ein Rohr mit seitlichen, gelochten
PE-Einsätzen) wird das einströmende Wasser dichtegetrieben auf die passende Höhe
verteilt [Q101][Q103]. Bei korrekter Einbindung kann sich eine Trennschicht von nur rund
10 cm zwischen einer 80 °C-Zone und einer deutlich kühleren Rücklaufzone ausbilden [Q101].

**Hart oder stetig.** Stetig — reine Effizienz-/Nutzbarkeitsfrage (verfügbare
Temperaturspreizung, Ladezustand für nachgeschaltete Erzeugerprioritäten), kein
Schadens- oder Abschaltmechanismus.

**Relevanz für EPOS-Plan.** Mittel — die Einschichtung bestimmt indirekt, welcher Erzeuger
in welcher Stunde laden/entladen kann; sofern das Pufferzonenmodell in EPOS-Plan die
Einschichtung bereits über Zonen/Schwellen abbildet, ist hier kein zusätzlicher Mechanismus
nötig.


## Übersichtstafel

| Erzeuger | Art der Grenze | Typischer Wert | Wirkung bei Verletzung | Übliche Abhilfe | Relevanz Stundenrechnung |
|---|---|---|---|---|---|
| Brennwertkessel Gas | stetig (Wirkungsgrad) | Taupunkt ≈57–59 °C; Nutzungsgrad 102,4 % (20 °C) → 91,7 % (60 °C) [Q80][Q82] | Kein Schaden, nur sinkender Brennwertnutzen (NT-Betrieb) | Niedriger Rücklauf halten, keine Rücklaufanhebung | **Hoch** — Rücklauf schwankt stündlich mit Lastgang/Heizkurve, geht direkt in den Stunden-Nutzungsgrad ein |
| Brennwertkessel Öl | stetig (Wirkungsgrad) | Taupunkt ≈47–48 °C; Nutzungsgrad 99,4 % (20 °C) → 92,2 % (60 °C) [Q80][Q82] | Wie Gas, engeres Fenster | Wie Gas | **Hoch** — wie Gas |
| NT-/Konstanttemperaturkessel (Bestand) | hart, zeitverzögert (Korrosion) | Mindestrücklauf 55–65 °C [Q83][Q84] | Taupunktkorrosion/Versottung über Monate–Jahre | Rücklaufanhebung (Mischventil) | **Gering/mittel** — kein Referenzprojekt mit diesem Kesseltyp; wo vorhanden, wirkt die Mischventil-Randbedingung, nicht der Kessel selbst |
| Biomassekessel (Pellet/Hackschnitzel/Scheitholz) | hart (Pflichtbauteil) + stetig (Brennwertvariante) | Mindestrücklauf 55 °C (Pellet) / 60–65 °C (Scheitholz); DIN EN 303-5 ≥55 °C [Q85][Q87] | Versottung, Teerbildung, Garantie-/Förderverlust | Rücklaufanhebung (Pflicht) + Pufferspeicher (BImSchV/BAFA: 30–55 l/kW) [Q86] | **Hoch** — Rücklaufanhebung/Puffer-Interaktion ist stundenweise ladezustandsabhängig |
| BHKW (Verbrennungsmotor) | **hart** (Sicherheitsabschaltung) | Motorkühlkreis max. 70 °C; Beispiel Dachs: Abschaltung > 73 °C [Q89][Q90] | Sofortige Abschaltung des BHKW | Pufferspeicher, hydraulische Weiche, kühlster Rücklauf zum BHKW | **Hoch** — exakt der Mechanismus, den das WP-Konzept für die Rücklaufgrenze vorschlägt; stundenweise Verfügbarkeit |
| Brennstoffzelle PEM (z. B. Vitovalor) | **hart** (Betriebsgrenze Datenblatt) | Rücklauf max. 50 °C [Q92] | Eingeschränkter/kein Betrieb oberhalb der Grenze | Niedertemperatur-Heizkreis (FBH), Pufferspeicher | **Hoch** — enges Fenster, stundenweise kritisch bei Mischrücklauf |
| Fernwärme-Hausstation (Randnotiz) | vertraglich hart (Pönale) + stetig (Netz) | Vertragsgrenze meist 50 °C, ideal ≤45 °C, AGFW-Grädigkeit ≤5 K [Q95][Q96] | Preiszuschlag, Regeleingriff durch Rücklaufbegrenzer | Rücklaufbegrenzer, korrekte TWE-Einbindung | **Gering** — EPOS-Plan bildet Fernwärme nur als einfachen Erzeuger ohne Netzmodell ab |
| Solarthermie | stetig (Wirkungsgrad) | a1 ≈1,2–4,8 W/(m²K) je Kollektortyp; 59–68 % η bei ΔT=40 K [Q98][Q99] | Sinkender Kollektorertrag, keine Abschaltung | Schichtspeicher mit tiefer Solar-Rücklaufentnahme | **Mittel** — Kollektoreintritt folgt der stündlichen Pufferschichtung, meist schon über Zonenmodell erfasst |
| Elektro-Heizstab/-kessel, Power-to-Heat | hart, aber **Höchsttemperatur**, keine Rücklaufgrenze | STB z. B. 98–108 °C (Vorlauf/Kessel) [Q102] | Abschaltung bei Übertemperatur (Vorlauf, nicht Rücklauf) | Sicherheitstemperaturbegrenzer | **Gering** — kein Rücklaufbezug |
| Pufferspeicher (Randnotiz) | stetig (Schichtung) | Trennschicht ca. 10 cm bei korrekter Einbindung [Q101] | Vermischte Schichten, geringere nutzbare Spreizung | Schichtlanze, dichtegetriebene Einströmung | **Mittel** — bestimmt indirekt stundenweise Erzeugerpriorität, meist bereits im Pufferzonenmodell abgebildet |


## Quellen (alle abgerufen am 08.10.2026)

- [Q80] Wikipedia: Rauchgaskondensation. https://de.wikipedia.org/wiki/Rauchgaskondensation
- [Q81] Boy, H.-G. (bosy-online.de): Heizwert oder Brennwerttechnik (Taupunktangabe Erdgas ≈57 °C, über Suchindex-Auszug; Volltext beim Abruf nicht dekodierbar). http://www.bosy-online.de/Brennwerttechnik.htm
- [Q82] Böhm, G. / SBZ-Online (Ulmer Verlag): Wie effektiv sind Brennwertkessel? (Tabelle Nutzungsgrad vs. Rücklauftemperatur, über Suchindex-Auszug; PDF-Volltext beim Abruf nicht dekodierbar). https://www.sbz-online.de/sites/default/files/ulmer/de-sbz/document/file_186069.pdf
- [Q83] Paschotta, R.: Rücklaufanhebung. RP-Energie-Lexikon. https://www.energie-lexikon.info/ruecklaufanhebung.html
- [Q84] bosy-online.de: Rücklauftemperaturanhebung für Holz- und Pelletkessel (über Suchindex-Auszug). http://www.bosy-online.de/Ruecklauftemperaturanhebung.htm
- [Q85] Kesselheld: Rücklaufanhebung für Holzkessel – Möglichkeiten und Vorgehen. https://www.kesselheld.de/ruecklaufanhebung-fuer-holzkessel/
- [Q86] BAFA: Förderübersicht Biomasse (Basis-, Innovations- und Zusatzförderung). https://www.bafa.de/SharedDocs/Downloads/DE/Energie/ew_biomasse_foerderuebersicht.pdf
- [Q87] DIN EN 303-5:2021-11, Heizkessel – Teil 5 (Anwendungsbereich/Mindestkesseleintrittstemperatur und Pufferformel paraphrasiert nach Sekundärquelle, nicht zitiert). https://webstore.ansi.org/standards/din/dinen3032021de · (sek.) SBZ-Monteur: Wie funktioniert eigentlich die Auslegung eines Pufferspeichers für Festbrennstoffkessel? https://www.sbz-monteur.de/wie-funktioniert-eigentlich/die-auslegung-eines-pufferspeichers-fuer-festbrennstoffkessel
- [Q88] FNR (Fachagentur Nachwachsende Rohstoffe): Schlussbericht BioKond – Abgaskondensation zur Effizienzsteigerung bei Biomasseheizanlagen. https://www.fnr.de/fileadmin/projektdatenbank/22409517.pdf
- [Q89] Yados GmbH: Betriebsanleitung Blockheizkraftwerke (BHKW) mit Otto- und Dieselmotoren, Stand 09/2021. https://yados.de/medias/Betriebsanleitung-Blockheizkraftwerke-09-2021-DE.pdf
- [Q90] SenerTec GmbH: Technisches Datenblatt Dachs 2.9; Bedien- und Einstellanleitung MSR2 (Herstellerbeispiel). https://www.senertec.de/wp-content/uploads/2021/01/8098-501-000-06-Technisches-Datenblatt-Dachs-2_9.pdf · http://www.heizkostenfrei-leben.de/downloads/technische_doku/Bedien%20Einstellanleitung%20MSR2.pdf
- [Q91] Sokratherm GmbH: Brennwertnutzung zur Wirkungsgradverbesserung (Herstellerbeispiel). https://www.sokratherm.de/blockheizkraftwerke/ausstattungsvarianten/brennwertnutzung-zur-wirkungsgradverbesserung/ · BHKW-Infozentrum: Glossar Brennwertnutzung. https://www.bhkw-infozentrum.de/glossar-lexikon/brennwertnutzung.html
- [Q92] Viessmann Werke: Datenblatt/Planungsanleitung Vitovalor PT2, Brennstoffzellen-Heizgerät (Herstellerbeispiel). https://community.viessmann.de/viessmann/attachments/viessmann/customers-fuel-cell/1149/1/Datenblatt%20Vitovalor%20PT2.PDF
- [Q93] energie-experten.org: Brennstoffzellen-Heizgeräte im Vergleich, Stand 2026 (sek.). https://www.energie-experten.org/heizung/brennstoffzelle/brennstoffzellen-heizung/heizgeraete
- [Q94] BHKW-Infozentrum: Whispergen – der Whispertech-Stirlingmotor als Mini-BHKW (sek.). https://www.bhkw-infozentrum.de/innovative/stirlingmotor_whispertech.html · bhkw-prinz.de: WhisperGen Mini-BHKW Stirlingmotor (sek., Herstellerbeispiele). https://www.bhkw-prinz.de/whispergen-mikro-bhkw-mit-stirlingmotor/10/comment-page-1
- [Q95] AGFW-Regelwerk FW 515, zitiert nach Technischen Anschlussbedingungen Fernwärme (Beispiel ESTW; paraphrasiert, nicht zitiert). https://www.estw.de/de/Energie-Wasser/Hausanschluss/Fernwaerme/TAB-FW-515-M-2025-02.pdf
- [Q96] mywarm GmbH: So reduzieren Fernwärme-Versorger die Rücklauftemperaturen in ihren Netzen, Stand 2026 (sek.). https://mywarm.com/so-reduzieren-fernwaerme-versorger-die-ruecklauftemperaturen-in-ihren-netzen/
- [Q97] readkong.com (Digitalisat Fachartikel): Vorsicht heiß – Ursachen hoher Rücklauftemperaturen in Fernwärmenetzen (sek.). https://de.readkong.com/page/vorsicht-heis-ursachen-hoher-r-cklauftemperaturen-in-4829687
- [Q98] kollektorleistung.de: Die Kollektorgleichung zur Berechnung der Wirkungsgradkennlinie. https://www.kollektorleistung.de/Kollektorgleichung.html
- [Q99] SBZ-Monteur: Wie entsteht eigentlich der Wirkungsgrad von Solaranlagen? – Flachko vs. Röhre. https://www.sbz-monteur.de/wie-entsteht-eigentlich/der-wirkungsgrad-von-solaranlagen-flachko-vs-roehre
- [Q100] Swissolar: Dimensionierungshilfe Sonnenkollektoren – Grundlagen für thermische Solaranlagen. https://www.swissolar.ch/01_wissen/fachwissen/solarwaerme/merkblaetter/sonnenkollektoren-dimensionierungshilfe.pdf
- [Q101] Viessmann Community (Forumsdiskussion, sek.): Optimale Temperaturschichtung im Pufferspeicher. https://community.viessmann.de/t5/Waermepumpe-Hybridsysteme/Optimale-Temperaturschichtung-im-Pufferspeicher/td-p/516819
- [Q102] Bosch Industriekessel: Elektrokessel ELHB – Power-to-Heat-Lösung (Herstellerbeispiel). https://www.bosch-industrial.com/de/de/ocs/gewerbe-industrie/elektrokessel-fuer-heizwaerme-und-heisswasser-elhb-20939747-p/
- [Q103] Holzheizer-Forum (Forumsdiskussion, sek.): Schichtlanze an Pufferspeicher. https://www.holzheizer-forum.de/forum/thread/51094-schichtlanze-an-pufferspeicher/
- [Q104] DIN V 18599-5 (Anwendungsbereich/Rechengang zur monatlichen Rücklauftemperatur paraphrasiert, nicht zitiert). Übersicht: https://de.wikipedia.org/wiki/DIN_V_18599 · https://www.baunormenlexikon.de/norm/din-v-18599-5/27724809-5b44-4721-858f-accae738b1e3
- [Q105] Bigladder Software / NREL: Boilers – Engineering Reference, EnergyPlus Documentation. https://energyplus.readthedocs.io/en/latest/guides/engineering-reference/14.2-boilers.html
- [Q106] TU Graz (Digital Library, sek., Suchindex-Auszug, PDF beim Abruf nicht erreichbar): Nutzungsgradoptimierung von Biomasse- und Biomasse/Solar-Heizsystemen. https://diglib.tugraz.at/download.php?id=576a76cc50bae&location=browse
- [Q107] Velasolaris: Kollektormodell nach Europäischer Norm (EN) – Polysun-Handbuch. https://www.velasolaris.com/handbuch/polysun-standard/solarthermie-und-konventionelle-heiztechnik/solarkollektoren/kollektormodell-nach-europaeischer-norm-en/
- [Q108] BHKW-Infozentrum: VDI 4655 – Referenzlastprofile von Ein- und Mehrfamilienhäusern für KWK-Anlagen. https://www.bhkw-infozentrum.de/richtlinien/vdi-4655-referenzlastprofile-von-ein-und-mehrfamilienhaeusern-fuer-kwk-anlagen.html · ASUE: BHKW-Kenndaten 2014-15. https://www.asue.de/leistungen/publikationen/bhkw-kenndaten-2014-15


## Offene Punkte / nicht erreichbare Quellen

- delta-q.de „Auswirkung Kesselnutzungsgrad" (kesselnutzungsgrad.pdf) und „4.2_Kessel.pdf"
  sowie die Hochschule-Biberach-Studie zu bivalenten BHKW/Brennwertkessel-Anlagen ließen
  sich per Volltextabruf nicht dekodieren (komprimiertes PDF); die daraus über den
  Such-Index gewonnenen Werte sind in Abschnitt 1 verarbeitet und als solche
  gekennzeichnet [Q82].
- Für Wasserstoffbeimischung ins Erdgas und deren Einfluss auf den Taupunkt bzw. die
  Brennwertnutzen-Kurve wurde keine quantitative frei zugängliche Quelle gefunden; nur die
  qualitative Kondensationsenthalpie-Reihe H2 18 % / Erdgas 11 % / Heizöl 6 % [Q80].
- Für SOFC-Brennstoffzellen (BlueGen) wurde kein expliziter Rücklauf-Grenzwert gefunden.
- Für Solarthermie wurde keine einzelne, direkt zitierfähige %/K- oder
  kWh/(m²·a)/K-Kennzahl für den Rücklauftemperatur-Einfluss bei Heizungsunterstützung
  gefunden; Abschnitt 6 leitet die Größenordnung aus den Kollektorkennwerten ab (Abl.).
- AGFW FW 520 (Arbeitsblatt Hausanschlussstationen) wurde nur über Sekundärnennung
  identifiziert, der Originaltext war hinter keiner frei zugänglichen Quelle auffindbar.
