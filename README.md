# Immich Sync

Immich Sync is a Windows application designed to synchronize folders between your local machine and a remote Immich server. It provides flexible sync options, queue management, and user-configurable settings for seamless file transfers.

![Immich Sync Screenshot](https://media.discordapp.net/attachments/994044917355663450/1390798764616450288/image.png?ex=686991c9&is=68684049&hm=f16b501ca4948e8ea0c14a94743a7be607e8f5745d290f3013e51762862f5a07&=&format=webp&quality=lossless&width=907&height=544)

### Disclaimer:
This code is held together with hope, duct tape, and maybe a few sticks. I'm not a pro developer—just someone who needed a solution and decided to build something and share it.
If you're more experienced with coding, have suggestions, or want to help improve the project, please feel free to jump in! Just keep in mind, I don’t have a lot of free time, so I might not be able to fix or implement everything myself.

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
