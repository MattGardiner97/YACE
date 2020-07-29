# YACE (Yet Another CHIP-8 Emulator)

A CHIP-8 emulator written in C#. The system is a .NET Standard 2.0 class library, meaning it can be used within any UI framework such as Windows Forms, WPF, or theoretically even a web browser using Blazor.

![](https://i.imgur.com/Ayg1b91.gif)


## Features
* Runs CHIP-8 ROMs with correct timing
* Correct audio
* Debug window
* Disassembler window
* Memory viewer

## Installation
A Windows Forms frontend is the current default for the project.

The installation process is as follows:
1. Clone the repository using Visual Studio 2019
2. Build
3. Run

### Manual Installation
YACE can be built manually using the following process:

**Clone the repository**

    git clone https://github.com/MattGardiner97/YACE.git

**Change to YACE directory**

    cd YACE

**Build and run using .NET Core CLI**

    dotnet run --project YACE_WinForms

## Running
A collection of public domain Chip-8 ROMs can be found [here](https://www.zophar.net/pdroms/chip8/chip-8-games-pack.html).
