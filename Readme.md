# ScreenCaptureBlocker

## Overview

ScreenCaptureBlocker is a Windows Forms application designed to analyze screen captures using the OpenAI API. It allows users to capture a portion of their screen, send it to OpenAI for analysis, and display the results in a separate window. Additionally, it attempts to prevent screen capture tools from capturing the analysis window itself.

## Features

- **Screen Capture Analysis:** Captures the screen and sends it to OpenAI for analysis.
- **OpenAI Integration:** Utilizes the OpenAI API to analyze the captured screen content.
- **Hotkey Activation:** Activates the screen capture and analysis via a hotkey (Ctrl+Alt+B).
- **Screen Capture Protection:** Attempts to prevent screen capture tools from capturing the analysis window.
- **Loading Indicator:** Displays a loading bar while the screen is being captured and analyzed.
- **Error Handling:** Provides informative error messages for API failures and other issues.

## Prerequisites

- [.NET Framework](https://dotnet.microsoft.com/en-us/download/dotnet-framework) (version compatible with Windows Forms applications)
- [OpenAI API Key](https://platform.openai.com/account/api-keys)

## Setup

1.  **Clone the repository:**

    ```bash
    git clone [repository URL]
    cd [repository directory]
    ```

2.  **Open the project in Visual Studio:**

    Open the `WinFormsApp1.sln` file in Visual Studio.

3.  **Add your OpenAI API Key:**

    Replace `"your-api-key"` with your actual OpenAI API key in the `Program.cs` file:

    ```csharp
    private const string OpenAI_API_Key = "your-api-key"; // Replace with your actual API key
    ```

4.  **Build the project:**

    Build the solution in Visual Studio to restore NuGet packages and compile the application.

## Usage

1.  **Run the application:**

    Start the application from Visual Studio or by running the executable file.

2.  **Activate screen capture and analysis:**

    Press `Ctrl+Alt+B` to capture the screen and initiate the analysis.

3.  **View the analysis:**

    The analysis results will be displayed in a separate window.

## Code Overview

- `MainForm.cs`: Contains the main application logic, including screen capture, OpenAI API integration, and UI management.
- `Program.cs`: Contains the entry point of the application.

### Key Components

- **Windows API Imports:**

  - `GetForegroundWindow`: Retrieves a handle to the foreground window.
  - `GetWindowText`: Retrieves the text of the specified window's title bar.
  - `SetWindowDisplayAffinity`: Controls whether a window is included in screen captures.
  - `RegisterHotKey`/`UnregisterHotKey`: Registers and unregisters a system-wide hotkey.

- **OpenAI API Integration:**

  - Uses `HttpClient` to send requests to the OpenAI API.
  - Converts the captured screen to a base64 string for transmission.
  - Parses the JSON response from the OpenAI API to extract the analysis results.

- **UI Management:**
  - Creates a separate form (`responseForm`) to display the analysis results.
  - Uses a `ProgressBar` to indicate loading status.
  - Handles hotkey presses to toggle the visibility of the analysis form.

## Troubleshooting

- **Hotkey Registration Error:**

  - If the hotkey (Ctrl+Alt+B) fails to register, ensure that no other application is using the same hotkey combination.

- **API Key Error:**

  - If the application displays an error message indicating an invalid API key, double-check that you have correctly entered your OpenAI API key in the `Program.cs` file.

- **Network Issues:**

  - Ensure that your computer has a stable internet connection to communicate with the OpenAI API.

- **Screen Capture Issues:**
  - If the screen capture fails, ensure that the application has the necessary permissions to access the screen.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues to suggest improvements or report bugs.

## License

This project is licensed under the [MIT License](LICENSE).
