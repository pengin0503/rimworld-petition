#!/usr/bin/env python3
"""Static checks for the mod's XML, run without the game.

Checks:
  * every XML file under About/, Common/ and LoadFolders.xml parses
  * GrievanceDef: required fields, known kind, no duplicate defNames
  * every Keyed key used in Source/ exists in each language, and no language
    has keys that English lacks
  * Japanese DefInjected entries point at existing defs and fields
With --core <RimWorld Data/Core/Defs>:
  * every thought referenced by a GrievanceDef exists in Core
  * Cessation grievances only use memory thoughts (durationDays > 0 or a
    Thought_Memory thoughtClass, following ParentName inheritance)

Usage: python3 -I tools/validate_mod_xml.py [--core PATH]
Exit code is non-zero when any check fails.
"""

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
GRIEVANCE_TAG = "Petitions.GrievanceDef"
GRIEVANCE_REQUIRED = ("defName", "label", "description", "demand")
GRIEVANCE_KINDS = {"Remedy", "Cessation"}
LANGUAGES = ("English", "Japanese")
# Thought classes that derive from Thought_Memory in Core.
MEMORY_THOUGHT_CLASSES = {
    "Thought_Memory", "Thought_MemorySocial", "Thought_FoodEaten", "Thought_MemoryObservation",
    "Thought_MemoryObservationTerror", "Thought_Banished", "Thought_MemoryRoyalTitle",
}

errors = []


def error(message):
    errors.append(message)


def parse(path):
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as exc:
        error(f"{path.relative_to(ROOT)}: XML parse error: {exc}")
        return None


def check_wellformed():
    files = [ROOT / "LoadFolders.xml", *sorted((ROOT / "About").rglob("*.xml")), *sorted((ROOT / "Common").rglob("*.xml"))]
    for path in files:
        parse(path)


def load_grievances():
    grievances = {}
    for path in sorted((ROOT / "Common" / "Defs").rglob("*.xml")):
        root = parse(path)
        if root is None:
            continue
        abstract_by_name = {d.get("Name"): d for d in root if d.tag == GRIEVANCE_TAG and d.get("Name")}
        for node in root:
            if node.tag != GRIEVANCE_TAG or node.get("Abstract") == "True":
                continue

            def field(name, node=node):
                current = node
                while current is not None:
                    value = current.find(name)
                    if value is not None:
                        return value
                    current = abstract_by_name.get(current.get("ParentName"))
                return None

            def_name = node.findtext("defName")
            for required in GRIEVANCE_REQUIRED:
                if field(required) is None or not (field(required).text or "").strip():
                    error(f"{GRIEVANCE_TAG} {def_name}: missing {required}")
            kind = field("kind").text if field("kind") is not None else "Remedy"
            if kind not in GRIEVANCE_KINDS:
                error(f"{GRIEVANCE_TAG} {def_name}: unknown kind {kind}")
            thoughts_node = field("thoughts")
            thoughts = [li.text.strip() for li in thoughts_node.findall("li")] if thoughts_node is not None else []
            events_node = field("historyEvents")
            events = [li.text.strip() for li in events_node.findall("li")] if events_node is not None else []
            if not thoughts and not events:
                error(f"{GRIEVANCE_TAG} {def_name}: no thoughts or historyEvents")
            if def_name in grievances:
                error(f"{GRIEVANCE_TAG} {def_name}: duplicate defName")
            grievances[def_name] = {"kind": kind, "thoughts": thoughts, "events": events}
    return grievances


def load_core_thoughts(core_defs):
    """Returns defName -> resolved dict of durationDays / thoughtClass, following ParentName."""
    nodes_by_name, defs = {}, []
    for path in sorted((core_defs / "ThoughtDefs").rglob("*.xml")):
        root = ET.parse(path).getroot()
        for node in root:
            if node.tag != "ThoughtDef":
                continue
            if node.get("Name"):
                nodes_by_name[node.get("Name")] = node
            if node.findtext("defName"):
                defs.append(node)

    def resolve(node, name):
        while node is not None:
            if node.find(name) is not None:
                return node.findtext(name)
            node = nodes_by_name.get(node.get("ParentName"))
        return None

    return {
        d.findtext("defName"): {"durationDays": resolve(d, "durationDays"), "thoughtClass": resolve(d, "thoughtClass")}
        for d in defs
    }


def check_thoughts(grievances, core_defs):
    core = load_core_thoughts(core_defs)
    core_events = set()
    for path in sorted(core_defs.rglob("*.xml")):
        for node in ET.parse(path).getroot():
            if node.tag == "HistoryEventDef" and node.findtext("defName"):
                core_events.add(node.findtext("defName"))
    for def_name, grievance in grievances.items():
        for event in grievance["events"]:
            if event not in core_events:
                error(f"{GRIEVANCE_TAG} {def_name}: history event {event} not found in Core")
        for thought in grievance["thoughts"]:
            info = core.get(thought)
            if info is None:
                error(f"{GRIEVANCE_TAG} {def_name}: thought {thought} not found in Core")
                continue
            if grievance["kind"] == "Cessation":
                is_memory = float(info["durationDays"] or 0) > 0 or info["thoughtClass"] in MEMORY_THOUGHT_CLASSES
                if not is_memory:
                    error(f"{GRIEVANCE_TAG} {def_name}: Cessation thought {thought} is not a memory")


