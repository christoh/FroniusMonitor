---
paths:
  - .github/scripts/ghcr_cleanup.py
  - .github/workflows/cleanup-packages.yml
  - .github/workflows/publish-images.yml
  - docker-compose.yml
  - docker-bake.hcl
  - Dockerfile
  - .dockerignore
---

# GHCR dangling-image cleanup

## One Dockerfile for both images (since 2026-09-22)

The two Dockerfiles (`HomeAutomationServer/Dockerfile`, `HomeAutomationClient/HomeAutomationClient.Browser/Dockerfile`)
were merged into **`Dockerfile` at the repository root**, because both published the browser client - the server
image hosts it at `/` since it got `PublishClientApp`. Stages: `shell-files` (scratch; `.profile` and the fastfetch
config both images share), `client-builder` (SDK + python3 + wasm-tools, publishes `HomeAutomationClient.Browser`
to `/client`), `client` (nginx runner), `server-builder` (plain SDK, no workload; `dotnet build`, then
`dotnet publish --no-build -p:PublishClientApp=true -p:ClientAppPrePublished=true -p:CopyClientWwwRoot=false
-p:ClientPublishDir=/client/` with the client bind-mounted from `client-builder` - a `COPY` would store the client
a second time in a layer of its own; that publish only copies the renamed manifest), `server` (last, so a bare
`docker build .` still builds the server). The bake targets select them with `target = "server"` / `"client"`.

**The client's wwwroot is one layer shared by both images:** both runners have the identical line
`COPY --link --from=client-builder /client/wwwroot /app/wwwroot`. `--link` builds the layer on its own instead of on
top of the image below, so its digest does not depend on the base image, the stage or the platform. That is why the
server's publish passes `CopyClientWwwRoot=false` (the csproj otherwise copies wwwroot into the publish dir, which
would put the files into the server image twice), and why the client image carries only wwwroot and no longer the
rest of the client's publish output (`dotnet.js`, `web.config`, runtimeconfig, manifest), which nginx never served.
The two lines must stay identical - a `--chmod`, a different path or a missing `--link` on one side makes two layers
again.

**The client image's nginx says how long a file may be cached** (since 2026-11-16): a name with a content hash
(`.<10 characters>.wasm`, `.dat`, `.js` - the assemblies, the native module, the runtime, ICU) is `immutable` for a
year, everything else - `index.html`, `main.js`, `dotnet.js`, Avalonia's `avalonia.js` and `storage.js`, a deep link
like `/powerflow` - `no-cache`, so the browser revalidates it and gets a 304 while it has not changed. Before, nginx
sent no `Cache-Control`, a browser kept the unhashed files by its own reckoning, and after the update to Avalonia
12.1.6 a Chrome ran the new assemblies with the cached `avalonia.js` of 12.1.4 and stopped at "Loading..."
(`Caniuse.isApplePlatform must be a Function but was undefined`). The server image needs nothing of the kind:
`MapStaticAssets` sends the headers itself.

**A copy already cached that way stays until the browser's own estimate runs out**, because the new headers only
reach it when the browser asks again. It is kept per host name: `home.hochstätter.de` failed while
`home.hochstaetter.de` worked in the same Chrome, with byte-identical files on the server. Neither Ctrl+Shift+R
(the runtime imports `avalonia.js` after the page has loaded, and that import may still come from the cache) nor
DevTools' "Clear site data" (cookies, storage and Cache Storage, not the HTTP cache) removed it; **"Empty cache
and hard reload"** - right-click on the reload button while DevTools are open - did.

Built once only because BuildKit merges identical vertices across the targets of one bake run: `client-builder`
is `--platform=$BUILDPLATFORM` and uses no `TARGET*` argument, so it is the same LLB for every platform and both
images. An ARG in it that differs per platform or per target, or a differing context/build-arg between the two
bake targets, silently brings back the double build. `.dockerignore` (`**/bin/`, `**/obj/`) replaced the
`rm -fr bin obj` steps both Dockerfiles had. Since 2026-10-24 `client-builder` rewrites the SDK image's apt sources
from `http://` to `https://` before `apt-get update` - see "Building and pushing from Claude Code on the web" below
for why. `.ashrc` and `/etc/motd` stay per image on purpose: they name the
image, and the server's has the `doas`/`root` aliases for its non-root `app` user.

