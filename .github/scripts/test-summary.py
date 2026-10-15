"""Lee los .trx de dotnet test y deja un resumen en la página de la ejecución de GitHub Actions.
Cada test que falla aparece además como anotación de error, visible sin abrir el log."""
import glob, os, sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
rows, failures = [], []
for path in sorted(glob.glob(os.path.join(sys.argv[1], "**", "*.trx"), recursive=True)):
    root = ET.parse(path).getroot()
    counters = root.find("t:ResultSummary/t:Counters", NS)
    project = os.path.basename(path).split("_")[0]
    defs = {u.get("id"): u.get("name") for u in root.iterfind(".//t:UnitTest", NS)}
    if counters is not None:
        rows.append((project, counters.get("total"), counters.get("passed"), counters.get("failed")))
    for r in root.iterfind(".//t:UnitTestResult", NS):
        if r.get("outcome") == "Failed":
            msg = r.find("t:Output/t:ErrorInfo/t:Message", NS)
            text = (msg.text or "").strip().replace("\n", " ")[:600] if msg is not None else ""
            failures.append((r.get("testName") or defs.get(r.get("testId"), "?"), text))

lines = ["## Tests", "", "| Proyecto | Total | Pasan | Fallan |", "|---|---|---|---|"]
lines += [f"| {p} | {t} | {ok} | {ko} |" for p, t, ok, ko in rows]
if failures:
    lines += ["", "### Fallos", ""] + [f"- **{n}**: {m}" for n, m in failures]
summary = os.environ.get("GITHUB_STEP_SUMMARY")
if summary:
    with open(summary, "a", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
print("\n".join(lines))
for name, message in failures[:20]:
    print(f"::error title=Test fallido: {name}::{message}")
