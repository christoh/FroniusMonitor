# How the two images are built and pushed. docker-compose.yml only runs them.
#
#   docker buildx bake --push                 both images, every platform, pushed to ghcr.io with the index annotated
#   docker buildx bake server --load          one image for this machine's platform only, into the local daemon
#   docker buildx bake --print                what the above resolve to, without building anything
#   REGISTRY=ghcr.io/me TAG=test docker buildx bake --push
#
# The Dockerfiles carry the same values as LABELs, which land on each platform's manifest. In a multi-arch push the
# tag points at an index above those manifests, and it is the index GitHub reads for the package page (repository
# link, description, README), so the very same values go onto the index here as annotations. Compose has no place
# for that, which is why the builds live in this file.

variable "REGISTRY" {
  default = "ghcr.io/christoh"
}

variable "TAG" {
  default = "latest"
}

group "default" {
  targets = ["server", "client"]
}

# The OCI annotations of the image index, the "index:" prefix being what buildx needs to put them there.
function "index_annotations" {
  params = [title, description]
  result = [
    "index:org.opencontainers.image.title=${title}",
    "index:org.opencontainers.image.description=${description}",
    "index:org.opencontainers.image.source=https://github.com/christoh/FroniusMonitor",
    "index:org.opencontainers.image.url=https://github.com/christoh/FroniusMonitor",
    "index:org.opencontainers.image.documentation=https://github.com/christoh/FroniusMonitor/blob/master/README.md",
    "index:org.opencontainers.image.licenses=AGPL-3.0-only",
    "index:org.opencontainers.image.vendor=Christoph Hochstätter",
    "index:org.opencontainers.image.authors=Christoph Hochstätter",
  ]
}

target "server" {
  context    = "."
  dockerfile = "HomeAutomationServer/Dockerfile"
  tags       = ["${REGISTRY}/home-automation-server:${TAG}"]
  platforms  = ["linux/amd64", "linux/arm64", "linux/arm/v7"]
  annotations = index_annotations(
    "Home Automation Server",
    "Collects data from Fronius GEN24 inverters, Fronius Wattpilot chargers, AVM FRITZ! devices and Toshiba air conditioners, and serves it to the Home Automation Control Center clients."
  )
}

target "client" {
  context    = "."
  dockerfile = "HomeAutomationClient/HomeAutomationClient.Browser/Dockerfile"
  tags       = ["${REGISTRY}/home-automation-client:${TAG}"]
  platforms  = ["linux/amd64", "linux/arm64", "linux/arm/v7", "linux/386"]
  annotations = index_annotations(
    "Home Automation Control Center (browser client)",
    "The browser client of the Home Automation Control Center, an Avalonia WebAssembly app served by nginx."
  )
}
