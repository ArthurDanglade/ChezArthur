import re
from pathlib import Path

prefab = Path(r"c:\Users\arthu\Chez Arthur\Assets\_Project\Prefabs\UI\CharacterDetailPopup.prefab")
text = prefab.read_text(encoding="utf-8")

KNOWN = {
    "fe87c0e1cc204ed48ad3b37840f39efc": "Image",
    "1344c3c82d62a2a41a3576d8abb8e3ea": "RawImage",
    "f4688fdb7df04437aeb418b961361dc5": "TextMeshProUGUI",
    "4e29b1a8efbd4b44bb3f3716e73f07ff": "Button",
    "d01df344a6c6b1c4e8c0795bf62e1449": "CharacterDetailPopup",
    "4aa36f3892133dc4387d54fe230f7dd3": "CharacterArtworkView",
    "ec3336c719fca9f4da9ba60420fb4f84": "PassiveEntryUI",
    "e19747de3f5aca642ab2be37e372fb86": "Outline",
    "59f8146938fff824cb5fd77236b75775": "VerticalLayoutGroup",
    "3245ec927659c4140ac4f8d17403cc18": "ContentSizeFitter",
    "30649d3a9faa99c48a7b1166b86bf2a0": "HorizontalLayoutGroup",
    "306cc8c2b49d9694b8250035e41a632b": "LayoutElement",
    "1aa08ab6e0800fa44ae55d278d1423e3": "ScrollRect",
    "3312d7739989d2b4e91e6319e9a96d76": "RectMask2D",
    "31a86e883b885294e8c45dfd1b46195a": "Mask",
}

objects = {}
for block in re.split(r"(?=^--- !u!)", text, flags=re.M):
    m = re.match(r"^--- !u!(\d+) &(\d+)\s*\n(.*)", block, re.S)
    if not m:
        continue
    objects[m.group(2)] = {"utype": m.group(1), "body": m.group(3)}

gos = {}
for oid, obj in objects.items():
    if obj["utype"] != "1":
        continue
    body = obj["body"]
    name = re.search(r"m_Name: (.+)", body).group(1).rstrip()
    gos[oid] = {"name": name, "comp_types": [], "script_guids": []}

rt_to_go = {}
unknown_script_guids = set()
for oid, obj in objects.items():
    ut, body = obj["utype"], obj["body"]
    go_m = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)
    go_id = go_m.group(1) if go_m else None
    if ut == "224":
        children = re.findall(r"- \{fileID: (\d+)\}", body.split("m_Father:")[0])
        father = re.search(r"m_Father: \{fileID: (\d+)\}", body).group(1)
        amin = re.search(r"m_AnchorMin: \{x: ([^,]+), y: ([^}]+)\}", body)
        amax = re.search(r"m_AnchorMax: \{x: ([^,]+), y: ([^}]+)\}", body)
        apos = re.search(r"m_AnchoredPosition: \{x: ([^,]+), y: ([^}]+)\}", body)
        size = re.search(r"m_SizeDelta: \{x: ([^,]+), y: ([^}]+)\}", body)
        pivot = re.search(r"m_Pivot: \{x: ([^,]+), y: ([^}]+)\}", body)
        info = {
            "children": children,
            "father": father,
            "amin": (amin.group(1), amin.group(2)),
            "amax": (amax.group(1), amax.group(2)),
            "apos": (apos.group(1), apos.group(2)),
            "size": (size.group(1), size.group(2)),
            "pivot": (pivot.group(1), pivot.group(2)),
        }
        if go_id in gos:
            gos[go_id]["rt_info"] = info
            gos[go_id]["comp_types"].append("RectTransform")
            rt_to_go[oid] = go_id
    elif ut == "222" and go_id in gos:
        gos[go_id]["comp_types"].append("CanvasRenderer")
    elif ut == "225" and go_id in gos:
        gos[go_id]["comp_types"].append("CanvasGroup")
    elif ut == "114" and go_id in gos:
        sm = re.search(r"m_Script: \{fileID: 11500000, guid: ([a-f0-9]+)", body)
        guid = sm.group(1) if sm else None
        tname = KNOWN.get(guid)
        if not tname:
            unknown_script_guids.add(guid)
            tname = f"Script({guid})"
        gos[go_id]["comp_types"].append(tname)
        gos[go_id]["script_guids"].append(guid)


def fmt_rt(info):
    if info["amin"] == ("0", "0") and info["amax"] == ("1", "1"):
        return "stretch"
    return (
        f"anchorsMin={info['amin']} anchorsMax={info['amax']} "
        f"pivot={info['pivot']} anchoredPos={info['apos']} sizeDelta={info['size']}"
    )


def print_tree(go_id, depth=0):
    go = gos[go_id]
    indent = "  " * depth
    types = ", ".join(go["comp_types"])
    rt = fmt_rt(go["rt_info"])
    print(f"{indent}- {go['name']} | [{types}] | RT: {rt}")
    for child_rt in go["rt_info"]["children"]:
        if child_rt in rt_to_go:
            print_tree(rt_to_go[child_rt], depth + 1)


print("=== HIERARCHY ===")
for gid, g in gos.items():
    if g["rt_info"]["father"] == "0":
        print_tree(gid)

print("\n=== KEY SCRIPTS ===")
for gid, g in gos.items():
    for t in g["comp_types"]:
        if t in ("CharacterDetailPopup", "CharacterArtworkView"):
            print(f'{t} on GO "{g["name"]}" fileID={gid}')

print("\nUNKNOWN SCRIPT GUIDS:")
for g in sorted(x for x in unknown_script_guids if x):
    print(g)

print("\nALL NON-KNOWN ASSET GUIDS:")
for g in sorted(set(re.findall(r"guid: ([a-f0-9]{32})", text))):
    if g not in KNOWN and g != "0000000000000000f000000000000000":
        print(g)

print("\nBUILTIN SPRITES:")
for m in re.finditer(
    r"m_Sprite: \{fileID: (-?\d+), guid: 0000000000000000f000000000000000", text
):
    print(" builtin fileID", m.group(1))

print("NULL SPRITES count:", len(re.findall(r"m_Sprite: \{fileID: 0\}", text)))

print("\nFONTS:")
seen = set()
for m in re.finditer(r"m_fontAsset: \{fileID: (\d+), guid: ([a-f0-9]+)", text):
    key = (m.group(1), m.group(2))
    if key not in seen:
        seen.add(key)
        print(m.group(1), m.group(2))

print("\nTEXTURES:")
for m in re.finditer(r"m_Texture: \{fileID: (-?\d+)(?:, guid: ([a-f0-9]+))?", text):
    print(m.group(1), m.group(2))

print("\nGO_COUNT", len(gos))
print("\nSPRITE REFS DETAIL:")
# find which GO has each sprite by scanning Image blocks
for oid, obj in objects.items():
    if obj["utype"] != "114":
        continue
    body = obj["body"]
    if "m_Sprite:" not in body:
        continue
    go_m = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)
    sm = re.search(
        r"m_Sprite: \{fileID: (-?\d+)(?:, guid: ([a-f0-9]+), type: (\d+))?\}", body
    )
    if go_m and sm:
        go = gos[go_m.group(1)]
        print(
            f'  GO "{go["name"]}": fileID={sm.group(1)} guid={sm.group(2)}'
        )