## OCI labels (added 2026-09-15)

Both runner stages (`client`, `server`) of the Dockerfile carry the `org.opencontainers.image.*` labels -
`source`, `url`, `documentation`, `title`, `description`, `licenses` (`AGPL-3.0-only`, the repo's LICENSE),
`vendor`, `authors`, `base.name`. `source` is what GitHub uses to attach a package to the repository.
**Multi-arch caveat:** a `LABEL` ends up on each platform manifest, while GitHub reads the *index* the tag points at. That is why the
builds moved out of `docker-compose.yml` into **`docker-bake.hcl`** (same day): its two targets carry the
platforms, the tags (`REGISTRY`/`TAG` variables, defaults `ghcr.io/christoh` and `latest`) and
`annotations = index_annotations(title, description)`, a bake function that yields the `index:`-prefixed OCI
annotations, so `docker buildx bake --push` annotates the index on every push without a flag to remember.
Compose's `build:` has no `annotations` key and only runs the images now. `docker buildx bake --print`
validates the file without building. The values exist twice on purpose - in the Dockerfile for anyone who
runs `docker build`, in the bake file for the index - because a Dockerfile cannot read the bake file.

This repo publishes multi-arch Docker images to GitHub Container Registry
(`ghcr.io/christoh/home-automation-server`, `ghcr.io/christoh/home-automation-client`, see
`docker-compose.yml`). Only the `:latest` tag is ever pulled by users; every other version sitting in the
package is left over from a previous build.

**Since 2026-09-16 the push happens here**, in `.github/workflows/publish-images.yml`: a push to `master` runs
`docker buildx bake --push` on `ubuntu-latest`, authenticating to ghcr.io with the workflow's own
`GITHUB_TOKEN` and `packages: write`, so no personal access token is involved in publishing. The workflow
deliberately restates none of the platforms, tags or annotations - it runs the command `docker-bake.hcl`
documents, and that file stays the only place they are written down. `linux/arm64` and `linux/arm/v7` are
emulated through QEMU there, because GitHub has no native runner for them in this setup; `linux/386` runs on
the amd64 host unaided.

**The checkout fetches the whole history with its tags** (`fetch-depth: 0`, since 2026-10-24). GitInfo derives the
version from the last tag and the commits since; `actions/checkout`'s default of one commit without tags made it
`0.0.1` (base `0.0.0`, one commit) in every image the workflow had published until then - measured on a depth 1
clone of `dev`, where a full clone gave `1.42.15`. It went unnoticed until the About dialog showed the version.

### `permission_denied: write_package`, and the setting that fixes it

A pre-existing GHCR package does **not** accept a push from a repository's `GITHUB_TOKEN`, however the workflow
sets `permissions:`. Both packages here are user-owned (`/users/christoh/packages/...`) and predate the
workflow, so the first two runs failed at the export step with

```
failed to push ghcr.io/christoh/home-automation-server:latest: denied: permission_denied: write_package
```

even though the `docker/login-action` step had succeeded - logging in proves authentication, not the right to
write that package. `packages: write` grants the *token* a scope; the *package* still has to admit the
repository. The `org.opencontainers.image.source` label does not do it either: that links the package to the
repository for display and inherits its visibility, nothing more. Only a package created by a workflow gets
that access automatically, which these were not.

The fix is per package and only its owner can do it, in the UI - there is no API call and nothing in the
repository that can express it:

*`https://github.com/users/christoh/packages/container/<package>/settings` → Manage Actions access → Add
Repository → `FroniusMonitor` → Role **Write**.*

Done on 2026-09-16 for both packages; the run went green immediately afterwards. Watch out for the second one:
the server was granted first and the next run pushed the server and then failed on the client, leaving the two
images out of step until the client was granted too. **A new package needs this before its first push, or the
build runs to completion and is thrown away at the last step.**

**That makes this cleanup a recurring chore rather than a one-off.** Every run of the workflow publishes a new
index, and the previous build's index and all its children become untagged immediately - which is exactly the
litter the rest of this document is about. Nothing prunes them automatically. Deleting still has to happen
against the GitHub Packages API, and still needs a token with packages scopes, because `GITHUB_TOKEN` is
scoped to publishing and not to deletion.

### The cleanup runs itself now

`.github/workflows/cleanup-packages.yml` runs `.github/scripts/ghcr_cleanup.py` every Sunday, and on demand
from the Actions tab - a manual run reports without deleting unless `dry_run` is unticked, a scheduled one
deletes. It runs on `GITHUB_TOKEN`, so **no personal access token is
involved in this repository at all**, publishing or deleting. That works only because the developer set the
FroniusMonitor repository's role on both packages to **Admin** on 2026-09-16 (Package settings -> Manage
Actions access), not the Write that publishing needs: deleting a version takes admin on the package. The
secret `GHCR_CLEANUP_TOKEN` stays supported as a fallback - a classic PAT with `read:packages` and
`delete:packages`, used in preference when it is set - for the day a package this repository has no admin on
has to be cleaned. Normally it is unset.

