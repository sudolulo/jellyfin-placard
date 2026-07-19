import sys, json, time, urllib.request, urllib.error
from urllib.parse import quote
BASE="http://192.168.50.1:30013"; USER="flan"
GUID="b6f8e2a4-1c3d-4e5f-9a7b-2d4c6e8f0a1b"
MYURL="https://git.onetick.ninja/flan/jellyfin-placard/raw/branch/main/manifest.json"
H='MediaBrowser Client="poster-pin", Device="code", DeviceId="poster-pin-code-8e1f", Version="1.0"'
def call(p,m="GET",d=None,h=None,t=30):
    r=urllib.request.Request(BASE+p,data=d,method=m,headers=h or {})
    with urllib.request.urlopen(r,timeout=t) as x:
        b=x.read(); return json.loads(b) if b else None
def auth(pw):
    r=call("/Users/AuthenticateByName","POST",json.dumps({"Username":USER,"Pw":pw}).encode(),
           {"Authorization":H,"Content-Type":"application/json"})
    return {"Authorization":f'MediaBrowser Token="{r["AccessToken"]}"'}
pw=sys.stdin.readline().rstrip("\n"); TOK=auth(pw)
# 1. uninstall the manually-dropped plugin
try:
    call(f"/Plugins/{GUID}/1.0.0.0","DELETE",h=TOK); print("uninstalled manual copy")
except urllib.error.HTTPError as e:
    print("uninstall ->", e.code)
time.sleep(3)
# 2. install from the catalog (downloads + checksum-verifies the release zip)
q=f"version=1.0.0.0&assemblyGuid={GUID}&repositoryUrl={quote(MYURL, safe='')}"
try:
    call(f"/Packages/Installed/Placard?{q}","POST",h=TOK); print("catalog install queued")
except urllib.error.HTTPError as e:
    print("install ->", e.code, e.read()[:200])
time.sleep(10)
# 3. restart to activate
print("restart ...")
try: call("/System/Restart","POST",h=TOK,t=10)
except Exception: pass
time.sleep(12)
for _ in range(75):
    try: call("/System/Info/Public"); break
    except Exception: time.sleep(2)
time.sleep(4); TOK=auth(pw)
# 4. verify loaded + config reachable
plugins=call("/Plugins",h=TOK) or []
pl=[p for p in plugins if "placard" in p.get("Name","").lower()]
if pl:
    print("INSTALLED:", pl[0].get("Name"), pl[0].get("Version"), "status:", pl[0].get("Status"),
          "| canUninstall:", pl[0].get("CanUninstall"))
else:
    print("Placard NOT loaded after install")
try:
    cfg=call(f"/Plugins/{GUID}/Configuration",h=TOK)
    print("config OK; pins lines:", len((cfg or {}).get("PinnedSources","").splitlines()))
except urllib.error.HTTPError as e:
    print("config ->", e.code)
