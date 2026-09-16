# Gegenlesen des Konzepts — Blickwinkel IFC, Bibliotheken, Lizenzen (15.09.2026)

**Protokoll.** Einzelbefund des Workflows ‚konzept-gegenlesen‘ (Modell Opus, mit Quellenprüfung im Netz) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

**BEFUNDE — Blickwinkel „IFC, Bibliotheken, Lizenzen"** (Datei: `C:\Waermeplan\EPOS-Plan\Dokumentation\aktuell\Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`)

---

**1. CDDL-Bewertung juristisch falsch verkürzt — die Quelltextpflicht gilt auch für unveränderte Dateien**
(a) Kap. 7.4, Z. 1066–1067: „**CDDL-1.0 ist Datei-Copyleft:** Einbindung in ein proprietäres Produkt ist zulässig, solange / die CDDL-Dateien unverändert bleiben; der Lizenztext wird mit ausgeliefert (Setup: Lizenzhinweise)."
(b) Die Bedingung „solange die CDDL-Dateien unverändert bleiben" ist keine CDDL-Bedingung; CDDL §3.1 verlangt die Quelltext-Verfügbarkeit für **jede** ausgelieferte Covered Software — auch für unveränderte — samt Hinweis an den Empfänger, wie er sie bekommt; unverändert zu bleiben erspart nur die Offenlegung **eigener** Modifikationen.
(c) SPDX, CDDL-1.0 §3.1: „Any Covered Software that You distribute or otherwise make available in Executable form must also be made available in Source Code form and that Source Code form must be distributed only under the terms of this License." (https://spdx.org/licenses/CDDL-1.0.html); §3.5 erlaubt die Executable unter eigener Lizenz, §3.6 den „Larger Work"; identischer Text in xBIMs eigener Lizenzseite https://docs.xbim.net/license/license.html
(d) **hoch** (sachlich falsch, und zwar an der Stelle, auf die sich Q9 stützt)
(e) Ersatztext: „**CDDL-1.0 ist Datei-Copyleft.** Die Einbindung in ein proprietäres Produkt ist zulässig (§3.6 Larger Work); die eigene Binärfassung darf unter eigener Lizenz ausgeliefert werden (§3.5). Drei Auflagen bleiben: (1) §3.1 — der Quelltext **aller** ausgelieferten CDDL-Dateien muss in Source-Code-Form unter CDDL verfügbar sein, auch wenn nichts geändert wurde; es genügt der dauerhafte Verweis auf github.com/xBimTeam bzw. die NuGet-Quellpakete, und dieser Verweis muss dem Empfänger mitgeteilt werden. (2) §3.4 — Copyright-, Patent- und Markenvermerke dürfen nicht entfernt werden; eigene Änderungen an CDDL-Dateien sind als solche zu kennzeichnen. (3) Der Lizenztext wird mit ausgeliefert (Setup: Lizenzhinweise). Werden xBIM-Dateien geändert, ist der geänderte Quelltext dieser Dateien unter CDDL bereitzustellen — deshalb Regel: **xBIM nur als NuGet-Paket einbinden, nie forken.**"

---

