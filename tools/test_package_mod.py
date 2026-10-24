#!/usr/bin/env python3
"""Tests for tools/package_mod.py. Run: python3 -I -m unittest discover -s tools -p 'test_*.py'"""

import importlib.util
import tempfile
import unittest
import zipfile
from pathlib import Path

spec = importlib.util.spec_from_file_location("package_mod", Path(__file__).resolve().parent / "package_mod.py")
package_mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package_mod)


class PackageTests(unittest.TestCase):
    def mod(self, root, about="0.1.0", project="0.1.0", dll=b"MZ" + b"\0" * 64):
        for name, data in {
            "About/About.xml": f"<ModMetaData><modVersion>{about}</modVersion></ModMetaData>".encode(),
            "Source/Petitions/Petitions.csproj": f"<Project><PropertyGroup><Version>{project}</Version></PropertyGroup></Project>".encode(),
            "LoadFolders.xml": b"<loadFolders/>",
            "Common/Defs/a.xml": b"<Defs/>",
            "Common/Languages/Japanese/Keyed/b.xml": b"<LanguageData/>",
            "Source/Petitions/x.cs": b"class X {}",
            "docs/notes.md": b"notes",
            "tools/t.py": b"",
        }.items():
            path = root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        if dll is not None:
            (root / "1.7/Assemblies").mkdir(parents=True)
            (root / "1.7/Assemblies/Petitions.dll").write_bytes(dll)
            (root / "1.7/Assemblies/0Harmony.dll").write_bytes(b"MZ")

    def test_only_runtime_files_in_one_folder(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.mod(root)
            out = package_mod.package(root / "out.zip", root)
            with zipfile.ZipFile(out) as archive:
                self.assertEqual(sorted(archive.namelist()), sorted("Petitions/" + n for n in [
                    "About/About.xml", "LoadFolders.xml", "Common/Defs/a.xml",
                    "Common/Languages/Japanese/Keyed/b.xml", "1.7/Assemblies/Petitions.dll"]))
            first = out.read_bytes()
            package_mod.package(root / "out.zip", root)
            self.assertEqual(first, out.read_bytes())

    def test_missing_or_invalid_dll_creates_nothing(self):
        for dll in (None, b"not a dll"):
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                self.mod(root, dll=dll)
                with self.assertRaises(FileNotFoundError):
                    package_mod.package(root / "out.zip", root)
                self.assertFalse((root / "out.zip").exists())

    def test_versions_and_tag_must_match(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.mod(root)
            self.assertEqual(package_mod.check_version("v0.1.0", root), "0.1.0")
            self.assertEqual(package_mod.check_version("v0.1.0-test.1", root), "0.1.0")
            with self.assertRaises(ValueError):
                package_mod.check_version("v0.2.0", root)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.mod(root, project="0.2.0")
            with self.assertRaises(ValueError):
                package_mod.check_version(None, root)

    def test_repository_versions_match(self):
        about, project = package_mod.versions()
        self.assertEqual(about, project)


if __name__ == "__main__":
    unittest.main()
