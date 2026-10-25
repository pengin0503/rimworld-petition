#!/usr/bin/env python3
"""Builds the install ZIP: a single Petitions/ folder with only what RimWorld loads.

Contents: About/, LoadFolders.xml, Common/ (Defs, Languages) and 1.7/Assemblies/Petitions.dll.
Source, tests, docs, tools, game assemblies and 0Harmony.dll are never included.

Usage: python3 -I tools/package_mod.py [--output dist/Petitions-<version>.zip] [--version vX.Y.Z]
"""

import argparse
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FOLDER = "Petitions"
DLL = "1.7/Assemblies/Petitions.dll"
CONTENT = {"About": {".xml", ".png"}, "Common": {".xml", ".png"}}


def versions(root=ROOT):
    about = ET.parse(root / "About" / "About.xml").findtext("modVersion")
    project = ET.parse(root / "Source" / "Petitions" / "Petitions.csproj").findtext(".//Version")
    return about, project


def check_version(tag, root=ROOT):
    about, project = versions(root)
    if not about or about != project:
        raise ValueError(f"About modVersion {about!r} and Petitions.csproj Version {project!r} must match")
    if tag is not None and tag.lstrip("v").split("-")[0] != about:
        raise ValueError(f"tag {tag!r} does not match version {about}")
    return about


def files(root=ROOT):
    """Relative paths of every file that goes into the package, sorted."""
    result = ["LoadFolders.xml", DLL]
    for folder, suffixes in CONTENT.items():
        for path in sorted((root / folder).rglob("*")):
            if path.is_symlink():
                raise ValueError(f"symlink in mod content: {path}")
            if path.is_file() and path.suffix.lower() in suffixes:
                result.append(path.relative_to(root).as_posix())
    dll = root / DLL
    if not dll.is_file() or dll.read_bytes()[:2] != b"MZ":
        raise FileNotFoundError(f"{DLL} is missing; build Source/Petitions.slnx -c Release first")
    return sorted(result)


def package(output, root=ROOT):
    output = Path(output)
    output.parent.mkdir(parents=True, exist_ok=True)
    temp = output.with_suffix(output.suffix + ".tmp")
    try:
        with zipfile.ZipFile(temp, "w", zipfile.ZIP_DEFLATED) as archive:
            for name in files(root):
                info = zipfile.ZipInfo(f"{FOLDER}/{name}", date_time=(2026, 1, 1, 0, 0, 0))
                info.compress_type = zipfile.ZIP_DEFLATED
                info.external_attr = 0o100644 << 16
                archive.writestr(info, (root / name).read_bytes())
        temp.replace(output)
    finally:
        if temp.exists():
            temp.unlink()
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", help="release tag to check against the mod version")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    version = check_version(args.version)
    output = args.output or ROOT / "dist" / f"Petitions-{version}.zip"
    print(package(output))
    return 0


if __name__ == "__main__":
    sys.exit(main())