The script implements the rule this document states rather than the tempting shortcut. It asks the registry
what each **tag** resolves to, keeps that index and every digest it references, and deletes only what is left
over. It never deletes on "untagged". It refuses to delete anything if no tagged version resolved, so a
registry hiccup cannot empty a package, and it re-reads every tag afterwards to prove the cleanup did not
break what it was protecting.

The publish workflow ignores `.github/scripts/**` and `cleanup-packages.yml` in its `paths-ignore`, so
changing the cleanup no longer rebuilds and republishes both images - which it did twice on 2026-09-16 before
the entries were added, each time leaving another 16 versions for the cleanup to collect. `paths-ignore` is
evaluated per push and not per file, so a push that touches one of these *and* anything else still publishes,
which is what you want.

**The two workflows share the concurrency group `ghcr-packages`, and must keep sharing it.** The cleanup
decides what to keep by reading what the tags resolve to; an index published between that read and the
deletes is in neither the keep set nor a tag the cleanup knows about, so it would be deleted as a leftover
minutes after being published, breaking `:latest`. The window is small and the schedules rarely collide,
which is exactly what makes it the kind of fault that turns up once, in production, and is never reproduced.
It was noticed on 2026-09-16 because the first dry run happened to overlap a publish.

Checked against the live packages on 2026-09-16: the keep sets it computes are 7 for the server and 9 for the
client, the same numbers both manual cleanups arrived at. The delete path was exercised against a stubbed API
with fabricated leftovers mixed into the real keep set; only the leftovers went.

## Building and pushing from Claude Code on the web

Done for the first time on 2026-10-24, the client image and then the server image, from `dev` and on the
developer's instruction (the workflow only ever publishes from `master`). Docker is installed in the cloud container but not running, and
every network access goes through the session's HTTPS proxy, which gives these traps:

- **Start the daemon yourself**, with the proxy in its environment: `HTTPS_PROXY=$HTTPS_PROXY dockerd &`. It comes
  up with the containerd image store, so the plain `docker` driver builds all four platforms and pushes the index
  with the bake file's annotations; no `docker-container` builder is needed.
- **Mount `binfmt_misc` on the host before registering QEMU**: `mount -t binfmt_misc binfmt_misc
  /proc/sys/fs/binfmt_misc`, then `docker run --privileged --rm tonistiigi/binfmt --install arm64,arm,386`. Without
  the mount the registration is gone as soon as that container exits, and every arm container dies with
  `exec format error`.
- **`RUN` reaches the network only through the proxy on the host's loopback**, so the build needs host networking
  and the proxy as build arguments. A bake override in a scratch file does it without touching the repository:
  `target "client" { network = "host"  args = { HTTPS_PROXY = "$HTTPS_PROXY", https_proxy = "$HTTPS_PROXY" } }`,
  run as `docker buildx bake --allow network.host -f docker-bake.hcl -f <override> client --push`. **Write the
  override from `$HTTPS_PROXY` every time, never a port copied from earlier**: the port is the session's and changes
  when the session is resumed. A stale one fails the first `RUN` with `Connection refused` on the loopback, which
  is how the first server build of 2026-10-24 died. The predefined proxy arguments are not written into the image
  history. No proxy CA was needed inside the build: apk, NuGet and the Ubuntu archive over HTTPS all went through
  without it.
