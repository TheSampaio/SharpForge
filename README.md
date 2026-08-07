# Sharp Forge

Sharp Forge is a Windows desktop application for batch image upscaling with the Real-ESRGAN NCNN Vulkan runtime. It provides a focused queue-based workflow, live progress, safe cancellation, model discovery, and configurable GPU processing settings.

![Sharp Forge preview](SharpForge/Resources/Thumbnails/showcase-sharp-forge-01.jpg)

## Features

- Add multiple images through a file picker or drag and drop.
- Add all supported images located directly inside a dropped folder.
- Preview queued images without locking the source files.
- Discover valid Real-ESRGAN `.param` and `.bin` model pairs automatically.
- Control tile size, GPU ID, pipeline thread counts, and concurrent jobs.
- Save results to a model-specific directory or beside each source file with a suffix.
- Skip outputs that already exist.
- Cancel active processes without leaving incomplete output files behind.
- View per-file progress and a final completed, skipped, and failed summary.

Supported input extensions are BMP, GIF, JPEG, PNG, TIFF, and WebP. Real-ESRGAN supports PNG, JPEG, and WebP output through the application.

## Requirements

- Windows 10 or later.
- A Vulkan-capable GPU with a current graphics driver.
- The .NET 8 Desktop Runtime to run a framework-dependent build.
- The .NET 8 SDK or later to build from source.

## Development setup

1. Clone the repository.
2. Run `Build\Setup.bat` once. The script downloads the pinned Real-ESRGAN runtime into `SharpForge\Runtime\realesrgan`.
3. Build the solution:

   ```powershell
   dotnet build SharpForge.sln --configuration Debug
   ```

4. Run the application:

   ```powershell
   dotnet run --project SharpForge\SharpForge.csproj
   ```

You can also pass image or directory paths directly to the executable. This supports shortcuts and Windows **Open with** workflows.

The project copies the local runtime and model files to the build and publish output directories. Runtime binaries are intentionally excluded from Git.

## Usage

1. Select **Add images**, or drop supported images or a folder onto the preview area.
2. Select a queued image to inspect its preview.
3. Choose an AI model and output format.
4. Keep **Concurrent jobs** at `1` unless the GPU has enough memory for multiple Real-ESRGAN processes.
5. Reduce **Tile size** if Real-ESRGAN reports an out-of-memory error.
6. Select **Start upscaling**. The same button can safely cancel the active batch.

By default, output files are written to a directory named `_Upscaled (model-name)` beside each source directory. When the suffix option is enabled, each output is stored beside its source as `file-name (model-name).extension`.

## Architecture

The application keeps UI concerns separate from runtime and batch-processing concerns:

```text
SharpForge/
├── Configuration/   Runtime path configuration
├── Models/          Immutable options, progress, and result types
├── Services/        File discovery, model discovery, output paths, and processing
├── Source/          WinForms windows and interaction logic
└── Resources/       Application icons and images
```

`IImageUpscaler` is the process boundary. `RealEsrganUpscaler` implements that boundary with safely separated command-line arguments and process-tree cancellation. `UpscaleBatchProcessor` owns bounded concurrency, output directory creation, skip behavior, and progress aggregation. The main form captures one immutable settings snapshot before starting background work and only updates controls through UI-thread progress callbacks.

## Publishing

Install the local runtime first, then publish normally:

```powershell
dotnet publish SharpForge\SharpForge.csproj --configuration Release --runtime win-x64 --self-contained false
```

Verify that the publish output contains `realesrgan\realesrgan-ncnn-vulkan.exe` and the `realesrgan\models` directory before distributing it.

## Third-party software and assets

Sharp Forge uses [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN) for AI image upscaling. Real-ESRGAN is distributed under its own license.

The interface renders embedded [Lucide](https://lucide.dev/) SVG icons through [SVG.NET](https://github.com/svg-net/SVG). Lucide icons are distributed under the ISC License, included beside the SVG assets. SVG.NET is distributed under the Microsoft Public License.

The flag icons are attributed to their respective Flaticon creators:

- Brazil icon by High Quality Icons.
- United States icon by Freepik.
- European Union icon by Marcus Christensen.

Sharp Forge was also inspired by [Cupscale](https://github.com/n00mkrad/cupscale).

## License

Sharp Forge is licensed under the BSD 3-Clause License. See [LICENSE](LICENSE) for the complete terms.

Users are responsible for ensuring they have the legal right to process their source images. Image enhancement does not grant rights to copyrighted material.