def keyed_keys(language):
    keys = set()
    for path in sorted((ROOT / "Common" / "Languages" / language / "Keyed").rglob("*.xml")):
        root = parse(path)
        if root is not None:
            keys.update(child.tag for child in root)
    return keys


def check_keyed():
    used = set()
    for path in sorted((ROOT / "Source" / "Petitions").rglob("*.cs")):
        used.update(re.findall(r'"(Petitions_[A-Za-z0-9_]+)"\s*\.Translate\(', path.read_text(encoding="utf-8")))
    english = keyed_keys("English")
    for language in LANGUAGES:
        keys = keyed_keys(language)
        for key in sorted(used - keys):
            error(f"Keyed {language}: missing key {key} used in Source")
        for key in sorted(keys - english):
            error(f"Keyed {language}: key {key} does not exist in English")
    for key in sorted(english - used):
        error(f"Keyed English: key {key} is not used in Source")


def check_def_injected(grievances):
    injected_root = ROOT / "Common" / "Languages" / "Japanese" / "DefInjected"
    def_fields = {}
    for path in sorted((ROOT / "Common" / "Defs").rglob("*.xml")):
        root = parse(path)
        if root is None:
            continue
        for node in root:
            name = node.findtext("defName")
            if name:
                def_fields[(node.tag, name)] = {child.tag for child in node}
    for folder in sorted(p for p in injected_root.iterdir() if p.is_dir()):
        for path in sorted(folder.rglob("*.xml")):
            root = parse(path)
            if root is None:
                continue
            for child in root:
                def_name, _, field = child.tag.partition(".")
                fields = def_fields.get((folder.name, def_name))
                if fields is None:
                    error(f"DefInjected {folder.name}/{path.name}: unknown def {def_name}")
                elif field.split(".")[0] not in fields:
                    error(f"DefInjected {folder.name}/{path.name}: {def_name} has no field {field}")
    for def_name in grievances:
        for field in ("label", "description", "demand"):
            found = False
            for path in (injected_root / GRIEVANCE_TAG).rglob("*.xml"):
                root = parse(path)
                if root is not None and root.find(f"{def_name}.{field}") is not None:
                    found = True
            if not found:
                error(f"DefInjected Japanese: {def_name}.{field} is not translated")


# Translated fields required for the mod's own defs, per def type.
REQUIRED_TRANSLATIONS = {
    "ThoughtDef": ("stages.0.label", "stages.0.description"),
    "HediffDef": ("label", "description"),
    "MainButtonDef": ("label", "description"),
}


def check_custom_def_translations():
    injected_root = ROOT / "Common" / "Languages" / "Japanese" / "DefInjected"
    for path in sorted((ROOT / "Common" / "Defs").rglob("*.xml")):
        root = parse(path)
        if root is None:
            continue
        for node in root:
            fields = REQUIRED_TRANSLATIONS.get(node.tag)
            def_name = node.findtext("defName")
            if not fields or not def_name:
                continue
            translated = set()
            for injected in (injected_root / node.tag).rglob("*.xml"):
                injected_xml = parse(injected)
                if injected_xml is not None:
                    translated.update(child.tag for child in injected_xml)
            for field in fields:
                if f"{def_name}.{field}" not in translated:
                    error(f"DefInjected Japanese: {node.tag} {def_name}.{field} is not translated")


def check_defof():
    """Every field of PetitionsDefOf must name a def defined in Common/Defs."""
    source = ROOT / "Source" / "Petitions" / "PetitionsDefOf.cs"
    if not source.exists():
        error("Source/Petitions/PetitionsDefOf.cs is missing")
        return
    defined = set()
    for path in sorted((ROOT / "Common" / "Defs").rglob("*.xml")):
        root = parse(path)
        if root is not None:
            defined.update((node.tag, node.findtext("defName")) for node in root if node.findtext("defName"))
    for def_type, name in re.findall(r"public static (\w+Def) (\w+);", source.read_text(encoding="utf-8")):
        if (def_type, name) not in defined:
            error(f"PetitionsDefOf.{name}: no {def_type} with that defName in Common/Defs")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--core", type=Path, help="Path to RimWorld Data/Core/Defs")
    args = parser.parse_args()

    check_wellformed()
    grievances = load_grievances()
    if args.core:
        check_thoughts(grievances, args.core)
    check_keyed()
    check_def_injected(grievances)
    check_custom_def_translations()
    check_defof()

    if errors:
        print("\n".join(errors))
        print(f"{len(errors)} problem(s) found.")
        return 1
    print(f"OK: {len(grievances)} grievance defs" + (", thoughts checked against Core" if args.core else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
