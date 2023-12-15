# Addressable Chunk Loader System
Addressable Chunk Loader is a package for loading rooms(mainly for the castle area) as addressable chunks/scenes 
for SS2 using addressables. It handles:
1. Loading/Unloading rooms/addressable chunks based on distance from player.
2. Marking rooms/chunks to always keep loaded for particular areas or game needs.
3. Async await API + events for handling room loading/unloading.

# Scene Setup:
1. Each room has an exterior and an interior scene.
2. Both scenes are addressable.
1. Exterior is loaded when player is in range
1. Interior is loaded when player enters the room(via door or function call)
1. The room interior should have a RoomInstance monobehavior with a RoomEntryZone.
1. RoomEntryZone detects player entry into a  trigger collider.
1. You can hit play mode in any interior scene
4. Keep a master scene and bake all scenes with that as the active scene. See lighting notes for details.

# Room loading:
A room can be loaded because:
1. It is in always load list of current room
radius distance from player within some threshold.
2. It is in always load list for current room/floor.
We may want to add other ways to determine if rooms should be loaded.
As long as one system says that a room should be kept loaded, we should keep it loaded.
3. If a room is no longer required at all, it is unloaded after a timer
RoomLoader handles actual loading:
4. It keeps a Dictionary<Room, RoomLoadHandle> for exterior and interior
Any room that will be loaded/is loaded is in the dicts
5. Once a room needs to be  unloaded(timer expired), it is also removed from the dict
RoomManager handles loading rooms in player range/ adding/removing must load rooms(based on story reqs or whatever)
6. You can wait till a room is loaded. In the samples, the door does not animate the door until the room is loaded.

# Usage:
1. Ensure that a RoomManager singleton exists in your scene.
Set the player ref on the RoomManager.
2. Use separate exterior and interior scenes per room.
3. The RoomData scriptable stores and uses room world position.
4. The scene root should always be in the room's correct position.
5. When the root position has been moved, go to the RoomInstance object and update the position
6. The gizmos indicate when they don't match to aid you.
7. You are responsible to ensure that the scriptable is updated. 

# Known issues:
Don't put the player objects in the rooms if you use RoomEntryZone:
1. If multiple rooms each with RoomEntryZone and player objects are loaded. 
1. The roommanager will consider the player to be at the last room. 
2. This depends on execution and room dependency order. Don't do it. 
3. Sample scenes has a player spawning example that avoids the above scenario .

# Lighting Notes:
## Do:
### Baking workflow:
1. Keep a "master" scene containing lightprobes for the other rooms.
2. Load the master scene.
3. Ensure that it is the active scene.
4. Additively load room interior and exterior
5. The additive scenes should share the same light settings asset.
6. Bake.
You must do this for any light update.

### In runtime:
As of yet, it doesn't seem necessary to actually load the master scene in runtime after baking.
So the package currently just loads assets separately.

### Do not:
1. Do not bake rooms separately.
2. Do not light probe them separately. Only the master scene should contain the light probes.

### Note:
Unity can handle lightmaps from different additive scenes. But not light probes. The above workflow using a master scene is a workaround for that.
