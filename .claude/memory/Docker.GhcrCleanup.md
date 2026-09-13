---
paths:
  - docker-compose.yml
  - HomeAutomationServer/Dockerfile
  - HomeAutomationClient/HomeAutomationClient.Browser/Dockerfile
---

# GHCR dangling-image cleanup

This repo publishes multi-arch Docker images to GitHub Container Registry
(`ghcr.io/christoh/home-automation-server`, `ghcr.io/christoh/home-automation-client`, see
`docker-compose.yml`). Only the `:latest` tag is ever pulled by users; every other version sitting in the
package is left over from a previous build. There is no `.github/workflows` file in this repo - images are
built and pushed by an external pipeline - so nothing here creates or names these versions on our side, and
cleanup has to happen against the GitHub Packages API directly.

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