- **A resumed session has lost the daemon and the QEMU registration** - both live in the container, not in the
  repository. Start `dockerd` and register QEMU again, as above, before building.
- **The proxy refuses plain HTTP** (`405 Method Not Allowed`), and the SDK image's apt sources are
  `http://archive.ubuntu.com` - hence the HTTPS rewrite in `client-builder`. The archive is public and signed, so
  HTTP was fine on its own; the developer had left it on purpose and agreed to the change for this reason.

Two more things before the push:

- **The cloud clone is shallow and has no tags**, so GitInfo computes a version like `0.0.111`.
  `git fetch --unshallow --tags origin` first; on 2026-10-24 that gave `1.42.9` plus the commits since.
- **The session's `GH_TOKEN` may push both packages**: `printf '%s' "$GH_TOKEN" | docker login ghcr.io -u christoh
  --password-stdin`. It is short lived (hours), so log in right before the push.

**The server image does not start in the cloud container**, so check a pushed one by its files rather than by
running it (`docker run --rm --entrypoint sh <image> -c 'grep -l … /app/*.dll'`). The Modbus server binds `[::]:1502`
and the container has no IPv6, not even with `--network host`, so the process dies with `SocketException (97):
Address family not supported by protocol`. Before that it also needs a writable `/app/Settings.xml`: without one
mounted it tries to create a default file, which the non-root `app` user may not, so a probe has to run as root.
Neither is a fault of the image; in production `Settings.xml` is mounted and the host has IPv6.

### Where the client runs: amazon2

`amazon2.hochstaetter.de` is the EC2 instance named "Docker Host" in `eu-central-1`, reachable through SSM
(`AWS-RunShellScript`). **The developer moved to a new AWS account on 2026-11-27 and set the host up anew there.**
The AWS connector signs in to the new account; the old one, and its instance `i-0fcbdf4bd2de0dc19` (t4g.nano), are
out of reach. **Look the instance ID up rather than trusting one written down**: `DescribeInstanceInformation`,
whose `ComputerName` is `amazon2.hochstaetter.de` - `i-003d020f7b6f6ea99` on 2026-11-28 (t9g.small, Alpine 3.24,
aarch64, Docker 29.9, Compose v5.7).

Looked at through SSM on 2026-11-28, read only:

- **The client** is the service `home-automation-client` of `/home/christoh/src/kartellvz/docker-compose.yml`,
  container `home-automation-client.kartellvz`, with **`platform: linux/arm64`** - `linux/arm/v7` on the old host,
  so the push has to include arm64 now; the bake file has both - and `pull_policy: always`. **arm/v7 cannot run
  there at all**: the new instance has a Neoverse V3 CPU, which has no 32-bit (AArch32) mode (the developer,
  2026-11-28). Do not set a service on amazon2 back to `linux/arm/v7`, and do not take an arm/v7 image as a
  fallback for it. Rolling out should be,
  as before, `docker compose pull home-automation-client && docker compose up -d --no-deps home-automation-client`
  in that directory - not yet tried on this host. The image it ran then had no `RepoDigests`, so it had not been pulled there; compare a rollout by the
  digest of the image pulled, not with what ran before.
- The same compose file has `nginx.kartellvz` (certbot's certificates in its `letsencrypt` volume), `ntp`,
  `onedrive-mcp.kartellvz` and `whatsapp-mcp.kartellvz`, all running, every service on `linux/arm64`. Its
  `kartellvz.db` and `kartellvz.web` services had no containers at all.
- Other compose projects under `/home/christoh/docker/`: `claude` (`claude-runner`), `portainer`, `vlmcsd` (the KMS
  image from the new account's ECR), and `home-automation`, whose image is still the old account's ECR and which had
  no container. **The HA server does not run on amazon2.**
- **The network is as it was, except IPv6** (the developer, 2026-11-28): the VPC is set up exactly as before and
  amazon2 has the same private address in it, WireGuard is as before (so `172.31.15.1` through the tunnel, as the
  FRITZ!Box and reverse proxy documents say), and the IPv4 Elastic IP `3.77.190.163` was transferred to the new
  account. The IPv6 address could not be taken along: it is `2a05:d014:1dc2:6301:1b::2` now, `…:9eb:d01:1b::2` on
  the old host. The AAAA records of `amazon2.hochstaetter.de`, `fritzbox.hochstaetter.de` and
  `fritzbox.hochstätter.ch` already named the new one on 2026-11-28 (checked over DNS over HTTPS, as was the
  address on the instance's interface). An IPv6 address of amazon2 written down before that date is the old one.

**The compose files on that host hold secrets**; read the lines you need - the service names, `image`, `platform`,
`container_name`, `pull_policy` - rather than printing whole files.

**Old images are pruned there every night** (since 2026-11-28, on the developer's request). The registry cleanup
above deletes only versions in GHCR; what `docker compose pull` has brought to a host stays in that host's Docker
store, untagged, after a newer build takes the tag - 13 such images, 1.28 GB, had gathered on amazon2 within a day
of the move. `/etc/periodic/daily/docker-image-prune` runs `docker image prune -f` (images without a tag that no
container uses; tagged images are left alone) and logs what it reclaimed to syslog under the tag
`docker-image-prune`. `crond` was neither running nor in the default runlevel on the new host, so it was added and
started for this - which also makes the `logrotate` in the same directory run again, daily at 02:00 with the rest of
`/etc/periodic/daily`. Consequence: **there is no previous image to go back to**, neither on the host nor, after the
Sunday cleanup, in GHCR; going back means building the older commit again. The developer accepted that ("we can
build again any time"). So deleting the old image after a rollout is no longer a step of its own.

**The host's backup is a monthly AMI**, by the DLM policy "Intranet Gateway" (`IMAGE_MANAGEMENT`, instances tagged
`IntranetGateway=1`, which the new instance is; on the 1st at 03:00 UTC, one kept). Pruning does **not** make it
smaller: an EBS snapshot holds the blocks that were ever written, not what the file system calls used, and deleting
a file does not unwrite its blocks. Only a discard would, and the instance's disk has none
(`/sys/block/nvme0n1/queue/discard_max_bytes` is 0), so `fstrim` cannot hand anything back - tried on
2026-11-28: **`fstrim -va` exits 0 and prints nothing, which looks like success and is not**; `-a` silently skips a
file system whose device cannot discard. `fstrim -v /` says it: "the discard operation is not supported". The AMI
taken during the move on 2026-11-25 already had 13.9 GB of its 16 GB volume in its snapshot, with about 6 GB in
use. The developer says the AMI is stored compressed in S3; if that is what counts, blocks overwritten with zeros
would cost next to nothing where freed blocks keep their old, compressed and so incompressible, layer data.
Zero-filling the free space once would test it, by the next snapshot's size - not done, and it fills the disk while
it runs.

## Why "dangling" is not "untagged"

A naive read of "dangling images" is "anything without a tag" - that is wrong here and deleting on that basis
breaks `:latest`. Each multi-arch push creates:

- One version tagged `latest` - an OCI image *index* (manifest list).
- Several **untagged** versions immediately below it with the same timestamp: one per built platform
  (`linux/amd64`, `arm64`, `arm/v7`, ...) plus one per build attestation (`architecture/os: "unknown"`). These
  are referenced *by digest* from inside the `latest` index and are exactly as essential as the tag itself -
  removing them leaves `latest` pointing at manifests that no longer exist.

So "dangling" correctly means: any version **not reachable from any tag you intend to keep**. In practice, only
the newest build's own tag + its own children are safe to keep; every older build (all still-untagged versions
from earlier timestamps) is superseded and safe to delete in full.

**Before deleting anything**, confirm which versions the tag you're keeping actually references:

```
docker login ghcr.io -u <user> --password-stdin   # token needs at least read:packages
docker manifest inspect ghcr.io/<owner>/<package>:latest
```

This prints the OCI index with the `digest` of every child manifest. Cross-reference those digests against the
`name` field of `GET /users/{owner}/packages/container/{package}/versions` (each version's `name` *is* its
digest) to get the numeric version `id`s to keep. Do this per package - the platform/attestation counts differ
(the server image ships 3 platforms, the client image ships 4, so their "keep sets" are 7 and 9 versions
respectively, not the same number).

## API and auth

Deletion is `DELETE /users/{owner}/packages/container/{package}/versions/{id}` (or `/orgs/{org}/...` for an
org-owned package). Listing is the same path with `GET`, paginated at 100/page - a repo with frequent CI builds
can easily have 1000+ versions per package, so always paginate fully (`--paginate` with `gh api`, or loop pages)
before deciding what to keep.

The token needs **`read:packages` and `delete:packages`** scopes. A token already used for `git push` (repo
scope) is not enough, and neither is a default `gh auth login` (`repo`, `read:org`, `gist`, `admin:public_key`
by default, but no packages scopes). Add scopes to an existing session with:

```
gh auth refresh --hostname github.com -s read:packages,delete:packages
```

This must be run interactively (it opens a browser to grant the extra scopes) - it cannot be done from a
non-interactive/automated shell.

### Environment quirk observed on this machine

An AI agent's automated PowerShell sessions on this machine could **not** see credentials that `gh auth login`
had just stored (`gh auth status` reported "not logged in" even though the user's own interactive terminal
showed it logged in, and `cmdkey /list` proved the Windows Credential Manager entry existed). This looks like a
DPAPI/credential-store scoping difference between the interactive desktop session and whatever session
non-interactive automation runs under - it is not specific to any one agent or tool.

Workaround that worked: have the *user* run `gh auth refresh ...` and `setx GH_TOKEN (gh auth token)`
themselves (interactively, e.g. via a small `.ps1` script they double-click), which persists the token to
`HKCU:\Environment`. An automated session can then read it directly from the registry even though it can't read
gh's own keyring-backed store:

```powershell
$token = (Get-ItemProperty -Path 'HKCU:\Environment' -Name GH_TOKEN).GH_TOKEN
$env:GH_TOKEN = $token   # makes `gh` and `Invoke-RestMethod` calls in *this* process work
```

Note `setx` only writes the registry; it does not affect already-open processes, including the one that ran
`setx` - a *new* process must read it (hence reading straight from `HKCU:\Environment` instead of relying on
`$env:GH_TOKEN` being inherited).

## Practical notes from the one cleanup done so far

- Scale: ~1185 versions existed for `home-automation-server` (keep 7) and ~1712 for `home-automation-client`
  (keep 9) - almost all from historical CI builds. Deleting ~2880 versions sequentially via
  `Invoke-RestMethod ... -Method Delete` with an ~80ms pause between calls took a few minutes per package and
  hit zero rate-limit errors; no batching/parallelism was needed.
- GitHub retains deleted package versions for 30 days and they can be restored via the API in that window, which
  makes this a lower-risk operation than it first appears - still confirm the keep-set digests against
  `docker manifest inspect` before running any bulk delete, since a wrong keep-set breaks the tag immediately
  and is a worse failure mode than "still has old images."
- After deleting, re-run `docker manifest inspect ghcr.io/<owner>/<package>:latest` to confirm the tag still
  resolves before considering the cleanup done.
- Second cleanup, 2026-09-15, from Git Bash with `gh` (74 and 113 versions, 67 and 104 deleted, keep sets 7 and 9
  again): `gh api` takes `--hostname github.com`, not `-h` (which is help). Three traps cost a round each: Git
  Bash turns a leading `/` of the endpoint into `C:/Program Files/Git/...` on a DELETE (omit the slash or set
  `MSYS_NO_PATHCONV=1`), `jq` output written to a file has CRLF so ids read back carry a `\r` (`tr -d '\r'`),
  and `<(...)` process substitution does not work for `--slurpfile` (write a temp file). The token has to be
  refreshed interactively by the developer (`gh auth refresh -h github.com -s read:packages,delete:packages`);
  the automated shell saw the refreshed keyring token straight away this time, no registry detour needed.