**2. Lizenzspalte für `Xbim.Geometry` ist unvollständig — sie zieht LGPL-2.1-Code nach und widerspricht dem eigenen Ausschlusssatz**
(a) Kap. 7.4, Tabelle Z. 1055: „| Xbim.Geometry | 6.3.891 | CDDL-1.0 | net472, net8.0 | **C++/CLI, nur Windows** (`Ijwhost.dll`, `win-x64`) | dito | ja (OCCT) |" — und Z. 1068: „LGPL (IfcOpenShell) und GPL (IFC2SB) scheiden für den Kern aus."
(b) `Xbim.Geometry.Engine.Interop` hängt zwingend an `Xbim.Geometry.Occt` ≥ 7.8.1, und dieses Paket steht unter **LGPL-2.1 mit OCCT-Ausnahme (OCCT-exception-1.0)**, nicht unter CDDL — die Windows-Schale zöge also genau die Lizenzfamilie herein, die zwei Zeilen tiefer ausgeschlossen wird.
(c) nuspec `xbim.geometry.engine.interop/6.3.891-netcore`: Abhängigkeit `Xbim.Geometry.Occt (7.8.1)` (https://api.nuget.org/v3-flatcontainer/xbim.geometry.engine.interop/6.3.891-netcore/xbim.geometry.engine.interop.nuspec); Lizenz des Occt-Pakets: https://www.nuget.org/packages/Xbim.Geometry.Occt („LGPL 2.1 … with the special exception enabling header files to be distributed", OCCT-exception-1.0)
(d) **hoch** (irreführend: der Leser entscheidet über Q9 auf Basis einer falschen Lizenzangabe)
(e) Ersatztext für die Tabellenzelle und den Ausschlusssatz: „| Xbim.Geometry | 6.3.891-netcore (Vorabversion; letzte stabile: 5.1.820) | CDDL-1.0, **zieht `Xbim.Geometry.Occt` 7.8.1 unter LGPL-2.1 + OCCT-exception-1.0 nach** | … |" und „LGPL und GPL scheiden für den Kern aus (IfcOpenShell LGPL-3.0, IFC2SB GPL); auch `Xbim.Geometry` fällt damit aus dem Kern — nicht nur wegen C++/CLI, sondern weil OCCT unter LGPL-2.1 steht und in der Windows-Schale dynamische Bindung und Austauschbarkeit nachgewiesen werden müssten."

---

**3. `Xbim.Geometry 6.3.891` ist eine Vorabversion; die letzte stabile Version ist 5.1.820**
(a) Kap. 7.4, Z. 1055: „| Xbim.Geometry | 6.3.891 | CDDL-1.0 | net472, net8.0 |"
(b) Die Version heißt `6.3.891-netcore` und ist ein Prerelease; die höchste stabile Version von `Xbim.Geometry.Engine.Interop` ist 5.1.820 (11.04.2025, nur net472) — auf einer stabilen Version gibt es .NET-8-Unterstützung also gar nicht.
(c) https://api.nuget.org/v3-flatcontainer/xbim.geometry.engine.interop/index.json (höchste Einträge: `5.1.820`, `6.1.801-netcore`, `6.3.873-netcore`, `6.3.891-netcore`); https://www.nuget.org/packages/Xbim.Geometry.Engine.Interop („Latest version 5.1.820, 4/11/2025")
(d) **mittel**
(e) Ersatztext: „| Xbim.Geometry (`Xbim.Geometry.Engine.Interop`) | **6.3.891-netcore (Vorabversion)**, stabil zuletzt 5.1.820 (11.04.2025, nur net472) | … |"

---

**4. AixLib steht nicht unter „BSD 3-Clause", sondern unter einem geänderten 3-Klausel-BSD mit Zusatzabsatz**
(a) Kap. 5.1, Z. 675–676: „`AixLib.ThermalZones.ReducedOrder.Validation.VDI6007.TestCase1…12` (RWTH Aachen, E.ON ERC, / EBC; **BSD 3-Clause**)." (gleichlautend Kap. 10.1, Z. 1225: „(BSD 3-Clause, Copyright-Vermerk im Test)")
(b) Der Lizenztext von AixLib sagt selbst, es sei ein *überarbeiteter* 3-Klausel-BSD mit einem **zusätzlichen Absatz** (Rückfluss von Verbesserungen); es ist damit nicht die SPDX-Kennung `BSD-3-Clause`, und GitHub erkennt für das Repository überhaupt keine Standardlizenz (`spdx_id: null`).
(c) https://raw.githubusercontent.com/RWTH-EBC/AixLib/main/AixLib/UsersGuide/License.mo — „The license is a revised 3 clause BSD license with an ADDED paragraph at the end that makes it easy to accept improvements."; Copyright-Zeile aus `AixLib/UsersGuide/Copyright.mo`: „Copyright (c) 2010-2018, RWTH Aachen University, E.ON Energy Research Center, Institute for Energy Efficient Buildings and Indoor Climate."; https://api.github.com/repos/RWTH-EBC/AixLib (license = null)
(d) **mittel** (die Lizenz, die man mitliefern soll, ist nicht die genannte)
(e) Ersatztext: „… (RWTH Aachen, E.ON ERC, EBC; **überarbeitete 3-Klausel-BSD-Lizenz mit Zusatzabsatz zur Rückgabe von Verbesserungen**, Wortlaut in `AixLib/UsersGuide/License.mo` — nicht identisch mit SPDX `BSD-3-Clause`, deshalb ist der AixLib-Wortlaut selbst mitzuliefern, nicht eine BSD-Vorlage). Der mitzuführende Vermerk lautet wörtlich: ‚Copyright (c) 2010-2018, RWTH Aachen University, E.ON Energy Research Center, Institute for Energy Efficient Buildings and Indoor Climate.'"

