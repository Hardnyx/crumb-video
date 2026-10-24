# Contributing

Crumb is in its initial setup stage. Start with the [roadmap](docs/roadmap.md) and [architecture](docs/architecture.md).

Keep changes focused. For a new feature, describe the user problem and expected behavior in an issue or pull request. Bug reports should include the platform, Crumb version when available, chosen size limit, and the error message. Do not upload private videos or logs containing personal paths without reviewing them first.

Use conventional commit messages, for example `feat: add video selection` or `fix: account for audio in size budget`. Keep code, identifiers, and public documentation in English. Update documentation when behavior changes.

Once the application exists, validate changes with the relevant build and checks. Compression changes should be exercised against real media, including a silent clip, portrait video, a path with spaces, and an input with audio. Verify the output's byte size and duration, preservation of the original, and cleanup after cancellation.

There is no build or test suite yet. Do not report unperformed checks as passing. Avoid committing downloaded tools, personal media, or generated release archives.
