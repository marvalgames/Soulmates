__________________________________________________________________________________________

Package "Procedural Generation Grid"
Version 1.6.6.2.7 (Beta)

Made by FImpossible Creations - Filip Moeglich
https://www.FilipMoeglich.pl
FImpossibleGames@Gmail.com or Filip.Moeglich@Gmail.com

__________________________________________________________________________________________

Youtube: https://www.youtube.com/channel/UCDvDWSr6MAu1Qy9vX4w8jkw
Facebook: https://www.facebook.com/FImpossibleGames
Twitter (@FimpossibleC): https://twitter.com/FImpossibleC
Discord: https://discord.gg/rdD2Yu

__________________________________________________________________________________________

Package Contests:

Procedural Generation Grid - Demo Scenes.unitypackage
Package contains example scenes, scripts and assets showing how Procedural Generation Grid can be used.

Procedural Generation Grid - Assembly Definitions.unitypackage (Supported since Unity 2017)
Assembly Definition files to speed up compilation time of your project (Fimpossible Directories will have no influence on compilation time of whole project)

Lists of included visual scripting nodes: https://drive.google.com/drive/folders/1K9uJwOyIjPoQpD4KtPldYPqKy6reGAfe?usp=sharing

__________________________________________________________________________________________

Known issues: 

Some Unity versions, around 2019.4.1 have bug which is causing FieldSetup presets to break
when trying to rename it's file or duplicate it.


If you use Unity 2018.2 or lower DEMO prefabs might be broken.
Unfortunatelly Unity does not support back compatibility for prefabs on this versions.
Since examples database is so big (258 prefabs) I can't afford another two weeks of making example assets,
so please consider using Unity 2019.4+ for demo scenes as learning version.


When Unity opens with some glitched DPI setup then Grid Painter "Paint" button and some other
GUI is not visible, I wasn't able to fix it yet. 
If you have this issue, you an fix it by clicking right mouse button 
on running Unity App Icon -> Properties -> Compatibility -> Button on the bottom about changing DPI
-> toggle on the bottom about scaling dpi -> then select System -> Restart Unity Editor -> Now all should work!
(above solution is windows only)

__________________________________________________________________________________________

Description:

- Procedural Generation Grid is a system which uses grids to define fields and paths for procedural generation algorithms.

- System is designed to be flexible and able to generate very various and complex objects/places.

- It can be used to generate procedurally whole game levels at runtime or for editor use to quickly paint levels for speed up level design process.
 
- System is using something like visual scripting, so you don’t need to know how to code (but you can still take benefits out of it) you just need to plan logics for your needs.

- Procedural Generation Grid is right now in Beta Stage, so many features may change, new features will come and some small elements are not fully finished but developmental stage is completed enough to be functional.
 
- Package is providing additional packages for randomly placing smaller objects in physical space using Unity’s collision system (Object’s Stamper and Pipe Generator)

__________________________________________________________________________________________

Beta Version 1.6.6.2.7:
- Added Sync Instances Count Planner Operation

Beta Version 1.6.6.2.6:
- Grid Painter now will remove cells with the first mouse button when holding control in the same time (for mac users)

Beta Version 1.6.6.2.5:
- Now adding cell instructions to the cell is allowing to add multiple instructions with the same definition but different directions/positions/helper strings

Beta Version 1.6.6.2.4:
- Add Internal Field Planner value now has option to be Saved for later use after generating level

Beta Version 1.6.6.2.3:
- Added 'ApplyPrefabScale' property in 'Check If Collides' node
- Added 'InvolveTemporaryOffsets' propert in 'Draw Gizmo' node

Beta Version 1.6.6.2.2:
- Added 'Is Field Fully Contained By' planner node
- Added 'Get Random Cell With No Instruction' planner node
- Fixed Rect Generate Planner Field check for Parent Planner, solving null reference in the Function Nodes

Beta Version 1.6.6.2.1:
- Now Field Setup Spawners contains .WorldMatrix variable, containing information about generator's transform in world space
- New Field Setup Node: Check World Collision - Checking bounds collision with objects which are already on the scene

Beta Version 1.6.6.2:
- Added Build Planner 'Operations' feature to call custom script actions during certain generating stages
- Added Field Planner 'Operations' feature to call custom script actions during certain generating stages

Beta Version 1.6.6.1.16:
- Added "Nodes list" file with links to the nodes lists descriptions
https://drive.google.com/drive/folders/1K9uJwOyIjPoQpD4KtPldYPqKy6reGAfe?usp=sharing
- Added generic menu's options with links to the lists (right mouse button on the graph / field setup node)

Beta Version 1.6.6.1.15:
- When combining LOD group objects, using just LOD0 meshes for mesh combine operation

Beta Version 1.6.6.1.14:
- Object Stamper : Raycast alignment now works properly with main object rotation angles
- Fixed mesh combining transforming when exceeding vertex limits for a single mesh

Beta Version 1.6.6.1.13:
- Tight Placement Force Forward Rotation Toggle (after node expand)