---

**5. Die AixLib-Lizenz deckt die VDI-6007-Referenzzahlen selbst nicht ab**
(a) Kap. 5.1, Z. 676–677: „Die Tabellen dort geben die Normwerte wieder; die Richtlinie selbst / lag nicht vor."
(b) Genau das ist der Lizenzknoten: Die BSD-artige AixLib-Lizenz kann nur AixLib-eigenes Material freigeben; die Referenzwerte sind der Inhalt der VDI-Richtlinie 6007 Blatt 1 (VDI-Urheberrecht) — ihre Aufnahme in Testdateien eines verkauften Produkts ist lizenzrechtlich nicht aus AixLib abzuleiten, und das Papier zieht diesen Schluss stillschweigend.
(c) AixLib `TestCase1.mo` führt die Werte als eingebettete `CombiTimeTable` (`table=[0,22; 3600,22; … 5184000,50]`, Anmerkung `__Dymola_LockedEditing="Model from IBPSA"`): https://raw.githubusercontent.com/RWTH-EBC/AixLib/main/AixLib/ThermalZones/ReducedOrder/Validation/VDI6007/TestCase1.mo — Rechnung: Lizenzgeber kann nur Rechte einräumen, die er hält (nemo plus iuris).
(d) **mittel** (unbelegt/unklar, mit Haftungsfolge)
(e) Ergänzungssatz nach Z. 677: „Offen bleibt, ob die aus AixLib übernommenen **Zahlenwerte** von der AixLib-Lizenz gedeckt sind: Sie geben Tabellen der VDI 6007 Blatt 1 wieder, an denen der VDI das Urheberrecht hält; die BSD-artige Freigabe der RWTH kann daran keine Rechte einräumen. Vor der Abnahme ist entweder die Richtlinie zu beziehen und die Zitierfähigkeit der Prüfpunkte zu klären, oder die Referenzreihen sind nur als Prüfdatei im internen Testlauf zu führen und nicht auszuliefern (Frage für Q9 aufnehmen)."

---

