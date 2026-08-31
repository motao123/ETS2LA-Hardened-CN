#!/bin/bash
set -e
# Linux needs its own lock evaluation: the committed lock files are
# Windows-neutral and Linux-only packages (e.g. SDL3) are OS-conditional.
dotnet restore ETS2LA.Linux.slnf -r linux-x64 --force-evaluate
# Build the entire project as release (to update plugins)
dotnet build ETS2LA.Linux.slnf -c Release --no-incremental --no-restore
# Then publish the UI project as a self-contained Linux x64 application
dotnet publish ETS2LA/ETS2LA.csproj -c Release -r linux-x64 --self-contained -o ./publish --no-restore
# Copy the assets folder to the publish dir
cp -r Assets ./publish
