Overview
========

This project is meant to be a replacement to [LSA](http://sourceforge.net/projects/lsa-svn/).

Prerequisites
=============

First, you need to checkout [this](https://lsa-svn.svn.sourceforge.net/svnroot/lsa-svn) Subversion repository since it contains all the assets required for the game.

**Building requires "assets", "interface" and "scripts" folders to be present in the 
solution directory**, so the directory layout is as follows:

	/<solution>/Demo
	/<solution>/GameEngine
	/<solution>/ImportTool
	/<solution>/FX
	/<solution>/assets
	/<solution>/scripts
	/<solution>/interface

Building
========

Requires MSBuild/Visual Studio on Windows, xbuild/MonoDevelop on Linux.
Minimum .NET Framework version is 4.0, and Mono 2.8 should be enough on Linux.

In solution directory, do the following:

	msbuild Calcifer.sln

or on Linux:

	xbuild Calcifer.sln
	
Keep in mind that on Linux it might be necessary to run the following line after build:

	mono ./bin/ImportTool.exe ../assets/test.map
	
After that, just run `bin/Demo.exe` (use Mono on Linux)

Getting Binaries
================

[Compiled binaries](https://www.dropbox.com/sh/699dl4tfmzd04cm/FgHLxyvrJ1) are also
available. However, you still need the repository mentioned in "Prerequisites" section.