**6. „Automation in Construction 2025" mit 64 %/48 % ist nicht auffindbar belegt**
(a) Kap. 7.3, Z. 1043–1046: „Erfahrungsbefund der Literatur (RWTH, Automation in Construction 2025): Zertifizierung / garantiert keine vollständigen Energiedaten; … 64 % der Interoperabilitätsstudien melden Geometriefehler, 48 % / Datenverlust."
(b) Weder Autor, Titel noch DOI sind genannt, und gezielte Suchen nach den beiden Prozentwerten in Verbindung mit der Zeitschrift führen zu keiner Arbeit — die Zahlen sind für einen Leser nicht nachprüfbar.
(c) Suchläufe ohne Treffer auf die Werte „64 %"/„48 %" in Automation in Construction 2025 (Zeitschriftenregister: https://www.sciencedirect.com/journal/automation-in-construction); das Papier nennt selbst keine Fundstelle.
(d) **mittel** (unbelegt — im selben Kapitel, das sonst jede Zahl mit Quelle führt)
(e) Ersatztext: „Erfahrungsbefund der Literatur (Autor, Titel, Automation in Construction, Band/Seite, DOI: …): …" — oder, falls die Quelle nicht wiederbeschafft wird: „Erfahrungsbefunde aus der Literatur (Quelle noch zu belegen, deshalb hier ohne Prozentangaben): Zertifizierung garantiert keine vollständigen Energiedaten; 2nd-Level-Geometrie kommt, die Gegenstücke in Nachbarzonen fehlen."

---

**7. `RefLatitude` hat drei *oder vier* Glieder — das vierte (Millionstel-Sekunden) fehlt, obwohl es Autorensysteme schreiben**
(a) Kap. 7.2, Z. 1024: „`IfcSite.RefLatitude/RefLongitude` (**Grad/Minuten/Sekunden-Tupel**, nicht Dezimalgrad) | zwei klassische Importfallen"
(b) Der Typ ist `IfcCompoundPlaneAngleMeasure` = `LIST [3:4] OF INTEGER`; das optionale vierte Glied sind Millionstel-Sekunden, und alle Glieder tragen dasselbe Vorzeichen — ein Leser, der starr drei Glieder erwartet, verliert Genauigkeit oder bricht ab.
(c) https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/lexical/IfcCompoundPlaneAngleMeasure.htm und https://standards.buildingsmart.org/IFC/RELEASE/IFC4/FINAL/HTML/schema/ifcmeasureresource/lexical/ifccompoundplaneanglemeasure.htm: „LIST [3:4] OF INTEGER"; viertes Glied „number of millionth-seconds", Bereich (−1 000 000, 1 000 000); Einheit stets Grad/Minute/Sekunde unabhängig von der Einheitenzuweisung
(d) **mittel** (fachlich unvollständig an einer Stelle, die ausdrücklich eine Importfalle benennt)
(e) Ersatztext: „`IfcSite.RefLatitude/RefLongitude` als `IfcCompoundPlaneAngleMeasure` = **`LIST [3:4] OF INTEGER`** (Grad, Minuten, Sekunden, optional Millionstel-Sekunden; alle Glieder mit gleichem Vorzeichen; Einheit immer Grad/Min/Sek unabhängig von `IfcUnitAssignment`), nicht Dezimalgrad — der Leser muss drei **und** vier Glieder beherrschen"

---

**8. Die gbXML-Aussage „keine auffindbare Lizenzangabe" wird als unproblematisch behandelt — rechtlich ist das der ungünstigere Fall**
(a) Kap. 7.7, Z. 1137–1140: „Aber: kein / ISO-Standard, keine auffindbare Lizenzangabe des Schemas … / **Ergänzung, nicht Ersatz — Stufe G5, reines XSD-Deserialisieren, 10–15 PT.**"
(b) Fehlende Lizenzangabe bedeutet nicht Freigabe; da Stufe G5 „reines XSD-Deserialisieren" heißt, wird das XSD (bzw. daraus erzeugter Code) im Produkt landen — das ist ohne Rechteklärung ein offener Punkt, den der Absatz nicht zieht.
(c) Schema-Stand bestätigt: https://www.gbxml.org/Schema_Current_GreenBuildingXML_gbXML — Version 8.01, Januar 2026; auf der Seite keine Lizenzaussage; Rechnung: ohne Rechteeinräumung gilt das gesetzliche Urheberrecht des Rechteinhabers.
(d) **mittel**
(e) Ersatztext: „Aber: kein ISO-Standard, **und zum Schema ist keine Lizenzangabe auffindbar — das heißt nicht ‚frei', sondern ungeklärt**; vor G5 ist beim gbXML-Konsortium eine schriftliche Nutzungserlaubnis für die Ablage des XSD bzw. des daraus erzeugten Codes im Produkt einzuholen (Aufwand oben nicht enthalten). Im DACH-Raum schwach verbreitet …"

---

**9. Die Bibliotheks-Empfehlung deckt die in derselben Tabelle versprochene Schemabreite nicht ab**
(a) Kap. 7.4, Z. 1070: „**Empfehlung:** `Xbim.Ifc4` + `Xbim.IO.MemoryModel` im Kern" — gegen Kap. 7.6 Nr. 2: „erkennt Schema (2x3 / 4 / 4.3)"
(b) `Xbim.Ifc4` deckt nur IFC4; für 2x3 und 4.3 sind die eigenständigen NuGet-Pakete `Xbim.Ifc2x3` und `Xbim.Ifc4x3` nötig (beide 6.1.605), die in der Tabellenzeile (Z. 1054) zwar genannt, in der Empfehlung aber weggelassen sind.
(c) https://api.nuget.org/v3-flatcontainer/xbim.ifc4x3/index.json (höchste Version 6.1.605); https://api.nuget.org/v3-flatcontainer/xbim.essentials/index.json (Metapaket, ebenfalls 6.1.605)
(d) **mittel** (innerer Widerspruch, führt bei G4a zu Nacharbeit)
(e) Ersatztext: „**Empfehlung:** `Xbim.Ifc2x3` + `Xbim.Ifc4` + `Xbim.Ifc4x3` + `Xbim.IO.MemoryModel` im Kern (oder das Metapaket `Xbim.Essentials` 6.1.605, wenn der Trimming-Nachweis für alle drei Schemata ohnehin geführt wird); `GeometryGymIFC_Core` (MIT) als Ausweg …"

---

**10. MPL-2.0 (web-ifc) fehlt im Ausschlusssatz, obwohl die Tabelle einen web-ifc-Pfad nennt**
(a) Kap. 7.4, Z. 1057 und 1068: „| ara3d/IFC-toolkit | Vorabversion | MIT | net8.0 | optional web-ifc-DLL | STEP | über C++ |" … „LGPL (IfcOpenShell) und GPL (IFC2SB) scheiden für den Kern aus."
(b) Der Ausweichpfad `ara3d/IFC-toolkit` bindet optional web-ifc ein; web-ifc steht unter MPL-2.0, ebenfalls Datei-Copyleft mit Quelltextpflicht — der Satz listet aber nur LGPL und GPL, so dass MPL als unproblematisch erscheint.
(c) https://registry.npmjs.org/web-ifc/latest (Version 0.0.77, `license: MPL-2.0`, Repository ThatOpen/engine_web-ifc); https://api.github.com/repos/ara3d/ifc-toolkit (MIT, letzter Push 16.05.2025)
(d) **mittel**
(e) Ersatztext: „LGPL (IfcOpenShell) und GPL (IFC2SB) scheiden für den Kern aus; **MPL-2.0 (web-ifc) ist ebenfalls Datei-Copyleft mit Quelltextpflicht und damit nur für den optionalen ara3d-Pfad zu prüfen, nicht für den Kern.**"

---

**11. „`IfcZone` (nicht hierarchisch)" gibt die Regel wieder, verschweigt aber die Ausnahme, die der Leser antrifft**
(a) Kap. 7.2, Z. 1019: „Zonen über `IfcZone` (nicht hierarchisch, Mehrfachzuordnung möglich)"
(b) Die Schemaregel (`WR1`/`WR2`) erlaubt als `RelatedObjects` ausdrücklich `IfcSpace`, `IfcZone` **und** `IfcSpatialZone` — Zonen in Zonen kommen also vor, obwohl der Erläuterungstext „may not be hierarchical" sagt; ein Leser, der Schachtelung nicht behandelt, zählt Flächen doppelt.
(c) https://raw.githubusercontent.com/buildingSMART/IFC4.3.x-development/master/docs/schemas/core/IfcProductExtension/Entities/IfcZone.md — „Only objects of type _IfcSpace_, _IfcZone_ and _IfcSpatialZone_ are allowed as _RelatedObjects_."
(d) **mittel**
(e) Ersatztext: „Zonen über `IfcZone` (Mehrfachzuordnung möglich; laut Text keine Hierarchie, **das Schema erlaubt als `RelatedObjects` aber auch `IfcZone`/`IfcSpatialZone` — Schachtelung ist also zu erkennen und beim Flächenaufsummieren zu entschachteln**)"

---

**12. TABULA/IWU-Vorgabewerte ohne Zenodo-Kennung und ohne Lizenz**
(a) Kap. 7.6 Nr. 4, Z. 1123–1124: „Was IFC nicht liefert, wird je `Baualtersklasse` vorbelegt (Typgebäude / TABULA/IWU, Zenodo 2025) und **sichtbar als Vorgabe markiert**."
(b) Es sind mehrere IWU-Zenodo-Datensätze aus 2025 im Umlauf; ohne DOI/Record und ohne Angabe der Datensatzlizenz ist weder klar, welche Tabellenwerte gemeint sind, noch ob sie in ein verkauftes Produkt eingebettet werden dürfen — dasselbe Lizenzthema wie bei AixLib, nur ungenannt.
(c) Beispielsatz „Tabellenwerte des Typgebäude-Modells zur energetischen Bewertung des Wohngebäudebestands (Typgebaeude-Tabellenwerte.xlsx)", https://zenodo.org/records/15488548 (Stein/Loga 2025); weitere IWU-Datensätze 2025 unter https://www.iwu.de/forschung/gebaeudebestand/repraesentative-typgebaeude/
(d) **mittel**
(e) Ersatztext: „… wird je `Baualtersklasse` aus dem Typgebäude-Modell des IWU vorbelegt (Stein/Loga 2025, Zenodo-Record **[DOI eintragen]**, Lizenz **[CC-… eintragen]**; die Lizenzbedingung ist wie bei AixLib im Setup zu nennen) und **sichtbar als Vorgabe markiert**."

---

**13. KIT-Nutzungsbedingung ungenau wiedergegeben und ohne den vorgeschriebenen Quellenwortlaut**
(a) Kap. 7.6 Nr. 6, Z. 1130: „33 U-Werte, Nutzung uneingeschränkt mit Namensnennung) als Importprobe unter" (ebenso Kap. 7.8 dreimal „frei mit Namensnennung")
(b) Die Quelle sagt „unrestricted use" und verlangt die Namensnennung ausdrücklich nur **für Veröffentlichungen** — außerdem schreibt sie den Wortlaut der Quellenangabe vor, den das Papier nicht wiedergibt.
(c) https://www.ifcwiki.org/index.php?title=KIT_IFC_Examples — „These examples are made by the Institute for Applied Computer Science (IAI) at the Karlsruhe Institute of Technology (KIT), and are for unrestricted use. If you use these examples for publications, you should provide the following source: Institute for Automation and Applied Informatics (IAI) / Karlsruhe Institute of Technology (KIT)"
(d) **niedrig**
(e) Ersatztext: „… Nutzung uneingeschränkt; bei Veröffentlichungen ist die Quelle im vorgegebenen Wortlaut zu nennen: ‚Institut für Automation und angewandte Informatik (IAI) / Karlsruher Institut für Technologie (KIT)' — dieser Satz gehört in den Quellenvermerk unter `Referenzlaeufe/Importproben/`."

