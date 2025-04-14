#!/bin/env bash
find . -xdev \( -name Dockerfile -o -name docker-compose.yml \) -exec dos2unix "{}" \;
