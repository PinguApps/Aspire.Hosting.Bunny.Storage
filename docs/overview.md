# Overview

This repo provides an Aspire hosting package and a runtime package.

- Local run: Azure Blob Storage emulator via Azurite.
- Deploy: Bunny Storage via Bunny Core API.
- App code: `IObjectStorage` and optional `IObjectStorageProvider`.

Bunny is not made to pretend to be Azure Blob Storage. The runtime abstraction is the compatibility layer.
