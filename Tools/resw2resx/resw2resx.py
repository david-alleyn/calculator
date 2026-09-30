#!/usr/bin/env python3
"""resw -> resx converter for the Calculator localization pipeline.

Reads each ``<locale>/*.resw`` pair under ``src/Calculator/Resources`` and emits
.NET ``.resx`` catalogs (Microsoft ResX v2.0 schema) that the SDK compiles into
``ResourceManager`` resources / satellite assemblies. Preserves the resource
``name`` key and its value; the resw ``<source>`` comment (if present) is carried
into the resx ``<comment>`` element.

Layouts:
  flat    (default) SDK-friendly: en-US becomes the neutral ``Resources.resx`` /
          ``CEngineStrings.resx`` and every other locale gets a culture suffix
          (``Resources.fr-FR.resx``) so ``AssignCulture`` infers the culture.
  locale  human-friendly: ``<out>/<locale>/Resources.resx`` per locale.

Usage:
    python3 Tools/resw2resx/resw2resx.py                         # all locales, flat
    python3 Tools/resw2resx/resw2resx.py --out build/lang/resx   # custom output
    python3 Tools/resw2resx/resw2resx.py --layout locale         # per-locale dirs
    python3 Tools/resw2resx/resw2resx.py --locales en-US he-IL   # subset
"""

import argparse
import os
import sys
import xml.etree.ElementTree as ET

NEUTRAL_LOCALE = "en-US"
CATALOG_NAMES = ("Resources", "CEngineStrings")

RESOURCES_DIR = os.path.normpath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "src", "Calculator", "Resources")
)

RESX_HEADERS = [
    ("resmimetype", "text/microsoft-resx"),
    ("version", "2.0"),
    (
        "reader",
        "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, "
        "Culture=neutral, PublicKeyToken=b77a5c561934e089",
    ),
    (
        "writer",
        "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, "
        "Culture=neutral, PublicKeyToken=b77a5c561934e089",
    ),
]


def parse_resw(path):
    """Return an ordered list of (name, value, comment) tuples."""
    tree = ET.parse(path)
    root = tree.getroot()
    entries = []
    for data in root.findall("data"):
        name = data.get("name")
        if name is None:
            continue
        value_el = data.find("value")
        comment_el = data.find("comment")
        value = value_el.text if value_el is not None and value_el.text is not None else ""
        comment = comment_el.text if comment_el is not None and comment_el.text is not None else ""
        entries.append((name, value, comment))
    return entries


def build_resx(entries):
    root = ET.Element("root")
    for name, value in RESX_HEADERS:
        resheader = ET.SubElement(root, "resheader", {"name": name})
        ET.SubElement(resheader, "value").text = value
    for name, value, comment in entries:
        data = ET.SubElement(root, "data", {"name": name, "xml:space": "preserve"})
        ET.SubElement(data, "value").text = value
        if comment:
            ET.SubElement(data, "comment").text = comment
    return root


def serialize(root):
    ET.indent(root, space="  ")
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


def convert_locale(locale, out_root, layout):
    src_dir = os.path.join(RESOURCES_DIR, locale)
    if not os.path.isdir(src_dir):
        print(f"  skip {locale} (missing source dir)", file=sys.stderr)
        return 0

    count = 0
    for catalog in CATALOG_NAMES:
        src = os.path.join(src_dir, catalog + ".resw")
        if not os.path.isfile(src):
            continue

        resx_root = build_resx(parse_resw(src))

        if layout == "flat":
            os.makedirs(out_root, exist_ok=True)
            file_name = catalog + ".resx" if locale == NEUTRAL_LOCALE else f"{catalog}.{locale}.resx"
            out_path = os.path.join(out_root, file_name)
        else:
            out_dir = os.path.join(out_root, locale)
            os.makedirs(out_dir, exist_ok=True)
            out_path = os.path.join(out_dir, catalog + ".resx")

        with open(out_path, "wb") as fh:
            fh.write(serialize(resx_root))
        count += 1
    print(f"  {locale}: {count} file(s)")
    return count


def find_locales():
    return sorted(
        d for d in os.listdir(RESOURCES_DIR)
        if os.path.isdir(os.path.join(RESOURCES_DIR, d))
    )


def main():
    parser = argparse.ArgumentParser(description="Convert Calculator .resw catalogs to .resx.")
    parser.add_argument("--out", default=os.path.join("build", "lang", "resx"),
                        help="output root directory (default: build/lang/resx)")
    parser.add_argument("--layout", choices=("flat", "locale"), default="flat",
                        help="flat emits SDK culture-suffixed files; locale emits per-locale dirs")
    parser.add_argument("--locales", nargs="*",
                        help="locale(s) to convert (default: all)")
    args = parser.parse_args()

    locales = args.locales or find_locales()
    total = 0
    for locale in locales:
        total += convert_locale(locale, os.path.normpath(args.out), args.layout)

    print(f"Converted {total} catalog(s) across {len(locales)} locale(s).")
    if total == 0:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
