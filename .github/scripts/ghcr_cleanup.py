#!/usr/bin/env python3
"""Delete the GHCR container versions no tag points at any more.

"Untagged" is NOT the criterion, and deleting on it breaks the images: a multi-arch push creates one tagged
index plus one untagged version per platform and per attestation, and the index references those by digest.
They are as essential as the tag. See .claude/memory/Docker.GhcrCleanup.md.

So the keep set is computed from the registry, not guessed: every tagged version, plus every digest any tagged
index references. Everything else is a leftover of an earlier build and is deleted.

Read-only unless DRY_RUN is false, and it refuses to delete at all if the keep set could not be established.
"""
import json
import os
import sys
import urllib.error
import urllib.request

API = "https://api.github.com"
REGISTRY = "https://ghcr.io"


def request(url, token, method="GET", accept="application/vnd.github+json"):
    req = urllib.request.Request(url, method=method)
    req.add_header("Accept", accept)
    req.add_header("Authorization", f"Bearer {token}")
    req.add_header("X-GitHub-Api-Version", "2022-11-28")
    with urllib.request.urlopen(req) as response:
        body = response.read()
        return response.status, response.headers, (json.loads(body) if body else None)


def registry_token(owner, package, token):
    """A pull token for the registry. GHCR takes the same token, base64 encoded, for this exchange."""
    import base64
    url = f"{REGISTRY}/token?scope=repository:{owner}/{package}:pull&service=ghcr.io"
    req = urllib.request.Request(url)
    if token:
        req.add_header("Authorization", "Basic " + base64.b64encode(f"x:{token}".encode()).decode())
    # Without a token the header is left off entirely rather than sent empty: GHCR answers an anonymous
    # request for a public package with 200, and the same request carrying "Basic x:" with 403.
    with urllib.request.urlopen(req) as response:
        return json.loads(response.read())["token"]


def manifest(owner, package, reference, pull_token):
    """The raw manifest of a tag or digest, whatever its media type."""
    url = f"{REGISTRY}/v2/{owner}/{package}/manifests/{reference}"
    req = urllib.request.Request(url)
    req.add_header("Authorization", f"Bearer {pull_token}")
    req.add_header("Accept", ", ".join([
        "application/vnd.oci.image.index.v1+json",
        "application/vnd.oci.image.manifest.v1+json",
        "application/vnd.docker.distribution.manifest.list.v2+json",
        "application/vnd.docker.distribution.manifest.v2+json",
    ]))
    with urllib.request.urlopen(req) as response:
        return json.loads(response.read())


def all_versions(owner, package, token):
    """Every version of the package. Paginated at 100; a package with years of CI behind it has thousands."""
    versions, page = [], 1
    while True:
        url = f"{API}/users/{owner}/packages/container/{package}/versions?per_page=100&page={page}"
        _, _, batch = request(url, token)
        if not batch:
            return versions
        versions.extend(batch)
        page += 1


def keep_set(owner, package, versions, token):
    """Digests that must survive: every tagged version, and everything a tagged index points at."""
    pull_token = registry_token(owner, package, token)
    keep = set()

    for version in versions:
        tags = (version.get("metadata") or {}).get("container", {}).get("tags") or []
        if not tags:
            continue
        keep.add(version["name"])
        # Read through the tag itself rather than the digest: that is what a user pulling it resolves.
        for tag in tags:
            for child in manifest(owner, package, tag, pull_token).get("manifests", []):
                keep.add(child["digest"])
    return keep


def delete(owner, package, version_id, token):
    """Own-user packages answer on /user; /users/{owner} is the admin spelling. Try both before giving up."""
    for url in (
        f"{API}/user/packages/container/{package}/versions/{version_id}",
        f"{API}/users/{owner}/packages/container/{package}/versions/{version_id}",
    ):
        try:
            request(url, token, method="DELETE")
            return True
        except urllib.error.HTTPError as error:
            if error.code not in (403, 404):
                raise
    return False


def clean(owner, package, token, dry_run):
    versions = all_versions(owner, package, token)
    keep = keep_set(owner, package, versions, token)

    if not keep:
        print(f"{package}: no tagged version resolved - refusing to delete anything", file=sys.stderr)
        return 1

    doomed = [v for v in versions if v["name"] not in keep]
    print(f"{package}: {len(versions)} versions, keeping {len(keep)}, deleting {len(doomed)}")

    if not doomed:
        return 0

    if dry_run:
        for version in doomed[:10]:
            print(f"  would delete {version['name'][:19]}... (id {version['id']})")
        if len(doomed) > 10:
            print(f"  ... and {len(doomed) - 10} more")
        return 0

    failed = 0
    for version in doomed:
        if not delete(owner, package, version["id"], token):
            failed += 1
            print(f"  could not delete id {version['id']}", file=sys.stderr)
    print(f"{package}: deleted {len(doomed) - failed}, failed {failed}")

    # The tag has to still resolve, or the cleanup broke the very thing it was protecting. Say that in one
    # line if it does not: a stack trace is the last thing wanted at the point where an image may be broken,
    # and the versions are restorable through the API for 30 days.
    try:
        pull_token = registry_token(owner, package, token)
        for version in versions:
            for tag in ((version.get("metadata") or {}).get("container", {}).get("tags") or []):
                manifest(owner, package, tag, pull_token)
                print(f"{package}: {tag} still resolves")
    except Exception as error:  # noqa: BLE001 - whatever went wrong, the operator has to be told plainly
        print(f"{package}: COULD NOT VERIFY the tags after deleting ({error}). GitHub can restore deleted "
              f"versions for 30 days - check the package before the next run.", file=sys.stderr)
        return 1

    return 1 if failed else 0


def main():
    token = os.environ["GH_TOKEN"]
    owner = os.environ["OWNER"]
    packages = [p.strip() for p in os.environ["PACKAGES"].split(",") if p.strip()]
    dry_run = os.environ.get("DRY_RUN", "true").lower() != "false"

    print("DRY RUN - nothing will be deleted\n" if dry_run else "DELETING\n")
    return max(clean(owner, package, token, dry_run) for package in packages)


if __name__ == "__main__":
    sys.exit(main())
