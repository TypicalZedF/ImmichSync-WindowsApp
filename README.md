# Immich Sync

Immich Sync is a Windows application designed to synchronize folders between your local machine and a remote Immich server. It provides flexible sync options, queue management, and user-configurable settings for seamless file transfers.

## Features

- **Sync Folders:** Specify multiple folders to sync with your Immich server.
- **Queue Management:** Upload, download, delete, rename, and move files with prioritized work units.
- **Authentication:** Securely store and encrypt your JWT token for server authentication.
- **Customizable Settings:** Configure server URLs, sync delay, concurrent uploads, and more.
- **Minimize to Tray:** Option to start minimized and control window behavior.
- **Pause/Resume Queue:** Ability to pause and resume the sync queue.

## Configuration

Settings are stored in a user-specific `settings.json` file under your AppData directory. You can configure:
- Local and remote server URLs
- Network ID
- Folders to sync
- Sync behavior options

## Requirements

- .NET 8.0 or later
- Windows OS

## Usage

1. Launch the application.
2. Configure your server URLs and authentication details.
3. Add folders you want to sync.
4. Start the sync process. The app will handle file operations and queue management automatically.

## License

This project is licensed under the MIT License.
