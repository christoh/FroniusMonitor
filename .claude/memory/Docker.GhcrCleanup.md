---
paths:
  - .github/workflows/publish-images.yml
  - docker-compose.yml
  - docker-bake.hcl
  - HomeAutomationServer/Dockerfile
  - HomeAutomationClient/HomeAutomationClient.Browser/Dockerfile
---

# GHCR dangling-image cleanup

## OCI labels (added 2026-09-15)

Both Dockerfiles carry the `org.opencontainers.image.*` labels in their **runner** stage - `source`, `url`,
`documentation`, `title`, `description`, `licenses` (`AGPL-3.0-only`, the repo's LICENSE), `vendor`, `authors`,
`base.name`. `source` is what GitHub uses to attach a package to the repository. **Multi-arch caveat:** a
`LABEL` ends up on each platform manifest, while GitHub reads the *index* the tag points at. That is why the
builds moved out of `docker-compose.yml` into **`docker-bake.hcl`** (same day): its two targets carry the
platforms, the tags (`REGISTRY`/`TAG` variables, defaults `ghcr.io/christoh` and `latest`) and
`annotations = index_annotations(title, description)`, a bake function that yields the `index:`-prefixed OCI
annotations, so `docker buildx bake --push` annotates the index on every push without a flag to remember.
Compose's `build:` has no `annotations` key and only runs the images now. `docker buildx bake --print`
validates the file without building. The values exist twice on purpose - in the Dockerfiles for anyone who
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
