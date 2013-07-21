Ketchup
===========

*Ketchup* is a mod for [Kerbal Space Program][ksp] which implements a programmable CPU.

Description
---------------
The CPU is an implementation of the [DCPU-16][dcpu] [spec][dcpu-spec], originally designed by
[Markus "Notch" Persson][notch] for the game [0x10c][0x10c]. Emulation of the CPU is provided by the
[Tomato][tomato] library. Implementations of the [monitor][monitor-spec], [keyboard][keyboard-spec], and
[clock][clock-spec] devices are also dervied from Tomato.

*Ketchup* is currently at the proof-of-concept stage, with *many* features and improvements to be made. Most
notably, there currently exist no devices capable of interacting with the vessel, so using the CPU for flight
control is not currently possible. However, any program written for the DCPU-16 should be able to run within
Kerbal Space Program.

Downloads
------------
Release information and downloads may be found on the GitHub [releases][releases] page.

Building
------------
In order to build *Ketchup* two assemblies are required from your Kerbal Space Program installation. Because
there are no obvious distribution licenses for these libraries, they cannot be commited to source control.

1. Make a `KSP` directory under the `Dependencies` directory.
2. From the `KSP_Data/Managed` directory under your Kerbal Space Program installation, copy the following files to
   the previously created directory: `Assembly-CSharp.dll` and `UnityEngine.dll`.
3. Execute `msbuild` (.NET) or `xbuild` (Mono) to build.

You must use the `Tomato.dll` found in the `Dependencies` directory, as it is modified to be usable by Kerbal
Space Program. Or you may build your own copy from the `ksp-compat` [branch][tomato-ksp-compat] of my Tomato fork.

Installation
----------------
If you built from source, navigate to the `Output/Debug` or `Output/Release` directory depending on the
configuration of your build. Copy the entire `Ketchup` directory to the `GameData` directory in your Kerbal Space
Program installation.

If you are installing from a pre-packaged distribution, simply extract the `Ketchup` directory to the `GameData`
directory in your Kerbal Space Program installation.

Usage
---------
Two new parts will appear in game under the "Pods" section of the craft editor: the **ENIAK-16S Computer** and
the **ENIAK-16L Computer**, these "Mechanical Kerbal Brains" replace the **RC-001S Remote Guidance Unit** and the
**RC-L01 Remote Guidance Unit** respectively. The computers will consume twice as much electricity as a remote
guidance unit however. Construct a rocket normally with one of the new parts.

When you're ready to launch you should see a new window with the name of the computer you added. In the text
field labelled "Memory Image" enter `helloworld.bin`  and then click the **PWR** button. You should now see
`Hello, world!` displayed on the monitor. You can write custom programs in DCPU-16 assembly and build them with an
assembler, such as [Organic][organic]. Copy the built binary to the `GameData/Ketchup/Plugins/PluginData/Ketchup`
directory, and you should be able to run it like `helloworld.bin`.

When a computer is powered on on a vessel, time warp is limited to 4x. Computers have a clock speed of 100KHz,
relative to game time (up to 400KHz relative to real time). Please check the GitHub [issue tracker][issues] for a
list of all current issues.

Name
--------
*Ketchup* is derived predominantly from the Tomato project, and its name begins with a *K* so... yeah.

[0x10c]: http://0x10c.com/
[clock-spec]: http://dcpu.com/clock/
[dcpu]: http://dcpu.com/
[dcpu-spec]: http://dcpu.com/dcpu-16/
[issues]: https://github.com/dbent/Ketchup/issues
[keyboard-spec]: http://dcpu.com/keyboard/
[ksp]: https://kerbalspaceprogram.com/
[monitor-spec]: http://dcpu.com/monitor/
[notch]: https://mojang.com/notch/
[organic]: https://github.com/SirCmpwn/organic
[releases]: https://github.com/dbent/Ketchup/releases
[tomato]: https://github.com/SirCmpwn/Tomato
[tomato-ksp-compat]: https://github.com/dbent/Tomato/tree/ksp-compat