---

**14. „alle Werte als `IfcPowerMeasure`" ist am Primärdokument nicht nachzuweisen; belegt ist es nur für die Luftwechselrate**
(a) Kap. 7.2, Z. 1026: „`Pset_SpaceThermalLoad` (alle Werte als `IfcPowerMeasure` typisiert, auch die Luftwechselrate — Schemafehler)"
(b) Nachweisbar ist, dass `AirExchangeRate` als `P_BOUNDEDVALUE`/`IfcPowerMeasure` definiert ist; die Allaussage „alle Werte" konnte an der Primärquelle nicht bestätigt werden (buildingSMART-Server antwortet auf die Pset-Seiten mit HTTP 403) und sollte deshalb auf das Belegte zurückgenommen werden.
(c) https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/lexical/Pset_SpaceThermalLoad.htm (Abschnitt 6.2.4.25) — `AirExchangeRate`: `P_BOUNDEDVALUE` / `IfcPowerMeasure`; direkter Abruf der Attributtabelle vom Server verweigert (403), Spiegel `vfkjsd.cn` nicht erreichbar
(d) **niedrig** (unscharf, nicht falsch)
(e) Ersatztext: „`Pset_SpaceThermalLoad` (durchgängig `P_BOUNDEDVALUE`; **auch `AirExchangeRate` ist als `IfcPowerMeasure` typisiert — Schemafehler, belegt in IFC 4.3.2 Abschn. 6.2.4.25**)"

