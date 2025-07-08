# ProgrWPF CMM Application

A modern WPF application designed for visualizing and managing Coordinate Measuring Machine (CMM) data workflows. This project demonstrates best practices in WPF development, including UI composition, data binding, and the separation of concerns through a service-oriented architecture.

## ✨ Features

- **Dynamic UI:** The user interface is built programmatically using factory classes, allowing for flexible and extensible layouts.
- **3D Model Visualization:** Displays a 3D model (`.glb`) of the part being measured.
- **CMM Data Loading:** Loads CMM measurement points from an XML file.
- **Measurement Simulation:** Simulates a CMM measurement process with real-time progress updates.
- **Interactive Results:** Displays measurement results in a hierarchical tree view, with status indicators (Passed, Failed, In Progress).
- **Data Export:** Exports measurement reports to both **PDF** and **CSV** formats.
- **Modern Architecture:** Follows SOLID principles by separating UI, business logic, and data into distinct layers.

## 🏗️ Project Structure

The project is organized into a clean, logical structure to promote maintainability and scalability.

```
ProgrWPF/
│
├── 📂 Animation/
│   └── GridLengthAnimation.cs      # Custom animation for grid panels.
│
├── 📂 Data/
│   ├── CmmPoint.cs                 # Model for a single CMM coordinate point.
│   ├── MeasurementResult.cs        # Model for the result of a measurement.
│   ├── CmmDataLoader.cs            # Logic for loading CMM points from XML.
│   └── PropertyItem.cs             # Helper class for displaying properties in the UI.
│
├── 📂 Models/
│   ├── Klassischer Leuchtturm.glb  # Sample 3D model for visualization.
│   └── Klassischer Leuchtturm.xml  # Sample CMM points data.
│
├── 📂 Services/
│   ├── MeasurementService.cs       # Handles all business logic for the measurement simulation.
│   └── ReportService.cs            # Handles the generation of PDF and CSV reports.
│
├── 📂 UI/
│   ├── RibbonFactory.cs            # Creates the top ribbon menu.
│   ├── LeftPanelFactory.cs         # Creates the left panel (CMM points list).
│   ├── CenterPanelFactory.cs       # Creates the center panel (3D viewer).
│   └── RightPanelFactory.cs        # Creates the right panel (measurement results and controls).
│
├── App.cs                          # Application entry point and startup logic.
├── MainWindow.cs                   # The main application window, orchestrates the UI panels.
├── Program.cs                      # Main program entry point.
└── ProgrWPF.csproj                 # The MSBuild project file.
```

## 🧠 Core Logic & Architectural Decisions

### UI Factories (Code-First UI)

Instead of using extensive XAML files, the UI is constructed programmatically within the `UI` factory classes. This approach was chosen to:
- **Promote Reusability:** Each panel is a self-contained component.
- **Enable Dynamic Layouts:** Makes it easier to add, remove, or reconfigure UI elements at runtime.
- **Simplify Complex Views:** Avoids deeply nested and complex XAML, keeping the view logic clean and manageable in C#.

### Service-Oriented Architecture

The core business logic is encapsulated in services found in the `/Services` directory.
- `MeasurementService`: Manages the state of the measurement process (Start, Pause, Stop, Repeat). It runs the simulation asynchronously and raises events to notify the UI of progress and completion. This decouples the simulation logic from the UI that displays it.
- `ReportService`: Contains all the logic for creating report documents (PDF and CSV). It takes the measurement data and a file path, and handles the entire document generation and saving process. This isolates the complexity of file formatting and I/O from the rest of the application.

This separation of concerns (SoC) makes the application more robust, easier to maintain, and simpler to test.

## 🚀 Getting Started

### Prerequisites

- **.NET Framework 4.8.1:** Ensure the [.NET Framework 4.8.1 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481) is installed.
- **Visual Studio:** Visual Studio 2022 or later is recommended, with the **.NET desktop development** workload installed.

### Installation & Running

1. **Clone the repository:**
   ```sh
   git clone <repository-url>
   ```
2. **Open the solution:**
   - Navigate to the project directory and open `ProgrWPF.sln` in Visual Studio.
3. **Restore NuGet Packages:**
   - Visual Studio should automatically restore the required NuGet packages (PDFsharp, etc.) when you open the solution. If not, right-click the solution in the Solution Explorer and select "Restore NuGet Packages".
4. **Build the solution:**
   - Press `Ctrl+Shift+B` or go to `Build > Build Solution`.
5. **Run the application:**
   - Press `F5` or click the "Start" button in Visual Studio.

## 📖 Usage

1. **Load Data:**
   - Click the **"Load Points"** button in the top ribbon. This will load the sample CMM data from `Models/Klassischer Leuchtturm.xml` and display the points in the left panel.
2. **Run Simulation:**
   - Use the **▶ (Start)**, **⏸ (Pause)**, and **⏹ (Stop)** buttons in the right panel to control the measurement simulation.
   - The progress of the measurement is shown in the status bar at the bottom.
3. **View Results:**
   - As the simulation runs, the results for each point will appear in the right-hand panel.
   - The status of each point is indicated by a color:
     - **Green:** Passed (deviation is within tolerance)
     - **Red:** Failed (deviation exceeds tolerance)
     - **Orange:** In Progress
     - **Gray:** Not Measured
4. **Export Report:**
   - Click the **"Export"** menu in the right panel.
   - Select **"Export as PDF"** or **"Export as CSV"** to save a report of the measurement results.
