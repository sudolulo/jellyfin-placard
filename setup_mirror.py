import sys, json, urllib.request, urllib.error
gitea_tok = sys.stdin.readline().strip()
gh_tok = sys.stdin.readline().strip()
GITEA="https://git.onetick.ninja/api/v1"; GH="https://api.github.com"
def api(base, path, tokhdr, method="GET", body=None, extra=None):
    d=json.dumps(body).encode() if body is not None else None
    h={"Authorization":tokhdr,"Content-Type":"application/json"}
    if extra: h.update(extra)
    r=urllib.request.Request(base+path, data=d, method=method, headers=h)
    with urllib.request.urlopen(r,timeout=30) as x:
        b=x.read(); return json.loads(b) if b else None
def gitea(p,m="GET",b=None): return api(GITEA,p,f"token {gitea_tok}",m,b)
def gh(p,m="GET",b=None): return api(GH,p,f"Bearer {gh_tok}",m,b,{"Accept":"application/vnd.github+json"})
TOPICS=["jellyfin","jellyfin-plugin","skiasharp","dotnet","media-server"]

# 1. create GitHub repo
try:
    repo=gh("/user/repos","POST",{"name":"jellyfin-placard",
        "description":"Jellyfin plugin: bake library names onto backdrops (Placard)",
        "private":False,"has_issues":True,"has_wiki":False,
        "homepage":"https://git.onetick.ninja/flan/jellyfin-placard"})
    print("github repo:", repo.get("full_name"))
except urllib.error.HTTPError as e:
    print("gh repo:", e.code, e.read()[:150].decode(errors='replace'))
# github topics
try:
    gh("/repos/sudolulo/jellyfin-placard/topics","PUT",{"names":TOPICS}); print("gh topics set")
except urllib.error.HTTPError as e: print("gh topics:", e.code)

# 2. gitea push-mirror -> github
try:
    pm=gitea("/repos/flan/jellyfin-placard/push_mirrors","POST",{
        "remote_address":"https://github.com/sudolulo/jellyfin-placard.git",
        "remote_username":"sudolulo","remote_password":gh_tok,
        "sync_on_commit":True,"interval":"8h0m0s"})
    print("push-mirror:", pm.get("remote_address"), "sync_on_commit", pm.get("sync_on_commit"))
except urllib.error.HTTPError as e:
    print("push_mirror:", e.code, e.read()[:200].decode(errors='replace'))
# gitea topics
try:
    gitea("/repos/flan/jellyfin-placard/topics","PUT",{"topics":TOPICS}); print("gitea topics set")
except urllib.error.HTTPError as e: print("gitea topics:", e.code)

# 3. trigger sync
try:
    gitea("/repos/flan/jellyfin-placard/push_mirrors-sync","POST"); print("mirror sync triggered")
except urllib.error.HTTPError as e: print("sync:", e.code, e.read()[:150].decode(errors='replace'))