Beta Version 1.6.6.1.12:
- Build Planner Command Indicator component to transfer commands within prefab into build plan
- Planner Mesh Ignore Component - to ignore using mesh in 'prefab to grid' calculations
- Build Planner: Get Direction From To node
- Build Planner: Get Cell Instruction by index node
- Build Planner: Updated logics inside few nodes
- Pathfind node customize remove overlaps code option
- Fixed Build Planner Executor Composition Switch when selecting object with executor on

Beta Version 1.6.6.1.11:
- Fixed gui for build planner executor prefab spawn mode

Beta Version 1.6.6.1.10:
- Added 'Open Node Script File' and 'Switch Debug Variable' options when hitting right mouse button on the Field Setup Node Header

Beta Version 1.6.6.1.9:
- New Planner Shape Generator: Complex/Random Tunnels (Limited)
- New Planner Node: "Remove Too Far Cells"
- New Planner Node: "Generate Bounds"
- New menu option after right-clicking on the planner node "Open Node Source Script File" to quickly jump into .cs file of the node

Beta Version 1.6.6.1.8:
- Added non 'Direct' offset mode for 'Check If Colliding' node
- 'Check If Colliding' collision data read operation will keep prefabs positions intact
- Added experimental 'Sorted' cells execution order

Beta Version 1.6.6.1.7:
- Added possibility to inject custom A* step cost for path finding
Example inside PR_PathFindGenerateLowerCost node
- Remove Overlapping Cells toggle for pathfind extra parameters (visible when clicked on the button next to the 'display pathfind setup' after expanding node)
- Tile Designer normals computing (phong smoothing) now is using different - more precise algorithm
- New: 'Allow Every Few' - Field Setup Node


Beta Version 1.6.6.1.6:
- Field Setup: Added Physics.SyncTransforms() call during object generation stages
- Added try-catch for destroying not needed MeshRenderers during mesh combining (to prevent error when some plugin depends on mesh renderers)
- Build Planner: Fixed few nodes to support reading cells of dynamically generated shapes
- Added "Near Walls Detection" build planner function node

Beta Version 1.6.6.1.5:
- Hotfix for part of Buid Planner Executor not initializing planner fields when dropping prefabed executor on scene
- Few small changes in mod graph nodes

Beta Version 1.6.6.1.4:
- Hotfix for 'Add' node when using Planners inputs, now list of grouped planners will clear in each generate call
- 'Add Cell Instruction' BP node now have new option "Add String Data"
- 'Iterate Cells' BP node now supports multiple planners input, so you don't need to use 'Iterate Planners' node.
- Cell Data refresh hotfix for Grid Painter

Beta Version 1.6.6.1.3:
- Get Coordinates FS Node's "Run On Repetition" feature now will inherit use of rules of the spawner (but still needs more testing)
- Add Cell Data FS Node now implements cells selector
- New Simple Dungeon Game Demo Scene
- Roof Plane Generator FS node : new "Thickness" feature
- Tile Designer - Advanced -> new 'Stacker' Generator
- Tile Designer FS Node - Multiple Meshes -> To generate new mesh for each spawn (useful for random tile design generators like Stacker, Cables Generator or for the Post Filters)
- Tile Designer FS Node - 'collider' mode string command
- Changed logic of 'Run on repetition' parameter, of "Get Coordinates" FS node
- Few small fixes in logic

Beta Version 1.6.6.1.2:
- New Expand Bounds Size FS Node
- Fix for Prefab Field GUI in Build Planner Window
- Extra options for Prefab to Bounds Shape Generator

Beta Version 1.6.6.1.1:
- Object Stamper Emitter - Overrider Implementation: Now you can use one stamper file and swap prefabs to spawn in the Object Stamp Emitter component window
- Walls Destroyer feature for the Stairs Placer FS Node.
- Edges Placer FS Node.
- Now Grid Painter's 'Cell Data Strings' are cleared properly.

Beta Version 1.6.6.1:
IF YOU USE ASSEMBLY DEFINITIONS: It's recommended to remove Fimpossible Creations/Plugins - Level Design
directory and import package again, (some files moved and package importer will not handle it properly)
and importing updated assembly definition packs.

- Possibility to display grid painter's cells as aligned boxes
- Fix for Grid Painter not saving cells after unity close
- Updated planner nodes methods to prevent displaying deprecated warning messages
- Few other fixes

