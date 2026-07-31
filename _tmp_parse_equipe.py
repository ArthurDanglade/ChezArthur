import re
from pathlib import Path

path = Path(r"Assets/_Project/Scenes/Hub.unity")
text = path.read_text(encoding="utf-8")

gos = {}
# simpler parse: GameObject blocks
for m in re.finditer(
    r"--- !u!1 &(\d+)\nGameObject:(?:\n(?!---).*)*?  m_Component:\n((?:  - component: \{fileID: \d+\}\n)+)(?:(?!---).)*?  m_Name: (.*)",
    text,
):
    fid = m.group(1)
    comps = re.findall(r"fileID: (\d+)", m.group(2))
    name = m.group(3).strip()
    gos[fid] = {"name": name, "comps": comps, "children": [], "parent": None}

# Transform / RectTransform
for m in re.finditer(
    r"--- !u!(?:224|4) &(\d+)\n(?:Rect)?Transform:(?:\n(?!---).*)*?  m_GameObject: \{fileID: (\d+)\}(?:\n(?!---).*)*?  m_Father: \{fileID: (\d+)\}(?:\n(?!---).*)*?  m_Children:\n((?:  - \{fileID: \d+\}\n)*)",
    text,
):
    tfid, go, father, chblock = m.group(1), m.group(2), m.group(3), m.group(4)
    if go not in gos:
        continue
    gos[go]["tf"] = tfid
    gos[go]["father_tf"] = father
    gos[go]["child_tfs"] = re.findall(r"fileID: (\d+)", chblock)

tf2go = {g["tf"]: fid for fid, g in gos.items() if "tf" in g}
for fid, g in gos.items():
    ft = g.get("father_tf")
    if ft and ft != "0" and ft in tf2go:
        p = tf2go[ft]
        g["parent"] = p
        gos[p]["children"].append(fid)

# preserve child order from child_tfs
for fid, g in gos.items():
    if "child_tfs" not in g:
        continue
    ordered = []
    for ctf in g["child_tfs"]:
        if ctf in tf2go:
            ordered.append(tf2go[ctf])
    g["children"] = ordered


def dump(fid, indent=0, max_depth=12):
    if indent > max_depth:
        return
    g = gos[fid]
    print("  " * indent + f"- {g['name']}")
    for c in g["children"]:
        dump(c, indent + 1, max_depth)


roots = [fid for fid, g in gos.items() if g["name"] == "PageEquipe"]
print("=== PageEquipe hierarchy ===")
for r in roots:
    dump(r)

# CharacterDetailPopup
print("\n=== CharacterDetailPopup (top-level search) ===")
for fid, g in gos.items():
    if g["name"] == "CharacterDetailPopup":
        dump(fid, 0, 6)
        break