---

**15. „Reference View 1.2 nennt sie nicht" ohne Fundstelle**
(a) Kap. 7.2, Z. 1029–1031: „**Space Boundaries sind in keiner Haupt-MVD Pflicht** (Reference View 1.2 nennt sie nicht; / Design Transfer View ist Entwurf; die eigene „Space Boundary Add-on View" ist ein Anhang zur / IFC2x3-Koordinationsansicht)."
(b) Die beiden letzten Teilaussagen sind belegbar, die erste (RV 1.2 nennt Space Boundaries nicht) ließ sich am Primärdokument nicht prüfen — der MVD-Server liefert auf die RV-1.2-Seiten HTTP 403; eine Fundstelle fehlt im Papier.
(c) Bestätigt aus https://github.com/buildingSMART/technical.buildingsmart.org/blob/main/MVD-Database.md: „IFC2x3 TC1 — Space Boundary Addon View 1.1" (Status final, Basis **IFC2x3**, nicht IFC4) und „IFC4 ADD2 TC1 — Design Transfer View 1.1" (Status **draft**); RV-1.2-Konzeptliste unter https://standards.buildingsmart.org/MVD/RELEASE/IFC4/ADD2_TC1/RV1_2/HTML/ nicht abrufbar (403)
(d) **niedrig** (unbelegt in der Form, inhaltlich plausibel)
(e) Ersatztext: „**Space Boundaries sind in keiner Haupt-MVD Pflicht**: Der Reference View 1.2 führt `IfcRelSpaceBoundary` nicht (MVD-Konzeptliste RV1_2, buildingSMART, Abruf 15.09.2026); der Design Transfer View 1.1 hat den Status *draft*; die ‚Space Boundary Addon View 1.1' ist eine Ergänzung zur **IFC2x3**-Koordinationsansicht (MVD-Datenbank buildingSMART, `MVD-Database.md`)."

---

**Geprüft und bestätigt:**
`Xbim.Ifc4`/`Xbim.Common`/`Xbim.IO.MemoryModel` **6.1.605**, Lizenz **CDDL-1.0**, Zielrahmen **net10.0, net8.0, netstandard2.0/2.1** (nuspec + Gallery-Frameworkliste, veröffentlicht 15.07.2026) · `Xbim.Essentials` existiert als Metapaket in 6.1.605 · `ExpressMetaData.cs` ruft tatsächlich `module.GetTypes()` (Trimming-Risiko korrekt benannt) · **GeometryGymIFC_Core 26.8.17, MIT**, Zielrahmen netstandard2.0/net6.0/net7.0/net8.0 · **Hypar.IFC4 1.2.0, MIT, net6.0**; `hypar-io/ifc-gen` letzter Push **06.11.2023** („seit 2023 ohne Pflege" trifft zu) · **IfcOpenShell LGPL-3.0**, keine gepflegte .NET-Anbindung · **web-ifc 0.0.77, MPL-2.0** · **BIMserver AGPL-3.0** · **gbXML-Schema 8.01, Januar 2026**, ohne Lizenzangabe auf der Schemaseite · **Design Transfer View = Entwurf**, **Space Boundary Addon View 1.1 = IFC2x3-Anhang** · **1st-Level-Raumgrenzen** laut Schema „cannot be directly used for thermal analysis" · **`Pset_SpaceThermalRequirements` ist in IFC 4.3 entfallen** (nicht mehr im Verzeichnis `docs/schemas/core/IfcProductExtension/PropertySets/`, ersetzt u. a. durch `Pset_SpaceHVACDesign`) · **`Pset_BuildingCommon.YearOfConstruction` ist Text** (`IfcLabel`) · **`TrueNorth` optional** und bei `IfcMapConversion` informativ · **`SolarHeatGainTransmittance` = g-Wert/Gesamtenergiedurchlassgrad** · **ISO 16739-1:2024 = IFC 4.3.2.0** · **KIT/IAI-Beispieldateien** frei nutzbar · **AixLib enthält die VDI-6007-Referenzreihen als eingebettete `CombiTimeTable`** in `Validation/VDI6007/TestCase*.mo` — die Aussage des Papiers dazu stimmt · **VDI/bS 2552 Blatt 11.9 „Bauphysik"** existiert.