> Field Setup:
- Randomize Field Variable Post Event (in contrary to "Post Event" name, it's actually pre-generating-event)

: Roof Generator Nodes (using new Volume Spawner API)
- Roof Plane Generator (Generating plane in roof shape aligned with grid size)
- Roof Side Wall Generator (Generating polygonal shape, filling gap on the roof sides)
- Roof Generator - Placer (to spawn prefabs like single roof plates)
- Roof Generator - Edges Placer (to spawn prefabs around roof edges/top edge)

- Mod Spawner API allows now to spawn prefabs no only in cells space, but also in generator space and world space

> Tile Designer:
- Tile Designer node now offers using project file preset for generating tile mesh in Field Spawning Setup
- Tile Designer custom commands implementation for setting basic changes in Tile Design for one tile generating purposes
- Implemented generated meshes Post Filters
- Fixed Project file Tile Design Preset handling on scripts recompilation during opened Tile Designer window

> Build Planner:
- Field Planners now can be 'Prefab' type
- Prefab to Grid shape generator
- Build Planner Executor now can spawn prefabs instead of just grid painters
- New Utility Node: Windows Placer
- New Node: Quick Align (using different algorithm than other nodes)
- New Node: Iterate List (can iterate List<object>)

> Other minor changes:
- Sub Spawners properties which was using variables, wasn't prepared properly : fixed

> For coders:
- IF you're using Assembly Definitions, please read info on the top of the changelog!
- If you was using shape generator 'DrawGUI()' now you need to provide extra argument of type FieldPlanner : override void DrawGUI(SerializedObject so, FieldPlanner parent)
> This changelog will be added as summary of the the upcoming 1.6.7 update

Beta Version 1.6.6:

This is smaller update, focusing on Field Setup features.

*There are plans to rename 'Field Setup' type as 'Field Spawning Setup'
- Grid Painter editor scene view performance now will be better, when having grids with a lof of cells.
- Implemented "Custom Post Events" for the Field Setup
- 'Generate Light Probes' is not a Post Event option anymore, but it's implemented as Custom Post Event as an Example
- 'Weld Vertices' of static meshes Custom Post Event
- 'Vertex Colorize' Post Event for random world space vertex painting on resulting combined meshes
- Combined meshes are inheriting game object tag set in the design setup

> Others:
- Field Variables now are handling Curves and Color values
- Mesh combine rotation fix
- Tile Designer better edited Tile Selector

> Build Planner:
- Push Out of Collision node algorithm improvement - now after one push it's checking if still colliding with something, it will cost more time to compute but it will be more precise
- New Node: "Remove Cells Under"
- New Node: "Apply Perlin Noise Height Offset"
- New example shape generator: "Custom/Perlin Noise Field"


Beta Version 1.6.5:

This update is focusing on the improvements and new features for the Build Planner (layout).
Main changes are listed down below:

>>> Build Planner:

>> New Shape Generators:
- Divided Rectangle : Dedicated for house generators
- Random Rectangle + Instructions : As example of generating shape with already provided cell instructions

>> New Coded Nodes:
- Choose Fields : To search for fields with certain condition (example: get fields which are on the bottom of the layout)
- Choose One Field : To search through multiple fields with certain condition (example: find biggest one)
- Check Bounds Sweep Collision In Direction
- Get Neightbour Field : Getting nearest aligning fields
- Choose Neightbour Fields : Getting multiple aligning fields if possible
- Get Most Middle Cell : To get most middle cell out of provided group of cells
- Generate Empty Shape : Generating temporary container for multiple cells
- Get Disconnected Chunks : Finding 'Island' grid cells groups
- Copy Other Cell Parameters
- Get Cell Instruction Parameters
- Get Colliding/Not Colliding With Cells
- Select Cells On Edge
- Get Cells Count
- Get Bounds Parameter
- Center Field Origin
- Execute On Read / Read On Execute : for handling some specific cases, mostly for inside Function Nodes (f)
- Get Fatten Shape : to make thin paths fatter

>> New Function Nodes (the ones created without coding (f) ):
- Get Biggset Field (out of provided group of fields)
- Get Smallest Field
- Is Free Space In Direction
- Get Fields With Free Space In Direction
- Join And Discard
- Add Centered Instruction On Side (For entrance doorways commands)
- Add Instructions On Sides (for windows commands)
- Copy Cell Instructions
- Count Occupied Cells Around
- Get Cells On Side
- Is Neightbour Outside
- Select Cells With Cell Data

>> Other Features:
- Get Field Bounds now is handling multiple fields for input : generating one big bounds out of all provided fields
- Check Contact In Direction now offers self collision check toggle
- Function Node now is supporting Planner Port values for Input and Output
- Local variables handling improvement for the Function Nodes
- Overall improvements for Function Node and bugfixes
- If function node detects incorrect reference to parent project file function, it will display GUI helping fixing it

>> Other Changes:
- Node Collapse Feature : Gives possibility to improve graphs view clarity
- If A==B => output true/false comparison node now will handle null value comparison properly
- Now 'Set Local Variable' Node value will be assigned when output port is not connected (working like simple variable definition)
- Nodes can switch to debug mode by selecting 'Switch Debug Variable' after hitting right mouse button on the node. 
Some of the nodes can debug log some info helpful for bebugging with this feature enabled.
Also with this feature enabled, planner ports and universal ports will display their contained values in the tooltip.
- Small improvements in nodes like: Line Generate, 
- Fixed Get Cell node to read references properly
- Fixed some Join Shape Cells node exception cases when using shape fields
- Fixed bounds diagonal parameter calculation
- Fixed bug with no possibility for comparing bool with bool value
- Now 'Add' node will join strings
- Fixed planner port (orange one) bugs + done complete cleanup and rewrite
- Clicking on Planner Ports will debug draw contained grid's cells on scene view for debugging
- Some 'if' switch nodes now have possibility to invert output bool result
- Iterate rotations node now have toggle to randomize rotations order
- Collect all fields now contains "Get Sub Fields" toggle (visible when expanded)
- Other changes in various nodes like new paramters when expanded
- 'Generate Field Connections' - 'Call Extra Connections' mode is now iterating through all provided fields (instead of just the first one) as it should
- Dedicated features for packed-aligned rectangle fields path connections generating: Generate Fields Connections Node - "Only Aligning" , Path Find Generate Node - Single Step Find Mode
- Fix for 'Schedule Mesh Combine' node to include child spawns in mesh combine (related with wall placer)
- Some Scene Gizmos Improvements for Build Planner Display
- Few GUI Fixes

> Field Setup:
- Changed terrain related algorithms eliminating raycasting parameters - they're not needed anymore
- 'Expand' parameter for 'Spawn Until Collides' Field Setup node

> Other not so important new nodes:
- New Node: Debug Draw Field Highlight - To quickly display placement and cells of provided field
- New Node: If Is Null return true/false
- New Logic: Convert any value to bool
- New Logic Node: Compare and return true/false
- New Math Nodes: Clamp, Choose Greater/Smaller (Math Min Max)
- New Field Setup Node: Remove Terrain Trees

For coders who made custom nodes:
- Now CheckerField3D are contained with ICheckerReference interface, which returns checker using .CheckerReference
It's handled this way to allow iterating FieldPlanners / checkers using one iterator instead of two separated ones (Field Planners is implementing ICheckerReference too)
- Many PGGPlannerPort methods now are obsolete, there was complete cleanup and rewrite of this port's code


Added in small updates after 1.6.4 and before 1.6.5:
>>> Build Planner:
- Fixed action execution chain call within Function Nodes
- Removed possibility to remove nodes in the Function Nodes using Right Mouse Buttom -> 'Remove' option. It was causing errors, as for now you can remove nodes in function node using Delete keyboard button
- New properties for path find node
- Generate Connections Node: Possibility for ignoring fields with specific tags
- Now Field Planners tags are working properly (with multiple tags support when using ",")

> New Nodes:
- Get Connected Cells (fill)
- Cell Contains Data String

- Some nodes now have few more options after expanding
- Few changes for properties displayed names
- Few improvements/fixes for some planner scripts

>>> Field Setup:
- Wall Placer Node : removing overlaps logic update
- Small GUI changes
- New Node 'Cut Terrain Hole'
- New Node 'Remove Terrain Details'
Nodes above requires any object to be set as 'to spawn', the empty option will not work there

>>> Object Stamper:
- Fixed usage of layer mask for obstacle overlap check

>>> Mod Graph: New 'Line Check Cells' node.

- Fix: Spawn Limit Count node now will count instances properly (distance to cells, zero spawns bug)
- Fix: Tight Placement now works correctly with fraction-size-cells like 2.5, 3.5 etc.
- Fix: Field Port will correctly provide multiple ports ('Add' node bug)
- Fix: Fix for Get Inline/Outline checker input handling (now temporary field shapes are supported)
- Fix: Lerp node fix
- Fix: Line Generate clear finish cells now is handled in a better way
- Fix: Shedule Mesh Combine now will affect child spawns (wall placer)
- New Logic Node: if 50% => Execute A or B
- Debugging Gizmos for 'Create Connections' node
- Updated some nodes display



Beta Version 1.6.4:

This Update is focusing on the new features for the Build Planner (Layout), but also implements few new nodes for Field Setup (Spawning on grid).

>>> Build Planner:
-> Custom Sub Graphs: Possibility to use multiple graphs in a Field Planner.
It's simply extension for 'First Procedures' and 'Post Procedures (new button between them).
Now you can add between procedures and customize their execution order with.

-> Path Find Node: Now the node is offering much more customization.
In the extra separated window you can define available directions for search and set rules for using directions.
With this node now you will be able to generate nice connections between Fields on the flat and multi-floor levels.
This node is not fully compleated yet. There will be many changes in the future versions.

-> Coding: Implemented "InternalValueVariables". It's variable which can be read per Field Planner instance.
Right now it's used only be new node "PR_PathFind_NearestAndMark" to remember how many path room connections exists.
- Reworked Planner ports, now it can transfer multiple checker fields and code is more readable for future changes.
Please check if the changes not broken some of your graph logics, there was a lot of changes done.
- Added Sub-Fields support. It allows to generate multiple grids without using instances.
It's used by new Shape Generator 'Shattered Rectangle' and by node 'Split Field'
- Reworked Matrix handling (position,rotation,scale) for layout fields, now it should work a bit faster.
There was a lot changes, if some of your planners stopped working after update, please report.

-> More Notes:
- Changed Field Planner icon (it's more blue now) to avoid confusing with Field Setup
- Join Field Cells Node change: foldout for auto-alignment toggle.
- Get Nearest Field Planner Node change: New custom condition bool input port (it's input but it's placed on the right side of node)
- When there are nodes in the post procedures graph the button will be light-blue to indicate content
- Cell Gizmos are displayed in a different way if of the same field cells are below/above.
- New Port Connection Draw Mode : "Line"
- Field Setup Compositions now implements Injections

-> New Build Planner Nodes: 
- Split Field
- Iterate Cells In Line
- Push Out Of Collision Away
- Bounds Separate Push Out
- Check Bounds Collision Between
- If switch => Execute A or B
- Is Field Aligning With
- Call Other Graph Logic
- Push In Dir Push Until Fully Contained By

-> New Shape Generator 'Complex/Shattered Rectangle' (in next versions will provide more slicing-randomization settings)

>>> Field Setup: 
- New node 'Double Doors Helper', 'Directional Remove', 'Stairs Placer Helper'
- Call Sub-Spawner code was reworked for more universal API, if you was using sub spawners and something stopped working please report!
- New Mod Graph Node: 'Iterate Cell Instructions'
- Cells Selector now will display it's 3D representation in simple view in the corner

>>> Tile Designer: Now cutting out polygons control over resulting normals on the mesh are better controlled


1.6.1.2:
- Build Planner: !early version! Added possibility to generate sub-fields (multiple grids without using instances)
Nodes: Add Sub Field, Get Sub Field By Index, Get Sub Field Count and methods to add Sub Fields which can be used in shape generator scripts (fieldPlanner.AddSubField(Checker3D))
- Now Build Planner Value Node can use Unity Engine Asset 'Object' type as port value
- Fixed Generating Light Probes in Field Setup Post Events (rewrite)
Also added Field Node "Disable Light Probes On Cell" to allow restricting which cells should have generated Light Probes.
- Few Small Fixes

1.6.1.1:
- Core Fimpossible scripts update
- Check Collision In Cell node now can generate bounds out of multiple renderers/colliders
- Now Build Planner Executor will transfer cell datas onto generated grids
- Fixed value planner node string value port drag


Beta Version 1.6.1:

> Object Stamper:
! New Physical Placement Feature !
- Physical Placement Settings for 'Stamp Emitter' and for the 'Multi Stamp Emitter' components
You can use raycasting + physics simulation, just physics simulation and just raycasting as you wish.
This feature is preparing isolated physics scene for generated objects and detected colliders around, simulating unity physics
fall for generated objects and giving final output in an instant (it costs some CPU - more time to generate but results are very useful)
- New simple 'Visual Stamp' component to randomize game start mesh material, object scale, renderer's meshes, particles play time etc.
- New simple 'Audio Stamp' component to randomize game start audio source clips / volume / pitch

> Field Setup:
- Physical Placement Settings for 'Stack Spawner' node for Field Setup
- New "Simulate Physics" node to drop-align your props with physics collision simulation
- Mesh Combine GUI switch in Modification Packs foldout under Field Setup Window
- Mesh Combining now supports meshes with multiple materials! (sub-meshes)
- Fixed issues with trigger colliders/reflection probes/light probes auto-generation


Beta Version 1.6.0:

> Field Setup:
- Now you can rename Field Modificators without using extra window
- Base Class for model generator nodes
- Cable Mesh Generator node
- "Check If Collides" node now allows to check collision in more cells around
- "Wall Placer" node now exposes new toggles which can help solving some setup cases

> Mod Graph:
- "Get Field Variable" node to read FieldSetup / ModificatorsPack variables in MOD Graph View.

> Tile Designer:
- New Cable Generator in custom mesh mode
- Improved UI of the combiner view
- New rotation mode switch in the combiner view

> Build Planner:
- Minimap Utilities for generating textures with shape of the grid
- Most of the minimap shape generators are not working with old generator components, it works with Grid Painters / Build Planner Executor
- A plenty of components for handling minimaps, minimap objects and generating minimap shapes out of Field generators
- Now you can disable auto-trigger-connection creation with the third button in top left corner of node graph

- Some small fixes for GUI, some nodes and editor performance

There was a plenty of changes in different nodes. 
If you experience some changes in your setups, please notify me so I will check logics to ensure it should work like that.


Beta Version 1.5.9:

> Field Setup:
- Added popup list button On top right of Field Mod window to switch between modificators of parent package
- Sub-Spawner node becomes deprecated
- New Sub-Spawners approach: You can add sub spawner by hitting '+ Add Spawner +' and selecting new option to add sub-spawner.
Sub-Spawners are stored in separated list.
You can call sub-spawners using 'Call Sub Spawner' node.
- Check cell neightbours node new toggle to enable 3D rotor check
- Few changes in Wall Placer node

> Tile Designer:
- Sweep Algorithm (for generating arches, pipes, branches etc.)
- Loft Height Shift option for height curve points (helpful for wall skew)
- Possiblity to maximize curve view with zoom features (early version)

> Build Planner New Nodes:
- Get Total Fields Count (just getting count of Field Planners or Instances of the Build Planner)
- Get Field Instance by Iteration, Get Instance of Planner (giving access to planners instances) 
- Get Global Index of Instance (identify which build planner instance it is) 

- Object Stamper: Now local space rotation will be used when spawning using multi emitter
- Few small fixes


Beta Version 1.5.8:
- Possibility to change Field Variable using 'Schedule Field Injection' node
- New Build Planner Nodes:
Set Cell Parameter, Detect Cell In, Get Most Cells Direction, Get Random Cell In, Is Cell Data In Range
- New Math Nodes:
Choose Random, Get Value By Axis, Vector Cross
- Few small fixes


Beta Version 1.5.7:
- Fixed generating multiple reflection probes on grids with different cell sizes
- Mesh combine will split mesh into multiple meshes if result has more than 65k verticles (unity combine limit, such combination was causing unity error!)
- Doorway command renderer center measure fix if using empty prefab (useful for clear wall commands)
- Floor placer advanced mode negation switch (useful for floor holes generating)
- Tile Designer Primitives (Adjustable cube with bevel, sphere, cylinder + experimental weld vertices feature for primitive models)
- Tile Designer Loft : Collapse Parameter (useful for roofs corners)
- Tile Designer : "Override Normal" values won't be normalized to allow controlling mesh deformation without limits (useful for roofs)


Beta Version 1.5.6:

Field Setup:
- New Node: Random Mesh - Generating object with mesh renderer with one of the meshes from provided list
- Possibility to call Grid Painter 'Generate Objects' through FieldSetup window instead of separated grid generation (including auto refreshing)
- Fix: Generating combined mesh object with Grid Painter will be removed when clicking "Clear Generated"
- Fix: Wall Placer will remove overlapping spawns after checking conditions of other nodes used in stack
- Wall Placer now can make overlapping spawns as ghosts: not spawning model but make spawn present in algorithm for getting coordinates/spawning props on walls etc.
- Reflection Probes now can be generated with limited size : resulting in multiple probes in for example long corridors instead of one long reflection probe
- Injections now will support Vector2 and Vector3 variables, strings and Unity Engine Project Objects
- Clicking right mouse button on the rename button in Cell Modificators list will result in auto-rename for the Field Modificator with the current command name
- If Field Command is using Field Mod which not belongs to the parent Field Setup, it will have blue background

Mod Graph:
- Possibility to export mod graph scheme into new file
- Possibility to call Mod Graph logics from file using Mod Graph Node
- New nodes (24) for node-based controll over spawning

All Node Graphs:
- New Vectors Node: Angle/Dot Product, Direction to rotation / rotation to direction conversion
- New Node: Bool Trigger - handy to call iteration break
- Node Ports now can switch between different styles for drawing wires in order to make long-wire connections look less dirty on the graph
just click right mouse button on some output port to display popup and switch draw mode
- If => Execute A or B Node now allows to pin multiple inputs onto bool port, which will result in checking them all in manner selected in foldout (visible after connecting multiple pins)
You can choose to require all bools to be true, or just one to be bool to forward execution.
- Enter with cursor on alternatively drawn connection to draw it in default style if some connections aren't clear
- Fix: If == equal nodes now will compare vector2 and vector3 values properly
- Fix: float ports now will read int values properly


Beta Version 1.5.5:

Field Designer:
- MOD GRAPH NODE! Which allows to use node graph for spawning logics. Now it contains just few simple nodes
added just for checking if everything is working, future updates will bring many more MOD GRAPH nodes. 

Tile Designer:
- Extrude Non-symmetrical mode (polygon like forming)
- Extrude with depth = 0 will result in caps polygons only
- Height curve for Loft (not adding subdivisions but working on distrubution curve's subdivs)
- Flip Normals/Face, UVOffset, UVRotate and UVScale tools for tile mesh instances (Combine bookmark)
- Added "Force Not Remove" mode in the tile instance combiner to exclude instances from removing shape operation
- Quick Edit button on the Tile Designer Node header when node is folded in

Scene Optimizing:
- Ability to combine generated prefabs onto single draw call mesh (per material)
> Use Combine Mesh node to schedule spawn prefab to be combined
> Modificators packages contains new switch in the inspector window "Combine Spawns", sheduling all spawned objects to be combined, without need for adding Combine Mesh nodes (then you can choose ignore combining selective spawners with this node)
> Each Modificator contains new switch on the top position of the inspector window "Combine" allowing to switch combining mode for all spawners
> Ignore Combining component which will not allow to combine selective mesh renderers if required

Build Planner:
- Split Node now will correctly allow to use Vector2 values
- Int ports now will read float input values correctly
- Vector3 ports now will read Vector2 and VectorInt input values correctly
- New node "Choose Random" allowing to choose randomly value from provided multiple connections to the node's input port
- New node "Push In Direction Until Collides" to push out field out of some collision in defined direction
- New node "Get Value by Axis" allowing to choose single Vector3 dimension using provided axis vector
- New node "Set Local Variable Allocated" allowing to set and remember value of the local variable for oprations in the field instance iterations
- New node "Rect Generate" generating rectangle shape of provided x,y,z dimensions values
- "Get Field Planner from Selector" when unfolded and connected using planner port will allow to choose instance id to provide
- When there is more than 12 nodes on the graph, off-screen nodes draw will be disabled to improve graph performance
- Fixed some issues with 'Remove Field Cells', 'Join Field Cells' operations when fields origins are changed (centerize origin and change origin methods was bugged)
- 'Get Nearest Cell' is not checking all cells distances (added 'fast' switch when node is folded out)
- Cosmetics: Added possibilty to write custom info in the build planners preset files


Beta Version 1.5.0:
- Introducing 'Build Planner' Node Graph System:
Node Graph system will be developed until the end of Beta Version of the Plugin.
It's early version of the system, first it will aim to provide nodes and features
to replace all of the other, hard-coded generators (facility, simple dungeon, rectangle of fields etc.)
Initial version report:
> Node Graph system with more than 70 coded nodes (much more in the future)
> 6 Initial Shape Generator algorithms
> Early manual update for the node graph system
> Build Planner Executor component to handle build plans
> Function Nodes Support (functions created out of other nodes - no compilation needed)

- Field Setup Compositions for grid painters: 
Allowing to replace variables, spawned prefabs and editing other aspects of any field setup.
- GUI update for the field setup to make it a bit more compact and quicker to read with some new icons.
- Grid painters now allows to add ghost cells: no spawning in cell but trated as cell in graph, good for creating empty space inside grid.

- Tile Designer window and Tile Designer Field Setup node.
Tile Designer allows to generate architecture tile shapes like walls, roofs, pillars
and create holes in meshes and spawn them on the scene / generate prefab without need to 
do models in the modelling software.


Beta Version 1.1.1:
- Beginner mode switch button for Field Designer Window, with just most important views to prepare FieldSetups.
- Draft workflow replaced with "New" button, it creates FieldSetup file in some directory, if you feel you can move FieldSetup to some custom directory you can use "Move" button, whe "Generate New" button functionality replaced by "+" button.

- Some new unity inspector fixes
- Some smaller fixes in build plan algorithm


Beta Version 1.1.0:

- New work-in-progress components with implemented new way of generating objects, making possible
instantiating just changed cells, async computing rules logics and instantiating game objects in coroutines.
This component are not yet supporting all features of the old components, but in some updates it will be fulfilled.
- New 3 example scenes: Playmode grid painting, Infinite run, 2.5D/3D platformer level
- Cell selector have option to display choosed cells in different perspectives, which can be helpful for setting rules based on vertical logics or 2D
- New Component "Pose Stamp" for random but partially defined random position/rotation setup
- Some GUI and Gizmos updates/fixes
- New Node: 'Helper Pivot Correction' helpful if you're using walls with pivot in corner, with this node you can adjust it to the center to make furniture placement easier!
- Node Update: Doorway node now can be debugged for correcting measure origins
- Now "Empty" prefab spawns will be visible in scene view gizmos as green sphere
- "Empty" and some debugging gizmos now can be displayed using Grid Painter when enabling "Draw Grid with Details" Debug draw mode (grid needs to be re-generated to display gizmos)
- You can choose to inherit or no rotations of previous cell spawns under spawners advanced settings (toggle on the right of "Cell Check Mode")
- Now new added rules will be hidden inside file, so unfolding preset files contents will not display them, if needed then it can be exposed with inspector window of Modificators Pack
- If you want to keep project more clean for searching field modificators, you can use Modificators Pack buttons for hiding/exposing their visibility, or using new button visible on header of Field Modificator ("A/B" button, package needs to be inside FieldSetup / ModPack in order to see this button)
- Clicking on prefab thumbnails in the FieldModificator window will switch to spawner which is spawning the prefab
- On the Unity 2021+ the injection list in build plan wasn't working - fixed

Variables Features (After 1.0.5):
- Spawner inspector window is drawing global rules with possibility to not use global rules on selected spawner
- ModificatorsPack can have added rules which can be triggered on all spawners inside pack / ignored for selective spawners
- ModificatorsPack can contain variables which can be used by nodes in the Package
- FieldSetup have option to display FieldVariables by type
- When hitting variable GUI dot there are new options like "Add Variable"
- Grid Painter component now have many options for handling variables, in next versions it will be available for every generator component
- Some smaller fixes

Beta Version 1.0.5:

- New Feature: Cell Selector Window which allows to freely select multiple cells to be executed on cells around, implemented by few other nodes (listed below)

- New Node: 'Operations/Stack Spawner' which works like Multi Objects Stamper without need of creating preset files etc. (needs some more testing)
- New Node: 'Operations/Pipe Spawner' which requires PipeGenerator preset to spawn objects with PipeGenerator algorithm
- New Node: 'Quick Solutions/Call Rules Logics Of' it will call nodes of other FieldModificator's spawner
- New Node: 'Transforming/Utilities/Prefab Offset' and 'Rotate On Repeat' first for correcting origin of spawned objects using default position inside prefab file and second for rotating object when using "Repeat" of Spawner

- Node Update: Floor Placer has possibility to advanced customize auto-placement
- Node Update: Most of the nodes under '/Cells' gets possibility to influence / check multiple cells with cell selector (new button opens selector window)
- Node Update: Check Cell Neightbours can have access to further cells than 9 around using cells selector (button opens selector window)

- Field Designer Window Update: Buttons for export Variant/Copy of FieldModificator are removed, this events can be triggered by hitting right mouse button on '[0]', '[1]' buttons on the left
- Field Designer Window Update: '+' button now adds automatically new FieldModificator without need to hitting 'IN' button after that, empty FieldModificator field still can be added with '+ []' button

- ModificatorsPack now have option to reset/set custom seed for generating (can be setted through inspector window)
- ModificatorsPack now have option to mass add additional tags to all spawners during generating time

- Grid Painter Update: If there is need to display more than 8 Field Commands they're bookmarked
- Grid Painter Update: More compact inspector window with "More Options" foldout
- Grid Painter Update: 2D paint toggle to draw cells in XY axis
- Grid Painter Update: Possibility to change FieldSetup variables for generation time (under More Options foldout)
- Grid Painter Update: Possibility to ignore selective FieldModificators out of FieldSetup (under More Options foldout)

- Shape Designer Window now can be used, for now only Facility Generator/Simple Dungeon Generator is supporting custom shapes
Shape designer window will have much more features in future updates. (like manual adding cell data / using rules to generate shapes and restrictions with relation to other grids in world)

- Create/GameObject Unity Editor menu now have new bookmark "/Generators" for quick creating game object with PGG components inside
- Some upgrades and fixes in PGG core


Beta Version 1.0.4:
- New Node + New Component: Align to Ground for placing object on ground accordingly to ground angle, very useful for placing on terrain
- New Node + New Component: Flatten Terrain for aligning Unity Terrain ground level under spawned objects
- New Node: Acquire Spawn: Mostly used for allowing /Modelling nodes to be executed without need of being added to spawner with spawning object, can be used after all spawns with "Empty" spawn
- New Node: Replace with Random Prefab: Which can be used instead of spawner's prefabs list
- New Node: Set Material Property: To change some material property on spawned object
- New Node: If World Position: Working in the same way like "Grid Position" but using world space position instead of local grid space positioning
- Node Update: Logic If Block: Added possibility to use "AND" "OR" conditions and breaking or triggering events mode
- Added buttons in spawner prefab list to automatically generate prefabs out of Model files and automatically adding colliders if not present in prefab (if you see button and press it when prefab already have some collider then no collider will be added, some prefab info just need to be updated)
- Improved some algorithms for Grid Painter component


Beta Version 1.0.3:
- New feature for copying/pasting FiledModificators by using right mouse button on "[0]" "[1]" buttons in FieldDesigner window in FieldModificators lists
- Changing Cell Size in Field Designer window should result in updated preview on the scene view
- Added "Generate" button on scene view gui for Grid Painter
- Added feature for working with "Draft" Field Setup in FieldDesigner window and exporting to file when ready
- Added option to "Copy All Properties" and "Paste All Properties" when hitting right mouse button on any rule header title (be careful, it needs to be tested to make sure everything works right)
- Added searchbox window for adding rules when using Unity 2019.4+
- Added "IF Logic Block" node to call events if some conditions are met inside node
- Added "Scale Range" node for more controll over objects random scaling
- Fixed "Get / Move-Scale-Rotate" nodes GetCoordinates logics
- Fixed "Fill Rectangle" feature not filling completely area for MiniCityGenerator
- Fixed some "GetCoordinates" logics using "Cell Size" unit size and added "Run on repetition" parameter (check tooltip in unity)
- Found and fixed some issues in AnalyzeCell node


Beta Version 1.0.2:
- New Generators Feature: Now generated rooms/areas can be outlined with any field setup using "Outline Fill" option in the "Additional Options" tab
- New Generators Feature: Now generated area can be covered with any field setup using "Fill Rectangle" option in the "Additional Options" tab
Above features are NOT available for the BuildingPlanGenerator (it's old component which isn't using newest framework of PGG)

- New Node: "Transforming/Noise/" - Perlin Noise Offset and - Rotation/Scale Perlin Noise Offset
- New Node: "Modelling/" - Set Random Mesh Material

- Some node moved to the different bookmark (node: IfRotated moved to /Placement)
- Some nodes moved to Transforming/Legacy bookmark (nodes: WorldOffset, DirectOffset, Rotate, Scale)
Their features can be replaced by single node "Move-Rotate-Scale" or "Get Position-Rotation-Scale"

- Changed default core asset rules for ClearWall modificator
- Some nodes will take a bit less space in the inpsector window

Fixes:
- Fixed BuildingPlanGenerator corridor doorway command to choose right command from the field setup
- Fixed some issue with generating doorways with BuildingPlanGenerator


Beta Version 1.0.1:
- DPI Fix for painting with Field Designer Window
- In Door Placer node added option to measure remove distance with object's bounds
- More Options under "Window->Fimpossible Creations->Level Design" and added presets scheme to the QuickStart.pdf file
- New Node "Duplicate Spawns"
- Added PGG Starter Window displayed only once when importing package, then it can be displayed by going to "Window->Fimpossible Creations->Level Design->Display PGG Startup Window"
- Now build plan shows arrows to change oder of FieldSetups, order will have influence on FacilityGenerator algorithm if disabling "Shuffle Plan Order"
- Updated and modified some "Checker" framework methods for Generators like FacilityGenerator/SimpleDungeonGenerator
! Previously generated levels with this generators can look different now even when using the same seed!


Beta Version 1.0.0.1:
- DPI fix for Grid Painter Component
- Added extensions to Vector2Int to support old Unity Versions like 2017.4
- Added links to PGG tutorials and manual in Window/Fimpossible...
- Fixed SetMaterial rule to interate through all renderers