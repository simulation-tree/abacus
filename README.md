# Abacus

A dog food project.

### Instructions

To run these samples:
1. clone this repo
2. run the `clone-dependencies.bat` file to pull other repos
3. open the `Abacus.slnx` solution or build the `simulator` folder

The sample can be chosen by modifying the `EntryPoint.cs` file to use a different program.

### Sample programs

- [`AbacusProgram`](program/Programs/AbacusProgram.cs)
  - fly around camera, with a mesh that modifies at runtime and dynamic text, and demos modifying the window
  - Escape or X = close the demo
  - G = set the text to a new guid
  - T = set the text to current date time
  - L = set the text to a random sample string
  - J = enable/disable the dummy renderer
  - O = lerp the vertices of the dummy renderer in a circle
  - E = change the sampled area of the texture that the dummy renderer uses
  - R = toggles window resizing
  - B = toggles borderless
  - F = toggles fullscreen
  - N = minimizes the window
  - M = maximizes the window
  - Arrow keys + B = resize the red square
  - Arrow keys + B + Alt = move the red square around
  - Arrow keys + Alt = resize the window
  - Arrow keys = move the window
  - Hold V = lerp the window to a preset location and size
- [`ButtonsAndRaycasting`](program/Programs/ButtonsAndRaycasting.cs)
  - fly around camera, and you test clicking on buttons
  - left click on the blue cube to push it around
  - clicking on the ui squares prints to log
- [`ControlsTest`](program/Programs/ControlsTest.cs)
  - demos the ui framework
  - a resizable virtual window, that when closed closes the demo
  - scrollbars
  - buttons
  - text fields
  - toggle boxes
  - dropdowns
  - hierarchy tree menu
  - right click menu
- [`DesktopPlatformer`](program/Programs/DesktopPlatformer.cs)
  - a 2d platformer without a window, the player is simply on your desktop
  - the player is animated, and the frames are loaded from indivudual images and assemebled into an atlas when loaded
  - Escape = close the demo
  - WASD and space = move and jump around
  - used to test transparent windows
- [`JustOneWindow`](program/Programs/JustOneWindow.cs)
  - tests creating a window from an entity, and that closing it finishes the demo
- [`MultipleWindowsAndFileDialog`](program/Programs/MultipleWindowsAndFileDialog.cs)
  - 2 windows to test file dialogs on a specific window
  - Z = open multiple files
  - X = open a single file
  - C = choose a file path to save to
  - V = choose a directory
- [`PhysicsDemo`](program/Programs/PhysicsDemo.cs)
  - fly around camera, non exhaustive test of physics integration
  - if the camera is close enough to the ball, and you hover over it, the ball changes color
  - Escape = close the demo
  - Arrow keys = tilt the floor
  - JLIK = resize the floor
  - R = reset the balls position and velocity
  - T = reset floor tilt
  - G = make the ball jump up
  - Right click = spawn a debug raycast visual
- [`SelectionTest`](program/Programs/SelectionTest.cs)
  - for debugging ui changing visuals when selected
  - 3 buttons on display
- [`SimpleProgram`](program/Programs/SimpleProgram.cs)
  - a windowless program that stays alive for 3 seconds then closes
- [`VoxelGameProgram`](<program/Programs/Voxel Game/VoxelGameProgram.cs>)
  - fly around camera, with a voxel terrain generator
  - block textures are loaded from individual images, and assembled into an atlas
  - has labels for average, max and current fps and is what i use to compare performance from update to update
- [`WindowCreationTest`](program/Programs/WindowCreationTest.cs)
  - automatic test where multiple windows get created, with their own unique ui
  - a red window, then a green and a blue window, then a yellow window
  - each of these windows have a resizable square on display
- [`WindowThatFollowsTheMouse`](program/Programs/WindowThatFollowsTheMouse.cs)
  - an empty window that follows the mouse around on screen
  - Escape = close the demo
  - Shift = increases/decrease follow speed