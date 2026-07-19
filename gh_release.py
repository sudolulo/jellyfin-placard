import sys, os, json, time, hashlib, urllib.request, urllib.error
gh_tok=sys.stdin.readline().strip()
GH="https://api.github.com"; UP="https://uploads.github.com"
ZIP="/home/dev/jellyfin-placard/Jellyfin.Plugin.Placard/bin/Release/net9.0/placard_1.0.0.0.zip"
if not os.path.exists(ZIP):
    print("ZIP MISSING:", ZIP); sys.exit(1)
zbytes=open(ZIP,"rb").read()
print("zip:", len(zbytes), "bytes md5", hashlib.md5(zbytes).hexdigest())
def gh(base,path,method="GET",body=None,data=None,ctype="application/json"):
    if body is not None: data=json.dumps(body).encode()
    h={"Authorization":f"Bearer {gh_tok}","Accept":"application/vnd.github+json"}
    if data is not None: h["Content-Type"]=ctype
    r=urllib.request.Request(base+path,data=data,method=method,headers=h)
    with urllib.request.urlopen(r,timeout=90) as x:
        b=x.read(); return json.loads(b) if b else None
# wait for mirrored tag
ok=False
for _ in range(40):
    try: gh(GH,"/repos/sudolulo/jellyfin-placard/git/refs/tags/v1.0.0"); ok=True; break
    except urllib.error.HTTPError as e:
        if e.code==404: time.sleep(3)
        else: raise
print("tag v1.0.0 present on github:", ok)
# create (or fetch) release
try:
    rel=gh(GH,"/repos/sudolulo/jellyfin-placard/releases","POST",
           {"tag_name":"v1.0.0","name":"Placard 1.0.0","body":"Initial release.","draft":False,"prerelease":False})
except urllib.error.HTTPError as e:
    if e.code==422:
        rel=gh(GH,"/repos/sudolulo/jellyfin-placard/releases/tags/v1.0.0")
    else:
        print("release err:", e.code, e.read()[:200].decode(errors='replace')); raise
rid=rel["id"]; print("release id:", rid)
# upload asset (delete existing same-name first if present)
for a in rel.get("assets",[]):
    if a["name"]=="placard_1.0.0.0.zip":
        gh(GH,f"/repos/sudolulo/jellyfin-placard/releases/assets/{a['id']}","DELETE")
try:
    asset=gh(UP,f"/repos/sudolulo/jellyfin-placard/releases/{rid}/assets?name=placard_1.0.0.0.zip",
             "POST",data=zbytes,ctype="application/zip")
    print("ASSET_URL:", asset.get("browser_download_url"))
except urllib.error.HTTPError as e:
    print("asset upload:", e.code, e.read()[:200].decode(errors='replace'))
