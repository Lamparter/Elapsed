# `Riverside.ResumableUploads`

This package contains a reusable TUS client for resumable uploads over HTTP.
It uses [OwlCore.Storage](https://nuget.org/packages/OwlCore.Storage) as its file storage API, an abstraction that allows consumers to upload any file from any source resumably via TUS.
The client model is designed to allow wrapping the TUS protocol over HTTP clients, however it is possible to define a custom TUS protocol upload destination by fulfilling the contracts in the `ITusTransport` interface.
